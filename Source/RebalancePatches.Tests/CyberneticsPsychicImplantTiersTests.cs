using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsPsychicImplantTiersTests
    {
        private const string Key = "cybernetics.tiers";

        /// <summary>One rung of the ladder, as the cost list that defines it.</summary>
        private class Rung
        {
            public string Name;
            public string Owner;          // mod id, for Check.Optional's "expected absence" message
            public int ComponentValue;    // 200 x ComponentSpacer + 800 x gitsMicromachines
            public int Plasteel;
            public int Eltex;
            public string Tier;           // rung label, for failure messages
        }

        private const string RungTransformed = "one currency transformed, no meaningful drawback";
        private const string RungTrade = "one currency transformed, with a real drawback";
        private const string RungPartial = "a third to a half of one currency, no drawback";
        private const string RungSuppression = "suppression";
        private const string RungBelow = "below the rungs - a single use";
        private const string RungCapstone = "psyfocus manufactured in code";

        private static readonly Rung[] Ladder =
        {
            // 3505 - the list PI_NHEC and PI_NHRA already shared, once the decorative metal is gone.
            new Rung { Name = "PI_NHEC", Owner = Ids.PsychicImplants, ComponentValue = 1600, Plasteel = 30, Eltex = 9, Tier = RungTransformed },
            new Rung { Name = "PI_NHRA", Owner = Ids.PsychicImplants, ComponentValue = 1600, Plasteel = 30, Eltex = 9, Tier = RungTransformed },
            new Rung { Name = "PI_HOPS", Owner = Ids.PsychicImplants, ComponentValue = 1600, Plasteel = 30, Eltex = 9, Tier = RungTransformed },

            // 3000 - PI_Resistance's own list, unchanged.
            new Rung { Name = "PI_Resistance", Owner = Ids.PsychicImplants, ComponentValue = 1600, Plasteel = 30, Eltex = 6, Tier = RungTrade },
            new Rung { Name = "PI_NHCS", Owner = Ids.PsychicImplants, ComponentValue = 1600, Plasteel = 30, Eltex = 6, Tier = RungTrade },

            // 2065 - PI_AMC's own list, less its decorative gold.
            new Rung { Name = "PI_AMC", Owner = Ids.PsychicImplants, ComponentValue = 800, Plasteel = 15, Eltex = 6, Tier = RungPartial },
            new Rung { Name = "PI_PRC", Owner = Ids.PsychicImplants, ComponentValue = 800, Plasteel = 15, Eltex = 6, Tier = RungPartial },

            // 1000 - PI_Passivator's own list.
            new Rung { Name = "PI_Passivator", Owner = Ids.PsychicImplants, ComponentValue = 400, Plasteel = 15, Eltex = 2, Tier = RungSuppression },
            new Rung { Name = "PI_TPDD", Owner = Ids.PsychicImplants, ComponentValue = 400, Plasteel = 15, Eltex = 2, Tier = RungSuppression },

            // 540 - one prevented overload, consumed in the saving.
            new Rung { Name = "PI_NHSV", Owner = Ids.PsychicImplants, ComponentValue = 200, Plasteel = 5, Eltex = 1, Tier = RungBelow },

            // The two capstones, each on its own list.
            new Rung { Name = "PI_PFC", Owner = Ids.PsychicImplants, ComponentValue = 2000, Plasteel = 120, Eltex = 20, Tier = RungCapstone },
            new Rung { Name = "PI_AMV", Owner = Ids.PsychicImplants, ComponentValue = 3200, Plasteel = 60, Eltex = 18, Tier = RungCapstone },

            new Rung { Name = "EltexAdornments", Owner = Ids.IntegratedImplants, ComponentValue = 800, Plasteel = -1, Eltex = -1, Tier = "Integrated Implants, unmoved" },
            new Rung { Name = "NeuralHeatsink", Owner = Ids.IntegratedImplants, ComponentValue = 1200, Plasteel = -1, Eltex = -1, Tier = "Integrated Implants, unmoved" },
        };

        private static readonly Dictionary<string, string> MetalsRemoved = new Dictionary<string, string>
        {
            { "PI_NHEC", "Gold" },        // 30 gold, against PI_NHRA's 30 silver, for the same item
            { "PI_NHRA", "Silver" },
            { "PI_NHCS", "Gold" },
            { "PI_AMC", "Gold" },
            { "PI_PRC", "Gold" },
            { "PI_TPDD", "Gold" },
        };

        private static readonly Dictionary<string, string> ArchotechPsychic = new Dictionary<string, string>
        {
            { "PsychicAmplifier", Ids.Royalty },       // Core def, but only reachable with Royalty
            { "PsychicSensitizer", Ids.Royalty },
            { "PsychicReader", Ids.Royalty },
            { "PsychicHarmonizer", Ids.Royalty },
            { "PsychicLevitator", Ids.IntegratedImplants },
            { "PsychicNullifier", Ids.IntegratedImplants },
            { "PsychokeneticShield", Ids.IntegratedImplants },
        };

        private static readonly Dictionary<string, string> OutsideTheLadder = new Dictionary<string, string>
        {
            { "LTS_PrestigeSubdermalArmour", "armour first - gated on RBP_CybDefensiveSystems with the skin glands, which the torso-frames group prices" },
            { "PsychicAgonizer", "Integrated Implants' bioferrite line - bioferrite shaper, Anomaly-tab research, install surgery never moved" },
            { "PsychicBeguiler", "Integrated Implants' bioferrite line" },
            { "PsychicSustainer", "Integrated Implants' bioferrite line" },
            { "LTS_PsychicReaper", "Integrated Implants' bioferrite line" },
            { "LTS_Voidlink", "Integrated Implants' bioferrite line" },
        };

        private static readonly string[] PsychicStats =
        {
            "PsychicSensitivity", "PsychicEntropyMax", "PsychicEntropyRecoveryRate",
            "PsychicEntropyGain", "MeditationFocusGain", "VPE_PsyfocusCostFactor",
            "PsyfocusPerKill", "PsyfocusCostFactor",
        };

        // ------------------------------------------------------------------------------------

        [Test]
        public static void PsychicImplantRungs()
        {
            if (!Check.Ready(Key))
                return;

            var covered = new List<string>();
            foreach (Rung rung in Ladder)
            {
                ThingDef def = Check.Optional<ThingDef>(rung.Name, Key, rung.Owner);
                if (def == null)
                    continue;
                covered.Add($"{rung.Name}={def.BaseMarketValue:0.#}");

                Check.Soft(ComponentValueOf(def) == rung.ComponentValue,
                    $"{rung.Name} ({rung.Tier}) component value is {ComponentsShown(def)}, rung says " +
                    $"{rung.ComponentValue}");

                if (rung.Plasteel >= 0)
                    Check.Soft((Check.CostOf(def, "Plasteel") ?? 0) == rung.Plasteel,
                        $"{rung.Name} ({rung.Tier}) costs {Show(Check.CostOf(def, "Plasteel"))} plasteel, " +
                        $"rung says {rung.Plasteel}");

                if (rung.Eltex >= 0)
                {
                    int? eltex = Check.CostOf(def, "VPE_Eltex");
                    if (eltex == null)
                        Check.Note($"{rung.Name} carries no VPE_Eltex node (Vanilla Psycasts Expanded " +
                            $"{(ModsConfig.IsActive(Ids.VPE) ? "IS" : "is not")} active)");
                    else
                        Check.Soft(eltex.Value == rung.Eltex,
                            $"{rung.Name} ({rung.Tier}) costs {eltex.Value} eltex, rung says {rung.Eltex}");
                }
            }

            Check.Note(covered.Count == 0
                ? "no psychic implant present (neither Psychic Implants nor Integrated Implants active)"
                : "psychic implants checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void RungsAreLevelAndOrdered()
        {
            if (!Check.Ready(Key, Ids.PsychicImplants))
                return;

            // Within a rung: same price, because same job.
            SameRung(RungTransformed, "PI_NHEC", "PI_NHRA", "PI_HOPS");
            SameRung(RungTrade, "PI_Resistance", "PI_NHCS");
            SameRung(RungPartial, "PI_AMC", "PI_PRC");
            SameRung(RungSuppression, "PI_Passivator", "PI_TPDD");

            Ascends("PI_NHSV", "PI_Passivator");
            Ascends("PI_Passivator", "PI_AMC");
            Ascends("PI_AMC", "PI_Resistance");
            Ascends("PI_Resistance", "PI_NHEC");
            Ascends("PI_NHEC", "PI_PFC");

            ThingDef hops = DefDatabase<ThingDef>.GetNamedSilentFail("PI_HOPS");
            ThingDef nhec = DefDatabase<ThingDef>.GetNamedSilentFail("PI_NHEC");
            if (hops != null && nhec != null)
                Check.Soft(hops.BaseMarketValue >= nhec.BaseMarketValue - 25f,
                    $"PI_HOPS grants PsychicSensitivity +0.75 and costs {hops.BaseMarketValue:0.#}, " +
                    $"under PI_NHEC's {nhec.BaseMarketValue:0.#} for one doubled heat stat");

            Check.SoftResult();
        }

        [Test]
        public static void DecorativeMetalIsGone()
        {
            if (!Check.Ready(Key, Ids.PsychicImplants))
                return;

            var covered = new List<string>();
            foreach (KeyValuePair<string, string> pair in MetalsRemoved)
            {
                ThingDef def = Check.Optional<ThingDef>(pair.Key, Key, Ids.PsychicImplants);
                if (def == null)
                    continue;
                covered.Add(pair.Key);
                Check.Soft(Check.CostOf(def, pair.Value) == null,
                    $"{pair.Key} still costs {Show(Check.CostOf(def, pair.Value))} {pair.Value.ToLower()}, " +
                    "which puts it off the rung it was moved onto");
            }

            Check.Note(covered.Count == 0
                ? "no Psychic Implants item present"
                : "metal removals checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void CapstoneWorkIsLevel()
        {
            if (!Check.Ready(Key, Ids.PsychicImplants))
                return;

            ThingDef amv = Check.Optional<ThingDef>("PI_AMV", Key, Ids.PsychicImplants);
            ThingDef pfc = Check.Optional<ThingDef>("PI_PFC", Key, Ids.PsychicImplants);
            if (amv == null || pfc == null)
            {
                Check.Note("PI_AMV or PI_PFC absent; capstone work not checked");
                Check.SoftResult();
                return;
            }

            float amvWork = amv.GetStatValueAbstract(StatDefOf.WorkToMake);
            float pfcWork = pfc.GetStatValueAbstract(StatDefOf.WorkToMake);
            Check.Note($"capstone work: PI_AMV {amvWork:0}, PI_PFC {pfcWork:0}");

            Check.Soft(Math.Abs(amvWork - pfcWork) < 1f,
                $"PI_AMV takes {amvWork:0} work and PI_PFC {pfcWork:0}; both sit on the same research " +
                "node and both manufacture psyfocus in code, so they build in the same time");

            // The capstones must also be the longest builds in the line, or "capstone" means nothing.
            foreach (Rung rung in Ladder)
            {
                if (rung.Tier == RungCapstone || rung.Owner != Ids.PsychicImplants)
                    continue;
                ThingDef other = DefDatabase<ThingDef>.GetNamedSilentFail(rung.Name);
                if (other == null)
                    continue;
                float work = other.GetStatValueAbstract(StatDefOf.WorkToMake);
                Check.Soft(work <= amvWork + 0.5f,
                    $"{rung.Name} takes {work:0} work, more than the capstone PI_AMV's {amvWork:0}");
            }

            Check.SoftResult();
        }

        [Test]
        public static void ArchotechPsychicImplantsAreAcquisitionOnly()
        {
            if (!Check.Ready(Key))
                return;

            var covered = new List<string>();
            foreach (KeyValuePair<string, string> pair in ArchotechPsychic)
            {
                ThingDef def = Check.Optional<ThingDef>(pair.Key, Key, pair.Value);
                if (def == null)
                    continue;
                covered.Add($"{pair.Key}={def.BaseMarketValue:0.#}");

                Check.Soft(def.recipeMaker == null,
                    $"{pair.Key} has gained a recipeMaker, so it is now crafted and its flat market " +
                    $"value of {def.BaseMarketValue:0.#} is no longer what it costs to make");
                Check.Soft(def.costList == null || def.costList.Count == 0,
                    $"{pair.Key} has gained a cost list, so its price is now derived rather than declared");
                Check.Soft(Check.StatBase(def, "MarketValue") != null,
                    $"{pair.Key} no longer declares a MarketValue in statBases; an acquisition-only " +
                    "part has no other way to be priced");

                RecipeDef producer = ProducerOf(def);
                Check.Soft(producer == null,
                    $"{pair.Key} is produced by {(producer == null ? "" : producer.defName)}; psychic " +
                    "power is a parallel progression and no recipe should manufacture one");
            }

            Check.Note(covered.Count == 0
                ? "no archotech psychic implant present (Royalty and Integrated Implants both absent)"
                : "archotech psychic implants checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void EveryCraftablePsychicImplantIsPriced()
        {
            if (!Check.Ready(Key))
                return;

            var onLadder = new HashSet<string>();
            foreach (Rung rung in Ladder)
                onLadder.Add(rung.Name);

            var found = new List<string>();
            var unpriced = new List<string>();
            var seen = new HashSet<string>();

            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (!recipe.IsSurgery)
                    continue;
                HediffDef hediff = recipe.addsHediff;
                if (hediff == null || hediff.addedPartProps != null)
                    continue;
                ThingDef item = hediff.spawnThingOnRemoved;
                if (item == null || item.recipeMaker == null)
                    continue;
                if (!MovesAPsychicCurrency(hediff))
                    continue;
                if (!seen.Add(item.defName))
                    continue;

                found.Add($"{item.defName}={item.BaseMarketValue:0.#}");
                if (onLadder.Contains(item.defName) || OutsideTheLadder.ContainsKey(item.defName))
                    continue;
                unpriced.Add($"{item.defName} ({ModOf(item)}, {item.BaseMarketValue:0.#} silver, via " +
                    $"{recipe.defName} adding {hediff.defName})");
            }

            Check.Note($"swept {found.Count} craftable psychic implant(s): " +
                string.Join(", ", found.ToArray()));

            foreach (string one in unpriced)
                Check.Soft(false,
                    $"{one} is a craftable psychic implant on no rung of CyberneticsPsychicImplantTiers.xml " +
                    "and in no documented exclusion - price it, or record why it is left alone");

            if (ModsConfig.IsActive(Ids.PsychicImplants))
                Check.Soft(found.Count >= 8,
                    $"sweep found only {found.Count} craftable psychic implants with Psychic Implants " +
                    "active; it ships ten with a readable stat surface, so the mechanism this sweep " +
                    "matches on has stopped matching");

            Check.SoftResult();
        }

        // ------------------------------------------------------------------------------------

        private static void SameRung(string tier, params string[] names)
        {
            ThingDef anchor = null;
            foreach (string name in names)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                    continue;
                if (anchor == null)
                {
                    anchor = def;
                    continue;
                }
                Check.Soft(Math.Abs(def.BaseMarketValue - anchor.BaseMarketValue) < 25f,
                    $"{def.defName} costs {def.BaseMarketValue:0.#} against {anchor.defName}'s " +
                    $"{anchor.BaseMarketValue:0.#}; both sit on the \"{tier}\" rung and should be level");
            }
        }

        private static void Ascends(string cheaper, string dearer)
        {
            ThingDef a = DefDatabase<ThingDef>.GetNamedSilentFail(cheaper);
            ThingDef b = DefDatabase<ThingDef>.GetNamedSilentFail(dearer);
            if (a == null || b == null)
                return;
            Check.Soft(a.BaseMarketValue < b.BaseMarketValue,
                $"{cheaper} costs {a.BaseMarketValue:0.#} and {dearer} {b.BaseMarketValue:0.#}; the " +
                "ladder runs the other way");
        }

        private static bool MovesAPsychicCurrency(HediffDef hediff)
        {
            if (hediff.stages == null)
                return false;
            foreach (HediffStage stage in hediff.stages)
            {
                if (stage == null)
                    continue;
                if (HasPsychicStat(stage.statOffsets) || HasPsychicStat(stage.statFactors))
                    return true;
            }
            return false;
        }

        private static bool HasPsychicStat(List<StatModifier> list)
        {
            if (list == null)
                return false;
            foreach (StatModifier modifier in list)
            {
                if (modifier?.stat == null)
                    continue;
                foreach (string name in PsychicStats)
                    if (modifier.stat.defName == name)
                        return true;
            }
            return false;
        }

        private static RecipeDef ProducerOf(ThingDef item)
        {
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.products == null)
                    continue;
                foreach (ThingDefCountClass product in recipe.products)
                    if (product?.thingDef == item)
                        return recipe;
            }
            return null;
        }

        private static int ComponentValueOf(ThingDef def)
        {
            int spacers = Check.CostOf(def, "ComponentSpacer") ?? 0;
            int micromachines = Check.CostOf(def, "gitsMicromachines") ?? 0;
            return spacers * 200 + micromachines * 800;
        }

        private static string ComponentsShown(ThingDef def)
        {
            int spacers = Check.CostOf(def, "ComponentSpacer") ?? 0;
            int micromachines = Check.CostOf(def, "gitsMicromachines") ?? 0;
            return $"{ComponentValueOf(def)} ({spacers} advanced component(s) + " +
                $"{micromachines} micromachine(s))";
        }

        private static string Show(int? value) => value.HasValue ? value.Value.ToString() : "absent";

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "unknown mod";
    }
}
