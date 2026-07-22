using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREFungoidTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.CherryPicker, Ids.VREFungoid))
                return;
            Check.GenesGone("VRE_Telepathy", "VRE_TotalAntirotLungs");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.CherryPicker, Ids.VREFungoid))
                return;
            Check.XenoGene("VRE_Fungoid", "WVC_NaturalTelepathy");
            Check.XenoGene("VRE_Fungoid", "AG_LungRotImmunity");
        }
    }
}