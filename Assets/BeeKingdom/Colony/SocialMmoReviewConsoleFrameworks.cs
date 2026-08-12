using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum SocialMmoReviewPanelType { QaIntakeSummary, SocialSignalSummary, AllianceActivitySummary, ArmyPvPBalanceSummary, AbuseWarningSummary, LiveOpsCandidateSummary, CompetitionReadinessSummary, ToolPermissionSummary, ScenarioHandoffSummary, ToolingGateSummary }
    public enum SocialMmoReviewPanelState { Available, Partial, Missing, BlockedByServer, BlockedByPrivacy, OutOfScope }
    public enum ReviewConsoleDiagnosticCode { ReviewConsoleInputMissing, ReviewConsolePanelUnowned, ReviewConsoleMutationForbidden, ReviewConsoleLiveAdminForbidden, ReviewConsoleLocalTruthRisk }
    public sealed class SocialMmoReviewConsoleOwnerRef { public SocialMmoReviewConsoleOwnerRef(string ownerId) { OwnerId = ownerId ?? string.Empty; } public string OwnerId { get; } }
    public sealed class SocialMmoReviewConsoleGap { public SocialMmoReviewConsoleGap(string gapId, bool open) { GapId = gapId ?? string.Empty; Open = open; } public string GapId { get; } public bool Open { get; } }
    public sealed class SocialMmoReviewConsoleBlockedAction { public SocialMmoReviewConsoleBlockedAction(string actionId, bool mutationRequested = false, bool liveAdminRequested = false, bool localTruthClaimed = false) { ActionId = actionId ?? string.Empty; MutationRequested = mutationRequested; LiveAdminRequested = liveAdminRequested; LocalTruthClaimed = localTruthClaimed; } public string ActionId { get; } public bool MutationRequested { get; } public bool LiveAdminRequested { get; } public bool LocalTruthClaimed { get; } }
    public sealed class SocialMmoReviewConsoleInput
    {
        public SocialMmoReviewConsoleInput(string sourceBeeId, SocialMmoReviewPanelType panelType, IReadOnlyList<string> evidenceRefs, SocialMmoReviewConsoleOwnerRef owner, string freshnessStatus, IReadOnlyList<SocialMmoReviewConsoleGap> knownGaps, IReadOnlyList<string> serverDependencies, IReadOnlyList<SocialMmoReviewConsoleBlockedAction> blockedActions, SocialMmoReviewPanelState state = SocialMmoReviewPanelState.Available)
        {
            SourceBeeId = sourceBeeId ?? string.Empty; PanelType = panelType; EvidenceRefs = evidenceRefs ?? Array.Empty<string>(); Owner = owner; FreshnessStatus = freshnessStatus ?? string.Empty; KnownGaps = knownGaps ?? Array.Empty<SocialMmoReviewConsoleGap>(); ServerDependencies = serverDependencies ?? Array.Empty<string>(); BlockedActions = blockedActions ?? Array.Empty<SocialMmoReviewConsoleBlockedAction>(); State = state;
        }
        public string SourceBeeId { get; } public SocialMmoReviewPanelType PanelType { get; } public IReadOnlyList<string> EvidenceRefs { get; } public SocialMmoReviewConsoleOwnerRef Owner { get; } public string FreshnessStatus { get; } public IReadOnlyList<SocialMmoReviewConsoleGap> KnownGaps { get; } public IReadOnlyList<string> ServerDependencies { get; } public IReadOnlyList<SocialMmoReviewConsoleBlockedAction> BlockedActions { get; } public SocialMmoReviewPanelState State { get; }
    }
    public sealed class SocialMmoReviewConsolePanel { public SocialMmoReviewConsolePanel(SocialMmoReviewPanelType panelType, bool enabled) { PanelType = panelType; Enabled = enabled; } public SocialMmoReviewPanelType PanelType { get; } public bool Enabled { get; } }
    public sealed class SocialMmoReviewConsoleBoundary
    {
        public SocialMmoReviewConsoleBoundary(string consoleId, IReadOnlyList<SocialMmoReviewConsoleInput> inputs, IReadOnlyList<SocialMmoReviewConsolePanel> panels) { ConsoleId = ColonyIntegrationIds.Require(consoleId); Inputs = inputs ?? Array.Empty<SocialMmoReviewConsoleInput>(); Panels = panels ?? Array.Empty<SocialMmoReviewConsolePanel>(); }
        public string ConsoleId { get; } public IReadOnlyList<SocialMmoReviewConsoleInput> Inputs { get; } public IReadOnlyList<SocialMmoReviewConsolePanel> Panels { get; }
        public ReviewConsoleDiagnostics Evaluate()
        {
            var findings = new List<ReviewConsoleDiagnosticCode>();
            if (Inputs.Count == 0 || Inputs.Any(i => string.IsNullOrWhiteSpace(i.SourceBeeId) || i.EvidenceRefs.Count == 0 || i.State == SocialMmoReviewPanelState.Missing)) findings.Add(ReviewConsoleDiagnosticCode.ReviewConsoleInputMissing);
            if (Inputs.Any(i => i.Owner == null || string.IsNullOrWhiteSpace(i.Owner.OwnerId))) findings.Add(ReviewConsoleDiagnosticCode.ReviewConsolePanelUnowned);
            if (Inputs.SelectMany(i => i.BlockedActions).Any(a => a.MutationRequested)) findings.Add(ReviewConsoleDiagnosticCode.ReviewConsoleMutationForbidden);
            if (Inputs.SelectMany(i => i.BlockedActions).Any(a => a.LiveAdminRequested)) findings.Add(ReviewConsoleDiagnosticCode.ReviewConsoleLiveAdminForbidden);
            if (Inputs.SelectMany(i => i.BlockedActions).Any(a => a.LocalTruthClaimed)) findings.Add(ReviewConsoleDiagnosticCode.ReviewConsoleLocalTruthRisk);
            return new ReviewConsoleDiagnostics(findings);
        }
    }
    public sealed class ReviewConsoleDiagnostics { public ReviewConsoleDiagnostics(IReadOnlyList<ReviewConsoleDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ReviewConsoleDiagnosticCode>(); } public IReadOnlyList<ReviewConsoleDiagnosticCode> Findings { get; } public bool Contains(ReviewConsoleDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoEvidenceAgeBand { CurrentSession, RecentBuild, PreviousLot, Obsolete, UnknownAge }
    public enum SocialMmoEvidenceReliability { High, Medium, Low, Unknown }
    public enum SocialMmoEvidenceOwnerStatus { Assigned, Missing, ServerOnly }
    public enum SocialMmoEvidenceFreshnessStatus { Fresh, Stale, MissingSource, MissingOwner, BlockedByServer, SensitiveRestricted }
    public enum EvidenceFreshnessDiagnosticCode { EvidenceSourceMissing, EvidenceOwnerMissing, EvidenceFreshnessUnknown, EvidenceObsolete, EvidenceServerAuditRequired }
    public sealed class SocialMmoEvidenceInvalidationReason { public SocialMmoEvidenceInvalidationReason(string reasonId, bool obsolete = false, bool serverAuditRequired = false) { ReasonId = reasonId ?? string.Empty; Obsolete = obsolete; ServerAuditRequired = serverAuditRequired; } public string ReasonId { get; } public bool Obsolete { get; } public bool ServerAuditRequired { get; } }
    public sealed class SocialMmoEvidenceSourceRef
    {
        public SocialMmoEvidenceSourceRef(string evidenceId, string sourceKind, string sourcePathOrBee, string sourceLot, string ownerRole, SocialMmoEvidenceAgeBand freshnessBand, SocialMmoEvidenceReliability reliability, IReadOnlyList<SocialMmoEvidenceInvalidationReason> invalidationReasons, bool sensitiveRestricted = false)
        {
            EvidenceId = evidenceId ?? string.Empty; SourceKind = sourceKind ?? string.Empty; SourcePathOrBee = sourcePathOrBee ?? string.Empty; SourceLot = sourceLot ?? string.Empty; OwnerRole = ownerRole ?? string.Empty; FreshnessBand = freshnessBand; Reliability = reliability; InvalidationReasons = invalidationReasons ?? Array.Empty<SocialMmoEvidenceInvalidationReason>(); SensitiveRestricted = sensitiveRestricted;
        }
        public string EvidenceId { get; } public string SourceKind { get; } public string SourcePathOrBee { get; } public string SourceLot { get; } public string OwnerRole { get; } public SocialMmoEvidenceAgeBand FreshnessBand { get; } public SocialMmoEvidenceReliability Reliability { get; } public IReadOnlyList<SocialMmoEvidenceInvalidationReason> InvalidationReasons { get; } public bool SensitiveRestricted { get; }
    }
    public sealed class SocialMmoEvidenceFreshnessAudit
    {
        public SocialMmoEvidenceFreshnessAudit(string auditId, IReadOnlyList<SocialMmoEvidenceSourceRef> sources) { AuditId = ColonyIntegrationIds.Require(auditId); Sources = sources ?? Array.Empty<SocialMmoEvidenceSourceRef>(); }
        public string AuditId { get; } public IReadOnlyList<SocialMmoEvidenceSourceRef> Sources { get; }
        public EvidenceFreshnessDiagnostics Evaluate()
        {
            var findings = new List<EvidenceFreshnessDiagnosticCode>();
            if (Sources.Count == 0 || Sources.Any(s => string.IsNullOrWhiteSpace(s.SourceKind) || string.IsNullOrWhiteSpace(s.SourcePathOrBee))) findings.Add(EvidenceFreshnessDiagnosticCode.EvidenceSourceMissing);
            if (Sources.Any(s => string.IsNullOrWhiteSpace(s.OwnerRole))) findings.Add(EvidenceFreshnessDiagnosticCode.EvidenceOwnerMissing);
            if (Sources.Any(s => s.FreshnessBand == SocialMmoEvidenceAgeBand.UnknownAge || s.Reliability == SocialMmoEvidenceReliability.Unknown)) findings.Add(EvidenceFreshnessDiagnosticCode.EvidenceFreshnessUnknown);
            if (Sources.Any(s => s.FreshnessBand == SocialMmoEvidenceAgeBand.Obsolete || s.InvalidationReasons.Any(r => r.Obsolete))) findings.Add(EvidenceFreshnessDiagnosticCode.EvidenceObsolete);
            if (Sources.Any(s => s.InvalidationReasons.Any(r => r.ServerAuditRequired))) findings.Add(EvidenceFreshnessDiagnosticCode.EvidenceServerAuditRequired);
            return new EvidenceFreshnessDiagnostics(findings);
        }
    }
    public sealed class EvidenceFreshnessDiagnostics { public EvidenceFreshnessDiagnostics(IReadOnlyList<EvidenceFreshnessDiagnosticCode> findings) { Findings = findings ?? Array.Empty<EvidenceFreshnessDiagnosticCode>(); } public IReadOnlyList<EvidenceFreshnessDiagnosticCode> Findings { get; } public bool Contains(EvidenceFreshnessDiagnosticCode code) { return Findings.Contains(code); } }

    public enum GovernanceExportRecipient { ArchitectReviewer, QaReviewer, ServerReviewer, WorkerImplementer, DemoReviewer }
    public enum GovernanceExportStatus { DraftGovernance, NeedsServerReview, NeedsQaReview, BlockedByPrivacy, ExportableForReview }
    public enum GovernanceExportDiagnosticCode { GovernanceExportSensitiveDataBlocked, GovernanceExportOfficialVerdictForbidden, GovernanceExportServerDependencyMissing, GovernanceExportOwnerMissing }
    public sealed class GovernanceExportDecisionItem { public GovernanceExportDecisionItem(string decisionId, bool officialVerdictClaimed = false, bool containsSensitiveData = false) { DecisionId = decisionId ?? string.Empty; OfficialVerdictClaimed = officialVerdictClaimed; ContainsSensitiveData = containsSensitiveData; } public string DecisionId { get; } public bool OfficialVerdictClaimed { get; } public bool ContainsSensitiveData { get; } }
    public sealed class GovernanceExportBlockerItem { public GovernanceExportBlockerItem(string blockerId, bool open) { BlockerId = blockerId ?? string.Empty; Open = open; } public string BlockerId { get; } public bool Open { get; } }
    public sealed class GovernanceExportOwnerAssignment { public GovernanceExportOwnerAssignment(string ownerId, GovernanceExportRecipient role) { OwnerId = ownerId ?? string.Empty; Role = role; } public string OwnerId { get; } public GovernanceExportRecipient Role { get; } }
    public sealed class GovernanceExportServerDependency { public GovernanceExportServerDependency(string topicId, bool missing = false) { TopicId = topicId ?? string.Empty; Missing = missing; } public string TopicId { get; } public bool Missing { get; } }
    public sealed class GovernanceExportRedactionPolicy { public GovernanceExportRedactionPolicy(bool required, bool applied) { Required = required; Applied = applied; } public bool Required { get; } public bool Applied { get; } }
    public sealed class AlliancePvpGovernanceExport
    {
        public AlliancePvpGovernanceExport(string exportId, string generatedFromConsoleView, IReadOnlyList<GovernanceExportDecisionItem> decisionItems, IReadOnlyList<GovernanceExportBlockerItem> blockerItems, IReadOnlyList<GovernanceExportOwnerAssignment> ownerAssignments, IReadOnlyList<GovernanceExportServerDependency> serverDependencies, GovernanceExportRedactionPolicy redactionPolicy, GovernanceExportStatus exportStatus)
        {
            ExportId = ColonyIntegrationIds.Require(exportId); GeneratedFromConsoleView = generatedFromConsoleView ?? string.Empty; DecisionItems = decisionItems ?? Array.Empty<GovernanceExportDecisionItem>(); BlockerItems = blockerItems ?? Array.Empty<GovernanceExportBlockerItem>(); OwnerAssignments = ownerAssignments ?? Array.Empty<GovernanceExportOwnerAssignment>(); ServerDependencies = serverDependencies ?? Array.Empty<GovernanceExportServerDependency>(); RedactionPolicy = redactionPolicy; ExportStatus = exportStatus;
        }
        public string ExportId { get; } public string GeneratedFromConsoleView { get; } public IReadOnlyList<GovernanceExportDecisionItem> DecisionItems { get; } public IReadOnlyList<GovernanceExportBlockerItem> BlockerItems { get; } public IReadOnlyList<GovernanceExportOwnerAssignment> OwnerAssignments { get; } public IReadOnlyList<GovernanceExportServerDependency> ServerDependencies { get; } public GovernanceExportRedactionPolicy RedactionPolicy { get; } public GovernanceExportStatus ExportStatus { get; }
        public GovernanceExportDiagnostics Evaluate()
        {
            var findings = new List<GovernanceExportDiagnosticCode>();
            if ((RedactionPolicy != null && RedactionPolicy.Required && !RedactionPolicy.Applied) || DecisionItems.Any(d => d.ContainsSensitiveData)) findings.Add(GovernanceExportDiagnosticCode.GovernanceExportSensitiveDataBlocked);
            if (DecisionItems.Any(d => d.OfficialVerdictClaimed)) findings.Add(GovernanceExportDiagnosticCode.GovernanceExportOfficialVerdictForbidden);
            if (ServerDependencies.Count == 0 || ServerDependencies.Any(d => d.Missing)) findings.Add(GovernanceExportDiagnosticCode.GovernanceExportServerDependencyMissing);
            if (OwnerAssignments.Count == 0 || OwnerAssignments.Any(o => string.IsNullOrWhiteSpace(o.OwnerId))) findings.Add(GovernanceExportDiagnosticCode.GovernanceExportOwnerMissing);
            return new GovernanceExportDiagnostics(findings);
        }
    }
    public sealed class GovernanceExportDiagnostics { public GovernanceExportDiagnostics(IReadOnlyList<GovernanceExportDiagnosticCode> findings) { Findings = findings ?? Array.Empty<GovernanceExportDiagnosticCode>(); } public IReadOnlyList<GovernanceExportDiagnosticCode> Findings { get; } public bool Contains(GovernanceExportDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SensitiveEvidenceClass { PublicReviewSafe, InternalReviewOnly, RedactedRequired, VictimProtected, ServerOnlyFuture, ForbiddenForDemo, Unclassified }
    public enum SensitiveEvidenceDiagnosticCode { SensitiveEvidenceUnclassified, SensitiveEvidenceRedactionMissing, VictimExposureRisk, SensitiveEvidenceSanctionForbidden, SensitiveEvidenceDemoForbidden }
    public sealed class SensitiveEvidenceRedactionRule { public SensitiveEvidenceRedactionRule(string ruleId, bool applied) { RuleId = ruleId ?? string.Empty; Applied = applied; } public string RuleId { get; } public bool Applied { get; } }
    public sealed class SensitiveEvidenceAccessReason { public SensitiveEvidenceAccessReason(string reasonId) { ReasonId = reasonId ?? string.Empty; } public string ReasonId { get; } }
    public sealed class SensitiveEvidenceBlockedUse { public SensitiveEvidenceBlockedUse(string useId, bool sanctionRequested = false, bool demoRequested = false) { UseId = useId ?? string.Empty; SanctionRequested = sanctionRequested; DemoRequested = demoRequested; } public string UseId { get; } public bool SanctionRequested { get; } public bool DemoRequested { get; } }
    public sealed class SensitiveEvidenceDisclosureRisk { public SensitiveEvidenceDisclosureRisk(string riskId, bool victimExposure = false) { RiskId = riskId ?? string.Empty; VictimExposure = victimExposure; } public string RiskId { get; } public bool VictimExposure { get; } }
    public sealed class SensitiveEvidenceClassification
    {
        public SensitiveEvidenceClassification(string evidenceId, SensitiveEvidenceClass sensitivityClass, IReadOnlyList<SensitiveEvidenceRedactionRule> redactionRules, IReadOnlyList<GovernanceExportRecipient> allowedReviewRoles, IReadOnlyList<string> forbiddenSurfaces, IReadOnlyList<SensitiveEvidenceDisclosureRisk> disclosureRisks, string serverOnlyReason, IReadOnlyList<SensitiveEvidenceBlockedUse> blockedUses)
        {
            EvidenceId = evidenceId ?? string.Empty; SensitivityClass = sensitivityClass; RedactionRules = redactionRules ?? Array.Empty<SensitiveEvidenceRedactionRule>(); AllowedReviewRoles = allowedReviewRoles ?? Array.Empty<GovernanceExportRecipient>(); ForbiddenSurfaces = forbiddenSurfaces ?? Array.Empty<string>(); DisclosureRisks = disclosureRisks ?? Array.Empty<SensitiveEvidenceDisclosureRisk>(); ServerOnlyReason = serverOnlyReason ?? string.Empty; BlockedUses = blockedUses ?? Array.Empty<SensitiveEvidenceBlockedUse>();
        }
        public string EvidenceId { get; } public SensitiveEvidenceClass SensitivityClass { get; } public IReadOnlyList<SensitiveEvidenceRedactionRule> RedactionRules { get; } public IReadOnlyList<GovernanceExportRecipient> AllowedReviewRoles { get; } public IReadOnlyList<string> ForbiddenSurfaces { get; } public IReadOnlyList<SensitiveEvidenceDisclosureRisk> DisclosureRisks { get; } public string ServerOnlyReason { get; } public IReadOnlyList<SensitiveEvidenceBlockedUse> BlockedUses { get; }
    }
    public sealed class SocialMmoSensitiveEvidenceBoundary
    {
        public SocialMmoSensitiveEvidenceBoundary(string boundaryId, IReadOnlyList<SensitiveEvidenceClassification> classifications) { BoundaryId = ColonyIntegrationIds.Require(boundaryId); Classifications = classifications ?? Array.Empty<SensitiveEvidenceClassification>(); }
        public string BoundaryId { get; } public IReadOnlyList<SensitiveEvidenceClassification> Classifications { get; }
        public SensitiveEvidenceDiagnostics Evaluate()
        {
            var findings = new List<SensitiveEvidenceDiagnosticCode>();
            if (Classifications.Count == 0 || Classifications.Any(c => c.SensitivityClass == SensitiveEvidenceClass.Unclassified || string.IsNullOrWhiteSpace(c.EvidenceId))) findings.Add(SensitiveEvidenceDiagnosticCode.SensitiveEvidenceUnclassified);
            if (Classifications.Any(c => (c.SensitivityClass == SensitiveEvidenceClass.RedactedRequired || c.SensitivityClass == SensitiveEvidenceClass.VictimProtected) && c.RedactionRules.Any(r => !r.Applied))) findings.Add(SensitiveEvidenceDiagnosticCode.SensitiveEvidenceRedactionMissing);
            if (Classifications.SelectMany(c => c.DisclosureRisks).Any(r => r.VictimExposure)) findings.Add(SensitiveEvidenceDiagnosticCode.VictimExposureRisk);
            if (Classifications.SelectMany(c => c.BlockedUses).Any(u => u.SanctionRequested)) findings.Add(SensitiveEvidenceDiagnosticCode.SensitiveEvidenceSanctionForbidden);
            if (Classifications.Any(c => c.SensitivityClass == SensitiveEvidenceClass.ForbiddenForDemo || c.ForbiddenSurfaces.Contains("DEMO-012")) || Classifications.SelectMany(c => c.BlockedUses).Any(u => u.DemoRequested)) findings.Add(SensitiveEvidenceDiagnosticCode.SensitiveEvidenceDemoForbidden);
            return new SensitiveEvidenceDiagnostics(findings);
        }
    }
    public sealed class SensitiveEvidenceDiagnostics { public SensitiveEvidenceDiagnostics(IReadOnlyList<SensitiveEvidenceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SensitiveEvidenceDiagnosticCode>(); } public IReadOnlyList<SensitiveEvidenceDiagnosticCode> Findings { get; } public bool Contains(SensitiveEvidenceDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ArmyCompetitionReviewVerdict { ReviewOnlyReady, NeedsArmyEvidence, NeedsFairnessReview, BlockedByServerAuthority, BlockedByAbuseRisk, BlockedByOfficialScoreClaim }
    public enum ArmyCompetitionDiagnosticCode { ArmyCompetitionEvidenceMissing, ArmyCompetitionPowerScoreForbidden, ArmyCompetitionMatchmakingForbidden, ArmyCompetitionCombatRuntimeForbidden, ArmyCompetitionServerReviewRequired }
    public sealed class ArmyCompetitionReadinessInput { public ArmyCompetitionReadinessInput(string signalId, bool officialPowerScoreClaimed = false) { SignalId = signalId ?? string.Empty; OfficialPowerScoreClaimed = officialPowerScoreClaimed; } public string SignalId { get; } public bool OfficialPowerScoreClaimed { get; } }
    public sealed class ArmyCompetitionFairnessRisk { public ArmyCompetitionFairnessRisk(string riskId, bool open) { RiskId = riskId ?? string.Empty; Open = open; } public string RiskId { get; } public bool Open { get; } }
    public sealed class ArmyCompetitionMissingCondition { public ArmyCompetitionMissingCondition(string conditionId) { ConditionId = conditionId ?? string.Empty; } public string ConditionId { get; } }
    public sealed class ArmyCompetitionServerBlocker { public ArmyCompetitionServerBlocker(string topicId, bool open) { TopicId = topicId ?? string.Empty; Open = open; } public string TopicId { get; } public bool Open { get; } }
    public sealed class ArmyCompetitionAbuseGuard { public ArmyCompetitionAbuseGuard(string guardId, bool missing) { GuardId = guardId ?? string.Empty; Missing = missing; } public string GuardId { get; } public bool Missing { get; } }
    public sealed class ArmyCompetitionReadinessReview
    {
        public ArmyCompetitionReadinessReview(string reviewId, IReadOnlyList<ArmyCompetitionReadinessInput> armySignals, AllianceCompetitionReadinessProjection allianceCompetitionProjection, IReadOnlyList<ArmyCompetitionFairnessRisk> fairnessRisks, IReadOnlyList<ArmyCompetitionMissingCondition> missingConditions, IReadOnlyList<ArmyCompetitionAbuseGuard> abuseGuards, IReadOnlyList<ArmyCompetitionServerBlocker> serverBlockers, ArmyCompetitionReviewVerdict reviewVerdict, bool matchmakingRequested = false, bool combatRuntimeRequested = false)
        {
            ReviewId = ColonyIntegrationIds.Require(reviewId); ArmySignals = armySignals ?? Array.Empty<ArmyCompetitionReadinessInput>(); AllianceCompetitionProjection = allianceCompetitionProjection; FairnessRisks = fairnessRisks ?? Array.Empty<ArmyCompetitionFairnessRisk>(); MissingConditions = missingConditions ?? Array.Empty<ArmyCompetitionMissingCondition>(); AbuseGuards = abuseGuards ?? Array.Empty<ArmyCompetitionAbuseGuard>(); ServerBlockers = serverBlockers ?? Array.Empty<ArmyCompetitionServerBlocker>(); ReviewVerdict = reviewVerdict; MatchmakingRequested = matchmakingRequested; CombatRuntimeRequested = combatRuntimeRequested;
        }
        public string ReviewId { get; } public IReadOnlyList<ArmyCompetitionReadinessInput> ArmySignals { get; } public AllianceCompetitionReadinessProjection AllianceCompetitionProjection { get; } public IReadOnlyList<ArmyCompetitionFairnessRisk> FairnessRisks { get; } public IReadOnlyList<ArmyCompetitionMissingCondition> MissingConditions { get; } public IReadOnlyList<ArmyCompetitionAbuseGuard> AbuseGuards { get; } public IReadOnlyList<ArmyCompetitionServerBlocker> ServerBlockers { get; } public ArmyCompetitionReviewVerdict ReviewVerdict { get; } public bool MatchmakingRequested { get; } public bool CombatRuntimeRequested { get; }
        public ArmyCompetitionReadinessDiagnostics Evaluate()
        {
            var findings = new List<ArmyCompetitionDiagnosticCode>();
            if (ArmySignals.Count == 0 || MissingConditions.Count > 0) findings.Add(ArmyCompetitionDiagnosticCode.ArmyCompetitionEvidenceMissing);
            if (ArmySignals.Any(s => s.OfficialPowerScoreClaimed) || ReviewVerdict == ArmyCompetitionReviewVerdict.BlockedByOfficialScoreClaim) findings.Add(ArmyCompetitionDiagnosticCode.ArmyCompetitionPowerScoreForbidden);
            if (MatchmakingRequested) findings.Add(ArmyCompetitionDiagnosticCode.ArmyCompetitionMatchmakingForbidden);
            if (CombatRuntimeRequested) findings.Add(ArmyCompetitionDiagnosticCode.ArmyCompetitionCombatRuntimeForbidden);
            if (ServerBlockers.Any(b => b.Open)) findings.Add(ArmyCompetitionDiagnosticCode.ArmyCompetitionServerReviewRequired);
            return new ArmyCompetitionReadinessDiagnostics(findings);
        }
    }
    public sealed class ArmyCompetitionReadinessDiagnostics { public ArmyCompetitionReadinessDiagnostics(IReadOnlyList<ArmyCompetitionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyCompetitionDiagnosticCode>(); } public IReadOnlyList<ArmyCompetitionDiagnosticCode> Findings { get; } public bool Contains(ArmyCompetitionDiagnosticCode code) { return Findings.Contains(code); } }

    public enum LiveOpsCandidateNonExecutionStatus { CandidateOnly, BlockedByRisk, BlockedByServer, BlockedByPrivacy, NotExecutable }
    public enum LiveOpsReviewDiagnosticCode { LiveOpsReviewCandidateMissingValue, LiveOpsReviewRewardForbidden, LiveOpsReviewCalendarForbidden, LiveOpsReviewMonetizationForbidden, LiveOpsReviewExecutionForbidden }
    public sealed class LiveOpsCandidatePlayerValue { public LiveOpsCandidatePlayerValue(string valueId) { ValueId = valueId ?? string.Empty; } public string ValueId { get; } }
    public sealed class LiveOpsCandidateOperationalRisk { public LiveOpsCandidateOperationalRisk(string riskId, bool open) { RiskId = riskId ?? string.Empty; Open = open; } public string RiskId { get; } public bool Open { get; } }
    public sealed class LiveOpsCandidateExecutionBlocker { public LiveOpsCandidateExecutionBlocker(string blockerId, bool rewardRequested = false, bool calendarRequested = false, bool monetizationRequested = false, bool executionRequested = false) { BlockerId = blockerId ?? string.Empty; RewardRequested = rewardRequested; CalendarRequested = calendarRequested; MonetizationRequested = monetizationRequested; ExecutionRequested = executionRequested; } public string BlockerId { get; } public bool RewardRequested { get; } public bool CalendarRequested { get; } public bool MonetizationRequested { get; } public bool ExecutionRequested { get; } }
    public sealed class LiveOpsCandidateReviewCard
    {
        public LiveOpsCandidateReviewCard(string candidateId, LiveOpsCandidatePlayerValue playerValue, string allianceValue, string worldValue, IReadOnlyList<LiveOpsCandidateOperationalRisk> operationalRisks, IReadOnlyList<string> privacyConstraints, IReadOnlyList<string> serverDependencies, LiveOpsCandidateNonExecutionStatus nonExecutionStatus, string nextOwner, IReadOnlyList<LiveOpsCandidateExecutionBlocker> executionBlockers)
        {
            CandidateId = candidateId ?? string.Empty; PlayerValue = playerValue; AllianceValue = allianceValue ?? string.Empty; WorldValue = worldValue ?? string.Empty; OperationalRisks = operationalRisks ?? Array.Empty<LiveOpsCandidateOperationalRisk>(); PrivacyConstraints = privacyConstraints ?? Array.Empty<string>(); ServerDependencies = serverDependencies ?? Array.Empty<string>(); NonExecutionStatus = nonExecutionStatus; NextOwner = nextOwner ?? string.Empty; ExecutionBlockers = executionBlockers ?? Array.Empty<LiveOpsCandidateExecutionBlocker>();
        }
        public string CandidateId { get; } public LiveOpsCandidatePlayerValue PlayerValue { get; } public string AllianceValue { get; } public string WorldValue { get; } public IReadOnlyList<LiveOpsCandidateOperationalRisk> OperationalRisks { get; } public IReadOnlyList<string> PrivacyConstraints { get; } public IReadOnlyList<string> ServerDependencies { get; } public LiveOpsCandidateNonExecutionStatus NonExecutionStatus { get; } public string NextOwner { get; } public IReadOnlyList<LiveOpsCandidateExecutionBlocker> ExecutionBlockers { get; }
    }
    public sealed class LiveOpsCandidateReviewBoard
    {
        public LiveOpsCandidateReviewBoard(string boardId, IReadOnlyList<LiveOpsCandidateReviewCard> cards) { BoardId = ColonyIntegrationIds.Require(boardId); Cards = cards ?? Array.Empty<LiveOpsCandidateReviewCard>(); }
        public string BoardId { get; } public IReadOnlyList<LiveOpsCandidateReviewCard> Cards { get; }
        public LiveOpsCandidateReviewDiagnostics Evaluate()
        {
            var findings = new List<LiveOpsReviewDiagnosticCode>();
            if (Cards.Count == 0 || Cards.Any(c => c.PlayerValue == null || string.IsNullOrWhiteSpace(c.PlayerValue.ValueId))) findings.Add(LiveOpsReviewDiagnosticCode.LiveOpsReviewCandidateMissingValue);
            if (Cards.SelectMany(c => c.ExecutionBlockers).Any(b => b.RewardRequested)) findings.Add(LiveOpsReviewDiagnosticCode.LiveOpsReviewRewardForbidden);
            if (Cards.SelectMany(c => c.ExecutionBlockers).Any(b => b.CalendarRequested)) findings.Add(LiveOpsReviewDiagnosticCode.LiveOpsReviewCalendarForbidden);
            if (Cards.SelectMany(c => c.ExecutionBlockers).Any(b => b.MonetizationRequested)) findings.Add(LiveOpsReviewDiagnosticCode.LiveOpsReviewMonetizationForbidden);
            if (Cards.SelectMany(c => c.ExecutionBlockers).Any(b => b.ExecutionRequested) || Cards.Any(c => c.NonExecutionStatus == LiveOpsCandidateNonExecutionStatus.NotExecutable)) findings.Add(LiveOpsReviewDiagnosticCode.LiveOpsReviewExecutionForbidden);
            return new LiveOpsCandidateReviewDiagnostics(findings);
        }
    }
    public sealed class LiveOpsCandidateReviewDiagnostics { public LiveOpsCandidateReviewDiagnostics(IReadOnlyList<LiveOpsReviewDiagnosticCode> findings) { Findings = findings ?? Array.Empty<LiveOpsReviewDiagnosticCode>(); } public IReadOnlyList<LiveOpsReviewDiagnosticCode> Findings { get; } public bool Contains(LiveOpsReviewDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ModerationHandoffDestination { QaReview, ServerReview, ArchitectReview }
    public enum ModerationHandoffNonSanctionStatus { ReadyForModerationReview, NeedsRedaction, BlockedByPrivacy, BlockedByMissingContext, ServerReviewRequired }
    public enum ModerationHandoffDiagnosticCode { ModerationHandoffRedactionMissing, ModerationHandoffVictimExposureRisk, ModerationHandoffSanctionForbidden, ModerationHandoffFalsePositiveMissing, ModerationHandoffServerReviewRequired }
    public sealed class ModerationHandoffRedactionProfile { public ModerationHandoffRedactionProfile(bool applied) { Applied = applied; } public bool Applied { get; } }
    public sealed class ModerationHandoffFalsePositiveNote { public ModerationHandoffFalsePositiveNote(string noteId) { NoteId = noteId ?? string.Empty; } public string NoteId { get; } }
    public sealed class ModerationHandoffConfidentialityFlag { public ModerationHandoffConfidentialityFlag(string flagId, bool victimExposureRisk = false) { FlagId = flagId ?? string.Empty; VictimExposureRisk = victimExposureRisk; } public string FlagId { get; } public bool VictimExposureRisk { get; } }
    public sealed class ModerationHandoffServerReviewNeed { public ModerationHandoffServerReviewNeed(string needId, bool required) { NeedId = needId ?? string.Empty; Required = required; } public string NeedId { get; } public bool Required { get; } }
    public sealed class ModerationHandoffEvidenceBundle
    {
        public ModerationHandoffEvidenceBundle(string bundleId, IReadOnlyList<string> warningRefs, IReadOnlyList<string> redactedEvidence, ModerationHandoffRedactionProfile redactionProfile, IReadOnlyList<ModerationHandoffFalsePositiveNote> falsePositiveNotes, IReadOnlyList<ModerationHandoffConfidentialityFlag> confidentialityFlags, IReadOnlyList<string> missingContext, IReadOnlyList<ModerationHandoffServerReviewNeed> serverReviewNeeds, ModerationHandoffNonSanctionStatus nonSanctionStatus, bool sanctionRequested = false)
        {
            BundleId = ColonyIntegrationIds.Require(bundleId); WarningRefs = warningRefs ?? Array.Empty<string>(); RedactedEvidence = redactedEvidence ?? Array.Empty<string>(); RedactionProfile = redactionProfile; FalsePositiveNotes = falsePositiveNotes ?? Array.Empty<ModerationHandoffFalsePositiveNote>(); ConfidentialityFlags = confidentialityFlags ?? Array.Empty<ModerationHandoffConfidentialityFlag>(); MissingContext = missingContext ?? Array.Empty<string>(); ServerReviewNeeds = serverReviewNeeds ?? Array.Empty<ModerationHandoffServerReviewNeed>(); NonSanctionStatus = nonSanctionStatus; SanctionRequested = sanctionRequested;
        }
        public string BundleId { get; } public IReadOnlyList<string> WarningRefs { get; } public IReadOnlyList<string> RedactedEvidence { get; } public ModerationHandoffRedactionProfile RedactionProfile { get; } public IReadOnlyList<ModerationHandoffFalsePositiveNote> FalsePositiveNotes { get; } public IReadOnlyList<ModerationHandoffConfidentialityFlag> ConfidentialityFlags { get; } public IReadOnlyList<string> MissingContext { get; } public IReadOnlyList<ModerationHandoffServerReviewNeed> ServerReviewNeeds { get; } public ModerationHandoffNonSanctionStatus NonSanctionStatus { get; } public bool SanctionRequested { get; }
        public ModerationHandoffDiagnostics Evaluate()
        {
            var findings = new List<ModerationHandoffDiagnosticCode>();
            if (RedactionProfile == null || !RedactionProfile.Applied || RedactedEvidence.Count == 0) findings.Add(ModerationHandoffDiagnosticCode.ModerationHandoffRedactionMissing);
            if (ConfidentialityFlags.Any(f => f.VictimExposureRisk)) findings.Add(ModerationHandoffDiagnosticCode.ModerationHandoffVictimExposureRisk);
            if (SanctionRequested) findings.Add(ModerationHandoffDiagnosticCode.ModerationHandoffSanctionForbidden);
            if (FalsePositiveNotes.Count == 0 || FalsePositiveNotes.Any(n => string.IsNullOrWhiteSpace(n.NoteId))) findings.Add(ModerationHandoffDiagnosticCode.ModerationHandoffFalsePositiveMissing);
            if (ServerReviewNeeds.Any(n => n.Required) || NonSanctionStatus == ModerationHandoffNonSanctionStatus.ServerReviewRequired) findings.Add(ModerationHandoffDiagnosticCode.ModerationHandoffServerReviewRequired);
            return new ModerationHandoffDiagnostics(findings);
        }
    }
    public sealed class ModerationHandoffDiagnostics { public ModerationHandoffDiagnostics(IReadOnlyList<ModerationHandoffDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ModerationHandoffDiagnosticCode>(); } public IReadOnlyList<ModerationHandoffDiagnosticCode> Findings { get; } public bool Contains(ModerationHandoffDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoDecisionType { ArchitectureDirection, QaConcern, ServerDependency, DemoVisibilityChoice, PrivacyRestriction, LiveOpsCandidateDisposition, CompetitionReadinessDisposition }
    public enum DecisionLogDiagnosticCode { DecisionLogSourceMissing, DecisionLogOwnerMissing, DecisionLogLiveHistoryForbidden, DecisionLogOfficialAuditForbidden, DecisionLogImpactMissing }
    public sealed class SocialMmoDecisionSourceRef { public SocialMmoDecisionSourceRef(string sourceId) { SourceId = sourceId ?? string.Empty; } public string SourceId { get; } }
    public sealed class SocialMmoDecisionImpact { public SocialMmoDecisionImpact(string impactId) { ImpactId = impactId ?? string.Empty; } public string ImpactId { get; } }
    public sealed class SocialMmoDecisionOwner { public SocialMmoDecisionOwner(string ownerRole) { OwnerRole = ownerRole ?? string.Empty; } public string OwnerRole { get; } }
    public sealed class SocialMmoDecisionNonRuntimeFlag { public SocialMmoDecisionNonRuntimeFlag(bool liveHistoryClaimed = false, bool officialAuditClaimed = false) { LiveHistoryClaimed = liveHistoryClaimed; OfficialAuditClaimed = officialAuditClaimed; } public bool LiveHistoryClaimed { get; } public bool OfficialAuditClaimed { get; } }
    public sealed class SocialMmoDecisionEntry
    {
        public SocialMmoDecisionEntry(string decisionId, SocialMmoDecisionType decisionType, IReadOnlyList<SocialMmoDecisionSourceRef> sourceRefs, string decisionDateMarker, SocialMmoDecisionOwner owner, IReadOnlyList<SocialMmoProductPillar> affectedPillars, SocialMmoDecisionImpact nextWorkImpact, SocialMmoDecisionNonRuntimeFlag nonRuntimeFlag)
        {
            DecisionId = decisionId ?? string.Empty; DecisionType = decisionType; SourceRefs = sourceRefs ?? Array.Empty<SocialMmoDecisionSourceRef>(); DecisionDateMarker = decisionDateMarker ?? string.Empty; Owner = owner; AffectedPillars = affectedPillars ?? Array.Empty<SocialMmoProductPillar>(); NextWorkImpact = nextWorkImpact; NonRuntimeFlag = nonRuntimeFlag;
        }
        public string DecisionId { get; } public SocialMmoDecisionType DecisionType { get; } public IReadOnlyList<SocialMmoDecisionSourceRef> SourceRefs { get; } public string DecisionDateMarker { get; } public SocialMmoDecisionOwner Owner { get; } public IReadOnlyList<SocialMmoProductPillar> AffectedPillars { get; } public SocialMmoDecisionImpact NextWorkImpact { get; } public SocialMmoDecisionNonRuntimeFlag NonRuntimeFlag { get; }
    }
    public sealed class SocialMmoDecisionLogProjection
    {
        public SocialMmoDecisionLogProjection(string logId, IReadOnlyList<SocialMmoDecisionEntry> entries) { LogId = ColonyIntegrationIds.Require(logId); Entries = entries ?? Array.Empty<SocialMmoDecisionEntry>(); }
        public string LogId { get; } public IReadOnlyList<SocialMmoDecisionEntry> Entries { get; }
        public DecisionLogDiagnostics Evaluate()
        {
            var findings = new List<DecisionLogDiagnosticCode>();
            if (Entries.Count == 0 || Entries.Any(e => e.SourceRefs.Count == 0 || e.SourceRefs.Any(s => string.IsNullOrWhiteSpace(s.SourceId)))) findings.Add(DecisionLogDiagnosticCode.DecisionLogSourceMissing);
            if (Entries.Any(e => e.Owner == null || string.IsNullOrWhiteSpace(e.Owner.OwnerRole))) findings.Add(DecisionLogDiagnosticCode.DecisionLogOwnerMissing);
            if (Entries.Any(e => e.NonRuntimeFlag != null && e.NonRuntimeFlag.LiveHistoryClaimed)) findings.Add(DecisionLogDiagnosticCode.DecisionLogLiveHistoryForbidden);
            if (Entries.Any(e => e.NonRuntimeFlag != null && e.NonRuntimeFlag.OfficialAuditClaimed)) findings.Add(DecisionLogDiagnosticCode.DecisionLogOfficialAuditForbidden);
            if (Entries.Any(e => e.NextWorkImpact == null || string.IsNullOrWhiteSpace(e.NextWorkImpact.ImpactId))) findings.Add(DecisionLogDiagnosticCode.DecisionLogImpactMissing);
            return new DecisionLogDiagnostics(findings);
        }
    }
    public sealed class DecisionLogDiagnostics { public DecisionLogDiagnostics(IReadOnlyList<DecisionLogDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DecisionLogDiagnosticCode>(); } public IReadOnlyList<DecisionLogDiagnosticCode> Findings { get; } public bool Contains(DecisionLogDiagnosticCode code) { return Findings.Contains(code); } }

    public enum OperationalRiskMovement { Unchanged, ReducedByEvidence, ReducedByDecision, IncreasedByNewGap, BlockedByServer, BlockedByQa, BlockedByPrivacy, NotMeasurableYet }
    public enum OperationalRiskDiagnosticCode { OperationalRiskSourceMissing, OperationalRiskResolutionOverclaimed, OperationalRiskReleaseReadyForbidden, OperationalRiskAlphaReadyForbidden, OperationalRiskServerBlockerOpen }
    public sealed class OperationalRiskEvidenceLink { public OperationalRiskEvidenceLink(string evidenceId) { EvidenceId = evidenceId ?? string.Empty; } public string EvidenceId { get; } }
    public sealed class OperationalRiskBlocker { public OperationalRiskBlocker(string blockerId, bool serverBlocker = false) { BlockerId = blockerId ?? string.Empty; ServerBlocker = serverBlocker; } public string BlockerId { get; } public bool ServerBlocker { get; } }
    public sealed class OperationalRiskReadinessWarning { public OperationalRiskReadinessWarning(string warningId, bool releaseReadyClaimed = false, bool alphaReadyClaimed = false) { WarningId = warningId ?? string.Empty; ReleaseReadyClaimed = releaseReadyClaimed; AlphaReadyClaimed = alphaReadyClaimed; } public string WarningId { get; } public bool ReleaseReadyClaimed { get; } public bool AlphaReadyClaimed { get; } }
    public sealed class OperationalRiskItem
    {
        public OperationalRiskItem(string riskId, string domain, string playerImpact, int currentSeverity, OperationalRiskMovement movement, IReadOnlyList<OperationalRiskEvidenceLink> evidenceLinks, IReadOnlyList<string> decisionLinks, IReadOnlyList<OperationalRiskBlocker> blockers, IReadOnlyList<OperationalRiskReadinessWarning> readinessWarnings, bool resolvedClaimed = false)
        {
            RiskId = riskId ?? string.Empty; Domain = domain ?? string.Empty; PlayerImpact = playerImpact ?? string.Empty; CurrentSeverity = currentSeverity; Movement = movement; EvidenceLinks = evidenceLinks ?? Array.Empty<OperationalRiskEvidenceLink>(); DecisionLinks = decisionLinks ?? Array.Empty<string>(); Blockers = blockers ?? Array.Empty<OperationalRiskBlocker>(); ReadinessWarnings = readinessWarnings ?? Array.Empty<OperationalRiskReadinessWarning>(); ResolvedClaimed = resolvedClaimed;
        }
        public string RiskId { get; } public string Domain { get; } public string PlayerImpact { get; } public int CurrentSeverity { get; } public OperationalRiskMovement Movement { get; } public IReadOnlyList<OperationalRiskEvidenceLink> EvidenceLinks { get; } public IReadOnlyList<string> DecisionLinks { get; } public IReadOnlyList<OperationalRiskBlocker> Blockers { get; } public IReadOnlyList<OperationalRiskReadinessWarning> ReadinessWarnings { get; } public bool ResolvedClaimed { get; }
    }
    public sealed class SocialMmoOperationalRiskBurnDown
    {
        public SocialMmoOperationalRiskBurnDown(string burnDownId, IReadOnlyList<OperationalRiskItem> risks) { BurnDownId = ColonyIntegrationIds.Require(burnDownId); Risks = risks ?? Array.Empty<OperationalRiskItem>(); }
        public string BurnDownId { get; } public IReadOnlyList<OperationalRiskItem> Risks { get; }
        public OperationalRiskBurnDownDiagnostics Evaluate()
        {
            var findings = new List<OperationalRiskDiagnosticCode>();
            if (Risks.Count == 0 || Risks.Any(r => r.EvidenceLinks.Count == 0 && r.DecisionLinks.Count == 0)) findings.Add(OperationalRiskDiagnosticCode.OperationalRiskSourceMissing);
            if (Risks.Any(r => r.ResolvedClaimed)) findings.Add(OperationalRiskDiagnosticCode.OperationalRiskResolutionOverclaimed);
            if (Risks.SelectMany(r => r.ReadinessWarnings).Any(w => w.ReleaseReadyClaimed)) findings.Add(OperationalRiskDiagnosticCode.OperationalRiskReleaseReadyForbidden);
            if (Risks.SelectMany(r => r.ReadinessWarnings).Any(w => w.AlphaReadyClaimed)) findings.Add(OperationalRiskDiagnosticCode.OperationalRiskAlphaReadyForbidden);
            if (Risks.SelectMany(r => r.Blockers).Any(b => b.ServerBlocker) || Risks.Any(r => r.Movement == OperationalRiskMovement.BlockedByServer)) findings.Add(OperationalRiskDiagnosticCode.OperationalRiskServerBlockerOpen);
            return new OperationalRiskBurnDownDiagnostics(findings);
        }
    }
    public sealed class OperationalRiskBurnDownDiagnostics { public OperationalRiskBurnDownDiagnostics(IReadOnlyList<OperationalRiskDiagnosticCode> findings) { Findings = findings ?? Array.Empty<OperationalRiskDiagnosticCode>(); } public IReadOnlyList<OperationalRiskDiagnosticCode> Findings { get; } public bool Contains(OperationalRiskDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoReviewConsoleClosureVerdict { ReadyForArchitectValidation, ReadyWithGovernanceWarnings, NeedsPlannerRevision, BlockedByMissingConsoleInput, BlockedByPrivacyRisk, BlockedByServerAuthorityGap, BlockedByRuntimeClaim, BlockedByBee381Premature }
    public enum ReviewClosureDiagnosticCode { ReviewClosureInputMissing, ReviewClosurePrivacyRiskOpen, ReviewClosureServerAuthorityGapOpen, ReviewClosureRuntimeClaimDetected, ReviewClosureLiveOpsFinalForbidden, Bee381Premature }
    public sealed class SocialMmoReviewConsoleClosureInput
    {
        public SocialMmoReviewConsoleClosureInput(SocialMmoReviewConsoleBoundary reviewConsole, SocialMmoEvidenceFreshnessAudit evidenceFreshnessAudit, AlliancePvpGovernanceExport governanceExport, SocialMmoSensitiveEvidenceBoundary privacyBoundary, ArmyCompetitionReadinessReview armyCompetitionReview, LiveOpsCandidateReviewBoard liveOpsReviewBoard, ModerationHandoffEvidenceBundle moderationHandoffRedaction, SocialMmoDecisionLogProjection decisionLogProjection, SocialMmoOperationalRiskBurnDown operationalRiskBurnDown)
        {
            ReviewConsole = reviewConsole; EvidenceFreshnessAudit = evidenceFreshnessAudit; GovernanceExport = governanceExport; PrivacyBoundary = privacyBoundary; ArmyCompetitionReview = armyCompetitionReview; LiveOpsReviewBoard = liveOpsReviewBoard; ModerationHandoffRedaction = moderationHandoffRedaction; DecisionLogProjection = decisionLogProjection; OperationalRiskBurnDown = operationalRiskBurnDown;
        }
        public SocialMmoReviewConsoleBoundary ReviewConsole { get; } public SocialMmoEvidenceFreshnessAudit EvidenceFreshnessAudit { get; } public AlliancePvpGovernanceExport GovernanceExport { get; } public SocialMmoSensitiveEvidenceBoundary PrivacyBoundary { get; } public ArmyCompetitionReadinessReview ArmyCompetitionReview { get; } public LiveOpsCandidateReviewBoard LiveOpsReviewBoard { get; } public ModerationHandoffEvidenceBundle ModerationHandoffRedaction { get; } public SocialMmoDecisionLogProjection DecisionLogProjection { get; } public SocialMmoOperationalRiskBurnDown OperationalRiskBurnDown { get; }
    }
    public sealed class SocialMmoReviewConsoleClosureCoverage { public SocialMmoReviewConsoleClosureCoverage(bool privacyRiskOpen = false, bool runtimeClaim = false, bool liveOpsFinalClaim = false) { PrivacyRiskOpen = privacyRiskOpen; RuntimeClaim = runtimeClaim; LiveOpsFinalClaim = liveOpsFinalClaim; } public bool PrivacyRiskOpen { get; } public bool RuntimeClaim { get; } public bool LiveOpsFinalClaim { get; } }
    public sealed class SocialMmoReviewConsoleClosureBlocker { public SocialMmoReviewConsoleClosureBlocker(string blockerId, bool serverAuthorityGap = false) { BlockerId = blockerId ?? string.Empty; ServerAuthorityGap = serverAuthorityGap; } public string BlockerId { get; } public bool ServerAuthorityGap { get; } }
    public sealed class Bee381BlockerStatus { public Bee381BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class SocialMmoReviewConsoleClosureGate
    {
        public const string Bee381BlockedMessage = "BEE-381 bloquee jusqu'a validation architecte.";
        public SocialMmoReviewConsoleClosureGate(string gateId, SocialMmoReviewConsoleClosureInput input, SocialMmoReviewConsoleClosureCoverage coverage, IReadOnlyList<SocialMmoReviewConsoleClosureBlocker> blockers, Bee381BlockerStatus bee381BlockerStatus)
        {
            GateId = ColonyIntegrationIds.Require(gateId); Input = input; Coverage = coverage ?? new SocialMmoReviewConsoleClosureCoverage(); Blockers = blockers ?? Array.Empty<SocialMmoReviewConsoleClosureBlocker>(); Bee381BlockerStatus = bee381BlockerStatus ?? new Bee381BlockerStatus(false, Bee381BlockedMessage);
        }
        public string GateId { get; } public SocialMmoReviewConsoleClosureInput Input { get; } public SocialMmoReviewConsoleClosureCoverage Coverage { get; } public IReadOnlyList<SocialMmoReviewConsoleClosureBlocker> Blockers { get; } public Bee381BlockerStatus Bee381BlockerStatus { get; }
        public SocialMmoReviewConsoleClosureDiagnostics Evaluate()
        {
            var findings = new List<ReviewClosureDiagnosticCode>();
            if (Input == null || Input.ReviewConsole == null || Input.EvidenceFreshnessAudit == null || Input.GovernanceExport == null || Input.PrivacyBoundary == null || Input.ArmyCompetitionReview == null || Input.LiveOpsReviewBoard == null || Input.ModerationHandoffRedaction == null || Input.DecisionLogProjection == null || Input.OperationalRiskBurnDown == null) findings.Add(ReviewClosureDiagnosticCode.ReviewClosureInputMissing);
            if (Coverage.PrivacyRiskOpen) findings.Add(ReviewClosureDiagnosticCode.ReviewClosurePrivacyRiskOpen);
            if (Blockers.Any(b => b.ServerAuthorityGap)) findings.Add(ReviewClosureDiagnosticCode.ReviewClosureServerAuthorityGapOpen);
            if (Coverage.RuntimeClaim) findings.Add(ReviewClosureDiagnosticCode.ReviewClosureRuntimeClaimDetected);
            if (Coverage.LiveOpsFinalClaim) findings.Add(ReviewClosureDiagnosticCode.ReviewClosureLiveOpsFinalForbidden);
            if (Bee381BlockerStatus.PrematureAttempt) findings.Add(ReviewClosureDiagnosticCode.Bee381Premature);
            return new SocialMmoReviewConsoleClosureDiagnostics(ResolveVerdict(findings), findings);
        }
        private static SocialMmoReviewConsoleClosureVerdict ResolveVerdict(IReadOnlyList<ReviewClosureDiagnosticCode> findings)
        {
            if (findings.Contains(ReviewClosureDiagnosticCode.Bee381Premature)) return SocialMmoReviewConsoleClosureVerdict.BlockedByBee381Premature;
            if (findings.Contains(ReviewClosureDiagnosticCode.ReviewClosureRuntimeClaimDetected) || findings.Contains(ReviewClosureDiagnosticCode.ReviewClosureLiveOpsFinalForbidden)) return SocialMmoReviewConsoleClosureVerdict.BlockedByRuntimeClaim;
            if (findings.Contains(ReviewClosureDiagnosticCode.ReviewClosureServerAuthorityGapOpen)) return SocialMmoReviewConsoleClosureVerdict.BlockedByServerAuthorityGap;
            if (findings.Contains(ReviewClosureDiagnosticCode.ReviewClosurePrivacyRiskOpen)) return SocialMmoReviewConsoleClosureVerdict.BlockedByPrivacyRisk;
            if (findings.Contains(ReviewClosureDiagnosticCode.ReviewClosureInputMissing)) return SocialMmoReviewConsoleClosureVerdict.BlockedByMissingConsoleInput;
            return findings.Count == 0 ? SocialMmoReviewConsoleClosureVerdict.ReadyForArchitectValidation : SocialMmoReviewConsoleClosureVerdict.ReadyWithGovernanceWarnings;
        }
    }
    public sealed class SocialMmoReviewConsoleClosureDiagnostics { public SocialMmoReviewConsoleClosureDiagnostics(SocialMmoReviewConsoleClosureVerdict verdict, IReadOnlyList<ReviewClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<ReviewClosureDiagnosticCode>(); } public SocialMmoReviewConsoleClosureVerdict Verdict { get; } public IReadOnlyList<ReviewClosureDiagnosticCode> Findings { get; } public bool Contains(ReviewClosureDiagnosticCode code) { return Findings.Contains(code); } }
}
