using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class NpcModuleHostTests
    {
        private const string Key = "cybernetics.npchosts";
        private const string ModulesKey = "cybernetics.modules";

        private const int Cap = 2;

        [Test]
        public static void EveryModuleDeclaresTheSlotsItNeeds()
        {
            if (!Check.Ready(ModulesKey, Ids.EBSG))
                return;

            var silent = new List<string>();
            var slots = new HashSet<string>();
            int modules = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props == null)
                    continue;
                modules++;
                List<string> ids = ModuleApi.RequiredSlotIds(props);
                if (ids.Count == 0)
                    silent.Add(def.defName);
                foreach (string id in ids)
                    slots.Add(id);
            }

            Check.Note($"{modules} module(s) across {slots.Count} slot family/families: "
                + string.Join(", ", slots.OrderBy(s => s).ToArray()));
            // A module naming no slot fits nowhere at all - not a host problem, a broken conversion.
            Check.Soft(modules > 0, "no modules exist at all - the conversion patches did not apply");
            Check.Soft(silent.Count == 0,
                $"{silent.Count} module(s) name no slot, so nothing can ever host them: "
                + string.Join(", ", silent.Take(10).ToArray()));
            Check.SoftResult();
        }

        [Test]
        public static void EverySlotFamilyHasAHostToFit()
        {
            if (!Check.Ready(Key, Ids.EBSG))
                return;

            var needed = new HashSet<string>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props != null)
                    foreach (string id in ModuleApi.RequiredSlotIds(props))
                        needed.Add(id);
            }
            if (!Check.Soft(needed.Count > 0, "no module names a slot, so this asserts nothing"))
            {
                Check.SoftResult();
                return;
            }

            var hosts = NpcModuleHostPatches.HostsBySlot();
            var orphans = new List<string>();
            foreach (string slot in needed.OrderBy(s => s))
            {
                if (!hosts.TryGetValue(slot, out var options) || options.Count == 0)
                {
                    orphans.Add(slot);
                    continue;
                }
                Check.Note($"{slot}: {options.Count} host(s), cheapest {options[0].recipe.defName} "
                    + $"at {options[0].cost:0}");
            }

            Check.Soft(orphans.Count == 0,
                $"{orphans.Count} slot family/families have no host surgery in this modlist, so their "
                + "modules are dropped from every generated pawn: " + string.Join(", ", orphans.ToArray()));
            Check.SoftResult();
        }

        [Test]
        public static void ChainsRouteThroughTheCheapestPartTheyMustInstall()
        {
            if (!Check.Ready(Key, Ids.EBSG))
                return;

            var precursors = NpcModuleHostPatches.PrecursorsByHediff();
            int families = 0;
            foreach (var pair in NpcModuleHostPatches.HostsBySlot())
            {
                var options = pair.Value;
                if (options.Count < 2)
                    continue;

                float chosen = PrecursorCost(precursors, options[0].recipe);
                if (chosen < 0f)
                    continue;   // the cheapest host installs directly - nothing to route around

                families++;
                foreach (var other in options)
                {
                    float rival = PrecursorCost(precursors, other.recipe);
                    if (rival < 0f)
                        continue;
                    Check.Soft(chosen <= rival,
                        $"{pair.Key}: picked {options[0].recipe.defName}, whose precursor costs "
                        + $"{chosen:0}, over {other.recipe.defName} at {rival:0} - a chain is being "
                        + "chosen on the surgery's own price instead of the part it must install first");
                }
            }

            Check.Note($"{families} slot family/families are reached through a converting host");
            Check.SoftResult();
        }

        private static float PrecursorCost(
            Dictionary<HediffDef, List<NpcModuleHostPatches.HostOption>> precursors, RecipeDef host)
        {
            if (host.removesHediff == null)
                return -1f;
            if (!precursors.TryGetValue(host.removesHediff, out var options))
                return -1f;
            foreach (var option in options)
                if (option.recipe != host)
                    return option.cost;
            return -1f;
        }

        [Test]
        public static void TheGeneratorHookIsInPlace()
        {
            if (!Check.Ready(Key, Ids.EBSG))
                return;

            var target = AccessTools.Method(
                AccessTools.TypeByName("EBSGFramework.HarmonyPatches"), "InstallInitialPartPostfix");
            Check.HarmonyPatched(target, "module install during pawn generation");
        }

        [Test]
        public static void AHostlessPawnGetsOneFitted()
        {
            if (!Check.Ready(Key, Ids.EBSG))
                return;

            ThingDef module = FirstHostableModule();
            if (!Check.Soft(module != null,
                "no module in this modlist has a host surgery, so the fitter could not be exercised"))
            {
                Check.SoftResult();
                return;
            }

            Pawn pawn = MakeTestPawn();
            if (!Check.Soft(pawn != null, "could not generate a test pawn - nothing was asserted"))
            {
                Check.SoftResult();
                return;
            }

            try
            {
                object props = ModuleApi.ModulePropsOf(module);
                Check.Note($"module {module.defName}, slots "
                    + string.Join("/", ModuleApi.RequiredSlotIds(props).ToArray()));
                if (!Check.Soft(!ModuleApi.HasOpenSlotFor(pawn, props),
                    "the fresh test pawn already has a host, so fitting one cannot be observed"))
                {
                    Check.SoftResult();
                    return;
                }

                bool handedOn = NpcModuleHostPatches.InstallInitialPartPrefix(pawn, module);
                Check.Soft(handedOn,
                    "the module was dropped even though a host surgery for its slot exists");
                Check.Soft(ModuleApi.HasOpenSlotFor(pawn, props),
                    "no open slot after fitting - the module would land nowhere");
                Check.SoftResult();
            }
            finally
            {
                Discard(pawn);
            }
        }

        [Test]
        public static void AConversionOnlyHostIsReachedThroughItsPrecursor()
        {
            if (!Check.Ready(Key, Ids.EBSG, Ids.IntegratedImplants))
                return;

            ThingDef module = FirstConversionHostedModule(out string slot);
            if (!Check.Soft(module != null,
                "no module's slots are supplied purely by conversion surgeries, so the precursor "
                + "chain was not exercised at all"))
            {
                Check.SoftResult();
                return;
            }

            var options = NpcModuleHostPatches.HostsBySlot()[slot];
            Check.Note($"module {module.defName}, slot {slot}, cheapest host {options[0].recipe.defName} "
                + $"converts {options[0].recipe.removesHediff?.defName ?? "?"}");

            Pawn pawn = MakeTestPawn();
            if (!Check.Soft(pawn != null, "could not generate a test pawn - nothing was asserted"))
            {
                Check.SoftResult();
                return;
            }

            try
            {
                object props = ModuleApi.ModulePropsOf(module);
                if (!Check.Soft(!ModuleApi.HasOpenSlotFor(pawn, props),
                    "the fresh test pawn already has a host, so the chain cannot be observed"))
                {
                    Check.SoftResult();
                    return;
                }

                bool handedOn = NpcModuleHostPatches.InstallInitialPartPrefix(pawn, module);
                Check.Soft(handedOn, "the module was dropped - the conversion host was never reached");
                Check.Soft(ModuleApi.HasOpenSlotFor(pawn, props),
                    "no open slot after fitting - the module would land nowhere");
                Check.Soft(Offers(pawn, slot),
                    $"the pawn carries nothing offering {slot}, so the conversion step did not happen");
                Check.SoftResult();
            }
            finally
            {
                Discard(pawn);
            }
        }

        [Test]
        public static void NoMoreHostsThanTheCapGoOntoOnePawn()
        {
            if (!Check.Ready(Key, Ids.EBSG))
                return;

            // One module per family, so every call asks for a host the pawn does not already carry.
            List<ThingDef> modules = OneHostableModulePerSlotFamily();
            if (!Check.Soft(modules.Count > Cap,
                $"only {modules.Count} hostable slot family/families exist in this modlist, so a cap "
                + $"of {Cap} could not be pressured and nothing was asserted"))
            {
                Check.SoftResult();
                return;
            }

            Pawn pawn = MakeTestPawn();
            if (!Check.Soft(pawn != null, "could not generate a test pawn - nothing was asserted"))
            {
                Check.SoftResult();
                return;
            }

            try
            {
                int before = HostCount(pawn);
                var trace = new List<string>();
                foreach (ThingDef module in modules)
                {
                    NpcModuleHostPatches.InstallInitialPartPrefix(pawn, module);
                    trace.Add($"{module.defName} -> {HostCount(pawn) - before}");
                }

                int fitted = HostCount(pawn) - before;
                Check.Note($"{modules.Count} module(s) offered, {before} host(s) already present: "
                    + string.Join(", ", trace.ToArray()));
                // Both halves matter: a fitter that installs nothing would satisfy the cap trivially.
                Check.Soft(fitted > 0,
                    "no host was fitted at all, so the cap was never actually exercised");
                Check.Soft(fitted <= Cap,
                    $"{fitted} host(s) went onto one generated pawn; the cap is {Cap}");
                Check.SoftResult();
            }
            finally
            {
                Discard(pawn);
            }
        }

        /// <summary>Modular hosts currently on the pawn - what the cap counts.</summary>
        private static int HostCount(Pawn pawn) => ModuleApi.ModularHosts(pawn).Count();

        /// <summary>Whether anything on the pawn offers this slot.</summary>
        private static bool Offers(Pawn pawn, string slot)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                if (ModuleApi.OfferedSlotIds(hediff.def).Contains(slot))
                    return true;
            return false;
        }

        private static ThingDef FirstConversionHostedModule(out string slot)
        {
            slot = null;
            var hosts = NpcModuleHostPatches.HostsBySlot();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.OrderBy(d => d.defName))
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props == null)
                    continue;

                string found = null;
                bool direct = false;
                foreach (string id in ModuleApi.RequiredSlotIds(props))
                {
                    if (!hosts.TryGetValue(id, out var options) || options.Count == 0)
                        continue;
                    if (options.Any(o => o.recipe.removesHediff == null))
                        direct = true;
                    else if (found == null)
                        found = id;
                }
                if (found != null && !direct)
                {
                    slot = found;
                    return def;
                }
            }
            return null;
        }

        /// <summary>One hostable module per slot family, so the cap can be pushed at.</summary>
        private static List<ThingDef> OneHostableModulePerSlotFamily()
        {
            var hosts = NpcModuleHostPatches.HostsBySlot();
            var seen = new HashSet<string>();
            var picked = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.OrderBy(d => d.defName))
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props == null)
                    continue;
                foreach (string id in ModuleApi.RequiredSlotIds(props))
                {
                    if (!hosts.ContainsKey(id) || !seen.Add(id))
                        continue;
                    picked.Add(def);
                    break;
                }
            }
            return picked;
        }

        /// <summary>A module whose slots some host surgery in this modlist can supply.</summary>
        private static ThingDef FirstHostableModule()
        {
            var hosts = NpcModuleHostPatches.HostsBySlot();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.OrderBy(d => d.defName))
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props == null)
                    continue;
                foreach (string slot in ModuleApi.RequiredSlotIds(props))
                    if (hosts.ContainsKey(slot))
                        return def;
            }
            return null;
        }

        private static Pawn MakeTestPawn()
        {
            try
            {
                return PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    allowAddictions: false, allowFood: false));
            }
            catch (System.Exception e)
            {
                Log.Warning("[RBP Tests] could not generate a test pawn: " + e.Message);
                return null;
            }
        }

        private static void Discard(Pawn pawn)
        {
            try
            {
                if (pawn != null && !pawn.Destroyed)
                    pawn.Destroy();
            }
            catch { }
        }
    }
}
