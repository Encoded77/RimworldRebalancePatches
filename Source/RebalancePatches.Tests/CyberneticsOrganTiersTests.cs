using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsOrganTiersTests
    {
        private const string Key = "cybernetics.tiers";

        private const float Surrogate = 0.80f;
        private const float Synthetic = 1.15f;
        private const float Advanced = 1.35f;
        private const float Archotech = 1.50f;

        private static readonly float[] LadderRungs = { Surrogate, Synthetic, Advanced, Archotech };

        private class Organ
        {
            public RecipeDef Install;
            public HediffDef Hediff;
            public ThingDef Item;
            public float Tier;
            public string TierName;
            public string Parts;

            public float Actual => Hediff.addedPartProps.partEfficiency;
        }

        [Test]
        public static void OrganTierLadder()
        {
            if (!Check.Ready(Key, Ids.EPOEForked))
                return;

            List<Organ> organs = Sweep(false);
            var swept = new List<string>();
            foreach (Organ organ in organs)
                swept.Add($"{organ.Item.defName}={organ.Actual:0.###}({organ.TierName})");
            Check.Note($"swept {organs.Count} artificial organ(s): " + string.Join(", ", swept.ToArray()));

            foreach (Organ organ in organs)
            {
                Check.Soft(Math.Abs(organ.Actual - organ.Tier) < 0.0005f,
                    $"{organ.Item.defName} ({organ.TierName} tier, replaces {organ.Parts} via " +
                    $"{organ.Install.defName}, from {ModOf(organ.Item)}) has partEfficiency " +
                    $"{organ.Actual:0.###}, ladder says {organ.Tier:0.###}");
            }

            Check.Soft(organs.Count >= 10,
                $"sweep found only {organs.Count} artificial organs; EPOE-Forked alone contributes " +
                "five surrogate and five synthetic ones, so the sweep is no longer matching what it " +
                "was written to match");

            Check.SoftResult();
        }

        [Test]
        public static void ModularOrgansMatchTheirTier()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants))
                return;

            var covered = new List<string>();
            foreach (string name in new[] { "LTS_ModularBionicKidney", "LTS_ModularBionicLung", "LTS_ModularBionicStomach" })
                AssertEfficiency(name, Synthetic, covered);
            foreach (string name in new[] { "LTS_ModularArchotechKidney", "LTS_ModularArchotechLung", "LTS_ModularArchotechStomach" })
                AssertEfficiency(name, Archotech, covered);

            Check.Note(covered.Count == 0
                ? "no modular organ present (Integrated Implants' conditional folders did not load)"
                : "modular organs checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void AnimalOrgansStayOffTheLadder()
        {
            if (!Check.Ready(Key, Ids.ADogSaid2))
                return;

            List<Organ> laddered = Sweep(false);
            List<Organ> animal = Sweep(true);

            var seen = new List<string>();
            foreach (Organ organ in animal)
                seen.Add($"{organ.Item.defName}={organ.Actual:0.###}");
            Check.Note($"found {animal.Count} animal organ(s): " + string.Join(", ", seen.ToArray()));

            foreach (Organ organ in animal)
            {
                foreach (Organ other in laddered)
                {
                    if (other.Item != organ.Item)
                        continue;
                    Check.Soft(false,
                        $"{organ.Item.defName} is an animal organ ({string.Join(",", CategoryNames(organ.Item))}) " +
                        $"but the human ladder sweep also returned it as {other.TierName} tier - the " +
                        "category exclusion has stopped working");
                }

                foreach (float rung in LadderRungs)
                {
                    Check.Soft(Math.Abs(organ.Actual - rung) >= 0.0005f,
                        $"{organ.Item.defName} is an animal organ but its partEfficiency " +
                        $"{organ.Actual:0.###} is exactly a rung of the human ladder - an operation in " +
                        "CyberneticsOrganTiers.xml has reached it");
                }
            }

            Check.Soft(animal.Count >= 10,
                $"found only {animal.Count} animal organs with A Dog Said 2 active; it ships twelve, " +
                "so the category sweep has stopped finding them and this test asserted almost nothing");

            Check.SoftResult();
        }

        [Test]
        public static void ArchotechOrgansAreAcquisitionOnly()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants))
                return;

            var covered = new List<string>();
            foreach (string name in new[]
            {
                "LTS_ArchotechHeart", "LTS_ArchotechLung", "LTS_ArchotechLiver",
                "LTS_ArchotechKidney", "LTS_ArchotechStomach",
            })
            {
                AssertEfficiency(name, Archotech, covered);
                ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (item == null)
                    continue;
                Check.Soft(item.recipeMaker == null,
                    $"{name} has gained a recipeMaker, so it is now crafted and its flat market value " +
                    $"of {item.BaseMarketValue:0.#} is no longer what it costs to make");
            }

            Check.Note(covered.Count == 0
                ? "no archotech organ present (Integrated Implants' ArchotechProstheticsAbsent folder " +
                  "did not load - Archotech Expanded: Prosthetics is active)"
                : "archotech organs checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void RecostedOrgans()
        {
            if (!Check.Ready(Key))
                return;

            var covered = new List<string>();

            foreach (string name in new[] { "Immunoenhancer", "LoveEnhancer" })
            {
                ThingDef def = Check.Optional<ThingDef>(name, Key, Ids.Royalty);
                if (def == null)
                    continue;
                covered.Add(name);
                Check.Soft(Check.CostOf(def, "ComponentSpacer") == 3,
                    $"{name} advanced components: {Show(Check.CostOf(def, "ComponentSpacer"))}");

                ThingDef shaper = DefDatabase<ThingDef>.GetNamedSilentFail("AestheticShaper");
                if (shaper != null)
                    Check.Soft(Math.Abs(def.BaseMarketValue - shaper.BaseMarketValue) < 25f,
                        $"{name} costs {def.BaseMarketValue:0.#} against the aesthetic shaper's " +
                        $"{shaper.BaseMarketValue:0.#}; both are one benefit on an organ site with no " +
                        "replacement and should sit on the same rung");
            }

            foreach (string name in new[] { "DetoxifierLung", "DetoxifierKidney" })
            {
                ThingDef def = Check.Optional<ThingDef>(name, Key, Ids.Biotech);
                if (def == null)
                    continue;
                covered.Add(name);
                Check.Soft(ComponentValueOf(def) == 1200,
                    $"{name} component value: {ComponentsShown(def)}");
            }

            foreach (string name in new[] { "DetoxifierStomach", "ReprocessorStomach" })
            {
                ThingDef def = Check.Optional<ThingDef>(name, Key, Ids.Royalty);
                if (def == null)
                    continue;
                covered.Add(name);
                Check.Soft(ComponentValueOf(def) == 1200,
                    $"{name} component value: {ComponentsShown(def)}");

                ThingDef detoxLungRung = DefDatabase<ThingDef>.GetNamedSilentFail("DetoxifierLung");
                if (detoxLungRung != null)
                    Check.Soft(Math.Abs(def.BaseMarketValue - detoxLungRung.BaseMarketValue) < 25f,
                        $"{name} costs {def.BaseMarketValue:0.#} against the detoxifier lung's " +
                        $"{detoxLungRung.BaseMarketValue:0.#}; both are an organ replacement plus a " +
                        "benefit and should sit on the same rung");
            }

            ThingDef nuclear = Check.Optional<ThingDef>("NuclearStomach", Key, Ids.Royalty);
            if (nuclear != null)
            {
                covered.Add("NuclearStomach");
                Check.Soft(ComponentValueOf(nuclear) == 800,
                    "NuclearStomach must stay one rung below the other two - its hunger reduction " +
                    "is paired with a carcinoma risk, so it is a trade rather than an upgrade; " +
                    "component value: " + ComponentsShown(nuclear));
            }

            ThingDef ironLung = Check.Optional<ThingDef>("USH_IronLung", Key, Ids.UshankaBioWarfare);
            if (ironLung != null)
            {
                covered.Add("USH_IronLung");
                Check.Soft(ComponentValueOf(ironLung) == 1200,
                    $"USH_IronLung component value: {ComponentsShown(ironLung)}");
                Check.Soft(Check.CostOf(ironLung, "Steel") == null,
                    $"USH_IronLung still costs {Show(Check.CostOf(ironLung, "Steel"))} steel, which puts it " +
                    "off the rung it was moved onto");

                ThingDef detoxLung = DefDatabase<ThingDef>.GetNamedSilentFail("DetoxifierLung");
                if (detoxLung != null)
                    Check.Soft(Math.Abs(ironLung.BaseMarketValue - detoxLung.BaseMarketValue) < 25f,
                        $"the iron lung is the detoxifier lung with the same efficiency and the same " +
                        $"ToxicEnvironmentResistance, yet costs {ironLung.BaseMarketValue:0.#} against " +
                        $"{detoxLung.BaseMarketValue:0.#}");
            }

            ThingDef detoxLiver = Check.Optional<ThingDef>("EPOE_DetoxifierSyntheticLiver", Key, Ids.EPOEForkedRoyalty);
            ThingDef plainLiver = DefDatabase<ThingDef>.GetNamedSilentFail("SyntheticLiver");
            if (detoxLiver != null && plainLiver != null)
            {
                covered.Add("EPOE_DetoxifierSyntheticLiver");
                Check.Soft(ComponentValueOf(detoxLiver) == 1200,
                    $"the specialist rung is defined by EPOE_DetoxifierSyntheticLiver, whose component " +
                    $"value is now {ComponentsShown(detoxLiver)} instead of 1200 - every organ moved " +
                    "onto that rung is now off it");
                Check.Soft(detoxLiver.BaseMarketValue > plainLiver.BaseMarketValue,
                    $"an organ carrying an extra stat must cost more than the plain one: " +
                    $"{detoxLiver.BaseMarketValue:0.#} vs {plainLiver.BaseMarketValue:0.#}");
            }

            Check.Note(covered.Count == 0
                ? "no re-costed organ present (Royalty, Biotech, Ushankas Biological Warfare and " +
                  "EPOE-Forked: Royalty all absent)"
                : "re-costed organs checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        private static int ComponentValueOf(ThingDef def)
        {
            int spacers = Check.CostOf(def, "ComponentSpacer") ?? 0;
            int micromachines = Check.CostOf(def, "gitsMicromachines") ?? 0;
            return spacers * 200 + micromachines * 800;
        }

        private static string ComponentsShown(ThingDef def)
        {
            int spacers = Check.CostOf(def, "ComponentSpacer") ?? 0;
            int micromachines = Check.CostOf(def, "gitsMicromachines") ?? 0;
            return $"{ComponentValueOf(def)} ({spacers} advanced component(s) + " +
                $"{micromachines} micromachine(s))";
        }

        private static string Show(int? value) => value.HasValue ? value.Value.ToString() : "absent";

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "unknown mod";

        private static List<string> CategoryNames(ThingDef item)
        {
            var names = new List<string>();
            if (item.thingCategories == null)
                return names;
            foreach (ThingCategoryDef category in item.thingCategories)
                if (category != null)
                    names.Add(category.defName);
            return names;
        }

        private static void AssertEfficiency(string hediffName, float expected, List<string> covered)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (hediff == null)
                return;
            covered.Add(hediffName);
            if (!Check.Soft(hediff.addedPartProps != null, $"{hediffName} has no addedPartProps"))
                return;
            Check.Soft(Math.Abs(hediff.addedPartProps.partEfficiency - expected) < 0.0005f,
                $"{hediffName} has partEfficiency {hediff.addedPartProps.partEfficiency:0.###}, " +
                $"ladder says {expected:0.###}");
        }

        private static List<Organ> Sweep(bool animalOnly)
        {
            var found = new List<Organ>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.workerClass == null
                    || !typeof(Recipe_InstallArtificialBodyPart).IsAssignableFrom(recipe.workerClass))
                    continue;
                HediffDef hediff = recipe.addsHediff;
                if (hediff?.addedPartProps == null || hediff.spawnThingOnRemoved == null)
                    continue;
                if (recipe.appliedOnFixedBodyParts == null || recipe.appliedOnFixedBodyParts.Count == 0)
                    continue;

                var names = new List<string>();
                foreach (BodyPartDef part in recipe.appliedOnFixedBodyParts)
                {
                    if (part == null)
                        continue;
                    string bare = part.defName.StartsWith("BS_Mechanical")
                        ? part.defName.Substring("BS_Mechanical".Length)
                        : part.defName;
                    if (IsOrganPart(bare) && !names.Contains(bare))
                        names.Add(bare);
                }
                if (names.Count == 0)
                    continue;

                ThingDef item = hediff.spawnThingOnRemoved;
                if (item.techLevel < TechLevel.Industrial)
                    continue;

                bool isAnimal = HasCategory(item, "BodyPartsAnimalArtificial");
                if (isAnimal != animalOnly)
                    continue;

                float tier;
                string tierName;
                if (!TierOf(item, out tier, out tierName) && !animalOnly)
                    continue;

                found.Add(new Organ
                {
                    Install = recipe,
                    Hediff = hediff,
                    Item = item,
                    Tier = tier,
                    TierName = tierName ?? "off-ladder",
                    Parts = string.Join("/", names.ToArray()),
                });
            }
            return found;
        }

        private static bool IsOrganPart(string bare) =>
            bare == "Heart" || bare == "Lung" || bare == "Liver" || bare == "Kidney" || bare == "Stomach";

        private static bool HasCategory(ThingDef item, string categoryDefName)
        {
            if (item.thingCategories == null)
                return false;
            foreach (ThingCategoryDef category in item.thingCategories)
                if (category?.defName == categoryDefName)
                    return true;
            return false;
        }

        private static bool TierOf(ThingDef item, out float tier, out string tierName)
        {
            tier = 0f;
            tierName = null;
            if (item.thingCategories == null)
                return false;
            foreach (ThingCategoryDef category in item.thingCategories)
            {
                switch (category?.defName)
                {
                    case "BodyPartsProsthetic": tier = Surrogate; tierName = "surrogate"; return true;
                    case "BodyPartsBionic": tier = Synthetic; tierName = "synthetic"; return true;
                    case "BodyPartsUltra": tier = Advanced; tierName = "advanced"; return true;
                    case "BodyPartsArchotech": tier = Archotech; tierName = "archotech"; return true;
                }
            }
            return false;
        }
    }
}
