using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsLimbTiersTests
    {
        private const string Key = "cybernetics.tiers";

        private const float Prosthetic = 0.80f;
        private const float Bionic = 1.15f;
        private const float Advanced = 1.35f;
        private const float Archotech = 1.50f;

        /// <summary>Fingers and toes sit above their tier.</summary>
        private const float BionicDigit = 1.35f;

        private class Limb
        {
            public RecipeDef Install;
            public HediffDef Hediff;
            public ThingDef Item;
            public float Tier;
            public string TierName;
            public bool IsDigit;
            public string Parts;

            public float Expected => IsDigit && TierName == "bionic" ? BionicDigit : Tier;
            public float Actual => Hediff.addedPartProps.partEfficiency;
        }

        [Test]
        public static void LimbTierLadder()
        {
            if (!Check.Ready(Key))
                return;

            List<Limb> limbs = Sweep();
            var swept = new List<string>();
            foreach (Limb limb in limbs)
                swept.Add($"{limb.Item.defName}={limb.Actual:0.###}({limb.TierName})");
            Check.Note($"swept {limbs.Count} artificial limb(s): " + string.Join(", ", swept.ToArray()));

            foreach (Limb limb in limbs)
            {
                Check.Soft(Math.Abs(limb.Actual - limb.Expected) < 0.0005f,
                    $"{limb.Item.defName} ({limb.TierName} tier, replaces {limb.Parts} via {limb.Install.defName}, " +
                    $"from {ModOf(limb.Item)}) has partEfficiency {limb.Actual:0.###}, " +
                    $"ladder says {limb.Expected:0.###}");
            }

            Check.Soft(limbs.Count >= 6,
                $"sweep found only {limbs.Count} artificial limbs; Core alone should contribute six, " +
                "so the sweep is no longer matching what it was written to match");

            Check.SoftResult();
        }

        [Test]
        public static void SpecialistLimbsMatchTheirTier()
        {
            if (!Check.Ready(Key))
                return;

            // Core, so it is always here and this test is never vacuous.
            AssertEfficiency(Check.Def<HediffDef>("PowerClaw"), Prosthetic, "power claw");
            AssertEfficiency(Check.Optional<HediffDef>("PowerArm", Key, Ids.EPOEForked), Bionic, "power arm");
            AssertEfficiency(Check.Optional<HediffDef>("AdvancedPowerArm", Key, Ids.EPOEForked), Advanced,
                "advanced power arm");

            Check.SoftResult();
        }

        [Test]
        public static void DigitsCostLessThanFullLimbs()
        {
            if (!Check.Ready(Key))
                return;

            List<Limb> limbs = Sweep();
            var comparisons = 0;
            foreach (string tierName in new[] { "prosthetic", "bionic", "advanced", "archotech" })
            {
                ThingDef dearestDigit = null;
                ThingDef cheapestLimb = null;
                foreach (Limb limb in limbs)
                {
                    if (limb.TierName != tierName)
                        continue;
                    if (limb.IsDigit)
                    {
                        if (dearestDigit == null || limb.Item.BaseMarketValue > dearestDigit.BaseMarketValue)
                            dearestDigit = limb.Item;
                    }
                    else if (cheapestLimb == null || limb.Item.BaseMarketValue < cheapestLimb.BaseMarketValue)
                    {
                        cheapestLimb = limb.Item;
                    }
                }

                if (dearestDigit == null || cheapestLimb == null)
                    continue;

                comparisons++;
                Check.Soft(dearestDigit.BaseMarketValue < cheapestLimb.BaseMarketValue,
                    $"{tierName} tier: {dearestDigit.defName} costs {dearestDigit.BaseMarketValue:0.#} silver, " +
                    $"which is not less than the cheapest whole part at that tier " +
                    $"({cheapestLimb.defName} at {cheapestLimb.BaseMarketValue:0.#})");
            }

            Check.Soft(comparisons > 0,
                "no tier had both a digit and a whole limb, so nothing was actually compared");
            Check.SoftResult();
        }

        [Test]
        public static void RecostedLimbs()
        {
            if (!Check.Ready(Key))
                return;

            var covered = new List<string>();

            ThingDef fieldHand = Check.Optional<ThingDef>("FieldHand", Key, Ids.Royalty);
            if (fieldHand != null)
            {
                covered.Add("FieldHand");
                Check.Soft(Check.CostOf(fieldHand, "Steel") == 45, $"FieldHand steel: {Show(Check.CostOf(fieldHand, "Steel"))}");
                Check.Soft(Check.CostOf(fieldHand, "ComponentIndustrial") == 7,
                    $"FieldHand components: {Show(Check.CostOf(fieldHand, "ComponentIndustrial"))}");
                ThingDef drillArm = Check.Optional<ThingDef>("DrillArm", Key, Ids.Royalty);
                if (drillArm != null)
                    Check.Soft(fieldHand.BaseMarketValue < drillArm.BaseMarketValue,
                        $"the field hand replaces a hand and grants one work stat, the drill arm replaces an " +
                        $"arm and grants two, yet the hand is not cheaper: " +
                        $"{fieldHand.BaseMarketValue:0.#} vs {drillArm.BaseMarketValue:0.#}");
            }

            ThingDef advFieldHand = Check.Optional<ThingDef>("EPOE_AdvancedFieldHand", Key, Ids.EPOEForkedRoyalty);
            if (advFieldHand != null)
            {
                covered.Add("EPOE_AdvancedFieldHand");
                Check.Soft(Check.CostOf(advFieldHand, "Steel") == 45,
                    $"EPOE_AdvancedFieldHand steel: {Show(Check.CostOf(advFieldHand, "Steel"))}");
                Check.Soft(Check.CostOf(advFieldHand, "ComponentIndustrial") == 7,
                    $"EPOE_AdvancedFieldHand components: {Show(Check.CostOf(advFieldHand, "ComponentIndustrial"))}");
            }

            ThingDef strider = DefDatabase<ThingDef>.GetNamedSilentFail("BS_BionicStriderLeg");
            if (strider != null)
            {
                covered.Add("BS_BionicStriderLeg");
                Check.Soft(Check.CostOf(strider, "Plasteel") == 20,
                    $"BS_BionicStriderLeg plasteel: {Show(Check.CostOf(strider, "Plasteel"))}");
                ThingDef spacerDef = DefDatabase<ThingDef>.GetNamedSilentFail("ComponentSpacer");
                ThingDef microDef = DefDatabase<ThingDef>.GetNamedSilentFail("gitsMicromachines");
                float striderComponents =
                    (Check.CostOf(strider, "ComponentSpacer") ?? 0) * (spacerDef?.BaseMarketValue ?? 200f)
                    + (microDef == null
                        ? 0f
                        : (Check.CostOf(strider, "gitsMicromachines") ?? 0) * microDef.BaseMarketValue);
                Check.Soft(Math.Abs(striderComponents - 1000f) < 0.51f,
                    $"BS_BionicStriderLeg carries {striderComponents:0} silver of components, expected " +
                    $"1000 ({Show(Check.CostOf(strider, "ComponentSpacer"))} advanced component(s) + " +
                    $"{Show(Check.CostOf(strider, "gitsMicromachines"))} micromachine(s))");
                float work = strider.GetStatValueAbstract(StatDefOf.WorkToMake);
                Check.Soft(Math.Abs(work - 34000f) < 1f,
                    $"BS_BionicStriderLeg resolved WorkToMake is {work:0}, expected 34000 - " +
                    "an inherited 26000 here means the added statBases entry did not override the base");

                ThingDef bionicLeg = DefDatabase<ThingDef>.GetNamedSilentFail("BionicLeg");
                if (bionicLeg != null)
                    Check.Soft(strider.BaseMarketValue > bionicLeg.BaseMarketValue,
                        $"the strider leg now matches the bionic leg's efficiency and adds MoveSpeed, so it " +
                        $"must cost more: {strider.BaseMarketValue:0.#} vs {bionicLeg.BaseMarketValue:0.#}");
            }

            Check.Note(covered.Count == 0
                ? "no re-costed limb present (Royalty, EPOE-Forked: Royalty and Big and Small all absent)"
                : "re-costed limbs checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void LocomotiveSpineIsAMovingLimbCore()
        {
            if (!Check.Ready(Key))
                return;

            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail("BS_BionicLocomotiveSpine");
            ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail("BS_BionicLocomotiveSpine");
            if (hediff == null || item == null)
            {
                Check.Note("BS_BionicLocomotiveSpine is absent - Big and Small injects it through a " +
                    "patch operation, so this is legitimate and nothing was asserted");
                return;
            }

            // 1. Classification. A limb, not a spine and not a module.
            RecipeDef install = null;
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                if (recipe.addsHediff == hediff && recipe.IsSurgery)
                {
                    install = recipe;
                    break;
                }

            if (install?.appliedOnFixedBodyParts != null)
            {
                var shown = new List<string>();
                foreach (BodyPartDef part in install.appliedOnFixedBodyParts)
                {
                    if (part == null)
                        continue;
                    shown.Add($"{part.defName}[{TagsOf(part)}]");
                    Check.Soft(HasTag(part, "MovingLimbCore"),
                        $"the locomotive spine installs on {part.defName}, tagged {TagsOf(part)}. It is " +
                        "on the LIMB ladder because that part is a MovingLimbCore - the snake body's " +
                        "leg - and the bionic rung and the two-bionic-legs price both follow from that");
                    Check.Soft(!HasTag(part, "Spine"),
                        $"the locomotive spine installs on {part.defName}, which is tagged Spine. Spines " +
                        "are the torso-frame pass's ladder, not this one, and a part carrying both tags " +
                        "would be priced twice");
                }
                Check.Note("locomotive spine installs on " + string.Join(", ", shown.ToArray()));
            }
            else
            {
                Check.Soft(false, "no install surgery adds BS_BionicLocomotiveSpine, so its " +
                    "classification could not be checked at all");
            }

            BodyDef human = DefDatabase<BodyDef>.GetNamedSilentFail("Human");
            if (human != null && install?.appliedOnFixedBodyParts != null)
            {
                var reachable = false;
                foreach (BodyPartRecord record in human.AllParts)
                    if (install.appliedOnFixedBodyParts.Contains(record.def))
                        reachable = true;
                Check.Note(reachable
                    ? "the locomotive spine is installable on the vanilla Human body"
                    : "the locomotive spine is not installable on the vanilla Human body - only on the " +
                      "snake bodies that carry BS_SnakeSpine; expected, and recorded rather than fixed");
            }

            Check.Soft(hediff.hediffClass != null
                    && typeof(Hediff_AddedPart).IsAssignableFrom(hediff.hediffClass),
                $"the locomotive spine is a {hediff.hediffClass?.Name ?? "null"}; only a Hediff_AddedPart " +
                "contributes partEfficiency, so as anything else the 1.15 written here is inert and the " +
                "item is back to being Moving +0.25 for 2415 silver");
            AssertEfficiency(hediff, Bionic, "the bionic locomotive spine");

            float? mirrored = QualityBionicsBaseEfficiency(hediff);
            if (mirrored.HasValue)
                Check.Soft(Math.Abs(mirrored.Value - Bionic) < 0.0005f,
                    $"the locomotive spine's Quality Bionics baseEfficiency is {mirrored.Value:0.###} " +
                    $"against its partEfficiency {hediff.addedPartProps?.partEfficiency:0.###}; the two " +
                    "agree on every other part and a split here is a half-applied patch");

            Check.Soft(Check.CostOf(item, "Plasteel") == 80,
                $"BS_BionicLocomotiveSpine plasteel: {Show(Check.CostOf(item, "Plasteel"))}");

            ThingDef spacer = DefDatabase<ThingDef>.GetNamedSilentFail("ComponentSpacer");
            ThingDef micro = DefDatabase<ThingDef>.GetNamedSilentFail("gitsMicromachines");
            float componentValue = (Check.CostOf(item, "ComponentSpacer") ?? 0) * (spacer?.BaseMarketValue ?? 200f)
                + (micro == null ? 0f : (Check.CostOf(item, "gitsMicromachines") ?? 0) * micro.BaseMarketValue);
            Check.Soft(Math.Abs(componentValue - 1600f) < 0.51f,
                $"BS_BionicLocomotiveSpine carries {componentValue:0} silver of components; the rung is " +
                "eight advanced components' worth, 1600, whether they are eight components or the two " +
                "micromachines cybernetics.micromachines turns them into");

            ThingDef bionicLeg = DefDatabase<ThingDef>.GetNamedSilentFail("BionicLeg");
            if (bionicLeg != null)
                Check.Soft(item.BaseMarketValue > 2f * bionicLeg.BaseMarketValue,
                    $"the locomotive spine is {item.BaseMarketValue:0.#} against two bionic legs at " +
                    $"{2f * bionicLeg.BaseMarketValue:0.#}. It replaces the snake body's only moving-limb " +
                    "core, so it does both legs' work, and it also carries Moving +0.25 - it cannot be " +
                    "cheaper than the pair it stands in for");

            Check.SoftResult();
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
            var names = new List<string>();
            if (part.tags != null)
                foreach (BodyPartTagDef tag in part.tags)
                    if (tag != null)
                        names.Add(tag.defName);
            return names.Count == 0 ? "no tags" : string.Join(",", names.ToArray());
        }

        private static float? QualityBionicsBaseEfficiency(HediffDef hediff)
        {
            if (hediff.comps == null)
                return null;
            foreach (object props in hediff.comps)
            {
                if (props == null || props.GetType().Name != "HediffCompProperties_QualityBionics")
                    continue;
                try
                {
                    return Convert.ToSingle(Check.Field(props, "baseEfficiency"));
                }
                catch (Exception e)
                {
                    Check.Note($"could not read Quality Bionics baseEfficiency: {e.Message}");
                    return null;
                }
            }
            return null;
        }

        private static string Show(int? value) => value.HasValue ? value.Value.ToString() : "absent";

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "unknown mod";

        private static void AssertEfficiency(HediffDef hediff, float expected, string what)
        {
            if (hediff == null)
                return;
            if (!Check.Soft(hediff.addedPartProps != null, $"{what} ({hediff.defName}) has no addedPartProps"))
                return;
            Check.Soft(Math.Abs(hediff.addedPartProps.partEfficiency - expected) < 0.0005f,
                $"{what} ({hediff.defName}) has partEfficiency {hediff.addedPartProps.partEfficiency:0.###}, " +
                $"ladder says {expected:0.###}");
        }

        private static List<Limb> Sweep()
        {
            var found = new List<Limb>();
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

                var limbParts = 0;
                var digitParts = 0;
                var names = new List<string>();
                foreach (BodyPartDef part in recipe.appliedOnFixedBodyParts)
                {
                    if (part == null)
                        continue;
                    string bare = part.defName.StartsWith("BS_Mechanical")
                        ? part.defName.Substring("BS_Mechanical".Length)
                        : part.defName;
                    if (IsDigitPart(bare))
                        digitParts++;
                    if (IsLimbPart(bare))
                    {
                        limbParts++;
                        if (!names.Contains(bare))
                            names.Add(bare);
                    }
                }
                if (limbParts == 0)
                    continue;

                ThingDef item = hediff.spawnThingOnRemoved;
                if (item.techLevel < TechLevel.Industrial || IsWeaponLimb(item))
                    continue;

                float tier;
                string tierName;
                if (!TierOf(item, out tier, out tierName))
                    continue;

                found.Add(new Limb
                {
                    Install = recipe,
                    Hediff = hediff,
                    Item = item,
                    Tier = tier,
                    TierName = tierName,
                    IsDigit = digitParts == limbParts,
                    Parts = string.Join("/", names.ToArray()),
                });
            }
            return found;
        }

        private static bool IsLimbPart(string bare) =>
            bare == "Shoulder" || bare == "Arm" || bare == "Hand" || bare == "Finger"
            || bare == "Leg" || bare == "Foot" || bare == "Toe";

        private static bool IsDigitPart(string bare) => bare == "Finger" || bare == "Toe";

        private static bool IsWeaponLimb(ThingDef item)
        {
            if (item.violentTechHediff)
                return true;
            if (item.techHediffsTags == null)
                return false;
            foreach (string tag in item.techHediffsTags)
                if (tag == "AdvancedWeapon" || tag == "SpecialWeapon")
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
