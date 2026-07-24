using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsTorsoFrameTiersTests
    {
        private const string Key = "cybernetics.tiers";

        private const float Prosthetic = 0.80f;
        private const float Bionic = 1.15f;
        private const float Advanced = 1.35f;
        private const float Archotech = 1.50f;

        private const float Tolerance = 0.0005f;

        private class Spine
        {
            public RecipeDef Install;
            public HediffDef Hediff;
            public ThingDef Item;
            public float Tier;
            public string TierName;

            public float Actual => Hediff.addedPartProps.partEfficiency;
        }

        [Test]
        public static void BodyPartTagsStillSupportTheReasoning()
        {
            if (!Check.Ready(Key))
                return;

            BodyPartDef spine = DefDatabase<BodyPartDef>.GetNamedSilentFail("Spine");
            if (Check.Soft(spine != null, "BodyPartDef 'Spine' not found"))
                Check.Soft(HasTag(spine, "Spine"),
                    "the Spine body part no longer carries the Spine tag, which is what "
                    + "PawnCapacityWorker_Moving multiplies by - a spine's efficiency would now be "
                    + $"inert and the spine prices are wrong. Tags: {TagsOf(spine)}");

            foreach (string name in new[] { "Ribcage", "Sternum" })
            {
                BodyPartDef part = DefDatabase<BodyPartDef>.GetNamedSilentFail(name);
                if (!Check.Soft(part != null, $"BodyPartDef '{name}' not found"))
                    continue;
                Check.Soft(HasTag(part, "BreathingSourceCage"),
                    $"the {name} body part no longer carries BreathingSourceCage. That tag is read "
                    + "with maximum 1f, which is the reason a thoracic frame is priced for its "
                    + $"module slots and not for its efficiency. Tags: {TagsOf(part)}");
            }

            BodyPartDef torso = DefDatabase<BodyPartDef>.GetNamedSilentFail("Torso");
            if (Check.Soft(torso != null, "BodyPartDef 'Torso' not found"))
            {
                Check.Note($"Torso tags: {TagsOf(torso)}");
                Check.Soft(torso.tags == null || torso.tags.Count == 0,
                    "the Torso body part has gained a tag. No capacity read it before, which is why "
                    + "no torso implant in this group has a rung; if a capacity reads it now, every "
                    + $"torso implant needs one. Tags: {TagsOf(torso)}");
            }

            Check.SoftResult();
        }

        [Test]
        public static void SpineLadder()
        {
            if (!Check.Ready(Key))
                return;

            List<Spine> spines = Sweep(SweepKind.Human);
            var swept = new List<string>();
            foreach (Spine spine in spines)
                swept.Add($"{spine.Item.defName}={spine.Actual:0.###}({spine.TierName})");
            Check.Note($"swept {spines.Count} artificial spine(s): " + string.Join(", ", swept.ToArray()));

            foreach (Spine spine in spines)
            {
                Check.Soft(Math.Abs(spine.Actual - spine.Tier) < Tolerance,
                    $"{spine.Item.defName} ({spine.TierName} tier, installed by {spine.Install.defName}, "
                    + $"from {ModOf(spine.Item)}) has partEfficiency {spine.Actual:0.###}, ladder says "
                    + $"{spine.Tier:0.###}");
            }

            Check.Soft(spines.Count >= 1,
                $"the sweep found {spines.Count} artificial spines. Core ships the bionic spine on "
                + "every modlist, so the sweep has stopped matching what it was written to match and "
                + "this test asserted nothing");

            Check.SoftResult();
        }

        [Test]
        public static void RevenantVertebraeStaysOffTheLadder()
        {
            if (!Check.Ready(Key, Ids.Anomaly))
                return;

            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail("RevenantVertebrae");
            if (hediff == null)
            {
                Check.Note("RevenantVertebrae absent");
                Check.SoftResult();
                return;
            }

            Check.Note($"RevenantVertebrae: hediffClass {hediff.hediffClass?.Name}, " +
                $"partEfficiency {hediff.addedPartProps?.partEfficiency.ToString("0.###") ?? "<none>"}, " +
                $"abilities {(hediff.abilities == null ? 0 : hediff.abilities.Count)}");

            Check.Soft(!typeof(Hediff_AddedPart).IsAssignableFrom(hediff.hediffClass),
                "RevenantVertebrae is now a Hediff_AddedPart, so its part efficiency has stopped " +
                "being inert and it needs a rung on the spine ladder after all");

            foreach (Spine spine in Sweep(SweepKind.Human))
                Check.Soft(spine.Hediff != hediff,
                    "the spine sweep returned RevenantVertebrae; it is an implant granting an " +
                    "ability, not a replacement bought for moving capacity, and the ladder would " +
                    "demand a number of it that does nothing");

            Check.SoftResult();
        }

        [Test]
        public static void ModularSpinesMatchTheirTier()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants))
                return;

            var covered = new List<string>();
            AssertEfficiency("LTS_ModularBionicSpine", Bionic, covered);
            AssertEfficiency("LTS_ModularArchotechSpine", Archotech, covered);

            Check.Note(covered.Count == 0
                ? "no modular spine present (Integrated Implants' conditional folder did not load, or "
                  + "EBSG Framework is absent)"
                : "modular spines checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void RecostedSpines()
        {
            if (!Check.Ready(Key))
                return;

            var covered = new List<string>();

            ThingDef bionic = DefDatabase<ThingDef>.GetNamedSilentFail("BionicSpine");
            ThingDef bionicLeg = DefDatabase<ThingDef>.GetNamedSilentFail("BionicLeg");
            if (bionic != null)
            {
                covered.Add("BionicSpine");
                Check.Soft(Check.CostOf(bionic, "Plasteel") == 105,
                    $"BionicSpine plasteel: {Show(Check.CostOf(bionic, "Plasteel"))}, expected 105");

                Check.Soft(ComponentValueOf(bionic) == 800,
                    $"BionicSpine component value: {ComponentsShown(bionic)}, expected 800 - the "
                    + "spine premium is paid in plasteel precisely so the component count stays put");

                if (bionicLeg != null)
                    Check.Soft(bionic.BaseMarketValue > bionicLeg.BaseMarketValue * 1.5f,
                        $"a bionic spine costs {bionic.BaseMarketValue:0.#} against a bionic leg's "
                        + $"{bionicLeg.BaseMarketValue:0.#}. One spine multiplies Moving by exactly "
                        + "what both legs together multiply it by, so it must not be the cheap way "
                        + "to buy that");
            }

            ThingDef advanced = Check.Optional<ThingDef>("AdvancedBionicSpine", Key, Ids.EPOEForked);
            if (advanced != null)
            {
                covered.Add("AdvancedBionicSpine");
                Check.Soft(Check.CostOf(advanced, "Plasteel") == 155,
                    $"AdvancedBionicSpine plasteel: {Show(Check.CostOf(advanced, "Plasteel"))}, expected 155");
                Check.Soft(ComponentValueOf(advanced) == 1200,
                    $"AdvancedBionicSpine component value: {ComponentsShown(advanced)}, expected 1200 "
                    + "- untouched so it stays inside the micromachine swap's six-component group");

                if (bionic != null)
                    Check.Soft(advanced.BaseMarketValue > bionic.BaseMarketValue,
                        $"the advanced spine costs {advanced.BaseMarketValue:0.#} and the bionic one "
                        + $"{bionic.BaseMarketValue:0.#}; the tiers have inverted");

                ThingDef archotech = DefDatabase<ThingDef>.GetNamedSilentFail("LTS_ArchotechSpine");
                if (archotech != null)
                    Check.Soft(advanced.BaseMarketValue < archotech.BaseMarketValue,
                        $"the advanced spine costs {advanced.BaseMarketValue:0.#} against the "
                        + $"archotech spine's {archotech.BaseMarketValue:0.#}, which is strictly "
                        + "better and should never be the cheaper of the two");
            }

            RecipeDef upgrade = DefDatabase<RecipeDef>.GetNamedSilentFail("CreateAdvancedBionicSpine");
            if (upgrade != null && bionic != null && advanced != null)
            {
                covered.Add("CreateAdvancedBionicSpine");
                int upgradePlasteel = ConsumedCount(upgrade, "Plasteel");
                int bionicPlasteel = Check.CostOf(bionic, "Plasteel") ?? 0;
                int advancedPlasteel = Check.CostOf(advanced, "Plasteel") ?? 0;
                Check.Soft(bionicPlasteel + upgradePlasteel == advancedPlasteel,
                    $"upgrading a bionic spine consumes {bionicPlasteel} + {upgradePlasteel} = "
                    + $"{bionicPlasteel + upgradePlasteel} plasteel, while building an advanced spine "
                    + $"outright costs {advancedPlasteel}. Whichever is cheaper makes the other bill "
                    + "pointless");
            }

            ThingDef simple = Check.Optional<ThingDef>("SimpleSpine", Key, Ids.EPOEForked);
            if (simple != null)
            {
                covered.Add("SimpleSpine");
                Check.Soft(Check.CostOf(simple, "Steel") == 40 && Check.CostOf(simple, "Plasteel") == null,
                    "SimpleSpine should still be the prosthetic tier's 40 steel and no plasteel; it "
                    + $"is {Show(Check.CostOf(simple, "Steel"))} steel and "
                    + $"{Show(Check.CostOf(simple, "Plasteel"))} plasteel");
            }

            Check.Note(covered.Count == 0
                ? "no re-costed spine present"
                : "re-costed spines checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void ThoracicFramesMatchTheLadder()
        {
            if (!Check.Ready(Key))
                return;

            var covered = new List<string>();
            AssertEfficiency("RBP_ThoracicFrameBionicHediff", Bionic, covered);
            AssertEfficiency("RBP_ThoracicFrameAdvancedHediff", Advanced, covered);
            AssertEfficiency("RBP_ThoracicFrameArchotechHediff", Archotech, covered);
            AssertEfficiency("RBP_LivingFrameHediff", Archotech, covered);

            ThingDef advancedFrame = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_ThoracicFrameAdvanced");
            if (advancedFrame != null)
            {
                Check.Soft(ComponentValueOf(advancedFrame) == 1600,
                    $"RBP_ThoracicFrameAdvanced component value: {ComponentsShown(advancedFrame)}, "
                    + "expected 1600 (eight advanced components' worth)");
                Check.Soft(Check.CostOf(advancedFrame, "Plasteel") == 55,
                    $"RBP_ThoracicFrameAdvanced plasteel: {Show(Check.CostOf(advancedFrame, "Plasteel"))}");

                ThingDef bionicFrame = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_ThoracicFrameBionic");
                if (bionicFrame != null)
                    Check.Soft(advancedFrame.BaseMarketValue > bionicFrame.BaseMarketValue,
                        $"the advanced frame costs {advancedFrame.BaseMarketValue:0.#} and the bionic "
                        + $"one {bionicFrame.BaseMarketValue:0.#}; a frame with an extra slot cannot "
                        + "be the cheaper of the two");
            }

            Check.Note(covered.Count == 0
                ? "no thoracic frame present (cybernetics.thoracicframe and cybernetics.livingframe "
                  + "are both off)"
                : "frames checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void TorsoImplantsHaveNoRung()
        {
            if (!Check.Ready(Key))
                return;

            var seen = new List<string>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.addsHediff == null || !TargetsBarePart(recipe, "Torso"))
                    continue;
                seen.Add(recipe.addsHediff.defName);
                Check.Soft(recipe.addsHediff.addedPartProps == null,
                    $"{recipe.addsHediff.defName} (installed on the Torso by {recipe.defName}, from "
                    + $"{ModOf(recipe.addsHediff)}) declares addedPartProps with partEfficiency "
                    + $"{recipe.addsHediff.addedPartProps?.partEfficiency:0.###}. An added part on the "
                    + "Torso is inherited by every part beneath it, so this is a whole-body "
                    + "multiplier, not a torso implant");
            }

            Check.Note($"torso implants seen: {seen.Count}"
                + (seen.Count == 0 ? "" : " - " + string.Join(", ", seen.ToArray())));

            Check.Soft(seen.Count >= 5,
                $"only {seen.Count} surgeries install a hediff on the Torso; the sweep has stopped "
                + "matching and this test asserted almost nothing");

            Check.SoftResult();
        }

        [Test]
        public static void AnimalAndAndroidSpinesStayOffTheHumanLadder()
        {
            if (!Check.Ready(Key))
                return;

            List<Spine> human = Sweep(SweepKind.Human);
            List<Spine> foreignParts = Sweep(SweepKind.AnimalOrAndroid);

            var seen = new List<string>();
            foreach (Spine spine in foreignParts)
                seen.Add($"{spine.Item.defName}={spine.Actual:0.###}");
            Check.Note($"found {foreignParts.Count} animal/android spine(s): "
                + string.Join(", ", seen.ToArray()));

            foreach (Spine spine in foreignParts)
                foreach (Spine other in human)
                    if (other.Item == spine.Item)
                        Check.Soft(false,
                            $"{spine.Item.defName} is an animal or android part "
                            + $"({string.Join(",", CategoryNames(spine.Item).ToArray())}) yet the human "
                            + $"sweep also returned it as {other.TierName} tier - the category "
                            + "exclusion has stopped working");

            HediffDef animalBionic = DefDatabase<HediffDef>.GetNamedSilentFail("BionicSpineAnimal");
            if (animalBionic?.addedPartProps != null)
                Check.Soft(Math.Abs(animalBionic.addedPartProps.partEfficiency - 1.00f) < Tolerance,
                    $"BionicSpineAnimal has partEfficiency {animalBionic.addedPartProps.partEfficiency:0.###}. "
                    + "A Dog Said ships it with none declared, i.e. 1.00; an operation of ours has "
                    + "reached an animal part");

            foreach (string name in new[] { "VREA_ArtificialSpine", "VREA_ArtificialRibcage" })
            {
                HediffDef android = DefDatabase<HediffDef>.GetNamedSilentFail(name);
                if (android?.addedPartProps == null)
                    continue;
                Check.Soft(Math.Abs(android.addedPartProps.partEfficiency - 1.00f) < Tolerance,
                    $"{name} has partEfficiency {android.addedPartProps.partEfficiency:0.###}. Vanilla "
                    + "Races Expanded - Android ships its parts with none declared, i.e. 1.00 - parts "
                    + "that match flesh and never fail - so an operation of ours has reached it");
            }

            Check.SoftResult();
        }

        // ------------------------------------------------------------------------------------

        private enum SweepKind { Human, AnimalOrAndroid }

        private static List<Spine> Sweep(SweepKind kind)
        {
            var found = new List<Spine>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.workerClass == null
                    || !typeof(Recipe_InstallArtificialBodyPart).IsAssignableFrom(recipe.workerClass))
                    continue;
                HediffDef hediff = recipe.addsHediff;
                if (hediff?.addedPartProps == null || hediff.spawnThingOnRemoved == null)
                    continue;
                if (!typeof(Hediff_AddedPart).IsAssignableFrom(hediff.hediffClass))
                    continue;
                if (!TargetsBarePart(recipe, "Spine"))
                    continue;

                ThingDef item = hediff.spawnThingOnRemoved;
                if (item.techLevel < TechLevel.Industrial)
                    continue;

                bool foreignPart = HasCategory(item, "BodyPartsAnimalArtificial")
                    || HasCategory(item, "VREA_BodyPartsAndroid");
                if (foreignPart != (kind == SweepKind.AnimalOrAndroid))
                    continue;

                float tier;
                string tierName;
                if (!TierOf(item, out tier, out tierName) && kind == SweepKind.Human)
                    continue;

                found.Add(new Spine
                {
                    Install = recipe,
                    Hediff = hediff,
                    Item = item,
                    Tier = tier,
                    TierName = tierName ?? "off-ladder",
                });
            }
            return found;
        }

        private static bool TargetsBarePart(RecipeDef recipe, string bareName)
        {
            if (recipe.appliedOnFixedBodyParts == null)
                return false;
            foreach (BodyPartDef part in recipe.appliedOnFixedBodyParts)
            {
                if (part == null)
                    continue;
                string bare = part.defName.StartsWith("BS_Mechanical")
                    ? part.defName.Substring("BS_Mechanical".Length)
                    : part.defName;
                if (bare == bareName)
                    return true;
            }
            return false;
        }

        private static void AssertEfficiency(string hediffName, float expected, List<string> covered)
        {
            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            if (hediff == null)
                return;
            covered.Add(hediffName);
            if (!Check.Soft(hediff.addedPartProps != null, $"{hediffName} has no addedPartProps"))
                return;
            Check.Soft(Math.Abs(hediff.addedPartProps.partEfficiency - expected) < Tolerance,
                $"{hediffName} has partEfficiency {hediff.addedPartProps.partEfficiency:0.###}, "
                + $"ladder says {expected:0.###}");
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
            return $"{ComponentValueOf(def)} ({spacers} advanced component(s) + "
                + $"{micromachines} micromachine(s))";
        }

        private static int ConsumedCount(RecipeDef recipe, string thingDefName)
        {
            if (recipe.ingredients == null)
                return 0;
            int total = 0;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                if (ingredient?.filter == null)
                    continue;
                foreach (ThingDef allowed in ingredient.filter.AllowedThingDefs)
                    if (allowed != null && allowed.defName == thingDefName)
                    {
                        total += (int)ingredient.GetBaseCount();
                        break;
                    }
            }
            return total;
        }

        private static bool HasTag(BodyPartDef part, string tagDefName)
        {
            if (part.tags == null)
                return false;
            foreach (BodyPartTagDef tag in part.tags)
                if (tag?.defName == tagDefName)
                    return true;
            return false;
        }

        private static string TagsOf(BodyPartDef part)
        {
            if (part.tags == null || part.tags.Count == 0)
                return "none";
            var names = new List<string>();
            foreach (BodyPartTagDef tag in part.tags)
                if (tag != null)
                    names.Add(tag.defName);
            return string.Join(", ", names.ToArray());
        }

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

        private static string Show(int? value) => value.HasValue ? value.Value.ToString() : "absent";

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "unknown mod";
    }
}
