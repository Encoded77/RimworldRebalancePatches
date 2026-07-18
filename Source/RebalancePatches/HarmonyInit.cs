using HarmonyLib;
using RebalancePatches.Mods.AlteredCarbon;
using Verse;

namespace RebalancePatches
{
    [StaticConstructorOnStartup]
    internal static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("encoded.rebalancepatches");
            AlteredCarbonPatches.TryApply(harmony);
        }
    }
}
