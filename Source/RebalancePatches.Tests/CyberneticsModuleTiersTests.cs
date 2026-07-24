using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsModuleTiersTests
    {
        private const string Key = "cybernetics.tiers";

        private const string MicromachineKey = "cybernetics.micromachines";

        private const string ModuleCategory = "LTS_Modules";
        private const string ModuleCompType = "EBSGFramework.CompProperties_UseEffectHediffModule";

        private const string Spacer = "ComponentSpacer";
        private const string Micromachine = "gitsMicromachines";
        private const string Industrial = "ComponentIndustrial";

        /// <summary>Owner of the crypto claw's bench; the module only loads with it.</summary>
        private const string Cryptoforge = "vanillaquestsexpanded.cryptoforge";

        /// <summary>The tier's frozen work. Every module carries it except one, below.</summary>
        private const float ModuleWork = 1500f;

        private const string CryptoClaw = "LTS_ModuleArm_CryptoClaw";
        private const float CryptoClawWork = 32000f;

        // ------------------------------------------------------------------ the sweep

        private static List<ThingDef> Modules()
        {
            var found = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.thingCategories == null)
                    continue;
                foreach (ThingCategoryDef cat in def.thingCategories)
                {
                    if (cat != null && cat.defName == ModuleCategory)
                    {
                        found.Add(def);
                        break;
                    }
                }
            }
            return found;
        }

        private static bool IsModuleItem(ThingDef def)
        {
            if (def.comps == null)
                return false;
            foreach (CompProperties c in def.comps)
                if (c != null && c.GetType().FullName == ModuleCompType)
                    return true;
            return false;
        }

        private static bool SweptOrNoted(List<ThingDef> modules)
        {
            if (modules.Count > 0)
                return true;
            Check.Note($"no ThingDef sits in the {ModuleCategory} category - Integrated Implants " +
                "loads its module family only when Medical System Expansion 2 is absent, so this " +
                "is legitimate in an MSE2 modlist and a renamed category otherwise");
            return false;
        }

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "unknown mod";

        private static ThingDef Module(string defName, string ownerMod = Ids.IntegratedImplants)
        {
            if (Modules().Count == 0)
                return null;
            return Check.Optional<ThingDef>(defName, Key, ownerMod);
        }

        private static int Cost(ThingDef def, string ingredient)
        {
            int? n = Check.CostOf(def, ingredient);
            return n ?? 0;
        }

        private static string CostString(ThingDef def)
        {
            if (def.costList == null || def.costList.Count == 0)
                return "(no cost list)";
            var parts = new List<string>();
            foreach (ThingDefCountClass c in def.costList)
                parts.Add($"{c.count}x{c.thingDef?.defName ?? "null"}");
            return string.Join(" + ", parts.ToArray());
        }

        private static float AdvancedComponentValue(ThingDef def)
        {
            float total = 0f;
            ThingDef spacer = DefDatabase<ThingDef>.GetNamedSilentFail(Spacer);
            ThingDef micro = DefDatabase<ThingDef>.GetNamedSilentFail(Micromachine);
            if (spacer != null)
                total += Cost(def, Spacer) * spacer.BaseMarketValue;
            if (micro != null)
                total += Cost(def, Micromachine) * micro.BaseMarketValue;
            return total;
        }

        /// <summary>Prices are rounded to the nearest 5 past ~200, so nothing finer than that.</summary>
        private static bool ValueIs(ThingDef def, float expected) =>
            Math.Abs(def.BaseMarketValue - expected) < 0.51f;

        private static void AssertValue(ThingDef def, float expected, string rung)
        {
            Check.Soft(ValueIs(def, expected),
                $"{def.defName} ({def.label}, from {ModOf(def)}) is worth {def.BaseMarketValue:0.##} " +
                $"but the {rung} rung is {expected:0.##}; it costs {CostString(def)} at " +
                $"{def.GetStatValueAbstract(StatDefOf.WorkToMake):0} work");
        }

        // ------------------------------------------------------------------ the constraint

        [Test]
        public static void ModuleWorkIsUnchanged()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            List<ThingDef> modules = Modules();
            if (!SweptOrNoted(modules))
            {
                Check.SoftResult();
                return;
            }

            var seen = new List<string>();
            foreach (ThingDef def in modules)
            {
                float work = def.GetStatValueAbstract(StatDefOf.WorkToMake);
                seen.Add($"{def.defName}={work:0}");

                if (def.defName == CryptoClaw)
                {
                    Check.Soft(Math.Abs(work - CryptoClawWork) < 0.51f,
                        $"{CryptoClaw} is at {work:0} work; Integrated Implants ships it at " +
                        $"{CryptoClawWork:0} and this pass does not touch work");
                    continue;
                }

                Check.Soft(Math.Abs(work - ModuleWork) < 0.51f,
                    $"{def.defName} ({def.label}, from {ModOf(def)}) is at {work:0} work; every " +
                    $"module inherits {ModuleWork:0} from LTS_BaseModuleItem and this pass " +
                    "deliberately changes no module's work - only its cost list");
            }

            Check.Note($"swept {modules.Count} module(s): " + string.Join(", ", seen.ToArray()));

            Check.Soft(modules.Count >= 25,
                $"the {ModuleCategory} sweep found only {modules.Count} item(s); Integrated " +
                "Implants ships around forty, so the sweep has stopped matching what it was " +
                "written to match");

            Check.SoftResult();
        }

        [Test]
        public static void NoModuleDeclaresItsOwnMarketValue()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            List<ThingDef> modules = Modules();
            if (!SweptOrNoted(modules))
            {
                Check.SoftResult();
                return;
            }

            foreach (ThingDef def in modules)
            {
                float? declared = Check.StatBase(def, "MarketValue");
                Check.Soft(declared == null,
                    $"{def.defName} ({def.label}, from {ModOf(def)}) declares MarketValue " +
                    $"{declared:0.##} in statBases, which wins over its cost list of " +
                    $"{CostString(def)}; module prices are derived, never declared");
            }

            Check.SoftResult();
        }

        // ------------------------------------------------------------------ the rungs

        [Test]
        public static void BaseModuleRung()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            List<ThingDef> modules = Modules();
            if (!SweptOrNoted(modules))
            {
                Check.SoftResult();
                return;
            }

            ThingDef spacer = Check.Def<ThingDef>(Spacer);
            float rungValue = 4f * spacer.BaseMarketValue;

            var onRung = new List<string>();
            foreach (ThingDef def in modules)
            {
                if (!IsModuleItem(def))
                    continue;
                if (Cost(def, "Plasteel") != 10)
                    continue;
                if (Math.Abs(AdvancedComponentValue(def) - rungValue) > 0.51f)
                    continue;
                onRung.Add(def.defName);
                AssertValue(def, 895f, "large-slot module");
            }

            Check.Note($"{onRung.Count} module(s) on the base rung: " + string.Join(", ", onRung.ToArray()));
            Check.Soft(onRung.Count >= 10,
                $"only {onRung.Count} module(s) sit on the base rung of {rungValue:0} silver of " +
                "advanced components plus 10 plasteel; fifteen inherit it from LTS_BaseModuleItem, " +
                "so either the base was re-costed or the sweep stopped matching");

            Check.SoftResult();
        }

        /// <summary>The small-slot, passive and cosmetic rungs, all pinned rather than moved.</summary>
        [Test]
        public static void SmallSlotAndBottomRungs()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            ThingDef spacer = Check.Def<ThingDef>(Spacer);

            foreach (string name in new[]
            {
                "LTS_ModuleEye_Targeting", "LTS_ModuleEye_NightVision", "LTS_ModuleEye_ProtectiveLens",
            })
            {
                ThingDef def = Module(name);
                if (def == null)
                    continue;
                Check.Soft(Math.Abs(AdvancedComponentValue(def) - 4f * spacer.BaseMarketValue) < 0.51f &&
                           Cost(def, "Plasteel") == 5,
                    $"{name} costs {CostString(def)}; the small-slot rung is 4 advanced components " +
                    "and 5 plasteel");
                AssertValue(def, 850f, "small-slot module");
            }

            // The telescopic sight is on the same rung but needs Vanilla Expanded Framework.
            ThingDef telescopic = DefDatabase<ThingDef>.GetNamedSilentFail("LTS_ModuleEye_Telescopic");
            if (telescopic != null)
                AssertValue(telescopic, 850f, "small-slot module");
            else
                Check.Note("LTS_ModuleEye_Telescopic absent (needs Vanilla Expanded Framework)");

            ThingDef colour = Module("LTS_ModuleEye_EyeColour");
            if (colour != null)
            {
                Check.Soft(Cost(colour, "Plasteel") == 0 &&
                           Math.Abs(AdvancedComponentValue(colour) - spacer.BaseMarketValue) < 0.51f,
                    $"LTS_ModuleEye_EyeColour costs {CostString(colour)}; the cosmetic rung is one " +
                    "advanced component and nothing else");
                AssertValue(colour, 205f, "cosmetic");
            }

            ThingDef grip = Module("LTS_ModuleArm_DeathGrip");
            if (grip != null)
            {
                Check.Soft(Cost(grip, "Plasteel") == 5 &&
                           Math.Abs(AdvancedComponentValue(grip) - spacer.BaseMarketValue) < 0.51f,
                    $"LTS_ModuleArm_DeathGrip costs {CostString(grip)}; the passive rung is one " +
                    "advanced component and 5 plasteel");
                AssertValue(grip, 250f, "passive");
            }

            Check.SoftResult();
        }

        // ------------------------------------------------------------------ what moved

        [Test]
        public static void IndustrialMeleeRungMatchesRoyaltysImplants()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG, Ids.Royalty))
                return;

            // Royalty's own shipped numbers, pinned by this pass rather than changed by it.
            ThingDef elbow = Check.Def<ThingDef>("ElbowBlade");
            int royaltySteel = Cost(elbow, "Steel");
            int royaltyComponents = Cost(elbow, Industrial);
            Check.Note($"Royalty weapon implant rung: {CostString(elbow)} at " +
                $"{elbow.GetStatValueAbstract(StatDefOf.WorkToMake):0} work = {elbow.BaseMarketValue:0.##}");

            foreach (string name in new[] { "ElbowBlade", "HandTalon", "KneeSpike", "VenomFangs", "VenomTalon" })
            {
                ThingDef def = Check.Def<ThingDef>(name);
                Check.Soft(Cost(def, "Steel") == 40 && Cost(def, Industrial) == 7,
                    $"{name} (Royalty's own item) costs {CostString(def)}; this pass pins all five " +
                    "of Royalty's weapon implants at 40 steel and 7 industrial components because " +
                    "the blade modules are priced off them");
                AssertValue(def, 355f, "Royalty weapon implant");
            }

            foreach (string name in new[]
            {
                "LTS_ModuleArm_ArmBlade", "LTS_ModuleLeg_FootBlade", "LTS_ModuleLeg_VenomFootBlade",
            })
            {
                ThingDef def = Module(name);
                if (def == null)
                    continue;
                Check.Soft(Cost(def, "Steel") == royaltySteel && Cost(def, Industrial) == royaltyComponents,
                    $"{name} costs {CostString(def)} but grants a tool Royalty sells for " +
                    $"{royaltySteel} steel and {royaltyComponents} industrial components " +
                    $"({CostString(elbow)}); the module must carry the same ingredient list");
                Check.Soft(AdvancedComponentValue(def) < 0.51f,
                    $"{name} carries advanced components ({CostString(def)}); the industrial melee " +
                    "rung is steel and industrial components only");
                AssertValue(def, 305f, "industrial melee module");
            }

            Check.SoftResult();
        }

        [Test]
        public static void IndustrialRangedRungMatchesTheShoulderTurret()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            ThingDef turret = Module("LTS_ShoulderTurret");
            if (turret == null)
            {
                Check.SoftResult();
                return;
            }

            int turretSteel = Cost(turret, "Steel");
            int turretComponents = Cost(turret, Industrial);
            Check.Note($"shoulder turret rung: {CostString(turret)} at " +
                $"{turret.GetStatValueAbstract(StatDefOf.WorkToMake):0} work = {turret.BaseMarketValue:0.##}");

            foreach (string name in new[] { "LTS_ModuleArm_SMG", "LTS_ModuleArm_Shotgun" })
            {
                ThingDef def = Module(name);
                if (def == null)
                    continue;
                Check.Soft(Cost(def, "Steel") == turretSteel && Cost(def, Industrial) == turretComponents,
                    $"{name} costs {CostString(def)}; the shoulder turret - the same mod's own " +
                    $"industrial built-in ranged weapon - costs {CostString(turret)} and is the rung");
                AssertValue(def, 375f, "industrial built-in ranged module");
            }

            ThingDef inc = Module("LTS_ModuleArm_Incinerator", Ids.Anomaly);
            if (inc != null)
            {
                Check.Soft(Cost(inc, "Steel") == turretSteel && Cost(inc, Industrial) == turretComponents,
                    $"LTS_ModuleArm_Incinerator costs {CostString(inc)}; it takes the shoulder " +
                    $"turret's list ({CostString(turret)}) plus its own bioferrite");
                Check.Soft(Cost(inc, "Bioferrite") == 30,
                    $"LTS_ModuleArm_Incinerator costs {CostString(inc)}; its 30 bioferrite is kept");
                AssertValue(inc, 400f, "industrial built-in ranged module plus bioferrite");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CryptoClawIsPricedFromItsCostList()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            ThingDef claw = Module(CryptoClaw, Cryptoforge);
            if (claw == null)
            {
                Check.SoftResult();
                return;
            }

            Check.Note($"{CryptoClaw}: {CostString(claw)} at " +
                $"{claw.GetStatValueAbstract(StatDefOf.WorkToMake):0} work = {claw.BaseMarketValue:0.##}");
            Check.Soft(Check.StatBase(claw, "MarketValue") == null,
                $"{CryptoClaw} still declares a MarketValue in statBases, which overrides its cost list");
            AssertValue(claw, 1050f, "crypto claw, derived from its own ingredients");

            Check.SoftResult();
        }

        // ------------------------------------------------------------------ conversions

        [Test]
        public static void ConversionModulesAreNeverCheaperThanWhatTheyConsume()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG))
                return;

            List<ThingDef> modules = Modules();
            if (!SweptOrNoted(modules))
            {
                Check.SoftResult();
                return;
            }

            var conversions = new List<string>();
            foreach (ThingDef def in modules)
            {
                if (def.costList == null)
                    continue;
                foreach (ThingDefCountClass c in def.costList)
                {
                    if (c.thingDef == null || !c.thingDef.isTechHediff)
                        continue;
                    conversions.Add($"{def.defName}={def.BaseMarketValue:0.##} from " +
                        $"{c.thingDef.defName}={c.thingDef.BaseMarketValue:0.##}");
                    Check.Soft(def.BaseMarketValue >= c.thingDef.BaseMarketValue - 0.51f,
                        $"{def.defName} is worth {def.BaseMarketValue:0.##} but is made by " +
                        $"consuming {c.thingDef.defName}, which is worth " +
                        $"{c.thingDef.BaseMarketValue:0.##}; a conversion module can never be " +
                        "cheaper than the part it destroys");
                }
            }

            Check.Note(conversions.Count == 0
                ? "no conversion module present"
                : $"{conversions.Count} conversion(s): " + string.Join(", ", conversions.ToArray()));
            Check.Soft(conversions.Count >= 5,
                $"only {conversions.Count} conversion module(s) found; Integrated Implants ships " +
                "eleven, so the tech-hediff test has stopped identifying them");

            Check.SoftResult();
        }

        [Test]
        public static void NuclearStomachModulePaysForItsMissingDrawback()
        {
            if (!Check.Ready(Key, Ids.IntegratedImplants, Ids.EBSG, Ids.Royalty))
                return;

            ThingDef module = Module("LTS_ModuleStomach_Nuclear", Ids.Royalty);
            ThingDef organ = Check.Optional<ThingDef>("NuclearStomach", Key, Ids.Royalty);
            if (module == null || organ == null)
            {
                Check.SoftResult();
                return;
            }

            ThingDef spacer = Check.Def<ThingDef>(Spacer);
            Check.Note($"LTS_ModuleStomach_Nuclear: {CostString(module)} = {module.BaseMarketValue:0.##}; " +
                $"NuclearStomach = {organ.BaseMarketValue:0.##}");

            Check.Soft(Cost(module, "NuclearStomach") == 1,
                $"LTS_ModuleStomach_Nuclear costs {CostString(module)}; it must still consume the organ");
            Check.Soft(Math.Abs(AdvancedComponentValue(module) - 2f * spacer.BaseMarketValue) < 0.51f,
                $"LTS_ModuleStomach_Nuclear costs {CostString(module)}; the surcharge is two " +
                $"advanced components ({2f * spacer.BaseMarketValue:0} silver) on top of the organ");

            foreach (string sibling in new[] { "LTS_ModuleStomach_Reprocessor", "LTS_ModuleStomach_Sterilizing" })
            {
                ThingDef other = Module(sibling, Ids.Royalty);
                if (other == null)
                    continue;
                Check.Soft(Math.Abs(module.BaseMarketValue - other.BaseMarketValue) < 0.51f,
                    $"LTS_ModuleStomach_Nuclear is worth {module.BaseMarketValue:0.##} and " +
                    $"{sibling} {other.BaseMarketValue:0.##}; the three stomach modules sit on one " +
                    "line because none of them carries the organ's carcinoma risk");
            }

            Check.SoftResult();
        }

        // ------------------------------------------------------------------ micromachines

        [Test]
        public static void NoModuleCarriesAWholeGroupOfAdvancedComponents()
        {
            if (!Check.Ready(MicromachineKey, Ids.GiTS, Ids.IntegratedImplants, Ids.EBSG))
                return;

            List<ThingDef> modules = Modules();
            if (!SweptOrNoted(modules))
            {
                Check.SoftResult();
                return;
            }

            foreach (ThingDef def in modules)
            {
                int spacers = Cost(def, Spacer);
                Check.Soft(spacers < 4,
                    $"{def.defName} ({def.label}, from {ModOf(def)}) still costs {spacers} advanced " +
                    $"components and {Cost(def, Micromachine)} micromachine(s) - {CostString(def)}; " +
                    "four components are one micromachine, so no module may carry four or more");
            }

            Check.SoftResult();
        }

        [Test]
        public static void NonModuleItemsInTheseGroupsWereSwapped()
        {
            if (!Check.Ready(MicromachineKey, Ids.GiTS))
                return;

            // defName, components before the swap, owning mod.
            string[][] swapped =
            {
                new[] { "BS_BionicLocomotiveSpine", "8", null },
                new[] { "LTS_SubdermalArmour", "6", Ids.IntegratedImplants },
                new[] { "LTS_StealthSystem", "6", Ids.IntegratedImplants },
                new[] { "EPOE_AdvancedElbowBlade", "6", Ids.EPOEForkedRoyalty },
                new[] { "EPOE_AdvancedKneeSpike", "6", Ids.EPOEForkedRoyalty },
                new[] { "BiochameleonDevice", "5", Ids.IntegratedImplants },
                new[] { "LTS_ShoulderMortar", "5", Ids.IntegratedImplants },
                new[] { "LTS_ShoulderRocketPod", "5", Ids.IntegratedImplants },
                new[] { "BiotunerImplant", "4", Ids.IntegratedImplants },
                new[] { "CranialInsulation", "4", Ids.IntegratedImplants },
                new[] { "HackingPortImplant", "4", Ids.IntegratedImplants },
                new[] { "LTS_CellularAdapter", "4", Ids.IntegratedImplants },
                new[] { "LTS_ShoulderBeamTurret", "4", Ids.IntegratedImplants },
                new[] { "LTS_ShoulderChargeTurret", "4", Ids.IntegratedImplants },
                new[] { "HemoFangs", "4", Ids.IntegratedImplants },
                new[] { "TacticalCorneaImplant", "4", Ids.EPOEForked },
                new[] { "AC_RogianArmBlade", "4", Ids.AlteredCarbon },
                new[] { "EPOE_AdvancedHandTalon", "4", Ids.EPOEForkedRoyalty },
                new[] { "EPOE_ScytherElbowBlade", "4", Ids.EPOEForkedRoyalty },
                new[] { "EPOE_ScytherKneeSpike", "4", Ids.EPOEForkedRoyalty },
            };

            ThingDef micro = Check.Def<ThingDef>(Micromachine);
            ThingDef spacer = Check.Def<ThingDef>(Spacer);
            var covered = new List<string>();

            foreach (string[] row in swapped)
            {
                ThingDef def = row[2] == null
                    ? DefDatabase<ThingDef>.GetNamedSilentFail(row[0])
                    : Check.Optional<ThingDef>(row[0], MicromachineKey, row[2]);
                if (def == null)
                    continue;

                int before = int.Parse(row[1]);
                int expectedMicro = before / 4;
                int expectedSpacer = before % 4;
                covered.Add($"{def.defName}:{CostString(def)}");

                Check.Soft(Cost(def, Micromachine) == expectedMicro && Cost(def, Spacer) == expectedSpacer,
                    $"{def.defName} (from {ModOf(def)}) costs {CostString(def)}; it shipped at " +
                    $"{before} advanced components, so the rule makes it {expectedMicro} " +
                    $"micromachine(s) and {expectedSpacer} advanced component(s)");

                Check.Soft(Math.Abs(AdvancedComponentValue(def) - before * spacer.BaseMarketValue) < 0.51f,
                    $"{def.defName} carries {AdvancedComponentValue(def):0} silver of components " +
                    $"after the swap and {before * spacer.BaseMarketValue:0} before; the swap is " +
                    $"only value-neutral while a micromachine ({micro.BaseMarketValue:0}) is worth " +
                    $"exactly four advanced components ({4f * spacer.BaseMarketValue:0})");
            }

            Check.Note(covered.Count == 0
                ? "none of the named items is present"
                : $"{covered.Count} swapped: " + string.Join(", ", covered.ToArray()));
            Check.SoftResult();
        }

        [Test]
        public static void ItemsBelowTheThresholdAreUntouched()
        {
            if (!Check.Ready(MicromachineKey, Ids.GiTS))
                return;

            string[][] below =
            {
                new[] { "LTS_ShieldImplant", "3", Ids.IntegratedImplants },
                new[] { "LTS_EmergencyShield", "3", Ids.IntegratedImplants },
                new[] { "LTS_EmergencyVacseal", "3", Ids.IntegratedImplants },
                new[] { "LTS_FireSupressionSystem", "3", Ids.IntegratedImplants },
                new[] { "StrengthEnhancer", "3", Ids.IntegratedImplants },
                new[] { "WiredReflex", "3", Ids.IntegratedImplants },
                new[] { "RespirationRib", "3", Ids.EPOEForked },
                new[] { "EPOE_ScytherHandTalon", "3", Ids.EPOEForkedRoyalty },
                new[] { "LTS_ModuleArm_CryptoClaw", "3", Cryptoforge },
                new[] { "LTS_Gravlifter", "2", Ids.IntegratedImplants },
                new[] { "LTS_ModuleLeg_GravKick", "2", Ids.IntegratedImplants },
                new[] { "LTS_ModuleStomach_Nuclear", SettingsRegistry.GetEffective(Key) ? "2" : "0", Ids.Royalty },
                new[] { "LTS_ModuleEye_EyeColour", "1", Ids.IntegratedImplants },
                new[] { "LTS_ModuleArm_DeathGrip", "1", Ids.IntegratedImplants },
                new[] { "PainkillerRib", "1", Ids.EPOEForked },
                new[] { "CoagulatorRib", "1", Ids.EPOEForked },
                new[] { "MedicalRib", "1", Ids.EPOEForked },
                new[] { "WakeUpRib", "1", Ids.EPOEForked },
                new[] { "DruggedRib", "1", Ids.EPOEForked },
                new[] { "AdrenalineRib", "1", Ids.EPOEForked },
                new[] { "CoolerRib", "1", Ids.EPOEForked },
                new[] { "HeaterRib", "1", Ids.EPOEForked },
            };

            foreach (string[] row in below)
            {
                ThingDef def = Check.Optional<ThingDef>(row[0], MicromachineKey, row[2]);
                if (def == null)
                    continue;
                Check.Soft(Cost(def, Micromachine) == 0 && Cost(def, Spacer) == int.Parse(row[1]),
                    $"{def.defName} (from {ModOf(def)}) costs {CostString(def)}; it carries " +
                    $"{row[1]} advanced component(s), which is below the group of four, so the " +
                    "micromachine rule must leave it alone");
            }

            Check.SoftResult();
        }
    }
}
