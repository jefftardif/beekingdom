using System;
using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class HiveActionReadinessFrameworks471To480Tests
    {
        [Test]
        public void IntentUpgradeConstructionAndProduction_BlockOfficialActions()
        {
            var rail = new HivePlayerActionIntentRail("rail", new[]
            {
                new HivePlayerActionIntent("upgrade", HiveIntentDomain.Upgrade, 0, string.Empty, string.Empty, previewBadgeVisible: false, HiveIntentAuthorityState.PreviewBlocked, serverDependencyVisible: false, opensZoneDetail: false, officialActionClaim: true)
            }, null, HiveIntentRailDisplayMode.Empty);
            HiveIntentRailDiagnostics railDiagnostics = rail.Evaluate();
            Assert.That(railDiagnostics.Contains(HiveIntentRailDiagnosticCode.HiveIntentMissing), Is.True);
            Assert.That(railDiagnostics.Contains(HiveIntentRailDiagnosticCode.HiveIntentPriorityInvalid), Is.True);
            Assert.That(railDiagnostics.Contains(HiveIntentRailDiagnosticCode.HiveIntentPreviewBadgeMissing), Is.True);
            Assert.That(railDiagnostics.Contains(HiveIntentRailDiagnosticCode.HiveIntentOfficialActionClaim), Is.True);
            Assert.That(railDiagnostics.Contains(HiveIntentRailDiagnosticCode.HiveIntentServerDependencyHidden), Is.True);
            Assert.That(railDiagnostics.Contains(HiveIntentRailDiagnosticCode.HiveIntentRouteMissing), Is.True);

            var upgrade = new HiveUpgradeCandidatePreviewPanel("upgrade-panel", new[]
            {
                new HiveUpgradeCandidatePreview("nurserie", string.Empty, string.Empty, string.Empty, new[]
                {
                    new HiveUpgradePreviewRequirement("miel", string.Empty, visible: false, serverDependencyVisible: false, officialCalculationClaim: true)
                }, HiveUpgradePreviewStatus.PreviewAvailable, linkedToIntentRail: false, serverDependencyVisible: false, officialUpgradeClaim: true)
            }, null);
            HiveUpgradeCandidateDiagnostics upgradeDiagnostics = upgrade.Evaluate();
            Assert.That(upgradeDiagnostics.Contains(HiveUpgradeCandidateDiagnosticCode.UpgradeCandidateMissing), Is.True);
            Assert.That(upgradeDiagnostics.Contains(HiveUpgradeCandidateDiagnosticCode.UpgradeRequirementMissing), Is.True);
            Assert.That(upgradeDiagnostics.Contains(HiveUpgradeCandidateDiagnosticCode.UpgradeBenefitMissing), Is.True);
            Assert.That(upgradeDiagnostics.Contains(HiveUpgradeCandidateDiagnosticCode.UpgradeOfficialClaim), Is.True);
            Assert.That(upgradeDiagnostics.Contains(HiveUpgradeCandidateDiagnosticCode.UpgradeServerDependencyHidden), Is.True);
            Assert.That(upgradeDiagnostics.Contains(HiveUpgradeCandidateDiagnosticCode.UpgradeIntentRouteMissing), Is.True);

            var construction = new HiveConstructionPrerequisitePanel("construction", string.Empty, new[]
            {
                new HiveConstructionRequirement("visuel", HiveConstructionRequirementSource.VisualMaturity, HiveConstructionRequirementState.PreviewMissing, string.Empty, visible: false, serverDependencyVisible: false, officialCalculationClaim: true)
            }, linkedToIntentRail: false, constructionStartClaim: true);
            HiveConstructionDiagnostics constructionDiagnostics = construction.Evaluate();
            Assert.That(constructionDiagnostics.Contains(HiveConstructionDiagnosticCode.ConstructionPanelMissing), Is.True);
            Assert.That(constructionDiagnostics.Contains(HiveConstructionDiagnosticCode.ConstructionRequirementMissing), Is.True);
            Assert.That(constructionDiagnostics.Contains(HiveConstructionDiagnosticCode.ConstructionServerUnknownMissing), Is.True);
            Assert.That(constructionDiagnostics.Contains(HiveConstructionDiagnosticCode.ConstructionOfficialClaim), Is.True);
            Assert.That(constructionDiagnostics.Contains(HiveConstructionDiagnosticCode.ConstructionIntentRouteMissing), Is.True);
            Assert.That(constructionDiagnostics.Contains(HiveConstructionDiagnosticCode.ConstructionServerDependencyHidden), Is.True);

            var production = new HiveProductionQueueIntentPreview(string.Empty, new[]
            {
                new HiveProductionIntentSlot("slot-a", string.Empty, string.Empty, string.Empty, missingResource: true, missingRole: true, routeToShortage: false, routeToWorkforce: false, serverDependencyVisible: false, timerClaim: true, collectClaim: true, spendClaim: true, accelerationClaim: true)
            }, HiveProductionQueuePreviewState.PreviewOnly);
            HiveProductionQueueDiagnostics productionDiagnostics = production.Evaluate();
            Assert.That(productionDiagnostics.Contains(HiveProductionQueueDiagnosticCode.ProductionProducerMissing), Is.True);
            Assert.That(productionDiagnostics.Contains(HiveProductionQueueDiagnosticCode.ProductionSlotMissing), Is.True);
            Assert.That(productionDiagnostics.Contains(HiveProductionQueueDiagnosticCode.ProductionPreviewDataMissing), Is.True);
            Assert.That(productionDiagnostics.Contains(HiveProductionQueueDiagnosticCode.ProductionOfficialClaim), Is.True);
            Assert.That(productionDiagnostics.Contains(HiveProductionQueueDiagnosticCode.ProductionShortageRouteMissing), Is.True);
            Assert.That(productionDiagnostics.Contains(HiveProductionQueueDiagnosticCode.ProductionWorkforceRouteMissing), Is.True);
            Assert.That(productionDiagnostics.Contains(HiveProductionQueueDiagnosticCode.ProductionServerDependencyHidden), Is.True);
        }

        [Test]
        public void ShortageWorkforceDefenseAndResearch_BlockEconomyAssignmentCombatAndProgression()
        {
            var shortage = new HiveResourceShortageResolutionPath(string.Empty, string.Empty, new[]
            {
                new HiveResourceResolutionOption("storage", HiveResourceResolutionKind.ImproveStorage, string.Empty, HiveResolutionAvailability.PreviewRouteAvailable, "BEE-471", serverDependencyVisible: false, transactionClaim: true)
            });
            HiveResourceShortageDiagnostics shortageDiagnostics = shortage.Evaluate();
            Assert.That(shortageDiagnostics.Contains(HiveResourceShortageDiagnosticCode.ShortageMissing), Is.True);
            Assert.That(shortageDiagnostics.Contains(HiveResourceShortageDiagnosticCode.ResolutionOptionMissing), Is.True);
            Assert.That(shortageDiagnostics.Contains(HiveResourceShortageDiagnosticCode.ResolutionTransactionClaim), Is.True);
            Assert.That(shortageDiagnostics.Contains(HiveResourceShortageDiagnosticCode.ResolutionRouteMissing), Is.True);
            Assert.That(shortageDiagnostics.Contains(HiveResourceShortageDiagnosticCode.ResolutionServerDependencyHidden), Is.True);

            var workforce = new HiveWorkforcePreparationPlanner("workforce", new[]
            {
                new HiveWorkforceNeedPreview("ouvriere", string.Empty, HiveWorkforceNeedCategory.Production, 0, string.Empty, HiveWorkforceNeedStatus.PreviewNeed, serverDependencyVisible: false, assignmentClaim: true, trainingClaim: true)
            }, new HiveWorkforceCoverageSummary(string.Empty, visible: false, officialPopulationClaim: true));
            HiveWorkforceDiagnostics workforceDiagnostics = workforce.Evaluate();
            Assert.That(workforceDiagnostics.Contains(HiveWorkforceDiagnosticCode.WorkforceNeedMissing), Is.True);
            Assert.That(workforceDiagnostics.Contains(HiveWorkforceDiagnosticCode.WorkforceSeverityMissing), Is.True);
            Assert.That(workforceDiagnostics.Contains(HiveWorkforceDiagnosticCode.WorkforceAssignmentClaim), Is.True);
            Assert.That(workforceDiagnostics.Contains(HiveWorkforceDiagnosticCode.WorkforcePopulationOfficialClaim), Is.True);
            Assert.That(workforceDiagnostics.Contains(HiveWorkforceDiagnosticCode.WorkforceServerDependencyHidden), Is.True);

            var defense = new HiveDefenseReadinessSnapshot("defense", new[]
            {
                new HiveDefenseReadinessItem("defense", string.Empty, string.Empty, HiveDefenseItemState.PreviewNeed, serverDependencyVisible: false, liveActionClaim: true)
            }, HiveDefenseReadinessLevel.Fragile, Array.Empty<string>());
            HiveDefenseDiagnostics defenseDiagnostics = defense.Evaluate();
            Assert.That(defenseDiagnostics.Contains(HiveDefenseDiagnosticCode.DefenseZoneMissing), Is.True);
            Assert.That(defenseDiagnostics.Contains(HiveDefenseDiagnosticCode.DefenseRoleNeedMissing), Is.True);
            Assert.That(defenseDiagnostics.Contains(HiveDefenseDiagnosticCode.DefenseLiveActionClaim), Is.True);
            Assert.That(defenseDiagnostics.Contains(HiveDefenseDiagnosticCode.DefenseServerBoundaryMissing), Is.True);
            Assert.That(defenseDiagnostics.Contains(HiveDefenseDiagnosticCode.DefenseServerDependencyHidden), Is.True);

            var research = new HiveResearchGeneticsChoicePreview("research", new[]
            {
                new HiveStrategicChoicePreview("research-a", HiveStrategicChoiceDomain.Research, string.Empty, string.Empty, HiveChoiceAuthorityState.PreviewOnly, string.Empty, serverDependencyVisible: false, activationClaim: true, officialBonusClaim: true)
            }, null);
            HiveResearchGeneticsDiagnostics researchDiagnostics = research.Evaluate();
            Assert.That(researchDiagnostics.Contains(HiveResearchGeneticsDiagnosticCode.StrategicChoiceMissing), Is.True);
            Assert.That(researchDiagnostics.Contains(HiveResearchGeneticsDiagnosticCode.ResearchGeneticsDomainMissing), Is.True);
            Assert.That(researchDiagnostics.Contains(HiveResearchGeneticsDiagnosticCode.StrategicChoiceBenefitMissing), Is.True);
            Assert.That(researchDiagnostics.Contains(HiveResearchGeneticsDiagnosticCode.StrategicChoiceOfficialClaim), Is.True);
            Assert.That(researchDiagnostics.Contains(HiveResearchGeneticsDiagnosticCode.StrategicChoiceServerDependencyHidden), Is.True);
        }

        [Test]
        public void SessionFlowAndClosureGate_BlockRewardsAndBee481PrematureRelease()
        {
            var flow = new HiveMobileSessionGoalFlow("flow", new[]
            {
                new HiveSessionGoalStep("open", string.Empty, "BEE-471", HiveSessionStepStatus.Viewed, serverDependencyVisible: false)
            }, HiveSessionGoalState.TargetExperiencePreview, new HiveSessionExitSummary(string.Empty, visible: false, persistentClaim: true, rewardClaim: true, streakClaim: true));
            HiveSessionFlowDiagnostics flowDiagnostics = flow.Evaluate();
            Assert.That(flowDiagnostics.Contains(HiveSessionFlowDiagnosticCode.SessionStepCountInvalid), Is.True);
            Assert.That(flowDiagnostics.Contains(HiveSessionFlowDiagnosticCode.SessionRequiredStepMissing), Is.True);
            Assert.That(flowDiagnostics.Contains(HiveSessionFlowDiagnosticCode.SessionInstructionMissing), Is.True);
            Assert.That(flowDiagnostics.Contains(HiveSessionFlowDiagnosticCode.SessionRewardClaim), Is.True);
            Assert.That(flowDiagnostics.Contains(HiveSessionFlowDiagnosticCode.SessionPersistentSummaryClaim), Is.True);
            Assert.That(flowDiagnostics.Contains(HiveSessionFlowDiagnosticCode.SessionServerDependencyHidden), Is.True);

            var officialGate = new HiveActionReadinessClosureGate("official", CoverageWithMutation(row => row.BeeId == "BEE-474" ? new HiveActionReadinessCoverageRow(row.BeeId, row.Surface, HiveActionReadinessCoverageStatus.OfficialActionActive, row.EvidenceSource, officialActionActive: true, hiddenServerDependency: false, demoPathVisible: true) : row), Array.Empty<HiveActionReadinessReserve>(), Bee481BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(officialGate.Verdict, Is.EqualTo(HiveActionReadinessVerdict.BlockedByOfficialAction));

            var hiddenServerGate = new HiveActionReadinessClosureGate("hidden", CoverageWithMutation(row => row.BeeId == "BEE-477" ? new HiveActionReadinessCoverageRow(row.BeeId, row.Surface, HiveActionReadinessCoverageStatus.HiddenServerDependency, row.EvidenceSource, officialActionActive: false, hiddenServerDependency: true, demoPathVisible: true) : row), Array.Empty<HiveActionReadinessReserve>(), Bee481BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(hiddenServerGate.Verdict, Is.EqualTo(HiveActionReadinessVerdict.BlockedByHiddenServerDependency));

            var missingDemoGate = new HiveActionReadinessClosureGate("demo", CoverageWithMutation(row => row.BeeId == "BEE-479" ? new HiveActionReadinessCoverageRow(row.BeeId, row.Surface, HiveActionReadinessCoverageStatus.MissingDemoPath, row.EvidenceSource, officialActionActive: false, hiddenServerDependency: false, demoPathVisible: false) : row), Array.Empty<HiveActionReadinessReserve>(), Bee481BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(missingDemoGate.Verdict, Is.EqualTo(HiveActionReadinessVerdict.BlockedByMissingDemoPath));

            var prematureGate = new HiveActionReadinessClosureGate("premature", ValidCoverage(), Array.Empty<HiveActionReadinessReserve>(), Bee481BlockerStatus.ReleasedByFutureArchitectDecision);
            Assert.That(prematureGate.Verdict, Is.EqualTo(HiveActionReadinessVerdict.BlockedByBee481Premature));

            var readyGate = new HiveActionReadinessClosureGate("ready", ValidCoverage(), new[] { new HiveActionReadinessReserve("qa-global", "Global suite still has historical failures outside this lot.") }, Bee481BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(readyGate.Verdict, Is.EqualTo(HiveActionReadinessVerdict.ReadyWithReserves));
        }

        private static IReadOnlyList<HiveActionReadinessCoverageRow> ValidCoverage()
        {
            var rows = new List<HiveActionReadinessCoverageRow>();
            for (int bee = 471; bee <= 479; bee++)
            {
                rows.Add(new HiveActionReadinessCoverageRow("BEE-" + bee, "Surface " + bee, HiveActionReadinessCoverageStatus.Covered, "SandboxPlayground", officialActionActive: false, hiddenServerDependency: false, demoPathVisible: true));
            }

            return rows;
        }

        private static IReadOnlyList<HiveActionReadinessCoverageRow> CoverageWithMutation(Func<HiveActionReadinessCoverageRow, HiveActionReadinessCoverageRow> mutate)
        {
            var rows = new List<HiveActionReadinessCoverageRow>();
            foreach (HiveActionReadinessCoverageRow row in ValidCoverage())
            {
                rows.Add(mutate(row));
            }

            return rows;
        }
    }
}
