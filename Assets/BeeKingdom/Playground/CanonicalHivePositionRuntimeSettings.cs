using UnityEngine;

namespace BeeKingdom.Playground
{
    // Candidate only. No asset created or feature enabled in the active project.
    public sealed class CanonicalHivePositionRuntimeSettings : ScriptableObject
    {
        public bool Enabled = false;
        public string WorldId = "";
        public string SurfaceId = "";
        public string CoordinateUnit = "";
    }
}
