using System.Collections.Generic;
using System.Linq;
using BeeKingdom.QA;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class AuthorityGovernanceFrameworks191To200Tests
    {
        [Test]
        public void CoverageMatrix_ReportsCoveredAxesAndGaps()
        {
            var empty = new AuthorityIntegrationCoverageMatrix(null);
            var matrix = new AuthorityIntegrationCoverageMatrix(new[]
            {
                new AuthorityCoverageCell(AuthorityCoverageAxis.Commands, "BEE-152", "CommandEnvelope", "DEMO-010", "evidence-command"),
                new AuthorityCoverageCell(AuthorityCoverageAxis.Demos, "BEE-192", "DemoEvidence", "DEMO-010", string.Empty)
            });

            Assert.IsTrue(empty.Diagnostics.Gaps.Any());
            Assert.IsTrue(matrix.Cells.First().Covered);
            Assert.IsTrue(matrix.Diagnostics.Gaps.Any(gap => gap.Axis == AuthorityCoverageAxis.Reconciliation));
            Assert.IsTrue(matrix.Diagnostics.Gaps.Any(gap => gap.Axis == AuthorityCoverageAxis.Demos));
        }

        [Test]
        public void ServerDemoEvidenceBundle_ExcludesInvalidEvidenceAndKeepsGaps()
        {
            var bundle = new ServerDemoEvidenceBundle(new[]
            {
                new ServerDemoEvidenceEntry("valid", ServerDemoEvidenceScope.Demo010, "DEMO-010", "SERVER-008", true),
                new ServerDemoEvidenceEntry("invalid", ServerDemoEvidenceScope.Demo011, "DEMO-011", "SERVER-008", false),
                new ServerDemoEvidenceEntry("missing-demo", ServerDemoEvidenceScope.Demo012, string.Empty, "SERVER-008", true)
            }, new[] { "networking-runtime-absent" });

            Assert.AreEqual(2, bundle.Entries.Count);
            Assert.IsTrue(bundle.Manifest.EntryIds.SequenceEqual(bundle.Manifest.EntryIds.OrderBy(id => id)));
            Assert.IsTrue(bundle.KnownGaps.Contains("networking-runtime-absent"));
            Assert.IsTrue(bundle.Diagnostics.Issues.Any());
        }

        [Test]
        public void MultiplayerRiskRegister_DiagnosesEvidenceAndJustification()
        {
            var register = new MultiplayerScenarioRiskRegister(new[]
            {
                new MultiplayerScenarioRisk("transport", MultiplayerRiskSeverity.High, MultiplayerRiskStatus.Open, string.Empty, string.Empty, "risk"),
                new MultiplayerScenarioRisk("accepted", MultiplayerRiskSeverity.Medium, MultiplayerRiskStatus.Accepted, string.Empty, "accepted for prototype", "limitation"),
                new MultiplayerScenarioRisk("blocked", MultiplayerRiskSeverity.Critical, MultiplayerRiskStatus.Blocked, "evidence", string.Empty, "blocker")
            });

            Assert.AreEqual("blocked", register.Risks[0].Id);
            Assert.IsTrue(register.Diagnostics.Issues.Any(issue => issue.Contains("transport")));
            Assert.IsFalse(register.Diagnostics.Issues.Any(issue => issue.Contains("accepted")));
        }

        [Test]
        public void ContractMigrationGuard_DetectsBreakingChangesVersionAndMigrationGaps()
        {
            var guard = new ContractMigrationGuard();
            var oldSchema = new Dictionary<string, string> { ["id"] = "string", ["status"] = "enum-a" };
            var newSchema = new Dictionary<string, string> { ["id"] = "string", ["status"] = "enum-b", ["newField"] = "int" };
            ContractMigrationDiagnostics diagnostics = guard.Compare(oldSchema, newSchema, false, false);
            ContractMigrationDiagnostics removed = guard.Compare(oldSchema, new Dictionary<string, string> { ["id"] = "string" }, true, true);

            Assert.IsTrue(diagnostics.Findings.Any(f => f.Reason.Contains("Field added")));
            Assert.IsTrue(diagnostics.Findings.Any(f => f.Reason.Contains("enum")));
            Assert.IsTrue(diagnostics.Findings.Any(f => f.Field == "version" && f.Severity == ContractMigrationSeverity.Blocked));
            Assert.IsTrue(diagnostics.Findings.Any(f => f.Field == "migration"));
            Assert.IsTrue(removed.Findings.Any(f => f.Reason.Contains("removed")));
        }

        [Test]
        public void DocumentationSyncPlan_TracksMissingSectionsInStableOrder()
        {
            var plan = new AuthorityDocumentationSyncPlan(new[]
            {
                new AuthorityDocumentationRule("BEE-195", new AuthorityDocumentationSection("SERVER-002", "contracts"), "migration", MultiplayerRiskSeverity.High, AuthorityDocumentationStatus.Missing),
                new AuthorityDocumentationRule("BEE-192", new AuthorityDocumentationSection("DEMO-010", "evidence"), "demo bundle", MultiplayerRiskSeverity.Medium, AuthorityDocumentationStatus.NeedsUpdate)
            });

            Assert.AreEqual("BEE-192", plan.Rules[0].Bee);
            Assert.IsTrue(plan.Diagnostics.Issues.Any(issue => issue.Contains("SERVER-002")));
        }

        [Test]
        public void WorkerServerHandoffChecklist_ComputesVerdicts()
        {
            Assert.AreEqual(WorkerServerHandoffVerdict.Ready, new WorkerServerHandoffChecklist(new[] { new WorkerServerHandoffItem("no-server-creation", WorkerServerHandoffStatus.Done) }).Verdict);
            Assert.AreEqual(WorkerServerHandoffVerdict.ReadyWithWarnings, new WorkerServerHandoffChecklist(new[] { new WorkerServerHandoffItem("docs", WorkerServerHandoffStatus.Warning) }).Verdict);
            Assert.AreEqual(WorkerServerHandoffVerdict.Incomplete, new WorkerServerHandoffChecklist(new[] { new WorkerServerHandoffItem("qa-read", WorkerServerHandoffStatus.Incomplete) }).Verdict);
            Assert.AreEqual(WorkerServerHandoffVerdict.Blocked, new WorkerServerHandoffChecklist(new[] { new WorkerServerHandoffItem("service-created-without-server", WorkerServerHandoffStatus.Blocked) }).Verdict);
        }

        [Test]
        public void AuthorityLotReview_ValidatesLotShapeReportsQaAndRisks()
        {
            var review = new AuthorityLotReview();

            Assert.AreEqual(AuthorityLotReviewVerdict.Approved, review.Review(new AuthorityLotReviewInput(181, 190, true, true, true, null)).Verdict);
            Assert.AreEqual(AuthorityLotReviewVerdict.NeedsRevision, review.Review(new AuthorityLotReviewInput(181, 190, false, true, true, null)).Verdict);
            Assert.AreEqual(AuthorityLotReviewVerdict.Blocked, review.Review(new AuthorityLotReviewInput(181, 190, true, true, true, new[] { new AuthorityLotReviewFinding("risk", MultiplayerRiskSeverity.Critical, "critical") })).Verdict);
            Assert.AreEqual(AuthorityLotReviewVerdict.Blocked, review.Review(new AuthorityLotReviewInput(181, 190, true, true, false, null)).Verdict);
        }

        [Test]
        public void BetaNetworkReadinessProjection_ReturnsExpectedVerdicts()
        {
            Assert.AreEqual(BetaNetworkVerdict.OnTrack, BetaNetworkReadinessProjection.Evaluate(true, true, false, true).Verdict);
            Assert.AreEqual(BetaNetworkVerdict.AtRisk, BetaNetworkReadinessProjection.Evaluate(true, false, false, true).Verdict);
            Assert.AreEqual(BetaNetworkVerdict.Blocked, BetaNetworkReadinessProjection.Evaluate(true, true, true, true).Verdict);
            Assert.AreEqual(BetaNetworkVerdict.InsufficientEvidence, BetaNetworkReadinessProjection.Evaluate(true, true, false, false).Verdict);
        }

        [Test]
        public void CommercialRiskGate_SortsAndDominatesBySeverity()
        {
            var gate = new AuthorityCommercialRiskGate();

            Assert.AreEqual(CommercialRiskVerdict.Watch, gate.Evaluate(new[] { new CommercialRiskCriterion("demo limitation", CommercialRiskVerdict.Watch, "risk") }));
            Assert.AreEqual(CommercialRiskVerdict.AtRisk, gate.Evaluate(new[] { new CommercialRiskCriterion("security gap", CommercialRiskVerdict.AtRisk, "risk") }));
            Assert.AreEqual(CommercialRiskVerdict.Blocked, gate.Evaluate(new[] { new CommercialRiskCriterion("desync", CommercialRiskVerdict.Blocked, "risk") }));
            Assert.AreEqual("desync", gate.Diagnostics.Findings[0].Name);
        }

        [Test]
        public void AuthorityClosureGate_BlocksBee201BeforeValidation()
        {
            var gate = new AuthorityReadinessClosureGate();
            var ready = new[] { new AuthorityClosureCriterion("lot-151-160", true, false, true), new AuthorityClosureCriterion("qa-read", true, false, true) };

            Assert.AreEqual(AuthorityClosureVerdict.Closed, gate.Evaluate(ready, false).Verdict);
            Assert.AreEqual(AuthorityClosureVerdict.ClosedWithWarnings, gate.Evaluate(new[] { new AuthorityClosureCriterion("docs", true, true, false) }, false).Verdict);
            Assert.AreEqual(AuthorityClosureVerdict.NeedsRevision, gate.Evaluate(new[] { new AuthorityClosureCriterion("report", false, false, false) }, false).Verdict);
            Assert.AreEqual(AuthorityClosureVerdict.Blocked, gate.Evaluate(ready, true).Verdict);
        }
    }
}
