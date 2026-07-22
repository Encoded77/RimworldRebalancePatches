using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREPhytokinTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.CherryPicker, Ids.VREPhytokin))
                return;
            Check.GenesGone("VRE_Female");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.CherryPicker, Ids.VREPhytokin))
                return;
            Check.XenoGene("VRE_Animakin", "Body_FemaleOnly");
            Check.XenoGene("VRE_Gauranlenkin", "Body_FemaleOnly");
            Check.XenoGene("VRE_Poluxkin", "Body_FemaleOnly");
        }
    }
}