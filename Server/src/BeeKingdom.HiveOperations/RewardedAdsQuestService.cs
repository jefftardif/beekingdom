using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

// One authenticated rewarded video may be credited at most three times per
// player and UTC day. This state is stored in the EXISTING atomic HivePlayerStates
// transaction, alongside the real honey and pollen balances.
public sealed record RewardedAdsQuestState(
    Dictionary<string, int> CountByDay,
    Dictionary<string, DateTimeOffset> ProcessedEvents,
    long LifetimeHoneyCredited,
    long LifetimePollenCredited);

public sealed record RewardedAdsQuestSnapshot(
    Guid PlayerId, Guid HiveId, string ContractVersion, bool Enabled,
    DateTimeOffset DayUtc, DateTimeOffset NextResetUtc, DateTimeOffset ServerTimeUtc,
    int CompletedCount, int MaximumCount, long NextHoney, long NextPollen,
    long LifetimeHoneyCredited, long LifetimePollenCredited);

public sealed record LevelPlayRewardCallback(
    string Timestamp, string EventId, string UserId, string Rewards, string Signature,
    string? AppKey = null);

public sealed record LevelPlayRewardProcessingResult(bool Ack, string Code, string EventId);

public sealed class RewardedAdsQuestOptions
{
    public const string SectionName = "RewardedAdsQuest";
    public const string ExpectedAppKey = "28f6cc45d";
    public bool Enabled { get; set; }
    // NEVER put this value into committed appsettings or into Unity.
    public string PrivateKey { get; set; } = string.Empty;
    public string AppKey { get; set; } = ExpectedAppKey;

    public bool IsReady =>
        Enabled && PrivateKey is { Length: >= 32 } &&
        string.Equals(AppKey, ExpectedAppKey, StringComparison.Ordinal);
}

public static class LevelPlayRewardSignature
{
    // LevelPlay documents timestamp as UTC yyyyMMddHHmm (not a Unix timestamp).
    // md5(timestamp + eventId + URL-decoded applicationUserId + rewards + privateKey)
    public static bool TryVerify(
        LevelPlayRewardCallback callback, RewardedAdsQuestOptions options,
        DateTimeOffset now, out Guid playerId, out DateTimeOffset eventTime)
    {
        playerId = Guid.Empty;
        eventTime = default;
        if (callback is null || options is null || !options.IsReady ||
            callback.UserId is not { Length: 32 } ||
            !Guid.TryParseExact(callback.UserId, "N", out playerId) ||
            playerId == Guid.Empty ||
            callback.EventId is not { Length: >= 1 and <= 128 } ||
            callback.EventId.Any(c => !char.IsAsciiLetterOrDigit(c)) ||
            !string.Equals(callback.Rewards, "1", StringComparison.Ordinal) ||
            callback.Signature is not { Length: 32 } ||
            callback.Signature.Any(c => !Uri.IsHexDigit(c)) ||
            callback.Timestamp is not { Length: 12 } ||
            (callback.AppKey is not null &&
             !string.Equals(callback.AppKey, options.AppKey, StringComparison.Ordinal)) ||
            !DateTime.TryParseExact(callback.Timestamp, "yyyyMMddHHmm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal, out DateTime parsed))
            return false;

        eventTime = new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
        // Retry window of the provider: 7 days, with a safety margin.
        if (eventTime < now.AddDays(-10) || eventTime > now.AddMinutes(5))
            return false;

        byte[] message = Encoding.UTF8.GetBytes(
            callback.Timestamp + callback.EventId + callback.UserId +
            callback.Rewards + options.PrivateKey);
        byte[] computed = MD5.HashData(message);
        byte[] supplied = Convert.FromHexString(callback.Signature);
        return CryptographicOperations.FixedTimeEquals(computed, supplied);
    }
}

public sealed class RewardedAdsQuestService(
    IHiveStateRepository repository,
    IServerClock clock,
    RewardedAdsQuestOptions options)
{
    public const int DailyLimit = 3;
    public const string ContractVersion = "beekindom-rewarded-quest-v1";
    // Includes the third-video bonus chest; intentionally modest vs PvP economy.
    private static readonly (long Honey, long Pollen)[] Rewards =
        [(20L, 10L), (25L, 15L), (75L, 35L)];

    public async Task<RewardedAdsQuestSnapshot?> ReadAsync(
        Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        var state = await repository.ReadAsync(playerId, hiveId, ct);
        if (state is null) return null;
        return Snapshot(state, clock.UtcNow, options.IsReady);
    }

    public async Task<LevelPlayRewardProcessingResult> ProcessAsync(
        LevelPlayRewardCallback callback, CancellationToken ct = default)
    {
        if (!options.IsReady)
            return new(false, "not_configured", string.Empty);

        DateTimeOffset now = clock.UtcNow;
        if (!LevelPlayRewardSignature.TryVerify(
                callback, options, now, out Guid playerId, out DateTimeOffset eventTime))
            return new(false, "invalid_signature_or_payload", string.Empty);

        IReadOnlyList<Guid> hives = await repository.ListHiveIdsAsync(playerId, ct);
        // The official game has exactly one registered hive per player. Fail
        // closed instead of crediting a different hive or creating one silently.
        if (hives.Count != 1 || hives[0] == Guid.Empty)
            return new(false, "unknown_or_ambiguous_hive", callback.EventId);

        Guid hiveId = hives[0];
        if (await repository.ReadAsync(playerId, hiveId, ct) is null)
            return new(false, "hive_not_found", callback.EventId);

        LevelPlayRewardProcessingResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            RewardedAdsQuestState existing = state.RewardedAdsQuest ??
                new(new Dictionary<string, int>(StringComparer.Ordinal),
                    new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal),
                    0, 0);
            Dictionary<string, int> days = new(existing.CountByDay, StringComparer.Ordinal);
            Dictionary<string, DateTimeOffset> processed =
                new(existing.ProcessedEvents, StringComparer.Ordinal);

            // Keep > provider retry horizon. An event older than 10 days is
            // rejected before reaching this block.
            DateTimeOffset cutoff = now.AddDays(-12);
            foreach (string id in processed.Where(pair => pair.Value < cutoff)
                                           .Select(pair => pair.Key).ToArray())
                processed.Remove(id);
            foreach (string day in days.Keys.ToArray())
            {
                if (DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime parsed) &&
                    parsed < now.UtcDateTime.Date.AddDays(-12))
                    days.Remove(day);
            }
            if (processed.ContainsKey(callback.EventId))
            {
                result = new(true, "already_processed", callback.EventId);
                return state;
            }
            if (processed.Count >= 512)
            {
                result = new(false, "event_ledger_full", callback.EventId);
                return state;
            }

            string dayKey = eventTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            days.TryGetValue(dayKey, out int completed);
            processed[callback.EventId] = now;
            if (completed >= DailyLimit)
            {
                result = new(true, "daily_limit_reached", callback.EventId);
                return state with
                {
                    Revision = state.Revision + 1,
                    RewardedAdsQuest = existing with
                    {
                        CountByDay = days,
                        ProcessedEvents = processed
                    }
                };
            }

            Dictionary<string, ResourceBalance> resources =
                new(state.Resources, StringComparer.Ordinal);
            var reward = Rewards[completed];
            long creditedHoney = Credit(resources, "honey", reward.Honey);
            long creditedPollen = Credit(resources, "pollen", reward.Pollen);
            days[dayKey] = completed + 1;
            result = new(true, "credited", callback.EventId);
            return state with
            {
                Revision = state.Revision + 1,
                Resources = resources,
                RewardedAdsQuest = new(days, processed,
                    checked(existing.LifetimeHoneyCredited + creditedHoney),
                    checked(existing.LifetimePollenCredited + creditedPollen))
            };
        }, ct);

        return result ?? new(false, "transaction_failed", callback.EventId);
    }

    private static long Credit(Dictionary<string, ResourceBalance> resources, string key, long amount)
    {
        if (!resources.TryGetValue(key, out ResourceBalance? balance) ||
            balance.Amount < 0 || balance.Capacity < 0 || balance.Amount > balance.Capacity)
            throw new InvalidDataException("Hive resource balance is invalid.");
        long credited = Math.Min(amount, balance.Capacity - balance.Amount);
        resources[key] = balance with { Amount = checked(balance.Amount + credited) };
        return credited;
    }

    private static RewardedAdsQuestSnapshot Snapshot(
        PlayerHiveState state, DateTimeOffset now, bool enabled)
    {
        DateTimeOffset utcDay = new(now.UtcDateTime.Date, TimeSpan.Zero);
        string day = utcDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        RewardedAdsQuestState? rewards = state.RewardedAdsQuest;
        int completed = 0;
        rewards?.CountByDay?.TryGetValue(day, out completed);
        completed = Math.Clamp(completed, 0, DailyLimit);
        var next = completed < DailyLimit ? Rewards[completed] : (0L, 0L);
        return new(state.PlayerId, state.HiveId, ContractVersion, enabled, utcDay,
            utcDay.AddDays(1), now, completed, DailyLimit, next.Item1, next.Item2,
            rewards?.LifetimeHoneyCredited ?? 0L,
            rewards?.LifetimePollenCredited ?? 0L);
    }
}
