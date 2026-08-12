using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class PlayerReturnNetworkFrameworks431To440Tests
    {
        [Test]
        public void ReturnNetworkHomeRecapAndNotifications_BlockFalseReturnLoops()
        {
            var network = new PlayerReturnNetwork("network", new[]
            {
                new ReturnNetworkNode("home", homeAccessible: true),
                new ReturnNetworkNode("ruche"),
                new ReturnNetworkNode("alliance")
            }, new[]
            {
                new ReturnNetworkRoute("route", "ruche", string.Empty, new ReturnReasonPreview(string.Empty, pushClaim: true), ReturnPriorityHint.CriticalPreview, ReturnServerDependency.NotificationFuture, string.Empty, serverDependencyVisible: false)
            });
            PlayerReturnDiagnostics networkDiagnostics = network.Evaluate();
            Assert.That(networkDiagnostics.Contains(PlayerReturnDiagnosticCode.ReturnNodeMissing), Is.True);
            Assert.That(networkDiagnostics.Contains(PlayerReturnDiagnosticCode.ReturnRouteDeadEnd), Is.True);
            Assert.That(networkDiagnostics.Contains(PlayerReturnDiagnosticCode.ReturnReasonMissing), Is.True);
            Assert.That(networkDiagnostics.Contains(PlayerReturnDiagnosticCode.ReturnPushNotificationForbidden), Is.True);
            Assert.That(networkDiagnostics.Contains(PlayerReturnDiagnosticCode.ReturnServerDependencyHidden), Is.True);

            var home = new HomeReturnRoute("home-route", new[]
            {
                new SafeExitAction(string.Empty, "world", new ReturnContextSnapshot(string.Empty, contextLost: true), new UnsavedPreviewNotice(string.Empty, visible: false, runtimeSaveClaim: true), null, new ExitServerDependency(visible: false), confirmationCount: 3)
            });
            HomeReturnDiagnostics homeDiagnostics = home.Evaluate();
            Assert.That(homeDiagnostics.Contains(HomeReturnDiagnosticCode.HomeReturnMissing), Is.True);
            Assert.That(homeDiagnostics.Contains(HomeReturnDiagnosticCode.SafeExitContextLost), Is.True);
            Assert.That(homeDiagnostics.Contains(HomeReturnDiagnosticCode.UnsavedPreviewMessageMissing), Is.True);
            Assert.That(homeDiagnostics.Contains(HomeReturnDiagnosticCode.HomeReturnRuntimeSaveClaim), Is.True);
            Assert.That(homeDiagnostics.Contains(HomeReturnDiagnosticCode.ExitRouteOverConfirmation), Is.True);

            var recap = new SessionRecapPreview("recap", new[]
            {
                new RecapVisitedSurface("profile", personalData: true)
            }, "Won", null, new RecapPrivacyNotice(visible: false, privacyRisk: true), RecapServerDependency.AnalyticsFuture, serverDependencyVisible: false, new RecapProgressClaimGuard(progressClaim: true, rewardClaim: true));
            SessionRecapDiagnostics recapDiagnostics = recap.Evaluate();
            Assert.That(recapDiagnostics.Contains(SessionRecapDiagnosticCode.SessionRecapProgressClaim), Is.True);
            Assert.That(recapDiagnostics.Contains(SessionRecapDiagnosticCode.SessionRecapRewardForbidden), Is.True);
            Assert.That(recapDiagnostics.Contains(SessionRecapDiagnosticCode.SessionRecapPrivacyRisk), Is.True);
            Assert.That(recapDiagnostics.Contains(SessionRecapDiagnosticCode.NextReturnPromptMissing), Is.True);
            Assert.That(recapDiagnostics.Contains(SessionRecapDiagnosticCode.RecapServerDependencyHidden), Is.True);

            var notifications = new NotificationReturnLoopPreview("notifications", new[]
            {
                new ReturnNotificationItem("push", ReturnNotificationKind.Chat, "Ada wrote to you", null, new UnreadOfficialClaimGuard(officialUnreadClaim: true, pushLiveClaim: true), new NotificationPreviewExpiry("soon"), new NotificationServerDependency(visible: false), personalData: true)
            });
            NotificationDiagnostics notificationDiagnostics = notifications.Evaluate();
            Assert.That(notificationDiagnostics.Contains(NotificationDiagnosticCode.NotificationPushForbidden), Is.True);
            Assert.That(notificationDiagnostics.Contains(NotificationDiagnosticCode.UnreadOfficialClaimForbidden), Is.True);
            Assert.That(notificationDiagnostics.Contains(NotificationDiagnosticCode.NotificationRouteMissing), Is.True);
            Assert.That(notificationDiagnostics.Contains(NotificationDiagnosticCode.NotificationPersonalDataRisk), Is.True);
            Assert.That(notificationDiagnostics.Contains(NotificationDiagnosticCode.NotificationServerDependencyHidden), Is.True);
        }

        [Test]
        public void AllianceHiveWorldAndArmy_BlockRuntimeAuthorityClaims()
        {
            var alliance = new AllianceHelpReturnLoop("help", new[]
            {
                new AllianceHelpRequestPreview("request", HelpNeedKind.Defense, "Send help now", new HelpReturnRoute(string.Empty, string.Empty), new HelpJournalPreviewEntry(string.Empty, visible: false), new AllianceHelpServerDependency(visible: false, membershipVisible: false), liveSendRequested: true, personalMessage: true)
            });
            AllianceHelpDiagnostics allianceDiagnostics = alliance.Evaluate();
            Assert.That(allianceDiagnostics.Contains(AllianceHelpDiagnosticCode.AllianceHelpLiveSendForbidden), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceHelpDiagnosticCode.AllianceMembershipRequiredHidden), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceHelpDiagnosticCode.HelpReturnRouteMissing), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceHelpDiagnosticCode.HelpJournalEntryMissing), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceHelpDiagnosticCode.AllianceHelpServerDependencyHidden), Is.True);

            var hive = new HiveNeedReturnSignal("hive", HiveNeedKind.Resource, HiveNeedSurfaceOrigin.Home, new HiveNeedPreviewReason(string.Empty, alarmist: true), HiveReturnPriority.PreviewUrgent, string.Empty, new HiveNeedBlocker("collect", runtimeAction: true, officialCostClaim: true), new HiveNeedServerDependency(visible: false));
            HiveNeedDiagnostics hiveDiagnostics = hive.Evaluate();
            Assert.That(hiveDiagnostics.Contains(HiveNeedDiagnosticCode.HiveNeedReasonMissing), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveNeedDiagnosticCode.HiveNeedRuntimeActionForbidden), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveNeedDiagnosticCode.HiveNeedReturnRouteMissing), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveNeedDiagnosticCode.HiveNeedCostOfficialClaim), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveNeedDiagnosticCode.HiveNeedServerDependencyHidden), Is.True);

            var world = new WorldReturnSignal("world", WorldReturnSignalKind.Threat, string.Empty, null, new WorldActionBlocker("attack", forbiddenRuntimeAction: true, liveThreatClaim: true, rewardClaim: true), string.Empty, new WorldServerDependency(visible: false));
            WorldDiagnostics worldDiagnostics = world.Evaluate();
            Assert.That(worldDiagnostics.Contains(WorldDiagnosticCode.WorldReturnActionForbidden), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldDiagnosticCode.WorldThreatLiveClaim), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldDiagnosticCode.WorldOpportunityRewardClaim), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldDiagnosticCode.WorldReturnRouteMissing), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldDiagnosticCode.WorldServerDependencyHidden), Is.True);

            var army = new ArmyReadinessReturnSignal("army", new DefenseReadinessPreview("ready", officialClaim: true), null, "world", string.Empty, new ArmyTrainingActionBlocker("train", runtimeTraining: true, lossClaim: true, rewardClaim: true), new ArmyServerDependency(visible: false));
            ArmyDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyDiagnosticCode.ArmyReadinessOfficialClaim), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDiagnosticCode.ArmyTrainingRuntimeForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDiagnosticCode.ArmyLossRewardForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDiagnosticCode.ArmyReturnRouteMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyDiagnosticCode.ArmyServerDependencyHidden), Is.True);
        }

        [Test]
        public void AccessibilityAndClosureGate_BlockBee441PrematureRelease()
        {
            var accessibility = new MobileReturnNavigationAccessibility("access", new[]
            {
                new ReturnControlReadabilityRule("back", new ReturnTargetLabelRule("this label is far too long for mobile", ambiguous: true), 8, new ReturnTouchTargetNeed(32), new ReturnContrastNeed(2.5f), overlapStatus: true, ReturnAccessibilityVerdict.BlockedByCertificationClaim, certificationClaim: true)
            });
            AccessibilityDiagnostics accessibilityDiagnostics = accessibility.Evaluate();
            Assert.That(accessibilityDiagnostics.Contains(AccessibilityDiagnosticCode.ReturnLabelTooLong), Is.True);
            Assert.That(accessibilityDiagnostics.Contains(AccessibilityDiagnosticCode.ReturnTargetAmbiguous), Is.True);
            Assert.That(accessibilityDiagnostics.Contains(AccessibilityDiagnosticCode.ReturnTouchTargetTooSmall), Is.True);
            Assert.That(accessibilityDiagnostics.Contains(AccessibilityDiagnosticCode.ReturnControlOverlap), Is.True);
            Assert.That(accessibilityDiagnostics.Contains(AccessibilityDiagnosticCode.ReturnAccessibilityCertificationClaim), Is.True);

            var gate = new PlayerReturnNetworkClosureGate("gate", new[]
            {
                new ReturnNetworkCoverageMatrix("BEE-431", string.Empty, string.Empty, "demo", "ui", "qa", string.Empty, ReturnLoopVerdict.BlockedByDeadEndSurface),
                new ReturnNetworkCoverageMatrix("BEE-434", "notification", "push", "demo", "ui", "qa", "server", ReturnLoopVerdict.BlockedByLiveNotificationClaim),
                new ReturnNetworkCoverageMatrix("BEE-439", "access", "readability", "demo", "ui", "qa", "server", ReturnLoopVerdict.BlockedByAccessibilityGap)
            }, new ReturnDemoEvidenceNeed(string.Empty, reserveVisible: false), new ReturnServerBoundaryAudit(visible: false), new Bee441BlockerStatus(prematureAttempt: true, message: "blocked"));
            ReturnClosureDiagnostics closureDiagnostics = gate.Evaluate();
            Assert.That(closureDiagnostics.Contains(ReturnClosureDiagnosticCode.ReturnNetworkNodeGap), Is.True);
            Assert.That(closureDiagnostics.Contains(ReturnClosureDiagnosticCode.ReturnLoopDeadEnd), Is.True);
            Assert.That(closureDiagnostics.Contains(ReturnClosureDiagnosticCode.ReturnLiveClaimDetected), Is.True);
            Assert.That(closureDiagnostics.Contains(ReturnClosureDiagnosticCode.ReturnAccessibilityGap), Is.True);
            Assert.That(closureDiagnostics.Contains(ReturnClosureDiagnosticCode.Bee441PrematureRelease), Is.True);
            Assert.That(closureDiagnostics.Verdict, Is.EqualTo(ReturnLoopVerdict.BlockedByBee441Premature));
        }
    }
}
