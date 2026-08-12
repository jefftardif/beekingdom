using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class PlayerOnboardingDemoEvidenceFrameworks422To430Tests
    {
        [Test]
        public void ContinuityOnboardingProfileStyleAndGoals_BlockFalsePlayerReadiness()
        {
            var continuity = new DemoVisualContinuityGuard("guard", new[]
            {
                new DemoSurfaceVisibilityCheck(
                    "home",
                    new DemoRenderableAnchor(string.Empty, visible: false),
                    DemoSurfaceVisibleState.BlueOnly,
                    new DemoFallbackVisualState(string.Empty, visible: false),
                    new DemoReadOnlyLimitNotice(string.Empty, visible: false),
                    "blue")
            }, bootstrapReplacementRequested: true);
            DemoVisualContinuityDiagnostics continuityDiagnostics = continuity.Evaluate();
            Assert.That(continuityDiagnostics.Contains(DemoVisualContinuityDiagnosticCode.DemoRenderableAnchorMissing), Is.True);
            Assert.That(continuityDiagnostics.Contains(DemoVisualContinuityDiagnosticCode.DemoFallbackVisualMissing), Is.True);
            Assert.That(continuityDiagnostics.Contains(DemoVisualContinuityDiagnosticCode.DemoReadOnlyLimitHidden), Is.True);
            Assert.That(continuityDiagnostics.Contains(DemoVisualContinuityDiagnosticCode.DemoSurfaceInvisible), Is.True);
            Assert.That(continuityDiagnostics.Contains(DemoVisualContinuityDiagnosticCode.DemoBootstrapReplacementForbidden), Is.True);

            var onboarding = new PlayerOnboardingPath("path", new[]
            {
                new OnboardingStepPreview(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    OnboardingStepStatus.PreviewAction,
                    null,
                    OnboardingServerDependency.AccountFuture,
                    null,
                    runtimeTutorialClaim: true,
                    serverDependencyVisible: false)
            }, mobileVisibleStepCount: 9);
            OnboardingDiagnostics onboardingDiagnostics = onboarding.Evaluate();
            Assert.That(onboardingDiagnostics.Contains(OnboardingDiagnosticCode.OnboardingStepMissing), Is.True);
            Assert.That(onboardingDiagnostics.Contains(OnboardingDiagnosticCode.OnboardingExitRouteMissing), Is.True);
            Assert.That(onboardingDiagnostics.Contains(OnboardingDiagnosticCode.OnboardingRuntimeTutorialClaim), Is.True);
            Assert.That(onboardingDiagnostics.Contains(OnboardingDiagnosticCode.OnboardingServerDependencyHidden), Is.True);
            Assert.That(onboardingDiagnostics.Contains(OnboardingDiagnosticCode.OnboardingMobileOverloadRisk), Is.True);

            var profile = new PlayerHiveProfilePreview(
                "profile",
                "Ada",
                "Sunny Hive",
                new PlayerStyleSignalPreview(PlayerStyleSignalKind.Unknown, string.Empty),
                HiveProfileVisibilityState.PublicFuture,
                HiveProfileServerDependency.AccountRequired,
                new ProfilePrivacyFutureMarker(visible: true),
                persistent: true,
                containsPersonalData: true,
                rankingClaim: true,
                serverDependencyVisible: false);
            HiveProfileDiagnostics profileDiagnostics = profile.Evaluate();
            Assert.That(profileDiagnostics.Contains(HiveProfileDiagnosticCode.HiveProfilePersistenceForbidden), Is.True);
            Assert.That(profileDiagnostics.Contains(HiveProfileDiagnosticCode.PlayerIdentityServerDependencyHidden), Is.True);
            Assert.That(profileDiagnostics.Contains(HiveProfileDiagnosticCode.PersonalDataForbidden), Is.True);
            Assert.That(profileDiagnostics.Contains(HiveProfileDiagnosticCode.HiveProfileRankingClaimForbidden), Is.True);
            Assert.That(profileDiagnostics.Contains(HiveProfileDiagnosticCode.PlayerStyleSignalMissing), Is.True);

            var playstyles = new PlaystyleSelectionPreview("styles", new[]
            {
                new PlaystyleOptionCard(
                    "war",
                    "Fight",
                    new PlaystyleImpactHint("bonus", officialBonus: true, matchmakingClaim: true, monetizationClaim: true),
                    previewSelected: true,
                    new PlaystyleReversibilityNotice(visible: false),
                    new PlaystyleServerDependency(visible: false),
                    PlaystyleLockState.PreviewSelected)
            });
            PlaystyleDiagnostics playstyleDiagnostics = playstyles.Evaluate();
            Assert.That(playstyleDiagnostics.Contains(PlaystyleDiagnosticCode.PlaystyleOfficialBonusForbidden), Is.True);
            Assert.That(playstyleDiagnostics.Contains(PlaystyleDiagnosticCode.PlaystyleMatchmakingClaimForbidden), Is.True);
            Assert.That(playstyleDiagnostics.Contains(PlaystyleDiagnosticCode.PlaystylePersistenceHidden), Is.True);
            Assert.That(playstyleDiagnostics.Contains(PlaystyleDiagnosticCode.PlaystyleMonetizationClaimForbidden), Is.True);
            Assert.That(playstyleDiagnostics.Contains(PlaystyleDiagnosticCode.PlaystyleReversibilityMissing), Is.True);

            var goals = new FirstSessionGoalStack("goals", new[]
            {
                new FirstSessionGoalItem(
                    "goal",
                    GoalStackPriority.Primary,
                    "Get reward",
                    string.Empty,
                    new GoalStackCompletionPreview(previewComplete: true, officialClaim: true),
                    new GoalStackRewardBlocker(rewardRequested: true),
                    GoalStackServerDependency.RewardFuture,
                    serverDependencyVisible: false)
            }, initiallyVisibleGoals: 6);
            FirstSessionGoalDiagnostics goalDiagnostics = goals.Evaluate();
            Assert.That(goalDiagnostics.Contains(FirstSessionGoalDiagnosticCode.FirstSessionGoalMissingSurface), Is.True);
            Assert.That(goalDiagnostics.Contains(FirstSessionGoalDiagnosticCode.FirstSessionRewardForbidden), Is.True);
            Assert.That(goalDiagnostics.Contains(FirstSessionGoalDiagnosticCode.FirstSessionCompletionOfficialClaim), Is.True);
            Assert.That(goalDiagnostics.Contains(FirstSessionGoalDiagnosticCode.FirstSessionGoalOverload), Is.True);
            Assert.That(goalDiagnostics.Contains(FirstSessionGoalDiagnosticCode.FirstSessionServerDependencyHidden), Is.True);
        }

        [Test]
        public void AllyIntentAndEvidence_BlockServerAuthoritativeAndProductionClaims()
        {
            var allies = new AllyDiscoveryPreview("allies", new[]
            {
                new AllyCandidatePreviewCard(
                    "ally",
                    "Real Player",
                    PlayerStyleSignalKind.Defensive,
                    new CompatibilityHint("matched", matchmakingClaim: true),
                    new SocialPrivacyGuard(visible: false, personalDataPresent: true),
                    SocialInvitationStatus.SentForbidden,
                    new InvitationServerDependency(visible: false))
            }, new SocialInvitationPreview("invite", runtimeRequested: true));
            AllyDiscoveryDiagnostics allyDiagnostics = allies.Evaluate();
            Assert.That(allyDiagnostics.Contains(AllyDiscoveryDiagnosticCode.AllyCandidatePersonalDataForbidden), Is.True);
            Assert.That(allyDiagnostics.Contains(AllyDiscoveryDiagnosticCode.SocialInvitationRuntimeForbidden), Is.True);
            Assert.That(allyDiagnostics.Contains(AllyDiscoveryDiagnosticCode.AllyDiscoveryMatchmakingClaim), Is.True);
            Assert.That(allyDiagnostics.Contains(AllyDiscoveryDiagnosticCode.SocialPrivacyNoticeMissing), Is.True);
            Assert.That(allyDiagnostics.Contains(AllyDiscoveryDiagnosticCode.InvitationServerDependencyHidden), Is.True);

            var intents = new PeaceDefenseExpansionPreview("intents", new[]
            {
                new PeaceDefenseExpansionIntent(
                    "expand",
                    NonAggressivePosture.Expansionist,
                    "Claim tile",
                    "world",
                    "capture territory",
                    new WorldEconomyServerDependency(visible: false),
                    "world",
                    new NonAggressionLimitNotice(string.Empty, visible: false),
                    officialReward: true,
                    defenseRuntimeEffect: true,
                    territoryClaim: true)
            });
            PeaceDefenseExpansionDiagnostics intentDiagnostics = intents.Evaluate();
            Assert.That(intentDiagnostics.Contains(PeaceDefenseExpansionDiagnosticCode.PeacefulRewardOfficialForbidden), Is.True);
            Assert.That(intentDiagnostics.Contains(PeaceDefenseExpansionDiagnosticCode.DefenseRuntimeEffectForbidden), Is.True);
            Assert.That(intentDiagnostics.Contains(PeaceDefenseExpansionDiagnosticCode.ExpansionTerritoryClaimForbidden), Is.True);
            Assert.That(intentDiagnostics.Contains(PeaceDefenseExpansionDiagnosticCode.EconomyServerDependencyHidden), Is.True);
            Assert.That(intentDiagnostics.Contains(PeaceDefenseExpansionDiagnosticCode.NonAggressionMessageMissing), Is.True);

            var evidence = new DemoPlayModeEvidenceCapture("evidence", new[]
            {
                new VisualEvidenceFrame(
                    string.Empty,
                    string.Empty,
                    "release proof",
                    NonBlankFrameCheck.Blank,
                    new EvidenceLimitNotice(string.Empty, visible: false),
                    EvidencePrivacyStatus.PersonalDataPresent,
                    "production",
                    productionClaim: true)
            });
            DemoEvidenceDiagnostics evidenceDiagnostics = evidence.Evaluate();
            Assert.That(evidenceDiagnostics.Contains(DemoEvidenceDiagnosticCode.VisualEvidenceMissing), Is.True);
            Assert.That(evidenceDiagnostics.Contains(DemoEvidenceDiagnosticCode.VisualFrameBlank), Is.True);
            Assert.That(evidenceDiagnostics.Contains(DemoEvidenceDiagnosticCode.EvidenceLimitNoticeMissing), Is.True);
            Assert.That(evidenceDiagnostics.Contains(DemoEvidenceDiagnosticCode.PersonalDataInCaptureForbidden), Is.True);
            Assert.That(evidenceDiagnostics.Contains(DemoEvidenceDiagnosticCode.EvidenceClaimTooStrong), Is.True);
        }

        [Test]
        public void OnboardingDemoClosureGate_BlocksBee431PrematureAndProductionClaims()
        {
            var coverage = new[]
            {
                new OnboardingLotCoverageMatrix("BEE-422", string.Empty, string.Empty, "ui", string.Empty, string.Empty, DemoEvidenceReadinessVerdict.BlockedByMissingSurface)
            };
            var gate = new PlayerOnboardingDemoEvidenceClosureGate(
                "gate",
                coverage,
                new PlayerSurfaceLimitAudit(productionClaim: true, visualRegressionRisk: true),
                new ServerDependencyVisibilityAudit(visible: false),
                new Bee431BlockerStatus(prematureAttempt: true, message: "No release"));
            OnboardingClosureDiagnostics diagnostics = gate.Evaluate();
            Assert.That(diagnostics.Contains(OnboardingClosureDiagnosticCode.OnboardingLotSurfaceMissing), Is.True);
            Assert.That(diagnostics.Contains(OnboardingClosureDiagnosticCode.DemoEvidenceReserveHidden), Is.True);
            Assert.That(diagnostics.Contains(OnboardingClosureDiagnosticCode.ServerDependencyAuditMissing), Is.True);
            Assert.That(diagnostics.Contains(OnboardingClosureDiagnosticCode.ProductionClaimDetected), Is.True);
            Assert.That(diagnostics.Contains(OnboardingClosureDiagnosticCode.Bee431PrematureRelease), Is.True);
            Assert.That(diagnostics.Verdict, Is.EqualTo(DemoEvidenceReadinessVerdict.BlockedByBee431Premature));
        }
    }
}
