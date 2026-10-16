using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BeeKingdom.Playground {


    public sealed class HiveMapBankBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Bank Runtime";

        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindAnyObjectByType<HiveMapBankBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapBankBootstrap>();
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
            if (FindAnyObjectByType<HiveMapBankBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapBankBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (subscribedController != null) return;

            BuildingInteractionController controller = FindAnyObjectByType<BuildingInteractionController>();
            if (controller == null) return;
            controller.Selection.BuildingClicked += OnBuildingClicked;
            subscribedController = controller;
        }

        private void OnDestroy()
        {
            HiveViewProductUiPresenter.CloseBankOverlayForExternalHost();
            if (subscribedController != null)
                subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !string.Equals(building.BuildingType, BuildingTypes.Bank, StringComparison.Ordinal))
                return;

            if (HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking() &&
                !HiveViewProductUiPresenter.BankOverlayOpenForExternalHost)
                return;

            HiveViewProductUiPresenter.OpenBankOverlayForExternalHost();
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.BankOverlayOpenForExternalHost) return;
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost
                || HiveViewProductUiPresenter.ConstructionOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.UpgradeProgressOverlayOpenForExternalHost) return;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                HiveViewProductUiPresenter.CloseBankOverlayForExternalHost();
                Event.current.Use();
                return;
            }
            HiveViewProductUiPresenter.DrawBankOverlayForExternalHost(Screen.width < 900);
        }
    }

}
