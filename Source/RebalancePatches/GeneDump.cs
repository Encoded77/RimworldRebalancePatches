using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches
{
    internal static class GeneDump
    {
        private const int MaxDepth = 8;

        private static readonly HashSet<string> SkippedFields = new HashSet<string>
        {
            "defName", "label", "description", "modContentPack", "index", "shortHash", "fileName",
            "generated", "debugRandomId", "descriptionHyperlinks", "modExtensions", "iconPath",
            "renderNodeProperties",
        };

        private static readonly Type[] ReferencedDefTypes =
        {
            typeof(HediffDef), typeof(AbilityDef), typeof(ThoughtDef), typeof(NeedDef), typeof(MentalBreakDef),
        };

        [DebugAction("RebalancePatches", "Dump gene database", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpGeneDatabaseEntry() => DumpGeneDatabase();

        [DebugAction("RebalancePatches", "Dump gene database", allowedGameStates = AllowedGameStates.Playing)]
        internal static void DumpGeneDatabase()
        {
            try
            {
                string dir = Path.Combine(GenFilePaths.SaveDataFolderPath, "RebalancePatches", "tmp");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "GeneDump.json");
                var ctx = new DumpContext();
                WriteDump(ctx);
                File.WriteAllText(path, ctx.Json.ToString(), Encoding.UTF8);
                Log.Message($"[RebalancePatches] Gene dump: {ctx.GeneCount} genes, {ctx.XenotypeCount} xenotypes, {ctx.ReferencedCount} referenced defs, {ctx.AcquisitionHits} acquisition hits -> {path}");
            }
            catch (Exception e)
            {
                Log.Error($"[RebalancePatches] Gene dump failed: {e}");
            }
        }

        private sealed class DumpContext
        {
            public readonly Json Json = new Json();
            public readonly Queue<Def> PendingRefs = new Queue<Def>();
            public readonly HashSet<Def> QueuedRefs = new HashSet<Def>();
            public readonly HashSet<object> Stack = new HashSet<object>(ReferenceComparer.Instance);
            public readonly Dictionary<Type, object> Baselines = new Dictionary<Type, object>();
            public int GeneCount, XenotypeCount, ReferencedCount, AcquisitionHits;
        }

        private static void WriteDump(DumpContext ctx)
        {
            Json j = ctx.Json;
            j.BeginObject();

            j.Name("meta");
            j.BeginObject();
            j.Name("gameVersion"); j.Value(VersionControl.CurrentVersionStringWithRev);
            j.Name("dumpedAt"); j.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            j.Name("activeMods");
            j.BeginArray();
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
                j.Value(mod.PackageIdPlayerFacing);
            j.EndArray();
            j.EndObject();

            j.Name("genes");
            j.BeginArray();
            foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefsListForReading.OrderBy(g => g.defName, StringComparer.Ordinal))
            {
                WriteDefEntry(ctx, gene);
                ctx.GeneCount++;
            }
            j.EndArray();

            j.Name("xenotypes");
            j.BeginArray();
            foreach (XenotypeDef xeno in DefDatabase<XenotypeDef>.AllDefsListForReading.OrderBy(x => x.defName, StringComparer.Ordinal))
            {
                WriteDefEntry(ctx, xeno);
                ctx.XenotypeCount++;
            }
            j.EndArray();

            j.Name("referenced");
            j.BeginObject();
            var written = new HashSet<Def>();
            while (ctx.PendingRefs.Count > 0)
            {
                Def def = ctx.PendingRefs.Dequeue();
                if (!written.Add(def))
                    continue;
                j.Name($"{def.GetType().Name}:{def.defName}");
                WriteDefEntry(ctx, def);
                ctx.ReferencedCount++;
            }
            j.EndObject();

            WriteAcquisition(ctx);

            j.EndObject();
        }

        private static void WriteDefEntry(DumpContext ctx, Def def)
        {
            Json j = ctx.Json;
            j.BeginObject();
            j.Name("defName"); j.Value(def.defName);
            j.Name("label"); j.Value(def.label);
            j.Name("mod"); j.Value(def.modContentPack != null ? $"{def.modContentPack.Name} [{def.modContentPack.PackageIdPlayerFacing}]" : "?");
            if (def.GetType() != typeof(GeneDef) && def.GetType() != typeof(XenotypeDef))
            {
                j.Name("$defType"); j.Value(def.GetType().FullName);
            }
            if (!def.description.NullOrEmpty())
            {
                j.Name("description"); j.Value(def.description);
            }
            if (def is GeneDef g && !g.renderNodeProperties.NullOrEmpty())
            {
                j.Name("hasGraphics"); j.Value(true);
            }

            WriteFields(ctx, def, 1);

            if (!def.modExtensions.NullOrEmpty())
            {
                j.Name("modExtensions");
                j.BeginArray();
                foreach (DefModExtension ext in def.modExtensions)
                    WriteValue(ctx, ext, typeof(DefModExtension), 1);
                j.EndArray();
            }
            j.EndObject();
        }

        private static void WriteFields(DumpContext ctx, object obj, int depth)
        {
            Type type = obj.GetType();
            object baseline = GetBaseline(ctx, type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (SkippedFields.Contains(field.Name))
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

                ctx.Json.Name(field.Name);
                WriteValue(ctx, value, field.FieldType, depth + 1);
            }
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

        private static object GetBaseline(DumpContext ctx, Type type)
        {
            if (ctx.Baselines.TryGetValue(type, out object baseline))
                return baseline;
            try { baseline = Activator.CreateInstance(type); }
            catch { baseline = null; }
            ctx.Baselines[type] = baseline;
            return baseline;
        }

        private static void WriteValue(DumpContext ctx, object value, Type declaredType, int depth)
        {
            Json j = ctx.Json;
            if (value == null)
            {
                j.Null();
                return;
            }
            switch (value)
            {
                case string s: j.Value(s); return;
                case bool b: j.Value(b); return;
                case char c: j.Value(c.ToString()); return;
                case Type t: j.Value(t.FullName); return;
                case CurvePoint cp: j.BeginArray(); j.Value(cp.x); j.Value(cp.y); j.EndArray(); return;
                case Color col: j.Value($"RGBA({col.r:0.###}, {col.g:0.###}, {col.b:0.###}, {col.a:0.###})"); return;
                case FloatRange fr: j.Value(fr.ToString()); return;
                case IntRange ir: j.Value(ir.ToString()); return;
                case Def def:
                    j.Value(def.defName ?? def.label ?? "?");
                    QueueReferenced(ctx, def);
                    return;
            }

            Type type = value.GetType();
            if (type.IsEnum)
            {
                j.Value(value.ToString());
                return;
            }
            if (type.IsPrimitive || value is decimal)
            {
                j.Number(value);
                return;
            }
            if (depth > MaxDepth)
            {
                j.Value("<max-depth>");
                return;
            }
            if (value is IDictionary dict)
            {
                j.BeginObject();
                foreach (DictionaryEntry entry in dict)
                {
                    j.Name(entry.Key is Def keyDef ? keyDef.defName : entry.Key?.ToString() ?? "null");
                    WriteValue(ctx, entry.Value, typeof(object), depth + 1);
                }
                j.EndObject();
                return;
            }
            if (value is IEnumerable enumerable)
            {
                j.BeginArray();
                foreach (object item in enumerable)
                    WriteValue(ctx, item, typeof(object), depth + 1);
                j.EndArray();
                return;
            }

            bool isRefType = !type.IsValueType;
            if (isRefType && !ctx.Stack.Add(value))
            {
                j.Value("<cycle>");
                return;
            }
            j.BeginObject();
            if (type != declaredType)
            {
                j.Name("$type"); j.Value(type.FullName);
            }
            WriteFields(ctx, value, depth);
            j.EndObject();
            if (isRefType)
                ctx.Stack.Remove(value);
        }

        private static void QueueReferenced(DumpContext ctx, Def def)
        {
            for (int i = 0; i < ReferencedDefTypes.Length; i++)
            {
                if (ReferencedDefTypes[i].IsInstanceOfType(def))
                {
                    if (ctx.QueuedRefs.Add(def))
                        ctx.PendingRefs.Enqueue(def);
                    return;
                }
            }
        }

        private sealed class AcquisitionHit
        {
            public string Root, Mod, Via;
        }

        private static void WriteAcquisition(DumpContext ctx)
        {
            var hits = new Dictionary<Def, List<AcquisitionHit>>();

            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var visited = new HashSet<object>(ReferenceComparer.Instance);
                string root = $"ThingDef:{thing.defName}";
                string mod = thing.modContentPack?.Name ?? "?";
                ScanForGeneRefs(thing.comps, root, mod, "comps", hits, visited, 0);
                ScanForGeneRefs(thing.modExtensions, root, mod, "modExtensions", hits, visited, 0);
                ScanForGeneRefs(thing.ingestible, root, mod, "ingestible", hits, visited, 0);
            }
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                var visited = new HashSet<object>(ReferenceComparer.Instance);
                string root = $"RecipeDef:{recipe.defName}";
                string mod = recipe.modContentPack?.Name ?? "?";
                foreach (FieldInfo field in recipe.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object value;
                    try { value = field.GetValue(recipe); }
                    catch { continue; }
                    ScanForGeneRefs(value, root, mod, field.Name, hits, visited, 0);
                }
            }

            Json j = ctx.Json;
            j.Name("acquisition");
            j.BeginObject();
            foreach (var pair in hits.OrderBy(p => p.Key.defName, StringComparer.Ordinal))
            {
                j.Name($"{pair.Key.GetType().Name}:{pair.Key.defName}");
                j.BeginArray();
                foreach (AcquisitionHit hit in pair.Value)
                {
                    j.BeginObject();
                    j.Name("root"); j.Value(hit.Root);
                    j.Name("mod"); j.Value(hit.Mod);
                    j.Name("via"); j.Value(hit.Via);
                    j.EndObject();
                    ctx.AcquisitionHits++;
                }
                j.EndArray();
            }
            j.EndObject();
        }

        private static void ScanForGeneRefs(object obj, string root, string mod, string via, Dictionary<Def, List<AcquisitionHit>> hits, HashSet<object> visited, int depth)
        {
            if (obj == null || depth > MaxDepth)
                return;
            if (obj is GeneDef || obj is XenotypeDef)
            {
                var def = (Def)obj;
                if (!hits.TryGetValue(def, out List<AcquisitionHit> list))
                    hits[def] = list = new List<AcquisitionHit>();
                list.Add(new AcquisitionHit { Root = root, Mod = mod, Via = via });
                return;
            }
            if (obj is Def || obj is string)
                return;
            Type type = obj.GetType();
            if (type.IsPrimitive || type.IsEnum || obj is Type || obj is Delegate || obj is UnityEngine.Object)
                return;
            if (!type.IsValueType && !visited.Add(obj))
                return;

            if (obj is IDictionary dict)
            {
                foreach (DictionaryEntry entry in dict)
                {
                    ScanForGeneRefs(entry.Key, root, mod, via, hits, visited, depth + 1);
                    ScanForGeneRefs(entry.Value, root, mod, via, hits, visited, depth + 1);
                }
                return;
            }
            if (obj is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                    ScanForGeneRefs(item, root, mod, via, hits, visited, depth + 1);
                return;
            }
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value;
                try { value = field.GetValue(obj); }
                catch { continue; }
                ScanForGeneRefs(value, root, mod, $"{type.Name}.{field.Name}", hits, visited, depth + 1);
            }
        }
    }
}
