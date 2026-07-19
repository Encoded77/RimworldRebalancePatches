using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BSSlimesTests
    {
        [Test]
        public static void SciFiNames()
        {
            if (!Check.Ready("scifinames.slimes", Ids.BigSmallSlimes))
                return;
            Check.Eq(Check.Def<XenotypeDef>("BS_GreenSlime").label, "green plasmoid", "BS_GreenSlime label");
            Check.Eq(Check.Def<XenotypeDef>("BS_EmperorSlime").label, "emperor plasmoid", "BS_EmperorSlime label");
            Check.Eq(Check.Def<XenotypeDef>("BS_PinkSlime").label, "pink plasmoid", "BS_PinkSlime label");
            Check.Eq(Check.Def<XenotypeDef>("BS_ElixirSlime").label, "elixir plasmoid", "BS_ElixirSlime label");
            Check.Eq(Check.Def<XenotypeDef>("BS_FrostSlime").label, "frost plasmoid", "BS_FrostSlime label");
            Check.Eq(Check.Def<XenotypeDef>("BS_LavaSlime").label, "lava plasmoid", "BS_LavaSlime label");
            Check.Eq(Check.Def<XenotypeDef>("BS_FrostSlimeGiant").label, "giant frost plasmoid", "BS_FrostSlimeGiant label");
            Check.Eq(Check.Def<XenotypeDef>("BS_LavaSlimeGiant").label, "giant lava plasmoid", "BS_LavaSlimeGiant label");
            Check.Eq(Check.Def<XenotypeDef>("BS_BananaSplitSlime").label, "horned banana split neko cream plasmoid", "BS_BananaSplitSlime label");
            Check.Eq(Check.Def<XenotypeDef>("BS_BananaSplitSlimeGiant").label, "greater horned banana split neko cream plasmoid", "BS_BananaSplitSlimeGiant label");
            FactionDef faction = Check.Def<FactionDef>("BS_SlimeFaction");
            Check.Eq(faction.label, "plasmoid faction", "BS_SlimeFaction label");
            Check.Eq(faction.fixedName, "Escaped Plasmoids", "BS_SlimeFaction fixedName");
        }

        [Test]
        public static void XenotypesRewired()
        {
            if (!Check.Ready("genepool.dedup", Ids.AlphaGenes, Ids.WVC, Ids.BigSmallCore, Ids.CherryPicker, Ids.BigSmallSlimes))
                return;
            Check.XenoGene("BS_BananaSplitSlime", "AG_ColdImmunity");
            Check.XenoGene("BS_BananaSplitSlimeGiant", "AG_ColdImmunity");
            if (ModsConfig.IsActive(Ids.VREPigskin))
            {
                Check.XenoGene("BS_EmperorSlime", "VRE_SlowAging");
                Check.XenoGene("BS_FrostSlime", "VRE_SlowAging");
                Check.XenoGene("BS_FrostSlimeGiant", "VRE_SlowAging");
                Check.XenoGene("BS_LavaSlimeGiant", "VRE_SlowAging");
            }
            if (ModsConfig.IsActive(Ids.VREHighmate))
                Check.XenoGene("BS_PinkSlime", "VRE_Flirty");
        }
    }
}

