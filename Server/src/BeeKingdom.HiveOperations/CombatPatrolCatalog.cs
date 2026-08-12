namespace BeeKingdom.HiveOperations;

// Starting balance table for the 7 bestiary tiers already visible on the world map
// (see Docs/WorldMapRuntimeEntitiesWave1/EntityMatrix.md). Tunable — not a final balance.
public static class CombatPatrolCatalog
{
    public const string CatalogVersion = "phase-combat-patrol-v1";

    public static readonly IReadOnlyDictionary<int, BestiaryTierDefinition> Tiers = new Dictionary<int, BestiaryTierDefinition>
    {
        [1] = new(1, "Puceron voleur", "wingrunners", 40, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30), 60, 30),
        [2] = new(2, "Fourmi coupeuse", "guardians", 90, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), 120, 70),
        [3] = new(3, "Araignee sauteuse", "darters", 160, TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(120), 220, 140),
        [4] = new(4, "Mante predatrice", "guardians", 260, TimeSpan.FromSeconds(60), TimeSpan.FromMinutes(4), 380, 240),
        [5] = new(5, "Frelon brigand", "wingrunners", 400, TimeSpan.FromSeconds(90), TimeSpan.FromMinutes(8), 620, 400),
        [6] = new(6, "Scorpion des racines", "darters", 600, TimeSpan.FromSeconds(120), TimeSpan.FromMinutes(16), 1000, 650),
        [7] = new(7, "Reine frelon ancienne", "wingrunners", 900, TimeSpan.FromSeconds(180), TimeSpan.FromMinutes(30), 1800, 1200)
    };

    public static bool TryGet(int tier, out BestiaryTierDefinition definition) => Tiers.TryGetValue(tier, out definition!);

    // Base slot (1) is free. Slots 2-3 (index 0-1 below) are purchasable with honey/pollen.
    // Slots 4-5 are premium (real money) — see CombatPatrolService.GrantPremiumSlotAsync, which
    // only grants the entitlement and never validates a payment itself; a real store-receipt
    // check must sit in front of that call before this ships.
    public const int MaxConcurrentSlots = 5;
    public const int MaxResourcePurchasedSlots = 2;
    public const int MaxPremiumPurchasedSlots = 2;

    public static readonly IReadOnlyList<(long Honey, long Pollen)> ResourceSlotCosts = new[]
    {
        (Honey: 800L, Pollen: 500L),
        (Honey: 2200L, Pollen: 1400L)
    };
}
