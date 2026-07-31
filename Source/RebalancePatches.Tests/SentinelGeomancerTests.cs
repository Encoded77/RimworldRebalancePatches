using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelGeomancerTests
    {
        [Test]
        public static void GeomancerPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelGeomancer, "RBP_Gene_Geomancer", "VPE_GeomancerSTNL");

        [Test]
        public static void GeomancerGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelGeomancer, "RBP_Gene_Geomancer",
                Ids.Biotech, "Dirtmole",
                Ids.BigSmallCore, "VU_Gatekeeper",
                Ids.BSLamias, "LoS_Gorgon",
                Ids.BSRaces, "BS_Dwarf",
                Ids.BSRaces, "BS_Jotun",
                Ids.Stoneborn, "Stoneborn");
    }
}
