using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSSlimesTests
    {
        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BigSmallSlimes))
                return;
            Check.XenoGene("BS_BananaSplitSlime", "AG_ColdImmunity");
            Check.XenoGene("BS_BananaSplitSlimeGiant", "AG_ColdImmunity");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("BS_EmperorSlime", "VRE_SlowAging");
                Check.XenoGene("BS_FrostSlime", "VRE_SlowAging");
                Check.XenoGene("BS_FrostSlimeGiant", "VRE_SlowAging");
                Check.XenoGene("BS_LavaSlimeGiant", "VRE_SlowAging");
            }
            if (ModsConfig.IsActive(Ids.VREHighmate))
                Check.XenoGene("BS_PinkSlime", "VRE_Flirty");
        }
    }
}
