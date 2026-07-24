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
            if (SettingsMigrations.Apply(Settings))
                WriteSettings();
            HarmonyBootstrap.EnsureEarlyApplied();
        }

        public override string SettingsCategory() => "Rebalance Patches";

        public override void DoSettingsWindowContents(Rect inRect) => UI.SettingsWindowContents.Draw(inRect);
    }
}
