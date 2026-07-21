using System.Collections.Generic;
using HarmonyLib;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class CoreTests
    {
        [Test]
        public static void InspirationsRespectPrecepts()
        {
            if (!Check.Ready("memes.inspirations"))
                return;
            InspirationNullifyingExtension frenzy = Check.Def<InspirationDef>("Frenzy_Shoot")
                .GetModExtension<InspirationNullifyingExtension>();
            Check.True(frenzy != null, "Frenzy_Shoot lacks InspirationNullifyingExtension");
            InspirationNullifyingExtension taming = Check.Def<InspirationDef>("Inspired_Taming")
                .GetModExtension<InspirationNullifyingExtension>();
            Check.True(taming != null, "Inspired_Taming lacks InspirationNullifyingExtension");
            if (ModsConfig.IsActive(Ids.VIEMemes))
            {
                Check.True(Check.AnyDefNamed(frenzy.nullifyingPrecepts, "VME_Violence_Abhorrent"),
                    "Frenzy_Shoot not nullified by VME_Violence_Abhorrent");
                Check.True(Check.AnyDefNamed(taming.nullifyingPrecepts, "VME_Ranching_Disliked"),
                    "Inspired_Taming not nullified by VME_Ranching_Disliked");
            }
            if (ModsConfig.IsActive(Ids.AlphaMemes))
                Check.True(Check.AnyDefNamed(frenzy.nullifyingPrecepts, "AM_CombatProwess_Melee"),
                    "Frenzy_Shoot not nullified by AM_CombatProwess_Melee");
            if (ModsConfig.IsActive(Ids.Ideology))
                Check.HarmonyPatched(AccessTools.Method(typeof(InspirationWorker), "InspirationCanOccur"),
                    "memes.inspirations");
        }

        [Test]
        public static void GenericFactionsKeepABaselinerFloor()
        {
            if (!Check.Ready("xenotypes.vanilla", Ids.Biotech))
                return;
            foreach (string name in new[] { "OutlanderCivil", "OutlanderRough", "Pirate" })
            {
                FactionDef faction = Check.Def<FactionDef>(name);
                float baseliner = Check.BaselinerShare(faction);
                Check.True(baseliner >= 0.35f,
                    $"{name} leaves only {baseliner:P1} for baseliners, below the 35% floor");
            }
        }

        [Test]
        public static void TribalFactionsGainAThematicRoster()
        {
            if (!Check.Ready("xenotypes.vanilla", Ids.Biotech))
                return;
            foreach (string name in new[] { "TribeCivil", "TribeRough", "TribeSavage" })
            {
                FactionDef faction = Check.Def<FactionDef>(name);
                Check.True(Check.HasXenotype(faction, "Neanderthal"), $"{name} lacks Neanderthal");
                float baseliner = Check.BaselinerShare(faction);
                Check.True(baseliner >= 0.6f,
                    $"{name} leaves only {baseliner:P1} for baseliners; tribals should stay mostly baseliner");
            }
        }

        [Test]
        public static void PredatoryXenotypesConcentrateInPirates()
        {
            if (!Check.Ready("xenotypes.vanilla", Ids.Biotech, Ids.Boglegs))
                return;
            FactionDef pirate = Check.Def<FactionDef>("Pirate");
            FactionDef civil = Check.Def<FactionDef>("OutlanderCivil");
            Check.True(Check.XenotypeChanceOf(pirate, "DV_Bogleg") > Check.XenotypeChanceOf(civil, "DV_Bogleg"),
                "boglegs must be commoner among pirates than in settled outlander towns");
            if (ModsConfig.IsActive(Ids.Buzzers))
                Check.True(!Check.HasXenotype(civil, "DV_Buzzer"), "OutlanderCivil still spawns buzzers");
        }

        [Test]
        public static void IndustrialXenotypesConcentrateInSettledOutlanders()
        {
            if (!Check.Ready("xenotypes.vanilla", Ids.Biotech, Ids.Halffoot))
                return;
            FactionDef civil = Check.Def<FactionDef>("OutlanderCivil");
            Check.True(Check.XenotypeChanceOf(civil, "DV_Halffoot") > 0f, "OutlanderCivil lacks half-foots");
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("Pirate"), "DV_Halffoot"),
                "Pirate still spawns half-foots");
        }

        [Test]
        public static void InjectedDefsAreAttributedToThisMod()
        {
            // Defs added by PatchOperationAdd have no source asset, so the game files them under
            // patchedDefs without a modContentPack. Research tree UIs badge each project with its
            // source mod off that field, so ours would render blank next to everyone else's.
            // DefAttribution claims them back; this catches any that slip through.
            var unattributed = new List<string>();
            foreach (Def def in LoadedModManager.PatchedDefsForReading)
            {
                if (def.defName != null && def.defName.StartsWith("RBP_") && def.modContentPack == null)
                    unattributed.Add(def.defName);
            }
            Check.True(unattributed.Count == 0,
                $"{unattributed.Count} injected defs have no source mod: {string.Join(", ", unattributed.ToArray())}");

            // And the ones that were claimed must point at us, not at whoever loaded last.
            ResearchTabDef tab = Check.Optional<ResearchTabDef>("RBP_GeneticsTab", "geneticsresearch.core");
            if (tab != null)
                Check.Eq(tab.modContentPack?.PackageId, "encoded.rebalancepatches", "RBP_GeneticsTab.modContentPack");
        }
    }
}
