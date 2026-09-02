using BeeKingdom.Accounts.Models;
using BeeKingdom.Authentication.Providers;
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
    private readonly IAccountCredentialStore credentials;

    public PlayerDirectoryService(IAccountService accounts, IAccountCredentialStore credentials)
    {
        this.accounts = accounts;
        this.credentials = credentials;
    }

    // M043R-CL: was BeeKingdom.Accounts-only, so real Google/onboarded players (who only ever get
    // a row in BeeKingdom.Authentication.AuthenticationAccounts, never in this project's own
    // Account record - see M043P) were invisible to search even though GetByPlayerId already knew
    // them. Merges the authoritative source (real onboarded players) with the legacy source
    // (synthetic/seeded test accounts that only exist here), deduplicated by PlayerId with the
    // same identity precedence M043P established for GetByPlayerId: authoritative wins on
    // conflict. Still deliberately rejects an empty/too-short query rather than returning
    // "everyone" - a blank q="" must never be a trivial way to extract the whole player base
    // (M043B brief, Privacy section).
    public IReadOnlyList<PlayerPublicIdentity> Search(string displayNameContains, int offset, int limit)
    {
        string trimmed = (displayNameContains ?? string.Empty).Trim();
        if (trimmed.Length < MinQueryLength || trimmed.Length > MaxQueryLength)
            throw new ArgumentException("query_too_short_or_too_long");

        int safeOffset = Math.Max(0, offset);
        int safeLimit = Math.Clamp(limit <= 0 ? 20 : limit, 1, MaxLimit);

        IEnumerable<PlayerPublicIdentity> legacy = accounts.QueryAccount(new AccountQuery(DisplayNameContains: trimmed))
            .Where(account => account.Profile.Status == AccountStatus.Active)
            .Select(ToPublicIdentity);

        IEnumerable<PlayerPublicIdentity> authoritative = credentials.SearchByDisplayName(trimmed)
            .Where(account => account.IsOnboarded && !string.IsNullOrWhiteSpace(account.DisplayName))
            .Select(account => new PlayerPublicIdentity(account.PlayerId, account.DisplayName));

        var merged = new Dictionary<PlayerId, PlayerPublicIdentity>();
        foreach (PlayerPublicIdentity identity in legacy) merged[identity.PlayerId] = identity;
        foreach (PlayerPublicIdentity identity in authoritative) merged[identity.PlayerId] = identity;

        // Prefix matches first (e.g. "St" -> "Stara" ranks above a hypothetical "Allstar"), then
        // alphabetical - a nicer default than raw insertion order for a player-facing search list.
        return merged.Values
            .OrderByDescending(identity => identity.DisplayName.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
            .ThenBy(identity => identity.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Skip(safeOffset)
            .Take(safeLimit)
            .ToArray();
    }

    // M043P-CL: the real, onboarded public name (POST /auth/display-name) lives in
    // BeeKingdom.Authentication's AuthenticationAccounts, not on this project's own Account
    // record - that field exists but the real Google-auth onboarding flow never writes to it, so
    // every real player showed up as a truncated PlayerId everywhere (Alliance dashboard, Journal
    // actor names, member roster). Authoritative source checked first; the Accounts-based lookup
    // remains as a fallback for any account that only exists there (e.g. synthetic/seeded test
    // accounts created directly via IAccountService, never through real Google onboarding).
    public PlayerPublicIdentity? GetByPlayerId(PlayerId playerId)
    {
        if (credentials.TryGetByPlayerId(playerId, out AuthenticationAccount authAccount)
            && authAccount.IsOnboarded && !string.IsNullOrWhiteSpace(authAccount.DisplayName))
        {
            return new PlayerPublicIdentity(playerId, authAccount.DisplayName);
        }

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
