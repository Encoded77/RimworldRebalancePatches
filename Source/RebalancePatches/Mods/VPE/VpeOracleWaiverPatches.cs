using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RebalancePatches.Mods.VPE
{
    internal static class VpeOracleWaiverPatches
    {
        private const string VpeModId = "vanillaexpanded.vpsycastse";
        private const string SettingKey = "psycast.oraclewaiver";
        private const string OracleHediff = "RBP_EchoOracleHediff";

        [ThreadStatic] private static bool raw;
        private static bool warned;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                if (!ModsConfig.IsActive(VpeModId) || !SettingsRegistry.GetEffective(SettingKey))
                    return;

                Type pathType = AccessTools.TypeByName("VanillaPsycastsExpanded.PsycasterPathDef");
                Type implantType = AccessTools.TypeByName("VanillaPsycastsExpanded.Hediff_PsycastAbilities");
                MethodInfo canUnlock = pathType == null ? null
                    : AccessTools.Method(pathType, "CanPawnUnlock", new[] { typeof(Pawn) });
                MethodInfo unlockPath = implantType == null ? null
                    : AccessTools.Method(implantType, "UnlockPath", new[] { pathType });
                if (canUnlock == null || unlockPath == null)
                    return;

                harmony.Patch(canUnlock, postfix: new HarmonyMethod(typeof(VpeOracleWaiverPatches), nameof(WaiveForOracle)));
                harmony.Patch(unlockPath, postfix: new HarmonyMethod(typeof(VpeOracleWaiverPatches), nameof(RecordWaiver)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not attach the ORACLE path waiver:\n" + ex);
            }
        }

        private static OracleWaiverComp Waiver(Pawn pawn)
        {
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null)
                return null;
            for (int i = 0; i < hediffs.Count; i++)
                if (hediffs[i].def.defName == OracleHediff)
                    return hediffs[i].TryGetComp<OracleWaiverComp>();
            return null;
        }

        private static void WaiveForOracle(object __instance, Pawn pawn, ref bool __result)
        {
            try
            {
                if (raw || __result || pawn == null || !(__instance is Def def))
                    return;
                OracleWaiverComp waiver = Waiver(pawn);
                if (waiver == null)
                    return;
                if (waiver.waivedPath == null || waiver.waivedPath == def.defName)
                    __result = true;
            }
            catch (Exception ex)
            {
                WarnOnce(ex);
            }
        }

        private static void RecordWaiver(Hediff __instance, Def path)
        {
            try
            {
                Pawn pawn = __instance?.pawn;
                if (pawn == null || path == null)
                    return;
                OracleWaiverComp waiver = Waiver(pawn);
                if (waiver == null || waiver.waivedPath != null)
                    return;

                // Only a path the pawn could not have taken on its own consumes the waiver.
                bool allowed;
                raw = true;
                try
                {
                    allowed = Traverse.Create(path).Method("CanPawnUnlock", pawn).GetValue<bool>();
                }
                finally
                {
                    raw = false;
                }
                if (!allowed)
                    waiver.waivedPath = path.defName;
            }
            catch (Exception ex)
            {
                WarnOnce(ex);
            }
        }

        private static void WarnOnce(Exception ex)
        {
            if (warned)
                return;
            warned = true;
            Log.Warning("[Rebalance Patches] ORACLE path waiver failed; path unlocks follow their own rules:\n" + ex);
        }
    }
}
