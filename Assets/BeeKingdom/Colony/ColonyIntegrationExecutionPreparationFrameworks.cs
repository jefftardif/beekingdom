using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum WorkerExecutionInputStatus { ReadyForWorker, NeedsEvidence, NeedsArchitecture, NeedsQa, Blocked, OutOfScope }
    public enum WorkerExecutionDiagnosticCode { WorkerInputMissing, BoundaryMissing, EvidenceMissing, OwnerMissing, ForbiddenExecutionRequested }

    public sealed class WorkerExecutionPrerequisite
    {
        public WorkerExecutionPrerequisite(string prerequisiteId, string sourceBee, string owner, string requiredEvidence, string demoSurface, string runtimeBoundary, string nextAction, WorkerExecutionInputStatus status)
        {
            PrerequisiteId = ColonyIntegrationIds.Require(prerequisiteId);
            SourceBee = sourceBee ?? string.Empty;
            Owner = owner ?? string.Empty;
            RequiredEvidence = requiredEvidence ?? string.Empty;
            DemoSurface = demoSurface ?? string.Empty;
            RuntimeBoundary = runtimeBoundary ?? string.Empty;
            NextAction = nextAction ?? string.Empty;
            Status = status;
        }

        public string PrerequisiteId { get; }
        public string SourceBee { get; }
        public string Owner { get; }
        public string RequiredEvidence { get; }
        public string DemoSurface { get; }
        public string RuntimeBoundary { get; }
        public string NextAction { get; }
        public WorkerExecutionInputStatus Status { get; }
    }

    public sealed class WorkerExecutionInput
    {
        public WorkerExecutionInput(string inputId, string sourceBee, string owner, string requiredEvidence, string demoSurface, string runtimeBoundary, string nextAction, WorkerExecutionInputStatus status, bool forbiddenExecutionRequested = false, IReadOnlyList<WorkerExecutionPrerequisite> prerequisites = null)
        {
            InputId = ColonyIntegrationIds.Require(inputId);
            SourceBee = sourceBee ?? string.Empty;
            Owner = owner ?? string.Empty;
            RequiredEvidence = requiredEvidence ?? string.Empty;
            DemoSurface = demoSurface ?? string.Empty;
            RuntimeBoundary = runtimeBoundary ?? string.Empty;
            NextAction = nextAction ?? string.Empty;
            Status = status;
            ForbiddenExecutionRequested = forbiddenExecutionRequested;
            Prerequisites = prerequisites ?? Array.Empty<WorkerExecutionPrerequisite>();
        }

        public string InputId { get; }
        public string SourceBee { get; }
        public string Owner { get; }
        public string RequiredEvidence { get; }
        public string DemoSurface { get; }
        public string RuntimeBoundary { get; }
        public string NextAction { get; }
        public WorkerExecutionInputStatus Status { get; }
        public bool ForbiddenExecutionRequested { get; }
        public IReadOnlyList<WorkerExecutionPrerequisite> Prerequisites { get; }
    }

    public sealed class WorkerExecutionBlocker
    {
        public WorkerExecutionBlocker(string blockerId, string sourceBee, string owner, string reason, string nextAction)
        {
            BlockerId = ColonyIntegrationIds.Require(blockerId);
            SourceBee = sourceBee ?? string.Empty;
            Owner = owner ?? string.Empty;
            Reason = reason ?? string.Empty;
            NextAction = nextAction ?? string.Empty;
        }

        public string BlockerId { get; }
        public string SourceBee { get; }
        public string Owner { get; }
        public string Reason { get; }
        public string NextAction { get; }
    }

    public sealed class ColonyWorkerExecutionIntake
    {
        public ColonyWorkerExecutionIntake(IReadOnlyList<WorkerExecutionInput> inputs, IReadOnlyList<WorkerExecutionBlocker> blockers = null)
        {
            Inputs = (inputs ?? Array.Empty<WorkerExecutionInput>()).OrderBy(i => i.SourceBee, StringComparer.Ordinal).ThenBy(i => i.InputId, StringComparer.Ordinal).ToArray();
            Blockers = blockers ?? Array.Empty<WorkerExecutionBlocker>();
        }

        public IReadOnlyList<WorkerExecutionInput> Inputs { get; }
        public IReadOnlyList<WorkerExecutionBlocker> Blockers { get; }

        public WorkerExecutionDiagnostics Evaluate()
        {
            var findings = new List<WorkerExecutionDiagnosticCode>();
            if (Inputs.Count == 0 || Inputs.Any(i => string.IsNullOrWhiteSpace(i.SourceBee))) findings.Add(WorkerExecutionDiagnosticCode.WorkerInputMissing);
            if (Inputs.Any(i => string.IsNullOrWhiteSpace(i.RuntimeBoundary))) findings.Add(WorkerExecutionDiagnosticCode.BoundaryMissing);
            if (Inputs.Any(i => string.IsNullOrWhiteSpace(i.RequiredEvidence))) findings.Add(WorkerExecutionDiagnosticCode.EvidenceMissing);
            if (Inputs.Any(i => string.IsNullOrWhiteSpace(i.Owner)) || Blockers.Any(b => string.IsNullOrWhiteSpace(b.Owner))) findings.Add(WorkerExecutionDiagnosticCode.OwnerMissing);
            if (Inputs.Any(i => i.ForbiddenExecutionRequested)) findings.Add(WorkerExecutionDiagnosticCode.ForbiddenExecutionRequested);
            return new WorkerExecutionDiagnostics(BuildStatus(findings), findings);
        }

        private static WorkerExecutionInputStatus BuildStatus(IReadOnlyList<WorkerExecutionDiagnosticCode> findings)
        {
            if (findings.Contains(WorkerExecutionDiagnosticCode.ForbiddenExecutionRequested)) return WorkerExecutionInputStatus.Blocked;
            if (findings.Contains(WorkerExecutionDiagnosticCode.EvidenceMissing)) return WorkerExecutionInputStatus.NeedsEvidence;
            if (findings.Contains(WorkerExecutionDiagnosticCode.BoundaryMissing)) return WorkerExecutionInputStatus.NeedsArchitecture;
            if (findings.Contains(WorkerExecutionDiagnosticCode.OwnerMissing)) return WorkerExecutionInputStatus.Blocked;
            return findings.Count == 0 ? WorkerExecutionInputStatus.ReadyForWorker : WorkerExecutionInputStatus.Blocked;
        }
    }

    public sealed class WorkerExecutionDiagnostics
    {
        public WorkerExecutionDiagnostics(WorkerExecutionInputStatus status, IReadOnlyList<WorkerExecutionDiagnosticCode> findings) { Status = status; Findings = findings ?? Array.Empty<WorkerExecutionDiagnosticCode>(); }
        public WorkerExecutionInputStatus Status { get; }
        public IReadOnlyList<WorkerExecutionDiagnosticCode> Findings { get; }
        public bool Contains(WorkerExecutionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyImplementationSliceKind { BoundaryAdapters, ReadModelBindings, DemoWiring, QaObservationHooks, RegressionPack, RiskBurnDown }
    public enum ColonyImplementationSliceStatus { Ready, Blocked, NeedsEvidence, NeedsAdapter, OutOfScope }
    public enum SliceDemoSurface { DEMO002, DEMO003, DEMO004, DEMO005, DEMO007, DEMO009, DEMO011, DEMO012 }
    public enum SliceMapDiagnosticCode { SliceSourceMissing, SliceDependencyCycle, SliceOwnerMissing, ManagerReplacementRequested, RuntimeParallelRequested }

    public sealed class SliceDependency
    {
        public SliceDependency(ColonyImplementationSliceKind from, ColonyImplementationSliceKind to)
        {
            From = from;
            To = to;
        }

        public ColonyImplementationSliceKind From { get; }
        public ColonyImplementationSliceKind To { get; }
    }

    public sealed class ColonyImplementationSlice
    {
        public ColonyImplementationSlice(ColonyImplementationSliceKind kind, string sourceBee, string owner, ColonyImplementationSliceStatus status, IReadOnlyList<SliceDemoSurface> demoSurfaces, bool managerReplacementRequested = false, bool runtimeParallelRequested = false)
        {
            Kind = kind;
            SourceBee = sourceBee ?? string.Empty;
            Owner = owner ?? string.Empty;
            Status = status;
            DemoSurfaces = demoSurfaces ?? Array.Empty<SliceDemoSurface>();
            ManagerReplacementRequested = managerReplacementRequested;
            RuntimeParallelRequested = runtimeParallelRequested;
        }

        public ColonyImplementationSliceKind Kind { get; }
        public string SourceBee { get; }
        public string Owner { get; }
        public ColonyImplementationSliceStatus Status { get; }
        public IReadOnlyList<SliceDemoSurface> DemoSurfaces { get; }
        public bool ManagerReplacementRequested { get; }
        public bool RuntimeParallelRequested { get; }
    }

    public sealed class ColonyImplementationSliceMap
    {
        public ColonyImplementationSliceMap(IReadOnlyList<ColonyImplementationSlice> slices, IReadOnlyList<SliceDependency> dependencies)
        {
            Slices = (slices ?? Array.Empty<ColonyImplementationSlice>()).OrderBy(s => s.Kind).ToArray();
            Dependencies = dependencies ?? Array.Empty<SliceDependency>();
        }

        public IReadOnlyList<ColonyImplementationSlice> Slices { get; }
        public IReadOnlyList<SliceDependency> Dependencies { get; }

        public SliceMapDiagnostics Evaluate()
        {
            var findings = new List<SliceMapDiagnosticCode>();
            if (Slices.Any(s => string.IsNullOrWhiteSpace(s.SourceBee))) findings.Add(SliceMapDiagnosticCode.SliceSourceMissing);
            if (Slices.Any(s => string.IsNullOrWhiteSpace(s.Owner))) findings.Add(SliceMapDiagnosticCode.SliceOwnerMissing);
            if (Slices.Any(s => s.ManagerReplacementRequested)) findings.Add(SliceMapDiagnosticCode.ManagerReplacementRequested);
            if (Slices.Any(s => s.RuntimeParallelRequested)) findings.Add(SliceMapDiagnosticCode.RuntimeParallelRequested);
            if (HasCycle()) findings.Add(SliceMapDiagnosticCode.SliceDependencyCycle);
            return new SliceMapDiagnostics(findings);
        }

        private bool HasCycle()
        {
            var graph = Dependencies.GroupBy(d => d.From).ToDictionary(g => g.Key, g => g.Select(d => d.To).ToList());
            var visiting = new HashSet<ColonyImplementationSliceKind>();
            var visited = new HashSet<ColonyImplementationSliceKind>();
            foreach (ColonyImplementationSliceKind node in graph.Keys)
            {
                if (Visit(node, graph, visiting, visited)) return true;
            }

            return false;
        }

        private static bool Visit(ColonyImplementationSliceKind node, Dictionary<ColonyImplementationSliceKind, List<ColonyImplementationSliceKind>> graph, HashSet<ColonyImplementationSliceKind> visiting, HashSet<ColonyImplementationSliceKind> visited)
        {
            if (visited.Contains(node)) return false;
            if (!visiting.Add(node)) return true;
            if (graph.TryGetValue(node, out List<ColonyImplementationSliceKind> next))
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

    public class SliceMapDiagnostics
    {
        public SliceMapDiagnostics(IReadOnlyList<SliceMapDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SliceMapDiagnosticCode>(); }
        public IReadOnlyList<SliceMapDiagnosticCode> Findings { get; }
        public bool Contains(SliceMapDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class ImplementationSliceDiagnostics : SliceMapDiagnostics
    {
        public ImplementationSliceDiagnostics(IReadOnlyList<SliceMapDiagnosticCode> findings) : base(findings) { }
    }

    public enum ColonyRuntimeAdapterPort { ReadModelInput, EventObservation, CommandIntentFuture, SnapshotReference, DemoProjection }
    public enum AdapterSourceOfTruth { Population, BeeAI, Construction, Resources, Defense, Strategy, World, Demo, ServerFuture, Ambiguous }
    public enum ColonyRuntimeAdapterStatus { Expected, Available, Missing, Blocked, Forbidden }
    public enum AdapterDiagnosticCode { AdapterSourceMissing, ManagerReplacementDetected, MutableAdapterRequested, SourceOfTruthAmbiguous, EngineBypassRequested }

    public sealed class AdapterGap
    {
        public AdapterGap(string gapId, string description, string nextAction)
        {
            GapId = gapId ?? string.Empty;
            Description = description ?? string.Empty;
            NextAction = nextAction ?? string.Empty;
        }

        public string GapId { get; }
        public string Description { get; }
        public string NextAction { get; }
    }

    public sealed class ColonyRuntimeAdapterPortContract
    {
        public ColonyRuntimeAdapterPortContract(string adapterId, string domainPair, AdapterSourceOfTruth? sourceOfTruth, ColonyRuntimeAdapterPort port, string input, string output, string limit, ColonyRuntimeAdapterStatus status, bool managerReplacementRequested = false, bool mutableAdapterRequested = false, bool engineBypassRequested = false, IReadOnlyList<AdapterGap> gaps = null)
        {
            AdapterId = ColonyIntegrationIds.Require(adapterId);
            DomainPair = domainPair ?? string.Empty;
            SourceOfTruth = sourceOfTruth;
            Port = port;
            Input = input ?? string.Empty;
            Output = output ?? string.Empty;
            Limit = limit ?? string.Empty;
            Status = status;
            ManagerReplacementRequested = managerReplacementRequested;
            MutableAdapterRequested = mutableAdapterRequested;
            EngineBypassRequested = engineBypassRequested;
            Gaps = gaps ?? Array.Empty<AdapterGap>();
        }

        public string AdapterId { get; }
        public string DomainPair { get; }
        public AdapterSourceOfTruth? SourceOfTruth { get; }
        public ColonyRuntimeAdapterPort Port { get; }
        public string Input { get; }
        public string Output { get; }
        public string Limit { get; }
        public ColonyRuntimeAdapterStatus Status { get; }
        public bool ManagerReplacementRequested { get; }
        public bool MutableAdapterRequested { get; }
        public bool EngineBypassRequested { get; }
        public IReadOnlyList<AdapterGap> Gaps { get; }
    }

    public sealed class ColonyRuntimeAdapterContract
    {
        public ColonyRuntimeAdapterContract(IReadOnlyList<ColonyRuntimeAdapterPortContract> adapters) { Adapters = adapters ?? Array.Empty<ColonyRuntimeAdapterPortContract>(); }
        public IReadOnlyList<ColonyRuntimeAdapterPortContract> Adapters { get; }
        public AdapterDiagnostics Evaluate()
        {
            var findings = new List<AdapterDiagnosticCode>();
            if (Adapters.Count == 0 || Adapters.Any(a => a.SourceOfTruth == null)) findings.Add(AdapterDiagnosticCode.AdapterSourceMissing);
            if (Adapters.Any(a => a.ManagerReplacementRequested)) findings.Add(AdapterDiagnosticCode.ManagerReplacementDetected);
            if (Adapters.Any(a => a.MutableAdapterRequested)) findings.Add(AdapterDiagnosticCode.MutableAdapterRequested);
            if (Adapters.Any(a => a.SourceOfTruth == AdapterSourceOfTruth.Ambiguous)) findings.Add(AdapterDiagnosticCode.SourceOfTruthAmbiguous);
            if (Adapters.Any(a => a.EngineBypassRequested)) findings.Add(AdapterDiagnosticCode.EngineBypassRequested);
            return new AdapterDiagnostics(findings);
        }
    }

    public sealed class AdapterDiagnostics
    {
        public AdapterDiagnostics(IReadOnlyList<AdapterDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AdapterDiagnosticCode>(); }
        public IReadOnlyList<AdapterDiagnosticCode> Findings { get; }
        public bool Contains(AdapterDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ReadModelBindingSource { Population, AIIntent, ConstructionFootprint, Resources, Defense, Strategy, Emergency, DemoGate, Missing, Ambiguous }
    public enum ReadModelBindingStatus { Bound, MissingSource, AmbiguousSource, OutOfScope, ForbiddenMutation }
    public enum ReadModelBindingDiagnosticCode { BindingSourceMissing, BindingAmbiguous, SourceMutationRequested, TransformUnseeded, ReadModelFieldUnowned }

    public sealed class ReadModelBindingField
    {
        public ReadModelBindingField(string readModel, string field, ReadModelBindingSource source, string owner, string transform, bool transformSeeded = true, bool sourceMutationRequested = false)
        {
            ReadModel = readModel ?? string.Empty;
            Field = field ?? string.Empty;
            Source = source;
            Owner = owner ?? string.Empty;
            Transform = transform ?? string.Empty;
            TransformSeeded = transformSeeded;
            SourceMutationRequested = sourceMutationRequested;
        }

        public string ReadModel { get; }
        public string Field { get; }
        public ReadModelBindingSource Source { get; }
        public string Owner { get; }
        public string Transform { get; }
        public bool TransformSeeded { get; }
        public bool SourceMutationRequested { get; }
    }

    public sealed class ColonyReadModelBinding
    {
        public ColonyReadModelBinding(IReadOnlyList<ReadModelBindingField> fields, ReadModelBindingStatus status = ReadModelBindingStatus.Bound)
        {
            Fields = fields ?? Array.Empty<ReadModelBindingField>();
            Status = status;
        }

        public IReadOnlyList<ReadModelBindingField> Fields { get; }
        public ReadModelBindingStatus Status { get; }
        public ReadModelBindingDiagnostics Evaluate()
        {
            var findings = new List<ReadModelBindingDiagnosticCode>();
            if (Fields.Count == 0 || Fields.Any(f => f.Source == ReadModelBindingSource.Missing)) findings.Add(ReadModelBindingDiagnosticCode.BindingSourceMissing);
            if (Fields.Any(f => f.Source == ReadModelBindingSource.Ambiguous)) findings.Add(ReadModelBindingDiagnosticCode.BindingAmbiguous);
            if (Fields.Any(f => f.SourceMutationRequested) || Status == ReadModelBindingStatus.ForbiddenMutation) findings.Add(ReadModelBindingDiagnosticCode.SourceMutationRequested);
            if (Fields.Any(f => !f.TransformSeeded)) findings.Add(ReadModelBindingDiagnosticCode.TransformUnseeded);
            if (Fields.Any(f => string.IsNullOrWhiteSpace(f.Owner) || string.IsNullOrWhiteSpace(f.Field))) findings.Add(ReadModelBindingDiagnosticCode.ReadModelFieldUnowned);
            return new ReadModelBindingDiagnostics(findings);
        }
    }

    public sealed class ReadModelBindingDiagnostics
    {
        public ReadModelBindingDiagnostics(IReadOnlyList<ReadModelBindingDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ReadModelBindingDiagnosticCode>(); }
        public IReadOnlyList<ReadModelBindingDiagnosticCode> Findings { get; }
        public bool Contains(ReadModelBindingDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum DemoWiringStep { BindReadModel, ExposeFixtureStatus, ShowTrace, ShowDiffs, ShowFailures, ShowGate }
    public enum DemoWiringSurface { DEMO002, DEMO003, DEMO004, DEMO005, DEMO007, DEMO009, DEMO011, DEMO012 }
    public enum DemoWiringStatus { Ready, MissingSurface, MissingSource, Blocked, OutOfScope }
    public enum DemoWiringDiagnosticCode { DemoSurfaceMissing, WiringSourceMissing, SceneCreationRequested, DemoSpecSeparateRequested, VisualSuccessMissing }

    public sealed class DemoWiringStepEntry
    {
        public DemoWiringStepEntry(DemoWiringStep step, DemoWiringSurface? surface, string source, string visualSuccess, string limit, DemoWiringStatus status, bool sceneCreationRequested = false, bool demoSpecSeparateRequested = false)
        {
            Step = step;
            Surface = surface;
            Source = source ?? string.Empty;
            VisualSuccess = visualSuccess ?? string.Empty;
            Limit = limit ?? string.Empty;
            Status = status;
            SceneCreationRequested = sceneCreationRequested;
            DemoSpecSeparateRequested = demoSpecSeparateRequested;
        }

        public DemoWiringStep Step { get; }
        public DemoWiringSurface? Surface { get; }
        public string Source { get; }
        public string VisualSuccess { get; }
        public string Limit { get; }
        public DemoWiringStatus Status { get; }
        public bool SceneCreationRequested { get; }
        public bool DemoSpecSeparateRequested { get; }
    }

    public sealed class ColonyDemoWiringPlan
    {
        public ColonyDemoWiringPlan(IReadOnlyList<DemoWiringStepEntry> steps) { Steps = steps ?? Array.Empty<DemoWiringStepEntry>(); }
        public IReadOnlyList<DemoWiringStepEntry> Steps { get; }
        public DemoWiringDiagnostics Evaluate()
        {
            var findings = new List<DemoWiringDiagnosticCode>();
            if (Steps.Count == 0 || Steps.Any(s => s.Surface == null)) findings.Add(DemoWiringDiagnosticCode.DemoSurfaceMissing);
            if (Steps.Any(s => string.IsNullOrWhiteSpace(s.Source))) findings.Add(DemoWiringDiagnosticCode.WiringSourceMissing);
            if (Steps.Any(s => s.SceneCreationRequested)) findings.Add(DemoWiringDiagnosticCode.SceneCreationRequested);
            if (Steps.Any(s => s.DemoSpecSeparateRequested)) findings.Add(DemoWiringDiagnosticCode.DemoSpecSeparateRequested);
            if (Steps.Any(s => string.IsNullOrWhiteSpace(s.VisualSuccess))) findings.Add(DemoWiringDiagnosticCode.VisualSuccessMissing);
            return new DemoWiringDiagnostics(findings);
        }
    }

    public sealed class DemoWiringDiagnostics
    {
        public DemoWiringDiagnostics(IReadOnlyList<DemoWiringDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DemoWiringDiagnosticCode>(); }
        public IReadOnlyList<DemoWiringDiagnosticCode> Findings { get; }
        public bool Contains(DemoWiringDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyQaObservationSourceType { WorkerSlice, ReadModelBinding, RuntimeAdapter, DemoPanel, ServerEscalation, RegressionScenario }
    public enum ColonyQaObservationStatus { Observed, MissingEvidence, Blocked, OutOfScope, InvalidClaim }
    public enum ColonyQaObservationDiagnosticCode { QaHookSourceMissing, ObservationPayloadMissing, DemoVerdictClaimed, FinalQaTestClaimed, UnsafeQaObservationExport, ObservationOwnerMissing }

    public sealed class ColonyQaObservationSource
    {
        public ColonyQaObservationSource(string beeId, string sliceId, ColonyQaObservationSourceType sourceType, string owner, string expectedSignal)
        {
            BeeId = beeId ?? string.Empty;
            SliceId = sliceId ?? string.Empty;
            SourceType = sourceType;
            Owner = owner ?? string.Empty;
            ExpectedSignal = expectedSignal ?? string.Empty;
        }

        public string BeeId { get; }
        public string SliceId { get; }
        public ColonyQaObservationSourceType SourceType { get; }
        public string Owner { get; }
        public string ExpectedSignal { get; }
    }

    public sealed class ColonyQaObservationPayload
    {
        public ColonyQaObservationPayload(string hookId, ColonyQaObservationStatus status, string observedValue, string expectedValue, string evidenceReference, string missingEvidenceReason, string nextOwner)
        {
            HookId = hookId ?? string.Empty;
            Status = status;
            ObservedValue = observedValue ?? string.Empty;
            ExpectedValue = expectedValue ?? string.Empty;
            EvidenceReference = evidenceReference ?? string.Empty;
            MissingEvidenceReason = missingEvidenceReason ?? string.Empty;
            NextOwner = nextOwner ?? string.Empty;
        }

        public string HookId { get; }
        public ColonyQaObservationStatus Status { get; }
        public string ObservedValue { get; }
        public string ExpectedValue { get; }
        public string EvidenceReference { get; }
        public string MissingEvidenceReason { get; }
        public string NextOwner { get; }
    }

    public sealed class ColonyQaObservationExport
    {
        public ColonyQaObservationExport(string hookId, string label, ColonyQaObservationStatus status, string qaMeaning, string demoMeaning, string limitations, bool unsafeExport = false)
        {
            HookId = hookId ?? string.Empty;
            Label = label ?? string.Empty;
            Status = status;
            QaMeaning = qaMeaning ?? string.Empty;
            DemoMeaning = demoMeaning ?? string.Empty;
            Limitations = limitations ?? string.Empty;
            UnsafeExport = unsafeExport;
        }

        public string HookId { get; }
        public string Label { get; }
        public ColonyQaObservationStatus Status { get; }
        public string QaMeaning { get; }
        public string DemoMeaning { get; }
        public string Limitations { get; }
        public bool UnsafeExport { get; }
    }

    public class ColonyIntegrationQaObservationHook
    {
        public ColonyIntegrationQaObservationHook(string hookId, string title, string beeId, ColonyQaObservationSource source, ColonyQaObservationPayload payload, ColonyQaObservationStatus status, DateTime createdAt, DateTime updatedAt, ColonyQaObservationExport export = null, bool demoVerdictClaimed = false, bool finalQaTestClaimed = false)
        {
            HookId = ColonyIntegrationIds.Require(hookId);
            Title = title ?? string.Empty;
            BeeId = beeId ?? string.Empty;
            Source = source;
            Payload = payload;
            Status = status;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Export = export;
            DemoVerdictClaimed = demoVerdictClaimed;
            FinalQaTestClaimed = finalQaTestClaimed;
        }

        public string HookId { get; }
        public string Title { get; }
        public string BeeId { get; }
        public ColonyQaObservationSource Source { get; }
        public ColonyQaObservationPayload Payload { get; }
        public ColonyQaObservationStatus Status { get; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }
        public ColonyQaObservationExport Export { get; }
        public bool DemoVerdictClaimed { get; }
        public bool FinalQaTestClaimed { get; }
    }

    public sealed class ColonyQaObservationHook : ColonyIntegrationQaObservationHook
    {
        public ColonyQaObservationHook(string hookId, string title, string beeId, ColonyQaObservationSource source, ColonyQaObservationPayload payload, ColonyQaObservationStatus status, DateTime createdAt, DateTime updatedAt, ColonyQaObservationExport export = null, bool demoVerdictClaimed = false, bool finalQaTestClaimed = false)
            : base(hookId, title, beeId, source, payload, status, createdAt, updatedAt, export, demoVerdictClaimed, finalQaTestClaimed) { }
    }

    public readonly struct ColonyQaObservationUpdated
    {
        public ColonyQaObservationUpdated(string beeId, string hookId, ColonyQaObservationStatus status, ColonyQaObservationSourceType sourceType, string owner, DateTime timestamp)
        {
            BeeId = beeId ?? string.Empty;
            HookId = hookId ?? string.Empty;
            Status = status;
            SourceType = sourceType;
            Owner = owner ?? string.Empty;
            Timestamp = timestamp;
        }

        public string BeeId { get; }
        public string HookId { get; }
        public ColonyQaObservationStatus Status { get; }
        public ColonyQaObservationSourceType SourceType { get; }
        public string Owner { get; }
        public DateTime Timestamp { get; }
    }

    public readonly struct ColonyQaObservationBlocked
    {
        public ColonyQaObservationBlocked(string beeId, string hookId, ColonyQaObservationStatus status, ColonyQaObservationSourceType sourceType, string owner, DateTime timestamp)
        {
            BeeId = beeId ?? string.Empty;
            HookId = hookId ?? string.Empty;
            Status = status;
            SourceType = sourceType;
            Owner = owner ?? string.Empty;
            Timestamp = timestamp;
        }

        public string BeeId { get; }
        public string HookId { get; }
        public ColonyQaObservationStatus Status { get; }
        public ColonyQaObservationSourceType SourceType { get; }
        public string Owner { get; }
        public DateTime Timestamp { get; }
    }

    public readonly struct ColonyQaInvalidVerdictClaimDetected
    {
        public ColonyQaInvalidVerdictClaimDetected(string beeId, string hookId, ColonyQaObservationStatus status, ColonyQaObservationSourceType sourceType, string owner, DateTime timestamp)
        {
            BeeId = beeId ?? string.Empty;
            HookId = hookId ?? string.Empty;
            Status = status;
            SourceType = sourceType;
            Owner = owner ?? string.Empty;
            Timestamp = timestamp;
        }

        public string BeeId { get; }
        public string HookId { get; }
        public ColonyQaObservationStatus Status { get; }
        public ColonyQaObservationSourceType SourceType { get; }
        public string Owner { get; }
        public DateTime Timestamp { get; }
    }

    public static class ColonyQaObservationDiagnostics
    {
        public static IReadOnlyList<ColonyQaObservationDiagnosticCode> Evaluate(IReadOnlyList<ColonyIntegrationQaObservationHook> hooks)
        {
            IReadOnlyList<ColonyIntegrationQaObservationHook> list = hooks ?? Array.Empty<ColonyIntegrationQaObservationHook>();
            var findings = new List<ColonyQaObservationDiagnosticCode>();
            if (list.Count == 0 || list.Any(h => h.Source == null || string.IsNullOrWhiteSpace(h.Source.BeeId))) findings.Add(ColonyQaObservationDiagnosticCode.QaHookSourceMissing);
            if (list.Any(h => h.Payload == null || string.IsNullOrWhiteSpace(h.Payload.HookId))) findings.Add(ColonyQaObservationDiagnosticCode.ObservationPayloadMissing);
            if (list.Any(h => h.DemoVerdictClaimed)) findings.Add(ColonyQaObservationDiagnosticCode.DemoVerdictClaimed);
            if (list.Any(h => h.FinalQaTestClaimed)) findings.Add(ColonyQaObservationDiagnosticCode.FinalQaTestClaimed);
            if (list.Any(h => h.Export != null && h.Export.UnsafeExport)) findings.Add(ColonyQaObservationDiagnosticCode.UnsafeQaObservationExport);
            if (list.Any(h => h.Source != null && string.IsNullOrWhiteSpace(h.Source.Owner))) findings.Add(ColonyQaObservationDiagnosticCode.ObservationOwnerMissing);
            return findings;
        }
    }

    public enum ColonyDependencyOwnership { WorkerOwned, DemoOwned, QaOwned, ArchitectOwned, ServerAnalysisRequired, ServerSpecRequiredFuture, ServerOutOfScope }
    public enum ColonyServerEscalationStatus { Pending, ResolvedLocally, EscalatedFuture, BlockedImplementation, OutOfScope }
    public enum ServerDependencyEscalationDiagnosticCode { ServerDependencyHidden, ServerServiceRequested, EndpointRequestedWithoutServerBee, OwnerMissing, EscalationConditionMissing, ServerScopeAmbiguous, ServerProgressMismatch }

    public sealed class ColonyServerEscalationCondition
    {
        public ColonyServerEscalationCondition(string reason, string observedSignal, string expectedServerCapability, string localAlternative, string triggerThreshold, string futureServerSpecHint)
        {
            Reason = reason ?? string.Empty;
            ObservedSignal = observedSignal ?? string.Empty;
            ExpectedServerCapability = expectedServerCapability ?? string.Empty;
            LocalAlternative = localAlternative ?? string.Empty;
            TriggerThreshold = triggerThreshold ?? string.Empty;
            FutureServerSpecHint = futureServerSpecHint ?? string.Empty;
        }

        public string Reason { get; }
        public string ObservedSignal { get; }
        public string ExpectedServerCapability { get; }
        public string LocalAlternative { get; }
        public string TriggerThreshold { get; }
        public string FutureServerSpecHint { get; }
    }

    public sealed class ColonyServerEscalationEvidence
    {
        public ColonyServerEscalationEvidence(string evidenceId, string sourceReference, string beeId)
        {
            EvidenceId = evidenceId ?? string.Empty;
            SourceReference = sourceReference ?? string.Empty;
            BeeId = beeId ?? string.Empty;
        }

        public string EvidenceId { get; }
        public string SourceReference { get; }
        public string BeeId { get; }
    }

    public sealed class ColonyServerDependencyEscalation
    {
        public ColonyServerDependencyEscalation(string escalationId, string beeId, string sourceReference, ColonyDependencyOwnership? owner, ColonyServerEscalationCondition condition, ColonyServerEscalationEvidence evidence, int severity, ColonyServerEscalationStatus status, bool serverDependencyHidden = false, bool serverServiceRequested = false, bool endpointRequestedWithoutServerBee = false, bool serverScopeAmbiguous = false, bool serverProgressMismatch = false)
        {
            EscalationId = ColonyIntegrationIds.Require(escalationId);
            BeeId = beeId ?? string.Empty;
            SourceReference = sourceReference ?? string.Empty;
            Owner = owner;
            Condition = condition;
            Evidence = evidence;
            Severity = Math.Max(0, severity);
            Status = status;
            ServerDependencyHidden = serverDependencyHidden;
            ServerServiceRequested = serverServiceRequested;
            EndpointRequestedWithoutServerBee = endpointRequestedWithoutServerBee;
            ServerScopeAmbiguous = serverScopeAmbiguous;
            ServerProgressMismatch = serverProgressMismatch;
        }

        public string EscalationId { get; }
        public string BeeId { get; }
        public string SourceReference { get; }
        public ColonyDependencyOwnership? Owner { get; }
        public ColonyServerEscalationCondition Condition { get; }
        public ColonyServerEscalationEvidence Evidence { get; }
        public int Severity { get; }
        public ColonyServerEscalationStatus Status { get; }
        public bool ServerDependencyHidden { get; }
        public bool ServerServiceRequested { get; }
        public bool EndpointRequestedWithoutServerBee { get; }
        public bool ServerScopeAmbiguous { get; }
        public bool ServerProgressMismatch { get; }
    }

    public sealed class ColonyServerEscalationQueue
    {
        public ColonyServerEscalationQueue(string queueId, IReadOnlyList<ColonyServerDependencyEscalation> pendingEscalations, IReadOnlyList<ColonyServerDependencyEscalation> resolvedLocally, IReadOnlyList<ColonyServerDependencyEscalation> blockedImplementationAttempts)
        {
            QueueId = ColonyIntegrationIds.Require(queueId);
            PendingEscalations = pendingEscalations ?? Array.Empty<ColonyServerDependencyEscalation>();
            ResolvedLocally = resolvedLocally ?? Array.Empty<ColonyServerDependencyEscalation>();
            BlockedImplementationAttempts = blockedImplementationAttempts ?? Array.Empty<ColonyServerDependencyEscalation>();
        }

        public string QueueId { get; }
        public IReadOnlyList<ColonyServerDependencyEscalation> PendingEscalations { get; }
        public IReadOnlyList<ColonyServerDependencyEscalation> ResolvedLocally { get; }
        public IReadOnlyList<ColonyServerDependencyEscalation> BlockedImplementationAttempts { get; }

        public ServerDependencyEscalationDiagnostics Evaluate()
        {
            IReadOnlyList<ColonyServerDependencyEscalation> all = PendingEscalations.Concat(ResolvedLocally).Concat(BlockedImplementationAttempts).ToArray();
            var findings = new List<ServerDependencyEscalationDiagnosticCode>();
            if (all.Any(e => e.ServerDependencyHidden)) findings.Add(ServerDependencyEscalationDiagnosticCode.ServerDependencyHidden);
            if (all.Any(e => e.ServerServiceRequested)) findings.Add(ServerDependencyEscalationDiagnosticCode.ServerServiceRequested);
            if (all.Any(e => e.EndpointRequestedWithoutServerBee)) findings.Add(ServerDependencyEscalationDiagnosticCode.EndpointRequestedWithoutServerBee);
            if (all.Any(e => e.Owner == null)) findings.Add(ServerDependencyEscalationDiagnosticCode.OwnerMissing);
            if (all.Any(e => e.Condition == null || string.IsNullOrWhiteSpace(e.Condition.Reason))) findings.Add(ServerDependencyEscalationDiagnosticCode.EscalationConditionMissing);
            if (all.Any(e => e.ServerScopeAmbiguous)) findings.Add(ServerDependencyEscalationDiagnosticCode.ServerScopeAmbiguous);
            if (all.Any(e => e.ServerProgressMismatch)) findings.Add(ServerDependencyEscalationDiagnosticCode.ServerProgressMismatch);
            return new ServerDependencyEscalationDiagnostics(findings);
        }
    }

    public sealed class ServerDependencyEscalationDiagnostics
    {
        public ServerDependencyEscalationDiagnostics(IReadOnlyList<ServerDependencyEscalationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ServerDependencyEscalationDiagnosticCode>(); }
        public IReadOnlyList<ServerDependencyEscalationDiagnosticCode> Findings { get; }
        public bool Contains(ServerDependencyEscalationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public readonly struct ColonyServerDependencyEscalated
    {
        public ColonyServerDependencyEscalated(string beeId, string escalationId, ColonyDependencyOwnership owner, string condition, ColonyServerEscalationStatus status, DateTime timestamp)
        {
            BeeId = beeId ?? string.Empty;
            EscalationId = escalationId ?? string.Empty;
            Owner = owner;
            Condition = condition ?? string.Empty;
            Status = status;
            Timestamp = timestamp;
        }

        public string BeeId { get; }
        public string EscalationId { get; }
        public ColonyDependencyOwnership Owner { get; }
        public string Condition { get; }
        public ColonyServerEscalationStatus Status { get; }
        public DateTime Timestamp { get; }
    }

    public enum ColonyRegressionScenarioCategory { NormalColonyProgression, MissingRuntimeAdapter, PartialReadModelAvailable, IncompleteDemoWiring, QaObservationBlocked, ServerDependencyEscalated, ExecutionOrderRisk }
    public enum ColonyRegressionScenarioStatus { Observable, Blocked, GapSignalled, OutOfScope }
    public enum RegressionScenarioPackDiagnosticCode { ScenarioSeedMissing, FixtureReferenceMissing, ExpectedObservationMissing, FinalSuiteExecutionClaimed, PerformanceClaimFinal, ScenarioBeeSourceMissing, RegressionLimitMissing }

    public sealed class ColonyRegressionFixtureReference
    {
        public ColonyRegressionFixtureReference(string fixtureId, string sourceBee)
        {
            FixtureId = fixtureId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
        }

        public string FixtureId { get; }
        public string SourceBee { get; }
    }

    public sealed class ColonyRegressionExpectedObservation
    {
        public ColonyRegressionExpectedObservation(string hookId, ColonyQaObservationStatus expectedStatus, string expectedSignal, string acceptedVariance, string nonFinalMeaning)
        {
            HookId = hookId ?? string.Empty;
            ExpectedStatus = expectedStatus;
            ExpectedSignal = expectedSignal ?? string.Empty;
            AcceptedVariance = acceptedVariance ?? string.Empty;
            NonFinalMeaning = nonFinalMeaning ?? string.Empty;
        }

        public string HookId { get; }
        public ColonyQaObservationStatus ExpectedStatus { get; }
        public string ExpectedSignal { get; }
        public string AcceptedVariance { get; }
        public string NonFinalMeaning { get; }
    }

    public sealed class ColonyRegressionExecutionLimit
    {
        public ColonyRegressionExecutionLimit(string limitId, string description)
        {
            LimitId = limitId ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string LimitId { get; }
        public string Description { get; }
    }

    public sealed class ColonyRegressionScenario
    {
        public ColonyRegressionScenario(string scenarioId, string title, ColonyRegressionScenarioCategory category, int? seed, ColonyRegressionFixtureReference fixtureReference, IReadOnlyList<string> beeSources, ColonyRegressionExpectedObservation expectedObservation, IReadOnlyList<string> blockedBy, ColonyRegressionExecutionLimit executionLimit, ColonyRegressionScenarioStatus status, bool finalSuiteExecutionClaimed = false, bool performanceClaimFinal = false)
        {
            ScenarioId = ColonyIntegrationIds.Require(scenarioId);
            Title = title ?? string.Empty;
            Category = category;
            Seed = seed;
            FixtureReference = fixtureReference;
            BeeSources = beeSources ?? Array.Empty<string>();
            ExpectedObservation = expectedObservation;
            BlockedBy = blockedBy ?? Array.Empty<string>();
            ExecutionLimit = executionLimit;
            Status = status;
            FinalSuiteExecutionClaimed = finalSuiteExecutionClaimed;
            PerformanceClaimFinal = performanceClaimFinal;
        }

        public string ScenarioId { get; }
        public string Title { get; }
        public ColonyRegressionScenarioCategory Category { get; }
        public int? Seed { get; }
        public ColonyRegressionFixtureReference FixtureReference { get; }
        public IReadOnlyList<string> BeeSources { get; }
        public ColonyRegressionExpectedObservation ExpectedObservation { get; }
        public IReadOnlyList<string> BlockedBy { get; }
        public ColonyRegressionExecutionLimit ExecutionLimit { get; }
        public ColonyRegressionScenarioStatus Status { get; }
        public bool FinalSuiteExecutionClaimed { get; }
        public bool PerformanceClaimFinal { get; }
    }

    public class ColonyIntegrationRegressionScenarioPack
    {
        public ColonyIntegrationRegressionScenarioPack(string packId, string title, IReadOnlyList<ColonyRegressionScenario> scenarios, string generatedFromBeeRange, string limitations)
        {
            PackId = ColonyIntegrationIds.Require(packId);
            Title = title ?? string.Empty;
            Scenarios = scenarios ?? Array.Empty<ColonyRegressionScenario>();
            GeneratedFromBeeRange = generatedFromBeeRange ?? string.Empty;
            Limitations = limitations ?? string.Empty;
        }

        public string PackId { get; }
        public string Title { get; }
        public IReadOnlyList<ColonyRegressionScenario> Scenarios { get; }
        public string GeneratedFromBeeRange { get; }
        public string Limitations { get; }

        public RegressionScenarioPackDiagnostics Evaluate()
        {
            var findings = new List<RegressionScenarioPackDiagnosticCode>();
            if (Scenarios.Any(s => s.Seed == null)) findings.Add(RegressionScenarioPackDiagnosticCode.ScenarioSeedMissing);
            if (Scenarios.Any(s => s.FixtureReference == null || string.IsNullOrWhiteSpace(s.FixtureReference.FixtureId))) findings.Add(RegressionScenarioPackDiagnosticCode.FixtureReferenceMissing);
            if (Scenarios.Any(s => s.ExpectedObservation == null || string.IsNullOrWhiteSpace(s.ExpectedObservation.ExpectedSignal))) findings.Add(RegressionScenarioPackDiagnosticCode.ExpectedObservationMissing);
            if (Scenarios.Any(s => s.FinalSuiteExecutionClaimed)) findings.Add(RegressionScenarioPackDiagnosticCode.FinalSuiteExecutionClaimed);
            if (Scenarios.Any(s => s.PerformanceClaimFinal)) findings.Add(RegressionScenarioPackDiagnosticCode.PerformanceClaimFinal);
            if (Scenarios.Any(s => s.BeeSources.Count == 0 || s.BeeSources.Any(string.IsNullOrWhiteSpace))) findings.Add(RegressionScenarioPackDiagnosticCode.ScenarioBeeSourceMissing);
            if (string.IsNullOrWhiteSpace(Limitations) || Scenarios.Any(s => s.ExecutionLimit == null || string.IsNullOrWhiteSpace(s.ExecutionLimit.LimitId))) findings.Add(RegressionScenarioPackDiagnosticCode.RegressionLimitMissing);
            return new RegressionScenarioPackDiagnostics(findings);
        }
    }

    public sealed class ColonyRegressionScenarioPack : ColonyIntegrationRegressionScenarioPack
    {
        public ColonyRegressionScenarioPack(string packId, string title, IReadOnlyList<ColonyRegressionScenario> scenarios, string generatedFromBeeRange, string limitations)
            : base(packId, title, scenarios, generatedFromBeeRange, limitations) { }
    }

    public sealed class RegressionScenarioPackDiagnostics
    {
        public RegressionScenarioPackDiagnostics(IReadOnlyList<RegressionScenarioPackDiagnosticCode> findings) { Findings = findings ?? Array.Empty<RegressionScenarioPackDiagnosticCode>(); }
        public IReadOnlyList<RegressionScenarioPackDiagnosticCode> Findings { get; }
        public bool Contains(RegressionScenarioPackDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyImplementationRiskStatus { Open, Reduced, AcceptedByArchitect, TransferredToServer, TransferredToQa, TransferredToWorker, Blocked, InvalidAcceptance }
    public enum ColonyImplementationRiskSeverity { Low, Medium, High, Critical }
    public enum ColonyRiskDecisionType { Reduction, TransferToServer, TransferToQa, TransferToWorker, ArchitectAcceptance, BlockedAcceptance }
    public enum ImplementationRiskBurnDownDiagnosticCode { RiskSourceMissing, RiskOwnerMissing, BurnDownClaimWithoutEvidence, AcceptedRiskWithoutAuthority, ServerRiskUnescalated, QaRiskMisclassified, Bee291PrematureRiskClosure }

    public sealed class ColonyRiskEvidence
    {
        public ColonyRiskEvidence(string evidenceId, string sourceType, string reference, string observedResult, string limitation)
        {
            EvidenceId = evidenceId ?? string.Empty;
            SourceType = sourceType ?? string.Empty;
            Reference = reference ?? string.Empty;
            ObservedResult = observedResult ?? string.Empty;
            Limitation = limitation ?? string.Empty;
        }

        public string EvidenceId { get; }
        public string SourceType { get; }
        public string Reference { get; }
        public string ObservedResult { get; }
        public string Limitation { get; }
    }

    public sealed class ColonyRiskDecision
    {
        public ColonyRiskDecision(string decisionId, string riskId, ColonyRiskDecisionType decisionType, string authority, string reason)
        {
            DecisionId = decisionId ?? string.Empty;
            RiskId = riskId ?? string.Empty;
            DecisionType = decisionType;
            Authority = authority ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string DecisionId { get; }
        public string RiskId { get; }
        public ColonyRiskDecisionType DecisionType { get; }
        public string Authority { get; }
        public string Reason { get; }
    }

    public sealed class ColonyImplementationRiskItem
    {
        public ColonyImplementationRiskItem(string riskId, string title, string sourceBee, string sourceReference, ColonyImplementationRiskSeverity severity, string owner, ColonyImplementationRiskStatus status, ColonyRiskEvidence evidence, string nextAction, ColonyRiskDecision decision = null, bool serverRiskUnescalated = false, bool qaRiskMisclassified = false, bool bee291PrematureRiskClosure = false)
        {
            RiskId = ColonyIntegrationIds.Require(riskId);
            Title = title ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            SourceReference = sourceReference ?? string.Empty;
            Severity = severity;
            Owner = owner ?? string.Empty;
            Status = status;
            Evidence = evidence;
            NextAction = nextAction ?? string.Empty;
            Decision = decision;
            ServerRiskUnescalated = serverRiskUnescalated;
            QaRiskMisclassified = qaRiskMisclassified;
            Bee291PrematureRiskClosure = bee291PrematureRiskClosure;
        }

        public string RiskId { get; }
        public string Title { get; }
        public string SourceBee { get; }
        public string SourceReference { get; }
        public ColonyImplementationRiskSeverity Severity { get; }
        public string Owner { get; }
        public ColonyImplementationRiskStatus Status { get; }
        public ColonyRiskEvidence Evidence { get; }
        public string NextAction { get; }
        public ColonyRiskDecision Decision { get; }
        public bool ServerRiskUnescalated { get; }
        public bool QaRiskMisclassified { get; }
        public bool Bee291PrematureRiskClosure { get; }
    }

    public sealed class ColonyRiskBurnDownExport
    {
        public ColonyRiskBurnDownExport(string burnDownId, int openCount, int reducedCount, int transferredCount, int blockedCount)
        {
            BurnDownId = burnDownId ?? string.Empty;
            OpenCount = openCount;
            ReducedCount = reducedCount;
            TransferredCount = transferredCount;
            BlockedCount = blockedCount;
        }

        public string BurnDownId { get; }
        public int OpenCount { get; }
        public int ReducedCount { get; }
        public int TransferredCount { get; }
        public int BlockedCount { get; }
    }

    public sealed class ColonyImplementationRiskBurnDown
    {
        public ColonyImplementationRiskBurnDown(string burnDownId, string beeRange, IReadOnlyList<ColonyImplementationRiskItem> risks)
        {
            BurnDownId = ColonyIntegrationIds.Require(burnDownId);
            BeeRange = beeRange ?? string.Empty;
            Risks = risks ?? Array.Empty<ColonyImplementationRiskItem>();
        }

        public string BurnDownId { get; }
        public string BeeRange { get; }
        public IReadOnlyList<ColonyImplementationRiskItem> Risks { get; }
        public int OpenCount => Risks.Count(r => r.Status == ColonyImplementationRiskStatus.Open);
        public int ReducedCount => Risks.Count(r => r.Status == ColonyImplementationRiskStatus.Reduced);
        public int TransferredCount => Risks.Count(r => r.Status == ColonyImplementationRiskStatus.TransferredToQa || r.Status == ColonyImplementationRiskStatus.TransferredToServer || r.Status == ColonyImplementationRiskStatus.TransferredToWorker);
        public int BlockedCount => Risks.Count(r => r.Status == ColonyImplementationRiskStatus.Blocked || r.Status == ColonyImplementationRiskStatus.InvalidAcceptance);

        public ImplementationRiskBurnDownDiagnostics Evaluate()
        {
            var findings = new List<ImplementationRiskBurnDownDiagnosticCode>();
            if (Risks.Any(r => string.IsNullOrWhiteSpace(r.SourceBee) || string.IsNullOrWhiteSpace(r.SourceReference))) findings.Add(ImplementationRiskBurnDownDiagnosticCode.RiskSourceMissing);
            if (Risks.Any(r => string.IsNullOrWhiteSpace(r.Owner))) findings.Add(ImplementationRiskBurnDownDiagnosticCode.RiskOwnerMissing);
            if (Risks.Any(r => (r.Status == ColonyImplementationRiskStatus.Reduced || r.Status == ColonyImplementationRiskStatus.AcceptedByArchitect) && (r.Evidence == null || string.IsNullOrWhiteSpace(r.Evidence.EvidenceId)))) findings.Add(ImplementationRiskBurnDownDiagnosticCode.BurnDownClaimWithoutEvidence);
            if (Risks.Any(r => r.Status == ColonyImplementationRiskStatus.AcceptedByArchitect && (r.Decision == null || !string.Equals(r.Decision.Authority, "Architect", StringComparison.Ordinal)))) findings.Add(ImplementationRiskBurnDownDiagnosticCode.AcceptedRiskWithoutAuthority);
            if (Risks.Any(r => r.ServerRiskUnescalated)) findings.Add(ImplementationRiskBurnDownDiagnosticCode.ServerRiskUnescalated);
            if (Risks.Any(r => r.QaRiskMisclassified)) findings.Add(ImplementationRiskBurnDownDiagnosticCode.QaRiskMisclassified);
            if (Risks.Any(r => r.Bee291PrematureRiskClosure)) findings.Add(ImplementationRiskBurnDownDiagnosticCode.Bee291PrematureRiskClosure);
            return new ImplementationRiskBurnDownDiagnostics(findings, new ColonyRiskBurnDownExport(BurnDownId, OpenCount, ReducedCount, TransferredCount, BlockedCount));
        }
    }

    public sealed class ImplementationRiskBurnDownDiagnostics
    {
        public ImplementationRiskBurnDownDiagnostics(IReadOnlyList<ImplementationRiskBurnDownDiagnosticCode> findings, ColonyRiskBurnDownExport export)
        {
            Findings = findings ?? Array.Empty<ImplementationRiskBurnDownDiagnosticCode>();
            Export = export;
        }

        public IReadOnlyList<ImplementationRiskBurnDownDiagnosticCode> Findings { get; }
        public ColonyRiskBurnDownExport Export { get; }
        public bool Contains(ImplementationRiskBurnDownDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyExecutionReadinessVerdictType { ReadyForWorkerExecution, ReadyWithWarnings, NeedsPlannerRevision, BlockedByQaObservation, BlockedByServerDependency, BlockedByRegressionScenario, BlockedByImplementationRisk, BlockedByBee291Premature }
    public enum ColonyExecutionReadinessDiagnosticCode { ReadinessInputMissing, WorkerIntakeIncomplete, SliceMapBlocked, RuntimeAdapterGapOpen, ReadModelBindingMissing, DemoWiringMissing, QaObservationBlocked, ServerEscalationUnresolved, RegressionPackIncomplete, ImplementationRiskOpen, Bee291Premature }

    public sealed class ColonyExecutionReadinessInputSet
    {
        public ColonyExecutionReadinessInputSet(string intakeReference, string sliceMapReference, string adapterReference, string readModelReference, string demoWiringReference, string qaHookReference, string serverEscalationReference, string regressionScenarioReference, string riskBurnDownReference)
        {
            IntakeReference = intakeReference ?? string.Empty;
            SliceMapReference = sliceMapReference ?? string.Empty;
            AdapterReference = adapterReference ?? string.Empty;
            ReadModelReference = readModelReference ?? string.Empty;
            DemoWiringReference = demoWiringReference ?? string.Empty;
            QaHookReference = qaHookReference ?? string.Empty;
            ServerEscalationReference = serverEscalationReference ?? string.Empty;
            RegressionScenarioReference = regressionScenarioReference ?? string.Empty;
            RiskBurnDownReference = riskBurnDownReference ?? string.Empty;
        }

        public string IntakeReference { get; }
        public string SliceMapReference { get; }
        public string AdapterReference { get; }
        public string ReadModelReference { get; }
        public string DemoWiringReference { get; }
        public string QaHookReference { get; }
        public string ServerEscalationReference { get; }
        public string RegressionScenarioReference { get; }
        public string RiskBurnDownReference { get; }

        public bool HasMissingInput => string.IsNullOrWhiteSpace(IntakeReference)
            || string.IsNullOrWhiteSpace(SliceMapReference)
            || string.IsNullOrWhiteSpace(AdapterReference)
            || string.IsNullOrWhiteSpace(ReadModelReference)
            || string.IsNullOrWhiteSpace(DemoWiringReference)
            || string.IsNullOrWhiteSpace(QaHookReference)
            || string.IsNullOrWhiteSpace(ServerEscalationReference)
            || string.IsNullOrWhiteSpace(RegressionScenarioReference)
            || string.IsNullOrWhiteSpace(RiskBurnDownReference);
    }

    public sealed class ColonyExecutionReadinessVerdict
    {
        public ColonyExecutionReadinessVerdict(ColonyExecutionReadinessVerdictType verdictType, string evidence, string limitations, string bee291Status, string authorizedNextStep)
        {
            VerdictType = verdictType;
            Evidence = evidence ?? string.Empty;
            Limitations = limitations ?? string.Empty;
            Bee291Status = bee291Status ?? string.Empty;
            AuthorizedNextStep = authorizedNextStep ?? string.Empty;
        }

        public ColonyExecutionReadinessVerdictType VerdictType { get; }
        public string Evidence { get; }
        public string Limitations { get; }
        public string Bee291Status { get; }
        public string AuthorizedNextStep { get; }
    }

    public sealed class ColonyExecutionReadinessBlocker
    {
        public ColonyExecutionReadinessBlocker(string blockerId, string sourceBee, string reason, string owner, string requiredAction)
        {
            BlockerId = ColonyIntegrationIds.Require(blockerId);
            SourceBee = sourceBee ?? string.Empty;
            Reason = reason ?? string.Empty;
            Owner = owner ?? string.Empty;
            RequiredAction = requiredAction ?? string.Empty;
        }

        public string BlockerId { get; }
        public string SourceBee { get; }
        public string Reason { get; }
        public string Owner { get; }
        public string RequiredAction { get; }
    }

    public sealed class ColonyExecutionReadinessExport
    {
        public ColonyExecutionReadinessExport(string gateId, ColonyExecutionReadinessVerdictType verdictType, IReadOnlyList<ColonyExecutionReadinessBlocker> blockers, IReadOnlyList<string> warnings, string nextAction)
        {
            GateId = gateId ?? string.Empty;
            VerdictType = verdictType;
            Blockers = blockers ?? Array.Empty<ColonyExecutionReadinessBlocker>();
            Warnings = warnings ?? Array.Empty<string>();
            NextAction = nextAction ?? string.Empty;
        }

        public string GateId { get; }
        public ColonyExecutionReadinessVerdictType VerdictType { get; }
        public IReadOnlyList<ColonyExecutionReadinessBlocker> Blockers { get; }
        public IReadOnlyList<string> Warnings { get; }
        public string NextAction { get; }
    }

    public sealed class ColonyExecutionReadinessGate
    {
        public const string Bee291BlockedStatus = "BEE-291 bloquee jusqu'a validation architecte.";

        public ColonyExecutionReadinessGate(string gateId, string beeRange, ColonyExecutionReadinessInputSet inputSet, IReadOnlyList<ColonyExecutionReadinessBlocker> blockers, IReadOnlyList<string> warnings, string nextAction, bool workerIntakeIncomplete = false, bool sliceMapBlocked = false, bool runtimeAdapterGapOpen = false, bool readModelBindingMissing = false, bool demoWiringMissing = false, bool qaObservationBlocked = false, bool serverEscalationUnresolved = false, bool regressionPackIncomplete = false, bool implementationRiskOpen = false, bool bee291PrematureAttempt = false)
        {
            GateId = ColonyIntegrationIds.Require(gateId);
            BeeRange = beeRange ?? string.Empty;
            InputSet = inputSet;
            Blockers = blockers ?? Array.Empty<ColonyExecutionReadinessBlocker>();
            Warnings = warnings ?? Array.Empty<string>();
            NextAction = nextAction ?? string.Empty;
            WorkerIntakeIncomplete = workerIntakeIncomplete;
            SliceMapBlocked = sliceMapBlocked;
            RuntimeAdapterGapOpen = runtimeAdapterGapOpen;
            ReadModelBindingMissing = readModelBindingMissing;
            DemoWiringMissing = demoWiringMissing;
            QaObservationBlocked = qaObservationBlocked;
            ServerEscalationUnresolved = serverEscalationUnresolved;
            RegressionPackIncomplete = regressionPackIncomplete;
            ImplementationRiskOpen = implementationRiskOpen;
            Bee291PrematureAttempt = bee291PrematureAttempt;
        }

        public string GateId { get; }
        public string BeeRange { get; }
        public ColonyExecutionReadinessInputSet InputSet { get; }
        public IReadOnlyList<ColonyExecutionReadinessBlocker> Blockers { get; }
        public IReadOnlyList<string> Warnings { get; }
        public string NextAction { get; }
        public bool WorkerIntakeIncomplete { get; }
        public bool SliceMapBlocked { get; }
        public bool RuntimeAdapterGapOpen { get; }
        public bool ReadModelBindingMissing { get; }
        public bool DemoWiringMissing { get; }
        public bool QaObservationBlocked { get; }
        public bool ServerEscalationUnresolved { get; }
        public bool RegressionPackIncomplete { get; }
        public bool ImplementationRiskOpen { get; }
        public bool Bee291PrematureAttempt { get; }

        public ColonyExecutionReadinessDiagnostics Evaluate()
        {
            var findings = new List<ColonyExecutionReadinessDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput) findings.Add(ColonyExecutionReadinessDiagnosticCode.ReadinessInputMissing);
            if (WorkerIntakeIncomplete) findings.Add(ColonyExecutionReadinessDiagnosticCode.WorkerIntakeIncomplete);
            if (SliceMapBlocked) findings.Add(ColonyExecutionReadinessDiagnosticCode.SliceMapBlocked);
            if (RuntimeAdapterGapOpen) findings.Add(ColonyExecutionReadinessDiagnosticCode.RuntimeAdapterGapOpen);
            if (ReadModelBindingMissing) findings.Add(ColonyExecutionReadinessDiagnosticCode.ReadModelBindingMissing);
            if (DemoWiringMissing) findings.Add(ColonyExecutionReadinessDiagnosticCode.DemoWiringMissing);
            if (QaObservationBlocked) findings.Add(ColonyExecutionReadinessDiagnosticCode.QaObservationBlocked);
            if (ServerEscalationUnresolved) findings.Add(ColonyExecutionReadinessDiagnosticCode.ServerEscalationUnresolved);
            if (RegressionPackIncomplete) findings.Add(ColonyExecutionReadinessDiagnosticCode.RegressionPackIncomplete);
            if (ImplementationRiskOpen) findings.Add(ColonyExecutionReadinessDiagnosticCode.ImplementationRiskOpen);
            if (Bee291PrematureAttempt) findings.Add(ColonyExecutionReadinessDiagnosticCode.Bee291Premature);

            ColonyExecutionReadinessVerdictType verdictType = BuildVerdict(findings);
            var verdict = new ColonyExecutionReadinessVerdict(verdictType, GateId, "Readiness lot only; no QA final, server service or BEE-291.", Bee291BlockedStatus, NextAction);
            var export = new ColonyExecutionReadinessExport(GateId, verdictType, Blockers, Warnings, NextAction);
            return new ColonyExecutionReadinessDiagnostics(verdict, findings, export);
        }

        private static ColonyExecutionReadinessVerdictType BuildVerdict(IReadOnlyList<ColonyExecutionReadinessDiagnosticCode> findings)
        {
            if (findings.Contains(ColonyExecutionReadinessDiagnosticCode.Bee291Premature)) return ColonyExecutionReadinessVerdictType.BlockedByBee291Premature;
            if (findings.Contains(ColonyExecutionReadinessDiagnosticCode.ServerEscalationUnresolved)) return ColonyExecutionReadinessVerdictType.BlockedByServerDependency;
            if (findings.Contains(ColonyExecutionReadinessDiagnosticCode.QaObservationBlocked)) return ColonyExecutionReadinessVerdictType.BlockedByQaObservation;
            if (findings.Contains(ColonyExecutionReadinessDiagnosticCode.RegressionPackIncomplete)) return ColonyExecutionReadinessVerdictType.BlockedByRegressionScenario;
            if (findings.Contains(ColonyExecutionReadinessDiagnosticCode.ImplementationRiskOpen)) return ColonyExecutionReadinessVerdictType.BlockedByImplementationRisk;
            if (findings.Count > 0) return ColonyExecutionReadinessVerdictType.NeedsPlannerRevision;
            return ColonyExecutionReadinessVerdictType.ReadyForWorkerExecution;
        }
    }

    public sealed class ColonyExecutionReadinessDiagnostics
    {
        public ColonyExecutionReadinessDiagnostics(ColonyExecutionReadinessVerdict verdict, IReadOnlyList<ColonyExecutionReadinessDiagnosticCode> findings, ColonyExecutionReadinessExport export)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<ColonyExecutionReadinessDiagnosticCode>();
            Export = export;
        }

        public ColonyExecutionReadinessVerdict Verdict { get; }
        public IReadOnlyList<ColonyExecutionReadinessDiagnosticCode> Findings { get; }
        public ColonyExecutionReadinessExport Export { get; }
        public bool Contains(ColonyExecutionReadinessDiagnosticCode code) { return Findings.Contains(code); }
    }
}
