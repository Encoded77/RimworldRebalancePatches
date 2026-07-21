using System;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Defs injected with PatchOperationAdd have no source asset behind them, so LoadedModManager
    /// files them under patchedDefs and never assigns modContentPack - only defs that came from a
    /// real file get attributed. Research tree UIs read that field to badge each project with the
    /// mod it came from, so every project this mod injects renders without the badge that every
    /// other mod's research carries.
    ///
    /// Claim them back. Only defs that are both unattributed and carry our RBP_ prefix are touched,
    /// so this can never take credit for a def belonging to someone else.
    /// </summary>
    internal static class DefAttribution
    {
        private const string Prefix = "RBP_";

        public static void TryApply()
        {
            try
            {
                Apply();
            }
            catch (Exception e)
            {
                Log.Warning("[Rebalance Patches] Could not attribute injected defs to this mod:\n" + e);
            }
        }

        private static void Apply()
        {
            ModContentPack ours = LoadedModManager.GetMod<RebalancePatchesMod>()?.Content;
            if (ours == null)
                return;

            int claimed = 0;
            // PatchedDefsForReading is exactly the set that fell through the unattributed branch,
            // so there is no need to sweep every DefDatabase looking for them.
            foreach (Def def in LoadedModManager.PatchedDefsForReading)
            {
                if (def.modContentPack == null && def.defName != null && def.defName.StartsWith(Prefix))
                {
                    def.modContentPack = ours;
                    claimed++;
                }
            }
            if (claimed > 0)
                Log.Message($"[RebalancePatches] Attributed {claimed} injected defs to this mod, "
                    + "so research tree UIs can show where they came from.");
        }
    }
}
