using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class SocialMmoReviewConsoleFrameworks371To380Tests
    {
        [Test]
        public void ReviewConsoleFreshnessGovernanceAndPrivacy_BlockUnsafeReviewClaims()
        {
            var console = new SocialMmoReviewConsoleBoundary("console", new[]
            {
                new SocialMmoReviewConsoleInput(string.Empty, SocialMmoReviewPanelType.QaIntakeSummary, Array.Empty<string>(), new SocialMmoReviewConsoleOwnerRef(string.Empty), "unknown", Array.Empty<SocialMmoReviewConsoleGap>(), Array.Empty<string>(), new[] { new SocialMmoReviewConsoleBlockedAction("admin", mutationRequested: true, liveAdminRequested: true, localTruthClaimed: true) }, SocialMmoReviewPanelState.Missing)
            }, Array.Empty<SocialMmoReviewConsolePanel>());
            ReviewConsoleDiagnostics consoleDiagnostics = console.Evaluate();
            Assert.That(consoleDiagnostics.Contains(ReviewConsoleDiagnosticCode.ReviewConsoleInputMissing), Is.True);
            Assert.That(consoleDiagnostics.Contains(ReviewConsoleDiagnosticCode.ReviewConsolePanelUnowned), Is.True);
            Assert.That(consoleDiagnostics.Contains(ReviewConsoleDiagnosticCode.ReviewConsoleMutationForbidden), Is.True);
            Assert.That(consoleDiagnostics.Contains(ReviewConsoleDiagnosticCode.ReviewConsoleLiveAdminForbidden), Is.True);
            Assert.That(consoleDiagnostics.Contains(ReviewConsoleDiagnosticCode.ReviewConsoleLocalTruthRisk), Is.True);

            var audit = new SocialMmoEvidenceFreshnessAudit("audit", new[]
            {
                new SocialMmoEvidenceSourceRef("evidence", string.Empty, string.Empty, "BEE-372", string.Empty, SocialMmoEvidenceAgeBand.Obsolete, SocialMmoEvidenceReliability.Unknown, new[] { new SocialMmoEvidenceInvalidationReason("server", obsolete: true, serverAuditRequired: true) })
            });
            EvidenceFreshnessDiagnostics auditDiagnostics = audit.Evaluate();
            Assert.That(auditDiagnostics.Contains(EvidenceFreshnessDiagnosticCode.EvidenceSourceMissing), Is.True);
            Assert.That(auditDiagnostics.Contains(EvidenceFreshnessDiagnosticCode.EvidenceOwnerMissing), Is.True);
            Assert.That(auditDiagnostics.Contains(EvidenceFreshnessDiagnosticCode.EvidenceFreshnessUnknown), Is.True);
            Assert.That(auditDiagnostics.Contains(EvidenceFreshnessDiagnosticCode.EvidenceObsolete), Is.True);
            Assert.That(auditDiagnostics.Contains(EvidenceFreshnessDiagnosticCode.EvidenceServerAuditRequired), Is.True);

            var export = new AlliancePvpGovernanceExport("export", "console", new[] { new GovernanceExportDecisionItem("decision", officialVerdictClaimed: true, containsSensitiveData: true) }, Array.Empty<GovernanceExportBlockerItem>(), new[] { new GovernanceExportOwnerAssignment(string.Empty, GovernanceExportRecipient.ServerReviewer) }, new[] { new GovernanceExportServerDependency("server", missing: true) }, new GovernanceExportRedactionPolicy(required: true, applied: false), GovernanceExportStatus.BlockedByPrivacy);
            GovernanceExportDiagnostics exportDiagnostics = export.Evaluate();
            Assert.That(exportDiagnostics.Contains(GovernanceExportDiagnosticCode.GovernanceExportSensitiveDataBlocked), Is.True);
            Assert.That(exportDiagnostics.Contains(GovernanceExportDiagnosticCode.GovernanceExportOfficialVerdictForbidden), Is.True);
            Assert.That(exportDiagnostics.Contains(GovernanceExportDiagnosticCode.GovernanceExportServerDependencyMissing), Is.True);
            Assert.That(exportDiagnostics.Contains(GovernanceExportDiagnosticCode.GovernanceExportOwnerMissing), Is.True);

            var privacy = new SocialMmoSensitiveEvidenceBoundary("privacy", new[]
            {
                new SensitiveEvidenceClassification(string.Empty, SensitiveEvidenceClass.Unclassified, new[] { new SensitiveEvidenceRedactionRule("mask", applied: false) }, Array.Empty<GovernanceExportRecipient>(), new[] { "DEMO-012" }, new[] { new SensitiveEvidenceDisclosureRisk("victim", victimExposure: true) }, "server-only", new[] { new SensitiveEvidenceBlockedUse("sanction", sanctionRequested: true, demoRequested: true) })
            });
            SensitiveEvidenceDiagnostics privacyDiagnostics = privacy.Evaluate();
            Assert.That(privacyDiagnostics.Contains(SensitiveEvidenceDiagnosticCode.SensitiveEvidenceUnclassified), Is.True);
            Assert.That(privacyDiagnostics.Contains(SensitiveEvidenceDiagnosticCode.VictimExposureRisk), Is.True);
            Assert.That(privacyDiagnostics.Contains(SensitiveEvidenceDiagnosticCode.SensitiveEvidenceSanctionForbidden), Is.True);
            Assert.That(privacyDiagnostics.Contains(SensitiveEvidenceDiagnosticCode.SensitiveEvidenceDemoForbidden), Is.True);
        }

        [Test]
        public void ArmyLiveOpsModerationDecisionAndBurnDown_BlockRuntimeAndReadinessClaims()
        {
            var army = new ArmyCompetitionReadinessReview("army", new[] { new ArmyCompetitionReadinessInput("signal", officialPowerScoreClaimed: true) }, null, Array.Empty<ArmyCompetitionFairnessRisk>(), new[] { new ArmyCompetitionMissingCondition("condition") }, Array.Empty<ArmyCompetitionAbuseGuard>(), new[] { new ArmyCompetitionServerBlocker("combat", open: true) }, ArmyCompetitionReviewVerdict.BlockedByOfficialScoreClaim, matchmakingRequested: true, combatRuntimeRequested: true);
            ArmyCompetitionReadinessDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyCompetitionDiagnosticCode.ArmyCompetitionEvidenceMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyCompetitionDiagnosticCode.ArmyCompetitionPowerScoreForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyCompetitionDiagnosticCode.ArmyCompetitionMatchmakingForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyCompetitionDiagnosticCode.ArmyCompetitionCombatRuntimeForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyCompetitionDiagnosticCode.ArmyCompetitionServerReviewRequired), Is.True);

            var liveOps = new LiveOpsCandidateReviewBoard("liveops", new[]
            {
                new LiveOpsCandidateReviewCard("candidate", null, "alliance", "world", Array.Empty<LiveOpsCandidateOperationalRisk>(), Array.Empty<string>(), Array.Empty<string>(), LiveOpsCandidateNonExecutionStatus.NotExecutable, string.Empty, new[] { new LiveOpsCandidateExecutionBlocker("exec", rewardRequested: true, calendarRequested: true, monetizationRequested: true, executionRequested: true) })
            });
            LiveOpsCandidateReviewDiagnostics liveOpsDiagnostics = liveOps.Evaluate();
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsReviewDiagnosticCode.LiveOpsReviewCandidateMissingValue), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsReviewDiagnosticCode.LiveOpsReviewRewardForbidden), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsReviewDiagnosticCode.LiveOpsReviewCalendarForbidden), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsReviewDiagnosticCode.LiveOpsReviewMonetizationForbidden), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsReviewDiagnosticCode.LiveOpsReviewExecutionForbidden), Is.True);

            var moderation = new ModerationHandoffEvidenceBundle("moderation", new[] { "warning" }, Array.Empty<string>(), new ModerationHandoffRedactionProfile(applied: false), Array.Empty<ModerationHandoffFalsePositiveNote>(), new[] { new ModerationHandoffConfidentialityFlag("victim", victimExposureRisk: true) }, Array.Empty<string>(), new[] { new ModerationHandoffServerReviewNeed("server", required: true) }, ModerationHandoffNonSanctionStatus.ServerReviewRequired, sanctionRequested: true);
            ModerationHandoffDiagnostics moderationDiagnostics = moderation.Evaluate();
            Assert.That(moderationDiagnostics.Contains(ModerationHandoffDiagnosticCode.ModerationHandoffRedactionMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationHandoffDiagnosticCode.ModerationHandoffVictimExposureRisk), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationHandoffDiagnosticCode.ModerationHandoffSanctionForbidden), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationHandoffDiagnosticCode.ModerationHandoffFalsePositiveMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationHandoffDiagnosticCode.ModerationHandoffServerReviewRequired), Is.True);

            var decisions = new SocialMmoDecisionLogProjection("decisions", new[]
            {
                new SocialMmoDecisionEntry("decision", SocialMmoDecisionType.ServerDependency, Array.Empty<SocialMmoDecisionSourceRef>(), "BEE-378", new SocialMmoDecisionOwner(string.Empty), Array.Empty<SocialMmoProductPillar>(), null, new SocialMmoDecisionNonRuntimeFlag(liveHistoryClaimed: true, officialAuditClaimed: true))
            });
            DecisionLogDiagnostics decisionDiagnostics = decisions.Evaluate();
            Assert.That(decisionDiagnostics.Contains(DecisionLogDiagnosticCode.DecisionLogSourceMissing), Is.True);
            Assert.That(decisionDiagnostics.Contains(DecisionLogDiagnosticCode.DecisionLogOwnerMissing), Is.True);
            Assert.That(decisionDiagnostics.Contains(DecisionLogDiagnosticCode.DecisionLogLiveHistoryForbidden), Is.True);
            Assert.That(decisionDiagnostics.Contains(DecisionLogDiagnosticCode.DecisionLogOfficialAuditForbidden), Is.True);
            Assert.That(decisionDiagnostics.Contains(DecisionLogDiagnosticCode.DecisionLogImpactMissing), Is.True);

            var burnDown = new SocialMmoOperationalRiskBurnDown("risks", new[]
            {
                new OperationalRiskItem("risk", "PvP", "fairness", 5, OperationalRiskMovement.BlockedByServer, Array.Empty<OperationalRiskEvidenceLink>(), Array.Empty<string>(), new[] { new OperationalRiskBlocker("server", serverBlocker: true) }, new[] { new OperationalRiskReadinessWarning("ready", releaseReadyClaimed: true, alphaReadyClaimed: true) }, resolvedClaimed: true)
            });
            OperationalRiskBurnDownDiagnostics burnDownDiagnostics = burnDown.Evaluate();
            Assert.That(burnDownDiagnostics.Contains(OperationalRiskDiagnosticCode.OperationalRiskSourceMissing), Is.True);
            Assert.That(burnDownDiagnostics.Contains(OperationalRiskDiagnosticCode.OperationalRiskResolutionOverclaimed), Is.True);
            Assert.That(burnDownDiagnostics.Contains(OperationalRiskDiagnosticCode.OperationalRiskReleaseReadyForbidden), Is.True);
            Assert.That(burnDownDiagnostics.Contains(OperationalRiskDiagnosticCode.OperationalRiskAlphaReadyForbidden), Is.True);
            Assert.That(burnDownDiagnostics.Contains(OperationalRiskDiagnosticCode.OperationalRiskServerBlockerOpen), Is.True);
        }

        [Test]
        public void ReviewConsoleClosureGate_BlocksBee381PrematureAndFinalRuntimeClaims()
        {
            var gate = new SocialMmoReviewConsoleClosureGate("gate", null, new SocialMmoReviewConsoleClosureCoverage(privacyRiskOpen: true, runtimeClaim: true, liveOpsFinalClaim: true), new[] { new SocialMmoReviewConsoleClosureBlocker("server", serverAuthorityGap: true) }, new Bee381BlockerStatus(prematureAttempt: true, SocialMmoReviewConsoleClosureGate.Bee381BlockedMessage));
            SocialMmoReviewConsoleClosureDiagnostics diagnostics = gate.Evaluate();

            Assert.That(diagnostics.Verdict, Is.EqualTo(SocialMmoReviewConsoleClosureVerdict.BlockedByBee381Premature));
            Assert.That(diagnostics.Contains(ReviewClosureDiagnosticCode.ReviewClosureInputMissing), Is.True);
            Assert.That(diagnostics.Contains(ReviewClosureDiagnosticCode.ReviewClosurePrivacyRiskOpen), Is.True);
            Assert.That(diagnostics.Contains(ReviewClosureDiagnosticCode.ReviewClosureServerAuthorityGapOpen), Is.True);
            Assert.That(diagnostics.Contains(ReviewClosureDiagnosticCode.ReviewClosureRuntimeClaimDetected), Is.True);
            Assert.That(diagnostics.Contains(ReviewClosureDiagnosticCode.ReviewClosureLiveOpsFinalForbidden), Is.True);
            Assert.That(diagnostics.Contains(ReviewClosureDiagnosticCode.Bee381Premature), Is.True);
        }
    }
}
