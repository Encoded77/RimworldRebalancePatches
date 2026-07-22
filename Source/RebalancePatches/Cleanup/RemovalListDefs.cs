using System.Collections.Generic;
using Verse;

namespace RebalancePatches
{
    // These class names are an XML contract: def files reference them as
    // <RebalancePatches.GeneRemovalListDef> and friends, so they cannot be renamed without
    // rewriting every def that uses one. The namespace is what the XML binds to, not the file,
    // so they are free to live here rather than next to the engine that consumes them.

    /// <summary>
    /// Genes to strip via Cherry Picker. A list stands on its own <see cref="requiredMods"/> plus
    /// the load folder it ships in, which already gates the mod owning the genes: name the mod that
    /// owns the surviving gene, so a removal never fires without its canonical replacement present.
    /// A list whose survivor is vanilla, or that removes without replacing, needs no requiredMods.
    /// </summary>
    public class GeneRemovalListDef : Def
    {
        public string settingKey;
        public List<string> requiredMods = new List<string>();
        public List<string> genes = new List<string>();
        public List<string> genePrefixes = new List<string>();
    }

    /// <summary>
    /// Non-gene defs to strip via Cherry Picker. Each list states its own
    /// <see cref="requiredMods"/> and stands on that alone. Note that Cherry Picker never deletes a
    /// ThingDef - it neuters one in place - so expect these to remain in the database, unobtainable
    /// rather than absent.
    /// </summary>
    public class ThingRemovalListDef : Def
    {
        public string settingKey;
        public List<string> requiredMods = new List<string>();
        public List<string> things = new List<string>();
    }

    /// <summary>
    /// Recipes to strip via Cherry Picker. Mostly for recipes another mod generates at startup
    /// rather than declaring in XML, which a patch operation cannot reach - Cherry Picker's second
    /// pass runs at the main menu, by which point implied defs exist.
    /// </summary>
    public class RecipeRemovalListDef : Def
    {
        public string settingKey;
        public List<string> requiredMods = new List<string>();
        public List<string> recipes = new List<string>();
    }

    /// <summary>
    /// Genes to hand to a xenotype, so a xenotype that lost a gene to a removal list keeps the
    /// function through the canonical replacement instead of simply losing it. A replacement from
    /// an absent mod is skipped silently, so a rewire handing out only modded genes gates itself;
    /// state <see cref="requiredMods"/> when the replacement is vanilla but the paired removal is
    /// gated, or the xenotype would gain a gene it never lost one for.
    /// </summary>
    public class XenotypeRewireDef : Def
    {
        public string settingKey;
        public List<string> requiredMods = new List<string>();
        public List<XenotypeRewireEntry> xenotypes = new List<XenotypeRewireEntry>();
    }

    public class XenotypeRewireEntry
    {
        public string xenotype;
        public List<string> genes = new List<string>();
    }
}
