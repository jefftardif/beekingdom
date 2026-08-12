using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee1020PixelContourProofCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-079_BEE1001_1020_Source/NativeAfter";
        private const string ManifestPath = OutputDirectory + "/DEMO-079_NativeAfterCaptureConditions.md";
        private const string JsonManifestPath = OutputDirectory + "/DEMO-079_NativeAfterCaptureManifest.json";
        private const string StateRequested = "BeeKingdom.Playground.BEE1020PixelContour.Requested";
        private const string StateFrames = "BeeKingdom.Playground.BEE1020PixelContour.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.BEE1020PixelContour.Captured";
        private const string StateIndex = "BeeKingdom.Playground.BEE1020PixelContour.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly Vector2 Pan;
            public readonly float Zoom;

            public CaptureSpec(string id, string label, string fileName, int width, int height, string hotspotId, Vector2 pan, float zoom)
            {
                Id = id;
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                Pan = pan;
                Zoom = zoom;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("AFTER_ReserveMiel", "Reserve miel native AFTER", "AFTER_ReserveMiel.png", 1280, 720, "honey_storage", Vector2.zero, 1.10f),
            new CaptureSpec("AFTER_Administration", "Administration native AFTER", "AFTER_Administration.png", 1280, 720, "administration_core", Vector2.zero, 1.10f),
            new CaptureSpec("AFTER_Nurserie", "Nurserie native AFTER", "AFTER_Nurserie.png", 1280, 720, "nursery_cluster", new Vector2(8f, 0f), 1.12f),
            new CaptureSpec("AFTER_Caserne", "Caserne native AFTER", "AFTER_Caserne.png", 1280, 720, "guard_post", new Vector2(-18f, 8f), 1.14f),
            new CaptureSpec("AFTER_Recherche", "Recherche native AFTER", "AFTER_Recherche.png", 1280, 720, "research_node", new Vector2(-26f, 12f), 1.14f),
            new CaptureSpec("AFTER_Genetique", "Genetique native AFTER", "AFTER_Genetique.png", 1280, 720, "genetics_garden", new Vector2(-36f, 6f), 1.12f),
            new CaptureSpec("AFTER_PanZoom_Alignment", "Pan zoom contour alignment native AFTER", "AFTER_PanZoom_Alignment.png", 1280, 720, "research_node", new Vector2(-42f, 18f), 1.32f)
        };

        static SandboxBee1020PixelContourProofCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-079 Pixel Contours")]
        public static void CaptureDemo079PixelContours()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(JsonManifestPath);
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

        public static void CaptureDemo079NativeAfterForBatch()
        {
            CaptureDemo079PixelContours();
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
            if (frames < 80) return;

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
                    if (frames < 220) return;
                    throw new InvalidOperationException("DEMO-079 screenshot was not written: " + path);
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
                File.WriteAllText(JsonManifestPath, BuildJsonManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-079 pixel contour screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-079 pixel contour capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-079 Pixel Contour Capture Manifest");
            builder.AppendLine();
            builder.AppendLine("- Scope: `playable_hive_only`");
            builder.AppendLine("- Source: `SandboxBee1020PixelContourProofCapture`");
            builder.AppendLine("- Surface: `SandboxPlayground Play Mode local`");
            builder.AppendLine("- Capture directory: `" + OutputDirectory + "`");
            builder.AppendLine("- Native AFTER capture: `true`");
            builder.AppendLine("- External post-produced overlay: `false`");
            builder.AppendLine("- Fresh organic BEE-1022 status: `ARCH-238_VALIDATED_ORGANIC_CONTOURS`");
            builder.AppendLine("- World map relaunched: `false`");
            builder.AppendLine("- BEE-881 created or unlocked: `false`");
            builder.AppendLine("- Official server live: `false`");
            builder.AppendLine("- Official endpoint: `false`");
            builder.AppendLine("- Official save/economy/army persistence: `false`");
            builder.AppendLine("- Hitbox visible: `false`");
            builder.AppendLine("- Generic circle halo final: `false`");
            builder.AppendLine("- Contour schema: `" + HivePixelPerfectContourCalibration.SchemaVersion + "`");
            builder.AppendLine("- Coordinate space: `" + HivePixelPerfectContourCalibration.CoordinateSpace + "`");
            builder.AppendLine("- QA proof finalization: `NO - ARCH-240 requires UI-B visual contour source before final proof`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                ApplyProofScenario(capture);
                Vector2[] visual = HiveViewProductUiPresenter.GetReferenceHotspotPolygonForProof(capture.HotspotId);
                Vector2[] hitbox = HiveViewProductUiPresenter.GetReferenceHotspotTactileHitboxForProof(capture.HotspotId);
                Vector2Int size = ReadPngSize(PathFor(capture), capture.Width, capture.Height);
                FileInfo file = new FileInfo(PathFor(capture));
                builder.AppendLine("### " + capture.Id);
                builder.AppendLine();
                builder.AppendLine("- label: `" + capture.Label + "`");
                builder.AppendLine("- hotspot_id: `" + capture.HotspotId + "`");
                builder.AppendLine("- file: `" + PathFor(capture) + "`");
                builder.AppendLine("- file_exists: `" + File.Exists(PathFor(capture)).ToString() + "`");
                builder.AppendLine("- file_size_bytes: `" + (file.Exists ? file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + "`");
                builder.AppendLine("- actual_dimensions: `" + size.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + size.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- visual_contour_points: `" + visual.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- tactile_hitbox_points: `" + hitbox.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- visual_and_hitbox_separated: `" + (Vector2.Distance(visual[0], hitbox[0]) > 1f).ToString() + "`");
                builder.AppendLine("- hitbox_visible: `false`");
                builder.AppendLine("- local_demo_only: `true`");
                builder.AppendLine();
            }

            builder.AppendLine("## Runtime Rows");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_079_NATIVE_AFTER_CAPTURE = NO");
            builder.AppendLine("Reason: ARCH-240 stops final capture claims until UI-B visual contour source is validated.");
            return builder.ToString();
        }

        private static string BuildJsonManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"schema\": \"bee-kingdom.demo079.native-after-capture.v1\",");
            builder.AppendLine("  \"createdBy\": \"Builder-B\",");
            builder.AppendLine("  \"captureTool\": \"SandboxBee1020PixelContourProofCapture\",");
            builder.AppendLine("  \"captureDirectory\": \"" + JsonEscape(OutputDirectory) + "\",");
            builder.AppendLine("  \"surface\": \"SandboxPlayground Play Mode local\",");
            builder.AppendLine("  \"nativeAfterCapture\": true,");
            builder.AppendLine("  \"externalPostProducedOverlay\": false,");
            builder.AppendLine("  \"bee1022OrganicContoursStatus\": \"ARCH-238_VALIDATED_ORGANIC_CONTOURS\",");
            builder.AppendLine("  \"readyForDemo079NativeAfterCapture\": false,");
            builder.AppendLine("  \"readyReason\": \"ARCH-240 retrogrades native captures to technical evidence until UI-B visual contour source is validated.\",");
            builder.AppendLine("  \"arch240VisualSourceRequired\": true,");
            builder.AppendLine("  \"qaReadyWithoutUiBVisualSource\": false,");
            builder.AppendLine("  \"nonClaims\": {");
            builder.AppendLine("    \"worldMapRuntime\": false,");
            builder.AppendLine("    \"bee881CreatedOrUnlocked\": false,");
            builder.AppendLine("    \"officialServerLive\": false,");
            builder.AppendLine("    \"officialEndpoint\": false,");
            builder.AppendLine("    \"officialSave\": false,");
            builder.AppendLine("    \"officialEconomy\": false,");
            builder.AppendLine("    \"officialPersistentArmy\": false");
            builder.AppendLine("  },");
            builder.AppendLine("  \"captures\": [");
            for (int i = 0; i < Captures.Length; i++)
            {
                CaptureSpec capture = Captures[i];
                FileInfo file = new FileInfo(PathFor(capture));
                Vector2Int size = ReadPngSize(PathFor(capture), capture.Width, capture.Height);
                builder.AppendLine("    {");
                builder.AppendLine("      \"id\": \"" + JsonEscape(capture.Id) + "\",");
                builder.AppendLine("      \"label\": \"" + JsonEscape(capture.Label) + "\",");
                builder.AppendLine("      \"hotspotId\": \"" + JsonEscape(capture.HotspotId) + "\",");
                builder.AppendLine("      \"path\": \"" + JsonEscape(PathFor(capture)) + "\",");
                builder.AppendLine("      \"exists\": " + JsonBool(file.Exists) + ",");
                builder.AppendLine("      \"sizeBytes\": " + (file.Exists ? file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + ",");
                builder.AppendLine("      \"requestedWidth\": " + capture.Width.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"requestedHeight\": " + capture.Height.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"actualWidth\": " + size.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"actualHeight\": " + size.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"zoom\": " + capture.Zoom.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"panX\": " + capture.Pan.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"panY\": " + capture.Pan.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                builder.AppendLine("    }" + (i < Captures.Length - 1 ? "," : string.Empty));
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static void ApplyProofScenario(CaptureSpec capture)
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
        }

        private static string PathFor(CaptureSpec capture)
        {
            return OutputDirectory + "/" + capture.FileName;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string JsonEscape(string value)
        {
            return value.Replace("\\", "/").Replace("\"", "\\\"");
        }

        private static Vector2Int ReadPngSize(string path, int fallbackWidth, int fallbackHeight)
        {
            if (!File.Exists(path)) return new Vector2Int(fallbackWidth, fallbackHeight);
            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(bytes)) return new Vector2Int(fallbackWidth, fallbackHeight);
                return new Vector2Int(texture.width, texture.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Type gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
                EditorWindow window = gameView == null ? null : EditorWindow.GetWindow(gameView);
                if (window != null)
                {
                    window.minSize = new Vector2(width, height);
                    window.maxSize = new Vector2(width, height);
                    window.titleContent = new GUIContent(label);
                    window.Repaint();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not resize Game View for DEMO-079 capture: " + exception.Message);
            }
        }
    }
}
