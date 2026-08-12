using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum SocialServerReadinessStatus { WaitingForBeeServerScan, ReadyForServerReview, BlockedByMissingOwner, RuntimeForbidden, ServerSpecCreationForbidden }
    public enum SocialServerImpactDiagnosticCode { SocialServerTopicMissing, SocialServerScanPending, SocialAuthorityReasonMissing, SocialRuntimeImplementationForbidden, ServerSpecCreationForbidden }

    public sealed class SocialServerAuthorityReason
    {
        public SocialServerAuthorityReason(string reasonId, string description)
        {
            ReasonId = reasonId ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string ReasonId { get; }
        public string Description { get; }
    }

    public sealed class SocialServerImpactRisk
    {
        public SocialServerImpactRisk(string riskId, string playerRisk, bool open)
        {
            RiskId = riskId ?? string.Empty;
            PlayerRisk = playerRisk ?? string.Empty;
            Open = open;
        }

        public string RiskId { get; }
        public string PlayerRisk { get; }
        public bool Open { get; }
    }

    public sealed class SocialServerImpactTopic
    {
        public SocialServerImpactTopic(string topicId, string sourceBee, IReadOnlyList<SocialMmoProductPillar> productPillars, SocialServerAuthorityReason authorityReason, SocialServerImpactRisk playerRisk, SocialServerReadinessStatus readinessStatus, string demoVisibility, string qaConcern)
        {
            TopicId = topicId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            ProductPillars = productPillars ?? Array.Empty<SocialMmoProductPillar>();
            AuthorityReason = authorityReason;
            PlayerRisk = playerRisk;
            ReadinessStatus = readinessStatus;
            DemoVisibility = demoVisibility ?? string.Empty;
            QaConcern = qaConcern ?? string.Empty;
        }

        public string TopicId { get; }
        public string SourceBee { get; }
        public IReadOnlyList<SocialMmoProductPillar> ProductPillars { get; }
        public SocialServerAuthorityReason AuthorityReason { get; }
        public SocialServerImpactRisk PlayerRisk { get; }
        public SocialServerReadinessStatus ReadinessStatus { get; }
        public string DemoVisibility { get; }
        public string QaConcern { get; }
    }

    public sealed class SocialServerIntakeExport
    {
        public SocialServerIntakeExport(string exportId, IReadOnlyList<SocialServerImpactTopic> topics, string limitation)
        {
            ExportId = exportId ?? string.Empty;
            Topics = topics ?? Array.Empty<SocialServerImpactTopic>();
            Limitation = limitation ?? string.Empty;
        }

        public string ExportId { get; }
        public IReadOnlyList<SocialServerImpactTopic> Topics { get; }
        public string Limitation { get; }
    }

    public sealed class SocialServerImpactIntake
    {
        public SocialServerImpactIntake(string intakeId, IReadOnlyList<SocialServerImpactTopic> topics, bool beeServerScanPending, bool runtimeImplementationRequested, bool serverSpecCreationRequested)
        {
            IntakeId = ColonyIntegrationIds.Require(intakeId);
            Topics = topics ?? Array.Empty<SocialServerImpactTopic>();
            BeeServerScanPending = beeServerScanPending;
            RuntimeImplementationRequested = runtimeImplementationRequested;
            ServerSpecCreationRequested = serverSpecCreationRequested;
        }

        public string IntakeId { get; }
        public IReadOnlyList<SocialServerImpactTopic> Topics { get; }
        public bool BeeServerScanPending { get; }
        public bool RuntimeImplementationRequested { get; }
        public bool ServerSpecCreationRequested { get; }

        public SocialServerImpactDiagnostics Evaluate()
        {
            var findings = new List<SocialServerImpactDiagnosticCode>();
            if (Topics.Count == 0 || Topics.Any(t => string.IsNullOrWhiteSpace(t.TopicId) || string.IsNullOrWhiteSpace(t.SourceBee))) findings.Add(SocialServerImpactDiagnosticCode.SocialServerTopicMissing);
            if (BeeServerScanPending || Topics.Any(t => t.ReadinessStatus == SocialServerReadinessStatus.WaitingForBeeServerScan)) findings.Add(SocialServerImpactDiagnosticCode.SocialServerScanPending);
            if (Topics.Any(t => t.AuthorityReason == null || string.IsNullOrWhiteSpace(t.AuthorityReason.ReasonId))) findings.Add(SocialServerImpactDiagnosticCode.SocialAuthorityReasonMissing);
            if (RuntimeImplementationRequested || Topics.Any(t => t.ReadinessStatus == SocialServerReadinessStatus.RuntimeForbidden)) findings.Add(SocialServerImpactDiagnosticCode.SocialRuntimeImplementationForbidden);
            if (ServerSpecCreationRequested || Topics.Any(t => t.ReadinessStatus == SocialServerReadinessStatus.ServerSpecCreationForbidden)) findings.Add(SocialServerImpactDiagnosticCode.ServerSpecCreationForbidden);
            return new SocialServerImpactDiagnostics(findings);
        }

        public SocialServerIntakeExport Export(string exportId)
        {
            return new SocialServerIntakeExport(exportId, Topics, "Read-only Unity intake. Bee Server must scan before backend work.");
        }
    }

    public sealed class SocialServerImpactDiagnostics
    {
        public SocialServerImpactDiagnostics(IReadOnlyList<SocialServerImpactDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialServerImpactDiagnosticCode>(); }
        public IReadOnlyList<SocialServerImpactDiagnosticCode> Findings { get; }
        public bool Contains(SocialServerImpactDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AlliancePersistenceDiagnosticCode { AlliancePersistentFieldUnowned, AllianceLocalPersistenceForbidden, AllianceOfficialIdentityServerRequired, AllianceAuditHistoryServerRequired, AlliancePersistenceVersionMissing }

    public sealed class AllianceOfficialIdentityCandidate
    {
        public AllianceOfficialIdentityCandidate(string fieldId, string serverOwner, bool serverAuthoritative)
        {
            FieldId = fieldId ?? string.Empty;
            ServerOwner = serverOwner ?? string.Empty;
            ServerAuthoritative = serverAuthoritative;
        }

        public string FieldId { get; }
        public string ServerOwner { get; }
        public bool ServerAuthoritative { get; }
    }

    public sealed class AlliancePersistentMemberRecordCandidate
    {
        public AlliancePersistentMemberRecordCandidate(string memberFieldId, bool serverAuthoritative)
        {
            MemberFieldId = memberFieldId ?? string.Empty;
            ServerAuthoritative = serverAuthoritative;
        }

        public string MemberFieldId { get; }
        public bool ServerAuthoritative { get; }
    }

    public sealed class AlliancePersistentRoleRecordCandidate
    {
        public AlliancePersistentRoleRecordCandidate(string roleFieldId, bool serverAuthoritative)
        {
            RoleFieldId = roleFieldId ?? string.Empty;
            ServerAuthoritative = serverAuthoritative;
        }

        public string RoleFieldId { get; }
        public bool ServerAuthoritative { get; }
    }

    public sealed class AllianceAuditHistoryCandidate
    {
        public AllianceAuditHistoryCandidate(string auditId, bool serverRequired)
        {
            AuditId = auditId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string AuditId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class AlliancePersistenceAuthorityGap
    {
        public AlliancePersistenceAuthorityGap(string gapId, string owner, string description)
        {
            GapId = gapId ?? string.Empty;
            Owner = owner ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string GapId { get; }
        public string Owner { get; }
        public string Description { get; }
    }

    public sealed class AlliancePersistenceBoundaryContract
    {
        public AlliancePersistenceBoundaryContract(string allianceProjectionId, IReadOnlyList<AllianceOfficialIdentityCandidate> officialFields, IReadOnlyList<string> localProjectionFields, IReadOnlyList<AlliancePersistentMemberRecordCandidate> memberRecordCandidates, IReadOnlyList<AlliancePersistentRoleRecordCandidate> roleRecordCandidates, IReadOnlyList<AllianceAuditHistoryCandidate> auditRequirements, IReadOnlyList<AlliancePersistenceAuthorityGap> serverAuthorityGaps, string contractVersion, bool localPersistenceRequested, bool persistenceAllowed)
        {
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            OfficialFields = officialFields ?? Array.Empty<AllianceOfficialIdentityCandidate>();
            LocalProjectionFields = localProjectionFields ?? Array.Empty<string>();
            MemberRecordCandidates = memberRecordCandidates ?? Array.Empty<AlliancePersistentMemberRecordCandidate>();
            RoleRecordCandidates = roleRecordCandidates ?? Array.Empty<AlliancePersistentRoleRecordCandidate>();
            AuditRequirements = auditRequirements ?? Array.Empty<AllianceAuditHistoryCandidate>();
            ServerAuthorityGaps = serverAuthorityGaps ?? Array.Empty<AlliancePersistenceAuthorityGap>();
            ContractVersion = contractVersion ?? string.Empty;
            LocalPersistenceRequested = localPersistenceRequested;
            PersistenceAllowed = persistenceAllowed;
        }

        public string AllianceProjectionId { get; }
        public IReadOnlyList<AllianceOfficialIdentityCandidate> OfficialFields { get; }
        public IReadOnlyList<string> LocalProjectionFields { get; }
        public IReadOnlyList<AlliancePersistentMemberRecordCandidate> MemberRecordCandidates { get; }
        public IReadOnlyList<AlliancePersistentRoleRecordCandidate> RoleRecordCandidates { get; }
        public IReadOnlyList<AllianceAuditHistoryCandidate> AuditRequirements { get; }
        public IReadOnlyList<AlliancePersistenceAuthorityGap> ServerAuthorityGaps { get; }
        public string ContractVersion { get; }
        public bool LocalPersistenceRequested { get; }
        public bool PersistenceAllowed { get; }

        public AlliancePersistenceDiagnostics Evaluate()
        {
            var findings = new List<AlliancePersistenceDiagnosticCode>();
            if (OfficialFields.Any(f => !f.ServerAuthoritative || string.IsNullOrWhiteSpace(f.ServerOwner)) || ServerAuthorityGaps.Any(g => string.IsNullOrWhiteSpace(g.Owner))) findings.Add(AlliancePersistenceDiagnosticCode.AlliancePersistentFieldUnowned);
            if (LocalPersistenceRequested || PersistenceAllowed) findings.Add(AlliancePersistenceDiagnosticCode.AllianceLocalPersistenceForbidden);
            if (OfficialFields.Count == 0 || OfficialFields.Any(f => string.IsNullOrWhiteSpace(f.FieldId))) findings.Add(AlliancePersistenceDiagnosticCode.AllianceOfficialIdentityServerRequired);
            if (AuditRequirements.Count == 0 || AuditRequirements.Any(a => a.ServerRequired)) findings.Add(AlliancePersistenceDiagnosticCode.AllianceAuditHistoryServerRequired);
            if (string.IsNullOrWhiteSpace(ContractVersion)) findings.Add(AlliancePersistenceDiagnosticCode.AlliancePersistenceVersionMissing);
            return new AlliancePersistenceDiagnostics(findings);
        }
    }

    public sealed class AlliancePersistenceDiagnostics
    {
        public AlliancePersistenceDiagnostics(IReadOnlyList<AlliancePersistenceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AlliancePersistenceDiagnosticCode>(); }
        public IReadOnlyList<AlliancePersistenceDiagnosticCode> Findings { get; }
        public bool Contains(AlliancePersistenceDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceAbuseAuditVerdict { WarningProjection, BlockedProjection, EscalateToServer, EvidenceMissing, MutationForbidden }
    public enum AlliancePermissionAbuseDiagnosticCode { SensitiveAlliancePermissionUnaudited, AllianceAbuseEvidenceMissing, AllianceAbuseVictimScopeMissing, AllianceAbuseServerOwnerMissing, AlliancePermissionMutationForbidden }

    public sealed class AllianceSensitivePermission
    {
        public AllianceSensitivePermission(string permissionId, AllianceRoleKind actorRole, bool audited)
        {
            PermissionId = permissionId ?? string.Empty;
            ActorRole = actorRole;
            Audited = audited;
        }

        public string PermissionId { get; }
        public AllianceRoleKind ActorRole { get; }
        public bool Audited { get; }
    }

    public sealed class AllianceAbuseEvidenceRequirement
    {
        public AllianceAbuseEvidenceRequirement(string evidenceId, bool missing)
        {
            EvidenceId = evidenceId ?? string.Empty;
            Missing = missing;
        }

        public string EvidenceId { get; }
        public bool Missing { get; }
    }

    public sealed class AllianceAbuseServerOwner
    {
        public AllianceAbuseServerOwner(string ownerId, string responsibility)
        {
            OwnerId = ownerId ?? string.Empty;
            Responsibility = responsibility ?? string.Empty;
        }

        public string OwnerId { get; }
        public string Responsibility { get; }
    }

    public sealed class AllianceAbuseScenario
    {
        public AllianceAbuseScenario(string scenarioId, AllianceSensitivePermission sensitivePermission, IReadOnlyList<string> affectedPlayers, string abuseType, AllianceAbuseEvidenceRequirement evidenceRequirement, AllianceAbuseServerOwner serverOwner, AllianceAbuseAuditVerdict auditVerdict, bool permissionMutationRequested)
        {
            ScenarioId = scenarioId ?? string.Empty;
            SensitivePermission = sensitivePermission;
            AffectedPlayers = affectedPlayers ?? Array.Empty<string>();
            AbuseType = abuseType ?? string.Empty;
            EvidenceRequirement = evidenceRequirement;
            ServerOwner = serverOwner;
            AuditVerdict = auditVerdict;
            PermissionMutationRequested = permissionMutationRequested;
        }

        public string ScenarioId { get; }
        public AllianceSensitivePermission SensitivePermission { get; }
        public IReadOnlyList<string> AffectedPlayers { get; }
        public string AbuseType { get; }
        public AllianceAbuseEvidenceRequirement EvidenceRequirement { get; }
        public AllianceAbuseServerOwner ServerOwner { get; }
        public AllianceAbuseAuditVerdict AuditVerdict { get; }
        public bool PermissionMutationRequested { get; }
    }

    public sealed class AlliancePermissionAbuseAudit
    {
        public AlliancePermissionAbuseAudit(string auditId, IReadOnlyList<AllianceAbuseScenario> scenarios)
        {
            AuditId = ColonyIntegrationIds.Require(auditId);
            Scenarios = scenarios ?? Array.Empty<AllianceAbuseScenario>();
        }

        public string AuditId { get; }
        public IReadOnlyList<AllianceAbuseScenario> Scenarios { get; }

        public AlliancePermissionAbuseDiagnostics Evaluate()
        {
            var findings = new List<AlliancePermissionAbuseDiagnosticCode>();
            if (Scenarios.Count == 0 || Scenarios.Any(s => s.SensitivePermission == null || !s.SensitivePermission.Audited)) findings.Add(AlliancePermissionAbuseDiagnosticCode.SensitiveAlliancePermissionUnaudited);
            if (Scenarios.Any(s => s.EvidenceRequirement == null || s.EvidenceRequirement.Missing)) findings.Add(AlliancePermissionAbuseDiagnosticCode.AllianceAbuseEvidenceMissing);
            if (Scenarios.Any(s => s.AffectedPlayers.Count == 0)) findings.Add(AlliancePermissionAbuseDiagnosticCode.AllianceAbuseVictimScopeMissing);
            if (Scenarios.Any(s => s.ServerOwner == null || string.IsNullOrWhiteSpace(s.ServerOwner.OwnerId))) findings.Add(AlliancePermissionAbuseDiagnosticCode.AllianceAbuseServerOwnerMissing);
            if (Scenarios.Any(s => s.PermissionMutationRequested || s.AuditVerdict == AllianceAbuseAuditVerdict.MutationForbidden)) findings.Add(AlliancePermissionAbuseDiagnosticCode.AlliancePermissionMutationForbidden);
            return new AlliancePermissionAbuseDiagnostics(findings);
        }
    }

    public sealed class AlliancePermissionAbuseDiagnostics
    {
        public AlliancePermissionAbuseDiagnostics(IReadOnlyList<AlliancePermissionAbuseDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AlliancePermissionAbuseDiagnosticCode>(); }
        public IReadOnlyList<AlliancePermissionAbuseDiagnosticCode> Findings { get; }
        public bool Contains(AlliancePermissionAbuseDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum DiplomacyNegotiationState { Draft, Proposed, CounterProposed, AcceptedProjected, RejectedProjected, ExpiredProjected, WithdrawnProjected, BetrayalRiskFlagged, ServerAuthorityRequired }
    public enum DiplomacyNegotiationDiagnosticCode { DiplomacyNegotiationPermissionMissing, DiplomacyOfferInvalid, DiplomacyCounterOfferConflict, DiplomacyNegotiationExpiredProjected, DiplomacyOfficialAcceptanceForbidden, DiplomacyBetrayalRiskOpen }

    public sealed class DiplomacyOfferProjection
    {
        public DiplomacyOfferProjection(string offerId, DiplomacyRelationshipContractType requestedRelationshipType, bool valid)
        {
            OfferId = offerId ?? string.Empty;
            RequestedRelationshipType = requestedRelationshipType;
            Valid = valid;
        }

        public string OfferId { get; }
        public DiplomacyRelationshipContractType RequestedRelationshipType { get; }
        public bool Valid { get; }
    }

    public sealed class DiplomacyCounterOfferProjection
    {
        public DiplomacyCounterOfferProjection(string counterOfferId, bool conflict)
        {
            CounterOfferId = counterOfferId ?? string.Empty;
            Conflict = conflict;
        }

        public string CounterOfferId { get; }
        public bool Conflict { get; }
    }

    public sealed class DiplomacyNegotiationExpiryProjection
    {
        public DiplomacyNegotiationExpiryProjection(bool expiredProjected, string reason)
        {
            ExpiredProjected = expiredProjected;
            Reason = reason ?? string.Empty;
        }

        public bool ExpiredProjected { get; }
        public string Reason { get; }
    }

    public sealed class DiplomacyBetrayalFlag
    {
        public DiplomacyBetrayalFlag(string flagId, bool open)
        {
            FlagId = flagId ?? string.Empty;
            Open = open;
        }

        public string FlagId { get; }
        public bool Open { get; }
    }

    public sealed class DiplomacyNegotiationFlowContract
    {
        public DiplomacyNegotiationFlowContract(string negotiationId, string sourceAllianceProjectionId, string targetAllianceProjectionId, DiplomacyRelationshipContractType requestedRelationshipType, DiplomacyNegotiationState currentState, IReadOnlyList<DiplomacyOfferProjection> offers, IReadOnlyList<DiplomacyCounterOfferProjection> counterOffers, DiplomacyNegotiationExpiryProjection expiryProjection, IReadOnlyList<DiplomacyBetrayalFlag> betrayalFlags, IReadOnlyList<string> serverAuthorityTopics, bool permissionPresent, bool officialAcceptanceRequested)
        {
            NegotiationId = ColonyIntegrationIds.Require(negotiationId);
            SourceAllianceProjectionId = sourceAllianceProjectionId ?? string.Empty;
            TargetAllianceProjectionId = targetAllianceProjectionId ?? string.Empty;
            RequestedRelationshipType = requestedRelationshipType;
            CurrentState = currentState;
            Offers = offers ?? Array.Empty<DiplomacyOfferProjection>();
            CounterOffers = counterOffers ?? Array.Empty<DiplomacyCounterOfferProjection>();
            ExpiryProjection = expiryProjection;
            BetrayalFlags = betrayalFlags ?? Array.Empty<DiplomacyBetrayalFlag>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
            PermissionPresent = permissionPresent;
            OfficialAcceptanceRequested = officialAcceptanceRequested;
        }

        public string NegotiationId { get; }
        public string SourceAllianceProjectionId { get; }
        public string TargetAllianceProjectionId { get; }
        public DiplomacyRelationshipContractType RequestedRelationshipType { get; }
        public DiplomacyNegotiationState CurrentState { get; }
        public IReadOnlyList<DiplomacyOfferProjection> Offers { get; }
        public IReadOnlyList<DiplomacyCounterOfferProjection> CounterOffers { get; }
        public DiplomacyNegotiationExpiryProjection ExpiryProjection { get; }
        public IReadOnlyList<DiplomacyBetrayalFlag> BetrayalFlags { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }
        public bool PermissionPresent { get; }
        public bool OfficialAcceptanceRequested { get; }

        public DiplomacyNegotiationDiagnostics Evaluate()
        {
            var findings = new List<DiplomacyNegotiationDiagnosticCode>();
            if (!PermissionPresent) findings.Add(DiplomacyNegotiationDiagnosticCode.DiplomacyNegotiationPermissionMissing);
            if (Offers.Count == 0 || Offers.Any(o => !o.Valid || string.IsNullOrWhiteSpace(o.OfferId))) findings.Add(DiplomacyNegotiationDiagnosticCode.DiplomacyOfferInvalid);
            if (CounterOffers.Any(c => c.Conflict)) findings.Add(DiplomacyNegotiationDiagnosticCode.DiplomacyCounterOfferConflict);
            if ((ExpiryProjection != null && ExpiryProjection.ExpiredProjected) || CurrentState == DiplomacyNegotiationState.ExpiredProjected) findings.Add(DiplomacyNegotiationDiagnosticCode.DiplomacyNegotiationExpiredProjected);
            if (OfficialAcceptanceRequested || CurrentState == DiplomacyNegotiationState.ServerAuthorityRequired) findings.Add(DiplomacyNegotiationDiagnosticCode.DiplomacyOfficialAcceptanceForbidden);
            if (BetrayalFlags.Any(b => b.Open) || CurrentState == DiplomacyNegotiationState.BetrayalRiskFlagged) findings.Add(DiplomacyNegotiationDiagnosticCode.DiplomacyBetrayalRiskOpen);
            return new DiplomacyNegotiationDiagnostics(findings);
        }
    }

    public sealed class DiplomacyNegotiationDiagnostics
    {
        public DiplomacyNegotiationDiagnostics(IReadOnlyList<DiplomacyNegotiationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DiplomacyNegotiationDiagnosticCode>(); }
        public IReadOnlyList<DiplomacyNegotiationDiagnosticCode> Findings { get; }
        public bool Contains(DiplomacyNegotiationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum WarRallyReadinessVerdict { ProjectionReady, BlockedByPermission, BlockedByTarget, BlockedByWindow, BlockedByProtection, RuntimeMobilizationForbidden, ServerAuthorityRequired }
    public enum WarRallyPlanningDiagnosticCode { WarRallyPermissionMissing, WarRallyTargetInvalid, WarRallyWindowConflict, WarRallyBeginnerProtectionBlocks, WarRallyRuntimeMobilizationForbidden, WarRallyServerAuthorityRequired }

    public sealed class WarRallyParticipantProjection
    {
        public WarRallyParticipantProjection(string playerHiveIdentityId, bool readyProjected)
        {
            PlayerHiveIdentityId = playerHiveIdentityId ?? string.Empty;
            ReadyProjected = readyProjected;
        }

        public string PlayerHiveIdentityId { get; }
        public bool ReadyProjected { get; }
    }

    public sealed class WarRallyWindowProjection
    {
        public WarRallyWindowProjection(string windowId, bool conflict)
        {
            WindowId = windowId ?? string.Empty;
            Conflict = conflict;
        }

        public string WindowId { get; }
        public bool Conflict { get; }
    }

    public sealed class WarRallyTargetProjection
    {
        public WarRallyTargetProjection(string targetProjectionId, bool valid)
        {
            TargetProjectionId = targetProjectionId ?? string.Empty;
            Valid = valid;
        }

        public string TargetProjectionId { get; }
        public bool Valid { get; }
    }

    public sealed class WarRallyProtectionCheck
    {
        public WarRallyProtectionCheck(bool beginnerProtectionBlocks, string reason)
        {
            BeginnerProtectionBlocks = beginnerProtectionBlocks;
            Reason = reason ?? string.Empty;
        }

        public bool BeginnerProtectionBlocks { get; }
        public string Reason { get; }
    }

    public sealed class WarRallyPlanningBoundary
    {
        public WarRallyPlanningBoundary(string rallyId, string sourceAllianceProjectionId, WarRallyTargetProjection targetProjection, AllianceRoleKind creatorRole, IReadOnlyList<WarRallyParticipantProjection> participants, WarRallyWindowProjection rallyWindow, IReadOnlyList<WarRallyProtectionCheck> protectionChecks, WarRallyReadinessVerdict readinessVerdict, bool permissionPresent, bool runtimeMobilizationRequested, bool serverAuthorityRequired)
        {
            RallyId = ColonyIntegrationIds.Require(rallyId);
            SourceAllianceProjectionId = sourceAllianceProjectionId ?? string.Empty;
            TargetProjection = targetProjection;
            CreatorRole = creatorRole;
            Participants = participants ?? Array.Empty<WarRallyParticipantProjection>();
            RallyWindow = rallyWindow;
            ProtectionChecks = protectionChecks ?? Array.Empty<WarRallyProtectionCheck>();
            ReadinessVerdict = readinessVerdict;
            PermissionPresent = permissionPresent;
            RuntimeMobilizationRequested = runtimeMobilizationRequested;
            ServerAuthorityRequired = serverAuthorityRequired;
        }

        public string RallyId { get; }
        public string SourceAllianceProjectionId { get; }
        public WarRallyTargetProjection TargetProjection { get; }
        public AllianceRoleKind CreatorRole { get; }
        public IReadOnlyList<WarRallyParticipantProjection> Participants { get; }
        public WarRallyWindowProjection RallyWindow { get; }
        public IReadOnlyList<WarRallyProtectionCheck> ProtectionChecks { get; }
        public WarRallyReadinessVerdict ReadinessVerdict { get; }
        public bool PermissionPresent { get; }
        public bool RuntimeMobilizationRequested { get; }
        public bool ServerAuthorityRequired { get; }

        public WarRallyPlanningDiagnostics Evaluate()
        {
            var findings = new List<WarRallyPlanningDiagnosticCode>();
            if (!PermissionPresent) findings.Add(WarRallyPlanningDiagnosticCode.WarRallyPermissionMissing);
            if (TargetProjection == null || !TargetProjection.Valid || string.IsNullOrWhiteSpace(TargetProjection.TargetProjectionId)) findings.Add(WarRallyPlanningDiagnosticCode.WarRallyTargetInvalid);
            if (RallyWindow == null || RallyWindow.Conflict) findings.Add(WarRallyPlanningDiagnosticCode.WarRallyWindowConflict);
            if (ProtectionChecks.Any(p => p.BeginnerProtectionBlocks)) findings.Add(WarRallyPlanningDiagnosticCode.WarRallyBeginnerProtectionBlocks);
            if (RuntimeMobilizationRequested || ReadinessVerdict == WarRallyReadinessVerdict.RuntimeMobilizationForbidden) findings.Add(WarRallyPlanningDiagnosticCode.WarRallyRuntimeMobilizationForbidden);
            if (ServerAuthorityRequired || ReadinessVerdict == WarRallyReadinessVerdict.ServerAuthorityRequired) findings.Add(WarRallyPlanningDiagnosticCode.WarRallyServerAuthorityRequired);
            return new WarRallyPlanningDiagnostics(findings);
        }
    }

    public sealed class WarRallyPlanningDiagnostics
    {
        public WarRallyPlanningDiagnostics(IReadOnlyList<WarRallyPlanningDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WarRallyPlanningDiagnosticCode>(); }
        public IReadOnlyList<WarRallyPlanningDiagnosticCode> Findings { get; }
        public bool Contains(WarRallyPlanningDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ArmyTrainingQueueDiagnosticCode { ArmyTrainingQueueRuntimeForbidden, ArmyPersistentUnitForbidden, ArmyTrainingCostNonFinal, ArmyCapacityConstraintMissing, ArmyPayToWinRiskOpen, ArmyServerAuthorityRequired }

    public sealed class ArmyTrainingSlotProjection
    {
        public ArmyTrainingSlotProjection(string slotId, ArmyTrainingLifecycleStage stage, bool runtimeQueueRequested, bool persistentUnitRequested)
        {
            SlotId = slotId ?? string.Empty;
            Stage = stage;
            RuntimeQueueRequested = runtimeQueueRequested;
            PersistentUnitRequested = persistentUnitRequested;
        }

        public string SlotId { get; }
        public ArmyTrainingLifecycleStage Stage { get; }
        public bool RuntimeQueueRequested { get; }
        public bool PersistentUnitRequested { get; }
    }

    public sealed class ArmyTrainingCostProjection
    {
        public ArmyTrainingCostProjection(string costId, bool nonFinalBalance)
        {
            CostId = costId ?? string.Empty;
            NonFinalBalance = nonFinalBalance;
        }

        public string CostId { get; }
        public bool NonFinalBalance { get; }
    }

    public sealed class ArmyTrainingCapacityConstraint
    {
        public ArmyTrainingCapacityConstraint(string constraintId, bool missing)
        {
            ConstraintId = constraintId ?? string.Empty;
            Missing = missing;
        }

        public string ConstraintId { get; }
        public bool Missing { get; }
    }

    public sealed class ArmyReadinessSocialSignal
    {
        public ArmyReadinessSocialSignal(string signalId, double value)
        {
            SignalId = signalId ?? string.Empty;
            Value = ColonyIntegrationIds.Clamp01(value);
        }

        public string SignalId { get; }
        public double Value { get; }
    }

    public sealed class ArmyTrainingBalanceRisk
    {
        public ArmyTrainingBalanceRisk(string riskId, bool payToWinOpen)
        {
            RiskId = riskId ?? string.Empty;
            PayToWinOpen = payToWinOpen;
        }

        public string RiskId { get; }
        public bool PayToWinOpen { get; }
    }

    public sealed class ArmyTrainingQueueReadinessProjection
    {
        public ArmyTrainingQueueReadinessProjection(string playerHiveIdentityId, IReadOnlyList<ArmyTrainingSlotProjection> queueSlots, IReadOnlyList<ArmyTrainingCapacityConstraint> capacityConstraints, IReadOnlyList<ArmyTrainingCostProjection> costProjections, IReadOnlyList<ArmyReadinessSocialSignal> socialReadinessSignals, IReadOnlyList<ArmyTrainingBalanceRisk> balanceRisks, IReadOnlyList<string> serverAuthorityTopics)
        {
            PlayerHiveIdentityId = playerHiveIdentityId ?? string.Empty;
            QueueSlots = queueSlots ?? Array.Empty<ArmyTrainingSlotProjection>();
            CapacityConstraints = capacityConstraints ?? Array.Empty<ArmyTrainingCapacityConstraint>();
            CostProjections = costProjections ?? Array.Empty<ArmyTrainingCostProjection>();
            SocialReadinessSignals = socialReadinessSignals ?? Array.Empty<ArmyReadinessSocialSignal>();
            BalanceRisks = balanceRisks ?? Array.Empty<ArmyTrainingBalanceRisk>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
        }

        public string PlayerHiveIdentityId { get; }
        public IReadOnlyList<ArmyTrainingSlotProjection> QueueSlots { get; }
        public IReadOnlyList<ArmyTrainingCapacityConstraint> CapacityConstraints { get; }
        public IReadOnlyList<ArmyTrainingCostProjection> CostProjections { get; }
        public IReadOnlyList<ArmyReadinessSocialSignal> SocialReadinessSignals { get; }
        public IReadOnlyList<ArmyTrainingBalanceRisk> BalanceRisks { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }

        public ArmyTrainingQueueDiagnostics Evaluate()
        {
            var findings = new List<ArmyTrainingQueueDiagnosticCode>();
            if (QueueSlots.Any(s => s.RuntimeQueueRequested)) findings.Add(ArmyTrainingQueueDiagnosticCode.ArmyTrainingQueueRuntimeForbidden);
            if (QueueSlots.Any(s => s.PersistentUnitRequested)) findings.Add(ArmyTrainingQueueDiagnosticCode.ArmyPersistentUnitForbidden);
            if (CostProjections.Count == 0 || CostProjections.Any(c => c.NonFinalBalance)) findings.Add(ArmyTrainingQueueDiagnosticCode.ArmyTrainingCostNonFinal);
            if (CapacityConstraints.Count == 0 || CapacityConstraints.Any(c => c.Missing)) findings.Add(ArmyTrainingQueueDiagnosticCode.ArmyCapacityConstraintMissing);
            if (BalanceRisks.Any(r => r.PayToWinOpen)) findings.Add(ArmyTrainingQueueDiagnosticCode.ArmyPayToWinRiskOpen);
            if (ServerAuthorityTopics.Count > 0) findings.Add(ArmyTrainingQueueDiagnosticCode.ArmyServerAuthorityRequired);
            return new ArmyTrainingQueueDiagnostics(findings);
        }
    }

    public sealed class ArmyTrainingQueueDiagnostics
    {
        public ArmyTrainingQueueDiagnostics(IReadOnlyList<ArmyTrainingQueueDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyTrainingQueueDiagnosticCode>(); }
        public IReadOnlyList<ArmyTrainingQueueDiagnosticCode> Findings { get; }
        public bool Contains(ArmyTrainingQueueDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum DefeatRecoveryDiagnosticCode { DefeatLossRuntimeForbidden, RecoveryProtectionMissing, RetentionRiskUnclassified, RepeatedAttackAfterDefeatRisk, CompensationServerAuthorityRequired, DefeatHistoryStorageForbidden }

    public sealed class ProjectedLossLimit
    {
        public ProjectedLossLimit(string limitId, bool runtimeLossRequested, bool nonFinal)
        {
            LimitId = limitId ?? string.Empty;
            RuntimeLossRequested = runtimeLossRequested;
            NonFinal = nonFinal;
        }

        public string LimitId { get; }
        public bool RuntimeLossRequested { get; }
        public bool NonFinal { get; }
    }

    public sealed class RecoveryAssistanceSignal
    {
        public RecoveryAssistanceSignal(string signalId, bool compensationServerRequired)
        {
            SignalId = signalId ?? string.Empty;
            CompensationServerRequired = compensationServerRequired;
        }

        public string SignalId { get; }
        public bool CompensationServerRequired { get; }
    }

    public sealed class PostDefeatVulnerabilityWindow
    {
        public PostDefeatVulnerabilityWindow(string windowId, bool repeatedAttackRisk)
        {
            WindowId = windowId ?? string.Empty;
            RepeatedAttackRisk = repeatedAttackRisk;
        }

        public string WindowId { get; }
        public bool RepeatedAttackRisk { get; }
    }

    public sealed class RetentionRiskIndicator
    {
        public RetentionRiskIndicator(string riskId, bool unclassified)
        {
            RiskId = riskId ?? string.Empty;
            Unclassified = unclassified;
        }

        public string RiskId { get; }
        public bool Unclassified { get; }
    }

    public sealed class DefeatRecoveryRetentionProtectionProjection
    {
        public DefeatRecoveryRetentionProtectionProjection(string playerHiveIdentityId, string defeatContextProjection, IReadOnlyList<ProjectedLossLimit> projectedLossLimits, PostDefeatVulnerabilityWindow vulnerabilityWindow, IReadOnlyList<RecoveryAssistanceSignal> recoverySignals, IReadOnlyList<RetentionRiskIndicator> retentionRisks, IReadOnlyList<string> serverAuthorityTopics, bool protectionMissing, bool defeatHistoryStorageRequested)
        {
            PlayerHiveIdentityId = playerHiveIdentityId ?? string.Empty;
            DefeatContextProjection = defeatContextProjection ?? string.Empty;
            ProjectedLossLimits = projectedLossLimits ?? Array.Empty<ProjectedLossLimit>();
            VulnerabilityWindow = vulnerabilityWindow;
            RecoverySignals = recoverySignals ?? Array.Empty<RecoveryAssistanceSignal>();
            RetentionRisks = retentionRisks ?? Array.Empty<RetentionRiskIndicator>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
            ProtectionMissing = protectionMissing;
            DefeatHistoryStorageRequested = defeatHistoryStorageRequested;
        }

        public string PlayerHiveIdentityId { get; }
        public string DefeatContextProjection { get; }
        public IReadOnlyList<ProjectedLossLimit> ProjectedLossLimits { get; }
        public PostDefeatVulnerabilityWindow VulnerabilityWindow { get; }
        public IReadOnlyList<RecoveryAssistanceSignal> RecoverySignals { get; }
        public IReadOnlyList<RetentionRiskIndicator> RetentionRisks { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }
        public bool ProtectionMissing { get; }
        public bool DefeatHistoryStorageRequested { get; }

        public DefeatRecoveryDiagnostics Evaluate()
        {
            var findings = new List<DefeatRecoveryDiagnosticCode>();
            if (ProjectedLossLimits.Any(l => l.RuntimeLossRequested)) findings.Add(DefeatRecoveryDiagnosticCode.DefeatLossRuntimeForbidden);
            if (ProtectionMissing || ProjectedLossLimits.Count == 0) findings.Add(DefeatRecoveryDiagnosticCode.RecoveryProtectionMissing);
            if (RetentionRisks.Count == 0 || RetentionRisks.Any(r => r.Unclassified)) findings.Add(DefeatRecoveryDiagnosticCode.RetentionRiskUnclassified);
            if (VulnerabilityWindow != null && VulnerabilityWindow.RepeatedAttackRisk) findings.Add(DefeatRecoveryDiagnosticCode.RepeatedAttackAfterDefeatRisk);
            if (RecoverySignals.Any(s => s.CompensationServerRequired) || ServerAuthorityTopics.Count > 0) findings.Add(DefeatRecoveryDiagnosticCode.CompensationServerAuthorityRequired);
            if (DefeatHistoryStorageRequested) findings.Add(DefeatRecoveryDiagnosticCode.DefeatHistoryStorageForbidden);
            return new DefeatRecoveryDiagnostics(findings);
        }
    }

    public sealed class DefeatRecoveryDiagnostics
    {
        public DefeatRecoveryDiagnostics(IReadOnlyList<DefeatRecoveryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DefeatRecoveryDiagnosticCode>(); }
        public IReadOnlyList<DefeatRecoveryDiagnosticCode> Findings { get; }
        public bool Contains(DefeatRecoveryDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ModerationReportType { Harassment, HateOrOffensiveName, Spam, AlliancePermissionAbuse, WarHarassment, DiplomacyScam, TreasuryCoercion, Missing }
    public enum ModerationReportStatusProjection { DraftReport, SubmittedProjection, EvidenceMissing, NeedsServerReview, RejectedProjection, SanctionForbiddenLocally }
    public enum SocialModerationDiagnosticCode { ModerationReportTypeMissing, ModerationEvidenceMissing, ModerationPrivacyRuleMissing, ModerationSanctionRuntimeForbidden, ModerationHistoryStorageForbidden, ModerationServerAuthorityRequired }

    public sealed class ModerationEvidenceProjection
    {
        public ModerationEvidenceProjection(string evidenceId, bool missing)
        {
            EvidenceId = evidenceId ?? string.Empty;
            Missing = missing;
        }

        public string EvidenceId { get; }
        public bool Missing { get; }
    }

    public sealed class ModerationPrivacyRuleProjection
    {
        public ModerationPrivacyRuleProjection(string ruleId, bool missing)
        {
            RuleId = ruleId ?? string.Empty;
            Missing = missing;
        }

        public string RuleId { get; }
        public bool Missing { get; }
    }

    public sealed class ModerationSanctionAuthorityTopic
    {
        public ModerationSanctionAuthorityTopic(string topicId, bool serverRequired)
        {
            TopicId = topicId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class SocialModerationReportBoundary
    {
        public SocialModerationReportBoundary(string reportId, ModerationReportType reportType, string reporterProjectionId, string targetProjectionId, ModerationEvidenceProjection evidenceProjection, ModerationReportStatusProjection statusProjection, IReadOnlyList<ModerationPrivacyRuleProjection> privacyRules, IReadOnlyList<ModerationSanctionAuthorityTopic> serverAuthorityTopics, bool sanctionRuntimeRequested, bool moderationHistoryStorageRequested)
        {
            ReportId = ColonyIntegrationIds.Require(reportId);
            ReportType = reportType;
            ReporterProjectionId = reporterProjectionId ?? string.Empty;
            TargetProjectionId = targetProjectionId ?? string.Empty;
            EvidenceProjection = evidenceProjection;
            StatusProjection = statusProjection;
            PrivacyRules = privacyRules ?? Array.Empty<ModerationPrivacyRuleProjection>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<ModerationSanctionAuthorityTopic>();
            SanctionRuntimeRequested = sanctionRuntimeRequested;
            ModerationHistoryStorageRequested = moderationHistoryStorageRequested;
        }

        public string ReportId { get; }
        public ModerationReportType ReportType { get; }
        public string ReporterProjectionId { get; }
        public string TargetProjectionId { get; }
        public ModerationEvidenceProjection EvidenceProjection { get; }
        public ModerationReportStatusProjection StatusProjection { get; }
        public IReadOnlyList<ModerationPrivacyRuleProjection> PrivacyRules { get; }
        public IReadOnlyList<ModerationSanctionAuthorityTopic> ServerAuthorityTopics { get; }
        public bool SanctionRuntimeRequested { get; }
        public bool ModerationHistoryStorageRequested { get; }

        public SocialModerationDiagnostics Evaluate()
        {
            var findings = new List<SocialModerationDiagnosticCode>();
            if (ReportType == ModerationReportType.Missing) findings.Add(SocialModerationDiagnosticCode.ModerationReportTypeMissing);
            if (EvidenceProjection == null || EvidenceProjection.Missing || StatusProjection == ModerationReportStatusProjection.EvidenceMissing) findings.Add(SocialModerationDiagnosticCode.ModerationEvidenceMissing);
            if (PrivacyRules.Count == 0 || PrivacyRules.Any(p => p.Missing)) findings.Add(SocialModerationDiagnosticCode.ModerationPrivacyRuleMissing);
            if (SanctionRuntimeRequested || StatusProjection == ModerationReportStatusProjection.SanctionForbiddenLocally) findings.Add(SocialModerationDiagnosticCode.ModerationSanctionRuntimeForbidden);
            if (ModerationHistoryStorageRequested) findings.Add(SocialModerationDiagnosticCode.ModerationHistoryStorageForbidden);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired)) findings.Add(SocialModerationDiagnosticCode.ModerationServerAuthorityRequired);
            return new SocialModerationDiagnostics(findings);
        }
    }

    public sealed class SocialModerationDiagnostics
    {
        public SocialModerationDiagnostics(IReadOnlyList<SocialModerationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialModerationDiagnosticCode>(); }
        public IReadOnlyList<SocialModerationDiagnosticCode> Findings { get; }
        public bool Contains(SocialModerationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum TerritoryClaimStatus { ClaimIntentProjected, ConflictingClaimProjected, DiplomacyBlockedProjected, WarRequiredProjected, OfficialTerritoryForbidden, ServerAuthorityRequired }
    public enum TerritoryClaimDiagnosticCode { TerritoryClaimRegionMissing, TerritoryOfficialClaimForbidden, TerritoryClaimConflictOpen, TerritoryBenefitRuntimeForbidden, TerritorySnowballRiskOpen, TerritoryServerAuthorityRequired }

    public sealed class TerritoryClaimRegionReference
    {
        public TerritoryClaimRegionReference(string regionId, bool missing)
        {
            RegionId = regionId ?? string.Empty;
            Missing = missing;
        }

        public string RegionId { get; }
        public bool Missing { get; }
    }

    public sealed class TerritoryClaimConflictProjection
    {
        public TerritoryClaimConflictProjection(string conflictId, bool open)
        {
            ConflictId = conflictId ?? string.Empty;
            Open = open;
        }

        public string ConflictId { get; }
        public bool Open { get; }
    }

    public sealed class TerritoryClaimBenefitExpectation
    {
        public TerritoryClaimBenefitExpectation(string benefitId, bool runtimeBenefitRequested)
        {
            BenefitId = benefitId ?? string.Empty;
            RuntimeBenefitRequested = runtimeBenefitRequested;
        }

        public string BenefitId { get; }
        public bool RuntimeBenefitRequested { get; }
    }

    public sealed class TerritoryClaimRisk
    {
        public TerritoryClaimRisk(string riskId, bool snowballOpen)
        {
            RiskId = riskId ?? string.Empty;
            SnowballOpen = snowballOpen;
        }

        public string RiskId { get; }
        public bool SnowballOpen { get; }
    }

    public sealed class TerritoryClaimServerAuthorityTopic
    {
        public TerritoryClaimServerAuthorityTopic(string topicId, bool serverRequired)
        {
            TopicId = topicId ?? string.Empty;
            ServerRequired = serverRequired;
        }

        public string TopicId { get; }
        public bool ServerRequired { get; }
    }

    public sealed class AllianceTerritoryClaimProjection
    {
        public AllianceTerritoryClaimProjection(string claimId, string allianceProjectionId, TerritoryClaimRegionReference regionReference, TerritoryClaimStatus claimStatus, IReadOnlyList<TerritoryClaimConflictProjection> conflictProjections, IReadOnlyList<TerritoryClaimBenefitExpectation> benefitExpectations, IReadOnlyList<TerritoryClaimRisk> risks, IReadOnlyList<TerritoryClaimServerAuthorityTopic> serverAuthorityTopics)
        {
            ClaimId = ColonyIntegrationIds.Require(claimId);
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            RegionReference = regionReference;
            ClaimStatus = claimStatus;
            ConflictProjections = conflictProjections ?? Array.Empty<TerritoryClaimConflictProjection>();
            BenefitExpectations = benefitExpectations ?? Array.Empty<TerritoryClaimBenefitExpectation>();
            Risks = risks ?? Array.Empty<TerritoryClaimRisk>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<TerritoryClaimServerAuthorityTopic>();
        }

        public string ClaimId { get; }
        public string AllianceProjectionId { get; }
        public TerritoryClaimRegionReference RegionReference { get; }
        public TerritoryClaimStatus ClaimStatus { get; }
        public IReadOnlyList<TerritoryClaimConflictProjection> ConflictProjections { get; }
        public IReadOnlyList<TerritoryClaimBenefitExpectation> BenefitExpectations { get; }
        public IReadOnlyList<TerritoryClaimRisk> Risks { get; }
        public IReadOnlyList<TerritoryClaimServerAuthorityTopic> ServerAuthorityTopics { get; }

        public TerritoryClaimDiagnostics Evaluate()
        {
            var findings = new List<TerritoryClaimDiagnosticCode>();
            if (RegionReference == null || RegionReference.Missing || string.IsNullOrWhiteSpace(RegionReference.RegionId)) findings.Add(TerritoryClaimDiagnosticCode.TerritoryClaimRegionMissing);
            if (ClaimStatus == TerritoryClaimStatus.OfficialTerritoryForbidden) findings.Add(TerritoryClaimDiagnosticCode.TerritoryOfficialClaimForbidden);
            if (ConflictProjections.Any(c => c.Open) || ClaimStatus == TerritoryClaimStatus.ConflictingClaimProjected) findings.Add(TerritoryClaimDiagnosticCode.TerritoryClaimConflictOpen);
            if (BenefitExpectations.Any(b => b.RuntimeBenefitRequested)) findings.Add(TerritoryClaimDiagnosticCode.TerritoryBenefitRuntimeForbidden);
            if (Risks.Any(r => r.SnowballOpen)) findings.Add(TerritoryClaimDiagnosticCode.TerritorySnowballRiskOpen);
            if (ServerAuthorityTopics.Any(t => t.ServerRequired) || ClaimStatus == TerritoryClaimStatus.ServerAuthorityRequired) findings.Add(TerritoryClaimDiagnosticCode.TerritoryServerAuthorityRequired);
            return new TerritoryClaimDiagnostics(findings);
        }
    }

    public sealed class TerritoryClaimDiagnostics
    {
        public TerritoryClaimDiagnostics(IReadOnlyList<TerritoryClaimDiagnosticCode> findings) { Findings = findings ?? Array.Empty<TerritoryClaimDiagnosticCode>(); }
        public IReadOnlyList<TerritoryClaimDiagnosticCode> Findings { get; }
        public bool Contains(TerritoryClaimDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum SocialAuthorityReadinessVerdictType { ReadyForArchitectValidation, ReadyWithServerWarnings, NeedsPlannerRevision, BlockedByMissingInput, BlockedByServerAuthorityGap, BlockedByPlayerProtectionGap, BlockedByDemoEvidenceGap, BlockedByBee331Premature }
    public enum SocialAuthorityReadinessDiagnosticCode { SocialAuthorityInputMissing, SocialAuthorityPillarMissing, SocialAuthorityServerGapOpen, SocialAuthorityProtectionGapOpen, SocialAuthorityDemoGapOpen, Bee331Premature }

    public sealed class SocialAuthorityInputSet
    {
        public SocialAuthorityInputSet(string serverImpactIntake, string alliancePersistenceBoundary, string permissionAbuseAudit, string diplomacyNegotiationFlow, string warRallyPlanning, string armyTrainingReadiness, string defeatRecoveryProtection, string moderationReportingBoundary, string territoryClaimProjection)
        {
            ServerImpactIntake = serverImpactIntake ?? string.Empty;
            AlliancePersistenceBoundary = alliancePersistenceBoundary ?? string.Empty;
            PermissionAbuseAudit = permissionAbuseAudit ?? string.Empty;
            DiplomacyNegotiationFlow = diplomacyNegotiationFlow ?? string.Empty;
            WarRallyPlanning = warRallyPlanning ?? string.Empty;
            ArmyTrainingReadiness = armyTrainingReadiness ?? string.Empty;
            DefeatRecoveryProtection = defeatRecoveryProtection ?? string.Empty;
            ModerationReportingBoundary = moderationReportingBoundary ?? string.Empty;
            TerritoryClaimProjection = territoryClaimProjection ?? string.Empty;
        }

        public string ServerImpactIntake { get; }
        public string AlliancePersistenceBoundary { get; }
        public string PermissionAbuseAudit { get; }
        public string DiplomacyNegotiationFlow { get; }
        public string WarRallyPlanning { get; }
        public string ArmyTrainingReadiness { get; }
        public string DefeatRecoveryProtection { get; }
        public string ModerationReportingBoundary { get; }
        public string TerritoryClaimProjection { get; }

        public bool HasMissingInput()
        {
            return string.IsNullOrWhiteSpace(ServerImpactIntake)
                || string.IsNullOrWhiteSpace(AlliancePersistenceBoundary)
                || string.IsNullOrWhiteSpace(PermissionAbuseAudit)
                || string.IsNullOrWhiteSpace(DiplomacyNegotiationFlow)
                || string.IsNullOrWhiteSpace(WarRallyPlanning)
                || string.IsNullOrWhiteSpace(ArmyTrainingReadiness)
                || string.IsNullOrWhiteSpace(DefeatRecoveryProtection)
                || string.IsNullOrWhiteSpace(ModerationReportingBoundary)
                || string.IsNullOrWhiteSpace(TerritoryClaimProjection);
        }
    }

    public sealed class SocialAuthorityCoverageMatrix
    {
        public SocialAuthorityCoverageMatrix(IReadOnlyList<SocialMmoProductPillar> coveredPillars, bool demoEvidencePresent)
        {
            CoveredPillars = coveredPillars ?? Array.Empty<SocialMmoProductPillar>();
            DemoEvidencePresent = demoEvidencePresent;
        }

        public IReadOnlyList<SocialMmoProductPillar> CoveredPillars { get; }
        public bool DemoEvidencePresent { get; }
        public bool Covers(SocialMmoProductPillar pillar) { return CoveredPillars.Contains(pillar); }
    }

    public sealed class SocialAuthorityServerGap
    {
        public SocialAuthorityServerGap(string gapId, string topic, bool open)
        {
            GapId = gapId ?? string.Empty;
            Topic = topic ?? string.Empty;
            Open = open;
        }

        public string GapId { get; }
        public string Topic { get; }
        public bool Open { get; }
    }

    public sealed class SocialAuthorityRiskRegister
    {
        public SocialAuthorityRiskRegister(IReadOnlyList<string> risks)
        {
            Risks = risks ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Risks { get; }
    }

    public sealed class SocialPlayerProtectionCoverage
    {
        public SocialPlayerProtectionCoverage(bool beginnerProtectionCovered, bool antiHarassmentCovered, bool defeatRecoveryCovered)
        {
            BeginnerProtectionCovered = beginnerProtectionCovered;
            AntiHarassmentCovered = antiHarassmentCovered;
            DefeatRecoveryCovered = defeatRecoveryCovered;
        }

        public bool BeginnerProtectionCovered { get; }
        public bool AntiHarassmentCovered { get; }
        public bool DefeatRecoveryCovered { get; }
        public bool HasGap() { return !BeginnerProtectionCovered || !AntiHarassmentCovered || !DefeatRecoveryCovered; }
    }

    public sealed class Bee331BlockerStatus
    {
        public Bee331BlockerStatus(bool prematureAttempt, string message)
        {
            PrematureAttempt = prematureAttempt;
            Message = message ?? string.Empty;
        }

        public bool PrematureAttempt { get; }
        public string Message { get; }
    }

    public sealed class SocialAuthorityReadinessVerdict
    {
        public SocialAuthorityReadinessVerdict(SocialAuthorityReadinessVerdictType verdictType, IReadOnlyList<SocialAuthorityReadinessDiagnosticCode> diagnostics)
        {
            VerdictType = verdictType;
            Diagnostics = diagnostics ?? Array.Empty<SocialAuthorityReadinessDiagnosticCode>();
        }

        public SocialAuthorityReadinessVerdictType VerdictType { get; }
        public IReadOnlyList<SocialAuthorityReadinessDiagnosticCode> Diagnostics { get; }
        public bool Contains(SocialAuthorityReadinessDiagnosticCode code) { return Diagnostics.Contains(code); }
    }

    public sealed class SocialAuthorityReadinessGate
    {
        public const string Bee331BlockedMessage = "BEE-331 bloquee jusqu'a validation architecte.";

        public SocialAuthorityReadinessGate(string gateId, SocialAuthorityInputSet inputSet, SocialAuthorityCoverageMatrix coverageMatrix, IReadOnlyList<SocialAuthorityServerGap> serverGaps, SocialAuthorityRiskRegister riskRegister, SocialPlayerProtectionCoverage protectionCoverage, Bee331BlockerStatus bee331Status)
        {
            GateId = ColonyIntegrationIds.Require(gateId);
            InputSet = inputSet;
            CoverageMatrix = coverageMatrix;
            ServerGaps = serverGaps ?? Array.Empty<SocialAuthorityServerGap>();
            RiskRegister = riskRegister;
            ProtectionCoverage = protectionCoverage;
            Bee331Status = bee331Status;
        }

        public string GateId { get; }
        public SocialAuthorityInputSet InputSet { get; }
        public SocialAuthorityCoverageMatrix CoverageMatrix { get; }
        public IReadOnlyList<SocialAuthorityServerGap> ServerGaps { get; }
        public SocialAuthorityRiskRegister RiskRegister { get; }
        public SocialPlayerProtectionCoverage ProtectionCoverage { get; }
        public Bee331BlockerStatus Bee331Status { get; }

        public SocialAuthorityReadinessVerdict Evaluate()
        {
            var diagnostics = BuildDiagnostics();
            return new SocialAuthorityReadinessVerdict(ResolveVerdict(diagnostics), diagnostics);
        }

        private IReadOnlyList<SocialAuthorityReadinessDiagnosticCode> BuildDiagnostics()
        {
            var diagnostics = new List<SocialAuthorityReadinessDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput()) diagnostics.Add(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityInputMissing);
            if (CoverageMatrix == null || !CoverageMatrix.Covers(SocialMmoProductPillar.Alliances) || !CoverageMatrix.Covers(SocialMmoProductPillar.Diplomacy) || !CoverageMatrix.Covers(SocialMmoProductPillar.War) || !CoverageMatrix.Covers(SocialMmoProductPillar.PvP) || !CoverageMatrix.Covers(SocialMmoProductPillar.Communication) || !CoverageMatrix.Covers(SocialMmoProductPillar.Army) || !CoverageMatrix.Covers(SocialMmoProductPillar.PlayerProgression)) diagnostics.Add(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityPillarMissing);
            if (ServerGaps.Count == 0 || ServerGaps.Any(g => g.Open)) diagnostics.Add(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityServerGapOpen);
            if (ProtectionCoverage == null || ProtectionCoverage.HasGap()) diagnostics.Add(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityProtectionGapOpen);
            if (CoverageMatrix == null || !CoverageMatrix.DemoEvidencePresent) diagnostics.Add(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityDemoGapOpen);
            if (Bee331Status != null && Bee331Status.PrematureAttempt) diagnostics.Add(SocialAuthorityReadinessDiagnosticCode.Bee331Premature);
            return diagnostics;
        }

        private SocialAuthorityReadinessVerdictType ResolveVerdict(IReadOnlyList<SocialAuthorityReadinessDiagnosticCode> diagnostics)
        {
            if (diagnostics.Contains(SocialAuthorityReadinessDiagnosticCode.Bee331Premature)) return SocialAuthorityReadinessVerdictType.BlockedByBee331Premature;
            if (diagnostics.Contains(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityInputMissing)) return SocialAuthorityReadinessVerdictType.BlockedByMissingInput;
            if (diagnostics.Contains(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityServerGapOpen)) return SocialAuthorityReadinessVerdictType.BlockedByServerAuthorityGap;
            if (diagnostics.Contains(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityProtectionGapOpen)) return SocialAuthorityReadinessVerdictType.BlockedByPlayerProtectionGap;
            if (diagnostics.Contains(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityDemoGapOpen)) return SocialAuthorityReadinessVerdictType.BlockedByDemoEvidenceGap;
            if (diagnostics.Contains(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityPillarMissing)) return SocialAuthorityReadinessVerdictType.NeedsPlannerRevision;
            return SocialAuthorityReadinessVerdictType.ReadyForArchitectValidation;
        }
    }
}
