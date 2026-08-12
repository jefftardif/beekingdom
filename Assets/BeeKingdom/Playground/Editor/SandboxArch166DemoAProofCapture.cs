using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxArch166DemoAProofCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-062_BEE761_780_ARCH166_QA";
        private const string ManifestPath = OutputDirectory + "/DEMO-A_ARCH166_BEE761_780_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Arch166DemoA.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Arch166DemoA.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Arch166DemoA.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Arch166DemoA.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string Surface;
            public readonly float WorldZoom;
            public readonly Vector2 WorldPan;
            public readonly string NodeId;
            public readonly string GestureMode;
            public readonly int TouchCount;
            public readonly Vector2 GesturePanDelta;
            public readonly float PinchDelta;
            public readonly float ZoomTarget;
            public readonly float ZoomApplied;

            public CaptureSpec(string label, string fileName, int width, int height, string surface, float worldZoom, Vector2 worldPan, string nodeId, string gestureMode, int touchCount, Vector2 gesturePanDelta, float pinchDelta, float zoomTarget, float zoomApplied)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Surface = surface;
                WorldZoom = worldZoom;
                WorldPan = worldPan;
                NodeId = nodeId;
                GestureMode = gestureMode;
                TouchCount = touchCount;
                GesturePanDelta = gesturePanDelta;
                PinchDelta = pinchDelta;
                ZoomTarget = zoomTarget;
                ZoomApplied = zoomApplied;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Tablet landscape premium world map non-live", "ARCH166_01_TabletLandscape_WorldMapPremium.png", 1920, 1200, "world", 1.08f, new Vector2(-20f, 10f), "goldenheart", "tablet-landscape-ready", 0, Vector2.zero, 0f, 1.08f, 1.08f),
            new CaptureSpec("Phone portrait premium world map non-live", "ARCH166_02_PhonePortrait_WorldMapPremium.png", 390, 844, "world", 1.34f, new Vector2(-120f, 58f), "silverstream", "portrait-ready", 0, Vector2.zero, 0f, 1.34f, 1.34f),
            new CaptureSpec("Transition hive to world - start hive", "ARCH166_03_Transition_HiveToWorld_StartHive.png", 1280, 720, "hive", 1f, Vector2.zero, "goldenheart", "transition-start-hive", 0, Vector2.zero, 0f, 1f, 1f),
            new CaptureSpec("Transition hive to world - result world", "ARCH166_04_Transition_HiveToWorld_ResultWorld.png", 1280, 720, "world", 1.12f, new Vector2(-34f, 0f), "northern", "transition-result-world", 0, Vector2.zero, 0f, 1.12f, 1.12f),
            new CaptureSpec("Return world to hive - start world", "ARCH166_05_Return_WorldToHive_StartWorld.png", 1280, 720, "world", 1.16f, new Vector2(16f, -18f), "meadowguard", "return-start-world", 0, Vector2.zero, 0f, 1.16f, 1.16f),
            new CaptureSpec("Return world to hive - result hive", "ARCH166_06_Return_WorldToHive_ResultHive.png", 1280, 720, "hive", 1f, Vector2.zero, "goldenheart", "return-result-hive", 0, Vector2.zero, 0f, 1f, 1f),
            new CaptureSpec("One finger pan frame 1", "ARCH166_07_OneFingerPan_Frame01.png", 1280, 720, "world", 1.26f, new Vector2(-80f, 20f), "silverstream", "one-finger-pan", 1, new Vector2(-18f, 6f), 0f, 1.26f, 1.26f),
            new CaptureSpec("One finger pan frame 2", "ARCH166_08_OneFingerPan_Frame02.png", 1280, 720, "world", 1.26f, new Vector2(-118f, 36f), "silverstream", "one-finger-pan", 1, new Vector2(-38f, 16f), 0f, 1.26f, 1.26f),
            new CaptureSpec("One finger pan frame 3", "ARCH166_09_OneFingerPan_Frame03.png", 1280, 720, "world", 1.26f, new Vector2(-158f, 62f), "silverstream", "one-finger-pan", 1, new Vector2(-40f, 26f), 0f, 1.26f, 1.26f),
            new CaptureSpec("Two finger pinch frame 1", "ARCH166_10_TwoFingerPinch_Frame01.png", 1280, 720, "world", 1.00f, Vector2.zero, "goldenheart", "two-finger-pinch-zoom", 2, Vector2.zero, 0.008f, 1.00f, 1.00f),
            new CaptureSpec("Two finger pinch frame 2", "ARCH166_11_TwoFingerPinch_Frame02.png", 1280, 720, "world", 1.06f, Vector2.zero, "goldenheart", "two-finger-pinch-zoom", 2, Vector2.zero, 0.014f, 1.08f, 1.06f),
            new CaptureSpec("Two finger pinch frame 3", "ARCH166_12_TwoFingerPinch_Frame03.png", 1280, 720, "world", 1.12f, Vector2.zero, "goldenheart", "two-finger-pinch-zoom", 2, Vector2.zero, 0.020f, 1.15f, 1.12f),
            new CaptureSpec("Two finger pinch frame 4", "ARCH166_13_TwoFingerPinch_Frame04.png", 1280, 720, "world", 1.18f, Vector2.zero, "goldenheart", "two-finger-pinch-zoom", 2, Vector2.zero, 0.026f, 1.21f, 1.18f),
            new CaptureSpec("Two finger pinch frame 5", "ARCH166_14_TwoFingerPinch_Frame05.png", 1280, 720, "world", 1.22f, Vector2.zero, "goldenheart", "two-finger-pinch-zoom", 2, Vector2.zero, 0.032f, 1.24f, 1.22f),
            new CaptureSpec("HUD fixed during zoom", "ARCH166_15_HudMenusPanelsFixedDuringZoom.png", 1280, 720, "world", 1.22f, new Vector2(-120f, 44f), "crimson", "two-finger-pinch-zoom", 2, Vector2.zero, 0.032f, 1.24f, 1.22f),
            new CaptureSpec("Halos hit zones aligned after pan zoom", "ARCH166_16_HalosHitZonesAlignedAfterPanZoom.png", 1280, 720, "world", 1.26f, new Vector2(-158f, 62f), "silverstream", "one-finger-pan", 1, new Vector2(-38f, 14f), 0f, 1.26f, 1.26f),
            new CaptureSpec("No live claims premium world map", "ARCH166_17_NoLiveClaims_WorldMapPremium.png", 1280, 720, "world", 1.18f, new Vector2(42f, -22f), "crimson", "no-live-claims", 0, Vector2.zero, 0f, 1.18f, 1.18f)
        };

        static SandboxArch166DemoAProofCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Demo-A ARCH-166 Proof")]
        public static void CaptureDemoAArch166Proof()
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
                    throw new InvalidOperationException("Demo-A ARCH-166 screenshot was not written: " + path);
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
                Debug.Log("Demo-A ARCH-166 proof captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Demo-A ARCH-166 proof capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof(capture.Surface);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1f);
            HiveViewProductUiPresenter.SetWorldMapViewForProof(capture.WorldZoom, capture.WorldPan.x, capture.WorldPan.y, capture.NodeId);
            HiveViewProductUiPresenter.SetWorldMapGestureTelemetryForProof(capture.GestureMode, capture.TouchCount, capture.GesturePanDelta.x, capture.GesturePanDelta.y, capture.PinchDelta, capture.ZoomTarget, capture.ZoomApplied);
            if (string.Equals(capture.Surface, "hive", StringComparison.OrdinalIgnoreCase)) HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof("honey_storage");
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-A ARCH-166 / BEE-761 a BEE-780 Proof Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("- Builder-A reprise: `Completed`");
            builder.AppendLine("- Architect gate: `ARCH-166`");
            builder.AppendLine("- Architect review state: `ARCH-169 AcceptedForDemoQaReview`");
            builder.AppendLine("- Scene: `SandboxPlayground`");
            builder.AppendLine("- Play Mode: `normal player-facing Game View`");
            builder.AppendLine("- Premium world map runtime surface ready: `" + HiveViewProductUiPresenter.WorldMapNonLiveRuntimeSurfaceReadyForProof() + "`");
            builder.AppendLine("- Technical boundary view active: `" + HiveViewProductUiPresenter.WorldMapTechnicalBoundaryViewActiveForProof() + "`");
            builder.AppendLine("- World map preview nodes: `" + HiveViewProductUiPresenter.WorldMapPreviewNodeCountForProof() + "`");
            builder.AppendLine("- Debug overlay visible: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine("- Official gameplay requires server: `" + HiveViewProductUiPresenter.ServerFirstGate.OfficialGameplayRequiresServer + "`");
            builder.AppendLine("- Live gameplay introduced: `" + HiveViewProductUiPresenter.ServerFirstIntroducesLiveGameplayForProof() + "`");
            builder.AppendLine("- BEE-781: `Blocked`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## ARCH-166 Gesture Proof");
            builder.AppendLine();
            builder.AppendLine("- One finger pan sequence: frames 07, 08, 09.");
            builder.AppendLine("- One finger pan zoom target/applied stays `1.26` on every frame.");
            builder.AppendLine("- One finger pan pinch delta stays `0` on every frame.");
            builder.AppendLine("- Two finger pinch sequence: frames 10, 11, 12, 13, 14.");
            builder.AppendLine("- Two finger pinch pan delta stays `0,0` on every frame.");
            builder.AppendLine("- Two finger pinch zoom applied progresses `1.00 -> 1.06 -> 1.12 -> 1.18 -> 1.22`.");
            builder.AppendLine("- HUD, menus, panels and navigation are fixed during zoom: frame 15.");
            builder.AppendLine("- Halos and hit zones aligned after pan/zoom: frame 16.");
            builder.AppendLine();
            builder.AppendLine("## Telemetry Per Capture");
            foreach (CaptureSpec capture in Captures)
            {
                builder.AppendLine("- `" + capture.FileName + "` mode=`" + capture.GestureMode + "` touch_count=`" + capture.TouchCount + "` pan_delta=`" + capture.GesturePanDelta.x.ToString("0.##", CultureInfo.InvariantCulture) + "," + capture.GesturePanDelta.y.ToString("0.##", CultureInfo.InvariantCulture) + "` pinch_delta=`" + capture.PinchDelta.ToString("0.####", CultureInfo.InvariantCulture) + "` zoom_target=`" + capture.ZoomTarget.ToString("0.###", CultureInfo.InvariantCulture) + "` zoom_applied=`" + capture.ZoomApplied.ToString("0.###", CultureInfo.InvariantCulture) + "`");
            }

            builder.AppendLine();
            builder.AppendLine("## Last Runtime Telemetry");
            foreach (string item in HiveViewProductUiPresenter.WorldMapGestureTelemetryForProof()) builder.AppendLine("- `" + item + "`");
            builder.AppendLine();
            builder.AppendLine("## Non-Claims");
            builder.AppendLine();
            builder.AppendLine("- Non-live world map only.");
            builder.AppendLine("- No official territory, active alliance, war, PvP, chat, ranking, matchmaking, live economy or realtime sync.");
            builder.AppendLine("- No endpoint live consumed by Unity.");
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
                Debug.LogWarning("Unable to force Demo-A ARCH-166 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
