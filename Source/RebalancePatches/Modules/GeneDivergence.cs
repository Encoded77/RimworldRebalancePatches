using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    public class HediffCompProperties_GeneDivergence : HediffCompProperties
    {
        /// <summary>Divergence points that buy one full point of severity, i.e. one stage.</summary>
        public float pointsPerTier = 8f;

        public float minSeverity = 1f;

        public float maxSeverity = 5f;

        public int recheckIntervalTicks = 60000;

        public HediffCompProperties_GeneDivergence()
        {
            compClass = typeof(HediffComp_GeneDivergence);
        }
    }

    public class HediffComp_GeneDivergence : HediffComp
    {
        private float cachedPoints;
        private int ticksUntilRecheck;

        public HediffCompProperties_GeneDivergence Props => (HediffCompProperties_GeneDivergence)props;

        public float Points => cachedPoints;

        public override string CompTipStringExtra =>
            "Gene divergence: " + cachedPoints.ToString("0.#") + " (from the baseline for this pawn's xenotype)";

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref cachedPoints, "RBP_geneDivergencePoints", 0f);
            Scribe_Values.Look(ref ticksUntilRecheck, "RBP_geneDivergenceRecheck", 0);
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            Recalculate();
            ticksUntilRecheck = Rand.RangeInclusive(1, Math.Max(1, Props.recheckIntervalTicks));
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            ticksUntilRecheck -= delta;
            if (ticksUntilRecheck > 0)
                return;
            ticksUntilRecheck = Math.Max(1, Props.recheckIntervalTicks);
            Recalculate();
        }

        private void Recalculate()
        {
            cachedPoints = DivergencePointsFor(Pawn);
            float target = SeverityFor(cachedPoints, Props.pointsPerTier, Props.minSeverity, Props.maxSeverity);
            if (Math.Abs(parent.Severity - target) > 0.0001f)
                parent.Severity = target;
        }

        internal static float DivergencePointsFor(Pawn pawn)
        {
            Pawn_GeneTracker genes = pawn?.genes;
            if (genes == null)
                return 0f;
            if (IsSyntheticBody(pawn))
                return 0f;
            return DivergencePoints(GeneDefsOf(genes), genes.Xenotype?.genes);
        }

        internal static float DivergencePoints(List<GeneDef> actual, List<GeneDef> baseline)
        {
            var unmatchedBaseline = new Dictionary<GeneDef, int>();
            if (baseline != null)
                foreach (GeneDef gene in baseline)
                {
                    if (gene == null)
                        continue;
                    unmatchedBaseline.TryGetValue(gene, out int count);
                    unmatchedBaseline[gene] = count + 1;
                }

            float points = 0f;
            if (actual != null)
                foreach (GeneDef gene in actual)
                {
                    if (gene == null)
                        continue;
                    if (unmatchedBaseline.TryGetValue(gene, out int remaining) && remaining > 0)
                    {
                        unmatchedBaseline[gene] = remaining - 1;
                        continue;
                    }
                    points += WeightOf(gene);
                }

            foreach (KeyValuePair<GeneDef, int> leftover in unmatchedBaseline)
                if (leftover.Value > 0)
                    points += leftover.Value * WeightOf(leftover.Key);

            return points;
        }

        private static float WeightOf(GeneDef gene) => 1f + Math.Abs(gene.biostatMet);

        internal static float SeverityFor(float points, float pointsPerTier, float min, float max)
        {
            if (pointsPerTier <= 0f)
                return min;
            float severity = min + points / pointsPerTier;
            if (severity < min) return min;
            if (severity > max) return max;
            return severity;
        }

        private static List<GeneDef> GeneDefsOf(Pawn_GeneTracker genes)
        {
            List<Gene> live = genes.GenesListForReading;
            var defs = new List<GeneDef>(live.Count);
            foreach (Gene gene in live)
                if (gene?.def != null)
                    defs.Add(gene.def);
            return defs;
        }

        private static bool androidCheckResolved;
        private static MethodInfo isAndroidMethod;

        internal static bool IsSyntheticBody(Pawn pawn)
        {
            if (pawn == null)
                return false;
            if (!androidCheckResolved)
            {
                androidCheckResolved = true;
                try
                {
                    Type utils = GenTypes.GetTypeInAnyAssembly("VREAndroids.Utils");
                    MethodInfo method = utils?.GetMethod("IsAndroid",
                        BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Pawn) }, null);
                    if (method != null && method.ReturnType == typeof(bool))
                        isAndroidMethod = method;
                }
                catch (Exception e)
                {
                    Log.Warning("[RebalancePatches] Resolving the android check failed; " +
                                "gene divergence will not exclude synthetic bodies: " + e.Message);
                }
            }
            if (isAndroidMethod == null)
                return false;
            try
            {
                return (bool)isAndroidMethod.Invoke(null, new object[] { pawn });
            }
            catch (Exception e)
            {
                isAndroidMethod = null;
                Log.Warning("[RebalancePatches] The android check threw; gene divergence will no longer " +
                            "exclude synthetic bodies this session: " + e.Message);
                return false;
            }
        }
    }
}
