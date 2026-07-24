using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RebalancePatchesTests")]

namespace RebalancePatches
{
    internal static class ModuleApi
    {
        private static bool resolved;
        private static Type compModularType, compUseEffectType, compPropsUseEffectType, moduleSlotType, compPropsModularType;
        private static MethodInfo getOpenSlotsByProps, installModule, removeModule;
        private static FieldInfo ownerHediffField, usedSlotField, slotIdField, slotIdsField, slotsField;
        private static FieldInfo payloadDefsField, linkedHediffsField;

        public static bool Available
        {
            get { Resolve(); return compModularType != null; }
        }

        private static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            try
            {
                compModularType = GenTypes.GetTypeInAnyAssembly("EBSGFramework.HediffComp_Modular");
                compUseEffectType = GenTypes.GetTypeInAnyAssembly("EBSGFramework.CompUseEffect_HediffModule");
                compPropsUseEffectType = GenTypes.GetTypeInAnyAssembly("EBSGFramework.CompProperties_UseEffectHediffModule");
                moduleSlotType = GenTypes.GetTypeInAnyAssembly("EBSGFramework.ModuleSlot");
                compPropsModularType = GenTypes.GetTypeInAnyAssembly("EBSGFramework.HediffCompProperties_Modular");
                if (compModularType == null || compUseEffectType == null || compPropsUseEffectType == null
                    || moduleSlotType == null || compPropsModularType == null)
                {
                    compModularType = null;
                    return;
                }

                getOpenSlotsByProps = compModularType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "GetOpenSlots"
                                         && m.GetParameters().Length == 1
                                         && m.GetParameters()[0].ParameterType == compPropsUseEffectType);
                installModule = compModularType.GetMethod("InstallModule", BindingFlags.Public | BindingFlags.Instance);
                removeModule = compModularType.GetMethod("RemoveModule", BindingFlags.Public | BindingFlags.Instance);
                ownerHediffField = compUseEffectType.GetField("ownerHediff", BindingFlags.Public | BindingFlags.Instance);
                usedSlotField = compUseEffectType.GetField("usedSlot", BindingFlags.Public | BindingFlags.Instance);
                slotIdField = moduleSlotType.GetField("slotID", BindingFlags.Public | BindingFlags.Instance);
                slotIdsField = compPropsUseEffectType.GetField("slotIDs", BindingFlags.Public | BindingFlags.Instance);
                slotsField = compPropsModularType.GetField("slots", BindingFlags.Public | BindingFlags.Instance);
                payloadDefsField = compPropsUseEffectType.GetField("hediffs", BindingFlags.Public | BindingFlags.Instance);
                linkedHediffsField = compUseEffectType.GetField("linkedHediffs", BindingFlags.Public | BindingFlags.Instance);

                if (getOpenSlotsByProps == null || installModule == null
                    || ownerHediffField == null || usedSlotField == null || slotIdField == null
                    || slotIdsField == null || slotsField == null)
                {
                    Log.Error("[RebalancePatches] EBSG module API found but its members did not resolve; module surgeries disabled.");
                    compModularType = null;
                }
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] Resolving the EBSG module API failed; module surgeries disabled: " + e);
                compModularType = null;
            }
        }

        /// <summary>The module properties on a ThingDef, or null if it is not a module.</summary>
        public static object ModulePropsOf(ThingDef def)
        {
            Resolve();
            if (compPropsUseEffectType == null || def?.comps == null) return null;
            foreach (CompProperties props in def.comps)
                if (props != null && compPropsUseEffectType.IsInstanceOfType(props))
                    return props;
            return null;
        }

        /// <summary>Every hediff on the pawn carrying a modular comp, paired with that comp.</summary>
        public static IEnumerable<(Hediff Hediff, object Comp)> ModularHosts(Pawn pawn)
        {
            Resolve();
            if (compModularType == null || pawn?.health?.hediffSet == null) yield break;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (!(hediff is HediffWithComps withComps) || withComps.comps == null) continue;
                foreach (HediffComp comp in withComps.comps)
                    if (comp != null && compModularType.IsInstanceOfType(comp))
                    {
                        yield return (hediff, comp);
                        break;
                    }
            }
        }

        public static List<string> RequiredSlotIds(object moduleProps)
        {
            Resolve();
            var ids = new List<string>();
            if (slotIdsField == null || moduleProps == null) return ids;
            if (slotIdsField.GetValue(moduleProps) is IEnumerable raw)
                foreach (object id in raw)
                    if (id is string s && !ids.Contains(s))
                        ids.Add(s);
            return ids;
        }

        /// <summary>Whether the members the quality layer needs resolved.</summary>
        public static bool QualityMembersAvailable
        {
            get { Resolve(); return compModularType != null && payloadDefsField != null && linkedHediffsField != null; }
        }

        public static List<HediffDef> PayloadHediffs(object moduleProps)
        {
            Resolve();
            var defs = new List<HediffDef>();
            if (payloadDefsField == null || moduleProps == null) return defs;
            if (payloadDefsField.GetValue(moduleProps) is IEnumerable raw)
                foreach (object entry in raw)
                    if (entry is HediffDef def && !defs.Contains(def))
                        defs.Add(def);
            return defs;
        }

        public static List<Hediff> LinkedHediffs(object useEffectComp)
        {
            Resolve();
            var hediffs = new List<Hediff>();
            if (linkedHediffsField == null || useEffectComp == null) return hediffs;
            if (linkedHediffsField.GetValue(useEffectComp) is IEnumerable raw)
                foreach (object entry in raw)
                    if (entry is Hediff hediff)
                        hediffs.Add(hediff);
            return hediffs;
        }

        /// <summary>The module comp on an item instance, or null if it is not a module.</summary>
        public static object UseEffectOf(ThingWithComps module)
        {
            Resolve();
            if (compUseEffectType == null || module?.AllComps == null) return null;
            return module.AllComps.FirstOrDefault(c => compUseEffectType.IsInstanceOfType(c));
        }

        /// <summary>The module instances a host currently holds.</summary>
        public static IEnumerable<ThingWithComps> InstalledModules(object modularComp)
        {
            Resolve();
            if (compModularType == null || modularComp == null) yield break;
            FieldInfo holder = compModularType.GetField("moduleHolder", BindingFlags.Public | BindingFlags.Instance);
            if (!(holder?.GetValue(modularComp) is IEnumerable items)) yield break;
            foreach (object item in items)
                if (item is ThingWithComps module)
                    yield return module;
        }

        public static List<string> OfferedSlotIds(HediffDef hediff)
        {
            Resolve();
            var ids = new List<string>();
            if (slotsField == null || hediff?.comps == null) return ids;
            foreach (HediffCompProperties props in hediff.comps)
            {
                if (props == null || !compPropsModularType.IsInstanceOfType(props)) continue;
                if (!(slotsField.GetValue(props) is IEnumerable slots)) continue;
                foreach (object slot in slots)
                    if (slotIdField.GetValue(slot) is string id && !ids.Contains(id))
                        ids.Add(id);
            }
            return ids;
        }

        /// <summary>Whether any host already on the pawn has room for this module.</summary>
        public static bool HasOpenSlotFor(Pawn pawn, object moduleProps)
        {
            foreach (var (_, comp) in ModularHosts(pawn))
                if (FirstOpenSlot(comp, moduleProps) != null)
                    return true;
            return false;
        }

        public static string FirstOpenSlot(object modularComp, object moduleProps)
        {
            Resolve();
            if (getOpenSlotsByProps == null || modularComp == null || moduleProps == null) return null;
            try
            {
                if (!(getOpenSlotsByProps.Invoke(modularComp, new[] { moduleProps }) is IEnumerable slots)) return null;
                foreach (object slot in slots)
                    return slotIdField.GetValue(slot) as string;
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] EBSG GetOpenSlots threw: " + e);
            }
            return null;
        }

        public static bool Install(Hediff hostHediff, object modularComp, ThingWithComps module, string slotId)
        {
            Resolve();
            if (installModule == null || module == null || slotId == null) return false;
            object useComp = module.AllComps?.FirstOrDefault(c => compUseEffectType.IsInstanceOfType(c));
            if (useComp == null) return false;
            try
            {
                ownerHediffField.SetValue(useComp, hostHediff);
                usedSlotField.SetValue(useComp, slotId);
                installModule.Invoke(modularComp, new object[] { module });
                return true;
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] EBSG InstallModule threw: " + e);
                return false;
            }
        }

        public static bool Remove(object modularComp, ThingWithComps module, bool forced)
        {
            Resolve();
            if (removeModule == null || modularComp == null || module == null) return false;
            try
            {
                removeModule.Invoke(modularComp, new object[] { module, forced });
                return true;
            }
            catch (Exception e)
            {
                Log.Error("[RebalancePatches] EBSG RemoveModule threw: " + e);
                return false;
            }
        }
    }
}
