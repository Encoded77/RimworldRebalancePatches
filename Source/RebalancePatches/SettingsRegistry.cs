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
                new RebalanceToggle("altered.shieldsfab", "Advanced shields research requires Fabrication",
                    "The advanced shields research additionally requires Fabrication, since its gear can only be crafted at the fabrication bench anyway."),
                new RebalanceToggle("altered.cuirassier", "Cuirassier belt uses vanilla shield scaling",
                    "The cuirassier belt swaps VEF's shield bubble for the vanilla shield comp (120 base max shield, scales with quality, doesn't block outgoing shots)."),
                new RebalanceToggle("altered.traitblacklist", "Neural editor ignores physical/psychic traits",
                    "The stack saving options ignore body-bound traits from Hauts' Added Traits (Awakened, Transcendent and the totem traits), The Sims Traits and Vanilla Traits Expanded, so they aren't carried between sleeves. Entries apply only when their mod is loaded."),
                new RebalanceToggle("altered.sleevecancer", "Fix sleeve quality cancer rates",
                    "Good through legendary sleeve quality genes multiply cancer rate by negative numbers (-0.1 to -0.5), inverting the stat; they become proper reduction factors (x0.9 to x0.5)."),
            }, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("altered.relayrange", "Casting relay range per relay",
                    "World tiles of needlecasting range each powered casting relay adds to a neural matrix. Altered Carbon's own value is 5; toggling this off keeps it.",
                    10, 1, 25),
            }),

            new RebalanceGroup("bigsmall", "Big and Small - Genes & More", new List<RebalanceToggle>
            {
                new RebalanceToggle("bigsmall.madscience", "Mad science field testing requires gun turrets",
                    "The mad science field testing research itself gets the vanilla Gun turrets prerequisite, instead of every turret and ray weapon it unlocks listing Gun turrets separately, which garbles their requirement display."),
                new RebalanceToggle("bigsmall.geneintegrator", "Gene integrator at archite tier",
                    "The gene integrator (turns all xenogenes into endogenes, freeing the slots to stack more) moves behind experimental archite gene tools plus Archogenetics, costs an archite capsule and advanced components to craft, and its market value rises to 4000."),
            }),

            new RebalanceGroup("vfepirates", "Vanilla Factions Expanded - Pirates", new List<RebalanceToggle>
            {
                new RebalanceToggle("vfepirates.chargeweapons", "Clean warcasket charge weapon prerequisites",
                    "Warcasket charge blaster and lance require pulse-charged munitions plus spacer warcasket weaponry, dropping the inherited redundant prerequisite; with VWE - Coilguns, the warcasket railgun gets the same treatment via Mass Drivers. With Warcasket Weapon Quality, its direct-craft recipes get the same research gates the weapon boxes had."),
                new RebalanceToggle("vfepirates.empirescenario", "Empire not permanently hostile to pirates",
                    "The Empire is no longer permanently hostile to the player pirate faction from the Low orbit crash scenario, so reputation can be repaired. Needs Royalty."),
            }),

            new RebalanceGroup("vfeempire", "Vanilla Factions Expanded - Empire", new List<RebalanceToggle>
            {
                new RebalanceToggle("vfeempire.qol", "Royal armchair throne + candelabra glow radius",
                    "Vanilla Furniture Expanded's royal armchair satisfies the Stellarch and High Stellarch throne room seat requirement, and the candelabra shows its light radius when placing."),
            }),

            new RebalanceGroup("rimsenal", "Rimsenal - Core", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimsenal.corpcost", "Corp techs cost 3000",
                    "The three corp tier-1 researches (crystalline, furnace, kinetic) drop from 3050 to 3000 base cost."),
                new RebalanceToggle("rimsenal.armortechs", "Armors require their corp's tech",
                    "Rimsenal armors require their own corporation's research (Greydale modular gear via modular equipment manufacturing, Yeonhwa suits via crystalline techs, Jotun heavy armors via furnace/siege techs, Tesseron carapaces via kinetic techs) instead of vanilla flak/recon/powered armor. Also repairs the artillery armor's malformed siege gun prerequisite."),
                new RebalanceToggle("rimsenal.modularweapons", "Modular weapons via modular equipment manufacturing",
                    "The modular carbine and MRS conversion kit require modular equipment manufacturing instead of vanilla gun researches, and the GD multi launcher additionally requires it alongside Mortars."),
            }),

            new RebalanceGroup("rimsenalspacer", "Rimsenal - Spacer Faction Pack", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimsenalspacer.caravanmechs", "Caravans without mechanoid guards",
                    "Spacer faction trade caravans no longer bring mechanoids as guards; the mechs frequently get left behind and roamed the colony map forever."),
                new RebalanceToggle("rimsenalspacer.smartweapons", "Clean smart weapon research prerequisites",
                    "Smart charge weapons and the smart minigun drop their redundant inherited gunsmithing prerequisite; with Royalty, the smart visor unlocks from the Gunlink research, which is renamed to smart targeting systems."),
            }),

            new RebalanceGroup("memes", "Meme & Ideology Fixes", new List<RebalanceToggle>
            {
                new RebalanceToggle("memes.factions", "No pacifist memes for warlike factions",
                    "With Vanilla Ideology Expanded - Memes and Structures and Alpha Memes: Rimsenal's Spacer, Feral and Federation factions can't generate with the vow of nonviolence meme, which breaks their combat pawn generation."),
                new RebalanceToggle("memes.anomalytraits", "Occultist and void fascination fit inhuman memes",
                    "The Occultist trait and void fascination agree with the Inhuman and Ritualist memes (Anomaly + VIE - Memes and Structures)."),
                new RebalanceToggle("memes.inspirations", "Inspirations respect ideology precepts",
                    "Pawns whose ideology abhors an activity stop rolling inspirations for it: shooting/melee frenzies blocked by VIE - Memes and Structures and Alpha Memes violence precepts, taming inspiration by their ranching/bonding precepts, and Vanilla Social Interactions Expanded's frenzies by the matching precepts. Needs Ideology."),
            }),

            new RebalanceGroup("xenotypes", "Xenotype Spawning", new List<RebalanceToggle>
            {
                new RebalanceToggle("xenotypes.factions", "Xenotypes join fitting factions",
                    "WVC's Mechakin, Rogueformer and Genethrower move from generic outlander/pirate pools to Rimsenal's Spacer factions, and Odyssey's Salvagers/Traders guild gain Rimsenal, Det's and Alpha Genes xenotypes where fitting."),
                new RebalanceToggle("xenotypes.wvcchances", "Fewer WVC xenotypes in generic factions",
                    "WVC's Featherdust, Cat deity, Blank, Sandycat and Undead stop spawning in generic vanilla factions; Undead and Sandycat move to the Horax cult instead (Anomaly)."),
            }),

            new RebalanceGroup("implants", "Integrated Implants", new List<RebalanceToggle>
            {
                new RebalanceToggle("implants.chipbad", "Skill chips survive cleansing effects",
                    "Marks Integrated Implants' skill chip hediffs isBad=false so healer serums, biosculpting and similar purges don't rip them out. Needs EBSG Framework; does nothing without it."),
                new RebalanceToggle("implants.chiptiers", "Mechanitor implants need Alpha Mechs chips",
                    "Mechhive satellite uplink, mechwomb, warprogrammer interface and remote dominator cost Alpha Mechs' tier 4/5/6/6 chips instead of vanilla tier 2/2/3/3 chips. Needs Alpha Mechs; does nothing without it."),
                new RebalanceToggle("implants.voicelockmasochist", "Masochists enjoy the voicelock",
                    "Pawns with the Masochist trait get +8 mood instead of -8 while their voicelock is active."),
                new RebalanceToggle("implants.shoulderslimes", "Shoulder turrets install on the shoulder",
                    "With Big and Small - Slimes loaded, shoulder turret and shoulder charge turret surgeries target the shoulder instead of the torso, which slime bodies lack (avoids errors). Does nothing without that mod."),
                new RebalanceToggle("implants.waterpathing", "Levitating implants ignore water",
                    "Pawns with the psychic levitator or personal grav engine implant move over water at no extra pathing cost."),
                new RebalanceToggle("implants.boosterrange", "Signal boosters stack with Alpha Genes command range genes",
                    "Alpha Genes' increased/decreased command range genes override the mech command radius outright, discarding signal booster implants; with this fix the booster's bonus is added on top of the gene's 35/15 tile radius. Needs Alpha Genes; does nothing without it."),
            }),

            new RebalanceGroup("vreinsector", "Vanilla Races Expanded - Insector", new List<RebalanceToggle>
            {
                new RebalanceToggle("vreinsector.colossalweapons", "Colossal insectors wield giant weapons",
                    "Carriers of the colossal geneline gene (VFE - Insectoids 2) get Big and Small's Giant trait, letting them equip B&S giant weapons. Needs VFE - Insectoids 2 and Big and Small - Genes & More; does nothing without them."),
            }),

            new RebalanceGroup("vse", "Vanilla Skills Expanded", new List<RebalanceToggle>
            {
                new RebalanceToggle("vse.reloadingstat", "Reloading expertise uses vanilla cooldown stat",
                    "The gunner expertise offsets the vanilla ranged cooldown factor instead of Vanilla Expanded Framework's verb cooldown stat, so the bonus shows on weapon stats."),
            }),

            new RebalanceGroup("impactweaponry", "Impact Weaponry - Reloaded", new List<RebalanceToggle>
            {
                new RebalanceToggle("impactweaponry.bolterprereq", "Clean warcasket impact bolter prerequisites",
                    "The warcasket impact bolter requires spacer warcasket weaponry (VFE - Pirates) plus impact shot, dropping the redundant extra prerequisite. Needs VFE - Pirates; does nothing without it."),
            }),

            new RebalanceGroup("spacerarsenal", "Spacer Arsenal", new List<RebalanceToggle>
            {
                new RebalanceToggle("spacerarsenal.prereqs", "Heavy weapons via VWE researches",
                    "With Vanilla Weapons Expanded: brute rifle, clash HMG/rifle and contact/thump grenades require Heavy Weapons plus Fabrication. With VWE - Coilguns: coil lance and sparksabre require Mass Drivers. Does nothing without those mods."),
            }),

            new RebalanceGroup("vanilla", "Vanilla & DLC", new List<RebalanceToggle>
            {
                new RebalanceToggle("vanilla.healingenhancer", "Healing enhancer uses injury healing factor",
                    "The Royalty healing enhancer implant grants x1.5 injury healing factor instead of the hidden natural healing factor, so the bonus shows up in the pawn's stat window."),
                new RebalanceToggle("vanilla.mechraidgroups", "Combined mechanoid raid groups",
                    "Adds melee, light, heavy and all-star raid compositions to the Mechanoid faction mixing vanilla mechs with Alpha Mechs and Rimsenal Spacer mechs (each entry only applies when its mod is loaded), plus a bomb-rush group with Alpha Mechs and Rimsenal Spacer."),
                new RebalanceToggle("vanilla.toxicmeat", "Toxic meat unchecked by default",
                    "VAE - Waste Animals' toxic meat is disallowed by default in hopper storage and meal recipe ingredient filters."),
                new RebalanceToggle("vanilla.creepjoinersurgery", "Creep joiners accept human surgeries",
                    "Every surgery recipe that lists humans as a target (implants and prosthetics from any mod included) also accepts Anomaly's creep joiners, who use their own race def and are otherwise skipped by modded implants."),
            }, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("vanilla.genecomplexitybase", "Extra base xenogerm complexity",
                    "Genetic complexity added to the gene assembler's base limit of 6, before gene processors. Toggling this off keeps vanilla. Needs Biotech.",
                    10, 0, 25),
                new RebalanceSlider("vanilla.genecomplexityprocessor", "Complexity per gene processor",
                    "Genetic complexity each powered gene processor adds to the assembler's limit. Vanilla is 2; toggling this off keeps it. Needs Biotech.",
                    3, 1, 10),
            }),

            new RebalanceGroup("vqea", "Vanilla Quests Expanded - Ancients", new List<RebalanceToggle>
            {
                new RebalanceToggle("vqea.sittable", "Sittable ancient hospital seating",
                    "The ancient hospital armchair and bench become actual seats, using the comfort they already have."),
                new RebalanceToggle("vqea.giantweapons", "Enormous and Herculean wield giant weapons",
                    "Carriers of the enormous or herculean archite genes get Big and Small's Giant trait, letting them equip B&S giant weapons. Needs Big and Small - Genes & More; does nothing without it."),
                new RebalanceToggle("vqea.patientgown", "Patient gown blunt armor nerf",
                    "The ancient patient gown's blunt armor drops from 50% to 10%, so pawns stop preferring it over real armor."),
                new RebalanceToggle("vqea.injectorwhitelist", "Curated archogen injector gene pool",
                    "The archogen injector and ancient experiment pawns roll genes from a curated whitelist (VQE Ancients' archite powers plus mild drawbacks from vanilla, Alpha Genes, Big and Small, WVC, VRE and Det's xenotype packs) instead of every archite and negative gene from all loaded mods."),
            }),

            new RebalanceGroup("eltex", "Eltex Weaponry", new List<RebalanceToggle>
            {
                new RebalanceToggle("eltex.spawns", "Eltex weapons only on psycasters",
                    "Eltex weapons stop spawning on random enemies: they become psychic-tagged gear carried by Empire cataphracts, Empire psycasters (with Vanilla Psycasts Expanded) and deserters (with VFE - Empire). Needs Royalty."),
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
                new RebalanceToggle("genetics.agtools", "Craftable Alpha Genes gene toolkits",
                    "A new gene toolkits research project after gene processor lets you craft Alpha Genes' single-use gene tools at the fabrication bench; the archotech variants also cost an archite capsule. Trader and quest acquisition is unchanged, and the archotech xenogenefier's market value is fixed to match its siblings. Does nothing unless the tab toggle is on."),
                new RebalanceToggle("genetics.alphagenes", "Alpha Genes xenogenetics lab quest names",
                    "Renames Alpha Genes' abandoned biotech lab quest and site to xenogenetics lab flavour, as Progression: Genetics did. Works on its own."),
            }),

            new RebalanceGroup("alphagenes", "Alpha Genes", new List<RebalanceToggle>
            {
                new RebalanceToggle("alphagenes.genepacks", "Alpha Genes genes in vanilla genepacks",
                    "Alpha Genes' genes spawn in vanilla genepacks (cosmetic genes at much lower weight), the random genepack spawner in gene lab quests only yields vanilla genepacks, and alphapacks/mixedpacks become unobtainable (existing ones are untouched)."),
            }),

            new RebalanceGroup("alphamemes", "Alpha Memes", new List<RebalanceToggle>
            {
                new RebalanceToggle("alphamemes.vacstonetiles", "Vacstone styled tiles",
                    "Jewish, kemetic, steampunk, neolithic and ocular styled tiles can be built from Odyssey's vacstone blocks. Needs Odyssey; does nothing without it."),
            }),

            new RebalanceGroup("geneconflicts", "Gene Conflict Fixes", new List<RebalanceToggle>
            {
                new RebalanceToggle("geneconflicts.bloodlust", "Bloodlust and Distressed genes conflict",
                    "Big and Small's bloodlust gene and VRE - Highmate's distressed gene force traits that suppress each other and bug out when combined; they become mutually exclusive. Needs both mods; does nothing with only one."),
                new RebalanceToggle("geneconflicts.psychic", "Psychic UV/dark sensitivity vs psychically dull/deaf",
                    "WVC - Xenotypes and Genes' psychic UV sensitivity and psychic dark sensitivity genes become mutually exclusive with the vanilla psychically dull and psychically deaf genes, whose forced traits they would otherwise fight."),
                new RebalanceToggle("geneconflicts.firefoam", "Firefoam pop vs fire obsession",
                    "WVC - Xenotypes and Genes' firefoam pop gene suppresses the Pyromaniac trait that Alpha Genes' fire obsession gene forces; they become mutually exclusive. Needs both mods; does nothing with only one."),
                new RebalanceToggle("geneconflicts.hemogen", "No hemogen drain stacking",
                    "Big and Small's greater blood drain and WVC's hemogen gain join the mutual-exclusion tag VRE - Sanguophage already uses for its hemogen drain genes, and the vanilla hemogen drain gene gets that tag too."),
                new RebalanceToggle("geneconflicts.deathless", "Deathless-type genes are mutually exclusive",
                    "Vanilla deathless, Big and Small's revenant soul and immortal return, WVC's undead and never dead, and VRE - Archon's transcendent can no longer be combined on one pawn."),
                new RebalanceToggle("geneconflicts.dodge", "Melee dodge genes are mutually exclusive",
                    "Melee dodge genes from VQE - Ancients (prowess), Rimsenal Harana (agile fighter), Rimsenal Askbarn (lightning reflexes, born warrior), Det's Keshig (deft, lumbering) and Highborn Xenotype (fencer) share VRE - Lycanthrope's melee dodge exclusion tag, so dodge bonuses can't stack across mods."),
            }),

            new RebalanceGroup("odyssey", "Odyssey", new List<RebalanceToggle>
            {
                new RebalanceToggle("odyssey.shuttle", "Long-range passenger shuttle",
                    "Raises the passenger shuttle's chemfuel capacity from 400 to 2000 and its cargo mass capacity from 500 to 2000."),
                new RebalanceToggle("odyssey.vacuumtrims", "Vacuum resistance trims on modded armor",
                    "Trims the vacuum resistance of spacer armor from Rimsenal - Core, Rimsenal - Federation, Altered Carbon 2, Spacer Arsenal and Impact Weaponry - Reloaded (each only when loaded), keeping full vacuum protection hard to reach; the Spacer Arsenal ensign and Impact Weaponry crusader helmets also lose a little sharp armor. Needs Vanilla Gravship Expanded - Chapter 1, whose balance assumes scarce vacuum resistance; does nothing without it."),
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
