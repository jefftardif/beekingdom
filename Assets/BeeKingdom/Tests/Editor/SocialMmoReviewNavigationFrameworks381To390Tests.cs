using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class SocialMmoReviewNavigationFrameworks381To390Tests
    {
        [Test]
        public void NavigationDemoDiffAndPrivacy_BlockRuntimeSensitiveAndHiddenLimits()
        {
            var navigation = new SocialMmoReviewNavigationMap("nav", new[]
            {
                new ReviewNavigationNode(string.Empty, ReviewNavigationNodeType.Risk, string.Empty, "risk", string.Empty, Array.Empty<string>(), new[] { "server" }, new[] { new ReviewNavigationEdge("edge", "runtime", runtimeRoute: true, localTruthClaimed: true, blockedVisible: true) })
            }, new ReviewNavigationContext("ctx"), new ReviewNavigationOwnerTrail(Array.Empty<string>()), new[] { new ReviewNavigationBlockedRoute("runtime", runtimeRoute: true) });
            ReviewNavigationDiagnostics navigationDiagnostics = navigation.Evaluate();
            Assert.That(navigationDiagnostics.Contains(ReviewNavigationDiagnosticCode.ReviewNavigationNodeMissing), Is.True);
            Assert.That(navigationDiagnostics.Contains(ReviewNavigationDiagnosticCode.ReviewNavigationOwnerTrailMissing), Is.True);
            Assert.That(navigationDiagnostics.Contains(ReviewNavigationDiagnosticCode.ReviewNavigationRuntimeRouteForbidden), Is.True);
            Assert.That(navigationDiagnostics.Contains(ReviewNavigationDiagnosticCode.ReviewNavigationLocalTruthRisk), Is.True);
            Assert.That(navigationDiagnostics.Contains(ReviewNavigationDiagnosticCode.ReviewNavigationBlockedRouteVisible), Is.True);

            var binding = new SocialMmoDemoBindingContract("binding", new[]
            {
                new DemoReviewBindingField("field", "node", null, DemoReviewBindingVisibilityState.BlockedByPrivacy, new DemoReviewBindingRedactionRule(required: true, applied: false), string.Empty, "server", new[] { new DemoReviewBindingLimit("runtime", hidden: true, runtimeClaim: true, separateSpecRequested: true) })
            });
            DemoBindingDiagnostics bindingDiagnostics = binding.Evaluate();
            Assert.That(bindingDiagnostics.Contains(DemoBindingDiagnosticCode.DemoBindingSurfaceMissing), Is.True);
            Assert.That(bindingDiagnostics.Contains(DemoBindingDiagnosticCode.DemoBindingRuntimeClaimDetected), Is.True);
            Assert.That(bindingDiagnostics.Contains(DemoBindingDiagnosticCode.DemoBindingRedactionMissing), Is.True);
            Assert.That(bindingDiagnostics.Contains(DemoBindingDiagnosticCode.DemoBindingSeparateSpecForbidden), Is.True);
            Assert.That(bindingDiagnostics.Contains(DemoBindingDiagnosticCode.DemoBindingLimitHidden), Is.True);

            var diff = new GovernanceExportDiffReview("diff", new GovernanceExportDiffInput("old", "new", compatible: false), new[]
            {
                new GovernanceExportDiffItem(GovernanceExportDiffType.EvidenceBecameStale, "old", "new", "decision", SocialMmoProductPillar.PvP, 5, "Obsolete", "server", officialVerdictClaimed: true, sensitiveDataPresent: true)
            });
            GovernanceExportDiffDiagnostics diffDiagnostics = diff.Evaluate();
            Assert.That(diffDiagnostics.Verdict, Is.EqualTo(GovernanceExportDiffVerdict.BlockedByOfficialVerdict));
            Assert.That(diffDiagnostics.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffIncompatibleExport), Is.True);
            Assert.That(diffDiagnostics.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffOfficialVerdictForbidden), Is.True);
            Assert.That(diffDiagnostics.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffStaleEvidenceDetected), Is.True);
            Assert.That(diffDiagnostics.Contains(GovernanceDiffDiagnosticCode.GovernanceDiffSensitiveDataBlocked), Is.True);

            var viewer = new PrivacySafeEvidenceViewer("viewer", EvidenceViewerReadMode.RedactedDetail, new[]
            {
                new EvidenceViewerRedactedField("field", "Victim", "masked", new EvidenceViewerSensitivityBadge(SensitiveEvidenceClass.VictimProtected, victimExposureRisk: true), new EvidenceViewerExportGuard(exportAllowed: false, exportRequested: true), "blocked", string.Empty, rawDataVisible: true)
            }, new EvidenceViewerPunitiveUseBlocker(punitiveUseRequested: true), Array.Empty<EvidenceViewerAuditNote>());
            EvidenceViewerDiagnostics viewerDiagnostics = viewer.Evaluate();
            Assert.That(viewerDiagnostics.Contains(EvidenceViewerDiagnosticCode.EvidenceViewerRawDataForbidden), Is.True);
            Assert.That(viewerDiagnostics.Contains(EvidenceViewerDiagnosticCode.EvidenceViewerExportRefused), Is.True);
            Assert.That(viewerDiagnostics.Contains(EvidenceViewerDiagnosticCode.EvidenceViewerVictimExposureRisk), Is.True);
            Assert.That(viewerDiagnostics.Contains(EvidenceViewerDiagnosticCode.EvidenceViewerPunitiveUseForbidden), Is.True);
            Assert.That(viewerDiagnostics.Contains(EvidenceViewerDiagnosticCode.EvidenceViewerFalsePositiveContextMissing), Is.True);
        }

        [Test]
        public void ScenarioArmyTimelineModerationAndServerAlignment_BlockOperationalClaims()
        {
            var scenario = new AlliancePvpScenarioPreviewLens("scenario", new[]
            {
                new ScenarioPreviewNarrativeStep("step", "raid", "preview", Array.Empty<SocialMmoProductPillar>(), Array.Empty<ScenarioPreviewRiskMarker>(), Array.Empty<ScenarioPreviewQaQuestion>(), Array.Empty<ScenarioPreviewServerDependency>(), new[] { new ScenarioPreviewExecutionBlocker("exec", combatExecution: true, matchmaking: true, reward: true, loss: true) })
            });
            ScenarioPreviewDiagnostics scenarioDiagnostics = scenario.Evaluate();
            Assert.That(scenarioDiagnostics.Contains(ScenarioPreviewDiagnosticCode.ScenarioPreviewCombatExecutionForbidden), Is.True);
            Assert.That(scenarioDiagnostics.Contains(ScenarioPreviewDiagnosticCode.ScenarioPreviewMatchmakingForbidden), Is.True);
            Assert.That(scenarioDiagnostics.Contains(ScenarioPreviewDiagnosticCode.ScenarioPreviewRewardForbidden), Is.True);
            Assert.That(scenarioDiagnostics.Contains(ScenarioPreviewDiagnosticCode.ScenarioPreviewLossForbidden), Is.True);
            Assert.That(scenarioDiagnostics.Contains(ScenarioPreviewDiagnosticCode.ScenarioPreviewServerDependencyMissing), Is.True);

            var army = new ArmyCompetitionDrilldownLens("army", new[]
            {
                new ArmyReadinessDrilldownItem(string.Empty, null, Array.Empty<string>(), new[] { "missing" }, "fairness", new PayToWinRiskDrilldown(open: true), new ArmyServerAuthorityMarker("server", required: true), new[] { new CompetitionActivationBlocker("combat", combatActivation: true, officialScore: true) })
            });
            ArmyDrilldownDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyDrilldownDiagnosticCode.ArmyDrilldownSignalMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDrilldownDiagnosticCode.ArmyDrilldownOfficialScoreForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDrilldownDiagnosticCode.ArmyDrilldownPayToWinRiskOpen), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDrilldownDiagnosticCode.ArmyDrilldownCombatActivationForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDrilldownDiagnosticCode.ArmyDrilldownServerAuthorityRequired), Is.True);

            var timeline = new LiveOpsCandidateTimelineMock("timeline", new[]
            {
                new LiveOpsTimelineMockSlot("slot", "candidate", 1, Array.Empty<LiveOpsTimelineMockMarker>(), "value", new[] { new LiveOpsTimelineActivationBlocker("active", registration: true, reward: true, notification: true, monetization: true) }, Array.Empty<LiveOpsTimelineReviewDependency>(), new LiveOpsTimelineNonCalendarMarker(publishedDateRequested: true), new LiveOpsTimelinePlayerPromiseGuard(activePromiseClaimed: true))
            });
            LiveOpsTimelineDiagnostics timelineDiagnostics = timeline.Evaluate();
            Assert.That(timelineDiagnostics.Contains(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineCalendarForbidden), Is.True);
            Assert.That(timelineDiagnostics.Contains(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineRegistrationForbidden), Is.True);
            Assert.That(timelineDiagnostics.Contains(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineRewardForbidden), Is.True);
            Assert.That(timelineDiagnostics.Contains(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineNotificationForbidden), Is.True);
            Assert.That(timelineDiagnostics.Contains(LiveOpsTimelineDiagnosticCode.LiveOpsTimelineMonetizationForbidden), Is.True);

            var moderation = new ModerationReviewCaseWalkthrough("moderation", new[]
            {
                new ModerationWalkthroughStep("step", ModerationWalkthroughStepKind.SanctionForbidden, new ModerationWalkthroughRedactionCheck(redacted: false, rawEvidenceVisible: true), new ModerationWalkthroughFalsePositiveCheck(present: false), new ModerationWalkthroughOwnerAction(string.Empty), "context", new ModerationWalkthroughServerHandoffMarker(required: true), forbiddenOutcome: true)
            });
            ModerationWalkthroughDiagnostics moderationDiagnostics = moderation.Evaluate();
            Assert.That(moderationDiagnostics.Contains(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughRawEvidenceForbidden), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughFalsePositiveMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughOwnerMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughSanctionForbidden), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationWalkthroughDiagnosticCode.ModerationWalkthroughServerHandoffRequired), Is.True);

            var alignment = new SocialMmoServerEscalationReviewAlignment("alignment", new[]
            {
                new ServerEscalationReviewTopic(string.Empty, Array.Empty<string>(), new ServerEscalationAuthorityReason(string.Empty), new[] { new ServerEscalationLocalBlocker("runtime", localRuntimeClaim: true) }, Array.Empty<ServerEscalationReadinessQuestion>(), new[] { "runtime" }, new ServerEscalationNonPriorityFlag(backendPriorityClaimed: true, serverSpecCreationRequested: true))
            });
            ServerEscalationAlignmentDiagnostics alignmentDiagnostics = alignment.Evaluate();
            Assert.That(alignmentDiagnostics.Contains(ServerEscalationDiagnosticCode.ServerEscalationTopicMissing), Is.True);
            Assert.That(alignmentDiagnostics.Contains(ServerEscalationDiagnosticCode.ServerEscalationAuthorityReasonMissing), Is.True);
            Assert.That(alignmentDiagnostics.Contains(ServerEscalationDiagnosticCode.ServerEscalationSpecCreationForbidden), Is.True);
            Assert.That(alignmentDiagnostics.Contains(ServerEscalationDiagnosticCode.ServerEscalationBackendPriorityForbidden), Is.True);
            Assert.That(alignmentDiagnostics.Contains(ServerEscalationDiagnosticCode.ServerEscalationLocalRuntimeClaimDetected), Is.True);
        }

        [Test]
        public void ReviewNavigationClosureGate_BlocksBee391PrematureAndRuntimeClaims()
        {
            var gate = new SocialMmoReviewNavigationClosureGate("gate", null, new ReviewNavigationClosureCoverage(demoBindingGap: true, privacyRiskOpen: true, runtimeClaim: true), new[] { new ReviewNavigationClosureBlocker("server", serverAlignmentGap: true) }, new Bee391BlockerStatus(prematureAttempt: true, SocialMmoReviewNavigationClosureGate.Bee391BlockedMessage));
            SocialMmoReviewNavigationClosureDiagnostics diagnostics = gate.Evaluate();

            Assert.That(diagnostics.Verdict, Is.EqualTo(ReviewNavigationClosureVerdict.BlockedByBee391Premature));
            Assert.That(diagnostics.Contains(NavigationClosureDiagnosticCode.NavigationClosureInputMissing), Is.True);
            Assert.That(diagnostics.Contains(NavigationClosureDiagnosticCode.NavigationClosureDemoBindingGap), Is.True);
            Assert.That(diagnostics.Contains(NavigationClosureDiagnosticCode.NavigationClosurePrivacyRiskOpen), Is.True);
            Assert.That(diagnostics.Contains(NavigationClosureDiagnosticCode.NavigationClosureServerAlignmentGap), Is.True);
            Assert.That(diagnostics.Contains(NavigationClosureDiagnosticCode.NavigationClosureRuntimeClaimDetected), Is.True);
            Assert.That(diagnostics.Contains(NavigationClosureDiagnosticCode.Bee391Premature), Is.True);
        }
    }
}
