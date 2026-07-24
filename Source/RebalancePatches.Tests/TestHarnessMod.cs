using System;
using System.IO;
using UnityEngine;
using Verse;

namespace RebalancePatches.Tests
{
    internal static class Harness
    {
        public const string ArgKey = "rbptest";

        /// <summary>Lets a human watch the same run: everything happens except the quit.</summary>
        public const string KeepOpenArgKey = "rbpkeepopen";

        public const string SelfTestArgKey = "rbpselftest";

        public const string SelfTestFallback = "fallback";
        public const string SelfTestAbort = "abort";

        public const string SelfTestMapGen = "mapgen";

        /// <summary>The forced failure path for this run, or null for an ordinary one.</summary>
        public static string SelfTest { get; private set; }

        public static string RunId { get; private set; }
        public static bool KeepOpen { get; private set; }
        public static DateTime StartedUtc { get; private set; }
        public static string Phase { get; private set; } = "not-armed";

        public static bool Armed => RunId != null;

        public static string TmpDir =>
            Path.Combine(GenFilePaths.SaveDataFolderPath, "RebalancePatches", "tmp");

        public static string RootDir { get; private set; }

        public static void Arm(string runId, bool keepOpen, string rootDir)
        {
            RunId = runId;
            KeepOpen = keepOpen;
            RootDir = rootDir;
            StartedUtc = DateTime.UtcNow;
            SelfTest = GenCommandLine.TryGetCommandLineArg(SelfTestArgKey, out string mode) && !mode.NullOrEmpty()
                ? mode
                : null;
            if (SelfTest != null)
                Log.Warning($"[RBP Harness] SELF-TEST '{SelfTest}': this run deliberately takes a failure path.");
        }

        public static string AssemblyPath()
        {
            try
            {
                if (RootDir.NullOrEmpty())
                    return null;
                string path = Path.Combine(RootDir, "Assemblies",
                    typeof(Harness).Assembly.GetName().Name + ".dll");
                return File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        public static void Beat(string phase)
        {
            Phase = phase;
            if (!Armed)
                return;
            try
            {
                Directory.CreateDirectory(TmpDir);
                File.WriteAllText(Path.Combine(TmpDir, "heartbeat.txt"),
                    $"{RunId} {phase} {DateTime.UtcNow:o}", System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                Log.Warning($"[RBP Harness] Could not write heartbeat '{phase}': {e.Message}");
            }
        }
    }

    public class TestHarnessMod : Mod
    {
        public TestHarnessMod(ModContentPack content) : base(content)
        {
            if (!GenCommandLine.TryGetCommandLineArg(Harness.ArgKey, out string runId) || runId.NullOrEmpty())
                return;

            Harness.Arm(runId, GenCommandLine.CommandLineArgPassed(Harness.KeepOpenArgKey), content?.RootDir);
            HarnessLogTap.Register();
            Harness.Beat("mod-init");

            HarnessMenuStarter.TryApply(new HarmonyLib.Harmony("encoded.rebalancepatches.harness"));

            LongEventHandler.ExecuteWhenFinished(() => Harness.Beat("defs-ready"));

            Application.quitting += WriteAbortedReport;
        }

        private static void WriteAbortedReport()
        {
            if (TestRunReport.Written)
                return;
            TestRunReport.Write("aborted", null);
        }
    }
}
