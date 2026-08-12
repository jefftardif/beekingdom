using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum ToolingMilestoneProofStatus { ProvenByDocument, VisibleInDemoSurface, ImplementedByWorkerReport, BlockedByServer, BlockedByPrivacy, NotRuntimeProof, MissingEvidence }
    public enum ToolingMilestoneDemoStatus { Visible, GapVisible, Missing, NotApplicable }
    public enum ToolingMilestoneDiagnosticCode { ToolingMilestoneEvidenceMissing, ToolingMilestoneOwnerMissing, ToolingMilestoneRuntimeProofForbidden, ToolingMilestoneServerDependencyOpen, ToolingMilestoneDemoGapVisible }
    public sealed class ToolingMilestoneServerDependency { public ToolingMilestoneServerDependency(string topicId, bool open) { TopicId = topicId ?? string.Empty; Open = open; } public string TopicId { get; } public bool Open { get; } }
    public sealed class ToolingMilestoneBlockedClaim { public ToolingMilestoneBlockedClaim(string claimId, bool runtimeProofClaimed = false) { ClaimId = claimId ?? string.Empty; RuntimeProofClaimed = runtimeProofClaimed; } public string ClaimId { get; } public bool RuntimeProofClaimed { get; } }
    public sealed class ToolingMilestoneEvidenceRow { public ToolingMilestoneEvidenceRow(string beeRange, string domain, string evidenceSource, string ownerRole, ToolingMilestoneProofStatus proofStatus, ToolingMilestoneDemoStatus demoStatus, ToolingMilestoneServerDependency serverDependency, IReadOnlyList<ToolingMilestoneBlockedClaim> blockedClaims) { BeeRange = beeRange ?? string.Empty; Domain = domain ?? string.Empty; EvidenceSource = evidenceSource ?? string.Empty; OwnerRole = ownerRole ?? string.Empty; ProofStatus = proofStatus; DemoStatus = demoStatus; ServerDependency = serverDependency; BlockedClaims = blockedClaims ?? Array.Empty<ToolingMilestoneBlockedClaim>(); } public string BeeRange { get; } public string Domain { get; } public string EvidenceSource { get; } public string OwnerRole { get; } public ToolingMilestoneProofStatus ProofStatus { get; } public ToolingMilestoneDemoStatus DemoStatus { get; } public ToolingMilestoneServerDependency ServerDependency { get; } public IReadOnlyList<ToolingMilestoneBlockedClaim> BlockedClaims { get; } }
    public sealed class SocialMmoToolingMilestoneEvidenceMatrix
    {
        public SocialMmoToolingMilestoneEvidenceMatrix(string matrixId, IReadOnlyList<ToolingMilestoneEvidenceRow> rows) { MatrixId = ColonyIntegrationIds.Require(matrixId); Rows = rows ?? Array.Empty<ToolingMilestoneEvidenceRow>(); }
        public string MatrixId { get; } public IReadOnlyList<ToolingMilestoneEvidenceRow> Rows { get; }
        public ToolingMilestoneDiagnostics Evaluate()
        {
            var findings = new List<ToolingMilestoneDiagnosticCode>();
            if (Rows.Count == 0 || Rows.Any(r => string.IsNullOrWhiteSpace(r.EvidenceSource) || r.ProofStatus == ToolingMilestoneProofStatus.MissingEvidence)) findings.Add(ToolingMilestoneDiagnosticCode.ToolingMilestoneEvidenceMissing);
            if (Rows.Any(r => string.IsNullOrWhiteSpace(r.OwnerRole))) findings.Add(ToolingMilestoneDiagnosticCode.ToolingMilestoneOwnerMissing);
            if (Rows.SelectMany(r => r.BlockedClaims).Any(c => c.RuntimeProofClaimed)) findings.Add(ToolingMilestoneDiagnosticCode.ToolingMilestoneRuntimeProofForbidden);
            if (Rows.Any(r => r.ServerDependency != null && r.ServerDependency.Open)) findings.Add(ToolingMilestoneDiagnosticCode.ToolingMilestoneServerDependencyOpen);
            if (Rows.Any(r => r.DemoStatus == ToolingMilestoneDemoStatus.GapVisible || r.DemoStatus == ToolingMilestoneDemoStatus.Missing)) findings.Add(ToolingMilestoneDiagnosticCode.ToolingMilestoneDemoGapVisible);
            return new ToolingMilestoneDiagnostics(findings);
        }
    }
    public sealed class ToolingMilestoneDiagnostics { public ToolingMilestoneDiagnostics(IReadOnlyList<ToolingMilestoneDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ToolingMilestoneDiagnosticCode>(); } public IReadOnlyList<ToolingMilestoneDiagnosticCode> Findings { get; } public bool Contains(ToolingMilestoneDiagnosticCode code) { return Findings.Contains(code); } }

    public enum DemoRegressionDiagnosticCode { DemoRegressionCheckpointMissing, DemoRegressionLimitHidden, DemoRegressionRedactionMissing, DemoRegressionRuntimeClaimDetected, DemoRegressionSeparateSpecForbidden }
    public sealed class DemoRegressionLimitAssertion { public DemoRegressionLimitAssertion(string limitText, bool hidden = false) { LimitText = limitText ?? string.Empty; Hidden = hidden; } public string LimitText { get; } public bool Hidden { get; } }
    public sealed class DemoRegressionRedactionCheck { public DemoRegressionRedactionCheck(bool required, bool applied) { Required = required; Applied = applied; } public bool Required { get; } public bool Applied { get; } }
    public sealed class DemoRegressionBlockedClaim { public DemoRegressionBlockedClaim(string claimId, bool runtimeClaim = false, bool separateSpecRequested = false) { ClaimId = claimId ?? string.Empty; RuntimeClaim = runtimeClaim; SeparateSpecRequested = separateSpecRequested; } public string ClaimId { get; } public bool RuntimeClaim { get; } public bool SeparateSpecRequested { get; } }
    public sealed class DemoRegressionVisualCheckpoint { public DemoRegressionVisualCheckpoint(string checkpointId, DemoReviewBindingSurface targetSurface, string expectedVisibleElement, DemoRegressionLimitAssertion requiredLimit, DemoRegressionRedactionCheck redactionCheck, IReadOnlyList<DemoRegressionBlockedClaim> blockedRuntimeClaims) { CheckpointId = checkpointId ?? string.Empty; TargetSurface = targetSurface; ExpectedVisibleElement = expectedVisibleElement ?? string.Empty; RequiredLimit = requiredLimit; RedactionCheck = redactionCheck; BlockedRuntimeClaims = blockedRuntimeClaims ?? Array.Empty<DemoRegressionBlockedClaim>(); } public string CheckpointId { get; } public DemoReviewBindingSurface TargetSurface { get; } public string ExpectedVisibleElement { get; } public DemoRegressionLimitAssertion RequiredLimit { get; } public DemoRegressionRedactionCheck RedactionCheck { get; } public IReadOnlyList<DemoRegressionBlockedClaim> BlockedRuntimeClaims { get; } }
    public sealed class DemoRegressionCaptureScenario { public DemoRegressionCaptureScenario(string scenarioId, IReadOnlyList<DemoRegressionVisualCheckpoint> checkpoints) { ScenarioId = scenarioId ?? string.Empty; Checkpoints = checkpoints ?? Array.Empty<DemoRegressionVisualCheckpoint>(); } public string ScenarioId { get; } public IReadOnlyList<DemoRegressionVisualCheckpoint> Checkpoints { get; } }
    public sealed class SocialMmoDemoReadinessRegressionCapture
    {
        public SocialMmoDemoReadinessRegressionCapture(string captureId, IReadOnlyList<DemoRegressionCaptureScenario> scenarios) { CaptureId = ColonyIntegrationIds.Require(captureId); Scenarios = scenarios ?? Array.Empty<DemoRegressionCaptureScenario>(); }
        public string CaptureId { get; } public IReadOnlyList<DemoRegressionCaptureScenario> Scenarios { get; }
        public DemoRegressionDiagnostics Evaluate()
        {
            var checkpoints = Scenarios.SelectMany(s => s.Checkpoints).ToArray();
            var findings = new List<DemoRegressionDiagnosticCode>();
            if (checkpoints.Length == 0 || checkpoints.Any(c => string.IsNullOrWhiteSpace(c.CheckpointId) || string.IsNullOrWhiteSpace(c.ExpectedVisibleElement))) findings.Add(DemoRegressionDiagnosticCode.DemoRegressionCheckpointMissing);
            if (checkpoints.Any(c => c.RequiredLimit == null || c.RequiredLimit.Hidden || string.IsNullOrWhiteSpace(c.RequiredLimit.LimitText))) findings.Add(DemoRegressionDiagnosticCode.DemoRegressionLimitHidden);
            if (checkpoints.Any(c => c.RedactionCheck != null && c.RedactionCheck.Required && !c.RedactionCheck.Applied)) findings.Add(DemoRegressionDiagnosticCode.DemoRegressionRedactionMissing);
            if (checkpoints.SelectMany(c => c.BlockedRuntimeClaims).Any(c => c.RuntimeClaim)) findings.Add(DemoRegressionDiagnosticCode.DemoRegressionRuntimeClaimDetected);
            if (checkpoints.SelectMany(c => c.BlockedRuntimeClaims).Any(c => c.SeparateSpecRequested)) findings.Add(DemoRegressionDiagnosticCode.DemoRegressionSeparateSpecForbidden);
            return new DemoRegressionDiagnostics(findings);
        }
    }
    public sealed class DemoRegressionDiagnostics { public DemoRegressionDiagnostics(IReadOnlyList<DemoRegressionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DemoRegressionDiagnosticCode>(); } public IReadOnlyList<DemoRegressionDiagnosticCode> Findings { get; } public bool Contains(DemoRegressionDiagnosticCode code) { return Findings.Contains(code); } }

    public enum CrossHandoffOwnerRole { QaReviewer, ServerReviewer, DemoReviewer, WorkerImplementer, ArchitectReviewer, Missing }
    public enum CrossHandoffDiagnosticCode { CrossHandoffOwnerMissing, CrossHandoffSourceMissing, CrossHandoffLimitMissing, CrossHandoffFinalPriorityForbidden, CrossHandoffRuntimeTicketForbidden }
    public sealed class CrossHandoffSourceRef { public CrossHandoffSourceRef(string sourceId) { SourceId = sourceId ?? string.Empty; } public string SourceId { get; } }
    public sealed class CrossHandoffBlocker { public CrossHandoffBlocker(string blockerId, bool serverDependency = false) { BlockerId = blockerId ?? string.Empty; ServerDependency = serverDependency; } public string BlockerId { get; } public bool ServerDependency { get; } }
    public sealed class CrossHandoffLimit { public CrossHandoffLimit(string limitText) { LimitText = limitText ?? string.Empty; } public string LimitText { get; } }
    public sealed class CrossHandoffLedgerEntry { public CrossHandoffLedgerEntry(string topicId, IReadOnlyList<CrossHandoffSourceRef> sourceRefs, CrossHandoffOwnerRole fromRole, CrossHandoffOwnerRole toRole, CrossHandoffOwnerRole ownerRole, CrossHandoffBlocker blocker, CrossHandoffLimit limit, string serverDependency, bool finalPriorityClaimed = false, bool runtimeTicketRequested = false) { TopicId = topicId ?? string.Empty; SourceRefs = sourceRefs ?? Array.Empty<CrossHandoffSourceRef>(); FromRole = fromRole; ToRole = toRole; OwnerRole = ownerRole; Blocker = blocker; Limit = limit; ServerDependency = serverDependency ?? string.Empty; FinalPriorityClaimed = finalPriorityClaimed; RuntimeTicketRequested = runtimeTicketRequested; } public string TopicId { get; } public IReadOnlyList<CrossHandoffSourceRef> SourceRefs { get; } public CrossHandoffOwnerRole FromRole { get; } public CrossHandoffOwnerRole ToRole { get; } public CrossHandoffOwnerRole OwnerRole { get; } public CrossHandoffBlocker Blocker { get; } public CrossHandoffLimit Limit { get; } public string ServerDependency { get; } public bool FinalPriorityClaimed { get; } public bool RuntimeTicketRequested { get; } }
    public sealed class SocialMmoCrossHandoffLedger
    {
        public SocialMmoCrossHandoffLedger(string ledgerId, IReadOnlyList<CrossHandoffLedgerEntry> entries) { LedgerId = ColonyIntegrationIds.Require(ledgerId); Entries = entries ?? Array.Empty<CrossHandoffLedgerEntry>(); }
        public string LedgerId { get; } public IReadOnlyList<CrossHandoffLedgerEntry> Entries { get; }
        public CrossHandoffDiagnostics Evaluate()
        {
            var findings = new List<CrossHandoffDiagnosticCode>();
            if (Entries.Any(e => e.OwnerRole == CrossHandoffOwnerRole.Missing)) findings.Add(CrossHandoffDiagnosticCode.CrossHandoffOwnerMissing);
            if (Entries.Count == 0 || Entries.Any(e => e.SourceRefs.Count == 0 || e.SourceRefs.Any(s => string.IsNullOrWhiteSpace(s.SourceId)))) findings.Add(CrossHandoffDiagnosticCode.CrossHandoffSourceMissing);
            if (Entries.Any(e => e.Limit == null || string.IsNullOrWhiteSpace(e.Limit.LimitText))) findings.Add(CrossHandoffDiagnosticCode.CrossHandoffLimitMissing);
            if (Entries.Any(e => e.FinalPriorityClaimed)) findings.Add(CrossHandoffDiagnosticCode.CrossHandoffFinalPriorityForbidden);
            if (Entries.Any(e => e.RuntimeTicketRequested)) findings.Add(CrossHandoffDiagnosticCode.CrossHandoffRuntimeTicketForbidden);
            return new CrossHandoffDiagnostics(findings);
        }
    }
    public sealed class CrossHandoffDiagnostics { public CrossHandoffDiagnostics(IReadOnlyList<CrossHandoffDiagnosticCode> findings) { Findings = findings ?? Array.Empty<CrossHandoffDiagnosticCode>(); } public IReadOnlyList<CrossHandoffDiagnosticCode> Findings { get; } public bool Contains(CrossHandoffDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ToolingDebtCategory { FragileEvidence, DemoGap, QaGap, ServerDependency, PrivacyRisk, RuntimeConfusion, StaleDocumentation, IncompleteHandoff }
    public enum ToolingDebtSeverity { Missing, Low, Medium, High, Critical }
    public enum ToolingDebtDiagnosticCode { ToolingDebtOwnerMissing, ToolingDebtSeverityMissing, ToolingDebtAcceptedAsReadyForbidden, ToolingDebtRuntimeConfusionOpen, ToolingDebtServerDependencyOpen }
    public sealed class ToolingDebtOwner { public ToolingDebtOwner(string ownerRole) { OwnerRole = ownerRole ?? string.Empty; } public string OwnerRole { get; } }
    public sealed class ToolingDebtNotAcceptedMarker { public ToolingDebtNotAcceptedMarker(bool acceptedAsReady = false) { AcceptedAsReady = acceptedAsReady; } public bool AcceptedAsReady { get; } }
    public sealed class ToolingDebtItem { public ToolingDebtItem(string debtId, ToolingDebtCategory category, ToolingDebtSeverity severity, ToolingDebtOwner owner, IReadOnlyList<string> sourceRefs, string blocker, ToolingDebtNotAcceptedMarker notAcceptedMarker) { DebtId = debtId ?? string.Empty; Category = category; Severity = severity; Owner = owner; SourceRefs = sourceRefs ?? Array.Empty<string>(); Blocker = blocker ?? string.Empty; NotAcceptedMarker = notAcceptedMarker; } public string DebtId { get; } public ToolingDebtCategory Category { get; } public ToolingDebtSeverity Severity { get; } public ToolingDebtOwner Owner { get; } public IReadOnlyList<string> SourceRefs { get; } public string Blocker { get; } public ToolingDebtNotAcceptedMarker NotAcceptedMarker { get; } }
    public sealed class SocialMmoToolingDebtRegister
    {
        public SocialMmoToolingDebtRegister(string registerId, IReadOnlyList<ToolingDebtItem> items) { RegisterId = ColonyIntegrationIds.Require(registerId); Items = items ?? Array.Empty<ToolingDebtItem>(); }
        public string RegisterId { get; } public IReadOnlyList<ToolingDebtItem> Items { get; }
        public ToolingDebtDiagnostics Evaluate()
        {
            var findings = new List<ToolingDebtDiagnosticCode>();
            if (Items.Any(i => i.Owner == null || string.IsNullOrWhiteSpace(i.Owner.OwnerRole))) findings.Add(ToolingDebtDiagnosticCode.ToolingDebtOwnerMissing);
            if (Items.Count == 0 || Items.Any(i => i.Severity == ToolingDebtSeverity.Missing)) findings.Add(ToolingDebtDiagnosticCode.ToolingDebtSeverityMissing);
            if (Items.Any(i => i.NotAcceptedMarker != null && i.NotAcceptedMarker.AcceptedAsReady)) findings.Add(ToolingDebtDiagnosticCode.ToolingDebtAcceptedAsReadyForbidden);
            if (Items.Any(i => i.Category == ToolingDebtCategory.RuntimeConfusion)) findings.Add(ToolingDebtDiagnosticCode.ToolingDebtRuntimeConfusionOpen);
            if (Items.Any(i => i.Category == ToolingDebtCategory.ServerDependency)) findings.Add(ToolingDebtDiagnosticCode.ToolingDebtServerDependencyOpen);
            return new ToolingDebtDiagnostics(findings);
        }
    }
    public sealed class ToolingDebtDiagnostics { public ToolingDebtDiagnostics(IReadOnlyList<ToolingDebtDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ToolingDebtDiagnosticCode>(); } public IReadOnlyList<ToolingDebtDiagnosticCode> Findings { get; } public bool Contains(ToolingDebtDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ReviewerComprehensionDiagnosticCode { ReviewerComprehensionSignalMissing, ReviewerComprehensionMarketingTextDetected, ReviewerComprehensionRuntimePromiseDetected, ReviewerComprehensionServerDependencyHidden, ReviewerComprehensionBlockerHidden }
    public sealed class ReviewerComprehensionExpectedSignal { public ReviewerComprehensionExpectedSignal(string signalId, bool visible) { SignalId = signalId ?? string.Empty; Visible = visible; } public string SignalId { get; } public bool Visible { get; } }
    public sealed class ReviewerComprehensionFailure { public ReviewerComprehensionFailure(string failureId, bool blockerHidden = false) { FailureId = failureId ?? string.Empty; BlockerHidden = blockerHidden; } public string FailureId { get; } public bool BlockerHidden { get; } }
    public sealed class ReviewerComprehensionNoMarketingRule { public ReviewerComprehensionNoMarketingRule(bool marketingTextDetected) { MarketingTextDetected = marketingTextDetected; } public bool MarketingTextDetected { get; } }
    public sealed class ReviewerComprehensionRuntimePromiseGuard { public ReviewerComprehensionRuntimePromiseGuard(bool runtimePromiseDetected) { RuntimePromiseDetected = runtimePromiseDetected; } public bool RuntimePromiseDetected { get; } }
    public sealed class ReviewerComprehensionQuestion { public ReviewerComprehensionQuestion(string questionId, ReviewerComprehensionExpectedSignal expectedVisualSignal, ReviewerComprehensionFailure failureCondition, string linkedDebtId, bool serverDependencyVisible, ReviewerComprehensionRuntimePromiseGuard runtimePromiseGuard, ReviewerComprehensionNoMarketingRule noMarketingRule) { QuestionId = questionId ?? string.Empty; ExpectedVisualSignal = expectedVisualSignal; FailureCondition = failureCondition; LinkedDebtId = linkedDebtId ?? string.Empty; ServerDependencyVisible = serverDependencyVisible; RuntimePromiseGuard = runtimePromiseGuard; NoMarketingRule = noMarketingRule; } public string QuestionId { get; } public ReviewerComprehensionExpectedSignal ExpectedVisualSignal { get; } public ReviewerComprehensionFailure FailureCondition { get; } public string LinkedDebtId { get; } public bool ServerDependencyVisible { get; } public ReviewerComprehensionRuntimePromiseGuard RuntimePromiseGuard { get; } public ReviewerComprehensionNoMarketingRule NoMarketingRule { get; } }
    public sealed class ExternalReviewerComprehensionChecklist
    {
        public ExternalReviewerComprehensionChecklist(string checklistId, IReadOnlyList<ReviewerComprehensionQuestion> questions) { ChecklistId = ColonyIntegrationIds.Require(checklistId); Questions = questions ?? Array.Empty<ReviewerComprehensionQuestion>(); }
        public string ChecklistId { get; } public IReadOnlyList<ReviewerComprehensionQuestion> Questions { get; }
        public ReviewerComprehensionDiagnostics Evaluate()
        {
            var findings = new List<ReviewerComprehensionDiagnosticCode>();
            if (Questions.Count == 0 || Questions.Any(q => q.ExpectedVisualSignal == null || !q.ExpectedVisualSignal.Visible)) findings.Add(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionSignalMissing);
            if (Questions.Any(q => q.NoMarketingRule != null && q.NoMarketingRule.MarketingTextDetected)) findings.Add(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionMarketingTextDetected);
            if (Questions.Any(q => q.RuntimePromiseGuard != null && q.RuntimePromiseGuard.RuntimePromiseDetected)) findings.Add(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionRuntimePromiseDetected);
            if (Questions.Any(q => !q.ServerDependencyVisible)) findings.Add(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionServerDependencyHidden);
            if (Questions.Any(q => q.FailureCondition != null && q.FailureCondition.BlockerHidden)) findings.Add(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionBlockerHidden);
            return new ReviewerComprehensionDiagnostics(findings);
        }
    }
    public sealed class ReviewerComprehensionDiagnostics { public ReviewerComprehensionDiagnostics(IReadOnlyList<ReviewerComprehensionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ReviewerComprehensionDiagnosticCode>(); } public IReadOnlyList<ReviewerComprehensionDiagnosticCode> Findings { get; } public bool Contains(ReviewerComprehensionDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ToolingPerformanceDiagnosticCode { ToolingPerformanceSignalMissingLimit, ToolingPerformanceBenchmarkFinalForbidden, ToolingPerformanceTelemetryProductionForbidden, ToolingPerformanceGuaranteeForbidden, ToolingPerformanceDensityWarning }
    public sealed class ToolingPerformanceMeasurementLimit { public ToolingPerformanceMeasurementLimit(string text) { Text = text ?? string.Empty; } public string Text { get; } }
    public sealed class ToolingPerformanceDisplayCost { public ToolingPerformanceDisplayCost(double hint, bool densityWarning = false) { Hint = hint; DensityWarning = densityWarning; } public double Hint { get; } public bool DensityWarning { get; } }
    public sealed class ToolingPerformanceLogicalCost { public ToolingPerformanceLogicalCost(double hint) { Hint = hint; } public double Hint { get; } }
    public sealed class ToolingPerformanceGuaranteeBlocker { public ToolingPerformanceGuaranteeBlocker(bool benchmarkFinalClaimed = false, bool telemetryProductionRequested = false, bool guaranteeClaimed = false) { BenchmarkFinalClaimed = benchmarkFinalClaimed; TelemetryProductionRequested = telemetryProductionRequested; GuaranteeClaimed = guaranteeClaimed; } public bool BenchmarkFinalClaimed { get; } public bool TelemetryProductionRequested { get; } public bool GuaranteeClaimed { get; } }
    public sealed class ToolingPerformanceSignal { public ToolingPerformanceSignal(string signalId, string sourceSurface, ToolingPerformanceLogicalCost logicalCostHint, ToolingPerformanceDisplayCost displayCostHint, ToolingPerformanceMeasurementLimit measurementLimit, ToolingPerformanceGuaranteeBlocker guaranteeBlocked) { SignalId = signalId ?? string.Empty; SourceSurface = sourceSurface ?? string.Empty; LogicalCostHint = logicalCostHint; DisplayCostHint = displayCostHint; MeasurementLimit = measurementLimit; GuaranteeBlocked = guaranteeBlocked; } public string SignalId { get; } public string SourceSurface { get; } public ToolingPerformanceLogicalCost LogicalCostHint { get; } public ToolingPerformanceDisplayCost DisplayCostHint { get; } public ToolingPerformanceMeasurementLimit MeasurementLimit { get; } public ToolingPerformanceGuaranteeBlocker GuaranteeBlocked { get; } }
    public sealed class SocialMmoToolingPerformanceSignalBoundary
    {
        public SocialMmoToolingPerformanceSignalBoundary(string boundaryId, IReadOnlyList<ToolingPerformanceSignal> signals) { BoundaryId = ColonyIntegrationIds.Require(boundaryId); Signals = signals ?? Array.Empty<ToolingPerformanceSignal>(); }
        public string BoundaryId { get; } public IReadOnlyList<ToolingPerformanceSignal> Signals { get; }
        public ToolingPerformanceDiagnostics Evaluate()
        {
            var findings = new List<ToolingPerformanceDiagnosticCode>();
            if (Signals.Count == 0 || Signals.Any(s => s.MeasurementLimit == null || string.IsNullOrWhiteSpace(s.MeasurementLimit.Text))) findings.Add(ToolingPerformanceDiagnosticCode.ToolingPerformanceSignalMissingLimit);
            if (Signals.Any(s => s.GuaranteeBlocked != null && s.GuaranteeBlocked.BenchmarkFinalClaimed)) findings.Add(ToolingPerformanceDiagnosticCode.ToolingPerformanceBenchmarkFinalForbidden);
            if (Signals.Any(s => s.GuaranteeBlocked != null && s.GuaranteeBlocked.TelemetryProductionRequested)) findings.Add(ToolingPerformanceDiagnosticCode.ToolingPerformanceTelemetryProductionForbidden);
            if (Signals.Any(s => s.GuaranteeBlocked != null && s.GuaranteeBlocked.GuaranteeClaimed)) findings.Add(ToolingPerformanceDiagnosticCode.ToolingPerformanceGuaranteeForbidden);
            if (Signals.Any(s => s.DisplayCostHint != null && s.DisplayCostHint.DensityWarning)) findings.Add(ToolingPerformanceDiagnosticCode.ToolingPerformanceDensityWarning);
            return new ToolingPerformanceDiagnostics(findings);
        }
    }
    public sealed class ToolingPerformanceDiagnostics { public ToolingPerformanceDiagnostics(IReadOnlyList<ToolingPerformanceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ToolingPerformanceDiagnosticCode>(); } public IReadOnlyList<ToolingPerformanceDiagnosticCode> Findings { get; } public bool Contains(ToolingPerformanceDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ToolchainFailureSeverity { Low, Medium, High, Critical }
    public enum ToolchainFailureDiagnosticCode { ToolchainFailureModeMissing, ToolchainFailureSilentCorrectionForbidden, ToolchainFailurePrivacyLeakDetected, ToolchainFailureServerBypassDetected, ToolchainFailureFalseVerdictDetected }
    public sealed class ToolchainFailureBlockRule { public ToolchainFailureBlockRule(bool blocksReview, bool silentCorrectionRequested = false) { BlocksReview = blocksReview; SilentCorrectionRequested = silentCorrectionRequested; } public bool BlocksReview { get; } public bool SilentCorrectionRequested { get; } }
    public sealed class ToolchainFailureRecoveryHint { public ToolchainFailureRecoveryHint(string hint, bool executableRecovery = false) { Hint = hint ?? string.Empty; ExecutableRecovery = executableRecovery; } public string Hint { get; } public bool ExecutableRecovery { get; } }
    public sealed class ToolchainFailureDemoMarker { public ToolchainFailureDemoMarker(string markerId) { MarkerId = markerId ?? string.Empty; } public string MarkerId { get; } }
    public sealed class ToolchainFailureMode { public ToolchainFailureMode(string failureId, string category, ToolchainFailureSeverity severity, ToolchainFailureBlockRule blockRule, string linkedDebtId, ToolchainFailureRecoveryHint recoveryHint, ToolchainFailureDemoMarker demoMarker, bool privacyLeak = false, bool serverBypass = false, bool falseVerdict = false) { FailureId = failureId ?? string.Empty; Category = category ?? string.Empty; Severity = severity; BlockRule = blockRule; LinkedDebtId = linkedDebtId ?? string.Empty; RecoveryHint = recoveryHint; DemoMarker = demoMarker; PrivacyLeak = privacyLeak; ServerBypass = serverBypass; FalseVerdict = falseVerdict; } public string FailureId { get; } public string Category { get; } public ToolchainFailureSeverity Severity { get; } public ToolchainFailureBlockRule BlockRule { get; } public string LinkedDebtId { get; } public ToolchainFailureRecoveryHint RecoveryHint { get; } public ToolchainFailureDemoMarker DemoMarker { get; } public bool PrivacyLeak { get; } public bool ServerBypass { get; } public bool FalseVerdict { get; } }
    public sealed class SocialMmoToolchainFailureModeCatalog
    {
        public SocialMmoToolchainFailureModeCatalog(string catalogId, IReadOnlyList<ToolchainFailureMode> modes) { CatalogId = ColonyIntegrationIds.Require(catalogId); Modes = modes ?? Array.Empty<ToolchainFailureMode>(); }
        public string CatalogId { get; } public IReadOnlyList<ToolchainFailureMode> Modes { get; }
        public ToolchainFailureDiagnostics Evaluate()
        {
            var findings = new List<ToolchainFailureDiagnosticCode>();
            if (Modes.Count == 0 || Modes.Any(m => string.IsNullOrWhiteSpace(m.FailureId))) findings.Add(ToolchainFailureDiagnosticCode.ToolchainFailureModeMissing);
            if (Modes.Any(m => m.BlockRule != null && m.BlockRule.SilentCorrectionRequested)) findings.Add(ToolchainFailureDiagnosticCode.ToolchainFailureSilentCorrectionForbidden);
            if (Modes.Any(m => m.PrivacyLeak)) findings.Add(ToolchainFailureDiagnosticCode.ToolchainFailurePrivacyLeakDetected);
            if (Modes.Any(m => m.ServerBypass)) findings.Add(ToolchainFailureDiagnosticCode.ToolchainFailureServerBypassDetected);
            if (Modes.Any(m => m.FalseVerdict)) findings.Add(ToolchainFailureDiagnosticCode.ToolchainFailureFalseVerdictDetected);
            return new ToolchainFailureDiagnostics(findings);
        }
    }
    public sealed class ToolchainFailureDiagnostics { public ToolchainFailureDiagnostics(IReadOnlyList<ToolchainFailureDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ToolchainFailureDiagnosticCode>(); } public IReadOnlyList<ToolchainFailureDiagnosticCode> Findings { get; } public bool Contains(ToolchainFailureDiagnosticCode code) { return Findings.Contains(code); } }

    public enum MilestoneProjectionDiagnosticCode { MilestoneProjectionGapHidden, MilestoneProjectionReleaseDecisionForbidden, MilestoneProjectionAlphaReadyForbidden, MilestoneProjectionServerReadyForbidden, MilestoneProjectionDependencyMissing }
    public sealed class MilestoneProjectionGain { public MilestoneProjectionGain(string sourceRange, string gainType, SocialMmoProductPillar playerPillar, string visibleEvidence, string remainingGap, string nextPhaseSignal) { SourceRange = sourceRange ?? string.Empty; GainType = gainType ?? string.Empty; PlayerPillar = playerPillar; VisibleEvidence = visibleEvidence ?? string.Empty; RemainingGap = remainingGap ?? string.Empty; NextPhaseSignal = nextPhaseSignal ?? string.Empty; } public string SourceRange { get; } public string GainType { get; } public SocialMmoProductPillar PlayerPillar { get; } public string VisibleEvidence { get; } public string RemainingGap { get; } public string NextPhaseSignal { get; } }
    public sealed class MilestoneProjectionGap { public MilestoneProjectionGap(string gapId, bool hidden = false) { GapId = gapId ?? string.Empty; Hidden = hidden; } public string GapId { get; } public bool Hidden { get; } }
    public sealed class MilestoneProjectionDependency { public MilestoneProjectionDependency(string dependencyId) { DependencyId = dependencyId ?? string.Empty; } public string DependencyId { get; } }
    public sealed class MilestoneProjectionForbiddenDecision { public MilestoneProjectionForbiddenDecision(bool alphaReady = false, bool releaseReady = false, bool serverReady = false) { AlphaReady = alphaReady; ReleaseReady = releaseReady; ServerReady = serverReady; } public bool AlphaReady { get; } public bool ReleaseReady { get; } public bool ServerReady { get; } }
    public sealed class PlaygroundToolsMilestoneProjection
    {
        public PlaygroundToolsMilestoneProjection(string projectionId, IReadOnlyList<MilestoneProjectionGain> gains, IReadOnlyList<MilestoneProjectionGap> gaps, IReadOnlyList<MilestoneProjectionDependency> dependencies, MilestoneProjectionForbiddenDecision forbiddenDecision) { ProjectionId = ColonyIntegrationIds.Require(projectionId); Gains = gains ?? Array.Empty<MilestoneProjectionGain>(); Gaps = gaps ?? Array.Empty<MilestoneProjectionGap>(); Dependencies = dependencies ?? Array.Empty<MilestoneProjectionDependency>(); ForbiddenDecision = forbiddenDecision; }
        public string ProjectionId { get; } public IReadOnlyList<MilestoneProjectionGain> Gains { get; } public IReadOnlyList<MilestoneProjectionGap> Gaps { get; } public IReadOnlyList<MilestoneProjectionDependency> Dependencies { get; } public MilestoneProjectionForbiddenDecision ForbiddenDecision { get; }
        public MilestoneProjectionDiagnostics Evaluate()
        {
            var findings = new List<MilestoneProjectionDiagnosticCode>();
            if (Gaps.Any(g => g.Hidden)) findings.Add(MilestoneProjectionDiagnosticCode.MilestoneProjectionGapHidden);
            if (ForbiddenDecision != null && ForbiddenDecision.ReleaseReady) findings.Add(MilestoneProjectionDiagnosticCode.MilestoneProjectionReleaseDecisionForbidden);
            if (ForbiddenDecision != null && ForbiddenDecision.AlphaReady) findings.Add(MilestoneProjectionDiagnosticCode.MilestoneProjectionAlphaReadyForbidden);
            if (ForbiddenDecision != null && ForbiddenDecision.ServerReady) findings.Add(MilestoneProjectionDiagnosticCode.MilestoneProjectionServerReadyForbidden);
            if (Dependencies.Count == 0 || Dependencies.Any(d => string.IsNullOrWhiteSpace(d.DependencyId))) findings.Add(MilestoneProjectionDiagnosticCode.MilestoneProjectionDependencyMissing);
            return new MilestoneProjectionDiagnostics(findings);
        }
    }
    public sealed class MilestoneProjectionDiagnostics { public MilestoneProjectionDiagnostics(IReadOnlyList<MilestoneProjectionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<MilestoneProjectionDiagnosticCode>(); } public IReadOnlyList<MilestoneProjectionDiagnosticCode> Findings { get; } public bool Contains(MilestoneProjectionDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ScaleHandoffDiagnosticCode { ScaleHandoffOwnerMissing, ScaleHandoffRiskMissing, ScaleHandoffB401Premature, ScaleHandoffTelemetryProductionForbidden, ScaleHandoffOperationsSpecForbidden }
    public sealed class ScaleOperationsRiskCarryover { public ScaleOperationsRiskCarryover(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class ScaleOperationsOwnerAssignment { public ScaleOperationsOwnerAssignment(string ownerRole) { OwnerRole = ownerRole ?? string.Empty; } public string OwnerRole { get; } }
    public sealed class ScaleOperationsBlockedToday { public ScaleOperationsBlockedToday(bool bee401Premature = false) { Bee401Premature = bee401Premature; } public bool Bee401Premature { get; } }
    public sealed class ScaleOperationsFutureQuestion { public ScaleOperationsFutureQuestion(string questionId) { QuestionId = questionId ?? string.Empty; } public string QuestionId { get; } }
    public sealed class ScaleOperationsHandoffTopic { public ScaleOperationsHandoffTopic(string topicId, IReadOnlyList<string> sourceBeeIds, ScaleOperationsOwnerAssignment owner, IReadOnlyList<ScaleOperationsRiskCarryover> riskCarryovers, ScaleOperationsBlockedToday blockedToday, IReadOnlyList<ScaleOperationsFutureQuestion> futureQuestions, IReadOnlyList<string> forbiddenClaims) { TopicId = topicId ?? string.Empty; SourceBeeIds = sourceBeeIds ?? Array.Empty<string>(); Owner = owner; RiskCarryovers = riskCarryovers ?? Array.Empty<ScaleOperationsRiskCarryover>(); BlockedToday = blockedToday; FutureQuestions = futureQuestions ?? Array.Empty<ScaleOperationsFutureQuestion>(); ForbiddenClaims = forbiddenClaims ?? Array.Empty<string>(); } public string TopicId { get; } public IReadOnlyList<string> SourceBeeIds { get; } public ScaleOperationsOwnerAssignment Owner { get; } public IReadOnlyList<ScaleOperationsRiskCarryover> RiskCarryovers { get; } public ScaleOperationsBlockedToday BlockedToday { get; } public IReadOnlyList<ScaleOperationsFutureQuestion> FutureQuestions { get; } public IReadOnlyList<string> ForbiddenClaims { get; } }
    public sealed class ScaleOperationsHandoffBundle
    {
        public ScaleOperationsHandoffBundle(string bundleId, IReadOnlyList<ScaleOperationsHandoffTopic> topics) { BundleId = ColonyIntegrationIds.Require(bundleId); Topics = topics ?? Array.Empty<ScaleOperationsHandoffTopic>(); }
        public string BundleId { get; } public IReadOnlyList<ScaleOperationsHandoffTopic> Topics { get; }
        public ScaleHandoffDiagnostics Evaluate()
        {
            var findings = new List<ScaleHandoffDiagnosticCode>();
            if (Topics.Any(t => t.Owner == null || string.IsNullOrWhiteSpace(t.Owner.OwnerRole))) findings.Add(ScaleHandoffDiagnosticCode.ScaleHandoffOwnerMissing);
            if (Topics.Count == 0 || Topics.Any(t => t.RiskCarryovers.Count == 0)) findings.Add(ScaleHandoffDiagnosticCode.ScaleHandoffRiskMissing);
            if (Topics.Any(t => t.BlockedToday != null && t.BlockedToday.Bee401Premature)) findings.Add(ScaleHandoffDiagnosticCode.ScaleHandoffB401Premature);
            if (Topics.Any(t => t.ForbiddenClaims.Contains("TelemetryProduction"))) findings.Add(ScaleHandoffDiagnosticCode.ScaleHandoffTelemetryProductionForbidden);
            if (Topics.Any(t => t.ForbiddenClaims.Contains("OperationsSpec"))) findings.Add(ScaleHandoffDiagnosticCode.ScaleHandoffOperationsSpecForbidden);
            return new ScaleHandoffDiagnostics(findings);
        }
    }
    public sealed class ScaleHandoffDiagnostics { public ScaleHandoffDiagnostics(IReadOnlyList<ScaleHandoffDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ScaleHandoffDiagnosticCode>(); } public IReadOnlyList<ScaleHandoffDiagnosticCode> Findings { get; } public bool Contains(ScaleHandoffDiagnosticCode code) { return Findings.Contains(code); } }

    public enum PlaygroundToolsClosureVerdict { ReadyForArchitectValidation, ReadyWithToolingWarnings, NeedsPlannerRevision, BlockedByMissingEvidence, BlockedByDemoRegression, BlockedByServerDependency, BlockedByRuntimeClaim, BlockedByBee401Premature }
    public enum PlaygroundClosureDiagnosticCode { PlaygroundClosureInputMissing, PlaygroundClosureEvidenceMissing, PlaygroundClosureDemoRegressionOpen, PlaygroundClosureServerDependencyOpen, PlaygroundClosureRuntimeClaimDetected, Bee401Premature }
    public sealed class PlaygroundToolsClosureInput { public PlaygroundToolsClosureInput(object readModelsAndTooling, object qaLiveOpsGovernance, object reviewConsoleGovernance, object reviewNavigationHandoff, SocialMmoToolingMilestoneEvidenceMatrix milestoneEvidenceMatrix, SocialMmoDemoReadinessRegressionCapture demoRegressionCapture, SocialMmoCrossHandoffLedger crossHandoffLedger, SocialMmoToolingDebtRegister toolingDebtRegister, ScaleOperationsHandoffBundle scaleOperationsHandoffBundle) { ReadModelsAndTooling = readModelsAndTooling; QaLiveOpsGovernance = qaLiveOpsGovernance; ReviewConsoleGovernance = reviewConsoleGovernance; ReviewNavigationHandoff = reviewNavigationHandoff; MilestoneEvidenceMatrix = milestoneEvidenceMatrix; DemoRegressionCapture = demoRegressionCapture; CrossHandoffLedger = crossHandoffLedger; ToolingDebtRegister = toolingDebtRegister; ScaleOperationsHandoffBundle = scaleOperationsHandoffBundle; } public object ReadModelsAndTooling { get; } public object QaLiveOpsGovernance { get; } public object ReviewConsoleGovernance { get; } public object ReviewNavigationHandoff { get; } public SocialMmoToolingMilestoneEvidenceMatrix MilestoneEvidenceMatrix { get; } public SocialMmoDemoReadinessRegressionCapture DemoRegressionCapture { get; } public SocialMmoCrossHandoffLedger CrossHandoffLedger { get; } public SocialMmoToolingDebtRegister ToolingDebtRegister { get; } public ScaleOperationsHandoffBundle ScaleOperationsHandoffBundle { get; } }
    public sealed class PlaygroundToolsClosureCoverage { public PlaygroundToolsClosureCoverage(bool evidenceMissing = false, bool demoRegressionOpen = false, bool runtimeClaim = false) { EvidenceMissing = evidenceMissing; DemoRegressionOpen = demoRegressionOpen; RuntimeClaim = runtimeClaim; } public bool EvidenceMissing { get; } public bool DemoRegressionOpen { get; } public bool RuntimeClaim { get; } }
    public sealed class PlaygroundToolsClosureBlocker { public PlaygroundToolsClosureBlocker(string blockerId, bool serverDependencyOpen = false) { BlockerId = blockerId ?? string.Empty; ServerDependencyOpen = serverDependencyOpen; } public string BlockerId { get; } public bool ServerDependencyOpen { get; } }
    public sealed class Bee401BlockerStatus { public Bee401BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class PlaygroundToolsClosureGate
    {
        public const string Bee401BlockedMessage = "BEE-401 bloquee jusqu'a validation architecte.";
        public PlaygroundToolsClosureGate(string gateId, PlaygroundToolsClosureInput input, PlaygroundToolsClosureCoverage coverage, IReadOnlyList<PlaygroundToolsClosureBlocker> blockers, Bee401BlockerStatus bee401BlockerStatus) { GateId = ColonyIntegrationIds.Require(gateId); Input = input; Coverage = coverage ?? new PlaygroundToolsClosureCoverage(); Blockers = blockers ?? Array.Empty<PlaygroundToolsClosureBlocker>(); Bee401BlockerStatus = bee401BlockerStatus ?? new Bee401BlockerStatus(false, Bee401BlockedMessage); }
        public string GateId { get; } public PlaygroundToolsClosureInput Input { get; } public PlaygroundToolsClosureCoverage Coverage { get; } public IReadOnlyList<PlaygroundToolsClosureBlocker> Blockers { get; } public Bee401BlockerStatus Bee401BlockerStatus { get; }
        public PlaygroundToolsClosureDiagnostics Evaluate()
        {
            var findings = new List<PlaygroundClosureDiagnosticCode>();
            if (Input == null || Input.MilestoneEvidenceMatrix == null || Input.DemoRegressionCapture == null || Input.CrossHandoffLedger == null || Input.ToolingDebtRegister == null || Input.ScaleOperationsHandoffBundle == null) findings.Add(PlaygroundClosureDiagnosticCode.PlaygroundClosureInputMissing);
            if (Coverage.EvidenceMissing) findings.Add(PlaygroundClosureDiagnosticCode.PlaygroundClosureEvidenceMissing);
            if (Coverage.DemoRegressionOpen) findings.Add(PlaygroundClosureDiagnosticCode.PlaygroundClosureDemoRegressionOpen);
            if (Blockers.Any(b => b.ServerDependencyOpen)) findings.Add(PlaygroundClosureDiagnosticCode.PlaygroundClosureServerDependencyOpen);
            if (Coverage.RuntimeClaim) findings.Add(PlaygroundClosureDiagnosticCode.PlaygroundClosureRuntimeClaimDetected);
            if (Bee401BlockerStatus.PrematureAttempt) findings.Add(PlaygroundClosureDiagnosticCode.Bee401Premature);
            return new PlaygroundToolsClosureDiagnostics(ResolveVerdict(findings), findings);
        }
        private static PlaygroundToolsClosureVerdict ResolveVerdict(IReadOnlyList<PlaygroundClosureDiagnosticCode> findings)
        {
            if (findings.Contains(PlaygroundClosureDiagnosticCode.Bee401Premature)) return PlaygroundToolsClosureVerdict.BlockedByBee401Premature;
            if (findings.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureRuntimeClaimDetected)) return PlaygroundToolsClosureVerdict.BlockedByRuntimeClaim;
            if (findings.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureServerDependencyOpen)) return PlaygroundToolsClosureVerdict.BlockedByServerDependency;
            if (findings.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureDemoRegressionOpen)) return PlaygroundToolsClosureVerdict.BlockedByDemoRegression;
            if (findings.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureEvidenceMissing)) return PlaygroundToolsClosureVerdict.BlockedByMissingEvidence;
            if (findings.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureInputMissing)) return PlaygroundToolsClosureVerdict.NeedsPlannerRevision;
            return findings.Count == 0 ? PlaygroundToolsClosureVerdict.ReadyForArchitectValidation : PlaygroundToolsClosureVerdict.ReadyWithToolingWarnings;
        }
    }
    public sealed class PlaygroundToolsClosureDiagnostics { public PlaygroundToolsClosureDiagnostics(PlaygroundToolsClosureVerdict verdict, IReadOnlyList<PlaygroundClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<PlaygroundClosureDiagnosticCode>(); } public PlaygroundToolsClosureVerdict Verdict { get; } public IReadOnlyList<PlaygroundClosureDiagnosticCode> Findings { get; } public bool Contains(PlaygroundClosureDiagnosticCode code) { return Findings.Contains(code); } }
}
