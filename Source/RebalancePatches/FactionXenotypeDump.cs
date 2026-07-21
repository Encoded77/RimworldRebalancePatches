using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Dev dump of which xenotypes can spawn in which factions, feeding the xenotype faction audit.
    /// Beyond the raw rosters it derives the two views the audit is actually made against: xenotypes
    /// no faction can roll, and every faction's modded-xenotype total against its baseliner share.
    ///
    /// A xenotype reaches a faction two ways - the faction's own xenotypeSet, or the xenotypeSet of a
    /// PawnKindDef the faction generates - so both are walked. Counting only the first would report
    /// pawnkind-only xenotypes as orphans.
    /// </summary>
    internal static class FactionXenotypeDump
    {
        [DebugAction("RebalancePatches", "Dump xenotype factions", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => DumpXenotypeFactions();

        [DebugAction("RebalancePatches", "Dump xenotype factions", allowedGameStates = AllowedGameStates.Playing)]
        internal static void DumpXenotypeFactions()
        {
            try
            {
                string dir = Path.Combine(GenFilePaths.SaveDataFolderPath, "RebalancePatches", "tmp");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "XenotypeFactionDump.json");
                var index = new Index();
                index.Build();
                var j = new Json();
                Write(j, index);
                File.WriteAllText(path, j.ToString(), Encoding.UTF8);
                Log.Message($"[RebalancePatches] Xenotype faction dump: {index.Factions.Count} factions, "
                    + $"{index.Xenotypes.Count} xenotypes, {index.Orphans.Count} orphans -> {path}");
            }
            catch (Exception e)
            {
                Log.Error($"[RebalancePatches] Xenotype faction dump failed: {e}");
            }
        }

        /// <summary>One route by which a xenotype can be rolled.</summary>
        private sealed class Route
        {
            public FactionDef Faction;      // null when the pawnkind belongs to no faction
            public PawnKindDef Kind;        // null when the chance sits on the faction itself
            public float Chance;
        }

        private sealed class Index
        {
            public readonly List<FactionDef> Factions = new List<FactionDef>();
            public readonly List<XenotypeDef> Xenotypes = new List<XenotypeDef>();
            public readonly List<XenotypeDef> Orphans = new List<XenotypeDef>();
            public readonly Dictionary<XenotypeDef, List<Route>> Routes = new Dictionary<XenotypeDef, List<Route>>();
            public readonly Dictionary<FactionDef, List<PawnKindDef>> KindsByFaction = new Dictionary<FactionDef, List<PawnKindDef>>();
            public readonly Dictionary<XenotypeDef, List<XenotypeDef>> DoubleParents = new Dictionary<XenotypeDef, List<XenotypeDef>>();

            public void Build()
            {
                Factions.AddRange(DefDatabase<FactionDef>.AllDefsListForReading
                    .OrderBy(f => f.defName, StringComparer.Ordinal));
                Xenotypes.AddRange(DefDatabase<XenotypeDef>.AllDefsListForReading
                    .OrderBy(x => x.defName, StringComparer.Ordinal));

                // Which factions can field which pawnkinds. A faction reaches a kind through its
                // group makers (fighters, traders, carriers, guards) or its basic member kind.
                var factionsByKind = new Dictionary<PawnKindDef, List<FactionDef>>();
                foreach (FactionDef faction in Factions)
                {
                    var kinds = new List<PawnKindDef>();
                    if (faction.basicMemberKind != null)
                        kinds.Add(faction.basicMemberKind);
                    if (faction.pawnGroupMakers != null)
                    {
                        foreach (PawnGroupMaker maker in faction.pawnGroupMakers)
                        {
                            AddKinds(kinds, maker.options);
                            AddKinds(kinds, maker.traders);
                            AddKinds(kinds, maker.carriers);
                            AddKinds(kinds, maker.guards);
                        }
                    }
                    KindsByFaction[faction] = kinds;
                    foreach (PawnKindDef kind in kinds)
                    {
                        if (!factionsByKind.TryGetValue(kind, out List<FactionDef> owners))
                            factionsByKind[kind] = owners = new List<FactionDef>();
                        if (!owners.Contains(faction))
                            owners.Add(faction);
                    }
                }

                foreach (FactionDef faction in Factions)
                    foreach (XenotypeChance chance in Chances(faction.xenotypeSet))
                        AddRoute(chance.xenotype, new Route { Faction = faction, Chance = chance.chance });

                foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefsListForReading
                    .OrderBy(k => k.defName, StringComparer.Ordinal))
                {
                    List<XenotypeChance> chances = Chances(kind.xenotypeSet).ToList();
                    if (chances.Count == 0)
                        continue;
                    factionsByKind.TryGetValue(kind, out List<FactionDef> owners);
                    foreach (XenotypeChance chance in chances)
                    {
                        if (owners == null || owners.Count == 0)
                        {
                            AddRoute(chance.xenotype, new Route { Kind = kind, Chance = chance.chance });
                            continue;
                        }
                        foreach (FactionDef owner in owners)
                            AddRoute(chance.xenotype, new Route { Faction = owner, Kind = kind, Chance = chance.chance });
                    }
                }

                // A xenotype listed in another's doubleXenotypeChances can surface whenever that
                // parent rolls, so it is reachable even with no roster of its own.
                foreach (XenotypeDef xeno in Xenotypes)
                {
                    if (xeno.doubleXenotypeChances == null)
                        continue;
                    foreach (XenotypeChance chance in xeno.doubleXenotypeChances)
                    {
                        if (chance?.xenotype == null)
                            continue;
                        if (!DoubleParents.TryGetValue(chance.xenotype, out List<XenotypeDef> parents))
                            DoubleParents[chance.xenotype] = parents = new List<XenotypeDef>();
                        if (!parents.Contains(xeno))
                            parents.Add(xeno);
                    }
                }

                foreach (XenotypeDef xeno in Xenotypes)
                {
                    if (xeno == XenotypeDefOf.Baseliner)
                        continue;
                    if (!Routes.ContainsKey(xeno))
                        Orphans.Add(xeno);
                }
            }

            private static void AddKinds(List<PawnKindDef> kinds, List<PawnGenOption> options)
            {
                if (options == null)
                    return;
                foreach (PawnGenOption option in options)
                    if (option?.kind != null && !kinds.Contains(option.kind))
                        kinds.Add(option.kind);
            }

            private void AddRoute(XenotypeDef xeno, Route route)
            {
                if (xeno == null || xeno == XenotypeDefOf.Baseliner)
                    return;
                if (!Routes.TryGetValue(xeno, out List<Route> list))
                    Routes[xeno] = list = new List<Route>();
                list.Add(route);
            }
        }

        /// <summary>XenotypeSet keeps its list private; the indexer and Count are the public way in.</summary>
        private static IEnumerable<XenotypeChance> Chances(XenotypeSet set)
        {
            if (set == null)
                yield break;
            for (int i = 0; i < set.Count; i++)
            {
                XenotypeChance chance = set[i];
                if (chance?.xenotype != null && chance.xenotype != XenotypeDefOf.Baseliner)
                    yield return chance;
            }
        }

        private static bool IsModded(Def def) => def?.modContentPack != null && !def.modContentPack.IsOfficialMod;

        private static string ModOf(Def def) =>
            def?.modContentPack != null
                ? $"{def.modContentPack.Name} [{def.modContentPack.PackageIdPlayerFacing}]"
                : "?";

        private static void Write(Json j, Index index)
        {
            j.BeginObject();

            j.Name("meta");
            j.BeginObject();
            j.Name("gameVersion"); j.Value(VersionControl.CurrentVersionStringWithRev);
            j.Name("dumpedAt"); j.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            j.Name("factionCount"); j.Number(index.Factions.Count);
            j.Name("xenotypeCount"); j.Number(index.Xenotypes.Count);
            j.Name("orphanCount"); j.Number(index.Orphans.Count);
            j.Name("activeMods");
            j.BeginArray();
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
                j.Value(mod.PackageIdPlayerFacing);
            j.EndArray();
            j.EndObject();

            WriteFactions(j, index);
            WriteXenotypes(j, index);
            WriteDerived(j, index);

            j.EndObject();
        }

        private static void WriteFactions(Json j, Index index)
        {
            j.Name("factions");
            j.BeginArray();
            foreach (FactionDef faction in index.Factions)
            {
                j.BeginObject();
                j.Name("defName"); j.Value(faction.defName);
                j.Name("label"); j.Value(faction.label);
                j.Name("mod"); j.Value(ModOf(faction));
                if (!faction.categoryTag.NullOrEmpty())
                {
                    j.Name("categoryTag"); j.Value(faction.categoryTag);
                }
                if (faction.hidden) { j.Name("hidden"); j.Value(true); }
                if (faction.isPlayer) { j.Name("isPlayer"); j.Value(true); }
                if (!faction.humanlikeFaction) { j.Name("humanlikeFaction"); j.Value(false); }
                if (faction.permanentEnemy) { j.Name("permanentEnemy"); j.Value(true); }

                index.KindsByFaction.TryGetValue(faction, out List<PawnKindDef> kinds);
                j.Name("pawnKindCount"); j.Number(kinds?.Count ?? 0);

                float modded = 0f, official = 0f;
                j.Name("xenotypeSet");
                j.BeginObject();
                foreach (XenotypeChance chance in Chances(faction.xenotypeSet))
                {
                    j.Name(chance.xenotype.defName); j.Number(chance.chance);
                    if (IsModded(chance.xenotype))
                        modded += chance.chance;
                    else
                        official += chance.chance;
                }
                j.EndObject();

                // Xenotypes this faction can only reach through its pawnkinds, which the roster above
                // does not show - they still eat into what actually spawns.
                var viaKinds = new Dictionary<string, float>();
                if (kinds != null)
                {
                    foreach (PawnKindDef kind in kinds)
                    {
                        foreach (XenotypeChance chance in Chances(kind.xenotypeSet))
                        {
                            if (faction.xenotypeSet != null && faction.xenotypeSet.Contains(chance.xenotype))
                                continue;
                            string key = $"{kind.defName}:{chance.xenotype.defName}";
                            if (!viaKinds.ContainsKey(key))
                                viaKinds[key] = chance.chance;
                        }
                    }
                }
                if (viaKinds.Count > 0)
                {
                    j.Name("viaPawnKinds");
                    j.BeginObject();
                    foreach (var pair in viaKinds.OrderBy(p => p.Key, StringComparer.Ordinal))
                    {
                        j.Name(pair.Key); j.Number(pair.Value);
                    }
                    j.EndObject();
                }

                j.Name("moddedTotal"); j.Number(modded);
                j.Name("officialTotal"); j.Number(official);
                j.Name("baselinerChance");
                j.Number(faction.xenotypeSet?.BaselinerChance ?? 1f);
                j.EndObject();
            }
            j.EndArray();
        }

        private static void WriteXenotypes(Json j, Index index)
        {
            j.Name("xenotypes");
            j.BeginArray();
            foreach (XenotypeDef xeno in index.Xenotypes)
            {
                if (xeno == XenotypeDefOf.Baseliner)
                    continue;
                j.BeginObject();
                j.Name("defName"); j.Value(xeno.defName);
                j.Name("label"); j.Value(xeno.label);
                j.Name("mod"); j.Value(ModOf(xeno));
                j.Name("modded"); j.Value(IsModded(xeno));
                j.Name("inheritable"); j.Value(xeno.inheritable);
                j.Name("geneCount"); j.Number(xeno.genes?.Count ?? 0);
                j.Name("archite"); j.Value(xeno.Archite);
                j.Name("combatPowerFactor"); j.Number(xeno.combatPowerFactor);
                j.Name("canGenerateAsCombatant"); j.Value(xeno.canGenerateAsCombatant);
                j.Name("factionlessGenerationWeight"); j.Number(xeno.factionlessGenerationWeight);
                j.Name("displayPriority"); j.Number(xeno.displayPriority);

                index.Routes.TryGetValue(xeno, out List<Route> routes);
                j.Name("routeCount"); j.Number(routes?.Count ?? 0);
                j.Name("routes");
                j.BeginArray();
                if (routes != null)
                {
                    foreach (Route route in routes
                        .OrderBy(r => r.Faction?.defName ?? "~", StringComparer.Ordinal)
                        .ThenBy(r => r.Kind?.defName ?? "", StringComparer.Ordinal))
                    {
                        j.BeginObject();
                        j.Name("faction"); j.Value(route.Faction?.defName);
                        if (route.Faction != null)
                        {
                            j.Name("factionMod"); j.Value(ModOf(route.Faction));
                        }
                        if (route.Kind != null)
                        {
                            j.Name("pawnKind"); j.Value(route.Kind.defName);
                        }
                        j.Name("chance"); j.Number(route.Chance);
                        j.EndObject();
                    }
                }
                j.EndArray();
                j.EndObject();
            }
            j.EndArray();
        }

        private static void WriteDerived(Json j, Index index)
        {
            j.Name("derived");
            j.BeginObject();

            // Xenotypes no faction and no pawnkind can roll. Some are deliberate (player-crafted,
            // quest-only, joke entries), so each carries the signals needed to tell those apart.
            j.Name("orphans");
            j.BeginArray();
            foreach (XenotypeDef xeno in index.Orphans.OrderBy(x => ModOf(x), StringComparer.Ordinal)
                .ThenBy(x => x.defName, StringComparer.Ordinal))
            {
                j.BeginObject();
                j.Name("defName"); j.Value(xeno.defName);
                j.Name("label"); j.Value(xeno.label);
                j.Name("mod"); j.Value(ModOf(xeno));
                j.Name("modded"); j.Value(IsModded(xeno));
                j.Name("inheritable"); j.Value(xeno.inheritable);
                j.Name("geneCount"); j.Number(xeno.genes?.Count ?? 0);
                j.Name("archite"); j.Value(xeno.Archite);
                j.Name("combatPowerFactor"); j.Number(xeno.combatPowerFactor);
                j.Name("canGenerateAsCombatant"); j.Value(xeno.canGenerateAsCombatant);
                j.Name("factionlessGenerationWeight"); j.Number(xeno.factionlessGenerationWeight);
                if (index.DoubleParents.TryGetValue(xeno, out List<XenotypeDef> parents))
                {
                    j.Name("viaDoubleXenotype");
                    j.BeginArray();
                    foreach (XenotypeDef parent in parents.OrderBy(p => p.defName, StringComparer.Ordinal))
                        j.Value(parent.defName);
                    j.EndArray();
                }
                if (!xeno.description.NullOrEmpty())
                {
                    j.Name("description"); j.Value(xeno.description);
                }
                j.EndObject();
            }
            j.EndArray();

            // Per-faction modded share, worst first: the working list for the budget caps.
            j.Name("budgets");
            j.BeginArray();
            var budgets = new List<KeyValuePair<FactionDef, float>>();
            foreach (FactionDef faction in index.Factions)
            {
                float modded = Chances(faction.xenotypeSet).Where(c => IsModded(c.xenotype)).Sum(c => c.chance);
                if (modded > 0f)
                    budgets.Add(new KeyValuePair<FactionDef, float>(faction, modded));
            }
            foreach (var pair in budgets.OrderByDescending(p => p.Value))
            {
                j.BeginObject();
                j.Name("faction"); j.Value(pair.Key.defName);
                j.Name("label"); j.Value(pair.Key.label);
                j.Name("mod"); j.Value(ModOf(pair.Key));
                j.Name("moddedTotal"); j.Number(pair.Value);
                j.Name("baselinerChance"); j.Number(pair.Key.xenotypeSet?.BaselinerChance ?? 1f);
                j.EndObject();
            }
            j.EndArray();

            // Factions that can field pawns but have no xenotype roster at all - the faction-first
            // half of the audit, where a thin roster is the finding.
            j.Name("emptyRosters");
            j.BeginArray();
            foreach (FactionDef faction in index.Factions)
            {
                if (faction.isPlayer || faction.hidden || !faction.humanlikeFaction)
                    continue;
                if (Chances(faction.xenotypeSet).Any())
                    continue;
                index.KindsByFaction.TryGetValue(faction, out List<PawnKindDef> kinds);
                if (kinds == null || kinds.Count == 0)
                    continue;
                j.BeginObject();
                j.Name("faction"); j.Value(faction.defName);
                j.Name("label"); j.Value(faction.label);
                j.Name("mod"); j.Value(ModOf(faction));
                j.Name("pawnKindCount"); j.Number(kinds.Count);
                j.EndObject();
            }
            j.EndArray();

            j.EndObject();
        }
    }
}
