using System.Xml;
using Verse;

namespace RebalancePatches
{
    public abstract class PatchOperationFromSetting : PatchOperation
    {
        public string settingKey;
        public string xpath;
        public XmlContainer value;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (string.IsNullOrEmpty(settingKey) || string.IsNullOrEmpty(xpath) || value?.node == null)
                return false;

            string settingValue = SettingsRegistry.GetEffectiveValue(settingKey).ToString();
            XmlNode substituted = value.node.CloneNode(true);
            Substitute(substituted, settingValue);

            var targets = new System.Collections.Generic.List<XmlNode>();
            foreach (XmlNode node in xml.SelectNodes(xpath))
                targets.Add(node);
            foreach (XmlNode node in targets)
                ApplyOn(node, substituted);
            return targets.Count > 0;
        }

        protected abstract void ApplyOn(XmlNode target, XmlNode substituted);

        private static void Substitute(XmlNode node, string settingValue)
        {
            if (node.NodeType == XmlNodeType.Text && node.Value != null)
                node.Value = node.Value.Replace("{value}", settingValue);
            foreach (XmlNode child in node.ChildNodes)
                Substitute(child, settingValue);
        }
    }

    public class PatchOperationAddFromSetting : PatchOperationFromSetting
    {
        protected override void ApplyOn(XmlNode target, XmlNode substituted)
        {
            foreach (XmlNode child in substituted.ChildNodes)
                target.AppendChild(target.OwnerDocument.ImportNode(child, true));
        }
    }

    public class PatchOperationReplaceFromSetting : PatchOperationFromSetting
    {
        protected override void ApplyOn(XmlNode target, XmlNode substituted)
        {
            XmlNode parent = target.ParentNode;
            foreach (XmlNode child in substituted.ChildNodes)
                parent.InsertBefore(parent.OwnerDocument.ImportNode(child, true), target);
            parent.RemoveChild(target);
        }
    }
}
