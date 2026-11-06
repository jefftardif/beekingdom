using System;
using System.Collections.Generic;

namespace BeeKingdom.Alliance.Territories;

/// <summary>
/// Candidate read-only capacity rule. Does not grant land, construct boundaries,
/// change access, or mutate alliance progression. No production tuning defaults.
/// Areas must be computed from authoritative geometry in one declared surface unit.
/// </summary>
public sealed record TerritoryCapacityTier(int MinimumLevel, decimal MaximumArea);
public sealed record TerritoryCapacitySchedule(
    string Version, string SurfaceId, string AreaUnit,
    IReadOnlyList<TerritoryCapacityTier> Tiers);
public sealed record TerritoryCapacityInput(
    Guid AllianceId, string WorldId, string SurfaceId, string AreaUnit,
    int? AuthoritativeLevel, long ProgressionRevision,
    decimal CurrentArea, decimal ProposedTotalArea);
public sealed record TerritoryCapacityResult(
    bool CanFit, string Code, string ScheduleVersion,
    decimal? MaximumArea, decimal? RemainingArea, bool ExistingAreaExceedsCapacity);

public static class AllianceTerritoryCapacity
{
    public static TerritoryCapacityResult Evaluate(
        TerritoryCapacitySchedule? schedule, TerritoryCapacityInput? input)
    {
        // Fail closed; absence of capacity data is not a zero-sized real territory.
        TerritoryCapacityResult Reject(string code) => new(false, code,
            schedule?.Version ?? string.Empty, null, null, false);
        if (input is null) return Reject("missing_input");
        if (input.AllianceId == Guid.Empty || string.IsNullOrWhiteSpace(input.WorldId))
            return Reject("invalid_identity");
        if (input.AuthoritativeLevel is null) return Reject("progression_unavailable");
        if (input.AuthoritativeLevel < 1 || input.ProgressionRevision < 0)
            return Reject("invalid_progression");
        if (input.CurrentArea < 0 || input.ProposedTotalArea < 0)
            return Reject("invalid_area");
        if (schedule is null) return Reject("schedule_unavailable");
        if (string.IsNullOrWhiteSpace(schedule.Version)
            || string.IsNullOrWhiteSpace(schedule.SurfaceId)
            || string.IsNullOrWhiteSpace(schedule.AreaUnit)
            || schedule.Tiers is null || schedule.Tiers.Count == 0)
            return Reject("invalid_schedule");
        if (!string.Equals(schedule.SurfaceId, input.SurfaceId, StringComparison.Ordinal)
            || !string.Equals(schedule.AreaUnit, input.AreaUnit, StringComparison.Ordinal))
            return Reject("coordinate_scope_mismatch");

        decimal? maximum = null;
        int previousLevel = 0;
        decimal previousArea = 0;
        foreach (TerritoryCapacityTier tier in schedule.Tiers)
        {
            if (tier is null || tier.MinimumLevel <= previousLevel
                || tier.MaximumArea <= previousArea)
                return Reject("invalid_schedule");
            previousLevel = tier.MinimumLevel;
            previousArea = tier.MaximumArea;
            if (tier.MinimumLevel <= input.AuthoritativeLevel.Value)
                maximum = tier.MaximumArea;
        }
        if (maximum is null) return Reject("level_not_configured");
        decimal remaining = Math.Max(0, maximum.Value - input.CurrentArea);
        bool over = input.CurrentArea > maximum.Value;
        bool fits = input.ProposedTotalArea <= maximum.Value;
        return new(fits, fits ? "within_capacity" : "capacity_exceeded",
            schedule.Version, maximum, remaining, over);
        // Even when over capacity, this evaluator never removes land. Reduction,
        // capture and growth are separate server commands requiring explicit rules.
    }
}

public enum FloralTerritorialLinkKind { PublicSite, AllianceInstallation }
public sealed record FloralTerritorialLinkInput(
    FloralTerritorialLinkKind Kind,
    string SiteWorldId, string TerritoryWorldId,
    string SiteSurfaceId, string TerritorySurfaceId,
    Guid InstallationAllianceId, Guid TerritoryAllianceId,
    bool? AuthoritativeContainment);
public sealed record FloralTerritorialLinkResult(bool Allowed, string Code);

/// <summary>
/// Checks a declared link, not geometry or access rights. Public floral sites are
/// never converted into alliance property by proximity or enclosure.
/// </summary>
public static class FloralTerritorialLink
{
    public static FloralTerritorialLinkResult Evaluate(FloralTerritorialLinkInput? input)
    {
        if (input is null) return new(false, "missing_input");
        if (input.Kind == FloralTerritorialLinkKind.PublicSite)
            return new(false, "public_site_remains_public");
        if (input.Kind != FloralTerritorialLinkKind.AllianceInstallation)
            return new(false, "unsupported_link_kind");
        if (string.IsNullOrWhiteSpace(input.SiteWorldId)
            || string.IsNullOrWhiteSpace(input.SiteSurfaceId)
            || !string.Equals(input.SiteWorldId, input.TerritoryWorldId, StringComparison.Ordinal)
            || !string.Equals(input.SiteSurfaceId, input.TerritorySurfaceId, StringComparison.Ordinal))
            return new(false, "coordinate_scope_mismatch");
        if (input.InstallationAllianceId == Guid.Empty
            || input.InstallationAllianceId != input.TerritoryAllianceId)
            return new(false, "alliance_mismatch");
        if (input.AuthoritativeContainment is null)
            return new(false, "geometry_unavailable");
        return input.AuthoritativeContainment.Value
            ? new(true, "installation_within_territory")
            : new(false, "installation_outside_territory");
    }
}
