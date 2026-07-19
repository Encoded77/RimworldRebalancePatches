// Offline analyzer for GeneDump.json (throwaway, gitignored).
// Builds an "effect signature" per gene (stats, capacities, aptitudes, abilities,
// hediff payloads flattened via the dump's referenced section) and emits:
//   duplicates.md — groups of genes with identical signatures
//   overlaps.md   — per effect atom, every gene touching it
//   cosmetics.md  — genes with no functional effect (cosmetic consolidation targets)
using System.Text;
using System.Text.Json;

string dumpPath = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "AppData", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios",
        "RebalancePatches", "tmp", "GeneDump.json");
string outDir = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "out");
outDir = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDir);

using var doc = JsonDocument.Parse(File.ReadAllText(dumpPath));
var root = doc.RootElement;
var genes = root.GetProperty("genes");
var referenced = root.GetProperty("referenced");
var xenotypes = root.GetProperty("xenotypes");

// Which xenotypes use each gene (reachability context for the report).
var geneToXenotypes = new Dictionary<string, List<string>>();
foreach (var x in xenotypes.EnumerateArray())
{
    if (!x.TryGetProperty("genes", out var gl) || gl.ValueKind != JsonValueKind.Array) continue;
    string xn = x.GetProperty("defName").GetString();
    foreach (var g in gl.EnumerateArray())
    {
        string gn = g.GetString();
        if (gn == null) continue;
        if (!geneToXenotypes.TryGetValue(gn, out var list)) geneToXenotypes[gn] = list = new List<string>();
        list.Add(xn);
    }
}

// Fields that are presentation/metadata, not gameplay effects.
var skipFields = new HashSet<string>
{
    "defName", "label", "labelShortAdj", "mod", "description", "descriptionShort", "$defType", "$type",
    "displayCategory", "displayOrderInCategory", "displayOrderOffset", "geneClass", "hasGraphics",
    "symbolPack", "exclusionTags", "prerequisite", "biostatMet", "biostatCpx", "biostatArc",
    "selectionWeight", "selectionWeightCultist", "selectionWeightFactorDarkSkin", "canGenerateInGeneSet",
    "marketValueFactor", "iconColor", "randomBrightnessFactor", "renderNodeProperties",
    "resourceGizmoType", "resourceLabel", "resourceGizmoThresholds", "showGizmoOnWorldView",
    "showGizmoWhenDrafted", "showGizmoOnMultiSelect", "customEffectDescriptions",
    "skinColorBase", "skinColorOverride", "hairColorOverride", "skinIsHairColor", "tattoosVisible",
    "bodyType", "forcedHeadTypes", "hairTagFilter", "beardTagFilter", "fur", "soundCall", "soundDeath",
    "soundWounded", "graphicData", "passOnDirectly", "womanCanBeMother", "randomChosen",
    "canBeRemovedFromEndogenome", "removeOnRedressIfNotOfKind", "dontMindRawFood",
    "minAgeActive", "deathHistoryEvent",
};

// Gene fields whose string/list values name hediffs or abilities granted by the gene
// (expanded through the referenced section). Anything else stringy stays inert.
var refTypes = new[] { "HediffDef", "AbilityDef", "ThoughtDef", "NeedDef", "MentalBreakDef" };
var refKeys = new HashSet<string>();
foreach (var p in referenced.EnumerateObject()) refKeys.Add(p.Name);

var sigs = new Dictionary<string, SortedSet<string>>();      // gene -> atoms
var geneMeta = new Dictionary<string, (string mod, string cls, int met, int cpx, int arc)>();

// Runtime-generated derivatives of real genes (VEF astrogene copies, Big and Small
// metamorphosis pairs) — not dedup targets; the source gene is the target.
string[] generatedSuffixes = { "_Astrogene", "_Metamorphosis", "_Retromorphosis" };
int generatedSkipped = 0;
var allNames = new HashSet<string>();
foreach (var g in genes.EnumerateArray()) allNames.Add(g.GetProperty("defName").GetString());

foreach (var g in genes.EnumerateArray())
{
    string name = g.GetProperty("defName").GetString();
    if (generatedSuffixes.Any(sfx => name.EndsWith(sfx))) { generatedSkipped++; continue; }
    // VRE-Android copies of existing genes (VREA_<original>); VREA genes without a
    // counterpart are genuine VRE-Android content and stay.
    if (name.StartsWith("VREA_") && allNames.Contains(name.Substring(5))) { generatedSkipped++; continue; }
    string mod = g.TryGetProperty("mod", out var m) ? m.GetString() : "?";
    string cls = g.TryGetProperty("geneClass", out var c) ? c.GetString() : null;
    int met = g.TryGetProperty("biostatMet", out var bm) ? bm.GetInt32() : 0;
    int cpx = g.TryGetProperty("biostatCpx", out var bc) ? bc.GetInt32() : 0;
    int arc = g.TryGetProperty("biostatArc", out var ba) ? ba.GetInt32() : 0;
    geneMeta[name] = (mod, cls, met, cpx, arc);

    var atoms = new SortedSet<string>(StringComparer.Ordinal);
    var visited = new HashSet<string>();
    Collect(g, null, atoms, visited, 0, isRoot: true);
    // A non-vanilla geneClass is itself behavior even when no data fields show it.
    if (cls != null && cls != "Verse.Gene") atoms.Add("class:" + cls);
    sigs[name] = atoms;
}

void Collect(JsonElement el, string parentField, SortedSet<string> atoms, HashSet<string> visited, int depth, bool isRoot = false)
{
    if (depth > 12) return;
    switch (el.ValueKind)
    {
        case JsonValueKind.Object:
            string type = el.TryGetProperty("$type", out var t) ? t.GetString() : null;
            if (type != null && type.EndsWith("StatModifier"))
            {
                string stat = el.GetProperty("stat").GetString();
                double v = el.TryGetProperty("value", out var vv) ? vv.GetDouble() : 0;
                bool factor = parentField != null && parentField.Contains("actor", StringComparison.OrdinalIgnoreCase);
                atoms.Add((factor ? "statF:" : "stat:") + stat + ":" + Dir(factor ? v - 1 : v));
                return;
            }
            if (type != null && type.EndsWith("PawnCapacityModifier"))
            {
                string cap = el.GetProperty("capacity").GetString();
                if (el.TryGetProperty("offset", out var o)) atoms.Add("cap:" + cap + ":" + Dir(o.GetDouble()));
                if (el.TryGetProperty("factor", out var f)) atoms.Add("capF:" + cap + ":" + Dir(f.GetDouble() - 1));
                if (el.TryGetProperty("postFactor", out var pf)) atoms.Add("capF:" + cap + ":" + Dir(pf.GetDouble() - 1));
                if (el.TryGetProperty("setMax", out var sm)) atoms.Add("capMax:" + cap + ":" + sm.GetDouble().ToString("0.##"));
                return;
            }
            if (type != null && type.EndsWith(".Aptitude"))
            {
                atoms.Add("apt:" + el.GetProperty("skill").GetString() + ":" + Dir(el.GetProperty("level").GetDouble()));
                return;
            }
            foreach (var p in el.EnumerateObject())
            {
                if (isRoot && skipFields.Contains(p.Name)) continue;
                if (p.Name == "$type" || p.Name == "$defType") continue;
                // Numeric effect fields directly on the gene (painFactor, foodPoisonChance, ...).
                if (isRoot && p.Value.ValueKind == JsonValueKind.Number)
                {
                    double v = p.Value.GetDouble();
                    string n = p.Name;
                    if (n.EndsWith("Factor") || n.EndsWith("FactorNonStackable")) atoms.Add("field:" + n + ":" + Dir(v - 1));
                    else if (n.EndsWith("Offset") || n.EndsWith("Chance") || n.EndsWith("MtbDays") || n.EndsWith("PerDay")) atoms.Add("field:" + n + ":" + Dir(v));
                    else atoms.Add("field:" + n + "=" + v.ToString("0.###"));
                    continue;
                }
                Collect(p.Value, p.Name, atoms, visited, depth + 1);
            }
            return;
        case JsonValueKind.Array:
            foreach (var item in el.EnumerateArray()) Collect(item, parentField, atoms, visited, depth + 1);
            return;
        case JsonValueKind.String:
            string s = el.GetString();
            foreach (var rt in refTypes)
            {
                string key = rt + ":" + s;
                if (!refKeys.Contains(key)) continue;
                if (rt == "AbilityDef") atoms.Add("ability:" + s);
                else if (rt == "ThoughtDef") atoms.Add("thought:" + s);
                else if (rt == "NeedDef") atoms.Add("need:" + s);
                else if (rt == "MentalBreakDef") atoms.Add("break:" + s);
                else atoms.Add("hediff:" + s);
                if (visited.Add(key) && rt == "HediffDef")
                    Collect(referenced.GetProperty(key), null, atoms, visited, depth + 1);
                return;
            }
            return;
        case JsonValueKind.True:
            if (isRoot || parentField != null) atoms.Add("flag:" + parentField);
            return;
        default:
            return;
    }
}

static string Dir(double v) => v > 0.0001 ? "+" : v < -0.0001 ? "-" : "0";

// ---- duplicates.md: identical signatures ----
var groups = sigs.Where(kv => kv.Value.Count > 0)
    .GroupBy(kv => string.Join("|", kv.Value), kv => kv.Key)
    .Where(gr => gr.Count() > 1)
    .Select(gr => (atoms: sigs[gr.First()], members: gr.OrderBy(x => x).ToList()))
    .OrderByDescending(gr => gr.members.Select(mm => geneMeta[mm].mod).Distinct().Count())
    .ThenByDescending(gr => gr.members.Count)
    .ToList();

var sb = new StringBuilder();
sb.AppendLine("# Duplicate candidates — identical effect signatures");
sb.AppendLine();
sb.AppendLine($"Genes with a non-empty signature: {sigs.Count(kv => kv.Value.Count > 0)} / {sigs.Count} ({generatedSkipped} runtime-generated derivatives excluded). Groups: {groups.Count} (cross-mod first).");
sb.AppendLine();
int gi = 0;
foreach (var (atoms, members) in groups)
{
    int modCount = members.Select(mm => geneMeta[mm].mod).Distinct().Count();
    sb.AppendLine($"## Group {++gi} — {members.Count} genes, {modCount} mod(s)");
    sb.AppendLine();
    sb.AppendLine("Signature: `" + string.Join("`, `", atoms.Take(20)) + (atoms.Count > 20 ? $"` … +{atoms.Count - 20} more" : "`"));
    sb.AppendLine();
    foreach (var mm in members)
    {
        var (mod, cls, met, cpx, arc) = geneMeta[mm];
        string xs = geneToXenotypes.TryGetValue(mm, out var xl) ? $" — xenotypes: {string.Join(", ", xl.Take(6))}{(xl.Count > 6 ? $" +{xl.Count - 6}" : "")}" : " — no xenotype";
        sb.AppendLine($"- **{mm}** ({mod}) met {met}, cpx {cpx}{(arc != 0 ? $", arc {arc}" : "")}{xs}");
    }
    sb.AppendLine();
}
File.WriteAllText(Path.Combine(outDir, "duplicates.md"), sb.ToString());

// ---- overlaps.md: atom -> genes index ----
var atomIndex = new Dictionary<string, List<string>>();
foreach (var (gene, atoms) in sigs)
    foreach (var a in atoms)
    {
        if (!atomIndex.TryGetValue(a, out var list)) atomIndex[a] = list = new List<string>();
        list.Add(gene);
    }
sb = new StringBuilder();
sb.AppendLine("# Effect overlap index — every atom touched by 2+ genes");
sb.AppendLine();
foreach (var (atom, list) in atomIndex.Where(kv => kv.Value.Count > 1)
             .OrderByDescending(kv => kv.Value.Count))
{
    sb.AppendLine($"## `{atom}` — {list.Count} genes");
    sb.AppendLine();
    foreach (var gene in list.OrderBy(x => x))
        sb.AppendLine($"- {gene} ({geneMeta[gene].mod}, met {geneMeta[gene].met})");
    sb.AppendLine();
}
File.WriteAllText(Path.Combine(outDir, "overlaps.md"), sb.ToString());

// ---- cosmetics.md: empty signatures ----
sb = new StringBuilder();
sb.AppendLine("# Cosmetic / no-effect genes (consolidation + zero-metabolism candidates)");
sb.AppendLine();
var cosmetics = sigs.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).OrderBy(x => geneMeta[x].mod).ThenBy(x => x).ToList();
sb.AppendLine($"{cosmetics.Count} genes with no detected functional effect.");
sb.AppendLine();
string lastMod = null;
foreach (var gene in cosmetics)
{
    var (mod, cls, met, cpx, arc) = geneMeta[gene];
    if (mod != lastMod) { sb.AppendLine(); sb.AppendLine($"## {mod}"); sb.AppendLine(); lastMod = mod; }
    string cost = (met != 0 || cpx != 0 || arc != 0) ? $" — **met {met}, cpx {cpx}{(arc != 0 ? $", arc {arc}" : "")}**" : "";
    sb.AppendLine($"- {gene}{cost}");
}
File.WriteAllText(Path.Combine(outDir, "cosmetics.md"), sb.ToString());

// ---- xenotypes.md: per-xenotype gene loadout with biostat totals ----
var geneLabel = new Dictionary<string, string>();
foreach (var g in genes.EnumerateArray())
{
    string n = g.GetProperty("defName").GetString();
    geneLabel[n] = g.TryGetProperty("label", out var lb) ? lb.GetString() : n;
}
string ShortMod(string mod)
{
    if (mod == null) return "?";
    int br = mod.IndexOf(" [");
    return br > 0 ? mod.Substring(0, br) : mod;
}
sb = new StringBuilder();
sb.AppendLine("# Xenotypes — post-cleanup loadouts");
sb.AppendLine();
var xlist = xenotypes.EnumerateArray()
    .OrderBy(x => ShortMod(x.TryGetProperty("mod", out var xm) ? xm.GetString() : "?"), StringComparer.Ordinal)
    .ThenBy(x => x.GetProperty("defName").GetString(), StringComparer.Ordinal)
    .ToList();
sb.AppendLine("| Xenotype | Mod | Genes | Met | Cpx | Arc | Inheritable | CPF |");
sb.AppendLine("|---|---|---|---|---|---|---|---|");
var xrows = new List<(JsonElement x, List<string> gl, int met, int cpx, int arc)>();
foreach (var x in xlist)
{
    var gl = new List<string>();
    if (x.TryGetProperty("genes", out var xg) && xg.ValueKind == JsonValueKind.Array)
        foreach (var g in xg.EnumerateArray()) if (g.GetString() is string gs) gl.Add(gs);
    int met = 0, cpx = 0, arc = 0;
    foreach (var gn in gl)
        if (geneMeta.TryGetValue(gn, out var gm)) { met += gm.met; cpx += gm.cpx; arc += gm.arc; }
    xrows.Add((x, gl, met, cpx, arc));
    bool inh = x.TryGetProperty("inheritable", out var iv) && iv.ValueKind == JsonValueKind.True;
    string cpf = x.TryGetProperty("combatPowerFactor", out var cf) ? cf.GetDouble().ToString("0.##") : "1";
    sb.AppendLine($"| {x.GetProperty("defName").GetString()} | {ShortMod(x.TryGetProperty("mod", out var m2) ? m2.GetString() : "?")} | {gl.Count} | {met} | {cpx} | {arc} | {(inh ? "yes" : "no")} | {cpf} |");
}
sb.AppendLine();
foreach (var (x, gl, met, cpx, arc) in xrows)
{
    string dn = x.GetProperty("defName").GetString();
    string mod = ShortMod(x.TryGetProperty("mod", out var m3) ? m3.GetString() : "?");
    bool inh = x.TryGetProperty("inheritable", out var iv) && iv.ValueKind == JsonValueKind.True;
    sb.AppendLine($"## {dn} ({(x.TryGetProperty("label", out var xl) ? xl.GetString() : dn)}) — {mod}");
    sb.AppendLine();
    sb.Append($"Met {met}, Cpx {cpx}");
    if (arc != 0) sb.Append($", Arc {arc}");
    if (inh) sb.Append(" — inheritable (germline)");
    if (x.TryGetProperty("combatPowerFactor", out var cf2)) sb.Append($" — combatPowerFactor {cf2.GetDouble():0.##}");
    sb.AppendLine();
    sb.AppendLine();
    foreach (var gn in gl)
    {
        if (!geneMeta.TryGetValue(gn, out var gm))
        {
            // Runtime-generated derivative or unknown; still list it.
            sb.AppendLine($"- {gn} (?)");
            continue;
        }
        var atoms = sigs.TryGetValue(gn, out var at) ? at : null;
        string eff = atoms == null || atoms.Count == 0
            ? "cosmetic/none"
            : string.Join(", ", atoms.Take(8)) + (atoms.Count > 8 ? $" +{atoms.Count - 8} more" : "");
        string cost = $"met {gm.met}, cpx {gm.cpx}" + (gm.arc != 0 ? $", arc {gm.arc}" : "");
        sb.AppendLine($"- **{gn}** \"{geneLabel.GetValueOrDefault(gn, gn)}\" ({ShortMod(gm.mod)}; {cost}) — {eff}");
    }
    sb.AppendLine();
}
File.WriteAllText(Path.Combine(outDir, "xenotypes.md"), sb.ToString());

Console.WriteLine($"genes: {sigs.Count}, with signature: {sigs.Count(kv => kv.Value.Count > 0)}, cosmetic: {cosmetics.Count}");
Console.WriteLine($"duplicate groups: {groups.Count} ({groups.Sum(g => g.members.Count)} genes)");
Console.WriteLine($"reports -> {outDir}");
