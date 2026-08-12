using BeeKingdom.Authentication.Models;

namespace BeeKingdom.Authentication.Sessions;

public interface IAuthenticationSessionStore
{
    void Save(AuthenticationSession session);
    bool TryGet(string sessionId, out AuthenticationSession session);
    IReadOnlyList<AuthenticationSession> GetAccountSessions(Guid accountId);
}
