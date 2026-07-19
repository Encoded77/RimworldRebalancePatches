using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimsenalCoreTests
    {
        [Test]
        public static void ArmorTechs()
        {
            if (!Check.Ready("rimsenal.armortechs", Ids.RimsenalCore))
                return;
            void SingleIs(string thing, string research)
            {
                ThingDef def = Check.Def<ThingDef>(thing);
                Check.Eq(Check.RecipePrereq(def)?.defName, research, $"{thing} recipeMaker.researchPrerequisite");
            }
            foreach (string thing in new[] { "Apparel_Strikesuit", "Apparel_Dropsuit" })
                Check.PrereqsAre(Check.RecipePrereqs(Check.Def<ThingDef>(thing)), $"{thing} recipeMaker.researchPrerequisites", "ShardTech");
            foreach (string thing in new[] { "Apparel_StrikesuitH", "Apparel_DropsuitH" })
                SingleIs(thing, "ShardTech");
            foreach (string thing in new[] { "Apparel_HazardCarapace", "Apparel_ReflactorArmor", "Apparel_HazardCarapaceH", "Apparel_ReflactorArmorH" })
                SingleIs(thing, "KineticTech");
            foreach (string thing in new[] { "Apparel_FSArmor", "Apparel_AssaultArmor", "Apparel_FSArmorH", "Apparel_AssaultArmorH" })
                SingleIs(thing, "MoltenTech");
            foreach (string thing in new[] { "Apparel_SecurityArmor", "Apparel_SecurityHelmet", "Apparel_MedicArmor",
                "Apparel_PioneerArmor", "Apparel_NomadArmor", "Apparel_MedicH", "Apparel_PioneerH", "Apparel_NomadH" })
                SingleIs(thing, "DefenceTech");
            SingleIs("Apparel_Korp", "SiegeTech");
        }

        [Test]
        public static void ModularWeaponTechs()
        {
            if (!Check.Ready("rimsenal.modularweapons", Ids.RimsenalCore))
                return;
            foreach (string thing in new[] { "GD_ModularCarbine", "MRSKitR", "MBPSKit" })
                Check.Eq(Check.RecipePrereq(Check.Def<ThingDef>(thing))?.defName, "DefenceTech", $"{thing} recipeMaker.researchPrerequisite");
            // BaseGDGun's inherited single researchPrerequisite survives the patch; the game requires it AND the list.
            ThingDef launcher = Check.Def<ThingDef>("GD_GrenadeLauncher");
            Check.Eq(Check.RecipePrereq(launcher)?.defName, "DefenceTech", "GD_GrenadeLauncher recipeMaker.researchPrerequisite");
            Check.PrereqsAre(Check.RecipePrereqs(launcher), "GD_GrenadeLauncher recipeMaker.researchPrerequisites", "Mortars", "DefenceTech");
        }

        [Test]
        public static void VacuumTrims()
        {
            if (!Check.Ready("odyssey.vacuumtrims", Ids.RimsenalCore, Ids.Odyssey, Ids.VGravshipC1))
                return;
            foreach (string thing in new[] { "Apparel_StrikesuitH", "Apparel_AssaultArmorH", "Apparel_FSArmorH", "Apparel_CloseCombatHelmet", "Apparel_DropsuitH" })
                Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>(thing).equippedStatOffsets, "VacuumResistance"), 0.61f, $"{thing} VacuumResistance");
            foreach (string thing in new[] { "Apparel_Strikesuit", "Apparel_Dropsuit" })
                Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>(thing).equippedStatOffsets, "VacuumResistance"), 0.31f, $"{thing} VacuumResistance");
            Check.Eq(Check.StatModifierValue(Check.Def<ThingDef>("Apparel_ReflactorArmorH").equippedStatOffsets, "VacuumResistance"), 0.65f, "Apparel_ReflactorArmorH VacuumResistance");
        }

        [Test]
        public static void CorpTechCosts()
        {
            if (!Check.Ready("rimsenal.corpcost", Ids.RimsenalCore))
                return;
            foreach (string research in new[] { "KineticTech", "MoltenTech", "ShardTech" })
                Check.Eq(Check.Def<ResearchProjectDef>(research).baseCost, 3000f, $"{research}.baseCost");
        }
    }
}
