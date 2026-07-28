using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.AlteredCarbon
{
    // Turns a freshly generated starting pawn into an empty sleeve that a neural stack, stored in the bunker
    // neural matrix, is actively needlecasting into, and tunes the malfunctioning relay to that matrix. All
    // Altered Carbon access is reflective so the assembly stays optional; every step is guarded and logs which
    // step failed rather than leaving a silent no-op.
    public static class NeedlecastStartup
    {
        private static bool resolved;
        private static bool usable;

        // Resolves the Altered Carbon reflection surface and reports whether every member bound - used by the
        // test suite to catch a signature drift before it becomes a silent no-op in game.
        public static bool DiagnosticsUsable()
        {
            Resolve();
            return usable;
        }

        private static Type tNeuralStack;
        private static Type tCompNeuralCache;
        private static Type tCastingRelayComp;
        private static MethodInfo neuralDataGetter;
        private static MethodInfo copyFromPawn;
        private static FieldInfo managerInstance;
        private static MethodInfo registerStack;
        private static MethodInfo makeEmptySleeve;
        private static MethodInfo needlecastTo;
        private static FieldInfo trackedToMatrixField;
        private static FieldInfo tunedToField;
        private static FieldInfo tuningToField;
        private static FieldInfo tuningTimeLeftField;
        private static FieldInfo tunedRelaysField;
        private static FieldInfo powerOnField;

        private static ThingDef activeStackDef;
        private static HediffDef remoteStackHediff;
        private static BodyPartDef neckDef;

        private static void Resolve()
        {
            if (resolved)
                return;
            resolved = true;
            try
            {
                tNeuralStack = AccessTools.TypeByName("AlteredCarbon.NeuralStack");
                tCompNeuralCache = AccessTools.TypeByName("AlteredCarbon.CompNeuralCache");
                tCastingRelayComp = AccessTools.TypeByName("AlteredCarbon.CompCastingRelay");
                Type tNeuralData = AccessTools.TypeByName("AlteredCarbon.NeuralData");
                Type tManager = AccessTools.TypeByName("AlteredCarbon.AlteredCarbonManager");
                Type tUtils = AccessTools.TypeByName("AlteredCarbon.AC_Utils");
                Type tMatrix = AccessTools.TypeByName("AlteredCarbon.Building_NeuralMatrix");
                if (tNeuralStack == null || tCompNeuralCache == null || tNeuralData == null || tManager == null
                    || tUtils == null || tMatrix == null)
                    return;

                neuralDataGetter = AccessTools.PropertyGetter(tNeuralStack, "NeuralData");
                copyFromPawn = AccessTools.Method(tNeuralData, "CopyFromPawn",
                    new[] { typeof(Pawn), typeof(ThingDef), typeof(bool), typeof(bool) });
                managerInstance = AccessTools.Field(tManager, "Instance");
                registerStack = AccessTools.Method(tManager, "RegisterStack", new[] { tNeuralStack });
                makeEmptySleeve = AccessTools.Method(tUtils, "MakeEmptySleeve", new[] { typeof(Pawn) });
                needlecastTo = AccessTools.Method(tNeuralStack, "NeedlecastTo", new[] { typeof(LocalTargetInfo) });
                trackedToMatrixField = AccessTools.Field(tNeuralData, "trackedToMatrix");
                tunedRelaysField = AccessTools.Field(tMatrix, "tunedCastingRelays");
                if (tCastingRelayComp != null)
                {
                    tunedToField = AccessTools.Field(tCastingRelayComp, "tunedTo");
                    tuningToField = AccessTools.Field(tCastingRelayComp, "tuningTo");
                    tuningTimeLeftField = AccessTools.Field(tCastingRelayComp, "tuningTimeLeft");
                }
                powerOnField = AccessTools.Field(typeof(CompPowerTrader), "powerOnInt");

                activeStackDef = DefDatabase<ThingDef>.GetNamedSilentFail("AC_ActiveNeuralStack");
                remoteStackHediff = DefDatabase<HediffDef>.GetNamedSilentFail("AC_RemoteStack");
                neckDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Neck");

                usable = neuralDataGetter != null && copyFromPawn != null && managerInstance != null
                    && registerStack != null && makeEmptySleeve != null && needlecastTo != null
                    && trackedToMatrixField != null && activeStackDef != null && remoteStackHediff != null;
                if (!usable)
                    Log.Warning("[Rebalance Patches] Altered Carbon needlecasting members did not all resolve; "
                        + $"getter={neuralDataGetter != null}, copy={copyFromPawn != null}, register={registerStack != null}, "
                        + $"empty={makeEmptySleeve != null}, cast={needlecastTo != null}, tracked={trackedToMatrixField != null}, "
                        + $"stackDef={activeStackDef != null}, remoteHediff={remoteStackHediff != null}");
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not resolve Altered Carbon needlecasting members:\n{ex}");
            }
        }

        // Captures the pawn's mind into an active stack, stores it in the matrix, installs a neural receiver on
        // the sleeve, empties the sleeve, and starts the cast. Returns false (leaving the pawn untouched) on any failure.
        public static bool TrySetup(Pawn pawn, Thing matrix)
        {
            Resolve();
            if (!usable || pawn == null || matrix == null)
                return false;
            string step = "start";
            try
            {
                step = "find cache";
                object cache = FindCache(matrix);
                if (cache == null)
                {
                    Log.Warning("[Rebalance Patches] Digitized start: matrix has no CompNeuralCache.");
                    return false;
                }

                step = "force matrix power";
                ForcePowerOn(matrix);

                step = "make stack";
                object stack = ThingMaker.MakeThing(activeStackDef);
                step = "copy mind";
                object neuralData = neuralDataGetter.Invoke(stack, null);
                copyFromPawn.Invoke(neuralData, new object[] { pawn, activeStackDef, false, true });

                step = "register stack";
                object manager = managerInstance.GetValue(null);
                if (manager == null)
                    return false;
                registerStack.Invoke(manager, new[] { stack });

                step = "store stack in matrix";
                if (!(cache is IThingHolder holder))
                {
                    Log.Warning("[Rebalance Patches] Digitized start: matrix cache is not an IThingHolder.");
                    return false;
                }
                ((Thing)stack).SetFactionDirect(Faction.OfPlayer);
                bool stored = holder.GetDirectlyHeldThings().TryAdd((Thing)stack, false);
                if (!stored)
                {
                    Log.Warning("[Rebalance Patches] Digitized start: could not store the neural stack in the matrix.");
                    return false;
                }

                // A direct TryAdd bypasses CompNeuralCache.Notify_HauledTo, which is what normally tracks the stack
                // to its matrix. Without this the connect status is ConnectionDisrupted every tick and the cast drops.
                step = "track stack to matrix";
                trackedToMatrixField.SetValue(neuralData, matrix);

                step = "install neural receiver";
                BodyPartRecord neck = null;
                if (neckDef != null)
                    neck = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def == neckDef);
                pawn.health.AddHediff(remoteStackHediff, neck);

                step = "empty sleeve";
                makeEmptySleeve.Invoke(null, new object[] { pawn });

                step = "needlecast";
                needlecastTo.Invoke(stack, new object[] { new LocalTargetInfo(pawn) });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Digitized-start needlecast failed at step '{step}':\n{ex}");
                return false;
            }
        }

        // Instantly tunes a casting relay to the matrix (bypassing the timed tuning), setting both the relay's
        // tunedTo pointer and the matrix's tuned-relay list.
        public static bool TryTuneRelay(Thing relay, Thing matrix)
        {
            Resolve();
            if (tCastingRelayComp == null || tunedToField == null || tunedRelaysField == null || relay == null || matrix == null)
                return false;
            try
            {
                ForcePowerOn(relay);
                object comp = FindComp(relay, tCastingRelayComp);
                if (comp == null)
                    return false;
                tunedToField.SetValue(comp, matrix);
                tuningToField?.SetValue(comp, null);
                tuningTimeLeftField?.SetValue(comp, 0);
                if (tunedRelaysField.GetValue(matrix) is IList list && !list.Contains(relay))
                    list.Add(relay);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Failed to tune the malfunctioning relay to the matrix:\n{ex}");
                return false;
            }
        }

        private static void ForcePowerOn(Thing thing)
        {
            if (powerOnField == null || !(thing is ThingWithComps twc))
                return;
            CompPowerTrader power = twc.GetComp<CompPowerTrader>();
            if (power != null)
                powerOnField.SetValue(power, true);
        }

        private static object FindCache(Thing matrix)
        {
            return FindComp(matrix, tCompNeuralCache);
        }

        private static object FindComp(Thing thing, Type compType)
        {
            if (compType == null || !(thing is ThingWithComps twc))
                return null;
            foreach (ThingComp comp in twc.AllComps)
                if (compType.IsInstanceOfType(comp))
                    return comp;
            return null;
        }
    }
}
