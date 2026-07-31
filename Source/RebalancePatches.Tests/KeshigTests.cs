using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class KeshigTests
    {
        [Test]
        public static void PsycastGenesAssigned() =>
            PathGenes.RaceRoster(Ids.Keshig,
                "", "DV_Keshig", "Gene_Warlord",
                "", "DV_Keshig", "Gene_Protector");
    }
}
