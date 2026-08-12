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
    public static class SandboxBee760PremiumWorldMapCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-061_BEE760_PremiumWorldMapNonLive";
        private const string ManifestPath = OutputDirectory + "/BEE-760_PremiumWorldMapNonLive_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee760PremiumWorldMap.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee760PremiumWorldMap.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee760PremiumWorldMap.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee760PremiumWorldMap.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string SurfaceMode;
            public readonly string HiveHotspotId;
            public readonly Vector2 HivePan;
            public readonly float HiveZoom;
            public readonly float WorldZoom;
            public readonly Vector2 WorldPan;
            public readonly string WorldNodeId;

            public CaptureSpec(string label, string fileName, int width, int height, string surfaceMode, string hiveHotspotId, Vector2 hivePan, float hiveZoom, float worldZoom, Vector2 worldPan, string worldNodeId)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                SurfaceMode = surfaceMode;
                HiveHotspotId = hiveHotspotId;
                HivePan = hivePan;
                HiveZoom = hiveZoom;
                WorldZoom = worldZoom;
                WorldPan = worldPan;
                WorldNodeId = worldNodeId;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Tablet landscape premium world map non-live", "BEE-760_01_TabletLandscape_PremiumWorldMap_NonLive.png", 1920, 1200, "world", "honey_storage", Vector2.zero, 1f, 1.08f, new Vector2(-20f, 10f), "goldenheart"),
            new CaptureSpec("Phone portrait premium world map non-live", "BEE-760_02_PhonePortrait_PremiumWorldMap_NonLive.png", 390, 844, "world", "honey_storage", Vector2.zero, 1f, 1.34f, new Vector2(-120f, 58f), "silverstream"),
            new CaptureSpec("Transition hive to world start", "BEE-760_03_Transition_HiveToWorld_StartHive.png", 1280, 720, "hive", "honey_storage", Vector2.zero, 1f, 1f, Vector2.zero, "goldenheart"),
            new CaptureSpec("Transition hive to world result", "BEE-760_04_Transition_HiveToWorld_ResultWorld.png", 1280, 720, "world", "honey_storage", Vector2.zero, 1f, 1.12f, new Vector2(-34f, 0f), "northern"),
            new CaptureSpec("Return world to hive start", "BEE-760_05_Return_WorldToHive_StartWorld.png", 1280, 720, "world", "alliance_future_hall", Vector2.zero, 1f, 1.16f, new Vector2(16f, -18f), "meadowguard"),
            new CaptureSpec("Return world to hive result", "BEE-760_06_Return_WorldToHive_ResultHive.png", 1280, 720, "hive", "alliance_future_hall", Vector2.zero, 1.08f, 1f, Vector2.zero, "goldenheart"),
            new CaptureSpec("Premium world map no live claims", "BEE-760_07_NoLiveClaims_PremiumWorldMap.png", 1280, 720, "world", "guard_post", Vector2.zero, 1f, 1.22f, new Vector2(44f, -22f), "crimson")
        };

        static SandboxBee760PremiumWorldMapCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-760 Premium World Map Non-Live")]
        public static void CaptureBee760PremiumWorldMap()
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
                    throw new InvalidOperationException("BEE-760 premium world map screenshot was not written: " + path);
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
                Debug.Log("BEE-760 premium world map non-live captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-760 premium world map non-live capture failed: " + exception);
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
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof(capture.SurfaceMode);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.HivePan.x, capture.HivePan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.HiveZoom);
            HiveViewProductUiPresenter.SetWorldMapViewForProof(capture.WorldZoom, capture.WorldPan.x, capture.WorldPan.y, capture.WorldNodeId);
            HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HiveHotspotId);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-760 Premium World Map Non-Live Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("- Scene: `SandboxPlayground`");
            builder.AppendLine("- Play Mode: `normal player-facing Game View`");
            builder.AppendLine("- Premium world map runtime surface ready: `" + HiveViewProductUiPresenter.WorldMapNonLiveRuntimeSurfaceReadyForProof() + "`");
            builder.AppendLine("- Technical boundary view active: `" + HiveViewProductUiPresenter.WorldMapTechnicalBoundaryViewActiveForProof() + "`");
            builder.AppendLine("- World map preview nodes: `" + HiveViewProductUiPresenter.WorldMapPreviewNodeCountForProof() + "`");
            builder.AppendLine("- Debug overlay visible: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine("- Official gameplay requires server: `" + HiveViewProductUiPresenter.ServerFirstGate.OfficialGameplayRequiresServer + "`");
            builder.AppendLine("- Live gameplay introduced: `" + HiveViewProductUiPresenter.ServerFirstIntroducesLiveGameplayForProof() + "`");
            builder.AppendLine("- BEE-761: `Blocked`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Readiness");
            builder.AppendLine();
            foreach (string line in HiveViewProductUiPresenter.WorldMapScalableReadinessForProof()) builder.AppendLine("- `" + line + "`");
            builder.AppendLine();
            builder.AppendLine("## Non-Claims");
            builder.AppendLine();
            builder.AppendLine("- No live territory, war, scouting, economy, chat, ranking, matchmaking, official account or realtime sync.");
            builder.AppendLine("- Premium world map is player-facing non-live evidence, not production MMO gameplay.");
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
                Debug.LogWarning("Unable to force BEE-760 premium world map Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
