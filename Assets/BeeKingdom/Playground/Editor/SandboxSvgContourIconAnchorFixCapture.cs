using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxSvgContourIconAnchorFixCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-084_SvgContourIconAnchorFix";
        private const string ManifestPath = OutputDirectory + "/DEMO-084_SvgContourIconAnchorFix_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.SvgContourIconAnchorFix.Requested";
        private const string StateFrames = "BeeKingdom.Playground.SvgContourIconAnchorFix.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.SvgContourIconAnchorFix.Captured";
        private const string StateIndex = "BeeKingdom.Playground.SvgContourIconAnchorFix.Index";

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
            new CaptureSpec("DesktopGameView", "Desktop Game View", "DEMO084_DesktopGameView_1280x720.png", 1280, 720, "honey_storage", Vector2.zero, 1.08f),
            new CaptureSpec("TabletLandscape", "Tablet landscape", "DEMO084_TabletLandscape_1920x1200.png", 1920, 1200, "honey_storage", Vector2.zero, 1.05f),
            new CaptureSpec("PhonePortrait", "Phone portrait", "DEMO084_PhonePortrait_390x844.png", 390, 844, "guard_post", new Vector2(-160f, 48f), 1.16f),
            new CaptureSpec("AcademieSelected", "Academie selected", "DEMO084_Academie_Selected_1280x720.png", 1280, 720, "academy_canopy", new Vector2(110f, -6f), 1.16f),
            new CaptureSpec("ReserveMielSelected", "Reserve miel selected", "DEMO084_ReserveMiel_Selected_1280x720.png", 1280, 720, "honey_storage", Vector2.zero, 1.12f),
            new CaptureSpec("NurserieSelected", "Nurserie selected", "DEMO084_Nurserie_Selected_1280x720.png", 1280, 720, "nursery_cluster", new Vector2(-56f, 8f), 1.12f),
            new CaptureSpec("CaserneSelected", "Caserne selected", "DEMO084_Caserne_Selected_1280x720.png", 1280, 720, "guard_post", new Vector2(74f, 4f), 1.12f),
            new CaptureSpec("AcademieZoom", "Academie contour and icon zoom", "DEMO084_Academie_Zoom_1280x720.png", 1280, 720, "academy_canopy", new Vector2(164f, -18f), 1.42f)
        };

        static SandboxSvgContourIconAnchorFixCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-084 SVG Contour Icon Anchor Fix")]
        public static void CaptureSvgContourIconAnchorFix()
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

        public static void CaptureSvgContourIconAnchorFixForBatch()
        {
            CaptureSvgContourIconAnchorFix();
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
                    if (frames < 240) return;
                    throw new InvalidOperationException("DEMO-084 screenshot was not written: " + path);
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
                Debug.Log("DEMO-084 SVG contour icon anchor screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-084 SVG contour icon anchor capture failed: " + exception);
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
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_session_start_collect");
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-084 SVG Contour Icon Anchor Fix Manifest");
            builder.AppendLine();
            builder.AppendLine("- Date: 2026-07-13");
            builder.AppendLine("- Surface: `SandboxPlayground Play Mode local`");
            builder.AppendLine("- Runtime background: `PremiumBeeReference/bg_beehive`");
            builder.AppendLine("- Runtime contour JSON: `Assets/BeeKingdom/Playground/Resources/BeeKingdom/HiveVisualContours.json`");
            builder.AppendLine("- Builder-A report: `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_SvgContourIconAnchorFix_Report.md`");
            builder.AppendLine("- Official server/live claim: `false`");
            builder.AppendLine("- World map relaunched: `false`");
            builder.AppendLine("- BEE-881 created or unlocked: `false`");
            builder.AppendLine("- Pixel-perfect artistic claim beyond supplied SVG: `false`");
            builder.AppendLine();
            builder.AppendLine("## Import Status");
            builder.AppendLine();
            foreach (string row in HiveVisualContourImportRuntime.ImportStatusRows()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Zone Anchor Checks");
            builder.AppendLine();
            builder.AppendLine("| Zone | Imported SVG contour | Visual anchor available | Capture |");
            builder.AppendLine("| --- | --- | --- | --- |");
            AppendZoneRow(builder, "academy_canopy", "Academie", "DEMO084_Academie_Selected_1280x720.png");
            AppendZoneRow(builder, "honey_storage", "ReserveMiel", "DEMO084_ReserveMiel_Selected_1280x720.png");
            AppendZoneRow(builder, "nursery_cluster", "Nurserie", "DEMO084_Nurserie_Selected_1280x720.png");
            AppendZoneRow(builder, "guard_post", "Caserne", "DEMO084_Caserne_Selected_1280x720.png");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                Vector2Int size = ReadPngSize(PathFor(capture), capture.Width, capture.Height);
                FileInfo file = new FileInfo(PathFor(capture));
                builder.AppendLine("### " + capture.Id);
                builder.AppendLine();
                builder.AppendLine("- label: `" + capture.Label + "`");
                builder.AppendLine("- file: `" + PathFor(capture) + "`");
                builder.AppendLine("- file_exists: `" + File.Exists(PathFor(capture)).ToString() + "`");
                builder.AppendLine("- file_size_bytes: `" + (file.Exists ? file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + "`");
                builder.AppendLine("- actual_dimensions: `" + size.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + size.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- hotspot: `" + capture.HotspotId + "`");
                builder.AppendLine("- zoom: `" + capture.Zoom.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine();
            }

            builder.AppendLine("READY_FOR_QA_NEW_HIVE_BACKGROUND_SVG_ZONES = YES");
            return builder.ToString();
        }

        private static void AppendZoneRow(StringBuilder builder, string hotspotId, string label, string fileName)
        {
            bool hasContour = HiveVisualContourImportRuntime.TryGetVisualContour(hotspotId, out Vector2[] contour) && contour.Length >= 3;
            bool hasAnchor = HiveVisualContourImportRuntime.TryGetVisualAnchor(hotspotId, out Vector2 anchor);
            string anchorText = hasAnchor
                ? anchor.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "," + anchor.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                : "missing";
            builder.AppendLine("| " + label + " | `" + hasContour.ToString() + "` | `" + hasAnchor.ToString() + " / " + anchorText + "` | `" + OutputDirectory + "/" + fileName + "` |");
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static string PathFor(CaptureSpec capture)
        {
            return OutputDirectory + "/" + capture.FileName;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
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
                Debug.LogWarning("Could not resize Game View for DEMO-084 capture: " + exception.Message);
            }
        }
    }
}
