using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BambaMeleeTests
    {
        [Test]
        public static void UltraWeaponsSellableOnly()
        {
            if (!Check.Ready("bambamelee.ultratrade", Ids.BambaMelee))
                return;

            foreach (string defName in new[]
            {
                "Bamba_Melee_Ultra_Gladius", "Bamba_Melee_Ultra_Longsword",
                "Bamba_Melee_Ultra_Axe", "Bamba_Melee_Ultra_Spear",
            })
            {
                ThingDef def = Check.Def<ThingDef>(defName);
                Check.Soft(def.tradeability == Tradeability.Sellable,
                    $"{defName} tradeability is {def.tradeability}, expected Sellable");
            }

            if (ModsConfig.IsActive(Ids.BambaBOR) || ModsConfig.IsActive(Ids.BambaBORLegacy))
            {
                foreach (string defName in new[]
                {
                    "Bamba_Melee_Solar_Gladius", "Bamba_Melee_Solar_Longsword",
                    "Bamba_Melee_Solar_Hammer", "Bamba_Melee_Solar_Spear",
                    "Bamba_Melee_Quasar_Gladius", "Bamba_Melee_Quasar_Longsword",
                    "Bamba_Melee_Quasar_Hammer", "Bamba_Melee_Quasar_Spear",
                })
                {
                    ThingDef def = Check.Def<ThingDef>(defName);
                    Check.Soft(def.tradeability == Tradeability.Sellable,
                        $"{defName} tradeability is {def.tradeability}, expected Sellable");
                }
            }

            Check.SoftResult();
        }
    }
}
