using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Reflective def-to-JSON writer shared by the dev database dumps. Walks every public instance
    /// field, skipping anything still at its type's constructed default, so a dump shows what a def
    /// actually says rather than the whole class surface. Defs referenced by a walked field are
    /// queued and written once each into a "referenced" block, which keeps the dump self-contained
    /// without duplicating a def per mention.
    ///
    /// Extracted from GeneDump so the implant dump can reuse it; behaviour is unchanged.
    /// </summary>
    internal sealed class DefWalker
    {
        private readonly HashSet<string> skippedFields;
        private readonly Type[] referencedDefTypes;
        private readonly HashSet<Type> bareDefTypes;
        private readonly int maxDepth;

        private readonly HashSet<object> stack = new HashSet<object>(ReferenceComparer.Instance);
        private readonly Dictionary<Type, object> baselines = new Dictionary<Type, object>();
        private readonly Queue<Def> pendingRefs = new Queue<Def>();
        private readonly HashSet<Def> queuedRefs = new HashSet<Def>();

        public readonly Json Json = new Json();
        public int ReferencedCount;

        /// <summary>Fields that are noise in every dump: identity, engine bookkeeping, art.</summary>
        public static readonly HashSet<string> DefaultSkippedFields = new HashSet<string>
        {
            "defName", "label", "description", "modContentPack", "index", "shortHash", "fileName",
            "generated", "debugRandomId", "descriptionHyperlinks", "modExtensions", "iconPath",
            "renderNodeProperties",
        };

        /// <param name="referencedDefTypes">Def types to follow and emit in the referenced block.</param>
        /// <param name="bareDefTypes">Def types that are the dump's subject, so they need no $defType tag.</param>
        public DefWalker(Type[] referencedDefTypes, Type[] bareDefTypes = null,
            HashSet<string> skippedFields = null, int maxDepth = 8)
        {
            this.referencedDefTypes = referencedDefTypes ?? new Type[0];
            this.bareDefTypes = new HashSet<Type>(bareDefTypes ?? new Type[0]);
            this.skippedFields = skippedFields ?? DefaultSkippedFields;
            this.maxDepth = maxDepth;
        }

        /// <summary>Writes one def as a JSON object. <paramref name="extra"/> adds computed fields.</summary>
        public void WriteDefEntry(Def def, Action<Def> extra = null)
        {
            Json.BeginObject();
            Json.Name("defName"); Json.Value(def.defName);
            Json.Name("label"); Json.Value(def.label);
            Json.Name("mod"); Json.Value(def.modContentPack != null
                ? $"{def.modContentPack.Name} [{def.modContentPack.PackageIdPlayerFacing}]"
                : "?");
            if (!bareDefTypes.Contains(def.GetType()))
            {
                Json.Name("$defType"); Json.Value(def.GetType().FullName);
            }
            if (!def.description.NullOrEmpty())
            {
                Json.Name("description"); Json.Value(def.description);
            }

            extra?.Invoke(def);

            WriteFields(def, 1);

            if (!def.modExtensions.NullOrEmpty())
            {
                Json.Name("modExtensions");
                Json.BeginArray();
                foreach (DefModExtension ext in def.modExtensions)
                    WriteValue(ext, typeof(DefModExtension), 1);
                Json.EndArray();
            }
            Json.EndObject();
        }

        /// <summary>Drains the queued referenced defs into a named JSON object. Call last.</summary>
        public void WriteReferencedBlock(string name)
        {
            Json.Name(name);
            Json.BeginObject();
            var written = new HashSet<Def>();
            while (pendingRefs.Count > 0)
            {
                Def def = pendingRefs.Dequeue();
                if (!written.Add(def))
                    continue;
                Json.Name($"{def.GetType().Name}:{def.defName}");
                WriteDefEntry(def);
                ReferencedCount++;
            }
            Json.EndObject();
        }

        public void WriteFields(object obj, int depth)
        {
            Type type = obj.GetType();
            object baseline = GetBaseline(type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (skippedFields.Contains(field.Name))
                    continue;
                if (field.IsDefined(typeof(UnsavedAttribute), true))
                    continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) || typeof(Delegate).IsAssignableFrom(field.FieldType))
                    continue;

                object value;
                try { value = field.GetValue(obj); }
                catch { continue; }

                object baseValue = null;
                if (baseline != null)
                {
                    try { baseValue = field.GetValue(baseline); }
                    catch { }
                }
                if (IsDefaultLike(value, baseValue))
                    continue;

                Json.Name(field.Name);
                WriteValue(value, field.FieldType, depth + 1);
            }
        }

        public void WriteValue(object value, Type declaredType, int depth)
        {
            if (value == null)
            {
                Json.Null();
                return;
            }
            switch (value)
            {
                case string s: Json.Value(s); return;
                case bool b: Json.Value(b); return;
                case char c: Json.Value(c.ToString()); return;
                case Type t: Json.Value(t.FullName); return;
                case CurvePoint cp: Json.BeginArray(); Json.Value(cp.x); Json.Value(cp.y); Json.EndArray(); return;
                case UnityEngine.Color col: Json.Value($"RGBA({col.r:0.###}, {col.g:0.###}, {col.b:0.###}, {col.a:0.###})"); return;
                case FloatRange fr: Json.Value(fr.ToString()); return;
                case IntRange ir: Json.Value(ir.ToString()); return;
                case Def def:
                    Json.Value(def.defName ?? def.label ?? "?");
                    QueueReferenced(def);
                    return;
            }

            Type type = value.GetType();
            if (type.IsEnum)
            {
                Json.Value(value.ToString());
                return;
            }
            if (type.IsPrimitive || value is decimal)
            {
                Json.Number(value);
                return;
            }
            if (depth > maxDepth)
            {
                Json.Value("<max-depth>");
                return;
            }
            if (value is IDictionary dict)
            {
                Json.BeginObject();
                foreach (DictionaryEntry entry in dict)
                {
                    Json.Name(entry.Key is Def keyDef ? keyDef.defName : entry.Key?.ToString() ?? "null");
                    WriteValue(entry.Value, typeof(object), depth + 1);
                }
                Json.EndObject();
                return;
            }
            if (value is IEnumerable enumerable)
            {
                Json.BeginArray();
                foreach (object item in enumerable)
                    WriteValue(item, typeof(object), depth + 1);
                Json.EndArray();
                return;
            }

            bool isRefType = !type.IsValueType;
            if (isRefType && !stack.Add(value))
            {
                Json.Value("<cycle>");
                return;
            }
            Json.BeginObject();
            if (type != declaredType)
            {
                Json.Name("$type"); Json.Value(type.FullName);
            }
            WriteFields(value, depth);
            Json.EndObject();
            if (isRefType)
                stack.Remove(value);
        }

        private static bool IsDefaultLike(object value, object baseValue)
        {
            if (value == null)
                return true;
            if (value is string s)
                return s.Length == 0 || Equals(s, baseValue as string);
            if (value is ICollection col)
                return col.Count == 0;
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum)
                return value.Equals(baseValue ?? (type.IsValueType ? Activator.CreateInstance(type) : null));
            return false;
        }

        private object GetBaseline(Type type)
        {
            if (baselines.TryGetValue(type, out object baseline))
                return baseline;
            try { baseline = Activator.CreateInstance(type); }
            catch { baseline = null; }
            baselines[type] = baseline;
            return baseline;
        }

        private void QueueReferenced(Def def)
        {
            for (int i = 0; i < referencedDefTypes.Length; i++)
            {
                if (referencedDefTypes[i].IsInstanceOfType(def))
                {
                    if (queuedRefs.Add(def))
                        pendingRefs.Enqueue(def);
                    return;
                }
            }
        }
    }
}
