using System;
using System.Collections.Generic;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M009-CX wave 3: Champion Hall has a real catalog and an official server read model.
    // HiveMap exposes the supported read-only roster/catalog surface directly instead of
    // routing through the LivingHive monolith or enabling unprotected progression mutations.
    public sealed class HiveMapChampionHallBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Champion Hall Runtime";
        private const float PanelWidth = 440f;
        private const float PanelHeight = 420f;

        public static bool OverlayOpenForExternalHost { get; private set; }

        private BuildingDefinition selectedBuilding;
        private BuildingInteractionController subscribedController;
        private RemoteChampionBeeSnapshot snapshot;
        private Vector2 scroll;
        private bool busy;
        private string status = "Etat championnes non charge.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindAnyObjectByType<HiveMapChampionHallBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapChampionHallBootstrap>();
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
            if (FindAnyObjectByType<HiveMapChampionHallBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapChampionHallBootstrap>();
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
            if (building == null || !string.Equals(building.BuildingType, BuildingTypes.ChampionHall, StringComparison.Ordinal)) return;
            selectedBuilding = building;
            OverlayOpenForExternalHost = true;
            RefreshChampionState();
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
            GUILayout.Label("Hall des Championnes", GUI.skin.label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(28f))) OverlayOpenForExternalHost = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label(status, new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Space(6f);

            DrawSnapshotSummary();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(245f));
            IReadOnlyList<ChampionBeeDefinition> all = ChampionBeeCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                DrawChampionRow(all[i]);
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rafraichir"))
            {
                RefreshChampionState();
            }
            if (GUILayout.Button("Ameliorer"))
            {
                OverlayOpenForExternalHost = false;
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(building.LegacyKey);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSnapshotSummary()
        {
            if (busy)
            {
                GUILayout.Label("Lecture officielle en cours...");
                return;
            }

            if (snapshot == null)
            {
                GUILayout.Label("Catalogue local disponible : " + ChampionBeeCatalog.All.Count + " championnes.");
                GUILayout.Label("Session officielle requise pour afficher proprietes et affectations serveur.");
                return;
            }

            int owned = snapshot.Levels == null ? 0 : snapshot.Levels.Count;
            int assigned = snapshot.AssignedBeeIds == null ? 0 : snapshot.AssignedBeeIds.Count;
            GUILayout.Label("Possedees : " + owned + " / " + ChampionBeeCatalog.All.Count);
            GUILayout.Label("En mission : " + assigned + " / " + Math.Max(1, snapshot.MaxAssigned) + " - revision " + snapshot.Revision);
        }

        private void DrawChampionRow(ChampionBeeDefinition definition)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(definition.FallbackName + " - " + RarityLabel(definition.Rarity) + " - " + RoleLabel(definition.Role));
            GUILayout.Label(ChampionStateLine(definition), new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Label(EffectLine(definition), new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.EndVertical();
        }

        private string ChampionStateLine(ChampionBeeDefinition definition)
        {
            if (snapshot == null || snapshot.Levels == null)
                return "Catalogue connu; progression officielle non configuree dans cette session.";
            if (!snapshot.Levels.TryGetValue(definition.Id, out int level))
                return "Non possedee - deblocage Coeur royal niv. " + ChampionBeeCatalog.UnlockCoeurRoyalLevel(definition.Rarity);
            bool assigned = snapshot.AssignedBeeIds != null && snapshot.AssignedBeeIds.Contains(definition.Id);
            return "Niv. " + level + (assigned ? " - en mission" : " - disponible");
        }

        private static string EffectLine(ChampionBeeDefinition definition)
        {
            if (definition.Role == ChampionBeeRole.Civilian)
                return "Effet actuel supporte : bonus de production +" + definition.ProductionBonusPercentPerLevel.ToString("0.#") + "% par niveau.";
            return "Effet actuel supporte : +" + definition.ArmyBonusPerLevel + " taille armee / niveau, +"
                + definition.RoleStatBonusPercentPerLevel.ToString("0.#") + "% " + RoleLabel(definition.Role) + " / niveau.";
        }

        private async void RefreshChampionState()
        {
            if (busy) return;
            IHiveChampionBeeClient client = MobileAccountSessionRuntimeBootstrap.ChampionBeeClient;
            if (client == null || MobileAccountSessionRuntimeBootstrap.GameplayHiveId == Guid.Empty)
            {
                snapshot = null;
                status = "Client Champion officiel non configure. Affichage du catalogue local uniquement.";
                return;
            }

            busy = true;
            status = "Lecture du serveur Champion...";
            try
            {
                snapshot = await client.ReadAsync(MobileAccountSessionRuntimeBootstrap.GameplayHiveId);
                status = "Etat Champion officiel lu depuis le serveur.";
            }
            catch (Exception exception)
            {
                snapshot = null;
                status = "Lecture Champion indisponible : " + exception.GetType().Name;
            }
            finally
            {
                busy = false;
            }
        }

        private static string RarityLabel(ChampionBeeRarity rarity)
        {
            return rarity == ChampionBeeRarity.Legendary ? "Legendaire" : "Rare";
        }

        private static string RoleLabel(ChampionBeeRole role)
        {
            switch (role)
            {
                case ChampionBeeRole.Guardians: return "Gardiennes";
                case ChampionBeeRole.Wingrunners: return "Voltigeuses";
                case ChampionBeeRole.Darters: return "Lanceuses";
                default: return "Civil";
            }
        }
    }
}
