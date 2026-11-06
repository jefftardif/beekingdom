using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace BeeKingdom.Alliance.Territories;

public sealed record TerritoryWorldMember(Guid PlayerId, Guid AllianceId, TerritoryFoundationRole Role);
public sealed record TerritoryAllianceProgression(Guid AllianceId, int Level, long Revision, string Source);
public sealed record TerritoryRegistryRead(Guid AllianceId, string WorldId, string SurfaceId,
    long MembershipRevision, TerritoryMapView View);

// Explicit server-owned candidate configuration. Never accepted from a game request.
// Membership here is authoritative ONLY in this isolated registry mode; it is not a
// claim that legacy AllianceRepository mutations are synchronized with this document.
public sealed record TerritoryWorldConfiguration(string WorldId, string SurfaceId, string AreaUnit,
    long Revision, TerritoryMapBounds Bounds, bool CompletePlacementCoverage,
    TerritoryWorldMember[] Members, TerritoryAllianceProgression[] Progressions,
    TerritoryPoint[][] Exclusions, TerritoryFoundationPolicy FoundationPolicy,
    TerritoryCapacitySchedule AllianceSchedule, HeadquartersCapacitySchedule HeadquartersSchedule,
    long? RequiredConstructionWork = null);

/// <summary>
/// Candidate local registry. Cross-process file locking keeps configured world access,
/// membership, progression and complete placement coverage stable during foundation.
/// No default world, automatic membership, floral-level substitution or production DI.
/// </summary>
public sealed class DurableTerritoryWorldRegistry
{
    private readonly string root;
    private readonly string path;
    private readonly string world;
    private readonly string surface;
    private readonly TimeProvider clock;

    public DurableTerritoryWorldRegistry(string root, string world, string surface, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(world) || string.IsNullOrWhiteSpace(surface))
            throw new ArgumentException("registry_scope_required");
        this.root = Path.GetFullPath(root);
        this.world = world;
        this.surface = surface;
        this.clock = clock;
        string key = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { world, surface })));
        path = Path.Combine(this.root, key + ".registry.json");
    }

    // Infrastructure-only setup/CAS. Deliberately no HTTP route. Importers must own
    // the complete declared world/surface; partial discoveries are not full coverage.
    public void PublishConfiguration(TerritoryWorldConfiguration configuration, long? expectedRevision)
    {
        var next = JsonSerializer.Deserialize<TerritoryWorldConfiguration>(JsonSerializer.Serialize(configuration))
            ?? throw new ArgumentException("registry_configuration_missing");
        Validate(next);
        Directory.CreateDirectory(root);
        using var guard = Lock();
        var prior = Read();
        if (prior?.Revision != expectedRevision || next.Revision != checked((expectedRevision ?? 0) + 1))
            throw new InvalidOperationException("registry_revision_conflict");
        if (prior is not null)
        {
            if (prior.Progressions.Any(p => !next.Progressions.Any(n => n.AllianceId == p.AllianceId)))
                throw new InvalidOperationException("registry_progression_removal_requires_migration");
            if (prior.Bounds != next.Bounds || prior.AreaUnit != next.AreaUnit)
                throw new InvalidOperationException("registry_coordinate_migration_required");
            foreach (var progress in next.Progressions)
            {
                var old = prior.Progressions.SingleOrDefault(p => p.AllianceId == progress.AllianceId);
                if (old is not null && (progress.Source != old.Source || progress.Revision < old.Revision
                    || (progress.Level != old.Level && progress.Revision <= old.Revision)))
                    throw new InvalidOperationException("registry_progression_revision_conflict");
            }
        }
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(output, next); output.Flush(true); }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    // The caller must obtain playerId from the validated session, never the body.
    public TerritoryFoundationResult Found(Guid authenticatedPlayerId, string requestId, TerritoryPoint position)
    {
        var store = new DurableTerritoryFoundationStore(root, world, surface, clock);
        return store.Create(authenticatedPlayerId, requestId, position, () => Acquire(authenticatedPlayerId));
    }

    public TerritoryWorkResult CreditVerifiedWork(Guid authenticatedPlayerId, Guid buildingId,
        string source, string receiptId, Func<string, string, VerifiedTerritoryWork?>? verifiedServerReceipts)
    {
        var store = new DurableTerritoryFoundationStore(root, world, surface, clock);
        return store.CreditVerifiedWork(authenticatedPlayerId, buildingId, source, receiptId,
            () => Acquire(authenticatedPlayerId), verifiedServerReceipts);
    }

    public TerritoryMapView ReadMine(Guid authenticatedPlayerId, string coordinateUnit, long minimumRevision = 0)
    {
        return ReadForSession(authenticatedPlayerId, coordinateUnit, minimumRevision)?.View
            ?? new(false, "registry_membership_unavailable", null, null);
    }

    public TerritoryRegistryRead? ReadForSession(Guid authenticatedPlayerId, string coordinateUnit,
        long minimumRevision = 0, Guid? minimumAllianceId = null)
    {
        var store = new DurableTerritoryFoundationStore(root, world, surface, clock);
        var read = store.ReadCompleted(authenticatedPlayerId, () => Acquire(authenticatedPlayerId));
        if (read is null) return null;
        long floor = minimumAllianceId is null || minimumAllianceId == read.AllianceId ? minimumRevision : 0;
        return new(read.AllianceId, world, surface, read.MembershipRevision,
            TerritoryMapReader.Project(read.Territory, floor, coordinateUnit));
    }

    private ITerritoryFoundationAuthorityLease? Acquire(Guid player)
    {
        // Order: foundation ledger lock -> registry lock. Configuration publication
        // never takes the ledger lock. Existing independent writers are not covered.
        var guard = Lock();
        try
        {
            var configuration = Read();
            if (configuration is null) { guard.Dispose(); return null; }
            var member = configuration.Members.SingleOrDefault(m => m.PlayerId == player);
            if (member is null) { guard.Dispose(); return null; }
            var progression = configuration.Progressions.SingleOrDefault(p => p.AllianceId == member.AllianceId);
            if (progression is null) { guard.Dispose(); return null; }
            return new RegistryLease(guard, configuration, member, progression);
        }
        catch { guard.Dispose(); throw; }
    }

    private FileStream Lock() => new(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

    private TerritoryWorldConfiguration? Read()
    {
        try
        {
            using var input = File.OpenRead(path);
            var configuration = JsonSerializer.Deserialize<TerritoryWorldConfiguration>(input)
                ?? throw new InvalidDataException("registry_configuration_invalid");
            Validate(configuration);
            return configuration;
        }
        catch (FileNotFoundException) { return null; }
    }

    private void Validate(TerritoryWorldConfiguration c)
    {
        if (c.WorldId != world || c.SurfaceId != surface || string.IsNullOrWhiteSpace(c.AreaUnit)
            || c.Revision < 1 || c.Members is null || c.Progressions is null || c.Exclusions is null
            || c.FoundationPolicy is null || c.AllianceSchedule is null || c.HeadquartersSchedule is null)
            throw new InvalidDataException("registry_configuration_invalid");
        var boundsRing = new[] { new TerritoryPoint(c.Bounds.MinX, c.Bounds.MinY), new TerritoryPoint(c.Bounds.MaxX, c.Bounds.MinY),
            new TerritoryPoint(c.Bounds.MaxX, c.Bounds.MaxY), new TerritoryPoint(c.Bounds.MinX, c.Bounds.MaxY) };
        if (!TerritoryGeometry.Validate(boundsRing, c.Bounds).Valid
            || c.Exclusions.Any(r => !TerritoryGeometry.Validate(r, c.Bounds).Valid))
            throw new InvalidDataException("registry_geometry_invalid");
        var players = new HashSet<Guid>(); var alliances = new HashSet<Guid>();
        foreach (var m in c.Members)
            if (m is null || m.PlayerId == Guid.Empty || m.AllianceId == Guid.Empty || !players.Add(m.PlayerId)
                || !Enum.IsDefined(m.Role)) throw new InvalidDataException("registry_members_invalid");
        foreach (var p in c.Progressions)
        {
            if (p is null || p.AllianceId == Guid.Empty || p.Level < 1 || p.Revision < 0
                || string.IsNullOrWhiteSpace(p.Source) || !alliances.Add(p.AllianceId))
                throw new InvalidDataException("registry_progression_invalid");
            var capacity = AllianceTerritoryCapacity.Evaluate(c.AllianceSchedule,
                new(p.AllianceId, world, surface, c.AreaUnit, p.Level, p.Revision, 0, 0));
            if (!capacity.CanFit) throw new InvalidDataException("registry_capacity_invalid");
        }
    }

    private sealed class RegistryLease : ITerritoryFoundationAuthorityLease
    {
        private FileStream? guard;
        public TerritoryFoundationContext Context { get; }
        public TerritoryFoundationPolicy Policy { get; }
        public bool CompletePlacementCoverage { get; }
        public bool IsCurrent => guard is not null;
        public long? RequiredConstructionWork { get; }
        public IReadOnlyList<IReadOnlyList<TerritoryPoint>> ExcludedFootprints { get; }

        public RegistryLease(FileStream guard, TerritoryWorldConfiguration c, TerritoryWorldMember member,
            TerritoryAllianceProgression progression)
        {
            this.guard = guard;
            Context = new(member.PlayerId, member.AllianceId, member.Role, true, true, c.WorldId, c.SurfaceId,
                c.AreaUnit, progression.Source, progression.Level, progression.Revision, c.Revision, c.Bounds,
                c.AllianceSchedule, c.HeadquartersSchedule);
            Policy = c.FoundationPolicy;
            RequiredConstructionWork = c.RequiredConstructionWork;
            CompletePlacementCoverage = c.CompletePlacementCoverage;
            ExcludedFootprints = c.Exclusions;
        }

        public void Dispose() { guard?.Dispose(); guard = null; }
    }
}
