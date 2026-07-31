using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VRELycanthropeTests
    {
        [Test]
        public static void PsycastGenesAssigned() =>
            PathGenes.RaceRoster(Ids.VRELycanthrope,
                "", "VRE_Lycan", "Gene_Warlord");
    }
}
