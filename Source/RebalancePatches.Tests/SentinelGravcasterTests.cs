using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelGravcasterTests
    {
        [Test]
        public static void GravcasterPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelGravcaster, "RBP_Gene_Gravcaster", "VPE_GravmancerSTNL");

        [Test]
        public static void GravcasterGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelGravcaster, "RBP_Gene_Gravcaster",
                Ids.AlphaGenes, "AG_Fleetkind",
                Ids.Avaloi, "DV_Avaloi",
                Ids.Odyssey, "Starjack");
    }
}
