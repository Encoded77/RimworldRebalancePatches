using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimsenalHaranaTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.RimsenalHarana, Ids.Stoneborn))
                return;
            Check.GenesGone("Aggression_PassivelyAggressive");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.RimsenalHarana, Ids.Stoneborn))
                return;
            Check.XenoGene("Harana", "DV_Aggression_Irascible");
        }
    }
}