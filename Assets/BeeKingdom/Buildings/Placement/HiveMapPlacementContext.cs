using UnityEngine;

namespace BeeKingdom.Buildings.Placement
{
    [AddComponentMenu("Bee Kingdom/Hive Map Placement Context")]
    public sealed class HiveMapPlacementContext : MonoBehaviour
    {
        [Header("Contexte de placement HiveMap")]
        [Tooltip("Chemin relatif au sidecar de placement pour CE contexte")]
        public string sidecarPath = "Assets/Experiments/Environment2D5D/Config/BuildingPlacementEditor_HiveMap_Saves.json";

        [Tooltip("Si true, BuildingPlacementLayoutIO consulte BuildingPlaceholderLayout_FINAL comme fallback. Si false, aucune ancienne position n'est utilisée.")]
        public bool useOfficialLayoutFallback = false;

        [Header("Backdrop (runtime)")]
        public Texture2D backdropTexture;

        [Header("Identification")]
        public string layoutName = "HiveMap";
    }
}