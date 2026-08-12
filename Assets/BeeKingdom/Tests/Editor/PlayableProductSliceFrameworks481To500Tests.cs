using System;
using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class PlayableProductSliceFrameworks481To500Tests
    {
        [Test]
        public void WorldSocialTradeAndConflictPreviews_BlockLiveClaims()
        {
            var worldExit = new HiveToWorldPlayerExitPreview("world", new[]
            {
                new WorldExitPreviewRoute("explore", "Explorer", WorldExitPreviewDomain.Exploration, string.Empty, WorldExitPreviewState.LiveMapBlocked, returnsToHive: false, serverBoundaryVisible: false, liveClaim: true)
            }, null, new WorldExitAuthorityNotice(string.Empty, visible: false));
            PlayableSliceDiagnostics worldDiagnostics = worldExit.Evaluate();
            Assert.That(worldDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(worldDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(worldDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(worldDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var scouting = new WorldScoutingIntentPreview("scouting", new[]
            {
                new ScoutingIntentOption("resource", "ressource", string.Empty, ScoutingPreviewRisk.LowPreview, ScoutingAuthorityState.PreviewOnly, string.Empty, returnsToWorldExit: false, serverBoundaryVisible: false, sendClaim: true, reportClaim: true)
            }, null);
            PlayableSliceDiagnostics scoutingDiagnostics = scouting.Evaluate();
            Assert.That(scoutingDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(scoutingDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(scoutingDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(scoutingDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var alliance = new AllianceDiscoveryJoinIntentPreview("alliance", new[]
            {
                new AlliancePreviewCard("real", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, serverBoundaryVisible: false, realPlayerClaim: true)
            }, AllianceJoinIntentState.PreviewOnly, returnsToAlliancePortal: false, returnsToWorldExit: false);
            PlayableSliceDiagnostics allianceDiagnostics = alliance.Evaluate();
            Assert.That(allianceDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(allianceDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(allianceDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(allianceDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var help = new AllianceHelpRequestComposerPreview("help", new[]
            {
                new HelpRequestDraftOption("draft", "ressource", string.Empty, string.Empty, string.Empty, serverBoundaryVisible: false, sendClaim: true, unreadClaim: true)
            }, null, HelpRequestSendState.PreviewDraft, returnsToSource: false);
            PlayableSliceDiagnostics helpDiagnostics = help.Evaluate();
            Assert.That(helpDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(helpDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(helpDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(helpDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var inbox = new CommunicationInboxPreview("inbox", new[]
            {
                new CommunicationPreviewChannel("system", string.Empty, CommunicationPreviewKind.System, string.Empty, linkedToHelp: false, linkedToScoutingReports: false, serverBoundaryVisible: false, liveInputClaim: true, unreadClaim: true)
            }, null);
            PlayableSliceDiagnostics inboxDiagnostics = inbox.Evaluate();
            Assert.That(inboxDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(inboxDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(inboxDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(inboxDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);
        }

        [Test]
        public void EventTradeConflictAndArmyPreviews_BlockRuntimeEconomyAndPvp()
        {
            var eventBoard = new WorldEventBoardPreview("events", new[]
            {
                new WorldEventPreviewCard("event", string.Empty, WorldEventPreviewKind.AllianceCooperation, string.Empty, WorldEventParticipationState.PreviewOnly, communicationLinked: false, serverBoundaryVisible: false, rewardClaim: true, rankingClaim: true, timerLiveClaim: true)
            }, WorldEventBoardState.EventsPreviewed);
            PlayableSliceDiagnostics eventDiagnostics = eventBoard.Evaluate();
            Assert.That(eventDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(eventDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(eventDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(eventDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var trade = new TradeRouteOpportunityPreview("trade", new[]
            {
                new TradeOpportunityCard("trade", string.Empty, string.Empty, string.Empty, TradeOpportunityAuthorityState.PreviewOnly, serverBoundaryVisible: false, transactionClaim: true, priceClaim: true, deliveryClaim: true)
            }, null);
            PlayableSliceDiagnostics tradeDiagnostics = trade.Evaluate();
            Assert.That(tradeDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(tradeDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(tradeDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var conflict = new ConflictRiskRallyIntentPreview("conflict", new[]
            {
                new ConflictPreviewSignal("threat", string.Empty, ConflictPreviewSeverity.Risk, ConflictAuthorityState.PreviewOnly, serverBoundaryVisible: false, liveTargetClaim: true)
            }, new[]
            {
                new RallyIntentPreview("rally", string.Empty, defenseLinked: false, allianceLinked: false, serverBoundaryVisible: false, launchClaim: true)
            }, antiHarassmentNoticeVisible: false, beginnerProtectionNoticeVisible: false);
            PlayableSliceDiagnostics conflictDiagnostics = conflict.Evaluate();
            Assert.That(conflictDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(conflictDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(conflictDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(conflictDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var army = new ArmyDefensePlayableReadinessPreview("army", new[]
            {
                new ArmyReadinessPreviewItem("def", "defense", string.Empty, string.Empty, roleLinked: false, scoutingLinked: false, rallyLinked: false, serverBoundaryVisible: false, liveActionClaim: true)
            }, ArmyReadinessPreviewVerdict.PreparedPreview, antiHarassmentNoticeVisible: false);
            PlayableSliceDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(armyDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(armyDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(armyDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);
        }

        [Test]
        public void PlayerLoopFeedbackProgressionAndGovernance_BlockOfficialProgression()
        {
            var onboarding = new PlayerOnboardingFirstHiveMinute("onboarding", new[]
            {
                new OnboardingHiveStep("step", string.Empty, string.Empty, OnboardingStepLimit.PreviewOnly, rewardClaim: true)
            }, OnboardingPreviewState.BlockedByLiveClaim);
            PlayableSliceDiagnostics onboardingDiagnostics = onboarding.Evaluate();
            Assert.That(onboardingDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingPlayableLoop), Is.True);
            Assert.That(onboardingDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(onboardingDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var hub = new PlayerHomeCommandHubPreview("hub", new[]
            {
                new HomeHubTilePreview("ruche", string.Empty, string.Empty, string.Empty, serverBoundaryVisible: false, unreadClaim: true, profileClaim: true)
            }, HomeHubPreviewState.BlockedByLiveClaim);
            PlayableSliceDiagnostics hubDiagnostics = hub.Evaluate();
            Assert.That(hubDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(hubDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(hubDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var loop = new GuidedActionPreparationLoop("loop", new[]
            {
                new GuidedActionLoopStep("choose", string.Empty, string.Empty, GuidedActionStepState.Viewed, spendClaim: true)
            }, GuidedActionLoopOutcome.BlockedByLiveClaim);
            PlayableSliceDiagnostics loopDiagnostics = loop.Evaluate();
            Assert.That(loopDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingPlayableLoop), Is.True);
            Assert.That(loopDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(loopDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingRoute), Is.True);
            Assert.That(loopDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var feedback = new PlayerFeedbackBlockerToneCatalog("feedback", new[]
            {
                new PlayerFeedbackBlockerMessage("pay", "server", "Paie pour continuer. Deuxieme phrase.", string.Empty, monetizationLanguage: true, blamesPlayer: true)
            }, Array.Empty<PlayerFeedbackToneRule>());
            PlayableSliceDiagnostics feedbackDiagnostics = feedback.Evaluate();
            Assert.That(feedbackDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(feedbackDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingQaControl), Is.True);
            Assert.That(feedbackDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var recap = new PlayerSessionRecapPreview("recap", new[]
            {
                new SessionRecapFact("a", string.Empty),
                new SessionRecapFact("b", string.Empty),
                new SessionRecapFact("c", string.Empty),
                new SessionRecapFact("d", string.Empty)
            }, new NextBestActionPreview("next", string.Empty, string.Empty, string.Empty, rewardClaim: true), SessionRecapPersistenceState.PersistentClaimBlocked);
            PlayableSliceDiagnostics recapDiagnostics = recap.Evaluate();
            Assert.That(recapDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(recapDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(recapDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var strip = new PlayerProgressionVisibilityStrip("strip", new[]
            {
                new PlayerProgressionPreviewMarker("xp", string.Empty, string.Empty, ProgressionMarkerPreviewState.DotSeen, officialProgressionClaim: true)
            }, ProgressionStripAuthorityState.OfficialProgressionClaimBlocked);
            PlayableSliceDiagnostics stripDiagnostics = strip.Evaluate();
            Assert.That(stripDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingSurface), Is.True);
            Assert.That(stripDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(stripDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingDemoEvidence), Is.True);
            Assert.That(stripDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);
        }

        [Test]
        public void Bee500ReadinessAndGate_BlockMissingProofHiddenAuthorityAndBee501()
        {
            var accumulator = new Bee500DemoReadinessAccumulator("demo", new[]
            {
                new DemoReadinessSurface("hive", "BEE-451..470", "hive", DemoReadinessSurfaceState.ContractReady)
            }, Array.Empty<DemoReadinessReserve>());
            Assert.That(accumulator.Status, Is.EqualTo(Bee500ReadinessStatus.BlockedByMissingSurface));

            var midwave = new Bee500PlayableSliceMidwaveAlignment("midwave", new[]
            {
                new Bee500SliceSurfaceRow("BEE-481", "world", "exit", string.Empty)
            }, Array.Empty<Bee500NextStepRow>());
            Assert.That(midwave.Status, Is.EqualTo(Bee500MidwaveAlignmentStatus.BlockedByMissingSurface));

            var serverMap = new ServerAuthorityReadinessMapForPlayableSlice("server-map", new[]
            {
                new ServerAuthorityFutureDomain("account", string.Empty, string.Empty, string.Empty)
            }, Array.Empty<ServerAuthorityOpenQuestion>(), backendImplementationClaim: true);
            PlayableSliceDiagnostics serverDiagnostics = serverMap.Evaluate();
            Assert.That(serverDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingServerBoundary), Is.True);
            Assert.That(serverDiagnostics.Contains(PlayableSliceDiagnosticCode.MissingQaControl), Is.True);
            Assert.That(serverDiagnostics.Contains(PlayableSliceDiagnosticCode.ForbiddenLiveClaim), Is.True);

            var demoQa = new Bee500DemoQaPlayableSliceReadiness("demoqa", new[]
            {
                new Bee500EvidenceRequirement("hub", "hub visible", "hub", string.Empty, prepared: true)
            }, Array.Empty<Bee500QaRisk>());
            Assert.That(demoQa.Verdict, Is.EqualTo(Bee500DemoQaReadinessVerdict.BlockedByMissingEvidence));

            var liveGate = new PlayableProductMilestoneGate("live", ValidGateRows(row => row.DomainId == "commerce" ? new PlayableSliceGateRow(row.DomainId, row.RequiredProof, string.Empty, PlayableSliceGateStatus.LiveClaim) : row), Array.Empty<PlayableSliceReserve>(), Bee501BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(liveGate.Verdict, Is.EqualTo(PlayableProductMilestoneVerdict.BlockedByLiveClaim));

            var hiddenGate = new PlayableProductMilestoneGate("hidden", ValidGateRows(row => row.DomainId == "server-authority" ? new PlayableSliceGateRow(row.DomainId, row.RequiredProof, row.ForbiddenClaim, PlayableSliceGateStatus.HiddenServerAuthority) : row), Array.Empty<PlayableSliceReserve>(), Bee501BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(hiddenGate.Verdict, Is.EqualTo(PlayableProductMilestoneVerdict.BlockedByHiddenServerAuthority));

            var prematureGate = new PlayableProductMilestoneGate("premature", ValidGateRows(row => row), Array.Empty<PlayableSliceReserve>(), Bee501BlockerStatus.ReleasedByFutureArchitectDecision);
            Assert.That(prematureGate.Verdict, Is.EqualTo(PlayableProductMilestoneVerdict.BlockedByBee501Premature));

            var readyGate = new PlayableProductMilestoneGate("ready", ValidGateRows(row => row), new[] { new PlayableSliceReserve("demo", "Demo BEE-500 proof remains pending.") }, Bee501BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(readyGate.Verdict, Is.EqualTo(PlayableProductMilestoneVerdict.ReadyWithPreviewReserves));
        }

        private static IReadOnlyList<PlayableSliceGateRow> ValidGateRows(Func<PlayableSliceGateRow, PlayableSliceGateRow> mutate)
        {
            string[] domains =
            {
                "premiere-minute", "hub", "ruche", "boucle-action", "feedbacks", "progression", "monde", "alliance", "communication", "evenements", "commerce", "conflit", "armee", "server-authority", "demo-qa"
            };

            var rows = new List<PlayableSliceGateRow>();
            foreach (string domain in domains)
            {
                rows.Add(mutate(new PlayableSliceGateRow(domain, "Proof for " + domain, "No live claim for " + domain, PlayableSliceGateStatus.Covered)));
            }

            return rows;
        }
    }
}
