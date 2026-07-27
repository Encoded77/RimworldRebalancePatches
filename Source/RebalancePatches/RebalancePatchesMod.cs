using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RebalancePatches
{
    public class RebalancePatchesMod : Mod
    {
        public static RebalancePatchesSettings Settings;

        // Only a save driven by the settings menu can change anything; the migration write in the
        // constructor must not be mistaken for a user change (and runs before XML defaults exist).
        private static bool menuOpened;

        public RebalancePatchesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RebalancePatchesSettings>();
            SettingsRegistry.Bind(Settings);
            if (SettingsMigrations.Apply(Settings))
                WriteSettings();
            Log.Message($"[Rebalance Patches] Settings loaded: {SettingsDiagnostics.LoadedSummary(Settings)}");
            HarmonyBootstrap.EnsureEarlyApplied();
        }

        public override string SettingsCategory() => "Rebalance Patches";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            menuOpened = true;
            UI.SettingsWindowContents.Draw(inRect);
        }

        public override void WriteSettings()
        {
            if (!menuOpened)
            {
                base.WriteSettings();
                return;
            }

            string previous = Settings.appliedSignature;
            string current = SettingsDiagnostics.EffectiveSignature();
            if (previous != current)
                Settings.appliedSignature = current;
            base.WriteSettings();

            if (previous == current)
                return;

            if (string.IsNullOrEmpty(previous))
                Log.Message("[Rebalance Patches] Recorded the settings baseline.");
            else
            {
                List<string> changed = SettingsDiagnostics.ChangedKeys(previous, current);
                Log.Message($"[Rebalance Patches] Settings changed ({changed.Count}): {string.Join(", ", changed)}");
            }

            // Any effective change makes a cached patch flattening stale, whether we could name the
            // previous state or not (an existing config's first save has no baseline to diff).
            if (GagarinCache.Present())
            {
                int removed = GagarinCache.Reset();
                Log.Message($"[Rebalance Patches] Reset the Missile Girl XML patch cache ({removed} file(s)) " +
                            "so the changed settings apply on the next restart.");
            }
        }
    }
}
