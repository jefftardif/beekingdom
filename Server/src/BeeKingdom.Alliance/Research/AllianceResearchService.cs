using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Alliance.Research;

// M051-CL: a donation spans TWO aggregates - the donating player's own PlayerHiveState (resources
// debited) and the shared AllianceResearchState (progress incremented, contribution recorded).
// There is no cross-aggregate distributed transaction in this codebase (PlayerHiveState and Alliance
// research are different persistence keys/locks), so this service applies the same practical,
// documented compromise AllianceHelpService already established for its own two-aggregate
// contribute flow: the step with the real risk of "paid for nothing" is done first and made
// idempotent on its own (resource debit, guarded by PlayerHiveState.Receipts - the same mechanism
// every other paid action in this codebase already uses), then the Alliance-side progress increment
// is applied, itself independently idempotent (ProcessedDonationIds). If a technology was completed
// by a concurrent donation between this player's pre-check and their own atomic increment, their
// resources are still spent for a real reason: their contribution total still increases (visible,
// honest, never silently dropped) even though that specific technology's progress could not advance
// further past its requirement - there is no scenario where resources vanish with literally nothing
// recorded for the player.
public sealed class AllianceResearchService
{
    private readonly IAllianceRepository allianceRepository;
    private readonly IAllianceResearchRepository researchRepository;
    private readonly IHiveStateRepository hiveStateRepository;
    private readonly IOptions<AllianceResearchOptions> options;
    private readonly IServerClock clock;
    private readonly ILogger<AllianceResearchService>? logger;

    public AllianceResearchService(
        IAllianceRepository allianceRepository,
        IAllianceResearchRepository researchRepository,
        IHiveStateRepository hiveStateRepository,
        IOptions<AllianceResearchOptions> options,
        IServerClock clock,
        ILogger<AllianceResearchService>? logger = null)
    {
        this.allianceRepository = allianceRepository;
        this.researchRepository = researchRepository;
        this.hiveStateRepository = hiveStateRepository;
        this.options = options;
        this.clock = clock;
        this.logger = logger;
    }

    private void RequireEnabled()
    {
        if (!options.Value.Enabled) throw new InvalidOperationException("alliance_research_disabled");
    }

    public async Task<AllianceResearchReadSnapshot> GetSnapshotAsync(PlayerId actorPlayerId, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) throw new InvalidOperationException("not_a_member");

        AllianceResearchState state = await researchRepository.ReadAsync(membership.AllianceId.Value, cancellationToken) ?? AllianceResearchState.Empty(membership.AllianceId.Value);
        return BuildSnapshot(state, actorPlayerId);
    }

    public async Task<AllianceResearchDonateResult> DonateAsync(PlayerId actorPlayerId, DonateToAllianceResearchCommand command, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        if (command is null || string.IsNullOrWhiteSpace(command.TechnologyId) || string.IsNullOrWhiteSpace(command.ClientRequestId) || command.HiveId == Guid.Empty)
            return new AllianceResearchDonateResult(false, "invalid_request", null);
        if (!AllianceResearchCatalog.TryGet(command.TechnologyId, out AllianceResearchCatalog.TechnologyDefinition definition))
            return new AllianceResearchDonateResult(false, "technology_not_found", null);

        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return new AllianceResearchDonateResult(false, "not_a_member", null);

        IReadOnlyList<Guid> ownedHiveIds = await hiveStateRepository.ListHiveIdsAsync(actorPlayerId.Value, cancellationToken);
        if (!ownedHiveIds.Contains(command.HiveId)) return new AllianceResearchDonateResult(false, "hive_not_owned", null);

        // Fail fast on the obvious cases before ever touching resources - the authoritative gate
        // still lives inside the Alliance-side atomic mutation below (see class comment).
        AllianceResearchState precheckState = await researchRepository.ReadAsync(membership.AllianceId.Value, cancellationToken) ?? AllianceResearchState.Empty(membership.AllianceId.Value);
        if (!AllianceResearchCatalog.PrerequisitesMet(definition, precheckState.Technologies))
            return new AllianceResearchDonateResult(false, "technology_locked", null);
        if (precheckState.Technologies.TryGetValue(definition.TechnologyId, out AllianceTechnologyProgress? existingProgress) && existingProgress.Completed)
            return new AllianceResearchDonateResult(false, "technology_completed", null);

        // Step 1/2: debit the player's REAL resources, atomically, idempotent via the same
        // Receipts mechanism every other paid action already uses (see e.g. ChampionBee level-up).
        string idempotencyKey = "alliance-research-donate:" + command.ClientRequestId;
        string debitCode = "not_run";
        await hiveStateRepository.ExecuteAtomicallyAsync(actorPlayerId.Value, command.HiveId, state =>
        {
            if (state.Receipts.TryGetValue(idempotencyKey, out IdempotencyReceipt? stored))
            {
                debitCode = stored.Code;
                return state;
            }

            Dictionary<string, ResourceBalance> resources = new(state.Resources);
            foreach ((string resourceKey, long cost) in definition.DonationCost)
            {
                ResourceBalance balance = resources.GetValueOrDefault(resourceKey, new ResourceBalance(0, 0));
                if (balance.Amount < cost)
                {
                    debitCode = "insufficient_resources";
                    return state;
                }
            }
            foreach ((string resourceKey, long cost) in definition.DonationCost)
            {
                ResourceBalance balance = resources.GetValueOrDefault(resourceKey, new ResourceBalance(0, 0));
                resources[resourceKey] = balance with { Amount = balance.Amount - cost };
            }

            debitCode = "debited";
            Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts)
            {
                [idempotencyKey] = new IdempotencyReceipt("alliance-research-donate", true, debitCode, null, clock.UtcNow)
            };
            return state with { Revision = state.Revision + 1, Resources = resources, Receipts = receipts };
        }, cancellationToken);

        if (debitCode == "insufficient_resources")
            return new AllianceResearchDonateResult(false, "insufficient_resources", null);

        // Step 2/2: apply Alliance progress + contribution, atomically, independently idempotent
        // via ProcessedDonationIds (guards a retry of THIS step alone, e.g. the client never saw
        // step 1's success and retries the whole donation with the same ClientRequestId).
        string donationKey = actorPlayerId.Value.ToString("N") + ":" + command.ClientRequestId;
        AllianceResearchState updatedResearch = await researchRepository.ExecuteAtomicallyAsync(membership.AllianceId.Value, state =>
        {
            if (state.ProcessedDonationIds.Contains(donationKey)) return state;

            Dictionary<string, AllianceTechnologyProgress> technologies = new(state.Technologies, StringComparer.Ordinal);
            technologies.TryGetValue(definition.TechnologyId, out AllianceTechnologyProgress? current);
            current ??= new AllianceTechnologyProgress(definition.TechnologyId, 0, false, null);

            DateTimeOffset now = clock.UtcNow;
            HashSet<string> processed = new(state.ProcessedDonationIds, StringComparer.Ordinal) { donationKey };

            Dictionary<Guid, AllianceResearchContribution> contributions = new(state.Contributions);
            AllianceResearchContribution contribution = contributions.GetValueOrDefault(actorPlayerId.Value, new AllianceResearchContribution(actorPlayerId.Value, 0, 0));
            contributions[actorPlayerId.Value] = contribution with
            {
                TotalPoints = contribution.TotalPoints + definition.DonationProgressPerDonation,
                DonationCount = contribution.DonationCount + 1
            };

            // The technology may have been completed by a concurrent donation between this
            // request's pre-check and this atomic mutation - the contribution above still counts
            // (real resources were really spent), but progress itself never exceeds RequiredProgress
            // and a completed technology is never re-opened.
            if (!current.Completed && !AllianceResearchCatalog.PrerequisitesMet(definition, technologies))
            {
                // Prerequisite was reverted somehow (should not happen - technologies never
                // un-complete) - defensive no-op, contribution already recorded above.
            }
            else if (!current.Completed)
            {
                long newProgress = Math.Min(current.CurrentProgress + definition.DonationProgressPerDonation, definition.RequiredProgress);
                bool nowCompleted = newProgress >= definition.RequiredProgress;
                technologies[definition.TechnologyId] = current with
                {
                    CurrentProgress = newProgress,
                    Completed = nowCompleted,
                    CompletedAtUtc = nowCompleted ? now : current.CompletedAtUtc
                };
            }

            return state with { Revision = state.Revision + 1, Technologies = technologies, Contributions = contributions, ProcessedDonationIds = processed };
        }, cancellationToken);

        bool justCompleted = updatedResearch.Technologies.TryGetValue(definition.TechnologyId, out AllianceTechnologyProgress? finalProgress) && finalProgress.Completed;
        logger?.LogInformation("Alliance Research donation: {PlayerId} donated to {TechnologyId} in alliance {AllianceId} (completed={Completed}).",
            actorPlayerId.Value, definition.TechnologyId, membership.AllianceId.Value, justCompleted);

        return new AllianceResearchDonateResult(true, "donation_applied", BuildSnapshot(updatedResearch, actorPlayerId));
    }

    private static AllianceResearchReadSnapshot BuildSnapshot(AllianceResearchState state, PlayerId actorPlayerId)
    {
        List<AllianceTechnologyReadModel> technologies = [];
        foreach (AllianceResearchCatalog.TechnologyDefinition definition in AllianceResearchCatalog.Technologies)
        {
            state.Technologies.TryGetValue(definition.TechnologyId, out AllianceTechnologyProgress? progress);
            bool completed = progress?.Completed ?? false;
            bool locked = !completed && !AllianceResearchCatalog.PrerequisitesMet(definition, state.Technologies);
            technologies.Add(new AllianceTechnologyReadModel(
                definition.TechnologyId, definition.Branch, definition.Tier, definition.DisplayNameKey, definition.DescriptionKey,
                definition.BonusSummaryKey, definition.RequiredProgress, progress?.CurrentProgress ?? 0, completed, progress?.CompletedAtUtc,
                definition.PrerequisiteIds, locked, !locked && !completed, definition.DonationCost, definition.DonationProgressPerDonation));
        }

        state.Contributions.TryGetValue(actorPlayerId.Value, out AllianceResearchContribution? myContribution);
        return new AllianceResearchReadSnapshot(state.AllianceId, AllianceResearchCatalog.ContractVersion, DateTimeOffset.UtcNow, state.Revision,
            technologies, myContribution?.TotalPoints ?? 0, myContribution?.DonationCount ?? 0);
    }
}
