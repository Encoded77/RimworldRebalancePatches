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
            Check.True(ours != null, "encoded.rebalancepatches not among running mods");
            int gated = 0;
            foreach (PatchOperation op in ours.Patches)
            {
                PatchOperationIfEnabled wrapper = op as PatchOperationIfEnabled;
                Check.True(wrapper != null,
                    $"top-level patch operation {op.GetType().Name} is not wrapped in PatchOperationIfEnabled (from {op.sourceFile})");
                gated++;
                Check.True(!string.IsNullOrEmpty(wrapper.settingKey),
                    $"PatchOperationIfEnabled without settingKey (from {op.sourceFile})");
                Check.True(SettingsRegistry.GroupOf(wrapper.settingKey) != null,
                    $"settingKey '{wrapper.settingKey}' (from {op.sourceFile}) is not registered in SettingsRegistry");
            }
            Check.True(gated > 0, "no patch operations loaded for encoded.rebalancepatches");
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
    }
}
