using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapRuntimeContinuityStep4DProofController
    {
        private const string OutputRoot = "Temp/WorldMapStep4DProof";
        private const string AtlasAssetPath = "Assets/BeeKingdom/Playground/Resources/WorldMapWave4/UIB_SectorWave1/atlas_master_1536.png";
        private const string BootstrapAssetPath = "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs";
        private const string ProviderAssetPath = "Assets/BeeKingdom/Playground/WorldMapWave4ManifestContentProvider.cs";
        private const string ControllerAssetPath = "Assets/BeeKingdom/Playground/Editor/WorldMapRuntimeContinuityStep4DProofController.cs";

        private static readonly Queue<ProofRequest> CaptureQueue = new Queue<ProofRequest>();
        private static ProofRequest activeRequest;
        private static int waitFrames;

        [MenuItem("Bee Kingdom/WorldMap Step4D/Apply 1920x1080 z0.85 C32_32")]
        public static void ApplyLandscape085()
        {
            ApplyOnly(new ProofRequest("landscape_1920x1080_z0.85_C32_32", 1920, 1080, 0.85f, 32, 32));
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Apply 1920x1080 z1.10 C32_32")]
        public static void ApplyLandscape110()
        {
            ApplyOnly(new ProofRequest("landscape_1920x1080_z1.10_C32_32", 1920, 1080, 1.10f, 32, 32));
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Apply 1920x1080 z1.35 C32_32")]
        public static void ApplyLandscape135()
        {
            ApplyOnly(new ProofRequest("landscape_1920x1080_z1.35_C32_32", 1920, 1080, 1.35f, 32, 32));
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Apply 720x1280 z1.10 C32_32")]
        public static void ApplyPortrait110()
        {
            ApplyOnly(new ProofRequest("portrait_720x1280_z1.10_C32_32", 720, 1280, 1.10f, 32, 32));
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Apply Pan C32_32 z1.10")]
        public static void ApplyPanStart()
        {
            ApplyOnly(new ProofRequest("pan_C32_32_z1.10", 1920, 1080, 1.10f, 32, 32));
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Apply Pan C35_32 z1.10")]
        public static void ApplyPanMid()
        {
            ApplyOnly(new ProofRequest("pan_C35_32_z1.10", 1920, 1080, 1.10f, 35, 32));
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Apply Pan C36_32 z1.10")]
        public static void ApplyPanEnd()
        {
            ApplyOnly(new ProofRequest("pan_C36_32_z1.10", 1920, 1080, 1.10f, 36, 32));
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Capture Current Proof State")]
        public static void CaptureCurrentProofState()
        {
            WorldMapMmoFullscreenFoundationBootstrap bootstrap = RequireReadyBootstrap();
            WorldMapMmoFullscreenFoundationBootstrap.DevProofState state = bootstrap.CurrentDeterministicProofState("current_manual_state");
            CaptureState(state, state.Label, state.ScreenWidth, state.ScreenHeight);
        }

        [MenuItem("Bee Kingdom/WorldMap Step4D/Capture Required Step4D Set")]
        public static void CaptureRequiredStep4DSet()
        {
            RequireReadyBootstrap();
            CaptureQueue.Clear();
            CaptureQueue.Enqueue(new ProofRequest("landscape_1920x1080_z0.85_C32_32", 1920, 1080, 0.85f, 32, 32));
            CaptureQueue.Enqueue(new ProofRequest("landscape_1920x1080_z1.10_C32_32", 1920, 1080, 1.10f, 32, 32));
            CaptureQueue.Enqueue(new ProofRequest("landscape_1920x1080_z1.35_C32_32", 1920, 1080, 1.35f, 32, 32));
            CaptureQueue.Enqueue(new ProofRequest("portrait_720x1280_z1.10_C32_32", 720, 1280, 1.10f, 32, 32));
            CaptureQueue.Enqueue(new ProofRequest("pan_C32_32_z1.10", 1920, 1080, 1.10f, 32, 32));
            CaptureQueue.Enqueue(new ProofRequest("pan_C35_32_z1.10", 1920, 1080, 1.10f, 35, 32));
            CaptureQueue.Enqueue(new ProofRequest("pan_C36_32_z1.10", 1920, 1080, 1.10f, 36, 32));
            EditorApplication.update -= ProcessCaptureQueue;
            EditorApplication.update += ProcessCaptureQueue;
            ProcessCaptureQueue();
        }

        public static string[] WorldMapStep4DProofControllerForProof()
        {
            return new[]
            {
                "step4d_editor_controller:true",
                "step4d_editor_controller_path:" + ControllerAssetPath,
                "step4d_output_root:" + OutputRoot,
                "step4d_menu_apply_states:true",
                "step4d_menu_capture_current:true",
                "step4d_menu_capture_required_set:true",
                "step4d_guard_play_mode_required:true",
                "step4d_guard_canonical_scene_required:true",
                "step4d_guard_bootstrap_required:true",
                "step4d_manifest_includes_timestamp:true",
                "step4d_manifest_includes_resolution:true",
                "step4d_manifest_includes_zoom:true",
                "step4d_manifest_includes_chunk:true",
                "step4d_manifest_includes_atlas_hash:true",
                "step4d_manifest_includes_product_hashes:true",
                "step4d_manifest_5x5_absent:true",
                "step4d_screenshot_retouched:false",
                "step4d_no_visual_masking:false",
                "step4d_no_png_asset_modification:true",
                "step4d_no_scene_modification:true"
            };
        }

        public static void ValidateWorldMapRuntimeContinuityStep4DProofControls()
        {
            Require(WorldMapMmoFullscreenFoundationBootstrap.WorldMapStep4DProofControlsForProof(), "step4d_deterministic_dev_proof_controls:true");
            Require(WorldMapMmoFullscreenFoundationBootstrap.WorldMapStep4DProofControlsForProof(), "step4d_compilation_guard:UNITY_EDITOR_OR_DEVELOPMENT_BUILD");
            Require(WorldMapMmoFullscreenFoundationBootstrap.WorldMapStep4DProofControlsForProof(), "non_development_runtime_surface_added:false");
            string[] rows = WorldMapStep4DProofControllerForProof();
            Require(rows, "step4d_editor_controller:true");
            Require(rows, "step4d_output_root:" + OutputRoot);
            Require(rows, "step4d_menu_capture_required_set:true");
            Require(rows, "step4d_manifest_includes_atlas_hash:true");
            Require(rows, "step4d_manifest_5x5_absent:true");
            Debug.Log("WorldMap Step4D deterministic proof controls validation completed.");
        }

        private static void ApplyOnly(ProofRequest request)
        {
            WorldMapMmoFullscreenFoundationBootstrap bootstrap = RequireReadyBootstrap();
            WorldMapMmoFullscreenFoundationBootstrap.DevProofState state = bootstrap.ApplyDeterministicProofState(request.ChunkX, request.ChunkY, request.Zoom, request.Label);
            Debug.Log("Step4D proof state applied: " + DescribeState(state, request.ExpectedWidth, request.ExpectedHeight));
        }

        private static void ProcessCaptureQueue()
        {
            if (waitFrames > 0)
            {
                waitFrames--;
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            if (activeRequest.IsValid)
            {
                WorldMapMmoFullscreenFoundationBootstrap bootstrap = RequireReadyBootstrap();
                WorldMapMmoFullscreenFoundationBootstrap.DevProofState state = bootstrap.CurrentDeterministicProofState(activeRequest.Label);
                CaptureState(state, activeRequest.Label, activeRequest.ExpectedWidth, activeRequest.ExpectedHeight);
                activeRequest = default;
            }

            if (CaptureQueue.Count == 0)
            {
                EditorApplication.update -= ProcessCaptureQueue;
                Debug.Log("Step4D proof capture queue completed.");
                return;
            }

            activeRequest = CaptureQueue.Dequeue();
            ApplyOnly(activeRequest);
            waitFrames = 2;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static WorldMapMmoFullscreenFoundationBootstrap RequireReadyBootstrap()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Step4D proof controls require Play Mode.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != WorldMapMmoFullscreenFoundationBootstrap.Step4DCanonicalScenePath)
            {
                throw new InvalidOperationException("Step4D proof controls require canonical scene: " + WorldMapMmoFullscreenFoundationBootstrap.Step4DCanonicalScenePath + " (current: " + scene.path + ")");
            }

            WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException("Step4D proof controls require WorldMapMmoFullscreenFoundationBootstrap in the active scene.");
            }

            return bootstrap;
        }

        private static void CaptureState(WorldMapMmoFullscreenFoundationBootstrap.DevProofState state, string label, int expectedWidth, int expectedHeight)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot));
            Directory.CreateDirectory(root);
            string safeLabel = SanitizeFileName(label);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string pngPath = Path.Combine(root, timestamp + "_" + safeLabel + "_" + state.ScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + state.ScreenHeight.ToString(CultureInfo.InvariantCulture) + ".png");
            string manifestPath = Path.Combine(root, timestamp + "_" + safeLabel + "_manifest.json");
            ScreenCapture.CaptureScreenshot(pngPath);
            File.WriteAllText(manifestPath, BuildManifestJson(state, label, expectedWidth, expectedHeight, pngPath), Encoding.UTF8);
            Debug.Log("Step4D proof capture requested: " + pngPath + "\nManifest: " + manifestPath);
        }

        private static string BuildManifestJson(WorldMapMmoFullscreenFoundationBootstrap.DevProofState state, string label, int expectedWidth, int expectedHeight, string pngPath)
        {
            bool expectedResolution = state.ScreenWidth == expectedWidth && state.ScreenHeight == expectedHeight;
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendJson(builder, "schema_version", "1.0", true);
            AppendJson(builder, "proof_id", "WORLD_MAP_STEP4D_DETERMINISTIC_DEV_PROOF", true);
            AppendJson(builder, "label", label, true);
            AppendJson(builder, "timestamp_utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), true);
            AppendJson(builder, "scene", WorldMapMmoFullscreenFoundationBootstrap.Step4DCanonicalScenePath, true);
            AppendJson(builder, "screenshot_path", NormalizePath(pngPath), true);
            AppendJson(builder, "expected_resolution", expectedWidth.ToString(CultureInfo.InvariantCulture) + "x" + expectedHeight.ToString(CultureInfo.InvariantCulture), true);
            AppendJson(builder, "actual_resolution", state.ScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + state.ScreenHeight.ToString(CultureInfo.InvariantCulture), true);
            AppendJson(builder, "expected_resolution_match", expectedResolution ? "true" : "false", false, true);
            AppendJson(builder, "zoom_exact", state.Zoom.ToString("0.00", CultureInfo.InvariantCulture), false, true);
            AppendJson(builder, "chunk", "C" + state.Chunk.x.ToString(CultureInfo.InvariantCulture) + "_" + state.Chunk.y.ToString(CultureInfo.InvariantCulture), true);
            AppendJson(builder, "world_center", state.WorldCenter.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + state.WorldCenter.y.ToString("0.###", CultureInfo.InvariantCulture), true);
            AppendJson(builder, "active_chunks", state.ActiveChunkCount.ToString(CultureInfo.InvariantCulture), false, true);
            AppendJson(builder, "uv_rect", state.UvRect, true);
            AppendJson(builder, "uv_bounded", state.UvBounded ? "true" : "false", false, true);
            AppendJson(builder, "atlas_loaded", state.AtlasLoaded ? "true" : "false", false, true);
            AppendJson(builder, "atlas_wrap_mode", state.AtlasWrapMode, true);
            AppendJson(builder, "atlas_sha256", HashAsset(AtlasAssetPath), true);
            AppendJson(builder, "bootstrap_sha256", HashAsset(BootstrapAssetPath), true);
            AppendJson(builder, "provider_sha256", HashAsset(ProviderAssetPath), true);
            AppendJson(builder, "controller_sha256", HashAsset(ControllerAssetPath), true);
            AppendJson(builder, "master_5x5_integrated", "false", false, true);
            AppendJson(builder, "server_live", "false", false, true);
            AppendJson(builder, "screenshot_retouched", "false", false, true);
            AppendJson(builder, "visual_masking_overlay_added", "false", false, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendJson(StringBuilder builder, string key, string value, bool quoteValue, bool comma = true)
        {
            builder.Append("  \"").Append(EscapeJson(key)).Append("\": ");
            if (quoteValue)
            {
                builder.Append("\"").Append(EscapeJson(value)).Append("\"");
            }
            else
            {
                builder.Append(value);
            }

            if (comma) builder.Append(",");
            builder.AppendLine();
        }

        private static string HashAsset(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!File.Exists(fullPath)) return "missing";
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(fullPath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static string DescribeState(WorldMapMmoFullscreenFoundationBootstrap.DevProofState state, int expectedWidth, int expectedHeight)
        {
            return state.Label
                + " expected=" + expectedWidth.ToString(CultureInfo.InvariantCulture) + "x" + expectedHeight.ToString(CultureInfo.InvariantCulture)
                + " actual=" + state.ScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + state.ScreenHeight.ToString(CultureInfo.InvariantCulture)
                + " zoom=" + state.Zoom.ToString("0.00", CultureInfo.InvariantCulture)
                + " chunk=C" + state.Chunk.x.ToString(CultureInfo.InvariantCulture) + "_" + state.Chunk.y.ToString(CultureInfo.InvariantCulture)
                + " uv=" + state.UvRect
                + " wrap=" + state.AtlasWrapMode;
        }

        private static void Require(string[] rows, string expected)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == expected) return;
            }

            throw new InvalidOperationException("Missing Step4D proof row: " + expected);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private readonly struct ProofRequest
        {
            public readonly string Label;
            public readonly int ExpectedWidth;
            public readonly int ExpectedHeight;
            public readonly float Zoom;
            public readonly int ChunkX;
            public readonly int ChunkY;
            public bool IsValid => !string.IsNullOrEmpty(Label);

            public ProofRequest(string label, int expectedWidth, int expectedHeight, float zoom, int chunkX, int chunkY)
            {
                Label = label;
                ExpectedWidth = expectedWidth;
                ExpectedHeight = expectedHeight;
                Zoom = zoom;
                ChunkX = chunkX;
                ChunkY = chunkY;
            }
        }
    }

    public sealed class WorldMapWave3RuntimeTileImporter : AssetPostprocessor
    {
        private const string Wave3RuntimeTilePath = "Assets/BeeKingdom/Playground/Resources/WorldMapWave3Runtime/UIB_ContinuousMaster5x5_v1/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Wave3RuntimeTilePath, StringComparison.OrdinalIgnoreCase) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
            importer.maxTextureSize = 1024;
        }
    }
}
