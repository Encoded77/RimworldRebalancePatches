using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSYokaiTests
    {
        [Test]
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.yokai", Ids.BSYokai))
                return;
            Check.Eq(Check.Def<XenotypeDef>("BS_Kitsune").label, "vulpid", "BS_Kitsune label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Nekomata").label, "felid", "BS_Nekomata label");
            Check.Eq(Check.Def<XenotypeDef>("BS_RedOni").label, "crimson hornbrute", "BS_RedOni label");
            Check.Eq(Check.Def<XenotypeDef>("BS_BlueOni").label, "cobalt hornbrute", "BS_BlueOni label");
            Check.Eq(Check.Def<XenotypeDef>("BS_GreatRedOni").label, "great crimson hornbrute", "BS_GreatRedOni label");
            Check.Eq(Check.Def<XenotypeDef>("BS_GreatBlueOni").label, "great cobalt hornbrute", "BS_GreatBlueOni label");
            Check.Eq(Check.Def<XenotypeDef>("BS_LesserOni").label, "lesser hornbrute", "BS_LesserOni label");
            FactionDef union = Check.Def<FactionDef>("BS_YokaiUnion");
            Check.Eq(union.label, "Chimeric Union", "BS_YokaiUnion label");
            Check.Eq(union.pawnSingular, "chimeric", "BS_YokaiUnion pawnSingular");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSYokai))
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
            if (!Check.Ready("genetics.bsdupes", Ids.BigSmallCore, Ids.CherryPicker, Ids.BSYokai))
                return;
            Check.XenoGene("BS_Nekomata", "BS_LesserDeathless");
        }
    }
}

