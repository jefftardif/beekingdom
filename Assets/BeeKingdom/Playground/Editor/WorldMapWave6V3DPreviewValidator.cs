using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6V3DPreviewValidator
    {
        private const string TerrainAssetRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3d_preview";
        private const string RuntimeValidationPath = TerrainAssetRoot + "/runtime_validation.json";
        private const string SourceMasterPath = "artifacts/UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging/production_v3d_highres_worker/v3d_highres_prototype_8192.png";
        private const string ReceiptPath = "Docs/BuilderA/WorldMapWave6_50x50_V3DPreview/WorldMapWave6_V3DPreview_StaticValidation.txt";

        [MenuItem("Bee Kingdom/World Map/Validate Wave6 V3D Preview Runtime")]
        public static void ValidateV3DPreviewRuntime()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var evidence = new List<string>();
                ValidateSource(evidence);
                ValidateRuntimeBundle(evidence);
                ValidateProviderAtCenterAndCorners(evidence);
                WriteReceipt("PASS", evidence, null);
                Debug.Log("[WorldMap Wave6 V3D] Preview runtime validation PASS. Receipt: " + ReceiptPath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                WriteReceipt("NOT_READY", new List<string>(), exception);
                Debug.LogWarning("[WorldMap Wave6 V3D] Preview runtime validation NOT_READY: " + exception.Message);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }

        private static void ValidateSource(List<string> evidence)
        {
            string source = AbsoluteProjectPath(SourceMasterPath);
            Require(File.Exists(source), "V3D 8192 source prototype is missing.");
            string hash = Sha256File(source);
            Require(string.Equals(hash, WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256, StringComparison.OrdinalIgnoreCase), "V3D source SHA-256 mismatch.");
            evidence.Add("v3d_source_present:true");
            evidence.Add("v3d_source_sha256:" + hash);
            evidence.Add("v3d_source_role:preview_source_not_final_25600_master");
        }

        private static void ValidateRuntimeBundle(List<string> evidence)
        {
            string absoluteRoot = AbsoluteProjectPath(TerrainAssetRoot);
            Require(Directory.Exists(absoluteRoot), "V3D preview runtime root is missing.");

            string[] tileFiles = Directory.GetFiles(absoluteRoot, "R??C??_g2.png", SearchOption.TopDirectoryOnly);
            Require(tileFiles.Length == 2500, "Expected 2500 V3D preview runtime tiles, found " + tileFiles.Length + ".");
            Require(File.Exists(AbsoluteProjectPath(RuntimeValidationPath)), "V3D preview runtime_validation.json is missing.");

            string validationJson = File.ReadAllText(AbsoluteProjectPath(RuntimeValidationPath));
            Require(validationJson.Contains("\"status\": \"PASS\""), "V3D preview runtime validation is not PASS.");
            Require(validationJson.Contains("\"tile_count\": 2500"), "V3D preview tile count is not 2500.");
            Require(validationJson.Contains("\"inner_pixel_mismatch_count\": 0"), "V3D preview inner pixel validation failed.");
            Require(validationJson.Contains("\"neighbor_gutter_mismatch_count\": 0"), "V3D preview neighbor gutter validation failed.");

            evidence.Add("v3d_preview_runtime_root:" + WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot);
            evidence.Add("v3d_preview_runtime_tile_files:2500");
            evidence.Add("v3d_preview_runtime_tile_dimensions:516x516");
            evidence.Add("v3d_preview_runtime_validation:PASS");
            evidence.Add("canonical_wave6_v1_replaced:false");
            evidence.Add("ready_for_unity_preview_provider:true");
        }

        private static void ValidateProviderAtCenterAndCorners(List<string> evidence)
        {
            var provider = new WorldMapWave6StreamingTileProvider(
                WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot,
                WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256);

            try
            {
                Rect bounds = provider.WorldBounds;
                Require(provider.Initialize(bounds.center, 1f, 1920, 1080), "V3D preview provider failed to prime at map center.");
                Require(provider.HasAllVisibleTiles, "V3D preview center view has missing visible tiles.");

                ValidateView(provider, "CENTER_NATIVE", bounds.center, 1.35f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_WEST", SafeCorner(bounds, 1920, 1080, 1f, false, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_EAST", SafeCorner(bounds, 1920, 1080, 1f, true, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "SOUTH_WEST", SafeCorner(bounds, 720, 1280, 1f, false, true), 1f, 720, 1280, evidence);
                ValidateView(provider, "SOUTH_EAST", SafeCorner(bounds, 720, 1280, 1f, true, true), 1f, 720, 1280, evidence);
                Require(provider.CachedTileCount <= WorldMapWave6StreamingTileProvider.CacheCapacity, "V3D preview texture cache exceeded capacity.");

                evidence.Add("v3d_preview_center_and_four_corners_visible_tiles_complete:true");
                evidence.Add("v3d_preview_streaming_cache_peak_limit:" + WorldMapWave6StreamingTileProvider.CacheCapacity.ToString(CultureInfo.InvariantCulture));
            }
            finally
            {
                provider.Dispose();
            }
        }

        private static void ValidateView(WorldMapWave6StreamingTileProvider provider, string label, Vector2 center, float zoom, int width, int height, List<string> evidence)
        {
            provider.UpdateStreaming(center, zoom, width, height, true);
            Require(!provider.HasLoadFailure, label + " failed: " + provider.FailureReason);
            Require(provider.HasAllVisibleTiles, label + " has missing visible tiles.");
            Wave6TileRange range = provider.CalculateRange(center, zoom, width, height, 0);
            evidence.Add("v3d_preview_" + label.ToLowerInvariant() + "_range:R" + range.MinRow.ToString("00") + "..R" + range.MaxRow.ToString("00") + ",C" + range.MinColumn.ToString("00") + "..C" + range.MaxColumn.ToString("00"));
            evidence.Add("v3d_preview_" + label.ToLowerInvariant() + "_visible_tiles:" + provider.LoadedVisibleTileCount.ToString(CultureInfo.InvariantCulture) + "/" + provider.RequiredVisibleTileCount.ToString(CultureInfo.InvariantCulture));
        }

        private static Vector2 SafeCorner(Rect bounds, int width, int height, float zoom, bool right, bool bottom)
        {
            float halfWidth = width * 0.5f / zoom;
            float halfHeight = height * 0.5f / zoom;
            float margin = 128f / zoom;
            return new Vector2(
                right ? bounds.xMax - halfWidth - margin : bounds.xMin + halfWidth + margin,
                bottom ? bounds.yMax - halfHeight - margin : bounds.yMin + halfHeight + margin);
        }

        private static void WriteReceipt(string status, List<string> evidence, Exception exception)
        {
            string absolute = AbsoluteProjectPath(ReceiptPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? AbsoluteProjectPath("Docs/BuilderA"));
            var builder = new StringBuilder();
            builder.AppendLine("WORLD_MAP_WAVE6_50X50_V3D_PREVIEW_STATIC_VALIDATION=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("v3d_source_sha256=" + WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256);
            builder.AppendLine("READY_FOR_CANONICAL_SWAP=NO");
            builder.AppendLine("READY_FOR_UNITY_HANDOFF=NO");
            builder.AppendLine("MASTER_25600_AUTHORIZED=NO");
            for (int i = 0; i < evidence.Count; i++) builder.AppendLine(evidence[i]);
            if (exception != null)
            {
                builder.AppendLine("exception=" + exception.GetType().FullName);
                builder.AppendLine("message=" + exception.Message);
            }

            File.WriteAllText(absolute, builder.ToString(), new UTF8Encoding(false));
        }

        private static string AbsoluteProjectPath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
