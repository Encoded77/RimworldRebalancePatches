using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>An implant hediff whose stage effects scale with the quality of the item it was installed
    /// from. Only the stat/capacity numbers scale (via <see cref="QualityScaling"/>); anything else on a
    /// stage — an adaptation's pain or hunger penalty, for instance — is left exactly as authored.</summary>
    public class Hediff_QualityImplant : Hediff_Implant
    {
        private QualityCategory quality = QualityCategory.Normal;

        // (defName, stageIndex, quality) -> scaled stage. Scaled stages depend only on these, so one
        // shared cache serves every pawn; entries are never mutated after they are built.
        private static readonly Dictionary<string, HediffStage> scaledStages = new Dictionary<string, HediffStage>();

        public QualityCategory Quality => quality;

        public void SetQuality(QualityCategory value) => quality = value;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref quality, "RBP_quality", QualityCategory.Normal);
        }

        public override HediffStage CurStage
        {
            get
            {
                HediffStage baseStage = base.CurStage;
                if (quality == QualityCategory.Normal || baseStage == null || def == null)
                    return baseStage;

                string key = def.defName + "|" + CurStageIndex + "|" + (int)quality;
                if (!scaledStages.TryGetValue(key, out HediffStage scaled))
                {
                    scaled = QualityScaling.ScaledStage(baseStage, quality);
                    scaledStages[key] = scaled;
                }
                return scaled;
            }
        }

        public override string LabelInBrackets
        {
            get
            {
                string baseLabel = base.LabelInBrackets;
                string qualityLabel = quality.GetLabel();
                return baseLabel.NullOrEmpty() ? qualityLabel : baseLabel + ", " + qualityLabel;
            }
        }
    }
}
