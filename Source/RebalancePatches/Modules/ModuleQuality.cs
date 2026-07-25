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

        internal static float FactorOf(QualityCategory quality) => QualityScaling.FactorOf(quality);

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
            // A banded payload has one stage per quality; the default listing would show all of them at
            // once. Deliberately replace it with just the base (stage 0) numbers so the info card reads
            // as a single implant rather than a stack of quality bands.
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
                HediffStage band = QualityScaling.CloneStage(original);
                band.minSeverity = SeverityFor(quality);
                if (quality != QualityCategory.Normal)
                    band.label = quality.GetLabel();
                QualityScaling.Scale(band, QualityScaling.FactorOf(quality));
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
