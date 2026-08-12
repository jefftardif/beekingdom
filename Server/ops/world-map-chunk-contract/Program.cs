using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;
using BeeKingdom.Shared.WorldMap;

namespace BeeKingdom.WorldMapChunkContractVerifier;

internal static class Program
{
    private const string FullWindowFileName = "example-window-5x5.json";
    private const string EdgeWindowFileName = "example-edge-window.json";
    private const string Seed = "bee-kingdom-world-map-readiness-seed";
    private const string ArtisticRevision = "art-revision-readiness-001";

    private static readonly WorldId WorldId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    private static readonly GameServerId GameServerId = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2 || (args[0] != "generate" && args[0] != "verify"))
            {
                throw new ArgumentException("Usage: BeeKingdom.WorldMapChunkContractVerifier <generate|verify> <contract-root>");
            }

            string contractRoot = Path.GetFullPath(args[1]);
            if (!Directory.Exists(contractRoot))
            {
                throw new DirectoryNotFoundException($"Contract root does not exist: {contractRoot}");
            }

            WorldMapChunkWindowResponse fullWindow = CreateFullWindow();
            WorldMapChunkWindowResponse edgeWindow = CreateEdgeWindow();

            if (args[0] == "generate")
            {
                WriteCanonicalExample(contractRoot, FullWindowFileName, fullWindow);
                WriteCanonicalExample(contractRoot, EdgeWindowFileName, edgeWindow);
                Console.WriteLine($"Generated canonical DTO examples in {contractRoot}");
            }

            VerifyExample(contractRoot, FullWindowFileName, fullWindow, expectedChunkCount: 25);
            VerifyExample(contractRoot, EdgeWindowFileName, edgeWindow, expectedChunkCount: 9);

            Console.WriteLine("WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS");
            Console.WriteLine($"FullWindowChunks = {fullWindow.Chunks.Count}");
            Console.WriteLine($"FullWindowManifestHash = {fullWindow.Cache.ManifestHash}");
            Console.WriteLine($"FullWindowOverlayRevision = {fullWindow.Overlays.OverlayRevision}");
            Console.WriteLine($"FullWindowOverlayHash = {fullWindow.Overlays.OverlayHash}");
            Console.WriteLine($"FullWindowETag = {fullWindow.Cache.ETag}");
            Console.WriteLine($"EdgeWindowChunks = {edgeWindow.Chunks.Count}");
            Console.WriteLine($"EdgeWindowManifestHash = {edgeWindow.Cache.ManifestHash}");
            Console.WriteLine($"EdgeWindowOverlayRevision = {edgeWindow.Overlays.OverlayRevision}");
            Console.WriteLine($"EdgeWindowOverlayHash = {edgeWindow.Overlays.OverlayHash}");
            Console.WriteLine($"EdgeWindowETag = {edgeWindow.Cache.ETag}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = FAIL: {exception.Message}");
            return 1;
        }
    }

    private static WorldMapChunkWindowResponse CreateFullWindow()
    {
        return WorldMapChunkReadinessContract.CreateReadinessWindow(
            WorldId,
            GameServerId,
            centerChunkX: 10,
            centerChunkY: -4);
    }

    private static WorldMapChunkWindowResponse CreateEdgeWindow()
    {
        return WorldMapChunkReadinessContract.CreateReadinessWindow(
            WorldId,
            GameServerId,
            centerChunkX: 0,
            centerChunkY: 0,
            worldMinChunkX: 0,
            worldMaxChunkX: 2,
            worldMinChunkY: 0,
            worldMaxChunkY: 2);
    }

    private static void WriteCanonicalExample(string contractRoot, string fileName, WorldMapChunkWindowResponse response)
    {
        string payload = JsonSerializer.Serialize(response, WorldMapChunkJson.CreateOptions(writeIndented: true));
        File.WriteAllText(Path.Combine(contractRoot, fileName), Normalize(payload) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void VerifyExample(
        string contractRoot,
        string fileName,
        WorldMapChunkWindowResponse expected,
        int expectedChunkCount)
    {
        string path = Path.Combine(contractRoot, fileName);
        Require(File.Exists(path), $"Missing JSON example: {fileName}");

        string payload = File.ReadAllText(path, Encoding.UTF8);
        Require(!payload.Contains("placeholder", StringComparison.OrdinalIgnoreCase), $"{fileName} contains a placeholder value.");

        JsonSerializerOptions options = WorldMapChunkJson.CreateOptions(writeIndented: true);
        WorldMapChunkWindowResponse actual = JsonSerializer.Deserialize<WorldMapChunkWindowResponse>(payload, options)
            ?? throw new InvalidOperationException($"{fileName} deserialized to null.");

        string canonicalExpected = JsonSerializer.Serialize(expected, options);
        string canonicalActual = JsonSerializer.Serialize(actual, options);
        Require(Normalize(payload) == Normalize(canonicalExpected), $"{fileName} is not the canonical runtime DTO serialization.");
        Require(Normalize(canonicalActual) == Normalize(canonicalExpected), $"{fileName} failed deterministic typed round-trip.");

        VerifyTopLevel(actual);
        VerifyChunks(actual, expectedChunkCount);
        VerifyCache(actual);
        VerifyOverlays(actual);
        VerifyPagination(actual, expectedChunkCount);
        VerifyGuardrails(actual, expectedChunkCount);
        VerifyNonClaims(actual);
        VerifyPreparatoryFeatures(actual);
    }

    private static void VerifyTopLevel(WorldMapChunkWindowResponse response)
    {
        Require(response.EvidenceId == WorldMapChunkReadinessContract.EvidenceId, "EvidenceId mismatch.");
        Require(response.WorldId == WorldId, "WorldId mismatch.");
        Require(response.GameServerId == GameServerId, "GameServerId mismatch.");
        Require(response.Seed == Seed, "Seed mismatch.");
        Require(response.ArtisticRevision == ArtisticRevision, "Artistic revision mismatch.");
        Require(response.ContractName == WorldMapChunkReadinessContract.ReadinessContractVersion, "Contract name mismatch.");
        Require(response.ContractVersion == ContractVersion.Current, "ContractVersion mismatch.");
        Require(response.ReadOnly, "Response must be read-only.");
        Require(response.NonLive, "Response must be non-live.");
        Require(!response.OfficialEndpoint, "Response must not claim an official endpoint.");
        Require(!response.MutationAllowed, "Response must not allow mutation.");
        Require(response.Errors.Count == 0, "Successful examples must contain an empty errors list.");
    }

    private static void VerifyChunks(WorldMapChunkWindowResponse response, int expectedChunkCount)
    {
        int minX = Math.Max(response.WorldMinChunkX, response.CenterChunkX - response.Radius);
        int maxX = Math.Min(response.WorldMaxChunkX, response.CenterChunkX + response.Radius);
        int minY = Math.Max(response.WorldMinChunkY, response.CenterChunkY - response.Radius);
        int maxY = Math.Min(response.WorldMaxChunkY, response.CenterChunkY + response.Radius);

        List<(int X, int Y)> expectedCoordinates = [];
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                expectedCoordinates.Add((x, y));
            }
        }

        Require(response.Radius == 2, "Wave 1 examples must use radius 2.");
        Require(response.Chunks.Count == expectedChunkCount, $"Expected {expectedChunkCount} chunks.");
        Require(expectedCoordinates.Count == expectedChunkCount, "World bounds do not produce the expected clipped window.");
        Require(response.Chunks.Select(chunk => (chunk.ChunkX, chunk.ChunkY)).Distinct().Count() == expectedChunkCount, "Chunk coordinates must be unique.");

        for (int index = 0; index < response.Chunks.Count; index++)
        {
            WorldMapChunkDescriptor chunk = response.Chunks[index];
            (int expectedX, int expectedY) = expectedCoordinates[index];
            Require(chunk.ChunkX == expectedX && chunk.ChunkY == expectedY, $"Chunk {index} is not ordered Y then X.");
            Require(chunk.Origin == new WorldCoordinate(expectedX * 256L, expectedY * 256L), $"Chunk {index} origin mismatch.");
            Require(chunk.Width == 256 && chunk.Height == 256, $"Chunk {index} dimensions mismatch.");
            Require(chunk.BackgroundLayerKey == $"bg:{response.ArtisticRevision}:{expectedX}:{expectedY}", $"Chunk {index} background key mismatch.");
            Require(chunk.SeamContinuityRequired, $"Chunk {index} must require seam continuity.");
            Require(!chunk.ContainsPaintedOverlays, $"Chunk {index} must not contain painted overlays.");
            Require(chunk.Revision == 1, $"Chunk {index} revision mismatch.");
        }
    }

    private static void VerifyCache(WorldMapChunkWindowResponse response)
    {
        string manifestInput = $"{response.Seed}|{response.ArtisticRevision}|{string.Join(';', response.Chunks.Select(chunk => $"{chunk.ChunkX},{chunk.ChunkY},{chunk.BackgroundLayerKey},{chunk.Revision}"))}";
        string manifestHash = Sha256(manifestInput);
        string overlayWire = JsonSerializer.Serialize(
            response.Overlays with { OverlayHash = "" },
            WorldMapChunkJson.CreateOptions());
        string overlayHash = Sha256(overlayWire);
        string etagInput = $"{response.WorldId}|{response.GameServerId}|{response.CenterChunkX}|{response.CenterChunkY}|{response.Radius}|{manifestHash}|{response.Overlays.OverlayRevision}|{overlayHash}";
        string etag = $"W/\"{Sha256(etagInput)}\"";

        Require(response.Cache.ManifestHash == manifestHash, "Manifest hash mismatch.");
        Require(response.Overlays.OverlayHash == overlayHash, "Overlay wire hash mismatch.");
        Require(response.Cache.ETag == etag, "ETag mismatch.");
        Require(response.Cache.ArtisticRevision == response.ArtisticRevision, "Cache artistic revision mismatch.");
        Require(response.Cache.CacheControl == "private, max-age=60", "Cache-Control mismatch.");
        Require(response.Cache.InvalidationKey == $"world:{response.WorldId}:map:{response.ArtisticRevision}", "Cache invalidation key mismatch.");
        Require(response.Cache.GeneratedAtUtc == DateTimeOffset.UnixEpoch, "GeneratedAtUtc must be deterministic.");
        Require(response.Cache.ExpiresAtUtc == DateTimeOffset.UnixEpoch.AddMinutes(1), "ExpiresAtUtc must be deterministic.");
    }

    private static void VerifyOverlays(WorldMapChunkWindowResponse response)
    {
        Require(response.Overlays.OverlayRevision == "overlay-readiness-001", "Overlay revision mismatch.");
        Require(IsLowerHexSha256(response.Overlays.OverlayHash), "Overlay hash must be a lowercase SHA-256 value.");
        Require(!response.Overlays.PaintedIntoBackground, "Overlays must remain separate from the background.");
        Require(!response.Overlays.ServerAuthoritative && !response.Overlays.Live, "Overlay envelope must remain non-authoritative and non-live.");
        Require(response.Overlays.Hives.Count == 1, "Expected one complete hive overlay entry.");
        Require(response.Overlays.Resources.Count == 1, "Expected one complete resource overlay entry.");
        Require(response.Overlays.Flights.Count == 1, "Expected one complete flight overlay entry.");
        Require(
            response.Overlays.Hives.Select(hive => hive.HiveMarkerId).SequenceEqual(response.Overlays.Hives.Select(hive => hive.HiveMarkerId).OrderBy(id => id, StringComparer.Ordinal)),
            "Hive overlays must use canonical identifier order.");
        Require(
            response.Overlays.Resources.Select(resource => resource.ResourceNodeId).SequenceEqual(response.Overlays.Resources.Select(resource => resource.ResourceNodeId).OrderBy(id => id, StringComparer.Ordinal)),
            "Resource overlays must use canonical identifier order.");
        Require(
            response.Overlays.Flights.Select(flight => flight.FlightId).SequenceEqual(response.Overlays.Flights.Select(flight => flight.FlightId).OrderBy(id => id, StringComparer.Ordinal)),
            "Flight overlays must use canonical identifier order.");

        WorldHiveOverlay hive = response.Overlays.Hives.Single();
        Require(hive.HiveMarkerId == "hive-readiness-001", "Hive marker ID mismatch.");
        Require(hive.Position == WorldMapChunkReadinessContract.StableCoordinate(response.CenterChunkX, response.CenterChunkY, 7, 11), "Hive position mismatch.");
        Require(hive.PowerBand == "preview_band" && !hive.ServerAuthoritative && !hive.Live, "Hive readiness flags mismatch.");

        WorldResourceOverlay resource = response.Overlays.Resources.Single();
        Require(resource.ResourceNodeId == "resource-readiness-001", "Resource node ID mismatch.");
        Require(resource.Position == WorldMapChunkReadinessContract.StableCoordinate(response.CenterChunkX, response.CenterChunkY, 19, 23), "Resource position mismatch.");
        Require(resource.ResourceKind == "nectar" && resource.RichnessBand == "preview_band", "Resource metadata mismatch.");
        Require(!resource.ServerAuthoritative && !resource.Live, "Resource readiness flags mismatch.");

        WorldFlightOverlay flight = response.Overlays.Flights.Single();
        Require(flight.FlightId == "flight-readiness-001", "Flight ID mismatch.");
        Require(flight.Origin == WorldMapChunkReadinessContract.StableCoordinate(response.CenterChunkX, response.CenterChunkY, 3, 5), "Flight origin mismatch.");
        Require(flight.Destination == WorldMapChunkReadinessContract.StableCoordinate(response.CenterChunkX + 1, response.CenterChunkY, 29, 31), "Flight destination mismatch.");
        Require(flight.Kind == WorldFlightKind.Gather && flight.State == WorldFlightState.PreviewOnly, "Flight kind/state mismatch.");
        Require(flight.AirOnly && !flight.RoadGraphUsed, "Flights must be air-only and must not use a road graph.");
        Require(!flight.ServerAuthoritative && !flight.Live, "Flight readiness flags mismatch.");
    }

    private static void VerifyPagination(WorldMapChunkWindowResponse response, int expectedChunkCount)
    {
        Require(response.Pagination.DeterministicOrdering, "Pagination ordering must be deterministic.");
        Require(response.Pagination.PageSize == expectedChunkCount, "Pagination page size mismatch.");
        Require(response.Pagination.NextPageToken is null, "NextPageToken must be explicitly null in Wave 1 examples.");
        Require(response.Pagination.DeltaToken == $"delta:{response.Cache.ETag}", "Delta token mismatch.");
        Require(response.Pagination.SnapshotRevision == 1, "Snapshot revision mismatch.");
        Require(response.Pagination.SinceRevisionApplied is null, "SinceRevisionApplied must be explicitly null in Wave 1 examples.");
    }

    private static void VerifyGuardrails(WorldMapChunkWindowResponse response, int expectedChunkCount)
    {
        int estimatedPayloadBytes = 2048 + (expectedChunkCount * 512) + 256 + 256 + 384;
        string[] errorCodes = Enum.GetNames<WorldMapChunkErrorCode>();

        Require(response.Guardrails.PayloadBudgetBytes == 98_304, "Payload budget mismatch.");
        Require(response.Guardrails.EstimatedPayloadBytes == estimatedPayloadBytes, "Estimated payload size mismatch.");
        Require(response.Guardrails.EstimatedPayloadBytes <= response.Guardrails.PayloadBudgetBytes, "Example exceeds payload budget.");
        Require(response.Guardrails.MaxRadius == 2, "Maximum radius mismatch.");
        Require(response.Guardrails.MaxWindowChunks == 25, "Maximum chunk count mismatch.");
        Require(response.Guardrails.RequiresOverlaysSeparateFromBackground, "Overlay separation guardrail missing.");
        Require(response.Guardrails.RequiresAirOnlyFlights, "Air-only guardrail missing.");
        Require(response.Guardrails.RequiresNoRoadGraph, "No-road-graph guardrail missing.");
        Require(response.Guardrails.ErrorCodes.SequenceEqual(errorCodes), "Error code catalog mismatch.");
    }

    private static void VerifyNonClaims(WorldMapChunkWindowResponse response)
    {
        WorldMapChunkNonClaims nonClaims = response.NonClaims;
        Require(!nonClaims.OfficialEndpointLive, "OfficialEndpointLive must be false.");
        Require(!nonClaims.OfficialPersistenceLive, "OfficialPersistenceLive must be false.");
        Require(!nonClaims.OfficialPlayerData, "OfficialPlayerData must be false.");
        Require(!nonClaims.OfficialProgression, "OfficialProgression must be false.");
        Require(!nonClaims.ServerAuthorityActive, "ServerAuthorityActive must be false.");
        Require(!nonClaims.UnityConnected, "UnityConnected must be false.");
        Require(!nonClaims.SqlBacked, "SqlBacked must be false.");
        Require(!nonClaims.StagingOrProductionTouched, "StagingOrProductionTouched must be false.");
    }

    private static void VerifyPreparatoryFeatures(WorldMapChunkWindowResponse response)
    {
        Require(response.PreparatoryFeatures.IfNoneMatchPassive, "IfNoneMatch must be marked passive/preparatory.");
        Require(response.PreparatoryFeatures.DeltaPageTokenPassive, "DeltaPageToken must be marked passive/preparatory.");
        Require(response.PreparatoryFeatures.ContractVersionNegotiationPassive, "Version negotiation must be marked passive/preparatory.");
        Require(response.PreparatoryFeatures.FutureErrorCodesPassive, "Future error codes must be marked passive/preparatory.");
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static bool IsLowerHexSha256(string value)
    {
        return value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
