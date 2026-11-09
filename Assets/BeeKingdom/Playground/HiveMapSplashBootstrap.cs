using System;
using BeeKingdom.Audio;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Draws HiveViewProductUiPresenter's own splash/login screen (Accueil/Connexion/
    // Creation, "Jouer en demo locale") in front of the HiveMap scene until the player
    // enters the hive, then reveals HiveMap's own systems (LivingHiveMenuCanvas rail,
    // building clicks) that were gated off in the meantime. Safe to call Draw() directly:
    // see HiveViewProductUiPresenter.HasEnteredHiveForExternalHost for why it never
    // triggers the 3D hex-grid hive scene (EnsureSceneObjects is never called from inside
    // that file, and DrawInternal returns immediately after the splash gate).
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapSplashBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Splash Runtime";

        private bool revealed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapSplashBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapSplashBootstrap>();
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
            if (FindFirstObjectByType<HiveMapSplashBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapSplashBootstrap>();
        }

        private void Start()
        {
            // Same trigger point as LivingHiveDemoBootstrap.Start() / SandboxPlaygroundBootstrap.Start(),
            // which HiveMap deliberately never runs - so nothing else in this scene ever started the
            // hive music.
            MusicManager.EnsureInstance().Play(MusicTrack.Hive);
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.LocalPreview);
            ApplyGate(HiveViewProductUiPresenter.HasEnteredHiveForExternalHost);
        }

        private void Update()
        {
            bool entered = HiveViewProductUiPresenter.HasEnteredHiveForExternalHost;
            if (!entered)
            {
                // IMGUI may lose mouse events when another GUI callback
                // captures its hotControl. Poll the Unity input manager
                // independently: login tabs must not depend on GUI.Button.
                PollSplashNavigationInput();
#if UNITY_EDITOR
                // Developer escape hatch while diagnosing a completely
                // unfocused/blocked GameView: F8 enters the login tab without
                // needing IMGUI pointer events. Never included in player builds.
                if (Input.GetKeyDown(KeyCode.F8))
                {
                    bool portrait = Screen.height > Screen.width;
                    Rect[] tabs = HiveViewProductUiPresenter.SplashAuthTabRectsForProof(
                        portrait, Screen.width, Screen.height);
                    Vector2 loginPoint = ScreenPointerToGuiPointForProof(
                        tabs[1].center, Screen.height);
                    TrySplashPointer(loginPoint);
                }
#endif
                if (revealed) revealed = false;
            }
            if (!revealed || !entered)
                ApplyGate(HiveViewProductUiPresenter.HasEnteredHiveForExternalHost);
        }

        public static Vector2 ScreenPointerToGuiPointForProof(Vector2 pointer, float height)
        {
            return new Vector2(pointer.x, height - pointer.y);
        }

        private void PollSplashNavigationInput()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began) continue;
                    TrySplashPointer(touch.position);
                    return;
                }
            }
            if (Input.GetMouseButtonDown(0))
                TrySplashPointer(Input.mousePosition);
        }

        public static bool TryNavigateFromScreenPointerForProof(
            Vector2 screenPoint, float width, float height, bool pressed)
        {
            return HiveViewProductUiPresenter.TryNavigateSplashTabForProof(
                height > width, width, height,
                ScreenPointerToGuiPointForProof(screenPoint, height), pressed);
        }

        private static void TrySplashPointer(Vector2 screenPoint)
        {
            bool handled = TryNavigateFromScreenPointerForProof(
                screenPoint, Screen.width, Screen.height, true);
#if UNITY_EDITOR
            if (handled)
                Debug.Log("[BK-Splash] Update fallback selected tab: " +
                    HiveViewProductUiPresenter.SplashAuthTabStateForProof);
            else
                Debug.Log("[BK-Splash] Update saw pointer outside active tabs; viewport=" +
                    Screen.width + "x" + Screen.height);
#endif
        }

        // Hides/disables HiveMap's own menu + building interaction while the splash/login
        // screen is up (it renders full-screen on top, but its uGUI/3D-raycast input isn't
        // automatically blocked by IMGUI, so this gate keeps clicks from leaking through to
        // whatever's underneath). Runs from Update (not just Start) because the other
        // AutoStart bootstraps (LivingHiveMenuRuntime, BuildingRuntimeViewBootstrap) aren't
        // guaranteed to have run yet on this same AfterSceneLoad frame.
        private void ApplyGate(bool entered)
        {
            if (LivingHiveMenuRuntime.Root != null) LivingHiveMenuRuntime.Root.SetActive(entered);

            BuildingInteractionController controller = FindFirstObjectByType<BuildingInteractionController>();
            if (controller != null) controller.IsEnabled = entered;

            if (entered && !revealed)
            {
                // See SkipGuidedTutorialForExternalHost: entering the hive auto-starts an
                // onboarding tutorial step that silently blocks reused overlays (mini-chat)
                // since HiveMap has no UI to advance/dismiss it.
                HiveViewProductUiPresenter.SkipGuidedTutorialForExternalHost();
                revealed = true;
            }
        }

        private void OnGUI()
        {
            if (HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            // Splash/login is the only interactive surface before HiveMap
            // entry. Other runtime bootstraps may leave GUI.enabled false or
            // draw an IMGUI underlay; never let them steal the login tabs.
            // The startup loading screen deliberately uses -32750 and
            // therefore still takes priority while it is genuinely active.
            int priorDepth = GUI.depth;
            bool priorEnabled = GUI.enabled;
            Matrix4x4 priorMatrix = GUI.matrix;
            try
            {
                GUI.depth = -31000;
                GUI.enabled = true;
                GUI.matrix = Matrix4x4.identity;
                HiveViewProductUiPresenter.Draw(60f, Screen.width < 900);
            }
            finally
            {
                GUI.depth = priorDepth;
                GUI.enabled = priorEnabled;
                GUI.matrix = priorMatrix;
            }
        }
    }
}
