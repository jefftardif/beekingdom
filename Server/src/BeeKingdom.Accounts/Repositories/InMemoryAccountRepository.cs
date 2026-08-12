using BeeKingdom.Accounts.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Accounts.Repositories;

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<Guid, AccountRecord> accountsById = new();
    private readonly Dictionary<PlayerId, Guid> accountIdByPlayerId = new();
    private readonly Dictionary<string, Guid> accountIdByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly object sync = new();

    public AccountRecord Create(AccountRecord account)
    {
        lock (sync)
        {
            if (accountIdByEmail.ContainsKey(account.Profile.Email))
            {
                throw new InvalidOperationException("Account email already exists.");
            }

            accountsById[account.Profile.AccountId] = account;
            accountIdByPlayerId[account.Profile.PlayerId] = account.Profile.AccountId;
            accountIdByEmail[account.Profile.Email] = account.Profile.AccountId;
            return account;
        }
    }

    public AccountRecord? Get(Guid accountId)
    {
        lock (sync)
        {
            return accountsById.TryGetValue(accountId, out AccountRecord? account) ? account : null;
        }
    }

    public AccountRecord? GetByPlayerId(PlayerId playerId)
    {
        lock (sync)
        {
            return accountIdByPlayerId.TryGetValue(playerId, out Guid accountId) ? Get(accountId) : null;
        }
    }

    public AccountRecord? GetByEmail(string email)
    {
        lock (sync)
        {
            return accountIdByEmail.TryGetValue(email, out Guid accountId) ? Get(accountId) : null;
        }
    }

    public AccountRecord Save(AccountRecord account)
    {
        lock (sync)
        {
            accountsById[account.Profile.AccountId] = account;
            accountIdByPlayerId[account.Profile.PlayerId] = account.Profile.AccountId;
            accountIdByEmail[account.Profile.Email] = account.Profile.AccountId;
            return account;
        }
    }

    public IReadOnlyList<AccountRecord> Query(AccountQuery query)
    {
        lock (sync)
        {
            IEnumerable<AccountRecord> values = accountsById.Values;
            if (!string.IsNullOrWhiteSpace(query.Email))
            {
                values = values.Where(account => string.Equals(account.Profile.Email, query.Email, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Status.HasValue)
            {
                values = values.Where(account => account.Profile.Status == query.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.DisplayNameContains))
            {
                values = values.Where(account => account.Profile.DisplayName.Contains(query.DisplayNameContains, StringComparison.OrdinalIgnoreCase));
            }

            return values.OrderBy(account => account.Profile.CreationDate).ToArray();
        }
    }
}
