using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.VanillaSkillsExpanded
{
    /// <summary>
    /// Vanilla Skills Expanded only ever hands expertise to colonists the player picks it for.
    /// This gives every generated humanlike a chance to already hold one matching expertise it
    /// qualifies for, so raiders, recruits and visitors carry them too. Vanilla Skills Expanded
    /// exposes no generation hook, so this is a postfix on PawnGenerator.GeneratePawn; the target
    /// mod is optional, so its types are reached by reflection.
    /// </summary>
    internal static class ExpertiseGenerationPatches
    {
        private const string SettingKey = "vse.expertisegeneration";
        private const string ChanceKey = "vse.expertisegenerationchance";

        private static Type expertiseDefType;
        private static FieldInfo skillField;         // ExpertiseDef.skill (SkillDef)
        private static FieldInfo hideField;          // ExpertiseDef.hide (bool)
        private static MethodInfo canApplyOnMethod;  // ExpertiseDef.CanApplyOn(Pawn, out string)
        private static MethodInfo expertiseForPawn;  // static ExpertiseTrackers.Expertise(Pawn)
        private static MethodInfo addExpertiseMethod; // ExpertiseTracker.AddExpertise(ExpertiseDef)
        private static PropertyInfo allExpertiseProp; // ExpertiseTracker.AllExpertise (IList)
        private static PropertyInfo levelProp;        // ExpertiseRecord.Level (int)
        private static bool warned;

        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IsActive("vanillaexpanded.skills") || !SettingsRegistry.GetEffective(SettingKey))
                return;
            try
            {
                expertiseDefType = AccessTools.TypeByName("VSE.Expertise.ExpertiseDef");
                Type trackersType = AccessTools.TypeByName("VSE.ExpertiseTrackers");
                Type trackerType = AccessTools.TypeByName("VSE.ExpertiseTracker");
                Type recordType = AccessTools.TypeByName("VSE.ExpertiseRecord");
                if (expertiseDefType == null || trackersType == null || trackerType == null || recordType == null)
                    throw new MissingMemberException("Vanilla Skills Expanded expertise types not found");

                skillField = AccessTools.Field(expertiseDefType, "skill");
                hideField = AccessTools.Field(expertiseDefType, "hide");
                canApplyOnMethod = AccessTools.Method(expertiseDefType, "CanApplyOn",
                    new[] { typeof(Pawn), typeof(string).MakeByRefType() });
                expertiseForPawn = AccessTools.Method(trackersType, "Expertise", new[] { typeof(Pawn) });
                addExpertiseMethod = AccessTools.Method(trackerType, "AddExpertise", new[] { expertiseDefType });
                allExpertiseProp = AccessTools.Property(trackerType, "AllExpertise");
                levelProp = AccessTools.Property(recordType, "Level");
                if (skillField == null || hideField == null || canApplyOnMethod == null || expertiseForPawn == null
                    || addExpertiseMethod == null || allExpertiseProp == null || levelProp == null)
                    throw new MissingMemberException("Vanilla Skills Expanded expertise members not found");

                harmony.Patch(
                    AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                        new[] { typeof(PawnGenerationRequest) }),
                    postfix: new HarmonyMethod(typeof(ExpertiseGenerationPatches), nameof(GeneratePawnPostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch Vanilla Skills Expanded expertise generation:\n{ex}");
            }
        }

        private static void GeneratePawnPostfix(Pawn __result)
        {
            try
            {
                Grant(__result, SettingsRegistry.GetEffectiveValue(ChanceKey) / 100f);
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning($"[Rebalance Patches] Expertise generation postfix failed:\n{ex}");
                }
            }
        }

        /// <summary>Rolls <paramref name="chance"/> once for the pawn and, on success, grants it one
        /// expertise it qualifies for - Vanilla Skills Expanded's own CanApplyOn does the gating
        /// (skill level, passion, per-pawn cap, one-per-skill) - weighted toward its stronger skills,
        /// at a low starting level. Returns the granted def, or null. Internal so a test can drive it
        /// with a forced chance.</summary>
        internal static Def Grant(Pawn pawn, float chance)
        {
            if (pawn?.skills == null || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
                return null;
            if (pawn.DevelopmentalStage != DevelopmentalStage.Adult)
                return null;
            if (chance <= 0f || !Rand.Chance(chance))
                return null;

            List<Def> pool = null;
            List<int> weights = null;
            foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(expertiseDefType))
            {
                if ((bool)hideField.GetValue(def))
                    continue;
                object[] args = { pawn, null };
                if (!(bool)canApplyOnMethod.Invoke(def, args))
                    continue;
                SkillDef skill = skillField.GetValue(def) as SkillDef;
                int level = skill == null ? 0 : (pawn.skills.GetSkill(skill)?.Level ?? 0);
                (pool ??= new List<Def>()).Add(def);
                (weights ??= new List<int>()).Add(Math.Max(1, level));
            }
            if (pool == null)
                return null;

            Def chosen = WeightedPick(pool, weights);
            object tracker = expertiseForPawn.Invoke(null, new object[] { pawn });
            if (tracker == null)
                return null;
            addExpertiseMethod.Invoke(tracker, new object[] { chosen });

            if (allExpertiseProp.GetValue(tracker) is IList records && records.Count > 0)
                levelProp.SetValue(records[records.Count - 1], Rand.RangeInclusive(1, 3));
            return chosen;
        }

        private static Def WeightedPick(List<Def> defs, List<int> weights)
        {
            int total = 0;
            foreach (int w in weights)
                total += w;
            int roll = Rand.RangeInclusive(1, total);
            for (int i = 0; i < defs.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0)
                    return defs[i];
            }
            return defs[defs.Count - 1];
        }
    }
}
