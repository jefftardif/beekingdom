using System;

namespace BeeKingdom.WorldMap
{
    // Configuration complete de la carte du monde. Immutable apres construction ;
    // toujours valider via Validate() avant utilisation (le fait WorldGrid/WorldManager).
    public sealed class WorldConfiguration
    {
        public long ChunkSize { get; }
        public long TileSize { get; }
        public long TilesPerChunk => ChunkSize / TileSize;
        public StreamingSettings Streaming { get; }
        public CameraSettings Camera { get; }
        public LodSettings Lod { get; }
        public PoolSettings Pool { get; }

        public WorldConfiguration(
            long chunkSize,
            long tileSize,
            StreamingSettings streaming,
            CameraSettings camera,
            LodSettings lod,
            PoolSettings pool)
        {
            ChunkSize = chunkSize;
            TileSize = tileSize;
            Streaming = streaming ?? new StreamingSettings();
            Camera = camera ?? new CameraSettings();
            Lod = lod ?? new LodSettings();
            Pool = pool ?? new PoolSettings();
            Validate();
        }

        public static WorldConfiguration CreateDefault()
        {
            return new WorldConfiguration(
                chunkSize: 64,
                tileSize: 1,
                streaming: new StreamingSettings(),
                camera: new CameraSettings(),
                lod: new LodSettings(),
                pool: new PoolSettings());
        }

        public void Validate()
        {
            if (ChunkSize < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(ChunkSize), "The chunk size must be at least 4 world units.");
            }

            if (TileSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(TileSize), "The tile size must be strictly positive.");
            }

            if (ChunkSize % TileSize != 0)
            {
                throw new ArgumentException("The chunk size must be a multiple of the tile size.");
            }

            if (Streaming.LoadRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Streaming), "The load radius must be positive.");
            }

            if (Streaming.UnloadRadius < Streaming.LoadRadius)
            {
                throw new ArgumentException("The unload radius must be greater than or equal to the load radius.");
            }

            if (Streaming.MaxConcurrentLoads < 1 || Streaming.MaxConcurrentUnloads < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(Streaming), "At least one concurrent load/unload must be allowed.");
            }

            if (Camera.ZoomMin < 1f || Camera.ZoomMax < Camera.ZoomMin)
            {
                throw new ArgumentOutOfRangeException(nameof(Camera), "The camera zoom range is invalid.");
            }

            if (Camera.DecelerationTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(Camera), "The camera deceleration time must be strictly positive.");
            }

            if (Camera.HasBounds)
            {
                if (Camera.MinBound.X > Camera.MaxBound.X || Camera.MinBound.Y > Camera.MaxBound.Y)
                {
                    throw new ArgumentException("The camera bounds are inverted.");
                }
            }

            if (Lod.NearDistance < 0f || Lod.MidDistance < Lod.NearDistance || Lod.FarDistance < Lod.MidDistance)
            {
                throw new ArgumentOutOfRangeException(nameof(Lod), "The LOD distances must be increasing.");
            }

            if (Pool.MaxPerKey < 0 || Pool.WarmupPerKey < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Pool), "The pool sizes must be positive.");
            }
        }
    }

    // Reglages du streaming de chunks.
    public sealed class StreamingSettings
    {
        public int LoadRadius { get; }
        public int UnloadRadius { get; }
        public int MaxConcurrentLoads { get; }
        public int MaxConcurrentUnloads { get; }

        public StreamingSettings(
            int loadRadius = 2,
            int unloadRadius = 4,
            int maxConcurrentLoads = 4,
            int maxConcurrentUnloads = 4)
        {
            LoadRadius = loadRadius;
            UnloadRadius = unloadRadius;
            MaxConcurrentLoads = maxConcurrentLoads;
            MaxConcurrentUnloads = maxConcurrentUnloads;
        }
    }

    // Reglages de la camera du monde.
    public sealed class CameraSettings
    {
        // Zoom = demi-hauteur visible en unites monde (orthographic size).
        public float ZoomMin { get; }
        public float ZoomMax { get; }
        public float MoveSpeed { get; }
        public float InertiaMaxSpeed { get; }
        public float DecelerationTime { get; }
        public bool HasBounds { get; }
        public WorldPosition MinBound { get; }
        public WorldPosition MaxBound { get; }
        public float RecenterDuration { get; }
        public bool DoubleClickEnabled { get; }
        public float DoubleClickWindowSeconds { get; }
        public float DoubleClickRadiusPixels { get; }
        public float DragThresholdPixels { get; }

        public CameraSettings(
            float zoomMin = 8f,
            float zoomMax = 512f,
            float moveSpeed = 90f,
            float inertiaMaxSpeed = 1600f,
            float decelerationTime = 0.6f,
            bool hasBounds = false,
            WorldPosition? minBound = null,
            WorldPosition? maxBound = null,
            float recenterDuration = 0.45f,
            bool doubleClickEnabled = true,
            float doubleClickWindowSeconds = 0.35f,
            float doubleClickRadiusPixels = 36f,
            float dragThresholdPixels = 8f)
        {
            ZoomMin = zoomMin;
            ZoomMax = zoomMax;
            MoveSpeed = moveSpeed;
            InertiaMaxSpeed = inertiaMaxSpeed;
            DecelerationTime = decelerationTime;
            HasBounds = hasBounds;
            MinBound = minBound ?? default;
            MaxBound = maxBound ?? default;
            RecenterDuration = recenterDuration;
            DoubleClickEnabled = doubleClickEnabled;
            DoubleClickWindowSeconds = doubleClickWindowSeconds;
            DoubleClickRadiusPixels = doubleClickRadiusPixels;
            DragThresholdPixels = dragThresholdPixels;
        }
    }

    // Seuils de niveaux de detail (unites monde).
    public sealed class LodSettings
    {
        public float NearDistance { get; }
        public float MidDistance { get; }
        public float FarDistance { get; }

        public LodSettings(float nearDistance = 128f, float midDistance = 512f, float farDistance = 2048f)
        {
            NearDistance = nearDistance;
            MidDistance = midDistance;
            FarDistance = farDistance;
        }
    }

    // Reglages des pools d'objets.
    public sealed class PoolSettings
    {
        // MaxPerKey = 0 signifie illimite.
        public int MaxPerKey { get; }
        public int WarmupPerKey { get; }

        public PoolSettings(int maxPerKey = 0, int warmupPerKey = 0)
        {
            MaxPerKey = maxPerKey;
            WarmupPerKey = warmupPerKey;
        }
    }
}
