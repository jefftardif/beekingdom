using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapWave6StreamingTileProvider : IDisposable
    {
        public const string ResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v1";
        public const string V3DPreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3d_preview";
        public const string V3ECandidateResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3e_candidate";
        public const string V3MPreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3m_preview";
        public const string V3VCandidateResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3v_candidate";
        public const string V3OReducedAuditPreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3o_reduced_audit_preview";
        public const string RouteLockCoherentProofResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_route_lock_coherent_proof";
        public const string RouteLock8192ScaleBridgeProofResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_route_lock_8192_scale_bridge_proof";
        public const string Wave5Method12288PreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview";
        public const string SupportCenterNativeAuditPreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_support_center_native_audit_preview";
        public const string V2INativeAuditPreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_native_audit_preview";
        public const string V2OPerimeterAuditPreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2o_perimeter_audit_preview";
        public const string V2IRepairAuditPreviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview";
        public const string V2ISelectedHdLocalRepairReviewResourceRoot = "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_selected_hd_local_repair_review";
        public const string ExpectedMasterSha256 = "03793053993cf71af0ed1997fbb8a00c695ca32f31ddd512b28625002c033203";
        public const string V3DPreviewExpectedMasterSha256 = "5331FB1C5E5A8029FC205425D8C4DCF23C0794D79B5DA49DDB58368BDB48DF37";
        public const string V3ECandidateExpectedMasterSha256 = "978C79C66792040F3FDE79077BE8506041FD993E695599EDCD693F2FFB60CDE3";
        public const string V3MPreviewExpectedMasterSha256 = "5734EA1D4A7A840E7CF036B683B5D109A85D1E8D615EBF796CDA682F583A7A3B";
        public const string V3VCandidateExpectedMasterSha256 = "PENDING_V3V_PRODUCTION_SCALE_SOURCE_SHA256";
        public const string V3OReducedAuditPreviewExpectedMasterSha256 = "8C0EB5250019B253BFE712D76B079E209AFE399DA645D8D68BD0BD77462F2D5B";
        public const string RouteLockCoherentProofExpectedMasterSha256 = "D0C5C1F1B81D820FD29C10DCEA76ECA18E3B333770611ED07D0BC988F8807511";
        public const string RouteLock8192ScaleBridgeProofExpectedMasterSha256 = "307FF4B6EC6D08FCEF196AEF5298AA79F5D5FD7AFC634BE4834CB999BE8ACD0F";
        public const string Wave5Method12288PreviewExpectedMasterSha256 = "3CE816052FFF97BCDE78251FA930C4D725DC622120D3644C806A9C1BE1330697";
        public const string SupportCenterNativeAuditPreviewExpectedMasterSha256 = "EFE4266F86D5D70C4CA54023B6443B8DB2687850865C38336FFB9D0E46A7BAA4";
        public const string V2INativeAuditPreviewExpectedMasterSha256 = "0779DC8526B87B8E9449B74F9414CF2D9D938960A87569246624F8B608F17160";
        public const string V2OPerimeterAuditPreviewExpectedMasterSha256 = "84A7BF606DC9107E9AAEB6C5F4D55EBFD84AB3C85E6F1EE57D1C3FEC4AE6A302";
        public const string V2IRepairAuditPreviewExpectedMasterSha256 = "ACF680AAA2A47399858C88C1182646E54D0637FF6A22D6ACCFFDD795BCF9EF3B";
        public const string V2ISelectedHdLocalRepairReviewExpectedMasterSha256 = "73AB71872949D75DDDE40B3CA945B1CE3B4AC2B71AA76AF031BD54FD951F57DF";
        public const int OriginChunkX = 7;
        public const int OriginChunkY = 7;
        public const int Rows = 50;
        public const int Columns = 50;
        public const int TileSize = 512;
        public const int RuntimeTileSize = 516;
        public const int Gutter = 2;
        public const int CacheCapacity = 128;
        public const int PrefetchRing = 1;

        private readonly Dictionary<Vector2Int, Wave6RuntimeTile> cache = new Dictionary<Vector2Int, Wave6RuntimeTile>();
        private readonly Dictionary<Vector2Int, PendingTile> pending = new Dictionary<Vector2Int, PendingTile>();
        private readonly Dictionary<Vector2Int, int> lastTouchedFrame = new Dictionary<Vector2Int, int>();
        private readonly HashSet<Vector2Int> desiredCore = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> desiredPrefetch = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> scratchCoordinates = new List<Vector2Int>(128);
        private readonly List<Wave6RuntimeTile> visibleTiles = new List<Wave6RuntimeTile>(128);
        private readonly string resourceRoot;
        private readonly string expectedMasterSha256;
        private Wave6RuntimeManifest manifest;

        public WorldMapWave6StreamingTileProvider()
            : this(ResourceRoot, ExpectedMasterSha256)
        {
        }

        public WorldMapWave6StreamingTileProvider(string resourceRoot, string expectedMasterSha256)
        {
            this.resourceRoot = string.IsNullOrEmpty(resourceRoot) ? ResourceRoot : resourceRoot;
            this.expectedMasterSha256 = string.IsNullOrEmpty(expectedMasterSha256) ? ExpectedMasterSha256 : expectedMasterSha256;
        }

        public bool ManifestReady { get; private set; }
        public bool HasLoadFailure { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public int CachedTileCount => cache.Count;
        public int PendingTileCount => pending.Count;
        public int RequiredVisibleTileCount { get; private set; }
        public int LoadedVisibleTileCount { get; private set; }
        public bool HasAllVisibleTiles => ManifestReady && RequiredVisibleTileCount > 0 && RequiredVisibleTileCount == LoadedVisibleTileCount;
        public Rect WorldBounds => new Rect(OriginChunkX * TileSize, OriginChunkY * TileSize, Columns * TileSize, Rows * TileSize);
        public string MasterSha256 => LoadedPackageSha256;

        private string LoadedPackageSha256
        {
            get
            {
                if (manifest == null || manifest.source == null) return string.Empty;
                return !string.IsNullOrEmpty(manifest.source.source_superpanel_sha256)
                    ? manifest.source.source_superpanel_sha256
                    : manifest.source.master_sha256;
            }
        }

        public bool Initialize(Vector2 center, float zoom, int screenWidth, int screenHeight)
        {
            TextAsset manifestAsset = Resources.Load<TextAsset>(resourceRoot + "/runtime_manifest");
            if (manifestAsset == null)
            {
                Fail("Wave6 runtime_manifest.json is missing from Resources: " + resourceRoot);
                return false;
            }

            try
            {
                manifest = JsonUtility.FromJson<Wave6RuntimeManifest>(manifestAsset.text);
            }
            catch (Exception exception)
            {
                Fail("Wave6 runtime manifest could not be parsed: " + exception.Message);
                return false;
            }

            if (!ValidateManifest(manifest, out string validationFailure))
            {
                Fail(validationFailure);
                return false;
            }

            ManifestReady = true;
            HasLoadFailure = false;
            FailureReason = string.Empty;
            UpdateStreaming(center, zoom, screenWidth, screenHeight, true);
            return HasAllVisibleTiles;
        }

        public void UpdateStreaming(Vector2 center, float zoom, int screenWidth, int screenHeight, bool primeSynchronously = false)
        {
            if (!ManifestReady || HasLoadFailure) return;

            CompletePendingRequests();
            BuildDesiredSets(center, zoom, screenWidth, screenHeight);

            foreach (Vector2Int coordinate in desiredCore)
            {
                Touch(coordinate);
                if (cache.ContainsKey(coordinate)) continue;
                if (!LoadSynchronously(coordinate))
                {
                    Fail("Visible Wave6 tile failed to load: " + TileId(coordinate));
                    return;
                }
            }

            foreach (Vector2Int coordinate in desiredPrefetch)
            {
                Touch(coordinate);
                if (cache.ContainsKey(coordinate) || pending.ContainsKey(coordinate)) continue;
                if (primeSynchronously)
                {
                    if (!LoadSynchronously(coordinate))
                    {
                        Fail("Prefetch Wave6 tile failed to load: " + TileId(coordinate));
                        return;
                    }
                }
                else if (pending.Count < 6)
                {
                    QueueAsynchronousLoad(coordinate);
                }
            }

            EvictLeastRecentlyUsedTiles();
            RefreshVisibleTiles();
        }

        public IReadOnlyList<Wave6RuntimeTile> VisibleTiles
        {
            get
            {
                RefreshVisibleTiles();
                return visibleTiles;
            }
        }

        public bool TryGetTile(int row, int column, out Wave6RuntimeTile tile)
        {
            return cache.TryGetValue(new Vector2Int(column, row), out tile);
        }

        public Wave6TileRange CalculateRange(Vector2 center, float zoom, int screenWidth, int screenHeight, int ring)
        {
            float safeZoom = Mathf.Max(0.01f, zoom);
            float halfWidth = Mathf.Max(1, screenWidth) * 0.5f / safeZoom;
            float halfHeight = Mathf.Max(1, screenHeight) * 0.5f / safeZoom;
            Rect bounds = WorldBounds;
            float epsilon = 0.001f;
            int minColumn = Mathf.FloorToInt((center.x - halfWidth - bounds.xMin) / TileSize);
            int maxColumn = Mathf.FloorToInt((center.x + halfWidth - epsilon - bounds.xMin) / TileSize);
            int minRow = Mathf.FloorToInt((center.y - halfHeight - bounds.yMin) / TileSize);
            int maxRow = Mathf.FloorToInt((center.y + halfHeight - epsilon - bounds.yMin) / TileSize);
            return new Wave6TileRange(
                Mathf.Clamp(minRow - ring, 0, Rows - 1),
                Mathf.Clamp(maxRow + ring, 0, Rows - 1),
                Mathf.Clamp(minColumn - ring, 0, Columns - 1),
                Mathf.Clamp(maxColumn + ring, 0, Columns - 1));
        }

        public static Vector2 TileAnchorWorld(int row, int column, float localX, float localY)
        {
            return new Vector2(
                (OriginChunkX + column) * TileSize + localX,
                (OriginChunkY + row) * TileSize + localY);
        }

        public static string TileId(int row, int column)
        {
            return "R" + row.ToString("00") + "C" + column.ToString("00");
        }

        public void Dispose()
        {
            foreach (PendingTile request in pending.Values)
            {
                if (request.Request != null && request.Request.isDone && request.Request.asset != null)
                {
                    Resources.UnloadAsset(request.Request.asset);
                }
            }

            pending.Clear();
            foreach (Wave6RuntimeTile tile in cache.Values)
            {
                if (tile.Texture != null) Resources.UnloadAsset(tile.Texture);
            }

            cache.Clear();
            lastTouchedFrame.Clear();
            desiredCore.Clear();
            desiredPrefetch.Clear();
            visibleTiles.Clear();
        }

        private void BuildDesiredSets(Vector2 center, float zoom, int screenWidth, int screenHeight)
        {
            desiredCore.Clear();
            desiredPrefetch.Clear();
            Wave6TileRange core = CalculateRange(center, zoom, screenWidth, screenHeight, 0);
            Wave6TileRange prefetch = CalculateRange(center, zoom, screenWidth, screenHeight, PrefetchRing);
            AddRange(core, desiredCore);
            AddRange(prefetch, desiredPrefetch);
            RequiredVisibleTileCount = desiredCore.Count;
        }

        private static void AddRange(Wave6TileRange range, HashSet<Vector2Int> destination)
        {
            for (int row = range.MinRow; row <= range.MaxRow; row++)
            {
                for (int column = range.MinColumn; column <= range.MaxColumn; column++)
                {
                    destination.Add(new Vector2Int(column, row));
                }
            }
        }

        private void RefreshVisibleTiles()
        {
            visibleTiles.Clear();
            LoadedVisibleTileCount = 0;
            foreach (Vector2Int coordinate in desiredCore)
            {
                if (!cache.TryGetValue(coordinate, out Wave6RuntimeTile tile)) continue;
                visibleTiles.Add(tile);
                LoadedVisibleTileCount++;
                Touch(coordinate);
            }

            visibleTiles.Sort(CompareTiles);
        }

        private static int CompareTiles(Wave6RuntimeTile left, Wave6RuntimeTile right)
        {
            int row = left.Row.CompareTo(right.Row);
            return row != 0 ? row : left.Column.CompareTo(right.Column);
        }

        private bool LoadSynchronously(Vector2Int coordinate)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourceRoot + "/" + TileId(coordinate));
            if (!ConfigureTexture(texture)) return false;
            cache[coordinate] = CreateTile(coordinate, texture);
            Touch(coordinate);
            return true;
        }

        private void QueueAsynchronousLoad(Vector2Int coordinate)
        {
            ResourceRequest request = Resources.LoadAsync<Texture2D>(resourceRoot + "/" + TileId(coordinate));
            pending[coordinate] = new PendingTile(coordinate, request);
        }

        private void CompletePendingRequests()
        {
            if (pending.Count == 0) return;
            scratchCoordinates.Clear();
            foreach (KeyValuePair<Vector2Int, PendingTile> entry in pending)
            {
                if (!entry.Value.Request.isDone) continue;
                Texture2D texture = entry.Value.Request.asset as Texture2D;
                if (!ConfigureTexture(texture))
                {
                    Fail("Asynchronous Wave6 tile failed to load: " + TileId(entry.Key));
                    return;
                }

                cache[entry.Key] = CreateTile(entry.Key, texture);
                Touch(entry.Key);
                scratchCoordinates.Add(entry.Key);
            }

            for (int i = 0; i < scratchCoordinates.Count; i++) pending.Remove(scratchCoordinates[i]);
        }

        private static bool ConfigureTexture(Texture2D texture)
        {
            if (texture == null || texture.width != RuntimeTileSize || texture.height != RuntimeTileSize) return false;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 1;
            return true;
        }

        private static Wave6RuntimeTile CreateTile(Vector2Int coordinate, Texture2D texture)
        {
            int row = coordinate.y;
            int column = coordinate.x;
            int chunkX = OriginChunkX + column;
            int chunkY = OriginChunkY + row;
            Rect worldRect = new Rect(chunkX * TileSize, chunkY * TileSize, TileSize, TileSize);
            Rect gutterWorldRect = new Rect(
                worldRect.xMin - Gutter,
                worldRect.yMin - Gutter,
                TileSize + Gutter * 2,
                TileSize + Gutter * 2);
            return new Wave6RuntimeTile(TileId(row, column), row, column, chunkX, chunkY, texture, worldRect, gutterWorldRect);
        }

        private void EvictLeastRecentlyUsedTiles()
        {
            if (cache.Count <= CacheCapacity) return;
            scratchCoordinates.Clear();
            foreach (Vector2Int coordinate in cache.Keys)
            {
                if (desiredPrefetch.Contains(coordinate) || pending.ContainsKey(coordinate)) continue;
                scratchCoordinates.Add(coordinate);
            }

            scratchCoordinates.Sort((left, right) => LastTouched(left).CompareTo(LastTouched(right)));
            int index = 0;
            while (cache.Count > CacheCapacity && index < scratchCoordinates.Count)
            {
                Vector2Int coordinate = scratchCoordinates[index++];
                Texture2D texture = cache[coordinate].Texture;
                cache.Remove(coordinate);
                lastTouchedFrame.Remove(coordinate);
                if (texture != null) Resources.UnloadAsset(texture);
            }
        }

        private void Touch(Vector2Int coordinate)
        {
            lastTouchedFrame[coordinate] = Time.frameCount;
        }

        private int LastTouched(Vector2Int coordinate)
        {
            return lastTouchedFrame.TryGetValue(coordinate, out int frame) ? frame : int.MinValue;
        }

        private static string TileId(Vector2Int coordinate)
        {
            return TileId(coordinate.y, coordinate.x) + "_g2";
        }

        private bool ValidateManifest(Wave6RuntimeManifest value, out string failure)
        {
            if (value == null)
            {
                failure = "Wave6 runtime manifest is empty.";
                return false;
            }

            bool isV1Manifest = value.schema == "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1";
            bool isV2Manifest = value.schema == "bee-kingdom.world-map.wave6-unity-runtime-bundle.v2";
            if (!isV1Manifest && !isV2Manifest)
            {
                failure = "Wave6 runtime manifest schema is not supported.";
                return false;
            }

            if (value.source == null)
            {
                failure = "Wave6 runtime manifest source is missing.";
                return false;
            }

            string packageSha256 = isV2Manifest && !string.IsNullOrEmpty(value.source.source_superpanel_sha256)
                ? value.source.source_superpanel_sha256
                : value.source.master_sha256;
            if (!string.Equals(packageSha256, expectedMasterSha256, StringComparison.OrdinalIgnoreCase))
            {
                failure = "Wave6 runtime package SHA-256 does not match the frozen package.";
                return false;
            }

            if (value.grid == null
                || value.grid.rows != Rows
                || value.grid.columns != Columns
                || value.grid.tile_size != TileSize
                || value.grid.runtime_tile_size != RuntimeTileSize
                || value.grid.gutter != Gutter
                || value.grid.origin_chunk_x != OriginChunkX
                || value.grid.origin_chunk_y != OriginChunkY)
            {
                failure = "Wave6 runtime grid/origin/gutter contract is invalid.";
                return false;
            }

            if (value.tile_count != Rows * Columns || value.tiles == null || value.tiles.Length != Rows * Columns)
            {
                failure = "Wave6 runtime manifest does not contain 2500 tiles.";
                return false;
            }

            bool[,] seen = new bool[Rows, Columns];
            for (int i = 0; i < value.tiles.Length; i++)
            {
                Wave6ManifestTile tile = value.tiles[i];
                if (tile == null
                    || tile.row < 0 || tile.row >= Rows
                    || tile.column < 0 || tile.column >= Columns
                    || tile.chunk_x != OriginChunkX + tile.column
                    || tile.chunk_y != OriginChunkY + tile.row
                    || tile.width != RuntimeTileSize
                    || tile.height != RuntimeTileSize
                    || tile.gutter != Gutter
                    || tile.resource_name != TileId(tile.row, tile.column) + "_g2"
                    || seen[tile.row, tile.column])
                {
                    failure = "Wave6 runtime tile record is invalid at index " + i + ".";
                    return false;
                }

                seen[tile.row, tile.column] = true;
            }

            failure = string.Empty;
            return true;
        }

        private void Fail(string reason)
        {
            HasLoadFailure = true;
            FailureReason = reason;
            Debug.LogError("[WorldMap Wave6] " + reason);
        }

        [Serializable]
        private sealed class Wave6RuntimeManifest
        {
            public string schema;
            public Wave6ManifestSource source;
            public Wave6ManifestGrid grid;
            public int tile_count;
            public Wave6ManifestTile[] tiles;
        }

        [Serializable]
        private sealed class Wave6ManifestSource
        {
            public string master_sha256;
            public string source_superpanel_sha256;
        }

        [Serializable]
        private sealed class Wave6ManifestGrid
        {
            public int rows;
            public int columns;
            public int tile_size;
            public int runtime_tile_size;
            public int gutter;
            public int origin_chunk_x;
            public int origin_chunk_y;
        }

        [Serializable]
        private sealed class Wave6ManifestTile
        {
            public int row;
            public int column;
            public int chunk_x;
            public int chunk_y;
            public string resource_name;
            public int width;
            public int height;
            public int gutter;
        }

        private sealed class PendingTile
        {
            public readonly Vector2Int Coordinate;
            public readonly ResourceRequest Request;

            public PendingTile(Vector2Int coordinate, ResourceRequest request)
            {
                Coordinate = coordinate;
                Request = request;
            }
        }
    }

    public readonly struct Wave6RuntimeTile
    {
        public static readonly Rect FullTextureUv = new Rect(0f, 0f, 1f, 1f);

        public readonly string Id;
        public readonly int Row;
        public readonly int Column;
        public readonly int ChunkX;
        public readonly int ChunkY;
        public readonly Texture2D Texture;
        public readonly Rect WorldRect;
        public readonly Rect GutterWorldRect;
        public Rect CoreUv => new Rect(
            WorldMapWave6StreamingTileProvider.Gutter / (float)WorldMapWave6StreamingTileProvider.RuntimeTileSize,
            WorldMapWave6StreamingTileProvider.Gutter / (float)WorldMapWave6StreamingTileProvider.RuntimeTileSize,
            WorldMapWave6StreamingTileProvider.TileSize / (float)WorldMapWave6StreamingTileProvider.RuntimeTileSize,
            WorldMapWave6StreamingTileProvider.TileSize / (float)WorldMapWave6StreamingTileProvider.RuntimeTileSize);

        public Wave6RuntimeTile(string id, int row, int column, int chunkX, int chunkY, Texture2D texture, Rect worldRect, Rect gutterWorldRect)
        {
            Id = id;
            Row = row;
            Column = column;
            ChunkX = chunkX;
            ChunkY = chunkY;
            Texture = texture;
            WorldRect = worldRect;
            GutterWorldRect = gutterWorldRect;
        }
    }

    public readonly struct Wave6TileRange
    {
        public readonly int MinRow;
        public readonly int MaxRow;
        public readonly int MinColumn;
        public readonly int MaxColumn;

        public int Count => (MaxRow - MinRow + 1) * (MaxColumn - MinColumn + 1);

        public Wave6TileRange(int minRow, int maxRow, int minColumn, int maxColumn)
        {
            MinRow = minRow;
            MaxRow = maxRow;
            MinColumn = minColumn;
            MaxColumn = maxColumn;
        }
    }
}
