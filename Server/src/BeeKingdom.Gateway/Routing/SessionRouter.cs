using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;

namespace BeeKingdom.Gateway.Routing;

public sealed class SessionRouter
{
    private readonly AuthenticationManager authentication;

    public SessionRouter(AuthenticationManager authentication)
    {
        this.authentication = authentication;
    }

    public TokenValidationResult ValidateSession(string accessToken)
    {
        return authentication.ValidateToken(accessToken);
    }
}
