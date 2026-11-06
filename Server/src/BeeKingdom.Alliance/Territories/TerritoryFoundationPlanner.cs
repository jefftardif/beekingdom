using System;
using System.Collections.Generic;

namespace BeeKingdom.Alliance.Territories;

public enum TerritoryFoundationRole { Member, Officer, Leader }
public sealed record TerritoryFoundationPolicy(bool Enabled, bool OfficersAllowed,
    string Version, decimal InitialAreaFraction, string BuildingBlueprintKey);
public sealed record TerritoryFoundationContext(Guid PlayerId, Guid AllianceId,
    TerritoryFoundationRole Role, bool CurrentMembershipConfirmed, bool WorldMembershipConfirmed,
    string WorldId, string SurfaceId, string AreaUnit, string ProgressionSource,
    int AllianceLevel, long AllianceProgressionRevision, long WorldPlacementRevision,
    TerritoryMapBounds Bounds, TerritoryCapacitySchedule AllianceSchedule,
    HeadquartersCapacitySchedule HeadquartersSchedule);
public sealed record TerritoryFoundationPlan(bool Ready, string Code, Guid PlayerId, Guid AllianceId,
    string WorldId, string SurfaceId, string PolicyVersion, string BuildingBlueprintKey,
    long WorldPlacementRevision, TerritoryPoint? HeadquartersPosition,
    IReadOnlyList<TerritoryPoint>? ProposedRing, decimal? ProposedArea);

/// <summary>
/// Server-side foundation preparation, not a purchase, completed building or land grant.
/// Requires explicit tuning and a current authoritative placement check over the WHOLE
/// footprint. A later transaction must recheck these revisions and atomically reserve
/// the site, create the construction entity and record its idempotency receipt.
/// Candidate shape is a square centred on the building; no radial marker is used as ownership.
/// </summary>
public static class TerritoryFoundationPlanner
{
    public static TerritoryFoundationPlan Prepare(TerritoryFoundationPolicy? policy,
        TerritoryFoundationContext? context, TerritoryPoint requestedPosition,
        Func<IReadOnlyList<TerritoryPoint>,long,bool>? placementIsAdmissible)
    {
        TerritoryFoundationPlan No(string code) => new(false,code,Guid.Empty,Guid.Empty,"","","","",0,null,null,null);
        if(policy is null || !policy.Enabled) return No("foundation_disabled");
        if(string.IsNullOrWhiteSpace(policy.Version) || string.IsNullOrWhiteSpace(policy.BuildingBlueprintKey)
            || policy.InitialAreaFraction<=0 || policy.InitialAreaFraction>1) return No("foundation_policy_unavailable");
        if(context is null || context.PlayerId==Guid.Empty || context.AllianceId==Guid.Empty
            || !context.CurrentMembershipConfirmed) return No("foundation_membership_required");
        if(context.Role!=TerritoryFoundationRole.Leader
            && !(context.Role==TerritoryFoundationRole.Officer && policy.OfficersAllowed)) return No("foundation_role_denied");
        if(!context.WorldMembershipConfirmed || string.IsNullOrWhiteSpace(context.WorldId)
            || string.IsNullOrWhiteSpace(context.SurfaceId) || context.WorldPlacementRevision<0)
            return No("foundation_world_unavailable");
        if(string.IsNullOrWhiteSpace(context.ProgressionSource)) return No("foundation_progression_unavailable");
        var alliance=AllianceTerritoryCapacity.Evaluate(context.AllianceSchedule,
            new(context.AllianceId,context.WorldId,context.SurfaceId,context.AreaUnit,context.AllianceLevel,
                context.AllianceProgressionRevision,0m,0m));
        if(!alliance.CanFit) return No(alliance.Code);
        // Level 1 is a TARGET for construction, not an already-earned building level.
        var first=HeadquartersCapacity.EvaluateConstructionTarget(context.AllianceLevel,alliance.MaximumArea!.Value,
            1,context.HeadquartersSchedule);
        if(!first.Available) return No(first.Code);
        if(placementIsAdmissible is null) return No("foundation_placement_authority_unavailable");
        decimal area=first.MaximumArea!.Value*policy.InitialAreaFraction;
        decimal half=(decimal)Math.Sqrt((double)(area/4m));
        half=Math.Min(half,1000000m);
        half=decimal.Floor(half*1000000m)/1000000m;
        while(4m*half*half>area) half-=0.000001m;
        if(half<=0) return No("foundation_footprint_unsupported");
        if(Math.Abs(requestedPosition.X)>1000000m || Math.Abs(requestedPosition.Y)>1000000m)
            return No("foundation_position_unsupported");
        var ring=new[]{new TerritoryPoint(requestedPosition.X-half,requestedPosition.Y-half),
            new TerritoryPoint(requestedPosition.X+half,requestedPosition.Y-half),
            new TerritoryPoint(requestedPosition.X+half,requestedPosition.Y+half),
            new TerritoryPoint(requestedPosition.X-half,requestedPosition.Y+half)};
        var geometry=TerritoryGeometry.Validate(ring,context.Bounds);
        if(!geometry.Valid) return No(geometry.Code);
        var immutable=Array.AsReadOnly(ring);
        if(!placementIsAdmissible(immutable,context.WorldPlacementRevision)) return No("foundation_destination_denied");
        return new(true,"foundation_plan_ready",context.PlayerId,context.AllianceId,context.WorldId,context.SurfaceId,
            policy.Version,policy.BuildingBlueprintKey,context.WorldPlacementRevision,requestedPosition,immutable,geometry.Area);
    }
}
