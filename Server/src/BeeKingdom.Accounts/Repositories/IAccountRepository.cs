using BeeKingdom.Accounts.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Accounts.Repositories;

public interface IAccountRepository
{
    AccountRecord Create(AccountRecord account);
    AccountRecord? Get(Guid accountId);
    AccountRecord? GetByPlayerId(PlayerId playerId);
    AccountRecord? GetByEmail(string email);
    AccountRecord Save(AccountRecord account);
    IReadOnlyList<AccountRecord> Query(AccountQuery query);
}
