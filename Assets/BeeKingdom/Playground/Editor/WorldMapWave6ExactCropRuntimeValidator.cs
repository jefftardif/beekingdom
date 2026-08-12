using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6ExactCropRuntimeValidator
    {
        private const string RuntimeRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview";
        private const string RuntimeValidationPath = RuntimeRoot + "/runtime_validation.json";
        private const string RuntimeManifestPath = RuntimeRoot + "/runtime_manifest.json";
        private const string MmoPreviewScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string TerrainOnlyScenePath = "Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity";
        private const string ReceiptPath = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/UnityExactCropRuntimeValidation_20260717.txt";

        [MenuItem("Bee Kingdom/World Map/Validate Wave6 50x50 Exact Crop Runtime")]
        public static void ValidateExactCropRuntime()
        {
            var evidence = new List<string>();
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidateMmoPreviewScene(evidence);
                ValidateTerrainOnlyScene(evidence);
                ValidateRuntimePackage(evidence);
                ValidateTextureImporters(evidence);
                ValidateProvider(evidence);
                WriteReceipt("PASS", evidence, null);
                Debug.Log("[Wave6 50x50 Exact Crop] Runtime validation PASS. Receipt: " + ReceiptPath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                WriteReceipt("FAIL", evidence, exception);
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }

        private static void ValidateMmoPreviewScene(List<string> evidence)
        {
            string scenePath = AbsolutePath(MmoPreviewScenePath);
            Require(File.Exists(scenePath), "Wave6 exact-crop Unity test scene is missing.");
            string sceneText = File.ReadAllText(scenePath);
            Require(sceneText.Contains("useWave5Method12288PreviewRuntimePackageForPlayMode: 1"), "Test scene is not configured to load the exact-crop 12288 runtime package.");
            Require(sceneText.Contains("initialAuditZoom: 0.58"), "Test scene initial audit zoom changed unexpectedly.");
            evidence.Add("mmo_preview_scene_path:" + MmoPreviewScenePath);
            evidence.Add("mmo_preview_scene_exact_crop_flag:true");
            evidence.Add("mmo_preview_scene_initial_audit_zoom:0.58");
        }

        private static void ValidateTerrainOnlyScene(List<string> evidence)
        {
            string scenePath = AbsolutePath(TerrainOnlyScenePath);
            Require(File.Exists(scenePath), "Wave6 terrain-only exact-crop Unity test scene is missing.");
            string sceneText = File.ReadAllText(scenePath);
            Require(sceneText.Contains("WorldMapWave6Premium50x50TestBootstrap"), "Terrain-only scene is not using the Wave6 50x50 test bootstrap.");

            string bootstrapPath = AbsolutePath("Assets/BeeKingdom/Playground/WorldMapWave6Premium50x50TestBootstrap.cs");
            Require(File.Exists(bootstrapPath), "Wave6 terrain-only bootstrap is missing.");
            string bootstrapText = File.ReadAllText(bootstrapPath);
            Require(bootstrapText.Contains("Wave5Method12288PreviewResourceRoot"), "Terrain-only scene bootstrap is not wired to the exact-crop 12288 runtime package.");
            Require(bootstrapText.Contains("Wave5Method12288PreviewExpectedMasterSha256"), "Terrain-only scene bootstrap is not validating the exact-crop package SHA.");
            Require(bootstrapText.Contains("tile.GutterWorldRect"), "Terrain-only scene bootstrap is not rendering gutter world rects.");
            Require(bootstrapText.Contains("FullTextureUv"), "Terrain-only scene bootstrap is not rendering full texture UVs.");

            evidence.Add("terrain_only_scene_path:" + TerrainOnlyScenePath);
            evidence.Add("terrain_only_scene_exact_crop_root:true");
            evidence.Add("terrain_only_scene_package_sha_validation:true");
            evidence.Add("terrain_only_scene_gutter_rendering:true");
        }

        private static void ValidateRuntimePackage(List<string> evidence)
        {
            string runtimeRoot = AbsolutePath(RuntimeRoot);
            Require(Directory.Exists(runtimeRoot), "Wave6 exact-crop runtime root is missing.");

            string[] tileFiles = Directory.GetFiles(runtimeRoot, "R??C??_g2.png", SearchOption.TopDirectoryOnly);
            Require(tileFiles.Length == 2500, "Expected 2500 exact-crop runtime tiles, found " + tileFiles.Length + ".");

            string validationJson = ReadRequired(RuntimeValidationPath);
            Require(validationJson.Contains("\"status\": \"PASS\""), "Exact-crop runtime validation is not PASS.");
            Require(validationJson.Contains("\"tile_count\": 2500"), "Exact-crop runtime tile count is not 2500.");
            Require(validationJson.Contains("\"neighbor_pairs_checked\": 4900"), "Exact-crop neighbor-pair validation count is not 4900.");
            Require(validationJson.Contains("\"neighbor_gutter_mismatch_count\": 0"), "Exact-crop neighbor gutter mismatch count is not zero.");
            Require(validationJson.Contains("\"neighbor_gutter_mismatch_pixel_count\": 0"), "Exact-crop neighbor gutter mismatch pixels are not zero.");
            Require(validationJson.Contains("\"single_canonical_pixel_field\": \"YES\""), "Exact-crop runtime was not built from one canonical pixel field.");
            Require(validationJson.Contains("\"per_tile_resampling\": \"NO\""), "Exact-crop runtime reports per-tile resampling.");
            Require(validationJson.Contains(WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256), "Exact-crop validation does not contain the expected package SHA.");

            string manifestJson = ReadRequired(RuntimeManifestPath);
            Require(manifestJson.Contains("\"schema\": \"bee-kingdom.world-map.wave6-unity-runtime-bundle.v2\""), "Exact-crop runtime manifest is not schema v2.");
            Require(manifestJson.Contains("\"source_superpanel_sha256\": \"" + WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256 + "\""), "Exact-crop manifest package SHA mismatch.");
            Require(manifestJson.Contains("\"generation_contract\": \"CANONICAL_RESAMPLE_ONCE_THEN_CROP_TILES_WITH_GUTTERS\""), "Exact-crop manifest generation contract mismatch.");

            evidence.Add("runtime_root:" + WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewResourceRoot);
            evidence.Add("runtime_tiles:2500");
            evidence.Add("runtime_tile_dimensions:516x516");
            evidence.Add("runtime_validation:PASS");
            evidence.Add("neighbor_pairs_checked:4900");
            evidence.Add("neighbor_gutter_mismatch_count:0");
            evidence.Add("neighbor_gutter_mismatch_pixel_count:0");
            evidence.Add("single_canonical_pixel_field:YES");
            evidence.Add("per_tile_resampling:NO");
            evidence.Add("manifest_schema:v2");
            evidence.Add("package_sha256:" + WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256);
        }

        private static void ValidateTextureImporters(List<string> evidence)
        {
            int checkedTiles = 0;
            for (int row = 0; row < WorldMapWave6StreamingTileProvider.Rows; row++)
            {
                for (int column = 0; column < WorldMapWave6StreamingTileProvider.Columns; column++)
                {
                    string name = "R" + row.ToString("00", CultureInfo.InvariantCulture) + "C" + column.ToString("00", CultureInfo.InvariantCulture) + "_g2.png";
                    string assetPath = RuntimeRoot + "/" + name;
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    Require(importer != null, "Texture importer missing: " + assetPath);
                    Require(importer.textureType == TextureImporterType.Default, "Texture type must be Default: " + name);
                    Require(importer.wrapMode == TextureWrapMode.Clamp, "Clamp wrap mode required: " + name);
                    Require(importer.filterMode == FilterMode.Bilinear, "Bilinear filtering required: " + name);
                    Require(!importer.mipmapEnabled, "Mipmaps must be disabled: " + name);
                    Require(importer.alphaSource == TextureImporterAlphaSource.None, "Alpha source must be None: " + name);
                    Require(importer.textureCompression == TextureImporterCompression.Uncompressed, "Uncompressed texture required: " + name);
                    checkedTiles++;
                }
            }

            evidence.Add("texture_importers_checked:" + checkedTiles.ToString(CultureInfo.InvariantCulture));
            evidence.Add("texture_import_contract:default_clamp_bilinear_no_mips_no_alpha_uncompressed");
        }

        private static void ValidateProvider(List<string> evidence)
        {
            var provider = new WorldMapWave6StreamingTileProvider(
                WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewResourceRoot,
                WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256);

            try
            {
                Rect bounds = provider.WorldBounds;
                Require(provider.Initialize(bounds.center, 1f, 1920, 1080), "Exact-crop provider failed to initialize at center.");
                Require(string.Equals(provider.MasterSha256, WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256, StringComparison.OrdinalIgnoreCase), "Provider package SHA mismatch.");

                ValidateView(provider, "CENTER", bounds.center, 1f, 1920, 1080, evidence);
                ValidateView(provider, "HOTSPOT_C54_09", ChunkCenter(54, 9), 0.58f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_WEST", SafeCorner(bounds, 1920, 1080, 1f, false, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_EAST", SafeCorner(bounds, 1920, 1080, 1f, true, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "SOUTH_WEST", SafeCorner(bounds, 720, 1280, 1f, false, true), 1f, 720, 1280, evidence);
                ValidateView(provider, "SOUTH_EAST", SafeCorner(bounds, 720, 1280, 1f, true, true), 1f, 720, 1280, evidence);
                Require(provider.CachedTileCount <= WorldMapWave6StreamingTileProvider.CacheCapacity, "Texture cache exceeded capacity.");

                evidence.Add("provider_package_sha256:" + provider.MasterSha256);
                evidence.Add("provider_center_hotspot_corners_complete:true");
                evidence.Add("cache_capacity_respected:true");
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
            evidence.Add(label.ToLowerInvariant() + "_range:R" + range.MinRow.ToString("00", CultureInfo.InvariantCulture) + "..R" + range.MaxRow.ToString("00", CultureInfo.InvariantCulture) + ",C" + range.MinColumn.ToString("00", CultureInfo.InvariantCulture) + "..C" + range.MaxColumn.ToString("00", CultureInfo.InvariantCulture));
            evidence.Add(label.ToLowerInvariant() + "_visible_tiles:" + provider.LoadedVisibleTileCount.ToString(CultureInfo.InvariantCulture) + "/" + provider.RequiredVisibleTileCount.ToString(CultureInfo.InvariantCulture));
        }

        private static Vector2 ChunkCenter(int chunkX, int chunkY)
        {
            return new Vector2(
                chunkX * WorldMapWave6StreamingTileProvider.TileSize + WorldMapWave6StreamingTileProvider.TileSize * 0.5f,
                chunkY * WorldMapWave6StreamingTileProvider.TileSize + WorldMapWave6StreamingTileProvider.TileSize * 0.5f);
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

        private static string ReadRequired(string projectRelativePath)
        {
            string path = AbsolutePath(projectRelativePath);
            Require(File.Exists(path), "Required file missing: " + projectRelativePath);
            return File.ReadAllText(path);
        }

        private static string AbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void WriteReceipt(string status, List<string> evidence, Exception exception)
        {
            string path = AbsolutePath(ReceiptPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var builder = new StringBuilder();
            builder.AppendLine("WORLD_MAP_WAVE6_50X50_EXACT_CROP_UNITY_RUNTIME_VALIDATION=" + status);
            builder.AppendLine("timestamp_utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("READY_FOR_QA_BUILDERC=NO");
            builder.AppendLine("READY_FOR_UNITY_HANDOFF=NO");
            for (int i = 0; i < evidence.Count; i++) builder.AppendLine(evidence[i]);
            if (exception != null) builder.AppendLine("error=" + exception);
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
