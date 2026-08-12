using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeeKingdom.Shared.WorldMap;

public static class WorldMapChunkReadinessContract
{
    public const string EvidenceId = "SERVER-062-WORLD-MAP-CHUNK-CONTRACT-WAVE1";
    public const int DefaultWindowRadius = 2;
    public const int DefaultWindowSize = 5;
    public const int DefaultPayloadBudgetBytes = 96 * 1024;
    public const string ReadinessContractVersion = "world-map-chunk-readiness-v1";

    public static WorldMapChunkWindowResponse CreateReadinessWindow(
        WorldId worldId,
        GameServerId gameServerId,
        int centerChunkX,
        int centerChunkY,
        int worldMinChunkX = -1024,
        int worldMaxChunkX = 1024,
        int worldMinChunkY = -1024,
        int worldMaxChunkY = 1024,
        string seed = "bee-kingdom-world-map-readiness-seed",
        string artisticRevision = "art-revision-readiness-001")
    {
        WorldMapChunkRequest request = new(
            worldId,
            gameServerId,
            centerChunkX,
            centerChunkY,
            DefaultWindowRadius,
            seed,
            artisticRevision,
            IfNoneMatch: null,
            SinceRevision: null,
            DeltaPageToken: null,
            ContractVersion.Current);

        return CreateReadinessWindow(request, worldMinChunkX, worldMaxChunkX, worldMinChunkY, worldMaxChunkY);
    }

    public static WorldMapChunkWindowResponse CreateReadinessWindow(
        WorldMapChunkRequest request,
        int worldMinChunkX = -1024,
        int worldMaxChunkX = 1024,
        int worldMinChunkY = -1024,
        int worldMaxChunkY = 1024)
    {
        if (request.Radius is < 0 or > DefaultWindowRadius)
        {
            return Rejected(request, WorldMapChunkErrorCode.RadiusOutOfRange, "Radius must be between 0 and 2 for Wave 1 readiness.");
        }

        if (worldMinChunkX > worldMaxChunkX || worldMinChunkY > worldMaxChunkY)
        {
            return Rejected(request, WorldMapChunkErrorCode.InvalidWorldBounds, "World bounds are invalid.");
        }

        IReadOnlyList<WorldMapChunkDescriptor> chunks = BuildChunkWindow(request, worldMinChunkX, worldMaxChunkX, worldMinChunkY, worldMaxChunkY);
        string manifestHash = ComputeManifestHash(request.Seed, request.ArtisticRevision, chunks);

        WorldMapChunkCacheMetadata cache = new(
            ETag: "",
            ManifestHash: manifestHash,
            ArtisticRevision: request.ArtisticRevision,
            CacheControl: "private, max-age=60",
            InvalidationKey: $"world:{request.WorldId}:map:{request.ArtisticRevision}",
            GeneratedAtUtc: DateTimeOffset.UnixEpoch,
            ExpiresAtUtc: DateTimeOffset.UnixEpoch.AddMinutes(1));

        WorldMapChunkOverlayEnvelope overlays = new(
            Hives: [new WorldHiveOverlay("hive-readiness-001", StableCoordinate(request.CenterChunkX, request.CenterChunkY, 7, 11), "preview_band", ServerAuthoritative: false, Live: false)],
            Resources: [new WorldResourceOverlay("resource-readiness-001", StableCoordinate(request.CenterChunkX, request.CenterChunkY, 19, 23), "nectar", "preview_band", ServerAuthoritative: false, Live: false)],
            Flights:
            [
                new WorldFlightOverlay(
                    "flight-readiness-001",
                    StableCoordinate(request.CenterChunkX, request.CenterChunkY, 3, 5),
                    StableCoordinate(request.CenterChunkX + 1, request.CenterChunkY, 29, 31),
                    WorldFlightKind.Gather,
                    WorldFlightState.PreviewOnly,
                    AirOnly: true,
                    RoadGraphUsed: false,
                    ServerAuthoritative: false,
                    Live: false)
            ],
            PaintedIntoBackground: false,
            ServerAuthoritative: false,
            Live: false,
            OverlayRevision: "overlay-readiness-001",
            OverlayHash: "");

        WorldMapChunkPagination pagination = new(
            DeterministicOrdering: true,
            PageSize: chunks.Count,
            NextPageToken: null,
            DeltaToken: null,
            SnapshotRevision: 1,
            SinceRevisionApplied: request.SinceRevision);

        WorldMapChunkNonClaims nonClaims = new(
            OfficialEndpointLive: false,
            OfficialPersistenceLive: false,
            OfficialPlayerData: false,
            OfficialProgression: false,
            ServerAuthorityActive: false,
            UnityConnected: false,
            SqlBacked: false,
            StagingOrProductionTouched: false);

        WorldMapChunkGuardrails guardrails = new(
            PayloadBudgetBytes: DefaultPayloadBudgetBytes,
            EstimatedPayloadBytes: EstimatePayloadBytes(chunks.Count, overlays),
            MaxRadius: DefaultWindowRadius,
            MaxWindowChunks: DefaultWindowSize * DefaultWindowSize,
            RequiresOverlaysSeparateFromBackground: true,
            RequiresAirOnlyFlights: true,
            RequiresNoRoadGraph: true,
            ErrorCodes: Enum.GetNames<WorldMapChunkErrorCode>());

        WorldMapChunkWindowResponse response = new(
            EvidenceId,
            request.WorldId,
            request.GameServerId,
            request.CenterChunkX,
            request.CenterChunkY,
            request.Radius,
            worldMinChunkX,
            worldMaxChunkX,
            worldMinChunkY,
            worldMaxChunkY,
            request.Seed,
            request.ArtisticRevision,
            ReadinessContractVersion,
            ReadOnly: true,
            NonLive: true,
            OfficialEndpoint: false,
            MutationAllowed: false,
            Chunks: chunks,
            Cache: cache,
            Overlays: overlays,
            Pagination: pagination,
            Guardrails: guardrails,
            NonClaims: nonClaims,
            Errors: [],
            PreparatoryFeatures: WorldMapChunkPreparatoryFeatures.AllPassive,
            ContractVersion.Current);

        return FinalizeReadinessOverlays(response, overlays);
    }

    public static WorldCoordinate StableCoordinate(int chunkX, int chunkY, int offsetX, int offsetY)
    {
        const int chunkSize = 256;
        return new WorldCoordinate((chunkX * chunkSize) + offsetX, (chunkY * chunkSize) + offsetY);
    }

    public static WorldMapChunkWindowResponse FinalizeReadinessOverlays(
        WorldMapChunkWindowResponse response,
        WorldMapChunkOverlayEnvelope overlays)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(overlays);

        WorldMapChunkOverlayEnvelope canonicalOverlays = CanonicalizeOverlayOrder(overlays) with
        {
            OverlayHash = ""
        };
        canonicalOverlays = canonicalOverlays with
        {
            OverlayHash = ComputeOverlayHash(canonicalOverlays)
        };

        int estimatedPayloadBytes = EstimatePayloadBytes(response.Chunks.Count, canonicalOverlays);
        string etag = ComputeEtag(
            response.WorldId,
            response.GameServerId,
            response.CenterChunkX,
            response.CenterChunkY,
            response.Radius,
            response.Cache.ManifestHash,
            canonicalOverlays.OverlayRevision,
            canonicalOverlays.OverlayHash);
        WorldMapChunkGuardrails guardrails = response.Guardrails with
        {
            EstimatedPayloadBytes = estimatedPayloadBytes
        };
        WorldMapChunkCacheMetadata cache = response.Cache with
        {
            ETag = etag
        };
        WorldMapChunkPagination pagination = response.Pagination with
        {
            DeltaToken = $"delta:{etag}"
        };

        List<WorldMapChunkContractError> errors = response.Errors
            .Where(error => error.Code is not WorldMapChunkErrorCode.OverlayContractViolation
                and not WorldMapChunkErrorCode.PayloadBudgetExceeded)
            .ToList();

        if (ViolatesReadinessOverlayContract(canonicalOverlays))
        {
            errors.Add(new WorldMapChunkContractError(
                WorldMapChunkErrorCode.OverlayContractViolation,
                "Overlays must have a non-empty revision and remain separate, non-live, non-authoritative and air-only without a road graph."));
        }

        if (estimatedPayloadBytes > guardrails.PayloadBudgetBytes)
        {
            errors.Add(new WorldMapChunkContractError(
                WorldMapChunkErrorCode.PayloadBudgetExceeded,
                "Final overlay payload exceeds the readiness budget."));
        }

        return response with
        {
            Cache = cache,
            Overlays = canonicalOverlays,
            Pagination = pagination,
            Guardrails = guardrails,
            Errors = errors
        };
    }

    private static IReadOnlyList<WorldMapChunkDescriptor> BuildChunkWindow(WorldMapChunkRequest request, int minX, int maxX, int minY, int maxY)
    {
        List<WorldMapChunkDescriptor> chunks = [];
        for (int y = request.CenterChunkY - request.Radius; y <= request.CenterChunkY + request.Radius; y++)
        {
            for (int x = request.CenterChunkX - request.Radius; x <= request.CenterChunkX + request.Radius; x++)
            {
                if (x < minX || x > maxX || y < minY || y > maxY)
                {
                    continue;
                }

                WorldCoordinate origin = StableCoordinate(x, y, 0, 0);
                chunks.Add(new WorldMapChunkDescriptor(
                    x,
                    y,
                    origin,
                    Width: 256,
                    Height: 256,
                    BackgroundLayerKey: $"bg:{request.ArtisticRevision}:{x}:{y}",
                    SeamContinuityRequired: true,
                    ContainsPaintedOverlays: false,
                    Revision: 1));
            }
        }

        return chunks;
    }

    private static WorldMapChunkWindowResponse Rejected(WorldMapChunkRequest request, WorldMapChunkErrorCode code, string message)
    {
        return new WorldMapChunkWindowResponse(
            EvidenceId,
            request.WorldId,
            request.GameServerId,
            request.CenterChunkX,
            request.CenterChunkY,
            request.Radius,
            WorldMinChunkX: 0,
            WorldMaxChunkX: 0,
            WorldMinChunkY: 0,
            WorldMaxChunkY: 0,
            request.Seed,
            request.ArtisticRevision,
            ReadinessContractVersion,
            ReadOnly: true,
            NonLive: true,
            OfficialEndpoint: false,
            MutationAllowed: false,
            Chunks: [],
            Cache: new WorldMapChunkCacheMetadata("", "", request.ArtisticRevision, "no-store", "", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch),
            Overlays: WorldMapChunkOverlayEnvelope.Empty,
            Pagination: new WorldMapChunkPagination(true, 0, null, null, 0, request.SinceRevision),
            Guardrails: new WorldMapChunkGuardrails(DefaultPayloadBudgetBytes, 0, DefaultWindowRadius, DefaultWindowSize * DefaultWindowSize, true, true, true, Enum.GetNames<WorldMapChunkErrorCode>()),
            NonClaims: WorldMapChunkNonClaims.AllFalse,
            Errors: [new WorldMapChunkContractError(code, message)],
            PreparatoryFeatures: WorldMapChunkPreparatoryFeatures.AllPassive,
            ContractVersion.Current);
    }

    private static string ComputeManifestHash(string seed, string artisticRevision, IReadOnlyList<WorldMapChunkDescriptor> chunks)
    {
        string input = $"{seed}|{artisticRevision}|{string.Join(';', chunks.Select(chunk => $"{chunk.ChunkX},{chunk.ChunkY},{chunk.BackgroundLayerKey},{chunk.Revision}"))}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    private static string ComputeEtag(
        WorldId worldId,
        GameServerId gameServerId,
        int chunkX,
        int chunkY,
        int radius,
        string manifestHash,
        string overlayRevision,
        string overlayHash)
    {
        string input = $"{worldId}|{gameServerId}|{chunkX}|{chunkY}|{radius}|{manifestHash}|{overlayRevision}|{overlayHash}";
        return $"W/\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()}\"";
    }

    private static string ComputeOverlayHash(WorldMapChunkOverlayEnvelope overlays)
    {
        byte[] wirePayload = JsonSerializer.SerializeToUtf8Bytes(overlays, WorldMapChunkJson.CreateOptions());
        return Convert.ToHexString(SHA256.HashData(wirePayload)).ToLowerInvariant();
    }

    private static WorldMapChunkOverlayEnvelope CanonicalizeOverlayOrder(WorldMapChunkOverlayEnvelope overlays)
    {
        return overlays with
        {
            Hives = overlays.Hives
                .OrderBy(hive => hive.HiveMarkerId, StringComparer.Ordinal)
                .ThenBy(hive => hive.Position.X)
                .ThenBy(hive => hive.Position.Y)
                .ThenBy(hive => hive.PowerBand, StringComparer.Ordinal)
                .ThenBy(hive => hive.ServerAuthoritative)
                .ThenBy(hive => hive.Live)
                .ToArray(),
            Resources = overlays.Resources
                .OrderBy(resource => resource.ResourceNodeId, StringComparer.Ordinal)
                .ThenBy(resource => resource.Position.X)
                .ThenBy(resource => resource.Position.Y)
                .ThenBy(resource => resource.ResourceKind, StringComparer.Ordinal)
                .ThenBy(resource => resource.RichnessBand, StringComparer.Ordinal)
                .ThenBy(resource => resource.ServerAuthoritative)
                .ThenBy(resource => resource.Live)
                .ToArray(),
            Flights = overlays.Flights
                .OrderBy(flight => flight.FlightId, StringComparer.Ordinal)
                .ThenBy(flight => flight.Origin.X)
                .ThenBy(flight => flight.Origin.Y)
                .ThenBy(flight => flight.Destination.X)
                .ThenBy(flight => flight.Destination.Y)
                .ThenBy(flight => flight.Kind)
                .ThenBy(flight => flight.State)
                .ThenBy(flight => flight.AirOnly)
                .ThenBy(flight => flight.RoadGraphUsed)
                .ThenBy(flight => flight.ServerAuthoritative)
                .ThenBy(flight => flight.Live)
                .ToArray()
        };
    }

    private static int EstimatePayloadBytes(int chunkCount, WorldMapChunkOverlayEnvelope overlays)
    {
        return 2048 + (chunkCount * 512) + (overlays.Hives.Count * 256) + (overlays.Resources.Count * 256) + (overlays.Flights.Count * 384);
    }

    private static bool ViolatesReadinessOverlayContract(WorldMapChunkOverlayEnvelope overlays)
    {
        return string.IsNullOrWhiteSpace(overlays.OverlayRevision)
            || overlays.PaintedIntoBackground
            || overlays.ServerAuthoritative
            || overlays.Live
            || overlays.Hives.Any(hive => hive.ServerAuthoritative || hive.Live)
            || overlays.Resources.Any(resource => resource.ServerAuthoritative || resource.Live)
            || overlays.Flights.Any(flight =>
                !flight.AirOnly
                || flight.RoadGraphUsed
                || flight.ServerAuthoritative
                || flight.Live);
    }
}

public sealed record WorldMapChunkRequest(
    WorldId WorldId,
    GameServerId GameServerId,
    int CenterChunkX,
    int CenterChunkY,
    int Radius,
    string Seed,
    string ArtisticRevision,
    string? IfNoneMatch,
    long? SinceRevision,
    string? DeltaPageToken,
    ContractVersion ContractVersion);

public sealed record WorldMapChunkWindowResponse(
    string EvidenceId,
    WorldId WorldId,
    GameServerId GameServerId,
    int CenterChunkX,
    int CenterChunkY,
    int Radius,
    int WorldMinChunkX,
    int WorldMaxChunkX,
    int WorldMinChunkY,
    int WorldMaxChunkY,
    string Seed,
    string ArtisticRevision,
    string ContractName,
    bool ReadOnly,
    bool NonLive,
    bool OfficialEndpoint,
    bool MutationAllowed,
    IReadOnlyList<WorldMapChunkDescriptor> Chunks,
    WorldMapChunkCacheMetadata Cache,
    WorldMapChunkOverlayEnvelope Overlays,
    WorldMapChunkPagination Pagination,
    WorldMapChunkGuardrails Guardrails,
    WorldMapChunkNonClaims NonClaims,
    IReadOnlyList<WorldMapChunkContractError> Errors,
    WorldMapChunkPreparatoryFeatures PreparatoryFeatures,
    ContractVersion ContractVersion);

public sealed record WorldMapChunkDescriptor(
    int ChunkX,
    int ChunkY,
    WorldCoordinate Origin,
    int Width,
    int Height,
    string BackgroundLayerKey,
    bool SeamContinuityRequired,
    bool ContainsPaintedOverlays,
    long Revision);

public readonly record struct WorldCoordinate(long X, long Y);

public sealed record WorldMapChunkCacheMetadata(
    string ETag,
    string ManifestHash,
    string ArtisticRevision,
    string CacheControl,
    string InvalidationKey,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record WorldMapChunkOverlayEnvelope(
    IReadOnlyList<WorldHiveOverlay> Hives,
    IReadOnlyList<WorldResourceOverlay> Resources,
    IReadOnlyList<WorldFlightOverlay> Flights,
    bool PaintedIntoBackground,
    bool ServerAuthoritative,
    bool Live,
    [property: JsonRequired] string OverlayRevision,
    [property: JsonRequired] string OverlayHash)
{
    public static WorldMapChunkOverlayEnvelope Empty { get; } = new(
        [],
        [],
        [],
        false,
        false,
        false,
        "overlay-empty-readiness-001",
        "91dc39da9345848e64335d7ba500ca92a38e415e2f7e469b13b0ea993ec0577e");
}

public sealed record WorldHiveOverlay(
    string HiveMarkerId,
    WorldCoordinate Position,
    string PowerBand,
    bool ServerAuthoritative,
    bool Live);

public sealed record WorldResourceOverlay(
    string ResourceNodeId,
    WorldCoordinate Position,
    string ResourceKind,
    string RichnessBand,
    bool ServerAuthoritative,
    bool Live);

public sealed record WorldFlightOverlay(
    string FlightId,
    WorldCoordinate Origin,
    WorldCoordinate Destination,
    WorldFlightKind Kind,
    WorldFlightState State,
    bool AirOnly,
    bool RoadGraphUsed,
    bool ServerAuthoritative,
    bool Live);

public enum WorldFlightKind
{
    Gather = 0,
    AttackFuture = 1,
    ReinforceFuture = 2,
    TransportFuture = 3,
    Return = 4
}

public enum WorldFlightState
{
    PreviewOnly = 0,
    FutureReserved = 1,
    FutureInFlight = 2,
    FutureReturning = 3,
    FutureCompleted = 4
}

public sealed record WorldMapChunkPagination(
    bool DeterministicOrdering,
    int PageSize,
    string? NextPageToken,
    string? DeltaToken,
    long SnapshotRevision,
    long? SinceRevisionApplied);

public sealed record WorldMapChunkGuardrails(
    int PayloadBudgetBytes,
    int EstimatedPayloadBytes,
    int MaxRadius,
    int MaxWindowChunks,
    bool RequiresOverlaysSeparateFromBackground,
    bool RequiresAirOnlyFlights,
    bool RequiresNoRoadGraph,
    IReadOnlyList<string> ErrorCodes);

public sealed record WorldMapChunkNonClaims(
    bool OfficialEndpointLive,
    bool OfficialPersistenceLive,
    bool OfficialPlayerData,
    bool OfficialProgression,
    bool ServerAuthorityActive,
    bool UnityConnected,
    bool SqlBacked,
    bool StagingOrProductionTouched)
{
    public static WorldMapChunkNonClaims AllFalse { get; } = new(false, false, false, false, false, false, false, false);
}

public sealed record WorldMapChunkContractError(WorldMapChunkErrorCode Code, string Message);

public sealed record WorldMapChunkPreparatoryFeatures(
    bool IfNoneMatchPassive,
    bool DeltaPageTokenPassive,
    bool ContractVersionNegotiationPassive,
    bool FutureErrorCodesPassive)
{
    public static WorldMapChunkPreparatoryFeatures AllPassive { get; } = new(true, true, true, true);
}

public enum WorldMapChunkErrorCode
{
    RadiusOutOfRange = 0,
    InvalidWorldBounds = 1,
    PayloadBudgetExceeded = 2,
    UnknownWorld = 3,
    UnknownChunk = 4,
    ManifestRevisionMismatch = 5,
    DeltaTokenInvalid = 6,
    AuthRequiredFuture = 7,
    OverlayContractViolation = 8
}
