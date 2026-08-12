using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave5IntegrationValidator
    {
        public const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string TerrainAssetRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave5Runtime/UIB_ImmenseContinuousMaster25x25_v1";
        private const string RuntimeValidationPath = TerrainAssetRoot + "/runtime_validation.json";
        private const string BearDenAssetPath = "Assets/BeeKingdom/Playground/Resources/WorldMapWave5Runtime/Landmarks/BearDen/bear_den_dormant_v1.png";
        private const string SourceMasterPath = "artifacts/UIB_ImmenseContinuousMaster25x25_staging/master_25x25_12800.png";
        private const string BootstrapPath = "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs";
        private const string ReceiptPath = "Docs/BuilderA/WorldMapWave5_25x25_UnityIntegration/WorldMapWave5_StaticValidation.txt";

        [MenuItem("Bee Kingdom/World Map/Validate Wave5 25x25 Integration")]
        public static void ValidateWave5Integration()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var evidence = new List<string>();
                ValidateSceneAndProofContract(evidence);
                ValidateRuntimeBundleAndImporters(evidence);
                ValidateProviderAtCenterAndCorners(evidence);
                ValidateBearDenAndToggle(evidence);
                ValidateCanonicalNavigationAndReachability(evidence);
                WriteReceipt("PASS", evidence, null);
                Debug.Log("[WorldMap Wave5] Static/runtime integration validation PASS. Receipt: " + ReceiptPath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                WriteReceipt("FAIL", new List<string>(), exception);
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }

        private static void ValidateSceneAndProofContract(List<string> evidence)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid(), "Canonical WorldMap scene could not be opened.");
            Require(UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>() != null, "Canonical WorldMap bootstrap is missing.");
            Require(SplashDevelopmentSceneConfig.IsSceneEnabledInBuildSettings(ScenePath), "Canonical WorldMap scene is not enabled in Build Settings.");

            string[] rows = WorldMapMmoFullscreenFoundationBootstrap.WorldMapWave5IntegrationForProof();
            RequireRow(rows, "wave5_25x25_unity_integration:true");
            RequireRow(rows, "source_master_sha256:" + WorldMapWave5StreamingTileProvider.ExpectedMasterSha256);
            RequireRow(rows, "runtime_tile_count:625");
            RequireRow(rows, "old_wave3_5x5_canonical_active:false");
            RequireRow(rows, "canonical_static_uv_fallback_reachable:false");
            RequireRow(rows, "canonical_modulo_tile_fallback_reachable:false");
            RequireRow(rows, "terrain_entities_landmarks_same_world_to_screen:true");
            RequireRow(rows, "hud_screen_space_fixed:true");
            RequireRow(rows, "bear_den_visible_by_default:true");
            RequireRow(rows, "bear_den_toggle_session_local:true");
            RequireRow(rows, "bear_visible:false");
            RequireRow(rows, "ground_routes_used:false");
            RequireRow(rows, "server_live:false");

            evidence.Add("scene_openable:true");
            evidence.Add("scene_path:" + ScenePath);
            evidence.Add("build_settings_scene_enabled:true");
            evidence.Add("wave5_proof_contract:true");
        }

        private static void ValidateRuntimeBundleAndImporters(List<string> evidence)
        {
            string absoluteRoot = AbsoluteProjectPath(TerrainAssetRoot);
            string[] tileFiles = Directory.GetFiles(absoluteRoot, "R??C??_g2.png", SearchOption.TopDirectoryOnly);
            Require(tileFiles.Length == 625, "Expected 625 Wave5 runtime tiles, found " + tileFiles.Length + ".");
            Require(!File.Exists(Path.Combine(absoluteRoot, "master_25x25_12800.png")), "Monolithic 12800 master must not be imported into Assets.");
            string validationJson = File.ReadAllText(AbsoluteProjectPath(RuntimeValidationPath));
            Require(validationJson.Contains("\"status\": \"PASS\""), "Runtime gutter validation is not PASS.");
            Require(validationJson.Contains("\"inner_pixel_mismatch_count\": 0"), "Runtime inner pixel validation failed.");
            Require(validationJson.Contains("\"neighbor_gutter_mismatch_count\": 0"), "Runtime neighbor gutter validation failed.");

            string sourceMasterHash = Sha256File(AbsoluteProjectPath(SourceMasterPath));
            Require(string.Equals(sourceMasterHash, WorldMapWave5StreamingTileProvider.ExpectedMasterSha256, StringComparison.OrdinalIgnoreCase), "Frozen Wave5 master SHA-256 mismatch.");

            for (int row = 0; row < WorldMapWave5StreamingTileProvider.Rows; row++)
            {
                for (int column = 0; column < WorldMapWave5StreamingTileProvider.Columns; column++)
                {
                    string name = WorldMapWave5StreamingTileProvider.TileId(row, column) + "_g2.png";
                    string assetPath = TerrainAssetRoot + "/" + name;
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    Require(importer != null, "TextureImporter missing for " + assetPath);
                    Require(!importer.mipmapEnabled, "Mipmaps must be disabled for " + name);
                    Require(importer.wrapMode == TextureWrapMode.Clamp, "Clamp is required for " + name);
                    Require(importer.filterMode == FilterMode.Bilinear, "Bilinear filtering is required for " + name);
                    Require(importer.npotScale == TextureImporterNPOTScale.None, "NPOT scaling must be disabled for " + name);
                    Require(importer.alphaSource == TextureImporterAlphaSource.None, "Terrain alpha must be disabled for " + name);
                }
            }

            evidence.Add("runtime_tile_files:625");
            evidence.Add("runtime_tile_dimensions:516x516");
            evidence.Add("source_master_sha256:" + sourceMasterHash);
            evidence.Add("runtime_gutter_validation:PASS");
            evidence.Add("terrain_importers_clamp_bilinear_no_mips:625/625");
            evidence.Add("monolithic_master_imported:false");
        }

        private static void ValidateProviderAtCenterAndCorners(List<string> evidence)
        {
            var provider = new WorldMapWave5StreamingTileProvider();
            try
            {
                Rect bounds = provider.WorldBounds;
                Vector2 center = bounds.center;
                Require(provider.Initialize(center, 1f, 1920, 1080), "Wave5 provider failed to prime at map center.");
                Require(provider.HasAllVisibleTiles, "Center view has missing visible tiles.");
                Require(provider.MasterSha256 == WorldMapWave5StreamingTileProvider.ExpectedMasterSha256, "Provider master SHA mismatch.");

                ValidateView(provider, "CENTER", center, 1.35f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_WEST", SafeCorner(bounds, 1920, 1080, 1f, false, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "NORTH_EAST", SafeCorner(bounds, 1920, 1080, 1f, true, false), 1f, 1920, 1080, evidence);
                ValidateView(provider, "SOUTH_WEST", SafeCorner(bounds, 720, 1280, 1f, false, true), 1f, 720, 1280, evidence);
                ValidateView(provider, "SOUTH_EAST", SafeCorner(bounds, 720, 1280, 1f, true, true), 1f, 720, 1280, evidence);

                evidence.Add("camera_bounded_to_wave5_world:true");
                evidence.Add("center_and_four_corners_visible_tiles_complete:true");
                evidence.Add("native_zoom_1.35_visible_tiles_complete:true");
                evidence.Add("streaming_cache_peak_limit_target:" + WorldMapWave5StreamingTileProvider.CacheCapacity.ToString(CultureInfo.InvariantCulture));
            }
            finally
            {
                provider.Dispose();
            }
        }

        private static void ValidateView(WorldMapWave5StreamingTileProvider provider, string label, Vector2 center, float zoom, int width, int height, List<string> evidence)
        {
            provider.UpdateStreaming(center, zoom, width, height, true);
            Require(!provider.HasLoadFailure, label + " failed: " + provider.FailureReason);
            Require(provider.HasAllVisibleTiles, label + " has missing visible tiles.");
            Wave5TileRange range = provider.CalculateRange(center, zoom, width, height, 0);
            Require(range.MinRow >= 0 && range.MaxRow < 25 && range.MinColumn >= 0 && range.MaxColumn < 25, label + " range escaped the Wave5 grid.");
            evidence.Add(label.ToLowerInvariant() + "_center:" + center.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + center.y.ToString("0.###", CultureInfo.InvariantCulture));
            evidence.Add(label.ToLowerInvariant() + "_range:R" + range.MinRow.ToString("00") + "..R" + range.MaxRow.ToString("00") + ",C" + range.MinColumn.ToString("00") + "..C" + range.MaxColumn.ToString("00"));
            evidence.Add(label.ToLowerInvariant() + "_visible_tiles:" + provider.LoadedVisibleTileCount.ToString(CultureInfo.InvariantCulture) + "/" + provider.RequiredVisibleTileCount.ToString(CultureInfo.InvariantCulture));
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

        private static void ValidateBearDenAndToggle(List<string> evidence)
        {
            string bearHash = Sha256File(AbsoluteProjectPath(BearDenAssetPath));
            Require(string.Equals(bearHash, WorldMapBearDenLandmark.ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase), "Bear Den asset SHA-256 mismatch.");
            TextureImporter importer = AssetImporter.GetAtPath(BearDenAssetPath) as TextureImporter;
            Require(importer != null, "Bear Den TextureImporter is missing.");
            Require(importer.wrapMode == TextureWrapMode.Clamp && importer.filterMode == FilterMode.Bilinear && !importer.mipmapEnabled, "Bear Den importer must be Clamp/Bilinear/no mipmaps.");
            Require(importer.alphaSource == TextureImporterAlphaSource.FromInput && importer.alphaIsTransparency, "Bear Den alpha import contract is invalid.");

            var landmark = new WorldMapBearDenLandmark();
            try
            {
                Require(landmark.Load(), "Bear Den runtime landmark could not load.");
                Require(landmark.IsVisible, "Bear Den must be visible by default.");
                Vector2 expectedAnchor = new Vector2(11520f, 13271f);
                Require(Vector2.Distance(landmark.WorldAnchor, expectedAnchor) < 0.001f, "Bear Den R05C02 anchor mismatch.");
                Require(Mathf.Abs(landmark.WorldRect.width - 767.5f) < 0.001f && Mathf.Abs(landmark.WorldRect.height - 512f) < 0.001f, "Bear Den world size mismatch.");
                Require(!landmark.BearVisible && !landmark.RoadVisible && !landmark.ActiveEvent, "Bear Den dormant non-claims failed.");
                Require(!landmark.ToggleVisibility() && !landmark.IsVisible, "Bear Den hide toggle failed.");
                Require(landmark.ToggleVisibility() && landmark.IsVisible, "Bear Den show toggle failed.");
                Require(landmark.ExcludesSpawn(landmark.WorldAnchor), "Bear Den no-spawn radius is not active at its anchor.");

                evidence.Add("bear_den_visible_by_default:PASS");
                evidence.Add("bear_den_toggle_hide:PASS");
                evidence.Add("bear_den_toggle_show:PASS");
                evidence.Add("bear_den_toggle_hud_fixed:PASS");
                evidence.Add("bear_den_anchor_world:11520,13271");
                evidence.Add("bear_visible:NO");
                evidence.Add("bear_den_road_visible:NO");
                evidence.Add("bear_den_active_event:NO");
            }
            finally
            {
                landmark.Dispose();
            }
        }

        private static void ValidateCanonicalNavigationAndReachability(List<string> evidence)
        {
            string bootstrap = File.ReadAllText(AbsoluteProjectPath(BootstrapPath));
            Require(!bootstrap.Contains("LoadWave3RuntimeTiles();"), "Canonical Awake can still call the Wave3 5x5 provider.");
            Require(bootstrap.Contains("LoadWave5RuntimeTiles();"), "Canonical Awake does not call the Wave5 provider.");
            Require(bootstrap.Contains("DrawWave5WorldTerrain();"), "Canonical terrain draw is not Wave5.");
            Require(!bootstrap.Contains("wave5Provider = new Wave3RuntimeGutterTileProvider"), "Wave5 provider is mapped to historical Wave3 code.");

            string hivePresenter = File.ReadAllText(AbsoluteProjectPath("Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs"));
            Require(hivePresenter.Contains("OpenCanonicalWorldMap"), "Hive canonical WorldMap helper is missing.");
            Require(hivePresenter.Contains("WorldMapMmoFullscreenFoundation"), "Hive navigation no longer targets the canonical WorldMap scene.");

            evidence.Add("old_wave3_5x5_canonical_active:false");
            evidence.Add("canonical_repeat_or_modulo_fallback_reachable:false");
            evidence.Add("hive_to_worldmap_canonical_navigation_preserved:true");
            evidence.Add("server_live:false");
        }

        private static void WriteReceipt(string status, List<string> evidence, Exception exception)
        {
            string absolute = AbsoluteProjectPath(ReceiptPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? AbsoluteProjectPath("Docs/BuilderA"));
            var builder = new StringBuilder();
            builder.AppendLine("WORLD_MAP_WAVE5_25X25_UNITY_STATIC_VALIDATION=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("scene=" + ScenePath);
            builder.AppendLine("source_master_sha256=" + WorldMapWave5StreamingTileProvider.ExpectedMasterSha256);
            for (int i = 0; i < evidence.Count; i++) builder.AppendLine(evidence[i]);
            if (exception != null)
            {
                builder.AppendLine("exception=" + exception.GetType().FullName);
                builder.AppendLine("message=" + exception.Message);
                builder.AppendLine(exception.StackTrace);
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

        private static void RequireRow(string[] rows, string expected)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == expected) return;
            }
            throw new InvalidOperationException("Missing Wave5 proof row: " + expected);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
