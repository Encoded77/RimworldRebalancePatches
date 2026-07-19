using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class WVCTests
    {
        [Test]
        public static void WvcXenotypesLeaveGenericFactions()
        {
            if (!Check.Ready("xenotypes.wvcchances", Ids.WVC))
                return;
            void NotIn(string factionName, string xenotype)
            {
                FactionDef faction = Check.Optional<FactionDef>(factionName, "xenotypes.wvcchances");
                if (faction != null)
                    Check.True(!Check.HasXenotype(faction, xenotype), $"{factionName} still spawns {xenotype}");
            }
            NotIn("PirateYttakin", "WVC_Featherdust");
            NotIn("Beggars", "WVC_CatDeity");
            NotIn("Beggars", "WVC_Blank");
            if (ModsConfig.IsActive(Ids.Anomaly))
            {
                foreach (string faction in new[] { "OutlanderCivil", "TribeRough", "TribeCivil", "PirateWaster", "Empire", "Beggars" })
                    NotIn(faction, "WVC_Undead");
                NotIn("TribeSavageImpid", "WVC_Sandycat");
                NotIn("PirateYttakin", "WVC_Sandycat");
                FactionDef horax = Check.Def<FactionDef>("HoraxCult");
                Check.True(Check.HasXenotype(horax, "WVC_Undead"), "HoraxCult lacks WVC_Undead");
                Check.True(Check.HasXenotype(horax, "WVC_Sandycat"), "HoraxCult lacks WVC_Sandycat");
            }
        }

        [Test]
        public static void InternalDupeGenesRemoved()
        {
            if (!Check.Ready("genepool.wvcdupes", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.GenesGone(
                "WVC_PsychicAbility_Dull", "WVC_PsychicAbility_Deaf", "WVC_Pain_Extra",
                "WVC_NaturalImmunity_PerfectImmunity", "WVC_NaturalDisease_DiseaseFree", "WVC_NaturalAgeless",
                "WVC_Neversleep", "WVC_MechaAI_FirmwareCreatorMachine", "WVC_MechaAI_FirmwareWarMachine",
                "WVC_MechaAI_FirmwareGreenMachine", "WVC_MechaAI_FirmwareRogueMachine",
                "WVC_MechaAI_FirmwarePropagandaMachine", "WVC_MechaAI_FirmwareMechanitorMachine",
                "WVC_PatternAptitude_Shapeshifter", "WVC_MinMaxTemp_Natural", "WVC_MinMaxTemp_Scarifier",
                "WVC_Unbreakable", "WVC_GrayGooSkin", "WVC_ToxResist_Total", "WVC_NaturalMechlink",
                "WVC_NaturalPsylink", "WVC_ImplanterFangs", "WVC_ArchiteTelepathy", "WVC_Delicate", "WVC_Undead",
                "WVC_ArchiteDisableBeauty", "WVC_ArchiteDisableComfort", "WVC_ArchiteDisableJoy",
                "WVC_ArchiteDisableOutdoors", "WVC_PerfectImmunity_DiseaseFree",
                "WVC_ArchitePerfectImmunity_DiseaseFree", "WVC_PsychicAbility_Archite", "WVC_MechaAI_Base",
                "WVC_MechaAI_SoftwareDisableTalking", "WVC_MechaHidden_ArchiteForge", "WVC_MecaBodyParts_Lung",
                "WVC_MecaBodyParts_Spine", "WVC_Mecha_EarsCat", "WVC_FoxEarsA", "WVC_Dustogenic_Metabolism",
                "WVC_Learning_Scarifier", "WVC_Invulnerable", "WVC_NaturalReimplanter",
                "WVC_Reimplanter_RiseFromTheDead", "WVC_ReimplanterNatural_Endogenes",
                "WVC_ReimplanterNatural_Xenogenes", "WVC_ReimplanterArchite_Endogenes",
                "WVC_ReimplanterArchite_Xenogenes", "WVC_ReimplanterArchite_RiseEndogenes",
                "WVC_ReimplanterArchite_RiseXenogenes");
            Check.True(DefDatabase<GeneDef>.GetNamedSilentFail("WVC_ImplanterFang") != null,
                "WVC_ImplanterFang (kept sibling) missing");
        }

        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.GenesGone(
                "WVC_MaxTemp_ArchiteIncrease", "WVC_MinTemp_ArchiteDecrease", "WVC_Pain_Nullified",
                "WVC_BodySize_Small", "WVC_BodySize_Average", "WVC_BodySize_Large",
                "WVC_FemaleOnly", "WVC_MaleOnly", "WVC_Monogender_Disabled",
                "WVC_BleedStopper", "WVC_DeadStomach", "WVC_RepairSkin",
                "WVC_WoundHealing_UnrealFast", "WVC_WoundHealing_SuperSlowHealing",
                "WVC_ArmoredSkin_Stone", "WVC_ArmoredSkin_Steel", "WVC_ArmoredSkin_Plasteel",
                "WVC_ArmoredSkin_Fortress", "WVC_ClothedArmor", "WVC_Invisibility",
                "WVC_MechBandwidth_Enchanced", "WVC_MechBandwidth_Extreme");
            if (ModsConfig.IsActive(Ids.VREPigskin))
                Check.GenesGone("WVC_NaturalFastAging", "WVC_NaturalSlowAging", "WVC_NaturalFastGrowing",
                    "WVC_ForeverYoung", "WVC_AgeDebuff_Libido", "WVC_AgeDebuff_MoveSpeed", "WVC_AgeDebuff_Timeless");
            if (ModsConfig.IsActive(Ids.VREFungoid))
                Check.GenesGone("WVC_NoLearning", "WVC_Learning_SlowNoSkillDecay", "WVC_DisabledAllWork_Blank");
            if (ModsConfig.IsActive(Ids.VREGenie))
                Check.GenesGone("WVC_Immunity_Non");
            if (ModsConfig.IsActive(Ids.VRESanguophage))
                Check.GenesGone("WVC_UVSensitivity_Deadly");
            if (ModsConfig.IsActive(Ids.VREHussar))
                Check.GenesGone("WVC_Tough");
            if (ModsConfig.IsActive(Ids.RimsenalAskbarn))
                Check.GenesGone("WVC_FurskinInstincts_KeenEye");
        }

        [Test]
        public static void XenotypesRewiredInternalDupes()
        {
            if (!Check.Ready("genepool.wvcdupes", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.XenoGene("WVC_Ashen", "PerfectImmunity");
            Check.XenoGene("WVC_Ashen", "DiseaseFree");
            Check.XenoGene("WVC_Ashen", "AG_HeatImmunity");
            Check.XenoGene("WVC_Ashen", "AG_ColdImmunity");
            Check.XenoGene("WVC_Beholdkind", "Ageless");
            Check.XenoGene("WVC_Beholdkind", "BS_TrulyAgeless");
            Check.XenoGene("WVC_Bloodeater", "Delicate");
            Check.XenoGene("WVC_CatDeity", "PsychicAbility_Extreme");
            Check.XenoGene("WVC_Featherdust", "Pain_Extra");
            Check.XenoGene("WVC_Golemkind", "PsychicAbility_Dull");
            Check.XenoGene("WVC_Lilith", "WVC_NaturalTelepathy");
            Check.XenoGene("WVC_Lilith", "Neversleep");
            Check.XenoGene("WVC_Meca", "BS_Fast_TotalHealing");
            Check.XenoGene("WVC_Mechamata", "Ears_Cat");
            Check.XenoGene("WVC_Mechamata", "Robust");
            Check.XenoGene("WVC_Nociokin", "PerfectImmunity");
            Check.XenoGene("WVC_Overrider", "WVC_NaturalTelepathy");
            Check.XenoGene("WVC_Reaperkind", "PsychicAbility_Deaf");
            Check.XenoGene("WVC_Resurgent", "ToxResist_Total");
            Check.XenoGene("WVC_Resurgent", "Deathless");
            Check.XenoGene("WVC_Rustkind", "XenogermReimplanter");
            Check.XenoGene("WVC_Rustkind", "WVC_ReimplanterNatural_RiseXenogenes");
            Check.XenoGene("WVC_Sandycat", "WVC_ImplanterFang");
            Check.XenoGene("WVC_Undead", "Deathless");
            if (ModsConfig.IsActive(Ids.VREPhytokin))
                Check.XenoGene("WVC_Bloodeater", "VRE_Photosynthesis");
            if (ModsConfig.IsActive(Ids.VRELycanthrope))
                Check.XenoGene("WVC_Lilith", "VRE_GermlineReimplanter");
            if (ModsConfig.IsActive(Ids.VREHussar))
                Check.XenoGene("WVC_Mechamata", "VREH_Toughness");
            if (ModsConfig.IsActive(Ids.VREFungoid))
            {
                Check.XenoGene("WVC_Ashen", "VRE_NoSkillLoss");
                Check.XenoGene("WVC_Ashen", "Learning_Slow");
            }
        }

        [Test]
        public static void XenotypesRewiredDedup()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.XenoGene("WVC_Ashen", "WoundHealing_UltraFast");
            Check.XenoGene("WVC_Beholdkind", "BS_SmallFrame");
            Check.XenoGene("WVC_CatDeity", "AG_Foodless");
            Check.XenoGene("WVC_CatDeity", "AG_Invisibility");
            Check.XenoGene("WVC_Fleshkind", "BS_SmallFrame");
            Check.XenoGene("WVC_Golemkind", "AG_ArmourMajor");
            Check.XenoGene("WVC_Leper", "Body_FemaleOnly");
            Check.XenoGene("WVC_Mechamata", "AG_HeatImmunity");
            Check.XenoGene("WVC_Reaperkind", "VU_NoBlood");
            Check.XenoGene("WVC_Resurgent", "BS_Pain_None");
            Check.XenoGene("WVC_Ripperkind", "VU_NoBlood");
            Check.XenoGene("WVC_RuneDryad", "WoundHealing_VerySlow");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("WVC_Blank", "BS_EarlyMaturity");
                Check.XenoGene("WVC_Golemkind", "VRE_SlowAging");
                Check.XenoGene("WVC_RuneDryad", "BS_Very_Slow");
                Check.XenoGene("WVC_Undead", "VRE_FastAging");
                if (ModsConfig.IsActive(Ids.VREWaster))
                    Check.XenoGene("WVC_Blank", "VRE_Instability_Extreme");
            }
            if (ModsConfig.IsActive(Ids.VREGenie))
                Check.XenoGene("WVC_CatDeity", "VRE_Immunity_VeryWeak");
            if (ModsConfig.IsActive(Ids.VRESanguophage))
                Check.XenoGene("WVC_Shadoweater", "VRE_Sensitivity_Dangerous");
            if (ModsConfig.IsActive(Ids.VREHussar))
                Check.XenoGene("WVC_Nociokin", "VREH_Toughness");
        }

        [Test]
        public static void ApexXenotypesNeverWander()
        {
            if (!Check.Ready("xenotypes.wvcspawns", Ids.WVC, Ids.Biotech))
                return;
            foreach (string name in new[] { "WVC_Ferrkind", "WVC_GeneThrower", "WVC_Rustkind", "WVC_CatDeity" })
                Check.Eq(Check.Def<XenotypeDef>(name).factionlessGenerationWeight, 0f, name + ".factionlessGenerationWeight");
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("OutlanderCivil"), "WVC_GeneThrower"), "OutlanderCivil spawns WVC_GeneThrower");
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("PirateWaster"), "WVC_CatDeity"), "PirateWaster spawns WVC_CatDeity");
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("OutlanderRefugee"), "WVC_CatDeity"), "OutlanderRefugee spawns WVC_CatDeity");
            FactionDef beggars = DefDatabase<FactionDef>.GetNamedSilentFail("Beggars");
            if (beggars != null)
                Check.True(!Check.HasXenotype(beggars, "WVC_CatDeity"), "Beggars spawns WVC_CatDeity");
        }
    }
}
