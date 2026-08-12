using System.Diagnostics;
using BeeKingdom.Authentication.Configuration;
using BeeKingdom.Authentication.Diagnostics;
using BeeKingdom.Authentication.Events;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication.Sessions;
using BeeKingdom.Authentication.Tokens;
using BeeKingdom.Infrastructure.Time;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Authentication;

public interface IAuthenticationService
{
    AuthenticationDiagnostics Diagnostics { get; }
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> AuthenticateWithGoogleAsync(GoogleAuthenticationRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationTokenPair?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    TokenValidationResult ValidateToken(string accessToken);
    bool RevokeToken(string token);
    bool Logout(string sessionId);
    int LogoutAllSessions(Guid accountId);
    AuthenticationSession? QuerySession(string sessionId);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IAuthenticationProvider provider;
    private readonly IAccountCredentialStore accounts;
    private readonly IGoogleIdentityExchanger googleIdentityExchanger;
    private readonly AuthenticationTokenManager tokenManager;
    private readonly AuthenticationSessionValidator sessionValidator;
    private readonly IAuthenticationSessionStore sessions;
    private readonly IAuthenticationEventSink eventSink;
    private readonly IServerClock clock;
    private readonly AuthenticationOptions options;

    public AuthenticationService(
        IAuthenticationProvider provider,
        IAccountCredentialStore accounts,
        IGoogleIdentityExchanger googleIdentityExchanger,
        AuthenticationTokenManager tokenManager,
        AuthenticationSessionValidator sessionValidator,
        IAuthenticationSessionStore sessions,
        IAuthenticationEventSink eventSink,
        IServerClock clock,
        IOptions<AuthenticationOptions> options)
    {
        this.provider = provider;
        this.accounts = accounts;
        this.googleIdentityExchanger = googleIdentityExchanger;
        this.tokenManager = tokenManager;
        this.sessionValidator = sessionValidator;
        this.sessions = sessions;
        this.eventSink = eventSink;
        this.clock = clock;
        this.options = options.Value;
    }

    public AuthenticationDiagnostics Diagnostics { get; } = new();

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        long start = Stopwatch.GetTimestamp();
        AuthenticationProviderResult providerResult = await provider.AuthenticateAsync(request, cancellationToken);
        if (!providerResult.Succeeded || providerResult.Account == null)
        {
            Diagnostics.RecordFailure(Stopwatch.GetTimestamp() - start);
            eventSink.Publish(new AuthenticationFailed(clock.UtcNow, request.Email, providerResult.ErrorCode ?? "authentication_failed"));
            return AuthenticationResult.Failure(providerResult.ErrorCode ?? "authentication_failed", providerResult.ErrorMessage ?? "Authentication failed.");
        }

        return CreateSessionResult(
            providerResult.Account,
            provider.ProviderKind,
            request.ClientVersion,
            request.IpAddress,
            request.DeviceIdentifier,
            request.Region,
            isNewAccount: false);
    }

    public async Task<AuthenticationResult> AuthenticateWithGoogleAsync(GoogleAuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        long start = Stopwatch.GetTimestamp();
        GoogleIdentity identity;
        try
        {
            identity = await googleIdentityExchanger.ExchangeAuthorizationCodeAsync(
                request.AuthorizationCode,
                request.CodeVerifier,
                request.RedirectUri,
                cancellationToken);
        }
        catch
        {
            Diagnostics.RecordFailure(Stopwatch.GetTimestamp() - start);
            return AuthenticationResult.Failure("google_exchange_failed", "Google sign-in could not be completed.");
        }

        if (string.IsNullOrWhiteSpace(identity.Subject) || string.IsNullOrWhiteSpace(identity.Email) || !identity.EmailVerified)
        {
            Diagnostics.RecordFailure(Stopwatch.GetTimestamp() - start);
            return AuthenticationResult.Failure("google_identity_invalid", "Google account is missing a verified email.");
        }

        bool isNewAccount = false;
        AuthenticationAccount account;
        if (!accounts.TryGetByGoogleSubjectId(identity.Subject, out account!))
        {
            if (accounts.TryGetByEmail(identity.Email, out AuthenticationAccount existingByEmail))
            {
                account = existingByEmail with { GoogleSubjectId = identity.Subject };
                accounts.Save(account);
            }
            else
            {
                account = accounts.CreateGoogleAccount(identity.Subject, identity.Email);
                isNewAccount = true;
            }
        }

        if (account.State == AccountSecurityState.Disabled)
        {
            Diagnostics.RecordFailure(Stopwatch.GetTimestamp() - start);
            return AuthenticationResult.Failure("account_disabled", "Account is disabled.");
        }

        return CreateSessionResult(
            account,
            AuthenticationProviderKind.Google,
            request.ClientVersion,
            request.IpAddress,
            request.DeviceIdentifier,
            request.Region,
            isNewAccount);
    }

    private AuthenticationResult CreateSessionResult(
        AuthenticationAccount account,
        AuthenticationProviderKind providerKind,
        string clientVersion,
        string ipAddress,
        string deviceIdentifier,
        string region,
        bool isNewAccount)
    {
        long start = Stopwatch.GetTimestamp();
        if (sessions.GetAccountSessions(account.AccountId).Count(session => !session.IsRevoked) >= options.MaxSessionsPerAccount)
        {
            Diagnostics.RecordFailure(Stopwatch.GetTimestamp() - start);
            return AuthenticationResult.Failure("max_sessions_reached", "Maximum session count reached.");
        }

        string sessionId = Guid.NewGuid().ToString("N");
        DateTimeOffset now = clock.UtcNow;
        AuthenticationSession session = new(
            sessionId,
            account.PlayerId,
            account.AccountId,
            providerKind,
            now,
            now,
            now.Add(options.RefreshTokenLifetime),
            clientVersion,
            ipAddress,
            deviceIdentifier,
            region,
            false);

        sessions.Save(session);
        AuthenticationTokenPair tokens = tokenManager.CreateTokenPair(account.PlayerId, account.AccountId, sessionId, now);
        Diagnostics.RecordSuccess(Stopwatch.GetTimestamp() - start);
        eventSink.Publish(new PlayerAuthenticated(clock.UtcNow, account.PlayerId, account.AccountId, sessionId));
        eventSink.Publish(new SessionCreated(clock.UtcNow, account.PlayerId, account.AccountId, sessionId));
        return AuthenticationResult.Success(account.PlayerId, account.AccountId, session, tokens, isNewAccount, account.DisplayName, account.IsOnboarded);
    }

    public Task<AuthenticationTokenPair?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(tokenManager.RotateRefreshToken(refreshToken));
    }

    public TokenValidationResult ValidateToken(string accessToken) => sessionValidator.ValidateToken(accessToken);

    public bool RevokeToken(string token) => tokenManager.RevokeToken(token);

    public bool Logout(string sessionId)
    {
        if (!sessions.TryGet(sessionId, out AuthenticationSession session))
        {
            return false;
        }

        sessions.Save(session with { IsRevoked = true });
        Diagnostics.RecordSessionClosed();
        eventSink.Publish(new PlayerLoggedOut(clock.UtcNow, session.PlayerId, session.AccountId, session.SessionId));
        eventSink.Publish(new SessionRevoked(clock.UtcNow, session.PlayerId, session.AccountId, session.SessionId));
        return true;
    }

    public int LogoutAllSessions(Guid accountId)
    {
        int count = 0;
        foreach (AuthenticationSession session in sessions.GetAccountSessions(accountId))
        {
            if (!session.IsRevoked && Logout(session.SessionId))
            {
                count++;
            }
        }

        return count;
    }

    public AuthenticationSession? QuerySession(string sessionId)
    {
        return sessions.TryGet(sessionId, out AuthenticationSession session) ? session : null;
    }
}
