using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AlphaMemesTests
    {
        [Test]
        public static void VacstoneStyledTilesBuildable()
        {
            if (!Check.Ready("alphamemes.vacstonetiles", Ids.AlphaMemes, Ids.Odyssey))
                return;
            var expected = new Dictionary<string, int>
            {
                { "RBP_AM_JewishTileVacstone", 4 },
                { "RBP_AM_JewishFineTileVacstone", 20 },
                { "RBP_AM_KemeticTileVacstone", 4 },
                { "RBP_AM_KemeticFineTileVacstone", 20 },
                { "RBP_AM_SteampunkTileVacstone", 4 },
                { "RBP_AM_Tile_OcularVacstone", 20 },
            };
            foreach (KeyValuePair<string, int> pair in expected)
            {
                TerrainDef tile = Check.Def<TerrainDef>(pair.Key);
                Check.Eq(Check.CostOf(tile, "BlocksVacstone"), pair.Value, $"{pair.Key} BlocksVacstone cost");
                Check.True(tile.designationCategory != null, $"{pair.Key} is not buildable (no designationCategory)");
            }
        }
    }
}
