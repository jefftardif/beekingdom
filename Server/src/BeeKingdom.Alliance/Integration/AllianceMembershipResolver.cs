using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Integration;

// M042-CL: the real, server-authoritative implementation of BeeKingdom.Chat's
// IAllianceMembershipResolver seam - wraps IAllianceRepository.GetActiveMembership directly, so
// alliance/leaders chat access is decided from the exact same membership rows AllianceService
// itself enforces (never a second, potentially-drifting copy of "who's in this alliance").
public sealed class AllianceMembershipResolver : IAllianceMembershipResolver
{
    private readonly IAllianceRepository repository;

    public AllianceMembershipResolver(IAllianceRepository repository)
    {
        this.repository = repository;
    }

    public ChatPermissionRole? GetMemberRole(Guid allianceId, Guid playerId)
    {
        AllianceMembership? membership = repository.GetActiveMembership(new AllianceId(allianceId), new PlayerId(playerId));
        return membership?.Role switch
        {
            AllianceRole.Leader => ChatPermissionRole.Leader,
            AllianceRole.Officer => ChatPermissionRole.Officer,
            AllianceRole.Member => ChatPermissionRole.Member,
            _ => null
        };
    }
}
