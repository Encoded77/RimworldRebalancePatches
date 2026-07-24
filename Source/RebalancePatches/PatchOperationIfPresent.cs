using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace RebalancePatches
{
    public class PatchOperationIfPresent : PatchOperation
    {
        public string xpath;
        public List<PatchOperation> operations;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (string.IsNullOrEmpty(xpath) || operations == null)
                return true;
            if (xml.SelectSingleNode(xpath) == null)
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
                    Log.Error($"[Rebalance Patches] guarded operation on '{xpath}' threw:\n{ex}");
                }
            }
            return true;
        }
    }
}
