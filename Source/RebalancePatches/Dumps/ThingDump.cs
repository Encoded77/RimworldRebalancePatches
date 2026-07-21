using System;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Every item and building ThingDef. Implants, weapons, apparel and workbenches all live here,
    /// which is what makes this the file the weapon and armor audits will read too.
    ///
    /// Pawns, plants, filth, motes and projectiles are skipped: they multiply the file size several
    /// times over and no audit that reads this file asks about them.
    ///
    /// Stat values are resolved rather than left as statBases, because a def's real market value or
    /// work cost often comes from its parent chain or a stat part, and neither survives the dump.
    /// </summary>
    internal static class ThingDump
    {
        private static readonly Type[] Referenced =
        {
            typeof(ThingCategoryDef), typeof(StatDef), typeof(ResearchProjectDef), typeof(RecipeDef),
            typeof(HediffDef), typeof(BodyPartDef), typeof(StuffCategoryDef), typeof(TraitDef),
        };

        [DebugAction("RebalancePatches", "Dump things", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump things", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(Referenced, bareDefTypes: new[] { typeof(ThingDef) });
            int total = 0;

            DumpRunner.Run("ThingDump.json", walker, w =>
            {
                Json j = w.Json;
                j.Name("things");
                j.BeginArray();
                foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefsListForReading
                             .Where(t => t.category == ThingCategory.Item || t.category == ThingCategory.Building)
                             .OrderBy(t => t.defName, StringComparer.Ordinal))
                {
                    w.WriteDefEntry(thing, _ =>
                    {
                        j.Name("category"); j.Value(thing.category.ToString());
                        j.Name("isImplantLike"); j.Value(IsImplantLike(thing));

                        // Resolved stats: inherited and stat-part contributions are invisible in
                        // statBases, and every costing decision is made against the resolved value.
                        j.Name("resolvedStats");
                        j.BeginObject();
                        WriteStat(j, thing, StatDefOf.MarketValue, "MarketValue");
                        WriteStat(j, thing, StatDefOf.WorkToMake, "WorkToMake");
                        WriteStat(j, thing, StatDefOf.Mass, "Mass");
                        WriteStat(j, thing, StatDefOf.MaxHitPoints, "MaxHitPoints");
                        j.EndObject();

                        // Which recipes produce it, so "how do I get one" is answerable without
                        // scanning every recipe in the analyzer.
                        j.Name("producedBy");
                        j.BeginArray();
                        foreach (string recipe in DefDatabase<RecipeDef>.AllDefsListForReading
                                     .Where(r => r.products != null && r.products.Any(p => p.thingDef == thing))
                                     .Select(r => r.defName).OrderBy(s => s, StringComparer.Ordinal))
                            j.Value(recipe);
                        j.EndArray();
                    });
                    total++;
                }
                j.EndArray();
            }, () => $"{total} items and buildings");
        }

        private static void WriteStat(Json j, ThingDef thing, StatDef stat, string name)
        {
            try
            {
                j.Name(name);
                j.Number(thing.GetStatValueAbstract(stat));
            }
            catch
            {
                // Some modded stat parts assume a spawned thing; a missing value beats a failed dump.
                j.Null();
            }
        }

        /// <summary>
        /// Filed under a body-part category, or installed as one. A hint for the analyzer, not a
        /// gate — the analyzer still joins against the recipes that actually install things.
        /// </summary>
        private static bool IsImplantLike(ThingDef thing)
        {
            if (thing.isTechHediff) return true;
            if (thing.thingCategories == null) return false;
            foreach (ThingCategoryDef cat in thing.thingCategories)
                if (cat != null && cat.defName.IndexOf("BodyPart", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
