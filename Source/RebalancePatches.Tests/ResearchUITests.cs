using System.Collections.Generic;
using HarmonyLib;
using RebalancePatches.Mods.ResearchUI;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ResearchUITests
    {
        [Test]
        public static void EmptyResearchTabFilterIsRegistered()
        {
            if (!Check.Ready(EmptyResearchTabPatches.SettingKey))
                return;
            Check.HarmonyPatched(
                AccessTools.Method(typeof(MainTabWindow_Research), nameof(MainTabWindow_Research.PostOpen)),
                "hide empty research tabs");
            Check.True(EmptyResearchTabPatches.Active,
                "the research window's private tab list could not be found, so the filter stood itself "
                + "down and no tab will ever be hidden");
        }

        [Test]
        public static void EmptyTabsAreHiddenAndPopulatedTabsAreNot()
        {
            if (!Check.Ready(EmptyResearchTabPatches.SettingKey))
                return;

            var withProjects = new HashSet<ResearchTabDef>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                if (project.tab != null)
                    withProjects.Add(project.tab);

            List<ResearchTabDef> allTabs = DefDatabase<ResearchTabDef>.AllDefsListForReading;
            if (!Check.Soft(allTabs.Count > 0,
                    "no ResearchTabDefs are loaded at all, so the filter could not be exercised"))
            {
                Check.SoftResult();
                return;
            }

            List<ResearchTabDef> hidden =
                EmptyResearchTabPatches.TabsToHide(allTabs, ResearchTabDefOf.Main);
            var hiddenSet = new HashSet<ResearchTabDef>(hidden);

            int empties = 0;
            foreach (ResearchTabDef tab in allTabs)
                if (!withProjects.Contains(tab))
                    empties++;
            Check.Note($"{allTabs.Count} tab(s) loaded, {empties} hold no project, "
                + $"{hidden.Count} would be hidden: {Names(hidden)}");

            foreach (ResearchTabDef tab in allTabs)
            {
                if (withProjects.Contains(tab))
                {
                    Check.Soft(!hiddenSet.Contains(tab),
                        $"tab '{tab.defName}' has research projects assigned to it but would be hidden");
                }
                else if (tab != ResearchTabDefOf.Main)
                {
                    Check.Soft(hiddenSet.Contains(tab),
                        $"tab '{tab.defName}' has no research project assigned to it but would still be drawn");
                }
            }

            Check.Soft(!hiddenSet.Contains(ResearchTabDefOf.Main), "the Main tab would be hidden");
            Check.Soft(EmptyResearchTabPatches.IsEmpty(ResearchTabDefOf.Main) == !withProjects.Contains(ResearchTabDefOf.Main),
                "IsEmpty disagrees with the def database about the Main tab");
            Check.SoftResult();
        }

        [Test]
        public static void CurrentTabAndLastTabSurviveTheFilter()
        {
            if (!Check.Ready(EmptyResearchTabPatches.SettingKey))
                return;

            ResearchTabDef main = ResearchTabDefOf.Main;
            if (!Check.Soft(main != null, "ResearchTabDefOf.Main did not resolve"))
            {
                Check.SoftResult();
                return;
            }

            var emptyTab = new ResearchTabDef { defName = "RBP_TestOnly_EmptyTab" };
            var populatedTab = new ResearchTabDef { defName = "RBP_TestOnly_PopulatedTab" };
            var withProjects = new HashSet<ResearchTabDef> { main, populatedTab };
            var candidates = new List<ResearchTabDef> { main, populatedTab, emptyTab };

            List<ResearchTabDef> hidden =
                EmptyResearchTabPatches.TabsToHide(candidates, main, withProjects);
            Check.Soft(hidden.Count == 1 && hidden.Contains(emptyTab),
                "viewing Main: only the projectless tab should be hidden, "
                + $"actually hidden: {Names(hidden)}");

            hidden = EmptyResearchTabPatches.TabsToHide(candidates, emptyTab, withProjects);
            Check.Soft(hidden.Count == 0,
                "viewing the projectless tab: the tab being viewed must never be hidden, "
                + $"actually hidden: {Names(hidden)}");

            hidden = EmptyResearchTabPatches.TabsToHide(
                new List<ResearchTabDef> { emptyTab }, null, new HashSet<ResearchTabDef>());
            Check.Soft(hidden.Count == 0,
                "every tab projectless and no Main: the tab row must not be emptied, "
                + $"actually hidden: {Names(hidden)}");

            Check.SoftResult();
        }

        private static string Names(List<ResearchTabDef> tabs)
        {
            if (tabs == null || tabs.Count == 0)
                return "(none)";
            var names = new List<string>(tabs.Count);
            foreach (ResearchTabDef tab in tabs)
                names.Add(tab == null ? "null" : tab.defName);
            return string.Join(", ", names.ToArray());
        }
    }
}
