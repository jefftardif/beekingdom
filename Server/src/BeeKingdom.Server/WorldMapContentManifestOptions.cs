namespace BeeKingdom.Server;

public sealed class WorldMapContentManifestOptions
{
    public const string SectionName = "WorldMapContentManifest";
    public bool Enabled { get; set; }
    public string Channel { get; set; } = "stable";
    public string Version { get; set; } = "";
    public string Platform { get; set; } = "android";
    public string MinimumAppVersion { get; set; } = "";
    public List<WorldMapBundleOptions> Bundles { get; set; } = [];
}

public sealed class WorldMapBundleOptions
{
    public string BundleId { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
    public string Uri { get; set; } = "";
}
