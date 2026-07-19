using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREArchonTests
    {
        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.VREArchon))
                return;
            Check.XenoGene("VRE_Archon", "BS_EarlyMaturity");
            Check.XenoGene("VRE_Archon", "WVC_ArchitePsylink");
        }
    }
}
