using UnityEngine;

namespace BeeKingdom.Networking
{
    [CreateAssetMenu(fileName="AllianceTerritoryRuntime",menuName="Bee Kingdom/Networking/Alliance Territory Runtime")]
    public sealed class AllianceTerritoryRuntimeSettings : ScriptableObject
    {
        public bool Enabled;
        public bool HeadquartersDetailsEnabled; // Separate opt-in; false by default.
        public string WorldId;
        public string SurfaceId;
        public string CoordinateUnit;
    }
}
