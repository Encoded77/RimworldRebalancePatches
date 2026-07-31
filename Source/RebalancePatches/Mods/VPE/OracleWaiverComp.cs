using Verse;

namespace RebalancePatches
{
    /// <summary>Rides the Echo ORACLE hediff and remembers the one psycast path this brain let its
    /// bearer take against the path's own requirements. Scribed, so the choice survives save/load;
    /// removing the brain discards it with the hediff, and a future ORACLE carries a fresh waiver.</summary>
    public class HediffCompProperties_OracleWaiver : HediffCompProperties
    {
        public HediffCompProperties_OracleWaiver() => compClass = typeof(OracleWaiverComp);
    }

    public class OracleWaiverComp : HediffComp
    {
        public string waivedPath;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref waivedPath, "waivedPath");
        }
    }
}
