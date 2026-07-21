using System;
using System.IO;
using System.Text;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Shared plumbing for the dev database dumps: output path, the meta block, the referenced-def
    /// block, error handling and the log line. Each dump supplies only its own sections.
    ///
    /// The dumps are deliberately one-def-type-each. Cross-def work — resolving a recipe's research
    /// chain, checking which bodies carry a part, deciding whether an android may take a surgery —
    /// belongs in the offline analyzer, which can join several dumps and be re-run in a second.
    /// Baking those joins into a dump means restarting RimWorld to change your mind about a rule.
    /// </summary>
    internal static class DumpRunner
    {
        public static string TmpDir =>
            Path.Combine(GenFilePaths.SaveDataFolderPath, "RebalancePatches", "tmp");

        /// <param name="fileName">Output file, e.g. "RecipeDump.json".</param>
        /// <param name="walker">Walker configured with this dump's referenced def types.</param>
        /// <param name="writeSections">Writes the dump's own named sections into the open object.</param>
        /// <param name="summary">One-line count summary for the log, evaluated after writing.</param>
        public static void Run(string fileName, DefWalker walker, Action<DefWalker> writeSections, Func<string> summary)
        {
            try
            {
                Directory.CreateDirectory(TmpDir);
                string path = Path.Combine(TmpDir, fileName);

                Json j = walker.Json;
                j.BeginObject();
                WriteMeta(j);
                writeSections(walker);
                walker.WriteReferencedBlock("referenced");
                j.EndObject();

                File.WriteAllText(path, j.ToString(), Encoding.UTF8);
                Log.Message($"[RebalancePatches] {fileName}: {summary()}, {walker.ReferencedCount} referenced -> {path}");
            }
            catch (Exception e)
            {
                Log.Error($"[RebalancePatches] {fileName} failed: {e}");
            }
        }

        public static void WriteMeta(Json j)
        {
            j.Name("meta");
            j.BeginObject();
            j.Name("gameVersion"); j.Value(VersionControl.CurrentVersionStringWithRev);
            j.Name("dumpedAt"); j.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            j.Name("activeMods");
            j.BeginArray();
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
                j.Value(mod.PackageIdPlayerFacing);
            j.EndArray();
            j.EndObject();
        }

        /// <summary>Mod id in the form the dumps and analyzers key on.</summary>
        public static string ModOf(Def def) =>
            def?.modContentPack != null
                ? $"{def.modContentPack.Name} [{def.modContentPack.PackageIdPlayerFacing}]"
                : "?";
    }
}
