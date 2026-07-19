using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSLamiasTests
    {
        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSLamias))
                return;
            Check.XenoGene("BS_ViperPrototypeBiomecha", "AG_ArmourMedium");
            Check.XenoGene("BS_ViperPrototypeBiomecha", "AG_ColdImmunity");
            Check.XenoGene("LoS_Anacondaman", "AG_ArmourMedium");
            Check.XenoGene("LoS_ScenarioTiamat", "BS_AcidResistanceTotal");
            Check.XenoGene("LoS_Silver", "AG_ArmourMedium");
            Check.XenoGene("Naga", "AG_ArmourMedium");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("LoS_Adderman", "VRE_FastAging");
                Check.XenoGene("LoS_Silver", "VRE_SlowAging");
                Check.XenoGene("Naga", "VRE_SlowAging");
            }
            if (ModsConfig.IsActive(Ids.VREArchon))
                Check.XenoGene("LoS_ScenarioTiamat", "VRE_ShortPregnancy");
        }
    }
}
