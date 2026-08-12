using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapWave5StreamingTileProvider : IDisposable
    {
        public const string ResourceRoot = "WorldMapWave5Runtime/UIB_ImmenseContinuousMaster25x25_v1";
        public const string ExpectedMasterSha256 = "50f3ff9640251f365484f31de4aa5ab542587381e5f8eeb9324d67be37125913";
        public const int OriginChunkX = 20;
        public const int OriginChunkY = 20;
        public const int Rows = 25;
        public const int Columns = 25;
        public const int TileSize = 512;
        public const int RuntimeTileSize = 516;
        public const int Gutter = 2;
        public const int CacheCapacity = 96;
        public const int PrefetchRing = 1;

        private readonly Dictionary<Vector2Int, Wave5RuntimeTile> cache = new Dictionary<Vector2Int, Wave5RuntimeTile>();
        private readonly Dictionary<Vector2Int, PendingTile> pending = new Dictionary<Vector2Int, PendingTile>();
        private readonly Dictionary<Vector2Int, int> lastTouchedFrame = new Dictionary<Vector2Int, int>();
        private readonly HashSet<Vector2Int> desiredCore = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> desiredPrefetch = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> scratchCoordinates = new List<Vector2Int>(128);
        private readonly List<Wave5RuntimeTile> visibleTiles = new List<Wave5RuntimeTile>(96);
        private Wave5RuntimeManifest manifest;

        public bool ManifestReady { get; private set; }
        public bool HasLoadFailure { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public int CachedTileCount => cache.Count;
        public int PendingTileCount => pending.Count;
        public int RequiredVisibleTileCount { get; private set; }
        public int LoadedVisibleTileCount { get; private set; }
        public bool HasAllVisibleTiles => ManifestReady && RequiredVisibleTileCount > 0 && RequiredVisibleTileCount == LoadedVisibleTileCount;
        public Rect WorldBounds => new Rect(OriginChunkX * TileSize, OriginChunkY * TileSize, Columns * TileSize, Rows * TileSize);
        public string MasterSha256 => manifest != null && manifest.source != null ? manifest.source.master_sha256 : string.Empty;

        public bool Initialize(Vector2 center, float zoom, int screenWidth, int screenHeight)
        {
            TextAsset manifestAsset = Resources.Load<TextAsset>(ResourceRoot + "/runtime_manifest");
            if (manifestAsset == null)
            {
                Fail("Wave5 runtime_manifest.json is missing from Resources.");
                return false;
            }

            try
            {
                manifest = JsonUtility.FromJson<Wave5RuntimeManifest>(manifestAsset.text);
            }
            catch (Exception exception)
            {
                Fail("Wave5 runtime manifest could not be parsed: " + exception.Message);
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
                    Fail("Visible Wave5 tile failed to load: " + TileId(coordinate));
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
                        Fail("Prefetch Wave5 tile failed to load: " + TileId(coordinate));
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

        public IReadOnlyList<Wave5RuntimeTile> VisibleTiles
        {
            get
            {
                RefreshVisibleTiles();
                return visibleTiles;
            }
        }

        public bool TryGetTile(int row, int column, out Wave5RuntimeTile tile)
        {
            return cache.TryGetValue(new Vector2Int(column, row), out tile);
        }

        public Wave5TileRange CalculateRange(Vector2 center, float zoom, int screenWidth, int screenHeight, int ring)
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
            return new Wave5TileRange(
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
            foreach (Wave5RuntimeTile tile in cache.Values)
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
            Wave5TileRange core = CalculateRange(center, zoom, screenWidth, screenHeight, 0);
            Wave5TileRange prefetch = CalculateRange(center, zoom, screenWidth, screenHeight, PrefetchRing);
            AddRange(core, desiredCore);
            AddRange(prefetch, desiredPrefetch);
            RequiredVisibleTileCount = desiredCore.Count;
        }

        private static void AddRange(Wave5TileRange range, HashSet<Vector2Int> destination)
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
                if (!cache.TryGetValue(coordinate, out Wave5RuntimeTile tile)) continue;
                visibleTiles.Add(tile);
                LoadedVisibleTileCount++;
                Touch(coordinate);
            }

            visibleTiles.Sort(CompareTiles);
        }

        private static int CompareTiles(Wave5RuntimeTile left, Wave5RuntimeTile right)
        {
            int row = left.Row.CompareTo(right.Row);
            return row != 0 ? row : left.Column.CompareTo(right.Column);
        }

        private bool LoadSynchronously(Vector2Int coordinate)
        {
            Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + "/" + TileId(coordinate));
            if (!ConfigureTexture(texture)) return false;
            cache[coordinate] = CreateTile(coordinate, texture);
            Touch(coordinate);
            return true;
        }

        private void QueueAsynchronousLoad(Vector2Int coordinate)
        {
            ResourceRequest request = Resources.LoadAsync<Texture2D>(ResourceRoot + "/" + TileId(coordinate));
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
                    Fail("Asynchronous Wave5 tile failed to load: " + TileId(entry.Key));
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

        private static Wave5RuntimeTile CreateTile(Vector2Int coordinate, Texture2D texture)
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
            return new Wave5RuntimeTile(TileId(row, column), row, column, chunkX, chunkY, texture, worldRect, gutterWorldRect);
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

        private static bool ValidateManifest(Wave5RuntimeManifest value, out string failure)
        {
            if (value == null)
            {
                failure = "Wave5 runtime manifest is empty.";
                return false;
            }

            if (value.schema != "bee-kingdom.world-map.wave5-unity-runtime-bundle.v1")
            {
                failure = "Wave5 runtime manifest schema is not supported.";
                return false;
            }

            if (value.source == null || !string.Equals(value.source.master_sha256, ExpectedMasterSha256, StringComparison.OrdinalIgnoreCase))
            {
                failure = "Wave5 runtime master SHA-256 does not match the frozen package.";
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
                failure = "Wave5 runtime grid/origin/gutter contract is invalid.";
                return false;
            }

            if (value.tile_count != Rows * Columns || value.tiles == null || value.tiles.Length != Rows * Columns)
            {
                failure = "Wave5 runtime manifest does not contain 625 tiles.";
                return false;
            }

            bool[,] seen = new bool[Rows, Columns];
            for (int i = 0; i < value.tiles.Length; i++)
            {
                Wave5ManifestTile tile = value.tiles[i];
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
                    failure = "Wave5 runtime tile record is invalid at index " + i + ".";
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
            Debug.LogError("[WorldMap Wave5] " + reason);
        }

        [Serializable]
        private sealed class Wave5RuntimeManifest
        {
            public string schema;
            public Wave5ManifestSource source;
            public Wave5ManifestGrid grid;
            public int tile_count;
            public Wave5ManifestTile[] tiles;
        }

        [Serializable]
        private sealed class Wave5ManifestSource
        {
            public string master_sha256;
        }

        [Serializable]
        private sealed class Wave5ManifestGrid
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
        private sealed class Wave5ManifestTile
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

    public readonly struct Wave5RuntimeTile
    {
        public readonly string Id;
        public readonly int Row;
        public readonly int Column;
        public readonly int ChunkX;
        public readonly int ChunkY;
        public readonly Texture2D Texture;
        public readonly Rect WorldRect;
        public readonly Rect GutterWorldRect;

        public Wave5RuntimeTile(string id, int row, int column, int chunkX, int chunkY, Texture2D texture, Rect worldRect, Rect gutterWorldRect)
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

    public readonly struct Wave5TileRange
    {
        public readonly int MinRow;
        public readonly int MaxRow;
        public readonly int MinColumn;
        public readonly int MaxColumn;

        public int Count => (MaxRow - MinRow + 1) * (MaxColumn - MinColumn + 1);

        public Wave5TileRange(int minRow, int maxRow, int minColumn, int maxColumn)
        {
            MinRow = minRow;
            MaxRow = maxRow;
            MinColumn = minColumn;
            MaxColumn = maxColumn;
        }
    }
}
