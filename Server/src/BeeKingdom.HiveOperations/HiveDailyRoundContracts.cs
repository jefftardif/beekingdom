namespace BeeKingdom.HiveOperations;
public sealed record HiveDailyRoundSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, DateTimeOffset DayUtc, DateTimeOffset NextResetUtc, DateTimeOffset ServerTimeUtc, long Revision, IReadOnlyDictionary<string,bool> Facts, int CompletedCount, long HoneyReward, long PollenReward, bool ClaimAvailable, DateTimeOffset? ClaimedAtUtc);
public sealed record HiveDailyRoundClaimRequest(long ExpectedRevision, string IdempotencyKey, string ExpectedDayUtc);
public sealed record HiveDailyRoundClaimReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, DateTimeOffset DayUtc, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, long CreditedHoney, long CreditedPollen, string Code);
public sealed record HiveDailyRoundClaimResponse(HiveDailyRoundClaimReceipt Receipt, HiveDailyRoundSnapshot Snapshot);
