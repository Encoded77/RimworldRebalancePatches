using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREPigskinTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.VREPigskin))
                return;
            Check.GenesGone("VRE_EverGrowing");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.VREPigskin))
                return;
            Check.XenoGene("VRE_Boarskin", "BS_EndlessGrowth");
        }
    }
}