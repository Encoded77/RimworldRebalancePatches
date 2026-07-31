using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelBugmancerTests
    {
        [Test]
        public static void BugmancerPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelBugmancer, "RBP_Gene_Bugmancer", "VPE_BugmancerSTNL");

        [Test]
        public static void BugmancerGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelBugmancer, "RBP_Gene_Bugmancer",
                Ids.AlphaGenes, "AG_Hiveling",
                Ids.BSMoreXenos, "BS_Broodmother",
                Ids.BSMoreXenos, "BS_Devilspider",
                Ids.BSMoreXenos, "BS_HiveQueen",
                Ids.BSMoreXenos, "BS_Weaver",
                Ids.Buzzers, "DV_Buzzer",
                Ids.VREInsector, "VRE_Insector");
    }
}
