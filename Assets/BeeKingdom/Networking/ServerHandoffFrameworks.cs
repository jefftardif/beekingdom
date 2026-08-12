using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Networking
{
    public enum ColonyCommandDestination
    {
        Unknown,
        Construction,
        Population,
        Resources,
        Ai,
        World,
        Administration
    }

    public sealed class ColonyCommandRoute
    {
        public ColonyCommandRoute(string commandKind, ColonyCommandDestination destination)
        {
            CommandKind = commandKind ?? string.Empty;
            Destination = destination;
        }

        public string CommandKind { get; }
        public ColonyCommandDestination Destination { get; }
    }

    public sealed class ColonyCommandRouteDiagnostics
    {
        public ColonyCommandRouteDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class ColonyCommandRouteResult
    {
        public ColonyCommandRouteResult(bool routed, ColonyCommandDestination destination, ColonyCommandRouteDiagnostics diagnostics)
        {
            Routed = routed;
            Destination = destination;
            Diagnostics = diagnostics ?? new ColonyCommandRouteDiagnostics(Array.Empty<string>());
        }

        public bool Routed { get; }
        public ColonyCommandDestination Destination { get; }
        public ColonyCommandRouteDiagnostics Diagnostics { get; }
    }

    public sealed class ColonyCommandRouter
    {
        private readonly List<ColonyCommandRoute> routes;

        public ColonyCommandRouter(IEnumerable<ColonyCommandRoute> routes)
        {
            this.routes = (routes ?? Array.Empty<ColonyCommandRoute>())
                .OrderBy(route => route.CommandKind, StringComparer.Ordinal)
                .ToList();
        }

        public IReadOnlyList<ColonyCommandRoute> Routes => routes;

        public static ColonyCommandRouter CreateDefault()
        {
            return new ColonyCommandRouter(new[]
            {
                new ColonyCommandRoute("admin", ColonyCommandDestination.Administration),
                new ColonyCommandRoute("ai", ColonyCommandDestination.Ai),
                new ColonyCommandRoute("build", ColonyCommandDestination.Construction),
                new ColonyCommandRoute("population", ColonyCommandDestination.Population),
                new ColonyCommandRoute("resource", ColonyCommandDestination.Resources),
                new ColonyCommandRoute("world", ColonyCommandDestination.World)
            });
        }

        public ColonyCommandRouteResult Route(string commandKind, RegionalCommandScopeKind scopeKind)
        {
            if (scopeKind != RegionalCommandScopeKind.Colony)
            {
                return new ColonyCommandRouteResult(false, ColonyCommandDestination.Unknown, new ColonyCommandRouteDiagnostics(new[] { "Command is outside colony scope" }));
            }

            ColonyCommandRoute route = routes.FirstOrDefault(candidate => string.Equals(candidate.CommandKind, commandKind, StringComparison.Ordinal));
            if (route == null || route.Destination == ColonyCommandDestination.Unknown)
            {
                return new ColonyCommandRouteResult(false, ColonyCommandDestination.Unknown, new ColonyCommandRouteDiagnostics(new[] { "Unknown colony command destination" }));
            }

            return new ColonyCommandRouteResult(true, route.Destination, new ColonyCommandRouteDiagnostics(Array.Empty<string>()));
        }
    }

    public enum RegionalCommandScopeKind
    {
        Unknown,
        Colony,
        Region,
        World,
        Session,
        ReadOnlyObservation
    }

    public sealed class RegionalCommandScope
    {
        public RegionalCommandScope(RegionalCommandScopeKind kind, string regionId = "", string colonyId = "", bool readOnly = false)
        {
            Kind = kind;
            RegionId = regionId ?? string.Empty;
            ColonyId = colonyId ?? string.Empty;
            ReadOnly = readOnly;
        }

        public RegionalCommandScopeKind Kind { get; }
        public string RegionId { get; }
        public string ColonyId { get; }
        public bool ReadOnly { get; }
    }

    public sealed class RegionalCommandScopeRule
    {
        public RegionalCommandScopeRule(RegionalCommandScopeKind kind, bool mutationAllowed)
        {
            Kind = kind;
            MutationAllowed = mutationAllowed;
        }

        public RegionalCommandScopeKind Kind { get; }
        public bool MutationAllowed { get; }
    }

    public sealed class RegionalCommandScopeDiagnostics
    {
        public RegionalCommandScopeDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class RegionalCommandScopeResult
    {
        public RegionalCommandScopeResult(bool accepted, RegionalCommandScopeDiagnostics diagnostics)
        {
            Accepted = accepted;
            Diagnostics = diagnostics ?? new RegionalCommandScopeDiagnostics(Array.Empty<string>());
        }

        public bool Accepted { get; }
        public RegionalCommandScopeDiagnostics Diagnostics { get; }
    }

    public sealed class RegionalCommandScopeValidator
    {
        private readonly HashSet<string> knownRegions;
        private readonly Dictionary<RegionalCommandScopeKind, RegionalCommandScopeRule> rules;

        public RegionalCommandScopeValidator(IEnumerable<string> knownRegions, IEnumerable<RegionalCommandScopeRule> rules = null)
        {
            this.knownRegions = new HashSet<string>(knownRegions ?? Array.Empty<string>(), StringComparer.Ordinal);
            this.rules = (rules ?? DefaultRules()).ToDictionary(rule => rule.Kind, rule => rule);
        }

        public IReadOnlyList<RegionalCommandScopeRule> Rules => rules.Values.OrderBy(rule => rule.Kind).ToList();

        public RegionalCommandScopeResult Validate(RegionalCommandScope scope, bool mutationRequested)
        {
            var issues = new List<string>();
            if (scope == null || scope.Kind == RegionalCommandScopeKind.Unknown)
            {
                issues.Add("Scope is unknown");
                return new RegionalCommandScopeResult(false, new RegionalCommandScopeDiagnostics(issues));
            }

            if ((scope.Kind == RegionalCommandScopeKind.Region || scope.Kind == RegionalCommandScopeKind.Colony || scope.Kind == RegionalCommandScopeKind.ReadOnlyObservation)
                && !knownRegions.Contains(scope.RegionId))
            {
                issues.Add("Region is unknown");
            }

            if (scope.Kind == RegionalCommandScopeKind.Colony && string.IsNullOrWhiteSpace(scope.ColonyId))
            {
                issues.Add("Colony scope requires a colony id");
            }

            if (scope.Kind == RegionalCommandScopeKind.ReadOnlyObservation && mutationRequested)
            {
                issues.Add("Read-only observation cannot request mutation");
            }

            if (rules.TryGetValue(scope.Kind, out RegionalCommandScopeRule rule) && mutationRequested && !rule.MutationAllowed)
            {
                issues.Add("Mutation is not allowed for this scope");
            }

            return new RegionalCommandScopeResult(issues.Count == 0, new RegionalCommandScopeDiagnostics(issues));
        }

        private static IEnumerable<RegionalCommandScopeRule> DefaultRules()
        {
            yield return new RegionalCommandScopeRule(RegionalCommandScopeKind.Colony, true);
            yield return new RegionalCommandScopeRule(RegionalCommandScopeKind.Region, true);
            yield return new RegionalCommandScopeRule(RegionalCommandScopeKind.World, true);
            yield return new RegionalCommandScopeRule(RegionalCommandScopeKind.Session, true);
            yield return new RegionalCommandScopeRule(RegionalCommandScopeKind.ReadOnlyObservation, false);
        }
    }

    public enum ServerTickPhase
    {
        InputValidation,
        CommandRouting,
        Simulation,
        EventEmission,
        Snapshot,
        Digest,
        Diagnostics
    }

    public sealed class ServerTickInput
    {
        public ServerTickInput(long tick, IEnumerable<ServerTickPhase> phases)
        {
            Tick = tick;
            Phases = (phases ?? Array.Empty<ServerTickPhase>()).ToList();
        }

        public long Tick { get; }
        public IReadOnlyList<ServerTickPhase> Phases { get; }
    }

    public sealed class ServerTickOutput
    {
        public ServerTickOutput(bool snapshotProduced, bool digestProduced)
        {
            SnapshotProduced = snapshotProduced;
            DigestProduced = digestProduced;
        }

        public bool SnapshotProduced { get; }
        public bool DigestProduced { get; }
    }

    public sealed class ServerTickDiagnostics
    {
        public ServerTickDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class ServerSimulationTickContract
    {
        private static readonly ServerTickPhase[] Expected =
        {
            ServerTickPhase.InputValidation,
            ServerTickPhase.CommandRouting,
            ServerTickPhase.Simulation,
            ServerTickPhase.EventEmission,
            ServerTickPhase.Snapshot,
            ServerTickPhase.Digest,
            ServerTickPhase.Diagnostics
        };

        public IReadOnlyList<ServerTickPhase> ExpectedPhases => Expected;

        public ServerTickDiagnostics Validate(ServerTickInput input, ServerTickOutput output = null)
        {
            var issues = new List<string>();
            List<ServerTickPhase> phases = (input?.Phases ?? Expected).ToList();
            int lastIndex = -1;
            foreach (ServerTickPhase phase in phases)
            {
                int index = Array.IndexOf(Expected, phase);
                if (index < lastIndex)
                {
                    issues.Add("Tick phases are out of deterministic order");
                }

                lastIndex = index;
            }

            if (phases.Contains(ServerTickPhase.Snapshot) && phases.Contains(ServerTickPhase.CommandRouting)
                && phases.IndexOf(ServerTickPhase.CommandRouting) > phases.IndexOf(ServerTickPhase.Snapshot))
            {
                issues.Add("Command routing cannot occur after snapshot");
            }

            if (output != null && output.DigestProduced && !output.SnapshotProduced)
            {
                issues.Add("Digest output requires a snapshot output");
            }

            return new ServerTickDiagnostics(issues);
        }
    }

    public enum ObservationScope
    {
        Colony,
        Region,
        Session,
        EventStream,
        ReadModel
    }

    public sealed class ObservationFilter
    {
        public ObservationFilter(string name, bool exposesSecret = false)
        {
            Name = name ?? string.Empty;
            ExposesSecret = exposesSecret;
        }

        public string Name { get; }
        public bool ExposesSecret { get; }
    }

    public sealed class ObservationDeliveryPolicy
    {
        public ObservationDeliveryPolicy(bool readOnly = true, bool mutationRequested = false)
        {
            ReadOnly = readOnly;
            MutationRequested = mutationRequested;
        }

        public bool ReadOnly { get; }
        public bool MutationRequested { get; }
    }

    public sealed class ClientObservationSubscription
    {
        public ClientObservationSubscription(string clientId, ObservationScope scope, ObservationFilter filter, ObservationDeliveryPolicy policy, bool authorized)
        {
            ClientId = clientId ?? string.Empty;
            Scope = scope;
            Filter = filter;
            Policy = policy;
            Authorized = authorized;
        }

        public string ClientId { get; }
        public ObservationScope Scope { get; }
        public ObservationFilter Filter { get; }
        public ObservationDeliveryPolicy Policy { get; }
        public bool Authorized { get; }
    }

    public sealed class ObservationSubscriptionDiagnostics
    {
        public ObservationSubscriptionDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class ClientObservationSubscriptionRegistry
    {
        private readonly List<ClientObservationSubscription> subscriptions = new List<ClientObservationSubscription>();

        public IReadOnlyList<ClientObservationSubscription> Subscriptions => subscriptions
            .OrderBy(subscription => subscription.ClientId, StringComparer.Ordinal)
            .ThenBy(subscription => subscription.Scope)
            .ToList();

        public ObservationSubscriptionDiagnostics Add(ClientObservationSubscription subscription)
        {
            var issues = new List<string>();
            if (subscription == null)
            {
                return new ObservationSubscriptionDiagnostics(new[] { "Subscription is missing" });
            }

            if (!subscription.Authorized)
            {
                issues.Add("Client is not authorized for observation");
            }

            if (subscription.Policy == null || !subscription.Policy.ReadOnly || subscription.Policy.MutationRequested)
            {
                issues.Add("Observation subscription must be read-only");
            }

            if (subscription.Filter == null || string.IsNullOrWhiteSpace(subscription.Filter.Name))
            {
                issues.Add("Observation filter is unknown");
            }
            else if (subscription.Filter.ExposesSecret)
            {
                issues.Add("Observation filter exposes a server secret");
            }

            if (issues.Count == 0)
            {
                subscriptions.Add(subscription);
            }

            return new ObservationSubscriptionDiagnostics(issues);
        }
    }

    public enum AuthoritativeEventKind
    {
        Unknown,
        CommandAccepted,
        CommandRejected,
        SimulationApplied,
        SnapshotProduced,
        ObservationNotified
    }

    public sealed class AuthoritativeEventEntry
    {
        public AuthoritativeEventEntry(long sequence, long tick, ServerTickPhase phase, string commandId, AuthoritativeEventKind kind, string message = "")
        {
            Sequence = sequence;
            Tick = tick;
            Phase = phase;
            CommandId = commandId ?? string.Empty;
            Kind = kind;
            Message = message ?? string.Empty;
        }

        public long Sequence { get; }
        public long Tick { get; }
        public ServerTickPhase Phase { get; }
        public string CommandId { get; }
        public AuthoritativeEventKind Kind { get; }
        public string Message { get; }
    }

    public sealed class AuthoritativeEventCursor
    {
        public AuthoritativeEventCursor(long afterSequence)
        {
            AfterSequence = afterSequence;
        }

        public long AfterSequence { get; }
    }

    public sealed class AuthoritativeEventJournalDiagnostics
    {
        public AuthoritativeEventJournalDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class AuthoritativeEventJournal
    {
        private readonly List<AuthoritativeEventEntry> entries = new List<AuthoritativeEventEntry>();

        public IReadOnlyList<AuthoritativeEventEntry> Entries => Sorted(entries);

        public AuthoritativeEventJournalDiagnostics Append(AuthoritativeEventEntry entry)
        {
            if (entry == null)
            {
                return new AuthoritativeEventJournalDiagnostics(new[] { "Event is missing" });
            }

            if (entry.Kind == AuthoritativeEventKind.Unknown)
            {
                return new AuthoritativeEventJournalDiagnostics(new[] { "Event kind is unknown" });
            }

            entries.Add(entry);
            return ValidateSequence();
        }

        public IReadOnlyList<AuthoritativeEventEntry> Replay(AuthoritativeEventCursor cursor)
        {
            long after = cursor?.AfterSequence ?? -1;
            return Sorted(entries.Where(entry => entry.Sequence > after));
        }

        public AuthoritativeEventJournalDiagnostics ValidateSequence()
        {
            var issues = new List<string>();
            long expected = 1;
            foreach (AuthoritativeEventEntry entry in entries.OrderBy(entry => entry.Sequence))
            {
                if (entry.Sequence != expected)
                {
                    issues.Add("Authoritative event sequence has a gap");
                    break;
                }

                expected++;
            }

            return new AuthoritativeEventJournalDiagnostics(issues);
        }

        private static IReadOnlyList<AuthoritativeEventEntry> Sorted(IEnumerable<AuthoritativeEventEntry> source)
        {
            return source.OrderBy(entry => entry.Tick)
                .ThenBy(entry => entry.Phase)
                .ThenBy(entry => entry.CommandId, StringComparer.Ordinal)
                .ThenBy(entry => entry.Sequence)
                .ToList();
        }
    }

    public enum RetryDecision
    {
        Replayable,
        DuplicateIgnored,
        ReturnExistingResult,
        RejectedConflict,
        Expired
    }

    public sealed class RetryCommandFingerprint : IEquatable<RetryCommandFingerprint>
    {
        public RetryCommandFingerprint(string idempotencyKey, string commandKind, string payloadHash)
        {
            IdempotencyKey = idempotencyKey ?? string.Empty;
            CommandKind = commandKind ?? string.Empty;
            PayloadHash = payloadHash ?? string.Empty;
        }

        public string IdempotencyKey { get; }
        public string CommandKind { get; }
        public string PayloadHash { get; }
        public bool HasKey => !string.IsNullOrWhiteSpace(IdempotencyKey);
        public bool Equals(RetryCommandFingerprint other) => other != null && IdempotencyKey == other.IdempotencyKey && CommandKind == other.CommandKind && PayloadHash == other.PayloadHash;
        public override bool Equals(object obj) => Equals(obj as RetryCommandFingerprint);
        public override int GetHashCode() => (IdempotencyKey + "|" + CommandKind + "|" + PayloadHash).GetHashCode();
    }

    public sealed class RetryPolicyRule
    {
        public RetryPolicyRule(string commandKind, bool replayable)
        {
            CommandKind = commandKind ?? string.Empty;
            Replayable = replayable;
        }

        public string CommandKind { get; }
        public bool Replayable { get; }
    }

    public sealed class RetryPolicyDiagnostics
    {
        public RetryPolicyDiagnostics(RetryDecision decision, string reason)
        {
            Decision = decision;
            Reason = reason ?? string.Empty;
        }

        public RetryDecision Decision { get; }
        public string Reason { get; }
    }

    public sealed class RetryIdempotencyPolicy
    {
        public RetryPolicyDiagnostics Evaluate(RetryCommandFingerprint incoming, RetryCommandFingerprint existing, bool existingApplied, bool expired)
        {
            if (incoming == null || !incoming.HasKey)
            {
                return new RetryPolicyDiagnostics(RetryDecision.RejectedConflict, "Retry is missing idempotency key");
            }

            if (expired)
            {
                return new RetryPolicyDiagnostics(RetryDecision.Expired, "Retry window expired");
            }

            if (existing == null)
            {
                return new RetryPolicyDiagnostics(RetryDecision.Replayable, "First command occurrence");
            }

            if (incoming.IdempotencyKey == existing.IdempotencyKey && !incoming.Equals(existing))
            {
                return new RetryPolicyDiagnostics(RetryDecision.RejectedConflict, "Same idempotency key has different payload");
            }

            return existingApplied
                ? new RetryPolicyDiagnostics(RetryDecision.ReturnExistingResult, "Command was already applied")
                : new RetryPolicyDiagnostics(RetryDecision.DuplicateIgnored, "Duplicate command is already queued");
        }
    }

    public enum ConflictKind
    {
        Unknown,
        Scope,
        Sequence,
        Retry,
        SessionState,
        ResourceLock,
        EventOrder
    }

    public enum ConflictSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public sealed class ConflictContext
    {
        public ConflictContext(string commandId, string sessionId, RegionalCommandScope scope, AuthoritativeEventEntry eventEntry, string queueState)
        {
            CommandId = commandId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            Scope = scope;
            EventEntry = eventEntry;
            QueueState = queueState ?? string.Empty;
        }

        public string CommandId { get; }
        public string SessionId { get; }
        public RegionalCommandScope Scope { get; }
        public AuthoritativeEventEntry EventEntry { get; }
        public string QueueState { get; }
    }

    public sealed class ConflictFinding
    {
        public ConflictFinding(ConflictKind kind, ConflictSeverity severity, string evidence)
        {
            Kind = kind;
            Severity = severity;
            Evidence = evidence ?? string.Empty;
        }

        public ConflictKind Kind { get; }
        public ConflictSeverity Severity { get; }
        public string Evidence { get; }
    }

    public sealed class ConflictResolutionDiagnostic
    {
        public IReadOnlyList<ConflictFinding> Analyze(ConflictContext context, RegionalCommandScopeResult scopeResult, RetryPolicyDiagnostics retryDiagnostics, AuthoritySessionState sessionState, AuthoritativeEventJournalDiagnostics journalDiagnostics)
        {
            var findings = new List<ConflictFinding>();
            if (context == null)
            {
                return new[] { new ConflictFinding(ConflictKind.Unknown, ConflictSeverity.Error, "Conflict context is missing") };
            }

            if (scopeResult != null && !scopeResult.Accepted)
            {
                findings.Add(new ConflictFinding(ConflictKind.Scope, ConflictSeverity.Error, context.CommandId));
            }

            if (retryDiagnostics != null && retryDiagnostics.Decision == RetryDecision.RejectedConflict)
            {
                findings.Add(new ConflictFinding(ConflictKind.Retry, ConflictSeverity.Error, retryDiagnostics.Reason));
            }

            if (sessionState == AuthoritySessionState.Closed)
            {
                findings.Add(new ConflictFinding(ConflictKind.SessionState, ConflictSeverity.Critical, context.SessionId));
            }

            if (journalDiagnostics != null && !journalDiagnostics.Valid)
            {
                findings.Add(new ConflictFinding(ConflictKind.EventOrder, ConflictSeverity.Error, context.EventEntry?.Sequence.ToString() ?? string.Empty));
            }

            if (findings.Count == 0)
            {
                findings.Add(new ConflictFinding(ConflictKind.Unknown, ConflictSeverity.Info, "No known conflict detected"));
            }

            return findings.OrderByDescending(finding => finding.Severity).ThenBy(finding => finding.Kind).ToList();
        }
    }

    public enum RecoveryRequirement
    {
        ValidSession,
        CompatibleVersion,
        SnapshotOrDelta,
        QueueReconciliation,
        ObservationResubscribe,
        Authorization
    }

    public enum RecoveryStep
    {
        ValidateAuthorization,
        ValidateSession,
        ValidateVersion,
        SelectSnapshotOrDelta,
        ReconcileQueue,
        ResubscribeObservations
    }

    public enum RecoveryVerdict
    {
        Recoverable,
        NeedsSnapshot,
        Rejected,
        Blocked
    }

    public sealed class RecoveryDiagnostics
    {
        public RecoveryDiagnostics(RecoveryVerdict verdict, IReadOnlyList<string> issues, IReadOnlyList<RecoveryStep> steps)
        {
            Verdict = verdict;
            Issues = issues ?? Array.Empty<string>();
            Steps = steps ?? Array.Empty<RecoveryStep>();
        }

        public RecoveryVerdict Verdict { get; }
        public IReadOnlyList<string> Issues { get; }
        public IReadOnlyList<RecoveryStep> Steps { get; }
    }

    public sealed class DisconnectRecoveryContract
    {
        public IReadOnlyList<RecoveryStep> Steps { get; } = new[]
        {
            RecoveryStep.ValidateAuthorization,
            RecoveryStep.ValidateSession,
            RecoveryStep.ValidateVersion,
            RecoveryStep.SelectSnapshotOrDelta,
            RecoveryStep.ReconcileQueue,
            RecoveryStep.ResubscribeObservations
        };

        public RecoveryDiagnostics Evaluate(AuthoritySessionContext session, ProtocolVersionDiagnostics version, bool authorized, bool hasSnapshot, bool hasDelta, bool snapshotStale)
        {
            var issues = new List<string>();
            if (!authorized)
            {
                return new RecoveryDiagnostics(RecoveryVerdict.Rejected, new[] { "Authorization is missing" }, Steps);
            }

            if (session == null || session.State != AuthoritySessionState.Reconnecting)
            {
                issues.Add("Session is not reconnecting");
            }

            if (version == null || !version.Known || !version.Supported)
            {
                issues.Add("Protocol version is incompatible");
                return new RecoveryDiagnostics(RecoveryVerdict.Blocked, issues, Steps);
            }

            if (!hasSnapshot && !hasDelta)
            {
                issues.Add("Snapshot or delta is required");
                return new RecoveryDiagnostics(RecoveryVerdict.NeedsSnapshot, issues, Steps);
            }

            if (snapshotStale)
            {
                issues.Add("Client snapshot is stale");
                return new RecoveryDiagnostics(RecoveryVerdict.NeedsSnapshot, issues, Steps);
            }

            return new RecoveryDiagnostics(issues.Count == 0 ? RecoveryVerdict.Recoverable : RecoveryVerdict.Blocked, issues, Steps);
        }
    }

    public enum AuthorityLoadBudgetScope
    {
        CommandsPerTick,
        Subscriptions,
        Snapshots,
        Deltas,
        JournalEntries,
        RecoveryAttempts
    }

    public sealed class AuthorityLoadBudget
    {
        public AuthorityLoadBudget(AuthorityLoadBudgetScope scope, int softLimit, int hardLimit, string degradationPriority)
        {
            Scope = scope;
            SoftLimit = softLimit;
            HardLimit = hardLimit;
            DegradationPriority = degradationPriority ?? string.Empty;
        }

        public AuthorityLoadBudgetScope Scope { get; }
        public int SoftLimit { get; }
        public int HardLimit { get; }
        public string DegradationPriority { get; }
    }

    public sealed class AuthorityLoadBudgetFinding
    {
        public AuthorityLoadBudgetFinding(AuthorityLoadBudgetScope scope, AuthorityTelemetrySeverity severity, string message)
        {
            Scope = scope;
            Severity = severity;
            Message = message ?? string.Empty;
        }

        public AuthorityLoadBudgetScope Scope { get; }
        public AuthorityTelemetrySeverity Severity { get; }
        public string Message { get; }
    }

    public sealed class AuthorityLoadBudgetDiagnostics
    {
        public AuthorityLoadBudgetDiagnostics(IReadOnlyList<AuthorityLoadBudgetFinding> findings)
        {
            Findings = findings ?? Array.Empty<AuthorityLoadBudgetFinding>();
        }

        public IReadOnlyList<AuthorityLoadBudgetFinding> Findings { get; }
        public bool HardExceeded => Findings.Any(finding => finding.Severity == AuthorityTelemetrySeverity.Critical);
    }

    public sealed class AuthorityLoadBudgetPolicy
    {
        private readonly List<AuthorityLoadBudget> budgets;

        public AuthorityLoadBudgetPolicy(IEnumerable<AuthorityLoadBudget> budgets)
        {
            this.budgets = (budgets ?? Array.Empty<AuthorityLoadBudget>()).OrderBy(budget => budget.Scope).ToList();
        }

        public IReadOnlyList<AuthorityLoadBudget> Budgets => budgets;

        public AuthorityLoadBudgetDiagnostics Evaluate(IReadOnlyDictionary<AuthorityLoadBudgetScope, int> usage)
        {
            var findings = new List<AuthorityLoadBudgetFinding>();
            foreach (AuthorityLoadBudget budget in budgets)
            {
                int value = usage != null && usage.TryGetValue(budget.Scope, out int count) ? count : 0;
                if (value > budget.HardLimit)
                {
                    findings.Add(new AuthorityLoadBudgetFinding(budget.Scope, AuthorityTelemetrySeverity.Critical, "Hard budget exceeded"));
                }
                else if (value > budget.SoftLimit)
                {
                    findings.Add(new AuthorityLoadBudgetFinding(budget.Scope, AuthorityTelemetrySeverity.Warning, "Soft budget exceeded"));
                }
            }

            return new AuthorityLoadBudgetDiagnostics(findings.OrderByDescending(finding => finding.Severity).ThenBy(finding => finding.Scope).ToList());
        }
    }

    public enum ServerHandoffVerdict
    {
        Ready,
        ReadyWithWarnings,
        NeedsRevision,
        Blocked
    }

    public sealed class ServerHandoffCriterion
    {
        public ServerHandoffCriterion(string name, bool passed, bool blocking, string detail = "")
        {
            Name = name ?? string.Empty;
            Passed = passed;
            Blocking = blocking;
            Detail = detail ?? string.Empty;
        }

        public string Name { get; }
        public bool Passed { get; }
        public bool Blocking { get; }
        public string Detail { get; }
    }

    public sealed class ServerHandoffDiagnostics
    {
        public ServerHandoffDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
    }

    public sealed class ServerHandoffReport
    {
        public ServerHandoffReport(ServerHandoffVerdict verdict, IEnumerable<ServerHandoffCriterion> criteria)
        {
            Verdict = verdict;
            Criteria = (criteria ?? Array.Empty<ServerHandoffCriterion>()).OrderBy(criterion => criterion.Name, StringComparer.Ordinal).ToList();
            Diagnostics = new ServerHandoffDiagnostics(Criteria.Where(criterion => !criterion.Passed).Select(criterion => criterion.Detail).ToList());
        }

        public ServerHandoffVerdict Verdict { get; }
        public IReadOnlyList<ServerHandoffCriterion> Criteria { get; }
        public ServerHandoffDiagnostics Diagnostics { get; }
    }

    public sealed class ServerHandoffGate
    {
        public ServerHandoffReport Evaluate(IEnumerable<ServerHandoffCriterion> criteria, AuthorityLoadBudgetDiagnostics budgetDiagnostics = null)
        {
            var allCriteria = (criteria ?? Array.Empty<ServerHandoffCriterion>()).ToList();
            if (budgetDiagnostics != null)
            {
                foreach (AuthorityLoadBudgetFinding finding in budgetDiagnostics.Findings)
                {
                    allCriteria.Add(new ServerHandoffCriterion($"budget-{finding.Scope}", false, finding.Severity == AuthorityTelemetrySeverity.Critical, finding.Message));
                }
            }

            ServerHandoffVerdict verdict;
            if (allCriteria.Any(criterion => !criterion.Passed && criterion.Blocking))
            {
                verdict = ServerHandoffVerdict.Blocked;
            }
            else if (allCriteria.Any(criterion => !criterion.Passed && !criterion.Blocking))
            {
                verdict = ServerHandoffVerdict.NeedsRevision;
            }
            else if (budgetDiagnostics != null && budgetDiagnostics.Findings.Any(finding => finding.Severity == AuthorityTelemetrySeverity.Warning))
            {
                verdict = ServerHandoffVerdict.ReadyWithWarnings;
            }
            else
            {
                verdict = ServerHandoffVerdict.Ready;
            }

            return new ServerHandoffReport(verdict, allCriteria);
        }
    }
}
