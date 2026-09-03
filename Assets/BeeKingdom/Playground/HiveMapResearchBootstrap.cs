using System;
using BeeKingdom.Core.Integration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Wires LivingHiveResearchBridge.SetHandlers(...) so LivingHiveResearchHost (in the
    // BeeKingdom.LivingHiveMenu assembly, which cannot legally reference this assembly) can route
    // Research building clicks to the official server-backed Research overlay when an authenticated
    // session has one configured, and draws that overlay via OnGUI. Falls back to
    // LivingHiveResearchHost's own existing local-preview fullscreen window (M011 modal-safe)
    // whenever no official session is available; this bootstrap never touches that fallback path.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapResearchBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Research Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapResearchBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapResearchBootstrap>();
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
            if (FindFirstObjectByType<HiveMapResearchBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapResearchBootstrap>();
        }

        private void Start()
        {
            LivingHiveResearchBridge.SetHandlers(
                () => HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost,
                () => MobileAccountSessionRuntimeBootstrap.IsResearchControllerAvailableForExternalHost(),
                OpenOfficialOverlay);
        }

        private static void OpenOfficialOverlay()
        {
            HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost();
            MobileAccountSessionRuntimeBootstrap.ResearchControllerForHiveMap.Refresh();
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyBuildingSelected("research_node"); } catch {}
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyWindowOpened("research_node"); } catch {}
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (!HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost) return;
            HiveViewProductUiPresenter.RefreshResearchIfOverlayOpenAndDue();
            bool compact = Screen.width < 900;

            // M040X-CL: same fix as HiveMapProductionBootstrap/HiveMapBarrackBootstrap - see
            // Docs/AI/Missions/M040X-CL-FTUE-Overlay-Occlusion-Fix.md.
            bool clipToDialogue = BeeKingdom.Tutorial.TutorialDialoguePresenter.IsAnyDialogueVisible;
            if (clipToDialogue)
            {
                Rect panelRect = BeeKingdom.Tutorial.TutorialDialoguePresenter.GetCurrentPanelRect();
                GUI.BeginGroup(new Rect(0f, 0f, Screen.width, panelRect.yMin));
            }

            HiveViewProductUiPresenter.DrawResearchOverlayForExternalHost(compact);

            if (clipToDialogue) GUI.EndGroup();
        }
    }
}
