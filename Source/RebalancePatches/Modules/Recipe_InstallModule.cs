using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    public class Recipe_InstallModule : Recipe_Surgery
    {
        /// <summary>The module ThingDef this recipe installs: its first fixed ingredient.</summary>
        private ThingDef ModuleDef =>
            recipe?.ingredients?.FirstOrDefault()?.filter?.AllowedThingDefs?.FirstOrDefault(d => ModuleApi.ModulePropsOf(d) != null);

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!ModuleApi.Available || !(thing is Pawn pawn)) return false;
            object moduleProps = ModuleApi.ModulePropsOf(ModuleDef);
            if (moduleProps == null) return false;
            return HostsWithRoom(pawn, moduleProps).Any();
        }

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            if (!ModuleApi.Available) yield break;
            object moduleProps = ModuleApi.ModulePropsOf(ModuleDef);
            if (moduleProps == null) yield break;
            foreach (var host in HostsWithRoom(pawn, moduleProps))
                if (host.Hediff.Part != null)
                    yield return host.Hediff.Part;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            object moduleProps = ModuleApi.ModulePropsOf(ModuleDef);
            if (moduleProps == null) return;

            var hosts = HostsWithRoom(pawn, moduleProps).ToList();
            var chosen = hosts.FirstOrDefault(h => h.Hediff.Part == part);
            if (chosen.Comp == null) chosen = hosts.FirstOrDefault();
            if (chosen.Comp == null)
            {
                Messages.Message("RebalancePatches.NoOpenModuleSlot".Translate(pawn.LabelShort, ModuleDef?.label ?? "module"),
                    pawn, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            ThingWithComps module = ingredients?.OfType<ThingWithComps>().FirstOrDefault(t => t.def == ModuleDef);
            if (module == null) return;
            if (module.stackCount > 1) module = (ThingWithComps)module.SplitOff(1);

            if (!ModuleApi.Install(chosen.Hediff, chosen.Comp, module, chosen.SlotId))
                return;

            if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
        }

        /// <summary>Hosts on this pawn with an open slot for the module, and which slot.</summary>
        private static IEnumerable<(Hediff Hediff, object Comp, string SlotId)> HostsWithRoom(Pawn pawn, object moduleProps)
        {
            foreach (var (hediff, comp) in ModuleApi.ModularHosts(pawn))
            {
                string slot = ModuleApi.FirstOpenSlot(comp, moduleProps);
                if (slot != null)
                    yield return (hediff, comp, slot);
            }
        }
    }
}
