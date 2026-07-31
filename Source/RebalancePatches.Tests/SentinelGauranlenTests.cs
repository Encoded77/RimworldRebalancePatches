using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelGauranlenTests
    {
        [Test]
        public static void GauranlenPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelGauranlen, "RBP_Gene_Gauranlen", "VPE_GauranlenSTNL");

        [Test]
        public static void GauranlenGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelGauranlen, "RBP_Gene_Gauranlen",
                Ids.BSRaces, "BS_TrollOld",
                Ids.VREPhytokin, "VRE_Gauranlenkin");
    }
}
