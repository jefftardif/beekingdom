using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Networking
{
    public enum ClientPredictionScope { VisualOnly, InputBuffer, ReadModel, ServerOnly, Forbidden }
    public enum ClientPredictionLimit { WithinLimit, Expired, MissingReadModelSource, Irreversible }

    public sealed class ClientPredictionRule
    {
        public ClientPredictionRule(string action, ClientPredictionScope scope, long maxLogicalTicks, string cancelCondition)
        {
            Action = action ?? string.Empty;
            Scope = scope;
            MaxLogicalTicks = maxLogicalTicks;
            CancelCondition = cancelCondition ?? string.Empty;
        }

        public string Action { get; }
        public ClientPredictionScope Scope { get; }
        public long MaxLogicalTicks { get; }
        public string CancelCondition { get; }
    }

    public sealed class ClientPredictionDiagnostics
    {
        public ClientPredictionDiagnostics(bool allowed, ClientPredictionLimit limit, string reason)
        {
            Allowed = allowed;
            Limit = limit;
            Reason = reason ?? string.Empty;
        }

        public bool Allowed { get; }
        public ClientPredictionLimit Limit { get; }
        public string Reason { get; }
    }

    public sealed class ClientPredictionContract
    {
        private readonly List<ClientPredictionRule> rules;

        public ClientPredictionContract(IEnumerable<ClientPredictionRule> rules)
        {
            this.rules = (rules ?? Array.Empty<ClientPredictionRule>()).OrderBy(rule => rule.Action, StringComparer.Ordinal).ToList();
        }

        public IReadOnlyList<ClientPredictionRule> Rules => rules;

        public ClientPredictionDiagnostics Evaluate(string action, long ageTicks, bool readModelSourcePresent)
        {
            ClientPredictionRule rule = rules.FirstOrDefault(candidate => candidate.Action == action);
            if (rule == null || rule.Scope == ClientPredictionScope.ServerOnly || rule.Scope == ClientPredictionScope.Forbidden)
            {
                return new ClientPredictionDiagnostics(false, ClientPredictionLimit.Irreversible, "Action is not client-predictable");
            }

            if (!readModelSourcePresent)
            {
                return new ClientPredictionDiagnostics(false, ClientPredictionLimit.MissingReadModelSource, "Read model source is missing");
            }

            if (ageTicks > rule.MaxLogicalTicks)
            {
                return new ClientPredictionDiagnostics(false, ClientPredictionLimit.Expired, "Prediction exceeded logical tick limit");
            }

            return new ClientPredictionDiagnostics(true, ClientPredictionLimit.WithinLimit, rule.CancelCondition);
        }
    }

    public enum PredictionInputStatus { Pending, Acknowledged, Rejected, Expired, Invalidated }

    public sealed class PredictionInputEntry
    {
        public PredictionInputEntry(string commandId, long sequence, long tick, string action, PredictionInputStatus status = PredictionInputStatus.Pending, string rejectionReason = "")
        {
            CommandId = commandId ?? string.Empty;
            Sequence = sequence;
            Tick = tick;
            Action = action ?? string.Empty;
            Status = status;
            RejectionReason = rejectionReason ?? string.Empty;
        }

        public string CommandId { get; }
        public long Sequence { get; }
        public long Tick { get; }
        public string Action { get; }
        public PredictionInputStatus Status { get; }
        public string RejectionReason { get; }

        public PredictionInputEntry WithStatus(PredictionInputStatus status, string reason = "") => new PredictionInputEntry(CommandId, Sequence, Tick, Action, status, reason);
    }

    public sealed class PredictionInputReplayCursor
    {
        public PredictionInputReplayCursor(long afterSequence) { AfterSequence = afterSequence; }
        public long AfterSequence { get; }
    }

    public sealed class PredictionInputBufferDiagnostics
    {
        public PredictionInputBufferDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); }
        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class PredictionInputBuffer
    {
        private readonly List<PredictionInputEntry> entries = new List<PredictionInputEntry>();
        public IReadOnlyList<PredictionInputEntry> Entries => Ordered(entries);

        public PredictionInputBufferDiagnostics Add(PredictionInputEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.CommandId))
            {
                return new PredictionInputBufferDiagnostics(new[] { "Prediction input is missing command id" });
            }

            entries.Add(entry);
            return new PredictionInputBufferDiagnostics(Array.Empty<string>());
        }

        public void Acknowledge(string commandId) => Replace(commandId, PredictionInputStatus.Acknowledged);
        public void Reject(string commandId, string reason) => Replace(commandId, PredictionInputStatus.Invalidated, reason);

        public void Expire(long currentTick, long maxAgeTicks)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Status == PredictionInputStatus.Pending && currentTick - entries[i].Tick > maxAgeTicks)
                {
                    entries[i] = entries[i].WithStatus(PredictionInputStatus.Expired, "Expired by logical tick");
                }
            }
        }

        public IReadOnlyList<PredictionInputEntry> Replay(PredictionInputReplayCursor cursor)
        {
            long after = cursor?.AfterSequence ?? -1;
            return Ordered(entries.Where(entry => entry.Sequence > after));
        }

        private void Replace(string commandId, PredictionInputStatus status, string reason = "")
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].CommandId == commandId)
                {
                    entries[i] = entries[i].WithStatus(status, reason);
                }
            }
        }

        private static IReadOnlyList<PredictionInputEntry> Ordered(IEnumerable<PredictionInputEntry> source)
        {
            return source.OrderBy(entry => entry.Sequence).ThenBy(entry => entry.Tick).ThenBy(entry => entry.CommandId, StringComparer.Ordinal).ToList();
        }
    }

    public enum SnapshotDifferenceKind { Field, Collection, Version, Digest, Tick }
    public enum SnapshotDifferenceSeverity { Info, Warning, Error, Critical }

    public sealed class SnapshotDifference
    {
        public SnapshotDifference(SnapshotDifferenceKind kind, SnapshotDifferenceSeverity severity, string path, string clientValue, string authorityValue)
        {
            Kind = kind;
            Severity = severity;
            Path = path ?? string.Empty;
            ClientValue = clientValue ?? string.Empty;
            AuthorityValue = authorityValue ?? string.Empty;
        }

        public SnapshotDifferenceKind Kind { get; }
        public SnapshotDifferenceSeverity Severity { get; }
        public string Path { get; }
        public string ClientValue { get; }
        public string AuthorityValue { get; }
    }

    public sealed class SnapshotCorrectionHint
    {
        public SnapshotCorrectionHint(string path, string hint) { Path = path ?? string.Empty; Hint = hint ?? string.Empty; }
        public string Path { get; }
        public string Hint { get; }
    }

    public sealed class SnapshotComparatorDiagnostics
    {
        public SnapshotComparatorDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); }
        public IReadOnlyList<string> Issues { get; }
    }

    public sealed class SnapshotComparisonResult
    {
        public SnapshotComparisonResult(IReadOnlyList<SnapshotDifference> differences, IReadOnlyList<SnapshotCorrectionHint> hints, SnapshotComparatorDiagnostics diagnostics)
        {
            Differences = differences ?? Array.Empty<SnapshotDifference>();
            Hints = hints ?? Array.Empty<SnapshotCorrectionHint>();
            Diagnostics = diagnostics ?? new SnapshotComparatorDiagnostics(Array.Empty<string>());
        }

        public IReadOnlyList<SnapshotDifference> Differences { get; }
        public IReadOnlyList<SnapshotCorrectionHint> Hints { get; }
        public SnapshotComparatorDiagnostics Diagnostics { get; }
        public bool Matches => Differences.Count == 0 && Diagnostics.Issues.Count == 0;
    }

    public sealed class ReconciliationSnapshotComparator
    {
        public SnapshotComparisonResult Compare(
            IReadOnlyDictionary<string, string> client,
            IReadOnlyDictionary<string, string> authority,
            ProtocolVersion clientVersion,
            ProtocolVersion authorityVersion,
            string clientDigest,
            string authorityDigest,
            long clientTick,
            long authorityTick)
        {
            var diffs = new List<SnapshotDifference>();
            var issues = new List<string>();
            if (!clientVersion.Equals(authorityVersion))
            {
                issues.Add("Protocol version mismatch");
                diffs.Add(new SnapshotDifference(SnapshotDifferenceKind.Version, SnapshotDifferenceSeverity.Critical, "version", clientVersion.ToString(), authorityVersion.ToString()));
            }

            if (clientDigest != authorityDigest)
            {
                diffs.Add(new SnapshotDifference(SnapshotDifferenceKind.Digest, SnapshotDifferenceSeverity.Error, "digest", clientDigest, authorityDigest));
            }

            if (clientTick != authorityTick)
            {
                diffs.Add(new SnapshotDifference(SnapshotDifferenceKind.Tick, SnapshotDifferenceSeverity.Warning, "tick", clientTick.ToString(), authorityTick.ToString()));
            }

            foreach (string key in (client?.Keys ?? Array.Empty<string>()).Union(authority?.Keys ?? Array.Empty<string>()).OrderBy(key => key, StringComparer.Ordinal))
            {
                string left = client != null && client.TryGetValue(key, out string c) ? c : string.Empty;
                string right = authority != null && authority.TryGetValue(key, out string a) ? a : string.Empty;
                if (left != right)
                {
                    diffs.Add(new SnapshotDifference(SnapshotDifferenceKind.Field, SnapshotDifferenceSeverity.Error, key, left, right));
                }
            }

            return new SnapshotComparisonResult(diffs, diffs.Select(diff => new SnapshotCorrectionHint(diff.Path, "Refresh visual read model from authority")).ToList(), new SnapshotComparatorDiagnostics(issues));
        }
    }

    public enum RollbackDomain { Unknown, Visual, InputBuffer, ReadModel, Resource, Construction, Population, World, Session }
    public enum RollbackEligibilityVerdict { RollbackAllowed, VisualCorrectionOnly, ServerOverwrite, Forbidden, Blocked }

    public sealed class RollbackEligibilityRule
    {
        public RollbackEligibilityRule(RollbackDomain domain, RollbackEligibilityVerdict verdict)
        {
            Domain = domain;
            Verdict = verdict;
        }

        public RollbackDomain Domain { get; }
        public RollbackEligibilityVerdict Verdict { get; }
    }

    public sealed class RollbackEligibilityDiagnostics
    {
        public RollbackEligibilityDiagnostics(RollbackEligibilityVerdict verdict, string reason)
        {
            Verdict = verdict;
            Reason = reason ?? string.Empty;
        }

        public RollbackEligibilityVerdict Verdict { get; }
        public string Reason { get; }
    }

    public sealed class RollbackEligibilityPolicy
    {
        private readonly List<RollbackEligibilityRule> rules;

        public RollbackEligibilityPolicy(IEnumerable<RollbackEligibilityRule> rules = null)
        {
            this.rules = (rules ?? DefaultRules()).OrderBy(rule => rule.Domain).ToList();
        }

        public IReadOnlyList<RollbackEligibilityRule> Rules => rules;

        public RollbackEligibilityDiagnostics Evaluate(RollbackDomain domain)
        {
            RollbackEligibilityRule rule = rules.FirstOrDefault(candidate => candidate.Domain == domain);
            return rule == null || domain == RollbackDomain.Unknown
                ? new RollbackEligibilityDiagnostics(RollbackEligibilityVerdict.Blocked, "Unknown rollback domain")
                : new RollbackEligibilityDiagnostics(rule.Verdict, "Policy only; no correction applied");
        }

        private static IEnumerable<RollbackEligibilityRule> DefaultRules()
        {
            yield return new RollbackEligibilityRule(RollbackDomain.Visual, RollbackEligibilityVerdict.RollbackAllowed);
            yield return new RollbackEligibilityRule(RollbackDomain.InputBuffer, RollbackEligibilityVerdict.RollbackAllowed);
            yield return new RollbackEligibilityRule(RollbackDomain.ReadModel, RollbackEligibilityVerdict.ServerOverwrite);
            yield return new RollbackEligibilityRule(RollbackDomain.Resource, RollbackEligibilityVerdict.Forbidden);
            yield return new RollbackEligibilityRule(RollbackDomain.Construction, RollbackEligibilityVerdict.ServerOverwrite);
            yield return new RollbackEligibilityRule(RollbackDomain.Population, RollbackEligibilityVerdict.Forbidden);
            yield return new RollbackEligibilityRule(RollbackDomain.World, RollbackEligibilityVerdict.ServerOverwrite);
            yield return new RollbackEligibilityRule(RollbackDomain.Session, RollbackEligibilityVerdict.Forbidden);
        }
    }

    public enum VisualCorrectionKind { Snap, Fade, Interpolate, RefreshRequired, Blocked }
    public enum VisualCorrectionSeverity { Info, Warning, Error, Blocked }

    public sealed class VisualCorrectionEntry
    {
        public VisualCorrectionEntry(string sourceDiffPath, VisualCorrectionKind kind, VisualCorrectionSeverity severity, string targetReadModel, string clientSafeMessage)
        {
            SourceDiffPath = sourceDiffPath ?? string.Empty;
            Kind = kind;
            Severity = severity;
            TargetReadModel = targetReadModel ?? string.Empty;
            ClientSafeMessage = clientSafeMessage ?? string.Empty;
        }

        public string SourceDiffPath { get; }
        public VisualCorrectionKind Kind { get; }
        public VisualCorrectionSeverity Severity { get; }
        public string TargetReadModel { get; }
        public string ClientSafeMessage { get; }
    }

    public sealed class VisualCorrectionDiagnostics
    {
        public VisualCorrectionDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); }
        public IReadOnlyList<string> Issues { get; }
    }

    public sealed class VisualCorrectionReadModel
    {
        public VisualCorrectionReadModel(IEnumerable<VisualCorrectionEntry> entries, VisualCorrectionDiagnostics diagnostics)
        {
            Entries = (entries ?? Array.Empty<VisualCorrectionEntry>()).OrderBy(entry => entry.TargetReadModel, StringComparer.Ordinal).ThenBy(entry => entry.SourceDiffPath, StringComparer.Ordinal).ToList();
            Diagnostics = diagnostics ?? new VisualCorrectionDiagnostics(Array.Empty<string>());
        }

        public IReadOnlyList<VisualCorrectionEntry> Entries { get; }
        public VisualCorrectionDiagnostics Diagnostics { get; }

        public static VisualCorrectionReadModel FromDifferences(IEnumerable<SnapshotDifference> differences, RollbackEligibilityVerdict verdict, string targetReadModel)
        {
            var issues = new List<string>();
            var entries = new List<VisualCorrectionEntry>();
            foreach (SnapshotDifference diff in differences ?? Array.Empty<SnapshotDifference>())
            {
                if (string.IsNullOrWhiteSpace(diff.Path))
                {
                    issues.Add("Source diff is missing");
                    continue;
                }

                bool blocked = verdict == RollbackEligibilityVerdict.Forbidden || verdict == RollbackEligibilityVerdict.Blocked;
                entries.Add(new VisualCorrectionEntry(
                    diff.Path,
                    blocked ? VisualCorrectionKind.Blocked : VisualCorrectionKind.RefreshRequired,
                    blocked ? VisualCorrectionSeverity.Blocked : VisualCorrectionSeverity.Warning,
                    targetReadModel,
                    blocked ? "Update blocked until authority refresh is available." : "Display will refresh from authority."));
            }

            return new VisualCorrectionReadModel(entries, new VisualCorrectionDiagnostics(issues));
        }
    }

    public enum LatencyPattern { Delay, Jitter, PacketLoss, Reorder, Reconnect }

    public readonly struct LatencyScenarioSeed
    {
        public LatencyScenarioSeed(int value) { Value = value; }
        public int Value { get; }
    }

    public sealed class LatencyEvent
    {
        public LatencyEvent(string eventId, long tick, LatencyPattern pattern, int amount)
        {
            EventId = eventId ?? string.Empty;
            Tick = tick;
            Pattern = pattern;
            Amount = amount;
        }

        public string EventId { get; }
        public long Tick { get; }
        public LatencyPattern Pattern { get; }
        public int Amount { get; }
    }

    public sealed class LatencyScenarioDiagnostics
    {
        public LatencyScenarioDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); }
        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class LatencySimulationScenario
    {
        public LatencySimulationScenario(LatencyScenarioSeed seed, IEnumerable<LatencyPattern> patterns, int eventCount)
        {
            Seed = seed;
            Patterns = (patterns ?? Array.Empty<LatencyPattern>()).ToList();
            Events = Build(seed, Patterns, eventCount);
            Diagnostics = new LatencyScenarioDiagnostics(Events.Any(e => e.Pattern == LatencyPattern.PacketLoss) ? new[] { "Packet loss simulated" } : Array.Empty<string>());
        }

        public LatencyScenarioSeed Seed { get; }
        public IReadOnlyList<LatencyPattern> Patterns { get; }
        public IReadOnlyList<LatencyEvent> Events { get; }
        public LatencyScenarioDiagnostics Diagnostics { get; }

        private static IReadOnlyList<LatencyEvent> Build(LatencyScenarioSeed seed, IReadOnlyList<LatencyPattern> patterns, int eventCount)
        {
            var events = new List<LatencyEvent>();
            for (int i = 0; i < eventCount; i++)
            {
                LatencyPattern pattern = patterns.Count == 0 ? LatencyPattern.Delay : patterns[(seed.Value + i) % patterns.Count];
                long tick = i * 10L + Math.Abs(seed.Value % 7);
                if (pattern == LatencyPattern.Reorder)
                {
                    tick = Math.Max(0, tick - 5);
                }

                events.Add(new LatencyEvent($"latency-{i}", tick, pattern, Math.Abs((seed.Value + i * 31) % 100)));
            }

            return events.OrderBy(e => e.Tick).ThenBy(e => e.EventId, StringComparer.Ordinal).ToList();
        }
    }

    public enum ReconciliationFailureCode { Unknown, DriftMismatch, OrderMismatch, SnapshotMismatch, VersionMismatch, CommandRejected, MissingData }
    public enum ReconciliationFailureCategory { Unknown, Drift, Order, Snapshot, Version, Command, Data }

    public sealed class ReconciliationFailure
    {
        public ReconciliationFailure(ReconciliationFailureCode code, ReconciliationFailureCategory category, AuthorityTelemetrySeverity severity, string recommendedAction)
        {
            Code = code;
            Category = category;
            Severity = severity;
            RecommendedAction = recommendedAction ?? string.Empty;
        }

        public ReconciliationFailureCode Code { get; }
        public ReconciliationFailureCategory Category { get; }
        public AuthorityTelemetrySeverity Severity { get; }
        public string RecommendedAction { get; }
    }

    public sealed class ReconciliationFailureDiagnostics
    {
        public ReconciliationFailureDiagnostics(ReconciliationFailure failure) { Failure = failure; }
        public ReconciliationFailure Failure { get; }
    }

    public sealed class ReconciliationFailureCatalog
    {
        private readonly List<ReconciliationFailure> failures;

        public ReconciliationFailureCatalog()
        {
            failures = new List<ReconciliationFailure>
            {
                new ReconciliationFailure(ReconciliationFailureCode.DriftMismatch, ReconciliationFailureCategory.Drift, AuthorityTelemetrySeverity.Critical, "Compare authority digest and refresh read model."),
                new ReconciliationFailure(ReconciliationFailureCode.OrderMismatch, ReconciliationFailureCategory.Order, AuthorityTelemetrySeverity.Error, "Inspect event and input ordering."),
                new ReconciliationFailure(ReconciliationFailureCode.SnapshotMismatch, ReconciliationFailureCategory.Snapshot, AuthorityTelemetrySeverity.Error, "Request authoritative snapshot handoff."),
                new ReconciliationFailure(ReconciliationFailureCode.VersionMismatch, ReconciliationFailureCategory.Version, AuthorityTelemetrySeverity.Critical, "Block until compatible protocol version is used."),
                new ReconciliationFailure(ReconciliationFailureCode.CommandRejected, ReconciliationFailureCategory.Command, AuthorityTelemetrySeverity.Warning, "Invalidate predicted input."),
                new ReconciliationFailure(ReconciliationFailureCode.MissingData, ReconciliationFailureCategory.Data, AuthorityTelemetrySeverity.Error, "Require missing evidence before reconciliation."),
                new ReconciliationFailure(ReconciliationFailureCode.Unknown, ReconciliationFailureCategory.Unknown, AuthorityTelemetrySeverity.Warning, "Keep unknown visible for QA triage.")
            };
        }

        public IReadOnlyList<ReconciliationFailure> Failures => failures.OrderBy(failure => failure.Code).ToList();

        public ReconciliationFailureDiagnostics Map(string signal)
        {
            string normalized = signal ?? string.Empty;
            if (normalized.Contains("version", StringComparison.OrdinalIgnoreCase)) return new ReconciliationFailureDiagnostics(Get(ReconciliationFailureCode.VersionMismatch));
            if (normalized.Contains("missing", StringComparison.OrdinalIgnoreCase)) return new ReconciliationFailureDiagnostics(Get(ReconciliationFailureCode.MissingData));
            if (normalized.Contains("drift", StringComparison.OrdinalIgnoreCase) || normalized.Contains("digest", StringComparison.OrdinalIgnoreCase)) return new ReconciliationFailureDiagnostics(Get(ReconciliationFailureCode.DriftMismatch));
            if (normalized.Contains("order", StringComparison.OrdinalIgnoreCase)) return new ReconciliationFailureDiagnostics(Get(ReconciliationFailureCode.OrderMismatch));
            if (normalized.Contains("reject", StringComparison.OrdinalIgnoreCase)) return new ReconciliationFailureDiagnostics(Get(ReconciliationFailureCode.CommandRejected));
            return new ReconciliationFailureDiagnostics(Get(ReconciliationFailureCode.Unknown));
        }

        private ReconciliationFailure Get(ReconciliationFailureCode code) => failures.First(failure => failure.Code == code);
    }

    public enum ClientConsistencyVerdict { Converged, Stale, Divergent, InsufficientEvidence, Blocked }

    public sealed class ClientConsistencySample
    {
        public ClientConsistencySample(string clientId, long tick, string digest)
        {
            ClientId = clientId ?? string.Empty;
            Tick = tick;
            Digest = digest ?? string.Empty;
        }

        public string ClientId { get; }
        public long Tick { get; }
        public string Digest { get; }
    }

    public sealed class ClientConsistencyFinding
    {
        public ClientConsistencyFinding(string clientId, ClientConsistencyVerdict verdict, string reason)
        {
            ClientId = clientId ?? string.Empty;
            Verdict = verdict;
            Reason = reason ?? string.Empty;
        }

        public string ClientId { get; }
        public ClientConsistencyVerdict Verdict { get; }
        public string Reason { get; }
    }

    public sealed class ClientConsistencyDiagnostics
    {
        public ClientConsistencyDiagnostics(ClientConsistencyVerdict verdict, IReadOnlyList<ClientConsistencyFinding> findings)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<ClientConsistencyFinding>();
        }

        public ClientConsistencyVerdict Verdict { get; }
        public IReadOnlyList<ClientConsistencyFinding> Findings { get; }
    }

    public sealed class CrossClientConsistencyAudit
    {
        public ClientConsistencyDiagnostics Audit(string authoritativeDigest, long authoritativeTick, IEnumerable<ClientConsistencySample> samples, long staleToleranceTicks)
        {
            var list = (samples ?? Array.Empty<ClientConsistencySample>()).OrderBy(s => s.ClientId, StringComparer.Ordinal).ToList();
            if (string.IsNullOrWhiteSpace(authoritativeDigest))
            {
                return new ClientConsistencyDiagnostics(ClientConsistencyVerdict.InsufficientEvidence, new[] { new ClientConsistencyFinding(string.Empty, ClientConsistencyVerdict.InsufficientEvidence, "Authoritative baseline is missing") });
            }

            var findings = new List<ClientConsistencyFinding>();
            foreach (ClientConsistencySample sample in list)
            {
                if (authoritativeTick - sample.Tick > staleToleranceTicks)
                {
                    findings.Add(new ClientConsistencyFinding(sample.ClientId, ClientConsistencyVerdict.Stale, "Client sample is stale"));
                }
                else if (sample.Digest != authoritativeDigest)
                {
                    findings.Add(new ClientConsistencyFinding(sample.ClientId, ClientConsistencyVerdict.Divergent, "Client differs from authoritative baseline"));
                }
            }

            ClientConsistencyVerdict verdict = findings.Any(f => f.Verdict == ClientConsistencyVerdict.Divergent)
                ? ClientConsistencyVerdict.Divergent
                : findings.Any(f => f.Verdict == ClientConsistencyVerdict.Stale)
                    ? ClientConsistencyVerdict.Stale
                    : ClientConsistencyVerdict.Converged;

            return new ClientConsistencyDiagnostics(verdict, findings);
        }
    }

    public enum AuthorityQAEvidenceKind { Replay, Digest, Reconciliation, CrossClientAudit, Telemetry, RegionalEvidence }

    public sealed class AuthorityQAEvidenceRef
    {
        public AuthorityQAEvidenceRef(string id, AuthorityQAEvidenceKind kind, string beeSource, string qaPath)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            BeeSource = beeSource ?? string.Empty;
            QaPath = qaPath ?? string.Empty;
        }

        public string Id { get; }
        public AuthorityQAEvidenceKind Kind { get; }
        public string BeeSource { get; }
        public string QaPath { get; }
    }

    public sealed class AuthorityQAEvidenceLink
    {
        public AuthorityQAEvidenceLink(AuthorityQAEvidenceRef source, AuthorityQAEvidenceRef target)
        {
            Source = source;
            Target = target;
        }

        public AuthorityQAEvidenceRef Source { get; }
        public AuthorityQAEvidenceRef Target { get; }
    }

    public sealed class AuthorityQAEvidenceBridgeDiagnostics
    {
        public AuthorityQAEvidenceBridgeDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); }
        public IReadOnlyList<string> Issues { get; }
    }

    public sealed class AuthorityQAEvidenceBridge
    {
        public AuthorityQAEvidenceBridgeDiagnostics Diagnostics { get; private set; } = new AuthorityQAEvidenceBridgeDiagnostics(Array.Empty<string>());

        public IReadOnlyList<AuthorityQAEvidenceLink> Link(IEnumerable<AuthorityQAEvidenceRef> refs)
        {
            var issues = new List<string>();
            var references = (refs ?? Array.Empty<AuthorityQAEvidenceRef>()).OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
            var links = new List<AuthorityQAEvidenceLink>();
            foreach (AuthorityQAEvidenceRef source in references)
            {
                if (string.IsNullOrWhiteSpace(source.BeeSource))
                {
                    issues.Add($"Orphan evidence: {source.Id}");
                    continue;
                }

                AuthorityQAEvidenceRef target = references.FirstOrDefault(r => r.Id != source.Id && r.BeeSource == source.BeeSource);
                if (target != null)
                {
                    links.Add(new AuthorityQAEvidenceLink(source, target));
                }
            }

            Diagnostics = new AuthorityQAEvidenceBridgeDiagnostics(issues);
            return links.OrderBy(link => link.Source.Id, StringComparer.Ordinal).ThenBy(link => link.Target.Id, StringComparer.Ordinal).ToList();
        }
    }

    public enum PredictionReadinessVerdict { Ready, ReadyWithWarnings, NeedsRevision, Blocked }

    public sealed class PredictionReadinessCriterion
    {
        public PredictionReadinessCriterion(string name, bool passed, bool blocking, string detail = "")
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

    public sealed class PredictionReadinessDiagnostics
    {
        public PredictionReadinessDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); }
        public IReadOnlyList<string> Issues { get; }
    }

    public sealed class PredictionReadinessReport
    {
        public PredictionReadinessReport(PredictionReadinessVerdict verdict, IEnumerable<PredictionReadinessCriterion> criteria)
        {
            Verdict = verdict;
            Criteria = (criteria ?? Array.Empty<PredictionReadinessCriterion>()).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
            Diagnostics = new PredictionReadinessDiagnostics(Criteria.Where(c => !c.Passed).Select(c => c.Detail).ToList());
        }

        public PredictionReadinessVerdict Verdict { get; }
        public IReadOnlyList<PredictionReadinessCriterion> Criteria { get; }
        public PredictionReadinessDiagnostics Diagnostics { get; }
    }

    public sealed class PredictionReadinessGate
    {
        public PredictionReadinessReport Evaluate(IEnumerable<PredictionReadinessCriterion> criteria, bool latencyWarning = false)
        {
            var all = (criteria ?? Array.Empty<PredictionReadinessCriterion>()).ToList();
            PredictionReadinessVerdict verdict = all.Any(c => !c.Passed && c.Blocking)
                ? PredictionReadinessVerdict.Blocked
                : all.Any(c => !c.Passed)
                    ? PredictionReadinessVerdict.NeedsRevision
                    : latencyWarning
                        ? PredictionReadinessVerdict.ReadyWithWarnings
                        : PredictionReadinessVerdict.Ready;
            return new PredictionReadinessReport(verdict, all);
        }
    }
}
