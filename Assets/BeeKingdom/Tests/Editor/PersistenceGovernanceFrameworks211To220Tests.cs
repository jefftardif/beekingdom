using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Save;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PersistenceGovernanceFrameworks211To220Tests
    {
        [Test]
        public void PersistentDataClassification_DetectsMissingOwnerSensitivityAndForbiddenClass()
        {
            var classification = new PersistentDataClassification(new[]
            {
                new PersistentDataClassificationRule(PersistenceDataKind.ServerColonyState, PersistentDataClass.Identity, PersistentDataSensitivity.Internal, PersistenceBoundaryOwner.ServerAuthority, DataRetentionStatus.Keep),
                new PersistentDataClassificationRule(PersistenceDataKind.QaEvidence, PersistentDataClass.QAEvidence, PersistentDataSensitivity.Internal, PersistenceBoundaryOwner.QaReport, DataRetentionStatus.Archive),
                new PersistentDataClassificationRule(PersistenceDataKind.HiveSave, PersistentDataClass.GameplayState, PersistentDataSensitivity.Unknown, PersistenceBoundaryOwner.UnityLocalSave, DataRetentionStatus.Keep),
                new PersistentDataClassificationRule(PersistenceDataKind.DemoEvidence, PersistentDataClass.Forbidden, PersistentDataSensitivity.Public, PersistenceBoundaryOwner.Unknown, DataRetentionStatus.Forbidden)
            });

            Assert.IsTrue(classification.Rules.Any(r => r.DataClass == PersistentDataClass.Identity));
            Assert.IsTrue(classification.Rules.Any(r => r.DataClass == PersistentDataClass.QAEvidence));
            Assert.IsTrue(classification.Diagnostics.Issues.Any(issue => issue.Contains("Owner")));
            Assert.IsTrue(classification.Diagnostics.Issues.Any(issue => issue.Contains("Sensitivity")));
            Assert.IsTrue(classification.Diagnostics.Issues.Any(issue => issue.Contains("Forbidden")));
        }

        [Test]
        public void SaveMigrationDependencyGraph_DetectsLinearCycleMissingAndOrphan()
        {
            var linear = new SaveMigrationDependencyGraph(new[]
            {
                new SaveMigrationNode("migration-1", "migration"),
                new SaveMigrationNode("schema-1", "snapshot schema")
            }, new[] { new SaveMigrationEdge("migration-1", "schema-1", SaveMigrationDependencyKind.Requires) });
            var bad = new SaveMigrationDependencyGraph(new[]
            {
                new SaveMigrationNode("a", "migration"),
                new SaveMigrationNode("b", "schema"),
                new SaveMigrationNode("orphan", "migration")
            }, new[]
            {
                new SaveMigrationEdge("a", "b", SaveMigrationDependencyKind.Requires),
                new SaveMigrationEdge("b", "a", SaveMigrationDependencyKind.Blocks),
                new SaveMigrationEdge("a", "missing", SaveMigrationDependencyKind.Requires),
                new SaveMigrationEdge("a", "b", SaveMigrationDependencyKind.Unknown)
            });

            Assert.IsFalse(linear.Diagnostics.Issues.Any());
            Assert.IsTrue(bad.Diagnostics.Issues.Any(issue => issue.Contains("Cycle")));
            Assert.IsTrue(bad.Diagnostics.Issues.Any(issue => issue.Contains("Missing")));
            Assert.IsTrue(bad.Diagnostics.Issues.Any(issue => issue.Contains("Orphan")));
            Assert.IsTrue(bad.Diagnostics.Issues.Any(issue => issue.Contains("Unknown")));
        }

        [Test]
        public void SnapshotCompactionPolicy_ProducesExpectedPlans()
        {
            var policy = new SnapshotCompactionPolicy();

            Assert.AreEqual(SnapshotCompactionEligibility.CompactCandidate, policy.Evaluate(SnapshotFamily.Hive, SnapshotIntegrityVerdict.Valid, false, false, true).Eligibility);
            Assert.AreEqual(SnapshotCompactionEligibility.KeepFull, policy.Evaluate(SnapshotFamily.Hive, SnapshotIntegrityVerdict.Valid, true, false, true).Eligibility);
            Assert.AreEqual(SnapshotCompactionEligibility.Forbidden, policy.Evaluate(SnapshotFamily.Hive, SnapshotIntegrityVerdict.Blocked, false, false, true).Eligibility);
            Assert.AreEqual(SnapshotCompactionEligibility.NeedsMigration, policy.Evaluate(SnapshotFamily.Hive, SnapshotIntegrityVerdict.Valid, false, true, true).Eligibility);
        }

        [Test]
        public void LongRunStorageBudget_ReportsSoftHardAndUnknownScopes()
        {
            var budget = new LongRunStorageBudget(new[]
            {
                new LongRunStorageBudgetRule(LongRunStorageScope.Snapshots, 10, 20),
                new LongRunStorageBudgetRule(LongRunStorageScope.QaEvidence, 1, 2),
                new LongRunStorageBudgetRule(LongRunStorageScope.Unknown, 0, 0)
            });
            LongRunStorageBudgetDiagnostics diagnostics = budget.Evaluate(new Dictionary<LongRunStorageScope, int>
            {
                [LongRunStorageScope.Snapshots] = 15,
                [LongRunStorageScope.QaEvidence] = 99
            }, criticalEvidence: true);

            Assert.IsTrue(diagnostics.Findings.Any(f => f.Severity == SnapshotIntegritySeverity.Warning));
            Assert.IsFalse(diagnostics.Findings.Any(f => f.Scope == LongRunStorageScope.QaEvidence));
            Assert.IsTrue(diagnostics.Findings.Any(f => f.Scope == LongRunStorageScope.Unknown));
        }

        [Test]
        public void PersistenceAuditTrail_OrdersAndDiagnosesEntries()
        {
            var trail = new PersistenceAuditTrail(new[]
            {
                new PersistenceAuditEntry(2, PersistenceAuditActor.UnityWorker, PersistenceAuditAction.ValidateSnapshot, PersistenceDataKind.HiveSave, PersistentDataClass.GameplayState, "ok", "BEE-215"),
                new PersistenceAuditEntry(1, PersistenceAuditActor.Unknown, PersistenceAuditAction.DestructiveForbidden, PersistenceDataKind.HiveSave, PersistentDataClass.GameplayState, "blocked", "BEE-215", containsSecret: true)
            });

            Assert.AreEqual(1, trail.Entries[0].Revision);
            Assert.IsTrue(trail.Diagnostics.Issues.Any(issue => issue.Contains("Actor")));
            Assert.IsTrue(trail.Diagnostics.Issues.Any(issue => issue.Contains("Destructive")));
            Assert.IsTrue(trail.Diagnostics.Issues.Any(issue => issue.Contains("secret")));
        }

        [Test]
        public void DataRecoveryPlan_IsNonDestructiveAndEvidenceAware()
        {
            Assert.AreEqual(DataRecoveryVerdict.Quarantine, new DataRecoveryPlan(DataRecoveryTrigger.InvalidChecksum, false).Verdict);
            Assert.AreEqual(DataRecoveryVerdict.NeedsMigration, new DataRecoveryPlan(DataRecoveryTrigger.MissingMigration, false).Verdict);
            Assert.AreEqual(DataRecoveryVerdict.ManualReview, new DataRecoveryPlan(DataRecoveryTrigger.DeadReference, false).Verdict);
            Assert.AreEqual(DataRecoveryVerdict.Blocked, new DataRecoveryPlan(DataRecoveryTrigger.CriticalEvidence, true).Verdict);
        }

        [Test]
        public void CrossVersionLoadMatrix_SortsScenariosAndKeepsEvidenceGaps()
        {
            var matrix = new CrossVersionLoadMatrix(new[]
            {
                new CrossVersionLoadScenario("supported", "1", "1", new SaveMigrationVersion(1), new SnapshotSchemaVersion(1), true, CrossVersionLoadVerdict.Supported),
                new CrossVersionLoadScenario("blocked", "1", "1", new SaveMigrationVersion(1), new SnapshotSchemaVersion(0), false, CrossVersionLoadVerdict.Blocked),
                new CrossVersionLoadScenario("gap", "1", "1", new SaveMigrationVersion(1), new SnapshotSchemaVersion(1), false, CrossVersionLoadVerdict.InsufficientEvidence)
            });

            Assert.AreEqual("blocked", matrix.Scenarios[0].Id);
            Assert.IsTrue(matrix.Scenarios.Any(s => s.ExpectedVerdict == CrossVersionLoadVerdict.Supported));
            Assert.IsTrue(matrix.Scenarios.Any(s => s.ExpectedVerdict == CrossVersionLoadVerdict.Blocked));
            Assert.IsTrue(matrix.Diagnostics.Issues.Any(issue => issue.Contains("gap")));
        }

        [Test]
        public void PersistentContentRegistryLink_ResolvesWithoutDuplicatingContent()
        {
            var link = new PersistentContentRegistryLink();
            var registry = new Dictionary<string, string> { ["building-honeycomb"] = "1" };

            Assert.AreEqual(ContentRegistryLinkStatus.Resolved, link.Resolve(new PersistentContentRef("building-honeycomb", "building", "1"), registry, false, false));
            Assert.AreEqual(ContentRegistryLinkStatus.MissingDefinition, link.Resolve(new PersistentContentRef("missing", "building", "1"), registry, false, false));
            Assert.AreEqual(ContentRegistryLinkStatus.DeprecatedDefinition, link.Resolve(new PersistentContentRef("building-honeycomb", "building", "1"), registry, true, false));
            Assert.AreEqual(ContentRegistryLinkStatus.ForbiddenDuplicate, link.Resolve(new PersistentContentRef("building-honeycomb", "building", "1"), registry, false, true));
            Assert.AreEqual(ContentRegistryLinkStatus.VersionMismatch, link.Resolve(new PersistentContentRef("building-honeycomb", "building", "2"), registry, false, false));
        }

        [Test]
        public void PersistenceQACoverageMatrix_ExposesMissingAndObsoleteEvidence()
        {
            var matrix = new PersistenceQACoverageMatrix(new[]
            {
                new PersistenceQACoverageCell(PersistenceQACoverageAxis.Integrity, "integrity-evidence"),
                new PersistenceQACoverageCell(PersistenceQACoverageAxis.Migrations, string.Empty),
                new PersistenceQACoverageCell(PersistenceQACoverageAxis.Recovery, "old-recovery", obsolete: true)
            });

            Assert.IsTrue(matrix.Cells.Any(c => c.Axis == PersistenceQACoverageAxis.Integrity && c.Covered));
            Assert.IsTrue(matrix.Diagnostics.Gaps.Any(g => g.Axis == PersistenceQACoverageAxis.Migrations));
            Assert.IsTrue(matrix.Diagnostics.Gaps.Any(g => g.Axis == PersistenceQACoverageAxis.Recovery && g.Reason.Contains("obsolete")));
        }

        [Test]
        public void DataGovernanceGate_ReturnsExpectedVerdicts()
        {
            var gate = new DataGovernanceGate();
            var ready = new[] { new DataGovernanceCriterion("classification", true, false, true), new DataGovernanceCriterion("audit", true, false, false) };

            Assert.AreEqual(DataGovernanceVerdict.Ready, gate.Evaluate(ready, false).Verdict);
            Assert.AreEqual(DataGovernanceVerdict.NeedsRevision, gate.Evaluate(new[] { new DataGovernanceCriterion("classification", false, false, false) }, false).Verdict);
            Assert.AreEqual(DataGovernanceVerdict.Blocked, gate.Evaluate(new[] { new DataGovernanceCriterion("destructive-action", false, false, true) }, false).Verdict);
            Assert.AreEqual(DataGovernanceVerdict.ReadyWithWarnings, gate.Evaluate(new[] { new DataGovernanceCriterion("qa-gap", true, true, false) }, false).Verdict);
            Assert.AreEqual(DataGovernanceVerdict.Blocked, gate.Evaluate(ready, true).Verdict);
        }
    }
}
