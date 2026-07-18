using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace RebalancePatches
{
    public class PatchOperationIfEnabled : PatchOperation
    {
        public string settingKey;
        public bool defaultOn = true;
        public List<PatchOperation> operations;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (string.IsNullOrEmpty(settingKey))
                return true;
            SettingsRegistry.RegisterXmlDefault(settingKey, defaultOn);
            if (!SettingsRegistry.GetEffective(settingKey) || operations == null)
                return true;

            foreach (PatchOperation op in operations)
            {
                if (op == null)
                    continue;
                try
                {
                    op.Apply(xml);
                }
                catch (Exception ex)
                {
                    Log.Error($"[Rebalance Patches] '{settingKey}' operation threw:\n{ex}");
                }
            }
            return true;
        }
    }
}
