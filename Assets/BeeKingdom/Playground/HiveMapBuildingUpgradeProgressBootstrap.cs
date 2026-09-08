using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M059-CL - Parts 2 et 3 du retour du premier testeur externe (Alex).
    //
    // Deux besoins distincts, volontairement portes par le MEME bootstrap parce qu'ils
    // decrivent le meme fait de jeu (une amelioration serveur reellement en cours) et
    // doivent donc apparaitre et disparaitre exactement ensemble :
    //
    //   Part 3 - une barre de progression compacte au-dessus du batiment en travaux, pour
    //            comprendre l'avancement d'un coup d'oeil sans rien ouvrir ;
    //   Part 2 - un clic sur ce batiment ouvre une fenetre d'amelioration en cours plutot
    //            que la fenetre ordinaire du batiment.
    //
    // Ce bootstrap est SEPARE de HiveMapBuildingUpgradeVisualStateBootstrap (le pulse
    // bleu/cyan et la validation au clic quand l'operation attend sa validation), qui n'est
    // ni modifie ni remplace : le pulse dit "chantier ici", la barre dit "ou en est le
    // chantier", et l'etat AwaitingCompletion reste entierement la propriete de l'autre
    // bootstrap. Les deux crochets de preemption cohabitent sans se disputer le clic parce
    // qu'ils lisent deux etats mutuellement exclusifs du serveur : ActiveOfficialUpgrade...
    // ne repond que sur "running", ReadyToCompleteOfficialUpgrade... que sur
    // "awaiting_completion", et le serveur ne porte qu'une seule operation de construction
    // pour toute la ruche.
    //
    // Aucune source de temps locale : la progression et le temps restant viennent de
    // l'operation serveur (StartedAtUtc/CompletesAtUtc projetes sur l'horloge serveur), donc
    // l'affichage se reconstruit correctement apres un aller-retour de scene ou une
    // reconnexion, et suit le compte authentifie puisque le controleur d'amelioration est
    // detruit et recree par la session de compte.
    //
    // Meme strategie d'auto-amorcage que les autres bootstraps Environment2D5D.
    public sealed class HiveMapBuildingUpgradeProgressBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Building Upgrade Progress Runtime";

        // Bornes de la barre en pixels ecran : assez lisible en zoom arriere, jamais geante
        // en zoom avant. La conversion monde -> pixels reprend celle deja utilisee par
        // HiveMapBuildingUpgradeVisualStateBootstrap pour le badge d'achevement.
        private const float BarWorldUnits = 9.2f;
        private const float FallbackPixelsPerWorldUnit = 11.76f;

        private BuildingInteractionController controller;
        private bool preemptionHookInstalled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapBuildingUpgradeProgressBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapBuildingUpgradeProgressBootstrap>();
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
            if (FindFirstObjectByType<HiveMapBuildingUpgradeProgressBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapBuildingUpgradeProgressBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (controller == null)
            {
                controller = FindFirstObjectByType<BuildingInteractionController>();
                if (controller == null) return;
            }

            if (!preemptionHookInstalled)
            {
                BuildingInteractionController.RegisterCompletionPreemption(TryOpenUpgradeProgressOnClick);
                preemptionHookInstalled = true;
            }
        }

        private void OnDestroy()
        {
            if (preemptionHookInstalled)
                BuildingInteractionController.UnregisterCompletionPreemption(TryOpenUpgradeProgressOnClick);
            // Une fenetre laissee ouverte survivrait au dechargement de la scene via la
            // statique du presenteur - meme famille de fuite que les drapeaux d'overlay
            // corriges par M056A/M058.
            HiveViewProductUiPresenter.CloseUpgradeProgressOverlayForExternalHost();
        }

        // Consomme le clic UNIQUEMENT sur le batiment qui porte vraiment l'amelioration en
        // cours. Tout autre batiment renvoie false et ouvre sa fenetre ordinaire, exactement
        // comme avant. Le batiment en attente de validation ne passe jamais par ici : son
        // propre crochet le traite, et ActiveOfficialUpgradeHotspotId... ne repond pas pour lui.
        private static bool TryOpenUpgradeProgressOnClick(BuildingDefinition building)
        {
            if (building == null) return false;
            string hotspotId = BuildingMappingTable.GetByBuildingType(building.BuildingType).LegacyKey;
            return HiveViewProductUiPresenter.TryOpenUpgradeProgressOverlayForExternalHost(hotspotId);
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;

            bool compact = Screen.width < 900;

            // La fenetre passe devant tout le reste : Unity ne garantit pas l'ordre des OnGUI
            // entre MonoBehaviours, donc on force la profondeur plutot que d'esperer un ordre.
            if (HiveViewProductUiPresenter.UpgradeProgressOverlayOpenForExternalHost)
            {
                int previousDepth = GUI.depth;
                GUI.depth = -20;
                HiveViewProductUiPresenter.DrawUpgradeProgressOverlayForExternalHost(compact);
                GUI.depth = previousDepth;
                return;
            }

            if (controller == null) return;
            // Meme garde que le badge d'achevement : rien du monde ne doit transpercer un
            // overlay plein ecran ouvert par ailleurs.
            if (HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()) return;

            string runningHotspotId = HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost();
            if (string.IsNullOrEmpty(runningHotspotId)) return;
            if (!BuildingCatalog.TryGetByLegacyKey(runningHotspotId, out BuildingDefinition definition)) return;
            // M095-CL : meme correctif que HiveMapProductionInfoBootstrap/HiveMapProductionBootstrap.
            if (!controller.Registry.TryGetGameObjectByBuildingType(definition.BuildingType, out GameObject target) || target == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            Rect rect = ScreenRectFor(target, camera);
            if (rect.width <= 0f) return;

            float pixelsPerWorldUnit = camera.orthographic && camera.orthographicSize > 0.001f
                ? Screen.height / (2f * camera.orthographicSize)
                : FallbackPixelsPerWorldUnit;
            float barWidth = BarWorldUnits * pixelsPerWorldUnit;
            // Le temps restant n'est lisible qu'a partir d'une certaine taille : en dessous,
            // on garde la seule barre plutot que d'encombrer la ruche d'un texte illisible.
            bool showRemaining = barWidth >= 96f;
            HiveViewProductUiPresenter.DrawBuildingUpgradeProgressBarForExternalHost(rect, barWidth, showRemaining);
        }

        // Meme projection d'emprise ecran que HiveMapBuildingUpgradeVisualStateBootstrap et
        // HiveMapProductionBootstrap - garantit que la barre reste calee sur le batiment au
        // pan comme au zoom, sans systeme de suivi parallele.
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
