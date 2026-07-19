using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class StonebornTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.Stoneborn))
                return;
            Check.GenesGone("DV_SmallBuild", "DV_Undergrounder");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.Stoneborn))
                return;
            Check.XenoGene("Stoneborn", "Undergrounder");
        }
    }
}