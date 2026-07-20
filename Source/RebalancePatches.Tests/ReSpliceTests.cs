using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ReSpliceTests
    {
        [Test]
        public static void BuildingsViaDedicatedResearch()
        {
            if (!Check.Ready("geneticsresearch.resplice", Ids.ReSplice, Ids.Biotech) || !Check.GeneticsTabLoaded("geneticsresearch.resplice"))
                return;
            ResearchTabDef tab = Check.Def<ResearchTabDef>("RBP_GeneticsTab");
            ResearchProjectDef centrifuge = Check.Def<ResearchProjectDef>("RBP_GenepackCentrifuge");
            Check.Eq(centrifuge.baseCost, 1400f, "RBP_GenepackCentrifuge.baseCost");
            Check.Eq(centrifuge.tab, tab, "RBP_GenepackCentrifuge.tab");
            Check.PrereqsAre(centrifuge.prerequisites, "RBP_GenepackCentrifuge.prerequisites", "Xenogermination");
            ResearchProjectDef replicator = Check.Def<ResearchProjectDef>("RBP_XenogermReplicator");
            Check.Eq(replicator.baseCost, 2000f, "RBP_XenogermReplicator.baseCost");
            Check.PrereqsAre(replicator.prerequisites, "RBP_XenogermReplicator.prerequisites", "GeneProcessor");
            ThingDef geneCentrifuge = Check.Def<ThingDef>("RS_GeneCentrifuge");
            Check.PrereqsAre(geneCentrifuge.researchPrerequisites, "RS_GeneCentrifuge.researchPrerequisites", "RBP_GenepackCentrifuge");
            Check.Eq(geneCentrifuge.label, "genepack centrifuge", "RS_GeneCentrifuge.label");
            ThingDef duplicator = Check.Def<ThingDef>("RS_XenoGermDuplicator");
            Check.PrereqsAre(duplicator.researchPrerequisites, "RS_XenoGermDuplicator.researchPrerequisites", "RBP_XenogermReplicator");
            Check.Eq(duplicator.label, "xenogerm replicator", "RS_XenoGermDuplicator.label");
        }
    }
}
