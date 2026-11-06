using System;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Infirmary now uses its restored Activities recovery ward; the remaining buildings keep this fallback.
    // M008-CX wave 2: Genetics and Infirmary were marked "future" in LivingHive and have
    // no server-authoritative gameplay controller in the current client. HiveMap should
    // still give a clear building-specific response instead of silently falling through to
    // Construction, while preserving that existing upgrade path one tap deeper.
    // M009-CX wave 3 extends the same honest status pattern to Academy and Defense after
    // inspection confirmed they do not yet own official building-specific gameplay.
    // M013-CX wave 4 adds Bank: the catalog marks it future and no bank-specific
    // server-backed gameplay controller exists in the current client.
    public sealed class HiveMapUnsupportedBuildingBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Unsupported Building Runtime";

        public static bool OverlayOpenForExternalHost { get; private set; }

        private HiveMapPremiumBuildingWindow infirmaryWindow;
        private HiveMapPremiumBuildingWindow defenseWindow;
        private HiveMapPremiumBuildingWindow geneticsWindow;
        private HiveMapPremiumBuildingWindow academyWindow;
        private IHiveSquadReservationPanelController boundDefenseSquad;
        private ICombatPatrolPanelController boundDefensePatrol;
        private ICombatPatrolPanelController boundInfirmaryController;
        private bool IsAcademySelection => selectedBuilding != null &&
            string.Equals(selectedBuilding.BuildingType, BuildingTypes.Academy,
                StringComparison.Ordinal);
        private bool IsGeneticsSelection => selectedBuilding != null &&
            string.Equals(selectedBuilding.BuildingType, BuildingTypes.Genetics, StringComparison.Ordinal);
        private bool IsDefenseSelection => selectedBuilding != null &&
            string.Equals(selectedBuilding.BuildingType, BuildingTypes.Defense, StringComparison.Ordinal);
        private bool IsInfirmarySelection => selectedBuilding != null &&
            string.Equals(selectedBuilding.BuildingType, BuildingTypes.Infirmary, StringComparison.Ordinal);
        private BuildingDefinition selectedBuilding;
        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindAnyObjectByType<HiveMapUnsupportedBuildingBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
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
            if (FindAnyObjectByType<HiveMapUnsupportedBuildingBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (OverlayOpenForExternalHost && IsInfirmarySelection && infirmaryWindow != null)
            {
                var activeController = MobileAccountSessionRuntimeBootstrap.CombatPatrolControllerForHiveMap;
                if (!ReferenceEquals(boundInfirmaryController, activeController))
                {
                    boundInfirmaryController = activeController;
                    infirmaryWindow.BindInfirmaryController(activeController);
                }
                infirmaryWindow.SetSuppressed(InfirmarySuppressed());
            }
            if (OverlayOpenForExternalHost && IsDefenseSelection && defenseWindow != null)
            {
                var squad = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap;
                var patrol = MobileAccountSessionRuntimeBootstrap.CombatPatrolControllerForHiveMap;
                if (!ReferenceEquals(boundDefenseSquad, squad) ||
                    !ReferenceEquals(boundDefensePatrol, patrol))
                {
                    boundDefenseSquad = squad;
                    boundDefensePatrol = patrol;
                    defenseWindow.BindDefenseControllers(squad, patrol);
                }
                defenseWindow.SetSuppressed(DefenseSuppressed());
            }
            if (OverlayOpenForExternalHost && IsGeneticsSelection && geneticsWindow != null)
                geneticsWindow.SetSuppressed(GeneticsSuppressed());
            if (OverlayOpenForExternalHost && IsAcademySelection && academyWindow != null)
                academyWindow.SetSuppressed(AcademySuppressed());
            if (subscribedController != null) return;
            BuildingInteractionController controller = FindAnyObjectByType<BuildingInteractionController>();
            if (controller == null) return;
            controller.Selection.BuildingClicked += OnBuildingClicked;
            subscribedController = controller;
        }

        private void OnDestroy()
        {
            CloseInfirmaryWindow();
            CloseDefenseWindow();
            CloseGeneticsWindow();
            CloseAcademyWindow();
            if (subscribedController != null) subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !IsTracked(building.BuildingType)) return;
            selectedBuilding = building;
            OverlayOpenForExternalHost = true;
            if (IsInfirmarySelection)
            {
                // Only one premium building window may remain active.
                if (defenseWindow != null) defenseWindow.Hide();
                if (academyWindow != null) academyWindow.Hide();
                // Restore the existing recovery ward, not the future-feature placeholder.
                if (infirmaryWindow == null)
                {
                    GameObject root = new GameObject("Infirmary Activities Screen");
                    root.transform.SetParent(transform, false);
                    infirmaryWindow = root.AddComponent<HiveMapPremiumBuildingWindow>();
                }
                boundInfirmaryController = MobileAccountSessionRuntimeBootstrap.CombatPatrolControllerForHiveMap;
                infirmaryWindow.BindInfirmaryController(boundInfirmaryController);
                if (geneticsWindow != null) geneticsWindow.Hide();
                infirmaryWindow.Show(building, "Infirmerie", "Infirmary", string.Empty, string.Empty,
                    CloseInfirmaryWindow, UpgradeInfirmary);
                infirmaryWindow.SetSuppressed(InfirmarySuppressed());
            }
            else if (IsDefenseSelection)
            {
                if (infirmaryWindow != null) infirmaryWindow.Hide();
                if (academyWindow != null) academyWindow.Hide();
                if (defenseWindow == null)
                {
                    GameObject root = new GameObject("Defense Activities Screen");
                    root.transform.SetParent(transform, false);
                    defenseWindow = root.AddComponent<HiveMapPremiumBuildingWindow>();
                }
                boundDefenseSquad = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap;
                boundDefensePatrol = MobileAccountSessionRuntimeBootstrap.CombatPatrolControllerForHiveMap;
                defenseWindow.BindDefenseControllers(boundDefenseSquad, boundDefensePatrol);
                if (geneticsWindow != null) geneticsWindow.Hide();
                defenseWindow.Show(building, "Défense", "Defense", string.Empty,
                    string.Empty, CloseDefenseWindow, UpgradeDefense);
                defenseWindow.SetSuppressed(DefenseSuppressed());
            }
            else if (IsGeneticsSelection)
            {
                if (infirmaryWindow != null) infirmaryWindow.Hide();
                if (academyWindow != null) academyWindow.Hide();
                if (defenseWindow != null) defenseWindow.Hide();
                if (geneticsWindow == null)
                {
                    GameObject root = new GameObject("Genetics Activities Screen");
                    root.transform.SetParent(transform, false);
                    geneticsWindow = root.AddComponent<HiveMapPremiumBuildingWindow>();
                }
                geneticsWindow.Show(building, "Génétique", "Genetics",
                    string.Empty, string.Empty,
                    CloseGeneticsWindow, UpgradeGenetics);
                geneticsWindow.SetSuppressed(GeneticsSuppressed());
            }
            else if (IsAcademySelection)
            {
                if (infirmaryWindow != null) infirmaryWindow.Hide();
                if (defenseWindow != null) defenseWindow.Hide();
                if (geneticsWindow != null) geneticsWindow.Hide();
                if (academyWindow == null)
                {
                    GameObject root = new GameObject("Academy Activities Screen");
                    root.transform.SetParent(transform, false);
                    academyWindow = root.AddComponent<HiveMapPremiumBuildingWindow>();
                }
                academyWindow.Show(building, "Académie", "Academy",
                    string.Empty, string.Empty,
                    CloseAcademyWindow, UpgradeAcademy);
                academyWindow.SetSuppressed(AcademySuppressed());
            }
            else
            {
                if (infirmaryWindow != null) infirmaryWindow.Hide();
                if (defenseWindow != null) defenseWindow.Hide();
                if (geneticsWindow != null) geneticsWindow.Hide();
                if (academyWindow != null) academyWindow.Hide();
            }
        }

        private static bool InfirmarySuppressed()
        {
            return HiveMapActivitiesBootstrap.ModalOpenForExternalHost
                || HiveMapArmyBootstrap.ModalOpenForExternalHost
                || HiveViewProductUiPresenter.ConstructionOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.UpgradeProgressOverlayOpenForExternalHost;
        }

        private static bool DefenseSuppressed()
        {
            return InfirmarySuppressed() ||
                HiveViewProductUiPresenter.PlayerProfileOverlayOpenForExternalHost;
        }

        private static bool GeneticsSuppressed()
        {
            return DefenseSuppressed();
        }

        private static bool AcademySuppressed()
        {
            return DefenseSuppressed()
                || HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.CommunicationOverlayOpenForExternalHost;
        }

        private void CloseAcademyWindow()
        {
            if (!IsAcademySelection && academyWindow == null) return;
            OverlayOpenForExternalHost = false;
            selectedBuilding = null;
            if (academyWindow != null) academyWindow.Hide();
        }

        private void UpgradeAcademy()
        {
            string key = selectedBuilding?.LegacyKey;
            CloseAcademyWindow();
            if (!string.IsNullOrWhiteSpace(key))
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(key);
        }

        private void CloseGeneticsWindow()
        {
            if (!IsGeneticsSelection && geneticsWindow == null) return;
            OverlayOpenForExternalHost = false;
            selectedBuilding = null;
            if (geneticsWindow != null) geneticsWindow.Hide();
        }

        private void UpgradeGenetics()
        {
            string key = selectedBuilding?.LegacyKey;
            CloseGeneticsWindow();
            if (!string.IsNullOrWhiteSpace(key))
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(key);
        }
        private void CloseDefenseWindow()
        {
            if (!IsDefenseSelection && defenseWindow == null) return;
            OverlayOpenForExternalHost = false;
            selectedBuilding = null;
            boundDefenseSquad = null;
            boundDefensePatrol = null;
            if (defenseWindow != null) defenseWindow.Hide();
        }

        private void UpgradeDefense()
        {
            string key = selectedBuilding?.LegacyKey;
            CloseDefenseWindow();
            CloseGeneticsWindow();
            if (!string.IsNullOrWhiteSpace(key))
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(key);
        }
        private void CloseInfirmaryWindow()
        {
            OverlayOpenForExternalHost = false;
            selectedBuilding = null;
            boundInfirmaryController = null;
            if (infirmaryWindow != null) infirmaryWindow.Hide();
        }

        private void UpgradeInfirmary()
        {
            string key = selectedBuilding?.LegacyKey;
            CloseInfirmaryWindow();
            if (!string.IsNullOrWhiteSpace(key))
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(key);
        }

        private void OnGUI()
        {
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost) return;
            if (!OverlayOpenForExternalHost) return;
            if (IsInfirmarySelection)
            {
                // Retained-mode UI owns rendering and input; do not draw the old popup.
                if (!InfirmarySuppressed() && Event.current.type == EventType.KeyDown
                    && Event.current.keyCode == KeyCode.Escape)
                {
                    CloseInfirmaryWindow();
                    Event.current.Use();
                }
                return;
            }
            if (IsDefenseSelection)
            {
                // The retained-mode full screen receives events, not the
                // old provisional popup. No click-through to the hive.
                if (!DefenseSuppressed() && Event.current.type == EventType.KeyDown
                    && Event.current.keyCode == KeyCode.Escape)
                {
                    CloseDefenseWindow();
                    Event.current.Use();
                }
                return;
            }
            if (IsGeneticsSelection)
            {
                if (!GeneticsSuppressed() && Event.current.type == EventType.KeyDown
                    && Event.current.keyCode == KeyCode.Escape)
                {
                    CloseGeneticsWindow();
                    Event.current.Use();
                }
                return;
            }
            if (IsAcademySelection)
            {
                // Academy is a retained-mode fullscreen modal, not a small popup.
                if (!AcademySuppressed() && Event.current.type == EventType.KeyDown
                    && Event.current.keyCode == KeyCode.Escape)
                {
                    CloseAcademyWindow();
                    Event.current.Use();
                }
                return;
            }
            // Every tracked building (Academy, Defense, Genetics, Infirmary)
            // has a real retained-mode premium canvas above. Never resurrect
            // the old generic gray GUI.Box/GUILayout placeholder.
            OverlayOpenForExternalHost = false;
            selectedBuilding = null;
        }
        private static bool IsTracked(string buildingType)
        {
            return string.Equals(buildingType, BuildingTypes.Infirmary, StringComparison.Ordinal)
                || string.Equals(buildingType, BuildingTypes.Genetics, StringComparison.Ordinal)
                || string.Equals(buildingType, BuildingTypes.Academy, StringComparison.Ordinal)
                || string.Equals(buildingType, BuildingTypes.Defense, StringComparison.Ordinal);
        }

    }
}
