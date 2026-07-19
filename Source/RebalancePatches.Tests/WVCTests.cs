using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class WVCTests
    {
        [Test]
        public static void WvcXenotypesLeaveGenericFactions()
        {
            if (!Check.Ready("xenotypes.wvcchances", Ids.WVC))
                return;
            void NotIn(string factionName, string xenotype)
            {
                FactionDef faction = Check.Optional<FactionDef>(factionName, "xenotypes.wvcchances");
                if (faction != null)
                    Check.True(!Check.HasXenotype(faction, xenotype), $"{factionName} still spawns {xenotype}");
            }
            NotIn("PirateYttakin", "WVC_Featherdust");
            NotIn("Beggars", "WVC_CatDeity");
            NotIn("Beggars", "WVC_Blank");
            if (ModsConfig.IsActive(Ids.Anomaly))
            {
                foreach (string faction in new[] { "OutlanderCivil", "TribeRough", "TribeCivil", "PirateWaster", "Empire", "Beggars" })
                    NotIn(faction, "WVC_Undead");
                NotIn("TribeSavageImpid", "WVC_Sandycat");
                NotIn("PirateYttakin", "WVC_Sandycat");
                FactionDef horax = Check.Def<FactionDef>("HoraxCult");
                Check.True(Check.HasXenotype(horax, "WVC_Undead"), "HoraxCult lacks WVC_Undead");
                Check.True(Check.HasXenotype(horax, "WVC_Sandycat"), "HoraxCult lacks WVC_Sandycat");
            }
        }
    }
}
