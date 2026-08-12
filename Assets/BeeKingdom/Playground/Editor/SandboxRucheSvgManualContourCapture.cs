using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxRucheSvgManualContourCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-079_BEE1001_1020_Source/SvgManualContours";
        private const string ScreenshotPath = OutputDirectory + "/RucheSvgManualContours_1280x720.png";
        private const string ManifestPath = OutputDirectory + "/RucheSvgManualContours_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.RucheSvgManualContour.Requested";
        private const string StateFrames = "BeeKingdom.Playground.RucheSvgManualContour.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.RucheSvgManualContour.Captured";

        static SandboxRucheSvgManualContourCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Ruche SVG Manual Contours")]
        public static void CaptureRucheSvgManualContours()
        {
            Directory.CreateDirectory(OutputDirectory);
            DeleteIfExists(ScreenshotPath);
            DeleteIfExists(ManifestPath);
            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        public static void CaptureForBatch()
        {
            CaptureRucheSvgManualContours();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyRuntimeState();
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            ApplyRuntimeState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 80) return;

            try
            {
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(ScreenshotPath);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (!File.Exists(ScreenshotPath) || new FileInfo(ScreenshotPath).Length == 0)
                {
                    if (frames < 220) return;
                    throw new InvalidOperationException("SVG manual contour screenshot was not written: " + ScreenshotPath);
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("Ruche SVG manual contour screenshot captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Ruche SVG manual contour capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyRuntimeState()
        {
            TrySetGameViewSize(1280, 720, "Ruche SVG manual contours");
            Screen.SetResolution(1280, 720, false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1.08f);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Ruche SVG Manual Contours Manifest");
            builder.AppendLine();
            builder.AppendLine("- Scene: `SandboxPlayground`");
            builder.AppendLine("- Scene path: `" + ScenePath + "`");
            builder.AppendLine("- Capture: `" + ScreenshotPath + "`");
            builder.AppendLine("- SVG source: `C:/projets/beekingdom/ruche.svg`");
            builder.AppendLine("- Runtime contour JSON: `Assets/BeeKingdom/Playground/Resources/BeeKingdom/HiveVisualContours.json`");
            builder.AppendLine("- Nurserie contour source: `SVG path Nurserie`");
            builder.AppendLine("- ReserveMiel contour source: `SVG path ReserveMiel`");
            builder.AppendLine("- Runtime art layer: `PremiumBeeReference/hive-ui-target.png`");
            builder.AppendLine("- World map touched: `false`");
            builder.AppendLine("- Server/live claim: `false`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Import Status");
            builder.AppendLine();
            foreach (string row in HiveVisualContourImportRuntime.ImportStatusRows()) builder.AppendLine("- " + row);
            foreach (string id in HiveVisualContourImportRuntime.ImportedZoneIds()) builder.AppendLine("- imported_visual_zone_id:" + id);
            builder.AppendLine();
            builder.AppendLine("## Clickability Probe");
            builder.AppendLine();
            builder.AppendLine("- ReserveMiel hit_test_selects: `" + ProbeHitSelection("honey_storage") + "`");
            builder.AppendLine("- Nurserie hit_test_selects: `" + ProbeHitSelection("nursery_cluster") + "`");
            return builder.ToString();
        }

        private static string ProbeHitSelection(string hotspotId)
        {
            if (!HiveVisualContourImportRuntime.TryGetVisualContour(hotspotId, out Vector2[] contour) || contour.Length < 3) return "missing_imported_contour";

            Vector2 center = Vector2.zero;
            for (int i = 0; i < contour.Length; i++) center += contour[i];
            center /= contour.Length;

            bool selected = HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(center.x, center.y);
            return selected ? HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof() : "not_selected";
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            Type gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
            EditorWindow window = gameView == null ? null : EditorWindow.GetWindow(gameView);
            window?.Focus();
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
