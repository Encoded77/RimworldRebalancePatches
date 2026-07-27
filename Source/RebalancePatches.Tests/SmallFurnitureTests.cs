using HarmonyLib;
using RebalancePatches.Mods.SmallFurniture;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SmallFurnitureTests
    {
        [Test]
        public static void HiTechBenchStandInIsRegistered()
        {
            if (!Check.Ready(SmallFurnitureResearchBench.SettingKey, Ids.SmallFurniture))
                return;
            Check.HarmonyPatched(
                AccessTools.Method(typeof(ResearchProjectDef),
                    nameof(ResearchProjectDef.CanBeResearchedAt),
                    new[] { typeof(Building_ResearchBench), typeof(bool) }),
                "let Small Furniture hi-tech benches stand in for the vanilla bench");
            Check.HarmonyPatched(
                AccessTools.PropertyGetter(typeof(Alert_NeedResearchBench), "HasRequiredResearchBench"),
                "clear the need-research-bench alert at Small Furniture hi-tech benches");
        }

        [Test]
        public static void HiTechBenchesCoverTheVanillaBench()
        {
            if (!Check.Ready(SmallFurnitureResearchBench.SettingKey, Ids.SmallFurniture))
                return;

            ThingDef vanilla = Check.Def<ThingDef>("HiTechResearchBench");
            ThingDef simple = Check.Def<ThingDef>("SimpleResearchBench");
            ThingDef small = Check.Def<ThingDef>("XER_SmallHiTechResearchBench");
            ThingDef medium = Check.Def<ThingDef>("XER_MediumHiTechResearchBench");

            Check.Soft(SmallFurnitureResearchBench.BenchCoversBuilding(small, vanilla),
                "the small hi-tech research bench should stand in for the vanilla Hi-Tech Research Bench");
            Check.Soft(SmallFurnitureResearchBench.BenchCoversBuilding(medium, vanilla),
                "the medium hi-tech research bench should stand in for the vanilla Hi-Tech Research Bench");

            Check.Soft(!SmallFurnitureResearchBench.BenchCoversBuilding(vanilla, vanilla),
                "the vanilla bench is handled by vanilla and must not be treated as a stand-in");
            Check.Soft(!SmallFurnitureResearchBench.BenchCoversBuilding(small, simple),
                "the small hi-tech bench must not stand in for the simple research bench");

            Check.SoftResult();
        }
    }
}
