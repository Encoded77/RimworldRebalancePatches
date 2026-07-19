using System.Collections;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AlteredCarbonTests
    {
        [Test]
        public static void AdvancedShieldsRequireFabrication()
        {
            if (!Check.Ready("altered.shieldsfab", Ids.AlteredCarbon))
                return;
            Check.True(Check.ContainsResearch(Check.Def<ResearchProjectDef>("AC_AdvancedShieldBelt").prerequisites, "Fabrication"),
                "AC_AdvancedShieldBelt lacks Fabrication prerequisite");
        }

        [Test]
        public static void SleeveQualityCancerRates()
        {
            if (!Check.Ready("altered.sleevecancer", Ids.AlteredCarbon))
                return;
            var expected = new Dictionary<string, float>
            {
                { "AC_SleeveQuality_Good", 0.9f },
                { "AC_SleeveQuality_Excellent", 0.8f },
                { "AC_SleeveQuality_Masterwork", 0.7f },
                { "AC_SleeveQuality_Legendary", 0.5f },
            };
            foreach (KeyValuePair<string, float> pair in expected)
            {
                GeneDef gene = Check.Def<GeneDef>(pair.Key);
                Check.Eq(Check.StatModifierValue(gene.statFactors, "CancerRate"), pair.Value, $"{pair.Key} CancerRate factor");
            }
        }

        [Test]
        public static void NeuralEditorIgnoresBodyBoundTraits()
        {
            if (!Check.Ready("altered.traitblacklist", Ids.AlteredCarbon, Ids.HautsTraits, Ids.Royalty))
                return;
            Def options = Check.DefOfType("AlteredCarbon.StackSavingOptions", "AC_StackSavingOptions");
            IEnumerable ignores = (IEnumerable)Check.Field(options, "ignoresTraits");
            foreach (string trait in new[] { "HVT_AwakenedAugur", "HVT_LatentPsychic", "HVT_TTraitThrumbo" })
                Check.True(Check.AnyDefNamed(ignores, trait), $"AC_StackSavingOptions.ignoresTraits lacks {trait}");
        }

        [Test]
        public static void VaeRangedShieldBeltUnobtainable()
        {
            if (!Check.Ready("altered.shieldbelt", Ids.AlteredCarbon, Ids.VAEAccessories))
                return;
            ThingDef belt = Check.Def<ThingDef>("VAEA_Apparel_RangedShieldBelt");
            Check.True(belt.recipeMaker == null, "VAEA_Apparel_RangedShieldBelt still has a recipeMaker");
            Check.Eq(belt.generateAllowChance, 0f, "VAEA_Apparel_RangedShieldBelt.generateAllowChance");
            Check.Eq(belt.tradeability, Tradeability.None, "VAEA_Apparel_RangedShieldBelt.tradeability");
            Check.True(belt.thingSetMakerTags.NullOrEmpty(), "VAEA_Apparel_RangedShieldBelt still has thingSetMakerTags");
        }

        [Test]
        public static void CuirassierBeltUsesVanillaShield()
        {
            if (!Check.Ready("altered.cuirassier", Ids.AlteredCarbon))
                return;
            ThingDef belt = Check.Def<ThingDef>("AC_CuirassierBelt");
            Check.Eq(Check.StatBase(belt, "EnergyShieldRechargeRate"), 0.13f, "AC_CuirassierBelt EnergyShieldRechargeRate");
            Check.Eq(Check.StatBase(belt, "EnergyShieldEnergyMax"), 1.2f, "AC_CuirassierBelt EnergyShieldEnergyMax");
            CompProperties_Shield shield = null;
            foreach (CompProperties comp in belt.comps)
            {
                Check.True(!comp.GetType().Name.Contains("ShieldBubble"), "AC_CuirassierBelt still has VEF ShieldBubble comp");
                if (comp is CompProperties_Shield s)
                    shield = s;
            }
            Check.True(shield != null, "AC_CuirassierBelt has no vanilla CompProperties_Shield");
            Check.True(!shield.blocksRangedWeapons, "AC_CuirassierBelt shield still blocks outgoing ranged shots");
        }

        [Test]
        public static void CastingRelayRangeMatchesSlider()
        {
            if (!Check.Ready("altered.relayrange", Ids.AlteredCarbon))
                return;
            ThingDef relay = Check.Def<ThingDef>("AC_CastingRelay");
            CastingRelayRangeExtension extension = relay.GetModExtension<CastingRelayRangeExtension>();
            Check.True(extension != null, "AC_CastingRelay has no CastingRelayRangeExtension");
            Check.Eq(Check.Field(extension, "tilesPerRelay"), SettingsRegistry.GetEffectiveValue("altered.relayrange"),
                "AC_CastingRelay tilesPerRelay vs slider value");
        }

        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.AlteredCarbon, Ids.Odyssey, Ids.VGravshipC1))
                return;
            Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>("AC_Apparel_ChrysalisHelmet").equippedStatOffsets, "VacuumResistance"),
                0.62f, "AC_Apparel_ChrysalisHelmet VacuumResistance");
        }
    }
}
