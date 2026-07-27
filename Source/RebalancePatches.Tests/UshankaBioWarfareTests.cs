using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class UshankaBioWarfareTests
    {
        private const string Root = "USH_PathogenProductionRes";
        private const string Advanced = "RBP_USH_AdvancedPathogens";
        private const string Lethal = "RBP_USH_LethalPathogens";
        private const string Antigen = "RBP_USH_AntigenAnalysis";

        [Test]
        public static void StagingNodesExistAndChainOffTheRoot()
        {
            if (!Check.Ready("biowarfare.researchtree", Ids.UshankaBioWarfare))
                return;

            ResearchProjectDef advanced = Check.Def<ResearchProjectDef>(Advanced);
            ResearchProjectDef lethal = Check.Def<ResearchProjectDef>(Lethal);
            ResearchProjectDef antigen = Check.Def<ResearchProjectDef>(Antigen);

            Check.Soft(PrereqIs(advanced, Root), $"{Advanced} does not require the root {Root}");
            Check.Soft(PrereqIs(lethal, Advanced), $"{Lethal} does not require {Advanced} - the offence tiers are not chained");
            Check.Soft(PrereqIs(antigen, Root), $"{Antigen} does not require the root {Root}");

            // The antigens analyzer now unlocks from the antigen-analysis node.
            ThingDef analyzer = DefDatabase<ThingDef>.GetNamedSilentFail("USH_AntigensAnalyzer");
            if (analyzer != null)
                Check.Soft(ThingResearchIs(analyzer, Antigen),
                    $"USH_AntigensAnalyzer still unlocks from something other than {Antigen}");

            Check.SoftResult();
        }

        [Test]
        public static void WeaponResearchClimbsTheTiersWithRisingCost()
        {
            if (!Check.Ready("biowarfare.researchtree", Ids.UshankaBioWarfare))
                return;

            // Tier 1 stays on the root at its original cost.
            foreach (string d in new[] { "Flu", "SleepingSickness" })
            {
                ResearchProjectDef op = Check.Def<ResearchProjectDef>($"USH_{d}OperationRes");
                Check.Soft(PrereqIs(op, Root), $"{d} operation should still branch from the root");
                Check.Soft(op.baseCost == 400f, $"{d} operation costs {op.baseCost}, expected 400");
            }
            // Tier 2 behind advanced pathogens at 700.
            foreach (string d in new[] { "Malaria", "Scaria", "Plague" })
            {
                ResearchProjectDef op = Check.Def<ResearchProjectDef>($"USH_{d}OperationRes");
                Check.Soft(PrereqIs(op, Advanced), $"{d} operation should require {Advanced}");
                Check.Soft(op.baseCost == 700f, $"{d} operation costs {op.baseCost}, expected 700");
            }
            // Tier 3 behind lethal pathogens at 1000. FleshBreaker is always present; Necroa is a
            // MayRequire add-on disease, so it is only checked when its mod is loaded.
            ResearchProjectDef flesh = Check.Def<ResearchProjectDef>("USH_FleshBreakerOperationRes");
            Check.Soft(PrereqIs(flesh, Lethal), $"FleshBreaker operation should require {Lethal}");
            Check.Soft(flesh.baseCost == 1000f, $"FleshBreaker operation costs {flesh.baseCost}, expected 1000");

            ResearchProjectDef necroa = Check.Optional<ResearchProjectDef>(
                "USH_NecroaOperationRes", "biowarfare.researchtree", Ids.NecroaArchovirus);
            if (necroa != null)
            {
                Check.Soft(PrereqIs(necroa, Lethal), $"Necroa operation should require {Lethal}");
                Check.Soft(necroa.baseCost == 1000f, $"Necroa operation costs {necroa.baseCost}, expected 1000");
            }

            Check.SoftResult();
        }

        [Test]
        public static void VaccinesBranchFromAntigenAnalysisNotTheirWeapon()
        {
            if (!Check.Ready("biowarfare.researchtree", Ids.UshankaBioWarfare))
                return;

            // Always-present diseases.
            var costs = new Dictionary<string, float>
            {
                { "Flu", 800f }, { "SleepingSickness", 800f },
                { "Malaria", 1200f }, { "Scaria", 1200f }, { "Plague", 1200f },
            };
            foreach (KeyValuePair<string, float> kv in costs)
                CheckVaccine(Check.Def<ResearchProjectDef>($"USH_{kv.Key}VaccineRes"), kv.Key, kv.Value);

            // Necroa's vaccine only exists with its add-on mod.
            ResearchProjectDef necroa = Check.Optional<ResearchProjectDef>(
                "USH_NecroaVaccineRes", "biowarfare.researchtree", Ids.NecroaArchovirus);
            if (necroa != null)
                CheckVaccine(necroa, "Necroa", 1600f);

            ResearchProjectDef infection = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("USH_InfectionVaccineRes");
            if (infection != null)
                Check.Soft(PrereqIs(infection, Antigen), $"the generic infection vaccine should branch from {Antigen}");

            Check.SoftResult();
        }

        private static void CheckVaccine(ResearchProjectDef vac, string disease, float expectedCost)
        {
            Check.Soft(PrereqIs(vac, Antigen),
                $"{disease} vaccine still branches from its own weapon research instead of {Antigen} - offence and defence are not independent");
            Check.Soft(!RequiresAnyOperation(vac),
                $"{disease} vaccine still lists a weapon operation as a prerequisite");
            Check.Soft(vac.baseCost == expectedCost, $"{disease} vaccine costs {vac.baseCost}, expected {expectedCost}");
        }

        private static bool PrereqIs(ResearchProjectDef def, string prereqDefName)
        {
            if (def.prerequisites == null)
                return false;
            foreach (ResearchProjectDef p in def.prerequisites)
                if (p != null && p.defName == prereqDefName)
                    return true;
            return false;
        }

        private static bool RequiresAnyOperation(ResearchProjectDef def)
        {
            if (def.prerequisites == null)
                return false;
            foreach (ResearchProjectDef p in def.prerequisites)
                if (p != null && p.defName.StartsWith("USH_") && p.defName.EndsWith("OperationRes"))
                    return true;
            return false;
        }

        private static bool ThingResearchIs(ThingDef thing, string researchDefName)
        {
            if (thing.researchPrerequisites == null)
                return false;
            foreach (ResearchProjectDef p in thing.researchPrerequisites)
                if (p != null && p.defName == researchDefName)
                    return true;
            return false;
        }
    }
}
