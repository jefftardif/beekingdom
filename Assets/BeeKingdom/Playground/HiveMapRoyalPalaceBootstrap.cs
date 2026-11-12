using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    public sealed class HiveMapRoyalPalaceBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Royal Palace Runtime";
        public static bool OverlayOpenForExternalHost { get; private set; }
        public static bool ModalOpenForExternalHost =>
            OverlayOpenForExternalHost || HiveViewProductUiPresenter.ColonyOverviewOpenForExternalHost;

        private BuildingInteractionController subscribedController;
        private HiveMapRoyalPalacePremiumWindow premiumWindow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart() => InitializeForScene(SceneManager.GetActiveScene());

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded
                || !scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return;
            if (FindAnyObjectByType<HiveMapRoyalPalaceBootstrap>() != null) return;
            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapRoyalPalaceBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost)
            {
                Close();
                return;
            }
            if (premiumWindow != null)
                premiumWindow.SetSuppressed(HiveMapActivitiesBootstrap.ModalOpenForExternalHost
                    || HiveMapArmyBootstrap.ModalOpenForExternalHost
                    || HiveViewProductUiPresenter.ColonyOverviewOpenForExternalHost);
            if (subscribedController != null) return;
            BuildingInteractionController controller = FindAnyObjectByType<BuildingInteractionController>();
            if (controller == null) return;
            controller.Selection.BuildingClicked += OnBuildingClicked;
            subscribedController = controller;
        }

        private void OnDisable() => Close();

        private void OnDestroy()
        {
            Close();
            if (subscribedController != null)
                subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void Close()
        {
            OverlayOpenForExternalHost = false;
            if (premiumWindow != null) premiumWindow.Hide();
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (!isActiveAndEnabled || building == null || !string.Equals(building.BuildingType,
                BuildingTypes.RoyalPalace, StringComparison.Ordinal)) return;
            if (premiumWindow == null) premiumWindow = gameObject.AddComponent<HiveMapRoyalPalacePremiumWindow>();
            OverlayOpenForExternalHost = true;
            premiumWindow.Show(Close);
            HiveViewProductUiPresenter.RefreshRoyalPalaceUpgradeForExternalHost();
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyWindowOpened("administration_core"); } catch { }
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyBuildingSelected("administration_core"); } catch { }
        }

        private void OnGUI()
        {
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost) return;
            if (HiveViewProductUiPresenter.ColonyOverviewOpenForExternalHost)
            {
                HiveViewProductUiPresenter.DrawColonyOverviewOverlayForExternalHost(Screen.width < 900);
                return;
            }
            if (OverlayOpenForExternalHost && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
        }
    }
}
