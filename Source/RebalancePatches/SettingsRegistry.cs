using System.Collections.Generic;
using Verse;

namespace RebalancePatches
{
    // Label and description text lives in Languages/English/Keyed, keyed by the setting key
    // (RBP.<key>.label / RBP.<key>.desc), so every user-facing string here is translatable.
    public class RebalanceToggle
    {
        private static readonly string[] NoMods = new string[0];

        public readonly string key;
        public string label => ("RBP." + key + ".label").Translate();
        public string description => ("RBP." + key + ".desc").Translate();
        public readonly bool defaultOn;
        public readonly string[] requiredMods;
        public readonly string[] anyOfMods;
        public readonly string dependsOn;

        public RebalanceToggle(string key, bool defaultOn = true,
            string[] requiredMods = null, string[] anyOfMods = null, string dependsOn = null)
        {
            this.key = key;
            this.defaultOn = defaultOn;
            this.requiredMods = requiredMods ?? NoMods;
            this.anyOfMods = anyOfMods ?? NoMods;
            this.dependsOn = dependsOn;
        }
    }

    public class RebalanceSlider
    {
        private static readonly string[] NoMods = new string[0];

        public readonly string key;
        public string label => ("RBP." + key + ".label").Translate();
        public string description => ("RBP." + key + ".desc").Translate();
        public readonly int defaultValue;
        public readonly int min;
        public readonly int max;
        public readonly bool defaultOn;
        public readonly string[] requiredMods;

        public RebalanceSlider(string key, int defaultValue, int min, int max,
            bool defaultOn = true, string[] requiredMods = null)
        {
            this.key = key;
            this.defaultValue = defaultValue;
            this.min = min;
            this.max = max;
            this.defaultOn = defaultOn;
            this.requiredMods = requiredMods ?? NoMods;
        }
    }

    public class RebalanceGroup
    {
        private static readonly string[] NoMods = new string[0];

        public readonly string key;
        public string label => ("RBP." + key + ".label").Translate();
        public string description => ("RBP." + key + ".desc").Translate();
        public readonly bool defaultOn;
        public readonly List<RebalanceToggle> children;
        public readonly List<RebalanceSlider> sliders;
        public readonly string[] requiredMods;
        public readonly bool isOverhaul;

        public RebalanceGroup(string key, List<RebalanceToggle> children, bool defaultOn = true,
            List<RebalanceSlider> sliders = null, string[] requiredMods = null, bool isOverhaul = false)
        {
            this.key = key;
            this.children = children;
            this.defaultOn = defaultOn;
            this.sliders = sliders ?? new List<RebalanceSlider>();
            this.requiredMods = requiredMods ?? NoMods;
            this.isOverhaul = isOverhaul;
        }
    }

    public static class SettingsRegistry
    {
        public static readonly List<RebalanceGroup> Groups = new List<RebalanceGroup>
        {
            new RebalanceGroup("genetics", new List<RebalanceToggle>
            {
                new RebalanceToggle("genetics.agsummons",
                    requiredMods: new[] { "owlchemist.cherrypicker", "sarg.alphagenes" }),
                new RebalanceToggle("genetics.wvcdupes",
                    requiredMods: new[] { "owlchemist.cherrypicker", "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("genetics.dedup",
                    requiredMods: new[] { "owlchemist.cherrypicker" },
                    anyOfMods: new[] { "sarg.alphagenes", "wvc.sergkart.races.biotech", "redmattis.bigsmall.core",
                        "det.avaloi", "det.brawnum", "det.halffoot", "det.stoneborn",
                        "rimsenal.askbarn", "rimsenal.harana", "rimsenal.zohar",
                        "vanillaracesexpanded.archon", "vanillaracesexpanded.fungoid", "vanillaracesexpanded.genie",
                        "vanillaracesexpanded.highmate", "vanillaracesexpanded.hussar", "vanillaracesexpanded.phytokin",
                        "vanillaracesexpanded.pigskin", "vanillaracesexpanded.saurid" }),
                new RebalanceToggle("genetics.hussaraptitudes",
                    requiredMods: new[] { "vanillaracesexpanded.hussar" }),
                new RebalanceToggle("genetics.bsdupes",
                    requiredMods: new[] { "owlchemist.cherrypicker", "redmattis.bigsmall.core" }),
                new RebalanceToggle("genetics.boglegwater",
                    requiredMods: new[] { "det.boglegs", "sarg.alphagenes" }),
                new RebalanceToggle("genetics.stonebornskin",
                    requiredMods: new[] { "det.stoneborn", "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("genetics.neanderthalfrost",
                    requiredMods: new[] { "sarg.alphagenes", "ludeon.rimworld.biotech" }),
                new RebalanceToggle("genetics.wvcspawns",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
            }, defaultOn: false, isOverhaul: true),

            new RebalanceGroup("geneticsresearch", new List<RebalanceToggle>
            {
                new RebalanceToggle("geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.resplice",
                    requiredMods: new[] { "resplice.xotr.core" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.extractortiers",
                    requiredMods: new[] { "redmattis.geneextractor" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.genenodes",
                    requiredMods: new[] { "redmattis.geneextractor" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.generipper",
                    anyOfMods: new[] { "defi.generipper", "danielwedemeyer.generipper" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.genefab",
                    requiredMods: new[] { "amch.eragon.hcgenefabrication" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.vqea",
                    requiredMods: new[] { "vanillaquestsexpanded.ancients" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.consumables",
                    anyOfMods: new[] { "sarg.alphagenes", "redmattis.bigsmall.core", "wvc.sergkart.races.biotech" },
                    dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.alphagenes",
                    requiredMods: new[] { "sarg.alphagenes" }),
            }, defaultOn: false, requiredMods: new[] { "ludeon.rimworld.biotech" }, isOverhaul: true),

            
            new RebalanceGroup("xenotypes", new List<RebalanceToggle>
            {
                new RebalanceToggle("xenotypes.vanilla"),
                new RebalanceToggle("xenotypes.royalty",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
                new RebalanceToggle("xenotypes.odyssey",
                    requiredMods: new[] { "ludeon.rimworld.odyssey" }),
                new RebalanceToggle("xenotypes.rimsenal",
                    requiredMods: new[] { "rimsenal.spacer" }),
            }, requiredMods: new[] { "ludeon.rimworld.biotech" }, isOverhaul: true),

            new RebalanceGroup("scifinames", new List<RebalanceToggle>
            {
                new RebalanceToggle("scifinames.bsraces",
                    requiredMods: new[] { "redmattis.bigsmall" }),
                new RebalanceToggle("scifinames.bigsmall",
                    requiredMods: new[] { "redmattis.bigsmall.core" }),
                new RebalanceToggle("scifinames.heaven",
                    requiredMods: new[] { "redmattis.heaven" }),
                new RebalanceToggle("scifinames.yokai",
                    requiredMods: new[] { "redmattis.yokai" }),
                new RebalanceToggle("scifinames.lamias",
                    requiredMods: new[] { "redmattis.lamiasandothersnakes" }),
                new RebalanceToggle("scifinames.slimes",
                    requiredMods: new[] { "redmattis.bsslimes" }),
                new RebalanceToggle("scifinames.morexenos",
                    requiredMods: new[] { "redmattis.morexenos" }),
                new RebalanceToggle("scifinames.wvc",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("scifinames.alphagenes",
                    requiredMods: new[] { "sarg.alphagenes" }),
            }, defaultOn: false, isOverhaul: true),

            new RebalanceGroup("vse", new List<RebalanceToggle>
            {
                new RebalanceToggle("vse.expertiseconsolidation"),
                new RebalanceToggle("vse.expertisegeneration"),
            }, defaultOn: false, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("vse.expertisegenerationchance", 20, 0, 50),
            }, requiredMods: new[] { "vanillaexpanded.skills" }, isOverhaul: true),

            new RebalanceGroup("worldaugment", new List<RebalanceToggle>
            {
                new RebalanceToggle("worldaugment.bionics"),
                new RebalanceToggle("worldaugment.cyberbrains",
                    requiredMods: new[] { "moistestwhale.gitscyberbrains" },
                    dependsOn: "worldaugment.bionics"),
            }, defaultOn: false, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("worldaugment.frequency", 100, 0, 200),
            }, isOverhaul: true),

            new RebalanceGroup("cybernetics", new List<RebalanceToggle>
            {
                new RebalanceToggle("cybernetics.modules",
                    requiredMods: new[] { "ebsg.framework" }),
                new RebalanceToggle("cybernetics.npchosts",
                    requiredMods: new[] { "ebsg.framework" }),
                new RebalanceToggle("cybernetics.advancedorgans",
                    requiredMods: new[] { "vat.epoeforked" }),
                new RebalanceToggle("cybernetics.thoracicframe"),
                new RebalanceToggle("cybernetics.tiers"),
                new RebalanceToggle("cybernetics.micromachines",
                    requiredMods: new[] { "moistestwhale.gitscyberbrains" }),
                new RebalanceToggle("cybernetics.archotechshards",
                    dependsOn: "cyberneticsresearch.body"),
                new RebalanceToggle("cybernetics.androidblocklist",
                    requiredMods: new[] { "vanillaracesexpanded.android" }),
                new RebalanceToggle("cybernetics.echobrains",
                    requiredMods: new[] { "moistestwhale.gitscyberbrains" }),
                new RebalanceToggle("cybernetics.cyberbrainquality",
                    requiredMods: new[] { "moistestwhale.gitscyberbrains" }),
                new RebalanceToggle("cybernetics.livingframe"),
                new RebalanceToggle("cybernetics.ascension",
                    requiredMods: new[] { "vanillaracesexpanded.android" }),
                new RebalanceToggle("cybernetics.androidconversion",
                    requiredMods: new[] { "vanillaracesexpanded.android" }),
            }, defaultOn: false, isOverhaul: true),

            new RebalanceGroup("cyberneticsresearch", new List<RebalanceToggle>
            {
                new RebalanceToggle("cyberneticsresearch.core",
                    dependsOn: "cybernetics.modules"),
                new RebalanceToggle("cyberneticsresearch.body",
                    dependsOn: "cyberneticsresearch.core"),
                new RebalanceToggle("cyberneticsresearch.modules",
                    dependsOn: "cyberneticsresearch.core"),
                new RebalanceToggle("cyberneticsresearch.cyberbrains",
                    requiredMods: new[] { "moistestwhale.gitscyberbrains" },
                    dependsOn: "cyberneticsresearch.core"),
                new RebalanceToggle("cyberneticsresearch.mind",
                    anyOfMods: new[] { "cedaro.psychicimplant", "hlx.ultratechalteredcarbon" },
                    dependsOn: "cyberneticsresearch.core"),
                new RebalanceToggle("cyberneticsresearch.capstones",
                    dependsOn: "cyberneticsresearch.core"),
                new RebalanceToggle("cyberneticsresearch.retire",
                    dependsOn: "cyberneticsresearch.core"),
            }, defaultOn: false, isOverhaul: true),

            new RebalanceGroup("ideology", new List<RebalanceToggle>
            {
                new RebalanceToggle("ideology.genememes",
                    requiredMods: new[] { "ludeon.rimworld.biotech" }),
            }, requiredMods: new[] { "ludeon.rimworld.ideology" }),

            new RebalanceGroup("rimiot", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimiot.costs"),
                new RebalanceToggle("rimiot.power"),
            }, requiredMods: new[] { "cn.rimiot" }),

            new RebalanceGroup("autohydroponics", new List<RebalanceToggle>
            {
                new RebalanceToggle("autohydroponics.costlier"),
            }, requiredMods: new[] { "poncho.automatichydroponics" }),

            new RebalanceGroup("biowarfare", new List<RebalanceToggle>
            {
                new RebalanceToggle("biowarfare.researchtree"),
            }, requiredMods: new[] { "ushanka.biologicalwarfare" }),

            new RebalanceGroup("altered", new List<RebalanceToggle>
            {
                new RebalanceToggle("altered.shieldbelt",
                    requiredMods: new[] { "vanillaexpanded.vaeaccessories" }),
                new RebalanceToggle("altered.shieldsfab"),
                new RebalanceToggle("altered.cuirassier"),
                new RebalanceToggle("altered.traitblacklist"),
                new RebalanceToggle("altered.sleevecancer"),
                new RebalanceToggle("altered.sleevemarket"),
            }, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("altered.relayrange",
                    10, 1, 25),
                new RebalanceSlider("altered.sleevemarket.richness",
                    3, 1, 8),
            }, requiredMods: new[] { "hlx.ultratechalteredcarbon" }),

            new RebalanceGroup("bigsmall", new List<RebalanceToggle>
            {
                new RebalanceToggle("bigsmall.madscience"),
            }, requiredMods: new[] { "redmattis.bigsmall.core" }),

            new RebalanceGroup("vfepirates", new List<RebalanceToggle>
            {
                new RebalanceToggle("vfepirates.chargeweapons"),
                new RebalanceToggle("vfepirates.empirescenario",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
            }, requiredMods: new[] { "oskarpotocki.vfe.pirates" }),

            new RebalanceGroup("vfeempire", new List<RebalanceToggle>
            {
                new RebalanceToggle("vfeempire.qol"),
            }, requiredMods: new[] { "oskarpotocki.vfe.empire" }),

            new RebalanceGroup("rimsenal", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimsenal.corpcost"),
                new RebalanceToggle("rimsenal.armortechs"),
                new RebalanceToggle("rimsenal.modularweapons"),
            }, requiredMods: new[] { "rimsenal.core" }),

            new RebalanceGroup("rimsenalspacer", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimsenalspacer.caravanmechs"),
                new RebalanceToggle("rimsenalspacer.smartweapons"),
            }, requiredMods: new[] { "rimsenal.spacer" }),

            new RebalanceGroup("memes", new List<RebalanceToggle>
            {
                new RebalanceToggle("memes.factions",
                    requiredMods: new[] { "vanillaexpanded.vmemese", "sarg.alphamemes" },
                    anyOfMods: new[] { "rimsenal.spacer", "rimsenal.federation" }),
                new RebalanceToggle("memes.anomalytraits",
                    requiredMods: new[] { "ludeon.rimworld.anomaly", "ludeon.rimworld.ideology" }),
                new RebalanceToggle("memes.inspirations",
                    requiredMods: new[] { "ludeon.rimworld.ideology" }),
            }),

            new RebalanceGroup("implants", new List<RebalanceToggle>
            {
                new RebalanceToggle("implants.chipbad"),
                new RebalanceToggle("implants.chiptiers",
                    requiredMods: new[] { "sarg.alphamechs" }),
                new RebalanceToggle("implants.voicelockmasochist"),
                new RebalanceToggle("implants.shoulderslimes",
                    requiredMods: new[] { "redmattis.bsslimes" }),
                new RebalanceToggle("implants.waterpathing"),
                new RebalanceToggle("implants.boosterrange",
                    requiredMods: new[] { "sarg.alphagenes" }),
                new RebalanceToggle("implants.cerebrexsurgery",
                    requiredMods: new[] { "ludeon.rimworld.biotech", "ludeon.rimworld.odyssey" }),
            }, requiredMods: new[] { "lts.i" }),

            new RebalanceGroup("vreinsector", new List<RebalanceToggle>
            {
                new RebalanceToggle("vreinsector.colossalweapons",
                    requiredMods: new[] { "oskarpotocki.vfe.insectoid2", "redmattis.bigsmall.core" }),
            }, requiredMods: new[] { "vanillaracesexpanded.insector" }),

            new RebalanceGroup("impactweaponry", new List<RebalanceToggle>
            {
                new RebalanceToggle("impactweaponry.bolterprereq",
                    requiredMods: new[] { "oskarpotocki.vfe.pirates" }),
            }, requiredMods: new[] { "detvisor.impactweaponryreloaded" }),

            new RebalanceGroup("spacerarsenal", new List<RebalanceToggle>
            {
                new RebalanceToggle("spacerarsenal.prereqs",
                    anyOfMods: new[] { "vanillaexpanded.vwe", "vanillaexpanded.vwec" }),
            }, requiredMods: new[] { "det.spacerarsenal" }),

            new RebalanceGroup("vanilla", new List<RebalanceToggle>
            {
                new RebalanceToggle("vanilla.healingenhancer",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
                new RebalanceToggle("vanilla.mechraidgroups",
                    requiredMods: new[] { "ludeon.rimworld.biotech" }),
                new RebalanceToggle("vanilla.toxicmeat",
                    requiredMods: new[] { "vanillaexpanded.vaewaste" }),
                new RebalanceToggle("vanilla.hideemptyresearchtabs"),
                new RebalanceToggle("vanilla.creepjoinersurgery",
                    requiredMods: new[] { "ludeon.rimworld.anomaly" }),
            }, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("vanilla.genecomplexitybase",
                    10, 0, 25, requiredMods: new[] { "ludeon.rimworld.biotech" }),
                new RebalanceSlider("vanilla.genecomplexityprocessor",
                    3, 1, 10, requiredMods: new[] { "ludeon.rimworld.biotech" }),
            }),

            new RebalanceGroup("vqea", new List<RebalanceToggle>
            {
                new RebalanceToggle("vqea.sittable"),
                new RebalanceToggle("vqea.giantweapons",
                    requiredMods: new[] { "redmattis.bigsmall.core" }),
                new RebalanceToggle("vqea.patientgown"),
                new RebalanceToggle("vqea.gowntrade"),
                new RebalanceToggle("vqea.injectorwhitelist"),
                new RebalanceToggle("vqea.nofabricatedarchite",
                    requiredMods: new[] { "owlchemist.cherrypicker", "amch.eragon.hcgenefabrication" }),
            }, requiredMods: new[] { "vanillaquestsexpanded.ancients" }),

            new RebalanceGroup("eltex", new List<RebalanceToggle>
            {
                new RebalanceToggle("eltex.spawns",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
            }, requiredMods: new[] { "zal.eltexweaponry" }),

            new RebalanceGroup("smallfurniture", new List<RebalanceToggle>
            {
                new RebalanceToggle("smallfurniture.hitechbenchresearch"),
            }, requiredMods: new[] { "xercaine.furniture.small" }),

            new RebalanceGroup("bambamelee", new List<RebalanceToggle>
            {
                new RebalanceToggle("bambamelee.ultratrade"),
            }, requiredMods: new[] { "bamba.allbambamelee.tiered" }),

            new RebalanceGroup("yart", new List<RebalanceToggle>
            {
                new RebalanceToggle("yart.unlockgrouping"),
            }, requiredMods: new[] { "seohyeon.yart" }),

            new RebalanceGroup("progressionrobotics", new List<RebalanceToggle>
            {
                new RebalanceToggle("progressionrobotics.groupmechs",
                    anyOfMods: new[] { "ludeon.rimworld.biotech", "sarg.alphamechs" }),
                new RebalanceToggle("progressionrobotics.groupgear",
                    anyOfMods: new[] { "ludeon.rimworld.biotech", "sarg.alphamechs" }),
            }, defaultOn: false, requiredMods: new[] { "ferny.progressionrobotics2" }),

            new RebalanceGroup("gits", new List<RebalanceToggle>
            {
                new RebalanceToggle("gits.merchant"),
                new RebalanceToggle("gits.surgeries",
                    requiredMods: new[] { "vat.epoeforked" }),
                new RebalanceToggle("gits.mentalbreak"),
                new RebalanceToggle("gits.extremedrawbacks"),
                new RebalanceToggle("gits.research"),
                new RebalanceToggle("gits.cyberbrainnames"),
            }, requiredMods: new[] { "moistestwhale.gitscyberbrains" }),

            new RebalanceGroup("alphagenes", new List<RebalanceToggle>
            {
                new RebalanceToggle("alphagenes.genepacks"),
                new RebalanceToggle("alphagenes.beautyrename",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
            }, requiredMods: new[] { "sarg.alphagenes" }),

            new RebalanceGroup("alphamemes", new List<RebalanceToggle>
            {
                new RebalanceToggle("alphamemes.vacstonetiles",
                    requiredMods: new[] { "ludeon.rimworld.odyssey" }),
            }, requiredMods: new[] { "sarg.alphamemes" }),

            new RebalanceGroup("geneconflicts", new List<RebalanceToggle>
            {
                new RebalanceToggle("geneconflicts.bloodlust",
                    requiredMods: new[] { "redmattis.bigsmall.core", "vanillaracesexpanded.highmate" }),
                new RebalanceToggle("geneconflicts.psychic",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("geneconflicts.firefoam",
                    requiredMods: new[] { "wvc.sergkart.races.biotech", "sarg.alphagenes" }),
                new RebalanceToggle("geneconflicts.hemogen"),
                new RebalanceToggle("geneconflicts.deathless"),
                new RebalanceToggle("geneconflicts.dodge",
                    anyOfMods: new[] { "vanillaquestsexpanded.ancients", "rimsenal.harana", "rimsenal.askbarn", "det.keshig", "elsov.highborn" }),
                new RebalanceToggle("geneconflicts.claws",
                    anyOfMods: new[] { "sarg.alphagenes", "vanillaracesexpanded.saurid", "vanillaracesexpanded.sanguophage", "wvc.sergkart.races.biotech", "redmattis.bigsmall.core", "vanillaracesexpanded.insector", "vanillaquestsexpanded.ancients" }),
                new RebalanceToggle("geneconflicts.bleedrate",
                    requiredMods: new[] { "redmattis.bigsmall.core", "vanillaracesexpanded.genie" }),
                new RebalanceToggle("geneconflicts.flirty",
                    requiredMods: new[] { "vanillaracesexpanded.highmate", "redmattis.bigsmall.core" }),
                new RebalanceToggle("geneconflicts.meleespeed",
                    requiredMods: new[] { "det.brawnum", "vanillaracesexpanded.archon" }),
            }, requiredMods: new[] { "ludeon.rimworld.biotech" }),

            new RebalanceGroup("odyssey", new List<RebalanceToggle>
            {
                new RebalanceToggle("odyssey.shuttle"),
                new RebalanceToggle("odyssey.vacuumtrims",
                    requiredMods: new[] { "vanillaexpanded.gravship" },
                    anyOfMods: new[] { "rimsenal.core", "rimsenal.federation", "hlx.ultratechalteredcarbon", "det.spacerarsenal", "detvisor.impactweaponryreloaded" }),
            }, requiredMods: new[] { "ludeon.rimworld.odyssey" }),

            new RebalanceGroup("vge", new List<RebalanceToggle>
            {
                new RebalanceToggle("vge.gravjumpercapacity"),
            }, requiredMods: new[] { "vanillaexpanded.gravship" }),

            new RebalanceGroup("dev", new List<RebalanceToggle>
            {
                new RebalanceToggle("dev.genedump",
                    defaultOn: false),
                new RebalanceToggle("dev.xenofactiondump",
                    defaultOn: false),
                new RebalanceToggle("dev.recipedump",
                    defaultOn: false),
                new RebalanceToggle("dev.hediffdump",
                    defaultOn: false),
                new RebalanceToggle("dev.researchdump",
                    defaultOn: false),
                new RebalanceToggle("dev.thingdump",
                    defaultOn: false),
                new RebalanceToggle("dev.bodydump",
                    defaultOn: false),
                new RebalanceToggle("dev.acquisitiondump",
                    defaultOn: false),
                new RebalanceToggle("dev.modrulesdump",
                    defaultOn: false),
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

        public static RebalanceToggle ToggleOf(string key)
        {
            foreach (RebalanceGroup g in Groups)
                foreach (RebalanceToggle c in g.children)
                    if (c.key == key)
                        return c;
            return null;
        }

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
