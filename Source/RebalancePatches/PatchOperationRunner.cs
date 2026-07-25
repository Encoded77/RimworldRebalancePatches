using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace RebalancePatches
{
    /// <summary>Shared body for the guarded container operations: run each child, isolating throws so one
    /// bad operation cannot abort the rest or the whole patch load.</summary>
    internal static class PatchOperationRunner
    {
        public static void RunAll(List<PatchOperation> operations, XmlDocument xml, string context)
        {
            if (operations == null)
                return;
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
                    Log.Error($"[Rebalance Patches] {context} operation threw:\n{ex}");
                }
            }
        }
    }
}
