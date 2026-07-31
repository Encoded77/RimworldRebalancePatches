using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelHydromancerTests
    {
        [Test]
        public static void HydromancerPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelHydromancer, "RBP_Gene_Hydromancer", "VPE_HydromancerSTNL");

        [Test]
        public static void HydromancerGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelHydromancer, "RBP_Gene_Hydromancer",
                Ids.AlphaGenes, "AG_Nereid",
                Ids.BSLamias, "LoS_ScenarioTiamat",
                Ids.BSLamias, "LoS_Siren",
                Ids.Boglegs, "DV_Bogleg");
    }
}
