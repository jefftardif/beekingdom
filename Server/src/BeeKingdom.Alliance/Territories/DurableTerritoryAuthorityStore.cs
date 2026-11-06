using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Alliance.Territories;

/// <summary>
/// Candidate authoritative aggregate: one ring per alliance/world/map. Persisted
/// explicitly by an authorized future command, never inferred from nearby sites.
/// ProgressionSource names the chosen territorial progression, distinct from floral
/// mastery. Revision covers ownership state, geometry, level and schedule together.
/// No foundation/extension/capture permissions are implemented by this storage layer.
/// </summary>
public sealed record TerritoryAuthorityState(
    Guid TerritoryId, Guid AllianceId, string WorldId, string SurfaceId, string AreaUnit,
    long Revision, bool Active, string ProgressionSource, long ProgressionRevision,
    int TerritorialLevel, TerritoryMapBounds Bounds, TerritoryPoint[] Ring,
    TerritoryCapacitySchedule Schedule, TerritoryHeadquarters Headquarters,
    HeadquartersCapacitySchedule HeadquartersSchedule);

/// <summary>
/// Unregistered durable candidate, following existing Alliance atomic JSON storage.
/// A cross-instance exclusive lock protects compare-and-swap on this local filesystem.
/// Not a distributed database. No directory/data is created by construction or read.
/// </summary>
public sealed class DurableTerritoryAuthorityStore : ITerritoryCapacitySnapshotReader
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string directory;
    private readonly TimeProvider clock;
    public DurableTerritoryAuthorityStore(string directory, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("storage_path_required", nameof(directory));
        this.directory = Path.GetFullPath(directory);
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public ValueTask<TerritoryCapacitySnapshot?> ReadAsync(TerritoryCapacityReadRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = ReadState(request);
        if (state is null) return ValueTask.FromResult<TerritoryCapacitySnapshot?>(null);
        decimal area = Validate(state);
        cancellationToken.ThrowIfCancellationRequested();
        // Reading the authoritative committed file observes current state now; no cache timestamp is renewed.
        var allianceCapacity = AllianceTerritoryCapacity.Evaluate(state.Schedule,
            new(state.AllianceId,state.WorldId,state.SurfaceId,state.AreaUnit,state.TerritorialLevel,state.ProgressionRevision,0m,0m));
        var buildingCapacity = HeadquartersCapacity.Evaluate(state.TerritorialLevel, allianceCapacity.MaximumArea!.Value,
            state.Headquarters, state.HeadquartersSchedule);
        if (!buildingCapacity.Available)
            return ValueTask.FromResult<TerritoryCapacitySnapshot?>(new(state.AllianceId,state.WorldId,state.SurfaceId,
                state.AreaUnit,state.Revision,clock.GetUtcNow(),state.TerritorialLevel,state.Active ? area : 0m,
                state.Schedule with {Tiers=Array.AsReadOnly(state.Schedule.Tiers.ToArray())},buildingCapacity.Code));
        // Project one already-resolved joint ceiling; keep the complete two schedules
        // in authority state. No conflation of alliance and building levels.
        var resolvedSchedule = new TerritoryCapacitySchedule(
            state.Schedule.Version + "/" + state.HeadquartersSchedule.Version, state.SurfaceId, state.AreaUnit,
            Array.AsReadOnly(new[]{new TerritoryCapacityTier(1,buildingCapacity.MaximumArea!.Value)}));
        var snapshot = new TerritoryCapacitySnapshot(state.AllianceId, state.WorldId, state.SurfaceId,
            state.AreaUnit, state.Revision, clock.GetUtcNow(), state.TerritorialLevel,
            state.Active ? area : 0m, resolvedSchedule);
        return ValueTask.FromResult<TerritoryCapacitySnapshot?>(snapshot);
    }

    public TerritoryAuthorityState? ReadState(TerritoryCapacityReadRequest request)
    {
        ValidateKey(request.AllianceId, request.WorldId, request.SurfaceId);
        string path = PathFor(request.AllianceId, request.WorldId, request.SurfaceId);
        string json;
        try { json = File.ReadAllText(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        // A corrupt file is an explicit error, never interpreted as vacant land.
        var state = JsonSerializer.Deserialize<TerritoryAuthorityState>(json, Json)
            ?? throw new InvalidDataException("territory_state_missing");
        if (state.AllianceId != request.AllianceId || state.WorldId != request.WorldId || state.SurfaceId != request.SurfaceId)
            throw new InvalidDataException("territory_state_scope_mismatch");
        Validate(state);
        return state; // Deserialization returns detached arrays; callers cannot mutate the stored state.
    }

    // Infrastructure operation only. A future command must authorize membership,
    // check dissolution, neighboring claims, protected/public zones and progression
    // provenance before committing. This is deliberately not registered in DI/HTTP.
    public void Commit(TerritoryAuthorityState state, long? expectedRevision)
    {
        // Freeze caller-owned arrays before validation and persistence.
        var frozen = JsonSerializer.Deserialize<TerritoryAuthorityState>(JsonSerializer.Serialize(state, Json), Json)
            ?? throw new InvalidDataException("territory_state_missing");
        decimal area = Validate(frozen);
        if (expectedRevision < 0 || expectedRevision == long.MaxValue
            || frozen.Revision != (expectedRevision is null ? 1 : expectedRevision.Value + 1))
            throw new InvalidOperationException("revision_conflict");
        Directory.CreateDirectory(directory);
        string path = PathFor(frozen.AllianceId, frozen.WorldId, frozen.SurfaceId);
        using var guard = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var current = ReadState(new(frozen.AllianceId, frozen.WorldId, frozen.SurfaceId, 0));
        if (current?.Revision != expectedRevision) throw new InvalidOperationException("revision_conflict");
        if (current is not null && current.TerritoryId != frozen.TerritoryId)
            throw new InvalidOperationException("territory_identity_mismatch");
        if (current is not null && (current.AreaUnit != frozen.AreaUnit || current.Bounds != frozen.Bounds
            || current.ProgressionSource != frozen.ProgressionSource))
            throw new InvalidOperationException("authority_scope_changed");
        if (current is not null && frozen.ProgressionRevision < current.ProgressionRevision)
            throw new InvalidOperationException("progression_revision_conflict");
        if (current is not null && frozen.TerritorialLevel != current.TerritorialLevel
            && frozen.ProgressionRevision == current.ProgressionRevision)
            throw new InvalidOperationException("progression_revision_conflict");
        if (current is not null && current.Headquarters.BuildingId != frozen.Headquarters.BuildingId)
            throw new InvalidOperationException("headquarters_identity_mismatch");
        if (current is not null && (frozen.Headquarters.Revision < current.Headquarters.Revision
            || (frozen.Headquarters != current.Headquarters && frozen.Headquarters.Revision == current.Headquarters.Revision)))
            throw new InvalidOperationException("headquarters_revision_conflict");
        var capacity = AllianceTerritoryCapacity.Evaluate(frozen.Schedule,
            new(frozen.AllianceId, frozen.WorldId, frozen.SurfaceId, frozen.AreaUnit, frozen.TerritorialLevel,
                frozen.ProgressionRevision, frozen.Active ? area : 0m, frozen.Active ? area : 0m));
        // A downgrade may preserve the existing occupied ring, but it never permits
        // a new over-budget claim, reshaping or reactivation through persistence.
        var buildingCapacity = HeadquartersCapacity.Evaluate(frozen.TerritorialLevel,capacity.MaximumArea!.Value,
            frozen.Headquarters,frozen.HeadquartersSchedule);
        bool overBudget = frozen.Active && (!buildingCapacity.Available || area > buildingCapacity.MaximumArea!.Value);
        if ((!capacity.CanFit || overBudget) && (current is null || !current.Active || !frozen.Active
            || !current.Ring.SequenceEqual(frozen.Ring)))
            throw new InvalidOperationException("capacity_exceeded");
        string temporary = path + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(frozen, Json));
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    // Shared structural validation for internal eligibility callers. Performs no I/O.
    internal static decimal Validate(TerritoryAuthorityState state)
    {
        ValidateKey(state.AllianceId, state.WorldId, state.SurfaceId);
        if (state.TerritoryId == Guid.Empty || state.Revision < 1 || state.ProgressionRevision < 0
            || string.IsNullOrWhiteSpace(state.ProgressionSource))
            throw new InvalidDataException("invalid_authority_identity");
        var geometry = TerritoryGeometry.Validate(state.Ring, state.Bounds);
        if (!geometry.Valid) throw new InvalidDataException(geometry.Code);
        if (state.Headquarters is null || state.Headquarters.BuildingId == Guid.Empty
            || state.Headquarters.Level < 1 || state.Headquarters.Revision < 0)
            throw new InvalidDataException("headquarters_missing_or_invalid");
        if (!TerritoryGeometry.ContainsValidated(state.Ring,state.Headquarters.Position))
            throw new InvalidDataException("headquarters_outside_territory");
        var capacity = AllianceTerritoryCapacity.Evaluate(state.Schedule,
            new(state.AllianceId, state.WorldId, state.SurfaceId, state.AreaUnit, state.TerritorialLevel,
                state.ProgressionRevision, state.Active ? geometry.Area!.Value : 0m, state.Active ? geometry.Area!.Value : 0m));
        // Existing occupation may outlive a capacity reduction; preserve it without granting growth.
        if (capacity.Code != "within_capacity" && capacity.Code != "capacity_exceeded")
            throw new InvalidDataException(capacity.Code);
        var headquarters = HeadquartersCapacity.Evaluate(state.TerritorialLevel,capacity.MaximumArea!.Value,
            state.Headquarters with {Operational=true},state.HeadquartersSchedule);
        if (!headquarters.Available && headquarters.Code != "headquarters_alliance_unlock_unmet")
            throw new InvalidDataException(headquarters.Code);
        return geometry.Area!.Value;
    }
    private static void ValidateKey(Guid alliance, string world, string surface)
    {
        if (alliance == Guid.Empty || string.IsNullOrWhiteSpace(world) || string.IsNullOrWhiteSpace(surface))
            throw new ArgumentException("invalid_territory_key");
    }
    private string PathFor(Guid alliance, string world, string surface)
    {
        // Hash encoded scope, never interpolate map/world strings into a filesystem path.
        var scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { world, surface }))));
        return Path.Combine(directory, alliance.ToString("N") + "-" + scope + ".json");
    }
}
