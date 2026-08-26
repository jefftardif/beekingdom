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
    // M009-CX wave 3 extends the same honest status pattern to Academy and Defense after
    // inspection confirmed they do not yet own official building-specific gameplay.
    // M013-CX wave 4 adds Bank: the catalog marks it future and no bank-specific
    // server-backed gameplay controller exists in the current client.
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
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost) return;
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
                || string.Equals(buildingType, BuildingTypes.Genetics, StringComparison.Ordinal)
                || string.Equals(buildingType, BuildingTypes.Academy, StringComparison.Ordinal)
                || string.Equals(buildingType, BuildingTypes.Defense, StringComparison.Ordinal)
                || string.Equals(buildingType, BuildingTypes.Bank, StringComparison.Ordinal);
        }

        private static string BuildingStatus(BuildingDefinition building)
        {
            if (string.Equals(building.BuildingType, BuildingTypes.Infirmary, StringComparison.Ordinal))
                return "Les soins officiels ne sont pas encore exposes par un controleur Infirmary. Les soigneuses restent donc en attente de fonctionnalite.";
            if (string.Equals(building.BuildingType, BuildingTypes.Academy, StringComparison.Ordinal))
                return "L'Academie est presente comme batiment futur. La Recherche officielle reste portee par son propre noeud et sa fenetre HiveMap dediee; aucune formation Academie separee n'est server-backed aujourd'hui.";
            if (string.Equals(building.BuildingType, BuildingTypes.Defense, StringComparison.Ordinal))
                return "La Defense reste une zone future. Les systemes combat/perimetre existants vivent dans les parcours Armee et serveur, mais ne sont pas encore une action officielle de ce batiment.";
            if (string.Equals(building.BuildingType, BuildingTypes.Bank, StringComparison.Ordinal))
                return "La Banque est presente comme batiment futur. Les stocks, recompenses et ressources officielles restent portes par leurs panneaux et clients dedies; aucune action bancaire separee n'est server-backed aujourd'hui.";
            return "La genetique officielle reste une capacite future : les choix de mutation/progression ne sont pas encore server-backed.";
        }

        private static string BuildingTitle(BuildingDefinition building)
        {
            if (string.Equals(building.BuildingType, BuildingTypes.Infirmary, StringComparison.Ordinal)) return "Infirmerie";
            if (string.Equals(building.BuildingType, BuildingTypes.Academy, StringComparison.Ordinal)) return "Academie";
            if (string.Equals(building.BuildingType, BuildingTypes.Defense, StringComparison.Ordinal)) return "Defense";
            if (string.Equals(building.BuildingType, BuildingTypes.Bank, StringComparison.Ordinal)) return "Banque";
            return "Genetique";
        }
    }
}
