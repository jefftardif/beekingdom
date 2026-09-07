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

    // M056-CL: HARD delete of the credential row - the only way the email address becomes free to
    // register again (Email carries a UNIQUE index, see DatabaseCatalog 030_authentication_sessions).
    // Deliberately NOT the same thing as AccountService.DeleteAccount /
    // AccountStatus.Deleted, which is a status flag on the dormant BeeKingdom.Accounts
    // AccountManager/AccountProfile subsystem and has no effect whatsoever on login: this store is
    // what Google/email authentication actually reads. Returns false when no such account exists.
    // Only ever reached from the Admin-gated account-deletion endpoint.
    bool DeleteAccount(Guid accountId);
}
