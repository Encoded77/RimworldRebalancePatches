using System.Collections.Generic;
using Verse;

namespace RebalancePatches
{
    public class RebalancePatchesSettings : ModSettings
    {
        public Dictionary<string, bool> values = new Dictionary<string, bool>();
        public Dictionary<string, int> intValues = new Dictionary<string, int>();

        public int configVersion;

        public bool CameFromDisk { get; private set; }

        public bool TryGet(string key, out bool value) => values.TryGetValue(key, out value);

        public void Set(string key, bool value) => values[key] = value;

        public bool TryGetInt(string key, out int value) => intValues.TryGetValue(key, out value);

        public void SetInt(string key, int value) => intValues[key] = value;

        public void RemoveInt(string key) => intValues.Remove(key);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref values, "values", LookMode.Value, LookMode.Value);
            values ??= new Dictionary<string, bool>();
            Scribe_Collections.Look(ref intValues, "intValues", LookMode.Value, LookMode.Value);
            intValues ??= new Dictionary<string, int>();
            Scribe_Values.Look(ref configVersion, "configVersion", 0);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                CameFromDisk = true;
        }
    }
}
