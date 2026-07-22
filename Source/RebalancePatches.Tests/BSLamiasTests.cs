using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSLamiasTests
    {
        [Test]
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.lamias", Ids.BSLamias))
                return;
            Check.Eq(Check.Def<XenotypeDef>("LoS_Lamia").label, "serpid", "LoS_Lamia label");
            Check.Eq(Check.Def<XenotypeDef>("LoS_Siren").label, "mesmer serpid", "LoS_Siren label");
            Check.Eq(Check.Def<XenotypeDef>("LoS_Gorgon").label, "petrifex serpid", "LoS_Gorgon label");
            Check.Eq(Check.Def<XenotypeDef>("Naga").label, "greater serpid", "Naga label");
            Check.Eq(Check.Def<XenotypeDef>("Nagaraj").label, "serpid prime", "Nagaraj label");
            Check.Eq(Check.Def<XenotypeDef>("LoS_ScenarioTiamat").label, "progenitor serpid", "LoS_ScenarioTiamat label");
            Check.Eq(Check.Def<XenotypeDef>("LoS_Adderman").label, "Adderman", "LoS_Adderman label unchanged");
            Check.Eq(Check.Def<FactionDef>("LoS_SnekFaction").label, "serpid tribal federation", "LoS_SnekFaction label");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSLamias))
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

