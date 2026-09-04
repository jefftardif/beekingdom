using BeeKingdom.Accounts;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Alliance.Help;

// M045-CL: Alliance Help never invents its own timer or its own notion of Alliance membership -
// membership truth always comes from IAllianceRepository (the exact same authority AllianceService
// itself uses), and the real remaining duration always comes from OperationTimerReduction reading/
// mutating the SAME PlayerHiveState every other timer system (SpeedUp included) already mutates.
// This service only owns the cooperative-help bookkeeping (requests, contributions, eligibility,
// balance) layered on top of those two existing authorities.
public sealed class AllianceHelpService
{
    private readonly IAllianceHelpRepository helpRepository;
    private readonly IAllianceRepository allianceRepository;
    private readonly IHiveStateRepository hiveStateRepository;
    private readonly IOptions<AllianceHelpOptions> options;
    private readonly IServerClock clock;
    private readonly ILogger<AllianceHelpService>? logger;
    private readonly IPlayerDirectoryService? playerDirectory;

    public AllianceHelpService(
        IAllianceHelpRepository helpRepository,
        IAllianceRepository allianceRepository,
        IHiveStateRepository hiveStateRepository,
        IOptions<AllianceHelpOptions> options,
        IServerClock clock,
        ILogger<AllianceHelpService>? logger = null,
        IPlayerDirectoryService? playerDirectory = null)
    {
        this.helpRepository = helpRepository;
        this.allianceRepository = allianceRepository;
        this.hiveStateRepository = hiveStateRepository;
        this.options = options;
        this.clock = clock;
        this.logger = logger;
        this.playerDirectory = playerDirectory;
    }

    private AllianceHelpOptions O => options.Value;

    private void RequireEnabled()
    {
        if (!O.Enabled) throw new InvalidOperationException("alliance_help_disabled");
    }

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
    {
        SpeedUpCategories.Construction, SpeedUpCategories.Research, SpeedUpCategories.Training, SpeedUpCategories.Healing
    };

    // ---------------- Create request ----------------

    public async Task<AllianceHelpCommandResult> CreateRequestAsync(PlayerId actorPlayerId, CreateAllianceHelpRequestCommand command, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        if (command is null || string.IsNullOrWhiteSpace(command.OperationCategory) || string.IsNullOrWhiteSpace(command.OperationTargetId) || string.IsNullOrWhiteSpace(command.ClientRequestId))
            return new AllianceHelpCommandResult(false, "invalid_request", null);
        if (!AllowedCategories.Contains(command.OperationCategory))
            return new AllianceHelpCommandResult(false, "invalid_category", null);

        // Invariant 2/3: requester must currently belong to an Alliance; the request is bound to it.
        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return new AllianceHelpCommandResult(false, "not_a_member", null);

        // The client only ever names an operation on ITS OWN hive - never trusted blindly: the hive
        // must actually belong to the authenticated actor (invariant 1, enforced here rather than
        // assumed from the route).
        IReadOnlyList<Guid> ownedHiveIds = await hiveStateRepository.ListHiveIdsAsync(actorPlayerId.Value, cancellationToken);
        if (!ownedHiveIds.Contains(command.HiveId)) return new AllianceHelpCommandResult(false, "hive_not_owned", null);

        // No repeated request for the same still-active operation - server-side, not just a
        // disabled button client-side. Idempotent-friendly: if one is already open, hand it back
        // as success instead of erroring.
        AllianceHelpRequest? existingOpen = await helpRepository.GetOpenForPlayerOperationAsync(actorPlayerId.Value, command.OperationCategory, command.OperationTargetId, cancellationToken);
        if (existingOpen != null) return new AllianceHelpCommandResult(true, "request_already_open", existingOpen);

        PlayerHiveState? state = await hiveStateRepository.ReadAsync(actorPlayerId.Value, command.HiveId, cancellationToken);
        if (state == null) return new AllianceHelpCommandResult(false, "hive_not_found", null);

        DateTimeOffset now = clock.UtcNow;
        if (!OperationTimerReduction.TryPeek(state, command.OperationCategory, command.OperationTargetId, now, out OperationTimerInfo timer))
            return new AllianceHelpCommandResult(false, "operation_not_found", null);
        if (timer.Completed) return new AllianceHelpCommandResult(false, "operation_completed", null);

        long originalDurationSeconds = (long)Math.Max(0, (timer.EndsAtUtc - timer.StartedAtUtc).TotalSeconds);
        if (originalDurationSeconds < O.MinEligibleOriginalDurationSeconds)
            return new AllianceHelpCommandResult(false, "operation_too_short", null);

        AllianceHelpRequest request = new(
            HelpRequestId: Guid.NewGuid(),
            AllianceId: membership.AllianceId,
            RequestingPlayerId: actorPlayerId,
            RequestingHiveId: command.HiveId,
            OperationCategory: command.OperationCategory,
            OperationTargetId: command.OperationTargetId,
            OperationId: timer.OperationId,
            CreatedAtUtc: now,
            Status: AllianceHelpRequestStatus.Open,
            OriginalDurationSeconds: originalDurationSeconds,
            HelpCount: 0,
            MaxHelpCount: O.MaxHelpCount,
            Revision: 0,
            ClientRequestId: command.ClientRequestId);

        AllianceHelpRequest? created = await helpRepository.TryCreateAsync(request, cancellationToken);
        if (created == null)
        {
            // Lost a create race against a concurrent identical request (DB unique index) -
            // idempotent-friendly: hand back whichever one actually won.
            AllianceHelpRequest? winner = await helpRepository.GetOpenForPlayerOperationAsync(actorPlayerId.Value, command.OperationCategory, command.OperationTargetId, cancellationToken);
            return winner != null ? new AllianceHelpCommandResult(true, "request_already_open", winner) : new AllianceHelpCommandResult(false, "create_failed", null);
        }

        logger?.LogInformation("Alliance Help request {HelpRequestId} created by {PlayerId} for {Category}/{TargetId} in alliance {AllianceId}.",
            created.HelpRequestId, actorPlayerId.Value, command.OperationCategory, command.OperationTargetId, membership.AllianceId.Value);
        return new AllianceHelpCommandResult(true, "request_created", created);
    }

    // ---------------- List ----------------

    // Requests from OTHER members of the caller's alliance that are still open - the mission's own
    // "list active requests from other members" wording. The caller's own open request (if any) is
    // exposed separately via GetMyOpenRequestAsync, matching the distinct "Vous avez demandé..." UI
    // state instead of a self-help row that would never be actionable.
    public async Task<IReadOnlyList<AllianceHelpRequest>> ListHelpableForCurrentAllianceAsync(PlayerId actorPlayerId, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        AllianceMembership? membership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return Array.Empty<AllianceHelpRequest>();

        IReadOnlyList<AllianceHelpRequest> open = await helpRepository.ListOpenForAllianceAsync(membership.AllianceId.Value, cancellationToken);
        return open.Where(request => request.RequestingPlayerId.Value != actorPlayerId.Value).ToList();
    }

    public async Task<bool> HasHelpedAsync(Guid helpRequestId, PlayerId actorPlayerId, CancellationToken cancellationToken = default)
        => await helpRepository.GetContributionAsync(helpRequestId, actorPlayerId.Value, cancellationToken) != null;

    // Read-model used by the Unity "Aides" list: resolves DisplayName and live RemainingSeconds per
    // row. A request whose underlying operation already completed/vanished (race with the normal
    // collect flow) is simply skipped here rather than shown with a stale/zero timer.
    public async Task<IReadOnlyList<AllianceHelpRequestView>> ListHelpableViewsForCurrentAllianceAsync(PlayerId actorPlayerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AllianceHelpRequest> requests = await ListHelpableForCurrentAllianceAsync(actorPlayerId, cancellationToken);
        List<AllianceHelpRequestView> views = [];
        DateTimeOffset now = clock.UtcNow;
        foreach (AllianceHelpRequest request in requests)
        {
            PlayerHiveState? requesterState = await hiveStateRepository.ReadAsync(request.RequestingPlayerId.Value, request.RequestingHiveId, cancellationToken);
            if (requesterState == null || !OperationTimerReduction.TryPeek(requesterState, request.OperationCategory, request.OperationTargetId, now, out OperationTimerInfo timer) || timer.Completed)
                continue;

            bool alreadyHelped = await HasHelpedAsync(request.HelpRequestId, actorPlayerId, cancellationToken);
            string displayName = playerDirectory?.GetByPlayerId(request.RequestingPlayerId)?.DisplayName ?? string.Empty;
            long remainingSeconds = (long)Math.Max(0, (timer.EndsAtUtc - now).TotalSeconds);
            views.Add(new AllianceHelpRequestView(request.HelpRequestId, request.RequestingPlayerId.Value, displayName, request.OperationCategory,
                request.OperationTargetId, remainingSeconds, request.HelpCount, request.MaxHelpCount, alreadyHelped, request.CreatedAtUtc));
        }
        return views;
    }

    public async Task<AllianceHelpRequest?> GetMyOpenRequestAsync(PlayerId actorPlayerId, string operationCategory, string operationTargetId, CancellationToken cancellationToken = default)
        => await helpRepository.GetOpenForPlayerOperationAsync(actorPlayerId.Value, operationCategory, operationTargetId, cancellationToken);

    // ---------------- Contribute ----------------

    public async Task<ContributeAllianceHelpResult> ContributeAsync(PlayerId actorPlayerId, Guid helpRequestId, string clientRequestId, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        if (string.IsNullOrWhiteSpace(clientRequestId)) return new ContributeAllianceHelpResult(false, "invalid_request", null, null);

        AllianceMembership? actorMembership = allianceRepository.GetActiveMembershipForPlayer(actorPlayerId);
        if (actorMembership == null) return new ContributeAllianceHelpResult(false, "not_a_member", null, null);

        AllianceHelpRequest? request = await helpRepository.GetAsync(helpRequestId, cancellationToken);
        if (request == null) return new ContributeAllianceHelpResult(false, "not_found", null, null);

        // Invariant 4: helper must be in the SAME alliance the request is bound to right now (not
        // whatever alliance existed when the request was created).
        if (request.AllianceId.Value != actorMembership.AllianceId.Value)
            return new ContributeAllianceHelpResult(false, "different_alliance", request, null);

        // Invariant 5: cannot help your own request.
        if (request.RequestingPlayerId.Value == actorPlayerId.Value)
            return new ContributeAllianceHelpResult(false, "cannot_help_own_request", request, null);

        // Invariant 6/7: idempotent-friendly - a helper can only ever contribute once to a given
        // request, so "already have a contribution row" IS the idempotency key here (independent of
        // whether the retried call's ClientRequestId matches the first attempt's).
        AllianceHelpContribution? existingContribution = await helpRepository.GetContributionAsync(helpRequestId, actorPlayerId.Value, cancellationToken);
        if (existingContribution != null)
            return new ContributeAllianceHelpResult(true, "already_helped", request, existingContribution.DurationReductionSeconds);

        if (request.Status != AllianceHelpRequestStatus.Open)
            return new ContributeAllianceHelpResult(false, "request_not_open", request, null);
        if (request.HelpCount >= request.MaxHelpCount)
            return new ContributeAllianceHelpResult(false, "help_full", request, null);

        // Invariant 9: the underlying real operation must still exist and not already be complete.
        PlayerHiveState? requesterState = await hiveStateRepository.ReadAsync(request.RequestingPlayerId.Value, request.RequestingHiveId, cancellationToken);
        if (requesterState == null) return new ContributeAllianceHelpResult(false, "operation_not_found", request, null);
        DateTimeOffset now = clock.UtcNow;
        if (!OperationTimerReduction.TryPeek(requesterState, request.OperationCategory, request.OperationTargetId, now, out OperationTimerInfo timer) || timer.Completed)
        {
            await helpRepository.TryUpdateStatusAsync(helpRequestId, request.Revision, AllianceHelpRequestStatus.Expired, cancellationToken);
            return new ContributeAllianceHelpResult(false, "operation_completed", request, null);
        }

        long remainingSeconds = (long)Math.Max(0, (timer.EndsAtUtc - now).TotalSeconds);
        long reductionSeconds = Math.Min(O.ComputeReductionSeconds(request.OriginalDurationSeconds), remainingSeconds);
        if (reductionSeconds <= 0) return new ContributeAllianceHelpResult(false, "operation_completed", request, null);

        // M045-CL concurrency note (documented in the mission report): the AllianceHelp "slot" is
        // reserved FIRST, atomically (unique contribution row + HelpCount/MaxHelpCount/Revision all
        // checked in one DB transaction) - this is the layer with the real risk of a double-apply
        // under concurrent "Aider" clicks, so it goes first and is the one guaranteed exactly-once.
        // The real operation's timer is only reduced AFTER that reservation succeeds. If the second
        // step somehow failed, the failure mode is "a help slot was spent but the timer didn't move"
        // (recoverable, and reduction is generous - never worse) rather than the reverse.
        AllianceHelpContribution contribution = new(helpRequestId, actorPlayerId, now, reductionSeconds, clientRequestId);
        (bool applied, string code, AllianceHelpRequest? updatedRequest) = await helpRepository.TryContributeAsync(helpRequestId, request.Revision, contribution, cancellationToken);
        if (!applied) return new ContributeAllianceHelpResult(false, code, updatedRequest ?? request, null);

        PlayerHiveState updatedHiveState = await hiveStateRepository.ExecuteAtomicallyAsync(request.RequestingPlayerId.Value, request.RequestingHiveId, currentState =>
        {
            DateTimeOffset applyNow = clock.UtcNow;
            if (!OperationTimerReduction.TryReduce(currentState, request.OperationCategory, request.OperationTargetId, applyNow, TimeSpan.FromSeconds(reductionSeconds), out PlayerHiveState reducedState, out OperationTimerInfo _))
                return currentState;
            return reducedState with { Revision = currentState.Revision + 1 };
        }, cancellationToken);

        logger?.LogInformation("Alliance Help contribution: {HelperPlayerId} helped request {HelpRequestId} ({Category}/{TargetId}) for -{ReductionSeconds}s.",
            actorPlayerId.Value, helpRequestId, request.OperationCategory, request.OperationTargetId, reductionSeconds);

        return new ContributeAllianceHelpResult(true, "help_applied", updatedRequest, reductionSeconds);
    }

    public async Task<ContributeAllianceHelpAllResult> ContributeAllAsync(PlayerId actorPlayerId, string clientRequestId, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        IReadOnlyList<AllianceHelpRequest> helpable = await ListHelpableForCurrentAllianceAsync(actorPlayerId, cancellationToken);
        List<ContributeAllianceHelpResult> results = [];
        foreach (AllianceHelpRequest request in helpable)
        {
            bool alreadyHelped = await HasHelpedAsync(request.HelpRequestId, actorPlayerId, cancellationToken);
            if (alreadyHelped || request.Status != AllianceHelpRequestStatus.Open || request.HelpCount >= request.MaxHelpCount) continue;
            // Each request gets its own derived ClientRequestId so a retried "Help All" call is
            // idempotent per-request too (same underlying per-contribution idempotency as a single
            // Contribute call), without needing the client to enumerate ids itself.
            string perRequestClientId = clientRequestId + ":" + request.HelpRequestId.ToString("N");
            ContributeAllianceHelpResult result = await ContributeAsync(actorPlayerId, request.HelpRequestId, perRequestClientId, cancellationToken);
            results.Add(result);
        }
        return new ContributeAllianceHelpAllResult(results);
    }
}
