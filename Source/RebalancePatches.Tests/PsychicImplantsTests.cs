using System.Collections;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class PsychicImplantsTests
    {
        [Test]
        public static void EveryPsychicImplantCanBeRemoved()
        {
            if (!Check.Ready("cybernetics.modules", Ids.PsychicImplants, Ids.EBSG))
                return;

            var installable = new System.Collections.Generic.HashSet<string>();
            var removable = new System.Collections.Generic.HashSet<string>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe.addsHediff != null && recipe.addsHediff.defName.StartsWith("PI_"))
                    installable.Add(recipe.addsHediff.defName);
                if (recipe.removesHediff != null && recipe.removesHediff.defName.StartsWith("PI_"))
                    removable.Add(recipe.removesHediff.defName);
            }

            var stuck = new System.Collections.Generic.List<string>();
            foreach (string h in installable)
                if (!removable.Contains(h))
                    stuck.Add(h);

            Check.Soft(stuck.Count == 0,
                $"{stuck.Count} psychic implant(s) can be installed but never removed " +
                $"({string.Join(", ", stuck.ToArray())}) - with ejectable=false that is permanent");
            Check.SoftResult();
        }

        private static readonly Dictionary<string, float> Modules = new Dictionary<string, float>
        {
            { "PI_AMC", 1f },         // MeditationFocusGain x1.5
            { "PI_AMV", 1f },         // passive meditation, no stat modifiers
            { "PI_HOPS", 2f },        // PsychicSensitivity +0.75
            { "PI_NHCS", 1f },        // PsychicEntropyGain x2, VPE_PsyfocusCostFactor x0.5
            { "PI_NHEC", 2f },        // PsychicEntropyMax x2
            { "PI_NHRA", 2f },        // PsychicEntropyRecoveryRate x2
            { "PI_NHSV", 1f },        // one-shot overload protection, no stat modifiers
            { "PI_Passivator", 1f },  // PsychicSensitivity x0.02
            { "PI_PFC", 2f },         // endless psyfocus, PsychicEntropyRecoveryRate x0.25
            { "PI_PRC", 1f },         // VPE_PsyfocusCostFactor x0.75
            { "PI_Resistance", 1f },  // VPE_PsyfocusCostFactor x0.5, MeditationFocusGain x0.5
            { "PI_TPDD", 1f },        // PsychicSensitivity -0.02, blocks drone and soothe
        };

        [Test]
        public static void ImplantsAreCortexModules()
        {
            if (!Check.Ready("cybernetics.modules", Ids.PsychicImplants, Ids.EBSG))
                return;

            foreach (KeyValuePair<string, float> pair in Modules)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(pair.Key);
                if (!Check.Soft(def != null, $"{pair.Key} ThingDef missing"))
                    continue;
                if (!Check.Soft(def.comps != null, $"{pair.Key} has no comps at all"))
                    continue;

                object module = null;
                foreach (CompProperties props in def.comps)
                    if (props != null && props.GetType().Name.Contains("CompProperties_UseEffectHediffModule"))
                    {
                        module = props;
                        break;
                    }
                if (!Check.Soft(module != null,
                        $"{pair.Key} carries no module comp; comps are: {CompNames(def)}"))
                    continue;

                IEnumerable slotIDs = Check.Field(module, "slotIDs") as IEnumerable;
                if (Check.Soft(slotIDs != null, $"{pair.Key} module comp declares no slotIDs"))
                {
                    bool cortex = false;
                    List<string> seen = new List<string>();
                    foreach (object slot in slotIDs)
                    {
                        seen.Add(slot as string);
                        if ((slot as string) == "RBP_CortexSlot")
                            cortex = true;
                    }
                    Check.Soft(cortex,
                        $"{pair.Key} does not target RBP_CortexSlot; slotIDs are: {string.Join(", ", seen.ToArray())}");
                }

                object capacity = Check.Field(module, "requiredCapacity");
                if (Check.Soft(capacity != null, $"{pair.Key} module comp has no requiredCapacity"))
                    Check.Soft(System.Convert.ToSingle(capacity) == pair.Value,
                        $"{pair.Key} requiredCapacity is {System.Convert.ToSingle(capacity)}, expected {pair.Value}");

                IEnumerable hediffs = Check.Field(module, "hediffs") as IEnumerable;
                if (Check.Soft(hediffs != null, $"{pair.Key} module comp grants no hediffs"))
                {
                    bool granted = false;
                    List<string> seen = new List<string>();
                    foreach (object hediff in hediffs)
                    {
                        HediffDef hd = hediff as HediffDef;
                        seen.Add(hd == null ? "null" : hd.defName);
                        if (hd != null && hd.defName == pair.Key)
                            granted = true;
                    }
                    Check.Soft(granted,
                        $"{pair.Key} module does not grant hediff {pair.Key}; grants: {string.Join(", ", seen.ToArray())}");
                }

                foreach (CompProperties props in def.comps)
                    Check.Soft(props == null || !props.GetType().Name.Contains("CompProperties_Usable"),
                        $"{pair.Key} is still self-usable");
            }

            Check.SoftResult();
        }

        [Test]
        public static void ImplantSurgeriesUseModuleWorker()
        {
            if (!Check.Ready("cybernetics.modules", Ids.PsychicImplants, Ids.EBSG))
                return;

            foreach (string thing in Modules.Keys)
            {
                string recipeName = "Install_" + thing;
                RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (!Check.Soft(recipe != null, $"{recipeName} RecipeDef missing"))
                    continue;

                Check.Soft(recipe.workerClass == typeof(Recipe_InstallModule),
                    $"{recipeName} worker is {(recipe.workerClass == null ? "null" : recipe.workerClass.FullName)}, " +
                    "expected RebalancePatches.Recipe_InstallModule");
            }

            Check.SoftResult();
        }

        private static string CompNames(ThingDef def)
        {
            List<string> names = new List<string>();
            if (def.comps != null)
                foreach (CompProperties props in def.comps)
                    names.Add(props == null ? "null" : props.GetType().Name);
            return names.Count == 0 ? "(none)" : string.Join(", ", names.ToArray());
        }
    }
}
