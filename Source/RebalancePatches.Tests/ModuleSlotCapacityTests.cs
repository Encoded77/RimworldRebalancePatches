using System;
using System.Collections;
using System.Collections.Generic;
using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ModuleSlotCapacityTests
    {
        private const string Key = "cybernetics.modules";

        private static readonly string[] Sites =
        {
            "Arm", "Eye", "Jaw", "Kidney", "Leg", "Lung", "Nose", "Spine", "Stomach",
        };

        [Test]
        public static void ModulesStayInThePawnGenerationPool()
        {
            if (!Check.Ready(Key, Ids.EBSG))
                return;

            int tagged = 0, untagged = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.comps == null)
                    continue;
                bool isModule = false;
                foreach (object props in def.comps)
                    if (props != null && props.GetType().Name == "CompProperties_UseEffectHediffModule")
                        isModule = true;
                if (!isModule)
                    continue;
                if (def.techHediffsTags != null && def.techHediffsTags.Count > 0)
                    tagged++;
                else
                    untagged++;
            }

            Check.Note($"modules in the NPC pool: {tagged} tagged, {untagged} untagged");
            Check.Soft(tagged > 0,
                "no module carries techHediffsTags - modules are meant to stay available to NPC " +
                "generation, with the host fitted in C# rather than the tags removed");
            Check.SoftResult();
        }

        [Test]
        public static void BionicHostsHoldTwoModules()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            foreach (string site in Sites)
                AssertCapacity("LTS_ModularBionic" + site, "LTS_" + site + "ModuleSlot", 2);

            Check.SoftResult();
        }

        [Test]
        public static void ArchotechHostsHoldThreeModules()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            foreach (string site in Sites)
                AssertCapacity("LTS_ModularArchotech" + site, "LTS_" + site + "ModuleSlot", 3);

            Check.SoftResult();
        }

        [Test]
        public static void NoModuleSlotIsUnlimited()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            var unlimited = new List<string>();
            var foreign = new List<string>();
            foreach (HediffDef hediff in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                if (hediff.comps == null)
                    continue;
                foreach (object props in hediff.comps)
                {
                    if (props == null || props.GetType().Name != "HediffCompProperties_Modular")
                        continue;
                    foreach (var slot in SlotsOf(props))
                    {
                        if (slot.Value >= 0)
                            continue;
                        if (slot.Key.StartsWith("LTS_") || slot.Key.StartsWith("RBP_"))
                            unlimited.Add($"{hediff.defName}/{slot.Key}");
                        else
                            foreign.Add($"{hediff.defName}/{slot.Key}");
                    }
                }
            }

            if (foreign.Count > 0)
                Check.Note($"{foreign.Count} uncapped module slot(s) outside this project's domain: " +
                    string.Join(", ", foreign.ToArray()));

            Check.Soft(unlimited.Count == 0,
                $"{unlimited.Count} module slot(s) still uncapped: {string.Join(", ", unlimited.ToArray())}");
            Check.SoftResult();
        }

        private static void AssertCapacity(string hediffName, string slotID, int expected)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (!Check.Soft(hediff != null, $"HediffDef '{hediffName}' not found - renamed or removed upstream?"))
                return;
            if (!Check.Soft(hediff.comps != null, $"{hediffName} has no comps at all"))
                return;

            foreach (object props in hediff.comps)
            {
                if (props == null || props.GetType().Name != "HediffCompProperties_Modular")
                    continue;
                foreach (var slot in SlotsOf(props))
                {
                    if (slot.Key != slotID)
                        continue;
                    Check.Soft(slot.Value == expected,
                        $"{hediffName}/{slotID} capacity is {slot.Value}, expected {expected}");
                    return;
                }
            }

            Check.Soft(false, $"{hediffName} declares no modular slot {slotID}");
        }

        private static IEnumerable<KeyValuePair<string, int>> SlotsOf(object props)
        {
            var found = new List<KeyValuePair<string, int>>();
            object raw;
            try
            {
                raw = Check.Field(props, "slots");
            }
            catch (Exception e)
            {
                Check.Soft(false, $"modular comp has no slots field: {e.Message}");
                return found;
            }

            if (!(raw is IEnumerable slots))
                return found;

            foreach (object slot in slots)
            {
                if (slot == null)
                    continue;
                try
                {
                    string id = Check.Field(slot, "slotID") as string;
                    int capacity = Convert.ToInt32(Check.Field(slot, "capacity"));
                    if (id != null)
                        found.Add(new KeyValuePair<string, int>(id, capacity));
                }
                catch (Exception e)
                {
                    Check.Soft(false, $"a module slot could not be read: {e.Message}");
                }
            }
            return found;
        }
    }
}
