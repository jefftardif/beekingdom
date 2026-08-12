using BeeKingdom.World;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldMapProductionizationFrameworks781To800Tests
    {
        [Test]
        public void IntakeCoversArch172ReservesWithoutLiveClaims()
        {
            WorldMapProductionizationIntake intake = WorldMapProductionizationNoClaimGuard.CreateDefaultIntake();

            Assert.That(intake.IsValidNonLiveIntake(), Is.True);
            Assert.That(intake.ReadOnly, Is.True);
            Assert.That(intake.NonLive, Is.True);
            Assert.That(intake.ProductionPublishAllowed, Is.False);
            Assert.That(intake.Arch172ReservesCovered, Does.Contain("SelectionSuppressionManifestInconsistency"));
            Assert.That(intake.Arch172ReservesCovered, Does.Contain("HitZoneMatrixIncomplete"));
            Assert.That(intake.Arch172ReservesCovered, Does.Contain("AutomatedArch166TestsNotYetImplemented"));
            Assert.That(WorldMapProductionizationNoClaimGuard.Allows("Monde en preparation. Donnees non officielles."), Is.True);
            Assert.That(WorldMapProductionizationNoClaimGuard.Allows("production published live world"), Is.False);
        }

        [Test]
        public void AtlasTileRegionReadinessComputesVisibleTiles()
        {
            WorldMapAtlasTileRegionReadiness readiness = WorldMapAtlasTileRegionReadiness.CreateDefaultPreview();

            var visible = readiness.ComputeVisibleTiles(new Rect(0.25f, 0.25f, 0.35f, 0.35f), 1, 1);

            Assert.That(readiness.CoordinateSpace, Is.EqualTo("world-normalized"));
            Assert.That(readiness.Regions.Count, Is.EqualTo(12));
            Assert.That(readiness.Tiles.Count, Is.GreaterThan(0));
            Assert.That(visible.Count, Is.GreaterThan(0));
            Assert.That(visible.Count, Is.LessThanOrEqualTo(readiness.MaxResidentTilesMobile));
            Assert.That(readiness.RegionAt(new Vector2(0.38f, 0.40f)).RegionId, Is.EqualTo("region_01_01"));
        }

        [Test]
        public void Arch166TelemetryRulesSeparatePanAndPinch()
        {
            var panFrames = new[]
            {
                new WorldMapGestureTelemetryFrame("ARCH166-AUTO-001", 1, 1, WorldMapGestureMode.OneFingerPan, new Vector2(30f, 0f), 0f, 1f, 1f, 0f, true, "PanThresholdCrossed", true, true, true, true)
            };
            var pinchFrames = new[]
            {
                new WorldMapGestureTelemetryFrame("ARCH166-AUTO-002", 1, 2, WorldMapGestureMode.TwoFingerPinchZoom, Vector2.zero, 0.08f, 1.20f, 1.12f, 0.45f, true, "PinchActive", true, true, true, true)
            };

            Assert.That(WorldMapArch166GestureCertification.OneFingerPanDoesNotZoom(panFrames), Is.True);
            Assert.That(WorldMapArch166GestureCertification.TwoFingerPinchOnly(pinchFrames), Is.True);
            Assert.That(WorldMapArch166GestureCertification.ZoomVelocityIsClamped(pinchFrames), Is.True);
            Assert.That(WorldMapArch166GestureCertification.FixedHudAndAlignmentHold(pinchFrames), Is.True);
        }

        [Test]
        public void HitTestMatrixCoversCenterBorderOutsideAfterTransform()
        {
            WorldMapPostTransformHitTestMatrix matrix = WorldMapPostTransformHitTestMatrix.CreatePreviewMatrix();

            Assert.That(matrix.AllRequiredCasesPresent, Is.True);
            Assert.That(matrix.Passed, Is.True);
            Assert.That(matrix.Cases[0].UsedInverseTransform, Is.True);
        }

        [Test]
        public void ServerSelectionReadinessKeepsCapacityAndNonOfficialSelection()
        {
            var option = new WorldRegistrySelectionOption(
                "future-world-preview",
                "not-assigned",
                WorldMapServerReadinessStatus.Preparing,
                serverRecommended: true,
                serverFull: false,
                minAccounts: 800,
                maxAccounts: 1500,
                minActivePlayers: 300,
                maxActivePlayers: 600,
                minVeryActivePlayers: 100,
                maxVeryActivePlayers: 300,
                maxAlliancePlayers: 100,
                officialSelection: false);

            Assert.That(option.IsNonLiveReadinessValid(), Is.True);
            Assert.That(option.OfficialSelection, Is.False);
            Assert.That(option.MaxAlliancePlayers, Is.EqualTo(100));
        }
    }
}
