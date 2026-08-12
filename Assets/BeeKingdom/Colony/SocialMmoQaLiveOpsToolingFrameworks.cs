using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum SocialMmoQaEvidenceStatus { EvidenceAvailable, EvidencePartial, GapOpen, ServerBlocked, QaScenarioRequired }
    public enum QaIntakeDiagnosticCode { QaIntakeSourceMissing, QaIntakeOwnerMissing, QaIntakeRiskUnclassified, QaIntakeRuntimeClaimForbidden, QaIntakeFinalValidationForbidden }
    public sealed class SocialMmoQaOpenRisk { public SocialMmoQaOpenRisk(string riskId, bool classified) { RiskId = riskId ?? string.Empty; Classified = classified; } public string RiskId { get; } public bool Classified { get; } }
    public sealed class SocialMmoQaOwner { public SocialMmoQaOwner(string ownerId) { OwnerId = ownerId ?? string.Empty; } public string OwnerId { get; } }
    public sealed class SocialMmoQaRuntimeLimit { public SocialMmoQaRuntimeLimit(string limitId, bool runtimeClaimed, bool finalValidationClaimed) { LimitId = limitId ?? string.Empty; RuntimeClaimed = runtimeClaimed; FinalValidationClaimed = finalValidationClaimed; } public string LimitId { get; } public bool RuntimeClaimed { get; } public bool FinalValidationClaimed { get; } }
    public sealed class SocialMmoQaIntakeEntry
    {
        public SocialMmoQaIntakeEntry(string entryId, string sourceReference, SocialMmoProductPillar productPillar, SocialMmoQaEvidenceStatus evidenceStatus, IReadOnlyList<SocialMmoQaOpenRisk> openRisks, SocialMmoQaOwner owner, IReadOnlyList<SocialMmoQaRuntimeLimit> runtimeLimits)
        {
            EntryId = entryId ?? string.Empty; SourceReference = sourceReference ?? string.Empty; ProductPillar = productPillar; EvidenceStatus = evidenceStatus; OpenRisks = openRisks ?? Array.Empty<SocialMmoQaOpenRisk>(); Owner = owner; RuntimeLimits = runtimeLimits ?? Array.Empty<SocialMmoQaRuntimeLimit>();
        }
        public string EntryId { get; } public string SourceReference { get; } public SocialMmoProductPillar ProductPillar { get; } public SocialMmoQaEvidenceStatus EvidenceStatus { get; } public IReadOnlyList<SocialMmoQaOpenRisk> OpenRisks { get; } public SocialMmoQaOwner Owner { get; } public IReadOnlyList<SocialMmoQaRuntimeLimit> RuntimeLimits { get; }
    }
    public sealed class SocialMmoQaIntakeMatrix
    {
        public SocialMmoQaIntakeMatrix(string matrixId, IReadOnlyList<SocialMmoQaIntakeEntry> entries) { MatrixId = ColonyIntegrationIds.Require(matrixId); Entries = entries ?? Array.Empty<SocialMmoQaIntakeEntry>(); }
        public string MatrixId { get; } public IReadOnlyList<SocialMmoQaIntakeEntry> Entries { get; }
        public SocialMmoQaIntakeDiagnostics Evaluate()
        {
            var findings = new List<QaIntakeDiagnosticCode>();
            if (Entries.Count == 0 || Entries.Any(e => string.IsNullOrWhiteSpace(e.SourceReference))) findings.Add(QaIntakeDiagnosticCode.QaIntakeSourceMissing);
            if (Entries.Any(e => e.Owner == null || string.IsNullOrWhiteSpace(e.Owner.OwnerId))) findings.Add(QaIntakeDiagnosticCode.QaIntakeOwnerMissing);
            if (Entries.Any(e => e.OpenRisks.Any(r => !r.Classified))) findings.Add(QaIntakeDiagnosticCode.QaIntakeRiskUnclassified);
            if (Entries.Any(e => e.RuntimeLimits.Any(l => l.RuntimeClaimed))) findings.Add(QaIntakeDiagnosticCode.QaIntakeRuntimeClaimForbidden);
            if (Entries.Any(e => e.RuntimeLimits.Any(l => l.FinalValidationClaimed))) findings.Add(QaIntakeDiagnosticCode.QaIntakeFinalValidationForbidden);
            return new SocialMmoQaIntakeDiagnostics(findings);
        }
    }
    public sealed class SocialMmoQaIntakeDiagnostics { public SocialMmoQaIntakeDiagnostics(IReadOnlyList<QaIntakeDiagnosticCode> findings) { Findings = findings ?? Array.Empty<QaIntakeDiagnosticCode>(); } public IReadOnlyList<QaIntakeDiagnosticCode> Findings { get; } public bool Contains(QaIntakeDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialSignalDiagnosticCode { SocialSignalOriginMissing, SocialSignalFreshnessMissing, SocialSignalPrivacyLimitMissing, ProductionTelemetryForbidden, SocialSignalServerAuthorityRequired }
    public sealed class SocialSignalOrigin { public SocialSignalOrigin(string sourceId) { SourceId = sourceId ?? string.Empty; } public string SourceId { get; } }
    public sealed class SocialSignalFreshness { public SocialSignalFreshness(string freshnessId) { FreshnessId = freshnessId ?? string.Empty; } public string FreshnessId { get; } }
    public sealed class SocialSignalPrivacyLimit { public SocialSignalPrivacyLimit(string limitId) { LimitId = limitId ?? string.Empty; } public string LimitId { get; } }
    public sealed class SocialSignalProductionTelemetryBlocker { public SocialSignalProductionTelemetryBlocker(bool productionTelemetryEnabled) { ProductionTelemetryEnabled = productionTelemetryEnabled; } public bool ProductionTelemetryEnabled { get; } }
    public sealed class PlaygroundSocialSignal
    {
        public PlaygroundSocialSignal(string signalId, string signalType, SocialSignalOrigin origin, SocialSignalFreshness freshness, IReadOnlyList<SocialSignalPrivacyLimit> privacyLimits, bool productionTelemetryEnabled, IReadOnlyList<string> serverAuthorityTopics)
        {
            SignalId = signalId ?? string.Empty; SignalType = signalType ?? string.Empty; Origin = origin; Freshness = freshness; PrivacyLimits = privacyLimits ?? Array.Empty<SocialSignalPrivacyLimit>(); ProductionTelemetryEnabled = productionTelemetryEnabled; ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
        }
        public string SignalId { get; } public string SignalType { get; } public SocialSignalOrigin Origin { get; } public SocialSignalFreshness Freshness { get; } public IReadOnlyList<SocialSignalPrivacyLimit> PrivacyLimits { get; } public bool ProductionTelemetryEnabled { get; } public IReadOnlyList<string> ServerAuthorityTopics { get; }
    }
    public sealed class PlaygroundSocialSignalTelemetryContract
    {
        public PlaygroundSocialSignalTelemetryContract(string contractId, IReadOnlyList<PlaygroundSocialSignal> signals) { ContractId = ColonyIntegrationIds.Require(contractId); Signals = signals ?? Array.Empty<PlaygroundSocialSignal>(); }
        public string ContractId { get; } public IReadOnlyList<PlaygroundSocialSignal> Signals { get; }
        public PlaygroundSocialSignalTelemetryDiagnostics Evaluate()
        {
            var findings = new List<SocialSignalDiagnosticCode>();
            if (Signals.Any(s => s.Origin == null || string.IsNullOrWhiteSpace(s.Origin.SourceId))) findings.Add(SocialSignalDiagnosticCode.SocialSignalOriginMissing);
            if (Signals.Any(s => s.Freshness == null || string.IsNullOrWhiteSpace(s.Freshness.FreshnessId))) findings.Add(SocialSignalDiagnosticCode.SocialSignalFreshnessMissing);
            if (Signals.Any(s => s.PrivacyLimits.Count == 0)) findings.Add(SocialSignalDiagnosticCode.SocialSignalPrivacyLimitMissing);
            if (Signals.Any(s => s.ProductionTelemetryEnabled)) findings.Add(SocialSignalDiagnosticCode.ProductionTelemetryForbidden);
            if (Signals.Any(s => s.ServerAuthorityTopics.Count > 0)) findings.Add(SocialSignalDiagnosticCode.SocialSignalServerAuthorityRequired);
            return new PlaygroundSocialSignalTelemetryDiagnostics(findings);
        }
    }
    public sealed class PlaygroundSocialSignalTelemetryDiagnostics { public PlaygroundSocialSignalTelemetryDiagnostics(IReadOnlyList<SocialSignalDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialSignalDiagnosticCode>(); } public IReadOnlyList<SocialSignalDiagnosticCode> Findings { get; } public bool Contains(SocialSignalDiagnosticCode code) { return Findings.Contains(code); } }

    public enum AllianceActivityAlertLevel { Informational, Warning, Blocked, OfficialVerdict }
    public enum AllianceActivityDiagnosticCode { AllianceActivitySignalMissing, AllianceActivityMissingDataOpen, AllianceActivityOfficialScoreForbidden, AllianceProgressionRuntimeForbidden, AlliancePressureRiskOpen }
    public sealed class AllianceActivityHealthSignal { public AllianceActivityHealthSignal(string signalId, string sourceId) { SignalId = signalId ?? string.Empty; SourceId = sourceId ?? string.Empty; } public string SignalId { get; } public string SourceId { get; } }
    public sealed class AllianceActivityHealthAlert { public AllianceActivityHealthAlert(string alertId, AllianceActivityAlertLevel level, bool officialScoreClaimed) { AlertId = alertId ?? string.Empty; Level = level; OfficialScoreClaimed = officialScoreClaimed; } public string AlertId { get; } public AllianceActivityAlertLevel Level { get; } public bool OfficialScoreClaimed { get; } }
    public sealed class AllianceActivityMissingData { public AllianceActivityMissingData(string dataId, bool open) { DataId = dataId ?? string.Empty; Open = open; } public string DataId { get; } public bool Open { get; } }
    public sealed class AllianceActivityPressureRisk { public AllianceActivityPressureRisk(string riskId, bool open) { RiskId = riskId ?? string.Empty; Open = open; } public string RiskId { get; } public bool Open { get; } }
    public sealed class AllianceActivityDashboardLimit { public AllianceActivityDashboardLimit(string limitId, bool progressionRuntimeClaimed) { LimitId = limitId ?? string.Empty; ProgressionRuntimeClaimed = progressionRuntimeClaimed; } public string LimitId { get; } public bool ProgressionRuntimeClaimed { get; } }
    public sealed class AllianceActivityHealthDashboardBoundary
    {
        public AllianceActivityHealthDashboardBoundary(string allianceProjectionId, IReadOnlyList<AllianceActivityHealthSignal> healthSignals, IReadOnlyList<AllianceActivityHealthAlert> alerts, IReadOnlyList<AllianceActivityMissingData> missingData, IReadOnlyList<AllianceActivityPressureRisk> pressureRisks, IReadOnlyList<AllianceActivityDashboardLimit> limits)
        {
            AllianceProjectionId = allianceProjectionId ?? string.Empty; HealthSignals = healthSignals ?? Array.Empty<AllianceActivityHealthSignal>(); Alerts = alerts ?? Array.Empty<AllianceActivityHealthAlert>(); MissingData = missingData ?? Array.Empty<AllianceActivityMissingData>(); PressureRisks = pressureRisks ?? Array.Empty<AllianceActivityPressureRisk>(); Limits = limits ?? Array.Empty<AllianceActivityDashboardLimit>();
        }
        public string AllianceProjectionId { get; } public IReadOnlyList<AllianceActivityHealthSignal> HealthSignals { get; } public IReadOnlyList<AllianceActivityHealthAlert> Alerts { get; } public IReadOnlyList<AllianceActivityMissingData> MissingData { get; } public IReadOnlyList<AllianceActivityPressureRisk> PressureRisks { get; } public IReadOnlyList<AllianceActivityDashboardLimit> Limits { get; }
        public AllianceActivityHealthDashboardDiagnostics Evaluate()
        {
            var findings = new List<AllianceActivityDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(AllianceProjectionId) || HealthSignals.Count == 0 || HealthSignals.Any(s => string.IsNullOrWhiteSpace(s.SourceId))) findings.Add(AllianceActivityDiagnosticCode.AllianceActivitySignalMissing);
            if (MissingData.Any(d => d.Open)) findings.Add(AllianceActivityDiagnosticCode.AllianceActivityMissingDataOpen);
            if (Alerts.Any(a => a.Level == AllianceActivityAlertLevel.OfficialVerdict || a.OfficialScoreClaimed)) findings.Add(AllianceActivityDiagnosticCode.AllianceActivityOfficialScoreForbidden);
            if (Limits.Any(l => l.ProgressionRuntimeClaimed)) findings.Add(AllianceActivityDiagnosticCode.AllianceProgressionRuntimeForbidden);
            if (PressureRisks.Any(r => r.Open)) findings.Add(AllianceActivityDiagnosticCode.AlliancePressureRiskOpen);
            return new AllianceActivityHealthDashboardDiagnostics(findings);
        }
    }
    public sealed class AllianceActivityHealthDashboardDiagnostics { public AllianceActivityHealthDashboardDiagnostics(IReadOnlyList<AllianceActivityDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceActivityDiagnosticCode>(); } public IReadOnlyList<AllianceActivityDiagnosticCode> Findings { get; } public bool Contains(AllianceActivityDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ArmyPvpBalanceDiagnosticCode { BalanceSignalSourceMissing, OfficialPowerCalculationForbidden, PayToWinSignalMissing, RecoveryBalanceSignalMissing, BalanceServerAuthorityRequired }
    public sealed class AsymmetryRiskSignal { public AsymmetryRiskSignal(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class PayToWinRiskSignal { public PayToWinRiskSignal(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class RecoveryBalanceSignal { public RecoveryBalanceSignal(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class OfficialPowerCalculationBlocker { public OfficialPowerCalculationBlocker(bool officialPowerAllowed) { OfficialPowerAllowed = officialPowerAllowed; } public bool OfficialPowerAllowed { get; } }
    public sealed class ArmyPvPBalanceSignal
    {
        public ArmyPvPBalanceSignal(string signalId, string signalType, string sourceBee, string observedRisk, string qaNeed, string serverAuthorityTopic, bool officialPowerAllowed)
        {
            SignalId = signalId ?? string.Empty; SignalType = signalType ?? string.Empty; SourceBee = sourceBee ?? string.Empty; ObservedRisk = observedRisk ?? string.Empty; QaNeed = qaNeed ?? string.Empty; ServerAuthorityTopic = serverAuthorityTopic ?? string.Empty; OfficialPowerAllowed = officialPowerAllowed;
        }
        public string SignalId { get; } public string SignalType { get; } public string SourceBee { get; } public string ObservedRisk { get; } public string QaNeed { get; } public string ServerAuthorityTopic { get; } public bool OfficialPowerAllowed { get; }
    }
    public sealed class ArmyPvPBalanceSignalCatalog
    {
        public ArmyPvPBalanceSignalCatalog(string catalogId, IReadOnlyList<ArmyPvPBalanceSignal> signals, IReadOnlyList<PayToWinRiskSignal> payToWinRisks, IReadOnlyList<RecoveryBalanceSignal> recoverySignals) { CatalogId = ColonyIntegrationIds.Require(catalogId); Signals = signals ?? Array.Empty<ArmyPvPBalanceSignal>(); PayToWinRisks = payToWinRisks ?? Array.Empty<PayToWinRiskSignal>(); RecoverySignals = recoverySignals ?? Array.Empty<RecoveryBalanceSignal>(); }
        public string CatalogId { get; } public IReadOnlyList<ArmyPvPBalanceSignal> Signals { get; } public IReadOnlyList<PayToWinRiskSignal> PayToWinRisks { get; } public IReadOnlyList<RecoveryBalanceSignal> RecoverySignals { get; }
        public ArmyPvPBalanceSignalDiagnostics Evaluate()
        {
            var findings = new List<ArmyPvpBalanceDiagnosticCode>();
            if (Signals.Count == 0 || Signals.Any(s => string.IsNullOrWhiteSpace(s.SourceBee))) findings.Add(ArmyPvpBalanceDiagnosticCode.BalanceSignalSourceMissing);
            if (Signals.Any(s => s.OfficialPowerAllowed)) findings.Add(ArmyPvpBalanceDiagnosticCode.OfficialPowerCalculationForbidden);
            if (PayToWinRisks.Count == 0) findings.Add(ArmyPvpBalanceDiagnosticCode.PayToWinSignalMissing);
            if (RecoverySignals.Count == 0) findings.Add(ArmyPvpBalanceDiagnosticCode.RecoveryBalanceSignalMissing);
            if (Signals.Any(s => !string.IsNullOrWhiteSpace(s.ServerAuthorityTopic))) findings.Add(ArmyPvpBalanceDiagnosticCode.BalanceServerAuthorityRequired);
            return new ArmyPvPBalanceSignalDiagnostics(findings);
        }
    }
    public sealed class ArmyPvPBalanceSignalDiagnostics { public ArmyPvPBalanceSignalDiagnostics(IReadOnlyList<ArmyPvpBalanceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyPvpBalanceDiagnosticCode>(); } public IReadOnlyList<ArmyPvpBalanceDiagnosticCode> Findings { get; } public bool Contains(ArmyPvpBalanceDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialAbuseDiagnosticCode { AbuseWarningSignalMissing, AbuseWarningPrivacyMissing, AbuseWarningFalsePositiveRiskMissing, AbuseSanctionForbidden, AbuseRuntimeEnforcementForbidden, AbuseServerAuthorityRequired }
    public sealed class SocialAbuseWarningConfidence { public SocialAbuseWarningConfidence(double value) { Value = ColonyIntegrationIds.Clamp01(value); } public double Value { get; } }
    public sealed class SocialAbusePrivacyShield { public SocialAbusePrivacyShield(string shieldId) { ShieldId = shieldId ?? string.Empty; } public string ShieldId { get; } }
    public sealed class SocialAbuseFalsePositiveRisk { public SocialAbuseFalsePositiveRisk(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class SocialAbuseServerAuthorityTopic { public SocialAbuseServerAuthorityTopic(string topicId, bool serverRequired) { TopicId = topicId ?? string.Empty; ServerRequired = serverRequired; } public string TopicId { get; } public bool ServerRequired { get; } }
    public sealed class SocialAbuseWarningSignal
    {
        public SocialAbuseWarningSignal(string signalId, string abuseType, string sourceReference, SocialAbuseWarningConfidence confidence, SocialAbusePrivacyShield privacyShield, SocialAbuseFalsePositiveRisk falsePositiveRisk, bool runtimeEnforcementAllowed, bool sanctionRequested, SocialAbuseServerAuthorityTopic serverAuthorityTopic)
        {
            SignalId = signalId ?? string.Empty; AbuseType = abuseType ?? string.Empty; SourceReference = sourceReference ?? string.Empty; Confidence = confidence; PrivacyShield = privacyShield; FalsePositiveRisk = falsePositiveRisk; RuntimeEnforcementAllowed = runtimeEnforcementAllowed; SanctionRequested = sanctionRequested; ServerAuthorityTopic = serverAuthorityTopic;
        }
        public string SignalId { get; } public string AbuseType { get; } public string SourceReference { get; } public SocialAbuseWarningConfidence Confidence { get; } public SocialAbusePrivacyShield PrivacyShield { get; } public SocialAbuseFalsePositiveRisk FalsePositiveRisk { get; } public bool RuntimeEnforcementAllowed { get; } public bool SanctionRequested { get; } public SocialAbuseServerAuthorityTopic ServerAuthorityTopic { get; }
    }
    public sealed class SocialAbuseEarlyWarningContract
    {
        public SocialAbuseEarlyWarningContract(string contractId, IReadOnlyList<SocialAbuseWarningSignal> warnings) { ContractId = ColonyIntegrationIds.Require(contractId); Warnings = warnings ?? Array.Empty<SocialAbuseWarningSignal>(); }
        public string ContractId { get; } public IReadOnlyList<SocialAbuseWarningSignal> Warnings { get; }
        public SocialAbuseEarlyWarningDiagnostics Evaluate()
        {
            var findings = new List<SocialAbuseDiagnosticCode>();
            if (Warnings.Count == 0 || Warnings.Any(w => string.IsNullOrWhiteSpace(w.SignalId))) findings.Add(SocialAbuseDiagnosticCode.AbuseWarningSignalMissing);
            if (Warnings.Any(w => w.PrivacyShield == null || string.IsNullOrWhiteSpace(w.PrivacyShield.ShieldId))) findings.Add(SocialAbuseDiagnosticCode.AbuseWarningPrivacyMissing);
            if (Warnings.Any(w => w.FalsePositiveRisk == null || string.IsNullOrWhiteSpace(w.FalsePositiveRisk.RiskId))) findings.Add(SocialAbuseDiagnosticCode.AbuseWarningFalsePositiveRiskMissing);
            if (Warnings.Any(w => w.SanctionRequested)) findings.Add(SocialAbuseDiagnosticCode.AbuseSanctionForbidden);
            if (Warnings.Any(w => w.RuntimeEnforcementAllowed)) findings.Add(SocialAbuseDiagnosticCode.AbuseRuntimeEnforcementForbidden);
            if (Warnings.Any(w => w.ServerAuthorityTopic == null || w.ServerAuthorityTopic.ServerRequired)) findings.Add(SocialAbuseDiagnosticCode.AbuseServerAuthorityRequired);
            return new SocialAbuseEarlyWarningDiagnostics(findings);
        }
    }
    public sealed class SocialAbuseEarlyWarningDiagnostics { public SocialAbuseEarlyWarningDiagnostics(IReadOnlyList<SocialAbuseDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialAbuseDiagnosticCode>(); } public IReadOnlyList<SocialAbuseDiagnosticCode> Findings { get; } public bool Contains(SocialAbuseDiagnosticCode code) { return Findings.Contains(code); } }

    public enum LiveOpsEventCandidateStatus { CandidateOnly, BlockedByServer, BlockedByRewardDesign, BlockedByModerationRisk }
    public enum LiveOpsDiagnosticCode { LiveOpsCandidateMissing, LiveOpsRewardForbidden, LiveOpsCalendarForbidden, LiveOpsMonetizationForbidden, LiveOpsRankingForbidden, LiveOpsServerAuthorityRequired }
    public sealed class LiveOpsEventAudienceProjection { public LiveOpsEventAudienceProjection(string audienceId) { AudienceId = audienceId ?? string.Empty; } public string AudienceId { get; } }
    public sealed class LiveOpsEventRisk { public LiveOpsEventRisk(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class LiveOpsEventRewardBlocker { public LiveOpsEventRewardBlocker(bool rewardRequested, bool calendarRequested, bool monetizationRequested, bool rankingRequested) { RewardRequested = rewardRequested; CalendarRequested = calendarRequested; MonetizationRequested = monetizationRequested; RankingRequested = rankingRequested; } public bool RewardRequested { get; } public bool CalendarRequested { get; } public bool MonetizationRequested { get; } public bool RankingRequested { get; } }
    public sealed class LiveOpsEventServerAuthorityTopic { public LiveOpsEventServerAuthorityTopic(string topicId, bool serverRequired) { TopicId = topicId ?? string.Empty; ServerRequired = serverRequired; } public string TopicId { get; } public bool ServerRequired { get; } }
    public sealed class LiveOpsEventCandidate
    {
        public LiveOpsEventCandidate(string candidateId, string theme, IReadOnlyList<SocialMmoProductPillar> productPillars, LiveOpsEventAudienceProjection audienceProjection, string playerValue, IReadOnlyList<LiveOpsEventRisk> risks, LiveOpsEventCandidateStatus status, LiveOpsEventRewardBlocker rewardBlocker, IReadOnlyList<LiveOpsEventServerAuthorityTopic> serverAuthorityTopics)
        {
            CandidateId = candidateId ?? string.Empty; Theme = theme ?? string.Empty; ProductPillars = productPillars ?? Array.Empty<SocialMmoProductPillar>(); AudienceProjection = audienceProjection; PlayerValue = playerValue ?? string.Empty; Risks = risks ?? Array.Empty<LiveOpsEventRisk>(); Status = status; RewardBlocker = rewardBlocker; ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<LiveOpsEventServerAuthorityTopic>();
        }
        public string CandidateId { get; } public string Theme { get; } public IReadOnlyList<SocialMmoProductPillar> ProductPillars { get; } public LiveOpsEventAudienceProjection AudienceProjection { get; } public string PlayerValue { get; } public IReadOnlyList<LiveOpsEventRisk> Risks { get; } public LiveOpsEventCandidateStatus Status { get; } public LiveOpsEventRewardBlocker RewardBlocker { get; } public IReadOnlyList<LiveOpsEventServerAuthorityTopic> ServerAuthorityTopics { get; }
    }
    public sealed class LiveOpsEventCandidateBoundary
    {
        public LiveOpsEventCandidateBoundary(string boundaryId, IReadOnlyList<LiveOpsEventCandidate> candidates) { BoundaryId = ColonyIntegrationIds.Require(boundaryId); Candidates = candidates ?? Array.Empty<LiveOpsEventCandidate>(); }
        public string BoundaryId { get; } public IReadOnlyList<LiveOpsEventCandidate> Candidates { get; }
        public LiveOpsEventCandidateDiagnostics Evaluate()
        {
            var findings = new List<LiveOpsDiagnosticCode>();
            if (Candidates.Count == 0 || Candidates.Any(c => string.IsNullOrWhiteSpace(c.CandidateId))) findings.Add(LiveOpsDiagnosticCode.LiveOpsCandidateMissing);
            if (Candidates.Any(c => c.RewardBlocker != null && c.RewardBlocker.RewardRequested)) findings.Add(LiveOpsDiagnosticCode.LiveOpsRewardForbidden);
            if (Candidates.Any(c => c.RewardBlocker != null && c.RewardBlocker.CalendarRequested)) findings.Add(LiveOpsDiagnosticCode.LiveOpsCalendarForbidden);
            if (Candidates.Any(c => c.RewardBlocker != null && c.RewardBlocker.MonetizationRequested)) findings.Add(LiveOpsDiagnosticCode.LiveOpsMonetizationForbidden);
            if (Candidates.Any(c => c.RewardBlocker != null && c.RewardBlocker.RankingRequested)) findings.Add(LiveOpsDiagnosticCode.LiveOpsRankingForbidden);
            if (Candidates.Any(c => c.ServerAuthorityTopics.Any(t => t.ServerRequired))) findings.Add(LiveOpsDiagnosticCode.LiveOpsServerAuthorityRequired);
            return new LiveOpsEventCandidateDiagnostics(findings);
        }
    }
    public sealed class LiveOpsEventCandidateDiagnostics { public LiveOpsEventCandidateDiagnostics(IReadOnlyList<LiveOpsDiagnosticCode> findings) { Findings = findings ?? Array.Empty<LiveOpsDiagnosticCode>(); } public IReadOnlyList<LiveOpsDiagnosticCode> Findings { get; } public bool Contains(LiveOpsDiagnosticCode code) { return Findings.Contains(code); } }

    public enum AllianceCompetitionReadinessVerdict { NotReady, Partial, ReadyForDesignReview, BlockedByServer }
    public enum AllianceCompetitionDiagnosticCode { CompetitionConditionMissing, CompetitionFairnessCheckMissing, CompetitionAbuseGuardMissing, CompetitionRankingForbidden, CompetitionMatchmakingForbidden, CompetitionRewardForbidden }
    public sealed class AllianceCompetitionCondition { public AllianceCompetitionCondition(string conditionId, bool present) { ConditionId = conditionId ?? string.Empty; Present = present; } public string ConditionId { get; } public bool Present { get; } }
    public sealed class AllianceCompetitionFairnessCheck { public AllianceCompetitionFairnessCheck(string checkId, bool present) { CheckId = checkId ?? string.Empty; Present = present; } public string CheckId { get; } public bool Present { get; } }
    public sealed class AllianceCompetitionAbuseGuard { public AllianceCompetitionAbuseGuard(string guardId, bool present) { GuardId = guardId ?? string.Empty; Present = present; } public string GuardId { get; } public bool Present { get; } }
    public sealed class AllianceCompetitionMissingInput { public AllianceCompetitionMissingInput(string inputId) { InputId = inputId ?? string.Empty; } public string InputId { get; } }
    public sealed class AllianceCompetitionServerAuthorityTopic { public AllianceCompetitionServerAuthorityTopic(string topicId) { TopicId = topicId ?? string.Empty; } public string TopicId { get; } }
    public sealed class AllianceCompetitionReadinessProjection
    {
        public AllianceCompetitionReadinessProjection(string competitionCandidateId, IReadOnlyList<AllianceCompetitionCondition> conditions, IReadOnlyList<AllianceCompetitionFairnessCheck> fairnessChecks, IReadOnlyList<AllianceCompetitionAbuseGuard> abuseGuards, IReadOnlyList<AllianceCompetitionMissingInput> missingInputs, AllianceCompetitionReadinessVerdict verdict, IReadOnlyList<AllianceCompetitionServerAuthorityTopic> serverAuthorityTopics, bool rankingRequested, bool matchmakingRequested, bool rewardRequested)
        {
            CompetitionCandidateId = competitionCandidateId ?? string.Empty; Conditions = conditions ?? Array.Empty<AllianceCompetitionCondition>(); FairnessChecks = fairnessChecks ?? Array.Empty<AllianceCompetitionFairnessCheck>(); AbuseGuards = abuseGuards ?? Array.Empty<AllianceCompetitionAbuseGuard>(); MissingInputs = missingInputs ?? Array.Empty<AllianceCompetitionMissingInput>(); Verdict = verdict; ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<AllianceCompetitionServerAuthorityTopic>(); RankingRequested = rankingRequested; MatchmakingRequested = matchmakingRequested; RewardRequested = rewardRequested;
        }
        public string CompetitionCandidateId { get; } public IReadOnlyList<AllianceCompetitionCondition> Conditions { get; } public IReadOnlyList<AllianceCompetitionFairnessCheck> FairnessChecks { get; } public IReadOnlyList<AllianceCompetitionAbuseGuard> AbuseGuards { get; } public IReadOnlyList<AllianceCompetitionMissingInput> MissingInputs { get; } public AllianceCompetitionReadinessVerdict Verdict { get; } public IReadOnlyList<AllianceCompetitionServerAuthorityTopic> ServerAuthorityTopics { get; } public bool RankingRequested { get; } public bool MatchmakingRequested { get; } public bool RewardRequested { get; }
        public AllianceCompetitionReadinessDiagnostics Evaluate()
        {
            var findings = new List<AllianceCompetitionDiagnosticCode>();
            if (Conditions.Count == 0 || Conditions.Any(c => !c.Present) || MissingInputs.Count > 0) findings.Add(AllianceCompetitionDiagnosticCode.CompetitionConditionMissing);
            if (FairnessChecks.Count == 0 || FairnessChecks.Any(c => !c.Present)) findings.Add(AllianceCompetitionDiagnosticCode.CompetitionFairnessCheckMissing);
            if (AbuseGuards.Count == 0 || AbuseGuards.Any(g => !g.Present)) findings.Add(AllianceCompetitionDiagnosticCode.CompetitionAbuseGuardMissing);
            if (RankingRequested) findings.Add(AllianceCompetitionDiagnosticCode.CompetitionRankingForbidden);
            if (MatchmakingRequested) findings.Add(AllianceCompetitionDiagnosticCode.CompetitionMatchmakingForbidden);
            if (RewardRequested) findings.Add(AllianceCompetitionDiagnosticCode.CompetitionRewardForbidden);
            return new AllianceCompetitionReadinessDiagnostics(findings);
        }
    }
    public sealed class AllianceCompetitionReadinessDiagnostics { public AllianceCompetitionReadinessDiagnostics(IReadOnlyList<AllianceCompetitionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceCompetitionDiagnosticCode>(); } public IReadOnlyList<AllianceCompetitionDiagnosticCode> Findings { get; } public bool Contains(AllianceCompetitionDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoToolRoleProjection { Viewer, QaReviewer, DemoReviewer, WorkerImplementer, ServerReviewer, ArchitectReviewer }
    public enum ToolPermissionDiagnosticCode { ToolPermissionRoleMissing, ToolPermissionImplicit, ToolMutationForbidden, ToolSanctionForbidden, ToolServerOverrideForbidden, ToolLocalTruthRiskOpen }
    public sealed class SocialMmoToolPermission { public SocialMmoToolPermission(string permissionId, SocialMmoToolRoleProjection role, string action, bool readOnly, bool exportAllowed, bool mutationAllowed, string serverAuthorityTopic) { PermissionId = permissionId ?? string.Empty; Role = role; Action = action ?? string.Empty; ReadOnly = readOnly; ExportAllowed = exportAllowed; MutationAllowed = mutationAllowed; ServerAuthorityTopic = serverAuthorityTopic ?? string.Empty; } public string PermissionId { get; } public SocialMmoToolRoleProjection Role { get; } public string Action { get; } public bool ReadOnly { get; } public bool ExportAllowed { get; } public bool MutationAllowed { get; } public string ServerAuthorityTopic { get; } }
    public sealed class SocialMmoToolForbiddenAction { public SocialMmoToolForbiddenAction(string actionId, bool sanctionRequested, bool serverOverrideRequested) { ActionId = actionId ?? string.Empty; SanctionRequested = sanctionRequested; ServerOverrideRequested = serverOverrideRequested; } public string ActionId { get; } public bool SanctionRequested { get; } public bool ServerOverrideRequested { get; } }
    public sealed class SocialMmoToolLocalTruthRisk { public SocialMmoToolLocalTruthRisk(string riskId, bool open) { RiskId = riskId ?? string.Empty; Open = open; } public string RiskId { get; } public bool Open { get; } }
    public sealed class SocialMmoToolPermissionServerTopic { public SocialMmoToolPermissionServerTopic(string topicId) { TopicId = topicId ?? string.Empty; } public string TopicId { get; } }
    public sealed class SocialMmoToolPermissionBoundary
    {
        public SocialMmoToolPermissionBoundary(string boundaryId, IReadOnlyList<SocialMmoToolPermission> permissions, IReadOnlyList<SocialMmoToolForbiddenAction> forbiddenActions, IReadOnlyList<SocialMmoToolLocalTruthRisk> localTruthRisks) { BoundaryId = ColonyIntegrationIds.Require(boundaryId); Permissions = permissions ?? Array.Empty<SocialMmoToolPermission>(); ForbiddenActions = forbiddenActions ?? Array.Empty<SocialMmoToolForbiddenAction>(); LocalTruthRisks = localTruthRisks ?? Array.Empty<SocialMmoToolLocalTruthRisk>(); }
        public string BoundaryId { get; } public IReadOnlyList<SocialMmoToolPermission> Permissions { get; } public IReadOnlyList<SocialMmoToolForbiddenAction> ForbiddenActions { get; } public IReadOnlyList<SocialMmoToolLocalTruthRisk> LocalTruthRisks { get; }
        public SocialMmoToolPermissionDiagnostics Evaluate()
        {
            var findings = new List<ToolPermissionDiagnosticCode>();
            if (Permissions.Count == 0) findings.Add(ToolPermissionDiagnosticCode.ToolPermissionRoleMissing);
            if (Permissions.Any(p => string.IsNullOrWhiteSpace(p.PermissionId) || string.IsNullOrWhiteSpace(p.Action))) findings.Add(ToolPermissionDiagnosticCode.ToolPermissionImplicit);
            if (Permissions.Any(p => p.MutationAllowed || !p.ReadOnly)) findings.Add(ToolPermissionDiagnosticCode.ToolMutationForbidden);
            if (ForbiddenActions.Any(a => a.SanctionRequested)) findings.Add(ToolPermissionDiagnosticCode.ToolSanctionForbidden);
            if (ForbiddenActions.Any(a => a.ServerOverrideRequested)) findings.Add(ToolPermissionDiagnosticCode.ToolServerOverrideForbidden);
            if (LocalTruthRisks.Any(r => r.Open)) findings.Add(ToolPermissionDiagnosticCode.ToolLocalTruthRiskOpen);
            return new SocialMmoToolPermissionDiagnostics(findings);
        }
    }
    public sealed class SocialMmoToolPermissionDiagnostics { public SocialMmoToolPermissionDiagnostics(IReadOnlyList<ToolPermissionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ToolPermissionDiagnosticCode>(); } public IReadOnlyList<ToolPermissionDiagnosticCode> Findings { get; } public bool Contains(ToolPermissionDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoScenarioHandoffVerdict { ReadyForQaDesign, NeedsEvidence, BlockedByServer, BlockedByRuntimeLimit }
    public enum QaScenarioHandoffDiagnosticCode { QaScenarioHandoffItemMissing, QaScenarioEvidenceMissing, QaScenarioOwnerMissing, QaScenarioRuntimeExecutionForbidden, QaScenarioServerDependencyMissing }
    public sealed class SocialMmoScenarioEvidenceLink { public SocialMmoScenarioEvidenceLink(string evidenceId) { EvidenceId = evidenceId ?? string.Empty; } public string EvidenceId { get; } }
    public sealed class SocialMmoScenarioRuntimeLimit { public SocialMmoScenarioRuntimeLimit(string limitId, bool runtimeExecutionRequested) { LimitId = limitId ?? string.Empty; RuntimeExecutionRequested = runtimeExecutionRequested; } public string LimitId { get; } public bool RuntimeExecutionRequested { get; } }
    public sealed class SocialMmoScenarioOwnerMap { public SocialMmoScenarioOwnerMap(bool workerOwner, bool qaOwner, bool demoOwner, bool serverOwner) { WorkerOwner = workerOwner; QaOwner = qaOwner; DemoOwner = demoOwner; ServerOwner = serverOwner; } public bool WorkerOwner { get; } public bool QaOwner { get; } public bool DemoOwner { get; } public bool ServerOwner { get; } }
    public sealed class SocialMmoQaScenarioHandoffItem
    {
        public SocialMmoQaScenarioHandoffItem(string scenarioId, string scenarioType, IReadOnlyList<string> sourceBees, IReadOnlyList<SocialMmoScenarioEvidenceLink> evidenceLinks, IReadOnlyList<SocialMmoScenarioRuntimeLimit> runtimeLimits, SocialMmoScenarioOwnerMap ownerMap, SocialMmoScenarioHandoffVerdict verdict)
        {
            ScenarioId = scenarioId ?? string.Empty; ScenarioType = scenarioType ?? string.Empty; SourceBees = sourceBees ?? Array.Empty<string>(); EvidenceLinks = evidenceLinks ?? Array.Empty<SocialMmoScenarioEvidenceLink>(); RuntimeLimits = runtimeLimits ?? Array.Empty<SocialMmoScenarioRuntimeLimit>(); OwnerMap = ownerMap; Verdict = verdict;
        }
        public string ScenarioId { get; } public string ScenarioType { get; } public IReadOnlyList<string> SourceBees { get; } public IReadOnlyList<SocialMmoScenarioEvidenceLink> EvidenceLinks { get; } public IReadOnlyList<SocialMmoScenarioRuntimeLimit> RuntimeLimits { get; } public SocialMmoScenarioOwnerMap OwnerMap { get; } public SocialMmoScenarioHandoffVerdict Verdict { get; }
    }
    public sealed class SocialMmoQaScenarioHandoffBundle
    {
        public SocialMmoQaScenarioHandoffBundle(string bundleId, IReadOnlyList<SocialMmoQaScenarioHandoffItem> items) { BundleId = ColonyIntegrationIds.Require(bundleId); Items = items ?? Array.Empty<SocialMmoQaScenarioHandoffItem>(); }
        public string BundleId { get; } public IReadOnlyList<SocialMmoQaScenarioHandoffItem> Items { get; }
        public SocialMmoQaScenarioHandoffDiagnostics Evaluate()
        {
            var findings = new List<QaScenarioHandoffDiagnosticCode>();
            if (Items.Count == 0 || Items.Any(i => string.IsNullOrWhiteSpace(i.ScenarioId))) findings.Add(QaScenarioHandoffDiagnosticCode.QaScenarioHandoffItemMissing);
            if (Items.Any(i => i.EvidenceLinks.Count == 0)) findings.Add(QaScenarioHandoffDiagnosticCode.QaScenarioEvidenceMissing);
            if (Items.Any(i => i.OwnerMap == null || !i.OwnerMap.WorkerOwner || !i.OwnerMap.QaOwner || !i.OwnerMap.DemoOwner)) findings.Add(QaScenarioHandoffDiagnosticCode.QaScenarioOwnerMissing);
            if (Items.Any(i => i.RuntimeLimits.Any(l => l.RuntimeExecutionRequested))) findings.Add(QaScenarioHandoffDiagnosticCode.QaScenarioRuntimeExecutionForbidden);
            if (Items.Any(i => i.OwnerMap == null || !i.OwnerMap.ServerOwner)) findings.Add(QaScenarioHandoffDiagnosticCode.QaScenarioServerDependencyMissing);
            return new SocialMmoQaScenarioHandoffDiagnostics(findings);
        }
    }
    public sealed class SocialMmoQaScenarioHandoffDiagnostics { public SocialMmoQaScenarioHandoffDiagnostics(IReadOnlyList<QaScenarioHandoffDiagnosticCode> findings) { Findings = findings ?? Array.Empty<QaScenarioHandoffDiagnosticCode>(); } public IReadOnlyList<QaScenarioHandoffDiagnosticCode> Findings { get; } public bool Contains(QaScenarioHandoffDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoQaToolingVerdict { ReadyForArchitectValidation, ReadyWithWarnings, NeedsPlannerRevision, BlockedByMissingInput, BlockedByRuntimeClaim, BlockedByServerAuthorityGap, BlockedByPrivacyRisk, BlockedByBee371Premature }
    public enum QaToolingDiagnosticCode { QaToolingInputMissing, QaToolingRuntimeClaimDetected, QaToolingServerAuthorityGapOpen, QaToolingPrivacyRiskOpen, QaToolingLiveOpsFinalForbidden, Bee371Premature }
    public sealed class SocialMmoQaToolingInputSet
    {
        public SocialMmoQaToolingInputSet(string qaIntakeMatrix, string socialSignalTelemetryContract, string allianceActivityDashboard, string armyPvpBalanceSignalCatalog, string socialAbuseEarlyWarning, string liveOpsEventCandidates, string allianceCompetitionReadiness, string toolPermissionBoundary, string qaScenarioHandoffBundle)
        {
            QaIntakeMatrix = qaIntakeMatrix ?? string.Empty; SocialSignalTelemetryContract = socialSignalTelemetryContract ?? string.Empty; AllianceActivityDashboard = allianceActivityDashboard ?? string.Empty; ArmyPvpBalanceSignalCatalog = armyPvpBalanceSignalCatalog ?? string.Empty; SocialAbuseEarlyWarning = socialAbuseEarlyWarning ?? string.Empty; LiveOpsEventCandidates = liveOpsEventCandidates ?? string.Empty; AllianceCompetitionReadiness = allianceCompetitionReadiness ?? string.Empty; ToolPermissionBoundary = toolPermissionBoundary ?? string.Empty; QaScenarioHandoffBundle = qaScenarioHandoffBundle ?? string.Empty;
        }
        public string QaIntakeMatrix { get; } public string SocialSignalTelemetryContract { get; } public string AllianceActivityDashboard { get; } public string ArmyPvpBalanceSignalCatalog { get; } public string SocialAbuseEarlyWarning { get; } public string LiveOpsEventCandidates { get; } public string AllianceCompetitionReadiness { get; } public string ToolPermissionBoundary { get; } public string QaScenarioHandoffBundle { get; }
        public bool HasMissingInput() { return string.IsNullOrWhiteSpace(QaIntakeMatrix) || string.IsNullOrWhiteSpace(SocialSignalTelemetryContract) || string.IsNullOrWhiteSpace(AllianceActivityDashboard) || string.IsNullOrWhiteSpace(ArmyPvpBalanceSignalCatalog) || string.IsNullOrWhiteSpace(SocialAbuseEarlyWarning) || string.IsNullOrWhiteSpace(LiveOpsEventCandidates) || string.IsNullOrWhiteSpace(AllianceCompetitionReadiness) || string.IsNullOrWhiteSpace(ToolPermissionBoundary) || string.IsNullOrWhiteSpace(QaScenarioHandoffBundle); }
    }
    public sealed class SocialMmoQaToolingCoverage { public SocialMmoQaToolingCoverage(bool runtimeClaim, bool privacyRisk, bool liveOpsFinalClaim) { RuntimeClaim = runtimeClaim; PrivacyRisk = privacyRisk; LiveOpsFinalClaim = liveOpsFinalClaim; } public bool RuntimeClaim { get; } public bool PrivacyRisk { get; } public bool LiveOpsFinalClaim { get; } }
    public sealed class SocialMmoQaToolingRisk { public SocialMmoQaToolingRisk(string riskId, bool open) { RiskId = riskId ?? string.Empty; Open = open; } public string RiskId { get; } public bool Open { get; } }
    public sealed class SocialMmoQaToolingBlocker { public SocialMmoQaToolingBlocker(string blockerId, bool serverAuthorityGap) { BlockerId = blockerId ?? string.Empty; ServerAuthorityGap = serverAuthorityGap; } public string BlockerId { get; } public bool ServerAuthorityGap { get; } }
    public sealed class Bee371BlockerStatus { public Bee371BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class SocialMmoQaToolingReadinessGate
    {
        public const string Bee371BlockedMessage = "BEE-371 bloquee jusqu'a validation architecte.";
        public SocialMmoQaToolingReadinessGate(string gateId, SocialMmoQaToolingInputSet inputSet, SocialMmoQaToolingCoverage coverage, IReadOnlyList<SocialMmoQaToolingRisk> risks, IReadOnlyList<SocialMmoQaToolingBlocker> blockers, Bee371BlockerStatus bee371Status)
        {
            GateId = ColonyIntegrationIds.Require(gateId); InputSet = inputSet; Coverage = coverage; Risks = risks ?? Array.Empty<SocialMmoQaToolingRisk>(); Blockers = blockers ?? Array.Empty<SocialMmoQaToolingBlocker>(); Bee371Status = bee371Status;
        }
        public string GateId { get; } public SocialMmoQaToolingInputSet InputSet { get; } public SocialMmoQaToolingCoverage Coverage { get; } public IReadOnlyList<SocialMmoQaToolingRisk> Risks { get; } public IReadOnlyList<SocialMmoQaToolingBlocker> Blockers { get; } public Bee371BlockerStatus Bee371Status { get; }
        public SocialMmoQaToolingReadinessDiagnostics Evaluate()
        {
            var findings = new List<QaToolingDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput()) findings.Add(QaToolingDiagnosticCode.QaToolingInputMissing);
            if (Coverage != null && Coverage.RuntimeClaim) findings.Add(QaToolingDiagnosticCode.QaToolingRuntimeClaimDetected);
            if (Blockers.Any(b => b.ServerAuthorityGap)) findings.Add(QaToolingDiagnosticCode.QaToolingServerAuthorityGapOpen);
            if (Coverage != null && Coverage.PrivacyRisk) findings.Add(QaToolingDiagnosticCode.QaToolingPrivacyRiskOpen);
            if (Coverage != null && Coverage.LiveOpsFinalClaim) findings.Add(QaToolingDiagnosticCode.QaToolingLiveOpsFinalForbidden);
            if (Bee371Status != null && Bee371Status.PrematureAttempt) findings.Add(QaToolingDiagnosticCode.Bee371Premature);
            return new SocialMmoQaToolingReadinessDiagnostics(ResolveVerdict(findings), findings);
        }
        private static SocialMmoQaToolingVerdict ResolveVerdict(IReadOnlyList<QaToolingDiagnosticCode> findings)
        {
            if (findings.Contains(QaToolingDiagnosticCode.Bee371Premature)) return SocialMmoQaToolingVerdict.BlockedByBee371Premature;
            if (findings.Contains(QaToolingDiagnosticCode.QaToolingInputMissing)) return SocialMmoQaToolingVerdict.BlockedByMissingInput;
            if (findings.Contains(QaToolingDiagnosticCode.QaToolingRuntimeClaimDetected) || findings.Contains(QaToolingDiagnosticCode.QaToolingLiveOpsFinalForbidden)) return SocialMmoQaToolingVerdict.BlockedByRuntimeClaim;
            if (findings.Contains(QaToolingDiagnosticCode.QaToolingServerAuthorityGapOpen)) return SocialMmoQaToolingVerdict.BlockedByServerAuthorityGap;
            if (findings.Contains(QaToolingDiagnosticCode.QaToolingPrivacyRiskOpen)) return SocialMmoQaToolingVerdict.BlockedByPrivacyRisk;
            return SocialMmoQaToolingVerdict.ReadyForArchitectValidation;
        }
    }
    public sealed class SocialMmoQaToolingReadinessDiagnostics { public SocialMmoQaToolingReadinessDiagnostics(SocialMmoQaToolingVerdict verdict, IReadOnlyList<QaToolingDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<QaToolingDiagnosticCode>(); } public SocialMmoQaToolingVerdict Verdict { get; } public IReadOnlyList<QaToolingDiagnosticCode> Findings { get; } public bool Contains(QaToolingDiagnosticCode code) { return Findings.Contains(code); } }
}
