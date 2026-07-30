using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class AchievementsTests
    {
        private const string RewardType = "AchievementsExpanded.AchievementReward";

        [Test]
        public static void RewardCosts()
        {
            if (!Check.Ready("achievements.rewardcosts", Ids.VAEAchievements))
                return;
            AssertRewardSet("");
            if (ModsConfig.IsActive(Ids.VWE))
                AssertRewardSet("VE_WE");
            if (ModsConfig.IsActive(Ids.VARME))
                AssertRewardSet("VE");
            Check.SoftResult();
        }

        [Test]
        public static void NewRewards()
        {
            if (!Check.Ready("achievements.newrewards", Ids.VAEAchievements))
                return;
            AssertReward("RBP_AmbrosiaSproutReward", 10, "AmbrosiaSprout");
            AssertReward("RBP_HerdMigrationReward", 15, "HerdMigration");
            AssertReward("RBP_MeteoriteImpactReward", 20, "MeteoriteImpact");
            AssertReward("RBP_ShipChunkDropReward", 25, "ShipChunkDrop");
            AssertReward("RBP_SelfTameReward", 30, "SelfTame");
            AssertReward("RBP_PsychicSootheReward", 40, "PsychicSoothe");
            AssertReward("RBP_ThrumboPassesReward", 50, "ThrumboPasses");
            AssertReward("RBP_RefugeePodCrashReward", 75, "RefugeePodCrash");
            AssertReward("RBP_WandererJoinReward", 150, "WandererJoin");
            Check.SoftResult();
        }

        [Test]
        public static void AnomalyRewards()
        {
            if (!Check.Ready("achievements.anomalyrewards", Ids.VAEAchievements, Ids.Anomaly))
                return;
            AssertReward("RBP_HarbingerTreeReward", 30, "HarbingerTreeSpawn");
            AssertReward("RBP_MysteriousCargoReward", 40, "MysteriousCargoCube");
            Check.SoftResult();
        }

        private static void AssertRewardSet(string suffix)
        {
            Check.Soft(Check.DefOfTypeOptional(RewardType, "TradeCaravanReward" + suffix) == null,
                $"TradeCaravanReward{suffix} still present - the trade caravan exchange was not removed");
            AssertCost("CargoPodsReward" + suffix, 40);
            AssertCost("RandomRewardQuestItem" + suffix, 150);
            AssertCost("ManInBlackReward" + suffix, 200);
            AssertCost("EnemyRaidReward" + suffix, 10);
            AssertCost("RandomEventReward" + suffix, 25);
        }

        private static void AssertCost(string defName, int expected)
        {
            Def reward = Check.DefOfTypeOptional(RewardType, defName);
            if (!Check.Soft(reward != null, $"reward '{defName}' not found - renamed or removed upstream"))
                return;
            int cost = (int)Check.Field(reward, "cost");
            Check.Soft(cost == expected, $"reward '{defName}' cost is {cost}, expected {expected}");
        }

        private static void AssertReward(string defName, int expectedCost, string expectedIncident)
        {
            Def reward = Check.DefOfTypeOptional(RewardType, defName);
            if (!Check.Soft(reward != null, $"injected reward '{defName}' not found"))
                return;
            int cost = (int)Check.Field(reward, "cost");
            Check.Soft(cost == expectedCost, $"reward '{defName}' cost is {cost}, expected {expectedCost}");

            var incident = Check.Field(reward, "incident") as Def;
            Check.Soft(incident != null && incident.defName == expectedIncident,
                $"reward '{defName}' fires '{incident?.defName ?? "nothing"}', expected '{expectedIncident}'");

            var tab = Check.Field(reward, "tab") as Def;
            Check.Soft(tab != null && tab.defName == "Main",
                $"reward '{defName}' sits on tab '{tab?.defName ?? "none"}', expected 'Main'");
        }
    }
}
