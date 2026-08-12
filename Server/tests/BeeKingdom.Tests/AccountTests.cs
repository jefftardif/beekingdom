using BeeKingdom.Accounts;
using BeeKingdom.Accounts.DependencyInjection;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class AccountTests
{
    [Test]
    public void CreateAccountUsesConfiguredDefaults()
    {
        AccountManager accounts = CreateProvider().GetRequiredService<AccountManager>();

        AccountRecord account = accounts.CreateAccount(new CreateAccountRequest("Queen Bee", "queen@bee.test"));

        Assert.Multiple(() =>
        {
            Assert.That(account.Profile.AccountId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(account.Profile.DisplayName, Is.EqualTo("Queen Bee"));
            Assert.That(account.Profile.Language, Is.EqualTo("fr-CA"));
            Assert.That(account.Settings.Currency, Is.EqualTo("CAD"));
            Assert.That(account.Profile.Status, Is.EqualTo(AccountStatus.PendingVerification));
            Assert.That(accounts.Diagnostics.TotalAccounts, Is.EqualTo(1));
        });
    }

    [Test]
    public void UpdateProfileChangesIdentityFieldsOnly()
    {
        AccountManager accounts = CreateProvider().GetRequiredService<AccountManager>();
        AccountRecord account = accounts.CreateAccount(new CreateAccountRequest("Worker", "worker@bee.test"));

        AccountRecord updated = accounts.UpdateProfile(account.Profile.AccountId, "Worker Prime", "en-US", "America/Toronto", "CA");

        Assert.Multiple(() =>
        {
            Assert.That(updated.Profile.DisplayName, Is.EqualTo("Worker Prime"));
            Assert.That(updated.Profile.Language, Is.EqualTo("en-US"));
            Assert.That(updated.Profile.TimeZone, Is.EqualTo("America/Toronto"));
            Assert.That(updated.Profile.Country, Is.EqualTo("CA"));
        });
    }

    [Test]
    public void UpdatePreferencesStoresExtensiblePreferences()
    {
        AccountManager accounts = CreateProvider().GetRequiredService<AccountManager>();
        AccountRecord account = accounts.CreateAccount(new CreateAccountRequest("Builder", "builder@bee.test"));
        AccountPreferences preferences = new("fr-CA", false, true, "High", 0.5d, false, new Dictionary<string, string> { ["ui.theme"] = "dark" });

        AccountRecord updated = accounts.UpdatePreferences(account.Profile.AccountId, preferences);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Preferences, Is.EqualTo(preferences));
            Assert.That(updated.Preferences.Extensions["ui.theme"], Is.EqualTo("dark"));
        });
    }

    [Test]
    public void SuspendAndReactivateAccountUseValidatedTransitions()
    {
        AccountManager accounts = CreateProvider().GetRequiredService<AccountManager>();
        AccountRecord account = accounts.CreateAccount(new CreateAccountRequest("Guard", "guard@bee.test"));

        AccountRecord suspended = accounts.SuspendAccount(account.Profile.AccountId);
        AccountRecord active = accounts.ReactivateAccount(account.Profile.AccountId);

        Assert.Multiple(() =>
        {
            Assert.That(suspended.Profile.Status, Is.EqualTo(AccountStatus.Suspended));
            Assert.That(active.Profile.Status, Is.EqualTo(AccountStatus.Active));
        });
    }

    [Test]
    public void DeletedAccountCannotBeReactivated()
    {
        AccountManager accounts = CreateProvider().GetRequiredService<AccountManager>();
        AccountRecord account = accounts.CreateAccount(new CreateAccountRequest("Scout", "scout@bee.test"));

        accounts.DeleteAccount(account.Profile.AccountId);

        Assert.Throws<InvalidOperationException>(() => accounts.ReactivateAccount(account.Profile.AccountId));
    }

    [Test]
    public void QueryAccountFiltersByStatus()
    {
        AccountManager accounts = CreateProvider().GetRequiredService<AccountManager>();
        AccountRecord first = accounts.CreateAccount(new CreateAccountRequest("One", "one@bee.test"));
        accounts.CreateAccount(new CreateAccountRequest("Two", "two@bee.test"));
        accounts.SuspendAccount(first.Profile.AccountId);

        IReadOnlyList<AccountRecord> suspended = accounts.QueryAccount(new AccountQuery(Status: AccountStatus.Suspended));

        Assert.That(suspended, Has.Count.EqualTo(1));
        Assert.That(suspended[0].Profile.Email, Is.EqualTo("one@bee.test"));
    }

    private static ServiceProvider CreateProvider()
    {
        Dictionary<string, string?> values = new()
        {
            ["Accounts:DefaultLanguage"] = "fr-CA",
            ["Accounts:DefaultTimeZone"] = "America/Toronto",
            ["Accounts:DefaultCountry"] = "CA",
            ["Accounts:DefaultCurrency"] = "CAD"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomAccounts(configuration)
            .BuildServiceProvider();
    }
}
