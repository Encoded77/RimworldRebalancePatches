using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VenatorsTests
    {

        [Test]
        public static void PsycastGenesAssigned() =>
            PathGenes.RaceRoster(Ids.Venators,
                "", "DV_Venator", "Gene_Nightstalker");
    }
}
