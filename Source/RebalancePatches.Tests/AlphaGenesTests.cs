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
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.alphagenes", Ids.AlphaGenes))
                return;
            Check.Eq(Check.Def<XenotypeDef>("AG_Efreet").label, "cindrid", "AG_Efreet label");
            Check.Eq(Check.Def<XenotypeDef>("AG_Nereid").label, "abyssid", "AG_Nereid label");
            Check.Eq(Check.Def<XenotypeDef>("AG_Drakonori").label, "drakonori", "AG_Drakonori label unchanged");
        }

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
        public static void NeanderthalFrostbiteResistance()
        {
            if (!Check.Ready("genetics.neanderthalfrost", Ids.AlphaGenes, Ids.Biotech))
                return;
            Check.XenoGene("Neanderthal", "AG_FrostbiteResistance");
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
            if (!Check.Ready("geneticsresearch.alphagenes", Ids.AlphaGenes))
                return;
            Check.Eq(Check.Def<SitePartDef>("AG_AbandonedBiotechLab").label, "abandoned xenogenetics lab",
                "AG_AbandonedBiotechLab.label");
        }

        [Test]
        public static void GeneToolkitsCraftable()
        {
            if (!Check.Ready("geneticsresearch.consumables", Ids.AlphaGenes, Ids.Biotech) || !Check.GeneticsTabLoaded("geneticsresearch.consumables"))
                return;
            ResearchProjectDef toolkits = Check.Def<ResearchProjectDef>("RBP_GeneToolkits");
            Check.Eq(toolkits.baseCost, 2500f, "RBP_GeneToolkits.baseCost");
            Check.Eq(toolkits.techLevel, TechLevel.Spacer, "RBP_GeneToolkits.techLevel");
            Check.PrereqsAre(toolkits.prerequisites, "RBP_GeneToolkits.prerequisites", "GeneProcessor");
            foreach (string tool in new[] { "AG_GenepackTweaker", "AG_GenepackDisruptor", "AG_GeneRemover", "AG_Endogenefier",
                "AG_Xenogenefier", "AG_XenotypeNullifier", "AG_XenotypeInjector" })
            {
                ThingDef def = Check.Def<ThingDef>(tool);
                Check.Eq(Check.RecipePrereq(def)?.defName, "RBP_GeneToolkits", $"{tool} recipeMaker.researchPrerequisite");
                Check.True(Check.CostOf(def, "Neutroamine") >= 4, $"{tool} costs no neutroamine");
                Check.True(Check.StatBase(def, "WorkToMake") >= 6000f, $"{tool} lacks WorkToMake");
            }
            // The archotech variants carry the pair as a list, not the singular field, so
            // archogenetics gates them alongside the toolkits project itself.
            foreach (string tool in new[] { "AG_ArchoGeneRemover", "AG_ArchoEndogenefier", "AG_ArchoXenogenefier" })
            {
                ThingDef def = Check.Def<ThingDef>(tool);
                Check.PrereqsAre(Check.RecipePrereqs(def), $"{tool} recipeMaker.researchPrerequisites",
                    "RBP_GeneToolkits", "Archogenetics");
                // The same pair has to sit on the ThingDef, not just on recipeMaker: research-tree
                // UIs read ThingDef.researchPrerequisites to decide which "unlocked with" group an
                // item belongs to, and without it the item shows as ungated next to its own recipe.
                Check.PrereqsAre(def.researchPrerequisites, $"{tool} researchPrerequisites",
                    "RBP_GeneToolkits", "Archogenetics");
                Check.Eq(Check.CostOf(def, "ArchiteCapsule"), 1, $"{tool} costList[ArchiteCapsule]");
                Check.True(Check.StatBase(def, "WorkToMake") >= 6000f, $"{tool} lacks WorkToMake");
            }
            Check.Eq(Check.StatBase(Check.Def<ThingDef>("AG_ArchoXenogenefier"), "MarketValue"), 500f,
                "AG_ArchoXenogenefier MarketValue");
        }

        [Test]
        public static void GermlineMutatorRemoved()
        {
            if (!Check.Ready("geneticsresearch.consumables", Ids.AlphaGenes, Ids.Biotech, Ids.CherryPicker)
                || !Check.GeneticsTabLoaded("geneticsresearch.consumables"))
                return;
            Check.ThingUnobtainable("AG_GermlineMutator");
            // Cherry Picker does not know about Alpha Genes' custom spawner comp, so its dispenser
            // entry is ours to strip - otherwise the dispenser keeps handing the tool out.
            Check.True(!DispenserOffers("AG_GermlineMutator"), "AG_RandomGeneTool still offers AG_GermlineMutator");
        }

        /// <summary>
        /// True when Alpha Genes' random gene tool dispenser still lists <paramref name="thingDefName"/>
        /// in its weighted spawn table. Read reflectively: the comp type lives in Alpha Genes.
        /// </summary>
        internal static bool DispenserOffers(string thingDefName)
        {
            ThingDef dispenser = DefDatabase<ThingDef>.GetNamedSilentFail("AG_RandomGeneTool");
            if (dispenser?.comps == null)
                return false;
            foreach (CompProperties props in dispenser.comps)
            {
                if (props.GetType().Name != "CompProperties_RandomItemSpawner")
                    continue;
                if (!(Check.Field(props, "items") is IEnumerable items))
                    continue;
                foreach (object entry in items)
                {
                    if (Check.Field(entry, "item") is ThingDef item && item.defName == thingDefName)
                        return true;
                }
            }
            return false;
        }

        [Test]
        public static void SummonGenesRemoved()
        {
            if (!Check.Ready("genetics.agsummons", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            foreach (string name in new[] { "AG_AnimalSummon_Randomizer", "AG_MajorAnimalSummon_Randomizer",
                "AG_MinorAnimalSummon_Randomizer", "AG_SummonTempBandwidth" })
                Check.True(DefDatabase<GeneDef>.GetNamedSilentFail(name) == null, $"{name} still present");
            foreach (GeneDef def in DefDatabase<GeneDef>.AllDefsListForReading)
                Check.True(!def.defName.StartsWith("AlphaGenes_AnimalSummon_") && !def.defName.StartsWith("AlphaGenes_Animal_Summon"),
                    $"summon gene {def.defName} still present");
        }

        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.GenesGone("AG_Painless", "AG_SmallerBodySize", "AG_LargerBodySize", "AG_Male", "AG_Female",
                "AG_AcidImmunity", "AG_AcidWeakness", "AG_ToughSinews", "AG_ArmourMinor",
                "AG_InnateMechlink", "AG_InnatePsylink", "AG_NearBlindness");
            if (ModsConfig.IsActive(Ids.VREPigskin))
                Check.GenesGone("AG_FasterAging", "AG_SlowerAging");
            if (ModsConfig.IsActive(Ids.VREArchon))
                Check.GenesGone("AG_FastGestation", "AG_SlowGestation");
            if (ModsConfig.IsActive(Ids.VRESaurid))
                Check.GenesGone("AG_EggLaying");
            if (ModsConfig.IsActive(Ids.VREPhytokin))
                Check.GenesGone("AG_LightSustenance");
            if (ModsConfig.IsActive(Ids.VRELycanthrope))
                Check.GenesGone("AG_NightOwl");
            if (ModsConfig.IsActive(Ids.VREInsector))
                Check.GenesGone("AG_VFEI_VatGrownInsectoidSkin");
            if (ModsConfig.IsActive(Ids.VREWaster))
                Check.GenesGone("AG_Instability_Lethal");
            if (ModsConfig.IsActive(Ids.IntegratedImplants))
                Check.GenesGone("AG_DecreasedCommandRange");
            Check.True(DefDatabase<GeneDef>.GetNamedSilentFail("AG_HeatImmunity") != null, "AG_HeatImmunity (canonical) missing");
            Check.True(DefDatabase<GeneDef>.GetNamedSilentFail("AG_ColdImmunity") != null, "AG_ColdImmunity (canonical) missing");
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

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker))
                return;
            Check.XenoGene("AG_Malachai", "BS_LargeFrame");
            Check.XenoGene("AG_Malachai", "BS_Pain_None");
            // AG_RoxTouched only exists with Medieval Overhaul active (Alpha Genes conditional content).
            if (DefDatabase<RimWorld.XenotypeDef>.GetNamedSilentFail("AG_RoxTouched") != null)
                Check.XenoGene("AG_RoxTouched", "BS_LargeFrame");
            Check.XenoGene("AG_Taukai", "BS_Pain_None");
            Check.XenoGene("VRE_Ocularkin", "Body_FemaleOnly");
            if (ModsConfig.IsActive(Ids.VRESaurid))
                Check.XenoGene("AG_Drakonori", "VRESaurids_Oviparous");
            if (ModsConfig.IsActive(Ids.VREArchon))
                Check.XenoGene("AG_Lapis", "VRE_LongPregnancy");
            if (ModsConfig.IsActive(Ids.VREFungoid))
                Check.XenoGene("AG_Mycormorph", "WVC_NaturalTelepathy");
        }

        [Test]
        public static void AngelicBeautyRenamed()
        {
            if (!Check.Ready("alphagenes.beautyrename", Ids.AlphaGenes, Ids.WVC))
                return;
            Check.Eq(Check.Def<GeneDef>("AG_Beauty_Angelic").label, "uncanny beauty", "AG_Beauty_Angelic label");
        }
    }
}
