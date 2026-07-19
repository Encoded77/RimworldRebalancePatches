using System.Collections;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AlphaGenesTests
    {
        [Test]
        public static void AlphapacksUnobtainable()
        {
            if (!Check.Ready("alphagenes.genepacks", Ids.AlphaGenes))
                return;
            foreach (string name in new[] { "AG_Alphapack", "AG_Mixedpack" })
            {
                ThingDef pack = Check.Def<ThingDef>(name);
                Check.Eq(pack.tradeability, Tradeability.None, $"{name}.tradeability");
                Check.True(pack.thingSetMakerTags.NullOrEmpty(), $"{name} still has thingSetMakerTags");
            }
        }

        [Test]
        public static void MainlineGeneWeights()
        {
            if (!Check.Ready("alphagenes.genepacks", Ids.AlphaGenes))
                return;
            Check.Eq(Check.Def<GeneDef>("AG_MechConversion").selectionWeight, 0.5f, "AG_MechConversion.selectionWeight");
            Check.Eq(Check.Def<GeneDef>("AG_RainbowWings").selectionWeight, 0.2f, "AG_RainbowWings.selectionWeight");
            Check.Eq(Check.Def<GeneDef>("AG_MothAntennae").selectionWeight, 0.02f, "AG_MothAntennae.selectionWeight");
            Check.Eq(Check.Def<GeneDef>("AG_Teratogenesis").selectionWeight, 0.02f, "AG_Teratogenesis.selectionWeight");
            Check.Eq(Check.Def<GeneDef>("AG_UnstableMutation").selectionWeight, 1f, "AG_UnstableMutation.selectionWeight (removed, back to default)");
        }

        [Test]
        public static void AnomalyGeneWeights()
        {
            if (!Check.Ready("alphagenes.genepacks", Ids.AlphaGenes, Ids.Anomaly))
                return;
            Check.Eq(Check.Def<GeneDef>("AG_DeadlifeProducer").selectionWeight, 1f, "AG_DeadlifeProducer.selectionWeight (removed, back to default)");
            Check.Eq(Check.Def<GeneDef>("AG_TwitchingToughspikes").selectionWeight, 0.02f, "AG_TwitchingToughspikes.selectionWeight");
        }

        [Test]
        public static void RoyaltyGeneWeights()
        {
            if (!Check.Ready("alphagenes.genepacks", Ids.AlphaGenes, Ids.Royalty))
                return;
            Check.Eq(Check.Def<GeneDef>("AG_Invisibility").selectionWeight, 1f, "AG_Invisibility.selectionWeight (removed, back to default)");
        }

        [Test]
        public static void XenogeneticsLabQuestFlavour()
        {
            if (!Check.Ready("genetics.alphagenes", Ids.AlphaGenes))
                return;
            Check.Eq(Check.Def<SitePartDef>("AG_AbandonedBiotechLab").label, "abandoned xenogenetics lab",
                "AG_AbandonedBiotechLab.label");
        }

        [Test]
        public static void GeneToolkitsCraftable()
        {
            if (!Check.Ready("genetics.agtools", Ids.AlphaGenes, Ids.Biotech) || !Check.GeneticsTabLoaded("genetics.agtools"))
                return;
            ResearchProjectDef toolkits = Check.Def<ResearchProjectDef>("RBP_GeneToolkits");
            Check.Eq(toolkits.baseCost, 2500f, "RBP_GeneToolkits.baseCost");
            Check.PrereqsAre(toolkits.prerequisites, "RBP_GeneToolkits.prerequisites", "GeneProcessor");
            foreach (string tool in new[] { "AG_GenepackTweaker", "AG_GenepackDisruptor", "AG_GeneRemover", "AG_Endogenefier",
                "AG_Xenogenefier", "AG_GermlineMutator", "AG_XenotypeNullifier", "AG_XenotypeInjector",
                "AG_ArchoGeneRemover", "AG_ArchoEndogenefier", "AG_ArchoXenogenefier" })
            {
                ThingDef def = Check.Def<ThingDef>(tool);
                Check.Eq(Check.RecipePrereq(def)?.defName, "RBP_GeneToolkits", $"{tool} recipeMaker.researchPrerequisite");
                Check.True(Check.CostOf(def, "Neutroamine") >= 4, $"{tool} costs no neutroamine");
                Check.True(Check.StatBase(def, "WorkToMake") >= 6000f, $"{tool} lacks WorkToMake");
            }
            foreach (string tool in new[] { "AG_ArchoGeneRemover", "AG_ArchoEndogenefier", "AG_ArchoXenogenefier" })
                Check.Eq(Check.CostOf(Check.Def<ThingDef>(tool), "ArchiteCapsule"), 1, $"{tool} costList[ArchiteCapsule]");
            Check.Eq(Check.StatBase(Check.Def<ThingDef>("AG_ArchoXenogenefier"), "MarketValue"), 500f,
                "AG_ArchoXenogenefier MarketValue");
        }

        [Test]
        public static void RandomGenepackSpawnerYieldsVanillaOnly()
        {
            if (!Check.Ready("alphagenes.genepacks", Ids.AlphaGenes))
                return;
            ThingDef spawner = Check.Def<ThingDef>("AG_RandomGenepack");
            CompProperties randomSpawner = null;
            foreach (CompProperties comp in spawner.comps)
                if (comp.GetType().Name.Contains("RandomItemSpawner"))
                    randomSpawner = comp;
            Check.True(randomSpawner != null, "AG_RandomGenepack has no CompProperties_RandomItemSpawner comp");
            IList items = (IList)Check.Field(randomSpawner, "items");
            Check.Eq(items.Count, 1, "AG_RandomGenepack spawner item count");
            object item = Check.Field(items[0], "item");
            Check.True(item is ThingDef d && d.defName == "Genepack",
                $"AG_RandomGenepack spawner item is '{(item as Def)?.defName ?? item?.ToString()}', expected 'Genepack'");
        }
    }
}
