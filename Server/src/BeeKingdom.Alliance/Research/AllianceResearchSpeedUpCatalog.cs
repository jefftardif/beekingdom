namespace BeeKingdom.Alliance.Research;

// M052-CL: Bible section 12 - "Accélérateur de Recherche d'Alliance" is an explicitly DISTINCT
// category from personal SpeedUps (BeeKingdom.HiveOperations.SpeedUpCategories: Universal/
// Construction/Research/Training/Healing/Manufacturing - all scoped to a single PlayerHiveState's
// own operations via OperationTimerReduction). Alliance Research's timer lives on the shared
// AllianceResearchState instead, so it cannot reuse that machinery without contaminating it (the
// mission's own explicit instruction) - this is a small, separate catalog with its own item ids,
// still stored in the same generic per-player inventory shape (PlayerHiveState.SpeedUps,
// Dictionary<string,int> itemId -> quantity) since that dictionary is already itemId-agnostic.
//
// Acquisition (shop purchase, event rewards) is explicitly out of M052's scope (mission: "Do NOT
// implement real-money purchasing... Do NOT implement event rewards") - items are only ever
// consumed here, never granted by production code yet. Tests seed PlayerHiveState.SpeedUps
// directly, the same convention CombatPatrolService's own un-acquirable "combat_recall_token"
// item already established.
public static class AllianceResearchSpeedUpCatalog
{
    public const string Category = "alliance_research";

    public sealed record ItemDefinition(string ItemId, TimeSpan Reduction);

    public static readonly IReadOnlyList<ItemDefinition> Items = new[]
    {
        new ItemDefinition("alliance_research_speedup_1h", TimeSpan.FromHours(1)),
        new ItemDefinition("alliance_research_speedup_3h", TimeSpan.FromHours(3)),
        new ItemDefinition("alliance_research_speedup_8h", TimeSpan.FromHours(8)),
        new ItemDefinition("alliance_research_speedup_24h", TimeSpan.FromHours(24)),
    };

    private static readonly IReadOnlyDictionary<string, ItemDefinition> ById =
        Items.ToDictionary(i => i.ItemId, StringComparer.Ordinal);

    public static bool TryGet(string itemId, out ItemDefinition definition) =>
        ById.TryGetValue(itemId ?? string.Empty, out definition!);
}
