using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VPERunesmithTests
    {
        [Test]
        public static void RunesmithPathIsGeneGated() =>
            PathGenes.PathGated(Ids.VPERunesmith, "RBP_Gene_Runesmith", "VPER_Runesmith");

        [Test]
        public static void RunesmithGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.VPERunesmith, "RBP_Gene_Runesmith",
                Ids.BSRaces, "BS_Dwarf",
                Ids.BSRaces, "BS_Svartalf",
                Ids.BSRaces, "BS_Gnome",
                Ids.Stoneborn, "Stoneborn",
                Ids.WVC, "WVC_Ferrkind");
    }
}
