using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class AllianceDiplomacyWarFoundationFrameworks311To320Tests
    {
        [Test]
        public void AllianceCreationRolesAndMembership_BlockPersistentOrImplicitRuntimeActions()
        {
            var creation = new AllianceCreationBoundary(
                "creation-a",
                new AllianceCreationRequestProjection(string.Empty, "AB", "A", "desc", "social"),
                Array.Empty<AllianceCreationCondition>(),
                new AllianceNameReservationProjection(duplicateRisk: true, definitiveReservationForbidden: true, "demo"),
                new[] { new AllianceCreationAbuseRisk("risk-a", "spam", open: true, "server moderation") },
                AllianceCreationVerdictKind.BlockedByRuntimeCreationForbidden,
                new[] { new AllianceCreationServerAuthorityTopic("name", "Bee Server", "unique name") },
                persistentCreationRequested: true,
                cooldownProjected: true);

            AllianceCreationDiagnostics creationDiagnostics = creation.Evaluate();
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AllianceCreatorIdentityMissing), Is.True);
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AllianceNameInvalid), Is.True);
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AllianceTagInvalid), Is.True);
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AllianceNameDuplicateRisk), Is.True);
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AllianceCreationCooldownProjected), Is.True);
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AllianceCreationAbuseRiskOpen), Is.True);
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AlliancePersistentCreationForbidden), Is.True);
            Assert.That(creationDiagnostics.Contains(AllianceCreationDiagnosticCode.AllianceCreationServerAuthorityRequired), Is.True);

            var hierarchy = new AllianceRoleHierarchyProjection(
                "alliance-a",
                Array.Empty<AllianceRoleDefinition>(),
                new[]
                {
                    new AlliancePermissionDefinition(string.Empty, "promote", AllianceRoleKind.Leader, AlliancePermissionScope.Mutation, readOnly: false, requiresServerAuthority: true, demoOnlyProjection: true, implicitPermission: true, runtimePermissionRequested: true)
                },
                new[] { new AllianceForbiddenAction("kick", "server only", requested: true) },
                new[] { new AllianceRoleMutationAuthorityTopic("role", "promotion", "Bee Server") });

            AllianceRolePermissionDiagnostics hierarchyDiagnostics = hierarchy.Evaluate();
            Assert.That(hierarchyDiagnostics.Contains(AllianceRolePermissionDiagnosticCode.AllianceRoleMissing), Is.True);
            Assert.That(hierarchyDiagnostics.Contains(AllianceRolePermissionDiagnosticCode.AllianceLeaderAuthorityMissing), Is.True);
            Assert.That(hierarchyDiagnostics.Contains(AllianceRolePermissionDiagnosticCode.AlliancePermissionImplicit), Is.True);
            Assert.That(hierarchyDiagnostics.Contains(AllianceRolePermissionDiagnosticCode.AlliancePermissionRuntimeForbidden), Is.True);
            Assert.That(hierarchyDiagnostics.Contains(AllianceRolePermissionDiagnosticCode.AllianceRoleMutationServerAuthorityRequired), Is.True);
            Assert.That(hierarchyDiagnostics.Contains(AllianceRolePermissionDiagnosticCode.AllianceForbiddenActionRequested), Is.True);

            var membership = new AllianceMembershipLifecycleProjection(
                string.Empty,
                string.Empty,
                AllianceMembershipLifecycleState.ServerAuthorityRequired,
                new AllianceInvitationProjection("invite-a", AllianceRoleKind.Member, permissionPresent: false),
                new AllianceApplicationProjection("app-a", duplicateProjected: true),
                new[] { new AllianceMembershipTransition(AllianceMembershipLifecycleState.AcceptedProjected, AllianceMembershipLifecycleState.RemovedProjected, AllianceRoleKind.Officer, "remove", requiresServerAuthority: true, "server") },
                new AllianceMembershipCooldownProjection(activeProjected: true, "recent leave"),
                Array.Empty<AllianceMembershipHistoryEntry>(),
                persistentMembershipRequested: true);

            AllianceMembershipLifecycleDiagnostics membershipDiagnostics = membership.Evaluate();
            Assert.That(membershipDiagnostics.Contains(AllianceMembershipLifecycleDiagnosticCode.AllianceMembershipStateInvalid), Is.True);
            Assert.That(membershipDiagnostics.Contains(AllianceMembershipLifecycleDiagnosticCode.AllianceInvitationPermissionMissing), Is.True);
            Assert.That(membershipDiagnostics.Contains(AllianceMembershipLifecycleDiagnosticCode.AllianceApplicationDuplicateProjected), Is.True);
            Assert.That(membershipDiagnostics.Contains(AllianceMembershipLifecycleDiagnosticCode.AllianceMembershipCooldownActiveProjected), Is.True);
            Assert.That(membershipDiagnostics.Contains(AllianceMembershipLifecycleDiagnosticCode.AllianceRemovalServerAuthorityRequired), Is.True);
            Assert.That(membershipDiagnostics.Contains(AllianceMembershipLifecycleDiagnosticCode.AlliancePersistentMembershipForbidden), Is.True);
        }

        [Test]
        public void CommunicationDiplomacyAndWar_BlockChatOfficialRelationsAndRuntimeWar()
        {
            var channel = new AllianceCommunicationChannelContract(
                "channel-a",
                AllianceChannelContractType.AllianceGeneral,
                new AllianceChannelAudienceProjection(Array.Empty<AllianceMembershipLifecycleState>(), Array.Empty<AllianceRoleKind>(), Array.Empty<AllianceRoleKind>(), string.Empty),
                new AllianceChannelPermissionRequirement(string.Empty, present: false),
                new AllianceModerationRequirement("harassment", requiresServerAuthority: true),
                new AllianceMessageRetentionProjection(storageRequested: true, "future"),
                runtimeMessagingAllowed: true,
                visibilityMismatch: true);

            AllianceCommunicationChannelDiagnostics channelDiagnostics = channel.Evaluate();
            Assert.That(channelDiagnostics.Contains(AllianceCommunicationChannelDiagnosticCode.AllianceChannelAudienceMissing), Is.True);
            Assert.That(channelDiagnostics.Contains(AllianceCommunicationChannelDiagnosticCode.AllianceChannelPermissionMissing), Is.True);
            Assert.That(channelDiagnostics.Contains(AllianceCommunicationChannelDiagnosticCode.AllianceMessageRuntimeForbidden), Is.True);
            Assert.That(channelDiagnostics.Contains(AllianceCommunicationChannelDiagnosticCode.AllianceMessageStorageForbidden), Is.True);
            Assert.That(channelDiagnostics.Contains(AllianceCommunicationChannelDiagnosticCode.AllianceModerationServerAuthorityRequired), Is.True);
            Assert.That(channelDiagnostics.Contains(AllianceCommunicationChannelDiagnosticCode.AllianceChannelVisibilityMismatch), Is.True);

            var diplomacy = new DiplomacyRelationshipStateContract(
                "diplomacy-a",
                new DiplomacyPartyProjection(string.Empty, AllianceRoleKind.Member),
                null,
                DiplomacyRelationshipContractType.Federation,
                new DiplomacyProposalProjection("proposal-a", AllianceRoleKind.Member, permissionPresent: false),
                new[] { new DiplomacyEffectExpectation("effect-a", "embargo", runtimeEffectRequested: true) },
                compatibilityConflict: true,
                Array.Empty<DiplomacyBetrayalRisk>(),
                new[] { new DiplomacyRelationshipServerAuthorityTopic("treaty", "Bee Server") },
                officialRelationshipAllowed: true);

            DiplomacyRelationshipStateDiagnostics diplomacyDiagnostics = diplomacy.Evaluate();
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipStateDiagnosticCode.DiplomacyPartyMissing), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipStateDiagnosticCode.DiplomacyPermissionMissing), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipStateDiagnosticCode.DiplomacyStateConflict), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipStateDiagnosticCode.DiplomacyEffectRuntimeForbidden), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipStateDiagnosticCode.DiplomacyOfficialStateForbidden), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipStateDiagnosticCode.DiplomacyServerAuthorityRequired), Is.True);

            var war = new WarDeclarationBoundary(
                "war-a",
                new WarDeclarationRequestProjection("alliance-a", "alliance-b", AllianceRoleKind.Member),
                DiplomacyRelationshipContractType.Protection,
                Array.Empty<WarDeclarationCondition>(),
                new WarCooldownProjection(activeProjected: true, "cooldown"),
                new WarBeginnerProtectionCheck(blocksDeclaration: true, "new player"),
                new WarHarassmentRiskCheck("repeatedTargeting", open: true),
                AllianceCreationVerdictKind.BlockedByServerAuthorityRequired,
                permissionPresent: false,
                runtimeWarRequested: true,
                serverAuthorityRequired: true);

            WarDeclarationDiagnostics warDiagnostics = war.Evaluate();
            Assert.That(warDiagnostics.Contains(WarDeclarationDiagnosticCode.WarDeclarationPermissionMissing), Is.True);
            Assert.That(warDiagnostics.Contains(WarDeclarationDiagnosticCode.WarDiplomacyStateIncompatible), Is.True);
            Assert.That(warDiagnostics.Contains(WarDeclarationDiagnosticCode.WarCooldownProjectedActive), Is.True);
            Assert.That(warDiagnostics.Contains(WarDeclarationDiagnosticCode.WarBeginnerProtectionBlocks), Is.True);
            Assert.That(warDiagnostics.Contains(WarDeclarationDiagnosticCode.WarHarassmentRiskOpen), Is.True);
            Assert.That(warDiagnostics.Contains(WarDeclarationDiagnosticCode.WarRuntimeForbidden), Is.True);
            Assert.That(warDiagnostics.Contains(WarDeclarationDiagnosticCode.WarServerAuthorityRequired), Is.True);
        }

        [Test]
        public void ProtectionEconomyJournalAndGate_BlockServerAuthorityGaps()
        {
            var protection = new BeginnerProtectionPolicyProjection(
                "protection-a",
                beginnerShieldMissing: true,
                new DefeatRecoveryProtectionProjection(missing: true, "no recovery"),
                new AttackFrequencyLimitProjection(missing: true, revengeLoopRiskOpen: true),
                new PowerDisparityProjection(unclassified: true, "unknown"),
                new HarassmentReportMarker(requiresServerAuthority: true, "report"),
                PvPProtectionPolicyVerdict.NeedsQaScenario);

            BeginnerProtectionDiagnostics protectionDiagnostics = protection.Evaluate();
            Assert.That(protectionDiagnostics.Contains(BeginnerProtectionDiagnosticCode.BeginnerProtectionMissing), Is.True);
            Assert.That(protectionDiagnostics.Contains(BeginnerProtectionDiagnosticCode.DefeatRecoveryProtectionMissing), Is.True);
            Assert.That(protectionDiagnostics.Contains(BeginnerProtectionDiagnosticCode.AttackFrequencyLimitMissing), Is.True);
            Assert.That(protectionDiagnostics.Contains(BeginnerProtectionDiagnosticCode.PowerDisparityUnclassified), Is.True);
            Assert.That(protectionDiagnostics.Contains(BeginnerProtectionDiagnosticCode.RevengeLoopRiskOpen), Is.True);
            Assert.That(protectionDiagnostics.Contains(BeginnerProtectionDiagnosticCode.HarassmentReportServerAuthorityRequired), Is.True);

            var treasury = new AllianceTreasuryBoundary(
                "alliance-a",
                100,
                new[] { new AllianceDonationProjection("donation-a", mutationRequested: true) },
                new[] { new AllianceTaxProjection("tax-a", serverAuthorityRequired: true) },
                new AllianceSharedStorageProjection(persistentStorageRequested: true, "server"),
                new[] { new AllianceTradeRouteProjection("route-a", runtimeRouteRequested: true) },
                new AllianceEconomyPermissionCheck("treasury", present: false),
                new[] { new AllianceEconomyRisk(AllianceEconomyRiskType.PayToWinBoost, open: true) },
                new[] { "treasury" },
                runtimeTreasuryRequested: true);

            AllianceEconomyDiagnostics treasuryDiagnostics = treasury.Evaluate();
            Assert.That(treasuryDiagnostics.Contains(AllianceEconomyDiagnosticCode.AllianceTreasuryRuntimeForbidden), Is.True);
            Assert.That(treasuryDiagnostics.Contains(AllianceEconomyDiagnosticCode.AllianceDonationMutationForbidden), Is.True);
            Assert.That(treasuryDiagnostics.Contains(AllianceEconomyDiagnosticCode.AllianceTaxServerAuthorityRequired), Is.True);
            Assert.That(treasuryDiagnostics.Contains(AllianceEconomyDiagnosticCode.AllianceSharedStoragePersistentForbidden), Is.True);
            Assert.That(treasuryDiagnostics.Contains(AllianceEconomyDiagnosticCode.AllianceEconomyPermissionMissing), Is.True);
            Assert.That(treasuryDiagnostics.Contains(AllianceEconomyDiagnosticCode.AllianceEconomyPayToWinRiskOpen), Is.True);

            var journal = new SocialEventJournalContract(
                "journal-a",
                new[]
                {
                    new SocialEventEntryProjection("event-a", SocialEventCategory.WarDeclarationProjection, new SocialEventSourceReference(string.Empty, "war"), new SocialEventAudienceProjection(string.Empty, undefined: true), SocialEventPrivacyLevel.Undefined, new SocialEventRetentionProjection(storageRequested: true, "future"), new SocialEventModerationMarker(moderationRequired: true, serverAuthorityRequired: true), officialStorageAllowed: true)
                },
                new[] { "history" });

            SocialEventJournalDiagnostics journalDiagnostics = journal.Evaluate();
            Assert.That(journalDiagnostics.Contains(SocialEventJournalDiagnosticCode.SocialEventSourceMissing), Is.True);
            Assert.That(journalDiagnostics.Contains(SocialEventJournalDiagnosticCode.SocialEventAudienceUndefined), Is.True);
            Assert.That(journalDiagnostics.Contains(SocialEventJournalDiagnosticCode.SocialEventPrivacyMissing), Is.True);
            Assert.That(journalDiagnostics.Contains(SocialEventJournalDiagnosticCode.SocialEventStorageForbidden), Is.True);
            Assert.That(journalDiagnostics.Contains(SocialEventJournalDiagnosticCode.SocialEventModerationRequired), Is.True);
            Assert.That(journalDiagnostics.Contains(SocialEventJournalDiagnosticCode.SocialEventServerAuthorityRequired), Is.True);

            var gate = new AllianceDiplomacyWarFoundationGate(
                "gate-a",
                new SocialFoundationInputSet("creation", "roles", "membership", "channels", "diplomacy", "war", "protection", "treasury", "journal"),
                new SocialFoundationCoverageMatrix(new[] { SocialMmoProductPillar.Alliances, SocialMmoProductPillar.Diplomacy, SocialMmoProductPillar.War, SocialMmoProductPillar.PvP, SocialMmoProductPillar.Communication, SocialMmoProductPillar.Economy }, demoEvidencePresent: true),
                new SocialFoundationServerAuthorityMatrix(new[] { "war" }, hasOpenGap: true),
                new SocialFoundationRiskRegister(new[] { "war harassment" }, qaRiskMissing: false),
                Array.Empty<SocialFoundationGap>(),
                new Bee321BlockerStatus(prematureAttempt: true, AllianceDiplomacyWarFoundationGate.Bee321BlockedMessage));

            AllianceDiplomacyWarFoundationVerdict verdict = gate.Evaluate();
            Assert.That(verdict.VerdictType, Is.EqualTo(AllianceDiplomacyWarFoundationVerdictType.BlockedByBee321Premature));
            Assert.That(verdict.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.SocialServerAuthorityGapOpen), Is.True);
            Assert.That(verdict.Contains(AllianceDiplomacyWarFoundationDiagnosticCode.Bee321Premature), Is.True);
        }
    }
}
