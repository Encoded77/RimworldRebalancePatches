using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Every HediffDef, plus two things that only exist while the game is running: EBSG Framework's
    /// modular slot declarations, and VRE - Android's per-hediff "androids can catch this" flag.
    /// Both are read by shape rather than by type reference, so this dumps correctly whether or not
    /// those mods are loaded.
    ///
    /// Slot capacity is the lever for implant stacking, so the slot table is emitted separately as
    /// well as inline: reading capacities per slot id is the question, and grouping it here saves
    /// the analyzer rebuilding it.
    /// </summary>
    internal static class HediffDump
    {
        private const string ModularCompName = "HediffCompProperties_Modular";
        private const string AndroidExtensionType = "VREAndroids.AndroidSettingsExtension";

        private static readonly Type[] Referenced =
        {
            typeof(ThingDef), typeof(StatDef), typeof(BodyPartDef), typeof(HediffDef),
            typeof(ThoughtDef), typeof(NeedDef), typeof(AbilityDef), typeof(MentalStateDef),
        };

        internal sealed class Slot
        {
            public string Id, Name;
            /// <summary>Null when the framework's field could not be read — distinct from a real 0.</summary>
            public int? Capacity;
            /// <summary>Field names actually on the slot object, so a rename is diagnosable.</summary>
            public string Fields;
        }

        [DebugAction("RebalancePatches", "Dump hediffs", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump hediffs", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(Referenced, bareDefTypes: new[] { typeof(HediffDef) });
            int total = 0, slots = 0;

            DumpRunner.Run("HediffDump.json", walker, w =>
            {
                Json j = w.Json;
                var all = DefDatabase<HediffDef>.AllDefsListForReading
                    .OrderBy(h => h.defName, StringComparer.Ordinal).ToList();

                j.Name("hediffs");
                j.BeginArray();
                foreach (HediffDef hediff in all)
                {
                    List<Slot> hediffSlots = ModularSlots(hediff);
                    w.WriteDefEntry(hediff, _ =>
                    {
                        // Cheap flags the analyzer would otherwise recompute for every hediff.
                        j.Name("isAddedPart"); j.Value(hediff.addedPartProps != null);
                        j.Name("androidCanCatch"); j.Value(AndroidCanCatch(hediff));

                        if (hediffSlots.Count > 0)
                        {
                            j.Name("modularSlots");
                            j.BeginArray();
                            foreach (Slot slot in hediffSlots)
                            {
                                j.BeginObject();
                                j.Name("slotID"); j.Value(slot.Id);
                                j.Name("slotName"); j.Value(slot.Name);
                                j.Name("capacity");
                                if (slot.Capacity.HasValue) j.Number(slot.Capacity.Value); else j.Null();
                                j.Name("uncapped"); j.Value(slot.Capacity.HasValue && slot.Capacity.Value < 0);
                                if (slot.Fields != null) { j.Name("slotFields"); j.Value(slot.Fields); }
                                j.EndObject();
                                slots++;
                            }
                            j.EndArray();
                        }
                    });
                    total++;
                }
                j.EndArray();

                WriteSlotTable(j, all);
            }, () => $"{total} hediffs, {slots} modular slots");
        }

        /// <summary>Every declared slot by id, its capacities and its hosts.</summary>
        private static void WriteSlotTable(Json j, List<HediffDef> all)
        {
            var byId = new Dictionary<string, List<(HediffDef Host, Slot S)>>();
            foreach (HediffDef hediff in all)
                foreach (Slot slot in ModularSlots(hediff))
                {
                    if (slot.Id == null) continue;
                    if (!byId.TryGetValue(slot.Id, out var list)) byId[slot.Id] = list = new();
                    list.Add((hediff, slot));
                }

            j.Name("modularSlotTable");
            j.BeginObject();
            foreach (var pair in byId.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                j.Name(pair.Key);
                j.BeginObject();
                j.Name("slotName"); j.Value(pair.Value[0].S.Name);
                j.Name("uncapped"); j.Value(pair.Value.Any(v => v.S.Capacity.HasValue && v.S.Capacity.Value < 0));
                j.Name("unreadableCapacity"); j.Value(pair.Value.Any(v => !v.S.Capacity.HasValue));
                j.Name("capacities");
                j.BeginArray();
                foreach (int? cap in pair.Value.Select(v => v.S.Capacity).Distinct().OrderBy(c => c ?? int.MinValue))
                {
                    if (cap.HasValue) j.Number(cap.Value); else j.Null();
                }
                j.EndArray();
                j.Name("hosts");
                j.BeginArray();
                foreach (var host in pair.Value.OrderBy(v => v.Host.defName, StringComparer.Ordinal))
                {
                    j.BeginObject();
                    j.Name("hediff"); j.Value(host.Host.defName);
                    j.Name("mod"); j.Value(DumpRunner.ModOf(host.Host));
                    j.Name("capacity");
                    if (host.S.Capacity.HasValue) j.Number(host.S.Capacity.Value); else j.Null();
                    j.EndObject();
                }
                j.EndArray();
                j.EndObject();
            }
            j.EndObject();
        }

        internal static List<Slot> ModularSlots(HediffDef hediff)
        {
            var result = new List<Slot>();
            if (hediff?.comps == null) return result;
            foreach (HediffCompProperties comp in hediff.comps)
            {
                if (comp == null) continue;
                if (comp.GetType().Name.IndexOf(ModularCompName, StringComparison.Ordinal) < 0) continue;
                FieldInfo slotsField = comp.GetType().GetField("slots", BindingFlags.Public | BindingFlags.Instance);
                if (!(slotsField?.GetValue(comp) is IEnumerable list)) continue;
                foreach (object entry in list)
                {
                    if (entry == null) continue;
                    Type t = entry.GetType();
                    // Read numerically rather than pattern-matching int: the framework is free to
                    // declare capacity as any numeric type, and a failed match is indistinguishable
                    // from a real zero.
                    object raw = t.GetField("capacity", BindingFlags.Public | BindingFlags.Instance)?.GetValue(entry);
                    int? capacity = null;
                    if (raw is IConvertible conv)
                    {
                        try { capacity = Convert.ToInt32(conv); }
                        catch { }
                    }
                    result.Add(new Slot
                    {
                        Id = t.GetField("slotID", BindingFlags.Public | BindingFlags.Instance)?.GetValue(entry) as string,
                        Name = t.GetField("slotName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(entry) as string,
                        Capacity = capacity,
                        Fields = capacity == null
                            ? string.Join(",", t.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name))
                            : null,
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// VRE - Android tags hediffs androids cannot receive via a mod extension, inherited through
        /// def parents (its patch applies one to DiseaseBase). Absent extension means allowed.
        /// </summary>
        private static bool AndroidCanCatch(HediffDef hediff)
        {
            if (hediff.modExtensions == null) return true;
            foreach (DefModExtension ext in hediff.modExtensions)
            {
                if (ext == null || ext.GetType().FullName != AndroidExtensionType) continue;
                FieldInfo field = ext.GetType().GetField("androidCanCatchIt", BindingFlags.Public | BindingFlags.Instance);
                if (field?.GetValue(ext) is bool can) return can;
            }
            return true;
        }
    }
}
