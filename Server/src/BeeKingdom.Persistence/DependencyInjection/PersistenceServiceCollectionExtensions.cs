using BeeKingdom.Persistence.Abstractions;
using BeeKingdom.Persistence.Backups;
using BeeKingdom.Persistence.Configuration;
using BeeKingdom.Persistence.Migrations;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Persistence.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Persistence.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .Validate(options =>
                string.Equals(options.Provider, PersistenceOptions.InMemoryProvider, StringComparison.OrdinalIgnoreCase)
                || string.Equals(options.Provider, PersistenceOptions.SqlServerProvider, StringComparison.OrdinalIgnoreCase),
                "Persistence provider must be InMemory or SqlServer.")
            .ValidateOnStart();

        services.AddOptions<SqlServerOptions>()
            .Bind(configuration.GetSection(SqlServerOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName), "DatabaseName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionStringName), "ConnectionStringName is required.")
            .Validate(options => !PersistenceOptions.UsesSqlServer(configuration) || HasRuntimeConnection(configuration, options), "SqlServer requires an external runtime connection string.")
            .Validate(options => !PersistenceOptions.UsesSqlServer(configuration) || HasMigrationConnection(configuration, options), "SqlServer requires an external migration connection string.")
            .Validate(options => options.CommandTimeoutSeconds > 0, "CommandTimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<IUnitOfWorkFactory, NoopUnitOfWorkFactory>();
        services.AddSingleton<SqlConnectionFactory>();
        services.AddSingleton<MigrationDiagnostics>();
        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<IMigrationRunner, SqlServerMigrationRunner>();
        }
        else
        {
            services.AddSingleton<IMigrationRunner, ScriptedMigrationRunner>();
        }

        services.AddSingleton<IBackupService, LoggingBackupService>();

        return services;
    }

    private static bool HasRuntimeConnection(IConfiguration configuration,SqlServerOptions options)
        => HasNamedConnection(configuration,options.RuntimeConnectionStringName)
           || !string.IsNullOrWhiteSpace(options.RuntimeConnectionString)
           || HasNamedConnection(configuration,options.ConnectionStringName)
           || !string.IsNullOrWhiteSpace(options.ConnectionString);

    private static bool HasMigrationConnection(IConfiguration configuration,SqlServerOptions options)
        => HasNamedConnection(configuration,options.MigrationConnectionStringName)
           || !string.IsNullOrWhiteSpace(options.MigrationConnectionString)
           || HasNamedConnection(configuration,options.ConnectionStringName)
           || !string.IsNullOrWhiteSpace(options.ConnectionString);

    private static bool HasNamedConnection(IConfiguration configuration,string? name)
        => !string.IsNullOrWhiteSpace(name)&&!string.IsNullOrWhiteSpace(configuration.GetConnectionString(name));
}
