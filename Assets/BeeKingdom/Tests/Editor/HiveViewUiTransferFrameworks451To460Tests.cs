using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class HiveViewUiTransferFrameworks451To460Tests
    {
        [Test]
        public void FoundationSpatialBuildingAndHud_BlockFinalUiAndEconomyClaims()
        {
            var transfer = new HiveViewUiTransfer("transfer", new[]
            {
                new HiveViewZoneDefinition("nurserie", string.Empty, string.Empty, Array.Empty<HiveViewDataNeed>(), new[] { new HiveViewVisualState("dense", readable: false) }, new[] { new HiveViewInteractionContract("build", defined: false, runtimeAction: true) }, HiveViewServerDependency.EconomyFuture, serverDependencyVisible: false, productionClaim: true, mobileReadabilityRisk: true)
            });
            HiveViewDiagnostics transferDiagnostics = transfer.Evaluate();
            Assert.That(transferDiagnostics.Contains(HiveViewDiagnosticCode.HiveViewZoneMissing), Is.True);
            Assert.That(transferDiagnostics.Contains(HiveViewDiagnosticCode.HiveViewInteractionUndefined), Is.True);
            Assert.That(transferDiagnostics.Contains(HiveViewDiagnosticCode.HiveViewServerDependencyHidden), Is.True);
            Assert.That(transferDiagnostics.Contains(HiveViewDiagnosticCode.HiveViewMobileReadabilityRisk), Is.True);
            Assert.That(transferDiagnostics.Contains(HiveViewDiagnosticCode.HiveViewProductionClaim), Is.True);

            var spatial = new HiveSpatialZoneMap("map", new[]
            {
                new HiveLayerDefinition("centre-reine", string.Empty, Array.Empty<string>(), null, new HiveZoomReadabilityState(readable: false, risk: true), readabilityRisk: true, null, new[] { new HiveCellCluster("c", overlapRisk: true) }, finalLayoutClaim: true)
            });
            HiveSpatialDiagnostics spatialDiagnostics = spatial.Evaluate();
            Assert.That(spatialDiagnostics.Contains(HiveSpatialDiagnosticCode.HiveLayerMissing), Is.True);
            Assert.That(spatialDiagnostics.Contains(HiveSpatialDiagnosticCode.HiveZoneOverlapRisk), Is.True);
            Assert.That(spatialDiagnostics.Contains(HiveSpatialDiagnosticCode.HiveZoomReadabilityRisk), Is.True);
            Assert.That(spatialDiagnostics.Contains(HiveSpatialDiagnosticCode.HiveDensityStageMissing), Is.True);
            Assert.That(spatialDiagnostics.Contains(HiveSpatialDiagnosticCode.HiveLayerServerDependencyHidden), Is.True);

            var selection = new HiveBuildingSelection("selection", new HiveBuildingDetailPanel(string.Empty, string.Empty, string.Empty, 1, string.Empty, string.Empty, string.Empty, new[] { new HiveBuildingActionPreview("upgrade", runtimeForbidden: true) }, new HiveBuildingPrerequisiteNotice(string.Empty, visible: false), new HiveBuildingServerDependency(visible: false)), selectionVisible: false, exitVisible: false);
            HiveBuildingDiagnostics selectionDiagnostics = selection.Evaluate();
            Assert.That(selectionDiagnostics.Contains(HiveBuildingDiagnosticCode.HiveBuildingSelectionMissing), Is.True);
            Assert.That(selectionDiagnostics.Contains(HiveBuildingDiagnosticCode.HiveBuildingDetailDataMissing), Is.True);
            Assert.That(selectionDiagnostics.Contains(HiveBuildingDiagnosticCode.HiveBuildingActionRuntimeForbidden), Is.True);
            Assert.That(selectionDiagnostics.Contains(HiveBuildingDiagnosticCode.HiveBuildingPrerequisiteHidden), Is.True);
            Assert.That(selectionDiagnostics.Contains(HiveBuildingDiagnosticCode.HiveBuildingServerDependencyHidden), Is.True);

            var hud = new HiveResourceHudPreview("hud", new[] { new HiveResourceStateNotice("official", officialClaim: true) }, new[]
            {
                new HiveProductionQueuePreview("queue", HiveResourceKind.Honey, "honey", "running", null, new HiveAccelerationPreviewBlocker(accelerationClaim: true, blockedVisible: false), new HiveEconomyServerDependency(visible: false), runtimeQueueClaim: true)
            });
            HiveProductionDiagnostics hudDiagnostics = hud.Evaluate();
            Assert.That(hudDiagnostics.Contains(HiveProductionDiagnosticCode.HiveResourceOfficialClaim), Is.True);
            Assert.That(hudDiagnostics.Contains(HiveProductionDiagnosticCode.HiveProductionQueueRuntimeForbidden), Is.True);
            Assert.That(hudDiagnostics.Contains(HiveProductionDiagnosticCode.HiveAccelerationForbidden), Is.True);
            Assert.That(hudDiagnostics.Contains(HiveProductionDiagnosticCode.HiveCapacityMissing), Is.True);
            Assert.That(hudDiagnostics.Contains(HiveProductionDiagnosticCode.HiveEconomyServerDependencyHidden), Is.True);
        }

        [Test]
        public void PopulationProgressionAlertsControlsAndAssets_BlockRuntimeAndPolishClaims()
        {
            var population = new HiveBeePopulationView("population", new[] { new BeeRoleAvailability(BeeRoleKind.Worker, visible: true) }, new[]
            {
                new BeeAssignmentPreview(BeeRoleKind.Worker, "nurserie", 4, 2, new HivePopulationCapacityNotice(string.Empty, visible: false), new BeeAssignmentActionBlocker(runtimeAssignmentClaim: true, blockedVisible: false), new BeePopulationServerDependency(visible: false), officialCountClaim: true)
            });
            BeePopulationDiagnostics populationDiagnostics = population.Evaluate();
            Assert.That(populationDiagnostics.Contains(BeePopulationDiagnosticCode.BeePopulationOfficialClaim), Is.True);
            Assert.That(populationDiagnostics.Contains(BeePopulationDiagnosticCode.BeeAssignmentRuntimeForbidden), Is.True);
            Assert.That(populationDiagnostics.Contains(BeePopulationDiagnosticCode.BeeRoleMissing), Is.True);
            Assert.That(populationDiagnostics.Contains(BeePopulationDiagnosticCode.BeeCapacityHidden), Is.True);
            Assert.That(populationDiagnostics.Contains(BeePopulationDiagnosticCode.BeePopulationServerDependencyHidden), Is.True);

            var stages = new HiveVisualProgressionStageSet("stages", new[]
            {
                new HiveVisualProgressionStage("early", HiveVisualStageKind.Early, new HiveStageDensityRule("dense", readabilityRisk: true), new HiveStageUnlockPreview("level up", officialUnlockClaim: true, rewardClaim: true), new[] { new HiveStageAssetNeed("queen", available: false) }, new HiveStageVisualNeed("too dense", readable: false), new HiveProgressionServerDependency(visible: false))
            });
            HiveStageDiagnostics stageDiagnostics = stages.Evaluate();
            Assert.That(stageDiagnostics.Contains(HiveStageDiagnosticCode.HiveStageMissing), Is.True);
            Assert.That(stageDiagnostics.Contains(HiveStageDiagnosticCode.HiveProgressionOfficialClaim), Is.True);
            Assert.That(stageDiagnostics.Contains(HiveStageDiagnosticCode.HiveStageReadabilityRisk), Is.True);
            Assert.That(stageDiagnostics.Contains(HiveStageDiagnosticCode.HiveStageAssetGap), Is.True);
            Assert.That(stageDiagnostics.Contains(HiveStageDiagnosticCode.HiveProgressionServerDependencyHidden), Is.True);

            var alert = new HiveAlertPreview("alert", null, HiveAlertSeverityPreview.MisleadingUrgent, string.Empty, null, new HiveAlertActionBlocker(liveClaim: true, officialActionClaim: true), new HiveAlertServerDependency(visible: false));
            HiveAlertDiagnostics alertDiagnostics = alert.Evaluate();
            Assert.That(alertDiagnostics.Contains(HiveAlertDiagnosticCode.HiveAlertLiveClaim), Is.True);
            Assert.That(alertDiagnostics.Contains(HiveAlertDiagnosticCode.HiveAlertRouteMissing), Is.True);
            Assert.That(alertDiagnostics.Contains(HiveAlertDiagnosticCode.HiveAlertSeverityMisleading), Is.True);
            Assert.That(alertDiagnostics.Contains(HiveAlertDiagnosticCode.HiveAlertActionForbidden), Is.True);
            Assert.That(alertDiagnostics.Contains(HiveAlertDiagnosticCode.HiveAlertServerDependencyHidden), Is.True);

            var viewport = new HiveMobileViewportControl("viewport", null, new HiveZoomLevelNeed(readable: false, risk: true), new[] { new HiveDisplayFilter("prod", "production", HiveFilterKind.Production, visibleState: true, mobileReadabilityNeed: false, new HiveViewportAccessibilityNeed(visible: false, certificationClaim: true), finalGestureClaimBlocked: false) }, new HiveFocusResetAction(visible: false), finalGestureClaim: true);
            HiveViewportDiagnostics viewportDiagnostics = viewport.Evaluate();
            Assert.That(viewportDiagnostics.Contains(HiveViewportDiagnosticCode.HivePanControlMissing), Is.True);
            Assert.That(viewportDiagnostics.Contains(HiveViewportDiagnosticCode.HiveZoomReadabilityRisk), Is.True);
            Assert.That(viewportDiagnostics.Contains(HiveViewportDiagnosticCode.HiveFilterMissing), Is.True);
            Assert.That(viewportDiagnostics.Contains(HiveViewportDiagnosticCode.HiveFocusResetMissing), Is.True);
            Assert.That(viewportDiagnostics.Contains(HiveViewportDiagnosticCode.HiveGestureFinalClaim), Is.True);

            var assets = new HiveUiAssetRequirementRegistry("assets", new[]
            {
                new HiveUiAssetRequirement("icon", HiveAssetCategory.ZoneIcon, "zone", new HiveAssetPlaceholderPolicy(temporaryMarked: false), accessibilityConcern: true, string.Empty, new HivePolishClaimGuard(finalAssetClaim: true, polishProductionClaim: true))
            }, new[] { new HiveAnimationRequirement("pulse", intrusive: true) }, new[] { new HiveSoundRequirement("click", accessibilityRisk: true) }, Array.Empty<HiveFeedbackEffectNeed>());
            HiveAssetDiagnostics assetDiagnostics = assets.Evaluate();
            Assert.That(assetDiagnostics.Contains(HiveAssetDiagnosticCode.HiveAssetRequirementMissing), Is.True);
            Assert.That(assetDiagnostics.Contains(HiveAssetDiagnosticCode.HivePlaceholderNotMarked), Is.True);
            Assert.That(assetDiagnostics.Contains(HiveAssetDiagnosticCode.HiveFinalAssetClaim), Is.True);
            Assert.That(assetDiagnostics.Contains(HiveAssetDiagnosticCode.HiveSoundAccessibilityRisk), Is.True);
            Assert.That(assetDiagnostics.Contains(HiveAssetDiagnosticCode.HivePolishProductionClaim), Is.True);
        }

        [Test]
        public void ClosureGate_BlocksBee461PrematureReleaseAndMissingArch057Coverage()
        {
            var gate = new HiveViewUiTransferClosureGate("gate", new[]
            {
                new HiveViewCoverageMatrix("BEE-451", string.Empty, string.Empty, string.Empty, "qa", string.Empty, HiveViewTransferVerdict.BlockedByMissingHiveZone),
                new HiveViewCoverageMatrix("BEE-459", "assets", "assets", "demo", "qa", "server", HiveViewTransferVerdict.BlockedByProductionClaim),
                new HiveViewCoverageMatrix("BEE-454", "economy", "hud", "demo", "qa", string.Empty, HiveViewTransferVerdict.BlockedByHiddenServerDependency)
            }, new HiveViewArch057Compliance(zonesCovered: false, interactionsCovered: false, visualStatesCovered: false, dataCovered: false, assetsCovered: false), new HiveViewDemoEvidenceNeed(visible: false), new HiveViewServerBoundaryAudit(visible: false), new Bee461BlockerStatus(prematureAttempt: true, message: "blocked"));
            HiveViewClosureDiagnostics diagnostics = gate.Evaluate();
            Assert.That(diagnostics.Contains(HiveViewClosureDiagnosticCode.HiveViewCoverageGap), Is.True);
            Assert.That(diagnostics.Contains(HiveViewClosureDiagnosticCode.Arch057RequirementMissing), Is.True);
            Assert.That(diagnostics.Contains(HiveViewClosureDiagnosticCode.HiveViewProductionClaim), Is.True);
            Assert.That(diagnostics.Contains(HiveViewClosureDiagnosticCode.HiveViewServerBoundaryHidden), Is.True);
            Assert.That(diagnostics.Contains(HiveViewClosureDiagnosticCode.Bee461PrematureRelease), Is.True);
            Assert.That(diagnostics.Verdict, Is.EqualTo(HiveViewTransferVerdict.BlockedByBee461Premature));
        }
    }
}
