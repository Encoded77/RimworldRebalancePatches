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
    public static class IntegratedImplantsCortexTests
    {
        /// <summary>An implant we converted: item defName, slot cost, and the mods it needs to exist.</summary>
        private struct Module
        {
            public string Thing;
            public string Recipe;
            public float Capacity;
            public string[] RequiredMods;

            public Module(string thing, string recipe, float capacity, params string[] requiredMods)
            {
                Thing = thing;
                Recipe = recipe;
                Capacity = capacity;
                RequiredMods = requiredMods;
            }
        }

        // Capacity 2 for the psychic implants and the combat one, 1 for the rest.
        private static readonly Module[] Modules =
        {
            new Module("LTS_ArchotechBlackbox", "LTS_InstallArchotechBlackbox", 1f),
            new Module("LTS_MemoryDatabank", "LTS_InstallMemoryDatabank", 1f),
            new Module("LTS_Neurostreamer", "LTS_InstallNeurostreamer", 1f),
            new Module("LTS_BrainDetonator", "LTS_InstallBrainDetonator", 1f),
            new Module("LTS_NeuralStunner", "LTS_InstallNeuralStunner", 1f),
            new Module("LTS_Voicelock", "LTS_InstallVoicelock", 1f),
            new Module("HackingPortImplant", "InstallHackingPortImplant", 1f),
            new Module("PsychicLevitator", "InstallPsychicLevitator", 2f),
            new Module("PsychicNullifier", "InstallPsychicNullifier", 2f),
            new Module("PsychokeneticShield", "InstallPsychokeneticShield", 2f),
            new Module("NeuralHeatsink", "InstallNeuralHeatsink", 1f, Ids.Royalty),
            new Module("LTS_GunneryAssistant", "LTS_InstallGunneryAssistant", 2f, Ids.VGravshipC1),
            new Module("PsychicAgonizer", "InstallPsychicAgonizer", 2f, Ids.Anomaly),
            new Module("PsychicBeguiler", "InstallPsychicBeguiler", 2f, Ids.Anomaly),
            new Module("PsychicSustainer", "InstallPsychicSustainer", 2f, Ids.Anomaly),
            new Module("LTS_PsychicReaper", "LTS_InstallPsychicReaper", 2f, Ids.Anomaly, Ids.Royalty),
        };

        [Test]
        public static void BrainImplantsAreCortexModules()
        {
            if (!Check.Ready("cybernetics.modules", Ids.IntegratedImplants, Ids.EBSG))
                return;

            int checkedModules = 0;
            foreach (Module module in Modules)
            {
                if (!AllActive(module.RequiredMods))
                    continue;

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(module.Thing);
                if (!Check.Soft(def != null, $"{module.Thing} not found - renamed or removed upstream?"))
                    continue;
                checkedModules++;

                object props = ModuleProps(def);
                if (!Check.Soft(props != null,
                    $"{module.Thing} carries no CompProperties_UseEffectHediffModule " +
                    $"(comps: {CompNames(def)})"))
                    continue;

                CheckSlots(module, props);
                CheckCapacity(module, props);
            }

            Check.Soft(checkedModules > 0,
                "no converted implant was present to check - Integrated Implants def names changed?");
            Check.Note($"implants checked: {checkedModules} of {Modules.Length}");
            Check.SoftResult();
        }

        [Test]
        public static void ModuleImplantsInstallThroughOurSurgery()
        {
            if (!Check.Ready("cybernetics.modules", Ids.IntegratedImplants, Ids.EBSG))
                return;

            foreach (Module module in Modules)
            {
                if (!AllActive(module.RequiredMods))
                    continue;

                RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(module.Recipe);
                if (!Check.Soft(recipe != null, $"{module.Recipe} not found - renamed or removed upstream?"))
                    continue;

                Check.Soft(recipe.workerClass == typeof(Recipe_InstallModule),
                    $"{module.Recipe} workerClass is '{recipe.workerClass?.FullName ?? "null"}', " +
                    "not RebalancePatches.Recipe_InstallModule");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CerebrexNodeInstallsBySurgery()
        {
            if (!Check.Ready("implants.cerebrexsurgery", Ids.IntegratedImplants, Ids.Biotech, Ids.Odyssey))
                return;

            ThingDef node = DefDatabase<ThingDef>.GetNamedSilentFail("LTS_CerebrexNode");
            if (!Check.Soft(node != null, "LTS_CerebrexNode not found - renamed or removed upstream?"))
            {
                Check.SoftResult();
                return;
            }

            bool usable = false, installEffect = false;
            if (node.comps != null)
                foreach (CompProperties props in node.comps)
                {
                    string name = props?.GetType().Name;
                    if (name == "CompProperties_Usable") usable = true;
                    if (name == "CompProperties_UseEffectInstallImplant") installEffect = true;
                }
            Check.Soft(!usable, $"LTS_CerebrexNode still carries CompProperties_Usable, so it self-installs (comps: {CompNames(node)})");
            Check.Soft(!installEffect, $"LTS_CerebrexNode still carries CompProperties_UseEffectInstallImplant (comps: {CompNames(node)})");

            // The install surgery we authored must exist, consume the node, and add its hediff.
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("RBP_InstallCerebrexNode");
            if (Check.Soft(recipe != null, "RBP_InstallCerebrexNode surgery was not created"))
            {
                Check.Soft(recipe.addsHediff?.defName == "LTS_CerebrexNodeImplant",
                    $"RBP_InstallCerebrexNode addsHediff is '{recipe.addsHediff?.defName ?? "null"}', expected LTS_CerebrexNodeImplant");
                Check.Soft(recipe.workerClass == typeof(RimWorld.Recipe_InstallImplant),
                    $"RBP_InstallCerebrexNode workerClass is '{recipe.workerClass?.FullName ?? "null"}', not Recipe_InstallImplant");
                bool onBrain = recipe.appliedOnFixedBodyParts != null
                    && recipe.appliedOnFixedBodyParts.Exists(p => p.defName == "Brain");
                Check.Soft(onBrain, "RBP_InstallCerebrexNode is not applied on the Brain");
                bool consumesNode = recipe.ingredients != null && recipe.ingredients.Exists(
                    i => i.filter != null && i.filter.AllowedThingDefs.Any(t => t.defName == "LTS_CerebrexNode"));
                Check.Soft(consumesNode, "RBP_InstallCerebrexNode does not consume LTS_CerebrexNode as an ingredient");
            }

            Check.SoftResult();
        }

        [Test]
        public static void ChipPortStaysAHost()
        {
            if (!Check.Ready("cybernetics.modules", Ids.IntegratedImplants, Ids.EBSG))
                return;

            ThingDef port = DefDatabase<ThingDef>.GetNamedSilentFail("LTS_SkillChipPort");
            if (!Check.Soft(port != null, "LTS_SkillChipPort not found - renamed or removed upstream?"))
            {
                Check.SoftResult();
                return;
            }

            Check.Soft(ModuleProps(port) == null,
                "LTS_SkillChipPort was turned into a module; it must stay a module host");
            Check.SoftResult();
        }

        private static void CheckSlots(Module module, object props)
        {
            IEnumerable slots;
            try
            {
                slots = Check.Field(props, "slotIDs") as IEnumerable;
            }
            catch (Exception e)
            {
                Check.Soft(false, $"{module.Thing} module comp has no slotIDs field: {e.Message}");
                return;
            }

            if (!Check.Soft(slots != null, $"{module.Thing} module comp declares no slotIDs"))
                return;

            var found = new List<string>();
            foreach (object slot in slots)
                found.Add(slot as string);

            Check.Soft(found.Contains("RBP_CortexSlot"),
                $"{module.Thing} does not target RBP_CortexSlot, actually targets: " +
                (found.Count == 0 ? "(none)" : string.Join(", ", found.ToArray())));
        }

        private static void CheckCapacity(Module module, object props)
        {
            try
            {
                float capacity = Convert.ToSingle(Check.Field(props, "requiredCapacity"));
                Check.Soft(capacity == module.Capacity,
                    $"{module.Thing} requiredCapacity is {capacity}, expected {module.Capacity}");
            }
            catch (Exception e)
            {
                Check.Soft(false, $"{module.Thing} requiredCapacity could not be read: {e.Message}");
            }
        }

        private static object ModuleProps(ThingDef def)
        {
            if (def?.comps == null)
                return null;
            foreach (CompProperties props in def.comps)
                if (props != null && props.GetType().Name == "CompProperties_UseEffectHediffModule")
                    return props;
            return null;
        }

        private static string CompNames(ThingDef def)
        {
            if (def?.comps == null)
                return "(none)";
            var names = new List<string>();
            foreach (CompProperties props in def.comps)
                names.Add(props == null ? "null" : props.GetType().Name);
            return names.Count == 0 ? "(none)" : string.Join(", ", names.ToArray());
        }

        private static bool AllActive(string[] packageIds)
        {
            if (packageIds == null)
                return true;
            foreach (string packageId in packageIds)
                if (!ModsConfig.IsActive(packageId))
                    return false;
            return true;
        }
    }
}
