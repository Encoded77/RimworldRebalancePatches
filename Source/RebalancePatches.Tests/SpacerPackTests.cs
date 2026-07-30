using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SpacerPackTests
    {
        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.SpacerPack, Ids.Odyssey, Ids.VGravshipC1))
                return;
            // A belt covers no body part and stacks with any armor, so 0.70 there was most of a suit
            // for free. Nothing in vanilla puts vacuum resistance on a belt at all.
            Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>("dvd_VoidsentPulsepack").equippedStatOffsets, "VacuumResistance"),
                0.20f, "dvd_VoidsentPulsepack VacuumResistance");
        }
    }
}
