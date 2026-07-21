using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GeneFabricationTests
    {
        [Test]
        public static void ArchogeneticsCapstone()
        {
            if (!Check.Ready("geneticsresearch.genefab", Ids.GeneFabrication, Ids.Biotech) || !Check.GeneticsTabLoaded("geneticsresearch.genefab"))
                return;
            ResearchProjectDef genefab = Check.Def<ResearchProjectDef>("GeneFabrication");
            Check.Eq(genefab.baseCost, 8000f, "GeneFabrication.baseCost");
            // Shipped as Title Case with a description about multianalyzers, which is not what the
            // project unlocks - retitled to match the rest of the tab and to say what it does.
            Check.Eq(genefab.label, "gene fabrication", "GeneFabrication.label");
            Check.True(!genefab.description.Contains("multianalyzer"),
                "GeneFabrication still carries its original multianalyzer description");
            Check.PrereqsAre(genefab.prerequisites, "GeneFabrication.prerequisites", "Archogenetics");
            Check.Eq(genefab.tab, Check.Def<ResearchTabDef>("RBP_GeneticsTab"), "GeneFabrication.tab");
            Check.Eq(genefab.requiredResearchBuilding?.defName, "HiTechResearchBench", "GeneFabrication.requiredResearchBuilding");
            Check.Eq(genefab.techLevel, TechLevel.Ultra, "GeneFabrication.techLevel");
            Check.Eq(genefab.researchViewX, 5f, "GeneFabrication.researchViewX");
        }

        [Test]
        public static void GenepackRecipesDoNotCloggTheTree()
        {
            if (!Check.Ready("geneticsresearch.genefab", Ids.GeneFabrication, Ids.Biotech)
                || !Check.GeneticsTabLoaded("geneticsresearch.genefab"))
                return;
            // Gene Fabrication implies one recipe per gene and gives the archite ones an
            // Archogenetics prerequisite. The capstone already gates the fabricator, so that
            // prerequisite is redundant and only serves to bury the Archogenetics research entry
            // under hundreds of "make genepack" rows in the research tree.
            int genepackRecipes = 0;
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (!recipe.defName.StartsWith("Make_Genepack_"))
                    continue;
                genepackRecipes++;
                Check.True(recipe.researchPrerequisite == null,
                    $"{recipe.defName} still lists {recipe.researchPrerequisite?.defName} as its own prerequisite");
            }
            Check.True(genepackRecipes > 0,
                "no Make_Genepack_ recipes found - Gene Fabrication renamed its generated recipes");
        }
    }
}
