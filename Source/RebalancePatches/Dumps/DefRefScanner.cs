using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Finds where one def is referenced by another, by walking object graphs reflectively rather
    /// than knowing each mod's comp and generator shapes. Answers "what can give the player this?"
    /// for any def type — genes, implants, weapons — which is why it lives outside any one dump.
    ///
    /// The walk stops at the first Def it meets, so it records the reference without wandering into
    /// that def's own graph and dragging in half the database.
    /// </summary>
    internal static class DefRefScanner
    {
        public const int DefaultMaxDepth = 8;

        public sealed class Hit
        {
            public string Root, Mod, Via;
        }

        public sealed class Results
        {
            public readonly Dictionary<Def, List<Hit>> Hits = new Dictionary<Def, List<Hit>>();
            public int Count;

            public void Add(Def def, string root, string mod, string via)
            {
                if (!Hits.TryGetValue(def, out List<Hit> list))
                    Hits[def] = list = new List<Hit>();
                list.Add(new Hit { Root = root, Mod = mod, Via = via });
                Count++;
            }
        }

        /// <summary>
        /// Scans every public field of each root. Use when any reference counts, wherever it hides.
        /// </summary>
        public static void ScanAllFields(Results results, IEnumerable<Def> roots, Func<Def, bool> isTarget,
            int maxDepth = DefaultMaxDepth)
        {
            foreach (Def root in roots)
            {
                var visited = new HashSet<object>(ReferenceComparer.Instance);
                string label = $"{root.GetType().Name}:{root.defName}";
                string mod = root.modContentPack?.PackageIdPlayerFacing ?? "?";
                // Walk the root's fields, not the root: the scan stops at the first Def, and the
                // root is one.
                foreach (FieldInfo field in root.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object value;
                    try { value = field.GetValue(root); }
                    catch { continue; }
                    Scan(results, value, label, mod, field.Name, isTarget, visited, 0, maxDepth);
                }
            }
        }

        /// <summary>
        /// Scans one value under a caller-chosen label. Use when only certain fields of a root are
        /// interesting and a full sweep would bury the signal.
        /// </summary>
        public static void Scan(Results results, object obj, string root, string mod, string via,
            Func<Def, bool> isTarget, HashSet<object> visited, int depth = 0, int maxDepth = DefaultMaxDepth)
        {
            if (obj == null || depth > maxDepth)
                return;
            if (obj is Def def)
            {
                if (isTarget(def))
                    results.Add(def, root, mod, via);
                return;
            }
            if (obj is string)
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
                    Scan(results, entry.Key, root, mod, via, isTarget, visited, depth + 1, maxDepth);
                    Scan(results, entry.Value, root, mod, via, isTarget, visited, depth + 1, maxDepth);
                }
                return;
            }
            if (obj is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                    Scan(results, item, root, mod, via, isTarget, visited, depth + 1, maxDepth);
                return;
            }
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value;
                try { value = field.GetValue(obj); }
                catch { continue; }
                Scan(results, value, root, mod, $"{type.Name}.{field.Name}", isTarget, visited, depth + 1, maxDepth);
            }
        }

        public static void Write(Json j, string name, Results results)
        {
            j.Name(name);
            j.BeginObject();
            foreach (var pair in results.Hits.OrderBy(p => p.Key.defName, StringComparer.Ordinal))
            {
                j.Name($"{pair.Key.GetType().Name}:{pair.Key.defName}");
                j.BeginArray();
                foreach (Hit hit in pair.Value)
                {
                    j.BeginObject();
                    j.Name("root"); j.Value(hit.Root);
                    j.Name("mod"); j.Value(hit.Mod);
                    j.Name("via"); j.Value(hit.Via);
                    j.EndObject();
                }
                j.EndArray();
            }
            j.EndObject();
        }
    }
}
