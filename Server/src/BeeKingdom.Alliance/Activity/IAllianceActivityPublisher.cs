using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Activity;

// M041-CL: the ingestion seam for future player-gameplay events (building upgrades, research,
// attacks, gathering) becoming Alliance activity. Nothing calls PublishForPlayerAsync yet - no
// BuildingUpgradeCompleted/ResearchCompleted/AttackResolved wiring exists to call it from - but
// the abstraction exists now so that wiring, when it's built, only needs to call this interface
// instead of reaching into AllianceService/IAllianceActivityRepository directly. Silently no-ops
// if the player has no alliance (not every player needs one).
public interface IAllianceActivityPublisher
{
    Task PublishForPlayerAsync(PlayerId playerId, AllianceActivityType type, AllianceActivityPayload? payload, string dedupeKey, CancellationToken cancellationToken = default);
}

public sealed class AllianceActivityPublisher : IAllianceActivityPublisher
{
    private readonly IAllianceRepository allianceRepository;
    private readonly IAllianceActivityRepository activityRepository;

    public AllianceActivityPublisher(IAllianceRepository allianceRepository, IAllianceActivityRepository activityRepository)
    {
        this.allianceRepository = allianceRepository;
        this.activityRepository = activityRepository;
    }

    public Task PublishForPlayerAsync(PlayerId playerId, AllianceActivityType type, AllianceActivityPayload? payload, string dedupeKey, CancellationToken cancellationToken = default)
    {
        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(playerId);
        if (membership == null) return Task.CompletedTask;

        activityRepository.AppendIdempotent(new AllianceActivityEvent
        {
            ActivityId = Guid.NewGuid(),
            AllianceId = membership.AllianceId,
            Type = type,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActorPlayerId = playerId,
            Visibility = AllianceActivityVisibility.MembersOnly,
            Payload = payload,
            Sequence = 0 // assigned by the repository
        }, dedupeKey);

        return Task.CompletedTask;
    }
}
