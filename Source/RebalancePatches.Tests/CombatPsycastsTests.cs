using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CombatPsycastsTests
    {
        [Test]
        public static void KinecticistPathIsGeneGated() =>
            PathGenes.PathGated(Ids.CombatPsycasts, "RBP_Gene_Kinecticist", "CP_Combat");

        [Test]
        public static void KinecticistGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.CombatPsycasts, "RBP_Gene_Kinecticist",
                Ids.Biotech, "Hussar",
                Ids.RimsenalAskbarn, "Uredd");
    }
}
