using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Save
{
    public enum PersistentDataClass { Identity, GameplayState, SimulationState, ConfigurationRef, Diagnostic, QAEvidence, Telemetry, RetentionPolicy, Forbidden }
    public enum PersistentDataSensitivity { Unknown, Public, Internal, Sensitive }
    public sealed class PersistentDataClassificationRule { public PersistentDataClassificationRule(PersistenceDataKind kind, PersistentDataClass dataClass, PersistentDataSensitivity sensitivity, PersistenceBoundaryOwner owner, DataRetentionStatus retention) { Kind = kind; DataClass = dataClass; Sensitivity = sensitivity; Owner = owner; Retention = retention; } public PersistenceDataKind Kind { get; } public PersistentDataClass DataClass { get; } public PersistentDataSensitivity Sensitivity { get; } public PersistenceBoundaryOwner Owner { get; } public DataRetentionStatus Retention { get; } }
    public sealed class PersistentDataClassificationDiagnostics { public PersistentDataClassificationDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistentDataClassification
    {
        public PersistentDataClassification(IEnumerable<PersistentDataClassificationRule> rules) { Rules = (rules ?? Array.Empty<PersistentDataClassificationRule>()).OrderBy(r => r.Kind).ToList(); Diagnostics = new PersistentDataClassificationDiagnostics(Rules.Where(r => r.Owner == PersistenceBoundaryOwner.Unknown).Select(r => "Owner missing").Concat(Rules.Where(r => r.Sensitivity == PersistentDataSensitivity.Unknown).Select(r => "Sensitivity unknown")).Concat(Rules.Where(r => r.DataClass == PersistentDataClass.Forbidden).Select(r => "Forbidden class")).ToList()); }
        public IReadOnlyList<PersistentDataClassificationRule> Rules { get; }
        public PersistentDataClassificationDiagnostics Diagnostics { get; }
    }

    public enum SaveMigrationDependencyKind { Requires, Produces, MigratesTo, ValidatedBy, Blocks, Unknown }
    public sealed class SaveMigrationNode { public SaveMigrationNode(string id, string kind) { Id = id ?? string.Empty; Kind = kind ?? string.Empty; } public string Id { get; } public string Kind { get; } }
    public sealed class SaveMigrationEdge { public SaveMigrationEdge(string from, string to, SaveMigrationDependencyKind kind) { From = from ?? string.Empty; To = to ?? string.Empty; Kind = kind; } public string From { get; } public string To { get; } public SaveMigrationDependencyKind Kind { get; } }
    public sealed class SaveMigrationGraphDiagnostics { public SaveMigrationGraphDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class SaveMigrationDependencyGraph
    {
        public SaveMigrationDependencyGraph(IEnumerable<SaveMigrationNode> nodes, IEnumerable<SaveMigrationEdge> edges) { Nodes = (nodes ?? Array.Empty<SaveMigrationNode>()).OrderBy(n => n.Id, StringComparer.Ordinal).ToList(); Edges = (edges ?? Array.Empty<SaveMigrationEdge>()).OrderBy(e => e.From, StringComparer.Ordinal).ThenBy(e => e.To, StringComparer.Ordinal).ToList(); Diagnostics = new SaveMigrationGraphDiagnostics(Detect()); }
        public IReadOnlyList<SaveMigrationNode> Nodes { get; }
        public IReadOnlyList<SaveMigrationEdge> Edges { get; }
        public SaveMigrationGraphDiagnostics Diagnostics { get; }
        private IReadOnlyList<string> Detect()
        {
            var issues = new List<string>();
            issues.AddRange(Edges.Where(e => e.Kind == SaveMigrationDependencyKind.Unknown).Select(e => "Unknown edge"));
            issues.AddRange(Edges.Where(e => Nodes.All(n => n.Id != e.From) || Nodes.All(n => n.Id != e.To)).Select(e => "Missing dependency"));
            issues.AddRange(Nodes.Where(n => n.Kind == "migration" && Edges.All(e => e.From != n.Id && e.To != n.Id)).Select(n => $"Orphan migration: {n.Id}"));
            if (Edges.Any(e => Edges.Any(other => other.From == e.To && other.To == e.From))) issues.Add("Cycle detected");
            return issues;
        }
    }

    public enum SnapshotCompactionEligibility { KeepFull, CompactCandidate, ArchiveCandidate, Forbidden, NeedsMigration }
    public sealed class SnapshotCompactionRule { public SnapshotCompactionRule(SnapshotFamily family, SnapshotCompactionEligibility eligibility) { Family = family; Eligibility = eligibility; } public SnapshotFamily Family { get; } public SnapshotCompactionEligibility Eligibility { get; } }
    public sealed class SnapshotCompactionDiagnostics { public SnapshotCompactionDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class SnapshotCompactionPlan { public SnapshotCompactionPlan(SnapshotFamily family, SnapshotCompactionEligibility eligibility) { Family = family; Eligibility = eligibility; } public SnapshotFamily Family { get; } public SnapshotCompactionEligibility Eligibility { get; } }
    public sealed class SnapshotCompactionPolicy
    {
        public SnapshotCompactionPlan Evaluate(SnapshotFamily family, SnapshotIntegrityVerdict integrity, bool hasCriticalEvidence, bool migrationRequired, bool oldEnough)
        {
            SnapshotCompactionEligibility eligibility = integrity == SnapshotIntegrityVerdict.Blocked ? SnapshotCompactionEligibility.Forbidden : migrationRequired ? SnapshotCompactionEligibility.NeedsMigration : hasCriticalEvidence ? SnapshotCompactionEligibility.KeepFull : oldEnough ? SnapshotCompactionEligibility.CompactCandidate : SnapshotCompactionEligibility.KeepFull;
            return new SnapshotCompactionPlan(family, eligibility);
        }
    }

    public enum LongRunStorageScope { SaveSlots, Snapshots, ReplayTracks, QaEvidence, Telemetry, Diagnostics, IdentityMaps, Unknown }
    public sealed class LongRunStorageBudgetRule { public LongRunStorageBudgetRule(LongRunStorageScope scope, int softLimit, int hardLimit) { Scope = scope; SoftLimit = softLimit; HardLimit = hardLimit; } public LongRunStorageScope Scope { get; } public int SoftLimit { get; } public int HardLimit { get; } }
    public sealed class LongRunStorageBudgetFinding { public LongRunStorageBudgetFinding(LongRunStorageScope scope, SnapshotIntegritySeverity severity, string message) { Scope = scope; Severity = severity; Message = message ?? string.Empty; } public LongRunStorageScope Scope { get; } public SnapshotIntegritySeverity Severity { get; } public string Message { get; } }
    public sealed class LongRunStorageBudgetDiagnostics { public LongRunStorageBudgetDiagnostics(IReadOnlyList<LongRunStorageBudgetFinding> findings) { Findings = findings ?? Array.Empty<LongRunStorageBudgetFinding>(); } public IReadOnlyList<LongRunStorageBudgetFinding> Findings { get; } }
    public sealed class LongRunStorageBudget
    {
        public LongRunStorageBudget(IEnumerable<LongRunStorageBudgetRule> rules) { Rules = (rules ?? Array.Empty<LongRunStorageBudgetRule>()).OrderBy(r => r.Scope).ToList(); }
        public IReadOnlyList<LongRunStorageBudgetRule> Rules { get; }
        public LongRunStorageBudgetDiagnostics Evaluate(IReadOnlyDictionary<LongRunStorageScope, int> usage, bool criticalEvidence)
        {
            var findings = new List<LongRunStorageBudgetFinding>();
            foreach (LongRunStorageBudgetRule rule in Rules)
            {
                if (rule.Scope == LongRunStorageScope.Unknown) { findings.Add(new LongRunStorageBudgetFinding(rule.Scope, SnapshotIntegritySeverity.Error, "Unknown scope")); continue; }
                int value = usage != null && usage.TryGetValue(rule.Scope, out int count) ? count : 0;
                if (criticalEvidence && rule.Scope == LongRunStorageScope.QaEvidence) continue;
                if (value > rule.HardLimit) findings.Add(new LongRunStorageBudgetFinding(rule.Scope, SnapshotIntegritySeverity.Critical, "Hard budget exceeded"));
                else if (value > rule.SoftLimit) findings.Add(new LongRunStorageBudgetFinding(rule.Scope, SnapshotIntegritySeverity.Warning, "Soft budget exceeded"));
            }
            return new LongRunStorageBudgetDiagnostics(findings.OrderByDescending(f => f.Severity).ThenBy(f => f.Scope).ToList());
        }
    }

    public enum PersistenceAuditAction { CreateSaveIntent, ValidateSnapshot, RejectLoad, MigrationDeclared, RetentionDecision, CompactionCandidate, EvidenceLink, DestructiveForbidden }
    public enum PersistenceAuditActor { Unknown, UnityWorker, Qa, BeeServer, Demo }
    public sealed class PersistenceAuditEntry { public PersistenceAuditEntry(long revision, PersistenceAuditActor actor, PersistenceAuditAction action, PersistenceDataKind boundary, PersistentDataClass dataClass, string outcome, string beeSource, bool containsSecret = false) { Revision = revision; Actor = actor; Action = action; Boundary = boundary; DataClass = dataClass; Outcome = outcome ?? string.Empty; BeeSource = beeSource ?? string.Empty; ContainsSecret = containsSecret; } public long Revision { get; } public PersistenceAuditActor Actor { get; } public PersistenceAuditAction Action { get; } public PersistenceDataKind Boundary { get; } public PersistentDataClass DataClass { get; } public string Outcome { get; } public string BeeSource { get; } public bool ContainsSecret { get; } }
    public sealed class PersistenceAuditDiagnostics { public PersistenceAuditDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistenceAuditTrail
    {
        public PersistenceAuditTrail(IEnumerable<PersistenceAuditEntry> entries) { Entries = (entries ?? Array.Empty<PersistenceAuditEntry>()).OrderBy(e => e.Revision).ThenBy(e => e.BeeSource, StringComparer.Ordinal).ToList(); Diagnostics = new PersistenceAuditDiagnostics(Entries.Where(e => e.Actor == PersistenceAuditActor.Unknown).Select(e => "Actor missing").Concat(Entries.Where(e => e.Action == PersistenceAuditAction.DestructiveForbidden).Select(e => "Destructive action forbidden")).Concat(Entries.Where(e => e.ContainsSecret).Select(e => "Potential secret in audit")).ToList()); }
        public IReadOnlyList<PersistenceAuditEntry> Entries { get; }
        public PersistenceAuditDiagnostics Diagnostics { get; }
    }

    public enum DataRecoveryTrigger { InvalidChecksum, MissingMigration, DeadReference, InvalidSnapshot, CriticalEvidence }
    public enum DataRecoveryVerdict { Recoverable, NeedsBackup, NeedsMigration, Quarantine, Blocked, ManualReview }
    public sealed class DataRecoveryStep { public DataRecoveryStep(int order, string description) { Order = order; Description = description ?? string.Empty; } public int Order { get; } public string Description { get; } }
    public sealed class DataRecoveryDiagnostics { public DataRecoveryDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class DataRecoveryPlan
    {
        public DataRecoveryPlan(DataRecoveryTrigger trigger, bool criticalEvidence)
        {
            Trigger = trigger;
            Verdict = trigger == DataRecoveryTrigger.InvalidChecksum ? DataRecoveryVerdict.Quarantine : trigger == DataRecoveryTrigger.MissingMigration ? DataRecoveryVerdict.NeedsMigration : trigger == DataRecoveryTrigger.DeadReference ? DataRecoveryVerdict.ManualReview : criticalEvidence ? DataRecoveryVerdict.Blocked : DataRecoveryVerdict.Recoverable;
            Steps = new[] { new DataRecoveryStep(1, "Record failure evidence"), new DataRecoveryStep(2, "Request non-destructive review") };
            Diagnostics = new DataRecoveryDiagnostics(criticalEvidence ? new[] { "Critical evidence prevents destructive recovery" } : Array.Empty<string>());
        }
        public DataRecoveryTrigger Trigger { get; }
        public DataRecoveryVerdict Verdict { get; }
        public IReadOnlyList<DataRecoveryStep> Steps { get; }
        public DataRecoveryDiagnostics Diagnostics { get; }
    }

    public enum CrossVersionLoadVerdict { Supported, Deprecated, NeedsMigration, Blocked, RecoveryRequired, InsufficientEvidence }
    public sealed class CrossVersionLoadStep { public CrossVersionLoadStep(int order, string description) { Order = order; Description = description ?? string.Empty; } public int Order { get; } public string Description { get; } }
    public sealed class CrossVersionLoadScenario { public CrossVersionLoadScenario(string id, string clientVersion, string serverContractVersion, SaveMigrationVersion saveVersion, SnapshotSchemaVersion schemaVersion, bool migrationPath, CrossVersionLoadVerdict expectedVerdict) { Id = id ?? string.Empty; ClientVersion = clientVersion ?? string.Empty; ServerContractVersion = serverContractVersion ?? string.Empty; SaveVersion = saveVersion; SchemaVersion = schemaVersion; MigrationPath = migrationPath; ExpectedVerdict = expectedVerdict; } public string Id { get; } public string ClientVersion { get; } public string ServerContractVersion { get; } public SaveMigrationVersion SaveVersion { get; } public SnapshotSchemaVersion SchemaVersion { get; } public bool MigrationPath { get; } public CrossVersionLoadVerdict ExpectedVerdict { get; } }
    public sealed class CrossVersionLoadDiagnostics { public CrossVersionLoadDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class CrossVersionLoadMatrix { public CrossVersionLoadMatrix(IEnumerable<CrossVersionLoadScenario> scenarios) { Scenarios = (scenarios ?? Array.Empty<CrossVersionLoadScenario>()).OrderBy(s => s.Id, StringComparer.Ordinal).ToList(); Diagnostics = new CrossVersionLoadDiagnostics(Scenarios.Where(s => s.ExpectedVerdict == CrossVersionLoadVerdict.InsufficientEvidence).Select(s => $"Insufficient evidence: {s.Id}").ToList()); } public IReadOnlyList<CrossVersionLoadScenario> Scenarios { get; } public CrossVersionLoadDiagnostics Diagnostics { get; } }

    public enum ContentRegistryLinkStatus { Resolved, MissingDefinition, DeprecatedDefinition, VersionMismatch, ForbiddenDuplicate }
    public sealed class PersistentContentRef { public PersistentContentRef(string id, string family, string version) { Id = id ?? string.Empty; Family = family ?? string.Empty; Version = version ?? string.Empty; } public string Id { get; } public string Family { get; } public string Version { get; } }
    public sealed class ContentRegistryLinkRule { public ContentRegistryLinkRule(string family) { Family = family ?? string.Empty; } public string Family { get; } }
    public sealed class ContentRegistryLinkDiagnostics { public ContentRegistryLinkDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistentContentRegistryLink
    {
        public ContentRegistryLinkStatus Resolve(PersistentContentRef reference, IReadOnlyDictionary<string, string> registry, bool deprecated, bool duplicate)
        {
            if (duplicate) return ContentRegistryLinkStatus.ForbiddenDuplicate;
            if (reference == null || registry == null || !registry.ContainsKey(reference.Id)) return ContentRegistryLinkStatus.MissingDefinition;
            if (deprecated) return ContentRegistryLinkStatus.DeprecatedDefinition;
            return registry[reference.Id] == reference.Version ? ContentRegistryLinkStatus.Resolved : ContentRegistryLinkStatus.VersionMismatch;
        }
    }

    public enum PersistenceQACoverageAxis { Boundaries, Migrations, Schema, Identity, Compatibility, Retention, Integrity, Failures, Recovery, ContentLinks, LongRunBudgets }
    public sealed class PersistenceQAEvidenceRequirement { public PersistenceQAEvidenceRequirement(PersistenceQACoverageAxis axis, string bee) { Axis = axis; Bee = bee ?? string.Empty; } public PersistenceQACoverageAxis Axis { get; } public string Bee { get; } }
    public sealed class PersistenceQACoverageCell { public PersistenceQACoverageCell(PersistenceQACoverageAxis axis, string evidence, bool obsolete = false) { Axis = axis; Evidence = evidence ?? string.Empty; Obsolete = obsolete; } public PersistenceQACoverageAxis Axis { get; } public string Evidence { get; } public bool Obsolete { get; } public bool Covered => !string.IsNullOrWhiteSpace(Evidence); }
    public sealed class PersistenceQACoverageGap { public PersistenceQACoverageGap(PersistenceQACoverageAxis axis, string reason) { Axis = axis; Reason = reason ?? string.Empty; } public PersistenceQACoverageAxis Axis { get; } public string Reason { get; } }
    public sealed class PersistenceQACoverageDiagnostics { public PersistenceQACoverageDiagnostics(IReadOnlyList<PersistenceQACoverageGap> gaps) { Gaps = gaps ?? Array.Empty<PersistenceQACoverageGap>(); } public IReadOnlyList<PersistenceQACoverageGap> Gaps { get; } }
    public sealed class PersistenceQACoverageMatrix
    {
        public PersistenceQACoverageMatrix(IEnumerable<PersistenceQACoverageCell> cells) { Cells = (cells ?? Array.Empty<PersistenceQACoverageCell>()).OrderBy(c => c.Axis).ToList(); Diagnostics = new PersistenceQACoverageDiagnostics(Enum.GetValues(typeof(PersistenceQACoverageAxis)).Cast<PersistenceQACoverageAxis>().Where(axis => Cells.All(c => c.Axis != axis || !c.Covered || c.Obsolete)).Select(axis => new PersistenceQACoverageGap(axis, Cells.Any(c => c.Axis == axis && c.Obsolete) ? "Evidence obsolete" : "Evidence missing")).ToList()); }
        public IReadOnlyList<PersistenceQACoverageCell> Cells { get; }
        public PersistenceQACoverageDiagnostics Diagnostics { get; }
    }

    public enum DataGovernanceVerdict { Ready, ReadyWithWarnings, NeedsRevision, Blocked }
    public sealed class DataGovernanceCriterion { public DataGovernanceCriterion(string name, bool passed, bool warning, bool blocking) { Name = name ?? string.Empty; Passed = passed; Warning = warning; Blocking = blocking; } public string Name { get; } public bool Passed { get; } public bool Warning { get; } public bool Blocking { get; } }
    public sealed class DataGovernanceDiagnostics { public DataGovernanceDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class DataGovernanceReport { public DataGovernanceReport(DataGovernanceVerdict verdict, IEnumerable<DataGovernanceCriterion> criteria) { Verdict = verdict; Criteria = (criteria ?? Array.Empty<DataGovernanceCriterion>()).OrderBy(c => c.Name, StringComparer.Ordinal).ToList(); Diagnostics = new DataGovernanceDiagnostics(Criteria.Where(c => !c.Passed).Select(c => c.Name).ToList()); } public DataGovernanceVerdict Verdict { get; } public IReadOnlyList<DataGovernanceCriterion> Criteria { get; } public DataGovernanceDiagnostics Diagnostics { get; } }
    public sealed class DataGovernanceGate
    {
        public DataGovernanceReport Evaluate(IEnumerable<DataGovernanceCriterion> criteria, bool bee221Referenced)
        {
            var list = (criteria ?? Array.Empty<DataGovernanceCriterion>()).ToList();
            if (bee221Referenced) list.Add(new DataGovernanceCriterion("BEE-221-blocked", false, false, true));
            DataGovernanceVerdict verdict = list.Any(c => !c.Passed && c.Blocking) ? DataGovernanceVerdict.Blocked : list.Any(c => !c.Passed) ? DataGovernanceVerdict.NeedsRevision : list.Any(c => c.Warning) ? DataGovernanceVerdict.ReadyWithWarnings : DataGovernanceVerdict.Ready;
            return new DataGovernanceReport(verdict, list);
        }
    }
}
