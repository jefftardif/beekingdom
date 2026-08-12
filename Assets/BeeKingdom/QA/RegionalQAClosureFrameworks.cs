using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.QA
{
    public enum RegionalEvidenceType { Replay, Benchmark, Demo, Blocker, History, MilestoneGate }
    public enum RegionalEvidenceVerdict { Unknown, Passed, Warning, Failed }
    public enum RegionalValidationCoverageAxis { BeeSource, Domain, Region, Demo, EvidenceType, Verdict, Milestone }
    public enum RegionalEvidenceBundleScopeKind { Region, BeeLot, Demo, Milestone, LogicalRun }
    public enum RegionalQADependencyNodeKind { Bee, Evidence, Requirement, Demo, Benchmark, Blocker, ReadinessScore, MilestoneGate }
    public enum RegionalQADependencyEdgeKind { Requires, Satisfies, Blocks, Informs, DerivedFrom, DisplayedBy }
    public enum RegionalRiskStatus { Open, Accepted, Blocked, Resolved, Deferred }
    public enum RegionalRiskSeverity { Info, Low, Medium, High, Critical }
    public enum RegionalDocumentationSectionKind { RegionalArchitecture, Replay, Benchmark, QAEvidence, DemoReadModel, Risks, MilestoneGate, WorkerHandoff, ServerImpact }
    public enum RegionalDocumentationSyncStatus { Required, Satisfied, Missing, NotApplicable }
    public enum RegionalArchitectureComplianceVerdict { Compliant, Warning, BlockingViolation, Information }
    public enum RegionalWorkerHandoffStatus { Ready, ReadyWithQuestions, Blocked, Incomplete }
    public enum RegionalLotReviewVerdict { Approved, ApprovedWithWarnings, NeedsRevision, Blocked }
    public enum RegionalAlphaProjectionVerdict { OnTrack, AtRisk, Blocked, InsufficientEvidence }
    public enum RegionalWorldExecutionClosureVerdict { Closed, ClosedWithWarnings, NeedsRevision, Blocked }

    public sealed class RegionalEvidenceRecord
    {
        public int BeeSource { get; }
        public string Domain { get; }
        public string Region { get; }
        public string DemoId { get; }
        public RegionalEvidenceType Type { get; }
        public RegionalEvidenceVerdict Verdict { get; }
        public bool IsValid { get; }
        public string EvidenceId { get; }
        public RegionalEvidenceRecord(string evidenceId, int beeSource, string domain, string region, string demoId, RegionalEvidenceType type, RegionalEvidenceVerdict verdict, bool isValid)
        {
            EvidenceId = Required(evidenceId);
            BeeSource = beeSource;
            Domain = domain ?? string.Empty;
            Region = region ?? string.Empty;
            DemoId = demoId ?? string.Empty;
            Type = type;
            Verdict = verdict;
            IsValid = isValid;
        }

        internal static string Required(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Id is required.");
            return value;
        }
    }

    public sealed class RegionalValidationCoverageCell
    {
        public int BeeSource { get; }
        public string Domain { get; }
        public RegionalEvidenceType EvidenceType { get; }
        public bool Covered { get; }
        public string EvidenceId { get; }
        public RegionalValidationCoverageCell(int beeSource, string domain, RegionalEvidenceType evidenceType, bool covered, string evidenceId)
        {
            BeeSource = beeSource;
            Domain = domain ?? string.Empty;
            EvidenceType = evidenceType;
            Covered = covered;
            EvidenceId = evidenceId ?? string.Empty;
        }
    }

    public sealed class RegionalValidationCoverageGap
    {
        public int BeeSource { get; }
        public string Domain { get; }
        public RegionalEvidenceType MissingEvidenceType { get; }
        public RegionalValidationCoverageGap(int beeSource, string domain, RegionalEvidenceType missingEvidenceType) { BeeSource = beeSource; Domain = domain ?? string.Empty; MissingEvidenceType = missingEvidenceType; }
    }

    public sealed class RegionalValidationCoverageMatrix
    {
        public IReadOnlyList<RegionalValidationCoverageCell> Cells { get; }
        public IReadOnlyList<RegionalValidationCoverageGap> Gaps { get; }
        public RegionalValidationCoverageMatrix(IReadOnlyList<RegionalValidationCoverageCell> cells, IReadOnlyList<RegionalValidationCoverageGap> gaps)
        {
            Cells = new List<RegionalValidationCoverageCell>(cells ?? Array.Empty<RegionalValidationCoverageCell>()).AsReadOnly();
            Gaps = new List<RegionalValidationCoverageGap>(gaps ?? Array.Empty<RegionalValidationCoverageGap>()).AsReadOnly();
        }

        public static RegionalValidationCoverageMatrix Build(IEnumerable<RegionalEvidenceRecord> evidences, IEnumerable<Tuple<int, string, RegionalEvidenceType>> expected)
        {
            List<RegionalEvidenceRecord> evidenceList = (evidences ?? Array.Empty<RegionalEvidenceRecord>()).OrderBy(e => e.BeeSource).ThenBy(e => e.Domain).ThenBy(e => e.Type).ToList();
            List<RegionalValidationCoverageCell> cells = evidenceList.Select(e => new RegionalValidationCoverageCell(e.BeeSource, e.Domain, e.Type, e.IsValid, e.EvidenceId)).ToList();
            List<RegionalValidationCoverageGap> gaps = new List<RegionalValidationCoverageGap>();
            foreach (Tuple<int, string, RegionalEvidenceType> item in expected ?? Array.Empty<Tuple<int, string, RegionalEvidenceType>>())
            {
                if (!cells.Any(c => c.BeeSource == item.Item1 && c.Domain == item.Item2 && c.EvidenceType == item.Item3 && c.Covered))
                {
                    gaps.Add(new RegionalValidationCoverageGap(item.Item1, item.Item2, item.Item3));
                }
            }

            return new RegionalValidationCoverageMatrix(cells.OrderBy(c => c.BeeSource).ThenBy(c => c.Domain).ThenBy(c => c.EvidenceType.ToString()).ToList(), gaps.OrderBy(g => g.BeeSource).ThenBy(g => g.Domain).ThenBy(g => g.MissingEvidenceType.ToString()).ToList());
        }
    }

    public sealed class RegionalValidationCoverageDiagnostics { public int GapCount { get; private set; } public void Capture(RegionalValidationCoverageMatrix matrix) { GapCount = matrix?.Gaps.Count ?? 0; } }

    public sealed class RegionalEvidenceBundleScope
    {
        public RegionalEvidenceBundleScopeKind Kind { get; }
        public int StartBee { get; }
        public int EndBee { get; }
        public string Id { get; }
        public RegionalEvidenceBundleScope(RegionalEvidenceBundleScopeKind kind, string id, int startBee = 0, int endBee = 0) { Kind = kind; Id = id ?? string.Empty; StartBee = startBee; EndBee = endBee; }
        public bool Includes(RegionalEvidenceRecord evidence) { return Kind != RegionalEvidenceBundleScopeKind.BeeLot || (evidence.BeeSource >= StartBee && evidence.BeeSource <= EndBee); }
    }

    public sealed class RegionalEvidenceBundleEntry { public string EvidenceId { get; } public int BeeSource { get; } public RegionalEvidenceType Type { get; } public RegionalEvidenceBundleEntry(RegionalEvidenceRecord evidence) { EvidenceId = evidence.EvidenceId; BeeSource = evidence.BeeSource; Type = evidence.Type; } }
    public sealed class RegionalEvidenceBundleManifest { public RegionalEvidenceBundleScope Scope { get; } public int EntryCount { get; } public RegionalEvidenceBundleManifest(RegionalEvidenceBundleScope scope, int entryCount) { Scope = scope; EntryCount = entryCount; } }
    public sealed class RegionalEvidenceBundle { public RegionalEvidenceBundleManifest Manifest { get; } public IReadOnlyList<RegionalEvidenceBundleEntry> Entries { get; } public IReadOnlyList<RegionalValidationCoverageGap> Gaps { get; } public RegionalEvidenceBundle(RegionalEvidenceBundleManifest manifest, IReadOnlyList<RegionalEvidenceBundleEntry> entries, IReadOnlyList<RegionalValidationCoverageGap> gaps) { Manifest = manifest; Entries = new List<RegionalEvidenceBundleEntry>(entries ?? Array.Empty<RegionalEvidenceBundleEntry>()).AsReadOnly(); Gaps = new List<RegionalValidationCoverageGap>(gaps ?? Array.Empty<RegionalValidationCoverageGap>()).AsReadOnly(); } }
    public sealed class RegionalEvidenceBundleDiagnostics { public int ExcludedInvalidEvidence { get; private set; } public void RecordExcluded() { ExcludedInvalidEvidence++; } }
    public sealed class RegionalEvidenceBundleBuilder
    {
        private readonly RegionalEvidenceBundleDiagnostics diagnostics;
        public RegionalEvidenceBundleBuilder(RegionalEvidenceBundleDiagnostics diagnostics = null) { this.diagnostics = diagnostics ?? new RegionalEvidenceBundleDiagnostics(); }
        public RegionalEvidenceBundle Build(RegionalEvidenceBundleScope scope, IEnumerable<RegionalEvidenceRecord> evidences, RegionalValidationCoverageMatrix matrix)
        {
            List<RegionalEvidenceBundleEntry> entries = new List<RegionalEvidenceBundleEntry>();
            foreach (RegionalEvidenceRecord evidence in (evidences ?? Array.Empty<RegionalEvidenceRecord>()).Where(scope.Includes).OrderBy(e => e.BeeSource).ThenBy(e => e.EvidenceId))
            {
                if (!evidence.IsValid) { diagnostics.RecordExcluded(); continue; }
                entries.Add(new RegionalEvidenceBundleEntry(evidence));
            }
            return new RegionalEvidenceBundle(new RegionalEvidenceBundleManifest(scope, entries.Count), entries, matrix?.Gaps ?? Array.Empty<RegionalValidationCoverageGap>());
        }
    }

    public sealed class RegionalQADependencyNode { public string NodeId { get; } public RegionalQADependencyNodeKind Kind { get; } public RegionalQADependencyNode(string nodeId, RegionalQADependencyNodeKind kind) { NodeId = RegionalEvidenceRecord.Required(nodeId); Kind = kind; } }
    public sealed class RegionalQADependencyEdge { public string FromNodeId { get; } public string ToNodeId { get; } public RegionalQADependencyEdgeKind Kind { get; } public RegionalQADependencyEdge(string fromNodeId, string toNodeId, RegionalQADependencyEdgeKind kind) { FromNodeId = RegionalEvidenceRecord.Required(fromNodeId); ToNodeId = RegionalEvidenceRecord.Required(toNodeId); Kind = kind; } }
    public sealed class RegionalQADependencyGraphDiagnostics { public int MissingDependencyCount { get; private set; } public int CycleCount { get; private set; } public int OrphanCount { get; private set; } public void Missing() { MissingDependencyCount++; } public void Cycle() { CycleCount++; } public void Orphan() { OrphanCount++; } }
    public sealed class RegionalQADependencyGraph
    {
        public IReadOnlyList<RegionalQADependencyNode> Nodes { get; }
        public IReadOnlyList<RegionalQADependencyEdge> Edges { get; }
        public RegionalQADependencyGraphDiagnostics Diagnostics { get; }
        public RegionalQADependencyGraph(IEnumerable<RegionalQADependencyNode> nodes, IEnumerable<RegionalQADependencyEdge> edges, RegionalQADependencyGraphDiagnostics diagnostics = null)
        {
            Diagnostics = diagnostics ?? new RegionalQADependencyGraphDiagnostics();
            List<RegionalQADependencyNode> nodeList = (nodes ?? Array.Empty<RegionalQADependencyNode>()).OrderBy(n => n.NodeId).ToList();
            HashSet<string> ids = new HashSet<string>(nodeList.Select(n => n.NodeId));
            List<RegionalQADependencyEdge> edgeList = new List<RegionalQADependencyEdge>();
            foreach (RegionalQADependencyEdge edge in edges ?? Array.Empty<RegionalQADependencyEdge>())
            {
                if (!ids.Contains(edge.FromNodeId) || !ids.Contains(edge.ToNodeId)) { Diagnostics.Missing(); continue; }
                edgeList.Add(edge);
            }
            Nodes = nodeList.AsReadOnly();
            Edges = edgeList.OrderBy(e => e.FromNodeId).ThenBy(e => e.ToNodeId).ThenBy(e => e.Kind.ToString()).ToList().AsReadOnly();
            DetectOrphans();
            DetectCycles();
        }
        private void DetectOrphans() { foreach (RegionalQADependencyNode node in Nodes) if (!Edges.Any(e => e.FromNodeId == node.NodeId || e.ToNodeId == node.NodeId)) Diagnostics.Orphan(); }
        private void DetectCycles() { foreach (RegionalQADependencyNode node in Nodes) if (HasPath(node.NodeId, node.NodeId, new HashSet<string>(), true)) { Diagnostics.Cycle(); return; } }
        private bool HasPath(string start, string target, HashSet<string> visited, bool first) { if (!first && start == target) return true; if (!visited.Add(start)) return false; foreach (RegionalQADependencyEdge edge in Edges.Where(e => e.FromNodeId == start)) if (HasPath(edge.ToNodeId, target, visited, false)) return true; return false; }
    }

    public sealed class RegionalRiskMitigation { public string Strategy { get; } public string ResolutionCondition { get; } public RegionalRiskMitigation(string strategy, string resolutionCondition) { Strategy = strategy ?? string.Empty; ResolutionCondition = resolutionCondition ?? string.Empty; } }
    public sealed class RegionalRisk
    {
        public string RiskId { get; } public int BeeSource { get; } public RegionalRiskStatus Status { get; } public RegionalRiskSeverity Severity { get; } public string Justification { get; } public string Cause { get; } public string Impact { get; } public RegionalRiskMitigation Mitigation { get; }
        public RegionalRisk(string riskId, int beeSource, RegionalRiskStatus status, RegionalRiskSeverity severity, string justification, string cause, string impact, RegionalRiskMitigation mitigation)
        {
            if (beeSource <= 0) throw new ArgumentException("BEE source is required.");
            if (status == RegionalRiskStatus.Accepted && string.IsNullOrWhiteSpace(justification)) throw new ArgumentException("Accepted risk requires justification.");
            RiskId = RegionalEvidenceRecord.Required(riskId); BeeSource = beeSource; Status = status; Severity = severity; Justification = justification ?? string.Empty; Cause = cause ?? string.Empty; Impact = impact ?? string.Empty; Mitigation = mitigation ?? new RegionalRiskMitigation(string.Empty, string.Empty);
        }
    }
    public sealed class RegionalRiskDiagnostics { public int RiskCount { get; private set; } public void RecordRisk() { RiskCount++; } }
    public sealed class RegionalRiskRegister
    {
        private readonly List<RegionalRisk> risks = new List<RegionalRisk>();
        public RegionalRiskDiagnostics Diagnostics { get; } = new RegionalRiskDiagnostics();
        public void RegisterRisk(RegionalRisk risk) { risks.Add(risk); Diagnostics.RecordRisk(); }
        public RegionalRisk RegisterBlockedFromGraph(string riskId, int beeSource, RegionalRiskSeverity severity, string graphNodeId) { RegionalRisk risk = new RegionalRisk(riskId, beeSource, RegionalRiskStatus.Blocked, severity, "Blocked by graph node " + graphNodeId, "dependency", "blocked validation", new RegionalRiskMitigation("resolve blocker", graphNodeId)); RegisterRisk(risk); return risk; }
        public IReadOnlyList<RegionalRisk> QueryRisks() { return risks.OrderByDescending(r => r.Severity).ThenBy(r => r.BeeSource).ThenBy(r => r.RiskId).ToList(); }
    }

    public sealed class RegionalDocumentationSection { public RegionalDocumentationSectionKind Kind { get; } public string DocumentId { get; } public RegionalDocumentationSection(RegionalDocumentationSectionKind kind, string documentId) { Kind = kind; DocumentId = documentId ?? string.Empty; } }
    public sealed class RegionalDocumentationSyncRule { public string RuleId { get; } public string Domain { get; } public RegionalDocumentationSectionKind SectionKind { get; } public bool ServerImpactRequired { get; } public RegionalDocumentationSyncRule(string ruleId, string domain, RegionalDocumentationSectionKind sectionKind, bool serverImpactRequired = false) { RuleId = RegionalEvidenceRecord.Required(ruleId); Domain = domain ?? string.Empty; SectionKind = sectionKind; ServerImpactRequired = serverImpactRequired; } }
    public sealed class RegionalDocumentationSyncObligation { public int BeeSource { get; } public RegionalDocumentationSectionKind SectionKind { get; } public string Reason { get; } public RegionalDocumentationSyncStatus Status { get; } public RegionalDocumentationSyncObligation(int beeSource, RegionalDocumentationSectionKind sectionKind, string reason, RegionalDocumentationSyncStatus status) { BeeSource = beeSource; SectionKind = sectionKind; Reason = reason ?? string.Empty; Status = status; } }
    public sealed class RegionalDocumentationSyncDiagnostics { public int MissingSections { get; private set; } public void RecordMissing() { MissingSections++; } }
    public sealed class RegionalDocumentationSyncPlan { public IReadOnlyList<RegionalDocumentationSyncObligation> Obligations { get; } public RegionalDocumentationSyncPlan(IReadOnlyList<RegionalDocumentationSyncObligation> obligations) { Obligations = new List<RegionalDocumentationSyncObligation>(obligations ?? Array.Empty<RegionalDocumentationSyncObligation>()).OrderBy(o => o.BeeSource).ThenBy(o => o.SectionKind.ToString()).ToList().AsReadOnly(); } }
    public sealed class RegionalDocumentationSyncPlanner
    {
        public RegionalDocumentationSyncPlan Build(IEnumerable<RegionalDocumentationSyncRule> rules, IEnumerable<RegionalEvidenceRecord> evidences, RegionalDocumentationSyncDiagnostics diagnostics = null)
        {
            List<RegionalDocumentationSyncObligation> obligations = new List<RegionalDocumentationSyncObligation>();
            foreach (RegionalEvidenceRecord evidence in evidences ?? Array.Empty<RegionalEvidenceRecord>())
            {
                foreach (RegionalDocumentationSyncRule rule in (rules ?? Array.Empty<RegionalDocumentationSyncRule>()).Where(r => r.Domain == evidence.Domain))
                {
                    obligations.Add(new RegionalDocumentationSyncObligation(evidence.BeeSource, rule.SectionKind, "Evidence " + evidence.EvidenceId, RegionalDocumentationSyncStatus.Required));
                }
            }
            if (obligations.Count == 0) diagnostics?.RecordMissing();
            return new RegionalDocumentationSyncPlan(obligations);
        }
    }

    public sealed class RegionalArchitectureComplianceRule { public string RuleId { get; } public RegionalArchitectureComplianceRule(string ruleId) { RuleId = RegionalEvidenceRecord.Required(ruleId); } }
    public sealed class RegionalArchitectureViolation { public string RuleId { get; } public RegionalArchitectureComplianceVerdict Verdict { get; } public string Reason { get; } public RegionalArchitectureViolation(string ruleId, RegionalArchitectureComplianceVerdict verdict, string reason) { RuleId = ruleId; Verdict = verdict; Reason = reason ?? string.Empty; } }
    public sealed class RegionalArchitectureComplianceCheck { public int BeeSource { get; } public bool CreatesScene { get; } public bool CreatesServer { get; } public bool MentionsFutureService { get; } public bool UsesWallClockBenchmark { get; } public bool CreatesPromptQA { get; } public bool DeclaresRelease { get; } public RegionalArchitectureComplianceCheck(int beeSource, bool createsScene = false, bool createsServer = false, bool mentionsFutureService = false, bool usesWallClockBenchmark = false, bool createsPromptQA = false, bool declaresRelease = false) { BeeSource = beeSource; CreatesScene = createsScene; CreatesServer = createsServer; MentionsFutureService = mentionsFutureService; UsesWallClockBenchmark = usesWallClockBenchmark; CreatesPromptQA = createsPromptQA; DeclaresRelease = declaresRelease; } }
    public sealed class RegionalArchitectureComplianceResult { public RegionalArchitectureComplianceVerdict Verdict { get; } public IReadOnlyList<RegionalArchitectureViolation> Violations { get; } public RegionalArchitectureComplianceResult(IReadOnlyList<RegionalArchitectureViolation> violations) { Violations = new List<RegionalArchitectureViolation>(violations ?? Array.Empty<RegionalArchitectureViolation>()).OrderBy(v => v.RuleId).ToList().AsReadOnly(); Verdict = Violations.Any(v => v.Verdict == RegionalArchitectureComplianceVerdict.BlockingViolation) ? RegionalArchitectureComplianceVerdict.BlockingViolation : Violations.Any(v => v.Verdict == RegionalArchitectureComplianceVerdict.Warning) ? RegionalArchitectureComplianceVerdict.Warning : RegionalArchitectureComplianceVerdict.Compliant; } }
    public sealed class RegionalArchitectureComplianceDiagnostics { public int ViolationCount { get; private set; } public void Capture(RegionalArchitectureComplianceResult result) { ViolationCount = result?.Violations.Count ?? 0; } }
    public sealed class RegionalArchitectureComplianceValidator
    {
        public RegionalArchitectureComplianceResult Validate(RegionalArchitectureComplianceCheck check)
        {
            List<RegionalArchitectureViolation> v = new List<RegionalArchitectureViolation>();
            if (check.CreatesScene) v.Add(new RegionalArchitectureViolation("no-scene-creation", RegionalArchitectureComplianceVerdict.BlockingViolation, "Engine BEE creates a scene."));
            if (check.CreatesServer) v.Add(new RegionalArchitectureViolation("no-server-without-server-bee", RegionalArchitectureComplianceVerdict.BlockingViolation, "Server code requires SERVER BEE."));
            if (check.UsesWallClockBenchmark) v.Add(new RegionalArchitectureViolation("no-wall-clock-benchmark", RegionalArchitectureComplianceVerdict.BlockingViolation, "Benchmark is not deterministic."));
            if (check.CreatesPromptQA) v.Add(new RegionalArchitectureViolation("no-prompt-qa-creation", RegionalArchitectureComplianceVerdict.BlockingViolation, "prompt_qa cannot be created here."));
            if (check.DeclaresRelease) v.Add(new RegionalArchitectureViolation("no-release-declaration", RegionalArchitectureComplianceVerdict.BlockingViolation, "Regional BEE cannot declare release."));
            if (check.MentionsFutureService) v.Add(new RegionalArchitectureViolation("future-service-impact", RegionalArchitectureComplianceVerdict.Warning, "Future service impact only."));
            return new RegionalArchitectureComplianceResult(v);
        }
    }

    public sealed class RegionalWorkerHandoffItem { public string ItemId { get; } public RegionalWorkerHandoffStatus Status { get; } public string Justification { get; } public RegionalWorkerHandoffItem(string itemId, RegionalWorkerHandoffStatus status, string justification) { ItemId = RegionalEvidenceRecord.Required(itemId); Status = status; Justification = justification ?? string.Empty; } }
    public sealed class RegionalWorkerHandoffChecklist { public int BeeSource { get; } public IReadOnlyList<RegionalWorkerHandoffItem> Items { get; } public RegionalWorkerHandoffStatus Verdict { get; } public RegionalWorkerHandoffChecklist(int beeSource, IReadOnlyList<RegionalWorkerHandoffItem> items) { BeeSource = beeSource; Items = new List<RegionalWorkerHandoffItem>(items ?? Array.Empty<RegionalWorkerHandoffItem>()).OrderBy(i => i.ItemId).ToList().AsReadOnly(); Verdict = Items.Any(i => i.Status == RegionalWorkerHandoffStatus.Blocked) ? RegionalWorkerHandoffStatus.Blocked : Items.Any(i => i.Status == RegionalWorkerHandoffStatus.Incomplete) ? RegionalWorkerHandoffStatus.Incomplete : Items.Any(i => i.Status == RegionalWorkerHandoffStatus.ReadyWithQuestions) ? RegionalWorkerHandoffStatus.ReadyWithQuestions : RegionalWorkerHandoffStatus.Ready; } }
    public sealed class RegionalWorkerHandoffDiagnostics { public int BlockedChecklists { get; private set; } public void Capture(RegionalWorkerHandoffChecklist checklist) { if (checklist.Verdict == RegionalWorkerHandoffStatus.Blocked) BlockedChecklists++; } }
    public sealed class RegionalWorkerHandoffBuilder { public RegionalWorkerHandoffChecklist Build(int beeSource, IEnumerable<RegionalWorkerHandoffItem> items, RegionalArchitectureComplianceResult compliance) { List<RegionalWorkerHandoffItem> list = new List<RegionalWorkerHandoffItem>(items ?? Array.Empty<RegionalWorkerHandoffItem>()); if (compliance != null && compliance.Verdict == RegionalArchitectureComplianceVerdict.BlockingViolation) list.Add(new RegionalWorkerHandoffItem("architecture", RegionalWorkerHandoffStatus.Blocked, "Blocking violation exists.")); return new RegionalWorkerHandoffChecklist(beeSource, list); } }

    public sealed class RegionalLotReviewFinding { public RegionalRiskSeverity Severity { get; } public string Message { get; } public RegionalLotReviewFinding(RegionalRiskSeverity severity, string message) { Severity = severity; Message = message ?? string.Empty; } }
    public sealed class RegionalLotReviewInput { public int StartBee { get; } public int EndBee { get; } public IReadOnlyList<int> PresentReports { get; } public IReadOnlyList<RegionalWorkerHandoffChecklist> Checklists { get; } public IReadOnlyList<RegionalArchitectureViolation> Violations { get; } public IReadOnlyList<RegionalRisk> Risks { get; } public RegionalLotReviewInput(int startBee, int endBee, IReadOnlyList<int> presentReports, IReadOnlyList<RegionalWorkerHandoffChecklist> checklists, IReadOnlyList<RegionalArchitectureViolation> violations, IReadOnlyList<RegionalRisk> risks) { StartBee = startBee; EndBee = endBee; PresentReports = presentReports ?? Array.Empty<int>(); Checklists = checklists ?? Array.Empty<RegionalWorkerHandoffChecklist>(); Violations = violations ?? Array.Empty<RegionalArchitectureViolation>(); Risks = risks ?? Array.Empty<RegionalRisk>(); } }
    public sealed class RegionalLotReview { public RegionalLotReviewVerdict Verdict { get; } public IReadOnlyList<RegionalLotReviewFinding> Findings { get; } public RegionalLotReview(RegionalLotReviewVerdict verdict, IReadOnlyList<RegionalLotReviewFinding> findings) { Verdict = verdict; Findings = new List<RegionalLotReviewFinding>(findings ?? Array.Empty<RegionalLotReviewFinding>()).OrderByDescending(f => f.Severity).ThenBy(f => f.Message).ToList().AsReadOnly(); } }
    public sealed class RegionalLotReviewDiagnostics { public int ReviewCount { get; private set; } public void Record() { ReviewCount++; } }
    public sealed class RegionalLotReviewer
    {
        public RegionalLotReview Review(RegionalLotReviewInput input)
        {
            List<RegionalLotReviewFinding> findings = new List<RegionalLotReviewFinding>();
            if (input.EndBee - input.StartBee != 9) findings.Add(new RegionalLotReviewFinding(RegionalRiskSeverity.Critical, "Lot must contain exactly 10 contiguous BEE."));
            for (int i = input.StartBee; i <= input.EndBee; i++) if (!input.PresentReports.Contains(i)) findings.Add(new RegionalLotReviewFinding(RegionalRiskSeverity.High, "Missing report BEE-" + i.ToString("D3")));
            if (input.Violations.Any(v => v.Verdict == RegionalArchitectureComplianceVerdict.BlockingViolation)) findings.Add(new RegionalLotReviewFinding(RegionalRiskSeverity.Critical, "Blocking architecture violation."));
            if (input.Risks.Any(r => r.Status == RegionalRiskStatus.Open && r.Severity >= RegionalRiskSeverity.High)) findings.Add(new RegionalLotReviewFinding(RegionalRiskSeverity.High, "Open high risk."));
            RegionalLotReviewVerdict verdict = findings.Any(f => f.Severity == RegionalRiskSeverity.Critical) ? RegionalLotReviewVerdict.Blocked : findings.Any(f => f.Message.StartsWith("Missing", StringComparison.Ordinal)) ? RegionalLotReviewVerdict.NeedsRevision : findings.Count > 0 ? RegionalLotReviewVerdict.ApprovedWithWarnings : RegionalLotReviewVerdict.Approved;
            return new RegionalLotReview(verdict, findings);
        }
    }

    public sealed class RegionalAlphaProjectionFactor { public string FactorId { get; } public double Contribution { get; } public string Justification { get; } public string ExpectedAction { get; } public RegionalAlphaProjectionFactor(string factorId, double contribution, string justification, string expectedAction) { FactorId = RegionalEvidenceRecord.Required(factorId); Contribution = contribution < 0d ? 0d : contribution > 1d ? 1d : contribution; Justification = justification ?? string.Empty; ExpectedAction = expectedAction ?? string.Empty; } }
    public sealed class RegionalAlphaProjectionGap { public string GapId { get; } public string Action { get; } public RegionalAlphaProjectionGap(string gapId, string action) { GapId = RegionalEvidenceRecord.Required(gapId); Action = action ?? string.Empty; } }
    public sealed class RegionalAlphaReadinessProjection { public RegionalAlphaProjectionVerdict Verdict { get; } public IReadOnlyList<RegionalAlphaProjectionFactor> Factors { get; } public IReadOnlyList<RegionalAlphaProjectionGap> Gaps { get; } public RegionalAlphaReadinessProjection(RegionalAlphaProjectionVerdict verdict, IReadOnlyList<RegionalAlphaProjectionFactor> factors, IReadOnlyList<RegionalAlphaProjectionGap> gaps) { Verdict = verdict; Factors = new List<RegionalAlphaProjectionFactor>(factors ?? Array.Empty<RegionalAlphaProjectionFactor>()).OrderBy(f => f.FactorId).ToList().AsReadOnly(); Gaps = new List<RegionalAlphaProjectionGap>(gaps ?? Array.Empty<RegionalAlphaProjectionGap>()).OrderBy(g => g.GapId).ToList().AsReadOnly(); } }
    public sealed class RegionalAlphaProjectionDiagnostics { public int ProjectionCount { get; private set; } public void Record() { ProjectionCount++; } }
    public sealed class RegionalAlphaReadinessProjector
    {
        public RegionalAlphaReadinessProjection Project(bool milestoneReady, RegionalLotReview lotReview, IEnumerable<RegionalRisk> risks, RegionalValidationCoverageMatrix coverage, RegionalWorkerHandoffChecklist handoff, RegionalEvidenceBundle bundle)
        {
            List<RegionalAlphaProjectionFactor> factors = new List<RegionalAlphaProjectionFactor> { new RegionalAlphaProjectionFactor("milestone", milestoneReady ? 1d : 0d, "Regional milestone gate", "resolve gate"), new RegionalAlphaProjectionFactor("lot", lotReview?.Verdict == RegionalLotReviewVerdict.Approved ? 1d : 0.5d, "Lot review", "address findings") };
            List<RegionalAlphaProjectionGap> gaps = new List<RegionalAlphaProjectionGap>();
            bool critical = risks != null && risks.Any(r => r.Severity == RegionalRiskSeverity.Critical && r.Status != RegionalRiskStatus.Resolved && r.Status != RegionalRiskStatus.Accepted);
            if (critical) gaps.Add(new RegionalAlphaProjectionGap("critical-risk", "resolve or explicitly accept critical risk"));
            if (coverage != null && coverage.Gaps.Count > 0) gaps.Add(new RegionalAlphaProjectionGap("coverage", "fill regional validation gaps"));
            if (bundle == null) gaps.Add(new RegionalAlphaProjectionGap("bundle", "create regional evidence bundle"));
            RegionalAlphaProjectionVerdict verdict = critical ? RegionalAlphaProjectionVerdict.Blocked : bundle == null ? RegionalAlphaProjectionVerdict.InsufficientEvidence : coverage != null && coverage.Gaps.Count > 0 ? RegionalAlphaProjectionVerdict.AtRisk : RegionalAlphaProjectionVerdict.OnTrack;
            return new RegionalAlphaReadinessProjection(verdict, factors, gaps);
        }
    }

    public sealed class RegionalWorldExecutionClosureCriterion { public string CriterionId { get; } public bool Passed { get; } public bool Blocking { get; } public string Reason { get; } public RegionalWorldExecutionClosureCriterion(string criterionId, bool passed, bool blocking, string reason) { CriterionId = RegionalEvidenceRecord.Required(criterionId); Passed = passed; Blocking = blocking; Reason = reason ?? string.Empty; } }
    public sealed class RegionalWorldExecutionClosureInput { public IReadOnlyList<RegionalWorldExecutionClosureCriterion> Criteria { get; } public bool ReferencesBee151 { get; } public RegionalWorldExecutionClosureInput(IReadOnlyList<RegionalWorldExecutionClosureCriterion> criteria, bool referencesBee151 = false) { Criteria = criteria ?? Array.Empty<RegionalWorldExecutionClosureCriterion>(); ReferencesBee151 = referencesBee151; } }
    public sealed class RegionalWorldExecutionClosureReport { public RegionalWorldExecutionClosureVerdict Verdict { get; } public IReadOnlyList<RegionalWorldExecutionClosureCriterion> Criteria { get; } public IReadOnlyList<string> Diagnostics { get; } public RegionalWorldExecutionClosureReport(RegionalWorldExecutionClosureVerdict verdict, IReadOnlyList<RegionalWorldExecutionClosureCriterion> criteria, IReadOnlyList<string> diagnostics) { Verdict = verdict; Criteria = new List<RegionalWorldExecutionClosureCriterion>(criteria ?? Array.Empty<RegionalWorldExecutionClosureCriterion>()).OrderBy(c => c.CriterionId).ToList().AsReadOnly(); Diagnostics = new List<string>(diagnostics ?? Array.Empty<string>()).AsReadOnly(); } }
    public sealed class RegionalWorldExecutionClosureDiagnostics { public int ClosureCount { get; private set; } public int Bee151ReferenceCount { get; private set; } public void Record(bool bee151) { ClosureCount++; if (bee151) Bee151ReferenceCount++; } }
    public sealed class RegionalWorldExecutionClosureGate
    {
        public RegionalWorldExecutionClosureReport Evaluate(RegionalWorldExecutionClosureInput input, RegionalWorldExecutionClosureDiagnostics diagnostics = null)
        {
            diagnostics?.Record(input.ReferencesBee151);
            List<string> messages = new List<string>();
            if (input.ReferencesBee151) messages.Add("BEE-151 reference is not allowed in this closure cycle.");
            bool blocking = input.Criteria.Any(c => !c.Passed && c.Blocking) || input.ReferencesBee151;
            bool missing = input.Criteria.Any(c => !c.Passed && !c.Blocking);
            RegionalWorldExecutionClosureVerdict verdict = blocking ? RegionalWorldExecutionClosureVerdict.Blocked : missing ? RegionalWorldExecutionClosureVerdict.NeedsRevision : input.Criteria.Any(c => c.Reason.IndexOf("warning", StringComparison.OrdinalIgnoreCase) >= 0) ? RegionalWorldExecutionClosureVerdict.ClosedWithWarnings : RegionalWorldExecutionClosureVerdict.Closed;
            return new RegionalWorldExecutionClosureReport(verdict, input.Criteria, messages);
        }
    }
}
