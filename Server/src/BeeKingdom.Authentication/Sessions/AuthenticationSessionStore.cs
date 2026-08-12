using BeeKingdom.Authentication.Models;

namespace BeeKingdom.Authentication.Sessions;

public sealed class AuthenticationSessionStore : IAuthenticationSessionStore
{
    private readonly Dictionary<string, AuthenticationSession> sessionsById = new(StringComparer.Ordinal);
    private readonly object sync = new();

    public void Save(AuthenticationSession session)
    {
        lock (sync)
        {
            sessionsById[session.SessionId] = session;
        }
    }

    public bool TryGet(string sessionId, out AuthenticationSession session)
    {
        lock (sync)
        {
            return sessionsById.TryGetValue(sessionId, out session!);
        }
    }

    public IReadOnlyList<AuthenticationSession> GetAccountSessions(Guid accountId)
    {
        lock (sync)
        {
            return sessionsById.Values.Where(session => session.AccountId == accountId).ToArray();
        }
    }
}
