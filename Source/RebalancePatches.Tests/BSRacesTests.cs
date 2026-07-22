using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSRacesTests
    {
        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSRaces))
                return;
            Check.XenoGene("BS_BrokenTitan", "AG_ColdImmunity");
            Check.XenoGene("BS_FireJotun", "AG_HeatImmunity");
            Check.XenoGene("BS_FleshGolemServant", "AG_ArmourMedium");
            Check.XenoGene("BS_FrostJotun", "AG_ColdImmunity");
            Check.XenoGene("BS_Hearthdoll", "AG_ArmourMedium");
            Check.XenoGene("BS_Jotun", "AG_ArmourMedium");
            Check.XenoGene("BS_PilotableFleshGolem", "AG_ArmourMedium");
            Check.XenoGene("BS_Redcap", "AG_ArmourMedium");
            Check.XenoGene("BS_Surtr", "AG_ArmourMedium");
            Check.XenoGene("BS_Surtr", "AG_HeatImmunity");
            Check.XenoGene("BS_Svartalf", "AG_ArmourMedium");
            Check.XenoGene("BS_Troll", "AG_ArmourMedium");
            Check.XenoGene("BS_TrollAdult", "AG_ArmourMedium");
            Check.XenoGene("BS_Ymir", "AG_ArmourMedium");
            Check.XenoGene("BS_Ymir", "AG_ColdImmunity");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("BS_Dwarf", "VRE_SlowAging");
                Check.XenoGene("BS_FireJotun", "VRE_SlowAging");
                Check.XenoGene("BS_FrostJotun", "VRE_SlowAging");
                Check.XenoGene("BS_Gnome", "VRE_SlowAging");
                Check.XenoGene("BS_GreatOgre", "VRE_SlowAging");
                Check.XenoGene("BS_Half_Jotun", "VRE_SlowAging");
                Check.XenoGene("BS_Redcap", "VRE_FastAging");
                Check.XenoGene("BS_Surtr", "VRE_SlowAging");
                Check.XenoGene("BS_Svartalf", "VRE_SlowAging");
                Check.XenoGene("BS_Ymir", "VRE_SlowAging");
            }
            if (ModsConfig.IsActive(Ids.VREArchon))
            {
                Check.XenoGene("BS_Corrupterd_Titan", "VRE_ShortPregnancy");
                Check.XenoGene("BS_Gnome", "VRE_ShortPregnancy");
                Check.XenoGene("BS_Redcap", "VRE_ShortPregnancy");
            }
            if (ModsConfig.IsActive(Ids.VREWaster))
            {
                Check.XenoGene("BS_BrokenTitan", "VRE_Instability_Extreme");
                Check.XenoGene("BS_Corrupterd_Titan", "VRE_Instability_Extreme");
            }
            if (ModsConfig.IsActive(Ids.VREStarjack))
            {
                Check.XenoGene("BS_BrokenTitan", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Surtr", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Ymir", "VREStarjack_VacuumResistance_Total");
            }
        }

        [Test]
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.bsraces", Ids.BSRaces))
                return;
            Check.Eq(Check.Def<XenotypeDef>("BS_Jotun").label, "gigant", "BS_Jotun label");
            Check.Eq(Check.Def<XenotypeDef>("BS_FrostJotun").label, "cryogigant", "BS_FrostJotun label");
            Check.Eq(Check.Def<XenotypeDef>("BS_FireJotun").label, "pyrogigant", "BS_FireJotun label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Surtr").label, "pyrogigant prime", "BS_Surtr label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Ymir").label, "cryogigant prime", "BS_Ymir label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Half_Jotun").label, "half-gigant", "BS_Half_Jotun label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Ogre").label, "hulker", "BS_Ogre label");
            Check.Eq(Check.Def<XenotypeDef>("BS_GreatOgre").label, "great hulker", "BS_GreatOgre label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Dwarf").label, "deepkin", "BS_Dwarf label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Gnome").label, "minikin", "BS_Gnome label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Svartalf").label, "umbrakin", "BS_Svartalf label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Redcap").label, "scrapper", "BS_Redcap label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Hearthguard").label, "warden synth", "BS_Hearthguard label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Hearthdoll").label, "service synth", "BS_Hearthdoll label");
            Check.Eq(Check.Def<XenotypeDef>("BS_PilotableFleshGolem").label, "bioconstruct", "BS_PilotableFleshGolem label");
            Check.Eq(Check.Def<XenotypeDef>("BS_FleshGolemServant").label, "bioconstruct servant", "BS_FleshGolemServant label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Troll").label, "juvenile regenerant", "BS_Troll label");
            Check.Eq(Check.Def<XenotypeDef>("BS_TrollAdult").label, "regenerant", "BS_TrollAdult label");
            Check.Eq(Check.Def<XenotypeDef>("BS_TrollOld").label, "elder regenerant", "BS_TrollOld label");
            Check.True(!Check.Def<XenotypeDef>("BS_Jotun").description.Contains("Jotun"), "BS_Jotun description mentions no jotun");

            FactionDef muspel = Check.Def<FactionDef>("BS_Muspelheim");
            Check.Eq(muspel.label, "Cinderhold Dominion", "BS_Muspelheim label");
            Check.Eq(muspel.fixedName, "The Cinderholds", "BS_Muspelheim fixedName");
            FactionDef nifl = Check.Def<FactionDef>("BS_Niflheim");
            Check.Eq(nifl.label, "Permafrost Clans", "BS_Niflheim label");
            Check.Eq(nifl.fixedName, "The Permafrost Clans", "BS_Niflheim fixedName");
            FactionDef ogres = Check.Def<FactionDef>("BS_OgreFaction");
            Check.Eq(ogres.label, "hulker tribe", "BS_OgreFaction label");
            Check.Eq(ogres.fixedName, "Hulker Tribes", "BS_OgreFaction fixedName");
            FactionDef little = Check.Def<FactionDef>("BS_LittlePeople");
            Check.Eq(little.label, "Minikin Union", "BS_LittlePeople label");
            Check.Eq(little.pawnSingular, "minikin", "BS_LittlePeople pawnSingular");
            Check.Eq(Check.Def<FactionDef>("BS_Dvergr_Medieval_Union").label, "Deepkin Trade Combine", "BS_Dvergr_Medieval_Union label");

            Check.Eq(Check.Def<PawnKindDef>("BS_Jotun_Knight").label, "cinderhold warrior", "BS_Jotun_Knight label");
            Check.Eq(Check.Def<PawnKindDef>("BS_Jotun_Berserker").label, "elite berserker", "BS_Jotun_Berserker label");
            Check.Eq(Check.Def<PawnKindDef>("BS_Ogre_Chieftain").label, "hulker chef", "BS_Ogre_Chieftain label");
            Check.Eq(Check.Def<PawnKindDef>("BS_Troll_Raider_Adult").label, "regenerant raider", "BS_Troll_Raider_Adult label");
            Check.Eq(Check.Def<PawnKindDef>("BS_FleshGolemWarrior").label, "bioconstruct", "BS_FleshGolemWarrior label");
            // The mecha jotuns load only with the Simple Androids addon, per BS Races' own LoadFolders.
            PawnKindDef mechaRanged = Check.Optional<PawnKindDef>("BS_MechaJotunRanged", "scifinames.bsraces", Ids.BSSimpleAndroids);
            if (mechaRanged != null)
                Check.Eq(mechaRanged.label, "mecha-gigant", "BS_MechaJotunRanged label");
            PawnKindDef mechaMelee = Check.Optional<PawnKindDef>("BS_MechaJotunMelee", "scifinames.bsraces", Ids.BSSimpleAndroids);
            if (mechaMelee != null)
                Check.Eq(mechaMelee.label, "mecha-gigant", "BS_MechaJotunMelee label");
        }

        [Test]
        public static void DeathlikeReplaced()
        {
            if (!Check.Ready("genetics.bsdupes", Ids.BigSmallCore, Ids.CherryPicker, Ids.BSRaces))
                return;
            Check.XenoGene("BS_FleshGolemServant", "BS_LesserDeathless");
            Check.XenoGene("BS_PilotableFleshGolem", "BS_LesserDeathless");
        }
    }
}

