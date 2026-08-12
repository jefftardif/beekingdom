using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class ColonyIntegrationReadinessFrameworks251To260Tests
    {
        [Test]
        public void BoundaryMap_ReportsOwnerDirectMutationManagerReplacementAndUnseededRandomness()
        {
            var map = new ColonyIntegrationBoundaryMap(new[]
            {
                new ColonyIntegrationBoundary(
                    ColonyIntegrationDomain.Population,
                    ColonyIntegrationDomain.World,
                    null,
                    new ColonyIntegrationInterface("direct-pop-world", ColonyIntegrationDirection.ForbiddenDirectMutation, "LegacyManager"),
                    managerReplacementRequested: true,
                    unseededRandomnessRequested: true)
            });

            ColonyIntegrationDiagnostics diagnostics = map.Evaluate();

            Assert.That(diagnostics.Contains(ColonyIntegrationDiagnosticCode.BoundaryOwnerMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationDiagnosticCode.DirectMutationDetected), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationDiagnosticCode.ManagerReplacementRequested), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationDiagnosticCode.UnseededRandomnessRequested), Is.True);
        }

        [Test]
        public void PopulationWorldContext_SortsInfluencesAndFlagsMissingWorldMutationAndLegacyConflict()
        {
            var context = new ColonyPopulationWorldContext(
                "colony-a",
                "region-a",
                string.Empty,
                42,
                new[]
                {
                    new PopulationWorldInfluence(PopulationWorldInfluenceKind.DangerLevel, 0.8d, "danger", "world"),
                    new PopulationWorldInfluence(PopulationWorldInfluenceKind.SeasonPressure, 0.3d, "season", string.Empty, false)
                },
                populationMutationRequested: true,
                legacyManagerConflict: true);

            PopulationWorldLinkDiagnostics diagnostics = context.Evaluate();

            Assert.That(context.Influences[0].Kind, Is.EqualTo(PopulationWorldInfluenceKind.SeasonPressure));
            Assert.That(diagnostics.Verdict, Is.EqualTo(PopulationWorldLinkVerdict.WorldContextMissing));
            Assert.That(diagnostics.Contains(PopulationWorldLinkDiagnosticCode.WorldSnapshotMissing), Is.True);
            Assert.That(diagnostics.Contains(PopulationWorldLinkDiagnosticCode.PopulationMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(PopulationWorldLinkDiagnosticCode.UnseededInfluence), Is.True);
            Assert.That(diagnostics.Contains(PopulationWorldLinkDiagnosticCode.LegacyManagerConflict), Is.True);
        }

        [Test]
        public void AIWorldIntentDiagnostics_SortsIntentsAndBlocksPathfindingOrTaskMutation()
        {
            var diagnostics = AIWorldIntentDiagnostics.Evaluate(new[]
            {
                new ColonyAIWorldIntent(AIWorldIntentKind.ObserveOnly, "region-b", "source-b", null, 2, priorityIsSeeded: false),
                new ColonyAIWorldIntent(AIWorldIntentKind.ExploreRegion, "region-a", "source-a", AIWorldIntentReason.VisibilityGap, 9, requiresPathfinding: true, directTaskMutationRequested: true)
            });

            Assert.That(diagnostics.Intents[0].Priority, Is.EqualTo(9));
            Assert.That(diagnostics.Verdict, Is.EqualTo(AIWorldIntentVerdict.IntentForbidden));
            Assert.That(diagnostics.Contains(AIWorldIntentDiagnosticCode.IntentReasonMissing), Is.True);
            Assert.That(diagnostics.Contains(AIWorldIntentDiagnosticCode.PathfindingOutOfScope), Is.True);
            Assert.That(diagnostics.Contains(AIWorldIntentDiagnosticCode.DirectTaskMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(AIWorldIntentDiagnosticCode.UnseededIntentPriority), Is.True);
        }

        [Test]
        public void ConstructionWorldFootprint_BlocksPlacementValidatorBypassAndScenePlacement()
        {
            var footprint = new ColonyConstructionWorldFootprint(
                "project-a",
                "region-a",
                new[] { new ConstructionFootprintConstraintEntry(ConstructionFootprintConstraint.BiomeConstraint, 4, "biome", isKnown: false) },
                new[] { new ConstructionExpansionDependency("external-route", ColonyIntegrationDomain.World, isKnown: false) },
                placementMutationRequested: true,
                constructionValidatorBypassed: true,
                scenePlacementRequested: true);

            ConstructionWorldFootprintDiagnostics diagnostics = footprint.Evaluate();

            Assert.That(diagnostics.Verdict, Is.EqualTo(ConstructionWorldFootprintVerdict.WorldConstraintMissing));
            Assert.That(diagnostics.Contains(ConstructionWorldFootprintDiagnosticCode.PlacementMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(ConstructionWorldFootprintDiagnosticCode.ConstructionValidatorBypassed), Is.True);
            Assert.That(diagnostics.Contains(ConstructionWorldFootprintDiagnosticCode.ScenePlacementRequested), Is.True);
            Assert.That(diagnostics.Contains(ConstructionWorldFootprintDiagnosticCode.ExpansionDependencyUnknown), Is.True);
        }

        [Test]
        public void ResourceLogisticsWorldLink_FlagsInventoryReservationPathfindingAndMissingSource()
        {
            var link = new ColonyResourceLogisticsWorldLink(
                new WorldResourceAvailability("nectar", "region-a", 0d, hasSource: false),
                100d,
                new[] { LogisticsPressure.ShortageRisk },
                new ConceptualRoute("route-a", "region-a", "colony-a", 12d, physicalPathRequested: true),
                new[] { LogisticsWorldRisk.QuantityConservationRisk },
                inventoryMutationRequested: true,
                reservationMutationRequested: true);

            LogisticsWorldDiagnostics diagnostics = link.Evaluate();

            Assert.That(diagnostics.Contains(LogisticsWorldDiagnosticCode.ResourceSourceMissing), Is.True);
            Assert.That(diagnostics.Contains(LogisticsWorldDiagnosticCode.InventoryMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(LogisticsWorldDiagnosticCode.ReservationMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(LogisticsWorldDiagnosticCode.PathfindingOutOfScope), Is.True);
            Assert.That(diagnostics.Contains(LogisticsWorldDiagnosticCode.QuantityConservationRisk), Is.True);
        }

        [Test]
        public void DefenseAlertDiagnostics_BlocksCombatMutationMissingSeverityAndUnseededThreat()
        {
            DefenseAlertDiagnostics diagnostics = DefenseAlertDiagnostics.Evaluate(new[]
            {
                new ColonyDefenseWorldAlert(null, "region-a", "gate", DefenseAlertSeverity.Missing, DefenseReadinessStatus.CombatOutOfScope, combatSimulationRequested: true, defenseMutationRequested: true, threatRollSeeded: false)
            });

            Assert.That(diagnostics.Contains(DefenseAlertDiagnosticCode.DangerSourceMissing), Is.True);
            Assert.That(diagnostics.Contains(DefenseAlertDiagnosticCode.AlertSeverityMissing), Is.True);
            Assert.That(diagnostics.Contains(DefenseAlertDiagnosticCode.CombatSimulationRequested), Is.True);
            Assert.That(diagnostics.Contains(DefenseAlertDiagnosticCode.DefenseMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(DefenseAlertDiagnosticCode.UnseededThreatRoll), Is.True);
        }

        [Test]
        public void StrategyFeedbackDiagnostics_ReportsMissingSourcesAutoDecisionConfidenceAndContradictions()
        {
            ColonyStrategyFeedbackDiagnostics diagnostics = ColonyStrategyFeedbackDiagnostics.Evaluate(new[]
            {
                new ColonyStrategyFeedback(
                    StrategyFeedbackRecommendation.DefenseReadiness,
                    new StrategyFeedbackSource[0],
                    StrategyFeedbackConfidence.Missing,
                    new[] { StrategyFeedbackLimit.ForecastOutOfScope, StrategyFeedbackLimit.ContradictorySignals },
                    StrategyFeedbackStatus.ForbiddenAutoDecision,
                    decisionMutationRequested: true)
            });

            Assert.That(diagnostics.Contains(StrategyFeedbackDiagnosticCode.FeedbackSourceMissing), Is.True);
            Assert.That(diagnostics.Contains(StrategyFeedbackDiagnosticCode.DecisionMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(StrategyFeedbackDiagnosticCode.ConfidenceMissing), Is.True);
            Assert.That(diagnostics.Contains(StrategyFeedbackDiagnosticCode.ForecastOutOfScope), Is.True);
            Assert.That(diagnostics.Contains(StrategyFeedbackDiagnosticCode.ContradictorySignals), Is.True);
        }

        [Test]
        public void EmergencyPropagationDiagnostics_DetectsCyclesDestructiveEffectsUnexplainedEdgesAndConflicts()
        {
            var propagation = new ColonyEmergencyPropagation(
                "emergency-a",
                new[]
                {
                    new EmergencyPropagationNodeState(EmergencyPropagationNode.WorldEvent, EmergencyPropagationSeverity.High),
                    new EmergencyPropagationNodeState(EmergencyPropagationNode.DefenseAlert, EmergencyPropagationSeverity.Conflict)
                },
                new[]
                {
                    new EmergencyPropagationEdge(EmergencyPropagationNode.WorldEvent, EmergencyPropagationNode.DefenseAlert, "BEE-256", "alert", EmergencyPropagationSeverity.High),
                    new EmergencyPropagationEdge(EmergencyPropagationNode.DefenseAlert, EmergencyPropagationNode.WorldEvent, string.Empty, string.Empty, EmergencyPropagationSeverity.Conflict)
                },
                destructiveEffectRequested: true);

            EmergencyPropagationDiagnostics diagnostics = propagation.Evaluate();

            Assert.That(diagnostics.Contains(EmergencyPropagationDiagnosticCode.PropagationCycleDetected), Is.True);
            Assert.That(diagnostics.Contains(EmergencyPropagationDiagnosticCode.DestructiveEffectRequested), Is.True);
            Assert.That(diagnostics.Contains(EmergencyPropagationDiagnosticCode.UnexplainedEdge), Is.True);
            Assert.That(diagnostics.Contains(EmergencyPropagationDiagnosticCode.SeverityConflict), Is.True);
        }

        [Test]
        public void DemoReadModelDiagnostics_FlagsMissingSourcesGameplayLogicMutationLimitsAndEvidence()
        {
            var readModel = new ColonyIntegrationDemoReadModel("colony-a", new[]
            {
                new ColonyIntegrationDemoSectionState(
                    ColonyIntegrationDemoSection.BoundaryMap,
                    ColonyIntegrationDemoBadge.Blocked,
                    string.Empty,
                    new ColonyIntegrationDemoEvidence[0],
                    new[] { "blocked" },
                    new string[0],
                    gameplayLogicDetected: true,
                    demoMutationRequested: true)
            });

            ColonyIntegrationDemoReadModelDiagnostics diagnostics = readModel.Evaluate();

            Assert.That(diagnostics.Contains(ColonyIntegrationDemoDiagnosticCode.DemoSectionSourceMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationDemoDiagnosticCode.GameplayLogicDetected), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationDemoDiagnosticCode.DemoMutationRequested), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationDemoDiagnosticCode.LimitMissing), Is.True);
            Assert.That(diagnostics.Contains(ColonyIntegrationDemoDiagnosticCode.EvidenceMissing), Is.True);
        }

        [Test]
        public void ReadinessGate_ReturnsExpectedVerdicts()
        {
            var readyGate = new ColonyIntegrationReadinessGate(ValidCriteria());
            Assert.That(readyGate.Evaluate().Verdict, Is.EqualTo(ColonyIntegrationReadinessVerdict.IntegrationReadyForReview));

            var bee261Gate = new ColonyIntegrationReadinessGate(ValidCriteria(), bee261Referenced: true);
            ColonyIntegrationReadinessDiagnostics bee261 = bee261Gate.Evaluate();
            Assert.That(bee261.Verdict, Is.EqualTo(ColonyIntegrationReadinessVerdict.BlockedByBee261Premature));
            Assert.That(bee261.Contains(ColonyIntegrationReadinessDiagnosticCode.Bee261Premature), Is.True);

            var directMutationGate = new ColonyIntegrationReadinessGate(new[]
            {
                new ColonyIntegrationReadinessCriterion("BEE-251", true, "evidence", directDomainMutation: true)
            });
            Assert.That(directMutationGate.Evaluate().Verdict, Is.EqualTo(ColonyIntegrationReadinessVerdict.BlockedByDirectMutation));

            var demoGapGate = new ColonyIntegrationReadinessGate(new[]
            {
                new ColonyIntegrationReadinessCriterion("BEE-259", true, "evidence", demoImpactDeclared: false)
            });
            Assert.That(demoGapGate.Evaluate().Verdict, Is.EqualTo(ColonyIntegrationReadinessVerdict.BlockedByDemoGap));
        }

        private static IReadOnlyList<ColonyIntegrationReadinessCriterion> ValidCriteria()
        {
            return new[]
            {
                new ColonyIntegrationReadinessCriterion("BEE-251", true, "boundary map"),
                new ColonyIntegrationReadinessCriterion("BEE-252", true, "population world"),
                new ColonyIntegrationReadinessCriterion("BEE-253", true, "ai intents"),
                new ColonyIntegrationReadinessCriterion("BEE-254", true, "construction footprint"),
                new ColonyIntegrationReadinessCriterion("BEE-255", true, "resource logistics"),
                new ColonyIntegrationReadinessCriterion("BEE-256", true, "defense alerts"),
                new ColonyIntegrationReadinessCriterion("BEE-257", true, "strategy feedback"),
                new ColonyIntegrationReadinessCriterion("BEE-258", true, "emergency propagation"),
                new ColonyIntegrationReadinessCriterion("BEE-259", true, "demo read model")
            };
        }
    }
}
