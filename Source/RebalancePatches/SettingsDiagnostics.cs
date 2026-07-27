using System;
using System.Collections.Generic;

namespace RebalancePatches
{
    /// <summary>Turns the settings state into stable strings: a one-line load summary, and an
    /// effective-value signature compared across settings-menu closes to tell whether the
    /// patch-gating state actually changed (which is what makes a cached patch flattening stale).</summary>
    internal static class SettingsDiagnostics
    {
        /// <summary>A canonical, order-stable snapshot of every registered setting's effective value.
        /// Computed while the game is running, so XML-declared defaults are already registered.</summary>
        public static string EffectiveSignature()
        {
            var parts = new List<string>();
            foreach (RebalanceGroup group in SettingsRegistry.Groups)
            {
                parts.Add(group.key + "=" + Flag(SettingsRegistry.GetEffective(group.key)));
                foreach (RebalanceToggle child in group.children)
                    parts.Add(child.key + "=" + Flag(SettingsRegistry.GetEffective(child.key)));
                foreach (RebalanceSlider slider in group.sliders)
                    parts.Add(slider.key + "=" + Flag(SettingsRegistry.GetEffective(slider.key))
                        + ":" + SettingsRegistry.GetEffectiveValue(slider.key));
            }
            parts.Sort(StringComparer.Ordinal);
            return string.Join(";", parts);
        }

        /// <summary>Keys whose effective value differs between two signatures.</summary>
        public static List<string> ChangedKeys(string previous, string current)
        {
            Dictionary<string, string> a = Parse(previous);
            Dictionary<string, string> b = Parse(current);
            var keys = new HashSet<string>(a.Keys);
            keys.UnionWith(b.Keys);

            var changed = new List<string>();
            foreach (string key in keys)
            {
                a.TryGetValue(key, out string av);
                b.TryGetValue(key, out string bv);
                if (av != bv)
                    changed.Add(key);
            }
            changed.Sort(StringComparer.Ordinal);
            return changed;
        }

        /// <summary>The overrides stored on disk, for the load-time log line.</summary>
        public static string LoadedSummary(RebalancePatchesSettings settings)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<string, bool> kv in settings.values)
                parts.Add(kv.Key + "=" + (kv.Value ? "on" : "off"));
            foreach (KeyValuePair<string, int> kv in settings.intValues)
                parts.Add(kv.Key + "=" + kv.Value);
            parts.Sort(StringComparer.Ordinal);
            return parts.Count == 0 ? "all defaults" : string.Join(", ", parts);
        }

        private static string Flag(bool on) => on ? "1" : "0";

        private static Dictionary<string, string> Parse(string signature)
        {
            var map = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(signature))
                return map;
            foreach (string part in signature.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0)
                    map[part.Substring(0, eq)] = part.Substring(eq + 1);
            }
            return map;
        }
    }
}
