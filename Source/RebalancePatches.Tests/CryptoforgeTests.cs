using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CryptoforgeTests
    {
        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.Cryptoforge, Ids.Odyssey, Ids.VGravshipC1))
                return;
            // The buff side: quest-locked endgame armor reaches full protection with its own helmet
            // (0.65 + 0.35), which no vanilla set manages.
            foreach (string armor in new[] { "VQE_CryptoArmor", "VQE_CryptoHeavyArmor" })
                Check.Soft(Check.StatModifierValue(Check.Def<ThingDef>(armor).equippedStatOffsets, "VacuumResistance") == 0.35f,
                    $"{armor} VacuumResistance is not 0.35");
            Check.SoftResult();
        }
    }
}
