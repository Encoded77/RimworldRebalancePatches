using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ValiChargedTests
    {
        [Test]
        public static void ChargeEltexWeaponsCarriedByPsycasters()
        {
            if (!Check.Ready("valicharged.psyspawns", Ids.ValiEltexSeries))
                return;

            // The series is seven melee weapons on one tag and two gunblades on another; melee joins the
            // psychic melee pool, the gunblades the psychic gun pool.
            int melee = 0, gunblades = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.weaponTags == null)
                    continue;
                if (def.weaponTags.Contains("V_EltexMelee"))
                {
                    Check.Soft(def.weaponTags.Contains("PsychicMelee"),
                        $"{def.defName} carries V_EltexMelee but not PsychicMelee");
                    melee++;
                }
                if (def.weaponTags.Contains("V_GunBlade"))
                {
                    Check.Soft(def.weaponTags.Contains("PsychicGun"),
                        $"{def.defName} carries V_GunBlade but not PsychicGun");
                    gunblades++;
                }
            }
            Check.Note($"{melee} melee + {gunblades} gunblade(s) checked");
            Check.Soft(melee >= 7, $"only {melee} V_EltexMelee weapon(s) found, expected the series' seven");
            Check.Soft(gunblades >= 2, $"only {gunblades} V_GunBlade weapon(s) found, expected two");
            Check.SoftResult();
        }
    }
}
