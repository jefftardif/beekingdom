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
    public static class WorldMapWave6ExactCropTerrainProofCapture
    {
        private const string OutputDirectory = "C:/projets/beekingdomgame-master/Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/UnityGameViewProof";
        private const string ReceiptPath = OutputDirectory + "/Wave6_50x50_ExactCropUnityGameViewProof_20260717.txt";
        private const string StateRequested = "BeeKingdom.WorldMapWave6ExactCropTerrainProof.Requested";
        private const string StateFrames = "BeeKingdom.WorldMapWave6ExactCropTerrainProof.Frames";
        private const string StateCaptured = "BeeKingdom.WorldMapWave6ExactCropTerrainProof.Captured";
        private const string StateIndex = "BeeKingdom.WorldMapWave6ExactCropTerrainProof.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int ChunkX;
            public readonly int ChunkY;
            public readonly float Zoom;
            public readonly int Width;
            public readonly int Height;

            public CaptureSpec(string label, string fileName, int chunkX, int chunkY, float zoom, int width, int height)
            {
                Label = label;
                FileName = fileName;
                ChunkX = chunkX;
                ChunkY = chunkY;
                Zoom = zoom;
                Width = width;
                Height = height;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("North-east C54_09 waterfall/forest seam", "wave6_exactcrop_gameview_C54_09.png", 54, 9, 0.58f, 1920, 1080),
            new CaptureSpec("Reported upside-down mountain zone C53_26", "wave6_exactcrop_gameview_C53_26.png", 53, 26, 0.58f, 1920, 1080),
            new CaptureSpec("Reported junction zone C52_19", "wave6_exactcrop_gameview_C52_19.png", 52, 19, 0.58f, 1920, 1080),
            new CaptureSpec("Reported far south-east junction zone C52_52", "wave6_exactcrop_gameview_C52_52.png", 52, 52, 0.58f, 1920, 1080),
            new CaptureSpec("Reported right-edge junction zone C48_46", "wave6_exactcrop_gameview_C48_46.png", 48, 46, 0.58f, 1920, 1080),
            new CaptureSpec("Central control view", "wave6_exactcrop_gameview_C32_32.png", 32, 32, 0.58f, 1920, 1080)
        };

        static WorldMapWave6ExactCropTerrainProofCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/World Map/Capture Wave6 50x50 Exact Crop Terrain Proof")]
        public static void CaptureExactCropTerrainProof()
        {
            Directory.CreateDirectory(OutputDirectory);
            for (int i = 0; i < Captures.Length; i++) DeleteIfExists(PathFor(Captures[i]));
            DeleteIfExists(ReceiptPath);

            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;

            EditorSceneManager.OpenScene(WorldMapWave6Premium50x50TestBootstrap.ScenePath);
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
            if (frames < 90) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    SandboxGameViewScreenshotWriter.Request(path);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 220) return;
                    throw new InvalidOperationException("Unity GameView screenshot was not written: " + path);
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

                File.WriteAllText(ReceiptPath, BuildReceipt(), new UTF8Encoding(false));
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("[Wave6 50x50 Exact Crop] Unity GameView proof captured. Receipt: " + ReceiptPath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                WriteFailure(exception);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("[Wave6 50x50 Exact Crop] Unity GameView proof failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, "Wave6 exact crop " + capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);

            WorldMapWave6Premium50x50TestBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapWave6Premium50x50TestBootstrap>();
            if (bootstrap == null) return;
            bootstrap.SetProofView(capture.ChunkX, capture.ChunkY, capture.Zoom, false, true);
        }

        private static string BuildReceipt()
        {
            var builder = new StringBuilder();
            builder.AppendLine("WORLD_MAP_WAVE6_50X50_EXACT_CROP_UNITY_GAMEVIEW_PROOF=PASS");
            builder.AppendLine("timestamp_utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("READY_FOR_QA_BUILDERC=NO");
            builder.AppendLine("READY_FOR_UNITY_HANDOFF=NO");
            builder.AppendLine("scene_path=" + WorldMapWave6Premium50x50TestBootstrap.ScenePath);
            builder.AppendLine("runtime_root=" + WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewResourceRoot);
            builder.AppendLine("runtime_sha256=" + WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256);
            builder.AppendLine("unity_render_contract=GutterWorldRect + FullTextureUv + clamp/bilinear/no-mips");
            builder.AppendLine("gutter_rendering=true");
            builder.AppendLine("tile_guides=false");
            builder.AppendLine("capture_resolution=1920x1080");
            builder.AppendLine("note=These captures prove the terrain-only exact-crop Unity path, not the obsolete V2I repair audit scene.");
            for (int i = 0; i < Captures.Length; i++)
            {
                CaptureSpec capture = Captures[i];
                builder.AppendLine("capture_" + (i + 1).ToString("00", CultureInfo.InvariantCulture) + "=" + capture.Label + "|C" + capture.ChunkX.ToString("00", CultureInfo.InvariantCulture) + "_" + capture.ChunkY.ToString("00", CultureInfo.InvariantCulture) + "|" + PathFor(capture));
            }

            return builder.ToString();
        }

        private static void WriteFailure(Exception exception)
        {
            Directory.CreateDirectory(OutputDirectory);
            var builder = new StringBuilder();
            builder.AppendLine("WORLD_MAP_WAVE6_50X50_EXACT_CROP_UNITY_GAMEVIEW_PROOF=FAIL");
            builder.AppendLine("timestamp_utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("scene_path=" + WorldMapWave6Premium50x50TestBootstrap.ScenePath);
            builder.AppendLine("error=" + exception);
            File.WriteAllText(ReceiptPath, builder.ToString(), new UTF8Encoding(false));
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
                object standaloneGroupType = Enum.Parse(gameViewSizeGroupType, "Standalone");
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { standaloneGroupType });
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
                Debug.LogWarning("Unable to force Wave6 exact-crop Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
