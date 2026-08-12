namespace BeeKingdom.Persistence.Configuration;

public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";

    public string DatabaseName { get; set; } = "BeeKingdom";
    public string ConnectionStringName { get; set; } = "BeeKingdomDb";
    public string ConnectionString { get; set; } = string.Empty;
    public string RuntimeConnectionStringName { get; set; } = string.Empty;
    public string RuntimeConnectionString { get; set; } = string.Empty;
    public string MigrationConnectionStringName { get; set; } = string.Empty;
    public string MigrationConnectionString { get; set; } = string.Empty;
    public string TablePrefix { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 30;
}
