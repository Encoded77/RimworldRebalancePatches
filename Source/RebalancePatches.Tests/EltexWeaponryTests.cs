using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class EltexWeaponryTests
    {
        [Test]
        public static void EltexWeaponsOnlyOnPsycasters()
        {
            if (!Check.Ready("eltex.spawns", Ids.EltexWeaponry, Ids.Royalty))
                return;
            void TagsAre(string thing, params string[] expected)
            {
                ThingDef def = Check.Def<ThingDef>(thing);
                var actual = def.weaponTags == null ? new List<string>() : new List<string>(def.weaponTags);
                actual.Sort();
                var want = new List<string>(expected);
                want.Sort();
                Check.Eq(string.Join(", ", actual), string.Join(", ", want), $"{thing}.weaponTags");
            }
            TagsAre("EW_Gun_EltexRifle", "PsychicGun");
            TagsAre("EW_Gun_PersonaEltexRifle", "PsychicGun", "Bladelink");
            TagsAre("EW_Melee_EltexWarmace", "PsychicMelee");
            void KindHasTag(string kindName)
            {
                PawnKindDef kind = Check.Def<PawnKindDef>(kindName);
                Check.True(kind.weaponTags != null && kind.weaponTags.Contains("PsychicGun"),
                    $"{kindName} weaponTags lack PsychicGun");
            }
            KindHasTag("Empire_Fighter_Cataphract");
            if (ModsConfig.IsActive(Ids.VPE))
                foreach (string kind in new[] { "Empire_Caster_Conflagrator", "Empire_Caster_Archotechist",
                    "Empire_Caster_Warlord_Ranged", "Empire_Caster_Protector" })
                    KindHasTag(kind);
            if (ModsConfig.IsActive(Ids.VFEEmpire))
                KindHasTag("VFEE_Deserter");
            if (ModsConfig.IsActive(Ids.VFEEmpire) && ModsConfig.IsActive(Ids.VPE))
                foreach (string kind in new[] { "VFEE_Deserter_Conflagator", "VFEE_Deserter_Warlord",
                    "VFEE_Deserter_Protector", "VFEE_Deserter_Nightstalker", "VFEE_Deserter_Harmonist" })
                    KindHasTag(kind);
        }
    }
}
