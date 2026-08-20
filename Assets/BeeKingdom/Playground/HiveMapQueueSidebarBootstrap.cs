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

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            bool anyOverlayOpen = HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.CommunicationOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.ConstructionOverlayOpenForExternalHost
                || LivingHiveResearchRuntime.IsModalOpen
                || HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost;
            if (anyOverlayOpen) return;

            HiveViewProductUiPresenter.DrawQueueSidebarForExternalHost(Screen.width < 900);
        }
    }
}
