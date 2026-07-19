using System.Text;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AnomalyTests
    {
        [Test]
        public static void CreepJoinersAcceptHumanSurgeries()
        {
            if (!Check.Ready("vanilla.creepjoinersurgery", Ids.Anomaly))
                return;
            ThingDef human = Check.Def<ThingDef>("Human");
            ThingDef creep = Check.Def<ThingDef>("CreepJoiner");
            var missing = new StringBuilder();
            int checkedCount = 0;
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.recipeUsers == null || !recipe.recipeUsers.Contains(human))
                    continue;
                checkedCount++;
                if (!recipe.recipeUsers.Contains(creep))
                    missing.Append(recipe.defName).Append(", ");
            }
            Check.True(checkedCount > 0, "no recipe with Human in recipeUsers found at all");
            Check.True(missing.Length == 0, $"recipes targeting Human but not CreepJoiner: {missing}");
        }

        [Test]
        public static void AnomalyTraitsAgreeWithInhumanMemes()
        {
            if (!Check.Ready("memes.anomalytraits", Ids.Anomaly, Ids.Ideology))
                return;
            foreach (string memeName in new[] { "Inhuman", "Ritualist" })
            {
                MemeDef meme = Check.Optional<MemeDef>(memeName, "memes.anomalytraits");
                if (meme == null)
                    continue;
                foreach (string trait in new[] { "Occultist", "VoidFascination" })
                {
                    bool found = false;
                    if (meme.agreeableTraits != null)
                        foreach (TraitRequirement req in meme.agreeableTraits)
                            if (req.def != null && req.def.defName == trait)
                                found = true;
                    Check.True(found, $"MemeDef {memeName} agreeableTraits lacks {trait}");
                }
            }
        }
    }
}
