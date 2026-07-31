using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.VPE
{
    internal static class VpeWearGatePatches
    {
        private const string VpeModId = "vanillaexpanded.vpsycastse";

        private static bool warned;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                if (!ModsConfig.IsActive(VpeModId))
                    return;

                MethodInfo canEquip = AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip),
                    new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) });
                MethodInfo scoreGain = AccessTools.Method(typeof(JobGiver_OptimizeApparel),
                    nameof(JobGiver_OptimizeApparel.ApparelScoreGain));
                if (canEquip == null || scoreGain == null)
                    return;

                harmony.Patch(canEquip, postfix: new HarmonyMethod(typeof(VpeWearGatePatches), nameof(RefuseUnattuned)));
                harmony.Patch(scoreGain, postfix: new HarmonyMethod(typeof(VpeWearGatePatches), nameof(NeverAutoWear)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not gate psychic vestments on sensitivity:\n" + ex);
            }
        }

        private static float Required(Thing thing) =>
            thing?.def?.GetModExtension<RequiredPsychicSensitivityExtension>()?.minSensitivity ?? 0f;

        private static bool Gated(Thing thing, Pawn pawn)
        {
            float min = Required(thing);
            return min > 0f && pawn != null
                && pawn.GetStatValue(StatDefOf.PsychicSensitivity) < min;
        }

        private static void RefuseUnattuned(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            try
            {
                if (!__result || !Gated(thing, pawn))
                    return;
                __result = false;
                cantReason = "RBP.WearGate".Translate(Required(thing).ToStringPercent());
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("[Rebalance Patches] Vestment wear gate failed; wearing is unrestricted:\n" + ex);
                }
            }
        }

        private static void NeverAutoWear(Pawn pawn, Apparel ap, ref float __result)
        {
            try
            {
                if (__result > 0f && Gated(ap, pawn))
                    __result = -50f;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("[Rebalance Patches] Vestment wear gate failed; wearing is unrestricted:\n" + ex);
                }
            }
        }
    }
}
