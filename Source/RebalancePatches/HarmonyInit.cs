using HarmonyLib;
using RebalancePatches.Mods.AlteredCarbon;
using RebalancePatches.Mods.Biotech;
using RebalancePatches.Mods.GeneFabrication;
using RebalancePatches.Mods.Inspirations;
using RebalancePatches.Mods.IntegratedImplants;
using RebalancePatches.Mods.VQEAncients;
using Verse;

namespace RebalancePatches
{
    [StaticConstructorOnStartup]
    internal static class HarmonyInit
    {
        static HarmonyInit() => HarmonyBootstrap.EnsureApplied();
    }

    public static class HarmonyBootstrap
    {
        private static bool applied;

        public static void EnsureApplied()
        {
            if (applied)
                return;
            applied = true;
            var harmony = new Harmony("encoded.rebalancepatches");
            DefAttribution.TryApply();
            AlteredCarbonPatches.TryApply(harmony);
            IntegratedImplantsPatches.TryApply(harmony);
            ArchogenWhitelistPatches.TryApply(harmony);
            InspirationNullifyPatches.TryApply(harmony);
            GeneComplexityPatches.TryApply(harmony);
            GeneFabricationPatches.TryApply(harmony);
            DefCleanup.TryApply();
            DumpAutoRun.TryApply(harmony);
        }
    }
}
