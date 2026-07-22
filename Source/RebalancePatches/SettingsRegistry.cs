using System.Collections.Generic;

namespace RebalancePatches
{
    public class RebalanceToggle
    {
        private static readonly string[] NoMods = new string[0];

        public readonly string key;
        public readonly string label;
        public readonly string description;
        public readonly bool defaultOn;
        public readonly string[] requiredMods;
        public readonly string[] anyOfMods;
        public readonly string dependsOn;

        public RebalanceToggle(string key, string label, string description, bool defaultOn = true,
            string[] requiredMods = null, string[] anyOfMods = null, string dependsOn = null)
        {
            this.key = key;
            this.label = label;
            this.description = description;
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
        public readonly string label;
        public readonly string description;
        public readonly int defaultValue;
        public readonly int min;
        public readonly int max;
        public readonly bool defaultOn;
        public readonly string[] requiredMods;

        public RebalanceSlider(string key, string label, string description, int defaultValue, int min, int max,
            bool defaultOn = true, string[] requiredMods = null)
        {
            this.key = key;
            this.label = label;
            this.description = description;
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
        public readonly string label;
        public readonly bool defaultOn;
        public readonly List<RebalanceToggle> children;
        public readonly List<RebalanceSlider> sliders;
        public readonly string[] requiredMods;
        public readonly bool isOverhaul;

        public RebalanceGroup(string key, string label, List<RebalanceToggle> children, bool defaultOn = true,
            List<RebalanceSlider> sliders = null, string[] requiredMods = null, bool isOverhaul = false)
        {
            this.key = key;
            this.label = label;
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
            new RebalanceGroup("genetics", "Genetics Overhaul", new List<RebalanceToggle>
            {
                new RebalanceToggle("genetics.agsummons", "Remove Alpha Genes summon genes",
                    "Removes Alpha Genes' animal summon genes (one per supported animal, ~90 with a large modlist, plus the summon randomizers and temporary bandwidth gene). No xenotype uses them; they only dilute the gene pool. Needs Cherry Picker, which does the removing.",
                    requiredMods: new[] { "owlchemist.cherrypicker", "sarg.alphagenes" }),
                new RebalanceToggle("genetics.wvcdupes", "Remove WVC-internal duplicate genes",
                    "Removes WVC - Xenotypes and Genes' genes that duplicate vanilla Biotech genes or WVC's own alternatives: psychically dull/deaf copies, extra pain, perfect immunity, non-senescent, natural ageless, never rest, the seven pattern aptitude genes, natural/super variants that have archite versions, unbreakable, invulnerable, implanter fangs, delicate and undead. WVC xenotypes that carried a removed gene get the surviving equivalent instead (vanilla or WVC's own kept version). Needs Cherry Picker, which does the removing.",
                    requiredMods: new[] { "owlchemist.cherrypicker", "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("genetics.dedup", "Remove cross-mod duplicate genes",
                    "One canonical gene per function: Alpha Genes keeps immunities, natural armor and bandwidth; Big and Small keeps no pain, body size, gender and healing-speed genes; specialist mods keep their specialty (VRE - Pigskin aging, VRE - Archon pregnancy, VRE - Saurid egg-laying, VRE - Waster cell instability, Det's Venators farsight and more). The losing duplicates from Alpha Genes, WVC, Big and Small, the VRE packs, Det's Xenotypes and Rimsenal xenotype packs are removed; entries whose canonical mod is missing are left alone, so this scales to whichever of those mods you run. Every xenotype that carried a removed gene is rewired to the canonical replacement, so races keep their function through the shared gene. Needs Cherry Picker, which does the removing.",
                    requiredMods: new[] { "owlchemist.cherrypicker" },
                    anyOfMods: new[] { "sarg.alphagenes", "wvc.sergkart.races.biotech", "redmattis.bigsmall.core",
                        "det.avaloi", "det.brawnum", "det.halffoot", "det.stoneborn",
                        "rimsenal.askbarn", "rimsenal.harana", "rimsenal.zohar",
                        "vanillaracesexpanded.archon", "vanillaracesexpanded.fungoid", "vanillaracesexpanded.genie",
                        "vanillaracesexpanded.highmate", "vanillaracesexpanded.hussar", "vanillaracesexpanded.phytokin",
                        "vanillaracesexpanded.pigskin", "vanillaracesexpanded.saurid" }),
                new RebalanceToggle("genetics.hussaraptitudes", "Consolidate VRE - Hussar weapon aptitudes",
                    "VRE - Hussar generates one aptitude gene per craftable weapon (~300 with a large modlist, one gene UI entry each). This replaces the whole family with four category genes — light/heavy melee and light/heavy ranged aptitude, split at 3 kg — with the same bonus and biostats. The hussar xenotypes' random weapon aptitude now rolls among the four, and with Gene Nodes - Genes for Sale loaded a new archite gene node delivers them. Existing pawns with an old per-weapon aptitude lose it (one-time load warning).",
                    requiredMods: new[] { "vanillaracesexpanded.hussar" }),
                new RebalanceToggle("genetics.bsdupes", "Remove Big and Small internal/legacy genes",
                    "Removes Big and Small - Genes & More genes its author treats as legacy or that duplicate its own alternatives: the three gene stabilizing genes (no replacement) and the deathlike body gene (undead xenotypes get Big and Small's unstable deathlessness instead). Needs Cherry Picker, which does the removing.",
                    requiredMods: new[] { "owlchemist.cherrypicker", "redmattis.bigsmall.core" }),
                new RebalanceToggle("genetics.boglegwater", "Boglegs: water striding",
                    "Boglegs gain Alpha Genes' water striding gene — no movement penalty in watery terrain, fitting swamp-dwellers.",
                    requiredMods: new[] { "det.boglegs", "sarg.alphagenes" }),
                new RebalanceToggle("genetics.stonebornskin", "Stoneborn: stoneskin",
                    "Det's Stoneborn gain WVC - Xenotypes and Genes' stoneskin gene — stone-covered bodies with natural armor and very low flammability, at a metabolism cost. Changes their appearance to stone-like skin.",
                    requiredMods: new[] { "det.stoneborn", "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("genetics.neanderthalfrost", "Neanderthals: frostbite resistance",
                    "Neanderthals gain Alpha Genes' frostbite resistance gene, halving frostbite damage — an ice-age adaptation.",
                    requiredMods: new[] { "sarg.alphagenes", "ludeon.rimworld.biotech" }),
                new RebalanceToggle("genetics.wvcspawns", "WVC: apex xenotypes never spawn as wanderers",
                    "WVC - Xenotypes and Genes' most powerful races — ferrkind, metalkin, rustkind and deadcat — no longer appear as random wanderers, refugees, beggars or faction pawns. They can still be obtained through WVC's own events, morphs and implanters, like the rest of its apex tier.",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
            }, defaultOn: false, isOverhaul: true),

            new RebalanceGroup("geneticsresearch", "Genetics Research Overhaul", new List<RebalanceToggle>
            {
                new RebalanceToggle("geneticsresearch.core", "Genetics research tab and tree (Biotech)",
                    "Adds a Genetics research tab rooted on a new basic genetic sampling project that unlocks the gene extractor and gene bank. Xenogermination becomes xenogerm assembly and moves there with gene processor and archogenetics, all at spacer tech with a hi-tech research bench required. Turn this on first; the rest of the group builds on it."),
                new RebalanceToggle("geneticsresearch.resplice", "ReSplice: Core buildings via dedicated research",
                    "ReSplice: Core's gene centrifuge and xenogerm duplicator move behind new genepack centrifuge and xenogerm replicator research projects on the Genetics tab and are renamed to match.",
                    requiredMods: new[] { "resplice.xotr.core" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.extractortiers", "Gene Extractor Tiers vats via dedicated research",
                    "Gene Extractor Tiers' gene extraction vat unlocks from a new research project after gene processor, and its archite vats from a new archite gene extraction project after archogenetics.",
                    requiredMods: new[] { "redmattis.geneextractor" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.genenodes", "Gene nodes via dedicated research, pricier archite nodes",
                    "Gene Extractor Tiers' base gene nodes unlock from a new gene nodes project after xenogerm assembly. Archite gene nodes, including Gene Nodes - Genes for Sale's, move behind a new archite gene nodes project after archogenetics and cost more to build.",
                    requiredMods: new[] { "redmattis.geneextractor" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.generipper", "Gene Ripper via dedicated research",
                    "Gene Ripper's machine unlocks from a new gene ripper research project after xenogerm assembly.",
                    anyOfMods: new[] { "defi.generipper", "danielwedemeyer.generipper" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.genefab", "Gene Fabrication as an archogenetics capstone",
                    "Gene Fabrication's research project moves to the Genetics tab as an ultratech capstone and requires archogenetics instead of gene processor plus fabrication. It is also retitled to lowercase gene fabrication to match the rest of the tab, and its description now describes the fabrication bench it unlocks rather than multianalyzers. Its per-gene genepack recipes also stop listing archogenetics as their own prerequisite, since the capstone already gates the fabricator: with a large gene modlist that is hundreds of recipes cluttering the archogenetics entry in the research tree, and nothing becomes available any earlier.",
                    requiredMods: new[] { "amch.eragon.hcgenefabrication" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.vqea", "Buildable VQE Ancients archogen laboratory",
                    "A new ultratech archogen engineering research project after archogenetics lets you build VQE Ancients' archogen injector and its linkable lab facilities at archite-tier costs. Recovering them from ancient labs remains the early route.",
                    requiredMods: new[] { "vanillaquestsexpanded.ancients" }, dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.consumables", "Unified gene tools and serums line",
                    "One research lane for every single-use genetic item across Alpha Genes, Big and Small - Genes & More and WVC - Xenotypes and Genes. Gene serums (after xenogerm assembly) takes over WVC's whole eight-project serum line: the serum lab, gene restoration, serum disassembly and the per-gene serums. Gene toolkits (after gene processor) unlocks Alpha Genes' gene tools and Big and Small's gene tools, xenogerm cloners and animal size serums at the fabrication bench, with the archotech variants additionally requiring archogenetics. Gene integration (ultratech, after archogenetics) unlocks Big and Small's gene integrator. Big and Small's experimental gene tools and animal size serums projects are removed in favour of the lane, and its mad science field testing becomes weaponized genetics: ultratech, after archogenetics, on the Genetics tab instead of industrial tier for 500 points. Three redundant tools are removed: Big and Small's xenodiscombobulator (Alpha Genes' xenotype injector does the same job), its archite xenogerm cloner (its archite genome cloner supersedes it) and Alpha Genes' germline mutator.",
                    anyOfMods: new[] { "sarg.alphagenes", "redmattis.bigsmall.core", "wvc.sergkart.races.biotech" },
                    dependsOn: "geneticsresearch.core"),
                new RebalanceToggle("geneticsresearch.alphagenes", "Alpha Genes xenogenetics lab quest names",
                    "Renames Alpha Genes' abandoned biotech lab quest and site to xenogenetics lab flavour, as Progression: Genetics did. Works on its own.",
                    requiredMods: new[] { "sarg.alphagenes" }),
            }, defaultOn: false, requiredMods: new[] { "ludeon.rimworld.biotech" }, isOverhaul: true),

            
            new RebalanceGroup("xenotypes", "Xenotype Spawning Overhaul", new List<RebalanceToggle>
            {
                new RebalanceToggle("xenotypes.vanilla", "Thematic xenotypes in vanilla factions",
                    "Outlander, pirate and tribal rosters are rebuilt around what each faction is. Settled outlanders lean industrial (Det's Half-foot, Biotech genies, Det's Brawnum), rough outlanders frontier (Det's Venators and Boglegs, yttakin, impids), and pirates predatory (Boglegs, Det's Buzzers, wasters, VRE - Hussar's uhlans). Every roster keeps at least 35% baseliners, where the modlist had crowded them down to under 10%. Tribal factions, which rolled baseliner every single time, gain a small primitive-themed roster (neanderthals, VRE - Saurid's saurids, Det's Venators, impids). Also thins WVC's oddball xenotypes out of generic pools, moving Undead and Sandycat to the Horax cult (Anomaly)."),
                new RebalanceToggle("xenotypes.royalty", "Thematic xenotypes in the Empire",
                    "The Empire leans aristocratic and military: Det's Avaloi, Biotech hussars and genies and Highborn Xenotype's highborn stay prominent, while Det's Keshig and Brawnum, Rimsenal's Harana, Odyssey's starjack and VRE - Android's awakened androids leave the roster. Keeps 37% baseliners.",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
                new RebalanceToggle("xenotypes.odyssey", "Thematic xenotypes for Odyssey factions",
                    "The Salvagers gain Det's Half-foot and hold a spread of Rimsenal, Det's and Alpha Genes xenotypes at even weight; the Traders guild gains Alpha Genes' Fleetkind.",
                    requiredMods: new[] { "ludeon.rimworld.odyssey" }),
                new RebalanceToggle("xenotypes.rimsenal", "Thematic xenotypes for Rimsenal factions",
                    "Rimsenal's Spacer factions gain the deep-space xenotypes displaced from planetside pools: Odyssey's starjack, Det's Keshig and Half-foot and VRE - Android's awakened androids. WVC's Mechakin, Rogueformer and Genethrower also move here from generic outlander and pirate pools.",
                    requiredMods: new[] { "rimsenal.spacer" }),
            }, requiredMods: new[] { "ludeon.rimworld.biotech" }, isOverhaul: true),

            new RebalanceGroup("scifinames", "Sci-fi Renaming Overhaul", new List<RebalanceToggle>
            {
                new RebalanceToggle("scifinames.bsraces", "Big and Small - Races",
                    "Renames the Norse-flavoured content to gene-line flavour: jotun become gigants (cryo/pyro variants), ogres hulkers, dvergr deepkin, nisse minikin, svartalfs umbrakin, redcaps scrappers, trolls regenerants, flesh golems bioconstructs and hearthguards/hearthdolls warden/service synths. The Muspelheim, Niflheim, ogre and little people factions, their pawn kinds and all descriptions follow suit.",
                    requiredMods: new[] { "redmattis.bigsmall" }),
                new RebalanceToggle("scifinames.bigsmall", "Big and Small - Genes & More",
                    "The succubus becomes the allurist, the hellguard the abyssal guard, the imp the greater impid, the returned reanimates, and the frost jotun adventurer a cryogigant.",
                    requiredMods: new[] { "redmattis.bigsmall.core" }),
                new RebalanceToggle("scifinames.heaven", "Big and Small - Heaven and Hell",
                    "Strips the religious mythos: angels become ascendants (Satan the adversary prime, Grigori watchers, Nephilim the halfwrought, Lilim the nightwrought), demons become abyssals (gluttons devourers), and the Heaven, Hell and Outcast factions become the Luminal Ascendancy, the Abyssal Dominion and the Exiles.",
                    requiredMods: new[] { "redmattis.heaven" }),
                new RebalanceToggle("scifinames.yokai", "Big and Small - Yokai",
                    "Kitsune become vulpids, nekomata felids and oni hornbrutes (crimson/cobalt); the Yokai Union becomes the Chimeric Union.",
                    requiredMods: new[] { "redmattis.yokai" }),
                new RebalanceToggle("scifinames.lamias", "Big and Small - Lamias",
                    "Lamia become serpids, sirens mesmer serpids, gorgons petrifex serpids, naga greater serpids and nagaraj serpid primes; Greek and Hindu myth references drop from descriptions and the snake tribal federation becomes the serpid tribal federation.",
                    requiredMods: new[] { "redmattis.lamiasandothersnakes" }),
                new RebalanceToggle("scifinames.slimes", "Big and Small - Slimes",
                    "Slimes become plasmoids across all xenotypes and the escaped slimes faction.",
                    requiredMods: new[] { "redmattis.bsslimes" }),
                new RebalanceToggle("scifinames.morexenos", "Big and Small - More Xenotypes",
                    "The devilspider becomes the dreadspider.",
                    requiredMods: new[] { "redmattis.morexenos" }),
                new RebalanceToggle("scifinames.wvc", "WVC - Xenotypes and Genes",
                    "The undead xenotype becomes the necrokin and the lilif the psykin.",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("scifinames.alphagenes", "Alpha Genes",
                    "Efreet become cindrids and nereids abyssids.",
                    requiredMods: new[] { "sarg.alphagenes" }),
            }, defaultOn: false, isOverhaul: true),

            new RebalanceGroup("vse", "Expertise Overhaul", new List<RebalanceToggle>
            {
                new RebalanceToggle("vse.expertiseconsolidation", "Consolidate and retune expertises",
                    "Replaces the long list of narrow expertises with 32 broader ones, two to four per skill, so every pick is a real choice. Bonuses become multipliers capping near +40% at expertise level 20 instead of flat offsets that reach +100% or overshoot a stat's ceiling; combat and stat-heavy expertises are tuned lower, quality expertises cost work speed, and the two psycast expertises carry real drawbacks. Adds mechanitor and psycast expertises, which the base mods lack. Folds in the expertises from Alpha Skills, Hauts' Framework, Vanilla Fishing Expanded and Vanilla Gravship Expanded, and picks up stats from Integrated Implants, Mechanoid Upgrades, Altered Carbon and Vanilla Psycasts Expanded when those are present. Off by default; existing pawns keep the expertise they already have."),
            }, defaultOn: false, requiredMods: new[] { "vanillaexpanded.skills" }, isOverhaul: true),

            new RebalanceGroup("rimiot", "RimIOT - Logistic Matrix", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimiot.costs", "Reduce build costs",
                    "Cheaper cable, connector and interface, with advanced components replaced by basic ones."),
                new RebalanceToggle("rimiot.power", "Remove power consumption",
                    "Network buildings draw no power and need no wiring, and their descriptions drop the power notes."),
            }, requiredMods: new[] { "cn.rimiot" }),

            new RebalanceGroup("altered", "Altered Carbon", new List<RebalanceToggle>
            {
                new RebalanceToggle("altered.shieldbelt", "Disable the ranged shield belt from VAE Accessories",
                    "Makes Vanilla Apparel Expanded - Accessories' ranged shield belt unobtainable in favour of Altered Carbon's cuirassier belt. The def is kept, so saves are unaffected.",
                    requiredMods: new[] { "vanillaexpanded.vaeaccessories" }),
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
            }, requiredMods: new[] { "hlx.ultratechalteredcarbon" }),

            new RebalanceGroup("bigsmall", "Big and Small - Genes & More", new List<RebalanceToggle>
            {
                new RebalanceToggle("bigsmall.madscience", "Mad science field testing requires gun turrets",
                    "The mad science field testing research itself gets the vanilla Gun turrets prerequisite, instead of every turret and ray weapon it unlocks listing Gun turrets separately, which garbles their requirement display."),
            }, requiredMods: new[] { "redmattis.bigsmall.core" }),

            new RebalanceGroup("vfepirates", "Vanilla Factions Expanded - Pirates", new List<RebalanceToggle>
            {
                new RebalanceToggle("vfepirates.chargeweapons", "Clean warcasket charge weapon prerequisites",
                    "Warcasket charge blaster and lance require pulse-charged munitions plus spacer warcasket weaponry, dropping the inherited redundant prerequisite; with VWE - Coilguns, the warcasket railgun gets the same treatment via Mass Drivers. With Warcasket Weapon Quality, its direct-craft recipes get the same research gates the weapon boxes had."),
                new RebalanceToggle("vfepirates.empirescenario", "Empire not permanently hostile to pirates",
                    "The Empire is no longer permanently hostile to the player pirate faction from the Low orbit crash scenario, so reputation can be repaired.",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
            }, requiredMods: new[] { "oskarpotocki.vfe.pirates" }),

            new RebalanceGroup("vfeempire", "Vanilla Factions Expanded - Empire", new List<RebalanceToggle>
            {
                new RebalanceToggle("vfeempire.qol", "Royal armchair throne + candelabra glow radius",
                    "Vanilla Furniture Expanded's royal armchair satisfies the Stellarch and High Stellarch throne room seat requirement, and the candelabra shows its light radius when placing."),
            }, requiredMods: new[] { "oskarpotocki.vfe.empire" }),

            new RebalanceGroup("rimsenal", "Rimsenal - Core", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimsenal.corpcost", "Corp techs cost 3000",
                    "The three corp tier-1 researches (crystalline, furnace, kinetic) drop from 3050 to 3000 base cost."),
                new RebalanceToggle("rimsenal.armortechs", "Armors require their corp's tech",
                    "Rimsenal armors require their own corporation's research (Greydale modular gear via modular equipment manufacturing, Yeonhwa suits via crystalline techs, Jotun heavy armors via furnace/siege techs, Tesseron carapaces via kinetic techs) instead of vanilla flak/recon/powered armor. Also repairs the artillery armor's malformed siege gun prerequisite."),
                new RebalanceToggle("rimsenal.modularweapons", "Modular weapons via modular equipment manufacturing",
                    "The modular carbine and MRS conversion kit require modular equipment manufacturing instead of vanilla gun researches, and the GD multi launcher additionally requires it alongside Mortars."),
            }, requiredMods: new[] { "rimsenal.core" }),

            new RebalanceGroup("rimsenalspacer", "Rimsenal - Spacer Faction Pack", new List<RebalanceToggle>
            {
                new RebalanceToggle("rimsenalspacer.caravanmechs", "Caravans without mechanoid guards",
                    "Spacer faction trade caravans no longer bring mechanoids as guards; the mechs frequently get left behind and roamed the colony map forever."),
                new RebalanceToggle("rimsenalspacer.smartweapons", "Clean smart weapon research prerequisites",
                    "Smart charge weapons and the smart minigun drop their redundant inherited gunsmithing prerequisite; with Royalty, the smart visor unlocks from the Gunlink research, which is renamed to smart targeting systems."),
            }, requiredMods: new[] { "rimsenal.spacer" }),

            new RebalanceGroup("memes", "Meme & Ideology Fixes", new List<RebalanceToggle>
            {
                new RebalanceToggle("memes.factions", "No pacifist memes for warlike factions",
                    "Rimsenal's Spacer and Federation factions can't generate with Alpha Memes' vow of nonviolence meme, which breaks their combat pawn generation. Alpha Memes only adds that meme alongside VIE - Memes and Structures, so both are needed.",
                    requiredMods: new[] { "vanillaexpanded.vmemese", "sarg.alphamemes" },
                    anyOfMods: new[] { "rimsenal.spacer", "rimsenal.federation" }),
                new RebalanceToggle("memes.anomalytraits", "Occultist and void fascination fit inhuman memes",
                    "The Occultist trait and void fascination agree with Anomaly's Inhuman and Ritualist memes, so pawns who have them no longer resent an ideology built around them. Needs Ideology, since memes do nothing without it.",
                    requiredMods: new[] { "ludeon.rimworld.anomaly", "ludeon.rimworld.ideology" }),
                new RebalanceToggle("memes.inspirations", "Inspirations respect ideology precepts",
                    "Pawns whose ideology abhors an activity stop rolling inspirations for it: shooting/melee frenzies blocked by VIE - Memes and Structures and Alpha Memes violence precepts, taming inspiration by their ranching/bonding precepts, and Vanilla Social Interactions Expanded's frenzies by the matching precepts.",
                    requiredMods: new[] { "ludeon.rimworld.ideology" }),
            }),

            new RebalanceGroup("implants", "Integrated Implants", new List<RebalanceToggle>
            {
                new RebalanceToggle("implants.chipbad", "Skill chips survive cleansing effects",
                    "Marks Integrated Implants' skill chip hediffs isBad=false so healer serums, biosculpting and similar purges don't rip them out."),
                new RebalanceToggle("implants.chiptiers", "Mechanitor implants need Alpha Mechs chips",
                    "Mechhive satellite uplink, mechwomb, warprogrammer interface and remote dominator cost Alpha Mechs' tier 4/5/6/6 chips instead of vanilla tier 2/2/3/3 chips.",
                    requiredMods: new[] { "sarg.alphamechs" }),
                new RebalanceToggle("implants.voicelockmasochist", "Masochists enjoy the voicelock",
                    "Pawns with the Masochist trait get +8 mood instead of -8 while their voicelock is active."),
                new RebalanceToggle("implants.shoulderslimes", "Shoulder turrets install on the shoulder",
                    "Shoulder turret and shoulder charge turret surgeries target the shoulder instead of the torso, which slime bodies lack (avoids errors).",
                    requiredMods: new[] { "redmattis.bsslimes" }),
                new RebalanceToggle("implants.waterpathing", "Levitating implants ignore water",
                    "Pawns with the psychic levitator or personal grav engine implant move over water at no extra pathing cost."),
                new RebalanceToggle("implants.boosterrange", "Signal boosters stack with Alpha Genes command range genes",
                    "Alpha Genes' increased/decreased command range genes override the mech command radius outright, discarding signal booster implants; with this fix the booster's bonus is added on top of the gene's 35/15 tile radius.",
                    requiredMods: new[] { "sarg.alphagenes" }),
            }, requiredMods: new[] { "lts.i" }),

            new RebalanceGroup("vreinsector", "Vanilla Races Expanded - Insector", new List<RebalanceToggle>
            {
                new RebalanceToggle("vreinsector.colossalweapons", "Colossal insectors wield giant weapons",
                    "Carriers of the colossal geneline gene get Big and Small's Giant trait, letting them equip B&S giant weapons.",
                    requiredMods: new[] { "oskarpotocki.vfe.insectoid2", "redmattis.bigsmall.core" }),
            }, requiredMods: new[] { "vanillaracesexpanded.insector" }),


            new RebalanceGroup("impactweaponry", "Impact Weaponry - Reloaded", new List<RebalanceToggle>
            {
                new RebalanceToggle("impactweaponry.bolterprereq", "Clean warcasket impact bolter prerequisites",
                    "The warcasket impact bolter requires spacer warcasket weaponry (VFE - Pirates) plus impact shot, dropping the redundant extra prerequisite.",
                    requiredMods: new[] { "oskarpotocki.vfe.pirates" }),
            }, requiredMods: new[] { "detvisor.impactweaponryreloaded" }),

            new RebalanceGroup("spacerarsenal", "Spacer Arsenal", new List<RebalanceToggle>
            {
                new RebalanceToggle("spacerarsenal.prereqs", "Heavy weapons via VWE researches",
                    "With Vanilla Weapons Expanded: brute rifle, clash HMG/rifle and contact/thump grenades require Heavy Weapons plus Fabrication. With VWE - Coilguns: coil lance and sparksabre require Mass Drivers.",
                    anyOfMods: new[] { "vanillaexpanded.vwe", "vanillaexpanded.vwec" }),
            }, requiredMods: new[] { "det.spacerarsenal" }),

            new RebalanceGroup("vanilla", "Vanilla & DLC", new List<RebalanceToggle>
            {
                new RebalanceToggle("vanilla.healingenhancer", "Healing enhancer uses injury healing factor",
                    "The Royalty healing enhancer implant grants x1.5 injury healing factor instead of the hidden natural healing factor, so the bonus shows up in the pawn's stat window.",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
                new RebalanceToggle("vanilla.mechraidgroups", "Combined mechanoid raid groups",
                    "Adds melee, light, heavy and all-star raid compositions to the Mechanoid faction mixing vanilla mechs with Alpha Mechs and Rimsenal Spacer mechs (each entry only applies when its mod is loaded), plus a bomb-rush group with Alpha Mechs and Rimsenal Spacer.",
                    requiredMods: new[] { "ludeon.rimworld.biotech" }),
                new RebalanceToggle("vanilla.toxicmeat", "Toxic meat unchecked by default",
                    "VAE - Waste Animals' toxic meat is disallowed by default in hopper storage and meal recipe ingredient filters.",
                    requiredMods: new[] { "vanillaexpanded.vaewaste" }),
                new RebalanceToggle("vanilla.creepjoinersurgery", "Creep joiners accept human surgeries",
                    "Every surgery recipe that lists humans as a target (implants and prosthetics from any mod included) also accepts Anomaly's creep joiners, who use their own race def and are otherwise skipped by modded implants.",
                    requiredMods: new[] { "ludeon.rimworld.anomaly" }),
            }, sliders: new List<RebalanceSlider>
            {
                new RebalanceSlider("vanilla.genecomplexitybase", "Extra base xenogerm complexity",
                    "Genetic complexity added to the gene assembler's base limit of 6, before gene processors. Toggling this off keeps vanilla.",
                    10, 0, 25, requiredMods: new[] { "ludeon.rimworld.biotech" }),
                new RebalanceSlider("vanilla.genecomplexityprocessor", "Complexity per gene processor",
                    "Genetic complexity each powered gene processor adds to the assembler's limit. Vanilla is 2; toggling this off keeps it.",
                    3, 1, 10, requiredMods: new[] { "ludeon.rimworld.biotech" }),
            }),

            new RebalanceGroup("vqea", "Vanilla Quests Expanded - Ancients", new List<RebalanceToggle>
            {
                new RebalanceToggle("vqea.sittable", "Sittable ancient hospital seating",
                    "The ancient hospital armchair and bench become actual seats, using the comfort they already have."),
                new RebalanceToggle("vqea.giantweapons", "Enormous and Herculean wield giant weapons",
                    "Carriers of the enormous or herculean archite genes get Big and Small's Giant trait, letting them equip B&S giant weapons.",
                    requiredMods: new[] { "redmattis.bigsmall.core" }),
                new RebalanceToggle("vqea.patientgown", "Patient gown blunt armor nerf",
                    "The ancient patient gown's blunt armor drops from 50% to 10%, so pawns stop preferring it over real armor."),
                new RebalanceToggle("vqea.injectorwhitelist", "Curated archogen injector gene pool",
                    "The archogen injector and ancient experiment pawns roll genes from a curated whitelist (VQE Ancients' archite powers plus mild drawbacks from vanilla, Alpha Genes, Big and Small, WVC, VRE and Det's xenotype packs) instead of every archite and negative gene from all loaded mods."),
                new RebalanceToggle("vqea.nofabricatedarchite", "Ancient archite genes cannot be fabricated",
                    "Gene Fabrication builds a genepack recipe for every gene in the game, including VQE Ancients' 33 archite powers, which VQE Ancients itself keeps out of random genepacks. Removes those recipes, so herculean, genius, matter phasing and the rest come from the archogen injector, ancient labs and quests rather than a bench. Needs Gene Fabrication and Cherry Picker; does nothing without them.",
                    requiredMods: new[] { "owlchemist.cherrypicker", "amch.eragon.hcgenefabrication" }),
            }, requiredMods: new[] { "vanillaquestsexpanded.ancients" }),

            new RebalanceGroup("eltex", "Eltex Weaponry", new List<RebalanceToggle>
            {
                new RebalanceToggle("eltex.spawns", "Eltex weapons only on psycasters",
                    "Eltex weapons stop spawning on random enemies: they become psychic-tagged gear carried by Empire cataphracts, Empire psycasters (with Vanilla Psycasts Expanded) and deserters (with VFE - Empire).",
                    requiredMods: new[] { "ludeon.rimworld.royalty" }),
            }, requiredMods: new[] { "zal.eltexweaponry" }),

            new RebalanceGroup("gits", "GiTS Cyberbrains", new List<RebalanceToggle>
            {
                new RebalanceToggle("gits.merchant", "Only basic cyberbrains sold by merchants",
                    "Removes the higher-tier cyberbrains from trader stock. They stay craftable and can still spawn on raiders."),
                new RebalanceToggle("gits.surgeries", "Surgeries via EPOE brain surgery, later research to ultratech",
                    "Cyberbrain install and nullify surgeries unlock at EPOE-Forked's Brain Surgery research instead of the GiTS node, and post-basic cyberization research moves to the ultratech tier.",
                    requiredMods: new[] { "vat.epoeforked" }),
                new RebalanceToggle("gits.mentalbreak", "Harsher extreme mental break",
                    "Raises the extreme cyberbrain mental break threshold offset from +20% to +40%."),
                new RebalanceToggle("gits.research", "Streamline the research tree",
                    "Collapses the nanite surgery researches into nanite grafting and removes the empty filler nodes."),
            }, requiredMods: new[] { "moistestwhale.gitscyberbrains" }),


            new RebalanceGroup("alphagenes", "Alpha Genes", new List<RebalanceToggle>
            {
                new RebalanceToggle("alphagenes.genepacks", "Alpha Genes genes in vanilla genepacks",
                    "Alpha Genes' genes spawn in vanilla genepacks (cosmetic genes at much lower weight), the random genepack spawner in gene lab quests only yields vanilla genepacks, and alphapacks/mixedpacks become unobtainable (existing ones are untouched)."),
                new RebalanceToggle("alphagenes.beautyrename", "Rename angelic beauty to uncanny beauty",
                    "Alpha Genes' angelic beauty gene is relabeled uncanny beauty so it can't be confused with WVC's angel beauty gene. Label only.",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
            }, requiredMods: new[] { "sarg.alphagenes" }),

            new RebalanceGroup("alphamemes", "Alpha Memes", new List<RebalanceToggle>
            {
                new RebalanceToggle("alphamemes.vacstonetiles", "Vacstone styled tiles",
                    "Jewish, kemetic, steampunk, neolithic and ocular styled tiles can be built from Odyssey's vacstone blocks.",
                    requiredMods: new[] { "ludeon.rimworld.odyssey" }),
            }, requiredMods: new[] { "sarg.alphamemes" }),

            new RebalanceGroup("geneconflicts", "Gene Conflict Fixes", new List<RebalanceToggle>
            {
                new RebalanceToggle("geneconflicts.bloodlust", "Bloodlust and Distressed genes conflict",
                    "Big and Small's bloodlust gene and VRE - Highmate's distressed gene force traits that suppress each other and bug out when combined; they become mutually exclusive.",
                    requiredMods: new[] { "redmattis.bigsmall.core", "vanillaracesexpanded.highmate" }),
                new RebalanceToggle("geneconflicts.psychic", "Psychic UV/dark sensitivity vs psychically dull/deaf",
                    "WVC - Xenotypes and Genes' psychic UV sensitivity and psychic dark sensitivity genes become mutually exclusive with the vanilla psychically dull and psychically deaf genes, whose forced traits they would otherwise fight.",
                    requiredMods: new[] { "wvc.sergkart.races.biotech" }),
                new RebalanceToggle("geneconflicts.firefoam", "Firefoam pop vs fire obsession",
                    "WVC - Xenotypes and Genes' firefoam pop gene suppresses the Pyromaniac trait that Alpha Genes' fire obsession gene forces; they become mutually exclusive.",
                    requiredMods: new[] { "wvc.sergkart.races.biotech", "sarg.alphagenes" }),
                new RebalanceToggle("geneconflicts.hemogen", "No hemogen drain stacking",
                    "Big and Small's greater blood drain and WVC's hemogen gain join the mutual-exclusion tag VRE - Sanguophage already uses for its hemogen drain genes, and the vanilla hemogen drain gene gets that tag too."),
                new RebalanceToggle("geneconflicts.deathless", "Deathless-type genes are mutually exclusive",
                    "Vanilla deathless, Big and Small's revenant soul and immortal return, WVC's undead and never dead, and VRE - Archon's transcendent can no longer be combined on one pawn."),
                new RebalanceToggle("geneconflicts.dodge", "Melee dodge genes are mutually exclusive",
                    "Melee dodge genes from VQE - Ancients (prowess), Rimsenal Harana (agile fighter), Rimsenal Askbarn (lightning reflexes, born warrior), Det's Keshig (deft, lumbering) and Highborn Xenotype (fencer) share VRE - Lycanthrope's melee dodge exclusion tag, so dodge bonuses can't stack across mods.",
                    anyOfMods: new[] { "vanillaquestsexpanded.ancients", "rimsenal.harana", "rimsenal.askbarn", "det.keshig", "elsov.highborn" }),
                new RebalanceToggle("geneconflicts.claws", "Claw genes are mutually exclusive",
                    "Innate claw and talon attack genes can't stack: Alpha Genes' clawed hands, crab claw and pneumatic claw, VRE - Saurid's saurid claws, VRE - Sanguophage's talons, WVC's kitty and archite claws, Big and Small's venom talons, VRE - Insector's charger claws and VQE - Ancients' plasteel claws share a mutual-exclusion tag.",
                    anyOfMods: new[] { "sarg.alphagenes", "vanillaracesexpanded.saurid", "vanillaracesexpanded.sanguophage", "wvc.sergkart.races.biotech", "redmattis.bigsmall.core", "vanillaracesexpanded.insector", "vanillaquestsexpanded.ancients" }),
                new RebalanceToggle("geneconflicts.bleedrate", "Slow bleeding vs hemophiliac",
                    "Big and Small's slow bleeding gene and VRE - Genie's hemophiliac gene pull bleed rate in opposite directions; they become mutually exclusive.",
                    requiredMods: new[] { "redmattis.bigsmall.core", "vanillaracesexpanded.genie" }),
                new RebalanceToggle("geneconflicts.flirty", "Flirty vs never flirts",
                    "VRE - Highmate's flirty gene and Big and Small's never flirts gene contradict each other; they become mutually exclusive.",
                    requiredMods: new[] { "vanillaracesexpanded.highmate", "redmattis.bigsmall.core" }),
                new RebalanceToggle("geneconflicts.meleespeed", "No melee attack speed stacking",
                    "Det's Brawnum's slow hitter joins the mutual-exclusion tag VRE - Archon uses for its fast and slow melee hitter genes, so melee speed genes can't stack across the two mods.",
                    requiredMods: new[] { "det.brawnum", "vanillaracesexpanded.archon" }),
            }, requiredMods: new[] { "ludeon.rimworld.biotech" }),



            new RebalanceGroup("odyssey", "Odyssey", new List<RebalanceToggle>
            {
                new RebalanceToggle("odyssey.shuttle", "Long-range passenger shuttle",
                    "Raises the passenger shuttle's chemfuel capacity from 400 to 2000 and its cargo mass capacity from 500 to 2000."),
                new RebalanceToggle("odyssey.vacuumtrims", "Vacuum resistance trims on modded armor",
                    "Trims the vacuum resistance of spacer armor from Rimsenal - Core, Rimsenal - Federation, Altered Carbon 2, Spacer Arsenal and Impact Weaponry - Reloaded (each only when loaded), keeping full vacuum protection hard to reach; the Spacer Arsenal ensign and Impact Weaponry crusader helmets also lose a little sharp armor. Vanilla Gravship Expanded - Chapter 1's balance assumes scarce vacuum resistance.",
                    requiredMods: new[] { "vanillaexpanded.gravship" },
                    anyOfMods: new[] { "rimsenal.core", "rimsenal.federation", "hlx.ultratechalteredcarbon", "det.spacerarsenal", "detvisor.impactweaponryreloaded" }),
            }, requiredMods: new[] { "ludeon.rimworld.odyssey" }),

            new RebalanceGroup("dev", "Developer", new List<RebalanceToggle>
            {
                new RebalanceToggle("dev.genedump", "Auto-refresh gene database dump",
                    "With dev mode on, rewrites GeneDump.json at the main menu so the dump always reflects the current modlist.",
                    defaultOn: false),
                new RebalanceToggle("dev.xenofactiondump", "Auto-refresh xenotype faction dump",
                    "With dev mode on, rewrites XenotypeFactionDump.json at the main menu: which xenotypes each faction can roll, which ones no faction can roll, and each faction's modded-xenotype share.",
                    defaultOn: false),
                new RebalanceToggle("dev.recipedump", "Auto-refresh recipe dump",
                    "With dev mode on, rewrites RecipeDump.json at the main menu: every recipe and surgery, with its worker class chain, benches and resolved ingredients.",
                    defaultOn: false),
                new RebalanceToggle("dev.hediffdump", "Auto-refresh hediff dump",
                    "With dev mode on, rewrites HediffDump.json at the main menu: every hediff, which ones are added body parts, and the modular slot table with its capacities.",
                    defaultOn: false),
                new RebalanceToggle("dev.researchdump", "Auto-refresh research dump",
                    "With dev mode on, rewrites ResearchDump.json at the main menu: every project and tab, each project's techprint item, and any project whose techprint went missing.",
                    defaultOn: false),
                new RebalanceToggle("dev.thingdump", "Auto-refresh thing dump",
                    "With dev mode on, rewrites ThingDump.json at the main menu: every item and building with its resolved market value, work, mass and hit points, and the recipes that produce it.",
                    defaultOn: false),
                new RebalanceToggle("dev.bodydump", "Auto-refresh body dump",
                    "With dev mode on, rewrites BodyDump.json at the main menu: every body's resolved part tree and every body part def. Finds surgeries that no loaded body can receive.",
                    defaultOn: false),
                new RebalanceToggle("dev.acquisitiondump", "Auto-refresh acquisition dump",
                    "With dev mode on, rewrites AcquisitionDump.json at the main menu: which traders stock each item, and every reward, quest, scenario and pawn gear table that hands it out.",
                    defaultOn: false),
                new RebalanceToggle("dev.modrulesdump", "Auto-refresh mod rules dump",
                    "With dev mode on, rewrites ModRulesDump.json at the main menu: rule defs from other mods that gate which surgeries and hediffs a pawn is allowed to receive.",
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
