using BeeKingdom.Accounts.Diagnostics;
using BeeKingdom.Accounts.Models;

namespace BeeKingdom.Accounts;

public sealed class AccountManager
{
    private readonly IAccountService service;

    public AccountManager(IAccountService service)
    {
        this.service = service;
    }

    public AccountDiagnostics Diagnostics => service.Diagnostics;
    public AccountRecord CreateAccount(CreateAccountRequest request) => service.CreateAccount(request);
    public AccountRecord? GetAccount(Guid accountId) => service.GetAccount(accountId);
    public AccountRecord? GetAccountByPlayerId(BeeKingdom.Shared.ValueObjects.PlayerId playerId) => service.GetAccountByPlayerId(playerId);
    public AccountRecord UpdateProfile(Guid accountId, string displayName, string? language = null, string? timeZone = null, string? country = null) => service.UpdateProfile(accountId, displayName, language, timeZone, country);
    public AccountRecord UpdatePreferences(Guid accountId, AccountPreferences preferences) => service.UpdatePreferences(accountId, preferences);
    public AccountRecord SuspendAccount(Guid accountId) => service.SuspendAccount(accountId);
    public AccountRecord ReactivateAccount(Guid accountId) => service.ReactivateAccount(accountId);
    public AccountRecord DeleteAccount(Guid accountId) => service.DeleteAccount(accountId);
    public IReadOnlyList<AccountRecord> QueryAccount(AccountQuery query) => service.QueryAccount(query);
}
