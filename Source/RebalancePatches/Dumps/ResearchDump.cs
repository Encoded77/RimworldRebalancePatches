using System;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Every ResearchProjectDef and every research tab. Serves any project that reshapes the tree,
    /// not just the implant work — research merging needs exactly this file.
    ///
    /// Techprints get first-class treatment because they are generated defs:
    /// ThingDefGenerator_Techprints creates one ThingDef named "Techprint_&lt;project&gt;" for every
    /// project with techprintCount above zero. Merge or delete a project and that item is stranded
    /// in trader stock and reward tables, still buyable, gating nothing. Recording the link here is
    /// what makes that catchable before the merge rather than after.
    /// </summary>
    internal static class ResearchDump
    {
        private static readonly Type[] Referenced =
        {
            typeof(ThingDef), typeof(ResearchTabDef), typeof(ResearchProjectDef),
        };

        [DebugAction("RebalancePatches", "Dump research", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump research", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(Referenced, bareDefTypes: new[] { typeof(ResearchProjectDef) });
            int total = 0, printed = 0, stranded = 0;

            DumpRunner.Run("ResearchDump.json", walker, w =>
            {
                Json j = w.Json;

                j.Name("tabs");
                j.BeginArray();
                foreach (ResearchTabDef tab in DefDatabase<ResearchTabDef>.AllDefsListForReading
                             .OrderBy(t => t.defName, StringComparer.Ordinal))
                {
                    j.BeginObject();
                    j.Name("defName"); j.Value(tab.defName);
                    j.Name("label"); j.Value(tab.label);
                    j.Name("mod"); j.Value(DumpRunner.ModOf(tab));
                    j.EndObject();
                }
                j.EndArray();

                j.Name("projects");
                j.BeginArray();
                foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading
                             .OrderBy(r => r.defName, StringComparer.Ordinal))
                {
                    w.WriteDefEntry(project, _ =>
                    {
                        j.Name("tab"); j.Value(project.tab?.defName);
                        j.Name("hasTechprint"); j.Value(project.techprintCount > 0);
                        if (project.techprintCount > 0)
                        {
                            ThingDef print = DefDatabase<ThingDef>.GetNamedSilentFail("Techprint_" + project.defName);
                            j.Name("techprintDef"); j.Value(print?.defName);
                            // A project wanting techprints with no generated item means something
                            // upstream removed it - the item cannot be bought, so the project cannot
                            // be finished.
                            j.Name("techprintMissing"); j.Value(print == null);
                            printed++;
                            if (print == null) stranded++;
                        }
                    });
                    total++;
                }
                j.EndArray();
            }, () => $"{total} projects, {printed} with techprints, {stranded} missing their techprint item");
        }
    }
}
