using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class ColonyIntegrationEvidenceRuntimeFrameworks261To280Tests
    {
        [Test]
        public void EvidenceRegistry_FlagsMissingSourceLimitContradictionAndInventedRuntimeProof()
        {
            var registry = new ColonyIntegrationEvidenceRegistry(new[]
            {
                new ColonyIntegrationEvidenceRecord("evidence-a", ColonyIntegrationDomain.Demo, string.Empty, null, ColonyIntegrationEvidenceStatus.Contradictory, string.Empty, demoClaimTooStrong: true, runtimeProofInvented: true)
            });

            ColonyIntegrationEvidenceDiagnostics diagnostics = registry.Evaluate();

            Assert.That(diagnostics.Contains(ColonyIntegrationEvidenceDiagnosticCode.EvidenceSourceMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationEvidenceDiagnosticCode.EvidenceLimitMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationEvidenceDiagnosticCode.EvidenceContradiction), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationEvidenceDiagnosticCode.DemoClaimTooStrong), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationEvidenceDiagnosticCode.RuntimeProofInvented), Is.True);
        }

        [Test]
        public void DependencyGraph_FlagsCycleMissingSourceOwnerAndForbiddenEdge()
        {
            var graph = new ColonyDomainDependencyGraph(
                new[] { new ColonyDomainDependencyNode(ColonyDependencyNode.World, string.Empty) },
                new[]
                {
                    new ColonyDomainDependencyEdge(ColonyDependencyNode.World, ColonyDependencyNode.Population, ColonyDependencyEdgeKind.ForbiddenDirectMutation, string.Empty, string.Empty),
                    new ColonyDomainDependencyEdge(ColonyDependencyNode.Population, ColonyDependencyNode.World, ColonyDependencyEdgeKind.ReadModel, "BEE-252", "population")
                });

            ColonyDependencyDiagnostics diagnostics = graph.Evaluate();

            Assert.That(diagnostics.Contains(ColonyDependencyDiagnosticCode.DependencySourceMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyDependencyDiagnosticCode.DependencyOwnerMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyDependencyDiagnosticCode.ForbiddenEdgeDetected), Is.True);
            Assert.That(diagnostics.Contains(ColonyDependencyDiagnosticCode.DependencyCycleDetected), Is.True);
        }

        [Test]
        public void SnapshotConflictSchedulerAndEventContracts_ReportForbiddenStates()
        {
            var snapshot = new ColonyCrossDomainSnapshotContract(
                "snap-a",
                new[] { new CrossDomainSnapshotReference(CrossDomainSnapshotFamily.Population, string.Empty, string.Empty, 1, CrossDomainSnapshotStatus.Missing, "limit") },
                sourceOwnershipMerged: true,
                saveEngineBypassed: true);
            Assert.That(snapshot.Evaluate().Contains(CrossDomainSnapshotDiagnosticCode.SaveEngineBypassed), Is.True);
            Assert.That(snapshot.Evaluate().Contains(CrossDomainSnapshotDiagnosticCode.SourceOwnershipMerged), Is.True);

            var conflicts = new ColonyIntegrationConflictCatalog(new[]
            {
                new ColonyIntegrationConflict(ColonyConflictCategory.EvidenceConflict, string.Empty, ColonyConflictSeverity.Missing, "impact", ColonyConflictNextAction.KeepBlocked, "limit", autoResolved: true, readyStatusClaimed: true, unsafeMessage: true)
            });
            Assert.That(conflicts.Evaluate().Contains(ColonyConflictDiagnosticCode.ConflictAutoResolved), Is.True);
            Assert.That(conflicts.Evaluate().Contains(ColonyConflictDiagnosticCode.ReadyStatusWithConflict), Is.True);

            var plan = new ColonyIntegrationSchedulerPhaseContract(
                new[] { ColonyIntegrationPhase.ReadWorldContext },
                new[]
                {
                    new ColonyIntegrationPhaseDependency(ColonyIntegrationPhase.ReadWorldContext, ColonyIntegrationPhase.GateReview, "BEE-265"),
                    new ColonyIntegrationPhaseDependency(ColonyIntegrationPhase.GateReview, ColonyIntegrationPhase.ReadWorldContext, "BEE-265")
                },
                new Dictionary<ColonyIntegrationPhase, string>(),
                runtimePhaseMutationRequested: true,
                nondeterministicPhaseOrder: true);
            Assert.That(plan.Evaluate().Contains(ColonyIntegrationPhaseDiagnosticCode.PhaseDependencyCycle), Is.True);
            Assert.That(plan.Evaluate().Contains(ColonyIntegrationPhaseDiagnosticCode.RuntimePhaseMutationRequested), Is.True);

            var bridge = new ColonyIntegrationEventBridgeContract(new[]
            {
                new ColonyIntegrationEventBridge(ColonyEventBridgeKind.ForbiddenMutableEvent, string.Empty, new ColonyEventBridgePayloadContract(string.Empty, "payload", isMutable: true), string.Empty, ColonyEventBridgeStatus.Forbidden, "limit", eventBusBypassed: true, consumerAmbiguous: true)
            });
            Assert.That(bridge.Evaluate().Contains(ColonyEventBridgeDiagnosticCode.MutableEventRequested), Is.True);
            Assert.That(bridge.Evaluate().Contains(ColonyEventBridgeDiagnosticCode.EventBusBypassed), Is.True);
        }

        [Test]
        public void DemoQaHandoffAndEvidenceGate_BlockInvalidClaims()
        {
            var scenarioMatrix = new ColonyIntegrationDemoScenarioMatrix(new[]
            {
                new ColonyDemoScenario("scenario-a", null, string.Empty, "visual", string.Empty, new[] { "limit" }, ColonyDemoScenarioStatus.Blocked, gameplayLogicRequested: true, sceneModificationRequested: true)
            });
            Assert.That(scenarioMatrix.Evaluate().Contains(ColonyDemoScenarioDiagnosticCode.DemoSurfaceMissing), Is.True);
            Assert.That(scenarioMatrix.Evaluate().Contains(ColonyDemoScenarioDiagnosticCode.GameplayLogicRequested), Is.True);

            var coverage = new ColonyIntegrationQACoverageMatrix(new[]
            {
                new ColonyQaCoverageCell(ColonyQaCoverageAxis.DemoSurface, string.Empty, ColonyQaCoverageStatus.NeedsQaSpec, ColonyQaCoverageGap.DemoOnly, qaSpecClaimed: true, runtimeTestClaimed: true, coverageGapHidden: true, demoOnlyCoverage: true)
            });
            Assert.That(coverage.Evaluate().Contains(ColonyQaCoverageDiagnosticCode.QaSpecClaimed), Is.True);
            Assert.That(coverage.Evaluate().Contains(ColonyQaCoverageDiagnosticCode.DemoOnlyCoverage), Is.True);

            var handoff = new ColonyIntegrationWorkerHandoffChecklist(new[]
            {
                new WorkerHandoffItem("item-a", string.Empty, null, WorkerHandoffStatus.NeedsQa, "next", architectureDecisionMissing: true, qaGapUnresolved: true, forbiddenImplementationRequested: true)
            });
            Assert.That(handoff.Evaluate().Contains(WorkerHandoffDiagnosticCode.OwnerMissing), Is.True);
            Assert.That(handoff.Evaluate().Contains(WorkerHandoffDiagnosticCode.ForbiddenImplementationRequested), Is.True);

            var gate = new ColonyIntegrationEvidenceGate(new[]
            {
                new ColonyIntegrationEvidenceCriterion("BEE-264", true, conflictOpen: true)
            }, bee271Referenced: true);
            Assert.That(gate.Evaluate().Verdict, Is.EqualTo(ColonyIntegrationEvidenceVerdict.BlockedByBee271Premature));
            Assert.That(gate.Evaluate().Contains(ColonyIntegrationEvidenceGateDiagnosticCode.ConflictOpen), Is.True);
        }

        [Test]
        public void RuntimeBoundaryFixturesReplayAndComparison_ReportReadinessViolations()
        {
            var boundary = new ColonyIntegrationRuntimeBoundary(new[]
            {
                new RuntimeBoundarySurfaceRecord(null, null, "BEE-271", "evidence", "limit", gameplayExecutionRequested: true, managerReplacementRequested: true, engineBypassRequested: true)
            });
            Assert.That(boundary.Evaluate().Contains(RuntimeBoundaryDiagnosticCode.GameplayExecutionRequested), Is.True);
            Assert.That(boundary.Evaluate().Contains(RuntimeBoundaryDiagnosticCode.EngineBypassRequested), Is.True);

            var fixtureCatalog = new ColonyIntegrationScenarioFixtureCatalog(new[]
            {
                new ColonyScenarioFixture("fixture-a", ColonyFixtureDomain.World, string.Empty, null, string.Empty, string.Empty, "limit", ColonyFixtureStatus.Missing, runtimeDataRequested: true)
            });
            Assert.That(fixtureCatalog.Evaluate().Contains(ColonyFixtureDiagnosticCode.FixtureSeedMissing), Is.True);
            Assert.That(fixtureCatalog.Evaluate().Contains(ColonyFixtureDiagnosticCode.FixtureRuntimeDataRequested), Is.True);

            var trace = new ColonyIntegrationReplayTrace("scenario-a", "fixture-a", new ColonyReplayTraceStepRecord[0], new[] { new ColonyReplayTraceCheckpoint("checkpoint-a", string.Empty, "expected") }, replaySystemBypassed: true, parallelGameplayExecution: true, traceOrderUnstable: true);
            Assert.That(trace.Evaluate().Contains(ColonyReplayTraceDiagnosticCode.ReplaySystemBypassed), Is.True);
            Assert.That(trace.Evaluate().Contains(ColonyReplayTraceDiagnosticCode.TraceStepMissing), Is.True);

            var comparison = new ColonyIntegrationStateComparison(new ColonyStateComparisonInputRecord[0], new[] { new ColonyStateDiff("diff-a", string.Empty, ColonyStateDiffSeverity.Missing, "explain", "limit") }, ownershipMismatch: true, autoCorrectionRequested: true);
            Assert.That(comparison.Evaluate().Contains(ColonyStateComparisonDiagnosticCode.ComparisonInputMissing), Is.True);
            Assert.That(comparison.Evaluate().Contains(ColonyStateComparisonDiagnosticCode.AutoCorrectionRequested), Is.True);
        }

        [Test]
        public void FailureDemoBenchmarkDocsReleaseAndRuntimeGate_BlockUnsafeClaims()
        {
            var taxonomy = new ColonyIntegrationFailureTaxonomy(new[]
            {
                new ColonyIntegrationFailureCode(string.Empty, ColonyIntegrationFailureCategory.StateMismatch, ColonyFailureSeverity.Missing, ColonyFailureCause.Unknown, string.Empty, string.Empty, "next", unsafeDetail: true, autoFixSuggested: true)
            });
            Assert.That(taxonomy.Evaluate().Contains(ColonyFailureDiagnosticCode.FailureCodeMissing), Is.True);
            Assert.That(taxonomy.Evaluate().Contains(ColonyFailureDiagnosticCode.AutoFixSuggested), Is.True);

            var validation = new ColonyIntegrationDemoValidationContract(new[]
            {
                new DemoValidationCriterion(ColonyDemoScenarioSurface.DEMO012, string.Empty, 1, new DemoValidationEvidence[0], new DemoValidationLimit[0], DemoValidationStatus.Blocked, separateDemoSpecRequested: true, parallelGameplayDetected: true)
            });
            Assert.That(validation.Evaluate().Contains(DemoValidationDiagnosticCode.DemoEvidenceMissing), Is.True);
            Assert.That(validation.Evaluate().Contains(DemoValidationDiagnosticCode.DemoSpecSeparateRequested), Is.True);

            var benchmark = BenchmarkSignalDiagnostics.Evaluate(new[]
            {
                new ColonyIntegrationBenchmarkSignal(BenchmarkSignalKind.ReadModelBuildCost, string.Empty, null, new[] { new BenchmarkSignalRisk("risk-a", "hidden", hidden: true) }, BenchmarkSignalStatus.SignalMissing, finalBenchmarkClaimed: true, measurementRuntimeMissing: true)
            });
            Assert.That(benchmark.Contains(BenchmarkSignalDiagnosticCode.FinalBenchmarkClaimed), Is.True);
            Assert.That(benchmark.Contains(BenchmarkSignalDiagnosticCode.PerformanceRiskHidden), Is.True);

            var docs = new ColonyIntegrationDocumentationSync(new[]
            {
                new DocumentationSyncRecord(null, DocumentationSyncStatus.Stale, new[] { new DocumentationSyncGap("gap-a", "next", demoDocGap: true) }, backlogMismatch: true, reportStale: true, autoSyncRequested: true)
            });
            Assert.That(docs.Evaluate().Contains(DocumentationSyncDiagnosticCode.DocumentationSourceMissing), Is.True);
            Assert.That(docs.Evaluate().Contains(DocumentationSyncDiagnosticCode.AutoSyncRequested), Is.True);

            var risk = new ColonyIntegrationReleaseRiskProjection(new[]
            {
                new ReleaseRiskItem("risk-a", ReleaseRiskMilestone.Alpha, null, ReleaseRiskStatus.ReleaseDecisionForbidden, string.Empty, "limit", releaseDecisionClaimed: true, serverDependencyHidden: true, qaDependencyHidden: true)
            });
            Assert.That(risk.Evaluate().Contains(ReleaseRiskDiagnosticCode.ReleaseDecisionClaimed), Is.True);
            Assert.That(risk.Evaluate().Contains(ReleaseRiskDiagnosticCode.ServerDependencyHidden), Is.True);

            var gate = new ColonyIntegrationRuntimeReadinessGate(new[]
            {
                new RuntimeReadinessCriterion("BEE-271", true, runtimeBoundaryViolation: true, demoValidationMissing: true, fixtureGapOpen: true, releaseDecisionClaimed: true, documentationDriftOpen: true)
            }, bee281Referenced: true);
            Assert.That(gate.Evaluate().Verdict, Is.EqualTo(RuntimeReadinessVerdict.BlockedByBee281Premature));
            Assert.That(gate.Evaluate().Contains(RuntimeReadinessDiagnosticCode.RuntimeBoundaryViolation), Is.True);
        }
    }
}
