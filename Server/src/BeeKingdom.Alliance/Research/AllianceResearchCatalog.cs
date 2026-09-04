namespace BeeKingdom.Alliance.Research;

// M051-CL: static Alpha content catalog - stable IDs are the authoritative identity (never a
// localized display string), mirrors the personal-Research convention
// (HiveOperationService.ResearchCatalog). Kept intentionally small: 9 technologies across 3
// branches, per the mission's explicit "compact Alpha foundation, not a giant production tree"
// instruction. Costs/RequiredProgress are Alpha-modest by design - human-testable by two accounts
// without days of accumulation (the mission's own "100 Honey/50 Pollen/25 Wax -> +10 progress"
// example is exactly what DonationCost/DonationProgressPerDonation below implement).
public static class AllianceResearchCatalog
{
    public const string BranchProsperity = "prosperity";
    public const string BranchCooperation = "cooperation";
    public const string BranchDefense = "defense";

    public sealed record TechnologyDefinition(
        string TechnologyId,
        string Branch,
        int Tier,
        string DisplayNameKey,
        string DescriptionKey,
        string BonusSummaryKey,
        long RequiredProgress,
        IReadOnlyList<string> PrerequisiteIds,
        IReadOnlyDictionary<string, long> DonationCost,
        long DonationProgressPerDonation,
        // Every Alpha technology contributes to exactly one bonus category, applied through the
        // small AllianceResearchBonusResolver below - never duplicated formula logic, only an
        // additional basis-point source merged alongside the existing bonus sources
        // (HiveOfflineProductionService.EffectiveRate/EffectiveCapacity,
        // CombatPatrolService's MergedPowerBonus) at their one real integration point each.
        long ProductionBp = 0,
        long CapacityBp = 0,
        long CombatPowerBp = 0);

    private static readonly IReadOnlyDictionary<string, long> AlphaCost1 = new Dictionary<string, long>(StringComparer.Ordinal) { ["honey"] = 100, ["pollen"] = 50, ["wax"] = 25 };
    private static readonly IReadOnlyDictionary<string, long> AlphaCost2 = new Dictionary<string, long>(StringComparer.Ordinal) { ["honey"] = 150, ["pollen"] = 75, ["wax"] = 40 };
    private static readonly IReadOnlyDictionary<string, long> AlphaCost3 = new Dictionary<string, long>(StringComparer.Ordinal) { ["honey"] = 200, ["pollen"] = 100, ["wax"] = 60 };
    private const long DonationProgress = 10;

    public const string ContractVersion = "alliance-research-alpha-v1";

    public static readonly IReadOnlyList<TechnologyDefinition> Technologies = new[]
    {
        new TechnologyDefinition("prosperity_shared_reserves_i", BranchProsperity, 1,
            "alliance.research.prosperity_shared_reserves_i.name", "alliance.research.prosperity_shared_reserves_i.desc",
            "alliance.research.bonus.production_percent", 60, Array.Empty<string>(), AlphaCost1, DonationProgress, ProductionBp: 100),
        new TechnologyDefinition("prosperity_shared_reserves_ii", BranchProsperity, 2,
            "alliance.research.prosperity_shared_reserves_ii.name", "alliance.research.prosperity_shared_reserves_ii.desc",
            "alliance.research.bonus.production_percent", 90, new[] { "prosperity_shared_reserves_i" }, AlphaCost2, DonationProgress, ProductionBp: 100),
        new TechnologyDefinition("prosperity_colony_logistics", BranchProsperity, 3,
            "alliance.research.prosperity_colony_logistics.name", "alliance.research.prosperity_colony_logistics.desc",
            "alliance.research.bonus.production_percent", 120, new[] { "prosperity_shared_reserves_ii" }, AlphaCost3, DonationProgress, ProductionBp: 200),

        new TechnologyDefinition("cooperation_coordinated_aid_i", BranchCooperation, 1,
            "alliance.research.cooperation_coordinated_aid_i.name", "alliance.research.cooperation_coordinated_aid_i.desc",
            "alliance.research.bonus.capacity_percent", 60, Array.Empty<string>(), AlphaCost1, DonationProgress, CapacityBp: 100),
        new TechnologyDefinition("cooperation_coordinated_aid_ii", BranchCooperation, 2,
            "alliance.research.cooperation_coordinated_aid_ii.name", "alliance.research.cooperation_coordinated_aid_ii.desc",
            "alliance.research.bonus.capacity_percent", 90, new[] { "cooperation_coordinated_aid_i" }, AlphaCost2, DonationProgress, CapacityBp: 100),
        new TechnologyDefinition("cooperation_collective_mobilization", BranchCooperation, 3,
            "alliance.research.cooperation_collective_mobilization.name", "alliance.research.cooperation_collective_mobilization.desc",
            "alliance.research.bonus.capacity_percent", 120, new[] { "cooperation_coordinated_aid_ii" }, AlphaCost3, DonationProgress, CapacityBp: 200),

        new TechnologyDefinition("defense_common_discipline_i", BranchDefense, 1,
            "alliance.research.defense_common_discipline_i.name", "alliance.research.defense_common_discipline_i.desc",
            "alliance.research.bonus.combat_power_percent", 60, Array.Empty<string>(), AlphaCost1, DonationProgress, CombatPowerBp: 100),
        new TechnologyDefinition("defense_common_discipline_ii", BranchDefense, 2,
            "alliance.research.defense_common_discipline_ii.name", "alliance.research.defense_common_discipline_ii.desc",
            "alliance.research.bonus.combat_power_percent", 90, new[] { "defense_common_discipline_i" }, AlphaCost2, DonationProgress, CombatPowerBp: 100),
        new TechnologyDefinition("defense_royal_guard", BranchDefense, 3,
            "alliance.research.defense_royal_guard.name", "alliance.research.defense_royal_guard.desc",
            "alliance.research.bonus.combat_power_percent", 120, new[] { "defense_common_discipline_ii" }, AlphaCost3, DonationProgress, CombatPowerBp: 200),
    };

    private static readonly IReadOnlyDictionary<string, TechnologyDefinition> ById =
        Technologies.ToDictionary(t => t.TechnologyId, StringComparer.Ordinal);

    public static bool TryGet(string technologyId, out TechnologyDefinition definition) =>
        ById.TryGetValue(technologyId ?? string.Empty, out definition!);

    public static bool PrerequisitesMet(TechnologyDefinition definition, IReadOnlyDictionary<string, AllianceTechnologyProgress> progress)
        => definition.PrerequisiteIds.All(id => progress.TryGetValue(id, out AllianceTechnologyProgress? p) && p.Completed);
}
