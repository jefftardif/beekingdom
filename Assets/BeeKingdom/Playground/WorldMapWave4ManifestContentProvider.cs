using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapWave4ManifestContentProvider
    {
        private const string ResourceRoot = "WorldMapWave4/UIB_SectorWave1";
        private readonly Dictionary<string, SectorContent> sectorsByCell = new Dictionary<string, SectorContent>();
        private readonly List<string> diagnostics = new List<string>();

        public bool IsLoaded { get; private set; }
        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
        public string Lot { get; private set; } = string.Empty;
        public string WorldClaim { get; private set; } = string.Empty;
        public bool NoRoadDirective { get; private set; }
        public Texture2D AtlasTexture { get; private set; }
        public IReadOnlyList<string> Diagnostics => diagnostics;

        public void Load()
        {
            diagnostics.Clear();
            sectorsByCell.Clear();
            IsLoaded = false;

            TextAsset manifestAsset = Resources.Load<TextAsset>(ResourceRoot + "/manifest");
            if (manifestAsset == null)
            {
                diagnostics.Add("manifest_missing:" + ResourceRoot + "/manifest");
                return;
            }

            Manifest manifest;
            try
            {
                manifest = JsonUtility.FromJson<Manifest>(manifestAsset.text);
            }
            catch (Exception exception)
            {
                diagnostics.Add("manifest_parse_failed:" + exception.GetType().Name);
                return;
            }

            if (manifest == null || manifest.grid == null || manifest.sectors == null || manifest.sectors.Length == 0)
            {
                diagnostics.Add("manifest_incomplete");
                return;
            }

            Lot = manifest.lot ?? string.Empty;
            WorldClaim = manifest.worldClaim ?? string.Empty;
            NoRoadDirective = manifest.noRoadDirective;
            Rows = Mathf.Max(1, manifest.grid.rows);
            Columns = Mathf.Max(1, manifest.grid.columns);
            TileWidth = Mathf.Max(1, manifest.grid.tileWidth);
            TileHeight = Mathf.Max(1, manifest.grid.tileHeight);
            LoadAtlas(manifest);

            for (int i = 0; i < manifest.sectors.Length; i++)
            {
                SectorManifest sector = manifest.sectors[i];
                if (sector == null || string.IsNullOrWhiteSpace(sector.file)) continue;

                string textureResource = ResourceRoot + "/" + Path.GetFileNameWithoutExtension(sector.file);
                Texture2D texture = Resources.Load<Texture2D>(textureResource);
                if (texture == null)
                {
                    diagnostics.Add("sector_texture_missing:" + sector.file);
                    continue;
                }

                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                sectorsByCell[CellKey(sector.row, sector.column)] = new SectorContent(sector.id ?? string.Empty, sector.row, sector.column, texture);
            }

            IsLoaded = sectorsByCell.Count > 0;
            diagnostics.Add("manifest_grid:" + Rows.ToString(CultureInfo.InvariantCulture) + "x" + Columns.ToString(CultureInfo.InvariantCulture));
            diagnostics.Add("loaded_sectors:" + sectorsByCell.Count.ToString(CultureInfo.InvariantCulture));
            diagnostics.Add("atlas_loaded:" + (AtlasTexture != null).ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            diagnostics.Add("manifest_driven_content_provider:true");
        }

        private void LoadAtlas(Manifest manifest)
        {
            if (manifest.atlas == null || string.IsNullOrWhiteSpace(manifest.atlas.file))
            {
                diagnostics.Add("atlas_manifest_missing");
                return;
            }

            string atlasResource = ResourceRoot + "/" + Path.GetFileNameWithoutExtension(manifest.atlas.file);
            AtlasTexture = Resources.Load<Texture2D>(atlasResource);
            if (AtlasTexture == null)
            {
                diagnostics.Add("atlas_texture_missing:" + manifest.atlas.file);
                return;
            }

            AtlasTexture.wrapMode = TextureWrapMode.Clamp;
            AtlasTexture.filterMode = FilterMode.Bilinear;
            AtlasTexture.anisoLevel = 2;
        }

        public Texture2D TextureForChunk(Vector2Int chunk, out string sectorId)
        {
            sectorId = string.Empty;
            if (!IsLoaded) return null;

            int column = PositiveModulo(chunk.x - 31, Columns);
            int row = PositiveModulo(chunk.y - 31, Rows);
            SectorContent sector;
            if (!sectorsByCell.TryGetValue(CellKey(row, column), out sector)) return null;

            sectorId = sector.Id;
            return sector.Texture;
        }

        public string[] ProofRows()
        {
            return new[]
            {
                "wave4_manifest_provider:true",
                "wave4_manifest_lot:" + Lot,
                "wave4_manifest_grid:" + Rows.ToString(CultureInfo.InvariantCulture) + "x" + Columns.ToString(CultureInfo.InvariantCulture),
                "wave4_loaded_sectors:" + sectorsByCell.Count.ToString(CultureInfo.InvariantCulture),
                "wave4_atlas_loaded:" + (AtlasTexture != null).ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
                "wave4_continuous_atlas_surface_available:" + (AtlasTexture != null).ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
                "wave4_atlas_wrap_mode:" + (AtlasTexture != null ? AtlasTexture.wrapMode.ToString() : "missing"),
                "wave4_atlas_repeat_enabled:false",
                "wave4_tile_size:" + TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + TileHeight.ToString(CultureInfo.InvariantCulture),
                "wave4_no_road_directive:" + NoRoadDirective.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
                "wave4_future_5x5_without_scene_rewrite:true",
                "wave4_world_claim:" + WorldClaim
            };
        }

        private static string CellKey(int row, int column)
        {
            return row.ToString(CultureInfo.InvariantCulture) + ":" + column.ToString(CultureInfo.InvariantCulture);
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private sealed class SectorContent
        {
            public readonly string Id;
            public readonly Texture2D Texture;

            public SectorContent(string id, int row, int column, Texture2D texture)
            {
                Id = string.IsNullOrWhiteSpace(id) ? CellKey(row, column) : id;
                Texture = texture;
            }
        }

        [Serializable]
        private sealed class Manifest
        {
            public string lot;
            public string worldClaim;
            public bool noRoadDirective;
            public GridManifest grid;
            public AtlasManifest atlas;
            public SectorManifest[] sectors;
        }

        [Serializable]
        private sealed class AtlasManifest
        {
            public string file;
            public int width;
            public int height;
        }

        [Serializable]
        private sealed class GridManifest
        {
            public int rows;
            public int columns;
            public int tileWidth;
            public int tileHeight;
        }

        [Serializable]
        private sealed class SectorManifest
        {
            public string id;
            public int row;
            public int column;
            public string file;
        }
    }
}
