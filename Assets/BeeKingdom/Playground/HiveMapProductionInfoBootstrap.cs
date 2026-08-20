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
    public sealed class HiveMapProductionInfoBootstrap : MonoBehaviour
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

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (subscribedController != null) return;
            BuildingInteractionController controller = FindFirstObjectByType<BuildingInteractionController>();
            if (controller == null) return;
            subscribedController = controller;
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (subscribedController == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            for (int i = 0; i < TrackedBuildingTypes.Length; i++)
            {
                string buildingType = TrackedBuildingTypes[i];
                GameObject go = subscribedController.Registry.GetGameObjectByBuildingType(buildingType);
                if (go == null) continue;

                Rect rect = ScreenRectFor(go, camera);
                if (rect.width <= 0f) continue;

                // Small "i" button anchored to the building's own screen rect, offset to its
                // top-right corner so it never sits on top of the collection badge (which is
                // centered on the same rect by HiveMapProductionBootstrap).
                Rect infoButton = new Rect(rect.xMax - 4f, rect.yMin - 4f, 26f, 26f);
                if (GUI.Button(infoButton, "i"))
                {
                    OverlayOpenForExternalHost = true;
                    openBuildingType = buildingType;
                    MobileAccountSessionRuntimeBootstrap.OfflineProductionControllerForHiveMap.Refresh();
                }
            }

            if (OverlayOpenForExternalHost) DrawInfoPanel();
        }

        private void DrawInfoPanel()
        {
            IHiveOfflineProductionPanelController controller = MobileAccountSessionRuntimeBootstrap.OfflineProductionControllerForHiveMap;
            HiveOfflineProductionScreenModel model = controller.Model;
            string hotspotId = openBuildingType == null
                ? null
                : BuildingMappingTable.GetByBuildingType(openBuildingType).LegacyKey;
            HiveOfflineProductionLineModel line = model == null || hotspotId == null ? null : model.FindLine(hotspotId);

            Rect panel = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                (Screen.height - PanelHeight) * 0.5f,
                PanelWidth,
                PanelHeight);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 10f, PanelWidth - 24f, PanelHeight - 20f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(BuildingLabelFor(openBuildingType));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(28f))) OverlayOpenForExternalHost = false;
            GUILayout.EndHorizontal();

            if (!controller.IsConfigured || model == null || model.State == HiveOfflineProductionScreenState.NotConfigured)
            {
                GUILayout.Label("Session officielle requise.");
            }
            else if (line == null)
            {
                GUILayout.Label("Chargement...");
            }
            else
            {
                GUILayout.Space(6f);
                GUILayout.Label("Ressource : " + line.ResourceKey);
                GUILayout.Label("En attente : " + line.PendingAmount.ToString("0"));
                GUILayout.Label("Taux : " + line.HourlyRate.ToString("0.0") + " / h");
                GUILayout.Label("Reserve : " + line.BalanceAmount + " / " + line.BalanceCapacity);
                if (line.IsResourceCapacityFull) GUILayout.Label("Capacite pleine.");
                if (model.IsReadOnly) GUILayout.Label("Hors ligne - lecture seule.");
            }

            GUILayout.EndArea();
        }

        private static string BuildingLabelFor(string buildingType)
        {
            if (string.Equals(buildingType, BuildingTypes.HoneyReserve, StringComparison.Ordinal)) return "Reserve de miel";
            if (string.Equals(buildingType, BuildingTypes.Warehouse, StringComparison.Ordinal)) return "Entrepot";
            if (string.Equals(buildingType, BuildingTypes.Transformation, StringComparison.Ordinal)) return "Transformation";
            return "Batiment";
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

            float guiY = Screen.height - centerScreen.y;
            return new Rect(centerScreen.x - halfWidth, guiY - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }
    }
}
