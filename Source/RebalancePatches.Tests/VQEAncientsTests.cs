using HarmonyLib;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VQEAncientsTests
    {
        [Test]
        public static void AncientHospitalSeatingSittable()
        {
            if (!Check.Ready("vqea.sittable", Ids.VQEAncients))
                return;
            foreach (string thing in new[] { "VQEA_AncientHospitalArmchair", "VQEA_AncientHospitalBench" })
                Check.True(Check.Def<ThingDef>(thing).building?.isSittable == true, $"{thing} is not sittable");
        }

        [Test]
        public static void GiantGenesForceGiantTrait()
        {
            if (!Check.Ready("vqea.giantweapons", Ids.VQEAncients, Ids.BigSmallCore))
                return;
            Check.Def<TraitDef>("BS_Giant");
            foreach (string gene in new[] { "VQEA_Herculean", "VQEA_Enormous" })
                Check.True(Check.HasForcedTrait(Check.Def<GeneDef>(gene), "BS_Giant"), $"{gene} does not force BS_Giant");
        }

        [Test]
        public static void BuildableArchogenLaboratory()
        {
            if (!Check.Ready("geneticsresearch.vqea", Ids.VQEAncients, Ids.Biotech) || !Check.GeneticsTabLoaded("geneticsresearch.vqea"))
                return;
            ResearchProjectDef engineering = Check.Def<ResearchProjectDef>("RBP_ArchogenEngineering");
            Check.Eq(engineering.baseCost, 10000f, "RBP_ArchogenEngineering.baseCost");
            Check.PrereqsAre(engineering.prerequisites, "RBP_ArchogenEngineering.prerequisites", "Archogenetics");
            Check.Eq(engineering.tab, Check.Def<ResearchTabDef>("RBP_GeneticsTab"), "RBP_ArchogenEngineering.tab");
            ThingDef injector = Check.Def<ThingDef>("VQEA_ArchogenInjector");
            Check.True(Check.ContainsResearch(injector.researchPrerequisites, "RBP_ArchogenEngineering"),
                "VQEA_ArchogenInjector does not unlock from RBP_ArchogenEngineering");
            Check.Eq(injector.designationCategory?.defName, "Biotech", "VQEA_ArchogenInjector.designationCategory");
            Check.Eq(Check.CostOf(injector, "ArchiteCapsule"), 1, "VQEA_ArchogenInjector costList[ArchiteCapsule]");
            Check.Eq(Check.StatBase(injector, "WorkToBuild"), 12000f, "VQEA_ArchogenInjector WorkToBuild");
            foreach (string facility in new[] { "VQEA_NeurostabilizerArray", "VQEA_CognitiveRecoveryArray", "VQEA_MutagenInhibitorCore" })
            {
                ThingDef def = Check.Def<ThingDef>(facility);
                Check.True(Check.ContainsResearch(def.researchPrerequisites, "RBP_ArchogenEngineering"),
                    $"{facility} does not unlock from RBP_ArchogenEngineering");
                Check.Eq(def.designationCategory?.defName, "Biotech", $"{facility}.designationCategory");
                Check.True(Check.CostOf(def, "ComponentSpacer") >= 1, $"{facility} costs no advanced components");
            }
        }

        [Test]
        public static void PatientGownBluntArmorNerf()
        {
            if (!Check.Ready("vqea.patientgown", Ids.VQEAncients))
                return;
            Check.Eq(Check.StatBase(Check.Def<ThingDef>("VQEA_Apparel_PatientGown"), "ArmorRating_Blunt"), 0.1f,
                "VQEA_Apparel_PatientGown ArmorRating_Blunt");
        }

        [Test]
        public static void ArchogenInjectorWhitelist()
        {
            if (!Check.Ready("vqea.injectorwhitelist", Ids.VQEAncients))
                return;
            ArchogenWhitelistDef whitelist = DefDatabase<ArchogenWhitelistDef>.GetNamedSilentFail("RBP_ArchogenInjectionWhitelist");
            Check.True(whitelist != null, "RBP_ArchogenInjectionWhitelist def missing");
            Check.True(whitelist.whitelistedGenes != null && whitelist.whitelistedGenes.Contains("VQEA_Herculean"),
                "whitelist lacks VQEA_Herculean");
            Check.True(whitelist.whitelistedGenes.Contains("Learning_Slow"), "whitelist lacks vanilla drawback genes");
            System.Type utils = AccessTools.TypeByName("VanillaQuestsExpandedAncients.Utils");
            Check.True(utils != null, "VanillaQuestsExpandedAncients.Utils type not found");
            Check.HarmonyPatched(AccessTools.Method(utils, "IsValidGeneForInjection"), "vqea.injectorwhitelist");
        }
    }
}
