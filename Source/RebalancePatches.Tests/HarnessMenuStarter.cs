using System;
using HarmonyLib;
using RimWorld;
using UnityEngine.SceneManagement;
using Verse;

namespace RebalancePatches.Tests
{
    internal static class HarnessMenuStarter
    {
        private const int MenuFrames = 5;

        private static int frames;
        private static bool started;

        public static void TryApply(Harmony harmony)
        {
            if (!Harness.Armed)
                return;
            try
            {
                harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI)),
                    postfix: new HarmonyMethod(typeof(HarnessMenuStarter), nameof(MenuFramePostfix)));

                harmony.Patch(
                    AccessTools.Method(typeof(GameAndMapInitExceptionHandlers),
                        nameof(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap)),
                    postfix: new HarmonyMethod(typeof(HarnessMenuStarter), nameof(MapGenFailedPostfix)));
            }
            catch (Exception e)
            {
                Log.Error("[RBP Harness] Could not hook the main menu, so no game will be started: " + e);
            }
        }

        private static void MapGenFailedPostfix(Exception e)
        {
            if (!Harness.Armed)
                return;
            try
            {
                Log.Error("[RBP Harness] World or map generation failed, ending the run: " + e);
                Harness.Beat("mapgen-failed");
                TestRunReport.Write("mapgen-failed", null);
            }
            catch (Exception writeFailure)
            {
                Log.Error("[RBP Harness] Could not write the mapgen-failed report: " + writeFailure);
            }
            if (!Harness.KeepOpen)
                Root.Shutdown();
        }

        private static void MenuFramePostfix()
        {
            if (started || !Harness.Armed)
                return;
            if (++frames < MenuFrames)
                return;

            started = true;
            try
            {
                Harness.Beat("menu-ready");

                if (Harness.SelfTest == Harness.SelfTestAbort)
                {
                    Log.Warning("[RBP Harness] SELF-TEST 'abort': quitting before the map.");
                    Root.Shutdown();
                    return;
                }

                if (Harness.SelfTest == Harness.SelfTestMapGen)
                {
                    Log.Warning("[RBP Harness] SELF-TEST 'mapgen': invoking the worldgen failure handler.");
                    GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap(
                        new Exception("self-test: simulated world generation failure"));
                    return;
                }
                SceneManager.LoadScene("Play");
            }
            catch (Exception e)
            {
                Log.Error("[RBP Harness] Starting the test game failed: " + e);
            }
        }
    }
}
