using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GiTSTests
    {
        [Test]
        public static void BasicCyberbrainHostsModules()
        {
            if (!Check.Ready("cybernetics.modules", Ids.GiTS, Ids.EBSG))
                return;

            HediffDef hediff = Check.Def<HediffDef>("gitsBasicCyberbrainHediff");
            Check.True(hediff.comps != null, "basic cyberbrain has comps");

            object modular = null;
            foreach (HediffCompProperties props in hediff.comps)
                if (props != null && props.GetType().Name.Contains("HediffCompProperties_Modular"))
                {
                    modular = props;
                    break;
                }
            Check.True(modular != null, "basic cyberbrain carries a modular comp");

            System.Collections.IEnumerable slots =
                Check.Field(modular, "slots") as System.Collections.IEnumerable;
            Check.True(slots != null, "modular comp declares slots");

            bool found = false;
            foreach (object slot in slots)
                if ((Check.Field(slot, "slotID") as string) == "RBP_CortexSlot")
                {
                    found = true;
                    Check.Eq(System.Convert.ToSingle(Check.Field(slot, "capacity")), 1f,
                        "RBP_CortexSlot capacity");
                }
            Check.True(found, "RBP_CortexSlot present on the basic cyberbrain");
        }

        [Test]
        public static void EveryCyberbrainTierHostsModules()
        {
            if (!Check.Ready("cybernetics.modules", Ids.GiTS, Ids.EBSG))
                return;

            var expected = new System.Collections.Generic.Dictionary<string, float>
            {
                { "gitsBasicCyberbrainHediff", 1f },     // gitsAddedCyberbrainBasicBase
                { "gitsEnhancedCyberbrainHediff", 2f },  // gitsAddedCyberbrainEnhancedBase
                { "gitsBS40Hediff", 3f },                // gitsAddedCyberbrainSpecWorkBase
                { "gitsHCQ11Hediff", 3f },               // gitsAddedCyberbrainSpecCombatBase
                { "gitsXS176Hediff", 4f },               // gitsAddedCyberbrainAdvWorkBase
                { "gitsCSOF2Hediff", 4f },               // gitsAddedCyberbrainAdvCombatBase
                { "gitsHADESHediff", 5f },               // gitsAddedCyberbrainExtremeBase
            };

            foreach (var pair in expected)
            {
                HediffDef hediff = Check.Def<HediffDef>(pair.Key);
                Check.True(hediff.comps != null, $"{pair.Key} has comps");

                object modular = null;
                foreach (HediffCompProperties props in hediff.comps)
                    if (props != null && props.GetType().Name.Contains("HediffCompProperties_Modular"))
                    {
                        modular = props;
                        break;
                    }
                Check.True(modular != null, $"{pair.Key} carries a modular comp");

                System.Collections.IEnumerable slots =
                    Check.Field(modular, "slots") as System.Collections.IEnumerable;
                Check.True(slots != null, $"{pair.Key} modular comp declares slots");

                bool found = false;
                foreach (object slot in slots)
                    if ((Check.Field(slot, "slotID") as string) == "RBP_CortexSlot")
                    {
                        found = true;
                        Check.Eq(System.Convert.ToSingle(Check.Field(slot, "capacity")), pair.Value,
                            $"{pair.Key} RBP_CortexSlot capacity");
                    }
                Check.True(found, $"RBP_CortexSlot present on {pair.Key}");
            }
        }

        [Test]
        public static void OnlyBasicCyberbrainsSold()
        {
            if (!Check.Ready("gits.merchant", Ids.GiTS))
                return;
            int tierBrains = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!def.defName.StartsWith("gits") || !def.defName.EndsWith("Cyberbrain"))
                    continue;
                if (def.defName == "gitsBasicCyberbrain" || def.defName == "gitsUnfinishedCyberbrain")
                    continue;
                tierBrains++;
                Check.True(def.tradeTags.NullOrEmpty(),
                    $"{def.defName} still has tradeTags ({(def.tradeTags == null ? "" : string.Join(", ", def.tradeTags))})");
            }
            Check.True(tierBrains >= 3, $"only {tierBrains} tier cyberbrains found - defName scheme changed?");
        }

        [Test]
        public static void HarsherExtremeMentalBreak()
        {
            if (!Check.Ready("gits.mentalbreak", Ids.GiTS))
                return;
            foreach (string hediffName in new[] { "gitsPX7Hediff", "gitsHADESHediff" })
            {
                HediffDef hediff = Check.Def<HediffDef>(hediffName);
                bool found = false;
                if (hediff.stages != null)
                    foreach (HediffStage stage in hediff.stages)
                        if (Check.StatModifierValue(stage.statOffsets, "MentalBreakThreshold") == 0.40f)
                            found = true;
                Check.True(found, $"{hediffName} has no stage with MentalBreakThreshold +0.40");
            }
        }

        /// <summary>True once the research overhaul has dissolved GiTS's own research tree.</summary>
        private static bool ResearchOverhaulActive =>
            DefDatabase<ResearchProjectDef>.GetNamedSilentFail("RBP_CybSurgicalImplantation") != null;

        [Test]
        public static void StreamlinedResearchTree()
        {
            if (!Check.Ready("gits.research", Ids.GiTS))
                return;

            // True on both paths: these five are redundant either way.
            foreach (string removed in new[] { "gitsResearchImmunityNanites", "gitsToxinRepairNanites",
                "gitsOrganDecayNanites", "gitsResearchCyberInterface", "gitsResearchNet" })
                Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(removed) == null,
                    $"filler research {removed} still exists");

            if (ResearchOverhaulActive)
            {
                Check.Note("research overhaul active - asserting GiTS research is fully dissolved");
                foreach (string dissolved in new[] { "gitsResearchNaniteGrafting", "gitsResearchMicromachines",
                    "gitsResearchCyberNetIntegration", "gitsResearchBasicCyberization" })
                    Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(dissolved) == null,
                        $"{dissolved} survived the research overhaul's retire pass");
            }
            else
            {
                ResearchProjectDef grafting = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("gitsResearchNaniteGrafting");
                if (Check.Soft(grafting != null, "gitsResearchNaniteGrafting missing with the overhaul off"))
                {
                    Check.Soft(grafting.baseCost == 800f,
                        $"gitsResearchNaniteGrafting.baseCost is {grafting.baseCost}, expected 800");
                    int naniteSurgeries = 0;
                    foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                        if (recipe.defName.StartsWith("gits")
                            && Check.ContainsResearch(recipe.researchPrerequisites, grafting.defName))
                            naniteSurgeries++;
                    Check.Soft(naniteSurgeries > 0, "no gits surgery recipe unlocks from gitsResearchNaniteGrafting");
                }
            }

            Check.SoftResult();
        }

        [Test]
        public static void SurgeriesViaEpoeBrainSurgery()
        {
            if (!Check.Ready("gits.surgeries", Ids.GiTS, Ids.EPOEForked))
                return;

            string expected = ResearchOverhaulActive ? "RBP_CybSurgicalImplantation" : "BrainSurgery";
            Check.Note($"cyberbrain surgeries expected on '{expected}'");

            int onExpected = 0;
            var stranded = new System.Collections.Generic.List<string>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.defName == null || !recipe.defName.StartsWith("gitsInstall"))
                    continue;
                if (recipe.addsHediff == null)
                    continue;
                if (recipe.researchPrerequisite?.defName == expected)
                    onExpected++;
                else if (recipe.researchPrerequisite == null
                         && (recipe.researchPrerequisites == null || recipe.researchPrerequisites.Count == 0))
                    stranded.Add(recipe.defName);
            }

            Check.Soft(onExpected > 0, $"no gits install surgery resolves to {expected}");
            Check.Soft(stranded.Count == 0,
                $"{stranded.Count} gits install surgery/surgeries have no research gate at all - a deleted " +
                $"project left them stranded: {string.Join(", ", stranded.ToArray())}");

            Check.SoftResult();
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> RoleNames =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "CS14", "Civis Culinary" },      { "DS23", "Civis Mining" },
            { "PS39", "Civis Agrarian" },      { "BS40", "Civis Construction" },
            { "FS57", "Civis Industrial" },    { "TS63", "Civis Diplomatic" },
            { "MS71", "Civis Medical" },       { "RS86", "Civis Analytic" },
            { "FS113", "Civis Homestead" },    { "YS151", "Civis Foundry" },
            { "XS176", "Civis Engineering" },  { "HCQ11", "Aegis Duelist" },
            { "HCQ14N", "Aegis Skirmisher" },  { "HCQ14T", "Aegis Reaver" },
            { "SS9G", "Aegis Gunner" },        { "SS9M", "Aegis Marksman" },
            { "HCQ19", "Aegis Warden" },       { "CSOF2", "Aegis Sniper" },
            { "CSOF4", "Aegis Vanguard" },     { "PX7", "Civis PX-7" },
            { "HADES", "Aegis HADES" },
        };

        [Test]
        public static void CyberbrainsNamedByRole()
        {
            if (!Check.Ready("gits.cyberbrainnames", Ids.GiTS))
                return;

            foreach (var pair in RoleNames)
            {
                string code = pair.Key, name = pair.Value;

                ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail($"gits{code}Cyberbrain");
                if (Check.Soft(item != null, $"gits{code}Cyberbrain missing"))
                {
                    Check.Soft(item.label == $"{name} cyberbrain",
                        $"gits{code}Cyberbrain label is '{item.label}', expected '{name} cyberbrain'");
                    Check.Soft(item.description != null && item.description.Contains(name),
                        $"gits{code}Cyberbrain description does not mention '{name}'");
                }

                HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail($"gits{code}Hediff");
                if (Check.Soft(hediff != null, $"gits{code}Hediff missing"))
                {
                    Check.Soft(hediff.label == $"{name} cyberbrain",
                        $"gits{code}Hediff label is '{hediff.label}', expected '{name} cyberbrain'");
                    Check.Soft(hediff.labelNoun == $"an installed {name} cyberbrain",
                        $"gits{code}Hediff labelNoun is '{hediff.labelNoun}'");
                }

                RecipeDef install = DefDatabase<RecipeDef>.GetNamedSilentFail($"gitsInstall{code}Cyberbrain");
                if (Check.Soft(install != null, $"gitsInstall{code}Cyberbrain missing"))
                    Check.Soft(install.label == $"install {name} cyberbrain",
                        $"gitsInstall{code}Cyberbrain label is '{install.label}'");
            }

            foreach (string maker in new[] { "Poseidon", "Hanka", "Locus-Solus" })
            {
                var hits = new System.Collections.Generic.List<string>();
                foreach (ThingDef d in DefDatabase<ThingDef>.AllDefsListForReading)
                    if (d.defName != null && d.defName.StartsWith("gits")
                        && ((d.label != null && d.label.Contains(maker))
                            || (d.description != null && d.description.Contains(maker))))
                        hits.Add(d.defName);
                foreach (HediffDef d in DefDatabase<HediffDef>.AllDefsListForReading)
                    if (d.defName != null && d.defName.StartsWith("gits")
                        && ((d.label != null && d.label.Contains(maker))
                            || (d.description != null && d.description.Contains(maker))))
                        hits.Add(d.defName);
                foreach (RecipeDef d in DefDatabase<RecipeDef>.AllDefsListForReading)
                    if (d.defName != null && d.defName.StartsWith("gits")
                        && ((d.label != null && d.label.Contains(maker))
                            || (d.description != null && d.description.Contains(maker))
                            || (d.jobString != null && d.jobString.Contains(maker))))
                        hits.Add(d.defName);
                Check.Soft(hits.Count == 0,
                    $"'{maker}' branding survives the rename on: {string.Join(", ", hits.ToArray())}");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CyberbrainsDropInertPartEfficiency()
        {
            if (!Check.Ready("gits.cyberbrainnames", Ids.GiTS))
                return;

            var offenders = new System.Collections.Generic.List<string>();
            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefsListForReading)
                if (d.defName != null && d.defName.EndsWith("Cyberbrain") && d.description != null
                    && d.description.Contains("Part Efficiency"))
                    offenders.Add(d.defName);

            Check.Soft(offenders.Count == 0,
                $"{offenders.Count} cyberbrain(s) still advertise the inert Part Efficiency line: " +
                string.Join(", ", offenders.ToArray()));
            Check.SoftResult();
        }

        [Test]
        public static void CyberbrainDescriptionsAreLoreFree()
        {
            if (!Check.Ready("gits.cyberbrainnames", Ids.GiTS))
                return;

            string[] lore = { "ghost retention", "cyberization", "cyberized", "Fort Plugs", "interface to the net" };
            var offenders = new System.Collections.Generic.List<string>();
            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (d.defName == null || !d.defName.StartsWith("gits") || !d.defName.EndsWith("Cyberbrain")
                    || d.defName == "gitsUnfinishedCyberbrain" || d.description == null)
                    continue;
                foreach (string term in lore)
                    if (d.description.Contains(term))
                        offenders.Add($"{d.defName}:'{term}'");
            }
            Check.Soft(offenders.Count == 0,
                $"GiTS lore survives in cyberbrain descriptions: {string.Join(", ", offenders.ToArray())}");
            Check.SoftResult();
        }

        [Test]
        public static void CyberbrainDescriptionsStateCortexSlots()
        {
            if (!Check.Ready("gits.cyberbrainnames", Ids.GiTS, Ids.EBSG))
                return;

            foreach (HediffDef hediff in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                if (hediff.defName == null || !hediff.defName.StartsWith("gits") || hediff.comps == null)
                    continue;
                int capacity = -1;
                foreach (HediffCompProperties props in hediff.comps)
                {
                    if (props == null || !props.GetType().Name.Contains("HediffCompProperties_Modular"))
                        continue;
                    if (!(Check.Field(props, "slots") is System.Collections.IEnumerable slots))
                        continue;
                    foreach (object slot in slots)
                        if ((Check.Field(slot, "slotID") as string) == "RBP_CortexSlot")
                            capacity = System.Convert.ToInt32(Check.Field(slot, "capacity"));
                }
                if (capacity < 0)
                    continue;

                string itemName = hediff.defName.EndsWith("CyberbrainHediff")
                    ? hediff.defName.Substring(0, hediff.defName.Length - "Hediff".Length)
                    : hediff.defName.Substring(0, hediff.defName.Length - "Hediff".Length) + "Cyberbrain";
                ThingDef item = DefDatabase<ThingDef>.GetNamedSilentFail(itemName);
                if (!Check.Soft(item != null, $"no item {itemName} for {hediff.defName}"))
                    continue;
                Check.Soft(item.description != null && item.description.Contains($"Cortex module slots: {capacity}"),
                    $"{itemName} description does not state 'Cortex module slots: {capacity}'");
            }
            Check.SoftResult();
        }
    }
}
