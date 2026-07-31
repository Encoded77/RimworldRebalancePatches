# Gene Changes

Everything the Genetics Overhaul module changes in the gene ecosystem: which genes are removed, which version is kept as the canonical one, what each affected xenotype receives instead, and which genes can no longer stack. Every part is a separate settings toggle (restart to apply); turning a toggle off restores its genes on the next restart. Removals run through Cherry Picker (a dependency) automatically — no Cherry Picker setup needed. Each section applies on its own terms: it needs Cherry Picker plus the mod owning the genes it removes, and where a gene loses to another mod's version, that mod too. Nothing is removed without its replacement present, so a partial gene modlist gets the part of the cleanup that fits it.

## Removed genes

### Alpha Genes summon genes (`genetics.agsummons`)

The whole animal summon family: one summon gene per supported animal (~90 with a large modlist), the three summon randomizers and the temporary bandwidth gene. No xenotype uses any of them; they only dilute the genepool.

### WVC-internal duplicates (`genetics.wvcdupes`)

WVC genes that duplicate vanilla Biotech genes or WVC's own alternatives (~50). The duplicates are removed and WVC's xenotypes carry the surviving version instead. Highlights: the psychically dull/deaf, extra pain, perfect immunity, non-senescent, ageless, never sleep, delicate and undead copies (vanilla versions kept); natural mechlink/psylink/telepathy and temperature/tox variants that have archite versions (archite kept); the pattern aptitude and MechaAI cosmetic-mechanics families; the non-archite reimplanter family (the archite implanter fang and post-implanter genes stay); unbreakable and invulnerable.

### Big and Small internal/legacy (`genetics.bsdupes`)

The three gene stabilizing genes (balance removal, no replacement) and the deathlike body gene (undead xenotypes get unstable deathlessness instead).

### Cross-mod duplicates (`genetics.dedup`)

One canonical gene per function. The guiding idea: *the most specialized mod wins its home turf* —

- Specialist VRE race packs always keep their specialty (Pigskin aging, Archon pregnancy and melee speed, Saurid eggs, Waster instability, Highmate libido, Sanguophage talons and UV, Hussar temperament, Fungoid learning, Genie immunity, Lycanthrope nocturnal, Starjack vacuum).
- Big and Small keeps body mechanics: size, gender, pain, healing speed, giant weapons, acid, undergrounder.
- Alpha Genes keeps general-utility singles: heat/cold immunity, natural armor, foodless, bandwidth, pack mule, fertility, tox vulnerability, psychic camouflage.
- Det's packs keep their signature quirks (Venators farsighted, Stoneborn irascible, Buzzers attraction, Keshig deft).
- WVC's archite-tier uniques (mechlink, psylink, telepathy, blindness) beat everyone's natural versions.

Each entry only applies when the mod whose gene wins is actually loaded; otherwise both versions are left alone. Removed genes' VRE - Starjack astrogene copies are removed with them.
| Function | Kept (canonical) | Removed |
| --- | --- | --- |
| Heat immunity | AG_HeatImmunity (Alpha Genes) | MaxTemp_HugeIncrease, FireImmunity, BS_MaxTemp_HugeIncrease_Android, BS_FireImmunity_Android (B&S); WVC_MaxTemp_ArchiteIncrease (WVC) |
| Cold immunity | AG_ColdImmunity (Alpha Genes) | MinTemp_HugeDecrease, BS_MinTemp_HugeDecrease_Android (B&S); WVC_MinTemp_ArchiteDecrease (WVC) |
| No pain | BS_Pain_None (Big and Small) | AG_Painless (AG); WVC_Pain_Nullified (WVC) |
| Body size | Big and Small size family | AG_SmallerBodySize, AG_LargerBodySize (AG); WVC_BodySize_Small/Average/Large (WVC); DV_SmallBuild, DV_ExtrasmallBuild (Det's Stoneborn/Half-foot) |
| Fixed gender | Big and Small male/female | AG_Male, AG_Female (AG); WVC_FemaleOnly, WVC_MaleOnly, WVC_Monogender_Disabled (WVC); VRE_Female (VRE - Phytokin) |
| Acid immunity/weakness | Big and Small acid genes | AG_AcidImmunity, AG_AcidWeakness (AG) |
| Natural armor | Alpha Genes armor genes | BS_NaturalArmor, BS_ToughSkin (B&S); WVC_ArmoredSkin_Stone/Steel/Plasteel/Fortress, WVC_ClothedArmor (WVC); AG_ArmourMinor (AG-internal) |
| Giant weapons | BS_GiantWeaponWielder (B&S) | AG_ToughSinews (AG) |
| Foodless | AG Foodless (Alpha Genes) | BS_NoFood (B&S); WVC_DeadStomach (WVC) |
| Mechlink / psylink | WVC archite versions | AG_InnateMechlink, AG_InnatePsylink (AG); VRE_InnatePsylink (VRE - Archon) |
| Blindness | WVC Blindness | AG_NearBlindness (AG) |
| Mech bandwidth | Alpha Genes bandwidth genes | WVC_MechBandwidth_Enchanced/Extreme (WVC); DV_Bandwidth_High (Det's Half-foot) |
| Invisibility | AG psychic camouflage | WVC_Invisibility (WVC) |
| Healing speed | Big and Small healing genes | WVC_WoundHealing_UnrealFast, WVC_WoundHealing_SuperSlowHealing, WVC_BleedStopper, WVC_RepairSkin (WVC); VRE_WoundHealing_VerySlow (VRE - Genie) |
| Aging (needs VRE - Pigskin) | VRE - Pigskin aging genes | AG_FasterAging, AG_SlowerAging (AG); BS_Fast/Slow/VeryFast/VerySlowAging, VU_UltraRapidAging (B&S); WVC_NaturalFast/SlowAging, WVC_NaturalFastGrowing, WVC_ForeverYoung, WVC_AgeDebuff_* (WVC) |
| Pregnancy (needs VRE - Archon) | VRE - Archon pregnancy genes | AG_FastGestation, AG_SlowGestation (AG); BS_ShortPregnancy (B&S) |
| Early maturity | B&S Early maturity | VRE_EarlyMaturity (VRE - Archon) |
| Egg laying (needs VRE - Saurid) | VRE - Saurid Oviparous | AG_EggLaying (AG) |
| Photosynthesis (needs VRE - Phytokin) | VRE - Phytokin | AG_LightSustenance (AG) |
| Night owl (needs VRE - Lycanthrope) | VRE Nocturnal | AG_NightOwl (AG); BS_NightOwl (B&S) |
| Vat-grown insectoid skin (needs VRE - Insector) | VRE - Insector version | AG_VFEI_VatGrownInsectoidSkin (AG) |
| Cell instability (needs VRE - Waster) | VRE - Waster genes | AG_Instability_Lethal (AG); BS_Instability_Catastrophic (B&S) |
| Command range (needs Integrated Implants) | implant compat | AG_DecreasedCommandRange (broken alongside Integrated Implants per TMM) |
| Flirty / libido (needs VRE - Highmate) | VRE - Highmate genes | BS_Flirty, VU_Libido_Succubus (B&S) |
| Talons (needs VRE - Sanguophage) | VRE Talons | BS_Talons (B&S) |
| No study / skill loss (needs VRE - Fungoid) | VRE - Fungoid genes | BS_Learning_None (B&S); WVC_NoLearning, WVC_Learning_SlowNoSkillDecay, WVC_DisabledAllWork_Blank (WVC) |
| Abrasive, tough (needs VRE - Hussar) | VREH Arrogant, VREH Tough | BS_Abrasive (B&S); WVC_Tough (WVC) |
| Giant frame | B&S Large frame | VREH_Giant (VRE - Hussar) |
| No immunity (needs VRE - Genie) | VRE - Genie | WVC_Immunity_Non (WVC) |
| Deadly UV (needs VRE - Sanguophage) | VRE Dangerous UV sensitivity | WVC_UVSensitivity_Deadly (WVC) |
| Keen eye (needs Rimsenal Askbarn) | Askbarn reflex genes | WVC_FurskinInstincts_KeenEye (WVC) |
| Genome dominance (needs Better Gene Inheritance) | BGI recessive/dominant | VRE_DominantGenome, VRE_RecessiveGenome (VRE - Highmate) |
| Telepathy / antirot lungs | WVC telepathy, AG antirot | VRE_Telepathy, VRE_TotalAntirotLungs (VRE - Fungoid) |
| Vacuum sealing (needs VRE - Starjack) | VREStarjack_VacuumResistance_Total (VRE - Starjack) | BS_EVA_Gene, BS_AndroidEVA_Gene (B&S) |
| Strong back / undergrounder / tox resist / passive-aggressive / longshot / born warrior / low fertility / evergrowing / pheromones / sensitive stomach | AG pack mule, B&S undergrounder, AG tox vulnerability, Det's Stoneborn irascible, Det's Venators farsighted, VRE dodge/melee genes, AG reduced fertility, B&S endless growth, Det's Buzzers attraction, AG frail stomach | DV_StrongBack (Brawnum); DV_Undergrounder (Stoneborn); DV_ToxResist_Terrible (Avaloi); Aggression_PassivelyAggressive (Harana); RSLongshot, RSBornWarrior, LowFertility (Askbarn); VRE_EverGrowing (Pigskin); VRESaurids_Pheromones (Saurid); Gene_SensitiveStomach (Zohar) |

## Xenotype changes

Every xenotype that carried a removed gene receives the canonical replacement, so races keep their function through the shared gene. Replacements from absent mods are skipped, as are genes a xenotype already has.
| Xenotype | Gene lost | Canonical replacement gained |
| --- | --- | --- |
| AG_Drakonori (Alpha Genes) | AG_EggLaying | VRESaurids_Oviparous |
| AG_Lapis (Alpha Genes) | AG_SlowGestation | VRE_LongPregnancy |
| AG_Malachai (Alpha Genes) | AG_LargerBodySize, AG_Painless | BS_LargeFrame, BS_Pain_None |
| AG_Mycormorph (Alpha Genes) | VRE_Telepathy | WVC_NaturalTelepathy |
| AG_RoxTouched (Alpha Genes) | AG_LargerBodySize | BS_LargeFrame |
| AG_Taukai (Alpha Genes) | AG_Painless | BS_Pain_None |
| Askbarn (Rimsenal Xenotype Pack - Askbarn) | LowFertility, RSLongshot | AG_ReducedFertile, DV_Farsighted |
| BS_Abomination (Big and Small - More Xenotypes) | BS_ShortPregnancy | VRE_ShortPregnancy |
| BS_Authority (Big and Small - Heaven and Hell) | BS_GeneStabilizing_Great, FireImmunity, MaxTemp_HugeIncrease, BS_EVA_Gene | AG_HeatImmunity, VREStarjack_VacuumResistance_Total |
| BS_BananaSplitSlime (Big and Small - Slimes) | MinTemp_HugeDecrease | AG_ColdImmunity |
| BS_BananaSplitSlimeGiant (Big and Small - Slimes) | MinTemp_HugeDecrease | AG_ColdImmunity |
| BS_BlueOni (Big and Small - Yokai) | BS_SlowAging | VRE_SlowAging |
| BS_BrokenTitan (Big and Small - Races) | BS_Instability_Catastrophic, MinTemp_HugeDecrease, BS_EVA_Gene | AG_ColdImmunity, VRE_Instability_Extreme, VREStarjack_VacuumResistance_Total |
| BS_Broodmother (Big and Small - More Xenotypes) | BS_FastAging, BS_ShortPregnancy | VRE_FastAging, VRE_ShortPregnancy |
| BS_Corrupterd_Titan (Big and Small - Races) | BS_Instability_Catastrophic, BS_ShortPregnancy | VRE_ShortPregnancy, VRE_Instability_Extreme |
| BS_Devilspider (Big and Small - More Xenotypes) | FireImmunity | AG_HeatImmunity |
| BS_Dwarf (Big and Small - Races) | BS_SlowAging | VRE_SlowAging |
| BS_EmperorSlime (Big and Small - Slimes) | BS_SlowAging | VRE_SlowAging |
| BS_FireJotun (Big and Small - Races) | BS_VerySlowAging, FireImmunity, MaxTemp_HugeIncrease | AG_HeatImmunity, VRE_SlowAging |
| BS_FleshGolemServant (Big and Small - Races) | BS_Deathlike, BS_ToughSkin | AG_ArmourMedium, BS_LesserDeathless |
| BS_FrostJotun (Big and Small - Races) | BS_VerySlowAging, MinTemp_HugeDecrease | AG_ColdImmunity, VRE_SlowAging |
| BS_FrostJotunInBlue (Big and Small - Genes & More) | BS_VerySlowAging, MinTemp_HugeDecrease | AG_ColdImmunity, VRE_SlowAging |
| BS_FrostSlime (Big and Small - Slimes) | BS_SlowAging | VRE_SlowAging |
| BS_FrostSlimeGiant (Big and Small - Slimes) | BS_VerySlowAging | VRE_SlowAging |
| BS_Glutton (Big and Small - Heaven and Hell) | BS_ToughSkin | AG_ArmourMedium |
| BS_Gnome (Big and Small - Races) | BS_ShortPregnancy, BS_VerySlowAging | VRE_SlowAging, VRE_ShortPregnancy |
| BS_GreatBlueOni (Big and Small - Yokai) | BS_ToughSkin, BS_VerySlowAging | AG_ArmourMedium, VRE_SlowAging |
| BS_GreatOgre (Big and Small - Races) | BS_SlowAging | VRE_SlowAging |
| BS_GreatRedOni (Big and Small - Yokai) | BS_ToughSkin, BS_VerySlowAging, FireImmunity, MaxTemp_HugeIncrease | AG_ArmourMedium, AG_HeatImmunity, VRE_SlowAging |
| BS_Grigori (Big and Small - Heaven and Hell) | BS_Flirty, MinTemp_HugeDecrease, BS_EVA_Gene | AG_ColdImmunity, VRE_Flirty, VREStarjack_VacuumResistance_Total |
| BS_Half_Jotun (Big and Small - Races) | BS_VerySlowAging | VRE_SlowAging |
| BS_Hearthdoll (Big and Small - Races) | BS_NaturalArmor | AG_ArmourMedium |
| BS_HiveQueen (Big and Small - More Xenotypes) | AG_Female, AG_VFEI_VatGrownInsectoidSkin, BS_FastAging, BS_NaturalArmor, BS_ShortPregnancy | Body_FemaleOnly, AG_ArmourMedium, VRE_FastAging, VRE_ShortPregnancy, VRE_VatGrownInsectoidSkin |
| BS_Jotun (Big and Small - Races) | BS_ToughSkin | AG_ArmourMedium |
| BS_Kitsune (Big and Small - Yokai) | BS_Flirty, FireImmunity | AG_HeatImmunity, VRE_Flirty |
| BS_LavaSlimeGiant (Big and Small - Slimes) | BS_SlowAging | VRE_SlowAging |
| BS_LesserOni (Big and Small - Yokai) | BS_SlowAging | VRE_SlowAging |
| BS_LilGlutton (Big and Small - Heaven and Hell) | BS_ToughSkin | AG_ArmourMedium |
| BS_Lilim (Big and Small - Heaven and Hell) | BS_Flirty, BS_Talons, VU_Libido_Succubus | VRE_Flirty, VRE_Libido_VeryHigh, VRE_Talons |
| BS_Malakim (Big and Small - Heaven and Hell) | MinTemp_HugeDecrease, BS_EVA_Gene | AG_ColdImmunity, VREStarjack_VacuumResistance_Total |
| BS_Nekomata (Big and Small - Yokai) | BS_Deathlike, BS_NightOwl | VRE_Nocturnal, BS_LesserDeathless |
| BS_Nephilim (Big and Small - Heaven and Hell) | BS_ToughSkin, BS_VerySlowAging | AG_ArmourMedium, VRE_SlowAging |
| BS_Parasite (Big and Small - More Xenotypes) | BS_NightOwl, BS_ToughSkin | AG_ArmourMedium, VRE_Nocturnal |
| BS_PilotableFleshGolem (Big and Small - Races) | BS_Deathlike, BS_ToughSkin | AG_ArmourMedium, BS_LesserDeathless |
| BS_PinkSlime (Big and Small - Slimes) | BS_Flirty | VRE_Flirty |
| BS_Redcap (Big and Small - Races) | BS_FastAging, BS_ShortPregnancy, BS_ToughSkin | AG_ArmourMedium, VRE_FastAging, VRE_ShortPregnancy |
| BS_RedOni (Big and Small - Yokai) | BS_SlowAging, BS_ToughSkin | AG_ArmourMedium, VRE_SlowAging |
| BS_Satan (Big and Small - Heaven and Hell) | BS_Abrasive, BS_NaturalArmor, BS_Talons, MinTemp_HugeDecrease, BS_EVA_Gene | AG_ArmourMedium, AG_ColdImmunity, VRE_Talons, VREH_Arrogant, VREStarjack_VacuumResistance_Total |
| BS_Surtr (Big and Small - Races) | BS_NaturalArmor, BS_VerySlowAging, FireImmunity, MaxTemp_HugeIncrease, BS_EVA_Gene | AG_ArmourMedium, AG_HeatImmunity, VRE_SlowAging, VREStarjack_VacuumResistance_Total |
| BS_Svartalf (Big and Small - Races) | BS_NaturalArmor, BS_SlowAging | AG_ArmourMedium, VRE_SlowAging |
| BS_Troll (Big and Small - Races) | BS_ToughSkin | AG_ArmourMedium |
| BS_TrollAdult (Big and Small - Races) | BS_GeneStabilizing_Moderate, BS_NaturalArmor | AG_ArmourMedium |
| BS_TrollOld (Big and Small - Races) | BS_GeneStabilizing_Great | none (balance removal) |
| BS_ViperPrototypeBiomecha (Big and Small - Lamias and other Snake-People) | BS_ToughSkin, MinTemp_HugeDecrease | AG_ArmourMedium, AG_ColdImmunity |
| BS_Weaver (Big and Small - More Xenotypes) | BS_NaturalArmor, BS_NightOwl | AG_ArmourMedium, VRE_Nocturnal |
| BS_Ymir (Big and Small - Races) | BS_NaturalArmor, BS_VerySlowAging, MinTemp_HugeDecrease, BS_EVA_Gene | AG_ArmourMedium, AG_ColdImmunity, VRE_SlowAging, VREStarjack_VacuumResistance_Total |
| DV_Avaloi (Det's Xenotypes - Avaloi) | DV_ToxResist_Terrible | AG_ToxResist_Vulnerability |
| DV_Brawnum (Det's Xenotypes - Brawnum) | DV_StrongBack | AG_PackMule |
| DV_Halffoot (Det's Xenotypes - Half-foot) | DV_Bandwidth_High, DV_Melee_Fast | AG_BandwidthIncrease, VRE_FastMeleeHitter |
| Harana (Rimsenal Xenotype Pack - Harana) | Aggression_PassivelyAggressive | DV_Aggression_Irascible |
| LoS_Adderman (Big and Small - Lamias and other Snake-People) | BS_FastAging | VRE_FastAging |
| LoS_Anacondaman (Big and Small - Lamias and other Snake-People) | BS_ToughSkin | AG_ArmourMedium |
| LoS_ScenarioTiamat (Big and Small - Lamias and other Snake-People) | AG_AcidImmunity, BS_ShortPregnancy | BS_AcidResistanceTotal, VRE_ShortPregnancy |
| LoS_Silver (Big and Small - Lamias and other Snake-People) | BS_SlowAging, BS_ToughSkin | AG_ArmourMedium, VRE_SlowAging |
| Naga (Big and Small - Lamias and other Snake-People) | BS_SlowAging, BS_ToughSkin | AG_ArmourMedium, VRE_SlowAging |
| Stoneborn (Det's Xenotypes - Stoneborn) | DV_Undergrounder | Undergrounder |
| Uredd (Rimsenal Xenotype Pack - Askbarn) | LowFertility, RSBornWarrior | AG_ReducedFertile, DV_DodgeChance_High |
| VRE_Animakin (Vanilla Races Expanded - Phytokin) | VRE_Female | Body_FemaleOnly |
| VRE_Archon (Vanilla Races Expanded - Archon) | VRE_EarlyMaturity, VRE_InnatePsylink | BS_EarlyMaturity, WVC_ArchitePsylink |
| VRE_Boarskin (Vanilla Races Expanded - Pigskin) | VRE_EverGrowing | BS_EndlessGrowth |
| VRE_Fungoid (Vanilla Races Expanded - Fungoid) | VRE_Telepathy, VRE_TotalAntirotLungs | WVC_NaturalTelepathy, AG_LungRotImmunity |
| VRE_Gauranlenkin (Vanilla Races Expanded - Phytokin) | VRE_Female | Body_FemaleOnly |
| VRE_Ocularkin (Alpha Genes) | AG_Female | Body_FemaleOnly |
| VRE_Poluxkin (Vanilla Races Expanded - Phytokin) | VRE_Female | Body_FemaleOnly |
| VREH_Uhlan (Vanilla Races Expanded - Hussar) | VREH_Giant | BS_LargeFrame |
| VRESaurids_Saurid (Vanilla Races Expanded - Saurid) | VRESaurids_Pheromones | DV_Pheromones |
| VU_Gatekeeper (Big and Small - Genes & More) | BS_NaturalArmor, FireImmunity, MaxTemp_HugeIncrease | AG_ArmourMedium, AG_HeatImmunity |
| VU_Hellguard (Big and Small - Genes & More) | BS_NaturalArmor, FireImmunity, MaxTemp_HugeIncrease | AG_ArmourMedium, AG_HeatImmunity |
| VU_Imp (Big and Small - Genes & More) | FireImmunity, MaxTemp_HugeIncrease | AG_HeatImmunity |
| VU_Returned (Big and Small - Genes & More) | BS_Deathlike, VU_UltraRapidAging | VRE_RapidAging, BS_LesserDeathless |
| VU_Returned (Big and Small - Yokai) | BS_Deathlike, VU_UltraRapidAging | VRE_RapidAging, BS_LesserDeathless |
| VU_Returned_Intact (Big and Small - Genes & More) | BS_Deathlike, VU_UltraRapidAging | VRE_RapidAging, BS_LesserDeathless |
| VU_ReturnedSkeletal (Big and Small - Genes & More) | BS_Deathlike, MaxTemp_HugeIncrease, MinTemp_HugeDecrease | AG_HeatImmunity, AG_ColdImmunity, BS_LesserDeathless |
| VU_Succubus (Big and Small - Genes & More) | BS_ToughSkin, FireImmunity, MaxTemp_HugeIncrease, VU_Libido_Succubus | AG_ArmourMedium, AG_HeatImmunity, VRE_Libido_VeryHigh |
| WVC_Ashen (WVC - Xenotypes and Genes) | WVC_ArchitePerfectImmunity_DiseaseFree, WVC_Learning_Scarifier, WVC_MinMaxTemp_Scarifier, WVC_PatternAptitude_Shapeshifter, WVC_WoundHealing_UnrealFast | PerfectImmunity, DiseaseFree, AG_HeatImmunity, AG_ColdImmunity, VRE_NoSkillLoss, Learning_Slow, WoundHealing_UltraFast |
| WVC_Beholdkind (WVC - Xenotypes and Genes) | WVC_BodySize_Small, WVC_NaturalAgeless, WVC_PerfectImmunity_DiseaseFree, WVC_Tough, WVC_UVSensitivity_Deadly | Ageless, BS_TrulyAgeless, PerfectImmunity, DiseaseFree, BS_SmallFrame, VRE_Sensitivity_Dangerous, VREH_Toughness |
| WVC_Blank (WVC - Xenotypes and Genes) | WVC_AgeDebuff_Timeless, WVC_BodySize_Small, WVC_NaturalAgeless, WVC_NaturalFastGrowing | Ageless, BS_TrulyAgeless, BS_SmallFrame, VRE_Instability_Extreme, BS_EarlyMaturity |
| WVC_Bloodeater (WVC - Xenotypes and Genes) | WVC_Delicate, WVC_Dustogenic_Metabolism | Delicate, VRE_Photosynthesis |
| WVC_CatDeity (WVC - Xenotypes and Genes) | WVC_BodySize_Small, WVC_DeadStomach, WVC_Immunity_Non, WVC_Invisibility, WVC_PsychicAbility_Archite | PsychicAbility_Extreme, BS_SmallFrame, AG_Foodless, AG_Invisibility, VRE_Immunity_VeryWeak |
| WVC_Featherdust (WVC - Xenotypes and Genes) | WVC_BodySize_Small, WVC_Dustogenic_Metabolism, WVC_NaturalFastGrowing, WVC_Pain_Extra, WVC_WoundHealing_UnrealFast | Pain_Extra, VRE_Photosynthesis, BS_SmallFrame, WoundHealing_UltraFast, BS_EarlyMaturity |
| WVC_Fleshkind (WVC - Xenotypes and Genes) | WVC_BodySize_Small | BS_SmallFrame |
| WVC_Golemkind (WVC - Xenotypes and Genes) | WVC_ArmoredSkin_Plasteel, WVC_ForeverYoung, WVC_NaturalFastGrowing, WVC_PsychicAbility_Dull, WVC_Tough | PsychicAbility_Dull, AG_ArmourMajor, VRE_SlowAging, BS_EarlyMaturity, VREH_Toughness |
| WVC_Leper (WVC - Xenotypes and Genes) | WVC_FemaleOnly | Body_FemaleOnly |
| WVC_Lilith (WVC - Xenotypes and Genes) | WVC_ArchiteTelepathy, WVC_BodySize_Small, WVC_NaturalAgeless, WVC_NaturalImmunity_PerfectImmunity, WVC_Neversleep, WVC_PsychicAbility_Archite, WVC_ReimplanterNatural_Endogenes | WVC_NaturalTelepathy, Ageless, BS_TrulyAgeless, PerfectImmunity, Neversleep, PsychicAbility_Extreme, VRE_GermlineReimplanter, BS_SmallFrame |
| WVC_Meca (WVC - Xenotypes and Genes) | WVC_MechaAI_FirmwareMechanitorMachine, WVC_MechaHidden_ArchiteForge, WVC_Neversleep | BS_Fast_TotalHealing, Neversleep |
| WVC_Mechamata (WVC - Xenotypes and Genes) | WVC_MaxTemp_ArchiteIncrease, WVC_Mecha_EarsCat, WVC_MinTemp_ArchiteDecrease, WVC_Unbreakable | Ears_Cat, Robust, VREH_Toughness, AG_HeatImmunity, AG_ColdImmunity |
| WVC_Nociokin (WVC - Xenotypes and Genes) | WVC_NaturalAgeless, WVC_PerfectImmunity_DiseaseFree, WVC_Tough | Ageless, BS_TrulyAgeless, PerfectImmunity, DiseaseFree, VREH_Toughness |
| WVC_Overrider (WVC - Xenotypes and Genes) | WVC_ArchiteTelepathy | WVC_NaturalTelepathy |
| WVC_Reaperkind (WVC - Xenotypes and Genes) | WVC_BleedStopper, WVC_DeadStomach, WVC_MaxTemp_ArchiteIncrease, WVC_MinTemp_ArchiteDecrease, WVC_PsychicAbility_Deaf | PsychicAbility_Deaf, VU_NoBlood, AG_Foodless, AG_HeatImmunity, AG_ColdImmunity |
| WVC_Resurgent (WVC - Xenotypes and Genes) | WVC_MinMaxTemp_Natural, WVC_Pain_Nullified, WVC_ToxResist_Total, WVC_Undead | AG_HeatImmunity, AG_ColdImmunity, ToxResist_Total, Deathless, BS_Pain_None |
| WVC_Ripperkind (WVC - Xenotypes and Genes) | WVC_BleedStopper, WVC_MaxTemp_ArchiteIncrease, WVC_MinTemp_ArchiteDecrease, WVC_NaturalAgeless, WVC_NaturalImmunity_PerfectImmunity, WVC_PsychicAbility_Deaf | Ageless, BS_TrulyAgeless, PerfectImmunity, PsychicAbility_Deaf, VU_NoBlood, AG_HeatImmunity, AG_ColdImmunity |
| WVC_RogueFormer (WVC - Xenotypes and Genes) | WVC_BodySize_Small, WVC_MecaBodyParts_Lung, WVC_MechaAI_FirmwareRogueMachine, WVC_NaturalAgeless | Ageless, BS_TrulyAgeless, BS_SmallFrame |
| WVC_RuneDryad (WVC - Xenotypes and Genes) | WVC_AgeDebuff_MoveSpeed, WVC_WoundHealing_SuperSlowHealing | WoundHealing_VerySlow, BS_Very_Slow |
| WVC_Rustkind (WVC - Xenotypes and Genes) | WVC_NaturalAgeless, WVC_ReimplanterArchite_RiseXenogenes, WVC_ReimplanterArchite_Xenogenes | Ageless, BS_TrulyAgeless, XenogermReimplanter, WVC_ReimplanterNatural_RiseXenogenes |
| WVC_Sacrifice (WVC - Xenotypes and Genes) | WVC_BodySize_Large, WVC_MaxTemp_ArchiteIncrease, WVC_MechaAI_Base, WVC_MinTemp_ArchiteDecrease, WVC_NoLearning, WVC_PsychicAbility_Deaf, WVC_Tough | VRE_NoSkillLoss, PsychicAbility_Deaf, BS_LargeFrame, AG_HeatImmunity, AG_ColdImmunity, VRE_NoStudy, VREH_Toughness |
| WVC_Sandycat (WVC - Xenotypes and Genes) | WVC_BodySize_Small, WVC_ImplanterFangs, WVC_NaturalAgeless | WVC_ImplanterFang, Ageless, BS_TrulyAgeless, BS_SmallFrame |
| WVC_Shadoweater (WVC - Xenotypes and Genes) | WVC_ImplanterFangs, WVC_Tough, WVC_UVSensitivity_Deadly | WVC_ImplanterFang, VRE_Sensitivity_Dangerous, VREH_Toughness |
| WVC_Undead (WVC - Xenotypes and Genes) | WVC_NaturalFastAging, WVC_NaturalFastGrowing, WVC_ToxResist_Total, WVC_Undead | ToxResist_Total, Deathless, VRE_FastAging, BS_EarlyMaturity |
| Zohar (Rimsenal Xenotype Pack - Zohar) | Gene_SensitiveStomach | AG_FrailStomach |

## Xenotype gene integration (`genetics.boglegwater`, `genetics.stonebornskin`, `genetics.neanderthalfrost`, `genetics.wvcspawns`)

Thematic gene additions to individual xenotypes, each behind its own toggle and active only when both the xenotype's mod and the gene's mod are loaded.

| Xenotype | Gene added | Effect |
| --- | --- | --- |
| DV_Bogleg (Det's Xenotypes - Boglegs) | AG_WaterStriding (Alpha Genes) | no movement penalty in watery terrain |
| Stoneborn (Det's Xenotypes - Stoneborn) | WVC_StoneSkin (WVC - Xenotypes and Genes) | +22% blunt / +12% sharp / +11% heat armor, flammability ×0.2, stone appearance; met -3, cpx 4 |
| Neanderthal (Biotech) | AG_FrostbiteResistance (Alpha Genes) | frostbite damage ×0.5; met -1 |

Spawn changes (`genetics.wvcspawns`): WVC_Ferrkind, WVC_GeneThrower, WVC_Rustkind and WVC_CatDeity get `factionlessGenerationWeight` 0 and are removed from the faction spawn pools WVC adds them to (OutlanderCivil, PirateWaster, OutlanderRefugee, Beggars). They stay available through WVC's events, morphs and implanter chains.

## Hussar weapon aptitudes (`genetics.hussaraptitudes`)

VRE - Hussar creates one weapon-aptitude gene per craftable weapon (~300 with a large modlist). The whole family is replaced by four category genes with the same bonus and cost: light and heavy melee aptitude, light and heavy ranged aptitude — heavy meaning weapons of 3 kg and up (charge lances, miniguns, giant weapons), light everything below (knives, swords, pistols, rifles under 3 kg). The Hussar and Uhlan xenotypes still receive a random aptitude at spawn, now one of the four. The new genes never roll in genepacks; with Gene Nodes - Genes for Sale loaded, a new archite gene node delivers them, and otherwise they can be extracted from hussars. Pawns from older saves that carried a per-weapon aptitude lose it with a one-time load warning.

## Gene conflicts (`geneconflicts.*`)

Cross-mod gene pairs that fight each other or stack brokenly become mutually exclusive: bloodlust vs distressed, psychic UV sensitivity vs psychically dull/deaf, firefoam pop vs fire obsession, hemogen drains, deathless variants, melee dodge genes, claw and talon genes, slow bleeding vs hemophiliac, flirty vs never flirts, and melee speed genes. Details per toggle in `Documentation.md`.
## Psycast path genes (`vpebiotech.*`)

With VPE - Biotech Integration loaded, psycast paths are gated by psycast genes. Two features complete that coverage across the modlist; both need Biotech, Vanilla Psycasts Expanded and VPE - Biotech Integration.

### New gating genes (`vpebiotech.pathgenes`)

Seventeen new marker genes in the Psycast display category (3 cpx, market value factor 3, original icons), one per path that previously had no gate at all. Mechanitor-only and void-focus paths keep their own gates. Each gene also joins the psycast gene node (`geneticsresearch.psycastnodes`) when that is enabled.

| Gene | Gates path | Path mod |
| --- | --- | --- |
| RBP_Gene_HeraldOfTheBlackHive | Herald of the Black Hive | Alpha Animals |
| RBP_Gene_Oculist | Oculist | Alpha Animals |
| RBP_Gene_Ravager | Ravager | Alpha Animals |
| RBP_Gene_Xenoseer | Xeno-seer | Alpha Animals |
| RBP_Gene_Kinecticist | Kinecticist | Combat Psycasts |
| RBP_Gene_Runesmith | Runesmith | VPE - Runesmith |
| RBP_Gene_Aeromancer | Aeromancer | Sentinel - Aeromancer |
| RBP_Gene_Animancer | Animancer | Sentinel - Anima |
| RBP_Gene_Biohazard | Biohazard | Sentinel - Biohazard |
| RBP_Gene_Biosoother | Biosoother | Sentinel - Biohazard |
| RBP_Gene_Bugmancer | Bugmancer | Sentinel - Bugmancer |
| RBP_Gene_Deadlife | Deadlife | Sentinel - Deadlife |
| RBP_Gene_Fleshshaper | Fleshshaper | Sentinel - Fleshshaper |
| RBP_Gene_Gauranlen | Awoken Gauranlen | Sentinel - Awoken Gauranlen |
| RBP_Gene_Geomancer | Geomancer | Sentinel - Geomancer |
| RBP_Gene_Gravcaster | Gravcaster | Sentinel - Gravcaster |
| RBP_Gene_Hydromancer | Hydromancer | Sentinel - Hydromancer |

### Xenotype assignments (`vpebiotech.xenotypes`)

Every xenotype without a psycast gene receives one to three thematic ones; a few covered xenotypes gain a fitting addition (marked +). Each line applies only when the mods owning the gene and the xenotype are both loaded. Skipped on purpose: baseliners, template xenotypes (random custom, sleeveliners, blankind), pilotable constructs and androids.

| Xenotype(s) | Genes |
| --- | --- |
| Dirtmole (+) | Geomancer |
| Waster (+) | Biohazard, Herald of the Black Hive |
| Hussar (+) | Ravager, Kinecticist |
| Starjack (+) | Gravcaster |
| Genie (+) | Xeno-seer |
| Yttakin (+), VREH_Uhlan (+) | Ravager |
| VRE_Lycan (+), VRE_Wolfman (+) | Ravager (+ Warlord on the lycan) |
| VRE_Ocularkin (+) | Wildspeaker, Xeno-seer, Oculist |
| Uredd (+), Askbarn | Kinecticist on Uredd; Staticlord + Skipmaster on both |
| AG_Animusen | Wildspeaker, Empath, Xeno-seer |
| AG_Drakonori | Archon, Conflagrator, Warlord, Ravager |
| AG_Efreet | Conflagrator, Warlord |
| AG_Lapis | Technomancer, Geomancer |
| AG_Malachai | Hemosage, Necropath, Warlord |
| AG_MindDevourer | Puppeteer, Archon, Harmonist |
| WVC_Ashen | Necropath, Nightstalker, Warlord |
| WVC_Featherdust | Archotechist, Necropath, Staticlord, Technomancer |
| WVC_GeneThrower | Archotechist, Conflagrator, Frostshaper |
| WVC_Golemkind | Technomancer, Protector |
| WVC_Meca | Staticlord, Technomancer, Archotechist |
| WVC_Resurgent | Necropath, Archon, Nightstalker |
| WVC_RogueFormer | Archotechist, Nightstalker, Conflagrator, Frostshaper |
| WVC_RuneDryad | Technomancer, Staticlord, Runesmith |
| WVC_Undead | Necropath, Warlord |
| AG_Fleetkind | Gravcaster, Xenoseer |
| AG_Nereid | Hydromancer, Skipmaster |
| AG_Wretch | Fleshshaper |
| AG_Taukai (+), AG_Helixien (+), AG_Hiveling (+), AG_Mycormorph (+) | Fleshshaper / Deadlife / Bugmancer / Biohazard |
| VU_Gatekeeper | Protector, Geomancer |
| VU_Hellguard | Warlord, Conflagrator |
| VU_Imp | Skipmaster, Technomancer |
| VU_Returned family | Deadlife, Necropath |
| VU_Succubus | Empath, Puppeteer |
| BS_FrostJotunInBlue | Frostshaper |
| BS_Authority | Warlord, Protector, Archon |
| BS_Malakim | Empath, Harmonist, Skipmaster |
| BS_Grigori | Mindbender, Xenoseer |
| BS_Nephilim | Warlord |
| BS_Lilim | Nightstalker, Puppeteer |
| BS_Satan | Conflagrator, Warlord, Archon |
| BS_Glutton, BS_LilGlutton | Ravager (+ Fleshshaper on the greater) |
| Lamias: Adderman / Anacondaman, Pythonman / Gorgon / Lamia / Tiamat / Argent / Siren / Naga / Nagaraj | Biohazard / Ravager / Geomancer + Nightstalker / Nightstalker / Fleshshaper + Hydromancer / Harmonist + Empath / Harmonist + Hydromancer / Warlord + Biohazard / Archon + Chronopath + Harmonist |
| BS_Abomination | Fleshshaper, Ravager |
| BS_Broodmother | Fleshshaper, Bugmancer |
| BS_Devilspider, BS_Weaver | Bugmancer, Nightstalker |
| BS_HiveQueen | Bugmancer, Herald of the Black Hive |
| BS_Mimic | Mindbender, Nightstalker |
| BS_Parasite | Puppeteer, Mindbender |
| BS_Dwarf | Geomancer, Runesmith |
| BS_Svartalf | Nightstalker, Runesmith |
| BS_Gnome | Technomancer, Runesmith |
| BS_Redcap | Ravager |
| BS_Jotun | Geomancer, Protector |
| BS_Half_Jotun, BS_BrokenTitan | Protector |
| BS_FireJotun, BS_Surtr | Conflagrator (+ Warlord on Surtr) |
| BS_FrostJotun, BS_Ymir | Frostshaper (+ Warlord on Ymir) |
| BS_Corrupterd_Titan | Ravager, Necropath |
| BS_Ogre, BS_GreatOgre | Ravager (+ Warlord on the great) |
| BS_Troll family | Fleshshaper (+ Gauranlen on the elder) |
| Slimes: green / pink / elixir / emperor / frost + banana split / lava | Fleshshaper / Biosoother + Fleshshaper / Biosoother / Harmonist + Fleshshaper / Frostshaper / Conflagrator |
| BS_ToxicSlduge | Biohazard |
| BS_Kitsune | Mindbender, Conflagrator, Puppeteer |
| BS_Nekomata | Necropath, Deadlife |
| Oni family | Ravager, Conflagrator (red) or Staticlord (cobalt), Warlord on the greats |
| DV_Avaloi | Empath, Gravcaster, Harmonist |
| DV_Bogleg | Hydromancer, Biohazard |
| DV_Brawnum | Protector, Wildspeaker |
| DV_Buzzer | Bugmancer, Biohazard |
| DV_Halffoot | Technomancer, Skipmaster |
| DV_Keshig | Warlord, Protector |
| Stoneborn | Geomancer, Runesmith |
| DV_Venator | Nightstalker, Xenoseer |
| HBX_Highborn | Empath, Harmonist, Warlord |
| Uredd | Staticlord, kinecticist |
| Harana | Aeromancer, Skipmaster |
| Zohar | Protector, Chronopath |
| VRE_Animakin (+), VRE_Gauranlenkin (+), VRE_Poluxkin (+) | Animancer / Gauranlen / Biosoother |
| VRE_Insector (+) | Bugmancer |
| WVC_Beholdkind | Oculist, Fleshshaper |
| WVC_Bloodeater | Hemosage |
| WVC_CatDeity | Mindbender, Necropath |
| WVC_Ferrkind | Technomancer, Runesmith |
| WVC_Fleshkind | Fleshshaper, Oculist |
| WVC_Leper | Biohazard |
| WVC_Lilith | Archon, Mindbender, Empath |
| WVC_Mechamata | Protector, Technomancer |
| WVC_Mergekin | Fleshshaper |
| WVC_Nociokin | Technomancer, Staticlord |
| WVC_Overrider | Chronopath, Fleshshaper |
| WVC_Reaperkind | Necropath, Protector |
| WVC_Ripperkind | Ravager, Protector |
| WVC_Rustkind | Technomancer, Biohazard |
| WVC_Sandycat | Deadlife, Necropath |
| WVC_Shadoweater | Nightstalker, Fleshshaper |
