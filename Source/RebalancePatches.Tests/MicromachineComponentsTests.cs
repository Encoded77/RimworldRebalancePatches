using System;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class MicromachineComponentsTests
    {
        private const string Key = "cybernetics.micromachines";

        private const string Spacer = "ComponentSpacer";
        private const string Micromachine = "gitsMicromachines";

        /// <summary>The three craft gates that define the advanced tier.</summary>
        private static readonly string[] AdvancedGates =
        {
            "RBP_CybAdvancedLimbs", "RBP_CybAdvancedOrgans", "RBP_CybAdvancedSenses",
        };

        private static readonly string[][] BelowThreshold =
        {
            new[] { "EPIA_ProtectiveExoskeleton", "3", Ids.EPOEForked },
            new[] { "EPIA_AuxiliaryAI_Combat", "3", Ids.EPOEForked },
            new[] { "EPOE_MAAI_Chip", "1", Ids.EPOEForked },
            new[] { "EPOE_DetoxifierEnhancer", "3", Ids.EPOEForkedRoyalty },
        };

        private class Advanced
        {
            public ThingDef Item;
            public string Gate;
            public int Spacers;
            public int Micromachines;

            /// <summary>What the cost list held before the swap, reconstructed at 4:1.</summary>
            public int OriginalComponents => Spacers + 4 * Micromachines;
        }

        [Test]
        public static void MicromachineIsWorthFourComponents()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            ThingDef micro = Check.Def<ThingDef>(Micromachine);
            ThingDef spacer = Check.Def<ThingDef>(Spacer);

            Check.Note($"{Micromachine} = {micro.BaseMarketValue:0.##}, {Spacer} = {spacer.BaseMarketValue:0.##}");
            Check.Soft(Math.Abs(micro.BaseMarketValue - 4f * spacer.BaseMarketValue) < 0.01f,
                $"a micromachine is worth {micro.BaseMarketValue:0.##} and an advanced component " +
                $"{spacer.BaseMarketValue:0.##}; the swap replaces four components with one " +
                "micromachine and is only price-neutral while that ratio is exactly 4:1");

            Check.SoftResult();
        }

        [Test]
        public static void AdvancedTierCarriesNoWholeGroupOfComponents()
        {
            if (!Check.Ready(Key, Ids.GiTS, Ids.EPOEForked))
                return;

            List<Advanced> swept = Sweep();
            Check.Note(Describe(swept));

            foreach (Advanced item in swept)
            {
                Check.Soft(item.Spacers < 4,
                    $"{item.Item.defName} (gated on {item.Gate}, from {ModOf(item.Item)}) still costs " +
                    $"{item.Spacers} advanced components and {item.Micromachines} micromachine(s); " +
                    "four components are one micromachine, so no advanced-tier item may carry four " +
                    "or more");
            }

            Check.Soft(swept.Count >= 15,
                $"the gate sweep found only {swept.Count} advanced-tier craftables; EPOE-Forked and " +
                "this mod together supply far more, so the sweep has stopped matching what it was " +
                "written to match and this test asserted almost nothing");

            Check.SoftResult();
        }

        [Test]
        public static void SwappedItemsKeptTheirValue()
        {
            if (!Check.Ready(Key, Ids.GiTS, Ids.EPOEForked))
                return;

            ThingDef micro = Check.Def<ThingDef>(Micromachine);
            ThingDef spacer = Check.Def<ThingDef>(Spacer);
            float mmValue = micro.BaseMarketValue;
            float csValue = spacer.BaseMarketValue;

            var swapped = new List<string>();
            foreach (Advanced item in Sweep())
            {
                if (item.Micromachines == 0)
                    continue;

                float after = csValue * item.Spacers + mmValue * item.Micromachines;
                float before = csValue * item.OriginalComponents;
                swapped.Add($"{item.Item.defName}={item.OriginalComponents}->{item.Spacers}+{item.Micromachines}mm");

                Check.Soft(Math.Abs(after - before) < 0.01f,
                    $"{item.Item.defName} was built from {item.OriginalComponents} advanced components " +
                    $"worth {before:0.##} and is now built from {item.Spacers} components plus " +
                    $"{item.Micromachines} micromachine(s) worth {after:0.##}");

                Check.Soft(item.Micromachines == item.OriginalComponents / 4
                        && item.Spacers == item.OriginalComponents % 4,
                    $"{item.Item.defName} holds {item.Micromachines} micromachine(s) and " +
                    $"{item.Spacers} components; {item.OriginalComponents} components should split " +
                    $"into {item.OriginalComponents / 4} and {item.OriginalComponents % 4}");
            }

            Check.Note(swapped.Count == 0
                ? "no advanced-tier item carries a micromachine"
                : "swapped items: " + string.Join(", ", swapped.ToArray()));

            Check.Soft(swapped.Count >= 10,
                $"only {swapped.Count} advanced-tier items carry a micromachine; the tier holds well " +
                "over ten items at four components or more, so the patch is not reaching them");

            Check.SoftResult();
        }

        [Test]
        public static void ItemsBelowTheThresholdAreUntouched()
        {
            if (!Check.Ready(Key, Ids.GiTS, Ids.EPOEForked))
                return;

            var covered = new List<string>();
            foreach (string[] entry in BelowThreshold)
            {
                string name = entry[0];
                int expected = int.Parse(entry[1]);

                ThingDef def = Check.Optional<ThingDef>(name, Key, entry[2]);
                if (def == null)
                    continue;

                int? spacers = Check.CostOf(def, Spacer);
                int? micros = Check.CostOf(def, Micromachine);
                covered.Add($"{name}={Show(spacers)}c/{Show(micros)}mm");

                Check.Soft(micros == null,
                    $"{name} carries {Show(micros)} micromachine(s); it costs {Show(spacers)} advanced " +
                    "components, below the group of four the rule trades in, so it should have been " +
                    "left alone");
                Check.Soft(spacers == expected,
                    $"{name} costs {Show(spacers)} advanced components, not the {expected} it had " +
                    "before this pass; either an operation reached it or its source mod re-costed it, " +
                    "and if it is now at four or more it needs to join the swap");
            }

            Check.Note(covered.Count == 0
                ? "no below-threshold item present (EPOE-Forked and EPOE-Forked: Royalty both absent)"
                : "below-threshold items checked: " + string.Join(", ", covered.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void UpgradeRecipesAreUntouched()
        {
            if (!Check.Ready(Key, Ids.GiTS, Ids.EPOEForked))
                return;

            var seen = new List<string>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (!GatedOnAdvancedTier(recipe.researchPrerequisite, recipe.researchPrerequisites))
                    continue;
                ThingDef product = recipe.ProducedThingDef;
                if (product == null || recipe.defName == "Make_" + product.defName)
                    continue;

                int spacers = ConsumedCount(recipe, Spacer);
                int micros = ConsumedCount(recipe, Micromachine);
                seen.Add($"{recipe.defName}={spacers}c/{micros}mm");

                Check.Soft(micros == 0,
                    $"{recipe.defName} consumes {micros} micromachine(s); the upgrade recipes carry " +
                    "fewer than four advanced components and are not part of the swap");
                Check.Soft(spacers < 4,
                    $"{recipe.defName} consumes {spacers} advanced components; that is a whole group " +
                    "of four the swap has not accounted for, so the upgrade path is now cheaper in " +
                    "real materials than building the part outright");
            }

            Check.Note(seen.Count == 0
                ? "no hand-written recipe is gated on an advanced-tier node"
                : "non-generated advanced-tier recipes: " + string.Join(", ", seen.ToArray()));

            // EPOE-Forked ships nine CreateAdvanced* upgrade recipes.
            Check.Soft(seen.Count >= 5,
                $"only {seen.Count} hand-written recipes are gated on the advanced nodes; EPOE-Forked " +
                "supplies nine upgrade recipes, so this test saw almost nothing");

            Check.SoftResult();
        }

        private static List<Advanced> Sweep()
        {
            var found = new List<Advanced>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.recipeMaker == null)
                    continue;
                string gate = MatchedGate(def.recipeMaker.researchPrerequisite,
                    def.recipeMaker.researchPrerequisites);
                if (gate == null)
                    continue;

                found.Add(new Advanced
                {
                    Item = def,
                    Gate = gate,
                    Spacers = Check.CostOf(def, Spacer) ?? 0,
                    Micromachines = Check.CostOf(def, Micromachine) ?? 0,
                });
            }
            return found;
        }

        private static bool GatedOnAdvancedTier(ResearchProjectDef single, List<ResearchProjectDef> many) =>
            MatchedGate(single, many) != null;

        private static string MatchedGate(ResearchProjectDef single, List<ResearchProjectDef> many)
        {
            foreach (string gate in AdvancedGates)
            {
                if (single != null && single.defName == gate)
                    return gate;
                if (many == null)
                    continue;
                foreach (ResearchProjectDef project in many)
                    if (project != null && project.defName == gate)
                        return gate;
            }
            return null;
        }

        private static int ConsumedCount(RecipeDef recipe, string thingDefName)
        {
            if (recipe.ingredients == null)
                return 0;
            int total = 0;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                if (ingredient?.filter == null)
                    continue;
                foreach (ThingDef allowed in ingredient.filter.AllowedThingDefs)
                    if (allowed != null && allowed.defName == thingDefName)
                    {
                        total += (int)ingredient.GetBaseCount();
                        break;
                    }
            }
            return total;
        }

        private static string Describe(List<Advanced> swept)
        {
            var parts = new List<string>();
            foreach (Advanced item in swept)
                parts.Add($"{item.Item.defName}[{item.Gate}]={item.Spacers}c/{item.Micromachines}mm");
            return $"swept {swept.Count} advanced-tier craftable(s): " + string.Join(", ", parts.ToArray());
        }

        private static readonly string[][] BionicOrgans =
        {
            new[] { "DetoxifierLung", "1200", Ids.Biotech },
            new[] { "DetoxifierKidney", "1200", Ids.Biotech },
            new[] { "USH_IronLung", "1200", Ids.UshankaBioWarfare },
            new[] { "DetoxifierStomach", "1200", Ids.Royalty },
            new[] { "ReprocessorStomach", "1200", Ids.Royalty },
            new[] { "NuclearStomach", "800", Ids.Royalty },
        };

        [Test]
        public static void BionicOrgansJoinedTheSwap()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            var covered = new List<string>();
            foreach (string[] entry in BionicOrgans)
            {
                string name = entry[0];
                int expectedValue = int.Parse(entry[1]);

                ThingDef def = Check.Optional<ThingDef>(name, Key, entry[2]);
                if (def == null)
                    continue;

                int spacers = Check.CostOf(def, Spacer) ?? 0;
                int micros = Check.CostOf(def, Micromachine) ?? 0;
                covered.Add($"{name}={spacers}c/{micros}mm");

                Check.Soft(spacers * 200 + micros * 800 == expectedValue,
                    $"{name} is worth {spacers * 200 + micros * 800} in components " +
                    $"({spacers}c + {micros}mm) instead of {expectedValue}; the swap must not " +
                    "move a price");
                Check.Soft(micros >= 1,
                    $"{name} carries no micromachine - it costs {spacers} advanced components, so " +
                    "either the patch did not reach it or its source mod re-costed it below four");
                Check.Soft(spacers < 4,
                    $"{name} still carries {spacers} advanced components, which is the whole group " +
                    "the rule trades in; the component count is running backwards against the " +
                    "advanced tier again");
            }

            Check.Note(covered.Count == 0
                ? "no bionic-tier organ present (Royalty, Biotech and Ushankas Biological Warfare all absent)"
                : "bionic-tier organs checked: " + string.Join(", ", covered.ToArray()));

            Check.Soft(covered.Count >= 3,
                $"only {covered.Count} of the six bionic-tier organs were found; this test is " +
                "asserting almost nothing");

            Check.SoftResult();
        }

        private static readonly string[][] UniversalRule =
        {
            new[] { "RBP_LivingFrame", "4000" },
            new[] { "RBP_GeneDivergenceImplant", "4000" },
            new[] { "PI_AMV", "3200" },
            new[] { "RBP_AndroidConversionKit", "2400" },
            new[] { "PI_PFC", "2000" },
            new[] { "RBP_ThoracicFrameAdvanced", "1600" },
            new[] { "PI_HOPS", "1600" },
            new[] { "PI_NHCS", "1600" },
            new[] { "PI_NHEC", "1600" },
            new[] { "PI_NHRA", "1600" },
            new[] { "PI_Resistance", "1600" },
            new[] { "RBP_ThoracicFrameBionic", "1200" },
            new[] { "NeuralHeatsink", "1200" },
            new[] { "LTS_PrestigeSubdermalArmour", "1200" },
            new[] { "LTS_MemoryDatabank", "1000" },
            new[] { "RBP_NeuroformSerum", "800" },
            new[] { "PI_AMC", "800" },
            new[] { "PI_PRC", "800" },
            new[] { "RBP_AuxiliaryAI_Agricultural", "800" },
            new[] { "RBP_AuxiliaryAI_Artisan", "800" },
            new[] { "RBP_AuxiliaryAI_Construction", "800" },
            new[] { "RBP_AuxiliaryAI_Diplomatic", "800" },
            new[] { "RBP_AuxiliaryAI_Medical", "800" },
            new[] { "RBP_AuxiliaryAI_Mining", "800" },
            new[] { "CircadianAssistant", "800" },
            new[] { "CircadianHalfCycler", "800" },
            new[] { "EPIA_CircadianRefresher", "800" },
            new[] { "LTS_SkillChipPort", "800" },
            new[] { "LearningAssistant", "800" },
            new[] { "Neurocalculator", "800" },
        };

        private static readonly string[] BelowThresholdCores =
        {
            "RBP_AuxiliaryAI_Brawler", "RBP_AuxiliaryAI_Commando", "RBP_AuxiliaryAI_Sharpshooter",
        };

        [Test]
        public static void TheRuleReachesEverythingElse()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            var covered = new List<string>();
            foreach (string[] entry in UniversalRule)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(entry[0]);
                if (def == null)
                    continue;

                int expected = int.Parse(entry[1]);
                int spacers = Check.CostOf(def, Spacer) ?? 0;
                int micros = Check.CostOf(def, Micromachine) ?? 0;
                covered.Add($"{entry[0]}={spacers}c/{micros}mm");

                Check.Soft(spacers * 200 + micros * 800 == expected,
                    $"{entry[0]} is worth {spacers * 200 + micros * 800} in components " +
                    $"({spacers}c + {micros}mm) instead of {expected}; the swap must not move a price");
                Check.Soft(micros >= 1,
                    $"{entry[0]} carries no micromachine but {spacers} advanced components; either " +
                    "the patch did not reach it or its source re-costed it below four");
                Check.Soft(spacers < 4,
                    $"{entry[0]} still carries {spacers} advanced components, a whole group the rule " +
                    "trades in");
            }

            foreach (string name in BelowThresholdCores)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                    continue;
                int spacers = Check.CostOf(def, Spacer) ?? 0;
                int micros = Check.CostOf(def, Micromachine) ?? 0;
                covered.Add($"{name}={spacers}c/{micros}mm");
                Check.Soft(micros == 0 && spacers == 3,
                    $"{name} costs {spacers} advanced components and {micros} micromachine(s); it " +
                    "carries 3, below the group of four, so the rule must leave it alone");
            }

            Check.Note(covered.Count == 0
                ? "none of the universal-rule items present"
                : "universal-rule items: " + string.Join(", ", covered.ToArray()));

            Check.Soft(covered.Count >= 10,
                $"only {covered.Count} of the {UniversalRule.Length + BelowThresholdCores.Length} " +
                "named items were found; this test is asserting almost nothing");

            Check.SoftResult();
        }

        [Test]
        public static void NoDefCarriesAnInheritedMicromachine()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            var carriers = new List<string>();
            var suspect = new List<string>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.costList == null)
                    continue;
                int micros = Check.CostOf(def, Micromachine) ?? 0;
                if (micros == 0)
                    continue;

                int spacers = Check.CostOf(def, Spacer) ?? 0;
                carriers.Add($"{def.defName}={spacers}c/{micros}mm");
                if (spacers >= 4)
                    suspect.Add($"{def.defName} ({spacers}c + {micros}mm, from {ModOf(def)})");
            }

            Check.Note($"{carriers.Count} def(s) carry micromachines");

            foreach (string s in suspect)
                Check.Soft(false,
                    s + " carries micromachines AND four or more advanced components. Every swap " +
                    "trades four components away, so this is a micromachine that arrived some other " +
                    "way - most likely inherited from a patched abstract base by a def whose own " +
                    "costList lacks Inherit=\"False\"");

            Check.Soft(carriers.Count >= 20,
                $"only {carriers.Count} defs carry micromachines; the swap covers well over a " +
                "hundred items, so it has stopped applying and this test asserted nothing");

            Check.SoftResult();
        }

        private static readonly string[] PlainBionicTier =
        {
            "BionicArm", "BionicLeg", "BionicEye", "BionicSpine",
            "BionicHeart", "BionicStomach",
            "SyntheticKidney", "SyntheticLiver", "SyntheticLung",
            "LTS_BionicKidney", "LTS_BionicLiver", "LTS_BionicLung", "LTS_BionicNose",
        };

        [Test]
        public static void NoCyberneticPartStillCarriesAWholeGroup()
        {
            if (!Check.Ready(Key, Ids.GiTS))
                return;

            var exempt = new HashSet<string>(PlainBionicTier);
            var offenders = new List<string>();
            int swept = 0;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.costList == null)
                    continue;

                bool cybernetic = def.isTechHediff
                    || (def.thingCategories != null && def.thingCategories.Exists(c =>
                        c.defName == "LTS_SpacerSkillChips" || c.defName == "LTS_Modules"));
                if (!cybernetic)
                    continue;

                swept++;
                if (exempt.Contains(def.defName))
                    continue;

                int spacers = Check.CostOf(def, Spacer) ?? 0;
                if (spacers >= 4)
                    offenders.Add($"{def.defName} ({spacers}c, from {ModOf(def)})");
            }

            Check.Note($"swept {swept} cybernetic item(s) with a cost list; " +
                $"{exempt.Count} plain-tier exemptions");

            foreach (string o in offenders)
                Check.Soft(false,
                    o + " still carries four or more advanced components. Every manufactured " +
                    "cybernetic part above the plain bionic tier takes micromachines, so either an " +
                    "operation did not reach it - check whether it inherits its cost list rather " +
                    "than declaring one - or it belongs in the plain-tier exemption list");

            foreach (string name in PlainBionicTier)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                    continue;
                Check.Soft((Check.CostOf(def, Micromachine) ?? 0) == 0,
                    $"{name} is plain bionic tier and carries a micromachine; replacing a lost limb " +
                    "or organ is meant to stay reachable without the neural interface node");
            }

            Check.Soft(swept >= 50,
                $"only {swept} cybernetic items were swept; the filter has stopped matching and " +
                "this test is asserting almost nothing");

            Check.SoftResult();
        }

        private static string Show(int? value) => value.HasValue ? value.Value.ToString() : "no";

        private static string ModOf(Def def) => def.modContentPack?.PackageId ?? "unknown mod";
    }
}
