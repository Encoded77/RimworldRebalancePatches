using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Every TraitDef with each of its degrees, and everything that hands a trait out without the
    /// random roll: backstories, genes, xenotypes, pawn kinds, scenarios, quests and precepts.
    ///
    /// Both halves matter for the same question. A trait's commonality only describes the roll, and a
    /// large modlist stacks hundreds of new entries into that pool, so the odds of any one vanilla
    /// trait quietly collapse; meanwhile the traits that actually turn up on pawns are often the
    /// forced ones, which no commonality number mentions.
    ///
    /// Degrees are summarised in a compact list as well as dumped raw, because the raw walk drops
    /// labels (they are noise in every other dump) and a degree without its label is unreadable.
    /// </summary>
    internal static class TraitDump
    {
        private static readonly Type[] Referenced =
        {
            typeof(StatDef), typeof(SkillDef), typeof(WorkTypeDef), typeof(ThoughtDef),
            typeof(MentalStateDef), typeof(AbilityDef), typeof(HediffDef), typeof(NeedDef),
        };

        [DebugAction("RebalancePatches", "Dump traits", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump traits", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(Referenced, bareDefTypes: new[] { typeof(TraitDef) });
            int total = 0;
            var refs = new DefRefScanner.Results();

            DumpRunner.Run("TraitDump.json", walker, w =>
            {
                Json j = w.Json;
                var traits = DefDatabase<TraitDef>.AllDefsListForReading
                    .OrderBy(t => t.defName, StringComparer.Ordinal).ToList();

                j.Name("traits");
                j.BeginArray();
                foreach (TraitDef trait in traits)
                {
                    w.WriteDefEntry(trait, _ =>
                    {
                        // Trait-level commonality is a private field, so the reflective walk misses
                        // the one number that decides how often the trait is rolled at all. The
                        // per-degree values only pick which degree, once the trait has won.
                        j.Name("commonality");
                        j.Number(trait.GetGenderSpecificCommonality(Gender.Male));
                        j.Name("commonalityFemale");
                        j.Number(trait.GetGenderSpecificCommonality(Gender.Female));
                        j.Name("degreeCount"); j.Number(trait.degreeDatas?.Count ?? 0);

                        j.Name("degrees");
                        j.BeginArray();
                        if (trait.degreeDatas != null)
                            foreach (TraitDegreeData degree in trait.degreeDatas)
                            {
                                j.BeginObject();
                                j.Name("degree"); j.Number(degree.degree);
                                j.Name("label"); j.Value(degree.label);
                                j.Name("commonality"); j.Number(degree.commonality);
                                j.EndObject();
                            }
                        j.EndArray();
                    });
                    total++;
                }
                j.EndArray();

                // Who hands the trait out directly. Same scan the acquisition dump runs for items:
                // the shapes differ per mod, so the reference is found by walking rather than by
                // knowing each field.
                var traitSet = new HashSet<Def>(traits.Cast<Def>());
                DefRefScanner.ScanAllFields(refs, DefDatabase<BackstoryDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<PawnKindDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<ScenarioDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<QuestScriptDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<HediffDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                if (ModsConfig.BiotechActive)
                {
                    DefRefScanner.ScanAllFields(refs, DefDatabase<GeneDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                    DefRefScanner.ScanAllFields(refs, DefDatabase<XenotypeDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                }
                if (ModsConfig.IdeologyActive)
                {
                    DefRefScanner.ScanAllFields(refs, DefDatabase<PreceptDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                    DefRefScanner.ScanAllFields(refs, DefDatabase<MemeDef>.AllDefsListForReading.Cast<Def>(), traitSet.Contains);
                }
                DefRefScanner.Write(j, "references", refs);
            }, () => $"{total} traits, {refs.Count} forced-trait references");
        }
    }
}
