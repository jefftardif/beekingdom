namespace BeeKingdom.HiveOperations;

public enum ChampionBeeRarity { Rare, Legendary }
public enum ChampionBeeRole { Guardians, Wingrunners, Darters, Civilian }

// Role + bonus fields mirror Assets/BeeKingdom/Playground/ChampionBeeCatalog.cs exactly (same
// values) - the server needed its own copy of these numbers to let an assigned champion bee
// actually change Combat Patrol's deterministic power math, not just show a cosmetic summary.
public sealed record ChampionBeeDefinition(
    string Id, ChampionBeeRarity Rarity, ChampionBeeRole Role,
    float RoleStatBonusPercentPerLevel, float GlobalStatBonusPercentPerLevel);

// Aggregate combat contribution of every assigned champion bee, expressed as basis points
// (100 = 1%) added to CombatPatrolResolution's per-family power modifier - see
// CombatContribution() below and CombatPatrolResolution.ComputeAvailablePower.
public sealed record ChampionCombatContribution(
    IReadOnlyDictionary<string, long> PowerBonusBpByFamily, IReadOnlyList<string> ContributingBeeIds);

// Miroir serveur de Assets/BeeKingdom/Playground/ChampionBeeCatalog.cs - les deux doivent
// rester synchronises si un nouveau champion est ajoute cote client.
public static class ChampionBeeCatalog
{
    public const int RareUnlockCoeurRoyalLevel = 3;
    public const int LegendaryUnlockCoeurRoyalLevel = 10;
    public const int MaxLevel = 10;
    public const int MaxAssigned = 5;
    private static readonly string[] CombatFamilies = ["guardians", "wingrunners", "darters"];

    public static readonly IReadOnlyDictionary<string, ChampionBeeDefinition> Definitions = new Dictionary<string, ChampionBeeDefinition>(StringComparer.Ordinal)
    {
        ["striga"] = new("striga", ChampionBeeRarity.Rare, ChampionBeeRole.Guardians, 3f, 0.5f),
        ["zephyra"] = new("zephyra", ChampionBeeRarity.Rare, ChampionBeeRole.Wingrunners, 3f, 0.5f),
        ["ambra"] = new("ambra", ChampionBeeRarity.Legendary, ChampionBeeRole.Darters, 5f, 1f),
        ["nectaria"] = new("nectaria", ChampionBeeRarity.Rare, ChampionBeeRole.Civilian, 0f, 0f),
        ["aurelia"] = new("aurelia", ChampionBeeRarity.Legendary, ChampionBeeRole.Civilian, 0f, 0f)
    };

    public static int UnlockCoeurRoyalLevel(ChampionBeeRarity rarity) => rarity == ChampionBeeRarity.Legendary ? LegendaryUnlockCoeurRoyalLevel : RareUnlockCoeurRoyalLevel;

    public static (long Honey, long Pollen) LevelUpCost(ChampionBeeRarity rarity, int currentLevel)
    {
        int level = Math.Max(1, currentLevel);
        bool legendary = rarity == ChampionBeeRarity.Legendary;
        long honey = legendary ? 300L + level * 220L : 150L + level * 90L;
        long pollen = legendary ? 120L + level * 70L : 60L + level * 30L;
        return (honey, pollen);
    }

    public static int MaxAssignedForCoeurRoyalLevel(int coeurRoyalLevel) => Math.Clamp(1 + coeurRoyalLevel / 8, 1, MaxAssigned);

    public static string CombatFamilyId(ChampionBeeRole role) => role switch
    {
        ChampionBeeRole.Guardians => "guardians",
        ChampionBeeRole.Wingrunners => "wingrunners",
        ChampionBeeRole.Darters => "darters",
        _ => string.Empty
    };

    // Every assigned champion bee whose role matches a squad family adds its role bonus to
    // that family only; every assigned champion bee (any role) also adds its (usually smaller)
    // global bonus to all three families - mirrors the cosmetic client summary, but now feeds
    // real combat math instead of only display text.
    public static ChampionCombatContribution CombatContribution(ChampionBeeProgressState? champions)
    {
        Dictionary<string, long> perFamily = CombatFamilies.ToDictionary(f => f, _ => 0L, StringComparer.Ordinal);
        List<string> contributors = new();
        long globalBonusBp = 0;
        if (champions?.AssignedBeeIds != null)
        {
            foreach (string beeId in champions.AssignedBeeIds)
            {
                if (!Definitions.TryGetValue(beeId, out ChampionBeeDefinition? definition)) continue;
                int level = Math.Clamp(champions.Levels.GetValueOrDefault(beeId, 1), 1, MaxLevel);
                string family = CombatFamilyId(definition.Role);
                long roleBonusBp = (long)Math.Round(definition.RoleStatBonusPercentPerLevel * level * 100d);
                long beeGlobalBonusBp = (long)Math.Round(definition.GlobalStatBonusPercentPerLevel * level * 100d);
                bool contributes = beeGlobalBonusBp > 0;
                if (!string.IsNullOrEmpty(family) && roleBonusBp > 0) { perFamily[family] += roleBonusBp; contributes = true; }
                globalBonusBp += beeGlobalBonusBp;
                if (contributes) contributors.Add(beeId);
            }
        }
        foreach (string family in CombatFamilies) perFamily[family] += globalBonusBp;
        return new(perFamily, contributors);
    }
}

// Contribution de puissance de combat des paliers de troupe deja promus, dans le meme format
// (basis points par famille) que ChampionCombatContribution - voir TroopTierCatalog.CombatContribution.
public sealed record TroopTierCombatContribution(
    IReadOnlyDictionary<string, long> PowerBonusBpByFamily, IReadOnlyDictionary<string, int> TierByFamily);

// Miroir serveur des paliers de troupe (T1-T3) definis cote client.
public static class TroopTierCatalog
{
    public const int MaxTier = 3;
    public static readonly IReadOnlySet<string> PopulationIds = new HashSet<string>(StringComparer.Ordinal) { "soldiers", "guardians", "scouts", "wingrunners", "darters" };
    // Seules ces trois populations combattent reellement en Patrouille de combat (voir
    // CombatPatrolResolution.Families) - soldiers/scouts existent dans le catalogue de
    // promotion mais n'ont aucun role de combat aujourd'hui.
    private static readonly string[] CombatFamilies = ["guardians", "wingrunners", "darters"];
    // +25% de puissance par palier au-dessus de 1 - reprend exactement la formule deja
    // utilisee cote client pour l'apercu cosmetique (HiveViewProductUiPresenter.TroopTierMultiplier),
    // maintenant appliquee au vrai calcul de combat plutot qu'a un texte local uniquement.
    public const long PowerBonusBpPerTierAboveOne = 2500;

    public static (long Honey, long Pollen) PromotionCost(int currentTier)
    {
        return (200L * currentTier, 80L * currentTier);
    }

    public static TroopTierCombatContribution CombatContribution(TroopTierState? troopTiers)
    {
        Dictionary<string, long> powerBonusBpByFamily = new(StringComparer.Ordinal);
        Dictionary<string, int> tierByFamily = new(StringComparer.Ordinal);
        foreach (string family in CombatFamilies)
        {
            int tier = Math.Clamp(troopTiers?.Tiers.GetValueOrDefault(family, 1) ?? 1, 1, MaxTier);
            tierByFamily[family] = tier;
            powerBonusBpByFamily[family] = (tier - 1) * PowerBonusBpPerTierAboveOne;
        }
        return new(powerBonusBpByFamily, tierByFamily);
    }
}
