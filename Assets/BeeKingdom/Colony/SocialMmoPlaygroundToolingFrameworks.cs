using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum SocialMmoReadModelStatus { AvailableProjection, Partial, Missing, ServerBlocked, DemoOnly }
    public enum PlaygroundReadModelDiagnosticCode { PlaygroundReadModelMissing, PlaygroundReadModelOwnerMissing, PlaygroundReadModelSourceMissing, PlaygroundReadModelMutationForbidden, PlaygroundReadModelServerDependencyOpen }

    public sealed class SocialMmoReadModelOwner { public SocialMmoReadModelOwner(string ownerId) { OwnerId = ownerId ?? string.Empty; } public string OwnerId { get; } }
    public sealed class SocialMmoReadModelGap { public SocialMmoReadModelGap(string gapId, bool open) { GapId = gapId ?? string.Empty; Open = open; } public string GapId { get; } public bool Open { get; } }
    public sealed class SocialMmoReadModelLimit { public SocialMmoReadModelLimit(string limitId, bool mutationRequested) { LimitId = limitId ?? string.Empty; MutationRequested = mutationRequested; } public string LimitId { get; } public bool MutationRequested { get; } }
    public sealed class SocialMmoReadModelServerDependency { public SocialMmoReadModelServerDependency(string topicId, bool open) { TopicId = topicId ?? string.Empty; Open = open; } public string TopicId { get; } public bool Open { get; } }

    public sealed class SocialMmoPlaygroundReadModelEntry
    {
        public SocialMmoPlaygroundReadModelEntry(string readModelId, string sourceBee, IReadOnlyList<SocialMmoProductPillar> productPillars, SocialMmoReadModelOwner owner, SocialMmoReadModelStatus status, IReadOnlyList<SocialMmoReadModelGap> gaps, IReadOnlyList<SocialMmoReadModelLimit> limits, SocialMmoReadModelServerDependency serverDependency)
        {
            ReadModelId = readModelId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            ProductPillars = productPillars ?? Array.Empty<SocialMmoProductPillar>();
            Owner = owner;
            Status = status;
            Gaps = gaps ?? Array.Empty<SocialMmoReadModelGap>();
            Limits = limits ?? Array.Empty<SocialMmoReadModelLimit>();
            ServerDependency = serverDependency;
        }

        public string ReadModelId { get; }
        public string SourceBee { get; }
        public IReadOnlyList<SocialMmoProductPillar> ProductPillars { get; }
        public SocialMmoReadModelOwner Owner { get; }
        public SocialMmoReadModelStatus Status { get; }
        public IReadOnlyList<SocialMmoReadModelGap> Gaps { get; }
        public IReadOnlyList<SocialMmoReadModelLimit> Limits { get; }
        public SocialMmoReadModelServerDependency ServerDependency { get; }
    }

    public sealed class SocialMmoPlaygroundReadModelInventory
    {
        public SocialMmoPlaygroundReadModelInventory(string inventoryId, IReadOnlyList<SocialMmoPlaygroundReadModelEntry> entries) { InventoryId = ColonyIntegrationIds.Require(inventoryId); Entries = entries ?? Array.Empty<SocialMmoPlaygroundReadModelEntry>(); }
        public string InventoryId { get; }
        public IReadOnlyList<SocialMmoPlaygroundReadModelEntry> Entries { get; }
        public PlaygroundReadModelDiagnostics Evaluate()
        {
            var findings = new List<PlaygroundReadModelDiagnosticCode>();
            if (Entries.Count == 0 || Entries.Any(e => string.IsNullOrWhiteSpace(e.ReadModelId))) findings.Add(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelMissing);
            if (Entries.Any(e => e.Owner == null || string.IsNullOrWhiteSpace(e.Owner.OwnerId))) findings.Add(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelOwnerMissing);
            if (Entries.Any(e => string.IsNullOrWhiteSpace(e.SourceBee))) findings.Add(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelSourceMissing);
            if (Entries.Any(e => e.Limits.Any(l => l.MutationRequested))) findings.Add(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelMutationForbidden);
            if (Entries.Any(e => e.ServerDependency == null || e.ServerDependency.Open || e.Status == SocialMmoReadModelStatus.ServerBlocked)) findings.Add(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelServerDependencyOpen);
            return new PlaygroundReadModelDiagnostics(findings);
        }
    }

    public sealed class PlaygroundReadModelDiagnostics { public PlaygroundReadModelDiagnostics(IReadOnlyList<PlaygroundReadModelDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlaygroundReadModelDiagnosticCode>(); } public IReadOnlyList<PlaygroundReadModelDiagnosticCode> Findings { get; } public bool Contains(PlaygroundReadModelDiagnosticCode code) { return Findings.Contains(code); } }

    public enum AllianceCooperationVisualStatus { Projected, Gap, Blocked, ServerRequired }
    public enum AllianceCooperationVisualizationDiagnosticCode { AllianceCooperationBindingMissing, AllianceCooperationVisualSourceMissing, AllianceCooperationUiFinalForbidden, AllianceCooperationGameplayMutationForbidden, AllianceCooperationRewardClaimForbidden }

    public abstract class AllianceCooperationVisualBindingBase
    {
        protected AllianceCooperationVisualBindingBase(string bindingId, string sourceId, AllianceCooperationVisualStatus status, bool mutationRequested, bool rewardClaimed) { BindingId = bindingId ?? string.Empty; SourceId = sourceId ?? string.Empty; Status = status; MutationRequested = mutationRequested; RewardClaimed = rewardClaimed; }
        public string BindingId { get; }
        public string SourceId { get; }
        public AllianceCooperationVisualStatus Status { get; }
        public bool MutationRequested { get; }
        public bool RewardClaimed { get; }
    }

    public sealed class AllianceObjectiveVisualBinding : AllianceCooperationVisualBindingBase { public AllianceObjectiveVisualBinding(string bindingId, string sourceId, AllianceCooperationVisualStatus status, bool mutationRequested = false, bool rewardClaimed = false) : base(bindingId, sourceId, status, mutationRequested, rewardClaimed) { } }
    public sealed class ContributionVisualBinding : AllianceCooperationVisualBindingBase { public ContributionVisualBinding(string bindingId, string sourceId, AllianceCooperationVisualStatus status, bool mutationRequested = false, bool rewardClaimed = false) : base(bindingId, sourceId, status, mutationRequested, rewardClaimed) { } }
    public sealed class MissionVisualBinding : AllianceCooperationVisualBindingBase { public MissionVisualBinding(string bindingId, string sourceId, AllianceCooperationVisualStatus status, bool mutationRequested = false, bool rewardClaimed = false) : base(bindingId, sourceId, status, mutationRequested, rewardClaimed) { } }
    public sealed class HelpRequestVisualBinding : AllianceCooperationVisualBindingBase { public HelpRequestVisualBinding(string bindingId, string sourceId, AllianceCooperationVisualStatus status, bool mutationRequested = false, bool rewardClaimed = false) : base(bindingId, sourceId, status, mutationRequested, rewardClaimed) { } }
    public sealed class AllianceCooperationVisualLimit { public AllianceCooperationVisualLimit(string limitId, bool uiFinalClaimed) { LimitId = limitId ?? string.Empty; UiFinalClaimed = uiFinalClaimed; } public string LimitId { get; } public bool UiFinalClaimed { get; } }

    public sealed class AllianceCooperationVisualizationBinding
    {
        public AllianceCooperationVisualizationBinding(string bindingId, string sourceReadModelId, IReadOnlyList<AllianceObjectiveVisualBinding> objectiveBindings, IReadOnlyList<ContributionVisualBinding> contributionBindings, IReadOnlyList<MissionVisualBinding> missionBindings, IReadOnlyList<HelpRequestVisualBinding> helpRequestBindings, IReadOnlyList<AllianceCooperationVisualLimit> visualLimits)
        {
            BindingId = ColonyIntegrationIds.Require(bindingId);
            SourceReadModelId = sourceReadModelId ?? string.Empty;
            ObjectiveBindings = objectiveBindings ?? Array.Empty<AllianceObjectiveVisualBinding>();
            ContributionBindings = contributionBindings ?? Array.Empty<ContributionVisualBinding>();
            MissionBindings = missionBindings ?? Array.Empty<MissionVisualBinding>();
            HelpRequestBindings = helpRequestBindings ?? Array.Empty<HelpRequestVisualBinding>();
            VisualLimits = visualLimits ?? Array.Empty<AllianceCooperationVisualLimit>();
        }

        public string BindingId { get; }
        public string SourceReadModelId { get; }
        public IReadOnlyList<AllianceObjectiveVisualBinding> ObjectiveBindings { get; }
        public IReadOnlyList<ContributionVisualBinding> ContributionBindings { get; }
        public IReadOnlyList<MissionVisualBinding> MissionBindings { get; }
        public IReadOnlyList<HelpRequestVisualBinding> HelpRequestBindings { get; }
        public IReadOnlyList<AllianceCooperationVisualLimit> VisualLimits { get; }
        public AllianceCooperationVisualizationDiagnostics Evaluate()
        {
            var all = ObjectiveBindings.Cast<AllianceCooperationVisualBindingBase>().Concat(ContributionBindings).Concat(MissionBindings).Concat(HelpRequestBindings).ToArray();
            var findings = new List<AllianceCooperationVisualizationDiagnosticCode>();
            if (all.Length == 0) findings.Add(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationBindingMissing);
            if (string.IsNullOrWhiteSpace(SourceReadModelId) || all.Any(b => string.IsNullOrWhiteSpace(b.SourceId))) findings.Add(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationVisualSourceMissing);
            if (VisualLimits.Any(l => l.UiFinalClaimed)) findings.Add(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationUiFinalForbidden);
            if (all.Any(b => b.MutationRequested)) findings.Add(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationGameplayMutationForbidden);
            if (all.Any(b => b.RewardClaimed)) findings.Add(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationRewardClaimForbidden);
            return new AllianceCooperationVisualizationDiagnostics(findings);
        }
    }

    public sealed class AllianceCooperationVisualizationDiagnostics { public AllianceCooperationVisualizationDiagnostics(IReadOnlyList<AllianceCooperationVisualizationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceCooperationVisualizationDiagnosticCode>(); } public IReadOnlyList<AllianceCooperationVisualizationDiagnosticCode> Findings { get; } public bool Contains(AllianceCooperationVisualizationDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ArmyReadinessVisualLevel { Low, Partial, High, Blocked }
    public enum ArmyReadinessVisualizationDiagnosticCode { ArmyVisualizationInputMissing, ArmyOfficialStatForbidden, ArmyCombatPowerVisualizationForbidden, ArmyRiskWarningMissing, ArmyServerDependencyHidden }
    public sealed class ArmyReadinessVisualSignal { public ArmyReadinessVisualSignal(string signalId, ArmyReadinessVisualLevel level, bool officialStatClaimed) { SignalId = signalId ?? string.Empty; Level = level; OfficialStatClaimed = officialStatClaimed; } public string SignalId { get; } public ArmyReadinessVisualLevel Level { get; } public bool OfficialStatClaimed { get; } }
    public sealed class ArmyCompositionVisualSummary { public ArmyCompositionVisualSummary(string summaryId, bool combatPowerClaimed) { SummaryId = summaryId ?? string.Empty; CombatPowerClaimed = combatPowerClaimed; } public string SummaryId { get; } public bool CombatPowerClaimed { get; } }
    public sealed class ArmyRiskVisualWarning { public ArmyRiskVisualWarning(string warningId) { WarningId = warningId ?? string.Empty; } public string WarningId { get; } }
    public sealed class ArmyServerDependencyVisualMarker { public ArmyServerDependencyVisualMarker(string topicId, bool visible) { TopicId = topicId ?? string.Empty; Visible = visible; } public string TopicId { get; } public bool Visible { get; } }
    public sealed class ArmyVisualizationLimit { public ArmyVisualizationLimit(string limitId) { LimitId = limitId ?? string.Empty; } public string LimitId { get; } }

    public sealed class ArmyReadinessVisualizationContract
    {
        public ArmyReadinessVisualizationContract(string visualizationId, IReadOnlyList<string> sourceReadModels, IReadOnlyList<ArmyReadinessVisualSignal> readinessSignals, ArmyCompositionVisualSummary compositionSummary, IReadOnlyList<ArmyRiskVisualWarning> riskWarnings, IReadOnlyList<ArmyServerDependencyVisualMarker> serverMarkers, IReadOnlyList<ArmyVisualizationLimit> visualLimits)
        {
            VisualizationId = ColonyIntegrationIds.Require(visualizationId);
            SourceReadModels = sourceReadModels ?? Array.Empty<string>();
            ReadinessSignals = readinessSignals ?? Array.Empty<ArmyReadinessVisualSignal>();
            CompositionSummary = compositionSummary;
            RiskWarnings = riskWarnings ?? Array.Empty<ArmyRiskVisualWarning>();
            ServerMarkers = serverMarkers ?? Array.Empty<ArmyServerDependencyVisualMarker>();
            VisualLimits = visualLimits ?? Array.Empty<ArmyVisualizationLimit>();
        }

        public string VisualizationId { get; }
        public IReadOnlyList<string> SourceReadModels { get; }
        public IReadOnlyList<ArmyReadinessVisualSignal> ReadinessSignals { get; }
        public ArmyCompositionVisualSummary CompositionSummary { get; }
        public IReadOnlyList<ArmyRiskVisualWarning> RiskWarnings { get; }
        public IReadOnlyList<ArmyServerDependencyVisualMarker> ServerMarkers { get; }
        public IReadOnlyList<ArmyVisualizationLimit> VisualLimits { get; }
        public ArmyReadinessVisualizationDiagnostics Evaluate()
        {
            var findings = new List<ArmyReadinessVisualizationDiagnosticCode>();
            if (SourceReadModels.Count == 0 || ReadinessSignals.Count == 0) findings.Add(ArmyReadinessVisualizationDiagnosticCode.ArmyVisualizationInputMissing);
            if (ReadinessSignals.Any(s => s.OfficialStatClaimed)) findings.Add(ArmyReadinessVisualizationDiagnosticCode.ArmyOfficialStatForbidden);
            if (CompositionSummary != null && CompositionSummary.CombatPowerClaimed) findings.Add(ArmyReadinessVisualizationDiagnosticCode.ArmyCombatPowerVisualizationForbidden);
            if (RiskWarnings.Count == 0) findings.Add(ArmyReadinessVisualizationDiagnosticCode.ArmyRiskWarningMissing);
            if (ServerMarkers.Count == 0 || ServerMarkers.Any(m => !m.Visible)) findings.Add(ArmyReadinessVisualizationDiagnosticCode.ArmyServerDependencyHidden);
            return new ArmyReadinessVisualizationDiagnostics(findings);
        }
    }

    public sealed class ArmyReadinessVisualizationDiagnostics { public ArmyReadinessVisualizationDiagnostics(IReadOnlyList<ArmyReadinessVisualizationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyReadinessVisualizationDiagnosticCode>(); } public IReadOnlyList<ArmyReadinessVisualizationDiagnosticCode> Findings { get; } public bool Contains(ArmyReadinessVisualizationDiagnosticCode code) { return Findings.Contains(code); } }

    public enum PvPFairnessDebugDiagnosticCode { PvPFairnessDebugInputMissing, PvPFairnessThresholdFinalClaimForbidden, PvPEnforcementRuntimeForbidden, PvPMatchmakingDebugClaimForbidden, PvPHarassmentWarningMissing }
    public sealed class PvPFairnessDebugScenarioRow { public PvPFairnessDebugScenarioRow(string scenarioId) { ScenarioId = scenarioId ?? string.Empty; } public string ScenarioId { get; } }
    public sealed class PvPFairnessThresholdProjection { public PvPFairnessThresholdProjection(string thresholdId, bool nonFinalBalance) { ThresholdId = thresholdId ?? string.Empty; NonFinalBalance = nonFinalBalance; } public string ThresholdId { get; } public bool NonFinalBalance { get; } }
    public sealed class PvPRecoveryDebugSignal { public PvPRecoveryDebugSignal(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class PvPHarassmentWarning { public PvPHarassmentWarning(string warningId) { WarningId = warningId ?? string.Empty; } public string WarningId { get; } }
    public sealed class PvPFairnessDebugLimit { public PvPFairnessDebugLimit(string limitId, bool enforcementRequested, bool matchmakingClaimed) { LimitId = limitId ?? string.Empty; EnforcementRequested = enforcementRequested; MatchmakingClaimed = matchmakingClaimed; } public string LimitId { get; } public bool EnforcementRequested { get; } public bool MatchmakingClaimed { get; } }

    public sealed class PvPFairnessDebugPanelContract
    {
        public PvPFairnessDebugPanelContract(string panelId, IReadOnlyList<PvPFairnessDebugScenarioRow> scenarioRows, IReadOnlyList<PvPFairnessThresholdProjection> thresholdProjections, IReadOnlyList<PvPRecoveryDebugSignal> recoverySignals, IReadOnlyList<PvPHarassmentWarning> harassmentWarnings, IReadOnlyList<PvPFairnessDebugLimit> debugLimits)
        {
            PanelId = ColonyIntegrationIds.Require(panelId);
            ScenarioRows = scenarioRows ?? Array.Empty<PvPFairnessDebugScenarioRow>();
            ThresholdProjections = thresholdProjections ?? Array.Empty<PvPFairnessThresholdProjection>();
            RecoverySignals = recoverySignals ?? Array.Empty<PvPRecoveryDebugSignal>();
            HarassmentWarnings = harassmentWarnings ?? Array.Empty<PvPHarassmentWarning>();
            DebugLimits = debugLimits ?? Array.Empty<PvPFairnessDebugLimit>();
        }

        public string PanelId { get; }
        public IReadOnlyList<PvPFairnessDebugScenarioRow> ScenarioRows { get; }
        public IReadOnlyList<PvPFairnessThresholdProjection> ThresholdProjections { get; }
        public IReadOnlyList<PvPRecoveryDebugSignal> RecoverySignals { get; }
        public IReadOnlyList<PvPHarassmentWarning> HarassmentWarnings { get; }
        public IReadOnlyList<PvPFairnessDebugLimit> DebugLimits { get; }
        public PvPFairnessDebugPanelDiagnostics Evaluate()
        {
            var findings = new List<PvPFairnessDebugDiagnosticCode>();
            if (ScenarioRows.Count == 0 || ThresholdProjections.Count == 0 || RecoverySignals.Count == 0) findings.Add(PvPFairnessDebugDiagnosticCode.PvPFairnessDebugInputMissing);
            if (ThresholdProjections.Any(t => !t.NonFinalBalance)) findings.Add(PvPFairnessDebugDiagnosticCode.PvPFairnessThresholdFinalClaimForbidden);
            if (DebugLimits.Any(l => l.EnforcementRequested)) findings.Add(PvPFairnessDebugDiagnosticCode.PvPEnforcementRuntimeForbidden);
            if (DebugLimits.Any(l => l.MatchmakingClaimed)) findings.Add(PvPFairnessDebugDiagnosticCode.PvPMatchmakingDebugClaimForbidden);
            if (HarassmentWarnings.Count == 0) findings.Add(PvPFairnessDebugDiagnosticCode.PvPHarassmentWarningMissing);
            return new PvPFairnessDebugPanelDiagnostics(findings);
        }
    }

    public sealed class PvPFairnessDebugPanelDiagnostics { public PvPFairnessDebugPanelDiagnostics(IReadOnlyList<PvPFairnessDebugDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PvPFairnessDebugDiagnosticCode>(); } public IReadOnlyList<PvPFairnessDebugDiagnosticCode> Findings { get; } public bool Contains(PvPFairnessDebugDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialServerHandoffPriority { Critical, High, Medium, Low, Missing }
    public enum SocialServerHandoffScanStatus { Missing, RequiresScan, ScannedNoServerCreated, BlockedUntilServerSpec }
    public enum SocialServerHandoffDiagnosticCode { SocialServerHandoffItemMissing, SocialServerPriorityMissing, SocialServerOwnerMissing, SocialServerScanStatusMissing, Server018CreationForbidden, SocialServerHandoffRuntimeForbidden }
    public sealed class SocialServerPriorityReason { public SocialServerPriorityReason(string reasonId) { ReasonId = reasonId ?? string.Empty; } public string ReasonId { get; } }
    public sealed class SocialServerHandoffBlocker { public SocialServerHandoffBlocker(string blockerId) { BlockerId = blockerId ?? string.Empty; } public string BlockerId { get; } }
    public sealed class SocialServerHandoffLimit { public SocialServerHandoffLimit(string limitId, bool server018Requested, bool runtimeRequested) { LimitId = limitId ?? string.Empty; Server018Requested = server018Requested; RuntimeRequested = runtimeRequested; } public string LimitId { get; } public bool Server018Requested { get; } public bool RuntimeRequested { get; } }
    public sealed class SocialServerHandoffQueueItem
    {
        public SocialServerHandoffQueueItem(string itemId, string sourceBee, string category, SocialServerHandoffPriority priority, SocialServerPriorityReason priorityReason, string ownerHint, SocialServerHandoffScanStatus scanStatus, IReadOnlyList<SocialServerHandoffBlocker> blockers)
        {
            ItemId = itemId ?? string.Empty; SourceBee = sourceBee ?? string.Empty; Category = category ?? string.Empty; Priority = priority; PriorityReason = priorityReason; OwnerHint = ownerHint ?? string.Empty; ScanStatus = scanStatus; Blockers = blockers ?? Array.Empty<SocialServerHandoffBlocker>();
        }
        public string ItemId { get; } public string SourceBee { get; } public string Category { get; } public SocialServerHandoffPriority Priority { get; } public SocialServerPriorityReason PriorityReason { get; } public string OwnerHint { get; } public SocialServerHandoffScanStatus ScanStatus { get; } public IReadOnlyList<SocialServerHandoffBlocker> Blockers { get; }
    }

    public sealed class SocialServerHandoffQueue
    {
        public SocialServerHandoffQueue(string queueId, IReadOnlyList<SocialServerHandoffQueueItem> items, IReadOnlyList<SocialServerHandoffLimit> limits) { QueueId = ColonyIntegrationIds.Require(queueId); Items = items ?? Array.Empty<SocialServerHandoffQueueItem>(); Limits = limits ?? Array.Empty<SocialServerHandoffLimit>(); }
        public string QueueId { get; }
        public IReadOnlyList<SocialServerHandoffQueueItem> Items { get; }
        public IReadOnlyList<SocialServerHandoffLimit> Limits { get; }
        public SocialServerHandoffQueueDiagnostics Evaluate()
        {
            var findings = new List<SocialServerHandoffDiagnosticCode>();
            if (Items.Count == 0) findings.Add(SocialServerHandoffDiagnosticCode.SocialServerHandoffItemMissing);
            if (Items.Any(i => i.Priority == SocialServerHandoffPriority.Missing || i.PriorityReason == null)) findings.Add(SocialServerHandoffDiagnosticCode.SocialServerPriorityMissing);
            if (Items.Any(i => string.IsNullOrWhiteSpace(i.OwnerHint))) findings.Add(SocialServerHandoffDiagnosticCode.SocialServerOwnerMissing);
            if (Items.Any(i => i.ScanStatus == SocialServerHandoffScanStatus.Missing)) findings.Add(SocialServerHandoffDiagnosticCode.SocialServerScanStatusMissing);
            if (Limits.Any(l => l.Server018Requested)) findings.Add(SocialServerHandoffDiagnosticCode.Server018CreationForbidden);
            if (Limits.Any(l => l.RuntimeRequested)) findings.Add(SocialServerHandoffDiagnosticCode.SocialServerHandoffRuntimeForbidden);
            return new SocialServerHandoffQueueDiagnostics(findings);
        }
    }
    public sealed class SocialServerHandoffQueueDiagnostics { public SocialServerHandoffQueueDiagnostics(IReadOnlyList<SocialServerHandoffDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialServerHandoffDiagnosticCode>(); } public IReadOnlyList<SocialServerHandoffDiagnosticCode> Findings { get; } public bool Contains(SocialServerHandoffDiagnosticCode code) { return Findings.Contains(code); } }

    public enum ModerationTriageStatus { NeedsServerModeration }
    public enum ModerationTriageDiagnosticCode { ModerationTriageCaseMissing, ModerationEvidenceExpectationMissing, ModerationPrivacyConstraintMissing, ModerationSanctionForbidden, ModerationOfficialStorageForbidden, ModerationRuntimeToolForbidden }
    public sealed class ModerationEvidenceExpectation { public ModerationEvidenceExpectation(string evidenceId) { EvidenceId = evidenceId ?? string.Empty; } public string EvidenceId { get; } }
    public sealed class ModerationPrivacyConstraint { public ModerationPrivacyConstraint(string constraintId) { ConstraintId = constraintId ?? string.Empty; } public string ConstraintId { get; } }
    public sealed class ModerationTriageRisk { public ModerationTriageRisk(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class ModerationTriageLimit { public ModerationTriageLimit(string limitId, bool sanctionRequested, bool officialStorageRequested, bool runtimeToolRequested) { LimitId = limitId ?? string.Empty; SanctionRequested = sanctionRequested; OfficialStorageRequested = officialStorageRequested; RuntimeToolRequested = runtimeToolRequested; } public string LimitId { get; } public bool SanctionRequested { get; } public bool OfficialStorageRequested { get; } public bool RuntimeToolRequested { get; } }
    public sealed class ModerationAbuseTriageCaseProjection
    {
        public ModerationAbuseTriageCaseProjection(string caseId, string abuseType, string sourceBee, ModerationEvidenceExpectation evidenceExpectation, IReadOnlyList<ModerationPrivacyConstraint> privacyConstraints, IReadOnlyList<ModerationTriageRisk> triageRisks, ModerationTriageStatus status, IReadOnlyList<ModerationTriageLimit> limits)
        {
            CaseId = caseId ?? string.Empty; AbuseType = abuseType ?? string.Empty; SourceBee = sourceBee ?? string.Empty; EvidenceExpectation = evidenceExpectation; PrivacyConstraints = privacyConstraints ?? Array.Empty<ModerationPrivacyConstraint>(); TriageRisks = triageRisks ?? Array.Empty<ModerationTriageRisk>(); Status = status; Limits = limits ?? Array.Empty<ModerationTriageLimit>();
        }
        public string CaseId { get; } public string AbuseType { get; } public string SourceBee { get; } public ModerationEvidenceExpectation EvidenceExpectation { get; } public IReadOnlyList<ModerationPrivacyConstraint> PrivacyConstraints { get; } public IReadOnlyList<ModerationTriageRisk> TriageRisks { get; } public ModerationTriageStatus Status { get; } public IReadOnlyList<ModerationTriageLimit> Limits { get; }
    }
    public sealed class ModerationAbuseTriageToolBoundary
    {
        public ModerationAbuseTriageToolBoundary(string boundaryId, IReadOnlyList<ModerationAbuseTriageCaseProjection> cases) { BoundaryId = ColonyIntegrationIds.Require(boundaryId); Cases = cases ?? Array.Empty<ModerationAbuseTriageCaseProjection>(); }
        public string BoundaryId { get; } public IReadOnlyList<ModerationAbuseTriageCaseProjection> Cases { get; }
        public ModerationAbuseTriageDiagnostics Evaluate()
        {
            var findings = new List<ModerationTriageDiagnosticCode>();
            if (Cases.Count == 0 || Cases.Any(c => string.IsNullOrWhiteSpace(c.CaseId))) findings.Add(ModerationTriageDiagnosticCode.ModerationTriageCaseMissing);
            if (Cases.Any(c => c.EvidenceExpectation == null || string.IsNullOrWhiteSpace(c.EvidenceExpectation.EvidenceId))) findings.Add(ModerationTriageDiagnosticCode.ModerationEvidenceExpectationMissing);
            if (Cases.Any(c => c.PrivacyConstraints.Count == 0)) findings.Add(ModerationTriageDiagnosticCode.ModerationPrivacyConstraintMissing);
            if (Cases.Any(c => c.Limits.Any(l => l.SanctionRequested))) findings.Add(ModerationTriageDiagnosticCode.ModerationSanctionForbidden);
            if (Cases.Any(c => c.Limits.Any(l => l.OfficialStorageRequested))) findings.Add(ModerationTriageDiagnosticCode.ModerationOfficialStorageForbidden);
            if (Cases.Any(c => c.Limits.Any(l => l.RuntimeToolRequested))) findings.Add(ModerationTriageDiagnosticCode.ModerationRuntimeToolForbidden);
            return new ModerationAbuseTriageDiagnostics(findings);
        }
    }
    public sealed class ModerationAbuseTriageDiagnostics { public ModerationAbuseTriageDiagnostics(IReadOnlyList<ModerationTriageDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ModerationTriageDiagnosticCode>(); } public IReadOnlyList<ModerationTriageDiagnosticCode> Findings { get; } public bool Contains(ModerationTriageDiagnosticCode code) { return Findings.Contains(code); } }

    public enum WarFixtureDiagnosticCode { WarFixtureMissing, WarFixturePrerequisiteMissing, WarFixtureRiskMissing, WarFixtureRuntimeExecutionForbidden, WarFixtureRewardForbidden, WarFixtureServerAuthorityRequired }
    public sealed class WarFixturePrerequisite { public WarFixturePrerequisite(string prerequisiteId, bool missing) { PrerequisiteId = prerequisiteId ?? string.Empty; Missing = missing; } public string PrerequisiteId { get; } public bool Missing { get; } }
    public sealed class WarFixtureRisk { public WarFixtureRisk(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class WarFixtureDemoExpectation { public WarFixtureDemoExpectation(string expectationId) { ExpectationId = expectationId ?? string.Empty; } public string ExpectationId { get; } }
    public sealed class WarFixtureRuntimeLimit { public WarFixtureRuntimeLimit(string limitId, bool runtimeExecutionRequested, bool rewardRequested) { LimitId = limitId ?? string.Empty; RuntimeExecutionRequested = runtimeExecutionRequested; RewardRequested = rewardRequested; } public string LimitId { get; } public bool RuntimeExecutionRequested { get; } public bool RewardRequested { get; } }
    public sealed class AllianceWarScenarioFixture
    {
        public AllianceWarScenarioFixture(string fixtureId, string scenarioType, IReadOnlyList<string> sourceBees, IReadOnlyList<WarFixturePrerequisite> prerequisites, IReadOnlyList<WarFixtureRisk> risks, WarFixtureDemoExpectation demoExpectation, IReadOnlyList<WarFixtureRuntimeLimit> runtimeLimits, IReadOnlyList<string> serverAuthorityTopics)
        {
            FixtureId = fixtureId ?? string.Empty; ScenarioType = scenarioType ?? string.Empty; SourceBees = sourceBees ?? Array.Empty<string>(); Prerequisites = prerequisites ?? Array.Empty<WarFixturePrerequisite>(); Risks = risks ?? Array.Empty<WarFixtureRisk>(); DemoExpectation = demoExpectation; RuntimeLimits = runtimeLimits ?? Array.Empty<WarFixtureRuntimeLimit>(); ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
        }
        public string FixtureId { get; } public string ScenarioType { get; } public IReadOnlyList<string> SourceBees { get; } public IReadOnlyList<WarFixturePrerequisite> Prerequisites { get; } public IReadOnlyList<WarFixtureRisk> Risks { get; } public WarFixtureDemoExpectation DemoExpectation { get; } public IReadOnlyList<WarFixtureRuntimeLimit> RuntimeLimits { get; } public IReadOnlyList<string> ServerAuthorityTopics { get; }
    }
    public sealed class AllianceWarScenarioFixtureCatalog
    {
        public AllianceWarScenarioFixtureCatalog(string catalogId, IReadOnlyList<AllianceWarScenarioFixture> fixtures) { CatalogId = ColonyIntegrationIds.Require(catalogId); Fixtures = fixtures ?? Array.Empty<AllianceWarScenarioFixture>(); }
        public string CatalogId { get; } public IReadOnlyList<AllianceWarScenarioFixture> Fixtures { get; }
        public AllianceWarScenarioFixtureDiagnostics Evaluate()
        {
            var findings = new List<WarFixtureDiagnosticCode>();
            if (Fixtures.Count == 0 || Fixtures.Any(f => string.IsNullOrWhiteSpace(f.FixtureId))) findings.Add(WarFixtureDiagnosticCode.WarFixtureMissing);
            if (Fixtures.Any(f => f.Prerequisites.Count == 0 || f.Prerequisites.Any(p => p.Missing))) findings.Add(WarFixtureDiagnosticCode.WarFixturePrerequisiteMissing);
            if (Fixtures.Any(f => f.Risks.Count == 0)) findings.Add(WarFixtureDiagnosticCode.WarFixtureRiskMissing);
            if (Fixtures.Any(f => f.RuntimeLimits.Any(l => l.RuntimeExecutionRequested))) findings.Add(WarFixtureDiagnosticCode.WarFixtureRuntimeExecutionForbidden);
            if (Fixtures.Any(f => f.RuntimeLimits.Any(l => l.RewardRequested))) findings.Add(WarFixtureDiagnosticCode.WarFixtureRewardForbidden);
            if (Fixtures.Any(f => f.ServerAuthorityTopics.Count > 0)) findings.Add(WarFixtureDiagnosticCode.WarFixtureServerAuthorityRequired);
            return new AllianceWarScenarioFixtureDiagnostics(findings);
        }
    }
    public sealed class AllianceWarScenarioFixtureDiagnostics { public AllianceWarScenarioFixtureDiagnostics(IReadOnlyList<WarFixtureDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WarFixtureDiagnosticCode>(); } public IReadOnlyList<WarFixtureDiagnosticCode> Findings { get; } public bool Contains(WarFixtureDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoEvidenceLinkType { SourceBee, WorkerReport, QaRisk, ServerStatus, DemoSurface, Limit }
    public enum EvidenceDrilldownDiagnosticCode { EvidenceDrilldownNodeMissing, EvidenceDrilldownOwnerMissing, EvidenceContradictionOpen, EvidenceAutoCorrectionForbidden, EvidenceLocalTruthForbidden }
    public sealed class SocialMmoEvidenceOwner { public SocialMmoEvidenceOwner(string ownerId) { OwnerId = ownerId ?? string.Empty; } public string OwnerId { get; } }
    public sealed class SocialMmoEvidenceLink { public SocialMmoEvidenceLink(SocialMmoEvidenceLinkType linkType, string targetId) { LinkType = linkType; TargetId = targetId ?? string.Empty; } public SocialMmoEvidenceLinkType LinkType { get; } public string TargetId { get; } }
    public sealed class SocialMmoEvidenceContradiction { public SocialMmoEvidenceContradiction(string contradictionId, bool open) { ContradictionId = contradictionId ?? string.Empty; Open = open; } public string ContradictionId { get; } public bool Open { get; } }
    public sealed class SocialMmoEvidenceDrilldownLimit { public SocialMmoEvidenceDrilldownLimit(string limitId, bool autoCorrectionRequested, bool localTruthClaimed) { LimitId = limitId ?? string.Empty; AutoCorrectionRequested = autoCorrectionRequested; LocalTruthClaimed = localTruthClaimed; } public string LimitId { get; } public bool AutoCorrectionRequested { get; } public bool LocalTruthClaimed { get; } }
    public sealed class SocialMmoEvidenceNode
    {
        public SocialMmoEvidenceNode(string nodeId, string evidenceType, string sourceReference, SocialMmoEvidenceOwner owner, IReadOnlyList<SocialMmoEvidenceLink> links, IReadOnlyList<SocialMmoEvidenceContradiction> contradictions, IReadOnlyList<SocialMmoEvidenceDrilldownLimit> limits)
        {
            NodeId = nodeId ?? string.Empty; EvidenceType = evidenceType ?? string.Empty; SourceReference = sourceReference ?? string.Empty; Owner = owner; Links = links ?? Array.Empty<SocialMmoEvidenceLink>(); Contradictions = contradictions ?? Array.Empty<SocialMmoEvidenceContradiction>(); Limits = limits ?? Array.Empty<SocialMmoEvidenceDrilldownLimit>();
        }
        public string NodeId { get; } public string EvidenceType { get; } public string SourceReference { get; } public SocialMmoEvidenceOwner Owner { get; } public IReadOnlyList<SocialMmoEvidenceLink> Links { get; } public IReadOnlyList<SocialMmoEvidenceContradiction> Contradictions { get; } public IReadOnlyList<SocialMmoEvidenceDrilldownLimit> Limits { get; }
    }
    public sealed class SocialMmoEvidenceDrilldown
    {
        public SocialMmoEvidenceDrilldown(string drilldownId, IReadOnlyList<SocialMmoEvidenceNode> nodes) { DrilldownId = ColonyIntegrationIds.Require(drilldownId); Nodes = nodes ?? Array.Empty<SocialMmoEvidenceNode>(); }
        public string DrilldownId { get; } public IReadOnlyList<SocialMmoEvidenceNode> Nodes { get; }
        public SocialMmoEvidenceDrilldownDiagnostics Evaluate()
        {
            var findings = new List<EvidenceDrilldownDiagnosticCode>();
            if (Nodes.Count == 0 || Nodes.Any(n => string.IsNullOrWhiteSpace(n.NodeId))) findings.Add(EvidenceDrilldownDiagnosticCode.EvidenceDrilldownNodeMissing);
            if (Nodes.Any(n => n.Owner == null || string.IsNullOrWhiteSpace(n.Owner.OwnerId))) findings.Add(EvidenceDrilldownDiagnosticCode.EvidenceDrilldownOwnerMissing);
            if (Nodes.Any(n => n.Contradictions.Any(c => c.Open))) findings.Add(EvidenceDrilldownDiagnosticCode.EvidenceContradictionOpen);
            if (Nodes.Any(n => n.Limits.Any(l => l.AutoCorrectionRequested))) findings.Add(EvidenceDrilldownDiagnosticCode.EvidenceAutoCorrectionForbidden);
            if (Nodes.Any(n => n.Limits.Any(l => l.LocalTruthClaimed))) findings.Add(EvidenceDrilldownDiagnosticCode.EvidenceLocalTruthForbidden);
            return new SocialMmoEvidenceDrilldownDiagnostics(findings);
        }
    }
    public sealed class SocialMmoEvidenceDrilldownDiagnostics { public SocialMmoEvidenceDrilldownDiagnostics(IReadOnlyList<EvidenceDrilldownDiagnosticCode> findings) { Findings = findings ?? Array.Empty<EvidenceDrilldownDiagnosticCode>(); } public IReadOnlyList<EvidenceDrilldownDiagnosticCode> Findings { get; } public bool Contains(EvidenceDrilldownDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoToolingVerdict { ReadyForClosureGate, ReadyWithWarnings, NeedsPlannerRevision, BlockedByHiddenGap, BlockedByLocalTruthRisk, BlockedByRuntimeClaim, BlockedByServerBypass }
    public enum ToolingRiskDiagnosticCode { ToolingInputMissing, ToolingHiddenGapDetected, ToolingLocalTruthDetected, ToolingRuntimeClaimDetected, ToolingServerBypassDetected, ToolingClosureBlocked }
    public sealed class SocialMmoToolingRiskInput { public SocialMmoToolingRiskInput(string toolId, bool present) { ToolId = toolId ?? string.Empty; Present = present; } public string ToolId { get; } public bool Present { get; } }
    public sealed class SocialMmoToolingRisk { public SocialMmoToolingRisk(string riskId, bool localTruth, bool runtimeClaim, bool serverBypass) { RiskId = riskId ?? string.Empty; LocalTruth = localTruth; RuntimeClaim = runtimeClaim; ServerBypass = serverBypass; } public string RiskId { get; } public bool LocalTruth { get; } public bool RuntimeClaim { get; } public bool ServerBypass { get; } }
    public sealed class SocialMmoToolingBlocker { public SocialMmoToolingBlocker(string blockerId, bool hiddenGap) { BlockerId = blockerId ?? string.Empty; HiddenGap = hiddenGap; } public string BlockerId { get; } public bool HiddenGap { get; } }
    public sealed class SocialMmoToolingWarning { public SocialMmoToolingWarning(string warningId) { WarningId = warningId ?? string.Empty; } public string WarningId { get; } }
    public sealed class SocialMmoToolingRiskGate
    {
        public SocialMmoToolingRiskGate(string gateId, IReadOnlyList<SocialMmoToolingRiskInput> inputs, IReadOnlyList<SocialMmoToolingRisk> risks, IReadOnlyList<SocialMmoToolingBlocker> blockers, IReadOnlyList<SocialMmoToolingWarning> warnings)
        {
            GateId = ColonyIntegrationIds.Require(gateId); Inputs = inputs ?? Array.Empty<SocialMmoToolingRiskInput>(); Risks = risks ?? Array.Empty<SocialMmoToolingRisk>(); Blockers = blockers ?? Array.Empty<SocialMmoToolingBlocker>(); Warnings = warnings ?? Array.Empty<SocialMmoToolingWarning>();
        }
        public string GateId { get; } public IReadOnlyList<SocialMmoToolingRiskInput> Inputs { get; } public IReadOnlyList<SocialMmoToolingRisk> Risks { get; } public IReadOnlyList<SocialMmoToolingBlocker> Blockers { get; } public IReadOnlyList<SocialMmoToolingWarning> Warnings { get; }
        public SocialMmoToolingRiskGateDiagnostics Evaluate()
        {
            var findings = new List<ToolingRiskDiagnosticCode>();
            if (Inputs.Count < 8 || Inputs.Any(i => !i.Present)) findings.Add(ToolingRiskDiagnosticCode.ToolingInputMissing);
            if (Blockers.Any(b => b.HiddenGap)) findings.Add(ToolingRiskDiagnosticCode.ToolingHiddenGapDetected);
            if (Risks.Any(r => r.LocalTruth)) findings.Add(ToolingRiskDiagnosticCode.ToolingLocalTruthDetected);
            if (Risks.Any(r => r.RuntimeClaim)) findings.Add(ToolingRiskDiagnosticCode.ToolingRuntimeClaimDetected);
            if (Risks.Any(r => r.ServerBypass)) findings.Add(ToolingRiskDiagnosticCode.ToolingServerBypassDetected);
            if (findings.Count > 0) findings.Add(ToolingRiskDiagnosticCode.ToolingClosureBlocked);
            return new SocialMmoToolingRiskGateDiagnostics(ResolveVerdict(findings), findings);
        }
        private static SocialMmoToolingVerdict ResolveVerdict(IReadOnlyList<ToolingRiskDiagnosticCode> findings)
        {
            if (findings.Contains(ToolingRiskDiagnosticCode.ToolingServerBypassDetected)) return SocialMmoToolingVerdict.BlockedByServerBypass;
            if (findings.Contains(ToolingRiskDiagnosticCode.ToolingRuntimeClaimDetected)) return SocialMmoToolingVerdict.BlockedByRuntimeClaim;
            if (findings.Contains(ToolingRiskDiagnosticCode.ToolingLocalTruthDetected)) return SocialMmoToolingVerdict.BlockedByLocalTruthRisk;
            if (findings.Contains(ToolingRiskDiagnosticCode.ToolingHiddenGapDetected)) return SocialMmoToolingVerdict.BlockedByHiddenGap;
            if (findings.Contains(ToolingRiskDiagnosticCode.ToolingInputMissing)) return SocialMmoToolingVerdict.NeedsPlannerRevision;
            return SocialMmoToolingVerdict.ReadyForClosureGate;
        }
    }
    public sealed class SocialMmoToolingRiskGateDiagnostics { public SocialMmoToolingRiskGateDiagnostics(SocialMmoToolingVerdict verdict, IReadOnlyList<ToolingRiskDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<ToolingRiskDiagnosticCode>(); } public SocialMmoToolingVerdict Verdict { get; } public IReadOnlyList<ToolingRiskDiagnosticCode> Findings { get; } public bool Contains(ToolingRiskDiagnosticCode code) { return Findings.Contains(code); } }

    public enum SocialMmoToolingClosureVerdict { ReadyForArchitectValidation, ReadyWithWarnings, NeedsPlannerRevision, BlockedByMissingTool, BlockedByHiddenGap, BlockedByServerReadinessGap, BlockedByDemoHonestyGap, BlockedByBee361Premature }
    public enum ToolingClosureDiagnosticCode { ToolingClosureInputMissing, ToolingClosureHiddenGapOpen, ToolingClosureServerGapOpen, ToolingClosureDemoHonestyGapOpen, ToolingClosureRuntimeForbidden, Bee361Premature }
    public sealed class SocialMmoToolingClosureInputSet
    {
        public SocialMmoToolingClosureInputSet(string readModelInventory, string cooperationVisualizationBinding, string armyReadinessVisualization, string pvpFairnessDebugPanel, string serverHandoffQueue, string moderationTriageBoundary, string warScenarioFixtures, string evidenceDrilldown, string toolingRiskGate)
        {
            ReadModelInventory = readModelInventory ?? string.Empty; CooperationVisualizationBinding = cooperationVisualizationBinding ?? string.Empty; ArmyReadinessVisualization = armyReadinessVisualization ?? string.Empty; PvpFairnessDebugPanel = pvpFairnessDebugPanel ?? string.Empty; ServerHandoffQueue = serverHandoffQueue ?? string.Empty; ModerationTriageBoundary = moderationTriageBoundary ?? string.Empty; WarScenarioFixtures = warScenarioFixtures ?? string.Empty; EvidenceDrilldown = evidenceDrilldown ?? string.Empty; ToolingRiskGate = toolingRiskGate ?? string.Empty;
        }
        public string ReadModelInventory { get; } public string CooperationVisualizationBinding { get; } public string ArmyReadinessVisualization { get; } public string PvpFairnessDebugPanel { get; } public string ServerHandoffQueue { get; } public string ModerationTriageBoundary { get; } public string WarScenarioFixtures { get; } public string EvidenceDrilldown { get; } public string ToolingRiskGate { get; }
        public bool HasMissingInput() { return string.IsNullOrWhiteSpace(ReadModelInventory) || string.IsNullOrWhiteSpace(CooperationVisualizationBinding) || string.IsNullOrWhiteSpace(ArmyReadinessVisualization) || string.IsNullOrWhiteSpace(PvpFairnessDebugPanel) || string.IsNullOrWhiteSpace(ServerHandoffQueue) || string.IsNullOrWhiteSpace(ModerationTriageBoundary) || string.IsNullOrWhiteSpace(WarScenarioFixtures) || string.IsNullOrWhiteSpace(EvidenceDrilldown) || string.IsNullOrWhiteSpace(ToolingRiskGate); }
    }
    public sealed class SocialMmoToolingCoverage { public SocialMmoToolingCoverage(bool hiddenGapOpen, bool demoHonestyGapOpen, bool runtimeForbiddenRequested) { HiddenGapOpen = hiddenGapOpen; DemoHonestyGapOpen = demoHonestyGapOpen; RuntimeForbiddenRequested = runtimeForbiddenRequested; } public bool HiddenGapOpen { get; } public bool DemoHonestyGapOpen { get; } public bool RuntimeForbiddenRequested { get; } }
    public sealed class SocialMmoToolingGap { public SocialMmoToolingGap(string gapId, bool serverGapOpen) { GapId = gapId ?? string.Empty; ServerGapOpen = serverGapOpen; } public string GapId { get; } public bool ServerGapOpen { get; } }
    public sealed class SocialMmoToolingOwnerMap { public SocialMmoToolingOwnerMap(bool serverOwnerPresent, bool demoOwnerPresent) { ServerOwnerPresent = serverOwnerPresent; DemoOwnerPresent = demoOwnerPresent; } public bool ServerOwnerPresent { get; } public bool DemoOwnerPresent { get; } }
    public sealed class Bee361BlockerStatus { public Bee361BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class SocialMmoPlaygroundToolingClosureGate
    {
        public const string Bee361BlockedMessage = "BEE-361 bloquee jusqu'a validation architecte.";
        public SocialMmoPlaygroundToolingClosureGate(string gateId, SocialMmoToolingClosureInputSet inputSet, SocialMmoToolingCoverage coverage, IReadOnlyList<SocialMmoToolingGap> gaps, SocialMmoToolingOwnerMap ownerMap, Bee361BlockerStatus bee361Status)
        {
            GateId = ColonyIntegrationIds.Require(gateId); InputSet = inputSet; Coverage = coverage; Gaps = gaps ?? Array.Empty<SocialMmoToolingGap>(); OwnerMap = ownerMap; Bee361Status = bee361Status;
        }
        public string GateId { get; } public SocialMmoToolingClosureInputSet InputSet { get; } public SocialMmoToolingCoverage Coverage { get; } public IReadOnlyList<SocialMmoToolingGap> Gaps { get; } public SocialMmoToolingOwnerMap OwnerMap { get; } public Bee361BlockerStatus Bee361Status { get; }
        public SocialMmoPlaygroundToolingClosureDiagnostics Evaluate()
        {
            var findings = new List<ToolingClosureDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput()) findings.Add(ToolingClosureDiagnosticCode.ToolingClosureInputMissing);
            if (Coverage == null || Coverage.HiddenGapOpen) findings.Add(ToolingClosureDiagnosticCode.ToolingClosureHiddenGapOpen);
            if (Gaps.Any(g => g.ServerGapOpen) || OwnerMap == null || !OwnerMap.ServerOwnerPresent) findings.Add(ToolingClosureDiagnosticCode.ToolingClosureServerGapOpen);
            if (Coverage == null || Coverage.DemoHonestyGapOpen || OwnerMap == null || !OwnerMap.DemoOwnerPresent) findings.Add(ToolingClosureDiagnosticCode.ToolingClosureDemoHonestyGapOpen);
            if (Coverage != null && Coverage.RuntimeForbiddenRequested) findings.Add(ToolingClosureDiagnosticCode.ToolingClosureRuntimeForbidden);
            if (Bee361Status != null && Bee361Status.PrematureAttempt) findings.Add(ToolingClosureDiagnosticCode.Bee361Premature);
            return new SocialMmoPlaygroundToolingClosureDiagnostics(ResolveVerdict(findings), findings);
        }
        private static SocialMmoToolingClosureVerdict ResolveVerdict(IReadOnlyList<ToolingClosureDiagnosticCode> findings)
        {
            if (findings.Contains(ToolingClosureDiagnosticCode.Bee361Premature)) return SocialMmoToolingClosureVerdict.BlockedByBee361Premature;
            if (findings.Contains(ToolingClosureDiagnosticCode.ToolingClosureInputMissing)) return SocialMmoToolingClosureVerdict.BlockedByMissingTool;
            if (findings.Contains(ToolingClosureDiagnosticCode.ToolingClosureHiddenGapOpen)) return SocialMmoToolingClosureVerdict.BlockedByHiddenGap;
            if (findings.Contains(ToolingClosureDiagnosticCode.ToolingClosureServerGapOpen)) return SocialMmoToolingClosureVerdict.BlockedByServerReadinessGap;
            if (findings.Contains(ToolingClosureDiagnosticCode.ToolingClosureDemoHonestyGapOpen)) return SocialMmoToolingClosureVerdict.BlockedByDemoHonestyGap;
            if (findings.Contains(ToolingClosureDiagnosticCode.ToolingClosureRuntimeForbidden)) return SocialMmoToolingClosureVerdict.NeedsPlannerRevision;
            return SocialMmoToolingClosureVerdict.ReadyForArchitectValidation;
        }
    }
    public sealed class SocialMmoPlaygroundToolingClosureDiagnostics { public SocialMmoPlaygroundToolingClosureDiagnostics(SocialMmoToolingClosureVerdict verdict, IReadOnlyList<ToolingClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<ToolingClosureDiagnosticCode>(); } public SocialMmoToolingClosureVerdict Verdict { get; } public IReadOnlyList<ToolingClosureDiagnosticCode> Findings { get; } public bool Contains(ToolingClosureDiagnosticCode code) { return Findings.Contains(code); } }
}
