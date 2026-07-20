using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GeneRipperTests
    {
        [Test]
        public static void GeneRipperViaDedicatedResearch()
        {
            if (!ModsConfig.IsActive(Ids.GeneRipperDefi) && !ModsConfig.IsActive(Ids.GeneRipperDW))
            {
                Log.Message("[RBP Tests] SKIP geneticsresearch.generipper: no Gene Ripper mod active");
                return;
            }
            if (!Check.Ready("geneticsresearch.generipper", Ids.Biotech) || !Check.GeneticsTabLoaded("geneticsresearch.generipper"))
                return;
            ResearchProjectDef ripperResearch = Check.Optional<ResearchProjectDef>("RBP_GeneRipper", "geneticsresearch.generipper");
            if (ripperResearch == null)
                return;
            Check.Eq(ripperResearch.baseCost, 1200f, "RBP_GeneRipper.baseCost");
            Check.PrereqsAre(ripperResearch.prerequisites, "RBP_GeneRipper.prerequisites", "Xenogermination");
            Check.PrereqsAre(Check.Def<ThingDef>("GeneRipper").researchPrerequisites,
                "GeneRipper.researchPrerequisites", "RBP_GeneRipper");
        }
    }
}
