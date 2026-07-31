using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelBiohazardTests
    {
        [Test]
        public static void BiohazardPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelBiohazard, "RBP_Gene_Biohazard", "VPE_BiohazardSTNL");

        [Test]
        public static void BiosootherPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelBiohazard, "RBP_Gene_Biosoother", "VPE_BiosootherSTNL");

        [Test]
        public static void BiohazardGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelBiohazard, "RBP_Gene_Biohazard",
                Ids.Biotech, "Waster",
                Ids.AlphaGenes, "AG_Mycormorph",
                Ids.BSLamias, "LoS_Adderman",
                Ids.BSLamias, "Naga",
                Ids.BigSmallSlimes, "BS_ToxicSlduge",
                Ids.Boglegs, "DV_Bogleg",
                Ids.Buzzers, "DV_Buzzer",
                Ids.WVC, "WVC_Leper",
                Ids.WVC, "WVC_Rustkind");

        [Test]
        public static void BiosootherGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelBiohazard, "RBP_Gene_Biosoother",
                Ids.BigSmallSlimes, "BS_PinkSlime",
                Ids.BigSmallSlimes, "BS_ElixirSlime",
                Ids.VREPhytokin, "VRE_Poluxkin");
    }
}
