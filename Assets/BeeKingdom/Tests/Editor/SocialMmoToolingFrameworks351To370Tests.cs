using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class SocialMmoToolingFrameworks351To370Tests
    {
        [Test]
        public void PlaygroundToolingInventoryBindingsAndVisualizations_BlockRuntimeClaims()
        {
            var inventory = new SocialMmoPlaygroundReadModelInventory("inventory", new[]
            {
                new SocialMmoPlaygroundReadModelEntry(string.Empty, string.Empty, Array.Empty<SocialMmoProductPillar>(), new SocialMmoReadModelOwner(string.Empty), SocialMmoReadModelStatus.ServerBlocked, Array.Empty<SocialMmoReadModelGap>(), new[] { new SocialMmoReadModelLimit("limit", mutationRequested: true) }, null)
            });
            PlaygroundReadModelDiagnostics inventoryDiagnostics = inventory.Evaluate();
            Assert.That(inventoryDiagnostics.Contains(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelMissing), Is.True);
            Assert.That(inventoryDiagnostics.Contains(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelOwnerMissing), Is.True);
            Assert.That(inventoryDiagnostics.Contains(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelSourceMissing), Is.True);
            Assert.That(inventoryDiagnostics.Contains(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelMutationForbidden), Is.True);
            Assert.That(inventoryDiagnostics.Contains(PlaygroundReadModelDiagnosticCode.PlaygroundReadModelServerDependencyOpen), Is.True);

            var binding = new AllianceCooperationVisualizationBinding("binding", string.Empty, Array.Empty<AllianceObjectiveVisualBinding>(), new[] { new ContributionVisualBinding("c", string.Empty, AllianceCooperationVisualStatus.Projected, rewardClaimed: true) }, new[] { new MissionVisualBinding("m", "mission", AllianceCooperationVisualStatus.Projected, mutationRequested: true) }, Array.Empty<HelpRequestVisualBinding>(), new[] { new AllianceCooperationVisualLimit("ui", uiFinalClaimed: true) });
            AllianceCooperationVisualizationDiagnostics bindingDiagnostics = binding.Evaluate();
            Assert.That(bindingDiagnostics.Contains(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationVisualSourceMissing), Is.True);
            Assert.That(bindingDiagnostics.Contains(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationUiFinalForbidden), Is.True);
            Assert.That(bindingDiagnostics.Contains(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationGameplayMutationForbidden), Is.True);
            Assert.That(bindingDiagnostics.Contains(AllianceCooperationVisualizationDiagnosticCode.AllianceCooperationRewardClaimForbidden), Is.True);

            var army = new ArmyReadinessVisualizationContract("army", Array.Empty<string>(), new[] { new ArmyReadinessVisualSignal("signal", ArmyReadinessVisualLevel.High, officialStatClaimed: true) }, new ArmyCompositionVisualSummary("summary", combatPowerClaimed: true), Array.Empty<ArmyRiskVisualWarning>(), new[] { new ArmyServerDependencyVisualMarker("server", visible: false) }, Array.Empty<ArmyVisualizationLimit>());
            ArmyReadinessVisualizationDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyReadinessVisualizationDiagnosticCode.ArmyVisualizationInputMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessVisualizationDiagnosticCode.ArmyOfficialStatForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessVisualizationDiagnosticCode.ArmyCombatPowerVisualizationForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessVisualizationDiagnosticCode.ArmyRiskWarningMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyReadinessVisualizationDiagnosticCode.ArmyServerDependencyHidden), Is.True);
        }

        [Test]
        public void DebugHandoffModerationFixturesDrilldownAndClosure_BlockServerBypassAndBee361()
        {
            var panel = new PvPFairnessDebugPanelContract("panel", Array.Empty<PvPFairnessDebugScenarioRow>(), new[] { new PvPFairnessThresholdProjection("threshold", nonFinalBalance: false) }, Array.Empty<PvPRecoveryDebugSignal>(), Array.Empty<PvPHarassmentWarning>(), new[] { new PvPFairnessDebugLimit("limit", enforcementRequested: true, matchmakingClaimed: true) });
            PvPFairnessDebugPanelDiagnostics panelDiagnostics = panel.Evaluate();
            Assert.That(panelDiagnostics.Contains(PvPFairnessDebugDiagnosticCode.PvPFairnessDebugInputMissing), Is.True);
            Assert.That(panelDiagnostics.Contains(PvPFairnessDebugDiagnosticCode.PvPFairnessThresholdFinalClaimForbidden), Is.True);
            Assert.That(panelDiagnostics.Contains(PvPFairnessDebugDiagnosticCode.PvPEnforcementRuntimeForbidden), Is.True);
            Assert.That(panelDiagnostics.Contains(PvPFairnessDebugDiagnosticCode.PvPMatchmakingDebugClaimForbidden), Is.True);
            Assert.That(panelDiagnostics.Contains(PvPFairnessDebugDiagnosticCode.PvPHarassmentWarningMissing), Is.True);

            var queue = new SocialServerHandoffQueue("queue", new[] { new SocialServerHandoffQueueItem("item", "BEE-355", "server", SocialServerHandoffPriority.Missing, null, string.Empty, SocialServerHandoffScanStatus.Missing, Array.Empty<SocialServerHandoffBlocker>()) }, new[] { new SocialServerHandoffLimit("limit", server018Requested: true, runtimeRequested: true) });
            SocialServerHandoffQueueDiagnostics queueDiagnostics = queue.Evaluate();
            Assert.That(queueDiagnostics.Contains(SocialServerHandoffDiagnosticCode.SocialServerPriorityMissing), Is.True);
            Assert.That(queueDiagnostics.Contains(SocialServerHandoffDiagnosticCode.SocialServerOwnerMissing), Is.True);
            Assert.That(queueDiagnostics.Contains(SocialServerHandoffDiagnosticCode.SocialServerScanStatusMissing), Is.True);
            Assert.That(queueDiagnostics.Contains(SocialServerHandoffDiagnosticCode.Server018CreationForbidden), Is.True);
            Assert.That(queueDiagnostics.Contains(SocialServerHandoffDiagnosticCode.SocialServerHandoffRuntimeForbidden), Is.True);

            var moderation = new ModerationAbuseTriageToolBoundary("mod", new[] { new ModerationAbuseTriageCaseProjection(string.Empty, "harassment", "BEE-356", null, Array.Empty<ModerationPrivacyConstraint>(), Array.Empty<ModerationTriageRisk>(), ModerationTriageStatus.NeedsServerModeration, new[] { new ModerationTriageLimit("limit", sanctionRequested: true, officialStorageRequested: true, runtimeToolRequested: true) }) });
            ModerationAbuseTriageDiagnostics moderationDiagnostics = moderation.Evaluate();
            Assert.That(moderationDiagnostics.Contains(ModerationTriageDiagnosticCode.ModerationTriageCaseMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationTriageDiagnosticCode.ModerationEvidenceExpectationMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationTriageDiagnosticCode.ModerationPrivacyConstraintMissing), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationTriageDiagnosticCode.ModerationSanctionForbidden), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationTriageDiagnosticCode.ModerationOfficialStorageForbidden), Is.True);
            Assert.That(moderationDiagnostics.Contains(ModerationTriageDiagnosticCode.ModerationRuntimeToolForbidden), Is.True);

            var fixtures = new AllianceWarScenarioFixtureCatalog("fixtures", new[] { new AllianceWarScenarioFixture(string.Empty, "rally", Array.Empty<string>(), new[] { new WarFixturePrerequisite("pre", missing: true) }, Array.Empty<WarFixtureRisk>(), null, new[] { new WarFixtureRuntimeLimit("limit", runtimeExecutionRequested: true, rewardRequested: true) }, new[] { "war" }) });
            AllianceWarScenarioFixtureDiagnostics fixtureDiagnostics = fixtures.Evaluate();
            Assert.That(fixtureDiagnostics.Contains(WarFixtureDiagnosticCode.WarFixtureMissing), Is.True);
            Assert.That(fixtureDiagnostics.Contains(WarFixtureDiagnosticCode.WarFixturePrerequisiteMissing), Is.True);
            Assert.That(fixtureDiagnostics.Contains(WarFixtureDiagnosticCode.WarFixtureRiskMissing), Is.True);
            Assert.That(fixtureDiagnostics.Contains(WarFixtureDiagnosticCode.WarFixtureRuntimeExecutionForbidden), Is.True);
            Assert.That(fixtureDiagnostics.Contains(WarFixtureDiagnosticCode.WarFixtureRewardForbidden), Is.True);
            Assert.That(fixtureDiagnostics.Contains(WarFixtureDiagnosticCode.WarFixtureServerAuthorityRequired), Is.True);

            var drilldown = new SocialMmoEvidenceDrilldown("drill", new[] { new SocialMmoEvidenceNode(string.Empty, "proof", "BEE-358", new SocialMmoEvidenceOwner(string.Empty), Array.Empty<SocialMmoEvidenceLink>(), new[] { new SocialMmoEvidenceContradiction("contradiction", open: true) }, new[] { new SocialMmoEvidenceDrilldownLimit("limit", autoCorrectionRequested: true, localTruthClaimed: true) }) });
            SocialMmoEvidenceDrilldownDiagnostics drillDiagnostics = drilldown.Evaluate();
            Assert.That(drillDiagnostics.Contains(EvidenceDrilldownDiagnosticCode.EvidenceDrilldownNodeMissing), Is.True);
            Assert.That(drillDiagnostics.Contains(EvidenceDrilldownDiagnosticCode.EvidenceDrilldownOwnerMissing), Is.True);
            Assert.That(drillDiagnostics.Contains(EvidenceDrilldownDiagnosticCode.EvidenceContradictionOpen), Is.True);
            Assert.That(drillDiagnostics.Contains(EvidenceDrilldownDiagnosticCode.EvidenceAutoCorrectionForbidden), Is.True);
            Assert.That(drillDiagnostics.Contains(EvidenceDrilldownDiagnosticCode.EvidenceLocalTruthForbidden), Is.True);

            var riskGate = new SocialMmoToolingRiskGate("risk", Array.Empty<SocialMmoToolingRiskInput>(), new[] { new SocialMmoToolingRisk("risk", localTruth: true, runtimeClaim: true, serverBypass: true) }, new[] { new SocialMmoToolingBlocker("hidden", hiddenGap: true) }, Array.Empty<SocialMmoToolingWarning>());
            SocialMmoToolingRiskGateDiagnostics riskDiagnostics = riskGate.Evaluate();
            Assert.That(riskDiagnostics.Verdict, Is.EqualTo(SocialMmoToolingVerdict.BlockedByServerBypass));

            var closure = new SocialMmoPlaygroundToolingClosureGate("closure", null, new SocialMmoToolingCoverage(hiddenGapOpen: true, demoHonestyGapOpen: true, runtimeForbiddenRequested: true), new[] { new SocialMmoToolingGap("server", serverGapOpen: true) }, new SocialMmoToolingOwnerMap(serverOwnerPresent: false, demoOwnerPresent: false), new Bee361BlockerStatus(prematureAttempt: true, SocialMmoPlaygroundToolingClosureGate.Bee361BlockedMessage));
            SocialMmoPlaygroundToolingClosureDiagnostics closureDiagnostics = closure.Evaluate();
            Assert.That(closureDiagnostics.Verdict, Is.EqualTo(SocialMmoToolingClosureVerdict.BlockedByBee361Premature));
            Assert.That(closureDiagnostics.Contains(ToolingClosureDiagnosticCode.ToolingClosureRuntimeForbidden), Is.True);
        }

        [Test]
        public void QaLiveOpsPermissionsScenarioAndGate_BlockProductionTelemetryAndBee371()
        {
            var qa = new SocialMmoQaIntakeMatrix("qa", new[] { new SocialMmoQaIntakeEntry("entry", string.Empty, SocialMmoProductPillar.PvP, SocialMmoQaEvidenceStatus.GapOpen, new[] { new SocialMmoQaOpenRisk("risk", classified: false) }, new SocialMmoQaOwner(string.Empty), new[] { new SocialMmoQaRuntimeLimit("limit", runtimeClaimed: true, finalValidationClaimed: true) }) });
            SocialMmoQaIntakeDiagnostics qaDiagnostics = qa.Evaluate();
            Assert.That(qaDiagnostics.Contains(QaIntakeDiagnosticCode.QaIntakeSourceMissing), Is.True);
            Assert.That(qaDiagnostics.Contains(QaIntakeDiagnosticCode.QaIntakeOwnerMissing), Is.True);
            Assert.That(qaDiagnostics.Contains(QaIntakeDiagnosticCode.QaIntakeRiskUnclassified), Is.True);
            Assert.That(qaDiagnostics.Contains(QaIntakeDiagnosticCode.QaIntakeRuntimeClaimForbidden), Is.True);
            Assert.That(qaDiagnostics.Contains(QaIntakeDiagnosticCode.QaIntakeFinalValidationForbidden), Is.True);

            var telemetry = new PlaygroundSocialSignalTelemetryContract("telemetry", new[] { new PlaygroundSocialSignal("signal", "activity", null, null, Array.Empty<SocialSignalPrivacyLimit>(), productionTelemetryEnabled: true, new[] { "analytics" }) });
            PlaygroundSocialSignalTelemetryDiagnostics telemetryDiagnostics = telemetry.Evaluate();
            Assert.That(telemetryDiagnostics.Contains(SocialSignalDiagnosticCode.SocialSignalOriginMissing), Is.True);
            Assert.That(telemetryDiagnostics.Contains(SocialSignalDiagnosticCode.SocialSignalFreshnessMissing), Is.True);
            Assert.That(telemetryDiagnostics.Contains(SocialSignalDiagnosticCode.SocialSignalPrivacyLimitMissing), Is.True);
            Assert.That(telemetryDiagnostics.Contains(SocialSignalDiagnosticCode.ProductionTelemetryForbidden), Is.True);
            Assert.That(telemetryDiagnostics.Contains(SocialSignalDiagnosticCode.SocialSignalServerAuthorityRequired), Is.True);

            var dashboard = new AllianceActivityHealthDashboardBoundary(string.Empty, Array.Empty<AllianceActivityHealthSignal>(), new[] { new AllianceActivityHealthAlert("official", AllianceActivityAlertLevel.OfficialVerdict, officialScoreClaimed: true) }, new[] { new AllianceActivityMissingData("data", open: true) }, new[] { new AllianceActivityPressureRisk("pressure", open: true) }, new[] { new AllianceActivityDashboardLimit("limit", progressionRuntimeClaimed: true) });
            AllianceActivityHealthDashboardDiagnostics dashboardDiagnostics = dashboard.Evaluate();
            Assert.That(dashboardDiagnostics.Contains(AllianceActivityDiagnosticCode.AllianceActivitySignalMissing), Is.True);
            Assert.That(dashboardDiagnostics.Contains(AllianceActivityDiagnosticCode.AllianceActivityOfficialScoreForbidden), Is.True);

            var balance = new ArmyPvPBalanceSignalCatalog("balance", new[] { new ArmyPvPBalanceSignal("signal", "payToWin", string.Empty, "risk", "qa", "server", officialPowerAllowed: true) }, Array.Empty<PayToWinRiskSignal>(), Array.Empty<RecoveryBalanceSignal>());
            ArmyPvPBalanceSignalDiagnostics balanceDiagnostics = balance.Evaluate();
            Assert.That(balanceDiagnostics.Contains(ArmyPvpBalanceDiagnosticCode.BalanceSignalSourceMissing), Is.True);
            Assert.That(balanceDiagnostics.Contains(ArmyPvpBalanceDiagnosticCode.OfficialPowerCalculationForbidden), Is.True);
            Assert.That(balanceDiagnostics.Contains(ArmyPvpBalanceDiagnosticCode.PayToWinSignalMissing), Is.True);
            Assert.That(balanceDiagnostics.Contains(ArmyPvpBalanceDiagnosticCode.RecoveryBalanceSignalMissing), Is.True);
            Assert.That(balanceDiagnostics.Contains(ArmyPvpBalanceDiagnosticCode.BalanceServerAuthorityRequired), Is.True);

            var abuse = new SocialAbuseEarlyWarningContract("abuse", new[] { new SocialAbuseWarningSignal(string.Empty, "revenge", "BEE-365", new SocialAbuseWarningConfidence(1), null, null, runtimeEnforcementAllowed: true, sanctionRequested: true, new SocialAbuseServerAuthorityTopic("moderation", serverRequired: true)) });
            SocialAbuseEarlyWarningDiagnostics abuseDiagnostics = abuse.Evaluate();
            Assert.That(abuseDiagnostics.Contains(SocialAbuseDiagnosticCode.AbuseWarningSignalMissing), Is.True);
            Assert.That(abuseDiagnostics.Contains(SocialAbuseDiagnosticCode.AbuseWarningPrivacyMissing), Is.True);
            Assert.That(abuseDiagnostics.Contains(SocialAbuseDiagnosticCode.AbuseWarningFalsePositiveRiskMissing), Is.True);
            Assert.That(abuseDiagnostics.Contains(SocialAbuseDiagnosticCode.AbuseSanctionForbidden), Is.True);
            Assert.That(abuseDiagnostics.Contains(SocialAbuseDiagnosticCode.AbuseRuntimeEnforcementForbidden), Is.True);
            Assert.That(abuseDiagnostics.Contains(SocialAbuseDiagnosticCode.AbuseServerAuthorityRequired), Is.True);
        }

        [Test]
        public void LiveOpsCompetitionPermissionsHandoffAndQaGate_BlockFinalOperations()
        {
            var liveOps = new LiveOpsEventCandidateBoundary("liveops", new[] { new LiveOpsEventCandidate(string.Empty, "week", Array.Empty<SocialMmoProductPillar>(), null, "value", Array.Empty<LiveOpsEventRisk>(), LiveOpsEventCandidateStatus.CandidateOnly, new LiveOpsEventRewardBlocker(rewardRequested: true, calendarRequested: true, monetizationRequested: true, rankingRequested: true), new[] { new LiveOpsEventServerAuthorityTopic("calendar", serverRequired: true) }) });
            LiveOpsEventCandidateDiagnostics liveOpsDiagnostics = liveOps.Evaluate();
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsDiagnosticCode.LiveOpsCandidateMissing), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsDiagnosticCode.LiveOpsRewardForbidden), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsDiagnosticCode.LiveOpsCalendarForbidden), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsDiagnosticCode.LiveOpsMonetizationForbidden), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsDiagnosticCode.LiveOpsRankingForbidden), Is.True);
            Assert.That(liveOpsDiagnostics.Contains(LiveOpsDiagnosticCode.LiveOpsServerAuthorityRequired), Is.True);

            var competition = new AllianceCompetitionReadinessProjection("competition", Array.Empty<AllianceCompetitionCondition>(), Array.Empty<AllianceCompetitionFairnessCheck>(), Array.Empty<AllianceCompetitionAbuseGuard>(), new[] { new AllianceCompetitionMissingInput("input") }, AllianceCompetitionReadinessVerdict.ReadyForDesignReview, Array.Empty<AllianceCompetitionServerAuthorityTopic>(), rankingRequested: true, matchmakingRequested: true, rewardRequested: true);
            AllianceCompetitionReadinessDiagnostics competitionDiagnostics = competition.Evaluate();
            Assert.That(competitionDiagnostics.Contains(AllianceCompetitionDiagnosticCode.CompetitionConditionMissing), Is.True);
            Assert.That(competitionDiagnostics.Contains(AllianceCompetitionDiagnosticCode.CompetitionFairnessCheckMissing), Is.True);
            Assert.That(competitionDiagnostics.Contains(AllianceCompetitionDiagnosticCode.CompetitionAbuseGuardMissing), Is.True);
            Assert.That(competitionDiagnostics.Contains(AllianceCompetitionDiagnosticCode.CompetitionRankingForbidden), Is.True);
            Assert.That(competitionDiagnostics.Contains(AllianceCompetitionDiagnosticCode.CompetitionMatchmakingForbidden), Is.True);
            Assert.That(competitionDiagnostics.Contains(AllianceCompetitionDiagnosticCode.CompetitionRewardForbidden), Is.True);

            var permissions = new SocialMmoToolPermissionBoundary("permissions", new[] { new SocialMmoToolPermission(string.Empty, SocialMmoToolRoleProjection.WorkerImplementer, string.Empty, readOnly: false, exportAllowed: true, mutationAllowed: true, "server") }, new[] { new SocialMmoToolForbiddenAction("ban", sanctionRequested: true, serverOverrideRequested: true) }, new[] { new SocialMmoToolLocalTruthRisk("truth", open: true) });
            SocialMmoToolPermissionDiagnostics permissionDiagnostics = permissions.Evaluate();
            Assert.That(permissionDiagnostics.Contains(ToolPermissionDiagnosticCode.ToolPermissionImplicit), Is.True);
            Assert.That(permissionDiagnostics.Contains(ToolPermissionDiagnosticCode.ToolMutationForbidden), Is.True);
            Assert.That(permissionDiagnostics.Contains(ToolPermissionDiagnosticCode.ToolSanctionForbidden), Is.True);
            Assert.That(permissionDiagnostics.Contains(ToolPermissionDiagnosticCode.ToolServerOverrideForbidden), Is.True);
            Assert.That(permissionDiagnostics.Contains(ToolPermissionDiagnosticCode.ToolLocalTruthRiskOpen), Is.True);

            var handoff = new SocialMmoQaScenarioHandoffBundle("handoff", new[] { new SocialMmoQaScenarioHandoffItem(string.Empty, "abuse", Array.Empty<string>(), Array.Empty<SocialMmoScenarioEvidenceLink>(), new[] { new SocialMmoScenarioRuntimeLimit("runtime", runtimeExecutionRequested: true) }, new SocialMmoScenarioOwnerMap(workerOwner: false, qaOwner: false, demoOwner: false, serverOwner: false), SocialMmoScenarioHandoffVerdict.BlockedByRuntimeLimit) });
            SocialMmoQaScenarioHandoffDiagnostics handoffDiagnostics = handoff.Evaluate();
            Assert.That(handoffDiagnostics.Contains(QaScenarioHandoffDiagnosticCode.QaScenarioHandoffItemMissing), Is.True);
            Assert.That(handoffDiagnostics.Contains(QaScenarioHandoffDiagnosticCode.QaScenarioEvidenceMissing), Is.True);
            Assert.That(handoffDiagnostics.Contains(QaScenarioHandoffDiagnosticCode.QaScenarioOwnerMissing), Is.True);
            Assert.That(handoffDiagnostics.Contains(QaScenarioHandoffDiagnosticCode.QaScenarioRuntimeExecutionForbidden), Is.True);
            Assert.That(handoffDiagnostics.Contains(QaScenarioHandoffDiagnosticCode.QaScenarioServerDependencyMissing), Is.True);

            var gate = new SocialMmoQaToolingReadinessGate("gate", null, new SocialMmoQaToolingCoverage(runtimeClaim: true, privacyRisk: true, liveOpsFinalClaim: true), new[] { new SocialMmoQaToolingRisk("risk", open: true) }, new[] { new SocialMmoQaToolingBlocker("server", serverAuthorityGap: true) }, new Bee371BlockerStatus(prematureAttempt: true, SocialMmoQaToolingReadinessGate.Bee371BlockedMessage));
            SocialMmoQaToolingReadinessDiagnostics gateDiagnostics = gate.Evaluate();
            Assert.That(gateDiagnostics.Verdict, Is.EqualTo(SocialMmoQaToolingVerdict.BlockedByBee371Premature));
            Assert.That(gateDiagnostics.Contains(QaToolingDiagnosticCode.QaToolingLiveOpsFinalForbidden), Is.True);
        }
    }
}
