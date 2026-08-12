using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class SocialMmoPlaygroundToolsClosureFrameworks391To400Tests
    {
        [Test]
        public void MilestoneRegressionHandoffDebtAndComprehension_BlockFalseReadiness()
        {
            var matrix = new SocialMmoToolingMilestoneEvidenceMatrix("matrix", new[]
            {
                new ToolingMilestoneEvidenceRow("BEE-351-390", "Social", string.Empty, string.Empty, ToolingMilestoneProofStatus.MissingEvidence, ToolingMilestoneDemoStatus.GapVisible, new ToolingMilestoneServerDependency("server", open: true), new[] { new ToolingMilestoneBlockedClaim("runtime", runtimeProofClaimed: true) })
            });
            ToolingMilestoneDiagnostics matrixDiagnostics = matrix.Evaluate();
            Assert.That(matrixDiagnostics.Contains(ToolingMilestoneDiagnosticCode.ToolingMilestoneEvidenceMissing), Is.True);
            Assert.That(matrixDiagnostics.Contains(ToolingMilestoneDiagnosticCode.ToolingMilestoneOwnerMissing), Is.True);
            Assert.That(matrixDiagnostics.Contains(ToolingMilestoneDiagnosticCode.ToolingMilestoneRuntimeProofForbidden), Is.True);
            Assert.That(matrixDiagnostics.Contains(ToolingMilestoneDiagnosticCode.ToolingMilestoneServerDependencyOpen), Is.True);
            Assert.That(matrixDiagnostics.Contains(ToolingMilestoneDiagnosticCode.ToolingMilestoneDemoGapVisible), Is.True);

            var regression = new SocialMmoDemoReadinessRegressionCapture("capture", new[]
            {
                new DemoRegressionCaptureScenario("scenario", new[] { new DemoRegressionVisualCheckpoint(string.Empty, DemoReviewBindingSurface.Demo012SandboxPlayground, string.Empty, new DemoRegressionLimitAssertion(string.Empty, hidden: true), new DemoRegressionRedactionCheck(required: true, applied: false), new[] { new DemoRegressionBlockedClaim("runtime", runtimeClaim: true, separateSpecRequested: true) }) })
            });
            DemoRegressionDiagnostics regressionDiagnostics = regression.Evaluate();
            Assert.That(regressionDiagnostics.Contains(DemoRegressionDiagnosticCode.DemoRegressionCheckpointMissing), Is.True);
            Assert.That(regressionDiagnostics.Contains(DemoRegressionDiagnosticCode.DemoRegressionLimitHidden), Is.True);
            Assert.That(regressionDiagnostics.Contains(DemoRegressionDiagnosticCode.DemoRegressionRedactionMissing), Is.True);
            Assert.That(regressionDiagnostics.Contains(DemoRegressionDiagnosticCode.DemoRegressionRuntimeClaimDetected), Is.True);
            Assert.That(regressionDiagnostics.Contains(DemoRegressionDiagnosticCode.DemoRegressionSeparateSpecForbidden), Is.True);

            var handoff = new SocialMmoCrossHandoffLedger("ledger", new[]
            {
                new CrossHandoffLedgerEntry("topic", Array.Empty<CrossHandoffSourceRef>(), CrossHandoffOwnerRole.WorkerImplementer, CrossHandoffOwnerRole.ServerReviewer, CrossHandoffOwnerRole.Missing, new CrossHandoffBlocker("server", serverDependency: true), null, "server", finalPriorityClaimed: true, runtimeTicketRequested: true)
            });
            CrossHandoffDiagnostics handoffDiagnostics = handoff.Evaluate();
            Assert.That(handoffDiagnostics.Contains(CrossHandoffDiagnosticCode.CrossHandoffOwnerMissing), Is.True);
            Assert.That(handoffDiagnostics.Contains(CrossHandoffDiagnosticCode.CrossHandoffSourceMissing), Is.True);
            Assert.That(handoffDiagnostics.Contains(CrossHandoffDiagnosticCode.CrossHandoffLimitMissing), Is.True);
            Assert.That(handoffDiagnostics.Contains(CrossHandoffDiagnosticCode.CrossHandoffFinalPriorityForbidden), Is.True);
            Assert.That(handoffDiagnostics.Contains(CrossHandoffDiagnosticCode.CrossHandoffRuntimeTicketForbidden), Is.True);

            var debt = new SocialMmoToolingDebtRegister("debt", new[]
            {
                new ToolingDebtItem("debt", ToolingDebtCategory.RuntimeConfusion, ToolingDebtSeverity.Missing, new ToolingDebtOwner(string.Empty), Array.Empty<string>(), "blocker", new ToolingDebtNotAcceptedMarker(acceptedAsReady: true)),
                new ToolingDebtItem("server", ToolingDebtCategory.ServerDependency, ToolingDebtSeverity.High, new ToolingDebtOwner("server"), Array.Empty<string>(), "server", new ToolingDebtNotAcceptedMarker())
            });
            ToolingDebtDiagnostics debtDiagnostics = debt.Evaluate();
            Assert.That(debtDiagnostics.Contains(ToolingDebtDiagnosticCode.ToolingDebtOwnerMissing), Is.True);
            Assert.That(debtDiagnostics.Contains(ToolingDebtDiagnosticCode.ToolingDebtSeverityMissing), Is.True);
            Assert.That(debtDiagnostics.Contains(ToolingDebtDiagnosticCode.ToolingDebtAcceptedAsReadyForbidden), Is.True);
            Assert.That(debtDiagnostics.Contains(ToolingDebtDiagnosticCode.ToolingDebtRuntimeConfusionOpen), Is.True);
            Assert.That(debtDiagnostics.Contains(ToolingDebtDiagnosticCode.ToolingDebtServerDependencyOpen), Is.True);

            var checklist = new ExternalReviewerComprehensionChecklist("checklist", new[]
            {
                new ReviewerComprehensionQuestion("question", new ReviewerComprehensionExpectedSignal("signal", visible: false), new ReviewerComprehensionFailure("failure", blockerHidden: true), "debt", serverDependencyVisible: false, new ReviewerComprehensionRuntimePromiseGuard(runtimePromiseDetected: true), new ReviewerComprehensionNoMarketingRule(marketingTextDetected: true))
            });
            ReviewerComprehensionDiagnostics checklistDiagnostics = checklist.Evaluate();
            Assert.That(checklistDiagnostics.Contains(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionSignalMissing), Is.True);
            Assert.That(checklistDiagnostics.Contains(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionMarketingTextDetected), Is.True);
            Assert.That(checklistDiagnostics.Contains(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionRuntimePromiseDetected), Is.True);
            Assert.That(checklistDiagnostics.Contains(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionServerDependencyHidden), Is.True);
            Assert.That(checklistDiagnostics.Contains(ReviewerComprehensionDiagnosticCode.ReviewerComprehensionBlockerHidden), Is.True);
        }

        [Test]
        public void PerformanceFailuresProjectionScaleAndClosure_BlockOperationalClaims()
        {
            var performance = new SocialMmoToolingPerformanceSignalBoundary("perf", new[]
            {
                new ToolingPerformanceSignal("signal", "DEMO-011", new ToolingPerformanceLogicalCost(10), new ToolingPerformanceDisplayCost(20, densityWarning: true), null, new ToolingPerformanceGuaranteeBlocker(benchmarkFinalClaimed: true, telemetryProductionRequested: true, guaranteeClaimed: true))
            });
            ToolingPerformanceDiagnostics performanceDiagnostics = performance.Evaluate();
            Assert.That(performanceDiagnostics.Contains(ToolingPerformanceDiagnosticCode.ToolingPerformanceSignalMissingLimit), Is.True);
            Assert.That(performanceDiagnostics.Contains(ToolingPerformanceDiagnosticCode.ToolingPerformanceBenchmarkFinalForbidden), Is.True);
            Assert.That(performanceDiagnostics.Contains(ToolingPerformanceDiagnosticCode.ToolingPerformanceTelemetryProductionForbidden), Is.True);
            Assert.That(performanceDiagnostics.Contains(ToolingPerformanceDiagnosticCode.ToolingPerformanceGuaranteeForbidden), Is.True);
            Assert.That(performanceDiagnostics.Contains(ToolingPerformanceDiagnosticCode.ToolingPerformanceDensityWarning), Is.True);

            var failures = new SocialMmoToolchainFailureModeCatalog("failures", new[]
            {
                new ToolchainFailureMode(string.Empty, "privacy leak", ToolchainFailureSeverity.Critical, new ToolchainFailureBlockRule(blocksReview: true, silentCorrectionRequested: true), "debt", new ToolchainFailureRecoveryHint("manual", executableRecovery: true), new ToolchainFailureDemoMarker("demo"), privacyLeak: true, serverBypass: true, falseVerdict: true)
            });
            ToolchainFailureDiagnostics failureDiagnostics = failures.Evaluate();
            Assert.That(failureDiagnostics.Contains(ToolchainFailureDiagnosticCode.ToolchainFailureModeMissing), Is.True);
            Assert.That(failureDiagnostics.Contains(ToolchainFailureDiagnosticCode.ToolchainFailureSilentCorrectionForbidden), Is.True);
            Assert.That(failureDiagnostics.Contains(ToolchainFailureDiagnosticCode.ToolchainFailurePrivacyLeakDetected), Is.True);
            Assert.That(failureDiagnostics.Contains(ToolchainFailureDiagnosticCode.ToolchainFailureServerBypassDetected), Is.True);
            Assert.That(failureDiagnostics.Contains(ToolchainFailureDiagnosticCode.ToolchainFailureFalseVerdictDetected), Is.True);

            var projection = new PlaygroundToolsMilestoneProjection("projection", Array.Empty<MilestoneProjectionGain>(), new[] { new MilestoneProjectionGap("gap", hidden: true) }, Array.Empty<MilestoneProjectionDependency>(), new MilestoneProjectionForbiddenDecision(alphaReady: true, releaseReady: true, serverReady: true));
            MilestoneProjectionDiagnostics projectionDiagnostics = projection.Evaluate();
            Assert.That(projectionDiagnostics.Contains(MilestoneProjectionDiagnosticCode.MilestoneProjectionGapHidden), Is.True);
            Assert.That(projectionDiagnostics.Contains(MilestoneProjectionDiagnosticCode.MilestoneProjectionReleaseDecisionForbidden), Is.True);
            Assert.That(projectionDiagnostics.Contains(MilestoneProjectionDiagnosticCode.MilestoneProjectionAlphaReadyForbidden), Is.True);
            Assert.That(projectionDiagnostics.Contains(MilestoneProjectionDiagnosticCode.MilestoneProjectionServerReadyForbidden), Is.True);
            Assert.That(projectionDiagnostics.Contains(MilestoneProjectionDiagnosticCode.MilestoneProjectionDependencyMissing), Is.True);

            var scale = new ScaleOperationsHandoffBundle("scale", new[]
            {
                new ScaleOperationsHandoffTopic("topic", Array.Empty<string>(), new ScaleOperationsOwnerAssignment(string.Empty), Array.Empty<ScaleOperationsRiskCarryover>(), new ScaleOperationsBlockedToday(bee401Premature: true), Array.Empty<ScaleOperationsFutureQuestion>(), new[] { "TelemetryProduction", "OperationsSpec" })
            });
            ScaleHandoffDiagnostics scaleDiagnostics = scale.Evaluate();
            Assert.That(scaleDiagnostics.Contains(ScaleHandoffDiagnosticCode.ScaleHandoffOwnerMissing), Is.True);
            Assert.That(scaleDiagnostics.Contains(ScaleHandoffDiagnosticCode.ScaleHandoffRiskMissing), Is.True);
            Assert.That(scaleDiagnostics.Contains(ScaleHandoffDiagnosticCode.ScaleHandoffB401Premature), Is.True);
            Assert.That(scaleDiagnostics.Contains(ScaleHandoffDiagnosticCode.ScaleHandoffTelemetryProductionForbidden), Is.True);
            Assert.That(scaleDiagnostics.Contains(ScaleHandoffDiagnosticCode.ScaleHandoffOperationsSpecForbidden), Is.True);
        }

        [Test]
        public void PlaygroundToolsClosureGate_BlocksBee401PrematureAndRuntimeClaims()
        {
            var gate = new PlaygroundToolsClosureGate("gate", null, new PlaygroundToolsClosureCoverage(evidenceMissing: true, demoRegressionOpen: true, runtimeClaim: true), new[] { new PlaygroundToolsClosureBlocker("server", serverDependencyOpen: true) }, new Bee401BlockerStatus(prematureAttempt: true, PlaygroundToolsClosureGate.Bee401BlockedMessage));
            PlaygroundToolsClosureDiagnostics diagnostics = gate.Evaluate();

            Assert.That(diagnostics.Verdict, Is.EqualTo(PlaygroundToolsClosureVerdict.BlockedByBee401Premature));
            Assert.That(diagnostics.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureInputMissing), Is.True);
            Assert.That(diagnostics.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureEvidenceMissing), Is.True);
            Assert.That(diagnostics.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureDemoRegressionOpen), Is.True);
            Assert.That(diagnostics.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureServerDependencyOpen), Is.True);
            Assert.That(diagnostics.Contains(PlaygroundClosureDiagnosticCode.PlaygroundClosureRuntimeClaimDetected), Is.True);
            Assert.That(diagnostics.Contains(PlaygroundClosureDiagnosticCode.Bee401Premature), Is.True);
        }
    }
}
