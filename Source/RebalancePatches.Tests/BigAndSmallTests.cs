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
    }
}
