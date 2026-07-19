# Changelog

## 1.3.1 (2026-07-19)

- Added tooling to dump loaded genes, xenotypes and genes acquisition method at runtime into a comprehensive json log.
- Added analyzer to aggregate dumped data into usable info.

## 1.3.0 (2026-07-19)

**Compat & rebalance batch** — re-implements a curated selection of ideas from "Too Many Mods - Compats and Rebalances" alongside our own fixes and features. Every feature has its own settings toggle and an automated test.

- Gene conflict fixes: mutually exclusive gene pairs that force fighting traits or stack broken effects — B&S bloodlust × VRE-Highmate distressed, WVC psychic UV/dark sensitivity × vanilla psychically dull/deaf, WVC firefoam pop × Alpha Genes fire obsession, hemogen drains (vanilla/B&S/WVC on VRE-Sanguophage's tag), deathless variants (vanilla/B&S/WVC/VRE-Archon), and melee dodge genes across VQEA/Harana/Askbarn/Keshig/Highborn on VRE-Lycanthrope's tag.
- Alpha Genes: AG genes spawn in vanilla genepacks (cosmetics much rarer), alphapacks/mixedpacks unobtainable, gene-lab quest spawner yields vanilla genepacks; new *gene toolkits* research (Genetics tab, after gene processor) makes all 11 AG gene tools craftable at the fabrication bench, archotech variants costing an archite capsule.
- Integrated Implants: shoulder turrets install on the shoulder (B&S Slimes error fix), masochists enjoy the voicelock, mechanitor implants cost Alpha Mechs tier 4-6 chips, skill chips survive cleansing, levitating implants ignore water pathing, and signal boosters stack with Alpha Genes command range genes.
- Rimsenal - Core: armors and modular weapons gated behind the matching corp techs, GD multi launcher needs Mortars, corp tier-1 techs cost 3000; Spacer: trade caravans no longer bring mechanoid guards, smart weapons drop the gunsmithing prereq and the smart visor unlocks from the renamed *smart targeting systems*.
- VFE - Pirates: warcasket charge weapons require pulse-charged munitions (railgun: Mass Drivers with Coilguns) — also applied to Warcasket Weapon Quality's direct-craft recipes; Empire no longer permanently hostile to the pirate scenario faction.
- VQE - Ancients: sittable ancient hospital seating, Enormous/Herculean wield B&S giant weapons, patient gown blunt armor nerf, and the archogen injector rolls from a curated gene whitelist instead of every loaded archite and negative gene.
- Altered Carbon: advanced shields need Fabrication, cuirassier belt uses vanilla shield scaling, neural editor ignores body-bound traits, sleeve quality cancer rates fixed (were negative).
- Memes & inspirations: Occultist/void fascination agree with Inhuman/Ritualist, warlike factions can't roll the vow of nonviolence, and inspirations respect ideology precepts (violence, ranching, recreation... incl. Vanilla Social Interactions Expanded frenzies).
- Xenotype spawning: WVC's Mechakin/Rogueformer/Genethrower move to Rimsenal Spacer factions, Odyssey's Salvagers/Traders guild gain fitting modded xenotypes, WVC's oddball xenotypes leave generic factions (Undead/Sandycat join the Horax cult).
- Vanilla & DLC: healing enhancer uses the visible injury healing factor, combined mechanoid raid groups (with Alpha Mechs and Rimsenal Spacer), toxic meat unchecked by default on hoppers (VAE Waste), creep joiners accept every human surgery, and two new gene complexity sliders.
- Odyssey: vacuum resistance trims on Rimsenal/AC2/Spacer Arsenal/Impact Weaponry armor, gated on Vanilla Gravship Expanded - Chapter 1; VRE - Insector colossal insectors wield giant weapons; VFE - Empire royal armchair throne rooms + candelabra glow preview.
- Weapons: VSE gunner expertise uses the vanilla cooldown stat, IWR bolter prereqs cleaned, Spacer Arsenal heavy weapons via VWE researches, Eltex weapons only on psycasters, Alpha Memes vacstone styled tiles, B&S mad science requires gun turrets.
- Genetics fixes: archite gene nodes that shipped their own bargain costs (VIPER, Ageless, Sanguophage, soul nodes, AG Helixan) now use the tier prices; Big and Small's gene integrator moves to the archite tier with archite capsule costs and a real market value.

## 1.2.0 (2026-07-18)

Now depends on Harmony (needed for the casting relay range slider).

- Altered Carbon: new toggleable slider for how many world tiles of needlecasting range each powered casting relay adds (default 10; Altered Carbon's own value is 5 and toggling the patch off keeps it). The relay description updates to match.
- RimIOT - Logistic Matrix: with power consumption removed, building descriptions no longer mention power and the leftover power-draw comps are stripped.
- Odyssey: new group — passenger shuttle chemfuel capacity 400 → 2000 and cargo mass capacity 500 → 2000.
- Patches can now declare their default toggle state in XML (`<defaultOn>false</defaultOn>` on `PatchOperationIfEnabled`), so future big overhauls can ship off by default. Everything currently ships on.

## 1.1.0 (2026-07-18)

**Genetics Research Overhaul** — a new settings group, inspired by Progression: Genetics but with no Vanilla Genetics Expanded dependency. Requires Biotech; every module has its own toggle.

- New Genetics research tab: basic genetic sampling → xenogerm assembly (renamed Xenogermination) → gene processor → archogenetics, all at spacer tech on the hi-tech research bench. Gene extractor and gene bank unlock at basic genetic sampling, the gene assembler at xenogerm assembly.
- Research costs scale up the tree: cheap entry (1000), mid-tier 1200–2500, archite tier 4500–10000.
- ReSplice: Core: gene centrifuge and xenogerm duplicator move behind new genepack centrifuge (after xenogerm assembly) and xenogerm replicator (after gene processor) research projects, renamed to match.
- Gene Extractor Tiers: gene extraction vat behind a new project after gene processor; archite vats behind a new archite gene extraction project after archogenetics.
- Gene nodes (Gene Extractor Tiers + Gene Nodes - Genes for Sale): base nodes behind a new gene nodes project after xenogerm assembly; archite nodes behind a new archite gene nodes project after archogenetics, with raised build costs.
- Gene Ripper: machine behind a new gene ripper project after xenogerm assembly.
- Gene Fabrication: research moves to the Genetics tab as an archogenetics capstone.
- VQE Ancients: new archogen engineering capstone research makes the archogen injector and its 12 lab facilities buildable at archite-tier costs; recovering them from ancient labs remains the early route.
- Alpha Genes: abandoned biotech lab quest renamed to xenogenetics lab flavour.

## 1.0.0 (2026-07-18)

Initial release.

- RimIOT - Logistic Matrix: cheaper builds, basic components only, no power consumption.
- Altered Carbon: disables the redundant ranged shield belt from VAE Accessories in favour of the cuirassier belt.
- GiTS Cyberbrains: only basic cyberbrains on the market, surgeries via EPOE brain surgery, post-basic cyberization at ultratech tier, harsher extreme mental break, and a streamlined research tree.
- Settings window with per-mod groups and per-feature toggles (restart required to apply).
