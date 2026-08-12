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
    public static class SandboxBee720RuntimeBridgeCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-057_BEE701_720_RuntimeBridge";
        private const string ManifestPath = OutputDirectory + "/BEE-720_RuntimeBridge_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee720RuntimeBridge.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee720RuntimeBridge.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee720RuntimeBridge.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee720RuntimeBridge.Index";

        private struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly RuntimeBridgePlayerMode Mode;
            public readonly string HotspotId;
            public readonly Vector2 Pan;
            public readonly bool ReducedMotion;

            public CaptureSpec(string label, string fileName, int width, int height, RuntimeBridgePlayerMode mode, string hotspotId, Vector2 pan, bool reducedMotion)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Mode = mode;
                HotspotId = hotspotId;
                Pan = pan;
                ReducedMotion = reducedMotion;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Runtime bridge desktop", "BEE-720_01_RuntimeBridgeDesktop.png", 1280, 720, RuntimeBridgePlayerMode.ServerPreparation, "honey_storage", Vector2.zero, false),
            new CaptureSpec("Offline fallback portrait", "BEE-720_02_OfflineFallbackPortrait.png", 390, 844, RuntimeBridgePlayerMode.OfflineFallback, "wax_workshop", new Vector2(-210f, 80f), false),
            new CaptureSpec("MMO shell preview", "BEE-720_03_MmoShellPreview.png", 1280, 720, RuntimeBridgePlayerMode.LocalPreview, "alliance_future_hall", Vector2.zero, false),
            new CaptureSpec("Reduced motion bridge portrait", "BEE-720_04_ReducedMotionBridgePortrait.png", 390, 844, RuntimeBridgePlayerMode.ServerPreparation, "honey_storage", new Vector2(-170f, 60f), true)
        };

        static SandboxBee720RuntimeBridgeCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-720 Runtime Bridge")]
        public static void CaptureBee720RuntimeBridge()
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
                    throw new InvalidOperationException("BEE-720 screenshot was not written: " + path);
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
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-720 runtime bridge captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-720 runtime bridge capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(capture.Mode);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(capture.ReducedMotion);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HotspotId);
        }

        private static string BuildManifest()
        {
            RuntimeBridgePlayerFacingState state = HiveViewProductUiPresenter.RuntimeBridgePlayerState;
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-720 Runtime Bridge Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("- Builder implementation: `Completed`");
            builder.AppendLine("- Gate verdict: `" + HiveViewProductUiPresenter.RuntimeBridgeGate.Verdict + "`");
            builder.AppendLine("- BEE-721: `Blocked`");
            builder.AppendLine("- BEE-700 baseline preserved: `" + HiveViewProductUiPresenter.RuntimeBridgeEvidence.Bee700BaselinePreserved + "`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Bridge Proof");
            builder.AppendLine();
            builder.AppendLine("- Contract count: `" + HiveViewProductUiPresenter.GetProductionRuntimeBridgeContractNamesForProof().Length + "`");
            builder.AppendLine("- Current player state: `" + state.Mode + "`");
            builder.AppendLine("- Offline consultation available: `" + state.OfflineConsultationAvailable + "`");
            builder.AppendLine("- Official gameplay requires server: `" + state.OfficialGameplayRequiresServer + "`");
            builder.AppendLine("- Gameplay mutation allowed: `" + state.GameplayMutationAllowed + "`");
            builder.AppendLine("- Live gameplay introduced: `" + HiveViewProductUiPresenter.RuntimeBridgeIntroducesLiveGameplayForProof() + "`");
            builder.AppendLine("- Debug overlay visible in player view: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine();
            builder.AppendLine("## Non-Claims");
            builder.AppendLine();
            foreach (string nonClaim in HiveViewProductUiPresenter.RuntimeBridgeEvidence.NonClaims) builder.AppendLine("- `" + nonClaim + "`");
            builder.AppendLine("- Offline mode is consultation/demo only: no progression, no save, no economy.");
            builder.AppendLine("- Server connection is the official future gameplay route, but no live account, official save, live economy, live alliance, chat, PvP, ranking, matchmaking or realtime synchronization is introduced by Unity.");
            builder.AppendLine();
            builder.AppendLine("## Server Handoff");
            builder.AppendLine();
            builder.AppendLine("- Unity routes authoritative, persistent or protocol work to Bee Server.");
            builder.AppendLine("- Expected server follow-up: SERVER-021 / SERVER-022 / SERVER-023.");
            return builder.ToString();
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
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
                Debug.LogWarning("Unable to force BEE-720 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
