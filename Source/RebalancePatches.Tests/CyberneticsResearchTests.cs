using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimTestRedux;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsResearchTests
    {
        private const string CoreKey = "cyberneticsresearch.core";
        private const string BodyKey = "cyberneticsresearch.body";
        private const string ModulesKey = "cyberneticsresearch.modules";
        private const string CyberbrainsKey = "cyberneticsresearch.cyberbrains";
        private const string MindKey = "cyberneticsresearch.mind";
        private const string CapstonesKey = "cyberneticsresearch.capstones";
        private const string RetireKey = "cyberneticsresearch.retire";

        private const string Ascension = "RBP_CybSyntheticAscension";
        private const string Symbiotic = "RBP_CybSymbioticIntegration";

        private const string TabName = "RBP_CyberneticsTab";
        private const string RootName = "RBP_CybSurgicalImplantation";

        private const string BodyProxy = "RBP_CybBionicOrgans";
        private const string ModulesProxy = "RBP_CybModularCybernetics";
        private const string CyberbrainsProxy = "RBP_CybBasicCyberbrain";
        private const string MindProxy = "RBP_CybEchoNeuralRegulation";

        // Old tabs, by defName as their owners declare them.
        private const string EpoeTab = "EPOE";
        private const string LtsTab = "LTS";
        private const string LtsAnomalyTab = "LTS_Anomaly";
        private const string GitsTab = "gitsResearch";
        private const string AcTab = "AC_AlteredCarbon";
        private const string Ads2Tab = "ADogSaid2";

        // packageIds this project has no Ids constant for.
        private const string EpoeRoyaltyId = "vat.epoeforkedroyalty";
        private const string Ads2Id = "sambucher.adogsaidanimalprosthetics2";

        // ============================== A. Tab and root ==============================

        [Test]
        public static void TabAndRootExist()
        {
            if (!Check.Ready(CoreKey))
                return;

            Check.Soft(DefDatabase<ResearchTabDef>.GetNamedSilentFail(TabName) != null,
                $"{TabName} not found - CyberneticsResearch.xml did not apply, so every other node in " +
                "this overhaul is homeless");

            ResearchProjectDef root = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(RootName);
            if (Check.Soft(root != null, $"{RootName} not found - the install-surgery root is missing"))
            {
                Check.Soft(root.techLevel == TechLevel.Industrial,
                    $"{RootName}.techLevel is {root.techLevel}, expected Industrial");
                Check.Soft(root.tab != null && root.tab.defName == TabName,
                    $"{RootName} sits on tab '{TabDefName(root)}', expected {TabName}");
                AssertPrerequisitesResolve(root);
            }

            Check.SoftResult();
        }

        // ====================== B. Every authored node sits on the tab ======================

        [Test]
        public static void BodyNodesSitOnTheTab()
        {
            if (!Check.Ready(BodyKey) || !TabLoaded(BodyKey))
                return;

            foreach (string node in new[]
            {
                "RBP_CybSurrogateOrgans", "RBP_CybBionicOrgans", "RBP_CybBionicSenses",
                "RBP_CybAdvancedLimbs", "RBP_CybExtraLimbs", "RBP_CybThoracicFrame",
                "RBP_CybAdvancedOrgans", "RBP_CybAdvancedSenses", "RBP_CybArchotechCybernetics",
                "RBP_CybVitalRibs", "RBP_CybClimateRibs", "RBP_CybOrganOptimizers",
            })
                AssertNodeOnTab(node, "body trunk");

            // Core, so always present: an absence here is a deleted vanilla def, not a missing mod.
            foreach (string repurposed in new[] { "Prosthetics", "Bionics" })
                AssertNodeOnTab(repurposed, "body trunk (repurposed vanilla node)");

            Check.SoftResult();
        }

        [Test]
        public static void ModuleNodesSitOnTheTab()
        {
            if (!Check.Ready(ModulesKey) || !TabLoaded(ModulesKey))
                return;

            foreach (string node in new[]
            {
                "RBP_CybModularCybernetics", "RBP_CybIntegralMelee", "RBP_CybIntegralRanged",
                "RBP_CybLocomotionSystems", "RBP_CybMetabolicSystems", "RBP_CybSkillChips",
                "RBP_CybUtilitySystems", "RBP_CybDefensiveSystems", "RBP_CybFieldManipulation",
                "RBP_CybUltratechWeaponModules",
            })
                AssertNodeOnTab(node, "module trunk");

            Check.SoftResult();
        }

        [Test]
        public static void CyberbrainNodesSitOnTheTab()
        {
            if (!Check.Ready(CyberbrainsKey, Ids.GiTS) || !TabLoaded(CyberbrainsKey))
                return;

            foreach (string node in new[]
            {
                "RBP_CybNeuralInterface", "RBP_CybBasicCyberbrain", "RBP_CybEnhancedCyberbrain",
                "RBP_CybNeuralCoprocessors", "RBP_CybNeuralWellBeing",
                "RBP_CybCivisCyberbrains", "RBP_CybCivisAdvanced", "RBP_CybCivisPX7",
                "RBP_CybAegisCyberbrains", "RBP_CybAegisAdvanced", "RBP_CybAegisHADES",
            })
                AssertNodeOnTab(node, "cyberbrain trunk");

            Check.SoftResult();
        }

        [Test]
        public static void MachineAssistChipUnlocksWithTheSharedComponents()
        {
            if (!Check.Ready(CyberbrainsKey, Ids.GiTS, Ids.EPOEForked) || !TabLoaded(CyberbrainsKey))
                return;

            ThingDef chip = DefDatabase<ThingDef>.GetNamedSilentFail("EPOE_MAAI_Chip");
            if (!Check.Soft(chip != null, "EPOE_MAAI_Chip not found although EPOE-Forked is active"))
            {
                Check.SoftResult();
                return;
            }

            ResearchProjectDef gate = Check.RecipePrereq(chip);
            if (!Check.Soft(gate != null,
                    "EPOE_MAAI_Chip has no craft gate at all - the repoint matched nothing"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(gate.defName == "RBP_CybNeuralInterface",
                $"EPOE_MAAI_Chip unlocks at '{gate.defName}', expected RBP_CybNeuralInterface - the " +
                "node the micromachines already unlock");

            foreach (string node in new[]
            {
                "RBP_CybBasicCyberbrain", "RBP_CybCivisPX7", "RBP_CybAegisHADES",
                "RBP_CybEchoCyberbrains",
            })
            {
                ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(node);
                if (project == null)
                    continue;
                Check.Soft(IsAncestorOf(gate, project),
                    $"{node} unlocks cyberbrains built out of MA-AI chips, but '{gate.defName}' is not " +
                    "in its prerequisite ancestry - a colony that walked this lane cannot make the chip");
            }

            Check.SoftResult();
        }

        private static bool IsAncestorOf(ResearchProjectDef ancestor, ResearchProjectDef project)
        {
            var seen = new HashSet<string>();
            var queue = new Queue<ResearchProjectDef>();
            queue.Enqueue(project);
            while (queue.Count > 0)
            {
                ResearchProjectDef current = queue.Dequeue();
                if (current?.prerequisites == null || !seen.Add(current.defName))
                    continue;
                foreach (ResearchProjectDef prereq in current.prerequisites)
                {
                    if (prereq == null)
                        continue;
                    if (prereq.defName == ancestor.defName)
                        return true;
                    queue.Enqueue(prereq);
                }
            }
            return false;
        }

        [Test]
        public static void MindNodesSitOnTheTab()
        {
            if (!Check.Ready(MindKey) || !TabLoaded(MindKey))
                return;

            foreach (string node in new[]
            {
                "RBP_CybEchoNeuralRegulation", "RBP_CybEchoPsyfocusSystems", "RBP_CybEchoCyberbrains",
                "RBP_CybEchoOracle", "RBP_CybEchoSensitivityAmplifier", "RBP_CybEchoFusionCore",
                "RBP_CybEchoSuppression",
            })
                AssertNodeOnTab(node, "mind trunk (Echo lane)");

            if (ModsConfig.IsActive(Ids.AlteredCarbon))
            {
                AssertNodeOnTab("RBP_CybCorticalStacks", "mind trunk (digitisation)");
                foreach (string node in new[]
                {
                    "AC_NeuralDigitalization", "AC_NeuralEditing", "AC_NeuralCasting",
                    "AC_SkilltrainerProduction", "AC_PsytrainerProduction", "AC_SleeveGestation",
                })
                    AssertNodeOnTab(node, "mind trunk (repurposed AC2 def)");
            }
            else
            {
                Check.Note("MindNodesSitOnTheTab: Altered Carbon 2 absent - the digitisation lane " +
                    "(RBP_CybCorticalStacks and the six repurposed AC_* defs) was not asserted.");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CapstoneNodesSitOnTheTab()
        {
            if (!Check.Ready(CapstonesKey) || !TabLoaded(CapstonesKey))
                return;

            foreach (string node in new[] { Ascension, Symbiotic })
            {
                AssertNodeOnTab(node, "capstones");
                ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(node);
                if (project == null)
                    continue;
                Check.Soft(project.baseCost == 9000f,
                    $"{node} costs {project.baseCost}, expected 9000 - both capstones sit at the same " +
                    "depth, so an asymmetry here is a thumb on the scale for one ending");
                Check.Soft(project.techLevel == TechLevel.Ultra,
                    $"{node} is {project.techLevel}, expected Ultra");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CapstonesShareOnePrerequisitePair()
        {
            if (!Check.Ready(CapstonesKey) || !TabLoaded(CapstonesKey))
                return;

            ResearchProjectDef ascension = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Ascension);
            ResearchProjectDef symbiotic = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Symbiotic);
            if (!Check.Soft(ascension != null && symbiotic != null,
                    "one or both capstone nodes are missing; nothing else here could be asserted"))
            {
                Check.SoftResult();
                return;
            }

            foreach (string anchor in new[] { "RBP_CybArchotechCybernetics", "RBP_CybCorticalStacks" })
            {
                if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(anchor) == null)
                {
                    Check.Note($"{anchor} absent in this configuration, so neither capstone was " +
                        "checked against it");
                    continue;
                }
                foreach (ResearchProjectDef capstone in new[] { ascension, symbiotic })
                    Check.Soft(Check.ContainsResearch(capstone.prerequisites, anchor),
                        $"{capstone.defName} does not list {anchor}; it lists " +
                        $"{PrereqNames(capstone)}. Both capstones must sit where the two halves of " +
                        "the tree meet, or one ending is reachable without the other's price");
            }

            Check.Soft(!Check.ContainsResearch(ascension.prerequisites, Symbiotic)
                       && !Check.ContainsResearch(symbiotic.prerequisites, Ascension),
                "one capstone is a prerequisite of the other - they are opposite endings, not a ladder");

            foreach (ResearchProjectDef capstone in new[] { ascension, symbiotic })
            {
                bool anchored = Check.ContainsResearch(capstone.prerequisites, "RBP_CybArchotechCybernetics")
                                || Check.ContainsResearch(capstone.prerequisites, "RBP_CybCorticalStacks");
                bool onRoot = Check.ContainsResearch(capstone.prerequisites, RootName);
                Check.Soft(onRoot != anchored,
                    anchored
                        ? $"{capstone.defName} still lists {RootName} alongside a real anchor " +
                          $"({PrereqNames(capstone)}); the floor should have come out from under it"
                        : $"{capstone.defName} has no trunk anchor and no floor either - it lists " +
                          $"{PrereqNames(capstone)}, which leaves the node unreachable");
            }

            Check.SoftResult();
        }

        [Test]
        public static void SymbioticIntegrationTakesArchogeneticsOnlyWhenItExists()
        {
            if (!Check.Ready(CapstonesKey) || !TabLoaded(CapstonesKey))
                return;

            ResearchProjectDef symbiotic = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Symbiotic);
            if (!Check.Soft(symbiotic != null, $"{Symbiotic} not found"))
            {
                Check.SoftResult();
                return;
            }

            bool archogeneticsExists =
                DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Archogenetics") != null;
            bool named = Check.ContainsResearch(symbiotic.prerequisites, "Archogenetics");
            Check.Note($"Archogenetics present: {archogeneticsExists}; named by {Symbiotic}: {named}");

            Check.Soft(named == archogeneticsExists,
                archogeneticsExists
                    ? $"{Symbiotic} does not require Archogenetics although that project exists; it " +
                      $"lists {PrereqNames(symbiotic)}. The flesh ending is where cybernetics meets " +
                      "the gene tree"
                    : $"{Symbiotic} requires Archogenetics, which does not exist in this modlist - " +
                      "the node is unreachable and the prerequisite dangles");

            Check.SoftResult();
        }

        [Test]
        public static void AscensionAdvertisesThePsychicResonator()
        {
            if (!Check.Ready(CapstonesKey, Ids.VREAndroid) || !TabLoaded(CapstonesKey))
                return;

            ResearchProjectDef ascension = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Ascension);
            if (!Check.Soft(ascension != null, $"{Ascension} not found"))
            {
                Check.SoftResult();
                return;
            }

            Def resonator = GenDefDatabase.GetDefSilentFail(typeof(GeneDef), "RBP_PsychicResonator");
            if (resonator == null)
            {
                Type androidGene = GenTypes.GetTypeInAnyAssembly("VREAndroids.AndroidGeneDef");
                if (androidGene != null)
                    resonator = GenDefDatabase.GetDefSilentFail(androidGene, "RBP_PsychicResonator");
            }
            if (resonator == null)
            {
                Check.Note("RBP_PsychicResonator absent (cybernetics.ascension off), hyperlink not asserted");
                Check.SoftResult();
                return;
            }

            bool linked = ascension.descriptionHyperlinks != null
                && ascension.descriptionHyperlinks.Any(link => link.def == resonator);
            Check.Soft(linked,
                $"{Ascension} does not hyperlink RBP_PsychicResonator. The gene is gated on this " +
                "project in code, so the card is the only place a player can find out it exists");

            Check.SoftResult();
        }

        [Test]
        public static void CapstoneDescriptionsQuoteTheRealNumbers()
        {
            if (!Check.Ready(CapstonesKey) || !TabLoaded(CapstonesKey))
                return;

            ResearchProjectDef ascension = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Ascension);
            ResearchProjectDef symbiotic = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Symbiotic);
            if (!Check.Soft(ascension != null && symbiotic != null, "one or both capstone nodes are missing"))
            {
                Check.SoftResult();
                return;
            }

            AssertWeaveLadderIsQuoted(symbiotic);
            AssertFrameCapacityIsQuoted(symbiotic);
            AssertAscensionChainIsQuoted(ascension);

            Check.SoftResult();
        }

        private static void AssertWeaveLadderIsQuoted(ResearchProjectDef symbiotic)
        {
            HediffDef weave = DefDatabase<HediffDef>.GetNamedSilentFail("RBP_GeneDivergenceImplantHediff");
            if (weave?.stages == null || weave.stages.Count < 3)
            {
                Check.Note("apotheosis weave absent (cybernetics.livingframe off), so the stat " +
                    "ladder quoted on the symbiotic integration card was not verified");
                return;
            }

            HediffStage floor = weave.stages[0];
            HediffStage top = weave.stages[weave.stages.Count - 1];

            AssertQuoted(symbiotic, "Injury Healing Factor",
                Range(StatOffsetOf(floor, "InjuryHealingFactor"), StatOffsetOf(top, "InjuryHealingFactor")));
            AssertQuoted(symbiotic, "Immunity Gain Speed",
                Range(StatOffsetOf(floor, "ImmunityGainSpeed"), StatOffsetOf(top, "ImmunityGainSpeed")));
            AssertQuoted(symbiotic, "Toxic Resistance",
                Range(StatOffsetOf(floor, "ToxicResistance"), StatOffsetOf(top, "ToxicResistance")));
            AssertQuoted(symbiotic, "Rest Rate Multiplier",
                Range(StatOffsetOf(floor, "RestRateMultiplier"), StatOffsetOf(top, "RestRateMultiplier")));
            AssertQuoted(symbiotic, "Pain",
                Range(PainOffsetOf(floor), PainOffsetOf(top)));

            float floorBlood = CapOffsetOf(floor, "BloodPumping");
            float topBlood = CapOffsetOf(top, "BloodPumping");
            foreach (string capacity in new[] { "BloodFiltration", "Breathing" })
                Check.Soft(CapOffsetOf(floor, capacity) == floorBlood
                           && CapOffsetOf(top, capacity) == topBlood,
                    $"{capacity} no longer tracks BloodPumping ({CapOffsetOf(floor, capacity)}/" +
                    $"{CapOffsetOf(top, capacity)} against {floorBlood}/{topBlood}), but the capstone " +
                    "description states all three on one line");
            AssertQuoted(symbiotic, "Blood Pumping, Blood Filtration, Breathing",
                Range(floorBlood, topBlood));

            HediffStage firstConscious = weave.stages.FirstOrDefault(stage => CapOffsetOf(stage, "Consciousness") > 0f);
            if (firstConscious == null)
                Check.Soft(false, "no weave stage grants Consciousness, but the capstone description quotes it");
            else
                AssertQuoted(symbiotic, "Consciousness",
                    Range(CapOffsetOf(firstConscious, "Consciousness"), CapOffsetOf(top, "Consciousness")));
        }

        private static void AssertFrameCapacityIsQuoted(ResearchProjectDef symbiotic)
        {
            float? living = TorsoSlots("RBP_LivingFrameHediff");
            float? archotech = TorsoSlots("RBP_ThoracicFrameArchotechHediff");
            if (!living.HasValue || !archotech.HasValue)
            {
                Check.Note("living frame or archotech thoracic frame slots unreadable (EBSG Framework " +
                    "or a content toggle off), so the slot counts on the card were not verified");
                return;
            }

            Check.Soft(symbiotic.description != null
                       && symbiotic.description.Contains(
                           $"Living frame: {living.Value:0} torso module slots, against {archotech.Value:0} on the archotech frame"),
                $"the symbiotic integration card does not state {living.Value:0} slots against " +
                $"{archotech.Value:0}; the frames now carry {living.Value:0} and {archotech.Value:0}");
        }

        private static void AssertAscensionChainIsQuoted(ResearchProjectDef ascension)
        {
            ThingDef serum = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_NeuroformSerum");
            ThingDef kit = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_AndroidConversionKit");
            RecipeDef surgery = DefDatabase<RecipeDef>.GetNamedSilentFail("RBP_ConvertToAndroid");
            if (serum == null || kit == null || surgery == null)
            {
                Check.Note("the conversion chain is absent (cybernetics.androidconversion off, or no " +
                    "android race), so the work amounts on the ascension card were not verified");
                return;
            }

            AssertContains(ascension, $"Neuroform serum: {Thousands(Check.StatBase(serum, "WorkToMake"))} work");
            AssertContains(ascension, $"Conversion kit: {Thousands(Check.StatBase(kit, "WorkToMake"))} work");
            AssertContains(ascension, $"Ascension surgery: {Thousands(surgery.workAmount)} work");

            HediffDef recovery = DefDatabase<HediffDef>.GetNamedSilentFail("RBP_SyntheticAscensionRecovery");
            if (recovery?.stages == null || recovery.stages.Count == 0)
            {
                Check.Note("ascension recovery hediff absent, so the downtime line was not verified");
                return;
            }

            PawnCapacityModifier moving = recovery.stages[0].capMods?
                .FirstOrDefault(mod => mod.capacity == PawnCapacityDefOf.Moving);
            IntRange? duration = DisappearsAfter(recovery);
            if (moving == null || !duration.HasValue)
            {
                Check.Note("recovery downtime is not readable off the hediff, so that line was not verified");
                return;
            }

            AssertContains(ascension,
                $"Moving and Manipulation at {moving.postFactor * 100f:0}% for " +
                $"{duration.Value.min / 60000f:0.#} to {duration.Value.max / 60000f:0.#} days");
        }

        private static void AssertQuoted(ResearchProjectDef project, string label, string value) =>
            AssertContains(project, $"{label}: {value}");

        private static void AssertContains(ResearchProjectDef project, string expected)
        {
            Check.Soft(project.description != null && project.description.Contains(expected),
                $"{project.defName}'s description does not contain \"{expected}\" - the text and the " +
                "defs disagree, so the card is telling the player a number that is no longer true");
        }

        /// <summary>"+35% to +115%", built from the defs rather than typed.</summary>
        private static string Range(float from, float to) => $"{Percent(from)} to {Percent(to)}";

        private static string Percent(float value) =>
            (value < 0f ? "-" : "+") + Mathf.RoundToInt(Mathf.Abs(value) * 100f) + "%";

        private static string Thousands(float? value) =>
            value.HasValue ? Thousands(value.Value) : "?";

        private static string Thousands(float value) =>
            Mathf.RoundToInt(value).ToString("N0", CultureInfo.InvariantCulture);

        private static float StatOffsetOf(HediffStage stage, string statName)
        {
            StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(statName);
            if (stage?.statOffsets == null || stat == null)
                return 0f;
            return stage.statOffsets.GetStatOffsetFromList(stat);
        }

        private static float CapOffsetOf(HediffStage stage, string capacityName)
        {
            if (stage?.capMods == null)
                return 0f;
            foreach (PawnCapacityModifier mod in stage.capMods)
                if (mod?.capacity != null && mod.capacity.defName == capacityName)
                    return mod.offset;
            return 0f;
        }

        /// <summary>A painFactor of 0.90 is what the card states as -10%.</summary>
        private static float PainOffsetOf(HediffStage stage) => (stage?.painFactor ?? 1f) - 1f;

        private static float? TorsoSlots(string hediffName)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (hediff?.comps == null)
                return null;
            foreach (HediffCompProperties props in hediff.comps)
            {
                if (props == null || !props.GetType().Name.Contains("HediffCompProperties_Modular"))
                    continue;
                if (!(Check.Field(props, "slots") is IEnumerable slots))
                    continue;
                foreach (object slot in slots)
                    if ((Check.Field(slot, "slotID") as string) == "RBP_TorsoSlot")
                        return Convert.ToSingle(Check.Field(slot, "capacity"));
            }
            return null;
        }

        private static IntRange? DisappearsAfter(HediffDef hediff)
        {
            if (hediff.comps == null)
                return null;
            foreach (HediffCompProperties props in hediff.comps)
                if (props is HediffCompProperties_Disappears disappears)
                    return disappears.disappearsAfterTicks;
            return null;
        }

        // ============================== B3. Techprints ==============================

        [Test]
        public static void TechprintsSitOnlyOnNodesNothingDependsOn()
        {
            if (!Check.Ready(CyberbrainsKey, Ids.Royalty) || !TabLoaded(CyberbrainsKey))
                return;

            var expected = new HashSet<string>
            {
                "RBP_CybCivisPX7", "RBP_CybAegisHADES", "RBP_CybEchoOracle",
                "RBP_CybEchoFusionCore", "RBP_CybEchoSuppression",
                "RBP_CybUltratechWeaponModules",
            };

            var printed = new List<string>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                if (project.defName == null || !project.defName.StartsWith("RBP_Cyb"))
                    continue;
                if (project.techprintCount > 0)
                    printed.Add(project.defName);
            }
            Check.Note($"techprinted nodes: {(printed.Count == 0 ? "none" : string.Join(", ", printed.ToArray()))}");

            foreach (string node in printed)
            {
                var dependants = new List<string>();
                foreach (ResearchProjectDef other in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                    if (other.prerequisites != null && Check.ContainsResearch(other.prerequisites, node))
                        dependants.Add(other.defName);
                Check.Soft(dependants.Count == 0,
                    $"{node} carries a techprint and {dependants.Count} project(s) require it " +
                    $"({string.Join(", ", dependants.ToArray())}). A techprint may only gate a dead " +
                    "end - here it stands between a colony and the rest of the tree");
            }

            foreach (string node in expected)
            {
                ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(node);
                if (project == null)
                {
                    Check.Note($"{node} absent in this configuration, techprint not asserted");
                    continue;
                }
                if (!Check.Soft(project.techprintCount > 0,
                        $"{node} carries no techprint; it is one of the seven nodes meant to need one"))
                    continue;
                Check.Soft(project.techprintCount == 1
                           && Math.Abs(project.techprintMarketValue - 2000f) < 0.5f,
                    $"{node} asks for {project.techprintCount} print(s) at " +
                    $"{project.techprintMarketValue:0}, expected 1 at 2000 - Royalty's own ultratech " +
                    "implant weight, which is the tier these sit at");
                Check.Soft(project.heldByFactionCategoryTags != null
                           && project.heldByFactionCategoryTags.Contains("Empire"),
                    $"{node}'s techprint is held by " +
                    $"{(project.heldByFactionCategoryTags == null ? "nobody" : string.Join(", ", project.heldByFactionCategoryTags.ToArray()))}; " +
                    "Empire is the only category any faction in this ecosystem carries, so any other " +
                    "value makes the print unobtainable and the project unfinishable");
            }

            foreach (string node in printed)
                Check.Soft(expected.Contains(node),
                    $"{node} has grown a techprint that nothing in this project asked for");

            Check.SoftResult();
        }

        [Test]
        public static void TechprintsExistOnlyWithAFactionToBuyThemFrom()
        {
            if (!Check.Ready(CyberbrainsKey) || !TabLoaded(CyberbrainsKey))
                return;

            var printed = new List<string>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                if (project.defName == null || !project.defName.StartsWith("RBP_Cyb"))
                    continue;
                if (project.techprintCount > 0)
                    printed.Add(project.defName);
            }

            if (ModsConfig.IsActive(Ids.Royalty))
                Check.Soft(printed.Count > 0,
                    "Royalty is loaded but no RBP_Cyb node carries a techprint - the Royalty-gated " +
                    "print operations did not fire, so the whole feature is silently off");
            else
                Check.Soft(printed.Count == 0,
                    $"no Royalty is loaded, so no Empire exists to sell a techprint, yet " +
                    $"{printed.Count} node(s) demand one and cannot be finished: {Join(printed)}");

            Check.SoftResult();
        }

        private static string Join(List<string> items) =>
            items.Count == 0 ? "none" : string.Join(", ", items.ToArray());

        // ========================= B4. The Anomaly bridge =========================

        private static readonly string[] AnomalyImplantProjects =
        {
            "LTS_AdvancedGhoulEnhancements", "DreadheartRegeneration", "PsychicAgonizer",
            "BlacksightDevice", "MetalhorrorSymbiont", "HyperalkalaicArchites", "PsychicBeguiler",
            "HulkificationSurgery", "HyperAdrenalineGland", "DefensiveImpaler", "DeadlifeCoil",
            "SlayersCollar", "StalkerizingSurgery", "NullificationLobotomy", "DroneLobotomy",
            "PsychicSustainer", "AnalgesialLobotomy",
        };

        [Test]
        public static void AnomalyBridgeGatesTheDarkKnowledgeTier()
        {
            if (!Check.Ready(ModulesKey, Ids.IntegratedImplants, Ids.Anomaly) || !TabLoaded(ModulesKey))
                return;

            ResearchProjectDef bridge = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("RBP_CybAnomalousGrafting");
            if (!Check.Soft(bridge != null,
                    "RBP_CybAnomalousGrafting not found although Integrated Implants and Anomaly are " +
                    "both active - the bridge node was not created"))
            {
                Check.SoftResult();
                return;
            }

            AssertNodeOnTab("RBP_CybAnomalousGrafting", "Anomaly bridge");
            Check.Soft(bridge.techLevel == TechLevel.Industrial,
                $"the bridge is {bridge.techLevel}, expected Industrial - it hangs off Prosthetics " +
                "and opens the horror tier early on the main ladder");
            Check.Soft(bridge.knowledgeCategory == null,
                $"the bridge declares knowledgeCategory {bridge.knowledgeCategory?.defName}; it must be " +
                "an ordinary research project, or it cannot be completed to open the tier");
            Check.Soft(Check.ContainsResearch(bridge.prerequisites, "Prosthetics"),
                "the bridge does not hang off Prosthetics; that edge is what places it early on the ladder");

            int checkedProjects = 0;
            foreach (string name in AnomalyImplantProjects)
            {
                ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(name);
                if (project == null)
                {
                    Check.Note($"{name} absent - renamed upstream, so its bridge gate is unverified");
                    continue;
                }
                checkedProjects++;

                Check.Soft(Check.ContainsResearch(project.prerequisites, "RBP_CybAnomalousGrafting"),
                    $"{name} does not require the bridge, so the horror tier it belongs to is still " +
                    "reachable with no cybernetics research at all");

                Check.Soft(project.knowledgeCategory != null,
                    $"{name} is no longer a knowledge project; the bridge was meant to add a gate, " +
                    "not convert it into ordinary research");
                Check.Soft(project.prerequisites != null && project.prerequisites.Count >= 2,
                    $"{name} lists {(project.prerequisites?.Count ?? 0)} prerequisite(s); it should " +
                    "carry its original dark-knowledge gate and the bridge, so at least two");

                AssertPrerequisitesResolve(project);
            }

            Check.Soft(checkedProjects > 0,
                "none of the seventeen Anomaly implant projects were found although both mods are " +
                "active - the whole set was renamed, and the bridge now gates nothing");
            Check.Note($"bridge gate verified on {checkedProjects} of {AnomalyImplantProjects.Length} projects");
            Check.SoftResult();
        }

        // Research gates crafting, not fitting.

        [Test]
        public static void SurgeriesGateOnTheRootNotOnCraftingTiers()
        {
            if (!Check.Ready(CoreKey) || !TabLoaded(CoreKey))
                return;

            var craftingTier = new HashSet<string>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                if (project.tab != null && project.tab.defName == TabName && project.defName != RootName)
                    craftingTier.Add(project.defName);

            // If this set were empty the sweep below would pass without testing anything.
            Check.Soft(craftingTier.Count > 0,
                $"no crafting-tier projects found on {TabName} - every trunk toggle is off, so the " +
                "D26 sweep would have passed vacuously");
            Check.Note($"D26 sweep: {craftingTier.Count} crafting-tier project(s) on {TabName}.");

            var offenders = new List<string>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.addsHediff == null && recipe.removesHediff == null)
                    continue;
                if (recipe.researchPrerequisite != null && craftingTier.Contains(recipe.researchPrerequisite.defName))
                    offenders.Add($"{recipe.defName} -> {recipe.researchPrerequisite.defName}");
                if (recipe.researchPrerequisites != null)
                    foreach (ResearchProjectDef prereq in recipe.researchPrerequisites)
                        if (prereq != null && craftingTier.Contains(prereq.defName))
                            offenders.Add($"{recipe.defName} -> {prereq.defName}");
            }

            Check.Soft(offenders.Count == 0,
                $"{offenders.Count} surgical recipe(s) are gated on a crafting-tier project instead of " +
                $"{RootName} - D26/D26b says research gates crafting, never fitting: " +
                string.Join(", ", offenders.ToArray()));

            AssertSurgeryOnRoot("InstallSurrogateKidney", Ids.EPOEForked);
            AssertSurgeryOnRoot("InstallAIPersonaCore", Ids.EPOEForked);
            AssertSurgeryOnRoot("InstallScytherBlade", Ids.EPOEForked);
            AssertSurgeryOnRoot("gitsInstallImmunityNanites", Ids.GiTS);
            AssertSurgeryOnRoot("LTS_InstallSubdermalArmour", Ids.IntegratedImplants);
            AssertSurgeryOnRoot("LTS_InstallArchotechHeart", Ids.IntegratedImplants);
            AssertSurgeryOnRoot("InstallSkeletalBracing", Ids.IntegratedImplants);
            AssertSurgeryOnRoot("Install_PI_HOPS", Ids.PsychicImplants);
            AssertSurgeryOnRoot("Install_PI_PFC", Ids.PsychicImplants);
            AssertSurgeryOnRoot("AC_InstallNeuralStack", Ids.AlteredCarbon);
            AssertSurgeryOnRoot("AC_InstallMentalFuse", Ids.AlteredCarbon);

            RecipeDef animalChips = DefDatabase<RecipeDef>.GetNamedSilentFail("RBP_InstallSkillChipAnimals");
            if (animalChips != null)
                Check.Soft(DescribeGate(animalChips).Contains(RootName),
                    $"RBP_InstallSkillChipAnimals resolves to '{DescribeGate(animalChips)}', expected " +
                    $"{RootName} - it was the one surgery we authored with no gate at all (D26c)");

            if (ModsConfig.IsActive(Ids.GiTS))
            {
                var wrong = new List<string>();
                int found = 0;
                foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                {
                    if (recipe.defName == null || !recipe.defName.StartsWith("gitsInstall")
                        || !recipe.defName.EndsWith("Cyberbrain"))
                        continue;
                    found++;
                    if (recipe.researchPrerequisite == null || recipe.researchPrerequisite.defName != RootName)
                        wrong.Add($"{recipe.defName}={recipe.researchPrerequisite?.defName ?? "ungated"}");
                }
                Check.Soft(found > 0,
                    "no gitsInstall*Cyberbrain recipes found although GiTS is active - renamed upstream?");
                Check.Soft(wrong.Count == 0,
                    $"{wrong.Count} cyberbrain install surgery(ies) do not resolve to {RootName}: " +
                    string.Join(", ", wrong.ToArray()));
            }

            if (ModsConfig.IsActive(Ads2Id) && ModsConfig.IsActive(Ids.EPOEForked))
            {
                RecipeDef animal = DefDatabase<RecipeDef>.GetNamedSilentFail("InstallBrainStimulatorAnimal");
                if (Check.Soft(animal != null,
                        "InstallBrainStimulatorAnimal not found although A Dog Said 2 and EPOE-Forked " +
                        "are both active - renamed upstream, so the animal-line carve-out is unverified"))
                    Check.Soft(animal.researchPrerequisite != null
                            && animal.researchPrerequisite.defName == "AnimalBionics",
                        "InstallBrainStimulatorAnimal resolves to " +
                        $"'{animal.researchPrerequisite?.defName ?? "null"}', expected AnimalBionics - " +
                        "animal surgery must stay on ADS2's own line, not on the human implantation root");
            }

            Check.SoftResult();
        }

        [Test]
        public static void ThoracicFramesGateOnTheirOwnNode()
        {
            if (!Check.Ready(BodyKey) || !TabLoaded(BodyKey))
                return;

            const string Node = "RBP_CybThoracicFrame";

            // The frames come from cybernetics.thoracicframe, which is independent of this toggle.
            ThingDef bionic = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_ThoracicFrameBionic");
            if (bionic == null)
            {
                Check.Note("ThoracicFramesGateOnTheirOwnNode: RBP_ThoracicFrameBionic absent - " +
                    "cybernetics.thoracicframe is off, so there are no frames to repoint.");
                Check.SoftResult();
                return;
            }

            ResearchProjectDef node = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Node);
            if (!Check.Soft(node != null,
                    $"{Node} not found although the body trunk ran - the frames have nowhere to land"))
            {
                Check.SoftResult();
                return;
            }

            AssertCraftGate(bionic, Node);

            ThingDef advanced = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_ThoracicFrameAdvanced");
            if (Check.Soft(advanced != null,
                    "RBP_ThoracicFrameAdvanced not found although RBP_ThoracicFrameBionic is present " +
                    "- one operation authors all three frames, so this cannot happen by configuration"))
                AssertCraftGate(advanced, Node);

            ThingDef archotech = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_ThoracicFrameArchotech");
            if (Check.Soft(archotech != null,
                    "RBP_ThoracicFrameArchotech not found although the other two frames are present"))
                Check.Soft(archotech.recipeMaker == null,
                    "RBP_ThoracicFrameArchotech has a recipeMaker gated on " +
                    $"'{DescribeCraftGate(archotech)}' - the archotech frame is acquisition-only, so " +
                    "it should carry no craft recipe to gate at all");

            var gated = new List<string>();
            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefsListForReading)
                if (thing.recipeMaker != null && CraftGateNames(thing).Contains(Node))
                    gated.Add(thing.defName);

            Check.Soft(gated.Count >= 2,
                $"{Node} gates {gated.Count} craftable item(s) ({string.Join(", ", gated.ToArray())}) - " +
                "expected at least the two thoracic frames; a node that gates nothing is a rung the " +
                "player pays for and receives nothing from");
            Check.Note($"{Node} gates: {string.Join(", ", gated.ToArray())}");

            Check.SoftResult();
        }

        [Test]
        public static void EpoeRestorationConsumablesSitWithTheirPrinter()
        {
            if (!Check.Ready(RetireKey, Ids.EPOEForked) || !TabLoaded(RetireKey))
                return;

            if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(BodyProxy) == null)
            {
                Check.Note("EpoeRestorationConsumablesSitWithTheirPrinter: " + BodyProxy + " absent - " +
                    "the body trunk is off, so the repoint is deliberately not applied.");
                Check.SoftResult();
                return;
            }

            foreach (string item in new[]
            {
                "SyntheticSkin", "EPIA_SyntheticBone", "NeuromuscularFramework", "NeurocureFramework",
            })
            {
                ThingDef def = Check.Optional<ThingDef>(item, RetireKey, Ids.EPOEForked);
                if (def == null)
                    continue;
                AssertCraftGate(def, BodyProxy);
            }

            ThingDef printer = Check.Optional<ThingDef>("TableOrgans", RetireKey, Ids.EPOEForked);
            if (printer != null)
                Check.Soft(printer.researchPrerequisites != null
                        && Check.ContainsResearch(printer.researchPrerequisites, BodyProxy),
                    "TableOrgans (synthetic nano printer) no longer resolves to " + BodyProxy +
                    " - the four consumables were moved to sit with it, so they are now on a node " +
                    "that does not unlock the bench that makes them");

            Check.SoftResult();
        }

        [Test]
        public static void TheRootAdvertisesNoManufacturedGoods()
        {
            if (!Check.Ready(CoreKey) || !TabLoaded(CoreKey))
                return;

            var offenders = new List<string>();
            int gated = 0;
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (!GateNames(recipe).Contains(RootName))
                    continue;
                gated++;
                if (recipe.products == null)
                    continue;
                foreach (ThingDefCountClass product in recipe.products)
                    if (product?.thingDef != null)
                        offenders.Add($"{recipe.defName} -> {product.thingDef.defName}");
            }

            // Without this the sweep passes trivially on any profile where the root gates nothing.
            Check.Soft(gated > 0,
                $"no recipe at all is gated on {RootName} - the D26 repoints did not run, so this " +
                "sweep proved nothing");
            Check.Note($"{RootName} gates {gated} recipe(s).");

            Check.Soft(offenders.Count == 0,
                $"{offenders.Count} recipe(s) gated on {RootName} produce an item, which puts that " +
                "item in the root's unlock preview: " + string.Join(", ", offenders.ToArray()) +
                " - the root is a surgery project and must advertise no craftable goods");

            Check.SoftResult();
        }

        [Test]
        public static void PsychicImplantsKeepTheirOwnCraftGate()
        {
            if (!Check.Ready(MindKey, Ids.PsychicImplants) || !TabLoaded(MindKey))
                return;

            const string LaneRoot = "RBP_CybEchoNeuralRegulation";

            ResearchProjectDef laneRoot = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(LaneRoot);
            if (!Check.Soft(laneRoot != null, $"{LaneRoot} not found although the mind trunk ran"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(laneRoot.prerequisites == null
                    || !Check.ContainsResearch(laneRoot.prerequisites, "AdvancedFabrication"),
                $"{LaneRoot}.prerequisites is '{PrereqNames(laneRoot)}' and includes AdvancedFabrication - " +
                "that is the display workaround, which was reverted; it gates the entire Echo lane " +
                "rather than the twelve items that actually need it");

            var missingEcho = new List<string>();
            var lostFabrication = new List<string>();
            int found = 0;
            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (thing.recipeMaker == null || !thing.defName.StartsWith("PI_"))
                    continue;
                List<string> gates = CraftGateNames(thing);
                if (gates.Count == 0)
                    continue;
                found++;
                bool onEcho = false;
                foreach (string gate in gates)
                    if (gate.StartsWith("RBP_CybEcho"))
                        onEcho = true;
                if (!onEcho)
                    missingEcho.Add($"{thing.defName}={string.Join("+", gates.ToArray())}");
                else if (!gates.Contains("AdvancedFabrication"))
                    lostFabrication.Add(thing.defName);
            }

            Check.Soft(found > 0,
                "no PI_ item carries a craft gate although Psychic Implants is active - the mind " +
                "trunk's craft repoints did not run, so this sweep proved nothing");
            Check.Note($"{found} psychic implant(s) carry a craft gate.");

            Check.Soft(missingEcho.Count == 0,
                $"{missingEcho.Count} psychic implant(s) never got an Echo node, so the mind trunk's " +
                "repoints missed them: " + string.Join(", ", missingEcho.ToArray()));

            Check.Soft(lostFabrication.Count == 0,
                $"{lostFabrication.Count} psychic implant(s) lost AdvancedFabrication from their craft " +
                "gate - the mod's own requirement must survive the repoint: " +
                string.Join(", ", lostFabrication.ToArray()));

            Check.SoftResult();
        }

        // ==================== D. The retired research is actually gone ====================

        private sealed class RetireGroup
        {
            public readonly string Label;
            public readonly string ModId;          // null = vanilla Core, always present
            public readonly string[] Guards;       // trunk proxies the removal is conditional on
            public readonly string[] Projects;

            public RetireGroup(string label, string modId, string[] guards, string[] projects)
            {
                Label = label;
                ModId = modId;
                Guards = guards;
                Projects = projects;
            }
        }

        /// <summary>The 62 dissolved projects. Read from CyberneticsResearchZRetire.xml, not guessed.</summary>
        private static readonly RetireGroup[] Retired =
        {
            new RetireGroup("EPOE-Forked, absorbed by the root", Ids.EPOEForked, new[] { RootName },
                new[] { "BrainSurgery", "EPOE_AIPersonaCoreImplant" }),

            new RetireGroup("Integrated Implants, absorbed by the root", Ids.IntegratedImplants, new[] { RootName },
                new[] { "I_ExtraArms", "I_ImplantedExplosives" }),

            new RetireGroup("EPOE-Forked organ, limb and rib line", Ids.EPOEForked, new[] { BodyProxy },
                new[]
                {
                    "SurrogateOrgans", "SyntheticOrgans", "AdvancedBionics", "RibReplacements",
                    "EPOE_OrganicOptimizer", "EPOE_SyntheticRepair", "EPOE_NeurologicalTreatment",
                    "EPOE_MAAIChip",
                }),

            new RetireGroup("EPOE-Forked Royalty add-on", EpoeRoyaltyId, new[] { BodyProxy },
                new[]
                {
                    "EPOE_AdvancedSpecializedLimbs", "EPOE_AdvancedCompactWeaponry",
                    "EPOE_SyntheticOrgansIntegration",
                }),

            new RetireGroup("vanilla Royalty, body content", Ids.Royalty, new[] { BodyProxy },
                new[]
                {
                    "SpecializedLimbs", "CompactWeaponry", "ArtificialMetabolism", "SkinHardening",
                    "HealingFactors", "FleshShaping",
                }),

            new RetireGroup("Integrated Implants faceplates", Ids.IntegratedImplants, new[] { BodyProxy },
                new[] { "LTS_BionicFaceplates" }),

            new RetireGroup("Integrated Implants module tiers", Ids.IntegratedImplants, new[] { ModulesProxy },
                new[]
                {
                    "I_BasicImplants", "I_BasicHormonalImplants", "I_SanguophageImplants",
                    "I_SubdermalArmour", "I_ShoulderTurrets", "I_CranialInsulation", "I_CaptiveControl",
                    "I_Biomimicry", "I_SkillChipPort", "LTS_NeuralHyperacceleration",
                    "LTS_EmergencyImplants", "LTS_FieldManipulationImplants", "LTS_ModularBionics",
                    "LTS_ModularBionicWeaponry", "LTS_ModularBionicWeaponryUltra",
                    "LTS_ModularBionicOptics", "LTS_ModularBionicArms", "LTS_ModularBionicLegs",
                }),

            new RetireGroup("vanilla Royalty, circadian implants", Ids.Royalty, new[] { ModulesProxy },
                new[] { "CircadianInfluence" }),

            new RetireGroup("Altered Carbon 2, Rogian arm blade", Ids.AlteredCarbon, new[] { ModulesProxy },
                new[] { "AC_RogianArmaments" }),

            new RetireGroup("Integrated Implants bionic tails", Ids.IntegratedImplants,
                new[] { BodyProxy, ModulesProxy }, new[] { "I_BionicTails" }),

            new RetireGroup("vanilla Royalty, venom synthesis", Ids.Royalty,
                new[] { BodyProxy, ModulesProxy }, new[] { "VenomSynthesis" }),

            new RetireGroup("EPOE-Forked glitterworld implants", Ids.EPOEForked,
                new[] { BodyProxy, CyberbrainsProxy }, new[] { "EPOE_Glitterworld_Implants" }),

            new RetireGroup("Altered Carbon 2, neural well-being content", Ids.AlteredCarbon,
                new[] { CyberbrainsProxy },
                new[] { "AC_VocalSynthesis", "AC_MentalFortification", "AC_REMEnchanment" }),

            new RetireGroup("Integrated Implants telecommunication", Ids.IntegratedImplants,
                new[] { CyberbrainsProxy }, new[] { "I_TelecommunicationImplants" }),

            new RetireGroup("GiTS tier gates", Ids.GiTS, new[] { CyberbrainsProxy, MindProxy },
                new[]
                {
                    "gitsResearchMicromachines", "gitsResearchNaniteGrafting",
                    "gitsResearchCyberNetIntegration", "gitsResearchBasicCyberization",
                    "gitsResearchEnhancedCyberization", "gitsResearchSpecializedCyberization",
                    "gitsResearchAdvSpecializedCyberization", "gitsResearchCombatCyberization",
                    "gitsResearchExtremeCyberization",
                }),

            new RetireGroup("vanilla Royalty, cognition", Ids.Royalty, new[] { CyberbrainsProxy, MindProxy },
                new[] { "NeuralComputation", "BrainWiring" }),

            new RetireGroup("Integrated Implants eltex augmentation", Ids.IntegratedImplants,
                new[] { MindProxy }, new[] { "I_EltexAugmentation" }),

            new RetireGroup("vanilla Royalty, molecular analysis", Ids.Royalty,
                new[] { BodyProxy, ModulesProxy, MindProxy }, new[] { "MolecularAnalysis" }),
        };

        [Test]
        public static void RetiredResearchIsGone()
        {
            if (!Check.Ready(RetireKey) || !TabLoaded(RetireKey))
                return;

            var shouldBeGone = new HashSet<string>();
            var skipped = new List<string>();

            foreach (RetireGroup group in Retired)
            {
                if (group.ModId != null && !ModsConfig.IsActive(group.ModId))
                {
                    skipped.Add($"{group.Label} (mod '{group.ModId}' not active)");
                    continue;
                }
                string missingGuard = FirstMissingGuard(group.Guards);
                if (missingGuard != null)
                {
                    skipped.Add($"{group.Label} (guard node {missingGuard} absent - that trunk did not run)");
                    continue;
                }

                foreach (string project in group.Projects)
                {
                    shouldBeGone.Add(project);
                    Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(project) == null,
                        $"{project} still present - the retire pass did not delete it ({group.Label})");
                }
            }

            if (skipped.Count > 0)
                Check.Note($"RetiredResearchIsGone: {skipped.Count} group(s) not asserted - " +
                    string.Join("; ", skipped.ToArray()));
            Check.Note($"RetiredResearchIsGone: asserted {shouldBeGone.Count} of the 62 dissolved projects.");

            // If nothing was in scope the sweep below is vacuous, and so is the whole test.
            if (Check.Soft(shouldBeGone.Count > 0,
                    "no retired project was in scope - every source mod is absent or every trunk is " +
                    "off, so this test would have passed without checking anything"))
                SweepForDanglingReferences(shouldBeGone);

            Check.SoftResult();
        }

        // ====================== E. The must-survive defs are still there ======================

        [Test]
        public static void MustSurviveDefsAreStillPresent()
        {
            if (!Check.Ready(RetireKey) || !TabLoaded(RetireKey))
                return;

            foreach (string vanilla in new[] { "Prosthetics", "Bionics" })
                Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(vanilla) != null,
                    $"{vanilla} is gone - it is REPURPOSED IN PLACE as a live node of this tab and " +
                    "must never be retired; every third-party bionic that resolves it is now ungated");

            if (ModsConfig.IsActive(Ids.AlteredCarbon))
            {
                foreach (string bound in new[]
                {
                    "AC_NeuralDigitalization", "AC_NeuralEditing", "AC_NeuralCasting",
                    "AC_SkilltrainerProduction", "AC_PsytrainerProduction",
                })
                    Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(bound) != null,
                        $"{bound} is gone - it is bound in AC_DefOf (AlteredCarbon.dll) and removing " +
                        "it throws at startup; it must be repurposed in place, never deleted");

                // Not in AC_DefOf, but repurposed the same way, and the sleeve lane hangs off it.
                Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail("AC_SleeveGestation") != null,
                    "AC_SleeveGestation is gone - it is repurposed in place as the sleeve gestation node");
            }
            else
            {
                Check.Note("MustSurviveDefsAreStillPresent: Altered Carbon 2 absent - the six " +
                    "repurposed AC_* defs were not asserted.");
            }

            Check.SoftResult();
        }

        // ========================== F. The carve-outs are untouched ==========================

        [Test]
        public static void CarveOutsAreUntouched()
        {
            if (!Check.Ready(RetireKey) || !TabLoaded(RetireKey))
                return;

            if (ModsConfig.IsActive(Ids.AlteredCarbon))
            {
                foreach (string carveOut in new[] { "AC_ChrysalisPoweredArmor", "AC_AdvancedShieldBelt" })
                {
                    ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(carveOut);
                    if (!Check.Soft(project != null,
                            $"{carveOut} is gone - it is an apparel carve-out; deleting it strands the " +
                            "items it unlocks, which no other node gates"))
                        continue;
                    Check.Soft(TabDefName(project) == AcTab,
                        $"{carveOut} sits on tab '{TabDefName(project)}', expected {AcTab} - the AC2 tab " +
                        "consolidation must not re-tab the two apparel carve-outs");
                }
            }
            else
            {
                Check.Note("CarveOutsAreUntouched: Altered Carbon 2 absent - the apparel carve-outs " +
                    "were not asserted.");
            }

            if (ModsConfig.IsActive(Ids.IntegratedImplants))
            {
                List<string> anomaly = ProjectsOnTab(LtsAnomalyTab);
                Check.Soft(anomaly.Count >= 17,
                    $"the {LtsAnomalyTab} tab holds {anomaly.Count} project(s), expected at least 17 - " +
                    "the dark-knowledge tab is untouched by this overhaul and must keep its projects");
                foreach (string member in new[] { "HulkificationSurgery", "DeadlifeCoil", "PsychicSustainer" })
                {
                    ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(member);
                    if (project != null)
                        Check.Soft(TabDefName(project) == LtsAnomalyTab,
                            $"{member} sits on tab '{TabDefName(project)}', expected {LtsAnomalyTab} - " +
                            "the dark-knowledge projects keep their own tab and economy");
                }
            }
            else
            {
                Check.Note("CarveOutsAreUntouched: Integrated Implants absent - the LTS_Anomaly tab " +
                    "was not asserted.");
            }

            if (ModsConfig.IsActive(Ads2Id))
            {
                // ADS2's animal line survives intact - its prerequisites are repointed, nothing more.
                foreach (string animal in new[] { "SimpleAnimalProsthetics", "AnimalBionics", "ToxFiltrationAnimal" })
                    Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(animal) != null,
                        $"{animal} is gone - A Dog Said 2's three animal projects are out of scope and " +
                        "still gate the animal prosthetic line");
            }
            else
            {
                Check.Note("CarveOutsAreUntouched: A Dog Said 2 absent - its three animal projects " +
                    "were not asserted.");
            }

            Check.SoftResult();
        }

        // ============================ G. The old tabs empty out ============================

        [Test]
        public static void OldTabsEmptyOut()
        {
            if (!Check.Ready(RetireKey) || !TabLoaded(RetireKey))
                return;

            // Proves the counting works and that this test is looking at the right thing.
            Check.Soft(ProjectsOnTab(TabName).Count > 0,
                $"the {TabName} tab holds no projects at all - the overhaul did not build a tree, so " +
                "the emptiness assertions below would be meaningless");

            string missingGuard = FirstMissingGuard(new[]
                { RootName, BodyProxy, ModulesProxy, CyberbrainsProxy, MindProxy });
            if (missingGuard != null)
            {
                Check.Note($"OldTabsEmptyOut: guard node {missingGuard} absent, so at least one trunk " +
                    "did not run - the old tabs are expected to still hold their projects and were " +
                    "not asserted empty.");
                Check.SoftResult();
                return;
            }

            AssertTabEmpty(EpoeTab, Ids.EPOEForked);
            AssertTabEmpty(LtsTab, Ids.IntegratedImplants);

            if (ModsConfig.IsActive(Ids.GiTS) && TabExists(GitsTab))
            {
                var expected = new List<string>();
                if (!SettingsRegistry.GetEffective("gits.research"))
                    expected.AddRange(new[]
                    {
                        "gitsResearchImmunityNanites", "gitsToxinRepairNanites", "gitsOrganDecayNanites",
                        "gitsResearchCyberInterface", "gitsResearchNet",
                    });
                if (!SettingsRegistry.GetEffective("gits.surgeries"))
                    expected.Add("gitsResearchBrainCyberization");

                List<string> leftovers = ProjectsOnTab(GitsTab);
                var unexpected = new List<string>();
                foreach (string project in leftovers)
                    if (!expected.Contains(project))
                        unexpected.Add(project);

                Check.Soft(unexpected.Count == 0,
                    $"the {GitsTab} tab still holds {unexpected.Count} project(s) the overhaul should " +
                    $"have dissolved: {string.Join(", ", unexpected.ToArray())}");
                if (expected.Count > 0)
                    Check.Note($"OldTabsEmptyOut: {GitsTab} keeps {expected.Count} project(s) because " +
                        "gits.research and/or gits.surgeries is off - they are those toggles' to delete, " +
                        "not the retire pass's.");
            }

            if (ModsConfig.IsActive(Ids.AlteredCarbon) && TabExists(AcTab))
            {
                List<string> leftovers = ProjectsOnTab(AcTab);
                var unexpected = new List<string>();
                foreach (string project in leftovers)
                    if (project != "AC_ChrysalisPoweredArmor" && project != "AC_AdvancedShieldBelt")
                        unexpected.Add(project);

                Check.Soft(unexpected.Count == 0,
                    $"the {AcTab} tab still holds {unexpected.Count} project(s) beyond the two apparel " +
                    $"carve-outs: {string.Join(", ", unexpected.ToArray())}");
                Check.Soft(leftovers.Count == 2,
                    $"the {AcTab} tab holds {leftovers.Count} project(s), expected exactly the two " +
                    "apparel carve-outs - if it is empty they were dissolved and their items are stranded");
            }

            AssertTabNotEmpty(LtsAnomalyTab, Ids.IntegratedImplants,
                "the dark-knowledge economy stays a separate tab reached through the bridge node");
            AssertTabNotEmpty(Ads2Tab, Ads2Id,
                "A Dog Said 2's animal research line is out of scope and keeps its own tab");

            Check.SoftResult();
        }

        // ================================== helpers ==================================

        private static bool TabLoaded(string settingKey)
        {
            if (DefDatabase<ResearchTabDef>.GetNamedSilentFail(TabName) != null)
                return true;
            Log.Message($"[RBP Tests] SKIP {settingKey}: {TabName} not present (cyberneticsresearch.core off)");
            return false;
        }

        private static bool TabExists(string tabDefName) =>
            DefDatabase<ResearchTabDef>.GetNamedSilentFail(tabDefName) != null;

        private static string TabDefName(ResearchProjectDef project) =>
            project?.tab == null ? "null" : project.tab.defName;

        private static List<string> ProjectsOnTab(string tabDefName)
        {
            var found = new List<string>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                if (project.tab != null && project.tab.defName == tabDefName)
                    found.Add(project.defName);
            return found;
        }

        /// <summary>The first guard node that is absent, or null when every one of them is present.</summary>
        private static string FirstMissingGuard(string[] guards)
        {
            foreach (string guard in guards)
                if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(guard) == null)
                    return guard;
            return null;
        }

        private static void AssertNodeOnTab(string defName, string what)
        {
            ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
            if (!Check.Soft(project != null,
                    $"{what}: ResearchProjectDef '{defName}' not found - the patch did not apply, or " +
                    "the def was renamed or removed upstream"))
                return;
            Check.Soft(project.tab != null && project.tab.defName == TabName,
                $"{defName} sits on tab '{TabDefName(project)}', expected {TabName} ({what})");
            AssertPrerequisitesResolve(project);
        }

        private static void AssertPrerequisitesResolve(ResearchProjectDef project)
        {
            AssertNoNullEntries(project.defName, "prerequisites", project.prerequisites);
            AssertNoNullEntries(project.defName, "hiddenPrerequisites", project.hiddenPrerequisites);
        }

        private static void AssertNoNullEntries(string owner, string field, List<ResearchProjectDef> list)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
                Check.Soft(list[i] != null,
                    $"{owner}.{field}[{i}] is null - it names a research project that does not exist " +
                    "(dangling cross-reference: a retired project, or a trunk whose toggle is off)");
        }

        private static List<string> CraftGateNames(ThingDef def)
        {
            var names = new List<string>();
            if (def?.recipeMaker == null)
                return names;
            if (def.recipeMaker.researchPrerequisite != null)
                names.Add(def.recipeMaker.researchPrerequisite.defName);
            if (def.recipeMaker.researchPrerequisites != null)
                foreach (ResearchProjectDef prereq in def.recipeMaker.researchPrerequisites)
                    names.Add(prereq?.defName ?? "<dangling>");
            return names;
        }

        private static string DescribeCraftGate(ThingDef def)
        {
            List<string> names = CraftGateNames(def);
            return names.Count == 0 ? "ungated" : string.Join("+", names.ToArray());
        }

        /// <summary>Every project a RecipeDef answers to, singular and plural fields alike.</summary>
        private static List<string> GateNames(RecipeDef recipe)
        {
            var names = new List<string>();
            if (recipe.researchPrerequisite != null)
                names.Add(recipe.researchPrerequisite.defName);
            if (recipe.researchPrerequisites != null)
                foreach (ResearchProjectDef prereq in recipe.researchPrerequisites)
                    names.Add(prereq?.defName ?? "<dangling>");
            return names;
        }

        private static string PrereqNames(ResearchProjectDef project)
        {
            if (project.prerequisites == null || project.prerequisites.Count == 0)
                return "none";
            var names = new List<string>();
            foreach (ResearchProjectDef prereq in project.prerequisites)
                names.Add(prereq?.defName ?? "<dangling>");
            return string.Join("+", names.ToArray());
        }

        private static void AssertCraftGate(ThingDef def, string expected)
        {
            List<string> names = CraftGateNames(def);
            Check.Soft(names.Count == 1 && names[0] == expected,
                $"{def.defName} crafting resolves to '{DescribeCraftGate(def)}', expected exactly " +
                $"{expected}");
        }

        private static void AssertSurgeryOnRoot(string recipeDefName, string ownerMod)
        {
            if (!ModsConfig.IsActive(ownerMod))
                return;
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
            if (!Check.Soft(recipe != null,
                    $"RecipeDef '{recipeDefName}' not found although '{ownerMod}' is active - renamed " +
                    "upstream, so the install-surgery repoint for that mod is unverified"))
                return;
            bool onRoot = recipe.researchPrerequisite?.defName == RootName;
            if (!onRoot && recipe.researchPrerequisites != null)
                foreach (ResearchProjectDef prereq in recipe.researchPrerequisites)
                    if (prereq?.defName == RootName)
                        onRoot = true;

            Check.Soft(onRoot,
                $"{recipeDefName} resolves to '{DescribeGate(recipe)}', " +
                $"expected {RootName} - every install surgery belongs to the root under D26/D26b");
        }

        private static string DescribeGate(RecipeDef recipe)
        {
            var parts = new List<string>();
            if (recipe.researchPrerequisite != null)
                parts.Add(recipe.researchPrerequisite.defName);
            if (recipe.researchPrerequisites != null)
                foreach (ResearchProjectDef prereq in recipe.researchPrerequisites)
                    parts.Add(prereq?.defName ?? "<dangling>");
            return parts.Count == 0 ? "ungated" : string.Join("+", parts.ToArray());
        }

        private static void SweepForDanglingReferences(HashSet<string> gone)
        {
            var dangling = new List<string>();

            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                CollectHits(dangling, gone, thing.defName, "researchPrerequisites", thing.researchPrerequisites);
                if (thing.recipeMaker == null)
                    continue;
                CollectHit(dangling, gone, thing.defName, "recipeMaker.researchPrerequisite",
                    thing.recipeMaker.researchPrerequisite);
                CollectHits(dangling, gone, thing.defName, "recipeMaker.researchPrerequisites",
                    thing.recipeMaker.researchPrerequisites);
            }

            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                CollectHit(dangling, gone, recipe.defName, "researchPrerequisite", recipe.researchPrerequisite);
                CollectHits(dangling, gone, recipe.defName, "researchPrerequisites", recipe.researchPrerequisites);
            }

            Check.Soft(dangling.Count == 0,
                $"{dangling.Count} def(s) still resolve a retired research project - the removal did " +
                $"not run, or the project was re-added: {string.Join(", ", dangling.ToArray())}");
        }

        private static void CollectHit(List<string> into, HashSet<string> gone, string owner, string field,
            ResearchProjectDef project)
        {
            if (project != null && gone.Contains(project.defName))
                into.Add($"{owner}.{field}={project.defName}");
        }

        private static void CollectHits(List<string> into, HashSet<string> gone, string owner, string field,
            List<ResearchProjectDef> list)
        {
            if (list == null)
                return;
            foreach (ResearchProjectDef project in list)
                CollectHit(into, gone, owner, field, project);
        }

        private static void AssertTabEmpty(string tabDefName, string ownerMod)
        {
            if (!ModsConfig.IsActive(ownerMod) || !TabExists(tabDefName))
                return;
            List<string> leftovers = ProjectsOnTab(tabDefName);
            Check.Soft(leftovers.Count == 0,
                $"the {tabDefName} tab still holds {leftovers.Count} project(s) - the player still sees " +
                $"the old tab beside the new one: {string.Join(", ", leftovers.ToArray())}");
        }

        private static void AssertTabNotEmpty(string tabDefName, string ownerMod, string because)
        {
            if (!ModsConfig.IsActive(ownerMod))
            {
                Check.Note($"OldTabsEmptyOut: '{ownerMod}' absent, so the {tabDefName} tab was not asserted.");
                return;
            }
            if (!Check.Soft(TabExists(tabDefName),
                    $"the {tabDefName} tab def is gone although '{ownerMod}' is active - {because}"))
                return;
            List<string> kept = ProjectsOnTab(tabDefName);
            Check.Soft(kept.Count > 0,
                $"the {tabDefName} tab holds no projects - {because}, so emptying it means the " +
                "consolidation reached content it was never meant to touch");
        }
    }
}
