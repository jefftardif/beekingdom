using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Save
{
    public enum PersistenceBoundaryOwner { Unknown, UnityLocalSave, EngineSnapshot, SharedContract, ServerAuthority, FutureSql, QaReport, DemoReadOnly }
    public enum PersistenceDataKind { HiveSave, WorldSave, RegionalSnapshot, AuthoritySnapshot, ServerColonyState, QaEvidence, DemoEvidence }
    public sealed class PersistenceBoundaryEntry { public PersistenceBoundaryEntry(PersistenceDataKind kind, PersistenceBoundaryOwner owner, bool canStore) { Kind = kind; Owner = owner; CanStore = canStore; } public PersistenceDataKind Kind { get; } public PersistenceBoundaryOwner Owner { get; } public bool CanStore { get; } }
    public sealed class PersistenceBoundaryDiagnostics { public PersistenceBoundaryDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistenceBoundaryInventory
    {
        public PersistenceBoundaryInventory(IEnumerable<PersistenceBoundaryEntry> entries)
        {
            Entries = (entries ?? Array.Empty<PersistenceBoundaryEntry>()).OrderBy(e => e.Kind).ThenBy(e => e.Owner).ToList();
            Diagnostics = new PersistenceBoundaryDiagnostics(Entries.Where(e => e.Owner == PersistenceBoundaryOwner.Unknown).Select(e => "Entry without owner").Concat(Entries.Where(e => e.Owner == PersistenceBoundaryOwner.DemoReadOnly && e.CanStore).Select(e => "Demo cannot own storage")).ToList());
        }
        public IReadOnlyList<PersistenceBoundaryEntry> Entries { get; }
        public PersistenceBoundaryDiagnostics Diagnostics { get; }
    }

    public readonly struct SaveMigrationVersion : IComparable<SaveMigrationVersion> { public SaveMigrationVersion(int value) { Value = value; } public int Value { get; } public int CompareTo(SaveMigrationVersion other) => Value.CompareTo(other.Value); public override string ToString() => $"save-{Value}"; }
    public sealed class SaveMigrationPrecondition { public SaveMigrationPrecondition(string name) { Name = name ?? string.Empty; } public string Name { get; } }
    public sealed class SaveMigrationEntry { public SaveMigrationEntry(SaveMigrationVersion source, SaveMigrationVersion target, string domain, IEnumerable<SaveMigrationPrecondition> preconditions, string fallback) { Source = source; Target = target; Domain = domain ?? string.Empty; Preconditions = (preconditions ?? Array.Empty<SaveMigrationPrecondition>()).ToList(); Fallback = fallback ?? string.Empty; } public SaveMigrationVersion Source { get; } public SaveMigrationVersion Target { get; } public string Domain { get; } public IReadOnlyList<SaveMigrationPrecondition> Preconditions { get; } public string Fallback { get; } }
    public sealed class SaveMigrationDiagnostics { public SaveMigrationDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class SaveMigrationManifest
    {
        public SaveMigrationManifest(IEnumerable<SaveMigrationEntry> entries)
        {
            Entries = (entries ?? Array.Empty<SaveMigrationEntry>()).OrderBy(e => e.Source).ThenBy(e => e.Target).ToList();
            Diagnostics = new SaveMigrationDiagnostics(DetectIssues(Entries));
        }
        public IReadOnlyList<SaveMigrationEntry> Entries { get; }
        public SaveMigrationDiagnostics Diagnostics { get; }
        public SaveMigrationEntry Resolve(SaveMigrationVersion source) => Entries.FirstOrDefault(e => e.Source.Value == source.Value);
        private static IReadOnlyList<string> DetectIssues(IReadOnlyList<SaveMigrationEntry> entries)
        {
            var issues = new List<string>();
            if (entries.Count == 0) issues.Add("Manifest empty");
            for (int i = 1; i < entries.Count; i++) if (entries[i - 1].Target.Value != entries[i].Source.Value) issues.Add("Migration chain broken");
            return issues;
        }
    }

    public enum SnapshotFamily { Unknown, Hive, Queen, Lifecycle, World, Region, Authority, QaEvidence }
    public readonly struct SnapshotSchemaVersion { public SnapshotSchemaVersion(int value) { Value = value; } public int Value { get; } public override string ToString() => $"schema-{Value}"; }
    public sealed class SnapshotSchemaRequirement { public SnapshotSchemaRequirement(SnapshotFamily family, SnapshotSchemaVersion current, SnapshotSchemaVersion minimum) { Family = family; Current = current; Minimum = minimum; } public SnapshotFamily Family { get; } public SnapshotSchemaVersion Current { get; } public SnapshotSchemaVersion Minimum { get; } }
    public sealed class SnapshotSchemaDiagnostics { public SnapshotSchemaDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class SnapshotSchemaRegistry
    {
        public SnapshotSchemaRegistry(IEnumerable<SnapshotSchemaRequirement> requirements) { Requirements = (requirements ?? Array.Empty<SnapshotSchemaRequirement>()).OrderBy(r => r.Family).ToList(); }
        public IReadOnlyList<SnapshotSchemaRequirement> Requirements { get; }
        public SnapshotSchemaDiagnostics Validate(SnapshotFamily family, SnapshotSchemaVersion version)
        {
            SnapshotSchemaRequirement req = Requirements.FirstOrDefault(r => r.Family == family);
            if (family == SnapshotFamily.Unknown || req == null) return new SnapshotSchemaDiagnostics(new[] { "Snapshot family unknown" });
            if (version.Value < req.Minimum.Value) return new SnapshotSchemaDiagnostics(new[] { "Snapshot schema obsolete" });
            if (version.Value > req.Current.Value) return new SnapshotSchemaDiagnostics(new[] { "Snapshot schema is newer than registry" });
            return new SnapshotSchemaDiagnostics(Array.Empty<string>());
        }
    }

    public enum PersistentIdentityDomain { Unknown, Hive, Colony, Region, World, Server, Snapshot, ReadModel }
    public sealed class PersistentIdentity { public PersistentIdentity(string id, PersistentIdentityDomain domain) { Id = id ?? string.Empty; Domain = domain; } public string Id { get; } public PersistentIdentityDomain Domain { get; } }
    public sealed class PersistentIdentityAlias { public PersistentIdentityAlias(string alias, string targetId) { Alias = alias ?? string.Empty; TargetId = targetId ?? string.Empty; } public string Alias { get; } public string TargetId { get; } }
    public sealed class PersistentIdentityDiagnostics { public PersistentIdentityDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistentIdentityMap
    {
        public PersistentIdentityMap(IEnumerable<PersistentIdentity> identities, IEnumerable<PersistentIdentityAlias> aliases)
        {
            Identities = (identities ?? Array.Empty<PersistentIdentity>()).OrderBy(i => i.Domain).ThenBy(i => i.Id, StringComparer.Ordinal).ToList();
            Aliases = (aliases ?? Array.Empty<PersistentIdentityAlias>()).OrderBy(a => a.Alias, StringComparer.Ordinal).ToList();
            Diagnostics = new PersistentIdentityDiagnostics(DetectIssues());
        }
        public IReadOnlyList<PersistentIdentity> Identities { get; }
        public IReadOnlyList<PersistentIdentityAlias> Aliases { get; }
        public PersistentIdentityDiagnostics Diagnostics { get; }
        public PersistentIdentity Resolve(string idOrAlias) => Identities.FirstOrDefault(i => i.Id == idOrAlias) ?? Identities.FirstOrDefault(i => i.Id == Aliases.FirstOrDefault(a => a.Alias == idOrAlias)?.TargetId);
        private IReadOnlyList<string> DetectIssues()
        {
            var issues = new List<string>();
            issues.AddRange(Identities.Where(i => i.Domain == PersistentIdentityDomain.Unknown).Select(i => $"Identity without domain: {i.Id}"));
            issues.AddRange(Identities.GroupBy(i => i.Id).Where(g => g.Count() > 1).Select(g => $"Duplicate identity: {g.Key}"));
            issues.AddRange(Aliases.GroupBy(a => a.Alias).Where(g => g.Select(a => a.TargetId).Distinct().Count() > 1).Select(g => $"Alias conflict: {g.Key}"));
            issues.AddRange(Aliases.Where(a => Identities.All(i => i.Id != a.TargetId)).Select(a => $"Dead reference: {a.Alias}"));
            return issues;
        }
    }

    public enum SaveCompatibilityAxis { SaveVersion, SnapshotSchema, ProtocolVersion, ClientVersion, ServerContract }
    public enum SaveCompatibilityVerdict { Compatible, NeedsMigration, Deprecated, Blocked, Unknown }
    public sealed class SaveCompatibilityCell { public SaveCompatibilityCell(SaveCompatibilityAxis axis, string value, SaveCompatibilityVerdict verdict, string reason) { Axis = axis; Value = value ?? string.Empty; Verdict = verdict; Reason = reason ?? string.Empty; } public SaveCompatibilityAxis Axis { get; } public string Value { get; } public SaveCompatibilityVerdict Verdict { get; } public string Reason { get; } }
    public sealed class SaveCompatibilityDiagnostics { public SaveCompatibilityDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class SaveCompatibilityMatrix
    {
        public SaveCompatibilityMatrix(IEnumerable<SaveCompatibilityCell> cells) { Cells = (cells ?? Array.Empty<SaveCompatibilityCell>()).OrderBy(c => c.Axis).ThenBy(c => c.Value, StringComparer.Ordinal).ToList(); }
        public IReadOnlyList<SaveCompatibilityCell> Cells { get; }
        public SaveCompatibilityDiagnostics Diagnostics => new SaveCompatibilityDiagnostics(Cells.Where(c => c.Verdict == SaveCompatibilityVerdict.Unknown).Select(c => $"Unknown compatibility: {c.Axis}/{c.Value}").ToList());
        public SaveCompatibilityVerdict Evaluate(SaveCompatibilityAxis axis, string value) => Cells.FirstOrDefault(c => c.Axis == axis && c.Value == value)?.Verdict ?? SaveCompatibilityVerdict.Unknown;
    }

    public enum DataRetentionScope { OfficialSave, ReplayTrack, AuthorityTelemetry, QaEvidence, AccountAdjacentId, DemoData }
    public enum DataRetentionStatus { Keep, Expire, Archive, Redact, Forbidden }
    public sealed class DataRetentionRule { public DataRetentionRule(DataRetentionScope scope, DataRetentionStatus status) { Scope = scope; Status = status; } public DataRetentionScope Scope { get; } public DataRetentionStatus Status { get; } }
    public sealed class DataRetentionDiagnostics { public DataRetentionDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class DataRetentionPolicy
    {
        public DataRetentionPolicy(IEnumerable<DataRetentionRule> rules) { Rules = (rules ?? Array.Empty<DataRetentionRule>()).OrderBy(r => r.Scope).ToList(); Diagnostics = new DataRetentionDiagnostics(Rules.Where(r => r.Status == DataRetentionStatus.Forbidden).Select(r => $"Forbidden data: {r.Scope}").ToList()); }
        public IReadOnlyList<DataRetentionRule> Rules { get; }
        public DataRetentionDiagnostics Diagnostics { get; }
        public DataRetentionStatus Evaluate(DataRetentionScope scope) => Rules.FirstOrDefault(r => r.Scope == scope)?.Status ?? DataRetentionStatus.Forbidden;
    }

    public enum SnapshotIntegrityVerdict { Valid, ValidWithWarnings, Invalid, Blocked }
    public enum SnapshotIntegritySeverity { Info, Warning, Error, Critical }
    public sealed class SnapshotIntegrityRule { public SnapshotIntegrityRule(string name) { Name = name ?? string.Empty; } public string Name { get; } }
    public sealed class SnapshotIntegrityIssue { public SnapshotIntegrityIssue(SnapshotIntegritySeverity severity, string message) { Severity = severity; Message = message ?? string.Empty; } public SnapshotIntegritySeverity Severity { get; } public string Message { get; } }
    public sealed class SnapshotIntegrityDiagnostics { public SnapshotIntegrityDiagnostics(IReadOnlyList<SnapshotIntegrityIssue> issues) { Issues = issues ?? Array.Empty<SnapshotIntegrityIssue>(); } public IReadOnlyList<SnapshotIntegrityIssue> Issues { get; } }
    public sealed class SnapshotIntegrityResult { public SnapshotIntegrityResult(SnapshotIntegrityVerdict verdict, SnapshotIntegrityDiagnostics diagnostics) { Verdict = verdict; Diagnostics = diagnostics; } public SnapshotIntegrityVerdict Verdict { get; } public SnapshotIntegrityDiagnostics Diagnostics { get; } }
    public sealed class SnapshotIntegrityCheck
    {
        public SnapshotIntegrityResult Validate(bool checksumValid, bool deadReference, bool obsoleteSchema, bool unstableOrder)
        {
            var issues = new List<SnapshotIntegrityIssue>();
            if (!checksumValid) issues.Add(new SnapshotIntegrityIssue(SnapshotIntegritySeverity.Critical, "Invalid checksum"));
            if (deadReference) issues.Add(new SnapshotIntegrityIssue(SnapshotIntegritySeverity.Error, "Dead reference"));
            if (obsoleteSchema) issues.Add(new SnapshotIntegrityIssue(SnapshotIntegritySeverity.Warning, "Obsolete schema"));
            if (unstableOrder) issues.Add(new SnapshotIntegrityIssue(SnapshotIntegritySeverity.Warning, "Unstable collection order"));
            SnapshotIntegrityVerdict verdict = issues.Any(i => i.Severity == SnapshotIntegritySeverity.Critical) ? SnapshotIntegrityVerdict.Blocked : issues.Any(i => i.Severity == SnapshotIntegritySeverity.Error) ? SnapshotIntegrityVerdict.Invalid : issues.Any() ? SnapshotIntegrityVerdict.ValidWithWarnings : SnapshotIntegrityVerdict.Valid;
            return new SnapshotIntegrityResult(verdict, new SnapshotIntegrityDiagnostics(issues));
        }
    }

    public enum PersistenceFailureCode { Unknown, VersionUnknown, DeadReference, ObsoleteSchema, InvalidChecksum, MissingMigration, PartialData, ForbiddenRetention }
    public enum PersistenceFailureCategory { Unknown, Version, Reference, Schema, Checksum, Migration, Data, Retention }
    public sealed class PersistenceFailure { public PersistenceFailure(PersistenceFailureCode code, PersistenceFailureCategory category, SnapshotIntegritySeverity severity, string action) { Code = code; Category = category; Severity = severity; RecommendedAction = action ?? string.Empty; } public PersistenceFailureCode Code { get; } public PersistenceFailureCategory Category { get; } public SnapshotIntegritySeverity Severity { get; } public string RecommendedAction { get; } }
    public sealed class PersistenceFailureDiagnostics { public PersistenceFailureDiagnostics(PersistenceFailure failure) { Failure = failure; } public PersistenceFailure Failure { get; } }
    public sealed class PersistenceFailureCatalog
    {
        private readonly List<PersistenceFailure> failures = new List<PersistenceFailure>
        {
            new PersistenceFailure(PersistenceFailureCode.InvalidChecksum, PersistenceFailureCategory.Checksum, SnapshotIntegritySeverity.Critical, "Block load and request clean snapshot."),
            new PersistenceFailure(PersistenceFailureCode.MissingMigration, PersistenceFailureCategory.Migration, SnapshotIntegritySeverity.Error, "Declare migration before load."),
            new PersistenceFailure(PersistenceFailureCode.DeadReference, PersistenceFailureCategory.Reference, SnapshotIntegritySeverity.Error, "Inspect identity map."),
            new PersistenceFailure(PersistenceFailureCode.PartialData, PersistenceFailureCategory.Data, SnapshotIntegritySeverity.Error, "Require complete evidence."),
            new PersistenceFailure(PersistenceFailureCode.ObsoleteSchema, PersistenceFailureCategory.Schema, SnapshotIntegritySeverity.Warning, "Check migration manifest."),
            new PersistenceFailure(PersistenceFailureCode.VersionUnknown, PersistenceFailureCategory.Version, SnapshotIntegritySeverity.Critical, "Reject unknown version."),
            new PersistenceFailure(PersistenceFailureCode.ForbiddenRetention, PersistenceFailureCategory.Retention, SnapshotIntegritySeverity.Critical, "Do not store forbidden data."),
            new PersistenceFailure(PersistenceFailureCode.Unknown, PersistenceFailureCategory.Unknown, SnapshotIntegritySeverity.Warning, "Keep visible for QA triage.")
        };
        public IReadOnlyList<PersistenceFailure> Failures => failures.OrderBy(f => f.Code).ToList();
        public PersistenceFailureDiagnostics Map(string signal)
        {
            string s = signal ?? string.Empty;
            if (s.IndexOf("checksum", StringComparison.OrdinalIgnoreCase) >= 0) return new PersistenceFailureDiagnostics(Get(PersistenceFailureCode.InvalidChecksum));
            if (s.IndexOf("migration", StringComparison.OrdinalIgnoreCase) >= 0) return new PersistenceFailureDiagnostics(Get(PersistenceFailureCode.MissingMigration));
            if (s.IndexOf("reference", StringComparison.OrdinalIgnoreCase) >= 0) return new PersistenceFailureDiagnostics(Get(PersistenceFailureCode.DeadReference));
            if (s.IndexOf("partial", StringComparison.OrdinalIgnoreCase) >= 0) return new PersistenceFailureDiagnostics(Get(PersistenceFailureCode.PartialData));
            return new PersistenceFailureDiagnostics(Get(PersistenceFailureCode.Unknown));
        }
        private PersistenceFailure Get(PersistenceFailureCode code) => failures.First(f => f.Code == code);
    }

    public enum SaveLoadQAEvidenceSource { MigrationManifest, SnapshotIntegrity, CompatibilityMatrix, FailureCatalog, Replay, QaReport }
    public enum SaveLoadQAEvidenceVerdict { Linked, Warning, Rejected }
    public sealed class SaveLoadQAEvidence { public SaveLoadQAEvidence(string key, SaveLoadQAEvidenceSource source, string beeSource, SnapshotFamily family, PersistenceFailureCode? failureCode) { Key = key ?? string.Empty; Source = source; BeeSource = beeSource ?? string.Empty; Family = family; FailureCode = failureCode; } public string Key { get; } public SaveLoadQAEvidenceSource Source { get; } public string BeeSource { get; } public SnapshotFamily Family { get; } public PersistenceFailureCode? FailureCode { get; } }
    public sealed class SaveLoadQAEvidenceLink { public SaveLoadQAEvidenceLink(SaveLoadQAEvidence evidence, SaveLoadQAEvidenceVerdict verdict) { Evidence = evidence; Verdict = verdict; } public SaveLoadQAEvidence Evidence { get; } public SaveLoadQAEvidenceVerdict Verdict { get; } }
    public sealed class SaveLoadQAEvidenceDiagnostics { public SaveLoadQAEvidenceDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class SaveLoadQAEvidenceBridge
    {
        public SaveLoadQAEvidenceDiagnostics Diagnostics { get; private set; }
        public IReadOnlyList<SaveLoadQAEvidenceLink> Link(IEnumerable<SaveLoadQAEvidence> evidences)
        {
            var issues = new List<string>();
            var links = new List<SaveLoadQAEvidenceLink>();
            foreach (SaveLoadQAEvidence evidence in evidences ?? Array.Empty<SaveLoadQAEvidence>())
            {
                if (string.IsNullOrWhiteSpace(evidence.BeeSource) || evidence.Family == SnapshotFamily.Unknown) { issues.Add($"Orphan evidence: {evidence.Key}"); continue; }
                if (evidence.Source == SaveLoadQAEvidenceSource.FailureCatalog && !evidence.FailureCode.HasValue) { issues.Add($"Failure code missing: {evidence.Key}"); links.Add(new SaveLoadQAEvidenceLink(evidence, SaveLoadQAEvidenceVerdict.Warning)); continue; }
                links.Add(new SaveLoadQAEvidenceLink(evidence, SaveLoadQAEvidenceVerdict.Linked));
            }
            Diagnostics = new SaveLoadQAEvidenceDiagnostics(issues);
            return links.OrderBy(l => l.Evidence.Key, StringComparer.Ordinal).ToList();
        }
    }

    public enum PersistenceFoundationVerdict { Ready, ReadyWithWarnings, NeedsRevision, Blocked }
    public sealed class PersistenceFoundationCriterion { public PersistenceFoundationCriterion(string name, bool passed, bool warning, bool blocking) { Name = name ?? string.Empty; Passed = passed; Warning = warning; Blocking = blocking; } public string Name { get; } public bool Passed { get; } public bool Warning { get; } public bool Blocking { get; } }
    public sealed class PersistenceFoundationDiagnostics { public PersistenceFoundationDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistenceFoundationReport { public PersistenceFoundationReport(PersistenceFoundationVerdict verdict, IEnumerable<PersistenceFoundationCriterion> criteria) { Verdict = verdict; Criteria = (criteria ?? Array.Empty<PersistenceFoundationCriterion>()).OrderBy(c => c.Name, StringComparer.Ordinal).ToList(); Diagnostics = new PersistenceFoundationDiagnostics(Criteria.Where(c => !c.Passed).Select(c => c.Name).ToList()); } public PersistenceFoundationVerdict Verdict { get; } public IReadOnlyList<PersistenceFoundationCriterion> Criteria { get; } public PersistenceFoundationDiagnostics Diagnostics { get; } }
    public sealed class PersistenceFoundationGate
    {
        public PersistenceFoundationReport Evaluate(IEnumerable<PersistenceFoundationCriterion> criteria, bool bee211Referenced)
        {
            var list = (criteria ?? Array.Empty<PersistenceFoundationCriterion>()).ToList();
            if (bee211Referenced) list.Add(new PersistenceFoundationCriterion("BEE-211-blocked", false, false, true));
            PersistenceFoundationVerdict verdict = list.Any(c => !c.Passed && c.Blocking) ? PersistenceFoundationVerdict.Blocked : list.Any(c => !c.Passed) ? PersistenceFoundationVerdict.NeedsRevision : list.Any(c => c.Warning) ? PersistenceFoundationVerdict.ReadyWithWarnings : PersistenceFoundationVerdict.Ready;
            return new PersistenceFoundationReport(verdict, list);
        }
    }
}
