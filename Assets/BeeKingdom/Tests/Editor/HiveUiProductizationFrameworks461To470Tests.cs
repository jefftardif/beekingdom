using System;
using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class HiveUiProductizationFrameworks461To470Tests
    {
        [Test]
        public void CompositionZonesStatesAndResources_BlockFinalLayoutAndOfficialClaims()
        {
            var blueprint = new HiveScreenCompositionBlueprint("blueprint", new[]
            {
                new HiveScreenRegion("central-hive", string.Empty, visible: false, new HivePanelDockingNeed("detail", dockVisible: false, obscuresCentralView: true), new HiveOverlayPriorityRule("alert", 1, collisionRisk: true), new HiveScreenServerDependency(visible: false), finalLayoutClaim: true)
            });
            HiveScreenDiagnostics blueprintDiagnostics = blueprint.Evaluate();
            Assert.That(blueprintDiagnostics.Contains(HiveScreenDiagnosticCode.HiveScreenRegionMissing), Is.True);
            Assert.That(blueprintDiagnostics.Contains(HiveScreenDiagnosticCode.HiveOverlayCollisionRisk), Is.True);
            Assert.That(blueprintDiagnostics.Contains(HiveScreenDiagnosticCode.HiveCentralViewObscured), Is.True);
            Assert.That(blueprintDiagnostics.Contains(HiveScreenDiagnosticCode.HiveLayoutFinalClaim), Is.True);
            Assert.That(blueprintDiagnostics.Contains(HiveScreenDiagnosticCode.HiveScreenServerDependencyHidden), Is.True);

            var catalog = new HiveFunctionalZoneCatalog("catalog", new[]
            {
                new HiveFunctionalZoneEntry("nurserie", new HiveZonePlayerPurpose(string.Empty), Array.Empty<HiveZoneDataRequirement>(), new[] { new HiveZoneRouteNeed("detail", visible: false) }, string.Empty, Array.Empty<string>(), new HiveZoneServerDependency(visible: false), officialActionClaim: true)
            });
            HiveZoneDiagnostics catalogDiagnostics = catalog.Evaluate();
            Assert.That(catalogDiagnostics.Contains(HiveZoneDiagnosticCode.HiveFunctionalZoneMissing), Is.True);
            Assert.That(catalogDiagnostics.Contains(HiveZoneDiagnosticCode.HiveZonePurposeMissing), Is.True);
            Assert.That(catalogDiagnostics.Contains(HiveZoneDiagnosticCode.HiveZoneDataMissing), Is.True);
            Assert.That(catalogDiagnostics.Contains(HiveZoneDiagnosticCode.HiveZoneActionForbidden), Is.True);
            Assert.That(catalogDiagnostics.Contains(HiveZoneDiagnosticCode.HiveZoneServerDependencyHidden), Is.True);

            var language = new HiveBuildingStateLanguage("states", new[]
            {
                new HiveBuildingStateToken("normal", new HiveStateVisualTreatment(hasIcon: false, hasTextAlternative: false, colorOnlyRisk: true), new HiveStatePlayerMessage(string.Empty), new HiveStateActionGuard(officialActionClaim: true), new HiveStateServerDependency(visible: false))
            });
            HiveBuildingStateDiagnostics stateDiagnostics = language.Evaluate();
            Assert.That(stateDiagnostics.Contains(HiveBuildingStateDiagnosticCode.HiveBuildingStateMissing), Is.True);
            Assert.That(stateDiagnostics.Contains(HiveBuildingStateDiagnosticCode.HiveStateMessageMissing), Is.True);
            Assert.That(stateDiagnostics.Contains(HiveBuildingStateDiagnosticCode.HiveStateColorOnlyRisk), Is.True);
            Assert.That(stateDiagnostics.Contains(HiveBuildingStateDiagnosticCode.HiveStateActionClaimForbidden), Is.True);
            Assert.That(stateDiagnostics.Contains(HiveBuildingStateDiagnosticCode.HiveStateServerDependencyHidden), Is.True);

            var resources = new HiveResourceReadability("resources", new[]
            {
                new HiveCapacityIndicator("miel", 10, 0, string.Empty, new HiveResourceThresholdPreview(string.Empty, visible: false), new HiveResourceNumberClaimGuard(officialValueClaim: true, spendOrCollectClaim: true), new HiveResourceServerDependency(visible: false))
            });
            HiveResourceReadabilityDiagnostics resourceDiagnostics = resources.Evaluate();
            Assert.That(resourceDiagnostics.Contains(HiveResourceReadabilityDiagnosticCode.HiveResourceValueOfficialClaim), Is.True);
            Assert.That(resourceDiagnostics.Contains(HiveResourceReadabilityDiagnosticCode.HiveCapacityIndicatorMissing), Is.True);
            Assert.That(resourceDiagnostics.Contains(HiveResourceReadabilityDiagnosticCode.HiveResourceTooltipMissing), Is.True);
            Assert.That(resourceDiagnostics.Contains(HiveResourceReadabilityDiagnosticCode.HiveResourceSpendForbidden), Is.True);
            Assert.That(resourceDiagnostics.Contains(HiveResourceReadabilityDiagnosticCode.HiveResourceServerDependencyHidden), Is.True);
        }

        [Test]
        public void RolesMilestonesAllianceAndAdministration_BlockRuntimeAndServerOwnedActions()
        {
            var roles = new HiveBeeRoleAffordance("roles", new[]
            {
                new BeeRoleCardPreview("ouvriere", string.Empty, Array.Empty<string>(), new BeeRoleAvailabilityBadge(string.Empty, previewMarked: false, officialAvailabilityClaim: true), new BeeRoleAssignmentHint(string.Empty, officialAssignmentBlocked: false), new BeeRoleActionGuard(assignmentClaim: true), new BeeRoleServerDependency(visible: false), accessibilityRisk: true)
            });
            BeeRoleAffordanceDiagnostics roleDiagnostics = roles.Evaluate();
            Assert.That(roleDiagnostics.Contains(BeeRoleAffordanceDiagnosticCode.BeeRoleAffordanceMissing), Is.True);
            Assert.That(roleDiagnostics.Contains(BeeRoleAffordanceDiagnosticCode.BeeRoleAvailabilityOfficialClaim), Is.True);
            Assert.That(roleDiagnostics.Contains(BeeRoleAffordanceDiagnosticCode.BeeRoleAssignmentClaim), Is.True);
            Assert.That(roleDiagnostics.Contains(BeeRoleAffordanceDiagnosticCode.BeeRoleAccessibilityRisk), Is.True);
            Assert.That(roleDiagnostics.Contains(BeeRoleAffordanceDiagnosticCode.BeeRoleServerDependencyHidden), Is.True);

            var milestones = new HiveVisualMilestoneStrip("milestones", new[]
            {
                new HiveMilestonePreviewItem("reine-centrale", new HiveMilestoneStageLink(string.Empty, string.Empty), string.Empty, new HiveMilestoneRewardGuard(rewardClaim: true, unlockClaim: true), new HiveMilestoneServerDependency(visible: false))
            });
            HiveMilestoneDiagnostics milestoneDiagnostics = milestones.Evaluate();
            Assert.That(milestoneDiagnostics.Contains(HiveMilestoneDiagnosticCode.HiveMilestoneMissing), Is.True);
            Assert.That(milestoneDiagnostics.Contains(HiveMilestoneDiagnosticCode.HiveMilestoneRewardClaim), Is.True);
            Assert.That(milestoneDiagnostics.Contains(HiveMilestoneDiagnosticCode.HiveMilestoneUnlockClaim), Is.True);
            Assert.That(milestoneDiagnostics.Contains(HiveMilestoneDiagnosticCode.HiveMilestoneRouteMissing), Is.True);
            Assert.That(milestoneDiagnostics.Contains(HiveMilestoneDiagnosticCode.HiveMilestoneServerDependencyHidden), Is.True);

            var portal = new HiveAlliancePortalLink("portal", new AllianceCenterZonePreview(string.Empty, visible: false), new[]
            {
                new HiveSocialRoute("chat-preview", string.Empty, string.Empty, new AlliancePortalActionGuard(liveClaim: true, membershipClaim: true), new AlliancePortalPrivacyNotice(string.Empty, visible: false), new AlliancePortalServerDependency(visible: false))
            });
            AlliancePortalDiagnostics portalDiagnostics = portal.Evaluate();
            Assert.That(portalDiagnostics.Contains(AlliancePortalDiagnosticCode.HiveAlliancePortalMissing), Is.True);
            Assert.That(portalDiagnostics.Contains(AlliancePortalDiagnosticCode.AlliancePortalLiveClaim), Is.True);
            Assert.That(portalDiagnostics.Contains(AlliancePortalDiagnosticCode.AlliancePortalRouteMissing), Is.True);
            Assert.That(portalDiagnostics.Contains(AlliancePortalDiagnosticCode.AlliancePortalPrivacyNoticeMissing), Is.True);
            Assert.That(portalDiagnostics.Contains(AlliancePortalDiagnosticCode.AlliancePortalServerDependencyHidden), Is.True);

            var administrative = new HiveAdministrationArchiveBankPreviewPanel("admin", new[]
            {
                new HiveAdministrativeZonePreview(HiveAdministrativeZoneKind.Bank, string.Empty, string.Empty, HiveAdministrativePreviewState.VisiblePreview, Array.Empty<string>(), Array.Empty<string>(), new HiveAdminPreviewLimitNotice(string.Empty, visible: false), new HiveZoneServerDependency(visible: false), officialActionClaim: true)
            }, null, new HiveAdminPreviewLimitNotice(string.Empty, visible: false));
            HiveAdministrativeDiagnostics administrativeDiagnostics = administrative.Evaluate();
            Assert.That(administrativeDiagnostics.Contains(HiveAdministrativeDiagnosticCode.AdministrativeZoneMissing), Is.True);
            Assert.That(administrativeDiagnostics.Contains(HiveAdministrativeDiagnosticCode.AdministrativeActionClaim), Is.True);
            Assert.That(administrativeDiagnostics.Contains(HiveAdministrativeDiagnosticCode.AdministrativeLimitNoticeMissing), Is.True);
            Assert.That(administrativeDiagnostics.Contains(HiveAdministrativeDiagnosticCode.AdministrativeServerDependencyHidden), Is.True);
        }

        [Test]
        public void DemoEvidenceAndClosureGate_BlockBee471PrematureRelease()
        {
            var blankHarness = new HiveViewDemoEvidenceHarness("blank", new[]
            {
                new HiveMobileViewportScenario("portrait", "Portrait", HiveEvidenceViewport.Portrait, new[] { "central-hive" })
            }, Array.Empty<HiveViewEvidenceFrame>(), screenRendered: false);
            Assert.That(blankHarness.Verdict, Is.EqualTo(HivePlayModeEvidenceVerdict.FailedByBlankScreen));

            var officialClaimHarness = new HiveViewDemoEvidenceHarness("claim", ValidScenarios(), new[]
            {
                new HiveViewEvidenceFrame("frame-461", "BEE-461", "central-hive", new HiveEvidenceLimitNotice("preview", visible: true), visible: true, officialClaim: true)
            });
            Assert.That(officialClaimHarness.Verdict, Is.EqualTo(HivePlayModeEvidenceVerdict.FailedByOfficialClaim));

            var passingHarness = new HiveViewDemoEvidenceHarness("pass", ValidScenarios(), ValidEvidenceFrames());
            Assert.That(passingHarness.Verdict, Is.EqualTo(HivePlayModeEvidenceVerdict.PassWithPreviewLimits));

            var gate = new HiveUiProductHandoffClosureGate("gate", new[]
            {
                new HiveUiCoverageMatrixRow("BEE-461", "composition", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-462", "zones", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-463", "state language", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-464", "resources", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-465", "roles", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-466", "milestones", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-467", "alliance", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-468", "administration", HiveUiProductCoverageStatus.Covered, "Hive Evidence", arch057Covered: true),
                new HiveUiCoverageMatrixRow("BEE-469", "demo evidence", HiveUiProductCoverageStatus.VisualReserve, "local harness", arch057Covered: true)
            }, new[] { new HiveUiReserveRegisterEntry("Demo", "Play Mode evidence remains visual-reserve only.") }, Bee471BlockerStatus.BlockedUntilArchitectValidation);
            Assert.That(gate.Verdict, Is.EqualTo(HiveArch057HandoffVerdict.ReadyWithVisualReserve));
            Assert.That(gate.Bee471Status, Is.EqualTo(Bee471BlockerStatus.BlockedUntilArchitectValidation));

            var prematureGate = new HiveUiProductHandoffClosureGate("premature", gate.Matrix, Array.Empty<HiveUiReserveRegisterEntry>(), Bee471BlockerStatus.ReleasedByFutureArchitectDecision);
            Assert.That(prematureGate.Verdict, Is.EqualTo(HiveArch057HandoffVerdict.BlockedByBee471Premature));
        }

        private static IReadOnlyList<HiveMobileViewportScenario> ValidScenarios()
        {
            return new[]
            {
                new HiveMobileViewportScenario("portrait", "Portrait", HiveEvidenceViewport.Portrait, new[] { "zones", "limits" }),
                new HiveMobileViewportScenario("landscape", "Compact landscape", HiveEvidenceViewport.CompactLandscape, new[] { "zones", "limits" })
            };
        }

        private static IReadOnlyList<HiveViewEvidenceFrame> ValidEvidenceFrames()
        {
            var frames = new List<HiveViewEvidenceFrame>();
            for (int bee = 461; bee <= 468; bee++)
            {
                frames.Add(new HiveViewEvidenceFrame("frame-" + bee, "BEE-" + bee, "element-" + bee, new HiveEvidenceLimitNotice("preview only", visible: true)));
            }

            return frames;
        }
    }
}
