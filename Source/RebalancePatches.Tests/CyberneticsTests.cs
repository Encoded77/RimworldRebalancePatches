using RimWorld;
using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CyberneticsTests
    {
        [Test]
        public static void ThoracicFrameTiers()
        {
            if (!Check.Ready("cybernetics.thoracicframe", Ids.EBSG))
                return;

            AssertFrame("RBP_ThoracicFrameBionicHediff", "RBP_ThoracicFrameBionic",
                "RBP_InstallThoracicFrameBionic", 1.15f, 2f);
            AssertFrame("RBP_ThoracicFrameAdvancedHediff", "RBP_ThoracicFrameAdvanced",
                "RBP_InstallThoracicFrameAdvanced", 1.35f, 3f);
            AssertFrame("RBP_ThoracicFrameArchotechHediff", "RBP_ThoracicFrameArchotech",
                "RBP_InstallThoracicFrameArchotech", 1.5f, 4f);
        }

        [Test]
        public static void ResearchGatesCraftingNotFitting()
        {
            if (!Check.Ready("cybernetics.thoracicframe"))
                return;

            ThingDef advanced = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_ThoracicFrameAdvanced");
            if (Check.Soft(advanced != null, "RBP_ThoracicFrameAdvanced not found"))
            {
                ResearchProjectDef gate = advanced.recipeMaker?.researchPrerequisite;
                Check.Soft(gate != null && gate.defName != "Bionics",
                    $"advanced thoracic frame (Ultra, 3 slots) crafts at '{gate?.defName ?? "no gate"}' - " +
                    "inheriting vanilla Bionics makes it free at the bionic tier");
            }

            // Crafting-tier gates that must never appear on an install surgery.
            string[] craftingGates = { "Bionics", "AdvancedBionics", "AdvancedFabrication" };
            foreach (string recipeName in new[]
            {
                "RBP_InstallThoracicFrameBionic",
                "RBP_InstallThoracicFrameAdvanced",
                "RBP_InstallThoracicFrameArchotech",
            })
            {
                RecipeDef install = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (!Check.Soft(install != null, $"{recipeName} not found"))
                    continue;

                var offending = new System.Collections.Generic.List<string>();
                if (install.researchPrerequisite != null
                    && System.Array.IndexOf(craftingGates, install.researchPrerequisite.defName) >= 0)
                    offending.Add(install.researchPrerequisite.defName);
                if (install.researchPrerequisites != null)
                    foreach (ResearchProjectDef p in install.researchPrerequisites)
                        if (p != null && System.Array.IndexOf(craftingGates, p.defName) >= 0)
                            offending.Add(p.defName);

                Check.Soft(offending.Count == 0,
                    $"{recipeName} is gated on crafting-tier research ({string.Join(", ", offending.ToArray())}) - " +
                    "installs belong at the Surgical implantation root, or a rewarded part cannot be fitted");
            }

            Check.SoftResult();
        }

        private static void AssertFrame(string hediffName, string thingName, string installRecipeName,
            float partEfficiency, float slotCapacity)
        {
            HediffDef hediff = Check.Def<HediffDef>(hediffName);

            Check.True(hediff.addedPartProps != null, $"{hediffName} has addedPartProps");
            Check.Eq(hediff.addedPartProps.partEfficiency, partEfficiency, $"{hediffName} partEfficiency");
            Check.True(hediff.addedPartProps.solid, $"{hediffName} addedPartProps.solid");
            Check.True(hediff.addedPartProps.betterThanNatural, $"{hediffName} addedPartProps.betterThanNatural");

            // The item the frame pops back out as, and what Quality Bionics keys off.
            ThingDef thing = Check.Def<ThingDef>(thingName);
            Check.Eq(hediff.spawnThingOnRemoved, thing, $"{hediffName} spawnThingOnRemoved");
            Check.True(thing.isTechHediff, $"{thingName} isTechHediff");

            Check.True(hediff.comps != null, $"{hediffName} has comps");
            object modular = null;
            foreach (HediffCompProperties props in hediff.comps)
                if (props != null && props.GetType().Name.Contains("HediffCompProperties_Modular"))
                {
                    modular = props;
                    break;
                }
            Check.True(modular != null, $"{hediffName} carries a modular comp");

            System.Collections.IEnumerable slots =
                Check.Field(modular, "slots") as System.Collections.IEnumerable;
            Check.True(slots != null, $"{hediffName} modular comp declares slots");

            bool found = false;
            foreach (object slot in slots)
                if ((Check.Field(slot, "slotID") as string) == "RBP_TorsoSlot")
                {
                    found = true;
                    Check.Eq(System.Convert.ToSingle(Check.Field(slot, "capacity")), slotCapacity,
                        $"{hediffName} RBP_TorsoSlot capacity");
                }
            Check.True(found, $"RBP_TorsoSlot present on {hediffName}");

            RecipeDef install = Check.Def<RecipeDef>(installRecipeName);
            Check.Eq(install.addsHediff, hediff, $"{installRecipeName} addsHediff");
            Check.True(Check.AnyDefNamed(install.appliedOnFixedBodyParts, "Ribcage"),
                $"{installRecipeName} does not target Ribcage");
            if (DefDatabase<BodyPartDef>.GetNamedSilentFail("BS_MechanicalRibs") != null)
                Check.True(Check.AnyDefNamed(install.appliedOnFixedBodyParts, "BS_MechanicalRibs"),
                    $"{installRecipeName} does not target BS_MechanicalRibs");
        }
    }
}
