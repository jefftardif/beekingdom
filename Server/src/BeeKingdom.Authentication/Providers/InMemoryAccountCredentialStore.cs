using System.Linq;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Providers;

public sealed class InMemoryAccountCredentialStore : IAccountCredentialStore
{
    private readonly Dictionary<string, AuthenticationAccount> accountsByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AuthenticationAccount> accountsByGoogleSubjectId = new(StringComparer.Ordinal);
    private readonly IPasswordHasher passwordHasher;
    private readonly object sync = new();

    public InMemoryAccountCredentialStore(IPasswordHasher passwordHasher)
    {
        this.passwordHasher = passwordHasher;
    }

    public AuthenticationAccount CreateEmailAccount(string email, string password)
    {
        AuthenticationAccount account = new(
            Guid.NewGuid(),
            PlayerId.New(),
            email.Trim(),
            passwordHasher.HashPassword(password),
            AccountSecurityState.Active,
            0,
            null);

        lock (sync)
        {
            accountsByEmail[account.Email] = account;
        }

        return account;
    }

    public AuthenticationAccount CreateGoogleAccount(string googleSubjectId, string email)
    {
        AuthenticationAccount account = new(
            Guid.NewGuid(),
            PlayerId.New(),
            email.Trim(),
            null,
            AccountSecurityState.Active,
            0,
            null,
            googleSubjectId);

        lock (sync)
        {
            accountsByEmail[account.Email] = account;
            accountsByGoogleSubjectId[googleSubjectId] = account;
        }

        return account;
    }

    public bool TryGetByEmail(string email, out AuthenticationAccount account)
    {
        lock (sync)
        {
            return accountsByEmail.TryGetValue(email.Trim(), out account!);
        }
    }

    public bool TryGetByGoogleSubjectId(string googleSubjectId, out AuthenticationAccount account)
    {
        lock (sync)
        {
            return accountsByGoogleSubjectId.TryGetValue(googleSubjectId, out account!);
        }
    }

    public bool TryGetByAccountId(Guid accountId, out AuthenticationAccount account)
    {
        lock (sync)
        {
            account = accountsByEmail.Values.FirstOrDefault(a => a.AccountId == accountId)!;
            return account != null;
        }
    }

    public bool IsDisplayNameTaken(Guid worldId, string displayName, Guid excludingAccountId)
    {
        lock (sync)
        {
            return accountsByEmail.Values.Any(a =>
                a.AccountId != excludingAccountId &&
                string.Equals(a.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Save(AuthenticationAccount account)
    {
        lock (sync)
        {
            accountsByEmail[account.Email] = account;
            if (!string.IsNullOrEmpty(account.GoogleSubjectId)) accountsByGoogleSubjectId[account.GoogleSubjectId] = account;
        }
    }
}
