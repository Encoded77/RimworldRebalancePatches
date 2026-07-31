using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class BetterQuestRewardsTests
    {
        // A reward group this large whose every branch allows nothing is not a design choice, it is a
        // block of items that failed to resolve. Isolated empty filters are legitimate and common.
        private const int DeadGroupSize = 10;

        [Test]
        public static void NoQuestRewardsThatCannotExist()
        {
            if (!Check.Ready("questrewards.psytrainers", Ids.BetterQuestRewards, Ids.VPE, Ids.FuckPsytrainers))
                return;

            ThingSetMakerDef def = Check.Def<ThingSetMakerDef>("Reward_ItemsStandard");
            if (!Check.Soft(def.root != null, "Reward_ItemsStandard has no root ThingSetMaker"))
            {
                Check.SoftResult();
                return;
            }

            var walked = new List<ThingSetMaker>();
            var dead = new List<string>();
            Walk(def.root, walked, dead);
            Check.Note($"walked {walked.Count} ThingSetMaker(s) under Reward_ItemsStandard");

            Check.Soft(dead.Count == 0,
                $"{dead.Count} reward group(s) under Reward_ItemsStandard have {DeadGroupSize} or more branches "
                + "and not one of them can produce any item, so every thing they name failed to resolve: "
                + string.Join(", ", dead.ToArray()));

            Check.SoftResult();
        }

        private static void Walk(ThingSetMaker maker, List<ThingSetMaker> walked, List<string> dead)
        {
            if (maker == null || walked.Contains(maker))
                return;
            walked.Add(maker);

            var children = new List<ThingSetMaker>();
            foreach (FieldInfo field in maker.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = field.GetValue(maker);
                if (value == null)
                    continue;
                if (value is ThingSetMaker single)
                {
                    children.Add(single);
                    continue;
                }
                if (!(value is IEnumerable list) || value is string)
                    continue;
                foreach (object entry in list)
                {
                    if (entry is ThingSetMaker direct)
                    {
                        children.Add(direct);
                        continue;
                    }
                    FieldInfo nested = entry?.GetType().GetField("thingSetMaker",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (nested?.GetValue(entry) is ThingSetMaker wrapped)
                        children.Add(wrapped);
                }
            }

            if (children.Count >= DeadGroupSize && children.TrueForAll(Barren))
                dead.Add($"{maker.GetType().Name} ({children.Count} branches)");

            foreach (ThingSetMaker child in children)
                Walk(child, walked, dead);
        }

        private static bool Barren(ThingSetMaker maker)
        {
            ThingFilter filter = maker.fixedParams.filter;
            return filter != null && filter.AllowedDefCount == 0;
        }
    }
}
