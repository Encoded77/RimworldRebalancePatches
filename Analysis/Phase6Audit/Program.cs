using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string dumpDir = args.Length > 0 ? args[0] : Path.Combine(home, "AppData", "LocalLow", "Ludeon Studios",
    "RimWorld by Ludeon Studios", "RebalancePatches", "tmp");
string repo = args.Length > 1 ? args[1] : @"C:\Code\RimworldRebalancePatches";
string outDir = Path.Combine(repo, "Analysis", "out");
Directory.CreateDirectory(outDir);

const string Tab = "RBP_CyberneticsTab";
const string Root = "RBP_CybSurgicalImplantation";

Console.WriteLine($"dumps: {dumpDir}");
var things = Load("ThingDump.json");
var recipes = Load("RecipeDump.json");
var hediffs = Load("HediffDump.json");
var research = Load("ResearchDump.json");
var acquisition = Load("AcquisitionDump.json");

JsonDocument Load(string f)
{
    string p = Path.Combine(dumpDir, f);
    if (!File.Exists(p)) throw new FileNotFoundException($"Missing dump: {p}");
    return JsonDocument.Parse(File.ReadAllText(p));
}

string dumpedAt = research.RootElement.GetProperty("meta").GetProperty("dumpedAt").GetString();
string gameVersion = research.RootElement.GetProperty("meta").GetProperty("gameVersion").GetString();

// ---- research ---------------------------------------------------------------------------------

var projects = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (var p in research.RootElement.GetProperty("projects").EnumerateArray())
    projects[Str(p, "defName")] = p;

var tabProjects = projects.Where(p => Str(p.Value, "tab") == Tab)
    .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
Console.WriteLine($"{projects.Count} projects, {tabProjects.Count} on {Tab}");

var trunkOfNode = new Dictionary<string, string>(StringComparer.Ordinal);
var trunkFiles = new (string File, string Trunk)[]
{
    ("CyberneticsResearch.xml", "root"),
    ("CyberneticsResearchBody.xml", "body"),
    ("CyberneticsResearchModules.xml", "modules"),
    ("CyberneticsResearchCyberbrains.xml", "cyberbrains"),
    ("CyberneticsResearchMind.xml", "mind"),
    ("CyberneticsResearchCapstones.xml", "capstones"),
};
var unresolvedTrunkFiles = new List<string>();
foreach (var (file, trunk) in trunkFiles)
{
    string path = Path.Combine(repo, "1.6", "CyberneticsResearch", "Patches", file);
    if (!File.Exists(path)) { unresolvedTrunkFiles.Add(file); continue; }
    string text = File.ReadAllText(path);
    foreach (Match m in Regex.Matches(text, @"<defName>([A-Za-z0-9_]+)</defName>"))
        if (!trunkOfNode.ContainsKey(m.Groups[1].Value)) trunkOfNode[m.Groups[1].Value] = trunk;
    foreach (Match m in Regex.Matches(text, @"ResearchProjectDef\[defName=""([A-Za-z0-9_]+)""\]"))
        if (!trunkOfNode.ContainsKey(m.Groups[1].Value)) trunkOfNode[m.Groups[1].Value] = trunk;
}

string TrunkOf(IEnumerable<string> gates)
{
    var t = gates.Where(g => tabProjects.ContainsKey(g))
        .Select(g => trunkOfNode.GetValueOrDefault(g, "(unmapped)"))
        .Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
    return t.Count == 0 ? "—" : string.Join(" + ", t);
}

// ---- hediffs ----------------------------------------------------------------------------------

var hediffById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (var h in hediffs.RootElement.GetProperty("hediffs").EnumerateArray())
    hediffById[Str(h, "defName")] = h;
Console.WriteLine($"{hediffById.Count} hediffs");

// ---- things -----------------------------------------------------------------------------------

var thingById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (var t in things.RootElement.GetProperty("things").EnumerateArray())
    thingById[Str(t, "defName")] = t;
Console.WriteLine($"{thingById.Count} things");

// ---- recipes ----------------------------------------------------------------------------------

var recipeById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (var r in recipes.RootElement.GetProperty("recipes").EnumerateArray())
    recipeById[Str(r, "defName")] = r;
Console.WriteLine($"{recipeById.Count} recipes");

// thing -> surgeries that install or remove it
var surgeriesByItem = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
// thing -> bench recipes that produce it
var producedBy = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
foreach (var r in recipeById.Values)
{
    bool surgery = Bool(r, "isSurgery");
    if (surgery)
    {
        string implant = Str(r, "implantIngredient");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (implant != null) ids.Add(implant);
        foreach (string i in Ingredients(r).SelectMany(i => i.Allowed)) ids.Add(i);
        foreach (string i in ids)
        {
            if (!thingById.ContainsKey(i)) continue;
            if (!surgeriesByItem.TryGetValue(i, out var l)) surgeriesByItem[i] = l = new();
            l.Add(r);
        }
    }
    if (r.TryGetProperty("products", out var prods) && prods.ValueKind == JsonValueKind.Array)
        foreach (var p in prods.EnumerateArray())
        {
            string td = Str(p, "thingDef");
            if (td == null) continue;
            if (!producedBy.TryGetValue(td, out var l)) producedBy[td] = l = new();
            l.Add(r);
        }
}

// ---- traders ----------------------------------------------------------------------------------

var tradersFor = new Dictionary<string, int>(StringComparer.Ordinal);
if (acquisition.RootElement.TryGetProperty("traders", out var traderBlock))
    foreach (var e in traderBlock.EnumerateObject())
        tradersFor[e.Name] = e.Value.EnumerateArray().Select(x => Str(x, "trader")).Distinct().Count();

// Group 3: every surgery moved onto the root node.
var rootSurgeries = recipeById.Values
    .Where(r => Bool(r, "isSurgery") && ResearchOf(r).Contains(Root)).ToList();
Console.WriteLine($"{rootSurgeries.Count} surgeries on {Root}");

var rows = new Dictionary<string, Row>(StringComparer.Ordinal);

Row RowFor(string defName)
{
    if (rows.TryGetValue(defName, out var existing)) return existing;
    var t = thingById[defName];
    var row = new Row { DefName = defName, Element = t };
    rows[defName] = row;
    return row;
}

foreach (var t in thingById.Values)
{
    string name = Str(t, "defName");
    var gates = CraftGates(t);
    bool onTab = gates.Any(g => tabProjects.ContainsKey(g));
    bool ours = name.StartsWith("RBP_", StringComparison.Ordinal);
    if (!onTab && !ours) continue;
    var row = RowFor(name);
    if (onTab) row.Reasons.Add("tab-gated");
    if (ours) row.Reasons.Add("RBP_ authored");
}

var rootOnlyItems = new HashSet<string>(StringComparer.Ordinal);
int rootIngredientNoise = 0;
foreach (var r in rootSurgeries)
{
    string implant = Str(r, "implantIngredient");
    if (implant == null || !thingById.ContainsKey(implant)) continue;
    if (rows.ContainsKey(implant)) { rows[implant].Reasons.Add("root surgery"); continue; }
    if (!Bool(thingById[implant], "isImplantLike")) { rootIngredientNoise++; continue; }
    rootOnlyItems.Add(implant);
}
Console.WriteLine($"{rows.Count} primary rows, {rootOnlyItems.Count} root-surgery-only items");

// ---- populate ---------------------------------------------------------------------------------

foreach (var row in rows.Values) Populate(row);
var rootOnlyRows = new List<Row>();
foreach (string n in rootOnlyItems)
{
    var row = new Row { DefName = n, Element = thingById[n] };
    row.Reasons.Add("root surgery");
    Populate(row);
    rootOnlyRows.Add(row);
}

void Populate(Row row)
{
    var t = row.Element;
    row.Label = Str(t, "label") ?? row.DefName;
    row.Mod = ShortMod(Str(t, "mod"));
    row.TechLevel = Str(t, "techLevel") ?? "—";
    var stats = t.TryGetProperty("resolvedStats", out var s) ? s : default;
    row.MarketValue = stats.ValueKind == JsonValueKind.Object ? Num(stats, "MarketValue") : 0;
    row.WorkToMake = stats.ValueKind == JsonValueKind.Object ? Num(stats, "WorkToMake") : 0;
    row.Mass = stats.ValueKind == JsonValueKind.Object ? Num(stats, "Mass") : 0;
    row.Gates = CraftGates(t);
    row.Trunk = TrunkOf(row.Gates);
    row.Traders = tradersFor.GetValueOrDefault(row.DefName);
    row.Category = Str(t, "category") ?? "Item";
    row.IsApparel = t.TryGetProperty("apparel", out _);
    row.IsImplantLike = Bool(t, "isImplantLike");

    // --- cost ---
    if (t.TryGetProperty("costList", out var cl) && cl.ValueKind == JsonValueKind.Array)
    {
        foreach (var c in cl.EnumerateArray())
        {
            string td = Str(c, "thingDef");
            if (td != null) row.Cost.Add((td, (int)NumOr(c, "count", 1f)));
        }
        row.CostSource = "costList";
    }
    int stuff = (int)Num(t, "costStuffCount");
    if (stuff > 0)
    {
        var cats = Strings(t, "stuffCategories");
        row.Cost.Add((cats.Count > 0 ? $"[stuff: {string.Join("/", cats)}]" : "[stuff]", stuff));
        row.CostSource = row.CostSource == null ? "costStuffCount" : row.CostSource + "+stuff";
    }
    // A standalone RecipeDef producing the item is the other way to be craftable.
    var makers = producedBy.GetValueOrDefault(row.DefName) ?? new List<JsonElement>();
    row.ProducingRecipes = makers.Select(m => Str(m, "defName")).ToList();
    int primaryIdx = makers.FindIndex(m => (Str(m, "defName") ?? "").StartsWith("Make_", StringComparison.Ordinal));
    if (row.Cost.Count == 0 && makers.Count > 0)
    {
        var m = makers[primaryIdx >= 0 ? primaryIdx : 0];
        foreach (var ing in Ingredients(m))
            row.Cost.Add((ing.Allowed.Count == 1 ? ing.Allowed[0] : $"[any of {ing.Allowed.Count}: {ing.Allowed[0]}…]", ing.Count));
        row.CostSource = $"recipe {Str(m, "defName")}";
    }
    // Work: recipeMaker.workAmount wins, then the resolved WorkToMake stat, then a producing recipe.
    if (t.TryGetProperty("recipeMaker", out var rm) && rm.ValueKind == JsonValueKind.Object)
    {
        row.HasRecipeMaker = true;
        float w = Num(rm, "workAmount");
        if (w > 0) { row.Work = w; row.WorkSource = "recipeMaker.workAmount"; }
        row.Benches = Strings(rm, "recipeUsers");
    }
    if (row.Work <= 0 && row.HasRecipeMaker && row.WorkToMake > 0)
    {
        row.Work = row.WorkToMake;
        row.WorkSource = "stat WorkToMake (recipeMaker bill)";
    }
    if (row.Work <= 0 && makers.Count > 0)
    {
        var maker = makers[primaryIdx >= 0 ? primaryIdx : 0];
        row.Work = Num(maker, "workAmount");
        row.WorkSource = $"recipe {Str(maker, "defName")}.workAmount";
        if (row.Benches.Count == 0) row.Benches = Strings(maker, "recipeUsers");
    }
    if (row.Work <= 0 && row.WorkToMake > 0) { row.Work = row.WorkToMake; row.WorkSource = "stat WorkToMake"; }
    row.Craftable = row.Cost.Count > 0 || row.HasRecipeMaker || makers.Count > 0;
    if (!row.Craftable) { row.Work = 0; row.WorkSource = null; }

    // --- what it does ---
    foreach (var sg in surgeriesByItem.GetValueOrDefault(row.DefName) ?? new List<JsonElement>())
    {
        string adds = Str(sg, "addsHediff");
        string rem = Str(sg, "removesHediff");
        string sname = Str(sg, "defName");
        bool install = adds != null;
        row.Surgeries.Add(new SurgeryRef
        {
            DefName = sname,
            Label = Str(sg, "label") ?? sname,
            Adds = adds,
            Removes = rem,
            Parts = Strings(sg, "appliedOnFixedBodyParts"),
            Research = ResearchOf(sg),
            Work = Num(sg, "workAmount"),
            IsInstall = install,
        });
        if (adds != null) row.Hediffs.Add(adds);
        foreach (string p in Strings(sg, "appliedOnFixedBodyParts")) row.Parts.Add(p);
    }
    // Modules install through a comp, not a surgery.
    if (t.TryGetProperty("comps", out var comps) && comps.ValueKind == JsonValueKind.Array)
        foreach (var c in comps.EnumerateArray())
        {
            string ty = Str(c, "$type") ?? "";
            row.CompTypes.Add(ShortType(ty));
            if (ty.Contains("UseEffectHediffModule", StringComparison.Ordinal)
                || ty.Contains("HediffModule", StringComparison.Ordinal))
            {
                row.IsModule = true;
                row.Slots.AddRange(Strings(c, "slotIDs"));
                foreach (string h in Strings(c, "hediffs")) row.Hediffs.Add(h);
            }
            if (ty.Contains("Usable", StringComparison.Ordinal)) row.SelfInstall = true;
            if (ty.Contains("UseEffectInstallImplant", StringComparison.Ordinal)
                || ty.Contains("UseEffect", StringComparison.Ordinal))
                foreach (string h in Strings(c, "hediffDefs").Concat(Strings(c, "hediffs")))
                    row.Hediffs.Add(h);
            string single = Str(c, "hediffDef");
            if (single != null && hediffById.ContainsKey(single)) row.Hediffs.Add(single);
        }
    row.Hediffs = row.Hediffs.Distinct().ToList();
    foreach (string h in row.Hediffs)
        if (!hediffById.ContainsKey(h)) row.Unresolved.Add(h);

    row.Effect = string.Join(" <br> ", row.Hediffs.Where(hediffById.ContainsKey).Select(DescribeHediff));
    if (row.Effect.Length == 0 && row.Hediffs.Count > 0) row.Effect = "*(hediff not in dump)*";
    row.PartEfficiency = row.Hediffs.Where(hediffById.ContainsKey)
        .Select(h => hediffById[h].TryGetProperty("addedPartProps", out var a) ? Num(a, "partEfficiency") : 0f)
        .DefaultIfEmpty(0f).Max();
}

var limbParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{ "Arm","Hand","Leg","Foot","Finger","Toe","Shoulder","Clavicle","Humerus","Radius","Femur","Tibia","Pelvis","Waist" };
var senseParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{ "Eye","Ear","Nose","Jaw","Tongue" };
var organParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{ "Heart","Lung","Kidney","Liver","Stomach","Neck","Artery","Intestine","Bladder","Pancreas","Spleen" };
var torsoParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{ "Torso","Spine","Skull","Body","Rib","Ribcage","Sternum" };

var weaponNodes = new HashSet<string>(StringComparer.Ordinal)
{ "RBP_CybIntegralMelee", "RBP_CybIntegralRanged", "RBP_CybUltratechWeaponModules" };

var psychicStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{ "PsychicSensitivity","MeditationFocusGain","PsychicEntropyMax","PsychicEntropyRecoveryRate","PsychicSensitivityOffset" };

// An item that installs nothing but is consumed making something else in this audit.
var usedAsIngredient = new HashSet<string>(
    rows.Values.Concat(rootOnlyRows).SelectMany(r => r.Cost).Select(c => c.Def), StringComparer.Ordinal);

HashSet<string> StatsTouched(Row r)
{
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (string h in r.Hediffs)
    {
        if (!hediffById.TryGetValue(h, out var hd)) continue;
        if (!hd.TryGetProperty("stages", out var st) || st.ValueKind != JsonValueKind.Array) continue;
        foreach (var stage in st.EnumerateArray())
            foreach (string prop in new[] { "statOffsets", "statFactors" })
                if (stage.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    foreach (var m in arr.EnumerateArray())
                        if (Str(m, "stat") is string s) result.Add(s);
    }
    return result;
}

bool HasWeaponComp(Row r) => r.Hediffs.Any(h => hediffById.TryGetValue(h, out var hd)
    && hd.TryGetProperty("comps", out var c) && c.ValueKind == JsonValueKind.Array
    && c.GetRawText().Contains("VerbGiver", StringComparison.Ordinal)
        | c.GetRawText().Contains("MeleeWeapon", StringComparison.Ordinal));

string RoleOf(Row r)
{
    string text = (r.DefName + " " + r.Label + " " + string.Join(" ", r.Hediffs)).ToLowerInvariant();
    var stats = StatsTouched(r);

    // Bare parts, BS_ synonyms stripped.
    var bare = r.Parts.Where(p => !p.StartsWith("BS_", StringComparison.Ordinal)).ToList();
    int limbs = bare.Count(limbParts.Contains);
    int senses = bare.Count(senseParts.Contains);
    int organs = bare.Count(organParts.Contains);
    int torso = bare.Count(torsoParts.Contains);
    bool brain = bare.Any(p => p.Contains("Brain", StringComparison.OrdinalIgnoreCase));

    string Base()
    {
        if (r.Category == "Building") return "Benches & buildings";
        if (r.Gates.Any(g => trunkOfNode.GetValueOrDefault(g) == "capstones")) return "Capstone items";
        if (r.Gates.Any(weaponNodes.Contains)) return "Weapon modules";
        // Organ optimizers are a node of their own; their nanobots touch half the body.
        if (r.Gates.Contains("RBP_CybOrganOptimizers")) return "Organs";
        if (Regex.IsMatch(text, @"cyberbrain|cortical stack|neural stack|archotech stack")) return "Cyberbrains";
        if (brain) return r.PartEfficiency > 0 ? "Cyberbrains" : "Neural add-ons";
        if (r.IsModule || r.Slots.Count > 0) return "Modules";
        int best = Math.Max(Math.Max(limbs, senses), Math.Max(organs, torso));
        if (best > 0)
        {
            if (limbs == best) return "Limbs";
            if (senses == best) return "Senses";
            if (organs == best) return "Organs";
            return "Torso frames";
        }
        if (Regex.IsMatch(text, @"thoracic|exoskeleton|\brib\b|frame")) return "Torso frames";
        if (HasWeaponComp(r)) return "Weapon modules";
        if (r.SelfInstall || Regex.IsMatch(text, @"trainer|serum|injector|\bkit\b|drug")) return "Consumables";
        if (r.Hediffs.Count > 0 || r.Surgeries.Count > 0) return "Modules";
        if (usedAsIngredient.Contains(r.DefName)) return "Intermediate components";
        return "Other";
    }

    string role = Base();
    if ((role == "Modules" || role == "Neural add-ons")
        && (stats.Overlaps(psychicStats) || r.Mod.Contains("Psychic", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(text, @"psy(focus|link|cast|chic)|eltex|psytrainer|nerve spool")))
        role = "Psychic implants";
    return role;
}

foreach (var r in rows.Values.Concat(rootOnlyRows))
{
    r.IsCapstone = r.Gates.Any(g => trunkOfNode.GetValueOrDefault(g) == "capstones");
    r.Role = RoleOf(r);
}

if (Environment.GetEnvironmentVariable("PHASE6_DIAG") == "1")
{
    var diag = new StringBuilder();
    diag.AppendLine("role\tdefName\tlabel\tmod\tvalue\tgates\tparts\tslots\tcomps\thediffs\tcapstone");
    foreach (var r in rows.Values.Concat(rootOnlyRows).OrderBy(r => r.Role, StringComparer.Ordinal))
        diag.AppendLine($"{r.Role}\t{r.DefName}\t{r.Label}\t{r.Mod}\t{F(r.MarketValue)}\t{string.Join("|", r.Gates)}\t" +
                        $"{string.Join("|", r.Parts)}\t{string.Join("|", r.Slots)}\t{string.Join("|", r.CompTypes)}\t" +
                        $"{string.Join("|", r.Hediffs)}\t{r.IsCapstone}");
    File.WriteAllText(Path.Combine(outDir, "phase6-diag.tsv"), diag.ToString());
    Console.WriteLine("wrote phase6-diag.tsv");
}

var main = rows.Values.OrderBy(r => r.Role, StringComparer.Ordinal).ToList();
var sb = new StringBuilder();

sb.AppendLine("# Phase 6 — cost and effect audit");
sb.AppendLine();
sb.AppendLine("Every item the cybernetics overhaul touches, with what it costs to make, what it is");
sb.AppendLine("worth, and what its hediff actually does. **This is a measurement, not a proposal** —");
sb.AppendLine("nothing here is a recommended value.");
sb.AppendLine();
sb.AppendLine("## How this was produced");
sb.AppendLine();
sb.AppendLine($"- Generated by `Analysis/Phase6Audit` from the dumps in `RebalancePatches/tmp`, dumped **{dumpedAt}** on RimWorld **{gameVersion}**.");
sb.AppendLine($"- Dumps read: `ThingDump.json` ({thingById.Count} items/buildings), `RecipeDump.json` ({recipeById.Count} recipes), `HediffDump.json` ({hediffById.Count} hediffs), `ResearchDump.json` ({projects.Count} projects), `AcquisitionDump.json` (trader coverage).");
sb.AppendLine("- Each dump carries a `referenced` block that repeats defs by name; only the top-level `things` / `recipes` / `hediffs` / `projects` arrays are read, so nothing is double-counted. Counts above are the array lengths.");
sb.AppendLine("- All numbers come from `System.Text.Json` and are formatted with `InvariantCulture`.");
sb.AppendLine("- **Market value** and **work** are `resolvedStats.MarketValue` / `WorkToMake` — post-inheritance, post-stat-part, which is what the game shows.");
sb.AppendLine("- **Effect** is the *last* hediff stage's `statOffsets` / `statFactors` / `capMods`, plus `addedPartProps.partEfficiency` and comp classes. Multi-stage hediffs are marked.");
sb.AppendLine("- An item lists **every** hediff any surgery installs from it. An arm shows its replacement hediff *and* its `LeftExtra…` / `RightExtra…` variants, because the same item is also the ingredient of the extra-limb surgery. That is the item's real effect surface, not a duplicate.");
sb.AppendLine("- A gate marked `*` is off this tab. Multiple gates in one cell are conjunctive: the player needs all of them.");
sb.AppendLine("- **Work** resolves in this order: `recipeMaker.workAmount`; else the `WorkToMake` stat when the item has a `recipeMaker` (its auto-generated `Make_` bill reads that stat); else a standalone producing recipe's `workAmount`, preferring the `Make_` bill over upgrade paths; else `WorkToMake`. The order matters both ways — items with an upgrade recipe alongside their `Make_` bill otherwise report the upgrade as their build cost, and items with no `recipeMaker` resolve `WorkToMake` to the stat default of 1.");
sb.AppendLine("- **Ingredients** come from `costList` (plus `costStuffCount`), falling back to the producing recipe. An absent `count` in a `costList` entry means **1**, not 0 — `ThingDefCountClass.count` defaults to 1 and the dump omits fields still at their default.");
sb.AppendLine($"- **Trunk** is resolved from which overhaul file declares or repurposes the gating node, not from a coordinate heuristic. Nodes on the tab: {tabProjects.Count}.");
sb.AppendLine();

// scope summary
sb.AppendLine("### Scope");
sb.AppendLine();
sb.AppendLine("| Set | Rule | Items |");
sb.AppendLine("| --- | --- | ---: |");
sb.AppendLine($"| 1 | craft gate is a node on `{Tab}` | {rows.Values.Count(r => r.Reasons.Contains("tab-gated"))} |");
sb.AppendLine($"| 2 | defName starts `RBP_` | {rows.Values.Count(r => r.Reasons.Contains("RBP_ authored"))} |");
sb.AppendLine($"| 3 | install/removal surgery moved to `{Root}` | {rootSurgeries.Count} recipes → {rootOnlyItems.Count + rows.Values.Count(r => r.Reasons.Contains("root surgery"))} distinct items |");
sb.AppendLine($"| — | **union, de-duplicated (detailed below)** | **{rows.Count}** |");
sb.AppendLine($"| — | set 3 only, crafting gate untouched (summarised, not tabled) | {rootOnlyItems.Count} |");
sb.AppendLine();

var unresolvedAll = rows.Values.Concat(rootOnlyRows).SelectMany(r => r.Unresolved).Distinct()
    .OrderBy(x => x, StringComparer.Ordinal).ToList();
{
    sb.AppendLine("### Could not resolve from the dumps");
    sb.AppendLine();
    if (unresolvedAll.Count == 0 && unresolvedTrunkFiles.Count == 0)
        sb.AppendLine("- Every def referenced by an audited item resolved; every trunk file was found.");
    foreach (string u in unresolvedAll) sb.AppendLine($"- hediff `{u}` referenced by an item but absent from `HediffDump.json`");
    foreach (string f in unresolvedTrunkFiles) sb.AppendLine($"- trunk file `{f}` not found — nodes it owns show as `(unmapped)`");
    sb.AppendLine("- **Build cost of buildings.** `ThingDump` resolves `MarketValue`, `WorkToMake`, `Mass` and `MaxHitPoints` only. A workbench is built, not crafted, so its real cost is `WorkToBuild`, which is not in the dump — every building's work of `1` below is a placeholder. Buildings are excluded from all work-based tests for that reason.");
    sb.AppendLine("- **Quality and stuff.** Market values are the abstract, normal-quality, default-stuff figures. An item made from a different stuff or at a different quality is worth something else in play.");
    sb.AppendLine();
}

// ---- role groups ------------------------------------------------------------------------------

string[] roleOrder =
{
    "Limbs", "Organs", "Senses", "Torso frames", "Cyberbrains", "Neural add-ons", "Modules",
    "Psychic implants", "Weapon modules", "Capstone items", "Consumables",
    "Intermediate components", "Benches & buildings", "Other",
};

sb.AppendLine("## Totals by role");
sb.AppendLine();
sb.AppendLine("| Role | Items | Median value | Value range | Median work | No recipe |");
sb.AppendLine("| --- | ---: | ---: | --- | ---: | ---: |");
foreach (string role in roleOrder)
{
    var g = main.Where(r => r.Role == role).ToList();
    if (g.Count == 0) continue;
    sb.AppendLine($"| {role} | {g.Count} | {F(Median(g.Select(r => r.MarketValue)))} | {F(g.Min(r => r.MarketValue))} – {F(g.Max(r => r.MarketValue))} " +
                  $"| {F(Median(g.Where(r => r.Work > 0).Select(r => r.Work)))} | {g.Count(r => !r.Craftable)} |");
}
sb.AppendLine($"| **all** | **{main.Count}** | {F(Median(main.Select(r => r.MarketValue)))} | {F(main.Min(r => r.MarketValue))} – {F(main.Max(r => r.MarketValue))} | {F(Median(main.Where(r => r.Work > 0).Select(r => r.Work)))} | {main.Count(r => !r.Craftable)} |");
sb.AppendLine();

foreach (string role in roleOrder)
{
    var g = main.Where(r => r.Role == role).OrderByDescending(r => r.MarketValue).ToList();
    if (g.Count == 0) continue;
    sb.AppendLine($"## {role} — {g.Count}");
    sb.AppendLine();
    sb.AppendLine("| Item | defName | Mod | Tech | Trunk | Gated by | Value | Work | Ingredients | Effect |");
    sb.AppendLine("| --- | --- | --- | --- | --- | --- | ---: | ---: | --- | --- |");
    foreach (var r in g)
        sb.AppendLine($"| {r.Label}{(r.IsCapstone ? " ⭑" : "")} | `{r.DefName}` | {r.Mod} | {r.TechLevel} | {r.Trunk} | {GateCell(r)} " +
                      $"| {F(r.MarketValue)} | {(r.Work > 0 ? F(r.Work) : "—")} | {CostCell(r)} | {(r.Effect.Length > 0 ? r.Effect : "—")} |");
    sb.AppendLine();
    if (g.Any(r => r.IsCapstone)) sb.AppendLine("⭑ = gated on a capstone node.");
    sb.AppendLine();
}

// ---- set 3 summary ----------------------------------------------------------------------------

sb.AppendLine($"## Set 3 — items whose surgery moved to `{Root}` but whose crafting gate is untouched");
sb.AppendLine();
sb.AppendLine($"`{Root}` is the install/removal gate for **{rootSurgeries.Count} surgical recipes**. Those recipes name");
sb.AppendLine($"**{rootOnlyItems.Count}** distinct implant items that are not in the tables above — their crafting is gated");
sb.AppendLine("elsewhere (or not at all), so Phase 6 re-costing does not reach them through the tab. Listed by owning mod.");
sb.AppendLine();
sb.AppendLine($"({rootIngredientNoise} further root recipes name a non-implant as their \"implant ingredient\" — cloth, components, ");
sb.AppendLine("medicine on a repair or bandage recipe. Those are dropped rather than counted as items.)");
sb.AppendLine();
sb.AppendLine("| Mod | Recipes on the root | Items not otherwise in scope | Median value | Value range | Uncraftable |");
sb.AppendLine("| --- | ---: | ---: | ---: | --- | ---: |");
var rootByMod = rootSurgeries.GroupBy(r => ShortMod(Str(r, "mod")))
    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
foreach (var g in rootOnlyRows.GroupBy(r => r.Mod).OrderByDescending(g => g.Count()))
    sb.AppendLine($"| {g.Key} | {rootByMod.GetValueOrDefault(g.Key)} | {g.Count()} | {F(Median(g.Select(r => r.MarketValue)))} " +
                  $"| {F(g.Min(r => r.MarketValue))} – {F(g.Max(r => r.MarketValue))} | {g.Count(r => !r.Craftable)} |");
foreach (var g in rootByMod.Where(m => rootOnlyRows.All(r => r.Mod != m.Key)).OrderByDescending(m => m.Value))
    sb.AppendLine($"| {g.Key} | {g.Value} | 0 | — | — | — |");
sb.AppendLine();
sb.AppendLine("By role, the same set:");
sb.AppendLine();
sb.AppendLine("| Role | Items | Median value | Value range |");
sb.AppendLine("| --- | ---: | ---: | --- |");
foreach (var g in rootOnlyRows.GroupBy(r => r.Role).OrderByDescending(g => g.Count()))
    sb.AppendLine($"| {g.Key} | {g.Count()} | {F(Median(g.Select(r => r.MarketValue)))} | {F(g.Min(r => r.MarketValue))} – {F(g.Max(r => r.MarketValue))} |");
sb.AppendLine();
sb.AppendLine("The 42 items themselves, since 42 is small enough to read. Most are uncraftable by design — archotech");
sb.AppendLine("and natural body parts that are harvested, quest rewards, or bought.");
sb.AppendLine();
sb.AppendLine("| Item | defName | Mod | Role | Value | Craftable | Gated by |");
sb.AppendLine("| --- | --- | --- | --- | ---: | --- | --- |");
foreach (var r in rootOnlyRows.OrderBy(r => r.Role, StringComparer.Ordinal).ThenByDescending(r => r.MarketValue))
    sb.AppendLine($"| {r.Label} | `{r.DefName}` | {r.Mod} | {r.Role} | {F(r.MarketValue)} | {(r.Craftable ? "yes" : "**no**")} | {GateCell(r)} |");
sb.AppendLine();

// ---- findings: component inventory -------------------------------------------------------------

sb.AppendLine("# Findings");
sb.AppendLine();
sb.AppendLine("## Crafting component inventory");
sb.AppendLine();
sb.AppendLine("Every ingredient def used by an item in the detailed tables above, who owns it, what it is");
sb.AppendLine("worth, and how many audited items consume it. This is the reuse-across-mods question.");
sb.AppendLine();

var ingUse = new Dictionary<string, List<Row>>(StringComparer.Ordinal);
var ingQty = new Dictionary<string, int>(StringComparer.Ordinal);
foreach (var r in main)
    foreach (var (def, count) in r.Cost)
    {
        if (!ingUse.TryGetValue(def, out var l)) ingUse[def] = l = new();
        l.Add(r);
        ingQty[def] = ingQty.GetValueOrDefault(def) + count;
    }

sb.AppendLine("| Ingredient | defName | Owning mod | Unit value | Items using it | Total units | Used by roles |");
sb.AppendLine("| --- | --- | --- | ---: | ---: | ---: | --- |");
foreach (var pair in ingUse.OrderByDescending(p => p.Value.Count).ThenBy(p => p.Key, StringComparer.Ordinal))
{
    var td = thingById.GetValueOrDefault(pair.Key);
    string label = td.ValueKind == JsonValueKind.Object ? Str(td, "label") ?? pair.Key : pair.Key;
    string mod = td.ValueKind == JsonValueKind.Object ? ShortMod(Str(td, "mod")) : "—";
    string val = td.ValueKind == JsonValueKind.Object && td.TryGetProperty("resolvedStats", out var st)
        ? F(Num(st, "MarketValue")) : "—";
    var roles = pair.Value.Select(r => r.Role).Distinct().OrderBy(x => x, StringComparer.Ordinal);
    sb.AppendLine($"| {label} | `{pair.Key}` | {mod} | {val} | **{pair.Value.Count}** | {ingQty[pair.Key]} | {string.Join(", ", roles)} |");
}
sb.AppendLine();

var modOwners = ingUse.Keys.Select(k => thingById.TryGetValue(k, out var td) ? ShortMod(Str(td, "mod")) : "—")
    .GroupBy(x => x).OrderByDescending(g => g.Count()).ToList();
sb.AppendLine($"**{ingUse.Count} distinct ingredient defs** across {modOwners.Count} owning mods: " +
              string.Join(", ", modOwners.Select(g => $"{g.Key} ({g.Count()})")) + ".");
sb.AppendLine();

// ---- findings: outliers -----------------------------------------------------------------------

string TierOf(Row r) => r.PartEfficiency <= 0 ? "add-on"
    : r.PartEfficiency >= 1.45f ? "archotech"
    : r.PartEfficiency >= 1.30f ? "advanced"
    : r.PartEfficiency >= 1.15f ? "bionic" : "prosthetic";
string[] tierOrder = { "prosthetic", "bionic", "advanced", "archotech", "add-on" };
foreach (var r in main) r.Tier = TierOf(r);

sb.AppendLine("## Reference — median value and work per role and tier");
sb.AppendLine();
sb.AppendLine("Tier is read from `addedPartProps.partEfficiency` (prosthetic <1.15, bionic <1.30, advanced <1.45,");
sb.AppendLine("archotech ≥1.45); *add-on* means the hediff replaces no body part, so it has no efficiency and no tier.");
sb.AppendLine("A role median that spans prosthetic to archotech is not a baseline anything can be measured against —");
sb.AppendLine("this table is the one the outlier test below actually uses.");
sb.AppendLine();
sb.AppendLine("| Role | " + string.Join(" | ", tierOrder) + " |");
sb.AppendLine("| --- |" + string.Concat(tierOrder.Select(_ => " ---: |")));
foreach (string role in roleOrder)
{
    var g = main.Where(r => r.Role == role).ToList();
    if (g.Count == 0) continue;
    var cells = tierOrder.Select(t =>
    {
        var tg = g.Where(r => r.Tier == t).ToList();
        return tg.Count == 0 ? "—" : $"{F(Median(tg.Select(r => r.MarketValue)))} <sub>n={tg.Count}</sub>";
    });
    sb.AppendLine($"| {role} | {string.Join(" | ", cells)} |");
}
sb.AppendLine();

sb.AppendLine("## Outliers");
sb.AppendLine();
sb.AppendLine("Two independent tests, because an item can be in line on one axis and wildly out on the other.");
sb.AppendLine();
sb.AppendLine("**A — value against its own role *and tier*.** Flagged at more than 3× or less than a third of the");
sb.AppendLine("median of role+tier groups with at least three members. Comparing a prosthetic hand against a");
sb.AppendLine("role median that includes archotech arms flags the whole entry tier and says nothing.");
sb.AppendLine();
sb.AppendLine("| Item | Role | Tier | Value | Group median | ×median | Work | Group median | ×median |");
sb.AppendLine("| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |");
int outlierCount = 0;
foreach (var group in main.GroupBy(r => (r.Role, r.Tier))
             .OrderBy(g => Array.IndexOf(roleOrder, g.Key.Role)).ThenBy(g => Array.IndexOf(tierOrder, g.Key.Tier)))
{
    var g = group.ToList();
    if (g.Count < 3) continue;
    float mv = Median(g.Select(r => r.MarketValue));
    float mw = Median(g.Where(r => r.Work > 0).Select(r => r.Work));
    foreach (var r in g.OrderByDescending(r => r.MarketValue))
    {
        float rv = mv > 0 ? r.MarketValue / mv : 0;
        float rw = mw > 0 && r.Work > 0 ? r.Work / mw : 0;
        if (rv is > 3f or (> 0 and < 0.34f) || rw is > 3f or (> 0 and < 0.34f))
        {
            sb.AppendLine($"| {r.Label} (`{r.DefName}`) | {group.Key.Role} | {group.Key.Tier} | {F(r.MarketValue)} | {F(mv)} | {rv.ToString("0.00", CultureInfo.InvariantCulture)} " +
                          $"| {(r.Work > 0 ? F(r.Work) : "—")} | {F(mw)} | {(rw > 0 ? rw.ToString("0.00", CultureInfo.InvariantCulture) : "—")} |");
            outlierCount++;
        }
    }
}
sb.AppendLine();
sb.AppendLine($"{outlierCount} flagged by test A.");
sb.AppendLine();

var ratioSet = main.Where(r => r.Craftable && r.Category != "Building" && r.Work > 0 && r.MarketValue > 0).ToList();
float medianRatio = Median(ratioSet.Select(r => r.Work / r.MarketValue));
sb.AppendLine("**B — work per unit of value.** Tier-free: whether an item's build time is proportionate to what");
sb.AppendLine($"it is worth is the same question at every tier. Median across all {ratioSet.Count} craftable audited items is");
sb.AppendLine($"**{F(medianRatio)} work per silver**. Flagged beyond 3× or under a third of that.");
sb.AppendLine();
sb.AppendLine("| Item | Role | Tier | Value | Work | Work per silver | ×median |");
sb.AppendLine("| --- | --- | --- | ---: | ---: | ---: | ---: |");
int ratioOutliers = 0;
foreach (var r in ratioSet.OrderByDescending(r => r.Work / r.MarketValue))
{
    float ratio = r.Work / r.MarketValue;
    float rel = ratio / medianRatio;
    if (rel is <= 3f and >= 0.34f) continue;
    sb.AppendLine($"| {r.Label} (`{r.DefName}`) | {r.Role} | {r.Tier} | {F(r.MarketValue)} | {F(r.Work)} | {ratio.ToString("0.0", CultureInfo.InvariantCulture)} | {rel.ToString("0.00", CultureInfo.InvariantCulture)} |");
    ratioOutliers++;
}
sb.AppendLine();
sb.AppendLine($"{ratioOutliers} flagged by test B.");
sb.AppendLine();

sb.AppendLine("**C — families sharing one identical work amount.** Five or more items from one mod built in exactly");
sb.AppendLine("the same time regardless of what they are worth. This is the structural form of test B: not one item");
sb.AppendLine("mis-costed, but a whole family whose work was never varied.");
sb.AppendLine();
sb.AppendLine("| Mod | Work | Items | Value range | Work per silver, best → worst |");
sb.AppendLine("| --- | ---: | ---: | --- | --- |");
foreach (var g in main.Where(r => r.Work > 0 && r.Category != "Building").GroupBy(r => (r.Mod, r.Work))
             .Where(g => g.Count() >= 5).OrderByDescending(g => g.Count()))
{
    var lo = g.Min(r => r.MarketValue);
    var hi = g.Max(r => r.MarketValue);
    sb.AppendLine($"| {g.Key.Mod} | {F(g.Key.Work)} | {g.Count()} | {F(lo)} – {F(hi)} " +
                  $"| {(hi > 0 ? F(g.Key.Work / hi) : "—")} → {(lo > 0 ? F(g.Key.Work / lo) : "—")} |");
}
sb.AppendLine();

// ---- findings: uncraftable --------------------------------------------------------------------

var uncraftable = main.Concat(rootOnlyRows).Where(r => !r.Craftable)
    .OrderByDescending(r => r.MarketValue).ToList();
sb.AppendLine($"## Items with no recipe at all — {uncraftable.Count}");
sb.AppendLine();
sb.AppendLine("No `costList`, no `recipeMaker`, no producing recipe. Reward, quest, harvest or trade-only, so they");
sb.AppendLine("**cannot be re-costed by ingredients** — market value is the only lever Phase 6 has on them.");
sb.AppendLine($"Only {main.Count(r => !r.Craftable)} of these is in the detailed tables above; the rest reach the audit through set 3, which is");
sb.AppendLine("exactly why the set-3 group cannot simply be re-costed alongside the rest.");
sb.AppendLine();
sb.AppendLine("| Item | defName | Mod | Role | Value | Set | Gated by | Traders selling |");
sb.AppendLine("| --- | --- | --- | --- | ---: | --- | --- | ---: |");
foreach (var r in uncraftable)
    sb.AppendLine($"| {r.Label} | `{r.DefName}` | {r.Mod} | {r.Role} | {F(r.MarketValue)} | {(rows.ContainsKey(r.DefName) ? "1/2" : "3")} | {GateCell(r)} | {(r.Traders > 0 ? r.Traders.ToString() : "—")} |");
sb.AppendLine();

// ---- findings: suspicious ---------------------------------------------------------------------

sb.AppendLine("## Things that look like mistakes of ours");
sb.AppendLine();
sb.AppendLine("Reported, not fixed.");
sb.AppendLine();

var suspicious = new List<(string Kind, string Text)>();
void Flag(string kind, string text) => suspicious.Add((kind, text));

// Roles where "nothing installs this" is a real defect rather than the normal state of a bench.
var implantRoles = new HashSet<string>(StringComparer.Ordinal)
{ "Limbs","Organs","Senses","Torso frames","Cyberbrains","Neural add-ons","Modules","Psychic implants","Weapon modules" };

foreach (var r in main.Where(r => r.Gates.Count > 1))
{
    var onTab = r.Gates.Where(tabProjects.ContainsKey).ToList();
    if (onTab.Count > 1)
        Flag("Two gates on this tab", $"`{r.DefName}` ({r.Label}) — {string.Join(", ", onTab.Select(x => $"`{x}`"))}. Conjunctive: the player must finish both to craft it.");
}
foreach (var r in main.Where(r => r.Gates.Any(g => !tabProjects.ContainsKey(g) && projects.ContainsKey(g))))
{
    var off = r.Gates.Where(g => !tabProjects.ContainsKey(g) && projects.ContainsKey(g)).ToList();
    if (r.Gates.Any(tabProjects.ContainsKey))
        Flag("Gated on the tab and off it", $"`{r.DefName}` ({r.Label}) — off-tab {string.Join(", ", off.Select(x => $"`{x}` ({Str(projects[x], "tab") ?? "main"} tab)"))}.");
}
foreach (string g in main.SelectMany(r => r.Gates).Distinct().Where(g => !projects.ContainsKey(g)))
    Flag("Gate is not a known project", $"`{g}` is referenced by an audited item but is not a project in `ResearchDump.json`.");
foreach (var r in main.Where(r => r.Reasons.Contains("RBP_ authored") && r.Gates.Count == 0))
    Flag("Ours and ungated", $"`{r.DefName}` ({r.Label}) is authored by this project and **carries no research gate at all**.");
foreach (var r in main.Where(r => r.Craftable && r.MarketValue <= 0))
    Flag("Craftable, zero value", $"`{r.DefName}` ({r.Label}) is craftable but resolves to a market value of 0.");
foreach (var r in main.Where(r => r.Craftable && r.Work <= 0 && r.Category != "Building"))
    Flag("Craftable, zero work", $"`{r.DefName}` ({r.Label}) is craftable but resolves to work 0.");
foreach (var r in main.Where(r => implantRoles.Contains(r.Role) && r.Surgeries.Count == 0 && r.Hediffs.Count == 0))
    Flag("Implant that installs nothing", $"`{r.DefName}` ({r.Label}) is classed *{r.Role}* but no surgery installs it and it grants no hediff.");
foreach (var r in main.Where(r => r.Trunk.Contains("(unmapped)")))
    Flag("Tab node no overhaul file declares", $"`{r.DefName}` ({r.Label}) — {string.Join(", ", r.Gates.Where(tabProjects.ContainsKey).Select(x => $"`{x}`"))}.");
foreach (var r in main.Where(r => r.Craftable && r.Category == "Item" && !r.IsApparel
                                  && r.Surgeries.Count == 0 && r.Hediffs.Count == 0
                                  && !usedAsIngredient.Contains(r.DefName)
                                  && r.Role != "Consumables"))
    Flag("Orphaned bill", $"`{r.DefName}` ({r.Label}) is craftable and gated on {GateCell(r)}, but **no surgery installs it and nothing consumes it**.");

var nodesGatingSurgeries = new HashSet<string>(
    recipeById.Values.Where(r => Bool(r, "isSurgery")).SelectMany(ResearchOf), StringComparer.Ordinal);
foreach (var p in tabProjects)
{
    if (main.Any(r => r.Gates.Contains(p.Key))) continue;
    if (nodesGatingSurgeries.Contains(p.Key)) continue;
    var dependents = projects.Values.Where(q => Strings(q, "prerequisites").Contains(p.Key))
        .Select(q => Str(q, "defName")).ToList();
    string tail = dependents.Count > 0
        ? $"It is a pure prerequisite: {dependents.Count} project(s) require it ({string.Join(", ", dependents.Select(x => $"`{x}`"))}), so the player pays {F(Num(p.Value, "baseCost"))} for a node that shows no unlocks of its own."
        : "Nothing lists it as a prerequisite either — it is unreachable content.";
    Flag("Tab node that unlocks nothing", $"`{p.Key}` ({Str(p.Value, "label")}, {F(Num(p.Value, "baseCost"))} research) gates **no craftable item and no surgery**. {tail}");
}

foreach (var r in main.Where(r => r.Mod == "?"))
    Flag("No source mod", $"`{r.DefName}` ({r.Label}) reports no `modContentPack` — a patch-added def, so it shows no source icon in-game and cannot be attributed.");

var animalOnTab = main.Where(r => r.Mod.Contains("Dog Said", StringComparison.OrdinalIgnoreCase)
                                  && r.Gates.Any(tabProjects.ContainsKey)).ToList();
if (animalOnTab.Count > 0)
    Flag("Documented intent vs resolved state", $"**{animalOnTab.Count} A Dog Said 2 animal items** resolve their craft gate to a cybernetics node " +
                   $"({string.Join(", ", animalOnTab.SelectMany(r => r.Gates.Where(tabProjects.ContainsKey)).Distinct().Select(x => $"`{x}`"))}). " +
                   "`CyberneticsResearchBody.xml` states ADS2's own abstract bases are *deliberately not* repointed so the animal " +
                   "items keep working — but they inherit from EPOE's repointed `EPIASurrogateBase` / `EPIASyntheticBase`, so the " +
                   $"repoint reaches them anyway: {string.Join(", ", animalOnTab.Select(r => $"`{r.DefName}`"))}.");

foreach (var gate in main.Where(r => r.Gates.Any(tabProjects.ContainsKey))
             .SelectMany(r => r.Gates.Where(tabProjects.ContainsKey).Select(g => (g, r)))
             .GroupBy(x => x.g))
{
    var roles = gate.Select(x => x.r.Role).Distinct().ToList();
    if (roles.Count >= 4)
        Flag("One node, many unrelated roles", $"`{gate.Key}` gates **{gate.Count()} items spanning {roles.Count} roles** " +
             $"({string.Join(", ", roles.OrderBy(x => x, StringComparer.Ordinal))}) — usually one abstract base carrying mixed content, " +
             "so the node's name describes only part of what it unlocks.");
}

foreach (var kind in suspicious.Distinct().GroupBy(s => s.Kind).OrderByDescending(g => g.Count()))
{
    sb.AppendLine($"### {kind.Key} — {kind.Count()}");
    sb.AppendLine();
    foreach (string s in kind.Select(x => x.Text).Distinct().OrderBy(x => x, StringComparer.Ordinal))
        sb.AppendLine($"- {s}");
    sb.AppendLine();
}
if (suspicious.Count == 0) sb.AppendLine("Nothing flagged.");
sb.AppendLine();

// ---- appendix: tab nodes ----------------------------------------------------------------------

sb.AppendLine("## Appendix — nodes on the tab");
sb.AppendLine();
sb.AppendLine("| Node | Label | Trunk | Tech | Cost | Items it gates |");
sb.AppendLine("| --- | --- | --- | --- | ---: | ---: |");
foreach (var p in tabProjects.OrderBy(p => trunkOfNode.GetValueOrDefault(p.Key, "zz"), StringComparer.Ordinal)
             .ThenBy(p => Num(p.Value, "baseCost")))
{
    int gated = main.Count(r => r.Gates.Contains(p.Key));
    sb.AppendLine($"| `{p.Key}` | {Str(p.Value, "label")} | {trunkOfNode.GetValueOrDefault(p.Key, "(unmapped)")} | {Str(p.Value, "techLevel") ?? "—"} | {F(Num(p.Value, "baseCost"))} | {gated} |");
}
sb.AppendLine();

File.WriteAllText(Path.Combine(outDir, "phase6-item-audit.md"), sb.ToString());
Console.WriteLine($"wrote {Path.Combine(outDir, "phase6-item-audit.md")}");
Console.WriteLine("--- role totals ---");
foreach (string role in roleOrder)
{
    var g = main.Where(r => r.Role == role).ToList();
    if (g.Count > 0) Console.WriteLine($"{role,-20} {g.Count,4}  median {F(Median(g.Select(r => r.MarketValue))),8}");
}
Console.WriteLine($"outliers {outlierCount}, uncraftable {uncraftable.Count}, ingredients {ingUse.Count}, suspicious {suspicious.Distinct().Count()}");

string GateCell(Row r)
{
    if (r.Gates.Count == 0) return "*(ungated)*";
    return string.Join(", ", r.Gates.Select(g => tabProjects.ContainsKey(g) ? $"`{g}`" : $"`{g}`*"));
}

string CostCell(Row r)
{
    if (r.Cost.Count == 0) return r.Craftable ? "*(no cost list)*" : "**no recipe**";
    return string.Join(", ", r.Cost.Select(c => $"{c.Count}× {Label(c.Def)}"));
}

string Label(string def) =>
    thingById.TryGetValue(def, out var td) ? Str(td, "label") ?? def : def;

List<string> CraftGates(JsonElement t)
{
    var result = new List<string>();
    result.AddRange(Strings(t, "researchPrerequisites"));
    if (t.TryGetProperty("recipeMaker", out var rm) && rm.ValueKind == JsonValueKind.Object)
    {
        string one = Str(rm, "researchPrerequisite");
        if (one != null) result.Add(one);
        result.AddRange(Strings(rm, "researchPrerequisites"));
    }
    foreach (var m in producedBy.GetValueOrDefault(Str(t, "defName")) ?? new List<JsonElement>())
        result.AddRange(ResearchOf(m));
    return result.Distinct().ToList();
}

static List<string> ResearchOf(JsonElement recipe)
{
    var result = new List<string>();
    string one = Str(recipe, "researchPrerequisite");
    if (one != null) result.Add(one);
    result.AddRange(Strings(recipe, "researchPrerequisites"));
    return result.Distinct().ToList();
}

static List<(int Count, List<string> Allowed)> Ingredients(JsonElement recipe)
{
    var result = new List<(int, List<string>)>();
    if (!recipe.TryGetProperty("resolvedIngredients", out var ings) || ings.ValueKind != JsonValueKind.Array)
        return result;
    foreach (var ing in ings.EnumerateArray())
    {
        var allowed = Strings(ing, "allowed");
        if (allowed.Count > 0) result.Add(((int)Num(ing, "count"), allowed));
    }
    return result;
}

string DescribeHediff(string defName)
{
    var h = hediffById[defName];
    var parts = new List<string>();

    if (h.TryGetProperty("addedPartProps", out var app) && app.ValueKind == JsonValueKind.Object)
    {
        float eff = Num(app, "partEfficiency");
        if (eff > 0) parts.Add($"**partEff {eff.ToString("0.##", CultureInfo.InvariantCulture)}**");
        if (Bool(app, "solid")) parts.Add("solid");
        if (Bool(app, "betterThanNatural")) parts.Add("betterThanNatural");
    }

    JsonElement stage = default;
    int stageCount = 0;
    if (h.TryGetProperty("stages", out var stages) && stages.ValueKind == JsonValueKind.Array && stages.GetArrayLength() > 0)
    {
        stageCount = stages.GetArrayLength();
        stage = stages[stageCount - 1];
    }
    if (stageCount > 1) parts.Add($"*{stageCount} stages, last shown*");

    if (stage.ValueKind == JsonValueKind.Object)
    {
        var so = Mods(stage, "statOffsets", "stat", "value", "+");
        if (so.Count > 0) parts.Add("offsets: " + string.Join(", ", so));
        var sf = Mods(stage, "statFactors", "stat", "value", "×");
        if (sf.Count > 0) parts.Add("factors: " + string.Join(", ", sf));
        var cm = Mods(stage, "capMods", "capacity", "offset", "+");
        if (cm.Count > 0) parts.Add("caps: " + string.Join(", ", cm));
        foreach (string f in new[] { "painOffset", "painFactor", "hungerRateFactor", "hungerRateFactorOffset", "restFallFactor", "socialFightChanceFactor", "vomitMtbDays", "forgetMemoryThoughtMtbDays", "lifeThreatening" })
        {
            float v = Num(stage, f);
            if (Math.Abs(v) > 0.0001f) parts.Add($"{f} {v.ToString("0.##", CultureInfo.InvariantCulture)}");
        }
        if (stage.TryGetProperty("makeImmuneTo", out var mi) && mi.ValueKind == JsonValueKind.Array)
            parts.Add($"immune to {mi.GetArrayLength()}");
    }

    // Top-level stat blocks: some mods put them on the def rather than a stage.
    var tso = Mods(h, "statOffsets", "stat", "value", "+");
    if (tso.Count > 0) parts.Add("def offsets: " + string.Join(", ", tso));

    if (h.TryGetProperty("comps", out var comps) && comps.ValueKind == JsonValueKind.Array)
    {
        var cs = new List<string>();
        foreach (var c in comps.EnumerateArray())
        {
            string ty = ShortType(Str(c, "$type") ?? "");
            var extra = new List<string>();
            foreach (string f in new[] { "abilityDef", "hediff", "slotID", "verbs", "tools" })
                if (c.TryGetProperty(f, out var v))
                    extra.Add(v.ValueKind == JsonValueKind.Array ? $"{f}×{v.GetArrayLength()}" : $"{f}={v}");
            var abilities = Strings(c, "abilities");
            if (abilities.Count > 0) extra.Add("abilities: " + string.Join("/", abilities));
            var slotIds = Strings(c, "slotIDs");
            if (slotIds.Count > 0) extra.Add("slots: " + string.Join("/", slotIds));
            if (c.TryGetProperty("slots", out var sl) && sl.ValueKind == JsonValueKind.Array)
                extra.Add(string.Join("/", sl.EnumerateArray().Select(x => $"{Str(x, "slotID")}(cap {Num(x, "capacity"):0})")));
            cs.Add(extra.Count > 0 ? $"{ty} [{string.Join("; ", extra)}]" : ty);
        }
        if (cs.Count > 0) parts.Add("comps: " + string.Join(", ", cs));
    }

    var tags = new List<string>();
    if (h.TryGetProperty("isBad", out var ib) && ib.ValueKind == JsonValueKind.True) tags.Add("isBad");
    if (tags.Count > 0) parts.Add(string.Join(", ", tags));

    string body = parts.Count > 0 ? string.Join("; ", parts) : "*(no measurable effect in dump)*";
    return $"`{defName}` — {body}";
}

static List<string> Mods(JsonElement e, string prop, string keyField, string valField, string sign)
{
    var result = new List<string>();
    if (!e.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array) return result;
    foreach (var m in arr.EnumerateArray())
    {
        string k = Str(m, keyField);
        if (k == null) continue;
        float v = Num(m, valField);
        // setMax on a capMod is a ceiling, not an offset — never silently drop it.
        float setMax = Num(m, "setMax");
        string s = sign == "×" ? $"{k} ×{v.ToString("0.###", CultureInfo.InvariantCulture)}"
            : $"{k} {(v >= 0 ? "+" : "")}{v.ToString("0.###", CultureInfo.InvariantCulture)}";
        if (Math.Abs(setMax) > 0.0001f) s += $" (setMax {setMax.ToString("0.##", CultureInfo.InvariantCulture)})";
        if (Math.Abs(v) < 0.0001f && Math.Abs(setMax) < 0.0001f) continue;
        result.Add(s);
    }
    return result;
}

static string ShortType(string full)
{
    int dot = full.LastIndexOf('.');
    string s = dot >= 0 ? full.Substring(dot + 1) : full;
    foreach (string prefix in new[] { "HediffCompProperties_", "CompProperties_", "HediffCompProperties", "CompProperties" })
        if (s.StartsWith(prefix, StringComparison.Ordinal)) return s.Substring(prefix.Length);
    return s;
}

static string ShortMod(string mod)
{
    if (mod == null) return "?";
    int open = mod.LastIndexOf('[');
    return open >= 0 ? mod.Substring(0, open).Trim() : mod;
}

static float Median(IEnumerable<float> values)
{
    var list = values.OrderBy(v => v).ToList();
    if (list.Count == 0) return 0;
    return list.Count % 2 == 1 ? list[list.Count / 2] : (list[list.Count / 2 - 1] + list[list.Count / 2]) / 2f;
}

static string F(float v) => v.ToString(Math.Abs(v) >= 100 ? "0" : "0.##", CultureInfo.InvariantCulture);

static string Str(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
        ? v.GetString() : null;

static bool Bool(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

/// <summary>Numeric field with an explicit fallback for fields the dump omits at their default.</summary>
static float NumOr(JsonElement e, string name, float fallback) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
        ? (float)v.GetDouble() : fallback;

static float Num(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
        ? (float)v.GetDouble() : 0f;

static List<string> Strings(JsonElement e, string name)
{
    var result = new List<string>();
    if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Array)
        return result;
    foreach (var item in v.EnumerateArray())
        if (item.ValueKind == JsonValueKind.String) result.Add(item.GetString());
    return result;
}

sealed class Row
{
    public string DefName, Label, Mod, TechLevel, Category = "Item", Trunk = "—", Role = "?", CostSource, WorkSource, Effect = "";
    public JsonElement Element;
    public float MarketValue, WorkToMake, Work, Mass, PartEfficiency;
    public int Traders;
    public string Tier = "add-on";
    public bool Craftable, HasRecipeMaker, IsModule, SelfInstall, IsCapstone, IsApparel, IsImplantLike;
    public List<(string Def, int Count)> Cost = new();
    public List<string> Gates = new(), Hediffs = new(), Slots = new(), Benches = new(),
        ProducingRecipes = new(), CompTypes = new(), Unresolved = new();
    public HashSet<string> Reasons = new(StringComparer.Ordinal);
    public HashSet<string> Parts = new(StringComparer.OrdinalIgnoreCase);
    public List<SurgeryRef> Surgeries = new();
}

sealed class SurgeryRef
{
    public string DefName, Label, Adds, Removes;
    public List<string> Parts = new(), Research = new();
    public float Work;
    public bool IsInstall;
}
