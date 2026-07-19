using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSHeavenAndHellTests
    {
        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSHeaven))
                return;
            Check.XenoGene("BS_Authority", "AG_HeatImmunity");
            Check.XenoGene("BS_Glutton", "AG_ArmourMedium");
            Check.XenoGene("BS_Grigori", "AG_ColdImmunity");
            Check.XenoGene("BS_LilGlutton", "AG_ArmourMedium");
            Check.XenoGene("BS_Malakim", "AG_ColdImmunity");
            Check.XenoGene("BS_Nephilim", "AG_ArmourMedium");
            Check.XenoGene("BS_Satan", "AG_ArmourMedium");
            Check.XenoGene("BS_Satan", "AG_ColdImmunity");
            if (ModsConfig.IsActive(Ids.VREPigskin))
                Check.XenoGene("BS_Nephilim", "VRE_SlowAging");
            if (ModsConfig.IsActive(Ids.VREHighmate))
            {
                Check.XenoGene("BS_Grigori", "VRE_Flirty");
                Check.XenoGene("BS_Lilim", "VRE_Flirty");
                Check.XenoGene("BS_Lilim", "VRE_Libido_VeryHigh");
            }
            if (ModsConfig.IsActive(Ids.VRESanguophage))
            {
                Check.XenoGene("BS_Lilim", "VRE_Talons");
                Check.XenoGene("BS_Satan", "VRE_Talons");
            }
            if (ModsConfig.IsActive(Ids.VREHussar))
                Check.XenoGene("BS_Satan", "VREH_Arrogant");
            if (ModsConfig.IsActive(Ids.VREStarjack))
            {
                Check.XenoGene("BS_Authority", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Grigori", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Malakim", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Satan", "VREStarjack_VacuumResistance_Total");
            }
        }
    }
}
