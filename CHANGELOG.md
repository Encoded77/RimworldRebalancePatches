# Changelog

## 1.10.0 (2026-07-24)

**Cybernetics Overhaul and Cybernetics Research Overhaul** — two new settings groups that pull the implant ecosystem into one system: Core, Royalty, Biotech, EPOE-Forked, Integrated Implants, GiTS Cyberbrains, Psychic Implants, Altered Carbon and VRE - Android. A single Cybernetics research tab rooted on one *surgical implantation* research, a clear three-branch progression (chassis, cortex, modules) in place of the mods' overlapping tabs, and two opposed capstones to build toward: synthetic ascension and symbiotic integration. Off by default; every part is its own toggle. Design notes in `Docs/CyberneticsChanges.md`.

- **Implants that host implants** — bolt-on implants that add a capability become modules and plug into a slot on a cyberbrain (cognitive) or thoracic frame (torso); host tier caps how many a pawn carries. Needs EBSG Framework.
- **One ladder of effect and price** — artificial body parts from every loaded mod share one tier ladder and are priced by what they do, so two parts doing the same job cost the same.
- **Cyberbrains by manufacturer** (`gits.cyberbrainnames`) — GiTS's cyberbrains are named for their role under three makers (Civis, Aegis, Echo) instead of factory codes, with descriptions rewritten in plain terms and a stated cortex-module slot count.

- **Group unlocks by every research that gates them** (`yart.unlockgrouping`) — on a Yet Another Research Tree project card, an item you can only make once a second research is finished was drawn in the plain unlock list, as though this project alone unlocked it, while the recipe that makes it was correctly filed under "Unlocked with *that research*". The item now joins its own recipe under that heading, matching how the vanilla research window groups them. Nothing about what has to be researched changes.

## 1.9.2 (2026-07-22)

- **`genetics.agsummons`** needs Alpha Genes and Cherry Picker. **`genetics.wvcdupes`** needs WVC and Cherry Picker. **`genetics.bsdupes`** needs Big and Small and Cherry Picker.
- **`genetics.dedup`** needs Cherry Picker plus any gene mod it has entries for. Each duplicate is still dropped only when the mod owning the surviving gene is loaded, so a smaller gene modlist now gets the part of the deduplication that applies to it instead of none of it.
- Duplicates that lose to a gene from a specific mod now say so individually, so nothing is removed without its replacement present.
- **`memes.factions`** now also requires one of Rimsenal - Spacer or Rimsenal - Federation; it was offered as available with neither loaded.
- **Hide empty research tabs** (`vanilla.hideemptyresearchtabs`) — a research tab that no research project is assigned to is no longer drawn in the research window. Applies to any tab from any mod, and the tab reappears by itself if a project is assigned to it again. The tab you are on and the Main tab are always shown.

## 1.9.1 (2026-07-21)

**More developer database dumps** — the gene and xenotype-faction dumps are joined by seven more, each with its own toggle in the Developer group and a debug action for use in-game: recipes and surgeries, hediffs, research projects and tabs, items and buildings, body part trees and races, item acquisition (which traders stock what, and every reward, quest and pawn-gear table that hands it out), and gating rule defs belonging to other mods. All are dev-mode only and nothing changes in a normal game.

- Each dump covers a single def type, so they can be joined for whichever question is being asked instead of a new dump being written per audit.
- Body dumps now record each race's body, so a surgery no modded race can receive can be told apart from an animal that simply lacks the part.
- Research dumps record each project's techprint item, and flag any project whose techprint has gone missing — such a project can never be finished.
- Each dump now runs independently, so one failing no longer stops the others.

## 1.9.0 (2026-07-21)

**Unified gene tools and serums lane** (`geneticsresearch.consumables`) — single-use genetic items came from three mods with three separate research lines in three tabs. They now share one lane on the Genetics tab: *gene serums* (spacer, after xenogerm assembly) replacing WVC's whole eight-project serum line, *gene toolkits* (spacer, after gene processor) for Alpha Genes' gene tools and Big and Small's tools and xenogerm cloners, and *gene integration* (ultratech, after archogenetics) for the gene integrator.

- The Genetics tab gains an ultratech band. Gene Fabrication was still spacer-tier despite sitting past archogenetics; it and VQE Ancients' archogen engineering are now ultratech, alongside the new gene integration capstone.
- Alpha Genes' archotech gene tools now require archogenetics as well as the toolkits project. They were previously craftable as soon as you had gene processor, which put chosen-gene surgery well ahead of the archite tier it belongs to.
- Big and Small's *experimental gene tools*, *experimental archite gene tools* and *animal size serums* projects are removed; their content moves onto the lane. WVC's eight serum projects all fold into *gene serums*.
- Big and Small's *mad science field testing* becomes **weaponized genetics**: ultratech, after archogenetics, on the Genetics tab. Its mutagenic ray weapons and turrets were previously reachable at industrial tier for 500 research points.
- Three redundant items are retired (needs Cherry Picker): the xenodiscombobulator, the archite xenogerm cloner and the germline mutator. They stop being craftable, tradeable and lootable, and Alpha Genes' random gene tool dispenser stops offering them; the items stay in place so existing saves holding one are unaffected.
- **Alpha Genes gene toolkits** and **Gene integrator at archite tier** are replaced by the single lane toggle. If you had either on, the lane is on.

**Gene Fabrication reads and sits where it should** (`geneticsresearch.genefab`) — the project is retitled from *Genetic Fabrication* to *gene fabrication* to match the rest of the tab, and its description rewritten: it previously described building multianalyzers with dark archotechnology, which is not what it unlocks.

- Gene Fabrication generates a genepack recipe for every gene in the game and marks each archite one as requiring archogenetics, which on a large gene modlist means hundreds of "make genepack" rows listed against that single project in the research tree. Those recipes drop the prerequisite, so the archogenetics entry is readable again and the research tree builds faster. The capstone already gates the fabricator, so nothing becomes craftable any earlier.

**Ancient archite genes cannot be fabricated** (`vqea.nofabricatedarchite`) — VQE - Ancients' 33 archite powers were among the genes Gene Fabrication would build to order, so herculean or matter phasing cost three archite capsules at a bench and bypassed the archogen injector whitelist entirely. Those recipes are removed; every other gene stays fabricable. Needs Gene Fabrication and Cherry Picker.

**Research from this mod shows its source** — research tree UIs badge each project with the mod it came from, read off a field the game only fills in for defs loaded from a file. Everything this mod injects by patch — the Genetics tab and its projects, the Vanilla Skills Expanded expertises, the aptitude genes, the Alpha Memes floors — came through unattributed and rendered blank. They now carry the mod's icon and answer to its name in research search filters.

## 1.8.0 (2026-07-21)

**Thematic xenotype rosters** — with many xenotype mods installed, the generic factions become a dumping ground: every mod adds its xenotypes to outlanders and pirates at similar weights until baseliners are under 10% of the people you meet and no faction has a recognisable character. The vanilla, Empire, Odyssey and Rimsenal faction rosters are now rebuilt around what each faction is, and every reworked roster keeps at least 35% baseliners.

- Settled outlander towns lean industrial, rough outlanders frontier, and pirates predatory; xenotypes trimmed from one faction gain weight in one that suits them instead of being dropped.
- Tribal factions gain a small primitive-themed roster. They previously rolled baseliner every single time, since the tribal base def had no xenotype set at all.
- The Empire leans aristocratic and military; deep-space xenotypes concentrate in Rimsenal's Spacer factions and Odyssey's Salvagers.
- The single **Xenotypes join fitting factions** toggle is replaced by one toggle per faction-owning mod (`xenotypes.vanilla`, `xenotypes.royalty`, `xenotypes.odyssey`, `xenotypes.rimsenal`), so the rework can be taken or left per mod. **Fewer WVC xenotypes in generic factions** folds into `xenotypes.vanilla`. If you turned either old toggle off, that choice carries over to the toggles that replaced it.
- Xenotype Spawning is now an overhaul group, sitting above the divider with the others. It is the one overhaul that ships **on** — it changes which xenotypes newly generated pawns roll, rather than reworking a system you build against, so an ongoing colony is unaffected except in pawns generated from here on.
## 1.7.0 (2026-07-20)

- New mod icon and preview image.

## 1.7.0 (2026-07-20)

**Expertise Consolidation** (`vse.expertiseconsolidation`, off by default) — Vanilla Skills Expanded's and modded narrow expertises are replaced by 32 broader ones, two to four per skill, so every pick is a genuine trade-off instead of a long list with one right answer. Bonuses become multipliers topping out near +40% at expertise level 20, instead of flat offsets reaching +100% or overshooting a stat's ceiling; combat and stat-heavy expertises are tuned below that ceiling. Expertises from Alpha Skills, Hauts' Framework, Vanilla Fishing Expanded and Vanilla Gravship Expanded are folded in when loaded, and stats from Integrated Implants, Mechanoid Upgrades, Altered Carbon and Vanilla Psycasts Expanded are picked up the same way. Pawns keep any expertise they already picked.

- Quality expertises now cost work speed: Virtuoso, Master builder and Artificer produce better work more slowly, against Prolific artisan, Siteworker and Fabricator going the other way.
- New mechanitor expertises: Mechlord (bandwidth, control groups, reach, gestation) on Intellectual and Mechwright (repair, running costs, combat trim) on Crafting.
- New psycast expertises with real drawbacks: Overchanneler (bigger heat ceiling and stronger casts, but breaks easily, tires fast, and never sheds all its heat) as vanguard on Melee and artillerist on Shooting, and Quietist (effortless heat recovery and cheap casts, but a small ceiling and weaker casts) on Artistic. Both need Royalty.

## 1.6.1 (2026-07-20)

- The mod settings window is now easier to navigate and shows at a glance which patches can actually apply with the current modlist.
- Fixed the Xenotype Spawning and Xenotype Gene Integration groups sharing one internal group key, which made a single header checkbox silently control both groups.

## 1.6.0 (2026-07-19)

**Sci-fi Renaming** — a new settings group that renames fantasy-, Norse- and religion-flavoured races and factions to gene-engineering flavour (labels, descriptions, faction names and pawn kinds). Purely cosmetic, one toggle per mod.

- Big and Small - Races: jotun → gigants (cryo/pyro/half, archite primes), ogres → hulkers, dvergr → deepkin, nisse → minikin, svartalfs → umbrakin, redcaps → scrappers, trolls → regenerants, flesh golems → bioconstructs, hearthguards/hearthdolls → warden/service synths; Muspelheim → Cinderhold Dominion, Niflheim → Permafrost Clans, Dvergr Trade Union → Deepkin Trade Combine, little people union → Minikin Union, ogre tribes → Hulker Tribes.
- Big and Small - Heaven and Hell: angels → ascendants (Satan → adversary prime, Grigori → watchers, Nephilim → halfwrought, Lilim → nightwrought), demons → abyssals (gluttons → devourers); Kingdom of Heaven → Luminal Ascendancy, Tyranny of Hell → Abyssal Dominion, Extraplanar Outcasts → Exiles.
- Big and Small - Genes & More: succubus → allurist, hellguard → abyssal guard, imp → greater impid, the returned → reanimates.
- Big and Small - Yokai: kitsune → vulpids, nekomata → felids, oni → hornbrutes (crimson/cobalt); Yokai Union → Chimeric Union.
- Big and Small - Lamias: lamia → serpids, sirens → mesmer serpids, gorgons → petrifex serpids, naga → greater serpids, nagaraj → serpid primes, Tiamat → progenitor serpid.
- Big and Small - Slimes: slimes → plasmoids, Escaped Slimes → Escaped Plasmoids.
- Big and Small - More Xenotypes: devilspider → dreadspider.
- WVC - Xenotypes and Genes: undead → necrokin, lilif → psykin.
- Alpha Genes: efreet → cindrids, nereids → abyssids.

## 1.5.0 (2026-07-19)

**Xenotype Gene Integration** — a new settings group adding thematic genes to individual xenotypes from the cleaned-up genepool. Each toggle needs the xenotype's mod plus the gene's mod.

- Boglegs gain water striding (Alpha Genes).
- Det's Stoneborn gain stoneskin (WVC - Xenotypes and Genes) — natural armor, very low flammability, stone appearance.
- Neanderthals gain frostbite resistance (Alpha Genes).
- WVC's apex races (ferrkind, metalkin, rustkind, deadcat) no longer spawn as wanderers, refugees, beggars or faction pawns — they must be earned through WVC's own events, morphs and implanters.

## 1.4.0 (2026-07-19)

**Genepool Cleanup** — a new settings group that deduplicates the gene ecosystem across the big gene mods. Now depends on Cherry Picker (removals are applied automatically, no Cherry Picker setup needed; toggling off restores). Active only when Alpha Genes, WVC - Xenotypes and Genes and Big and Small - Genes & More are all loaded. Full gene and xenotype details in `Docs/GeneChanges.md`.

- Alpha Genes' animal summon family removed (~90 genes, one per supported animal; no xenotype used them).
- WVC-internal duplicates removed (~50 genes duplicating vanilla or WVC's own archite versions); Big and Small's gene stabilizing and deathlike body genes removed.
- Cross-mod deduplication: one canonical gene per function — specialist VRE packs keep their specialty, Big and Small keeps body mechanics, Alpha Genes keeps general-utility genes, Det's packs keep their signature quirks, WVC's archite uniques win over natural versions. ~300 genes removed in total across Alpha Genes, WVC, Big and Small (all race packs), the VRE packs, Det's Xenotypes and the Rimsenal xenotype packs.
- Every affected xenotype (~110) is rewired to the canonical replacement gene, so races keep their function through shared genes.
- VRE - Hussar's ~300 per-weapon aptitude genes consolidated into four category genes (light/heavy melee, light/heavy ranged, split at 3 kg) with the same bonus; hussars still roll a random aptitude, and a new archite gene node delivers them with Gene Nodes - Genes for Sale.
- New gene conflicts: claw/talon genes across seven mods, slow bleeding vs hemophiliac, flirty vs never flirts, melee speed genes.
- Alpha Genes' angelic beauty relabeled *uncanny beauty* when WVC's angel beauty is present.
- Dev tooling: the gene database dump can refresh automatically at the main menu, via a new Developer settings toggle (off by default, shown only with dev mode on).

## 1.3.1 (2026-07-19)

- Added tooling to dump loaded genes, xenotypes and genes acquisition method at runtime into a comprehensive json log.
- Added analyzer to aggregate dumped data into usable info.

## 1.3.0 (2026-07-19)

**Compat & rebalance batch** — a curated batch of cross-mod fixes, rebalances and new features. Every feature has its own settings toggle and an automated test.

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
