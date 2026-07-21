// Offline analyzer for XenotypeFactionDump.json + GeneDump.json (throwaway, gitignored output).
// Joins each faction's xenotype roster to what those xenotypes actually are, so thematic fit can be
// judged, and works out what trimming a faction back to a baseliner floor would cost each entry.
//
// The game picks by weight, not by independent probability: PawnGenerator.XenotypesAvailableFor sums
// the faction, meme and pawnkind sets, appends Baseliner at (1 - sum) only when that is positive, and
// rolls TryRandomElementByWeight. So a roster summing past 1.0 has no baseliners at all, and every
// entry's real odds are weight/sum. Percentages below are those real odds, not the raw chances.
//
// Emits:
//   xeno-factions.md — per faction: roster, real odds, baseliner share, thematic worksheet
//   xeno-index.md    — per xenotype: what it is and everywhere it currently spawns
//   trim-plan.md     — factions under the baseliner floor, with per-entry scaled weights
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string tmp = Path.Combine(home, "AppData", "LocalLow", "Ludeon Studios",
    "RimWorld by Ludeon Studios", "RebalancePatches", "tmp");

string factionPath = args.Length > 0 ? args[0] : Path.Combine(tmp, "XenotypeFactionDump.json");
string genePath = args.Length > 1 ? args[1] : Path.Combine(tmp, "GeneDump.json");
string outDir = args.Length > 2 ? args[2] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "out");
float floor = args.Length > 3 ? float.Parse(args[3], CultureInfo.InvariantCulture) : 0.35f;
outDir = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDir);

using var factionDoc = JsonDocument.Parse(File.ReadAllText(factionPath));
using var geneDoc = JsonDocument.Parse(File.ReadAllText(genePath));

// ---- what each xenotype is -------------------------------------------------------------------

var blurbs = new Dictionary<string, string>();
foreach (var x in geneDoc.RootElement.GetProperty("xenotypes").EnumerateArray())
{
    string name = x.GetProperty("defName").GetString();
    if (name == null) continue;
    string desc = x.TryGetProperty("description", out var d) ? d.GetString() : null;
    blurbs[name] = Blurb(desc);
}

var xenos = new Dictionary<string, Xeno>();
foreach (var x in factionDoc.RootElement.GetProperty("xenotypes").EnumerateArray())
{
    string name = x.GetProperty("defName").GetString();
    if (name == null) continue;
    xenos[name] = new Xeno
    {
        DefName = name,
        Label = Str(x, "label") ?? name,
        Mod = ShortMod(Str(x, "mod")),
        Modded = Bool(x, "modded"),
        Combatant = Bool(x, "canGenerateAsCombatant"),
        Archite = Bool(x, "archite"),
        Genes = Num(x, "geneCount"),
        WildWeight = Num(x, "factionlessGenerationWeight"),
        Blurb = blurbs.TryGetValue(name, out var b) ? b : "",
    };
}

// ---- faction rosters -------------------------------------------------------------------------

var factions = new List<Faction>();
foreach (var f in factionDoc.RootElement.GetProperty("factions").EnumerateArray())
{
    var faction = new Faction
    {
        DefName = Str(f, "defName"),
        Label = Str(f, "label"),
        Mod = ShortMod(Str(f, "mod")),
        Hidden = Bool(f, "hidden"),
        IsPlayer = Bool(f, "isPlayer"),
        Humanlike = !f.TryGetProperty("humanlikeFaction", out var hl) || hl.GetBoolean(),
        PawnKinds = (int)Num(f, "pawnKindCount"),
        Baseliner = Num(f, "baselinerChance"),
    };
    if (f.TryGetProperty("xenotypeSet", out var set) && set.ValueKind == JsonValueKind.Object)
        foreach (var entry in set.EnumerateObject())
            faction.Entries.Add(new Entry { Xeno = entry.Name, Weight = (float)entry.Value.GetDouble() });
    faction.Entries.Sort((a, b) => b.Weight.CompareTo(a.Weight));
    factions.Add(faction);
}

// Where each xenotype currently appears, faction-level only (the layer this audit edits).
var placements = new Dictionary<string, List<(Faction F, float W)>>();
foreach (var f in factions)
    foreach (var e in f.Entries)
    {
        if (!placements.TryGetValue(e.Xeno, out var list)) placements[e.Xeno] = list = new();
        list.Add((f, e.Weight));
    }

// ---- xeno-factions.md ------------------------------------------------------------------------

// Rosters at or past 1.0 have deliberately traded all baseliners away - dedicated race factions.
// Separating them keeps the trim list to factions that meant to keep some.
var crowded = factions.Where(f => f.Entries.Count > 0 && f.Sum < 1f && f.Baseliner < floor)
    .OrderBy(f => f.Baseliner).ToList();
var dedicated = factions.Where(f => f.Entries.Count > 0 && f.Sum >= 1f)
    .OrderByDescending(f => f.Sum).ToList();
var healthy = factions.Where(f => f.Entries.Count > 0 && f.Sum < 1f && f.Baseliner >= floor)
    .OrderBy(f => f.Baseliner).ToList();
var empty = factions.Where(f => f.Entries.Count == 0 && !f.Hidden && !f.IsPlayer && f.Humanlike && f.PawnKinds > 0)
    .OrderBy(f => f.DefName).ToList();

var sb = new StringBuilder();
sb.AppendLine("# Faction xenotype rosters");
sb.AppendLine();
sb.AppendLine($"Baseliner floor for this run: **{floor:P0}**. Percentages are real spawn odds (weight/sum).");
sb.AppendLine();
sb.AppendLine($"- {crowded.Count} crowded (below the floor, still keep some baseliners)");
sb.AppendLine($"- {dedicated.Count} dedicated (roster sums to 1.0+, zero baseliners by design)");
sb.AppendLine($"- {healthy.Count} at or above the floor");
sb.AppendLine($"- {empty.Count} empty rosters that still field pawns");
sb.AppendLine();

Section("Crowded — below the baseliner floor", crowded);
Section("Dedicated — no baseliners by design", dedicated);
Section("At or above the floor", healthy);

sb.AppendLine("## Empty rosters");
sb.AppendLine();
sb.AppendLine("Humanlike factions that field pawns but roll baseliner every time.");
sb.AppendLine();
sb.AppendLine("| Faction | Label | Mod | Pawn kinds |");
sb.AppendLine("|---|---|---|---|");
foreach (var f in empty)
    sb.AppendLine($"| `{f.DefName}` | {f.Label} | {f.Mod} | {f.PawnKinds} |");
File.WriteAllText(Path.Combine(outDir, "xeno-factions.md"), sb.ToString());

void Section(string title, List<Faction> list)
{
    sb.AppendLine($"## {title}");
    sb.AppendLine();
    foreach (var f in list)
    {
        float denom = Math.Max(f.Sum, 1f);
        sb.AppendLine($"### {f.Label} — `{f.DefName}`");
        sb.AppendLine();
        sb.AppendLine($"*{f.Mod}* · sum **{f.Sum:0.###}** · baseliner **{Math.Max(0f, 1f - f.Sum) / denom:P1}** · {f.Entries.Count} entries · {f.PawnKinds} pawn kinds");
        sb.AppendLine();
        sb.AppendLine("| Xenotype | Weight | Odds | Mod | What it is |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var e in f.Entries)
        {
            xenos.TryGetValue(e.Xeno, out var x);
            string flags = x == null ? "" : (x.Combatant ? "" : " ⚠non-combat");
            sb.AppendLine($"| `{e.Xeno}` {x?.Label} | {e.Weight:0.####} | {e.Weight / denom:P1} | {x?.Mod} | {x?.Blurb}{flags} |");
        }
        sb.AppendLine();
    }
}

// ---- xeno-index.md ---------------------------------------------------------------------------

var idx = new StringBuilder();
idx.AppendLine("# Xenotype index");
idx.AppendLine();
idx.AppendLine("Every xenotype, what it is, and every faction roster it currently sits in.");
idx.AppendLine("`wild` is factionlessGenerationWeight — above zero means it already turns up as a wanderer.");
idx.AppendLine();
foreach (var x in xenos.Values.OrderBy(x => x.Mod, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.DefName, StringComparer.Ordinal))
{
    placements.TryGetValue(x.DefName, out var where);
    string spots = where == null || where.Count == 0
        ? "**nowhere**"
        : string.Join(", ", where.OrderByDescending(w => w.W).Select(w => $"`{w.F.DefName}` {w.W:0.###}"));
    idx.AppendLine($"### `{x.DefName}` — {x.Label}");
    idx.AppendLine();
    idx.AppendLine($"*{x.Mod}* · {x.Genes} genes{(x.Archite ? " · archite" : "")}{(x.Combatant ? "" : " · **non-combatant**")}{(x.WildWeight > 0 ? $" · wild {x.WildWeight:0.##}" : "")}");
    idx.AppendLine();
    idx.AppendLine($"{x.Blurb}");
    idx.AppendLine();
    idx.AppendLine($"Spawns in: {spots}");
    idx.AppendLine();
}
File.WriteAllText(Path.Combine(outDir, "xeno-index.md"), idx.ToString());

// ---- trim-plan.md ----------------------------------------------------------------------------

var trim = new StringBuilder();
trim.AppendLine("# Trim plan");
trim.AppendLine();
trim.AppendLine($"Scaling every entry by `target/sum` so the roster leaves **{floor:P0}** for baseliners,");
trim.AppendLine($"i.e. a target sum of **{1f - floor:0.###}**. Proportional scaling preserves each mod's relative");
trim.AppendLine("share, so it is a starting point for the thematic pass, not a substitute for it.");
trim.AppendLine();
foreach (var f in crowded)
{
    float target = 1f - floor;
    float scale = target / f.Sum;
    trim.AppendLine($"## {f.Label} — `{f.DefName}`");
    trim.AppendLine();
    trim.AppendLine($"*{f.Mod}* · sum {f.Sum:0.###} → {target:0.###} · scale **×{scale:0.###}** · baseliner {1f - f.Sum:P1} → {floor:P0}");
    trim.AppendLine();
    trim.AppendLine("| Xenotype | Now | Scaled | Mod |");
    trim.AppendLine("|---|---|---|---|");
    foreach (var e in f.Entries)
    {
        xenos.TryGetValue(e.Xeno, out var x);
        trim.AppendLine($"| `{e.Xeno}` | {e.Weight:0.####} | {Round(e.Weight * scale):0.####} | {x?.Mod} |");
    }
    trim.AppendLine();
}
File.WriteAllText(Path.Combine(outDir, "trim-plan.md"), trim.ToString());

Console.WriteLine($"{factions.Count} factions, {xenos.Count} xenotypes -> {outDir}");
Console.WriteLine($"crowded {crowded.Count}, dedicated {dedicated.Count}, healthy {healthy.Count}, empty {empty.Count}");

// ---- helpers ---------------------------------------------------------------------------------

// Keep patch XML readable: snap to 3 decimals, and to 4 only when 3 would round a small weight away.
static float Round(float v)
{
    float r = MathF.Round(v, 3);
    return r > 0f ? r : MathF.Round(v, 4);
}

static string Str(JsonElement e, string name) =>
    e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

static bool Bool(JsonElement e, string name) =>
    e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

static float Num(JsonElement e, string name) =>
    e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? (float)v.GetDouble() : 0f;

static string ShortMod(string mod) =>
    mod == null ? "?" : Regex.Replace(mod, @"\s*\[.*\]$", "");

// First sentence, stripped of rich text and the mod credit line most xenotype descriptions end with.
static string Blurb(string desc)
{
    if (string.IsNullOrWhiteSpace(desc)) return "";
    string s = Regex.Replace(desc, "<.*?>", " ");
    s = Regex.Replace(s, @"\s+", " ").Trim();
    int cut = s.IndexOf(". ", StringComparison.Ordinal);
    if (cut > 40) s = s[..(cut + 1)];
    if (s.Length > 240) s = s[..240].TrimEnd() + "…";
    return s.Replace("|", "\\|");
}

sealed class Xeno
{
    public string DefName, Label, Mod, Blurb;
    public bool Modded, Combatant, Archite;
    public float Genes, WildWeight;
}

sealed class Entry
{
    public string Xeno;
    public float Weight;
}

sealed class Faction
{
    public string DefName, Label, Mod;
    public bool Hidden, IsPlayer, Humanlike;
    public int PawnKinds;
    public float Baseliner;
    public List<Entry> Entries = new();
    public float Sum => Entries.Sum(e => e.Weight);
}
