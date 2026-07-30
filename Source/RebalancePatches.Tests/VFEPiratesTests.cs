using System;
using System.Collections.Generic;
using HarmonyLib;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VFEPiratesTests
    {
        /// <summary>
        /// Warcasket pieces are VFEPirates.WarcasketDef, not ThingDef, so they are reached by name
        /// rather than by a compile-time reference to a mod we do not depend on.
        /// </summary>
        public static float? WarcasketVacuum(string defName)
        {
            Type type = GenTypes.GetTypeInAnyAssembly("VFEPirates.WarcasketDef");
            if (type == null)
                return null;
            Def def = GenDefDatabase.GetDefSilentFail(type, defName);
            if (def == null)
                return null;
            var offsets = AccessTools.Field(type, "equippedStatOffsets")?.GetValue(def) as List<StatModifier>;
            return Check.StatModifierValue(offsets, "VacuumResistance");
        }

        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.VFEPirates, Ids.Odyssey, Ids.VGravshipC1, Ids.AlphaMechs))
                return;
            Check.Eq(WarcasketVacuum("VFEP_Warcasket_Helmet_MechController"), 0.65f,
                "VFEP_Warcasket_Helmet_MechController VacuumResistance");
        }

        [Test]
        public static void WarcasketChargeWeaponPrereqs()
        {
            if (!Check.Ready("vfepirates.chargeweapons", Ids.VFEPirates))
                return;
            foreach (string box in new[] { "VFEP_Box_ChargeLance", "VFEP_Box_ChargeBlaster" })
                Check.PrereqsAre(Check.Def<ThingDef>(box).researchPrerequisites, $"{box}.researchPrerequisites",
                    "ChargedShot", "VFEP_SpacerWarcasketWeaponry");
            if (ModsConfig.IsActive(Ids.VWECoilguns))
                Check.PrereqsAre(Check.Def<ThingDef>("VFEP_Box_Railgun").researchPrerequisites, "VFEP_Box_Railgun.researchPrerequisites",
                    "VWE_MassDrivers", "VFEP_SpacerWarcasketWeaponry");
            if (ModsConfig.IsActive(Ids.WarcasketQuality))
            {
                foreach (string gun in new[] { "VFEP_WarcasketGun_ChargeLance", "VFEP_WarcasketGun_ChargeBlaster" })
                    Check.PrereqsAre(Check.RecipePrereqs(Check.Def<ThingDef>(gun)), $"{gun} recipeMaker.researchPrerequisites",
                        "ChargedShot");
                if (ModsConfig.IsActive(Ids.VWECoilguns))
                    Check.PrereqsAre(Check.RecipePrereqs(Check.Def<ThingDef>("VFEP_WarcasketGun_Railgun")),
                        "VFEP_WarcasketGun_Railgun recipeMaker.researchPrerequisites",
                        "VWE_MassDrivers", "VFEP_SpacerWarcasketWeaponry");
            }
        }

        [Test]
        public static void EmpireNotPermanentlyHostileToPirates()
        {
            if (!Check.Ready("vfepirates.empirescenario", Ids.VFEPirates, Ids.Royalty))
                return;
            FactionDef empire = Check.Def<FactionDef>("Empire");
            FactionDef playerPirate = Check.Def<FactionDef>("VFEP_PlayerPirate");
            Check.True(empire.permanentEnemyToEveryoneExcept != null && empire.permanentEnemyToEveryoneExcept.Contains(playerPirate),
                "Empire.permanentEnemyToEveryoneExcept does not list VFEP_PlayerPirate");
        }
    }
}
