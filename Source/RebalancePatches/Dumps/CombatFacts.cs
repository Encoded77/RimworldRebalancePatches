using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// The combat numbers a weapon or a piece of apparel only has while the game is running, written
    /// into ThingDump so the arsenal reports can read them offline.
    ///
    /// Three of them cannot be recovered from a raw field dump at all. Projectile damage is a private
    /// field behind a method that applies the weapon's damage multiplier; armor ratings and melee DPS
    /// come from stat workers over the parent chain and the stuff; and a ThingDef's verb list is
    /// private, so nothing in the reflective walk ever sees a gun's range, warmup or burst.
    ///
    /// Every weapon also carries a list of reasons its damage may be understated — a verb or
    /// projectile class from a mod assembly, a beam, an explosion, extra damages, no projectile at
    /// all. A weapon on that list is not measured, and the reports must not rank it as though it were.
    /// </summary>
    internal static class CombatFacts
    {
        private static readonly Dictionary<string, StatDef> StatCache = new Dictionary<string, StatDef>();

        /// <summary>The core assembly. A verb, projectile or comp class from anywhere else is a mod's.</summary>
        private static readonly System.Reflection.Assembly CoreAssembly = typeof(Verb).Assembly;

        // ---- weapons ------------------------------------------------------------------------

        public static void WriteWeapon(Json j, ThingDef thing, DefWalker walker)
        {
            ThingDef stuff = thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null;
            var unmeasured = new List<string>();

            j.Name("weapon");
            j.BeginObject();

            j.Name("ranged"); j.Value(thing.IsRangedWeapon);
            j.Name("melee"); j.Value(thing.IsMeleeWeapon);
            if (stuff != null)
            {
                j.Name("defaultStuff"); j.Value(stuff.defName);
            }

            j.Name("stats");
            WeaponStats(j, thing, null);
            if (stuff != null)
            {
                j.Name("stuffedStats");
                WeaponStats(j, thing, stuff);
            }

            j.Name("verbs");
            j.BeginArray();
            foreach (VerbProperties verb in thing.Verbs)
                WriteVerb(j, thing, stuff, verb, unmeasured, walker);
            j.EndArray();

            if (thing.tools != null)
                foreach (Tool tool in thing.tools)
                {
                    if (tool == null) continue;
                    if (tool.hediff != null)
                        unmeasured.Add($"toolHediff:{tool.hediff.defName} ({tool.id})");
                    if (!tool.extraMeleeDamages.NullOrEmpty())
                        unmeasured.Add($"toolExtraDamages:{tool.extraMeleeDamages.Count} ({tool.id})");
                    if (tool.surpriseAttack != null)
                        unmeasured.Add($"toolSurpriseAttack ({tool.id})");
                }

            j.Name("unmeasured");
            j.BeginArray();
            foreach (string reason in unmeasured)
                j.Value(reason);
            j.EndArray();

            j.EndObject();
        }

        private static void WeaponStats(Json j, ThingDef thing, ThingDef stuff)
        {
            j.BeginObject();
            Stat(j, thing, stuff, "MeleeWeapon_AverageDPS");
            Stat(j, thing, stuff, "MeleeWeapon_AverageArmorPenetration");
            Stat(j, thing, stuff, "RangedWeapon_Cooldown");
            Stat(j, thing, stuff, "RangedWeapon_DamageMultiplier");
            Stat(j, thing, stuff, "RangedWeapon_ArmorPenetrationMultiplier");
            Stat(j, thing, stuff, "RangedWeapon_WarmupMultiplier");
            Stat(j, thing, stuff, "RangedWeapon_RangeMultiplier");
            Stat(j, thing, stuff, "AccuracyTouch");
            Stat(j, thing, stuff, "AccuracyShort");
            Stat(j, thing, stuff, "AccuracyMedium");
            Stat(j, thing, stuff, "AccuracyLong");
            Stat(j, thing, stuff, "MarketValue");
            Stat(j, thing, stuff, "WorkToMake");
            Stat(j, thing, stuff, "Mass");
            j.EndObject();
        }

        private static void WriteVerb(Json j, ThingDef thing, ThingDef stuff, VerbProperties verb,
            List<string> unmeasured, DefWalker walker)
        {
            if (verb == null)
                return;

            j.BeginObject();
            j.Name("label"); j.Value(verb.label);
            j.Name("class"); j.Value(verb.verbClass?.FullName);
            j.Name("melee"); j.Value(verb.IsMeleeAttack);
            j.Name("launchesProjectile"); j.Value(verb.LaunchesProjectile);
            j.Name("primary"); j.Value(verb.isPrimary);
            j.Name("manualOnly"); j.Value(verb.onlyManualCast);
            j.Name("violent"); j.Value(verb.violent);
            j.Name("range"); j.Number(verb.range);
            j.Name("minRange"); j.Number(verb.minRange);
            j.Name("warmup"); j.Number(verb.warmupTime);
            j.Name("cooldown"); j.Number(verb.defaultCooldownTime);
            j.Name("burst"); j.Number(verb.burstShotCount);
            j.Name("burstTicks"); j.Number(verb.ticksBetweenBurstShots);
            j.Name("forcedMissRadius"); j.Number(verb.ForcedMissRadius);
            j.Name("accuracy");
            j.BeginObject();
            j.Name("touch"); j.Number(verb.accuracyTouch);
            j.Name("short"); j.Number(verb.accuracyShort);
            j.Name("medium"); j.Number(verb.accuracyMedium);
            j.Name("long"); j.Number(verb.accuracyLong);
            j.EndObject();
            if (verb.consumeFuelPerShot > 0f)
            {
                j.Name("fuelPerShot"); j.Number(verb.consumeFuelPerShot);
            }

            if (verb.meleeDamageDef != null)
            {
                j.Name("meleeDamageDef"); j.Value(verb.meleeDamageDef.defName);
                j.Name("meleeDamageBase"); j.Number(verb.meleeDamageBaseAmount);
                j.Name("meleeArmorPenetrationBase"); j.Number(verb.meleeArmorPenetrationBase);
            }
            if (verb.beamDamageDef != null)
            {
                // A beam's damage is the damage def's own default, applied once per cell the beam
                // sweeps, so the width and the range it reaches full width at decide how much of a
                // burst lands on one pawn.
                j.Name("beamDamageDef"); j.Value(verb.beamDamageDef.defName);
                j.Name("beamDamage"); j.Number(verb.beamDamageDef.defaultDamage);
                j.Name("beamWidth"); j.Number(verb.beamWidth);
                j.Name("beamFullWidthRange"); j.Number(verb.beamFullWidthRange);
                unmeasured.Add($"beamDamage:{verb.beamDamageDef.defName}");
            }

            ThingDef projectile = verb.defaultProjectile;
            if (projectile?.projectile != null)
                WriteProjectile(j, thing, stuff, projectile, unmeasured, walker);
            else if (verb.LaunchesProjectile && !verb.IsMeleeAttack)
                unmeasured.Add($"noProjectile ({verb.verbClass?.Name})");

            if (verb.verbClass != null && verb.verbClass.Assembly != CoreAssembly)
                unmeasured.Add($"moddedVerbClass:{verb.verbClass.FullName}");
            else if (!verb.IsMeleeAttack && !verb.LaunchesProjectile)
                unmeasured.Add($"nonProjectileVerb:{verb.verbClass?.FullName}");

            j.EndObject();
        }

        private static void WriteProjectile(Json j, ThingDef thing, ThingDef stuff, ThingDef projectile,
            List<string> unmeasured, DefWalker walker)
        {
            ProjectileProperties props = projectile.projectile;

            j.Name("projectile");
            j.BeginObject();
            j.Name("def"); j.Value(projectile.defName);
            j.Name("class"); j.Value(projectile.thingClass?.FullName);
            j.Name("damageDef"); j.Value(props.damageDef?.defName);

            // Resolved, not the raw field: damageAmountBase is private and the weapon's damage
            // multiplier is applied on top of it.
            try
            {
                j.Name("damage"); j.Number(props.GetDamageAmount(thing, stuff));
            }
            catch { j.Null(); }

            try
            {
                float apMultiplier = 1f;
                StatDef apStat = Named("RangedWeapon_ArmorPenetrationMultiplier");
                if (apStat != null)
                    apMultiplier = thing.GetStatValueAbstract(apStat, stuff);
                j.Name("armorPenetration"); j.Number(props.GetArmorPenetration() * apMultiplier);
            }
            catch { j.Null(); }

            j.Name("speed"); j.Number(props.speed);
            j.Name("stoppingPower"); j.Number(props.stoppingPower);
            if (props.flyOverhead)
            {
                j.Name("flyOverhead"); j.Value(true);
            }
            if (props.ai_IsIncendiary)
            {
                j.Name("incendiary"); j.Value(true);
            }
            // Radius is a fact, not a verdict. A one-cell blast damages a single pawn for the same
            // amount a bullet would, so whether an explosion makes a weapon unrankable is the
            // report's call to make from the radius and the delivery, not this dump's.
            if (props.explosionRadius > 0f)
            {
                j.Name("explosionRadius"); j.Number(props.explosionRadius);
            }
            if (props.postExplosionSpawnThingDef != null)
            {
                j.Name("postExplosionSpawn"); j.Value(props.postExplosionSpawnThingDef.defName);
            }
            if (props.preExplosionSpawnThingDef != null)
            {
                j.Name("preExplosionSpawn"); j.Value(props.preExplosionSpawnThingDef.defName);
            }
            if (!props.extraDamages.NullOrEmpty())
            {
                j.Name("extraDamages");
                j.BeginArray();
                foreach (ExtraDamage extra in props.extraDamages)
                {
                    j.BeginObject();
                    j.Name("def"); j.Value(extra.def?.defName);
                    j.Name("amount"); j.Number(extra.amount);
                    j.Name("chance"); j.Number(extra.chance);
                    j.Name("armorPenetration"); j.Number(extra.armorPenetration);
                    j.EndObject();
                }
                j.EndArray();
                unmeasured.Add($"extraDamages:{props.extraDamages.Count} ({projectile.defName})");
            }

            // A modded ProjectileProperties subclass carries its own fields, and they are where the
            // real behaviour lives: a matchlock's shotCount turns one pull of the trigger into nine
            // pellets. Write whatever the subclass declares rather than guessing which fields matter.
            if (props.GetType() != typeof(ProjectileProperties) && walker != null)
            {
                j.Name("properties");
                j.BeginObject();
                j.Name("$type"); j.Value(props.GetType().FullName);
                walker.WriteFields(props, 3);
                j.EndObject();
            }
            j.EndObject();

            if (projectile.thingClass != null && projectile.thingClass.Assembly != CoreAssembly)
                unmeasured.Add($"moddedProjectileClass:{projectile.thingClass.FullName}");
        }

        // ---- apparel ------------------------------------------------------------------------

        public static void WriteApparel(Json j, ThingDef thing)
        {
            ThingDef stuff = thing.MadeFromStuff ? GenStuff.DefaultStuffFor(thing) : null;

            // Named "armor" rather than "apparel": ThingDef.apparel is a public field, so the
            // reflective walk already writes an "apparel" key and two of them would collide.
            j.Name("armor");
            j.BeginObject();

            // The share of a human body the piece protects. Two items with the same armor rating are
            // not equal when one is a helmet and the other a duster.
            try
            {
                j.Name("humanBodyCoverage"); j.Number(thing.apparel.HumanBodyCoverage);
            }
            catch { j.Null(); }

            if (stuff != null)
            {
                j.Name("defaultStuff"); j.Value(stuff.defName);
            }

            j.Name("stats");
            ApparelStats(j, thing, null);
            if (stuff != null)
            {
                j.Name("stuffedStats");
                ApparelStats(j, thing, stuff);
            }

            j.EndObject();
        }

        private static void ApparelStats(Json j, ThingDef thing, ThingDef stuff)
        {
            j.BeginObject();
            Stat(j, thing, stuff, "ArmorRating_Sharp");
            Stat(j, thing, stuff, "ArmorRating_Blunt");
            Stat(j, thing, stuff, "ArmorRating_Heat");
            Stat(j, thing, stuff, "Insulation_Cold");
            Stat(j, thing, stuff, "Insulation_Heat");
            // Vacuum, toxic and toxic-environment resistance are pawn stats, not apparel ones:
            // resolving them against the apparel def returns zero for everything. Apparel grants
            // them through equippedStatOffsets, which the reflective walk already dumps raw.
            Stat(j, thing, stuff, "MarketValue");
            Stat(j, thing, stuff, "WorkToMake");
            Stat(j, thing, stuff, "Mass");
            j.EndObject();
        }

        // ---- shared -------------------------------------------------------------------------

        /// <summary>
        /// Stats are looked up by name rather than through StatDefOf: several of these belong to a
        /// DLC, and a dump must not throw in a modlist that hasn't got it.
        /// </summary>
        private static StatDef Named(string defName)
        {
            if (StatCache.TryGetValue(defName, out StatDef stat))
                return stat;
            stat = DefDatabase<StatDef>.GetNamedSilentFail(defName);
            StatCache[defName] = stat;
            return stat;
        }

        private static void Stat(Json j, ThingDef thing, ThingDef stuff, string defName)
        {
            StatDef stat = Named(defName);
            if (stat == null)
                return;
            try
            {
                j.Name(defName);
                j.Number(thing.GetStatValueAbstract(stat, stuff));
            }
            catch
            {
                // Some modded stat parts assume a spawned thing; a missing value beats a failed dump.
                j.Null();
            }
        }
    }
}
