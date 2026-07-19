using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace RebalancePatches
{
    public class ArchogenWhitelistDef : Def
    {
        public List<string> whitelistedGenes;
    }
}

namespace RebalancePatches.Mods.VQEAncients
{
    internal static class ArchogenWhitelistPatches
    {
        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IsActive("vanillaquestsexpanded.ancients") || !SettingsRegistry.GetEffective("vqea.injectorwhitelist"))
                return;
            try
            {
                Type utils = AccessTools.TypeByName("VanillaQuestsExpandedAncients.Utils");
                var target = AccessTools.Method(utils, "IsValidGeneForInjection");
                if (target == null)
                    throw new MissingMemberException("VanillaQuestsExpandedAncients.Utils.IsValidGeneForInjection not found");
                harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(ArchogenWhitelistPatches), nameof(IsValidGeneForInjectionPrefix)));
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch VQE Ancients' archogen gene injection:\n{ex}");
            }
        }

        private static bool IsValidGeneForInjectionPrefix(GeneDef geneDef, ref bool __result)
        {
            List<ArchogenWhitelistDef> lists = DefDatabase<ArchogenWhitelistDef>.AllDefsListForReading;
            if (lists.Count == 0)
                return true;
            foreach (ArchogenWhitelistDef list in lists)
            {
                if (list.whitelistedGenes != null && list.whitelistedGenes.Contains(geneDef.defName))
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
    }
}
