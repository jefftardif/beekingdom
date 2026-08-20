using System;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Ports LivingHive's manual-production collection (accumulate over time, click to
    // collect, bee-swirl + ready glow) onto the 3 HiveMap production buildings: Honey
    // Reserve, Warehouse, Transformation ("Réserve de miel" / "Entrepôt" / "Atelier de
    // Cire" in LivingHive). Game logic is reused as-is from HiveViewProductUiPresenter's
    // local-preview manual production system (see its "Manual production" external-host
    // bridge region) - this bootstrap only ticks it every frame, forwards building clicks
    // to it, and renders the bee-swirl at each building's actual on-screen rect (computed
    // from the same camera BuildingPerspectiveCamera uses), since the original rendering is
    // hardwired to the flat reference-image coordinate space HiveMap doesn't use.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapProductionBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Production Runtime";

        private static readonly string[] TrackedBuildingTypes =
        {
            BuildingTypes.HoneyReserve,
            BuildingTypes.Warehouse,
            BuildingTypes.Transformation
        };

        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapProductionBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapProductionBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            HiveViewProductUiPresenter.TickManualProductionForExternalHost(Time.unscaledDeltaTime);

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
            if (LivingHiveResearchRuntime.IsModalOpen || HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost) return;
            if (building == null || !IsTrackedBuildingType(building.BuildingType)) return;
            string hotspotId = BuildingMappingTable.GetByBuildingType(building.BuildingType).LegacyKey;
            HiveViewProductUiPresenter.CollectManualProductionForExternalHost(hotspotId);
        }

        private static bool IsTrackedBuildingType(string buildingType)
        {
            for (int i = 0; i < TrackedBuildingTypes.Length; i++)
            {
                if (string.Equals(TrackedBuildingTypes[i], buildingType, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (LivingHiveResearchRuntime.IsModalOpen || HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost) return;
            if (subscribedController == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            float time = Time.unscaledTime;

            // World-space badge size converted to screen pixels via the orthographic
            // camera's current zoom, computed once and shared by all 3 buildings - Jeff
            // (2026-08-19): a size fixed in screen pixels looked right at one zoom level
            // but didn't rescale as the player zoomed in/out, and sizing it per-building
            // from each building's own on-screen rect made the 3 badges visibly different
            // sizes (the 3 production buildings sit at different screen depths in the
            // isometric view). A single zoom-derived size fixes both at once.
            const float BadgeWorldSize = 10.8f; // Jeff (2026-08-19): ~10% smaller than the original 12f
            float pixelsPerWorldUnit = camera.orthographic && camera.orthographicSize > 0.001f
                ? Screen.height / (2f * camera.orthographicSize)
                : 11.76f;
            float badgeGlowSize = Mathf.Clamp(BadgeWorldSize * pixelsPerWorldUnit, 40f, 220f);

            for (int i = 0; i < TrackedBuildingTypes.Length; i++)
            {
                string buildingType = TrackedBuildingTypes[i];
                GameObject go = subscribedController.Registry.GetGameObjectByBuildingType(buildingType);
                if (go == null) continue;

                Rect rect = ScreenRectFor(go, camera);
                if (rect.width <= 0f) continue;

                string hotspotId = BuildingMappingTable.GetByBuildingType(buildingType).LegacyKey;
                HiveViewProductUiPresenter.DrawManualProductionBeesForExternalHost(rect, hotspotId, time, badgeGlowSize);
                HiveViewProductUiPresenter.DrawManualCollectionFeedbackForExternalHost(rect, hotspotId);
            }
        }

        // Reuses the building's own click-collider bounds (BoxCollider set up by
        // BuildingRuntimeViewBootstrap around the artwork quad) rather than re-deriving a
        // size from the artwork - this is the same box the player already clicks.
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
