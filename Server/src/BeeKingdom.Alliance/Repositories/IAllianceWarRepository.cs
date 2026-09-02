using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public interface IAllianceWarRepository
{
    AllianceWar Save(AllianceWar war);
    AllianceWar? Get(Guid warId);
    IReadOnlyList<AllianceWar> ListActiveForAlliance(AllianceId allianceId);
    bool HasActiveWarBetween(AllianceId allianceA, AllianceId allianceB);

    Guid? GetDeclareReceipt(PlayerId actorPlayerId, string clientRequestId);
    void SaveDeclareReceipt(PlayerId actorPlayerId, string clientRequestId, Guid warId);
}
