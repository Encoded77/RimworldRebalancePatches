using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimsenalZoharTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.CherryPicker, Ids.RimsenalZohar))
                return;
            Check.GenesGone("Gene_SensitiveStomach");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.CherryPicker, Ids.RimsenalZohar))
                return;
            Check.XenoGene("Zohar", "AG_FrailStomach");
        }

        [Test]
        public static void PsycastGenesAssigned() =>
            PathGenes.RaceRoster(Ids.RimsenalZohar,
                "", "Zohar", "Gene_Protector",
                "", "Zohar", "Gene_Chronopath");
    }
}