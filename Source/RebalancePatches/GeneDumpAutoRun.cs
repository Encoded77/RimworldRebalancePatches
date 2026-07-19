using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    // With dev mode and the dev.genedump setting on, refresh GeneDump.json automatically
    // at the main menu so the dump is always current truth. Runs on the second menu frame: Cherry Picker
    // executes its removals in a first-frame postfix on the same method, and the
    // dump must reflect the post-removal gene pool.
    internal static class GeneDumpAutoRun
    {
        private static int menuFrames;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI)),
                    postfix: new HarmonyMethod(typeof(GeneDumpAutoRun), nameof(Postfix)));
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] Gene dump auto-run patch failed: " + e);
            }
        }

        private static void Postfix()
        {
            if (++menuFrames < 2)
                return;
            if (Prefs.DevMode && SettingsRegistry.GetEffective("dev.genedump"))
                GeneDump.DumpGeneDatabase();
            new Harmony("encoded.rebalancepatches.genedump")
                .Unpatch((MethodBase)AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI)),
                    HarmonyPatchType.Postfix, "encoded.rebalancepatches");
        }
    }
}
