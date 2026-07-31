using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimsenalHaranaTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.CherryPicker, Ids.RimsenalHarana, Ids.Stoneborn))
                return;
            Check.GenesGone("Aggression_PassivelyAggressive");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.CherryPicker, Ids.RimsenalHarana, Ids.Stoneborn))
                return;
            Check.XenoGene("Harana", "DV_Aggression_Irascible");
        }

        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.RimsenalHarana, Ids.Odyssey, Ids.VGravshipC1))
                return;
            Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>("Apparel_Shocksuit").equippedStatOffsets, "VacuumResistance"),
                0.32f, "Apparel_Shocksuit VacuumResistance");
        }

        [Test]
        public static void PsycastGenesAssigned() =>
            PathGenes.RaceRoster(Ids.RimsenalHarana,
                "", "Harana", "Gene_Skipmaster");
    }
}