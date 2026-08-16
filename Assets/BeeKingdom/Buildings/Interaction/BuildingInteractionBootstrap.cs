using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BeeKingdom.Buildings.Interaction
{
    [Serializable]
    internal sealed class SidecarSaveFile
    {
        public SidecarSaveEntry[] placements;
    }

    [Serializable]
    internal sealed class SidecarSaveEntry
    {
        public string buildingId;
        public string buildingType;
        public float X;
        public float TerrainY;
        public float Z;
        public float Rotation;
        public float Scale;
    }

    public sealed class BuildingInteractionBootstrap : MonoBehaviour
    {
        private const string BootstrapRootName = "BeeKingdom BuildingInteraction Runtime";
        private const string RelativeSidecarPath = "Assets/Experiments/Environment2D5D/Config/BuildingPlacementEditor_Saves.json";

        [SerializeField] private string _sidecarRelativePath = RelativeSidecarPath;
        [SerializeField] private bool _materializeRuntimeHitZones = true;

        private static bool _bootstrapped;

        public static bool IsBootstrapped
        {
            get { return _bootstrapped; }
        }

        public static void ResetForTests()
        {
            _bootstrapped = false;
        }

        private void Awake()
        {
            if (_bootstrapped)
            {
                Destroy(gameObject);
                return;
            }
            _bootstrapped = true;

            BuildingInteractionController controller = GetComponent<BuildingInteractionController>();
            if (controller == null) controller = gameObject.AddComponent<BuildingInteractionController>();

            TryScanExistingMarkers(controller.Registry);
            if (_materializeRuntimeHitZones)
                MaterializeRuntimeHitZones(controller.Registry);
        }

        public static int RunOnceOnCurrentScene(BuildingInteractionRegistry registry)
        {
            _bootstrapped = false;
            int total = 0;
            total += TryScanExistingMarkers(registry);
            total += MaterializeRuntimeHitZonesFromPath(registry, null);
            _bootstrapped = true;
            return total;
        }

        public static int TryScanExistingMarkers(BuildingInteractionRegistry registry)
        {
            int count = 0;
            foreach (UnityEngine.SceneManagement.Scene scene in UnityEngine.SceneManagement.SceneManager.GetAllScenes())
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    count += ScanGameObject(root, registry);
                }
            }
            return count;
        }

        private static int ScanGameObject(GameObject go, BuildingInteractionRegistry registry)
        {
            int count = 0;
            BuildingInteractionComponent[] markers = go.GetComponentsInChildren<BuildingInteractionComponent>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                BuildingInteractionComponent marker = markers[i];
                if (string.IsNullOrEmpty(marker.BuildingType)) continue;
                registry.Register(marker.gameObject, marker.BuildingType);
                count++;
            }
            return count;
        }

        public static int MaterializeRuntimeHitZones(BuildingInteractionRegistry registry)
        {
            return MaterializeRuntimeHitZonesFromPath(registry, null);
        }

        public static int MaterializeRuntimeHitZonesFromPath(BuildingInteractionRegistry registry, string explicitRelativePath)
        {
            string dataPath = Application.dataPath;
            string relative = string.IsNullOrEmpty(explicitRelativePath)
                ? RelativeSidecarPath
                : explicitRelativePath;
            if (relative.StartsWith("Assets/")) relative = relative.Substring("Assets/".Length);

            string fullPath = Path.Combine(dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
            string json = null;
            try
            {
                if (File.Exists(fullPath)) json = File.ReadAllText(fullPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildingInteractionBootstrap] Lecture sidecar impossible : " + e.Message);
            }

            if (json == null) return 0;

            SidecarSaveFile save = JsonUtility.FromJson<SidecarSaveFile>(json);
            if (save == null || save.placements == null) return 0;

            int count = 0;
            for (int i = 0; i < save.placements.Length; i++)
            {
                SidecarSaveEntry entry = save.placements[i];
                if (entry == null || string.IsNullOrEmpty(entry.buildingType)) continue;

                string buildingType = EntryToType(entry.buildingType);
                if (buildingType == null) continue;

                Vector3 position = new Vector3(entry.X, entry.TerrainY, entry.Z);
                Vector3 localScale = new Vector3(
                    entry.Scale > 0f ? entry.Scale : 1f,
                    1f,
                    entry.Scale > 0f ? entry.Scale : 1f);

                GameObject hit = new GameObject("RuntimeHit_" + buildingType);
                hit.transform.position = position;
                hit.transform.localScale = localScale;
                hit.transform.Rotate(0f, entry.Rotation, 0f, Space.World);

                BoxCollider collider = hit.AddComponent<BoxCollider>();
                collider.size = new Vector3(1f, 1.4f, 1f);

                BuildingInteractionComponent interaction = hit.AddComponent<BuildingInteractionComponent>();
                interaction.Configure(buildingType);

                registry.Register(hit, buildingType);
                count++;
            }
            return count;
        }

        private static string EntryToType(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            string upper = raw.Trim().ToUpperInvariant();
            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                if (string.Equals(BuildingTypes.All[i], upper, StringComparison.Ordinal))
                    return BuildingTypes.All[i];
            }
            return null;
        }
    }
}