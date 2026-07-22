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
            if (!Check.Ready("genetics.dedup", Ids.BigSmallCore, Ids.CherryPicker, Ids.VREPigskin))
                return;
            Check.GenesGone("VRE_EverGrowing");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.BigSmallCore, Ids.CherryPicker, Ids.VREPigskin))
                return;
            Check.XenoGene("VRE_Boarskin", "BS_EndlessGrowth");
        }
    }
}