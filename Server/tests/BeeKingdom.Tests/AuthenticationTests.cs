using BeeKingdom.Authentication;
using BeeKingdom.Authentication.DependencyInjection;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class AuthenticationTests
{
    [Test]
    public async Task AuthenticateCreatesSessionAndTokens()
    {
        ServiceProvider provider = CreateProvider();
        IAccountCredentialStore accounts = provider.GetRequiredService<IAccountCredentialStore>();
        accounts.CreateEmailAccount("queen@bee.test", "secret");

        AuthenticationManager authentication = provider.GetRequiredService<AuthenticationManager>();
        AuthenticationResult result = await authentication.Authenticate(CreateRequest("queen@bee.test", "secret"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Session, Is.Not.Null);
            Assert.That(result.Tokens, Is.Not.Null);
            Assert.That(result.Session!.SessionId, Is.Not.Empty);
            Assert.That(result.Tokens!.AccessToken, Is.Not.Empty);
            Assert.That(result.Tokens.RefreshToken, Is.Not.Empty);
        });
    }

    [Test]
    public async Task ValidateTokenAcceptsActiveAccessToken()
    {
        ServiceProvider provider = CreateProvider();
        provider.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount("worker@bee.test", "secret");

        AuthenticationManager authentication = provider.GetRequiredService<AuthenticationManager>();
        AuthenticationResult result = await authentication.Authenticate(CreateRequest("worker@bee.test", "secret"));
        TokenValidationResult validation = authentication.ValidateToken(result.Tokens!.AccessToken);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.True);
            Assert.That(validation.PlayerId, Is.EqualTo(result.PlayerId));
            Assert.That(validation.SessionId, Is.EqualTo(result.Session!.SessionId));
        });
    }

    [Test]
    public void NewAccountDefaultsToPlayerRole()
    {
        IAccountCredentialStore accounts = CreateProvider().GetRequiredService<IAccountCredentialStore>();
        AuthenticationAccount account = accounts.CreateEmailAccount("drone@bee.test", "secret");

        Assert.That(account.Role, Is.EqualTo(AccountRole.Player));
    }

    [Test]
    public void SaveRolePromotesAndDemotesAccount()
    {
        IAccountCredentialStore accounts = CreateProvider().GetRequiredService<IAccountCredentialStore>();
        AuthenticationAccount account = accounts.CreateEmailAccount("sentinel@bee.test", "secret");

        accounts.Save(account with { Role = AccountRole.Moderator });
        accounts.TryGetByAccountId(account.AccountId, out AuthenticationAccount promoted);

        accounts.Save(promoted with { Role = AccountRole.Player });
        accounts.TryGetByAccountId(account.AccountId, out AuthenticationAccount demoted);

        Assert.Multiple(() =>
        {
            Assert.That(promoted.Role, Is.EqualTo(AccountRole.Moderator));
            Assert.That(demoted.Role, Is.EqualTo(AccountRole.Player));
        });
    }

    [Test]
    public void SearchByDisplayNameFindsMatchingAccountsOnly()
    {
        IAccountCredentialStore accounts = CreateProvider().GetRequiredService<IAccountCredentialStore>();
        AuthenticationAccount scout = accounts.CreateEmailAccount("scout-role@bee.test", "secret");
        accounts.Save(scout with { DisplayName = "Scarlet Scout" });
        AuthenticationAccount guard = accounts.CreateEmailAccount("guard-role@bee.test", "secret");
        accounts.Save(guard with { DisplayName = "Golden Guard" });

        IReadOnlyList<AuthenticationAccount> results = accounts.SearchByDisplayName("scout");

        Assert.That(results.Select(a => a.AccountId), Is.EquivalentTo(new[] { scout.AccountId }));
    }

    [Test]
    public async Task RefreshTokenRotatesRefreshToken()
    {
        ServiceProvider provider = CreateProvider();
        provider.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount("builder@bee.test", "secret");

        AuthenticationManager authentication = provider.GetRequiredService<AuthenticationManager>();
        AuthenticationResult result = await authentication.Authenticate(CreateRequest("builder@bee.test", "secret"));
        AuthenticationTokenPair? rotated = await authentication.RefreshToken(result.Tokens!.RefreshToken);
        AuthenticationTokenPair? reused = await authentication.RefreshToken(result.Tokens.RefreshToken);

        Assert.Multiple(() =>
        {
            Assert.That(rotated, Is.Not.Null);
            Assert.That(rotated!.RefreshToken, Is.Not.EqualTo(result.Tokens.RefreshToken));
            Assert.That(reused, Is.Null);
        });
    }

    [Test]
    public async Task RevokedAccessTokenIsRejected()
    {
        ServiceProvider provider = CreateProvider();
        provider.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount("guard@bee.test", "secret");

        AuthenticationManager authentication = provider.GetRequiredService<AuthenticationManager>();
        AuthenticationResult result = await authentication.Authenticate(CreateRequest("guard@bee.test", "secret"));
        bool revoked = authentication.RevokeToken(result.Tokens!.AccessToken);
        TokenValidationResult validation = authentication.ValidateToken(result.Tokens.AccessToken);

        Assert.Multiple(() =>
        {
            Assert.That(revoked, Is.True);
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.ErrorCode, Is.EqualTo("token_revoked"));
        });
    }

    [Test]
    public async Task FailedAttemptsLockAccountTemporarily()
    {
        ServiceProvider provider = CreateProvider(maxFailedAttempts: 2);
        provider.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount("scout@bee.test", "secret");

        AuthenticationManager authentication = provider.GetRequiredService<AuthenticationManager>();
        await authentication.Authenticate(CreateRequest("scout@bee.test", "bad"));
        AuthenticationResult secondFailure = await authentication.Authenticate(CreateRequest("scout@bee.test", "bad"));
        AuthenticationResult lockedAttempt = await authentication.Authenticate(CreateRequest("scout@bee.test", "secret"));

        Assert.Multiple(() =>
        {
            Assert.That(secondFailure.Succeeded, Is.False);
            Assert.That(lockedAttempt.Succeeded, Is.False);
            Assert.That(lockedAttempt.ErrorCode, Is.EqualTo("account_locked"));
        });
    }

    private static AuthenticationRequest CreateRequest(string email, string password)
    {
        return new AuthenticationRequest(email, password, "1.0.0", "127.0.0.1", "device-tests", "local");
    }

    private static ServiceProvider CreateProvider(int maxFailedAttempts = 5)
    {
        Dictionary<string, string?> values = new()
        {
            ["Authentication:AccessTokenLifetime"] = "00:15:00",
            ["Authentication:RefreshTokenLifetime"] = "14.00:00:00",
            ["Authentication:MaxSessionsPerAccount"] = "5",
            ["Authentication:MaxFailedAttempts"] = maxFailedAttempts.ToString(),
            ["Authentication:LockoutDuration"] = "00:10:00",
            ["Authentication:MinimumClientVersion"] = "1.0.0"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomAuthentication(configuration)
            .BuildServiceProvider();
    }
}
