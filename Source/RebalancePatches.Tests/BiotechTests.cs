using HarmonyLib;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BiotechTests
    {
        [Test]
        public static void MechRaidGroupsAdded()
        {
            if (!Check.Ready("vanilla.mechraidgroups", Ids.Biotech))
                return;
            FactionDef mechanoid = Check.Def<FactionDef>("Mechanoid");
            bool HasOption(string kindName)
            {
                if (mechanoid.pawnGroupMakers == null)
                    return false;
                foreach (PawnGroupMaker maker in mechanoid.pawnGroupMakers)
                    if (maker.options != null)
                        foreach (PawnGenOption option in maker.options)
                            if (option.kind != null && option.kind.defName == kindName)
                                return true;
                return false;
            }
            Check.True(HasOption("Mech_Scyther"), "Mechanoid faction has no combat group with Mech_Scyther");
            if (ModsConfig.IsActive(Ids.RimsenalSpacer))
                Check.True(HasOption("Mech_Skutaton"), "Mechanoid raids never include Mech_Skutaton");
            if (ModsConfig.IsActive(Ids.AlphaMechs))
                Check.True(HasOption("AM_Aura"), "Mechanoid raids never include AM_Aura");
            if (ModsConfig.IsActive(Ids.RimsenalSpacer) && ModsConfig.IsActive(Ids.AlphaMechs))
                Check.True(HasOption("Mech_Bombito"), "Mechanoid raids never include Mech_Bombito (bomb-rush group)");
        }

        [Test]
        public static void GeneticsResearchTab()
        {
            if (!Check.Ready("geneticsresearch.core", Ids.Biotech))
                return;
            ResearchTabDef tab = Check.Def<ResearchTabDef>("RBP_GeneticsTab");
            ResearchProjectDef sampling = Check.Def<ResearchProjectDef>("RBP_GeneticSampling");
            Check.Eq(sampling.baseCost, 1000f, "RBP_GeneticSampling.baseCost");
            Check.Eq(sampling.tab, tab, "RBP_GeneticSampling.tab");
            Check.PrereqsAre(sampling.prerequisites, "RBP_GeneticSampling.prerequisites", "MicroelectronicsBasics");
            Check.Eq(sampling.requiredResearchBuilding?.defName, "HiTechResearchBench", "RBP_GeneticSampling.requiredResearchBuilding");
            ResearchProjectDef xenogermination = Check.Def<ResearchProjectDef>("Xenogermination");
            Check.Eq(xenogermination.label, "xenogerm assembly", "Xenogermination.label");
            Check.Eq(xenogermination.techLevel, TechLevel.Spacer, "Xenogermination.techLevel");
            Check.Eq(xenogermination.baseCost, 1500f, "Xenogermination.baseCost");
            Check.Eq(xenogermination.tab, tab, "Xenogermination.tab");
            Check.PrereqsAre(xenogermination.prerequisites, "Xenogermination.prerequisites", "RBP_GeneticSampling");
            ResearchProjectDef processor = Check.Def<ResearchProjectDef>("GeneProcessor");
            Check.Eq(processor.techLevel, TechLevel.Spacer, "GeneProcessor.techLevel");
            Check.Eq(processor.baseCost, 2500f, "GeneProcessor.baseCost");
            Check.Eq(processor.tab, tab, "GeneProcessor.tab");
            ResearchProjectDef archogenetics = Check.Def<ResearchProjectDef>("Archogenetics");
            Check.Eq(archogenetics.techLevel, TechLevel.Spacer, "Archogenetics.techLevel");
            Check.Eq(archogenetics.baseCost, 4000f, "Archogenetics.baseCost");
            Check.Eq(archogenetics.tab, tab, "Archogenetics.tab");
            Check.PrereqsAre(Check.Def<ThingDef>("GeneExtractor").researchPrerequisites,
                "GeneExtractor.researchPrerequisites", "RBP_GeneticSampling");
            Check.PrereqsAre(Check.Def<ThingDef>("GeneAssembler").researchPrerequisites,
                "GeneAssembler.researchPrerequisites", "Xenogermination");
            Check.True(Check.ContainsResearch(Check.Def<ThingDef>("GeneBank").researchPrerequisites, "RBP_GeneticSampling"),
                "GeneBank does not unlock from RBP_GeneticSampling");
        }

        [Test]
        public static void GeneComplexitySliders()
        {
            if (Check.Ready("vanilla.genecomplexityprocessor", Ids.Biotech))
                Check.Eq(Check.StatBase(Check.Def<ThingDef>("GeneProcessor"), "GeneticComplexityIncrease"),
                    (float)SettingsRegistry.GetEffectiveValue("vanilla.genecomplexityprocessor"),
                    "GeneProcessor GeneticComplexityIncrease vs slider value");
            if (Check.Ready("vanilla.genecomplexitybase", Ids.Biotech))
                Check.HarmonyPatched(AccessTools.Method(typeof(Building_GeneAssembler), "MaxComplexity"),
                    "vanilla.genecomplexitybase");
        }
    }
}
