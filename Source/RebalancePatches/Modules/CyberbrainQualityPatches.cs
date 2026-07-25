using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>Carries the quality of the installed cyberbrain item onto its <see cref="Hediff_QualityImplant"/>,
    /// which the surgery would otherwise discard when it consumes the item.</summary>
    internal static class CyberbrainQualityPatches
    {
        private const string SettingKey = "cybernetics.cyberbrainquality";
        private const string GitsModId = "moistestwhale.gitscyberbrains";

        private static bool warned;

        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IsActive(GitsModId))
                return;
            try
            {
                MethodInfo target = AccessTools.Method(typeof(Recipe_InstallImplant), nameof(RecipeWorker.ApplyOnPawn));
                if (target == null)
                    throw new MissingMemberException("RimWorld.Recipe_InstallImplant.ApplyOnPawn not found");
                harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(CyberbrainQualityPatches), nameof(CapturePrefix)),
                    postfix: new HarmonyMethod(typeof(CyberbrainQualityPatches), nameof(ApplyPostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not hook cyberbrain installation for quality; "
                    + "cyberbrains will install at normal strength regardless of quality:\n" + ex);
            }
        }

        internal static void CapturePrefix(RecipeWorker __instance, List<Thing> ingredients, out QualityCategory? __state)
        {
            __state = null;
            try
            {
                if (!SettingsRegistry.GetEffective(SettingKey))
                    return;
                if (!AddsQualityImplant(__instance?.recipe) || ingredients == null)
                    return;
                foreach (Thing ingredient in ingredients)
                    if (ingredient.TryGetComp<CompQuality>() is CompQuality comp)
                    {
                        __state = comp.Quality;
                        return;
                    }
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        internal static void ApplyPostfix(RecipeWorker __instance, Pawn pawn, BodyPartRecord part, QualityCategory? __state)
        {
            if (__state == null)
                return;
            try
            {
                HediffDef added = __instance?.recipe?.addsHediff;
                if (added == null || pawn?.health?.hediffSet == null)
                    return;

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int i = hediffs.Count - 1; i >= 0; i--)
                    if (hediffs[i].def == added && hediffs[i].Part == part && hediffs[i] is Hediff_QualityImplant implant)
                    {
                        implant.SetQuality(__state.Value);
                        return;
                    }
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        private static bool AddsQualityImplant(RecipeDef recipe)
        {
            HediffDef added = recipe?.addsHediff;
            return added != null && typeof(Hediff_QualityImplant).IsAssignableFrom(added.hediffClass);
        }

        private static void Warn(Exception ex)
        {
            if (warned)
                return;
            warned = true;
            Log.Warning("[Rebalance Patches] Applying cyberbrain quality at installation failed:\n" + ex);
        }
    }
}
