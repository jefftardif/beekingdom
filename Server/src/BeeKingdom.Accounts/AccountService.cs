using System.Diagnostics;
using BeeKingdom.Accounts.Configuration;
using BeeKingdom.Accounts.Diagnostics;
using BeeKingdom.Accounts.Events;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Accounts.Repositories;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Accounts;

public interface IAccountService
{
    AccountDiagnostics Diagnostics { get; }
    AccountRecord CreateAccount(CreateAccountRequest request);
    AccountRecord? GetAccount(Guid accountId);
    AccountRecord? GetAccountByPlayerId(PlayerId playerId);
    AccountRecord UpdateProfile(Guid accountId, string displayName, string? language, string? timeZone, string? country);
    AccountRecord UpdatePreferences(Guid accountId, AccountPreferences preferences);
    AccountRecord SuspendAccount(Guid accountId);
    AccountRecord ReactivateAccount(Guid accountId);
    AccountRecord DeleteAccount(Guid accountId);
    IReadOnlyList<AccountRecord> QueryAccount(AccountQuery query);
}

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository repository;
    private readonly IAccountEventSink eventSink;
    private readonly IServerClock clock;
    private readonly AccountOptions options;

    public AccountService(IAccountRepository repository, IAccountEventSink eventSink, IServerClock clock, IOptions<AccountOptions> options)
    {
        this.repository = repository;
        this.eventSink = eventSink;
        this.clock = clock;
        this.options = options.Value;
    }

    public AccountDiagnostics Diagnostics { get; } = new();

    public AccountRecord CreateAccount(CreateAccountRequest request)
    {
        long start = Stopwatch.GetTimestamp();
        AccountProfile profile = new(
            Guid.NewGuid(),
            PlayerId.New(),
            request.DisplayName,
            request.Email,
            request.Language ?? options.DefaultLanguage,
            request.TimeZone ?? options.DefaultTimeZone,
            request.Country ?? options.DefaultCountry,
            clock.UtcNow,
            null,
            AccountStatus.PendingVerification);

        AccountRecord account = new(profile, new AccountSettings(options.DefaultCurrency, true, true), CreateDefaultPreferences(profile.Language), CreateEmptyProgression());
        repository.Create(account);
        Diagnostics.RecordCreated(Stopwatch.GetTimestamp() - start);
        RefreshCounts();
        eventSink.Publish(new AccountCreated(clock.UtcNow, profile.AccountId, profile.PlayerId));
        return account;
    }

    public AccountRecord? GetAccount(Guid accountId) => repository.Get(accountId);
    public AccountRecord? GetAccountByPlayerId(PlayerId playerId) => repository.GetByPlayerId(playerId);

    public AccountRecord UpdateProfile(Guid accountId, string displayName, string? language, string? timeZone, string? country)
    {
        long start = Stopwatch.GetTimestamp();
        AccountRecord account = RequireAccount(accountId);
        AccountProfile profile = account.Profile with
        {
            DisplayName = displayName,
            Language = language ?? account.Profile.Language,
            TimeZone = timeZone ?? account.Profile.TimeZone,
            Country = country ?? account.Profile.Country
        };

        AccountRecord updated = repository.Save(account with { Profile = profile });
        Diagnostics.RecordUpdated(Stopwatch.GetTimestamp() - start);
        eventSink.Publish(new AccountUpdated(clock.UtcNow, profile.AccountId, profile.PlayerId));
        return updated;
    }

    public AccountRecord UpdatePreferences(Guid accountId, AccountPreferences preferences)
    {
        long start = Stopwatch.GetTimestamp();
        AccountRecord account = RequireAccount(accountId);
        AccountRecord updated = repository.Save(account with { Preferences = preferences });
        Diagnostics.RecordUpdated(Stopwatch.GetTimestamp() - start);
        eventSink.Publish(new PreferencesChanged(clock.UtcNow, account.Profile.AccountId, account.Profile.PlayerId));
        return updated;
    }

    public AccountRecord SuspendAccount(Guid accountId) => ChangeStatus(accountId, AccountStatus.Suspended);
    public AccountRecord ReactivateAccount(Guid accountId) => ChangeStatus(accountId, AccountStatus.Active);
    public AccountRecord DeleteAccount(Guid accountId) => ChangeStatus(accountId, AccountStatus.Deleted);
    public IReadOnlyList<AccountRecord> QueryAccount(AccountQuery query) => repository.Query(query);

    private AccountRecord ChangeStatus(Guid accountId, AccountStatus targetStatus)
    {
        long start = Stopwatch.GetTimestamp();
        AccountRecord account = RequireAccount(accountId);
        if (!CanTransition(account.Profile.Status, targetStatus))
        {
            throw new InvalidOperationException($"Invalid account status transition from {account.Profile.Status} to {targetStatus}.");
        }

        AccountRecord updated = repository.Save(account with { Profile = account.Profile with { Status = targetStatus } });
        Diagnostics.RecordUpdated(Stopwatch.GetTimestamp() - start);
        RefreshCounts();
        PublishStatusEvent(updated);
        return updated;
    }

    private AccountRecord RequireAccount(Guid accountId)
    {
        return repository.Get(accountId) ?? throw new KeyNotFoundException($"Account {accountId} was not found.");
    }

    private static bool CanTransition(AccountStatus current, AccountStatus target)
    {
        if (current == AccountStatus.Deleted)
        {
            return false;
        }

        return target switch
        {
            AccountStatus.Active => current is AccountStatus.PendingVerification or AccountStatus.Suspended,
            AccountStatus.Suspended => current is AccountStatus.Active or AccountStatus.PendingVerification,
            AccountStatus.Banned => current is AccountStatus.Active or AccountStatus.Suspended,
            AccountStatus.Deleted => current is not AccountStatus.Deleted,
            AccountStatus.PendingVerification => false,
            _ => false
        };
    }

    private void PublishStatusEvent(AccountRecord account)
    {
        IAccountEvent accountEvent = account.Profile.Status switch
        {
            AccountStatus.Suspended => new AccountSuspended(clock.UtcNow, account.Profile.AccountId, account.Profile.PlayerId),
            AccountStatus.Active => new AccountReactivated(clock.UtcNow, account.Profile.AccountId, account.Profile.PlayerId),
            AccountStatus.Deleted => new AccountDeleted(clock.UtcNow, account.Profile.AccountId, account.Profile.PlayerId),
            _ => new AccountUpdated(clock.UtcNow, account.Profile.AccountId, account.Profile.PlayerId)
        };
        eventSink.Publish(accountEvent);
    }

    private void RefreshCounts()
    {
        IReadOnlyList<AccountRecord> all = repository.Query(new AccountQuery());
        Diagnostics.SetStatusCounts(all.Count(account => account.Profile.Status == AccountStatus.Active), all.Count(account => account.Profile.Status == AccountStatus.Suspended));
    }

    private AccountPreferences CreateDefaultPreferences(string language) => new(language, true, false, "Auto", 1d, true, new Dictionary<string, string>());

    private static AccountProgression CreateEmptyProgression() => new(new HashSet<string>(), new Dictionary<string, double>(), new HashSet<string>(), Array.Empty<string>(), Array.Empty<string>());
}
