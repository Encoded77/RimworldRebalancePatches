using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class EventideTests
    {
        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.Eventide, Ids.Odyssey, Ids.VGravshipC1))
                return;
            // Same warcasket helmet value Vanilla Gravship Expanded gives the lines it covers itself.
            Check.Eq(VFEPiratesTests.WarcasketVacuum("dvd_WarcasketHelmet_Eventide"), 0.65f,
                "dvd_WarcasketHelmet_Eventide VacuumResistance");
        }
    }
}
