using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREHighmateTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.VREHighmate, Ids.BetterGeneInheritance))
                return;
            Check.GenesGone("VRE_DominantGenome", "VRE_RecessiveGenome");
        }
    }
}