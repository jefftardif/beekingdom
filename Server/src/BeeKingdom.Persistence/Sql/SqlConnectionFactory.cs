using BeeKingdom.Persistence.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Persistence.Sql;

public sealed class SqlConnectionFactory
{
    private readonly IConfiguration configuration;
    private readonly SqlServerOptions options;

    public SqlConnectionFactory(IConfiguration configuration, IOptions<SqlServerOptions> options)
    {
        this.configuration = configuration;
        this.options = options.Value;
    }

    public string GetConnectionString()
    {
        return GetRuntimeConnectionString();
    }

    public string GetRuntimeConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(options.RuntimeConnectionStringName))
        {
            string? dedicated = configuration.GetConnectionString(options.RuntimeConnectionStringName);
            if (!string.IsNullOrWhiteSpace(dedicated))
            {
                return dedicated;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.RuntimeConnectionString))
        {
            return options.RuntimeConnectionString;
        }

        string? configured = configuration.GetConnectionString(options.ConnectionStringName);
        return string.IsNullOrWhiteSpace(configured) ? options.ConnectionString : configured;
    }

    public string GetMigrationConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(options.MigrationConnectionStringName))
        {
            string? dedicated = configuration.GetConnectionString(options.MigrationConnectionStringName);
            if (!string.IsNullOrWhiteSpace(dedicated))
            {
                return dedicated;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.MigrationConnectionString))
        {
            return options.MigrationConnectionString;
        }

        string? configured = configuration.GetConnectionString(options.ConnectionStringName);
        return string.IsNullOrWhiteSpace(configured) ? options.ConnectionString : configured;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(GetRuntimeConnectionString());
    }

    public SqlConnection CreateMigrationConnection()
    {
        return new SqlConnection(GetMigrationConnectionString());
    }

    public SqlConnection CreateMasterConnection()
    {
        SqlConnectionStringBuilder builder = new(GetMigrationConnectionString())
        {
            InitialCatalog = "master"
        };

        return new SqlConnection(builder.ConnectionString);
    }

    public string GetDatabaseName()
    {
        SqlConnectionStringBuilder builder = new(GetMigrationConnectionString());
        return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? options.DatabaseName : builder.InitialCatalog;
    }
}
