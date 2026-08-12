using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum HiveScreenDiagnosticCode { HiveScreenRegionMissing, HiveOverlayCollisionRisk, HiveCentralViewObscured, HiveLayoutFinalClaim, HiveScreenServerDependencyHidden }
    public sealed class HiveScreenServerDependency { public HiveScreenServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HivePanelDockingNeed { public HivePanelDockingNeed(string panelId, bool dockVisible, bool obscuresCentralView = false) { PanelId = panelId ?? string.Empty; DockVisible = dockVisible; ObscuresCentralView = obscuresCentralView; } public string PanelId { get; } public bool DockVisible { get; } public bool ObscuresCentralView { get; } }
    public sealed class HiveOverlayPriorityRule { public HiveOverlayPriorityRule(string overlayId, int priority, bool collisionRisk = false) { OverlayId = overlayId ?? string.Empty; Priority = priority; CollisionRisk = collisionRisk; } public string OverlayId { get; } public int Priority { get; } public bool CollisionRisk { get; } }
    public sealed class HiveScreenRegion
    {
        public HiveScreenRegion(string regionId, string purpose, bool visible, HivePanelDockingNeed dockingNeed, HiveOverlayPriorityRule overlayRule, HiveScreenServerDependency serverDependency, bool finalLayoutClaim = false)
        { RegionId = regionId ?? string.Empty; Purpose = purpose ?? string.Empty; Visible = visible; DockingNeed = dockingNeed; OverlayRule = overlayRule; ServerDependency = serverDependency; FinalLayoutClaim = finalLayoutClaim; }
        public string RegionId { get; } public string Purpose { get; } public bool Visible { get; } public HivePanelDockingNeed DockingNeed { get; } public HiveOverlayPriorityRule OverlayRule { get; } public HiveScreenServerDependency ServerDependency { get; } public bool FinalLayoutClaim { get; }
    }
    public sealed class HiveScreenCompositionBlueprint
    {
        private static readonly string[] RequiredRegions = { "central-hive", "resource-hud", "detail-panel", "return-home", "filters", "alerts" };
        public HiveScreenCompositionBlueprint(string blueprintId, IReadOnlyList<HiveScreenRegion> regions) { BlueprintId = ColonyIntegrationIds.Require(blueprintId); Regions = regions ?? Array.Empty<HiveScreenRegion>(); }
        public string BlueprintId { get; } public IReadOnlyList<HiveScreenRegion> Regions { get; }
        public HiveScreenDiagnostics Evaluate()
        {
            var findings = new List<HiveScreenDiagnosticCode>();
            if (RequiredRegions.Any(required => Regions.All(r => !Same(r.RegionId, required))) || Regions.Any(r => !r.Visible || string.IsNullOrWhiteSpace(r.Purpose))) findings.Add(HiveScreenDiagnosticCode.HiveScreenRegionMissing);
            if (Regions.Any(r => r.OverlayRule != null && r.OverlayRule.CollisionRisk)) findings.Add(HiveScreenDiagnosticCode.HiveOverlayCollisionRisk);
            if (Regions.Any(r => r.DockingNeed == null || !r.DockingNeed.DockVisible || r.DockingNeed.ObscuresCentralView)) findings.Add(HiveScreenDiagnosticCode.HiveCentralViewObscured);
            if (Regions.Any(r => r.FinalLayoutClaim)) findings.Add(HiveScreenDiagnosticCode.HiveLayoutFinalClaim);
            if (Regions.Any(r => r.ServerDependency == null || !r.ServerDependency.Visible)) findings.Add(HiveScreenDiagnosticCode.HiveScreenServerDependencyHidden);
            return new HiveScreenDiagnostics(findings);
        }
        private static bool Same(string left, string right) { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }
    public sealed class HiveScreenDiagnostics { public HiveScreenDiagnostics(IReadOnlyList<HiveScreenDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveScreenDiagnosticCode>(); } public IReadOnlyList<HiveScreenDiagnosticCode> Findings { get; } public bool Contains(HiveScreenDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveScreenBlueprintDeclared { public HiveScreenBlueprintDeclared(string blueprintId) { BlueprintId = blueprintId ?? string.Empty; } public string BlueprintId { get; } }
    public sealed class HiveScreenRegionInspected { public HiveScreenRegionInspected(string regionId) { RegionId = regionId ?? string.Empty; } public string RegionId { get; } }
    public sealed class HiveOverlayRiskReported { public HiveOverlayRiskReported(string overlayId) { OverlayId = overlayId ?? string.Empty; } public string OverlayId { get; } }

    public enum HiveZoneDiagnosticCode { HiveFunctionalZoneMissing, HiveZonePurposeMissing, HiveZoneDataMissing, HiveZoneActionForbidden, HiveZoneServerDependencyHidden }
    public sealed class HiveZonePlayerPurpose { public HiveZonePlayerPurpose(string text) { Text = text ?? string.Empty; } public string Text { get; } }
    public sealed class HiveZoneDataRequirement { public HiveZoneDataRequirement(string dataId, bool visible) { DataId = dataId ?? string.Empty; Visible = visible; } public string DataId { get; } public bool Visible { get; } }
    public sealed class HiveZoneRouteNeed { public HiveZoneRouteNeed(string routeId, bool visible) { RouteId = routeId ?? string.Empty; Visible = visible; } public string RouteId { get; } public bool Visible { get; } }
    public sealed class HiveZoneServerDependency { public HiveZoneServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveFunctionalZoneEntry
    {
        public HiveFunctionalZoneEntry(string zoneId, HiveZonePlayerPurpose playerPurpose, IReadOnlyList<HiveZoneDataRequirement> visibleData, IReadOnlyList<HiveZoneRouteNeed> availableRoutes, string visualStatus, IReadOnlyList<string> blockedActions, HiveZoneServerDependency serverDependency, bool officialActionClaim = false)
        { ZoneId = zoneId ?? string.Empty; PlayerPurpose = playerPurpose; VisibleData = visibleData ?? Array.Empty<HiveZoneDataRequirement>(); AvailableRoutes = availableRoutes ?? Array.Empty<HiveZoneRouteNeed>(); VisualStatus = visualStatus ?? string.Empty; BlockedActions = blockedActions ?? Array.Empty<string>(); ServerDependency = serverDependency; OfficialActionClaim = officialActionClaim; }
        public string ZoneId { get; } public HiveZonePlayerPurpose PlayerPurpose { get; } public IReadOnlyList<HiveZoneDataRequirement> VisibleData { get; } public IReadOnlyList<HiveZoneRouteNeed> AvailableRoutes { get; } public string VisualStatus { get; } public IReadOnlyList<string> BlockedActions { get; } public HiveZoneServerDependency ServerDependency { get; } public bool OfficialActionClaim { get; }
    }
    public sealed class HiveFunctionalZoneCatalog
    {
        private static readonly string[] RequiredZones = { "nurserie", "reserves-miel", "caserne", "defense", "genetique", "recherche", "entrepot", "transformation", "infirmerie", "academie", "banque", "administration", "archives", "centre-alliance" };
        public HiveFunctionalZoneCatalog(string catalogId, IReadOnlyList<HiveFunctionalZoneEntry> zones) { CatalogId = ColonyIntegrationIds.Require(catalogId); Zones = zones ?? Array.Empty<HiveFunctionalZoneEntry>(); }
        public string CatalogId { get; } public IReadOnlyList<HiveFunctionalZoneEntry> Zones { get; }
        public HiveZoneDiagnostics Evaluate()
        {
            var findings = new List<HiveZoneDiagnosticCode>();
            if (RequiredZones.Any(required => Zones.All(z => !string.Equals(z.ZoneId, required, StringComparison.OrdinalIgnoreCase))) || Zones.Any(z => string.IsNullOrWhiteSpace(z.VisualStatus))) findings.Add(HiveZoneDiagnosticCode.HiveFunctionalZoneMissing);
            if (Zones.Any(z => z.PlayerPurpose == null || string.IsNullOrWhiteSpace(z.PlayerPurpose.Text))) findings.Add(HiveZoneDiagnosticCode.HiveZonePurposeMissing);
            if (Zones.Any(z => z.VisibleData.Count == 0 || z.VisibleData.Any(d => !d.Visible || string.IsNullOrWhiteSpace(d.DataId)))) findings.Add(HiveZoneDiagnosticCode.HiveZoneDataMissing);
            if (Zones.Any(z => z.OfficialActionClaim || z.BlockedActions.Count == 0 || z.AvailableRoutes.Count == 0 || z.AvailableRoutes.Any(r => !r.Visible))) findings.Add(HiveZoneDiagnosticCode.HiveZoneActionForbidden);
            if (Zones.Any(z => z.ServerDependency == null || !z.ServerDependency.Visible)) findings.Add(HiveZoneDiagnosticCode.HiveZoneServerDependencyHidden);
            return new HiveZoneDiagnostics(findings);
        }
    }
    public sealed class HiveZoneDiagnostics { public HiveZoneDiagnostics(IReadOnlyList<HiveZoneDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveZoneDiagnosticCode>(); } public IReadOnlyList<HiveZoneDiagnosticCode> Findings { get; } public bool Contains(HiveZoneDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveZoneCatalogBuilt { public HiveZoneCatalogBuilt(string catalogId) { CatalogId = catalogId ?? string.Empty; } public string CatalogId { get; } }
    public sealed class HiveZoneEntryInspected { public HiveZoneEntryInspected(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }
    public sealed class HiveZoneLimitExplained { public HiveZoneLimitExplained(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }

    public enum HiveBuildingStateDiagnosticCode { HiveBuildingStateMissing, HiveStateMessageMissing, HiveStateColorOnlyRisk, HiveStateActionClaimForbidden, HiveStateServerDependencyHidden }
    public sealed class HiveStateVisualTreatment { public HiveStateVisualTreatment(bool hasIcon, bool hasTextAlternative, bool colorOnlyRisk = false) { HasIcon = hasIcon; HasTextAlternative = hasTextAlternative; ColorOnlyRisk = colorOnlyRisk; } public bool HasIcon { get; } public bool HasTextAlternative { get; } public bool ColorOnlyRisk { get; } }
    public sealed class HiveStatePlayerMessage { public HiveStatePlayerMessage(string text) { Text = text ?? string.Empty; } public string Text { get; } }
    public sealed class HiveStateActionGuard { public HiveStateActionGuard(bool officialActionClaim) { OfficialActionClaim = officialActionClaim; } public bool OfficialActionClaim { get; } }
    public sealed class HiveStateServerDependency { public HiveStateServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveBuildingStateToken
    {
        public HiveBuildingStateToken(string stateId, HiveStateVisualTreatment visualTreatment, HiveStatePlayerMessage playerMessage, HiveStateActionGuard actionGuard, HiveStateServerDependency serverDependency)
        { StateId = stateId ?? string.Empty; VisualTreatment = visualTreatment; PlayerMessage = playerMessage; ActionGuard = actionGuard; ServerDependency = serverDependency; }
        public string StateId { get; } public HiveStateVisualTreatment VisualTreatment { get; } public HiveStatePlayerMessage PlayerMessage { get; } public HiveStateActionGuard ActionGuard { get; } public HiveStateServerDependency ServerDependency { get; }
    }
    public sealed class HiveBuildingStateLanguage
    {
        private static readonly string[] RequiredStates = { "normal", "selected", "construction", "upgrade", "locked", "inactive", "full", "production", "attention", "server-required", "preview" };
        public HiveBuildingStateLanguage(string languageId, IReadOnlyList<HiveBuildingStateToken> states) { LanguageId = ColonyIntegrationIds.Require(languageId); States = states ?? Array.Empty<HiveBuildingStateToken>(); }
        public string LanguageId { get; } public IReadOnlyList<HiveBuildingStateToken> States { get; }
        public HiveBuildingStateDiagnostics Evaluate()
        {
            var findings = new List<HiveBuildingStateDiagnosticCode>();
            if (RequiredStates.Any(required => States.All(s => !string.Equals(s.StateId, required, StringComparison.OrdinalIgnoreCase)))) findings.Add(HiveBuildingStateDiagnosticCode.HiveBuildingStateMissing);
            if (States.Any(s => s.PlayerMessage == null || string.IsNullOrWhiteSpace(s.PlayerMessage.Text))) findings.Add(HiveBuildingStateDiagnosticCode.HiveStateMessageMissing);
            if (States.Any(s => s.VisualTreatment == null || !s.VisualTreatment.HasIcon || !s.VisualTreatment.HasTextAlternative || s.VisualTreatment.ColorOnlyRisk)) findings.Add(HiveBuildingStateDiagnosticCode.HiveStateColorOnlyRisk);
            if (States.Any(s => s.ActionGuard != null && s.ActionGuard.OfficialActionClaim)) findings.Add(HiveBuildingStateDiagnosticCode.HiveStateActionClaimForbidden);
            if (States.Any(s => s.ServerDependency == null || !s.ServerDependency.Visible)) findings.Add(HiveBuildingStateDiagnosticCode.HiveStateServerDependencyHidden);
            return new HiveBuildingStateDiagnostics(findings);
        }
    }
    public sealed class HiveBuildingStateDiagnostics { public HiveBuildingStateDiagnostics(IReadOnlyList<HiveBuildingStateDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveBuildingStateDiagnosticCode>(); } public IReadOnlyList<HiveBuildingStateDiagnosticCode> Findings { get; } public bool Contains(HiveBuildingStateDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveBuildingStateCatalogBuilt { public HiveBuildingStateCatalogBuilt(string languageId) { LanguageId = languageId ?? string.Empty; } public string LanguageId { get; } }
    public sealed class HiveBuildingStateViewed { public HiveBuildingStateViewed(string stateId) { StateId = stateId ?? string.Empty; } public string StateId { get; } }
    public sealed class HiveBuildingStateLimitExplained { public HiveBuildingStateLimitExplained(string stateId) { StateId = stateId ?? string.Empty; } public string StateId { get; } }

    public enum HiveResourceReadabilityDiagnosticCode { HiveResourceValueOfficialClaim, HiveCapacityIndicatorMissing, HiveResourceTooltipMissing, HiveResourceSpendForbidden, HiveResourceServerDependencyHidden }
    public sealed class HiveResourceThresholdPreview { public HiveResourceThresholdPreview(string label, bool visible) { Label = label ?? string.Empty; Visible = visible; } public string Label { get; } public bool Visible { get; } }
    public sealed class HiveResourceNumberClaimGuard { public HiveResourceNumberClaimGuard(bool officialValueClaim, bool spendOrCollectClaim) { OfficialValueClaim = officialValueClaim; SpendOrCollectClaim = spendOrCollectClaim; } public bool OfficialValueClaim { get; } public bool SpendOrCollectClaim { get; } }
    public sealed class HiveResourceServerDependency { public HiveResourceServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveCapacityIndicator
    {
        public HiveCapacityIndicator(string resourceId, int valuePreview, int capacityPreview, string tooltip, HiveResourceThresholdPreview thresholdPreview, HiveResourceNumberClaimGuard claimGuard, HiveResourceServerDependency serverDependency)
        { ResourceId = resourceId ?? string.Empty; ValuePreview = valuePreview; CapacityPreview = capacityPreview; Tooltip = tooltip ?? string.Empty; ThresholdPreview = thresholdPreview; ClaimGuard = claimGuard; ServerDependency = serverDependency; }
        public string ResourceId { get; } public int ValuePreview { get; } public int CapacityPreview { get; } public string Tooltip { get; } public HiveResourceThresholdPreview ThresholdPreview { get; } public HiveResourceNumberClaimGuard ClaimGuard { get; } public HiveResourceServerDependency ServerDependency { get; }
    }
    public sealed class HiveResourceReadability
    {
        private static readonly string[] RequiredResources = { "miel", "cire", "population", "stockage" };
        public HiveResourceReadability(string readabilityId, IReadOnlyList<HiveCapacityIndicator> indicators) { ReadabilityId = ColonyIntegrationIds.Require(readabilityId); Indicators = indicators ?? Array.Empty<HiveCapacityIndicator>(); }
        public string ReadabilityId { get; } public IReadOnlyList<HiveCapacityIndicator> Indicators { get; }
        public HiveResourceReadabilityDiagnostics Evaluate()
        {
            var findings = new List<HiveResourceReadabilityDiagnosticCode>();
            if (Indicators.Any(i => i.ClaimGuard != null && i.ClaimGuard.OfficialValueClaim)) findings.Add(HiveResourceReadabilityDiagnosticCode.HiveResourceValueOfficialClaim);
            if (RequiredResources.Any(required => Indicators.All(i => !string.Equals(i.ResourceId, required, StringComparison.OrdinalIgnoreCase))) || Indicators.Any(i => i.CapacityPreview <= 0 || i.ThresholdPreview == null || !i.ThresholdPreview.Visible)) findings.Add(HiveResourceReadabilityDiagnosticCode.HiveCapacityIndicatorMissing);
            if (Indicators.Any(i => string.IsNullOrWhiteSpace(i.Tooltip))) findings.Add(HiveResourceReadabilityDiagnosticCode.HiveResourceTooltipMissing);
            if (Indicators.Any(i => i.ClaimGuard != null && i.ClaimGuard.SpendOrCollectClaim)) findings.Add(HiveResourceReadabilityDiagnosticCode.HiveResourceSpendForbidden);
            if (Indicators.Any(i => i.ServerDependency == null || !i.ServerDependency.Visible)) findings.Add(HiveResourceReadabilityDiagnosticCode.HiveResourceServerDependencyHidden);
            return new HiveResourceReadabilityDiagnostics(findings);
        }
    }
    public sealed class HiveResourceReadabilityDiagnostics { public HiveResourceReadabilityDiagnostics(IReadOnlyList<HiveResourceReadabilityDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveResourceReadabilityDiagnosticCode>(); } public IReadOnlyList<HiveResourceReadabilityDiagnosticCode> Findings { get; } public bool Contains(HiveResourceReadabilityDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveResourceReadabilityChecked { public HiveResourceReadabilityChecked(string readabilityId) { ReadabilityId = readabilityId ?? string.Empty; } public string ReadabilityId { get; } }
    public sealed class HiveCapacityIndicatorViewed { public HiveCapacityIndicatorViewed(string resourceId) { ResourceId = resourceId ?? string.Empty; } public string ResourceId { get; } }
    public sealed class HiveResourceActionBlocked { public HiveResourceActionBlocked(string resourceId) { ResourceId = resourceId ?? string.Empty; } public string ResourceId { get; } }

    public enum BeeRoleAffordanceDiagnosticCode { BeeRoleAffordanceMissing, BeeRoleAvailabilityOfficialClaim, BeeRoleAssignmentClaim, BeeRoleAccessibilityRisk, BeeRoleServerDependencyHidden }
    public sealed class BeeRoleAvailabilityBadge { public BeeRoleAvailabilityBadge(string label, bool previewMarked, bool officialAvailabilityClaim = false) { Label = label ?? string.Empty; PreviewMarked = previewMarked; OfficialAvailabilityClaim = officialAvailabilityClaim; } public string Label { get; } public bool PreviewMarked { get; } public bool OfficialAvailabilityClaim { get; } }
    public sealed class BeeRoleAssignmentHint { public BeeRoleAssignmentHint(string text, bool officialAssignmentBlocked) { Text = text ?? string.Empty; OfficialAssignmentBlocked = officialAssignmentBlocked; } public string Text { get; } public bool OfficialAssignmentBlocked { get; } }
    public sealed class BeeRoleActionGuard { public BeeRoleActionGuard(bool assignmentClaim) { AssignmentClaim = assignmentClaim; } public bool AssignmentClaim { get; } }
    public sealed class BeeRoleServerDependency { public BeeRoleServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class BeeRoleCardPreview
    {
        public BeeRoleCardPreview(string roleId, string displayName, IReadOnlyList<string> zoneCompatibility, BeeRoleAvailabilityBadge availabilityPreview, BeeRoleAssignmentHint assignmentHint, BeeRoleActionGuard actionGuard, BeeRoleServerDependency serverDependency, bool accessibilityRisk = false)
        { RoleId = roleId ?? string.Empty; DisplayName = displayName ?? string.Empty; ZoneCompatibility = zoneCompatibility ?? Array.Empty<string>(); AvailabilityPreview = availabilityPreview; AssignmentHint = assignmentHint; ActionGuard = actionGuard; ServerDependency = serverDependency; AccessibilityRisk = accessibilityRisk; }
        public string RoleId { get; } public string DisplayName { get; } public IReadOnlyList<string> ZoneCompatibility { get; } public BeeRoleAvailabilityBadge AvailabilityPreview { get; } public BeeRoleAssignmentHint AssignmentHint { get; } public BeeRoleActionGuard ActionGuard { get; } public BeeRoleServerDependency ServerDependency { get; } public bool AccessibilityRisk { get; }
    }
    public sealed class HiveBeeRoleAffordance
    {
        private static readonly string[] RequiredRoles = { "ouvriere", "nourrice", "garde", "eclaireuse", "soigneuse", "chercheuse", "soldat", "specialiste-futur" };
        public HiveBeeRoleAffordance(string affordanceId, IReadOnlyList<BeeRoleCardPreview> roles) { AffordanceId = ColonyIntegrationIds.Require(affordanceId); Roles = roles ?? Array.Empty<BeeRoleCardPreview>(); }
        public string AffordanceId { get; } public IReadOnlyList<BeeRoleCardPreview> Roles { get; }
        public BeeRoleAffordanceDiagnostics Evaluate()
        {
            var findings = new List<BeeRoleAffordanceDiagnosticCode>();
            if (RequiredRoles.Any(required => Roles.All(r => !string.Equals(r.RoleId, required, StringComparison.OrdinalIgnoreCase))) || Roles.Any(r => string.IsNullOrWhiteSpace(r.DisplayName) || r.ZoneCompatibility.Count == 0)) findings.Add(BeeRoleAffordanceDiagnosticCode.BeeRoleAffordanceMissing);
            if (Roles.Any(r => r.AvailabilityPreview == null || !r.AvailabilityPreview.PreviewMarked || r.AvailabilityPreview.OfficialAvailabilityClaim)) findings.Add(BeeRoleAffordanceDiagnosticCode.BeeRoleAvailabilityOfficialClaim);
            if (Roles.Any(r => r.ActionGuard != null && r.ActionGuard.AssignmentClaim || r.AssignmentHint == null || !r.AssignmentHint.OfficialAssignmentBlocked)) findings.Add(BeeRoleAffordanceDiagnosticCode.BeeRoleAssignmentClaim);
            if (Roles.Any(r => r.AccessibilityRisk || r.AvailabilityPreview == null || string.IsNullOrWhiteSpace(r.AvailabilityPreview.Label) || r.AssignmentHint == null || string.IsNullOrWhiteSpace(r.AssignmentHint.Text))) findings.Add(BeeRoleAffordanceDiagnosticCode.BeeRoleAccessibilityRisk);
            if (Roles.Any(r => r.ServerDependency == null || !r.ServerDependency.Visible)) findings.Add(BeeRoleAffordanceDiagnosticCode.BeeRoleServerDependencyHidden);
            return new BeeRoleAffordanceDiagnostics(findings);
        }
    }
    public sealed class BeeRoleAffordanceDiagnostics { public BeeRoleAffordanceDiagnostics(IReadOnlyList<BeeRoleAffordanceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<BeeRoleAffordanceDiagnosticCode>(); } public IReadOnlyList<BeeRoleAffordanceDiagnosticCode> Findings { get; } public bool Contains(BeeRoleAffordanceDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class BeeRoleAffordanceShown { public BeeRoleAffordanceShown(string roleId) { RoleId = roleId ?? string.Empty; } public string RoleId { get; } }
    public sealed class BeeRoleAvailabilityViewed { public BeeRoleAvailabilityViewed(string roleId) { RoleId = roleId ?? string.Empty; } public string RoleId { get; } }
    public sealed class BeeRoleAssignmentBlocked { public BeeRoleAssignmentBlocked(string roleId) { RoleId = roleId ?? string.Empty; } public string RoleId { get; } }

    public enum HiveMilestoneDiagnosticCode { HiveMilestoneMissing, HiveMilestoneRewardClaim, HiveMilestoneUnlockClaim, HiveMilestoneRouteMissing, HiveMilestoneServerDependencyHidden }
    public sealed class HiveMilestoneStageLink { public HiveMilestoneStageLink(string stage, string zoneRoute) { Stage = stage ?? string.Empty; ZoneRoute = zoneRoute ?? string.Empty; } public string Stage { get; } public string ZoneRoute { get; } }
    public sealed class HiveMilestoneRewardGuard { public HiveMilestoneRewardGuard(bool rewardClaim, bool unlockClaim) { RewardClaim = rewardClaim; UnlockClaim = unlockClaim; } public bool RewardClaim { get; } public bool UnlockClaim { get; } }
    public sealed class HiveMilestoneServerDependency { public HiveMilestoneServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveMilestonePreviewItem
    {
        public HiveMilestonePreviewItem(string milestoneId, HiveMilestoneStageLink stageLink, string previewMessage, HiveMilestoneRewardGuard rewardGuard, HiveMilestoneServerDependency serverDependency)
        { MilestoneId = milestoneId ?? string.Empty; StageLink = stageLink; PreviewMessage = previewMessage ?? string.Empty; RewardGuard = rewardGuard; ServerDependency = serverDependency; }
        public string MilestoneId { get; } public HiveMilestoneStageLink StageLink { get; } public string PreviewMessage { get; } public HiveMilestoneRewardGuard RewardGuard { get; } public HiveMilestoneServerDependency ServerDependency { get; }
    }
    public sealed class HiveVisualMilestoneStrip
    {
        private static readonly string[] RequiredMilestones = { "reine-centrale", "premiere-production", "stockage", "defense", "alliance", "administration" };
        public HiveVisualMilestoneStrip(string stripId, IReadOnlyList<HiveMilestonePreviewItem> milestones) { StripId = ColonyIntegrationIds.Require(stripId); Milestones = milestones ?? Array.Empty<HiveMilestonePreviewItem>(); }
        public string StripId { get; } public IReadOnlyList<HiveMilestonePreviewItem> Milestones { get; }
        public HiveMilestoneDiagnostics Evaluate()
        {
            var findings = new List<HiveMilestoneDiagnosticCode>();
            if (RequiredMilestones.Any(required => Milestones.All(m => !string.Equals(m.MilestoneId, required, StringComparison.OrdinalIgnoreCase))) || Milestones.Any(m => string.IsNullOrWhiteSpace(m.PreviewMessage))) findings.Add(HiveMilestoneDiagnosticCode.HiveMilestoneMissing);
            if (Milestones.Any(m => m.RewardGuard != null && m.RewardGuard.RewardClaim)) findings.Add(HiveMilestoneDiagnosticCode.HiveMilestoneRewardClaim);
            if (Milestones.Any(m => m.RewardGuard != null && m.RewardGuard.UnlockClaim)) findings.Add(HiveMilestoneDiagnosticCode.HiveMilestoneUnlockClaim);
            if (Milestones.Any(m => m.StageLink == null || string.IsNullOrWhiteSpace(m.StageLink.Stage) || string.IsNullOrWhiteSpace(m.StageLink.ZoneRoute))) findings.Add(HiveMilestoneDiagnosticCode.HiveMilestoneRouteMissing);
            if (Milestones.Any(m => m.ServerDependency == null || !m.ServerDependency.Visible)) findings.Add(HiveMilestoneDiagnosticCode.HiveMilestoneServerDependencyHidden);
            return new HiveMilestoneDiagnostics(findings);
        }
    }
    public sealed class HiveMilestoneDiagnostics { public HiveMilestoneDiagnostics(IReadOnlyList<HiveMilestoneDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveMilestoneDiagnosticCode>(); } public IReadOnlyList<HiveMilestoneDiagnosticCode> Findings { get; } public bool Contains(HiveMilestoneDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveMilestoneStripShown { public HiveMilestoneStripShown(string stripId) { StripId = stripId ?? string.Empty; } public string StripId { get; } }
    public sealed class HiveMilestoneInspected { public HiveMilestoneInspected(string milestoneId) { MilestoneId = milestoneId ?? string.Empty; } public string MilestoneId { get; } }
    public sealed class HiveMilestoneActionBlocked { public HiveMilestoneActionBlocked(string milestoneId) { MilestoneId = milestoneId ?? string.Empty; } public string MilestoneId { get; } }

    public enum AlliancePortalDiagnosticCode { HiveAlliancePortalMissing, AlliancePortalLiveClaim, AlliancePortalRouteMissing, AlliancePortalPrivacyNoticeMissing, AlliancePortalServerDependencyHidden }
    public sealed class AlliancePortalServerDependency { public AlliancePortalServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class AlliancePortalActionGuard { public AlliancePortalActionGuard(bool liveClaim, bool membershipClaim = false) { LiveClaim = liveClaim; MembershipClaim = membershipClaim; } public bool LiveClaim { get; } public bool MembershipClaim { get; } }
    public sealed class AlliancePortalPrivacyNotice { public AlliancePortalPrivacyNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class HiveSocialRoute
    {
        public HiveSocialRoute(string routeId, string sourceZone, string targetSurface, AlliancePortalActionGuard actionGuard, AlliancePortalPrivacyNotice privacyNotice, AlliancePortalServerDependency serverDependency)
        { RouteId = routeId ?? string.Empty; SourceZone = sourceZone ?? string.Empty; TargetSurface = targetSurface ?? string.Empty; ActionGuard = actionGuard; PrivacyNotice = privacyNotice; ServerDependency = serverDependency; }
        public string RouteId { get; } public string SourceZone { get; } public string TargetSurface { get; } public AlliancePortalActionGuard ActionGuard { get; } public AlliancePortalPrivacyNotice PrivacyNotice { get; } public AlliancePortalServerDependency ServerDependency { get; }
    }
    public sealed class AllianceCenterZonePreview { public AllianceCenterZonePreview(string zoneId, bool visible) { ZoneId = zoneId ?? string.Empty; Visible = visible; } public string ZoneId { get; } public bool Visible { get; } }
    public sealed class HiveAlliancePortalLink
    {
        private static readonly string[] RequiredRoutes = { "alliance-preview", "alliance-help", "chat-preview", "social-journal" };
        public HiveAlliancePortalLink(string portalId, AllianceCenterZonePreview centerZone, IReadOnlyList<HiveSocialRoute> routes) { PortalId = ColonyIntegrationIds.Require(portalId); CenterZone = centerZone; Routes = routes ?? Array.Empty<HiveSocialRoute>(); }
        public string PortalId { get; } public AllianceCenterZonePreview CenterZone { get; } public IReadOnlyList<HiveSocialRoute> Routes { get; }
        public AlliancePortalDiagnostics Evaluate()
        {
            var findings = new List<AlliancePortalDiagnosticCode>();
            if (CenterZone == null || !CenterZone.Visible || string.IsNullOrWhiteSpace(CenterZone.ZoneId)) findings.Add(AlliancePortalDiagnosticCode.HiveAlliancePortalMissing);
            if (Routes.Any(r => r.ActionGuard != null && (r.ActionGuard.LiveClaim || r.ActionGuard.MembershipClaim))) findings.Add(AlliancePortalDiagnosticCode.AlliancePortalLiveClaim);
            if (RequiredRoutes.Any(required => Routes.All(r => !string.Equals(r.RouteId, required, StringComparison.OrdinalIgnoreCase))) || Routes.Any(r => string.IsNullOrWhiteSpace(r.SourceZone) || string.IsNullOrWhiteSpace(r.TargetSurface))) findings.Add(AlliancePortalDiagnosticCode.AlliancePortalRouteMissing);
            if (Routes.Any(r => r.PrivacyNotice == null || !r.PrivacyNotice.Visible || string.IsNullOrWhiteSpace(r.PrivacyNotice.Text))) findings.Add(AlliancePortalDiagnosticCode.AlliancePortalPrivacyNoticeMissing);
            if (Routes.Any(r => r.ServerDependency == null || !r.ServerDependency.Visible)) findings.Add(AlliancePortalDiagnosticCode.AlliancePortalServerDependencyHidden);
            return new AlliancePortalDiagnostics(findings);
        }
    }
    public sealed class AlliancePortalDiagnostics { public AlliancePortalDiagnostics(IReadOnlyList<AlliancePortalDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AlliancePortalDiagnosticCode>(); } public IReadOnlyList<AlliancePortalDiagnosticCode> Findings { get; } public bool Contains(AlliancePortalDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveAlliancePortalShown { public HiveAlliancePortalShown(string portalId) { PortalId = portalId ?? string.Empty; } public string PortalId { get; } }
    public sealed class HiveSocialRouteFollowed { public HiveSocialRouteFollowed(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class AlliancePortalActionBlocked { public AlliancePortalActionBlocked(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }

    public enum HiveAdministrativeZoneKind { Bank, Administration, Archive }
    public enum HiveAdministrativePreviewState { VisiblePreview, RequiresFutureServerAuthority, LockedByMilestone }
    public enum HiveAdministrativeDiagnosticCode { AdministrativeZoneMissing, AdministrativeActionClaim, AdministrativeLimitNoticeMissing, AdministrativeServerDependencyHidden }
    public sealed class HiveAdminPreviewLimitNotice { public HiveAdminPreviewLimitNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class HiveAdministrativeZonePreview
    {
        public HiveAdministrativeZonePreview(HiveAdministrativeZoneKind kind, string zoneId, string description, HiveAdministrativePreviewState state, IReadOnlyList<string> previewFacts, IReadOnlyList<string> blockedActions, HiveAdminPreviewLimitNotice limitNotice, HiveZoneServerDependency serverDependency, bool officialActionClaim = false)
        { Kind = kind; ZoneId = zoneId ?? string.Empty; Description = description ?? string.Empty; State = state; PreviewFacts = previewFacts ?? Array.Empty<string>(); BlockedActions = blockedActions ?? Array.Empty<string>(); LimitNotice = limitNotice; ServerDependency = serverDependency; OfficialActionClaim = officialActionClaim; }
        public HiveAdministrativeZoneKind Kind { get; } public string ZoneId { get; } public string Description { get; } public HiveAdministrativePreviewState State { get; } public IReadOnlyList<string> PreviewFacts { get; } public IReadOnlyList<string> BlockedActions { get; } public HiveAdminPreviewLimitNotice LimitNotice { get; } public HiveZoneServerDependency ServerDependency { get; } public bool OfficialActionClaim { get; }
    }
    public sealed class HiveAdministrationArchiveBankPreviewPanel
    {
        public HiveAdministrationArchiveBankPreviewPanel(string panelId, IReadOnlyList<HiveAdministrativeZonePreview> zones, HiveAdministrativeZonePreview selectedZone, HiveAdminPreviewLimitNotice limitNotice) { PanelId = ColonyIntegrationIds.Require(panelId); Zones = zones ?? Array.Empty<HiveAdministrativeZonePreview>(); SelectedZone = selectedZone; LimitNotice = limitNotice; }
        public string PanelId { get; } public IReadOnlyList<HiveAdministrativeZonePreview> Zones { get; } public HiveAdministrativeZonePreview SelectedZone { get; } public HiveAdminPreviewLimitNotice LimitNotice { get; }
        public HiveAdministrativeDiagnostics Evaluate()
        {
            var findings = new List<HiveAdministrativeDiagnosticCode>();
            if (!Enum.GetValues(typeof(HiveAdministrativeZoneKind)).Cast<HiveAdministrativeZoneKind>().All(kind => Zones.Any(z => z.Kind == kind)) || Zones.Any(z => string.IsNullOrWhiteSpace(z.ZoneId) || string.IsNullOrWhiteSpace(z.Description) || z.PreviewFacts.Count == 0 || z.BlockedActions.Count == 0)) findings.Add(HiveAdministrativeDiagnosticCode.AdministrativeZoneMissing);
            if (Zones.Any(z => z.OfficialActionClaim)) findings.Add(HiveAdministrativeDiagnosticCode.AdministrativeActionClaim);
            if (LimitNotice == null || !LimitNotice.Visible || string.IsNullOrWhiteSpace(LimitNotice.Text) || Zones.Any(z => z.LimitNotice == null || !z.LimitNotice.Visible || string.IsNullOrWhiteSpace(z.LimitNotice.Text))) findings.Add(HiveAdministrativeDiagnosticCode.AdministrativeLimitNoticeMissing);
            if (Zones.Any(z => z.ServerDependency == null || !z.ServerDependency.Visible)) findings.Add(HiveAdministrativeDiagnosticCode.AdministrativeServerDependencyHidden);
            return new HiveAdministrativeDiagnostics(findings);
        }
    }
    public sealed class HiveAdministrativeDiagnostics { public HiveAdministrativeDiagnostics(IReadOnlyList<HiveAdministrativeDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveAdministrativeDiagnosticCode>(); } public IReadOnlyList<HiveAdministrativeDiagnosticCode> Findings { get; } public bool Contains(HiveAdministrativeDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveAdministrativeZonePreviewOpened { public HiveAdministrativeZonePreviewOpened(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }
    public sealed class HiveAdministrativeBlockedActionTapped { public HiveAdministrativeBlockedActionTapped(string actionId) { ActionId = actionId ?? string.Empty; } public string ActionId { get; } }
    public sealed class HiveAdministrativeLimitNoticeShown { public HiveAdministrativeLimitNoticeShown(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }

    public enum HiveEvidenceViewport { Portrait, CompactLandscape }
    public enum HivePlayModeEvidenceVerdict { NotRun, PassWithPreviewLimits, NeedsVisualCorrection, FailedByBlankScreen, FailedByMissingRequiredElement, FailedByOfficialClaim }
    public sealed class HiveEvidenceLimitNotice { public HiveEvidenceLimitNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class HiveMobileViewportScenario { public HiveMobileViewportScenario(string scenarioId, string displayName, HiveEvidenceViewport viewport, IReadOnlyList<string> requiredChecks) { ScenarioId = scenarioId ?? string.Empty; DisplayName = displayName ?? string.Empty; Viewport = viewport; RequiredChecks = requiredChecks ?? Array.Empty<string>(); } public string ScenarioId { get; } public string DisplayName { get; } public HiveEvidenceViewport Viewport { get; } public IReadOnlyList<string> RequiredChecks { get; } }
    public sealed class HiveViewEvidenceFrame
    {
        public HiveViewEvidenceFrame(string frameId, string relatedBee, string expectedVisibleElement, HiveEvidenceLimitNotice limitNotice, bool visible = true, bool officialClaim = false)
        { FrameId = frameId ?? string.Empty; RelatedBee = relatedBee ?? string.Empty; ExpectedVisibleElement = expectedVisibleElement ?? string.Empty; LimitNotice = limitNotice; Visible = visible; OfficialClaim = officialClaim; }
        public string FrameId { get; } public string RelatedBee { get; } public string ExpectedVisibleElement { get; } public HiveEvidenceLimitNotice LimitNotice { get; } public bool Visible { get; } public bool OfficialClaim { get; }
    }
    public sealed class HiveViewDemoEvidenceHarness
    {
        public HiveViewDemoEvidenceHarness(string harnessId, IReadOnlyList<HiveMobileViewportScenario> scenarios, IReadOnlyList<HiveViewEvidenceFrame> evidenceFrames, bool screenRendered = true) { HarnessId = ColonyIntegrationIds.Require(harnessId); Scenarios = scenarios ?? Array.Empty<HiveMobileViewportScenario>(); EvidenceFrames = evidenceFrames ?? Array.Empty<HiveViewEvidenceFrame>(); ScreenRendered = screenRendered; Verdict = ComputeVerdict(); }
        public string HarnessId { get; } public IReadOnlyList<HiveMobileViewportScenario> Scenarios { get; } public IReadOnlyList<HiveViewEvidenceFrame> EvidenceFrames { get; } public bool ScreenRendered { get; } public HivePlayModeEvidenceVerdict Verdict { get; }
        private HivePlayModeEvidenceVerdict ComputeVerdict()
        {
            if (!ScreenRendered || EvidenceFrames.Count == 0) return HivePlayModeEvidenceVerdict.FailedByBlankScreen;
            if (EvidenceFrames.Any(f => f.OfficialClaim)) return HivePlayModeEvidenceVerdict.FailedByOfficialClaim;
            if (!Scenarios.Any(s => s.Viewport == HiveEvidenceViewport.Portrait) || !Scenarios.Any(s => s.Viewport == HiveEvidenceViewport.CompactLandscape)) return HivePlayModeEvidenceVerdict.NeedsVisualCorrection;
            if (Enumerable.Range(461, 8).Any(bee => EvidenceFrames.All(f => !string.Equals(f.RelatedBee, "BEE-" + bee, StringComparison.OrdinalIgnoreCase))) || EvidenceFrames.Any(f => !f.Visible || string.IsNullOrWhiteSpace(f.ExpectedVisibleElement))) return HivePlayModeEvidenceVerdict.FailedByMissingRequiredElement;
            if (EvidenceFrames.Any(f => f.LimitNotice == null || !f.LimitNotice.Visible || string.IsNullOrWhiteSpace(f.LimitNotice.Text))) return HivePlayModeEvidenceVerdict.NeedsVisualCorrection;
            return HivePlayModeEvidenceVerdict.PassWithPreviewLimits;
        }
    }
    public sealed class HiveEvidenceScenarioStarted { public HiveEvidenceScenarioStarted(string scenarioId) { ScenarioId = scenarioId ?? string.Empty; } public string ScenarioId { get; } }
    public sealed class HiveEvidenceFrameChecked { public HiveEvidenceFrameChecked(string frameId) { FrameId = frameId ?? string.Empty; } public string FrameId { get; } }
    public sealed class HiveEvidenceVerdictComputed { public HiveEvidenceVerdictComputed(HivePlayModeEvidenceVerdict verdict) { Verdict = verdict; } public HivePlayModeEvidenceVerdict Verdict { get; } }

    public enum HiveArch057HandoffVerdict { ReadyForArchitectValidation, ReadyWithVisualReserve, NeedsPlannerRevision, BlockedByMissingZone, BlockedByMissingStateLanguage, BlockedByOfficialActionClaim, BlockedByHiddenServerDependency, BlockedByBee471Premature }
    public enum Bee471BlockerStatus { BlockedUntilArchitectValidation, StillBlockedAfterRevision, ReleasedByFutureArchitectDecision }
    public enum HiveUiProductCoverageStatus { Covered, NeedsDemoEvidence, VisualReserve, HiddenServerDependency, OfficialActionClaim, Missing }
    public sealed class HiveUiReserveRegisterEntry { public HiveUiReserveRegisterEntry(string owner, string reason) { Owner = owner ?? string.Empty; Reason = reason ?? string.Empty; } public string Owner { get; } public string Reason { get; } }
    public sealed class HiveUiCoverageMatrixRow
    {
        public HiveUiCoverageMatrixRow(string beeId, string surface, HiveUiProductCoverageStatus status, string evidenceSource, bool arch057Covered, bool officialActionClaim = false, bool hiddenServerDependency = false)
        { BeeId = beeId ?? string.Empty; Surface = surface ?? string.Empty; Status = status; EvidenceSource = evidenceSource ?? string.Empty; Arch057Covered = arch057Covered; OfficialActionClaim = officialActionClaim; HiddenServerDependency = hiddenServerDependency; }
        public string BeeId { get; } public string Surface { get; } public HiveUiProductCoverageStatus Status { get; } public string EvidenceSource { get; } public bool Arch057Covered { get; } public bool OfficialActionClaim { get; } public bool HiddenServerDependency { get; }
    }
    public sealed class HiveUiProductHandoffClosureGate
    {
        public HiveUiProductHandoffClosureGate(string gateId, IReadOnlyList<HiveUiCoverageMatrixRow> matrix, IReadOnlyList<HiveUiReserveRegisterEntry> reserves, Bee471BlockerStatus bee471Status)
        { GateId = ColonyIntegrationIds.Require(gateId); Matrix = matrix ?? Array.Empty<HiveUiCoverageMatrixRow>(); Reserves = reserves ?? Array.Empty<HiveUiReserveRegisterEntry>(); Bee471Status = bee471Status; Verdict = EvaluateVerdict(); }
        public string GateId { get; } public IReadOnlyList<HiveUiCoverageMatrixRow> Matrix { get; } public IReadOnlyList<HiveUiReserveRegisterEntry> Reserves { get; } public HiveArch057HandoffVerdict Verdict { get; } public Bee471BlockerStatus Bee471Status { get; }
        private HiveArch057HandoffVerdict EvaluateVerdict()
        {
            if (Bee471Status == Bee471BlockerStatus.ReleasedByFutureArchitectDecision) return HiveArch057HandoffVerdict.BlockedByBee471Premature;
            if (Enumerable.Range(461, 9).Any(bee => Matrix.All(row => !string.Equals(row.BeeId, "BEE-" + bee, StringComparison.OrdinalIgnoreCase))) || Matrix.Any(row => row.Status == HiveUiProductCoverageStatus.Missing || !row.Arch057Covered || string.IsNullOrWhiteSpace(row.Surface))) return HiveArch057HandoffVerdict.BlockedByMissingZone;
            if (Matrix.Any(row => string.Equals(row.BeeId, "BEE-463", StringComparison.OrdinalIgnoreCase) && row.Status == HiveUiProductCoverageStatus.Missing)) return HiveArch057HandoffVerdict.BlockedByMissingStateLanguage;
            if (Matrix.Any(row => row.OfficialActionClaim || row.Status == HiveUiProductCoverageStatus.OfficialActionClaim)) return HiveArch057HandoffVerdict.BlockedByOfficialActionClaim;
            if (Matrix.Any(row => row.HiddenServerDependency || row.Status == HiveUiProductCoverageStatus.HiddenServerDependency)) return HiveArch057HandoffVerdict.BlockedByHiddenServerDependency;
            if (Matrix.Any(row => row.Status == HiveUiProductCoverageStatus.NeedsDemoEvidence)) return HiveArch057HandoffVerdict.NeedsPlannerRevision;
            if (Matrix.Any(row => row.Status == HiveUiProductCoverageStatus.VisualReserve) || Reserves.Count > 0) return HiveArch057HandoffVerdict.ReadyWithVisualReserve;
            return HiveArch057HandoffVerdict.ReadyForArchitectValidation;
        }
    }
    public sealed class HiveUiClosureGateEvaluated { public HiveUiClosureGateEvaluated(HiveArch057HandoffVerdict verdict) { Verdict = verdict; } public HiveArch057HandoffVerdict Verdict { get; } }
    public sealed class HiveUiClosureReserveRegistered { public HiveUiClosureReserveRegistered(string reason) { Reason = reason ?? string.Empty; } public string Reason { get; } }
    public sealed class Bee471BlockerConfirmed { public Bee471BlockerConfirmed(Bee471BlockerStatus status) { Status = status; } public Bee471BlockerStatus Status { get; } }
}
