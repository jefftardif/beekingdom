namespace BeeKingdom.Authentication.Providers;

public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified);

public interface IGoogleIdentityExchanger
{
    Task<GoogleIdentity> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken);
}
