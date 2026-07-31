using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class HighbornTests
    {
        [Test]
        public static void PsycastGenesAssigned() =>
            PathGenes.RaceRoster(Ids.Highborn,
                "", "HBX_Highborn", "Gene_Empath",
                "", "HBX_Highborn", "Gene_Harmonist",
                "", "HBX_Highborn", "Gene_Warlord");
    }
}
