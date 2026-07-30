using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREArchonTests
    {
        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.VREArchon, Ids.Odyssey, Ids.VGravshipC1))
                return;
            // Archotech plate is the buff side of this feature: with any helmet it clears full protection.
            Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>("VREA_Apparel_Archoplate").equippedStatOffsets, "VacuumResistance"),
                0.70f, "VREA_Apparel_Archoplate VacuumResistance");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.VREArchon))
                return;
            Check.XenoGene("VRE_Archon", "BS_EarlyMaturity");
            Check.XenoGene("VRE_Archon", "WVC_ArchitePsylink");
        }
    }
}
