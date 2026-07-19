using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SpacerArsenalTests
    {
        [Test]
        public static void HeavyWeaponPrereqs()
        {
            if (!Check.Ready("spacerarsenal.prereqs", Ids.SpacerArsenal))
                return;
            if (ModsConfig.IsActive(Ids.VWE))
            {
                foreach (string thing in new[] { "DV_Weapon_GrenadeThump", "DV_Gun_BruteRifle", "DV_Gun_ClashHMG",
                    "DV_Gun_ClashRifle", "DV_Weapon_GrenadeContact" })
                {
                    ThingDef def = Check.Def<ThingDef>(thing);
                    Check.True(Check.RecipePrereq(def) == null, $"{thing} still has a single researchPrerequisite");
                    Check.PrereqsAre(Check.RecipePrereqs(def), $"{thing} recipeMaker.researchPrerequisites",
                        "Fabrication", "VWE_HeavyWeapons");
                }
            }
            if (ModsConfig.IsActive(Ids.VWECoilguns))
            {
                foreach (string thing in new[] { "DV_Gun_CoilgunLance", "DV_MeleeWeapon_SparkSabre" })
                    Check.Eq(Check.RecipePrereq(Check.Def<ThingDef>(thing))?.defName, "VWE_MassDrivers",
                        $"{thing} researchPrerequisite");
            }
        }

        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.SpacerArsenal, Ids.Odyssey, Ids.VGravshipC1))
                return;
            ThingDef helmet = Check.Def<ThingDef>("DV_Apparel_EnsignHelmet");
            Check.Eq(Check.StatModifierValue(helmet.equippedStatOffsets, "VacuumResistance"), 0.65f, "DV_Apparel_EnsignHelmet VacuumResistance");
            Check.Eq(Check.StatBase(helmet, "ArmorRating_Sharp"), 0.80f, "DV_Apparel_EnsignHelmet ArmorRating_Sharp");
        }
    }
}
