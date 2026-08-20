using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M008-CX wave 2: Genetics and Infirmary were marked "future" in LivingHive and have
    // no server-authoritative gameplay controller in the current client. HiveMap should
    // still give a clear building-specific response instead of silently falling through to
    // Construction, while preserving that existing upgrade path one tap deeper.
    public sealed class HiveMapUnsupportedBuildingBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Unsupported Building Runtime";
        private const float PanelWidth = 350f;
        private const float PanelHeight = 210f;

        public static bool OverlayOpenForExternalHost { get; private set; }

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
            if (subscribedController != null) subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !IsTracked(building.BuildingType)) return;
            selectedBuilding = building;
            OverlayOpenForExternalHost = true;
        }

        private void OnGUI()
        {
            if (!OverlayOpenForExternalHost) return;
            BuildingDefinition building = selectedBuilding;
            if (building == null)
            {
                OverlayOpenForExternalHost = false;
                return;
            }

            Rect panel = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                (Screen.height - PanelHeight) * 0.5f,
                PanelWidth,
                PanelHeight);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 10f, PanelWidth - 24f, PanelHeight - 20f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(BuildingTitle(building), GUI.skin.label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(28f))) OverlayOpenForExternalHost = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label(BuildingStatus(building), new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Space(8f);
            GUILayout.Label("Capacite actuelle : amelioration du batiment.", new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Label("Gameplay officiel : pas encore disponible dans le client actuel.", new GUIStyle(GUI.skin.label) { wordWrap = true });

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fermer"))
            {
                OverlayOpenForExternalHost = false;
            }
            if (GUILayout.Button("Ameliorer"))
            {
                OverlayOpenForExternalHost = false;
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(building.LegacyKey);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static bool IsTracked(string buildingType)
        {
            return string.Equals(buildingType, BuildingTypes.Infirmary, StringComparison.Ordinal)
                || string.Equals(buildingType, BuildingTypes.Genetics, StringComparison.Ordinal);
        }

        private static string BuildingStatus(BuildingDefinition building)
        {
            if (string.Equals(building.BuildingType, BuildingTypes.Infirmary, StringComparison.Ordinal))
                return "Les soins officiels ne sont pas encore exposes par un controleur Infirmary. Les soigneuses restent donc en attente de fonctionnalite.";
            return "La genetique officielle reste une capacite future : les choix de mutation/progression ne sont pas encore server-backed.";
        }

        private static string BuildingTitle(BuildingDefinition building)
        {
            if (string.Equals(building.BuildingType, BuildingTypes.Infirmary, StringComparison.Ordinal)) return "Infirmerie";
            return "Genetique";
        }
    }
}
