using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsSenseTiersTests
    {
        private const string Key = "cybernetics.tiers";

        private const float Prosthetic = 0.80f;
        private const float Bionic = 1.15f;
        private const float Advanced = 1.35f;
        private const float Archotech = 1.50f;

        private static readonly float[] LadderRungs = { Prosthetic, Bionic, Advanced, Archotech };

        private static readonly Dictionary<string, float> ShippedAtALadderValue =
            new Dictionary<string, float>
            {
                { "CochlearImplantAnimal", 0.80f },
                { "LightReceptorAnimal", 0.80f },
                { "ArtificialNoseAnimal", 0.80f },
            };

        private const string DecorativeEye = "GoldenEye";

        private class Sense
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
        public static void SenseTierLadder()
        {
            if (!Check.Ready(Key))
                return;

            List<Sense> senses = Sweep(false);
            var swept = new List<string>();
            foreach (Sense sense in senses)
                swept.Add($"{sense.Item.defName}={sense.Actual:0.###}({sense.TierName})");
            Check.Note($"swept {senses.Count} artificial sense(s): " + string.Join(", ", swept.ToArray()));

            foreach (Sense sense in senses)
            {
                Check.Soft(Math.Abs(sense.Actual - sense.Tier) < 0.0005f,
                    $"{sense.Item.defName} ({sense.TierName} tier, replaces {sense.Parts} via " +
                    $"{sense.Install.defName}, from {ModOf(sense.Item)}) has partEfficiency " +
                    $"{sense.Actual:0.###}, ladder says {sense.Tier:0.###}");
            }

            Check.Soft(senses.Count >= 5,
                $"sweep found only {senses.Count} artificial senses; Core alone should contribute " +
                "five, so the sweep is no longer matching what it was written to match");

            Check.SoftResult();
        }

        [Test]
        public static void ModularSensesMatchTheirTier()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants))
                return;

            var covered = new List<string>();
            foreach (string name in new[] { "LTS_ModularBionicEye", "LTS_ModularBionicJaw", "LTS_ModularBionicNose" })
                AssertEfficiency(name, Bionic, covered);
            foreach (string name in new[] { "LTS_ModularArchotechEye", "LTS_ModularArchotechJaw", "LTS_ModularArchotechNose" })
                AssertEfficiency(name, Archotech, covered);

            Check.Note(covered.Count == 0
                ? "no modular sense present (Integrated Implants' conditional folder did not load, " +
                  "or EBSG Framework is absent)"
                : "modular senses checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void ArchotechSensesAreAcquisitionOnly()
        {
            if (!Check.Ready(Key))
                return;

            var covered = new List<string>();
            foreach (string name in new[] { "ArchotechEye", "LTS_ArchotechEar", "LTS_ArchotechJaw", "LTS_ArchotechNose" })
            {
                AssertEfficiency(name, Archotech, covered);
                ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (item == null)
                    continue;
                Check.Soft(item.recipeMaker == null,
                    $"{name} has gained a recipeMaker, so it is now crafted and its flat market value " +
                    $"of {item.BaseMarketValue:0.#} is no longer what it costs to make");
            }

            // Core's archotech eye is unconditional, so this list is never empty on any modlist.
            Check.Soft(covered.Count >= 1,
                "no archotech sense found at all; Core's ArchotechEye is unconditional, so the " +
                "database no longer contains what this test was written against");
            Check.Note("archotech senses checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void GoldenEyeIsStillJewellery()
        {
            if (!Check.Ready(Key, Ids.EPOEForked))
                return;

            HediffDef hediff = Check.Optional<HediffDef>(DecorativeEye, Key, Ids.EPOEForked);
            if (hediff == null)
                return;

            if (Check.Soft(hediff.addedPartProps != null, "GoldenEye has no addedPartProps"))
                Check.Soft(Math.Abs(hediff.addedPartProps.partEfficiency) < 0.0005f,
                    $"GoldenEye has partEfficiency {hediff.addedPartProps.partEfficiency:0.###}; it is a " +
                    "decorative eye and must stay at 0, or it becomes a cheaper light receptor");

            ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail(DecorativeEye);
            if (item == null)
                return;

            Check.Soft(Check.CostOf(item, "Gold") == 20,
                $"GoldenEye gold: {Show(Check.CostOf(item, "Gold"))}");

            ThingDef receptor = DefDatabase<ThingDef>.GetNamedSilentFail("LightReceptor");
            if (receptor != null)
                Check.Soft(item.BaseMarketValue < receptor.BaseMarketValue && item.BaseMarketValue > 100f,
                    $"the golden eye costs {item.BaseMarketValue:0.#} against the light receptor's " +
                    $"{receptor.BaseMarketValue:0.#}; it should sit below a working eye but well above " +
                    "the 15.4 that made its two stat offsets free");

            Check.SoftResult();
        }

        [Test]
        public static void RecostedSenses()
        {
            if (!Check.Ready(Key, Ids.Royalty))
                return;

            ThingDef nose = Check.Optional<ThingDef>("AestheticNose", Key, Ids.Royalty);
            if (nose == null)
                return;

            Check.Soft(Check.CostOf(nose, "Plasteel") == 15,
                $"AestheticNose plasteel: {Show(Check.CostOf(nose, "Plasteel"))}");
            Check.Soft(Check.CostOf(nose, "ComponentSpacer") == 3,
                $"AestheticNose advanced components: {Show(Check.CostOf(nose, "ComponentSpacer"))}");

            ThingDef shaper = DefDatabase<ThingDef>.GetNamedSilentFail("AestheticShaper");
            if (shaper != null)
                Check.Soft(Math.Abs(nose.BaseMarketValue - shaper.BaseMarketValue) < 25f,
                    $"the aesthetic nose costs {nose.BaseMarketValue:0.#} against the aesthetic shaper's " +
                    $"{shaper.BaseMarketValue:0.#}; both grant PawnBeauty +1 and nothing else, so they " +
                    "belong on the same rung");

            ThingDef gastro = DefDatabase<ThingDef>.GetNamedSilentFail("GastroAnalyzer");
            if (gastro != null)
                Check.Soft(Check.CostOf(gastro, "ComponentSpacer") == 3,
                    $"the gastro-analyzer defines the bolt-on rung and now costs " +
                    $"{Show(Check.CostOf(gastro, "ComponentSpacer"))} advanced components instead of 3");

            Check.SoftResult();
        }

        [Test]
        public static void AnimalSensesStayOffTheLadder()
        {
            if (!Check.Ready(Key, Ids.ADogSaid2))
                return;

            List<Sense> laddered = Sweep(false);
            List<Sense> animal = Sweep(true);

            var seen = new List<string>();
            foreach (Sense sense in animal)
                seen.Add($"{sense.Item.defName}={sense.Actual:0.###}");
            Check.Note($"found {animal.Count} animal sense(s): " + string.Join(", ", seen.ToArray()));

            foreach (Sense sense in animal)
            {
                foreach (Sense other in laddered)
                {
                    if (other.Item != sense.Item)
                        continue;
                    Check.Soft(false,
                        $"{sense.Item.defName} is an animal sense ({string.Join(",", CategoryNames(sense.Item))}) " +
                        "but the human ladder sweep also returned it - the category exclusion has stopped working");
                }

                float shipped;
                if (ShippedAtALadderValue.TryGetValue(sense.Item.defName, out shipped))
                {
                    Check.Soft(Math.Abs(sense.Actual - shipped) < 0.0005f,
                        $"{sense.Item.defName} has partEfficiency {sense.Actual:0.###}; A Dog Said " +
                        $"ships it at {shipped:0.##} and this pass does not retier animal senses");
                    continue;
                }

                foreach (float rung in LadderRungs)
                {
                    Check.Soft(Math.Abs(sense.Actual - rung) >= 0.0005f,
                        $"{sense.Item.defName} is an animal sense but its partEfficiency " +
                        $"{sense.Actual:0.###} is exactly a rung of the human ladder - an operation in " +
                        "CyberneticsSenseTiers.xml has reached it");
                }
            }

            Check.Soft(animal.Count >= 4,
                $"found only {animal.Count} animal senses with A Dog Said 2 active; it ships an animal " +
                "eye, ear, nose, tongue, cochlear implant and light receptor, so the category sweep " +
                "has stopped finding them and this test asserted almost nothing");

            Check.SoftResult();
        }

        [Test]
        public static void BionicFaceplateSitsOnTheImplantRung()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants))
                return;

            ThingDef item = Check.Optional<ThingDef>("LTS_FaceplateArachnid", Key, Ids.IntegratedImplants);
            if (item == null)
            {
                Check.Note("LTS_FaceplateArachnid is absent - Integrated Implants loads its faceplates " +
                    "from a conditional folder, so this is legitimate and nothing was asserted");
                return;
            }

            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail("LTS_FaceplateArachnid");
            RecipeDef install = InstallRecipeFor(hediff);

            // 1. It is an implant, not a replacement. This is the whole basis of the rung.
            if (install != null)
            {
                Check.Soft(install.workerClass != null
                        && typeof(Recipe_InstallImplant).IsAssignableFrom(install.workerClass)
                        && !typeof(Recipe_InstallArtificialBodyPart).IsAssignableFrom(install.workerClass),
                    $"the bionic faceplate installs through {install.workerClass?.Name ?? "no worker"}; it " +
                    "was priced on the implant rung (830 plus one bolt-on each) precisely because it " +
                    "replaces nothing, and a replacement would belong on the efficiency ladder instead");
                Check.Soft(PartNames(install).Contains("Head"),
                    $"the bionic faceplate now installs on {string.Join("/", PartNames(install).ToArray())} " +
                    "rather than the Head");
            }
            else
            {
                Check.Soft(false, "no install surgery adds LTS_FaceplateArachnid, so its classification " +
                    "could not be checked at all");
            }

            if (hediff != null)
            {
                Check.Soft(hediff.addedPartProps == null,
                    "the bionic faceplate now declares addedPartProps, so it is an added part with a " +
                    "live efficiency; its 2030 was derived as an implant carrying four bolt-ons and " +
                    "that derivation no longer describes it");
                Check.Soft(SightOffsetOf(hediff) > 0.4f,
                    $"the bionic faceplate's Sight offset is {SightOffsetOf(hediff):0.##}; two of its four " +
                    "priced bolt-ons are that offset (ShootingAccuracyPawn +6 and MeleeHitChance +6 at " +
                    "Sight 1.5, read off Core's capacityOffsets), so a change here changes the price");
            }

            // 2. The rung, as ingredients and work rather than as a market value.
            Check.Soft(Check.CostOf(item, "Plasteel") == 15,
                $"LTS_FaceplateArachnid plasteel: {Show(Check.CostOf(item, "Plasteel"))}; the implant " +
                "rung is 15 plasteel plus components");

            float componentValue = ComponentValueOf(item);
            Check.Soft(Math.Abs(componentValue - 1800f) < 0.51f,
                $"LTS_FaceplateArachnid carries {componentValue:0} silver of components " +
                $"({Show(Check.CostOf(item, "ComponentSpacer"))} advanced + " +
                $"{Show(Check.CostOf(item, "gitsMicromachines"))} micromachine(s)); the rung is nine " +
                "advanced components' worth, 1800 - 830 for the implant and its first bolt-on, plus " +
                "400 each for dark vision and for Sight +0.5 counted twice");

            float work = item.GetStatValueAbstract(StatDefOf.WorkToMake);
            Check.Soft(Math.Abs(work - 26000f) < 1f,
                $"LTS_FaceplateArachnid work is {work:0}, expected the bionic tier's 26000 - the 2030 " +
                "figure is ingredients plus 0.0036 x work and moves if either does");

            ThingDef gastro = DefDatabase<ThingDef>.GetNamedSilentFail("GastroAnalyzer");
            if (gastro != null)
                Check.Soft(item.BaseMarketValue > gastro.BaseMarketValue,
                    $"the faceplate is {item.BaseMarketValue:0.#} against the gastro-analyzer's " +
                    $"{gastro.BaseMarketValue:0.#}; both are implants with no efficiency, but the " +
                    "faceplate carries four bolt-ons to the analyzer's one");

            ThingDef cornea = DefDatabase<ThingDef>.GetNamedSilentFail("TacticalCorneaImplant");
            if (cornea != null)
                Check.Soft(item.BaseMarketValue > cornea.BaseMarketValue,
                    $"the faceplate is {item.BaseMarketValue:0.#} against the tactical cornea implant's " +
                    $"{cornea.BaseMarketValue:0.#}. The cornea implant is the same shape - an implant on " +
                    "a sense site whose value is combat accuracy - and grants ShootingAccuracyPawn +5 / " +
                    "MeleeHitChance +3 / MeleeDodgeChance +3, strictly less than the faceplate's sight " +
                    "package, before the faceplate's toxin filter and dark vision are counted");

            Check.Note($"bionic faceplate: {item.BaseMarketValue:0.#} silver, " +
                $"{Show(Check.CostOf(item, "Plasteel"))} plasteel + " +
                $"{Show(Check.CostOf(item, "ComponentSpacer"))} advanced component(s) + " +
                $"{Show(Check.CostOf(item, "gitsMicromachines"))} micromachine(s) " +
                $"= {componentValue:0} of components, {work:0} work");
            Check.SoftResult();
        }

        private static RecipeDef InstallRecipeFor(HediffDef hediff)
        {
            if (hediff == null)
                return null;
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                if (recipe.addsHediff == hediff && recipe.IsSurgery)
                    return recipe;
            return null;
        }

        private static List<string> PartNames(RecipeDef recipe)
        {
            var names = new List<string>();
            if (recipe?.appliedOnFixedBodyParts == null)
                return names;
            foreach (BodyPartDef part in recipe.appliedOnFixedBodyParts)
                if (part != null && !names.Contains(part.defName))
                    names.Add(part.defName);
            return names;
        }

        private static float SightOffsetOf(HediffDef hediff)
        {
            if (hediff?.stages == null)
                return 0f;
            var total = 0f;
            foreach (HediffStage stage in hediff.stages)
            {
                if (stage?.capMods == null)
                    continue;
                foreach (PawnCapacityModifier mod in stage.capMods)
                    if (mod?.capacity != null && mod.capacity.defName == "Sight" && mod.offset > total)
                        total = mod.offset;
            }
            return total;
        }

        private static float ComponentValueOf(ThingDef def)
        {
            ThingDef spacer = DefDatabase<ThingDef>.GetNamedSilentFail("ComponentSpacer");
            ThingDef micro = DefDatabase<ThingDef>.GetNamedSilentFail("gitsMicromachines");
            float value = (Check.CostOf(def, "ComponentSpacer") ?? 0) * (spacer?.BaseMarketValue ?? 200f);
            if (micro != null)
                value += (Check.CostOf(def, "gitsMicromachines") ?? 0) * micro.BaseMarketValue;
            return value;
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

        private static List<Sense> Sweep(bool animalOnly)
        {
            var found = new List<Sense>();
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
                    if (IsSensePart(bare) && !names.Contains(bare))
                        names.Add(bare);
                }
                if (names.Count == 0)
                    continue;

                ThingDef item = hediff.spawnThingOnRemoved;
                if (item.techLevel < TechLevel.Industrial)
                    continue;
                if (item.defName == DecorativeEye)
                    continue;

                bool isAnimal = HasCategory(item, "BodyPartsAnimalArtificial");
                if (isAnimal != animalOnly)
                    continue;

                float tier;
                string tierName;
                if (!TierOf(item, out tier, out tierName) && !animalOnly)
                    continue;

                found.Add(new Sense
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

        private static bool IsSensePart(string bare) =>
            bare == "Eye" || bare == "Ear" || bare == "Nose" || bare == "Jaw" || bare == "Tongue";

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
                    case "BodyPartsProsthetic": tier = Prosthetic; tierName = "prosthetic"; return true;
                    case "BodyPartsBionic": tier = Bionic; tierName = "bionic"; return true;
                    case "BodyPartsUltra": tier = Advanced; tierName = "advanced"; return true;
                    case "BodyPartsArchotech": tier = Archotech; tierName = "archotech"; return true;
                }
            }
            return false;
        }
    }
}
