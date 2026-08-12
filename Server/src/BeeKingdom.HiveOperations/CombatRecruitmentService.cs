using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed record DoctrineRecruitmentDefinition(string Family, int BatchSize, long HoneyCost, long PollenCost, TimeSpan Duration);
public sealed record DoctrineRecruitmentOffer(string Family, int BatchSize, long HoneyCost, long PollenCost, TimeSpan Duration);
public sealed record DoctrineRecruitmentPublicOperation(Guid OperationId, string Family, int BatchSize, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc, string Status);
public sealed record DoctrineRecruitmentReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, Guid OperationId, string Family, int BatchSize, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, string Code);
public sealed record DoctrineRecruitmentSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, string CatalogVersion, long Revision, DateTimeOffset ServerTimeUtc, IReadOnlyList<DoctrineRecruitmentOffer> Offers, IReadOnlyDictionary<string, ResourceBalance> Balances, IReadOnlyDictionary<string, long> Counts, IReadOnlyList<string> LegacyRoles, DoctrineRecruitmentPublicOperation? ActiveOperation);
public sealed record DoctrineRecruitmentResponse(DoctrineRecruitmentReceipt Receipt, DoctrineRecruitmentSnapshot Snapshot);
public sealed record StartDoctrineTrainingCommand(Guid PlayerId, Guid HiveId, string Family, long ExpectedRevision, string IdempotencyKey);
public sealed record ClaimDoctrineTrainingCommand(Guid PlayerId, Guid HiveId, Guid OperationId, long ExpectedRevision, string IdempotencyKey);
public sealed record DoctrineRecruitmentResult(bool Succeeded, string Code, DoctrineRecruitmentSnapshot Snapshot, DoctrineRecruitmentReceipt? Receipt = null);

public sealed class CombatRecruitmentService
{
    private const int MaxReceipts = 128;
    public const string CatalogVersion = "phase4-combat-v1";
    public static readonly IReadOnlyDictionary<string, DoctrineRecruitmentDefinition> Catalog = new Dictionary<string, DoctrineRecruitmentDefinition>(StringComparer.Ordinal)
    {
        ["guardians"] = new("guardians", 4, 680, 180, TimeSpan.FromSeconds(14)),
        ["wingrunners"] = new("wingrunners", 6, 420, 260, TimeSpan.FromSeconds(14)),
        ["darters"] = new("darters", 8, 500, 120, TimeSpan.FromSeconds(14))
    };
    private readonly IHiveStateRepository repository; private readonly IServerClock clock;
    public CombatRecruitmentService(IHiveStateRepository repository, IServerClock clock) { this.repository = repository; this.clock = clock; }
    public async Task<DoctrineRecruitmentSnapshot> ReadAsync(Guid player, Guid hive, CancellationToken ct) => Snapshot(await repository.ReadAsync(player, hive, ct) ?? throw new KeyNotFoundException());
    public async Task<DoctrineRecruitmentResult> StartAsync(StartDoctrineTrainingCommand c, CancellationToken ct)
    {
        if (c.PlayerId == Guid.Empty || c.HiveId == Guid.Empty || c.ExpectedRevision < 0 || c.ExpectedRevision == long.MaxValue || !Catalog.ContainsKey(c.Family) || !ValidKey(c.IdempotencyKey))
            throw new ArgumentException("game.invalid_request");
        DoctrineRecruitmentResult? result = null;
        await repository.ExecuteAtomicallyAsync(c.PlayerId, c.HiveId, state => { var now = clock.UtcNow; var r = state.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new()); var hash = Hash($"start|{c.Family}|{c.ExpectedRevision}");
            if (r.Receipts.TryGetValue(c.IdempotencyKey, out var old)) { var storedOp = old.OperationId ?? Guid.Empty; var family = old.PreviousStep ?? c.Family; var batch = int.TryParse(old.ResultingStep, out var parsedBatch) ? parsedBatch : Catalog.GetValueOrDefault(family)?.BatchSize ?? 0; result = old.PayloadHash == hash ? new(old.Succeeded, old.Code, Snapshot(state), new(c.PlayerId,c.HiveId,c.IdempotencyKey,storedOp,family,batch,old.RevisionBefore ?? r.Revision,old.RevisionAfter ?? r.Revision,old.CreatedAtUtc,old.Code)) : new(false, "game.idempotency_conflict", Snapshot(state)); return state; }
            if (!Catalog.TryGetValue(c.Family, out var d)) { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (r.Revision != c.ExpectedRevision || r.ActiveOperation is not null) { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (state.BuildingLevels.GetValueOrDefault("guard_post") < 1) { result = new(false, "game.recruitment_precondition_failed", Snapshot(state)); return state; }
            if (!state.Resources.TryGetValue("honey", out var honey) || !state.Resources.TryGetValue("pollen", out var pollen) || honey.Amount < d.HoneyCost || pollen.Amount < d.PollenCost) { result = new(false, "game.insufficient_resources", Snapshot(state)); return state; }
            var op = new DoctrineTrainingOperation(Guid.NewGuid(), c.Family, d.BatchSize, now, now.Add(d.Duration), r.Revision + 1, c.IdempotencyKey, hash, false);
            var receipts = new Dictionary<string, IdempotencyReceipt>(r.Receipts) { [c.IdempotencyKey] = new(hash, true, "game.recruitment_started", op.OperationId, now, r.Revision, r.Revision + 1, c.Family, d.BatchSize.ToString(System.Globalization.CultureInfo.InvariantCulture)) };
            var updated = r with { Revision = r.Revision + 1, ActiveOperation = op, Receipts = receipts };
            var resources = new Dictionary<string, ResourceBalance>(state.Resources) { ["honey"] = honey with { Amount = honey.Amount - d.HoneyCost }, ["pollen"] = pollen with { Amount = pollen.Amount - d.PollenCost } };
            var next = state with { Revision = state.Revision + 1, Resources = resources, DoctrineRoster = updated }; result = new(true, "game.recruitment_started", Snapshot(next), new(c.PlayerId,c.HiveId,c.IdempotencyKey,op.OperationId,op.Family,op.BatchSize,r.Revision,updated.Revision,now,"game.recruitment_started")); return next;
        }, ct);
        return result ?? throw new InvalidOperationException("Recruitment mutation did not produce a result");
    }
    public async Task<DoctrineRecruitmentResult> ClaimAsync(ClaimDoctrineTrainingCommand c, CancellationToken ct)
    {
        if (c.PlayerId == Guid.Empty || c.HiveId == Guid.Empty || c.OperationId == Guid.Empty || c.ExpectedRevision < 0 || c.ExpectedRevision == long.MaxValue || !ValidKey(c.IdempotencyKey))
            throw new ArgumentException("game.invalid_request");
        DoctrineRecruitmentResult? result = null;
        await repository.ExecuteAtomicallyAsync(c.PlayerId, c.HiveId, state => { var r = state.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new()); var op = r.ActiveOperation;
            var claimHash = Hash($"claim|{c.OperationId}|{c.ExpectedRevision}");
            if (r.Receipts.TryGetValue(c.IdempotencyKey, out var receipt)) { var storedOp = receipt.OperationId ?? c.OperationId; var family = receipt.PreviousStep ?? "guardians"; var batch = int.TryParse(receipt.ResultingStep, out var parsedBatch) ? parsedBatch : Catalog.GetValueOrDefault(family)?.BatchSize ?? 0; result = receipt.PayloadHash == claimHash && receipt.Succeeded ? new(true, receipt.Code, Snapshot(state), new(c.PlayerId,c.HiveId,c.IdempotencyKey,storedOp,family,batch,receipt.RevisionBefore ?? r.Revision,receipt.RevisionAfter ?? r.Revision,receipt.CreatedAtUtc,receipt.Code)) : new(false, "game.idempotency_conflict", Snapshot(state)); return state; }
            if (op is null || op.OperationId != c.OperationId || r.Revision != c.ExpectedRevision) { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (clock.UtcNow < op.EndsAtUtc) { result = new(false, "game.recruitment_not_complete", Snapshot(state)); return state; }
            var currentCount = r.Counts.GetValueOrDefault(op.Family); if (currentCount > 1_000_000_000L - op.BatchSize) { result = new(false, "game.roster_capacity", Snapshot(state)); return state; }
            var counts = new Dictionary<string, long>(r.Counts) { [op.Family] = currentCount + op.BatchSize }; var accepted = clock.UtcNow; var rosterBefore = r.Revision; var rosterAfter = checked(r.Revision + 1); var receipts = new Dictionary<string, IdempotencyReceipt>(r.Receipts) { [c.IdempotencyKey] = new(claimHash, true, "game.recruitment_claimed", op.OperationId, accepted, rosterBefore, rosterAfter, op.Family, op.BatchSize.ToString(System.Globalization.CultureInfo.InvariantCulture)) }; while (receipts.Count > MaxReceipts) { var victim = receipts.OrderBy(x => x.Value.CreatedAtUtc).ThenBy(x => x.Key, StringComparer.Ordinal).First(x => x.Key != c.IdempotencyKey).Key; receipts.Remove(victim); } var nextRoster = r with { Revision = rosterAfter, Counts = counts, ActiveOperation = null, Receipts = receipts }; var next = state with { Revision = state.Revision + 1, DoctrineRoster = nextRoster }; result = new(true, "game.recruitment_claimed", Snapshot(next), new(c.PlayerId,c.HiveId,c.IdempotencyKey,op.OperationId,op.Family,op.BatchSize,rosterBefore,rosterAfter,accepted,"game.recruitment_claimed")); return next;
        }, ct);
        return result ?? throw new InvalidOperationException("Recruitment claim did not produce a result");
    }
    private DoctrineRecruitmentSnapshot Snapshot(PlayerHiveState s) { var now = clock.UtcNow; var r = s.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new()); var offers = Catalog.Values.Select(d => new DoctrineRecruitmentOffer(d.Family,d.BatchSize,d.HoneyCost,d.PollenCost,d.Duration)).ToArray(); var balances = new Dictionary<string,ResourceBalance>(StringComparer.Ordinal); foreach(var k in new[]{"honey","pollen"}) if(s.Resources.TryGetValue(k,out var b)) balances[k]=b; DoctrineRecruitmentPublicOperation? active = r.ActiveOperation is { } op ? new(op.OperationId,op.Family,op.BatchSize,op.StartedAtUtc,op.EndsAtUtc,now < op.EndsAtUtc ? "running" : "awaiting_completion") : null; return new(s.PlayerId,s.HiveId,"phase4-combat-recruitment-v1",CatalogVersion,r.Revision,now,offers,balances,new Dictionary<string,long>(r.Counts),["Soldats","Gardiennes","Eclaireuses"],active); }
    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Trim() == key && key.Length <= 256 && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
