using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum SocialMmoPillarEvidenceStatus { Projected, Demonstrable, GapOpen, ServerBlocked, QaScenarioRequired }
    public enum SocialMmoPillarEvidenceDiagnosticCode { SocialMmoPillarEvidenceMissing, SocialMmoPillarGapOpen, SocialMmoRuntimeEvidenceForbidden, SocialMmoServerDependencyMissing, SocialMmoQaRiskUnmapped }

    public sealed class SocialMmoDemoEvidenceReference
    {
        public SocialMmoDemoEvidenceReference(string demoSurface, string evidenceKind)
        {
            DemoSurface = demoSurface ?? string.Empty;
            EvidenceKind = evidenceKind ?? string.Empty;
        }

        public string DemoSurface { get; }
        public string EvidenceKind { get; }
    }

    public sealed class SocialMmoServerDependencyReference
    {
        public SocialMmoServerDependencyReference(string topic, bool serverRequired)
        {
            Topic = topic ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string Topic { get; }
        public bool ServerRequired { get; }
    }

    public sealed class SocialMmoQaRiskReference
    {
        public SocialMmoQaRiskReference(string riskId, bool mapped)
        {
            RiskId = riskId ?? string.Empty;
            Mapped = mapped;
        }

        public string RiskId { get; }
        public bool Mapped { get; }
    }

    public sealed class SocialMmoPillarGap
    {
        public SocialMmoPillarGap(string gapId, SocialMmoProductPillar pillar, bool open)
        {
            GapId = gapId ?? string.Empty;
            Pillar = pillar;
            Open = open;
        }

        public string GapId { get; }
        public SocialMmoProductPillar Pillar { get; }
        public bool Open { get; }
    }

    public sealed class SocialMmoPillarEvidenceEntry
    {
        public SocialMmoPillarEvidenceEntry(SocialMmoProductPillar pillar, string sourceBee, string evidenceKind, SocialMmoDemoEvidenceReference demoReference, SocialMmoQaRiskReference qaRiskReference, SocialMmoServerDependencyReference serverDependency, SocialMmoPillarEvidenceStatus status, bool runtimeReadyClaimed = false)
        {
            Pillar = pillar;
            SourceBee = sourceBee ?? string.Empty;
            EvidenceKind = evidenceKind ?? string.Empty;
            DemoReference = demoReference;
            QaRiskReference = qaRiskReference;
            ServerDependency = serverDependency;
            Status = status;
            RuntimeReadyClaimed = runtimeReadyClaimed;
        }

        public SocialMmoProductPillar Pillar { get; }
        public string SourceBee { get; }
        public string EvidenceKind { get; }
        public SocialMmoDemoEvidenceReference DemoReference { get; }
        public SocialMmoQaRiskReference QaRiskReference { get; }
        public SocialMmoServerDependencyReference ServerDependency { get; }
        public SocialMmoPillarEvidenceStatus Status { get; }
        public bool RuntimeReadyClaimed { get; }
    }

    public sealed class SocialMmoPillarEvidenceDiagnostics
    {
        public SocialMmoPillarEvidenceDiagnostics(IReadOnlyList<SocialMmoPillarEvidenceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialMmoPillarEvidenceDiagnosticCode>(); }
        public IReadOnlyList<SocialMmoPillarEvidenceDiagnosticCode> Findings { get; }
        public bool Contains(SocialMmoPillarEvidenceDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class SocialMmoPillarEvidenceMatrix
    {
        public SocialMmoPillarEvidenceMatrix(string matrixId, IReadOnlyList<SocialMmoPillarEvidenceEntry> entries, IReadOnlyList<SocialMmoPillarGap> gaps)
        {
            MatrixId = ColonyIntegrationIds.Require(matrixId);
            Entries = entries ?? Array.Empty<SocialMmoPillarEvidenceEntry>();
            Gaps = gaps ?? Array.Empty<SocialMmoPillarGap>();
        }

        public string MatrixId { get; }
        public IReadOnlyList<SocialMmoPillarEvidenceEntry> Entries { get; }
        public IReadOnlyList<SocialMmoPillarGap> Gaps { get; }

        public SocialMmoPillarEvidenceDiagnostics Evaluate()
        {
            var findings = new List<SocialMmoPillarEvidenceDiagnosticCode>();
            if (Entries.Count == 0 || Entries.Any(e => string.IsNullOrWhiteSpace(e.SourceBee) || e.DemoReference == null)) findings.Add(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoPillarEvidenceMissing);
            if (Gaps.Any(g => g.Open) || Entries.Any(e => e.Status == SocialMmoPillarEvidenceStatus.GapOpen)) findings.Add(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoPillarGapOpen);
            if (Entries.Any(e => e.RuntimeReadyClaimed)) findings.Add(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoRuntimeEvidenceForbidden);
            if (Entries.Any(e => e.ServerDependency == null || string.IsNullOrWhiteSpace(e.ServerDependency.Topic))) findings.Add(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoServerDependencyMissing);
            if (Entries.Any(e => e.QaRiskReference == null || !e.QaRiskReference.Mapped)) findings.Add(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoQaRiskUnmapped);
            return new SocialMmoPillarEvidenceDiagnostics(findings);
        }
    }

    public enum AllianceCooperationDemoDiagnosticCode { AllianceCooperationDemoInputMissing, AllianceCooperationReadModelMutationForbidden, AllianceCooperationRewardForbidden, AllianceCooperationDeliveryForbidden, AllianceCooperationDemoLimitMissing }

    public sealed class AllianceCooperationObjectiveView
    {
        public AllianceCooperationObjectiveView(string sourceBee, string objectiveId) { SourceBee = sourceBee ?? string.Empty; ObjectiveId = objectiveId ?? string.Empty; }
        public string SourceBee { get; }
        public string ObjectiveId { get; }
    }

    public sealed class AllianceCooperationContributionView
    {
        public AllianceCooperationContributionView(string sourceBee, string contributionId, bool rewardRequested) { SourceBee = sourceBee ?? string.Empty; ContributionId = contributionId ?? string.Empty; RewardRequested = rewardRequested; }
        public string SourceBee { get; }
        public string ContributionId { get; }
        public bool RewardRequested { get; }
    }

    public sealed class AllianceCooperationMissionView
    {
        public AllianceCooperationMissionView(string sourceBee, string missionId, bool mutationRequested) { SourceBee = sourceBee ?? string.Empty; MissionId = missionId ?? string.Empty; MutationRequested = mutationRequested; }
        public string SourceBee { get; }
        public string MissionId { get; }
        public bool MutationRequested { get; }
    }

    public sealed class AllianceCooperationHelpRequestView
    {
        public AllianceCooperationHelpRequestView(string sourceBee, string requestId, bool deliveryRequested) { SourceBee = sourceBee ?? string.Empty; RequestId = requestId ?? string.Empty; DeliveryRequested = deliveryRequested; }
        public string SourceBee { get; }
        public string RequestId { get; }
        public bool DeliveryRequested { get; }
    }

    public sealed class AllianceCooperationDemoLimit
    {
        public AllianceCooperationDemoLimit(string limitId, bool declared) { LimitId = limitId ?? string.Empty; Declared = declared; }
        public string LimitId { get; }
        public bool Declared { get; }
    }

    public sealed class AllianceCooperationReadModelDiagnostics
    {
        public AllianceCooperationReadModelDiagnostics(IReadOnlyList<AllianceCooperationDemoDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceCooperationDemoDiagnosticCode>(); }
        public IReadOnlyList<AllianceCooperationDemoDiagnosticCode> Findings { get; }
        public bool Contains(AllianceCooperationDemoDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class AllianceCooperationDemoReadModel
    {
        public AllianceCooperationDemoReadModel(string allianceProjectionId, IReadOnlyList<AllianceCooperationObjectiveView> objectiveViews, IReadOnlyList<AllianceCooperationContributionView> contributionViews, IReadOnlyList<AllianceCooperationMissionView> missionViews, IReadOnlyList<AllianceCooperationHelpRequestView> helpRequestViews, IReadOnlyList<AllianceCooperationDemoLimit> demoLimits, bool officialGameplayAllowed)
        {
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            ObjectiveViews = objectiveViews ?? Array.Empty<AllianceCooperationObjectiveView>();
            ContributionViews = contributionViews ?? Array.Empty<AllianceCooperationContributionView>();
            MissionViews = missionViews ?? Array.Empty<AllianceCooperationMissionView>();
            HelpRequestViews = helpRequestViews ?? Array.Empty<AllianceCooperationHelpRequestView>();
            DemoLimits = demoLimits ?? Array.Empty<AllianceCooperationDemoLimit>();
            OfficialGameplayAllowed = officialGameplayAllowed;
        }

        public string AllianceProjectionId { get; }
        public IReadOnlyList<AllianceCooperationObjectiveView> ObjectiveViews { get; }
        public IReadOnlyList<AllianceCooperationContributionView> ContributionViews { get; }
        public IReadOnlyList<AllianceCooperationMissionView> MissionViews { get; }
        public IReadOnlyList<AllianceCooperationHelpRequestView> HelpRequestViews { get; }
        public IReadOnlyList<AllianceCooperationDemoLimit> DemoLimits { get; }
        public bool OfficialGameplayAllowed { get; }

        public AllianceCooperationReadModelDiagnostics Evaluate()
        {
            var findings = new List<AllianceCooperationDemoDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(AllianceProjectionId) || ObjectiveViews.Count == 0 || ContributionViews.Count == 0 || MissionViews.Count == 0 || HelpRequestViews.Count == 0) findings.Add(AllianceCooperationDemoDiagnosticCode.AllianceCooperationDemoInputMissing);
            if (OfficialGameplayAllowed || MissionViews.Any(v => v.MutationRequested)) findings.Add(AllianceCooperationDemoDiagnosticCode.AllianceCooperationReadModelMutationForbidden);
            if (ContributionViews.Any(v => v.RewardRequested)) findings.Add(AllianceCooperationDemoDiagnosticCode.AllianceCooperationRewardForbidden);
            if (HelpRequestViews.Any(v => v.DeliveryRequested)) findings.Add(AllianceCooperationDemoDiagnosticCode.AllianceCooperationDeliveryForbidden);
            if (DemoLimits.Count == 0 || DemoLimits.Any(l => !l.Declared)) findings.Add(AllianceCooperationDemoDiagnosticCode.AllianceCooperationDemoLimitMissing);
            return new AllianceCooperationReadModelDiagnostics(findings);
        }
    }

    public enum ArmyReadinessRiskSeverity { Low, Medium, High, Blocking }
    public enum ArmyReadinessRiskDiagnosticCode { ArmyRiskSourceMissing, ArmyRiskSeverityMissing, ArmyPayToWinRiskUntracked, ArmySnowballRiskUntracked, ArmyServerAuthorityRiskOpen, ArmyQaScenarioMissing }

    public sealed class ArmyBalanceRiskSource
    {
        public ArmyBalanceRiskSource(string sourceBee, string riskType) { SourceBee = sourceBee ?? string.Empty; RiskType = riskType ?? string.Empty; }
        public string SourceBee { get; }
        public string RiskType { get; }
    }

    public sealed class ArmyServerAuthorityRisk
    {
        public ArmyServerAuthorityRisk(string topic, bool open) { Topic = topic ?? string.Empty; Open = open; }
        public string Topic { get; }
        public bool Open { get; }
    }

    public sealed class ArmyDemoRiskVisibility
    {
        public ArmyDemoRiskVisibility(string surface, bool visible) { Surface = surface ?? string.Empty; Visible = visible; }
        public string Surface { get; }
        public bool Visible { get; }
    }

    public sealed class ArmyQaScenarioNeed
    {
        public ArmyQaScenarioNeed(string scenarioId, bool missing) { ScenarioId = scenarioId ?? string.Empty; Missing = missing; }
        public string ScenarioId { get; }
        public bool Missing { get; }
    }

    public sealed class ArmyReadinessRiskEntry
    {
        public ArmyReadinessRiskEntry(string riskId, string sourceBee, string riskType, ArmyReadinessRiskSeverity severity, string playerImpact, ArmyDemoRiskVisibility demoVisibility, ArmyQaScenarioNeed qaScenarioNeed, ArmyServerAuthorityRisk serverAuthorityTopic)
        {
            RiskId = riskId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            RiskType = riskType ?? string.Empty;
            Severity = severity;
            PlayerImpact = playerImpact ?? string.Empty;
            DemoVisibility = demoVisibility;
            QaScenarioNeed = qaScenarioNeed;
            ServerAuthorityTopic = serverAuthorityTopic;
        }

        public string RiskId { get; }
        public string SourceBee { get; }
        public string RiskType { get; }
        public ArmyReadinessRiskSeverity Severity { get; }
        public string PlayerImpact { get; }
        public ArmyDemoRiskVisibility DemoVisibility { get; }
        public ArmyQaScenarioNeed QaScenarioNeed { get; }
        public ArmyServerAuthorityRisk ServerAuthorityTopic { get; }
    }

    public sealed class ArmyReadinessRiskDiagnostics
    {
        public ArmyReadinessRiskDiagnostics(IReadOnlyList<ArmyReadinessRiskDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyReadinessRiskDiagnosticCode>(); }
        public IReadOnlyList<ArmyReadinessRiskDiagnosticCode> Findings { get; }
        public bool Contains(ArmyReadinessRiskDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class ArmyReadinessRiskRegister
    {
        public ArmyReadinessRiskRegister(string registerId, IReadOnlyList<ArmyReadinessRiskEntry> risks)
        {
            RegisterId = ColonyIntegrationIds.Require(registerId);
            Risks = risks ?? Array.Empty<ArmyReadinessRiskEntry>();
        }

        public string RegisterId { get; }
        public IReadOnlyList<ArmyReadinessRiskEntry> Risks { get; }

        public ArmyReadinessRiskDiagnostics Evaluate()
        {
            var findings = new List<ArmyReadinessRiskDiagnosticCode>();
            if (Risks.Count == 0 || Risks.Any(r => string.IsNullOrWhiteSpace(r.SourceBee))) findings.Add(ArmyReadinessRiskDiagnosticCode.ArmyRiskSourceMissing);
            if (Risks.Any(r => string.IsNullOrWhiteSpace(r.RiskType))) findings.Add(ArmyReadinessRiskDiagnosticCode.ArmyRiskSeverityMissing);
            if (!Risks.Any(r => string.Equals(r.RiskType, "payToWin", StringComparison.OrdinalIgnoreCase))) findings.Add(ArmyReadinessRiskDiagnosticCode.ArmyPayToWinRiskUntracked);
            if (!Risks.Any(r => string.Equals(r.RiskType, "snowball", StringComparison.OrdinalIgnoreCase))) findings.Add(ArmyReadinessRiskDiagnosticCode.ArmySnowballRiskUntracked);
            if (Risks.Any(r => r.ServerAuthorityTopic == null || r.ServerAuthorityTopic.Open)) findings.Add(ArmyReadinessRiskDiagnosticCode.ArmyServerAuthorityRiskOpen);
            if (Risks.Any(r => r.QaScenarioNeed == null || r.QaScenarioNeed.Missing)) findings.Add(ArmyReadinessRiskDiagnosticCode.ArmyQaScenarioMissing);
            return new ArmyReadinessRiskDiagnostics(findings);
        }
    }

    public enum FairPvpScenarioDiagnosticCode { FairPvPScenarioMissing, FairPvPProtectionExpectationMissing, FairPvPFailureModeUnmapped, FairPvPMatchmakingForbidden, FairPvPRewardForbidden, FairPvPServerAuthorityRequired }

    public sealed class FairPvPProtectionExpectation
    {
        public FairPvPProtectionExpectation(string protectionId, bool present) { ProtectionId = protectionId ?? string.Empty; Present = present; }
        public string ProtectionId { get; }
        public bool Present { get; }
    }

    public sealed class FairPvPFailureMode
    {
        public FairPvPFailureMode(string failureId, bool mapped) { FailureId = failureId ?? string.Empty; Mapped = mapped; }
        public string FailureId { get; }
        public bool Mapped { get; }
    }

    public sealed class FairPvPDemoEvidenceNeed
    {
        public FairPvPDemoEvidenceNeed(string evidenceId, bool present) { EvidenceId = evidenceId ?? string.Empty; Present = present; }
        public string EvidenceId { get; }
        public bool Present { get; }
    }

    public sealed class FairPvPServerAuthorityTopic
    {
        public FairPvPServerAuthorityTopic(string topicId, bool serverRequired) { TopicId = topicId ?? string.Empty; ServerRequired = serverRequired; }
        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class FairPvPScenarioEntry
    {
        public FairPvPScenarioEntry(string scenarioId, string scenarioType, IReadOnlyList<string> actors, string risk, IReadOnlyList<FairPvPProtectionExpectation> protectionExpectations, IReadOnlyList<FairPvPFailureMode> failureModes, FairPvPDemoEvidenceNeed demoEvidenceNeed, FairPvPServerAuthorityTopic serverAuthorityTopic, bool runtimeExecutionAllowed, bool matchmakingRequested = false, bool rewardRequested = false)
        {
            ScenarioId = scenarioId ?? string.Empty;
            ScenarioType = scenarioType ?? string.Empty;
            Actors = actors ?? Array.Empty<string>();
            Risk = risk ?? string.Empty;
            ProtectionExpectations = protectionExpectations ?? Array.Empty<FairPvPProtectionExpectation>();
            FailureModes = failureModes ?? Array.Empty<FairPvPFailureMode>();
            DemoEvidenceNeed = demoEvidenceNeed;
            ServerAuthorityTopic = serverAuthorityTopic;
            RuntimeExecutionAllowed = runtimeExecutionAllowed;
            MatchmakingRequested = matchmakingRequested;
            RewardRequested = rewardRequested;
        }

        public string ScenarioId { get; }
        public string ScenarioType { get; }
        public IReadOnlyList<string> Actors { get; }
        public string Risk { get; }
        public IReadOnlyList<FairPvPProtectionExpectation> ProtectionExpectations { get; }
        public IReadOnlyList<FairPvPFailureMode> FailureModes { get; }
        public FairPvPDemoEvidenceNeed DemoEvidenceNeed { get; }
        public FairPvPServerAuthorityTopic ServerAuthorityTopic { get; }
        public bool RuntimeExecutionAllowed { get; }
        public bool MatchmakingRequested { get; }
        public bool RewardRequested { get; }
    }

    public sealed class FairPvpScenarioDiagnostics
    {
        public FairPvpScenarioDiagnostics(IReadOnlyList<FairPvpScenarioDiagnosticCode> findings) { Findings = findings ?? Array.Empty<FairPvpScenarioDiagnosticCode>(); }
        public IReadOnlyList<FairPvpScenarioDiagnosticCode> Findings { get; }
        public bool Contains(FairPvpScenarioDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class FairPvpScenarioCatalog
    {
        public FairPvpScenarioCatalog(string catalogId, IReadOnlyList<FairPvPScenarioEntry> scenarios)
        {
            CatalogId = ColonyIntegrationIds.Require(catalogId);
            Scenarios = scenarios ?? Array.Empty<FairPvPScenarioEntry>();
        }

        public string CatalogId { get; }
        public IReadOnlyList<FairPvPScenarioEntry> Scenarios { get; }

        public FairPvpScenarioDiagnostics Evaluate()
        {
            var findings = new List<FairPvpScenarioDiagnosticCode>();
            if (Scenarios.Count == 0 || Scenarios.Any(s => string.IsNullOrWhiteSpace(s.ScenarioId) || s.Actors.Count == 0)) findings.Add(FairPvpScenarioDiagnosticCode.FairPvPScenarioMissing);
            if (Scenarios.Any(s => s.ProtectionExpectations.Count == 0 || s.ProtectionExpectations.Any(p => !p.Present))) findings.Add(FairPvpScenarioDiagnosticCode.FairPvPProtectionExpectationMissing);
            if (Scenarios.Any(s => s.FailureModes.Count == 0 || s.FailureModes.Any(f => !f.Mapped))) findings.Add(FairPvpScenarioDiagnosticCode.FairPvPFailureModeUnmapped);
            if (Scenarios.Any(s => s.RuntimeExecutionAllowed || s.MatchmakingRequested)) findings.Add(FairPvpScenarioDiagnosticCode.FairPvPMatchmakingForbidden);
            if (Scenarios.Any(s => s.RewardRequested)) findings.Add(FairPvpScenarioDiagnosticCode.FairPvPRewardForbidden);
            if (Scenarios.Any(s => s.ServerAuthorityTopic == null || s.ServerAuthorityTopic.ServerRequired)) findings.Add(FairPvpScenarioDiagnosticCode.FairPvPServerAuthorityRequired);
            return new FairPvpScenarioDiagnostics(findings);
        }
    }

    public enum SocialServerEscalationStatus { RequiresBeeServerScan, PendingServerSpec, BlockedUntilAuthorized }
    public enum SocialServerEscalationDiagnosticCode { SocialServerEscalationItemMissing, SocialServerSourceBeeMissing, SocialServerOwnerMissing, SocialServerScanRequired, SocialServerImplementationForbidden, Server018CreationForbidden }

    public sealed class SocialServerOwnerHint
    {
        public SocialServerOwnerHint(string ownerId) { OwnerId = ownerId ?? string.Empty; }
        public string OwnerId { get; }
    }

    public sealed class SocialServerBlockerReason
    {
        public SocialServerBlockerReason(string reasonId) { ReasonId = reasonId ?? string.Empty; }
        public string ReasonId { get; }
    }

    public sealed class SocialServerImplementationForbiddenMarker
    {
        public SocialServerImplementationForbiddenMarker(bool implementationRequested, bool server018Requested) { ImplementationRequested = implementationRequested; Server018Requested = server018Requested; }
        public bool ImplementationRequested { get; }
        public bool Server018Requested { get; }
    }

    public sealed class SocialServerEscalationItem
    {
        public SocialServerEscalationItem(string itemId, string sourceBee, string category, string playerRisk, SocialServerOwnerHint serverOwnerHint, SocialServerBlockerReason blockerReason, SocialServerEscalationStatus status)
        {
            ItemId = itemId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            Category = category ?? string.Empty;
            PlayerRisk = playerRisk ?? string.Empty;
            ServerOwnerHint = serverOwnerHint;
            BlockerReason = blockerReason;
            Status = status;
        }

        public string ItemId { get; }
        public string SourceBee { get; }
        public string Category { get; }
        public string PlayerRisk { get; }
        public SocialServerOwnerHint ServerOwnerHint { get; }
        public SocialServerBlockerReason BlockerReason { get; }
        public SocialServerEscalationStatus Status { get; }
    }

    public sealed class SocialServerEscalationDiagnostics
    {
        public SocialServerEscalationDiagnostics(IReadOnlyList<SocialServerEscalationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialServerEscalationDiagnosticCode>(); }
        public IReadOnlyList<SocialServerEscalationDiagnosticCode> Findings { get; }
        public bool Contains(SocialServerEscalationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class SocialServerBundleExport
    {
        public SocialServerBundleExport(string exportId, IReadOnlyList<SocialServerEscalationItem> items) { ExportId = exportId ?? string.Empty; Items = items ?? Array.Empty<SocialServerEscalationItem>(); }
        public string ExportId { get; }
        public IReadOnlyList<SocialServerEscalationItem> Items { get; }
    }

    public sealed class SocialServerEscalationBundle
    {
        public SocialServerEscalationBundle(string bundleId, IReadOnlyList<SocialServerEscalationItem> items, SocialServerImplementationForbiddenMarker forbiddenMarker)
        {
            BundleId = ColonyIntegrationIds.Require(bundleId);
            Items = items ?? Array.Empty<SocialServerEscalationItem>();
            ForbiddenMarker = forbiddenMarker;
        }

        public string BundleId { get; }
        public IReadOnlyList<SocialServerEscalationItem> Items { get; }
        public SocialServerImplementationForbiddenMarker ForbiddenMarker { get; }

        public SocialServerEscalationDiagnostics Evaluate()
        {
            var findings = new List<SocialServerEscalationDiagnosticCode>();
            if (Items.Count == 0) findings.Add(SocialServerEscalationDiagnosticCode.SocialServerEscalationItemMissing);
            if (Items.Any(i => string.IsNullOrWhiteSpace(i.SourceBee))) findings.Add(SocialServerEscalationDiagnosticCode.SocialServerSourceBeeMissing);
            if (Items.Any(i => i.ServerOwnerHint == null || string.IsNullOrWhiteSpace(i.ServerOwnerHint.OwnerId))) findings.Add(SocialServerEscalationDiagnosticCode.SocialServerOwnerMissing);
            if (Items.Any(i => i.Status == SocialServerEscalationStatus.RequiresBeeServerScan)) findings.Add(SocialServerEscalationDiagnosticCode.SocialServerScanRequired);
            if (ForbiddenMarker != null && ForbiddenMarker.ImplementationRequested) findings.Add(SocialServerEscalationDiagnosticCode.SocialServerImplementationForbidden);
            if (ForbiddenMarker != null && ForbiddenMarker.Server018Requested) findings.Add(SocialServerEscalationDiagnosticCode.Server018CreationForbidden);
            return new SocialServerEscalationDiagnostics(findings);
        }

        public SocialServerBundleExport Export(string exportId) { return new SocialServerBundleExport(exportId, Items); }
    }

    public enum PlayerRetentionAfterConflictDiagnosticCode { PostConflictRecoveryPathMissing, PostConflictChurnRiskOpen, PostConflictRewardForbidden, PostConflictCompensationForbidden, PostConflictProtectionServerRequired }

    public sealed class PostConflictRecoveryPath
    {
        public PostConflictRecoveryPath(string pathId, bool missing) { PathId = pathId ?? string.Empty; Missing = missing; }
        public string PathId { get; }
        public bool Missing { get; }
    }

    public sealed class PostConflictMotivationSignal
    {
        public PostConflictMotivationSignal(string signalId) { SignalId = signalId ?? string.Empty; }
        public string SignalId { get; }
    }

    public sealed class PostConflictChurnRisk
    {
        public PostConflictChurnRisk(string riskId, bool open) { RiskId = riskId ?? string.Empty; Open = open; }
        public string RiskId { get; }
        public bool Open { get; }
    }

    public sealed class PostConflictAllianceSupportReference
    {
        public PostConflictAllianceSupportReference(string supportId, bool rewardRequested, bool compensationRequested) { SupportId = supportId ?? string.Empty; RewardRequested = rewardRequested; CompensationRequested = compensationRequested; }
        public string SupportId { get; }
        public bool RewardRequested { get; }
        public bool CompensationRequested { get; }
    }

    public sealed class PostConflictServerAuthorityTopic
    {
        public PostConflictServerAuthorityTopic(string topicId, bool serverRequired) { TopicId = topicId ?? string.Empty; ServerRequired = serverRequired; }
        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class PlayerRetentionAfterConflictProjection
    {
        public PlayerRetentionAfterConflictProjection(string playerHiveIdentityId, string conflictScenario, PostConflictRecoveryPath recoveryPath, IReadOnlyList<PostConflictMotivationSignal> motivationSignals, IReadOnlyList<PostConflictChurnRisk> churnRisks, PostConflictAllianceSupportReference allianceSupport, IReadOnlyList<PostConflictServerAuthorityTopic> serverAuthorityTopics)
        {
            PlayerHiveIdentityId = playerHiveIdentityId ?? string.Empty;
            ConflictScenario = conflictScenario ?? string.Empty;
            RecoveryPath = recoveryPath;
            MotivationSignals = motivationSignals ?? Array.Empty<PostConflictMotivationSignal>();
            ChurnRisks = churnRisks ?? Array.Empty<PostConflictChurnRisk>();
            AllianceSupport = allianceSupport;
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<PostConflictServerAuthorityTopic>();
        }

        public string PlayerHiveIdentityId { get; }
        public string ConflictScenario { get; }
        public PostConflictRecoveryPath RecoveryPath { get; }
        public IReadOnlyList<PostConflictMotivationSignal> MotivationSignals { get; }
        public IReadOnlyList<PostConflictChurnRisk> ChurnRisks { get; }
        public PostConflictAllianceSupportReference AllianceSupport { get; }
        public IReadOnlyList<PostConflictServerAuthorityTopic> ServerAuthorityTopics { get; }

        public PlayerRetentionAfterConflictDiagnostics Evaluate()
        {
            var findings = new List<PlayerRetentionAfterConflictDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(PlayerHiveIdentityId) || string.IsNullOrWhiteSpace(ConflictScenario) || RecoveryPath == null || RecoveryPath.Missing) findings.Add(PlayerRetentionAfterConflictDiagnosticCode.PostConflictRecoveryPathMissing);
            if (ChurnRisks.Any(r => r.Open)) findings.Add(PlayerRetentionAfterConflictDiagnosticCode.PostConflictChurnRiskOpen);
            if (AllianceSupport != null && AllianceSupport.RewardRequested) findings.Add(PlayerRetentionAfterConflictDiagnosticCode.PostConflictRewardForbidden);
            if (AllianceSupport != null && AllianceSupport.CompensationRequested) findings.Add(PlayerRetentionAfterConflictDiagnosticCode.PostConflictCompensationForbidden);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired)) findings.Add(PlayerRetentionAfterConflictDiagnosticCode.PostConflictProtectionServerRequired);
            return new PlayerRetentionAfterConflictDiagnostics(findings);
        }
    }

    public sealed class PlayerRetentionAfterConflictDiagnostics
    {
        public PlayerRetentionAfterConflictDiagnostics(IReadOnlyList<PlayerRetentionAfterConflictDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlayerRetentionAfterConflictDiagnosticCode>(); }
        public IReadOnlyList<PlayerRetentionAfterConflictDiagnosticCode> Findings { get; }
        public bool Contains(PlayerRetentionAfterConflictDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum WarCoordinationReadinessStatus { ReadyProjected, Partial, Blocked, ServerRequired }
    public enum AllianceWarCoordinationReadinessDiagnosticCode { WarCoordinationInputMissing, WarCoordinationGapOpen, WarCoordinationRuntimeForbidden, WarCoordinationProtectionMissing, WarCoordinationServerAuthorityRequired }

    public sealed class WarCoordinationDependency
    {
        public WarCoordinationDependency(string dependencyId, bool missing) { DependencyId = dependencyId ?? string.Empty; Missing = missing; }
        public string DependencyId { get; }
        public bool Missing { get; }
    }

    public sealed class WarCoordinationGap
    {
        public WarCoordinationGap(string gapId, bool open) { GapId = gapId ?? string.Empty; Open = open; }
        public string GapId { get; }
        public bool Open { get; }
    }

    public sealed class WarCoordinationRuntimeForbiddenMarker
    {
        public WarCoordinationRuntimeForbiddenMarker(bool warDeclarationRequested, bool mobilizationRequested) { WarDeclarationRequested = warDeclarationRequested; MobilizationRequested = mobilizationRequested; }
        public bool WarDeclarationRequested { get; }
        public bool MobilizationRequested { get; }
    }

    public sealed class WarCoordinationServerAuthorityTopic
    {
        public WarCoordinationServerAuthorityTopic(string topicId, bool serverRequired) { TopicId = topicId ?? string.Empty; ServerRequired = serverRequired; }
        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class WarCoordinationReadinessEntry
    {
        public WarCoordinationReadinessEntry(string axis, string sourceBee, WarCoordinationReadinessStatus status, IReadOnlyList<WarCoordinationDependency> dependencies, IReadOnlyList<WarCoordinationGap> gaps, WarCoordinationServerAuthorityTopic serverAuthorityTopic, bool protectionPresent)
        {
            Axis = axis ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            Status = status;
            Dependencies = dependencies ?? Array.Empty<WarCoordinationDependency>();
            Gaps = gaps ?? Array.Empty<WarCoordinationGap>();
            ServerAuthorityTopic = serverAuthorityTopic;
            ProtectionPresent = protectionPresent;
        }

        public string Axis { get; }
        public string SourceBee { get; }
        public WarCoordinationReadinessStatus Status { get; }
        public IReadOnlyList<WarCoordinationDependency> Dependencies { get; }
        public IReadOnlyList<WarCoordinationGap> Gaps { get; }
        public WarCoordinationServerAuthorityTopic ServerAuthorityTopic { get; }
        public bool ProtectionPresent { get; }
    }

    public sealed class AllianceWarCoordinationReadinessDiagnostics
    {
        public AllianceWarCoordinationReadinessDiagnostics(IReadOnlyList<AllianceWarCoordinationReadinessDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceWarCoordinationReadinessDiagnosticCode>(); }
        public IReadOnlyList<AllianceWarCoordinationReadinessDiagnosticCode> Findings { get; }
        public bool Contains(AllianceWarCoordinationReadinessDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class AllianceWarCoordinationReadinessMatrix
    {
        public AllianceWarCoordinationReadinessMatrix(string matrixId, IReadOnlyList<WarCoordinationReadinessEntry> entries, WarCoordinationRuntimeForbiddenMarker runtimeMarker)
        {
            MatrixId = ColonyIntegrationIds.Require(matrixId);
            Entries = entries ?? Array.Empty<WarCoordinationReadinessEntry>();
            RuntimeMarker = runtimeMarker;
        }

        public string MatrixId { get; }
        public IReadOnlyList<WarCoordinationReadinessEntry> Entries { get; }
        public WarCoordinationRuntimeForbiddenMarker RuntimeMarker { get; }

        public AllianceWarCoordinationReadinessDiagnostics Evaluate()
        {
            var findings = new List<AllianceWarCoordinationReadinessDiagnosticCode>();
            if (Entries.Count == 0 || Entries.Any(e => string.IsNullOrWhiteSpace(e.SourceBee) || e.Dependencies.Any(d => d.Missing))) findings.Add(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationInputMissing);
            if (Entries.Any(e => e.Gaps.Any(g => g.Open) || e.Status == WarCoordinationReadinessStatus.Blocked)) findings.Add(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationGapOpen);
            if (RuntimeMarker != null && (RuntimeMarker.WarDeclarationRequested || RuntimeMarker.MobilizationRequested)) findings.Add(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationRuntimeForbidden);
            if (Entries.Any(e => !e.ProtectionPresent)) findings.Add(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationProtectionMissing);
            if (Entries.Any(e => e.ServerAuthorityTopic == null || e.ServerAuthorityTopic.ServerRequired || e.Status == WarCoordinationReadinessStatus.ServerRequired)) findings.Add(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationServerAuthorityRequired);
            return new AllianceWarCoordinationReadinessDiagnostics(findings);
        }
    }

    public enum SocialMmoDemoAcceptanceDiagnosticCode { SocialMmoDemoProofMissing, SocialMmoDemoGapHidden, SocialMmoDemoRuntimeClaimForbidden, SocialMmoDemoLimitMissing, SocialMmoDemoSeparateSpecForbidden }

    public sealed class SocialMmoDemoVisibleProof
    {
        public SocialMmoDemoVisibleProof(string proofId, bool visible) { ProofId = proofId ?? string.Empty; Visible = visible; }
        public string ProofId { get; }
        public bool Visible { get; }
    }

    public sealed class SocialMmoDemoGap
    {
        public SocialMmoDemoGap(string gapId, bool hidden) { GapId = gapId ?? string.Empty; Hidden = hidden; }
        public string GapId { get; }
        public bool Hidden { get; }
    }

    public sealed class SocialMmoDemoWarning
    {
        public SocialMmoDemoWarning(string warningId, bool runtimeClaim) { WarningId = warningId ?? string.Empty; RuntimeClaim = runtimeClaim; }
        public string WarningId { get; }
        public bool RuntimeClaim { get; }
    }

    public sealed class SocialMmoDemoExternalObserverChecklist
    {
        public SocialMmoDemoExternalObserverChecklist(string checklistId, bool complete) { ChecklistId = checklistId ?? string.Empty; Complete = complete; }
        public string ChecklistId { get; }
        public bool Complete { get; }
    }

    public sealed class SocialMmoDemoLimit
    {
        public SocialMmoDemoLimit(string limitId, bool declared) { LimitId = limitId ?? string.Empty; Declared = declared; }
        public string LimitId { get; }
        public bool Declared { get; }
    }

    public sealed class SocialMmoDemoAcceptanceDiagnostics
    {
        public SocialMmoDemoAcceptanceDiagnostics(IReadOnlyList<SocialMmoDemoAcceptanceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialMmoDemoAcceptanceDiagnosticCode>(); }
        public IReadOnlyList<SocialMmoDemoAcceptanceDiagnosticCode> Findings { get; }
        public bool Contains(SocialMmoDemoAcceptanceDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class SocialMmoDemoAcceptanceSnapshot
    {
        public SocialMmoDemoAcceptanceSnapshot(string snapshotId, IReadOnlyList<SocialMmoDemoVisibleProof> visibleProofs, IReadOnlyList<SocialMmoDemoGap> gaps, IReadOnlyList<SocialMmoDemoWarning> warnings, SocialMmoDemoExternalObserverChecklist observerChecklist, IReadOnlyList<SocialMmoDemoLimit> limits, bool alphaReadyClaimAllowed, bool separateDemoSpecRequested = false)
        {
            SnapshotId = ColonyIntegrationIds.Require(snapshotId);
            VisibleProofs = visibleProofs ?? Array.Empty<SocialMmoDemoVisibleProof>();
            Gaps = gaps ?? Array.Empty<SocialMmoDemoGap>();
            Warnings = warnings ?? Array.Empty<SocialMmoDemoWarning>();
            ObserverChecklist = observerChecklist;
            Limits = limits ?? Array.Empty<SocialMmoDemoLimit>();
            AlphaReadyClaimAllowed = alphaReadyClaimAllowed;
            SeparateDemoSpecRequested = separateDemoSpecRequested;
        }

        public string SnapshotId { get; }
        public IReadOnlyList<SocialMmoDemoVisibleProof> VisibleProofs { get; }
        public IReadOnlyList<SocialMmoDemoGap> Gaps { get; }
        public IReadOnlyList<SocialMmoDemoWarning> Warnings { get; }
        public SocialMmoDemoExternalObserverChecklist ObserverChecklist { get; }
        public IReadOnlyList<SocialMmoDemoLimit> Limits { get; }
        public bool AlphaReadyClaimAllowed { get; }
        public bool SeparateDemoSpecRequested { get; }

        public SocialMmoDemoAcceptanceDiagnostics Evaluate()
        {
            var findings = new List<SocialMmoDemoAcceptanceDiagnosticCode>();
            if (VisibleProofs.Count == 0 || VisibleProofs.Any(p => !p.Visible)) findings.Add(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoProofMissing);
            if (Gaps.Any(g => g.Hidden)) findings.Add(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoGapHidden);
            if (AlphaReadyClaimAllowed || Warnings.Any(w => w.RuntimeClaim)) findings.Add(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoRuntimeClaimForbidden);
            if (Limits.Count == 0 || Limits.Any(l => !l.Declared)) findings.Add(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoLimitMissing);
            if (SeparateDemoSpecRequested) findings.Add(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoSeparateSpecForbidden);
            return new SocialMmoDemoAcceptanceDiagnostics(findings);
        }
    }

    public enum SocialMmoMilestoneDiagnosticCode { SocialMmoMilestoneInputMissing, SocialMmoMilestoneRiskUnowned, SocialMmoNextGateMissing, SocialMmoAlphaReadyClaimForbidden, SocialMmoServerOwnerMissing }

    public sealed class SocialMmoMilestoneAchievement
    {
        public SocialMmoMilestoneAchievement(string achievementId, string sourceBee) { AchievementId = achievementId ?? string.Empty; SourceBee = sourceBee ?? string.Empty; }
        public string AchievementId { get; }
        public string SourceBee { get; }
    }

    public sealed class SocialMmoMilestoneOpenRisk
    {
        public SocialMmoMilestoneOpenRisk(string riskId, string owner) { RiskId = riskId ?? string.Empty; Owner = owner ?? string.Empty; }
        public string RiskId { get; }
        public string Owner { get; }
    }

    public sealed class SocialMmoMilestoneNextGate
    {
        public SocialMmoMilestoneNextGate(string gateId) { GateId = gateId ?? string.Empty; }
        public string GateId { get; }
    }

    public sealed class SocialMmoMilestoneOwnerMap
    {
        public SocialMmoMilestoneOwnerMap(IReadOnlyList<string> owners, bool serverOwnerPresent) { Owners = owners ?? Array.Empty<string>(); ServerOwnerPresent = serverOwnerPresent; }
        public IReadOnlyList<string> Owners { get; }
        public bool ServerOwnerPresent { get; }
    }

    public sealed class SocialMmoAlphaNotReadyMarker
    {
        public SocialMmoAlphaNotReadyMarker(bool alphaReady) { AlphaReady = alphaReady; }
        public bool AlphaReady { get; }
    }

    public sealed class SocialMmoMilestoneDiagnostics
    {
        public SocialMmoMilestoneDiagnostics(IReadOnlyList<SocialMmoMilestoneDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialMmoMilestoneDiagnosticCode>(); }
        public IReadOnlyList<SocialMmoMilestoneDiagnosticCode> Findings { get; }
        public bool Contains(SocialMmoMilestoneDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class SocialMmoMilestoneProjection
    {
        public SocialMmoMilestoneProjection(string milestoneId, string coveredBeeRange, IReadOnlyList<SocialMmoMilestoneAchievement> achievements, IReadOnlyList<SocialMmoMilestoneOpenRisk> openRisks, IReadOnlyList<SocialMmoMilestoneNextGate> nextGates, SocialMmoMilestoneOwnerMap ownerMap, bool alphaReady)
        {
            MilestoneId = ColonyIntegrationIds.Require(milestoneId);
            CoveredBeeRange = coveredBeeRange ?? string.Empty;
            Achievements = achievements ?? Array.Empty<SocialMmoMilestoneAchievement>();
            OpenRisks = openRisks ?? Array.Empty<SocialMmoMilestoneOpenRisk>();
            NextGates = nextGates ?? Array.Empty<SocialMmoMilestoneNextGate>();
            OwnerMap = ownerMap;
            AlphaReady = alphaReady;
        }

        public string MilestoneId { get; }
        public string CoveredBeeRange { get; }
        public IReadOnlyList<SocialMmoMilestoneAchievement> Achievements { get; }
        public IReadOnlyList<SocialMmoMilestoneOpenRisk> OpenRisks { get; }
        public IReadOnlyList<SocialMmoMilestoneNextGate> NextGates { get; }
        public SocialMmoMilestoneOwnerMap OwnerMap { get; }
        public bool AlphaReady { get; }

        public SocialMmoMilestoneDiagnostics Evaluate()
        {
            var findings = new List<SocialMmoMilestoneDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(CoveredBeeRange) || Achievements.Count == 0) findings.Add(SocialMmoMilestoneDiagnosticCode.SocialMmoMilestoneInputMissing);
            if (OpenRisks.Any(r => string.IsNullOrWhiteSpace(r.Owner))) findings.Add(SocialMmoMilestoneDiagnosticCode.SocialMmoMilestoneRiskUnowned);
            if (NextGates.Count == 0 || NextGates.Any(g => string.IsNullOrWhiteSpace(g.GateId))) findings.Add(SocialMmoMilestoneDiagnosticCode.SocialMmoNextGateMissing);
            if (AlphaReady) findings.Add(SocialMmoMilestoneDiagnosticCode.SocialMmoAlphaReadyClaimForbidden);
            if (OwnerMap == null || !OwnerMap.ServerOwnerPresent) findings.Add(SocialMmoMilestoneDiagnosticCode.SocialMmoServerOwnerMissing);
            return new SocialMmoMilestoneDiagnostics(findings);
        }
    }

    public enum SocialMmoClosureVerdict { ReadyForArchitectValidation, ReadyWithWarnings, NeedsPlannerRevision, BlockedByEvidenceGap, BlockedByServerReadinessGap, BlockedByDemoHonestyGap, BlockedByQaRiskGap, BlockedByBee351Premature }
    public enum SocialMmoAlphaDirectionClosureDiagnosticCode { SocialMmoClosureInputMissing, SocialMmoEvidenceGapOpen, SocialMmoServerReadinessGapOpen, SocialMmoDemoHonestyGapOpen, SocialMmoAlphaReadyForbidden, Bee351Premature }

    public sealed class SocialMmoClosureInputSet
    {
        public SocialMmoClosureInputSet(SocialMmoPillarEvidenceMatrix pillarEvidenceMatrix, AllianceCooperationDemoReadModel cooperationDemoReadModel, ArmyReadinessRiskRegister armyRiskRegister, FairPvpScenarioCatalog pvpScenarioCatalog, SocialServerEscalationBundle serverEscalationBundle, PlayerRetentionAfterConflictProjection retentionAfterConflict, AllianceWarCoordinationReadinessMatrix warCoordinationReadiness, SocialMmoDemoAcceptanceSnapshot demoAcceptanceSnapshot, SocialMmoMilestoneProjection milestoneProjection)
        {
            PillarEvidenceMatrix = pillarEvidenceMatrix;
            CooperationDemoReadModel = cooperationDemoReadModel;
            ArmyRiskRegister = armyRiskRegister;
            PvpScenarioCatalog = pvpScenarioCatalog;
            ServerEscalationBundle = serverEscalationBundle;
            RetentionAfterConflict = retentionAfterConflict;
            WarCoordinationReadiness = warCoordinationReadiness;
            DemoAcceptanceSnapshot = demoAcceptanceSnapshot;
            MilestoneProjection = milestoneProjection;
        }

        public SocialMmoPillarEvidenceMatrix PillarEvidenceMatrix { get; }
        public AllianceCooperationDemoReadModel CooperationDemoReadModel { get; }
        public ArmyReadinessRiskRegister ArmyRiskRegister { get; }
        public FairPvpScenarioCatalog PvpScenarioCatalog { get; }
        public SocialServerEscalationBundle ServerEscalationBundle { get; }
        public PlayerRetentionAfterConflictProjection RetentionAfterConflict { get; }
        public AllianceWarCoordinationReadinessMatrix WarCoordinationReadiness { get; }
        public SocialMmoDemoAcceptanceSnapshot DemoAcceptanceSnapshot { get; }
        public SocialMmoMilestoneProjection MilestoneProjection { get; }

        public bool HasMissingInput()
        {
            return PillarEvidenceMatrix == null || CooperationDemoReadModel == null || ArmyRiskRegister == null || PvpScenarioCatalog == null || ServerEscalationBundle == null || RetentionAfterConflict == null || WarCoordinationReadiness == null || DemoAcceptanceSnapshot == null || MilestoneProjection == null;
        }
    }

    public sealed class SocialMmoClosureCoverage
    {
        public SocialMmoClosureCoverage(bool evidenceGapOpen, bool demoHonestyGapOpen) { EvidenceGapOpen = evidenceGapOpen; DemoHonestyGapOpen = demoHonestyGapOpen; }
        public bool EvidenceGapOpen { get; }
        public bool DemoHonestyGapOpen { get; }
    }

    public sealed class SocialMmoClosureRiskRegister
    {
        public SocialMmoClosureRiskRegister(bool qaRiskGapOpen, bool serverReadinessGapOpen) { QaRiskGapOpen = qaRiskGapOpen; ServerReadinessGapOpen = serverReadinessGapOpen; }
        public bool QaRiskGapOpen { get; }
        public bool ServerReadinessGapOpen { get; }
    }

    public sealed class SocialMmoClosureOwnerMap
    {
        public SocialMmoClosureOwnerMap(bool qaOwnerPresent, bool serverOwnerPresent, bool demoOwnerPresent) { QaOwnerPresent = qaOwnerPresent; ServerOwnerPresent = serverOwnerPresent; DemoOwnerPresent = demoOwnerPresent; }
        public bool QaOwnerPresent { get; }
        public bool ServerOwnerPresent { get; }
        public bool DemoOwnerPresent { get; }
    }

    public sealed class Bee351BlockerStatus
    {
        public Bee351BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; }
        public bool PrematureAttempt { get; }
        public string Message { get; }
    }

    public sealed class SocialMmoAlphaDirectionClosureDiagnostics
    {
        public SocialMmoAlphaDirectionClosureDiagnostics(SocialMmoClosureVerdict verdict, IReadOnlyList<SocialMmoAlphaDirectionClosureDiagnosticCode> findings)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<SocialMmoAlphaDirectionClosureDiagnosticCode>();
        }

        public SocialMmoClosureVerdict Verdict { get; }
        public IReadOnlyList<SocialMmoAlphaDirectionClosureDiagnosticCode> Findings { get; }
        public bool Contains(SocialMmoAlphaDirectionClosureDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class SocialMmoAlphaDirectionClosureGate
    {
        public const string Bee351BlockedMessage = "BEE-351 bloquee jusqu'a validation architecte.";

        public SocialMmoAlphaDirectionClosureGate(string gateId, SocialMmoClosureInputSet inputSet, SocialMmoClosureCoverage coverage, SocialMmoClosureRiskRegister riskRegister, SocialMmoClosureOwnerMap ownerMap, bool alphaReady, Bee351BlockerStatus bee351Status)
        {
            GateId = ColonyIntegrationIds.Require(gateId);
            InputSet = inputSet;
            Coverage = coverage;
            RiskRegister = riskRegister;
            OwnerMap = ownerMap;
            AlphaReady = alphaReady;
            Bee351Status = bee351Status;
        }

        public string GateId { get; }
        public SocialMmoClosureInputSet InputSet { get; }
        public SocialMmoClosureCoverage Coverage { get; }
        public SocialMmoClosureRiskRegister RiskRegister { get; }
        public SocialMmoClosureOwnerMap OwnerMap { get; }
        public bool AlphaReady { get; }
        public Bee351BlockerStatus Bee351Status { get; }

        public SocialMmoAlphaDirectionClosureDiagnostics Evaluate()
        {
            var findings = new List<SocialMmoAlphaDirectionClosureDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput()) findings.Add(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoClosureInputMissing);
            if (Coverage == null || Coverage.EvidenceGapOpen) findings.Add(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoEvidenceGapOpen);
            if (RiskRegister == null || RiskRegister.ServerReadinessGapOpen || OwnerMap == null || !OwnerMap.ServerOwnerPresent) findings.Add(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoServerReadinessGapOpen);
            if (Coverage == null || Coverage.DemoHonestyGapOpen || OwnerMap == null || !OwnerMap.DemoOwnerPresent) findings.Add(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoDemoHonestyGapOpen);
            if (AlphaReady) findings.Add(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoAlphaReadyForbidden);
            if (Bee351Status != null && Bee351Status.PrematureAttempt) findings.Add(SocialMmoAlphaDirectionClosureDiagnosticCode.Bee351Premature);
            return new SocialMmoAlphaDirectionClosureDiagnostics(ResolveVerdict(findings), findings);
        }

        private SocialMmoClosureVerdict ResolveVerdict(IReadOnlyList<SocialMmoAlphaDirectionClosureDiagnosticCode> findings)
        {
            if (findings.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.Bee351Premature)) return SocialMmoClosureVerdict.BlockedByBee351Premature;
            if (findings.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoAlphaReadyForbidden)) return SocialMmoClosureVerdict.NeedsPlannerRevision;
            if (findings.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoClosureInputMissing)) return SocialMmoClosureVerdict.BlockedByEvidenceGap;
            if (findings.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoServerReadinessGapOpen)) return SocialMmoClosureVerdict.BlockedByServerReadinessGap;
            if (findings.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoDemoHonestyGapOpen)) return SocialMmoClosureVerdict.BlockedByDemoHonestyGap;
            if (RiskRegister != null && RiskRegister.QaRiskGapOpen) return SocialMmoClosureVerdict.BlockedByQaRiskGap;
            if (findings.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoEvidenceGapOpen)) return SocialMmoClosureVerdict.ReadyWithWarnings;
            return SocialMmoClosureVerdict.ReadyForArchitectValidation;
        }
    }
}
