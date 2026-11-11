using System;
using System.Collections.Generic;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Unified premium interaction for Honey Reserve, Warehouse and Wax
    // Workshop. A building tap harvests only if the existing authoritative
    // production model says stock is collectible. A successful dispatch
    // makes the NEXT click on that building open the production details.
    // If there is nothing to harvest, the first click opens details.
    // The former floating "i" and its independent IMGUI input have been
    // retired. The read-only details never credit inventory themselves.
    //
    // Same runtime bootstrap strategy as other Environment2D5D services.
    public sealed partial class HiveMapProductionInfoBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Production Info Runtime";

        private static readonly string[] TrackedBuildingTypes =
        {
            BuildingTypes.HoneyReserve,
            BuildingTypes.Warehouse,
            BuildingTypes.Transformation
        };

        public static bool OverlayOpenForExternalHost { get; private set; }

        private BuildingInteractionController subscribedController;
        private string openBuildingType;
        private HiveMapPremiumBuildingWindow premiumWindow;
        // One deterministic click sequence per producer, even when its
        // server snapshot remains collectible during a pending request.
        private readonly HashSet<string> showDetailsAfterSuccessfulHarvest =
            new HashSet<string>(StringComparer.Ordinal);
        private int lastProducerClickFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapProductionInfoBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapProductionInfoBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (FindFirstObjectByType<HiveMapProductionInfoBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapProductionInfoBootstrap>();
        }

        private bool producerCompetingOverlayNoted;

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost)
            {
                showDetailsAfterSuccessfulHarvest.Clear();
                return;
            }
            if (OverlayOpenForExternalHost && premiumWindow != null)
            {
                // A producer info window already owns the modal gesture.
                // Previously another temporary modal flag could instantly
                // CLOSE this screen after ShowProductionInfo succeeded, which
                // is why logs said shown=True but no window ever appeared.
                // Other screens cannot silently revoke this user's choice;
                // only Back/Upgrade/Escape may call ClosePremiumInfo().
                bool competingOverlay =
                    HiveMapOverlayInputGateBootstrap.IsAnotherOverlayBlockingProductionInfo();
                if (competingOverlay && !producerCompetingOverlayNoted)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[BK-ProducerInfo] Transient competing overlay; " +
                        "keeping production panel in foreground");
#endif
                    producerCompetingOverlayNoted = true;
                }
                if (!competingOverlay) producerCompetingOverlayNoted = false;
                premiumWindow.SetSuppressed(false);
            }
            if (subscribedController != null) return;
            BuildingInteractionController controller = FindFirstObjectByType<BuildingInteractionController>();
            if (controller == null) return;
            controller.Selection.BuildingClicked += OnBuildingClicked;
            subscribedController = controller;
        }

        private static bool IsProducerBuildingType(string type)
        {
            return type == BuildingTypes.HoneyReserve ||
                   type == BuildingTypes.Warehouse ||
                   type == BuildingTypes.Transformation;
        }

        public static bool ShouldHarvestOnFirstClickForProof(
            bool collectable, bool detailsPending)
        {
            return collectable && !detailsPending;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !IsProducerBuildingType(building.BuildingType))
                return;
            // Both the building collider and artwork fallback may observe
            // the same frame. Never process one tap twice.
            if (lastProducerClickFrame == Time.frameCount) return;
            lastProducerClickFrame = Time.frameCount;
            if (OverlayOpenForExternalHost) return;

            string type = building.BuildingType;
            if (showDetailsAfterSuccessfulHarvest.Contains(type))
            {
                OpenPremiumInfo(type);
                if (OverlayOpenForExternalHost)
                    showDetailsAfterSuccessfulHarvest.Remove(type);
#if UNITY_EDITOR
                Debug.Log("[BK-ProducerInfo] Follow-up click opens " + type +
                    " shown=" + OverlayOpenForExternalHost);
#endif
                return;
            }

            string hotspotId=BuildingMappingTable.GetByBuildingType(type).LegacyKey;
            bool ready=HiveViewProductUiPresenter.ManualProductionReadyForExternalHost(
                hotspotId);
            if (ShouldHarvestOnFirstClickForProof(ready, false))
            {
                // The existing collection bridge uses the official server
                // when authenticated and the local preview only in demo.
                // No second callback is allowed to start another mutation.
                float accepted=HiveViewProductUiPresenter
                    .CollectManualProductionForExternalHost(hotspotId);
                if (accepted > 0f)
                {
                    showDetailsAfterSuccessfulHarvest.Add(type);
#if UNITY_EDITOR
                    Debug.Log("[BK-ProducerInfo] First click harvest dispatched: " +
                        type + " (next click shows details)");
#endif
                    return;
                }
            }

            // No authoritative harvest available (or it was rejected):
            // one click opens the premium details, with honest server status.
            OpenPremiumInfo(type);
#if UNITY_EDITOR
            Debug.Log("[BK-ProducerInfo] No harvest available, open details: " +
                type + " shown=" + OverlayOpenForExternalHost);
#endif
        }
        private void OnDestroy()
        {
            if (subscribedController != null)
                subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
            if (premiumWindow != null) premiumWindow.Hide();
            OverlayOpenForExternalHost = false;
            openBuildingType = null;
        }

        private void OpenPremiumInfo(string buildingType)
        {
            if (HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking())
            {
#if UNITY_EDITOR
                Debug.Log("[BK-ProducerInfo] Open withheld by active modal for " +
                    buildingType);
#endif
                return;
            }
            if (!BuildingCatalog.TryGetByBuildingType(buildingType, out BuildingDefinition building))
                return;
            var source = MobileAccountSessionRuntimeBootstrap.OfflineProductionControllerForHiveMap;
            if (premiumWindow == null)
            {
                GameObject root = new GameObject("Production Info Premium Screen");
                root.transform.SetParent(transform, false);
                premiumWindow = root.AddComponent<HiveMapPremiumBuildingWindow>();
            }
            openBuildingType = buildingType;
            OverlayOpenForExternalHost = true;
            premiumWindow.ShowProductionInfo(building,
                BuildingName(buildingType, true),
                BuildingName(buildingType, false),
                source, ClosePremiumInfo, UpgradeFromPremiumInfo);
            if (source != null && source.IsConfigured && !source.IsBusy)
                source.Refresh();
#if UNITY_EDITOR
            Canvas panelCanvas = premiumWindow.GetComponentInChildren<Canvas>(true);
            Debug.Log("[BK-ProducerInfo] Premium canvas opened: " + buildingType +
                " isOpen=" + premiumWindow.IsOpen +
                " producerMode=" + premiumWindow.ProductionInfoModeForProof +
                " canvasActive=" + (panelCanvas != null && panelCanvas.gameObject.activeInHierarchy));
#endif
        }

        private static string BuildingName(string type, bool french)
        {
            if (type == BuildingTypes.HoneyReserve)
                return french ? "Réserve de miel" : "Honey Reserve";
            if (type == BuildingTypes.Warehouse)
                return french ? "Entrepôt" : "Warehouse";
            if (type == BuildingTypes.Transformation)
                return french ? "Atelier de cire" : "Wax Workshop";
            return french ? "Bâtiment" : "Building";
        }

        private void ClosePremiumInfo()
        {
            if (!OverlayOpenForExternalHost) return;
            // A closing tap still belongs to this window, not the hive below.
            HiveMapOverlayInputGateBootstrap.CaptureClosingGesture();
            OverlayOpenForExternalHost = false;
            openBuildingType = null;
            producerCompetingOverlayNoted = false;
            if (premiumWindow != null) premiumWindow.Hide();
        }

        private void UpgradeFromPremiumInfo()
        {
            string type = openBuildingType;
            ClosePremiumInfo();
            if (BuildingMappingTable.TryGetByBuildingType(type,
                out LegacyMappingEntry mapped))
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(
                    mapped.LegacyKey);
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost ||
                !OverlayOpenForExternalHost) return;
            // The building itself is the only entry point; no floating "i"
            // buttons. Only Escape remains handled by this legacy GUI path.
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Escape)
            {
                ClosePremiumInfo();
                Event.current.Use();
            }
        }
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
