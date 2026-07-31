using System.Collections;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    internal static class PathGenes
    {
        public static void PathGated(string pathModId, string geneDefName, params string[] pathDefNames)
        {
            if (!Check.Ready("vpebiotech.pathgenes", Ids.VPE, Ids.Biotech, Ids.VpeBiotechIntegration, pathModId))
                return;
            if (GeneGatesExternallyDisabled())
            {
                Log.Warning("[RBP Tests] SKIP vpebiotech.pathgenes: Psycasts² disableGeneRequirements is ON and strips every path's requiredGene at startup - the gene gates cannot be asserted (or used) until that setting is off");
                return;
            }
            GeneDef gene = Check.Def<GeneDef>(geneDefName);
            Check.Soft(gene.displayCategory != null && gene.displayCategory.defName == "Psycast",
                $"{geneDefName} displayCategory is {gene.displayCategory?.defName ?? "null"}, expected Psycast");
            Check.Soft(gene.biostatCpx == 3, $"{geneDefName} biostatCpx is {gene.biostatCpx}, expected 3 (base gene did not resolve)");
            foreach (string pathDefName in pathDefNames)
            {
                Def path = Check.DefOfType("VanillaPsycastsExpanded.PsycasterPathDef", pathDefName);
                object required = Check.Field(path, "requiredGene");
                string actual = (required as Def)?.defName ?? "null";
                if (!Check.Soft(actual == geneDefName, $"{pathDefName}.requiredGene is {actual}, expected {geneDefName}"))
                {
                    Def archon = Check.DefOfType("VanillaPsycastsExpanded.PsycasterPathDef", "VPE_Archon");
                    string comparator = (Check.Field(archon, "requiredGene") as Def)?.defName ?? "null";
                    var copies = new System.Collections.Generic.List<string>();
                    foreach (System.Reflection.Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                        if (asm.GetName().Name == "VanillaPsycastsExpanded")
                            copies.Add(asm.ManifestModule.ModuleVersionId.ToString());
                    Check.Note($"{pathDefName}: type={path.GetType().FullName} typeAsmMvid={path.GetType().Assembly.ManifestModule.ModuleVersionId}; " +
                        $"comparator VPE_Archon.requiredGene={comparator}; lockedReason={Check.Field(path, "lockedReason") ?? "null"}; " +
                        $"VPE assemblies loaded: {copies.Count} [{string.Join(", ", copies)}]");
                }
            }
            ThingDef node = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_GN_Psycast");
            if (node != null)
                Check.Soft(NodeCarries(node, geneDefName), $"RBP_GN_Psycast exists but does not carry {geneDefName}");
            else
                Check.Note("RBP_GN_Psycast absent (psycast gene nodes off), roster not asserted");
            Check.SoftResult();
        }

        // Psycasts² ships disableGeneRequirements = true by default, which nulls requiredGene on every
        // PsycasterPathDef in a StaticConstructorOnStartup - after our patches, before our tests.
        private static bool GeneGatesExternallyDisabled()
        {
            if (!ModsConfig.IsActive("astryl.psycasts"))
                return false;
            try
            {
                System.Type modType = GenTypes.GetTypeInAnyAssembly("PsycastSynergies.PsycastSynergiesMod");
                object settings = modType?.GetProperty("Settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null)
                    ?? modType?.GetField("Settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
                if (settings == null)
                    return false;
                System.Reflection.FieldInfo field = settings.GetType().GetField("disableGeneRequirements",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                return field != null && (bool)field.GetValue(settings);
            }
            catch
            {
                return false;
            }
        }

        private static bool NodeCarries(ThingDef node, string geneDefName)
        {
            if (node.comps == null)
                return false;
            foreach (CompProperties comp in node.comps)
            {
                if (comp.GetType().Name != "CompProperties_GeneNode")
                    continue;
                if (Check.Field(comp, "geneList") is IEnumerable genes)
                    foreach (object o in genes)
                        if (o is Def d && d.defName == geneDefName)
                            return true;
            }
            return false;
        }

        public static void XenosCarry(string pathModId, string geneDefName, params string[] modAndXeno)
        {
            if (!Check.Ready("vpebiotech.xenotypes", Ids.VPE, Ids.Biotech, Ids.VpeBiotechIntegration, pathModId))
                return;
            if (!SettingsRegistry.GetEffective("vpebiotech.pathgenes"))
            {
                Log.Message($"[RBP Tests] SKIP vpebiotech.xenotypes: {geneDefName} rosters need vpebiotech.pathgenes on (expected)");
                return;
            }
            Check.Def<GeneDef>(geneDefName);
            for (int i = 0; i < modAndXeno.Length; i += 2)
            {
                if (!ModsConfig.IsActive(modAndXeno[i]))
                    continue;
                XenoSoft(modAndXeno[i + 1], geneDefName);
            }
            Check.SoftResult();
        }

        // Flat triplets: gene-owner gate mods (comma list, empty = none beyond danzen), xenotype, gene.
        public static void RaceRoster(string raceModId, params string[] triplets)
        {
            if (!Check.Ready("vpebiotech.xenotypes", Ids.VPE, Ids.Biotech, Ids.VpeBiotechIntegration, raceModId))
                return;
            for (int i = 0; i < triplets.Length; i += 3)
            {
                bool gated = false;
                if (!triplets[i].NullOrEmpty())
                    foreach (string gate in triplets[i].Split(','))
                        if (!ModsConfig.IsActive(gate))
                            gated = true;
                if (gated)
                    continue;
                XenoSoft(triplets[i + 1], triplets[i + 2]);
            }
            Check.SoftResult();
        }

        public static void XenoSoft(string xenotypeDefName, string geneDefName)
        {
            XenotypeDef xeno = DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeDefName);
            if (!Check.Soft(xeno != null, $"xenotype {xenotypeDefName} not found though its mod is active"))
                return;
            bool found = false;
            if (xeno.genes != null)
                foreach (GeneDef g in xeno.genes)
                    if (g.defName == geneDefName)
                        found = true;
            Check.Soft(found, $"xenotype {xenotypeDefName} lacks gene {geneDefName}");
        }
    }
}
