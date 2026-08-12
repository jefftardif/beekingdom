using System.Linq;
using BeeKingdom.Save;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PersistenceVisualVerificationFrameworks241To250Tests
    {
        [Test]
        public void ServerAnalysisIntake_ReportsMissingAnalysisSqlRuntimeAndSources()
        {
            var intake = new PersistenceServerAnalysisIntake(new[]
            {
                new PersistenceServerAnalysisItem("BEE-231", "boundary", true, true, true, "analyze", "evidence", PersistenceServerAnalysisStatus.ServerAnalysisRequired),
                new PersistenceServerAnalysisItem(string.Empty, "missing", true, false, false, "analyze", string.Empty, PersistenceServerAnalysisStatus.Blocked)
            }, 230, sqlRequested: true, runtimeRequested: true);

            Assert.AreEqual(2, intake.Items.Count);
            Assert.IsTrue(intake.Diagnostics.Contains(PersistenceServerAnalysisDiagnostics.ServerAnalysisMissing));
            Assert.IsTrue(intake.Diagnostics.Contains(PersistenceServerAnalysisDiagnostics.BeeSourceMissing));
            Assert.IsTrue(intake.Diagnostics.Contains(PersistenceServerAnalysisDiagnostics.SqlScopeRequested));
            Assert.IsTrue(intake.Diagnostics.Contains(PersistenceServerAnalysisDiagnostics.RuntimeImplementationRequested));
        }

        [Test]
        public void VisualReadinessPanel_DetectsMissingSectionsBadgeConflictAndRuntimeClaim()
        {
            var panel = new PersistenceVisualReadinessPanel(new[] { PersistenceVisualReadinessSection.RuntimeBoundary }, PersistenceReadinessBadge.Ready, new[] { new PersistencePanelBlocker("server", string.Empty) }, runtimeClaim: true);

            Assert.IsTrue(panel.Diagnostics.Contains(PersistencePanelWarning.PanelSourceMissing));
            Assert.IsTrue(panel.Diagnostics.Contains(PersistencePanelWarning.BadgeConflict));
            Assert.IsTrue(panel.Diagnostics.Contains(PersistencePanelWarning.RuntimeClaimDetected));
            Assert.IsTrue(panel.Diagnostics.Contains(PersistencePanelWarning.BlockerWithoutExplanation));
        }

        [Test]
        public void DemoBlockerExplanation_DetectsUnsafeAndIncompleteMessages()
        {
            var explanation = new PersistenceDemoBlockerExplanation(PersistenceDemoBlockerCode.ServerNotAnalyzed, string.Empty, "server missing", "backend risk", null, "secret auto-fix");

            Assert.IsTrue(explanation.Diagnostics.Contains(PersistenceDemoBlockerDiagnostics.BeeSourceMissing));
            Assert.IsTrue(explanation.Diagnostics.Contains(PersistenceDemoBlockerDiagnostics.MissingNextAction));
            Assert.IsTrue(explanation.Diagnostics.Contains(PersistenceDemoBlockerDiagnostics.UnsafeBlockerText));
            Assert.IsTrue(explanation.Diagnostics.Contains(PersistenceDemoBlockerDiagnostics.AutoFixSuggested));
        }

        [Test]
        public void EvidenceDrilldown_DetectsMissingContradictoryUnsafeAndUnlinkedEvidence()
        {
            var drilldown = new PersistenceEvidenceDrilldown(new[]
            {
                new PersistenceEvidenceLink("verdict", PersistenceEvidenceSource.WorkerReport, PersistenceEvidenceStatus.Confirmed, PersistenceEvidenceLimit.ContractOnly, "ok"),
                new PersistenceEvidenceLink("verdict", PersistenceEvidenceSource.ServerProgress, PersistenceEvidenceStatus.Contradictory, PersistenceEvidenceLimit.ServerNotAnalyzed, "ok"),
                new PersistenceEvidenceLink(string.Empty, null, PersistenceEvidenceStatus.Missing, null, "secret")
            });

            Assert.IsTrue(drilldown.Diagnostics.Contains(PersistenceEvidenceDiagnostics.EvidenceSourceMissing));
            Assert.IsTrue(drilldown.Diagnostics.Contains(PersistenceEvidenceDiagnostics.EvidenceContradiction));
            Assert.IsTrue(drilldown.Diagnostics.Contains(PersistenceEvidenceDiagnostics.EvidenceLimitMissing));
            Assert.IsTrue(drilldown.Diagnostics.Contains(PersistenceEvidenceDiagnostics.UnsafeEvidenceDetail));
            Assert.IsTrue(drilldown.Diagnostics.Contains(PersistenceEvidenceDiagnostics.UnlinkedVerdict));
        }

        [Test]
        public void RuntimeGapTriage_ReportsMissingFieldsAndForbiddenActions()
        {
            var report = new PersistenceRuntimeGapTriageReport(new[]
            {
                new PersistenceRuntimeGap(PersistenceRuntimeGapCategory.MissingFixture, PersistenceRuntimeGapSeverity.High, PersistenceRuntimeGapOwner.Worker, PersistenceRuntimeGapAction.ProvideFixture, "BEE-232", "evidence"),
                new PersistenceRuntimeGap(null, null, null, PersistenceRuntimeGapAction.DestructiveAction, "BEE-245", string.Empty)
            });

            Assert.IsTrue(report.Diagnostics.Contains(PersistenceRuntimeGapDiagnostics.GapCategoryMissing));
            Assert.IsTrue(report.Diagnostics.Contains(PersistenceRuntimeGapDiagnostics.GapOwnerMissing));
            Assert.IsTrue(report.Diagnostics.Contains(PersistenceRuntimeGapDiagnostics.DestructiveActionSuggested));
            Assert.IsTrue(report.Diagnostics.Contains(PersistenceRuntimeGapDiagnostics.GapEvidenceMissing));
            Assert.IsTrue(report.Diagnostics.Contains(PersistenceRuntimeGapDiagnostics.GapSeverityConflict));
        }

        [Test]
        public void BackendHandoffReview_ComputesBlocksAndAmbiguousOwners()
        {
            var review = new PersistenceBackendHandoffReview(new[] { PersistenceHandoffParticipant.Worker, PersistenceHandoffParticipant.BeeServer }, workerEvidence: false, architectureValid: false, qaRiskOpen: true, serverPremature: true);

            Assert.AreEqual(PersistenceHandoffStatus.BlockedByArchitecture, review.Status);
            Assert.IsTrue(review.Diagnostics.Contains(PersistenceHandoffDiagnostics.HandoffOwnerAmbiguous));
            Assert.IsTrue(review.Diagnostics.Contains(PersistenceHandoffDiagnostics.WorkerEvidenceMissing));
            Assert.IsTrue(review.Diagnostics.Contains(PersistenceHandoffDiagnostics.ArchitectureValidationMissing));
            Assert.IsTrue(review.Diagnostics.Contains(PersistenceHandoffDiagnostics.QaRiskUnresolved));
        }

        [Test]
        public void EvidenceAlignmentMatrix_ShowsContradictionsStaleAndDemoQaConfusion()
        {
            var matrix = new PersistenceEvidenceAlignmentMatrix(new[]
            {
                new PersistenceEvidenceAlignmentCell("BEE-247", PersistenceEvidenceAxis.WorkerReport, PersistenceEvidenceAlignmentVerdict.Confirmed, 1),
                new PersistenceEvidenceAlignmentCell("BEE-247", PersistenceEvidenceAxis.ServerProgress, PersistenceEvidenceAlignmentVerdict.Contradiction, 1),
                new PersistenceEvidenceAlignmentCell("BEE-248", PersistenceEvidenceAxis.DemoReadModel, PersistenceEvidenceAlignmentVerdict.Confirmed, 1, demoAsQa: true),
                new PersistenceEvidenceAlignmentCell("BEE-249", PersistenceEvidenceAxis.QaReport, PersistenceEvidenceAlignmentVerdict.MissingEvidence, 1)
            }, currentDate: 200);

            Assert.IsTrue(matrix.Diagnostics.Contains(PersistenceEvidenceAlignmentDiagnostics.AlignmentSourceMissing));
            Assert.IsTrue(matrix.Diagnostics.Contains(PersistenceEvidenceAlignmentDiagnostics.AlignmentContradiction));
            Assert.IsTrue(matrix.Diagnostics.Contains(PersistenceEvidenceAlignmentDiagnostics.StaleEvidence));
            Assert.IsTrue(matrix.Diagnostics.Contains(PersistenceEvidenceAlignmentDiagnostics.DemoQaConfusion));
            Assert.IsTrue(matrix.Diagnostics.Contains(PersistenceEvidenceAlignmentDiagnostics.ServerEvidenceUnavailable));
        }

        [Test]
        public void DemoRegressionCapture_BlocksQaAndRuntimeClaims()
        {
            var capture = new PersistenceDemoRegressionCapture(1, "DEMO-011", null, PersistenceDemoCaptureStatus.Observed, null, "evidence", claimsQaFinal: true, claimsRuntimeExecution: true);

            Assert.IsTrue(capture.Diagnostics.Contains(PersistenceDemoCaptureDiagnostics.CaptureScenarioMissing));
            Assert.IsTrue(capture.Diagnostics.Contains(PersistenceDemoCaptureDiagnostics.CaptureLimitMissing));
            Assert.IsTrue(capture.Diagnostics.Contains(PersistenceDemoCaptureDiagnostics.QaResultClaimed));
            Assert.IsTrue(capture.Diagnostics.Contains(PersistenceDemoCaptureDiagnostics.RuntimeExecutionClaimed));
        }

        [Test]
        public void MilestoneProjection_StaysInformationalAndDetectsHiddenDependencies()
        {
            var projection = new PersistenceMilestoneProjection(PersistenceMilestoneTarget.Beta, new[] { new PersistenceMilestoneGap("server analysis") }, releaseClaim: true, sourcePresent: false, serverVisible: false, qaVisible: false);

            Assert.AreEqual(PersistenceMilestoneStatus.ReleaseDecisionForbidden, projection.Status);
            Assert.IsTrue(projection.Diagnostics.Contains(PersistenceMilestoneProjectionDiagnostics.ReleaseDecisionClaimed));
            Assert.IsTrue(projection.Diagnostics.Contains(PersistenceMilestoneProjectionDiagnostics.ProjectionSourceMissing));
            Assert.IsTrue(projection.Diagnostics.Contains(PersistenceMilestoneProjectionDiagnostics.ServerDependencyHidden));
            Assert.IsTrue(projection.Diagnostics.Contains(PersistenceMilestoneProjectionDiagnostics.QaDependencyHidden));
        }

        [Test]
        public void VisualVerificationGate_ReturnsExpectedVerdicts()
        {
            var gate = new PersistenceVisualVerificationGate();
            var ready = new[] { new PersistenceVisualVerificationCriterion("BEE-241", true, false, true), new PersistenceVisualVerificationCriterion("BEE-242", true, false, true) };

            Assert.AreEqual(PersistenceVisualVerificationVerdict.VisualVerificationReady, gate.Evaluate(ready, false, false, false, false).Verdict);
            Assert.AreEqual(PersistenceVisualVerificationVerdict.BlockedByServerAnalysis, gate.Evaluate(ready, true, false, false, false).Verdict);
            Assert.AreEqual(PersistenceVisualVerificationVerdict.BlockedByEvidenceContradiction, gate.Evaluate(ready, false, true, false, false).Verdict);
            Assert.AreEqual(PersistenceVisualVerificationVerdict.BlockedByBee251Premature, gate.Evaluate(ready, false, false, true, false).Verdict);
            Assert.IsTrue(gate.Evaluate(new[] { new PersistenceVisualVerificationCriterion("BEE-242", true, false, false) }, false, false, false, false).Diagnostics.Contains(PersistenceVisualVerificationDiagnostics.DemoImpactMissing));
        }
    }
}
