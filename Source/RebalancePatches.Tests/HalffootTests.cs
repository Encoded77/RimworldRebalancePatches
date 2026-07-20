using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class HalffootTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.Halffoot))
                return;
            Check.GenesGone("DV_Bandwidth_High", "DV_ExtrasmallBuild");
            if (ModsConfig.IsActive(Ids.VREArchon))
                Check.GenesGone("DV_Melee_Fast");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.Halffoot))
                return;
            Check.XenoGene("DV_Halffoot", "AG_BandwidthIncrease");
            if (ModsConfig.IsActive(Ids.VREArchon))
                Check.XenoGene("DV_Halffoot", "VRE_FastMeleeHitter");
        }
    }
}
