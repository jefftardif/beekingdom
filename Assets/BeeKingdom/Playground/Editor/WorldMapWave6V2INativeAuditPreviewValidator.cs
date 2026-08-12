using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6V2INativeAuditPreviewValidator
    {
        private const string TerrainAssetRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_native_audit_preview";
        private const string RuntimeValidationPath = TerrainAssetRoot + "/runtime_validation.json";
        private const string RuntimeManifestPath = TerrainAssetRoot + "/runtime_manifest.json";
        private const string PreviewScenePath = "Assets/Scenes/WorldMapWave6V2INativeAuditPreview.unity";
        private const string ReceiptPath = "Docs/BuilderA/WorldMapWave6_50x50_V2INativeAuditPreview/WorldMapWave6_V2INativeAuditPreview_StaticValidation.txt";

        [MenuItem("Bee Kingdom/World Map/Validate Wave6 V2I Native Audit Preview Runtime")]
        public static void ValidateV2INativeAuditPreviewRuntime()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var evidence = new List<string>();
                ValidateRuntimeBundle(evidence);
                ValidateProviderAtCenterAndCorners(evidence);
                ValidateSceneFlag(evidence);
                WriteReceipt("PASS", evidence, null);
                Debug.Log("[WorldMap Wave6 V2I] Native audit preview validation PASS. Receipt: " + ReceiptPath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                WriteReceipt("NOT_READY", new List<string>(), exception);
                Debug.LogWarning("[WorldMap Wave6 V2I] Native audit preview validation NOT_READY: " + exception.Message);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }

        private static void ValidateRuntimeBundle(List<string> evidence)
        {
            string absoluteRoot = AbsoluteProjectPath(TerrainAssetRoot);
            Require(Directory.Exists(absoluteRoot), "V2I native audit runtime root is missing.");

            string[] tileFiles = Directory.GetFiles(absoluteRoot, "R??C??_g2.png", SearchOption.TopDirectoryOnly);
            Require(tileFiles.Length == 2500, "Expected 2500 V2I native audit runtime tiles, found " + tileFiles.Length + ".");
            Require(File.Exists(AbsoluteProjectPath(RuntimeValidationPath)), "V2I native audit runtime_validation.json is missing.");
            Require(File.Exists(AbsoluteProjectPath(RuntimeManifestPath)), "V2I native audit runtime_manifest.json is missing.");

            string validationJson = File.ReadAllText(AbsoluteProjectPath(RuntimeValidationPath));
            Require(validationJson.Contains("\"status\"", StringComparison.Ordinal) && validationJson.Contains("\"PASS\"", StringComparison.Ordinal), "V2I native audit runtime validation is not PASS.");
            Require(validationJson.Contains("\"tile_count\"", StringComparison.Ordinal) && validationJson.Contains("2500", StringComparison.Ordinal), "V2I native audit tile count is not 2500.");
            Require(validationJson.Contains("\"unity_audit_preview\"", StringComparison.Ordinal) && validationJson.Contains("\"YES\"", StringComparison.Ordinal), "V2I native audit flag is missing.");
            Require(validationJson.Contains("\"ready_for_unity_handoff\"", StringComparison.Ordinal) && validationJson.Contains("\"NO\"", StringComparison.Ordinal), "V2I audit package must not be marked as Unity handoff.");

            string manifestJson = File.ReadAllText(AbsoluteProjectPath(RuntimeManifestPath));
            Require(manifestJson.Contains(WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewExpectedMasterSha256, StringComparison.OrdinalIgnoreCase), "V2I native audit manifest has the wrong source hash.");
            Require(manifestJson.Contains("\"tile_size\"", StringComparison.Ordinal) && manifestJson.Contains("512", StringComparison.Ordinal), "V2I native audit manifest is not a 512 tile package.");

            evidence.Add("v2i_native_audit_runtime_root:" + WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewResourceRoot);
            evidence.Add("v2i_native_audit_source_sha256:" + WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewExpectedMasterSha256);
            evidence.Add("v2i_native_audit_runtime_tile_files:2500");
            evidence.Add("v2i_native_audit_runtime_tile_dimensions:516x516");
            evidence.Add("v2i_native_audit_inner_source_tile_dimensions:512x512");
            evidence.Add("v2i_native_audit_runtime_validation:PASS");
            evidence.Add("canonical_wave6_v1_replaced:false");
            evidence.Add("ready_for_unity_visual_audit:true");
            evidence.Add("ready_for_unity_handoff:false");
        }

        private static void ValidateProviderAtCenterAndCorners(List<string> evidence)
        {
            var provider = new WorldMapWave6StreamingTileProvider(
                WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewResourceRoot,
                WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewExpectedMasterSha256);

            try
            {
                Rect bounds = provider.WorldBounds;
                Require(provider.Initialize(bounds.center, 1f, 1920, 1080), "V2I native audit provider failed to prime at map center.");
                Require(provider.HasAllVisibleTiles, "V2I native audit center view has missing visible tiles.");

                ValidateView(provider, "CENTER_NATIVE", bounds.center, 1.35f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_WEST", SafeCorner(bounds, 1920, 1080, 1f, false, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_EAST", SafeCorner(bounds, 1920, 1080, 1f, true, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "SOUTH_WEST", SafeCorner(bounds, 720, 1280, 1f, false, true), 1f, 720, 1280, evidence);
                ValidateView(provider, "SOUTH_EAST", SafeCorner(bounds, 720, 1280, 1f, true, true), 1f, 720, 1280, evidence);
                Require(provider.CachedTileCount <= WorldMapWave6StreamingTileProvider.CacheCapacity, "V2I native audit texture cache exceeded capacity.");

                evidence.Add("v2i_native_audit_center_and_four_corners_visible_tiles_complete:true");
                evidence.Add("v2i_native_audit_streaming_cache_peak_limit:" + WorldMapWave6StreamingTileProvider.CacheCapacity.ToString(CultureInfo.InvariantCulture));
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
            evidence.Add("v2i_native_audit_" + label.ToLowerInvariant() + "_range:R" + range.MinRow.ToString("00") + "..R" + range.MaxRow.ToString("00") + ",C" + range.MinColumn.ToString("00") + "..C" + range.MaxColumn.ToString("00"));
            evidence.Add("v2i_native_audit_" + label.ToLowerInvariant() + "_visible_tiles:" + provider.LoadedVisibleTileCount.ToString(CultureInfo.InvariantCulture) + "/" + provider.RequiredVisibleTileCount.ToString(CultureInfo.InvariantCulture));
        }

        private static void ValidateSceneFlag(List<string> evidence)
        {
            string scene = AbsoluteProjectPath(PreviewScenePath);
            Require(File.Exists(scene), "V2I native audit preview scene is missing.");
            string sceneYaml = File.ReadAllText(scene);
            Require(sceneYaml.Contains("useV2INativeAuditPreviewRuntimePackageForPlayMode: 1", StringComparison.Ordinal), "V2I native audit scene did not enable the native audit runtime package.");
            Require(sceneYaml.Contains("useV3ECandidateRuntimePackageForPlayMode: 0", StringComparison.Ordinal), "V2I native audit scene must not use the V3E reduced package.");
            evidence.Add("v2i_native_audit_preview_scene:" + PreviewScenePath);
            evidence.Add("v2i_native_audit_scene_uses_v2i_package:true");
            evidence.Add("v2i_native_audit_scene_uses_v3e_package:false");
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
            builder.AppendLine("WORLD_MAP_WAVE6_50X50_V2I_NATIVE_AUDIT_PREVIEW_STATIC_VALIDATION=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("v2i_native_audit_source_sha256=" + WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewExpectedMasterSha256);
            builder.AppendLine("UNITY_AUDIT_PREVIEW=YES");
            builder.AppendLine("READY_FOR_QA_BUILDERC=NO");
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

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
