using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.LivingHiveMenu;

namespace BeeKingdom.Playground
{
    public static class HiveMapRuntimeBootstrapInitializer
    {
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            if (subscribed) return;
            subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsEnvironmentScene(scene)) return;
            ResetPresenterOverlayStateForSceneEntry();
            InitializeAllBootstraps(scene);
        }

        // M056A-CL : correction du defaut "HiveMap -> WorldMap -> HiveMap : plus de pan ni de
        // zoom, mais les batiments et les menus restent cliquables".
        //
        // CAUSE EXACTE : HiveViewProductUiPresenter est une classe STATIQUE dont la vingtaine de
        // booleens "ecran ouvert" survivent aux changements de scene (les statics ne se vident
        // qu'au domain reload). Or ces drapeaux sont lus par TROIS predicats differents, et un
        // seul d'entre eux garde la camera :
        //
        //   PremiumUiBlocksWorldInput()                 -> garde la CAMERA (pan + zoom)
        //   HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking() -> garde batiments + menus
        //
        // Plusieurs drapeaux (combatPatrolOverlayOpen, ouvert par le bouton ATTAQUER de la
        // WorldMap, mais aussi communicationPanelOpen, missionsCenterOpen, etc.) figurent dans
        // le PREMIER predicat et pas dans le second. S'ils restent a true en quittant la
        // WorldMap, on obtient exactement le symptome rapporte : camera morte, UI vivante.
        // Pire : aucun ecran de la HiveMap ne redessine ces overlays, donc rien ne pouvait plus
        // jamais les refermer - l'etat etait irrecuperable sans redemarrer le processus.
        //
        // ResetPremiumScreensForProof() remettait deja TOUS ces drapeaux a zero, mais n'etait
        // appele que depuis les tests. On le branche ici, sur le seam de re-initialisation
        // par chargement de scene deja prevu par le projet. Entrer dans la ruche ne doit
        // jamais heriter d'un overlay laisse ouvert dans une autre scene.
        private static void ResetPresenterOverlayStateForSceneEntry()
        {
            HiveViewProductUiPresenter.ResetPremiumScreensForProof();

            // Second verrou de la camera : BuildingPerspectiveCamera consulte aussi
            // DebugHotkeyGuard.TextInputHasFocus, qui teste GUIUtility.keyboardControl. Cet etat
            // IMGUI est GLOBAL et survit lui aussi a LoadScene : un champ de saisie encore
            // "focus" dans la scene precedente (auth, recherche WorldMap) laisserait la camera
            // bloquee de la meme maniere. On repart d'un focus clavier propre a chaque entree.
            GUIUtility.keyboardControl = 0;
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            if (scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return true;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.name != null && root.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void InitializeAllBootstraps(Scene scene)
        {
            // Building controller must exist before Research host attaches to its selection service
            BuildingRuntimeViewBootstrap.AutoStartForScene(scene);
            LivingHiveMenuRuntime.EnsureRuntime(scene);
            LivingHiveResearchRuntime.EnsureRuntime(scene);
            HiveMapSplashBootstrap.InitializeForScene(scene);
            HiveMapOverlayInputGateBootstrap.InitializeForScene(scene);
            HiveMapActivitiesBootstrap.InitializeForScene(scene);
            HiveMapAllianceBootstrap.InitializeForScene(scene);
            HiveMapBarrackBootstrap.InitializeForScene(scene);
            HiveMapBuildingUpgradeClickBootstrap.InitializeForScene(scene);
            HiveMapBuildingUpgradeVisualStateBootstrap.InitializeForScene(scene);
            // M049C-CL: same exact M038B-CL failure mode - HiveMapResearchVisualStateBootstrap's
            // own [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] AutoStart() only fires once, on
            // whatever scene is active the instant Play Mode starts (the splash/login scene,
            // never "Environment2D5D"-prefixed), so it never actually ran once the player
            // transitioned into the real HiveMap scene - confirmed live: CEO had a real Research
            // operation Running with zero visible pulse. Wired into the same production
            // installer Construction's own visual-state bootstrap already uses above.
            HiveMapResearchVisualStateBootstrap.InitializeForScene(scene);
            HiveMapChampionHallBootstrap.InitializeForScene(scene);
            HiveMapConstructionBootstrap.InitializeForScene(scene);
            HiveMapNurseryBootstrap.InitializeForScene(scene);
            HiveMapProductionBootstrap.InitializeForScene(scene);
            HiveMapProductionInfoBootstrap.InitializeForScene(scene);
            HiveMapQueueSidebarBootstrap.InitializeForScene(scene);
            HiveMapResourceHudBootstrap.InitializeForScene(scene);
            HiveMapRoyalPalaceBootstrap.InitializeForScene(scene);
            HiveMapSettingsBootstrap.InitializeForScene(scene);
            HiveMapUnsupportedBuildingBootstrap.InitializeForScene(scene);
            HiveMapArmyBootstrap.InitializeForScene(scene);
            HiveMapAmbientBeesBootstrap.InitializeForScene(scene);
            LivingHiveChatBridgeBootstrap.InitializeForScene(scene);
            // M038B-CL: both were missing from this list entirely - their [RuntimeInitializeOnLoadMethod]
            // AutoStart() only fires once, on whatever scene is active when Play Mode starts (the
            // splash/login scene, which never starts with "Environment2D5D"), so neither ever actually
            // ran once the player transitioned into the real HiveMap scene. Confirmed live: a fresh
            // account entering Environment2D5D_HiveMap_Test had zero FtueTutorialBootstrap instances.
            HiveMapResearchBootstrap.InitializeForScene(scene);
            BeeKingdom.Tutorial.FtueTutorialBootstrap.InitializeForScene(scene);
        }
    }
}