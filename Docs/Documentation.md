# Rebalance Patches — Feature Documentation

What every toggle in the mod settings does, grouped the same way as the settings window. Every feature can be turned off individually (restart required), and every feature silently does nothing if the mods it targets aren't loaded.

The settings window groups features by mod: groups collapse and expand, a search box filters by name and description, and settings whose required mods aren't in the modlist are greyed out with a note saying what's missing (hover for the full requirement list). Groups for mods you don't run can be hidden entirely with *Show inactive mods*.

**The five overhauls sit at the top of the settings window, above a divider.** They are sweeping changes to how whole systems work — Genetics Overhaul, Genetics Research Overhaul, Xenotype Spawning Overhaul, Sci-fi Renaming Overhaul and Expertise Overhaul — and nothing below the divider depends on them. Four of them are **off by default**, so nothing changes until you opt in; the exception is the Xenotype Spawning Overhaul, which is on, since it only affects which xenotypes newly generated pawns roll rather than reworking a system you build against. Turning an overhaul's group on enables everything inside it; individual pieces can then be turned back off. The patches below the divider stay on by default, since each is a small self-contained fix.

If you are **upgrading** from a version where an overhaul was on by default, it stays on: the mod records a config version and pins settings whose default has changed, so an ongoing colony never loses a system it was already running. Settings you chose yourself are always left alone. Only fresh installs start with the overhauls off.

---

## Patches

### RimIOT - Logistic Matrix

- **Cheaper builds** (`rimiot.costs`) — Cables, input connectors and interfaces cost a little steel and regular components instead of advanced ones. Passive logistics infrastructure leadin to performance gain on hauling shouldn't be an endgame investment.
- **No power consumption** (`rimiot.power`) — Network buildings no longer draw power or need wiring, and their descriptions are rewritten to match.

### Altered Carbon

- **Disable VAE ranged shield belt** (`altered.shieldbelt`) — Vanilla Apparel Expanded - Accessories' ranged shield belt is a cheaper duplicate of Altered Carbon's cuirassier belt, so it becomes uncraftable, untradeable and stops spawning; the harder-to-get AC version stays. Existing belts keep working.
- **Casting relay range slider** (`altered.relayrange`, slider 1–25, default 10) — Choose how many world tiles of needlecasting range each powered casting relay adds; I find Altered Carbon's fixed 5 is too short. Toggle off to keep the original behaviour.
- **Advanced shields need Fabrication** (`altered.shieldsfab`) — The advanced shield belt research additionally requires Fabrication; its gear is fabrication-bench only anyway.
- **Cuirassier belt on vanilla shields** (`altered.cuirassier`) — The cuirassier belt uses the vanilla shield mechanic instead of the shield bubble: it scales with quality and doesn't block your own shots.
- **Neural editor trait blacklist** (`altered.traitblacklist`) — Body-bound traits from Hauts' Added Traits, The Sims Traits and Vanilla Traits Expanded no longer carry between sleeves.
- **Sleeve quality cancer rates fixed** (`altered.sleevecancer`) — Good-to-legendary sleeve quality genes accidentally *inverted* their cancer-rate stat; they now properly reduce it (90% down to 50%), while awful and poor sleeves keep their increased rates.

### GiTS Cyberbrains

- **Only basic cyberbrains sold** (`gits.merchant`) — Traders no longer stock the enhanced, specialized, advanced or extreme tiers, so buying a top-tier brain can't skip the progression. They stay craftable and can spawn on raiders.
- **Harsher extreme mental break** (`gits.mentalbreak`) — The PX-7 and HADES cyberbrains' mental break threshold penalty doubles from +20% to +40%.
- **Streamlined research tree** (`gits.research`) — The three nanite surgery researches collapse into one, empty filler nodes are deleted and prerequisites are rewired; no more one-recipe padding.
- **Surgeries via EPOE, ultratech tiers** (`gits.surgeries`) — Cyberbrain surgery unlocks with EPOE-Forked's Brain Surgery research, and everything past the basic cyberbrain moves to ultratech, integrating GiTS into the EPOE surgery progression and pushing the crazy tiers to endgame. Needs EPOE-Forked; does nothing without it.

### Odyssey

- **Long-range passenger shuttle** (`odyssey.shuttle`) — Chemfuel capacity 400 → 2000 and cargo capacity 500 → 2000 kg. The stock shuttle barely leaves the neighbourhood and I don't want to install modded shuttles.
- **Vacuum resistance trims on modded armor** (`odyssey.vacuumtrims`) — Only active with Vanilla Gravship Expanded - Chapter 1, whose balance assumes 100% vacuum resistance is hard to reach — while modded spacer armor hands it out freely. Helmets and suits from Rimsenal (Core and Federation), Altered Carbon 2, Spacer Arsenal and Impact Weaponry - Reloaded get their vacuum resistance trimmed a few points below the cap, with a couple of description fixes so items advertise what they actually do.

### Gene conflict fixes (`geneconflicts.*`)

Genes whose forced traits fight each other, or whose bonuses stack brokenly across mods, become mutually exclusive:

- **`geneconflicts.bloodlust`** — Big and Small's bloodlust and VRE - Highmate's distressed force traits that suppress each other and bug out when combined.
- **`geneconflicts.psychic`** — WVC's psychic UV/dark sensitivity genes can't combine with the vanilla psychically dull and deaf genes they fight.
- **`geneconflicts.firefoam`** — WVC's firefoam pop (suppresses Pyromaniac) vs Alpha Genes' fire obsession (forces it).
- **`geneconflicts.hemogen`** — Hemogen drain genes from vanilla, Big and Small and WVC can no longer stack with VRE - Sanguophage's.
- **`geneconflicts.deathless`** — Deathless-type genes (vanilla deathless, Big and Small's revenant soul and immortal return, WVC's undead and never dead, VRE - Archon's transcendent) can't be combined on one pawn.
- **`geneconflicts.dodge`** — Melee dodge genes from VQE - Ancients, Rimsenal Harana, Rimsenal Askbarn, Det's Keshig and Highborn Xenotype share VRE - Lycanthrope's dodge exclusion, so dodge bonuses can't stack across mods.
- **`geneconflicts.claws`** — Innate claw and talon attack genes can't stack: Alpha Genes' clawed hands, crab claw and pneumatic claw, WVC's kitty and archite claws, Big and Small's venom talons, VRE - Saurid's claws, VRE - Sanguophage's talons, VRE - Insector's charger claws and VQE - Ancients' plasteel claws.
- **`geneconflicts.bleedrate`** — Big and Small's slow bleeding vs VRE - Genie's hemophiliac; the two pull bleed rate in opposite directions.
- **`geneconflicts.flirty`** — VRE - Highmate's flirty vs Big and Small's never flirts.
- **`geneconflicts.meleespeed`** — Det's Brawnum's slow hitter joins VRE - Archon's melee attack speed exclusion, so melee speed genes can't stack across the two mods.

### Alpha Genes

- **Genes in vanilla genepacks** (`alphagenes.genepacks`) — Alpha Genes normally keeps its genes out of vanilla genepacks; this lets them spawn there at sane rates (cosmetics much rarer), makes alphapacks and mixedpacks unobtainable (existing ones keep working), and the gene-lab quest spawner yields vanilla genepacks only.
- **Rename angelic beauty** (`alphagenes.beautyrename`) — With WVC loaded, Alpha Genes' "angelic beauty" is relabeled *uncanny beauty* so it can't be confused with WVC's "angel beauty". Label only.

### Big and Small / VFE / VRE

- **`bigsmall.madscience`** — Mad science requires the Gun turrets research (instead of every turret it unlocks requiring Gun turrets individually, which also displayed wrong).
- **`bigsmall.geneintegrator`** — The gene integrator — it turns all xenogenes into endogenes, freeing the slots to stack more — moves to the archite research tier, costs an archite capsule plus ultratech materials, and gets a real market value.
- **`vfepirates.chargeweapons`** — Warcasket charge weapon boxes require pulse-charged munitions research (the railgun needs Mass Drivers with Coilguns loaded); the same gates apply to Warcasket Weapon Quality's direct-craft recipes.
- **`vfepirates.empirescenario`** — The Empire is no longer permanently hostile to the pirate scenario faction, so the Low orbit crash scenario's reputation can be repaired (Royalty).
- **`vfeempire.qol`** — The royal armchair counts for Stellarch throne rooms; the candelabra shows its glow radius when placing.
- **`vreinsector.colossalweapons`** — VRE - Insector's colossal insectors can wield Big and Small's giant weapons.

### Rimsenal

- **`rimsenal.armortechs`** — Rimsenal armors unlock from their matching corp techs instead of the generic Recon/Marine/Powered armor researches (sets that had no research at all get one).
- **`rimsenal.modularweapons`** — The modular carbine, its conversion kit and the MBPS armor kit move behind the corp defence tech; the GD multi launcher also needs Mortars.
- **`rimsenal.corpcost`** — Tier-1 corp techs cost 3000 research.
- **`rimsenalspacer.caravanmechs`** — Rimsenal Spacer trade caravans no longer bring mechanoid guards (their caravan generation was broken).
- **`rimsenalspacer.smartweapons`** — Smart weapons drop their redundant gunsmithing prerequisite, and the smart visor unlocks from the renamed *smart targeting systems* research.

### Memes & inspirations

- **Inspirations respect precepts** (`memes.inspirations`) — Inspirations no longer roll on pawns whose ideology forbids the activity: shooting/melee frenzies respect violence precepts, taming respects ranching, recreation-type inspirations respect joy precepts, and so on — including Vanilla Social Interactions Expanded's frenzies (Ideology).
- **`memes.factions`** — Warlike Rimsenal factions can't roll Alpha Memes' vow of nonviolence, which broke their combat pawns.
- **`memes.anomalytraits`** — The Occultist trait and void fascination agree with the Inhuman and Ritualist memes (Anomaly).

### Integrated Implants

- **`implants.chipbad`** — Skill chips are no longer treated as ailments, so healer serums and biosculpting don't remove them.
- **`implants.chiptiers`** — The mechanitor implants (mechhive satellite uplink, mechwomb, warprogrammer interface, remote dominator) cost Alpha Mechs' high-tier chips instead of cheap ones.
- **`implants.voicelockmasochist`** — Masochists enjoy being voicelocked (+8 mood instead of -8).
- **`implants.shoulderslimes`** — Shoulder turrets install on the shoulder instead of the torso, which slime and robot bodies don't have (fixes errors with Big and Small - Slimes).
- **Levitating implants ignore water** (`implants.waterpathing`) — Pawns with the psychic levitator or gravlifter float over water instead of wading.
- **Signal boosters stack with AG command range genes** (`implants.boosterrange`) — Alpha Genes' command range genes used to override Integrated Implants' signal boosters entirely; now the gene sets the base range and boosters extend it, with the command ring drawn correctly. Needs both mods.

### Weapons & apparel

- **`impactweaponry.bolterprereq`** — The warcasket impact bolter needs spacer warcasket weaponry plus impact shot, dropping a redundant extra prerequisite.
- **`spacerarsenal.prereqs`** — Spacer Arsenal's heavy weapons unlock from Vanilla Weapons Expanded's Heavy Weapons + Fabrication; the coil weapons from Mass Drivers (Coilguns).
- **`eltex.spawns`** — Eltex weapons stop spawning on random raiders and appear where they belong: Empire cataphracts, psycasters and deserters (Royalty).
- **`alphamemes.vacstonetiles`** — Alpha Memes' styled tiles can be built from Odyssey's vacstone blocks.

### Vanilla & DLC

- **`vanilla.healingenhancer`** — The Royalty healing enhancer uses the visible injury healing stat instead of a hidden one, so its effect shows on the pawn's stat card.
- **`vanilla.mechraidgroups`** — Mechanoid raids come in combined compositions mixing vanilla, Alpha Mechs and Rimsenal Spacer mechs.
- **`vanilla.toxicmeat`** — VAE Waste's toxic meat is unchecked by default in hoppers and meal recipes.
- **`vanilla.creepjoinersurgery`** — Creep joiners accept every surgery a regular human can get, including modded implants and prosthetics (Anomaly).
- **Gene complexity sliders** (`vanilla.genecomplexitybase`, `vanilla.genecomplexityprocessor`) — Two sliders: extra base gene complexity for the gene assembler (default +10), and complexity per gene processor (default 3, vanilla 2). Toggling either off keeps the vanilla value.

### VQE Ancients

- **`vqea.sittable`** — The ancient hospital armchair and bench become actual seats.
- **`vqea.giantweapons`** — The Enormous and Herculean archite genes let their carriers wield Big and Small's giant weapons.
- **`vqea.patientgown`** — The patient gown's blunt armor drops from 0.5 to 0.1, so pawns stop preferring it over real armor.
- **Archogen injector whitelist** (`vqea.injectorwhitelist`) — The archogen injector and ancient-experiment pawns normally roll from *every* loaded archite and negative gene — absurd with a large modlist, including pawn-ruining drawbacks. They now roll from a curated list: VQE - Ancients' own archite powers plus mild drawbacks from vanilla and the major gene mods.

---

## Genetics Overhaul

Everything that reshapes the gene pool itself: pruning the duplicates the modlist accumulates, then handing individual xenotypes the genes that suit them. The research tree is a separate module, [Genetics Research Overhaul](#genetics-research-overhaul).

### Genepool cleanup

With every gene mod loaded the genepool holds hundreds of genes, dozens of which do the same thing under different names. This keeps one canonical gene per function, removes the duplicates, and rewires every xenotype that carried a duplicate to the canonical version — races keep their identity through shared genes. Removals run through **Cherry Picker** (a mod dependency) automatically, with no Cherry Picker setup needed; toggling a setting off restores its genes on the next restart. Only active when Alpha Genes, WVC - Xenotypes and Genes and Big and Small - Genes & More are all loaded (except the Hussar aptitudes toggle, which needs only VRE - Hussar). The full list of removed genes, replacements and xenotype changes is in `Docs/GeneChanges.md`.

- **`genetics.agsummons`** — Removes Alpha Genes' animal summon family (~90 genes, one per supported animal). No xenotype uses them; they only dilute the pool and I don't like them.
- **`genetics.wvcdupes`** — Removes WVC genes that duplicate vanilla Biotech genes or WVC's own alternatives (~50). WVC xenotypes get the surviving equivalent instead.
- **`genetics.bsdupes`** — Removes Big and Small's three gene stabilizing genes (balance, no replacement) and the deathlike body gene (undead xenotypes get unstable deathlessness instead).
- **`genetics.dedup`** — The cross-mod deduplication: Alpha Genes keeps immunities, natural armor, bandwidth, pack mule and the like; Big and Small keeps body size, gender, no pain and healing speed; each specialist VRE race pack keeps its specialty; Det's packs keep their signature quirks; WVC's archite-tier uniques win over everyone's natural versions. Duplicates whose canonical mod is missing are left alone.
- **`genetics.hussaraptitudes`** — VRE - Hussar generates one weapon-aptitude gene per craftable weapon (~300 with a large modlist). They're replaced by four category genes — light and heavy melee aptitude, light and heavy ranged aptitude (heavy = 3 kg and up) — with the same bonus and cost. The hussar xenotypes still get a random aptitude; with Gene Nodes - Genes for Sale, a new archite gene node delivers the four genes. Pawns from older saves that carried a per-weapon aptitude lose it with a one-time load warning.

### Xenotype gene integration

Small thematic gene additions to individual xenotypes, drawing on the cleaned-up genepool. Each toggle needs the xenotype's mod plus the gene's mod, and does nothing when either is missing.

- **`genetics.boglegwater`** — Boglegs gain water striding (Alpha Genes): no movement penalty in watery terrain.
- **`genetics.stonebornskin`** — Det's Stoneborn gain stoneskin (WVC - Xenotypes and Genes): stone-covered bodies with natural armor and very low flammability, at a metabolism cost. Their appearance changes to stone-like skin.
- **`genetics.neanderthalfrost`** — Neanderthals gain frostbite resistance (Alpha Genes): frostbite damage halved.
- **`genetics.wvcspawns`** — WVC - Xenotypes and Genes' most powerful races (ferrkind, metalkin, rustkind, deadcat) no longer spawn as random wanderers, refugees, beggars or faction pawns. They remain obtainable through WVC's own events, morphs and implanters, like the rest of its top tier.

## Genetics Research Overhaul

A cohesive rework of genetics research: vanilla puts a full gene-editing empire behind two cheap industrial researches; this stages it from basic sampling to archogenetics and gives every genetics mod a common backbone. Requires **Biotech**; every module has its own toggle and does nothing if its target mod is missing.

### The tree

All projects sit on a new **Genetics** research tab, at spacer tech, on the hi-tech research bench. Costs escalate down the tree.

```mermaid
graph LR
    S[basic genetic sampling<br/>1000] --> X[xenogerm assembly<br/>1500]
    X --> GP[gene processor<br/>2500]
    X --> GN[gene nodes<br/>1200]
    X --> GC[genepack centrifuge<br/>1400]
    X --> GR[gene ripper<br/>1200]
    GP --> A[archogenetics<br/>4000]
    GP --> EV[gene extraction vats<br/>3000]
    GP --> XR[xenogerm replicator<br/>2000]
    GP --> GT[gene toolkits<br/>2500]
    A --> AE[archite gene extraction<br/>5000]
    A --> AGN[archite gene nodes<br/>4500]
    A --> GF[gene fabrication<br/>8000]
    A --> ARE[archogen engineering<br/>10000]
```

### Core tree (`geneticsresearch.core`)

Creates the Genetics tab rooted on a new *basic genetic sampling* project (gene extractor and gene bank unlock there), renames Xenogermination to *xenogerm assembly*, and moves the gene processor and archogenetics onto the tab with raised costs. Third-party gene buildings automatically default to the sampling unlock.

### ReSplice: Core (`geneticsresearch.resplice`)

The gene centrifuge and xenogerm duplicator become deliberate unlocks behind new *genepack centrifuge* and *xenogerm replicator* projects, renamed to match.

### Gene Extractor Tiers (`geneticsresearch.extractortiers`)

The gene extraction vat becomes a mid-tree unlock and the two archite vats a late-tree one, so extraction stops being trivialised the moment basic xenogenetics finishes.

### Gene nodes (`geneticsresearch.genenodes`)

Base gene nodes get their own project after xenogerm assembly. Archite node libraries are effectively free archite genepacks, so every archite node — including the premium Ageless and Sanguophage tiers — moves behind *archite gene nodes* with real prices (more components, archite capsules, silver); nodes that shipped their own bargain prices now use the tier prices.

### Gene Ripper (`geneticsresearch.generipper`)

A kill-to-extract a specific gene machine shouldn't share the plain extractor's unlock: it moves behind its own *gene ripper* project.

### Gene Fabrication (`geneticsresearch.genefab`)

Fabricating genes from neutroamine is an end-of-tree power, not a gene-processor side grab: the research becomes an archogenetics capstone (cost 8000).

### VQE Ancients archogen lab (`geneticsresearch.vqea`)

A new *archogen engineering* capstone (10000, multianalyzer) lets you build the archogen injector and its 12 linkable lab facilities yourself at archite-tier costs — raiding ancient vaults stays the shortcut, research the long road.

### Alpha Genes gene toolkits (`geneticsresearch.agtools`)

Alpha Genes' eleven single-use gene tools normally come from traders and quest rewards only. A new *gene toolkits* project makes them all craftable at the fabrication bench, with costs scaling from genepack tweakers up to the archotech variants (which need an archite capsule). Trade acquisition is untouched.

### Alpha Genes quest flavour (`geneticsresearch.alphagenes`)

The abandoned biotech lab quest is renamed to xenogenetics-lab flavour matching the overhauled genetics theme.
## Xenotype Spawning Overhaul

Requires Biotech. With a lot of xenotype mods installed, the generic factions end up as a dumping ground: each mod adds its xenotypes to outlanders and pirates at similar weights, until baseliners are under 10% of everyone you meet and no faction has any character. This rebuilds the faction rosters around what each faction actually is, and nothing is deleted — a xenotype trimmed from one faction gains weight in one that suits it.

Unlike the other overhauls this one is **on by default**, since it changes who spawns rather than how a system works, and an ongoing colony sees it only in newly generated pawns.

- **Thematic xenotypes in vanilla factions** (`xenotypes.vanilla`) — Settled outlander towns lean industrial — Det's Half-foot, Biotech's genies, Det's Brawnum. Rough outlanders lean frontier — Det's Venators and Boglegs, yttakin, impids. Pirates lean predatory — Boglegs, Det's Buzzers, wasters, VRE - Hussar's uhlans. Every roster keeps at least 35% baseliners. Tribal factions, which rolled baseliner every single time, gain a small primitive-themed roster (neanderthals, VRE - Saurid's saurids, Det's Venators, impids) while staying about three-quarters baseliner. Also thins WVC's oddball xenotypes (Featherdust, Cat deity, Blank, Sandycat, Undead) out of the generic pools, with Undead and Sandycat joining the Horax cult instead (Anomaly).
- **Thematic xenotypes in the Empire** (`xenotypes.royalty`) — The Empire reads as aristocratic and military: Det's Avaloi, Biotech's hussars and genies and Highborn Xenotype's highborn stay prominent, while Det's Keshig and Brawnum, Rimsenal's Harana, Odyssey's starjack and VRE - Android's awakened androids leave the roster. Keeps 37% baseliners.
- **`xenotypes.odyssey`** — Odyssey's Salvagers gain Det's Half-foot alongside an even spread of Rimsenal, Det's and Alpha Genes xenotypes; the Traders guild gains Alpha Genes' Fleetkind.
- **`xenotypes.rimsenal`** — Rimsenal's Spacer factions gain the deep-space xenotypes displaced from planetside pools: Odyssey's starjack, Det's Keshig and Half-foot, and VRE - Android's awakened androids. WVC's Mechakin, Rogueformer and Genethrower also move here from the generic outlander and pirate pools.

## Sci-fi Renaming Overhaul

Renames fantasy-, Norse- and religion-flavoured races and factions to RimWorld's gene-engineering flavour: every renamed group reads as an engineered gene-line, with labels, descriptions, faction names and pawn kind names rewritten to match. Purely cosmetic — no stats, genes or spawning change, and existing saves are unaffected beyond the displayed names. One toggle per mod.

- **`scifinames.bsraces`** — Big and Small - Races: jotun become **gigants** (cryogigant, pyrogigant, half-gigant; the archite heirs become gigant primes), ogres **hulkers**, dvergr **deepkin**, nisse **minikin**, svartalfs **umbrakin**, redcaps **scrappers**, trolls **regenerants**, flesh golems **bioconstructs**, hearthguards and hearthdolls **warden and service synths**. The Kingdom of Muspelheim becomes the **Cinderhold Dominion**, the Tribes of Niflheim the **Permafrost Clans**, the Dvergr Trade Union the **Deepkin Trade Combine**, the little people union the **Minikin Union** and the ogre tribes the **Hulker Tribes**, with their fighters renamed to match.
- **`scifinames.bigsmall`** — Big and Small - Genes & More: the succubus becomes the **allurist**, the hellguard the **abyssal guard**, the imp the **greater impid**, the returned **reanimates** (decayed/skeletal variants) and the frost jotun adventurer a **cryogigant**.
- **`scifinames.heaven`** — Big and Small - Heaven and Hell: angels become **ascendants** (the authority the ascendant prime, Satan the adversary prime, the Grigori watchers, the Nephilim the halfwrought, the Lilim the nightwrought), demons become **abyssals** (gluttons devourers). The factions become the **Luminal Ascendancy** (emissaries), the **Abyssal Dominion** (abyssals) and the **Exiles**, and every holy/demonic pawn kind follows.
- **`scifinames.yokai`** — Big and Small - Yokai: kitsune become **vulpids**, nekomata **felids** and oni **hornbrutes** (crimson/cobalt, great and lesser); the Yokai Union becomes the **Chimeric Union**.
- **`scifinames.lamias`** — Big and Small - Lamias: lamia become **serpids**, sirens **mesmer serpids**, gorgons **petrifex serpids**, naga **greater serpids**, nagaraj **serpid primes** and Tiamat the **progenitor serpid**; the snake tribal federation becomes the serpid tribal federation and the Greek/Hindu myth references drop from descriptions.
- **`scifinames.slimes`** — Big and Small - Slimes: slimes become **plasmoids** across every xenotype, and the escaped slimes faction becomes the **Escaped Plasmoids** led by a plasmoid prime.
- **`scifinames.morexenos`** — Big and Small - More Xenotypes: the devilspider becomes the **dreadspider**.
- **`scifinames.wvc`** — WVC - Xenotypes and Genes: the undead xenotype becomes the **necrokin** and the lilif the **psykin**.
- **`scifinames.alphagenes`** — Alpha Genes: efreet become **cindrids** and nereids **abyssids**.

## Expertise Overhaul

**`vse.expertiseconsolidation`** — off by default. Needs Vanilla Skills Expanded.

With Vanilla Skills Expanded and its add-ons loaded, a pawn hitting level 15 is offered a list of roughly seventy expertises, most of them a single narrow stat: one for floors, another for smoothing, a third for roofs. Picking well means reading the whole list, and most entries are strictly worse than the two or three obvious ones. The bonuses are also enormous — they scale with expertise level up to 20, so a `+0.05` per level entry ends at `+1.0`, which for a chance-based stat means a hundred percentage points, and for a capped stat means most of the bonus is thrown away.

This toggle retires that list and replaces it with **32 broader expertises, two to four per skill**, each bundling everything that belonged together:

| Skill | Expertises |
| --- | --- |
| Shooting | Marksman, Gunslinger, Overchanneler (artillerist) |
| Melee | Warblade, Skirmisher, Overchanneler (vanguard) |
| Animals | Beastmaster, Herd steward |
| Plants | Cultivator, Wildwalker |
| Artistic | Virtuoso, Prolific artisan, Quietist |
| Construction | Master builder, Siteworker |
| Cooking | Gastronome, Victualler |
| Crafting | Artificer, Fabricator, Mechwright |
| Medicine | Chirurgeon, Physician |
| Mining | Assayer, Excavator |
| Intellectual | Scholar, Anomalist, Technician, Mechlord |
| Social | Diplomat, Taskmaster, Confidant, Cutpurse |

Every skill now offers a real trade-off — quality against throughput, offence against survivability, instead of a long list with one right answer.

### Strength

Bonuses are multipliers rather than flat additions wherever the stat allows it, and they top out around **+40% at expertise level 20**. Multiplying scales with the pawn instead of overwhelming them, and it stops bonuses from being silently wasted against a stat's ceiling. The handful of stats that start at zero — foraged food, art, construction and crafting quality — keep additive bonuses, tuned to the same strength.

Two things pull below that ceiling. **Combat expertises** are tuned down, because a percentage of shooting accuracy or melee damage is worth far more than the same percentage of cook speed. And **expertises carrying a lot of stats** are tuned down too, since breadth is itself power — an expertise touching eight stats at full strength would dwarf one touching two.

### Costs

Some expertises are deliberately not pure upside:

- **Virtuoso**, **Master builder** and **Artificer** raise the quality of what they produce and *lower the speed* at which they produce it. Their opposite numbers — Prolific artisan, Siteworker, Fabricator — do the reverse.
- **Overchanneler** and **Quietist** are psycast expertises, and psycasting is already strong, so both carry genuine drawbacks. Overchanneler grants a bigger neural heat ceiling and stronger psycasts, but breaks far more easily, tires much faster, and never fully sheds its heat. Quietist sheds heat effortlessly and casts for far less focus, but has a much smaller heat ceiling and weaker psycasts. Overchanneler is offered on both Melee and Shooting — as **vanguard** and **artillerist** respectively — so a psycaster is not forced into one combat style.

### Other mods

Expertises from **Alpha Skills**, **Hauts' Framework**, **Vanilla Fishing Expanded** and **Vanilla Gravship Expanded** are folded in when those mods are loaded: their stats ride along on whichever expertise they thematically belong to, so nothing is lost. Stats from **Integrated Implants**, **Mechanoid Upgrades**, **Altered Carbon** and **Vanilla Psycasts Expanded** are picked up the same way. Without them the same expertises simply carry fewer stats.

Two gaps in the base mods are filled: **Mechlord** and **Mechwright** cover mechanitors, Mechlord for bandwidth, control groups and reach, Mechwright for repair, running costs and combat trim. Both need Biotech. The psycast pair needs Royalty, and Anomalist needs Anomaly.

Save-compatible, pawns who already chose an expertise keep it, working exactly as before — the old entries are only removed from the selection screen.

---

