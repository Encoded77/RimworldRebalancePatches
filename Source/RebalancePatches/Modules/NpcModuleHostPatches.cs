using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    internal static class NpcModuleHostPatches
    {
        private const string SettingKey = "cybernetics.npchosts";

        private const int MaxHostsPerPawn = 2;

        private static readonly List<Thing> NoIngredients = new List<Thing>();

        private static bool warned;
        private static Dictionary<string, List<HostOption>> hostsBySlot;
        private static Dictionary<HediffDef, List<HostOption>> precursorsByHediff;

        private static int hostCountPawnId = -1;
        private static int hostsFitted;

        public static void TryApply(Harmony harmony)
        {
            if (!ModsConfig.IsActive("ebsg.framework"))
                return;
            try
            {
                Type patchType = AccessTools.TypeByName("EBSGFramework.HarmonyPatches");
                MethodInfo target = patchType == null
                    ? null
                    : AccessTools.Method(patchType, "InstallInitialPartPostfix");
                if (target == null)
                    throw new MissingMemberException("EBSGFramework.HarmonyPatches.InstallInitialPartPostfix not found");
                harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(NpcModuleHostPatches), nameof(InstallInitialPartPrefix)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Rebalance Patches] Could not hook module installation during pawn generation; "
                    + $"generated pawns given a module with no host will keep logging about it:\n{ex}");
            }
        }

        internal static bool InstallInitialPartPrefix(Pawn pawn, ThingDef partDef)
        {
            try
            {
                if (!SettingsRegistry.GetEffective(SettingKey))
                    return true;
                if (pawn?.health?.hediffSet == null || partDef == null)
                    return true;

                object moduleProps = ModuleApi.ModulePropsOf(partDef);
                if (moduleProps == null || ModuleApi.HasOpenSlotFor(pawn, moduleProps))
                    return true;

                TryFitHost(pawn, moduleProps);

                return ModuleApi.HasOpenSlotFor(pawn, moduleProps);
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning($"[Rebalance Patches] Fitting a module host during pawn generation failed:\n{ex}");
                }
                return true;
            }
        }

        private static void TryFitHost(Pawn pawn, object moduleProps)
        {
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
                return;

            List<string> needed = ModuleApi.RequiredSlotIds(moduleProps);
            if (needed.Count == 0 || AlreadyHosted(pawn, needed))
                return;
            if (HostsFittedOn(pawn) >= MaxHostsPerPawn)
                return;

            foreach (HostOption option in CandidatesFor(needed))
            {
                if (!TryApplyHost(pawn, option.recipe))
                    continue;
                hostsFitted++;
                return;
            }
        }

        private static bool TryApplyHost(Pawn pawn, RecipeDef host)
        {
            if (CanFit(pawn, host, out BodyPartRecord part))
            {
                host.Worker.ApplyOnPawn(pawn, part, null, NoIngredients, null);
                return true;
            }
            if (!TryApplyPrecursor(pawn, host))
                return false;
            if (CanFit(pawn, host, out part))
                host.Worker.ApplyOnPawn(pawn, part, null, NoIngredients, null);
            return true;
        }

        private static bool TryApplyPrecursor(Pawn pawn, RecipeDef host)
        {
            HediffDef input = host.removesHediff;
            if (input == null || pawn.health.hediffSet.HasHediff(input))
                return false;
            if (!PrecursorsByHediff().TryGetValue(input, out List<HostOption> options))
                return false;

            foreach (HostOption option in options)
            {
                if (option.recipe == host)
                    continue;
                if (!CanFit(pawn, option.recipe, out BodyPartRecord part))
                    continue;
                option.recipe.Worker.ApplyOnPawn(pawn, part, null, NoIngredients, null);
                return true;
            }
            return false;
        }

        private static bool AlreadyHosted(Pawn pawn, List<string> needed)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                foreach (string offered in ModuleApi.OfferedSlotIds(hediff.def))
                    if (needed.Contains(offered))
                        return true;
            return false;
        }

        private static bool CanFit(Pawn pawn, RecipeDef recipe, out BodyPartRecord part)
        {
            part = null;
            if (!pawn.def.AllRecipes.Contains(recipe))
                return false;
            if (!recipe.Worker.AvailableOnNow(pawn))
                return false;
            if (!recipe.targetsBodyPart)
                return true;
            List<BodyPartRecord> parts = recipe.Worker.GetPartsToApplyOn(pawn, recipe).ToList();
            if (parts.Count == 0)
                return false;
            part = parts.RandomElement();
            return true;
        }

        /// <summary>How many hosts have gone onto the pawn currently being generated.</summary>
        private static int HostsFittedOn(Pawn pawn)
        {
            if (pawn.thingIDNumber != hostCountPawnId)
            {
                hostCountPawnId = pawn.thingIDNumber;
                hostsFitted = 0;
            }
            return hostsFitted;
        }

        /// <summary>Every host that offers one of these slots, cheapest first.</summary>
        private static IEnumerable<HostOption> CandidatesFor(List<string> slotIds)
        {
            Dictionary<string, List<HostOption>> map = HostsBySlot();
            var pooled = new List<HostOption>();
            foreach (string slot in slotIds)
                if (map.TryGetValue(slot, out List<HostOption> options))
                    foreach (HostOption option in options)
                        if (!pooled.Contains(option))
                            pooled.Add(option);
            pooled.Sort(ByCost);
            return pooled;
        }

        internal static Dictionary<string, List<HostOption>> HostsBySlot()
        {
            if (hostsBySlot != null)
                return hostsBySlot;

            var map = new Dictionary<string, List<HostOption>>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.addsHediff == null)
                    continue;
                if (typeof(Recipe_InstallModule).IsAssignableFrom(recipe.workerClass))
                    continue;
                List<string> offered = ModuleApi.OfferedSlotIds(recipe.addsHediff);
                if (offered.Count == 0)
                    continue;

                var option = new HostOption(recipe, ChainCostOf(recipe));
                foreach (string slot in offered)
                {
                    if (!map.TryGetValue(slot, out List<HostOption> options))
                        map[slot] = options = new List<HostOption>();
                    options.Add(option);
                }
            }

            foreach (List<HostOption> options in map.Values)
                options.Sort(ByCost);
            hostsBySlot = map;
            return map;
        }

        internal static Dictionary<HediffDef, List<HostOption>> PrecursorsByHediff()
        {
            if (precursorsByHediff != null)
                return precursorsByHediff;

            var map = new Dictionary<HediffDef, List<HostOption>>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.addsHediff == null)
                    continue;
                if (typeof(Recipe_InstallModule).IsAssignableFrom(recipe.workerClass))
                    continue;
                if (!map.TryGetValue(recipe.addsHediff, out List<HostOption> options))
                    map[recipe.addsHediff] = options = new List<HostOption>();
                options.Add(new HostOption(recipe, CostOf(recipe)));
            }

            foreach (List<HostOption> options in map.Values)
                options.Sort(ByCost);
            precursorsByHediff = map;
            return map;
        }

        private static float ChainCostOf(RecipeDef recipe)
        {
            float total = CostOf(recipe);
            if (recipe.removesHediff == null)
                return total;
            if (!PrecursorsByHediff().TryGetValue(recipe.removesHediff, out List<HostOption> options))
                return total;
            foreach (HostOption option in options)
                if (option.recipe != recipe)
                    return total + option.cost;
            return total;
        }

        private static float CostOf(RecipeDef recipe)
        {
            float total = 0f;
            if (recipe.ingredients == null)
                return total;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                float cheapest = -1f;
                if (ingredient.filter != null)
                    foreach (ThingDef def in ingredient.filter.AllowedThingDefs)
                        if (cheapest < 0f || def.BaseMarketValue < cheapest)
                            cheapest = def.BaseMarketValue;
                if (cheapest > 0f)
                    total += cheapest * ingredient.GetBaseCount();
            }
            return total;
        }

        /// <summary>Cheapest first, then by name so the pick does not wobble between loads.</summary>
        private static int ByCost(HostOption a, HostOption b) =>
            a.cost == b.cost
                ? string.CompareOrdinal(a.recipe.defName, b.recipe.defName)
                : a.cost.CompareTo(b.cost);

        /// <summary>A surgery paired with what performing it costs, for ordering candidates.</summary>
        internal class HostOption
        {
            public readonly RecipeDef recipe;
            public readonly float cost;

            public HostOption(RecipeDef recipe, float cost)
            {
                this.recipe = recipe;
                this.cost = cost;
            }
        }
    }
}
