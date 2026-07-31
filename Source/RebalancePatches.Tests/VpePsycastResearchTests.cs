using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VpePsycastResearchTests
    {
        [Test]
        public static void PsychicTabAndGearChain()
        {
            if (!Check.Ready("psycast.research", Ids.VPE, Ids.Royalty))
                return;

            Check.Def<ResearchTabDef>("RBP_PsychicTab");

            ResearchProjectDef attunement = Check.Def<ResearchProjectDef>("RBP_PsyAttunement");
            Check.Soft(attunement.baseCost == 1000f, $"RBP_PsyAttunement.baseCost is {attunement.baseCost}, expected 1000");
            Check.PrereqsAre(attunement.prerequisites, "RBP_PsyAttunement.prerequisites", "NobleApparel");

            ResearchProjectDef working = Check.Def<ResearchProjectDef>("RBP_EltexWorking");
            Check.Soft(working.baseCost == 3000f, $"RBP_EltexWorking.baseCost is {working.baseCost}, expected 3000");
            Check.PrereqsAre(working.prerequisites, "RBP_EltexWorking.prerequisites", "RBP_PsyAttunement", "ComplexClothing");
            Check.Soft(working.techprintCount == 2, $"RBP_EltexWorking.techprintCount is {working.techprintCount}, expected 2");
            Check.Soft(working.heldByFactionCategoryTags != null && working.heldByFactionCategoryTags.Contains("Empire"),
                "RBP_EltexWorking techprint is not held by the Empire");

            ResearchProjectDef weave = Check.Def<ResearchProjectDef>("RBP_PsyArmorweave");
            Check.PrereqsAre(weave.prerequisites, "RBP_PsyArmorweave.prerequisites", "RBP_EltexWorking");

            Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail("VPE_CasterGear") == null,
                "VPE_CasterGear still exists; the absorb did not retire it");
            Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail("VPE_EltexGear") == null,
                "VPE_EltexGear still exists; the absorb did not retire it");

            void RecipeGate(string recipe, string expected)
            {
                RecipeDef def = Check.Optional<RecipeDef>(recipe, "psycast.research");
                if (def == null)
                {
                    Check.Note($"{recipe} absent");
                    return;
                }
                Check.Soft(def.researchPrerequisite?.defName == expected,
                    $"{recipe}.researchPrerequisite is {def.researchPrerequisite?.defName ?? "null"}, expected {expected}");
            }
            RecipeGate("VPE_Make_EltexRobe", "RBP_EltexWorking");
            RecipeGate("VPE_Make_EltexSkullcap", "RBP_EltexWorking");
            RecipeGate("VPE_Make_EltexStaff", "RBP_EltexWorking");
            RecipeGate("VPE_Make_EltexMask", "RBP_PsyAttunement");
            RecipeGate("VPE_Make_EltexCape", "RBP_PsyAttunement");
            RecipeGate("Make_VPE_MeleeWeapon_EltexDagger", "RBP_PsyAttunement");

            Check.SoftResult();
        }

        [Test]
        public static void EltexWeaponryJoinsTheTab()
        {
            if (!Check.Ready("psycast.eltexweaponry", Ids.VPE, Ids.Royalty, Ids.EltexWeaponry))
                return;

            ResearchProjectDef ew = Check.Def<ResearchProjectDef>("EW_EltexWeaponry");
            Check.Soft(ew.tab != null && ew.tab.defName == "RBP_PsychicTab",
                $"EW_EltexWeaponry.tab is {ew.tab?.defName ?? "null"}, expected RBP_PsychicTab");
            Check.PrereqsAre(ew.prerequisites, "EW_EltexWeaponry.prerequisites", "RBP_EltexWorking", "Fabrication");
            Check.Soft(ew.techprintCount == 2, $"EW_EltexWeaponry kept {ew.techprintCount} techprints, expected 2");
            Check.SoftResult();
        }

        [Test]
        public static void PrestigeArmorNeedsArmorweave()
        {
            if (!Check.Ready("psycast.prestigegate", Ids.VPE, Ids.Royalty))
                return;

            // The tag alone is not the family: mods hang PrestigeCombatGear on ordinary armor so nobles
            // will wear it (Cryptoforge, the forsaken set). The weave gate is owed only to items that are
            // actually psychic - a positive sensitivity grant - or actually built from eltex.
            bool Psychic(ThingDef def)
            {
                if (def.equippedStatOffsets != null)
                    foreach (StatModifier mod in def.equippedStatOffsets)
                        if (mod.value > 0f && (mod.stat.defName == "PsychicSensitivity"
                            || mod.stat.defName == "PsychicSensitivityOffset"))
                            return true;
                return def.costList != null
                    && def.costList.Exists(c => c.thingDef != null && c.thingDef.defName == "VPE_Eltex");
            }

            int gated = 0, exempt = 0;
            var missed = new List<string>();
            var wrongly = new List<string>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.apparel?.tags == null || !def.apparel.tags.Contains("PrestigeCombatGear"))
                    continue;
                RecipeDef make = DefDatabase<RecipeDef>.AllDefsListForReading
                    .Find(r => r.products != null && r.products.Count == 1 && r.products[0].thingDef == def);
                if (make == null)
                    continue;
                bool hasGate = (make.researchPrerequisites != null
                        && make.researchPrerequisites.Exists(p => p.defName == "RBP_PsyArmorweave"))
                    || make.researchPrerequisite?.defName == "RBP_PsyArmorweave";
                string detail = $"{def.defName} (recipe {make.defName}, prereqs: " +
                    $"{make.researchPrerequisite?.defName ?? "-"} / " +
                    $"{(make.researchPrerequisites == null ? "-" : string.Join("+", make.researchPrerequisites.ConvertAll(p => p.defName)))})";
                if (Psychic(def))
                {
                    if (hasGate) gated++;
                    else missed.Add(detail);
                }
                else
                {
                    if (hasGate) wrongly.Add(detail);
                    else exempt++;
                }
            }

            Check.Note($"{gated} psychic prestige recipe(s) gated on RBP_PsyArmorweave, {exempt} tag-only item(s) left alone");
            Check.Soft(missed.Count == 0,
                $"{missed.Count} psychic PrestigeCombatGear item(s) escaped the armorweave gate: " + string.Join("; ", missed));
            Check.Soft(wrongly.Count == 0,
                $"{wrongly.Count} tag-only item(s) with no psychic grant and no eltex cost were wrongly gated " +
                "behind the weave: " + string.Join("; ", wrongly));
            Check.Soft(gated >= 6, $"only {gated} prestige recipes gated; the vanilla six alone should qualify");
            Check.SoftResult();
        }

        [Test]
        public static void EltexRobeAndSkullcapRewardDedication()
        {
            if (!Check.Ready("psycast.eltexbuff", Ids.VPE, Ids.Royalty))
                return;

            void Offsets(string defName, params (string stat, float value)[] expected)
            {
                ThingDef def = Check.Def<ThingDef>(defName);
                foreach ((string stat, float value) in expected)
                {
                    StatModifier found = def.equippedStatOffsets?.Find(m => m.stat.defName == stat);
                    Check.Soft(found != null && System.Math.Abs(found.value - value) < 0.001f,
                        $"{defName} {stat} is {(found == null ? "absent" : found.value.ToString())}, expected {value}");
                }
            }
            Offsets("Apparel_PsyfocusRobe",
                ("PsychicSensitivityOffset", 0.3f), ("PsychicEntropyRecoveryRate", 0.1f),
                ("MeditationFocusGain", 0.2f), ("VPE_PsyfocusCostFactor", -0.1f));
            Offsets("Apparel_EltexSkullcap",
                ("PsychicSensitivityOffset", 0.5f), ("PsychicEntropyRecoveryRate", 0.1f),
                ("MeditationFocusGain", 0.1f), ("VPE_PsyfocusCostFactor", -0.05f));
            Check.SoftResult();
        }

        [Test]
        public static void EltexTrousersCloseTheWardrobe()
        {
            if (!Check.Ready("psycast.trousers", Ids.VPE, Ids.Royalty))
                return;

            ThingDef def = Check.Def<ThingDef>("RBP_Apparel_EltexTrousers");
            Check.Soft(def.apparel != null && def.apparel.layers.Exists(l => l.defName == "OnSkin")
                && def.apparel.bodyPartGroups.Exists(g => g.defName == "Legs"),
                "trousers are not an OnSkin legs item");
            Check.Soft(def.apparel != null && def.apparel.wornGraphicPath.NullOrEmpty(),
                "trousers carry a worn graphic; legs apparel should not render on pawns");
            Check.Soft(Check.CostOf(def, "VPE_Eltex") == 2, $"trousers cost {Check.CostOf(def, "VPE_Eltex")} eltex, expected 2");
            RecipeDef make = DefDatabase<RecipeDef>.AllDefsListForReading
                .Find(r => r.products != null && r.products.Exists(p => p.thingDef == def));
            Check.Soft(make != null && make.researchPrerequisite?.defName == "RBP_EltexWorking",
                $"trousers recipe gate is {make?.researchPrerequisite?.defName ?? "absent"}, expected RBP_EltexWorking");
            Check.SoftResult();
        }

        [Test]
        public static void VestmentsAreGatedOnSensitivity()
        {
            if (!Check.Ready("psycast.vestments", Ids.VPE, Ids.Royalty))
                return;

            ResearchProjectDef node = Check.Def<ResearchProjectDef>("RBP_PsyVestments");
            Check.PrereqsAre(node.prerequisites, "RBP_PsyVestments.prerequisites", "RBP_EltexWorking");

            void Garment(string defName, float sens, float gate, int eltex)
            {
                ThingDef def = Check.Def<ThingDef>(defName);
                StatModifier mod = def.equippedStatOffsets?.Find(m => m.stat.defName == "PsychicSensitivityOffset");
                Check.Soft(mod != null && System.Math.Abs(mod.value - sens) < 0.001f,
                    $"{defName} sensitivity offset is {(mod == null ? "absent" : mod.value.ToString())}, expected {sens}");
                var ext = def.GetModExtension<RequiredPsychicSensitivityExtension>();
                Check.Soft(ext != null && System.Math.Abs(ext.minSensitivity - gate) < 0.001f,
                    $"{defName} wear gate is {(ext == null ? "absent" : ext.minSensitivity.ToString())}, expected {gate}");
                Check.Soft(Check.CostOf(def, "VPE_Eltex") == eltex,
                    $"{defName} costs {Check.CostOf(def, "VPE_Eltex")} eltex, expected {eltex}");
                Check.Soft(def.tradeability == Tradeability.Sellable, $"{defName} tradeability is {def.tradeability}, expected Sellable");
                Check.Soft(def.thingSetMakerTags != null && def.thingSetMakerTags.Contains("RewardStandardLowFreq"),
                    $"{defName} lacks the low-frequency quest reward tag");
                RecipeDef make = DefDatabase<RecipeDef>.AllDefsListForReading
                    .Find(r => r.products != null && r.products.Exists(p => p.thingDef == def));
                Check.Soft(make != null && make.researchPrerequisite?.defName == "RBP_PsyVestments",
                    $"{defName} recipe gate is {make?.researchPrerequisite?.defName ?? "absent"}, expected RBP_PsyVestments");
            }
            Garment("RBP_Apparel_EltexVestment", 0.65f, 1.75f, 15);
            Garment("RBP_Apparel_EltexDiadem", 0.55f, 1.5f, 10);

            Check.HarmonyPatched(HarmonyLib.AccessTools.Method(typeof(RimWorld.EquipmentUtility), "CanEquip",
                new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }), "psycast.vestments wear gate");
            Check.SoftResult();
        }

        [Test]
        public static void OracleOpensOneLockedPath()
        {
            if (!Check.Ready("psycast.oraclewaiver", Ids.VPE, Ids.GiTS))
                return;

            HediffDef oracle = Check.Optional<HediffDef>("RBP_EchoOracleHediff", "psycast.oraclewaiver");
            if (oracle == null)
            {
                Check.Note("RBP_EchoOracleHediff absent (cybernetics.echobrains off), waiver is a no-op as designed");
                return;
            }
            Check.Soft(oracle.comps != null && oracle.comps.Exists(c => c is HediffCompProperties_OracleWaiver),
                "the ORACLE hediff carries no waiver comp, so the choice would not be remembered");

            System.Type pathType = HarmonyLib.AccessTools.TypeByName("VanillaPsycastsExpanded.PsycasterPathDef");
            if (Check.Soft(pathType != null, "PsycasterPathDef not found"))
                Check.HarmonyPatched(HarmonyLib.AccessTools.Method(pathType, "CanPawnUnlock", new[] { typeof(Pawn) }),
                    "psycast.oraclewaiver unlock check");
            System.Type implantType = HarmonyLib.AccessTools.TypeByName("VanillaPsycastsExpanded.Hediff_PsycastAbilities");
            if (Check.Soft(implantType != null, "Hediff_PsycastAbilities not found"))
                Check.HarmonyPatched(HarmonyLib.AccessTools.Method(implantType, "UnlockPath", new[] { pathType }),
                    "psycast.oraclewaiver recorder");
            Check.SoftResult();
        }

        [Test]
        public static void EchoLaneNeedsEltexWorking()
        {
            if (!Check.Ready("psycast.echotie", Ids.VPE, Ids.Royalty))
                return;

            ResearchProjectDef echo = Check.Optional<ResearchProjectDef>("RBP_CybEchoNeuralRegulation", "psycast.echotie");
            if (echo == null)
            {
                Check.Note("RBP_CybEchoNeuralRegulation absent (cyberneticsresearch.mind off), tie is a no-op as designed");
                return;
            }
            Check.True(Check.ContainsResearch(echo.prerequisites, "RBP_EltexWorking"),
                "the Echo lane's entry node does not require eltex working while both overhauls are on");
        }
    }
}
