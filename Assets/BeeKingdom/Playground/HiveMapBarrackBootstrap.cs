using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Opens HiveViewProductUiPresenter's Barrack training panel (Soldats/Gardiennes/
    // Eclaireuses, "guard_post" hotspot id - see BuildingMappingTable: BARRACK maps to
    // BuildingLegacyKeys.GuardPost) when the player clicks the BARRACK building. In
    // LivingHive this UI lived inside the reference-image hotspot detail panel; HiveMap's
    // 2.5D buildings have no equivalent, so the building click is the trigger here instead,
    // same as HiveMapAllianceBootstrap for ALLIANCE_CENTER.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapBarrackBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Barrack Runtime";

        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapBarrackBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapBarrackBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            HiveViewProductUiPresenter.TickBarrackTrainingForExternalHost();

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
            if (building == null || !string.Equals(building.BuildingType, BuildingTypes.Barrack, StringComparison.Ordinal)) return;
            // Jeff's request: tapping the Barrack while troops are ready claims them
            // directly instead of opening the full window - only falls through to opening
            // the panel when there's nothing ready to claim.
            if (HiveViewProductUiPresenter.TryClaimReadyTrainingOnTapForExternalHost()) return;
            HiveViewProductUiPresenter.OpenBarrackOverlayForExternalHost();
        }

        private void OnGUI()
        {
            bool compact = Screen.width < 900;
            HiveViewProductUiPresenter.DrawBarrackOverlayForExternalHost(compact);
            // Same OnGUI call as the panel it opens from, so it always draws on top of it -
            // see the identical comment in HiveMapConstructionBootstrap.
            HiveViewProductUiPresenter.DrawSpeedUpOverlayForExternalHost(compact);

            // "Ready to claim" badge on the Barrack itself, visible even while the panel is
            // closed - only meaningful when the player hasn't already opened the window
            // (which shows its own status), and only once the building's runtime GameObject
            // and camera are actually available.
            if (!HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost && subscribedController != null)
            {
                Camera camera = Camera.main;
                GameObject go = subscribedController.Registry.GetGameObjectByBuildingType(BuildingTypes.Barrack);
                if (camera != null && go != null)
                {
                    Rect rect = ScreenRectFor(go, camera);
                    if (rect.width > 0f) HiveViewProductUiPresenter.DrawTrainingReadyBadgeForExternalHost(rect, Time.unscaledTime);
                }
            }
        }

        // Same collider-bounds screen projection used by the other HiveMap bootstraps
        // (HiveMapProductionBootstrap, HiveMapBuildingUpgradeVisualStateBootstrap, etc.).
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

            float guiY = Screen.height - centerScreen.y;
            return new Rect(centerScreen.x - halfWidth, guiY - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }
    }
}
