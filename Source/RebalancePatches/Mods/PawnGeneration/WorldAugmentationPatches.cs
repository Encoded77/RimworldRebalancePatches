using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RebalancePatches.Mods.PawnGeneration
{
    /// <summary>
    /// Vanilla only ever augments a pawn through its PawnKindDef's small techHediffs budget, so with a
    /// deep implant modlist the rest of the world still looks unaugmented. This adds bionics (and, when
    /// enabled, GiTS cyberbrains) to generated non-player pawns weighted by their faction's tech level:
    /// spacer and ultra factions substantially, industrial modestly, tribal and medieval never. It runs
    /// on top of vanilla's own generation and reuses vanilla's market-value weighting and recipe install,
    /// drawing only on implants that are actually reachable (their techHediffsTags match), so it stays
    /// compatible with whatever implant mods are present without referencing any of them directly.
    /// </summary>
    internal static class WorldAugmentationPatches
    {
        private const string BionicsKey = "worldaugment.bionics";
        private const string CyberbrainsKey = "worldaugment.cyberbrains";
        private const string FrequencyKey = "worldaugment.frequency";

        private const string BionicTag = "Advanced";
        private const string CyberbrainCommonTag = "GiTS_TechHediff_CyberbrainCommon";
        private const string CyberbrainAdvancedTag = "GiTS_TechHediff_CyberbrainAdvanced";

        private static readonly List<Thing> NoIngredients = new List<Thing>();

        private static Tier[] tiers;
        private static float frequency;
        private static List<ThingDef> reachablePool;

        internal sealed class Tier
        {
            public TechLevel techLevel;
            public float chance;
            public FloatRange budget;
            public int maxSlots;
            public HashSet<string> tags;
        }

        public static void TryApply(Harmony harmony)
        {
            if (!SettingsRegistry.GetEffective(BionicsKey))
                return;
            try
            {
                bool cyberbrains = SettingsRegistry.GetEffective(CyberbrainsKey)
                    && ModsConfig.IsActive("moistestwhale.gitscyberbrains");
                frequency = SettingsRegistry.GetEffectiveValue(FrequencyKey) / 100f;
                tiers = BuildTiers(cyberbrains);
                reachablePool = null;   // rebuilt lazily once the def database is complete

                harmony.Patch(
                    AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                        new[] { typeof(PawnGenerationRequest) }),
                    postfix: new HarmonyMethod(typeof(WorldAugmentationPatches), nameof(GeneratePawnPostfix)));
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not patch world augmentation:\n{ex}");
            }
        }

        internal static Tier[] BuildTiers(bool cyberbrains)
        {
            var bionic = new HashSet<string> { BionicTag };
            var spacer = new HashSet<string> { BionicTag };
            var ultra = new HashSet<string> { BionicTag };
            if (cyberbrains)
            {
                spacer.Add(CyberbrainCommonTag);
                ultra.Add(CyberbrainCommonTag);
                ultra.Add(CyberbrainAdvancedTag);
            }
            return new[]
            {
                new Tier { techLevel = TechLevel.Ultra, chance = 0.50f, budget = new FloatRange(3000f, 7000f), maxSlots = 3, tags = ultra },
                new Tier { techLevel = TechLevel.Spacer, chance = 0.35f, budget = new FloatRange(2000f, 4500f), maxSlots = 2, tags = spacer },
                new Tier { techLevel = TechLevel.Industrial, chance = 0.15f, budget = new FloatRange(900f, 2200f), maxSlots = 2, tags = bionic },
            };
        }

        private static void GeneratePawnPostfix(Pawn __result)
        {
            try
            {
                Augment(__result);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Rebalance Patches] World augmentation postfix failed on " +
                            $"{__result?.kindDef?.defName ?? "?"}:\n{ex}");
            }
        }

        internal static Tier TierForLevel(TechLevel level)
        {
            if (tiers == null)
                return null;
            foreach (Tier t in tiers)
                if (t.techLevel == level)
                    return t;
            return null;
        }

        internal static void Augment(Pawn pawn)
        {
            if (pawn?.health == null || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
                return;
            if (pawn.DevelopmentalStage != DevelopmentalStage.Adult)
                return;
            Faction faction = pawn.Faction;
            if (faction == null || faction.IsPlayer || faction.def == null)
                return;

            Tier tier = TierForLevel(faction.def.techLevel);
            if (tier == null)
                return;

            float chance = tier.chance * frequency;
            if (chance <= 0f)
                return;

            float budget = tier.budget.RandomInRange;
            var installed = new HashSet<ThingDef>();
            for (int slot = 0; slot < tier.maxSlots; slot++)
            {
                if (Rand.Value > chance)
                    continue;

                ThingDef part = PickPart(pawn, tier.tags, budget, installed);
                if (part == null)
                    continue;

                installed.Add(part);
                if (TryInstall(pawn, part))
                    budget -= part.BaseMarketValue;
            }
        }

        private static ThingDef PickPart(Pawn pawn, HashSet<string> tags, float budget, HashSet<ThingDef> exclude)
        {
            List<ThingDef> candidates = null;
            foreach (ThingDef def in ReachablePool())
            {
                if (exclude.Contains(def) || def.BaseMarketValue > budget)
                    continue;
                if (!def.techHediffsTags.Any(tags.Contains))
                    continue;
                if (!CanInstall(pawn, def))
                    continue;
                (candidates ??= new List<ThingDef>()).Add(def);
            }
            return candidates == null ? null : candidates.RandomElementByWeight(d => d.BaseMarketValue);
        }

        // Every non-violent tech-hediff item carrying a tag this feature can reach, cached because the
        // def database no longer changes once patches are applied. Per-tier tag filtering happens later.
        internal static List<ThingDef> ReachablePool()
        {
            if (reachablePool != null)
                return reachablePool;

            var reach = new HashSet<string> { BionicTag, CyberbrainCommonTag, CyberbrainAdvancedTag };
            reachablePool = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                if (def.isTechHediff && !def.violentTechHediff
                    && def.techHediffsTags != null && def.techHediffsTags.Any(reach.Contains))
                    reachablePool.Add(def);
            return reachablePool;
        }

        internal static bool CanInstall(Pawn pawn, ThingDef part)
        {
            foreach (RecipeDef recipe in InstallRecipes(pawn, part))
            {
                if (!recipe.targetsBodyPart)
                    return true;
                if (recipe.Worker.GetPartsToApplyOn(pawn, recipe).Any())
                    return true;
            }
            return part.GetCompProperties<CompProperties_UseEffectInstallImplant>() != null;
        }

        internal static bool TryInstall(Pawn pawn, ThingDef part)
        {
            List<RecipeDef> recipes = InstallRecipes(pawn, part).ToList();
            if (recipes.Count > 0)
            {
                RecipeDef recipe = recipes.RandomElement();
                if (!recipe.targetsBodyPart)
                {
                    recipe.Worker.ApplyOnPawn(pawn, null, null, NoIngredients, null);
                    return true;
                }
                List<BodyPartRecord> parts = recipe.Worker.GetPartsToApplyOn(pawn, recipe).ToList();
                if (parts.Count == 0)
                    return false;
                recipe.Worker.ApplyOnPawn(pawn, parts.RandomElement(), null, NoIngredients, null);
                return true;
            }

            CompProperties_UseEffectInstallImplant comp = part.GetCompProperties<CompProperties_UseEffectInstallImplant>();
            if (comp == null)
                return false;
            List<BodyPartRecord> targets = pawn.RaceProps.body.GetPartsWithDef(comp.bodyPart);
            pawn.health.AddHediff(comp.hediffDef, targets.NullOrEmpty() ? null : targets.RandomElement());
            return true;
        }

        private static IEnumerable<RecipeDef> InstallRecipes(Pawn pawn, ThingDef part) =>
            DefDatabase<RecipeDef>.AllDefs.Where(r => r.IsIngredient(part) && pawn.def.AllRecipes.Contains(r));
    }
}
