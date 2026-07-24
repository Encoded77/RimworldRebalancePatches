using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    internal static class Ids
    {
        public const string Royalty = "ludeon.rimworld.royalty";
        public const string Ideology = "ludeon.rimworld.ideology";
        public const string Biotech = "ludeon.rimworld.biotech";
        public const string Anomaly = "ludeon.rimworld.anomaly";
        public const string Odyssey = "ludeon.rimworld.odyssey";

        public const string AlteredCarbon = "hlx.ultratechalteredcarbon";
        public const string BigSmallCore = "redmattis.bigsmall.core";
        public const string BigSmallSlimes = "redmattis.bsslimes";
        public const string BSSimpleAndroids = "redmattis.bigsmall.simpleandroids";
        public const string WVC = "wvc.sergkart.races.biotech";
        public const string VREHighmate = "vanillaracesexpanded.highmate";
        public const string VREArchon = "vanillaracesexpanded.archon";
        public const string VREInsector = "vanillaracesexpanded.insector";
        public const string RimsenalCore = "rimsenal.core";
        public const string RimsenalSpacer = "rimsenal.spacer";
        public const string RimsenalFederation = "rimsenal.federation";
        public const string RimsenalHarana = "rimsenal.harana";
        public const string RimsenalAskbarn = "rimsenal.askbarn";
        public const string RimsenalZohar = "rimsenal.zohar";
        public const string Keshig = "det.keshig";
        public const string Venators = "det.venators";
        public const string Highborn = "elsov.highborn";
        public const string VFEPirates = "oskarpotocki.vfe.pirates";
        public const string VFEEmpire = "oskarpotocki.vfe.empire";
        public const string VFEInsectoids2 = "oskarpotocki.vfe.insectoid2";
        public const string VFEDeserters = "oskarpotocki.vfe.deserters";
        public const string VWE = "vanillaexpanded.vwe";
        public const string VWECoilguns = "vanillaexpanded.vwec";
        public const string VSE = "vanillaexpanded.skills";
        public const string VPE = "vanillaexpanded.vpsycastse";
        public const string VIEMemes = "vanillaexpanded.vmemese";
        public const string VFECore = "vanillaexpanded.vfecore";
        public const string VAEWaste = "vanillaexpanded.vaewaste";
        public const string AlphaGenes = "sarg.alphagenes";
        public const string AlphaMemes = "sarg.alphamemes";
        public const string AlphaMechs = "sarg.alphamechs";
        public const string IntegratedImplants = "lts.i";
        public const string PsychicImplants = "cedaro.psychicimplant";
        public const string ImpactWeaponry = "detvisor.impactweaponryreloaded";
        public const string SpacerArsenal = "det.spacerarsenal";
        public const string EltexWeaponry = "zal.eltexweaponry";
        public const string HautsTraits = "hautarche.hautstraits";
        public const string VQEAncients = "vanillaquestsexpanded.ancients";
        public const string RimIOT = "cn.rimiot";
        public const string GiTS = "moistestwhale.gitscyberbrains";
        public const string EPOEForked = "vat.epoeforked";
        public const string EPOEForkedRoyalty = "vat.epoeforkedroyalty";
        public const string BigSmallFramework = "redmattis.betterprerequisites";
        public const string EBSG = "ebsg.framework";
        public const string ReSplice = "resplice.xotr.core";
        public const string GeneExtractorTiers = "redmattis.geneextractor";
        public const string GeneNodes = "redmattis.genenodes";
        public const string GeneRipperDefi = "defi.generipper";
        public const string GeneRipperDW = "danielwedemeyer.generipper";
        public const string GeneFabrication = "amch.eragon.hcgenefabrication";
        public const string VAEAccessories = "vanillaexpanded.vaeaccessories";
        public const string VGravshipC1 = "vanillaexpanded.gravship";
        public const string AlphaSkills = "sarg.alphaskills";
        public const string HautsFramework = "hautarche.hautsframework";
        public const string VCEF = "vanillaexpanded.vcef";
        public const string VSIE = "vanillaexpanded.vanillasocialinteractionsexpanded";
        public const string WarcasketQuality = "danzinagri.warcasketweaponquality";
        public const string CherryPicker = "owlchemist.cherrypicker";
        public const string VREHussar = "vanillaracesexpanded.hussar";
        public const string VRESaurid = "vanillaracesexpanded.saurid";
        public const string VREPhytokin = "vanillaracesexpanded.phytokin";
        public const string VREGenie = "vanillaracesexpanded.genie";
        public const string VREFungoid = "vanillaracesexpanded.fungoid";
        public const string VREPigskin = "vanillaracesexpanded.pigskin";
        public const string VREWaster = "vanillaracesexpanded.waster";
        public const string VRELycanthrope = "vanillaracesexpanded.lycanthrope";
        public const string VRESanguophage = "vanillaracesexpanded.sanguophage";
        public const string Stoneborn = "det.stoneborn";
        public const string Brawnum = "det.brawnum";
        public const string Halffoot = "det.halffoot";
        public const string Avaloi = "det.avaloi";
        public const string Boglegs = "det.boglegs";
        public const string Buzzers = "det.buzzers";
        public const string BetterGeneInheritance = "redmattis.bettergeneinheritance";
        public const string BSRaces = "redmattis.bigsmall";
        public const string BSYokai = "redmattis.yokai";
        public const string BSHeaven = "redmattis.heaven";
        public const string BSMoreXenos = "redmattis.morexenos";
        public const string BSLamias = "redmattis.lamiasandothersnakes";
        public const string VREStarjack = "vanillaracesexpanded.starjack";
        public const string VREAndroid = "vanillaracesexpanded.android";
        public const string VREAndroidConversion = "derp88.vreandroidconversion";
        public const string YART = "seohyeon.yart";
        public const string UshankaBioWarfare = "ushanka.biologicalwarfare";
        public const string ADogSaid2 = "sambucher.adogsaidanimalprosthetics2";
    }

    internal static class Check
    {
        public static bool Ready(string settingKey, params string[] modIds)
        {
            TestCoverage.BeginTest(CallerName(), settingKey);
            return ReadyCore(settingKey, modIds);
        }

        private static string CallerName()
        {
            try
            {
                var frame = new System.Diagnostics.StackTrace(2, false).GetFrame(0);
                var method = frame?.GetMethod();
                return method == null ? "?" : $"{method.DeclaringType?.Name}.{method.Name}";
            }
            catch { return "?"; }
        }

        private static bool ReadyCore(string settingKey, string[] modIds)
        {
            foreach (string id in modIds)
            {
                if (!ModsConfig.IsActive(id))
                {
                    TestCoverage.SkippedMissingMod(settingKey, id);
                    return false;
                }
            }
            if (!SettingsRegistry.GetEffective(settingKey))
            {
                TestCoverage.SkippedToggleOff(settingKey);
                return false;
            }
            TestCoverage.Ran(settingKey);
            return true;
        }

        public static void GenesGone(params string[] names)
        {
            foreach (string name in names)
                True(DefDatabase<GeneDef>.GetNamedSilentFail(name) == null, $"{name} still present");
        }

        public static void ThingUnobtainable(string name)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
            if (def == null)
                return;    // some other mod did delete it outright; nothing left to check
            Eq(def.BaseMarketValue, 0f, $"{name} BaseMarketValue");
            Eq(def.tradeability, Tradeability.None, $"{name} tradeability");
            True(def.thingCategories == null || def.thingCategories.Count == 0,
                $"{name} still sits in a thing category");
            True(def.thingSetMakerTags == null || def.thingSetMakerTags.Count == 0,
                $"{name} still carries reward tags");
            True(def.recipeMaker?.recipeUsers == null || def.recipeMaker.recipeUsers.Count == 0,
                $"{name} is still craftable at a bench");
        }

        public static bool GeneticsTabLoaded(string settingKey)
        {
            if (DefDatabase<ResearchTabDef>.GetNamedSilentFail("RBP_GeneticsTab") != null)
                return true;
            Log.Message($"[RBP Tests] SKIP {settingKey}: RBP_GeneticsTab not present (geneticsresearch.core off or Biotech absent)");
            return false;
        }

        public static T Def<T>(string defName) where T : Def
        {
            T def = DefDatabase<T>.GetNamedSilentFail(defName);
            if (def == null)
                throw Fail($"FAILED: {typeof(T).Name} '{defName}' not found - target mod renamed or removed it, " +
                    "or one of our own patches deleted it");
            return def;
        }

        public static T Optional<T>(string defName, string settingKey, string ownerMod = null) where T : Def
        {
            T def = DefDatabase<T>.GetNamedSilentFail(defName);
            if (def != null)
                return def;

            string what = $"{typeof(T).Name} '{defName}'";
            if (ownerMod != null && !ModsConfig.IsActive(ownerMod))
            {
                Log.Message($"[RBP Tests] SKIP {settingKey}: {what} absent - mod '{ownerMod}' not active (expected)");
                return null;
            }
            string removedBy = RemovalInfo.ActiveRemovalSetting(defName);
            if (removedBy != null)
            {
                Log.Message($"[RBP Tests] SKIP {settingKey}: {what} absent - removed by our own '{removedBy}' list (expected)");
                return null;
            }
            string ownerNote = ownerMod == null ? "" : $" and '{ownerMod}' is active";
            Log.Warning($"[RBP Tests] SKIP {settingKey}: {what} absent - UNEXPLAINED: no active removal list covers it{ownerNote}, so the patch may be dead (renamed or removed upstream?)");
            return null;
        }

        public static Def DefOfType(string typeName, string defName)
        {
            Type type = GenTypes.GetTypeInAnyAssembly(typeName);
            if (type == null)
                throw new Exception($"Def type '{typeName}' not found in any loaded assembly");
            Def def = GenDefDatabase.GetDefSilentFail(type, defName);
            if (def == null)
                throw new Exception($"{typeName} '{defName}' not found - target mod renamed or removed it");
            return def;
        }

        private static Exception Fail(string message)
        {
            TestCoverage.Failed(message);
            return new Exception(message);
        }

        public static bool Soft(bool condition, string because)
        {
            TestCoverage.Asserted();
            if (!condition)
                TestCoverage.Failed("FAILED: " + because);
            return condition;
        }

        /// <summary>Context recorded against this test, surfaced only if it fails.</summary>
        public static void Note(string message) => TestCoverage.Note(message);

        public static void SoftResult()
        {
            TestCoverage.SoftResultCalled();
            int count = TestCoverage.FailureCount();
            if (count > 0)
                throw new Exception($"FAILED: {count} problem(s) - see TestCoverage.txt for the full list");
        }

        public static void True(bool condition, string because)
        {
            TestCoverage.Asserted();
            if (!condition)
                throw Fail("FAILED: " + because);
        }

        public static void Eq(object actual, object expected, string what)
        {
            TestCoverage.Asserted();
            if (!Equals(actual, expected))
                throw Fail($"FAILED: {what}: expected '{expected ?? "null"}', got '{actual ?? "null"}'");
        }

        public static object Field(object obj, string name)
        {
            FieldInfo f = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null)
                throw new Exception($"Field '{name}' not found on {obj.GetType().FullName}");
            return f.GetValue(obj);
        }

        public static bool HasTag(GeneDef gene, string tag) =>
            gene.exclusionTags != null && gene.exclusionTags.Contains(tag);

        public static void GeneTag(GeneDef gene, string tag) =>
            True(HasTag(gene, tag), $"gene {gene.defName} lacks exclusion tag {tag} (has: {(gene.exclusionTags == null ? "none" : string.Join(", ", gene.exclusionTags))})");

        public static float? StatBase(BuildableDef def, string statDefName)
        {
            if (def.statBases == null)
                return null;
            foreach (StatModifier m in def.statBases)
                if (m.stat != null && m.stat.defName == statDefName)
                    return m.value;
            return null;
        }

        public static float? StatModifierValue(List<StatModifier> list, string statDefName)
        {
            if (list == null)
                return null;
            foreach (StatModifier m in list)
                if (m.stat != null && m.stat.defName == statDefName)
                    return m.value;
            return null;
        }

        public static ResearchProjectDef RecipePrereq(ThingDef def) => def.recipeMaker?.researchPrerequisite;

        public static List<ResearchProjectDef> RecipePrereqs(ThingDef def) => def.recipeMaker?.researchPrerequisites;

        public static void PrereqsAre(List<ResearchProjectDef> actual, string what, params string[] expectedDefNames)
        {
            True(actual != null, $"{what}: research prerequisite list is null");
            var actualNames = new List<string>();
            foreach (ResearchProjectDef p in actual)
                actualNames.Add(p.defName);
            actualNames.Sort();
            var expected = new List<string>(expectedDefNames);
            expected.Sort();
            Eq(string.Join(", ", actualNames), string.Join(", ", expected), what);
        }

        public static bool ContainsResearch(List<ResearchProjectDef> list, string defName)
        {
            if (list == null)
                return false;
            foreach (ResearchProjectDef p in list)
                if (p.defName == defName)
                    return true;
            return false;
        }

        public static int? CostOf(BuildableDef def, string thingDefName)
        {
            if (def.costList == null)
                return null;
            foreach (ThingDefCountClass c in def.costList)
                if (c.thingDef != null && c.thingDef.defName == thingDefName)
                    return c.count;
            return null;
        }

        public static bool HasXenotype(FactionDef faction, string xenotypeDefName)
        {
            XenotypeSet set = faction.xenotypeSet;
            if (set == null)
                return false;
            for (int i = 0; i < set.Count; i++)
                if (set[i].xenotype != null && set[i].xenotype.defName == xenotypeDefName)
                    return true;
            return false;
        }

        public static float XenotypeChanceOf(FactionDef faction, string xenotypeDefName)
        {
            XenotypeSet set = faction.xenotypeSet;
            if (set == null)
                return 0f;
            for (int i = 0; i < set.Count; i++)
                if (set[i].xenotype != null && set[i].xenotype.defName == xenotypeDefName)
                    return set[i].chance;
            return 0f;
        }

        public static float BaselinerShare(FactionDef faction) =>
            faction.xenotypeSet?.BaselinerChance ?? 1f;

        public static void XenoGene(string xenotypeDefName, string geneDefName)
        {
            // Startup tests run before [StaticConstructorOnStartup]; force the rewires on first.
            HarmonyBootstrap.EnsureApplied();
            XenotypeDef xeno = Def<XenotypeDef>(xenotypeDefName);
            bool found = false;
            if (xeno.genes != null)
                foreach (GeneDef g in xeno.genes)
                    if (g.defName == geneDefName)
                        found = true;
            True(found, $"xenotype {xenotypeDefName} lacks gene {geneDefName}");
        }

        public static bool HasForcedTrait(GeneDef gene, string traitDefName)
        {
            if (gene.forcedTraits == null)
                return false;
            foreach (GeneticTraitData t in gene.forcedTraits)
                if (t.def != null && t.def.defName == traitDefName)
                    return true;
            return false;
        }

        public static void HarmonyPatched(MethodBase method, string what)
        {
            // Startup tests run before [StaticConstructorOnStartup]; force our patches on first.
            HarmonyBootstrap.EnsureApplied();
            True(method != null, $"{what}: patch target method not found");
            HarmonyLib.Patches info = HarmonyLib.Harmony.GetPatchInfo(method);
            bool ours = false;
            if (info != null)
            {
                foreach (HarmonyLib.Patch p in info.Postfixes)
                    if (p.owner == "encoded.rebalancepatches")
                        ours = true;
                foreach (HarmonyLib.Patch p in info.Prefixes)
                    if (p.owner == "encoded.rebalancepatches")
                        ours = true;
            }
            True(ours, $"{what}: no RebalancePatches Harmony patch on {method.DeclaringType?.Name}.{method.Name}");
        }

        public static bool AnyDefNamed(IEnumerable defs, string defName)
        {
            if (defs == null)
                return false;
            foreach (object o in defs)
            {
                if (o is Def d && d.defName == defName)
                    return true;
                if (o is string s && s == defName)
                    return true;
            }
            return false;
        }
    }
}
