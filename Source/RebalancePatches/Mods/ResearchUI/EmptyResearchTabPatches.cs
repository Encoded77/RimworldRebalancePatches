using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.ResearchUI
{
    public static class EmptyResearchTabPatches
    {
        public const string SettingKey = "vanilla.hideemptyresearchtabs";

        private static FieldInfo tabsField;
        private static FieldInfo recordDefField;
        private static HashSet<ResearchTabDef> populatedTabs;

        /// <summary>Whether the window's tab list was found and the postfix is registered.</summary>
        public static bool Active { get; private set; }

        public static void TryApply(Harmony harmony)
        {
            try
            {
                MethodInfo postOpen = AccessTools.Method(typeof(MainTabWindow_Research),
                    nameof(MainTabWindow_Research.PostOpen));
                tabsField = AccessTools.Field(typeof(MainTabWindow_Research), "tabs");

                Type recordType = tabsField != null && tabsField.FieldType.IsGenericType
                    ? tabsField.FieldType.GetGenericArguments()[0]
                    : null;
                recordDefField = recordType == null ? null : AccessTools.Field(recordType, "def");

                if (postOpen == null || tabsField == null || recordDefField == null)
                {
                    Log.Warning("[Rebalance Patches] The research window's tab list could not be found, so "
                        + "empty research tabs will keep being drawn.");
                    tabsField = null;
                    recordDefField = null;
                    return;
                }

                harmony.Patch(postOpen,
                    postfix: new HarmonyMethod(typeof(EmptyResearchTabPatches), nameof(PostOpenPostfix)));
                Active = true;
            }
            catch (Exception ex)
            {
                tabsField = null;
                recordDefField = null;
                Active = false;
                Log.Warning("[Rebalance Patches] Could not filter empty research tabs:\n" + ex);
            }
        }

        private static HashSet<ResearchTabDef> PopulatedTabs()
        {
            if (populatedTabs != null)
                return populatedTabs;

            var found = new HashSet<ResearchTabDef>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                if (project.tab != null)
                    found.Add(project.tab);
            }

            populatedTabs = found;
            return populatedTabs;
        }

        public static bool IsEmpty(ResearchTabDef tab) => tab != null && !PopulatedTabs().Contains(tab);

        public static List<ResearchTabDef> TabsToHide(IList<ResearchTabDef> candidates, ResearchTabDef currentTab)
            => TabsToHide(candidates, currentTab, PopulatedTabs());

        public static List<ResearchTabDef> TabsToHide(IList<ResearchTabDef> candidates,
            ResearchTabDef currentTab, ICollection<ResearchTabDef> tabsWithProjects)
        {
            var hide = new List<ResearchTabDef>();
            if (candidates == null || tabsWithProjects == null)
                return hide;

            int kept = 0;
            foreach (ResearchTabDef tab in candidates)
            {
                if (tab == null || tab == currentTab || tab == ResearchTabDefOf.Main
                    || tabsWithProjects.Contains(tab))
                {
                    kept++;
                    continue;
                }
                hide.Add(tab);
            }

            if (kept == 0)
                hide.Clear();

            return hide;
        }

        private static void PostOpenPostfix(MainTabWindow_Research __instance)
        {
            try
            {
                if (!SettingsRegistry.GetEffective(SettingKey))
                    return;
                if (tabsField == null || recordDefField == null)
                    return;
                if (!(tabsField.GetValue(__instance) is IList records) || records.Count == 0)
                    return;

                var defs = new List<ResearchTabDef>(records.Count);
                foreach (object record in records)
                    defs.Add(recordDefField.GetValue(record) as ResearchTabDef);

                List<ResearchTabDef> hide = TabsToHide(defs, __instance.CurTab);
                if (hide.Count == 0)
                    return;

                for (int i = records.Count - 1; i >= 0; i--)
                    if (hide.Contains(defs[i]))
                        records.RemoveAt(i);
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not filter empty research tabs:\n" + ex);
            }
        }
    }
}
