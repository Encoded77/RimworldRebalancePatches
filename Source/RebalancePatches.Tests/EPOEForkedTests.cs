using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class EPOEForkedTests
    {
        [Test]
        public static void AdvancedOrgansFillTheTierGap()
        {
            if (!Check.Ready("cybernetics.advancedorgans", Ids.EPOEForked))
                return;

            var expected = new Dictionary<string, string>
            {
                { "RBP_AdvancedSyntheticHeart", "Heart" },
                { "RBP_AdvancedSyntheticLung", "Lung" },
                { "RBP_AdvancedSyntheticLiver", "Liver" },
                { "RBP_AdvancedSyntheticKidney", "Kidney" },
                { "RBP_AdvancedSyntheticStomach", "Stomach" },
                { "RBP_AdvancedSyntheticNose", "Nose" },
            };

            foreach (KeyValuePair<string, string> pair in expected)
            {
                HediffDef hediff = Check.Def<HediffDef>(pair.Key);
                Check.True(hediff.addedPartProps != null, $"{pair.Key} has addedPartProps");
                Check.Eq(hediff.addedPartProps.partEfficiency, 1.35f, $"{pair.Key} partEfficiency");
                Check.True(hediff.addedPartProps.betterThanNatural,
                    $"{pair.Key} counts as better than natural");

                // Quality Bionics only sees parts that hand an item back on removal.
                Check.Eq(hediff.spawnThingOnRemoved?.defName, pair.Key,
                    $"{pair.Key} spawns its item on removal");

                ThingDef item = Check.Def<ThingDef>(pair.Key);
                Check.True(item.isTechHediff, $"{pair.Key} item is a tech hediff");
                Check.Eq(item.techLevel, TechLevel.Spacer, $"{pair.Key} item techLevel");

                string recipeName = "RBP_Install" + pair.Key.Substring("RBP_".Length);
                RecipeDef recipe = Check.Def<RecipeDef>(recipeName);
                Check.Eq(recipe.addsHediff?.defName, pair.Key, $"{recipeName} adds the right hediff");
                string actualParts = recipe.appliedOnFixedBodyParts == null
                    ? "<null>"
                    : recipe.appliedOnFixedBodyParts.Count == 0
                        ? "<empty>"
                        : string.Join(", ", System.Linq.Enumerable.Select(
                            recipe.appliedOnFixedBodyParts, p => p.defName));
                Check.Note($"{recipeName} targets: {actualParts}");
                Check.Soft(recipe.appliedOnFixedBodyParts != null
                        && System.Linq.Enumerable.Any(recipe.appliedOnFixedBodyParts,
                            p => p.defName == pair.Value),
                    $"{recipeName} must target {pair.Value}; actually targets: {actualParts}");

                if (recipe.appliedOnFixedBodyParts != null)
                    foreach (string otherOrgan in expected.Values)
                    {
                        if (otherOrgan == pair.Value) continue;
                        Check.Soft(!System.Linq.Enumerable.Any(recipe.appliedOnFixedBodyParts,
                                p => p.defName == otherOrgan),
                            $"{recipeName} must not also target {otherOrgan}; actually targets: {actualParts}");
                    }

                Check.Soft(recipe.recipeUsers == null
                        || recipe.recipeUsers.Count == 0
                        || recipe.recipeUsers.Exists(u => u.defName == "Human"),
                    $"{recipeName} is offered on humans");
            }

            Check.SoftResult();
        }

        [Test]
        public static void AdvancedOrgansAndTheNoseCraftFromTheRightNode()
        {
            if (!Check.Ready("cybernetics.advancedorgans", Ids.EPOEForked))
                return;

            var expectedNode = new Dictionary<string, string>
            {
                { "RBP_AdvancedSyntheticHeart", "RBP_CybAdvancedOrgans" },
                { "RBP_AdvancedSyntheticLung", "RBP_CybAdvancedOrgans" },
                { "RBP_AdvancedSyntheticLiver", "RBP_CybAdvancedOrgans" },
                { "RBP_AdvancedSyntheticKidney", "RBP_CybAdvancedOrgans" },
                { "RBP_AdvancedSyntheticStomach", "RBP_CybAdvancedOrgans" },
                { "RBP_AdvancedSyntheticNose", "RBP_CybAdvancedSenses" },
            };

            foreach (KeyValuePair<string, string> pair in expectedNode)
            {
                ThingDef item = Check.Def<ThingDef>(pair.Key);
                string actual = item.recipeMaker?.researchPrerequisite?.defName ?? "<none>";
                Check.Note($"{pair.Key} crafts from {actual}");
                Check.Soft(actual == pair.Value,
                    $"{pair.Key} should be craftable from {pair.Value}, not {actual}");
            }

            Check.SoftResult();
        }
    }
}
