using System;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    /// <summary>
    /// Every BodyDef with its resolved part tree, every BodyPartDef, and every race's body. This is
    /// what turns "this surgery targets a Shoulder" into "these twelve modded humanlike races have
    /// no Shoulder, so they can never receive it" — the reverse-mismatch audit, done as an offline
    /// join. The race list matters as much as the bodies: without it an analyzer cannot tell a real
    /// xenotype gap from an animal that legitimately lacks the part.
    ///
    /// The part tree is resolved rather than structural: BodyDef.AllParts flattens the corePart
    /// hierarchy the game builds at load, which XML alone does not show.
    /// </summary>
    internal static class BodyDump
    {
        private static readonly Type[] Referenced =
        {
            typeof(BodyPartDef), typeof(BodyPartGroupDef), typeof(BodyPartTagDef),
        };

        [DebugAction("RebalancePatches", "Dump bodies", allowedGameStates = AllowedGameStates.Entry)]
        private static void DumpEntry() => Dump();

        [DebugAction("RebalancePatches", "Dump bodies", allowedGameStates = AllowedGameStates.Playing)]
        internal static void Dump()
        {
            var walker = new DefWalker(Referenced, bareDefTypes: new[] { typeof(BodyPartDef) });
            int bodies = 0, parts = 0, races = 0;

            DumpRunner.Run("BodyDump.json", walker, w =>
            {
                Json j = w.Json;

                j.Name("bodies");
                j.BeginArray();
                foreach (BodyDef body in DefDatabase<BodyDef>.AllDefsListForReading
                             .OrderBy(b => b.defName, StringComparer.Ordinal))
                {
                    j.BeginObject();
                    j.Name("defName"); j.Value(body.defName);
                    j.Name("label"); j.Value(body.label);
                    j.Name("mod"); j.Value(DumpRunner.ModOf(body));
                    j.Name("corePart"); j.Value(body.corePart?.def?.defName);

                    // Flattened parts with depth and parent, so the analyzer can both test presence
                    // cheaply and reconstruct the hierarchy when it needs to.
                    j.Name("parts");
                    j.BeginArray();
                    foreach (BodyPartRecord part in body.AllParts)
                    {
                        if (part?.def == null) continue;
                        j.BeginObject();
                        j.Name("part"); j.Value(part.def.defName);
                        j.Name("label"); j.Value(part.Label);
                        j.Name("parent"); j.Value(part.parent?.def?.defName);
                        j.Name("depth"); j.Number(Depth(part));
                        j.Name("coverage"); j.Number(part.coverage);
                        j.Name("groups");
                        j.BeginArray();
                        if (part.groups != null)
                            foreach (BodyPartGroupDef group in part.groups)
                                if (group != null) j.Value(group.defName);
                        j.EndArray();
                        j.EndObject();
                    }
                    j.EndArray();

                    j.Name("partNames");
                    j.BeginArray();
                    foreach (string name in body.AllParts.Where(p => p?.def != null)
                                 .Select(p => p.def.defName).Distinct().OrderBy(s => s, StringComparer.Ordinal))
                        j.Value(name);
                    j.EndArray();

                    j.EndObject();
                    bodies++;
                }
                j.EndArray();

                j.Name("bodyParts");
                j.BeginArray();
                foreach (BodyPartDef part in DefDatabase<BodyPartDef>.AllDefsListForReading
                             .OrderBy(p => p.defName, StringComparer.Ordinal))
                {
                    w.WriteDefEntry(part);
                    parts++;
                }
                j.EndArray();

                // Race → body, so an analyzer can tell which bodies belong to humanlikes. Without
                // this, "no body can receive this surgery" cannot distinguish a real xenotype gap
                // from an animal legitimately lacking the part. Pawn ThingDefs are excluded from
                // ThingDump, so this is the only place the join is available.
                j.Name("pawns");
                j.BeginArray();
                foreach (ThingDef race in DefDatabase<ThingDef>.AllDefsListForReading
                             .Where(t => t.race != null)
                             .OrderBy(t => t.defName, StringComparer.Ordinal))
                {
                    j.BeginObject();
                    j.Name("defName"); j.Value(race.defName);
                    j.Name("label"); j.Value(race.label);
                    j.Name("mod"); j.Value(DumpRunner.ModOf(race));
                    j.Name("body"); j.Value(race.race.body?.defName);
                    j.Name("humanlike"); j.Value(race.race.Humanlike);
                    j.Name("intelligence"); j.Value(race.race.intelligence.ToString());
                    j.Name("animal"); j.Value(race.race.Animal);
                    j.Name("isMechanoid"); j.Value(race.race.IsMechanoid);
                    j.EndObject();
                    races++;
                }
                j.EndArray();
            }, () => $"{bodies} bodies, {parts} body parts, {races} races");
        }

        private static int Depth(BodyPartRecord part)
        {
            int depth = 0;
            for (BodyPartRecord p = part.parent; p != null; p = p.parent) depth++;
            return depth;
        }
    }
}
