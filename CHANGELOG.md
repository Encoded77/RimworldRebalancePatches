# Changelog

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
