using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RoyaltyTests
    {
        [Test]
        public static void HealingEnhancerUsesInjuryHealingFactor()
        {
            if (!Check.Ready("vanilla.healingenhancer", Ids.Royalty))
                return;
            HediffDef enhancer = Check.Def<HediffDef>("HealingEnhancer");
            bool found = false;
            if (enhancer.stages != null)
                foreach (HediffStage stage in enhancer.stages)
                    if (Check.StatModifierValue(stage.statFactors, "InjuryHealingFactor") == 1.5f)
                        found = true;
            Check.True(found, "HealingEnhancer has no stage with InjuryHealingFactor x1.5");
        }
    }
}
