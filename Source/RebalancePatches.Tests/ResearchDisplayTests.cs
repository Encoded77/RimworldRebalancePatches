using System.Collections.Generic;
using RebalancePatches.Mods.ResearchDisplay;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ResearchDisplayTests
    {
        [Test]
        public static void UnlockGroupingIsRegistered()
        {
            if (!Check.Ready(YartUnlockGrouping.SettingKey, Ids.YART))
                return;
            Check.True(YartUnlockGrouping.Active,
                "YART's unlock-grouping method could not be found, so the postfix stood itself down and "
                + "every card will keep grouping items by their own research prerequisites alone");
            Check.HarmonyPatched(YartUnlockGrouping.Target, "research card unlock grouping");
        }

        [Test]
        public static void ItemGatedByASecondResearchIsFiledUnderIt()
        {
            if (!Check.Ready(YartUnlockGrouping.SettingKey, Ids.YART))
                return;

            var card = new ResearchProjectDef { defName = "RBP_TestOnly_CardProject" };
            var second = new ResearchProjectDef { defName = "RBP_TestOnly_SecondProject" };
            var item = new ThingDef { defName = "RBP_TestOnly_CraftedItem" };
            var recipe = new RecipeDef
            {
                defName = "RBP_TestOnly_MakeCraftedItem",
                researchPrerequisites = new List<ResearchProjectDef> { card, second },
            };
            var unrelated = new ThingDef { defName = "RBP_TestOnly_UnlistedItem" };

            foreach (Def d in new Def[] { card, second, item, recipe, unrelated })
                d.ResolveDefNameHash();

            var unlockers = new Dictionary<Def, List<ResearchProjectDef>>
            {
                { item, new List<ResearchProjectDef> { card, second } },
            };

            List<ResearchProjectDef> forItem =
                YartUnlockGrouping.AlsoGatedBy(item, new List<ResearchProjectDef>(), unlockers);
            Check.Soft(forItem.Contains(second),
                "the item's grouping still does not name the second research, so it would be drawn as a "
                + $"direct unlock of the card's own project; got: {Names(forItem)}");
            Check.Soft(forItem.Count == 2 && forItem.Contains(card),
                $"the item's grouping should name exactly the two gating projects; got: {Names(forItem)}");

            List<ResearchProjectDef> forRecipe =
                YartUnlockGrouping.AlsoGatedBy(recipe, recipe.researchPrerequisites, unlockers);
            Check.Soft(forRecipe.Count == 2 && forRecipe.Contains(card) && forRecipe.Contains(second),
                $"the recipe's grouping changed or gained a duplicate; got: {Names(forRecipe)}");
            Check.Soft(SameSet(forItem, forRecipe),
                "the item and the recipe that makes it would still be drawn under different headings: "
                + $"item {Names(forItem)}, recipe {Names(forRecipe)}");

            // A def no project lists comes back exactly as declared: this only ever adds.
            List<ResearchProjectDef> forUnrelated = YartUnlockGrouping.AlsoGatedBy(
                unrelated, new List<ResearchProjectDef> { second }, unlockers);
            Check.Soft(forUnrelated.Count == 1 && forUnrelated[0] == second,
                $"a def outside the map lost or gained a research; got: {Names(forUnrelated)}");

            Check.SoftResult();
        }

        [Test]
        public static void GroupingAgreesWithTheVanillaResearchWindow()
        {
            if (!Check.Ready(YartUnlockGrouping.SettingKey, Ids.YART))
                return;
            if (!Check.Soft(YartUnlockGrouping.Target != null,
                    "YART's unlock-grouping method was never found, so nothing could be invoked"))
            {
                Check.SoftResult();
                return;
            }

            List<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
            Dictionary<Def, List<ResearchProjectDef>> unlockers = ReverseUnlockMap(projects);
            Dictionary<ThingDef, List<ResearchProjectDef>> viaRecipe = ProductGateMap();

            if (!Check.Soft(unlockers.Count > 0,
                    "no research project unlocks anything, so the rule could not be exercised"))
            {
                Check.SoftResult();
                return;
            }

            int pairs = 0, regrouped = 0;
            string example = null;
            foreach (ResearchProjectDef project in projects)
                foreach (Def unlocked in project.UnlockedDefs)
                {
                    if (unlocked == null)
                        continue;
                    pairs++;

                    List<ResearchProjectDef> live = Without(Invoke(unlocked), project);
                    List<ResearchProjectDef> vanilla = Without(unlockers[unlocked], project);
                    List<ResearchProjectDef> declared = Without(DeclaredResearchOf(unlocked), project);

                    foreach (ResearchProjectDef needed in vanilla)
                        Check.Soft(live.Contains(needed),
                            $"on '{project.defName}', '{unlocked.defName}' would be grouped under "
                            + $"{Names(live)}, but the vanilla research window also requires "
                            + $"'{needed.defName}'");

                    foreach (ResearchProjectDef named in live)
                    {
                        if (declared.Contains(named) || vanilla.Contains(named))
                            continue;
                        Check.Soft(
                            unlocked is ThingDef thing && viaRecipe.TryGetValue(thing, out var gates)
                                && gates.Contains(named),
                            $"on '{project.defName}', grouping '{unlocked.defName}' names research "
                            + $"'{named.defName}', which neither the def nor any recipe producing it "
                            + "requires");
                    }

                    if (declared.Count == 0 && live.Count > 0)
                    {
                        regrouped++;
                        if (example == null)
                            example = $"{project.defName} -> {unlocked.defName} (also needs {Names(live)})";
                    }
                }

            Check.Note($"{pairs} project/unlock pair(s) checked, {regrouped} regrouped"
                + (example == null ? "" : $", e.g. {example}"));
            Check.SoftResult();
        }

        /// <summary>Runs the real, patched method, so an unapplied patch shows up as a failure.</summary>
        private static List<ResearchProjectDef> Invoke(Def def)
        {
            var result = new List<ResearchProjectDef>();
            if (YartUnlockGrouping.Target.Invoke(null, new object[] { def })
                is IEnumerable<ResearchProjectDef> projects)
                foreach (ResearchProjectDef project in projects)
                    result.Add(project);
            return result;
        }

        private static Dictionary<Def, List<ResearchProjectDef>> ReverseUnlockMap(
            List<ResearchProjectDef> projects)
        {
            var map = new Dictionary<Def, List<ResearchProjectDef>>();
            foreach (ResearchProjectDef project in projects)
                foreach (Def unlocked in project.UnlockedDefs)
                {
                    if (unlocked == null)
                        continue;
                    if (!map.TryGetValue(unlocked, out List<ResearchProjectDef> listing))
                        map[unlocked] = listing = new List<ResearchProjectDef>();
                    if (!listing.Contains(project))
                        listing.Add(project);
                }
            return map;
        }

        private static Dictionary<ThingDef, List<ResearchProjectDef>> ProductGateMap()
        {
            var map = new Dictionary<ThingDef, List<ResearchProjectDef>>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.products == null)
                    continue;
                List<ResearchProjectDef> gates = DeclaredResearchOf(recipe);
                if (gates.Count == 0)
                    continue;
                foreach (ThingDefCountClass product in recipe.products)
                {
                    if (product?.thingDef == null)
                        continue;
                    if (!map.TryGetValue(product.thingDef, out List<ResearchProjectDef> listing))
                        map[product.thingDef] = listing = new List<ResearchProjectDef>();
                    Add(listing, gates);
                }
            }
            return map;
        }

        /// <summary>YART's own ResearchPrereqsOf, recomputed independently of the code under test.</summary>
        private static List<ResearchProjectDef> DeclaredResearchOf(Def def)
        {
            var found = new List<ResearchProjectDef>();
            switch (def)
            {
                case ThingDef thing:
                    Add(found, thing.researchPrerequisites);
                    if (thing.plant != null)
                        Add(found, thing.plant.sowResearchPrerequisites);
                    break;
                case TerrainDef terrain:
                    Add(found, terrain.researchPrerequisites);
                    break;
                case RecipeDef recipe:
                    if (recipe.researchPrerequisite != null)
                        found.Add(recipe.researchPrerequisite);
                    Add(found, recipe.researchPrerequisites);
                    break;
            }
            return found;
        }

        private static void Add(List<ResearchProjectDef> into, List<ResearchProjectDef> from)
        {
            if (from == null)
                return;
            foreach (ResearchProjectDef project in from)
                if (project != null && !into.Contains(project))
                    into.Add(project);
        }

        private static List<ResearchProjectDef> Without(
            List<ResearchProjectDef> projects, ResearchProjectDef self)
        {
            var kept = new List<ResearchProjectDef>();
            foreach (ResearchProjectDef project in projects)
                if (project != null && project != self && !kept.Contains(project))
                    kept.Add(project);
            return kept;
        }

        private static bool SameSet(List<ResearchProjectDef> a, List<ResearchProjectDef> b)
        {
            if (a.Count != b.Count)
                return false;
            foreach (ResearchProjectDef project in a)
                if (!b.Contains(project))
                    return false;
            return true;
        }

        private static string Names(List<ResearchProjectDef> projects)
        {
            if (projects == null || projects.Count == 0)
                return "(nothing further)";
            var names = new List<string>(projects.Count);
            foreach (ResearchProjectDef project in projects)
                names.Add(project == null ? "null" : project.defName);
            return string.Join(", ", names.ToArray());
        }
    }
}
