using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum ColonyIntegrationEvidenceSource { BeeSpec, WorkerReport, QaReport, DemoReadModel, ServerProgress, RuntimeGap }
    public enum ColonyIntegrationEvidenceStatus { Available, Missing, Blocked, Contradictory, OutOfScope }
    public enum ColonyIntegrationEvidenceDiagnosticCode { EvidenceSourceMissing, EvidenceLimitMissing, EvidenceContradiction, DemoClaimTooStrong, RuntimeProofInvented }

    public sealed class ColonyIntegrationEvidenceRecord
    {
        public ColonyIntegrationEvidenceRecord(string evidenceId, ColonyIntegrationDomain domain, string sourceBee, ColonyIntegrationEvidenceSource? source, ColonyIntegrationEvidenceStatus status, string limit, bool demoClaimTooStrong = false, bool runtimeProofInvented = false)
        {
            EvidenceId = ColonyIntegrationIds.Require(evidenceId);
            Domain = domain;
            SourceBee = sourceBee ?? string.Empty;
            Source = source;
            Status = status;
            Limit = limit ?? string.Empty;
            DemoClaimTooStrong = demoClaimTooStrong;
            RuntimeProofInvented = runtimeProofInvented;
        }

        public string EvidenceId { get; }
        public ColonyIntegrationDomain Domain { get; }
        public string SourceBee { get; }
        public ColonyIntegrationEvidenceSource? Source { get; }
        public ColonyIntegrationEvidenceStatus Status { get; }
        public string Limit { get; }
        public bool DemoClaimTooStrong { get; }
        public bool RuntimeProofInvented { get; }
    }

    public sealed class ColonyIntegrationEvidenceRegistry
    {
        public ColonyIntegrationEvidenceRegistry(IReadOnlyList<ColonyIntegrationEvidenceRecord> records)
        {
            Records = (records ?? Array.Empty<ColonyIntegrationEvidenceRecord>())
                .OrderBy(r => r.Domain)
                .ThenBy(r => r.SourceBee, StringComparer.Ordinal)
                .ThenBy(r => r.EvidenceId, StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<ColonyIntegrationEvidenceRecord> Records { get; }

        public ColonyIntegrationEvidenceDiagnostics Evaluate()
        {
            var findings = new List<ColonyIntegrationEvidenceDiagnosticCode>();
            if (Records.Any(r => r.Source == null || string.IsNullOrWhiteSpace(r.SourceBee))) findings.Add(ColonyIntegrationEvidenceDiagnosticCode.EvidenceSourceMissing);
            if (Records.Any(r => string.IsNullOrWhiteSpace(r.Limit))) findings.Add(ColonyIntegrationEvidenceDiagnosticCode.EvidenceLimitMissing);
            if (Records.Any(r => r.Status == ColonyIntegrationEvidenceStatus.Contradictory)) findings.Add(ColonyIntegrationEvidenceDiagnosticCode.EvidenceContradiction);
            if (Records.Any(r => r.DemoClaimTooStrong)) findings.Add(ColonyIntegrationEvidenceDiagnosticCode.DemoClaimTooStrong);
            if (Records.Any(r => r.RuntimeProofInvented)) findings.Add(ColonyIntegrationEvidenceDiagnosticCode.RuntimeProofInvented);
            return new ColonyIntegrationEvidenceDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationEvidenceDiagnostics
    {
        public ColonyIntegrationEvidenceDiagnostics(IReadOnlyList<ColonyIntegrationEvidenceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyIntegrationEvidenceDiagnosticCode>(); }
        public IReadOnlyList<ColonyIntegrationEvidenceDiagnosticCode> Findings { get; }
        public bool Contains(ColonyIntegrationEvidenceDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyDependencyNode { World, Population, BeeAI, Construction, Resources, Defense, Strategy, Emergency, Demo }
    public enum ColonyDependencyEdgeKind { ReadModel, EventObservation, SnapshotInput, CommandIntent, ForbiddenDirectMutation }
    public enum ColonyDependencyDiagnosticCode { DependencySourceMissing, DependencyCycleDetected, DependencyOwnerMissing, ForbiddenEdgeDetected, UnstableGraphOrder }

    public sealed class ColonyDomainDependencyNode
    {
        public ColonyDomainDependencyNode(ColonyDependencyNode node, string ownerId)
        {
            Node = node;
            OwnerId = ownerId ?? string.Empty;
        }

        public ColonyDependencyNode Node { get; }
        public string OwnerId { get; }
    }

    public sealed class ColonyDependencyBlocker
    {
        public ColonyDependencyBlocker(string blockerId, string reason, bool isBlocking = true)
        {
            BlockerId = ColonyIntegrationIds.Require(blockerId);
            Reason = reason ?? string.Empty;
            IsBlocking = isBlocking;
        }

        public string BlockerId { get; }
        public string Reason { get; }
        public bool IsBlocking { get; }
    }

    public sealed class ColonyDomainDependencyEdge
    {
        public ColonyDomainDependencyEdge(ColonyDependencyNode from, ColonyDependencyNode to, ColonyDependencyEdgeKind kind, string sourceBee, string ownerId, IReadOnlyList<ColonyDependencyBlocker> blockers = null)
        {
            From = from;
            To = to;
            Kind = kind;
            SourceBee = sourceBee ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
            Blockers = blockers ?? Array.Empty<ColonyDependencyBlocker>();
        }

        public ColonyDependencyNode From { get; }
        public ColonyDependencyNode To { get; }
        public ColonyDependencyEdgeKind Kind { get; }
        public string SourceBee { get; }
        public string OwnerId { get; }
        public IReadOnlyList<ColonyDependencyBlocker> Blockers { get; }
    }

    public sealed class ColonyDomainDependencyGraph
    {
        public ColonyDomainDependencyGraph(IReadOnlyList<ColonyDomainDependencyNode> nodes, IReadOnlyList<ColonyDomainDependencyEdge> edges, bool unstableGraphOrder = false)
        {
            Nodes = (nodes ?? Array.Empty<ColonyDomainDependencyNode>()).OrderBy(n => n.Node).ToArray();
            Edges = (edges ?? Array.Empty<ColonyDomainDependencyEdge>()).OrderBy(e => e.From).ThenBy(e => e.To).ThenBy(e => e.Kind).ToArray();
            UnstableGraphOrder = unstableGraphOrder;
        }

        public IReadOnlyList<ColonyDomainDependencyNode> Nodes { get; }
        public IReadOnlyList<ColonyDomainDependencyEdge> Edges { get; }
        public bool UnstableGraphOrder { get; }

        public ColonyDependencyDiagnostics Evaluate()
        {
            var findings = new List<ColonyDependencyDiagnosticCode>();
            if (Edges.Any(e => string.IsNullOrWhiteSpace(e.SourceBee))) findings.Add(ColonyDependencyDiagnosticCode.DependencySourceMissing);
            if (Edges.Any(e => string.IsNullOrWhiteSpace(e.OwnerId)) || Nodes.Any(n => string.IsNullOrWhiteSpace(n.OwnerId))) findings.Add(ColonyDependencyDiagnosticCode.DependencyOwnerMissing);
            if (Edges.Any(e => e.Kind == ColonyDependencyEdgeKind.ForbiddenDirectMutation)) findings.Add(ColonyDependencyDiagnosticCode.ForbiddenEdgeDetected);
            if (UnstableGraphOrder) findings.Add(ColonyDependencyDiagnosticCode.UnstableGraphOrder);
            if (HasCycle(Edges)) findings.Add(ColonyDependencyDiagnosticCode.DependencyCycleDetected);
            return new ColonyDependencyDiagnostics(findings);
        }

        private static bool HasCycle(IReadOnlyList<ColonyDomainDependencyEdge> edges)
        {
            var graph = edges.GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.Select(e => e.To).ToList());
            var visiting = new HashSet<ColonyDependencyNode>();
            var visited = new HashSet<ColonyDependencyNode>();
            foreach (ColonyDependencyNode node in graph.Keys)
            {
                if (Visit(node, graph, visiting, visited)) return true;
            }

            return false;
        }

        private static bool Visit(ColonyDependencyNode node, Dictionary<ColonyDependencyNode, List<ColonyDependencyNode>> graph, HashSet<ColonyDependencyNode> visiting, HashSet<ColonyDependencyNode> visited)
        {
            if (visited.Contains(node)) return false;
            if (!visiting.Add(node)) return true;
            if (graph.TryGetValue(node, out List<ColonyDependencyNode> next))
            {
                for (int i = 0; i < next.Count; i++)
                {
                    if (Visit(next[i], graph, visiting, visited)) return true;
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }

    public sealed class ColonyDependencyDiagnostics
    {
        public ColonyDependencyDiagnostics(IReadOnlyList<ColonyDependencyDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyDependencyDiagnosticCode>(); }
        public IReadOnlyList<ColonyDependencyDiagnosticCode> Findings { get; }
        public bool Contains(ColonyDependencyDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum CrossDomainSnapshotFamily { Hive, WorldRegion, Population, AIIntent, ConstructionFootprint, ResourceLogistics, DefenseAlert, StrategyFeedback, EmergencyPropagation }
    public enum CrossDomainSnapshotStatus { Included, Missing, Blocked, OutOfScope, Stale }
    public enum CrossDomainSnapshotGap { None, FamilyMissing, ReferenceMissing, OwnershipMerged, Stale, SaveEngineBypassed }
    public enum CrossDomainSnapshotDiagnosticCode { SnapshotFamilyMissing, SourceOwnershipMerged, SnapshotReferenceMissing, StaleSnapshot, SaveEngineBypassed }

    public sealed class CrossDomainSnapshotReference
    {
        public CrossDomainSnapshotReference(CrossDomainSnapshotFamily family, string referenceId, string sourceOwnerId, int logicalVersion, CrossDomainSnapshotStatus status, string limit)
        {
            Family = family;
            ReferenceId = referenceId ?? string.Empty;
            SourceOwnerId = sourceOwnerId ?? string.Empty;
            LogicalVersion = logicalVersion;
            Status = status;
            Limit = limit ?? string.Empty;
        }

        public CrossDomainSnapshotFamily Family { get; }
        public string ReferenceId { get; }
        public string SourceOwnerId { get; }
        public int LogicalVersion { get; }
        public CrossDomainSnapshotStatus Status { get; }
        public string Limit { get; }
    }

    public class ColonyCrossDomainSnapshot
    {
        public ColonyCrossDomainSnapshot(string snapshotId, IReadOnlyList<CrossDomainSnapshotReference> references, bool sourceOwnershipMerged = false, bool saveEngineBypassed = false)
        {
            SnapshotId = ColonyIntegrationIds.Require(snapshotId);
            References = (references ?? Array.Empty<CrossDomainSnapshotReference>()).OrderBy(r => r.Family).ToArray();
            SourceOwnershipMerged = sourceOwnershipMerged;
            SaveEngineBypassed = saveEngineBypassed;
        }

        public string SnapshotId { get; }
        public IReadOnlyList<CrossDomainSnapshotReference> References { get; }
        public bool SourceOwnershipMerged { get; }
        public bool SaveEngineBypassed { get; }

        public CrossDomainSnapshotDiagnostics Evaluate()
        {
            var findings = new List<CrossDomainSnapshotDiagnosticCode>();
            if (References.Any(r => r.Status == CrossDomainSnapshotStatus.Missing)) findings.Add(CrossDomainSnapshotDiagnosticCode.SnapshotFamilyMissing);
            if (SourceOwnershipMerged || References.Any(r => string.IsNullOrWhiteSpace(r.SourceOwnerId))) findings.Add(CrossDomainSnapshotDiagnosticCode.SourceOwnershipMerged);
            if (References.Any(r => string.IsNullOrWhiteSpace(r.ReferenceId))) findings.Add(CrossDomainSnapshotDiagnosticCode.SnapshotReferenceMissing);
            if (References.Any(r => r.Status == CrossDomainSnapshotStatus.Stale)) findings.Add(CrossDomainSnapshotDiagnosticCode.StaleSnapshot);
            if (SaveEngineBypassed) findings.Add(CrossDomainSnapshotDiagnosticCode.SaveEngineBypassed);
            return new CrossDomainSnapshotDiagnostics(findings);
        }
    }

    public sealed class ColonyCrossDomainSnapshotContract : ColonyCrossDomainSnapshot
    {
        public ColonyCrossDomainSnapshotContract(string snapshotId, IReadOnlyList<CrossDomainSnapshotReference> references, bool sourceOwnershipMerged = false, bool saveEngineBypassed = false) : base(snapshotId, references, sourceOwnershipMerged, saveEngineBypassed) { }
    }

    public class CrossDomainSnapshotDiagnostics
    {
        public CrossDomainSnapshotDiagnostics(IReadOnlyList<CrossDomainSnapshotDiagnosticCode> findings) { Findings = findings ?? Array.Empty<CrossDomainSnapshotDiagnosticCode>(); }
        public IReadOnlyList<CrossDomainSnapshotDiagnosticCode> Findings { get; }
        public bool Contains(CrossDomainSnapshotDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class ColonyCrossDomainSnapshotDiagnostics : CrossDomainSnapshotDiagnostics
    {
        public ColonyCrossDomainSnapshotDiagnostics(IReadOnlyList<CrossDomainSnapshotDiagnosticCode> findings) : base(findings) { }
    }

    public enum ColonyConflictCategory { OwnershipConflict, DependencyConflict, PriorityConflict, EvidenceConflict, DemoRuntimeConflict, SchedulerPhaseConflict }
    public enum ColonyConflictSeverity { Missing, Info, Warning, Blocking, Forbidden }
    public enum ColonyConflictNextAction { None, AddEvidence, ResolveOwnership, UpdateDemoLimit, RequestArchitectureDecision, KeepBlocked }
    public enum ColonyConflictDiagnosticCode { ConflictSourceMissing, ConflictSeverityMissing, ConflictAutoResolved, ReadyStatusWithConflict, UnsafeConflictMessage }

    public sealed class ColonyIntegrationConflict
    {
        public ColonyIntegrationConflict(ColonyConflictCategory category, string sourceBee, ColonyConflictSeverity severity, string impact, ColonyConflictNextAction nextAction, string limit, bool autoResolved = false, bool readyStatusClaimed = false, bool unsafeMessage = false)
        {
            Category = category;
            SourceBee = sourceBee ?? string.Empty;
            Severity = severity;
            Impact = impact ?? string.Empty;
            NextAction = nextAction;
            Limit = limit ?? string.Empty;
            AutoResolved = autoResolved;
            ReadyStatusClaimed = readyStatusClaimed;
            UnsafeMessage = unsafeMessage;
        }

        public ColonyConflictCategory Category { get; }
        public string SourceBee { get; }
        public ColonyConflictSeverity Severity { get; }
        public string Impact { get; }
        public ColonyConflictNextAction NextAction { get; }
        public string Limit { get; }
        public bool AutoResolved { get; }
        public bool ReadyStatusClaimed { get; }
        public bool UnsafeMessage { get; }
    }

    public sealed class ColonyIntegrationConflictCatalog
    {
        public ColonyIntegrationConflictCatalog(IReadOnlyList<ColonyIntegrationConflict> conflicts)
        {
            Conflicts = (conflicts ?? Array.Empty<ColonyIntegrationConflict>()).OrderByDescending(c => c.Severity).ThenBy(c => c.Category).ToArray();
        }

        public IReadOnlyList<ColonyIntegrationConflict> Conflicts { get; }
        public ColonyIntegrationConflictDiagnostics Evaluate()
        {
            var findings = new List<ColonyConflictDiagnosticCode>();
            if (Conflicts.Any(c => string.IsNullOrWhiteSpace(c.SourceBee))) findings.Add(ColonyConflictDiagnosticCode.ConflictSourceMissing);
            if (Conflicts.Any(c => c.Severity == ColonyConflictSeverity.Missing)) findings.Add(ColonyConflictDiagnosticCode.ConflictSeverityMissing);
            if (Conflicts.Any(c => c.AutoResolved)) findings.Add(ColonyConflictDiagnosticCode.ConflictAutoResolved);
            if (Conflicts.Any(c => c.ReadyStatusClaimed && c.Severity >= ColonyConflictSeverity.Blocking)) findings.Add(ColonyConflictDiagnosticCode.ReadyStatusWithConflict);
            if (Conflicts.Any(c => c.UnsafeMessage)) findings.Add(ColonyConflictDiagnosticCode.UnsafeConflictMessage);
            return new ColonyIntegrationConflictDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationConflictDiagnostics
    {
        public ColonyIntegrationConflictDiagnostics(IReadOnlyList<ColonyConflictDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyConflictDiagnosticCode>(); }
        public IReadOnlyList<ColonyConflictDiagnosticCode> Findings { get; }
        public bool Contains(ColonyConflictDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyIntegrationPhase { ReadWorldContext, BuildReadModels, EvaluateConflicts, PublishObservations, UpdateDemoProjection, GateReview }
    public enum ColonyIntegrationPhaseDiagnosticCode { RuntimePhaseMutationRequested, SchedulerOrderUnknown, PhaseDependencyCycle, PhaseOwnerMissing, NondeterministicPhaseOrder }

    public sealed class ColonyIntegrationPhaseDependency
    {
        public ColonyIntegrationPhaseDependency(ColonyIntegrationPhase before, ColonyIntegrationPhase after, string sourceBee)
        {
            Before = before;
            After = after;
            SourceBee = sourceBee ?? string.Empty;
        }

        public ColonyIntegrationPhase Before { get; }
        public ColonyIntegrationPhase After { get; }
        public string SourceBee { get; }
    }

    public sealed class ColonyIntegrationPhaseWarning
    {
        public ColonyIntegrationPhaseWarning(ColonyIntegrationPhase phase, string message) { Phase = phase; Message = message ?? string.Empty; }
        public ColonyIntegrationPhase Phase { get; }
        public string Message { get; }
    }

    public class ColonyIntegrationSchedulerPhasePlan
    {
        public ColonyIntegrationSchedulerPhasePlan(IReadOnlyList<ColonyIntegrationPhase> phases, IReadOnlyList<ColonyIntegrationPhaseDependency> dependencies, IReadOnlyDictionary<ColonyIntegrationPhase, string> owners, bool runtimePhaseMutationRequested = false, bool schedulerOrderUnknown = false, bool nondeterministicPhaseOrder = false)
        {
            Phases = (phases ?? Array.Empty<ColonyIntegrationPhase>()).OrderBy(p => p).ToArray();
            Dependencies = (dependencies ?? Array.Empty<ColonyIntegrationPhaseDependency>()).OrderBy(d => d.Before).ThenBy(d => d.After).ToArray();
            Owners = owners ?? new Dictionary<ColonyIntegrationPhase, string>();
            RuntimePhaseMutationRequested = runtimePhaseMutationRequested;
            SchedulerOrderUnknown = schedulerOrderUnknown;
            NondeterministicPhaseOrder = nondeterministicPhaseOrder;
        }

        public IReadOnlyList<ColonyIntegrationPhase> Phases { get; }
        public IReadOnlyList<ColonyIntegrationPhaseDependency> Dependencies { get; }
        public IReadOnlyDictionary<ColonyIntegrationPhase, string> Owners { get; }
        public bool RuntimePhaseMutationRequested { get; }
        public bool SchedulerOrderUnknown { get; }
        public bool NondeterministicPhaseOrder { get; }

        public ColonyIntegrationSchedulerDiagnostics Evaluate()
        {
            var findings = new List<ColonyIntegrationPhaseDiagnosticCode>();
            if (RuntimePhaseMutationRequested) findings.Add(ColonyIntegrationPhaseDiagnosticCode.RuntimePhaseMutationRequested);
            if (SchedulerOrderUnknown) findings.Add(ColonyIntegrationPhaseDiagnosticCode.SchedulerOrderUnknown);
            if (Phases.Any(p => !Owners.TryGetValue(p, out string owner) || string.IsNullOrWhiteSpace(owner))) findings.Add(ColonyIntegrationPhaseDiagnosticCode.PhaseOwnerMissing);
            if (NondeterministicPhaseOrder) findings.Add(ColonyIntegrationPhaseDiagnosticCode.NondeterministicPhaseOrder);
            if (HasCycle()) findings.Add(ColonyIntegrationPhaseDiagnosticCode.PhaseDependencyCycle);
            return new ColonyIntegrationSchedulerDiagnostics(findings);
        }

        private bool HasCycle()
        {
            var graph = Dependencies.GroupBy(d => d.Before).ToDictionary(g => g.Key, g => g.Select(d => d.After).ToList());
            var visiting = new HashSet<ColonyIntegrationPhase>();
            var visited = new HashSet<ColonyIntegrationPhase>();
            foreach (ColonyIntegrationPhase phase in graph.Keys)
            {
                if (Visit(phase, graph, visiting, visited)) return true;
            }

            return false;
        }

        private static bool Visit(ColonyIntegrationPhase phase, Dictionary<ColonyIntegrationPhase, List<ColonyIntegrationPhase>> graph, HashSet<ColonyIntegrationPhase> visiting, HashSet<ColonyIntegrationPhase> visited)
        {
            if (visited.Contains(phase)) return false;
            if (!visiting.Add(phase)) return true;
            if (graph.TryGetValue(phase, out List<ColonyIntegrationPhase> next))
            {
                for (int i = 0; i < next.Count; i++)
                {
                    if (Visit(next[i], graph, visiting, visited)) return true;
                }
            }

            visiting.Remove(phase);
            visited.Add(phase);
            return false;
        }
    }

    public sealed class ColonyIntegrationSchedulerPhaseContract : ColonyIntegrationSchedulerPhasePlan
    {
        public ColonyIntegrationSchedulerPhaseContract(IReadOnlyList<ColonyIntegrationPhase> phases, IReadOnlyList<ColonyIntegrationPhaseDependency> dependencies, IReadOnlyDictionary<ColonyIntegrationPhase, string> owners, bool runtimePhaseMutationRequested = false, bool schedulerOrderUnknown = false, bool nondeterministicPhaseOrder = false) : base(phases, dependencies, owners, runtimePhaseMutationRequested, schedulerOrderUnknown, nondeterministicPhaseOrder) { }
    }

    public class ColonyIntegrationSchedulerDiagnostics
    {
        public ColonyIntegrationSchedulerDiagnostics(IReadOnlyList<ColonyIntegrationPhaseDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyIntegrationPhaseDiagnosticCode>(); }
        public IReadOnlyList<ColonyIntegrationPhaseDiagnosticCode> Findings { get; }
        public bool Contains(ColonyIntegrationPhaseDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class ColonyIntegrationPhaseDiagnostics : ColonyIntegrationSchedulerDiagnostics
    {
        public ColonyIntegrationPhaseDiagnostics(IReadOnlyList<ColonyIntegrationPhaseDiagnosticCode> findings) : base(findings) { }
    }

    public enum ColonyEventBridgeKind { ReadOnlyObservation, FutureDomainEvent, ForbiddenMutableEvent, DemoProjectionEvent, QaEvidenceEvent }
    public enum ColonyEventBridgeStatus { Expected, Available, Missing, Forbidden, OutOfScope }
    public enum ColonyEventBridgeDiagnosticCode { EventSourceMissing, MutableEventRequested, PayloadContractMissing, EventBusBypassed, ConsumerAmbiguous }

    public sealed class ColonyEventBridgePayloadContract
    {
        public ColonyEventBridgePayloadContract(string payloadId, string description, bool isMutable = false)
        {
            PayloadId = payloadId ?? string.Empty;
            Description = description ?? string.Empty;
            IsMutable = isMutable;
        }

        public string PayloadId { get; }
        public string Description { get; }
        public bool IsMutable { get; }
    }

    public sealed class ColonyIntegrationEventBridge
    {
        public ColonyIntegrationEventBridge(ColonyEventBridgeKind kind, string source, ColonyEventBridgePayloadContract payload, string consumer, ColonyEventBridgeStatus status, string limit, bool eventBusBypassed = false, bool consumerAmbiguous = false)
        {
            Kind = kind;
            Source = source ?? string.Empty;
            Payload = payload;
            Consumer = consumer ?? string.Empty;
            Status = status;
            Limit = limit ?? string.Empty;
            EventBusBypassed = eventBusBypassed;
            ConsumerAmbiguous = consumerAmbiguous;
        }

        public ColonyEventBridgeKind Kind { get; }
        public string Source { get; }
        public ColonyEventBridgePayloadContract Payload { get; }
        public string Consumer { get; }
        public ColonyEventBridgeStatus Status { get; }
        public string Limit { get; }
        public bool EventBusBypassed { get; }
        public bool ConsumerAmbiguous { get; }
    }

    public sealed class ColonyIntegrationEventBridgeContract
    {
        public ColonyIntegrationEventBridgeContract(IReadOnlyList<ColonyIntegrationEventBridge> bridges)
        {
            Bridges = bridges ?? Array.Empty<ColonyIntegrationEventBridge>();
        }

        public IReadOnlyList<ColonyIntegrationEventBridge> Bridges { get; }
        public ColonyIntegrationEventBridgeDiagnostics Evaluate()
        {
            var findings = new List<ColonyEventBridgeDiagnosticCode>();
            if (Bridges.Any(b => string.IsNullOrWhiteSpace(b.Source))) findings.Add(ColonyEventBridgeDiagnosticCode.EventSourceMissing);
            if (Bridges.Any(b => b.Kind == ColonyEventBridgeKind.ForbiddenMutableEvent || (b.Payload != null && b.Payload.IsMutable))) findings.Add(ColonyEventBridgeDiagnosticCode.MutableEventRequested);
            if (Bridges.Any(b => b.Payload == null || string.IsNullOrWhiteSpace(b.Payload.PayloadId))) findings.Add(ColonyEventBridgeDiagnosticCode.PayloadContractMissing);
            if (Bridges.Any(b => b.EventBusBypassed)) findings.Add(ColonyEventBridgeDiagnosticCode.EventBusBypassed);
            if (Bridges.Any(b => b.ConsumerAmbiguous || string.IsNullOrWhiteSpace(b.Consumer))) findings.Add(ColonyEventBridgeDiagnosticCode.ConsumerAmbiguous);
            return new ColonyIntegrationEventBridgeDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationEventBridgeDiagnostics
    {
        public ColonyIntegrationEventBridgeDiagnostics(IReadOnlyList<ColonyEventBridgeDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyEventBridgeDiagnosticCode>(); }
        public IReadOnlyList<ColonyEventBridgeDiagnosticCode> Findings { get; }
        public bool Contains(ColonyEventBridgeDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyDemoScenarioSurface { DEMO002, DEMO003, DEMO004, DEMO005, DEMO007, DEMO009, DEMO012 }
    public enum ColonyDemoScenarioStatus { Observable, ObservableWithWarnings, Blocked, OutOfScope, MissingReadModel }
    public enum ColonyDemoScenarioDiagnosticCode { ScenarioSourceMissing, DemoSurfaceMissing, GameplayLogicRequested, SceneModificationRequested, SuccessCriteriaMissing }

    public sealed class ColonyDemoScenario
    {
        public ColonyDemoScenario(string scenarioId, ColonyDemoScenarioSurface? surface, string sourceBee, string visualEvidence, string successCriteria, IReadOnlyList<string> limits, ColonyDemoScenarioStatus status, bool gameplayLogicRequested = false, bool sceneModificationRequested = false)
        {
            ScenarioId = ColonyIntegrationIds.Require(scenarioId);
            Surface = surface;
            SourceBee = sourceBee ?? string.Empty;
            VisualEvidence = visualEvidence ?? string.Empty;
            SuccessCriteria = successCriteria ?? string.Empty;
            Limits = limits ?? Array.Empty<string>();
            Status = status;
            GameplayLogicRequested = gameplayLogicRequested;
            SceneModificationRequested = sceneModificationRequested;
        }

        public string ScenarioId { get; }
        public ColonyDemoScenarioSurface? Surface { get; }
        public string SourceBee { get; }
        public string VisualEvidence { get; }
        public string SuccessCriteria { get; }
        public IReadOnlyList<string> Limits { get; }
        public ColonyDemoScenarioStatus Status { get; }
        public bool GameplayLogicRequested { get; }
        public bool SceneModificationRequested { get; }
    }

    public sealed class ColonyIntegrationDemoScenarioMatrix
    {
        public ColonyIntegrationDemoScenarioMatrix(IReadOnlyList<ColonyDemoScenario> scenarios)
        {
            Scenarios = (scenarios ?? Array.Empty<ColonyDemoScenario>()).OrderBy(s => s.Surface).ThenBy(s => s.ScenarioId, StringComparer.Ordinal).ToArray();
        }

        public IReadOnlyList<ColonyDemoScenario> Scenarios { get; }
        public ColonyIntegrationDemoScenarioDiagnostics Evaluate()
        {
            var findings = new List<ColonyDemoScenarioDiagnosticCode>();
            if (Scenarios.Any(s => string.IsNullOrWhiteSpace(s.SourceBee))) findings.Add(ColonyDemoScenarioDiagnosticCode.ScenarioSourceMissing);
            if (Scenarios.Any(s => s.Surface == null)) findings.Add(ColonyDemoScenarioDiagnosticCode.DemoSurfaceMissing);
            if (Scenarios.Any(s => s.GameplayLogicRequested)) findings.Add(ColonyDemoScenarioDiagnosticCode.GameplayLogicRequested);
            if (Scenarios.Any(s => s.SceneModificationRequested)) findings.Add(ColonyDemoScenarioDiagnosticCode.SceneModificationRequested);
            if (Scenarios.Any(s => string.IsNullOrWhiteSpace(s.SuccessCriteria))) findings.Add(ColonyDemoScenarioDiagnosticCode.SuccessCriteriaMissing);
            return new ColonyIntegrationDemoScenarioDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationDemoScenarioDiagnostics
    {
        public ColonyIntegrationDemoScenarioDiagnostics(IReadOnlyList<ColonyDemoScenarioDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyDemoScenarioDiagnosticCode>(); }
        public IReadOnlyList<ColonyDemoScenarioDiagnosticCode> Findings { get; }
        public bool Contains(ColonyDemoScenarioDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyQaCoverageAxis { Domain, Scenario, Evidence, Conflict, Snapshot, EventBridge, SchedulerPhase, DemoSurface }
    public enum ColonyQaCoverageStatus { CoveredByContract, NeedsQaSpec, NeedsRuntimeTest, Blocked, OutOfScope }
    public enum ColonyQaCoverageGap { None, MissingSpec, MissingRuntimeTest, HiddenGap, DemoOnly, SourceMissing }
    public enum ColonyQaCoverageDiagnosticCode { QaCoverageSourceMissing, QaSpecClaimed, RuntimeTestClaimed, CoverageGapHidden, DemoOnlyCoverage }

    public sealed class ColonyQaCoverageCell
    {
        public ColonyQaCoverageCell(ColonyQaCoverageAxis axis, string sourceBee, ColonyQaCoverageStatus status, ColonyQaCoverageGap gap, bool qaSpecClaimed = false, bool runtimeTestClaimed = false, bool coverageGapHidden = false, bool demoOnlyCoverage = false)
        {
            Axis = axis;
            SourceBee = sourceBee ?? string.Empty;
            Status = status;
            Gap = gap;
            QaSpecClaimed = qaSpecClaimed;
            RuntimeTestClaimed = runtimeTestClaimed;
            CoverageGapHidden = coverageGapHidden;
            DemoOnlyCoverage = demoOnlyCoverage;
        }

        public ColonyQaCoverageAxis Axis { get; }
        public string SourceBee { get; }
        public ColonyQaCoverageStatus Status { get; }
        public ColonyQaCoverageGap Gap { get; }
        public bool QaSpecClaimed { get; }
        public bool RuntimeTestClaimed { get; }
        public bool CoverageGapHidden { get; }
        public bool DemoOnlyCoverage { get; }
    }

    public class ColonyIntegrationQaCoverageMatrix
    {
        public ColonyIntegrationQaCoverageMatrix(IReadOnlyList<ColonyQaCoverageCell> cells) { Cells = (cells ?? Array.Empty<ColonyQaCoverageCell>()).OrderBy(c => c.Axis).ToArray(); }
        public IReadOnlyList<ColonyQaCoverageCell> Cells { get; }
        public ColonyQaCoverageDiagnostics Evaluate()
        {
            var findings = new List<ColonyQaCoverageDiagnosticCode>();
            if (Cells.Any(c => string.IsNullOrWhiteSpace(c.SourceBee))) findings.Add(ColonyQaCoverageDiagnosticCode.QaCoverageSourceMissing);
            if (Cells.Any(c => c.QaSpecClaimed)) findings.Add(ColonyQaCoverageDiagnosticCode.QaSpecClaimed);
            if (Cells.Any(c => c.RuntimeTestClaimed)) findings.Add(ColonyQaCoverageDiagnosticCode.RuntimeTestClaimed);
            if (Cells.Any(c => c.CoverageGapHidden)) findings.Add(ColonyQaCoverageDiagnosticCode.CoverageGapHidden);
            if (Cells.Any(c => c.DemoOnlyCoverage || c.Gap == ColonyQaCoverageGap.DemoOnly)) findings.Add(ColonyQaCoverageDiagnosticCode.DemoOnlyCoverage);
            return new ColonyQaCoverageDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationQACoverageMatrix : ColonyIntegrationQaCoverageMatrix
    {
        public ColonyIntegrationQACoverageMatrix(IReadOnlyList<ColonyQaCoverageCell> cells) : base(cells) { }
    }

    public class ColonyQaCoverageDiagnostics
    {
        public ColonyQaCoverageDiagnostics(IReadOnlyList<ColonyQaCoverageDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyQaCoverageDiagnosticCode>(); }
        public IReadOnlyList<ColonyQaCoverageDiagnosticCode> Findings { get; }
        public bool Contains(ColonyQaCoverageDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class ColonyIntegrationQACoverageDiagnostics : ColonyQaCoverageDiagnostics
    {
        public ColonyIntegrationQACoverageDiagnostics(IReadOnlyList<ColonyQaCoverageDiagnosticCode> findings) : base(findings) { }
    }

    public enum WorkerHandoffStatus { Ready, Blocked, NeedsEvidence, NeedsArchitecture, NeedsQa, OutOfScope }
    public enum WorkerHandoffOwner { Worker, QA, Architect, Demo, Server, Planner }
    public enum WorkerHandoffDiagnosticCode { HandoffSourceMissing, OwnerMissing, ArchitectureDecisionMissing, QaGapUnresolved, ForbiddenImplementationRequested }

    public sealed class WorkerHandoffItem
    {
        public WorkerHandoffItem(string itemId, string sourceBee, WorkerHandoffOwner? owner, WorkerHandoffStatus status, string nextAction, bool architectureDecisionMissing = false, bool qaGapUnresolved = false, bool forbiddenImplementationRequested = false)
        {
            ItemId = ColonyIntegrationIds.Require(itemId);
            SourceBee = sourceBee ?? string.Empty;
            Owner = owner;
            Status = status;
            NextAction = nextAction ?? string.Empty;
            ArchitectureDecisionMissing = architectureDecisionMissing;
            QaGapUnresolved = qaGapUnresolved;
            ForbiddenImplementationRequested = forbiddenImplementationRequested;
        }

        public string ItemId { get; }
        public string SourceBee { get; }
        public WorkerHandoffOwner? Owner { get; }
        public WorkerHandoffStatus Status { get; }
        public string NextAction { get; }
        public bool ArchitectureDecisionMissing { get; }
        public bool QaGapUnresolved { get; }
        public bool ForbiddenImplementationRequested { get; }
    }

    public sealed class ColonyIntegrationWorkerHandoffChecklist
    {
        public ColonyIntegrationWorkerHandoffChecklist(IReadOnlyList<WorkerHandoffItem> items) { Items = items ?? Array.Empty<WorkerHandoffItem>(); }
        public IReadOnlyList<WorkerHandoffItem> Items { get; }
        public ColonyIntegrationWorkerHandoffDiagnostics Evaluate()
        {
            var findings = new List<WorkerHandoffDiagnosticCode>();
            if (Items.Any(i => string.IsNullOrWhiteSpace(i.SourceBee))) findings.Add(WorkerHandoffDiagnosticCode.HandoffSourceMissing);
            if (Items.Any(i => i.Owner == null)) findings.Add(WorkerHandoffDiagnosticCode.OwnerMissing);
            if (Items.Any(i => i.ArchitectureDecisionMissing || i.Status == WorkerHandoffStatus.NeedsArchitecture)) findings.Add(WorkerHandoffDiagnosticCode.ArchitectureDecisionMissing);
            if (Items.Any(i => i.QaGapUnresolved || i.Status == WorkerHandoffStatus.NeedsQa)) findings.Add(WorkerHandoffDiagnosticCode.QaGapUnresolved);
            if (Items.Any(i => i.ForbiddenImplementationRequested)) findings.Add(WorkerHandoffDiagnosticCode.ForbiddenImplementationRequested);
            return new ColonyIntegrationWorkerHandoffDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationWorkerHandoffDiagnostics
    {
        public ColonyIntegrationWorkerHandoffDiagnostics(IReadOnlyList<WorkerHandoffDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WorkerHandoffDiagnosticCode>(); }
        public IReadOnlyList<WorkerHandoffDiagnosticCode> Findings { get; }
        public bool Contains(WorkerHandoffDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyIntegrationEvidenceVerdict { EvidenceReadyForWorker, ReadyWithWarnings, NeedsRevision, BlockedByConflict, BlockedByQaGap, BlockedByBee271Premature }
    public enum ColonyIntegrationEvidenceRisk { EvidenceGap, DependencyCycle, Conflict, QaCoverageGap, ForbiddenRuntime, Bee271Premature }
    public enum ColonyIntegrationEvidenceGateDiagnosticCode { Bee271Premature, EvidenceGapOpen, DependencyCycleOpen, ConflictOpen, QaCoverageGapOpen, ForbiddenRuntimeImplementation }

    public sealed class ColonyIntegrationEvidenceCriterion
    {
        public ColonyIntegrationEvidenceCriterion(string beeId, bool passed, bool evidenceGapOpen = false, bool dependencyCycleOpen = false, bool conflictOpen = false, bool qaCoverageGapOpen = false, bool forbiddenRuntimeImplementation = false)
        {
            BeeId = ColonyIntegrationIds.Require(beeId);
            Passed = passed;
            EvidenceGapOpen = evidenceGapOpen;
            DependencyCycleOpen = dependencyCycleOpen;
            ConflictOpen = conflictOpen;
            QaCoverageGapOpen = qaCoverageGapOpen;
            ForbiddenRuntimeImplementation = forbiddenRuntimeImplementation;
        }

        public string BeeId { get; }
        public bool Passed { get; }
        public bool EvidenceGapOpen { get; }
        public bool DependencyCycleOpen { get; }
        public bool ConflictOpen { get; }
        public bool QaCoverageGapOpen { get; }
        public bool ForbiddenRuntimeImplementation { get; }
    }

    public sealed class ColonyIntegrationEvidenceGate
    {
        public ColonyIntegrationEvidenceGate(IReadOnlyList<ColonyIntegrationEvidenceCriterion> criteria, bool bee271Referenced = false)
        {
            Criteria = criteria ?? Array.Empty<ColonyIntegrationEvidenceCriterion>();
            Bee271Referenced = bee271Referenced;
        }

        public IReadOnlyList<ColonyIntegrationEvidenceCriterion> Criteria { get; }
        public bool Bee271Referenced { get; }
        public ColonyIntegrationEvidenceGateDiagnostics Evaluate()
        {
            var findings = new List<ColonyIntegrationEvidenceGateDiagnosticCode>();
            if (Bee271Referenced) findings.Add(ColonyIntegrationEvidenceGateDiagnosticCode.Bee271Premature);
            if (Criteria.Any(c => c.EvidenceGapOpen)) findings.Add(ColonyIntegrationEvidenceGateDiagnosticCode.EvidenceGapOpen);
            if (Criteria.Any(c => c.DependencyCycleOpen)) findings.Add(ColonyIntegrationEvidenceGateDiagnosticCode.DependencyCycleOpen);
            if (Criteria.Any(c => c.ConflictOpen)) findings.Add(ColonyIntegrationEvidenceGateDiagnosticCode.ConflictOpen);
            if (Criteria.Any(c => c.QaCoverageGapOpen)) findings.Add(ColonyIntegrationEvidenceGateDiagnosticCode.QaCoverageGapOpen);
            if (Criteria.Any(c => c.ForbiddenRuntimeImplementation)) findings.Add(ColonyIntegrationEvidenceGateDiagnosticCode.ForbiddenRuntimeImplementation);

            ColonyIntegrationEvidenceVerdict verdict = findings.Contains(ColonyIntegrationEvidenceGateDiagnosticCode.Bee271Premature)
                ? ColonyIntegrationEvidenceVerdict.BlockedByBee271Premature
                : findings.Contains(ColonyIntegrationEvidenceGateDiagnosticCode.ConflictOpen)
                    ? ColonyIntegrationEvidenceVerdict.BlockedByConflict
                    : findings.Contains(ColonyIntegrationEvidenceGateDiagnosticCode.QaCoverageGapOpen)
                        ? ColonyIntegrationEvidenceVerdict.BlockedByQaGap
                        : Criteria.Any(c => !c.Passed) || findings.Count > 0
                            ? ColonyIntegrationEvidenceVerdict.NeedsRevision
                            : ColonyIntegrationEvidenceVerdict.EvidenceReadyForWorker;
            return new ColonyIntegrationEvidenceGateDiagnostics(verdict, findings);
        }
    }

    public sealed class ColonyIntegrationEvidenceGateDiagnostics
    {
        public ColonyIntegrationEvidenceGateDiagnostics(ColonyIntegrationEvidenceVerdict verdict, IReadOnlyList<ColonyIntegrationEvidenceGateDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<ColonyIntegrationEvidenceGateDiagnosticCode>(); }
        public ColonyIntegrationEvidenceVerdict Verdict { get; }
        public IReadOnlyList<ColonyIntegrationEvidenceGateDiagnosticCode> Findings { get; }
        public bool Contains(ColonyIntegrationEvidenceGateDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum RuntimeBoundarySurface { Population, AI, Construction, Resources, Defense, Strategy, Emergency, Demos, QA }
    public enum RuntimeBoundaryStatus { Observable, ContractOnly, RuntimeReadyCandidate, BlockedBeforeRuntime, OutOfScope }
    public enum RuntimeBoundaryDiagnosticCode { RuntimeSurfaceMissing, GameplayExecutionRequested, BoundaryStatusAmbiguous, ManagerReplacementRequested, EngineBypassRequested }

    public sealed class RuntimeBoundaryBlocker
    {
        public RuntimeBoundaryBlocker(string blockerId, string reason) { BlockerId = ColonyIntegrationIds.Require(blockerId); Reason = reason ?? string.Empty; }
        public string BlockerId { get; }
        public string Reason { get; }
    }

    public sealed class RuntimeBoundarySurfaceRecord
    {
        public RuntimeBoundarySurfaceRecord(RuntimeBoundarySurface? surface, RuntimeBoundaryStatus? status, string sourceBee, string evidence, string limit, bool gameplayExecutionRequested = false, bool managerReplacementRequested = false, bool engineBypassRequested = false)
        {
            Surface = surface;
            Status = status;
            SourceBee = sourceBee ?? string.Empty;
            Evidence = evidence ?? string.Empty;
            Limit = limit ?? string.Empty;
            GameplayExecutionRequested = gameplayExecutionRequested;
            ManagerReplacementRequested = managerReplacementRequested;
            EngineBypassRequested = engineBypassRequested;
        }

        public RuntimeBoundarySurface? Surface { get; }
        public RuntimeBoundaryStatus? Status { get; }
        public string SourceBee { get; }
        public string Evidence { get; }
        public string Limit { get; }
        public bool GameplayExecutionRequested { get; }
        public bool ManagerReplacementRequested { get; }
        public bool EngineBypassRequested { get; }
    }

    public sealed class ColonyIntegrationRuntimeBoundary
    {
        public ColonyIntegrationRuntimeBoundary(IReadOnlyList<RuntimeBoundarySurfaceRecord> surfaces) { Surfaces = surfaces ?? Array.Empty<RuntimeBoundarySurfaceRecord>(); }
        public IReadOnlyList<RuntimeBoundarySurfaceRecord> Surfaces { get; }
        public RuntimeBoundaryDiagnostics Evaluate()
        {
            var findings = new List<RuntimeBoundaryDiagnosticCode>();
            if (Surfaces.Any(s => s.Surface == null)) findings.Add(RuntimeBoundaryDiagnosticCode.RuntimeSurfaceMissing);
            if (Surfaces.Any(s => s.GameplayExecutionRequested)) findings.Add(RuntimeBoundaryDiagnosticCode.GameplayExecutionRequested);
            if (Surfaces.Any(s => s.Status == null)) findings.Add(RuntimeBoundaryDiagnosticCode.BoundaryStatusAmbiguous);
            if (Surfaces.Any(s => s.ManagerReplacementRequested)) findings.Add(RuntimeBoundaryDiagnosticCode.ManagerReplacementRequested);
            if (Surfaces.Any(s => s.EngineBypassRequested)) findings.Add(RuntimeBoundaryDiagnosticCode.EngineBypassRequested);
            return new RuntimeBoundaryDiagnostics(findings);
        }
    }

    public sealed class RuntimeBoundaryDiagnostics
    {
        public RuntimeBoundaryDiagnostics(IReadOnlyList<RuntimeBoundaryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<RuntimeBoundaryDiagnosticCode>(); }
        public IReadOnlyList<RuntimeBoundaryDiagnosticCode> Findings { get; }
        public bool Contains(RuntimeBoundaryDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyFixtureDomain { Population, AI, Construction, Resources, Defense, Strategy, Emergency, World }
    public enum ColonyFixtureStatus { Available, Missing, Blocked, Obsolete, OutOfScope }
    public enum ColonyFixtureDiagnosticCode { FixtureSourceMissing, FixtureSeedMissing, FixtureRuntimeDataRequested, FixtureScenarioMissing, FixtureEvidenceMissing }

    public class ColonyScenarioFixture
    {
        public ColonyScenarioFixture(string fixtureId, ColonyFixtureDomain domain, string scenarioId, int? seed, string sourceBee, string evidence, string limit, ColonyFixtureStatus status, bool runtimeDataRequested = false)
        {
            FixtureId = ColonyIntegrationIds.Require(fixtureId);
            Domain = domain;
            ScenarioId = scenarioId ?? string.Empty;
            Seed = seed;
            SourceBee = sourceBee ?? string.Empty;
            Evidence = evidence ?? string.Empty;
            Limit = limit ?? string.Empty;
            Status = status;
            RuntimeDataRequested = runtimeDataRequested;
        }

        public string FixtureId { get; }
        public ColonyFixtureDomain Domain { get; }
        public string ScenarioId { get; }
        public int? Seed { get; }
        public string SourceBee { get; }
        public string Evidence { get; }
        public string Limit { get; }
        public ColonyFixtureStatus Status { get; }
        public bool RuntimeDataRequested { get; }
    }

    public sealed class ColonyIntegrationScenarioFixtureCatalog
    {
        public ColonyIntegrationScenarioFixtureCatalog(IReadOnlyList<ColonyScenarioFixture> fixtures) { Fixtures = fixtures ?? Array.Empty<ColonyScenarioFixture>(); }
        public IReadOnlyList<ColonyScenarioFixture> Fixtures { get; }
        public ColonyIntegrationScenarioFixtureDiagnostics Evaluate()
        {
            var findings = new List<ColonyFixtureDiagnosticCode>();
            if (Fixtures.Any(f => string.IsNullOrWhiteSpace(f.SourceBee))) findings.Add(ColonyFixtureDiagnosticCode.FixtureSourceMissing);
            if (Fixtures.Any(f => f.Seed == null)) findings.Add(ColonyFixtureDiagnosticCode.FixtureSeedMissing);
            if (Fixtures.Any(f => f.RuntimeDataRequested)) findings.Add(ColonyFixtureDiagnosticCode.FixtureRuntimeDataRequested);
            if (Fixtures.Any(f => string.IsNullOrWhiteSpace(f.ScenarioId))) findings.Add(ColonyFixtureDiagnosticCode.FixtureScenarioMissing);
            if (Fixtures.Any(f => string.IsNullOrWhiteSpace(f.Evidence))) findings.Add(ColonyFixtureDiagnosticCode.FixtureEvidenceMissing);
            return new ColonyIntegrationScenarioFixtureDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationScenarioFixture : ColonyScenarioFixture
    {
        public ColonyIntegrationScenarioFixture(string fixtureId, ColonyFixtureDomain domain, string scenarioId, int? seed, string sourceBee, string evidence, string limit, ColonyFixtureStatus status, bool runtimeDataRequested = false) : base(fixtureId, domain, scenarioId, seed, sourceBee, evidence, limit, status, runtimeDataRequested) { }
    }

    public sealed class ColonyIntegrationScenarioFixtureDiagnostics
    {
        public ColonyIntegrationScenarioFixtureDiagnostics(IReadOnlyList<ColonyFixtureDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyFixtureDiagnosticCode>(); }
        public IReadOnlyList<ColonyFixtureDiagnosticCode> Findings { get; }
        public bool Contains(ColonyFixtureDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyReplayTraceStep { FixtureLoaded, ReadModelObserved, BoundaryEvaluated, EventBridgeObserved, ConflictChecked, DemoProjectionUpdated }
    public enum ColonyReplayTraceStatus { Stable, MissingStep, Blocked, OutOfScope, UnstableOrder }
    public enum ColonyReplayTraceDiagnosticCode { ReplaySystemBypassed, TraceStepMissing, CheckpointSourceMissing, ParallelGameplayExecution, TraceOrderUnstable }

    public sealed class ColonyReplayTraceCheckpoint
    {
        public ColonyReplayTraceCheckpoint(string checkpointId, string sourceBee, string expectedObservation)
        {
            CheckpointId = ColonyIntegrationIds.Require(checkpointId);
            SourceBee = sourceBee ?? string.Empty;
            ExpectedObservation = expectedObservation ?? string.Empty;
        }

        public string CheckpointId { get; }
        public string SourceBee { get; }
        public string ExpectedObservation { get; }
    }

    public sealed class ColonyReplayTraceStepRecord
    {
        public ColonyReplayTraceStepRecord(ColonyReplayTraceStep step, long order) { Step = step; Order = order; }
        public ColonyReplayTraceStep Step { get; }
        public long Order { get; }
    }

    public sealed class ColonyIntegrationReplayTrace
    {
        public ColonyIntegrationReplayTrace(string scenarioId, string fixtureId, IReadOnlyList<ColonyReplayTraceStepRecord> steps, IReadOnlyList<ColonyReplayTraceCheckpoint> checkpoints, bool replaySystemBypassed = false, bool parallelGameplayExecution = false, bool traceOrderUnstable = false)
        {
            ScenarioId = scenarioId ?? string.Empty;
            FixtureId = fixtureId ?? string.Empty;
            Steps = (steps ?? Array.Empty<ColonyReplayTraceStepRecord>()).OrderBy(s => s.Order).ToArray();
            Checkpoints = checkpoints ?? Array.Empty<ColonyReplayTraceCheckpoint>();
            ReplaySystemBypassed = replaySystemBypassed;
            ParallelGameplayExecution = parallelGameplayExecution;
            TraceOrderUnstable = traceOrderUnstable;
        }

        public string ScenarioId { get; }
        public string FixtureId { get; }
        public IReadOnlyList<ColonyReplayTraceStepRecord> Steps { get; }
        public IReadOnlyList<ColonyReplayTraceCheckpoint> Checkpoints { get; }
        public bool ReplaySystemBypassed { get; }
        public bool ParallelGameplayExecution { get; }
        public bool TraceOrderUnstable { get; }
        public ColonyIntegrationReplayTraceDiagnostics Evaluate()
        {
            var findings = new List<ColonyReplayTraceDiagnosticCode>();
            if (ReplaySystemBypassed) findings.Add(ColonyReplayTraceDiagnosticCode.ReplaySystemBypassed);
            if (Steps.Count == 0) findings.Add(ColonyReplayTraceDiagnosticCode.TraceStepMissing);
            if (Checkpoints.Any(c => string.IsNullOrWhiteSpace(c.SourceBee))) findings.Add(ColonyReplayTraceDiagnosticCode.CheckpointSourceMissing);
            if (ParallelGameplayExecution) findings.Add(ColonyReplayTraceDiagnosticCode.ParallelGameplayExecution);
            if (TraceOrderUnstable) findings.Add(ColonyReplayTraceDiagnosticCode.TraceOrderUnstable);
            return new ColonyIntegrationReplayTraceDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationReplayTraceDiagnostics
    {
        public ColonyIntegrationReplayTraceDiagnostics(IReadOnlyList<ColonyReplayTraceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyReplayTraceDiagnosticCode>(); }
        public IReadOnlyList<ColonyReplayTraceDiagnosticCode> Findings { get; }
        public bool Contains(ColonyReplayTraceDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyStateComparisonInput { ExpectedContract, ObservedReadModel, CrossDomainSnapshot, ReplayTraceCheckpoint }
    public enum ColonyStateDiffSeverity { Missing, Info, Warning, Blocking, Forbidden }
    public enum ColonyStateComparisonDiagnosticCode { ComparisonInputMissing, OwnershipMismatch, AutoCorrectionRequested, DiffSourceMissing, SeverityMissing }

    public sealed class ColonyStateComparisonInputRecord
    {
        public ColonyStateComparisonInputRecord(ColonyStateComparisonInput input, string sourceId, string ownerId)
        {
            Input = input;
            SourceId = sourceId ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
        }

        public ColonyStateComparisonInput Input { get; }
        public string SourceId { get; }
        public string OwnerId { get; }
    }

    public sealed class ColonyStateDiff
    {
        public ColonyStateDiff(string diffId, string sourceId, ColonyStateDiffSeverity severity, string explanation, string limit)
        {
            DiffId = ColonyIntegrationIds.Require(diffId);
            SourceId = sourceId ?? string.Empty;
            Severity = severity;
            Explanation = explanation ?? string.Empty;
            Limit = limit ?? string.Empty;
        }

        public string DiffId { get; }
        public string SourceId { get; }
        public ColonyStateDiffSeverity Severity { get; }
        public string Explanation { get; }
        public string Limit { get; }
    }

    public sealed class ColonyIntegrationStateComparison
    {
        public ColonyIntegrationStateComparison(IReadOnlyList<ColonyStateComparisonInputRecord> inputs, IReadOnlyList<ColonyStateDiff> diffs, bool ownershipMismatch = false, bool autoCorrectionRequested = false)
        {
            Inputs = inputs ?? Array.Empty<ColonyStateComparisonInputRecord>();
            Diffs = (diffs ?? Array.Empty<ColonyStateDiff>()).OrderByDescending(d => d.Severity).ThenBy(d => d.DiffId, StringComparer.Ordinal).ToArray();
            OwnershipMismatch = ownershipMismatch;
            AutoCorrectionRequested = autoCorrectionRequested;
        }

        public IReadOnlyList<ColonyStateComparisonInputRecord> Inputs { get; }
        public IReadOnlyList<ColonyStateDiff> Diffs { get; }
        public bool OwnershipMismatch { get; }
        public bool AutoCorrectionRequested { get; }
        public ColonyIntegrationStateComparisonDiagnostics Evaluate()
        {
            var findings = new List<ColonyStateComparisonDiagnosticCode>();
            if (Inputs.Count == 0) findings.Add(ColonyStateComparisonDiagnosticCode.ComparisonInputMissing);
            if (OwnershipMismatch || Inputs.Any(i => string.IsNullOrWhiteSpace(i.OwnerId))) findings.Add(ColonyStateComparisonDiagnosticCode.OwnershipMismatch);
            if (AutoCorrectionRequested) findings.Add(ColonyStateComparisonDiagnosticCode.AutoCorrectionRequested);
            if (Diffs.Any(d => string.IsNullOrWhiteSpace(d.SourceId))) findings.Add(ColonyStateComparisonDiagnosticCode.DiffSourceMissing);
            if (Diffs.Any(d => d.Severity == ColonyStateDiffSeverity.Missing)) findings.Add(ColonyStateComparisonDiagnosticCode.SeverityMissing);
            return new ColonyIntegrationStateComparisonDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationStateComparisonDiagnostics
    {
        public ColonyIntegrationStateComparisonDiagnostics(IReadOnlyList<ColonyStateComparisonDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyStateComparisonDiagnosticCode>(); }
        public IReadOnlyList<ColonyStateComparisonDiagnosticCode> Findings { get; }
        public bool Contains(ColonyStateComparisonDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyIntegrationFailureCategory { MissingFixture, RuntimeBoundaryViolation, ReplayTraceInvalid, StateMismatch, ConflictOpen, DemoValidationGap, BenchmarkSignalRisk, DocumentationDrift }
    public enum ColonyFailureSeverity { Missing, Info, Warning, Blocking, Forbidden }
    public enum ColonyFailureCause { Unknown, MissingEvidence, BoundaryViolation, StaleSource, HiddenDependency, UnsafeClaim, DemoGap }
    public enum ColonyFailureDiagnosticCode { FailureCodeMissing, FailureSeverityMissing, OpaqueFailureMessage, UnsafeFailureDetail, AutoFixSuggested }

    public sealed class ColonyIntegrationFailureCode
    {
        public ColonyIntegrationFailureCode(string code, ColonyIntegrationFailureCategory category, ColonyFailureSeverity severity, ColonyFailureCause cause, string demoSafeMessage, string qaOrientedMessage, string nextAction, bool unsafeDetail = false, bool autoFixSuggested = false)
        {
            Code = code ?? string.Empty;
            Category = category;
            Severity = severity;
            Cause = cause;
            DemoSafeMessage = demoSafeMessage ?? string.Empty;
            QaOrientedMessage = qaOrientedMessage ?? string.Empty;
            NextAction = nextAction ?? string.Empty;
            UnsafeDetail = unsafeDetail;
            AutoFixSuggested = autoFixSuggested;
        }

        public string Code { get; }
        public ColonyIntegrationFailureCategory Category { get; }
        public ColonyFailureSeverity Severity { get; }
        public ColonyFailureCause Cause { get; }
        public string DemoSafeMessage { get; }
        public string QaOrientedMessage { get; }
        public string NextAction { get; }
        public bool UnsafeDetail { get; }
        public bool AutoFixSuggested { get; }
    }

    public sealed class ColonyIntegrationFailureTaxonomy
    {
        public ColonyIntegrationFailureTaxonomy(IReadOnlyList<ColonyIntegrationFailureCode> failures) { Failures = failures ?? Array.Empty<ColonyIntegrationFailureCode>(); }
        public IReadOnlyList<ColonyIntegrationFailureCode> Failures { get; }
        public ColonyIntegrationFailureTaxonomyDiagnostics Evaluate()
        {
            var findings = new List<ColonyFailureDiagnosticCode>();
            if (Failures.Any(f => string.IsNullOrWhiteSpace(f.Code))) findings.Add(ColonyFailureDiagnosticCode.FailureCodeMissing);
            if (Failures.Any(f => f.Severity == ColonyFailureSeverity.Missing)) findings.Add(ColonyFailureDiagnosticCode.FailureSeverityMissing);
            if (Failures.Any(f => string.IsNullOrWhiteSpace(f.DemoSafeMessage) || string.IsNullOrWhiteSpace(f.QaOrientedMessage))) findings.Add(ColonyFailureDiagnosticCode.OpaqueFailureMessage);
            if (Failures.Any(f => f.UnsafeDetail)) findings.Add(ColonyFailureDiagnosticCode.UnsafeFailureDetail);
            if (Failures.Any(f => f.AutoFixSuggested)) findings.Add(ColonyFailureDiagnosticCode.AutoFixSuggested);
            return new ColonyIntegrationFailureTaxonomyDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationFailureTaxonomyDiagnostics
    {
        public ColonyIntegrationFailureTaxonomyDiagnostics(IReadOnlyList<ColonyFailureDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyFailureDiagnosticCode>(); }
        public IReadOnlyList<ColonyFailureDiagnosticCode> Findings { get; }
        public bool Contains(ColonyFailureDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum DemoValidationStatus { ValidForObservation, ValidWithWarnings, Blocked, OutOfScope }
    public enum DemoValidationDiagnosticCode { DemoCriterionMissing, DemoEvidenceMissing, DemoLimitMissing, DemoSpecSeparateRequested, GameplayParallelDetected }

    public sealed class DemoValidationEvidence
    {
        public DemoValidationEvidence(string evidenceId, string expectedVisualProof)
        {
            EvidenceId = evidenceId ?? string.Empty;
            ExpectedVisualProof = expectedVisualProof ?? string.Empty;
        }

        public string EvidenceId { get; }
        public string ExpectedVisualProof { get; }
    }

    public sealed class DemoValidationLimit
    {
        public DemoValidationLimit(string limitId, string description)
        {
            LimitId = limitId ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string LimitId { get; }
        public string Description { get; }
    }

    public sealed class DemoValidationCriterion
    {
        public DemoValidationCriterion(ColonyDemoScenarioSurface surface, string objective, int verificationOrder, IReadOnlyList<DemoValidationEvidence> evidences, IReadOnlyList<DemoValidationLimit> limits, DemoValidationStatus status, bool separateDemoSpecRequested = false, bool parallelGameplayDetected = false)
        {
            Surface = surface;
            Objective = objective ?? string.Empty;
            VerificationOrder = verificationOrder;
            Evidences = evidences ?? Array.Empty<DemoValidationEvidence>();
            Limits = limits ?? Array.Empty<DemoValidationLimit>();
            Status = status;
            SeparateDemoSpecRequested = separateDemoSpecRequested;
            ParallelGameplayDetected = parallelGameplayDetected;
        }

        public ColonyDemoScenarioSurface Surface { get; }
        public string Objective { get; }
        public int VerificationOrder { get; }
        public IReadOnlyList<DemoValidationEvidence> Evidences { get; }
        public IReadOnlyList<DemoValidationLimit> Limits { get; }
        public DemoValidationStatus Status { get; }
        public bool SeparateDemoSpecRequested { get; }
        public bool ParallelGameplayDetected { get; }
    }

    public sealed class ColonyIntegrationDemoValidationContract
    {
        public ColonyIntegrationDemoValidationContract(IReadOnlyList<DemoValidationCriterion> criteria) { Criteria = criteria ?? Array.Empty<DemoValidationCriterion>(); }
        public IReadOnlyList<DemoValidationCriterion> Criteria { get; }
        public ColonyIntegrationDemoValidationDiagnostics Evaluate()
        {
            var findings = new List<DemoValidationDiagnosticCode>();
            if (Criteria.Count == 0 || Criteria.Any(c => string.IsNullOrWhiteSpace(c.Objective))) findings.Add(DemoValidationDiagnosticCode.DemoCriterionMissing);
            if (Criteria.Any(c => c.Evidences.Count == 0 || c.Evidences.Any(e => string.IsNullOrWhiteSpace(e.EvidenceId)))) findings.Add(DemoValidationDiagnosticCode.DemoEvidenceMissing);
            if (Criteria.Any(c => c.Limits.Count == 0 || c.Limits.Any(l => string.IsNullOrWhiteSpace(l.LimitId)))) findings.Add(DemoValidationDiagnosticCode.DemoLimitMissing);
            if (Criteria.Any(c => c.SeparateDemoSpecRequested)) findings.Add(DemoValidationDiagnosticCode.DemoSpecSeparateRequested);
            if (Criteria.Any(c => c.ParallelGameplayDetected)) findings.Add(DemoValidationDiagnosticCode.GameplayParallelDetected);
            return new ColonyIntegrationDemoValidationDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationDemoValidationDiagnostics
    {
        public ColonyIntegrationDemoValidationDiagnostics(IReadOnlyList<DemoValidationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DemoValidationDiagnosticCode>(); }
        public IReadOnlyList<DemoValidationDiagnosticCode> Findings { get; }
        public bool Contains(DemoValidationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum BenchmarkSignalKind { ReadModelBuildCost, DependencyGraphSize, SnapshotCompareCost, EventBridgeCount, DemoProjectionCost, ConflictCount }
    public enum BenchmarkSignalStatus { SignalAvailable, SignalMissing, ThresholdProvisional, BenchmarkFinalOutOfScope }
    public enum BenchmarkSignalDiagnosticCode { SignalSourceMissing, ThresholdMissing, FinalBenchmarkClaimed, MeasurementRuntimeMissing, PerformanceRiskHidden }

    public sealed class BenchmarkSignalThreshold
    {
        public BenchmarkSignalThreshold(double warning, double blocking, bool provisional = true)
        {
            Warning = warning;
            Blocking = blocking;
            Provisional = provisional;
        }

        public double Warning { get; }
        public double Blocking { get; }
        public bool Provisional { get; }
    }

    public sealed class BenchmarkSignalRisk
    {
        public BenchmarkSignalRisk(string riskId, string description, bool hidden = false)
        {
            RiskId = riskId ?? string.Empty;
            Description = description ?? string.Empty;
            Hidden = hidden;
        }

        public string RiskId { get; }
        public string Description { get; }
        public bool Hidden { get; }
    }

    public sealed class ColonyIntegrationBenchmarkSignal
    {
        public ColonyIntegrationBenchmarkSignal(BenchmarkSignalKind kind, string sourceBee, BenchmarkSignalThreshold threshold, IReadOnlyList<BenchmarkSignalRisk> risks, BenchmarkSignalStatus status, bool finalBenchmarkClaimed = false, bool measurementRuntimeMissing = false)
        {
            Kind = kind;
            SourceBee = sourceBee ?? string.Empty;
            Threshold = threshold;
            Risks = risks ?? Array.Empty<BenchmarkSignalRisk>();
            Status = status;
            FinalBenchmarkClaimed = finalBenchmarkClaimed;
            MeasurementRuntimeMissing = measurementRuntimeMissing;
        }

        public BenchmarkSignalKind Kind { get; }
        public string SourceBee { get; }
        public BenchmarkSignalThreshold Threshold { get; }
        public IReadOnlyList<BenchmarkSignalRisk> Risks { get; }
        public BenchmarkSignalStatus Status { get; }
        public bool FinalBenchmarkClaimed { get; }
        public bool MeasurementRuntimeMissing { get; }
    }

    public sealed class BenchmarkSignalDiagnostics
    {
        public BenchmarkSignalDiagnostics(IReadOnlyList<BenchmarkSignalDiagnosticCode> findings) { Findings = findings ?? Array.Empty<BenchmarkSignalDiagnosticCode>(); }
        public IReadOnlyList<BenchmarkSignalDiagnosticCode> Findings { get; }
        public bool Contains(BenchmarkSignalDiagnosticCode code) { return Findings.Contains(code); }
        public static BenchmarkSignalDiagnostics Evaluate(IReadOnlyList<ColonyIntegrationBenchmarkSignal> signals)
        {
            IReadOnlyList<ColonyIntegrationBenchmarkSignal> list = signals ?? Array.Empty<ColonyIntegrationBenchmarkSignal>();
            var findings = new List<BenchmarkSignalDiagnosticCode>();
            if (list.Any(s => string.IsNullOrWhiteSpace(s.SourceBee))) findings.Add(BenchmarkSignalDiagnosticCode.SignalSourceMissing);
            if (list.Any(s => s.Threshold == null)) findings.Add(BenchmarkSignalDiagnosticCode.ThresholdMissing);
            if (list.Any(s => s.FinalBenchmarkClaimed)) findings.Add(BenchmarkSignalDiagnosticCode.FinalBenchmarkClaimed);
            if (list.Any(s => s.MeasurementRuntimeMissing)) findings.Add(BenchmarkSignalDiagnosticCode.MeasurementRuntimeMissing);
            if (list.Any(s => s.Risks.Any(r => r.Hidden))) findings.Add(BenchmarkSignalDiagnosticCode.PerformanceRiskHidden);
            return new BenchmarkSignalDiagnostics(findings);
        }
    }

    public enum DocumentationSyncSource { BeeSpec, WorkerReport, QaReport, ServerProgress, ArchitectPreplan, MasterBacklog, DemoReadModel }
    public enum DocumentationSyncStatus { InSync, NeedsUpdate, Missing, Stale, OutOfScope }
    public enum DocumentationSyncDiagnosticCode { DocumentationSourceMissing, BacklogMismatch, ReportStale, DemoDocGap, AutoSyncRequested }

    public sealed class DocumentationSyncGap
    {
        public DocumentationSyncGap(string gapId, string nextAction, bool demoDocGap = false)
        {
            GapId = gapId ?? string.Empty;
            NextAction = nextAction ?? string.Empty;
            DemoDocGap = demoDocGap;
        }

        public string GapId { get; }
        public string NextAction { get; }
        public bool DemoDocGap { get; }
    }

    public sealed class DocumentationSyncRecord
    {
        public DocumentationSyncRecord(DocumentationSyncSource? source, DocumentationSyncStatus status, IReadOnlyList<DocumentationSyncGap> gaps, bool backlogMismatch = false, bool reportStale = false, bool autoSyncRequested = false)
        {
            Source = source;
            Status = status;
            Gaps = gaps ?? Array.Empty<DocumentationSyncGap>();
            BacklogMismatch = backlogMismatch;
            ReportStale = reportStale;
            AutoSyncRequested = autoSyncRequested;
        }

        public DocumentationSyncSource? Source { get; }
        public DocumentationSyncStatus Status { get; }
        public IReadOnlyList<DocumentationSyncGap> Gaps { get; }
        public bool BacklogMismatch { get; }
        public bool ReportStale { get; }
        public bool AutoSyncRequested { get; }
    }

    public sealed class ColonyIntegrationDocumentationSync
    {
        public ColonyIntegrationDocumentationSync(IReadOnlyList<DocumentationSyncRecord> records) { Records = records ?? Array.Empty<DocumentationSyncRecord>(); }
        public IReadOnlyList<DocumentationSyncRecord> Records { get; }
        public ColonyIntegrationDocumentationSyncDiagnostics Evaluate()
        {
            var findings = new List<DocumentationSyncDiagnosticCode>();
            if (Records.Any(r => r.Source == null)) findings.Add(DocumentationSyncDiagnosticCode.DocumentationSourceMissing);
            if (Records.Any(r => r.BacklogMismatch)) findings.Add(DocumentationSyncDiagnosticCode.BacklogMismatch);
            if (Records.Any(r => r.ReportStale || r.Status == DocumentationSyncStatus.Stale)) findings.Add(DocumentationSyncDiagnosticCode.ReportStale);
            if (Records.Any(r => r.Gaps.Any(g => g.DemoDocGap))) findings.Add(DocumentationSyncDiagnosticCode.DemoDocGap);
            if (Records.Any(r => r.AutoSyncRequested)) findings.Add(DocumentationSyncDiagnosticCode.AutoSyncRequested);
            return new ColonyIntegrationDocumentationSyncDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationDocumentationSyncDiagnostics
    {
        public ColonyIntegrationDocumentationSyncDiagnostics(IReadOnlyList<DocumentationSyncDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DocumentationSyncDiagnosticCode>(); }
        public IReadOnlyList<DocumentationSyncDiagnosticCode> Findings { get; }
        public bool Contains(DocumentationSyncDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ReleaseRiskMilestone { Alpha, Beta, EarlyAccessCandidate, PostBeta }
    public enum ReleaseRiskOwner { Worker, QA, Architect, Demo, Server, Planner }
    public enum ReleaseRiskStatus { Informational, AtRisk, Blocked, OutOfScope, ReleaseDecisionForbidden }
    public enum ReleaseRiskDiagnosticCode { RiskSourceMissing, ReleaseDecisionClaimed, OwnerMissing, ServerDependencyHidden, QaDependencyHidden }

    public sealed class ReleaseRiskItem
    {
        public ReleaseRiskItem(string riskId, ReleaseRiskMilestone milestone, ReleaseRiskOwner? owner, ReleaseRiskStatus status, string source, string limit, bool releaseDecisionClaimed = false, bool serverDependencyHidden = false, bool qaDependencyHidden = false)
        {
            RiskId = ColonyIntegrationIds.Require(riskId);
            Milestone = milestone;
            Owner = owner;
            Status = status;
            Source = source ?? string.Empty;
            Limit = limit ?? string.Empty;
            ReleaseDecisionClaimed = releaseDecisionClaimed;
            ServerDependencyHidden = serverDependencyHidden;
            QaDependencyHidden = qaDependencyHidden;
        }

        public string RiskId { get; }
        public ReleaseRiskMilestone Milestone { get; }
        public ReleaseRiskOwner? Owner { get; }
        public ReleaseRiskStatus Status { get; }
        public string Source { get; }
        public string Limit { get; }
        public bool ReleaseDecisionClaimed { get; }
        public bool ServerDependencyHidden { get; }
        public bool QaDependencyHidden { get; }
    }

    public sealed class ColonyIntegrationReleaseRiskProjection
    {
        public ColonyIntegrationReleaseRiskProjection(IReadOnlyList<ReleaseRiskItem> risks) { Risks = risks ?? Array.Empty<ReleaseRiskItem>(); }
        public IReadOnlyList<ReleaseRiskItem> Risks { get; }
        public ColonyIntegrationReleaseRiskDiagnostics Evaluate()
        {
            var findings = new List<ReleaseRiskDiagnosticCode>();
            if (Risks.Any(r => string.IsNullOrWhiteSpace(r.Source))) findings.Add(ReleaseRiskDiagnosticCode.RiskSourceMissing);
            if (Risks.Any(r => r.ReleaseDecisionClaimed || r.Status == ReleaseRiskStatus.ReleaseDecisionForbidden)) findings.Add(ReleaseRiskDiagnosticCode.ReleaseDecisionClaimed);
            if (Risks.Any(r => r.Owner == null)) findings.Add(ReleaseRiskDiagnosticCode.OwnerMissing);
            if (Risks.Any(r => r.ServerDependencyHidden)) findings.Add(ReleaseRiskDiagnosticCode.ServerDependencyHidden);
            if (Risks.Any(r => r.QaDependencyHidden)) findings.Add(ReleaseRiskDiagnosticCode.QaDependencyHidden);
            return new ColonyIntegrationReleaseRiskDiagnostics(findings);
        }
    }

    public sealed class ColonyIntegrationReleaseRiskDiagnostics
    {
        public ColonyIntegrationReleaseRiskDiagnostics(IReadOnlyList<ReleaseRiskDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ReleaseRiskDiagnosticCode>(); }
        public IReadOnlyList<ReleaseRiskDiagnosticCode> Findings { get; }
        public bool Contains(ReleaseRiskDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum RuntimeReadinessVerdict { RuntimeReadinessObservable, ReadyWithWarnings, NeedsRevision, BlockedByDemoValidation, BlockedByRuntimeBoundary, BlockedByBee281Premature }
    public enum RuntimeReadinessRisk { RuntimeBoundaryViolation, DemoValidationMissing, FixtureGap, ReleaseDecision, DocumentationDrift, Bee281Premature }
    public enum RuntimeReadinessDiagnosticCode { Bee281Premature, RuntimeBoundaryViolation, DemoValidationMissing, FixtureGapOpen, ReleaseDecisionClaimed, DocumentationDriftOpen }

    public sealed class RuntimeReadinessCriterion
    {
        public RuntimeReadinessCriterion(string beeId, bool passed, bool runtimeBoundaryViolation = false, bool demoValidationMissing = false, bool fixtureGapOpen = false, bool releaseDecisionClaimed = false, bool documentationDriftOpen = false)
        {
            BeeId = ColonyIntegrationIds.Require(beeId);
            Passed = passed;
            RuntimeBoundaryViolation = runtimeBoundaryViolation;
            DemoValidationMissing = demoValidationMissing;
            FixtureGapOpen = fixtureGapOpen;
            ReleaseDecisionClaimed = releaseDecisionClaimed;
            DocumentationDriftOpen = documentationDriftOpen;
        }

        public string BeeId { get; }
        public bool Passed { get; }
        public bool RuntimeBoundaryViolation { get; }
        public bool DemoValidationMissing { get; }
        public bool FixtureGapOpen { get; }
        public bool ReleaseDecisionClaimed { get; }
        public bool DocumentationDriftOpen { get; }
    }

    public sealed class ColonyIntegrationRuntimeReadinessGate
    {
        public ColonyIntegrationRuntimeReadinessGate(IReadOnlyList<RuntimeReadinessCriterion> criteria, bool bee281Referenced = false)
        {
            Criteria = criteria ?? Array.Empty<RuntimeReadinessCriterion>();
            Bee281Referenced = bee281Referenced;
        }

        public IReadOnlyList<RuntimeReadinessCriterion> Criteria { get; }
        public bool Bee281Referenced { get; }
        public RuntimeReadinessDiagnostics Evaluate()
        {
            var findings = new List<RuntimeReadinessDiagnosticCode>();
            if (Bee281Referenced) findings.Add(RuntimeReadinessDiagnosticCode.Bee281Premature);
            if (Criteria.Any(c => c.RuntimeBoundaryViolation)) findings.Add(RuntimeReadinessDiagnosticCode.RuntimeBoundaryViolation);
            if (Criteria.Any(c => c.DemoValidationMissing)) findings.Add(RuntimeReadinessDiagnosticCode.DemoValidationMissing);
            if (Criteria.Any(c => c.FixtureGapOpen)) findings.Add(RuntimeReadinessDiagnosticCode.FixtureGapOpen);
            if (Criteria.Any(c => c.ReleaseDecisionClaimed)) findings.Add(RuntimeReadinessDiagnosticCode.ReleaseDecisionClaimed);
            if (Criteria.Any(c => c.DocumentationDriftOpen)) findings.Add(RuntimeReadinessDiagnosticCode.DocumentationDriftOpen);

            RuntimeReadinessVerdict verdict = findings.Contains(RuntimeReadinessDiagnosticCode.Bee281Premature)
                ? RuntimeReadinessVerdict.BlockedByBee281Premature
                : findings.Contains(RuntimeReadinessDiagnosticCode.RuntimeBoundaryViolation)
                    ? RuntimeReadinessVerdict.BlockedByRuntimeBoundary
                    : findings.Contains(RuntimeReadinessDiagnosticCode.DemoValidationMissing)
                        ? RuntimeReadinessVerdict.BlockedByDemoValidation
                        : Criteria.Any(c => !c.Passed) || findings.Count > 0
                            ? RuntimeReadinessVerdict.NeedsRevision
                            : RuntimeReadinessVerdict.RuntimeReadinessObservable;
            return new RuntimeReadinessDiagnostics(verdict, findings);
        }
    }

    public sealed class RuntimeReadinessDiagnostics
    {
        public RuntimeReadinessDiagnostics(RuntimeReadinessVerdict verdict, IReadOnlyList<RuntimeReadinessDiagnosticCode> findings)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<RuntimeReadinessDiagnosticCode>();
        }

        public RuntimeReadinessVerdict Verdict { get; }
        public IReadOnlyList<RuntimeReadinessDiagnosticCode> Findings { get; }
        public bool Contains(RuntimeReadinessDiagnosticCode code) { return Findings.Contains(code); }
    }
}
