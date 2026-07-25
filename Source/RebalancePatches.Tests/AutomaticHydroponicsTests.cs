using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AutomaticHydroponicsTests
    {
        [Test]
        public static void CostlierAndHigherPower()
        {
            if (!Check.Ready("autohydroponics.costlier", Ids.AutomaticHydroponics))
                return;

            CheckBuilding("AutoHydroponic", 1000, 12, 4, 2000f);
            CheckBuilding("SmallAutoHydroponic", 500, 8, 2, 1000f);
            Check.SoftResult();
        }

        private static void CheckBuilding(string defName, int steel, int industrial, int spacer, float power)
        {
            ThingDef def = Check.Def<ThingDef>(defName);
            Check.Soft(Check.CostOf(def, "Steel") == steel, $"{defName} costList[Steel] is {Check.CostOf(def, "Steel")}, expected {steel}");
            Check.Soft(Check.CostOf(def, "ComponentIndustrial") == industrial, $"{defName} costList[ComponentIndustrial] is {Check.CostOf(def, "ComponentIndustrial")}, expected {industrial}");
            Check.Soft(Check.CostOf(def, "ComponentSpacer") == spacer, $"{defName} costList[ComponentSpacer] is {Check.CostOf(def, "ComponentSpacer")}, expected {spacer}");

            float? actualPower = PowerConsumption(def);
            Check.Soft(actualPower == power, $"{defName} basePowerConsumption is {(actualPower.HasValue ? actualPower.Value.ToString() : "none")}, expected {power}");
        }

        private static float? PowerConsumption(ThingDef def)
        {
            if (def.comps == null)
                return null;
            foreach (CompProperties comp in def.comps)
                if (comp is CompProperties_Power powerComp)
                    return (float)Check.Field(powerComp, "basePowerConsumption");
            return null;
        }
    }
}
