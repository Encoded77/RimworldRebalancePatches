using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    // Weapon-category counterpart to per-weapon aptitude affecters: applies while
    // the pawn's primary weapon matches the melee/ranged flag and the heavy/light
    // split (heavy = mass of 3 kg or more, cached per ThingDef).
    public class ConditionalStatAffecter_WeaponClass : ConditionalStatAffecter
    {
        public bool melee;
        public bool heavy;

        private const float HeavyMassThreshold = 3f;
        private static readonly Dictionary<ThingDef, bool> heavyCache = new Dictionary<ThingDef, bool>();

        public override string Label =>
            (heavy ? "Heavy" : "Light") + (melee ? " melee" : " ranged") + " weapon equipped";

        public override bool Applies(StatRequest req)
        {
            if (!req.HasThing || !(req.Thing is Pawn pawn) || !pawn.RaceProps.Humanlike)
                return false;
            ThingWithComps primary = pawn.equipment?.Primary;
            if (primary == null)
                return false;
            ThingDef def = primary.def;
            if ((melee ? def.IsMeleeWeapon : def.IsRangedWeapon) != true)
                return false;
            return IsHeavy(def) == heavy;
        }

        public static bool IsHeavy(ThingDef def)
        {
            if (!heavyCache.TryGetValue(def, out bool value))
            {
                value = def.GetStatValueAbstract(StatDefOf.Mass) >= HeavyMassThreshold;
                heavyCache[def] = value;
            }
            return value;
        }
    }
}
