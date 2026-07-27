using System;
using System.IO;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Missile Girl - Performance Mod (formerly GAGARIN, also shipped inside Performance Optimizer)
    /// caches the flattened post-patch defs, keyed on patch-file and assembly content, not on our mod
    /// settings. Toggling a Rebalance Patches setting without editing a patch file therefore leaves a
    /// stale cache that replays the old flattened defs, so the change silently never applies. Deleting
    /// the cache files forces a full rebuild with the current settings on the next launch.
    /// </summary>
    internal static class GagarinCache
    {
        private static readonly string[] Files =
            { "Unified.xml", "Unified_Original.xml", "AssetsHash.xml", "AssetsHashInt.xml" };

        /// <summary>Mirrors Missile Girl's own CustomConfigFolderPath: the "MissileGirl/Cache" folder
        /// sitting beside RimWorld's Config folder.</summary>
        public static string DefaultCacheDir
        {
            get
            {
                DirectoryInfo parent = Directory.GetParent(GenFilePaths.ConfigFolderPath);
                return parent == null ? null : Path.Combine(parent.FullName, "MissileGirl", "Cache");
            }
        }

        public static bool Present() => PresentAt(DefaultCacheDir);

        /// <summary>Deletes the cache files if any are present; returns how many were removed.</summary>
        public static int Reset() => ResetAt(DefaultCacheDir);

        internal static bool PresentAt(string dir)
        {
            if (string.IsNullOrEmpty(dir))
                return false;
            try
            {
                foreach (string f in Files)
                    if (File.Exists(Path.Combine(dir, f)))
                        return true;
            }
            catch
            {
                // A path or permission problem is not a reason to fail settings saving.
            }
            return false;
        }

        internal static int ResetAt(string dir)
        {
            if (string.IsNullOrEmpty(dir))
                return 0;
            int removed = 0;
            try
            {
                foreach (string f in Files)
                {
                    string path = Path.Combine(dir, f);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        removed++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rebalance Patches] Could not reset the Missile Girl patch cache:\n{ex}");
            }
            return removed;
        }
    }
}
