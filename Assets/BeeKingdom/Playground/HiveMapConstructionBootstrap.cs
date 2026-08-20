using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Opens HiveViewProductUiPresenter's Construction panel (generic per-building leveling,
    // any of the 14 buildings) - reached only via the queue sidebar's Construction card
    // (HiveMapQueueSidebarBootstrap draws that card, HiveViewProductUiPresenter's
    // DrawQueueSidebarForExternalHost wires the click), no separate building-click trigger
    // needed here since the panel has its own building picker.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapConstructionBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Construction Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapConstructionBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapConstructionBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            HiveViewProductUiPresenter.TickConstructionForExternalHost();
        }

        private void OnGUI()
        {
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost) return;
            bool compact = Screen.width < 900;
            HiveViewProductUiPresenter.DrawConstructionOverlayForExternalHost(compact);
            // Drawn in the same OnGUI call, right after the panel it can be opened from, so
            // it always renders on top of it - Unity doesn't guarantee OnGUI call order
            // across different MonoBehaviours, so a separate bootstrap for this could
            // sometimes draw underneath the Construction backdrop instead of over it.
            HiveViewProductUiPresenter.DrawSpeedUpOverlayForExternalHost(compact);
        }
    }
}
