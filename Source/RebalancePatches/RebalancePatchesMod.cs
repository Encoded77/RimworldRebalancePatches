using UnityEngine;
using Verse;

namespace RebalancePatches
{
    public class RebalancePatchesMod : Mod
    {
        public static RebalancePatchesSettings Settings;

        public RebalancePatchesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RebalancePatchesSettings>();
            SettingsRegistry.Bind(Settings);
            // Runs before patches apply (CreateModClasses precedes ApplyPatches), so a pinned
            // setting takes effect on the very load that upgrades the config.
            if (SettingsMigrations.Apply(Settings))
                WriteSettings();
        }

        public override string SettingsCategory() => "Rebalance Patches";

        public override void DoSettingsWindowContents(Rect inRect) => UI.SettingsWindowContents.Draw(inRect);
    }
}
