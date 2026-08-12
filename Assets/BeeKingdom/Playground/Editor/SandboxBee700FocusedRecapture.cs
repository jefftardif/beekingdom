using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee700FocusedRecapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-056_BEE681_700_ProductionPolish";
        private const string ManifestPath = OutputDirectory + "/BEE-700_DensityMobileReducedMotion_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee700FocusedRecapture.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee700FocusedRecapture.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee700FocusedRecapture.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee700FocusedRecapture.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly Vector2 Pan;
            public readonly bool ReducedMotion;

            public CaptureSpec(string label, string fileName, int width, int height, string hotspotId, Vector2 pan, bool reducedMotion)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                Pan = pan;
                ReducedMotion = reducedMotion;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Portrait 390x844 corrected player-facing", "BEE-700_Focused_01_Portrait390x844_Corrected.png", 390, 844, "wax_workshop", new Vector2(-210f, 80f), false),
            new CaptureSpec("Reduced motion player-facing", "BEE-700_Focused_02_ReducedMotion_PlayerFacing.png", 390, 844, "honey_storage", new Vector2(-170f, 60f), true),
            new CaptureSpec("No debug overlay player-facing", "BEE-700_Focused_03_NoDebugOverlay_PlayerFacing.png", 1280, 720, "guard_post", Vector2.zero, false)
        };

        static SandboxBee700FocusedRecapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Recapture BEE-700 Focused Proof")]
        public static void RecaptureBee700FocusedProof()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);

            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentState();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            ApplyCurrentState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 62) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 150) return;
                    throw new InvalidOperationException("BEE-700 focused screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyCurrentState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-700 focused proof recaptured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-700 focused recapture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(capture.ReducedMotion);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HotspotId);
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-700 Density / Mobile / Reduced Motion Manifest");
            builder.AppendLine();
            builder.AppendLine("## Captures recapturees uniquement");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            }

            builder.AppendLine();
            builder.AppendLine("## Runtime values");
            builder.AppendLine();
            builder.AppendLine("- Desktop bee density budget: `" + HiveViewProductUiPresenter.BeeDensityBudget.DesktopVisibleBees + "`");
            builder.AppendLine("- Portrait bee density budget: `" + HiveViewProductUiPresenter.BeeDensityBudget.PortraitVisibleBees + "`");
            builder.AppendLine("- Reduced motion portrait cap used by presenter: `min(portrait budget, 4)`");
            builder.AppendLine("- Motion kinds available outside reduced proof: `" + string.Join(", ", HiveViewProductUiPresenter.GetLiveHiveBeeMotionKindsForProof()) + "`");
            builder.AppendLine("- Reduced motion player-facing capture included: `True`");
            builder.AppendLine("- Reduced motion toggle reset after capture: `True`");
            builder.AppendLine("- Player view debug overlay visible: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine("- Feedback pulse active: `" + HiveViewProductUiPresenter.IsProductionFeedbackPulseActiveForProof() + "`");
            builder.AppendLine("- Detail panel animating: `" + HiveViewProductUiPresenter.IsProductionDetailPanelAnimatingForProof() + "`");
            builder.AppendLine();
            builder.AppendLine("## Boundary");
            builder.AppendLine();
            builder.AppendLine("- Player-facing only.");
            builder.AppendLine("- No QA/debug overlay in captured views.");
            builder.AppendLine("- Feedback, motion and reduced motion are local visual preview only.");
            builder.AppendLine("- No server authority, persistence, economy, population update or synchronization is introduced.");
            return builder.ToString();
        }

        private static string PathFor(CaptureSpec capture)
        {
            return OutputDirectory + "/" + capture.FileName;
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                Type gameViewSizeGroupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
                Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                object sizesInstance = scriptableSingletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                object androidGroupType = Enum.Parse(gameViewSizeGroupType, "Android");
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { androidGroupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) }).Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force BEE-700 focused Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
