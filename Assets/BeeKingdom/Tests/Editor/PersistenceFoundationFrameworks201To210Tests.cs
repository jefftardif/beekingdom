using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Save;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PersistenceFoundationFrameworks201To210Tests
    {
        [Test]
        public void PersistenceBoundaryInventory_ValidatesOwnersAndDemoReadOnly()
        {
            var inventory = new PersistenceBoundaryInventory(new[]
            {
                new PersistenceBoundaryEntry(PersistenceDataKind.HiveSave, PersistenceBoundaryOwner.UnityLocalSave, true),
                new PersistenceBoundaryEntry(PersistenceDataKind.ServerColonyState, PersistenceBoundaryOwner.FutureSql, false),
                new PersistenceBoundaryEntry(PersistenceDataKind.DemoEvidence, PersistenceBoundaryOwner.DemoReadOnly, true),
                new PersistenceBoundaryEntry(PersistenceDataKind.QaEvidence, PersistenceBoundaryOwner.Unknown, false)
            });

            Assert.AreEqual(PersistenceBoundaryOwner.UnityLocalSave, inventory.Entries.First().Owner);
            Assert.IsTrue(inventory.Entries.Any(e => e.Owner == PersistenceBoundaryOwner.FutureSql));
            Assert.IsTrue(inventory.Diagnostics.Issues.Any(issue => issue.Contains("Demo")));
            Assert.IsTrue(inventory.Diagnostics.Issues.Any(issue => issue.Contains("owner")));
        }

        [Test]
        public void SaveMigrationManifest_ResolvesKnownMigrationAndDetectsBrokenChains()
        {
            var manifest = new SaveMigrationManifest(new[]
            {
                new SaveMigrationEntry(new SaveMigrationVersion(1), new SaveMigrationVersion(2), "hive", new[] { new SaveMigrationPrecondition("schema-known") }, "request snapshot"),
                new SaveMigrationEntry(new SaveMigrationVersion(3), new SaveMigrationVersion(4), "world", null, "blocked")
            });
            var empty = new SaveMigrationManifest(null);

            Assert.IsNotNull(manifest.Resolve(new SaveMigrationVersion(1)));
            Assert.IsNull(manifest.Resolve(new SaveMigrationVersion(9)));
            Assert.IsTrue(manifest.Diagnostics.Issues.Any(issue => issue.Contains("broken")));
            Assert.IsTrue(empty.Diagnostics.Issues.Any(issue => issue.Contains("empty")));
        }

        [Test]
        public void SnapshotSchemaRegistry_ValidatesKnownObsoleteAndUnknownFamilies()
        {
            var registry = new SnapshotSchemaRegistry(new[]
            {
                new SnapshotSchemaRequirement(SnapshotFamily.Hive, new SnapshotSchemaVersion(3), new SnapshotSchemaVersion(2))
            });

            Assert.IsFalse(registry.Validate(SnapshotFamily.Hive, new SnapshotSchemaVersion(3)).Issues.Any());
            Assert.IsTrue(registry.Validate(SnapshotFamily.Unknown, new SnapshotSchemaVersion(1)).Issues.Any());
            Assert.IsTrue(registry.Validate(SnapshotFamily.Hive, new SnapshotSchemaVersion(1)).Issues.Any(issue => issue.Contains("obsolete")));
            Assert.AreEqual(SnapshotFamily.Hive, registry.Requirements[0].Family);
        }

        [Test]
        public void PersistentIdentityMap_ResolvesAliasesAndReportsConflicts()
        {
            var map = new PersistentIdentityMap(new[]
            {
                new PersistentIdentity("hive-1", PersistentIdentityDomain.Hive),
                new PersistentIdentity("bad", PersistentIdentityDomain.Unknown)
            }, new[]
            {
                new PersistentIdentityAlias("main", "hive-1"),
                new PersistentIdentityAlias("conflict", "hive-1"),
                new PersistentIdentityAlias("conflict", "missing"),
                new PersistentIdentityAlias("dead", "missing")
            });

            Assert.AreEqual("hive-1", map.Resolve("main").Id);
            Assert.IsTrue(map.Diagnostics.Issues.Any(issue => issue.Contains("Alias conflict")));
            Assert.IsTrue(map.Diagnostics.Issues.Any(issue => issue.Contains("Dead reference")));
            Assert.IsTrue(map.Diagnostics.Issues.Any(issue => issue.Contains("domain")));
        }

        [Test]
        public void SaveCompatibilityMatrix_ReturnsVerdictsAndUnknownGaps()
        {
            var matrix = new SaveCompatibilityMatrix(new[]
            {
                new SaveCompatibilityCell(SaveCompatibilityAxis.SaveVersion, "1", SaveCompatibilityVerdict.Compatible, "current"),
                new SaveCompatibilityCell(SaveCompatibilityAxis.SnapshotSchema, "old", SaveCompatibilityVerdict.NeedsMigration, "obsolete"),
                new SaveCompatibilityCell(SaveCompatibilityAxis.ProtocolVersion, "blocked", SaveCompatibilityVerdict.Blocked, "blocked"),
                new SaveCompatibilityCell(SaveCompatibilityAxis.ClientVersion, "mystery", SaveCompatibilityVerdict.Unknown, "missing")
            });

            Assert.AreEqual(SaveCompatibilityVerdict.Compatible, matrix.Evaluate(SaveCompatibilityAxis.SaveVersion, "1"));
            Assert.AreEqual(SaveCompatibilityVerdict.NeedsMigration, matrix.Evaluate(SaveCompatibilityAxis.SnapshotSchema, "old"));
            Assert.AreEqual(SaveCompatibilityVerdict.Blocked, matrix.Evaluate(SaveCompatibilityAxis.ProtocolVersion, "blocked"));
            Assert.AreEqual(SaveCompatibilityVerdict.Unknown, matrix.Evaluate(SaveCompatibilityAxis.ServerContract, "none"));
            Assert.IsTrue(matrix.Diagnostics.Issues.Any());
        }

        [Test]
        public void DataRetentionPolicy_DeclaresNonDestructiveStatuses()
        {
            var policy = new DataRetentionPolicy(new[]
            {
                new DataRetentionRule(DataRetentionScope.OfficialSave, DataRetentionStatus.Keep),
                new DataRetentionRule(DataRetentionScope.AuthorityTelemetry, DataRetentionStatus.Expire),
                new DataRetentionRule(DataRetentionScope.QaEvidence, DataRetentionStatus.Archive),
                new DataRetentionRule(DataRetentionScope.AccountAdjacentId, DataRetentionStatus.Forbidden)
            });

            Assert.AreEqual(DataRetentionStatus.Keep, policy.Evaluate(DataRetentionScope.OfficialSave));
            Assert.AreEqual(DataRetentionStatus.Expire, policy.Evaluate(DataRetentionScope.AuthorityTelemetry));
            Assert.AreEqual(DataRetentionStatus.Archive, policy.Evaluate(DataRetentionScope.QaEvidence));
            Assert.AreEqual(DataRetentionStatus.Forbidden, policy.Evaluate(DataRetentionScope.AccountAdjacentId));
            Assert.IsTrue(policy.Diagnostics.Issues.Any());
        }

        [Test]
        public void SnapshotIntegrityCheck_ReturnsExpectedVerdicts()
        {
            var check = new SnapshotIntegrityCheck();

            Assert.AreEqual(SnapshotIntegrityVerdict.Valid, check.Validate(true, false, false, false).Verdict);
            Assert.AreEqual(SnapshotIntegrityVerdict.Blocked, check.Validate(false, false, false, false).Verdict);
            Assert.AreEqual(SnapshotIntegrityVerdict.Invalid, check.Validate(true, true, false, false).Verdict);
            Assert.AreEqual(SnapshotIntegrityVerdict.ValidWithWarnings, check.Validate(true, false, true, true).Verdict);
        }

        [Test]
        public void PersistenceFailureCatalog_MapsStableCodes()
        {
            var catalog = new PersistenceFailureCatalog();

            Assert.AreEqual(PersistenceFailureCode.InvalidChecksum, catalog.Map("checksum invalid").Failure.Code);
            Assert.AreEqual(PersistenceFailureCode.MissingMigration, catalog.Map("migration absent").Failure.Code);
            Assert.AreEqual(PersistenceFailureCode.DeadReference, catalog.Map("dead reference").Failure.Code);
            Assert.AreEqual(PersistenceFailureCode.PartialData, catalog.Map("partial data").Failure.Code);
            Assert.IsTrue(catalog.Failures.Select(f => f.Code).Distinct().Count() == catalog.Failures.Count);
        }

        [Test]
        public void SaveLoadQAEvidenceBridge_LinksEvidenceAndRejectsOrphans()
        {
            var bridge = new SaveLoadQAEvidenceBridge();
            IReadOnlyList<SaveLoadQAEvidenceLink> links = bridge.Link(new[]
            {
                new SaveLoadQAEvidence("integrity", SaveLoadQAEvidenceSource.SnapshotIntegrity, "BEE-207", SnapshotFamily.Hive, PersistenceFailureCode.InvalidChecksum),
                new SaveLoadQAEvidence("missing-code", SaveLoadQAEvidenceSource.FailureCatalog, "BEE-208", SnapshotFamily.World, null),
                new SaveLoadQAEvidence("orphan", SaveLoadQAEvidenceSource.QaReport, string.Empty, SnapshotFamily.Unknown, null)
            });

            Assert.IsTrue(links.Any(link => link.Evidence.Key == "integrity" && link.Verdict == SaveLoadQAEvidenceVerdict.Linked));
            Assert.IsTrue(links.Any(link => link.Evidence.Key == "missing-code" && link.Verdict == SaveLoadQAEvidenceVerdict.Warning));
            Assert.IsTrue(bridge.Diagnostics.Issues.Any(issue => issue.Contains("Orphan")));
        }

        [Test]
        public void PersistenceFoundationGate_ProducesReadinessVerdictsAndBlocksBee211()
        {
            var gate = new PersistenceFoundationGate();
            var ready = new[] { new PersistenceFoundationCriterion("boundaries", true, false, true), new PersistenceFoundationCriterion("integrity", true, false, true) };

            Assert.AreEqual(PersistenceFoundationVerdict.Ready, gate.Evaluate(ready, false).Verdict);
            Assert.AreEqual(PersistenceFoundationVerdict.ReadyWithWarnings, gate.Evaluate(new[] { new PersistenceFoundationCriterion("retention", true, true, false) }, false).Verdict);
            Assert.AreEqual(PersistenceFoundationVerdict.NeedsRevision, gate.Evaluate(new[] { new PersistenceFoundationCriterion("migration", false, false, false) }, false).Verdict);
            Assert.AreEqual(PersistenceFoundationVerdict.Blocked, gate.Evaluate(ready, true).Verdict);
        }
    }
}
