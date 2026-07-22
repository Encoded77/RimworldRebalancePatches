using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AvaloiTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.CherryPicker, Ids.Avaloi))
                return;
            Check.GenesGone("DV_ToxResist_Terrible");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.CherryPicker, Ids.Avaloi))
                return;
            Check.XenoGene("DV_Avaloi", "AG_ToxResist_Vulnerability");
        }
    }
}