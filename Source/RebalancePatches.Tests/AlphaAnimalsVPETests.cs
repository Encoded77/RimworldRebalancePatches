using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AlphaAnimalsVPETests
    {
        [Test]
        public static void HeraldPathIsGeneGated() =>
            PathGenes.PathGated(Ids.AlphaAnimals, "RBP_Gene_HeraldOfTheBlackHive", "AAVPE_HeraldOfTheBlackHive");

        [Test]
        public static void OculistPathIsGeneGated() =>
            PathGenes.PathGated(Ids.AlphaAnimals, "RBP_Gene_Oculist", "AAVPE_Oculist");

        [Test]
        public static void RavagerPathIsGeneGated() =>
            PathGenes.PathGated(Ids.AlphaAnimals, "RBP_Gene_Ravager", "AAVPE_Ravager");

        [Test]
        public static void XenoseerPathIsGeneGated() =>
            PathGenes.PathGated(Ids.AlphaAnimals, "RBP_Gene_Xenoseer", "AAVPE_Xenoseer");

        [Test]
        public static void HeraldGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.AlphaAnimals, "RBP_Gene_HeraldOfTheBlackHive",
                Ids.AlphaGenes, "AG_Hiveling",
                Ids.Biotech, "Waster",
                Ids.BSMoreXenos, "BS_HiveQueen");

        [Test]
        public static void OculistGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.AlphaAnimals, "RBP_Gene_Oculist",
                Ids.AlphaGenes, "VRE_Ocularkin",
                Ids.WVC, "WVC_Beholdkind",
                Ids.WVC, "WVC_Fleshkind");

        [Test]
        public static void RavagerGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.AlphaAnimals, "RBP_Gene_Ravager",
                Ids.Biotech, "Hussar",
                Ids.VREHussar, "VREH_Uhlan",
                Ids.Biotech, "Yttakin",
                Ids.VRELycanthrope, "VRE_Lycan",
                Ids.VRELycanthrope, "VRE_Wolfman",
                Ids.AlphaGenes, "AG_Drakonori",
                Ids.BSYokai, "BS_RedOni",
                Ids.BSYokai, "BS_GreatRedOni",
                Ids.BSYokai, "BS_BlueOni",
                Ids.BSYokai, "BS_GreatBlueOni",
                Ids.BSYokai, "BS_LesserOni",
                Ids.BSRaces, "BS_Redcap",
                Ids.BSRaces, "BS_Corrupterd_Titan",
                Ids.BSRaces, "BS_Ogre",
                Ids.BSRaces, "BS_GreatOgre",
                Ids.BSLamias, "LoS_Anacondaman",
                Ids.BSLamias, "LoS_Snakeman",
                Ids.BSMoreXenos, "BS_Abomination",
                Ids.BSHeaven, "BS_Glutton",
                Ids.BSHeaven, "BS_LilGlutton",
                Ids.WVC, "WVC_Ripperkind");

        [Test]
        public static void XenoseerGeneOnThematicXenotypes() =>
            PathGenes.XenosCarry(Ids.AlphaAnimals, "RBP_Gene_Xenoseer",
                Ids.Biotech, "Genie",
                Ids.AlphaGenes, "AG_Animusen",
                Ids.AlphaGenes, "VRE_Ocularkin",
                Ids.AlphaGenes, "AG_Fleetkind",
                Ids.BSHeaven, "BS_Grigori",
                Ids.Venators, "DV_Venator");
    }
}
