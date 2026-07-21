using System;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Every RecipeDef — surgeries and bench recipes alike. Surgeries carry the implant ecosystem;
    /// bench recipes carry what an item costs to make. Keeping both in one dump means the analyzer
    /// can ask "what installs this and what does making it cost" from a single file.
    ///
    /// The only computed field is <c>workerBaseChain</c>: recipe behaviour lives in a C# class
    /// hierarchy that no longer exists once the game is closed, so the chain has to be captured
    /// here. Everything else — research chains, which bodies have the target part, whether an
    /// android may take it — is a join the analyzer does across dumps.
    /// </summary>
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

                        // Base chain, so the analyzer can ask "is this a Recipe_InstallImplant?"
                        // without the type ever being loaded.
                        j.Name("workerBaseChain");
                        j.BeginArray();
                        for (Type t = recipe.workerClass; t != null && t != typeof(object); t = t.BaseType)
                            j.Value(t.Name);
                        j.EndArray();

                        // AllRecipeUsers folds in ThingDefs that list the recipe themselves, which
                        // recipeUsers alone misses.
                        j.Name("allRecipeUsers");
                        j.BeginArray();
                        foreach (string user in (recipe.AllRecipeUsers ?? Enumerable.Empty<ThingDef>())
                                     .Where(u => u != null).Select(u => u.defName).Distinct()
                                     .OrderBy(s => s, StringComparer.Ordinal))
                            j.Value(user);
                        j.EndArray();

                        // Ingredient filters resolve to concrete defs only while the database is
                        // live, so flatten them now.
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
    }
}
