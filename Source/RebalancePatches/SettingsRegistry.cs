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

    public class RebalanceSlider
    {
        public readonly string key;
        public readonly string label;
        public readonly string description;
        public readonly int defaultValue;
        public readonly int min;
        public readonly int max;
        public readonly bool defaultOn;

        public RebalanceSlider(string key, string label, string description, int defaultValue, int min, int max,
            bool defaultOn = true)
        {
            this.key = key;
            this.label = label;
            this.description = description;
            this.defaultValue = defaultValue;
            this.min = min;
            this.max = max;
            this.defaultOn = defaultOn;
        }
    }

    public class RebalanceGroup
    {
        public readonly string key;
        public readonly string label;
        public readonly bool defaultOn;
        public readonly List<RebalanceToggle> children;
        public readonly List<RebalanceSlider> sliders;

        public RebalanceGroup(string key, string label, List<RebalanceToggle> children, bool defaultOn = true,
            List<RebalanceSlider> sliders = null)
        {
            this.key = key;
            this.label = label;
            this.children = children;
            this.defaultOn = defaultOn;
            this.sliders = sliders ?? new List<RebalanceSlider>();
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
                    "Network buildings draw no power and need no wiring, and their descriptions drop the power notes."),
            }),

            new RebalanceGroup("altered", "Altered Carbon", new List<RebalanceToggle>
            {
                new RebalanceToggle("altered.shieldbelt", "Disable the ranged shield belt from VAE Accessories",
                    "Makes Vanilla Apparel Expanded - Accessories' ranged shield belt unobtainable in favour of Altered Carbon's cuirassier belt. Needs both mods. The def is kept, so saves are unaffected."),
            }, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("altered.relayrange", "Casting relay range per relay",
                    "World tiles of needlecasting range each powered casting relay adds to a neural matrix. Altered Carbon's own value is 5; toggling this off keeps it.",
                    10, 1, 25),
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

            new RebalanceGroup("genetics", "Genetics Research Overhaul", new List<RebalanceToggle>
            {
                new RebalanceToggle("genetics.core", "Genetics research tab and tree (Biotech)",
                    "Adds a Genetics research tab rooted on a new basic genetic sampling project that unlocks the gene extractor and gene bank. Xenogermination becomes xenogerm assembly and moves there with gene processor and archogenetics, all at spacer tech with a hi-tech research bench required. The other toggles in this group do nothing without this one."),
                new RebalanceToggle("genetics.resplice", "ReSplice: Core buildings via dedicated research",
                    "ReSplice: Core's gene centrifuge and xenogerm duplicator move behind new genepack centrifuge and xenogerm replicator research projects on the Genetics tab and are renamed to match. Does nothing unless the tab toggle is on."),
                new RebalanceToggle("genetics.extractortiers", "Gene Extractor Tiers vats via dedicated research",
                    "Gene Extractor Tiers' gene extraction vat unlocks from a new research project after gene processor, and its archite vats from a new archite gene extraction project after archogenetics. Does nothing unless the tab toggle is on."),
                new RebalanceToggle("genetics.genenodes", "Gene nodes via dedicated research, pricier archite nodes",
                    "Gene Extractor Tiers' base gene nodes unlock from a new gene nodes project after xenogerm assembly. Archite gene nodes, including Gene Nodes - Genes for Sale's, move behind a new archite gene nodes project after archogenetics and cost more to build. Does nothing unless the tab toggle is on."),
                new RebalanceToggle("genetics.generipper", "Gene Ripper via dedicated research",
                    "Gene Ripper's machine unlocks from a new gene ripper research project after xenogerm assembly. Does nothing unless the tab toggle is on."),
                new RebalanceToggle("genetics.genefab", "Gene Fabrication as an archogenetics capstone",
                    "Gene Fabrication's research project moves to the Genetics tab and requires archogenetics instead of gene processor plus fabrication. Does nothing unless the tab toggle is on."),
                new RebalanceToggle("genetics.vqea", "Buildable VQE Ancients archogen laboratory",
                    "A new archogen engineering research project after archogenetics lets you build VQE Ancients' archogen injector and its linkable lab facilities at archite-tier costs. Recovering them from ancient labs remains the early route. Does nothing unless the tab toggle is on."),
                new RebalanceToggle("genetics.alphagenes", "Alpha Genes xenogenetics lab quest names",
                    "Renames Alpha Genes' abandoned biotech lab quest and site to xenogenetics lab flavour, as Progression: Genetics did. Works on its own."),
            }),

            new RebalanceGroup("odyssey", "Odyssey", new List<RebalanceToggle>
            {
                new RebalanceToggle("odyssey.shuttle", "Long-range passenger shuttle",
                    "Raises the passenger shuttle's chemfuel capacity from 400 to 2000 and its cargo mass capacity from 500 to 2000."),
            }),
        };

        private static RebalancePatchesSettings settings;
        private static readonly Dictionary<string, bool> xmlDefaults = new Dictionary<string, bool>();

        public static void Bind(RebalancePatchesSettings boundSettings) => settings = boundSettings;

        public static void RegisterXmlDefault(string key, bool defaultOn) => xmlDefaults[key] = defaultOn;

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
            if (xmlDefaults.TryGetValue(key, out bool xmlDefault))
                return xmlDefault;
            foreach (RebalanceGroup g in Groups)
            {
                if (g.key == key)
                    return g.defaultOn;
                foreach (RebalanceToggle c in g.children)
                    if (c.key == key)
                        return c.defaultOn;
                foreach (RebalanceSlider s in g.sliders)
                    if (s.key == key)
                        return s.defaultOn;
            }
            return false;
        }

        public static int GetEffectiveValue(string key)
        {
            RebalanceSlider slider = SliderOf(key);
            if (slider == null)
                return 0;
            if (!GetEffective(key))
                return slider.defaultValue;
            return OwnValue(key);
        }

        public static int OwnValue(string key)
        {
            if (settings != null && settings.TryGetInt(key, out int v))
                return v;
            return SliderOf(key)?.defaultValue ?? 0;
        }

        public static bool IsValueOverridden(string key) => settings != null && settings.TryGetInt(key, out _);

        public static void SetValue(string key, int value)
        {
            RebalanceSlider slider = SliderOf(key);
            if (slider == null || settings == null)
                return;
            if (value == slider.defaultValue)
                settings.RemoveInt(key);
            else
                settings.SetInt(key, value);
        }

        public static void ClearValue(string key) => settings?.RemoveInt(key);

        public static RebalanceSlider SliderOf(string key)
        {
            foreach (RebalanceGroup g in Groups)
                foreach (RebalanceSlider s in g.sliders)
                    if (s.key == key)
                        return s;
            return null;
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
                foreach (RebalanceSlider s in g.sliders)
                    if (s.key == key)
                        return g;
            }
            return null;
        }
    }
}
