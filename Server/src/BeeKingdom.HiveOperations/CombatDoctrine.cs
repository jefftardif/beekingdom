namespace BeeKingdom.HiveOperations;

public sealed record CombatDoctrineSnapshot(string CatalogVersion, IReadOnlyList<string> Families, IReadOnlyDictionary<string, string> AdvantageCycle);

public sealed class CombatDoctrineService
{
    public const string CatalogVersion = "phase4-combat-v1";
    public static readonly IReadOnlyList<string> Families = ["guardians", "wingrunners", "darters"];
    public static readonly IReadOnlyDictionary<string, string> AdvantageCycle = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["guardians"] = "darters", ["darters"] = "wingrunners", ["wingrunners"] = "guardians"
    };

    public CombatDoctrineSnapshot GetSnapshot() => new(CatalogVersion, Families, AdvantageCycle);

    public bool HasAdvantage(string attacker, string defender) =>
        AdvantageCycle.TryGetValue(attacker, out string? target) && string.Equals(target, defender, StringComparison.Ordinal);
}
