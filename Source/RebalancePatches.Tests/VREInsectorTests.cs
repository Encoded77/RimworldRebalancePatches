using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREInsectorTests
    {
        [Test]
        public static void ColossalInsectorsForceGiantTrait()
        {
            if (!Check.Ready("vreinsector.colossalweapons", Ids.VREInsector, Ids.VFEInsectoids2, Ids.BigSmallCore))
                return;
            Def def = DefDatabase<GeneDef>.GetNamedSilentFail("VRE_Colossal")
                ?? Check.DefOfType("VanillaRacesExpandedInsector.GenelineGeneDef", "VRE_Colossal");
            GeneDef gene = def as GeneDef;
            Check.True(gene != null, $"VRE_Colossal is a {def.GetType().FullName}, not a GeneDef subclass");
            Check.True(Check.HasForcedTrait(gene, "BS_Giant"), "VRE_Colossal does not force BS_Giant");
        }
    }
}
