using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class SocialMmoMilestoneClosureFrameworks341To350Tests
    {
        [Test]
        public void EvidenceMatrixAndCooperationReadModel_SurfaceGapsAndForbiddenMutation()
        {
            var matrix = new SocialMmoPillarEvidenceMatrix("matrix-a", new[]
            {
                new SocialMmoPillarEvidenceEntry(SocialMmoProductPillar.Alliances, string.Empty, "demo", null, new SocialMmoQaRiskReference("qa", mapped: false), null, SocialMmoPillarEvidenceStatus.GapOpen, runtimeReadyClaimed: true)
            }, new[]
            {
                new SocialMmoPillarGap("gap", SocialMmoProductPillar.PvP, open: true)
            });

            SocialMmoPillarEvidenceDiagnostics matrixDiagnostics = matrix.Evaluate();
            Assert.That(matrixDiagnostics.Contains(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoPillarEvidenceMissing), Is.True);
            Assert.That(matrixDiagnostics.Contains(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoPillarGapOpen), Is.True);
            Assert.That(matrixDiagnostics.Contains(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoRuntimeEvidenceForbidden), Is.True);
            Assert.That(matrixDiagnostics.Contains(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoServerDependencyMissing), Is.True);
            Assert.That(matrixDiagnostics.Contains(SocialMmoPillarEvidenceDiagnosticCode.SocialMmoQaRiskUnmapped), Is.True);

            var cooperation = new AllianceCooperationDemoReadModel(string.Empty, Array.Empty<AllianceCooperationObjectiveView>(), new[]
            {
                new AllianceCooperationContributionView("BEE-332", "contribution", rewardRequested: true)
            }, new[]
            {
                new AllianceCooperationMissionView("BEE-333", "mission", mutationRequested: true)
            }, new[]
            {
                new AllianceCooperationHelpRequestView("BEE-338", "help", deliveryRequested: true)
            }, new[]
            {
                new AllianceCooperationDemoLimit("limit", declared: false)
            }, officialGameplayAllowed: true);

            AllianceCooperationReadModelDiagnostics cooperationDiagnostics = cooperation.Evaluate();
            Assert.That(cooperationDiagnostics.Contains(AllianceCooperationDemoDiagnosticCode.AllianceCooperationDemoInputMissing), Is.True);
            Assert.That(cooperationDiagnostics.Contains(AllianceCooperationDemoDiagnosticCode.AllianceCooperationReadModelMutationForbidden), Is.True);
            Assert.That(cooperationDiagnostics.Contains(AllianceCooperationDemoDiagnosticCode.AllianceCooperationRewardForbidden), Is.True);
            Assert.That(cooperationDiagnostics.Contains(AllianceCooperationDemoDiagnosticCode.AllianceCooperationDeliveryForbidden), Is.True);
            Assert.That(cooperationDiagnostics.Contains(AllianceCooperationDemoDiagnosticCode.AllianceCooperationDemoLimitMissing), Is.True);
        }

        [Test]
        public void ArmyPvpAndServerEscalation_BlockOfficialRuntimeAndServerCreation()
        {
            var army = new ArmyReadinessRiskRegister("army-risk", new[]
            {
                new ArmyReadinessRiskEntry("risk", string.Empty, string.Empty, ArmyReadinessRiskSeverity.High, "impact", new ArmyDemoRiskVisibility("DEMO-012", visible: true), new ArmyQaScenarioNeed("qa", missing: true), new ArmyServerAuthorityRisk("army", open: true))
            });

            ArmyReadinessRiskDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyReadinessRiskDiagnosticCode.ArmyRiskSourceMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessRiskDiagnosticCode.ArmyRiskSeverityMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessRiskDiagnosticCode.ArmyPayToWinRiskUntracked), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessRiskDiagnosticCode.ArmySnowballRiskUntracked), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessRiskDiagnosticCode.ArmyServerAuthorityRiskOpen), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessRiskDiagnosticCode.ArmyQaScenarioMissing), Is.True);

            var pvp = new FairPvpScenarioCatalog("pvp-catalog", new[]
            {
                new FairPvPScenarioEntry(string.Empty, "revenge", Array.Empty<string>(), "harassment", new[]
                {
                    new FairPvPProtectionExpectation("beginner", present: false)
                }, new[]
                {
                    new FairPvPFailureMode("farming", mapped: false)
                }, new FairPvPDemoEvidenceNeed("demo", present: false), new FairPvPServerAuthorityTopic("matchmaking", serverRequired: true), runtimeExecutionAllowed: true, matchmakingRequested: true, rewardRequested: true)
            });

            FairPvpScenarioDiagnostics pvpDiagnostics = pvp.Evaluate();
            Assert.That(pvpDiagnostics.Contains(FairPvpScenarioDiagnosticCode.FairPvPScenarioMissing), Is.True);
            Assert.That(pvpDiagnostics.Contains(FairPvpScenarioDiagnosticCode.FairPvPProtectionExpectationMissing), Is.True);
            Assert.That(pvpDiagnostics.Contains(FairPvpScenarioDiagnosticCode.FairPvPFailureModeUnmapped), Is.True);
            Assert.That(pvpDiagnostics.Contains(FairPvpScenarioDiagnosticCode.FairPvPMatchmakingForbidden), Is.True);
            Assert.That(pvpDiagnostics.Contains(FairPvpScenarioDiagnosticCode.FairPvPRewardForbidden), Is.True);
            Assert.That(pvpDiagnostics.Contains(FairPvpScenarioDiagnosticCode.FairPvPServerAuthorityRequired), Is.True);

            var server = new SocialServerEscalationBundle("server-bundle", new[]
            {
                new SocialServerEscalationItem("item", string.Empty, "PvP loss and rewards", "loss exploit", new SocialServerOwnerHint(string.Empty), new SocialServerBlockerReason("SERVER-018 forbidden"), SocialServerEscalationStatus.RequiresBeeServerScan)
            }, new SocialServerImplementationForbiddenMarker(implementationRequested: true, server018Requested: true));

            SocialServerEscalationDiagnostics serverDiagnostics = server.Evaluate();
            Assert.That(serverDiagnostics.Contains(SocialServerEscalationDiagnosticCode.SocialServerSourceBeeMissing), Is.True);
            Assert.That(serverDiagnostics.Contains(SocialServerEscalationDiagnosticCode.SocialServerOwnerMissing), Is.True);
            Assert.That(serverDiagnostics.Contains(SocialServerEscalationDiagnosticCode.SocialServerScanRequired), Is.True);
            Assert.That(serverDiagnostics.Contains(SocialServerEscalationDiagnosticCode.SocialServerImplementationForbidden), Is.True);
            Assert.That(serverDiagnostics.Contains(SocialServerEscalationDiagnosticCode.Server018CreationForbidden), Is.True);
        }

        [Test]
        public void RetentionWarDemoMilestoneAndClosure_KeepAlphaNotReadyAndBee351Blocked()
        {
            var retention = new PlayerRetentionAfterConflictProjection(string.Empty, string.Empty, new PostConflictRecoveryPath("path", missing: true), Array.Empty<PostConflictMotivationSignal>(), new[]
            {
                new PostConflictChurnRisk("churn", open: true)
            }, new PostConflictAllianceSupportReference("support", rewardRequested: true, compensationRequested: true), new[]
            {
                new PostConflictServerAuthorityTopic("protection", serverRequired: true)
            });

            PlayerRetentionAfterConflictDiagnostics retentionDiagnostics = retention.Evaluate();
            Assert.That(retentionDiagnostics.Contains(PlayerRetentionAfterConflictDiagnosticCode.PostConflictRecoveryPathMissing), Is.True);
            Assert.That(retentionDiagnostics.Contains(PlayerRetentionAfterConflictDiagnosticCode.PostConflictChurnRiskOpen), Is.True);
            Assert.That(retentionDiagnostics.Contains(PlayerRetentionAfterConflictDiagnosticCode.PostConflictRewardForbidden), Is.True);
            Assert.That(retentionDiagnostics.Contains(PlayerRetentionAfterConflictDiagnosticCode.PostConflictCompensationForbidden), Is.True);
            Assert.That(retentionDiagnostics.Contains(PlayerRetentionAfterConflictDiagnosticCode.PostConflictProtectionServerRequired), Is.True);

            var war = new AllianceWarCoordinationReadinessMatrix("war-matrix", new[]
            {
                new WarCoordinationReadinessEntry("rally", string.Empty, WarCoordinationReadinessStatus.ServerRequired, new[]
                {
                    new WarCoordinationDependency("mission", missing: true)
                }, new[]
                {
                    new WarCoordinationGap("fairness", open: true)
                }, new WarCoordinationServerAuthorityTopic("war", serverRequired: true), protectionPresent: false)
            }, new WarCoordinationRuntimeForbiddenMarker(warDeclarationRequested: true, mobilizationRequested: true));

            AllianceWarCoordinationReadinessDiagnostics warDiagnostics = war.Evaluate();
            Assert.That(warDiagnostics.Contains(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationInputMissing), Is.True);
            Assert.That(warDiagnostics.Contains(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationGapOpen), Is.True);
            Assert.That(warDiagnostics.Contains(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationRuntimeForbidden), Is.True);
            Assert.That(warDiagnostics.Contains(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationProtectionMissing), Is.True);
            Assert.That(warDiagnostics.Contains(AllianceWarCoordinationReadinessDiagnosticCode.WarCoordinationServerAuthorityRequired), Is.True);

            var snapshot = new SocialMmoDemoAcceptanceSnapshot("snapshot", new[] { new SocialMmoDemoVisibleProof("proof", visible: false) }, new[] { new SocialMmoDemoGap("gap", hidden: true) }, new[] { new SocialMmoDemoWarning("runtime", runtimeClaim: true) }, new SocialMmoDemoExternalObserverChecklist("check", complete: false), new[] { new SocialMmoDemoLimit("limit", declared: false) }, alphaReadyClaimAllowed: true, separateDemoSpecRequested: true);
            SocialMmoDemoAcceptanceDiagnostics snapshotDiagnostics = snapshot.Evaluate();
            Assert.That(snapshotDiagnostics.Contains(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoProofMissing), Is.True);
            Assert.That(snapshotDiagnostics.Contains(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoGapHidden), Is.True);
            Assert.That(snapshotDiagnostics.Contains(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoRuntimeClaimForbidden), Is.True);
            Assert.That(snapshotDiagnostics.Contains(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoLimitMissing), Is.True);
            Assert.That(snapshotDiagnostics.Contains(SocialMmoDemoAcceptanceDiagnosticCode.SocialMmoDemoSeparateSpecForbidden), Is.True);

            var milestone = new SocialMmoMilestoneProjection("milestone", string.Empty, Array.Empty<SocialMmoMilestoneAchievement>(), new[] { new SocialMmoMilestoneOpenRisk("risk", string.Empty) }, Array.Empty<SocialMmoMilestoneNextGate>(), new SocialMmoMilestoneOwnerMap(new[] { "QA" }, serverOwnerPresent: false), alphaReady: true);
            SocialMmoMilestoneDiagnostics milestoneDiagnostics = milestone.Evaluate();
            Assert.That(milestoneDiagnostics.Contains(SocialMmoMilestoneDiagnosticCode.SocialMmoMilestoneInputMissing), Is.True);
            Assert.That(milestoneDiagnostics.Contains(SocialMmoMilestoneDiagnosticCode.SocialMmoMilestoneRiskUnowned), Is.True);
            Assert.That(milestoneDiagnostics.Contains(SocialMmoMilestoneDiagnosticCode.SocialMmoNextGateMissing), Is.True);
            Assert.That(milestoneDiagnostics.Contains(SocialMmoMilestoneDiagnosticCode.SocialMmoAlphaReadyClaimForbidden), Is.True);
            Assert.That(milestoneDiagnostics.Contains(SocialMmoMilestoneDiagnosticCode.SocialMmoServerOwnerMissing), Is.True);

            var closure = new SocialMmoAlphaDirectionClosureGate("gate", null, new SocialMmoClosureCoverage(evidenceGapOpen: true, demoHonestyGapOpen: true), new SocialMmoClosureRiskRegister(qaRiskGapOpen: true, serverReadinessGapOpen: true), new SocialMmoClosureOwnerMap(qaOwnerPresent: false, serverOwnerPresent: false, demoOwnerPresent: false), alphaReady: true, new Bee351BlockerStatus(prematureAttempt: true, SocialMmoAlphaDirectionClosureGate.Bee351BlockedMessage));
            SocialMmoAlphaDirectionClosureDiagnostics closureDiagnostics = closure.Evaluate();
            Assert.That(closureDiagnostics.Verdict, Is.EqualTo(SocialMmoClosureVerdict.BlockedByBee351Premature));
            Assert.That(closureDiagnostics.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoClosureInputMissing), Is.True);
            Assert.That(closureDiagnostics.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoEvidenceGapOpen), Is.True);
            Assert.That(closureDiagnostics.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoServerReadinessGapOpen), Is.True);
            Assert.That(closureDiagnostics.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoDemoHonestyGapOpen), Is.True);
            Assert.That(closureDiagnostics.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.SocialMmoAlphaReadyForbidden), Is.True);
            Assert.That(closureDiagnostics.Contains(SocialMmoAlphaDirectionClosureDiagnosticCode.Bee351Premature), Is.True);
        }
    }
}
