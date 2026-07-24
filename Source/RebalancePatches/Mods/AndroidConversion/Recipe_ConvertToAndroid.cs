using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.AndroidConversion
{
    public class Recipe_ConvertToAndroid : Recipe_Surgery
    {
        private const string AndroidXenotypeName = "VREA_AndroidAwakened";
        private const string SyntheticBodyGeneName = "VREA_SyntheticBody";
        private const string ReactorHediffName = "VREA_Reactor";
        private const string RecoveryHediffName = "RBP_SyntheticAscensionRecovery";
        private const string StomachPartName = "Stomach";

        private static MethodInfo recheckHediffs;
        private static bool recheckHediffsResolved;

        private static MethodInfo androidCounterPart;
        private static bool androidCounterPartResolved;

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return base.AvailableOnNow(thing, part) && CanConvert(thing as Pawn);
        }

        public override bool CompletableEver(Pawn surgeryTarget) => CanConvert(surgeryTarget);

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer,
            List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                return;

            try
            {
                Convert(pawn);
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] Android conversion failed on "
                    + pawn?.LabelShortCap + ": " + e);
            }
        }

        private static bool CanConvert(Pawn pawn)
        {
            if (pawn?.genes == null)
                return false;
            GeneDef synthetic = DefDatabase<GeneDef>.GetNamedSilentFail(SyntheticBodyGeneName);
            return synthetic != null && !pawn.genes.HasActiveGene(synthetic);
        }

        private static void Convert(Pawn pawn)
        {
            XenotypeDef android = DefDatabase<XenotypeDef>.GetNamedSilentFail(AndroidXenotypeName);
            if (android == null)
            {
                Log.Error("[RebalancePatches] " + AndroidXenotypeName
                    + " is missing, so there is nothing to convert into.");
                return;
            }

            pawn.genes.SetXenotype(android);

            InstallAndroidParts(pawn);

            InstallReactor(pawn);

            pawn.needs?.AddOrRemoveNeedsAsAppropriate();

            RecheckHediffs(pawn);

            AddRecovery(pawn);
        }

        private static void InstallAndroidParts(Pawn pawn)
        {
            MethodInfo lookup = CounterPartLookup();
            if (lookup == null)
                return;

            // Snapshot first: adding a hediff mutates the set this walks.
            var parts = new List<BodyPartRecord>(pawn.health.hediffSet.GetNotMissingParts());
            foreach (BodyPartRecord part in parts)
            {
                HediffDef counterpart;
                try
                {
                    counterpart = lookup.Invoke(null, new object[] { part.def }) as HediffDef;
                }
                catch (Exception e)
                {
                    Log.Warning("[RebalancePatches] Could not read the android counterpart for "
                        + part.def?.defName + ": " + e);
                    return;
                }
                if (counterpart == null || HasOnPart(pawn, counterpart, part))
                    continue;
                pawn.health.AddHediff(counterpart, part);
            }
        }

        private static bool HasOnPart(Pawn pawn, HediffDef def, BodyPartRecord part)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                if (hediff.def == def && hediff.Part == part)
                    return true;
            return false;
        }

        private static MethodInfo CounterPartLookup()
        {
            if (!androidCounterPartResolved)
            {
                androidCounterPartResolved = true;
                Type utils = GenTypes.GetTypeInAnyAssembly("VREAndroids.Utils");
                if (utils != null)
                    androidCounterPart = HarmonyLib.AccessTools.Method(
                        utils, "GetAndroidCounterPart", new[] { typeof(BodyPartDef) });
            }
            return androidCounterPart;
        }

        private static void AddRecovery(Pawn pawn)
        {
            HediffDef recovery = DefDatabase<HediffDef>.GetNamedSilentFail(RecoveryHediffName);
            if (recovery == null || pawn.health.hediffSet.GetFirstHediffOfDef(recovery) != null)
                return;
            pawn.health.AddHediff(recovery);
        }

        private static void InstallReactor(Pawn pawn)
        {
            HediffDef reactor = DefDatabase<HediffDef>.GetNamedSilentFail(ReactorHediffName);
            if (reactor == null || pawn.health.hediffSet.GetFirstHediffOfDef(reactor) != null)
                return;

            BodyPartRecord site = ReactorSite(pawn);
            pawn.health.AddHediff(HediffMaker.MakeHediff(reactor, pawn, site), site);
        }

        private static BodyPartRecord ReactorSite(Pawn pawn)
        {
            BodyPartDef stomach = DefDatabase<BodyPartDef>.GetNamedSilentFail(StomachPartName);
            if (stomach != null)
                foreach (BodyPartRecord candidate in pawn.health.hediffSet.GetNotMissingParts())
                    if (candidate.def == stomach)
                        return candidate;
            return pawn.RaceProps.body.corePart;
        }

        private static void RecheckHediffs(Pawn pawn)
        {
            if (!recheckHediffsResolved)
            {
                recheckHediffsResolved = true;
                Type utils = GenTypes.GetTypeInAnyAssembly("VREAndroids.Utils");
                if (utils != null)
                    recheckHediffs = HarmonyLib.AccessTools.Method(utils, "RecheckHediffs");
            }
            if (recheckHediffs == null)
                return;
            try
            {
                recheckHediffs.Invoke(null, new object[] { pawn });
            }
            catch (Exception e)
            {
                Log.Warning("[RebalancePatches] VREAndroids.Utils.RecheckHediffs threw: " + e);
            }
        }
    }
}
