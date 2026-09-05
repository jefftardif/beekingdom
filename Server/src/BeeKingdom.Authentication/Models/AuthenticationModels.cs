using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Models;

public enum AuthenticationProviderKind
{
    EmailPassword = 0,
    Google = 1,
    Apple = 2,
    Steam = 3,
    Epic = 4,
    Guest = 5
}

public enum AccountSecurityState
{
    Active = 0,
    Locked = 1,
    Disabled = 2
}

// Admin is bootstrap-only (granted via the shared-secret admin support endpoint, never
// in-game) - it exists so a small number of trusted accounts can grant/revoke Moderator to
// other players from inside the game itself. See IAccountCredentialStore.Save.
public enum AccountRole
{
    Player = 0,
    Moderator = 1,
    Admin = 2
}

public sealed record AuthenticationRequest(
    string Email,
    string Password,
    string ClientVersion,
    string IpAddress,
    string DeviceIdentifier,
    string Region);

public sealed record GoogleAuthenticationRequest(
    string AuthorizationCode,
    string CodeVerifier,
    string RedirectUri,
    string ClientVersion,
    string IpAddress,
    string DeviceIdentifier,
    string Region);

public sealed record AuthenticationResult(
    bool Succeeded,
    PlayerId PlayerId,
    Guid AccountId,
    AuthenticationSession? Session,
    AuthenticationTokenPair? Tokens,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsNewAccount = false,
    string? DisplayName = null,
    bool IsOnboarded = false,
    // M0??-CL: lets the website (and any other client) learn "is this signed-in visitor an
    // Admin" from the login response itself, no second round-trip. Defaulted so every pre-existing
    // AuthenticationResult.Success(...) call site keeps compiling unchanged.
    AccountRole Role = AccountRole.Player)
{
    public static AuthenticationResult Success(
        PlayerId playerId,
        Guid accountId,
        AuthenticationSession session,
        AuthenticationTokenPair tokens,
        bool isNewAccount = false,
        string? displayName = null,
        bool isOnboarded = false,
        AccountRole role = AccountRole.Player)
    {
        return new AuthenticationResult(true, playerId, accountId, session, tokens, null, null, isNewAccount, displayName, isOnboarded, role);
    }

    public static AuthenticationResult Failure(string code, string message)
    {
        return new AuthenticationResult(false, default, Guid.Empty, null, null, code, message);
    }
}

public sealed record AuthenticationSession(
    string SessionId,
    PlayerId PlayerId,
    Guid AccountId,
    AuthenticationProviderKind AuthenticationProvider,
    DateTimeOffset LoginUtc,
    DateTimeOffset LastActivityUtc,
    DateTimeOffset ExpirationUtc,
    string ClientVersion,
    string IpAddress,
    string DeviceIdentifier,
    string Region,
    bool IsRevoked);

public sealed record AuthenticationTokenPair(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresUtc,
    DateTimeOffset RefreshTokenExpiresUtc,
    PlayerId PlayerId = default,
    string SessionId = "");

public sealed record TokenValidationResult(bool IsValid, PlayerId PlayerId, Guid AccountId, string SessionId, string? ErrorCode)
{
    public static TokenValidationResult Valid(PlayerId playerId, Guid accountId, string sessionId)
    {
        return new TokenValidationResult(true, playerId, accountId, sessionId, null);
    }

    public static TokenValidationResult Invalid(string code)
    {
        return new TokenValidationResult(false, default, Guid.Empty, string.Empty, code);
    }
}
