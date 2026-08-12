using System.Text.Json.Serialization;
namespace BeeKingdom.HiveOperations;

public sealed record HiveStockSnapshot(
    Guid PlayerId, Guid HiveId, string ContractVersion, string CatalogVersion, long Revision, DateTimeOffset ServerTimeUtc,
    ResourceStockSnapshot Honey, ResourceStockSnapshot Wax, ResourceStockSnapshot Pollen,
    long? Population, long? PopulationCapacity,
    IReadOnlyList<string> CompletedResearchIds,
    IReadOnlyList<HiveEngagementSnapshot> ActiveEngagements)
{
    [JsonIgnore] public ResourceStockSnapshot HoneyValue => Honey;
    [JsonIgnore] public ResourceStockSnapshot WaxValue => Wax;
    [JsonIgnore] public ResourceStockSnapshot PollenValue => Pollen;
    public IReadOnlyDictionary<string, ResourceStockSnapshot> Resources => new Dictionary<string, ResourceStockSnapshot>(StringComparer.Ordinal) { ["honey"] = Honey, ["wax"] = Wax, ["pollen"] = Pollen };
}

public sealed record ResourceStockSnapshot(long Amount, long Capacity);
public sealed record HiveEngagementSnapshot(Guid OperationId, string Kind, string Key, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc);

public static class HiveStockSnapshotFactory
{
    public const string ContractVersion = "living-hive-stock-v1";
    public static HiveStockSnapshot FromAuthoritativeState(PlayerHiveState state, string catalogVersion, DateTimeOffset serverTimeUtc)
    {
        if (state is null || state.PlayerId == Guid.Empty || state.HiveId == Guid.Empty || !ValidToken(catalogVersion) || serverTimeUtc == default || serverTimeUtc.Offset != TimeSpan.Zero || state.Revision < 0 || state.Resources is null || state.Operations is null || state.Research?.Completed is null) throw new InvalidDataException("Invalid hive stock state");
        var completed = state.Research!.Completed;
        ResourceStockSnapshot Stock(string key) { if (state.Resources.Count != 3 || !state.Resources.TryGetValue(key, out var value) || value.Amount < 0 || value.Capacity < value.Amount) throw new InvalidDataException("Invalid resource balance"); return new(value.Amount, value.Capacity); }
        List<HiveEngagementSnapshot> active = state.Operations
            .Where(x => x.Status != HiveOperationStatus.Collected)
            .Select(x => new HiveEngagementSnapshot(x.OperationId, x.Kind.ToString(), x.BuildingKey, x.StartedAtUtc, x.CompletesAtUtc)).ToList();
        if (state.Research?.ActiveOperation is ResearchOperation research)
            active.Add(new(research.OperationId, "Research", research.ResearchId, research.StartedAtUtc, research.EndsAtUtc));
        if (!state.Resources.Keys.OrderBy(x=>x).SequenceEqual(new[]{"honey","pollen","wax"}) || active.Count > 64 || active.Select(x=>x.OperationId).Distinct().Count()!=active.Count || active.Any(x => x.OperationId == Guid.Empty || (x.Kind != "BuildingUpgrade" && x.Kind != "Production" && x.Kind != "Training" && x.Kind != "Research") || !ValidToken(x.Key) || x.StartedAtUtc == default || x.EndsAtUtc == default || x.StartedAtUtc.Offset != TimeSpan.Zero || x.EndsAtUtc.Offset != TimeSpan.Zero || x.StartedAtUtc > serverTimeUtc || x.EndsAtUtc <= x.StartedAtUtc || x.EndsAtUtc - x.StartedAtUtc > TimeSpan.FromDays(30)) || completed.Keys.Count > 64 || completed.Keys.Any(x=>!ValidToken(x)) || completed.Keys.Distinct(StringComparer.Ordinal).Count()!=completed.Keys.Count) throw new InvalidDataException("Invalid engagements");
        return new(state.PlayerId, state.HiveId, ContractVersion, catalogVersion, state.Revision, serverTimeUtc, Stock("honey"), Stock("wax"), Stock("pollen"), null, null,
            completed.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray(), active);
    }
    private static bool ValidToken(string? value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim() && value.Length <= 64 && value.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c is '.' or '_' or '-');
}

public sealed class HiveStockSnapshotOptions
{
    public const string SectionName = "HiveStockSnapshot";
    public bool Enabled { get; set; }
    public string CatalogVersion { get; set; } = string.Empty;
    public void Validate() { if (!Enabled) return; if (!ValidToken(CatalogVersion)) throw new InvalidDataException("Invalid stock catalog"); }
    private static bool ValidToken(string? value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim() && value.Length <= 64 && value.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c is '.' or '_' or '-');
}
