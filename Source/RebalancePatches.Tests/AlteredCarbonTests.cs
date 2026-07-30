using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RebalancePatches.Mods.AlteredCarbon;
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
            Check.Soft(Check.StatModifierValue(Check.Def<ThingDef>("AC_Apparel_ChrysalisHelmet").equippedStatOffsets, "VacuumResistance") == 0.62f,
                "AC_Apparel_ChrysalisHelmet VacuumResistance is not 0.62");
            // The enviro suit helmet shipped at 0.97, enough on its own to make a pawn vacuum-proof.
            Check.Soft(Check.StatModifierValue(Check.Def<ThingDef>("AC_EnviroSuitHelmet").equippedStatOffsets, "VacuumResistance") == 0.65f,
                "AC_EnviroSuitHelmet VacuumResistance is not 0.65");
            Check.Soft(Check.StatModifierValue(Check.Def<ThingDef>("AC_EnviroSuit").equippedStatOffsets, "VacuumResistance") == 0.32f,
                "AC_EnviroSuit VacuumResistance is not 0.32");
            Check.SoftResult();
        }

        [Test]
        public static void SleeveBrokerTrader()
        {
            if (!Check.Ready("altered.sleevemarket", Ids.AlteredCarbon))
                return;
            TraderKindDef trader = Check.Def<TraderKindDef>("RBP_Orbital_SleeveMarket");
            Check.Soft(trader.orbital, "RBP_Orbital_SleeveMarket is not an orbital trader");

            List<StockGenerator> gens = trader.stockGenerators;
            Check.Note("stock generators: " + string.Join(", ", gens.Select(DescribeGen)));

            Check.Soft(gens.Any(g => g.GetType().Name == "StockGenerator_Sleeves"),
                "sleeve broker has no sleeve-body generator");
            Check.Soft(SingleDefGen(gens, "AC_EmptyNeuralStack") != null,
                "sleeve broker does not sell empty neural stacks");
            Check.Soft(SingleDefGen(gens, "AC_NanoStorageDrive") != null,
                "sleeve broker does not sell a nano storage drive");
            if (ModsConfig.IsActive(Ids.Biotech))
                Check.Soft(SingleDefGen(gens, "Genepack") != null,
                    "Biotech is active but the sleeve broker sells no genepacks");

            int richness = SettingsRegistry.GetEffectiveValue("altered.sleevemarket.richness");
            foreach (StockGenerator g in gens)
            {
                string sd = SingleDefName(g);
                bool scaled = g.GetType().Name == "StockGenerator_Sleeves" || sd == "AC_EmptyNeuralStack" || sd == "Genepack";
                if (scaled)
                    Check.Soft(g.countRange.max == richness,
                        $"{DescribeGen(g)} countRange.max {g.countRange.max} does not track richness slider {richness}");
            }

            Check.SoftResult();
        }

        [Test]
        public static void DigitizedScenarioRelay()
        {
            if (!Check.Ready("altered.scenario", Ids.AlteredCarbon))
                return;
            ThingDef relay = Check.Def<ThingDef>("RBP_MalfunctioningCastingRelay");
            CastingRelayRangeExtension extension = relay.GetModExtension<CastingRelayRangeExtension>();
            Check.Soft(extension != null, "RBP_MalfunctioningCastingRelay has no CastingRelayRangeExtension");
            if (extension != null)
                Check.Soft((int)Check.Field(extension, "tilesPerRelay") == 0,
                    "RBP_MalfunctioningCastingRelay tilesPerRelay is not 0 (needlecast range would exceed 1)");
            Check.Soft(relay.designationCategory == null,
                "RBP_MalfunctioningCastingRelay is buildable; it should exist only for the scenario");
            Check.Soft(relay.comps != null && relay.comps.Any(c => c.GetType().Name == "CompProperties_CastingRelay"),
                "RBP_MalfunctioningCastingRelay is missing Altered Carbon's CompProperties_CastingRelay");
            Check.SoftResult();
        }

        [Test]
        public static void DigitizedMatrixSelfPowered()
        {
            if (!Check.Ready("altered.scenario", Ids.AlteredCarbon))
                return;
            ThingDef matrix = Check.Def<ThingDef>("RBP_BunkerNeuralMatrix");
            CompProperties_Power power = matrix.comps.OfType<CompProperties_Power>().FirstOrDefault();
            Check.Soft(power != null && power.compClass == typeof(CompPowerPlant),
                "RBP_BunkerNeuralMatrix is not self-powered via CompPowerPlant");
            Check.Soft(power != null && (float)Check.Field(power, "basePowerConsumption") < 0f,
                "RBP_BunkerNeuralMatrix does not generate its own power (basePowerConsumption not negative)");
            Check.Soft(power != null && power.transmitsPower,
                "RBP_BunkerNeuralMatrix does not transmit power, so it never forms a net and stays unpowered (the cast would drop)");
            Check.Soft(matrix.comps.Any(c => c.compClass != null && c.compClass.Name == "CompNeuralCache"),
                "RBP_BunkerNeuralMatrix has no neural cache to hold the stack");
            Check.Soft(matrix.thingClass != null && matrix.thingClass.Name == "Building_NeuralMatrix",
                "RBP_BunkerNeuralMatrix is not a Building_NeuralMatrix (AC would not treat it as a matrix)");
            Check.SoftResult();
        }

        [Test]
        public static void DigitizedNeedlecastRuntime()
        {
            if (!Check.Ready("altered.scenario", Ids.AlteredCarbon))
                return;
            Check.Soft(NeedlecastStartup.DiagnosticsUsable(),
                "Altered Carbon needlecasting reflection did not fully resolve (see log)");

            Map map = Find.CurrentMap;
            if (map == null)
            {
                Check.Note("no current map; the runtime cast was not exercised");
                Check.SoftResult();
                return;
            }

            ThingDef matrixDef = Check.Def<ThingDef>("RBP_BunkerNeuralMatrix");
            HediffDef receiver = Check.Def<HediffDef>("AC_RemoteStack");
            Thing matrix = null;
            Pawn pawn = null;
            try
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 25, c => c.Standable(map));
                matrix = ThingMaker.MakeThing(matrixDef);
                matrix.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(matrix, cell, map, WipeMode.Vanish);

                pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(cell, map, 6, c => c.Standable(map)), map);

                bool ok = NeedlecastStartup.TrySetup(pawn, matrix);
                Check.Soft(ok, "TrySetup returned false - see the '[Rebalance Patches] Digitized-start needlecast failed at step ...' log line");
                Check.Soft(pawn.health.hediffSet.HasHediff(receiver),
                    "pawn did not receive the neural receiver (AC_RemoteStack)");
                HediffDef emptySleeve = Check.Def<HediffDef>("AC_EmptySleeve");
                Check.Soft(!pawn.health.hediffSet.HasHediff(emptySleeve),
                    "pawn is still an empty sleeve after the cast - the needlecast did not animate it");

                // AC re-checks the cast whenever the hediff cache is dirtied; force one to prove the cast survives
                // maintenance rather than dropping (which would re-empty the sleeve and leave the colonist downed).
                pawn.health.hediffSet.DirtyCache();
                Check.Soft(!pawn.health.hediffSet.HasHediff(emptySleeve),
                    "cast dropped on a hediff refresh - pawn re-emptied (connect status was not Connectable; check trackedToMatrix/power/range)");
            }
            catch (Exception ex)
            {
                Check.Soft(false, "runtime needlecast threw: " + ex);
            }
            finally
            {
                if (pawn != null && !pawn.Destroyed)
                    pawn.Destroy();
                if (matrix != null && !matrix.Destroyed)
                    matrix.Destroy();
            }
            Check.SoftResult();
        }

        [Test]
        public static void DigitizedScenarioParts()
        {
            if (!Check.Ready("altered.scenario", Ids.AlteredCarbon))
                return;
            ScenPartDef rig = Check.Def<ScenPartDef>("RBP_AC_BunkerRig");
            Check.Soft(rig.scenPartClass == typeof(RebalancePatches.Mods.AlteredCarbon.ScenPart_BunkerRig),
                "RBP_AC_BunkerRig scenPartClass is not ScenPart_BunkerRig");

            ScenarioDef scen = Check.Def<ScenarioDef>("RBP_AlteredCarbonDigitized");
            if (!Check.Soft(scen.scenario != null, "RBP_AlteredCarbonDigitized has no scenario"))
            {
                Check.SoftResult();
                return;
            }
            List<ScenPart> parts = scen.scenario.AllParts.ToList();
            Check.Note("scen parts: " + string.Join(", ", parts.Select(p => p.GetType().Name)));
            Check.Soft(parts.Any(p => p is RebalancePatches.Mods.AlteredCarbon.ScenPart_BunkerRig),
                "digitized scenario has no bunker-rig part");
            Check.Soft(parts.Any(p => p.GetType().Name == "ScenPart_Naked"),
                "digitized scenario is not a naked start");
            Check.SoftResult();
        }

        private static StockGenerator SingleDefGen(List<StockGenerator> gens, string defName)
        {
            foreach (StockGenerator g in gens)
                if (SingleDefName(g) == defName)
                    return g;
            return null;
        }

        private static string SingleDefName(StockGenerator g)
        {
            if (g is StockGenerator_SingleDef)
                return (Check.Field(g, "thingDef") as ThingDef)?.defName;
            return null;
        }

        private static string DescribeGen(StockGenerator g)
        {
            string sd = SingleDefName(g);
            return sd != null ? "SingleDef(" + sd + ")" : g.GetType().Name;
        }
    }
}
