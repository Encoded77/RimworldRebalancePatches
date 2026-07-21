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

        private static void Run(string setting, Action dump)
        {
            if (!SettingsRegistry.GetEffective(setting))
                return;
            try { dump(); }
            catch (Exception e) { Log.Error($"[RebalancePatches] Dump '{setting}' failed: {e}"); }
        }

        private static void Postfix()
        {
            if (++menuFrames < 2)
                return;
            if (Prefs.DevMode)
            {
                Run("dev.genedump", GeneDump.DumpGeneDatabase);
                Run("dev.xenofactiondump", FactionXenotypeDump.DumpXenotypeFactions);
                Run("dev.recipedump", RecipeDump.Dump);
                Run("dev.hediffdump", HediffDump.Dump);
                Run("dev.researchdump", ResearchDump.Dump);
                Run("dev.thingdump", ThingDump.Dump);
                Run("dev.bodydump", BodyDump.Dump);
                Run("dev.acquisitiondump", AcquisitionDump.Dump);
                Run("dev.modrulesdump", ModRulesDump.Dump);
            }
            // One dump throwing must not stop the rest; each already logs its own failure.
            new Harmony("encoded.rebalancepatches.dumpautorun")
                .Unpatch((MethodBase)AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI)),
                    HarmonyPatchType.Postfix, "encoded.rebalancepatches");
        }
    }
}
