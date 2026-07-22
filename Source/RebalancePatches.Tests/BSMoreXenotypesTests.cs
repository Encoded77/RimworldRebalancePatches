using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSMoreXenotypesTests
    {
        [Test]
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.morexenos", Ids.BSMoreXenos))
                return;
            Check.Eq(Check.Def<XenotypeDef>("BS_Devilspider").label, "dreadspider", "BS_Devilspider label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Mimic").label, "mimic", "BS_Mimic label unchanged");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSMoreXenos))
                return;
            Check.XenoGene("BS_Devilspider", "AG_HeatImmunity");
            Check.XenoGene("BS_HiveQueen", "Body_FemaleOnly");
            Check.XenoGene("BS_HiveQueen", "AG_ArmourMedium");
            Check.XenoGene("BS_Parasite", "AG_ArmourMedium");
            Check.XenoGene("BS_Weaver", "AG_ArmourMedium");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("BS_Broodmother", "VRE_FastAging");
                Check.XenoGene("BS_HiveQueen", "VRE_FastAging");
            }
            if (ModsConfig.IsActive(Ids.VREArchon))
            {
                Check.XenoGene("BS_Abomination", "VRE_ShortPregnancy");
                Check.XenoGene("BS_Broodmother", "VRE_ShortPregnancy");
                Check.XenoGene("BS_HiveQueen", "VRE_ShortPregnancy");
            }
            if (ModsConfig.IsActive(Ids.VREInsector))
                Check.XenoGene("BS_HiveQueen", "VRE_VatGrownInsectoidSkin");
            if (ModsConfig.IsActive(Ids.VRELycanthrope))
            {
                Check.XenoGene("BS_Parasite", "VRE_Nocturnal");
                Check.XenoGene("BS_Weaver", "VRE_Nocturnal");
            }
        }
    }
}

