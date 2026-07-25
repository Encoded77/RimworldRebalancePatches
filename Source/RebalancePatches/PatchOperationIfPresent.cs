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

            PatchOperationRunner.RunAll(operations, xml, $"guarded operation on '{xpath}'");
            return true;
        }
    }
}
