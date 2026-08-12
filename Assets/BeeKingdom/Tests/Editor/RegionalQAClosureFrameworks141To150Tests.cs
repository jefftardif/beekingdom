using System;
using System.Linq;
using BeeKingdom.QA;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class RegionalQAClosureFrameworks141To150Tests
    {
        [Test]
        public void CoverageMatrixShowsEvidenceAndGaps()
        {
            RegionalValidationCoverageMatrix matrix = RegionalValidationCoverageMatrix.Build(
                new[] { Evidence("e-replay", 141, RegionalEvidenceType.Replay, true) },
                new[] { Tuple.Create(141, "world", RegionalEvidenceType.Replay), Tuple.Create(141, "world", RegionalEvidenceType.Demo) });

            Assert.That(matrix.Cells.Single().EvidenceType, Is.EqualTo(RegionalEvidenceType.Replay));
            Assert.That(matrix.Gaps.Single().MissingEvidenceType, Is.EqualTo(RegionalEvidenceType.Demo));
        }

        [Test]
        public void EvidenceBundleKeepsOnlyValidEvidenceAndGaps()
        {
            RegionalEvidenceBundleDiagnostics diagnostics = new RegionalEvidenceBundleDiagnostics();
            RegionalValidationCoverageMatrix matrix = RegionalValidationCoverageMatrix.Build(Array.Empty<RegionalEvidenceRecord>(), new[] { Tuple.Create(142, "world", RegionalEvidenceType.Demo) });

            RegionalEvidenceBundle bundle = new RegionalEvidenceBundleBuilder(diagnostics).Build(
                new RegionalEvidenceBundleScope(RegionalEvidenceBundleScopeKind.BeeLot, "141-150", 141, 150),
                new[] { Evidence("valid", 142, RegionalEvidenceType.Demo, true), Evidence("invalid", 142, RegionalEvidenceType.Replay, false) },
                matrix);

            Assert.That(bundle.Entries.Count, Is.EqualTo(1));
            Assert.That(diagnostics.ExcludedInvalidEvidence, Is.EqualTo(1));
            Assert.That(bundle.Gaps.Count, Is.EqualTo(1));
        }

        [Test]
        public void DependencyGraphDetectsCycleMissingAndOrphan()
        {
            RegionalQADependencyGraph graph = new RegionalQADependencyGraph(
                new[] { new RegionalQADependencyNode("bee", RegionalQADependencyNodeKind.Bee), new RegionalQADependencyNode("evidence", RegionalQADependencyNodeKind.Evidence), new RegionalQADependencyNode("orphan", RegionalQADependencyNodeKind.Demo) },
                new[] { new RegionalQADependencyEdge("bee", "evidence", RegionalQADependencyEdgeKind.Requires), new RegionalQADependencyEdge("evidence", "bee", RegionalQADependencyEdgeKind.Satisfies), new RegionalQADependencyEdge("missing", "bee", RegionalQADependencyEdgeKind.Informs) });

            Assert.That(graph.Diagnostics.CycleCount, Is.EqualTo(1));
            Assert.That(graph.Diagnostics.MissingDependencyCount, Is.EqualTo(1));
            Assert.That(graph.Diagnostics.OrphanCount, Is.EqualTo(1));
        }

        [Test]
        public void RiskRegisterEnforcesSourceAndAcceptedJustification()
        {
            Assert.Throws<ArgumentException>(() => new RegionalRisk("risk", 0, RegionalRiskStatus.Open, RegionalRiskSeverity.High, "", "", "", null));
            Assert.Throws<ArgumentException>(() => new RegionalRisk("risk", 144, RegionalRiskStatus.Accepted, RegionalRiskSeverity.High, "", "", "", null));

            RegionalRiskRegister register = new RegionalRiskRegister();
            register.RegisterBlockedFromGraph("blocked", 144, RegionalRiskSeverity.Critical, "node");

            Assert.That(register.QueryRisks().Single().Status, Is.EqualTo(RegionalRiskStatus.Blocked));
        }

        [Test]
        public void DocumentationPlanCreatesExpectedObligation()
        {
            RegionalDocumentationSyncPlan plan = new RegionalDocumentationSyncPlanner().Build(
                new[] { new RegionalDocumentationSyncRule("qa", "world", RegionalDocumentationSectionKind.QAEvidence) },
                new[] { Evidence("evidence", 145, RegionalEvidenceType.Demo, true) });

            Assert.That(plan.Obligations.Single().SectionKind, Is.EqualTo(RegionalDocumentationSectionKind.QAEvidence));
        }

        [Test]
        public void ArchitectureComplianceFindsBlockingViolationsAndWarnings()
        {
            RegionalArchitectureComplianceResult result = new RegionalArchitectureComplianceValidator().Validate(new RegionalArchitectureComplianceCheck(146, createsScene: true, mentionsFutureService: true));

            Assert.That(result.Verdict, Is.EqualTo(RegionalArchitectureComplianceVerdict.BlockingViolation));
            Assert.That(result.Violations.Count, Is.EqualTo(2));
        }

        [Test]
        public void WorkerHandoffBlocksOnComplianceViolation()
        {
            RegionalArchitectureComplianceResult compliance = new RegionalArchitectureComplianceValidator().Validate(new RegionalArchitectureComplianceCheck(147, createsScene: true));
            RegionalWorkerHandoffChecklist checklist = new RegionalWorkerHandoffBuilder().Build(147, new[] { new RegionalWorkerHandoffItem("objective", RegionalWorkerHandoffStatus.Ready, "ok") }, compliance);

            Assert.That(checklist.Verdict, Is.EqualTo(RegionalWorkerHandoffStatus.Blocked));
        }

        [Test]
        public void LotReviewDetectsMissingReportsAndBlockingViolation()
        {
            RegionalLotReview review = new RegionalLotReviewer().Review(new RegionalLotReviewInput(
                141,
                150,
                new[] { 141, 142, 143 },
                Array.Empty<RegionalWorkerHandoffChecklist>(),
                new[] { new RegionalArchitectureViolation("rule", RegionalArchitectureComplianceVerdict.BlockingViolation, "bad") },
                Array.Empty<RegionalRisk>()));

            Assert.That(review.Verdict, Is.EqualTo(RegionalLotReviewVerdict.Blocked));
            Assert.That(review.Findings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void AlphaProjectionDoesNotDeclareGlobalAlphaAndHonorsBlockers()
        {
            RegionalAlphaReadinessProjection projection = new RegionalAlphaReadinessProjector().Project(
                true,
                new RegionalLotReview(RegionalLotReviewVerdict.Approved, Array.Empty<RegionalLotReviewFinding>()),
                new[] { new RegionalRisk("critical", 149, RegionalRiskStatus.Open, RegionalRiskSeverity.Critical, "open", "cause", "impact", null) },
                RegionalValidationCoverageMatrix.Build(Array.Empty<RegionalEvidenceRecord>(), Array.Empty<Tuple<int, string, RegionalEvidenceType>>()),
                new RegionalWorkerHandoffChecklist(149, new[] { new RegionalWorkerHandoffItem("ready", RegionalWorkerHandoffStatus.Ready, "ok") }),
                new RegionalEvidenceBundle(new RegionalEvidenceBundleManifest(new RegionalEvidenceBundleScope(RegionalEvidenceBundleScopeKind.BeeLot, "lot", 141, 150), 0), Array.Empty<RegionalEvidenceBundleEntry>(), Array.Empty<RegionalValidationCoverageGap>()));

            Assert.That(projection.Verdict, Is.EqualTo(RegionalAlphaProjectionVerdict.Blocked));
        }

        [Test]
        public void ClosureGateStopsBeforeBee151()
        {
            RegionalWorldExecutionClosureDiagnostics diagnostics = new RegionalWorldExecutionClosureDiagnostics();
            RegionalWorldExecutionClosureReport report = new RegionalWorldExecutionClosureGate().Evaluate(
                new RegionalWorldExecutionClosureInput(new[] { new RegionalWorldExecutionClosureCriterion("coverage", true, true, "ok") }, referencesBee151: true),
                diagnostics);

            Assert.That(report.Verdict, Is.EqualTo(RegionalWorldExecutionClosureVerdict.Blocked));
            Assert.That(diagnostics.Bee151ReferenceCount, Is.EqualTo(1));
        }

        private static RegionalEvidenceRecord Evidence(string id, int bee, RegionalEvidenceType type, bool valid)
        {
            return new RegionalEvidenceRecord(id, bee, "world", "region", "demo", type, RegionalEvidenceVerdict.Passed, valid);
        }
    }
}
