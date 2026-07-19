using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSYokaiTests
    {
        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSYokai))
                return;
            Check.XenoGene("BS_GreatBlueOni", "AG_ArmourMedium");
            Check.XenoGene("BS_GreatRedOni", "AG_ArmourMedium");
            Check.XenoGene("BS_GreatRedOni", "AG_HeatImmunity");
            Check.XenoGene("BS_Kitsune", "AG_HeatImmunity");
            Check.XenoGene("BS_RedOni", "AG_ArmourMedium");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("BS_BlueOni", "VRE_SlowAging");
                Check.XenoGene("BS_GreatBlueOni", "VRE_SlowAging");
                Check.XenoGene("BS_GreatRedOni", "VRE_SlowAging");
                Check.XenoGene("BS_LesserOni", "VRE_SlowAging");
                Check.XenoGene("BS_RedOni", "VRE_SlowAging");
            }
            if (ModsConfig.IsActive(Ids.VREHighmate))
                Check.XenoGene("BS_Kitsune", "VRE_Flirty");
            if (ModsConfig.IsActive(Ids.VRELycanthrope))
                Check.XenoGene("BS_Nekomata", "VRE_Nocturnal");
        }

        [Test]
        public static void DeathlikeReplaced()
        {
            if (!Check.Ready("genepool.bsdupes", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSYokai))
                return;
            Check.XenoGene("BS_Nekomata", "BS_LesserDeathless");
        }
    }
}
