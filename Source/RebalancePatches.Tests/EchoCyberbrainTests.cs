using System.Collections;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class EchoCyberbrainTests
    {
        private const string Key = "cybernetics.echobrains";

        [Test]
        public static void EchoModelsExistAndInstallBySurgery()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            foreach (string[] pair in new[]
            {
                new[] { "RBP_EchoSeerCyberbrain", "RBP_EchoSeerHediff", "RBP_InstallEchoSeerCyberbrain" },
                new[] { "RBP_EchoOracleCyberbrain", "RBP_EchoOracleHediff", "RBP_InstallEchoOracleCyberbrain" },
            })
            {
                ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail(pair[0]);
                Check.Soft(item != null, $"{pair[0]} not found - did the Echo patch apply?");

                HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(pair[1]);
                if (Check.Soft(hediff != null, $"{pair[1]} not found"))
                    Check.Soft(hediff.addedPartProps != null && hediff.addedPartProps.betterThanNatural,
                        $"{pair[1]} should replace the brain as a better-than-natural part");

                RecipeDef install = DefDatabase<RecipeDef>.GetNamedSilentFail(pair[2]);
                if (Check.Soft(install != null, $"{pair[2]} not found"))
                    Check.Soft(install.addsHediff?.defName == pair[1],
                        $"{pair[2]} addsHediff is '{install.addsHediff?.defName ?? "null"}', expected {pair[1]}");
            }

            Check.SoftResult();
        }

        [Test]
        public static void EchoModelsDropPsiDesignation()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            foreach (string[] pair in new[]
            {
                new[] { "RBP_EchoSeerCyberbrain", "Echo SEER cyberbrain" },
                new[] { "RBP_EchoOracleCyberbrain", "Echo ORACLE cyberbrain" },
            })
            {
                ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail(pair[0]);
                if (!Check.Soft(item != null, $"{pair[0]} not found"))
                    continue;
                Check.Soft(item.label == pair[1], $"{pair[0]} label is '{item.label}', expected '{pair[1]}'");
                Check.Soft(item.description != null && !item.description.Contains("Psi-"),
                    $"{pair[0]} description still carries a Psi designation");
            }

            Check.SoftResult();
        }

        [Test]
        public static void EchoLeadsThePsychicBand()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            float hades = MeditationGain("gitsHADESHediff");
            float px7 = MeditationGain("gitsPX7Hediff");
            float best = hades > px7 ? hades : px7;
            float oracle = MeditationGain("RBP_EchoOracleHediff");
            float seer = MeditationGain("RBP_EchoSeerHediff");

            Check.Soft(oracle > best,
                $"Echo ORACLE MeditationFocusGain {oracle} does not beat the best GiTS extreme ({best}) - " +
                "a combat brain would be the better psycaster, which defeats the lane");
            Check.Soft(seer > 0f && seer < oracle,
                $"Echo SEER MeditationFocusGain {seer} should sit above zero and below ORACLE ({oracle})");

            Check.Soft(Consciousness("RBP_EchoOracleHediff") < Consciousness("gitsHADESHediff"),
                "Echo ORACLE should grant less Consciousness than HADES - psychic power is bought, not added");

            Check.SoftResult();
        }

        [Test]
        public static void GeneralCyberbrainsAreNotPsycastingHardware()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            const float ceiling = 0.05f;
            var offenders = new System.Collections.Generic.List<string>();
            foreach (HediffDef hediff in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                if (hediff.defName == null || !hediff.defName.StartsWith("gits"))
                    continue;
                float gain = MeditationGain(hediff.defName);
                if (gain > ceiling)
                    offenders.Add($"{hediff.defName}={gain}");
            }

            Check.Soft(offenders.Count == 0,
                $"{offenders.Count} GiTS hediff(s) still grant MeditationFocusGain above {ceiling}: " +
                string.Join(", ", offenders.ToArray()));
            Check.SoftResult();
        }

        [Test]
        public static void EchoModelsCarryTierAbilities()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            AssertAbilities("RBP_EchoSeerHediff", "gitsAddedCyberbrainSpecWorkBase",
                new[] { "gitsPainReceptorDampen", "gitsWorkDBUplink" });
            AssertAbilities("RBP_EchoOracleHediff", "gitsAddedCyberbrainExtremeBase",
                new[] { "gitsPainReceptorNullify", "gitsWorkDBUplink" });

            Check.SoftResult();
        }

        private static void AssertAbilities(string hediffName, string tierBase, string[] expected)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (!Check.Soft(hediff != null, $"{hediffName} not found - did the Echo patch apply?"))
                return;

            var have = new System.Collections.Generic.List<string>();
            if (hediff.abilities != null)
                foreach (AbilityDef ability in hediff.abilities)
                    if (ability != null)
                        have.Add(ability.defName);

            foreach (string name in expected)
                Check.Soft(have.Contains(name),
                    $"{hediffName} sits on the same price rung as every model built on {tierBase} but " +
                    $"does not grant {name}; it grants " +
                    (have.Count == 0 ? "nothing" : string.Join(", ", have.ToArray())));
        }

        [Test]
        public static void EchoBrainsHostCortexModules()
        {
            if (!Check.Ready(Key, Ids.GiTS, Ids.EBSG))
                return;

            AssertCortexCapacity("RBP_EchoSeerHediff", 3);
            AssertCortexCapacity("RBP_EchoOracleHediff", 6);

            Check.SoftResult();
        }

        private static void AssertCortexCapacity(string hediffName, int expected)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (!Check.Soft(hediff != null, $"{hediffName} not found"))
                return;
            if (!Check.Soft(hediff.comps != null, $"{hediffName} has no comps"))
                return;

            foreach (object props in hediff.comps)
            {
                if (props == null || props.GetType().Name != "HediffCompProperties_Modular")
                    continue;
                if (!(Check.Field(props, "slots") is IEnumerable slots))
                    continue;
                foreach (object slot in slots)
                {
                    if ((Check.Field(slot, "slotID") as string) != "RBP_CortexSlot")
                        continue;
                    Check.Soft(System.Convert.ToInt32(Check.Field(slot, "capacity")) == expected,
                        $"{hediffName} RBP_CortexSlot capacity is " +
                        $"{System.Convert.ToInt32(Check.Field(slot, "capacity"))}, expected {expected}");
                    return;
                }
            }

            Check.Soft(false, $"{hediffName} declares no RBP_CortexSlot");
        }

        private static float StatOffset(string hediffName, string statName)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (hediff?.stages == null)
                return 0f;
            StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(statName);
            if (stat == null)
                return 0f;
            float best = 0f;
            foreach (HediffStage stage in hediff.stages)
            {
                if (stage?.statOffsets == null)
                    continue;
                float v = stage.statOffsets.GetStatOffsetFromList(stat);
                if (v > best)
                    best = v;
            }
            return best;
        }

        private static float MeditationGain(string hediffName) =>
            StatOffset(hediffName, "MeditationFocusGain");

        private static float Consciousness(string hediffName)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (hediff?.stages == null)
                return 0f;
            float best = 0f;
            foreach (HediffStage stage in hediff.stages)
            {
                if (stage?.capMods == null)
                    continue;
                foreach (PawnCapacityModifier mod in stage.capMods)
                    if (mod?.capacity == PawnCapacityDefOf.Consciousness && mod.offset > best)
                        best = mod.offset;
            }
            return best;
        }
    }
}
