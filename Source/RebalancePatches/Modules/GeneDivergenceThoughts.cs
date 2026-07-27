using RimWorld;
using Verse;

namespace RebalancePatches
{
    public class GeneDivergenceThoughtExtension : DefModExtension
    {
        // Divergence points that advance the situational thought by one stage.
        public float pointsPerStage = 5f;
    }

    public static class GeneDivergenceThought
    {
        public const float MinPoints = 0.0001f;

        public static float PointsFor(Pawn p) => HediffComp_GeneDivergence.DivergencePointsFor(p);

        public static bool IsDiverged(Pawn p) => PointsFor(p) > MinPoints;

        public static bool IsPristine(Pawn p)
        {
            if (p?.genes == null)
                return false;
            if (HediffComp_GeneDivergence.IsSyntheticBody(p))
                return false;
            return PointsFor(p) <= MinPoints;
        }

        public static int StageFor(float points, float pointsPerStage, int stageCount)
        {
            if (stageCount <= 1)
                return 0;
            if (pointsPerStage <= 0f)
                return 0;
            int stage = (int)(points / pointsPerStage);
            if (stage < 0) stage = 0;
            if (stage > stageCount - 1) stage = stageCount - 1;
            return stage;
        }
    }

    // Active while the pawn has diverged from its xenotype baseline; the stage rises with divergence.
    // A positive-mood ThoughtDef makes this a reward (the reshaped body), a negative one a penalty
    // (the defiled body) - the worker only measures, the def decides the sign.
    public class ThoughtWorker_Precept_GeneDivergence : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            float points = GeneDivergenceThought.PointsFor(p);
            if (points <= GeneDivergenceThought.MinPoints)
                return ThoughtState.Inactive;
            GeneDivergenceThoughtExtension ext = def.GetModExtension<GeneDivergenceThoughtExtension>();
            float per = ext != null ? ext.pointsPerStage : 5f;
            return ThoughtState.ActiveAtStage(GeneDivergenceThought.StageFor(points, per, def.stages.Count));
        }
    }

    // Active while the pawn is genetically pristine - it still matches its own xenotype template.
    public class ThoughtWorker_Precept_NoGeneDivergence : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            return GeneDivergenceThought.IsPristine(p) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    // Opinion of another pawn that has been engineered away from its baseline.
    public class ThoughtWorker_Precept_GeneDivergence_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return GeneDivergenceThought.IsDiverged(otherPawn) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    // Opinion of another pawn that is still genetically pristine.
    public class ThoughtWorker_Precept_NoGeneDivergence_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return GeneDivergenceThought.IsPristine(otherPawn) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }
}
