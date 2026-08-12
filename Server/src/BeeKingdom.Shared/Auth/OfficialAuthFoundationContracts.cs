using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Auth;

public static class OfficialAuthFoundation
{
    public const string EvidenceId = "SERVER-052-OFFICIAL-AUTH-FOUNDATION";

    public static OfficialAuthFoundationDescriptor CreateDescriptor()
    {
        return new OfficialAuthFoundationDescriptor(
            EvidenceId,
            ServerAuthoritativeRequired: true,
            UnityConnected: false,
            OfficialAccountLiveClaimAllowed: false,
            GoogleProviderActive: false,
            FacebookProviderActive: false,
            OAuthSecretsAllowedInRepository: false,
            ProductionPublishAllowed: false,
            Endpoints:
            [
                new OfficialAuthEndpointPlan("CreateAccount", "POST", "/auth/accounts", ImplementedNow: false, OfficialLiveNow: false, "Wrap account creation, credentials and initial session behind official validation."),
                new OfficialAuthEndpointPlan("Login", "POST", "/auth/login", ImplementedNow: true, OfficialLiveNow: false, "Harden existing primitive login before Unity integration."),
                new OfficialAuthEndpointPlan("RefreshToken", "POST", "/auth/refresh", ImplementedNow: true, OfficialLiveNow: false, "Rotate the one-time refresh token while preserving server-bound PlayerId and SessionId metadata."),
                new OfficialAuthEndpointPlan("GetPlayerProfile", "GET", "/me/profile", ImplementedNow: false, OfficialLiveNow: false, "Return authenticated profile without secrets."),
                new OfficialAuthEndpointPlan("ListGameServers", "GET", "/game-servers", ImplementedNow: false, OfficialLiveNow: false, "Expose read-only server selection candidates."),
                new OfficialAuthEndpointPlan("SelectGameServer", "POST", "/me/game-server-selection", ImplementedNow: false, OfficialLiveNow: false, "Persist future WorldId/GameServerId selection after capacity checks."),
                new OfficialAuthEndpointPlan("LinkGoogle", "POST", "/auth/link/google", ImplementedNow: false, OfficialLiveNow: false, "Wait for OAuth configuration and secret handling."),
                new OfficialAuthEndpointPlan("LinkFacebook", "POST", "/auth/link/facebook", ImplementedNow: false, OfficialLiveNow: false, "Wait for OAuth configuration and secret handling."),
                new OfficialAuthEndpointPlan("GuestDemo", "POST", "/auth/guest", ImplementedNow: false, OfficialLiveNow: false, "Keep guest/demo separate from official accounts.")
            ],
            ServerOwnedData:
            [
                "AccountId",
                "PlayerId",
                "CanonicalEmail",
                "DisplayName",
                "PasswordHash",
                "AccountStatus",
                "AuthenticationSessions",
                "AccessTokenHash",
                "RefreshTokenHash",
                "ProviderLinks",
                "WorldId",
                "GameServerId",
                "SecurityEvents"
            ],
            TemporaryLocalDemoAllowed:
            [
                "local demo splash state",
                "guest demo label",
                "mock profile preview",
                "server selection preview"
            ],
            OutOfScopeNow:
            [
                "Unity connection",
                "real Google OAuth",
                "real Facebook OAuth",
                "OAuth secrets",
                "production publish",
                "matchmaking",
                "alliances",
                "world map"
            ],
            SecurityRisks:
            [
                "credential stuffing without rate limiting",
                "provider token spoofing if OAuth is trusted client-side",
                "session fixation without token rotation",
                "secret leakage in repository or logs",
                "account enumeration through detailed auth errors",
                "official-live claim before QA validation"
            ],
            NextSteps:
            [
                "Add official account creation facade with duplicate email/display name handling.",
                "Harden auth HTTP responses with stable error envelope and no account enumeration.",
                "Add profile endpoint behind token validation.",
                "Add read-only game server selection contract backed by World Registry.",
                "Prepare OAuth provider configuration model without storing secrets in repository.",
                "Add HTTP tests before Unity integration."
            ],
            ContractVersion.Current);
    }

    public static IReadOnlyList<OfficialAuthErrorCode> RequiredErrorCodes { get; } =
    [
        OfficialAuthErrorCode.EmailAlreadyUsed,
        OfficialAuthErrorCode.DisplayNameAlreadyUsed,
        OfficialAuthErrorCode.InvalidPassword,
        OfficialAuthErrorCode.InvalidCredentials,
        OfficialAuthErrorCode.ServerUnavailable,
        OfficialAuthErrorCode.SessionExpired,
        OfficialAuthErrorCode.GoogleProviderNotConfigured,
        OfficialAuthErrorCode.FacebookProviderNotConfigured,
        OfficialAuthErrorCode.AccountSuspended,
        OfficialAuthErrorCode.AccountBanned
    ];
}

public sealed record OfficialAuthFoundationDescriptor(
    string EvidenceId,
    bool ServerAuthoritativeRequired,
    bool UnityConnected,
    bool OfficialAccountLiveClaimAllowed,
    bool GoogleProviderActive,
    bool FacebookProviderActive,
    bool OAuthSecretsAllowedInRepository,
    bool ProductionPublishAllowed,
    IReadOnlyList<OfficialAuthEndpointPlan> Endpoints,
    IReadOnlyList<string> ServerOwnedData,
    IReadOnlyList<string> TemporaryLocalDemoAllowed,
    IReadOnlyList<string> OutOfScopeNow,
    IReadOnlyList<string> SecurityRisks,
    IReadOnlyList<string> NextSteps,
    ContractVersion ContractVersion);

public sealed record OfficialAuthEndpointPlan(
    string Name,
    string Method,
    string Path,
    bool ImplementedNow,
    bool OfficialLiveNow,
    string NextAction);

public sealed record OfficialCreateAccountDraft(
    string Email,
    string DisplayName,
    string PasswordPolicyVersion,
    string ClientVersion,
    string Region,
    bool LocalDemoAllowed,
    bool OfficialLiveClaimAllowed,
    ContractVersion ContractVersion);

public sealed record OfficialLoginDraft(
    string Provider,
    string ClientVersion,
    string DeviceIdentifier,
    string Region,
    bool ServerMustValidateCredentials,
    bool OfficialLiveClaimAllowed,
    ContractVersion ContractVersion);

public sealed record OfficialSessionTokenDraft(
    string SessionId,
    PlayerId PlayerId,
    Guid AccountId,
    DateTimeOffset AccessTokenExpiresUtc,
    DateTimeOffset RefreshTokenExpiresUtc,
    bool TokensStoredAsHash,
    bool RefreshRotationRequired,
    ContractVersion ContractVersion);

public sealed record OfficialPlayerProfileDraft(
    Guid AccountId,
    PlayerId PlayerId,
    string DisplayName,
    string AccountStatus,
    WorldId? SelectedWorldId,
    GameServerId? SelectedGameServerId,
    bool SecretsIncluded,
    ContractVersion ContractVersion);

public enum OfficialAuthErrorCode
{
    EmailAlreadyUsed = 0,
    DisplayNameAlreadyUsed = 1,
    InvalidPassword = 2,
    InvalidCredentials = 3,
    ServerUnavailable = 4,
    SessionExpired = 5,
    GoogleProviderNotConfigured = 6,
    FacebookProviderNotConfigured = 7,
    AccountSuspended = 8,
    AccountBanned = 9,
    AuthRequired = 10,
    ClientVersionUnsupported = 11,
    ProviderTokenInvalid = 12,
    ProviderAccountAlreadyLinked = 13
}
