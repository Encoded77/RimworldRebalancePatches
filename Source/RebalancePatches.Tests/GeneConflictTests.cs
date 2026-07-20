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
                // WVC_Undead is removed by genetics.wvcdupes; only assert it when it survived.
                GeneDef undead = Check.Optional<GeneDef>("WVC_Undead", "geneconflicts.deathless");
                if (undead != null)
                    Check.GeneTag(undead, "RBP_Deathless");
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
            // RSBornWarrior is removed by genetics.dedup; only assert it when it survived.
            GeneDef bornWarrior = Check.Optional<GeneDef>("RSBornWarrior", "geneconflicts.dodge");
            if (bornWarrior != null)
                Check.GeneTag(bornWarrior, "MeleeDodge");
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

        [Test]
        public static void ClawsAlphaGenes()
        {
            if (!Check.Ready("geneconflicts.claws", Ids.AlphaGenes))
                return;
            Check.GeneTag(Check.Def<GeneDef>("AG_ClawedHands"), "RBP_Claws");
            Check.GeneTag(Check.Def<GeneDef>("AG_CrabClaw"), "RBP_Claws");
            GeneDef pneumatic = Check.Optional<GeneDef>("AG_VFEI_PneumaticClaw", "geneconflicts.claws");
            if (pneumatic != null)
                Check.GeneTag(pneumatic, "RBP_Claws");
        }

        [Test]
        public static void ClawsWVC()
        {
            if (!Check.Ready("geneconflicts.claws", Ids.WVC))
                return;
            Check.GeneTag(Check.Def<GeneDef>("WVC_NaturalBodyParts_Claws"), "RBP_Claws");
            Check.GeneTag(Check.Def<GeneDef>("WVC_MecaBodyParts_Claws"), "RBP_Claws");
        }

        [Test]
        public static void ClawsBigSmall()
        {
            if (!Check.Ready("geneconflicts.claws", Ids.BigSmallCore))
                return;
            Check.GeneTag(Check.Def<GeneDef>("LoS_VenomTalons"), "RBP_Claws");
        }

        [Test]
        public static void ClawsVRESaurid()
        {
            if (!Check.Ready("geneconflicts.claws", Ids.VRESaurid))
                return;
            Check.GeneTag(Check.Def<GeneDef>("VRESaurids_SauridClaws"), "RBP_Claws");
        }

        [Test]
        public static void ClawsVRESanguophage()
        {
            if (!Check.Ready("geneconflicts.claws", Ids.VRESanguophage))
                return;
            Check.GeneTag(Check.Def<GeneDef>("VRE_Talons"), "RBP_Claws");
        }

        [Test]
        public static void ClawsVREInsector()
        {
            if (!Check.Ready("geneconflicts.claws", Ids.VREInsector))
                return;
            Check.GeneTag(Check.Def<GeneDef>("VRE_ChargerClaws"), "RBP_Claws");
        }

        [Test]
        public static void ClawsVQEAncients()
        {
            if (!Check.Ready("geneconflicts.claws", Ids.VQEAncients))
                return;
            Check.GeneTag(Check.Def<GeneDef>("VQEA_PlasteelClaws"), "RBP_Claws");
        }

        [Test]
        public static void SlowBleedingVsHemophiliac()
        {
            if (!Check.Ready("geneconflicts.bleedrate", Ids.BigSmallCore))
                return;
            Check.GeneTag(Check.Def<GeneDef>("BS_SlowBleeding"), "RBP_BleedRate");
            if (ModsConfig.IsActive(Ids.VREGenie))
                Check.GeneTag(Check.Def<GeneDef>("VRE_Hemophiliac"), "RBP_BleedRate");
        }

        [Test]
        public static void FlirtyVsNotFlirty()
        {
            if (!Check.Ready("geneconflicts.flirty", Ids.BigSmallCore))
                return;
            Check.GeneTag(Check.Def<GeneDef>("BS_NotFlirty"), "RBP_Flirt");
            if (ModsConfig.IsActive(Ids.VREHighmate))
                Check.GeneTag(Check.Def<GeneDef>("VRE_Flirty"), "RBP_Flirt");
        }

        [Test]
        public static void MeleeSpeedBrawnum()
        {
            if (!Check.Ready("geneconflicts.meleespeed", Ids.Brawnum))
                return;
            Check.GeneTag(Check.Def<GeneDef>("DV_Melee_Slow"), "MeleeAttackSpeed");
        }
    }
}
