using BeeKingdom.Shared.ValueObjects;
using System.Collections.ObjectModel;
using System.Globalization;

namespace BeeKingdom.Shared.WorldMap;

public interface IWorldMapOverlaySnapshotGovernance
{
    ValueTask<WorldMapOverlayPublicationResult> PublishAsync(
        WorldMapOverlayPublishRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<WorldMapOverlaySnapshotReadResult> ReadLatestAsync(
        WorldMapOverlayScope scope,
        CancellationToken cancellationToken = default);

    ValueTask<WorldMapOverlaySnapshotHistoryResult> ReadHistoryAsync(
        WorldMapOverlayScope scope,
        CancellationToken cancellationToken = default);
}

public sealed class LocalWorldMapOverlaySnapshotProvider :
    IWorldMapOverlaySnapshotGovernance,
    IWorldMapChunkOverlayProvider
{
    public const string OverlayRevisionPrefix = "overlay-snapshot-";

    private const string SemanticComparisonRevision = "overlay-snapshot-semantic-v1";
    private readonly IReadOnlyDictionary<WorldMapOverlayScope, ScopeState> states;
    private readonly int historyCapacity;

    public LocalWorldMapOverlaySnapshotProvider(
        IEnumerable<WorldMapOverlayScope> scopes,
        WorldMapOverlaySnapshotOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        WorldMapOverlaySnapshotOptions effectiveOptions = options ?? WorldMapOverlaySnapshotOptions.Default;
        effectiveOptions.Validate();
        historyCapacity = effectiveOptions.HistoryCapacity;

        Dictionary<WorldMapOverlayScope, ScopeState> mutableStates = [];
        foreach (WorldMapOverlayScope scope in scopes)
        {
            if (scope.WorldId.Value == Guid.Empty || scope.GameServerId.Value == Guid.Empty)
            {
                throw new ArgumentException("Snapshot scopes require non-empty world and game-server identifiers.", nameof(scopes));
            }

            if (!mutableStates.TryAdd(scope, new ScopeState(scope)))
            {
                throw new ArgumentException("Snapshot scopes must be unique.", nameof(scopes));
            }
        }

        states = new ReadOnlyDictionary<WorldMapOverlayScope, ScopeState>(mutableStates);
    }

    public ValueTask<WorldMapOverlayPublicationResult> PublishAsync(
        WorldMapOverlayPublishRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!states.TryGetValue(request.Scope, out ScopeState? state))
        {
            return ValueTask.FromResult(WorldMapOverlayPublicationResult.ScopeNotFound());
        }

        IReadOnlyList<WorldMapOverlaySnapshotContractError> requestErrors = ValidateRequest(request);
        if (requestErrors.Count > 0)
        {
            return ValueTask.FromResult(WorldMapOverlayPublicationResult.RejectedContract(requestErrors));
        }

        CandidatePreparation preparation = PrepareCandidate(state.ValidationResponse, request.Content);
        if (preparation.Errors.Count > 0)
        {
            return ValueTask.FromResult(WorldMapOverlayPublicationResult.RejectedContract(preparation.Errors));
        }

        lock (state.Gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScopeSnapshotState? current = state.Read();

            if (current is not null && string.Equals(current.SemanticHash, preparation.SemanticHash, StringComparison.Ordinal))
            {
                return ValueTask.FromResult(WorldMapOverlayPublicationResult.NoChange(current.Latest));
            }

            if (!MatchesExpectedVersion(request, current?.Latest))
            {
                return ValueTask.FromResult(WorldMapOverlayPublicationResult.RejectedConflict(current?.Latest));
            }

            if (current?.Latest.Revision == long.MaxValue)
            {
                return ValueTask.FromResult(WorldMapOverlayPublicationResult.RejectedContract(
                [
                    new WorldMapOverlaySnapshotContractError(
                        WorldMapOverlaySnapshotContractErrorCode.RevisionExhausted,
                        "The local per-scope overlay revision is exhausted.")
                ]));
            }

            long nextRevision = (current?.Latest.Revision ?? 0) + 1;
            WorldMapChunkOverlayEnvelope revisionedEnvelope = ToEnvelope(
                preparation.Content,
                FormatOverlayRevision(nextRevision));
            WorldMapChunkWindowResponse finalized = WorldMapChunkReadinessContract.FinalizeReadinessOverlays(
                state.ValidationResponse,
                revisionedEnvelope);
            IReadOnlyList<WorldMapOverlaySnapshotContractError> finalErrors = MapFinalizationErrors(finalized.Errors);
            if (finalErrors.Count > 0)
            {
                return ValueTask.FromResult(WorldMapOverlayPublicationResult.RejectedContract(finalErrors));
            }

            WorldMapOverlaySnapshot snapshot = new(
                request.Scope,
                nextRevision,
                Freeze(finalized.Overlays));
            IReadOnlyList<WorldMapOverlaySnapshot> history = BuildHistory(current?.History, snapshot, historyCapacity);
            ScopeSnapshotState next = new(snapshot, preparation.SemanticHash, history);

            cancellationToken.ThrowIfCancellationRequested();
            state.Commit(next);
            return ValueTask.FromResult(WorldMapOverlayPublicationResult.Published(snapshot));
        }
    }

    public ValueTask<WorldMapOverlaySnapshotReadResult> ReadLatestAsync(
        WorldMapOverlayScope scope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!states.TryGetValue(scope, out ScopeState? state))
        {
            return ValueTask.FromResult(WorldMapOverlaySnapshotReadResult.ScopeNotFound());
        }

        ScopeSnapshotState? current = state.Read();
        return ValueTask.FromResult(current is null
            ? WorldMapOverlaySnapshotReadResult.SnapshotNotFound()
            : WorldMapOverlaySnapshotReadResult.Found(current.Latest));
    }

    public ValueTask<WorldMapOverlaySnapshotHistoryResult> ReadHistoryAsync(
        WorldMapOverlayScope scope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!states.TryGetValue(scope, out ScopeState? state))
        {
            return ValueTask.FromResult(WorldMapOverlaySnapshotHistoryResult.ScopeNotFound());
        }

        ScopeSnapshotState? current = state.Read();
        return ValueTask.FromResult(current is null
            ? WorldMapOverlaySnapshotHistoryResult.SnapshotNotFound()
            : WorldMapOverlaySnapshotHistoryResult.Found(current.History));
    }

    public ValueTask<WorldMapChunkOverlayEnvelope> GetOverlaysAsync(
        WorldMapChunkOverlayQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WorldMapOverlayScope scope = new(query.WorldId, query.GameServerId);
        if (!states.TryGetValue(scope, out ScopeState? state))
        {
            return ValueTask.FromResult(WorldMapChunkOverlayEnvelope.Empty);
        }

        ScopeSnapshotState? current = state.Read();
        return ValueTask.FromResult(current?.Latest.Overlays ?? WorldMapChunkOverlayEnvelope.Empty);
    }

    public static string FormatOverlayRevision(long revision)
    {
        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision), "Overlay revisions start at one.");
        }

        return OverlayRevisionPrefix + revision.ToString("D20", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<WorldMapOverlaySnapshotContractError> ValidateRequest(WorldMapOverlayPublishRequest request)
    {
        List<WorldMapOverlaySnapshotContractError> errors = [];
        if (request.Content is null)
        {
            errors.Add(new WorldMapOverlaySnapshotContractError(
                WorldMapOverlaySnapshotContractErrorCode.InvalidCollection,
                "Snapshot content is required."));
        }

        if (request.ExpectedRevision is < 0)
        {
            errors.Add(new WorldMapOverlaySnapshotContractError(
                WorldMapOverlaySnapshotContractErrorCode.InvalidExpectedVersion,
                "Expected revision cannot be negative."));
        }

        if (request.ExpectedOverlayHash is not null && string.IsNullOrWhiteSpace(request.ExpectedOverlayHash))
        {
            errors.Add(new WorldMapOverlaySnapshotContractError(
                WorldMapOverlaySnapshotContractErrorCode.InvalidExpectedVersion,
                "Expected overlay hash cannot be empty when supplied."));
        }

        return Freeze(errors);
    }

    private static CandidatePreparation PrepareCandidate(
        WorldMapChunkWindowResponse validationResponse,
        WorldMapOverlaySnapshotContent content)
    {
        WorldMapOverlaySnapshotContent detached = Detach(content);
        List<WorldMapOverlaySnapshotContractError> errors = ValidateIdentities(detached);
        if (errors.Any(error => error.Code == WorldMapOverlaySnapshotContractErrorCode.InvalidCollection))
        {
            return new CandidatePreparation(detached, SemanticHash: "", Freeze(errors));
        }

        WorldMapChunkOverlayEnvelope candidate = ToEnvelope(detached, SemanticComparisonRevision);
        WorldMapChunkWindowResponse finalized = WorldMapChunkReadinessContract.FinalizeReadinessOverlays(
            validationResponse,
            candidate);
        errors.AddRange(MapFinalizationErrors(finalized.Errors));

        WorldMapChunkOverlayEnvelope frozen = Freeze(finalized.Overlays);
        WorldMapOverlaySnapshotContent canonicalContent = WorldMapOverlaySnapshotContent.FromEnvelope(frozen);
        return new CandidatePreparation(
            canonicalContent,
            frozen.OverlayHash,
            Freeze(errors));
    }

    private static WorldMapOverlaySnapshotContent Detach(WorldMapOverlaySnapshotContent content)
    {
        if (content.Hives is null || content.Resources is null || content.Flights is null)
        {
            return content;
        }

        return new WorldMapOverlaySnapshotContent(
            Freeze(content.Hives),
            Freeze(content.Resources),
            Freeze(content.Flights),
            content.PaintedIntoBackground,
            content.ServerAuthoritative,
            content.Live);
    }

    private static List<WorldMapOverlaySnapshotContractError> ValidateIdentities(WorldMapOverlaySnapshotContent content)
    {
        List<WorldMapOverlaySnapshotContractError> errors = [];
        if (content.Hives is null || content.Resources is null || content.Flights is null)
        {
            errors.Add(new WorldMapOverlaySnapshotContractError(
                WorldMapOverlaySnapshotContractErrorCode.InvalidCollection,
                "Hive, resource and flight collections are required."));
            return errors;
        }

        ValidateIdentifiers(
            content.Hives,
            hive => hive.HiveMarkerId,
            WorldMapOverlaySnapshotContractErrorCode.DuplicateHiveMarkerId,
            "hive marker",
            errors);
        ValidateIdentifiers(
            content.Resources,
            resource => resource.ResourceNodeId,
            WorldMapOverlaySnapshotContractErrorCode.DuplicateResourceNodeId,
            "resource node",
            errors);
        ValidateIdentifiers(
            content.Flights,
            flight => flight.FlightId,
            WorldMapOverlaySnapshotContractErrorCode.DuplicateFlightId,
            "flight",
            errors);
        return errors;
    }

    private static void ValidateIdentifiers<T>(
        IReadOnlyList<T> items,
        Func<T, string> identifier,
        WorldMapOverlaySnapshotContractErrorCode duplicateCode,
        string label,
        ICollection<WorldMapOverlaySnapshotContractError> errors)
        where T : class
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (T item in items)
        {
            if (item is null || string.IsNullOrWhiteSpace(identifier(item)))
            {
                errors.Add(new WorldMapOverlaySnapshotContractError(
                    WorldMapOverlaySnapshotContractErrorCode.InvalidIdentifier,
                    $"Every {label} requires a non-empty identifier."));
                continue;
            }

            string value = identifier(item);
            if (!seen.Add(value))
            {
                errors.Add(new WorldMapOverlaySnapshotContractError(
                    duplicateCode,
                    $"Duplicate {label} identifier '{value}' is not allowed in a complete snapshot."));
            }
        }
    }

    private static bool MatchesExpectedVersion(
        WorldMapOverlayPublishRequest request,
        WorldMapOverlaySnapshot? current)
    {
        if (request.ExpectedRevision is long expectedRevision && expectedRevision != (current?.Revision ?? 0))
        {
            return false;
        }

        return request.ExpectedOverlayHash is null
            || string.Equals(request.ExpectedOverlayHash, current?.OverlayHash, StringComparison.Ordinal);
    }

    private static WorldMapChunkOverlayEnvelope ToEnvelope(
        WorldMapOverlaySnapshotContent content,
        string overlayRevision)
    {
        return new WorldMapChunkOverlayEnvelope(
            content.Hives,
            content.Resources,
            content.Flights,
            content.PaintedIntoBackground,
            content.ServerAuthoritative,
            content.Live,
            overlayRevision,
            OverlayHash: "");
    }

    private static IReadOnlyList<WorldMapOverlaySnapshotContractError> MapFinalizationErrors(
        IReadOnlyList<WorldMapChunkContractError> errors)
    {
        return Freeze(errors.Select(error => new WorldMapOverlaySnapshotContractError(
            error.Code == WorldMapChunkErrorCode.PayloadBudgetExceeded
                ? WorldMapOverlaySnapshotContractErrorCode.PayloadBudgetExceeded
                : WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation,
            error.Message)));
    }

    private static IReadOnlyList<WorldMapOverlaySnapshot> BuildHistory(
        IReadOnlyList<WorldMapOverlaySnapshot>? current,
        WorldMapOverlaySnapshot snapshot,
        int capacity)
    {
        List<WorldMapOverlaySnapshot> next = current?.ToList() ?? [];
        next.Add(snapshot);
        if (next.Count > capacity)
        {
            next.RemoveRange(0, next.Count - capacity);
        }

        return Freeze(next);
    }

    private static WorldMapChunkOverlayEnvelope Freeze(WorldMapChunkOverlayEnvelope overlays)
    {
        return overlays with
        {
            Hives = Freeze(overlays.Hives),
            Resources = Freeze(overlays.Resources),
            Flights = Freeze(overlays.Flights)
        };
    }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> values)
    {
        return Array.AsReadOnly(values.ToArray());
    }

    private sealed class ScopeState
    {
        private ScopeSnapshotState? current;

        public ScopeState(WorldMapOverlayScope scope)
        {
            ValidationResponse = WorldMapChunkReadinessContract.CreateReadinessWindow(
                scope.WorldId,
                scope.GameServerId,
                centerChunkX: 0,
                centerChunkY: 0);
        }

        public object Gate { get; } = new();

        public WorldMapChunkWindowResponse ValidationResponse { get; }

        public ScopeSnapshotState? Read() => Volatile.Read(ref current);

        public void Commit(ScopeSnapshotState next) => Volatile.Write(ref current, next);
    }

    private sealed record ScopeSnapshotState(
        WorldMapOverlaySnapshot Latest,
        string SemanticHash,
        IReadOnlyList<WorldMapOverlaySnapshot> History);

    private sealed record CandidatePreparation(
        WorldMapOverlaySnapshotContent Content,
        string SemanticHash,
        IReadOnlyList<WorldMapOverlaySnapshotContractError> Errors);
}

public readonly record struct WorldMapOverlayScope(WorldId WorldId, GameServerId GameServerId);

public sealed record WorldMapOverlaySnapshotContent(
    IReadOnlyList<WorldHiveOverlay> Hives,
    IReadOnlyList<WorldResourceOverlay> Resources,
    IReadOnlyList<WorldFlightOverlay> Flights,
    bool PaintedIntoBackground,
    bool ServerAuthoritative,
    bool Live)
{
    public static WorldMapOverlaySnapshotContent FromEnvelope(WorldMapChunkOverlayEnvelope overlays)
    {
        ArgumentNullException.ThrowIfNull(overlays);
        return new WorldMapOverlaySnapshotContent(
            Array.AsReadOnly(overlays.Hives.ToArray()),
            Array.AsReadOnly(overlays.Resources.ToArray()),
            Array.AsReadOnly(overlays.Flights.ToArray()),
            overlays.PaintedIntoBackground,
            overlays.ServerAuthoritative,
            overlays.Live);
    }
}

public sealed record WorldMapOverlayPublishRequest(
    WorldMapOverlayScope Scope,
    WorldMapOverlaySnapshotContent Content,
    long? ExpectedRevision = null,
    string? ExpectedOverlayHash = null);

public sealed record WorldMapOverlaySnapshot(
    WorldMapOverlayScope Scope,
    long Revision,
    WorldMapChunkOverlayEnvelope Overlays)
{
    public string OverlayRevision => Overlays.OverlayRevision;

    public string OverlayHash => Overlays.OverlayHash;
}

public sealed record WorldMapOverlayPublicationResult(
    WorldMapOverlayPublicationState State,
    WorldMapOverlaySnapshot? Snapshot,
    IReadOnlyList<WorldMapOverlaySnapshotContractError> Errors)
{
    public static WorldMapOverlayPublicationResult Published(WorldMapOverlaySnapshot snapshot) =>
        new(WorldMapOverlayPublicationState.Published, snapshot, []);

    public static WorldMapOverlayPublicationResult NoChange(WorldMapOverlaySnapshot snapshot) =>
        new(WorldMapOverlayPublicationState.NoChange, snapshot, []);

    public static WorldMapOverlayPublicationResult RejectedConflict(WorldMapOverlaySnapshot? current) =>
        new(WorldMapOverlayPublicationState.RejectedConflict, current, []);

    public static WorldMapOverlayPublicationResult RejectedContract(
        IReadOnlyList<WorldMapOverlaySnapshotContractError> errors) =>
        new(WorldMapOverlayPublicationState.RejectedContract, null, errors);

    public static WorldMapOverlayPublicationResult ScopeNotFound() =>
        new(WorldMapOverlayPublicationState.ScopeNotFound, null, []);
}

public enum WorldMapOverlayPublicationState
{
    Published = 0,
    NoChange = 1,
    RejectedConflict = 2,
    RejectedContract = 3,
    ScopeNotFound = 4
}

public sealed record WorldMapOverlaySnapshotReadResult(
    WorldMapOverlaySnapshotReadState State,
    WorldMapOverlaySnapshot? Snapshot)
{
    public static WorldMapOverlaySnapshotReadResult Found(WorldMapOverlaySnapshot snapshot) =>
        new(WorldMapOverlaySnapshotReadState.Found, snapshot);

    public static WorldMapOverlaySnapshotReadResult SnapshotNotFound() =>
        new(WorldMapOverlaySnapshotReadState.SnapshotNotFound, null);

    public static WorldMapOverlaySnapshotReadResult ScopeNotFound() =>
        new(WorldMapOverlaySnapshotReadState.ScopeNotFound, null);
}

public sealed record WorldMapOverlaySnapshotHistoryResult(
    WorldMapOverlaySnapshotReadState State,
    IReadOnlyList<WorldMapOverlaySnapshot> Snapshots)
{
    public static WorldMapOverlaySnapshotHistoryResult Found(IReadOnlyList<WorldMapOverlaySnapshot> snapshots) =>
        new(WorldMapOverlaySnapshotReadState.Found, snapshots);

    public static WorldMapOverlaySnapshotHistoryResult SnapshotNotFound() =>
        new(WorldMapOverlaySnapshotReadState.SnapshotNotFound, []);

    public static WorldMapOverlaySnapshotHistoryResult ScopeNotFound() =>
        new(WorldMapOverlaySnapshotReadState.ScopeNotFound, []);
}

public enum WorldMapOverlaySnapshotReadState
{
    Found = 0,
    SnapshotNotFound = 1,
    ScopeNotFound = 2
}

public sealed record WorldMapOverlaySnapshotContractError(
    WorldMapOverlaySnapshotContractErrorCode Code,
    string Message);

public enum WorldMapOverlaySnapshotContractErrorCode
{
    OverlayContractViolation = 0,
    PayloadBudgetExceeded = 1,
    DuplicateHiveMarkerId = 2,
    DuplicateResourceNodeId = 3,
    DuplicateFlightId = 4,
    InvalidIdentifier = 5,
    InvalidCollection = 6,
    InvalidExpectedVersion = 7,
    RevisionExhausted = 8
}

public sealed record WorldMapOverlaySnapshotOptions
{
    public const int MinimumHistoryCapacity = 2;
    public const int MaximumHistoryCapacity = 128;

    public static WorldMapOverlaySnapshotOptions Default { get; } = new();

    public WorldMapOverlaySnapshotOptions(int historyCapacity = MinimumHistoryCapacity)
    {
        HistoryCapacity = historyCapacity;
    }

    public int HistoryCapacity { get; }

    internal void Validate()
    {
        if (HistoryCapacity is < MinimumHistoryCapacity or > MaximumHistoryCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(HistoryCapacity),
                $"History capacity must be between {MinimumHistoryCapacity} and {MaximumHistoryCapacity}.");
        }
    }
}
