using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ProtocolAuthorityFrameworks161To170Tests
    {
        [Test]
        public void ProtocolRegistry_ResolvesSupportedAndRejectsUnknownVersions()
        {
            ProtocolVersionRegistry registry = ProtocolVersionRegistry.CreateDefault();

            ProtocolVersionDiagnostics supported = registry.Resolve(new ProtocolVersion(1, 0));
            ProtocolVersionDiagnostics unknown = registry.Resolve(new ProtocolVersion(9, 9));
            ProtocolVersionDiagnostics blocked = registry.Resolve(new ProtocolVersion(0, 1));

            Assert.IsTrue(supported.Known);
            Assert.IsTrue(supported.Supported);
            Assert.IsFalse(unknown.Known);
            Assert.IsFalse(blocked.Supported);
            Assert.AreEqual(ProtocolVersionStatus.Blocked, blocked.Status);
            CollectionAssert.AreEqual(
                registry.Entries.Select(entry => entry.Version).OrderBy(version => version).ToArray(),
                registry.Entries.Select(entry => entry.Version).ToArray());
            Assert.IsNotNull(registry.Entries.First(entry => entry.Status == ProtocolVersionStatus.Migrated).Migration);
        }

        [Test]
        public void ContractMatrix_ReportsCompatibleMissingAndBlockedVersionCells()
        {
            ProtocolVersionRegistry registry = ProtocolVersionRegistry.CreateDefault();
            SharedContractCompatibilityMatrix matrix = SharedContractCompatibilityMatrix.CreateDefault(registry);

            SharedContractCompatibilityDiagnostics compatible = matrix.Evaluate("CommandEnvelope", new ProtocolVersion(1, 0));
            SharedContractCompatibilityDiagnostics missing = matrix.Evaluate("MissingContract", new ProtocolVersion(1, 0));
            SharedContractCompatibilityDiagnostics blocked = matrix.Evaluate("CommandEnvelope", new ProtocolVersion(0, 1));

            Assert.IsFalse(compatible.HasIncompatibility);
            Assert.AreEqual(SharedContractCompatibilityVerdict.Missing, missing.Cells[0].Verdict);
            Assert.AreEqual(SharedContractCompatibilityVerdict.Incompatible, blocked.Cells[0].Verdict);
            Assert.IsTrue(matrix.Cells.Any(cell => cell.Consumer == SharedContractConsumer.QaReport));
            CollectionAssert.AreEqual(
                matrix.Cells.Select(cell => cell.ContractName).OrderBy(name => name).ToArray(),
                matrix.Cells.Select(cell => cell.ContractName).ToArray());
        }

        [Test]
        public void SnapshotHandoff_ValidatesDigestVersionScopeAndSource()
        {
            var valid = new SnapshotHandoffEnvelope(
                new SnapshotHandoffMetadata("snapshot-1", "server-authority", 12, new ProtocolVersion(1, 0), SnapshotHandoffScope.Colony),
                "abc",
                new SnapshotHandoffPayloadRef("payload-ref"));

            var missingDigest = new SnapshotHandoffEnvelope(valid.Metadata, string.Empty, valid.PayloadRef);
            var badVersion = new SnapshotHandoffEnvelope(
                new SnapshotHandoffMetadata("snapshot-2", "server-authority", 12, new ProtocolVersion(9, 9), SnapshotHandoffScope.Colony),
                "abc",
                new SnapshotHandoffPayloadRef("payload-ref"));
            var badScope = new SnapshotHandoffEnvelope(
                new SnapshotHandoffMetadata("snapshot-3", string.Empty, 12, new ProtocolVersion(1, 0), SnapshotHandoffScope.Unknown),
                "abc",
                new SnapshotHandoffPayloadRef("payload-ref"));

            Assert.IsTrue(SnapshotHandoffValidator.Validate(valid).Valid);
            Assert.IsFalse(SnapshotHandoffValidator.Validate(missingDigest).Valid);
            Assert.IsFalse(SnapshotHandoffValidator.Validate(badVersion).Valid);
            Assert.IsFalse(SnapshotHandoffValidator.Validate(badScope).Valid);
            Assert.AreEqual("payload-ref", valid.PayloadRef.ReferenceId);
        }

        [Test]
        public void ServerStateDigest_IsStableOrderedVersionedAndRejectsSensitiveFields()
        {
            var builder = new ServerStateDigestBuilder();
            var fieldsA = new[]
            {
                new ServerStateDigestField("food", "10"),
                new ServerStateDigestField("volatile-now", "123", volatileField: true),
                new ServerStateDigestField("bees", "4")
            };
            var fieldsB = new[]
            {
                new ServerStateDigestField("bees", "4"),
                new ServerStateDigestField("food", "10"),
                new ServerStateDigestField("volatile-now", "999", volatileField: true)
            };

            ServerStateDigest first = builder.Build(SnapshotHandoffScope.Colony, 3, new ProtocolVersion(1, 0), fieldsA);
            ServerStateDigest second = builder.Build(SnapshotHandoffScope.Colony, 3, new ProtocolVersion(1, 0), fieldsB);
            ServerStateDigest otherVersion = builder.Build(SnapshotHandoffScope.Colony, 3, new ProtocolVersion(1, 1), fieldsB);
            ServerStateDigest sensitive = builder.Build(SnapshotHandoffScope.Colony, 3, new ProtocolVersion(1, 0), new[] { new ServerStateDigestField("token", "secret", sensitive: true) });

            Assert.AreEqual(first.Checksum, second.Checksum);
            Assert.AreNotEqual(first.Checksum, otherVersion.Checksum);
            Assert.IsFalse(sensitive.Diagnostics.Valid);
        }

        [Test]
        public void ClientReadModelHydrator_HydratesCompatibleSnapshotAndRejectsDigestMismatch()
        {
            var envelope = new SnapshotHandoffEnvelope(
                new SnapshotHandoffMetadata("snapshot-1", "authority", 10, new ProtocolVersion(1, 0), SnapshotHandoffScope.World),
                "digest-1",
                new SnapshotHandoffPayloadRef("payload"));
            var hydrator = new ClientReadModelHydrator();

            ClientHydrationResult hydrated = hydrator.Hydrate(new ClientHydrationInput(envelope, "digest-1", 11, 5));
            ClientHydrationResult stale = hydrator.Hydrate(new ClientHydrationInput(envelope, "digest-1", 25, 5));
            ClientHydrationResult mismatch = hydrator.Hydrate(new ClientHydrationInput(envelope, "other", 11, 5));

            Assert.AreEqual(ClientHydrationStatus.Hydrated, hydrated.Status);
            Assert.AreEqual("snapshot-1", hydrated.ReadModel["snapshotId"]);
            Assert.AreEqual(ClientHydrationStatus.HydratedStale, stale.Status);
            Assert.AreEqual(ClientHydrationStatus.Rejected, mismatch.Status);
            Assert.AreEqual("digest-1", envelope.Digest);
        }

        [Test]
        public void DeltaSyncDryRun_OrdersOperationsAndReportsConflictsWithoutMutatingBaseState()
        {
            var baseState = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
            var contract = new DeltaSyncContract(
                DeltaSyncScope.Colony,
                new ProtocolVersion(1, 0),
                "base",
                "target",
                new[]
                {
                    new DeltaSyncOperation(2, DeltaSyncOperationKind.Remove, "b"),
                    new DeltaSyncOperation(1, DeltaSyncOperationKind.Update, "a", "9")
                });

            DeltaSyncReplayPlan plan = new DeltaSyncDryRun().Replay(contract, baseState, "base");
            DeltaSyncReplayPlan mismatch = new DeltaSyncDryRun().Replay(contract, baseState, "wrong");
            DeltaSyncReplayPlan missingRemove = new DeltaSyncDryRun().Replay(
                new DeltaSyncContract(DeltaSyncScope.Colony, new ProtocolVersion(1, 0), "base", "target", new[] { new DeltaSyncOperation(1, DeltaSyncOperationKind.Remove, "missing") }),
                baseState,
                "base");

            Assert.IsTrue(plan.Accepted);
            Assert.AreEqual("9", plan.ProjectedState["a"]);
            Assert.IsFalse(plan.ProjectedState.ContainsKey("b"));
            Assert.AreEqual("1", baseState["a"]);
            Assert.IsFalse(mismatch.Accepted);
            Assert.IsFalse(missingRemove.Accepted);
        }

        [Test]
        public void AuthoritySessionLifecycle_ControlsTransitionsAndQueueImpact()
        {
            var lifecycle = new AuthoritySessionLifecycle();
            AuthoritySessionContext created = new AuthoritySessionContext("s1", AuthoritySessionState.Created, 0, 2);

            AuthoritySessionDiagnostics joined = lifecycle.TryTransition(created, AuthoritySessionState.Joined, 1);
            AuthoritySessionDiagnostics active = lifecycle.TryTransition(joined.NextContext, AuthoritySessionState.Active, 2);
            AuthoritySessionDiagnostics invalid = lifecycle.TryTransition(new AuthoritySessionContext("s1", AuthoritySessionState.Closed, 3, 2), AuthoritySessionState.Active, 4);
            AuthoritySessionDiagnostics reconnecting = lifecycle.TryTransition(new AuthoritySessionContext("s1", AuthoritySessionState.Suspended, 5, 3), AuthoritySessionState.Reconnecting, 6);
            AuthoritySessionDiagnostics closed = lifecycle.TryTransition(active.NextContext, AuthoritySessionState.Closed, 7);

            Assert.IsTrue(joined.Accepted);
            Assert.IsTrue(active.Accepted);
            Assert.IsFalse(invalid.Accepted);
            Assert.IsTrue(reconnecting.Accepted);
            Assert.AreEqual(0, closed.NextContext.QueuedCommands);
        }

        [Test]
        public void MultiplayerDriftDetector_ProducesStableFindings()
        {
            ServerStateDigest authoritative = new ServerStateDigestBuilder().Build(
                SnapshotHandoffScope.World,
                100,
                new ProtocolVersion(1, 0),
                new[] { new ServerStateDigestField("a", "1") });
            var detector = new MultiplayerDriftDetector();

            MultiplayerDriftDiagnostics clean = detector.Detect(authoritative, new[]
            {
                new MultiplayerDriftSample("client-a", SnapshotHandoffScope.World, 100, new ProtocolVersion(1, 0), authoritative.Checksum)
            }, 5);
            MultiplayerDriftDiagnostics findings = detector.Detect(authoritative, new[]
            {
                new MultiplayerDriftSample("client-b", SnapshotHandoffScope.World, 90, new ProtocolVersion(1, 0), authoritative.Checksum),
                new MultiplayerDriftSample("client-a", SnapshotHandoffScope.World, 100, new ProtocolVersion(2, 0), "wrong")
            }, 5);

            Assert.IsFalse(clean.HasDrift);
            Assert.IsTrue(findings.Findings.Any(finding => finding.Kind == "StaleSample"));
            Assert.AreEqual("DigestMismatch", findings.Findings[0].Kind);
            Assert.AreEqual("client-a", findings.Findings[0].ClientId);
        }

        [Test]
        public void AuthorityTelemetryReport_IsReadOnlySortedAndPreservesDriftSources()
        {
            var diagnostics = new MultiplayerDriftDiagnostics(new[]
            {
                new MultiplayerDriftFinding("client-z", MultiplayerDriftSeverity.Critical, "DigestMismatch", "bad digest")
            });

            AuthorityTelemetryReport empty = AuthorityTelemetryReportBuilder.Empty();
            AuthorityTelemetryReport report = AuthorityTelemetryReportBuilder.FromDrift(diagnostics);

            Assert.IsNotNull(empty);
            Assert.IsTrue(report.Diagnostics.HasCriticalFindings);
            Assert.AreEqual("drift", report.Sections[0].Name);
            Assert.AreEqual("client-z", report.Sections[0].Findings[0].Source);
        }

        [Test]
        public void ProtocolReadinessGate_ReturnsExpectedVerdicts()
        {
            var gate = new ProtocolReadinessGate();
            var readyCriteria = new[] { new ProtocolReadinessCriterion("contracts", true, true), new ProtocolReadinessCriterion("digest", true, true) };
            AuthorityTelemetryReport warningReport = new AuthorityTelemetryReport(new[]
            {
                new AuthorityTelemetrySection("protocol", new[] { new AuthorityTelemetryFinding("registry", AuthorityTelemetrySeverity.Warning, "deprecated supported") })
            });

            ProtocolReadinessReport ready = gate.Evaluate(readyCriteria, AuthorityTelemetryReportBuilder.Empty());
            ProtocolReadinessReport warnings = gate.Evaluate(readyCriteria, warningReport);
            ProtocolReadinessReport blocked = gate.Evaluate(new[] { new ProtocolReadinessCriterion("version", false, true, "Unknown version") }, AuthorityTelemetryReportBuilder.Empty());
            ProtocolReadinessReport needsRevision = gate.Evaluate(readyCriteria, null);

            Assert.AreEqual(ProtocolReadinessVerdict.Ready, ready.Verdict);
            Assert.AreEqual(ProtocolReadinessVerdict.ReadyWithWarnings, warnings.Verdict);
            Assert.AreEqual(ProtocolReadinessVerdict.Blocked, blocked.Verdict);
            Assert.AreEqual(ProtocolReadinessVerdict.NeedsRevision, needsRevision.Verdict);
            CollectionAssert.AreEqual(
                ready.Criteria.Select(criterion => criterion.Name).OrderBy(name => name).ToArray(),
                ready.Criteria.Select(criterion => criterion.Name).ToArray());
        }
    }
}
