using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    public class InspirationNullifyingExtension : DefModExtension
    {
        public List<PreceptDef> nullifyingPrecepts;
    }
}

namespace RebalancePatches.Mods.Inspirations
{
    internal static class InspirationNullifyPatches
    {
        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IdeologyActive || !SettingsRegistry.GetEffective("memes.inspirations"))
                return;
            try
            {
                harmony.Patch(AccessTools.Method(typeof(InspirationWorker), nameof(InspirationWorker.InspirationCanOccur)),
                    postfix: new HarmonyMethod(typeof(InspirationNullifyPatches), nameof(InspirationCanOccurPostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch inspiration precept gating:\n{ex}");
            }
        }

        private static void InspirationCanOccurPostfix(InspirationWorker __instance, Pawn pawn, ref bool __result)
        {
            if (!__result)
                return;
            InspirationNullifyingExtension extension = __instance.def.GetModExtension<InspirationNullifyingExtension>();
            if (extension?.nullifyingPrecepts == null)
                return;
            Ideo ideo = pawn?.ideo?.Ideo;
            if (ideo == null)
                return;
            foreach (PreceptDef precept in extension.nullifyingPrecepts)
            {
                if (precept != null && ideo.HasPrecept(precept))
                {
                    __result = false;
                    return;
                }
            }
        }
    }
}
