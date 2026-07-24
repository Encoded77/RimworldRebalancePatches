using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches
{
    internal static class ModuleQuality
    {
        private const string SettingKey = "cybernetics.modules";

        private static readonly float[] Factors = { 0.80f, 0.90f, 1.00f, 1.06f, 1.12f, 1.18f, 1.25f };

        private static float SeverityFor(QualityCategory quality) => 0.8f + 0.1f * (int)quality;

        private const float LowestBand = 0.8f;
        private const float HighestBand = 1.4f;

        private static bool applied;
        private static bool warned;

        /// <summary>Payload defs that carry quality bands, so nothing else touches their severity.</summary>
        private static readonly HashSet<HediffDef> banded = new HashSet<HediffDef>();

        internal static bool IsBanded(HediffDef payload) => payload != null && banded.Contains(payload);

        /// <summary>The severity an installed module of this quality gives its payload.</summary>
        internal static float SeverityOf(QualityCategory quality) => SeverityFor(quality);

        internal static float FactorOf(QualityCategory quality) => Factors[(int)quality];

        public static void TryApply(Harmony harmony)
        {
            if (applied)
                return;
            applied = true;
            if (!ModsConfig.IsActive("ebsg.framework") || !ModuleApi.QualityMembersAvailable)
                return;
            if (!SettingsRegistry.GetEffective(SettingKey))
                return;

            try
            {
                BandPayloadStages();
                GiveModulesQuality();
                if (banded.Count == 0)
                    return;

                Type useEffect = AccessTools.TypeByName("EBSGFramework.CompUseEffect_HediffModule");
                MethodInfo target = useEffect == null ? null : AccessTools.Method(useEffect, "Install");
                if (target == null)
                    throw new MissingMemberException("EBSGFramework.CompUseEffect_HediffModule.Install not found");
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(ModuleQuality), nameof(InstallPostfix)));

                harmony.Patch(AccessTools.Method(typeof(HediffDef), nameof(HediffDef.SpecialDisplayStats)),
                    postfix: new HarmonyMethod(typeof(ModuleQuality), nameof(DisplayStatsPostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Module quality could not be wired up; modules will "
                    + "install at their normal strength regardless of quality:\n" + ex);
            }
        }

        internal static void InstallPostfix(object __instance)
        {
            try
            {
                if (!(__instance is ThingComp comp) || comp.parent == null)
                    return;
                CompQuality quality = comp.parent.TryGetComp<CompQuality>();
                if (quality == null)
                    return;

                float severity = SeverityFor(quality.Quality);
                foreach (Hediff payload in ModuleApi.LinkedHediffs(__instance))
                    if (payload != null && banded.Contains(payload.def))
                        payload.Severity = severity;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("[Rebalance Patches] Applying a module's quality to its payload failed:\n" + ex);
                }
            }
        }

        internal static void DisplayStatsPostfix(HediffDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            if (banded.Contains(__instance))
                __result = __instance.stages[0].SpecialDisplayStats();
        }

        private static void BandPayloadStages()
        {
            var seen = new HashSet<HediffDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props == null)
                    continue;
                foreach (HediffDef payload in ModuleApi.PayloadHediffs(props))
                    if (seen.Add(payload) && CanBand(payload))
                        Band(payload);
            }
        }

        private static bool CanBand(HediffDef payload)
        {
            if (payload?.stages == null || payload.stages.Count != 1)
                return false;
            if (payload.stages[0].minSeverity > 0f)
                return false;
            if (payload.lethalSeverity > 0f)
                return false;
            if (payload.minSeverity > LowestBand || payload.maxSeverity < HighestBand)
                return false;
            if (!LandsOnAnUnscaledBand(payload.initialSeverity))
                return false;
            if (HasSeverityDrivenComp(payload))
                return false;
            return HasScalableNumbers(payload.stages[0]);
        }

        private static bool HasSeverityDrivenComp(HediffDef payload)
        {
            if (payload.comps == null)
                return false;
            foreach (HediffCompProperties props in payload.comps)
                if (props is HediffCompProperties_SeverityPerDay
                    || props is HediffCompProperties_Disappears
                    || props is HediffCompProperties_Immunizable
                    || props is HediffCompProperties_TendDuration
                    || props is HediffCompProperties_SeverityFromHemogen
                    || props is HediffCompProperties_SeverityFromGasDensityDirect
                    || props is HediffCompProperties_ChangeImplantLevel
                    || props is HediffCompProperties_DamageBrain)
                    return true;
            return false;
        }

        private static bool LandsOnAnUnscaledBand(float severity) =>
            severity < LowestBand
            || (severity >= SeverityFor(QualityCategory.Normal) && severity < SeverityFor(QualityCategory.Good));

        private static void Band(HediffDef payload)
        {
            HediffStage original = payload.stages[0];
            var stages = new List<HediffStage> { original };
            foreach (QualityCategory quality in Enum.GetValues(typeof(QualityCategory)))
            {
                HediffStage band = CloneStage(original);
                band.minSeverity = SeverityFor(quality);
                if (quality != QualityCategory.Normal)
                    band.label = quality.GetLabel();
                Scale(band, Factors[(int)quality]);
                stages.Add(band);
            }
            payload.stages = stages;
            banded.Add(payload);
        }

        /// <summary>Whether a stage carries anything quality could plausibly move.</summary>
        private static bool HasScalableNumbers(HediffStage stage) =>
            (stage.statOffsets != null && stage.statOffsets.Count > 0)
            || (stage.statFactors != null && stage.statFactors.Count > 0)
            || (stage.capMods != null && stage.capMods.Count > 0)
            || stage.partEfficiencyOffset != 0f;

        private static void Scale(HediffStage stage, float factor)
        {
            if (factor == 1f)
                return;

            if (stage.statOffsets != null)
                foreach (StatModifier offset in stage.statOffsets)
                    offset.value *= factor;

            if (stage.statFactors != null)
                foreach (StatModifier statFactor in stage.statFactors)
                    statFactor.value = Mathf.Max(0f, 1f + (statFactor.value - 1f) * factor);

            if (stage.capMods != null)
                foreach (PawnCapacityModifier capMod in stage.capMods)
                {
                    capMod.offset = Toward(capMod.offset, factor);
                    capMod.postFactor = Mathf.Max(0f, 1f + Toward(capMod.postFactor - 1f, factor));
                }

            stage.partEfficiencyOffset = Toward(stage.partEfficiencyOffset, factor);
        }

        private static float Toward(float value, float factor)
        {
            if (value == 0f)
                return value;
            return value > 0f ? value * factor : value / factor;
        }

        private static HediffStage CloneStage(HediffStage source)
        {
            var clone = new HediffStage();
            foreach (FieldInfo field in typeof(HediffStage).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!field.IsInitOnly && !field.IsLiteral)
                    field.SetValue(clone, field.GetValue(source));
            clone.statOffsets = CopyStats(source.statOffsets);
            clone.statFactors = CopyStats(source.statFactors);
            clone.capMods = CopyCapMods(source.capMods);
            return clone;
        }

        private static List<StatModifier> CopyStats(List<StatModifier> source)
        {
            if (source == null)
                return null;
            var copy = new List<StatModifier>(source.Count);
            foreach (StatModifier modifier in source)
                copy.Add(new StatModifier { stat = modifier.stat, value = modifier.value });
            return copy;
        }

        private static List<PawnCapacityModifier> CopyCapMods(List<PawnCapacityModifier> source)
        {
            if (source == null)
                return null;
            var copy = new List<PawnCapacityModifier>(source.Count);
            foreach (PawnCapacityModifier modifier in source)
                copy.Add(new PawnCapacityModifier
                {
                    capacity = modifier.capacity,
                    offset = modifier.offset,
                    setMax = modifier.setMax,
                    postFactor = modifier.postFactor,
                    statFactorMod = modifier.statFactorMod,
                    setMaxCurveOverride = modifier.setMaxCurveOverride,
                    setMaxCurveEvaluateStat = modifier.setMaxCurveEvaluateStat,
                });
            return copy;
        }

        private static void GiveModulesQuality()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props == null || !ScalesWithQuality(props))
                    continue;
                if (def.comps == null)
                    def.comps = new List<CompProperties>();
                if (def.HasComp(typeof(CompQuality)))
                    continue;
                def.comps.Add(new CompProperties { compClass = typeof(CompQuality) });
            }
        }

        private static bool ScalesWithQuality(object moduleProps)
        {
            foreach (HediffDef payload in ModuleApi.PayloadHediffs(moduleProps))
                if (banded.Contains(payload))
                    return true;
            return false;
        }
    }
}
