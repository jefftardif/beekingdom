using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class CooperativePvpReadinessFrameworks331To340Tests
    {
        [Test]
        public void ObjectivesLedgerAndMissions_BlockRewardsCreditsAndRuntimeCommands()
        {
            var board = new AllianceObjectiveBoardContract("board-a", string.Empty, new[]
            {
                new AllianceObjectiveProjection(string.Empty, AllianceObjectiveType.RallyParticipationProjection, AllianceRoleKind.Officer, null, rewardAllowed: true, persistenceAllowed: true)
            }, Array.Empty<AllianceObjectiveVisibilityRule>(), new[]
            {
                new AllianceObjectiveSocialPressureRisk("pressure", open: true)
            }, new[]
            {
                new AllianceObjectiveServerAuthorityTopic("reward", serverRequired: true)
            });

            AllianceObjectiveDiagnostics boardDiagnostics = board.Evaluate();
            Assert.That(boardDiagnostics.Contains(AllianceObjectiveDiagnosticCode.AllianceObjectiveSourceMissing), Is.True);
            Assert.That(boardDiagnostics.Contains(AllianceObjectiveDiagnosticCode.AllianceObjectiveRewardForbidden), Is.True);
            Assert.That(boardDiagnostics.Contains(AllianceObjectiveDiagnosticCode.AllianceObjectiveProgressPersistenceForbidden), Is.True);
            Assert.That(boardDiagnostics.Contains(AllianceObjectiveDiagnosticCode.AllianceObjectiveSocialPressureRiskOpen), Is.True);
            Assert.That(boardDiagnostics.Contains(AllianceObjectiveDiagnosticCode.AllianceObjectiveServerAuthorityRequired), Is.True);

            var ledger = new CooperativeContributionLedgerBoundary("ledger-a", new[]
            {
                new ContributionEntryProjection("entry-a", "player", new ContributionSourceReference("objective", string.Empty), ContributionType.ProjectedDonation, new ContributionRecognitionProjection("badge", officialCreditAllowed: true), bankMutationRequested: true, new[]
                {
                    new ContributionAbuseRisk("payToWin", open: true),
                    new ContributionAbuseRisk("coercion", open: true)
                })
            }, new[] { new ContributionServerAuthorityTopic("credit", serverRequired: true) });

            ContributionLedgerDiagnostics ledgerDiagnostics = ledger.Evaluate();
            Assert.That(ledgerDiagnostics.Contains(ContributionDiagnosticCode.ContributionSourceMissing), Is.True);
            Assert.That(ledgerDiagnostics.Contains(ContributionDiagnosticCode.ContributionOfficialCreditForbidden), Is.True);
            Assert.That(ledgerDiagnostics.Contains(ContributionDiagnosticCode.ContributionBankMutationForbidden), Is.True);
            Assert.That(ledgerDiagnostics.Contains(ContributionDiagnosticCode.ContributionPayToWinRiskOpen), Is.True);
            Assert.That(ledgerDiagnostics.Contains(ContributionDiagnosticCode.ContributionCoercionRiskOpen), Is.True);
            Assert.That(ledgerDiagnostics.Contains(ContributionDiagnosticCode.ContributionServerAuthorityRequired), Is.True);

            var mission = new AllianceMissionCoordinationProjection("mission-a", string.Empty, AllianceMissionType.RallyPreparation, new AllianceMissionPriorityProjection("high", 10), new[]
            {
                new AllianceMissionParticipantProjection("player", voluntary: false)
            }, new[]
            {
                new AllianceMissionAssignmentIntent("assign", runtimeCommandRequested: true, forcedAssignmentRisk: true)
            }, new[]
            {
                new AllianceMissionCoordinationGap("chat", open: true)
            }, new[]
            {
                new AllianceMissionServerAuthorityTopic("mission", serverRequired: true)
            }, permissionPresent: false);

            AllianceMissionDiagnostics missionDiagnostics = mission.Evaluate();
            Assert.That(missionDiagnostics.Contains(AllianceMissionDiagnosticCode.AllianceMissionObjectiveMissing), Is.True);
            Assert.That(missionDiagnostics.Contains(AllianceMissionDiagnosticCode.AllianceMissionPermissionMissing), Is.True);
            Assert.That(missionDiagnostics.Contains(AllianceMissionDiagnosticCode.AllianceMissionRuntimeCommandForbidden), Is.True);
            Assert.That(missionDiagnostics.Contains(AllianceMissionDiagnosticCode.AllianceMissionForcedAssignmentRiskOpen), Is.True);
            Assert.That(missionDiagnostics.Contains(AllianceMissionDiagnosticCode.AllianceMissionCoordinationGapOpen), Is.True);
            Assert.That(missionDiagnostics.Contains(AllianceMissionDiagnosticCode.AllianceMissionServerAuthorityRequired), Is.True);
        }

        [Test]
        public void ArmyRallyLossAndFairness_BlockOfficialPvpRuntime()
        {
            var composition = new ArmyCompositionPreviewBoundary("player", new[]
            {
                new ArmyUnitFamilyPreview(ArmyUnitFamily.Soldiers, string.Empty, persistentCompositionRequested: true)
            }, new ArmyRoleBalanceProjection("balance", combatPowerOfficialRequested: true), Array.Empty<ArmyStrengthWeaknessProjection>(), Array.Empty<ArmyCompositionReadinessSignal>(), new[]
            {
                new ArmyCompositionBalanceRisk("risk", open: true, matchmakingImpactServerRequired: true)
            }, new[] { "army" });

            ArmyCompositionDiagnostics compositionDiagnostics = composition.Evaluate();
            Assert.That(compositionDiagnostics.Contains(ArmyCompositionDiagnosticCode.ArmyUnitFamilyPreviewMissing), Is.True);
            Assert.That(compositionDiagnostics.Contains(ArmyCompositionDiagnosticCode.ArmyPersistentCompositionForbidden), Is.True);
            Assert.That(compositionDiagnostics.Contains(ArmyCompositionDiagnosticCode.ArmyCombatPowerOfficialForbidden), Is.True);
            Assert.That(compositionDiagnostics.Contains(ArmyCompositionDiagnosticCode.ArmyBalanceRiskOpen), Is.True);
            Assert.That(compositionDiagnostics.Contains(ArmyCompositionDiagnosticCode.ArmyMatchmakingImpactServerRequired), Is.True);
            Assert.That(compositionDiagnostics.Contains(ArmyCompositionDiagnosticCode.ArmyCompositionServerAuthorityRequired), Is.True);

            var commitment = new RallyParticipantCommitmentProjection(string.Empty, string.Empty, RallyCommitmentState.ServerAuthorityRequired, new RallyCommitmentWindow("window", expiredProjected: true), new RallyWithdrawalProjection(withdrawnProjected: true, windowExpiredProjected: true), new[]
            {
                new RallyParticipationWarning("protection", open: true)
            }, new[] { "rally" }, consentPresent: false, mobilizationRequested: true);

            RallyCommitmentDiagnostics commitmentDiagnostics = commitment.Evaluate();
            Assert.That(commitmentDiagnostics.Contains(RallyCommitmentDiagnosticCode.RallyCommitmentMissing), Is.True);
            Assert.That(commitmentDiagnostics.Contains(RallyCommitmentDiagnosticCode.RallyParticipantConsentMissing), Is.True);
            Assert.That(commitmentDiagnostics.Contains(RallyCommitmentDiagnosticCode.RallyWithdrawalWindowExpiredProjected), Is.True);
            Assert.That(commitmentDiagnostics.Contains(RallyCommitmentDiagnosticCode.RallyProtectionWarningOpen), Is.True);
            Assert.That(commitmentDiagnostics.Contains(RallyCommitmentDiagnosticCode.RallyMobilizationForbidden), Is.True);
            Assert.That(commitmentDiagnostics.Contains(RallyCommitmentDiagnosticCode.RallyCommitmentServerAuthorityRequired), Is.True);

            var loss = new PvPLossBudgetBoundary("target", new[]
            {
                new ProjectedLossCategory("loot", runtimeLossRequested: true)
            }, new[]
            {
                new ProjectedLootLimit("limit", nonFinal: true)
            }, new RecoveryBudgetProjection("recovery", missing: true), new[]
            {
                new FrustrationRiskSignal("risk", frustrationOpen: true, farmingOpen: true)
            }, new[] { new PvPLossServerAuthorityTopic("loss", serverRequired: true) });

            PvPLossBudgetDiagnostics lossDiagnostics = loss.Evaluate();
            Assert.That(lossDiagnostics.Contains(PvPLossBudgetDiagnosticCode.PvPLossRuntimeForbidden), Is.True);
            Assert.That(lossDiagnostics.Contains(PvPLossBudgetDiagnosticCode.PvPLootLimitNonFinal), Is.True);
            Assert.That(lossDiagnostics.Contains(PvPLossBudgetDiagnosticCode.PvPRecoveryBudgetMissing), Is.True);
            Assert.That(lossDiagnostics.Contains(PvPLossBudgetDiagnosticCode.PvPFrustrationRiskOpen), Is.True);
            Assert.That(lossDiagnostics.Contains(PvPLossBudgetDiagnosticCode.PvPFarmingRiskOpen), Is.True);
            Assert.That(lossDiagnostics.Contains(PvPLossBudgetDiagnosticCode.PvPLossServerAuthorityRequired), Is.True);

            var fairness = new AntiSnowballFairnessPolicy("fairness", new[]
            {
                new PowerGapSignalProjection("power", nonFinalBalance: true)
            }, new[] { new RepeatedFarmingRiskProjection("farm", open: true) }, new[]
            {
                new EconomicAdvantageRiskProjection("economy", economicSnowballOpen: true, payToWinOpen: true)
            }, new[] { new TerritoryDominanceRiskProjection("territory", open: true) }, new[]
            {
                new FairnessWarningProjection("warning", serverAuthorityRequired: true)
            }, new[] { "fairness" });

            AntiSnowballDiagnostics fairnessDiagnostics = fairness.Evaluate();
            Assert.That(fairnessDiagnostics.Contains(AntiSnowballDiagnosticCode.PowerGapThresholdNonFinal), Is.True);
            Assert.That(fairnessDiagnostics.Contains(AntiSnowballDiagnosticCode.RepeatedFarmingRiskOpen), Is.True);
            Assert.That(fairnessDiagnostics.Contains(AntiSnowballDiagnosticCode.EconomicSnowballRiskOpen), Is.True);
            Assert.That(fairnessDiagnostics.Contains(AntiSnowballDiagnosticCode.PayToWinFairnessRiskOpen), Is.True);
            Assert.That(fairnessDiagnostics.Contains(AntiSnowballDiagnosticCode.TerritoryDominanceRiskOpen), Is.True);
            Assert.That(fairnessDiagnostics.Contains(AntiSnowballDiagnosticCode.FairnessServerAuthorityRequired), Is.True);
        }

        [Test]
        public void HelpWarReadinessAndGate_BlockTransfersOfficialWarAndBee341()
        {
            var help = new AllianceHelpRequestFlowContract("help-a", new[]
            {
                new AllianceHelpRequestProjection(string.Empty, "player", "alliance", HelpRequestType.Missing, new HelpRequestPriorityProjection("p", 1), new[]
                {
                    new HelpResponseProjection("response", resourceDeliveryRequested: true, teleportRequested: true)
                }, new HelpRequestExpiryProjection(expiredProjected: true), new[]
                {
                    new HelpRequestAbuseRisk("spam", open: true),
                    new HelpRequestAbuseRisk("moderation", open: true)
                }, new[] { "help" })
            });

            AllianceHelpRequestDiagnostics helpDiagnostics = help.Evaluate();
            Assert.That(helpDiagnostics.Contains(HelpRequestDiagnosticCode.HelpRequestTypeMissing), Is.True);
            Assert.That(helpDiagnostics.Contains(HelpRequestDiagnosticCode.HelpRequestSpamRiskOpen), Is.True);
            Assert.That(helpDiagnostics.Contains(HelpRequestDiagnosticCode.HelpRequestResourceDeliveryForbidden), Is.True);
            Assert.That(helpDiagnostics.Contains(HelpRequestDiagnosticCode.HelpRequestTeleportForbidden), Is.True);
            Assert.That(helpDiagnostics.Contains(HelpRequestDiagnosticCode.HelpRequestModerationRiskOpen), Is.True);
            Assert.That(helpDiagnostics.Contains(HelpRequestDiagnosticCode.HelpRequestServerAuthorityRequired), Is.True);

            var readiness = new WarReadinessSignalProjection(string.Empty, Array.Empty<AllianceWarReadinessComponent>(), new[]
            {
                new ArmyReadinessComponent("army", WarReadinessSignalLevel.Blocked)
            }, Array.Empty<TerritoryReadinessComponent>(), new[]
            {
                new ProtectionReadinessComponent("protection", protectionGapOpen: true, fairnessGapOpen: true)
            }, new[] { new WarReadinessGap("gap", open: true) }, Array.Empty<string>(), new[]
            {
                new WarReadinessServerAuthorityTopic("war", serverRequired: true)
            }, officialWarAllowed: true);

            WarReadinessDiagnostics readinessDiagnostics = readiness.Evaluate();
            Assert.That(readinessDiagnostics.Contains(WarReadinessDiagnosticCode.WarReadinessComponentMissing), Is.True);
            Assert.That(readinessDiagnostics.Contains(WarReadinessDiagnosticCode.WarReadinessSignalNonOfficial), Is.True);
            Assert.That(readinessDiagnostics.Contains(WarReadinessDiagnosticCode.WarReadinessProtectionGapOpen), Is.True);
            Assert.That(readinessDiagnostics.Contains(WarReadinessDiagnosticCode.WarReadinessFairnessGapOpen), Is.True);
            Assert.That(readinessDiagnostics.Contains(WarReadinessDiagnosticCode.WarDeclarationForbidden), Is.True);
            Assert.That(readinessDiagnostics.Contains(WarReadinessDiagnosticCode.WarReadinessServerAuthorityRequired), Is.True);

            var gate = new CooperativePvPReadinessGate("gate-a", new CooperativePvPInputSet("objectives", "ledger", "missions", "army", "rally", "loss", "fairness", "help", "war"), new CooperativeCoverageMatrix(objectiveGapOpen: false, demoEvidencePresent: true), new PvPFairnessCoverageMatrix(fairnessGapOpen: true), new PlayerProtectionReadinessMatrix(recoveryGapOpen: true), new CooperativePvPRiskRegister(new[] { "farming" }, warReadinessGapOpen: true), new Bee341BlockerStatus(prematureAttempt: true, CooperativePvPReadinessGate.Bee341BlockedMessage));

            CooperativePvPReadinessVerdict verdict = gate.Evaluate();
            Assert.That(verdict.VerdictType, Is.EqualTo(CooperativePvPReadinessVerdictType.BlockedByBee341Premature));
            Assert.That(verdict.Contains(CooperativePvPReadinessDiagnosticCode.PvPFairnessGapOpen), Is.True);
            Assert.That(verdict.Contains(CooperativePvPReadinessDiagnosticCode.PlayerRecoveryGapOpen), Is.True);
            Assert.That(verdict.Contains(CooperativePvPReadinessDiagnosticCode.WarReadinessGapOpen), Is.True);
            Assert.That(verdict.Contains(CooperativePvPReadinessDiagnosticCode.Bee341Premature), Is.True);
        }
    }
}
