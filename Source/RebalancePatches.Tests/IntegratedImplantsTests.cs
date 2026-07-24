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
        public static void TorsoImplantsAreModules()
        {
            if (!Check.Ready("cybernetics.modules", Ids.IntegratedImplants, Ids.EBSG))
                return;

            var modules = new[]
            {
                new[] { "LTS_StealthSystem", "1", "LTS_InstallStealthSystem" },
                new[] { "DeacidifierMembrane", "2", "InstallDeacidifierMembrane" },
                new[] { "BiochameleonDevice", "1", "InstallBiochameleonDevice" },
                new[] { "LTS_EmergencyShield", "2", "LTS_InstallEmergencyShield" },
                new[] { "LTS_EmergencyVacseal", "2", "LTS_InstallEmergencyVacseal" },
                new[] { "LTS_ShieldImplant", "2", "LTS_InstallShieldImplant" },
                new[] { "LTS_FireSupressionSystem", "1", "LTS_InstallFireSupressionSystem" },
                new[] { "CranialInsulation", "1", "InstallCranialInsulation" },
                new[] { "SkeletalBracing", "2", "InstallSkeletalBracing" },
                new[] { "LTS_SubdermalArmour", "2", "LTS_InstallSubdermalArmour" },
                new[] { "LTS_PrestigeSubdermalArmour", "2", "LTS_InstallPrestigeSubdermalArmour" },
                new[] { "InternalClimateControlImplant", "1", "InstallInternalClimateControlImplant" },
                new[] { "CryptoWakeImplant", "1", "InstallCryptoWakeImplant" },
                new[] { "BiotunerImplant", "1", "InstallBiotunerImplant" },
                new[] { "LTS_CellularAdapter", "1", "LTS_InstallCellularAdapter" },
                new[] { "LTS_CryptoCoagulator", "1", "LTS_InstallCryptoCoagulator" },
                new[] { "LTS_Gravlifter", "1", "LTS_InstallGravlifter" },
                new[] { "LTS_Vanguard", "1", "LTS_InstallVanguard" },
                new[] { "WiredReflex", "1", "InstallWiredReflex" },
                new[] { "StrengthEnhancer", "1", "InstallStrengthEnhancer" },
                new[] { "HyperAdrenalineGland", "1", "InstallHyperAdrenalineGland" },
                new[] { "LTS_JotunnskinGland", "1", "LTS_InstallJotunnskinGland" },
            };

            int converted = 0;
            foreach (string[] entry in modules)
            {
                string thingName = entry[0];
                float capacity = float.Parse(entry[1]);
                string recipeName = entry[2];

                ThingDef thing = DefDatabase<ThingDef>.GetNamedSilentFail(thingName);
                if (thing == null)
                    continue;
                converted++;

                if (!Check.Soft(thing.comps != null, $"{thingName} has no comps at all"))
                    continue;

                object module = null;
                bool usable = false;
                foreach (CompProperties props in thing.comps)
                {
                    if (props == null)
                        continue;
                    if (props.GetType().Name == "CompProperties_UseEffectHediffModule")
                        module = props;
                    if (props is CompProperties_Usable)
                        usable = true;
                }

                Check.Soft(!usable, $"{thingName} still carries CompProperties_Usable, so it can self-install");

                if (!Check.Soft(module != null, $"{thingName} carries no module comp; comps are: "
                        + string.Join(", ", thing.comps.ConvertAll(c => c?.GetType().Name ?? "null").ToArray())))
                    continue;

                var slotIds = Check.Field(module, "slotIDs") as System.Collections.Generic.List<string>;
                Check.Soft(slotIds != null && slotIds.Contains("RBP_TorsoSlot"),
                    $"{thingName} module slotIDs are [{(slotIds == null ? "null" : string.Join(", ", slotIds.ToArray()))}], not RBP_TorsoSlot");

                Check.Soft(System.Convert.ToSingle(Check.Field(module, "requiredCapacity")) == capacity,
                    $"{thingName} requiredCapacity is {Check.Field(module, "requiredCapacity")}, expected {capacity}");

                var hediffs = Check.Field(module, "hediffs") as System.Collections.IEnumerable;
                bool grantsOwnHediff = false;
                if (hediffs != null)
                    foreach (object h in hediffs)
                        if (h is Def def && def.defName == thingName)
                            grantsOwnHediff = true;
                Check.Soft(grantsOwnHediff, $"{thingName} module does not grant the {thingName} hediff");

                RecipeDef install = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (!Check.Soft(install != null, $"{recipeName} is missing while {thingName} exists"))
                    continue;
                Check.Soft(install.workerClass == typeof(Recipe_InstallModule),
                    $"{recipeName} workerClass is {install.workerClass?.FullName ?? "null"}, expected RebalancePatches.Recipe_InstallModule");
            }

            Check.Note($"{converted} of {modules.Length} torso implants were present in this modlist");
            Check.Soft(converted > 0, "no Integrated Implants torso implant defs loaded at all");
            Check.SoftResult();
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
