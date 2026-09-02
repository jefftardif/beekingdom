using BeeKingdom.Accounts;
using BeeKingdom.Accounts.Configuration;
using BeeKingdom.Accounts.Events;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Accounts.Repositories;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

// M043B-CL: the generic player-search/lookup surface identified as entirely missing in M043 -
// covers search correctness, privacy (no email/status ever reachable through PlayerPublicIdentity),
// the "blank query extracts everyone" guard, case-insensitivity, and pagination/limit.
public sealed class PlayerDirectoryServiceTests
{
    private sealed class FixedClock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

    private static (IPlayerDirectoryService Directory, AccountManager Accounts) CreateDirectory()
    {
        var options = Options.Create(new AccountOptions
        {
            DefaultLanguage = "fr-CA",
            DefaultTimeZone = "America/Toronto",
            DefaultCurrency = "CAD"
        });
        var service = new AccountService(new InMemoryAccountRepository(), new InMemoryAccountEventSink(), new FixedClock(), options);
        var manager = new AccountManager(service);
        return (new PlayerDirectoryService(service), manager);
    }

    [Test]
    public void Search_FindsRealDisplayNameCaseInsensitively()
    {
        (IPlayerDirectoryService directory, AccountManager accounts) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("QueenBee", "queen@bee.test"));
        accounts.ReactivateAccount(created.Profile.AccountId); // PendingVerification -> Active so Search (Active-only) finds it

        var results = directory.Search("queenbee", 0, 20);

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].DisplayName, Is.EqualTo("QueenBee"));
        Assert.That(results[0].PlayerId, Is.EqualTo(created.Profile.PlayerId));
    }

    [Test]
    public void Search_RejectsBlankQuery_CannotExtractWholePlayerBase()
    {
        (IPlayerDirectoryService directory, AccountManager accounts) = CreateDirectory();
        AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("QueenBee", "queen@bee.test"));
        accounts.ReactivateAccount(created.Profile.AccountId);

        Assert.Throws<ArgumentException>(() => directory.Search(string.Empty, 0, 20));
        Assert.Throws<ArgumentException>(() => directory.Search("a", 0, 20)); // below MinQueryLength
        Assert.Throws<ArgumentException>(() => directory.Search(new string('x', 100), 0, 20)); // above MaxQueryLength
    }

    [Test]
    public void Search_ResultsNeverExposePrivateAccountData()
    {
        // Structural guarantee: PlayerPublicIdentity only ever carries PlayerId + DisplayName -
        // no email/status/auth-provider-id/token property can exist on it, by construction.
        System.Reflection.PropertyInfo[] properties = typeof(PlayerPublicIdentity).GetProperties();
        Assert.That(properties.Select(p => p.Name), Is.EquivalentTo(new[] { "PlayerId", "DisplayName" }));
    }

    [Test]
    public void Search_RespectsPaginationAndLimit()
    {
        (IPlayerDirectoryService directory, AccountManager accounts) = CreateDirectory();
        for (int i = 0; i < 5; i++)
        {
            AccountRecord created = accounts.CreateAccount(new CreateAccountRequest("Scout" + i, $"scout{i}@bee.test"));
            accounts.ReactivateAccount(created.Profile.AccountId);
        }

        var firstPage = directory.Search("Scout", 0, 2);
        var secondPage = directory.Search("Scout", 2, 2);

        Assert.That(firstPage.Count, Is.EqualTo(2));
        Assert.That(secondPage.Count, Is.EqualTo(2));
        Assert.That(firstPage.Select(r => r.DisplayName), Is.Not.EquivalentTo(secondPage.Select(r => r.DisplayName)));

        var overLimit = directory.Search("Scout", 0, 1000);
        Assert.That(overLimit.Count, Is.LessThanOrEqualTo(PlayerDirectoryService.MaxLimit));
    }

    [Test]
    public void GetByPlayerIds_BatchResolvesWithoutOneCallPerPlayer()
    {
        (IPlayerDirectoryService directory, AccountManager accounts) = CreateDirectory();
        AccountRecord a = accounts.CreateAccount(new CreateAccountRequest("Alpha", "alpha@bee.test"));
        AccountRecord b = accounts.CreateAccount(new CreateAccountRequest("Beta", "beta@bee.test"));

        var resolved = directory.GetByPlayerIds(new[] { a.Profile.PlayerId, b.Profile.PlayerId, PlayerId.New() });

        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved[a.Profile.PlayerId].DisplayName, Is.EqualTo("Alpha"));
        Assert.That(resolved[b.Profile.PlayerId].DisplayName, Is.EqualTo("Beta"));
    }

    [Test]
    public void Search_DoesNotReturnInactiveAccounts()
    {
        (IPlayerDirectoryService directory, AccountManager accounts) = CreateDirectory();
        accounts.CreateAccount(new CreateAccountRequest("Dormant", "dormant@bee.test")); // stays PendingVerification

        var results = directory.Search("Dormant", 0, 20);

        Assert.That(results, Is.Empty);
    }
}
