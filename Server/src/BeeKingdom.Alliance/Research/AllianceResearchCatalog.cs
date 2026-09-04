namespace BeeKingdom.Alliance.Research;

// M052-CL: static catalog aligned with BIBLE_ALLIANCE_RESEARCH.md (C:\projets\beekingdom\BIBLE\
// BIBLE_ALLIANCE_RESEARCH.md, V1.0). Stable IDs remain the authoritative identity (never a
// localized string), mirroring the personal-Research convention - but the SHAPE of a technology
// changed from M051: no more single "progress toward completion", instead FundingRequirements
// (a list of resource/amount pairs, per Bible section 8/9) + ResearchDuration (a server-timed
// phase that only starts after funding completes AND a Chef/Officer launches it, per Bible
// section 2). Category (Minor/Major) replaces M051's flat single-track model with the Bible's two
// independent research slots (section 7).
//
// ALPHA SUBSET (M052, per the mission's explicit "representative subset, not 30-50 technologies"
// instruction): 8 technologies across the 4 required branches (Prospérité/Expansion/Coopération/
// Armée Royale) plus exactly one Major (Prospérité's "Âge d'abondance"), enough to prove
// prerequisite-gating, multi-resource funding, Minor+Major running simultaneously, and funding
// the next technology while the current one researches. Suprématie is intentionally absent - the
// Bible explicitly keeps it LOCKED/NOT IMPLEMENTED (section 19) until Alliance War exists.
//
// RETIRED FROM M051 (not in the Bible - see M052 report section 3): prosperity_colony_logistics,
// cooperation_collective_mobilization, defense_royal_guard (the Bible instead defines "Garde du
// royaume" as a MAJOR, not a third Minor tier - deferred rather than mis-implemented as a Minor).
//
// DURATIONS ARE ALPHA TEST VALUES, NOT FINAL BALANCE (Bible section 20/26 explicitly reserves
// final timing to a later balancing pass; section 12 mission text explicitly permits a shortened
// Alpha Major duration "for controlled testing ONLY if clearly marked"). Minutes here stand in for
// the Bible's target of days (Minor) / days-to-months (Major) so a human can exercise the full
// funding -> ready -> launch -> timer -> completion lifecycle in one sitting. The architecture
// itself is proven to support the Bible's real target range (30-60 day majors) by
// AllianceResearchServiceTests.LongDurationMathIsExact, not by deploying that value here.
public static class AllianceResearchCatalog
{
    public enum ResearchCategory { Minor, Major }

    public const string BranchProsperity = "prosperity";
    public const string BranchExpansion = "expansion";
    public const string BranchCooperation = "cooperation";
    public const string BranchArmyRoyal = "army_royal";
    // Bible section 19: exists in the design, stays out of the Alpha catalog entirely until
    // Alliance War/PvP ships - never populated below, kept here only so callers have the real
    // constant name instead of a magic string if/when it needs to appear in a "locked" UI list.
    public const string BranchSupremacy = "supremacy";

    public sealed record TechnologyDefinition(
        string TechnologyId,
        string Branch,
        ResearchCategory Category,
        int Tier,
        string DisplayNameKey,
        string DescriptionKey,
        string BonusSummaryKey,
        IReadOnlyList<string> PrerequisiteIds,
        // Bible section 8/9: a technology may require 1, 2, or 3 resources, and must normally NOT
        // primarily require the resource whose own production/collection it improves - never a
        // generic fixed "Honey+Pollen+Wax" bundle.
        IReadOnlyDictionary<string, long> FundingRequirements,
        // Bible section 2 Phase B: only starts once fully funded AND launched by a Chef/Officer.
        TimeSpan ResearchDuration,
        // Same three generic bonus categories M051 already integrated (HiveOfflineProductionService
        // for Production/Capacity, CombatPatrolService for CombatPower) - M052 keeps this
        // integration surface unchanged (see report "known compromises": true per-resource bonus
        // granularity, e.g. Honey-specific production, is deferred content/balancing work, not a
        // Bible lifecycle requirement).
        long ProductionBp = 0,
        long CapacityBp = 0,
        long CombatPowerBp = 0);

    public const string ContractVersion = "alliance-research-bible-v1";

    private static IReadOnlyDictionary<string, long> Cost(params (string resource, long amount)[] entries)
        => entries.ToDictionary(e => e.resource, e => e.amount, StringComparer.Ordinal);

    public static readonly IReadOnlyList<TechnologyDefinition> Technologies = new[]
    {
        // ---- PROSPÉRITÉ ----
        new TechnologyDefinition("prosperity_shared_reserves_i", BranchProsperity, ResearchCategory.Minor, 1,
            "alliance.research.prosperity_shared_reserves_i.name", "alliance.research.prosperity_shared_reserves_i.desc",
            "alliance.research.bonus.production_percent", Array.Empty<string>(),
            Cost(("honey", 3000), ("pollen", 2000), ("wax", 1500)), TimeSpan.FromMinutes(15), ProductionBp: 100),
        new TechnologyDefinition("prosperity_honey_mastery_i", BranchProsperity, ResearchCategory.Minor, 1,
            "alliance.research.prosperity_honey_mastery_i.name", "alliance.research.prosperity_honey_mastery_i.desc",
            "alliance.research.bonus.production_percent", new[] { "prosperity_shared_reserves_i" },
            // Bible section 8: Honey production must NOT be primarily funded by Honey itself.
            Cost(("pollen", 4000), ("wax", 2500)), TimeSpan.FromMinutes(20), ProductionBp: 150),
        new TechnologyDefinition("prosperity_age_of_abundance", BranchProsperity, ResearchCategory.Major, 1,
            "alliance.research.prosperity_age_of_abundance.name", "alliance.research.prosperity_age_of_abundance.desc",
            "alliance.research.bonus.production_percent", new[] { "prosperity_shared_reserves_i", "prosperity_honey_mastery_i" },
            Cost(("honey", 20000), ("pollen", 20000), ("wax", 15000)), TimeSpan.FromHours(2), ProductionBp: 300),

        // ---- EXPANSION ----
        // Real WorldMap gathering-speed integration is deferred (see report "known compromises") -
        // contributes to the same generic production bucket as a documented Alpha placeholder.
        new TechnologyDefinition("expansion_coordinated_harvest_i", BranchExpansion, ResearchCategory.Minor, 1,
            "alliance.research.expansion_coordinated_harvest_i.name", "alliance.research.expansion_coordinated_harvest_i.desc",
            "alliance.research.bonus.production_percent", Array.Empty<string>(),
            Cost(("honey", 2500), ("wax", 2000)), TimeSpan.FromMinutes(15), ProductionBp: 100),

        // ---- COOPÉRATION ----
        new TechnologyDefinition("cooperation_coordinated_aid_i", BranchCooperation, ResearchCategory.Minor, 1,
            "alliance.research.cooperation_coordinated_aid_i.name", "alliance.research.cooperation_coordinated_aid_i.desc",
            "alliance.research.bonus.capacity_percent", Array.Empty<string>(),
            Cost(("honey", 2000), ("pollen", 1500)), TimeSpan.FromMinutes(15), CapacityBp: 100),
        new TechnologyDefinition("cooperation_coordinated_aid_ii", BranchCooperation, ResearchCategory.Minor, 2,
            "alliance.research.cooperation_coordinated_aid_ii.name", "alliance.research.cooperation_coordinated_aid_ii.desc",
            "alliance.research.bonus.capacity_percent", new[] { "cooperation_coordinated_aid_i" },
            Cost(("honey", 3500), ("pollen", 2500)), TimeSpan.FromMinutes(25), CapacityBp: 100),

        // ---- ARMÉE ROYALE ----
        new TechnologyDefinition("defense_common_discipline_i", BranchArmyRoyal, ResearchCategory.Minor, 1,
            "alliance.research.defense_common_discipline_i.name", "alliance.research.defense_common_discipline_i.desc",
            "alliance.research.bonus.combat_power_percent", Array.Empty<string>(),
            Cost(("honey", 2000), ("pollen", 2000)), TimeSpan.FromMinutes(15), CombatPowerBp: 100),
        new TechnologyDefinition("defense_common_discipline_ii", BranchArmyRoyal, ResearchCategory.Minor, 2,
            "alliance.research.defense_common_discipline_ii.name", "alliance.research.defense_common_discipline_ii.desc",
            "alliance.research.bonus.combat_power_percent", new[] { "defense_common_discipline_i" },
            Cost(("honey", 3500), ("pollen", 3000), ("wax", 1500)), TimeSpan.FromMinutes(25), CombatPowerBp: 100),
    };

    private static readonly IReadOnlyDictionary<string, TechnologyDefinition> ById =
        Technologies.ToDictionary(t => t.TechnologyId, StringComparer.Ordinal);

    public static bool TryGet(string technologyId, out TechnologyDefinition definition) =>
        ById.TryGetValue(technologyId ?? string.Empty, out definition!);

    public static bool PrerequisitesMet(TechnologyDefinition definition, IReadOnlySet<string> completedTechnologyIds)
        => definition.PrerequisiteIds.All(completedTechnologyIds.Contains);

    public static IEnumerable<TechnologyDefinition> ForCategory(ResearchCategory category)
        => Technologies.Where(t => t.Category == category);
}
