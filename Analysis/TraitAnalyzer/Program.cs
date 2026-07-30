// Offline analyzer for TraitDump.json (throwaway, gitignored output).
//
// Two questions this answers that no single def can. First, dilution: traits are rolled by weight
// over the whole eligible pool, so every mod that adds one quietly lowers the odds of all the
// others, and a vanilla trait's real chance in a large modlist has nothing to do with the number in
// its def. Second, duplication: several mods independently add "moves faster", "shoots better",
// "hates social work", and nothing makes them conflict, so one pawn can hold three versions of the
// same idea and stack them.
//
// Shares below are commonality / pool, which is the roll before backstory and conflict filtering
// removes candidates. It is the right number for comparing traits with each other, and an upper
// bound on any single trait's real frequency.
//
// Emits:
//   traits.md — the pool and who fills it, work-disabling traits, overlap pairs, forced sources
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string tmp = Path.Combine(home, "AppData", "LocalLow", "Ludeon Studios",
    "RimWorld by Ludeon Studios", "RebalancePatches", "tmp");

string traitPath = args.Length > 0 ? args[0] : Path.Combine(tmp, "TraitDump.json");
string outDir = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "out");
outDir = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDir);

if (!File.Exists(traitPath))
{
    Console.Error.WriteLine($"No dump at {traitPath}. Enable dev.traitdump and launch the game to the main menu.");
    return 1;
}

using var doc = JsonDocument.Parse(File.ReadAllText(traitPath));

// ---- read ------------------------------------------------------------------------------------

var traits = new List<Trait>();
foreach (var t in doc.RootElement.GetProperty("traits").EnumerateArray())
{
    string defName = Str(t, "defName");
    if (defName == null) continue;

    var trait = new Trait
    {
        DefName = defName,
        Label = Str(t, "label") ?? "",
        Mod = ShortMod(Str(t, "mod")),
        Vanilla = (Str(t, "mod") ?? "").Contains("[ludeon.rimworld", StringComparison.OrdinalIgnoreCase),
        Commonality = Num(t, "commonality"),
        CommonalityFemale = Num(t, "commonalityFemale"),
        DisabledWorkTags = Str(t, "disabledWorkTags") ?? "",
        RequiredWorkTags = Str(t, "requiredWorkTags") ?? "",
        Conflicts = Strings(t, "conflictingTraits").ToList(),
        ExclusionTags = Strings(t, "exclusionTags").ToList(),
        DisabledWorkTypes = Strings(t, "disabledWorkTypes").ToList(),
        ForcedPassions = Strings(t, "forcedPassions").ToList(),
    };

    foreach (var d in Array(t, "degrees"))
        trait.Degrees.Add(new Degree
        {
            Value = (int)Num(d, "degree"),
            Label = Str(d, "label") ?? "",
            Commonality = Num(d, "commonality"),
        });

    // Effects live on the raw degree data. Aggregate them into one signature per trait: the same
    // idea shipped by two mods rarely uses the same degree count, but it does touch the same stats.
    foreach (var d in Array(t, "degreeDatas"))
    {
        foreach (var m in Array(d, "statOffsets"))
            trait.Add($"stat:{Str(m, "stat")}", Num(m, "value"));
        foreach (var m in Array(d, "statFactors"))
            trait.Add($"factor:{Str(m, "stat")}", Num(m, "value") - 1f);
        foreach (var g in Array(d, "skillGains"))
            trait.Add($"skill:{Str(g, "skill")}", Num(g, "amount"));
        string tags = Str(d, "disabledWorkTags");
        if (!string.IsNullOrEmpty(tags))
            trait.DegreeWorkTags.Add(tags);
        if (Num(d, "painFactor") != 0f) trait.Add("painFactor", Num(d, "painFactor") - 1f);
        if (Num(d, "hungerRateFactor") != 0f) trait.Add("hungerRateFactor", Num(d, "hungerRateFactor") - 1f);
        if (Num(d, "socialFightChanceFactor") != 0f)
            trait.Add("socialFightChanceFactor", Num(d, "socialFightChanceFactor") - 1f);
    }

    traits.Add(trait);
}

// ---- who forces a trait, without the roll -----------------------------------------------------

var forced = new Dictionary<string, List<(string Root, string Mod)>>();
if (doc.RootElement.TryGetProperty("references", out var refs))
    foreach (var entry in refs.EnumerateObject())
    {
        int colon = entry.Name.IndexOf(':');
        string def = colon >= 0 ? entry.Name[(colon + 1)..] : entry.Name;
        var list = forced.TryGetValue(def, out var existing) ? existing : forced[def] = new();
        foreach (var hit in entry.Value.EnumerateArray())
            list.Add((Str(hit, "root") ?? "?", Str(hit, "mod") ?? "?"));
    }
foreach (var trait in traits)
    if (forced.TryGetValue(trait.DefName, out var sources))
        trait.Forced = sources;

// ---- pool -------------------------------------------------------------------------------------

float pool = traits.Sum(t => t.Commonality);
float vanillaPool = traits.Where(t => t.Vanilla).Sum(t => t.Commonality);
var byMod = traits.GroupBy(t => t.Mod)
    .Select(g => (Mod: g.Key, Count: g.Count(), Weight: g.Sum(t => t.Commonality)))
    .OrderByDescending(g => g.Weight).ToList();

var sb = new StringBuilder();
sb.AppendLine("# Traits");
sb.AppendLine();
sb.AppendLine($"{traits.Count} traits from {byMod.Count} mods, {traits.Sum(t => t.Degrees.Count)} degrees in total.");
sb.AppendLine($"Roll pool **{pool:0.#}**, of which vanilla holds **{vanillaPool:0.#}** ({vanillaPool / pool:P1}).");
sb.AppendLine($"{traits.Count(t => t.Commonality <= 0f)} traits are never rolled at all and only arrive forced.");
sb.AppendLine();
sb.AppendLine("Shares are commonality / pool: the roll before backstory and conflict filtering, which is the");
sb.AppendLine("right number for comparing traits with each other and an upper bound on any one trait's frequency.");
sb.AppendLine();

sb.AppendLine("## Who fills the pool");
sb.AppendLine();
sb.AppendLine("| Mod | Traits | Weight | Share |");
sb.AppendLine("|---|---|---|---|");
foreach (var m in byMod)
    sb.AppendLine($"| {m.Mod} | {m.Count} | {m.Weight:0.#} | {m.Weight / pool:P1} |");
sb.AppendLine();

sb.AppendLine("## What dilution did to the vanilla traits");
sb.AppendLine();
sb.AppendLine("`alone` is the share the trait would have in a vanilla-only pool; `now` is its share in this");
sb.AppendLine("modlist. The ratio is the same for every vanilla trait, so read the column for the size of the");
sb.AppendLine("effect, not for the ordering.");
sb.AppendLine();
sb.AppendLine("| Trait | Weight | Alone | Now | Forced sources |");
sb.AppendLine("|---|---|---|---|---|");
foreach (var t in traits.Where(t => t.Vanilla && t.Commonality > 0f).OrderByDescending(t => t.Commonality))
    sb.AppendLine($"| `{t.DefName}` {t.Label} | {t.Commonality:0.##} | {t.Commonality / vanillaPool:P2} | " +
                  $"{t.Commonality / pool:P2} | {t.Forced.Count} |");
sb.AppendLine();

sb.AppendLine("## Heaviest modded entries");
sb.AppendLine();
sb.AppendLine("Modded traits pulling the largest share of the roll. A mod that ships twenty traits at the");
sb.AppendLine("vanilla default weight has quietly taken a fifth of the pool.");
sb.AppendLine();
sb.AppendLine("| Trait | Mod | Weight | Share | Degrees | Disables | Forced |");
sb.AppendLine("|---|---|---|---|---|---|---|");
foreach (var t in traits.Where(t => !t.Vanilla && t.Commonality > 0f)
             .OrderByDescending(t => t.Commonality).Take(50))
    sb.AppendLine($"| `{t.DefName}` {t.Label} | {t.Mod} | {t.Commonality:0.##} | {t.Commonality / pool:P2} | " +
                  $"{t.Degrees.Count} | {Escape(t.AllWorkTags)} | {t.Forced.Count} |");
sb.AppendLine();

sb.AppendLine("## Work-disabling traits");
sb.AppendLine();
sb.AppendLine("Every trait that takes work away from a pawn, with the odds of rolling it. These are the ones");
sb.AppendLine("whose combined weight decides how often a generated pawn is unusable for something.");
sb.AppendLine();
float disablingWeight = traits.Where(t => t.Disables).Sum(t => t.Commonality);
sb.AppendLine($"Combined weight **{disablingWeight:0.#}**, {disablingWeight / pool:P1} of the pool.");
sb.AppendLine();
sb.AppendLine("| Trait | Mod | Weight | Share | Work tags | Work types |");
sb.AppendLine("|---|---|---|---|---|---|");
foreach (var t in traits.Where(t => t.Disables).OrderByDescending(t => t.Commonality))
    sb.AppendLine($"| `{t.DefName}` {t.Label} | {t.Mod} | {t.Commonality:0.##} | {t.Commonality / pool:P2} | " +
                  $"{Escape(t.AllWorkTags)} | {Escape(string.Join(", ", t.DisabledWorkTypes))} |");
sb.AppendLine();

// ---- overlap ------------------------------------------------------------------------------

// Two traits overlap when they push the same stats the same way. Shared count alone is noisy, so a
// single shared effect only counts when it is the whole of both traits.
var pairs = new List<(Trait A, Trait B, List<string> Shared)>();
for (int i = 0; i < traits.Count; i++)
    for (int k = i + 1; k < traits.Count; k++)
    {
        Trait a = traits[i], b = traits[k];
        if (a.Effects.Count == 0 || b.Effects.Count == 0) continue;
        var shared = a.Effects.Keys.Where(e => b.Effects.ContainsKey(e)
                                               && Math.Sign(a.Effects[e]) == Math.Sign(b.Effects[e])).ToList();
        if (shared.Count == 0) continue;
        if (shared.Count == 1 && (a.Effects.Count > 1 || b.Effects.Count > 1)) continue;
        pairs.Add((a, b, shared));
    }

var unconflicted = pairs
    .Where(p => !p.A.Conflicts.Contains(p.B.DefName) && !p.B.Conflicts.Contains(p.A.DefName)
                && !p.A.ExclusionTags.Intersect(p.B.ExclusionTags).Any())
    .OrderByDescending(p => p.Shared.Count)
    .ToList();

// A mod's own traits overlapping each other is usually a deliberate family of variants. Two mods
// arriving at the same effects independently is the duplication worth acting on, so the two are
// separated rather than sorted together - otherwise one large trait pack fills the whole table.
var crossMod = unconflicted.Where(p => p.A.Mod != p.B.Mod).ToList();
var sameMod = unconflicted.Where(p => p.A.Mod == p.B.Mod).ToList();

sb.AppendLine("## Overlapping traits that can stack");
sb.AppendLine();
sb.AppendLine($"{pairs.Count} trait pairs push at least one of the same effects the same way; {unconflicted.Count}");
sb.AppendLine("of those declare no conflict and share no exclusion tag, so one pawn can hold both and add them up.");
sb.AppendLine($"{crossMod.Count} of them cross mod boundaries.");
sb.AppendLine();

sb.AppendLine("### Across mods");
sb.AppendLine();
sb.AppendLine("| Trait A | Trait B | Shared effects |");
sb.AppendLine("|---|---|---|");
foreach (var p in crossMod.Take(100))
    sb.AppendLine($"| `{p.A.DefName}` ({p.A.Mod}) | `{p.B.DefName}` ({p.B.Mod}) | " +
                  $"{Escape(string.Join(", ", p.Shared))} |");
sb.AppendLine();

sb.AppendLine($"### Within one mod (top 40 of {sameMod.Count})");
sb.AppendLine();
sb.AppendLine("| Trait A | Trait B | Mod | Shared effects |");
sb.AppendLine("|---|---|---|---|");
foreach (var p in sameMod.Take(40))
    sb.AppendLine($"| `{p.A.DefName}` | `{p.B.DefName}` | {p.A.Mod} | {Escape(string.Join(", ", p.Shared))} |");
sb.AppendLine();

// ---- forced ---------------------------------------------------------------------------------

sb.AppendLine("## Handed out without the roll");
sb.AppendLine();
sb.AppendLine("Backstories, genes, xenotypes, pawn kinds, scenarios and precepts that put a trait on a pawn");
sb.AppendLine("directly. A trait with many sources is common however small its commonality says it is.");
sb.AppendLine();
sb.AppendLine("| Trait | Mod | Weight | Sources | Kinds | Top sources |");
sb.AppendLine("|---|---|---|---|---|---|");
foreach (var t in traits.Where(t => t.Forced.Count > 0).OrderByDescending(t => t.Forced.Count).Take(80))
{
    string kinds = string.Join(", ", t.Forced.Select(f => RootKind(f.Root)).Distinct().OrderBy(s => s));
    string top = string.Join(", ", t.Forced.Take(4).Select(f => $"`{f.Root}`"));
    sb.AppendLine($"| `{t.DefName}` {t.Label} | {t.Mod} | {t.Commonality:0.##} | {t.Forced.Count} | " +
                  $"{Escape(kinds)} | {Escape(top)}{(t.Forced.Count > 4 ? ", …" : "")} |");
}
sb.AppendLine();

sb.AppendLine("## Full trait table");
sb.AppendLine();
sb.AppendLine("| Trait | Mod | Weight (M/F) | Share | Degrees | Effects | Disables | Conflicts | Forced |");
sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
foreach (var t in traits.OrderBy(t => t.Mod, StringComparer.OrdinalIgnoreCase)
             .ThenByDescending(t => t.Commonality))
    sb.AppendLine($"| `{t.DefName}` {t.Label} | {t.Mod} | {t.Commonality:0.##}/{t.CommonalityFemale:0.##} | " +
                  $"{t.Commonality / pool:P2} | {t.Degrees.Count} | {Escape(t.EffectSummary)} | " +
                  $"{Escape(t.AllWorkTags)} | {t.Conflicts.Count} | {t.Forced.Count} |");

File.WriteAllText(Path.Combine(outDir, "traits.md"), sb.ToString());

Console.WriteLine($"{traits.Count} traits, pool {pool:0.#} (vanilla {vanillaPool / pool:P1}), " +
                  $"{unconflicted.Count} stackable overlaps, " +
                  $"{traits.Count(t => t.Forced.Count > 0)} forced somewhere -> {outDir}");
return 0;

// ---- helpers -------------------------------------------------------------------------------

// "BackstoryDef:ColonyMechanic" -> "BackstoryDef".
static string RootKind(string root)
{
    int colon = root.IndexOf(':');
    return colon < 0 ? root : root[..colon];
}

static string Str(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
        ? v.GetString() : null;

static float Num(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
        ? (float)v.GetDouble() : 0f;

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

static string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|");

sealed class Degree
{
    public int Value;
    public string Label;
    public float Commonality;
}

sealed class Trait
{
    public string DefName, Label, Mod, DisabledWorkTags, RequiredWorkTags;
    public bool Vanilla;
    public float Commonality, CommonalityFemale;
    public List<Degree> Degrees = new();
    public List<string> Conflicts = new(), ExclusionTags = new(), DisabledWorkTypes = new(), ForcedPassions = new();
    public List<string> DegreeWorkTags = new();
    public List<(string Root, string Mod)> Forced = new();

    /// <summary>Effect name to its strongest value across degrees, signed. The overlap signature.</summary>
    public Dictionary<string, float> Effects = new();

    public void Add(string key, float value)
    {
        if (key == null || value == 0f) return;
        if (!Effects.TryGetValue(key, out float existing) || Math.Abs(value) > Math.Abs(existing))
            Effects[key] = value;
    }

    public bool Disables => !string.IsNullOrEmpty(AllWorkTags) || DisabledWorkTypes.Count > 0;

    public string AllWorkTags => string.Join(", ",
        DegreeWorkTags.Append(DisabledWorkTags).Where(s => !string.IsNullOrEmpty(s)).Distinct());

    public string EffectSummary => string.Join(", ", Effects.OrderByDescending(e => Math.Abs(e.Value))
        .Take(4).Select(e => $"{e.Key} {e.Value:+0.##;-0.##}"));
}
