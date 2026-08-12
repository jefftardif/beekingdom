using System;
using System.Linq;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class ColonyIntegrationExecutionPreparationFrameworks281To290Tests
    {
        [Test]
        public void WorkerExecutionIntake_FlagsMissingInputsAndForbiddenExecution()
        {
            var intake = new ColonyWorkerExecutionIntake(new[]
            {
                new WorkerExecutionInput("input-a", string.Empty, string.Empty, string.Empty, "DEMO-012", string.Empty, "next", WorkerExecutionInputStatus.Blocked, forbiddenExecutionRequested: true)
            });

            WorkerExecutionDiagnostics diagnostics = intake.Evaluate();

            Assert.That(diagnostics.Contains(WorkerExecutionDiagnosticCode.WorkerInputMissing), Is.True);
            Assert.That(diagnostics.Contains(WorkerExecutionDiagnosticCode.BoundaryMissing), Is.True);
            Assert.That(diagnostics.Contains(WorkerExecutionDiagnosticCode.EvidenceMissing), Is.True);
            Assert.That(diagnostics.Contains(WorkerExecutionDiagnosticCode.OwnerMissing), Is.True);
            Assert.That(diagnostics.Contains(WorkerExecutionDiagnosticCode.ForbiddenExecutionRequested), Is.True);
        }

        [Test]
        public void SliceMap_DetectsCyclesAndForbiddenRuntimeRequests()
        {
            var map = new ColonyImplementationSliceMap(
                new[]
                {
                    new ColonyImplementationSlice(ColonyImplementationSliceKind.BoundaryAdapters, string.Empty, string.Empty, ColonyImplementationSliceStatus.Blocked, new[] { SliceDemoSurface.DEMO012 }, managerReplacementRequested: true),
                    new ColonyImplementationSlice(ColonyImplementationSliceKind.ReadModelBindings, "BEE-284", "Worker", ColonyImplementationSliceStatus.Ready, new[] { SliceDemoSurface.DEMO012 }, runtimeParallelRequested: true)
                },
                new[]
                {
                    new SliceDependency(ColonyImplementationSliceKind.BoundaryAdapters, ColonyImplementationSliceKind.ReadModelBindings),
                    new SliceDependency(ColonyImplementationSliceKind.ReadModelBindings, ColonyImplementationSliceKind.BoundaryAdapters)
                });

            SliceMapDiagnostics diagnostics = map.Evaluate();

            Assert.That(diagnostics.Contains(SliceMapDiagnosticCode.SliceSourceMissing), Is.True);
            Assert.That(diagnostics.Contains(SliceMapDiagnosticCode.SliceOwnerMissing), Is.True);
            Assert.That(diagnostics.Contains(SliceMapDiagnosticCode.SliceDependencyCycle), Is.True);
            Assert.That(diagnostics.Contains(SliceMapDiagnosticCode.ManagerReplacementRequested), Is.True);
            Assert.That(diagnostics.Contains(SliceMapDiagnosticCode.RuntimeParallelRequested), Is.True);
        }

        [Test]
        public void AdapterAndReadModelContracts_BlockMutationAndAmbiguity()
        {
            var adapter = new ColonyRuntimeAdapterContract(new[]
            {
                new ColonyRuntimeAdapterPortContract("adapter-a", "Population->Demo", AdapterSourceOfTruth.Ambiguous, ColonyRuntimeAdapterPort.ReadModelInput, "input", "output", "limit", ColonyRuntimeAdapterStatus.Blocked, managerReplacementRequested: true, mutableAdapterRequested: true, engineBypassRequested: true)
            });

            AdapterDiagnostics adapterDiagnostics = adapter.Evaluate();
            Assert.That(adapterDiagnostics.Contains(AdapterDiagnosticCode.SourceOfTruthAmbiguous), Is.True);
            Assert.That(adapterDiagnostics.Contains(AdapterDiagnosticCode.ManagerReplacementDetected), Is.True);
            Assert.That(adapterDiagnostics.Contains(AdapterDiagnosticCode.MutableAdapterRequested), Is.True);
            Assert.That(adapterDiagnostics.Contains(AdapterDiagnosticCode.EngineBypassRequested), Is.True);

            var binding = new ColonyReadModelBinding(new[]
            {
                new ReadModelBindingField("DemoGate", string.Empty, ReadModelBindingSource.Ambiguous, string.Empty, "transform", transformSeeded: false, sourceMutationRequested: true),
                new ReadModelBindingField("Population", "count", ReadModelBindingSource.Missing, "Worker", "identity")
            }, ReadModelBindingStatus.ForbiddenMutation);

            ReadModelBindingDiagnostics bindingDiagnostics = binding.Evaluate();
            Assert.That(bindingDiagnostics.Contains(ReadModelBindingDiagnosticCode.BindingSourceMissing), Is.True);
            Assert.That(bindingDiagnostics.Contains(ReadModelBindingDiagnosticCode.BindingAmbiguous), Is.True);
            Assert.That(bindingDiagnostics.Contains(ReadModelBindingDiagnosticCode.SourceMutationRequested), Is.True);
            Assert.That(bindingDiagnostics.Contains(ReadModelBindingDiagnosticCode.TransformUnseeded), Is.True);
            Assert.That(bindingDiagnostics.Contains(ReadModelBindingDiagnosticCode.ReadModelFieldUnowned), Is.True);
        }

        [Test]
        public void DemoWiringAndQaHooks_RejectScenesSpecsAndFinalVerdicts()
        {
            var wiring = new ColonyDemoWiringPlan(new[]
            {
                new DemoWiringStepEntry(DemoWiringStep.BindReadModel, null, string.Empty, string.Empty, "limit", DemoWiringStatus.Blocked, sceneCreationRequested: true, demoSpecSeparateRequested: true)
            });

            DemoWiringDiagnostics wiringDiagnostics = wiring.Evaluate();
            Assert.That(wiringDiagnostics.Contains(DemoWiringDiagnosticCode.DemoSurfaceMissing), Is.True);
            Assert.That(wiringDiagnostics.Contains(DemoWiringDiagnosticCode.WiringSourceMissing), Is.True);
            Assert.That(wiringDiagnostics.Contains(DemoWiringDiagnosticCode.SceneCreationRequested), Is.True);
            Assert.That(wiringDiagnostics.Contains(DemoWiringDiagnosticCode.DemoSpecSeparateRequested), Is.True);
            Assert.That(wiringDiagnostics.Contains(DemoWiringDiagnosticCode.VisualSuccessMissing), Is.True);

            var hook = new ColonyIntegrationQaObservationHook(
                "hook-a",
                "Observation",
                "BEE-286",
                new ColonyQaObservationSource(string.Empty, "slice", ColonyQaObservationSourceType.DemoPanel, string.Empty, "signal"),
                null,
                ColonyQaObservationStatus.InvalidClaim,
                DateTime.UtcNow,
                DateTime.UtcNow,
                new ColonyQaObservationExport("hook-a", "label", ColonyQaObservationStatus.InvalidClaim, "qa", "demo", "limit", unsafeExport: true),
                demoVerdictClaimed: true,
                finalQaTestClaimed: true);

            var hookDiagnostics = ColonyQaObservationDiagnostics.Evaluate(new[] { hook });
            Assert.That(hookDiagnostics.Contains(ColonyQaObservationDiagnosticCode.QaHookSourceMissing), Is.True);
            Assert.That(hookDiagnostics.Contains(ColonyQaObservationDiagnosticCode.ObservationPayloadMissing), Is.True);
            Assert.That(hookDiagnostics.Contains(ColonyQaObservationDiagnosticCode.DemoVerdictClaimed), Is.True);
            Assert.That(hookDiagnostics.Contains(ColonyQaObservationDiagnosticCode.FinalQaTestClaimed), Is.True);
            Assert.That(hookDiagnostics.Contains(ColonyQaObservationDiagnosticCode.UnsafeQaObservationExport), Is.True);
            Assert.That(hookDiagnostics.Contains(ColonyQaObservationDiagnosticCode.ObservationOwnerMissing), Is.True);
        }

        [Test]
        public void ServerEscalationScenarioPackAndRiskBurnDown_BlockScopeDrift()
        {
            var escalation = new ColonyServerDependencyEscalation("esc-a", "BEE-287", "source", null, null, null, 3, ColonyServerEscalationStatus.BlockedImplementation, serverDependencyHidden: true, serverServiceRequested: true, endpointRequestedWithoutServerBee: true, serverScopeAmbiguous: true, serverProgressMismatch: true);
            var queue = new ColonyServerEscalationQueue("queue-a", new[] { escalation }, Array.Empty<ColonyServerDependencyEscalation>(), Array.Empty<ColonyServerDependencyEscalation>());
            ServerDependencyEscalationDiagnostics escalationDiagnostics = queue.Evaluate();
            Assert.That(escalationDiagnostics.Contains(ServerDependencyEscalationDiagnosticCode.ServerDependencyHidden), Is.True);
            Assert.That(escalationDiagnostics.Contains(ServerDependencyEscalationDiagnosticCode.ServerServiceRequested), Is.True);
            Assert.That(escalationDiagnostics.Contains(ServerDependencyEscalationDiagnosticCode.EndpointRequestedWithoutServerBee), Is.True);
            Assert.That(escalationDiagnostics.Contains(ServerDependencyEscalationDiagnosticCode.OwnerMissing), Is.True);
            Assert.That(escalationDiagnostics.Contains(ServerDependencyEscalationDiagnosticCode.EscalationConditionMissing), Is.True);

            var pack = new ColonyIntegrationRegressionScenarioPack("pack-a", "Regression", new[]
            {
                new ColonyRegressionScenario("scenario-a", "Scenario", ColonyRegressionScenarioCategory.ExecutionOrderRisk, null, null, Array.Empty<string>(), null, Array.Empty<string>(), null, ColonyRegressionScenarioStatus.Blocked, finalSuiteExecutionClaimed: true, performanceClaimFinal: true)
            }, "BEE-281..290", string.Empty);
            RegressionScenarioPackDiagnostics packDiagnostics = pack.Evaluate();
            Assert.That(packDiagnostics.Contains(RegressionScenarioPackDiagnosticCode.ScenarioSeedMissing), Is.True);
            Assert.That(packDiagnostics.Contains(RegressionScenarioPackDiagnosticCode.FixtureReferenceMissing), Is.True);
            Assert.That(packDiagnostics.Contains(RegressionScenarioPackDiagnosticCode.ExpectedObservationMissing), Is.True);
            Assert.That(packDiagnostics.Contains(RegressionScenarioPackDiagnosticCode.FinalSuiteExecutionClaimed), Is.True);
            Assert.That(packDiagnostics.Contains(RegressionScenarioPackDiagnosticCode.PerformanceClaimFinal), Is.True);
            Assert.That(packDiagnostics.Contains(RegressionScenarioPackDiagnosticCode.ScenarioBeeSourceMissing), Is.True);
            Assert.That(packDiagnostics.Contains(RegressionScenarioPackDiagnosticCode.RegressionLimitMissing), Is.True);

            var burnDown = new ColonyImplementationRiskBurnDown("risk-pack", "BEE-281..290", new[]
            {
                new ColonyImplementationRiskItem("risk-a", "Risk", string.Empty, string.Empty, ColonyImplementationRiskSeverity.Critical, string.Empty, ColonyImplementationRiskStatus.AcceptedByArchitect, null, "next", new ColonyRiskDecision("decision-a", "risk-a", ColonyRiskDecisionType.ArchitectAcceptance, "Worker", "reason"), serverRiskUnescalated: true, qaRiskMisclassified: true, bee291PrematureRiskClosure: true)
            });
            ImplementationRiskBurnDownDiagnostics riskDiagnostics = burnDown.Evaluate();
            Assert.That(riskDiagnostics.Contains(ImplementationRiskBurnDownDiagnosticCode.RiskSourceMissing), Is.True);
            Assert.That(riskDiagnostics.Contains(ImplementationRiskBurnDownDiagnosticCode.RiskOwnerMissing), Is.True);
            Assert.That(riskDiagnostics.Contains(ImplementationRiskBurnDownDiagnosticCode.BurnDownClaimWithoutEvidence), Is.True);
            Assert.That(riskDiagnostics.Contains(ImplementationRiskBurnDownDiagnosticCode.AcceptedRiskWithoutAuthority), Is.True);
            Assert.That(riskDiagnostics.Contains(ImplementationRiskBurnDownDiagnosticCode.ServerRiskUnescalated), Is.True);
            Assert.That(riskDiagnostics.Contains(ImplementationRiskBurnDownDiagnosticCode.QaRiskMisclassified), Is.True);
            Assert.That(riskDiagnostics.Contains(ImplementationRiskBurnDownDiagnosticCode.Bee291PrematureRiskClosure), Is.True);
        }

        [Test]
        public void ExecutionReadinessGate_BlocksBee291AndMissingInputs()
        {
            var gate = new ColonyExecutionReadinessGate(
                "gate-a",
                "BEE-281..290",
                new ColonyExecutionReadinessInputSet("intake", string.Empty, "adapter", "read-model", "demo", "qa", "server", "regression", "risk"),
                new[] { new ColonyExecutionReadinessBlocker("blocker-a", "BEE-290", "reason", "Architect", "Keep BEE-291 blocked") },
                new[] { "warning" },
                "handoff",
                workerIntakeIncomplete: true,
                sliceMapBlocked: true,
                runtimeAdapterGapOpen: true,
                readModelBindingMissing: true,
                demoWiringMissing: true,
                qaObservationBlocked: true,
                serverEscalationUnresolved: true,
                regressionPackIncomplete: true,
                implementationRiskOpen: true,
                bee291PrematureAttempt: true);

            ColonyExecutionReadinessDiagnostics diagnostics = gate.Evaluate();

            Assert.That(diagnostics.Verdict.VerdictType, Is.EqualTo(ColonyExecutionReadinessVerdictType.BlockedByBee291Premature));
            Assert.That(diagnostics.Verdict.Bee291Status, Is.EqualTo(ColonyExecutionReadinessGate.Bee291BlockedStatus));
            Assert.That(diagnostics.Contains(ColonyExecutionReadinessDiagnosticCode.ReadinessInputMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyExecutionReadinessDiagnosticCode.ServerEscalationUnresolved), Is.True);
            Assert.That(diagnostics.Contains(ColonyExecutionReadinessDiagnosticCode.Bee291Premature), Is.True);
        }
    }
}
