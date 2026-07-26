using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VGETests
    {
        [Test]
        public static void SturdierGravjumperEngine()
        {
            if (!Check.Ready("vge.gravjumpercapacity", Ids.VGravshipC1))
                return;
            ThingDef engine = Check.Def<ThingDef>("VGE_GravjumperEngine");
            Check.Eq(Check.StatBase(engine, "SubstructureSupport"), 200f, "VGE_GravjumperEngine SubstructureSupport");
        }
    }
}
