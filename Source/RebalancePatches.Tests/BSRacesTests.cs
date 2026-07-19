using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSRacesTests
    {
        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSRaces))
                return;
            Check.XenoGene("BS_BrokenTitan", "AG_ColdImmunity");
            Check.XenoGene("BS_FireJotun", "AG_HeatImmunity");
            Check.XenoGene("BS_FleshGolemServant", "AG_ArmourMedium");
            Check.XenoGene("BS_FrostJotun", "AG_ColdImmunity");
            Check.XenoGene("BS_Hearthdoll", "AG_ArmourMedium");
            Check.XenoGene("BS_Jotun", "AG_ArmourMedium");
            Check.XenoGene("BS_PilotableFleshGolem", "AG_ArmourMedium");
            Check.XenoGene("BS_Redcap", "AG_ArmourMedium");
            Check.XenoGene("BS_Surtr", "AG_ArmourMedium");
            Check.XenoGene("BS_Surtr", "AG_HeatImmunity");
            Check.XenoGene("BS_Svartalf", "AG_ArmourMedium");
            Check.XenoGene("BS_Troll", "AG_ArmourMedium");
            Check.XenoGene("BS_TrollAdult", "AG_ArmourMedium");
            Check.XenoGene("BS_Ymir", "AG_ArmourMedium");
            Check.XenoGene("BS_Ymir", "AG_ColdImmunity");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("BS_Dwarf", "VRE_SlowAging");
                Check.XenoGene("BS_FireJotun", "VRE_SlowAging");
                Check.XenoGene("BS_FrostJotun", "VRE_SlowAging");
                Check.XenoGene("BS_Gnome", "VRE_SlowAging");
                Check.XenoGene("BS_GreatOgre", "VRE_SlowAging");
                Check.XenoGene("BS_Half_Jotun", "VRE_SlowAging");
                Check.XenoGene("BS_Redcap", "VRE_FastAging");
                Check.XenoGene("BS_Surtr", "VRE_SlowAging");
                Check.XenoGene("BS_Svartalf", "VRE_SlowAging");
                Check.XenoGene("BS_Ymir", "VRE_SlowAging");
            }
            if (ModsConfig.IsActive(Ids.VREArchon))
            {
                Check.XenoGene("BS_Corrupterd_Titan", "VRE_ShortPregnancy");
                Check.XenoGene("BS_Gnome", "VRE_ShortPregnancy");
                Check.XenoGene("BS_Redcap", "VRE_ShortPregnancy");
            }
            if (ModsConfig.IsActive(Ids.VREWaster))
            {
                Check.XenoGene("BS_BrokenTitan", "VRE_Instability_Extreme");
                Check.XenoGene("BS_Corrupterd_Titan", "VRE_Instability_Extreme");
            }
            if (ModsConfig.IsActive(Ids.VREStarjack))
            {
                Check.XenoGene("BS_BrokenTitan", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Surtr", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Ymir", "VREStarjack_VacuumResistance_Total");
            }
        }

        [Test]
        public static void DeathlikeReplaced()
        {
            if (!Check.Ready("genepool.bsdupes", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSRaces))
                return;
            Check.XenoGene("BS_FleshGolemServant", "BS_LesserDeathless");
            Check.XenoGene("BS_PilotableFleshGolem", "BS_LesserDeathless");
        }
    }
}
