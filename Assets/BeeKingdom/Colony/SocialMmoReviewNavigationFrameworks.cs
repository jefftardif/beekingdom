using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum ReviewNavigationNodeType { Risk, Evidence, Export, PrivacyCase, ArmyCompetition, LiveOpsCandidate, ModerationCase, DecisionLogEntry, ServerEscalation }
    public enum ReviewNavigationDiagnosticCode { ReviewNavigationNodeMissing, ReviewNavigationOwnerTrailMissing, ReviewNavigationRuntimeRouteForbidden, ReviewNavigationLocalTruthRisk, ReviewNavigationBlockedRouteVisible }
    public sealed class ReviewNavigationContext { public ReviewNavigationContext(string contextId) { ContextId = contextId ?? string.Empty; } public string ContextId { get; } }
    public sealed class ReviewNavigationBlockedRoute { public ReviewNavigationBlockedRoute(string routeId, bool runtimeRoute = false) { RouteId = routeId ?? string.Empty; RuntimeRoute = runtimeRoute; } public string RouteId { get; } public bool RuntimeRoute { get; } }
    public sealed class ReviewNavigationOwnerTrail { public ReviewNavigationOwnerTrail(IReadOnlyList<string> ownerRoles) { OwnerRoles = ownerRoles ?? Array.Empty<string>(); } public IReadOnlyList<string> OwnerRoles { get; } public bool Missing => OwnerRoles.Count == 0 || OwnerRoles.Any(string.IsNullOrWhiteSpace); }
    public sealed class ReviewNavigationEdge { public ReviewNavigationEdge(string edgeId, string targetNodeId, bool runtimeRoute = false, bool localTruthClaimed = false, bool blockedVisible = false) { EdgeId = edgeId ?? string.Empty; TargetNodeId = targetNodeId ?? string.Empty; RuntimeRoute = runtimeRoute; LocalTruthClaimed = localTruthClaimed; BlockedVisible = blockedVisible; } public string EdgeId { get; } public string TargetNodeId { get; } public bool RuntimeRoute { get; } public bool LocalTruthClaimed { get; } public bool BlockedVisible { get; } }
    public sealed class ReviewNavigationNode
    {
        public ReviewNavigationNode(string nodeId, ReviewNavigationNodeType nodeType, string sourceBeeId, string title, string ownerRole, IReadOnlyList<string> evidenceRefs, IReadOnlyList<string> blockers, IReadOnlyList<ReviewNavigationEdge> outgoingRoutes)
        {
            NodeId = nodeId ?? string.Empty; NodeType = nodeType; SourceBeeId = sourceBeeId ?? string.Empty; Title = title ?? string.Empty; OwnerRole = ownerRole ?? string.Empty; EvidenceRefs = evidenceRefs ?? Array.Empty<string>(); Blockers = blockers ?? Array.Empty<string>(); OutgoingRoutes = outgoingRoutes ?? Array.Empty<ReviewNavigationEdge>();
        }
        public string NodeId { get; } public ReviewNavigationNodeType NodeType { get; } public string SourceBeeId { get; } public string Title { get; } public string OwnerRole { get; } public IReadOnlyList<string> EvidenceRefs { get; } public IReadOnlyList<string> Blockers { get; } public IReadOnlyList<ReviewNavigationEdge> OutgoingRoutes { get; }
    }
    public sealed class SocialMmoReviewNavigationMap
    {
        public SocialMmoReviewNavigationMap(string mapId, IReadOnlyList<ReviewNavigationNode> nodes, ReviewNavigationContext context, ReviewNavigationOwnerTrail ownerTrail, IReadOnlyList<ReviewNavigationBlockedRoute> blockedRoutes) { MapId = ColonyIntegrationIds.Require(mapId); Nodes = nodes ?? Array.Empty<ReviewNavigationNode>(); Context = context; OwnerTrail = ownerTrail; BlockedRoutes = blockedRoutes ?? Array.Empty<ReviewNavigationBlockedRoute>(); }
        public string MapId { get; } public IReadOnlyList<ReviewNavigationNode> Nodes { get; } public ReviewNavigationContext Context { get; } public ReviewNavigationOwnerTrail OwnerTrail { get; } public IReadOnlyList<ReviewNavigationBlockedRoute> BlockedRoutes { get; }
        public ReviewNavigationDiagnostics Evaluate()
        {
            var findings = new List<ReviewNavigationDiagnosticCode>();
            if (Nodes.Count == 0 || Nodes.Any(n => string.IsNullOrWhiteSpace(n.NodeId) || string.IsNullOrWhiteSpace(n.SourceBeeId))) findings.Add(ReviewNavigationDiagnosticCode.ReviewNavigationNodeMissing);
            if (OwnerTrail == null || OwnerTrail.Missing || Nodes.Any(n => string.IsNullOrWhiteSpace(n.OwnerRole))) findings.Add(ReviewNavigationDiagnosticCode.ReviewNavigationOwnerTrailMissing);
            if (BlockedRoutes.Any(r => r.RuntimeRoute) || Nodes.SelectMany(n => n.OutgoingRoutes).Any(r => r.RuntimeRoute)) findings.Add(ReviewNavigationDiagnosticCode.ReviewNavigationRuntimeRouteForbidden);
            if (Nodes.SelectMany(n => n.OutgoingRoutes).Any(r => r.LocalTruthClaimed)) findings.Add(ReviewNavigationDiagnosticCode.ReviewNavigationLocalTruthRisk);
            if (BlockedRoutes.Count > 0 || Nodes.SelectMany(n => n.OutgoingRoutes).Any(r => r.BlockedVisible)) findings.Add(ReviewNavigationDiagnosticCode.ReviewNavigationBlockedRouteVisible);
            return new ReviewNavigationDiagnostics(findings);
        }
    }
    public sealed class ReviewNavigationDiagnostics { public ReviewNavigationDiagnostics(IReadOnlyList<ReviewNavigationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ReviewNavigationDiagnosticCode>(); } public IReadOnlyList<ReviewNavigationDiagnosticCode> Findings { get; } public bool Contains(ReviewNavigationDiagnosticCode code) { return Findings.Contains(code); } }

    public enum DemoReviewBindingSurface { Demo012SandboxPlayground, Demo011PerformanceBenchmarkSuite, Demo009CombatDefenseReference }
    public enum DemoReviewBindingVisibilityState { Visible, VisibleWithWarning, Redacted, BlockedByPrivacy, BlockedByServer, NotDemonstrable }
    public enum DemoBindingDiagnosticCode { DemoBindingSurfaceMissing, DemoBindingRuntimeClaimDetected, DemoBindingRedactionMissing, DemoBindingSeparateSpecForbidden, DemoBindingLimitHidden }
    public sealed class DemoReviewBindingRedactionRule { public DemoReviewBindingRedactionRule(bool required, bool applied) { Required = required; Applied = applied; } public bool Required { get; } public bool Applied { get; } }
    public sealed class DemoReviewBindingLimit { public DemoReviewBindingLimit(string text, bool hidden = false, bool runtimeClaim = false, bool separateSpecRequested = false) { Text = text ?? string.Empty; Hidden = hidden; RuntimeClaim = runtimeClaim; SeparateSpecRequested = separateSpecRequested; } public string Text { get; } public bool Hidden { get; } public bool RuntimeClaim { get; } public bool SeparateSpecRequested { get; } }
    public sealed class DemoReviewBindingField
    {
        public DemoReviewBindingField(string fieldId, string sourceNodeId, DemoReviewBindingSurface? targetSurface, DemoReviewBindingVisibilityState visibilityState, DemoReviewBindingRedactionRule redactionRule, string visibleLimitText, string serverDependency, IReadOnlyList<DemoReviewBindingLimit> limits)
        {
            FieldId = fieldId ?? string.Empty; SourceNodeId = sourceNodeId ?? string.Empty; TargetSurface = targetSurface; VisibilityState = visibilityState; RedactionRule = redactionRule; VisibleLimitText = visibleLimitText ?? string.Empty; ServerDependency = serverDependency ?? string.Empty; Limits = limits ?? Array.Empty<DemoReviewBindingLimit>();
        }
        public string FieldId { get; } public string SourceNodeId { get; } public DemoReviewBindingSurface? TargetSurface { get; } public DemoReviewBindingVisibilityState VisibilityState { get; } public DemoReviewBindingRedactionRule RedactionRule { get; } public string VisibleLimitText { get; } public string ServerDependency { get; } public IReadOnlyList<DemoReviewBindingLimit> Limits { get; }
    }
    public sealed class SocialMmoDemoBindingContract
    {
        public SocialMmoDemoBindingContract(string contractId, IReadOnlyList<DemoReviewBindingField> fields) { ContractId = ColonyIntegrationIds.Require(contractId); Fields = fields ?? Array.Empty<DemoReviewBindingField>(); }
        public string ContractId { get; } public IReadOnlyList<DemoReviewBindingField> Fields { get; }
        public DemoBindingDiagnostics Evaluate()
        {
            var findings = new List<DemoBindingDiagnosticCode>();
            if (Fields.Count == 0 || Fields.Any(f => !f.TargetSurface.HasValue)) findings.Add(DemoBindingDiagnosticCode.DemoBindingSurfaceMissing);
            if (Fields.SelectMany(f => f.Limits).Any(l => l.RuntimeClaim)) findings.Add(DemoBindingDiagnosticCode.DemoBindingRuntimeClaimDetected);
            if (Fields.Any(f => f.RedactionRule != null && f.RedactionRule.Required && !f.RedactionRule.Applied)) findings.Add(DemoBindingDiagnosticCode.DemoBindingRedactionMissing);
            if (Fields.SelectMany(f => f.Limits).Any(l => l.SeparateSpecRequested)) findings.Add(DemoBindingDiagnosticCode.DemoBindingSeparateSpecForbidden);
            if (Fields.Any(f => string.IsNullOrWhiteSpace(f.VisibleLimitText)) || Fields.SelectMany(f => f.Limits).Any(l => l.Hidden)) findings.Add(DemoBindingDiagnosticCode.DemoBindingLimitHidden);
            return new DemoBindingDiagnostics(findings);
        }
    }
    public sealed class DemoBindingDiagnostics { public DemoBindingDiagnostics(IReadOnlyList<DemoBindingDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DemoBindingDiagnosticCode>(); } public IReadOnlyList<DemoBindingDiagnosticCode> Findings { get; } public bool Contains(DemoBindingDiagnosticCode code) { return Findings.Contains(code); } }

    public enum GovernanceExportDiffType { DecisionAdded, DecisionRemoved, BlockerAdded, BlockerClearedForReview, OwnerChanged, EvidenceBecameStale, PrivacyRestrictionAdded, ServerDependencyChanged }
    public enum GovernanceExportDiffVerdict { ReviewOnly, BlockedByInput, BlockedByCompatibility, BlockedBySensitiveData, BlockedByOfficialVerdict }
    public enum GovernanceDiffDiagnosticCode { GovernanceDiffInputMissing, GovernanceDiffIncompatibleExport, GovernanceDiffOfficialVerdictForbidden, GovernanceDiffStaleEvidenceDetected, GovernanceDiffSensitiveDataBlocked }
    public sealed class GovernanceExportDiffInput { public GovernanceExportDiffInput(string sourceExportId, string targetExportId, bool compatible = true) { SourceExportId = sourceExportId ?? string.Empty; TargetExportId = targetExportId ?? string.Empty; Compatible = compatible; } public string SourceExportId { get; } public string TargetExportId { get; } public bool Compatible { get; } }
    public sealed class GovernanceExportRegressionFlag { public GovernanceExportRegressionFlag(bool regression) { Regression = regression; } public bool Regression { get; } }
    public sealed class GovernanceExportObsoleteEvidenceFlag { public GovernanceExportObsoleteEvidenceFlag(bool obsolete) { Obsolete = obsolete; } public bool Obsolete { get; } }
    public sealed class GovernanceExportDiffItem { public GovernanceExportDiffItem(GovernanceExportDiffType diffType, string sourceExportId, string targetExportId, string affectedDecisionId, SocialMmoProductPillar affectedPillar, int severity, string evidenceStatus, string serverDependency, bool officialVerdictClaimed = false, bool sensitiveDataPresent = false) { DiffType = diffType; SourceExportId = sourceExportId ?? string.Empty; TargetExportId = targetExportId ?? string.Empty; AffectedDecisionId = affectedDecisionId ?? string.Empty; AffectedPillar = affectedPillar; Severity = severity; EvidenceStatus = evidenceStatus ?? string.Empty; ServerDependency = serverDependency ?? string.Empty; OfficialVerdictClaimed = officialVerdictClaimed; SensitiveDataPresent = sensitiveDataPresent; } public GovernanceExportDiffType DiffType { get; } public string SourceExportId { get; } public string TargetExportId { get; } public string AffectedDecisionId { get; } public SocialMmoProductPillar AffectedPillar { get; } public int Severity { get; } public string EvidenceStatus { get; } public string ServerDependency { get; } public bool OfficialVerdictClaimed { get; } public bool SensitiveDataPresent { get; } }
    public sealed class GovernanceExportDiffReview
    {
        public GovernanceExportDiffReview(string reviewId, GovernanceExportDiffInput input, IReadOnlyList<GovernanceExportDiffItem> items) { ReviewId = ColonyIntegrationIds.Require(reviewId); Input = input; Items = items ?? Array.Empty<GovernanceExportDiffItem>(); }
        public string ReviewId { get; } public GovernanceExportDiffInput Input { get; } public IReadOnlyList<GovernanceExportDiffItem> Items { get; }
        public GovernanceExportDiffDiagnostics Evaluate()
        {
            var findings = new List<GovernanceDiffDiagnosticCode>();
            if (Input == null || string.IsNullOrWhiteSpace(Input.SourceExportId) || string.IsNullOrWhiteSpace(Input.TargetExportId) || Items.Count == 0) findings.Add(GovernanceDiffDiagnosticCode.GovernanceDiffInputMissing);
            if (Input != null && !Input.Compatible) findings.Add(GovernanceDiffDiagnosticCode.GovernanceDiffIncompatibleExport);
            if (Items.Any(i => i.OfficialVerdictClaimed)) findings.Add(GovernanceDiffDiagnosticCode.GovernanceDiffOfficialVerdictForbidden);
            if (Items.Any(i => i.DiffType == GovernanceExportDiffType.EvidenceBecameStale || string.Equals(i.EvidenceStatus, "Obsolete", StringComparison.OrdinalIgnoreCase))) findings.Add(GovernanceDiffDiagnosticCode.GovernanceDiffStaleEvidenceDetected);
            if (Items.Any(i => i.SensitiveDataPresent)) findings.Add(GovernanceDiffDiagnosticCode.GovernanceDiffSensitiveDataBlocked);
            return new GovernanceExportDiffDiagnostics(ResolveVerdict(findings), findings);
        }
        private static GovernanceExportDiffVerdict ResolveVerdict(IReadOnlyList<GovernanceDiffDiagnosticCode> findings)
        {
            if (findings.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffOfficialVerdictForbidden)) return GovernanceExportDiffVerdict.BlockedByOfficialVerdict;
            if (findings.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffSensitiveDataBlocked)) return GovernanceExportDiffVerdict.BlockedBySensitiveData;
            if (findings.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffIncompatibleExport)) return GovernanceExportDiffVerdict.BlockedByCompatibility;
            if (findings.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffInputMissing)) return GovernanceExportDiffVerdict.BlockedByInput;
            return GovernanceExportDiffVerdict.ReviewOnly;
        }
    }
    public sealed class GovernanceExportDiffDiagnostics { public GovernanceExportDiffDiagnostics(GovernanceExportDiffVerdict verdict, IReadOnlyList<GovernanceDiffDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<GovernanceDiffDiagnosticCode>(); } public GovernanceExportDiffVerdict Verdict { get; } public IReadOnlyList<GovernanceDiffDiagnosticCode> Findings { get; } public bool Contains(GovernanceDiffDiagnosticCode code) { return Findings.Contains(code); } }

    public enum EvidenceViewerReadMode { SafeSummary, RedactedDetail, BlockedSensitive, ServerOnlyFuture, DemoForbidden }
    public enum EvidenceViewerDiagnosticCode { EvidenceViewerRawDataForbidden, EvidenceViewerExportRefused, EvidenceViewerVictimExposureRisk, EvidenceViewerPunitiveUseForbidden, EvidenceViewerFalsePositiveContextMissing }
    public sealed class EvidenceViewerSensitivityBadge { public EvidenceViewerSensitivityBadge(SensitiveEvidenceClass sensitivityClass, bool victimExposureRisk = false) { SensitivityClass = sensitivityClass; VictimExposureRisk = victimExposureRisk; } public SensitiveEvidenceClass SensitivityClass { get; } public bool VictimExposureRisk { get; } }
    public sealed class EvidenceViewerExportGuard { public EvidenceViewerExportGuard(bool exportAllowed, bool exportRequested = false) { ExportAllowed = exportAllowed; ExportRequested = exportRequested; } public bool ExportAllowed { get; } public bool ExportRequested { get; } }
    public sealed class EvidenceViewerPunitiveUseBlocker { public EvidenceViewerPunitiveUseBlocker(bool punitiveUseRequested) { PunitiveUseRequested = punitiveUseRequested; } public bool PunitiveUseRequested { get; } }
    public sealed class EvidenceViewerAuditNote { public EvidenceViewerAuditNote(string noteId, bool falsePositiveContextPresent) { NoteId = noteId ?? string.Empty; FalsePositiveContextPresent = falsePositiveContextPresent; } public string NoteId { get; } public bool FalsePositiveContextPresent { get; } }
    public sealed class EvidenceViewerRedactedField { public EvidenceViewerRedactedField(string fieldId, string label, string redactedValue, EvidenceViewerSensitivityBadge sensitivityBadge, EvidenceViewerExportGuard exportGuard, string blockedReason, string falsePositiveNoteRef, bool rawDataVisible = false) { FieldId = fieldId ?? string.Empty; Label = label ?? string.Empty; RedactedValue = redactedValue ?? string.Empty; SensitivityBadge = sensitivityBadge; ExportGuard = exportGuard; BlockedReason = blockedReason ?? string.Empty; FalsePositiveNoteRef = falsePositiveNoteRef ?? string.Empty; RawDataVisible = rawDataVisible; } public string FieldId { get; } public string Label { get; } public string RedactedValue { get; } public EvidenceViewerSensitivityBadge SensitivityBadge { get; } public EvidenceViewerExportGuard ExportGuard { get; } public string BlockedReason { get; } public string FalsePositiveNoteRef { get; } public bool RawDataVisible { get; } }
    public sealed class PrivacySafeEvidenceViewer
    {
        public PrivacySafeEvidenceViewer(string viewerId, EvidenceViewerReadMode mode, IReadOnlyList<EvidenceViewerRedactedField> fields, EvidenceViewerPunitiveUseBlocker punitiveUseBlocker, IReadOnlyList<EvidenceViewerAuditNote> auditNotes) { ViewerId = ColonyIntegrationIds.Require(viewerId); Mode = mode; Fields = fields ?? Array.Empty<EvidenceViewerRedactedField>(); PunitiveUseBlocker = punitiveUseBlocker; AuditNotes = auditNotes ?? Array.Empty<EvidenceViewerAuditNote>(); }
        public string ViewerId { get; } public EvidenceViewerReadMode Mode { get; } public IReadOnlyList<EvidenceViewerRedactedField> Fields { get; } public EvidenceViewerPunitiveUseBlocker PunitiveUseBlocker { get; } public IReadOnlyList<EvidenceViewerAuditNote> AuditNotes { get; }
        public EvidenceViewerDiagnostics Evaluate()
        {
            var findings = new List<EvidenceViewerDiagnosticCode>();
            if (Fields.Any(f => f.RawDataVisible)) findings.Add(EvidenceViewerDiagnosticCode.EvidenceViewerRawDataForbidden);
            if (Fields.Any(f => f.ExportGuard != null && f.ExportGuard.ExportRequested && !f.ExportGuard.ExportAllowed)) findings.Add(EvidenceViewerDiagnosticCode.EvidenceViewerExportRefused);
            if (Fields.Any(f => f.SensitivityBadge != null && f.SensitivityBadge.VictimExposureRisk)) findings.Add(EvidenceViewerDiagnosticCode.EvidenceViewerVictimExposureRisk);
            if (PunitiveUseBlocker != null && PunitiveUseBlocker.PunitiveUseRequested) findings.Add(EvidenceViewerDiagnosticCode.EvidenceViewerPunitiveUseForbidden);
            if (AuditNotes.Count == 0 || AuditNotes.Any(n => !n.FalsePositiveContextPresent)) findings.Add(EvidenceViewerDiagnosticCode.EvidenceViewerFalsePositiveContextMissing);
            return new EvidenceViewerDiagnostics(findings);
        }
    }
    public sealed class EvidenceViewerDiagnostics { public EvidenceViewerDiagnostics(IReadOnlyList<EvidenceViewerDiagnosticCode> findings) { Findings = findings ?? Array.Empty<EvidenceViewerDiagnosticCode>(); } public IReadOnlyList<EvidenceViewerDiagnosticCode> Findings { get; } public bool Contains(EvidenceViewerDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ScenarioPreviewDiagnosticCode { ScenarioPreviewCombatExecutionForbidden, ScenarioPreviewMatchmakingForbidden, ScenarioPreviewRewardForbidden, ScenarioPreviewLossForbidden, ScenarioPreviewServerDependencyMissing }
    public sealed class ScenarioPreviewRiskMarker { public ScenarioPreviewRiskMarker(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class ScenarioPreviewServerDependency { public ScenarioPreviewServerDependency(string topicId) { TopicId = topicId ?? string.Empty; } public string TopicId { get; } }
    public sealed class ScenarioPreviewQaQuestion { public ScenarioPreviewQaQuestion(string questionId) { QuestionId = questionId ?? string.Empty; } public string QuestionId { get; } }
    public sealed class ScenarioPreviewExecutionBlocker { public ScenarioPreviewExecutionBlocker(string blockerId, bool combatExecution = false, bool matchmaking = false, bool reward = false, bool loss = false) { BlockerId = blockerId ?? string.Empty; CombatExecution = combatExecution; Matchmaking = matchmaking; Reward = reward; Loss = loss; } public string BlockerId { get; } public bool CombatExecution { get; } public bool Matchmaking { get; } public bool Reward { get; } public bool Loss { get; } }
    public sealed class ScenarioPreviewNarrativeStep { public ScenarioPreviewNarrativeStep(string stepId, string scenarioId, string description, IReadOnlyList<SocialMmoProductPillar> affectedPillars, IReadOnlyList<ScenarioPreviewRiskMarker> riskMarkers, IReadOnlyList<ScenarioPreviewQaQuestion> qaQuestions, IReadOnlyList<ScenarioPreviewServerDependency> serverDependencies, IReadOnlyList<ScenarioPreviewExecutionBlocker> executionBlockers) { StepId = stepId ?? string.Empty; ScenarioId = scenarioId ?? string.Empty; Description = description ?? string.Empty; AffectedPillars = affectedPillars ?? Array.Empty<SocialMmoProductPillar>(); RiskMarkers = riskMarkers ?? Array.Empty<ScenarioPreviewRiskMarker>(); QaQuestions = qaQuestions ?? Array.Empty<ScenarioPreviewQaQuestion>(); ServerDependencies = serverDependencies ?? Array.Empty<ScenarioPreviewServerDependency>(); ExecutionBlockers = executionBlockers ?? Array.Empty<ScenarioPreviewExecutionBlocker>(); } public string StepId { get; } public string ScenarioId { get; } public string Description { get; } public IReadOnlyList<SocialMmoProductPillar> AffectedPillars { get; } public IReadOnlyList<ScenarioPreviewRiskMarker> RiskMarkers { get; } public IReadOnlyList<ScenarioPreviewQaQuestion> QaQuestions { get; } public IReadOnlyList<ScenarioPreviewServerDependency> ServerDependencies { get; } public IReadOnlyList<ScenarioPreviewExecutionBlocker> ExecutionBlockers { get; } }
    public sealed class AlliancePvpScenarioPreviewLens
    {
        public AlliancePvpScenarioPreviewLens(string lensId, IReadOnlyList<ScenarioPreviewNarrativeStep> steps) { LensId = ColonyIntegrationIds.Require(lensId); Steps = steps ?? Array.Empty<ScenarioPreviewNarrativeStep>(); }
        public string LensId { get; } public IReadOnlyList<ScenarioPreviewNarrativeStep> Steps { get; }
        public ScenarioPreviewDiagnostics Evaluate()
        {
            var blockers = Steps.SelectMany(s => s.ExecutionBlockers).ToArray();
            var findings = new List<ScenarioPreviewDiagnosticCode>();
            if (blockers.Any(b => b.CombatExecution)) findings.Add(ScenarioPreviewDiagnosticCode.ScenarioPreviewCombatExecutionForbidden);
            if (blockers.Any(b => b.Matchmaking)) findings.Add(ScenarioPreviewDiagnosticCode.ScenarioPreviewMatchmakingForbidden);
            if (blockers.Any(b => b.Reward)) findings.Add(ScenarioPreviewDiagnosticCode.ScenarioPreviewRewardForbidden);
            if (blockers.Any(b => b.Loss)) findings.Add(ScenarioPreviewDiagnosticCode.ScenarioPreviewLossForbidden);
            if (Steps.Count == 0 || Steps.Any(s => s.ServerDependencies.Count == 0 || s.ServerDependencies.Any(d => string.IsNullOrWhiteSpace(d.TopicId)))) findings.Add(ScenarioPreviewDiagnosticCode.ScenarioPreviewServerDependencyMissing);
            return new ScenarioPreviewDiagnostics(findings);
        }
    }
    public sealed class ScenarioPreviewDiagnostics { public ScenarioPreviewDiagnostics(IReadOnlyList<ScenarioPreviewDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ScenarioPreviewDiagnosticCode>(); } public IReadOnlyList<ScenarioPreviewDiagnosticCode> Findings { get; } public bool Contains(ScenarioPreviewDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ArmyDrilldownDiagnosticCode { ArmyDrilldownSignalMissing, ArmyDrilldownOfficialScoreForbidden, ArmyDrilldownPayToWinRiskOpen, ArmyDrilldownCombatActivationForbidden, ArmyDrilldownServerAuthorityRequired }
    public sealed class FairnessDrilldownCause { public FairnessDrilldownCause(string causeId) { CauseId = causeId ?? string.Empty; } public string CauseId { get; } }
    public sealed class PayToWinRiskDrilldown { public PayToWinRiskDrilldown(bool open) { Open = open; } public bool Open { get; } }
    public sealed class CompetitionActivationBlocker { public CompetitionActivationBlocker(string blockerId, bool combatActivation = false, bool officialScore = false) { BlockerId = blockerId ?? string.Empty; CombatActivation = combatActivation; OfficialScore = officialScore; } public string BlockerId { get; } public bool CombatActivation { get; } public bool OfficialScore { get; } }
    public sealed class ArmyServerAuthorityMarker { public ArmyServerAuthorityMarker(string topicId, bool required) { TopicId = topicId ?? string.Empty; Required = required; } public string TopicId { get; } public bool Required { get; } }
    public sealed class ArmyReadinessDrilldownItem { public ArmyReadinessDrilldownItem(string signalId, FairnessDrilldownCause cause, IReadOnlyList<string> evidenceRefs, IReadOnlyList<string> missingInputs, string fairnessImpact, PayToWinRiskDrilldown payToWinRisk, ArmyServerAuthorityMarker serverAuthorityMarker, IReadOnlyList<CompetitionActivationBlocker> activationBlockers) { SignalId = signalId ?? string.Empty; Cause = cause; EvidenceRefs = evidenceRefs ?? Array.Empty<string>(); MissingInputs = missingInputs ?? Array.Empty<string>(); FairnessImpact = fairnessImpact ?? string.Empty; PayToWinRisk = payToWinRisk; ServerAuthorityMarker = serverAuthorityMarker; ActivationBlockers = activationBlockers ?? Array.Empty<CompetitionActivationBlocker>(); } public string SignalId { get; } public FairnessDrilldownCause Cause { get; } public IReadOnlyList<string> EvidenceRefs { get; } public IReadOnlyList<string> MissingInputs { get; } public string FairnessImpact { get; } public PayToWinRiskDrilldown PayToWinRisk { get; } public ArmyServerAuthorityMarker ServerAuthorityMarker { get; } public IReadOnlyList<CompetitionActivationBlocker> ActivationBlockers { get; } }
    public sealed class ArmyCompetitionDrilldownLens
    {
        public ArmyCompetitionDrilldownLens(string lensId, IReadOnlyList<ArmyReadinessDrilldownItem> items) { LensId = ColonyIntegrationIds.Require(lensId); Items = items ?? Array.Empty<ArmyReadinessDrilldownItem>(); }
        public string LensId { get; } public IReadOnlyList<ArmyReadinessDrilldownItem> Items { get; }
        public ArmyDrilldownDiagnostics Evaluate()
        {
            var findings = new List<ArmyDrilldownDiagnosticCode>();
            if (Items.Count == 0 || Items.Any(i => string.IsNullOrWhiteSpace(i.SignalId))) findings.Add(ArmyDrilldownDiagnosticCode.ArmyDrilldownSignalMissing);
            if (Items.SelectMany(i => i.ActivationBlockers).Any(b => b.OfficialScore)) findings.Add(ArmyDrilldownDiagnosticCode.ArmyDrilldownOfficialScoreForbidden);
            if (Items.Any(i => i.PayToWinRisk != null && i.PayToWinRisk.Open)) findings.Add(ArmyDrilldownDiagnosticCode.ArmyDrilldownPayToWinRiskOpen);
            if (Items.SelectMany(i => i.ActivationBlockers).Any(b => b.CombatActivation)) findings.Add(ArmyDrilldownDiagnosticCode.ArmyDrilldownCombatActivationForbidden);
            if (Items.Any(i => i.ServerAuthorityMarker == null || i.ServerAuthorityMarker.Required)) findings.Add(ArmyDrilldownDiagnosticCode.ArmyDrilldownServerAuthorityRequired);
            return new ArmyDrilldownDiagnostics(findings);
        }
    }
    public sealed class ArmyDrilldownDiagnostics { public ArmyDrilldownDiagnostics(IReadOnlyList<ArmyDrilldownDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyDrilldownDiagnosticCode>(); } public IReadOnlyList<ArmyDrilldownDiagnosticCode> Findings { get; } public bool Contains(ArmyDrilldownDiagnosticCode code) { return Findings.Contains(code); } }

    public enum LiveOpsTimelineMockMarker { MockOnly, NoPublishedDate, NoRegistration, NoReward, NoNotification, NoMonetization, ServerReviewRequired }
    public enum LiveOpsTimelineDiagnosticCode { LiveOpsTimelineCalendarForbidden, LiveOpsTimelineRegistrationForbidden, LiveOpsTimelineRewardForbidden, LiveOpsTimelineNotificationForbidden, LiveOpsTimelineMonetizationForbidden }
    public sealed class LiveOpsTimelineNonCalendarMarker { public LiveOpsTimelineNonCalendarMarker(bool publishedDateRequested = false) { PublishedDateRequested = publishedDateRequested; } public bool PublishedDateRequested { get; } }
    public sealed class LiveOpsTimelineReviewDependency { public LiveOpsTimelineReviewDependency(string dependencyId) { DependencyId = dependencyId ?? string.Empty; } public string DependencyId { get; } }
    public sealed class LiveOpsTimelineActivationBlocker { public LiveOpsTimelineActivationBlocker(string blockerId, bool registration = false, bool reward = false, bool notification = false, bool monetization = false) { BlockerId = blockerId ?? string.Empty; Registration = registration; Reward = reward; Notification = notification; Monetization = monetization; } public string BlockerId { get; } public bool Registration { get; } public bool Reward { get; } public bool Notification { get; } public bool Monetization { get; } }
    public sealed class LiveOpsTimelinePlayerPromiseGuard { public LiveOpsTimelinePlayerPromiseGuard(bool activePromiseClaimed) { ActivePromiseClaimed = activePromiseClaimed; } public bool ActivePromiseClaimed { get; } }
    public sealed class LiveOpsTimelineMockSlot { public LiveOpsTimelineMockSlot(string slotId, string candidateId, int reviewOrder, IReadOnlyList<LiveOpsTimelineMockMarker> mockMarkers, string playerValue, IReadOnlyList<LiveOpsTimelineActivationBlocker> activationBlockers, IReadOnlyList<LiveOpsTimelineReviewDependency> serverDependencies, LiveOpsTimelineNonCalendarMarker nonCalendarMarker, LiveOpsTimelinePlayerPromiseGuard promiseGuard) { SlotId = slotId ?? string.Empty; CandidateId = candidateId ?? string.Empty; ReviewOrder = reviewOrder; MockMarkers = mockMarkers ?? Array.Empty<LiveOpsTimelineMockMarker>(); PlayerValue = playerValue ?? string.Empty; ActivationBlockers = activationBlockers ?? Array.Empty<LiveOpsTimelineActivationBlocker>(); ServerDependencies = serverDependencies ?? Array.Empty<LiveOpsTimelineReviewDependency>(); NonCalendarMarker = nonCalendarMarker; PromiseGuard = promiseGuard; } public string SlotId { get; } public string CandidateId { get; } public int ReviewOrder { get; } public IReadOnlyList<LiveOpsTimelineMockMarker> MockMarkers { get; } public string PlayerValue { get; } public IReadOnlyList<LiveOpsTimelineActivationBlocker> ActivationBlockers { get; } public IReadOnlyList<LiveOpsTimelineReviewDependency> ServerDependencies { get; } public LiveOpsTimelineNonCalendarMarker NonCalendarMarker { get; } public LiveOpsTimelinePlayerPromiseGuard PromiseGuard { get; } }
    public sealed class LiveOpsCandidateTimelineMock
    {
        public LiveOpsCandidateTimelineMock(string timelineId, IReadOnlyList<LiveOpsTimelineMockSlot> slots) { TimelineId = ColonyIntegrationIds.Require(timelineId); Slots = slots ?? Array.Empty<LiveOpsTimelineMockSlot>(); }
        public string TimelineId { get; } public IReadOnlyList<LiveOpsTimelineMockSlot> Slots { get; }
        public LiveOpsTimelineDiagnostics Evaluate()
        {
            var blockers = Slots.SelectMany(s => s.ActivationBlockers).ToArray();
            var findings = new List<LiveOpsTimelineDiagnosticCode>();
            if (Slots.Any(s => s.NonCalendarMarker == null || s.NonCalendarMarker.PublishedDateRequested || (s.PromiseGuard != null && s.PromiseGuard.ActivePromiseClaimed))) findings.Add(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineCalendarForbidden);
            if (blockers.Any(b => b.Registration)) findings.Add(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineRegistrationForbidden);
            if (blockers.Any(b => b.Reward)) findings.Add(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineRewardForbidden);
            if (blockers.Any(b => b.Notification)) findings.Add(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineNotificationForbidden);
            if (blockers.Any(b => b.Monetization)) findings.Add(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineMonetizationForbidden);
            return new LiveOpsTimelineDiagnostics(findings);
        }
    }
    public sealed class LiveOpsTimelineDiagnostics { public LiveOpsTimelineDiagnostics(IReadOnlyList<LiveOpsTimelineDiagnosticCode> findings) { Findings = findings ?? Array.Empty<LiveOpsTimelineDiagnosticCode>(); } public IReadOnlyList<LiveOpsTimelineDiagnosticCode> Findings { get; } public bool Contains(LiveOpsTimelineDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ModerationWalkthroughStepKind { SignalReceived, RedactedEvidence, MissingContext, PossibleFalsePositive, OwnerAssigned, ServerHandoffRequired, SanctionForbidden }
    public enum ModerationWalkthroughDiagnosticCode { ModerationWalkthroughRawEvidenceForbidden, ModerationWalkthroughFalsePositiveMissing, ModerationWalkthroughOwnerMissing, ModerationWalkthroughSanctionForbidden, ModerationWalkthroughServerHandoffRequired }
    public sealed class ModerationWalkthroughRedactionCheck { public ModerationWalkthroughRedactionCheck(bool redacted, bool rawEvidenceVisible = false) { Redacted = redacted; RawEvidenceVisible = rawEvidenceVisible; } public bool Redacted { get; } public bool RawEvidenceVisible { get; } }
    public sealed class ModerationWalkthroughFalsePositiveCheck { public ModerationWalkthroughFalsePositiveCheck(bool present) { Present = present; } public bool Present { get; } }
    public sealed class ModerationWalkthroughOwnerAction { public ModerationWalkthroughOwnerAction(string ownerRole) { OwnerRole = ownerRole ?? string.Empty; } public string OwnerRole { get; } }
    public sealed class ModerationWalkthroughServerHandoffMarker { public ModerationWalkthroughServerHandoffMarker(bool required) { Required = required; } public bool Required { get; } }
    public sealed class ModerationWalkthroughStep { public ModerationWalkthroughStep(string stepId, ModerationWalkthroughStepKind stepKind, ModerationWalkthroughRedactionCheck redactionCheck, ModerationWalkthroughFalsePositiveCheck falsePositiveCheck, ModerationWalkthroughOwnerAction ownerAction, string missingContext, ModerationWalkthroughServerHandoffMarker serverHandoffMarker, bool forbiddenOutcome = false) { StepId = stepId ?? string.Empty; StepKind = stepKind; RedactionCheck = redactionCheck; FalsePositiveCheck = falsePositiveCheck; OwnerAction = ownerAction; MissingContext = missingContext ?? string.Empty; ServerHandoffMarker = serverHandoffMarker; ForbiddenOutcome = forbiddenOutcome; } public string StepId { get; } public ModerationWalkthroughStepKind StepKind { get; } public ModerationWalkthroughRedactionCheck RedactionCheck { get; } public ModerationWalkthroughFalsePositiveCheck FalsePositiveCheck { get; } public ModerationWalkthroughOwnerAction OwnerAction { get; } public string MissingContext { get; } public ModerationWalkthroughServerHandoffMarker ServerHandoffMarker { get; } public bool ForbiddenOutcome { get; } }
    public sealed class ModerationReviewCaseWalkthrough
    {
        public ModerationReviewCaseWalkthrough(string walkthroughId, IReadOnlyList<ModerationWalkthroughStep> steps) { WalkthroughId = ColonyIntegrationIds.Require(walkthroughId); Steps = steps ?? Array.Empty<ModerationWalkthroughStep>(); }
        public string WalkthroughId { get; } public IReadOnlyList<ModerationWalkthroughStep> Steps { get; }
        public ModerationWalkthroughDiagnostics Evaluate()
        {
            var findings = new List<ModerationWalkthroughDiagnosticCode>();
            if (Steps.Any(s => s.RedactionCheck == null || s.RedactionCheck.RawEvidenceVisible || !s.RedactionCheck.Redacted)) findings.Add(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughRawEvidenceForbidden);
            if (Steps.Any(s => s.FalsePositiveCheck == null || !s.FalsePositiveCheck.Present)) findings.Add(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughFalsePositiveMissing);
            if (Steps.Any(s => s.OwnerAction == null || string.IsNullOrWhiteSpace(s.OwnerAction.OwnerRole))) findings.Add(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughOwnerMissing);
            if (Steps.Any(s => s.ForbiddenOutcome || s.StepKind == ModerationWalkthroughStepKind.SanctionForbidden)) findings.Add(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughSanctionForbidden);
            if (Steps.Any(s => s.ServerHandoffMarker != null && s.ServerHandoffMarker.Required)) findings.Add(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughServerHandoffRequired);
            return new ModerationWalkthroughDiagnostics(findings);
        }
    }
    public sealed class ModerationWalkthroughDiagnostics { public ModerationWalkthroughDiagnostics(IReadOnlyList<ModerationWalkthroughDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ModerationWalkthroughDiagnosticCode>(); } public IReadOnlyList<ModerationWalkthroughDiagnosticCode> Findings { get; } public bool Contains(ModerationWalkthroughDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ServerEscalationDiagnosticCode { ServerEscalationTopicMissing, ServerEscalationAuthorityReasonMissing, ServerEscalationSpecCreationForbidden, ServerEscalationBackendPriorityForbidden, ServerEscalationLocalRuntimeClaimDetected }
    public sealed class ServerEscalationAuthorityReason { public ServerEscalationAuthorityReason(string reasonId) { ReasonId = reasonId ?? string.Empty; } public string ReasonId { get; } }
    public sealed class ServerEscalationLocalBlocker { public ServerEscalationLocalBlocker(string blockerId, bool localRuntimeClaim = false) { BlockerId = blockerId ?? string.Empty; LocalRuntimeClaim = localRuntimeClaim; } public string BlockerId { get; } public bool LocalRuntimeClaim { get; } }
    public sealed class ServerEscalationReadinessQuestion { public ServerEscalationReadinessQuestion(string questionId) { QuestionId = questionId ?? string.Empty; } public string QuestionId { get; } }
    public sealed class ServerEscalationNonPriorityFlag { public ServerEscalationNonPriorityFlag(bool backendPriorityClaimed = false, bool serverSpecCreationRequested = false) { BackendPriorityClaimed = backendPriorityClaimed; ServerSpecCreationRequested = serverSpecCreationRequested; } public bool BackendPriorityClaimed { get; } public bool ServerSpecCreationRequested { get; } }
    public sealed class ServerEscalationReviewTopic { public ServerEscalationReviewTopic(string topicId, IReadOnlyList<string> sourceBeeIds, ServerEscalationAuthorityReason authorityReason, IReadOnlyList<ServerEscalationLocalBlocker> localBlockers, IReadOnlyList<ServerEscalationReadinessQuestion> readinessQuestions, IReadOnlyList<string> forbiddenLocalClaims, ServerEscalationNonPriorityFlag nonPriorityFlag) { TopicId = topicId ?? string.Empty; SourceBeeIds = sourceBeeIds ?? Array.Empty<string>(); AuthorityReason = authorityReason; LocalBlockers = localBlockers ?? Array.Empty<ServerEscalationLocalBlocker>(); ReadinessQuestions = readinessQuestions ?? Array.Empty<ServerEscalationReadinessQuestion>(); ForbiddenLocalClaims = forbiddenLocalClaims ?? Array.Empty<string>(); NonPriorityFlag = nonPriorityFlag; } public string TopicId { get; } public IReadOnlyList<string> SourceBeeIds { get; } public ServerEscalationAuthorityReason AuthorityReason { get; } public IReadOnlyList<ServerEscalationLocalBlocker> LocalBlockers { get; } public IReadOnlyList<ServerEscalationReadinessQuestion> ReadinessQuestions { get; } public IReadOnlyList<string> ForbiddenLocalClaims { get; } public ServerEscalationNonPriorityFlag NonPriorityFlag { get; } }
    public sealed class SocialMmoServerEscalationReviewAlignment
    {
        public SocialMmoServerEscalationReviewAlignment(string alignmentId, IReadOnlyList<ServerEscalationReviewTopic> topics) { AlignmentId = ColonyIntegrationIds.Require(alignmentId); Topics = topics ?? Array.Empty<ServerEscalationReviewTopic>(); }
        public string AlignmentId { get; } public IReadOnlyList<ServerEscalationReviewTopic> Topics { get; }
        public ServerEscalationAlignmentDiagnostics Evaluate()
        {
            var findings = new List<ServerEscalationDiagnosticCode>();
            if (Topics.Count == 0 || Topics.Any(t => string.IsNullOrWhiteSpace(t.TopicId))) findings.Add(ServerEscalationDiagnosticCode.ServerEscalationTopicMissing);
            if (Topics.Any(t => t.AuthorityReason == null || string.IsNullOrWhiteSpace(t.AuthorityReason.ReasonId))) findings.Add(ServerEscalationDiagnosticCode.ServerEscalationAuthorityReasonMissing);
            if (Topics.Any(t => t.NonPriorityFlag != null && t.NonPriorityFlag.ServerSpecCreationRequested)) findings.Add(ServerEscalationDiagnosticCode.ServerEscalationSpecCreationForbidden);
            if (Topics.Any(t => t.NonPriorityFlag != null && t.NonPriorityFlag.BackendPriorityClaimed)) findings.Add(ServerEscalationDiagnosticCode.ServerEscalationBackendPriorityForbidden);
            if (Topics.SelectMany(t => t.LocalBlockers).Any(b => b.LocalRuntimeClaim) || Topics.Any(t => t.ForbiddenLocalClaims.Count > 0)) findings.Add(ServerEscalationDiagnosticCode.ServerEscalationLocalRuntimeClaimDetected);
            return new ServerEscalationAlignmentDiagnostics(findings);
        }
    }
    public sealed class ServerEscalationAlignmentDiagnostics { public ServerEscalationAlignmentDiagnostics(IReadOnlyList<ServerEscalationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ServerEscalationDiagnosticCode>(); } public IReadOnlyList<ServerEscalationDiagnosticCode> Findings { get; } public bool Contains(ServerEscalationDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ReviewNavigationClosureVerdict { ReadyForArchitectValidation, ReadyWithNavigationWarnings, NeedsPlannerRevision, BlockedByDemoBindingGap, BlockedByPrivacyViewerRisk, BlockedByServerAlignmentGap, BlockedByRuntimeClaim, BlockedByBee391Premature }
    public enum NavigationClosureDiagnosticCode { NavigationClosureInputMissing, NavigationClosureDemoBindingGap, NavigationClosurePrivacyRiskOpen, NavigationClosureServerAlignmentGap, NavigationClosureRuntimeClaimDetected, Bee391Premature }
    public sealed class ReviewNavigationClosureInput { public ReviewNavigationClosureInput(SocialMmoReviewNavigationMap navigationMap, SocialMmoDemoBindingContract demoBinding, GovernanceExportDiffReview governanceDiff, PrivacySafeEvidenceViewer privacyViewer, AlliancePvpScenarioPreviewLens pvpScenarioPreview, ArmyCompetitionDrilldownLens armyDrilldown, LiveOpsCandidateTimelineMock liveOpsTimelineMock, ModerationReviewCaseWalkthrough moderationWalkthrough, SocialMmoServerEscalationReviewAlignment serverEscalationAlignment) { NavigationMap = navigationMap; DemoBinding = demoBinding; GovernanceDiff = governanceDiff; PrivacyViewer = privacyViewer; PvpScenarioPreview = pvpScenarioPreview; ArmyDrilldown = armyDrilldown; LiveOpsTimelineMock = liveOpsTimelineMock; ModerationWalkthrough = moderationWalkthrough; ServerEscalationAlignment = serverEscalationAlignment; } public SocialMmoReviewNavigationMap NavigationMap { get; } public SocialMmoDemoBindingContract DemoBinding { get; } public GovernanceExportDiffReview GovernanceDiff { get; } public PrivacySafeEvidenceViewer PrivacyViewer { get; } public AlliancePvpScenarioPreviewLens PvpScenarioPreview { get; } public ArmyCompetitionDrilldownLens ArmyDrilldown { get; } public LiveOpsCandidateTimelineMock LiveOpsTimelineMock { get; } public ModerationReviewCaseWalkthrough ModerationWalkthrough { get; } public SocialMmoServerEscalationReviewAlignment ServerEscalationAlignment { get; } }
    public sealed class ReviewNavigationClosureCoverage { public ReviewNavigationClosureCoverage(bool demoBindingGap = false, bool privacyRiskOpen = false, bool runtimeClaim = false) { DemoBindingGap = demoBindingGap; PrivacyRiskOpen = privacyRiskOpen; RuntimeClaim = runtimeClaim; } public bool DemoBindingGap { get; } public bool PrivacyRiskOpen { get; } public bool RuntimeClaim { get; } }
    public sealed class ReviewNavigationClosureBlocker { public ReviewNavigationClosureBlocker(string blockerId, bool serverAlignmentGap = false) { BlockerId = blockerId ?? string.Empty; ServerAlignmentGap = serverAlignmentGap; } public string BlockerId { get; } public bool ServerAlignmentGap { get; } }
    public sealed class Bee391BlockerStatus { public Bee391BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class SocialMmoReviewNavigationClosureGate
    {
        public const string Bee391BlockedMessage = "BEE-391 bloquee jusqu'a validation architecte.";
        public SocialMmoReviewNavigationClosureGate(string gateId, ReviewNavigationClosureInput input, ReviewNavigationClosureCoverage coverage, IReadOnlyList<ReviewNavigationClosureBlocker> blockers, Bee391BlockerStatus bee391BlockerStatus) { GateId = ColonyIntegrationIds.Require(gateId); Input = input; Coverage = coverage ?? new ReviewNavigationClosureCoverage(); Blockers = blockers ?? Array.Empty<ReviewNavigationClosureBlocker>(); Bee391BlockerStatus = bee391BlockerStatus ?? new Bee391BlockerStatus(false, Bee391BlockedMessage); }
        public string GateId { get; } public ReviewNavigationClosureInput Input { get; } public ReviewNavigationClosureCoverage Coverage { get; } public IReadOnlyList<ReviewNavigationClosureBlocker> Blockers { get; } public Bee391BlockerStatus Bee391BlockerStatus { get; }
        public SocialMmoReviewNavigationClosureDiagnostics Evaluate()
        {
            var findings = new List<NavigationClosureDiagnosticCode>();
            if (Input == null || Input.NavigationMap == null || Input.DemoBinding == null || Input.GovernanceDiff == null || Input.PrivacyViewer == null || Input.PvpScenarioPreview == null || Input.ArmyDrilldown == null || Input.LiveOpsTimelineMock == null || Input.ModerationWalkthrough == null || Input.ServerEscalationAlignment == null) findings.Add(NavigationClosureDiagnosticCode.NavigationClosureInputMissing);
            if (Coverage.DemoBindingGap) findings.Add(NavigationClosureDiagnosticCode.NavigationClosureDemoBindingGap);
            if (Coverage.PrivacyRiskOpen) findings.Add(NavigationClosureDiagnosticCode.NavigationClosurePrivacyRiskOpen);
            if (Blockers.Any(b => b.ServerAlignmentGap)) findings.Add(NavigationClosureDiagnosticCode.NavigationClosureServerAlignmentGap);
            if (Coverage.RuntimeClaim) findings.Add(NavigationClosureDiagnosticCode.NavigationClosureRuntimeClaimDetected);
            if (Bee391BlockerStatus.PrematureAttempt) findings.Add(NavigationClosureDiagnosticCode.Bee391Premature);
            return new SocialMmoReviewNavigationClosureDiagnostics(ResolveVerdict(findings), findings);
        }
        private static ReviewNavigationClosureVerdict ResolveVerdict(IReadOnlyList<NavigationClosureDiagnosticCode> findings)
        {
            if (findings.Contains(NavigationClosureDiagnosticCode.Bee391Premature)) return ReviewNavigationClosureVerdict.BlockedByBee391Premature;
            if (findings.Contains(NavigationClosureDiagnosticCode.NavigationClosureRuntimeClaimDetected)) return ReviewNavigationClosureVerdict.BlockedByRuntimeClaim;
            if (findings.Contains(NavigationClosureDiagnosticCode.NavigationClosureServerAlignmentGap)) return ReviewNavigationClosureVerdict.BlockedByServerAlignmentGap;
            if (findings.Contains(NavigationClosureDiagnosticCode.NavigationClosurePrivacyRiskOpen)) return ReviewNavigationClosureVerdict.BlockedByPrivacyViewerRisk;
            if (findings.Contains(NavigationClosureDiagnosticCode.NavigationClosureDemoBindingGap)) return ReviewNavigationClosureVerdict.BlockedByDemoBindingGap;
            if (findings.Contains(NavigationClosureDiagnosticCode.NavigationClosureInputMissing)) return ReviewNavigationClosureVerdict.NeedsPlannerRevision;
            return findings.Count == 0 ? ReviewNavigationClosureVerdict.ReadyForArchitectValidation : ReviewNavigationClosureVerdict.ReadyWithNavigationWarnings;
        }
    }
    public sealed class SocialMmoReviewNavigationClosureDiagnostics { public SocialMmoReviewNavigationClosureDiagnostics(ReviewNavigationClosureVerdict verdict, IReadOnlyList<NavigationClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<NavigationClosureDiagnosticCode>(); } public ReviewNavigationClosureVerdict Verdict { get; } public IReadOnlyList<NavigationClosureDiagnosticCode> Findings { get; } public bool Contains(NavigationClosureDiagnosticCode code) { return Findings.Contains(code); } }
}
