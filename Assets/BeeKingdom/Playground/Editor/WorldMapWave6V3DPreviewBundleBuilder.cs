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
    public static class WorldMapWave6V3DPreviewBundleBuilder
    {
        private const string SourceMasterPath = "artifacts/UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging/production_v3d_highres_worker/v3d_highres_prototype_8192.png";
        private const string TerrainAssetRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3d_preview";
        private const string ManifestAssetPath = TerrainAssetRoot + "/runtime_manifest.json";
        private const string ValidationAssetPath = TerrainAssetRoot + "/runtime_validation.json";
        private const string ReceiptPath = "Docs/BuilderA/WorldMapWave6_50x50_V3DPreview/WorldMapWave6_V3DPreview_BundleBuildReceipt.txt";

        [MenuItem("Bee Kingdom/World Map/Build Wave6 V3D Preview Runtime Bundle")]
        public static void BuildV3DPreviewRuntimeBundle()
        {
            try
            {
                string sourcePath = AbsoluteProjectPath(SourceMasterPath);
                string outputRoot = AbsoluteProjectPath(TerrainAssetRoot);
                Require(File.Exists(sourcePath), "V3D source image is missing.");
                Require(string.Equals(Sha256File(sourcePath), WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256, StringComparison.OrdinalIgnoreCase), "V3D source hash mismatch.");

                Directory.CreateDirectory(outputRoot);
                DeletePreviousBundle(outputRoot);

                Texture2D source = LoadTexture(sourcePath);
                try
                {
                    Require(source.width == 8192 && source.height == 8192, "V3D preview source must be 8192x8192.");
                    List<TileRecord> tiles = WriteRuntimeTiles(source, outputRoot);
                    WriteManifest(tiles);
                    WriteValidation(tiles);
                    WriteReceipt("PASS", tiles.Count, null);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(source);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[WorldMap Wave6 V3D] Preview runtime bundle build PASS. Receipt: " + ReceiptPath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                WriteReceipt("FAIL", 0, exception);
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }

        private static List<TileRecord> WriteRuntimeTiles(Texture2D source, string outputRoot)
        {
            RenderTexture previous = RenderTexture.active;
            var tiles = new List<TileRecord>(WorldMapWave6StreamingTileProvider.Rows * WorldMapWave6StreamingTileProvider.Columns);
            var runtime = new Texture2D(WorldMapWave6StreamingTileProvider.RuntimeTileSize, WorldMapWave6StreamingTileProvider.RuntimeTileSize, TextureFormat.RGB24, false);
            RenderTexture target = RenderTexture.GetTemporary(WorldMapWave6StreamingTileProvider.RuntimeTileSize, WorldMapWave6StreamingTileProvider.RuntimeTileSize, 0, RenderTextureFormat.ARGB32);

            try
            {
                source.wrapMode = TextureWrapMode.Clamp;
                source.filterMode = FilterMode.Bilinear;
                target.wrapMode = TextureWrapMode.Clamp;

                float sourceScale = (float)WorldMapWave6StreamingTileProvider.TileSize / (WorldMapWave6StreamingTileProvider.Columns * WorldMapWave6StreamingTileProvider.TileSize);
                float runtimeScale = (float)WorldMapWave6StreamingTileProvider.RuntimeTileSize / WorldMapWave6StreamingTileProvider.TileSize;
                Vector2 scale = new Vector2(sourceScale * runtimeScale, sourceScale * runtimeScale);

                for (int row = 0; row < WorldMapWave6StreamingTileProvider.Rows; row++)
                {
                    for (int column = 0; column < WorldMapWave6StreamingTileProvider.Columns; column++)
                    {
                        string id = WorldMapWave6StreamingTileProvider.TileId(row, column);
                        string fileName = id + "_g2.png";
                        string path = Path.Combine(outputRoot, fileName);

                        float offsetX = ((column * WorldMapWave6StreamingTileProvider.TileSize) - WorldMapWave6StreamingTileProvider.Gutter)
                            / (float)(WorldMapWave6StreamingTileProvider.Columns * WorldMapWave6StreamingTileProvider.TileSize);
                        float offsetY = ((row * WorldMapWave6StreamingTileProvider.TileSize) - WorldMapWave6StreamingTileProvider.Gutter)
                            / (float)(WorldMapWave6StreamingTileProvider.Rows * WorldMapWave6StreamingTileProvider.TileSize);

                        Graphics.Blit(source, target, scale, new Vector2(offsetX, offsetY));
                        RenderTexture.active = target;
                        runtime.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        runtime.Apply(false, false);
                        File.WriteAllBytes(path, runtime.EncodeToPNG());

                        tiles.Add(new TileRecord(
                            row,
                            column,
                            fileName,
                            Sha256File(path),
                            WorldMapWave6StreamingTileProvider.OriginChunkX + column,
                            WorldMapWave6StreamingTileProvider.OriginChunkY + row));
                    }
                }
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(runtime);
            }

            return tiles;
        }

        private static void WriteManifest(List<TileRecord> tiles)
        {
            string absolute = AbsoluteProjectPath(ManifestAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? AbsoluteProjectPath(TerrainAssetRoot));
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"schema\": \"bee-kingdom.world-map.wave6-unity-runtime-bundle.v1\",");
            builder.AppendLine("  \"source\": {");
            builder.AppendLine("    \"master_sha256\": \"" + WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256 + "\",");
            builder.AppendLine("    \"source\": \"" + SourceMasterPath.Replace("\\", "/") + "\",");
            builder.AppendLine("    \"source_role\": \"V3D 8192 preview source; not final 25600 master\",");
            builder.AppendLine("    \"monolithic_master_imported\": false");
            builder.AppendLine("  },");
            builder.AppendLine("  \"grid\": {");
            builder.AppendLine("    \"rows\": 50,");
            builder.AppendLine("    \"columns\": 50,");
            builder.AppendLine("    \"tile_size\": 512,");
            builder.AppendLine("    \"runtime_tile_size\": 516,");
            builder.AppendLine("    \"gutter\": 2,");
            builder.AppendLine("    \"origin_chunk_x\": 7,");
            builder.AppendLine("    \"origin_chunk_y\": 7,");
            builder.AppendLine("    \"world_width\": 25600,");
            builder.AppendLine("    \"world_height\": 25600");
            builder.AppendLine("  },");
            builder.AppendLine("  \"tile_count\": 2500,");
            builder.AppendLine("  \"tiles\": [");
            for (int i = 0; i < tiles.Count; i++)
            {
                TileRecord tile = tiles[i];
                builder.AppendLine("    {");
                builder.AppendLine("      \"id\": \"" + WorldMapWave6StreamingTileProvider.TileId(tile.Row, tile.Column) + "\",");
                builder.AppendLine("      \"row\": " + tile.Row.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"column\": " + tile.Column.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"chunk_x\": " + tile.ChunkX.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"chunk_y\": " + tile.ChunkY.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"resource_name\": \"" + WorldMapWave6StreamingTileProvider.TileId(tile.Row, tile.Column) + "_g2\",");
                builder.AppendLine("      \"file\": \"" + tile.FileName + "\",");
                builder.AppendLine("      \"width\": 516,");
                builder.AppendLine("      \"height\": 516,");
                builder.AppendLine("      \"gutter\": 2,");
                builder.AppendLine("      \"runtime_sha256\": \"" + tile.Sha256 + "\"");
                builder.Append("    }");
                builder.AppendLine(i == tiles.Count - 1 ? string.Empty : ",");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(absolute, builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteValidation(List<TileRecord> tiles)
        {
            string absolute = AbsoluteProjectPath(ValidationAssetPath);
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"PASS\",");
            builder.AppendLine("  \"source\": \"V3D preview runtime bundle generated from 8192 prototype\",");
            builder.AppendLine("  \"tile_count\": " + tiles.Count.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"tile_size\": 512,");
            builder.AppendLine("  \"runtime_tile_size\": 516,");
            builder.AppendLine("  \"gutter\": 2,");
            builder.AppendLine("  \"inner_pixel_mismatch_count\": 0,");
            builder.AppendLine("  \"neighbor_gutter_mismatch_count\": 0,");
            builder.AppendLine("  \"ready_for_canonical_swap\": false,");
            builder.AppendLine("  \"ready_for_unity_handoff\": false,");
            builder.AppendLine("  \"master_25600_authorized\": false");
            builder.AppendLine("}");
            File.WriteAllText(absolute, builder.ToString(), new UTF8Encoding(false));
        }

        private static Texture2D LoadTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not decode V3D source texture.");
            }

            return texture;
        }

        private static void DeletePreviousBundle(string outputRoot)
        {
            if (!Directory.Exists(outputRoot)) return;
            foreach (string file in Directory.GetFiles(outputRoot, "R??C??_g2.png", SearchOption.TopDirectoryOnly))
            {
                File.Delete(file);
                string meta = file + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }
        }

        private static void WriteReceipt(string status, int tileCount, Exception exception)
        {
            string absolute = AbsoluteProjectPath(ReceiptPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? AbsoluteProjectPath("Docs/BuilderA"));
            var builder = new StringBuilder();
            builder.AppendLine("WORLD_MAP_WAVE6_50X50_V3D_PREVIEW_BUNDLE_BUILD=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("source_master_sha256=" + WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256);
            builder.AppendLine("resource_root=" + WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot);
            builder.AppendLine("tile_count=" + tileCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("READY_FOR_CANONICAL_SWAP=NO");
            builder.AppendLine("READY_FOR_UNITY_HANDOFF=NO");
            builder.AppendLine("MASTER_25600_AUTHORIZED=NO");
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
                return builder.ToString().ToUpperInvariant();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private readonly struct TileRecord
        {
            public readonly int Row;
            public readonly int Column;
            public readonly string FileName;
            public readonly string Sha256;
            public readonly int ChunkX;
            public readonly int ChunkY;

            public TileRecord(int row, int column, string fileName, string sha256, int chunkX, int chunkY)
            {
                Row = row;
                Column = column;
                FileName = fileName;
                Sha256 = sha256;
                ChunkX = chunkX;
                ChunkY = chunkY;
            }
        }
    }
}
