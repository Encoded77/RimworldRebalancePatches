using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class IdeologyTests
    {
        private const string ProMeme = "RBP_Genecrafted";
        private const string PuristMeme = "RBP_BloodlinePurity";
        private const string ProPrecept = "RBP_GeneTemplate_Transcend";
        private const string PuristPrecept = "RBP_GeneTemplate_Sacred";
        private const string ExclusionTag = "RBP_GeneticTemplate";

        [Test]
        public static void MemesAreWiredToTheirPreceptsAndExcludeEachOther()
        {
            if (!Check.Ready("ideology.genememes", Ids.Ideology, Ids.Biotech))
                return;

            MemeDef pro = DefDatabase<MemeDef>.GetNamedSilentFail(ProMeme);
            MemeDef purist = DefDatabase<MemeDef>.GetNamedSilentFail(PuristMeme);
            if (!Check.Soft(pro != null && purist != null,
                    $"one of the gene memes is missing ({ProMeme}={pro != null}, {PuristMeme}={purist != null})"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(pro.exclusionTags != null && pro.exclusionTags.Contains(ExclusionTag)
                       && purist.exclusionTags != null && purist.exclusionTags.Contains(ExclusionTag),
                "the two memes must share an exclusion tag so an ideoligion cannot take both at once");

            Check.Soft(RequiresPrecept(pro, ProPrecept),
                $"{ProMeme} does not require {ProPrecept} via requireOne");
            Check.Soft(RequiresPrecept(purist, PuristPrecept),
                $"{PuristMeme} does not require {PuristPrecept} via requireOne");

            // Each meme wears its own shipped icon, not a borrowed vanilla one.
            Check.Soft(pro.iconPath == "UI/Memes/RBP_Genecrafted",
                $"{ProMeme} iconPath is '{pro.iconPath}', expected UI/Memes/RBP_Genecrafted");
            Check.Soft(purist.iconPath == "UI/Memes/RBP_BloodlinePurity",
                $"{PuristMeme} iconPath is '{purist.iconPath}', expected UI/Memes/RBP_BloodlinePurity");

            Check.SoftResult();
        }

        [Test]
        public static void PreceptsCarryTheRightThoughtsAndCrossLinkTheMemes()
        {
            if (!Check.Ready("ideology.genememes", Ids.Ideology, Ids.Biotech))
                return;

            PreceptDef pro = DefDatabase<PreceptDef>.GetNamedSilentFail(ProPrecept);
            PreceptDef purist = DefDatabase<PreceptDef>.GetNamedSilentFail(PuristPrecept);
            if (!Check.Soft(pro != null && purist != null, "one of the gene precepts is missing"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(pro.issue?.defName == "RBP_GeneticTemplate" && purist.issue?.defName == "RBP_GeneticTemplate",
                "both precepts must sit on the RBP_GeneticTemplate issue");

            Check.Soft(HasMeme(pro.associatedMemes, ProMeme) && HasMeme(pro.conflictingMemes, PuristMeme),
                $"{ProPrecept} must be associated with {ProMeme} and conflict with {PuristMeme}");
            Check.Soft(HasMeme(purist.associatedMemes, PuristMeme) && HasMeme(purist.conflictingMemes, ProMeme),
                $"{PuristPrecept} must be associated with {PuristMeme} and conflict with {ProMeme}");

            List<string> proThoughts = SituationalThoughts(pro);
            List<string> puristThoughts = SituationalThoughts(purist);
            Check.Note("Genecrafted thoughts: " + string.Join(", ", proThoughts.ToArray()));
            Check.Note("Bloodline Purity thoughts: " + string.Join(", ", puristThoughts.ToArray()));

            foreach (string t in new[] { "RBP_GeneDiverged_Approved", "RBP_GeneBaseline_Disapproved", "RBP_GeneBaseline_Disapproved_Social" })
                Check.Soft(proThoughts.Contains(t), $"{ProPrecept} is missing situational thought {t}");
            foreach (string t in new[] { "RBP_GeneDiverged_Disapproved", "RBP_GeneDiverged_Disapproved_Social" })
                Check.Soft(puristThoughts.Contains(t), $"{PuristPrecept} is missing situational thought {t}");

            Check.SoftResult();
        }

        [Test]
        public static void ThoughtsUseOurWorkersAndPointTheRightWay()
        {
            if (!Check.Ready("ideology.genememes", Ids.Ideology, Ids.Biotech))
                return;

            // Diverged self thoughts: our scaling worker, an extension, more than one stage to climb.
            foreach (var pair in new[]
            {
                new { name = "RBP_GeneDiverged_Approved", sign = 1 },
                new { name = "RBP_GeneDiverged_Disapproved", sign = -1 },
            })
            {
                ThoughtDef t = DefDatabase<ThoughtDef>.GetNamedSilentFail(pair.name);
                if (!Check.Soft(t != null, $"{pair.name} not found"))
                    continue;
                Check.Soft(t.workerClass == typeof(ThoughtWorker_Precept_GeneDivergence),
                    $"{pair.name} uses worker '{t.workerClass?.Name ?? "null"}', expected ThoughtWorker_Precept_GeneDivergence");
                Check.Soft(t.GetModExtension<GeneDivergenceThoughtExtension>() != null,
                    $"{pair.name} carries no GeneDivergenceThoughtExtension, so the worker cannot bucket it");
                Check.Soft(t.stages != null && t.stages.Count >= 2,
                    $"{pair.name} has {t.stages?.Count ?? 0} stages - a scaling thought needs room to climb");
                Check.Soft(MoodMonotone(t, pair.sign),
                    $"{pair.name} mood stages do not run {(pair.sign > 0 ? "up" : "down")} in the {(pair.sign > 0 ? "positive" : "negative")} direction");
            }

            // Baseline penalty for Genecrafted: our pristine worker, a single negative stage.
            ThoughtDef baseline = DefDatabase<ThoughtDef>.GetNamedSilentFail("RBP_GeneBaseline_Disapproved");
            if (Check.Soft(baseline != null, "RBP_GeneBaseline_Disapproved not found"))
            {
                Check.Soft(baseline.workerClass == typeof(ThoughtWorker_Precept_NoGeneDivergence),
                    "RBP_GeneBaseline_Disapproved must use the pristine worker");
                Check.Soft(baseline.stages != null && baseline.stages.Count == 1 && baseline.stages[0].baseMoodEffect < 0f,
                    "RBP_GeneBaseline_Disapproved must be a single penalty stage");
            }

            // Social thoughts: opinion offsets, opposite targets, correct workers.
            CheckSocial("RBP_GeneBaseline_Disapproved_Social", typeof(ThoughtWorker_Precept_NoGeneDivergence_Social));
            CheckSocial("RBP_GeneDiverged_Disapproved_Social", typeof(ThoughtWorker_Precept_GeneDivergence_Social));

            Check.SoftResult();
        }

        [Test]
        public static void DivergenceStagingAndPristineDetectionAreSane()
        {
            if (!Check.Ready("ideology.genememes", Ids.Ideology, Ids.Biotech))
                return;

            // Bucketing: below one step is stage 0, each step advances, and it clamps to the last stage.
            Check.Soft(GeneDivergenceThought.StageFor(0.5f, 5f, 5) == 0, "half a step should sit at stage 0");
            Check.Soft(GeneDivergenceThought.StageFor(5f, 5f, 5) == 1, "one full step should reach stage 1");
            Check.Soft(GeneDivergenceThought.StageFor(24f, 5f, 5) == 4, "24 points over 5-point steps should clamp to the last of 5 stages");
            Check.Soft(GeneDivergenceThought.StageFor(1000f, 5f, 4) == 3, "a huge score must clamp to the last stage, never overflow");

            // IsPristine/IsDiverged must agree with the raw point count for whatever pawn we get.
            // In a heavy modlist even a forced baseliner can carry framework-injected genes and so
            // read as genuinely diverged, so we assert the relationship, not that it is pristine.
            Pawn pawn = MakeBaselinerPawn();
            if (pawn == null || pawn.genes == null)
            {
                Check.Note("could not generate a pawn with genes; live pristine/diverged check skipped");
                Discard(pawn);
                Check.SoftResult();
                return;
            }
            try
            {
                float p0 = GeneDivergenceThought.PointsFor(pawn);
                Check.Note($"generated {pawn.genes.Xenotype?.defName ?? "?"} pawn scores {p0:0.#} divergence points");
                Check.Soft(GeneDivergenceThought.IsPristine(pawn) == (p0 <= GeneDivergenceThought.MinPoints),
                    $"IsPristine disagrees with the point count ({p0:0.#})");
                Check.Soft(GeneDivergenceThought.IsDiverged(pawn) == (p0 > GeneDivergenceThought.MinPoints),
                    $"IsDiverged disagrees with the point count ({p0:0.#})");
                Check.Soft(!(GeneDivergenceThought.IsPristine(pawn) && GeneDivergenceThought.IsDiverged(pawn)),
                    "a pawn reads as both pristine and diverged at once");

                // Bolting on a gene that is off the pawn's own template must raise divergence and,
                // whatever it was before, leave it reading as diverged rather than pristine.
                GeneDef extra = OffTemplateGeneFor(pawn);
                if (extra != null)
                {
                    pawn.genes.AddGene(extra, true);
                    float p1 = GeneDivergenceThought.PointsFor(pawn);
                    Check.Soft(p1 > p0,
                        $"adding off-template gene '{extra.defName}' did not raise divergence ({p0:0.#} -> {p1:0.#})");
                    Check.Soft(GeneDivergenceThought.IsDiverged(pawn) && !GeneDivergenceThought.IsPristine(pawn),
                        $"a pawn given off-template gene '{extra.defName}' does not read as diverged (points {p1:0.#})");
                }
                else
                {
                    Check.Note("no off-template gene candidate found; diverged-side live check skipped");
                }
            }
            finally
            {
                Discard(pawn);
            }

            Check.SoftResult();
        }

        private static bool RequiresPrecept(MemeDef meme, string preceptDefName)
        {
            if (meme.requireOne == null)
                return false;
            foreach (List<PreceptDef> group in meme.requireOne)
                if (group != null)
                    foreach (PreceptDef p in group)
                        if (p != null && p.defName == preceptDefName)
                            return true;
            return false;
        }

        private static bool HasMeme(List<MemeDef> memes, string defName)
        {
            if (memes == null)
                return false;
            foreach (MemeDef m in memes)
                if (m != null && m.defName == defName)
                    return true;
            return false;
        }

        private static List<string> SituationalThoughts(PreceptDef precept)
        {
            var names = new List<string>();
            if (precept.comps != null)
                foreach (PreceptComp comp in precept.comps)
                    if (comp is PreceptComp_SituationalThought sit && sit.thought != null)
                        names.Add(sit.thought.defName);
            return names;
        }

        private static bool MoodMonotone(ThoughtDef t, int sign)
        {
            float prev = 0f;
            for (int i = 0; i < t.stages.Count; i++)
            {
                float mood = t.stages[i].baseMoodEffect;
                if (sign > 0 && mood <= 0f) return false;
                if (sign < 0 && mood >= 0f) return false;
                if (i > 0 && sign > 0 && mood < prev) return false;
                if (i > 0 && sign < 0 && mood > prev) return false;
                prev = mood;
            }
            return true;
        }

        private static void CheckSocial(string defName, Type workerType)
        {
            ThoughtDef t = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
            if (!Check.Soft(t != null, $"{defName} not found"))
                return;
            Check.Soft(t.thoughtClass == typeof(Thought_SituationalSocial),
                $"{defName} must be a Thought_SituationalSocial to carry an opinion offset");
            Check.Soft(t.workerClass == workerType,
                $"{defName} uses worker '{t.workerClass?.Name ?? "null"}', expected {workerType.Name}");
            Check.Soft(t.stages != null && t.stages.Count >= 1 && t.stages[0].baseOpinionOffset < 0f,
                $"{defName} must carry a negative opinion offset");
        }

        private static GeneDef OffTemplateGeneFor(Pawn pawn)
        {
            // A self-contained, non-archite gene the pawn neither already carries nor has in its own
            // xenotype template, so adding it is an unmatched gene and strictly raises divergence.
            var owned = new HashSet<GeneDef>();
            if (pawn.genes != null)
            {
                foreach (Gene g in pawn.genes.GenesListForReading)
                    if (g?.def != null)
                        owned.Add(g.def);
                if (pawn.genes.Xenotype?.genes != null)
                    foreach (GeneDef g in pawn.genes.Xenotype.genes)
                        owned.Add(g);
            }

            foreach (GeneDef g in DefDatabase<GeneDef>.AllDefs)
                if (g.biostatArc == 0 && g.biostatMet != 0 && g.prerequisite == null
                    && g.exclusionTags.NullOrEmpty() && !owned.Contains(g))
                    return g;
            foreach (GeneDef g in DefDatabase<GeneDef>.AllDefs)
                if (g.biostatArc == 0 && g.biostatMet != 0 && !owned.Contains(g))
                    return g;
            return null;
        }

        private static Pawn MakeBaselinerPawn()
        {
            try
            {
                return PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    allowAddictions: false, allowFood: false,
                    forcedXenotype: XenotypeDefOf.Baseliner));
            }
            catch (Exception e)
            {
                Log.Warning("[RBP Tests] could not generate a baseliner pawn: " + e.Message);
                return null;
            }
        }

        private static void Discard(Pawn pawn)
        {
            try
            {
                if (pawn != null && !pawn.Destroyed)
                    pawn.Destroy();
            }
            catch { }
        }
    }
}
