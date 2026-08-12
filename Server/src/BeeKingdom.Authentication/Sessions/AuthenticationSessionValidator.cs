using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Tokens;
using BeeKingdom.Infrastructure.Time;

namespace BeeKingdom.Authentication.Sessions;

public sealed class AuthenticationSessionValidator
{
    private readonly AuthenticationTokenManager tokenManager;
    private readonly IAuthenticationSessionStore sessions;
    private readonly IServerClock clock;

    public AuthenticationSessionValidator(AuthenticationTokenManager tokenManager, IAuthenticationSessionStore sessions, IServerClock clock)
    {
        this.tokenManager = tokenManager;
        this.sessions = sessions;
        this.clock = clock;
    }

    public TokenValidationResult ValidateToken(string accessToken)
    {
        TokenValidationResult token = tokenManager.ValidateAccessToken(accessToken);
        if (!token.IsValid)
        {
            return token;
        }

        if (!sessions.TryGet(token.SessionId, out AuthenticationSession session))
        {
            return TokenValidationResult.Invalid("session_not_found");
        }

        if (session.IsRevoked)
        {
            return TokenValidationResult.Invalid("session_revoked");
        }

        if (session.ExpirationUtc <= clock.UtcNow)
        {
            return TokenValidationResult.Invalid("session_expired");
        }

        sessions.Save(session with { LastActivityUtc = clock.UtcNow });
        return token;
    }
}
