using System.Collections;
using System.Collections.Generic;
using RimTestRedux;
using RimWorld;
using Verse;

namespace RebalancePatches.Tests
{
    [TestSuite]
    public static class EPOEForkedModuleTests
    {
        private const string Key = "cybernetics.modules";
        private const string Worker = "RebalancePatches.Recipe_InstallModule";
        private const string EPOERoyalty = "vat.epoeforkedroyalty";

        [Test]
        public static void RibImplantsAreTorsoModules()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            foreach (string defName in new[]
            {
                "MedicalRib", "DruggedRib", "AdrenalineRib", "PainkillerRib", "HeaterRib",
                "CoolerRib", "CoagulatorRib", "RespirationRib", "WakeUpRib",
            })
                AssertModule(defName, "RBP_TorsoSlot", 1f, defName);

            Check.SoftResult();
        }

        [Test]
        public static void BrainImplantsAreCortexModules()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            AssertModule("AIChip", "RBP_CortexSlot", 1f, "AIChip");
            AssertModule("BrainStimulator", "RBP_CortexSlot", 1f, "BrainStimulator");
            AssertModule("AIPersonaCore", "RBP_CortexSlot", 2f, "AIPersonaCore");

            Check.SoftResult();
        }

        [Test]
        public static void CircadianRefresherIsACortexModule()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            ThingDef def = Check.Optional<ThingDef>("EPIA_CircadianRefresher", Key, EPOERoyalty);
            if (def == null)
                return;

            AssertModuleOn(def, "RBP_CortexSlot", 1f, "EPIA_CircadianRefresher");
            AssertWorker("EPIA_InstallCircadianRefresher", optionalOwner: EPOERoyalty);

            Check.SoftResult();
        }

        [Test]
        public static void ConvertedImplantsInstallViaModuleSurgery()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            foreach (string recipeName in new[]
            {
                "InstallMedicalRib", "InstallDruggedRib", "InstallAdrenalineRib", "InstallPainkillerRib",
                "InstallHeaterRib", "InstallCoolerRib", "InstallCoagulatorRib", "InstallRespirationRib",
                "InstallWakeUpRib", "InstallAIChip", "InstallBrainStimulator", "InstallAIPersonaCore",
            })
                AssertWorker(recipeName);

            Check.SoftResult();
        }

        [Test]
        public static void ConvertedImplantsAreNotSelfInstallable()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            foreach (string defName in new[]
            {
                "MedicalRib", "DruggedRib", "AdrenalineRib", "PainkillerRib", "HeaterRib",
                "CoolerRib", "CoagulatorRib", "RespirationRib", "WakeUpRib",
                "AIChip", "BrainStimulator", "AIPersonaCore",
            })
            {
                ThingDef def = Check.Def<ThingDef>(defName);
                if (def.comps == null)
                    continue;
                bool usable = false;
                foreach (CompProperties props in def.comps)
                    if (props is CompProperties_Usable)
                        usable = true;
                Check.Soft(!usable, $"{defName} still carries CompProperties_Usable, so it can be self-installed");
            }

            Check.SoftResult();
        }

        [Test]
        public static void AuxiliaryAIsAreOnePerSpecialization()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            foreach (var pair in AuxiliaryAIs)
                AssertModule(pair.Key, "LTS_SkillChipSlot", 1f, pair.Value);

            Check.SoftResult();
        }

        [Test]
        public static void AuxiliaryAIsGrantDistinctHediffs()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            var owners = new Dictionary<string, string>();
            foreach (string itemName in AuxiliaryAIs.Keys)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(itemName);
                if (!Check.Soft(def != null, $"ThingDef '{itemName}' not found - did the patch apply?"))
                    continue;

                List<string> granted = GrantedHediffs(def);
                if (!Check.Soft(granted.Count == 1,
                        $"{itemName} grants {granted.Count} hediffs ({string.Join(", ", granted.ToArray())}); a module item must grant exactly one"))
                    continue;

                string hediff = granted[0];
                if (owners.ContainsKey(hediff))
                {
                    Check.Soft(false,
                        $"{itemName} and {owners[hediff]} both grant {hediff}; each specialization needs its own hediff");
                    continue;
                }
                owners[hediff] = itemName;
            }

            Check.Soft(owners.Count == 9,
                $"expected 9 distinct auxiliary AI hediffs, found {owners.Count}");
            Check.SoftResult();
        }

        [Test]
        public static void AuxiliaryAIsInstallViaModuleSurgery()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            foreach (string recipeName in new[]
            {
                "EPIA_InstallAuxiliaryAI_Artisan", "InstallConstructorCore", "InstallDiplomatCore",
                "InstallDoctorCore", "InstallFarmerCore", "InstallMinerCore",
                "EPIA_InstallAuxiliaryAI_Brawler", "EPIA_InstallAuxiliaryAI_Commando",
                "EPIA_InstallAuxiliaryAI_Sharpshooter",
            })
                AssertWorker(recipeName);

            Check.SoftResult();
        }

        [Test]
        public static void GenericAuxiliaryAIsRetired()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            foreach (string defName in new[] { "EPIA_AuxiliaryAI_Worker", "EPIA_AuxiliaryAI_Combat" })
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    Check.Note($"{defName} is gone from the database entirely; neutering was expected to leave it in place");
                    continue;
                }

                var stillFedBy = new List<string>();
                foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                {
                    if (recipe.addsHediff == null || recipe.fixedIngredientFilter == null)
                        continue;
                    if (recipe.fixedIngredientFilter.Allows(def))
                        stillFedBy.Add(recipe.defName);
                }

                Check.Soft(stillFedBy.Count == 0,
                    $"{defName} still feeds {stillFedBy.Count} install surgery/surgeries " +
                    $"({string.Join(", ", stillFedBy.ToArray())}); each should take a per-specialization module");
            }

            Check.SoftResult();
        }

        [Test]
        public static void CorneaImplantIsAnEyeModule()
        {
            if (!Check.Ready(Key, Ids.EPOEForked, Ids.EBSG))
                return;

            AssertModule("TacticalCorneaImplant", "LTS_EyeModuleSlot", 1f, "TacticalCorneaImplant");
            AssertWorker("InstallTacticalCorneaImplant");

            Check.SoftResult();
        }

        // Item -> the one hediff it grants. Worker roles first, then combat.
        private static readonly Dictionary<string, string> AuxiliaryAIs = new Dictionary<string, string>
        {
            { "RBP_AuxiliaryAI_Artisan", "EPIA_AuxiliaryAI_Artisan" },
            { "RBP_AuxiliaryAI_Construction", "ConstructorCore" },
            { "RBP_AuxiliaryAI_Diplomatic", "DiplomatCore" },
            { "RBP_AuxiliaryAI_Medical", "DoctorCore" },
            { "RBP_AuxiliaryAI_Agricultural", "FarmerCore" },
            { "RBP_AuxiliaryAI_Mining", "MinerCore" },
            { "RBP_AuxiliaryAI_Brawler", "EPIA_AuxiliaryAI_Brawler" },
            { "RBP_AuxiliaryAI_Commando", "EPIA_AuxiliaryAI_Commando" },
            { "RBP_AuxiliaryAI_Sharpshooter", "EPIA_AuxiliaryAI_Sharpshooter" },
        };

        private static List<string> GrantedHediffs(ThingDef def)
        {
            var granted = new List<string>();
            if (def.comps == null)
                return granted;
            foreach (CompProperties props in def.comps)
            {
                if (props == null || props.GetType().Name != "CompProperties_UseEffectHediffModule")
                    continue;
                if (Check.Field(props, "hediffs") is IEnumerable hediffs)
                    foreach (object h in hediffs)
                        granted.Add((h as Def)?.defName);
            }
            return granted;
        }

        private static void AssertModule(string defName, string slotID, float capacity, string hediffName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (!Check.Soft(def != null, $"ThingDef '{defName}' not found - renamed or removed upstream?"))
                return;
            AssertModuleOn(def, slotID, capacity, hediffName);
        }

        private static void AssertModuleOn(ThingDef def, string slotID, float capacity, string hediffName)
        {
            if (!Check.Soft(def.comps != null, $"{def.defName} has no comps at all"))
                return;

            object module = null;
            var seen = new List<string>();
            foreach (CompProperties props in def.comps)
            {
                if (props == null)
                    continue;
                seen.Add(props.GetType().Name);
                if (props.GetType().Name == "CompProperties_UseEffectHediffModule")
                    module = props;
            }
            if (!Check.Soft(module != null,
                    $"{def.defName} carries no module comp; comps are: {string.Join(", ", seen.ToArray())}"))
                return;

            var slots = new List<string>();
            if (Check.Field(module, "slotIDs") is IEnumerable slotIDs)
                foreach (object id in slotIDs)
                    slots.Add(id as string);
            Check.Soft(slots.Contains(slotID),
                $"{def.defName} module does not target {slotID}; actually targets: {string.Join(", ", slots.ToArray())}");

            float actual = System.Convert.ToSingle(Check.Field(module, "requiredCapacity"));
            Check.Soft(actual == capacity,
                $"{def.defName} requiredCapacity is {actual}, expected {capacity}");

            var granted = new List<string>();
            if (Check.Field(module, "hediffs") is IEnumerable hediffs)
                foreach (object h in hediffs)
                    granted.Add((h as Def)?.defName);
            Check.Soft(granted.Contains(hediffName),
                $"{def.defName} module does not grant {hediffName}; actually grants: {string.Join(", ", granted.ToArray())}");
        }

        private static void AssertWorker(string recipeName, string optionalOwner = null)
        {
            RecipeDef recipe = optionalOwner == null
                ? DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName)
                : Check.Optional<RecipeDef>(recipeName, Key, optionalOwner);
            if (optionalOwner == null && !Check.Soft(recipe != null,
                    $"RecipeDef '{recipeName}' not found - renamed or removed upstream?"))
                return;
            if (recipe == null)
                return;

            string actual = recipe.workerClass?.FullName ?? "null";
            Check.Soft(actual == Worker,
                $"{recipeName} workerClass is {actual}, expected {Worker}");
        }
    }
}
