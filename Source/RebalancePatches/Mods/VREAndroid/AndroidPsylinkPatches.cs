using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RebalancePatches.Mods.VREAndroid
{
    internal static class AndroidPsylinkPatches
    {
        private const string AndroidModId = "vanillaracesexpanded.android";

        [ThreadStatic] private static bool reapplying;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                if (!ModsConfig.IsActive(AndroidModId))
                    return;

                MethodInfo target = AccessTools.Method(
                    typeof(Hediff_Psylink), nameof(Hediff_Psylink.ChangeLevel), new[] { typeof(int) });
                if (target == null)
                    return;

                harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(AndroidPsylinkPatches), nameof(RecordLevel)),
                    postfix: new HarmonyMethod(typeof(AndroidPsylinkPatches), nameof(ApplyIfBlocked)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not keep psylink levels reachable for "
                    + "overridden synthetic bodies:\n" + ex);
            }
        }

        private static void RecordLevel(Hediff_Psylink __instance, out int __state)
        {
            __state = __instance.level;
        }

        private static void ApplyIfBlocked(Hediff_Psylink __instance, int levelOffset, int __state)
        {
            try
            {
                if (reapplying || levelOffset == 0 || __instance.level != __state)
                    return;
                if (!StatPart_PsychicDeafnessOverride.AppliesTo(__instance.pawn))
                    return;

                reapplying = true;
                try
                {
                    __instance.ChangeLevel(levelOffset, sendLetter: true);
                }
                finally
                {
                    reapplying = false;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Restoring a blocked psylink level change failed:\n" + ex);
            }
        }
    }
}
