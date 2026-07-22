using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BrawnumTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.CherryPicker, Ids.Brawnum))
                return;
            Check.GenesGone("DV_StrongBack");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.CherryPicker, Ids.Brawnum))
                return;
            Check.XenoGene("DV_Brawnum", "AG_PackMule");
        }
    }
}