using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.Networking
{
    public readonly struct ProtocolVersion : IComparable<ProtocolVersion>, IEquatable<ProtocolVersion>
    {
        public ProtocolVersion(int major, int minor = 0)
        {
            Major = major;
            Minor = minor;
        }

        public int Major { get; }
        public int Minor { get; }
        public bool IsDefined => Major > 0;

        public int CompareTo(ProtocolVersion other)
        {
            int major = Major.CompareTo(other.Major);
            return major != 0 ? major : Minor.CompareTo(other.Minor);
        }

        public bool Equals(ProtocolVersion other) => Major == other.Major && Minor == other.Minor;
        public override bool Equals(object obj) => obj is ProtocolVersion other && Equals(other);
        public override int GetHashCode() => (Major * 397) ^ Minor;
        public override string ToString() => $"v{Major}.{Minor}";
    }

    public enum ProtocolVersionStatus
    {
        Supported,
        Deprecated,
        Blocked,
        Migrated
    }

    public sealed class ProtocolMigrationRef
    {
        public ProtocolMigrationRef(ProtocolVersion fromVersion, ProtocolVersion toVersion, string beeSource)
        {
            FromVersion = fromVersion;
            ToVersion = toVersion;
            BeeSource = beeSource ?? string.Empty;
        }

        public ProtocolVersion FromVersion { get; }
        public ProtocolVersion ToVersion { get; }
        public string BeeSource { get; }
    }

    public sealed class ProtocolVersionDiagnostics
    {
        public ProtocolVersionDiagnostics(bool known, bool supported, string reason, ProtocolVersionStatus? status = null)
        {
            Known = known;
            Supported = supported;
            Reason = reason ?? string.Empty;
            Status = status;
        }

        public bool Known { get; }
        public bool Supported { get; }
        public string Reason { get; }
        public ProtocolVersionStatus? Status { get; }
    }

    public sealed class ProtocolVersionEntry
    {
        public ProtocolVersionEntry(ProtocolVersion version, ProtocolVersionStatus status, string beeSource, string reason = "", ProtocolMigrationRef migration = null)
        {
            Version = version;
            Status = status;
            BeeSource = beeSource ?? string.Empty;
            Reason = reason ?? string.Empty;
            Migration = migration;
        }

        public ProtocolVersion Version { get; }
        public ProtocolVersionStatus Status { get; }
        public string BeeSource { get; }
        public string Reason { get; }
        public ProtocolMigrationRef Migration { get; }
    }

    public sealed class ProtocolVersionRegistry
    {
        private readonly List<ProtocolVersionEntry> entries;

        public ProtocolVersionRegistry(IEnumerable<ProtocolVersionEntry> entries)
        {
            this.entries = (entries ?? Array.Empty<ProtocolVersionEntry>())
                .OrderBy(entry => entry.Version)
                .ToList();
        }

        public IReadOnlyList<ProtocolVersionEntry> Entries => entries;

        public static ProtocolVersionRegistry CreateDefault()
        {
            return new ProtocolVersionRegistry(new[]
            {
                new ProtocolVersionEntry(new ProtocolVersion(1, 0), ProtocolVersionStatus.Supported, "BEE-161", "Initial shared authority protocol"),
                new ProtocolVersionEntry(new ProtocolVersion(0, 9), ProtocolVersionStatus.Deprecated, "BEE-161", "Prototype compatibility only"),
                new ProtocolVersionEntry(new ProtocolVersion(0, 8), ProtocolVersionStatus.Migrated, "BEE-161", "Migrated to v1.0", new ProtocolMigrationRef(new ProtocolVersion(0, 8), new ProtocolVersion(1, 0), "BEE-161")),
                new ProtocolVersionEntry(new ProtocolVersion(0, 1), ProtocolVersionStatus.Blocked, "BEE-161", "Pre-contract prototype version")
            });
        }

        public ProtocolVersionDiagnostics Resolve(ProtocolVersion version)
        {
            ProtocolVersionEntry entry = entries.FirstOrDefault(candidate => candidate.Version.Equals(version));
            if (entry == null)
            {
                return new ProtocolVersionDiagnostics(false, false, "Unknown protocol version");
            }

            bool supported = entry.Status == ProtocolVersionStatus.Supported || entry.Status == ProtocolVersionStatus.Deprecated;
            return new ProtocolVersionDiagnostics(true, supported, entry.Reason, entry.Status);
        }
    }

    public enum SharedContractConsumer
    {
        UnityClient,
        ServerShared,
        QaReport,
        DemoReadOnly
    }

    public enum SharedContractCompatibilityVerdict
    {
        Compatible,
        Deprecated,
        Missing,
        Incompatible
    }

    public sealed class SharedContractCompatibilityCell
    {
        public SharedContractCompatibilityCell(string contractName, ProtocolVersion version, SharedContractConsumer consumer, SharedContractCompatibilityVerdict verdict, string reason = "")
        {
            ContractName = contractName ?? string.Empty;
            Version = version;
            Consumer = consumer;
            Verdict = verdict;
            Reason = reason ?? string.Empty;
        }

        public string ContractName { get; }
        public ProtocolVersion Version { get; }
        public SharedContractConsumer Consumer { get; }
        public SharedContractCompatibilityVerdict Verdict { get; }
        public string Reason { get; }
    }

    public sealed class SharedContractCompatibilityDiagnostics
    {
        public SharedContractCompatibilityDiagnostics(IReadOnlyList<SharedContractCompatibilityCell> cells)
        {
            Cells = cells ?? Array.Empty<SharedContractCompatibilityCell>();
        }

        public IReadOnlyList<SharedContractCompatibilityCell> Cells { get; }
        public bool HasIncompatibility => Cells.Any(cell => cell.Verdict == SharedContractCompatibilityVerdict.Incompatible || cell.Verdict == SharedContractCompatibilityVerdict.Missing);
    }

    public sealed class SharedContractCompatibilityMatrix
    {
        private readonly ProtocolVersionRegistry registry;
        private readonly List<SharedContractCompatibilityCell> cells;

        public SharedContractCompatibilityMatrix(ProtocolVersionRegistry registry, IEnumerable<SharedContractCompatibilityCell> cells)
        {
            this.registry = registry ?? ProtocolVersionRegistry.CreateDefault();
            this.cells = (cells ?? Array.Empty<SharedContractCompatibilityCell>())
                .OrderBy(cell => cell.ContractName, StringComparer.Ordinal)
                .ThenBy(cell => cell.Version)
                .ThenBy(cell => cell.Consumer)
                .ToList();
        }

        public IReadOnlyList<SharedContractCompatibilityCell> Cells => cells;

        public static SharedContractCompatibilityMatrix CreateDefault(ProtocolVersionRegistry registry = null)
        {
            registry = registry ?? ProtocolVersionRegistry.CreateDefault();
            ProtocolVersion current = new ProtocolVersion(1, 0);
            return new SharedContractCompatibilityMatrix(registry, new[]
            {
                new SharedContractCompatibilityCell("CommandEnvelope", current, SharedContractConsumer.UnityClient, SharedContractCompatibilityVerdict.Compatible),
                new SharedContractCompatibilityCell("CommandEnvelope", current, SharedContractConsumer.ServerShared, SharedContractCompatibilityVerdict.Compatible),
                new SharedContractCompatibilityCell("SnapshotHandoffEnvelope", current, SharedContractConsumer.UnityClient, SharedContractCompatibilityVerdict.Compatible),
                new SharedContractCompatibilityCell("SnapshotHandoffEnvelope", current, SharedContractConsumer.ServerShared, SharedContractCompatibilityVerdict.Compatible),
                new SharedContractCompatibilityCell("AuthorityTelemetryReport", current, SharedContractConsumer.QaReport, SharedContractCompatibilityVerdict.Compatible),
                new SharedContractCompatibilityCell("AuthorityTelemetryReport", current, SharedContractConsumer.DemoReadOnly, SharedContractCompatibilityVerdict.Compatible)
            });
        }

        public SharedContractCompatibilityDiagnostics Evaluate(string contractName, ProtocolVersion version)
        {
            ProtocolVersionDiagnostics protocol = registry.Resolve(version);
            if (!protocol.Known || protocol.Status == ProtocolVersionStatus.Blocked)
            {
                return new SharedContractCompatibilityDiagnostics(new[]
                {
                    new SharedContractCompatibilityCell(contractName, version, SharedContractConsumer.UnityClient, SharedContractCompatibilityVerdict.Incompatible, protocol.Reason)
                });
            }

            IReadOnlyList<SharedContractCompatibilityCell> result = cells
                .Where(cell => string.Equals(cell.ContractName, contractName, StringComparison.Ordinal) && cell.Version.Equals(version))
                .ToList();

            if (result.Count == 0)
            {
                return new SharedContractCompatibilityDiagnostics(new[]
                {
                    new SharedContractCompatibilityCell(contractName, version, SharedContractConsumer.UnityClient, SharedContractCompatibilityVerdict.Missing, "Contract is not declared in compatibility matrix")
                });
            }

            return new SharedContractCompatibilityDiagnostics(result);
        }
    }

    public enum SnapshotHandoffScope
    {
        Unknown,
        Session,
        Colony,
        Region,
        World
    }

    public sealed class SnapshotHandoffMetadata
    {
        public SnapshotHandoffMetadata(string snapshotId, string authoritativeSource, long tick, ProtocolVersion protocolVersion, SnapshotHandoffScope scope)
        {
            SnapshotId = snapshotId ?? string.Empty;
            AuthoritativeSource = authoritativeSource ?? string.Empty;
            Tick = tick;
            ProtocolVersion = protocolVersion;
            Scope = scope;
        }

        public string SnapshotId { get; }
        public string AuthoritativeSource { get; }
        public long Tick { get; }
        public ProtocolVersion ProtocolVersion { get; }
        public SnapshotHandoffScope Scope { get; }
    }

    public sealed class SnapshotHandoffPayloadRef
    {
        public SnapshotHandoffPayloadRef(string referenceId, string contentType = "logical-ref")
        {
            ReferenceId = referenceId ?? string.Empty;
            ContentType = contentType ?? string.Empty;
        }

        public string ReferenceId { get; }
        public string ContentType { get; }
    }

    public sealed class SnapshotHandoffEnvelope
    {
        public SnapshotHandoffEnvelope(SnapshotHandoffMetadata metadata, string digest, SnapshotHandoffPayloadRef payloadRef)
        {
            Metadata = metadata;
            Digest = digest ?? string.Empty;
            PayloadRef = payloadRef;
        }

        public SnapshotHandoffMetadata Metadata { get; }
        public string Digest { get; }
        public SnapshotHandoffPayloadRef PayloadRef { get; }
    }

    public sealed class SnapshotHandoffDiagnostics
    {
        public SnapshotHandoffDiagnostics(bool valid, IReadOnlyList<string> issues)
        {
            Valid = valid;
            Issues = issues ?? Array.Empty<string>();
        }

        public bool Valid { get; }
        public IReadOnlyList<string> Issues { get; }
    }

    public static class SnapshotHandoffValidator
    {
        public static SnapshotHandoffDiagnostics Validate(SnapshotHandoffEnvelope envelope, ProtocolVersionRegistry registry = null)
        {
            registry = registry ?? ProtocolVersionRegistry.CreateDefault();
            var issues = new List<string>();
            if (envelope == null)
            {
                return new SnapshotHandoffDiagnostics(false, new[] { "Envelope is missing" });
            }

            if (envelope.Metadata == null)
            {
                issues.Add("Metadata is missing");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(envelope.Metadata.AuthoritativeSource))
                {
                    issues.Add("Authoritative source is missing");
                }

                if (envelope.Metadata.Scope == SnapshotHandoffScope.Unknown)
                {
                    issues.Add("Snapshot scope is unknown");
                }

                ProtocolVersionDiagnostics version = registry.Resolve(envelope.Metadata.ProtocolVersion);
                if (!version.Known || !version.Supported)
                {
                    issues.Add("Protocol version is not compatible");
                }
            }

            if (string.IsNullOrWhiteSpace(envelope.Digest))
            {
                issues.Add("Digest is missing");
            }

            return new SnapshotHandoffDiagnostics(issues.Count == 0, issues);
        }
    }

    public sealed class ServerStateDigestField
    {
        public ServerStateDigestField(string key, string value, bool volatileField = false, bool sensitive = false)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            Volatile = volatileField;
            Sensitive = sensitive;
        }

        public string Key { get; }
        public string Value { get; }
        public bool Volatile { get; }
        public bool Sensitive { get; }
    }

    public sealed class ServerStateDigestPolicy
    {
        public ServerStateDigestPolicy(bool rejectSensitiveFields = true)
        {
            RejectSensitiveFields = rejectSensitiveFields;
        }

        public bool RejectSensitiveFields { get; }
    }

    public sealed class ServerStateDigestDiagnostics
    {
        public ServerStateDigestDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class ServerStateDigest
    {
        public ServerStateDigest(SnapshotHandoffScope scope, long tick, ProtocolVersion version, string checksum, ServerStateDigestDiagnostics diagnostics)
        {
            Scope = scope;
            Tick = tick;
            Version = version;
            Checksum = checksum ?? string.Empty;
            Diagnostics = diagnostics ?? new ServerStateDigestDiagnostics(Array.Empty<string>());
        }

        public SnapshotHandoffScope Scope { get; }
        public long Tick { get; }
        public ProtocolVersion Version { get; }
        public string Checksum { get; }
        public ServerStateDigestDiagnostics Diagnostics { get; }
    }

    public sealed class ServerStateDigestBuilder
    {
        public ServerStateDigest Build(SnapshotHandoffScope scope, long tick, ProtocolVersion version, IEnumerable<ServerStateDigestField> fields, ServerStateDigestPolicy policy = null)
        {
            policy = policy ?? new ServerStateDigestPolicy();
            var issues = new List<string>();
            var stableFields = new List<ServerStateDigestField>();

            foreach (ServerStateDigestField field in fields ?? Array.Empty<ServerStateDigestField>())
            {
                if (field.Sensitive && policy.RejectSensitiveFields)
                {
                    issues.Add($"Sensitive field rejected: {field.Key}");
                    continue;
                }

                if (!field.Volatile)
                {
                    stableFields.Add(field);
                }
            }

            string canonical = string.Join("|", stableFields
                .OrderBy(field => field.Key, StringComparer.Ordinal)
                .ThenBy(field => field.Value, StringComparer.Ordinal)
                .Select(field => $"{field.Key}={field.Value}"));

            string raw = $"{scope}:{tick}:{version}:{canonical}";
            string checksum;
            using (SHA256 sha = SHA256.Create())
            {
                checksum = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).Replace("-", string.Empty).ToLowerInvariant();
            }

            return new ServerStateDigest(scope, tick, version, checksum, new ServerStateDigestDiagnostics(issues));
        }
    }

    public sealed class ClientHydrationInput
    {
        public ClientHydrationInput(SnapshotHandoffEnvelope envelope, string expectedDigest, long currentTick, long staleAfterTicks)
        {
            Envelope = envelope;
            ExpectedDigest = expectedDigest ?? string.Empty;
            CurrentTick = currentTick;
            StaleAfterTicks = staleAfterTicks;
        }

        public SnapshotHandoffEnvelope Envelope { get; }
        public string ExpectedDigest { get; }
        public long CurrentTick { get; }
        public long StaleAfterTicks { get; }
    }

    public enum ClientHydrationStatus
    {
        Hydrated,
        HydratedStale,
        Rejected
    }

    public sealed class ClientHydrationDiagnostics
    {
        public ClientHydrationDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
    }

    public sealed class ClientHydrationResult
    {
        public ClientHydrationResult(ClientHydrationStatus status, IReadOnlyDictionary<string, string> readModel, ClientHydrationDiagnostics diagnostics)
        {
            Status = status;
            ReadModel = readModel ?? new Dictionary<string, string>();
            Diagnostics = diagnostics ?? new ClientHydrationDiagnostics(Array.Empty<string>());
        }

        public ClientHydrationStatus Status { get; }
        public IReadOnlyDictionary<string, string> ReadModel { get; }
        public ClientHydrationDiagnostics Diagnostics { get; }
    }

    public sealed class ClientReadModelHydrator
    {
        public ClientHydrationResult Hydrate(ClientHydrationInput input, ProtocolVersionRegistry registry = null)
        {
            var issues = new List<string>();
            SnapshotHandoffDiagnostics handoff = SnapshotHandoffValidator.Validate(input?.Envelope, registry);
            issues.AddRange(handoff.Issues);

            if (input == null || input.Envelope == null || input.Envelope.Digest != input.ExpectedDigest)
            {
                issues.Add("Digest mismatch");
            }

            if (issues.Count > 0)
            {
                return new ClientHydrationResult(ClientHydrationStatus.Rejected, null, new ClientHydrationDiagnostics(issues));
            }

            bool stale = input.CurrentTick - input.Envelope.Metadata.Tick > input.StaleAfterTicks;
            var model = new Dictionary<string, string>
            {
                ["snapshotId"] = input.Envelope.Metadata.SnapshotId,
                ["scope"] = input.Envelope.Metadata.Scope.ToString(),
                ["digest"] = input.Envelope.Digest,
                ["source"] = input.Envelope.Metadata.AuthoritativeSource
            };

            if (stale)
            {
                issues.Add("Snapshot is stale");
            }

            return new ClientHydrationResult(stale ? ClientHydrationStatus.HydratedStale : ClientHydrationStatus.Hydrated, model, new ClientHydrationDiagnostics(issues));
        }
    }

    public enum DeltaSyncScope
    {
        Session,
        Colony,
        Region,
        World
    }

    public enum DeltaSyncOperationKind
    {
        Add,
        Update,
        Remove,
        Replace,
        Unknown
    }

    public sealed class DeltaSyncOperation
    {
        public DeltaSyncOperation(int order, DeltaSyncOperationKind kind, string key, string value = "")
        {
            Order = order;
            Kind = kind;
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public int Order { get; }
        public DeltaSyncOperationKind Kind { get; }
        public string Key { get; }
        public string Value { get; }
    }

    public sealed class DeltaSyncContract
    {
        public DeltaSyncContract(DeltaSyncScope scope, ProtocolVersion version, string baseDigest, string targetDigest, IEnumerable<DeltaSyncOperation> operations)
        {
            Scope = scope;
            Version = version;
            BaseDigest = baseDigest ?? string.Empty;
            TargetDigest = targetDigest ?? string.Empty;
            Operations = (operations ?? Array.Empty<DeltaSyncOperation>()).OrderBy(operation => operation.Order).ThenBy(operation => operation.Key, StringComparer.Ordinal).ToList();
        }

        public DeltaSyncScope Scope { get; }
        public ProtocolVersion Version { get; }
        public string BaseDigest { get; }
        public string TargetDigest { get; }
        public IReadOnlyList<DeltaSyncOperation> Operations { get; }
    }

    public sealed class DeltaSyncDiagnostics
    {
        public DeltaSyncDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
        public bool Valid => Issues.Count == 0;
    }

    public sealed class DeltaSyncReplayPlan
    {
        public DeltaSyncReplayPlan(bool accepted, IReadOnlyDictionary<string, string> projectedState, DeltaSyncDiagnostics diagnostics)
        {
            Accepted = accepted;
            ProjectedState = projectedState ?? new Dictionary<string, string>();
            Diagnostics = diagnostics ?? new DeltaSyncDiagnostics(Array.Empty<string>());
        }

        public bool Accepted { get; }
        public IReadOnlyDictionary<string, string> ProjectedState { get; }
        public DeltaSyncDiagnostics Diagnostics { get; }
    }

    public sealed class DeltaSyncDryRun
    {
        public DeltaSyncReplayPlan Replay(DeltaSyncContract contract, IReadOnlyDictionary<string, string> baseState, string actualBaseDigest)
        {
            var issues = new List<string>();
            if (contract == null)
            {
                return new DeltaSyncReplayPlan(false, null, new DeltaSyncDiagnostics(new[] { "Delta contract is missing" }));
            }

            if (string.IsNullOrWhiteSpace(contract.BaseDigest) || string.IsNullOrWhiteSpace(contract.TargetDigest))
            {
                issues.Add("Base and target digests are required");
            }

            if (!string.Equals(contract.BaseDigest, actualBaseDigest, StringComparison.Ordinal))
            {
                issues.Add("Base digest mismatch");
            }

            var projected = new Dictionary<string, string>(baseState ?? new Dictionary<string, string>(), StringComparer.Ordinal);
            foreach (DeltaSyncOperation operation in contract.Operations)
            {
                if (operation.Kind == DeltaSyncOperationKind.Unknown)
                {
                    issues.Add($"Unknown operation: {operation.Key}");
                    continue;
                }

                if (operation.Kind == DeltaSyncOperationKind.Remove && !projected.ContainsKey(operation.Key))
                {
                    issues.Add($"Remove target missing: {operation.Key}");
                    continue;
                }

                if (operation.Kind == DeltaSyncOperationKind.Remove)
                {
                    projected.Remove(operation.Key);
                }
                else
                {
                    projected[operation.Key] = operation.Value;
                }
            }

            return new DeltaSyncReplayPlan(issues.Count == 0, projected, new DeltaSyncDiagnostics(issues));
        }
    }

    public enum AuthoritySessionState
    {
        Created,
        Joined,
        Active,
        Suspended,
        Reconnecting,
        Closed
    }

    public enum AuthoritySessionQueueImpact
    {
        PreserveQueuedCommands,
        ExpireQueuedCommands,
        RejectQueuedCommands
    }

    public sealed class AuthoritySessionContext
    {
        public AuthoritySessionContext(string sessionId, AuthoritySessionState state, long tick, int queuedCommands)
        {
            SessionId = sessionId ?? string.Empty;
            State = state;
            Tick = tick;
            QueuedCommands = queuedCommands;
        }

        public string SessionId { get; }
        public AuthoritySessionState State { get; }
        public long Tick { get; }
        public int QueuedCommands { get; }
    }

    public sealed class AuthoritySessionTransition
    {
        public AuthoritySessionTransition(AuthoritySessionState from, AuthoritySessionState to, AuthoritySessionQueueImpact queueImpact)
        {
            From = from;
            To = to;
            QueueImpact = queueImpact;
        }

        public AuthoritySessionState From { get; }
        public AuthoritySessionState To { get; }
        public AuthoritySessionQueueImpact QueueImpact { get; }
    }

    public sealed class AuthoritySessionDiagnostics
    {
        public AuthoritySessionDiagnostics(bool accepted, string reason, AuthoritySessionContext nextContext)
        {
            Accepted = accepted;
            Reason = reason ?? string.Empty;
            NextContext = nextContext;
        }

        public bool Accepted { get; }
        public string Reason { get; }
        public AuthoritySessionContext NextContext { get; }
    }

    public sealed class AuthoritySessionLifecycle
    {
        private readonly List<AuthoritySessionTransition> transitions = new List<AuthoritySessionTransition>
        {
            new AuthoritySessionTransition(AuthoritySessionState.Created, AuthoritySessionState.Joined, AuthoritySessionQueueImpact.PreserveQueuedCommands),
            new AuthoritySessionTransition(AuthoritySessionState.Joined, AuthoritySessionState.Active, AuthoritySessionQueueImpact.PreserveQueuedCommands),
            new AuthoritySessionTransition(AuthoritySessionState.Active, AuthoritySessionState.Suspended, AuthoritySessionQueueImpact.PreserveQueuedCommands),
            new AuthoritySessionTransition(AuthoritySessionState.Suspended, AuthoritySessionState.Reconnecting, AuthoritySessionQueueImpact.PreserveQueuedCommands),
            new AuthoritySessionTransition(AuthoritySessionState.Reconnecting, AuthoritySessionState.Active, AuthoritySessionQueueImpact.PreserveQueuedCommands),
            new AuthoritySessionTransition(AuthoritySessionState.Active, AuthoritySessionState.Closed, AuthoritySessionQueueImpact.ExpireQueuedCommands),
            new AuthoritySessionTransition(AuthoritySessionState.Suspended, AuthoritySessionState.Closed, AuthoritySessionQueueImpact.ExpireQueuedCommands),
            new AuthoritySessionTransition(AuthoritySessionState.Reconnecting, AuthoritySessionState.Closed, AuthoritySessionQueueImpact.ExpireQueuedCommands)
        };

        public IReadOnlyList<AuthoritySessionTransition> Transitions => transitions;

        public AuthoritySessionDiagnostics TryTransition(AuthoritySessionContext context, AuthoritySessionState targetState, long tick)
        {
            if (context == null)
            {
                return new AuthoritySessionDiagnostics(false, "Session context is missing", null);
            }

            AuthoritySessionTransition transition = transitions.FirstOrDefault(candidate => candidate.From == context.State && candidate.To == targetState);
            if (transition == null)
            {
                return new AuthoritySessionDiagnostics(false, $"Transition {context.State} -> {targetState} is not allowed", context);
            }

            int queuedCommands = transition.QueueImpact == AuthoritySessionQueueImpact.ExpireQueuedCommands ? 0 : context.QueuedCommands;
            return new AuthoritySessionDiagnostics(true, string.Empty, new AuthoritySessionContext(context.SessionId, targetState, tick, queuedCommands));
        }
    }

    public enum MultiplayerDriftSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public sealed class MultiplayerDriftSample
    {
        public MultiplayerDriftSample(string clientId, SnapshotHandoffScope scope, long tick, ProtocolVersion version, string digest, bool authorized = true)
        {
            ClientId = clientId ?? string.Empty;
            Scope = scope;
            Tick = tick;
            Version = version;
            Digest = digest ?? string.Empty;
            Authorized = authorized;
        }

        public string ClientId { get; }
        public SnapshotHandoffScope Scope { get; }
        public long Tick { get; }
        public ProtocolVersion Version { get; }
        public string Digest { get; }
        public bool Authorized { get; }
    }

    public sealed class MultiplayerDriftFinding
    {
        public MultiplayerDriftFinding(string clientId, MultiplayerDriftSeverity severity, string kind, string message)
        {
            ClientId = clientId ?? string.Empty;
            Severity = severity;
            Kind = kind ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string ClientId { get; }
        public MultiplayerDriftSeverity Severity { get; }
        public string Kind { get; }
        public string Message { get; }
    }

    public sealed class MultiplayerDriftDiagnostics
    {
        public MultiplayerDriftDiagnostics(IReadOnlyList<MultiplayerDriftFinding> findings)
        {
            Findings = findings ?? Array.Empty<MultiplayerDriftFinding>();
        }

        public IReadOnlyList<MultiplayerDriftFinding> Findings { get; }
        public bool HasDrift => Findings.Any(finding => finding.Severity >= MultiplayerDriftSeverity.Error);
    }

    public sealed class MultiplayerDriftDetector
    {
        public MultiplayerDriftDiagnostics Detect(ServerStateDigest authoritativeDigest, IEnumerable<MultiplayerDriftSample> samples, long staleToleranceTicks)
        {
            var findings = new List<MultiplayerDriftFinding>();
            var sampleList = (samples ?? Array.Empty<MultiplayerDriftSample>()).OrderBy(sample => sample.ClientId, StringComparer.Ordinal).ToList();
            if (authoritativeDigest == null)
            {
                return new MultiplayerDriftDiagnostics(new[] { new MultiplayerDriftFinding(string.Empty, MultiplayerDriftSeverity.Critical, "MissingBaseline", "Authoritative digest is missing") });
            }

            if (sampleList.Count == 0)
            {
                findings.Add(new MultiplayerDriftFinding(string.Empty, MultiplayerDriftSeverity.Warning, "MissingSample", "No client sample was provided"));
            }

            foreach (MultiplayerDriftSample sample in sampleList)
            {
                if (!sample.Authorized || sample.Scope != authoritativeDigest.Scope)
                {
                    findings.Add(new MultiplayerDriftFinding(sample.ClientId, MultiplayerDriftSeverity.Error, "UnauthorizedScope", "Client sample is not authorized for this scope"));
                }

                if (!sample.Version.Equals(authoritativeDigest.Version))
                {
                    findings.Add(new MultiplayerDriftFinding(sample.ClientId, MultiplayerDriftSeverity.Error, "VersionMismatch", "Client protocol version differs from authority"));
                }

                if (authoritativeDigest.Tick - sample.Tick > staleToleranceTicks)
                {
                    findings.Add(new MultiplayerDriftFinding(sample.ClientId, MultiplayerDriftSeverity.Warning, "StaleSample", "Client sample is stale"));
                }

                if (!string.Equals(sample.Digest, authoritativeDigest.Checksum, StringComparison.Ordinal))
                {
                    findings.Add(new MultiplayerDriftFinding(sample.ClientId, MultiplayerDriftSeverity.Critical, "DigestMismatch", "Client digest differs from authority"));
                }
            }

            return new MultiplayerDriftDiagnostics(findings
                .OrderByDescending(finding => finding.Severity)
                .ThenBy(finding => finding.ClientId, StringComparer.Ordinal)
                .ThenBy(finding => finding.Kind, StringComparer.Ordinal)
                .ToList());
        }
    }

    public enum AuthorityTelemetrySeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public sealed class AuthorityTelemetryFinding
    {
        public AuthorityTelemetryFinding(string source, AuthorityTelemetrySeverity severity, string message)
        {
            Source = source ?? string.Empty;
            Severity = severity;
            Message = message ?? string.Empty;
        }

        public string Source { get; }
        public AuthorityTelemetrySeverity Severity { get; }
        public string Message { get; }
    }

    public sealed class AuthorityTelemetrySection
    {
        public AuthorityTelemetrySection(string name, IEnumerable<AuthorityTelemetryFinding> findings)
        {
            Name = name ?? string.Empty;
            Findings = (findings ?? Array.Empty<AuthorityTelemetryFinding>())
                .OrderByDescending(finding => finding.Severity)
                .ThenBy(finding => finding.Source, StringComparer.Ordinal)
                .ToList();
        }

        public string Name { get; }
        public IReadOnlyList<AuthorityTelemetryFinding> Findings { get; }
    }

    public sealed class AuthorityTelemetryDiagnostics
    {
        public AuthorityTelemetryDiagnostics(bool hasCriticalFindings)
        {
            HasCriticalFindings = hasCriticalFindings;
        }

        public bool HasCriticalFindings { get; }
    }

    public sealed class AuthorityTelemetryReport
    {
        public AuthorityTelemetryReport(IEnumerable<AuthorityTelemetrySection> sections)
        {
            Sections = (sections ?? Array.Empty<AuthorityTelemetrySection>()).OrderBy(section => section.Name, StringComparer.Ordinal).ToList();
            Diagnostics = new AuthorityTelemetryDiagnostics(Sections.SelectMany(section => section.Findings).Any(finding => finding.Severity == AuthorityTelemetrySeverity.Critical));
        }

        public IReadOnlyList<AuthorityTelemetrySection> Sections { get; }
        public AuthorityTelemetryDiagnostics Diagnostics { get; }
    }

    public static class AuthorityTelemetryReportBuilder
    {
        public static AuthorityTelemetryReport Empty()
        {
            return new AuthorityTelemetryReport(new[]
            {
                new AuthorityTelemetrySection("contracts", Array.Empty<AuthorityTelemetryFinding>()),
                new AuthorityTelemetrySection("deltas", Array.Empty<AuthorityTelemetryFinding>()),
                new AuthorityTelemetrySection("digests", Array.Empty<AuthorityTelemetryFinding>()),
                new AuthorityTelemetrySection("drift", Array.Empty<AuthorityTelemetryFinding>()),
                new AuthorityTelemetrySection("protocol", Array.Empty<AuthorityTelemetryFinding>()),
                new AuthorityTelemetrySection("sessions", Array.Empty<AuthorityTelemetryFinding>()),
                new AuthorityTelemetrySection("snapshots", Array.Empty<AuthorityTelemetryFinding>()),
                new AuthorityTelemetrySection("unresolved-risks", Array.Empty<AuthorityTelemetryFinding>())
            });
        }

        public static AuthorityTelemetryReport FromDrift(MultiplayerDriftDiagnostics diagnostics)
        {
            IEnumerable<AuthorityTelemetryFinding> findings = (diagnostics?.Findings ?? Array.Empty<MultiplayerDriftFinding>())
                .Select(finding => new AuthorityTelemetryFinding(finding.ClientId, MapSeverity(finding.Severity), finding.Message));
            return new AuthorityTelemetryReport(new[] { new AuthorityTelemetrySection("drift", findings) });
        }

        private static AuthorityTelemetrySeverity MapSeverity(MultiplayerDriftSeverity severity)
        {
            switch (severity)
            {
                case MultiplayerDriftSeverity.Critical:
                    return AuthorityTelemetrySeverity.Critical;
                case MultiplayerDriftSeverity.Error:
                    return AuthorityTelemetrySeverity.Error;
                case MultiplayerDriftSeverity.Warning:
                    return AuthorityTelemetrySeverity.Warning;
                default:
                    return AuthorityTelemetrySeverity.Info;
            }
        }
    }

    public enum ProtocolReadinessVerdict
    {
        Ready,
        ReadyWithWarnings,
        NeedsRevision,
        Blocked
    }

    public sealed class ProtocolReadinessCriterion
    {
        public ProtocolReadinessCriterion(string name, bool passed, bool blocking, string detail = "")
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

    public sealed class ProtocolReadinessDiagnostics
    {
        public ProtocolReadinessDiagnostics(IReadOnlyList<string> issues)
        {
            Issues = issues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Issues { get; }
    }

    public sealed class ProtocolReadinessReport
    {
        public ProtocolReadinessReport(ProtocolReadinessVerdict verdict, IEnumerable<ProtocolReadinessCriterion> criteria)
        {
            Verdict = verdict;
            Criteria = (criteria ?? Array.Empty<ProtocolReadinessCriterion>()).OrderBy(criterion => criterion.Name, StringComparer.Ordinal).ToList();
            Diagnostics = new ProtocolReadinessDiagnostics(Criteria.Where(criterion => !criterion.Passed).Select(criterion => criterion.Detail).ToList());
        }

        public ProtocolReadinessVerdict Verdict { get; }
        public IReadOnlyList<ProtocolReadinessCriterion> Criteria { get; }
        public ProtocolReadinessDiagnostics Diagnostics { get; }
    }

    public sealed class ProtocolReadinessGate
    {
        public ProtocolReadinessReport Evaluate(IEnumerable<ProtocolReadinessCriterion> criteria, AuthorityTelemetryReport telemetryReport)
        {
            var allCriteria = (criteria ?? Array.Empty<ProtocolReadinessCriterion>()).ToList();
            if (telemetryReport == null)
            {
                allCriteria.Add(new ProtocolReadinessCriterion("telemetry-report", false, false, "Authority telemetry report is missing"));
            }
            else if (telemetryReport.Diagnostics.HasCriticalFindings)
            {
                allCriteria.Add(new ProtocolReadinessCriterion("telemetry-critical", false, true, "Authority telemetry contains critical findings"));
            }

            ProtocolReadinessVerdict verdict;
            if (allCriteria.Any(criterion => !criterion.Passed && criterion.Blocking))
            {
                verdict = ProtocolReadinessVerdict.Blocked;
            }
            else if (allCriteria.Any(criterion => !criterion.Passed))
            {
                verdict = ProtocolReadinessVerdict.NeedsRevision;
            }
            else if (telemetryReport != null && telemetryReport.Sections.SelectMany(section => section.Findings).Any(finding => finding.Severity == AuthorityTelemetrySeverity.Warning))
            {
                verdict = ProtocolReadinessVerdict.ReadyWithWarnings;
            }
            else
            {
                verdict = ProtocolReadinessVerdict.Ready;
            }

            return new ProtocolReadinessReport(verdict, allCriteria);
        }
    }
}
