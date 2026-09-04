using BeeKingdom.Alliance.Activity;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Category = BeeKingdom.Alliance.Research.AllianceResearchCatalog.ResearchCategory;

namespace BeeKingdom.Alliance.Research;

// M052-CL: Bible-aligned service (BIBLE_ALLIANCE_RESEARCH.md V1.0). Owns the full lifecycle -
// Chef-only funding-target selection, member donations (clamped to the real remaining need, never
// overshooting), Chef/Officer-authorized launch, server-authoritative timer resolution (lazy,
// deterministic, idempotent - see ResolveElapsedResearch), and Alliance Research SpeedUp
// application. Two independent category slots (Minor/Major) per Bible section 7.
//
// ATOMICITY (same documented compromise M051 established, reviewed and preserved under the new
// funding model - see M052 report "known compromises" for the honest failure-mode statement):
// donation and speedup both debit the PLAYER's own PlayerHiveState first (idempotent via
// Receipts), then mutate the ALLIANCE-owned AllianceResearchState second (independently idempotent
// via its own Processed*Ids sets). There is still no distributed transaction across the two
// aggregates - if the process dies between steps, the player's resources are already spent and
// the Alliance-side effect is merely pending (recoverable by retrying the SAME ClientRequestId,
// which replays step 1 harmlessly and applies step 2). This is the same "never resources-for-nothing,
// but not textbook two-phase-commit" guarantee M051 documented, not an upgrade to full atomicity.
public sealed class AllianceResearchService
{
    private readonly IAllianceRepository allianceRepository;
    private readonly IAllianceResearchRepository researchRepository;
    private readonly IHiveStateRepository hiveStateRepository;
    private readonly IOptions<AllianceResearchOptions> options;
    private readonly IServerClock clock;
    private readonly IAllianceActivityPublisher? activityPublisher;
    private readonly ILogger<AllianceResearchService>? logger;

    public AllianceResearchService(
        IAllianceRepository allianceRepository,
        IAllianceResearchRepository researchRepository,
        IHiveStateRepository hiveStateRepository,
        IOptions<AllianceResearchOptions> options,
        IServerClock clock,
        IAllianceActivityPublisher? activityPublisher = null,
        ILogger<AllianceResearchService>? logger = null)
    {
        this.allianceRepository = allianceRepository;
        this.researchRepository = researchRepository;
        this.hiveStateRepository = hiveStateRepository;
        this.options = options;
        this.clock = clock;
        this.activityPublisher = activityPublisher;
        this.logger = logger;
    }

    private void RequireEnabled()
    {
        if (!options.Value.Enabled) throw new InvalidOperationException("alliance_research_disabled");
    }

    // ---------------- Read ----------------

    public async Task<AllianceResearchReadSnapshot> GetSnapshotAsync(PlayerId actorPlayerId, CancellationToken ct = default)
    {
        RequireEnabled();
        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) throw new InvalidOperationException("not_a_member");

        DateTimeOffset now = clock.UtcNow;
        (AllianceResearchState state, List<(string TechnologyId, Category Category)> justCompleted) =
            await ResolveAndPersistElapsedResearchAsync(membership.AllianceId.Value, now, ct);
        await PublishCompletionsAsync(actorPlayerId, justCompleted, ct);

        return BuildSnapshot(state, actorPlayerId, membership.Role, now);
    }

    // ---------------- Chef: select/change funding target ----------------

    public async Task<AllianceResearchCommandResult> SelectFundingTargetAsync(PlayerId actorPlayerId, SelectAllianceResearchFundingTargetCommand command, CancellationToken ct = default)
    {
        RequireEnabled();
        if (command is null || string.IsNullOrWhiteSpace(command.TechnologyId) || string.IsNullOrWhiteSpace(command.ClientRequestId))
            return Fail("invalid_request");
        if (!AllianceResearchCatalog.TryGet(command.TechnologyId, out AllianceResearchCatalog.TechnologyDefinition definition))
            return Fail("technology_not_found");

        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return Fail("not_a_member");
        // Bible section 4: exclusively the Chef - not even an Officer may select/change the target.
        if (membership.Role != AllianceRole.Leader) return Fail("not_authorized");

        DateTimeOffset now = clock.UtcNow;
        string? code = null;
        AllianceResearchState updated = await researchRepository.ExecuteAtomicallyAsync(membership.AllianceId.Value, state =>
        {
            (state, _) = ResolveElapsedResearch(state, now);
            HashSet<string> completedIds = state.Completed.Keys.ToHashSet(StringComparer.Ordinal);
            if (state.Completed.ContainsKey(definition.TechnologyId)) { code = "technology_completed"; return state; }
            if (!AllianceResearchCatalog.PrerequisitesMet(definition, completedIds)) { code = "technology_locked"; return state; }
            AllianceResearchSlot? activeSlot = definition.Category == Category.Minor ? state.MinorResearch : state.MajorResearch;
            if (activeSlot?.TechnologyId == definition.TechnologyId) { code = "technology_already_researching"; return state; }

            code = "funding_target_selected";
            return definition.Category == Category.Minor
                ? state with { Revision = state.Revision + 1, MinorFundingTargetId = definition.TechnologyId }
                : state with { Revision = state.Revision + 1, MajorFundingTargetId = definition.TechnologyId };
        }, ct);

        if (code != "funding_target_selected") return new AllianceResearchCommandResult(false, code!, BuildSnapshot(updated, actorPlayerId, membership.Role, now));

        await PublishAsync(actorPlayerId, AllianceActivityType.AllianceResearchFundingTargetSelected,
            new AllianceActivityPayload { EntityKey = definition.TechnologyId },
            "alliance-research-target:" + definition.Category + ":" + command.ClientRequestId, ct);
        return new AllianceResearchCommandResult(true, code, BuildSnapshot(updated, actorPlayerId, membership.Role, now));
    }

    // ---------------- Donate ----------------

    public async Task<AllianceResearchCommandResult> DonateAsync(PlayerId actorPlayerId, DonateToAllianceResearchCommand command, CancellationToken ct = default)
    {
        RequireEnabled();
        if (command is null || string.IsNullOrWhiteSpace(command.TechnologyId) || string.IsNullOrWhiteSpace(command.ResourceKey) ||
            command.Amount <= 0 || command.ClientRequestId is null || command.HiveId == Guid.Empty)
            return Fail("invalid_request");
        if (!AllianceResearchCatalog.TryGet(command.TechnologyId, out AllianceResearchCatalog.TechnologyDefinition definition))
            return Fail("technology_not_found");
        if (!definition.FundingRequirements.TryGetValue(command.ResourceKey, out long required))
            return Fail("invalid_resource");

        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return Fail("not_a_member");
        IReadOnlyList<Guid> ownedHiveIds = await hiveStateRepository.ListHiveIdsAsync(actorPlayerId.Value, ct);
        if (!ownedHiveIds.Contains(command.HiveId)) return Fail("hive_not_owned");

        // Fail fast before ever touching resources - the authoritative gate is still the atomic
        // Alliance-side mutation in step 2 below.
        DateTimeOffset precheckNow = clock.UtcNow;
        AllianceResearchState precheckState = AllianceResearchStateMigrator.ToCurrent(
            await researchRepository.ReadAsync(membership.AllianceId.Value, ct) ?? AllianceResearchState.Empty(membership.AllianceId.Value));
        (precheckState, _) = ResolveElapsedResearch(precheckState, precheckNow);
        string? precheckReject = ValidateDonatable(definition, precheckState, membership.AllianceId.Value);
        if (precheckReject != null) return Fail(precheckReject);
        long precheckRemaining = required - CurrentlyFunded(precheckState, definition.TechnologyId, command.ResourceKey);
        if (precheckRemaining <= 0) return Fail("technology_completed_funding_for_resource");
        long clampedAmount = Math.Min(command.Amount, precheckRemaining);

        // Step 1/2: debit the player's REAL resources, atomically, idempotent via the same
        // Receipts mechanism every other paid action in this codebase already uses.
        string idempotencyKey = "alliance-research-donate:" + command.ClientRequestId;
        string debitCode = "not_run";
        long debitedAmount = 0;
        await hiveStateRepository.ExecuteAtomicallyAsync(actorPlayerId.Value, command.HiveId, state =>
        {
            if (state.Receipts.TryGetValue(idempotencyKey, out IdempotencyReceipt? stored))
            {
                debitCode = stored.Code;
                debitedAmount = clampedAmount; // replay: the same amount was already debited the first time.
                return state;
            }

            ResourceBalance balance = state.Resources.GetValueOrDefault(command.ResourceKey, new ResourceBalance(0, 0));
            if (balance.Amount < clampedAmount)
            {
                debitCode = "insufficient_resources";
                return state;
            }

            Dictionary<string, ResourceBalance> resources = new(state.Resources) { [command.ResourceKey] = balance with { Amount = balance.Amount - clampedAmount } };
            debitCode = "debited";
            debitedAmount = clampedAmount;
            Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [idempotencyKey] = new IdempotencyReceipt("alliance-research-donate", true, debitCode, null, clock.UtcNow) };
            return state with { Revision = state.Revision + 1, Resources = resources, Receipts = receipts };
        }, ct);

        if (debitCode == "insufficient_resources") return Fail("insufficient_resources");

        // Step 2/2: apply the real contribution to the Alliance's shared funding state, atomically,
        // independently idempotent via ProcessedDonationIds.
        string donationKey = actorPlayerId.Value.ToString("N") + ":" + command.ClientRequestId;
        DateTimeOffset now = clock.UtcNow;
        AllianceResearchState updated = await researchRepository.ExecuteAtomicallyAsync(membership.AllianceId.Value, state =>
        {
            (state, _) = ResolveElapsedResearch(state, now);
            if (state.ProcessedDonationIds.Contains(donationKey)) return state;

            Dictionary<string, AllianceTechnologyFunding> funding = new(state.Funding, StringComparer.Ordinal);
            AllianceTechnologyFunding techFunding = funding.TryGetValue(definition.TechnologyId, out AllianceTechnologyFunding? existing) ? existing : AllianceTechnologyFunding.Empty();
            Dictionary<string, long> contributed = new(techFunding.Contributed, StringComparer.Ordinal);
            long already = contributed.GetValueOrDefault(command.ResourceKey);
            long room = Math.Max(0, required - already);
            long applied = Math.Min(debitedAmount, room);
            contributed[command.ResourceKey] = already + applied;
            funding[definition.TechnologyId] = techFunding with { Contributed = contributed };

            Dictionary<Guid, AllianceResearchContribution> contributions = new(state.Contributions);
            AllianceResearchContribution current = contributions.GetValueOrDefault(actorPlayerId.Value, new AllianceResearchContribution(actorPlayerId.Value, 0, 0, 0));
            long currencyAwarded = (long)Math.Floor(applied * options.Value.AllianceCurrencyPerContributionPoint);
            contributions[actorPlayerId.Value] = current with
            {
                TotalPoints = current.TotalPoints + applied,
                DonationCount = current.DonationCount + 1,
                AllianceCurrencyBalance = current.AllianceCurrencyBalance + currencyAwarded
            };

            HashSet<string> processed = new(state.ProcessedDonationIds, StringComparer.Ordinal) { donationKey };
            return state with { Revision = state.Revision + 1, Funding = funding, Contributions = contributions, ProcessedDonationIds = processed };
        }, ct);

        bool nowFullyFunded = FundingComplete(updated, definition);
        if (nowFullyFunded)
            await PublishAsync(actorPlayerId, AllianceActivityType.AllianceTechnologyCompleted, new AllianceActivityPayload { EntityKey = definition.TechnologyId, Result = "funded" },
                "alliance-research-funded:" + definition.TechnologyId, ct);

        logger?.LogInformation("Alliance Research donation: {PlayerId} donated {Amount} {Resource} to {TechnologyId}.", actorPlayerId.Value, clampedAmount, command.ResourceKey, definition.TechnologyId);
        return new AllianceResearchCommandResult(true, "donation_applied", BuildSnapshot(updated, actorPlayerId, membership.Role, now));
    }

    // Real, authoritative gate for whether a donation to this technology is currently legal -
    // reused identically by the pre-check and (implicitly, by construction) the atomic mutation
    // above, which re-derives the same facts fresh from the just-resolved state.
    private static string? ValidateDonatable(AllianceResearchCatalog.TechnologyDefinition definition, AllianceResearchState state, Guid allianceId)
    {
        if (state.Completed.ContainsKey(definition.TechnologyId)) return "technology_completed";
        AllianceResearchSlot? activeSlot = definition.Category == Category.Minor ? state.MinorResearch : state.MajorResearch;
        if (activeSlot?.TechnologyId == definition.TechnologyId) return "technology_researching";
        if (FundingComplete(state, definition)) return "technology_ready";
        string? target = definition.Category == Category.Minor ? state.MinorFundingTargetId : state.MajorFundingTargetId;
        if (!string.Equals(target, definition.TechnologyId, StringComparison.Ordinal)) return "not_the_funding_target";
        return null;
    }

    private static long CurrentlyFunded(AllianceResearchState state, string technologyId, string resourceKey)
        => state.Funding.TryGetValue(technologyId, out AllianceTechnologyFunding? funding) ? funding.Contributed.GetValueOrDefault(resourceKey) : 0;

    private static bool FundingComplete(AllianceResearchState state, AllianceResearchCatalog.TechnologyDefinition definition)
    {
        if (definition.FundingRequirements.Count == 0) return true;
        if (!state.Funding.TryGetValue(definition.TechnologyId, out AllianceTechnologyFunding? funding)) return false;
        foreach ((string resource, long required) in definition.FundingRequirements)
            if (funding.Contributed.GetValueOrDefault(resource) < required) return false;
        return true;
    }

    // ---------------- Chef/Officer: launch ----------------

    public async Task<AllianceResearchCommandResult> LaunchAsync(PlayerId actorPlayerId, LaunchAllianceResearchCommand command, CancellationToken ct = default)
    {
        RequireEnabled();
        if (command is null || string.IsNullOrWhiteSpace(command.TechnologyId) || string.IsNullOrWhiteSpace(command.ClientRequestId))
            return Fail("invalid_request");
        if (!AllianceResearchCatalog.TryGet(command.TechnologyId, out AllianceResearchCatalog.TechnologyDefinition definition))
            return Fail("technology_not_found");

        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return Fail("not_a_member");
        // Bible section 4: Chef always may; Officer may (Alpha keeps this unconditional for
        // Officers - the Bible's own "if their permissions allow" fine-grained Officer permission
        // model does not exist yet in this codebase, so every Officer may launch, matching the
        // mission's own explicit conservative-Alpha instruction).
        if (membership.Role != AllianceRole.Leader && membership.Role != AllianceRole.Officer) return Fail("not_authorized");

        DateTimeOffset now = clock.UtcNow;
        string launchKey = actorPlayerId.Value.ToString("N") + ":" + command.ClientRequestId;
        string? code = null;
        AllianceResearchState updated = await researchRepository.ExecuteAtomicallyAsync(membership.AllianceId.Value, state =>
        {
            (state, _) = ResolveElapsedResearch(state, now);
            if (state.ProcessedLaunchIds.Contains(launchKey)) { code = "launch_applied"; return state; }
            if (state.Completed.ContainsKey(definition.TechnologyId)) { code = "technology_completed"; return state; }
            if (!FundingComplete(state, definition)) { code = "funding_incomplete"; return state; }

            bool isMinor = definition.Category == Category.Minor;
            AllianceResearchSlot? slot = isMinor ? state.MinorResearch : state.MajorResearch;
            if (slot != null) { code = slot.TechnologyId == definition.TechnologyId ? "already_researching" : "slot_occupied"; return state; }

            AllianceResearchSlot newSlot = new(definition.TechnologyId, now, now + definition.ResearchDuration);
            HashSet<string> processed = new(state.ProcessedLaunchIds, StringComparer.Ordinal) { launchKey };
            code = "launch_applied";
            return isMinor
                ? state with { Revision = state.Revision + 1, MinorResearch = newSlot, ProcessedLaunchIds = processed }
                : state with { Revision = state.Revision + 1, MajorResearch = newSlot, ProcessedLaunchIds = processed };
        }, ct);

        bool succeeded = code == "launch_applied";
        if (succeeded)
            await PublishAsync(actorPlayerId, AllianceActivityType.AllianceTechnologyCompleted, new AllianceActivityPayload { EntityKey = definition.TechnologyId, Result = "launched" },
                "alliance-research-launched:" + definition.TechnologyId + ":" + now.ToUnixTimeSeconds(), ct);
        return new AllianceResearchCommandResult(succeeded, code!, BuildSnapshot(updated, actorPlayerId, membership.Role, now));
    }

    // ---------------- Chef/Officer: Alliance Research SpeedUp ----------------

    public async Task<AllianceResearchCommandResult> ApplySpeedUpAsync(PlayerId actorPlayerId, ApplyAllianceResearchSpeedUpCommand command, CancellationToken ct = default)
    {
        RequireEnabled();
        if (command is null || string.IsNullOrWhiteSpace(command.TechnologyId) || string.IsNullOrWhiteSpace(command.ItemId) ||
            string.IsNullOrWhiteSpace(command.ClientRequestId) || command.HiveId == Guid.Empty)
            return Fail("invalid_request");
        if (!AllianceResearchCatalog.TryGet(command.TechnologyId, out AllianceResearchCatalog.TechnologyDefinition definition))
            return Fail("technology_not_found");
        if (!AllianceResearchSpeedUpCatalog.TryGet(command.ItemId, out AllianceResearchSpeedUpCatalog.ItemDefinition item))
            return Fail("item_not_found");

        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return Fail("not_a_member");
        if (membership.Role != AllianceRole.Leader && membership.Role != AllianceRole.Officer) return Fail("not_authorized");

        IReadOnlyList<Guid> ownedHiveIds = await hiveStateRepository.ListHiveIdsAsync(actorPlayerId.Value, ct);
        if (!ownedHiveIds.Contains(command.HiveId)) return Fail("hive_not_owned");

        // Step 1/2: consume exactly one unit of the item from the player's own inventory, atomic,
        // idempotent via Receipts (same mechanism donation/every other paid action already uses).
        string idempotencyKey = "alliance-research-speedup:" + command.ClientRequestId;
        string consumeCode = "not_run";
        await hiveStateRepository.ExecuteAtomicallyAsync(actorPlayerId.Value, command.HiveId, state =>
        {
            if (state.Receipts.TryGetValue(idempotencyKey, out IdempotencyReceipt? stored)) { consumeCode = stored.Code; return state; }
            Dictionary<string, int> inventory = new(state.SpeedUps ?? new Dictionary<string, int>(StringComparer.Ordinal));
            if (inventory.GetValueOrDefault(command.ItemId) <= 0) { consumeCode = "no_speedup_available"; return state; }
            inventory[command.ItemId] -= 1;
            consumeCode = "consumed";
            Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [idempotencyKey] = new IdempotencyReceipt("alliance-research-speedup", true, consumeCode, null, clock.UtcNow) };
            return state with { Revision = state.Revision + 1, SpeedUps = inventory, Receipts = receipts };
        }, ct);

        if (consumeCode == "no_speedup_available") return Fail("no_speedup_available");

        // Step 2/2: reduce the real research timer, atomic, independently idempotent.
        string speedUpKey = actorPlayerId.Value.ToString("N") + ":" + command.ClientRequestId;
        DateTimeOffset now = clock.UtcNow;
        string? code = null;
        AllianceResearchState updated = await researchRepository.ExecuteAtomicallyAsync(membership.AllianceId.Value, state =>
        {
            (state, _) = ResolveElapsedResearch(state, now);
            if (state.ProcessedSpeedUpIds.Contains(speedUpKey)) { code = "speedup_applied"; return state; }

            bool isMinor = definition.Category == Category.Minor;
            AllianceResearchSlot? slot = isMinor ? state.MinorResearch : state.MajorResearch;
            if (slot == null || slot.TechnologyId != definition.TechnologyId) { code = "technology_not_researching"; return state; }

            // Never overshoot below "now" - a completion is resolved by the very next read
            // (ResolveElapsedResearch), not forced here.
            DateTimeOffset newCompletesAt = slot.CompletesAtUtc - item.Reduction;
            if (newCompletesAt < now) newCompletesAt = now;
            AllianceResearchSlot reduced = slot with { CompletesAtUtc = newCompletesAt };
            HashSet<string> processed = new(state.ProcessedSpeedUpIds, StringComparer.Ordinal) { speedUpKey };
            code = "speedup_applied";
            return isMinor
                ? state with { Revision = state.Revision + 1, MinorResearch = reduced, ProcessedSpeedUpIds = processed }
                : state with { Revision = state.Revision + 1, MajorResearch = reduced, ProcessedSpeedUpIds = processed };
        }, ct);

        bool succeeded = code == "speedup_applied";
        return new AllianceResearchCommandResult(succeeded, code ?? "technology_not_researching", BuildSnapshot(updated, actorPlayerId, membership.Role, now));
    }

    // ---------------- Timer resolution (server-authoritative, lazy, idempotent by construction) ----------------

    // No player needs to remain online: whichever slot's CompletesAtUtc has passed is moved into
    // Completed and its slot cleared, deterministically, the next time ANY player's request
    // touches this Alliance's state (GetSnapshotAsync itself calls this via an atomic mutation,
    // exactly like HiveOfflineProductionService.Accrue's own "resolve on every read" convention).
    // Idempotent by construction: once a slot is null, nothing more happens for it - no separate
    // Processed-id set is needed for completion itself.
    private static (AllianceResearchState State, List<(string TechnologyId, Category Category)> JustCompleted) ResolveElapsedResearch(AllianceResearchState state, DateTimeOffset now)
    {
        List<(string, Category)> justCompleted = new();
        AllianceResearchSlot? minor = state.MinorResearch;
        AllianceResearchSlot? major = state.MajorResearch;
        Dictionary<string, AllianceCompletedTechnology> completed = state.Completed;
        bool changed = false;

        if (minor != null && now >= minor.CompletesAtUtc)
        {
            completed = new Dictionary<string, AllianceCompletedTechnology>(completed, StringComparer.Ordinal) { [minor.TechnologyId] = new AllianceCompletedTechnology(minor.TechnologyId, minor.CompletesAtUtc) };
            justCompleted.Add((minor.TechnologyId, Category.Minor));
            minor = null;
            changed = true;
        }
        if (major != null && now >= major.CompletesAtUtc)
        {
            completed = new Dictionary<string, AllianceCompletedTechnology>(completed, StringComparer.Ordinal) { [major.TechnologyId] = new AllianceCompletedTechnology(major.TechnologyId, major.CompletesAtUtc) };
            justCompleted.Add((major.TechnologyId, Category.Major));
            major = null;
            changed = true;
        }

        if (!changed) return (state, justCompleted);
        return (state with { Revision = state.Revision + 1, MinorResearch = minor, MajorResearch = major, Completed = completed }, justCompleted);
    }

    private async Task<(AllianceResearchState State, List<(string TechnologyId, Category Category)> JustCompleted)> ResolveAndPersistElapsedResearchAsync(Guid allianceId, DateTimeOffset now, CancellationToken ct)
    {
        List<(string, Category)> justCompleted = new();
        AllianceResearchState updated = await researchRepository.ExecuteAtomicallyAsync(allianceId, state =>
        {
            (AllianceResearchState next, List<(string, Category)> completedNow) = ResolveElapsedResearch(state, now);
            justCompleted = completedNow;
            return next;
        }, ct);
        return (updated, justCompleted);
    }

    private async Task PublishCompletionsAsync(PlayerId triggeringPlayerId, List<(string TechnologyId, Category Category)> justCompleted, CancellationToken ct)
    {
        foreach ((string technologyId, Category _) in justCompleted)
        {
            await PublishAsync(triggeringPlayerId, AllianceActivityType.AllianceTechnologyCompleted,
                new AllianceActivityPayload { EntityKey = technologyId, Result = "completed" },
                "alliance-research-completed:" + technologyId, ct);
        }
    }

    private async Task PublishAsync(PlayerId actorPlayerId, AllianceActivityType type, AllianceActivityPayload? payload, string dedupeKey, CancellationToken ct)
    {
        if (activityPublisher == null || string.IsNullOrEmpty(dedupeKey)) return;
        try { await activityPublisher.PublishForPlayerAsync(actorPlayerId, type, payload, dedupeKey, ct); }
        catch (Exception exception) { logger?.LogWarning(exception, "Alliance Research activity publish failed for {DedupeKey}.", dedupeKey); }
    }

    private static AllianceResearchCommandResult Fail(string code) => new(false, code, null);

    // ---------------- Snapshot projection ----------------

    private static AllianceResearchReadSnapshot BuildSnapshot(AllianceResearchState state, PlayerId actorPlayerId, AllianceRole role, DateTimeOffset now)
    {
        HashSet<string> completedIds = state.Completed.Keys.ToHashSet(StringComparer.Ordinal);
        List<AllianceTechnologyReadModel> technologies = new(AllianceResearchCatalog.Technologies.Count);
        foreach (AllianceResearchCatalog.TechnologyDefinition definition in AllianceResearchCatalog.Technologies)
        {
            AllianceTechnologyState techState = ResolveTechnologyState(definition, state, completedIds);
            AllianceResearchSlot? slot = definition.Category == Category.Minor ? state.MinorResearch : state.MajorResearch;
            bool isActiveSlot = slot?.TechnologyId == definition.TechnologyId;
            state.Funding.TryGetValue(definition.TechnologyId, out AllianceTechnologyFunding? funding);
            state.Completed.TryGetValue(definition.TechnologyId, out AllianceCompletedTechnology? completedRecord);

            technologies.Add(new AllianceTechnologyReadModel(
                definition.TechnologyId, definition.Branch, definition.Category == Category.Minor ? "minor" : "major", definition.Tier,
                definition.DisplayNameKey, definition.DescriptionKey, definition.BonusSummaryKey, definition.PrerequisiteIds,
                techState, definition.FundingRequirements, funding?.Contributed ?? new Dictionary<string, long>(StringComparer.Ordinal),
                (long)definition.ResearchDuration.TotalSeconds,
                isActiveSlot ? slot!.StartedAtUtc : null, isActiveSlot ? slot!.CompletesAtUtc : null,
                completedRecord?.CompletedAtUtc,
                definition.ProductionBp, definition.CapacityBp, definition.CombatPowerBp));
        }

        state.Contributions.TryGetValue(actorPlayerId.Value, out AllianceResearchContribution? myContribution);
        bool isLeader = role == AllianceRole.Leader;
        bool isOfficerOrAbove = role is AllianceRole.Leader or AllianceRole.Officer;

        return new AllianceResearchReadSnapshot(state.AllianceId, AllianceResearchCatalog.ContractVersion, now, state.Revision,
            technologies, state.MinorFundingTargetId, state.MajorFundingTargetId,
            state.MinorResearch?.TechnologyId, state.MajorResearch?.TechnologyId,
            myContribution?.TotalPoints ?? 0, myContribution?.DonationCount ?? 0, myContribution?.AllianceCurrencyBalance ?? 0,
            CanSelectFundingTarget: isLeader, CanLaunch: isOfficerOrAbove, CanUseSpeedUp: isOfficerOrAbove);
    }

    private static AllianceTechnologyState ResolveTechnologyState(AllianceResearchCatalog.TechnologyDefinition definition, AllianceResearchState state, HashSet<string> completedIds)
    {
        if (state.Completed.ContainsKey(definition.TechnologyId)) return AllianceTechnologyState.Completed;
        AllianceResearchSlot? slot = definition.Category == Category.Minor ? state.MinorResearch : state.MajorResearch;
        if (slot?.TechnologyId == definition.TechnologyId) return AllianceTechnologyState.Researching;
        if (FundingComplete(state, definition)) return AllianceTechnologyState.Ready;
        string? target = definition.Category == Category.Minor ? state.MinorFundingTargetId : state.MajorFundingTargetId;
        if (string.Equals(target, definition.TechnologyId, StringComparison.Ordinal)) return AllianceTechnologyState.Funding;
        return AllianceResearchCatalog.PrerequisitesMet(definition, completedIds) ? AllianceTechnologyState.Eligible : AllianceTechnologyState.Locked;
    }
}
