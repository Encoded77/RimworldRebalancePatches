using RimWorld;
using Verse;

namespace RebalancePatches
{
    public class StatPart_PsychicDeafnessOverride : StatPart
    {
        /// <summary>The hardware this project ships. Also read by the psylink patches.</summary>
        public const string CounterGeneDefName = "RBP_PsychicResonator";

        /// <summary>The android race's deafness hardware - the thing being countered.</summary>
        public const string DeafnessGeneDefName = "VREA_PsychicallyDeaf";

        public float restoredValue = 1f;

        private static GeneDef counterGene;
        private static GeneDef deafnessGene;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (val < restoredValue && AppliesTo(PawnOf(req)))
                val = restoredValue;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!AppliesTo(PawnOf(req)))
                return null;
            return counterGene.LabelCap + ": " + restoredValue.ToStringPercent();
        }

        public static bool AppliesTo(Pawn pawn)
        {
            if (pawn?.genes == null || !Resolve())
                return false;
            return HasActive(pawn, counterGene) && HasActive(pawn, deafnessGene);
        }

        private static Pawn PawnOf(StatRequest req) => req.HasThing ? req.Thing as Pawn : null;

        private static bool Resolve()
        {
            if (counterGene != null && deafnessGene != null)
                return true;
            counterGene = DefDatabase<GeneDef>.GetNamedSilentFail(CounterGeneDefName);
            deafnessGene = DefDatabase<GeneDef>.GetNamedSilentFail(DeafnessGeneDefName);
            return counterGene != null && deafnessGene != null;
        }

        private static bool HasActive(Pawn pawn, GeneDef def)
        {
            Gene gene = pawn.genes.GetGene(def);
            return gene != null && gene.Active;
        }
    }
}
