using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ImpactWeaponryTests
    {
        [Test]
        public static void ImpactBolterPrereqs()
        {
            if (!Check.Ready("impactweaponry.bolterprereq", Ids.ImpactWeaponry, Ids.VFEPirates))
                return;
            Check.PrereqsAre(Check.Def<ThingDef>("DV_Box_ImpactBolter").researchPrerequisites,
                "DV_Box_ImpactBolter.researchPrerequisites", "VFEP_SpacerWarcasketWeaponry", "DV_ImpactShot");
        }

        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.ImpactWeaponry, Ids.Odyssey, Ids.VGravshipC1))
                return;
            ThingDef helmet = Check.Def<ThingDef>("DV_Apparel_ArmorHelmetCrusader");
            Check.Eq(Check.StatModifierValue(helmet.equippedStatOffsets, "VacuumResistance"), 0.67f, "DV_Apparel_ArmorHelmetCrusader VacuumResistance");
            Check.Eq(Check.StatBase(helmet, "ArmorRating_Sharp"), 1.10f, "DV_Apparel_ArmorHelmetCrusader ArmorRating_Sharp");
        }
    }
}
