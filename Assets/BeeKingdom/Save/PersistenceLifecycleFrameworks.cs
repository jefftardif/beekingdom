using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Save
{
    public enum PersistentLifecycleState { Draft, Active, Deprecated, ArchivedCandidate, Blocked, Invalid }
    public sealed class PersistentLifecycleTransition { public PersistentLifecycleTransition(PersistentLifecycleState from, PersistentLifecycleState to, string reason) { From = from; To = to; Reason = reason ?? string.Empty; } public PersistentLifecycleState From { get; } public PersistentLifecycleState To { get; } public string Reason { get; } }
    public sealed class PersistentLifecycleConflict { public PersistentLifecycleConflict(string reason) { Reason = reason ?? string.Empty; } public string Reason { get; } }
    public sealed class PersistentLifecycleDiagnostics { public PersistentLifecycleDiagnostics(IReadOnlyList<PersistentLifecycleConflict> conflicts) { Conflicts = conflicts ?? Array.Empty<PersistentLifecycleConflict>(); } public IReadOnlyList<PersistentLifecycleConflict> Conflicts { get; } public bool Allowed => Conflicts.Count == 0; }
    public sealed class PersistentLifecycleRule
    {
        public PersistentLifecycleDiagnostics Validate(PersistentLifecycleTransition transition, bool qaCriticalEvidence)
        {
            var conflicts = new List<PersistentLifecycleConflict>();
            bool allowed = transition != null && ((transition.From == PersistentLifecycleState.Draft && transition.To == PersistentLifecycleState.Active) || (transition.From == PersistentLifecycleState.Active && transition.To == PersistentLifecycleState.Deprecated && !string.IsNullOrWhiteSpace(transition.Reason)) || (transition.To == PersistentLifecycleState.ArchivedCandidate));
            if (!allowed) conflicts.Add(new PersistentLifecycleConflict("Unknown or unsupported transition"));
            if (qaCriticalEvidence && transition != null && transition.To == PersistentLifecycleState.Invalid) conflicts.Add(new PersistentLifecycleConflict("Critical QA evidence blocks invalidation"));
            return new PersistentLifecycleDiagnostics(conflicts);
        }
    }

    public enum RetentionFactor { Gameplay, Qa, Audit, Server, Demo, Forbidden, Budget, Compatibility }
    public enum RetentionResolution { Keep, ArchiveCandidate, ExpireCandidate, RedactRequired, Blocked }
    public sealed class RetentionSchedule { public RetentionSchedule(DataRetentionScope scope, RetentionResolution resolution) { Scope = scope; Resolution = resolution; } public DataRetentionScope Scope { get; } public RetentionResolution Resolution { get; } }
    public sealed class RetentionScheduleDiagnostics { public RetentionScheduleDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class RetentionScheduleResolver
    {
        public (RetentionSchedule Schedule, RetentionScheduleDiagnostics Diagnostics) Resolve(PersistentDataClass dataClass, PersistentLifecycleState state, bool qaCritical, bool budgetWarning, bool physicalRetentionRequested)
        {
            var issues = new List<string>();
            if (physicalRetentionRequested) issues.Add("Physical retention action requested");
            RetentionResolution resolution = dataClass == PersistentDataClass.Forbidden ? RetentionResolution.Blocked : qaCritical || dataClass == PersistentDataClass.QAEvidence ? RetentionResolution.Keep : state == PersistentLifecycleState.Deprecated ? RetentionResolution.ArchiveCandidate : budgetWarning ? RetentionResolution.ExpireCandidate : RetentionResolution.Keep;
            return (new RetentionSchedule(DataRetentionScope.OfficialSave, resolution), new RetentionScheduleDiagnostics(issues));
        }
    }

    public enum ArchiveEligibilityVerdict { Eligible, EligibleWithWarnings, NotEligible, Blocked }
    public enum ArchiveBlockReason { CriticalQAEvidence, InvalidSnapshot, AuditRequired, ActiveGameplayState, RecoveryRequired }
    public sealed class ArchiveEligibilityDiagnostics { public ArchiveEligibilityDiagnostics(IReadOnlyList<ArchiveBlockReason> reasons) { Reasons = reasons ?? Array.Empty<ArchiveBlockReason>(); } public IReadOnlyList<ArchiveBlockReason> Reasons { get; } }
    public sealed class ArchiveEligibilityRule { public ArchiveEligibilityRule(ArchiveBlockReason reason) { Reason = reason; } public ArchiveBlockReason Reason { get; } }
    public sealed class ArchiveEligibilityPolicy
    {
        public (ArchiveEligibilityVerdict Verdict, ArchiveEligibilityDiagnostics Diagnostics) Evaluate(bool criticalQa, bool deprecatedCompatible, bool invalidSnapshot, bool auditRequired, bool activeGameplay)
        {
            var reasons = new List<ArchiveBlockReason>();
            if (criticalQa) reasons.Add(ArchiveBlockReason.CriticalQAEvidence);
            if (invalidSnapshot) reasons.Add(ArchiveBlockReason.InvalidSnapshot);
            if (auditRequired) reasons.Add(ArchiveBlockReason.AuditRequired);
            if (activeGameplay) reasons.Add(ArchiveBlockReason.ActiveGameplayState);
            ArchiveEligibilityVerdict verdict = criticalQa || auditRequired || activeGameplay ? ArchiveEligibilityVerdict.Blocked : invalidSnapshot ? ArchiveEligibilityVerdict.NotEligible : deprecatedCompatible ? ArchiveEligibilityVerdict.EligibleWithWarnings : ArchiveEligibilityVerdict.Eligible;
            return (verdict, new ArchiveEligibilityDiagnostics(reasons.OrderBy(r => r).ToList()));
        }
    }

    public enum SensitiveFieldClass { Unknown, Token, AccountId, SessionId, CorrelationId, ServerDiagnostic, QaReport }
    public enum RedactionOutputRule { RawForbidden, Redacted, HashedReference, ClientSafe, QAOnly }
    public sealed class RedactionRequirement { public RedactionRequirement(SensitiveFieldClass fieldClass, RedactionOutputRule outputRule) { FieldClass = fieldClass; OutputRule = outputRule; } public SensitiveFieldClass FieldClass { get; } public RedactionOutputRule OutputRule { get; } }
    public sealed class RedactionDiagnostics { public RedactionDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class RedactionRequirementRegistry
    {
        private readonly List<RedactionRequirement> requirements;
        public RedactionRequirementRegistry(IEnumerable<RedactionRequirement> requirements) { this.requirements = (requirements ?? Array.Empty<RedactionRequirement>()).OrderBy(r => r.FieldClass).ToList(); }
        public IReadOnlyList<RedactionRequirement> Requirements => requirements;
        public (RedactionOutputRule Rule, RedactionDiagnostics Diagnostics) Resolve(SensitiveFieldClass fieldClass)
        {
            RedactionRequirement requirement = requirements.FirstOrDefault(r => r.FieldClass == fieldClass);
            if (fieldClass == SensitiveFieldClass.Unknown || requirement == null) return (RedactionOutputRule.RawForbidden, new RedactionDiagnostics(new[] { "Sensitive field is not classified" }));
            return (requirement.OutputRule, new RedactionDiagnostics(Array.Empty<string>()));
        }
    }

    public enum PersistenceEventKind { Unknown, Create, Load, Save, MigrateCandidate, CompactCandidate, Verify, Fail, Recover, Reject, RedactionRequired }
    public enum PersistenceEventSeverity { Info, Warning, Error, Critical }
    public enum PersistenceEventSource { Unity, Worker, QA, BeeServer, Demo }
    public sealed class PersistenceEventCorrelation { public PersistenceEventCorrelation(string id) { Id = id ?? string.Empty; } public string Id { get; } }
    public sealed class PersistenceEventTaxonomy
    {
        public PersistenceEventSeverity Classify(PersistenceEventKind kind)
        {
            return kind == PersistenceEventKind.Unknown ? PersistenceEventSeverity.Critical : kind == PersistenceEventKind.Fail || kind == PersistenceEventKind.Reject ? PersistenceEventSeverity.Error : kind == PersistenceEventKind.RedactionRequired ? PersistenceEventSeverity.Warning : PersistenceEventSeverity.Info;
        }
    }

    public enum LongRunSamplingPriority { Low, Medium, High, Critical }
    public sealed class LongRunSamplingCriterion { public LongRunSamplingCriterion(string name, LongRunSamplingPriority priority) { Name = name ?? string.Empty; Priority = priority; } public string Name { get; } public LongRunSamplingPriority Priority { get; } }
    public sealed class LongRunSnapshotSample { public LongRunSnapshotSample(string sampleId, IEnumerable<LongRunSamplingCriterion> criteria) { SampleId = sampleId ?? string.Empty; Criteria = (criteria ?? Array.Empty<LongRunSamplingCriterion>()).OrderByDescending(c => c.Priority).ThenBy(c => c.Name, StringComparer.Ordinal).ToList(); Priority = Criteria.Any() ? Criteria.Max(c => c.Priority) : LongRunSamplingPriority.Low; } public string SampleId { get; } public IReadOnlyList<LongRunSamplingCriterion> Criteria { get; } public LongRunSamplingPriority Priority { get; } }
    public sealed class LongRunSamplingDiagnostics { public LongRunSamplingDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class LongRunSamplingPlan { public LongRunSamplingPlan(IEnumerable<LongRunSnapshotSample> samples) { Samples = (samples ?? Array.Empty<LongRunSnapshotSample>()).OrderByDescending(s => s.Priority).ThenBy(s => s.SampleId, StringComparer.Ordinal).ToList(); Diagnostics = new LongRunSamplingDiagnostics(Array.Empty<string>()); } public IReadOnlyList<LongRunSnapshotSample> Samples { get; } public LongRunSamplingDiagnostics Diagnostics { get; } }

    public enum PersistenceDriftKind { Unknown, Schema, Identity, Retention, Evidence, Integrity, ContentRegistry }
    public enum PersistenceDriftSeverity { Info, Warning, Error, Critical }
    public sealed class PersistenceDriftFinding { public PersistenceDriftFinding(PersistenceDriftKind kind, PersistenceDriftSeverity severity, string source, string recommendation) { Kind = kind; Severity = severity; Source = source ?? string.Empty; Recommendation = recommendation ?? string.Empty; } public PersistenceDriftKind Kind { get; } public PersistenceDriftSeverity Severity { get; } public string Source { get; } public string Recommendation { get; } }
    public sealed class PersistenceDriftDiagnostics { public PersistenceDriftDiagnostics(IReadOnlyList<PersistenceDriftFinding> findings) { Findings = findings ?? Array.Empty<PersistenceDriftFinding>(); } public IReadOnlyList<PersistenceDriftFinding> Findings { get; } }
    public sealed class PersistenceDriftDetector
    {
        public PersistenceDriftDiagnostics Detect(string expected, string observed, PersistenceDriftKind kind, string source)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(observed) || kind == PersistenceDriftKind.Unknown) return new PersistenceDriftDiagnostics(new[] { new PersistenceDriftFinding(PersistenceDriftKind.Unknown, PersistenceDriftSeverity.Warning, source, "Keep unknown drift visible") });
            if (expected == observed) return new PersistenceDriftDiagnostics(Array.Empty<PersistenceDriftFinding>());
            PersistenceDriftSeverity severity = kind == PersistenceDriftKind.Integrity || kind == PersistenceDriftKind.Identity ? PersistenceDriftSeverity.Critical : PersistenceDriftSeverity.Error;
            return new PersistenceDriftDiagnostics(new[] { new PersistenceDriftFinding(kind, severity, source, "Review non-destructively") });
        }
    }

    public enum DataGovernanceReportVerdict { Valid, ValidWithWarnings, NeedsEvidence, Blocked }
    public sealed class DataGovernanceReportFinding { public DataGovernanceReportFinding(string source, PersistenceDriftSeverity severity, string message) { Source = source ?? string.Empty; Severity = severity; Message = message ?? string.Empty; } public string Source { get; } public PersistenceDriftSeverity Severity { get; } public string Message { get; } }
    public sealed class DataGovernanceReportSection { public DataGovernanceReportSection(string name, IEnumerable<DataGovernanceReportFinding> findings, bool requiresRedaction) { Name = name ?? string.Empty; Findings = (findings ?? Array.Empty<DataGovernanceReportFinding>()).OrderByDescending(f => f.Severity).ThenBy(f => f.Source, StringComparer.Ordinal).ToList(); RequiresRedaction = requiresRedaction; } public string Name { get; } public IReadOnlyList<DataGovernanceReportFinding> Findings { get; } public bool RequiresRedaction { get; } }
    public sealed class DataGovernanceReportDiagnostics { public DataGovernanceReportDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class DataGovernanceExportReport { public DataGovernanceExportReport(IEnumerable<DataGovernanceReportSection> sections) { Sections = (sections ?? Array.Empty<DataGovernanceReportSection>()).OrderBy(s => s.Name, StringComparer.Ordinal).ToList(); Diagnostics = new DataGovernanceReportDiagnostics(Sections.Where(s => s.RequiresRedaction).Select(s => $"Redaction required: {s.Name}").Concat(Sections.Where(s => !s.Findings.Any()).Select(s => $"Evidence missing: {s.Name}")).ToList()); Verdict = Sections.SelectMany(s => s.Findings).Any(f => f.Severity == PersistenceDriftSeverity.Critical) ? DataGovernanceReportVerdict.Blocked : Diagnostics.Issues.Any() ? DataGovernanceReportVerdict.ValidWithWarnings : DataGovernanceReportVerdict.Valid; } public IReadOnlyList<DataGovernanceReportSection> Sections { get; } public DataGovernanceReportDiagnostics Diagnostics { get; } public DataGovernanceReportVerdict Verdict { get; } }

    public enum PersistenceServerHandoffStatus { Ready, AnalysisRequired, Warning, Blocked }
    public sealed class PersistenceServerHandoffRequirement { public PersistenceServerHandoffRequirement(string beeSource, string requirement, PersistenceServerHandoffStatus status) { BeeSource = beeSource ?? string.Empty; Requirement = requirement ?? string.Empty; Status = status; } public string BeeSource { get; } public string Requirement { get; } public PersistenceServerHandoffStatus Status { get; } }
    public sealed class PersistenceServerHandoffGap { public PersistenceServerHandoffGap(string reason) { Reason = reason ?? string.Empty; } public string Reason { get; } }
    public sealed class PersistenceServerHandoffDiagnostics { public PersistenceServerHandoffDiagnostics(IReadOnlyList<PersistenceServerHandoffGap> gaps) { Gaps = gaps ?? Array.Empty<PersistenceServerHandoffGap>(); } public IReadOnlyList<PersistenceServerHandoffGap> Gaps { get; } }
    public sealed class PersistenceServerHandoffChecklist
    {
        public PersistenceServerHandoffChecklist(IEnumerable<PersistenceServerHandoffRequirement> requirements)
        {
            Requirements = (requirements ?? Array.Empty<PersistenceServerHandoffRequirement>()).OrderBy(r => r.BeeSource, StringComparer.Ordinal).ThenBy(r => r.Requirement, StringComparer.Ordinal).ToList();
            Diagnostics = new PersistenceServerHandoffDiagnostics(Requirements.Where(r => string.IsNullOrWhiteSpace(r.BeeSource)).Select(r => new PersistenceServerHandoffGap("Requirement without BEE source")).Concat(Requirements.Where(r => r.Status == PersistenceServerHandoffStatus.AnalysisRequired).Select(r => new PersistenceServerHandoffGap($"Analysis required: {r.BeeSource}"))).ToList());
        }
        public IReadOnlyList<PersistenceServerHandoffRequirement> Requirements { get; }
        public PersistenceServerHandoffDiagnostics Diagnostics { get; }
    }

    public enum PersistenceLifecycleVerdict { Ready, ReadyWithWarnings, NeedsRevision, Blocked }
    public sealed class PersistenceLifecycleCriterion { public PersistenceLifecycleCriterion(string name, bool passed, bool warning, bool blocking) { Name = name ?? string.Empty; Passed = passed; Warning = warning; Blocking = blocking; } public string Name { get; } public bool Passed { get; } public bool Warning { get; } public bool Blocking { get; } }
    public sealed class PersistenceLifecycleDiagnostics { public PersistenceLifecycleDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistenceLifecycleReport { public PersistenceLifecycleReport(PersistenceLifecycleVerdict verdict, IEnumerable<PersistenceLifecycleCriterion> criteria) { Verdict = verdict; Criteria = (criteria ?? Array.Empty<PersistenceLifecycleCriterion>()).OrderBy(c => c.Name, StringComparer.Ordinal).ToList(); Diagnostics = new PersistenceLifecycleDiagnostics(Criteria.Where(c => !c.Passed).Select(c => c.Name).ToList()); } public PersistenceLifecycleVerdict Verdict { get; } public IReadOnlyList<PersistenceLifecycleCriterion> Criteria { get; } public PersistenceLifecycleDiagnostics Diagnostics { get; } }
    public sealed class PersistenceLifecycleGate
    {
        public PersistenceLifecycleReport Evaluate(IEnumerable<PersistenceLifecycleCriterion> criteria, bool serverProgressRead, bool bee231Referenced)
        {
            var list = (criteria ?? Array.Empty<PersistenceLifecycleCriterion>()).ToList();
            if (!serverProgressRead) list.Add(new PersistenceLifecycleCriterion("SERVER_PROGRESS-read", false, false, true));
            if (bee231Referenced) list.Add(new PersistenceLifecycleCriterion("BEE-231-blocked", false, false, true));
            PersistenceLifecycleVerdict verdict = list.Any(c => !c.Passed && c.Blocking) ? PersistenceLifecycleVerdict.Blocked : list.Any(c => !c.Passed) ? PersistenceLifecycleVerdict.NeedsRevision : list.Any(c => c.Warning) ? PersistenceLifecycleVerdict.ReadyWithWarnings : PersistenceLifecycleVerdict.Ready;
            return new PersistenceLifecycleReport(verdict, list);
        }
    }
}
