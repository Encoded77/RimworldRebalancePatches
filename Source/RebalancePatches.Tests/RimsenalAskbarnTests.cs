using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimsenalAskbarnTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.RimsenalAskbarn))
                return;
            Check.GenesGone("LowFertility");
            if (ModsConfig.IsActive(Ids.Venators))
                Check.GenesGone("RSLongshot");
            if (ModsConfig.IsActive(Ids.Keshig))
                Check.GenesGone("RSBornWarrior");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.RimsenalAskbarn))
                return;
            Check.XenoGene("Askbarn", "AG_ReducedFertile");
            Check.XenoGene("Uredd", "AG_ReducedFertile");
            if (ModsConfig.IsActive(Ids.Venators))
                Check.XenoGene("Askbarn", "DV_Farsighted");
            if (ModsConfig.IsActive(Ids.Keshig))
                Check.XenoGene("Uredd", "DV_DodgeChance_High");
        }
    }
}
