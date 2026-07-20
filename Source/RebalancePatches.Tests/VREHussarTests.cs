using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREHussarTests
    {
        [Test]
        public static void DedupLosersRemoved()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.VREHussar))
                return;
            Check.GenesGone("VREH_Giant");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.VREHussar))
                return;
            Check.XenoGene("VREH_Uhlan", "BS_LargeFrame");
        }

        [Test]
        public static void AptitudesConsolidated()
        {
            if (!Check.Ready("genetics.hussaraptitudes", Ids.VREHussar))
                return;
            System.Type templateType = GenTypes.GetTypeInAnyAssembly("VREHussars.WeaponGeneTemplateDef");
            Check.True(templateType != null, "VREHussars.WeaponGeneTemplateDef type missing - VRE-Hussar reworked its generator");
            Check.True(GenDefDatabase.GetDefSilentFail(templateType, "VREHT_WeaponAptitude") == null,
                "VREHT_WeaponAptitude template still present - per-weapon genes still generate");
            foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefsListForReading)
                Check.True(!gene.defName.StartsWith("VREHT_WeaponAptitude_") || gene.defName == "VREHT_WeaponAptitude_Randomizer",
                    $"generated per-weapon aptitude survived: {gene.defName}");
            Check.Def<GeneDef>("VREHT_WeaponAptitude_Randomizer");
            foreach (string name in new[] { "RBP_VREHT_Aptitude_LightMelee", "RBP_VREHT_Aptitude_HeavyMelee",
                "RBP_VREHT_Aptitude_LightRanged", "RBP_VREHT_Aptitude_HeavyRanged" })
            {
                GeneDef gene = Check.Def<GeneDef>(name);
                Check.GeneTag(gene, "VREH_WeaponAptitude");
                Check.True(gene.conditionalStatAffecters != null && gene.conditionalStatAffecters.Count == 1
                    && gene.conditionalStatAffecters[0] is ConditionalStatAffecter_WeaponClass,
                    $"{name} lacks the weapon-class stat affecter");
            }
            if (ModsConfig.IsActive(Ids.GeneNodes))
                Check.Def<ThingDef>("RBP_GET_WeaponAptitude");
        }

        [Test]
        public static void AptitudeClassificationAndFactors()
        {
            if (!Check.Ready("genetics.hussaraptitudes", Ids.VREHussar))
                return;
            // The affecter classifies by the engine's melee/ranged flag plus mass >= 3 kg.
            Check.True(!ConditionalStatAffecter_WeaponClass.IsHeavy(Check.Def<ThingDef>("MeleeWeapon_Knife")), "knife (0.5 kg) classified heavy");
            Check.True(!ConditionalStatAffecter_WeaponClass.IsHeavy(Check.Def<ThingDef>("MeleeWeapon_LongSword")), "longsword (2 kg) classified heavy");
            Check.True(!ConditionalStatAffecter_WeaponClass.IsHeavy(Check.Def<ThingDef>("Gun_Autopistol")), "autopistol (1.2 kg) classified heavy");
            Check.True(ConditionalStatAffecter_WeaponClass.IsHeavy(Check.Def<ThingDef>("Gun_ChargeLance")), "charge lance (8 kg) classified light");
            Check.True(ConditionalStatAffecter_WeaponClass.IsHeavy(Check.Def<ThingDef>("Gun_Minigun")), "minigun (10 kg) classified light");
            AptitudeAffecter("RBP_VREHT_Aptitude_LightMelee", true, false, "MeleeHitChance");
            AptitudeAffecter("RBP_VREHT_Aptitude_HeavyMelee", true, true, "MeleeHitChance");
            AptitudeAffecter("RBP_VREHT_Aptitude_LightRanged", false, false, "ShootingAccuracyPawn");
            AptitudeAffecter("RBP_VREHT_Aptitude_HeavyRanged", false, true, "ShootingAccuracyPawn");
        }

        private static void AptitudeAffecter(string geneName, bool melee, bool heavy, string statDefName)
        {
            GeneDef gene = Check.Def<GeneDef>(geneName);
            var affecter = gene.conditionalStatAffecters?[0] as ConditionalStatAffecter_WeaponClass;
            Check.True(affecter != null, $"{geneName} lacks ConditionalStatAffecter_WeaponClass");
            Check.Eq(affecter.melee, melee, $"{geneName} melee flag");
            Check.Eq(affecter.heavy, heavy, $"{geneName} heavy flag");
            Check.Eq(Check.StatModifierValue(affecter.statFactors, statDefName), 1.5f, $"{geneName} {statDefName} factor");
        }
    }
}