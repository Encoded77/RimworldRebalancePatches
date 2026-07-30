using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RebalancePatches.Mods.VanillaSkillsExpanded;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VanillaSkillsExpandedTests
    {
        private static readonly string[] AggregateDefNames =
        {
            "RBP_Marksman", "RBP_Gunslinger",
            "RBP_Warblade", "RBP_Skirmisher",
            "RBP_Beastmaster", "RBP_Steward",
            "RBP_Cultivator", "RBP_Wildwalker",
            "RBP_Virtuoso", "RBP_Prolific",
            "RBP_MasterBuilder", "RBP_Siteworker",
            "RBP_Gastronome", "RBP_Victualler",
            "RBP_Artificer", "RBP_Fabricator",
            "RBP_Chirurgeon", "RBP_Physician",
            "RBP_Assayer", "RBP_Excavator",
            "RBP_Scholar", "RBP_Anomalist", "RBP_Technician",
            "RBP_Diplomat", "RBP_Taskmaster", "RBP_Confidant", "RBP_Cutpurse",
        };

        private static readonly string[] BiotechAggregateDefNames =
        {
            "RBP_Mechlord", "RBP_Mechwright",
        };

        private static readonly string[] RoyaltyAggregateDefNames =
        {
            "RBP_Overchanneler", "RBP_OverchannelerRanged", "RBP_Quietist",
        };

        private const float Standard = 0.02f;
        private const float Wide = 0.015f;
        private const float Combat = 0.015f;
        private const float CombatWide = 0.0125f;

        private const string StaleLoad =
            "The toggle reads as enabled but the patch never ran, so the def database was built while it was still off. " +
            "Restart RimWorld and re-run; if Faster Game Loading is active, clear its cache too.";

        private static Def Expertise(string defName)
        {
            try
            {
                return Check.DefOfType("VSE.Expertise.ExpertiseDef", defName);
            }
            catch (Exception) when (defName.StartsWith("RBP_"))
            {
                throw new Exception($"'{defName}' is missing. {StaleLoad}");
            }
        }

        private static List<StatModifier> Offsets(Def def) =>
            (List<StatModifier>)Check.Field(def, "statOffsets");

        private static List<StatModifier> Factors(Def def) =>
            (List<StatModifier>)Check.Field(def, "statFactors");

        private static void HiddenAll(string what, params string[] defNames)
        {
            foreach (string defName in defNames)
                Check.True((bool)Check.Field(Expertise(defName), "hide"),
                    $"{what} expertise '{defName}' was not hidden. {StaleLoad}");
        }

        private static void WellFormed(string defName)
        {
            Def def = Expertise(defName);
            Check.True(Check.Field(def, "skill") != null, $"{defName} has no skill");
            Check.True(!(bool)Check.Field(def, "hide"), $"{defName} must not be hidden");
            bool hasStats = (Offsets(def) != null && Offsets(def).Count > 0)
                            || (Factors(def) != null && Factors(def).Count > 0);
            Check.True(hasStats, $"{defName} has no stat offsets or factors");
        }

        private static void WithinPowerBand(string defName)
        {
            Def def = Expertise(defName);
            foreach (StatModifier m in Factors(def) ?? new List<StatModifier>())
                Check.True(Math.Abs(m.value) <= 0.0201f,
                    $"{defName} factor {m.stat?.defName} is {m.value}, above the 0.02 per level band");
            foreach (StatModifier m in Offsets(def) ?? new List<StatModifier>())
                Check.True(Math.Abs(m.value) <= 0.3001f,
                    $"{defName} offset {m.stat?.defName} is {m.value}, above the 0.3 per level ceiling");
        }

        [Test]
        public static void AggregatesExist()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            foreach (string defName in AggregateDefNames)
                WellFormed(defName);
        }

        [Test]
        public static void MechanitorAggregatesExist()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.Biotech))
                return;
            foreach (string defName in BiotechAggregateDefNames)
            {
                WellFormed(defName);
                WithinPowerBand(defName);
            }
            Check.Eq(Check.StatModifierValue(Offsets(Expertise("RBP_Mechlord")), "MechBandwidth"), 0.1f,
                "RBP_Mechlord MechBandwidth");
            Check.Eq(Check.StatModifierValue(Factors(Expertise("RBP_Mechwright")), "MechRepairSpeed"), Wide,
                "RBP_Mechwright MechRepairSpeed");
            Check.True(Check.StatModifierValue(Offsets(Expertise("RBP_Mechlord")), "WorkSpeedGlobalOffsetMech") == null,
                "mech combat and work stats belong on RBP_Mechwright, not RBP_Mechlord");
        }

        [Test]
        public static void PsycastAggregatesExist()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.Royalty))
                return;
            foreach (string defName in RoyaltyAggregateDefNames)
            {
                WellFormed(defName);
                WithinPowerBand(defName);
            }
            Check.Eq(Check.Field(Expertise("RBP_Overchanneler"), "skill"),
                DefDatabase<SkillDef>.GetNamed("Melee"), "RBP_Overchanneler skill");
            Check.Eq(Check.Field(Expertise("RBP_OverchannelerRanged"), "skill"),
                DefDatabase<SkillDef>.GetNamed("Shooting"), "RBP_OverchannelerRanged skill");
            string melee = Expertise("RBP_Overchanneler").label;
            string ranged = Expertise("RBP_OverchannelerRanged").label;
            Check.True(melee != ranged, "the two Overchanneler variants must have distinct labels");
            Check.True(melee.StartsWith("Overchanneler") && ranged.StartsWith("Overchanneler"),
                "both Overchanneler variants must keep the shared name with a differentiating suffix");
        }

        [Test]
        public static void PsycastAggregatesCarryDrawbacks()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.Royalty))
                return;
            foreach (string defName in new[] { "RBP_Overchanneler", "RBP_OverchannelerRanged" })
            {
                Check.True(Check.StatModifierValue(Factors(Expertise(defName)), "MentalBreakThreshold") > 0f,
                    $"{defName} must raise MentalBreakThreshold as a drawback");
                Check.True(Check.StatModifierValue(Factors(Expertise(defName)), "RestFallRateFactor") > 0f,
                    $"{defName} must raise RestFallRateFactor as a drawback");
            }
            Check.True(Check.StatModifierValue(Offsets(Expertise("RBP_Quietist")), "PsychicEntropyMax") < 0f,
                "RBP_Quietist must lower PsychicEntropyMax as a drawback");
            Check.True(Check.StatModifierValue(Factors(Expertise("RBP_Quietist")), "PsychicSensitivity") < 0f,
                "RBP_Quietist must lower PsychicSensitivity as a drawback");
        }

        [Test]
        public static void QualityAggregatesPayInSpeed()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            Check.True(Check.StatModifierValue(Factors(Expertise("RBP_Virtuoso")), "VSE_ArtSpeed") < 0f,
                "RBP_Virtuoso must trade art speed for quality");
            Check.True(Check.StatModifierValue(Factors(Expertise("RBP_MasterBuilder")), "ConstructionSpeed") < 0f,
                "RBP_MasterBuilder must trade construction speed for quality");
            Check.True(Check.StatModifierValue(Factors(Expertise("RBP_Artificer")), "VSE_WeaponCreationSpeed") < 0f,
                "RBP_Artificer must trade weapon crafting speed for quality");
            Check.True(Check.StatModifierValue(Factors(Expertise("RBP_Artificer")), "VSE_TailoringSpeed") < 0f,
                "RBP_Artificer must trade tailoring speed for quality");
        }

        [Test]
        public static void CombatAndWideAggregatesAreTieredDown()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            foreach (string defName in new[] { "RBP_Marksman", "RBP_Gunslinger", "RBP_Warblade", "RBP_Skirmisher" })
                foreach (StatModifier m in Factors(Expertise(defName)) ?? new List<StatModifier>())
                    Check.True(Math.Abs(m.value) <= 0.0151f,
                        $"combat aggregate {defName} factor {m.stat?.defName} is {m.value}, above the 0.015 combat band");
            foreach (string defName in new[] { "RBP_Beastmaster", "RBP_Steward", "RBP_Siteworker", "RBP_Fabricator", "RBP_Scholar" })
                foreach (StatModifier m in Factors(Expertise(defName)) ?? new List<StatModifier>())
                    Check.True(Math.Abs(m.value) <= 0.0151f,
                        $"wide aggregate {defName} factor {m.stat?.defName} is {m.value}, above the 0.015 wide band");
        }

        [Test]
        public static void VanillaSkillsExpandedExpertisesHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            HiddenAll("VSE", "Precision", "CloseQuarters", "Gunner", "VSE_Sniping", "Sharp", "Blunt",
                "VSE_Striking", "Tamer", "Rancher", "Hunter", "Trainer", "QualityExpert", "QuantityExpert",
                "Flooring", "Repairman", "Architect", "Foreman", "VSE_Smoothing", "Butcher", "DrugChef",
                "IndustrialChef", "MechanoidExpert", "Tailor", "Weaponsmith", "IndustrialProcessExpert",
                "Surgeon", "BattlefieldMedic", "InfectiousDiseaseExpert", "VSE_Operating", "Driller",
                "OreExpert", "Tunneller", "Geologist", "Pharmacologist", "Researcher", "Forager",
                "HarvesterCareful", "GreenThumb", "Warden", "Negotiator");
        }

        [Test]
        public static void IdeologyExpertisesHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.Ideology))
                return;
            HiddenAll("VSE", "Hacker", "Treespeaker", "Proselytizer");
        }

        [Test]
        public static void AnomalyExpertiseHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.Anomaly))
                return;
            HiddenAll("VSE", "VSE_DarkStudy");
        }

        [Test]
        public static void HighmateExpertiseHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.VREHighmate))
                return;
            HiddenAll("VSE", "VSE_Lovin");
        }

        [Test]
        public static void AlphaSkillsExpertisesHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.AlphaSkills))
                return;
            HiddenAll("Alpha Skills", "AS_Blasting", "AS_Mortaring", "AS_Pummeling", "AS_Cleaving",
                "AS_Evading", "AS_Enduring", "AS_CraftingQuality", "AS_CraftingYield", "AS_Smelting",
                "AS_Panning", "AS_Salvaging", "AS_Trafficking", "AS_RangedDodging", "AS_Mindfulness");
        }

        [Test]
        public static void HautsFrameworkExpertisesHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.HautsFramework))
                return;
            HiddenAll("Hauts' Framework", "Hauts_Demolisher", "Hauts_Surveyor", "Hauts_Skulduggery",
                "Hauts_Larceny");
        }

        [Test]
        public static void FishingExpertisesHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.VCEF))
                return;
            HiddenAll("Vanilla Fishing Expanded", "VCEF_Swiftcasting", "VCEF_Catchmastery", "VCEF_Aquabounty");
        }

        [Test]
        public static void GravshipExpertisesHidden()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.VGravshipC1))
                return;
            HiddenAll("Vanilla Gravship Expanded", "VGE_GravshipResearch", "VGE_GravshipMaintaining",
                "VGE_GravshipTargeting");
        }

        [Test]
        public static void PowerBandIsCapped()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            foreach (string defName in AggregateDefNames)
                WithinPowerBand(defName);
        }

        [Test]
        public static void ShootingAggregateUsesVanillaCooldownStat()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            Def gunslinger = Expertise("RBP_Gunslinger");
            Check.Eq(Check.StatModifierValue(Factors(gunslinger), "RangedCooldownFactor"), -Combat,
                "RBP_Gunslinger RangedCooldownFactor");
            Check.True(Check.StatModifierValue(Factors(gunslinger), "VEF_VerbCooldownFactor") == null,
                "RBP_Gunslinger must not use VEF_VerbCooldownFactor");
        }

        [Test]
        public static void QualityAggregatesUseOffsets()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            Check.Eq(Check.StatModifierValue(Offsets(Expertise("RBP_Virtuoso")), "VSE_ArtQuality"), 0.025f,
                "RBP_Virtuoso VSE_ArtQuality");
            Check.Eq(Check.StatModifierValue(Offsets(Expertise("RBP_MasterBuilder")), "VSE_ConstructQuality"), 0.025f,
                "RBP_MasterBuilder VSE_ConstructQuality");
            Check.Eq(Check.StatModifierValue(Offsets(Expertise("RBP_Artificer")), "VSE_CraftingQuality"), 0.025f,
                "RBP_Artificer VSE_CraftingQuality");
        }

        [Test]
        public static void OptionalModStatsRideOnAggregates()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE, Ids.AlphaSkills, Ids.HautsFramework,
                    Ids.VGravshipC1, Ids.VCEF, Ids.Odyssey))
                return;
            Check.Eq(Check.StatModifierValue(Factors(Expertise("RBP_Fabricator")), "AS_CraftingYield"), Wide,
                "RBP_Fabricator AS_CraftingYield");
            Check.Eq(Check.StatModifierValue(Factors(Expertise("RBP_Scholar")), "Hauts_SurveySpeed"), Wide,
                "RBP_Scholar Hauts_SurveySpeed");
            Check.Eq(Check.StatModifierValue(Factors(Expertise("RBP_Scholar")), "VGE_GravshipResearch"), Wide,
                "RBP_Scholar VGE_GravshipResearch");
            Check.Eq(Check.StatModifierValue(Offsets(Expertise("RBP_Steward")), "VCEF_FishingLuckOffset"), 0.004f,
                "RBP_Steward VCEF_FishingLuckOffset");
            Check.Eq(Check.StatModifierValue(Factors(Expertise("RBP_Steward")), "FishingYield"), Wide,
                "RBP_Steward FishingYield");
        }

        [Test]
        public static void NoAggregateReferencesAMissingStat()
        {
            if (!Check.Ready("vse.expertiseconsolidation", Ids.VSE))
                return;
            foreach (string defName in AllAggregateDefNames())
            {
                Def def = Expertise(defName);
                foreach (StatModifier m in Offsets(def) ?? new List<StatModifier>())
                    Check.True(m.stat != null,
                        $"{defName} has a stat offset whose StatDef did not resolve - a MayRequire gate is missing a mod");
                foreach (StatModifier m in Factors(def) ?? new List<StatModifier>())
                    Check.True(m.stat != null,
                        $"{defName} has a stat factor whose StatDef did not resolve - a MayRequire gate is missing a mod");
            }
        }

        private static List<string> AllAggregateDefNames()
        {
            List<string> all = new List<string>(AggregateDefNames);
            if (ModsConfig.IsActive(Ids.Biotech))
                all.AddRange(BiotechAggregateDefNames);
            if (ModsConfig.IsActive(Ids.Royalty))
                all.AddRange(RoyaltyAggregateDefNames);
            return all;
        }

        [Test]
        public static void GeneratedPawnsCanStartWithAnExpertise()
        {
            if (!Check.Ready("vse.expertisegeneration", Ids.VSE))
                return;

            // The feature is a postfix on pawn generation; without it nothing below could fire.
            Check.HarmonyPatched(
                AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                    new[] { typeof(PawnGenerationRequest) }),
                "expertise generation");

            Pawn pawn = MakeAdult();
            if (!Check.Soft(pawn?.skills != null, "could not generate a test pawn with skills"))
            {
                Discard(pawn);
                Check.SoftResult();
                return;
            }
            try
            {
                // Generation may have granted one already; start from a clean slate we control.
                ClearExpertise(pawn);

                // Everything a generated pawn varies in can sink a skill below the level Vanilla
                // Skills Expanded demands, however high we set it: a backstory or trait that disables
                // the skill makes it read back as level 0, and an aptitude gene shifts the level too.
                // Flatten every skill and raise one this pawn can actually qualify in, so the roll
                // below depends on nothing generation happened to hand us.
                SkillDef focus = FocusSkill(pawn);
                Check.Note(DescribePawn(pawn, focus));
                if (focus == null)
                {
                    Log.Message("[RBP Tests] SKIP vse.expertisegeneration: no skill on the generated pawn can " +
                        $"reach VSE's LevelToGetExpertise of {LevelToGetExpertise()} and carry a visible expertise");
                    Discard(pawn);
                    Check.SoftResult();
                    return;
                }

                Check.Soft(ExpertiseGenerationPatches.Grant(pawn, 0f) == null,
                    "a zero chance still granted an expertise");

                // Record what VSE itself accepts and refuses right now, so a failure below reports the
                // pool it saw rather than sending someone off to read CanApplyOn.
                List<Def> eligible = ReportCandidates(pawn, focus);

                Def granted = ExpertiseGenerationPatches.Grant(pawn, 1f);
                if (!Check.Soft(granted != null,
                        $"a full-chance roll granted nothing while {eligible.Count} expertise def(s) passed VSE's " +
                        $"own CanApplyOn for {focus.defName} at level {pawn.skills.GetSkill(focus).Level} - " +
                        "the notes list every candidate and why each was refused"))
                {
                    Discard(pawn);
                    Check.SoftResult();
                    return;
                }
                Check.Note($"granted '{granted.defName}'");
                Check.Soft(eligible.Contains(granted),
                    $"granted '{granted.defName}', which VSE's own CanApplyOn had just refused");

                int level = LevelOfLastExpertise(pawn);
                Check.Soft(level >= 1 && level <= 3, $"granted expertise started at level {level}, expected 1 to 3");

                // Repeated grants must never take a pawn past Vanilla Skills Expanded's own per-pawn
                // cap - CanApplyOn enforces it and we honour its refusal. Read the real setting rather
                // than assume its default, so the test holds whatever the player set it to.
                int max = MaxExpertise();
                for (int i = 0; i < max + 3; i++)
                    ExpertiseGenerationPatches.Grant(pawn, 1f);
                Check.Soft(ExpertiseCount(pawn) <= max,
                    $"grants ran the pawn to {ExpertiseCount(pawn)} expertise past the cap of {max}");
            }
            finally
            {
                Discard(pawn);
            }
            Check.SoftResult();
        }

        private static Pawn MakeAdult()
        {
            try
            {
                return PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    allowAddictions: false, allowFood: false,
                    developmentalStages: DevelopmentalStage.Adult));
            }
            catch (Exception e)
            {
                Log.Warning("[RBP Tests] could not generate a test pawn: " + e.Message);
                return null;
            }
        }

        private static Type ExpertiseDefType() => GenTypes.GetTypeInAnyAssembly("VSE.Expertise.ExpertiseDef");

        private static IEnumerable<Def> AllExpertiseDefs()
        {
            Type type = ExpertiseDefType();
            return type == null ? new List<Def>() : GenDefDatabase.GetAllDefsInDatabaseForDef(type);
        }

        private static SkillDef SkillOf(Def def) => Check.Field(def, "skill") as SkillDef;

        private static bool Hidden(Def def) => (bool)Check.Field(def, "hide");

        private static bool CanApplyOn(Def def, Pawn pawn, out string reason)
        {
            MethodInfo method = ExpertiseDefType()?.GetMethod("CanApplyOn",
                new[] { typeof(Pawn), typeof(string).MakeByRefType() });
            if (method == null)
            {
                reason = "CanApplyOn(Pawn, out string) not found on VSE.Expertise.ExpertiseDef";
                return false;
            }
            object[] args = { pawn, null };
            bool ok = (bool)method.Invoke(def, args);
            reason = args[1] as string;
            return ok;
        }

        private static bool HasVisibleExpertise(SkillDef skill)
        {
            foreach (Def def in AllExpertiseDefs())
                if (!Hidden(def) && SkillOf(def) == skill)
                    return true;
            return false;
        }

        /// <summary>Flattens every skill, then raises the one skill this pawn can actually hold an
        /// expertise in - Shooting where the pawn allows it - to a level Vanilla Skills Expanded
        /// accepts. Returns that skill, or null when no skill on this pawn can qualify.</summary>
        private static SkillDef FocusSkill(Pawn pawn)
        {
            foreach (SkillRecord record in pawn.skills.skills)
            {
                record.Level = 0;
                record.passion = Passion.None;
                record.xpSinceLastLevel = 0f;
            }

            List<SkillDef> order = new List<SkillDef> { SkillDefOf.Shooting };
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
                if (skill != SkillDefOf.Shooting)
                    order.Add(skill);

            int need = LevelToGetExpertise();
            foreach (SkillDef skill in order)
            {
                if (!HasVisibleExpertise(skill))
                    continue;
                SkillRecord record = pawn.skills.GetSkill(skill);
                if (record == null || record.TotallyDisabled)
                    continue;
                record.Level = SkillRecord.MaxLevel;   // aptitude genes can still pull the level we read back down
                record.passion = Passion.Major;        // Major is never a bad passion, so CanApplyOn allows it
                if (record.Level >= need)
                    return skill;
                record.Level = 0;
                record.passion = Passion.None;
            }
            return null;
        }

        /// <summary>Notes the candidate pool and every refusal, and returns the defs VSE accepts.
        /// Notes surface only on failure, so a future red run says what it saw.</summary>
        private static List<Def> ReportCandidates(Pawn pawn, SkillDef focus)
        {
            List<Def> eligible = new List<Def>();
            List<string> eligibleNames = new List<string>();
            List<string> focusLines = new List<string>();
            Dictionary<string, int> refusals = new Dictionary<string, int>();
            int hidden = 0;
            int refused = 0;

            foreach (Def def in AllExpertiseDefs())
            {
                if (Hidden(def))
                {
                    hidden++;
                    continue;
                }
                bool ok = CanApplyOn(def, pawn, out string reason);
                if (string.IsNullOrEmpty(reason))
                    reason = "no reason given";
                if (ok)
                {
                    eligible.Add(def);
                    eligibleNames.Add(def.defName);
                }
                else
                {
                    refused++;
                    refusals.TryGetValue(reason, out int seen);
                    refusals[reason] = seen + 1;
                }
                if (SkillOf(def) == focus)
                    focusLines.Add(ok ? $"{def.defName}=eligible" : $"{def.defName}=refused ({reason})");
            }

            Check.Note($"VSE gate: LevelToGetExpertise={LevelToGetExpertise()}, MaxExpertise={MaxExpertise()}, " +
                $"AllowExpertiseOverlap={ExpertiseOverlapAllowed()}");
            Check.Note($"{focus.defName} expertise defs: {Listed(focusLines)}");
            Check.Note($"pool: {eligible.Count} eligible ({Listed(eligibleNames)}), {refused} refused, {hidden} hidden");
            foreach (KeyValuePair<string, int> pair in refusals)
                Check.Note($"refused x{pair.Value}: {pair.Key}");
            return eligible;
        }

        private static string Listed(List<string> items, int max = 12)
        {
            if (items.Count == 0)
                return "none";
            if (items.Count <= max)
                return string.Join(", ", items.ToArray());
            return string.Join(", ", items.GetRange(0, max).ToArray()) + $", +{items.Count - max} more";
        }

        private static string DescribePawn(Pawn pawn, SkillDef focus)
        {
            List<string> traits = new List<string>();
            if (pawn.story?.traits?.allTraits != null)
                foreach (Trait trait in pawn.story.traits.allTraits)
                    traits.Add(trait.def.defName + (trait.Degree == 0 ? "" : $"({trait.Degree})"));

            SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
            string focusPart = "none qualified";
            if (focus != null)
            {
                SkillRecord record = pawn.skills.GetSkill(focus);
                focusPart = $"{focus.defName} level={record.Level} passion={record.passion}";
            }
            // Grant refuses non-adults and non-humanlikes outright, so name them here too: with a
            // non-empty pool they are the only other way it can hand back null.
            return $"pawn '{pawn.LabelShortCap}' {pawn.DevelopmentalStage}, humanlike={pawn.RaceProps?.Humanlike}, " +
                $"xenotype={pawn.genes?.Xenotype?.defName ?? "none"}, " +
                $"childhood={pawn.story?.Childhood?.defName ?? "none"}, adulthood={pawn.story?.Adulthood?.defName ?? "none"}, " +
                $"disabled work tags={pawn.CombinedDisabledWorkTags}, traits=[{Listed(traits)}]; " +
                $"Shooting disabled={shooting.TotallyDisabled} aptitude={shooting.Aptitude}; focus={focusPart}";
        }

        private static object Tracker(Pawn pawn)
        {
            Type trackers = GenTypes.GetTypeInAnyAssembly("VSE.ExpertiseTrackers");
            return trackers?.GetMethod("Expertise", new[] { typeof(Pawn) })?.Invoke(null, new object[] { pawn });
        }

        private static IList Records(Pawn pawn)
        {
            object tracker = Tracker(pawn);
            return tracker?.GetType().GetProperty("AllExpertise")?.GetValue(tracker) as IList;
        }

        private static void ClearExpertise(Pawn pawn)
        {
            object tracker = Tracker(pawn);
            tracker?.GetType().GetMethod("ClearExpertise")?.Invoke(tracker, null);
        }

        private static int ExpertiseCount(Pawn pawn) => Records(pawn)?.Count ?? 0;

        /// <summary>Reads one of Vanilla Skills Expanded's own settings, so the test holds whatever
        /// the player set rather than assuming the shipped default.</summary>
        private static object Setting(string name)
        {
            object settings = GenTypes.GetTypeInAnyAssembly("VSE.SkillsMod")
                ?.GetField("Settings")?.GetValue(null);
            return settings?.GetType().GetField(name)?.GetValue(settings);
        }

        private static int MaxExpertise() => Setting("MaxExpertise") is int i ? i : 1;

        private static int LevelToGetExpertise() => Setting("LevelToGetExpertise") is int i ? i : 15;

        private static bool ExpertiseOverlapAllowed() => !(Setting("AllowExpertiseOverlap") is bool b) || b;

        private static int LevelOfLastExpertise(Pawn pawn)
        {
            IList records = Records(pawn);
            if (records == null || records.Count == 0)
                return 0;
            object last = records[records.Count - 1];
            return (int)last.GetType().GetProperty("Level").GetValue(last);
        }

        private static void Discard(Pawn pawn)
        {
            try
            {
                if (pawn != null && !pawn.Destroyed)
                    pawn.Destroy();
            }
            catch { }
        }
    }
}
