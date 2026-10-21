using System.Text.RegularExpressions;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// Public floral-site pilot catalogue. Entries are supplied by server configuration
/// and are not inferred from Unity tiles. OFF by default on every environment.
/// </summary>
public sealed class FloralSiteOptions
{
    public const string SectionName = "FloralSites";
    public bool Enabled { get; set; }
    public string CatalogVersion { get; set; } = "";
    public List<FloralSiteDefinition> Sites { get; set; } = [];

    public void Validate()
    {
        if (!Enabled && Sites.Count == 0) return;
        if (!ValidId(CatalogVersion) || Sites.Count is < 1 or > 128)
            throw new InvalidDataException("Invalid floral site catalogue metadata.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (FloralSiteDefinition site in Sites)
        {
            if (site is null
                || !ValidId(site.WorldId) || !ValidId(site.RegionId)
                || !ValidId(site.SiteId) || !ValidId(site.FamilyId)
                || string.IsNullOrWhiteSpace(site.NameFr)
                || string.IsNullOrWhiteSpace(site.NameEn)
                || site.NameFr.Length > 128 || site.NameEn.Length > 128
                || site.MaximumPollenUnits is < 1 or > 1_000_000_000
                || site.InitialFertilityBp is < 0 or > 10000
                || site.InitialBiodiversityBp is < 0 or > 10000
                || !keys.Add(site.SiteId))
                throw new InvalidDataException("Invalid or duplicated floral site definition.");
        }
    }

    public static bool ValidId(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length is >= 2 and <= 64
        && Regex.IsMatch(value, "^[a-z][a-z0-9._-]*$", RegexOptions.CultureInvariant);
}

public sealed record FloralSiteDefinition(
    string WorldId, string RegionId, string SiteId, string FamilyId,
    string NameFr, string NameEn, long MaximumPollenUnits,
    int InitialFertilityBp, int InitialBiodiversityBp);

/// <summary>
/// Single global copy of the site's persisted state. Not owned by any player.
/// All monetary changes will require cross-aggregate SQL transactions in P2/P3.
/// </summary>
public sealed record FloralSiteState(
    string WorldId, string RegionId, string SiteId, string FamilyId,
    string CatalogVersion, long Revision, long PollenRemaining,
    long PollenCapacity, int FertilityBp, int BiodiversityBp,
    DateTimeOffset LastSettledAtUtc);

public sealed record FloralSiteSnapshot(
    string ContractVersion, string WorldId, string RegionId, string SiteId,
    string FamilyId, string NameFr, string NameEn,
    string CatalogVersion, long Revision, DateTimeOffset ServerTimeUtc,
    long PollenRemaining, long PollenCapacity,
    int FertilityBp, int BiodiversityBp, string Phase,
    bool PublicAccess);

public interface IFloralSiteRepository
{
    Task<FloralSiteState> ReadOrCreateAsync(
        FloralSiteDefinition definition, string catalogVersion,
        DateTimeOffset nowUtc, CancellationToken ct = default);
}

/// <summary>
/// Read-only public metadata and persistent world-state projection.
/// No harvest/expedition endpoint is exposed until hive+site+ledger atomicity
/// is proven. In particular, never create resources from a client Harvest call.
/// </summary>
public sealed class FloralSiteService(
    IFloralSiteRepository repository, IServerClock clock, FloralSiteOptions options)
{
    public const string ContractVersion = "bk-world-floral-site-v1-alpha";
    private readonly FloralSiteOptions settings = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<IReadOnlyList<FloralSiteSnapshot>> ListAsync(CancellationToken ct = default)
    {
        EnsureEnabled();
        var results = new List<FloralSiteSnapshot>(settings.Sites.Count);
        foreach (FloralSiteDefinition site in settings.Sites)
            results.Add(await ReadKnownAsync(site, ct));
        return results;
    }

    public async Task<FloralSiteSnapshot> ReadAsync(
        string siteId, CancellationToken ct = default)
    {
        EnsureEnabled();
        if (!FloralSiteOptions.ValidId(siteId))
            throw new ArgumentException("game.invalid_request");
        FloralSiteDefinition? definition = settings.Sites
            .SingleOrDefault(site => site.SiteId == siteId);
        if (definition is null)
            throw new KeyNotFoundException("game.floral_site_not_found");
        return await ReadKnownAsync(definition, ct);
    }

    private async Task<FloralSiteSnapshot> ReadKnownAsync(
        FloralSiteDefinition definition, CancellationToken ct)
    {
        DateTimeOffset now = clock.UtcNow.ToUniversalTime();
        FloralSiteState state = await repository.ReadOrCreateAsync(
            definition, settings.CatalogVersion, now, ct);
        ValidateStored(state);
        if (!string.Equals(state.WorldId, definition.WorldId, StringComparison.Ordinal)
            || !string.Equals(state.SiteId, definition.SiteId, StringComparison.Ordinal)
            || !string.Equals(state.FamilyId, definition.FamilyId, StringComparison.Ordinal))
            throw new InvalidDataException("Floral site catalogue identity changed.");
        return new FloralSiteSnapshot(
            ContractVersion, state.WorldId, state.RegionId, state.SiteId,
            state.FamilyId, definition.NameFr, definition.NameEn,
            state.CatalogVersion, state.Revision, now, state.PollenRemaining,
            state.PollenCapacity, state.FertilityBp, state.BiodiversityBp,
            "blooming", true);
    }

    public static FloralSiteState InitialState(
        FloralSiteDefinition site, string catalogVersion,
        DateTimeOffset nowUtc) => new(
            site.WorldId, site.RegionId, site.SiteId, site.FamilyId,
            catalogVersion, 0, site.MaximumPollenUnits, site.MaximumPollenUnits,
            site.InitialFertilityBp, site.InitialBiodiversityBp,
            nowUtc.ToUniversalTime());

    public static void ValidateStored(FloralSiteState state)
    {
        if (!FloralSiteOptions.ValidId(state.WorldId)
            || !FloralSiteOptions.ValidId(state.RegionId)
            || !FloralSiteOptions.ValidId(state.SiteId)
            || !FloralSiteOptions.ValidId(state.FamilyId)
            || !FloralSiteOptions.ValidId(state.CatalogVersion)
            || state.Revision < 0 || state.PollenCapacity <= 0
            || state.PollenRemaining < 0 || state.PollenRemaining > state.PollenCapacity
            || state.FertilityBp is < 0 or > 10000
            || state.BiodiversityBp is < 0 or > 10000
            || state.LastSettledAtUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("Invalid persisted shared floral site.");
    }

    private void EnsureEnabled()
    {
        settings.Validate();
        if (!settings.Enabled)
            throw new InvalidOperationException("game.floral_sites_disabled");
    }
}
