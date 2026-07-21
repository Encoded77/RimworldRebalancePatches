using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    public class GeneRemovalListDef : Def
    {
        public string settingKey;
        public List<string> requiredMods = new List<string>();
        public List<string> genes = new List<string>();
        public List<string> genePrefixes = new List<string>();
    }

    public class XenotypeRewireDef : Def
    {
        public string settingKey;
        public List<string> requiredMods = new List<string>();
        public List<XenotypeRewireEntry> xenotypes = new List<XenotypeRewireEntry>();
    }

    public class XenotypeRewireEntry
    {
        public string xenotype;
        public List<string> genes = new List<string>();
    }

    /// <summary>
    /// Why a def one of our patches targets is missing from the DefDatabase. A def we removed
    /// ourselves and a def upstream renamed look identical once it is gone, so the tests ask here
    /// before deciding whether an absence is worth reporting.
    /// </summary>
    public static class GeneRemovalInfo
    {
        /// <summary>
        /// The settingKey of the removal list currently stripping <paramref name="defName"/>, or
        /// null when none is. A list that exists but is toggled off, or whose requiredMods are
        /// absent, does not count: it removed nothing, so it explains nothing.
        /// </summary>
        public static string ActiveRemovalSetting(string defName)
        {
            bool coreActive = GeneCleanup.CoreModsActive();
            foreach (GeneRemovalListDef list in DefDatabase<GeneRemovalListDef>.AllDefsListForReading)
                if (GeneCleanup.ListActive(list, coreActive) && GeneCleanup.Matches(list, defName))
                    return list.settingKey;
            return null;
        }
    }

    internal static class GeneCleanup
    {
        // The gene ecosystem rebalance assumes all three core gene mods; removals
        // never apply with only a subset loaded.
        private static readonly string[] CoreMods =
        {
            "sarg.alphagenes",
            "wvc.sergkart.races.biotech",
            "redmattis.bigsmall.core",
        };

        public static void TryApply()
        {
            try
            {
                Apply();
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] Genepool cleanup failed: " + e);
            }
        }

        private static void Apply()
        {
            HashSet<string> removedDefs = CherryPickerRemovedDefs();
            if (removedDefs == null)
                return;
            bool coreActive = CoreModsActive();
            ApplyRemovals(removedDefs, coreActive);
            ApplyRewires(coreActive);
        }

        internal static bool CoreModsActive()
        {
            foreach (string id in CoreMods)
                if (!ModsConfig.IsActive(id))
                    return false;
            return true;
        }

        internal static bool ListActive(GeneRemovalListDef list, bool coreActive) =>
            coreActive && RequiredModsActive(list.requiredMods) && SettingsRegistry.GetEffective(list.settingKey);

        private static void ApplyRemovals(HashSet<string> removedDefs, bool coreActive)
        {
            List<GeneRemovalListDef> lists = DefDatabase<GeneRemovalListDef>.AllDefsListForReading;
            if (lists.Count == 0)
                return;

            var activeLists = new List<GeneRemovalListDef>();
            var desired = new HashSet<string>();
            foreach (GeneRemovalListDef list in lists)
            {
                if (ListActive(list, coreActive))
                {
                    activeLists.Add(list);
                    desired.UnionWith(GeneratedKeys(list));
                }
            }
            // Existing Cherry Picker keys in our domain: genes an earlier pass already
            // stripped from the DefDatabase keep their stored key when still wanted;
            // everything else (toggled-off list, stale key format) is swept out so
            // toggling stays symmetric and no dead keys accumulate.
            var undesired = new HashSet<string>();
            foreach (string key in removedDefs)
            {
                string[] parts = key.Split('/');
                if (parts.Length < 2 || desired.Contains(key))
                    continue;
                string defName = parts[1];
                bool inActiveList = false;
                foreach (GeneRemovalListDef list in activeLists)
                    if (Matches(list, defName))
                    {
                        inActiveList = true;
                        break;
                    }
                if (inActiveList && DefDatabase<GeneDef>.GetNamedSilentFail(defName) == null)
                {
                    desired.Add(key);
                    continue;
                }
                foreach (GeneRemovalListDef list in lists)
                    if (Matches(list, defName))
                    {
                        undesired.Add(key);
                        break;
                    }
            }

            int queued = 0;
            foreach (string key in desired)
                if (removedDefs.Add(key))
                    queued++;
            int restored = 0;
            foreach (string key in undesired)
                if (removedDefs.Remove(key))
                    restored++;
            if (desired.Count > 0 || restored > 0)
                Log.Message($"[RebalancePatches] Genepool cleanup: {desired.Count} genes removed via Cherry Picker ({queued} newly queued, {restored} restored).");
        }

        private static bool RequiredModsActive(List<string> requiredMods)
        {
            foreach (string id in requiredMods)
                if (!ModsConfig.IsActive(id))
                    return false;
            return true;
        }

        // Xenotypes that lost a gene to the removal lists get the canonical
        // replacement instead. Same gate as the removals: replacement genes from absent mods and
        // genes the xenotype already carries are skipped silently.
        private static void ApplyRewires(bool coreActive)
        {
            if (!coreActive)
                return;
            int added = 0;
            foreach (XenotypeRewireDef rewire in DefDatabase<XenotypeRewireDef>.AllDefsListForReading)
            {
                if (!RequiredModsActive(rewire.requiredMods) || !SettingsRegistry.GetEffective(rewire.settingKey))
                    continue;
                foreach (XenotypeRewireEntry entry in rewire.xenotypes)
                {
                    XenotypeDef xeno = DefDatabase<XenotypeDef>.GetNamedSilentFail(entry.xenotype);
                    if (xeno?.genes == null)
                        continue;
                    foreach (string name in entry.genes)
                    {
                        GeneDef gene = DefDatabase<GeneDef>.GetNamedSilentFail(name);
                        if (gene != null && !xeno.genes.Contains(gene))
                        {
                            xeno.genes.Add(gene);
                            added++;
                        }
                    }
                }
            }
            if (added > 0)
                Log.Message($"[RebalancePatches] Genepool rewire: {added} canonical replacement genes added to xenotypes.");
        }

        private static HashSet<string> GeneratedKeys(GeneRemovalListDef list)
        {
            var keys = new HashSet<string>();
            foreach (string name in list.genes)
            {
                AddGene(keys, name);
                // VEF astrogene copies (VRE - Starjack) of a removed gene go with it.
                AddGene(keys, name + "_Astrogene");
            }
            if (list.genePrefixes.Count > 0)
                foreach (GeneDef def in DefDatabase<GeneDef>.AllDefsListForReading)
                    foreach (string prefix in list.genePrefixes)
                        if (def.defName.StartsWith(prefix))
                            keys.Add(KeyOf(def));
            return keys;
        }

        private static void AddGene(HashSet<string> keys, string name)
        {
            GeneDef def = DefDatabase<GeneDef>.GetNamedSilentFail(name);
            if (def != null)
                keys.Add(KeyOf(def));
        }

        internal static bool Matches(GeneRemovalListDef list, string defName)
        {
            if (defName.EndsWith("_Astrogene"))
                defName = defName.Substring(0, defName.Length - "_Astrogene".Length);
            if (list.genes.Contains(defName))
                return true;
            foreach (string prefix in list.genePrefixes)
                if (defName.StartsWith(prefix))
                    return true;
            return false;
        }

        // Mirrors Cherry Picker's DefUtility.ToKey: def types outside the Verse and
        // RimWorld namespaces need the namespace as a third key segment or its
        // ToType lookup fails and the key is silently skipped.
        private static string KeyOf(GeneDef def)
        {
            Type type = def.GetType();
            string key = type.Name + "/" + def.defName;
            string ns = type.Namespace;
            if (ns == null || ns == "Verse" || ns == "RimWorld")
                return key;
            return key + "/" + ns;
        }

        private static HashSet<string> CherryPickerRemovedDefs()
        {
            Type settingsType = GenTypes.GetTypeInAnyAssembly("CherryPicker.ModSettings_CherryPicker");
            if (settingsType == null)
                return null;
            return AccessTools.Field(settingsType, "allRemovedDefs")?.GetValue(null) as HashSet<string>;
        }
    }
}
