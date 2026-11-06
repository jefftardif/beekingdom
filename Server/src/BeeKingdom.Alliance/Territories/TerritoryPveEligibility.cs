using System;
using System.IO;

namespace BeeKingdom.Alliance.Territories;

public enum TerritorialOpponentKind { Unknown, Creature, Player }
public enum TerritoryPositionReference { Unspecified, Hive, Army }
public sealed record TerritorialActorContext(Guid CurrentAllianceId, bool CurrentMembershipConfirmed,
    string WorldId, string SurfaceId, Guid PlayerId);
public sealed record TerritorialPositionContext(TerritoryPositionReference Reference,
    string WorldId, string SurfaceId, TerritoryPoint Position, Guid HiveId, Guid OwnerPlayerId, long PositionRevision);
public sealed record TerritorialCreatureContext(TerritorialOpponentKind Kind,
    string WorldId, string SurfaceId, TerritoryPoint Position);
public sealed record TerritoryEligibility(bool Eligible, string Code);

/// <summary>
/// Internal gates only: no combat modifier, raid mutation, entitlement or numeric
/// balancing. All contexts come from current server records, not client assertions.
/// Admission applies only at join time; it does not expel an existing participant.
/// </summary>
public static class TerritoryPveEligibility
{
    public static TerritoryEligibility CreatureAttack(TerritoryAuthorityState? territory,
        TerritorialActorContext? actor, TerritorialCreatureContext? target)
    {
        // Check opponent type before considering any territorial advantage: PvP
        // and unknown targets can never receive a creature-only attack modifier.
        if (target is null || target.Kind != TerritorialOpponentKind.Creature)
            return new(false, "creature_target_required");
        var access = CheckTerritory(territory, actor);
        if (!access.Eligible) return access;
        if (target.WorldId != territory!.WorldId || target.SurfaceId != territory.SurfaceId)
            return new(false, "target_scope_mismatch");
        return TerritoryGeometry.ContainsValidated(territory.Ring, target.Position)
            ? new(true, "creature_inside_territory") : new(false, "creature_outside_territory");
    }

    public static TerritoryEligibility JoinCreatureRaid(TerritoryAuthorityState? territory,
        TerritorialActorContext? actor, TerritorialPositionContext? actorPosition,
        TerritorialOpponentKind raidTargetKind)
    {
        if (raidTargetKind != TerritorialOpponentKind.Creature) return new(false, "creature_raid_required");
        var access = CheckTerritory(territory, actor);
        if (!access.Eligible) return access;
        // Product rule confirmed: the player's HIVE must be in their alliance's
        // territory at join time. Army position is never a substitute.
        if (actorPosition is null || actorPosition.Reference != TerritoryPositionReference.Hive)
            return new(false, "raid_position_source_mismatch");
        if (actorPosition.HiveId == Guid.Empty || actorPosition.OwnerPlayerId != actor!.PlayerId || actorPosition.PositionRevision < 0)
            return new(false, "raid_hive_ownership_mismatch");
        if (actorPosition.WorldId != territory!.WorldId || actorPosition.SurfaceId != territory.SurfaceId)
            return new(false, "actor_position_scope_mismatch");
        return TerritoryGeometry.ContainsValidated(territory.Ring, actorPosition.Position)
            ? new(true, "raid_join_position_inside") : new(false, "raid_join_position_outside");
        // No restriction on the raid target's position is invented here. The approved
        // creature attack bonus checks its target separately in CreatureAttack.
    }

    private static TerritoryEligibility CheckTerritory(TerritoryAuthorityState? territory, TerritorialActorContext? actor)
    {
        if (territory is null || !territory.Active) return new(false, "territory_unavailable");
        if (actor is null || actor.PlayerId == Guid.Empty || !actor.CurrentMembershipConfirmed || actor.CurrentAllianceId == Guid.Empty
            || actor.CurrentAllianceId != territory.AllianceId)
            return new(false, "current_alliance_membership_required");
        if (actor.WorldId != territory.WorldId || actor.SurfaceId != territory.SurfaceId)
            return new(false, "actor_scope_mismatch");
        if (territory.Headquarters is null || !territory.Headquarters.Operational)
            return new(false, "headquarters_not_operational");
        try { DurableTerritoryAuthorityStore.Validate(territory); }
        catch (InvalidDataException ex) { return new(false, ex.Message); }
        catch (ArgumentException) { return new(false, "invalid_territory_key"); }
        var capacity = AllianceTerritoryCapacity.Evaluate(territory.Schedule,
            new(territory.AllianceId,territory.WorldId,territory.SurfaceId,territory.AreaUnit,
                territory.TerritorialLevel,territory.ProgressionRevision,0m,0m));
        var building = HeadquartersCapacity.Evaluate(territory.TerritorialLevel,capacity.MaximumArea!.Value,
            territory.Headquarters,territory.HeadquartersSchedule);
        if (!building.Available) return new(false,building.Code);
        return new(true, "territory_context_valid");
    }
}
