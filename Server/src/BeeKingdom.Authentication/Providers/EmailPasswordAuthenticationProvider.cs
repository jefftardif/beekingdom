using BeeKingdom.Authentication.Configuration;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Infrastructure.Time;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Authentication.Providers;

public sealed class EmailPasswordAuthenticationProvider : IAuthenticationProvider
{
    private readonly IAccountCredentialStore accounts;
    private readonly IPasswordHasher passwordHasher;
    private readonly IServerClock clock;
    private readonly AuthenticationOptions options;

    public EmailPasswordAuthenticationProvider(IAccountCredentialStore accounts, IPasswordHasher passwordHasher, IServerClock clock, IOptions<AuthenticationOptions> options)
    {
        this.accounts = accounts;
        this.passwordHasher = passwordHasher;
        this.clock = clock;
        this.options = options.Value;
    }

    public AuthenticationProviderKind ProviderKind => AuthenticationProviderKind.EmailPassword;

    public Task<AuthenticationProviderResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        if (!accounts.TryGetByEmail(request.Email, out AuthenticationAccount account))
        {
            return Task.FromResult(AuthenticationProviderResult.Failure("invalid_credentials", "Invalid credentials."));
        }

        if (account.State == AccountSecurityState.Disabled)
        {
            return Task.FromResult(AuthenticationProviderResult.Failure("account_disabled", "Account is disabled."));
        }

        if (account.LockedUntilUtc.HasValue && account.LockedUntilUtc.Value > clock.UtcNow)
        {
            return Task.FromResult(AuthenticationProviderResult.Failure("account_locked", "Account is temporarily locked."));
        }

        if (string.IsNullOrEmpty(account.PasswordHash) || !passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
        {
            int failedAttempts = account.FailedAttempts + 1;
            DateTimeOffset? lockedUntil = failedAttempts >= options.MaxFailedAttempts ? clock.UtcNow.Add(options.LockoutDuration) : null;
            accounts.Save(account with { FailedAttempts = failedAttempts, LockedUntilUtc = lockedUntil, State = lockedUntil.HasValue ? AccountSecurityState.Locked : account.State });
            return Task.FromResult(AuthenticationProviderResult.Failure("invalid_credentials", "Invalid credentials."));
        }

        accounts.Save(account with { FailedAttempts = 0, LockedUntilUtc = null, State = AccountSecurityState.Active });
        return Task.FromResult(AuthenticationProviderResult.Success(account));
    }
}
