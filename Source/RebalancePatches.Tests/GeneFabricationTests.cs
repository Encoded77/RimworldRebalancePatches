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
            Check.PrereqsAre(genefab.prerequisites, "GeneFabrication.prerequisites", "Archogenetics");
            Check.Eq(genefab.tab, Check.Def<ResearchTabDef>("RBP_GeneticsTab"), "GeneFabrication.tab");
            Check.Eq(genefab.requiredResearchBuilding?.defName, "HiTechResearchBench", "GeneFabrication.requiredResearchBuilding");
        }
    }
}
