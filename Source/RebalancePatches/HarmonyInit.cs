using HarmonyLib;
using RebalancePatches.Mods.AlteredCarbon;
using RebalancePatches.Mods.Biotech;
using RebalancePatches.Mods.GeneFabrication;
using RebalancePatches.Mods.Inspirations;
using RebalancePatches.Mods.IntegratedImplants;
using RebalancePatches.Mods.ResearchDisplay;
using RebalancePatches.Mods.ResearchUI;
using RebalancePatches.Mods.VQEAncients;
using RebalancePatches.Mods.VREAndroid;
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
        private static bool earlyApplied;

        public static void EnsureEarlyApplied()
        {
            if (earlyApplied)
                return;
            earlyApplied = true;
            Mods.VFEDeserters.VfedContrabandPatches.TryApply(new Harmony("encoded.rebalancepatches.early"));
        }

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
            NpcModuleHostPatches.TryApply(harmony);
            ModuleQuality.TryApply(harmony);
            AndroidPsylinkPatches.TryApply(harmony);
            AndroidHardwareResearchGate.TryApply(harmony);
            EmptyResearchTabPatches.TryApply(harmony);
            YartUnlockGrouping.TryApply(harmony);
            DefCleanup.TryApply();
            DumpAutoRun.TryApply(harmony);
        }
    }
}
