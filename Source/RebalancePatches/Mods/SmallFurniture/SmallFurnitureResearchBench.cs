using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.SmallFurniture
{
    /// <summary>
    /// Small Furniture's medium and small hi-tech research benches carry a DefExtension_InterchangeDef
    /// naming the vanilla bench they stand in for, but the mod only honours it while its own (unrelated)
    /// small multi-analyzer toggle is on, so with that toggle off no research requiring the vanilla
    /// Hi-Tech Research Bench can be started at them. This reads the same interchange marking and lets
    /// those benches satisfy the requirement regardless, mirroring the vanilla power and facility checks.
    /// </summary>
    public static class SmallFurnitureResearchBench
    {
        public const string SettingKey = "smallfurniture.hitechbenchresearch";
        private const string ModId = "xercaine.furniture.small";
        private const string ExtensionTypeName = "SmallFurniture.DefExtension_InterchangeDef";

        private static bool built;
        private static Dictionary<ThingDef, ThingDef> benchToBuilding;
        private static Dictionary<ThingDef, ThingDef> facilityToFacility;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                if (!ModsConfig.IsActive(ModId))
                    return;

                MethodInfo target = AccessTools.Method(typeof(ResearchProjectDef),
                    nameof(ResearchProjectDef.CanBeResearchedAt),
                    new[] { typeof(Building_ResearchBench), typeof(bool) });
                if (target == null)
                {
                    Log.Warning("[Rebalance Patches] ResearchProjectDef.CanBeResearchedAt could not be found, so "
                        + "Small Furniture's hi-tech research benches will not stand in for the vanilla bench.");
                    return;
                }

                harmony.Patch(target,
                    postfix: new HarmonyMethod(typeof(SmallFurnitureResearchBench), nameof(CanBeResearchedAtPostfix)));

                // The "need research bench" alert runs its own private def == requiredResearchBuilding
                // check instead of CanBeResearchedAt, so it keeps firing at the stand-in benches.
                MethodInfo alertGetter = AccessTools.PropertyGetter(typeof(Alert_NeedResearchBench),
                    "HasRequiredResearchBench");
                if (alertGetter != null)
                    harmony.Patch(alertGetter,
                        postfix: new HarmonyMethod(typeof(SmallFurnitureResearchBench), nameof(HasRequiredResearchBenchPostfix)));
                else
                    Log.Warning("[Rebalance Patches] Alert_NeedResearchBench.HasRequiredResearchBench could not "
                        + "be found, so the need-research-bench alert may keep showing at Small Furniture benches.");
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not let Small Furniture research benches stand in "
                    + "for the vanilla bench:\n" + ex);
            }
        }

        /// <summary>Whether <paramref name="benchDef"/> is marked to stand in for <paramref name="requiredBuilding"/>.</summary>
        public static bool BenchCoversBuilding(ThingDef benchDef, ThingDef requiredBuilding)
        {
            if (benchDef == null || requiredBuilding == null)
                return false;
            Build();
            return benchToBuilding.TryGetValue(benchDef, out ThingDef stood) && stood == requiredBuilding;
        }

        private static void CanBeResearchedAtPostfix(ResearchProjectDef __instance,
            Building_ResearchBench bench, bool ignoreResearchBenchPowerStatus, ref bool __result)
        {
            try
            {
                if (__result)
                    return;
                if (!SettingsRegistry.GetEffective(SettingKey))
                    return;
                if (bench == null || __instance.requiredResearchBuilding == null)
                    return;
                if (!BenchCoversBuilding(bench.def, __instance.requiredResearchBuilding))
                    return;

                if (!ignoreResearchBenchPowerStatus)
                {
                    CompPowerTrader power = bench.GetComp<CompPowerTrader>();
                    if (power != null && !power.PowerOn)
                        return;
                }

                List<ThingDef> facilities = __instance.requiredResearchFacilities;
                if (facilities != null && facilities.Count > 0)
                {
                    CompAffectedByFacilities affected = bench.TryGetComp<CompAffectedByFacilities>();
                    if (affected == null)
                        return;
                    List<Thing> linked = affected.LinkedFacilitiesListForReading;
                    for (int i = 0; i < facilities.Count; i++)
                        if (!FacilityPresent(facilities[i], linked, affected))
                            return;
                }

                __result = true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Small Furniture research bench stand-in threw:\n" + ex);
            }
        }

        private static void HasRequiredResearchBenchPostfix(ref bool __result)
        {
            try
            {
                if (__result)
                    return;
                if (!SettingsRegistry.GetEffective(SettingKey))
                    return;
                ResearchProjectDef project = Find.ResearchManager?.GetProject();
                ThingDef required = project?.requiredResearchBuilding;
                if (required == null)
                    return;

                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    List<Building> buildings = maps[i].listerBuildings.allBuildingsColonist;
                    for (int j = 0; j < buildings.Count; j++)
                    {
                        if (buildings[j] is Building_ResearchBench && BenchCoversBuilding(buildings[j].def, required))
                        {
                            __result = true;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Small Furniture research bench alert override threw:\n" + ex);
            }
        }

        private static bool FacilityPresent(ThingDef required, List<Thing> linked, CompAffectedByFacilities affected)
        {
            Build();
            for (int i = 0; i < linked.Count; i++)
            {
                Thing x = linked[i];
                if (!affected.IsFacilityActive(x))
                    continue;
                if (x.def == required)
                    return true;
                if (facilityToFacility.TryGetValue(x.def, out ThingDef stood) && stood == required)
                    return true;
            }
            return false;
        }

        private static void Build()
        {
            if (built)
                return;
            built = true;
            benchToBuilding = new Dictionary<ThingDef, ThingDef>();
            facilityToFacility = new Dictionary<ThingDef, ThingDef>();

            Type extType = GenTypes.GetTypeInAnyAssembly(ExtensionTypeName);
            if (extType == null)
                return;
            FieldInfo benchField = AccessTools.Field(extType, "altWorkbenchDef");
            FieldInfo facilityField = AccessTools.Field(extType, "altLinkFacilityDef");
            if (benchField == null && facilityField == null)
                return;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.modExtensions == null)
                    continue;
                for (int i = 0; i < def.modExtensions.Count; i++)
                {
                    DefModExtension ext = def.modExtensions[i];
                    if (ext == null || ext.GetType() != extType)
                        continue;
                    if (benchField?.GetValue(ext) is ThingDef altBench)
                        benchToBuilding[def] = altBench;
                    if (facilityField?.GetValue(ext) is ThingDef altFacility)
                        facilityToFacility[def] = altFacility;
                }
            }
        }
    }
}
