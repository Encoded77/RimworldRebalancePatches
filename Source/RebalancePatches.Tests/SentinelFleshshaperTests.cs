using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SentinelFleshshaperTests
    {
        [Test]
        public static void FleshshaperPathIsGeneGated() =>
            PathGenes.PathGated(Ids.SentinelFleshshaper, "RBP_Gene_Fleshshaper", "VPE_FleshmancerSTNL");

        [Test]
        public static void FleshshaperGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.SentinelFleshshaper, "RBP_Gene_Fleshshaper",
                Ids.AlphaGenes, "AG_Wretch",
                Ids.AlphaGenes, "AG_Taukai",
                Ids.BSHeaven, "BS_Glutton",
                Ids.BSLamias, "LoS_ScenarioTiamat",
                Ids.BSMoreXenos, "BS_Abomination",
                Ids.BSMoreXenos, "BS_Broodmother",
                Ids.BSRaces, "BS_Troll",
                Ids.BSRaces, "BS_TrollAdult",
                Ids.BSRaces, "BS_TrollOld",
                Ids.BigSmallSlimes, "BS_GreenSlime",
                Ids.BigSmallSlimes, "BS_PinkSlime",
                Ids.BigSmallSlimes, "BS_EmperorSlime",
                Ids.WVC, "WVC_Beholdkind",
                Ids.WVC, "WVC_Fleshkind",
                Ids.WVC, "WVC_Mergekin",
                Ids.WVC, "WVC_Overrider",
                Ids.WVC, "WVC_Shadoweater");
    }
}
