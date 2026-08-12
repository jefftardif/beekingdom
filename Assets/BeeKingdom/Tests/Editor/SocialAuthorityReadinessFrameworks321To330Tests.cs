using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class SocialAuthorityReadinessFrameworks321To330Tests
    {
        [Test]
        public void ServerIntakePersistenceAndAbuse_BlockRuntimeAndUnownedOfficialState()
        {
            var intake = new SocialServerImpactIntake("intake-a", new[]
            {
                new SocialServerImpactTopic(string.Empty, string.Empty, Array.Empty<SocialMmoProductPillar>(), null, new SocialServerImpactRisk("risk", "abuse", true), SocialServerReadinessStatus.WaitingForBeeServerScan, "demo", "qa")
            }, beeServerScanPending: true, runtimeImplementationRequested: true, serverSpecCreationRequested: true);

            SocialServerImpactDiagnostics intakeDiagnostics = intake.Evaluate();
            Assert.That(intakeDiagnostics.Contains(SocialServerImpactDiagnosticCode.SocialServerTopicMissing), Is.True);
            Assert.That(intakeDiagnostics.Contains(SocialServerImpactDiagnosticCode.SocialServerScanPending), Is.True);
            Assert.That(intakeDiagnostics.Contains(SocialServerImpactDiagnosticCode.SocialAuthorityReasonMissing), Is.True);
            Assert.That(intakeDiagnostics.Contains(SocialServerImpactDiagnosticCode.SocialRuntimeImplementationForbidden), Is.True);
            Assert.That(intakeDiagnostics.Contains(SocialServerImpactDiagnosticCode.ServerSpecCreationForbidden), Is.True);

            var persistence = new AlliancePersistenceBoundaryContract("alliance-a", new[]
            {
                new AllianceOfficialIdentityCandidate("name", string.Empty, serverAuthoritative: false)
            }, Array.Empty<string>(), Array.Empty<AlliancePersistentMemberRecordCandidate>(), Array.Empty<AlliancePersistentRoleRecordCandidate>(), Array.Empty<AllianceAuditHistoryCandidate>(), new[]
            {
                new AlliancePersistenceAuthorityGap("gap", string.Empty, "owner missing")
            }, string.Empty, localPersistenceRequested: true, persistenceAllowed: true);

            AlliancePersistenceDiagnostics persistenceDiagnostics = persistence.Evaluate();
            Assert.That(persistenceDiagnostics.Contains(AlliancePersistenceDiagnosticCode.AlliancePersistentFieldUnowned), Is.True);
            Assert.That(persistenceDiagnostics.Contains(AlliancePersistenceDiagnosticCode.AllianceLocalPersistenceForbidden), Is.True);
            Assert.That(persistenceDiagnostics.Contains(AlliancePersistenceDiagnosticCode.AllianceOfficialIdentityServerRequired), Is.True);
            Assert.That(persistenceDiagnostics.Contains(AlliancePersistenceDiagnosticCode.AllianceAuditHistoryServerRequired), Is.True);
            Assert.That(persistenceDiagnostics.Contains(AlliancePersistenceDiagnosticCode.AlliancePersistenceVersionMissing), Is.True);

            var abuse = new AlliancePermissionAbuseAudit("audit-a", new[]
            {
                new AllianceAbuseScenario("scenario-a", new AllianceSensitivePermission("kick", AllianceRoleKind.Officer, audited: false), Array.Empty<string>(), "kickAbuse", new AllianceAbuseEvidenceRequirement("evidence", missing: true), new AllianceAbuseServerOwner(string.Empty, "moderation"), AllianceAbuseAuditVerdict.MutationForbidden, permissionMutationRequested: true)
            });

            AlliancePermissionAbuseDiagnostics abuseDiagnostics = abuse.Evaluate();
            Assert.That(abuseDiagnostics.Contains(AlliancePermissionAbuseDiagnosticCode.SensitiveAlliancePermissionUnaudited), Is.True);
            Assert.That(abuseDiagnostics.Contains(AlliancePermissionAbuseDiagnosticCode.AllianceAbuseEvidenceMissing), Is.True);
            Assert.That(abuseDiagnostics.Contains(AlliancePermissionAbuseDiagnosticCode.AllianceAbuseVictimScopeMissing), Is.True);
            Assert.That(abuseDiagnostics.Contains(AlliancePermissionAbuseDiagnosticCode.AllianceAbuseServerOwnerMissing), Is.True);
            Assert.That(abuseDiagnostics.Contains(AlliancePermissionAbuseDiagnosticCode.AlliancePermissionMutationForbidden), Is.True);
        }

        [Test]
        public void NegotiationRallyAndArmy_BlockOfficialAcceptanceMobilizationAndPersistentUnits()
        {
            var negotiation = new DiplomacyNegotiationFlowContract("negotiation-a", "a", "b", DiplomacyRelationshipContractType.Protection, DiplomacyNegotiationState.BetrayalRiskFlagged, new[]
            {
                new DiplomacyOfferProjection(string.Empty, DiplomacyRelationshipContractType.Protection, valid: false)
            }, new[]
            {
                new DiplomacyCounterOfferProjection("counter", conflict: true)
            }, new DiplomacyNegotiationExpiryProjection(expiredProjected: true, "expired"), new[]
            {
                new DiplomacyBetrayalFlag("betrayal", open: true)
            }, new[] { "official treaty" }, permissionPresent: false, officialAcceptanceRequested: true);

            DiplomacyNegotiationDiagnostics negotiationDiagnostics = negotiation.Evaluate();
            Assert.That(negotiationDiagnostics.Contains(DiplomacyNegotiationDiagnosticCode.DiplomacyNegotiationPermissionMissing), Is.True);
            Assert.That(negotiationDiagnostics.Contains(DiplomacyNegotiationDiagnosticCode.DiplomacyOfferInvalid), Is.True);
            Assert.That(negotiationDiagnostics.Contains(DiplomacyNegotiationDiagnosticCode.DiplomacyCounterOfferConflict), Is.True);
            Assert.That(negotiationDiagnostics.Contains(DiplomacyNegotiationDiagnosticCode.DiplomacyNegotiationExpiredProjected), Is.True);
            Assert.That(negotiationDiagnostics.Contains(DiplomacyNegotiationDiagnosticCode.DiplomacyOfficialAcceptanceForbidden), Is.True);
            Assert.That(negotiationDiagnostics.Contains(DiplomacyNegotiationDiagnosticCode.DiplomacyBetrayalRiskOpen), Is.True);

            var rally = new WarRallyPlanningBoundary("rally-a", "alliance", new WarRallyTargetProjection(string.Empty, valid: false), AllianceRoleKind.Member, Array.Empty<WarRallyParticipantProjection>(), new WarRallyWindowProjection("window", conflict: true), new[]
            {
                new WarRallyProtectionCheck(beginnerProtectionBlocks: true, "new player")
            }, WarRallyReadinessVerdict.RuntimeMobilizationForbidden, permissionPresent: false, runtimeMobilizationRequested: true, serverAuthorityRequired: true);

            WarRallyPlanningDiagnostics rallyDiagnostics = rally.Evaluate();
            Assert.That(rallyDiagnostics.Contains(WarRallyPlanningDiagnosticCode.WarRallyPermissionMissing), Is.True);
            Assert.That(rallyDiagnostics.Contains(WarRallyPlanningDiagnosticCode.WarRallyTargetInvalid), Is.True);
            Assert.That(rallyDiagnostics.Contains(WarRallyPlanningDiagnosticCode.WarRallyWindowConflict), Is.True);
            Assert.That(rallyDiagnostics.Contains(WarRallyPlanningDiagnosticCode.WarRallyBeginnerProtectionBlocks), Is.True);
            Assert.That(rallyDiagnostics.Contains(WarRallyPlanningDiagnosticCode.WarRallyRuntimeMobilizationForbidden), Is.True);
            Assert.That(rallyDiagnostics.Contains(WarRallyPlanningDiagnosticCode.WarRallyServerAuthorityRequired), Is.True);

            var army = new ArmyTrainingQueueReadinessProjection("player-a", new[]
            {
                new ArmyTrainingSlotProjection("slot-a", ArmyTrainingLifecycleStage.TrainingPlanning, runtimeQueueRequested: true, persistentUnitRequested: true)
            }, new[]
            {
                new ArmyTrainingCapacityConstraint("capacity", missing: true)
            }, new[]
            {
                new ArmyTrainingCostProjection("cost", nonFinalBalance: true)
            }, Array.Empty<ArmyReadinessSocialSignal>(), new[]
            {
                new ArmyTrainingBalanceRisk("p2w", payToWinOpen: true)
            }, new[] { "army server" });

            ArmyTrainingQueueDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyTrainingQueueDiagnosticCode.ArmyTrainingQueueRuntimeForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingQueueDiagnosticCode.ArmyPersistentUnitForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingQueueDiagnosticCode.ArmyTrainingCostNonFinal), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingQueueDiagnosticCode.ArmyCapacityConstraintMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingQueueDiagnosticCode.ArmyPayToWinRiskOpen), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingQueueDiagnosticCode.ArmyServerAuthorityRequired), Is.True);
        }

        [Test]
        public void RecoveryModerationTerritoryAndGate_BlockFinalRuntimeClaims()
        {
            var recovery = new DefeatRecoveryRetentionProtectionProjection("player-a", "defeat", new[]
            {
                new ProjectedLossLimit("loss", runtimeLossRequested: true, nonFinal: true)
            }, new PostDefeatVulnerabilityWindow("window", repeatedAttackRisk: true), new[]
            {
                new RecoveryAssistanceSignal("help", compensationServerRequired: true)
            }, new[]
            {
                new RetentionRiskIndicator("risk", unclassified: true)
            }, new[] { "compensation" }, protectionMissing: true, defeatHistoryStorageRequested: true);

            DefeatRecoveryDiagnostics recoveryDiagnostics = recovery.Evaluate();
            Assert.That(recoveryDiagnostics.Contains(DefeatRecoveryDiagnosticCode.DefeatLossRuntimeForbidden), Is.True);
            Assert.That(recoveryDiagnostics.Contains(DefeatRecoveryDiagnosticCode.RecoveryProtectionMissing), Is.True);
            Assert.That(recoveryDiagnostics.Contains(DefeatRecoveryDiagnosticCode.RetentionRiskUnclassified), Is.True);
            Assert.That(recoveryDiagnostics.Contains(DefeatRecoveryDiagnosticCode.RepeatedAttackAfterDefeatRisk), Is.True);
            Assert.That(recoveryDiagnostics.Contains(DefeatRecoveryDiagnosticCode.CompensationServerAuthorityRequired), Is.True);
            Assert.That(recoveryDiagnostics.Contains(DefeatRecoveryDiagnosticCode.DefeatHistoryStorageForbidden), Is.True);

            var moderation = new SocialModerationReportBoundary("report-a", ModerationReportType.Missing, "reporter", "target", new ModerationEvidenceProjection("evidence", missing: true), ModerationReportStatusProjection.SanctionForbiddenLocally, new[]
            {
                new ModerationPrivacyRuleProjection("privacy", missing: true)
            }, new[]
            {
                new ModerationSanctionAuthorityTopic("sanction", serverRequired: true)
            }, sanctionRuntimeRequested: true, moderationHistoryStorageRequested: true);

            SocialModerationDiagnostics moderationDiagnostics = moderation.Evaluate();
            Assert.That(moderationDiagnostics.Contains(SocialModerationDiagnosticCode.ModerationReportTypeMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(SocialModerationDiagnosticCode.ModerationEvidenceMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(SocialModerationDiagnosticCode.ModerationPrivacyRuleMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(SocialModerationDiagnosticCode.ModerationSanctionRuntimeForbidden), Is.True);
            Assert.That(moderationDiagnostics.Contains(SocialModerationDiagnosticCode.ModerationHistoryStorageForbidden), Is.True);
            Assert.That(moderationDiagnostics.Contains(SocialModerationDiagnosticCode.ModerationServerAuthorityRequired), Is.True);

            var territory = new AllianceTerritoryClaimProjection("claim-a", "alliance", new TerritoryClaimRegionReference(string.Empty, missing: true), TerritoryClaimStatus.OfficialTerritoryForbidden, new[]
            {
                new TerritoryClaimConflictProjection("conflict", open: true)
            }, new[]
            {
                new TerritoryClaimBenefitExpectation("bonus", runtimeBenefitRequested: true)
            }, new[]
            {
                new TerritoryClaimRisk("snowball", snowballOpen: true)
            }, new[]
            {
                new TerritoryClaimServerAuthorityTopic("territory", serverRequired: true)
            });

            TerritoryClaimDiagnostics territoryDiagnostics = territory.Evaluate();
            Assert.That(territoryDiagnostics.Contains(TerritoryClaimDiagnosticCode.TerritoryClaimRegionMissing), Is.True);
            Assert.That(territoryDiagnostics.Contains(TerritoryClaimDiagnosticCode.TerritoryOfficialClaimForbidden), Is.True);
            Assert.That(territoryDiagnostics.Contains(TerritoryClaimDiagnosticCode.TerritoryClaimConflictOpen), Is.True);
            Assert.That(territoryDiagnostics.Contains(TerritoryClaimDiagnosticCode.TerritoryBenefitRuntimeForbidden), Is.True);
            Assert.That(territoryDiagnostics.Contains(TerritoryClaimDiagnosticCode.TerritorySnowballRiskOpen), Is.True);
            Assert.That(territoryDiagnostics.Contains(TerritoryClaimDiagnosticCode.TerritoryServerAuthorityRequired), Is.True);

            var gate = new SocialAuthorityReadinessGate("gate-a", new SocialAuthorityInputSet("intake", "persistence", "abuse", "diplomacy", "rally", "army", "recovery", "moderation", "territory"), new SocialAuthorityCoverageMatrix(new[]
            {
                SocialMmoProductPillar.Alliances,
                SocialMmoProductPillar.Diplomacy,
                SocialMmoProductPillar.War,
                SocialMmoProductPillar.PvP,
                SocialMmoProductPillar.Communication,
                SocialMmoProductPillar.Army,
                SocialMmoProductPillar.PlayerProgression
            }, demoEvidencePresent: true), new[]
            {
                new SocialAuthorityServerGap("server", "pvp", open: true)
            }, new SocialAuthorityRiskRegister(new[] { "harassment" }), new SocialPlayerProtectionCoverage(true, true, true), new Bee331BlockerStatus(prematureAttempt: true, SocialAuthorityReadinessGate.Bee331BlockedMessage));

            SocialAuthorityReadinessVerdict verdict = gate.Evaluate();
            Assert.That(verdict.VerdictType, Is.EqualTo(SocialAuthorityReadinessVerdictType.BlockedByBee331Premature));
            Assert.That(verdict.Contains(SocialAuthorityReadinessDiagnosticCode.SocialAuthorityServerGapOpen), Is.True);
            Assert.That(verdict.Contains(SocialAuthorityReadinessDiagnosticCode.Bee331Premature), Is.True);
        }
    }
}
