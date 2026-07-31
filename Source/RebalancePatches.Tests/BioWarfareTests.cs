using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BioWarfareTests
    {
        [Test]
        public static void ResearchTreeIsTwoNodes()
        {
            if (!Check.Ready("biowarfare.researchtree", Ids.BioWarfare))
                return;

            ResearchProjectDef weapons = Check.Def<ResearchProjectDef>("RBP_USH_Weaponisation");
            Check.PrereqsAre(weapons.prerequisites, "RBP_USH_Weaponisation.prerequisites", "USH_PathogenProductionRes");
            Check.Soft(weapons.baseCost == 1200f, $"RBP_USH_Weaponisation.baseCost is {weapons.baseCost}, expected 1200");

            ResearchProjectDef antibodies = Check.Def<ResearchProjectDef>("RBP_USH_Antibodies");
            Check.PrereqsAre(antibodies.prerequisites, "RBP_USH_Antibodies.prerequisites", "USH_PathogenProductionRes");
            Check.Soft(antibodies.requiredResearchBuilding != null
                && antibodies.requiredResearchBuilding.defName == "USH_AntigensAnalyzer",
                "RBP_USH_Antibodies is not bound to the antigens analyzer, so it would be researchable "
                + "from the normal tab without a disease sample");

            // The whole point of the consolidation: the per-disease ladder is gone, and with it the
            // staging tolls that made specialising cost more than the content behind it.
            var leftovers = new List<string>();
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                if (project.defName == null)
                    continue;
                if (project.defName.StartsWith("USH_") &&
                    (project.defName.Contains("OperationRes") || project.defName.Contains("VaccineRes")))
                    leftovers.Add(project.defName);
                if (project.defName == "RBP_USH_AdvancedPathogens" || project.defName == "RBP_USH_LethalPathogens"
                    || project.defName == "RBP_USH_AntigenAnalysis")
                    leftovers.Add(project.defName + " (retired staging node)");
            }
            Check.Soft(leftovers.Count == 0,
                $"{leftovers.Count} per-disease or staging research project(s) survived the consolidation: "
                + string.Join(", ", leftovers.ToArray()));

            // Nothing may still point at a retired project: those are hard cross-reference errors.
            var dangling = new List<string>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                if (def.researchPrerequisites != null)
                    foreach (ResearchProjectDef prereq in def.researchPrerequisites)
                        if (prereq == null)
                            dangling.Add(def.defName);
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                if (recipe.researchPrerequisite == null && recipe.researchPrerequisites != null)
                    foreach (ResearchProjectDef prereq in recipe.researchPrerequisites)
                        if (prereq == null)
                            dangling.Add(recipe.defName);
            Check.Soft(dangling.Count == 0,
                $"{dangling.Count} def(s) carry an unresolved research prerequisite after the retire pass: "
                + string.Join(", ", dangling.ToArray()));

            Check.SoftResult();
        }

        [Test]
        public static void EveryDiseaseVaccinesFromOneProject()
        {
            if (!Check.Ready("biowarfare.researchtree", Ids.BioWarfare))
                return;

            // The analyzer researches whatever project the loaded sample's disease names, so every
            // disease has to name the consolidated one or its sample becomes unusable.
            System.Type diseaseType = HarmonyLib.AccessTools.TypeByName("USH_BW.CombatDiseaseDef");
            if (!Check.Soft(diseaseType != null, "USH_BW.CombatDiseaseDef not found"))
            {
                Check.SoftResult();
                return;
            }

            var wrong = new List<string>();
            var noVaccine = new List<string>();
            int repointed = 0;
            foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(diseaseType))
            {
                string named = (Check.Field(def, "vaccineResProjectDef") as Def)?.defName;
                // Flesh-breaker is a chemical, not an infection: the mod ships no vaccine for it, so a
                // null here is the mod's own design rather than a repoint we missed.
                if (named == null)
                    noVaccine.Add(def.defName);
                else if (named == "RBP_USH_Antibodies")
                    repointed++;
                else
                    wrong.Add($"{def.defName} -> {named}");
            }
            Check.Note($"{repointed} disease(s) repointed, {noVaccine.Count} with no vaccine by design: "
                + string.Join(", ", noVaccine.ToArray()));
            Check.Soft(wrong.Count == 0,
                $"{wrong.Count} disease(s) still point at a per-disease vaccine project: " + string.Join(", ", wrong.ToArray()));
            Check.Soft(repointed >= 6,
                $"only {repointed} disease(s) point at the consolidated project; every vaccinable disease should");
            Check.SoftResult();
        }
    }
}
