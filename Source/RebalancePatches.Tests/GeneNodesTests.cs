using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GeneNodesTests
    {
        [Test]
        public static void TrueArchiteNodesPricier()
        {
            if (!Check.Ready("genetics.genenodes", Ids.GeneNodes, Ids.Biotech) || !Check.GeneticsTabLoaded("genetics.genenodes"))
                return;
            if (Check.Optional<ResearchProjectDef>("RBP_ArchiteGeneNodes", "genetics.genenodes") == null)
                return;
            int trueArchiteNodes = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!Check.ContainsResearch(def.researchPrerequisites, "RBP_ArchiteGeneNodes"))
                    continue;
                if (Check.CostOf(def, "Silver") == 1000)
                {
                    trueArchiteNodes++;
                    Check.Eq(Check.CostOf(def, "ArchiteCapsule"), 3, $"{def.defName} costList[ArchiteCapsule]");
                }
            }
            Check.True(trueArchiteNodes > 0, "no Genes-for-Sale archite node got the pricier costList (Silver 1000)");
        }
    }
}
