using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GeneExtractorTiersTests
    {
        [Test]
        public static void ExtractionVatsViaDedicatedResearch()
        {
            if (!Check.Ready("genetics.extractortiers", Ids.GeneExtractorTiers, Ids.Biotech) || !Check.GeneticsTabLoaded("genetics.extractortiers"))
                return;
            ResearchProjectDef vats = Check.Def<ResearchProjectDef>("RBP_GeneExtractionVats");
            Check.Eq(vats.baseCost, 3000f, "RBP_GeneExtractionVats.baseCost");
            Check.PrereqsAre(vats.prerequisites, "RBP_GeneExtractionVats.prerequisites", "GeneProcessor");
            ResearchProjectDef archite = Check.Def<ResearchProjectDef>("RBP_ArchiteExtraction");
            Check.Eq(archite.baseCost, 5000f, "RBP_ArchiteExtraction.baseCost");
            Check.PrereqsAre(archite.prerequisites, "RBP_ArchiteExtraction.prerequisites", "Archogenetics");
            Check.PrereqsAre(Check.Def<ThingDef>("GET_GeneExtractor_II").researchPrerequisites,
                "GET_GeneExtractor_II.researchPrerequisites", "RBP_GeneExtractionVats");
            foreach (string vat in new[] { "GET_GeneExtractor_III", "GET_GeneExtractor_IV" })
                Check.PrereqsAre(Check.Def<ThingDef>(vat).researchPrerequisites,
                    $"{vat}.researchPrerequisites", "RBP_ArchiteExtraction");
        }

        [Test]
        public static void GeneNodesViaDedicatedResearch()
        {
            if (!Check.Ready("genetics.genenodes", Ids.GeneExtractorTiers, Ids.Biotech) || !Check.GeneticsTabLoaded("genetics.genenodes"))
                return;
            ResearchProjectDef nodes = Check.Def<ResearchProjectDef>("RBP_GeneNodes");
            Check.Eq(nodes.baseCost, 1200f, "RBP_GeneNodes.baseCost");
            Check.PrereqsAre(nodes.prerequisites, "RBP_GeneNodes.prerequisites", "Xenogermination");
            ResearchProjectDef architeNodes = Check.Def<ResearchProjectDef>("RBP_ArchiteGeneNodes");
            Check.Eq(architeNodes.baseCost, 4500f, "RBP_ArchiteGeneNodes.baseCost");
            Check.PrereqsAre(architeNodes.prerequisites, "RBP_ArchiteGeneNodes.prerequisites", "Archogenetics");
            int baseNodes = 0;
            int architeNodeCount = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (Check.ContainsResearch(def.researchPrerequisites, "RBP_GeneNodes"))
                    baseNodes++;
                if (Check.ContainsResearch(def.researchPrerequisites, "RBP_ArchiteGeneNodes"))
                {
                    architeNodeCount++;
                    Check.True(Check.CostOf(def, "ArchiteCapsule") >= 2, $"{def.defName} costs no archite capsules");
                    Check.True(Check.CostOf(def, "Silver") >= 750, $"{def.defName} lacks the silver cost");
                }
            }
            Check.True(baseNodes > 0, "no gene node unlocks from RBP_GeneNodes");
            Check.True(architeNodeCount > 0, "no archite gene node unlocks from RBP_ArchiteGeneNodes");
        }
    }
}
