using System.Collections.Generic;
using Verse;

namespace RebalancePatches
{
    public class RebalancePatchesSettings : ModSettings
    {
        public Dictionary<string, bool> values = new Dictionary<string, bool>();

        public bool TryGet(string key, out bool value) => values.TryGetValue(key, out value);

        public void Set(string key, bool value) => values[key] = value;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref values, "values", LookMode.Value, LookMode.Value);
            values ??= new Dictionary<string, bool>();
        }
    }
}
