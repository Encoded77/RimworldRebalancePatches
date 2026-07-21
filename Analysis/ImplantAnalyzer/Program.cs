// Offline analyzer for the implant ecosystem dumps (throwaway, gitignored output).
//
// Joins RecipeDump + HediffDump + ThingDump + ResearchDump + BodyDump + AcquisitionDump +
// ModRulesDump into the audit Big Project 6 needs. Every cross-def judgement lives here rather than
// in the dumps, so changing a rule costs a re-run instead of a RimWorld restart.
//
// Emits:
//   implants.md       — master inventory: what installs where, from which mod, gated by what
//   implant-hosts.md  — the "no wetware mount" audit + the modular slot capacity table
//   implant-android.md— which surgeries an android accepts, and what the blocklist never mentions
//   implant-research.md — research bloat: unlock counts, merge candidates, techprint exposure
//   implant-gaps.md   — reverse mismatches and tier coverage holes
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string tmp = Path.Combine(home, "AppData", "LocalLow", "Ludeon Studios",
    "RimWorld by Ludeon Studios", "RebalancePatches", "tmp");

string dumpDir = args.Length > 0 ? args[0] : tmp;
string outDir = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "out");
outDir = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDir);

// The mods this project is actually about. Everything else is context, not subject.
var EcosystemMods = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["vat.epoeforked"] = "EPOE-Forked",
    ["vat.epoeforkedroyalty"] = "EPOE-Royalty",
    ["lts.i"] = "Integrated Implants",
    ["moistestwhale.gitscyberbrains"] = "GiTS",
    ["hlx.ultratechalteredcarbon"] = "Altered Carbon 2",
    ["cedaro.psychicimplant"] = "Psychic Implants",
    ["vanillaracesexpanded.android"] = "VRE Android",
    ["sambucher.adogsaidanimalprosthetics2"] = "A Dog Said 2",
    ["derp88.vreandroidconversion"] = "Digital Conversion",
    ["asunib.epoeiicompat"] = "EPOE-II compat",
};

Console.WriteLine($"Reading dumps from {dumpDir}");
var recipes = Load(dumpDir, "RecipeDump.json");
var hediffs = Load(dumpDir, "HediffDump.json");
var things = Load(dumpDir, "ThingDump.json");
var research = Load(dumpDir, "ResearchDump.json");
var bodies = Load(dumpDir, "BodyDump.json");
var acquisition = Load(dumpDir, "AcquisitionDump.json");
var modRules = Load(dumpDir, "ModRulesDump.json");

// ---- models ---------------------------------------------------------------------------------

var hediffById = new Dictionary<string, Hediff>(StringComparer.Ordinal);
// Raw elements kept alongside the model: the power analysis needs stage-level stat and capacity
// data that the flattened model deliberately does not carry.
var hediffRaw = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (var h in hediffs.RootElement.GetProperty("hediffs").EnumerateArray())
{
    string name = Str(h, "defName");
    if (name == null) continue;
    hediffRaw[name] = h;
    hediffById[name] = new Hediff
    {
        DefName = name,
        Label = Str(h, "label") ?? name,
        Mod = ShortMod(Str(h, "mod")),
        IsAddedPart = Bool(h, "isAddedPart"),
        AndroidCanCatch = !h.TryGetProperty("androidCanCatch", out var ac) || ac.GetBoolean(),
        // HediffDef.isBad defaults to TRUE, and the dump records only non-default values — so the
        // property appears only when a mod explicitly set it false. Absence means "is an ailment".
        IsBad = !h.TryGetProperty("isBad", out var isBadProp) || isBadProp.ValueKind == JsonValueKind.True,
        Slots = ReadSlots(h),
    };
}

var thingById = new Dictionary<string, Thing>(StringComparer.Ordinal);
// Things carrying CompProperties_Usable can be self-installed with no surgeon. That is either a
// deliberate solo-play affordance (mechlink pattern) or an accidental bypass of the surgery gate.
var selfInstallable = new HashSet<string>(StringComparer.Ordinal);
foreach (var t in things.RootElement.GetProperty("things").EnumerateArray())
{
    string name = Str(t, "defName");
    if (name == null) continue;
    if (t.TryGetProperty("comps", out var comps) && comps.ValueKind == JsonValueKind.Array
        && comps.GetRawText().Contains("CompProperties_Usable", StringComparison.Ordinal))
        selfInstallable.Add(name);
    var stats = t.TryGetProperty("resolvedStats", out var s) ? s : default;
    thingById[name] = new Thing
    {
        DefName = name,
        Label = Str(t, "label") ?? name,
        Mod = ShortMod(Str(t, "mod")),
        TechLevel = Str(t, "techLevel") ?? "Undefined",
        IsImplantLike = Bool(t, "isImplantLike"),
        MarketValue = stats.ValueKind == JsonValueKind.Object ? Num(stats, "MarketValue") : 0,
        WorkToMake = stats.ValueKind == JsonValueKind.Object ? Num(stats, "WorkToMake") : 0,
        ProducedBy = Strings(t, "producedBy"),
    };
}

var projectById = new Dictionary<string, Project>(StringComparer.Ordinal);
foreach (var p in research.RootElement.GetProperty("projects").EnumerateArray())
{
    string name = Str(p, "defName");
    if (name == null) continue;
    projectById[name] = new Project
    {
        DefName = name,
        Label = Str(p, "label") ?? name,
        Mod = ShortMod(Str(p, "mod")),
        Tab = Str(p, "tab") ?? "(main)",
        TechLevel = Str(p, "techLevel") ?? "Undefined",
        BaseCost = Num(p, "baseCost"),
        Prerequisites = Strings(p, "prerequisites"),
        HasTechprint = Bool(p, "hasTechprint"),
        TechprintDef = Str(p, "techprintDef"),
        TechprintMissing = Bool(p, "techprintMissing"),
    };
}

// Body part presence, for the reverse-mismatch check.
var partsByBody = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
foreach (var b in bodies.RootElement.GetProperty("bodies").EnumerateArray())
{
    string name = Str(b, "defName");
    if (name == null) continue;
    partsByBody[name] = new HashSet<string>(Strings(b, "partNames"), StringComparer.Ordinal);
}

// Which bodies belong to humanlike races. An animal lacking a Shoulder is correct; a humanlike
// race that cannot receive a humanlike implant is the bug this audit is looking for.
var humanlikeBodyNames = new HashSet<string>(StringComparer.Ordinal);
var racesByBody = new Dictionary<string, List<string>>(StringComparer.Ordinal);
if (bodies.RootElement.TryGetProperty("pawns", out var pawns))
    foreach (var p in pawns.EnumerateArray())
    {
        string body = Str(p, "body");
        if (body == null) continue;
        if (!racesByBody.TryGetValue(body, out var list)) racesByBody[body] = list = new List<string>();
        list.Add(Str(p, "label") ?? Str(p, "defName"));
        if (Bool(p, "humanlike")) humanlikeBodyNames.Add(body);
    }

// Android rules, as the game currently applies them.
var disallowedRecipes = new HashSet<string>(StringComparer.Ordinal);
bool androidModPresent = false;
if (modRules.RootElement.TryGetProperty("ruleDefs", out var rules)
    && rules.TryGetProperty("VREAndroids.AndroidSettings", out var androidRules))
{
    androidModPresent = Bool(androidRules, "defFound");
    foreach (string r in Strings(androidRules, "disallowedRecipes")) disallowedRecipes.Add(r);
}

// Trader coverage per item.
var tradersFor = new Dictionary<string, List<string>>(StringComparer.Ordinal);
if (acquisition.RootElement.TryGetProperty("traders", out var traders))
    foreach (var entry in traders.EnumerateObject())
        tradersFor[entry.Name] = entry.Value.EnumerateArray()
            .Select(e => Str(e, "trader")).Where(s => s != null).Distinct().ToList();

// ---- surgeries ------------------------------------------------------------------------------

var surgeries = new List<Surgery>();
foreach (var r in recipes.RootElement.GetProperty("recipes").EnumerateArray())
{
    if (!Bool(r, "isSurgery")) continue;
    string name = Str(r, "defName");
    if (name == null) continue;

    var chain = Strings(r, "workerBaseChain").ToHashSet(StringComparer.Ordinal);
    string addsHediff = Str(r, "addsHediff");
    string removesHediff = Str(r, "removesHediff");
    hediffById.TryGetValue(addsHediff ?? "", out Hediff added);

    var surgery = new Surgery
    {
        DefName = name,
        Label = Str(r, "label") ?? name,
        Mod = ShortMod(Str(r, "mod")),
        WorkerChain = chain,
        AddsHediff = addsHediff,
        RemovesHediff = removesHediff,
        TargetParts = Strings(r, "appliedOnFixedBodyParts"),
        Research = ResolveResearch(r),
        Ingredients = ReadIngredients(r),
        WorkAmount = Num(r, "workAmount"),
    };

    // Attach class — moved out of the dump so the rule is reviewable here.
    //   replacesPart : swaps a body part, so the host is artificial by definition
    //   addOn        : adds a hediff and removes nothing — the host part stays flesh
    //   upgrade      : swaps one hediff for another (modularisation, tier changes)
    //   removes      : takes something out
    bool replaces = added?.IsAddedPart == true
                    || chain.Contains("Recipe_InstallArtificialBodyPart")
                    || chain.Contains("Recipe_InstallNaturalBodyPart");
    surgery.Attach =
        removesHediff != null && addsHediff != null ? "upgrade"
        : replaces ? "replacesPart"
        : addsHediff != null ? "addOn"
        : chain.Contains("Recipe_RemoveBodyPart") || chain.Contains("Recipe_RemoveImplant") ? "removes"
        : "other";

    // The wetware rule: an add-on mounts on whatever part is named, flesh included.
    surgery.FleshHostPossible = surgery.Attach == "addOn";

    // Android verdict, mirroring VREAndroids.RecipeWorker_AvailableOnNow_Patch.
    surgery.AndroidBlockReason = AndroidBlock(surgery, added);

    surgeries.Add(surgery);
}

var ecosystem = surgeries.Where(s => EcosystemMods.ContainsKey(s.Mod)).ToList();
Console.WriteLine($"{surgeries.Count} surgeries, {ecosystem.Count} from ecosystem mods");

// ---- implants.md ----------------------------------------------------------------------------

var sb = new StringBuilder();
Head(sb, "Implant inventory",
    "Every surgery from the prosthetics/implant ecosystem, with what it installs, what it costs and what gates it.");

foreach (var group in ecosystem.GroupBy(s => s.Mod).OrderByDescending(g => g.Count()))
{
    sb.AppendLine($"## {EcosystemMods[group.Key]} — {group.Count()} surgeries");
    sb.AppendLine();
    sb.AppendLine("| Surgery | Attach | Item | Value | Research | Android |");
    sb.AppendLine("| --- | --- | --- | ---: | --- | --- |");
    foreach (var s in group.OrderBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
    {
        Thing item = s.Ingredients.Select(i => thingById.GetValueOrDefault(i)).FirstOrDefault(t => t?.IsImplantLike == true)
                     ?? s.Ingredients.Select(i => thingById.GetValueOrDefault(i)).FirstOrDefault(t => t != null);
        sb.AppendLine($"| {s.Label} | {s.Attach} | {item?.Label ?? "—"} | {item?.MarketValue.ToString("0", CultureInfo.InvariantCulture) ?? "—"} " +
                      $"| {(s.Research.Count > 0 ? string.Join(", ", s.Research) : "—")} | {(s.AndroidBlockReason == null ? "yes" : "**no** — " + s.AndroidBlockReason)} |");
    }
    sb.AppendLine();
}
Write(outDir, "implants.md", sb);

// ---- implant-hosts.md -----------------------------------------------------------------------

sb = new StringBuilder();
Head(sb, "Module hosts and slot capacities",
    "Which add-on implants can mount on unmodified flesh, and how many modules each slot accepts.");

var fleshMounts = ecosystem.Where(s => s.FleshHostPossible).ToList();
sb.AppendLine($"## Add-ons that mount on flesh — {fleshMounts.Count}");
sb.AppendLine();
sb.AppendLine("These add a hediff without replacing a body part, so nothing stops them being installed");
sb.AppendLine("on an unaugmented pawn. Each needs sorting into *convert to slot module* or *host-part gate*.");
sb.AppendLine();
sb.AppendLine("| Surgery | Mod | Target parts | Research |");
sb.AppendLine("| --- | --- | --- | --- |");
foreach (var s in fleshMounts.OrderBy(s => s.Mod, StringComparer.Ordinal).ThenBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
    sb.AppendLine($"| {s.Label} | {EcosystemMods[s.Mod]} | {Join(s.TargetParts)} | {Join(s.Research)} |");
sb.AppendLine();

sb.AppendLine("## Modular slot table");
sb.AppendLine();
sb.AppendLine("Capacity `-1` means unlimited: one host part accepts every module that fits the slot.");
sb.AppendLine();
sb.AppendLine("| Slot | Name | Capacities | Uncapped | Hosts |");
sb.AppendLine("| --- | --- | --- | --- | ---: |");
int unreadable = 0;
if (hediffs.RootElement.TryGetProperty("modularSlotTable", out var slotTable))
    foreach (var slot in slotTable.EnumerateObject().OrderBy(s => s.Name, StringComparer.Ordinal))
    {
        // Capacity is null when the framework field could not be read — never conflate that with 0,
        // which would read as "this slot accepts nothing".
        var caps = slot.Value.TryGetProperty("capacities", out var c)
            ? c.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.Number ? x.GetInt32().ToString() : "?").ToList()
            : new List<string>();
        int hosts = slot.Value.TryGetProperty("hosts", out var h) ? h.GetArrayLength() : 0;
        if (Bool(slot.Value, "unreadableCapacity")) unreadable++;
        sb.AppendLine($"| `{slot.Name}` | {Str(slot.Value, "slotName")} | {string.Join(", ", caps)} " +
                      $"| {(Bool(slot.Value, "uncapped") ? "**yes**" : "no")} | {hosts} |");
    }
sb.AppendLine();
if (unreadable > 0)
    sb.AppendLine($"⚠ **{unreadable} slots reported an unreadable capacity (`?`).** The framework's field could " +
                  "not be read reflectively — check `slotFields` in HediffDump.json for its real name before " +
                  "trusting any capacity number here.");
Write(outDir, "implant-hosts.md", sb);

// ---- implant-android.md ---------------------------------------------------------------------

sb = new StringBuilder();
Head(sb, "Android installability",
    androidModPresent
        ? "What an android can currently receive, and which ecosystem content the blocklist never mentions."
        : "VRE - Android was not loaded for this dump; verdicts below are unconstrained.");

var blocked = ecosystem.Where(s => s.AndroidBlockReason != null).ToList();
var allowed = ecosystem.Where(s => s.AndroidBlockReason == null).ToList();
sb.AppendLine($"- **{allowed.Count}** ecosystem surgeries an android currently accepts");
sb.AppendLine($"- **{blocked.Count}** currently refused");
sb.AppendLine();

// The point of the audit: the shipped blocklist names vanilla content only.
var moddedNamed = disallowedRecipes.Where(r => surgeries.Any(s => s.DefName == r && EcosystemMods.ContainsKey(s.Mod))).ToList();
sb.AppendLine($"The blocklist names **{disallowedRecipes.Count}** recipes, of which **{moddedNamed.Count}** come from ecosystem mods.");
sb.AppendLine();

sb.AppendLine("## Accepted today — candidates for the wetware blocklist");
sb.AppendLine();
sb.AppendLine("| Surgery | Mod | Attach | Adds hediff |");
sb.AppendLine("| --- | --- | --- | --- |");
foreach (var s in allowed.OrderBy(s => s.Mod, StringComparer.Ordinal).ThenBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
    sb.AppendLine($"| {s.Label} | {EcosystemMods[s.Mod]} | {s.Attach} | {s.AddsHediff ?? "—"} |");
sb.AppendLine();

sb.AppendLine("## Already refused");
sb.AppendLine();
sb.AppendLine("| Surgery | Mod | Reason |");
sb.AppendLine("| --- | --- | --- |");
foreach (var s in blocked.OrderBy(s => s.Mod, StringComparer.Ordinal).ThenBy(s => s.Label, StringComparer.OrdinalIgnoreCase))
    sb.AppendLine($"| {s.Label} | {EcosystemMods[s.Mod]} | {s.AndroidBlockReason} |");
Write(outDir, "implant-android.md", sb);

// ---- implant-research.md --------------------------------------------------------------------

sb = new StringBuilder();
Head(sb, "Research bloat",
    "Ecosystem research projects by what they actually unlock, plus techprint exposure for the merge work.");

// Unlock counts, computed here rather than dumped, so the definition can change freely.
var unlockCount = new Dictionary<string, int>(StringComparer.Ordinal);
foreach (var r in recipes.RootElement.GetProperty("recipes").EnumerateArray())
    foreach (string p in ResolveResearch(r))
        unlockCount[p] = unlockCount.GetValueOrDefault(p) + 1;
foreach (var t in things.RootElement.GetProperty("things").EnumerateArray())
    foreach (string p in Strings(t, "researchPrerequisites"))
        unlockCount[p] = unlockCount.GetValueOrDefault(p) + 1;

var ecoProjects = projectById.Values.Where(p => EcosystemMods.ContainsKey(p.Mod)).ToList();
sb.AppendLine($"**{ecoProjects.Count} projects** across {ecoProjects.Select(p => p.Mod).Distinct().Count()} mods " +
              $"and {ecoProjects.Select(p => p.Tab).Distinct().Count()} research tabs.");
sb.AppendLine();

foreach (var group in ecoProjects.GroupBy(p => p.Mod).OrderByDescending(g => g.Count()))
{
    sb.AppendLine($"## {EcosystemMods[group.Key]} — {group.Count()} projects, tab(s): {Join(group.Select(p => p.Tab).Distinct())}");
    sb.AppendLine();
    sb.AppendLine("| Project | Tech | Cost | Unlocks | Techprint |");
    sb.AppendLine("| --- | --- | ---: | ---: | --- |");
    foreach (var p in group.OrderBy(p => unlockCount.GetValueOrDefault(p.DefName)).ThenBy(p => p.Label))
    {
        int unlocks = unlockCount.GetValueOrDefault(p.DefName);
        string print = !p.HasTechprint ? "—" : p.TechprintMissing ? "**MISSING**" : p.TechprintDef;
        sb.AppendLine($"| {p.Label} | {p.TechLevel} | {p.BaseCost:0} | {unlocks}{(unlocks <= 1 ? " ⚠" : "")} | {print} |");
    }
    sb.AppendLine();
}
sb.AppendLine("⚠ marks projects unlocking one thing or nothing — the merge candidates.");
sb.AppendLine();

var strandedPrints = projectById.Values.Where(p => p.TechprintMissing).ToList();
if (strandedPrints.Count > 0)
{
    sb.AppendLine("## Projects whose techprint item is missing");
    sb.AppendLine();
    sb.AppendLine("These require techprints that no longer exist, so they cannot be completed.");
    sb.AppendLine();
    foreach (var p in strandedPrints.OrderBy(p => p.DefName, StringComparer.Ordinal))
        sb.AppendLine($"- `{p.DefName}` ({p.Label}) — {p.Mod}");
}
Write(outDir, "implant-research.md", sb);

// ---- implant-gaps.md ------------------------------------------------------------------------

sb = new StringBuilder();
Head(sb, "Coverage gaps and reverse mismatches",
    "Surgeries no loaded body can receive, and implants sold below the tier that gates them.");

sb.AppendLine("## Surgeries targeting parts some bodies lack");
sb.AppendLine();
sb.AppendLine("A body missing every targeted part can never receive the surgery. Humanlike bodies here are");
sb.AppendLine("the real finding; animal bodies are usually correct.");
sb.AppendLine();
sb.AppendLine("| Surgery | Mod | Target parts | Bodies missing all |");
sb.AppendLine("| --- | --- | --- | ---: |");
foreach (var s in ecosystem.Where(s => s.TargetParts.Count > 0))
{
    var missing = partsByBody.Where(b => !s.TargetParts.Any(p => b.Value.Contains(p))).Select(b => b.Key).ToList();
    if (missing.Count == 0) continue;
    sb.AppendLine($"| {s.Label} | {EcosystemMods[s.Mod]} | {Join(s.TargetParts)} | {missing.Count} |");
}
sb.AppendLine();

sb.AppendLine("## Implants a trader can sell");
sb.AppendLine();
sb.AppendLine("Research gating means little when the item is purchasable. Cross-check against the tier");
sb.AppendLine("each one is meant to sit at.");
sb.AppendLine();
sb.AppendLine("| Item | Mod | Value | Traders |");
sb.AppendLine("| --- | --- | ---: | ---: |");
foreach (var t in thingById.Values.Where(t => t.IsImplantLike && EcosystemMods.ContainsKey(t.Mod))
             .OrderByDescending(t => t.MarketValue))
{
    var sellers = tradersFor.GetValueOrDefault(t.DefName);
    if (sellers == null || sellers.Count == 0) continue;
    sb.AppendLine($"| {t.Label} | {EcosystemMods[t.Mod]} | {t.MarketValue:0} | {sellers.Count} |");
}
Write(outDir, "implant-gaps.md", sb);

// ---- implant-audit.md -----------------------------------------------------------------------

sb = new StringBuilder();
Head(sb, "Phase 2 audit",
    "Machine-checkable audit items: implants treated as ailments, implants installable without a surgeon, and surgeries no humanlike body can receive.");

// --- item 5: isBad coverage ---
// A hediff flagged isBad is treated as an ailment, so healer serums, biosculpting and Anomaly's
// regeneration strip it. Implants should not be ailments. The existing implants.chipbad fix is
// this pattern applied to one mod.
var badImplants = new List<Hediff>();
foreach (var s in ecosystem)
{
    if (s.AddsHediff == null) continue;
    if (!hediffById.TryGetValue(s.AddsHediff, out Hediff h)) continue;
    if (h.IsBad && !badImplants.Contains(h)) badImplants.Add(h);
}
sb.AppendLine($"## Implants flagged as ailments — {badImplants.Count}");
sb.AppendLine();
sb.AppendLine("`isBad = true` means healer serums, biosculpting and regeneration will remove these.");
sb.AppendLine("An implant the player paid for should not be treated as a disease.");
sb.AppendLine();
if (badImplants.Count == 0) sb.AppendLine("None found.");
else
{
    sb.AppendLine("| Hediff | Label | Mod | Added part |");
    sb.AppendLine("| --- | --- | --- | --- |");
    foreach (var h in badImplants.OrderBy(h => h.Mod, StringComparer.Ordinal).ThenBy(h => h.DefName, StringComparer.Ordinal))
        sb.AppendLine($"| `{h.DefName}` | {h.Label} | {ModName(h.Mod)} | {(h.IsAddedPart ? "yes" : "no")} |");
}
sb.AppendLine();

// --- item 10: self-installable sweep ---
var selfInstallImplants = thingById.Values
    .Where(t => t.IsImplantLike && selfInstallable.Contains(t.DefName))
    .OrderBy(t => t.Mod, StringComparer.Ordinal).ThenBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
    .ToList();
sb.AppendLine($"## Implants installable without a surgeon — {selfInstallImplants.Count}");
sb.AppendLine();
sb.AppendLine("These carry `CompProperties_Usable`, so a pawn installs them by using the item — no");
sb.AppendLine("doctor, no anaesthesia, no failure chance. Each is either a deliberate solo-play");
sb.AppendLine("affordance (the vanilla mechlink pattern) or an accidental bypass of the surgery gate.");
sb.AppendLine();
sb.AppendLine("| Item | Mod | Value | Also has an install surgery |");
sb.AppendLine("| --- | --- | ---: | --- |");
foreach (var t in selfInstallImplants)
{
    bool hasSurgery = ecosystem.Any(s => s.Ingredients.Contains(t.DefName));
    sb.AppendLine($"| {t.Label} | {ModName(t.Mod)} | {t.MarketValue:0} | {(hasSurgery ? "yes" : "**no**")} |");
}
sb.AppendLine();

// --- item 8: reverse mismatches, humanlike only ---
// Joined through BodyDump's pawns section: only bodies actually used by a humanlike race count.
var humanlikeBodies = partsByBody.Where(b => humanlikeBodyNames.Contains(b.Key))
    .ToDictionary(b => b.Key, b => b.Value, StringComparer.Ordinal);
sb.AppendLine($"## Surgeries some humanlike bodies cannot receive");
sb.AppendLine();
sb.AppendLine($"Checked against **{humanlikeBodies.Count} humanlike bodies** (races flagged humanlike in");
sb.AppendLine("BodyDump). A body missing every targeted part can never receive the surgery.");
sb.AppendLine();

// Group by the affected body set: dozens of surgeries failing on the same body is one gap, not
// dozens, and grouping shows which body is actually the problem.
var mismatches = new Dictionary<string, List<Surgery>>(StringComparer.Ordinal);
// A Dog Said is animal-only by design and verified sealed, so its surgeries failing on every
// humanlike body is the intended behaviour, not a gap.
foreach (var s in ecosystem.Where(s => s.TargetParts.Count > 0
                                       && !string.Equals(s.Mod, "sambucher.adogsaidanimalprosthetics2",
                                           StringComparison.OrdinalIgnoreCase)))
{
    var missing = humanlikeBodies.Where(b => !s.TargetParts.Any(p => b.Value.Contains(p)))
        .Select(b => b.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
    if (missing.Count == 0) continue;
    string key = string.Join(", ", missing);
    if (!mismatches.TryGetValue(key, out var list)) mismatches[key] = list = new List<Surgery>();
    list.Add(s);
}
int mismatchRows = mismatches.Values.Sum(v => v.Count);

sb.AppendLine($"**{mismatchRows} surgery/body gaps**, grouped by which bodies are affected:");
sb.AppendLine();
foreach (var group in mismatches.OrderByDescending(g => g.Value.Count))
{
    var races = group.Key.Split(", ").SelectMany(b => racesByBody.TryGetValue(b, out var r) ? r : new List<string>())
        .Distinct().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    sb.AppendLine($"### {group.Value.Count} surgeries unavailable to: {group.Key}");
    sb.AppendLine();
    if (races.Count > 0)
        sb.AppendLine($"*Races: {string.Join(", ", races.Take(12))}{(races.Count > 12 ? $" (+{races.Count - 12} more)" : "")}*");
    sb.AppendLine();
    foreach (var mod in group.Value.GroupBy(s => s.Mod).OrderByDescending(g => g.Count()))
        sb.AppendLine($"- **{ModName(mod.Key)}** ({mod.Count()}): {string.Join(", ", mod.Take(8).Select(s => s.Label))}" +
                      $"{(mod.Count() > 8 ? $" (+{mod.Count() - 8} more)" : "")}");
    sb.AppendLine();
}
if (mismatchRows == 0) sb.AppendLine("None — every humanlike body can receive every ecosystem surgery.");
Write(outDir, "implant-audit.md", sb);

// ---- implant-android-blocklist.md -----------------------------------------------------------
//
// Audit item 6. The design rule is that androids accept everything except evident wetware — so this
// proposes which of the currently-allowed surgeries should be blocked, and why. Classification is
// heuristic and deliberately visible: every entry states the rule that caught it, so a wrong call is
// argued with rather than discovered in-game.

sb = new StringBuilder();
Head(sb, "Android blocklist proposal",
    "Which ecosystem surgeries an android should be refused, classified by why. The shipped blocklist names zero ecosystem content.");

// Metabolic organs: a synthetic liver in a machine has no referent.
var metabolicParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "Heart", "Lung", "Kidney", "Liver", "Stomach" };

(string Rule, string Why)? Classify(Surgery s)
{
    string text = ((s.Label ?? "") + " " + (s.AddsHediff ?? "")).ToLowerInvariant();

    if (Regex.IsMatch(text, @"nanite|micromachine|immun|toxin|organ decay|mmsi"))
        return ("wetware nanites", "biological nanite colonies have nothing to inhabit");
    if (Regex.IsMatch(text, @"hemogen|deathrest|sanguophage|lacto|blood|marrow|fang"))
        return ("hemogenic / deathrest", "xenotype biology, not machinery");
    if (Regex.IsMatch(text, @"deadlife|dreadheart|metalhorror|ghoul|void|collar|impaler|blacksight|shambler"))
        return ("anomaly / entity", "grown or attached by entities, needs living tissue");
    if (Regex.IsMatch(text, @"womb|fertil|pregnan"))
        return ("reproductive", "androids are manufactured, not gestated");
    if (Regex.IsMatch(text, @"gland|adrenaline|hormon|metabolic|digest"))
        return ("endocrine / metabolic", "regulates a biology an android does not have");
    if (s.Attach == "replacesPart" && s.TargetParts.Any(p => metabolicParts.Contains(p)))
        return ("metabolic organ", "replaces an organ an android has no use for");
    return null;
}

// Two mods are excluded before classification, for opposite reasons:
//   A Dog Said  - animal-only and verified sealed, so it can never reach a humanlike android.
//   VRE Android - its content IS android content. The reactor replaces a Stomach, which the
//                 metabolic-organ rule would otherwise block on the very pawns it exists for.
var excludedFromBlocklist = new[]
{
    "sambucher.adogsaidanimalprosthetics2",
    "vanillaracesexpanded.android",
};

var proposals = new List<(Surgery S, string Rule, string Why)>();
foreach (var s in ecosystem.Where(s => s.AndroidBlockReason == null
                                       && !excludedFromBlocklist.Contains(s.Mod, StringComparer.OrdinalIgnoreCase)))
{
    var c = Classify(s);
    if (c.HasValue) proposals.Add((s, c.Value.Rule, c.Value.Why));
}

sb.AppendLine($"- **{allowed.Count}** ecosystem surgeries an android currently accepts");
sb.AppendLine($"- **{proposals.Count}** proposed for the blocklist");
sb.AppendLine($"- **{allowed.Count - proposals.Count}** remain allowed — the permissive default (D3)");
sb.AppendLine();
sb.AppendLine("Classification is heuristic, matching on labels and hediff names. Review each rule:");
sb.AppendLine("a false positive costs androids an implant they should have, a false negative leaves");
sb.AppendLine("the hole this audit exists to close.");
sb.AppendLine();

foreach (var group in proposals.GroupBy(p => p.Rule).OrderByDescending(g => g.Count()))
{
    sb.AppendLine($"## {group.Key} — {group.Count()}");
    sb.AppendLine();
    sb.AppendLine($"*{group.First().Why}*");
    sb.AppendLine();
    sb.AppendLine("| Surgery | Mod | Recipe defName |");
    sb.AppendLine("| --- | --- | --- |");
    foreach (var p in group.OrderBy(p => p.S.Mod, StringComparer.Ordinal).ThenBy(p => p.S.Label, StringComparer.OrdinalIgnoreCase))
        sb.AppendLine($"| {p.S.Label} | {ModName(p.S.Mod)} | `{p.S.DefName}` |");
    sb.AppendLine();
}

sb.AppendLine("## Ready-to-patch defNames");
sb.AppendLine();
sb.AppendLine("For `VREA_AndroidSettings.disallowedRecipes`:");
sb.AppendLine();
sb.AppendLine("```xml");
foreach (var p in proposals.OrderBy(p => p.S.DefName, StringComparer.Ordinal))
    sb.AppendLine($"<li>{p.S.DefName}</li>");
sb.AppendLine("```");
Write(outDir, "implant-android-blocklist.md", sb);

// ---- implant-power.md -----------------------------------------------------------------------
//
// Audit items 3 and 4. Naively summing every implant that touches a stat overstates wildly, because
// a pawn has one heart and one brain. So contributions are grouped by target body part and only the
// best per part is counted: that is what one pawn can actually reach.

sb = new StringBuilder();
Head(sb, "Power outliers and stacking",
    "What one pawn can actually stack on a single stat, and which implants are strongest for their tier.");

// How many of each part a Human actually has. Left and right arms share one BodyPartDef, so an
// implant targeting "Arm" can be installed twice — counting it once understates every paired part.
var partMultiplicity = new Dictionary<string, int>(StringComparer.Ordinal);
foreach (var b in bodies.RootElement.GetProperty("bodies").EnumerateArray())
{
    if (Str(b, "defName") != "Human") continue;
    if (!b.TryGetProperty("parts", out var plist)) continue;
    foreach (var p in plist.EnumerateArray())
    {
        string pn = Str(p, "part");
        if (pn == null) continue;
        partMultiplicity[pn] = partMultiplicity.GetValueOrDefault(pn) + 1;
    }
}
int Multiplicity(string part) => Math.Max(1, partMultiplicity.GetValueOrDefault(part, 1));

// stat -> part -> best contribution (and who provides it)
var byStat = new Dictionary<string, Dictionary<string, (float Val, string Who, string Mod)>>(StringComparer.Ordinal);
var capByStat = new Dictionary<string, Dictionary<string, (float Val, string Who, string Mod)>>(StringComparer.Ordinal);

void Record(Dictionary<string, Dictionary<string, (float, string, string)>> into,
    string stat, string part, float val, string who, string mod)
{
    if (Math.Abs(val) < 0.0001f) return;
    if (!into.TryGetValue(stat, out var parts)) into[stat] = parts = new(StringComparer.Ordinal);
    if (!parts.TryGetValue(part, out var cur) || val > cur.Item1) parts[part] = (val, who, mod);
}

foreach (var s in ecosystem)
{
    if (s.AddsHediff == null) continue;
    if (!hediffRaw.TryGetValue(s.AddsHediff, out JsonElement hd)) continue;
    string part = s.TargetParts.Count > 0 ? s.TargetParts[0] : "(none)";

    if (!hd.TryGetProperty("stages", out var stages) || stages.ValueKind != JsonValueKind.Array || stages.GetArrayLength() == 0)
        continue;
    var last = stages[stages.GetArrayLength() - 1];

    int mult = Multiplicity(part);
    string partLabel = mult > 1 ? $"{part} ×{mult}" : part;

    if (last.TryGetProperty("statOffsets", out var so) && so.ValueKind == JsonValueKind.Array)
        foreach (var m in so.EnumerateArray())
            Record(byStat, Str(m, "stat") ?? "?", partLabel, Num(m, "value") * mult, s.Label, s.Mod);

    if (last.TryGetProperty("capMods", out var cm) && cm.ValueKind == JsonValueKind.Array)
        foreach (var m in cm.EnumerateArray())
            Record(capByStat, Str(m, "capacity") ?? "?", partLabel, Num(m, "offset") * mult, s.Label, s.Mod);
}

// --- modules: every one that fits a slot, since every slot is currently uncapped ---
// Modules bypass the per-part logic entirely: they install into slots, and with capacity -1 a host
// accepts all of them at once. This is the stacking the capacity system exists to bound, so it is
// summed rather than best-per-part.
int moduleCount = 0;
foreach (var t in things.RootElement.GetProperty("things").EnumerateArray())
{
    if (!t.TryGetProperty("comps", out var comps) || comps.ValueKind != JsonValueKind.Array) continue;
    string slot = null, label = Str(t, "label") ?? Str(t, "defName");
    var granted = new List<string>();
    foreach (var c in comps.EnumerateArray())
    {
        if ((Str(c, "$type") ?? "").IndexOf("UseEffectHediffModule", StringComparison.Ordinal) < 0) continue;
        slot = Strings(c, "slotIDs").FirstOrDefault();
        granted.AddRange(Strings(c, "hediffs"));
    }
    if (slot == null || granted.Count == 0) continue;
    moduleCount++;

    foreach (string g in granted)
    {
        if (!hediffRaw.TryGetValue(g, out JsonElement gh)) continue;
        if (!gh.TryGetProperty("stages", out var gs) || gs.ValueKind != JsonValueKind.Array || gs.GetArrayLength() == 0) continue;
        var gl = gs[gs.GetArrayLength() - 1];
        // Key by module name, not slot, so all modules in a slot accumulate rather than competing.
        if (gl.TryGetProperty("statOffsets", out var gso) && gso.ValueKind == JsonValueKind.Array)
            foreach (var m in gso.EnumerateArray())
                Record(byStat, Str(m, "stat") ?? "?", $"[module] {label}", Num(m, "value"), label, ShortMod(Str(t, "mod")));
        if (gl.TryGetProperty("capMods", out var gcm) && gcm.ValueKind == JsonValueKind.Array)
            foreach (var m in gcm.EnumerateArray())
                Record(capByStat, Str(m, "capacity") ?? "?", $"[module] {label}", Num(m, "offset"), label, ShortMod(Str(t, "mod")));
    }
}

sb.AppendLine("## Capacity stacking — the biggest levers");
sb.AppendLine();
sb.AppendLine("Capacities gate everything a pawn does, so these matter more than any single stat.");
sb.AppendLine("**Reachable** sums the best contribution per body part, scaled by how many of that part a");
sb.AppendLine($"Human has (two arms, two eyes), plus every one of the **{moduleCount} modules** that fits a slot —");
sb.AppendLine("because every slot is currently uncapped, so a host accepts all of them at once.");
sb.AppendLine();
sb.AppendLine("Negative offsets are included, so a total can read below its largest single contribution.");
sb.AppendLine();
sb.AppendLine("| Capacity | Reachable | Sources | Largest single |");
sb.AppendLine("| --- | ---: | ---: | --- |");
foreach (var pair in capByStat.OrderByDescending(p => p.Value.Values.Sum(v => v.Val)))
{
    float total = pair.Value.Values.Sum(v => v.Val);
    var top = pair.Value.Values.OrderByDescending(v => v.Val).First();
    sb.AppendLine($"| {pair.Key} | **+{total:0.##}** | {pair.Value.Count} parts | {top.Who} (+{top.Val:0.##}, {ModName(top.Mod)}) |");
}
sb.AppendLine();

sb.AppendLine("## Stat stacking — top 25 by reachable total");
sb.AppendLine();
sb.AppendLine("| Stat | Reachable | Sources | Largest single |");
sb.AppendLine("| --- | ---: | ---: | --- |");
foreach (var pair in byStat.OrderByDescending(p => p.Value.Values.Sum(v => v.Val)).Take(25))
{
    float total = pair.Value.Values.Sum(v => v.Val);
    var top = pair.Value.Values.OrderByDescending(v => v.Val).First();
    sb.AppendLine($"| {pair.Key} | **+{total:0.##}** | {pair.Value.Count} parts | {top.Who} (+{top.Val:0.##}, {ModName(top.Mod)}) |");
}
sb.AppendLine();

sb.AppendLine("## Where each stackable capacity comes from");
sb.AppendLine();
foreach (var pair in capByStat.OrderByDescending(p => p.Value.Values.Sum(v => v.Val)).Take(4))
{
    sb.AppendLine($"### {pair.Key} — +{pair.Value.Values.Sum(v => v.Val):0.##} across {pair.Value.Count} parts");
    sb.AppendLine();
    sb.AppendLine("| Body part | Best implant | Value | Mod |");
    sb.AppendLine("| --- | --- | ---: | --- |");
    foreach (var p in pair.Value.OrderByDescending(p => p.Value.Val))
        sb.AppendLine($"| {p.Key} | {p.Value.Who} | +{p.Value.Val:0.##} | {ModName(p.Value.Mod)} |");
    sb.AppendLine();
}
Write(outDir, "implant-power.md", sb);

// ---- implant-coverage.md --------------------------------------------------------------------
//
// Audit items 1, 2 and 7 share one analysis: who replaces which body part, at which tier. Two mods
// replacing the same part are duplicates competing for one slot (items 1 and 2); a part covered at
// one tier but not the next is a ladder gap (item 7).

sb = new StringBuilder();
Head(sb, "Part coverage, duplicates and tier gaps",
    "Which mods replace which body part, at which tier. Duplicates compete for one slot; missing tiers are ladder gaps.");

// Tier inferred from the label, since the ladder is expressed in names not data.
string TierOf(string label)
{
    string l = (label ?? "").ToLowerInvariant();
    if (l.Contains("archotech")) return "archotech";
    if (l.Contains("advanced")) return "advanced";
    if (l.Contains("bionic")) return "bionic";
    if (Regex.IsMatch(l, @"prosthet|simple|peg |hook|wooden|denture")) return "prosthetic";
    return "other";
}

string[] tierOrder = { "prosthetic", "bionic", "advanced", "archotech" };

// part -> tier -> list of (label, mod)
var coverage = new Dictionary<string, Dictionary<string, List<(string Label, string Mod)>>>(StringComparer.Ordinal);
foreach (var s in ecosystem.Where(s => s.Attach == "replacesPart"
                                       && !string.Equals(s.Mod, "sambucher.adogsaidanimalprosthetics2", StringComparison.OrdinalIgnoreCase)))
{
    foreach (string part in s.TargetParts.Where(p => !p.StartsWith("BS_", StringComparison.Ordinal)))
    {
        if (!coverage.TryGetValue(part, out var tiers)) coverage[part] = tiers = new(StringComparer.Ordinal);
        string tier = TierOf(s.Label);
        if (!tiers.TryGetValue(tier, out var list)) tiers[tier] = list = new();
        list.Add((s.Label, s.Mod));
    }
}

// --- items 1 and 2: same part, same tier, different mods ---
var collisions = new List<(string Part, string Tier, List<(string Label, string Mod)> Items)>();
foreach (var part in coverage)
    foreach (var tier in part.Value)
        if (tier.Value.Select(i => i.Mod).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            collisions.Add((part.Key, tier.Key, tier.Value));

sb.AppendLine($"## Duplicates — {collisions.Count} part/tier slots contested by more than one mod");
sb.AppendLine();
sb.AppendLine("A pawn has one of each part, so these compete directly. Each needs a canonical winner,");
sb.AppendLine("with the rest retired through Cherry Picker or re-tiered to a different rung.");
sb.AppendLine();
sb.AppendLine("| Body part | Tier | Competing implants |");
sb.AppendLine("| --- | --- | --- |");
foreach (var c in collisions.OrderBy(c => c.Part, StringComparer.Ordinal).ThenBy(c => Array.IndexOf(tierOrder, c.Tier)))
    sb.AppendLine($"| {c.Part} | {c.Tier} | {string.Join(" · ", c.Items.Select(i => $"{i.Label} *({ModName(i.Mod)})*"))} |");
sb.AppendLine();

// --- item 7: tier gaps ---
sb.AppendLine("## Tier coverage — where the ladder has holes");
sb.AppendLine();
sb.AppendLine("A part covered at bionic and archotech but not prosthetic has no early-game entry;");
sb.AppendLine("one covered at prosthetic and archotech skips a rung. Both are Phase 4 candidates.");
sb.AppendLine();
sb.AppendLine("| Body part | prosthetic | bionic | advanced | archotech | Gap |");
sb.AppendLine("| --- | :-: | :-: | :-: | :-: | --- |");
int gapCount = 0;
foreach (var part in coverage.OrderBy(p => p.Key, StringComparer.Ordinal))
{
    var has = tierOrder.Select(t => part.Value.ContainsKey(t)).ToArray();
    if (!has.Any(h => h)) continue;
    int first = Array.IndexOf(has, true), last = Array.LastIndexOf(has, true);
    var missing = new List<string>();
    for (int i = first; i <= last; i++) if (!has[i]) missing.Add(tierOrder[i]);
    if (!has[0] && first > 0) missing.Insert(0, "no entry tier");
    if (missing.Count == 0) continue;
    gapCount++;
    sb.AppendLine($"| {part.Key} | {(has[0] ? "✓" : "·")} | {(has[1] ? "✓" : "·")} | {(has[2] ? "✓" : "·")} | {(has[3] ? "✓" : "·")} | {string.Join(", ", missing)} |");
}
if (gapCount == 0) sb.AppendLine("| — | | | | | none |");
Write(outDir, "implant-coverage.md", sb);

Console.WriteLine($"Wrote reports to {outDir}");
Console.WriteLine($"  audit: {badImplants.Count} ailment-flagged implants, {selfInstallImplants.Count} self-installable, {mismatchRows} body mismatches");
Console.WriteLine($"  coverage: {collisions.Count} contested part/tier slots, {gapCount} parts with ladder gaps");
Console.WriteLine($"  android: {proposals.Count} of {allowed.Count} allowed surgeries proposed for the blocklist");

// ---- helpers --------------------------------------------------------------------------------

string AndroidBlock(Surgery s, Hediff added)
{
    if (!androidModPresent) return null;
    if (s.WorkerChain.Contains("Recipe_AdministerIngestible") && !s.WorkerChain.Contains("Recipe_AdministerNeutroamineForAndroid"))
        return "ingestible";
    if (s.WorkerChain.Contains("Recipe_RemoveBodyPart") && !s.WorkerChain.Contains("Recipe_RemoveArtificialBodyPart"))
        return "removes natural part";
    if (s.WorkerChain.Contains("Recipe_InstallNaturalBodyPart"))
        return "natural part";
    if (added != null && !added.AndroidCanCatch)
        return "hediff flagged androidCanCatchIt=false";
    if (disallowedRecipes.Contains(s.DefName))
        return "on the blocklist";
    return null;
}

List<string> ResolveResearch(JsonElement recipe)
{
    var result = new List<string>();
    string single = Str(recipe, "researchPrerequisite");
    if (single != null) result.Add(single);
    result.AddRange(Strings(recipe, "researchPrerequisites"));
    return result.Distinct().ToList();
}

List<string> ReadIngredients(JsonElement recipe)
{
    var result = new List<string>();
    if (!recipe.TryGetProperty("resolvedIngredients", out var ings)) return result;
    foreach (var ing in ings.EnumerateArray())
        foreach (string allowed in Strings(ing, "allowed"))
            result.Add(allowed);
    return result;
}

static List<Slot> ReadSlots(JsonElement h)
{
    var result = new List<Slot>();
    if (!h.TryGetProperty("modularSlots", out var slots)) return result;
    foreach (var s in slots.EnumerateArray())
        result.Add(new Slot { Id = Str(s, "slotID"), Name = Str(s, "slotName"), Capacity = (int)Num(s, "capacity") });
    return result;
}

static JsonDocument Load(string dir, string file)
{
    string path = Path.Combine(dir, file);
    if (!File.Exists(path))
        throw new FileNotFoundException($"Missing dump: {path}. Enable its dev toggle and load to the main menu.");
    return JsonDocument.Parse(File.ReadAllText(path));
}

// Dumps record "Name [packageid]"; the id alone is the stable key.
static string ShortMod(string mod)
{
    if (mod == null) return "?";
    int open = mod.LastIndexOf('[');
    return open >= 0 ? mod.Substring(open + 1).TrimEnd(']') : mod;
}

static string Str(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
        ? v.GetString() : null;

static bool Bool(JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

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

string ModName(string packageId) => EcosystemMods.TryGetValue(packageId ?? "", out string n) ? n : packageId;

static string Join(IEnumerable<string> values)
{
    var list = values.ToList();
    return list.Count == 0 ? "—" : string.Join(", ", list);
}

static void Head(StringBuilder sb, string title, string blurb)
{
    sb.AppendLine($"# {title}");
    sb.AppendLine();
    sb.AppendLine(blurb);
    sb.AppendLine();
}

static void Write(string dir, string file, StringBuilder sb)
{
    File.WriteAllText(Path.Combine(dir, file), sb.ToString());
    Console.WriteLine($"  {file}");
}

sealed class Surgery
{
    public string DefName, Label, Mod, Attach, AddsHediff, RemovesHediff, AndroidBlockReason;
    public HashSet<string> WorkerChain = new();
    public List<string> TargetParts = new(), Research = new(), Ingredients = new();
    public bool FleshHostPossible;
    public float WorkAmount;
}

sealed class Hediff
{
    public string DefName, Label, Mod;
    public bool IsAddedPart, AndroidCanCatch, IsBad;
    public List<Slot> Slots = new();
}

sealed class Slot
{
    public string Id, Name;
    public int Capacity;
}

sealed class Thing
{
    public string DefName, Label, Mod, TechLevel;
    public bool IsImplantLike;
    public float MarketValue, WorkToMake;
    public List<string> ProducedBy = new();
}

sealed class Project
{
    public string DefName, Label, Mod, Tab, TechLevel, TechprintDef;
    public float BaseCost;
    public List<string> Prerequisites = new();
    public bool HasTechprint, TechprintMissing;
}
