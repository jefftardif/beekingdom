using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Providers;

public interface IAccountCredentialStore
{
    AuthenticationAccount CreateEmailAccount(string email, string password);
    AuthenticationAccount CreateGoogleAccount(string googleSubjectId, string email);
    bool TryGetByEmail(string email, out AuthenticationAccount account);
    bool TryGetByGoogleSubjectId(string googleSubjectId, out AuthenticationAccount account);
    bool TryGetByAccountId(Guid accountId, out AuthenticationAccount account);
    // M043P-CL: the authoritative source for a player's real, onboarded public DisplayName
    // (set via POST /auth/display-name) - Alliance/PlayerDirectory previously read a different,
    // unrelated DisplayName field on BeeKingdom.Accounts' own Account record, which the real
    // Google-auth onboarding flow never populates. See PlayerDirectoryService.GetByPlayerId.
    bool TryGetByPlayerId(PlayerId playerId, out AuthenticationAccount account);
    bool IsDisplayNameTaken(Guid worldId, string displayName, Guid excludingAccountId);
    IReadOnlyList<AuthenticationAccount> SearchByDisplayName(string displayNameContains);
    void Save(AuthenticationAccount account);
}
