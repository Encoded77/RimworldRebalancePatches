using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BoglegsTests
    {
        [Test]
        public static void WaterStridingAdded()
        {
            if (!Check.Ready("xenotypes.boglegwater", Ids.Boglegs, Ids.AlphaGenes, Ids.Biotech))
                return;
            Check.XenoGene("DV_Bogleg", "AG_WaterStriding");
        }
    }
}
