using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Alliance.Territories;

/// <summary>
/// Internal candidate contract, not an HTTP request and not a possession command.
/// An API adapter must authenticate and authorize the requested alliance first.
/// MinimumRevision is a caller's last observed snapshot, not a client assertion of ownership.
/// </summary>
public sealed record TerritoryCapacityReadRequest(
    Guid AllianceId, string WorldId, string SurfaceId, long MinimumRevision);

/// <summary>
/// One coherent authoritative read. Revision covers progression, occupied area,
/// and schedule together. CapturedAt is source observation time, not cache access time.
/// Level must be the chosen territorial progression; never infer it from member count
/// or substitute floral mastery. CurrentArea must come from real occupied geometry.
/// Providers must publish a deeply immutable schedule, including its tiers, and
/// honor cancellation/deadlines. IReadOnlyList alone does not freeze a mutable list.
/// </summary>
public sealed record TerritoryCapacitySnapshot(
    Guid AllianceId, string WorldId, string SurfaceId, string AreaUnit,
    long Revision, DateTimeOffset CapturedAt, int? TerritorialLevel,
    decimal CurrentArea, TerritoryCapacitySchedule? Schedule, string? UnavailabilityCode = null);

public interface ITerritoryCapacitySnapshotReader
{
    ValueTask<TerritoryCapacitySnapshot?> ReadAsync(
        TerritoryCapacityReadRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Capacity information only. Available never means a territory exists or may be
/// annexed. This model deliberately has no fabricated boundary or economic effects.
/// Null numeric fields distinguish unavailable data from a real zero area.
/// </summary>
public sealed record TerritoryCapacityView(
    bool Available, string Code, long? Revision, DateTimeOffset? CapturedAt,
    int? TerritorialLevel, string? ScheduleVersion, string? AreaUnit,
    decimal? OccupiedArea, decimal? MaximumArea, decimal? RemainingArea,
    bool? ExistingAreaExceedsCapacity);

public sealed class AllianceTerritoryCapacityReader
{
    private readonly ITerritoryCapacitySnapshotReader source;
    private readonly TimeProvider clock;
    private readonly TimeSpan maximumAge;

    // No default freshness policy or production schedule is silently activated.
    public AllianceTerritoryCapacityReader(
        ITerritoryCapacitySnapshotReader source, TimeProvider clock, TimeSpan maximumAge)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (maximumAge <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumAge));
        this.maximumAge = maximumAge;
    }

    public async ValueTask<TerritoryCapacityView> ReadAsync(
        TerritoryCapacityReadRequest? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.AllianceId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.WorldId)
            || string.IsNullOrWhiteSpace(request.SurfaceId) || request.MinimumRevision < 0)
            return Unavailable("invalid_request");

        // Source failures/cancellation propagate; never turn a failed read into an empty territory.
        var snapshot = await source.ReadAsync(request, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (snapshot is null) return Unavailable("snapshot_unavailable");
        if (snapshot.AllianceId != request.AllianceId
            || !string.Equals(snapshot.WorldId, request.WorldId, StringComparison.Ordinal)
            || !string.Equals(snapshot.SurfaceId, request.SurfaceId, StringComparison.Ordinal))
            return Unavailable("snapshot_scope_mismatch");
        if (snapshot.Revision < 0) return Unavailable("invalid_revision");
        if (snapshot.Revision < request.MinimumRevision) return Unavailable("snapshot_older_than_requested");
        var now = clock.GetUtcNow();
        if (snapshot.CapturedAt > now) return Unavailable("snapshot_from_future");
        if (now - snapshot.CapturedAt > maximumAge) return Unavailable("snapshot_stale");
        if (!string.IsNullOrWhiteSpace(snapshot.UnavailabilityCode)) return Unavailable(snapshot.UnavailabilityCode);

        // Evaluate the existing occupation, never a player-supplied annexation area.
        var capacity = AllianceTerritoryCapacity.Evaluate(snapshot.Schedule,
            new TerritoryCapacityInput(snapshot.AllianceId, snapshot.WorldId,
                snapshot.SurfaceId, snapshot.AreaUnit, snapshot.TerritorialLevel,
                snapshot.Revision, snapshot.CurrentArea, snapshot.CurrentArea));
        if (capacity.Code != "within_capacity" && capacity.Code != "capacity_exceeded")
            return Unavailable(capacity.Code);

        return new(true, capacity.Code, snapshot.Revision, snapshot.CapturedAt,
            snapshot.TerritorialLevel, capacity.ScheduleVersion, snapshot.AreaUnit,
            snapshot.CurrentArea, capacity.MaximumArea, capacity.RemainingArea,
            capacity.ExistingAreaExceedsCapacity);
    }

    private static TerritoryCapacityView Unavailable(string code) =>
        new(false, code, null, null, null, null, null, null, null, null, null);
}
