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
        public static void GeneIntegratorUltraTier()
        {
            if (!Check.Ready("geneticsresearch.consumables", Ids.BigSmallCore, Ids.Biotech)
                || !Check.GeneticsTabLoaded("geneticsresearch.consumables"))
                return;
            ResearchProjectDef integration = Check.Def<ResearchProjectDef>("RBP_GeneIntegration");
            Check.Eq(integration.techLevel, TechLevel.Ultra, "RBP_GeneIntegration.techLevel");
            Check.Eq(integration.baseCost, 5000f, "RBP_GeneIntegration.baseCost");
            Check.PrereqsAre(integration.prerequisites, "RBP_GeneIntegration.prerequisites", "Archogenetics");

            ThingDef integrator = Check.Optional<ThingDef>("BS_GeneGeneIntegrator", "geneticsresearch.consumables");
            if (integrator == null)
                return;
            // The child adds a recipeMaker with only researchPrerequisite set, so the rest of the
            // node still merges in from BS_GeneToolBase - assert the resolved single field.
            Check.Eq(Check.RecipePrereq(integrator)?.defName, "RBP_GeneIntegration",
                "BS_GeneGeneIntegrator recipeMaker.researchPrerequisite");
            Check.Eq(Check.CostOf(integrator, "ArchiteCapsule"), 1, "BS_GeneGeneIntegrator costList[ArchiteCapsule]");
            Check.Eq(Check.CostOf(integrator, "ComponentSpacer"), 2, "BS_GeneGeneIntegrator costList[ComponentSpacer]");
            Check.Eq(Check.StatBase(integrator, "MarketValue"), 4000f, "BS_GeneGeneIntegrator MarketValue");
        }

        [Test]
        public static void GeneToolsMergedIntoToolkits()
        {
            if (!Check.Ready("geneticsresearch.consumables", Ids.BigSmallCore, Ids.Biotech)
                || !Check.GeneticsTabLoaded("geneticsresearch.consumables"))
                return;
            // Both B&S projects are gone, so nothing may still point at them.
            foreach (string gone in new[] { "BS_GeneScience", "BS_ArchiteGeneScience" })
                Check.True(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(gone) == null,
                    $"{gone} still present");
            ResearchProjectDef madScience = Check.Optional<ResearchProjectDef>("BS_MadScienceField", "geneticsresearch.consumables");
            if (madScience != null)
                Check.True(!Check.ContainsResearch(madScience.prerequisites, "BS_GeneScience"),
                    "BS_MadScienceField still requires BS_GeneScience");

            foreach (string tool in new[] { "BS_CreateXenogerm", "BS_CreateXenogerm_Endo" })
            {
                ThingDef def = Check.Optional<ThingDef>(tool, "geneticsresearch.consumables");
                if (def != null)
                    Check.Eq(Check.RecipePrereq(def)?.defName, "RBP_GeneToolkits",
                        $"{tool} recipeMaker.researchPrerequisite");
            }
            ThingDef architeCloner = Check.Optional<ThingDef>("BS_CreateArchiteXenogerm_Endo", "geneticsresearch.consumables");
            if (architeCloner != null)
            {
                Check.PrereqsAre(Check.RecipePrereqs(architeCloner),
                    "BS_CreateArchiteXenogerm_Endo recipeMaker.researchPrerequisites",
                    "RBP_GeneToolkits", "Archogenetics");
                // Also on the ThingDef, so research-tree UIs group the item with its own recipe
                // instead of listing it as an ungated unlock.
                Check.PrereqsAre(architeCloner.researchPrerequisites,
                    "BS_CreateArchiteXenogerm_Endo researchPrerequisites",
                    "RBP_GeneToolkits", "Archogenetics");
            }
        }

        [Test]
        public static void AnimalSizeSerumsMerged()
        {
            if (!Check.Ready("geneticsresearch.consumables", Ids.BigSmallCore, Ids.Biotech)
                || !Check.GeneticsTabLoaded("geneticsresearch.consumables"))
                return;
            Check.True(DefDatabase<ResearchProjectDef>.GetNamedSilentFail("BS_AnimalGrowthSerums") == null,
                "BS_AnimalGrowthSerums still present");
            foreach (string serum in new[] { "BS_Giant_Serum", "BS_Shrink_Serum" })
            {
                ThingDef def = Check.Optional<ThingDef>(serum, "geneticsresearch.consumables");
                if (def != null)
                    Check.Eq(Check.RecipePrereq(def)?.defName, "RBP_GeneToolkits",
                        $"{serum} recipeMaker.researchPrerequisite");
            }
        }

        [Test]
        public static void WeaponizedGeneticsAtUltraTier()
        {
            if (!Check.Ready("geneticsresearch.consumables", Ids.BigSmallCore, Ids.Biotech)
                || !Check.GeneticsTabLoaded("geneticsresearch.consumables"))
                return;
            ResearchProjectDef weapons = Check.Optional<ResearchProjectDef>("BS_MadScienceField", "geneticsresearch.consumables");
            if (weapons == null)
                return;
            Check.Eq(weapons.label, "weaponized genetics", "BS_MadScienceField.label");
            Check.Eq(weapons.techLevel, TechLevel.Ultra, "BS_MadScienceField.techLevel");
            Check.Eq(weapons.baseCost, 4000f, "BS_MadScienceField.baseCost");
            Check.Eq(weapons.tab, Check.Def<ResearchTabDef>("RBP_GeneticsTab"), "BS_MadScienceField.tab");
            Check.True(Check.ContainsResearch(weapons.prerequisites, "Archogenetics"),
                "BS_MadScienceField lacks the Archogenetics prerequisite");
            // Both retired projects must be off its prerequisite list, or it is unreachable.
            foreach (string gone in new[] { "BS_GeneScience", "BS_AnimalGrowthSerums" })
                Check.True(!Check.ContainsResearch(weapons.prerequisites, gone),
                    $"BS_MadScienceField still requires {gone}");
            // bigsmall.madscience adds GunTurrets earlier in the folder; we must not have wiped it.
            if (SettingsRegistry.GetEffective("bigsmall.madscience"))
                Check.True(Check.ContainsResearch(weapons.prerequisites, "GunTurrets"),
                    "the consumables re-tier dropped the GunTurrets prerequisite");
        }

        [Test]
        public static void RedundantGeneToolsRemoved()
        {
            if (!Check.Ready("geneticsresearch.consumables", Ids.BigSmallCore, Ids.Biotech, Ids.CherryPicker)
                || !Check.GeneticsTabLoaded("geneticsresearch.consumables"))
                return;
            // The discombobulator only goes when Alpha Genes is present to supply its replacement.
            if (ModsConfig.IsActive(Ids.AlphaGenes))
            {
                Check.ThingUnobtainable("BS_GeneDicombobulator");
                // Cherry Picker cannot see Alpha Genes' custom spawner comp; those entries are ours.
                Check.True(!AlphaGenesTests.DispenserOffers("BS_GeneDicombobulator"),
                    "AG_RandomGeneTool still offers BS_GeneDicombobulator");
                Check.True(!AlphaGenesTests.DispenserOffers("BS_CreateArchiteXenogerm"),
                    "AG_RandomGeneTool still offers BS_CreateArchiteXenogerm");
            }
            Check.ThingUnobtainable("BS_CreateArchiteXenogerm");
            // Its replacement has to survive intact - it was the cut one's only ingredient.
            ThingDef survivor = Check.Optional<ThingDef>("BS_CreateArchiteXenogerm_Endo", "geneticsresearch.consumables");
            if (survivor != null)
                Check.True(survivor.tradeability != Tradeability.None,
                    "BS_CreateArchiteXenogerm_Endo was neutered along with the cut cloner");
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
