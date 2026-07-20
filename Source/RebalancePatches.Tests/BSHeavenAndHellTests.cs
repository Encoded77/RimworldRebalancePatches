using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSHeavenAndHellTests
    {
        [Test]
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.heaven", Ids.BSHeaven))
                return;
            Check.Eq(Check.Def<XenotypeDef>("BS_Authority").label, "ascendant prime", "BS_Authority label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Malakim").label, "ascendant", "BS_Malakim label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Satan").label, "adversary prime", "BS_Satan label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Grigori").label, "watcher", "BS_Grigori label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Glutton").label, "devourer", "BS_Glutton label");
            Check.Eq(Check.Def<XenotypeDef>("BS_LilGlutton").label, "lesser devourer", "BS_LilGlutton label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Nephilim").label, "halfwrought", "BS_Nephilim label");
            Check.Eq(Check.Def<XenotypeDef>("BS_Lilim").label, "nightwrought", "BS_Lilim label");

            FactionDef heaven = Check.Def<FactionDef>("BS_Heaven");
            Check.Eq(heaven.label, "Luminal Ascendancy", "BS_Heaven label");
            Check.Eq(heaven.fixedName, "The Luminal Ascendancy", "BS_Heaven fixedName");
            Check.Eq(heaven.pawnSingular, "emissary", "BS_Heaven pawnSingular");
            FactionDef hell = Check.Def<FactionDef>("BS_Hell");
            Check.Eq(hell.label, "Abyssal Dominion", "BS_Hell label");
            Check.Eq(hell.fixedName, "The Abyssal Dominion", "BS_Hell fixedName");
            Check.Eq(hell.pawnSingular, "abyssal", "BS_Hell pawnSingular");
            FactionDef outcasts = Check.Def<FactionDef>("BS_Outcasts");
            Check.Eq(outcasts.label, "The Exiles", "BS_Outcasts label");
            Check.Eq(outcasts.pawnSingular, "exile", "BS_Outcasts pawnSingular");

            Check.Eq(Check.Def<PawnKindDef>("BS_ServitorBasic").label, "ascendant servitor", "BS_ServitorBasic label");
            Check.Eq(Check.Def<PawnKindDef>("BS_Satan").label, "adversary", "BS_Satan pawnkind label");
            Check.Eq(Check.Def<PawnKindDef>("BS_AuthorityWarrior").label, "ascendant warrior", "BS_AuthorityWarrior label");
            Check.Eq(Check.Def<PawnKindDef>("BS_AuthorityLord").label, "high ascendant", "BS_AuthorityLord label");
            Check.Eq(Check.Def<PawnKindDef>("BS_Metatron").label, "voice of the archotech", "BS_Metatron label");
            Check.Eq(Check.Def<PawnKindDef>("BS_BigGlutton").label, "devourer", "BS_BigGlutton label");
            Check.Eq(Check.Def<PawnKindDef>("VU_DemonWarrior").label, "abyssal guard", "VU_DemonWarrior label");
            Check.Eq(Check.Def<PawnKindDef>("VU_DemonPrince").label, "abyssal prince", "VU_DemonPrince label");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genetics.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BSHeaven))
                return;
            Check.XenoGene("BS_Authority", "AG_HeatImmunity");
            Check.XenoGene("BS_Glutton", "AG_ArmourMedium");
            Check.XenoGene("BS_Grigori", "AG_ColdImmunity");
            Check.XenoGene("BS_LilGlutton", "AG_ArmourMedium");
            Check.XenoGene("BS_Malakim", "AG_ColdImmunity");
            Check.XenoGene("BS_Nephilim", "AG_ArmourMedium");
            Check.XenoGene("BS_Satan", "AG_ArmourMedium");
            Check.XenoGene("BS_Satan", "AG_ColdImmunity");
            if (ModsConfig.IsActive(Ids.VREPigskin))
                Check.XenoGene("BS_Nephilim", "VRE_SlowAging");
            if (ModsConfig.IsActive(Ids.VREHighmate))
            {
                Check.XenoGene("BS_Grigori", "VRE_Flirty");
                Check.XenoGene("BS_Lilim", "VRE_Flirty");
                Check.XenoGene("BS_Lilim", "VRE_Libido_VeryHigh");
            }
            if (ModsConfig.IsActive(Ids.VRESanguophage))
            {
                Check.XenoGene("BS_Lilim", "VRE_Talons");
                Check.XenoGene("BS_Satan", "VRE_Talons");
            }
            if (ModsConfig.IsActive(Ids.VREHussar))
                Check.XenoGene("BS_Satan", "VREH_Arrogant");
            if (ModsConfig.IsActive(Ids.VREStarjack))
            {
                Check.XenoGene("BS_Authority", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Grigori", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Malakim", "VREStarjack_VacuumResistance_Total");
                Check.XenoGene("BS_Satan", "VREStarjack_VacuumResistance_Total");
            }
        }
    }
}

