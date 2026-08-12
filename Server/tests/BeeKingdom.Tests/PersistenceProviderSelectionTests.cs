using BeeKingdom.Accounts.DependencyInjection;
using BeeKingdom.Accounts.Repositories;
using BeeKingdom.Authentication.DependencyInjection;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication.Sessions;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.DependencyInjection;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Colony.DependencyInjection;
using BeeKingdom.Colony.Repositories;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Persistence.DependencyInjection;
using BeeKingdom.Persistence.Migrations;
using BeeKingdom.Persistence.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeeKingdom.Tests;

public sealed class PersistenceProviderSelectionTests
{
    [Test]
    public void InMemoryProviderSelectsInMemoryStores()
    {
        using ServiceProvider provider = BuildProvider("InMemory");

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IMigrationRunner>(), Is.TypeOf<ScriptedMigrationRunner>());
            Assert.That(provider.GetRequiredService<IAccountRepository>(), Is.TypeOf<InMemoryAccountRepository>());
            Assert.That(provider.GetRequiredService<IAccountCredentialStore>(), Is.TypeOf<InMemoryAccountCredentialStore>());
            Assert.That(provider.GetRequiredService<IAuthenticationSessionStore>(), Is.TypeOf<AuthenticationSessionStore>());
            Assert.That(provider.GetRequiredService<IColonyRepository>(), Is.TypeOf<InMemoryColonyRepository>());
            Assert.That(provider.GetRequiredService<IChatRepository>(), Is.TypeOf<InMemoryChatRepository>());
            Assert.That(provider.GetRequiredService<IChatAudienceResolver>(), Is.TypeOf<LocalChatAudienceResolver>());
            Assert.That(provider.GetRequiredService<IChatRealtimeDispatcher>(), Is.TypeOf<SignalRChatRealtimeDispatcher>());
        });
    }

    [Test]
    public void SqlServerProviderSelectsSqlStoresWithoutOpeningRemoteDatabase()
    {
        using ServiceProvider provider = BuildProvider("SqlServer");

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IMigrationRunner>(), Is.TypeOf<SqlServerMigrationRunner>());
            Assert.That(provider.GetRequiredService<IAccountRepository>(), Is.TypeOf<SqlAccountRepository>());
            Assert.That(provider.GetRequiredService<IAccountCredentialStore>(), Is.TypeOf<SqlAccountCredentialStore>());
            Assert.That(provider.GetRequiredService<IAuthenticationSessionStore>(), Is.TypeOf<SqlAuthenticationSessionStore>());
            Assert.That(provider.GetRequiredService<IColonyRepository>(), Is.TypeOf<SqlColonyRepository>());
            Assert.That(provider.GetRequiredService<IChatRepository>(), Is.TypeOf<SqlChatRepository>());
        });
    }

    [Test]
    public void SqlServerOptionsCanSeparateRuntimeAndMigrationIdentities()
    {
        using ServiceProvider provider = BuildProvider("SqlServer", new Dictionary<string, string?>
        {
            ["ConnectionStrings:BeeKingdomRuntime"] = "Server=runtime-host;Database=BeeKingdom;User Id=runtime_user;Password=runtime-secret;TrustServerCertificate=True;",
            ["ConnectionStrings:BeeKingdomMigrations"] = "Server=migration-host;Database=BeeKingdom;User Id=migration_user;Password=migration-secret;TrustServerCertificate=True;",
            ["SqlServer:RuntimeConnectionStringName"] = "BeeKingdomRuntime",
            ["SqlServer:MigrationConnectionStringName"] = "BeeKingdomMigrations"
        });

        SqlConnectionFactory factory = provider.GetRequiredService<SqlConnectionFactory>();

        Assert.Multiple(() =>
        {
            Assert.That(factory.GetRuntimeConnectionString(), Does.Contain("runtime_user"));
            Assert.That(factory.GetMigrationConnectionString(), Does.Contain("migration_user"));
        });
    }

    [Test]
    public void DedicatedConnectionNamesTakePrecedenceOverLegacyFallback()
    {
        using ServiceProvider provider = BuildProvider("SqlServer", new Dictionary<string, string?>
        {
            ["ConnectionStrings:BeeKingdomDb"] = "Server=legacy-host;Database=BeeKingdom;User Id=legacy_user;Password=legacy-secret;TrustServerCertificate=True;",
            ["ConnectionStrings:BeeKingdomRuntime"] = "Server=runtime-host;Database=BeeKingdom;User Id=runtime_user;Password=runtime-secret;TrustServerCertificate=True;",
            ["ConnectionStrings:BeeKingdomMigrations"] = "Server=migration-host;Database=BeeKingdom;User Id=migration_user;Password=migration-secret;TrustServerCertificate=True;",
            ["SqlServer:RuntimeConnectionStringName"] = "BeeKingdomRuntime",
            ["SqlServer:MigrationConnectionStringName"] = "BeeKingdomMigrations"
        });

        SqlConnectionFactory factory = provider.GetRequiredService<SqlConnectionFactory>();

        Assert.Multiple(() =>
        {
            Assert.That(factory.GetRuntimeConnectionString(), Does.Contain("runtime_user"));
            Assert.That(factory.GetRuntimeConnectionString(), Does.Not.Contain("legacy_user"));
            Assert.That(factory.GetMigrationConnectionString(), Does.Contain("migration_user"));
            Assert.That(factory.GetMigrationConnectionString(), Does.Not.Contain("legacy_user"));
        });
    }

    [Test]
    public void MigrationConnectionDoesNotFallBackToDedicatedRuntimeIdentity()
    {
        using ServiceProvider provider = BuildProvider("SqlServer", new Dictionary<string, string?>
        {
            ["ConnectionStrings:BeeKingdomRuntime"] = "Server=runtime-host;Database=BeeKingdom;User Id=runtime_user;Password=runtime-secret;TrustServerCertificate=True;",
            ["SqlServer:RuntimeConnectionStringName"] = "BeeKingdomRuntime",
            ["SqlServer:MigrationConnectionStringName"] = string.Empty,
            ["SqlServer:MigrationConnectionString"] = string.Empty
        });

        SqlConnectionFactory factory = provider.GetRequiredService<SqlConnectionFactory>();

        Assert.Multiple(() =>
        {
            Assert.That(factory.GetRuntimeConnectionString(), Does.Contain("runtime_user"));
            Assert.That(factory.GetMigrationConnectionString(), Does.Contain("MSSQLLocalDB"));
            Assert.That(factory.GetMigrationConnectionString(), Does.Not.Contain("runtime_user"));
        });
    }

    private static ServiceProvider BuildProvider(string persistenceProvider, IReadOnlyDictionary<string, string?>? overrides = null)
    {
        Dictionary<string, string?> values = new()
        {
            ["Persistence:Provider"] = persistenceProvider,
            ["SqlServer:DatabaseName"] = "BeeKingdomTests",
            ["SqlServer:ConnectionStringName"] = "BeeKingdomDb",
            ["SqlServer:ConnectionString"] = "Server=(localdb)\\MSSQLLocalDB;Database=BeeKingdomTests;Trusted_Connection=True;TrustServerCertificate=True;",
            ["SqlServer:CommandTimeoutSeconds"] = "1",
            ["Accounts:DefaultLanguage"] = "en-US",
            ["Accounts:DefaultTimeZone"] = "UTC",
            ["Accounts:DefaultCurrency"] = "USD",
            ["Authentication:AccessTokenLifetime"] = "00:15:00",
            ["Authentication:RefreshTokenLifetime"] = "14.00:00:00",
            ["Authentication:MaxSessionsPerAccount"] = "5",
            ["Authentication:MaxFailedAttempts"] = "5",
            ["Colony:MaxSnapshotBytes"] = "1048576",
            ["Colony:AutoSaveInterval"] = "00:05:00",
            ["Colony:RetentionDays"] = "30",
            ["Chat:Enabled"] = "false",
            ["Chat:RealtimeEnabled"] = "false"
        };

        if (overrides != null)
        {
            foreach (KeyValuePair<string, string?> entry in overrides)
            {
                values[entry.Key] = entry.Value;
            }
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddLogging();
        services
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomPersistence(configuration)
            .AddBeeKingdomAuthentication(configuration)
            .AddBeeKingdomAccounts(configuration)
            .AddBeeKingdomColony(configuration)
            .AddBeeKingdomChat(configuration);

        return services.BuildServiceProvider(validateScopes: true);
    }
}
