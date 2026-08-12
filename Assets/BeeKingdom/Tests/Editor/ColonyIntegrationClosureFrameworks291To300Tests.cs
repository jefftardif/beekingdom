using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class ColonyIntegrationClosureFrameworks291To300Tests
    {
        [Test]
        public void LaunchChecklist_BlocksMissingEvidenceOwnersAndBee301()
        {
            var checklist = new ColonyImplementationLaunchChecklist("launch-a", "BEE-291..300", string.Empty, new[]
            {
                new ColonyLaunchChecklistItem("item-a", "BEE-291", string.Empty, string.Empty, string.Empty, ColonyLaunchPrerequisiteStatus.MissingOwner, ColonyLaunchBlockingReason.OwnerMissing, "next", prematureSliceExecutionRequested: true, multiplayerArmyHandoffMissing: true)
            }, bee301LaunchAttempted: true);

            ImplementationLaunchChecklistDiagnostics diagnostics = checklist.Evaluate();

            Assert.That(diagnostics.Contains(ImplementationLaunchChecklistDiagnosticCode.LaunchChecklistItemMissing), Is.True);
            Assert.That(diagnostics.Contains(ImplementationLaunchChecklistDiagnosticCode.LaunchOwnerMissing), Is.True);
            Assert.That(diagnostics.Contains(ImplementationLaunchChecklistDiagnosticCode.LaunchEvidenceMissing), Is.True);
            Assert.That(diagnostics.Contains(ImplementationLaunchChecklistDiagnosticCode.PrematureSliceExecutionRequested), Is.True);
            Assert.That(diagnostics.Contains(ImplementationLaunchChecklistDiagnosticCode.Bee301LaunchAttemptBlocked), Is.True);
            Assert.That(diagnostics.Contains(ImplementationLaunchChecklistDiagnosticCode.MultiplayerArmyHandoffMissing), Is.True);
        }

        [Test]
        public void SliceVerificationAndAdapterSmoke_BlockMutationHiddenSignalsAndServerAuthority()
        {
            var verification = new ColonySliceExecutionVerification("verify-a", "slice-map", new[]
            {
                new ColonySliceVerificationCase("case-a", "slice", "Worker", Array.Empty<string>(), "criterion", forbiddenMutation: true, multiplayerArmySignalDeclared: false)
            }, new[]
            {
                new ColonySliceVerificationResult("case-a", ColonySliceVerificationResultKind.InvalidOwnership, "evidence", Array.Empty<string>(), "next", ownerChanged: true, dependencyInvalid: true, limitHidden: true, replanRequired: true)
            }, "export");

            SliceExecutionVerificationDiagnostics verificationDiagnostics = verification.Evaluate();
            Assert.That(verificationDiagnostics.Contains(SliceExecutionVerificationDiagnosticCode.SliceOwnerChanged), Is.True);
            Assert.That(verificationDiagnostics.Contains(SliceExecutionVerificationDiagnosticCode.SliceDependencyInvalid), Is.True);
            Assert.That(verificationDiagnostics.Contains(SliceExecutionVerificationDiagnosticCode.SliceLimitHidden), Is.True);
            Assert.That(verificationDiagnostics.Contains(SliceExecutionVerificationDiagnosticCode.SliceReplanRequired), Is.True);
            Assert.That(verificationDiagnostics.Contains(SliceExecutionVerificationDiagnosticCode.RuntimeMutationRequested), Is.True);
            Assert.That(verificationDiagnostics.Contains(SliceExecutionVerificationDiagnosticCode.MultiplayerArmySignalHidden), Is.True);

            var smoke = new ColonyAdapterSmokeValidation("smoke-a", "registry", new[]
            {
                new ColonyAdapterSmokeProbe("probe-a", "adapter", null, null, "signal", mutationAllowed: true)
            }, new ColonyAdapterSmokeReport(new[]
            {
                new ColonyAdapterSmokeSignal("probe-a", ColonyAdapterSmokeStatus.ServerAuthorityRequired, "observed", new ColonyAdapterSmokeGap("gap-a", string.Empty, "limit"), "limit")
            }));

            AdapterSmokeValidationDiagnostics smokeDiagnostics = smoke.Evaluate();
            Assert.That(smokeDiagnostics.Contains(AdapterSmokeValidationDiagnosticCode.AdapterSmokeSourceMissing), Is.True);
            Assert.That(smokeDiagnostics.Contains(AdapterSmokeValidationDiagnosticCode.AdapterSmokePortMissing), Is.True);
            Assert.That(smokeDiagnostics.Contains(AdapterSmokeValidationDiagnosticCode.AdapterSmokeMutationRequested), Is.True);
            Assert.That(smokeDiagnostics.Contains(AdapterSmokeValidationDiagnosticCode.AdapterSmokeGapUnclassified), Is.True);
            Assert.That(smokeDiagnostics.Contains(AdapterSmokeValidationDiagnosticCode.ServerAuthorityRequired), Is.True);
        }

        [Test]
        public void DemoConsistencyQaExportAndServerReview_BlockFinalOrAuthoritativeClaims()
        {
            var consistency = new ColonyReadModelDemoConsistency("check-a", new[]
            {
                new ColonyDemoReadModelSurface("surface-a", "DEMO-012", "read-model", Array.Empty<string>(), ColonyReadModelFreshnessStatus.InvalidMutableProjection, registered: false, mutableProjection: true, serverAuthorityLimitMissing: true),
                new ColonyDemoReadModelSurface("surface-b", "DEMO-011", "read-model", Array.Empty<string>(), ColonyReadModelFreshnessStatus.Stale)
            }, Array.Empty<ReadModelBindingField>(), new[]
            {
                new ColonyDemoConsistencyMismatch("mismatch-a", "field", "server", "a", "b", 2)
            }, ColonyReadModelFreshnessStatus.Mismatch);

            ReadModelDemoConsistencyDiagnostics consistencyDiagnostics = consistency.Evaluate();
            Assert.That(consistencyDiagnostics.Contains(ReadModelDemoConsistencyDiagnosticCode.DemoReadModelMismatch), Is.True);
            Assert.That(consistencyDiagnostics.Contains(ReadModelDemoConsistencyDiagnosticCode.DemoReadModelStale), Is.True);
            Assert.That(consistencyDiagnostics.Contains(ReadModelDemoConsistencyDiagnosticCode.DemoBindingMissing), Is.True);
            Assert.That(consistencyDiagnostics.Contains(ReadModelDemoConsistencyDiagnosticCode.DemoProjectionMutable), Is.True);
            Assert.That(consistencyDiagnostics.Contains(ReadModelDemoConsistencyDiagnosticCode.DemoSurfaceUnregistered), Is.True);
            Assert.That(consistencyDiagnostics.Contains(ReadModelDemoConsistencyDiagnosticCode.ServerAuthorityLimitMissing), Is.True);

            var evidence = new ColonyQaEvidenceExportPackage("qa-a", "BEE-291..300", new[]
            {
                new ColonyQaEvidenceRecord("evidence-a", string.Empty, ColonyQaEvidenceSourceType.MultiplayerArmyHandoffEvidence, "ref", ColonyQaEvidenceExportStatus.RejectedAsFinalVerdict, "QA", "final", null)
            }, new[]
            {
                new ColonyQaEvidenceRecord("evidence-b", "BEE-295", ColonyQaEvidenceSourceType.Risk, "ref", ColonyQaEvidenceExportStatus.Blocked, "QA", "non-final", new ColonyQaEvidenceLimitation("limit", "limit"))
            }, Array.Empty<ColonyQaEvidenceLimitation>(), finalVerdictAllowed: true, promptQaCreationRequested: true);

            ColonyQaEvidenceExportDiagnostics evidenceDiagnostics = evidence.Evaluate();
            Assert.That(evidenceDiagnostics.Contains(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceSourceMissing), Is.True);
            Assert.That(evidenceDiagnostics.Contains(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceLimitMissing), Is.True);
            Assert.That(evidenceDiagnostics.Contains(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceFinalVerdictClaimed), Is.True);
            Assert.That(evidenceDiagnostics.Contains(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceExportBlocked), Is.True);
            Assert.That(evidenceDiagnostics.Contains(ColonyQaEvidenceExportDiagnosticCode.PromptQaCreationRequested), Is.True);

            var review = new ColonyServerEscalationReviewGate("server-a", null, new[]
            {
                new ColonyServerEscalationReviewItem("esc-a", "BEE-296", "pvp", string.Empty, ColonyServerEscalationVerdict.InvalidServerImplementationRequest, "Server", new ColonyServerFutureSpecCandidate("candidate-a", "SERVER-018", premature: true), endpointCreationRequested: true, sqlCreationRequested: true)
            }, "summary", "next", serverProgressOutOfDate: true);

            ServerEscalationReviewDiagnostics reviewDiagnostics = review.Evaluate();
            Assert.That(reviewDiagnostics.Contains(ServerEscalationReviewDiagnosticCode.ServerEscalationReviewMissing), Is.True);
            Assert.That(reviewDiagnostics.Contains(ServerEscalationReviewDiagnosticCode.ServerVerdictWithoutEvidence), Is.True);
            Assert.That(reviewDiagnostics.Contains(ServerEscalationReviewDiagnosticCode.ServerSpecPremature), Is.True);
            Assert.That(reviewDiagnostics.Contains(ServerEscalationReviewDiagnosticCode.EndpointCreationRequested), Is.True);
            Assert.That(reviewDiagnostics.Contains(ServerEscalationReviewDiagnosticCode.SqlCreationRequested), Is.True);
            Assert.That(reviewDiagnostics.Contains(ServerEscalationReviewDiagnosticCode.ServerProgressOutOfDate), Is.True);
        }

        [Test]
        public void RunbookSnapshotProjectionAndClosure_BlockFinalClaimsAndMissingArch026()
        {
            var runbook = new ColonyIntegrationRegressionRunbook("runbook-a", "pack", new[]
            {
                new ColonyRegressionRunStep("step-a", "scenario", 0, new ColonyRegressionRunSeed(null, "source"), string.Empty, "DEMO-012", new ColonyRegressionExpectedProvisionalResult("signal", "limit"), ColonyRegressionRunbookStatus.FinalSuiteClaimBlocked, fixtureMissing: true),
                new ColonyRegressionRunStep("step-b", "scenario-b", 0, new ColonyRegressionRunSeed(2, "source"), "Worker", "DEMO-011", new ColonyRegressionExpectedProvisionalResult("signal", "limit"), ColonyRegressionRunbookStatus.Blocked)
            }, "limit", finalSuiteAllowed: true);

            RegressionRunbookDiagnostics runbookDiagnostics = runbook.Evaluate();
            Assert.That(runbookDiagnostics.Contains(RegressionRunbookDiagnosticCode.RegressionSeedMissing), Is.True);
            Assert.That(runbookDiagnostics.Contains(RegressionRunbookDiagnosticCode.RegressionFixtureMissing), Is.True);
            Assert.That(runbookDiagnostics.Contains(RegressionRunbookDiagnosticCode.RegressionOwnerMissing), Is.True);
            Assert.That(runbookDiagnostics.Contains(RegressionRunbookDiagnosticCode.RegressionFinalSuiteClaimed), Is.True);
            Assert.That(runbookDiagnostics.Contains(RegressionRunbookDiagnosticCode.RegressionOrderAmbiguous), Is.True);

            var snapshot = new ColonyDemoAcceptanceSnapshot("snapshot-a", new[]
            {
                new ColonyDemoAcceptanceSurface("surface-a", "DEMO-012", ColonyDemoAcceptanceStatus.SurfaceMissing)
            }, new[]
            {
                new ColonyDemoAcceptanceCaptureRequirement("capture-a", "DEMO-012", string.Empty, "BEE-298", "state", new ColonyDemoAcceptanceBlocker("blocker-a", "Demo", "hidden", hidden: true), string.Empty)
            }, new[]
            {
                new ColonyDemoAcceptanceBlocker("blocker-b", "Demo", "hidden", hidden: true)
            }, Array.Empty<string>(), ColonyDemoAcceptanceStatus.QaFinalClaimBlocked, qaFinalClaimed: true);

            ColonyDemoAcceptanceDiagnostics snapshotDiagnostics = snapshot.Evaluate();
            Assert.That(snapshotDiagnostics.Contains(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceSurfaceMissing), Is.True);
            Assert.That(snapshotDiagnostics.Contains(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceCaptureMissing), Is.True);
            Assert.That(snapshotDiagnostics.Contains(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceBlockerHidden), Is.True);
            Assert.That(snapshotDiagnostics.Contains(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceQaClaimed), Is.True);
            Assert.That(snapshotDiagnostics.Contains(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceLimitMissing), Is.True);

            var projection = new ColonyAlphaReadinessProjection("alpha-a", "BEE-251..298", Array.Empty<ColonyAlphaCondition>(), new[]
            {
                new ColonyAlphaOpenRisk("risk-a", "Architect", ColonyImplementationRiskSeverity.High, payToWinRiskUnclassified: true)
            }, new[]
            {
                new ColonyAlphaMissingCondition("condition-a", string.Empty, "missing")
            }, new[]
            {
                new ColonyMultiplayerArmyTransitionCondition("transition-a", "BEE-301..310", Array.Empty<string>(), false, new ColonyPlayerInvestmentProjection(true, false, true, runtimeMonetizationClaimed: true), string.Empty, null)
            }, ColonyAlphaProjectionStatus.NotAlphaReady, "limit", alphaReadyClaimed: true, serverDependencyOpen: true, qaEvidenceIncomplete: true, demoSnapshotIncomplete: true);

            ColonyAlphaReadinessDiagnostics projectionDiagnostics = projection.Evaluate();
            Assert.That(projectionDiagnostics.Contains(ColonyAlphaProjectionDiagnosticCode.AlphaConditionMissing), Is.True);
            Assert.That(projectionDiagnostics.Contains(ColonyAlphaProjectionDiagnosticCode.AlphaOwnerMissing), Is.True);
            Assert.That(projectionDiagnostics.Contains(ColonyAlphaProjectionDiagnosticCode.AlphaReadyClaimBlocked), Is.True);
            Assert.That(projectionDiagnostics.Contains(ColonyAlphaProjectionDiagnosticCode.MultiplayerArmyConditionMissing), Is.True);
            Assert.That(projectionDiagnostics.Contains(ColonyAlphaProjectionDiagnosticCode.PlayerInvestmentModelMissing), Is.True);
            Assert.That(projectionDiagnostics.Contains(ColonyAlphaProjectionDiagnosticCode.ServerAuthorityForConflictMissing), Is.True);
            Assert.That(projectionDiagnostics.Contains(ColonyAlphaProjectionDiagnosticCode.PayToWinRiskUnclassified), Is.True);

            var closure = new ColonyIntegrationClosureGate("closure-a", "BEE-251..300", new ColonyIntegrationClosureInputSet("launch", string.Empty, "smoke", "consistency", "qa", "server", "runbook", "snapshot", "alpha"), new[]
            {
                new ColonyIntegrationClosureGap("gap-a", "BEE-300", string.Empty, ColonyClosureGapSeverity.Critical, "next")
            }, string.Empty, null, alphaReadyClaimed: true, qaFinalClaimed: true, serverReadyClaimed: true, bee301PrematureAttempt: true);

            ColonyIntegrationClosureDiagnostics closureDiagnostics = closure.Evaluate();
            Assert.That(closureDiagnostics.Verdict.VerdictType, Is.EqualTo(ColonyIntegrationClosureVerdictType.BlockedByBee301Premature));
            Assert.That(closureDiagnostics.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureInputMissing), Is.True);
            Assert.That(closureDiagnostics.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureEvidenceMissing), Is.True);
            Assert.That(closureDiagnostics.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureGapOwnerMissing), Is.True);
            Assert.That(closureDiagnostics.Contains(ColonyIntegrationClosureDiagnosticCode.Arch026HandoffMissing), Is.True);
            Assert.That(closureDiagnostics.Contains(ColonyIntegrationClosureDiagnosticCode.ServerAuthorityForPvpMissing), Is.True);
            Assert.That(closureDiagnostics.Contains(ColonyIntegrationClosureDiagnosticCode.Bee301Premature), Is.True);
        }
    }
}
