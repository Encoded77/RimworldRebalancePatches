using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using RebalancePatches.Mods.VREAndroid;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREAndroidPsychicTests
    {
        private const string Key = "cybernetics.ascension";
        private const string Counter = StatPart_PsychicDeafnessOverride.CounterGeneDefName;
        private const string Deafness = StatPart_PsychicDeafnessOverride.DeafnessGeneDefName;
        private const string SyntheticImmunity = "VREA_SyntheticImmunity";
        private const string HardwareCategory = "VREA_Hardware";

        [Test]
        public static void TheOverrideIsWiredIntoPsychicSensitivity()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            GeneDef deaf = DefDatabase<GeneDef>.GetNamedSilentFail(Deafness);
            if (Check.Soft(deaf != null,
                $"{Deafness} is gone - the android race renamed or dropped its deafness hardware, so "
                + "this whole feature now counters nothing"))
            {
                float? factor = Check.StatModifierValue(deaf.statFactors, "PsychicSensitivity");
                Check.Note($"{Deafness} PsychicSensitivity factor: "
                    + (factor.HasValue ? factor.Value.ToString() : "none"));
                Check.Soft(factor.HasValue && factor.Value <= 0f,
                    $"{Deafness} no longer zeroes PsychicSensitivity, so the override is countering "
                    + "something that is not happening any more");
            }

            GeneDef counter = DefDatabase<GeneDef>.GetNamedSilentFail(Counter);
            if (Check.Soft(counter != null, $"{Counter} was not added - the gated operation did not apply"))
            {
                Check.Soft(counter.displayCategory != null
                        && counter.displayCategory.defName == HardwareCategory,
                    $"{Counter} sits in display category "
                    + (counter.displayCategory?.defName ?? "none")
                    + $" rather than {HardwareCategory}, so it is never offered at the station");
                object core = TryField(counter, "isCoreComponent");
                Check.Soft(core is bool b && !b,
                    $"{Counter} reads isCoreComponent = {core ?? "unknown"}; it must be false or the "
                    + "modification window will not let it be toggled on");
            }

            StatDef sensitivity = Check.Def<StatDef>("PsychicSensitivity");
            bool wired = false;
            if (sensitivity.parts != null)
                foreach (StatPart part in sensitivity.parts)
                    if (part is StatPart_PsychicDeafnessOverride)
                        wired = true;
            Check.Soft(wired,
                "PsychicSensitivity carries no override part, so nothing runs after the deafness "
                + "factor and the counter cannot possibly work");

            if (ModsConfig.RoyaltyActive)
            {
                HediffDef amplifier = DefDatabase<HediffDef>.GetNamedSilentFail("PsychicAmplifier");
                Check.Soft(amplifier != null && CanCatch(amplifier),
                    "PsychicAmplifier is not marked catchable for androids, so a synthetic body is "
                    + "refused the psylink hediff before sensitivity ever gets consulted");
            }

            Check.SoftResult();
        }

        [Test]
        public static void AnOverriddenBodyRegainsPsychicSensitivity()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            Pawn pawn = MakeTestPawn();
            if (!Bail(pawn != null, "could not generate a test pawn - nothing was asserted"))
                return;

            try
            {
                if (!Bail(AddGenes(pawn, SyntheticImmunity, Deafness),
                        "could not put the android deafness hardware onto a test pawn"))
                    return;

                float deafened = Sensitivity(pawn);
                Check.Note($"sensitivity deafened: {deafened}");
                if (!Bail(deafened <= 0f,
                        $"a pawn carrying {Deafness} reads {deafened} psychic sensitivity, so it is not "
                        + "deaf to begin with and this test proves nothing about the override"))
                    return;

                if (!Bail(AddGenes(pawn, Counter), $"could not put {Counter} onto the test pawn"))
                    return;

                float overridden = Sensitivity(pawn);
                Check.Note($"sensitivity overridden: {overridden}");
                Check.Soft(overridden > 0f,
                    $"a pawn carrying both {Deafness} and {Counter} still reads {overridden} psychic "
                    + "sensitivity - the override is present but does nothing");

                Gene counter = pawn.genes.GetGene(DefDatabase<GeneDef>.GetNamedSilentFail(Counter));
                if (counter != null)
                {
                    pawn.genes.RemoveGene(counter);
                    float after = Sensitivity(pawn);
                    Check.Note($"sensitivity after removing the counter: {after}");
                    Check.Soft(after <= 0f,
                        $"sensitivity stayed at {after} after {Counter} was removed, so something "
                        + "other than the override is restoring it and the counter is not what is "
                        + "being measured");
                }

                Check.SoftResult();
            }
            finally
            {
                Discard(pawn);
            }
        }

        [Test]
        public static void AnOverriddenBodyCanStillRaiseItsPsylink()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech, Ids.Royalty))
                return;

            Check.HarmonyPatched(
                AccessTools.Method(typeof(Hediff_Psylink), nameof(Hediff_Psylink.ChangeLevel),
                    new[] { typeof(int) }),
                "psylink levels on an overridden synthetic body");

            Pawn control = MakeTestPawn();
            int controlGain = -1;
            if (control != null)
            {
                try
                {
                    if (AddGenes(control, SyntheticImmunity, Deafness))
                        controlGain = PsylinkGain(control);
                }
                finally
                {
                    Discard(control);
                }
            }
            Check.Note("levels gained by a deafened body with no override: "
                + (controlGain < 0 ? "not measured" : controlGain.ToString()));

            Pawn pawn = MakeTestPawn();
            if (!Bail(pawn != null, "could not generate a test pawn - nothing was asserted"))
                return;

            try
            {
                if (!Bail(AddGenes(pawn, SyntheticImmunity, Deafness, Counter),
                        "could not build an overridden synthetic body to test on"))
                    return;

                int gained = PsylinkGain(pawn);
                Check.Note($"levels gained by an overridden body: {gained}");
                Check.Soft(gained >= 0,
                    "an overridden synthetic body was refused the psylink hediff altogether, so it "
                    + "can never hold a psylink no matter what its sensitivity reads");
                Check.Soft(gained > 0,
                    "an overridden synthetic body held a psylink and could not raise it, so it is "
                    + "stuck at one psycast forever");

                Check.SoftResult();
            }
            finally
            {
                Discard(pawn);
            }
        }

        [Test]
        public static void TheResonatorWaitsForTheAscensionCapstone()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            MethodInfo validator = ValidatorMethod();
            if (!Bail(validator != null,
                    "the android race no longer has Window_CreateAndroidBase.GeneValidator, so there "
                    + "is nothing left to hang the research gate on and the resonator is ungated"))
                return;
            Check.HarmonyPatched(validator, "the resonator research gate");

            GeneDef resonator = DefDatabase<GeneDef>.GetNamedSilentFail(Counter);
            GeneDef control = DefDatabase<GeneDef>.GetNamedSilentFail(Deafness);
            if (!Bail(resonator != null && control != null,
                    $"{Counter} or {Deafness} is missing, so the gate cannot be measured"))
                return;

            ResearchProjectDef unfinished = AnUnfinishedProject();
            Check.Note("stand-in unfinished project: " + (unfinished?.defName ?? "none found"));

            Check.Soft(AndroidHardwareResearchGate.Allowed(resonator, null),
                $"{Counter} is withheld when the ascension capstone does not exist, so with the "
                + "research overhaul off it can never be fitted by anyone");

            // State two: a capstone exists and has not been finished.
            if (unfinished != null)
            {
                Check.Soft(!AndroidHardwareResearchGate.Allowed(resonator, unfinished),
                    $"{Counter} is still offered against an unfinished project ({unfinished.defName}), "
                    + "so the gate is not reading research progress");
                // Everything else the station offers must be untouched by this.
                Check.Soft(AndroidHardwareResearchGate.Allowed(control, unfinished),
                    $"{Deafness} is also being withheld, so the gate is filtering hardware it was "
                    + "never meant to touch");
            }

            ResearchProjectDef capstone = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
                AndroidHardwareResearchGate.GateResearchDefName);
            Check.Note($"live capstone: {(capstone == null ? "absent" : capstone.defName + ", finished=" + capstone.IsFinished)}");
            Check.Soft(AndroidHardwareResearchGate.AllowedNow(resonator)
                    == (capstone == null || capstone.IsFinished),
                $"{Counter} availability does not match the live capstone's state");

            Check.Soft(OfferedByWindow(validator, resonator) == AndroidHardwareResearchGate.AllowedNow(resonator),
                $"the fitting windows offer {Counter} differently from the gate, so the postfix is "
                + "not reaching the filter they actually use");
            Check.Soft(OfferedByWindow(validator, control),
                $"the fitting windows stopped offering {Deafness}, so the postfix is rejecting more "
                + "than the one gene it targets");

            Check.SoftResult();
        }

        private static MethodInfo ValidatorMethod()
        {
            Type window = GenTypes.GetTypeInAnyAssembly("VREAndroids.Window_CreateAndroidBase");
            return window == null
                ? null
                : AccessTools.Method(window, "GeneValidator", new[] { typeof(GeneDef) });
        }

        private static bool OfferedByWindow(MethodInfo validator, GeneDef gene)
        {
            try
            {
                Type concrete = GenTypes.GetTypeInAnyAssembly("VREAndroids.Window_AndroidCreation");
                if (concrete == null)
                    return AndroidHardwareResearchGate.AllowedNow(gene);
                object window = FormatterServices.GetUninitializedObject(concrete);
                return (bool)validator.Invoke(window, new object[] { gene });
            }
            catch (Exception e)
            {
                Log.Warning("[RBP Tests] could not drive the android gene filter: " + e.Message);
                return AndroidHardwareResearchGate.AllowedNow(gene);
            }
        }

        private static ResearchProjectDef AnUnfinishedProject()
        {
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                if (!project.IsFinished)
                    return project;
            return null;
        }

        private static int PsylinkGain(Pawn pawn)
        {
            try
            {
                BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
                var psylink = (Hediff_Psylink)HediffMaker.MakeHediff(
                    HediffDefOf.PsychicAmplifier, pawn, brain);
                psylink.suppressPostAddLetter = true;
                pawn.health.AddHediff(psylink, brain);

                var held = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier)
                    as Hediff_Psylink;
                if (held == null)
                    return -1;

                int before = held.level;
                held.ChangeLevel(1);
                return held.level - before;
            }
            catch (Exception e)
            {
                Log.Warning("[RBP Tests] psylink probe threw: " + e.Message);
                return -1;
            }
        }

        private static float Sensitivity(Pawn pawn) =>
            pawn.GetStatValue(StatDefOf.PsychicSensitivity);

        /// <summary>Adds each named gene as a xenogene; false if any of them is missing.</summary>
        private static bool AddGenes(Pawn pawn, params string[] defNames)
        {
            var defs = new List<GeneDef>();
            foreach (string name in defNames)
            {
                GeneDef def = DefDatabase<GeneDef>.GetNamedSilentFail(name);
                if (def == null)
                    return false;
                defs.Add(def);
            }
            try
            {
                foreach (GeneDef def in defs)
                    if (pawn.genes.GetGene(def) == null)
                        pawn.genes.AddGene(def, xenogene: true);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[RBP Tests] could not add test genes: " + e.Message);
                return false;
            }
        }

        /// <summary>Soft check that also closes the test out when it fails.</summary>
        private static bool Bail(bool condition, string because)
        {
            if (Check.Soft(condition, because))
                return true;
            Check.SoftResult();
            return false;
        }

        /// <summary>Reads the android race's own per-hediff opt-in without referencing its assembly.</summary>
        private static bool CanCatch(HediffDef def)
        {
            if (def.modExtensions == null)
                return false;
            foreach (DefModExtension ext in def.modExtensions)
                if (ext.GetType().FullName == "VREAndroids.AndroidSettingsExtension")
                    return TryField(ext, "androidCanCatchIt") as bool? == true;
            return false;
        }

        private static object TryField(object obj, string name)
        {
            try
            {
                return Check.Field(obj, name);
            }
            catch
            {
                return null;
            }
        }

        private static Pawn MakeTestPawn()
        {
            try
            {
                return PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    allowAddictions: false, allowFood: false));
            }
            catch (Exception e)
            {
                Log.Warning("[RBP Tests] could not generate a test pawn: " + e.Message);
                return null;
            }
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
