using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VFEDesertersTests
    {
        private const string Key = "cyberneticsresearch.retire";

        [Test]
        public static void ContrabandSurvivesTheResearchRetire()
        {
            if (!Check.Ready(Key, Ids.VFEDeserters))
                return;

            System.Type manager = AccessTools.TypeByName("VFED.ContrabandManager");
            if (!Check.Soft(manager != null,
                    "VFED.ContrabandManager not found although the mod is active - renamed upstream, "
                    + "so the contraband compatibility patch is unverified"))
            {
                Check.SoftResult();
                return;
            }

            object all;
            try
            {
                all = AccessTools.Field(manager, "AllContraband")?.GetValue(null);
            }
            catch (System.Exception e)
            {
                Check.Soft(false, "VFED.ContrabandManager failed to initialise, so its contraband list is "
                    + "empty for the whole session - a research project it names in C# was retired: " + e.Message);
                Check.SoftResult();
                return;
            }

            if (!Check.Soft(all != null,
                    "VFED.ContrabandManager.AllContraband is null - the static constructor died, which "
                    + "is what happens when a research project it hard-references no longer exists"))
            {
                Check.SoftResult();
                return;
            }

            int registered = all is ICollection collection ? collection.Count : -1;
            Check.Note($"deserter contraband entries: {registered}");
            Check.Soft(registered > 0,
                "the deserter contraband list is empty - it initialised but registered nothing");

            Check.Soft(CountEmpireImplants(all) > 0,
                "no Empire implant is marked contraband - the substituted lookup returned nothing, so "
                + "the retired projects were replaced by an empty set rather than by their items");

            Check.SoftResult();
        }

        private static int CountEmpireImplants(object all)
        {
            int found = 0;
            if (!(all is IEnumerable entries))
                return 0;
            foreach (object entry in entries)
            {
                if (entry == null)
                    continue;
                FieldInfo item1 = entry.GetType().GetField("Item1");
                if (!(item1?.GetValue(entry) is ThingDef def) || def.techHediffsTags == null)
                    continue;
                foreach (string tag in def.techHediffsTags)
                    if (tag != null && tag.StartsWith("ImplantEmpire"))
                    {
                        found++;
                        break;
                    }
            }
            return found;
        }
    }
}
