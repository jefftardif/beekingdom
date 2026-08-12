using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class ScaleOperationsEntryFrameworks401To410Tests
    {
        [Test]
        public void InventoryNavigationSurfaceAndAssets_BlockConfusingOperationalClaims()
        {
            var inventory = new ScaleOperationsSourceInventory("inventory", new[]
            {
                new OperationsSourceItem(string.Empty, string.Empty, "BEE-351-400", string.Empty, OperationsSourceFreshness.Unknown, new[] { OperationsSourceLimit.Hidden }, OperationsSourceMobileStatus.Missing, OperationsSourceServerDependency.Blocked, telemetryProductionRequested: true, uiSurfaceConfused: true)
            });
            OperationsSourceDiagnostics inventoryDiagnostics = inventory.Evaluate();
            Assert.That(inventoryDiagnostics.Contains(OperationsSourceDiagnosticCode.OperationsSourceMissing), Is.True);
            Assert.That(inventoryDiagnostics.Contains(OperationsSourceDiagnosticCode.OperationsSourceOwnerMissing), Is.True);
            Assert.That(inventoryDiagnostics.Contains(OperationsSourceDiagnosticCode.OperationsSourceLimitHidden), Is.True);
            Assert.That(inventoryDiagnostics.Contains(OperationsSourceDiagnosticCode.OperationsSourceTelemetryProductionForbidden), Is.True);
            Assert.That(inventoryDiagnostics.Contains(OperationsSourceDiagnosticCode.OperationsSourceUiSurfaceConfused), Is.True);

            var shell = new MobileNavigationShellContract("shell", new[]
            {
                new MobileNavSurface("hive", MobileNavSurfaceKind.Player, null, null, Array.Empty<string>(), new[] { "admin" }, new MobileNavConstraints(24, productionFinalClaim: true), new MobileNavActiveState(false), new MobileNavSurfaceBoundary(playerToAdminRoute: true, confused: true))
            });
            MobileNavigationDiagnostics shellDiagnostics = shell.Evaluate();
            Assert.That(shellDiagnostics.Contains(MobileNavDiagnosticCode.MobileNavRouteMissing), Is.True);
            Assert.That(shellDiagnostics.Contains(MobileNavDiagnosticCode.MobileNavDeadEndDetected), Is.True);
            Assert.That(shellDiagnostics.Contains(MobileNavDiagnosticCode.MobileNavSurfaceBoundaryConfused), Is.True);
            Assert.That(shellDiagnostics.Contains(MobileNavDiagnosticCode.MobileNavTouchTargetTooSmall), Is.True);
            Assert.That(shellDiagnostics.Contains(MobileNavDiagnosticCode.MobileNavProductionFinalClaimForbidden), Is.True);

            var separation = new DemoProductionSurfaceSeparation("surfaces", new[]
            {
                new SurfaceClassification(string.Empty, SurfaceClass.MixedForbidden, SurfaceAudience.ForbiddenMixed, new SurfaceVisualMarker("marker", visible: false), Array.Empty<string>(), new[] { new SurfaceForbiddenRoute("admin", exposedToPlayer: true) }, new SurfacePromiseGuard(productionPromiseClaimed: true, finalThemeClaimed: true)),
                new SurfaceClassification("admin", SurfaceClass.ServerAdmin, SurfaceAudience.Player, null, Array.Empty<string>(), Array.Empty<SurfaceForbiddenRoute>(), new SurfacePromiseGuard())
            });
            SurfaceSeparationDiagnostics separationDiagnostics = separation.Evaluate();
            Assert.That(separationDiagnostics.Contains(SurfaceSeparationDiagnosticCode.SurfaceClassificationMissing), Is.True);
            Assert.That(separationDiagnostics.Contains(SurfaceSeparationDiagnosticCode.SurfaceMixedForbidden), Is.True);
            Assert.That(separationDiagnostics.Contains(SurfaceSeparationDiagnosticCode.SurfaceAdminExposedToPlayer), Is.True);
            Assert.That(separationDiagnostics.Contains(SurfaceSeparationDiagnosticCode.SurfaceProductionPromiseForbidden), Is.True);
            Assert.That(separationDiagnostics.Contains(SurfaceSeparationDiagnosticCode.SurfaceVisualMarkerMissing), Is.True);

            var assets = new ProfessionalAssetReadinessRegistry("assets", new[]
            {
                new AssetReadinessItem("asset", null, AssetUsageSurface.Demo, "128x128", string.Empty, AssetReadinessStatus.Temporary, null, new[] { new AssetProductionReadinessBlocker("missing-final-art", open: true) }, finalClaim: true)
            });
            AssetReadinessDiagnostics assetDiagnostics = assets.Evaluate();
            Assert.That(assetDiagnostics.Contains(AssetReadinessDiagnosticCode.AssetReadinessMissingCategory), Is.True);
            Assert.That(assetDiagnostics.Contains(AssetReadinessDiagnosticCode.AssetTemporaryMarkerMissing), Is.True);
            Assert.That(assetDiagnostics.Contains(AssetReadinessDiagnosticCode.AssetFinalClaimForbidden), Is.True);
            Assert.That(assetDiagnostics.Contains(AssetReadinessDiagnosticCode.AssetMobileUsageMissing), Is.True);
            Assert.That(assetDiagnostics.Contains(AssetReadinessDiagnosticCode.AssetProductionBlockerOpen), Is.True);
        }

        [Test]
        public void HubSocialArmyAndWorld_BlockRuntimeAndServerAuthoritativeClaims()
        {
            var hub = new SocialMmoHomeHubUxContract("hub", new[]
            {
                new HomeHubZone("hive", 1, HomeHubTargetSurface.Hive, new HomeHubPrimaryAction("upgrade", runtimeAction: true), new HomeHubStatusBadge("ready", misleading: true), new HomeHubRuntimeLimitMarker("demo only", visible: true), string.Empty, mobileOverlap: true, marketingTextDetected: true)
            }, Array.Empty<HomeHubAlertSlot>());
            HomeHubDiagnostics hubDiagnostics = hub.Evaluate();
            Assert.That(hubDiagnostics.Contains(HomeHubDiagnosticCode.HomeHubZoneMissing), Is.True);
            Assert.That(hubDiagnostics.Contains(HomeHubDiagnosticCode.HomeHubRuntimeActionForbidden), Is.True);
            Assert.That(hubDiagnostics.Contains(HomeHubDiagnosticCode.HomeHubBadgeMisleading), Is.True);
            Assert.That(hubDiagnostics.Contains(HomeHubDiagnosticCode.HomeHubMobileOverlapDetected), Is.True);
            Assert.That(hubDiagnostics.Contains(HomeHubDiagnosticCode.HomeHubMarketingTextDetected), Is.True);

            var social = new AllianceChatMobileEntryContract("social", new[]
            {
                new ChatChannelPreview("world", ChatChannelKind.World, SocialEntryTab.WorldChat, new UnreadStatePreview(4, official: true), new ModerationVisibleLimit("preview", finalClaim: true), new SocialSearchFutureMarker(runtimeSearchRequested: true), runtimeBlocked: false)
            });
            SocialEntryDiagnostics socialDiagnostics = social.Evaluate();
            Assert.That(socialDiagnostics.Contains(SocialEntryDiagnosticCode.SocialEntryTabMissing), Is.True);
            Assert.That(socialDiagnostics.Contains(SocialEntryDiagnosticCode.ChatLiveActivationForbidden), Is.True);
            Assert.That(socialDiagnostics.Contains(SocialEntryDiagnosticCode.UnreadStateOfficialForbidden), Is.True);
            Assert.That(socialDiagnostics.Contains(SocialEntryDiagnosticCode.ModerationFinalClaimForbidden), Is.True);
            Assert.That(socialDiagnostics.Contains(SocialEntryDiagnosticCode.SocialSearchRuntimeForbidden), Is.True);

            var army = new ArmyWarReadinessMobileEntry("army", Array.Empty<ArmyUnitCardPreview>(), new[]
            {
                new WarActionIntentPreview("attack", WarActionIntentKind.InspectAttackOptions, new WarServerAuthorityBlocker(required: true, visible: false), null, new BeginnerProtectionMarker(false), runtimeBlocked: false, Array.Empty<string>(), combatRuntimeRequested: true, officialScoreClaimed: true)
            });
            ArmyWarDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyWarDiagnosticCode.ArmyEntryUnitCardMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyWarDiagnosticCode.WarRuntimeCombatForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyWarDiagnosticCode.ArmyOfficialScoreForbidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyWarDiagnosticCode.PvpProtectionHidden), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyWarDiagnosticCode.WarServerAuthorityRequired), Is.True);

            var world = new WorldMapEventJournalUxContract("world", new[]
            {
                new WorldMapMarkerPreview(string.Empty, WorldMapMarkerKind.TerritoryPreview, runtimeLimitVisible: false, officialTerritory: true)
            }, new WorldMapFilterSet(Array.Empty<string>()), new[]
            {
                new EventJournalEntryPreview("event", EventJournalPreviewType.WorldEvent, Array.Empty<string>(), null, new WorldEventRuntimeLimit(visible: false, liveEventRequested: true), OperationsSourceServerDependency.ServerAuthoritative, 1)
            }, new[] { new WorldMapZoomRule("zoom", performanceLimitVisible: false) });
            WorldMapDiagnostics worldDiagnostics = world.Evaluate();
            Assert.That(worldDiagnostics.Contains(WorldMapDiagnosticCode.WorldMapMarkerMissing), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMapDiagnosticCode.WorldMapEventLiveForbidden), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMapDiagnosticCode.WorldMapTerritoryOfficialForbidden), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMapDiagnosticCode.WorldMapPerformanceLimitHidden), Is.True);
            Assert.That(worldDiagnostics.Contains(WorldMapDiagnosticCode.WorldJournalFilterMissing), Is.True);
        }

        [Test]
        public void ReadabilityAndClosureGate_BlockBee411PrematureAndRuntimeClaims()
        {
            var readability = new OperationsReadabilityAccessibilityGate("readability", new[]
            {
                new ReadabilityCriterion("criterion", "home", requiredState: false, "too small", "mobile", demoBlocking: true, textSizePx: 10, overlap: true, contrastAcceptable: false, touchTargetPixels: 20, localizationRisk: true, finalPolishClaim: true)
            });
            ReadabilityGateDiagnostics readabilityDiagnostics = readability.Evaluate();
            Assert.That(readabilityDiagnostics.Verdict, Is.EqualTo(ReadabilityGateVerdict.BlockedByFinalPolishClaim));
            Assert.That(readabilityDiagnostics.Contains(ReadabilityGateDiagnosticCode.ReadabilityTextTooSmall), Is.True);
            Assert.That(readabilityDiagnostics.Contains(ReadabilityGateDiagnosticCode.ReadabilityOverlapDetected), Is.True);
            Assert.That(readabilityDiagnostics.Contains(ReadabilityGateDiagnosticCode.AccessibilityContrastInsufficient), Is.True);
            Assert.That(readabilityDiagnostics.Contains(ReadabilityGateDiagnosticCode.TouchTargetTooSmall), Is.True);
            Assert.That(readabilityDiagnostics.Contains(ReadabilityGateDiagnosticCode.LocalizationRiskOpen), Is.True);
            Assert.That(readabilityDiagnostics.Contains(ReadabilityGateDiagnosticCode.FinalPolishClaimForbidden), Is.True);

            var closure = new ScaleOperationsEntryClosureGate("closure", null, new ScaleOperationsEntryCoverage(mobileGapOpen: true, surfaceConfusionOpen: true, assetGapOpen: true, runtimeClaimDetected: true), new[] { new ScaleOperationsEntryBlocker("runtime", runtimeClaim: true) }, new Bee411BlockerStatusForScale(Bee411BlockerState.AttemptBlocked, ScaleOperationsEntryClosureGate.Bee411BlockedMessage));
            ScaleOperationsEntryDiagnostics closureDiagnostics = closure.Evaluate();
            Assert.That(closureDiagnostics.Verdict, Is.EqualTo(ScaleOperationsEntryVerdict.BlockedByBee411Premature));
            Assert.That(closureDiagnostics.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryInputMissing), Is.True);
            Assert.That(closureDiagnostics.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryMobileGapOpen), Is.True);
            Assert.That(closureDiagnostics.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntrySurfaceConfusionOpen), Is.True);
            Assert.That(closureDiagnostics.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryAssetGapOpen), Is.True);
            Assert.That(closureDiagnostics.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryRuntimeClaimDetected), Is.True);
            Assert.That(closureDiagnostics.Contains(ScaleOperationsEntryDiagnosticCode.Bee411Premature), Is.True);
        }
    }
}
