using System.Collections.Generic;
using System.Reflection;
using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SettingsMigrationTests
    {
        private static RebalancePatchesSettings Fake(int configVersion, bool cameFromDisk)
        {
            var s = new RebalancePatchesSettings { configVersion = configVersion };
            // CameFromDisk is set by ExposeData during a real load; simulate it here.
            PropertyInfo p = typeof(RebalancePatchesSettings).GetProperty("CameFromDisk");
            p.GetSetMethod(true).Invoke(s, new object[] { cameFromDisk });
            return s;
        }

        private static List<string> Pending(RebalancePatchesSettings s) =>
            (List<string>)typeof(SettingsMigrations)
                .GetMethod("PendingKeys", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { s });

        [Test]
        public static void FreshInstallTakesCurrentDefaults()
        {
            RebalancePatchesSettings s = Fake(0, cameFromDisk: false);
            SettingsMigrations.Apply(s);
            Check.Eq(s.values.Count, 0, "fresh install must not pin any setting");
            Check.Eq(s.configVersion, SettingsMigrations.CurrentVersion, "fresh install config version");
        }

        [Test]
        public static void UpgradedInstallKeepsOverhaulsOn()
        {
            RebalancePatchesSettings s = Fake(0, cameFromDisk: true);
            SettingsMigrations.Apply(s);
            foreach (string key in new[] { "genetics", "geneticsresearch", "scifinames" })
            {
                Check.True(s.TryGet(key, out bool v), $"upgrade must pin '{key}'");
                Check.True(v, $"'{key}' must be pinned on, as it was before the update");
            }
            Check.Eq(s.configVersion, SettingsMigrations.CurrentVersion, "upgraded config version");
        }

        [Test]
        public static void RetiredGroupKeysAreCleanedUp()
        {
            RebalancePatchesSettings s = Fake(0, cameFromDisk: true);
            SettingsMigrations.Apply(s);
            Check.True(!s.TryGet("genepool", out _), "'genepool' must not survive the merge");
            Check.True(!s.TryGet("xenogenes", out _), "'xenogenes' must not survive the merge");
        }

        [Test]
        public static void ResearchChoiceIsNotMistakenForTheMergedGroup()
        {
            // The merged Genetics Overhaul took over the "genetics" key that the research overhaul
            // used to own. A stored value must follow its meaning, not its key.
            RebalancePatchesSettings s = Fake(1, cameFromDisk: true);
            s.Set("genetics", false);   // research overhaul, turned off by hand pre-merge
            s.Set("genepool", true);
            s.Set("xenogenes", true);
            SettingsMigrations.Apply(s);
            Check.True(s.TryGet("geneticsresearch", out bool research) && !research,
                "the old 'genetics' value belongs to the research overhaul and must stay off");
            Check.True(s.TryGet("genetics", out bool merged) && merged,
                "the merged group must take its value from genepool/xenogenes, not from the old key");
        }

        [Test]
        public static void MergeKeepsTheGroupOnIfEitherSourceWasOn()
        {
            RebalancePatchesSettings s = Fake(1, cameFromDisk: true);
            s.Set("genepool", true);
            s.Set("xenogenes", false);
            SettingsMigrations.Apply(s);
            Check.True(s.TryGet("genetics", out bool v) && v,
                "merged group stays on when either source was on");

            RebalancePatchesSettings off = Fake(1, cameFromDisk: true);
            off.Set("genepool", false);
            off.Set("xenogenes", false);
            SettingsMigrations.Apply(off);
            Check.True(off.TryGet("genetics", out bool w) && !w,
                "merged group is off only when both sources were off");
        }

        [Test]
        public static void ExplicitChoiceIsNeverOverwritten()
        {
            RebalancePatchesSettings s = Fake(0, cameFromDisk: true);
            s.Set("scifinames", false);
            SettingsMigrations.Apply(s);
            Check.True(s.TryGet("scifinames", out bool v) && !v,
                "a setting the player turned off by hand must stay off");
            Check.True(s.TryGet("genetics", out bool g) && g, "untouched settings are still pinned");
        }

        [Test]
        public static void MigrationIsIdempotent()
        {
            RebalancePatchesSettings s = Fake(0, cameFromDisk: true);
            SettingsMigrations.Apply(s);
            s.Set("genetics", false);
            SettingsMigrations.Apply(s);
            Check.True(s.TryGet("genetics", out bool v) && !v,
                "re-running the migration must not undo a later choice");
            Check.Eq(Pending(s).Count, 0, "nothing left pending once migrated");
        }

        [Test]
        public static void AlreadyCurrentConfigIsUntouched()
        {
            RebalancePatchesSettings s = Fake(SettingsMigrations.CurrentVersion, cameFromDisk: true);
            Check.True(!SettingsMigrations.Apply(s), "a migrated config must not report a change");
            Check.Eq(s.values.Count, 0, "a config already at the current version must not be altered");
        }

        [Test]
        public static void OnlyReportsAChangeWhenThereIsOne()
        {
            RebalancePatchesSettings upgraded = Fake(0, cameFromDisk: true);
            Check.True(SettingsMigrations.Apply(upgraded), "an upgrade must report a change so it gets written");
            Check.True(!SettingsMigrations.Apply(upgraded), "a second pass must report no change");

            RebalancePatchesSettings fresh = Fake(0, cameFromDisk: false);
            Check.True(SettingsMigrations.Apply(fresh), "a fresh install must stamp its version once");
            Check.True(!SettingsMigrations.Apply(fresh), "and not rewrite on every launch after that");
        }

        [Test]
        public static void ChildKeysAreCarriedToTheirNewNames()
        {
            RebalancePatchesSettings s = Fake(1, cameFromDisk: true);
            s.Set("genetics.core", false);              // research tree child, turned off by hand
            s.Set("genepool.dedup", false);             // genepool child, turned off by hand
            s.Set("xenotypes.stonebornskin", false);    // xenotype-gene child, turned off by hand
            s.Set("xenotypes.factions", false);         // retired in v4, fans out per faction owner
            SettingsMigrations.Apply(s);

            Check.True(s.TryGet("geneticsresearch.core", out bool core) && !core,
                "a research child must follow its group to geneticsresearch.*");
            Check.True(s.TryGet("genetics.dedup", out bool dedup) && !dedup,
                "a genepool child must be carried to genetics.*");
            Check.True(s.TryGet("genetics.stonebornskin", out bool skin) && !skin,
                "a xenotype-gene child must be carried to genetics.*");
            Check.True(s.TryGet("xenotypes.vanilla", out bool vanilla) && !vanilla,
                "the retired xenotypes.factions must carry its choice to xenotypes.vanilla");
            Check.True(s.TryGet("xenotypes.odyssey", out bool odyssey) && !odyssey,
                "the retired xenotypes.factions must reach every faction key that replaced it");
            Check.True(!s.TryGet("xenotypes.factions", out _), "the retired faction key must not survive");
            Check.True(!s.TryGet("genetics.core", out _), "old research child key must not survive");
            Check.True(!s.TryGet("genepool.dedup", out _), "old genepool child key must not survive");
        }

        [Test]
        public static void ConsumablesLaneInheritsEitherPredecessor()
        {
            // Two v5 sources share one target, so the lane ends up on if either was on.
            RebalancePatchesSettings offBoth = Fake(4, cameFromDisk: true);
            offBoth.Set("geneticsresearch.agtools", false);
            offBoth.Set("bigsmall.geneintegrator", false);
            SettingsMigrations.Apply(offBoth);
            Check.True(offBoth.TryGet("geneticsresearch.consumables", out bool none) && !none,
                "both predecessors off must leave the lane off");
            Check.True(!offBoth.TryGet("geneticsresearch.agtools", out _),
                "the retired agtools key must not survive");
            Check.True(!offBoth.TryGet("bigsmall.geneintegrator", out _),
                "the retired geneintegrator key must not survive");

            RebalancePatchesSettings oneOn = Fake(4, cameFromDisk: true);
            oneOn.Set("geneticsresearch.agtools", false);
            oneOn.Set("bigsmall.geneintegrator", true);
            SettingsMigrations.Apply(oneOn);
            Check.True(oneOn.TryGet("geneticsresearch.consumables", out bool either) && either,
                "either predecessor on must leave the lane on");
        }

        [Test]
        public static void MigratedKeysAreAllRegistered()
        {
            RebalancePatchesSettings s = Fake(0, cameFromDisk: true);
            SettingsMigrations.Apply(s);
            foreach (string key in s.values.Keys)
                Check.True(SettingsRegistry.GroupOf(key) != null,
                    $"migration leaves '{key}' in the config, which is not a registered setting");
        }
    }
}
