using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.CyberneticsResearch
{
    /// <summary>Maps retired defNames of ours to their replacements when a save references them.
    /// RBP_CybCivisPX7 became RBP_CybCivisFlagship: the trailing digit made the game reject its
    /// generated techprint ThingDef's name.</summary>
    internal static class DefRenamePatches
    {
        public static void TryApply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(BackCompatibility), nameof(BackCompatibility.BackCompatibleDefName)),
                    postfix: new HarmonyMethod(typeof(DefRenamePatches), nameof(MapRenamedDefs)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not map renamed defs for old saves:\n" + ex);
            }
        }

        private static void MapRenamedDefs(Type defType, string defName, ref string __result)
        {
            try
            {
                if (__result != defName)
                    return;
                if (defType == typeof(ResearchProjectDef) && defName == "RBP_CybCivisPX7")
                    __result = "RBP_CybCivisFlagship";
                else if (defType == typeof(ThingDef) && defName == "Techprint_RBP_CybCivisPX7")
                    __result = "Techprint_RBP_CybCivisFlagship";
            }
            catch
            {
            }
        }
    }
}
