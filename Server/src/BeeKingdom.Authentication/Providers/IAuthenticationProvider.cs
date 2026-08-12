using BeeKingdom.Authentication.Models;

namespace BeeKingdom.Authentication.Providers;

public interface IAuthenticationProvider
{
    AuthenticationProviderKind ProviderKind { get; }
    Task<AuthenticationProviderResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default);
}

public sealed record AuthenticationProviderResult(bool Succeeded, AuthenticationAccount? Account, string? ErrorCode, string? ErrorMessage)
{
    public static AuthenticationProviderResult Success(AuthenticationAccount account) => new(true, account, null, null);
    public static AuthenticationProviderResult Failure(string code, string message) => new(false, null, code, message);
}
