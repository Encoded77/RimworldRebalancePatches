using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelAnimaTests
    {
        [Test]
        public static void AnimancerPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelAnima, "RBP_Gene_Animancer", "VPE_AnimaSTNL");

        [Test]
        public static void AnimancerGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelAnima, "RBP_Gene_Animancer",
                Ids.VREPhytokin, "VRE_Animakin");
    }
}
