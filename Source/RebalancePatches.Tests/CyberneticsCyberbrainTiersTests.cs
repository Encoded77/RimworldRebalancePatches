using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsCyberbrainTiersTests
    {
        private const string Key = "cybernetics.tiers";

        private const string RootCategory = "gitsCategoriesCyberbrainBase";

        /// <summary>One rung of the ladder.</summary>
        private class Rung
        {
            public string Category;
            public string Name;
            public int Micromachines;
            public float Work;
            public float Efficiency;
            public float ValueWithChips;
        }

        private static readonly Rung[] Ladder =
        {
            new Rung { Category = "gitsCategoriesBasicCyberbrain",       Name = "basic",       Micromachines = 1, Work = 28000f, Efficiency = 1.10f, ValueWithChips = 1950f },
            new Rung { Category = "gitsCategoriesEnhancedCyberbrain",    Name = "enhanced",    Micromachines = 1, Work = 32000f, Efficiency = 1.35f, ValueWithChips = 2925f },
            new Rung { Category = "gitsCategoriesSpecializedCyberbrain", Name = "specialized", Micromachines = 2, Work = 34000f, Efficiency = 1.40f, ValueWithChips = 3770f },
            new Rung { Category = "gitsCategoriesAdvancedCyberbrain",    Name = "advanced",    Micromachines = 3, Work = 36000f, Efficiency = 1.45f, ValueWithChips = 5140f },
            new Rung { Category = "gitsCategoriesExtremeCyberbrain",     Name = "extreme",     Micromachines = 4, Work = 40000f, Efficiency = 1.50f, ValueWithChips = 6995f },
        };

        [Test]
        public static void CyberbrainMicromachineLadder()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            List<ThingDef> swept = Sweep();
            NoteSweep(swept);
            if (!Check.Soft(swept.Count >= 23,
                $"the category sweep found only {swept.Count} cyberbrain(s); GiTS alone ships " +
                "twenty-three, so the sweep has stopped matching what it was written to match and " +
                "this test asserted almost nothing"))
            {
                Check.SoftResult();
                return;
            }

            foreach (ThingDef item in swept)
            {
                Rung rung = RungOf(item);
                if (rung == null)
                {
                    Check.Soft(false,
                        $"{item.defName} is in {RootCategory} but carries no tier category, so no rung " +
                        $"of the ladder claims it; its categories are {CategoriesShown(item)}");
                    continue;
                }

                Check.Soft(MicromachineValueOf(item) == rung.Micromachines * 800,
                    $"{item.defName} ({rung.Name} rung, from {ModOf(item)}) carries " +
                    $"{ComponentsShown(item)}; the rung is {rung.Micromachines} micromachine(s), " +
                    $"worth {rung.Micromachines * 800}");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CyberbrainWorkLadder()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            List<ThingDef> swept = Sweep();
            if (!Check.Soft(swept.Count >= 23,
                $"the category sweep found only {swept.Count} cyberbrain(s); nothing was asserted"))
            {
                Check.SoftResult();
                return;
            }

            foreach (ThingDef item in swept)
            {
                Rung rung = RungOf(item);
                if (rung == null)
                    continue;
                float? work = Check.StatBase(item, "WorkToMake");
                Check.Soft(work.HasValue && Math.Abs(work.Value - rung.Work) < 0.5f,
                    $"{item.defName} ({rung.Name} rung) has WorkToMake " +
                    $"{(work.HasValue ? work.Value.ToString("0") : "absent")}, ladder says {rung.Work:0}");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CyberbrainPriceLadder()
        {
            if (!Check.Ready(Key, Ids.GiTS, Ids.EPOEForked))
                return;

            var rungValue = new Dictionary<string, float>();
            List<ThingDef> swept = Sweep();
            if (!Check.Soft(swept.Count >= 23,
                $"the category sweep found only {swept.Count} cyberbrain(s); nothing was asserted"))
            {
                Check.SoftResult();
                return;
            }

            foreach (ThingDef item in swept)
            {
                Rung rung = RungOf(item);
                if (rung == null)
                    continue;

                Check.Soft(Math.Abs(item.BaseMarketValue - rung.ValueWithChips) < 2.5f,
                    $"{item.defName} ({rung.Name} rung, from {ModOf(item)}) is worth " +
                    $"{item.BaseMarketValue:0.#}, ladder says {rung.ValueWithChips:0}; it carries " +
                    $"{ComponentsShown(item)} and {Check.StatBase(item, "WorkToMake")} work");

                if (!rungValue.ContainsKey(rung.Name))
                    rungValue[rung.Name] = item.BaseMarketValue;
            }

            var steps = new List<string>();
            float previousValue = 0f;
            float previousStep = 0f;
            bool first = true;
            foreach (Rung rung in Ladder)
            {
                float value;
                if (!rungValue.TryGetValue(rung.Name, out value))
                    continue;
                if (!first)
                {
                    float step = value - previousValue;
                    steps.Add($"{rung.Name} +{step:0}");
                    Check.Soft(step > 0f,
                        $"the {rung.Name} rung costs {value:0.#}, no more than the rung below it " +
                        $"({previousValue:0.#})");
                    if (rung.Name == "advanced" || rung.Name == "extreme")
                        Check.Soft(step >= previousStep - 0.5f,
                            $"the step onto the {rung.Name} rung is +{step:0} against +{previousStep:0} " +
                            "for the rung below it, yet it buys strictly more - another trade, another " +
                            "cortex slot and a better chassis. Marginal price must not fall as " +
                            "capability rises; that inversion is what this pass was written to remove");
                    previousStep = step;
                }
                previousValue = value;
                first = false;
            }
            Check.Note("ladder steps: " + string.Join(", ", steps.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void CyberbrainEfficiencyIsDeclarativeOnly()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            Dictionary<ThingDef, HediffDef> pairs = PairItemsWithHediffs();
            if (!Check.Soft(pairs.Count >= 23,
                $"only {pairs.Count} cyberbrain(s) could be paired with their install surgery; the " +
                "pairing walks RecipeDef.ingredients and has stopped finding them"))
            {
                Check.SoftResult();
                return;
            }

            var live = new List<string>();
            foreach (KeyValuePair<ThingDef, HediffDef> pair in pairs)
            {
                Rung rung = RungOf(pair.Key);
                if (rung == null)
                    continue;
                HediffDef hediff = pair.Value;

                Check.Soft(hediff.addedPartProps != null
                        && Math.Abs(hediff.addedPartProps.partEfficiency - rung.Efficiency) < 0.0005f,
                    $"{hediff.defName} (installed from {pair.Key.defName}, {rung.Name} rung) declares " +
                    $"partEfficiency " +
                    $"{(hediff.addedPartProps == null ? "none" : hediff.addedPartProps.partEfficiency.ToString("0.###"))}, " +
                    $"its tier declares {rung.Efficiency:0.###}");

                if (hediff.hediffClass != null && typeof(Hediff_AddedPart).IsAssignableFrom(hediff.hediffClass))
                    live.Add($"{hediff.defName}={hediff.hediffClass.Name}");
            }

            Check.Soft(live.Count == 0,
                "cyberbrain hediffs are Hediff_Implant, so their declared partEfficiency is inert and " +
                "the prices in this suite were derived on that basis. These are now added parts, " +
                "which makes the ladder live and the prices wrong: " +
                string.Join(", ", live.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void PX7IsBreadthRatherThanBestInEveryTrade()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            HediffDef flagship = DefDatabase<HediffDef>.GetNamedSilentFail("gitsPX7Hediff");
            if (flagship == null)
            {
                Check.Note("gitsPX7Hediff is absent; nothing was asserted");
                return;
            }

            List<HediffDef> workModels = TaggedCyberbrains("GiTS_WorkCyberbrain");
            if (!Check.Soft(workModels.Count >= 8,
                $"only {workModels.Count} hediff(s) carry GiTS_WorkCyberbrain; GiTS ships eleven Civis " +
                "models, so the tag sweep has stopped matching and this test asserted nothing"))
            {
                Check.SoftResult();
                return;
            }

            Dictionary<string, float> mine = OffsetsOfLastStage(flagship);
            var behind = new List<string>();
            var seen = new List<string>();

            foreach (HediffDef model in workModels)
            {
                if (model == flagship)
                    continue;
                foreach (KeyValuePair<string, float> entry in OffsetsOfLastStage(model))
                {
                    if (entry.Key == "MentalBreakThreshold" || entry.Key == "CertaintyLossFactor")
                        continue;
                    float ours;
                    if (mine.TryGetValue(entry.Key, out ours) && ours >= entry.Value - 0.0005f)
                        continue;
                    string line = mine.ContainsKey(entry.Key)
                        ? $"{entry.Key} {ours:0.##} < {entry.Value:0.##} ({model.defName})"
                        : $"{entry.Key} absent, {entry.Value:0.##} on {model.defName}";
                    if (!seen.Contains(entry.Key))
                    {
                        seen.Add(entry.Key);
                        behind.Add(line);
                    }
                }
            }

            behind.Sort();
            Check.Note($"PX-7 trails a cheaper Civis model on {behind.Count} stat(s): " +
                string.Join(" | ", behind.ToArray()));

            string[] expected =
            {
                "ArrestSuccessChance",
                "ConstructSuccessChance",
                "DeepDrillingSpeed",
                "FixBrokenDownBuildingSuccessChance",
                "MedicalSurgerySuccessChance",
                "MedicalTendQuality",
                "MedicalTendSpeed",
                "MiningYield",
            };
            seen.Sort();
            Check.Soft(string.Join(",", seen.ToArray()) == string.Join(",", expected),
                "the set of stats where the PX-7 trails a cheaper Civis model has changed. It was " +
                $"[{string.Join(",", expected)}] and is now [{string.Join(",", seen.ToArray())}]. " +
                "RBP_CybCivisPX7's description is written to that list - it no longer claims the " +
                "flagship is best at everything - so a change here means the text needs re-reading");

            Check.SoftResult();
        }

        [Test]
        public static void AdvancedCombatBrainsOutrankTheSpecializedRung()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            var advanced = new List<HediffDef>();
            var specialized = new List<HediffDef>();
            foreach (HediffDef hediff in TaggedCyberbrains("GiTS_CombatCyberbrain"))
            {
                if (HasTag(hediff, "GiTS_AdvancedCyberbrain"))
                    advanced.Add(hediff);
                else if (HasTag(hediff, "GiTS_SpecializedCyberbrain"))
                    specialized.Add(hediff);
            }

            var melee = new List<string>();
            foreach (HediffDef hediff in TaggedCyberbrains("GiTS_Cyberbrain"))
            {
                float cooldown = FactorOfLastStage(hediff, "MeleeCooldownFactor");
                float damage = FactorOfLastStage(hediff, "MeleeDamageFactor");
                if (cooldown > 0f && damage > 0f)
                    melee.Add($"{hediff.defName}={damage / cooldown:0.00}");
            }
            melee.Sort();
            Check.Note("melee damage per cooldown: " + string.Join(", ", melee.ToArray()));

            if (!Check.Soft(advanced.Count >= 3 && specialized.Count >= 5,
                $"the tag sweep found {advanced.Count} advanced and {specialized.Count} specialized " +
                "combat model(s); GiTS ships three and five, so it has stopped matching and this test " +
                "asserted nothing"))
            {
                Check.SoftResult();
                return;
            }

            var bestSlots = 0;
            var bestAbilities = 0;
            foreach (HediffDef hediff in specialized)
            {
                if (CortexSlotsOf(hediff) > bestSlots)
                    bestSlots = CortexSlotsOf(hediff);
                if (AbilityCount(hediff) > bestAbilities)
                    bestAbilities = AbilityCount(hediff);
            }

            foreach (HediffDef hediff in advanced)
            {
                Check.Soft(AbilityCount(hediff) > bestAbilities,
                    $"{hediff.defName} grants {AbilityCount(hediff)} cyberbrain abilit(ies) against " +
                    $"{bestAbilities} at the rung below, yet costs 1370 more. The advanced combat rung's " +
                    "price is justified on what it carries rather than on its combat numbers - the " +
                    "HCQ19 has the lowest melee damage-per-cooldown of the four melee models - so this " +
                    "is the argument, not a detail");

                if (bestSlots > 0)
                    Check.Soft(CortexSlotsOf(hediff) > bestSlots,
                        $"{hediff.defName} holds {CortexSlotsOf(hediff)} cortex module(s) against " +
                        $"{bestSlots} at the rung below, yet costs 1370 more");
            }

            if (bestSlots == 0)
                Check.Note("no cyberbrain declares a cortex slot - EBSG or cybernetics.modules is off, " +
                    "so only the ability half of the rung argument was checked");

            Check.SoftResult();
        }

        private static List<HediffDef> TaggedCyberbrains(string tag)
        {
            var found = new List<HediffDef>();
            foreach (HediffDef hediff in DefDatabase<HediffDef>.AllDefsListForReading)
                if (HasTag(hediff, tag))
                    found.Add(hediff);
            return found;
        }

        private static bool HasTag(HediffDef hediff, string tag) =>
            hediff.tags != null && hediff.tags.Contains(tag);

        private static Dictionary<string, float> OffsetsOfLastStage(HediffDef hediff)
        {
            var found = new Dictionary<string, float>();
            if (hediff.stages == null || hediff.stages.Count == 0)
                return found;
            HediffStage stage = hediff.stages[hediff.stages.Count - 1];
            if (stage?.statOffsets == null)
                return found;
            foreach (StatModifier modifier in stage.statOffsets)
                if (modifier?.stat != null)
                    found[modifier.stat.defName] = modifier.value;
            return found;
        }

        private static float FactorOfLastStage(HediffDef hediff, string statDefName)
        {
            if (hediff.stages == null || hediff.stages.Count == 0)
                return 0f;
            HediffStage stage = hediff.stages[hediff.stages.Count - 1];
            if (stage?.statFactors == null)
                return 0f;
            foreach (StatModifier modifier in stage.statFactors)
                if (modifier?.stat != null && modifier.stat.defName == statDefName)
                    return modifier.value;
            return 0f;
        }

        private static int AbilityCount(HediffDef hediff) =>
            hediff.abilities == null ? 0 : hediff.abilities.Count;

        private static int CortexSlotsOf(HediffDef hediff)
        {
            if (hediff.comps == null)
                return 0;
            foreach (object props in hediff.comps)
            {
                if (props == null || props.GetType().Name != "HediffCompProperties_Modular")
                    continue;
                object raw;
                try
                {
                    raw = Check.Field(props, "slots");
                }
                catch (Exception)
                {
                    return 0;
                }
                if (!(raw is System.Collections.IEnumerable slots))
                    return 0;
                foreach (object slot in slots)
                {
                    if (slot == null)
                        continue;
                    try
                    {
                        object id = Check.Field(slot, "slotID");
                        if (id == null || id.ToString() != "RBP_CortexSlot")
                            continue;
                        return Convert.ToInt32(Check.Field(slot, "capacity"));
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                }
            }
            return 0;
        }

        private static List<ThingDef> Sweep()
        {
            var found = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                if (HasCategory(def, RootCategory))
                    found.Add(def);
            return found;
        }

        private static Dictionary<ThingDef, HediffDef> PairItemsWithHediffs()
        {
            var pairs = new Dictionary<ThingDef, HediffDef>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                HediffDef hediff = recipe.addsHediff;
                if (hediff == null || recipe.ingredients == null)
                    continue;
                foreach (IngredientCount ingredient in recipe.ingredients)
                {
                    if (ingredient?.filter == null)
                        continue;
                    foreach (ThingDef allowed in ingredient.filter.AllowedThingDefs)
                    {
                        if (allowed == null || !HasCategory(allowed, RootCategory))
                            continue;
                        if (!pairs.ContainsKey(allowed))
                            pairs.Add(allowed, hediff);
                    }
                }
            }
            return pairs;
        }

        private static Rung RungOf(ThingDef item)
        {
            foreach (Rung rung in Ladder)
                if (HasCategory(item, rung.Category))
                    return rung;
            return null;
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

        private static string CategoriesShown(ThingDef item)
        {
            var names = new List<string>();
            if (item.thingCategories != null)
                foreach (ThingCategoryDef category in item.thingCategories)
                    if (category != null)
                        names.Add(category.defName);
            return names.Count == 0 ? "none" : string.Join(", ", names.ToArray());
        }

        private static int MicromachineValueOf(ThingDef def) =>
            (Check.CostOf(def, "gitsMicromachines") ?? 0) * 800;

        private static string ComponentsShown(ThingDef def)
        {
            int micromachines = Check.CostOf(def, "gitsMicromachines") ?? 0;
            int spacers = Check.CostOf(def, "ComponentSpacer") ?? 0;
            int chips = Check.CostOf(def, "EPOE_MAAI_Chip") ?? 0;
            return $"{micromachines} micromachine(s) worth {micromachines * 800}, " +
                $"{spacers} advanced component(s), {chips} MA-AI chip(s)";
        }

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "unknown mod";

        private static void NoteSweep(List<ThingDef> swept)
        {
            var shown = new List<string>();
            foreach (ThingDef item in swept)
            {
                Rung rung = RungOf(item);
                shown.Add($"{item.defName}={item.BaseMarketValue:0}({rung?.Name ?? "no rung"})");
            }
            Check.Note($"swept {swept.Count} cyberbrain(s): " + string.Join(", ", shown.ToArray()));
        }
    }
}
