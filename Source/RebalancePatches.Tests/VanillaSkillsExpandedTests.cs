using System.Collections;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VanillaSkillsExpandedTests
    {
        [Test]
        public static void GunnerExpertiseUsesVanillaCooldownStat()
        {
            if (!Check.Ready("vse.reloadingstat", Ids.VSE))
                return;
            Def gunner = Check.DefOfType("VSE.Expertise.ExpertiseDef", "Gunner");
            IEnumerable offsets = (IEnumerable)Check.Field(gunner, "statOffsets");
            bool hasVanilla = false;
            bool hasVef = false;
            if (offsets != null)
            {
                foreach (object o in offsets)
                {
                    if (o is StatModifier m && m.stat != null)
                    {
                        if (m.stat.defName == "RangedCooldownFactor" && m.value == -0.025f)
                            hasVanilla = true;
                        if (m.stat.defName == "VEF_VerbCooldownFactor")
                            hasVef = true;
                    }
                }
            }
            Check.True(hasVanilla, "Gunner expertise lacks RangedCooldownFactor -0.025 offset");
            Check.True(!hasVef, "Gunner expertise still offsets VEF_VerbCooldownFactor");
        }
    }
}
