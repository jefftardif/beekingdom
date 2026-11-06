using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeeKingdom.Alliance.Territories;

// The provider must hold authoritative membership, progression and placement stable
// until Dispose, including exclusion of competing non-territorial placement writers.
// A best-effort read or a client-supplied snapshot does NOT satisfy this contract.
public interface ITerritoryFoundationAuthorityLease : IDisposable
{
    TerritoryFoundationContext Context { get; }
    TerritoryFoundationPolicy Policy { get; }
    bool CompletePlacementCoverage { get; }
    bool IsCurrent { get; }
    long? RequiredConstructionWork { get; }
    IReadOnlyList<IReadOnlyList<TerritoryPoint>> ExcludedFootprints { get; }
}

public sealed record TerritoryConstruction(Guid BuildingId, Guid PlayerId, Guid AllianceId,
    string WorldId, string SurfaceId, string BlueprintKey, string PolicyVersion,
    TerritoryPoint Position, TerritoryPoint[] ReservedRing, long PlacementRevision,
    int TargetLevel, bool Operational, DateTimeOffset CreatedAtUtc, long? RequiredWork = null);

public sealed record TerritoryFoundationReceipt(Guid PlayerId, string RequestId, string PayloadHash,
    TerritoryConstruction Construction);

public sealed record TerritoryFoundationLedger(int Version, string WorldId, string SurfaceId,
    long Revision, TerritoryFoundationReceipt[] Receipts,
    TerritoryWorkCredit[]? WorkCredits = null, TerritoryAuthorityState[]? CompletedTerritories = null);

public sealed record VerifiedTerritoryWork(string Source, string ReceiptId, Guid PlayerId,
    Guid AllianceId, Guid BuildingId, long Units);
public sealed record TerritoryWorkCredit(VerifiedTerritoryWork Proof, long RequiredWork);
public sealed record TerritoryWorkResult(bool Applied, bool Replayed, string Code, long CompletedWork,
    long RequiredWork, TerritoryAuthorityState? Territory);
public sealed record TerritoryCompletedRead(Guid AllianceId, long MembershipRevision, TerritoryAuthorityState? Territory);

public sealed record TerritoryFoundationResult(bool Created, bool Replayed, string Code,
    TerritoryConstruction? Construction);

/// <summary>
/// Local single-host transaction: construction, reserved footprint and idempotency
/// receipt are one atomic document. Verified collective work can complete the
/// territory in this same ledger. No payment or gameplay bonus is applied here.
/// No production authority-lease provider is registered by this candidate.
/// </summary>
public sealed class DurableTerritoryFoundationStore
{
    private readonly string directory;
    private readonly string world;
    private readonly string surface;
    private readonly TimeProvider clock;

    public DurableTerritoryFoundationStore(string directory, string world, string surface, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(world)
            || string.IsNullOrWhiteSpace(surface)) throw new ArgumentException("foundation_scope_required");
        this.directory = Path.GetFullPath(directory);
        this.world = world;
        this.surface = surface;
        this.clock = clock;
    }

    public TerritoryFoundationResult Create(Guid playerId, string requestId, TerritoryPoint position,
        Func<ITerritoryFoundationAuthorityLease?>? acquireAuthority)
    {
        TerritoryFoundationResult No(string code) => new(false, false, code, null);
        if (playerId == Guid.Empty || string.IsNullOrWhiteSpace(requestId) || requestId.Length > 128)
            return No("foundation_request_invalid");
        if (acquireAuthority is null) return No("foundation_authority_unavailable");
        Directory.CreateDirectory(directory);
        string key = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { world, surface })));
        string path = Path.Combine(directory, key + ".foundation.json");
        // Lock order is ledger, then authoritative lease. All participating writers
        // must obey it. Contention/I/O errors propagate; they never mean vacant land.
        using var fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var authority = acquireAuthority();
        if (authority is null) return No("foundation_authority_unavailable");
        if (!authority.IsCurrent) return No("foundation_authority_expired");
        var context = authority.Context;
        if (context.PlayerId != playerId || context.WorldId != world || context.SurfaceId != surface)
            return No("foundation_authority_scope_mismatch");
        if (!context.CurrentMembershipConfirmed || !context.WorldMembershipConfirmed)
            return No("foundation_membership_required");
        if (!authority.CompletePlacementCoverage || authority.ExcludedFootprints is null)
            return No("foundation_placement_coverage_unavailable");
        var ledger = Read(path);
        ValidateLedger(ledger, context.Bounds);
        string payloadHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new { playerId, context.AllianceId, world, surface,
                x = position.X.ToString("G29", CultureInfo.InvariantCulture),
                y = position.Y.ToString("G29", CultureInfo.InvariantCulture) })));
        var prior = ledger.Receipts.SingleOrDefault(r => r.PlayerId == playerId && r.RequestId == requestId);
        // Replay is a read of an existing receipt, after current membership/world
        // checks. A role downgrade or disabled creation policy does not create again.
        if (prior is not null)
            return prior.PayloadHash == payloadHash
                ? new(false, true, "foundation_replayed", prior.Construction)
                : No("foundation_idempotency_conflict");
        if (ledger.Receipts.Any(r => r.Construction.AllianceId == context.AllianceId))
            return No("foundation_already_exists");
        foreach (var excluded in authority.ExcludedFootprints)
            if (!TerritoryGeometry.Validate(excluded, context.Bounds).Valid)
                return No("foundation_placement_data_invalid");
        bool Free(IReadOnlyList<TerritoryPoint> ring, long revision) =>
            revision == context.WorldPlacementRevision
            && !authority.ExcludedFootprints.Any(x => TerritoryFootprintOverlap.IntersectsValidated(ring, x))
            && !ledger.Receipts.Any(r => TerritoryFootprintOverlap.IntersectsValidated(ring, r.Construction.ReservedRing));
        var plan = TerritoryFoundationPlanner.Prepare(authority.Policy, context, position, Free);
        if (!plan.Ready) return No(plan.Code);
        if (!authority.IsCurrent) return No("foundation_authority_expired");
        var construction = new TerritoryConstruction(Guid.NewGuid(), playerId, context.AllianceId,
            world, surface, plan.BuildingBlueprintKey, plan.PolicyVersion, position,
            plan.ProposedRing!.ToArray(), context.WorldPlacementRevision, 1, false, clock.GetUtcNow(), authority.RequiredConstructionWork);
        var receipt = new TerritoryFoundationReceipt(playerId, requestId, payloadHash, construction);
        var next = ledger with { Revision = checked(ledger.Revision + 1), Receipts = ledger.Receipts.Append(receipt).ToArray() };
        if (!Publish(path, next, authority)) return No("foundation_authority_expired");
        return new(true, false, "foundation_construction_reserved", construction);
    }

    // The proof resolver is server infrastructure, not request JSON. It must resolve
    // an immutable, already-verified work receipt; this method does not debit inventory.
    public TerritoryWorkResult CreditVerifiedWork(Guid player, Guid buildingId, string source, string receiptId,
        Func<ITerritoryFoundationAuthorityLease?> acquireAuthority,
        Func<string, string, VerifiedTerritoryWork?>? resolveVerifiedWork)
    {
        TerritoryWorkResult No(string code) => new(false, false, code, 0, 0, null);
        if (player == Guid.Empty || buildingId == Guid.Empty || string.IsNullOrWhiteSpace(source)
            || string.IsNullOrWhiteSpace(receiptId) || source.Length > 128 || receiptId.Length > 128)
            return No("construction_request_invalid");
        if (resolveVerifiedWork is null) return No("construction_work_source_unavailable");
        Directory.CreateDirectory(directory);
        string key = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { world, surface })));
        string path = Path.Combine(directory, key + ".foundation.json");
        using var fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var authority = acquireAuthority();
        if (authority is null || !authority.IsCurrent) return No("construction_authority_unavailable");
        var c = authority.Context;
        if (c.PlayerId != player || !c.CurrentMembershipConfirmed || !c.WorldMembershipConfirmed
            || c.WorldId != world || c.SurfaceId != surface) return No("construction_membership_required");
        if (!authority.CompletePlacementCoverage) return No("construction_placement_coverage_unavailable");
        var ledger = Read(path);
        ValidateLedger(ledger, c.Bounds);
        var construction = ledger.Receipts.Select(r => r.Construction).SingleOrDefault(b => b.BuildingId == buildingId);
        if (construction is null || construction.AllianceId != c.AllianceId) return No("construction_not_owned");
        var proof = resolveVerifiedWork(source, receiptId);
        if (proof is null || proof.Source != source || proof.ReceiptId != receiptId || proof.PlayerId != player
            || proof.AllianceId != c.AllianceId || proof.BuildingId != buildingId || proof.Units <= 0)
            return No("construction_work_proof_invalid");
        var credits = ledger.WorkCredits ?? Array.Empty<TerritoryWorkCredit>();
        var completed = ledger.CompletedTerritories ?? Array.Empty<TerritoryAuthorityState>();
        var prior = credits.SingleOrDefault(w => w.Proof.Source == source && w.Proof.ReceiptId == receiptId);
        var forBuilding = credits.Where(w => w.Proof.BuildingId == buildingId).ToArray();
        long total = forBuilding.Sum(w => w.Proof.Units);
        long required = construction.RequiredWork ?? 0;
        var territory = completed.SingleOrDefault(t => t.Headquarters.BuildingId == buildingId);
        if (prior is not null) return prior.Proof == proof
            ? new(false, true, "construction_work_replayed", total, required, territory)
            : No("construction_work_receipt_conflict");
        if (territory is not null) return No("construction_already_completed");
        if (authority.ExcludedFootprints.Any(r => !TerritoryGeometry.Validate(r, c.Bounds).Valid
            || TerritoryFootprintOverlap.IntersectsValidated(construction.ReservedRing, r)))
            return No("construction_placement_changed");
        if (required <= 0) return No("construction_work_policy_unavailable");
        if (proof.Units > required - total) return No("construction_work_exceeds_remaining");
        long nextTotal = checked(total + proof.Units);
        if (nextTotal == required)
        {
            territory = new(Guid.NewGuid(), c.AllianceId, world, surface, c.AreaUnit, 1, true,
                c.ProgressionSource, c.AllianceProgressionRevision, c.AllianceLevel, c.Bounds,
                construction.ReservedRing.ToArray(), c.AllianceSchedule,
                new(buildingId, 1, 1, construction.Position, true), c.HeadquartersSchedule);
            decimal area = DurableTerritoryAuthorityStore.Validate(territory);
            var capacity = AllianceTerritoryCapacity.Evaluate(c.AllianceSchedule,
                new(c.AllianceId, world, surface, c.AreaUnit, c.AllianceLevel, c.AllianceProgressionRevision, 0, area));
            var hq = HeadquartersCapacity.Evaluate(c.AllianceLevel, capacity.MaximumArea ?? 0, territory.Headquarters, c.HeadquartersSchedule);
            if (!capacity.CanFit || !hq.Available || area > hq.MaximumArea) return No("construction_capacity_unavailable");
            completed = completed.Append(territory).ToArray();
        }
        var next = ledger with { Revision = checked(ledger.Revision + 1),
            WorkCredits = credits.Append(new TerritoryWorkCredit(proof, required)).ToArray(), CompletedTerritories = completed };
        if (!Publish(path, next, authority)) return No("construction_authority_expired");
        return new(true, false, territory is null ? "construction_work_recorded" : "construction_completed", nextTotal, required, territory);
    }

    private static bool Publish(string path, TerritoryFoundationLedger next, ITerritoryFoundationAuthorityLease authority)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, next);
                stream.Flush(flushToDisk: true);
            }
            if (!authority.IsCurrent) return false;
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return true;
    }

    public TerritoryCompletedRead? ReadCompleted(Guid player, Func<ITerritoryFoundationAuthorityLease?> acquireAuthority)
    {
        if (player == Guid.Empty) return null;
        Directory.CreateDirectory(directory);
        string key = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { world, surface })));
        string path = Path.Combine(directory, key + ".foundation.json");
        using var fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var authority = acquireAuthority();
        if (authority is null || !authority.IsCurrent || authority.Context.PlayerId != player
            || !authority.Context.CurrentMembershipConfirmed || !authority.Context.WorldMembershipConfirmed
            || authority.Context.WorldId != world || authority.Context.SurfaceId != surface) return null;
        var ledger = Read(path);
        ValidateLedger(ledger, authority.Context.Bounds);
        return new(authority.Context.AllianceId, authority.Context.WorldPlacementRevision,
            ledger.CompletedTerritories?.SingleOrDefault(t => t.AllianceId == authority.Context.AllianceId));
    }

    private TerritoryFoundationLedger Read(string path)
    {
        try
        {
            using var input = File.OpenRead(path);
            return JsonSerializer.Deserialize<TerritoryFoundationLedger>(input)
                ?? throw new InvalidDataException("foundation_ledger_invalid");
        }
        catch (FileNotFoundException) { return new(1, world, surface, 0, Array.Empty<TerritoryFoundationReceipt>()); }
    }

    private void ValidateLedger(TerritoryFoundationLedger ledger, TerritoryMapBounds bounds)
    {
        if (ledger.Version != 1 || ledger.WorldId != world || ledger.SurfaceId != surface
            || ledger.Receipts is null || ledger.Revision != ledger.Receipts.Length + (ledger.WorkCredits?.Length ?? 0))
            throw new InvalidDataException("foundation_ledger_invalid");
        var requests = new HashSet<(Guid, string)>();
        var buildings = new HashSet<Guid>();
        var alliances = new HashSet<Guid>();
        for (int i = 0; i < ledger.Receipts.Length; i++)
        {
            var receipt = ledger.Receipts[i];
            var c = receipt?.Construction;
            if (receipt is null || c is null || receipt.PlayerId == Guid.Empty || c.PlayerId != receipt.PlayerId
                || string.IsNullOrWhiteSpace(receipt.RequestId) || string.IsNullOrWhiteSpace(receipt.PayloadHash)
                || !requests.Add((receipt.PlayerId, receipt.RequestId)) || c.BuildingId == Guid.Empty
                || !buildings.Add(c.BuildingId) || c.AllianceId == Guid.Empty || !alliances.Add(c.AllianceId)
                || c.WorldId != world || c.SurfaceId != surface || c.Operational || c.TargetLevel != 1
                || c.PlacementRevision < 0 || string.IsNullOrWhiteSpace(c.BlueprintKey) || string.IsNullOrWhiteSpace(c.PolicyVersion)
                || !TerritoryGeometry.Validate(c.ReservedRing, bounds).Valid
                || !TerritoryGeometry.ContainsValidated(c.ReservedRing, c.Position))
                throw new InvalidDataException("foundation_ledger_invalid");
            for (int j = 0; j < i; j++)
                if (TerritoryFootprintOverlap.IntersectsValidated(c.ReservedRing, ledger.Receipts[j].Construction.ReservedRing))
                    throw new InvalidDataException("foundation_ledger_overlap");
        }
        var work = ledger.WorkCredits ?? Array.Empty<TerritoryWorkCredit>();
        var proofIds = new HashSet<(string, string)>();
        foreach (var credit in work)
        {
            if (credit?.Proof is not { } p || p.Units <= 0 || p.PlayerId == Guid.Empty || credit.RequiredWork <= 0
                || string.IsNullOrWhiteSpace(p.Source) || string.IsNullOrWhiteSpace(p.ReceiptId)
                || !proofIds.Add((p.Source, p.ReceiptId))
                || !ledger.Receipts.Any(r => r.Construction.BuildingId == p.BuildingId && r.Construction.AllianceId == p.AllianceId
                    && r.Construction.RequiredWork == credit.RequiredWork))
                throw new InvalidDataException("construction_work_invalid");
        }
        foreach (var group in work.GroupBy(w => w.Proof.BuildingId))
            if (group.Select(w => w.RequiredWork).Distinct().Count() != 1 || group.Sum(w => w.Proof.Units) > group.First().RequiredWork)
                throw new InvalidDataException("construction_work_invalid");
        var completedIds = new HashSet<Guid>();
        foreach (var territory in ledger.CompletedTerritories ?? Array.Empty<TerritoryAuthorityState>())
        {
            if (territory is null) throw new InvalidDataException("construction_completion_invalid");
            DurableTerritoryAuthorityStore.Validate(territory);
            var building = ledger.Receipts.Select(r => r.Construction).SingleOrDefault(b => b.BuildingId == territory.Headquarters.BuildingId);
            var contributions = work.Where(w => w.Proof.BuildingId == territory.Headquarters.BuildingId).ToArray();
            if (building is null || !completedIds.Add(building.BuildingId) || territory.AllianceId != building.AllianceId
                || territory.WorldId != world || territory.SurfaceId != surface || !territory.Active || !territory.Headquarters.Operational
                || territory.Headquarters.Level != 1 || territory.Headquarters.Position != building.Position
                || !territory.Ring.SequenceEqual(building.ReservedRing) || contributions.Length == 0
                || contributions.Sum(w => w.Proof.Units) != contributions[0].RequiredWork)
                throw new InvalidDataException("construction_completion_invalid");
        }
        if (work.GroupBy(w => w.Proof.BuildingId).Any(g => g.Sum(w => w.Proof.Units) == g.First().RequiredWork && !completedIds.Contains(g.Key)))
            throw new InvalidDataException("construction_completion_missing");
    }
}

public static class TerritoryFootprintOverlap
{
    // Validated simple rings only. Touching is conservatively reserved too; this is
    // an explicit candidate placement convention, not a new ownership/combat rule.
    public static bool IntersectsValidated(IReadOnlyList<TerritoryPoint> a, IReadOnlyList<TerritoryPoint> b)
    {
        int na = a.Count - (a[0] == a[a.Count - 1] ? 1 : 0);
        int nb = b.Count - (b[0] == b[b.Count - 1] ? 1 : 0);
        for (int i = 0; i < na; i++)
            for (int j = 0; j < nb; j++)
                if (Crosses(a[i], a[(i + 1) % na], b[j], b[(j + 1) % nb])) return true;
        return TerritoryGeometry.ContainsValidated(a, b[0]) || TerritoryGeometry.ContainsValidated(b, a[0]);
    }

    private static Int128 Cross(TerritoryPoint a, TerritoryPoint b, TerritoryPoint c)
    {
        const decimal scale = 1000000m;
        long ax = (long)(a.X * scale), ay = (long)(a.Y * scale);
        return (Int128)((long)(b.X * scale) - ax) * ((long)(c.Y * scale) - ay)
            - (Int128)((long)(b.Y * scale) - ay) * ((long)(c.X * scale) - ax);
    }

    private static bool On(TerritoryPoint a, TerritoryPoint p, TerritoryPoint b) =>
        p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X)
        && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);

    private static bool Crosses(TerritoryPoint a, TerritoryPoint b, TerritoryPoint c, TerritoryPoint d)
    {
        var x = Cross(a, b, c); var y = Cross(a, b, d); var z = Cross(c, d, a); var w = Cross(c, d, b);
        if ((x == 0 && On(a, c, b)) || (y == 0 && On(a, d, b))
            || (z == 0 && On(c, a, d)) || (w == 0 && On(c, b, d))) return true;
        return ((x < 0 && y > 0) || (x > 0 && y < 0)) && ((z < 0 && w > 0) || (z > 0 && w < 0));
    }
}
