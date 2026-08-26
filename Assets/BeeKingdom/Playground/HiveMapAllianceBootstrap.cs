using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Opens HiveViewProductUiPresenter's Alliance headquarters screen when the player
    // clicks the ALLIANCE_CENTER building. In SandboxPlayground this came from a
    // dedicated "Alliance" rail button (10-item landscape rail); HiveMap's 5-button menu
    // doesn't have that button, so the building itself is the trigger here instead —
    // additive to the existing building-selection highlight, doesn't replace it.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D". BuildingInteractionController.IsEnabled is gated off by
    // HiveMapSplashBootstrap until the player enters the hive, so building clicks (and
    // this handler) naturally can't fire before then either.
    public sealed class HiveMapAllianceBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Alliance Runtime";
        private const string AllianceCenterBuildingType = "ALLIANCE_CENTER";

        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapAllianceBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapAllianceBootstrap>();
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
            if (FindFirstObjectByType<HiveMapAllianceBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapAllianceBootstrap>();
        }

        private void Update()
        {
            // Polled rather than found once in Start(): BuildingInteractionController is
            // created by a separate AutoStart bootstrap (BuildingRuntimeViewBootstrap) with
            // no guaranteed ordering relative to this one on the same AfterSceneLoad frame.
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
            if (building == null || !string.Equals(building.BuildingType, AllianceCenterBuildingType, StringComparison.Ordinal)) return;
            HiveViewProductUiPresenter.OpenAllianceOverlayForExternalHost();
        }

        private void OnGUI()
        {
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost) return;
            HiveViewProductUiPresenter.DrawAllianceOverlayForExternalHost(Screen.width < 900);
            HiveViewProductUiPresenter.DrawAllianceUpgradeButtonForExternalHost();
        }
    }
}
