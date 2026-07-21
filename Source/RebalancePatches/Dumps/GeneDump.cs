using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches
{
    internal static class GeneDump
    {
        private const int MaxDepth = 8;

        private static readonly Type[] ReferencedDefTypes =
        {
            typeof(HediffDef), typeof(AbilityDef), typeof(ThoughtDef), typeof(NeedDef), typeof(MentalBreakDef),
        };

        [DebugAction("RebalancePatches", "Dump gene database", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpGeneDatabaseEntry() => DumpGeneDatabase();

        [DebugAction("RebalancePatches", "Dump gene database", allowedGameStates = AllowedGameStates.Playing)]
        internal static void DumpGeneDatabase()
        {
            try
            {
                string dir = Path.Combine(GenFilePaths.SaveDataFolderPath, "RebalancePatches", "tmp");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "GeneDump.json");
                var ctx = new DumpContext();
                WriteDump(ctx);
                File.WriteAllText(path, ctx.Json.ToString(), Encoding.UTF8);
                Log.Message($"[RebalancePatches] Gene dump: {ctx.GeneCount} genes, {ctx.XenotypeCount} xenotypes, {ctx.ReferencedCount} referenced defs, {ctx.AcquisitionHits} acquisition hits -> {path}");
            }
            catch (Exception e)
            {
                Log.Error($"[RebalancePatches] Gene dump failed: {e}");
            }
        }

        private sealed class DumpContext
        {
            public readonly DefWalker Walker = new DefWalker(
                ReferencedDefTypes,
                bareDefTypes: new[] { typeof(GeneDef), typeof(XenotypeDef) },
                maxDepth: MaxDepth);
            public Json Json => Walker.Json;
            public int GeneCount, XenotypeCount, AcquisitionHits;
            public int ReferencedCount => Walker.ReferencedCount;
        }

        private static void WriteDump(DumpContext ctx)
        {
            Json j = ctx.Json;
            j.BeginObject();

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

            j.Name("genes");
            j.BeginArray();
            foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefsListForReading.OrderBy(g => g.defName, StringComparer.Ordinal))
            {
                ctx.Walker.WriteDefEntry(gene, _ =>
                {
                    if (!gene.renderNodeProperties.NullOrEmpty())
                    {
                        j.Name("hasGraphics"); j.Value(true);
                    }
                });
                ctx.GeneCount++;
            }
            j.EndArray();

            j.Name("xenotypes");
            j.BeginArray();
            foreach (XenotypeDef xeno in DefDatabase<XenotypeDef>.AllDefsListForReading.OrderBy(x => x.defName, StringComparer.Ordinal))
            {
                ctx.Walker.WriteDefEntry(xeno);
                ctx.XenotypeCount++;
            }
            j.EndArray();

            ctx.Walker.WriteReferencedBlock("referenced");

            WriteAcquisition(ctx);

            j.EndObject();
        }

        /// <summary>
        /// Where genes and xenotypes are handed out from. Scans only the ThingDef fields that can
        /// carry a gene — a full sweep pulls in every recipe ingredient's parent chain and buries
        /// the real sources — but every field of a recipe, since those reference genes anywhere.
        /// </summary>
        private static void WriteAcquisition(DumpContext ctx)
        {
            var results = new DefRefScanner.Results();
            Func<Def, bool> isTarget = def => def is GeneDef || def is XenotypeDef;

            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var visited = new HashSet<object>(ReferenceComparer.Instance);
                string root = $"ThingDef:{thing.defName}";
                string mod = thing.modContentPack?.Name ?? "?";
                DefRefScanner.Scan(results, thing.comps, root, mod, "comps", isTarget, visited, 0, MaxDepth);
                DefRefScanner.Scan(results, thing.modExtensions, root, mod, "modExtensions", isTarget, visited, 0, MaxDepth);
                DefRefScanner.Scan(results, thing.ingestible, root, mod, "ingestible", isTarget, visited, 0, MaxDepth);
            }
            DefRefScanner.ScanAllFields(results, DefDatabase<RecipeDef>.AllDefsListForReading.Cast<Def>(), isTarget, MaxDepth);

            DefRefScanner.Write(ctx.Json, "acquisition", results);
            ctx.AcquisitionHits = results.Count;
        }
    }
}
