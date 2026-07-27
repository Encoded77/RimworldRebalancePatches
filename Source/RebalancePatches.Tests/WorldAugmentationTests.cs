using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RebalancePatches.Mods.PawnGeneration;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class WorldAugmentationTests
    {
        [Test]
        public static void TiersDescendByTechLevelAndGateCyberbrains()
        {
            WorldAugmentationPatches.Tier[] plain = WorldAugmentationPatches.BuildTiers(false);
            WorldAugmentationPatches.Tier[] withBrains = WorldAugmentationPatches.BuildTiers(true);

            // Only the three high tiers exist; tribal/medieval must never get a tier.
            Check.Soft(plain.All(t => t.techLevel == TechLevel.Ultra || t.techLevel == TechLevel.Spacer
                    || t.techLevel == TechLevel.Industrial),
                "a tier below Industrial exists - tribal and medieval pawns must never be augmented");
            Check.Soft(!plain.Any(t => t.techLevel == TechLevel.Medieval || t.techLevel == TechLevel.Neolithic),
                "a medieval or neolithic tier was defined");

            float u = ChanceOf(plain, TechLevel.Ultra), s = ChanceOf(plain, TechLevel.Spacer), i = ChanceOf(plain, TechLevel.Industrial);
            Check.Soft(u > s && s > i && i > 0f,
                $"tier chances do not descend by tech (ultra {u}, spacer {s}, industrial {i})");

            // Cyberbrains: off everywhere without the toggle; common for spacer and both for ultra with it.
            Check.Soft(plain.All(t => !t.tags.Contains("GiTS_TechHediff_CyberbrainCommon")
                    && !t.tags.Contains("GiTS_TechHediff_CyberbrainAdvanced")),
                "cyberbrain tags present although cyberbrains are off");
            WorldAugmentationPatches.Tier bU = withBrains.First(t => t.techLevel == TechLevel.Ultra);
            WorldAugmentationPatches.Tier bS = withBrains.First(t => t.techLevel == TechLevel.Spacer);
            WorldAugmentationPatches.Tier bI = withBrains.First(t => t.techLevel == TechLevel.Industrial);
            Check.Soft(bU.tags.Contains("GiTS_TechHediff_CyberbrainCommon") && bU.tags.Contains("GiTS_TechHediff_CyberbrainAdvanced"),
                "ultra tier lacks both cyberbrain tags when cyberbrains are on");
            Check.Soft(bS.tags.Contains("GiTS_TechHediff_CyberbrainCommon") && !bS.tags.Contains("GiTS_TechHediff_CyberbrainAdvanced"),
                "spacer tier should carry the common cyberbrain tag only");
            Check.Soft(!bI.tags.Contains("GiTS_TechHediff_CyberbrainCommon"),
                "industrial tier should never carry cyberbrains");

            Check.SoftResult();
        }

        [Test]
        public static void ReachablePoolIsAdvancedNonWeaponImplantsOnly()
        {
            if (!Check.Ready("worldaugment.bionics"))
                return;

            // Applying the patch also captures the tier config the pool test relies on.
            Check.HarmonyPatched(
                AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                    new[] { typeof(PawnGenerationRequest) }),
                "world augmentation");

            List<ThingDef> pool = WorldAugmentationPatches.ReachablePool();
            Check.Note($"reachable pool holds {pool.Count} implant(s)");
            if (!Check.Soft(pool.Count > 0, "the reachable pool is empty - even vanilla bionics are missing"))
            {
                Check.SoftResult();
                return;
            }

            var reachTags = new HashSet<string> { "Advanced", "GiTS_TechHediff_CyberbrainCommon", "GiTS_TechHediff_CyberbrainAdvanced" };
            foreach (ThingDef def in pool)
            {
                Check.Soft(def.isTechHediff, $"{def.defName} is in the pool but is not a tech hediff");
                Check.Soft(!def.violentTechHediff, $"{def.defName} is a weapon implant and must be excluded");
                Check.Soft(def.techHediffsTags != null && def.techHediffsTags.Any(reachTags.Contains),
                    $"{def.defName} carries no reachable tag");
            }

            ThingDef bionicArm = DefDatabase<ThingDef>.GetNamedSilentFail("BionicArm");
            if (bionicArm != null)
                Check.Soft(pool.Contains(bionicArm), "the vanilla bionic arm is not in the reachable pool");

            // Archotech parts carry no techHediffsTags, so they must never leak in.
            foreach (string name in new[] { "ArchotechArm", "ArchotechLeg", "ArchotechEye" })
            {
                ThingDef arch = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (arch != null)
                    Check.Soft(!pool.Contains(arch), $"{name} leaked into the augmentation pool");
            }

            Check.SoftResult();
        }

        [Test]
        public static void InstallsAReachableImplantOnAGeneratedPawn()
        {
            if (!Check.Ready("worldaugment.bionics"))
                return;

            Pawn pawn = MakeAdult();
            if (!Check.Soft(pawn?.health != null, "could not generate a test pawn"))
            {
                Discard(pawn);
                Check.SoftResult();
                return;
            }
            try
            {
                ThingDef part = null;
                foreach (ThingDef def in WorldAugmentationPatches.ReachablePool())
                    if (WorldAugmentationPatches.CanInstall(pawn, def))
                    {
                        part = def;
                        break;
                    }
                if (part == null)
                {
                    Check.Note("no reachable implant could be installed on the test pawn; install path unexercised");
                    Check.SoftResult();
                    return;
                }

                int before = pawn.health.hediffSet.hediffs.Count;
                bool ok = WorldAugmentationPatches.TryInstall(pawn, part);
                Check.Soft(ok, $"TryInstall reported failure for '{part.defName}' although CanInstall passed");
                Check.Soft(pawn.health.hediffSet.hediffs.Count > before,
                    $"installing '{part.defName}' added no hediff to the pawn");
            }
            finally
            {
                Discard(pawn);
            }
            Check.SoftResult();
        }

        private static float ChanceOf(WorldAugmentationPatches.Tier[] tiers, TechLevel level) =>
            tiers.First(t => t.techLevel == level).chance;

        private static Pawn MakeAdult()
        {
            try
            {
                return PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    allowAddictions: false, allowFood: false,
                    developmentalStages: DevelopmentalStage.Adult));
            }
            catch (Exception e)
            {
                Log.Warning("[RBP Tests] could not generate a test pawn: " + e.Message);
                return null;
            }
        }

        private static void Discard(Pawn pawn)
        {
            try
            {
                if (pawn != null && !pawn.Destroyed)
                    pawn.Destroy();
            }
            catch { }
        }
    }
}
