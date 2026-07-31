// Offline analyzer for ThingDump.json + AcquisitionDump.json (throwaway, gitignored output).
// Ranks every loaded weapon and every piece of apparel against the vanilla ceiling for its tech
// level, so the arsenal work has a measured starting point instead of an impression.
//
// Ranged DPS is computed here rather than in the dump, because the formula is the part that gets
// argued about and re-running this takes a second:
//     cycle   = warmup x warmupMultiplier + cooldown + (burst - 1) x burstTicks / 60
//     dps     = damage x burst / cycle
//     effDps  = dps x the weapon's own medium-range accuracy
// Weapon-side accuracy only: the shooter's skill scales every weapon alike, so it cannot change an
// ordering. Melee DPS is the game's own MeleeWeapon_AverageDPS, resolved with the default stuff.
//
// The dump records why a weapon's damage may be understated, and the two kinds are not equal:
//   hard — no projectile, a beam, a projectile class from a mod assembly, or indirect fire that
//          cannot be aimed at a single target. Nothing comparable is left, so these are ranked
//          nowhere and sit in the "not measured" queue.
//   soft — a modded verb class, extra damages, a tool that applies a hediff, an explosion wider
//          than one cell. The visible number is real but a floor, so these are ranked with a flag.
//          A one-cell blast is not flagged at all: the explosion worker deals one damage instance
//          per thing, so against a single pawn it is a bullet with a flash.
//
// Emits:
//   weapons.md — ceilings per tech level, everything above them, the full tables, the two queues
//   armor.md   — the same for apparel, plus vacuum resistance and insulation outliers
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string tmp = Path.Combine(home, "AppData", "LocalLow", "Ludeon Studios",
    "RimWorld by Ludeon Studios", "RebalancePatches", "tmp");

string thingPath = args.Length > 0 ? args[0] : Path.Combine(tmp, "ThingDump.json");
string acqPath = args.Length > 1 ? args[1] : Path.Combine(tmp, "AcquisitionDump.json");
string outDir = args.Length > 2 ? args[2] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "out");
outDir = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDir);

string[] techOrder = { "Undefined", "Animal", "Neolithic", "Medieval", "Industrial", "Spacer", "Ultra", "Archotech" };

// A reason the visible damage is wrong, versus a reason there is no visible damage at all.
var hardReasons = new HashSet<string> { "noProjectile", "beamDamage", "moddedProjectileClass", "indirectFire" };

const string EltexExtension = "EltexWeaponry.ModExtension_PsychicDamage";

// Projectile classes from mod assemblies, decompiled and checked one by one. A class here is one
// whose damage against a single human is the projectile's own damage after all: the override is
// cosmetic, or adds an effect on top, or spreads over extra targets it hits *after* the first.
// Anything not in this set stays unreadable and out of the rankings.
var plainProjectiles = new Dictionary<string, string>
{
    ["VEF.Weapons.LaserBeam"] = "",
    ["VFEMedieval.WarbowArrow"] = "",
    ["VEF.Weapons.Projectile_TopAttackMissile"] = "",
    ["VEF.Weapons.GaussProjectile"] = "first target only; later targets in the sweep take less",
    ["VFEMedieval.MatchlockProjectile"] = "damage falls off with distance; measured at half range",
    ["VEF.Weapons.FlamethrowProjectile"] = "plus a fire on the target, which no static figure carries",
    ["VanillaQuestsExpandedCryptoforge.Bullet_Cryptobolter"] = "plus a slowdown hediff",
    ["AlphaGenes.BlackHydraBullet"] = "plus a lightning strike nearby",
};

// Verb classes from mod assemblies that were read and do nothing to the damage: either they are the
// vanilla behaviour plus bookkeeping, or their contribution is already modelled above.
var benignVerbClasses = new HashSet<string>
{
    "VEF.Weapons.Verb_Shoot",            // vanilla Verb_Shoot plus XP and weapon wear
    "VFEMedieval.Verb_ShootWithSmoke",   // the pellets it fires are counted from the projectile
};

// Projectiles that deal no hit-point damage at all. Not unknown - zero, for a stated reason.
var zeroDamageProjectiles = new Dictionary<string, string>
{
    ["BigAndSmall.BS_StatusBullet"] = "applies a hediff, never calls the damage path",
    ["VEF.Weapons.Projectile_SmokeGrenade"] = "smoke payload, damage def deals 0",
};

using var thingDoc = JsonDocument.Parse(File.ReadAllText(thingPath));

// ---- acquisition: can it be bought, is it handed out ------------------------------------------

var traderCount = new Dictionary<string, int>();
var rewardCount = new Dictionary<string, int>();
if (File.Exists(acqPath))
{
    using var acqDoc = JsonDocument.Parse(File.ReadAllText(acqPath));
    if (acqDoc.RootElement.TryGetProperty("traders", out var traders))
        foreach (var entry in traders.EnumerateObject())
            traderCount[entry.Name] = entry.Value.GetArrayLength();
    if (acqDoc.RootElement.TryGetProperty("references", out var refs))
        foreach (var entry in refs.EnumerateObject())
        {
            int colon = entry.Name.IndexOf(':');
            string def = colon >= 0 ? entry.Name[(colon + 1)..] : entry.Name;
            rewardCount[def] = entry.Value.GetArrayLength();
        }
}

// ---- read the dump ----------------------------------------------------------------------------

var weapons = new List<Weapon>();
var apparel = new List<Armor>();
int noCombatBlock = 0;

foreach (var t in thingDoc.RootElement.GetProperty("things").EnumerateArray())
{
    string defName = Str(t, "defName");
    if (defName == null) continue;

    bool hasWeapon = t.TryGetProperty("weapon", out var w) && w.ValueKind == JsonValueKind.Object;
    bool hasArmor = t.TryGetProperty("armor", out var a) && a.ValueKind == JsonValueKind.Object;
    if (!hasWeapon && !hasArmor)
    {
        // Carries the fields a weapon or apparel has, but no resolved block: a stale dump.
        if (t.TryGetProperty("tools", out _) || t.TryGetProperty("apparel", out _))
            noCombatBlock++;
        continue;
    }

    string mod = ShortMod(Str(t, "mod"));
    bool vanilla = (Str(t, "mod") ?? "").Contains("[ludeon.rimworld", StringComparison.OrdinalIgnoreCase);
    string tech = Str(t, "techLevel") ?? "Undefined";
    var stats = t.TryGetProperty("resolvedStats", out var rs) ? rs : default;
    float value = Num(stats, "MarketValue");
    float work = Num(stats, "WorkToMake");
    float mass = Num(stats, "Mass");

    if (hasWeapon)
        weapons.Add(ReadWeapon(t, w, defName, mod, vanilla, tech, value, work, mass));
    if (hasArmor)
        apparel.Add(ReadArmor(t, a, defName, mod, vanilla, tech, value, work, mass));
}

// ---- weapons.md ---------------------------------------------------------------------------------

var ranged = weapons.Where(x => x.Ranged && !x.Hidden && x.Measured && !x.NoDamage)
    .OrderByDescending(x => x.EffDps).ToList();
var melee = weapons.Where(x => !x.Ranged && !x.Hidden && x.Measured && !x.NoDamage)
    .OrderByDescending(x => x.EffDps).ToList();
var unmeasured = weapons.Where(x => !x.Hidden && !x.Measured).ToList();
var partial = weapons.Where(x => !x.Hidden && x.Measured && x.Soft.Count > 0).ToList();
var harmless = weapons.Where(x => !x.Hidden && x.NoDamage).ToList();
var hidden = weapons.Where(x => x.Hidden).ToList();

var wb = new StringBuilder();
wb.AppendLine("# Weapons");
wb.AppendLine();
wb.AppendLine($"{weapons.Count} weapons in the dump: {ranged.Count} ranged and {melee.Count} melee measured, " +
              $"{unmeasured.Count} not measurable, {partial.Count} measured but incomplete, " +
              $"{harmless.Count} that deal no hit points at all, {hidden.Count} turret or hidden guns held back.");
wb.AppendLine();
wb.AppendLine("`dps` is damage x burst over the full warmup + cooldown + burst cycle. `eff` multiplies it by the");
wb.AppendLine("weapon's own medium-range accuracy; the shooter's skill is left out because it scales every weapon");
wb.AppendLine("alike. Melee uses the game's MeleeWeapon_AverageDPS with the weapon's default stuff.");
wb.AppendLine();
wb.AppendLine("Weapons with no readable damage at all are in [Not measured](#not-measured) and appear in no");
wb.AppendLine("ranking. Weapons whose visible damage is real but incomplete are ranked, and flagged in the `!`");
wb.AppendLine("column; see [Measured but incomplete](#measured-but-incomplete).");
wb.AppendLine();
wb.AppendLine("`est` is the DPS after the damage this report can reconstruct from a mod's code: Eltex Weaponry's");
wb.AppendLine("psychic multiplier and flat bonus, and the melee extra damages the game's own DPS stat leaves out.");
wb.AppendLine("Where no such mechanism applies it equals `dps`. Every ranking, ceiling and ratio below uses `est`,");
wb.AppendLine("so a weapon whose real damage is hidden is compared on its real damage; see");
wb.AppendLine("[Estimated above the card](#estimated-above-the-card) for what was added and on what assumptions.");
wb.AppendLine();
wb.AppendLine("A mod of `?` means the def was injected by a patch rather than defined in a mod's own files, so the");
wb.AppendLine("game records no source for it. The ceiling for each tech level is the strongest *craftable* vanilla");
wb.AppendLine("weapon there, craftable meaning a recipe exists in this modlist; an uncraftable trophy is used only");
wb.AppendLine("when the tech level has nothing else, and is marked.");
wb.AppendLine();

CeilingSection(wb, "Ranged", ranged);
CeilingSection(wb, "Melee", melee);

wb.AppendLine("## Above the vanilla ceiling");
wb.AppendLine();
wb.AppendLine("Modded weapons that beat the strongest measured vanilla weapon of their own tech level.");
wb.AppendLine("`x` is the ratio to that ceiling. This is the list the arsenal pass works from.");
wb.AppendLine();
AboveCeiling(wb, "Ranged", ranged);
AboveCeiling(wb, "Melee", melee);

wb.AppendLine("## Cheap for what they do");
wb.AppendLine();
wb.AppendLine("Highest effective DPS per 100 silver of market value, among weapons a trader can stock.");
wb.AppendLine("A strong weapon that is also cheap and buyable is a pricing hole, not a reward.");
wb.AppendLine();
wb.AppendLine("| Weapon | Mod | Tech | Eff DPS | Value | Eff/100s | Traders |");
wb.AppendLine("|---|---|---|---|---|---|---|");
foreach (var x in weapons.Where(x => !x.Hidden && x.Measured && x.Value > 0 && x.Traders > 0)
             .OrderByDescending(x => x.EffDps / x.Value * 100f).Take(40))
    wb.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.EffDps:0.0} | {x.Value:0} | " +
                  $"{x.EffDps / x.Value * 100f:0.00} | {x.Traders} |");
wb.AppendLine();

wb.AppendLine("## Estimated above the card");
wb.AppendLine();
wb.AppendLine("Weapons whose real single-target damage is higher than their stat card, with what was added.");
wb.AppendLine("Eltex assumes a baseliner shooter holding this weapon at normal quality against an ordinary human;");
wb.AppendLine("a psycaster wielder or a mechanoid target moves it in either direction. Melee extra damages assume");
wb.AppendLine("the game's own tool weighting (expected damage squared x chance factor).");
wb.AppendLine();
wb.AppendLine("| Weapon | Mod | Tech | Card DPS | Est DPS | x | Mechanism |");
wb.AppendLine("|---|---|---|---|---|---|---|");
foreach (var x in weapons.Where(x => x.Estimated && !x.Hidden && !x.NoDamage)
             .OrderByDescending(x => x.Dps > 0f ? x.EstDps / x.Dps : float.MaxValue))
    wb.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Dps:0.0} | {x.EstDps:0.0} | " +
                  // A beam has no card figure to be a multiple of: it fires no projectile at all.
                  $"{(x.Dps > 0f ? (x.EstDps / x.Dps).ToString("0.00") : "-")} | " +
                  $"{Escape(string.Join("; ", x.Estimates))} |");
wb.AppendLine();

wb.AppendLine("## Full ranged table");
wb.AppendLine();
wb.AppendLine("| Weapon | Mod | Tech | Dmg | AP | Range | Burst | Cycle | DPS | Est | Eff | Acc(m) | Value | Buy | Given | ! |");
wb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var x in ranged)
    wb.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Damage:0.#} | {x.ArmorPen:P0} | " +
                  $"{x.Range:0.#} | {x.Burst} | {x.Cycle:0.00} | {x.Dps:0.0} | {x.EstDps:0.0} | {x.EffDps:0.0} | " +
                  $"{x.Accuracy:P0} | {x.Value:0} | {x.Traders} | {x.Rewards} | {x.Flags} |");
wb.AppendLine();

wb.AppendLine("## Full melee table");
wb.AppendLine();
wb.AppendLine("| Weapon | Mod | Tech | DPS | Est | AP | Stuff | Value | Buy | Given | ! |");
wb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var x in melee)
    wb.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Dps:0.0} | {x.EstDps:0.0} | " +
                  $"{x.ArmorPen:P0} | {x.Stuff ?? "-"} | {x.Value:0} | {x.Traders} | {x.Rewards} | {x.Flags} |");
wb.AppendLine();

if (harmless.Count > 0)
{
    wb.AppendLine("## No hit-point damage");
    wb.AppendLine();
    wb.AppendLine("These do not injure anyone, which is a measurement rather than a gap. They are in no ranking.");
    wb.AppendLine();
    wb.AppendLine("| Weapon | Mod | Tech | Why | Value |");
    wb.AppendLine("|---|---|---|---|---|");
    foreach (var x in harmless.OrderBy(x => x.Mod, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.DefName, StringComparer.Ordinal))
        wb.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {Escape(string.Join("; ", x.Estimates))} | {x.Value:0} |");
    wb.AppendLine();
}

ReasonSection(wb, "Not measured", unmeasured, x => x.Hard,
    "Nothing comparable is left on these: indirect fire that cannot be aimed at one pawn, or a projectile",
    "class from a mod assembly that has not been read yet. They are in no ranking above.");

ReasonSection(wb, "Measured but incomplete", partial, x => x.Soft,
    "The visible damage is real, but something adds more on top that the stat card never shows. These are",
    "ranked above; treat their numbers as a floor.");

File.WriteAllText(Path.Combine(outDir, "weapons.md"), wb.ToString());

// ---- armor.md -------------------------------------------------------------------------------

var wearable = apparel.Where(x => x.Coverage > 0f).OrderByDescending(x => x.Shielding).ToList();

var ab = new StringBuilder();
ab.AppendLine("# Apparel and armor");
ab.AppendLine();
ab.AppendLine($"{apparel.Count} apparel defs, {wearable.Count} of them covering a human body.");
ab.AppendLine();
ab.AppendLine("`shield` is a single ranking number: `coverage x (0.6 x sharp + 0.4 x blunt)`. Sharp is weighted");
ab.AppendLine("higher because most incoming raid damage is sharp, and coverage is in it because a helmet and a");
ab.AppendLine("duster with the same rating are not the same protection. It is a sorting heuristic, not a truth.");
ab.AppendLine("Ratings are resolved with each item's default stuff where it has one.");
ab.AppendLine();

ab.AppendLine("## Ceiling by layer");
ab.AppendLine();
ab.AppendLine("Strongest vanilla piece per outermost layer, and how many modded pieces beat it.");
ab.AppendLine();
ab.AppendLine("| Layer | Count | Vanilla best | Shield | Modded above it | Best modded |");
ab.AppendLine("|---|---|---|---|---|---|");
foreach (var group in wearable.GroupBy(x => x.Layer).OrderByDescending(g => g.Count()))
{
    var best = BestVanilla(group);
    var above = group.Where(x => !x.Vanilla && best != null && x.Shielding > best.Shielding)
        .OrderByDescending(x => x.Shielding).ToList();
    ab.AppendLine($"| {group.Key} | {group.Count()} | {(best == null ? "none" : $"`{best.DefName}`")} | " +
                  $"{(best?.Shielding ?? 0f):0.00} | {above.Count} | " +
                  $"{(above.Count == 0 ? "-" : $"`{above[0].DefName}` {above[0].Shielding:0.00}")} |");
}
ab.AppendLine();

ab.AppendLine("## Above the vanilla ceiling");
ab.AppendLine();
ab.AppendLine("Modded apparel out-protecting the best vanilla piece on the same layer.");
ab.AppendLine();
ab.AppendLine("| Apparel | Mod | Tech | Layer | Sharp | Blunt | Heat | Cover | Shield | x | Value | Buy |");
ab.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var group in wearable.GroupBy(x => x.Layer))
{
    var best = BestVanilla(group);
    if (best == null) continue;
    foreach (var x in group.Where(x => !x.Vanilla && x.Shielding > best.Shielding)
                 .OrderByDescending(x => x.Shielding / best.Shielding))
        ab.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Layer} | {x.Sharp:P0} | {x.Blunt:P0} | " +
                      $"{x.Heat:P0} | {x.Coverage:P0} | {x.Shielding:0.00} | {x.Shielding / best.Shielding:0.00} | " +
                      $"{x.Value:0} | {x.Traders} |");
}
ab.AppendLine();

ab.AppendLine("## Vacuum resistance");
ab.AppendLine();
ab.AppendLine("Everything that resists vacuum at all, strongest first. Full resistance on anything short of");
ab.AppendLine("endgame armor is the pattern the vacuum trims exist to catch.");
ab.AppendLine();
ab.AppendLine("| Apparel | Mod | Tech | Layer | Vacuum | Toxic env | Sharp | Cover | Value | Buy |");
ab.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
foreach (var x in apparel.Where(x => x.Vacuum > 0f).OrderByDescending(x => x.Vacuum).ThenBy(x => x.Value))
    ab.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Layer} | {x.Vacuum:P0} | " +
                  $"{x.ToxicEnv:P0} | {x.Sharp:P0} | {x.Coverage:P0} | {x.Value:0} | {x.Traders} |");
ab.AppendLine();

ab.AppendLine("## Insulation outliers");
ab.AppendLine();
ab.AppendLine("| Apparel | Mod | Cold | Heat | Layer | Value |");
ab.AppendLine("|---|---|---|---|---|---|");
foreach (var x in apparel.OrderByDescending(x => Math.Max(x.Cold, x.HeatIns)).Take(30))
    ab.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Cold:0.#} | {x.HeatIns:0.#} | {x.Layer} | {x.Value:0} |");
ab.AppendLine();

ab.AppendLine("## Protection per silver");
ab.AppendLine();
ab.AppendLine("| Apparel | Mod | Tech | Shield | Value | Shield/100s | Mass | Buy |");
ab.AppendLine("|---|---|---|---|---|---|---|---|");
foreach (var x in wearable.Where(x => x.Value > 0 && x.Shielding > 0.05f)
             .OrderByDescending(x => x.Shielding / x.Value * 100f).Take(40))
    ab.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Shielding:0.00} | {x.Value:0} | " +
                  $"{x.Shielding / x.Value * 100f:0.00} | {x.Mass:0.0} | {x.Traders} |");
ab.AppendLine();

ab.AppendLine("## Full apparel table");
ab.AppendLine();
ab.AppendLine("| Apparel | Mod | Tech | Layer | Sharp | Blunt | Heat | Cover | Shield | Cold | Heat ins | Vac | Mass | Value |");
ab.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var x in wearable)
    ab.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Layer} | {x.Sharp:P0} | {x.Blunt:P0} | " +
                  $"{x.Heat:P0} | {x.Coverage:P0} | {x.Shielding:0.00} | {x.Cold:0.#} | {x.HeatIns:0.#} | " +
                  $"{x.Vacuum:P0} | {x.Mass:0.0} | {x.Value:0} |");

File.WriteAllText(Path.Combine(outDir, "armor.md"), ab.ToString());

// ---- psygear.md ---------------------------------------------------------------------------------
// The psycaster-gear audit: everything that grants a psychic stat while worn or wielded, plus the
// prestige family, enumerated by relationship (offsets, tags) rather than by name.

var psyApparel = apparel.Where(x => x.AnyPsychic || x.Prestige || x.PsyTradeTag)
    .OrderByDescending(x => x.PsySens).ThenByDescending(x => x.Value).ToList();
var psyWeapons = weapons.Where(x => x.AnyPsychic || x.PsyTradeTag)
    .OrderByDescending(x => x.PsySens).ThenByDescending(x => x.Value).ToList();

var pb = new StringBuilder();
pb.AppendLine("# Psycaster gear");
pb.AppendLine();
pb.AppendLine($"{psyApparel.Count} apparel and {psyWeapons.Count} weapons carry a psychic stat, the " +
              "PrestigeCombatGear tag or a psychic trade tag.");
pb.AppendLine();
pb.AppendLine("`sens (q)` is the quality-scaled PsychicSensitivityOffset stat (0.5x awful to 1.5x " +
              "legendary); `sens (flat)` is a plain PsychicSensitivity equipped offset, untouched by " +
              "quality. `conflict` marks the layers where armor and psychic clothing compete for the " +
              "slot (Shell, Overhead) - the slots where the robed and armored archetypes actually " +
              "differ; OnSkin and Middle pieces stack under power armor.");
pb.AppendLine();
pb.AppendLine("## Apparel");
pb.AppendLine();
pb.AppendLine("| Def | Mod | Tech | Layer | Conflict | Sharp | Sens (q) | Sens (flat) | Heat rec | " +
              "Heat max | Medit | Focus cost | Prestige | Value | Craft | Trade | Reward |");
pb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var x in psyApparel)
{
    bool conflict = x.Layer is "Shell" or "Overhead";
    pb.AppendLine($"| `{x.DefName}` {Escape(x.Label)} | {Escape(x.Mod)} | {x.Tech} | {x.Layer} | " +
                  $"{(conflict ? "yes" : "")} | {x.Sharp:P0} | {Off(x.PsySensQ)} | {Off(x.PsySensFlat)} | " +
                  $"{Off(x.PsyHeatRecovery)} | {Off(x.PsyHeatMax)} | {Off(x.PsyMeditation)} | " +
                  $"{Off(x.PsyFocusCost)} | {(x.Prestige ? "yes" : "")} | {x.Value:0} | " +
                  $"{(x.Craftable ? "yes" : "")} | {x.Traders} | {x.Rewards} |");
}
pb.AppendLine();
pb.AppendLine("## Weapons");
pb.AppendLine();
pb.AppendLine("| Def | Mod | Tech | Sens (q) | Sens (flat) | Heat rec | Medit | Focus cost | " +
              "Mechanism | Value | Craft |");
pb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var x in psyWeapons)
    pb.AppendLine($"| `{x.DefName}` {Escape(x.Label)} | {Escape(x.Mod)} | {x.Tech} | {Off(x.PsySensQ)} | " +
                  $"{Off(x.PsySensFlat)} | {Off(x.PsyHeatRecovery)} | {Off(x.PsyMeditation)} | " +
                  $"{Off(x.PsyFocusCost)} | {Escape(string.Join("; ", x.Estimates))} | {x.Value:0} | " +
                  $"{(x.Craftable ? "yes" : "")} |");
pb.AppendLine();
pb.AppendLine("## Sensitivity by slot");
pb.AppendLine();
pb.AppendLine("What one pawn can stack: the best sensitivity total per layer, armored build vs robed " +
              "build. Belts and packs are listed but rarely psychic.");
pb.AppendLine();
pb.AppendLine("| Layer | Best piece | Sens | Best armored alternative (sharp >= 20%) | Sens | Sharp |");
pb.AppendLine("|---|---|---|---|---|---|");
foreach (var group in psyApparel.GroupBy(x => x.Layer).OrderBy(g => g.Key))
{
    var best = group.OrderByDescending(x => x.PsySens).First();
    var armored = group.Where(x => x.Sharp >= 0.2f).OrderByDescending(x => x.PsySens).FirstOrDefault();
    pb.AppendLine($"| {group.Key} | `{best.DefName}` | {Off(best.PsySens)} | " +
                  $"{(armored == null ? "-" : $"`{armored.DefName}`")} | " +
                  $"{(armored == null ? "-" : Off(armored.PsySens))} | " +
                  $"{(armored == null ? "-" : armored.Sharp.ToString("P0"))} |");
}
File.WriteAllText(Path.Combine(outDir, "psygear.md"), pb.ToString());

static string Off(float v) => v == 0f ? "" : v.ToString("+0.###;-0.###");

Console.WriteLine($"{weapons.Count} weapons ({ranged.Count} ranged, {melee.Count} melee measured, " +
                  $"{unmeasured.Count} unmeasured, {partial.Count} partial), {apparel.Count} apparel -> {outDir}");
if (noCombatBlock > 0)
    Console.WriteLine($"{noCombatBlock} defs carry weapon or apparel fields but no resolved block: " +
                      "re-run the game with dev.thingdump on to refresh the dump.");

// ---- report sections ---------------------------------------------------------------------------

// Same rule as the weapon ceiling: a craftable vanilla piece is the yardstick, and an uncraftable
// one is only used when the layer has nothing else.
static Armor BestVanilla(IEnumerable<Armor> group)
{
    var vanilla = group.Where(x => x.Vanilla).ToList();
    return vanilla.Where(x => x.Craftable).OrderByDescending(x => x.Shielding).FirstOrDefault()
           ?? vanilla.OrderByDescending(x => x.Shielding).FirstOrDefault();
}

void CeilingSection(StringBuilder sb, string title, List<Weapon> list)
{
    sb.AppendLine($"## {title} ceiling by tech level");
    sb.AppendLine();
    sb.AppendLine("| Tech | Count | Vanilla best | Eff DPS | Modded best | Eff DPS | Above ceiling |");
    sb.AppendLine("|---|---|---|---|---|---|---|");
    foreach (string tech in techOrder.Where(t => list.Any(x => x.Tech == t)))
    {
        var atTech = list.Where(x => x.Tech == tech).ToList();
        var (ceiling, note) = Ceiling(atTech, list);
        var moddedBest = atTech.Where(x => !x.Vanilla).OrderByDescending(x => x.EffDps).FirstOrDefault();
        int above = ceiling == null ? 0 : atTech.Count(x => !x.Vanilla && x.EffDps > ceiling.EffDps);
        sb.AppendLine($"| {tech} | {atTech.Count} | {(ceiling == null ? "none" : $"`{ceiling.DefName}`{note}")} | " +
                      $"{(ceiling?.EffDps ?? 0f):0.0} | {(moddedBest == null ? "-" : $"`{moddedBest.DefName}`")} | " +
                      $"{(moddedBest?.EffDps ?? 0f):0.0} | {above} |");
    }
    sb.AppendLine();
}

// The yardstick is the strongest vanilla weapon the player can actually build at that tech level.
// Uncraftable trophies distort everything below them: Odyssey's alpha thrumbo horn out-damages every
// craftable melee weapon in the game and sits at Neolithic, which on its own would declare that no
// modded melee weapon anywhere is out of line.
(Weapon, string) Ceiling(List<Weapon> atTech, List<Weapon> all)
{
    var vanillaAtTech = atTech.Where(x => x.Vanilla).ToList();
    var craftable = vanillaAtTech.Where(x => x.Craftable).OrderByDescending(x => x.EffDps).FirstOrDefault();
    if (craftable != null)
        return (craftable, "");
    var anyAtTech = vanillaAtTech.OrderByDescending(x => x.EffDps).FirstOrDefault();
    if (anyAtTech != null)
        return (anyAtTech, " (no recipe)");

    var globalCraftable = all.Where(x => x.Vanilla && x.Craftable).OrderByDescending(x => x.EffDps).FirstOrDefault();
    if (globalCraftable != null)
        return (globalCraftable, " (global)");
    return (all.Where(x => x.Vanilla).OrderByDescending(x => x.EffDps).FirstOrDefault(), " (global, no recipe)");
}

void AboveCeiling(StringBuilder sb, string title, List<Weapon> list)
{
    sb.AppendLine($"### {title}");
    sb.AppendLine();
    sb.AppendLine("| Weapon | Mod | Tech | DPS | Eff | Ceiling | x | Value | Buy | Given | ! |");
    sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
    int rows = 0;
    foreach (string tech in techOrder)
    {
        var atTech = list.Where(x => x.Tech == tech).ToList();
        var (ceiling, _) = Ceiling(atTech, list);
        if (ceiling == null) continue;
        foreach (var x in atTech.Where(x => !x.Vanilla && x.EffDps > ceiling.EffDps)
                     .OrderByDescending(x => x.EffDps / ceiling.EffDps))
        {
            sb.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Dps:0.0} | {x.EffDps:0.0} | " +
                          $"`{ceiling.DefName}` {ceiling.EffDps:0.0} | {x.EffDps / ceiling.EffDps:0.00} | " +
                          $"{x.Value:0} | {x.Traders} | {x.Rewards} | {x.Flags} |");
            rows++;
        }
    }
    if (rows == 0)
        sb.AppendLine("| none | | | | | | | | | | |");
    sb.AppendLine();
}

static void ReasonSection(StringBuilder sb, string title, List<Weapon> list,
    Func<Weapon, List<string>> pick, string blurb1, string blurb2)
{
    sb.AppendLine($"## {title}");
    sb.AppendLine();
    sb.AppendLine(blurb1);
    sb.AppendLine(blurb2);
    sb.AppendLine();
    foreach (var group in list.SelectMany(x => pick(x).Select(r => (Kind: ReasonKind(r), Weapon: x)))
                 .GroupBy(p => p.Kind).OrderByDescending(g => g.Select(p => p.Weapon).Distinct().Count()))
    {
        var members = group.Select(p => p.Weapon).Distinct()
            .OrderBy(x => x.Mod, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.DefName, StringComparer.Ordinal).ToList();
        sb.AppendLine($"### {group.Key} ({members.Count})");
        sb.AppendLine();
        sb.AppendLine("| Weapon | Mod | Tech | Visible DPS | Detail |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var x in members)
            sb.AppendLine($"| `{x.DefName}` {x.Label} | {x.Mod} | {x.Tech} | {x.Dps:0.0} | " +
                          $"{Escape(string.Join("; ", pick(x).Where(r => ReasonKind(r) == group.Key)))} |");
        sb.AppendLine();
    }
}

// ---- parsing -------------------------------------------------------------------------------

Weapon ReadWeapon(JsonElement t, JsonElement w, string defName, string mod, bool vanilla, string tech,
    float value, float work, float mass)
{
    var stats = w.TryGetProperty("stuffedStats", out var ss) ? ss
        : w.TryGetProperty("stats", out var s) ? s : default;

    var weapon = new Weapon
    {
        DefName = defName,
        Label = Str(t, "label") ?? "",
        Mod = mod,
        Vanilla = vanilla,
        Tech = tech,
        Value = value,
        Work = work,
        Mass = mass,
        Ranged = Bool(w, "ranged"),
        Stuff = Str(w, "defaultStuff"),
        // Turret guns and one-use launchers are real weapons but not part of anyone's arsenal, and
        // they distort every ceiling they land in.
        Hidden = Bool(t, "menuHidden") || Bool(t, "destroyOnDrop")
                 || Strings(t, "weaponTags").Any(tag => tag.Contains("Turret", StringComparison.OrdinalIgnoreCase)),
        Traders = traderCount.TryGetValue(defName, out int tr) ? tr : 0,
        Rewards = rewardCount.TryGetValue(defName, out int rw) ? rw : 0,
        Craftable = t.TryGetProperty("producedBy", out var made) && made.ValueKind == JsonValueKind.Array
                    && made.GetArrayLength() > 0,
        PsySensQ = EquippedOffset(t, "PsychicSensitivityOffset"),
        PsySensFlat = EquippedOffset(t, "PsychicSensitivity"),
        PsyHeatRecovery = EquippedOffset(t, "PsychicEntropyRecoveryRate"),
        PsyMeditation = EquippedOffset(t, "MeditationFocusGain"),
        PsyFocusCost = EquippedOffset(t, "VPE_PsyfocusCostFactor"),
        PsyTradeTag = Strings(t, "tradeTags").Any(tag => tag.Contains("Psychic", StringComparison.OrdinalIgnoreCase)),
    };

    if (w.TryGetProperty("unmeasured", out var un) && un.ValueKind == JsonValueKind.Array)
        foreach (var r in un.EnumerateArray())
        {
            string reason = r.GetString() ?? "";
            // Older dumps flagged every explosion; this report decides that from the radius below.
            if (ReasonKind(reason) == "explosive")
                continue;
            if (ReasonKind(reason) == "moddedVerbClass"
                && benignVerbClasses.Contains(reason[(reason.IndexOf(':') + 1)..]))
                continue;
            if (hardReasons.Contains(ReasonKind(reason)))
                weapon.Hard.Add(reason);
            else
                weapon.Soft.Add(reason);
        }

    if (weapon.Ranged)
    {
        JsonElement? verb = PrimaryVerb(w, melee: false);
        if (verb is { } v)
        {
            var proj = v.TryGetProperty("projectile", out var p) ? p : default;
            weapon.Damage = Num(proj, "damage");
            weapon.ArmorPen = Num(proj, "armorPenetration");
            weapon.Burst = Math.Max(1, (int)Num(v, "burst"));

            float warmupMult = NumOr(stats, "RangedWeapon_WarmupMultiplier", 1f);
            float cooldownStat = Num(stats, "RangedWeapon_Cooldown");
            float cooldown = cooldownStat > 0f ? cooldownStat : Num(v, "cooldown");
            weapon.Cycle = Num(v, "warmup") * warmupMult + cooldown
                           + (weapon.Burst - 1) * Num(v, "burstTicks") / 60f;
            weapon.Range = Num(v, "range") * NumOr(stats, "RangedWeapon_RangeMultiplier", 1f);
            // Accuracy is a weapon stat in vanilla and a verb field in a few mods. The verb value is
            // only trusted when the stat is absent, since 1.0 is its default and would flatter it.
            weapon.Accuracy = stats.ValueKind == JsonValueKind.Object && stats.TryGetProperty("AccuracyMedium", out _)
                ? Num(stats, "AccuracyMedium")
                : (v.TryGetProperty("accuracy", out var acc) ? NumOr(acc, "medium", 1f) : 1f);
            weapon.Dps = weapon.Cycle > 0f ? weapon.Damage * weapon.Burst / weapon.Cycle : 0f;

            // An explosion is only a problem for a DPS number when it can reach more than the pawn
            // it hit. RimWorld's explosion worker emits one damage instance per thing, so a
            // one-cell blast is a bullet with a flash: the figure above is already right for it.
            // Above a cell, the number is a floor against a group. Indirect fire is different in
            // kind - a mortar shell that scatters and cannot be aimed at a duel has no comparable
            // DPS at all.
            float radius = Num(proj, "explosionRadius");
            // Vanilla grenades and launchers scatter by 1.9 cells; a gun with a hair of spread (the
            // impact rifle uses 0.04) is still aimed at a pawn. Half a cell is the dividing line.
            if (Bool(proj, "flyOverhead") || Num(v, "forcedMissRadius") >= 0.5f)
                weapon.Hard.Add($"indirectFire:{(radius > 0f ? radius.ToString("0.##") : "scatter")}");
            else if (radius > 1f)
                weapon.Soft.Add($"aoe:{radius:0.##}");

            ApplyProjectileClass(weapon, v, proj);
        }
        else
        {
            weapon.Hard.Add("noVerb");
        }
    }
    else
    {
        weapon.Dps = Num(stats, "MeleeWeapon_AverageDPS");
        weapon.ArmorPen = Num(stats, "MeleeWeapon_AverageArmorPenetration");
        weapon.Accuracy = 1f;
    }

    // A mod extension on a weapon is a hook for that mod's own code, and the code can do anything -
    // Eltex Weaponry multiplies every shot through ModExtension_PsychicDamage, which no field on the
    // def reveals. Flag the ones this report cannot interpret rather than pretend the stat card is
    // the whole weapon.
    float eltexBonus = 0f;
    bool eltex = false;
    foreach (var ext in Array(t, "modExtensions"))
    {
        string type = Str(ext, "$type");
        if (type == null || type.StartsWith("Verse.") || type.StartsWith("RimWorld."))
            continue;
        if (type == EltexExtension)
        {
            eltex = true;
            eltexBonus = Num(ext, "bonusDamage");
            continue;
        }
        weapon.Soft.Add($"modExt:{type}");
    }

    // ApplyProjectileClass may already have replaced the figure (a beam, a matchlock's pellets);
    // otherwise the card value is the starting point.
    if (weapon.Estimates.Count == 0)
        weapon.EstDps = weapon.Dps;

    if (eltex)
        ApplyEltex(weapon, t, eltexBonus);

    // Extra melee damages are applied on every hit, and MeleeWeapon_AverageDPS provably never reads
    // them: its worker averages AdjustedMeleeDamageAmount and AdjustedCooldown per tool and nothing
    // else. So a hammer with a 9-damage frostbite rider reads on the card as though it had none.
    float extraMelee = ExtraMeleeDps(t);
    if (extraMelee > 0f && !weapon.Ranged)
    {
        weapon.EstDps += extraMelee;
        weapon.Estimates.Add($"tool extra damages +{extraMelee:0.0} dps");
    }

    // A weapon that reached zero without any of the known reasons is still not measured; say so
    // rather than ranking it last. A weapon known to deal no damage is a different statement.
    if (weapon.Dps <= 0f && weapon.EstDps <= 0f && weapon.Hard.Count == 0 && !weapon.NoDamage)
        weapon.Hard.Add("noDamage");
    return weapon;
}

Armor ReadArmor(JsonElement t, JsonElement a, string defName, string mod, bool vanilla, string tech,
    float value, float work, float mass)
{
    var stats = a.TryGetProperty("stuffedStats", out var ss) ? ss
        : a.TryGetProperty("stats", out var s) ? s : default;
    var props = t.TryGetProperty("apparel", out var ap) ? ap : default;

    return new Armor
    {
        DefName = defName,
        Label = Str(t, "label") ?? "",
        Mod = mod,
        Vanilla = vanilla,
        Tech = tech,
        Value = value,
        Work = work,
        Mass = mass,
        Coverage = Num(a, "humanBodyCoverage"),
        Sharp = Num(stats, "ArmorRating_Sharp"),
        Blunt = Num(stats, "ArmorRating_Blunt"),
        Heat = Num(stats, "ArmorRating_Heat"),
        Cold = Num(stats, "Insulation_Cold"),
        HeatIns = Num(stats, "Insulation_Heat"),
        // Vacuum and toxic resistance are pawn stats the apparel offsets while worn, so they are in
        // equippedStatOffsets rather than anywhere on the apparel's own stat card.
        Vacuum = EquippedOffset(t, "VacuumResistance"),
        ToxicEnv = EquippedOffset(t, "ToxicEnvironmentResistance"),
        // Outermost layer, which is how the game itself decides what a piece is.
        Layer = Strings(props, "layers").LastOrDefault() ?? "-",
        Traders = traderCount.TryGetValue(defName, out int tr) ? tr : 0,
        Rewards = rewardCount.TryGetValue(defName, out int rw) ? rw : 0,
        Craftable = t.TryGetProperty("producedBy", out var made) && made.ValueKind == JsonValueKind.Array
                    && made.GetArrayLength() > 0,
        PsySensQ = EquippedOffset(t, "PsychicSensitivityOffset"),
        PsySensFlat = EquippedOffset(t, "PsychicSensitivity"),
        PsyHeatRecovery = EquippedOffset(t, "PsychicEntropyRecoveryRate"),
        PsyHeatMax = EquippedOffset(t, "PsychicEntropyMax"),
        PsyMeditation = EquippedOffset(t, "MeditationFocusGain"),
        PsyFocusCost = EquippedOffset(t, "VPE_PsyfocusCostFactor"),
        Prestige = Strings(props, "tags").Contains("PrestigeCombatGear"),
        PsyTradeTag = Strings(t, "tradeTags").Any(tag => tag.Contains("Psychic", StringComparison.OrdinalIgnoreCase)),
    };
}

// What a projectile class from a mod assembly actually does to one human, decided per class from the
// decompiled source rather than from the fact that the class is not vanilla. Resolving a class here
// clears the reason that kept the weapon out of every ranking.
void ApplyProjectileClass(Weapon weapon, JsonElement verb, JsonElement proj)
{
    // A beam does not fire a projectile at all: it sweeps cells, and how much of a burst lands on
    // one pawn depends on how wide the beam has spread. Full width is the pessimistic end, which is
    // what a target at range gets; at close range more of the burst lands on the same pawn.
    float beamDamage = Num(verb, "beamDamage");
    if (beamDamage > 0f && weapon.Cycle > 0f)
    {
        float hits = weapon.Burst / (Num(verb, "beamWidth") + 1f);
        weapon.EstDps = hits * beamDamage / weapon.Cycle;
        weapon.Estimates.Add($"beam {hits:0.#} of {weapon.Burst} hits x {beamDamage:0} damage");
        weapon.Hard.RemoveAll(r => ReasonKind(r) == "beamDamage");
        return;
    }

    string projectileClass = Str(proj, "class");
    if (projectileClass == null)
        return;

    if (zeroDamageProjectiles.TryGetValue(projectileClass, out string why))
    {
        weapon.NoDamage = true;
        weapon.EstDps = 0f;
        weapon.Estimates.Add($"no hit-point damage: {why}");
        weapon.Hard.RemoveAll(r => ReasonKind(r) == "moddedProjectileClass");
        return;
    }

    // A matchlock fires several pellets per pull, and the damage of each falls off with distance:
    // double at point blank, nothing at maximum range. Half range is the reference, where the
    // multiplier is exactly one and the shot is worth its pellet count.
    float shotCount = proj.TryGetProperty("properties", out var props) ? Num(props, "shotCount") : 0f;
    if (shotCount > 1f && weapon.Cycle > 0f)
    {
        weapon.EstDps = weapon.Damage * shotCount * weapon.Burst / weapon.Cycle;
        weapon.Estimates.Add($"{shotCount:0} pellets per shot, at half range");
        weapon.Hard.RemoveAll(r => ReasonKind(r) == "moddedProjectileClass");
        return;
    }

    // The dump flags a class on any of the weapon's verbs, and a gauss rifle carries its sweeping
    // projectile on the high-power verb rather than the primary one. So clear by the class named in
    // the reason, not only by what the primary verb fires.
    weapon.Hard.RemoveAll(reason =>
    {
        if (ReasonKind(reason) != "moddedProjectileClass")
            return false;
        string named = reason[(reason.IndexOf(':') + 1)..];
        if (!plainProjectiles.TryGetValue(named, out string note))
            return false;
        if (note.Length > 0)
            weapon.Soft.Add($"{named.Split('.').Last()}: {note}");
        return true;
    });
}

// Eltex Weaponry replaces a shot's damage rather than adding to it: a Harmony prefix on
// Projectile.ImpactSomething computes Ceil(base x clamp(shooterPsy x targetPsy, 0.25, 4)) + bonus and
// suppresses vanilla impact entirely. Only the bonus is in def data, so the rest is reconstructed
// here: the shooter is a baseliner (sensitivity 1.0) holding this weapon, whose own
// PsychicSensitivityOffset reaches the wielder through the stat's gear stat part, and the target is
// an ordinary human at 1.0. A psycaster or a mechanoid target moves this a long way in either
// direction, which no static figure can carry.
void ApplyEltex(Weapon weapon, JsonElement t, float bonus)
{
    float factor = Math.Clamp(1f + EquippedOffset(t, "PsychicSensitivityOffset"), 0.25f, 4f);

    if (weapon.Ranged)
    {
        if (weapon.Cycle <= 0f)
            return;
        float perShot = MathF.Ceiling(weapon.Damage * factor) + bonus;
        weapon.EstDps = perShot * weapon.Burst / weapon.Cycle;
    }
    else
    {
        // Melee takes the same factor and the same flat bonus, once per swing, so the flat part is
        // worth a full attack's cooldown of damage.
        float cooldown = AverageMeleeCooldown(t);
        if (cooldown <= 0f)
            return;
        weapon.EstDps = weapon.Dps * factor + bonus / cooldown;
    }
    weapon.Estimates.Add($"eltex psychic x{factor:0.##}{(bonus > 0f ? $" +{bonus:0}" : "")}");
}

// The game weights a melee tool by expected damage squared, times commonality, times chanceFactor.
// Reproduced here from the dumped tool data, minus the stuff and maneuver terms, which move every
// tool on one weapon together and so barely shift a ratio.
float ExtraMeleeDps(JsonElement t)
{
    float weightedExtra = 0f, weightedCooldown = 0f;
    foreach (var tool in Array(t, "tools"))
    {
        float cooldown = Num(tool, "cooldownTime");
        if (cooldown <= 0f)
            continue;
        float power = Num(tool, "power");
        float weight = power * power * NumOr(tool, "chanceFactor", 1f);
        float extra = 0f;
        foreach (var damage in Array(tool, "extraMeleeDamages"))
            extra += Num(damage, "amount") * NumOr(damage, "chance", 1f);
        weightedExtra += weight * extra;
        weightedCooldown += weight * cooldown;
    }
    return weightedCooldown > 0f ? weightedExtra / weightedCooldown : 0f;
}

float AverageMeleeCooldown(JsonElement t)
{
    float weightSum = 0f, weightedCooldown = 0f;
    foreach (var tool in Array(t, "tools"))
    {
        float cooldown = Num(tool, "cooldownTime");
        if (cooldown <= 0f)
            continue;
        float power = Num(tool, "power");
        float weight = power * power * NumOr(tool, "chanceFactor", 1f);
        weightSum += weight;
        weightedCooldown += weight * cooldown;
    }
    return weightSum > 0f ? weightedCooldown / weightSum : 0f;
}

// The attack the weapon is actually for: its primary verb, else the first of the right kind.
static JsonElement? PrimaryVerb(JsonElement w, bool melee)
{
    if (!w.TryGetProperty("verbs", out var verbs) || verbs.ValueKind != JsonValueKind.Array)
        return null;
    JsonElement? fallback = null;
    foreach (var v in verbs.EnumerateArray())
    {
        if (Bool(v, "melee") != melee) continue;
        if (Bool(v, "primary") && !Bool(v, "manualOnly")) return v;
        fallback ??= v;
    }
    return fallback;
}

// ---- helpers -------------------------------------------------------------------------------

// "aoe:3.9 (Bullet_X)" -> "aoe". The kind groups the report; the rest is the detail.
// Mod extensions keep their type in the kind: which extension it is decides whether it matters at
// all, and one benign graphics extension on 140 weapons would otherwise bury the one that doubles
// a weapon's damage.
static string ReasonKind(string reason)
{
    if (reason.StartsWith("modExt:", StringComparison.Ordinal))
        return reason;
    int colon = reason.IndexOf(':');
    int space = reason.IndexOf(' ');
    int cut = colon < 0 ? space : (space < 0 ? colon : Math.Min(colon, space));
    return cut < 0 ? reason : reason[..cut];
}

// equippedStatOffsets is a list of {stat, value}: the stats the item changes on whoever wears it.
static float EquippedOffset(JsonElement t, string stat)
{
    if (!t.TryGetProperty("equippedStatOffsets", out var list) || list.ValueKind != JsonValueKind.Array)
        return 0f;
    foreach (var m in list.EnumerateArray())
        if (Str(m, "stat") == stat)
            return Num(m, "value");
    return 0f;
}

static string Str(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
        ? v.GetString() : null;

static bool Bool(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

static float Num(JsonElement e, string name) => NumOr(e, name, 0f);

static float NumOr(JsonElement e, string name, float fallback) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
        ? (float)v.GetDouble() : fallback;

static IEnumerable<JsonElement> Array(JsonElement e, string name)
{
    if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Array)
        yield break;
    foreach (var item in v.EnumerateArray())
        yield return item;
}

static IEnumerable<string> Strings(JsonElement e, string name)
{
    foreach (var item in Array(e, name))
        if (item.ValueKind == JsonValueKind.String)
            yield return item.GetString();
}

static string ShortMod(string mod) => mod == null ? "?" : Regex.Replace(mod, @"\s*\[.*\]$", "");

static string Escape(string s) => s?.Replace("|", "\\|") ?? "";

sealed class Weapon
{
    public string DefName, Label, Mod, Tech, Stuff;
    public bool Vanilla, Ranged, Hidden, Craftable;

    /// <summary>Known to deal no hit points at all - a status dart, a smoke round, a point-defense gun.</summary>
    public bool NoDamage;
    public float Value, Work, Mass;
    public float Damage, ArmorPen, Range, Cycle, Dps, Accuracy = 1f;
    public int Burst = 1, Traders, Rewards;

    public float PsySensQ, PsySensFlat, PsyHeatRecovery, PsyMeditation, PsyFocusCost;
    public bool PsyTradeTag;

    public float PsySens => PsySensQ + PsySensFlat;
    public bool AnyPsychic => PsySensQ != 0f || PsySensFlat != 0f || PsyHeatRecovery != 0f
        || PsyMeditation != 0f || PsyFocusCost != 0f;

    /// <summary>Reasons there is no readable damage at all. Any of these keeps it out of the rankings.</summary>
    public List<string> Hard = new();

    /// <summary>Reasons the readable damage is a floor rather than the whole story.</summary>
    public List<string> Soft = new();

    /// <summary>Single-target DPS after the mechanisms this report can reconstruct.</summary>
    public float EstDps;

    /// <summary>One line per mechanism folded into <see cref="EstDps"/>.</summary>
    public List<string> Estimates = new();

    public bool Measured => Hard.Count == 0;
    public bool Estimated => Estimates.Count > 0;

    /// <summary>Every ranking uses this: the estimate where there is one, the card otherwise.</summary>
    public float EffDps => EstDps * Accuracy;
    public string Flags => Soft.Count == 0 ? "" : string.Join(" ", Soft.Select(ReasonAbbrev).Distinct());

    private static string ReasonAbbrev(string reason)
    {
        int colon = reason.IndexOf(':');
        int space = reason.IndexOf(' ');
        int cut = colon < 0 ? space : (space < 0 ? colon : Math.Min(colon, space));
        string kind = cut < 0 ? reason : reason[..cut];
        return kind switch
        {
            "moddedVerbClass" => "verb",
            "extraDamages" => "extra",
            "aoe" => "aoe",
            "modExt" => "modext",
            "toolHediff" => "hediff",
            "toolExtraDamages" => "toolextra",
            "toolSurpriseAttack" => "surprise",
            "nonProjectileVerb" => "nonproj",
            _ => kind,
        };
    }
}

sealed class Armor
{
    public string DefName, Label, Mod, Tech, Layer;
    public bool Vanilla, Craftable;
    public float Value, Work, Mass;
    public float Coverage, Sharp, Blunt, Heat, Cold, HeatIns, Vacuum, ToxicEnv;
    public int Traders, Rewards;

    // The two sensitivity grants are different mechanics: the Offset stat is quality-scaled
    // 0.5x-1.5x on the item, the plain stat is flat. A rebalance moving value between them changes
    // how much quality matters, so the report keeps them apart.
    public float PsySensQ, PsySensFlat, PsyHeatRecovery, PsyHeatMax, PsyMeditation, PsyFocusCost;
    public bool Prestige, PsyTradeTag;

    public float PsySens => PsySensQ + PsySensFlat;
    public bool AnyPsychic => PsySensQ != 0f || PsySensFlat != 0f || PsyHeatRecovery != 0f
        || PsyHeatMax != 0f || PsyMeditation != 0f || PsyFocusCost != 0f;

    public float Shielding => Coverage * (0.6f * Sharp + 0.4f * Blunt);
}
