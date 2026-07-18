using System.Collections.Generic;

namespace RebalancePatches
{
    public class RebalanceToggle
    {
        public readonly string key;
        public readonly string label;
        public readonly string description;
        public readonly bool defaultOn;

        public RebalanceToggle(string key, string label, string description, bool defaultOn = true)
        {
            this.key = key;
            this.label = label;
            this.description = description;
            this.defaultOn = defaultOn;
        }
    }

    public class RebalanceGroup
    {
        public readonly string key;
        public readonly string label;
        public readonly bool defaultOn;
        public readonly List<RebalanceToggle> children;

        public RebalanceGroup(string key, string label, List<RebalanceToggle> children, bool defaultOn = true)
        {
            this.key = key;
            this.label = label;
            this.children = children;
            this.defaultOn = defaultOn;
        }
    }

    public static class SettingsRegistry
    {
        public static readonly List<RebalanceGroup> Groups = new List<RebalanceGroup>
        {
            new RebalanceGroup("rimiot", "RimIOT - Logistic Matrix", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimiot.costs", "Reduce build costs",
                    "Cheaper cable, connector and interface, with advanced components replaced by basic ones."),
                new RebalanceToggle("rimiot.power", "Remove power consumption",
                    "Network buildings draw no power and need no wiring."),
            }),

            new RebalanceGroup("altered", "Altered Carbon", new List<RebalanceToggle>
            {
                new RebalanceToggle("altered.shieldbelt", "Disable the ranged shield belt from VAE Accessories",
                    "Makes Vanilla Apparel Expanded - Accessories' ranged shield belt unobtainable in favour of Altered Carbon's cuirassier belt. Needs both mods. The def is kept, so saves are unaffected."),
            }),

            new RebalanceGroup("gits", "GiTS Cyberbrains", new List<RebalanceToggle>
            {
                new RebalanceToggle("gits.merchant", "Only basic cyberbrains sold by merchants",
                    "Removes the higher-tier cyberbrains from trader stock. They stay craftable and can still spawn on raiders."),
                new RebalanceToggle("gits.surgeries", "Surgeries via EPOE brain surgery, later research to ultratech",
                    "Cyberbrain install and nullify surgeries unlock at EPOE-Forked's Brain Surgery research instead of the GiTS node, and post-basic cyberization research moves to the ultratech tier. Needs EPOE-Forked; does nothing without it."),
                new RebalanceToggle("gits.mentalbreak", "Harsher extreme mental break",
                    "Raises the extreme cyberbrain mental break threshold offset from +20% to +40%."),
                new RebalanceToggle("gits.research", "Streamline the research tree",
                    "Collapses the nanite surgery researches into nanite grafting and removes the empty filler nodes."),
            }),
        };

        private static RebalancePatchesSettings settings;

        public static void Bind(RebalancePatchesSettings boundSettings) => settings = boundSettings;

        public static bool GetEffective(string key)
        {
            RebalanceGroup group = GroupOf(key);
            if (group != null && group.key != key && !GetEffective(group.key))
                return false;
            return Own(key);
        }

        public static bool Own(string key)
        {
            if (settings != null && settings.TryGet(key, out bool v))
                return v;
            return DefaultOf(key);
        }

        public static void Set(string key, bool value) => settings?.Set(key, value);

        public static bool DefaultOf(string key)
        {
            foreach (RebalanceGroup g in Groups)
            {
                if (g.key == key)
                    return g.defaultOn;
                foreach (RebalanceToggle c in g.children)
                    if (c.key == key)
                        return c.defaultOn;
            }
            return false;
        }

        public static RebalanceGroup GroupOf(string key)
        {
            foreach (RebalanceGroup g in Groups)
            {
                if (g.key == key)
                    return g;
                foreach (RebalanceToggle c in g.children)
                    if (c.key == key)
                        return g;
            }
            return null;
        }
    }
}
