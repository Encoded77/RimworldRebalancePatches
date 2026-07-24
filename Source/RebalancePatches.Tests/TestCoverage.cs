using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RebalancePatches.Tests
{
    public static class TestCoverage
    {
        private static readonly HashSet<string> ran = new HashSet<string>();
        private static readonly Dictionary<string, string> missingMod = new Dictionary<string, string>();
        private static readonly HashSet<string> toggleOff = new HashSet<string>();

        private static System.DateTime lastRecord = System.DateTime.MinValue;

        private static readonly Dictionary<string, string> testStatus = new Dictionary<string, string>();
        private static string currentTest = "?";

        private static readonly HashSet<string> asserted = new HashSet<string>();

        // Tests that called SoftResult, so recorded failures were actually surfaced to the runner.
        private static readonly HashSet<string> surfaced = new HashSet<string>();

        public static void SoftResultCalled() => surfaced.Add(currentTest);

        private static List<string> UnsurfacedFailures() => failures
            .Where(p => p.Value.Count > 0 && !surfaced.Contains(p.Key))
            .Select(p => $"{p.Key}  ({p.Value.Count} failure(s) recorded, test did not go red)")
            .OrderBy(k => k, System.StringComparer.Ordinal)
            .ToList();

        public static void BeginTest(string testName, string settingKey)
        {
            BeginRunIfStale();
            currentTest = testName ?? "?";
            surfaced.Remove(currentTest);
            asserted.Remove(currentTest);
            testStatus[currentTest] = $"started    {settingKey}";
        }

        public static void Asserted() => asserted.Add(currentTest);

        private static readonly Dictionary<string, List<string>> failures = new Dictionary<string, List<string>>();

        public static void Failed(string message)
        {
            if (!failures.TryGetValue(currentTest, out List<string> list))
                failures[currentTest] = list = new List<string>();
            list.Add(message);
            testStatus[currentTest] = list.Count == 1
                ? $"FAILED     {message}"
                : $"FAILED     ({list.Count} problems) {list[0]}";
            Report();
        }

        /// <summary>How many failures the current test has recorded so far.</summary>
        public static int FailureCount() =>
            failures.TryGetValue(currentTest, out List<string> list) ? list.Count : 0;

        /// <summary>Extra context a test wants in the report only when something went wrong.</summary>
        public static void Note(string message)
        {
            if (!notes.TryGetValue(currentTest, out List<string> list))
                notes[currentTest] = list = new List<string>();
            list.Add(message);
        }

        private static readonly Dictionary<string, List<string>> notes = new Dictionary<string, List<string>>();

        private static void BeginRunIfStale()
        {
            System.DateTime now = System.DateTime.Now;
            if (lastRecord != System.DateTime.MinValue && (now - lastRecord).TotalSeconds > 10)
            {
                ran.Clear();
                toggleOff.Clear();
                missingMod.Clear();
                testStatus.Clear();
                failures.Clear();
                notes.Clear();
                asserted.Clear();
                surfaced.Clear();
            }
            lastRecord = now;
        }

        public static void Ran(string settingKey)
        {
            BeginRunIfStale();
            ran.Add(settingKey);
            testStatus[currentTest] = $"ran        {settingKey}";
            Report();
        }

        public static void SkippedMissingMod(string settingKey, string modId)
        {
            BeginRunIfStale();
            missingMod[settingKey] = modId;
            testStatus[currentTest] = $"skip-mod   {settingKey} ({modId})";
            Log.Message($"[RBP Tests] SKIP {settingKey}: mod '{modId}' not active");
            Report();
        }

        public static void SkippedToggleOff(string settingKey)
        {
            BeginRunIfStale();
            toggleOff.Add(settingKey);
            testStatus[currentTest] = $"skip-off   {settingKey}";
            Log.Warning($"[RBP Tests] SKIP {settingKey}: toggle disabled - feature NOT tested this run");
            Report();
        }

        public static void Report()
        {
            var allKeys = AllSettingKeys();
            var never = NeverReached(allKeys);
            var noAssert = NoAssert();
            int failed = FailedTests();
            string summary = $"COVERAGE: {ran.Count} exercised, {toggleOff.Count} skipped (toggle off), " +
                             $"{missingMod.Count} skipped (mod absent), {never.Count} with no test reached, " +
                             $"of {allKeys.Count} registered toggles.  TESTS: {testStatus.Count} seen, " +
                             $"{failed} FAILED, {noAssert.Count} ran without asserting anything.";
            WriteReportFile(summary, never, noAssert);
        }

        private static List<string> AllSettingKeys() => SettingsRegistry.Groups
            .SelectMany(g => g.children.Select(c => c.key))
            .Distinct()
            .ToList();

        private static List<string> NeverReached(List<string> allKeys) => allKeys
            .Where(k => !ran.Contains(k) && !toggleOff.Contains(k) && !missingMod.ContainsKey(k))
            .OrderBy(k => k, System.StringComparer.Ordinal)
            .ToList();

        // A test that ran but never asserted is reported separately: it is green and worthless.
        private static List<string> NoAssert() => testStatus
            .Where(p => p.Value.StartsWith("ran") && !asserted.Contains(p.Key))
            .Select(p => p.Key)
            .OrderBy(k => k, System.StringComparer.Ordinal)
            .ToList();

        private static int FailedTests() => testStatus.Values.Count(v => v.StartsWith("FAILED"));

        public static Snapshot Take()
        {
            var allKeys = AllSettingKeys();
            var snapshot = new Snapshot
            {
                seen = testStatus.Count,
                exercised = ran.Count,
                failed = FailedTests(),
                skippedModAbsent = missingMod.Count,
                skippedToggleOff = toggleOff.Count,
                registeredToggles = allKeys.Count,
                noTestReached = NeverReached(allKeys),
                ranWithoutAsserting = NoAssert(),
                unsurfaced = UnsurfacedFailures(),
                toggleOffKeys = toggleOff.OrderBy(k => k, System.StringComparer.Ordinal).ToList(),
            };
            foreach (var pair in failures.OrderBy(p => p.Key, System.StringComparer.Ordinal))
            {
                var record = new FailureRecord { test = pair.Key, messages = new List<string>(pair.Value) };
                if (notes.TryGetValue(pair.Key, out List<string> ns))
                    record.notes = new List<string>(ns);
                snapshot.failures.Add(record);
            }
            return snapshot;
        }

        /// <summary>One test's recorded failures, plus the context it asked to have shown alongside.</summary>
        public sealed class FailureRecord
        {
            public string test;
            public List<string> messages = new List<string>();
            public List<string> notes = new List<string>();
        }

        public sealed class Snapshot
        {
            public int seen;
            public int exercised;
            public int failed;
            public int skippedModAbsent;
            public int skippedToggleOff;
            public int registeredToggles;
            public List<string> noTestReached = new List<string>();
            public List<string> ranWithoutAsserting = new List<string>();
            public List<string> unsurfaced = new List<string>();
            public List<string> toggleOffKeys = new List<string>();
            public List<FailureRecord> failures = new List<FailureRecord>();
        }

        private static void WriteReportFile(string summary, List<string> never, List<string> noAssert)
        {
            try
            {
                string dir = System.IO.Path.Combine(GenFilePaths.SaveDataFolderPath, "RebalancePatches", "tmp");
                System.IO.Directory.CreateDirectory(dir);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"RimTest coverage - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(summary);
                sb.AppendLine();
                sb.AppendLine("Rewritten after every recorded result. A gap of more than 10s starts a new run,");
                sb.AppendLine("so this reflects the most recent manual run rather than every run since launch.");
                sb.AppendLine();

                if (failures.Count > 0)
                {
                    sb.AppendLine("== FAILURE DETAIL ==");
                    foreach (var pair in failures.OrderBy(p => p.Key, System.StringComparer.Ordinal))
                    {
                        sb.AppendLine($"  {pair.Key}");
                        foreach (string msg in pair.Value)
                            sb.AppendLine($"      - {msg}");
                        if (notes.TryGetValue(pair.Key, out List<string> ns))
                            foreach (string n in ns)
                                sb.AppendLine($"      note: {n}");
                        sb.AppendLine();
                    }
                    sb.AppendLine();
                }

                Section(sb, "FAILURES NOT SURFACED (green in the runner, broken in fact - add Check.SoftResult)",
                    UnsurfacedFailures());
                Section(sb, "RAN BUT ASSERTED NOTHING (green and worthless)", noAssert);
                Section(sb, "FAILED", testStatus
                    .Where(p => p.Value.StartsWith("FAILED"))
                    .OrderBy(p => p.Key, System.StringComparer.Ordinal)
                    .Select(p => $"{p.Key}  {p.Value}"));
                Section(sb, "NOT TESTED (mods present, toggle off)",
                    toggleOff.OrderBy(k => k, System.StringComparer.Ordinal));
                Section(sb, "NO TEST REACHED (no suite called Check.Ready)", never);
                Section(sb, "skipped - mod absent",
                    missingMod.OrderBy(p => p.Key, System.StringComparer.Ordinal).Select(p => $"{p.Key}  ({p.Value})"));
                Section(sb, "exercised", ran.OrderBy(k => k, System.StringComparer.Ordinal));
                Section(sb, "every test seen, by name", testStatus
                    .OrderBy(p => p.Key, System.StringComparer.Ordinal)
                    .Select(p => $"{p.Key.PadRight(52)} {p.Value}"));

                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "TestCoverage.txt"),
                    sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (System.Exception e)
            {
                Log.Warning("[RBP Tests] Could not write TestCoverage.txt: " + e.Message);
            }
        }

        private static void Section(System.Text.StringBuilder sb, string title, IEnumerable<string> lines)
        {
            var list = lines.ToList();
            sb.AppendLine($"== {title} - {list.Count} ==");
            foreach (string line in list) sb.AppendLine("  " + line);
            sb.AppendLine();
        }
    }
}
