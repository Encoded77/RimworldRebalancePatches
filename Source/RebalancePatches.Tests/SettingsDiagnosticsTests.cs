using System;
using System.IO;
using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SettingsDiagnosticsTests
    {
        [Test]
        public static void EffectiveSignatureIsStableAndCoversEveryKey()
        {
            string a = SettingsDiagnostics.EffectiveSignature();
            string b = SettingsDiagnostics.EffectiveSignature();
            Check.Soft(a == b, "EffectiveSignature is not deterministic across calls");
            Check.Soft(!string.IsNullOrEmpty(a), "EffectiveSignature is empty");

            // Every registered group, toggle and slider must appear, or a change to one would be
            // invisible to the staleness check and the Missile Girl cache would never reset for it.
            int expected = 0;
            foreach (RebalanceGroup g in SettingsRegistry.Groups)
                expected += 1 + g.children.Count + g.sliders.Count;
            int actual = a.Length == 0 ? 0 : a.Split(';').Length;
            Check.Soft(actual == expected,
                $"signature lists {actual} keys, expected {expected} (each group plus its toggles and sliders)");
            Check.SoftResult();
        }

        [Test]
        public static void ChangedKeysReportsOnlyRealDifferences()
        {
            Check.Soft(SettingsDiagnostics.ChangedKeys("a=1;b=0", "a=1;b=0").Count == 0,
                "identical signatures reported a change");

            // b flips, c is removed, d is added; a is unchanged.
            System.Collections.Generic.List<string> changed =
                SettingsDiagnostics.ChangedKeys("a=1;b=0;c=5", "a=1;b=1;d=2");
            Check.Soft(changed.Contains("b") && changed.Contains("c") && changed.Contains("d") && !changed.Contains("a"),
                $"ChangedKeys returned [{string.Join(",", changed)}], expected b, c and d only");
            Check.SoftResult();
        }

        [Test]
        public static void CacheResetDeletesTheKnownFilesAndToleratesAbsence()
        {
            string dir = Path.Combine(Path.GetTempPath(), "RBP_MGCacheTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                // An absent cache is a clean no-op, never a throw.
                Check.Soft(!GagarinCache.PresentAt(dir), "reported a cache present in an empty temp dir");
                Check.Soft(GagarinCache.ResetAt(dir) == 0, "claimed to delete files from a non-existent cache");

                Directory.CreateDirectory(dir);
                foreach (string f in new[] { "Unified.xml", "Unified_Original.xml", "AssetsHash.xml", "AssetsHashInt.xml" })
                    File.WriteAllText(Path.Combine(dir, f), "x");
                File.WriteAllText(Path.Combine(dir, "Textures.dat"), "keep");   // unrelated, must survive

                Check.Soft(GagarinCache.PresentAt(dir), "did not detect a populated cache");
                int removed = GagarinCache.ResetAt(dir);
                Check.Soft(removed == 4, $"reset removed {removed} file(s), expected 4");
                Check.Soft(!GagarinCache.PresentAt(dir), "cache still reads as present after reset");
                Check.Soft(File.Exists(Path.Combine(dir, "Textures.dat")), "reset deleted an unrelated file");
            }
            finally
            {
                try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
            }
            Check.SoftResult();
        }

        [Test]
        public static void DefaultCacheDirSitsBesideConfig()
        {
            string dir = GagarinCache.DefaultCacheDir;
            Check.Soft(dir != null && dir.Replace('\\', '/').EndsWith("MissileGirl/Cache"),
                $"DefaultCacheDir is '{dir}', expected it to end with MissileGirl/Cache");
            Check.SoftResult();
        }
    }
}
