using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VSIETests
    {
        [Test]
        public static void FrenziesRespectPrecepts()
        {
            if (!Check.Ready("memes.inspirations", Ids.VSIE))
                return;
            foreach (string inspiration in new[] { "VSIE_Melee_Frenzy", "VSIE_Inspired_Planting", "VSIE_Inspired_Research",
                "VSIE_Inspired_Mining", "VSIE_Party_Frenzy", "VSIE_Flirting_Frenzy" })
            {
                InspirationNullifyingExtension extension = Check.Def<InspirationDef>(inspiration)
                    .GetModExtension<InspirationNullifyingExtension>();
                Check.True(extension != null, $"{inspiration} lacks InspirationNullifyingExtension");
            }
            if (ModsConfig.IsActive(Ids.Ideology))
                Check.True(Check.AnyDefNamed(Check.Def<InspirationDef>("VSIE_Inspired_Mining")
                        .GetModExtension<InspirationNullifyingExtension>().nullifyingPrecepts, "Mining_Prohibited"),
                    "VSIE_Inspired_Mining not nullified by Mining_Prohibited");
        }
    }
}
