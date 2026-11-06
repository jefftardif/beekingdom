using System;
using System.Collections.Generic;

namespace BeeKingdom.Alliance.Territories;

/// <summary>
/// Reference to a real world building aggregate, not a generated map marker.
/// Construction/contribution, destruction, capture and relocation commands are
/// intentionally absent. The provider must verify this building exists and is owned
/// by the same alliance in the same world/surface before committing territory state.
/// </summary>
public sealed record TerritoryHeadquarters(
    Guid BuildingId, int Level, long Revision, TerritoryPoint Position, bool Operational);

public sealed record HeadquartersCapacityTier(
    int MinimumBuildingLevel, int RequiredAllianceLevel, decimal MaximumArea,
    int MaximumFloralInstallations);
public sealed record HeadquartersCapacitySchedule(string Version, IReadOnlyList<HeadquartersCapacityTier> Tiers);
public sealed record HeadquartersCapacityResult(bool Available, string Code,
    decimal? MaximumArea, int? MaximumFloralInstallations);

/// <summary>
/// Candidate capacity policy: alliance progression is an unlock/ceiling; the real
/// building's achieved level limits currently available area and floral slots.
/// No automatic construction or annexation, no resource cost or buff activated.
/// All numeric tuning must be explicitly configured; there are no production defaults.
/// </summary>
public static class HeadquartersCapacity
{
    public static HeadquartersCapacityResult Evaluate(int allianceLevel, decimal allianceAreaCeiling,
        TerritoryHeadquarters? building, HeadquartersCapacitySchedule? schedule)
    {
        HeadquartersCapacityResult No(string code) => new(false, code, null, null);
        if (building is null || building.BuildingId == Guid.Empty) return No("headquarters_missing");
        if (building.Level < 1 || building.Revision < 0 || allianceLevel < 1 || allianceAreaCeiling <= 0)
            return No("headquarters_progression_invalid");
        if (!building.Operational) return No("headquarters_not_operational");
        return EvaluateConstructionTarget(allianceLevel,allianceAreaCeiling,building.Level,schedule);
    }

    // A construction plan may inspect a target level without inventing an existing building identity.
    public static HeadquartersCapacityResult EvaluateConstructionTarget(int allianceLevel, decimal allianceAreaCeiling,
        int targetLevel, HeadquartersCapacitySchedule? schedule)
    {
        HeadquartersCapacityResult No(string code) => new(false,code,null,null);
        if(allianceLevel<1 || allianceAreaCeiling<=0 || targetLevel<1) return No("headquarters_progression_invalid");
        if (schedule is null) return No("headquarters_schedule_unavailable");
        if (string.IsNullOrWhiteSpace(schedule.Version) || schedule.Tiers is null || schedule.Tiers.Count == 0)
            return No("headquarters_schedule_invalid");
        int previousLevel = 0, previousUnlock = 0, previousSlots = -1;
        decimal previousArea = 0;
        HeadquartersCapacityTier? selected = null;
        foreach (var tier in schedule.Tiers)
        {
            if (tier is null || tier.MinimumBuildingLevel <= previousLevel || tier.RequiredAllianceLevel < 1
                || tier.RequiredAllianceLevel < previousUnlock || tier.MaximumArea <= 0
                || tier.MaximumArea < previousArea || tier.MaximumFloralInstallations < 0
                || tier.MaximumFloralInstallations < previousSlots)
                return No("headquarters_schedule_invalid");
            previousLevel = tier.MinimumBuildingLevel; previousUnlock = tier.RequiredAllianceLevel;
            previousArea = tier.MaximumArea; previousSlots = tier.MaximumFloralInstallations;
            if (tier.MinimumBuildingLevel <= targetLevel) selected = tier;
        }
        if (selected is null) return No("headquarters_level_not_configured");
        if (allianceLevel < selected.RequiredAllianceLevel) return No("headquarters_alliance_unlock_unmet");
        return new(true, "headquarters_capacity_available", Math.Min(allianceAreaCeiling, selected.MaximumArea), selected.MaximumFloralInstallations);
    }
}
