using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VAEWasteTests
    {
        [Test]
        public static void ToxicMeatUncheckedByDefault()
        {
            if (!Check.Ready("vanilla.toxicmeat", Ids.VAEWaste))
                return;
            ThingDef toxicMeat = Check.Def<ThingDef>("VAEWaste_ToxicMeat");
            ThingFilter hopperFilter = Check.Def<ThingDef>("Hopper").building?.defaultStorageSettings?.filter;
            Check.True(hopperFilter != null, "Hopper has no default storage filter");
            Check.True(!hopperFilter.Allows(toxicMeat), "Hopper default storage still allows toxic meat");
            RecipeDef cook = Check.Def<RecipeDef>("CookMealSimple");
            Check.True(cook.defaultIngredientFilter != null && !cook.defaultIngredientFilter.Allows(toxicMeat),
                "CookMealSimple default ingredients still allow toxic meat");
        }
    }
}
