using System;
using HarmonyLib;
using RimTestRedux;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VpeTests
    {
        [Test]
        public static void PawnGenerationSurvivesLockedPaths()
        {
            if (!Check.Ready("vpe.pawngenguard", Ids.VPE))
                return;

            Type patchType = AccessTools.TypeByName("VanillaPsycastsExpanded.PawnGen_Patch");
            if (!Check.Soft(patchType != null, "VanillaPsycastsExpanded.PawnGen_Patch not found, so the "
                + "pawn-generation guard has nothing to attach to"))
            {
                Check.SoftResult();
                return;
            }

            Check.HarmonyPatched(AccessTools.DeclaredMethod(patchType, "Postfix"), "vpe.pawngenguard");
            Check.SoftResult();
        }
    }
}
