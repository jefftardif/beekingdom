namespace BeeKingdom.Server;

public sealed record WorldMapContentManifestResponse(
    string ContractVersion,
    string Channel,
    string Version,
    string Platform,
    string MinimumAppVersion,
    IReadOnlyList<WorldMapBundleManifest> Bundles);

public sealed record WorldMapBundleManifest(string BundleId, long SizeBytes, string Sha256, string Uri);
