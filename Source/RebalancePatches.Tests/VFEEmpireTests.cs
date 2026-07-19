using System.Collections;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VFEEmpireTests
    {
        [Test]
        public static void RoyalArmchairSatisfiesThroneRoom()
        {
            if (!Check.Ready("vfeempire.qol", Ids.VFEEmpire, Ids.VFECore, Ids.Royalty))
                return;
            ThingDef armchair = Check.Def<ThingDef>("Seat_RoyalArmchair");
            int titlesChecked = 0;
            foreach (RoyalTitleDef title in DefDatabase<RoyalTitleDef>.AllDefsListForReading)
            {
                if (title.favorCost != 308 && title.defName != "VFEE_HighStellarch")
                    continue;
                titlesChecked++;
                bool found = false;
                if (title.throneRoomRequirements != null)
                {
                    foreach (object req in title.throneRoomRequirements)
                    {
                        var field = req.GetType().GetField("things");
                        if (field == null)
                            continue;
                        if (field.GetValue(req) is IEnumerable things)
                            foreach (object t in things)
                                if (t == armchair)
                                    found = true;
                    }
                }
                Check.True(found, $"{title.defName} throne room requirements do not accept Seat_RoyalArmchair");
            }
            Check.True(titlesChecked > 0, "no Stellarch/High Stellarch RoyalTitleDef found");
        }

        [Test]
        public static void CandelabraShowsGlowRadius()
        {
            if (!Check.Ready("vfeempire.qol", Ids.VFEEmpire))
                return;
            ThingDef candelabra = Check.Def<ThingDef>("VFEE_Candelabra");
            Check.True(candelabra.placeWorkers != null && candelabra.placeWorkers.Contains(typeof(PlaceWorker_GlowRadius)),
                "VFEE_Candelabra lacks PlaceWorker_GlowRadius");
        }
    }
}
