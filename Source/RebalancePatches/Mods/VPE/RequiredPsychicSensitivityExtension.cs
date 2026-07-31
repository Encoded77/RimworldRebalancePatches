using Verse;

namespace RebalancePatches
{
    /// <summary>Apparel carrying this refuses wearers below the given psychic sensitivity.
    /// Checked at equip time only; a wearer whose sensitivity later drops keeps the garment.</summary>
    public class RequiredPsychicSensitivityExtension : DefModExtension
    {
        public float minSensitivity = 1f;
    }
}
