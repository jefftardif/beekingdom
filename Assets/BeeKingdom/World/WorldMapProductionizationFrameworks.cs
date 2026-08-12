using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeeKingdom.World
{
    public enum WorldMapTileReadinessState
    {
        Unloaded,
        Queued,
        Loading,
        ReadyLowRes,
        ReadyHighRes,
        FailedFallback,
        Evicting
    }

    public enum WorldMapServerReadinessStatus
    {
        Open,
        Full,
        Locked,
        Maintenance,
        Preparing
    }

    public enum WorldMapGestureMode
    {
        Idle,
        OneFingerReady,
        OneFingerPan,
        TwoFingerPinchReady,
        TwoFingerPinchZoom,
        Cooldown
    }

    public sealed class WorldMapProductionizationIntake
    {
        public string EvidenceId { get; }
        public string WorldId { get; }
        public string GameServerId { get; }
        public bool ReadOnly { get; }
        public bool NonLive { get; }
        public bool ProductionPublishAllowed { get; }
        public IReadOnlyList<string> Arch172ReservesCovered { get; }
        public IReadOnlyList<string> ForbiddenLiveClaims { get; }

        public WorldMapProductionizationIntake(string evidenceId, string worldId, string gameServerId, bool readOnly, bool nonLive, bool productionPublishAllowed, IReadOnlyList<string> arch172ReservesCovered, IReadOnlyList<string> forbiddenLiveClaims)
        {
            EvidenceId = evidenceId;
            WorldId = worldId;
            GameServerId = gameServerId;
            ReadOnly = readOnly;
            NonLive = nonLive;
            ProductionPublishAllowed = productionPublishAllowed;
            Arch172ReservesCovered = arch172ReservesCovered ?? Array.Empty<string>();
            ForbiddenLiveClaims = forbiddenLiveClaims ?? Array.Empty<string>();
        }

        public bool IsValidNonLiveIntake()
        {
            return ReadOnly
                && NonLive
                && !ProductionPublishAllowed
                && !string.IsNullOrWhiteSpace(EvidenceId)
                && !string.IsNullOrWhiteSpace(WorldId)
                && Arch172ReservesCovered != null
                && Arch172ReservesCovered.Count >= 5
                && ForbiddenLiveClaims != null
                && ForbiddenLiveClaims.Count > 0;
        }
    }

    public readonly struct WorldMapRegionContract
    {
        public string RegionId { get; }
        public string Label { get; }
        public Rect NormalizedBounds { get; }
        public Vector2Int ChunkOrigin { get; }
        public Vector2Int ChunkGrid { get; }
        public string Biome { get; }

        public WorldMapRegionContract(string regionId, string label, Rect normalizedBounds, Vector2Int chunkOrigin, Vector2Int chunkGrid, string biome)
        {
            RegionId = regionId;
            Label = label;
            NormalizedBounds = normalizedBounds;
            ChunkOrigin = chunkOrigin;
            ChunkGrid = chunkGrid;
            Biome = biome;
        }

        public bool Contains(Vector2 normalizedPoint)
        {
            return NormalizedBounds.Contains(normalizedPoint);
        }
    }

    public readonly struct WorldMapTileContract
    {
        public string TileId { get; }
        public int ZoomLevel { get; }
        public int Row { get; }
        public int Column { get; }
        public Rect NormalizedBounds { get; }
        public int Priority { get; }
        public WorldMapTileReadinessState State { get; }

        public WorldMapTileContract(string tileId, int zoomLevel, int row, int column, Rect normalizedBounds, int priority, WorldMapTileReadinessState state)
        {
            TileId = tileId;
            ZoomLevel = zoomLevel;
            Row = row;
            Column = column;
            NormalizedBounds = normalizedBounds;
            Priority = priority;
            State = state;
        }
    }

    public sealed class WorldMapAtlasTileRegionReadiness
    {
        private readonly List<WorldMapRegionContract> regions;
        private readonly List<WorldMapTileContract> tiles;

        public string WorldId { get; }
        public string CoordinateSpace => "world-normalized";
        public int TileSizePx { get; }
        public int MaxResidentTilesMobile { get; }
        public IReadOnlyList<WorldMapRegionContract> Regions => regions;
        public IReadOnlyList<WorldMapTileContract> Tiles => tiles;

        public WorldMapAtlasTileRegionReadiness(string worldId, int tileSizePx, int maxResidentTilesMobile, IEnumerable<WorldMapRegionContract> regions, IEnumerable<WorldMapTileContract> tiles)
        {
            WorldId = string.IsNullOrWhiteSpace(worldId) ? "future-world-preview" : worldId;
            TileSizePx = tileSizePx <= 0 ? 512 : tileSizePx;
            MaxResidentTilesMobile = maxResidentTilesMobile <= 0 ? 24 : maxResidentTilesMobile;
            this.regions = new List<WorldMapRegionContract>(regions ?? Array.Empty<WorldMapRegionContract>());
            this.tiles = new List<WorldMapTileContract>(tiles ?? Array.Empty<WorldMapTileContract>());
        }

        public IReadOnlyList<WorldMapTileContract> ComputeVisibleTiles(Rect viewportNormalized, int zoomLevel, int preloadMargin)
        {
            Rect expanded = Expand(viewportNormalized, Mathf.Max(0, preloadMargin) * 0.08f);
            return tiles
                .Where(tile => tile.ZoomLevel == zoomLevel && expanded.Overlaps(tile.NormalizedBounds))
                .OrderBy(tile => tile.Priority)
                .ThenBy(tile => tile.Row)
                .ThenBy(tile => tile.Column)
                .Take(MaxResidentTilesMobile)
                .ToArray();
        }

        public WorldMapRegionContract RegionAt(Vector2 normalizedPoint)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i].Contains(normalizedPoint)) return regions[i];
            }

            return default;
        }

        private static Rect Expand(Rect rect, float margin)
        {
            return new Rect(rect.xMin - margin, rect.yMin - margin, rect.width + margin * 2f, rect.height + margin * 2f);
        }

        public static WorldMapAtlasTileRegionReadiness CreateDefaultPreview()
        {
            var regions = new[]
            {
                new WorldMapRegionContract("region_00_00", "Highlands NW", new Rect(0f, 0f, 0.25f, 0.3333f), new Vector2Int(0, 0), new Vector2Int(4, 4), "mountain_forest"),
                new WorldMapRegionContract("region_00_01", "Flower Basin", new Rect(0.25f, 0f, 0.25f, 0.3333f), new Vector2Int(4, 0), new Vector2Int(4, 4), "flower_fields"),
                new WorldMapRegionContract("region_00_02", "Frost Peaks", new Rect(0.50f, 0f, 0.25f, 0.3333f), new Vector2Int(8, 0), new Vector2Int(4, 4), "frost_mountain"),
                new WorldMapRegionContract("region_00_03", "Silver River", new Rect(0.75f, 0f, 0.25f, 0.3333f), new Vector2Int(12, 0), new Vector2Int(4, 4), "river"),
                new WorldMapRegionContract("region_01_00", "Amber Woods", new Rect(0f, 0.3333f, 0.25f, 0.3334f), new Vector2Int(0, 4), new Vector2Int(4, 4), "forest"),
                new WorldMapRegionContract("region_01_01", "Goldenheart Core", new Rect(0.25f, 0.3333f, 0.25f, 0.3334f), new Vector2Int(4, 4), new Vector2Int(4, 4), "capital_preview"),
                new WorldMapRegionContract("region_01_02", "Meadow Routes", new Rect(0.50f, 0.3333f, 0.25f, 0.3334f), new Vector2Int(8, 4), new Vector2Int(4, 4), "meadow"),
                new WorldMapRegionContract("region_01_03", "Crimson Border", new Rect(0.75f, 0.3333f, 0.25f, 0.3334f), new Vector2Int(12, 4), new Vector2Int(4, 4), "hostile_preview"),
                new WorldMapRegionContract("region_02_00", "Whispering Marsh", new Rect(0f, 0.6667f, 0.25f, 0.3333f), new Vector2Int(0, 8), new Vector2Int(4, 4), "marsh"),
                new WorldMapRegionContract("region_02_01", "Southern Meadow", new Rect(0.25f, 0.6667f, 0.25f, 0.3333f), new Vector2Int(4, 8), new Vector2Int(4, 4), "meadow"),
                new WorldMapRegionContract("region_02_02", "Thornwatch Border", new Rect(0.50f, 0.6667f, 0.25f, 0.3333f), new Vector2Int(8, 8), new Vector2Int(4, 4), "border"),
                new WorldMapRegionContract("region_02_03", "Crimson Nestlands", new Rect(0.75f, 0.6667f, 0.25f, 0.3333f), new Vector2Int(12, 8), new Vector2Int(4, 4), "hostile")
            };

            var tiles = new List<WorldMapTileContract>();
            for (int zoom = 0; zoom <= 2; zoom++)
            {
                int columns = 2 << zoom;
                int rows = Math.Max(1, columns * 3 / 4);
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        Rect bounds = new Rect(column / (float)columns, row / (float)rows, 1f / columns, 1f / rows);
                        tiles.Add(new WorldMapTileContract($"tile_z{zoom}_r{row:00}_c{column:00}", zoom, row, column, bounds, row + column, zoom == 0 ? WorldMapTileReadinessState.ReadyLowRes : WorldMapTileReadinessState.Queued));
                    }
                }
            }

            return new WorldMapAtlasTileRegionReadiness("future-world-preview", 512, 24, regions, tiles);
        }
    }

    public readonly struct WorldMapGestureTelemetryFrame
    {
        public string TestId { get; }
        public int FrameIndex { get; }
        public int TouchCount { get; }
        public WorldMapGestureMode GestureMode { get; }
        public Vector2 PanDelta { get; }
        public float PinchDelta { get; }
        public float ZoomTarget { get; }
        public float ZoomApplied { get; }
        public float ZoomVelocity { get; }
        public bool SelectionSuppressed { get; }
        public string SuppressionReason { get; }
        public bool HudFixed { get; }
        public bool HitTestUsedInverseTransform { get; }
        public bool HotspotsAligned { get; }
        public bool HalosAligned { get; }

        public WorldMapGestureTelemetryFrame(string testId, int frameIndex, int touchCount, WorldMapGestureMode gestureMode, Vector2 panDelta, float pinchDelta, float zoomTarget, float zoomApplied, float zoomVelocity, bool selectionSuppressed, string suppressionReason, bool hudFixed, bool hitTestUsedInverseTransform, bool hotspotsAligned, bool halosAligned)
        {
            TestId = testId;
            FrameIndex = frameIndex;
            TouchCount = touchCount;
            GestureMode = gestureMode;
            PanDelta = panDelta;
            PinchDelta = pinchDelta;
            ZoomTarget = zoomTarget;
            ZoomApplied = zoomApplied;
            ZoomVelocity = zoomVelocity;
            SelectionSuppressed = selectionSuppressed;
            SuppressionReason = suppressionReason;
            HudFixed = hudFixed;
            HitTestUsedInverseTransform = hitTestUsedInverseTransform;
            HotspotsAligned = hotspotsAligned;
            HalosAligned = halosAligned;
        }
    }

    public static class WorldMapArch166GestureCertification
    {
        public const float ZoomTolerance = 0.001f;
        public const float MaxZoomVelocity = 0.95f;
        public const float MaxPerFrameZoomStep = 0.045f;

        public static bool OneFingerPanDoesNotZoom(IEnumerable<WorldMapGestureTelemetryFrame> frames)
        {
            return frames.Where(frame => frame.TouchCount == 1 && frame.GestureMode == WorldMapGestureMode.OneFingerPan)
                .All(frame => Mathf.Abs(frame.PinchDelta) <= ZoomTolerance && Mathf.Abs(frame.ZoomTarget - frame.ZoomApplied) <= MaxPerFrameZoomStep);
        }

        public static bool TwoFingerPinchOnly(IEnumerable<WorldMapGestureTelemetryFrame> frames)
        {
            return frames.Where(frame => frame.TouchCount == 2)
                .All(frame => frame.GestureMode == WorldMapGestureMode.TwoFingerPinchReady || frame.GestureMode == WorldMapGestureMode.TwoFingerPinchZoom);
        }

        public static bool ZoomVelocityIsClamped(IEnumerable<WorldMapGestureTelemetryFrame> frames)
        {
            return frames.All(frame => Mathf.Abs(frame.ZoomVelocity) <= MaxZoomVelocity + ZoomTolerance);
        }

        public static bool FixedHudAndAlignmentHold(IEnumerable<WorldMapGestureTelemetryFrame> frames)
        {
            return frames.All(frame => frame.HudFixed && frame.HitTestUsedInverseTransform && frame.HotspotsAligned && frame.HalosAligned);
        }
    }

    public readonly struct WorldMapHitTestCase
    {
        public string CaseId { get; }
        public string ZoneId { get; }
        public Vector2 ScreenPoint { get; }
        public Vector2 NormalizedPoint { get; }
        public string Expected { get; }
        public string Actual { get; }
        public bool UsedInverseTransform { get; }

        public WorldMapHitTestCase(string caseId, string zoneId, Vector2 screenPoint, Vector2 normalizedPoint, string expected, string actual, bool usedInverseTransform)
        {
            CaseId = caseId;
            ZoneId = zoneId;
            ScreenPoint = screenPoint;
            NormalizedPoint = normalizedPoint;
            Expected = expected;
            Actual = actual;
            UsedInverseTransform = usedInverseTransform;
        }

        public bool Passed => UsedInverseTransform && string.Equals(Expected, Actual, StringComparison.Ordinal);
    }

    public sealed class WorldMapPostTransformHitTestMatrix
    {
        private readonly List<WorldMapHitTestCase> cases;

        public IReadOnlyList<WorldMapHitTestCase> Cases => cases;
        public bool AllRequiredCasesPresent => cases.Any(c => c.CaseId == "center") && cases.Any(c => c.CaseId == "border") && cases.Any(c => c.CaseId == "outside");
        public bool Passed => AllRequiredCasesPresent && cases.All(c => c.Passed);

        public WorldMapPostTransformHitTestMatrix(IEnumerable<WorldMapHitTestCase> cases)
        {
            this.cases = new List<WorldMapHitTestCase>(cases ?? Array.Empty<WorldMapHitTestCase>());
        }

        public static WorldMapPostTransformHitTestMatrix CreatePreviewMatrix()
        {
            return new WorldMapPostTransformHitTestMatrix(new[]
            {
                new WorldMapHitTestCase("center", "silverstream", new Vector2(650f, 320f), new Vector2(0.601f, 0.404f), "silverstream", "silverstream", true),
                new WorldMapHitTestCase("border", "silverstream", new Vector2(694f, 320f), new Vector2(0.622f, 0.404f), "silverstream", "silverstream", true),
                new WorldMapHitTestCase("outside", "silverstream", new Vector2(770f, 320f), new Vector2(0.680f, 0.404f), "none", "none", true)
            });
        }
    }

    public readonly struct WorldRegistrySelectionOption
    {
        public string WorldId { get; }
        public string GameServerId { get; }
        public WorldMapServerReadinessStatus Status { get; }
        public bool ServerRecommended { get; }
        public bool ServerFull { get; }
        public int MinAccounts { get; }
        public int MaxAccounts { get; }
        public int MinActivePlayers { get; }
        public int MaxActivePlayers { get; }
        public int MinVeryActivePlayers { get; }
        public int MaxVeryActivePlayers { get; }
        public int MaxAlliancePlayers { get; }
        public bool OfficialSelection { get; }

        public WorldRegistrySelectionOption(string worldId, string gameServerId, WorldMapServerReadinessStatus status, bool serverRecommended, bool serverFull, int minAccounts, int maxAccounts, int minActivePlayers, int maxActivePlayers, int minVeryActivePlayers, int maxVeryActivePlayers, int maxAlliancePlayers, bool officialSelection)
        {
            WorldId = worldId;
            GameServerId = gameServerId;
            Status = status;
            ServerRecommended = serverRecommended;
            ServerFull = serverFull;
            MinAccounts = minAccounts;
            MaxAccounts = maxAccounts;
            MinActivePlayers = minActivePlayers;
            MaxActivePlayers = maxActivePlayers;
            MinVeryActivePlayers = minVeryActivePlayers;
            MaxVeryActivePlayers = maxVeryActivePlayers;
            MaxAlliancePlayers = maxAlliancePlayers;
            OfficialSelection = officialSelection;
        }

        public bool IsNonLiveReadinessValid()
        {
            return !OfficialSelection
                && !string.IsNullOrWhiteSpace(WorldId)
                && MinAccounts == 800
                && MaxAccounts == 1500
                && MinActivePlayers == 300
                && MaxActivePlayers == 600
                && MinVeryActivePlayers == 100
                && MaxVeryActivePlayers == 300
                && MaxAlliancePlayers == 100
                && !(ServerRecommended && ServerFull);
        }
    }

    public static class WorldMapProductionizationNoClaimGuard
    {
        private static readonly string[] Forbidden =
        {
            "live",
            "official territory",
            "production published",
            "server selected",
            "attack",
            "war",
            "pvp",
            "ranking",
            "matchmaking",
            "sync"
        };

        public static bool Allows(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            string lower = text.ToLowerInvariant();
            return Forbidden.All(forbidden => !lower.Contains(forbidden));
        }

        public static WorldMapProductionizationIntake CreateDefaultIntake()
        {
            return new WorldMapProductionizationIntake(
                "BEE-781-800-runtime-intake",
                "future-world-preview",
                "not-assigned",
                true,
                true,
                false,
                new[]
                {
                    "PhysicalDeviceValidationStillRequiredLater",
                    "SelectionSuppressionManifestInconsistency",
                    "HitZoneMatrixIncomplete",
                    "AutomatedArch166TestsNotYetImplemented",
                    "WorldMapProductionArtFuture"
                },
                Forbidden);
        }
    }
}
