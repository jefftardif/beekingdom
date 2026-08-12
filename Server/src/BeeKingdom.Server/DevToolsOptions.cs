namespace BeeKingdom.Server;

// Local-development-only helpers (e.g. seeding a test login account) with no HTTP path to
// create one otherwise. Always false unless explicitly set in appsettings.Development.json —
// never enable in a deployed/production configuration.
public sealed class DevToolsOptions
{
    public const string SectionName = "DevTools";

    public bool AllowDevAccountSeeding { get; set; }
}
