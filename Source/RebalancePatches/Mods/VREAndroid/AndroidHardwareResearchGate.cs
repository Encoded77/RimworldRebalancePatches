using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.VREAndroid
{
    public static class AndroidHardwareResearchGate
    {
        private const string AndroidModId = "vanillaracesexpanded.android";
        private const string WindowTypeName = "VREAndroids.Window_CreateAndroidBase";
        private const string ValidatorName = "GeneValidator";

        /// <summary>The capstone that buys psychic capability. Owned by the research overhaul.</summary>
        public const string GateResearchDefName = "RBP_CybSyntheticAscension";

        private static ResearchProjectDef gate;
        private static bool gateResolved;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                if (!ModsConfig.IsActive(AndroidModId))
                    return;

                Type window = GenTypes.GetTypeInAnyAssembly(WindowTypeName);
                MethodInfo target = window == null
                    ? null
                    : AccessTools.Method(window, ValidatorName, new[] { typeof(GeneDef) });
                if (target == null)
                    return;

                harmony.Patch(target,
                    postfix: new HarmonyMethod(typeof(AndroidHardwareResearchGate), nameof(HideUntilResearched)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not gate android hardware on research:\n" + ex);
            }
        }

        private static void HideUntilResearched(GeneDef x, ref bool __result)
        {
            try
            {
                if (__result)
                    __result = AllowedNow(x);
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Android hardware research gate threw:\n" + ex);
            }
        }

        /// <summary>Whether this gene may be fitted right now, against the live capstone.</summary>
        public static bool AllowedNow(GeneDef gene) => Allowed(gene, Gate());

        public static bool Allowed(GeneDef gene, ResearchProjectDef project)
        {
            if (gene == null || gene.defName != StatPart_PsychicDeafnessOverride.CounterGeneDefName)
                return true;
            if (project == null)
                return true;
            if (Current.ProgramState != ProgramState.Playing || Find.ResearchManager == null)
                return true;
            return project.IsFinished;
        }

        private static ResearchProjectDef Gate()
        {
            if (!gateResolved)
            {
                gateResolved = true;
                gate = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(GateResearchDefName);
            }
            return gate;
        }
    }
}
