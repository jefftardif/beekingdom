using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// PROVISIONAL Alpha values, never enabled by default. Monetary and ecological
/// changes are legal only through the SQL cross-aggregate transaction.
/// </summary>
public sealed class CivilExpeditionOptions
{
    public const string SectionName = "CivilExpeditions";
    public bool Enabled { get; set; }
    /// <summary>
    /// Emergency operations mode: reject ALL new departures, but continue
    /// to settle already persisted due flights so civilian reservations and
    /// world resources cannot remain stranded during a feature shutdown.
    /// Explicitly OFF by default. Keep SQL schema and floral catalogue intact.
    /// </summary>
    public bool DrainExistingFlights { get; set; }

    public bool CanSettle(bool floralEnabled) =>
        DrainExistingFlights || (Enabled && floralEnabled);

    public bool CanStart(bool floralEnabled) =>
        Enabled && floralEnabled && !DrainExistingFlights;

    public int DurationMinutes { get; set; } = 15;
    public long MaxWorkers { get; set; } = 200;
    public long PollenPerWorker { get; set; } = 1;
    public int FertilityGainBp { get; set; } = 5;
    public int BiodiversityGainBp { get; set; } = 2;

    public void Validate()
    {
        if (DurationMinutes is < 3 or > 10080
            || MaxWorkers is < 1 or > 1000000
            || PollenPerWorker is < 1 or > 1000000
            || FertilityGainBp is < 0 or > 1000
            || BiodiversityGainBp is < 0 or > 1000)
            throw new InvalidDataException("Invalid civil expedition Alpha options.");
        _ = checked(MaxWorkers * PollenPerWorker);
    }
}

public sealed record StartCivilExpeditionRequest(
    string SiteId, long Workers, long ExpectedHiveRevision,
    long ExpectedSiteRevision, string IdempotencyKey,
    string Mode = "balanced");

public sealed record CivilExpeditionMission(
    Guid MissionId, string WorldId, string RegionId, string SiteId,
    long Workers, string Mode, DateTimeOffset StartedAtUtc,
    DateTimeOffset ArrivesAtUtc, DateTimeOffset WorkEndsAtUtc,
    DateTimeOffset ReturnsAtUtc, long PollenPerWorker,
    int FertilityGainBp, int BiodiversityGainBp,
    Guid? AllianceIdAtStart = null);

public sealed record CivilExpeditionCompletion(
    Guid MissionId, string SiteId, long PollenCredited,
    long PollinationPoints, DateTimeOffset CompletedAtUtc,
    long SiteRevisionAfter, long PollenBalanceAfter);

public sealed record CivilExpeditionReceipt(
    string Action, string IdempotencyKey, string PayloadHash, Guid MissionId,
    long HiveRevisionBefore, long HiveRevisionAfter,
    long SiteRevisionBefore, long SiteRevisionAfter,
    DateTimeOffset AcceptedAtUtc, string Code, long CreditedPollen = 0);

public sealed record CivilExpeditionState(
    long Revision, CivilExpeditionMission? Active,
    CivilExpeditionCompletion? LastCompletion,
    Dictionary<string, CivilExpeditionReceipt> Receipts);

public sealed record CivilExpeditionSnapshot(
    string ContractVersion, Guid PlayerId, Guid HiveId,
    long HiveRevision, DateTimeOffset ServerTimeUtc,
    CivilExpeditionMission? Active, string Status,
    CivilExpeditionCompletion? LastCompletion,
    long AvailableWorkers, long AssignedWorkers);

public sealed record CivilExpeditionResult(
    bool Succeeded, string Code, CivilExpeditionSnapshot Snapshot,
    CivilExpeditionReceipt? Receipt = null);

public sealed record CivilExpeditionContribution(
    Guid MissionId, Guid PlayerId, Guid HiveId,
    string WorldId, string SiteId, long PollenRemoved,
    long PollenCredited, long PollinationPoints, DateTimeOffset AtUtc);

public sealed record CivilExpeditionMutation(
    PlayerHiveState Hive, FloralSiteState Site,
    CivilExpeditionContribution? Contribution = null);

public interface ICivilExpeditionUnitOfWork
{
    // Must be one durable ACID transaction for the hive, floral site, and
    // contribution ledger. JSON repository is explicitly unsupported here.
    Task<CivilExpeditionMutation> ExecuteAsync(
        Guid playerId, Guid hiveId, FloralSiteDefinition site,
        string catalogVersion,
        Func<PlayerHiveState, FloralSiteState, CivilExpeditionMutation> change,
        CancellationToken ct = default);
}

public sealed record CivilDueExpedition(
    Guid MissionId, Guid PlayerId, Guid HiveId, DateTimeOffset ReturnsAtUtc);

public interface ICivilExpeditionDueIndex
{
    Task<IReadOnlyList<CivilDueExpedition>> DueAsync(
        DateTimeOffset nowUtc, int limit, CancellationToken ct = default);
}
public static class CivilExpeditionRules
{
    public const string ContractVersion = "bk-civil-expedition-v1-alpha";
    public const int MaxReceipts = 10000;

    public static CivilExpeditionState Empty() =>
        new(0, null, null, new(StringComparer.Ordinal));

    public static string Phase(CivilExpeditionMission mission, DateTimeOffset now) =>
        now >= mission.ReturnsAtUtc ? "awaiting_settlement"
            : now >= mission.WorkEndsAtUtc ? "returning"
            : now >= mission.ArrivesAtUtc ? "pollinating" : "outbound";

    public static CivilExpeditionSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        CivilExpeditionState exp = state.CivilExpeditions ?? Empty();
        CivilPopulationState? civil = state.CivilPopulation;
        return new CivilExpeditionSnapshot(
            ContractVersion, state.PlayerId, state.HiveId, state.Revision, now,
            exp.Active, exp.Active is null ? "idle" : Phase(exp.Active, now),
            exp.LastCompletion, civil?.AvailableWorkers ?? 0,
            civil?.AssignedWorkers ?? 0);
    }

    public static bool ValidKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) && key.Length is >= 8 and <= 128
        && key.All(c => c is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z' or >= '0' and <= '9'
            or '-' or '_' or '.' or ':');

    public static string Hash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static void ValidateStored(CivilExpeditionState exp, CivilPopulationState? civil)
    {
        if (exp.Revision < 0 || exp.Receipts is null
            || exp.Receipts.Count > MaxReceipts
            || exp.Receipts.Any(item => !ValidKey(item.Key)
                || item.Value is null
                || string.IsNullOrWhiteSpace(item.Value.PayloadHash)))
            throw new InvalidDataException("Invalid civil expedition receipts.");
        if (exp.Active is { } mission)
        {
            if (mission.MissionId == Guid.Empty || mission.Workers <= 0
                || mission.Mode != "balanced"
                || !FloralSiteOptions.ValidWorldId(mission.WorldId)
                || !FloralSiteOptions.ValidId(mission.RegionId)
                || !FloralSiteOptions.ValidId(mission.SiteId)
                || mission.StartedAtUtc.Offset != TimeSpan.Zero
                || mission.ArrivesAtUtc.Offset != TimeSpan.Zero
                || mission.WorkEndsAtUtc.Offset != TimeSpan.Zero
                || mission.ReturnsAtUtc.Offset != TimeSpan.Zero
                || !(mission.StartedAtUtc < mission.ArrivesAtUtc
                    && mission.ArrivesAtUtc < mission.WorkEndsAtUtc
                    && mission.WorkEndsAtUtc < mission.ReturnsAtUtc)
                || mission.ReturnsAtUtc - mission.StartedAtUtc > TimeSpan.FromDays(8)
                || (mission.AllianceIdAtStart.HasValue && mission.AllianceIdAtStart.Value == Guid.Empty)
                || mission.PollenPerWorker <= 0
                || mission.FertilityGainBp is < 0 or > 1000
                || mission.BiodiversityGainBp is < 0 or > 1000
                || civil is null || civil.AssignedWorkers < mission.Workers)
                throw new InvalidDataException("Invalid active civilian expedition.");
        }
        if (exp.LastCompletion is { } completion
            && (completion.MissionId == Guid.Empty
                || completion.PollenCredited < 0
                || completion.PollinationPoints < 0
                || completion.PollenBalanceAfter < 0
                || completion.SiteRevisionAfter < 0
                || completion.CompletedAtUtc.Offset != TimeSpan.Zero))
            throw new InvalidDataException("Invalid civilian expedition completion.");
    }
}
