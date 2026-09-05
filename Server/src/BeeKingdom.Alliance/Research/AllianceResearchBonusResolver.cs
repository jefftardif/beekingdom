using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.HiveOperations;
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
    private readonly IServerClock clock;

    public AllianceResearchBonusResolver(IAllianceRepository allianceRepository, IAllianceResearchRepository researchRepository, IServerClock clock)
    {
        this.allianceRepository = allianceRepository;
        this.researchRepository = researchRepository;
        this.clock = clock;
    }

    public async Task<AllianceResearchBonus> ResolveForPlayerAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(playerId);
        if (membership == null) return AllianceResearchBonus.None;
        return await ResolveForAllianceAsync(membership.AllianceId.Value, cancellationToken);
    }

    // M052-CL: reads ONLY state.Completed - never Funding/Ready/Researching (Bible "critical
    // difference #6": a technology grants nothing until its research timer actually finishes).
    // This is a read-only path (no ExecuteAtomicallyAsync), so it does NOT itself lazily resolve a
    // just-elapsed timer into Completed - that happens the next time any player's own
    // AllianceResearchService.GetSnapshotAsync call touches this Alliance (e.g. opening the
    // Alliance Center). A technology whose timer elapsed slightly before anyone last opened that
    // screen can therefore lag briefly before its bonus is observed here - a documented, bounded
    // staleness window, not a correctness bug (see M052 report "known compromises"): applying a
    // write-path resolution on every single production/combat calculation across every player
    // would trade a rare, small, self-healing lag for a real performance cost on the hottest paths
    // in the game.
    public async Task<AllianceResearchBonus> ResolveForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default)
    {
        AllianceResearchState? state = await researchRepository.ReadAsync(allianceId, cancellationToken);
        if (state == null) return AllianceResearchBonus.None;

        // M053-CL: close the "must reopen Alliance Center to receive an already-earned bonus" gap
        // documented as a known compromise in the M052 report - a research slot whose CompletesAtUtc
        // has already passed is, by Bible definition (section 2/3), objectively TERMINÉE even if no
        // mutation has yet persisted that fact into state.Completed. Counting it here is purely
        // additive and READ-ONLY (no write, no lazy-resolution side effect) - the authoritative
        // write-path resolution (AllianceResearchService.ResolveElapsedResearch, which actually moves
        // the slot into Completed and clears it) still happens exactly once, the next time any
        // request touches this Alliance. This intentionally avoids adding a write on the hottest
        // calculation path in the game (every production/combat tick) while still making the bonus
        // available from the instant it is objectively true, not from the instant someone happens to
        // open the Alliance Center. No polling, no background worker - purely a function of "now".
        HashSet<string> effectivelyCompleted = new(state.Completed.Keys, StringComparer.Ordinal);
        DateTimeOffset now = clock.UtcNow;
        if (state.MinorResearch != null && now >= state.MinorResearch.CompletesAtUtc) effectivelyCompleted.Add(state.MinorResearch.TechnologyId);
        if (state.MajorResearch != null && now >= state.MajorResearch.CompletesAtUtc) effectivelyCompleted.Add(state.MajorResearch.TechnologyId);
        if (effectivelyCompleted.Count == 0) return AllianceResearchBonus.None;

        long productionBp = 0, capacityBp = 0, combatPowerBp = 0;
        foreach (string technologyId in effectivelyCompleted)
        {
            if (!AllianceResearchCatalog.TryGet(technologyId, out AllianceResearchCatalog.TechnologyDefinition definition)) continue;
            productionBp += definition.ProductionBp;
            capacityBp += definition.CapacityBp;
            combatPowerBp += definition.CombatPowerBp;
        }
        return new AllianceResearchBonus(productionBp, capacityBp, combatPowerBp);
    }
}
