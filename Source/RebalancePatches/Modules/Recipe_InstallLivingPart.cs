using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RebalancePatches
{
    public class Recipe_InstallLivingPart : Recipe_InstallArtificialBodyPart
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            foreach (BodyPartRecord part in base.GetPartsToApplyOn(pawn, recipe))
                if (part?.def != null && part.def.alive)
                    yield return part;
        }
    }
}
