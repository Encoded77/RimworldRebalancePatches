using System;
using System.Collections.Generic;
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
    }
}
