using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class QualityScalingTests
    {
        private static HediffStage StageWith(StatDef stat, float value) =>
            new HediffStage { statOffsets = new List<StatModifier> { new StatModifier { stat = stat, value = value } } };

        [Test]
        public static void BenefitGrowsWithQuality()
        {
            var good = new StatDef { defName = "PsychicSensitivity" };
            HediffStage stage = StageWith(good, 0.20f);
            QualityScaling.Scale(stage, QualityScaling.FactorOf(QualityCategory.Legendary));
            Check.True(stage.statOffsets[0].value > 0.20f,
                "a higher-is-better offset should grow at legendary quality");
        }

        [Test]
        public static void LowerIsBetterDrawbackEasesWithQuality()
        {
            var bad = new StatDef { defName = "MentalBreakThreshold" };

            HediffStage best = StageWith(bad, 0.15f);
            QualityScaling.Scale(best, QualityScaling.FactorOf(QualityCategory.Legendary));
            Check.True(best.statOffsets[0].value < 0.15f,
                "a positive offset on a lower-is-better stat (e.g. mental break threshold) should ease at legendary quality");

            HediffStage worst = StageWith(bad, 0.15f);
            QualityScaling.Scale(worst, QualityScaling.FactorOf(QualityCategory.Awful));
            Check.True(worst.statOffsets[0].value > 0.15f,
                "the same drawback should worsen at awful quality");
        }

        [Test]
        public static void ScaledStagePreservesSourceAndAdaptationFields()
        {
            var good = new StatDef { defName = "PsychicSensitivity" };
            HediffStage source = StageWith(good, 0.10f);
            source.painFactor = 1.3f;

            HediffStage scaled = QualityScaling.ScaledStage(source, QualityCategory.Legendary);
            Check.True(scaled != source, "a non-normal quality returns a distinct scaled stage");
            Check.True(Mathf.Approximately(scaled.painFactor, 1.3f),
                "painFactor (an adaptation cost, not a stat number) must not scale");
            Check.True(Mathf.Approximately(source.statOffsets[0].value, 0.10f),
                "ScaledStage must not mutate the source stage");
        }

        [Test]
        public static void NormalQualityReturnsSourceUnchanged()
        {
            var stage = new HediffStage();
            Check.True(QualityScaling.ScaledStage(stage, QualityCategory.Normal) == stage,
                "normal quality returns the source stage as-is (no needless clone)");
        }
    }
}
