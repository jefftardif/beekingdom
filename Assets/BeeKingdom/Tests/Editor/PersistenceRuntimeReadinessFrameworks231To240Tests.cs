using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Save;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PersistenceRuntimeReadinessFrameworks231To240Tests
    {
        [Test]
        public void SaveLoadRuntimeBoundary_BlocksOutOfScopeRuntimeActions()
        {
            var boundary = new SaveLoadRuntimeBoundary();

            Assert.IsFalse(boundary.Validate(SaveLoadRuntimeIntent.SavePreview, new SaveLoadRuntimeInputPort("snapshot", new SnapshotSchemaVersion(1), "hive"), false, false, false, true).Gaps.Any());
            Assert.IsTrue(boundary.Validate(SaveLoadRuntimeIntent.LoadPreview, new SaveLoadRuntimeInputPort("snapshot", new SnapshotSchemaVersion(0), "hive"), false, false, false, false).Gaps.Contains(SaveLoadRuntimeGap.RuntimeNotImplemented));
            Assert.IsTrue(boundary.Validate(SaveLoadRuntimeIntent.SavePreview, new SaveLoadRuntimeInputPort("snapshot", new SnapshotSchemaVersion(1), "hive"), true, false, false, true).Gaps.Contains(SaveLoadRuntimeGap.StorageFinalOutOfScope));
            Assert.IsTrue(boundary.Validate(SaveLoadRuntimeIntent.CompatibilityCheck, new SaveLoadRuntimeInputPort("snapshot", new SnapshotSchemaVersion(1), "hive"), false, true, false, true).Gaps.Contains(SaveLoadRuntimeGap.MigrationExecutionBlocked));
            Assert.IsTrue(boundary.Validate(SaveLoadRuntimeIntent.ServerHandoffCheck, new SaveLoadRuntimeInputPort("snapshot", new SnapshotSchemaVersion(1), "hive"), false, false, true, true).Gaps.Contains(SaveLoadRuntimeGap.ServerAnalysisRequired));
        }

        [Test]
        public void PersistenceFixtureCatalog_ReportsFixtureGapsAndSortsStable()
        {
            var catalog = new PersistenceFixtureCatalog(new[]
            {
                new PersistenceFixtureDescriptor("valid", PersistenceFixtureDomain.HiveSnapshot, new SnapshotSchemaVersion(1), "hive", "evidence", PersistenceFixtureStatus.Available),
                new PersistenceFixtureDescriptor("invented", PersistenceFixtureDomain.WorldSnapshot, new SnapshotSchemaVersion(1), "world", string.Empty, PersistenceFixtureStatus.Available),
                new PersistenceFixtureDescriptor("unknown", PersistenceFixtureDomain.Unknown, new SnapshotSchemaVersion(0), string.Empty, string.Empty, PersistenceFixtureStatus.Forbidden)
            });

            Assert.AreEqual("valid", catalog.Fixtures[0].Name);
            Assert.IsTrue(catalog.Gaps.Contains(PersistenceFixtureGap.FixtureSourceMissing));
            Assert.IsTrue(catalog.Gaps.Contains(PersistenceFixtureGap.FixtureEvidenceInvented));
            Assert.IsTrue(catalog.Gaps.Contains(PersistenceFixtureGap.FixtureSchemaUnknown));
            Assert.IsTrue(catalog.Gaps.Contains(PersistenceFixtureGap.FixtureForbiddenForRuntime));
        }

        [Test]
        public void MigrationDryRunScenario_ProducesExpectedVerdictsWithoutMutation()
        {
            var scenario = new MigrationDryRunScenario();
            var steps = new[] { new MigrationDryRunStep(new SaveMigrationVersion(1), new SaveMigrationVersion(2)) };

            Assert.AreEqual(MigrationDryRunVerdict.WouldPass, scenario.Evaluate(new SaveMigrationVersion(1), new SaveMigrationVersion(2), steps, new[] { new MigrationDryRunPrecondition("ok", true) }, true, false, false));
            Assert.AreEqual(MigrationDryRunVerdict.BlockedByUnknownVersion, scenario.Evaluate(new SaveMigrationVersion(9), new SaveMigrationVersion(2), steps, null, true, false, false));
            Assert.AreEqual(MigrationDryRunVerdict.BlockedByCycle, scenario.Evaluate(new SaveMigrationVersion(1), new SaveMigrationVersion(2), steps, null, true, true, false));
            Assert.AreEqual(MigrationDryRunVerdict.BlockedByMissingFixture, scenario.Evaluate(new SaveMigrationVersion(1), new SaveMigrationVersion(2), steps, null, false, false, false));
            Assert.AreEqual(MigrationDryRunVerdict.ForbiddenRuntimeExecution, scenario.Evaluate(new SaveMigrationVersion(1), new SaveMigrationVersion(2), steps, null, true, false, true));
        }

        [Test]
        public void SnapshotVerificationHarness_ReturnsFindingsAndBlocksRepair()
        {
            var harness = new SnapshotVerificationHarness();

            Assert.AreEqual(SnapshotVerificationVerdict.Verified, harness.Verify(true, true, true, true, true, true, false).Verdict);
            Assert.AreEqual(SnapshotVerificationVerdict.Invalid, harness.Verify(false, true, true, true, true, true, false).Verdict);
            Assert.IsTrue(harness.Verify(true, false, true, true, true, true, false).Diagnostics.Findings.Contains(SnapshotVerificationFinding.IdentityCollision));
            Assert.IsTrue(harness.Verify(true, true, true, false, true, true, false).Diagnostics.Findings.Contains(SnapshotVerificationFinding.RegistryReferenceMissing));
            Assert.AreEqual(SnapshotVerificationVerdict.Blocked, harness.Verify(true, true, true, true, true, true, true).Verdict);
        }

        [Test]
        public void RedactionPreviewContract_BlocksUnsafeChannelsHashingAndMutation()
        {
            var registry = new RedactionRequirementRegistry(new[]
            {
                new RedactionRequirement(SensitiveFieldClass.CorrelationId, RedactionOutputRule.ClientSafe),
                new RedactionRequirement(SensitiveFieldClass.ServerDiagnostic, RedactionOutputRule.QAOnly)
            });
            var preview = new RedactionPreviewContract();

            Assert.AreEqual(RedactionPreviewVerdict.PreviewAllowed, preview.Preview(new RedactionPreviewRequest(SensitiveFieldClass.CorrelationId, RedactionPreviewChannel.ClientSafe, false, false), registry).Item.Verdict);
            Assert.AreEqual(RedactionPreviewVerdict.BlockedByForbiddenChannel, preview.Preview(new RedactionPreviewRequest(SensitiveFieldClass.ServerDiagnostic, RedactionPreviewChannel.ClientSafe, false, false), registry).Item.Verdict);
            Assert.AreEqual(RedactionPreviewVerdict.BlockedByUnclassifiedSecret, preview.Preview(new RedactionPreviewRequest(SensitiveFieldClass.Token, RedactionPreviewChannel.ClientSafe, false, false), registry).Item.Verdict);
            Assert.IsTrue(preview.Preview(new RedactionPreviewRequest(SensitiveFieldClass.CorrelationId, RedactionPreviewChannel.ClientSafe, false, true), registry).Diagnostics.Contains(RedactionPreviewDiagnostics.HashingOutOfScope));
            Assert.IsTrue(preview.Preview(new RedactionPreviewRequest(SensitiveFieldClass.CorrelationId, RedactionPreviewChannel.ClientSafe, true, false), registry).Diagnostics.Contains(RedactionPreviewDiagnostics.SourceMutationRequested));
        }

        [Test]
        public void PersistenceObservabilityHook_ValidatesCorrelationEvidenceAndStableOrder()
        {
            var contract = new PersistenceObservationHookContract();
            var payload = new PersistenceObservationPayload(new PersistenceCorrelationId("corr"), PersistenceObservabilityHook.SaveLoadBoundaryObserved, "BEE-231", "hive", PersistenceObservationSeverity.Warning, "code", "evidence", 2, "id-b");
            var missing = new PersistenceObservationPayload(new PersistenceCorrelationId(string.Empty), PersistenceObservabilityHook.SaveLoadBoundaryObserved, "BEE-231", "hive", PersistenceObservationSeverity.Warning, "code", string.Empty, 1, "id-a");
            var trail = new PersistenceObservationTrail(new[] { payload, missing });

            Assert.IsFalse(contract.Validate(payload, false).Any());
            Assert.IsTrue(contract.Validate(missing, false).Contains(PersistenceObservationDiagnostics.MissingCorrelationId));
            Assert.IsTrue(contract.Validate(missing, false).Contains(PersistenceObservationDiagnostics.PayloadEvidenceMissing));
            Assert.IsTrue(contract.Validate(payload, true).Contains(PersistenceObservationDiagnostics.MutableEventRequested));
            Assert.AreEqual(1, trail.Payloads[0].LogicalTick);
        }

        [Test]
        public void SaveLoadDemoReadModelBuilder_MapsBadgesWarningsAndSections()
        {
            var builder = new SaveLoadDemoReadModelBuilder();

            Assert.AreEqual(SaveLoadDemoReadinessBadge.ReadyForPreview, builder.Build(true, true, false, false).Badge);
            Assert.AreEqual(SaveLoadDemoReadinessBadge.ReadyWithWarnings, builder.Build(false, true, false, false).Badge);
            Assert.AreEqual(SaveLoadDemoReadinessBadge.ServerAnalysisRequired, builder.Build(true, false, false, false).Badge);
            Assert.AreEqual(SaveLoadDemoReadinessBadge.Blocked, builder.Build(true, true, true, false).Badge);
            Assert.IsTrue(builder.Build(true, true, false, true).Warnings.Contains(SaveLoadDemoWarning.SecretChannelBlocked));
        }

        [Test]
        public void PersistenceRegressionSuite_DetectsInvalidScenariosAndCoverageGaps()
        {
            var suite = new PersistenceRegressionSuite(new[]
            {
                new PersistenceRegressionScenario("boundary-positive", PersistenceRegressionCategory.Boundary, PersistenceRegressionExpectedVerdict.Positive, PersistenceRegressionStatus.UnitTestExpected, new PersistenceRegressionEvidenceLink("ev")),
                new PersistenceRegressionScenario("backend-future", PersistenceRegressionCategory.BackendReadiness, PersistenceRegressionExpectedVerdict.Blocking, PersistenceRegressionStatus.IntegrationFuture, new PersistenceRegressionEvidenceLink("ev")),
                new PersistenceRegressionScenario("invalid", PersistenceRegressionCategory.DemoReadModel, null, PersistenceRegressionStatus.ContractReady, null)
            });

            Assert.IsTrue(suite.Diagnostics.Contains(PersistenceRegressionDiagnostics.ExpectedVerdictMissing));
            Assert.IsTrue(suite.Diagnostics.Contains(PersistenceRegressionDiagnostics.EvidenceLinkMissing));
            Assert.IsTrue(suite.Diagnostics.Contains(PersistenceRegressionDiagnostics.RuntimeClaimInvented));
            Assert.IsTrue(suite.Diagnostics.Contains(PersistenceRegressionDiagnostics.CategoryCoverageGap));
        }

        [Test]
        public void BackendPersistenceReadinessMatrix_BlocksPrematureBackendImplementation()
        {
            var matrix = new BackendPersistenceReadinessMatrix(new[]
            {
                new BackendPersistenceRequirementRow("BEE-231", true, true, true, false, true, BackendPersistenceReadinessStatus.ReadyForServerAnalysis, string.Empty),
                new BackendPersistenceRequirementRow("BEE-239", false, true, true, true, true, BackendPersistenceReadinessStatus.BlockedBySqlScope, "sql"),
                new BackendPersistenceRequirementRow("BEE-238", false, true, true, false, true, BackendPersistenceReadinessStatus.BlockedByRuntimeScope, "service"),
                new BackendPersistenceRequirementRow(string.Empty, false, false, false, false, false, BackendPersistenceReadinessStatus.NeedsBeeRevision, "missing")
            }, 230);

            Assert.IsTrue(matrix.Diagnostics.Contains(BackendPersistenceDiagnostics.ServerAnalysisMissing));
            Assert.IsTrue(matrix.Diagnostics.Contains(BackendPersistenceDiagnostics.SqlImplementationRequested));
            Assert.IsTrue(matrix.Diagnostics.Contains(BackendPersistenceDiagnostics.ServiceCreationRequested));
            Assert.IsTrue(matrix.Diagnostics.Contains(BackendPersistenceDiagnostics.RequirementSourceMissing));
        }

        [Test]
        public void PersistenceRuntimeReadinessGate_ProducesExpectedVerdicts()
        {
            var gate = new PersistenceRuntimeReadinessGate();
            var ready = new[] { new PersistenceRuntimeReadinessCriterion("boundary", true, false, false), new PersistenceRuntimeReadinessCriterion("fixtures", true, false, false) };

            Assert.AreEqual(PersistenceRuntimeReadinessVerdict.ReadyForDesignReview, gate.Evaluate(ready, true, false, false, false).Verdict);
            Assert.AreEqual(PersistenceRuntimeReadinessVerdict.BlockedByServerAnalysis, gate.Evaluate(ready, false, false, false, false).Verdict);
            Assert.AreEqual(PersistenceRuntimeReadinessVerdict.BlockedByBee241Premature, gate.Evaluate(ready, true, true, false, false).Verdict);
            Assert.AreEqual(PersistenceRuntimeReadinessVerdict.BlockedByRuntimeScope, gate.Evaluate(ready, true, false, true, false).Verdict);
            Assert.AreEqual(PersistenceRuntimeReadinessVerdict.NeedsBeeRevision, gate.Evaluate(ready, true, false, false, true).Verdict);
        }
    }
}
