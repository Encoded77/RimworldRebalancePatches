using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GeneExtractorTiersTests
    {
        [Test]
        public static void PsycastGeneNodeEndsTheTree()
        {
            if (!Check.Ready("geneticsresearch.psycastnodes", Ids.GeneExtractorTiers, Ids.VpeBiotechIntegration, Ids.Biotech)
                || !Check.GeneticsTabLoaded("geneticsresearch.psycastnodes"))
                return;

            ResearchProjectDef project = Check.Def<ResearchProjectDef>("RBP_PsycastGeneNodes");
            Check.Soft(project.baseCost == 8000f, $"RBP_PsycastGeneNodes.baseCost is {project.baseCost}, expected 8000");
            Check.PrereqsAre(project.prerequisites, "RBP_PsycastGeneNodes.prerequisites", "RBP_ArchiteGeneNodes");

            ThingDef node = Check.Def<ThingDef>("RBP_GN_Psycast");
            Check.Soft(Check.ContainsResearch(node.researchPrerequisites, "RBP_PsycastGeneNodes"),
                "RBP_GN_Psycast does not unlock from RBP_PsycastGeneNodes");
            Check.Soft(Check.CostOf(node, "VPE_Eltex") == 10,
                $"RBP_GN_Psycast costs {Check.CostOf(node, "VPE_Eltex")} eltex, expected 10");

            // The node is only worth building if it actually carries the path genes the mod gates on.
            var carried = new System.Collections.Generic.List<string>();
            foreach (CompProperties comp in node.comps)
            {
                if (comp.GetType().Name != "CompProperties_GeneNode")
                    continue;
                if (Check.Field(comp, "geneList") is System.Collections.IEnumerable genes)
                    foreach (object gene in genes)
                        carried.Add(gene is Def def ? def.defName : gene?.ToString());
            }
            Check.Note($"node carries {carried.Count} gene(s): " + string.Join(", ", carried.ToArray()));

            foreach (string gene in new[] { "Gene_Archon", "Gene_Archotechist", "Gene_Hemosage", "Gene_Puppeteer",
                "Gene_Wildspeaker" })
                Check.Soft(carried.Contains(gene), $"RBP_GN_Psycast does not carry {gene}");

            // More Psycaster Genes gates each of its genes on its own path mod, so only assert the ones
            // whose path mod is actually loaded.
            if (ModsConfig.IsActive(Ids.MorePsycasterGenes))
                foreach (var pair in new[]
                {
                    new[] { "edern.combatpsycasts", "Gene_CP_Combat" },
                    new[] { "aranmaho.rangerclass", "Gene_Ranger" },
                    new[] { "myf.lightseeker", "Gene_LightSeeker" },
                    new[] { "myf.skyrunner", "Gene_Skyrunner" },
                    new[] { "rabbit.stuncastervpe2", "Gene_Stunskip" },
                    new[] { "aranmaho.ravenouseye.wildhunter.psycast", "Gene_Druid" },
                    new[] { "aranmaho.makai.psycast", "Gene_Golden_Order" },
                })
                    if (ModsConfig.IsActive(pair[0]))
                        Check.Soft(carried.Contains(pair[1]),
                            $"{pair[0]} is loaded but RBP_GN_Psycast does not carry {pair[1]}");

            Check.SoftResult();
        }

        [Test]
        public static void ExtractionVatsViaDedicatedResearch()
        {
            if (!Check.Ready("geneticsresearch.extractortiers", Ids.GeneExtractorTiers, Ids.Biotech) || !Check.GeneticsTabLoaded("geneticsresearch.extractortiers"))
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
            if (!Check.Ready("geneticsresearch.genenodes", Ids.GeneExtractorTiers, Ids.Biotech) || !Check.GeneticsTabLoaded("geneticsresearch.genenodes"))
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
