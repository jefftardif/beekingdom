using System;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Left-side queue timer sidebar (Construction / Entrainement / Recherche), reused from
    // LivingHive's DrawSideRail via HiveViewProductUiPresenter.DrawQueueSidebarForExternalHost.
    // Only drawn in the base hive view - hidden whenever a full-screen overlay (Alliance/
    // Communication/Barrack) is open, same as the monolith itself only shows this sidebar
    // in its own "normal hive view" branch, never alongside another full panel.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapQueueSidebarBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Queue Sidebar Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapQueueSidebarBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapQueueSidebarBootstrap>();
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
            if (FindFirstObjectByType<HiveMapQueueSidebarBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapQueueSidebarBootstrap>();
        }

        // M016E-CL freeze probe (temporary): once, catches the exact moment the queue sidebar
        // draws (and periodically refreshes researchController) while an official Research
        // session is configured, to rule in/out whether ResearchOverlayOpenForExternalHost is
        // unexpectedly false right after Research should have opened.
        private bool loggedSidebarDrawWhileResearchConfiguredOnce;

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            bool researchOpen = HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost;
            bool anyOverlayOpen = HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.CommunicationOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.ConstructionOverlayOpenForExternalHost
                || LivingHiveResearchRuntime.IsModalOpen || researchOpen
                || HiveMapActivitiesBootstrap.ModalOpenForExternalHost
                || HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost
                || HiveMapArmyBootstrap.ModalOpenForExternalHost;
            if (anyOverlayOpen)
            {
                loggedSidebarDrawWhileResearchConfiguredOnce = false;
                return;
            }

            if (!loggedSidebarDrawWhileResearchConfiguredOnce &&
                MobileAccountSessionRuntimeBootstrap.IsResearchControllerAvailableForExternalHost())
            {
                loggedSidebarDrawWhileResearchConfiguredOnce = true;
                Debug.Log("[M016E-FREEZE-PROBE] queue sidebar drawing while official research configured, researchOpen=" + researchOpen
                    + " localModalOpen=" + LivingHiveResearchRuntime.IsModalOpen + " t=" + Time.realtimeSinceStartup);
            }

            HiveViewProductUiPresenter.DrawQueueSidebarForExternalHost(Screen.width < 900);
        }
    }
}
