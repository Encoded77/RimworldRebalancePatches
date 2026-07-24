using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SettingsTests
    {
        [Test]
        public static void AllPatchXmlSettingKeysRegistered()
        {
            ModContentPack ours = null;
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
                if (mod.PackageId == "encoded.rebalancepatches")
                    ours = mod;
            if (!Check.Soft(ours != null, "encoded.rebalancepatches not among running mods"))
            {
                Check.SoftResult();
                return;
            }

            int gated = 0;
            var all = new List<PatchOperationIfEnabled>();
            foreach (PatchOperation op in ours.Patches)
            {
                Check.Soft(op is PatchOperationIfEnabled,
                    $"top-level patch operation {op.GetType().Name} is not wrapped in PatchOperationIfEnabled (from {op.sourceFile})");
                gated++;
                Collect(op, all, new HashSet<PatchOperation>(), 0);
            }

            foreach (PatchOperationIfEnabled wrapper in all)
            {
                if (!Check.Soft(!string.IsNullOrEmpty(wrapper.settingKey),
                        $"PatchOperationIfEnabled without settingKey (from {wrapper.sourceFile})"))
                    continue;
                Check.Soft(SettingsRegistry.GroupOf(wrapper.settingKey) != null,
                    $"settingKey '{wrapper.settingKey}' (from {wrapper.sourceFile}) is not registered in SettingsRegistry");
            }

            Check.Note($"{gated} top-level operation(s), {all.Count} settings gate(s) including nested");
            Check.Soft(gated > 0, "no patch operations loaded for encoded.rebalancepatches");
            Check.Soft(all.Count >= gated, "nested collection found fewer gates than there are top-level operations");
            Check.SoftResult();
        }

        private static void Collect(PatchOperation op, List<PatchOperationIfEnabled> found,
            HashSet<PatchOperation> seen, int depth)
        {
            if (op == null || depth > 12 || !seen.Add(op))
                return;
            if (op is PatchOperationIfEnabled gate)
                found.Add(gate);

            foreach (FieldInfo field in op.GetType()
                         .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object value;
                try { value = field.GetValue(op); }
                catch { continue; }

                if (value is PatchOperation child)
                {
                    Collect(child, found, seen, depth + 1);
                }
                else if (value is IEnumerable list && !(value is string))
                {
                    foreach (object item in list)
                        if (item is PatchOperation nested)
                            Collect(nested, found, seen, depth + 1);
                }
            }
        }

        [Test]
        public static void AllGenepoolDefSettingKeysRegistered()
        {
            foreach (GeneRemovalListDef def in DefDatabase<GeneRemovalListDef>.AllDefsListForReading)
                Check.True(!string.IsNullOrEmpty(def.settingKey) && SettingsRegistry.GroupOf(def.settingKey) != null,
                    $"GeneRemovalListDef {def.defName}: settingKey '{def.settingKey}' is not registered in SettingsRegistry");
            foreach (XenotypeRewireDef def in DefDatabase<XenotypeRewireDef>.AllDefsListForReading)
                Check.True(!string.IsNullOrEmpty(def.settingKey) && SettingsRegistry.GroupOf(def.settingKey) != null,
                    $"XenotypeRewireDef {def.defName}: settingKey '{def.settingKey}' is not registered in SettingsRegistry");
        }

        [Test]
        public static void RegistryDeclarationsConsistent()
        {
            var seenKeys = new HashSet<string>();
            foreach (RebalanceGroup group in SettingsRegistry.Groups)
            {
                Check.True(seenKeys.Add(group.key), $"duplicate group key '{group.key}'");
                CheckModIds(group.key, group.requiredMods);
                foreach (RebalanceToggle child in group.children)
                {
                    Check.True(seenKeys.Add(child.key), $"duplicate setting key '{child.key}'");
                    CheckModIds(child.key, child.requiredMods);
                    CheckModIds(child.key, child.anyOfMods);
                    if (child.dependsOn != null)
                        Check.True(SettingsRegistry.ToggleOf(child.dependsOn) != null,
                            $"'{child.key}' dependsOn '{child.dependsOn}' which is not a registered toggle");
                }
                foreach (RebalanceSlider slider in group.sliders)
                {
                    Check.True(seenKeys.Add(slider.key), $"duplicate setting key '{slider.key}'");
                    CheckModIds(slider.key, slider.requiredMods);
                }
            }
        }

        [Test]
        public static void EffectiveDefaultsMatchTheRegistry()
        {
            // key -> why the XML is allowed to disagree with the registry. Empty today.
            var deliberateOverrides = new Dictionary<string, string>();

            var compared = 0;
            foreach (RebalanceGroup group in SettingsRegistry.Groups)
            {
                foreach (RebalanceToggle child in group.children)
                {
                    compared++;
                    bool effective = SettingsRegistry.DefaultOf(child.key);
                    if (effective == child.defaultOn)
                        continue;
                    if (deliberateOverrides.TryGetValue(child.key, out string reason))
                    {
                        Check.Note($"{child.key}: XML default {effective} overrides registry " +
                            $"{child.defaultOn} on purpose - {reason}");
                        continue;
                    }
                    Check.Soft(false,
                        $"'{child.key}' is declared defaultOn: {child.defaultOn} in SettingsRegistry " +
                        $"but resolves to {effective} in game, because a patch file declared " +
                        "<defaultOn> for it. Either the registry is wrong or the XML is - a player " +
                        "reading the settings would be told one thing and given the other");
                }
            }

            Check.True(compared > 0, "no toggles were compared - SettingsRegistry.Groups was empty");
            Check.Note($"compared {compared} toggle default(s) against their effective value");
            Check.SoftResult();
        }

        [Test]
        public static void ChildKeysArePrefixedWithTheirGroup()
        {
            foreach (RebalanceGroup group in SettingsRegistry.Groups)
            {
                foreach (RebalanceToggle child in group.children)
                    CheckPrefix(group.key, child.key);
                foreach (RebalanceSlider slider in group.sliders)
                    CheckPrefix(group.key, slider.key);
            }
        }

        private static void CheckPrefix(string groupKey, string childKey)
        {
            Check.True(childKey.StartsWith(groupKey + "."),
                $"setting '{childKey}' belongs to group '{groupKey}' but is not prefixed '{groupKey}.' — "
                + "a prefix naming a different group makes keys ambiguous to maintain. Rename it and add "
                + "a KeyMove to SettingsMigrations so existing configs follow.");
        }

        private static void CheckModIds(string owner, string[] packageIds)
        {
            foreach (string id in packageIds)
                Check.True(!string.IsNullOrEmpty(id) && id == id.ToLowerInvariant() && !id.Contains(" "),
                    $"'{owner}' declares malformed packageId '{id}'");
        }
    }
}
