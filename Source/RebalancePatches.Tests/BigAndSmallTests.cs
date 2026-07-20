using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BigAndSmallTests
    {
        [Test]
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.bigsmall", Ids.BigSmallCore))
                return;
            Check.Eq(Check.Def<XenotypeDef>("VU_Succubus").label, "allurist", "VU_Succubus label");
            Check.Eq(Check.Def<XenotypeDef>("VU_Hellguard").label, "abyssal guard", "VU_Hellguard label");
            Check.Eq(Check.Def<XenotypeDef>("VU_Gatekeeper").label, "Gatekeeper", "VU_Gatekeeper label unchanged");
            Check.Eq(Check.Def<XenotypeDef>("VU_Imp").label, "greater impid", "VU_Imp label");
            Check.Eq(Check.Def<XenotypeDef>("VU_Returned").label, "decayed reanimate", "VU_Returned label");
            Check.Eq(Check.Def<XenotypeDef>("VU_Returned_Intact").label, "reanimate", "VU_Returned_Intact label");
            Check.Eq(Check.Def<XenotypeDef>("VU_ReturnedSkeletal").label, "skeletal reanimate", "VU_ReturnedSkeletal label");
            Check.Eq(Check.Def<XenotypeDef>("BS_FrostJotunInBlue").label, "cryogigant", "BS_FrostJotunInBlue label");
            Check.Eq(Check.Def<PawnKindDef>("BS_WomanInBlue").label, "gigant adventurer", "BS_WomanInBlue label");
        }

        [Test]
        public static void MadScienceFieldTestingPrereqs()
        {
            if (!Check.Ready("bigsmall.madscience", Ids.BigSmallCore))
                return;
            ResearchProjectDef madScience = Check.Optional<ResearchProjectDef>("BS_MadScienceField", "bigsmall.madscience");
            if (madScience == null)
                return;
            Check.True(Check.ContainsResearch(madScience.prerequisites, "GunTurrets"),
                "BS_MadScienceField lacks GunTurrets prerequisite");
            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (Check.ContainsResearch(thing.researchPrerequisites, "BS_MadScienceField"))
                    Check.True(!Check.ContainsResearch(thing.researchPrerequisites, "GunTurrets"),
                        $"{thing.defName} still lists GunTurrets alongside BS_MadScienceField");
                List<ResearchProjectDef> recipePrereqs = Check.RecipePrereqs(thing);
                if (Check.ContainsResearch(recipePrereqs, "BS_MadScienceField"))
                    Check.True(!Check.ContainsResearch(recipePrereqs, "GunTurrets"),
                        $"{thing.defName} recipeMaker still lists GunTurrets alongside BS_MadScienceField");
            }
        }

        [Test]
        public static void GeneIntegratorArchiteTier()
        {
            if (!Check.Ready("bigsmall.geneintegrator", Ids.BigSmallCore, Ids.Biotech))
                return;
            ThingDef integrator = Check.Optional<ThingDef>("BS_GeneGeneIntegrator", "bigsmall.geneintegrator");
            if (integrator == null)
                return;
            Check.Eq(Check.RecipePrereq(integrator)?.defName, "BS_ArchiteGeneScience",
                "BS_GeneGeneIntegrator recipeMaker.researchPrerequisite");
            Check.PrereqsAre(Check.RecipePrereqs(integrator), "BS_GeneGeneIntegrator recipeMaker.researchPrerequisites",
                "Archogenetics");
            Check.Eq(Check.CostOf(integrator, "ArchiteCapsule"), 1, "BS_GeneGeneIntegrator costList[ArchiteCapsule]");
            Check.Eq(Check.CostOf(integrator, "ComponentSpacer"), 2, "BS_GeneGeneIntegrator costList[ComponentSpacer]");
            Check.Eq(Check.StatBase(integrator, "MarketValue"), 4000f, "BS_GeneGeneIntegrator MarketValue");
        }

        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.GenesGone("MinTemp_HugeDecrease", "MaxTemp_HugeIncrease", "FireImmunity",
                "BS_MinTemp_HugeDecrease_Android", "BS_MaxTemp_HugeIncrease_Android", "BS_FireImmunity_Android",
                "BS_NaturalArmor", "BS_ToughSkin", "BS_NoFood");
            if (ModsConfig.IsActive(Ids.VREPigskin))
                Check.GenesGone("BS_FastAging", "BS_VeryFastAging", "BS_SlowAging", "BS_VerySlowAging", "VU_UltraRapidAging");
            if (ModsConfig.IsActive(Ids.VREHighmate))
                Check.GenesGone("BS_Flirty", "VU_Libido_Succubus");
            if (ModsConfig.IsActive(Ids.VREArchon))
                Check.GenesGone("BS_ShortPregnancy");
            if (ModsConfig.IsActive(Ids.VRESanguophage))
                Check.GenesGone("BS_Talons");
            if (ModsConfig.IsActive(Ids.VREFungoid))
                Check.GenesGone("BS_Learning_None");
            if (ModsConfig.IsActive(Ids.VREWaster))
                Check.GenesGone("BS_Instability_Catastrophic");
            if (ModsConfig.IsActive(Ids.VREHussar))
                Check.GenesGone("BS_Abrasive");
            if (ModsConfig.IsActive(Ids.VRELycanthrope))
                Check.GenesGone("BS_NightOwl");
            if (ModsConfig.IsActive(Ids.VREStarjack))
                Check.GenesGone("BS_EVA_Gene", "BS_AndroidEVA_Gene");
            Check.True(DefDatabase<GeneDef>.GetNamedSilentFail("BS_Pain_None") != null, "BS_Pain_None (canonical) missing");
        }

        [Test]
        public static void InternalLegacyGenesRemoved()
        {
            if (!Check.Ready("genetics.bsdupes", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.GenesGone("BS_GeneStabilizing_Moderate", "BS_GeneStabilizing_Great",
                "BS_GeneStabilizing_Extreme", "BS_Deathlike");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.XenoGene("BS_FrostJotunInBlue", "AG_ColdImmunity");
            Check.XenoGene("VU_Gatekeeper", "AG_ArmourMedium");
            Check.XenoGene("VU_Gatekeeper", "AG_HeatImmunity");
            Check.XenoGene("VU_Hellguard", "AG_ArmourMedium");
            Check.XenoGene("VU_Imp", "AG_HeatImmunity");
            Check.XenoGene("VU_ReturnedSkeletal", "AG_HeatImmunity");
            Check.XenoGene("VU_ReturnedSkeletal", "AG_ColdImmunity");
            Check.XenoGene("VU_Succubus", "AG_ArmourMedium");
            Check.XenoGene("VU_Succubus", "AG_HeatImmunity");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("BS_FrostJotunInBlue", "VRE_SlowAging");
                Check.XenoGene("VU_Returned", "VRE_RapidAging");
            }
            if (ModsConfig.IsActive(Ids.VREHighmate))
                Check.XenoGene("VU_Succubus", "VRE_Libido_VeryHigh");
        }

        [Test]
        public static void DeathlikeReplaced()
        {
            if (!Check.Ready("genetics.bsdupes", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.XenoGene("VU_Returned", "BS_LesserDeathless");
            Check.XenoGene("VU_Returned_Intact", "BS_LesserDeathless");
            Check.XenoGene("VU_ReturnedSkeletal", "BS_LesserDeathless");
        }
    }
}
