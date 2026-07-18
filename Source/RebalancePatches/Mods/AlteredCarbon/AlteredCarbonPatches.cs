using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.AlteredCarbon
{
    internal static class AlteredCarbonPatches
    {
        private static FieldInfo tunedRelaysField;
        private static PropertyInfo poweredProperty;
        private static bool warned;

        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IsActive("hlx.UltratechAlteredCarbon"))
                return;
            try
            {
                Type matrixType = AccessTools.TypeByName("AlteredCarbon.Building_NeuralMatrix");
                MethodInfo target = AccessTools.Method(matrixType, "NeedleCastRange");
                tunedRelaysField = AccessTools.Field(matrixType, "tunedCastingRelays");
                poweredProperty = AccessTools.Property(matrixType, "Powered");
                if (target == null || tunedRelaysField == null || poweredProperty == null)
                    throw new MissingMemberException("Building_NeuralMatrix members not found");
                harmony.Patch(target,
                    postfix: new HarmonyMethod(typeof(AlteredCarbonPatches), nameof(NeedleCastRangePostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch Altered Carbon's needlecast range:\n{ex}");
            }
        }

        private static void NeedleCastRangePostfix(object __instance, ref float __result)
        {
            try
            {
                float range = 1f;
                if ((bool)poweredProperty.GetValue(__instance, null))
                {
                    var relays = tunedRelaysField.GetValue(__instance) as IEnumerable;
                    if (relays != null)
                        foreach (object obj in relays)
                        {
                            if (!(obj is Thing relay))
                                continue;
                            CompPowerTrader power = relay.TryGetComp<CompPowerTrader>();
                            if (power == null || power.PowerOn)
                                range += relay.def.GetModExtension<CastingRelayRangeExtension>()?.tilesPerRelay ?? 5;
                        }
                }
                __result = range;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning($"[Rebalance Patches] Needlecast range postfix failed, keeping Altered Carbon's value:\n{ex}");
                }
            }
        }
    }
}
