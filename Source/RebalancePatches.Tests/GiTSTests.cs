using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GiTSTests
    {
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

        [Test]
        public static void StreamlinedResearchTree()
        {
            if (!Check.Ready("gits.research", Ids.GiTS))
                return;
            Check.Eq(Check.Def<ResearchProjectDef>("gitsResearchNaniteGrafting").baseCost, 800f, "gitsResearchNaniteGrafting.baseCost");
            foreach (string removed in new[] { "gitsResearchImmunityNanites", "gitsToxinRepairNanites",
                "gitsOrganDecayNanites", "gitsResearchCyberInterface", "gitsResearchNet" })
                Check.True(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(removed) == null,
                    $"filler research {removed} still exists");
            Check.PrereqsAre(Check.Def<ResearchProjectDef>("gitsResearchCyberNetIntegration").prerequisites,
                "gitsResearchCyberNetIntegration.prerequisites", "gitsResearchMicromachines", "MicroelectronicsBasics");
            ResearchProjectDef grafting = Check.Def<ResearchProjectDef>("gitsResearchNaniteGrafting");
            int naniteSurgeries = 0;
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                if (recipe.defName.StartsWith("gits") && Check.ContainsResearch(recipe.researchPrerequisites, grafting.defName))
                    naniteSurgeries++;
            Check.True(naniteSurgeries > 0, "no gits surgery recipe unlocks from gitsResearchNaniteGrafting");
        }

        [Test]
        public static void SurgeriesViaEpoeBrainSurgery()
        {
            if (!Check.Ready("gits.surgeries", Ids.GiTS, Ids.EPOEForked))
                return;
            ResearchProjectDef brainSurgery = Check.Def<ResearchProjectDef>("BrainSurgery");
            foreach (string research in new[] { "gitsResearchEnhancedCyberization", "gitsResearchSpecializedCyberization",
                "gitsResearchExtremeCyberization", "gitsResearchAdvSpecializedCyberization", "gitsResearchCombatCyberization" })
                Check.Eq(Check.Def<ResearchProjectDef>(research).techLevel, TechLevel.Ultra, $"{research}.techLevel");
            ResearchProjectDef basic = Check.Def<ResearchProjectDef>("gitsResearchBasicCyberization");
            Check.True(Check.ContainsResearch(basic.prerequisites, "BrainSurgery"),
                "gitsResearchBasicCyberization lacks BrainSurgery prerequisite");
            Check.True(Check.ContainsResearch(basic.prerequisites, "gitsResearchNaniteGrafting"),
                "gitsResearchBasicCyberization lacks gitsResearchNaniteGrafting prerequisite");
            Check.True(DefDatabase<ResearchProjectDef>.GetNamedSilentFail("gitsResearchBrainCyberization") == null,
                "gitsResearchBrainCyberization still exists");
            int brainSurgeryRecipes = 0;
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                if (recipe.defName.StartsWith("gits") && recipe.researchPrerequisite == brainSurgery)
                    brainSurgeryRecipes++;
            Check.True(brainSurgeryRecipes > 0, "no gits cyberbrain surgery unlocks from BrainSurgery");
        }
    }
}
