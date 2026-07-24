using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using Verse;

namespace RebalancePatches
{
    public class PatchOperationReplaceInText : PatchOperation
    {
        public string xpath;
        public string find;
        public string replace = "";
        public bool regex = false;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (string.IsNullOrEmpty(xpath) || string.IsNullOrEmpty(find))
                return false;

            var targets = new List<XmlNode>();
            foreach (XmlNode node in xml.SelectNodes(xpath))
                targets.Add(node);

            foreach (XmlNode node in targets)
            {
                string current = node.InnerText;
                if (string.IsNullOrEmpty(current))
                    continue;
                string updated = regex ? Regex.Replace(current, find, replace ?? "")
                                       : current.Replace(find, replace ?? "");
                if (updated == current)
                    continue;
                SetText(node, updated);
            }

            return targets.Count > 0;
        }

        private static void SetText(XmlNode node, string text)
        {
            bool hadCData = false;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.CDATA)
                {
                    hadCData = true;
                    break;
                }
            }

            for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
                node.RemoveChild(node.ChildNodes[i]);

            node.AppendChild(hadCData
                ? (XmlNode)node.OwnerDocument.CreateCDataSection(text)
                : node.OwnerDocument.CreateTextNode(text));
        }
    }
}
