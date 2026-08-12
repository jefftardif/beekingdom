using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class PlayerMemoryNetworkFrameworks441To450Tests
    {
        [Test]
        public void PlayerHiveAndAllianceMemory_BlockOfficialHistoryAndSocialPersistence()
        {
            var memory = new PlayerMemoryPreview("memory", new[]
            {
                new MemoryPreviewMoment("moment", null, string.Empty, MemoryImportanceHint.PreviewCritical, null, officialHistoryBlocked: false, MemoryServerDependency.HistoryFuture, serverDependencyVisible: false, persistent: true, officialHistoryClaim: true)
            });
            PlayerMemoryDiagnostics memoryDiagnostics = memory.Evaluate();
            Assert.That(memoryDiagnostics.Contains(PlayerMemoryDiagnosticCode.PlayerMemoryPersistenceForbidden), Is.True);
            Assert.That(memoryDiagnostics.Contains(PlayerMemoryDiagnosticCode.MemorySourceMissing), Is.True);
            Assert.That(memoryDiagnostics.Contains(PlayerMemoryDiagnosticCode.MemoryReturnRouteMissing), Is.True);
            Assert.That(memoryDiagnostics.Contains(PlayerMemoryDiagnosticCode.MemoryOfficialHistoryClaim), Is.True);
            Assert.That(memoryDiagnostics.Contains(PlayerMemoryDiagnosticCode.MemoryServerDependencyHidden), Is.True);

            var hive = new HiveMemoryMoment("hive", new HiveNeedMemorySource(null, string.Empty), string.Empty, new HivePriorityMemoryHint("urgent"), null, new HiveMutationClaimGuard(mutationClaim: true, costClaim: true), new HiveMemoryServerDependency(visible: false));
            HiveMemoryDiagnostics hiveDiagnostics = hive.Evaluate();
            Assert.That(hiveDiagnostics.Contains(HiveMemoryDiagnosticCode.HiveMemoryMutationForbidden), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveMemoryDiagnosticCode.HiveMemoryNeedMissing), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveMemoryDiagnosticCode.HiveMemoryReturnRouteMissing), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveMemoryDiagnosticCode.HiveMemoryCostClaim), Is.True);
            Assert.That(hiveDiagnostics.Contains(HiveMemoryDiagnosticCode.HiveMemoryServerDependencyHidden), Is.True);

            var alliance = new AllianceSharedMemoryPreview("alliance", new[]
            {
                new AllianceMemoryMoment("social", "help", new AllyMemoryReference("real-player", exampleOnly: false, personalData: true), "sent", new SharedMemoryPrivacyGuard(visible: false, risk: true), null, new AllianceMemoryServerDependency(visible: false), persistenceClaim: true, messageSentClaim: true)
            });
            AllianceMemoryDiagnostics allianceDiagnostics = alliance.Evaluate();
            Assert.That(allianceDiagnostics.Contains(AllianceMemoryDiagnosticCode.AllianceMemoryPersistenceForbidden), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMemoryDiagnosticCode.AllianceMemoryPersonalDataRisk), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMemoryDiagnosticCode.AllianceMessageSentClaim), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMemoryDiagnosticCode.AllianceMemoryRouteMissing), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMemoryDiagnosticCode.AllianceMemoryServerDependencyHidden), Is.True);
        }

        [Test]
        public void WorldArmyChoiceFilteringAndGoalBridge_BlockRuntimeAndRewardClaims()
        {
            var world = new WorldEventMemoryMarker("world", WorldMemoryMarkerKind.Threat, string.Empty, new WorldMemoryFreshnessPreview("live", liveClaim: true), null, new WorldLiveClaimGuard(liveClaim: true, rewardClaim: true, actionClaim: true), new WorldMemoryServerDependency(visible: false));
            WorldMemoryDiagnostics worldDiagnostics = world.Evaluate();
            Assert.That(worldDiagnostics.Contains(WorldMemoryDiagnosticCode.WorldMemoryLiveClaim), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMemoryDiagnosticCode.WorldMemoryRewardForbidden), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMemoryDiagnosticCode.WorldMemoryRouteMissing), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMemoryDiagnosticCode.WorldMemoryActionForbidden), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMemoryDiagnosticCode.WorldMemoryServerDependencyHidden), Is.True);

            var army = new ArmyReadinessMemoryRecord("army", null, new DefenseMemoryHint("ready", officialClaim: true), "world", new PvpRiskMemoryNotice("pvp", visible: false), string.Empty, new ArmyMemoryActionGuard(trainingClaim: true, combatClaim: true, lossClaim: true, rewardClaim: true), new ArmyMemoryServerDependency(visible: false));
            ArmyMemoryDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyMemoryDiagnosticCode.ArmyMemoryTrainingClaim), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyMemoryDiagnosticCode.ArmyMemoryCombatClaim), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyMemoryDiagnosticCode.ArmyMemoryLossRewardForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyMemoryDiagnosticCode.ArmyMemoryRouteMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyMemoryDiagnosticCode.ArmyMemoryServerDependencyHidden), Is.True);

            var reflection = new PlayerChoiceReflection("reflection", new[]
            {
                new ChoiceReflectionSignal("style", "defensive", string.Empty, new PlaystyleReflectionHint(PlayerStyleSignalKind.Defensive, "bonus"), null, new ChoiceOfficialClaimGuard(officialChoice: true, bonusClaim: true, matchmakingClaim: true), new ReflectionServerDependency(visible: false))
            });
            ChoiceDiagnostics choiceDiagnostics = reflection.Evaluate();
            Assert.That(choiceDiagnostics.Contains(ChoiceDiagnosticCode.ChoiceOfficialClaimForbidden), Is.True);
            Assert.That(choiceDiagnostics.Contains(ChoiceDiagnosticCode.ReflectionBonusClaim), Is.True);
            Assert.That(choiceDiagnostics.Contains(ChoiceDiagnosticCode.ReflectionMatchmakingClaim), Is.True);
            Assert.That(choiceDiagnostics.Contains(ChoiceDiagnosticCode.ReflectionReturnRouteMissing), Is.True);
            Assert.That(choiceDiagnostics.Contains(ChoiceDiagnosticCode.ReflectionServerDependencyHidden), Is.True);

            var filter = new MemoryJournalFilter(string.Empty, null, 0, new PrivacySafeMemoryView(maskVisible: false, personalDataLeak: true), string.Empty, new MemoryExportClaimGuard(exportBlocked: false, exportOfficialClaim: true), new MemorySearchPreviewBlocker(serverSearchClaim: true), new MemoryJournalServerDependency(visible: false));
            MemoryJournalDiagnostics filterDiagnostics = filter.Evaluate();
            Assert.That(filterDiagnostics.Contains(MemoryJournalDiagnosticCode.MemoryFilterCategoryMissing), Is.True);
            Assert.That(filterDiagnostics.Contains(MemoryJournalDiagnosticCode.MemoryPersonalDataLeak), Is.True);
            Assert.That(filterDiagnostics.Contains(MemoryJournalDiagnosticCode.MemoryExportForbidden), Is.True);
            Assert.That(filterDiagnostics.Contains(MemoryJournalDiagnosticCode.MemorySearchServerClaim), Is.True);
            Assert.That(filterDiagnostics.Contains(MemoryJournalDiagnosticCode.MemoryJournalServerDependencyHidden), Is.True);

            var bridge = new MemoryGoalReturnBridge("bridge", new[]
            {
                new MemoryDerivedGoalPreview("goal", new MemoryGoalSource(string.Empty), "claim", string.Empty, null, new GoalRewardClaimGuard(rewardClaim: true, completionClaim: true), new MemoryGoalServerDependency(visible: false))
            });
            MemoryGoalDiagnostics bridgeDiagnostics = bridge.Evaluate();
            Assert.That(bridgeDiagnostics.Contains(MemoryGoalDiagnosticCode.MemoryGoalSourceMissing), Is.True);
            Assert.That(bridgeDiagnostics.Contains(MemoryGoalDiagnosticCode.MemoryGoalRewardForbidden), Is.True);
            Assert.That(bridgeDiagnostics.Contains(MemoryGoalDiagnosticCode.MemoryGoalCompletionClaim), Is.True);
            Assert.That(bridgeDiagnostics.Contains(MemoryGoalDiagnosticCode.MemoryGoalRouteMissing), Is.True);
            Assert.That(bridgeDiagnostics.Contains(MemoryGoalDiagnosticCode.MemoryGoalServerDependencyHidden), Is.True);
        }

        [Test]
        public void MemoryReadabilityAndClosureGate_BlockBee451PrematureRelease()
        {
            var readability = new MobileMemoryReadabilityCheck("readability", new MemoryTextLengthRule(8), new MemoryIconClarityNeed(clear: false), new[]
            {
                new MemoryCardEvidenceNeed("card", "this memory card text is much too long", sourceVisible: false, routeVisible: false, privacyVisible: false, previewVisible: false, new MemoryDemoEvidenceFrame(nonBlank: false, productionClaim: true), MemoryReadabilityVerdict.BlockedByProductionClaim)
            });
            MemoryReadabilityDiagnostics readabilityDiagnostics = readability.Evaluate();
            Assert.That(readabilityDiagnostics.Contains(MemoryReadabilityDiagnosticCode.MemoryCardTextTooLong), Is.True);
            Assert.That(readabilityDiagnostics.Contains(MemoryReadabilityDiagnosticCode.MemorySourceNotVisible), Is.True);
            Assert.That(readabilityDiagnostics.Contains(MemoryReadabilityDiagnosticCode.MemoryReturnRouteNotVisible), Is.True);
            Assert.That(readabilityDiagnostics.Contains(MemoryReadabilityDiagnosticCode.MemoryDemoEvidenceMissing), Is.True);
            Assert.That(readabilityDiagnostics.Contains(MemoryReadabilityDiagnosticCode.MemoryProductionReadinessClaim), Is.True);

            var gate = new PlayerMemoryNetworkClosureGate("gate", new[]
            {
                new MemoryNetworkCoverageMatrix("BEE-441", string.Empty, string.Empty, string.Empty, "demo", "qa", string.Empty, MemoryDemoEvidenceVerdict.BlockedByMissingMemorySource),
                new MemoryNetworkCoverageMatrix("BEE-447", "privacy", "journal", string.Empty, "demo", "qa", "server", MemoryDemoEvidenceVerdict.BlockedByPrivacyGap),
                new MemoryNetworkCoverageMatrix("BEE-448", "goal", "route", "masked", "demo", "qa", "server", MemoryDemoEvidenceVerdict.BlockedByRewardOrCompletionClaim)
            }, new MemoryOfficialClaimAudit(officialHistoryClaim: true, rewardOrCompletionClaim: true), new MemoryServerBoundaryAudit(visible: false), new Bee451BlockerStatus(prematureAttempt: true, message: "blocked"));
            MemoryClosureDiagnostics closureDiagnostics = gate.Evaluate();
            Assert.That(closureDiagnostics.Contains(MemoryClosureDiagnosticCode.MemoryNetworkSourceGap), Is.True);
            Assert.That(closureDiagnostics.Contains(MemoryClosureDiagnosticCode.MemoryOfficialHistoryClaim), Is.True);
            Assert.That(closureDiagnostics.Contains(MemoryClosureDiagnosticCode.MemoryPrivacyGap), Is.True);
            Assert.That(closureDiagnostics.Contains(MemoryClosureDiagnosticCode.MemoryRewardClaimDetected), Is.True);
            Assert.That(closureDiagnostics.Contains(MemoryClosureDiagnosticCode.Bee451PrematureRelease), Is.True);
            Assert.That(closureDiagnostics.Verdict, Is.EqualTo(MemoryDemoEvidenceVerdict.BlockedByBee451Premature));
        }
    }
}
