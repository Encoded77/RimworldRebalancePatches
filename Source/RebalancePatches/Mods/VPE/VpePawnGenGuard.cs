using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.VPE
{
    internal static class VpePawnGenGuard
    {
        private const string VpeModId = "vanillaexpanded.vpsycastse";
        private const string SettingKey = "vpe.pawngenguard";

        private static Type kindExtensionType;
        private static MethodInfo canPawnUnlock;
        private static IList pathDefs;
        private static bool warned;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                if (!ModsConfig.IsActive(VpeModId) || !SettingsRegistry.GetEffective(SettingKey))
                    return;

                Type patchType = AccessTools.TypeByName("VanillaPsycastsExpanded.PawnGen_Patch");
                Type pathType = AccessTools.TypeByName("VanillaPsycastsExpanded.PsycasterPathDef");
                MethodInfo target = patchType == null ? null : AccessTools.DeclaredMethod(patchType, "Postfix");
                if (target == null || pathType == null)
                    return;

                canPawnUnlock = AccessTools.Method(pathType, "CanPawnUnlock", new[] { typeof(Pawn) });
                if (canPawnUnlock == null)
                    return;

                kindExtensionType = AccessTools.TypeByName("VanillaPsycastsExpanded.PawnKindAbilityExtension_Psycasts");
                pathDefs = AccessTools.Property(typeof(DefDatabase<>).MakeGenericType(pathType),
                    "AllDefsListForReading")?.GetValue(null) as IList;
                if (pathDefs == null)
                    return;

                harmony.Patch(target, prefix: new HarmonyMethod(typeof(VpePawnGenGuard), nameof(SkipWhenNoPathFits)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not guard psycast pawn generation:\n" + ex);
            }
        }

        private static bool SkipWhenNoPathFits(Pawn __0)
        {
            try
            {
                if (__0 == null || __0.RaceProps == null || __0.RaceProps.intelligence < Intelligence.Humanlike)
                    return true;
                if (HasPsycastKind(__0))
                    return true;
                return AnyPathFits(__0);
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("[Rebalance Patches] Psycast pawn-generation guard failed; leaving generation "
                        + "to Vanilla Psycasts Expanded:\n" + ex);
                }
                return true;
            }
        }

        private static bool HasPsycastKind(Pawn pawn)
        {
            if (kindExtensionType == null)
                return false;
            List<DefModExtension> extensions = pawn.kindDef?.modExtensions;
            if (extensions == null)
                return false;
            for (int i = 0; i < extensions.Count; i++)
                if (kindExtensionType.IsInstanceOfType(extensions[i]))
                    return true;
            return false;
        }

        private static bool AnyPathFits(Pawn pawn)
        {
            var args = new object[] { pawn };
            for (int i = 0; i < pathDefs.Count; i++)
            {
                object path = pathDefs[i];
                if (path != null && canPawnUnlock.Invoke(path, args) is bool fits && fits)
                    return true;
            }
            return false;
        }
    }
}
