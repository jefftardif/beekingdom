namespace BeeKingdom.HiveOperations;

public enum CombatPatrolOutcomeBand { Blocked, HardWon, Victory, DecisiveVictory }

public sealed record BestiaryTierDefinition(
    int Tier, string EnemyName, string HazardFamily, long RequiredPower,
    TimeSpan Duration, TimeSpan Cooldown, long HoneyReward, long PollenReward);

public sealed record CombatPatrolResolutionResult(
    CombatPatrolOutcomeBand Band, long ReadinessBp, long AvailablePower, long RequiredPower,
    IReadOnlyDictionary<string, long> PermanentLosses, IReadOnlyDictionary<string, long> WoundedLosses,
    long HoneyCredited, long PollenCredited);

// Pure, deterministic combat math: same squad + same tier always resolves to the same
// outcome (no RNG), so players can reason about risk before committing troops.
public static class CombatPatrolResolution
{
    public const long UnitPower = 5;
    public const long AdvantageBonusBp = 3500;
    public const long DisadvantageMalusBp = 2500;
    public const long BlockedThresholdBp = 7000;
    public const long VictoryThresholdBp = 10000;
    public const long DecisiveThresholdBp = 13000;
    // Most combat losses are wounded bees that recover after a rest period (see
    // CombatPatrolService's recovery queue) — only a small share is ever permanent.
    public const long PermanentLossShareBp = 1500;
    private static readonly string[] Families = ["guardians", "wingrunners", "darters"];

    public static long ComputeAvailablePower(IReadOnlyDictionary<string, long> squad, string hazardFamily, IReadOnlyDictionary<string, long>? championBonusBpByFamily = null)
    {
        long power = 0;
        foreach (string family in Families)
        {
            long count = Math.Max(0, squad.GetValueOrDefault(family));
            long modifierBp = 10000;
            if (HasAdvantage(family, hazardFamily)) modifierBp += AdvantageBonusBp;
            else if (HasAdvantage(hazardFamily, family)) modifierBp -= DisadvantageMalusBp;
            modifierBp += championBonusBpByFamily?.GetValueOrDefault(family) ?? 0;
            power = checked(power + count * UnitPower * modifierBp / 10000);
        }
        return power;
    }

    public static long ComputeReadinessBp(long availablePower, long requiredPower)
        => requiredPower <= 0 ? 0 : checked(availablePower * 10000 / requiredPower);

    public static bool CanLaunch(long readinessBp) => readinessBp >= BlockedThresholdBp;

    // Longer, harder patrols keep wounded bees resting longer — derived from the tier's
    // patrol duration so there is no separate balance table to keep in sync.
    public static TimeSpan ComputeRecoveryDuration(BestiaryTierDefinition tier) => tier.Duration * 6;

    public static CombatPatrolResolutionResult Resolve(IReadOnlyDictionary<string, long> squad, BestiaryTierDefinition tier, IReadOnlyDictionary<string, long>? championBonusBpByFamily = null)
    {
        long availablePower = ComputeAvailablePower(squad, tier.HazardFamily, championBonusBpByFamily);
        long readinessBp = ComputeReadinessBp(availablePower, tier.RequiredPower);

        if (readinessBp < BlockedThresholdBp)
        {
            return new(CombatPatrolOutcomeBand.Blocked, readinessBp, availablePower, tier.RequiredPower, EmptyLosses(), EmptyLosses(), 0, 0);
        }

        CombatPatrolOutcomeBand band;
        long lossRateBp;
        long rewardRateBp;
        if (readinessBp >= DecisiveThresholdBp)
        {
            band = CombatPatrolOutcomeBand.DecisiveVictory;
            lossRateBp = 0;
            rewardRateBp = 11000;
        }
        else if (readinessBp >= VictoryThresholdBp)
        {
            band = CombatPatrolOutcomeBand.Victory;
            lossRateBp = 500;
            rewardRateBp = 10000;
        }
        else
        {
            band = CombatPatrolOutcomeBand.HardWon;
            const long span = VictoryThresholdBp - BlockedThresholdBp;
            long progress = readinessBp - BlockedThresholdBp;
            lossRateBp = 3000 - progress * 1500 / span;
            rewardRateBp = 5000 + progress * 2000 / span;
        }

        (IReadOnlyDictionary<string, long> permanent, IReadOnlyDictionary<string, long> wounded) = DistributeLosses(squad, lossRateBp);
        long honey = checked(tier.HoneyReward * rewardRateBp / 10000);
        long pollen = checked(tier.PollenReward * rewardRateBp / 10000);
        return new(band, readinessBp, availablePower, tier.RequiredPower, permanent, wounded, honey, pollen);
    }

    private static bool HasAdvantage(string attacker, string defender) =>
        CombatDoctrineService.AdvantageCycle.TryGetValue(attacker, out string? target) && string.Equals(target, defender, StringComparison.Ordinal);

    private static (IReadOnlyDictionary<string, long> Permanent, IReadOnlyDictionary<string, long> Wounded) DistributeLosses(IReadOnlyDictionary<string, long> squad, long lossRateBp)
    {
        var permanent = new Dictionary<string, long>(StringComparer.Ordinal);
        var wounded = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (string family in Families)
        {
            long count = Math.Max(0, squad.GetValueOrDefault(family));
            long totalLost = Math.Min(count, count * lossRateBp / 10000);
            long permanentLost = totalLost * PermanentLossShareBp / 10000;
            permanent[family] = permanentLost;
            wounded[family] = totalLost - permanentLost;
        }
        return (permanent, wounded);
    }

    private static IReadOnlyDictionary<string, long> EmptyLosses() => Families.ToDictionary(f => f, _ => 0L, StringComparer.Ordinal);
}
