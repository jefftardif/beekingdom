using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PredictionReconciliationFrameworks181To190Tests
    {
        [Test]
        public void ClientPredictionContract_AllowsVisualOnlyAndRejectsServerOnlyOrExpired()
        {
            var contract = new ClientPredictionContract(new[]
            {
                new ClientPredictionRule("hover", ClientPredictionScope.VisualOnly, 5, "authority refresh"),
                new ClientPredictionRule("spend-resource", ClientPredictionScope.ServerOnly, 0, "never")
            });

            Assert.IsTrue(contract.Evaluate("hover", 3, true).Allowed);
            Assert.IsFalse(contract.Evaluate("spend-resource", 0, true).Allowed);
            Assert.AreEqual(ClientPredictionLimit.Expired, contract.Evaluate("hover", 8, true).Limit);
            Assert.AreEqual(ClientPredictionLimit.MissingReadModelSource, contract.Evaluate("hover", 1, false).Limit);
            CollectionAssert.AreEqual(contract.Rules.Select(r => r.Action).OrderBy(a => a).ToArray(), contract.Rules.Select(r => r.Action).ToArray());
        }

        [Test]
        public void PredictionInputBuffer_TracksStatusesReplayAndExpiration()
        {
            var buffer = new PredictionInputBuffer();
            buffer.Add(new PredictionInputEntry("cmd-2", 2, 10, "hover"));
            buffer.Add(new PredictionInputEntry("cmd-1", 1, 8, "select"));

            buffer.Acknowledge("cmd-1");
            buffer.Reject("cmd-2", "server rejected");
            buffer.Add(new PredictionInputEntry("cmd-3", 3, 1, "old"));
            buffer.Expire(20, 5);

            Assert.AreEqual(PredictionInputStatus.Acknowledged, buffer.Entries.First(e => e.CommandId == "cmd-1").Status);
            Assert.AreEqual(PredictionInputStatus.Invalidated, buffer.Entries.First(e => e.CommandId == "cmd-2").Status);
            Assert.AreEqual(PredictionInputStatus.Expired, buffer.Entries.First(e => e.CommandId == "cmd-3").Status);
            Assert.AreEqual("cmd-2", buffer.Replay(new PredictionInputReplayCursor(1))[0].CommandId);
        }

        [Test]
        public void ReconciliationSnapshotComparator_ProducesStableDiffsWithoutMutation()
        {
            var comparator = new ReconciliationSnapshotComparator();
            var client = new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" };
            var authority = new Dictionary<string, string> { ["a"] = "9", ["b"] = "2" };

            SnapshotComparisonResult same = comparator.Compare(client, client, new ProtocolVersion(1, 0), new ProtocolVersion(1, 0), "d", "d", 1, 1);
            SnapshotComparisonResult diff = comparator.Compare(client, authority, new ProtocolVersion(1, 0), new ProtocolVersion(2, 0), "d1", "d2", 1, 2);

            Assert.IsTrue(same.Matches);
            Assert.IsTrue(diff.Differences.Any(d => d.Kind == SnapshotDifferenceKind.Field && d.Path == "a"));
            Assert.IsTrue(diff.Differences.Any(d => d.Kind == SnapshotDifferenceKind.Version));
            Assert.AreEqual("1", client["a"]);
            Assert.IsTrue(diff.Hints.Any());
        }

        [Test]
        public void RollbackEligibilityPolicy_ProtectsSensitiveDomains()
        {
            var policy = new RollbackEligibilityPolicy();

            Assert.AreEqual(RollbackEligibilityVerdict.RollbackAllowed, policy.Evaluate(RollbackDomain.Visual).Verdict);
            Assert.AreEqual(RollbackEligibilityVerdict.Forbidden, policy.Evaluate(RollbackDomain.Resource).Verdict);
            Assert.AreEqual(RollbackEligibilityVerdict.ServerOverwrite, policy.Evaluate(RollbackDomain.ReadModel).Verdict);
            Assert.AreEqual(RollbackEligibilityVerdict.ServerOverwrite, policy.Evaluate(RollbackDomain.Construction).Verdict);
            Assert.AreEqual(RollbackEligibilityVerdict.Blocked, policy.Evaluate(RollbackDomain.Unknown).Verdict);
        }

        [Test]
        public void VisualCorrectionReadModel_ConvertsDiffsIntoReadOnlyEntries()
        {
            var diff = new SnapshotDifference(SnapshotDifferenceKind.Field, SnapshotDifferenceSeverity.Error, "bee.energy", "1", "2");
            VisualCorrectionReadModel visual = VisualCorrectionReadModel.FromDifferences(new[] { diff }, RollbackEligibilityVerdict.RollbackAllowed, "bee-read-model");
            VisualCorrectionReadModel blocked = VisualCorrectionReadModel.FromDifferences(new[] { diff }, RollbackEligibilityVerdict.Forbidden, "bee-read-model");
            VisualCorrectionReadModel missing = VisualCorrectionReadModel.FromDifferences(new[] { new SnapshotDifference(SnapshotDifferenceKind.Field, SnapshotDifferenceSeverity.Error, string.Empty, "1", "2") }, RollbackEligibilityVerdict.RollbackAllowed, "bee-read-model");

            Assert.AreEqual(VisualCorrectionKind.RefreshRequired, visual.Entries[0].Kind);
            Assert.AreEqual(VisualCorrectionKind.Blocked, blocked.Entries[0].Kind);
            Assert.IsFalse(string.IsNullOrWhiteSpace(visual.Entries[0].ClientSafeMessage));
            Assert.IsTrue(missing.Diagnostics.Issues.Any());
        }

        [Test]
        public void LatencySimulationScenario_IsSeededAndLogicalOnly()
        {
            var first = new LatencySimulationScenario(new LatencyScenarioSeed(42), new[] { LatencyPattern.Delay, LatencyPattern.PacketLoss, LatencyPattern.Reorder }, 5);
            var second = new LatencySimulationScenario(new LatencyScenarioSeed(42), new[] { LatencyPattern.Delay, LatencyPattern.PacketLoss, LatencyPattern.Reorder }, 5);
            var empty = new LatencySimulationScenario(new LatencyScenarioSeed(1), new LatencyPattern[0], 0);

            CollectionAssert.AreEqual(first.Events.Select(e => e.EventId).ToArray(), second.Events.Select(e => e.EventId).ToArray());
            Assert.IsTrue(first.Events.Any(e => e.Pattern == LatencyPattern.Reorder));
            Assert.IsTrue(first.Diagnostics.Issues.Any());
            Assert.IsTrue(empty.Diagnostics.Valid);
        }

        [Test]
        public void ReconciliationFailureCatalog_MapsSignalsToStableCodes()
        {
            var catalog = new ReconciliationFailureCatalog();

            Assert.AreEqual(ReconciliationFailureCode.DriftMismatch, catalog.Map("digest drift").Failure.Code);
            Assert.AreEqual(ReconciliationFailureCode.VersionMismatch, catalog.Map("version mismatch").Failure.Code);
            Assert.AreEqual(ReconciliationFailureCode.MissingData, catalog.Map("missing data").Failure.Code);
            Assert.IsTrue(catalog.Failures.Select(f => f.Code).Distinct().Count() == catalog.Failures.Count);
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.Map("missing").Failure.RecommendedAction));
        }

        [Test]
        public void CrossClientConsistencyAudit_UsesAuthorityBaselineNotMajority()
        {
            var audit = new CrossClientConsistencyAudit();

            Assert.AreEqual(ClientConsistencyVerdict.Converged, audit.Audit("auth", 10, new[] { new ClientConsistencySample("a", 10, "auth") }, 2).Verdict);
            Assert.AreEqual(ClientConsistencyVerdict.Stale, audit.Audit("auth", 10, new[] { new ClientConsistencySample("a", 1, "auth") }, 2).Verdict);
            Assert.AreEqual(ClientConsistencyVerdict.Divergent, audit.Audit("auth", 10, new[] { new ClientConsistencySample("a", 10, "client"), new ClientConsistencySample("b", 10, "client") }, 2).Verdict);
            Assert.AreEqual(ClientConsistencyVerdict.InsufficientEvidence, audit.Audit(string.Empty, 10, new[] { new ClientConsistencySample("a", 10, "client") }, 2).Verdict);
        }

        [Test]
        public void AuthorityQAEvidenceBridge_LinksBeeSourcesAndReportsOrphans()
        {
            var bridge = new AuthorityQAEvidenceBridge();
            IReadOnlyList<AuthorityQAEvidenceLink> links = bridge.Link(new[]
            {
                new AuthorityQAEvidenceRef("regional", AuthorityQAEvidenceKind.RegionalEvidence, "BEE-188", "C:/projets/beekingdom/QA"),
                new AuthorityQAEvidenceRef("authority", AuthorityQAEvidenceKind.CrossClientAudit, "BEE-188", "C:/projets/beekingdom/QA"),
                new AuthorityQAEvidenceRef("orphan", AuthorityQAEvidenceKind.Telemetry, string.Empty, "C:/projets/beekingdom/QA")
            });

            Assert.IsTrue(links.Any(link => link.Source.BeeSource == "BEE-188"));
            Assert.IsTrue(bridge.Diagnostics.Issues.Any(issue => issue.Contains("orphan")));
        }

        [Test]
        public void PredictionReadinessGate_ReturnsExpectedVerdicts()
        {
            var gate = new PredictionReadinessGate();
            var ready = new[] { new PredictionReadinessCriterion("prediction-bounded", true, true), new PredictionReadinessCriterion("failure-catalog", true, false) };

            Assert.AreEqual(PredictionReadinessVerdict.Ready, gate.Evaluate(ready).Verdict);
            Assert.AreEqual(PredictionReadinessVerdict.ReadyWithWarnings, gate.Evaluate(ready, latencyWarning: true).Verdict);
            Assert.AreEqual(PredictionReadinessVerdict.NeedsRevision, gate.Evaluate(new[] { new PredictionReadinessCriterion("failure-catalog", false, false, "missing") }).Verdict);
            Assert.AreEqual(PredictionReadinessVerdict.Blocked, gate.Evaluate(new[] { new PredictionReadinessCriterion("client-authority", false, true, "client cannot be truth") }).Verdict);
        }
    }
}
