using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RebalancePatches
{
    public class StockGenerator_Sleeves : StockGenerator
    {
        private static bool resolved;
        private static bool available;
        private static MethodInfo createEmptySleeve;
        private static MethodInfo makeEmptySleeve;
        private static bool warned;

        private static void Resolve()
        {
            resolved = true;
            if (!ModsConfig.IsActive("hlx.UltratechAlteredCarbon"))
                return;
            try
            {
                Type utils = AccessTools.TypeByName("AlteredCarbon.AC_Utils");
                createEmptySleeve = AccessTools.Method(utils, "CreateEmptySleeve",
                    new[] { typeof(Pawn), typeof(bool), typeof(bool) });
                makeEmptySleeve = AccessTools.Method(utils, "MakeEmptySleeve", new[] { typeof(Pawn) });
                available = createEmptySleeve != null && makeEmptySleeve != null;
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not resolve Altered Carbon sleeve helpers:\n{ex}");
            }
        }

        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            if (!resolved)
                Resolve();
            if (!available)
                yield break;
            int count = countRange.RandomInRange;
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = TryGenerateSleeve(forTile);
                if (pawn != null)
                    yield return pawn;
            }
        }

        private Pawn TryGenerateSleeve(PlanetTile forTile)
        {
            try
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    null,
                    PawnGenerationContext.NonPlayer,
                    forTile,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: false,
                    allowFood: false,
                    allowAddictions: false,
                    forceNoIdeo: true,
                    developmentalStages: DevelopmentalStage.Adult);
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                createEmptySleeve.Invoke(null, new object[] { pawn, true, true });
                makeEmptySleeve.Invoke(null, new object[] { pawn });
                return pawn;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning($"[Rebalance Patches] Failed to generate an empty sleeve for trade:\n{ex}");
                }
                return null;
            }
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return false;
        }
    }
}
