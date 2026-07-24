using System;
using System.Collections.Generic;
using System.Linq;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class VREAndroidConversionTests
    {
        private const string Key = "cybernetics.androidconversion";
        private const string Serum = "RBP_NeuroformSerum";
        private const string Kit = "RBP_AndroidConversionKit";
        private const string Surgery = "RBP_ConvertToAndroid";
        private const string Recovery = "RBP_SyntheticAscensionRecovery";
        private const string OldSerum = "ConversionSerum";

        [Test]
        public static void ConversionChainIsPricedAsACapstone()
        {
            if (!Check.Ready(Key, Ids.VREAndroid))
                return;

            ThingDef serum = DefDatabase<ThingDef>.GetNamedSilentFail(Serum);
            ThingDef kit = DefDatabase<ThingDef>.GetNamedSilentFail(Kit);
            if (!Check.Soft(serum != null && kit != null, "neuroform serum or conversion kit not found"))
            {
                Check.SoftResult();
                return;
            }

            Check.Note($"serum {serum.BaseMarketValue:0.#}, kit {kit.BaseMarketValue:0.#}");

            Check.Soft(Math.Abs(serum.BaseMarketValue - 2000f) < 25f,
                $"neuroform serum is {serum.BaseMarketValue:0.#}, expected about 2000");
            Check.Soft(Math.Abs(kit.BaseMarketValue - 7000f) < 25f,
                $"android conversion kit is {kit.BaseMarketValue:0.#}, expected about 7000");

            // The kit consumes a serum outright, so it can never be worth less than one.
            Check.Soft(kit.BaseMarketValue > serum.BaseMarketValue,
                $"the kit costs {kit.BaseMarketValue:0.#} but consumes a serum worth " +
                $"{serum.BaseMarketValue:0.#}");

            ThingDef frame = DefDatabase<ThingDef>.GetNamedSilentFail("RBP_LivingFrame");
            if (frame != null)
                Check.Soft(kit.BaseMarketValue > frame.BaseMarketValue,
                    $"the conversion kit costs {kit.BaseMarketValue:0.#} against the living frame's " +
                    $"{frame.BaseMarketValue:0.#}; the kit is the further capstone of the two");

            foreach (ThingDef def in new[] { serum, kit })
                Check.Soft(def.statBases == null
                        || !def.statBases.Any(s => s.stat == StatDefOf.MarketValue),
                    $"{def.defName} declares a MarketValue statBase; it is craftable, so its price " +
                    "must come from its cost list. A declared price also means re-costing an " +
                    "ingredient changes nothing, which matters because the kit consumes the serum");

            Check.SoftResult();
        }

        [Test]
        public static void ConversionChainIsThreeStages()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            ThingDef serum = Check.Def<ThingDef>(Serum);
            ThingDef kit = Check.Def<ThingDef>(Kit);
            RecipeDef surgery = Check.Def<RecipeDef>(Surgery);

            Check.Soft(serum.recipeMaker != null && Bench(serum, "VREA_AndroidPartWorkbench"),
                $"{Serum} is not crafted at VREA_AndroidPartWorkbench (benches: {BenchList(serum)})");
            Check.Soft(Check.CostOf(serum, "Neutroamine") > 0,
                $"{Serum} costs no neutroamine (costList: {CostList(serum)})");

            // Stage two is what makes this a chain rather than two unrelated items.
            Check.Soft(Check.CostOf(kit, Serum) == 1,
                $"{Kit} does not consume exactly one {Serum} (costList: {CostList(kit)})");
            Check.Soft(Check.CostOf(kit, "VREA_Reactor") == 1,
                $"{Kit} does not consume exactly one VREA_Reactor (costList: {CostList(kit)})");
            Check.Soft(kit.recipeMaker != null && Bench(kit, "VREA_AndroidPartWorkbench"),
                $"{Kit} is not crafted at VREA_AndroidPartWorkbench (benches: {BenchList(kit)})");

            float serumWork = Check.StatBase(serum, "WorkToMake") ?? 0f;
            float kitWork = Check.StatBase(kit, "WorkToMake") ?? 0f;
            Check.Soft(kitWork > serumWork,
                $"{Kit} WorkToMake {kitWork} is not above {Serum}'s {serumWork}");

            Check.Soft(IngredientCount(surgery, Kit) == 1,
                $"{Surgery} does not take exactly one {Kit} (ingredients: {IngredientList(surgery)})");

            Check.SoftResult();
        }

        [Test]
        public static void ConversionIsASurgery()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            RecipeDef surgery = Check.Def<RecipeDef>(Surgery);

            Check.Soft(surgery.IsSurgery,
                $"{Surgery} is not a surgery; recipeUsers = {DefNames(surgery.recipeUsers)}");
            Check.Soft(surgery.workerClass == typeof(Mods.AndroidConversion.Recipe_ConvertToAndroid),
                $"{Surgery} workerClass is {surgery.workerClass?.FullName ?? "null"}, " +
                "so nothing converts the pawn or installs the reactor");
            Check.Soft(!surgery.targetsBodyPart,
                $"{Surgery} targets a body part; a whole-body conversion must not ask for one");
            Check.Soft(surgery.humanlikeOnly, $"{Surgery} is not restricted to humanlikes");

            bool overhaulOn = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("RBP_CybSurgicalImplantation") != null;
            if (overhaulOn)
                Check.Soft(surgery.researchPrerequisite?.defName == "RBP_CybSurgicalImplantation",
                    $"{Surgery} resolves to '{surgery.researchPrerequisite?.defName ?? "ungated"}', "
                    + "expected the surgical-implantation root (D26c)");
            else
                Check.Soft(surgery.researchPrerequisite == null,
                    $"{Surgery} gates on '{surgery.researchPrerequisite?.defName}' with the research "
                    + "overhaul off, so that project does not exist and the gate cannot resolve");

            Check.SoftResult();
        }

        [Test]
        public static void CraftGatesFollowTheResearchOverhaul()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            bool capstones = SettingsRegistry.GetEffective("cyberneticsresearch.capstones");
            string expected = capstones ? "RBP_CybSyntheticAscension" : "VREA_AndroidTech";
            Check.Note($"cyberneticsresearch.capstones is {(capstones ? "on" : "off")}, "
                + $"so both craft gates must be {expected}");

            if (capstones)
                Check.Soft(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(expected) != null,
                    $"{expected} does not exist even though cyberneticsresearch.capstones is on");

            foreach (string name in new[] { Serum, Kit })
            {
                ThingDef def = Check.Def<ThingDef>(name);
                ResearchProjectDef gate = Check.RecipePrereq(def);
                Check.Soft(gate?.defName == expected,
                    $"{name} craft gate is {gate?.defName ?? "none"}, expected {expected}");

                Check.Soft(Check.RecipePrereqs(def).NullOrEmpty(),
                    $"{name} also carries a researchPrerequisites list " +
                    $"({DefNames(Check.RecipePrereqs(def))}); both would be enforced");
            }

            Check.SoftResult();
        }

        [Test]
        public static void SelfInstallRouteRetired()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.VREAndroidConversion, Ids.Biotech))
                return;

            ThingDef old = DefDatabase<ThingDef>.GetNamedSilentFail(OldSerum);
            if (!Check.Soft(old != null,
                $"{OldSerum} is gone from the database entirely; retirement was expected to leave " +
                "it in place so existing saves still load"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(old.recipeMaker == null,
                $"{OldSerum} is still craftable (benches: {BenchList(old)})");
            Check.Soft(old.tradeability == Tradeability.None,
                $"{OldSerum} tradeability is {old.tradeability}, so it can still be bought");

            // The whole point of the retirement: an existing stack must no longer rewrite anyone.
            var doers = new List<string>();
            if (old.ingestible?.outcomeDoers != null)
                foreach (IngestionOutcomeDoer doer in old.ingestible.outcomeDoers)
                    if (doer.GetType().FullName.Contains("ChangeXenotype"))
                        doers.Add(doer.GetType().FullName);
            Check.Soft(doers.Count == 0,
                $"{OldSerum} still carries xenotype-changing outcome doers ({string.Join(", ", doers.ToArray())})");

            var usable = new List<string>();
            if (old.comps != null)
                foreach (CompProperties comp in old.comps)
                    if (comp is CompProperties_Usable)
                        usable.Add(comp.GetType().Name);
            Check.Soft(usable.Count == 0,
                $"{OldSerum} still has a usable comp ({string.Join(", ", usable.ToArray())}), " +
                "so the use gizmo remains on every existing stack");

            var producers = new List<string>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                if (recipe.ProducedThingDef == old && !recipe.recipeUsers.NullOrEmpty())
                    producers.Add(recipe.defName);
            Check.Soft(producers.Count == 0,
                $"{OldSerum} is still produced by {string.Join(", ", producers.ToArray())}");

            Check.SoftResult();
        }

        [Test]
        public static void ConversionBuildsASyntheticBodyWithoutEatingBionics()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            RecipeDef surgery = Check.Def<RecipeDef>(Surgery);
            var worker = surgery.Worker as Mods.AndroidConversion.Recipe_ConvertToAndroid;
            if (!Bail(worker != null,
                    $"{Surgery} resolved no conversion worker, so nothing converts anybody"))
                return;

            Pawn pawn = MakeTestPawn();
            if (!Bail(pawn != null, "could not generate a test pawn - nothing was asserted"))
                return;

            try
            {
                BodyPartRecord arm = FirstPart(pawn, "Arm");
                HediffDef bionic = DefDatabase<HediffDef>.GetNamedSilentFail("BionicArm");
                bool implanted = false;
                if (arm != null && bionic != null)
                {
                    pawn.health.AddHediff(bionic, arm);
                    implanted = HasOnPart(pawn, bionic, arm);
                }
                Check.Note("bionic arm in place before conversion: " + implanted);

                worker.ApplyOnPawn(pawn, null, null, null, null);

                Check.Soft(pawn.genes?.Xenotype?.defName == "VREA_AndroidAwakened",
                    "the pawn did not come out as an awakened android (xenotype: "
                    + (pawn.genes?.Xenotype?.defName ?? "none") + "), so nothing below means anything");

                foreach (string name in new[]
                    { "VREA_AndroidArm", "VREA_AndroidLeg", "VREA_AndroidHand", "VREA_OpticalUnit", "VREA_NeuroPump" })
                {
                    HediffDef part = DefDatabase<HediffDef>.GetNamedSilentFail(name);
                    if (part == null)
                        continue;
                    Check.Soft(CountOf(pawn, part) > 0,
                        $"the converted pawn has no {name}, so it is an android walking around on the "
                        + "flesh it was born with");
                }

                if (implanted)
                    Check.Soft(HasOnPart(pawn, bionic, arm),
                        "the conversion removed a bionic arm the colony had already paid for; android "
                        + "parts are efficiency 1.0, so that is a pure downgrade");

                HediffDef reactor = DefDatabase<HediffDef>.GetNamedSilentFail("VREA_Reactor");
                if (reactor != null)
                    Check.Soft(pawn.health.hediffSet.GetFirstHediffOfDef(reactor) != null,
                        "the converted pawn has no reactor, so its power need can never be filled");

                HediffDef recovery = DefDatabase<HediffDef>.GetNamedSilentFail(Recovery);
                if (Check.Soft(recovery != null, $"{Recovery} does not exist, so nothing slows a "
                        + "converted pawn down and they walk off a whole-body replacement instantly"))
                    Check.Soft(pawn.health.hediffSet.GetFirstHediffOfDef(recovery) != null,
                        $"the converted pawn is not carrying {Recovery}; the surgery finished and left "
                        + "them immediately able-bodied");

                Check.SoftResult();
            }
            finally
            {
                Discard(pawn);
            }
        }

        [Test]
        public static void RecoveryIsRealAndTemporary()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            HediffDef recovery = Check.Def<HediffDef>(Recovery);

            bool disappears = false;
            if (recovery.comps != null)
                foreach (HediffCompProperties comp in recovery.comps)
                    if (comp is HediffCompProperties_Disappears)
                        disappears = true;
            Check.Soft(disappears,
                $"{Recovery} has no disappears comp, so a converted pawn is crippled permanently");

            HediffStage stage = recovery.stages.NullOrEmpty() ? null : recovery.stages[0];
            Check.Soft(stage != null && CapReduced(stage, "Moving") && CapReduced(stage, "Manipulation"),
                $"{Recovery} does not reduce Moving and Manipulation, so it is a label rather than "
                + "downtime (capMods: " + CapMods(stage) + ")");

            int settings = 0;
            if (recovery.modExtensions != null)
                foreach (DefModExtension ext in recovery.modExtensions)
                    if (ext.GetType().FullName == "VREAndroids.AndroidSettingsExtension")
                        settings++;
            Check.Soft(settings == 1,
                $"{Recovery} carries {settings} AndroidSettingsExtension(s); exactly one is required "
                + "because GetModExtension returns the first, so an inherited refusal would mask ours "
                + "(is modExtensions still Inherit=\"False\"?)");

            Check.Soft(AndroidCanCatch(recovery),
                $"{Recovery} is not marked catchable for androids, so synthetic immunity refuses it "
                + "the moment the patient becomes one and the recovery never applies at all");

            Check.SoftResult();
        }

        [Test]
        public static void SerumHasItsOwnLook()
        {
            if (!Check.Ready(Key, Ids.VREAndroid, Ids.Biotech))
                return;

            ThingDef serum = Check.Def<ThingDef>(Serum);
            string path = serum.graphicData?.texPath;
            Check.Note("serum texPath: " + (path ?? "none"));

            if (!Check.Soft(!path.NullOrEmpty(), $"{Serum} declares no texture at all"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(path != "Things/Item/Special/MechSerumHealer",
                $"{Serum} is still wearing the healer mech serum's art, which reads as a use-on-a-pawn "
                + "item - the exact click this feature removed");
            Check.Soft(ContentFinder<UnityEngine.Texture2D>.Get(path, reportFailure: false) != null,
                $"{Serum} points at '{path}', which resolves to no texture; it will render as a "
                + "magenta placeholder");

            Check.SoftResult();
        }

        private static bool CapReduced(HediffStage stage, string capacityDefName)
        {
            if (stage?.capMods == null)
                return false;
            foreach (PawnCapacityModifier mod in stage.capMods)
                if (mod.capacity != null && mod.capacity.defName == capacityDefName)
                    return mod.postFactor < 1f || mod.offset < 0f || mod.setMax < 1f;
            return false;
        }

        private static string CapMods(HediffStage stage)
        {
            if (stage?.capMods == null)
                return "none";
            var parts = new List<string>();
            foreach (PawnCapacityModifier mod in stage.capMods)
                parts.Add($"{mod.capacity?.defName}x{mod.postFactor}");
            return string.Join(", ", parts.ToArray());
        }

        /// <summary>Reads the android race's own per-hediff opt-in without referencing its assembly.</summary>
        private static bool AndroidCanCatch(HediffDef def)
        {
            if (def.modExtensions == null)
                return false;
            foreach (DefModExtension ext in def.modExtensions)
                if (ext.GetType().FullName == "VREAndroids.AndroidSettingsExtension")
                    return Check.Field(ext, "androidCanCatchIt") as bool? == true;
            return false;
        }

        private static BodyPartRecord FirstPart(Pawn pawn, string bodyPartDefName)
        {
            foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
                if (part.def.defName == bodyPartDefName)
                    return part;
            return null;
        }

        private static bool HasOnPart(Pawn pawn, HediffDef def, BodyPartRecord part)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                if (hediff.def == def && hediff.Part == part)
                    return true;
            return false;
        }

        private static int CountOf(Pawn pawn, HediffDef def)
        {
            int count = 0;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                if (hediff.def == def)
                    count++;
            return count;
        }

        /// <summary>Soft check that also closes the test out when it fails.</summary>
        private static bool Bail(bool condition, string because)
        {
            if (Check.Soft(condition, because))
                return true;
            Check.SoftResult();
            return false;
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

        private static bool Bench(ThingDef def, string benchDefName)
        {
            if (def.recipeMaker?.recipeUsers == null)
                return false;
            foreach (ThingDef user in def.recipeMaker.recipeUsers)
                if (user.defName == benchDefName)
                    return true;
            return false;
        }

        private static string BenchList(ThingDef def) =>
            def.recipeMaker?.recipeUsers == null ? "none" : DefNames(def.recipeMaker.recipeUsers);

        private static string CostList(ThingDef def)
        {
            if (def.costList.NullOrEmpty())
                return "empty";
            var parts = new List<string>();
            foreach (ThingDefCountClass cost in def.costList)
                parts.Add($"{cost.thingDef?.defName}x{cost.count}");
            return string.Join(", ", parts.ToArray());
        }

        private static int IngredientCount(RecipeDef recipe, string thingDefName)
        {
            ThingDef wanted = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
            if (wanted == null || recipe.ingredients == null)
                return -1;
            foreach (IngredientCount ing in recipe.ingredients)
                if (ing.filter != null && ing.filter.Allows(wanted))
                    return (int)ing.GetBaseCount();
            return -1;
        }

        private static string IngredientList(RecipeDef recipe)
        {
            if (recipe.ingredients.NullOrEmpty())
                return "none";
            var parts = new List<string>();
            foreach (IngredientCount ing in recipe.ingredients)
                parts.Add(ing.Summary);
            return string.Join(", ", parts.ToArray());
        }

        private static string DefNames<T>(List<T> defs) where T : Def
        {
            if (defs.NullOrEmpty())
                return "none";
            var names = new List<string>();
            foreach (T def in defs)
                names.Add(def.defName);
            return string.Join(", ", names.ToArray());
        }
    }
}
