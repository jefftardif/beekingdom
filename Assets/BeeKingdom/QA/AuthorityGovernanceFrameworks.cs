using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.QA
{
    public enum AuthorityCoverageAxis { Commands, Protocol, Snapshots, Sessions, Prediction, Reconciliation, Demos, QaEvidence }
    public sealed class AuthorityCoverageCell
    {
        public AuthorityCoverageCell(AuthorityCoverageAxis axis, string bee, string contract, string demo, string evidence)
        {
            Axis = axis; Bee = bee ?? string.Empty; Contract = contract ?? string.Empty; Demo = demo ?? string.Empty; Evidence = evidence ?? string.Empty;
        }
        public AuthorityCoverageAxis Axis { get; }
        public string Bee { get; }
        public string Contract { get; }
        public string Demo { get; }
        public string Evidence { get; }
        public bool Covered => !string.IsNullOrWhiteSpace(Evidence);
    }
    public sealed class AuthorityCoverageGap { public AuthorityCoverageGap(AuthorityCoverageAxis axis, string reason) { Axis = axis; Reason = reason ?? string.Empty; } public AuthorityCoverageAxis Axis { get; } public string Reason { get; } }
    public sealed class AuthorityCoverageDiagnostics { public AuthorityCoverageDiagnostics(IReadOnlyList<AuthorityCoverageGap> gaps) { Gaps = gaps ?? Array.Empty<AuthorityCoverageGap>(); } public IReadOnlyList<AuthorityCoverageGap> Gaps { get; } }
    public sealed class AuthorityIntegrationCoverageMatrix
    {
        public AuthorityIntegrationCoverageMatrix(IEnumerable<AuthorityCoverageCell> cells)
        {
            Cells = (cells ?? Array.Empty<AuthorityCoverageCell>()).OrderBy(c => c.Axis).ThenBy(c => c.Bee, StringComparer.Ordinal).ToList();
            Diagnostics = new AuthorityCoverageDiagnostics(Enum.GetValues(typeof(AuthorityCoverageAxis)).Cast<AuthorityCoverageAxis>().Where(axis => Cells.All(c => c.Axis != axis || !c.Covered)).Select(axis => new AuthorityCoverageGap(axis, "No evidence for axis")).ToList());
        }
        public IReadOnlyList<AuthorityCoverageCell> Cells { get; }
        public AuthorityCoverageDiagnostics Diagnostics { get; }
    }

    public enum ServerDemoEvidenceScope { Demo010, Demo011, Demo012, AuthorityTelemetry, Coverage, QaBridge }
    public sealed class ServerDemoEvidenceEntry
    {
        public ServerDemoEvidenceEntry(string id, ServerDemoEvidenceScope scope, string demoSource, string serverSource, bool valid)
        {
            Id = id ?? string.Empty; Scope = scope; DemoSource = demoSource ?? string.Empty; ServerSource = serverSource ?? string.Empty; Valid = valid;
        }
        public string Id { get; }
        public ServerDemoEvidenceScope Scope { get; }
        public string DemoSource { get; }
        public string ServerSource { get; }
        public bool Valid { get; }
    }
    public sealed class ServerDemoEvidenceManifest { public ServerDemoEvidenceManifest(IEnumerable<string> ids) { EntryIds = (ids ?? Array.Empty<string>()).OrderBy(id => id, StringComparer.Ordinal).ToList(); } public IReadOnlyList<string> EntryIds { get; } }
    public sealed class ServerDemoEvidenceDiagnostics { public ServerDemoEvidenceDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class ServerDemoEvidenceBundle
    {
        public ServerDemoEvidenceBundle(IEnumerable<ServerDemoEvidenceEntry> entries, IEnumerable<string> knownGaps)
        {
            var input = (entries ?? Array.Empty<ServerDemoEvidenceEntry>()).ToList();
            Entries = input.Where(e => e.Valid).OrderBy(e => e.Scope).ThenBy(e => e.Id, StringComparer.Ordinal).ToList();
            KnownGaps = (knownGaps ?? Array.Empty<string>()).OrderBy(g => g, StringComparer.Ordinal).ToList();
            Manifest = new ServerDemoEvidenceManifest(Entries.Select(e => e.Id));
            Diagnostics = new ServerDemoEvidenceDiagnostics(input.Where(e => !e.Valid).Select(e => $"Invalid evidence excluded: {e.Id}").Concat(input.Where(e => string.IsNullOrWhiteSpace(e.DemoSource)).Select(e => $"Demo source missing: {e.Id}")).ToList());
        }
        public IReadOnlyList<ServerDemoEvidenceEntry> Entries { get; }
        public IReadOnlyList<string> KnownGaps { get; }
        public ServerDemoEvidenceManifest Manifest { get; }
        public ServerDemoEvidenceDiagnostics Diagnostics { get; }
    }

    public enum MultiplayerRiskStatus { Open, Accepted, Blocked, Resolved }
    public enum MultiplayerRiskSeverity { Low, Medium, High, Critical }
    public sealed class MultiplayerScenarioRisk
    {
        public MultiplayerScenarioRisk(string id, MultiplayerRiskSeverity severity, MultiplayerRiskStatus status, string evidence, string justification, string kind)
        {
            Id = id ?? string.Empty; Severity = severity; Status = status; Evidence = evidence ?? string.Empty; Justification = justification ?? string.Empty; Kind = kind ?? string.Empty;
        }
        public string Id { get; }
        public MultiplayerRiskSeverity Severity { get; }
        public MultiplayerRiskStatus Status { get; }
        public string Evidence { get; }
        public string Justification { get; }
        public string Kind { get; }
    }
    public sealed class MultiplayerRiskDiagnostics { public MultiplayerRiskDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class MultiplayerScenarioRiskRegister
    {
        public MultiplayerScenarioRiskRegister(IEnumerable<MultiplayerScenarioRisk> risks)
        {
            Risks = (risks ?? Array.Empty<MultiplayerScenarioRisk>()).OrderByDescending(r => r.Severity).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
            Diagnostics = new MultiplayerRiskDiagnostics(Risks.Where(r => string.IsNullOrWhiteSpace(r.Evidence) && r.Status != MultiplayerRiskStatus.Accepted).Select(r => $"Risk without evidence: {r.Id}").Concat(Risks.Where(r => r.Status == MultiplayerRiskStatus.Accepted && string.IsNullOrWhiteSpace(r.Justification)).Select(r => $"Accepted risk lacks justification: {r.Id}")).ToList());
        }
        public IReadOnlyList<MultiplayerScenarioRisk> Risks { get; }
        public MultiplayerRiskDiagnostics Diagnostics { get; }
    }

    public enum ContractMigrationSeverity { Info, Warning, NeedsRevision, Blocked }
    public sealed class ContractMigrationRule { public ContractMigrationRule(string name, ContractMigrationSeverity severity) { Name = name ?? string.Empty; Severity = severity; } public string Name { get; } public ContractMigrationSeverity Severity { get; } }
    public sealed class ContractMigrationFinding { public ContractMigrationFinding(string field, ContractMigrationSeverity severity, string reason) { Field = field ?? string.Empty; Severity = severity; Reason = reason ?? string.Empty; } public string Field { get; } public ContractMigrationSeverity Severity { get; } public string Reason { get; } }
    public sealed class ContractMigrationDiagnostics { public ContractMigrationDiagnostics(IReadOnlyList<ContractMigrationFinding> findings) { Findings = findings ?? Array.Empty<ContractMigrationFinding>(); } public IReadOnlyList<ContractMigrationFinding> Findings { get; } }
    public sealed class ContractMigrationGuard
    {
        public ContractMigrationDiagnostics Compare(IReadOnlyDictionary<string, string> oldSchema, IReadOnlyDictionary<string, string> newSchema, bool versionUpdated, bool migrationRefPresent)
        {
            var findings = new List<ContractMigrationFinding>();
            foreach (string key in (oldSchema?.Keys ?? Array.Empty<string>()).Where(k => newSchema == null || !newSchema.ContainsKey(k))) findings.Add(new ContractMigrationFinding(key, ContractMigrationSeverity.Blocked, "Field removed"));
            foreach (string key in (newSchema?.Keys ?? Array.Empty<string>()).Where(k => oldSchema == null || !oldSchema.ContainsKey(k))) findings.Add(new ContractMigrationFinding(key, ContractMigrationSeverity.Warning, "Field added"));
            foreach (string key in (oldSchema?.Keys ?? Array.Empty<string>()).Intersect(newSchema?.Keys ?? Array.Empty<string>()).Where(k => oldSchema[k] != newSchema[k])) findings.Add(new ContractMigrationFinding(key, ContractMigrationSeverity.NeedsRevision, "Field type or enum changed"));
            if (!versionUpdated) findings.Add(new ContractMigrationFinding("version", ContractMigrationSeverity.Blocked, "Contract version missing"));
            if (!migrationRefPresent) findings.Add(new ContractMigrationFinding("migration", ContractMigrationSeverity.NeedsRevision, "Migration reference missing"));
            return new ContractMigrationDiagnostics(findings.OrderByDescending(f => f.Severity).ThenBy(f => f.Field, StringComparer.Ordinal).ToList());
        }
    }

    public enum AuthorityDocumentationStatus { Current, Missing, NeedsUpdate }
    public sealed class AuthorityDocumentationSection { public AuthorityDocumentationSection(string target, string section) { Target = target ?? string.Empty; Section = section ?? string.Empty; } public string Target { get; } public string Section { get; } }
    public sealed class AuthorityDocumentationRule { public AuthorityDocumentationRule(string bee, AuthorityDocumentationSection section, string reason, MultiplayerRiskSeverity severity, AuthorityDocumentationStatus status) { Bee = bee ?? string.Empty; Section = section; Reason = reason ?? string.Empty; Severity = severity; Status = status; } public string Bee { get; } public AuthorityDocumentationSection Section { get; } public string Reason { get; } public MultiplayerRiskSeverity Severity { get; } public AuthorityDocumentationStatus Status { get; } }
    public sealed class AuthorityDocumentationDiagnostics { public AuthorityDocumentationDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class AuthorityDocumentationSyncPlan
    {
        public AuthorityDocumentationSyncPlan(IEnumerable<AuthorityDocumentationRule> rules)
        {
            Rules = (rules ?? Array.Empty<AuthorityDocumentationRule>()).OrderBy(r => r.Bee, StringComparer.Ordinal).ThenBy(r => r.Section?.Target, StringComparer.Ordinal).ToList();
            Diagnostics = new AuthorityDocumentationDiagnostics(Rules.Where(r => r.Status == AuthorityDocumentationStatus.Missing).Select(r => $"Missing section: {r.Section?.Target}/{r.Section?.Section}").ToList());
        }
        public IReadOnlyList<AuthorityDocumentationRule> Rules { get; }
        public AuthorityDocumentationDiagnostics Diagnostics { get; }
    }

    public enum WorkerServerHandoffStatus { Done, Warning, Incomplete, Blocked }
    public enum WorkerServerHandoffVerdict { Ready, ReadyWithWarnings, Incomplete, Blocked }
    public sealed class WorkerServerHandoffItem { public WorkerServerHandoffItem(string name, WorkerServerHandoffStatus status) { Name = name ?? string.Empty; Status = status; } public string Name { get; } public WorkerServerHandoffStatus Status { get; } }
    public sealed class WorkerServerHandoffDiagnostics { public WorkerServerHandoffDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class WorkerServerHandoffChecklist
    {
        public WorkerServerHandoffChecklist(IEnumerable<WorkerServerHandoffItem> items)
        {
            Items = (items ?? Array.Empty<WorkerServerHandoffItem>()).OrderBy(i => i.Name, StringComparer.Ordinal).ToList();
            Verdict = Items.Any(i => i.Status == WorkerServerHandoffStatus.Blocked) ? WorkerServerHandoffVerdict.Blocked : Items.Any(i => i.Status == WorkerServerHandoffStatus.Incomplete) ? WorkerServerHandoffVerdict.Incomplete : Items.Any(i => i.Status == WorkerServerHandoffStatus.Warning) ? WorkerServerHandoffVerdict.ReadyWithWarnings : WorkerServerHandoffVerdict.Ready;
            Diagnostics = new WorkerServerHandoffDiagnostics(Items.Where(i => i.Status != WorkerServerHandoffStatus.Done).Select(i => i.Name).ToList());
        }
        public IReadOnlyList<WorkerServerHandoffItem> Items { get; }
        public WorkerServerHandoffVerdict Verdict { get; }
        public WorkerServerHandoffDiagnostics Diagnostics { get; }
    }

    public enum AuthorityLotReviewVerdict { Approved, ApprovedWithWarnings, NeedsRevision, Blocked }
    public sealed class AuthorityLotReviewFinding { public AuthorityLotReviewFinding(string id, MultiplayerRiskSeverity severity, string message) { Id = id ?? string.Empty; Severity = severity; Message = message ?? string.Empty; } public string Id { get; } public MultiplayerRiskSeverity Severity { get; } public string Message { get; } }
    public sealed class AuthorityLotReviewInput { public AuthorityLotReviewInput(int startBee, int endBee, bool reportsPresent, bool qaRead, bool contiguous, IEnumerable<AuthorityLotReviewFinding> findings) { StartBee = startBee; EndBee = endBee; ReportsPresent = reportsPresent; QaRead = qaRead; Contiguous = contiguous; Findings = findings ?? Array.Empty<AuthorityLotReviewFinding>(); } public int StartBee { get; } public int EndBee { get; } public bool ReportsPresent { get; } public bool QaRead { get; } public bool Contiguous { get; } public IEnumerable<AuthorityLotReviewFinding> Findings { get; } }
    public sealed class AuthorityLotReviewDiagnostics { public AuthorityLotReviewDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class AuthorityLotReview
    {
        public (AuthorityLotReviewVerdict Verdict, AuthorityLotReviewDiagnostics Diagnostics) Review(AuthorityLotReviewInput input)
        {
            var issues = new List<string>();
            if (input == null) return (AuthorityLotReviewVerdict.Blocked, new AuthorityLotReviewDiagnostics(new[] { "Input missing" }));
            if (!input.Contiguous) issues.Add("Lot is not contiguous");
            if (!input.ReportsPresent) issues.Add("Report missing");
            if (!input.QaRead) issues.Add("QA not read");
            bool critical = input.Findings.Any(f => f.Severity == MultiplayerRiskSeverity.Critical);
            AuthorityLotReviewVerdict verdict = critical || !input.Contiguous ? AuthorityLotReviewVerdict.Blocked : issues.Any() ? AuthorityLotReviewVerdict.NeedsRevision : input.Findings.Any() ? AuthorityLotReviewVerdict.ApprovedWithWarnings : AuthorityLotReviewVerdict.Approved;
            return (verdict, new AuthorityLotReviewDiagnostics(issues));
        }
    }

    public enum BetaNetworkVerdict { OnTrack, AtRisk, Blocked, InsufficientEvidence }
    public enum BetaNetworkFactor { Transport, Persistence, Protocol, ServerSimulation, Prediction, Reconciliation, Security, QaCoverage }
    public sealed class BetaNetworkGap { public BetaNetworkGap(BetaNetworkFactor factor, string reason) { Factor = factor; Reason = reason ?? string.Empty; } public BetaNetworkFactor Factor { get; } public string Reason { get; } }
    public sealed class BetaNetworkDiagnostics { public BetaNetworkDiagnostics(IReadOnlyList<BetaNetworkGap> gaps) { Gaps = gaps ?? Array.Empty<BetaNetworkGap>(); } public IReadOnlyList<BetaNetworkGap> Gaps { get; } }
    public sealed class BetaNetworkReadinessProjection
    {
        public BetaNetworkReadinessProjection(BetaNetworkVerdict verdict, IEnumerable<BetaNetworkGap> gaps) { Verdict = verdict; Diagnostics = new BetaNetworkDiagnostics((gaps ?? Array.Empty<BetaNetworkGap>()).OrderBy(g => g.Factor).ToList()); }
        public BetaNetworkVerdict Verdict { get; }
        public BetaNetworkDiagnostics Diagnostics { get; }
        public static BetaNetworkReadinessProjection Evaluate(bool coverageComplete, bool transportPresent, bool criticalRisk, bool sufficientEvidence)
        {
            var gaps = new List<BetaNetworkGap>();
            if (!transportPresent) gaps.Add(new BetaNetworkGap(BetaNetworkFactor.Transport, "Transport absent"));
            if (!coverageComplete) gaps.Add(new BetaNetworkGap(BetaNetworkFactor.QaCoverage, "Coverage incomplete"));
            return new BetaNetworkReadinessProjection(criticalRisk ? BetaNetworkVerdict.Blocked : !sufficientEvidence ? BetaNetworkVerdict.InsufficientEvidence : gaps.Any() ? BetaNetworkVerdict.AtRisk : BetaNetworkVerdict.OnTrack, gaps);
        }
    }

    public enum CommercialRiskVerdict { Acceptable, Watch, AtRisk, Blocked }
    public sealed class CommercialRiskCriterion { public CommercialRiskCriterion(string name, CommercialRiskVerdict verdict, string riskRegisterLink) { Name = name ?? string.Empty; Verdict = verdict; RiskRegisterLink = riskRegisterLink ?? string.Empty; } public string Name { get; } public CommercialRiskVerdict Verdict { get; } public string RiskRegisterLink { get; } }
    public sealed class CommercialRiskFinding { public CommercialRiskFinding(string name, CommercialRiskVerdict verdict) { Name = name ?? string.Empty; Verdict = verdict; } public string Name { get; } public CommercialRiskVerdict Verdict { get; } }
    public sealed class CommercialRiskDiagnostics { public CommercialRiskDiagnostics(IReadOnlyList<CommercialRiskFinding> findings) { Findings = findings ?? Array.Empty<CommercialRiskFinding>(); } public IReadOnlyList<CommercialRiskFinding> Findings { get; } }
    public sealed class AuthorityCommercialRiskGate
    {
        public CommercialRiskDiagnostics Diagnostics { get; private set; }
        public CommercialRiskVerdict Evaluate(IEnumerable<CommercialRiskCriterion> criteria)
        {
            var findings = (criteria ?? Array.Empty<CommercialRiskCriterion>()).Select(c => new CommercialRiskFinding(c.Name, c.Verdict)).OrderByDescending(f => f.Verdict).ThenBy(f => f.Name, StringComparer.Ordinal).ToList();
            Diagnostics = new CommercialRiskDiagnostics(findings);
            return findings.Any(f => f.Verdict == CommercialRiskVerdict.Blocked) ? CommercialRiskVerdict.Blocked : findings.Any(f => f.Verdict == CommercialRiskVerdict.AtRisk) ? CommercialRiskVerdict.AtRisk : findings.Any(f => f.Verdict == CommercialRiskVerdict.Watch) ? CommercialRiskVerdict.Watch : CommercialRiskVerdict.Acceptable;
        }
    }

    public enum AuthorityClosureVerdict { Closed, ClosedWithWarnings, NeedsRevision, Blocked }
    public sealed class AuthorityClosureCriterion { public AuthorityClosureCriterion(string name, bool passed, bool warning, bool blocking) { Name = name ?? string.Empty; Passed = passed; Warning = warning; Blocking = blocking; } public string Name { get; } public bool Passed { get; } public bool Warning { get; } public bool Blocking { get; } }
    public sealed class AuthorityClosureDiagnostics { public AuthorityClosureDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class AuthorityClosureReport { public AuthorityClosureReport(AuthorityClosureVerdict verdict, IEnumerable<AuthorityClosureCriterion> criteria) { Verdict = verdict; Criteria = (criteria ?? Array.Empty<AuthorityClosureCriterion>()).OrderBy(c => c.Name, StringComparer.Ordinal).ToList(); Diagnostics = new AuthorityClosureDiagnostics(Criteria.Where(c => !c.Passed).Select(c => c.Name).ToList()); } public AuthorityClosureVerdict Verdict { get; } public IReadOnlyList<AuthorityClosureCriterion> Criteria { get; } public AuthorityClosureDiagnostics Diagnostics { get; } }
    public sealed class AuthorityReadinessClosureGate
    {
        public AuthorityClosureReport Evaluate(IEnumerable<AuthorityClosureCriterion> criteria, bool bee201AppearedBeforeValidation)
        {
            var list = (criteria ?? Array.Empty<AuthorityClosureCriterion>()).ToList();
            if (bee201AppearedBeforeValidation) list.Add(new AuthorityClosureCriterion("BEE-201-before-validation", false, false, true));
            AuthorityClosureVerdict verdict = list.Any(c => !c.Passed && c.Blocking) ? AuthorityClosureVerdict.Blocked : list.Any(c => !c.Passed) ? AuthorityClosureVerdict.NeedsRevision : list.Any(c => c.Warning) ? AuthorityClosureVerdict.ClosedWithWarnings : AuthorityClosureVerdict.Closed;
            return new AuthorityClosureReport(verdict, list);
        }
    }
}
