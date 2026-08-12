using System;
using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class ActivationReadinessFrameworks501To520Tests
    {
        [Test]
        public void StabilizationNavigationCommandsAndHiveActions_BlockRegressionsAndImplicitServer()
        {
            var stabilization = new PlayableSlicePostMilestoneStabilization("stability", new[]
            {
                new PostMilestoneInvariant("HomeAlwaysReachable", "Hub", "Retour", "live runtime claim")
            }, Array.Empty<PostMilestoneReserve>());
            Assert.That(stabilization.Verdict, Is.EqualTo(PostMilestoneStabilityVerdict.BlockedByRegression));

            var audit = new MobileNavigationFrictionAudit("nav", new[]
            {
                new MobileRouteFriction("home-world", "Home", string.Empty, "MissingBackPath", 5)
            }, Array.Empty<MobileNavigationRecommendation>());
            Assert.That(audit.Verdict, Is.EqualTo(MobileNavigationAuditVerdict.BlockedByMissingBackPath));

            var candidates = new HiveActionActivationCandidateRegistry("actions", new[]
            {
                new HiveActionActivationCandidate("UpgradeRoom", "upgrade", "panel", string.Empty, "server risk")
            }, Array.Empty<HiveActionActivationBlocker>());
            Assert.That(candidates.Readiness, Is.EqualTo(HiveActionActivationReadiness.NeedsUiClarification));

            var commands = new ServerCommandReadinessChecklist("commands", new[]
            {
                new FutureCommandReadinessRow("UpgradeRoom", "UpgradeRoomCommand", string.Empty, string.Empty, implementationForbiddenNow: false)
            }, Array.Empty<ServerReadinessExclusion>());
            Assert.That(commands.Verdict, Is.EqualTo(ServerCommandReadinessVerdict.BlockedByImplementationLeak));
        }

        [Test]
        public void EconomyConstructionWorkforceSocialWorldAndCombatBoundaries_BlockOfficialMutation()
        {
            var economy = new ResourceEconomyActivationBoundary("economy", new[]
            {
                new ResourceBoundaryRule("honey", "official balance", "forbid spend", "preview")
            }, Array.Empty<ResourcePreviewScenario>());
            Assert.That(economy.Verdict, Is.EqualTo(ResourceEconomyBoundaryVerdict.BlockedByOfficialEconomyClaim));

            var construction = new ConstructionUpgradeActivationBoundary("construction", new[]
            {
                new ConstructionUpgradeCandidate("storage", "upgrade", "more space", "needs preview", "server")
            }, new[] { new ConstructionActivationRule("active", "active construction claim") });
            Assert.That(construction.Verdict, Is.EqualTo(ConstructionUpgradeBoundaryVerdict.BlockedByActivationClaim));

            var workforce = new WorkforceAssignmentActivationBoundary("workforce", new[]
            {
                new WorkforceNeedPreview("need", "ouvriere", 2, "missing", "mutation forbidden")
            }, new[] { new WorkforceAssignmentSuggestion("assign", "move bees", mutatesOfficialPopulation: true) });
            Assert.That(workforce.Verdict, Is.EqualTo(WorkforceBoundaryVerdict.BlockedByOfficialMutationClaim));

            var social = new AllianceCommunicationActivationBoundary("social", new[]
            {
                new SocialPreviewSurfaceRule("chat", "read preview", "live chat", "server future")
            }, new[] { new SocialLiveClaimRisk("send", "message sent", liveClaim: true) });
            Assert.That(social.Verdict, Is.EqualTo(SocialActivationBoundaryVerdict.BlockedByLiveSocialClaim));

            var scouting = new WorldScoutingReportActivationBoundary("scouting", new[]
            {
                new ScoutingReportPreview("prairie", "north", "nectar", "low", "return")
            }, new[] { new ScoutingOfficialClaimBlocker("map", "live map opened") });
            Assert.That(scouting.Verdict, Is.EqualTo(WorldScoutingBoundaryVerdict.BlockedByLiveMapClaim));

            var defense = new DefenseCombatActivationBoundary("defense", new[]
            {
                new DefenseReadinessSignal("guard", "guard low", "prepare guards", "no combat")
            }, new[] { new CombatLiveClaimBlocker("attack", "attack available", combatClaim: true) });
            Assert.That(defense.Verdict, Is.EqualTo(DefenseCombatBoundaryVerdict.BlockedByCombatClaim));
        }

        [Test]
        public void AccountInboxEventsTradeResearchPersistenceAccessibilityAndPerformance_BlockOfficialClaims()
        {
            var profile = new PlayerAccountProfileReadinessPreview("profile", new[]
            {
                new PlayerProfilePreviewField("display", "local name", "no account", "who owns identity?")
            }, new[] { new AccountAuthorityBlocker("login", "official account created", accountClaim: true) });
            Assert.That(profile.Verdict, Is.EqualTo(PlayerProfileReadinessVerdict.BlockedByOfficialAccountClaim));

            var inbox = new NotificationInboxActivationBoundary("inbox", new[]
            {
                new InboxPreviewMessageRule("system hint", "explain", "preview", "no push")
            }, new[] { new NotificationDeliveryBlocker("push", "push delivered", liveDeliveryClaim: true) });
            Assert.That(inbox.Verdict, Is.EqualTo(NotificationInboxBoundaryVerdict.BlockedByDeliveryClaim));

            var events = new EventParticipationActivationBoundary("events", new[]
            {
                new EventParticipationPreviewCard("festival", "narrative", "preview", "read", "no reward")
            }, new[] { new LiveOpsClaimBlocker("reward", liveOpsClaim: true) });
            Assert.That(events.Verdict, Is.EqualTo(EventParticipationBoundaryVerdict.BlockedByLiveOpsClaim));

            var trade = new TradeMarketActivationBoundary("trade", new[]
            {
                new TradeOpportunityPreview("pollen", "pollen", "maybe useful", "route risky", "no price")
            }, new[] { new MarketOfficialClaimBlocker("market", marketClaim: true) });
            Assert.That(trade.Verdict, Is.EqualTo(TradeMarketBoundaryVerdict.BlockedByMarketClaim));

            var research = new ResearchGeneticsActivationBoundary("research", new[]
            {
                new ResearchGeneticsPreviewChoice("harvest", "recolte", "better fantasy", "lab", "no bonus")
            }, new[] { new ResearchBalanceRisk("bonus", "official bonus", officialEffectClaim: true) });
            Assert.That(research.Verdict, Is.EqualTo(ResearchGeneticsBoundaryVerdict.BlockedByOfficialEffectClaim));

            var persistence = new OnboardingPersistenceDecision("persist", new[]
            {
                new OnboardingPersistenceCandidate("first", "helps", "where stored?", "privacy", "RejectedSensitiveData")
            }, Array.Empty<OnboardingPersistenceExclusion>());
            Assert.That(persistence.Verdict, Is.EqualTo(OnboardingPersistenceDecisionVerdict.BlockedByPrivacyRisk));

            var accessibility = new AccessibilityLocalizationReadiness("a11y", new[]
            {
                new AccessibilityLocalizationRule("tap", "all", "large tap", "tiny tap")
            }, new[] { new LocalizationTermRisk("claim", "live verb", liveVerbRisk: true) });
            Assert.That(accessibility.Verdict, Is.EqualTo(AccessibilityLocalizationVerdict.BlockedByLiveVerbRisk));

            var performance = new PlayableSlicePerformanceBudgetPreview("perf", new[]
            {
                new PerformanceBudgetPreviewRule("cards", "hub", "few cards", "crowded")
            }, new[] { new PerformanceBudgetRisk("benchmark", "avoid claim", benchmarkClaim: true) });
            Assert.That(performance.Verdict, Is.EqualTo(PerformanceBudgetPreviewVerdict.BlockedByBenchmarkClaim));
        }

        [Test]
        public void Demo600AndClosureGate_BlockLiveDemoServerLeakAndBee521()
        {
            var roadmap = new Bee600DemoRoadmapAccumulator("demo600", new[]
            {
                new Bee600DemoProofCandidate("profile", "BEE-501..520", "show profile", string.Empty, liveClaim: true)
            }, Array.Empty<Bee600DemoReserve>());
            Assert.That(roadmap.Verdict, Is.EqualTo(Bee600DemoRoadmapVerdict.BlockedByLiveDemoClaim));

            var hiddenServer = new ActivationReadinessLotClosureGate("server", ValidLedger(row => row.SourceBee == "BEE-504" ? new ActivationClosureLedgerRow(row.SourceBee, row.ClosureQuestion, row.EvidencePointer, "service serveur deduit", row.OwnerLane) : row), ValidLanes(), Array.Empty<ActivationClosureReserve>(), Bee521BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(hiddenServer.Verdict, Is.EqualTo(ActivationReadinessLotVerdict.BlockedByHiddenServerService));

            var liveClaim = new ActivationReadinessLotClosureGate("live", ValidLedger(row => row.SourceBee == "BEE-505" ? new ActivationClosureLedgerRow(row.SourceBee, row.ClosureQuestion, row.EvidencePointer, "feature live declaree", row.OwnerLane) : row), ValidLanes(), Array.Empty<ActivationClosureReserve>(), Bee521BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(liveClaim.Verdict, Is.EqualTo(ActivationReadinessLotVerdict.BlockedByLiveClaim));

            var premature = new ActivationReadinessLotClosureGate("premature", ValidLedger(row => row), ValidLanes(), Array.Empty<ActivationClosureReserve>(), Bee521BlockerStatus.ReleasedByFutureArchitectDecision);
            Assert.That(premature.Verdict, Is.EqualTo(ActivationReadinessLotVerdict.BlockedByBee521Premature));

            var ready = new ActivationReadinessLotClosureGate("ready", ValidLedger(row => row), ValidLanes(), new[] { new ActivationClosureReserve("bee500", "Narrative reserve remains visible.") }, Bee521BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(ready.Verdict, Is.EqualTo(ActivationReadinessLotVerdict.ReadyWithOpenReserves));
        }

        private static IReadOnlyList<ActivationClosureLedgerRow> ValidLedger(Func<ActivationClosureLedgerRow, ActivationClosureLedgerRow> mutate)
        {
            var rows = new List<ActivationClosureLedgerRow>();
            for (int bee = 501; bee <= 519; bee++)
            {
                rows.Add(mutate(new ActivationClosureLedgerRow("BEE-" + bee, "Question " + bee, "Evidence " + bee, "no refusal", "BuilderImplementation")));
            }

            return rows;
        }

        private static IReadOnlyList<ActivationClosureTeamLane> ValidLanes()
        {
            return new[]
            {
                new ActivationClosureTeamLane("BuilderImplementation", true),
                new ActivationClosureTeamLane("ServerImpactReview", true),
                new ActivationClosureTeamLane("UiMobileReview", true),
                new ActivationClosureTeamLane("QaLotValidation", true),
                new ActivationClosureTeamLane("Demo600Followup", true),
                new ActivationClosureTeamLane("ArchitectDecision", true)
            };
        }
    }
}
