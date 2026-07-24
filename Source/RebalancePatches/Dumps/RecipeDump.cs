using System;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    internal static class RecipeDump
    {
        private static readonly Type[] Referenced =
        {
            typeof(HediffDef), typeof(BodyPartDef), typeof(BodyPartGroupDef), typeof(ResearchProjectDef),
            typeof(ThingDef), typeof(StatDef), typeof(EffecterDef),
        };

        [DebugAction("RebalancePatches", "Dump recipes", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump recipes", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(Referenced, bareDefTypes: new[] { typeof(RecipeDef) });
            int total = 0, surgeries = 0;

            DumpRunner.Run("RecipeDump.json", walker, w =>
            {
                Json j = w.Json;
                j.Name("recipes");
                j.BeginArray();
                foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading
                             .OrderBy(r => r.defName, StringComparer.Ordinal))
                {
                    w.WriteDefEntry(recipe, _ =>
                    {
                        j.Name("isSurgery"); j.Value(recipe.IsSurgery);
                        j.Name("workerClass"); j.Value(recipe.workerClass?.FullName);

                        j.Name("workerBaseChain");
                        j.BeginArray();
                        for (Type t = recipe.workerClass; t != null && t != typeof(object); t = t.BaseType)
                            j.Value(t.Name);
                        j.EndArray();

                        j.Name("allRecipeUsers");
                        j.BeginArray();
                        foreach (string user in (recipe.AllRecipeUsers ?? Enumerable.Empty<ThingDef>())
                                     .Where(u => u != null).Select(u => u.defName).Distinct()
                                     .OrderBy(s => s, StringComparer.Ordinal))
                            j.Value(user);
                        j.EndArray();

                        j.Name("implantIngredient");
                        j.Value(ImplantIngredientOf(recipe)?.defName);

                        j.Name("resolvedIngredients");
                        j.BeginArray();
                        if (recipe.ingredients != null)
                            foreach (IngredientCount ing in recipe.ingredients)
                            {
                                if (ing?.filter == null) continue;
                                j.BeginObject();
                                j.Name("count"); j.Number(ing.GetBaseCount());
                                j.Name("allowed");
                                j.BeginArray();
                                foreach (ThingDef thing in ing.filter.AllowedThingDefs
                                             .Where(t => t != null).OrderBy(t => t.defName, StringComparer.Ordinal))
                                    j.Value(thing.defName);
                                j.EndArray();
                                j.EndObject();
                            }
                        j.EndArray();
                    });
                    total++;
                    if (recipe.IsSurgery) surgeries++;
                }
                j.EndArray();
            }, () => $"{total} recipes ({surgeries} surgeries)");
        }

        private static ThingDef ImplantIngredientOf(RecipeDef recipe)
        {
            if (recipe.ingredients == null) return null;
            ThingDef fallback = null;
            foreach (IngredientCount ing in recipe.ingredients)
            {
                if (ing?.filter == null) continue;
                foreach (ThingDef thing in ing.filter.AllowedThingDefs)
                {
                    if (thing == null || thing.IsMedicine) continue;
                    if (thing.isTechHediff) return thing;
                    if (thing.thingCategories != null)
                        foreach (ThingCategoryDef cat in thing.thingCategories)
                            if (cat != null && cat.defName.IndexOf("BodyPart", StringComparison.OrdinalIgnoreCase) >= 0)
                                return thing;
                    fallback ??= thing;
                }
            }
            return fallback;
        }
    }
}
