namespace BeeKingdom.HiveOperations;

/// <summary>
/// Experimental but deterministic rules for future nursery cohorts.
/// Server-owned projection only: no cohort is created, no reserve is credited,
/// and the existing BroodVitalityCareService's cost/duration/gains are unchanged.
/// This deliberately does NOT change the military roster or unlock client actions.
/// </summary>
public static class NurseryGrowthRules
{
    public const string ContractVersion = "bk-nursery-growth-balance-v1-draft";
    public const int InitialBaseRecruits = 30;
    public const int InitialBaseDurationSeconds = 3600;
    public const long InitialReserveCapacity = 120;
    private const int BasisPoints = 10_000;
    private const int MaxBaseRecruits = 1_000_000;
    private const int MaxBaseDurationSeconds = 604_800;
    private const long MaxReserve = 1_000_000_000;

    public static NurseryVitalityTier Tier(int value)
    {
        if (value is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "Nursery vitality must be between 0 and 100.");
        if (value <= 39) return NurseryVitalityTier.Fragile;
        if (value <= 64) return NurseryVitalityTier.Watch;
        if (value <= 89) return NurseryVitalityTier.Stable;
        return NurseryVitalityTier.Thriving;
    }

    public static int NutritionSpeedBasisPoints(int nutrition)
        => Tier(nutrition) switch
        {
            NurseryVitalityTier.Fragile => 8_000,
            NurseryVitalityTier.Watch => 9_000,
            NurseryVitalityTier.Stable => 10_000,
            NurseryVitalityTier.Thriving => 11_000,
            _ => throw new InvalidOperationException("Unsupported nutrition tier.")
        };

    public static int StabilityYieldBasisPoints(int stability)
        => Tier(stability) switch
        {
            NurseryVitalityTier.Fragile => 7_500,
            NurseryVitalityTier.Watch => 8_900,
            NurseryVitalityTier.Stable => 10_000,
            NurseryVitalityTier.Thriving => 10_450,
            _ => throw new InvalidOperationException("Unsupported stability tier.")
        };

    /// <summary>
    /// Project a potential cohort from immutable start-of-cohort parameters.
    /// Nothing in here writes to a hive, spends resources, mints recruits or
    /// changes troop training; an actual cohort service must persist a validated
    /// start snapshot through IHiveStateRepository before enabling play.
    /// </summary>
    public static NurseryCohortForecast Preview(
        int nutrition, int stability,
        int baseRecruits,
        int baseDurationSeconds,
        long alreadyReservedRecruits,
        long reserveCapacity,
        DateTimeOffset startsAtUtc)
    {
        if (baseRecruits is < 1 or > MaxBaseRecruits)
            throw new ArgumentOutOfRangeException(nameof(baseRecruits));
        if (baseDurationSeconds is < 1 or > MaxBaseDurationSeconds)
            throw new ArgumentOutOfRangeException(nameof(baseDurationSeconds));
        if (reserveCapacity is < 1 or > MaxReserve)
            throw new ArgumentOutOfRangeException(nameof(reserveCapacity));
        if (alreadyReservedRecruits < 0 || alreadyReservedRecruits > reserveCapacity)
            throw new ArgumentOutOfRangeException(nameof(alreadyReservedRecruits));
        if (startsAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Cohort timestamps must be UTC.", nameof(startsAtUtc));

        int nutritionBp = NutritionSpeedBasisPoints(nutrition);
        int stabilityBp = StabilityYieldBasisPoints(stability);
        int quality = Math.Min(nutrition, stability);

        // Exact integer arithmetic: ceil(baseSeconds / speedFactor).
        // The cap on baseDurationSeconds makes this overflow-proof.
        int durationSeconds = checked(
            (int)(((long)baseDurationSeconds * BasisPoints + nutritionBp - 1L)
                / nutritionBp));

        // Integer floor; no rounding up of recruits and no second combined
        // efficiency multiplier (which would penalize two times).
        int projectedRecruits = checked(
            (int)((long)baseRecruits * stabilityBp / BasisPoints));
        if (projectedRecruits < 1) projectedRecruits = 1;

        DateTimeOffset endsAtUtc;
        try { endsAtUtc = startsAtUtc.AddSeconds(durationSeconds); }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentException("Projected completion time is invalid.",
                nameof(startsAtUtc), ex);
        }

        // Never silently destroy excess recruits or allow overflow of the reserve.
        // Block a new batch if ALL expected recruits cannot be stored.
        bool hasCapacity = reserveCapacity - alreadyReservedRecruits
            >= projectedRecruits;

        return new NurseryCohortForecast(
            ContractVersion, Tier(quality),
            nutrition, stability, quality,
            nutritionBp, stabilityBp,
            // Diagnostic combined efficiency, NEVER applied again to duration/yield.
            (int)((long)nutritionBp * stabilityBp / BasisPoints),
            baseRecruits, projectedRecruits,
            baseDurationSeconds, durationSeconds,
            startsAtUtc, endsAtUtc,
            alreadyReservedRecruits, reserveCapacity, hasCapacity);
    }
}

public enum NurseryVitalityTier { Fragile, Watch, Stable, Thriving }

public sealed record NurseryCohortForecast(
    string ContractVersion,
    NurseryVitalityTier GlobalTier,
    int NutritionAtStart,
    int StabilityAtStart,
    int GlobalQuality,
    int NutritionSpeedBasisPoints,
    int StabilityYieldBasisPoints,
    int IndicativeCombinedEfficiencyBasisPoints,
    int BaseRecruits,
    int ExpectedRecruits,
    int BaseDurationSeconds,
    int DurationSeconds,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    long RecruitsAlreadyInReserve,
    long ReserveCapacity,
    bool CanStartWithCurrentReserve);
