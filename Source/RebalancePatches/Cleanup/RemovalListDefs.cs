using System.Collections.Generic;
using Verse;

namespace RebalancePatches
{
    // These class names are an XML contract: def files reference them as
    // <RebalancePatches.GeneRemovalListDef> and friends, so they cannot be renamed without
    // rewriting every def that uses one. The namespace is what the XML binds to, not the file,
    // so they are free to live here rather than next to the engine that consumes them.

    /// <summary>
    /// Genes to strip via Cherry Picker. Part of the genepool rebalance, so these lists carry its
    /// implicit gate as well as their own <see cref="requiredMods"/>: nothing applies unless all
    /// three core gene mods are loaded.
    /// </summary>
    public class GeneRemovalListDef : Def
    {
        public string settingKey;
        public List<string> requiredMods = new List<string>();
        public List<string> genes = new List<string>();
        public List<string> genePrefixes = new List<string>();
    }

    /// <summary>
    /// Non-gene defs to strip via Cherry Picker. Not part of the genepool rebalance, so no implicit
    /// three-mod gate: each list states its own <see cref="requiredMods"/> and stands on that alone.
    /// Note that Cherry Picker never deletes a ThingDef - it neuters one in place - so expect these
    /// to remain in the database, unobtainable rather than absent.
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
    /// function through the canonical replacement instead of simply losing it.
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
