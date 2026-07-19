using HarmonyLib;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class IntegratedImplantsTests
    {
        [Test]
        public static void SkillChipsNotBad()
        {
            if (!Check.Ready("implants.chipbad", Ids.IntegratedImplants))
                return;
            var skills = new[] { "Melee", "Shooting", "Construction", "Mining", "Cooking", "Plants",
                "Animals", "Crafting", "Artistic", "Medicine", "Social", "Intellectual" };
            int found = 0;
            foreach (string tier in new[] { "LTS_SpacerSkillChip_", "LTS_UltraSkillChip_" })
            {
                foreach (string skill in skills)
                {
                    HediffDef chip = DefDatabase<HediffDef>.GetNamedSilentFail(tier + skill);
                    if (chip == null)
                        continue;
                    found++;
                    Check.True(!chip.isBad, $"{chip.defName}.isBad is still true");
                }
            }
            if (found == 0)
                Log.Message("[RBP Tests] SKIP implants.chipbad: no skill chip hediffs loaded (II loads them only with Athena Framework + Anomaly)");
        }

        [Test]
        public static void MechanitorImplantsNeedAlphaMechsChips()
        {
            if (!Check.Ready("implants.chiptiers", Ids.IntegratedImplants, Ids.AlphaMechs))
                return;
            void CostIs(string building, string expectedChip, string removedChip)
            {
                ThingDef def = Check.Def<ThingDef>(building);
                Check.Eq(Check.CostOf(def, expectedChip), 1, $"{building} costList[{expectedChip}]");
                Check.True(Check.CostOf(def, removedChip) == null, $"{building} still costs {removedChip}");
            }
            CostIs("MechhiveSatelliteUplink", "AM_HyperLinkageChip", "PowerfocusChip");
            CostIs("Mechwomb", "AM_StellarProcessingChip", "PowerfocusChip");
            CostIs("WarprogrammerInterface", "AM_QuantumMatrixChip", "NanostructuringChip");
            CostIs("RemoteDominator", "AM_QuantumMatrixChip", "NanostructuringChip");
        }

        [Test]
        public static void MasochistsEnjoyVoicelock()
        {
            if (!Check.Ready("implants.voicelockmasochist", Ids.IntegratedImplants))
                return;
            ThoughtDef voicelock = Check.Def<ThoughtDef>("LTS_ActiveVoicelock");
            TraitDef masochist = Check.Def<TraitDef>("Masochist");
            Check.True(voicelock.nullifyingTraits != null && voicelock.nullifyingTraits.Contains(masochist),
                "LTS_ActiveVoicelock does not nullify for Masochist");
            ThoughtDef positive = Check.Def<ThoughtDef>("RBP_MasochistVoicelocked");
            Check.True(positive.requiredTraits != null && positive.requiredTraits.Contains(masochist),
                "RBP_MasochistVoicelocked does not require Masochist");
            Check.Eq(positive.stages[0].baseMoodEffect, 8f, "RBP_MasochistVoicelocked mood effect");
        }

        [Test]
        public static void ShoulderTurretsTargetShoulder()
        {
            if (!Check.Ready("implants.shoulderslimes", Ids.IntegratedImplants, Ids.BigSmallSlimes))
                return;
            foreach (string abilityName in new[] { "LTS_ToggleShoulderTurret", "LTS_ToggleShoulderChargeTurret" })
            {
                AbilityDef ability = Check.Def<AbilityDef>(abilityName);
                bool found = false;
                foreach (object comp in ability.comps)
                {
                    if (!comp.GetType().Name.Contains("ToggleHediff"))
                        continue;
                    found = true;
                    object location = Check.Field(comp, "location");
                    string locationName = location is Def d ? d.defName : location?.ToString();
                    Check.Eq(locationName, "Shoulder", $"{abilityName} toggle hediff location");
                }
                Check.True(found, $"{abilityName} has no ToggleHediff comp");
            }
        }

        [Test]
        public static void LevitatorsIgnoreWater()
        {
            if (!Check.Ready("implants.waterpathing", Ids.IntegratedImplants))
                return;
            Check.True(DefDatabase<HediffDef>.GetNamedSilentFail("PsychicLevitator") != null
                || DefDatabase<HediffDef>.GetNamedSilentFail("LTS_Gravlifter") != null,
                "neither PsychicLevitator nor LTS_Gravlifter hediff loaded");
            Check.HarmonyPatched(AccessTools.PropertyGetter(typeof(Pawn), "WaterCellCost"), "implants.waterpathing");
        }

        [Test]
        public static void BoosterStacksWithCommandRangeGenes()
        {
            if (!Check.Ready("implants.boosterrange", Ids.IntegratedImplants, Ids.AlphaGenes))
                return;
            Check.Def<HediffDef>("AG_IncreasedCommandRange");
            Check.Def<StatDef>("MechRemoteControlDistanceOffset");
            Check.HarmonyPatched(AccessTools.Method(typeof(Pawn_MechanitorTracker), "CanCommandTo"), "implants.boosterrange");
            Check.HarmonyPatched(AccessTools.Method(typeof(Pawn_MechanitorTracker), "DrawCommandRadius"), "implants.boosterrange");
        }
    }
}
