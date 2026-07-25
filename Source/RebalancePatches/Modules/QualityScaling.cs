using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches
{
    /// <summary>Shared quality-to-effect scaling for implants. Modules band a payload's stages by this;
    /// cyberbrains scale their live stage by it. A benefit grows with quality and shrinks with it, and a
    /// drawback eases at higher quality instead of worsening (see <see cref="Toward"/>).</summary>
    internal static class QualityScaling
    {
        /// <summary>Multiplier per quality, Awful..Legendary. Index by (int)QualityCategory.</summary>
        internal static readonly float[] Factors = { 0.80f, 0.90f, 1.00f, 1.06f, 1.12f, 1.18f, 1.25f };

        internal static float FactorOf(QualityCategory quality) => Factors[(int)quality];

        /// <summary>A stage scaled to a quality, or the source unchanged at Normal. Callers may cache.</summary>
        internal static HediffStage ScaledStage(HediffStage source, QualityCategory quality)
        {
            float factor = FactorOf(quality);
            if (source == null || factor == 1f)
                return source;
            HediffStage clone = CloneStage(source);
            Scale(clone, factor);
            return clone;
        }

        internal static void Scale(HediffStage stage, float factor)
        {
            if (factor == 1f)
                return;

            // For a stat, "better" is not always "more": mental break threshold and pain read the other
            // way, so scale toward each stat's good direction rather than assuming positive is good.
            if (stage.statOffsets != null)
                foreach (StatModifier offset in stage.statOffsets)
                    offset.value = TowardGood(offset.value, factor, offset.stat);

            if (stage.statFactors != null)
                foreach (StatModifier statFactor in stage.statFactors)
                    statFactor.value = Mathf.Max(0f, 1f + TowardGood(statFactor.value - 1f, factor, statFactor.stat));

            // Pawn capacities are always higher-is-better, so a positive offset is always the benefit.
            if (stage.capMods != null)
                foreach (PawnCapacityModifier capMod in stage.capMods)
                {
                    capMod.offset = Toward(capMod.offset, factor);
                    capMod.postFactor = Mathf.Max(0f, 1f + Toward(capMod.postFactor - 1f, factor));
                }

            stage.partEfficiencyOffset = Toward(stage.partEfficiencyOffset, factor);
        }

        /// <summary>Moves a value away from zero for a benefit, toward zero for a drawback, where a
        /// positive value is the benefit.</summary>
        internal static float Toward(float value, float factor)
        {
            if (value == 0f)
                return value;
            return value > 0f ? value * factor : value / factor;
        }

        /// <summary>Stats where a positive offset is the drawback (lower is better). RimWorld exposes no
        /// direction flag, so we name the lower-is-better stats that implant content actually uses; any
        /// other stat is treated as higher-is-better, which is the common case. Extend as content needs.</summary>
        private static readonly HashSet<string> LowerIsBetterStats = new HashSet<string>
        {
            "MentalBreakThreshold",
            "CertaintyLossFactor",
            "FoodPoisonChance",
            "AimingDelayFactor",
        };

        /// <summary>As <see cref="Toward"/>, but the benefit direction follows the stat: for a lower-is-better
        /// stat a positive offset is the drawback, so it eases with quality instead of growing (and a negative
        /// offset, which is the benefit there, grows).</summary>
        private static float TowardGood(float value, float factor, StatDef stat)
        {
            bool goodIsPositive = stat == null || !LowerIsBetterStats.Contains(stat.defName);
            return Toward(value, goodIsPositive ? factor : 1f / factor);
        }

        internal static HediffStage CloneStage(HediffStage source)
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
    }
}
