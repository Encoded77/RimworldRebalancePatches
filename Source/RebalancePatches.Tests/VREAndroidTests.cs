using System.Collections;
using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREAndroidTests
    {
        [Test]
        public static void AndroidsRefuseWetwareImplants()
        {
            if (!Check.Ready("cybernetics.androidblocklist", Ids.VREAndroid))
                return;

            Def settings = Check.DefOfType("VREAndroids.AndroidSettings", "VREA_AndroidSettings");
            IEnumerable disallowed = (IEnumerable)Check.Field(settings, "disallowedRecipes");
            Check.True(disallowed != null, "VREA_AndroidSettings.disallowedRecipes is null");

            foreach (string recipe in new[]
            {
                "InstallDreadheart",                            // anomaly / entity (Integrated Implants)
                "LTS_InstallArchotechHeart",                    // metabolic organ (Integrated Implants)
                "InstallSurrogateLung",                         // metabolic organ (EPOE-Forked)
                "InstallEPOE_DetoxifierEnhancer",               // metabolic organ (EPOE-Royalty)
                "InstallDeathrestCapacitor",                    // hemogenic / deathrest
                "InstallHyperAdrenalineGland",                  // endocrine / metabolic
                "InstallAdrenalineRib",                         // endocrine / metabolic (EPOE-Forked)
                "gitsInstallImmunityNanites",                   // wetware nanites (GiTS)
                "InstallArchowomb",                             // reproductive
                "ScrapGhoulPowerPlatingThin",                   // anomaly / entity, variant defName
            })
                Check.True(Check.AnyDefNamed(disallowed, recipe),
                    $"VREA_AndroidSettings.disallowedRecipes lacks {recipe}");

            // The patch appends; the entries the mod ships with must survive it.
            foreach (string shipped in new[] { "ExciseCarcinoma", "ImplantXenogerm", "BloodTransfusion" })
                Check.True(Check.AnyDefNamed(disallowed, shipped),
                    $"VREA_AndroidSettings.disallowedRecipes lost shipped entry {shipped} - patch replaced instead of appending");
        }
    }
}
