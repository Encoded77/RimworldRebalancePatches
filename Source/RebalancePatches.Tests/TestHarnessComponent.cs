using System;
using Verse;

namespace RebalancePatches.Tests
{
    public class TestHarnessComponent : GameComponent
    {
        private const int SettleFrames = 60;

        private bool armed;
        private bool finished;
        private int frames;

        public TestHarnessComponent(Game game)
        {
        }

        public override void FinalizeInit()
        {
            if (!Harness.Armed)
                return;
            armed = true;
            Harness.Beat("map-ready");
        }

        public override void GameComponentUpdate()
        {
            if (!armed || finished)
                return;
            if (++frames < SettleFrames)
                return;
            finished = true;
            RunAndQuit();
        }

        private static void RunAndQuit()
        {
            HarnessRunner.Result result = null;
            try
            {
                Harness.Beat("tests-running");
                result = HarnessRunner.Run();
                Harness.Beat("tests-done");
            }
            catch (Exception e)
            {
                Log.Error("[RBP Harness] Test run threw: " + e);
            }

            try
            {
                TestRunReport.Write(result == null ? "aborted" : "completed", result);
                Harness.Beat("report-written");
            }
            catch (Exception e)
            {
                Log.Error("[RBP Harness] Writing the run report threw: " + e);
            }

            if (Harness.KeepOpen)
                return;
            Harness.Beat("quitting");
            Root.Shutdown();
        }
    }
}
