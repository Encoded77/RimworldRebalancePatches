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

        [Test]
        public static void EmpireLeansAristocratic()
        {
            if (!Check.Ready("xenotypes.royalty", Ids.Royalty, Ids.Biotech))
                return;
            FactionDef empire = Check.Def<FactionDef>("Empire");
            float baseliner = Check.BaselinerShare(empire);
            Check.True(baseliner >= 0.35f,
                $"Empire leaves only {baseliner:P1} for baseliners, below the 35% floor");
            Check.True(Check.XenotypeChanceOf(empire, "Hussar") > 0f, "Empire lacks hussars");
            if (ModsConfig.IsActive(Ids.Odyssey))
                Check.True(!Check.HasXenotype(empire, "Starjack"), "Empire still spawns starjacks");
            if (ModsConfig.IsActive(Ids.RimsenalHarana))
                Check.True(!Check.HasXenotype(empire, "Harana"), "Empire still spawns Harana");
            if (ModsConfig.IsActive(Ids.Highborn))
                Check.Eq(Check.XenotypeChanceOf(empire, "HBX_Highborn"), 0.09f, "Empire HBX_Highborn chance");
        }
    }
}
