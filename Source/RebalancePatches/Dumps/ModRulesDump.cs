using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Rule defs belonging to other mods that gate what a pawn may receive, read reflectively so
    /// this compiles and runs without any of them installed.
    ///
    /// Today that means VRE - Android's settings def, which decides which recipes, hediffs and
    /// traits an android is refused. It is the surface the android blocklist work edits, and it is
    /// worth its own dump because the interesting fact is what it *fails* to mention: the list ships
    /// covering vanilla and DLC content only, so every modded implant bypasses it.
    ///
    /// Structured as a general "other mods' rule defs" dump so the next framework that gates content
    /// this way has somewhere to go instead of a second bespoke dumper.
    /// </summary>
    internal static class ModRulesDump
    {
        /// <summary>Def type full name → the string-list fields worth recording off it.</summary>
        private static readonly Dictionary<string, string[]> RuleDefs = new Dictionary<string, string[]>
        {
            ["VREAndroids.AndroidSettings"] = new[]
            {
                "disallowedRecipes", "androidsShouldNotReceiveHediffs", "disallowedTraits",
                "allowedTraits", "excludedNeedsForAndroids", "androidExclusiveNeeds",
                "androidSpecificMentalBreaks",
            },
        };

        [DebugAction("RebalancePatches", "Dump mod rules", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump mod rules", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(new Type[0]);
            int found = 0;

            DumpRunner.Run("ModRulesDump.json", walker, w =>
            {
                Json j = w.Json;
                j.Name("ruleDefs");
                j.BeginObject();
                foreach (var pair in RuleDefs.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    Type type = GenTypes.GetTypeInAnyAssembly(pair.Key);
                    j.Name(pair.Key);
                    j.BeginObject();
                    j.Name("present"); j.Value(type != null);
                    if (type != null)
                    {
                        object def = AllDefsOfType(type).FirstOrDefault();
                        j.Name("defFound"); j.Value(def != null);
                        if (def != null)
                        {
                            found++;
                            foreach (string field in pair.Value)
                            {
                                j.Name(field);
                                j.BeginArray();
                                foreach (string entry in ReadStrings(def, field))
                                    j.Value(entry);
                                j.EndArray();
                            }
                        }
                    }
                    j.EndObject();
                }
                j.EndObject();
            }, () => $"{found} of {RuleDefs.Count} rule defs present");
        }

        private static IEnumerable<string> ReadStrings(object obj, string fieldName)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (!(field?.GetValue(obj) is IEnumerable list)) return Enumerable.Empty<string>();
            var result = new List<string>();
            foreach (object item in list)
            {
                if (item is string s) result.Add(s);
                else if (item is Def d) result.Add(d.defName);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static IEnumerable<object> AllDefsOfType(Type defType)
        {
            Type db = typeof(DefDatabase<>).MakeGenericType(defType);
            PropertyInfo prop = db.GetProperty("AllDefsListForReading", BindingFlags.Public | BindingFlags.Static);
            return prop?.GetValue(null) is IEnumerable list ? list.Cast<object>() : Enumerable.Empty<object>();
        }
    }
}
