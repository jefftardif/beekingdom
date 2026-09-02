using BeeKingdom.Accounts.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Accounts;

// M043B-CL: the generic, reusable player-search/lookup surface the M043 report identified as
// entirely missing from this codebase (the only reachable name-search before this was
// /accounts/v1/role/lookup - admin-only, leaks email). Deliberately NOT Alliance-specific: it lives
// in BeeKingdom.Accounts (the account/profile domain) and returns only PlayerPublicIdentity
// (BeeKingdom.Shared) so Communication, Friends, mail recipient selection, and Alliance invites can
// all depend on this same service later without duplicating search logic - see
// Docs/AI/Missions/M043B-CL-Alliance-Center-Functional-Closeout.md section 5.
public interface IPlayerDirectoryService
{
    IReadOnlyList<PlayerPublicIdentity> Search(string displayNameContains, int offset, int limit);
    PlayerPublicIdentity? GetByPlayerId(PlayerId playerId);
    IReadOnlyDictionary<PlayerId, PlayerPublicIdentity> GetByPlayerIds(IReadOnlyCollection<PlayerId> playerIds);
}

public sealed class PlayerDirectoryService : IPlayerDirectoryService
{
    public const int MinQueryLength = 2;
    public const int MaxQueryLength = 64;
    public const int MaxLimit = 50;

    private readonly IAccountService accounts;

    public PlayerDirectoryService(IAccountService accounts)
    {
        this.accounts = accounts;
    }

    // Deliberately rejects an empty/too-short query rather than returning "everyone" - a blank q=""
    // must never be a trivial way to extract the whole player base (M043B brief, Privacy section).
    public IReadOnlyList<PlayerPublicIdentity> Search(string displayNameContains, int offset, int limit)
    {
        string trimmed = (displayNameContains ?? string.Empty).Trim();
        if (trimmed.Length < MinQueryLength || trimmed.Length > MaxQueryLength)
            throw new ArgumentException("query_too_short_or_too_long");

        int safeOffset = Math.Max(0, offset);
        int safeLimit = Math.Clamp(limit <= 0 ? 20 : limit, 1, MaxLimit);

        return accounts.QueryAccount(new AccountQuery(DisplayNameContains: trimmed))
            .Where(account => account.Profile.Status == AccountStatus.Active)
            .OrderBy(account => account.Profile.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(ToPublicIdentity)
            .ToArray();
    }

    public PlayerPublicIdentity? GetByPlayerId(PlayerId playerId)
    {
        AccountRecord? account = accounts.GetAccountByPlayerId(playerId);
        return account == null ? null : ToPublicIdentity(account);
    }

    // Batch resolution to avoid N+1 HTTP calls from Unity when rendering an Alliance member roster
    // (M043B brief, Part 5) - one server-side pass, not one round trip per member.
    public IReadOnlyDictionary<PlayerId, PlayerPublicIdentity> GetByPlayerIds(IReadOnlyCollection<PlayerId> playerIds)
    {
        var result = new Dictionary<PlayerId, PlayerPublicIdentity>();
        foreach (PlayerId playerId in playerIds.Distinct())
        {
            PlayerPublicIdentity? identity = GetByPlayerId(playerId);
            if (identity != null) result[playerId] = identity;
        }
        return result;
    }

    private static PlayerPublicIdentity ToPublicIdentity(AccountRecord account) =>
        new(account.Profile.PlayerId, account.Profile.DisplayName);
}
