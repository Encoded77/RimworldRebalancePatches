using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VRESauridTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.CherryPicker, Ids.VRESaurid, Ids.Buzzers))
                return;
            Check.GenesGone("VRESaurids_Pheromones");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.CherryPicker, Ids.VRESaurid, Ids.Buzzers))
                return;
            Check.XenoGene("VRESaurids_Saurid", "DV_Pheromones");
        }
    }
}