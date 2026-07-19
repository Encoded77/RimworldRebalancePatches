using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class GeneConflictTests
    {
        [Test]
        public static void BloodlustVsDistressed()
        {
            if (!Check.Ready("geneconflicts.bloodlust", Ids.BigSmallCore, Ids.VREHighmate))
                return;
            Check.GeneTag(Check.Def<GeneDef>("VRE_Distressed"), "RBP_BloodlustDistressed");
            GeneDef bloodlust = Check.Optional<GeneDef>("BS_Bloodlust", "geneconflicts.bloodlust");
            if (bloodlust != null)
                Check.GeneTag(bloodlust, "RBP_BloodlustDistressed");
        }

        [Test]
        public static void PsychicSensitivityVsDullDeaf()
        {
            if (!Check.Ready("geneconflicts.psychic", Ids.WVC, Ids.Biotech))
                return;
            foreach (string gene in new[] { "PsychicAbility_Dull", "PsychicAbility_Deaf" })
            {
                GeneDef def = Check.Def<GeneDef>(gene);
                Check.GeneTag(def, "WVC_PsychicSunSensitivityDeaf");
                Check.GeneTag(def, "WVC_PsychicMoonSensitivityDeaf");
            }
        }

        [Test]
        public static void FirefoamPopVsFireObsession()
        {
            if (!Check.Ready("geneconflicts.firefoam", Ids.WVC, Ids.AlphaGenes))
                return;
            Check.GeneTag(Check.Def<GeneDef>("WVC_FirefoampopMech"), "RBP_FirefoamPyromaniac");
            Check.GeneTag(Check.Def<GeneDef>("AG_FireObsession"), "RBP_FirefoamPyromaniac");
        }

        [Test]
        public static void HemogenDrainNoStacking()
        {
            if (!Check.Ready("geneconflicts.hemogen", Ids.Biotech))
                return;
            Check.GeneTag(Check.Def<GeneDef>("HemogenDrain"), "HemogenDrain");
            if (ModsConfig.IsActive(Ids.WVC))
                Check.GeneTag(Check.Def<GeneDef>("WVC_HemogenGain"), "HemogenDrain");
            if (ModsConfig.IsActive(Ids.BigSmallCore))
            {
                GeneDef greaterDrain = Check.Optional<GeneDef>("VU_GreaterHemogenDrain", "geneconflicts.hemogen");
                if (greaterDrain != null)
                    Check.GeneTag(greaterDrain, "HemogenDrain");
            }
        }

        [Test]
        public static void DeathlessGenesMutuallyExclusive()
        {
            if (!Check.Ready("geneconflicts.deathless", Ids.Biotech))
                return;
            Check.GeneTag(Check.Def<GeneDef>("Deathless"), "RBP_Deathless");
            if (ModsConfig.IsActive(Ids.WVC))
            {
                Check.GeneTag(Check.Def<GeneDef>("WVC_Undead"), "RBP_Deathless");
                Check.GeneTag(Check.Def<GeneDef>("WVC_NeverDead"), "RBP_Deathless");
            }
            if (ModsConfig.IsActive(Ids.VREArchon))
                Check.GeneTag(Check.Def<GeneDef>("VRE_Transcendent"), "RBP_Deathless");
            if (ModsConfig.IsActive(Ids.BigSmallCore))
            {
                foreach (string gene in new[] { "BS_ReturningSoul", "BS_Immortal" })
                {
                    GeneDef def = Check.Optional<GeneDef>(gene, "geneconflicts.deathless");
                    if (def != null)
                        Check.GeneTag(def, "RBP_Deathless");
                }
            }
        }

        [Test]
        public static void DodgeVQEAncients()
        {
            if (!Check.Ready("geneconflicts.dodge", Ids.VQEAncients))
                return;
            Check.GeneTag(Check.Def<GeneDef>("VQEA_Prowess"), "MeleeDodge");
        }

        [Test]
        public static void DodgeRimsenalHarana()
        {
            if (!Check.Ready("geneconflicts.dodge", Ids.RimsenalHarana))
                return;
            Check.GeneTag(Check.Def<GeneDef>("AgileStriker"), "MeleeDodge");
        }

        [Test]
        public static void DodgeRimsenalAskbarn()
        {
            if (!Check.Ready("geneconflicts.dodge", Ids.RimsenalAskbarn))
                return;
            Check.GeneTag(Check.Def<GeneDef>("RSLightningReflexes"), "MeleeDodge");
            Check.GeneTag(Check.Def<GeneDef>("RSBornWarrior"), "MeleeDodge");
        }

        [Test]
        public static void DodgeKeshig()
        {
            if (!Check.Ready("geneconflicts.dodge", Ids.Keshig))
                return;
            Check.GeneTag(Check.Def<GeneDef>("DV_DodgeChance_High"), "MeleeDodge");
            Check.GeneTag(Check.Def<GeneDef>("DV_DodgeChance_Low"), "MeleeDodge");
        }

        [Test]
        public static void DodgeHighborn()
        {
            if (!Check.Ready("geneconflicts.dodge", Ids.Highborn))
                return;
            Check.GeneTag(Check.Def<GeneDef>("HBX_Fencer"), "MeleeDodge");
        }
    }
}
