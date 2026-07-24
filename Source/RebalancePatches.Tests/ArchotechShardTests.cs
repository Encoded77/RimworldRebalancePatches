using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ArchotechShardTests
    {
        private const string Key = "cybernetics.archotechshards";
        private const string ShardName = "RBP_ArchotechShard";
        private const string ResearchName = "RBP_CybArchotechCybernetics";
        private const string ArchotechCategory = "BodyPartsArchotech";
        private const string TradeTag = "ExoticMisc";
        private const string RewardTag = "RewardStandardLowFreq";

        private const float ShardValue = 600f;

        /// <summary>Silver of part value one shard is worth building with. buildCost = value / this.</summary>
        private const float ValuePerShard = 700f;

        private static readonly string[] OutsideTheLoop =
        {
            "PsychicAmplifier", "PsychicSensitizer", "PsychicReader", "PsychicHarmonizer",
            "PsychicLevitator", "PsychicNullifier", "PsychokeneticShield",
        };

        private class Loop
        {
            public ThingDef Part;
            public RecipeDef Build;
            public RecipeDef Teardown;
            public int BuildCost;
            public int Yield;

            public int ExpectedBuildCost =>
                Math.Max(2, (int)Math.Round(Part.BaseMarketValue / ValuePerShard, MidpointRounding.AwayFromZero));

            public int ExpectedYield => (int)Math.Floor(ExpectedBuildCost / 2.0);
        }

        [Test]
        public static void ArchotechStackJoinsTheLoop()
        {
            if (!Check.Ready(Key, Ids.AlteredCarbon) || !LoopWired())
                return;

            ThingDef stack = DefDatabase<ThingDef>.GetNamedSilentFail("AC_EmptyArchotechStack");
            if (!Check.Soft(stack != null,
                    "AC_EmptyArchotechStack not found although Altered Carbon is active"))
            {
                Check.SoftResult();
                return;
            }

            RecipeDef build = DefDatabase<RecipeDef>.GetNamedSilentFail("RBP_BuildArchotechStack");
            RecipeDef teardown = DefDatabase<RecipeDef>.GetNamedSilentFail("RBP_TeardownArchotechStack");
            Check.Soft(build != null, "RBP_BuildArchotechStack not found - the stack cannot be made");
            Check.Soft(teardown != null, "RBP_TeardownArchotechStack not found - the loop is one-way");

            ThingDef arm = DefDatabase<ThingDef>.GetNamedSilentFail("ArchotechArm");
            if (arm != null)
                Check.Soft(stack.BaseMarketValue > arm.BaseMarketValue,
                    $"the empty archotech stack is worth {stack.BaseMarketValue:0} against an " +
                    $"archotech arm's {arm.BaseMarketValue:0}, so the loop would make it no dearer " +
                    "than a limb");

            Check.Soft(stack.thingSetMakerTags != null && stack.thingSetMakerTags.Contains(RewardTag),
                "the empty archotech stack has lost its reward tag; making it craftable was meant " +
                "to add a route, not close the one that existed");

            Check.SoftResult();
        }

        [Test]
        public static void ShardIsPricedAndTagged()
        {
            if (!Check.Ready(Key) || !LoopWired())
                return;

            ThingDef shard = Check.Def<ThingDef>(ShardName);

            Check.Soft(Check.StatBase(shard, "MarketValue") == ShardValue,
                $"{ShardName} MarketValue statBase is {Show(Check.StatBase(shard, "MarketValue"))}, " +
                $"the recycling arithmetic is built on {ShardValue}");
            Check.Soft(Math.Abs(shard.BaseMarketValue - ShardValue) < 0.5f,
                $"{ShardName} resolves to {shard.BaseMarketValue:0.#} rather than {ShardValue} - something " +
                "gave it a cost list or another mod re-priced it, and every build cost below is now wrong");
            Check.Soft(shard.techLevel == TechLevel.Archotech,
                $"{ShardName} techLevel is {shard.techLevel}, expected Archotech");
            Check.Soft(shard.stackLimit > 1,
                $"{ShardName} has stackLimit {shard.stackLimit}; a resource you hold four of at a time " +
                "must stack");

            Check.Soft(shard.tradeability == Tradeability.All,
                $"{ShardName} tradeability is {shard.tradeability}; only All lets a trader stock it");

            Check.Soft(shard.tradeTags != null && shard.tradeTags.Contains(TradeTag),
                $"{ShardName} tradeTags are [{Join(shard.tradeTags)}], expected to include {TradeTag}");
            Check.Soft(shard.thingSetMakerTags != null && shard.thingSetMakerTags.Contains(RewardTag),
                $"{ShardName} thingSetMakerTags are [{Join(shard.thingSetMakerTags)}], expected to include {RewardTag}");

            Check.SoftResult();
        }

        [Test]
        public static void ShardTagsAreLiveInTradeAndRewards()
        {
            if (!Check.Ready(Key) || !LoopWired())
                return;

            List<string> stocked = TradeTagsTradersStock();
            if (Check.Soft(stocked.Count > 0,
                "no trader in the database stocks by trade tag at all - this test's reflection over " +
                "StockGenerator has stopped finding anything and asserts nothing"))
            {
                Check.Note($"traders stock {stocked.Count} distinct trade tag(s)");
                Check.Soft(stocked.Contains(TradeTag),
                    $"no trader stocks the '{TradeTag}' trade tag, so shards can never be bought; " +
                    $"tags that are stocked: [{Join(stocked)}]");
                Check.Note("stocking traders: " + Join(TradersStocking(TradeTag)));
            }

            List<string> rewardTags = RewardTagsSetMakersAllow();
            if (Check.Soft(rewardTags.Count > 0,
                "no ThingSetMakerDef in the database allows by thingSetMakerTag at all - this test's " +
                "reflection over ThingFilter has stopped finding anything and asserts nothing"))
            {
                Check.Note($"reward generators allow {rewardTags.Count} distinct thingSetMakerTag(s)");
                Check.Soft(rewardTags.Contains(RewardTag),
                    $"no reward generator allows the '{RewardTag}' tag, so shards can never turn up as a " +
                    $"quest reward; tags that are allowed: [{Join(rewardTags)}]");
            }

            Check.SoftResult();
        }

        [Test]
        public static void EveryArchotechPartHasBothRecipes()
        {
            if (!Check.Ready(Key) || !LoopWired())
                return;

            List<Loop> loops = Sweep();
            var covered = new List<ThingDef>();
            foreach (Loop loop in loops)
                covered.Add(loop.Part);

            var names = new List<string>();
            foreach (Loop loop in loops)
                names.Add($"{loop.Part.defName}({loop.Part.BaseMarketValue:0}: {loop.BuildCost}/{loop.Yield})");
            Check.Note($"loop covers {loops.Count} part(s): " + Join(names));

            foreach (Loop loop in loops)
            {
                Check.Soft(loop.Build != null,
                    $"{loop.Part.defName} can be torn down for shards but not built from them - a " +
                    "one-way hole in the loop");
                Check.Soft(loop.Teardown != null,
                    $"{loop.Part.defName} can be built from shards but not torn down for them - a " +
                    "one-way hole in the loop");
            }

            var missing = new List<string>();
            foreach (ThingDef part in AllArchotechParts())
            {
                if (covered.Contains(part) || Array.IndexOf(OutsideTheLoop, part.defName) >= 0)
                    continue;
                missing.Add($"{part.defName} ({part.label}, {ModOf(part)}, value {part.BaseMarketValue:0})");
            }
            Check.Soft(missing.Count == 0,
                "archotech body part(s) exist that the recycling loop does not reach - either add " +
                "them to CyberneticsResearchShards.xml or record why they are out: " + Join(missing));

            Check.Soft(loops.Count >= 3,
                $"the sweep found only {loops.Count} part(s) in the loop; Core alone supplies three, " +
                "so the patch is no longer applying");

            Check.SoftResult();
        }

        [Test]
        public static void BuildAndTeardownFollowTheRule()
        {
            if (!Check.Ready(Key) || !LoopWired())
                return;

            ThingDef shard = Check.Def<ThingDef>(ShardName);
            List<Loop> loops = Sweep();

            foreach (Loop loop in loops)
            {
                if (loop.Build != null)
                {
                    Check.Soft(loop.BuildCost == loop.ExpectedBuildCost,
                        $"{loop.Build.defName} costs {loop.BuildCost} shard(s); {loop.Part.defName} is " +
                        $"worth {loop.Part.BaseMarketValue:0}, so the rule max(2, round(value/700)) says " +
                        $"{loop.ExpectedBuildCost}");
                    Check.Soft(CountIngredient(loop.Build, ThingDefOf.Plasteel) == 20,
                        $"{loop.Build.defName} takes {CountIngredient(loop.Build, ThingDefOf.Plasteel)} " +
                        "plasteel, every archotech assembly takes 20");
                    Check.Soft(Math.Abs(loop.Build.WorkAmountTotal(null) - 61000f) < 1f,
                        $"{loop.Build.defName} is {loop.Build.WorkAmountTotal(null):0} work, " +
                        "61000 is what puts a 2800 part on its own price");
                    Check.Soft(SoleProduct(loop.Build) == loop.Part,
                        $"{loop.Build.defName} does not produce exactly one {loop.Part.defName}");
                }

                if (loop.Teardown != null)
                {
                    Check.Soft(loop.Yield == loop.ExpectedYield,
                        $"{loop.Teardown.defName} yields {loop.Yield} shard(s); the rule " +
                        $"floor(buildCost/2) over a build cost of {loop.ExpectedBuildCost} says " +
                        $"{loop.ExpectedYield}");
                    Check.Soft(CountIngredient(loop.Teardown, loop.Part) == 1,
                        $"{loop.Teardown.defName} consumes " +
                        $"{CountIngredient(loop.Teardown, loop.Part)} {loop.Part.defName}, expected 1");
                }
            }

            foreach (Loop loop in loops)
            {
                if (loop.Build == null || loop.Teardown == null)
                    continue;
                Check.Soft(loop.Yield * 2 == loop.BuildCost || loop.Yield * 2 + 1 == loop.BuildCost,
                    $"{loop.Part.defName}: {loop.Yield} shard(s) out and {loop.BuildCost} in, which is " +
                    "not the two-salvaged-make-one ratio");
            }

            Check.Note($"shard priced at {shard.BaseMarketValue:0}; " +
                $"a {ShardValue * 4 + 180f + 0.0036f * 61000f:0.#} silver build against a 2800 part");

            Check.SoftResult();
        }

        [Test]
        public static void LoopCanNeverDuplicateValue()
        {
            if (!Check.Ready(Key) || !LoopWired())
                return;

            ThingDef shard = Check.Def<ThingDef>(ShardName);
            List<Loop> loops = Sweep();

            foreach (Loop loop in loops)
            {
                if (loop.Build != null && loop.Teardown != null)
                    Check.Soft(loop.Yield < loop.BuildCost,
                        $"{loop.Part.defName} gives back {loop.Yield} shard(s) and costs {loop.BuildCost} " +
                        "to build - at parity or better the bench is a free duplicator");

                if (loop.Teardown != null)
                    Check.Soft(loop.Yield * shard.BaseMarketValue < loop.Part.BaseMarketValue,
                        $"tearing down {loop.Part.defName} ({loop.Part.BaseMarketValue:0}) returns " +
                        $"{loop.Yield} shard(s) worth {loop.Yield * shard.BaseMarketValue:0} - salvage " +
                        "must always cost value, never mint it");
            }

            // The hard rule: nothing may produce a shard without eating an archotech part.
            var freeSources = new List<string>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (CountProduct(recipe, shard) <= 0)
                    continue;
                if (!ConsumesAnArchotechPart(recipe))
                    freeSources.Add(recipe.defName);
            }
            Check.Soft(freeSources.Count == 0,
                "recipe(s) produce archotech shards without consuming an archotech part, which turns " +
                "the conversion loop into a production line: " + Join(freeSources));

            Check.Soft(shard.recipeMaker == null,
                $"{ShardName} has gained a recipeMaker, so shards can be crafted at a bench directly");

            Check.SoftResult();
        }

        [Test]
        public static void RecipesAreGatedOnArchotechCybernetics()
        {
            if (!Check.Ready(Key) || !LoopWired())
                return;

            List<Loop> loops = Sweep();
            var recipes = new List<RecipeDef>();
            foreach (Loop loop in loops)
            {
                if (loop.Build != null) recipes.Add(loop.Build);
                if (loop.Teardown != null) recipes.Add(loop.Teardown);
            }
            Check.Note($"{recipes.Count} recycling recipe(s) checked");

            foreach (RecipeDef recipe in recipes)
            {
                bool single = recipe.researchPrerequisite != null
                    && recipe.researchPrerequisite.defName == ResearchName;
                bool listed = Check.ContainsResearch(recipe.researchPrerequisites, ResearchName);
                Check.Soft(single || listed,
                    $"{recipe.defName} is gated on " +
                    $"{(recipe.researchPrerequisite == null ? "nothing" : recipe.researchPrerequisite.defName)}" +
                    $"{(recipe.researchPrerequisites == null ? "" : " plus [" + JoinDefs(recipe.researchPrerequisites) + "]")}" +
                    $", expected {ResearchName}");

                Check.Soft(recipe.recipeUsers != null && ContainsDef(recipe.recipeUsers, "FabricationBench"),
                    $"{recipe.defName} recipeUsers are [{JoinDefs(recipe.recipeUsers)}], expected to " +
                    "include FabricationBench");
                Check.Soft(recipe.recipeUsers == null || !ContainsDef(recipe.recipeUsers, "Human"),
                    $"{recipe.defName} has become a surgery - archotech recycling is bench work");
            }

            Check.SoftResult();
        }

        // ------------------------------------------------------------------ sweeps and helpers

        private static bool LoopWired()
        {
            if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ResearchName) != null)
                return true;
            Log.Message($"[RBP Tests] SKIP {Key}: {ResearchName} not present " +
                "(cyberneticsresearch.body is off), so the recycling patch deliberately did nothing");
            return false;
        }

        private static List<Loop> Sweep()
        {
            var loops = new List<Loop>();
            ThingDef shard = DefDatabase<ThingDef>.GetNamedSilentFail(ShardName);
            if (shard == null)
                return loops;

            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                int shardsIn = CountIngredient(recipe, shard);
                int shardsOut = CountProduct(recipe, shard);

                if (shardsIn > 0 && shardsOut == 0)
                {
                    ThingDef product = SoleProduct(recipe);
                    if (product == null)
                        continue;
                    Loop loop = For(loops, product);
                    loop.Build = recipe;
                    loop.BuildCost = shardsIn;
                }
                else if (shardsOut > 0 && shardsIn == 0)
                {
                    ThingDef eaten = SoleIngredientOtherThan(recipe, shard);
                    if (eaten == null)
                        continue;
                    Loop loop = For(loops, eaten);
                    loop.Teardown = recipe;
                    loop.Yield = shardsOut;
                }
            }
            return loops;
        }

        private static Loop For(List<Loop> loops, ThingDef part)
        {
            foreach (Loop loop in loops)
                if (loop.Part == part)
                    return loop;
            var fresh = new Loop { Part = part };
            loops.Add(fresh);
            return fresh;
        }

        private static List<ThingDef> AllArchotechParts()
        {
            var found = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.techLevel != TechLevel.Archotech || !HasCategory(def, ArchotechCategory))
                    continue;
                found.Add(def);
            }
            return found;
        }

        private static bool ConsumesAnArchotechPart(RecipeDef recipe)
        {
            if (recipe.ingredients == null)
                return false;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                ThingDef def = ingredient.IsFixedIngredient ? ingredient.FixedIngredient : null;
                if (def == null)
                    continue;
                if (HasCategory(def, ArchotechCategory) || def.techLevel == TechLevel.Archotech)
                    return true;
            }
            return false;
        }

        private static bool HasCategory(ThingDef def, string categoryDefName)
        {
            if (def.thingCategories == null)
                return false;
            foreach (ThingCategoryDef category in def.thingCategories)
                if (category?.defName == categoryDefName)
                    return true;
            return false;
        }

        private static int CountIngredient(RecipeDef recipe, ThingDef wanted)
        {
            if (recipe.ingredients == null || wanted == null)
                return 0;
            int total = 0;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                if (!ingredient.IsFixedIngredient || ingredient.FixedIngredient != wanted)
                    continue;
                total += (int)ingredient.GetBaseCount();
            }
            return total;
        }

        private static int CountProduct(RecipeDef recipe, ThingDef wanted)
        {
            if (recipe.products == null || wanted == null)
                return 0;
            int total = 0;
            foreach (ThingDefCountClass product in recipe.products)
                if (product.thingDef == wanted)
                    total += product.count;
            return total;
        }

        private static ThingDef SoleProduct(RecipeDef recipe)
        {
            if (recipe.products == null || recipe.products.Count != 1 || recipe.products[0].count != 1)
                return null;
            return recipe.products[0].thingDef;
        }

        private static ThingDef SoleIngredientOtherThan(RecipeDef recipe, ThingDef excluded)
        {
            if (recipe.ingredients == null)
                return null;
            ThingDef only = null;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                if (!ingredient.IsFixedIngredient)
                    return null;
                ThingDef def = ingredient.FixedIngredient;
                if (def == null || def == excluded)
                    continue;
                if (only != null)
                    return null;
                only = def;
            }
            return only;
        }

        private static List<string> TradeTagsTradersStock()
        {
            var tags = new List<string>();
            foreach (TraderKindDef trader in DefDatabase<TraderKindDef>.AllDefsListForReading)
            {
                if (trader.stockGenerators == null)
                    continue;
                foreach (StockGenerator generator in trader.stockGenerators)
                    CollectSellTags(generator, tags);
            }
            return tags;
        }

        private static List<string> TradersStocking(string tag)
        {
            var traders = new List<string>();
            foreach (TraderKindDef trader in DefDatabase<TraderKindDef>.AllDefsListForReading)
            {
                if (trader.stockGenerators == null)
                    continue;
                var tags = new List<string>();
                foreach (StockGenerator generator in trader.stockGenerators)
                    CollectSellTags(generator, tags);
                if (tags.Contains(tag))
                    traders.Add(trader.defName);
            }
            return traders;
        }

        private static void CollectSellTags(object generator, List<string> into)
        {
            if (generator == null)
                return;
            foreach (FieldInfo field in generator.GetType()
                         .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.Name != "tradeTag" && field.Name != "tradeTags")
                    continue;
                Add(field.GetValue(generator), into);
            }
        }

        private static List<string> RewardTagsSetMakersAllow()
        {
            var tags = new List<string>();
            foreach (ThingSetMakerDef def in DefDatabase<ThingSetMakerDef>.AllDefsListForReading)
                CollectAllowTags(def.root, tags, 0);
            return tags;
        }

        private static void CollectAllowTags(object node, List<string> into, int depth)
        {
            if (node == null || depth > 10 || node is string || node is Def)
                return;

            if (node is ThingFilter filter)
            {
                FieldInfo field = typeof(ThingFilter).GetField("thingSetMakerTagsToAllow",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    Add(field.GetValue(filter), into);
                return;
            }

            if (node is IEnumerable list)
            {
                foreach (object item in list)
                    CollectAllowTags(item, into, depth + 1);
                return;
            }

            Type type = node.GetType();
            if (type.IsPrimitive || type.IsEnum)
                return;
            if (type.Namespace == null
                || (!type.Namespace.StartsWith("RimWorld") && !type.Namespace.StartsWith("Verse")))
                return;

            foreach (FieldInfo field in type.GetFields(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string))
                    continue;
                CollectAllowTags(field.GetValue(node), into, depth + 1);
            }
        }

        private static void Add(object value, List<string> into)
        {
            if (value is string single)
            {
                if (!into.Contains(single))
                    into.Add(single);
                return;
            }
            if (value is IEnumerable many)
                foreach (object item in many)
                    if (item is string text && !into.Contains(text))
                        into.Add(text);
        }

        private static bool ContainsDef(List<ThingDef> defs, string defName)
        {
            if (defs == null)
                return false;
            foreach (ThingDef def in defs)
                if (def?.defName == defName)
                    return true;
            return false;
        }

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "this mod or a patch";

        private static string Show(float? value) => value.HasValue ? value.Value.ToString("0.##") : "absent";

        private static string Join(List<string> items) =>
            items == null || items.Count == 0 ? "none" : string.Join(", ", items.ToArray());

        private static string JoinDefs<T>(List<T> defs) where T : Def
        {
            if (defs == null || defs.Count == 0)
                return "none";
            var names = new List<string>();
            foreach (T def in defs)
                names.Add(def?.defName ?? "null");
            return string.Join(", ", names.ToArray());
        }
    }
}
