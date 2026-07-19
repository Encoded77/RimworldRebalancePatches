using System.Collections.Generic;
using System.Xml;
using Verse;

namespace RebalancePatches
{
    public class PatchOperationFindModById : PatchOperation
    {
        public List<string> mods;
        public PatchOperation match;
        public PatchOperation nomatch;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool anyActive = false;
            if (mods != null)
            {
                foreach (string packageId in mods)
                {
                    if (ModsConfig.IsActive(packageId))
                    {
                        anyActive = true;
                        break;
                    }
                }
            }
            if (anyActive)
            {
                if (match != null)
                    return match.Apply(xml);
            }
            else if (nomatch != null)
            {
                return nomatch.Apply(xml);
            }
            return true;
        }

        public override string ToString()
        {
            return $"{base.ToString()}({mods.ToCommaList()})";
        }
    }
}
