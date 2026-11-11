using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M006-CL wave 1: adds a small "i" button next to Honey Reserve and Warehouse showing
    // the official server-backed pending/rate/capacity forecast (HiveOfflineProductionPanelController
    // - the same controller/data LivingHive's "Mobile production forecast" screen already
    // reads, see Docs/Demos/LivingHive.md). Read-only: this never collects anything, so the
    // existing manual-collection tap-to-collect flow owned by HiveMapProductionBootstrap
    // (badge, zoom-derived sizing, bee-swirl feedback, resource mutation) is completely
    // untouched - the "i" button is a separate small GUI.Button hit-tested by OnGUI itself,
    // never reaching BuildingInteractionController's 3D raycast or the building's own
    // BuildingClicked event.
    //
    // M008-CX wave 2: extends the same read-only forecast to Transformation, the third
    // manual-production building, without touching HiveMapProductionBootstrap's
    // tap-to-collect/badge/feedback behavior.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed partial class HiveMapProductionInfoBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Production Info Runtime";
        private const float PanelWidth = 300f;
        private const float PanelHeight = 170f;

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
        private GUIStyle cachedInfoBadgeStyle;

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
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
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

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !IsProducerBuildingType(building.BuildingType)) return;
            // A click on a producer opens a visible premium panel even when
            // nothing is ready to collect. The existing collection stays in
            // ProductionBootstrap and the new info view never credits stock.
            OpenPremiumInfo(building.BuildingType);
#if UNITY_EDITOR
            Debug.Log("[BK-ProducerInfo] Normal building click: " +
                building.BuildingType + " shown=" + OverlayOpenForExternalHost);
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
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (OverlayOpenForExternalHost)
            {
                // Do not dismiss an already opened producer panel on
                // subsequent IMGUI Layout/Repaint callbacks. The opening
                // request checked the other overlays beforehand.
                // The full-screen, retained-mode Canvas now owns the rendering.
                // No second legacy IMGUI popup or 3D hit zones may appear above it.
                if (Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.Escape)
                {
                    ClosePremiumInfo();
                    Event.current.Use();
                }
                return;
            }
            if (HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()) return;
            if (subscribedController == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            for (int i = 0; i < TrackedBuildingTypes.Length; i++)
            {
                string type = TrackedBuildingTypes[i];
                // The runtime registry can be incomplete while resources load.
                if (!subscribedController.Registry.TryGetGameObjectByBuildingType(
                    type, out GameObject go) || go == null) continue;
                Rect bounds = ScreenRectFor(go, camera);
                if (bounds.width <= 0f) continue;

                Rect target = ProducerInfoBadgeRectForProof(bounds,
                    Screen.width, Screen.height, HeaderBottom());
                Color tint = GUI.color;
                GUI.color = new Color(.98f,.73f,.26f,.97f);
                GUI.DrawTexture(target, Texture2D.whiteTexture);
                GUI.color = tint;
                if (cachedInfoBadgeStyle == null)
                {
                    cachedInfoBadgeStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 22,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(.12f,.075f,.035f,1f) }
                    };
                }
                GUI.Label(target,"i",cachedInfoBadgeStyle);
                if (GUI.Button(target, GUIContent.none, GUIStyle.none))
                {
                    OpenPremiumInfo(type);
                    return;
                }
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
