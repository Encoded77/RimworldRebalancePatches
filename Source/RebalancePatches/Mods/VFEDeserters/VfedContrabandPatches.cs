using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RebalancePatches.Mods.VFEDeserters
{
    internal static class VfedContrabandPatches
    {
        private const string ManagerType = "VFED.ContrabandManager";
        private const string EmpireTagPrefix = "ImplantEmpire";

        private static HashSet<ThingDef> served;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                Type manager = AccessTools.TypeByName(ManagerType);
                if (manager == null)
                    return;

                MethodInfo selector = FindProjectSelector(manager);
                if (selector == null)
                {
                    Log.Warning("[Rebalance Patches] Vanilla Factions Expanded - Deserters is present but its "
                        + "contraband lookup could not be found, so its Empire contraband list will be empty "
                        + "while the cybernetics research overhaul is on.");
                    return;
                }

                harmony.Patch(selector,
                    prefix: new HarmonyMethod(typeof(VfedContrabandPatches), nameof(ProjectUnlocksPrefix)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not keep the deserter contraband list working:\n" + ex);
            }
        }

        private static MethodInfo FindProjectSelector(Type manager)
        {
            foreach (Type nested in manager.GetNestedTypes(AccessTools.all))
                foreach (MethodInfo method in nested.GetMethods(AccessTools.all))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                        continue;
                    if (typeof(IEnumerable<ThingDef>).IsAssignableFrom(method.ReturnType))
                        return method;
                }
            return null;
        }

        private static bool ProjectUnlocksPrefix(string project, ref IEnumerable<ThingDef> __result)
        {
            try
            {
                if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(project) != null)
                    return true;

                if (served == null)
                    served = new HashSet<ThingDef>();

                var unlocks = new List<ThingDef>();
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.techHediffsTags == null || served.Contains(def))
                        continue;
                    foreach (string tag in def.techHediffsTags)
                        if (tag != null && tag.StartsWith(EmpireTagPrefix))
                        {
                            served.Add(def);
                            unlocks.Add(def);
                            break;
                        }
                }

                __result = unlocks;
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Substituting the deserter contraband lookup failed:\n" + ex);
                __result = new List<ThingDef>();
                return false;
            }
        }
    }
}
