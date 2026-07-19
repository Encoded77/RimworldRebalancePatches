using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches.Mods.IntegratedImplants
{
    internal static class IntegratedImplantsPatches
    {
        private static HediffDef levitator;
        private static HediffDef gravlifter;
        private static HediffDef increasedRange;
        private static HediffDef decreasedRange;
        private static StatDef rangeOffset;
        private static bool warned;

        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IsActive("lts.i"))
                return;
            if (SettingsRegistry.GetEffective("implants.waterpathing"))
                TryApplyWaterPathing(harmony);
            if (ModsConfig.IsActive("sarg.alphagenes") && SettingsRegistry.GetEffective("implants.boosterrange"))
                TryApplyBoosterRange(harmony);
        }

        private static void TryApplyWaterPathing(Harmony harmony)
        {
            try
            {
                levitator = DefDatabase<HediffDef>.GetNamedSilentFail("PsychicLevitator");
                gravlifter = DefDatabase<HediffDef>.GetNamedSilentFail("LTS_Gravlifter");
                if (levitator == null && gravlifter == null)
                    throw new MissingMemberException("neither PsychicLevitator nor LTS_Gravlifter hediff found");
                harmony.Patch(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.WaterCellCost)),
                    postfix: new HarmonyMethod(typeof(IntegratedImplantsPatches), nameof(WaterCellCostPostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch water pathing for Integrated Implants:\n{ex}");
            }
        }

        private static void WaterCellCostPostfix(Pawn __instance, ref int? __result)
        {
            HediffSet set = __instance?.health?.hediffSet;
            if (set == null)
                return;
            if ((levitator != null && set.HasHediff(levitator)) || (gravlifter != null && set.HasHediff(gravlifter)))
                __result = 1;
        }

        private static void TryApplyBoosterRange(Harmony harmony)
        {
            try
            {
                increasedRange = DefDatabase<HediffDef>.GetNamedSilentFail("AG_IncreasedCommandRange");
                decreasedRange = DefDatabase<HediffDef>.GetNamedSilentFail("AG_DecreasedCommandRange");
                rangeOffset = DefDatabase<StatDef>.GetNamedSilentFail("MechRemoteControlDistanceOffset");
                if ((increasedRange == null && decreasedRange == null) || rangeOffset == null)
                    throw new MissingMemberException("AG command range hediffs or MechRemoteControlDistanceOffset stat not found");
                harmony.Patch(AccessTools.Method(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanCommandTo)),
                    postfix: new HarmonyMethod(typeof(IntegratedImplantsPatches), nameof(CanCommandToPostfix)) { priority = Priority.Last });
                harmony.Patch(AccessTools.Method(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.DrawCommandRadius)),
                    postfix: new HarmonyMethod(typeof(IntegratedImplantsPatches), nameof(DrawCommandRadiusPostfix)) { priority = Priority.Last });
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch signal booster stacking with Alpha Genes command range:\n{ex}");
            }
        }

        private static float? GeneRangeRadius(Pawn pawn)
        {
            HediffSet set = pawn?.health?.hediffSet;
            if (set == null)
                return null;
            if (increasedRange != null && set.HasHediff(increasedRange))
                return 35f;
            if (decreasedRange != null && set.HasHediff(decreasedRange))
                return 15f;
            return null;
        }

        private static void CanCommandToPostfix(Pawn_MechanitorTracker __instance, LocalTargetInfo target, ref bool __result)
        {
            try
            {
                Pawn pawn = __instance.Pawn;
                float? geneRadius = GeneRangeRadius(pawn);
                if (geneRadius == null)
                    return;
                float radius = geneRadius.Value + pawn.GetStatValue(rangeOffset);
                __result = target.Cell.InBounds(pawn.MapHeld)
                    && pawn.Position.DistanceToSquared(target.Cell) < radius * radius;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning($"[Rebalance Patches] Booster command range postfix failed, keeping other mods' value:\n{ex}");
                }
            }
        }

        private static void DrawCommandRadiusPostfix(Pawn_MechanitorTracker __instance)
        {
            try
            {
                Pawn pawn = __instance.Pawn;
                if (!pawn.Spawned || !__instance.AnySelectedDraftedMechs)
                    return;
                float? geneRadius = GeneRangeRadius(pawn);
                if (geneRadius == null)
                    return;
                float offset = pawn.GetStatValue(rangeOffset);
                if (offset == 0f)
                    return;
                GenDraw.DrawRadiusRing(pawn.Position, geneRadius.Value + offset, Color.cyan,
                    c => pawn.mechanitor.CanCommandTo(c));
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning($"[Rebalance Patches] Booster command radius ring failed:\n{ex}");
                }
            }
        }
    }
}
