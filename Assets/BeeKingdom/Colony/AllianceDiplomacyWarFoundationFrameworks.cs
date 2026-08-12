using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum AllianceCreationVerdictKind { CreationProjectionAllowed, BlockedByMissingPlayerIdentity, BlockedByInvalidName, BlockedByDuplicateNameRisk, BlockedByCooldownProjection, BlockedByAbuseRisk, BlockedByServerAuthorityRequired, BlockedByRuntimeCreationForbidden }
    public enum AllianceCreationDiagnosticCode { AllianceCreatorIdentityMissing, AllianceNameInvalid, AllianceTagInvalid, AllianceNameDuplicateRisk, AllianceCreationCooldownProjected, AllianceCreationAbuseRiskOpen, AlliancePersistentCreationForbidden, AllianceCreationServerAuthorityRequired }

    public sealed class AllianceCreationCondition
    {
        public AllianceCreationCondition(string conditionId, string sourceBee, string label, bool satisfied, string blockingReason)
        {
            ConditionId = conditionId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            Label = label ?? string.Empty;
            Satisfied = satisfied;
            BlockingReason = blockingReason ?? string.Empty;
        }

        public string ConditionId { get; }
        public string SourceBee { get; }
        public string Label { get; }
        public bool Satisfied { get; }
        public string BlockingReason { get; }
    }

    public sealed class AllianceNameReservationProjection
    {
        public AllianceNameReservationProjection(bool duplicateRisk, bool definitiveReservationForbidden, string evidence)
        {
            DuplicateRisk = duplicateRisk;
            DefinitiveReservationForbidden = definitiveReservationForbidden;
            Evidence = evidence ?? string.Empty;
        }

        public bool DuplicateRisk { get; }
        public bool DefinitiveReservationForbidden { get; }
        public string Evidence { get; }
    }

    public sealed class AllianceCreationAbuseRisk
    {
        public AllianceCreationAbuseRisk(string riskId, string riskType, bool open, string mitigation)
        {
            RiskId = riskId ?? string.Empty;
            RiskType = riskType ?? string.Empty;
            Open = open;
            Mitigation = mitigation ?? string.Empty;
        }

        public string RiskId { get; }
        public string RiskType { get; }
        public bool Open { get; }
        public string Mitigation { get; }
    }

    public sealed class AllianceCreationServerAuthorityTopic
    {
        public AllianceCreationServerAuthorityTopic(string topicId, string owner, string reason)
        {
            TopicId = topicId ?? string.Empty;
            Owner = owner ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string TopicId { get; }
        public string Owner { get; }
        public string Reason { get; }
    }

    public sealed class AllianceCreationRequestProjection
    {
        public AllianceCreationRequestProjection(string creatorPlayerHiveIdentityId, string proposedAllianceName, string proposedAllianceTag, string proposedDescription, string expectedSocialPosture)
        {
            CreatorPlayerHiveIdentityId = creatorPlayerHiveIdentityId ?? string.Empty;
            ProposedAllianceName = proposedAllianceName ?? string.Empty;
            ProposedAllianceTag = proposedAllianceTag ?? string.Empty;
            ProposedDescription = proposedDescription ?? string.Empty;
            ExpectedSocialPosture = expectedSocialPosture ?? string.Empty;
        }

        public string CreatorPlayerHiveIdentityId { get; }
        public string ProposedAllianceName { get; }
        public string ProposedAllianceTag { get; }
        public string ProposedDescription { get; }
        public string ExpectedSocialPosture { get; }
    }

    public sealed class AllianceCreationBoundary
    {
        public AllianceCreationBoundary(string boundaryId, AllianceCreationRequestProjection requestProjection, IReadOnlyList<AllianceCreationCondition> projectedConditions, AllianceNameReservationProjection nameReservationProjection, IReadOnlyList<AllianceCreationAbuseRisk> abuseRisks, AllianceCreationVerdictKind verdict, IReadOnlyList<AllianceCreationServerAuthorityTopic> serverAuthorityTopics, bool persistentCreationRequested = false, bool cooldownProjected = false)
        {
            BoundaryId = ColonyIntegrationIds.Require(boundaryId);
            RequestProjection = requestProjection;
            ProjectedConditions = projectedConditions ?? Array.Empty<AllianceCreationCondition>();
            NameReservationProjection = nameReservationProjection;
            AbuseRisks = abuseRisks ?? Array.Empty<AllianceCreationAbuseRisk>();
            Verdict = verdict;
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<AllianceCreationServerAuthorityTopic>();
            PersistentCreationRequested = persistentCreationRequested;
            CooldownProjected = cooldownProjected;
        }

        public string BoundaryId { get; }
        public AllianceCreationRequestProjection RequestProjection { get; }
        public IReadOnlyList<AllianceCreationCondition> ProjectedConditions { get; }
        public AllianceNameReservationProjection NameReservationProjection { get; }
        public IReadOnlyList<AllianceCreationAbuseRisk> AbuseRisks { get; }
        public AllianceCreationVerdictKind Verdict { get; }
        public IReadOnlyList<AllianceCreationServerAuthorityTopic> ServerAuthorityTopics { get; }
        public bool PersistentCreationRequested { get; }
        public bool CooldownProjected { get; }

        public AllianceCreationDiagnostics Evaluate()
        {
            var findings = new List<AllianceCreationDiagnosticCode>();
            if (RequestProjection == null || string.IsNullOrWhiteSpace(RequestProjection.CreatorPlayerHiveIdentityId)) findings.Add(AllianceCreationDiagnosticCode.AllianceCreatorIdentityMissing);
            if (RequestProjection == null || string.IsNullOrWhiteSpace(RequestProjection.ProposedAllianceName) || RequestProjection.ProposedAllianceName.Length < 3) findings.Add(AllianceCreationDiagnosticCode.AllianceNameInvalid);
            if (RequestProjection == null || string.IsNullOrWhiteSpace(RequestProjection.ProposedAllianceTag) || RequestProjection.ProposedAllianceTag.Length < 2) findings.Add(AllianceCreationDiagnosticCode.AllianceTagInvalid);
            if (NameReservationProjection != null && NameReservationProjection.DuplicateRisk) findings.Add(AllianceCreationDiagnosticCode.AllianceNameDuplicateRisk);
            if (CooldownProjected) findings.Add(AllianceCreationDiagnosticCode.AllianceCreationCooldownProjected);
            if (AbuseRisks.Any(r => r.Open)) findings.Add(AllianceCreationDiagnosticCode.AllianceCreationAbuseRiskOpen);
            if (PersistentCreationRequested || Verdict == AllianceCreationVerdictKind.BlockedByRuntimeCreationForbidden) findings.Add(AllianceCreationDiagnosticCode.AlliancePersistentCreationForbidden);
            if (ServerAuthorityTopics.Count > 0 || Verdict == AllianceCreationVerdictKind.BlockedByServerAuthorityRequired) findings.Add(AllianceCreationDiagnosticCode.AllianceCreationServerAuthorityRequired);
            return new AllianceCreationDiagnostics(findings);
        }
    }

    public sealed class AllianceCreationDiagnostics
    {
        public AllianceCreationDiagnostics(IReadOnlyList<AllianceCreationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceCreationDiagnosticCode>(); }
        public IReadOnlyList<AllianceCreationDiagnosticCode> Findings { get; }
        public bool Contains(AllianceCreationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceRoleKind { Leader, Officer, Recruiter, Diplomat, Member, Guest }
    public enum AlliancePermissionScope { Read, Proposal, Mutation, Communication, Diplomacy, War, Treasury }
    public enum AllianceRolePermissionDiagnosticCode { AllianceRoleMissing, AllianceLeaderAuthorityMissing, AlliancePermissionImplicit, AlliancePermissionRuntimeForbidden, AllianceRoleMutationServerAuthorityRequired, AllianceForbiddenActionRequested }

    public sealed class AllianceRoleDefinition
    {
        public AllianceRoleDefinition(AllianceRoleKind role, int rank, string productUse)
        {
            Role = role;
            Rank = Math.Max(0, rank);
            ProductUse = productUse ?? string.Empty;
        }

        public AllianceRoleKind Role { get; }
        public int Rank { get; }
        public string ProductUse { get; }
    }

    public sealed class AlliancePermissionDefinition
    {
        public AlliancePermissionDefinition(string permissionId, string label, AllianceRoleKind minimumRole, AlliancePermissionScope scope, bool readOnly, bool requiresServerAuthority, bool demoOnlyProjection, bool implicitPermission = false, bool runtimePermissionRequested = false)
        {
            PermissionId = permissionId ?? string.Empty;
            Label = label ?? string.Empty;
            MinimumRole = minimumRole;
            Scope = scope;
            ReadOnly = readOnly;
            RequiresServerAuthority = requiresServerAuthority;
            DemoOnlyProjection = demoOnlyProjection;
            ImplicitPermission = implicitPermission;
            RuntimePermissionRequested = runtimePermissionRequested;
        }

        public string PermissionId { get; }
        public string Label { get; }
        public AllianceRoleKind MinimumRole { get; }
        public AlliancePermissionScope Scope { get; }
        public bool ReadOnly { get; }
        public bool RequiresServerAuthority { get; }
        public bool DemoOnlyProjection { get; }
        public bool ImplicitPermission { get; }
        public bool RuntimePermissionRequested { get; }
    }

    public sealed class AllianceForbiddenAction
    {
        public AllianceForbiddenAction(string actionId, string reason, bool requested)
        {
            ActionId = actionId ?? string.Empty;
            Reason = reason ?? string.Empty;
            Requested = requested;
        }

        public string ActionId { get; }
        public string Reason { get; }
        public bool Requested { get; }
    }

    public sealed class AllianceRoleMutationAuthorityTopic
    {
        public AllianceRoleMutationAuthorityTopic(string topicId, string mutation, string serverOwner)
        {
            TopicId = topicId ?? string.Empty;
            Mutation = mutation ?? string.Empty;
            ServerOwner = serverOwner ?? string.Empty;
        }

        public string TopicId { get; }
        public string Mutation { get; }
        public string ServerOwner { get; }
    }

    public sealed class AllianceRoleHierarchyProjection
    {
        public AllianceRoleHierarchyProjection(string allianceProjectionId, IReadOnlyList<AllianceRoleDefinition> roles, IReadOnlyList<AlliancePermissionDefinition> permissions, IReadOnlyList<AllianceForbiddenAction> forbiddenActions, IReadOnlyList<AllianceRoleMutationAuthorityTopic> authorityTopics)
        {
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            Roles = roles ?? Array.Empty<AllianceRoleDefinition>();
            Permissions = permissions ?? Array.Empty<AlliancePermissionDefinition>();
            ForbiddenActions = forbiddenActions ?? Array.Empty<AllianceForbiddenAction>();
            AuthorityTopics = authorityTopics ?? Array.Empty<AllianceRoleMutationAuthorityTopic>();
        }

        public string AllianceProjectionId { get; }
        public IReadOnlyList<AllianceRoleDefinition> Roles { get; }
        public IReadOnlyList<AlliancePermissionDefinition> Permissions { get; }
        public IReadOnlyList<AllianceForbiddenAction> ForbiddenActions { get; }
        public IReadOnlyList<AllianceRoleMutationAuthorityTopic> AuthorityTopics { get; }

        public AllianceRolePermissionDiagnostics Evaluate()
        {
            var findings = new List<AllianceRolePermissionDiagnosticCode>();
            if (Roles.Count == 0 || !Roles.Any(r => r.Role == AllianceRoleKind.Leader)) findings.Add(AllianceRolePermissionDiagnosticCode.AllianceRoleMissing);
            if (!Permissions.Any(p => p.MinimumRole == AllianceRoleKind.Leader && p.RequiresServerAuthority)) findings.Add(AllianceRolePermissionDiagnosticCode.AllianceLeaderAuthorityMissing);
            if (Permissions.Any(p => p.ImplicitPermission || string.IsNullOrWhiteSpace(p.PermissionId))) findings.Add(AllianceRolePermissionDiagnosticCode.AlliancePermissionImplicit);
            if (Permissions.Any(p => p.RuntimePermissionRequested)) findings.Add(AllianceRolePermissionDiagnosticCode.AlliancePermissionRuntimeForbidden);
            if (AuthorityTopics.Count > 0 || Permissions.Any(p => p.Scope == AlliancePermissionScope.Mutation && p.RequiresServerAuthority)) findings.Add(AllianceRolePermissionDiagnosticCode.AllianceRoleMutationServerAuthorityRequired);
            if (ForbiddenActions.Any(a => a.Requested)) findings.Add(AllianceRolePermissionDiagnosticCode.AllianceForbiddenActionRequested);
            return new AllianceRolePermissionDiagnostics(findings);
        }
    }

    public sealed class AllianceRolePermissionDiagnostics
    {
        public AllianceRolePermissionDiagnostics(IReadOnlyList<AllianceRolePermissionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceRolePermissionDiagnosticCode>(); }
        public IReadOnlyList<AllianceRolePermissionDiagnosticCode> Findings { get; }
        public bool Contains(AllianceRolePermissionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceMembershipLifecycleState { Unaffiliated, Invited, Applied, PendingReview, AcceptedProjected, RejectedProjected, LeftProjected, RemovedProjected, CooldownProjected, ServerAuthorityRequired }
    public enum AllianceMembershipLifecycleDiagnosticCode { AllianceMembershipStateInvalid, AllianceInvitationPermissionMissing, AllianceApplicationDuplicateProjected, AllianceMembershipCooldownActiveProjected, AllianceRemovalServerAuthorityRequired, AlliancePersistentMembershipForbidden }

    public sealed class AllianceInvitationProjection
    {
        public AllianceInvitationProjection(string invitationId, AllianceRoleKind requestedByRole, bool permissionPresent)
        {
            InvitationId = invitationId ?? string.Empty;
            RequestedByRole = requestedByRole;
            PermissionPresent = permissionPresent;
        }

        public string InvitationId { get; }
        public AllianceRoleKind RequestedByRole { get; }
        public bool PermissionPresent { get; }
    }

    public sealed class AllianceApplicationProjection
    {
        public AllianceApplicationProjection(string applicationId, bool duplicateProjected)
        {
            ApplicationId = applicationId ?? string.Empty;
            DuplicateProjected = duplicateProjected;
        }

        public string ApplicationId { get; }
        public bool DuplicateProjected { get; }
    }

    public sealed class AllianceMembershipCooldownProjection
    {
        public AllianceMembershipCooldownProjection(bool activeProjected, string reason)
        {
            ActiveProjected = activeProjected;
            Reason = reason ?? string.Empty;
        }

        public bool ActiveProjected { get; }
        public string Reason { get; }
    }

    public sealed class AllianceMembershipHistoryEntry
    {
        public AllianceMembershipHistoryEntry(string entryId, AllianceMembershipLifecycleState state, string sourceBee)
        {
            EntryId = entryId ?? string.Empty;
            State = state;
            SourceBee = sourceBee ?? string.Empty;
        }

        public string EntryId { get; }
        public AllianceMembershipLifecycleState State { get; }
        public string SourceBee { get; }
    }

    public sealed class AllianceMembershipTransition
    {
        public AllianceMembershipTransition(AllianceMembershipLifecycleState fromState, AllianceMembershipLifecycleState toState, AllianceRoleKind requestedByRole, string requiredPermission, bool requiresServerAuthority, string blockingReason)
        {
            FromState = fromState;
            ToState = toState;
            RequestedByRole = requestedByRole;
            RequiredPermission = requiredPermission ?? string.Empty;
            RequiresServerAuthority = requiresServerAuthority;
            BlockingReason = blockingReason ?? string.Empty;
        }

        public AllianceMembershipLifecycleState FromState { get; }
        public AllianceMembershipLifecycleState ToState { get; }
        public AllianceRoleKind RequestedByRole { get; }
        public string RequiredPermission { get; }
        public bool RequiresServerAuthority { get; }
        public string BlockingReason { get; }
    }

    public sealed class AllianceMembershipLifecycleProjection
    {
        public AllianceMembershipLifecycleProjection(string playerHiveIdentityId, string allianceProjectionId, AllianceMembershipLifecycleState currentState, AllianceInvitationProjection pendingInvitation, AllianceApplicationProjection pendingApplication, IReadOnlyList<AllianceMembershipTransition> projectedTransitions, AllianceMembershipCooldownProjection cooldownProjection, IReadOnlyList<AllianceMembershipHistoryEntry> historyProjection, bool persistentMembershipRequested = false)
        {
            PlayerHiveIdentityId = playerHiveIdentityId ?? string.Empty;
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            CurrentState = currentState;
            PendingInvitation = pendingInvitation;
            PendingApplication = pendingApplication;
            ProjectedTransitions = projectedTransitions ?? Array.Empty<AllianceMembershipTransition>();
            CooldownProjection = cooldownProjection;
            HistoryProjection = historyProjection ?? Array.Empty<AllianceMembershipHistoryEntry>();
            PersistentMembershipRequested = persistentMembershipRequested;
        }

        public string PlayerHiveIdentityId { get; }
        public string AllianceProjectionId { get; }
        public AllianceMembershipLifecycleState CurrentState { get; }
        public AllianceInvitationProjection PendingInvitation { get; }
        public AllianceApplicationProjection PendingApplication { get; }
        public IReadOnlyList<AllianceMembershipTransition> ProjectedTransitions { get; }
        public AllianceMembershipCooldownProjection CooldownProjection { get; }
        public IReadOnlyList<AllianceMembershipHistoryEntry> HistoryProjection { get; }
        public bool PersistentMembershipRequested { get; }

        public AllianceMembershipLifecycleDiagnostics Evaluate()
        {
            var findings = new List<AllianceMembershipLifecycleDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(PlayerHiveIdentityId) || string.IsNullOrWhiteSpace(AllianceProjectionId) || ProjectedTransitions.Count == 0) findings.Add(AllianceMembershipLifecycleDiagnosticCode.AllianceMembershipStateInvalid);
            if (PendingInvitation != null && !PendingInvitation.PermissionPresent) findings.Add(AllianceMembershipLifecycleDiagnosticCode.AllianceInvitationPermissionMissing);
            if (PendingApplication != null && PendingApplication.DuplicateProjected) findings.Add(AllianceMembershipLifecycleDiagnosticCode.AllianceApplicationDuplicateProjected);
            if (CooldownProjection != null && CooldownProjection.ActiveProjected) findings.Add(AllianceMembershipLifecycleDiagnosticCode.AllianceMembershipCooldownActiveProjected);
            if (ProjectedTransitions.Any(t => t.ToState == AllianceMembershipLifecycleState.RemovedProjected && t.RequiresServerAuthority)) findings.Add(AllianceMembershipLifecycleDiagnosticCode.AllianceRemovalServerAuthorityRequired);
            if (PersistentMembershipRequested || CurrentState == AllianceMembershipLifecycleState.ServerAuthorityRequired) findings.Add(AllianceMembershipLifecycleDiagnosticCode.AlliancePersistentMembershipForbidden);
            return new AllianceMembershipLifecycleDiagnostics(findings);
        }
    }

    public sealed class AllianceMembershipLifecycleDiagnostics
    {
        public AllianceMembershipLifecycleDiagnostics(IReadOnlyList<AllianceMembershipLifecycleDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceMembershipLifecycleDiagnosticCode>(); }
        public IReadOnlyList<AllianceMembershipLifecycleDiagnosticCode> Findings { get; }
        public bool Contains(AllianceMembershipLifecycleDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceChannelContractType { AllianceGeneral, AllianceLeadership, AllianceRecruitment, AllianceDiplomacyNotice, AllianceWarNotice, AllianceSystemLog }
    public enum AllianceCommunicationChannelDiagnosticCode { AllianceChannelAudienceMissing, AllianceChannelPermissionMissing, AllianceMessageRuntimeForbidden, AllianceMessageStorageForbidden, AllianceModerationServerAuthorityRequired, AllianceChannelVisibilityMismatch }

    public sealed class AllianceChannelAudienceProjection
    {
        public AllianceChannelAudienceProjection(IReadOnlyList<AllianceMembershipLifecycleState> membershipStates, IReadOnlyList<AllianceRoleKind> requiredRoles, IReadOnlyList<AllianceRoleKind> excludedRoles, string visibilityReason)
        {
            MembershipStates = membershipStates ?? Array.Empty<AllianceMembershipLifecycleState>();
            RequiredRoles = requiredRoles ?? Array.Empty<AllianceRoleKind>();
            ExcludedRoles = excludedRoles ?? Array.Empty<AllianceRoleKind>();
            VisibilityReason = visibilityReason ?? string.Empty;
        }

        public IReadOnlyList<AllianceMembershipLifecycleState> MembershipStates { get; }
        public IReadOnlyList<AllianceRoleKind> RequiredRoles { get; }
        public IReadOnlyList<AllianceRoleKind> ExcludedRoles { get; }
        public string VisibilityReason { get; }
    }

    public sealed class AllianceChannelPermissionRequirement
    {
        public AllianceChannelPermissionRequirement(string permissionId, bool present)
        {
            PermissionId = permissionId ?? string.Empty;
            Present = present;
        }

        public string PermissionId { get; }
        public bool Present { get; }
    }

    public sealed class AllianceMessageRetentionProjection
    {
        public AllianceMessageRetentionProjection(bool storageRequested, string policy)
        {
            StorageRequested = storageRequested;
            Policy = policy ?? string.Empty;
        }

        public bool StorageRequested { get; }
        public string Policy { get; }
    }

    public sealed class AllianceModerationRequirement
    {
        public AllianceModerationRequirement(string classification, bool requiresServerAuthority)
        {
            Classification = classification ?? string.Empty;
            RequiresServerAuthority = requiresServerAuthority;
        }

        public string Classification { get; }
        public bool RequiresServerAuthority { get; }
    }

    public sealed class AllianceCommunicationChannelContract
    {
        public AllianceCommunicationChannelContract(string channelId, AllianceChannelContractType channelType, AllianceChannelAudienceProjection audienceProjection, AllianceChannelPermissionRequirement permissionRequirement, AllianceModerationRequirement moderationRequirement, AllianceMessageRetentionProjection retentionProjection, bool runtimeMessagingAllowed, bool visibilityMismatch = false)
        {
            ChannelId = channelId ?? string.Empty;
            ChannelType = channelType;
            AudienceProjection = audienceProjection;
            PermissionRequirement = permissionRequirement;
            ModerationRequirement = moderationRequirement;
            RetentionProjection = retentionProjection;
            RuntimeMessagingAllowed = runtimeMessagingAllowed;
            VisibilityMismatch = visibilityMismatch;
        }

        public string ChannelId { get; }
        public AllianceChannelContractType ChannelType { get; }
        public AllianceChannelAudienceProjection AudienceProjection { get; }
        public AllianceChannelPermissionRequirement PermissionRequirement { get; }
        public AllianceModerationRequirement ModerationRequirement { get; }
        public AllianceMessageRetentionProjection RetentionProjection { get; }
        public bool RuntimeMessagingAllowed { get; }
        public bool VisibilityMismatch { get; }

        public AllianceCommunicationChannelDiagnostics Evaluate()
        {
            var findings = new List<AllianceCommunicationChannelDiagnosticCode>();
            if (AudienceProjection == null || (AudienceProjection.MembershipStates.Count == 0 && AudienceProjection.RequiredRoles.Count == 0)) findings.Add(AllianceCommunicationChannelDiagnosticCode.AllianceChannelAudienceMissing);
            if (PermissionRequirement == null || !PermissionRequirement.Present || string.IsNullOrWhiteSpace(PermissionRequirement.PermissionId)) findings.Add(AllianceCommunicationChannelDiagnosticCode.AllianceChannelPermissionMissing);
            if (RuntimeMessagingAllowed) findings.Add(AllianceCommunicationChannelDiagnosticCode.AllianceMessageRuntimeForbidden);
            if (RetentionProjection != null && RetentionProjection.StorageRequested) findings.Add(AllianceCommunicationChannelDiagnosticCode.AllianceMessageStorageForbidden);
            if (ModerationRequirement == null || ModerationRequirement.RequiresServerAuthority) findings.Add(AllianceCommunicationChannelDiagnosticCode.AllianceModerationServerAuthorityRequired);
            if (VisibilityMismatch) findings.Add(AllianceCommunicationChannelDiagnosticCode.AllianceChannelVisibilityMismatch);
            return new AllianceCommunicationChannelDiagnostics(findings);
        }
    }

    public sealed class AllianceCommunicationChannelDiagnostics
    {
        public AllianceCommunicationChannelDiagnostics(IReadOnlyList<AllianceCommunicationChannelDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceCommunicationChannelDiagnosticCode>(); }
        public IReadOnlyList<AllianceCommunicationChannelDiagnosticCode> Findings { get; }
        public bool Contains(AllianceCommunicationChannelDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum DiplomacyRelationshipContractType { Neutral, Peace, NonAggressionPact, Protection, Embargo, Coalition, Federation, WarProjected }
    public enum DiplomacyRelationshipStateDiagnosticCode { DiplomacyPartyMissing, DiplomacyPermissionMissing, DiplomacyStateConflict, DiplomacyEffectRuntimeForbidden, DiplomacyOfficialStateForbidden, DiplomacyServerAuthorityRequired }

    public sealed class DiplomacyPartyProjection
    {
        public DiplomacyPartyProjection(string allianceProjectionId, AllianceRoleKind actingRole)
        {
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            ActingRole = actingRole;
        }

        public string AllianceProjectionId { get; }
        public AllianceRoleKind ActingRole { get; }
    }

    public sealed class DiplomacyProposalProjection
    {
        public DiplomacyProposalProjection(string proposalId, AllianceRoleKind proposedByRole, bool permissionPresent)
        {
            ProposalId = proposalId ?? string.Empty;
            ProposedByRole = proposedByRole;
            PermissionPresent = permissionPresent;
        }

        public string ProposalId { get; }
        public AllianceRoleKind ProposedByRole { get; }
        public bool PermissionPresent { get; }
    }

    public sealed class DiplomacyEffectExpectation
    {
        public DiplomacyEffectExpectation(string effectId, string description, bool runtimeEffectRequested)
        {
            EffectId = effectId ?? string.Empty;
            Description = description ?? string.Empty;
            RuntimeEffectRequested = runtimeEffectRequested;
        }

        public string EffectId { get; }
        public string Description { get; }
        public bool RuntimeEffectRequested { get; }
    }

    public sealed class DiplomacyBetrayalRisk
    {
        public DiplomacyBetrayalRisk(string riskId, string reason, bool open)
        {
            RiskId = riskId ?? string.Empty;
            Reason = reason ?? string.Empty;
            Open = open;
        }

        public string RiskId { get; }
        public string Reason { get; }
        public bool Open { get; }
    }

    public sealed class DiplomacyRelationshipServerAuthorityTopic
    {
        public DiplomacyRelationshipServerAuthorityTopic(string topicId, string owner)
        {
            TopicId = topicId ?? string.Empty;
            Owner = owner ?? string.Empty;
        }

        public string TopicId { get; }
        public string Owner { get; }
    }

    public sealed class DiplomacyRelationshipStateContract
    {
        public DiplomacyRelationshipStateContract(string relationshipId, DiplomacyPartyProjection sourceParty, DiplomacyPartyProjection targetParty, DiplomacyRelationshipContractType relationshipType, DiplomacyProposalProjection proposalProjection, IReadOnlyList<DiplomacyEffectExpectation> expectedEffects, bool compatibilityConflict, IReadOnlyList<DiplomacyBetrayalRisk> betrayalRisks, IReadOnlyList<DiplomacyRelationshipServerAuthorityTopic> serverAuthorityTopics, bool officialRelationshipAllowed)
        {
            RelationshipId = ColonyIntegrationIds.Require(relationshipId);
            SourceParty = sourceParty;
            TargetParty = targetParty;
            RelationshipType = relationshipType;
            ProposalProjection = proposalProjection;
            ExpectedEffects = expectedEffects ?? Array.Empty<DiplomacyEffectExpectation>();
            CompatibilityConflict = compatibilityConflict;
            BetrayalRisks = betrayalRisks ?? Array.Empty<DiplomacyBetrayalRisk>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<DiplomacyRelationshipServerAuthorityTopic>();
            OfficialRelationshipAllowed = officialRelationshipAllowed;
        }

        public string RelationshipId { get; }
        public DiplomacyPartyProjection SourceParty { get; }
        public DiplomacyPartyProjection TargetParty { get; }
        public DiplomacyRelationshipContractType RelationshipType { get; }
        public DiplomacyProposalProjection ProposalProjection { get; }
        public IReadOnlyList<DiplomacyEffectExpectation> ExpectedEffects { get; }
        public bool CompatibilityConflict { get; }
        public IReadOnlyList<DiplomacyBetrayalRisk> BetrayalRisks { get; }
        public IReadOnlyList<DiplomacyRelationshipServerAuthorityTopic> ServerAuthorityTopics { get; }
        public bool OfficialRelationshipAllowed { get; }

        public DiplomacyRelationshipStateDiagnostics Evaluate()
        {
            var findings = new List<DiplomacyRelationshipStateDiagnosticCode>();
            if (SourceParty == null || TargetParty == null || string.IsNullOrWhiteSpace(SourceParty.AllianceProjectionId) || string.IsNullOrWhiteSpace(TargetParty.AllianceProjectionId)) findings.Add(DiplomacyRelationshipStateDiagnosticCode.DiplomacyPartyMissing);
            if (ProposalProjection == null || !ProposalProjection.PermissionPresent) findings.Add(DiplomacyRelationshipStateDiagnosticCode.DiplomacyPermissionMissing);
            if (CompatibilityConflict) findings.Add(DiplomacyRelationshipStateDiagnosticCode.DiplomacyStateConflict);
            if (ExpectedEffects.Any(e => e.RuntimeEffectRequested)) findings.Add(DiplomacyRelationshipStateDiagnosticCode.DiplomacyEffectRuntimeForbidden);
            if (OfficialRelationshipAllowed) findings.Add(DiplomacyRelationshipStateDiagnosticCode.DiplomacyOfficialStateForbidden);
            if (ServerAuthorityTopics.Count > 0) findings.Add(DiplomacyRelationshipStateDiagnosticCode.DiplomacyServerAuthorityRequired);
            return new DiplomacyRelationshipStateDiagnostics(findings);
        }
    }

    public sealed class DiplomacyRelationshipStateDiagnostics
    {
        public DiplomacyRelationshipStateDiagnostics(IReadOnlyList<DiplomacyRelationshipStateDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DiplomacyRelationshipStateDiagnosticCode>(); }
        public IReadOnlyList<DiplomacyRelationshipStateDiagnosticCode> Findings { get; }
        public bool Contains(DiplomacyRelationshipStateDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum WarDeclarationDiagnosticCode { WarDeclarationPermissionMissing, WarDiplomacyStateIncompatible, WarCooldownProjectedActive, WarBeginnerProtectionBlocks, WarHarassmentRiskOpen, WarRuntimeForbidden, WarServerAuthorityRequired }

    public sealed class WarDeclarationRequestProjection
    {
        public WarDeclarationRequestProjection(string sourceAllianceProjectionId, string targetAllianceProjectionId, AllianceRoleKind requestedByRole)
        {
            SourceAllianceProjectionId = sourceAllianceProjectionId ?? string.Empty;
            TargetAllianceProjectionId = targetAllianceProjectionId ?? string.Empty;
            RequestedByRole = requestedByRole;
        }

        public string SourceAllianceProjectionId { get; }
        public string TargetAllianceProjectionId { get; }
        public AllianceRoleKind RequestedByRole { get; }
    }

    public sealed class WarDeclarationCondition
    {
        public WarDeclarationCondition(string conditionId, bool satisfied, string blockingReason)
        {
            ConditionId = conditionId ?? string.Empty;
            Satisfied = satisfied;
            BlockingReason = blockingReason ?? string.Empty;
        }

        public string ConditionId { get; }
        public bool Satisfied { get; }
        public string BlockingReason { get; }
    }

    public sealed class WarCooldownProjection
    {
        public WarCooldownProjection(bool activeProjected, string reason)
        {
            ActiveProjected = activeProjected;
            Reason = reason ?? string.Empty;
        }

        public bool ActiveProjected { get; }
        public string Reason { get; }
    }

    public sealed class WarBeginnerProtectionCheck
    {
        public WarBeginnerProtectionCheck(bool blocksDeclaration, string reason)
        {
            BlocksDeclaration = blocksDeclaration;
            Reason = reason ?? string.Empty;
        }

        public bool BlocksDeclaration { get; }
        public string Reason { get; }
    }

    public sealed class WarHarassmentRiskCheck
    {
        public WarHarassmentRiskCheck(string riskType, bool open)
        {
            RiskType = riskType ?? string.Empty;
            Open = open;
        }

        public string RiskType { get; }
        public bool Open { get; }
    }

    public sealed class WarDeclarationBoundary
    {
        public WarDeclarationBoundary(string boundaryId, WarDeclarationRequestProjection requestProjection, DiplomacyRelationshipContractType diplomacyState, IReadOnlyList<WarDeclarationCondition> conditions, WarCooldownProjection cooldownProjection, WarBeginnerProtectionCheck beginnerProtectionCheck, WarHarassmentRiskCheck harassmentRiskCheck, AllianceCreationVerdictKind verdict, bool permissionPresent, bool runtimeWarRequested, bool serverAuthorityRequired)
        {
            BoundaryId = ColonyIntegrationIds.Require(boundaryId);
            RequestProjection = requestProjection;
            DiplomacyState = diplomacyState;
            Conditions = conditions ?? Array.Empty<WarDeclarationCondition>();
            CooldownProjection = cooldownProjection;
            BeginnerProtectionCheck = beginnerProtectionCheck;
            HarassmentRiskCheck = harassmentRiskCheck;
            Verdict = verdict;
            PermissionPresent = permissionPresent;
            RuntimeWarRequested = runtimeWarRequested;
            ServerAuthorityRequired = serverAuthorityRequired;
        }

        public string BoundaryId { get; }
        public WarDeclarationRequestProjection RequestProjection { get; }
        public DiplomacyRelationshipContractType DiplomacyState { get; }
        public IReadOnlyList<WarDeclarationCondition> Conditions { get; }
        public WarCooldownProjection CooldownProjection { get; }
        public WarBeginnerProtectionCheck BeginnerProtectionCheck { get; }
        public WarHarassmentRiskCheck HarassmentRiskCheck { get; }
        public AllianceCreationVerdictKind Verdict { get; }
        public bool PermissionPresent { get; }
        public bool RuntimeWarRequested { get; }
        public bool ServerAuthorityRequired { get; }

        public WarDeclarationDiagnostics Evaluate()
        {
            var findings = new List<WarDeclarationDiagnosticCode>();
            if (!PermissionPresent) findings.Add(WarDeclarationDiagnosticCode.WarDeclarationPermissionMissing);
            if (DiplomacyState == DiplomacyRelationshipContractType.Protection || DiplomacyState == DiplomacyRelationshipContractType.Federation || DiplomacyState == DiplomacyRelationshipContractType.Coalition) findings.Add(WarDeclarationDiagnosticCode.WarDiplomacyStateIncompatible);
            if (CooldownProjection != null && CooldownProjection.ActiveProjected) findings.Add(WarDeclarationDiagnosticCode.WarCooldownProjectedActive);
            if (BeginnerProtectionCheck != null && BeginnerProtectionCheck.BlocksDeclaration) findings.Add(WarDeclarationDiagnosticCode.WarBeginnerProtectionBlocks);
            if (HarassmentRiskCheck != null && HarassmentRiskCheck.Open) findings.Add(WarDeclarationDiagnosticCode.WarHarassmentRiskOpen);
            if (RuntimeWarRequested) findings.Add(WarDeclarationDiagnosticCode.WarRuntimeForbidden);
            if (ServerAuthorityRequired) findings.Add(WarDeclarationDiagnosticCode.WarServerAuthorityRequired);
            return new WarDeclarationDiagnostics(findings);
        }
    }

    public sealed class WarDeclarationDiagnostics
    {
        public WarDeclarationDiagnostics(IReadOnlyList<WarDeclarationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WarDeclarationDiagnosticCode>(); }
        public IReadOnlyList<WarDeclarationDiagnosticCode> Findings { get; }
        public bool Contains(WarDeclarationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum PvPProtectionPolicyVerdict { Visible, Missing, BlockedByServerAuthority, NeedsQaScenario }
    public enum BeginnerProtectionDiagnosticCode { BeginnerProtectionMissing, DefeatRecoveryProtectionMissing, AttackFrequencyLimitMissing, PowerDisparityUnclassified, RevengeLoopRiskOpen, HarassmentReportServerAuthorityRequired }

    public sealed class PlayerVulnerabilitySignal
    {
        public PlayerVulnerabilitySignal(string signalId, string riskType, bool open)
        {
            SignalId = signalId ?? string.Empty;
            RiskType = riskType ?? string.Empty;
            Open = open;
        }

        public string SignalId { get; }
        public string RiskType { get; }
        public bool Open { get; }
    }

    public sealed class AttackFrequencyLimitProjection
    {
        public AttackFrequencyLimitProjection(bool missing, bool revengeLoopRiskOpen)
        {
            Missing = missing;
            RevengeLoopRiskOpen = revengeLoopRiskOpen;
        }

        public bool Missing { get; }
        public bool RevengeLoopRiskOpen { get; }
    }

    public sealed class PowerDisparityProjection
    {
        public PowerDisparityProjection(bool unclassified, string reason)
        {
            Unclassified = unclassified;
            Reason = reason ?? string.Empty;
        }

        public bool Unclassified { get; }
        public string Reason { get; }
    }

    public sealed class DefeatRecoveryProtectionProjection
    {
        public DefeatRecoveryProtectionProjection(bool missing, string reason)
        {
            Missing = missing;
            Reason = reason ?? string.Empty;
        }

        public bool Missing { get; }
        public string Reason { get; }
    }

    public sealed class HarassmentReportMarker
    {
        public HarassmentReportMarker(bool requiresServerAuthority, string evidence)
        {
            RequiresServerAuthority = requiresServerAuthority;
            Evidence = evidence ?? string.Empty;
        }

        public bool RequiresServerAuthority { get; }
        public string Evidence { get; }
    }

    public sealed class BeginnerProtectionPolicyProjection
    {
        public BeginnerProtectionPolicyProjection(string policyId, bool beginnerShieldMissing, DefeatRecoveryProtectionProjection defeatRecoveryProtection, AttackFrequencyLimitProjection attackFrequencyLimit, PowerDisparityProjection powerDisparity, HarassmentReportMarker harassmentReportMarker, PvPProtectionPolicyVerdict verdict)
        {
            PolicyId = policyId ?? string.Empty;
            BeginnerShieldMissing = beginnerShieldMissing;
            DefeatRecoveryProtection = defeatRecoveryProtection;
            AttackFrequencyLimit = attackFrequencyLimit;
            PowerDisparity = powerDisparity;
            HarassmentReportMarker = harassmentReportMarker;
            Verdict = verdict;
        }

        public string PolicyId { get; }
        public bool BeginnerShieldMissing { get; }
        public DefeatRecoveryProtectionProjection DefeatRecoveryProtection { get; }
        public AttackFrequencyLimitProjection AttackFrequencyLimit { get; }
        public PowerDisparityProjection PowerDisparity { get; }
        public HarassmentReportMarker HarassmentReportMarker { get; }
        public PvPProtectionPolicyVerdict Verdict { get; }

        public BeginnerProtectionDiagnostics Evaluate()
        {
            var findings = new List<BeginnerProtectionDiagnosticCode>();
            if (BeginnerShieldMissing) findings.Add(BeginnerProtectionDiagnosticCode.BeginnerProtectionMissing);
            if (DefeatRecoveryProtection == null || DefeatRecoveryProtection.Missing) findings.Add(BeginnerProtectionDiagnosticCode.DefeatRecoveryProtectionMissing);
            if (AttackFrequencyLimit == null || AttackFrequencyLimit.Missing) findings.Add(BeginnerProtectionDiagnosticCode.AttackFrequencyLimitMissing);
            if (PowerDisparity == null || PowerDisparity.Unclassified) findings.Add(BeginnerProtectionDiagnosticCode.PowerDisparityUnclassified);
            if (AttackFrequencyLimit != null && AttackFrequencyLimit.RevengeLoopRiskOpen) findings.Add(BeginnerProtectionDiagnosticCode.RevengeLoopRiskOpen);
            if (HarassmentReportMarker != null && HarassmentReportMarker.RequiresServerAuthority) findings.Add(BeginnerProtectionDiagnosticCode.HarassmentReportServerAuthorityRequired);
            return new BeginnerProtectionDiagnostics(findings);
        }
    }

    public sealed class AntiHarassmentPolicyProjection
    {
        public AntiHarassmentPolicyProjection(string policyId, string protectedPlayerScope, string riskType, PlayerVulnerabilitySignal projectedSignal, string recommendedBlocker, bool requiresServerAuthority, bool qaScenarioRequired)
        {
            PolicyId = policyId ?? string.Empty;
            ProtectedPlayerScope = protectedPlayerScope ?? string.Empty;
            RiskType = riskType ?? string.Empty;
            ProjectedSignal = projectedSignal;
            RecommendedBlocker = recommendedBlocker ?? string.Empty;
            RequiresServerAuthority = requiresServerAuthority;
            QaScenarioRequired = qaScenarioRequired;
        }

        public string PolicyId { get; }
        public string ProtectedPlayerScope { get; }
        public string RiskType { get; }
        public PlayerVulnerabilitySignal ProjectedSignal { get; }
        public string RecommendedBlocker { get; }
        public bool RequiresServerAuthority { get; }
        public bool QaScenarioRequired { get; }
    }

    public sealed class BeginnerProtectionDiagnostics
    {
        public BeginnerProtectionDiagnostics(IReadOnlyList<BeginnerProtectionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<BeginnerProtectionDiagnosticCode>(); }
        public IReadOnlyList<BeginnerProtectionDiagnosticCode> Findings { get; }
        public bool Contains(BeginnerProtectionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceEconomyRiskType { Duplication, Laundering, UnfairTax, CoerciveDonation, PayToWinBoost, TradeRouteExploit }
    public enum AllianceEconomyDiagnosticCode { AllianceTreasuryRuntimeForbidden, AllianceDonationMutationForbidden, AllianceTaxServerAuthorityRequired, AllianceSharedStoragePersistentForbidden, AllianceEconomyPermissionMissing, AllianceEconomyPayToWinRiskOpen }

    public sealed class AllianceDonationProjection
    {
        public AllianceDonationProjection(string donationId, bool mutationRequested)
        {
            DonationId = donationId ?? string.Empty;
            MutationRequested = mutationRequested;
        }

        public string DonationId { get; }
        public bool MutationRequested { get; }
    }

    public sealed class AllianceSharedStorageProjection
    {
        public AllianceSharedStorageProjection(bool persistentStorageRequested, string limitation)
        {
            PersistentStorageRequested = persistentStorageRequested;
            Limitation = limitation ?? string.Empty;
        }

        public bool PersistentStorageRequested { get; }
        public string Limitation { get; }
    }

    public sealed class AllianceTaxProjection
    {
        public AllianceTaxProjection(string taxId, bool serverAuthorityRequired)
        {
            TaxId = taxId ?? string.Empty;
            ServerAuthorityRequired = serverAuthorityRequired;
        }

        public string TaxId { get; }
        public bool ServerAuthorityRequired { get; }
    }

    public sealed class AllianceTradeRouteProjection
    {
        public AllianceTradeRouteProjection(string routeId, bool runtimeRouteRequested)
        {
            RouteId = routeId ?? string.Empty;
            RuntimeRouteRequested = runtimeRouteRequested;
        }

        public string RouteId { get; }
        public bool RuntimeRouteRequested { get; }
    }

    public sealed class AllianceEconomyPermissionCheck
    {
        public AllianceEconomyPermissionCheck(string permissionId, bool present)
        {
            PermissionId = permissionId ?? string.Empty;
            Present = present;
        }

        public string PermissionId { get; }
        public bool Present { get; }
    }

    public sealed class AllianceEconomyRisk
    {
        public AllianceEconomyRisk(AllianceEconomyRiskType riskType, bool open)
        {
            RiskType = riskType;
            Open = open;
        }

        public AllianceEconomyRiskType RiskType { get; }
        public bool Open { get; }
    }

    public sealed class AllianceTreasuryBoundary
    {
        public AllianceTreasuryBoundary(string allianceProjectionId, double projectedBalance, IReadOnlyList<AllianceDonationProjection> donationProjections, IReadOnlyList<AllianceTaxProjection> taxProjections, AllianceSharedStorageProjection storageProjection, IReadOnlyList<AllianceTradeRouteProjection> tradeRouteProjections, AllianceEconomyPermissionCheck permissionCheck, IReadOnlyList<AllianceEconomyRisk> risks, IReadOnlyList<string> serverAuthorityTopics, bool runtimeTreasuryRequested)
        {
            AllianceProjectionId = allianceProjectionId ?? string.Empty;
            ProjectedBalance = Math.Max(0, projectedBalance);
            DonationProjections = donationProjections ?? Array.Empty<AllianceDonationProjection>();
            TaxProjections = taxProjections ?? Array.Empty<AllianceTaxProjection>();
            StorageProjection = storageProjection;
            TradeRouteProjections = tradeRouteProjections ?? Array.Empty<AllianceTradeRouteProjection>();
            PermissionCheck = permissionCheck;
            Risks = risks ?? Array.Empty<AllianceEconomyRisk>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
            RuntimeTreasuryRequested = runtimeTreasuryRequested;
        }

        public string AllianceProjectionId { get; }
        public double ProjectedBalance { get; }
        public IReadOnlyList<AllianceDonationProjection> DonationProjections { get; }
        public IReadOnlyList<AllianceTaxProjection> TaxProjections { get; }
        public AllianceSharedStorageProjection StorageProjection { get; }
        public IReadOnlyList<AllianceTradeRouteProjection> TradeRouteProjections { get; }
        public AllianceEconomyPermissionCheck PermissionCheck { get; }
        public IReadOnlyList<AllianceEconomyRisk> Risks { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }
        public bool RuntimeTreasuryRequested { get; }

        public AllianceEconomyDiagnostics Evaluate()
        {
            var findings = new List<AllianceEconomyDiagnosticCode>();
            if (RuntimeTreasuryRequested || TradeRouteProjections.Any(r => r.RuntimeRouteRequested)) findings.Add(AllianceEconomyDiagnosticCode.AllianceTreasuryRuntimeForbidden);
            if (DonationProjections.Any(d => d.MutationRequested)) findings.Add(AllianceEconomyDiagnosticCode.AllianceDonationMutationForbidden);
            if (TaxProjections.Any(t => t.ServerAuthorityRequired)) findings.Add(AllianceEconomyDiagnosticCode.AllianceTaxServerAuthorityRequired);
            if (StorageProjection != null && StorageProjection.PersistentStorageRequested) findings.Add(AllianceEconomyDiagnosticCode.AllianceSharedStoragePersistentForbidden);
            if (PermissionCheck == null || !PermissionCheck.Present) findings.Add(AllianceEconomyDiagnosticCode.AllianceEconomyPermissionMissing);
            if (Risks.Any(r => r.Open && r.RiskType == AllianceEconomyRiskType.PayToWinBoost)) findings.Add(AllianceEconomyDiagnosticCode.AllianceEconomyPayToWinRiskOpen);
            return new AllianceEconomyDiagnostics(findings);
        }
    }

    public sealed class AllianceEconomyDiagnostics
    {
        public AllianceEconomyDiagnostics(IReadOnlyList<AllianceEconomyDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceEconomyDiagnosticCode>(); }
        public IReadOnlyList<AllianceEconomyDiagnosticCode> Findings { get; }
        public bool Contains(AllianceEconomyDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum SocialEventCategory { AllianceCreationProjection, MembershipTransition, RolePermissionChangeProjection, DiplomacyProposal, WarDeclarationProjection, ProtectionPolicyWarning, TreasuryIntention, SystemNotification }
    public enum SocialEventPrivacyLevel { Public, Alliance, Leadership, Private, System, Undefined }
    public enum SocialEventJournalDiagnosticCode { SocialEventSourceMissing, SocialEventAudienceUndefined, SocialEventPrivacyMissing, SocialEventStorageForbidden, SocialEventModerationRequired, SocialEventServerAuthorityRequired }

    public sealed class SocialEventSourceReference
    {
        public SocialEventSourceReference(string sourceBee, string sourceReference)
        {
            SourceBee = sourceBee ?? string.Empty;
            SourceReference = sourceReference ?? string.Empty;
        }

        public string SourceBee { get; }
        public string SourceReference { get; }
    }

    public sealed class SocialEventAudienceProjection
    {
        public SocialEventAudienceProjection(string audienceId, bool undefined)
        {
            AudienceId = audienceId ?? string.Empty;
            Undefined = undefined;
        }

        public string AudienceId { get; }
        public bool Undefined { get; }
    }

    public sealed class SocialEventRetentionProjection
    {
        public SocialEventRetentionProjection(bool storageRequested, string policy)
        {
            StorageRequested = storageRequested;
            Policy = policy ?? string.Empty;
        }

        public bool StorageRequested { get; }
        public string Policy { get; }
    }

    public sealed class SocialEventModerationMarker
    {
        public SocialEventModerationMarker(bool moderationRequired, bool serverAuthorityRequired)
        {
            ModerationRequired = moderationRequired;
            ServerAuthorityRequired = serverAuthorityRequired;
        }

        public bool ModerationRequired { get; }
        public bool ServerAuthorityRequired { get; }
    }

    public sealed class SocialEventEntryProjection
    {
        public SocialEventEntryProjection(string eventId, SocialEventCategory category, SocialEventSourceReference sourceReference, SocialEventAudienceProjection audienceProjection, SocialEventPrivacyLevel privacyLevel, SocialEventRetentionProjection retentionProjection, SocialEventModerationMarker moderationMarker, bool officialStorageAllowed)
        {
            EventId = eventId ?? string.Empty;
            Category = category;
            SourceReference = sourceReference;
            AudienceProjection = audienceProjection;
            PrivacyLevel = privacyLevel;
            RetentionProjection = retentionProjection;
            ModerationMarker = moderationMarker;
            OfficialStorageAllowed = officialStorageAllowed;
        }

        public string EventId { get; }
        public SocialEventCategory Category { get; }
        public SocialEventSourceReference SourceReference { get; }
        public SocialEventAudienceProjection AudienceProjection { get; }
        public SocialEventPrivacyLevel PrivacyLevel { get; }
        public SocialEventRetentionProjection RetentionProjection { get; }
        public SocialEventModerationMarker ModerationMarker { get; }
        public bool OfficialStorageAllowed { get; }
    }

    public sealed class SocialEventJournalContract
    {
        public SocialEventJournalContract(string journalId, IReadOnlyList<SocialEventEntryProjection> entries, IReadOnlyList<string> serverAuthorityTopics)
        {
            JournalId = ColonyIntegrationIds.Require(journalId);
            Entries = entries ?? Array.Empty<SocialEventEntryProjection>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
        }

        public string JournalId { get; }
        public IReadOnlyList<SocialEventEntryProjection> Entries { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }

        public SocialEventJournalDiagnostics Evaluate()
        {
            var findings = new List<SocialEventJournalDiagnosticCode>();
            if (Entries.Count == 0 || Entries.Any(e => e.SourceReference == null || string.IsNullOrWhiteSpace(e.SourceReference.SourceBee))) findings.Add(SocialEventJournalDiagnosticCode.SocialEventSourceMissing);
            if (Entries.Any(e => e.AudienceProjection == null || e.AudienceProjection.Undefined)) findings.Add(SocialEventJournalDiagnosticCode.SocialEventAudienceUndefined);
            if (Entries.Any(e => e.PrivacyLevel == SocialEventPrivacyLevel.Undefined)) findings.Add(SocialEventJournalDiagnosticCode.SocialEventPrivacyMissing);
            if (Entries.Any(e => e.OfficialStorageAllowed || (e.RetentionProjection != null && e.RetentionProjection.StorageRequested))) findings.Add(SocialEventJournalDiagnosticCode.SocialEventStorageForbidden);
            if (Entries.Any(e => e.ModerationMarker != null && e.ModerationMarker.ModerationRequired)) findings.Add(SocialEventJournalDiagnosticCode.SocialEventModerationRequired);
            if (ServerAuthorityTopics.Count > 0 || Entries.Any(e => e.ModerationMarker != null && e.ModerationMarker.ServerAuthorityRequired)) findings.Add(SocialEventJournalDiagnosticCode.SocialEventServerAuthorityRequired);
            return new SocialEventJournalDiagnostics(findings);
        }
    }

    public sealed class SocialEventJournalDiagnostics
    {
        public SocialEventJournalDiagnostics(IReadOnlyList<SocialEventJournalDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialEventJournalDiagnosticCode>(); }
        public IReadOnlyList<SocialEventJournalDiagnosticCode> Findings { get; }
        public bool Contains(SocialEventJournalDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceDiplomacyWarFoundationVerdictType { ReadyForArchitectValidation, ReadyWithWarningsForArchitectValidation, NeedsPlannerRevision, BlockedByMissingBeeInput, BlockedByServerAuthorityGap, BlockedByDemoEvidenceGap, BlockedByQaRiskGap, BlockedByBee321Premature }
    public enum AllianceDiplomacyWarFoundationDiagnosticCode { SocialFoundationInputMissing, SocialPillarCoverageMissing, SocialServerAuthorityGapOpen, SocialDemoEvidenceMissing, SocialQaRiskMissing, Bee321Premature }

    public sealed class SocialFoundationInputSet
    {
        public SocialFoundationInputSet(string allianceCreationBoundary, string rolePermissionFramework, string membershipLifecycle, string communicationChannels, string diplomacyStates, string warDeclarationBoundary, string protectionPolicies, string treasuryBoundary, string socialEventJournal)
        {
            AllianceCreationBoundary = allianceCreationBoundary ?? string.Empty;
            RolePermissionFramework = rolePermissionFramework ?? string.Empty;
            MembershipLifecycle = membershipLifecycle ?? string.Empty;
            CommunicationChannels = communicationChannels ?? string.Empty;
            DiplomacyStates = diplomacyStates ?? string.Empty;
            WarDeclarationBoundary = warDeclarationBoundary ?? string.Empty;
            ProtectionPolicies = protectionPolicies ?? string.Empty;
            TreasuryBoundary = treasuryBoundary ?? string.Empty;
            SocialEventJournal = socialEventJournal ?? string.Empty;
        }

        public string AllianceCreationBoundary { get; }
        public string RolePermissionFramework { get; }
        public string MembershipLifecycle { get; }
        public string CommunicationChannels { get; }
        public string DiplomacyStates { get; }
        public string WarDeclarationBoundary { get; }
        public string ProtectionPolicies { get; }
        public string TreasuryBoundary { get; }
        public string SocialEventJournal { get; }

        public bool HasMissingInput()
        {
            return string.IsNullOrWhiteSpace(AllianceCreationBoundary)
                || string.IsNullOrWhiteSpace(RolePermissionFramework)
                || string.IsNullOrWhiteSpace(MembershipLifecycle)
                || string.IsNullOrWhiteSpace(CommunicationChannels)
                || string.IsNullOrWhiteSpace(DiplomacyStates)
                || string.IsNullOrWhiteSpace(WarDeclarationBoundary)
                || string.IsNullOrWhiteSpace(ProtectionPolicies)
                || string.IsNullOrWhiteSpace(TreasuryBoundary)
                || string.IsNullOrWhiteSpace(SocialEventJournal);
        }
    }

    public sealed class SocialFoundationCoverageMatrix
    {
        public SocialFoundationCoverageMatrix(IReadOnlyList<SocialMmoProductPillar> coveredPillars, bool demoEvidencePresent)
        {
            CoveredPillars = coveredPillars ?? Array.Empty<SocialMmoProductPillar>();
            DemoEvidencePresent = demoEvidencePresent;
        }

        public IReadOnlyList<SocialMmoProductPillar> CoveredPillars { get; }
        public bool DemoEvidencePresent { get; }
        public bool Covers(SocialMmoProductPillar pillar) { return CoveredPillars.Contains(pillar); }
    }

    public sealed class SocialFoundationServerAuthorityMatrix
    {
        public SocialFoundationServerAuthorityMatrix(IReadOnlyList<string> topics, bool hasOpenGap)
        {
            Topics = topics ?? Array.Empty<string>();
            HasOpenGap = hasOpenGap;
        }

        public IReadOnlyList<string> Topics { get; }
        public bool HasOpenGap { get; }
    }

    public sealed class SocialFoundationRiskRegister
    {
        public SocialFoundationRiskRegister(IReadOnlyList<string> risks, bool qaRiskMissing)
        {
            Risks = risks ?? Array.Empty<string>();
            QaRiskMissing = qaRiskMissing;
        }

        public IReadOnlyList<string> Risks { get; }
        public bool QaRiskMissing { get; }
    }

    public sealed class SocialFoundationGap
    {
        public SocialFoundationGap(string gapId, string owner, string description)
        {
            GapId = gapId ?? string.Empty;
            Owner = owner ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string GapId { get; }
        public string Owner { get; }
        public string Description { get; }
    }

    public sealed class Bee321BlockerStatus
    {
        public Bee321BlockerStatus(bool prematureAttempt, string message)
        {
            PrematureAttempt = prematureAttempt;
            Message = message ?? string.Empty;
        }

        public bool PrematureAttempt { get; }
        public string Message { get; }
    }

    public sealed class AllianceDiplomacyWarFoundationVerdict
    {
        public AllianceDiplomacyWarFoundationVerdict(AllianceDiplomacyWarFoundationVerdictType verdictType, IReadOnlyList<AllianceDiplomacyWarFoundationDiagnosticCode> diagnostics)
        {
            VerdictType = verdictType;
            Diagnostics = diagnostics ?? Array.Empty<AllianceDiplomacyWarFoundationDiagnosticCode>();
        }

        public AllianceDiplomacyWarFoundationVerdictType VerdictType { get; }
        public IReadOnlyList<AllianceDiplomacyWarFoundationDiagnosticCode> Diagnostics { get; }
        public bool Contains(AllianceDiplomacyWarFoundationDiagnosticCode code) { return Diagnostics.Contains(code); }
    }

    public sealed class AllianceDiplomacyWarFoundationGate
    {
        public const string Bee321BlockedMessage = "BEE-321 bloquee jusqu'a validation architecte.";

        public AllianceDiplomacyWarFoundationGate(string gateId, SocialFoundationInputSet inputSet, SocialFoundationCoverageMatrix pillarCoverage, SocialFoundationServerAuthorityMatrix serverAuthorityMatrix, SocialFoundationRiskRegister riskRegister, IReadOnlyList<SocialFoundationGap> gaps, Bee321BlockerStatus bee321Status)
        {
            GateId = ColonyIntegrationIds.Require(gateId);
            InputSet = inputSet;
            PillarCoverage = pillarCoverage;
            ServerAuthorityMatrix = serverAuthorityMatrix;
            RiskRegister = riskRegister;
            Gaps = gaps ?? Array.Empty<SocialFoundationGap>();
            Bee321Status = bee321Status;
        }

        public string GateId { get; }
        public SocialFoundationInputSet InputSet { get; }
        public SocialFoundationCoverageMatrix PillarCoverage { get; }
        public SocialFoundationServerAuthorityMatrix ServerAuthorityMatrix { get; }
        public SocialFoundationRiskRegister RiskRegister { get; }
        public IReadOnlyList<SocialFoundationGap> Gaps { get; }
        public Bee321BlockerStatus Bee321Status { get; }

        public AllianceDiplomacyWarFoundationVerdict Evaluate()
        {
            var diagnostics = BuildDiagnostics();
            return new AllianceDiplomacyWarFoundationVerdict(ResolveVerdict(diagnostics), diagnostics);
        }

        private IReadOnlyList<AllianceDiplomacyWarFoundationDiagnosticCode> BuildDiagnostics()
        {
            var diagnostics = new List<AllianceDiplomacyWarFoundationDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput()) diagnostics.Add(AllianceDiplomacyWarFoundationDiagnosticCode.SocialFoundationInputMissing);
            if (PillarCoverage == null || !PillarCoverage.Covers(SocialMmoProductPillar.Alliances) || !PillarCoverage.Covers(SocialMmoProductPillar.Diplomacy) || !PillarCoverage.Covers(SocialMmoProductPillar.War) || !PillarCoverage.Covers(SocialMmoProductPillar.PvP) || !PillarCoverage.Covers(SocialMmoProductPillar.Communication) || !PillarCoverage.Covers(SocialMmoProductPillar.Economy)) diagnostics.Add(AllianceDiplomacyWarFoundationDiagnosticCode.SocialPillarCoverageMissing);
            if (ServerAuthorityMatrix == null || ServerAuthorityMatrix.HasOpenGap || ServerAuthorityMatrix.Topics.Count == 0) diagnostics.Add(AllianceDiplomacyWarFoundationDiagnosticCode.SocialServerAuthorityGapOpen);
            if (PillarCoverage == null || !PillarCoverage.DemoEvidencePresent) diagnostics.Add(AllianceDiplomacyWarFoundationDiagnosticCode.SocialDemoEvidenceMissing);
            if (RiskRegister == null || RiskRegister.QaRiskMissing || RiskRegister.Risks.Count == 0) diagnostics.Add(AllianceDiplomacyWarFoundationDiagnosticCode.SocialQaRiskMissing);
            if (Bee321Status != null && Bee321Status.PrematureAttempt) diagnostics.Add(AllianceDiplomacyWarFoundationDiagnosticCode.Bee321Premature);
            return diagnostics;
        }

        private AllianceDiplomacyWarFoundationVerdictType ResolveVerdict(IReadOnlyList<AllianceDiplomacyWarFoundationDiagnosticCode> diagnostics)
        {
            if (diagnostics.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.Bee321Premature)) return AllianceDiplomacyWarFoundationVerdictType.BlockedByBee321Premature;
            if (diagnostics.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.SocialFoundationInputMissing)) return AllianceDiplomacyWarFoundationVerdictType.BlockedByMissingBeeInput;
            if (diagnostics.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.SocialServerAuthorityGapOpen)) return AllianceDiplomacyWarFoundationVerdictType.BlockedByServerAuthorityGap;
            if (diagnostics.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.SocialDemoEvidenceMissing)) return AllianceDiplomacyWarFoundationVerdictType.BlockedByDemoEvidenceGap;
            if (diagnostics.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.SocialQaRiskMissing)) return AllianceDiplomacyWarFoundationVerdictType.BlockedByQaRiskGap;
            if (diagnostics.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.SocialPillarCoverageMissing)) return AllianceDiplomacyWarFoundationVerdictType.NeedsPlannerRevision;
            return Gaps.Count == 0 ? AllianceDiplomacyWarFoundationVerdictType.ReadyForArchitectValidation : AllianceDiplomacyWarFoundationVerdictType.ReadyWithWarningsForArchitectValidation;
        }
    }
}
