using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelAeromancerTests
    {
        [Test]
        public static void AeromancerPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelAeromancer, "RBP_Gene_Aeromancer", "VPE_CycloneSTNL");

        [Test]
        public static void AeromancerGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelAeromancer, "RBP_Gene_Aeromancer",
                Ids.RimsenalHarana, "Harana");
    }
}
