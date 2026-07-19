using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimIOTTests
    {
        [Test]
        public static void ReducedBuildCosts()
        {
            if (!Check.Ready("rimiot.costs", Ids.RimIOT))
                return;
            ThingDef cable = Check.Def<ThingDef>("RimIOT_Cable");
            Check.Eq(Check.CostOf(cable, "Steel"), 3, "RimIOT_Cable costList[Steel]");
            Check.Eq(cable.costList.Count, 1, "RimIOT_Cable costList entry count");
            foreach (string name in new[] { "RimIOT_InputConnector", "RimIOT_Interface" })
            {
                ThingDef def = Check.Def<ThingDef>(name);
                Check.Eq(Check.CostOf(def, "Steel"), 20, $"{name} costList[Steel]");
                Check.Eq(Check.CostOf(def, "ComponentIndustrial"), 2, $"{name} costList[ComponentIndustrial]");
                Check.True(Check.CostOf(def, "ComponentSpacer") == null, $"{name} still costs advanced components");
                Check.Eq(def.costList.Count, 2, $"{name} costList entry count");
            }
        }

        [Test]
        public static void NoPowerConsumption()
        {
            if (!Check.Ready("rimiot.power", Ids.RimIOT))
                return;
            foreach (string name in new[] { "RimIOT_InputConnector", "RimIOT_Interface" })
            {
                ThingDef def = Check.Def<ThingDef>(name);
                if (def.comps == null)
                    continue;
                foreach (CompProperties comp in def.comps)
                {
                    Check.True(!(comp is CompProperties_Power), $"{name} still has CompProperties_Power");
                    Check.True(!(comp is CompProperties_Flickable), $"{name} still has CompProperties_Flickable");
                    Check.True(!comp.GetType().Name.Contains("InterfacePower"), $"{name} still has RimIOT InterfacePower comp");
                }
            }
        }
    }
}
