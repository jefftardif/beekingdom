using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Research;

// M051-CL: the ONE place completed Alliance Research is turned into a gameplay-usable bonus.
// Resolved fresh on every call from real current membership - never cached, never baked into a
// player's own stats, so "bonus applies only while currently a member" (the mission's explicit
// requirement) falls out naturally: no active membership => AllianceResearchBonus.None,
// regardless of what that player contributed in the past or belonged to before.
public sealed class AllianceResearchBonusResolver
{
    private readonly IAllianceRepository allianceRepository;
    private readonly IAllianceResearchRepository researchRepository;

    public AllianceResearchBonusResolver(IAllianceRepository allianceRepository, IAllianceResearchRepository researchRepository)
    {
        this.allianceRepository = allianceRepository;
        this.researchRepository = researchRepository;
    }

    public async Task<AllianceResearchBonus> ResolveForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(playerId);
        if (membership == null) return AllianceResearchBonus.None;
        return await ResolveForAllianceAsync(membership.AllianceId.Value, cancellationToken);
    }

    public async Task<AllianceResearchBonus> ResolveForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default)
    {
        AllianceResearchState? state = await researchRepository.ReadAsync(allianceId, cancellationToken);
        if (state == null || state.Technologies.Count == 0) return AllianceResearchBonus.None;

        long productionBp = 0, capacityBp = 0, combatPowerBp = 0;
        foreach (AllianceTechnologyProgress progress in state.Technologies.Values)
        {
            if (!progress.Completed) continue;
            if (!AllianceResearchCatalog.TryGet(progress.TechnologyId, out AllianceResearchCatalog.TechnologyDefinition definition)) continue;
            productionBp += definition.ProductionBp;
            capacityBp += definition.CapacityBp;
            combatPowerBp += definition.CombatPowerBp;
        }
        return new AllianceResearchBonus(productionBp, capacityBp, combatPowerBp);
    }
}
