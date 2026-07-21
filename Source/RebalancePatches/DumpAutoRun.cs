using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    // With dev mode and the matching dev setting on, refresh the database dumps automatically at the
    // main menu so they are always current truth. Runs on the second menu frame: Cherry Picker
    // executes its removals in a first-frame postfix on the same method, and the dumps must reflect
    // the post-removal def pool.
    internal static class DumpAutoRun
    {
        private static int menuFrames;

        public static void TryApply(Harmony harmony)
        {
            try
            {
                harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI)),
                    postfix: new HarmonyMethod(typeof(DumpAutoRun), nameof(Postfix)));
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] Dump auto-run patch failed: " + e);
            }
        }

        private static void Postfix()
        {
            if (++menuFrames < 2)
                return;
            if (Prefs.DevMode)
            {
                if (SettingsRegistry.GetEffective("dev.genedump"))
                    GeneDump.DumpGeneDatabase();
                if (SettingsRegistry.GetEffective("dev.xenofactiondump"))
                    FactionXenotypeDump.DumpXenotypeFactions();
            }
            new Harmony("encoded.rebalancepatches.dumpautorun")
                .Unpatch((MethodBase)AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI)),
                    HarmonyPatchType.Postfix, "encoded.rebalancepatches");
        }
    }
}
