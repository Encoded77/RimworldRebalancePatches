using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.GeneFabrication
{
    /// <summary>
    /// Gene Fabrication implies one Make_Genepack_&lt;gene&gt; recipe per GeneDef and gives every
    /// archite one researchPrerequisite = Archogenetics. On a large gene modlist that is hundreds of
    /// recipes hanging off a single research project, and research-tree UIs list each one, which
    /// buries whatever else that project unlocks.
    ///
    /// The prerequisite is redundant once Gene Fabrication is an archogenetics capstone: the
    /// fabricator itself needs the GeneFabrication project, which needs Archogenetics, so clearing
    /// it cannot make a genepack available any earlier. Applied only when that chain actually
    /// holds, so with the capstone feature off the recipes stay gated exactly as the mod ships them.
    /// </summary>
    internal static class GeneFabricationPatches
    {
        private const string RecipePrefix = "Make_Genepack_";

        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IsActive("amch.eragon.hcgenefabrication")
                || !SettingsRegistry.GetEffective("geneticsresearch.genefab"))
                return;
            try
            {
                if (!ArchogeneticsGatesFabricator())
                    return;

                // Gene Fabrication generates its recipes from a StaticConstructorOnStartup, the same
                // phase we run in, so we may land either side of it. Cover both orders: sweep the
                // recipes that already exist, and postfix the generator for the ones that do not.
                ClearExisting();

                Type generator = AccessTools.TypeByName("GeneFarbication.RecipeDefGenerator");
                var target = generator == null ? null : AccessTools.Method(generator, "GeneFabricationDef");
                if (target == null)
                    throw new MissingMemberException("GeneFarbication.RecipeDefGenerator.GeneFabricationDef not found");
                harmony.Patch(target,
                    postfix: new HarmonyMethod(typeof(GeneFabricationPatches), nameof(GeneFabricationDefPostfix)));
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not declutter Gene Fabrication's genepack recipes:\n{ex}");
            }
        }

        /// <summary>
        /// True when the fabricator already sits behind Archogenetics, which is what makes the
        /// per-recipe prerequisite redundant rather than load-bearing.
        /// </summary>
        internal static bool ArchogeneticsGatesFabricator()
        {
            ResearchProjectDef fabrication = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("GeneFabrication");
            ResearchProjectDef archogenetics = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Archogenetics");
            if (fabrication == null || archogenetics == null)
                return false;
            List<ResearchProjectDef> prerequisites = fabrication.prerequisites;
            return prerequisites != null && prerequisites.Contains(archogenetics);
        }

        private static void ClearExisting()
        {
            int cleared = 0;
            // Field writes only - never touch the list itself. Research-tree mods enumerate the
            // def databases off the main thread during startup, and mutating one collides with them.
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.defName != null && recipe.defName.StartsWith(RecipePrefix)
                    && recipe.researchPrerequisite != null)
                {
                    recipe.researchPrerequisite = null;
                    cleared++;
                }
            }
            if (cleared > 0)
                Log.Message("[RebalancePatches] Gene Fabrication: cleared the redundant archogenetics "
                    + $"prerequisite from {cleared} genepack recipes, which the fabricator's own research already gates.");
        }

        private static void GeneFabricationDefPostfix(RecipeDef __result)
        {
            if (__result?.defName != null && __result.defName.StartsWith(RecipePrefix))
                __result.researchPrerequisite = null;
        }
    }
}
