using System;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M006-CL wave 1: gives the Nursery a real click-to-open window in HiveMap. In
    // LivingHive the same server-authoritative capability (Feed/Stabilize, see
    // HiveBroodVitalityPanelController) only surfaces through the flat reference-image
    // hotspot detail panel, which HiveMap must never reproduce. This window is new,
    // self-contained IMGUI (not a call into HiveViewProductUiPresenter's Draw*ForExternalHost
    // bridge) that reads/mutates directly through
    // MobileAccountSessionRuntimeBootstrap.BroodVitalityControllerForHiveMap - the same
    // controller instance the monolith itself uses, so state stays consistent regardless of
    // which surface is open.
    //
    // Previously: clicking NURSERY fell through to HiveMapBuildingUpgradeClickBootstrap's
    // generic "no dedicated window" redirect, opening the Construction picker pre-selected
    // to Nursery. That fallback is now excluded for NURSERY (see
    // HiveMapBuildingUpgradeClickBootstrap.ExcludedBuildingTypes) - upgrading the Nursery
    // is still reachable via the "Ameliorer" button inside this window, which opens the
    // exact same Construction picker.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapNurseryBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Nursery Runtime";
        private const float PanelWidth = 360f;
        private const float PanelHeight = 300f;

        public static bool OverlayOpenForExternalHost { get; private set; }

        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapNurseryBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapNurseryBootstrap>();
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
            if (FindFirstObjectByType<HiveMapNurseryBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapNurseryBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;

            if (subscribedController == null)
            {
                BuildingInteractionController controller = FindFirstObjectByType<BuildingInteractionController>();
                if (controller == null) return;
                controller.Selection.BuildingClicked += OnBuildingClicked;
                subscribedController = controller;
            }

            if (OverlayOpenForExternalHost)
            {
                MobileAccountSessionRuntimeBootstrap.BroodVitalityControllerForHiveMap.Refresh();
            }
        }

        private void OnDestroy()
        {
            if (subscribedController != null) subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !string.Equals(building.BuildingType, BuildingTypes.Nursery, StringComparison.Ordinal)) return;
            OverlayOpenForExternalHost = true;
            MobileAccountSessionRuntimeBootstrap.BroodVitalityControllerForHiveMap.Refresh();
        }

        private void OnGUI()
        {
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost) return;
            if (!OverlayOpenForExternalHost) return;

            IHiveBroodVitalityPanelController controller = MobileAccountSessionRuntimeBootstrap.BroodVitalityControllerForHiveMap;
            HiveBroodVitalityScreenModel model = controller.Model;

            Rect panel = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                (Screen.height - PanelHeight) * 0.5f,
                PanelWidth,
                PanelHeight);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 10f, PanelWidth - 24f, PanelHeight - 20f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nurserie", GUI.skin.label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(28f))) OverlayOpenForExternalHost = false;
            GUILayout.EndHorizontal();

            if (!controller.IsConfigured || model.State == HiveBroodVitalityScreenState.NotConfigured)
            {
                GUILayout.Label("Session officielle requise.");
            }
            else if (!model.Initialized)
            {
                GUILayout.Label("Chargement...");
            }
            else
            {
                GUILayout.Space(6f);
                GUILayout.Label("Nutrition : " + model.Nutrition + " / 100");
                DrawBar(model.Nutrition / 100f);
                GUILayout.Label("Stabilite : " + model.Stability + " / 100");
                DrawBar(model.Stability / 100f);

                GUILayout.Space(10f);

                if (model.ActiveOperation != null)
                {
                    TimeSpan remaining = model.Remaining(controller.Elapsed);
                    bool ready = remaining <= TimeSpan.Zero;
                    string kindLabel = string.Equals(model.ActiveOperation.Type, HiveBroodVitalityClient.FeedingType, StringComparison.Ordinal)
                        ? "Nourrir"
                        : "Stabiliser";
                    GUILayout.Label(kindLabel + " en cours - " + (ready ? "pret" : FormatRemaining(remaining)));
                    GUI.enabled = !controller.IsBusy && model.CanComplete(controller.Elapsed);
                    if (GUILayout.Button("Terminer")) controller.Complete();
                    GUI.enabled = true;
                }
                else if (model.IsPending)
                {
                    GUILayout.Label("Commande en attente - session incertaine.");
                    GUI.enabled = !controller.IsBusy;
                    if (GUILayout.Button("Verifier la commande")) controller.Retry();
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = !controller.IsBusy && !model.IsReadOnly && model.CanStart(HiveBroodVitalityClient.FeedingType);
                    if (GUILayout.Button("Nourrir (" + HiveBroodVitalityClient.FeedingHoneyCost + " miel)")) controller.Start(HiveBroodVitalityClient.FeedingType);
                    GUI.enabled = !controller.IsBusy && !model.IsReadOnly && model.CanStart(HiveBroodVitalityClient.StabilizationType);
                    if (GUILayout.Button("Stabiliser (" + HiveBroodVitalityClient.StabilizationWaxCost + " cire)")) controller.Start(HiveBroodVitalityClient.StabilizationType);
                    GUI.enabled = true;
                }

                if (!string.IsNullOrEmpty(model.ErrorCode))
                {
                    GUILayout.Label("Erreur : " + model.ErrorCode);
                }
                if (model.IsReadOnly)
                {
                    GUILayout.Label("Hors ligne - lecture seule.");
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ameliorer"))
            {
                OverlayOpenForExternalHost = false;
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(
                    BuildingMappingTable.GetByBuildingType(BuildingTypes.Nursery).LegacyKey);
            }
            GUILayout.EndArea();
        }

        private static void DrawBar(float fill01)
        {
            Rect track = GUILayoutUtility.GetRect(0f, 12f, GUILayout.ExpandWidth(true));
            GUI.Box(track, string.Empty);
            Rect fillRect = new Rect(track.x + 1f, track.y + 1f, (track.width - 2f) * Mathf.Clamp01(fill01), track.height - 2f);
            GUI.Box(fillRect, string.Empty);
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            return string.Format("{0:00}:{1:00}", (int)remaining.TotalMinutes, remaining.Seconds);
        }
    }
}
