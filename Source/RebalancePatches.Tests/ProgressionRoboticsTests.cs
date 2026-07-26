using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ProgressionRoboticsTests
    {
        private static ResearchProjectDef Research(string defName) =>
            DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);

        private static void Survivor(string defName, string label, float cost)
        {
            ResearchProjectDef d = Research(defName);
            if (!Check.Soft(d != null, defName + " (merge survivor) is missing"))
                return;
            Check.Soft(d.label == label, $"{defName} label is '{d.label}', expected '{label}'");
            Check.Soft(d.baseCost == cost, $"{defName} baseCost is {d.baseCost}, expected {cost}");
        }

        private static void Gone(string defName) =>
            Check.Soft(Research(defName) == null, defName + " should have been merged away but still exists");

        private static void RecipeReq(string recipeDef, string research)
        {
            RecipeDef r = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDef);
            if (!Check.Soft(r != null, recipeDef + " recipe is missing"))
                return;
            Check.Soft(r.researchPrerequisite?.defName == research,
                $"{recipeDef} researchPrerequisite is '{r.researchPrerequisite?.defName}', expected '{research}'");
        }

        private static void ThingRecipeReq(string thingDef, string research)
        {
            ThingDef t = DefDatabase<ThingDef>.GetNamedSilentFail(thingDef);
            if (!Check.Soft(t != null, thingDef + " is missing"))
                return;
            Check.Soft(Check.RecipePrereq(t)?.defName == research,
                $"{thingDef} recipeMaker.researchPrerequisite is '{Check.RecipePrereq(t)?.defName}', expected '{research}'");
        }

        private static void ThingListReq(string thingDef, string research)
        {
            ThingDef t = DefDatabase<ThingDef>.GetNamedSilentFail(thingDef);
            if (!Check.Soft(t != null, thingDef + " is missing"))
                return;
            Check.Soft(Check.ContainsResearch(t.researchPrerequisites, research),
                $"{thingDef} researchPrerequisites should contain '{research}'");
        }

        [Test]
        public static void GroupMechs()
        {
            if (!Check.Ready("progressionrobotics.groupmechs", Ids.ProgressionRobotics, Ids.Biotech))
                return;

            // Biotech: standard combat mechs
            Survivor("Ferny_Cyclops", "Standard combat mechs", 3000f);
            RecipeReq("Pikeman", "Ferny_Cyclops");
            RecipeReq("Scorcher", "Ferny_Cyclops");
            RecipeReq("Tunneler", "Ferny_Cyclops");
            Gone("Ferny_Pikeman");
            Gone("Ferny_Scorcher");
            Gone("Ferny_Tunneler");

            // Biotech: utility mechs
            Survivor("Ferny_Cleansweeper", "Basic utility mechs", 800f);
            RecipeReq("Agrihand", "Ferny_Cleansweeper");
            Gone("Ferny_Agrihand");
            Survivor("Ferny_Fabricor", "Advanced utility mechs", 2300f);
            RecipeReq("Paramedic", "Ferny_Fabricor");
            Gone("Ferny_Paramedic");

            if (ModsConfig.IsActive(Ids.AlphaMechs))
            {
                Survivor("Ferny_Bellicor", "Militor variants", 1200f);
                RecipeReq("AM_ArtilleronRecipe", "Ferny_Bellicor");
                Gone("Ferny_Artilleron");

                Survivor("Ferny_Lux", "Energy-weapon mechs", 2300f);
                RecipeReq("AM_MunifexRecipe", "Ferny_Lux");
                RecipeReq("AM_PolychoronRecipe", "Ferny_Lux");
                Gone("Ferny_Munifex");
                Gone("Ferny_Polychoron");

                Survivor("Ferny_Aura", "Assault & support mechs", 1500f);
                RecipeReq("AM_OptioRecipe", "Ferny_Aura");
                Gone("Ferny_Optio");

                Survivor("Ferny_Demolisher", "Heavy assault mechs", 3400f);
                RecipeReq("AM_FirewormRecipe", "Ferny_Demolisher");
                RecipeReq("AM_PhalanxRecipe", "Ferny_Demolisher");
                Gone("Ferny_Fireworm");
                Gone("Ferny_Phalanx");

                Survivor("Ferny_Apoptosis", "Cryptoharmonized mechs", 6000f);
                RecipeReq("AM_InfernusRecipe", "Ferny_Apoptosis");
                Gone("Ferny_Infernus");

                Survivor("Ferny_Culinarius", "Culinary & cleaning mechs", 1000f);
                RecipeReq("AM_Mech_TurboCleanerRecipe", "Ferny_Culinarius");
                Gone("Ferny_Turbocleaner");
            }

            Check.SoftResult();
        }

        [Test]
        public static void GroupGear()
        {
            if (!Check.Ready("progressionrobotics.groupgear", Ids.ProgressionRobotics, Ids.Biotech))
                return;

            // Biotech: bandwidth gear
            Survivor("Ferny_AirwireHeadset", "Mechanitor bandwidth gear", 1600f);
            ThingRecipeReq("Apparel_PackControl", "Ferny_AirwireHeadset");
            ThingRecipeReq("Apparel_PackBandwidth", "Ferny_AirwireHeadset");
            ThingRecipeReq("ControlSublink", "Ferny_AirwireHeadset");
            Gone("Ferny_ControlPack");
            Gone("Ferny_BandwidthPack");
            Gone("Ferny_ControlSublink");

            // Biotech: advanced bandwidth gear
            Survivor("Ferny_ArrayHeadset", "Advanced bandwidth gear", 4000f);
            ThingListReq("BandNode", "Ferny_ArrayHeadset");
            ThingRecipeReq("ControlSublinkHigh", "Ferny_ArrayHeadset");
            ThingRecipeReq("Apparel_ArmorHelmetMechCommander", "Ferny_ArrayHeadset");
            ThingRecipeReq("Apparel_IntegratorHeadset", "Ferny_ArrayHeadset");
            Gone("Ferny_BandNode");
            Gone("Ferny_ControlSublinkHigh");
            Gone("Ferny_MechCommanderHelmet");
            Gone("Ferny_IntegratorHeadset");

            // Biotech: mech field support
            Survivor("Ferny_MechBooster", "Mech field support", 3400f);
            ThingRecipeReq("RemoteRepairer", "Ferny_MechBooster");
            ThingRecipeReq("RemoteShielder", "Ferny_MechBooster");
            Gone("Ferny_RemoteRepairer");
            Gone("Ferny_RemoteShielder");

            // Biotech: mechlord equipment
            Survivor("Ferny_MechlordSuit", "Mechlord equipment", 4500f);
            ThingRecipeReq("RepairProbe", "Ferny_MechlordSuit");
            Gone("Ferny_RepairProbe");

            if (ModsConfig.IsActive(Ids.AlphaMechs))
            {
                Survivor("Ferny_MechEfficiencyBooster", "Mech booster tuning", 4500f);
                ThingListReq("AM_MechArmorBooster", "Ferny_MechEfficiencyBooster");
                ThingListReq("AM_MechTargettingBooster", "Ferny_MechEfficiencyBooster");
                Gone("Ferny_MechProtectionBooster");
                Gone("Ferny_MechTargetingBooster");

                Survivor("Ferny_PersistentMechBooster", "Advanced mech boosters", 6000f);
                ThingListReq("AM_MechDisruptor", "Ferny_PersistentMechBooster");
                Gone("Ferny_MechDisruptor");
                ResearchProjectDef pmb = Research("Ferny_PersistentMechBooster");
                if (pmb != null)
                    Check.Soft(pmb.prerequisites != null && pmb.prerequisites.Count == 1 &&
                        pmb.prerequisites[0].defName == "Ferny_MechEfficiencyBooster",
                        "Ferny_PersistentMechBooster should require only Ferny_MechEfficiencyBooster");

                // Gear kept individual, repointed off the folded biotech nodes
                ResearchProjectDef master = Research("Ferny_MechMasterHelmet");
                if (Check.Soft(master != null, "Ferny_MechMasterHelmet is missing"))
                    Check.Soft(Check.ContainsResearch(master.prerequisites, "Ferny_ArrayHeadset"),
                        "Ferny_MechMasterHelmet should require Ferny_ArrayHeadset");
                ResearchProjectDef beam = Research("Ferny_BeamcasterBandNode");
                if (Check.Soft(beam != null, "Ferny_BeamcasterBandNode is missing"))
                    Check.Soft(Check.ContainsResearch(beam.prerequisites, "Ferny_ArrayHeadset") &&
                        Check.ContainsResearch(beam.prerequisites, "AM_MechanoidBeamcasting"),
                        "Ferny_BeamcasterBandNode should require Ferny_ArrayHeadset and AM_MechanoidBeamcasting");
            }

            Check.SoftResult();
        }
    }
}
