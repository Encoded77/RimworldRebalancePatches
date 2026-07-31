using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelDeadlifeTests
    {
        [Test]
        public static void DeadlifePathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelDeadlife, "RBP_Gene_Deadlife", "VPE_DeadlifeSTNL");

        [Test]
        public static void DeadlifeGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelDeadlife, "RBP_Gene_Deadlife",
                Ids.AlphaGenes, "AG_Helixien",
                Ids.BigSmallCore, "VU_Returned",
                Ids.BigSmallCore, "VU_Returned_Intact",
                Ids.BigSmallCore, "VU_ReturnedSkeletal",
                Ids.BSYokai, "BS_Nekomata",
                Ids.WVC, "WVC_Sandycat");
    }
}
