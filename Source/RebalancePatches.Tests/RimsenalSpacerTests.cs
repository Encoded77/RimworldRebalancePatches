using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class RimsenalSpacerTests
    {
        [Test]
        public static void CaravansWithoutMechGuards()
        {
            if (!Check.Ready("rimsenalspacer.caravanmechs", Ids.RimsenalSpacer))
                return;
            var mechs = new[] { "Mech_Tagmaton", "Mech_Skutaton", "Mech_Psiloid" };
            foreach (string factionName in new[] { "SpacerCivil", "SpacerRough" })
            {
                FactionDef faction = Check.Def<FactionDef>(factionName);
                Check.True(faction.pawnGroupMakers != null, $"{factionName} has no pawnGroupMakers");
                foreach (PawnGroupMaker maker in faction.pawnGroupMakers)
                {
                    if (maker.kindDef == null || maker.kindDef.defName != "Trader" || maker.guards == null)
                        continue;
                    foreach (PawnGenOption guard in maker.guards)
                        foreach (string mech in mechs)
                            Check.True(guard.kind == null || guard.kind.defName != mech,
                                $"{factionName} trader caravan still has {mech} guard");
                }
            }
        }

        [Test]
        public static void SmartWeaponPrereqsCleaned()
        {
            if (!Check.Ready("rimsenalspacer.smartweapons", Ids.RimsenalSpacer))
                return;
            foreach (string gun in new[] { "Gun_SmartChargeRifle", "Gun_SmartChargeLance", "Gun_SmartMinigun" })
                Check.True(Check.RecipePrereq(Check.Def<ThingDef>(gun)) == null,
                    $"{gun} still has a single researchPrerequisite ({Check.RecipePrereq(Check.Def<ThingDef>(gun))?.defName})");
            if (ModsConfig.IsActive(Ids.Royalty))
            {
                Check.Eq(Check.RecipePrereq(Check.Def<ThingDef>("Apparel_SmartVisor"))?.defName, "Gunlink",
                    "Apparel_SmartVisor researchPrerequisite");
                Check.Eq(Check.Def<ResearchProjectDef>("Gunlink").label, "smart targeting systems", "Gunlink label");
            }
        }

        [Test]
        public static void SpacerFactionsNeverPacifist()
        {
            if (!Check.Ready("memes.factions", Ids.RimsenalSpacer, Ids.VIEMemes, Ids.AlphaMemes))
                return;
            MemeDef nonViolence = Check.Def<MemeDef>("AM_NonViolence");
            foreach (string faction in new[] { "SpacerCivil", "SpacerRough" })
                Check.True(Check.Def<FactionDef>(faction).disallowedMemes?.Contains(nonViolence) == true,
                    $"{faction} does not disallow AM_NonViolence");
        }

        [Test]
        public static void WvcXenotypesMoveToSpacerFactions()
        {
            if (!Check.Ready("xenotypes.rimsenal", Ids.WVC))
                return;
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("OutlanderRough"), "WVC_Meca"), "OutlanderRough still spawns WVC_Meca");
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("OutlanderRough"), "WVC_RogueFormer"), "OutlanderRough still spawns WVC_RogueFormer");
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("Pirate"), "WVC_RogueFormer"), "Pirate still spawns WVC_RogueFormer");
            Check.True(!Check.HasXenotype(Check.Def<FactionDef>("OutlanderCivil"), "WVC_GeneThrower"), "OutlanderCivil still spawns WVC_GeneThrower");
            if (ModsConfig.IsActive(Ids.RimsenalSpacer))
            {
                foreach (string faction in new[] { "SpacerCivil", "SpacerRough" })
                    foreach (string xenotype in new[] { "WVC_GeneThrower", "WVC_Meca", "WVC_RogueFormer" })
                        Check.True(Check.HasXenotype(Check.Def<FactionDef>(faction), xenotype),
                            $"{faction} lacks xenotype {xenotype}");
            }
        }
    }
}
