using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Lets the player click directly on any building that has no dedicated window of its
    // own (Defense, Genetics, Infirmary, Academy, Bank, Royal Palace, Champion Hall) to
    // open the generic Construction picker pre-selected to that building, instead of
    // leaving the click with no UI feedback at all. Buildings that already have their own
    // window/action (Barrack, Alliance Center, the 3 production buildings, Research,
    // Nursery as of M006-CL wave 1) are excluded here - clicking those keeps its existing
    // meaning (open the window / collect / etc), and upgrading them happens via the
    // "Ameliorer" button inside their own window, same as the Construction picker's own
    // button for everything else.
    //
    // Also draws the prerequisite glow (see HiveViewProductUiPresenter's
    // HighlightedPrerequisiteBuildingTypeForExternalHost) on whichever building the player
    // is being redirected to (currently always ROYAL_PALACE, the only real upgrade
    // prerequisite in this system) - reuses the same on-screen rect computation as
    // HiveMapProductionBootstrap.ScreenRectFor.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapBuildingUpgradeClickBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Building Upgrade Click Runtime";

        private static readonly string[] ExcludedBuildingTypes =
        {
            BuildingTypes.Barrack,
            BuildingTypes.AllianceCenter,
            BuildingTypes.HoneyReserve,
            BuildingTypes.Warehouse,
            BuildingTypes.Transformation,
            BuildingTypes.Research,
            // M006-CL wave 1: Nursery now has its own window (HiveMapNurseryBootstrap),
            // which includes its own "Ameliorer" button routing to this same Construction
            // picker - no longer needs the generic no-window fallback.
            BuildingTypes.Nursery,
        };

        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapBuildingUpgradeClickBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapBuildingUpgradeClickBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Update()
        {
            if (subscribedController != null) return;
            BuildingInteractionController controller = FindFirstObjectByType<BuildingInteractionController>();
            if (controller == null) return;
            controller.Selection.BuildingClicked += OnBuildingClicked;
            subscribedController = controller;
        }

        private void OnDestroy()
        {
            if (subscribedController != null) subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || IsExcluded(building.BuildingType)) return;
            string hotspotId = BuildingMappingTable.GetByBuildingType(building.BuildingType).LegacyKey;
            // These 8 building types have no dedicated window of their own - open the
            // Construction picker pre-selected to this building instead of silently
            // attempting an upgrade with no UI feedback. The picker's own "Ameliorer"
            // button still goes through TryStartUpgradeWithPrerequisiteRedirectForExternalHost.
            HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(hotspotId);
        }

        private static bool IsExcluded(string buildingType)
        {
            for (int i = 0; i < ExcludedBuildingTypes.Length; i++)
            {
                if (string.Equals(ExcludedBuildingTypes[i], buildingType, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            string highlightedType = HiveViewProductUiPresenter.HighlightedPrerequisiteBuildingTypeForExternalHost;
            if (string.IsNullOrEmpty(highlightedType) || subscribedController == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            GameObject go = subscribedController.Registry.GetGameObjectByBuildingType(highlightedType);
            if (go == null) return;
            Rect rect = ScreenRectFor(go, camera);
            if (rect.width <= 0f) return;
            HiveViewProductUiPresenter.DrawPrerequisiteGlowForExternalHost(rect, Time.unscaledTime);
        }

        // Same collider-bounds screen projection as HiveMapProductionBootstrap.ScreenRectFor.
        private static Rect ScreenRectFor(GameObject go, Camera camera)
        {
            Collider collider = go.GetComponent<Collider>();
            Bounds bounds = collider != null ? collider.bounds : default;
            if (collider == null)
            {
                Renderer renderer = go.GetComponentInChildren<Renderer>();
                if (renderer == null) return default;
                bounds = renderer.bounds;
            }

            Vector3 centerScreen = camera.WorldToScreenPoint(bounds.center);
            if (centerScreen.z <= 0f) return default;

            Vector3 topScreen = camera.WorldToScreenPoint(bounds.center + new Vector3(0f, bounds.extents.y, 0f));
            Vector3 rightScreen = camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, 0f, 0f));
            float halfHeight = Mathf.Abs(topScreen.y - centerScreen.y);
            float halfWidth = Mathf.Abs(rightScreen.x - centerScreen.x);
            if (halfWidth <= 0f || halfHeight <= 0f) return default;

            // Unity screen space is bottom-left origin; IMGUI is top-left - flip Y.
            float guiY = Screen.height - centerScreen.y;
            return new Rect(centerScreen.x - halfWidth, guiY - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }
    }
}
