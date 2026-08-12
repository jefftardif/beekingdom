using BeeKingdom.Authentication.Models;

namespace BeeKingdom.Authentication;

public sealed class AuthenticationManager
{
    private readonly IAuthenticationService service;

    public AuthenticationManager(IAuthenticationService service)
    {
        this.service = service;
    }

    public Task<AuthenticationResult> Authenticate(AuthenticationRequest request, CancellationToken cancellationToken = default) => service.AuthenticateAsync(request, cancellationToken);
    public Task<AuthenticationResult> AuthenticateWithGoogle(GoogleAuthenticationRequest request, CancellationToken cancellationToken = default) => service.AuthenticateWithGoogleAsync(request, cancellationToken);
    public Task<AuthenticationTokenPair?> RefreshToken(string refreshToken, CancellationToken cancellationToken = default) => service.RefreshTokenAsync(refreshToken, cancellationToken);
    public TokenValidationResult ValidateToken(string accessToken) => service.ValidateToken(accessToken);
    public bool RevokeToken(string token) => service.RevokeToken(token);
    public bool Logout(string sessionId) => service.Logout(sessionId);
    public int LogoutAllSessions(Guid accountId) => service.LogoutAllSessions(accountId);
    public AuthenticationSession? QuerySession(string sessionId) => service.QuerySession(sessionId);
}
