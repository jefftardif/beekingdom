using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum AllianceObjectiveDiagnosticCode { AllianceObjectiveSourceMissing, AllianceObjectiveRewardForbidden, AllianceObjectiveProgressPersistenceForbidden, AllianceObjectiveSocialPressureRiskOpen, AllianceObjectiveServerAuthorityRequired }
    public enum AllianceObjectiveType { DefensePreparation, EconomicContributionProjection, MemberHelp, RallyParticipationProjection, ArmyReadiness, TerritoryExplorationProjection, SocialHealth }

    public sealed class AllianceObjectiveContributionExpectation
    {
        public AllianceObjectiveContributionExpectation(string expectationId, string description)
        {
            ExpectationId = expectationId ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string ExpectationId { get; }
        public string Description { get; }
    }

    public sealed class AllianceObjectiveProjection
    {
        public AllianceObjectiveProjection(string objectiveId, AllianceObjectiveType objectiveType, AllianceRoleKind proposedByRole, AllianceObjectiveContributionExpectation contributionExpectation, bool rewardAllowed, bool persistenceAllowed)
        {
            ObjectiveId = objectiveId ?? string.Empty;
            ObjectiveType = objectiveType;
            ProposedByRole = proposedByRole;
            ContributionExpectation = contributionExpectation;
            RewardAllowed = rewardAllowed;
            PersistenceAllowed = persistenceAllowed;
        }

        public string ObjectiveId { get; }
        public AllianceObjectiveType ObjectiveType { get; }
        public AllianceRoleKind ProposedByRole { get; }
        public AllianceObjectiveContributionExpectation ContributionExpectation { get; }
        public bool RewardAllowed { get; }
        public bool PersistenceAllowed { get; }
    }

    public sealed class AllianceObjectiveVisibilityRule
    {
        public AllianceObjectiveVisibilityRule(string ruleId, AllianceRoleKind visibleToRole)
        {
            RuleId = ruleId ?? string.Empty;
            VisibleToRole = visibleToRole;
        }

        public string RuleId { get; }
        public AllianceRoleKind VisibleToRole { get; }
    }

    public sealed class AllianceObjectiveSocialPressureRisk
    {
        public AllianceObjectiveSocialPressureRisk(string riskId, bool open)
        {
            RiskId = riskId ?? string.Empty;
            Open = open;
        }

        public string RiskId { get; }
        public bool Open { get; }
    }

    public sealed class AllianceObjectiveServerAuthorityTopic
    {
        public AllianceObjectiveServerAuthorityTopic(string topicId, bool serverRequired)
        {
            TopicId = topicId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class AllianceObjectiveBoardContract
    {
        public AllianceObjectiveBoardContract(string boardId, string allianceProjectionId, IReadOnlyList<AllianceObjectiveProjection> objectives, IReadOnlyList<AllianceObjectiveVisibilityRule> visibilityRules, IReadOnlyList<AllianceObjectiveSocialPressureRisk> socialPressureRisks, IReadOnlyList<AllianceObjectiveServerAuthorityTopic> serverAuthorityTopics)
        {
            BoardId = ColonyIntegrationIds.Require(boardId);
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            Objectives = objectives ?? Array.Empty<AllianceObjectiveProjection>();
            VisibilityRules = visibilityRules ?? Array.Empty<AllianceObjectiveVisibilityRule>();
            SocialPressureRisks = socialPressureRisks ?? Array.Empty<AllianceObjectiveSocialPressureRisk>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<AllianceObjectiveServerAuthorityTopic>();
        }

        public string BoardId { get; }
        public string AllianceProjectionId { get; }
        public IReadOnlyList<AllianceObjectiveProjection> Objectives { get; }
        public IReadOnlyList<AllianceObjectiveVisibilityRule> VisibilityRules { get; }
        public IReadOnlyList<AllianceObjectiveSocialPressureRisk> SocialPressureRisks { get; }
        public IReadOnlyList<AllianceObjectiveServerAuthorityTopic> ServerAuthorityTopics { get; }

        public AllianceObjectiveDiagnostics Evaluate()
        {
            var findings = new List<AllianceObjectiveDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(AllianceProjectionId) || Objectives.Count == 0 || Objectives.Any(o => string.IsNullOrWhiteSpace(o.ObjectiveId) || o.ContributionExpectation == null)) findings.Add(AllianceObjectiveDiagnosticCode.AllianceObjectiveSourceMissing);
            if (Objectives.Any(o => o.RewardAllowed)) findings.Add(AllianceObjectiveDiagnosticCode.AllianceObjectiveRewardForbidden);
            if (Objectives.Any(o => o.PersistenceAllowed)) findings.Add(AllianceObjectiveDiagnosticCode.AllianceObjectiveProgressPersistenceForbidden);
            if (SocialPressureRisks.Any(r => r.Open)) findings.Add(AllianceObjectiveDiagnosticCode.AllianceObjectiveSocialPressureRiskOpen);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired)) findings.Add(AllianceObjectiveDiagnosticCode.AllianceObjectiveServerAuthorityRequired);
            return new AllianceObjectiveDiagnostics(findings);
        }
    }

    public sealed class AllianceObjectiveDiagnostics
    {
        public AllianceObjectiveDiagnostics(IReadOnlyList<AllianceObjectiveDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceObjectiveDiagnosticCode>(); }
        public IReadOnlyList<AllianceObjectiveDiagnosticCode> Findings { get; }
        public bool Contains(AllianceObjectiveDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ContributionDiagnosticCode { ContributionSourceMissing, ContributionOfficialCreditForbidden, ContributionBankMutationForbidden, ContributionPayToWinRiskOpen, ContributionCoercionRiskOpen, ContributionServerAuthorityRequired }
    public enum ContributionType { ParticipationTime, ProjectedDonation, DefenseHelp, MissionParticipation, ArmyReadiness, PostDefeatSupport, ModerationHelp }

    public sealed class ContributionSourceReference
    {
        public ContributionSourceReference(string sourceId, string sourceBee)
        {
            SourceId = sourceId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
        }

        public string SourceId { get; }
        public string SourceBee { get; }
    }

    public sealed class ContributionRecognitionProjection
    {
        public ContributionRecognitionProjection(string recognitionId, bool officialCreditAllowed)
        {
            RecognitionId = recognitionId ?? string.Empty;
            OfficialCreditAllowed = officialCreditAllowed;
        }

        public string RecognitionId { get; }
        public bool OfficialCreditAllowed { get; }
    }

    public sealed class ContributionAbuseRisk
    {
        public ContributionAbuseRisk(string riskType, bool open)
        {
            RiskType = riskType ?? string.Empty;
            Open = open;
        }

        public string RiskType { get; }
        public bool Open { get; }
    }

    public sealed class ContributionServerAuthorityTopic
    {
        public ContributionServerAuthorityTopic(string topicId, bool serverRequired)
        {
            TopicId = topicId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class ContributionEntryProjection
    {
        public ContributionEntryProjection(string entryId, string contributorPlayerHiveIdentityId, ContributionSourceReference sourceReference, ContributionType contributionType, ContributionRecognitionProjection recognitionProjection, bool bankMutationRequested, IReadOnlyList<ContributionAbuseRisk> abuseRisks)
        {
            EntryId = entryId ?? string.Empty;
            ContributorPlayerHiveIdentityId = contributorPlayerHiveIdentityId ?? string.Empty;
            SourceReference = sourceReference;
            ContributionType = contributionType;
            RecognitionProjection = recognitionProjection;
            BankMutationRequested = bankMutationRequested;
            AbuseRisks = abuseRisks ?? Array.Empty<ContributionAbuseRisk>();
        }

        public string EntryId { get; }
        public string ContributorPlayerHiveIdentityId { get; }
        public ContributionSourceReference SourceReference { get; }
        public ContributionType ContributionType { get; }
        public ContributionRecognitionProjection RecognitionProjection { get; }
        public bool BankMutationRequested { get; }
        public IReadOnlyList<ContributionAbuseRisk> AbuseRisks { get; }
    }

    public sealed class CooperativeContributionLedgerBoundary
    {
        public CooperativeContributionLedgerBoundary(string ledgerId, IReadOnlyList<ContributionEntryProjection> entries, IReadOnlyList<ContributionServerAuthorityTopic> serverAuthorityTopics)
        {
            LedgerId = ColonyIntegrationIds.Require(ledgerId);
            Entries = entries ?? Array.Empty<ContributionEntryProjection>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<ContributionServerAuthorityTopic>();
        }

        public string LedgerId { get; }
        public IReadOnlyList<ContributionEntryProjection> Entries { get; }
        public IReadOnlyList<ContributionServerAuthorityTopic> ServerAuthorityTopics { get; }

        public ContributionLedgerDiagnostics Evaluate()
        {
            var findings = new List<ContributionDiagnosticCode>();
            if (Entries.Count == 0 || Entries.Any(e => e.SourceReference == null || string.IsNullOrWhiteSpace(e.SourceReference.SourceBee))) findings.Add(ContributionDiagnosticCode.ContributionSourceMissing);
            if (Entries.Any(e => e.RecognitionProjection != null && e.RecognitionProjection.OfficialCreditAllowed)) findings.Add(ContributionDiagnosticCode.ContributionOfficialCreditForbidden);
            if (Entries.Any(e => e.BankMutationRequested)) findings.Add(ContributionDiagnosticCode.ContributionBankMutationForbidden);
            if (Entries.Any(e => e.AbuseRisks.Any(r => r.Open && r.RiskType == "payToWin"))) findings.Add(ContributionDiagnosticCode.ContributionPayToWinRiskOpen);
            if (Entries.Any(e => e.AbuseRisks.Any(r => r.Open && r.RiskType == "coercion"))) findings.Add(ContributionDiagnosticCode.ContributionCoercionRiskOpen);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired)) findings.Add(ContributionDiagnosticCode.ContributionServerAuthorityRequired);
            return new ContributionLedgerDiagnostics(findings);
        }
    }

    public sealed class ContributionLedgerDiagnostics
    {
        public ContributionLedgerDiagnostics(IReadOnlyList<ContributionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ContributionDiagnosticCode>(); }
        public IReadOnlyList<ContributionDiagnosticCode> Findings { get; }
        public bool Contains(ContributionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceMissionDiagnosticCode { AllianceMissionObjectiveMissing, AllianceMissionPermissionMissing, AllianceMissionRuntimeCommandForbidden, AllianceMissionForcedAssignmentRiskOpen, AllianceMissionCoordinationGapOpen, AllianceMissionServerAuthorityRequired }
    public enum AllianceMissionType { DefensePreparation, ResourceSupportProjection, RallyPreparation, PostDefeatSupport, TerritoryScoutingProjection, ModerationCleanup, ObjectiveContributionDrive }

    public sealed class AllianceMissionParticipantProjection
    {
        public AllianceMissionParticipantProjection(string playerHiveIdentityId, bool voluntary)
        {
            PlayerHiveIdentityId = playerHiveIdentityId ?? string.Empty;
            Voluntary = voluntary;
        }

        public string PlayerHiveIdentityId { get; }
        public bool Voluntary { get; }
    }

    public sealed class AllianceMissionPriorityProjection
    {
        public AllianceMissionPriorityProjection(string priorityId, int value)
        {
            PriorityId = priorityId ?? string.Empty;
            Value = Math.Max(0, value);
        }

        public string PriorityId { get; }
        public int Value { get; }
    }

    public sealed class AllianceMissionAssignmentIntent
    {
        public AllianceMissionAssignmentIntent(string intentId, bool runtimeCommandRequested, bool forcedAssignmentRisk)
        {
            IntentId = intentId ?? string.Empty;
            RuntimeCommandRequested = runtimeCommandRequested;
            ForcedAssignmentRisk = forcedAssignmentRisk;
        }

        public string IntentId { get; }
        public bool RuntimeCommandRequested { get; }
        public bool ForcedAssignmentRisk { get; }
    }

    public sealed class AllianceMissionCoordinationGap
    {
        public AllianceMissionCoordinationGap(string gapId, bool open)
        {
            GapId = gapId ?? string.Empty;
            Open = open;
        }

        public string GapId { get; }
        public bool Open { get; }
    }

    public sealed class AllianceMissionServerAuthorityTopic
    {
        public AllianceMissionServerAuthorityTopic(string topicId, bool serverRequired)
        {
            TopicId = topicId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class AllianceMissionCoordinationProjection
    {
        public AllianceMissionCoordinationProjection(string missionId, string objectiveId, AllianceMissionType missionType, AllianceMissionPriorityProjection priorityProjection, IReadOnlyList<AllianceMissionParticipantProjection> participants, IReadOnlyList<AllianceMissionAssignmentIntent> assignmentIntents, IReadOnlyList<AllianceMissionCoordinationGap> coordinationGaps, IReadOnlyList<AllianceMissionServerAuthorityTopic> serverAuthorityTopics, bool permissionPresent)
        {
            MissionId = ColonyIntegrationIds.Require(missionId);
            ObjectiveId = objectiveId ?? string.Empty;
            MissionType = missionType;
            PriorityProjection = priorityProjection;
            Participants = participants ?? Array.Empty<AllianceMissionParticipantProjection>();
            AssignmentIntents = assignmentIntents ?? Array.Empty<AllianceMissionAssignmentIntent>();
            CoordinationGaps = coordinationGaps ?? Array.Empty<AllianceMissionCoordinationGap>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<AllianceMissionServerAuthorityTopic>();
            PermissionPresent = permissionPresent;
        }

        public string MissionId { get; }
        public string ObjectiveId { get; }
        public AllianceMissionType MissionType { get; }
        public AllianceMissionPriorityProjection PriorityProjection { get; }
        public IReadOnlyList<AllianceMissionParticipantProjection> Participants { get; }
        public IReadOnlyList<AllianceMissionAssignmentIntent> AssignmentIntents { get; }
        public IReadOnlyList<AllianceMissionCoordinationGap> CoordinationGaps { get; }
        public IReadOnlyList<AllianceMissionServerAuthorityTopic> ServerAuthorityTopics { get; }
        public bool PermissionPresent { get; }

        public AllianceMissionDiagnostics Evaluate()
        {
            var findings = new List<AllianceMissionDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(ObjectiveId)) findings.Add(AllianceMissionDiagnosticCode.AllianceMissionObjectiveMissing);
            if (!PermissionPresent) findings.Add(AllianceMissionDiagnosticCode.AllianceMissionPermissionMissing);
            if (AssignmentIntents.Any(a => a.RuntimeCommandRequested)) findings.Add(AllianceMissionDiagnosticCode.AllianceMissionRuntimeCommandForbidden);
            if (AssignmentIntents.Any(a => a.ForcedAssignmentRisk) || Participants.Any(p => !p.Voluntary)) findings.Add(AllianceMissionDiagnosticCode.AllianceMissionForcedAssignmentRiskOpen);
            if (CoordinationGaps.Any(g => g.Open)) findings.Add(AllianceMissionDiagnosticCode.AllianceMissionCoordinationGapOpen);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired)) findings.Add(AllianceMissionDiagnosticCode.AllianceMissionServerAuthorityRequired);
            return new AllianceMissionDiagnostics(findings);
        }
    }

    public sealed class AllianceMissionDiagnostics
    {
        public AllianceMissionDiagnostics(IReadOnlyList<AllianceMissionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceMissionDiagnosticCode>(); }
        public IReadOnlyList<AllianceMissionDiagnosticCode> Findings { get; }
        public bool Contains(AllianceMissionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ArmyCompositionDiagnosticCode { ArmyUnitFamilyPreviewMissing, ArmyPersistentCompositionForbidden, ArmyCombatPowerOfficialForbidden, ArmyBalanceRiskOpen, ArmyMatchmakingImpactServerRequired, ArmyCompositionServerAuthorityRequired }
    public enum ArmyUnitFamily { WorkersSupport, Soldiers, Guards, Scouts, Engineers, Medics, Carriers, SpecialUnitsPlaceholder }

    public sealed class ArmyUnitFamilyPreview
    {
        public ArmyUnitFamilyPreview(ArmyUnitFamily family, string role, bool persistentCompositionRequested)
        {
            Family = family;
            Role = role ?? string.Empty;
            PersistentCompositionRequested = persistentCompositionRequested;
        }

        public ArmyUnitFamily Family { get; }
        public string Role { get; }
        public bool PersistentCompositionRequested { get; }
    }

    public sealed class ArmyRoleBalanceProjection
    {
        public ArmyRoleBalanceProjection(string balanceId, bool combatPowerOfficialRequested)
        {
            BalanceId = balanceId ?? string.Empty;
            CombatPowerOfficialRequested = combatPowerOfficialRequested;
        }

        public string BalanceId { get; }
        public bool CombatPowerOfficialRequested { get; }
    }

    public sealed class ArmyStrengthWeaknessProjection
    {
        public ArmyStrengthWeaknessProjection(string projectionId, string strength, string weakness)
        {
            ProjectionId = projectionId ?? string.Empty;
            Strength = strength ?? string.Empty;
            Weakness = weakness ?? string.Empty;
        }

        public string ProjectionId { get; }
        public string Strength { get; }
        public string Weakness { get; }
    }

    public sealed class ArmyCompositionReadinessSignal
    {
        public ArmyCompositionReadinessSignal(string signalId, double value)
        {
            SignalId = signalId ?? string.Empty;
            Value = ColonyIntegrationIds.Clamp01(value);
        }

        public string SignalId { get; }
        public double Value { get; }
    }

    public sealed class ArmyCompositionBalanceRisk
    {
        public ArmyCompositionBalanceRisk(string riskId, bool open, bool matchmakingImpactServerRequired)
        {
            RiskId = riskId ?? string.Empty;
            Open = open;
            MatchmakingImpactServerRequired = matchmakingImpactServerRequired;
        }

        public string RiskId { get; }
        public bool Open { get; }
        public bool MatchmakingImpactServerRequired { get; }
    }

    public sealed class ArmyCompositionPreviewBoundary
    {
        public ArmyCompositionPreviewBoundary(string playerHiveIdentityId, IReadOnlyList<ArmyUnitFamilyPreview> unitFamilyPreviews, ArmyRoleBalanceProjection roleBalanceProjection, IReadOnlyList<ArmyStrengthWeaknessProjection> strengthWeaknessProjection, IReadOnlyList<ArmyCompositionReadinessSignal> readinessSignals, IReadOnlyList<ArmyCompositionBalanceRisk> balanceRisks, IReadOnlyList<string> serverAuthorityTopics)
        {
            PlayerHiveIdentityId = playerHiveIdentityId ?? string.Empty;
            UnitFamilyPreviews = unitFamilyPreviews ?? Array.Empty<ArmyUnitFamilyPreview>();
            RoleBalanceProjection = roleBalanceProjection;
            StrengthWeaknessProjection = strengthWeaknessProjection ?? Array.Empty<ArmyStrengthWeaknessProjection>();
            ReadinessSignals = readinessSignals ?? Array.Empty<ArmyCompositionReadinessSignal>();
            BalanceRisks = balanceRisks ?? Array.Empty<ArmyCompositionBalanceRisk>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
        }

        public string PlayerHiveIdentityId { get; }
        public IReadOnlyList<ArmyUnitFamilyPreview> UnitFamilyPreviews { get; }
        public ArmyRoleBalanceProjection RoleBalanceProjection { get; }
        public IReadOnlyList<ArmyStrengthWeaknessProjection> StrengthWeaknessProjection { get; }
        public IReadOnlyList<ArmyCompositionReadinessSignal> ReadinessSignals { get; }
        public IReadOnlyList<ArmyCompositionBalanceRisk> BalanceRisks { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }

        public ArmyCompositionDiagnostics Evaluate()
        {
            var findings = new List<ArmyCompositionDiagnosticCode>();
            if (UnitFamilyPreviews.Count == 0 || UnitFamilyPreviews.Any(u => string.IsNullOrWhiteSpace(u.Role))) findings.Add(ArmyCompositionDiagnosticCode.ArmyUnitFamilyPreviewMissing);
            if (UnitFamilyPreviews.Any(u => u.PersistentCompositionRequested)) findings.Add(ArmyCompositionDiagnosticCode.ArmyPersistentCompositionForbidden);
            if (RoleBalanceProjection != null && RoleBalanceProjection.CombatPowerOfficialRequested) findings.Add(ArmyCompositionDiagnosticCode.ArmyCombatPowerOfficialForbidden);
            if (BalanceRisks.Any(r => r.Open)) findings.Add(ArmyCompositionDiagnosticCode.ArmyBalanceRiskOpen);
            if (BalanceRisks.Any(r => r.MatchmakingImpactServerRequired)) findings.Add(ArmyCompositionDiagnosticCode.ArmyMatchmakingImpactServerRequired);
            if (ServerAuthorityTopics.Count > 0) findings.Add(ArmyCompositionDiagnosticCode.ArmyCompositionServerAuthorityRequired);
            return new ArmyCompositionDiagnostics(findings);
        }
    }

    public sealed class ArmyCompositionDiagnostics
    {
        public ArmyCompositionDiagnostics(IReadOnlyList<ArmyCompositionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyCompositionDiagnosticCode>(); }
        public IReadOnlyList<ArmyCompositionDiagnosticCode> Findings { get; }
        public bool Contains(ArmyCompositionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum RallyCommitmentState { InvitedProjected, ConfirmedProjected, TentativeProjected, WithdrawnProjected, ExpiredProjected, BlockedByProtection, ServerAuthorityRequired }
    public enum RallyCommitmentDiagnosticCode { RallyCommitmentMissing, RallyParticipantConsentMissing, RallyWithdrawalWindowExpiredProjected, RallyProtectionWarningOpen, RallyMobilizationForbidden, RallyCommitmentServerAuthorityRequired }

    public sealed class RallyCommitmentWindow
    {
        public RallyCommitmentWindow(string windowId, bool expiredProjected)
        {
            WindowId = windowId ?? string.Empty;
            ExpiredProjected = expiredProjected;
        }

        public string WindowId { get; }
        public bool ExpiredProjected { get; }
    }

    public sealed class RallyWithdrawalProjection
    {
        public RallyWithdrawalProjection(bool withdrawnProjected, bool windowExpiredProjected)
        {
            WithdrawnProjected = withdrawnProjected;
            WindowExpiredProjected = windowExpiredProjected;
        }

        public bool WithdrawnProjected { get; }
        public bool WindowExpiredProjected { get; }
    }

    public sealed class RallyParticipationWarning
    {
        public RallyParticipationWarning(string warningId, bool open)
        {
            WarningId = warningId ?? string.Empty;
            Open = open;
        }

        public string WarningId { get; }
        public bool Open { get; }
    }

    public sealed class RallyParticipantCommitmentProjection
    {
        public RallyParticipantCommitmentProjection(string participantPlayerHiveIdentityId, string rallyId, RallyCommitmentState commitmentState, RallyCommitmentWindow commitmentWindow, RallyWithdrawalProjection withdrawalProjection, IReadOnlyList<RallyParticipationWarning> warnings, IReadOnlyList<string> serverAuthorityTopics, bool consentPresent, bool mobilizationRequested)
        {
            ParticipantPlayerHiveIdentityId = participantPlayerHiveIdentityId ?? string.Empty;
            RallyId = rallyId ?? string.Empty;
            CommitmentState = commitmentState;
            CommitmentWindow = commitmentWindow;
            WithdrawalProjection = withdrawalProjection;
            Warnings = warnings ?? Array.Empty<RallyParticipationWarning>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
            ConsentPresent = consentPresent;
            MobilizationRequested = mobilizationRequested;
        }

        public string ParticipantPlayerHiveIdentityId { get; }
        public string RallyId { get; }
        public RallyCommitmentState CommitmentState { get; }
        public RallyCommitmentWindow CommitmentWindow { get; }
        public RallyWithdrawalProjection WithdrawalProjection { get; }
        public IReadOnlyList<RallyParticipationWarning> Warnings { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }
        public bool ConsentPresent { get; }
        public bool MobilizationRequested { get; }

        public RallyCommitmentDiagnostics Evaluate()
        {
            var findings = new List<RallyCommitmentDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(ParticipantPlayerHiveIdentityId) || string.IsNullOrWhiteSpace(RallyId)) findings.Add(RallyCommitmentDiagnosticCode.RallyCommitmentMissing);
            if (!ConsentPresent) findings.Add(RallyCommitmentDiagnosticCode.RallyParticipantConsentMissing);
            if ((CommitmentWindow != null && CommitmentWindow.ExpiredProjected) || (WithdrawalProjection != null && WithdrawalProjection.WindowExpiredProjected)) findings.Add(RallyCommitmentDiagnosticCode.RallyWithdrawalWindowExpiredProjected);
            if (Warnings.Any(w => w.Open) || CommitmentState == RallyCommitmentState.BlockedByProtection) findings.Add(RallyCommitmentDiagnosticCode.RallyProtectionWarningOpen);
            if (MobilizationRequested) findings.Add(RallyCommitmentDiagnosticCode.RallyMobilizationForbidden);
            if (ServerAuthorityTopics.Count > 0 || CommitmentState == RallyCommitmentState.ServerAuthorityRequired) findings.Add(RallyCommitmentDiagnosticCode.RallyCommitmentServerAuthorityRequired);
            return new RallyCommitmentDiagnostics(findings);
        }
    }

    public sealed class RallyCommitmentDiagnostics
    {
        public RallyCommitmentDiagnostics(IReadOnlyList<RallyCommitmentDiagnosticCode> findings) { Findings = findings ?? Array.Empty<RallyCommitmentDiagnosticCode>(); }
        public IReadOnlyList<RallyCommitmentDiagnosticCode> Findings { get; }
        public bool Contains(RallyCommitmentDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum PvPLossBudgetDiagnosticCode { PvPLossRuntimeForbidden, PvPLootLimitNonFinal, PvPRecoveryBudgetMissing, PvPFrustrationRiskOpen, PvPFarmingRiskOpen, PvPLossServerAuthorityRequired }

    public sealed class ProjectedLossCategory
    {
        public ProjectedLossCategory(string categoryId, bool runtimeLossRequested)
        {
            CategoryId = categoryId ?? string.Empty;
            RuntimeLossRequested = runtimeLossRequested;
        }

        public string CategoryId { get; }
        public bool RuntimeLossRequested { get; }
    }

    public sealed class ProjectedLootLimit
    {
        public ProjectedLootLimit(string limitId, bool nonFinal)
        {
            LimitId = limitId ?? string.Empty;
            NonFinal = nonFinal;
        }

        public string LimitId { get; }
        public bool NonFinal { get; }
    }

    public sealed class RecoveryBudgetProjection
    {
        public RecoveryBudgetProjection(string recoveryId, bool missing)
        {
            RecoveryId = recoveryId ?? string.Empty;
            Missing = missing;
        }

        public string RecoveryId { get; }
        public bool Missing { get; }
    }

    public sealed class FrustrationRiskSignal
    {
        public FrustrationRiskSignal(string riskId, bool frustrationOpen, bool farmingOpen)
        {
            RiskId = riskId ?? string.Empty;
            FrustrationOpen = frustrationOpen;
            FarmingOpen = farmingOpen;
        }

        public string RiskId { get; }
        public bool FrustrationOpen { get; }
        public bool FarmingOpen { get; }
    }

    public sealed class PvPLossServerAuthorityTopic
    {
        public PvPLossServerAuthorityTopic(string topicId, bool serverRequired)
        {
            TopicId = topicId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class PvPLossBudgetBoundary
    {
        public PvPLossBudgetBoundary(string targetPlayerHiveIdentityId, IReadOnlyList<ProjectedLossCategory> lossCategories, IReadOnlyList<ProjectedLootLimit> lootLimits, RecoveryBudgetProjection recoveryBudget, IReadOnlyList<FrustrationRiskSignal> frustrationRisks, IReadOnlyList<PvPLossServerAuthorityTopic> serverAuthorityTopics)
        {
            TargetPlayerHiveIdentityId = targetPlayerHiveIdentityId ?? string.Empty;
            LossCategories = lossCategories ?? Array.Empty<ProjectedLossCategory>();
            LootLimits = lootLimits ?? Array.Empty<ProjectedLootLimit>();
            RecoveryBudget = recoveryBudget;
            FrustrationRisks = frustrationRisks ?? Array.Empty<FrustrationRiskSignal>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<PvPLossServerAuthorityTopic>();
        }

        public string TargetPlayerHiveIdentityId { get; }
        public IReadOnlyList<ProjectedLossCategory> LossCategories { get; }
        public IReadOnlyList<ProjectedLootLimit> LootLimits { get; }
        public RecoveryBudgetProjection RecoveryBudget { get; }
        public IReadOnlyList<FrustrationRiskSignal> FrustrationRisks { get; }
        public IReadOnlyList<PvPLossServerAuthorityTopic> ServerAuthorityTopics { get; }

        public PvPLossBudgetDiagnostics Evaluate()
        {
            var findings = new List<PvPLossBudgetDiagnosticCode>();
            if (LossCategories.Any(c => c.RuntimeLossRequested)) findings.Add(PvPLossBudgetDiagnosticCode.PvPLossRuntimeForbidden);
            if (LootLimits.Count == 0 || LootLimits.Any(l => l.NonFinal)) findings.Add(PvPLossBudgetDiagnosticCode.PvPLootLimitNonFinal);
            if (RecoveryBudget == null || RecoveryBudget.Missing) findings.Add(PvPLossBudgetDiagnosticCode.PvPRecoveryBudgetMissing);
            if (FrustrationRisks.Any(r => r.FrustrationOpen)) findings.Add(PvPLossBudgetDiagnosticCode.PvPFrustrationRiskOpen);
            if (FrustrationRisks.Any(r => r.FarmingOpen)) findings.Add(PvPLossBudgetDiagnosticCode.PvPFarmingRiskOpen);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired)) findings.Add(PvPLossBudgetDiagnosticCode.PvPLossServerAuthorityRequired);
            return new PvPLossBudgetDiagnostics(findings);
        }
    }

    public sealed class PvPLossBudgetDiagnostics
    {
        public PvPLossBudgetDiagnostics(IReadOnlyList<PvPLossBudgetDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PvPLossBudgetDiagnosticCode>(); }
        public IReadOnlyList<PvPLossBudgetDiagnosticCode> Findings { get; }
        public bool Contains(PvPLossBudgetDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AntiSnowballDiagnosticCode { PowerGapThresholdNonFinal, RepeatedFarmingRiskOpen, EconomicSnowballRiskOpen, PayToWinFairnessRiskOpen, TerritoryDominanceRiskOpen, FairnessServerAuthorityRequired }

    public sealed class PowerGapSignalProjection
    {
        public PowerGapSignalProjection(string signalId, bool nonFinalBalance)
        {
            SignalId = signalId ?? string.Empty;
            NonFinalBalance = nonFinalBalance;
        }

        public string SignalId { get; }
        public bool NonFinalBalance { get; }
    }

    public sealed class RepeatedFarmingRiskProjection
    {
        public RepeatedFarmingRiskProjection(string riskId, bool open)
        {
            RiskId = riskId ?? string.Empty;
            Open = open;
        }

        public string RiskId { get; }
        public bool Open { get; }
    }

    public sealed class EconomicAdvantageRiskProjection
    {
        public EconomicAdvantageRiskProjection(string riskId, bool economicSnowballOpen, bool payToWinOpen)
        {
            RiskId = riskId ?? string.Empty;
            EconomicSnowballOpen = economicSnowballOpen;
            PayToWinOpen = payToWinOpen;
        }

        public string RiskId { get; }
        public bool EconomicSnowballOpen { get; }
        public bool PayToWinOpen { get; }
    }

    public sealed class TerritoryDominanceRiskProjection
    {
        public TerritoryDominanceRiskProjection(string riskId, bool open)
        {
            RiskId = riskId ?? string.Empty;
            Open = open;
        }

        public string RiskId { get; }
        public bool Open { get; }
    }

    public sealed class FairnessWarningProjection
    {
        public FairnessWarningProjection(string warningId, bool serverAuthorityRequired)
        {
            WarningId = warningId ?? string.Empty;
            ServerAuthorityRequired = serverAuthorityRequired;
        }

        public string WarningId { get; }
        public bool ServerAuthorityRequired { get; }
    }

    public sealed class AntiSnowballFairnessPolicy
    {
        public AntiSnowballFairnessPolicy(string policyId, IReadOnlyList<PowerGapSignalProjection> powerGapSignals, IReadOnlyList<RepeatedFarmingRiskProjection> farmingRisks, IReadOnlyList<EconomicAdvantageRiskProjection> economicAdvantageRisks, IReadOnlyList<TerritoryDominanceRiskProjection> territoryDominanceRisks, IReadOnlyList<FairnessWarningProjection> fairnessWarnings, IReadOnlyList<string> serverAuthorityTopics)
        {
            PolicyId = ColonyIntegrationIds.Require(policyId);
            PowerGapSignals = powerGapSignals ?? Array.Empty<PowerGapSignalProjection>();
            FarmingRisks = farmingRisks ?? Array.Empty<RepeatedFarmingRiskProjection>();
            EconomicAdvantageRisks = economicAdvantageRisks ?? Array.Empty<EconomicAdvantageRiskProjection>();
            TerritoryDominanceRisks = territoryDominanceRisks ?? Array.Empty<TerritoryDominanceRiskProjection>();
            FairnessWarnings = fairnessWarnings ?? Array.Empty<FairnessWarningProjection>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
        }

        public string PolicyId { get; }
        public IReadOnlyList<PowerGapSignalProjection> PowerGapSignals { get; }
        public IReadOnlyList<RepeatedFarmingRiskProjection> FarmingRisks { get; }
        public IReadOnlyList<EconomicAdvantageRiskProjection> EconomicAdvantageRisks { get; }
        public IReadOnlyList<TerritoryDominanceRiskProjection> TerritoryDominanceRisks { get; }
        public IReadOnlyList<FairnessWarningProjection> FairnessWarnings { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }

        public AntiSnowballDiagnostics Evaluate()
        {
            var findings = new List<AntiSnowballDiagnosticCode>();
            if (PowerGapSignals.Count == 0 || PowerGapSignals.Any(s => s.NonFinalBalance)) findings.Add(AntiSnowballDiagnosticCode.PowerGapThresholdNonFinal);
            if (FarmingRisks.Any(r => r.Open)) findings.Add(AntiSnowballDiagnosticCode.RepeatedFarmingRiskOpen);
            if (EconomicAdvantageRisks.Any(r => r.EconomicSnowballOpen)) findings.Add(AntiSnowballDiagnosticCode.EconomicSnowballRiskOpen);
            if (EconomicAdvantageRisks.Any(r => r.PayToWinOpen)) findings.Add(AntiSnowballDiagnosticCode.PayToWinFairnessRiskOpen);
            if (TerritoryDominanceRisks.Any(r => r.Open)) findings.Add(AntiSnowballDiagnosticCode.TerritoryDominanceRiskOpen);
            if (ServerAuthorityTopics.Count > 0 || FairnessWarnings.Any(w => w.ServerAuthorityRequired)) findings.Add(AntiSnowballDiagnosticCode.FairnessServerAuthorityRequired);
            return new AntiSnowballDiagnostics(findings);
        }
    }

    public sealed class AntiSnowballDiagnostics
    {
        public AntiSnowballDiagnostics(IReadOnlyList<AntiSnowballDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AntiSnowballDiagnosticCode>(); }
        public IReadOnlyList<AntiSnowballDiagnosticCode> Findings { get; }
        public bool Contains(AntiSnowballDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum HelpRequestDiagnosticCode { HelpRequestTypeMissing, HelpRequestSpamRiskOpen, HelpRequestResourceDeliveryForbidden, HelpRequestTeleportForbidden, HelpRequestModerationRiskOpen, HelpRequestServerAuthorityRequired }
    public enum HelpRequestType { PostDefeatRecovery, DefensePreparation, ObjectiveContribution, MilitaryAdvice, EconomicHelpProjection, ModerationReport, RallySupport, Missing }

    public sealed class HelpRequestPriorityProjection
    {
        public HelpRequestPriorityProjection(string priorityId, int value)
        {
            PriorityId = priorityId ?? string.Empty;
            Value = Math.Max(0, value);
        }

        public string PriorityId { get; }
        public int Value { get; }
    }

    public sealed class HelpResponseProjection
    {
        public HelpResponseProjection(string responseId, bool resourceDeliveryRequested, bool teleportRequested)
        {
            ResponseId = responseId ?? string.Empty;
            ResourceDeliveryRequested = resourceDeliveryRequested;
            TeleportRequested = teleportRequested;
        }

        public string ResponseId { get; }
        public bool ResourceDeliveryRequested { get; }
        public bool TeleportRequested { get; }
    }

    public sealed class HelpRequestExpiryProjection
    {
        public HelpRequestExpiryProjection(bool expiredProjected)
        {
            ExpiredProjected = expiredProjected;
        }

        public bool ExpiredProjected { get; }
    }

    public sealed class HelpRequestAbuseRisk
    {
        public HelpRequestAbuseRisk(string riskType, bool open)
        {
            RiskType = riskType ?? string.Empty;
            Open = open;
        }

        public string RiskType { get; }
        public bool Open { get; }
    }

    public sealed class AllianceHelpRequestProjection
    {
        public AllianceHelpRequestProjection(string requestId, string requesterPlayerHiveIdentityId, string allianceProjectionId, HelpRequestType helpType, HelpRequestPriorityProjection priorityProjection, IReadOnlyList<HelpResponseProjection> responses, HelpRequestExpiryProjection expiryProjection, IReadOnlyList<HelpRequestAbuseRisk> abuseRisks, IReadOnlyList<string> serverAuthorityTopics)
        {
            RequestId = requestId ?? string.Empty;
            RequesterPlayerHiveIdentityId = requesterPlayerHiveIdentityId ?? string.Empty;
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            HelpType = helpType;
            PriorityProjection = priorityProjection;
            Responses = responses ?? Array.Empty<HelpResponseProjection>();
            ExpiryProjection = expiryProjection;
            AbuseRisks = abuseRisks ?? Array.Empty<HelpRequestAbuseRisk>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
        }

        public string RequestId { get; }
        public string RequesterPlayerHiveIdentityId { get; }
        public string AllianceProjectionId { get; }
        public HelpRequestType HelpType { get; }
        public HelpRequestPriorityProjection PriorityProjection { get; }
        public IReadOnlyList<HelpResponseProjection> Responses { get; }
        public HelpRequestExpiryProjection ExpiryProjection { get; }
        public IReadOnlyList<HelpRequestAbuseRisk> AbuseRisks { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }
    }

    public sealed class AllianceHelpRequestFlowContract
    {
        public AllianceHelpRequestFlowContract(string flowId, IReadOnlyList<AllianceHelpRequestProjection> requests)
        {
            FlowId = ColonyIntegrationIds.Require(flowId);
            Requests = requests ?? Array.Empty<AllianceHelpRequestProjection>();
        }

        public string FlowId { get; }
        public IReadOnlyList<AllianceHelpRequestProjection> Requests { get; }

        public AllianceHelpRequestDiagnostics Evaluate()
        {
            var findings = new List<HelpRequestDiagnosticCode>();
            if (Requests.Count == 0 || Requests.Any(r => r.HelpType == HelpRequestType.Missing || string.IsNullOrWhiteSpace(r.RequestId))) findings.Add(HelpRequestDiagnosticCode.HelpRequestTypeMissing);
            if (Requests.Any(r => r.AbuseRisks.Any(a => a.Open && a.RiskType == "spam"))) findings.Add(HelpRequestDiagnosticCode.HelpRequestSpamRiskOpen);
            if (Requests.Any(r => r.Responses.Any(x => x.ResourceDeliveryRequested))) findings.Add(HelpRequestDiagnosticCode.HelpRequestResourceDeliveryForbidden);
            if (Requests.Any(r => r.Responses.Any(x => x.TeleportRequested))) findings.Add(HelpRequestDiagnosticCode.HelpRequestTeleportForbidden);
            if (Requests.Any(r => r.AbuseRisks.Any(a => a.Open && a.RiskType == "moderation"))) findings.Add(HelpRequestDiagnosticCode.HelpRequestModerationRiskOpen);
            if (Requests.Any(r => r.ServerAuthorityTopics.Count > 0)) findings.Add(HelpRequestDiagnosticCode.HelpRequestServerAuthorityRequired);
            return new AllianceHelpRequestDiagnostics(findings);
        }
    }

    public sealed class AllianceHelpRequestDiagnostics
    {
        public AllianceHelpRequestDiagnostics(IReadOnlyList<HelpRequestDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HelpRequestDiagnosticCode>(); }
        public IReadOnlyList<HelpRequestDiagnosticCode> Findings { get; }
        public bool Contains(HelpRequestDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum WarReadinessSignalLevel { Low, Partial, High, Blocked }
    public enum WarReadinessDiagnosticCode { WarReadinessComponentMissing, WarReadinessSignalNonOfficial, WarReadinessProtectionGapOpen, WarReadinessFairnessGapOpen, WarDeclarationForbidden, WarReadinessServerAuthorityRequired }

    public sealed class AllianceWarReadinessComponent
    {
        public AllianceWarReadinessComponent(string componentId, string sourceBee, WarReadinessSignalLevel signalLevel)
        {
            ComponentId = componentId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            SignalLevel = signalLevel;
        }

        public string ComponentId { get; }
        public string SourceBee { get; }
        public WarReadinessSignalLevel SignalLevel { get; }
    }

    public sealed class ArmyReadinessComponent
    {
        public ArmyReadinessComponent(string componentId, WarReadinessSignalLevel signalLevel)
        {
            ComponentId = componentId ?? string.Empty;
            SignalLevel = signalLevel;
        }

        public string ComponentId { get; }
        public WarReadinessSignalLevel SignalLevel { get; }
    }

    public sealed class TerritoryReadinessComponent
    {
        public TerritoryReadinessComponent(string componentId, bool gapOpen)
        {
            ComponentId = componentId ?? string.Empty;
            GapOpen = gapOpen;
        }

        public string ComponentId { get; }
        public bool GapOpen { get; }
    }

    public sealed class ProtectionReadinessComponent
    {
        public ProtectionReadinessComponent(string componentId, bool protectionGapOpen, bool fairnessGapOpen)
        {
            ComponentId = componentId ?? string.Empty;
            ProtectionGapOpen = protectionGapOpen;
            FairnessGapOpen = fairnessGapOpen;
        }

        public string ComponentId { get; }
        public bool ProtectionGapOpen { get; }
        public bool FairnessGapOpen { get; }
    }

    public sealed class WarReadinessGap
    {
        public WarReadinessGap(string gapId, bool open)
        {
            GapId = gapId ?? string.Empty;
            Open = open;
        }

        public string GapId { get; }
        public bool Open { get; }
    }

    public sealed class WarReadinessServerAuthorityTopic
    {
        public WarReadinessServerAuthorityTopic(string topicId, bool serverRequired)
        {
            TopicId = topicId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class WarReadinessSignalProjection
    {
        public WarReadinessSignalProjection(string allianceProjectionId, IReadOnlyList<AllianceWarReadinessComponent> allianceComponents, IReadOnlyList<ArmyReadinessComponent> armyComponents, IReadOnlyList<TerritoryReadinessComponent> territoryComponents, IReadOnlyList<ProtectionReadinessComponent> protectionComponents, IReadOnlyList<WarReadinessGap> gaps, IReadOnlyList<string> risks, IReadOnlyList<WarReadinessServerAuthorityTopic> serverAuthorityTopics, bool officialWarAllowed)
        {
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            AllianceComponents = allianceComponents ?? Array.Empty<AllianceWarReadinessComponent>();
            ArmyComponents = armyComponents ?? Array.Empty<ArmyReadinessComponent>();
            TerritoryComponents = territoryComponents ?? Array.Empty<TerritoryReadinessComponent>();
            ProtectionComponents = protectionComponents ?? Array.Empty<ProtectionReadinessComponent>();
            Gaps = gaps ?? Array.Empty<WarReadinessGap>();
            Risks = risks ?? Array.Empty<string>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<WarReadinessServerAuthorityTopic>();
            OfficialWarAllowed = officialWarAllowed;
        }

        public string AllianceProjectionId { get; }
        public IReadOnlyList<AllianceWarReadinessComponent> AllianceComponents { get; }
        public IReadOnlyList<ArmyReadinessComponent> ArmyComponents { get; }
        public IReadOnlyList<TerritoryReadinessComponent> TerritoryComponents { get; }
        public IReadOnlyList<ProtectionReadinessComponent> ProtectionComponents { get; }
        public IReadOnlyList<WarReadinessGap> Gaps { get; }
        public IReadOnlyList<string> Risks { get; }
        public IReadOnlyList<WarReadinessServerAuthorityTopic> ServerAuthorityTopics { get; }
        public bool OfficialWarAllowed { get; }

        public WarReadinessDiagnostics Evaluate()
        {
            var findings = new List<WarReadinessDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(AllianceProjectionId) || AllianceComponents.Count == 0 || ArmyComponents.Count == 0) findings.Add(WarReadinessDiagnosticCode.WarReadinessComponentMissing);
            if (AllianceComponents.Any(c => c.SignalLevel == WarReadinessSignalLevel.Blocked) || ArmyComponents.Any(c => c.SignalLevel == WarReadinessSignalLevel.Blocked)) findings.Add(WarReadinessDiagnosticCode.WarReadinessSignalNonOfficial);
            if (ProtectionComponents.Any(c => c.ProtectionGapOpen)) findings.Add(WarReadinessDiagnosticCode.WarReadinessProtectionGapOpen);
            if (ProtectionComponents.Any(c => c.FairnessGapOpen) || Gaps.Any(g => g.Open)) findings.Add(WarReadinessDiagnosticCode.WarReadinessFairnessGapOpen);
            if (OfficialWarAllowed) findings.Add(WarReadinessDiagnosticCode.WarDeclarationForbidden);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired)) findings.Add(WarReadinessDiagnosticCode.WarReadinessServerAuthorityRequired);
            return new WarReadinessDiagnostics(findings);
        }
    }

    public sealed class WarReadinessDiagnostics
    {
        public WarReadinessDiagnostics(IReadOnlyList<WarReadinessDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WarReadinessDiagnosticCode>(); }
        public IReadOnlyList<WarReadinessDiagnosticCode> Findings { get; }
        public bool Contains(WarReadinessDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum CooperativePvPReadinessVerdictType { ReadyForArchitectValidation, ReadyWithWarnings, NeedsPlannerRevision, BlockedByMissingCooperationInput, BlockedByPvPFairnessGap, BlockedByPlayerProtectionGap, BlockedByDemoEvidenceGap, BlockedByBee341Premature }
    public enum CooperativePvPReadinessDiagnosticCode { CooperativePvPInputMissing, CooperativeObjectiveGapOpen, PvPFairnessGapOpen, PlayerRecoveryGapOpen, WarReadinessGapOpen, Bee341Premature }

    public sealed class CooperativePvPInputSet
    {
        public CooperativePvPInputSet(string objectiveBoard, string contributionLedger, string missionCoordination, string armyCompositionPreview, string rallyCommitment, string lossBudget, string antiSnowballPolicy, string helpRequestFlow, string warReadinessSignals)
        {
            ObjectiveBoard = objectiveBoard ?? string.Empty;
            ContributionLedger = contributionLedger ?? string.Empty;
            MissionCoordination = missionCoordination ?? string.Empty;
            ArmyCompositionPreview = armyCompositionPreview ?? string.Empty;
            RallyCommitment = rallyCommitment ?? string.Empty;
            LossBudget = lossBudget ?? string.Empty;
            AntiSnowballPolicy = antiSnowballPolicy ?? string.Empty;
            HelpRequestFlow = helpRequestFlow ?? string.Empty;
            WarReadinessSignals = warReadinessSignals ?? string.Empty;
        }

        public string ObjectiveBoard { get; }
        public string ContributionLedger { get; }
        public string MissionCoordination { get; }
        public string ArmyCompositionPreview { get; }
        public string RallyCommitment { get; }
        public string LossBudget { get; }
        public string AntiSnowballPolicy { get; }
        public string HelpRequestFlow { get; }
        public string WarReadinessSignals { get; }

        public bool HasMissingInput()
        {
            return string.IsNullOrWhiteSpace(ObjectiveBoard)
                || string.IsNullOrWhiteSpace(ContributionLedger)
                || string.IsNullOrWhiteSpace(MissionCoordination)
                || string.IsNullOrWhiteSpace(ArmyCompositionPreview)
                || string.IsNullOrWhiteSpace(RallyCommitment)
                || string.IsNullOrWhiteSpace(LossBudget)
                || string.IsNullOrWhiteSpace(AntiSnowballPolicy)
                || string.IsNullOrWhiteSpace(HelpRequestFlow)
                || string.IsNullOrWhiteSpace(WarReadinessSignals);
        }
    }

    public sealed class CooperativeCoverageMatrix
    {
        public CooperativeCoverageMatrix(bool objectiveGapOpen, bool demoEvidencePresent)
        {
            ObjectiveGapOpen = objectiveGapOpen;
            DemoEvidencePresent = demoEvidencePresent;
        }

        public bool ObjectiveGapOpen { get; }
        public bool DemoEvidencePresent { get; }
    }

    public sealed class PvPFairnessCoverageMatrix
    {
        public PvPFairnessCoverageMatrix(bool fairnessGapOpen)
        {
            FairnessGapOpen = fairnessGapOpen;
        }

        public bool FairnessGapOpen { get; }
    }

    public sealed class PlayerProtectionReadinessMatrix
    {
        public PlayerProtectionReadinessMatrix(bool recoveryGapOpen)
        {
            RecoveryGapOpen = recoveryGapOpen;
        }

        public bool RecoveryGapOpen { get; }
    }

    public sealed class CooperativePvPRiskRegister
    {
        public CooperativePvPRiskRegister(IReadOnlyList<string> risks, bool warReadinessGapOpen)
        {
            Risks = risks ?? Array.Empty<string>();
            WarReadinessGapOpen = warReadinessGapOpen;
        }

        public IReadOnlyList<string> Risks { get; }
        public bool WarReadinessGapOpen { get; }
    }

    public sealed class Bee341BlockerStatus
    {
        public Bee341BlockerStatus(bool prematureAttempt, string message)
        {
            PrematureAttempt = prematureAttempt;
            Message = message ?? string.Empty;
        }

        public bool PrematureAttempt { get; }
        public string Message { get; }
    }

    public sealed class CooperativePvPReadinessVerdict
    {
        public CooperativePvPReadinessVerdict(CooperativePvPReadinessVerdictType verdictType, IReadOnlyList<CooperativePvPReadinessDiagnosticCode> diagnostics)
        {
            VerdictType = verdictType;
            Diagnostics = diagnostics ?? Array.Empty<CooperativePvPReadinessDiagnosticCode>();
        }

        public CooperativePvPReadinessVerdictType VerdictType { get; }
        public IReadOnlyList<CooperativePvPReadinessDiagnosticCode> Diagnostics { get; }
        public bool Contains(CooperativePvPReadinessDiagnosticCode code) { return Diagnostics.Contains(code); }
    }

    public sealed class CooperativePvPReadinessGate
    {
        public const string Bee341BlockedMessage = "BEE-341 bloquee jusqu'a validation architecte.";

        public CooperativePvPReadinessGate(string gateId, CooperativePvPInputSet inputSet, CooperativeCoverageMatrix cooperativeCoverage, PvPFairnessCoverageMatrix fairnessCoverage, PlayerProtectionReadinessMatrix playerProtectionCoverage, CooperativePvPRiskRegister riskRegister, Bee341BlockerStatus bee341Status)
        {
            GateId = ColonyIntegrationIds.Require(gateId);
            InputSet = inputSet;
            CooperativeCoverage = cooperativeCoverage;
            FairnessCoverage = fairnessCoverage;
            PlayerProtectionCoverage = playerProtectionCoverage;
            RiskRegister = riskRegister;
            Bee341Status = bee341Status;
        }

        public string GateId { get; }
        public CooperativePvPInputSet InputSet { get; }
        public CooperativeCoverageMatrix CooperativeCoverage { get; }
        public PvPFairnessCoverageMatrix FairnessCoverage { get; }
        public PlayerProtectionReadinessMatrix PlayerProtectionCoverage { get; }
        public CooperativePvPRiskRegister RiskRegister { get; }
        public Bee341BlockerStatus Bee341Status { get; }

        public CooperativePvPReadinessVerdict Evaluate()
        {
            var diagnostics = BuildDiagnostics();
            return new CooperativePvPReadinessVerdict(ResolveVerdict(diagnostics), diagnostics);
        }

        private IReadOnlyList<CooperativePvPReadinessDiagnosticCode> BuildDiagnostics()
        {
            var diagnostics = new List<CooperativePvPReadinessDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput()) diagnostics.Add(CooperativePvPReadinessDiagnosticCode.CooperativePvPInputMissing);
            if (CooperativeCoverage == null || CooperativeCoverage.ObjectiveGapOpen) diagnostics.Add(CooperativePvPReadinessDiagnosticCode.CooperativeObjectiveGapOpen);
            if (FairnessCoverage == null || FairnessCoverage.FairnessGapOpen) diagnostics.Add(CooperativePvPReadinessDiagnosticCode.PvPFairnessGapOpen);
            if (PlayerProtectionCoverage == null || PlayerProtectionCoverage.RecoveryGapOpen) diagnostics.Add(CooperativePvPReadinessDiagnosticCode.PlayerRecoveryGapOpen);
            if (RiskRegister == null || RiskRegister.WarReadinessGapOpen) diagnostics.Add(CooperativePvPReadinessDiagnosticCode.WarReadinessGapOpen);
            if (Bee341Status != null && Bee341Status.PrematureAttempt) diagnostics.Add(CooperativePvPReadinessDiagnosticCode.Bee341Premature);
            return diagnostics;
        }

        private CooperativePvPReadinessVerdictType ResolveVerdict(IReadOnlyList<CooperativePvPReadinessDiagnosticCode> diagnostics)
        {
            if (diagnostics.Contains(CooperativePvPReadinessDiagnosticCode.Bee341Premature)) return CooperativePvPReadinessVerdictType.BlockedByBee341Premature;
            if (diagnostics.Contains(CooperativePvPReadinessDiagnosticCode.CooperativePvPInputMissing)) return CooperativePvPReadinessVerdictType.BlockedByMissingCooperationInput;
            if (diagnostics.Contains(CooperativePvPReadinessDiagnosticCode.PvPFairnessGapOpen)) return CooperativePvPReadinessVerdictType.BlockedByPvPFairnessGap;
            if (diagnostics.Contains(CooperativePvPReadinessDiagnosticCode.PlayerRecoveryGapOpen)) return CooperativePvPReadinessVerdictType.BlockedByPlayerProtectionGap;
            if (diagnostics.Contains(CooperativePvPReadinessDiagnosticCode.CooperativeObjectiveGapOpen) || diagnostics.Contains(CooperativePvPReadinessDiagnosticCode.WarReadinessGapOpen)) return CooperativePvPReadinessVerdictType.ReadyWithWarnings;
            if (CooperativeCoverage == null || !CooperativeCoverage.DemoEvidencePresent) return CooperativePvPReadinessVerdictType.BlockedByDemoEvidenceGap;
            return CooperativePvPReadinessVerdictType.ReadyForArchitectValidation;
        }
    }
}
