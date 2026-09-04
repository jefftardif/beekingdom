namespace BeeKingdom.Alliance.Research;

// M052-CL: Bible-aligned domain model (BIBLE_ALLIANCE_RESEARCH.md V1.0). Replaces M051's single
// "progress toward completion" number with the Bible's two-phase lifecycle: Funding (per-resource,
// Chef-selected target) -> Ready -> Researching (server timer, launched by Chef/Officer) ->
// Completed (bonus active). AllianceResearchState is still the Alliance-owned aggregate - one row
// per Alliance, shared identically by every current member - same principle as M051, different
// shape.

public sealed record AllianceTechnologyFunding(Dictionary<string, long> Contributed)
{
    public static AllianceTechnologyFunding Empty() => new(new Dictionary<string, long>(StringComparer.Ordinal));
}

// One research slot (Minor or Major) - at most one technology of each category can occupy its
// slot at a time (Bible section 7). Cleared (set to null) once resolved to Completed.
public sealed record AllianceResearchSlot(string TechnologyId, DateTimeOffset StartedAtUtc, DateTimeOffset CompletesAtUtc);

public sealed record AllianceCompletedTechnology(string TechnologyId, DateTimeOffset CompletedAtUtc);

// TotalPoints: lifetime Contribution (Bible section 10 - historical, never spent/decreased).
// AllianceCurrencyBalance: spendable "Sceaux Royaux" foundation (Bible section 11) - a distinct,
// decreasing-when-spent balance. M052 only ever increases it (no spend path exists yet - that's
// the future Alliance Shop, explicitly out of scope).
public sealed record AllianceResearchContribution(Guid PlayerId, long TotalPoints, long DonationCount, long AllianceCurrencyBalance);

public sealed record AllianceResearchState(
    Guid AllianceId,
    int ModelVersion,
    long Revision,
    string? MinorFundingTargetId,
    string? MajorFundingTargetId,
    // Sparse - only technologies that have ever received a contribution appear here. Survives a
    // funding-target change untouched (Bible section 5 - "changing the target does not destroy
    // prior contributions").
    Dictionary<string, AllianceTechnologyFunding> Funding,
    AllianceResearchSlot? MinorResearch,
    AllianceResearchSlot? MajorResearch,
    Dictionary<string, AllianceCompletedTechnology> Completed,
    Dictionary<Guid, AllianceResearchContribution> Contributions,
    // Idempotency guards, one HashSet per operation KIND - a donation retry, a launch retry, and a
    // speedup retry are independent concerns and must not collide on the same key namespace.
    HashSet<string> ProcessedDonationIds,
    HashSet<string> ProcessedLaunchIds,
    HashSet<string> ProcessedSpeedUpIds)
{
    public const int CurrentModelVersion = 2;

    public static AllianceResearchState Empty(Guid allianceId) => new(
        allianceId, CurrentModelVersion, Revision: 0,
        null, null,
        new Dictionary<string, AllianceTechnologyFunding>(StringComparer.Ordinal),
        null, null,
        new Dictionary<string, AllianceCompletedTechnology>(StringComparer.Ordinal),
        new Dictionary<Guid, AllianceResearchContribution>(),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal));
}

// ---------------- Commands ----------------

// Bible section 4: exclusively the Chef's decision. TechnologyId's own Category (Minor/Major,
// read from the catalog) determines which of the two slots this targets - the caller never
// specifies Category directly, so there is no way to send a mismatched pair.
public sealed record SelectAllianceResearchFundingTargetCommand(string TechnologyId, string ClientRequestId);

// TechnologyId must equal the category's CURRENT funding target - donating to any other
// technology (including a merely-eligible-but-unselected one) is rejected server-side (Bible
// section 4's "members donate only to the currently designated technology").
public sealed record DonateToAllianceResearchCommand(Guid HiveId, string TechnologyId, string ResourceKey, long Amount, string ClientRequestId);

public sealed record LaunchAllianceResearchCommand(string TechnologyId, string ClientRequestId);

public sealed record ApplyAllianceResearchSpeedUpCommand(Guid HiveId, string TechnologyId, string ItemId, string ClientRequestId);

public sealed record AllianceResearchCommandResult(bool Succeeded, string Code, AllianceResearchReadSnapshot? Snapshot);

// ---------------- Read model ----------------

public enum AllianceTechnologyState
{
    Locked,      // prerequisites unmet
    Eligible,    // prerequisites met, not currently the Chef-selected funding target
    Funding,     // Chef-selected, accepting donations
    Ready,       // fully funded, awaiting Chef/Officer launch
    Researching, // server timer running
    Completed    // bonus active
}

public sealed record AllianceTechnologyReadModel(
    string TechnologyId,
    string Branch,
    string Category, // "minor" | "major"
    int Tier,
    string DisplayNameKey,
    string DescriptionKey,
    string BonusSummaryKey,
    IReadOnlyList<string> PrerequisiteIds,
    AllianceTechnologyState State,
    IReadOnlyDictionary<string, long> FundingRequired,
    IReadOnlyDictionary<string, long> FundingContributed,
    long ResearchDurationSeconds,
    DateTimeOffset? ResearchStartedAtUtc,
    DateTimeOffset? ResearchCompletesAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long ProductionBp,
    long CapacityBp,
    long CombatPowerBp);

public sealed record AllianceResearchReadSnapshot(
    Guid AllianceId,
    string ContractVersion,
    DateTimeOffset ServerTimeUtc,
    long Revision,
    IReadOnlyList<AllianceTechnologyReadModel> Technologies,
    string? MinorFundingTargetId,
    string? MajorFundingTargetId,
    string? MinorResearchingTechnologyId,
    string? MajorResearchingTechnologyId,
    long MyContributionPoints,
    long MyDonationCount,
    long MyAllianceCurrencyBalance,
    // Server-computed authority, never re-derived client-side (the mission's own explicit "server
    // remains authoritative even if UI hides controls" instruction) - Unity only reads these.
    bool CanSelectFundingTarget,
    bool CanLaunch,
    bool CanUseSpeedUp);

// Aggregated, already-resolved bonus a player currently receives from their Alliance's COMPLETED
// research only (never Funding/Ready/Researching - Bible section "critical difference #6"; the
// resolver below enforces this by construction, only ever iterating `state.Completed`).
public sealed record AllianceResearchBonus(long ProductionBp, long CapacityBp, long CombatPowerBp)
{
    public static readonly AllianceResearchBonus None = new(0, 0, 0);
}
