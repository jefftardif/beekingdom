using Microsoft.Extensions.Configuration;

namespace BeeKingdom.Persistence.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";
    public const string InMemoryProvider = "InMemory";
    public const string SqlServerProvider = "SqlServer";

    public string Provider { get; set; } = InMemoryProvider;

    public static bool UsesSqlServer(IConfiguration configuration)
    {
        string? provider = configuration.GetSection(SectionName).Get<PersistenceOptions>()?.Provider;
        return string.Equals(provider, SqlServerProvider, StringComparison.OrdinalIgnoreCase);
    }
}
