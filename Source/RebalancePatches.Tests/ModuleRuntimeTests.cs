using System.Linq;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class ModuleRuntimeTests
    {
        [Test]
        public static void InstallSurgeryIsOfferedForAModule()
        {
            if (!Check.Ready("cybernetics.modules", Ids.IntegratedImplants, Ids.EBSG))
                return;

            RecipeDef recipe = Check.Def<RecipeDef>("RBP_InstallSkillChipAnimals");
            Check.True(recipe.IsSurgery, "chip install is a surgery");
            Check.Eq(recipe.workerClass?.FullName, "RebalancePatches.Recipe_InstallModule",
                "chip install uses the module recipe worker");

            HediffDef portDef = Check.Def<HediffDef>("LTS_SkillChipPort");
            Pawn pawn = MakeTestPawn();
            if (pawn == null)
                return;

            try
            {
                Check.True(!recipe.Worker.AvailableOnNow(pawn), "not offered without a chip port");

                pawn.health.AddHediff(portDef, pawn.health.hediffSet.GetBrain());
                Check.True(recipe.Worker.AvailableOnNow(pawn), "offered once a chip port exists");
                Check.True(recipe.Worker.GetPartsToApplyOn(pawn, recipe).Any(),
                    "reports a body part to operate on");
            }
            finally
            {
                Discard(pawn);
            }
        }

        [Test]
        public static void ChipPortCapacityIsEnforced()
        {
            if (!Check.Ready("cybernetics.modules", Ids.IntegratedImplants, Ids.EBSG))
                return;

            HediffDef portDef = Check.Def<HediffDef>("LTS_SkillChipPort");
            ThingDef chipA = Check.Def<ThingDef>("LTS_SpacerSkillChip_Animals");
            ThingDef chipB = Check.Def<ThingDef>("LTS_SpacerSkillChip_Construction");
            ThingDef chipC = Check.Def<ThingDef>("LTS_SpacerSkillChip_Cooking");

            Pawn pawn = MakeTestPawn();
            if (pawn == null)
                return;

            try
            {
                Hediff port = pawn.health.AddHediff(portDef, pawn.health.hediffSet.GetBrain());
                object host = ModuleApi.ModularHosts(pawn).FirstOrDefault().Comp;
                Check.True(host != null, "chip port carries a modular comp");

                Check.True(InstallOne(port, host, chipA), "first chip installs");
                Check.True(InstallOne(port, host, chipB), "second chip installs");
                Check.True(!InstallOne(port, host, chipC), "third chip is refused - capacity 2 holds");
            }
            finally
            {
                Discard(pawn);
            }
        }

        [Test]
        public static void ModuleQualitySurvivesInstall()
        {
            if (!Check.Ready("cybernetics.modules", Ids.IntegratedImplants, Ids.EBSG))
                return;

            HarmonyBootstrap.EnsureApplied();

            QualitySubject subject = FindQualityScaledModule();
            if (!Check.Soft(subject != null,
                "no module in this modlist both carries quality and has a payload quality can scale, "
                + "so module quality was not exercised at all"))
            {
                Check.SoftResult();
                return;
            }

            Check.Note($"subject {subject.module.defName} -> payload {subject.payload.defName}, "
                + $"{subject.what} at {subject.Baseline:0.####} unmodified");

            Outcome legendary = Install(subject, QualityCategory.Legendary);
            Outcome awful = Install(subject, QualityCategory.Awful);
            Check.Note($"legendary: {legendary}; awful: {awful}");

            if (!Check.Soft(legendary.installed && awful.installed,
                    $"the module could not be installed on a generated pawn ({legendary.why} / {awful.why}), "
                    + "so neither half of quality was checked"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(legendary.storedQuality == QualityCategory.Legendary,
                $"installed module holds quality {legendary.storedQuality}, not Legendary - the "
                + "surgery is handing the framework a fresh copy instead of the ingredient");
            Check.Soft(awful.storedQuality == QualityCategory.Awful,
                $"installed module holds quality {awful.storedQuality}, not Awful");

            Check.Soft(!Near(legendary.effect, awful.effect),
                $"a legendary module does {legendary.effect:0.####} and an awful one {awful.effect:0.####} "
                + "- quality is stored but changes nothing");
            float wantLegendary = subject.Expected(QualityCategory.Legendary);
            float wantAwful = subject.Expected(QualityCategory.Awful);
            Check.Soft(Near(legendary.effect, wantLegendary),
                $"legendary payload is {legendary.effect:0.####}, expected {wantLegendary:0.####}");
            Check.Soft(Near(awful.effect, wantAwful),
                $"awful payload is {awful.effect:0.####}, expected {wantAwful:0.####}");

            Check.Soft(Near(legendary.severity, ModuleQuality.SeverityOf(QualityCategory.Legendary)),
                $"legendary payload sits at severity {legendary.severity:0.###}, expected "
                + $"{ModuleQuality.SeverityOf(QualityCategory.Legendary):0.###}");
            Check.SoftResult();
        }

        /// <summary>What one install of a known quality produced.</summary>
        private class Outcome
        {
            public bool installed;
            public string why = "not attempted";
            public QualityCategory storedQuality;
            public float severity;
            public float effect;

            public override string ToString() =>
                installed
                    ? $"{storedQuality} sev {severity:0.###} effect {effect:0.####}"
                    : $"not installed ({why})";
        }

        private enum Kind { StatOffset, StatFactor, CapacityOffset }

        private class QualitySubject
        {
            public ThingDef module;
            public HediffDef payload;
            public Kind kind;
            public StatDef stat;
            public PawnCapacityDef capacity;
            public string what;

            public float Baseline => Read(payload.stages[0]);

            public float Read(HediffStage stage)
            {
                if (stage == null)
                    return kind == Kind.StatFactor ? 1f : 0f;
                switch (kind)
                {
                    case Kind.StatOffset:
                        return Check.StatModifierValue(stage.statOffsets, stat.defName) ?? 0f;
                    case Kind.StatFactor:
                        return Check.StatModifierValue(stage.statFactors, stat.defName) ?? 1f;
                    default:
                        if (stage.capMods != null)
                            foreach (PawnCapacityModifier capMod in stage.capMods)
                                if (capMod.capacity == capacity)
                                    return capMod.offset;
                        return 0f;
                }
            }

            public float Expected(QualityCategory quality)
            {
                float factor = ModuleQuality.FactorOf(quality);
                float baseline = Baseline;
                switch (kind)
                {
                    case Kind.StatOffset:
                        return baseline * factor;
                    case Kind.StatFactor:
                        return System.Math.Max(0f, 1f + (baseline - 1f) * factor);
                    default:
                        return baseline > 0f ? baseline * factor : baseline / factor;
                }
            }
        }

        private static QualitySubject FindQualityScaledModule()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.OrderBy(d => d.defName))
            {
                object props = ModuleApi.ModulePropsOf(def);
                if (props == null || !def.HasComp(typeof(CompQuality)))
                    continue;
                if (!ModuleApi.RequiredSlotIds(props).Any(NpcModuleHostPatches.HostsBySlot().ContainsKey))
                    continue;
                foreach (HediffDef payload in ModuleApi.PayloadHediffs(props))
                {
                    if (!ModuleQuality.IsBanded(payload))
                        continue;
                    HediffStage stage = payload.stages[0];
                    if (stage.statOffsets != null)
                        foreach (StatModifier offset in stage.statOffsets)
                            if (offset.value != 0f && offset.stat != null)
                                return new QualitySubject
                                {
                                    module = def, payload = payload, kind = Kind.StatOffset,
                                    stat = offset.stat, what = "stat offset " + offset.stat.defName,
                                };
                    if (stage.capMods != null)
                        foreach (PawnCapacityModifier capMod in stage.capMods)
                            if (capMod.offset != 0f && capMod.capacity != null)
                                return new QualitySubject
                                {
                                    module = def, payload = payload, kind = Kind.CapacityOffset,
                                    capacity = capMod.capacity,
                                    what = "capacity offset " + capMod.capacity.defName,
                                };
                    if (stage.statFactors != null)
                        foreach (StatModifier statFactor in stage.statFactors)
                            if (statFactor.value != 1f && statFactor.stat != null)
                                return new QualitySubject
                                {
                                    module = def, payload = payload, kind = Kind.StatFactor,
                                    stat = statFactor.stat,
                                    what = "stat factor " + statFactor.stat.defName,
                                };
                }
            }
            return null;
        }

        private static Outcome Install(QualitySubject subject, QualityCategory quality)
        {
            var outcome = new Outcome();
            Pawn pawn = MakeTestPawn();
            if (pawn == null)
            {
                outcome.why = "no test pawn could be generated";
                return outcome;
            }

            try
            {
                object props = ModuleApi.ModulePropsOf(subject.module);
                if (!FitHost(pawn, props))
                {
                    outcome.why = "no host surgery in this modlist put a slot on the pawn";
                    return outcome;
                }

                var host = ModuleApi.ModularHosts(pawn)
                    .FirstOrDefault(h => ModuleApi.FirstOpenSlot(h.Comp, props) != null);
                if (host.Comp == null)
                {
                    outcome.why = "the fitted host had no open slot";
                    return outcome;
                }

                if (!(ThingMaker.MakeThing(subject.module) is ThingWithComps module))
                {
                    outcome.why = "the module def did not make a ThingWithComps";
                    return outcome;
                }
                CompQuality comp = module.TryGetComp<CompQuality>();
                if (comp == null)
                {
                    outcome.why = "the module instance has no quality comp";
                    return outcome;
                }
                comp.SetQuality(quality, ArtGenerationContext.Colony);

                var existing = pawn.health.hediffSet.hediffs.Where(h => h.def == subject.payload).ToList();

                string slot = ModuleApi.FirstOpenSlot(host.Comp, props);
                if (!ModuleApi.Install(host.Hediff, host.Comp, module, slot))
                {
                    outcome.why = "the framework refused the install";
                    return outcome;
                }

                ThingWithComps stored = ModuleApi.InstalledModules(host.Comp)
                    .FirstOrDefault(t => t.def == subject.module);
                if (stored?.TryGetComp<CompQuality>() == null)
                {
                    outcome.why = "the host is not holding the module, or holds one without quality";
                    return outcome;
                }
                Hediff payload = pawn.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.def == subject.payload && !existing.Contains(h));
                if (payload == null)
                {
                    outcome.why = "the module installed but its payload hediff never appeared";
                    return outcome;
                }

                outcome.installed = true;
                outcome.why = "installed";
                outcome.storedQuality = stored.TryGetComp<CompQuality>().Quality;
                outcome.severity = payload.Severity;
                outcome.effect = subject.Read(payload.CurStage);
                return outcome;
            }
            catch (System.Exception e)
            {
                outcome.why = "threw: " + e.Message;
                return outcome;
            }
            finally
            {
                Discard(pawn);
            }
        }

        private static bool FitHost(Pawn pawn, object props)
        {
            if (ModuleApi.HasOpenSlotFor(pawn, props))
                return true;

            var hosts = NpcModuleHostPatches.HostsBySlot();
            foreach (string slot in ModuleApi.RequiredSlotIds(props))
            {
                if (!hosts.TryGetValue(slot, out var options))
                    continue;
                foreach (var option in options)
                {
                    RecipeDef recipe = option.recipe;
                    if (!pawn.def.AllRecipes.Contains(recipe) || !recipe.Worker.AvailableOnNow(pawn))
                        continue;
                    var parts = recipe.Worker.GetPartsToApplyOn(pawn, recipe).ToList();
                    if (recipe.targetsBodyPart && parts.Count == 0)
                        continue;
                    recipe.Worker.ApplyOnPawn(pawn, parts.FirstOrDefault(), null, NoIngredients, null);
                    if (ModuleApi.HasOpenSlotFor(pawn, props))
                        return true;
                }
            }
            return false;
        }

        private static readonly System.Collections.Generic.List<Thing> NoIngredients =
            new System.Collections.Generic.List<Thing>();

        private static bool Near(float a, float b) => System.Math.Abs(a - b) <= 0.0005f + System.Math.Abs(b) * 0.01f;

        private static bool InstallOne(Hediff host, object hostComp, ThingDef moduleDef)
        {
            object props = ModuleApi.ModulePropsOf(moduleDef);
            string slot = ModuleApi.FirstOpenSlot(hostComp, props);
            if (slot == null)
                return false;
            ThingWithComps module = ThingMaker.MakeThing(moduleDef) as ThingWithComps;
            return module != null && ModuleApi.Install(host, hostComp, module, slot);
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
