using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// A setting whose registered default changed in a released version. Existing configs never
    /// stored a value for it, so on upgrade they would silently pick up the new default and change
    /// behaviour mid-save. Recording the flip here lets the migration pin the old default instead.
    /// </summary>
    internal class DefaultFlip
    {
        public readonly int version;
        public readonly string key;
        public readonly bool previousDefault;

        public DefaultFlip(int version, string key, bool previousDefault)
        {
            this.version = version;
            this.key = key;
            this.previousDefault = previousDefault;
        }
    }

    /// <summary>
    /// A setting key that was renamed or folded into another one. Several moves may share a target,
    /// in which case the target ends up on if any source was on.
    /// </summary>
    internal class KeyMove
    {
        public readonly int version;
        public readonly string from;
        public readonly string to;

        public KeyMove(int version, string from, string to)
        {
            this.version = version;
            this.from = from;
            this.to = to;
        }
    }

    /// <summary>
    /// Keeps an existing player's behaviour stable across mod updates that change a setting's
    /// default, rename it, or merge it into another. Fresh installs take the current defaults;
    /// upgraded installs keep what they had.
    /// </summary>
    public static class SettingsMigrations
    {
        /// Bump whenever a new flip or move is added below.
        public const int CurrentVersion = 4;

        private static readonly DefaultFlip[] Flips =
        {
            // 1.7.0 moved the four overhauls from on-by-default to off-by-default.
            new DefaultFlip(1, "genepool", true),
            new DefaultFlip(1, "xenogenes", true),
            new DefaultFlip(1, "genetics", true),
            new DefaultFlip(1, "scifinames", true),
        };

        private static readonly KeyMove[] Moves =
        {
            // 1.7.0 merged Genepool Overhaul and Xenotype Gene Integration into one Genetics
            // Overhaul, which took over the "genetics" key; the research overhaul moved aside to
            // "geneticsresearch". Sources are read from a snapshot taken before any move is
            // applied, so "genetics" reaches its new home before the merged group overwrites it.
            new KeyMove(2, "genetics", "geneticsresearch"),
            new KeyMove(2, "genepool", "genetics"),
            new KeyMove(2, "xenogenes", "genetics"),

            // 1.7.0 then realigned every child key with its group, so a key's prefix always names
            // the group it belongs to. Same snapshot rule: "genetics.*" is both source and target.
            new KeyMove(3, "genetics.core", "geneticsresearch.core"),
            new KeyMove(3, "genetics.resplice", "geneticsresearch.resplice"),
            new KeyMove(3, "genetics.extractortiers", "geneticsresearch.extractortiers"),
            new KeyMove(3, "genetics.genenodes", "geneticsresearch.genenodes"),
            new KeyMove(3, "genetics.generipper", "geneticsresearch.generipper"),
            new KeyMove(3, "genetics.genefab", "geneticsresearch.genefab"),
            new KeyMove(3, "genetics.vqea", "geneticsresearch.vqea"),
            new KeyMove(3, "genetics.agtools", "geneticsresearch.agtools"),
            new KeyMove(3, "genetics.alphagenes", "geneticsresearch.alphagenes"),
            new KeyMove(3, "genepool.agsummons", "genetics.agsummons"),
            new KeyMove(3, "genepool.wvcdupes", "genetics.wvcdupes"),
            new KeyMove(3, "genepool.dedup", "genetics.dedup"),
            new KeyMove(3, "genepool.hussaraptitudes", "genetics.hussaraptitudes"),
            new KeyMove(3, "genepool.bsdupes", "genetics.bsdupes"),
            new KeyMove(3, "xenotypes.boglegwater", "genetics.boglegwater"),
            new KeyMove(3, "xenotypes.stonebornskin", "genetics.stonebornskin"),
            new KeyMove(3, "xenotypes.neanderthalfrost", "genetics.neanderthalfrost"),
            new KeyMove(3, "xenotypes.wvcspawns", "genetics.wvcspawns"),

            // 1.8.0 replaced the single xenotypes.factions toggle with one key per faction-owning
            // mod, so the xenotype faction rework can be taken or left per mod. A player who turned
            // the old toggle off opted out of xenotype faction meddling entirely - fan the choice
            // out to every new key, including the ones with no predecessor. xenotypes.wvcchances
            // folded into xenotypes.vanilla, which now owns every vanilla-faction roster edit;
            // sharing a target means the result is on if either source was.
            new KeyMove(4, "xenotypes.factions", "xenotypes.vanilla"),
            new KeyMove(4, "xenotypes.factions", "xenotypes.royalty"),
            new KeyMove(4, "xenotypes.factions", "xenotypes.odyssey"),
            new KeyMove(4, "xenotypes.factions", "xenotypes.rimsenal"),
            new KeyMove(4, "xenotypes.wvcchances", "xenotypes.vanilla"),
        };

        /// <summary>
        /// Returns the keys this migration would pin, without touching the settings. Used by tests.
        /// </summary>
        internal static List<string> PendingKeys(RebalancePatchesSettings settings)
        {
            var pending = new List<string>();
            if (settings == null || settings.configVersion >= CurrentVersion || !settings.CameFromDisk)
                return pending;
            foreach (DefaultFlip flip in Flips)
                if (flip.version > settings.configVersion && !settings.TryGet(flip.key, out _))
                    pending.Add(flip.key);
            return pending;
        }

        /// <summary>Returns true when the config was changed and needs writing back to disk.</summary>
        public static bool Apply(RebalancePatchesSettings settings)
        {
            if (settings == null || settings.configVersion >= CurrentVersion)
                return false;

            // No config on disk means a fresh install: current defaults are what the player wants.
            // Still stamp the version, so a later flip doesn't mistake this for a pre-versioned config.
            if (!settings.CameFromDisk)
            {
                settings.configVersion = CurrentVersion;
                return true;
            }

            var pinned = new List<string>();
            var moved = new List<string>();
            for (int version = settings.configVersion + 1; version <= CurrentVersion; version++)
            {
                foreach (DefaultFlip flip in Flips)
                {
                    if (flip.version != version || settings.TryGet(flip.key, out _))
                        continue;
                    settings.Set(flip.key, flip.previousDefault);
                    pinned.Add(flip.key);
                }
                moved.AddRange(ApplyMoves(settings, version));
            }

            settings.configVersion = CurrentVersion;

            if (pinned.Count > 0)
                Log.Message($"[Rebalance Patches] Config upgraded to v{CurrentVersion}. Kept these on, "
                    + "as they were before the update and they affect existing saves: "
                    + string.Join(", ", pinned.ToArray())
                    + ". Turn them off in the mod settings if you want the new defaults.");
            if (moved.Count > 0)
                Log.Message("[Rebalance Patches] Carried settings across renamed groups: "
                    + string.Join(", ", moved.ToArray()) + ".");
            return true;
        }

        private static List<string> ApplyMoves(RebalancePatchesSettings settings, int version)
        {
            var applied = new List<string>();
            KeyMove[] due = Moves.Where(m => m.version == version).ToArray();
            if (due.Length == 0)
                return applied;

            // Snapshot first: a key can be both a source and a target within the same version.
            var before = new Dictionary<string, bool>(settings.values);
            var targets = new Dictionary<string, bool>();
            foreach (KeyMove move in due)
            {
                if (!before.TryGetValue(move.from, out bool value))
                    continue;
                targets[move.to] = targets.TryGetValue(move.to, out bool sofar) ? sofar || value : value;
                applied.Add($"{move.from} -> {move.to}");
            }

            foreach (KeyMove move in due)
                settings.values.Remove(move.from);
            foreach (KeyValuePair<string, bool> target in targets)
                settings.values[target.Key] = target.Value;
            return applied;
        }
    }
}
