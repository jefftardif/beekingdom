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
        // Shared Activities-v4 fullscreen view replaces the legacy 360x300 IMGUI popup.

        public static bool OverlayOpenForExternalHost { get; private set; }

        private BuildingInteractionController subscribedController;
        private HiveMapPremiumBuildingWindow premiumWindow;

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
            if (OverlayOpenForExternalHost && premiumWindow != null)
                premiumWindow.SetSuppressed(
                    HiveMapActivitiesBootstrap.ModalOpenForExternalHost ||
                    HiveMapArmyBootstrap.ModalOpenForExternalHost);
        }

        private void OnDestroy()
        {
            OverlayOpenForExternalHost = false;
            if (subscribedController != null)
                subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !string.Equals(building.BuildingType,
                BuildingTypes.Nursery, StringComparison.Ordinal)) return;
            OverlayOpenForExternalHost = true;
            if (premiumWindow == null)
            {
                GameObject root = new GameObject("Nursery Activities Screen");
                root.transform.SetParent(transform, false);
                premiumWindow = root.AddComponent<HiveMapPremiumBuildingWindow>();
            }
            premiumWindow.BindNurseryController(
                MobileAccountSessionRuntimeBootstrap.BroodVitalityControllerForHiveMap);
            premiumWindow.Show(building,
                "Nurserie", "Nursery",
                "Nutrition et stabilité de la colonie",
                "Colony nutrition and stability",
                ClosePremiumWindow, UpgradeFromPremiumWindow);
            premiumWindow.SetSuppressed(
                HiveMapActivitiesBootstrap.ModalOpenForExternalHost ||
                HiveMapArmyBootstrap.ModalOpenForExternalHost);
        }

        private void ClosePremiumWindow()
        {
            OverlayOpenForExternalHost = false;
            if (premiumWindow != null) premiumWindow.Hide();
        }

        private void UpgradeFromPremiumWindow()
        {
            ClosePremiumWindow();
            LegacyMappingEntry building =
                BuildingMappingTable.GetByBuildingType(BuildingTypes.Nursery);
            if (building != null && !string.IsNullOrWhiteSpace(building.LegacyKey))
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(
                    building.LegacyKey);
        }
    }
}
