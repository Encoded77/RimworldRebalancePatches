using HarmonyLib;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CoreTests
    {
        [Test]
        public static void InspirationsRespectPrecepts()
        {
            if (!Check.Ready("memes.inspirations"))
                return;
            InspirationNullifyingExtension frenzy = Check.Def<InspirationDef>("Frenzy_Shoot")
                .GetModExtension<InspirationNullifyingExtension>();
            Check.True(frenzy != null, "Frenzy_Shoot lacks InspirationNullifyingExtension");
            InspirationNullifyingExtension taming = Check.Def<InspirationDef>("Inspired_Taming")
                .GetModExtension<InspirationNullifyingExtension>();
            Check.True(taming != null, "Inspired_Taming lacks InspirationNullifyingExtension");
            if (ModsConfig.IsActive(Ids.VIEMemes))
            {
                Check.True(Check.AnyDefNamed(frenzy.nullifyingPrecepts, "VME_Violence_Abhorrent"),
                    "Frenzy_Shoot not nullified by VME_Violence_Abhorrent");
                Check.True(Check.AnyDefNamed(taming.nullifyingPrecepts, "VME_Ranching_Disliked"),
                    "Inspired_Taming not nullified by VME_Ranching_Disliked");
            }
            if (ModsConfig.IsActive(Ids.AlphaMemes))
                Check.True(Check.AnyDefNamed(frenzy.nullifyingPrecepts, "AM_CombatProwess_Melee"),
                    "Frenzy_Shoot not nullified by AM_CombatProwess_Melee");
            if (ModsConfig.IsActive(Ids.Ideology))
                Check.HarmonyPatched(AccessTools.Method(typeof(InspirationWorker), "InspirationCanOccur"),
                    "memes.inspirations");
        }
    }
}
