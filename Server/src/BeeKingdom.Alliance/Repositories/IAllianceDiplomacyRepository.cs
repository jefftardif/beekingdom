using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public interface IAllianceDiplomacyRepository
{
    AllianceDiplomaticRelation Save(AllianceDiplomaticRelation relation);

    // Looks up by the canonical (unordered) pair - callers pass the two alliance ids in any
    // order, the repository normalizes internally.
    AllianceDiplomaticRelation? GetRelation(AllianceId allianceA, AllianceId allianceB);
    IReadOnlyList<AllianceDiplomaticRelation> ListForAlliance(AllianceId allianceId);

    Guid? GetProposalReceipt(PlayerId actorPlayerId, string clientRequestId);
    void SaveProposalReceipt(PlayerId actorPlayerId, string clientRequestId, Guid relationId);
}
