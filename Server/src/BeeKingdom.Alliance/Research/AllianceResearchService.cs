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

        return await BuildSnapshotAsync(state, actorPlayerId, membership.Role, now, ct);
    }

    // M054-CL: Royal Seals now live on the player's own PlayerHiveState (see RoyalSealsWallet),
    // never in AllianceResearchState.Contributions - every snapshot fetches the real player-owned
    // balance fresh, independently of which Alliance (if any) is currently active, exactly like the
    // Bible's "personal wallet" framing requires. Wraps the pre-existing static BuildSnapshot so the
    // pure state-projection logic itself stays synchronous and unit-testable in isolation.
    private async Task<AllianceResearchReadSnapshot> BuildSnapshotAsync(AllianceResearchState state, PlayerId actorPlayerId, AllianceRole role, DateTimeOffset now, CancellationToken ct)
    {
        long royalSeals = await RoyalSealsWallet.GetBalanceAsync(hiveStateRepository, actorPlayerId.Value, ct);
        return BuildSnapshot(state, actorPlayerId, role, now, royalSeals);
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

        if (code != "funding_target_selected") return new AllianceResearchCommandResult(false, code!, await BuildSnapshotAsync(updated, actorPlayerId, membership.Role, now, ct));

        await PublishAsync(actorPlayerId, AllianceActivityType.AllianceResearchFundingTargetSelected,
            new AllianceActivityPayload { EntityKey = definition.TechnologyId },
            "alliance-research-target:" + definition.Category + ":" + command.ClientRequestId, ct);
        return new AllianceResearchCommandResult(true, code, await BuildSnapshotAsync(updated, actorPlayerId, membership.Role, now, ct));
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

        // Step 1/3: debit the player's REAL resources, atomically, idempotent via the same Receipts
        // mechanism every other paid action in this codebase already uses. `clampedAmount` is still
        // computed from a stale precheck read - see "resource overpayment" below for why this can
        // debit up to `clampedAmount` even though the Alliance side may only end up accepting less.
        //
        // RESOURCE OVERPAYMENT (M054A-CL analysis - deliberately NOT eliminated): making the debit
        // amount exactly equal the eventual `applied` amount would require knowing `applied` BEFORE
        // debiting, which means either (a) computing/reserving it on the Alliance aggregate first,
        // then debiting the player for exactly that reserved amount - but a debit failure
        // (insufficient resources) AFTER that reservation would leave the Alliance holding funding
        // progress no player ever actually paid for, an unrecoverable economy bug strictly worse
        // than the current one, since nothing can ever undo Alliance-side progress once other
        // players see it; or (b) a distributed transaction spanning both aggregates, explicitly
        // forbidden by this mission. Both are worse than the status quo. The status quo's actual
        // exposure is: in the rare case two donations race to fill the LAST bit of room on the exact
        // same resource of the exact same technology, the loser of that race may have MORE real
        // resources debited than the Alliance ultimately credits - the excess is not silently
        // invisible (see the log line at the end of this method, which now reports it explicitly
        // whenever it occurs) but it is not prevented. This is reported as a known, bounded,
        // logged limitation rather than a silent one - see the M054A report "resource overpayment"
        // section for the full analysis and why a STOP was warranted here instead of a workaround.
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

        // Step 2/3: apply the real contribution to the Alliance's shared funding state, atomically,
        // independently idempotent via ProcessedDonationIds. `applied` (never `debitedAmount`) is the
        // ONE authoritative amount that governs Alliance funding, ContributionPoints, AND (step 3
        // below) Royal Seals - the single canonical value the M054A mission requires. It is persisted
        // into DonationAppliedAmounts[donationKey] specifically so a REPLAY of this same donation
        // (idempotency hit on ProcessedDonationIds, which short-circuits before any of the funding/
        // contribution math below runs again) can still recover the exact original applied amount
        // for step 3, rather than needing to unsafely recompute it against already-mutated state.
        string donationKey = actorPlayerId.Value.ToString("N") + ":" + command.ClientRequestId;
        DateTimeOffset now = clock.UtcNow;
        long applied = 0;
        AllianceResearchState updated = await researchRepository.ExecuteAtomicallyAsync(membership.AllianceId.Value, state =>
        {
            (state, _) = ResolveElapsedResearch(state, now);
            if (state.ProcessedDonationIds.Contains(donationKey))
            {
                applied = state.DonationAppliedAmounts.GetValueOrDefault(donationKey);
                return state;
            }

            Dictionary<string, AllianceTechnologyFunding> funding = new(state.Funding, StringComparer.Ordinal);
            AllianceTechnologyFunding techFunding = funding.TryGetValue(definition.TechnologyId, out AllianceTechnologyFunding? existing) ? existing : AllianceTechnologyFunding.Empty();
            Dictionary<string, long> contributed = new(techFunding.Contributed, StringComparer.Ordinal);
            long already = contributed.GetValueOrDefault(command.ResourceKey);
            long room = Math.Max(0, required - already);
            applied = Math.Min(debitedAmount, room);
            contributed[command.ResourceKey] = already + applied;
            funding[definition.TechnologyId] = techFunding with { Contributed = contributed };

            // M054-CL: AllianceCurrencyBalance is no longer written here (or anywhere) - it is a
            // frozen legacy field, read-only compatibility for RoyalSealsMigrationService's one-time
            // backfill into the player's real wallet (PlayerHiveState.RoyalSeals, credited in step 3
            // below, from this SAME `applied` value). ContributionPoints/DonationCount remain exactly
            // as before M054/M054A: Alliance-scoped historical participation.
            Dictionary<Guid, AllianceResearchContribution> contributions = new(state.Contributions);
            AllianceResearchContribution current = contributions.GetValueOrDefault(actorPlayerId.Value, new AllianceResearchContribution(actorPlayerId.Value, 0, 0, 0));
            contributions[actorPlayerId.Value] = current with
            {
                TotalPoints = current.TotalPoints + applied,
                DonationCount = current.DonationCount + 1
            };

            HashSet<string> processed = new(state.ProcessedDonationIds, StringComparer.Ordinal) { donationKey };
            Dictionary<string, long> appliedAmounts = new(state.DonationAppliedAmounts, StringComparer.Ordinal) { [donationKey] = applied };
            return state with { Revision = state.Revision + 1, Funding = funding, Contributions = contributions, ProcessedDonationIds = processed, DonationAppliedAmounts = appliedAmounts };
        }, ct);

        // Step 3/3: credit the player's Royal Seals wallet from `applied` alone (M054A-CL) - a THIRD
        // atomic PlayerHiveState mutation, with its own idempotency key (independent from both the
        // step-1 debit key and RoyalSealsMigrationService's "royal-seals-migration:" prefix), so a
        // retry of the whole DonateAsync call can never double-credit even though this step runs
        // after two other already-idempotent steps. This guarantees the mission's canonical
        // invariant: RoyalSealsAward == floor(applied * ratio), always, regardless of concurrency -
        // no player can ever earn Royal Seals for resources that did not actually land in Alliance
        // funding.
        long currencyAwarded = (long)Math.Floor(applied * options.Value.AllianceCurrencyPerContributionPoint);
        string sealsIdempotencyKey = "alliance-research-seals:" + command.ClientRequestId;
        if (currencyAwarded > 0)
        {
            await hiveStateRepository.ExecuteAtomicallyAsync(actorPlayerId.Value, command.HiveId, state =>
            {
                if (state.Receipts.ContainsKey(sealsIdempotencyKey)) return state;
                Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [sealsIdempotencyKey] = new IdempotencyReceipt("alliance-research-seals", true, "credited", null, clock.UtcNow) };
                PlayerHiveState credited = RoyalSealsWallet.Credit(state, currencyAwarded);
                return credited with { Revision = state.Revision + 1, Receipts = receipts };
            }, ct);
        }

        bool nowFullyFunded = FundingComplete(updated, definition);
        if (nowFullyFunded)
            await PublishAsync(actorPlayerId, AllianceActivityType.AllianceTechnologyCompleted, new AllianceActivityPayload { EntityKey = definition.TechnologyId, Result = "funded" },
                "alliance-research-funded:" + definition.TechnologyId, ct);

        if (applied < debitedAmount)
            logger?.LogWarning("Alliance Research donation overpayment: {PlayerId} was debited {Debited} {Resource} but only {Applied} was applied to {TechnologyId} (concurrent donation clamp).",
                actorPlayerId.Value, debitedAmount, command.ResourceKey, applied, definition.TechnologyId);
        logger?.LogInformation("Alliance Research donation: {PlayerId} donated {Amount} {Resource} to {TechnologyId}.", actorPlayerId.Value, applied, command.ResourceKey, definition.TechnologyId);
        return new AllianceResearchCommandResult(true, "donation_applied", await BuildSnapshotAsync(updated, actorPlayerId, membership.Role, now, ct));
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
        return new AllianceResearchCommandResult(succeeded, code!, await BuildSnapshotAsync(updated, actorPlayerId, membership.Role, now, ct));
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
        return new AllianceResearchCommandResult(succeeded, code ?? "technology_not_researching", await BuildSnapshotAsync(updated, actorPlayerId, membership.Role, now, ct));
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

    private static AllianceResearchReadSnapshot BuildSnapshot(AllianceResearchState state, PlayerId actorPlayerId, AllianceRole role, DateTimeOffset now, long royalSealsBalance)
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
            myContribution?.TotalPoints ?? 0, myContribution?.DonationCount ?? 0, royalSealsBalance,
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
