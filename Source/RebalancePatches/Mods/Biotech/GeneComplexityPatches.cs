using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.Biotech
{
    internal static class GeneComplexityPatches
    {
        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.BiotechActive || !SettingsRegistry.GetEffective("vanilla.genecomplexitybase"))
                return;
            try
            {
                harmony.Patch(AccessTools.Method(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.MaxComplexity)),
                    postfix: new HarmonyMethod(typeof(GeneComplexityPatches), nameof(MaxComplexityPostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch gene assembler complexity:\n{ex}");
            }
        }

        private static void MaxComplexityPostfix(ref int __result)
        {
            __result += SettingsRegistry.GetEffectiveValue("vanilla.genecomplexitybase");
        }
    }
}
