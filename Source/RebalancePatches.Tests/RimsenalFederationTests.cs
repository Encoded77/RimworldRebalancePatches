using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimsenalFederationTests
    {
        [Test]
        public static void FederationNeverPacifist()
        {
            if (!Check.Ready("memes.factions", Ids.RimsenalFederation, Ids.VIEMemes, Ids.AlphaMemes))
                return;
            MemeDef nonViolence = Check.Def<MemeDef>("AM_NonViolence");
            Check.True(Check.Def<FactionDef>("FPC").disallowedMemes?.Contains(nonViolence) == true,
                "FPC does not disallow AM_NonViolence");
        }

        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.RimsenalFederation, Ids.Odyssey, Ids.VGravshipC1))
                return;
            Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>("Apparel_MarksmanGearH").equippedStatOffsets, "VacuumResistance"),
                0.59f, "Apparel_MarksmanGearH VacuumResistance");
        }
    }
}
