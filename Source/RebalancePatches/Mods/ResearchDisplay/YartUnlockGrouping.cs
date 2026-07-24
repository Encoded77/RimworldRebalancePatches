using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.ResearchDisplay
{
    public static class YartUnlockGrouping
    {
        public const string SettingKey = "yart.unlockgrouping";

        private const string YartModId = "seohyeon.yart";
        private const string UtilityTypeName = "YART.Utils.UnlockedDefsUtility";
        private const string MethodName = "ResearchPrereqsOf";

        private static Dictionary<Def, List<ResearchProjectDef>> unlockedBy;

        /// <summary>Whether YART's grouping input was found and the postfix is registered.</summary>
        public static bool Active { get; private set; }

        /// <summary>The patched method, so a test can assert against the real target.</summary>
        public static MethodInfo Target { get; private set; }

        public static void TryApply(Harmony harmony)
        {
            try
            {
                if (!ModsConfig.IsActive(YartModId))
                    return;

                Type utility = GenTypes.GetTypeInAnyAssembly(UtilityTypeName);
                MethodInfo target = utility == null
                    ? null
                    : AccessTools.Method(utility, MethodName, new[] { typeof(Def) });
                if (target == null)
                {
                    Log.Warning("[Rebalance Patches] Yet Another Research Tree is present but the method that "
                        + "decides which research a card's unlocks are grouped under could not be found, so "
                        + "items gated by a second research will keep being listed as direct unlocks.");
                    return;
                }

                harmony.Patch(target,
                    postfix: new HarmonyMethod(typeof(YartUnlockGrouping), nameof(AlsoGatedByPostfix)));
                Target = target;
                Active = true;
            }
            catch (Exception ex)
            {
                Active = false;
                Target = null;
                Log.Warning("[Rebalance Patches] Could not correct research card unlock grouping:\n" + ex);
            }
        }

        public static Dictionary<Def, List<ResearchProjectDef>> UnlockedBy()
        {
            if (unlockedBy != null)
                return unlockedBy;

            var map = new Dictionary<Def, List<ResearchProjectDef>>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                List<Def> unlocked;
                try
                {
                    unlocked = project.UnlockedDefs;
                }
                catch (Exception ex)
                {
                    // One project's patched getter throwing must not cost the whole map.
                    Log.Warning($"[Rebalance Patches] Could not read what '{project.defName}' unlocks:\n{ex}");
                    continue;
                }
                if (unlocked == null)
                    continue;

                for (int i = 0; i < unlocked.Count; i++)
                {
                    Def def = unlocked[i];
                    if (def == null)
                        continue;
                    if (!map.TryGetValue(def, out List<ResearchProjectDef> projects))
                    {
                        projects = new List<ResearchProjectDef>();
                        map[def] = projects;
                    }
                    if (!projects.Contains(project))
                        projects.Add(project);
                }
            }

            // Published only once complete, so a caller racing the build never sees a partial map.
            unlockedBy = map;
            return map;
        }

        public static List<ResearchProjectDef> AlsoGatedBy(Def def, IEnumerable<ResearchProjectDef> declared,
            Dictionary<Def, List<ResearchProjectDef>> unlockers)
        {
            var result = new List<ResearchProjectDef>();
            if (declared != null)
                foreach (ResearchProjectDef project in declared)
                    if (project != null && !result.Contains(project))
                        result.Add(project);

            if (def != null && unlockers != null && unlockers.TryGetValue(def, out List<ResearchProjectDef> also))
                for (int i = 0; i < also.Count; i++)
                    if (also[i] != null && !result.Contains(also[i]))
                        result.Add(also[i]);

            return result;
        }

        private static void AlsoGatedByPostfix(Def def, ref IEnumerable<ResearchProjectDef> __result)
        {
            try
            {
                if (!SettingsRegistry.GetEffective(SettingKey))
                    return;

                __result = AlsoGatedBy(def, __result, UnlockedBy());
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not correct research card unlock grouping:\n" + ex);
            }
        }
    }
}
