using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class SymbioticIntegrationTests
    {
        private const string FrameHediff = "RBP_LivingFrameHediff";
        private const string FrameThing = "RBP_LivingFrame";
        private const string ImplantHediff = "RBP_GeneDivergenceImplantHediff";
        private const string ImplantThing = "RBP_GeneDivergenceImplant";

        [Test]
        public static void CapstonesArePricedOnWhatTheyAdd()
        {
            if (!Check.Ready("cybernetics.livingframe"))
                return;

            ThingDef frame = DefDatabase<ThingDef>.GetNamedSilentFail(FrameThing);
            ThingDef weave = DefDatabase<ThingDef>.GetNamedSilentFail(ImplantThing);
            if (!Check.Soft(frame != null && weave != null, "living frame or apotheosis weave not found"))
            {
                Check.SoftResult();
                return;
            }

            Check.Note($"living frame {frame.BaseMarketValue:0.#}, apotheosis weave {weave.BaseMarketValue:0.#}");

            Check.Soft(Math.Abs(frame.BaseMarketValue - 4980f) < 25f,
                $"living frame is {frame.BaseMarketValue:0.#}, expected about 4980 - six torso slots " +
                "priced at what a bolt-on module is worth");
            Check.Soft(Math.Abs(weave.BaseMarketValue - 4980f) < 25f,
                $"apotheosis weave is {weave.BaseMarketValue:0.#}, expected about 4980 - roughly six " +
                "strong bolt-ons, the package a fully diverged host gets");

            ThingDef archotechFrame = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_ThoracicFrameArchotech");
            if (archotechFrame != null)
            {
                Check.Note($"archotech thoracic frame {archotechFrame.BaseMarketValue:0.#}");
                Check.Soft(frame.BaseMarketValue > archotechFrame.BaseMarketValue,
                    $"the living frame costs {frame.BaseMarketValue:0.#} against the archotech " +
                    $"thoracic frame's {archotechFrame.BaseMarketValue:0.#}, yet it carries two more " +
                    "module slots and can actually be built");
            }

            foreach (ThingDef def in new[] { frame, weave })
                Check.Soft(def.statBases == null
                        || !def.statBases.Any(s => s.stat == StatDefOf.MarketValue),
                    $"{def.defName} declares a MarketValue statBase; it is craftable, so its price " +
                    "must come from its cost list rather than from a fixed number");

            Check.SoftResult();
        }

        [Test]
        public static void LivingFrameHasMoreCapacityThanArchotech()
        {
            if (!Check.Ready("cybernetics.livingframe", Ids.EBSG))
                return;

            HediffDef frame = DefDatabase<HediffDef>.GetNamedSilentFail(FrameHediff);
            if (!Check.Soft(frame != null, $"{FrameHediff} not found"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(frame.addedPartProps != null && frame.addedPartProps.solid
                       && frame.addedPartProps.betterThanNatural,
                $"{FrameHediff} addedPartProps are not a solid better-than-natural part");
            Check.Soft(frame.spawnThingOnRemoved?.defName == FrameThing,
                $"{FrameHediff} pops out as '{frame.spawnThingOnRemoved?.defName ?? "nothing"}', expected {FrameThing}");

            float? capacity = TorsoSlotCapacity(frame);
            if (!Check.Soft(capacity.HasValue, $"{FrameHediff} has no RBP_TorsoSlot - EBSG is active, so the slot patch should have applied"))
            {
                Check.SoftResult();
                return;
            }
            Check.Soft(capacity.Value == 6f,
                $"living frame RBP_TorsoSlot capacity is {capacity.Value}, expected 6");

            HediffDef archotech = DefDatabase<HediffDef>.GetNamedSilentFail("RBP_ThoracicFrameArchotechHediff");
            float? archotechCapacity = archotech == null ? null : TorsoSlotCapacity(archotech);
            if (archotechCapacity.HasValue)
            {
                Check.Note($"archotech thoracic frame capacity: {archotechCapacity.Value}");
                Check.Soft(capacity.Value > archotechCapacity.Value,
                    $"living frame ({capacity.Value}) does not beat the archotech thoracic frame ({archotechCapacity.Value}) - " +
                    "the flesh capstone's whole reward is breadth");
            }
            else
            {
                // cybernetics.thoracicframe is a separate toggle; its absence is not a defect here.
                Check.Note("archotech thoracic frame absent (cybernetics.thoracicframe off), comparison skipped");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CapstoneItemsAreCraftableAtTheRightTier()
        {
            if (!Check.Ready("cybernetics.livingframe"))
                return;

            bool capstoneExists =
                DefDatabase<ResearchProjectDef>.GetNamedSilentFail("RBP_CybSymbioticIntegration") != null;
            Check.Note("RBP_CybSymbioticIntegration present: " + capstoneExists);

            foreach (string thingName in new[] { FrameThing, ImplantThing })
            {
                ThingDef thing = DefDatabase<ThingDef>.GetNamedSilentFail(thingName);
                if (!Check.Soft(thing != null, $"{thingName} not found"))
                    continue;

                Check.Soft(thing.isTechHediff, $"{thingName} is not a tech hediff item");
                if (!Check.Soft(thing.recipeMaker != null, $"{thingName} has no recipeMaker - it cannot be built at all"))
                    continue;

                string gate = thing.recipeMaker.researchPrerequisite?.defName ?? "no gate";
                if (capstoneExists)
                    Check.Soft(gate == "RBP_CybSymbioticIntegration",
                        $"{thingName} crafts at '{gate}', expected RBP_CybSymbioticIntegration");
                else
                    Check.Soft(gate != "Bionics" && gate != "no gate",
                        $"{thingName} crafts at '{gate}' - an endgame item must not fall back to the bionic tier");
            }

            string[] craftingGates = { "Bionics", "AdvancedBionics", "AdvancedFabrication", "RBP_CybSymbioticIntegration" };
            foreach (string recipeName in new[]
            {
                "RBP_InstallLivingFrame",
                "RBP_RemoveLivingFrame",
                "RBP_InstallGeneDivergenceImplant",
                "RBP_RemoveGeneDivergenceImplant",
            })
            {
                RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (!Check.Soft(recipe != null, $"{recipeName} not found"))
                    continue;

                var offending = new List<string>();
                if (recipe.researchPrerequisite != null
                    && Array.IndexOf(craftingGates, recipe.researchPrerequisite.defName) >= 0)
                    offending.Add(recipe.researchPrerequisite.defName);
                if (recipe.researchPrerequisites != null)
                    foreach (ResearchProjectDef prereq in recipe.researchPrerequisites)
                        if (prereq != null && Array.IndexOf(craftingGates, prereq.defName) >= 0)
                            offending.Add(prereq.defName);

                Check.Soft(offending.Count == 0,
                    $"{recipeName} is gated on crafting-tier research ({string.Join(", ", offending.ToArray())}) - " +
                    "installs belong at the surgical implantation root, or a rewarded part cannot be fitted");
            }

            Check.SoftResult();
        }

        [Test]
        public static void SyntheticBodiesAreRefusedBothItems()
        {
            if (!Check.Ready("cybernetics.livingframe", Ids.VREAndroid))
                return;

            List<string> disallowed = AndroidDisallowedRecipes();
            if (!Check.Soft(disallowed != null,
                    "VREA_AndroidSettings.disallowedRecipes could not be read - the android exclusion hook moved"))
            {
                Check.SoftResult();
                return;
            }

            foreach (string recipeName in new[]
            {
                "RBP_InstallLivingFrame",
                "RBP_RemoveLivingFrame",
                "RBP_InstallGeneDivergenceImplant",
                "RBP_RemoveGeneDivergenceImplant",
            })
                Check.Soft(disallowed.Contains(recipeName),
                    $"{recipeName} is not in VREA_AndroidSettings.disallowedRecipes - androids can take the flesh capstone");

            RecipeDef install = DefDatabase<RecipeDef>.GetNamedSilentFail("RBP_InstallLivingFrame");
            if (install != null)
            {
                Check.Soft(Check.AnyDefNamed(install.appliedOnFixedBodyParts, "Ribcage"),
                    "RBP_InstallLivingFrame does not target Ribcage");
                Check.Soft(install.workerClass == typeof(Recipe_InstallLivingPart),
                    $"RBP_InstallLivingFrame uses worker '{install.workerClass?.Name ?? "null"}', not "
                    + "Recipe_InstallLivingPart - nothing then stops a mechanical ribcage hosting it");

                BodyPartDef mechanical = DefDatabase<BodyPartDef>.GetNamedSilentFail("BS_MechanicalRibs");
                if (mechanical != null)
                    Check.Soft(!mechanical.alive,
                        "BS_MechanicalRibs reports alive=true, so the living-tissue filter this frame "
                        + "relies on would let it through - Big and Small changed the part upstream");
                else
                    Check.Note("BS_MechanicalRibs absent; the living-tissue filter is untested in this modlist");
            }

            Check.SoftResult();
        }

        [Test]
        public static void BaselinerStillGetsABonus()
        {
            if (!Check.Ready("cybernetics.livingframe"))
                return;

            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(ImplantHediff);
            if (!Check.Soft(hediff != null, $"{ImplantHediff} not found"))
            {
                Check.SoftResult();
                return;
            }

            HediffCompProperties_GeneDivergence props = DivergenceProps(hediff);
            if (!Check.Soft(props != null, $"{ImplantHediff} carries no gene-divergence comp"))
            {
                Check.SoftResult();
                return;
            }
            if (!Check.Soft(hediff.stages != null && hediff.stages.Count > 1,
                    $"{ImplantHediff} has {hediff.stages?.Count ?? 0} stages - divergence needs somewhere to go"))
            {
                Check.SoftResult();
                return;
            }

            // A baseliner is literally the empty gene list against the empty Baseliner gene list.
            float baselinerPoints = HediffComp_GeneDivergence.DivergencePoints(
                new List<GeneDef>(),
                DefDatabase<XenotypeDef>.GetNamedSilentFail("Baseliner")?.genes);
            Check.Soft(baselinerPoints == 0f,
                $"a baseliner scores {baselinerPoints} divergence points, expected 0");

            float floorSeverity = HediffComp_GeneDivergence.SeverityFor(
                baselinerPoints, props.pointsPerTier, props.minSeverity, props.maxSeverity);
            Check.Soft(floorSeverity == props.minSeverity,
                $"zero divergence resolves to severity {floorSeverity}, expected the floor {props.minSeverity}");

            HediffStage floor = hediff.stages[hediff.StageAtSeverity(floorSeverity)];
            Check.Note($"floor stage: '{floor.label}' at severity {floorSeverity}");

            float floorStatTotal = PositiveStatOffsetTotal(floor);
            float floorCapTotal = PositiveCapOffsetTotal(floor);
            Check.Soft(floorStatTotal > 0f,
                $"the floor stage '{floor.label}' grants {floorStatTotal} of positive stat offsets - " +
                "a colony that never touched genetics would get an implant that does nothing");
            Check.Soft(floorCapTotal > 0f,
                $"the floor stage '{floor.label}' grants {floorCapTotal} of positive capacity offsets - " +
                "a colony that never touched genetics would get an implant that does nothing");

            // And divergence must still be worth chasing, or the floor has swallowed the feature.
            HediffStage top = hediff.stages[hediff.stages.Count - 1];
            Check.Note($"top stage: '{top.label}' at severity {top.minSeverity}, " +
                       $"stat offsets {PositiveStatOffsetTotal(top)} vs floor {floorStatTotal}");
            Check.Soft(PositiveStatOffsetTotal(top) > floorStatTotal,
                $"the top stage '{top.label}' is no better than the floor - divergence buys nothing");

            Pawn pawn = MakeTestPawn();
            if (pawn == null)
            {
                Check.SoftResult();
                return;
            }
            try
            {
                float pawnPoints = HediffComp_GeneDivergence.DivergencePointsFor(pawn);
                Check.Note($"test pawn xenotype '{pawn.genes?.Xenotype?.defName ?? "none"}' scored {pawnPoints} points");

                Hediff installed = pawn.health.AddHediff(hediff, TorsoOf(pawn));
                Check.Soft(installed.Severity >= props.minSeverity,
                    $"installed implant sits at severity {installed.Severity}, below the floor {props.minSeverity}");
                Check.Soft(installed.CurStage != null && PositiveStatOffsetTotal(installed.CurStage) > 0f,
                    $"installed implant landed on stage '{installed.CurStage?.label ?? "none"}' with no positive stat offsets");
                if (pawnPoints == 0f)
                    Check.Soft(Math.Abs(installed.Severity - props.minSeverity) < 0.0001f,
                        $"a zero-divergence pawn landed at severity {installed.Severity}, expected exactly {props.minSeverity}");
            }
            finally
            {
                Discard(pawn);
            }

            Check.SoftResult();
        }

        [Test]
        public static void DivergenceIsMeasuredAgainstTheOwnXenotypeBaseline()
        {
            if (!Check.Ready("cybernetics.livingframe", Ids.Biotech))
                return;

            XenotypeDef xenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail("Hussar");
            if (xenotype == null || xenotype.genes == null || xenotype.genes.Count == 0)
                xenotype = DefDatabase<XenotypeDef>.AllDefs
                    .FirstOrDefault(x => x.genes != null && x.genes.Count >= 2);
            if (!Check.Soft(xenotype != null, "no xenotype with a gene list to measure against"))
            {
                Check.SoftResult();
                return;
            }
            Check.Note($"measuring against xenotype '{xenotype.defName}' ({xenotype.genes.Count} genes)");

            float stock = HediffComp_GeneDivergence.DivergencePoints(
                new List<GeneDef>(xenotype.genes), xenotype.genes);
            Check.Soft(stock == 0f,
                $"a stock {xenotype.defName} scores {stock} divergence points, expected 0 - " +
                "divergence is being measured against a baseliner rather than the pawn's own kind");

            GeneDef extra = DefDatabase<GeneDef>.AllDefs.FirstOrDefault(g => !xenotype.genes.Contains(g));
            if (!Check.Soft(extra != null, "no gene outside the xenotype's list to add"))
            {
                Check.SoftResult();
                return;
            }

            var engineered = new List<GeneDef>(xenotype.genes) { extra };
            float withExtra = HediffComp_GeneDivergence.DivergencePoints(engineered, xenotype.genes);
            float expected = 1f + Math.Abs(extra.biostatMet);
            Check.Soft(withExtra == expected,
                $"one extra gene ('{extra.defName}', met {extra.biostatMet}) scored {withExtra}, expected {expected} " +
                "(one point for the gene plus the magnitude of its metabolic cost)");

            // A gene taken away is also a departure from the baseline, and must cost the same.
            var stripped = new List<GeneDef>(xenotype.genes);
            GeneDef removed = stripped[0];
            stripped.RemoveAt(0);
            float withMissing = HediffComp_GeneDivergence.DivergencePoints(stripped, xenotype.genes);
            Check.Soft(withMissing == 1f + Math.Abs(removed.biostatMet),
                $"a stripped {xenotype.defName} (missing '{removed.defName}') scored {withMissing}, " +
                $"expected {1f + Math.Abs(removed.biostatMet)}");

            Check.SoftResult();
        }

        private static float? TorsoSlotCapacity(HediffDef hediff)
        {
            if (hediff.comps == null)
                return null;
            foreach (HediffCompProperties props in hediff.comps)
            {
                if (props == null || !props.GetType().Name.Contains("HediffCompProperties_Modular"))
                    continue;
                if (!(Check.Field(props, "slots") is IEnumerable slots))
                    continue;
                foreach (object slot in slots)
                    if ((Check.Field(slot, "slotID") as string) == "RBP_TorsoSlot")
                        return Convert.ToSingle(Check.Field(slot, "capacity"));
            }
            return null;
        }

        private static HediffCompProperties_GeneDivergence DivergenceProps(HediffDef hediff)
        {
            if (hediff.comps == null)
                return null;
            foreach (HediffCompProperties props in hediff.comps)
                if (props is HediffCompProperties_GeneDivergence divergence)
                    return divergence;
            return null;
        }

        private static float PositiveStatOffsetTotal(HediffStage stage)
        {
            float total = 0f;
            if (stage?.statOffsets != null)
                foreach (StatModifier modifier in stage.statOffsets)
                    if (modifier.value > 0f)
                        total += modifier.value;
            return total;
        }

        private static float PositiveCapOffsetTotal(HediffStage stage)
        {
            float total = 0f;
            if (stage?.capMods != null)
                foreach (PawnCapacityModifier modifier in stage.capMods)
                    if (modifier.offset > 0f)
                        total += modifier.offset;
            return total;
        }

        private static List<string> AndroidDisallowedRecipes()
        {
            try
            {
                Type settingsType = GenTypes.GetTypeInAnyAssembly("VREAndroids.AndroidSettings");
                if (settingsType == null)
                    return null;
                Def settings = GenDefDatabase.GetDefSilentFail(settingsType, "VREA_AndroidSettings");
                if (settings == null)
                    return null;
                if (!(Check.Field(settings, "disallowedRecipes") is IEnumerable entries))
                    return null;
                var names = new List<string>();
                foreach (object entry in entries)
                    if (entry is string name)
                        names.Add(name);
                return names;
            }
            catch
            {
                return null;
            }
        }

        private static BodyPartRecord TorsoOf(Pawn pawn)
        {
            return pawn.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(part => part.def == BodyPartDefOf.Torso);
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
            catch (Exception e)
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
