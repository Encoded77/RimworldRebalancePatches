using System.Collections.Generic;
using System.Xml;
using Verse;

namespace RebalancePatches
{
    public class PatchOperationIfEnabled : PatchOperation
    {
        public string settingKey;

        public bool? defaultOn;

        public List<PatchOperation> operations;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (string.IsNullOrEmpty(settingKey))
                return true;
            if (defaultOn.HasValue)
                SettingsRegistry.RegisterXmlDefault(settingKey, defaultOn.Value);
            if (!SettingsRegistry.GetEffective(settingKey) || operations == null)
                return true;

            PatchOperationRunner.RunAll(operations, xml, $"'{settingKey}'");
            return true;
        }
    }
}
