using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class OdysseyTests
    {
        [Test]
        public static void OdysseyFactionsGainXenotypes()
        {
            if (!Check.Ready("xenotypes.odyssey", Ids.Odyssey))
                return;
            FactionDef salvagers = Check.Def<FactionDef>("Salvagers");
            var perMod = new Dictionary<string, string[]>
            {
                { Ids.RimsenalZohar, new[] { "Zohar" } },
                { Ids.RimsenalAskbarn, new[] { "Askbarn", "Uredd" } },
                { Ids.RimsenalHarana, new[] { "Harana" } },
                { Ids.Venators, new[] { "DV_Venator" } },
                { Ids.Keshig, new[] { "DV_Keshig" } },
                { Ids.AlphaGenes, new[] { "AG_Fleetkind" } },
            };
            foreach (KeyValuePair<string, string[]> pair in perMod)
            {
                if (!ModsConfig.IsActive(pair.Key))
                    continue;
                foreach (string xenotype in pair.Value)
                    Check.True(Check.HasXenotype(salvagers, xenotype), $"Salvagers lack xenotype {xenotype}");
            }
            if (ModsConfig.IsActive(Ids.AlphaGenes))
                Check.True(Check.HasXenotype(Check.Def<FactionDef>("TradersGuild"), "AG_Fleetkind"),
                    "TradersGuild lacks xenotype AG_Fleetkind");
        }

        [Test]
        public static void LongRangePassengerShuttle()
        {
            if (!Check.Ready("odyssey.shuttle", Ids.Odyssey))
                return;
            ThingDef shuttle = Check.Def<ThingDef>("PassengerShuttle");
            CompProperties_Refuelable refuelable = shuttle.GetCompProperties<CompProperties_Refuelable>();
            Check.True(refuelable != null, "PassengerShuttle has no CompProperties_Refuelable");
            Check.Eq(refuelable.fuelCapacity, 2000f, "PassengerShuttle fuelCapacity");
            Check.Eq(refuelable.initialConfigurableTargetFuelLevel, 2000f, "PassengerShuttle initialConfigurableTargetFuelLevel");
            CompProperties_Transporter transporter = shuttle.GetCompProperties<CompProperties_Transporter>();
            Check.True(transporter != null, "PassengerShuttle has no CompProperties_Transporter");
            Check.Eq(transporter.massCapacity, 2000f, "PassengerShuttle massCapacity");
        }
    }
}
