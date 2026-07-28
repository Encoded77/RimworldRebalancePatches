using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RebalancePatches.Mods.AlteredCarbon
{
    // Scenario part for the digitized Altered Carbon start: builds a sealed, steel-walled bunker room at the
    // player start containing a self-powered neural matrix and a malfunctioning casting relay, then tunes the
    // relay and needlecasts the lone colonist's mind into their sleeve from the matrix. Does nothing when
    // Altered Carbon is absent.
    public class ScenPart_BunkerRig : ScenPart
    {
        private const string AlteredCarbonId = "hlx.ultratechalteredcarbon";
        private const string MatrixDefName = "RBP_BunkerNeuralMatrix";
        private const string RelayDefName = "RBP_MalfunctioningCastingRelay";

        public override string Summary(Scenario scen)
        {
            return "Digitized start: your mind wakes in a sealed bunker's derelict neural matrix, needlecast into a " +
                   "sleeve you don't fully control through an installed neural receiver. A malfunctioning relay caps " +
                   "your casting range at a single tile. If the matrix is destroyed, the connection - and the sleeve - " +
                   "dies with it.";
        }

        public override void PostMapGenerate(Map map)
        {
            if (Find.GameInitData == null || !ModsConfig.IsActive(AlteredCarbonId))
                return;
            try
            {
                BuildBunker(map);
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Failed to build the digitized-start bunker:\n{ex}");
            }
        }

        public override void PostGameStart()
        {
            if (!ModsConfig.IsActive(AlteredCarbonId))
                return;
            try
            {
                Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
                ThingDef matrixDef = DefDatabase<ThingDef>.GetNamedSilentFail(MatrixDefName);
                if (map == null || matrixDef == null)
                    return;
                Thing matrix = map.listerThings.ThingsOfDef(matrixDef).FirstOrDefault();
                if (matrix == null)
                    return;

                ThingDef relayDef = DefDatabase<ThingDef>.GetNamedSilentFail(RelayDefName);
                Thing relay = relayDef != null ? map.listerThings.ThingsOfDef(relayDef).FirstOrDefault() : null;
                if (relay != null)
                    NeedlecastStartup.TryTuneRelay(relay, matrix);

                foreach (Pawn pawn in map.mapPawns.FreeColonists.ToList())
                    NeedlecastStartup.TrySetup(pawn, matrix);
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Failed to start the digitized-start needlecast:\n{ex}");
            }
        }

        private static void BuildBunker(Map map)
        {
            ThingDef matrixDef = DefDatabase<ThingDef>.GetNamedSilentFail(MatrixDefName);
            ThingDef relayDef = DefDatabase<ThingDef>.GetNamedSilentFail(RelayDefName);
            ThingDef wallDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wall");
            ThingDef doorDef = DefDatabase<ThingDef>.GetNamedSilentFail("Door");
            ThingDef steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
            TerrainDef floor = DefDatabase<TerrainDef>.GetNamedSilentFail("Concrete");
            RoofDef roof = RoofDefOf.RoofConstructed;
            if (matrixDef == null || wallDef == null || doorDef == null || steel == null)
                return;

            const int outerW = 11;
            const int outerH = 9;
            IntVec3 start = MapGenerator.PlayerStartSpot;
            int ox = Mathf.Clamp(start.x - outerW / 2, 1, map.Size.x - outerW - 1);
            int oz = Mathf.Clamp(start.z + 1, 1, map.Size.z - outerH - 1);
            CellRect outer = new CellRect(ox, oz, outerW, outerH);
            CellRect interior = outer.ContractedBy(1);

            // South wall centre becomes the door, facing the pawn who stands at the player start below it.
            IntVec3 doorCell = new IntVec3(outer.minX + outerW / 2, 0, outer.minZ);

            foreach (IntVec3 cell in interior)
            {
                ClearForBuild(map, cell);
                if (floor != null)
                    map.terrainGrid.SetTerrain(cell, floor);
                if (roof != null)
                    map.roofGrid.SetRoof(cell, roof);
            }
            foreach (IntVec3 cell in outer.EdgeCells)
            {
                ClearForBuild(map, cell);
                if (floor != null)
                    map.terrainGrid.SetTerrain(cell, floor);
                if (roof != null)
                    map.roofGrid.SetRoof(cell, roof);
                Spawn(map, cell == doorCell ? doorDef : wallDef, steel, cell);
            }

            var matrixCenter = new IntVec3(interior.minX + 1, 0, interior.CenterCell.z);
            var relayCenter = new IntVec3(interior.maxX - 1, 0, interior.CenterCell.z);
            Spawn(map, matrixDef, null, matrixCenter);
            if (relayDef != null)
                Spawn(map, relayDef, null, relayCenter);
        }

        private static void ClearForBuild(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
                return;
            foreach (Thing thing in cell.GetThingList(map).ToList())
            {
                if (thing is Pawn || thing.def.category == ThingCategory.Item)
                    continue;
                if (thing.def.category == ThingCategory.Plant || thing.def.category == ThingCategory.Filth
                    || thing.def.destroyable && (thing.def.building == null || thing.def.building.isNaturalRock))
                    thing.Destroy();
            }
        }

        private static void Spawn(Map map, ThingDef def, ThingDef stuff, IntVec3 center)
        {
            if (def == null)
                return;
            CellRect rect = GenAdj.OccupiedRect(center, Rot4.North, def.Size);
            if (!rect.InBounds(map))
                return;
            Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? (stuff ?? GenStuff.DefaultStuffFor(def)) : null);
            thing.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(thing, center, map, Rot4.North, WipeMode.Vanish);
        }
    }
}
