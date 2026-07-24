using System;
using System.IO;
using System.Reflection;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    internal static class TestRunReport
    {
        public const int SchemaVersion = 1;

        public static bool Written { get; private set; }

        public static void Write(string outcome, HarnessRunner.Result result)
        {
            if (!Harness.Armed)
                return;
            try
            {
                string dir = Harness.TmpDir;
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "TestRun.json"), Build(outcome, result),
                    System.Text.Encoding.UTF8);
                Written = true;
            }
            catch (Exception e)
            {
                Log.Error("[RBP Harness] Could not write TestRun.json: " + e);
            }
        }

        private static string Build(string outcome, HarnessRunner.Result result)
        {
            var json = new Json();
            json.BeginObject();

            json.Name("schemaVersion"); json.Number(SchemaVersion);
            json.Name("runId"); json.Value(Harness.RunId);
            json.Name("startedUtc"); json.Value(Harness.StartedUtc.ToString("o"));
            json.Name("finishedUtc"); json.Value(DateTime.UtcNow.ToString("o"));
            json.Name("outcome"); json.Value(outcome);
            json.Name("phase"); json.Value(Harness.Phase);
            json.Name("runner"); json.Value(result == null ? null : result.runner);
            json.Name("runnerNote"); json.Value(result == null ? null : result.note);
            json.Name("runnerStatus"); json.Value(result == null ? null : result.runnerStatus);

            TestCoverage.Report();
            TestCoverage.Snapshot snapshot = TestCoverage.Take();
            WriteAssembly(json);
            WriteGame(json);
            WriteCounts(json, snapshot, result);
            WriteFailures(json, snapshot, result);
            WriteErrors(json);

            json.EndObject();
            return json.ToString();
        }

        private static void WriteAssembly(Json json)
        {
            json.Name("assembly");
            json.BeginObject();
            Assembly ours = typeof(TestRunReport).Assembly;
            string path = null;
            try { path = ours.Location; } catch { }
            if (string.IsNullOrEmpty(path))
                path = Harness.AssemblyPath();
            json.Name("path"); json.Value(string.IsNullOrEmpty(path) ? null : path);
            json.Name("lastWriteUtc");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                json.Value(new FileInfo(path).LastWriteTimeUtc.ToString("o"));
            else
                json.Null();
            json.Name("version"); json.Value(ours.GetName().Version?.ToString());
            json.Name("informationalVersion");
            json.Value(ours.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            json.EndObject();
        }

        private static void WriteGame(Json json)
        {
            json.Name("game");
            json.BeginObject();
            json.Name("version"); json.Value(VersionControl.CurrentVersionStringWithRev);
            json.Name("programState"); json.Value(Current.ProgramState.ToString());
            json.Name("mapCount"); json.Number(Current.Game?.Maps?.Count ?? 0);
            json.Name("activeModCount"); json.Number(LoadedModManager.RunningModsListForReading.Count);
            json.EndObject();
        }

        private static void WriteCounts(Json json, TestCoverage.Snapshot snapshot, HarnessRunner.Result result)
        {
            json.Name("counts");
            json.BeginObject();
            json.Name("seen"); json.Number(snapshot.seen);
            json.Name("ran"); json.Number(snapshot.exercised);
            json.Name("failed"); json.Number(snapshot.failed + (result == null ? 0 : result.fallbackFailures.Count));
            json.Name("skippedModAbsent"); json.Number(snapshot.skippedModAbsent);
            json.Name("skippedToggleOff"); json.Number(snapshot.skippedToggleOff);
            json.Name("ranWithoutAsserting"); json.Number(snapshot.ranWithoutAsserting.Count);
            json.Name("unsurfaced"); json.Number(snapshot.unsurfaced.Count);
            json.Name("noTestReached"); json.Number(snapshot.noTestReached.Count);
            json.Name("registeredToggles"); json.Number(snapshot.registeredToggles);
            json.Name("suitesFound"); json.Number(result == null ? 0 : result.suitesFound);
            json.Name("testsFound"); json.Number(result == null ? 0 : result.testsFound);
            json.EndObject();

            json.Name("lists");
            json.BeginObject();
            WriteStrings(json, "toggleOff", snapshot.toggleOffKeys);
            WriteStrings(json, "ranWithoutAsserting", snapshot.ranWithoutAsserting);
            WriteStrings(json, "unsurfaced", snapshot.unsurfaced);
            json.EndObject();
        }

        private static void WriteFailures(Json json, TestCoverage.Snapshot snapshot, HarnessRunner.Result result)
        {
            json.Name("failures");
            json.BeginArray();
            foreach (TestCoverage.FailureRecord record in snapshot.failures)
                WriteFailure(json, record);
            if (result != null)
                foreach (TestCoverage.FailureRecord record in result.fallbackFailures)
                    WriteFailure(json, record);
            json.EndArray();
        }

        private static void WriteFailure(Json json, TestCoverage.FailureRecord record)
        {
            json.BeginObject();
            json.Name("test"); json.Value(record.test);
            WriteStrings(json, "messages", record.messages);
            WriteStrings(json, "notes", record.notes);
            json.EndObject();
        }

        private static void WriteErrors(Json json)
        {
            json.Name("errorsTruncated"); json.Value(HarnessLogTap.Truncated);
            json.Name("errors");
            json.BeginArray();
            foreach (HarnessLogTap.Entry entry in HarnessLogTap.Entries)
            {
                json.BeginObject();
                json.Name("level"); json.Value(entry.level);
                json.Name("count"); json.Number(entry.count);
                json.Name("text"); json.Value(entry.text);
                WriteStrings(json, "frames", entry.frames);
                json.EndObject();
            }
            json.EndArray();
        }

        private static void WriteStrings(Json json, string name, System.Collections.Generic.List<string> values)
        {
            json.Name(name);
            json.BeginArray();
            if (values != null)
                foreach (string value in values)
                    json.Value(value);
            json.EndArray();
        }
    }
}
