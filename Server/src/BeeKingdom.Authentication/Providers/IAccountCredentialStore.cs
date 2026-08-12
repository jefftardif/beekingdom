using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Providers;

public interface IAccountCredentialStore
{
    AuthenticationAccount CreateEmailAccount(string email, string password);
    AuthenticationAccount CreateGoogleAccount(string googleSubjectId, string email);
    bool TryGetByEmail(string email, out AuthenticationAccount account);
    bool TryGetByGoogleSubjectId(string googleSubjectId, out AuthenticationAccount account);
    bool TryGetByAccountId(Guid accountId, out AuthenticationAccount account);
    bool IsDisplayNameTaken(Guid worldId, string displayName, Guid excludingAccountId);
    void Save(AuthenticationAccount account);
}
