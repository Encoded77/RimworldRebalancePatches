using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Every way an item reaches the player other than building it: trader stock, reward and loot
    /// tables, quests, scenarios, and the gear pawns spawn wearing. Not implant-specific — the same
    /// file answers "can this weapon be bought" and "does this armor drop from raiders".
    ///
    /// This matters because gating an item behind expensive research means nothing if a trader sells
    /// it at tier one, which is the class of hole already found in the merchant work.
    ///
    /// Trader coverage is asked of the live StockGenerator rather than inferred from its fields:
    /// generators select by category, tech level, tradeTags and price bands, and reimplementing that
    /// offline would be wrong in exactly the cases that matter.
    /// </summary>
    internal static class AcquisitionDump
    {
        [DebugAction("RebalancePatches", "Dump acquisition", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump acquisition", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(new Type[0]);
            int traded = 0;
            var refs = new DefRefScanner.Results();

            DumpRunner.Run("AcquisitionDump.json", walker, w =>
            {
                Json j = w.Json;

                var items = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(t => t.category == ThingCategory.Item)
                    .OrderBy(t => t.defName, StringComparer.Ordinal)
                    .ToList();
                var itemSet = new HashSet<Def>(items.Cast<Def>());

                // --- who sells what ---
                var sellers = new Dictionary<ThingDef, List<(string Trader, string Gen)>>();
                foreach (TraderKindDef trader in DefDatabase<TraderKindDef>.AllDefsListForReading)
                {
                    if (trader.stockGenerators == null) continue;
                    foreach (StockGenerator gen in trader.stockGenerators)
                    {
                        if (gen == null) continue;
                        foreach (ThingDef item in items)
                        {
                            bool handles;
                            try { handles = gen.HandlesThingDef(item); }
                            catch { continue; }
                            if (!handles) continue;
                            if (!sellers.TryGetValue(item, out var list)) sellers[item] = list = new();
                            list.Add((trader.defName, gen.GetType().Name));
                        }
                    }
                }

                j.Name("traders");
                j.BeginObject();
                foreach (var pair in sellers.OrderBy(p => p.Key.defName, StringComparer.Ordinal))
                {
                    j.Name(pair.Key.defName);
                    j.BeginArray();
                    foreach (var entry in pair.Value)
                    {
                        j.BeginObject();
                        j.Name("trader"); j.Value(entry.Trader);
                        j.Name("generator"); j.Value(entry.Gen);
                        j.EndObject();
                    }
                    j.EndArray();
                    traded++;
                }
                j.EndObject();

                // --- reward, loot, quest, scenario and spawn-gear references ---
                DefRefScanner.ScanAllFields(refs, DefDatabase<ThingSetMakerDef>.AllDefsListForReading.Cast<Def>(), itemSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<QuestScriptDef>.AllDefsListForReading.Cast<Def>(), itemSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<ScenarioDef>.AllDefsListForReading.Cast<Def>(), itemSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<PawnKindDef>.AllDefsListForReading.Cast<Def>(), itemSet.Contains);
                DefRefScanner.ScanAllFields(refs, DefDatabase<FactionDef>.AllDefsListForReading.Cast<Def>(), itemSet.Contains);
                DefRefScanner.Write(j, "references", refs);
            }, () => $"{traded} tradeable items, {refs.Count} reward and spawn references");
        }
    }
}
