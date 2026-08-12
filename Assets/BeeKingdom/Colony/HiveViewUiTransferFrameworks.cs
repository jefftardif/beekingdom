using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum HiveViewServerDependency { None, EconomyFuture, BuildingsFuture, ProgressionFuture, AllianceFuture, BankFuture, PopulationFuture }
    public enum HiveViewDiagnosticCode { HiveViewZoneMissing, HiveViewInteractionUndefined, HiveViewServerDependencyHidden, HiveViewMobileReadabilityRisk, HiveViewProductionClaim }
    public sealed class HiveViewVisualState { public HiveViewVisualState(string stateId, bool readable = true) { StateId = stateId ?? string.Empty; Readable = readable; } public string StateId { get; } public bool Readable { get; } }
    public sealed class HiveViewDataNeed { public HiveViewDataNeed(string dataId, bool visible) { DataId = dataId ?? string.Empty; Visible = visible; } public string DataId { get; } public bool Visible { get; } }
    public sealed class HiveViewInteractionContract { public HiveViewInteractionContract(string interactionId, bool defined, bool runtimeAction = false) { InteractionId = interactionId ?? string.Empty; Defined = defined; RuntimeAction = runtimeAction; } public string InteractionId { get; } public bool Defined { get; } public bool RuntimeAction { get; } }
    public sealed class HiveViewZoneDefinition
    {
        public HiveViewZoneDefinition(string zoneId, string displayName, string purpose, IReadOnlyList<HiveViewDataNeed> visibleData, IReadOnlyList<HiveViewVisualState> visualStates, IReadOnlyList<HiveViewInteractionContract> interactions, HiveViewServerDependency serverDependency, bool serverDependencyVisible = true, bool productionClaim = false, bool mobileReadabilityRisk = false)
        { ZoneId = zoneId ?? string.Empty; DisplayName = displayName ?? string.Empty; Purpose = purpose ?? string.Empty; VisibleData = visibleData ?? Array.Empty<HiveViewDataNeed>(); VisualStates = visualStates ?? Array.Empty<HiveViewVisualState>(); Interactions = interactions ?? Array.Empty<HiveViewInteractionContract>(); ServerDependency = serverDependency; ServerDependencyVisible = serverDependencyVisible; ProductionClaim = productionClaim; MobileReadabilityRisk = mobileReadabilityRisk; }
        public string ZoneId { get; } public string DisplayName { get; } public string Purpose { get; } public IReadOnlyList<HiveViewDataNeed> VisibleData { get; } public IReadOnlyList<HiveViewVisualState> VisualStates { get; } public IReadOnlyList<HiveViewInteractionContract> Interactions { get; } public HiveViewServerDependency ServerDependency { get; } public bool ServerDependencyVisible { get; } public bool ProductionClaim { get; } public bool MobileReadabilityRisk { get; }
    }
    public sealed class HiveViewUiTransfer
    {
        private static readonly string[] RequiredZones = { "nurserie", "reserves-miel", "caserne", "defense", "genetique", "recherche", "entrepot", "transformation", "infirmerie", "academie", "banque", "administration", "archives", "centre-alliance" };
        public HiveViewUiTransfer(string transferId, IReadOnlyList<HiveViewZoneDefinition> zones) { TransferId = ColonyIntegrationIds.Require(transferId); Zones = zones ?? Array.Empty<HiveViewZoneDefinition>(); }
        public string TransferId { get; } public IReadOnlyList<HiveViewZoneDefinition> Zones { get; }
        public HiveViewDiagnostics Evaluate()
        {
            var findings = new List<HiveViewDiagnosticCode>();
            if (RequiredZones.Any(required => Zones.All(z => !string.Equals(z.ZoneId, required, StringComparison.OrdinalIgnoreCase))) || Zones.Any(z => string.IsNullOrWhiteSpace(z.DisplayName) || string.IsNullOrWhiteSpace(z.Purpose) || z.VisibleData.Count == 0 || z.VisualStates.Count == 0)) findings.Add(HiveViewDiagnosticCode.HiveViewZoneMissing);
            if (Zones.Any(z => z.Interactions.Count == 0 || z.Interactions.Any(i => !i.Defined || i.RuntimeAction))) findings.Add(HiveViewDiagnosticCode.HiveViewInteractionUndefined);
            if (Zones.Any(z => z.ServerDependency != HiveViewServerDependency.None && !z.ServerDependencyVisible)) findings.Add(HiveViewDiagnosticCode.HiveViewServerDependencyHidden);
            if (Zones.Any(z => z.MobileReadabilityRisk || z.VisualStates.Any(s => !s.Readable))) findings.Add(HiveViewDiagnosticCode.HiveViewMobileReadabilityRisk);
            if (Zones.Any(z => z.ProductionClaim)) findings.Add(HiveViewDiagnosticCode.HiveViewProductionClaim);
            return new HiveViewDiagnostics(findings);
        }
    }
    public sealed class HiveViewDiagnostics { public HiveViewDiagnostics(IReadOnlyList<HiveViewDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveViewDiagnosticCode>(); } public IReadOnlyList<HiveViewDiagnosticCode> Findings { get; } public bool Contains(HiveViewDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveViewUiTransferDeclared { public HiveViewUiTransferDeclared(string transferId) { TransferId = transferId ?? string.Empty; } public string TransferId { get; } }
    public sealed class HiveViewZoneRegistered { public HiveViewZoneRegistered(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }
    public sealed class HiveViewLimitExplained { public HiveViewLimitExplained(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }

    public enum HiveDensityStage { Early, Mid, Late }
    public enum HiveSpatialDiagnosticCode { HiveLayerMissing, HiveZoneOverlapRisk, HiveZoomReadabilityRisk, HiveDensityStageMissing, HiveLayerServerDependencyHidden }
    public sealed class HiveZoomReadabilityState { public HiveZoomReadabilityState(bool readable, bool risk = false) { Readable = readable; Risk = risk; } public bool Readable { get; } public bool Risk { get; } }
    public sealed class HiveCellCluster { public HiveCellCluster(string clusterId, bool overlapRisk = false) { ClusterId = clusterId ?? string.Empty; OverlapRisk = overlapRisk; } public string ClusterId { get; } public bool OverlapRisk { get; } }
    public sealed class HiveLayerServerDependency { public HiveLayerServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveLayerDefinition
    {
        public HiveLayerDefinition(string layerId, string purpose, IReadOnlyList<string> zoneIds, HiveDensityStage? densityStage, HiveZoomReadabilityState zoomNeed, bool readabilityRisk, HiveLayerServerDependency serverDependency, IReadOnlyList<HiveCellCluster> clusters = null, bool finalLayoutClaim = false)
        { LayerId = layerId ?? string.Empty; Purpose = purpose ?? string.Empty; ZoneIds = zoneIds ?? Array.Empty<string>(); DensityStage = densityStage; ZoomNeed = zoomNeed; ReadabilityRisk = readabilityRisk; ServerDependency = serverDependency; Clusters = clusters ?? Array.Empty<HiveCellCluster>(); FinalLayoutClaim = finalLayoutClaim; }
        public string LayerId { get; } public string Purpose { get; } public IReadOnlyList<string> ZoneIds { get; } public HiveDensityStage? DensityStage { get; } public HiveZoomReadabilityState ZoomNeed { get; } public bool ReadabilityRisk { get; } public HiveLayerServerDependency ServerDependency { get; } public IReadOnlyList<HiveCellCluster> Clusters { get; } public bool FinalLayoutClaim { get; }
    }
    public sealed class HiveSpatialZoneMap
    {
        private static readonly string[] RequiredLayers = { "centre-reine", "cellules-production", "quartiers-militaires", "quartiers-sociaux", "quartiers-recherche", "stockage", "archives-administration" };
        public HiveSpatialZoneMap(string mapId, IReadOnlyList<HiveLayerDefinition> layers) { MapId = ColonyIntegrationIds.Require(mapId); Layers = layers ?? Array.Empty<HiveLayerDefinition>(); }
        public string MapId { get; } public IReadOnlyList<HiveLayerDefinition> Layers { get; }
        public HiveSpatialDiagnostics Evaluate()
        {
            var findings = new List<HiveSpatialDiagnosticCode>();
            if (RequiredLayers.Any(required => Layers.All(l => !string.Equals(l.LayerId, required, StringComparison.OrdinalIgnoreCase))) || Layers.Any(l => l.ZoneIds.Count == 0 || string.IsNullOrWhiteSpace(l.Purpose))) findings.Add(HiveSpatialDiagnosticCode.HiveLayerMissing);
            if (Layers.Any(l => l.Clusters.Any(c => c.OverlapRisk) || l.FinalLayoutClaim)) findings.Add(HiveSpatialDiagnosticCode.HiveZoneOverlapRisk);
            if (Layers.Any(l => l.ReadabilityRisk || l.ZoomNeed == null || !l.ZoomNeed.Readable || l.ZoomNeed.Risk)) findings.Add(HiveSpatialDiagnosticCode.HiveZoomReadabilityRisk);
            if (!Enum.GetValues(typeof(HiveDensityStage)).Cast<HiveDensityStage>().All(stage => Layers.Any(l => l.DensityStage == stage))) findings.Add(HiveSpatialDiagnosticCode.HiveDensityStageMissing);
            if (Layers.Any(l => l.ServerDependency == null || !l.ServerDependency.Visible)) findings.Add(HiveSpatialDiagnosticCode.HiveLayerServerDependencyHidden);
            return new HiveSpatialDiagnostics(findings);
        }
    }
    public sealed class HiveSpatialDiagnostics { public HiveSpatialDiagnostics(IReadOnlyList<HiveSpatialDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveSpatialDiagnosticCode>(); } public IReadOnlyList<HiveSpatialDiagnosticCode> Findings { get; } public bool Contains(HiveSpatialDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveSpatialZonesDeclared { public HiveSpatialZonesDeclared(string mapId) { MapId = mapId ?? string.Empty; } public string MapId { get; } }
    public sealed class HiveLayerInspected { public HiveLayerInspected(string layerId) { LayerId = layerId ?? string.Empty; } public string LayerId { get; } }
    public sealed class HiveDensityStageReviewed { public HiveDensityStageReviewed(HiveDensityStage stage) { Stage = stage; } public HiveDensityStage Stage { get; } }

    public enum HiveBuildingDiagnosticCode { HiveBuildingSelectionMissing, HiveBuildingDetailDataMissing, HiveBuildingActionRuntimeForbidden, HiveBuildingPrerequisiteHidden, HiveBuildingServerDependencyHidden }
    public sealed class HiveBuildingVisualState { public HiveBuildingVisualState(string stateId) { StateId = stateId ?? string.Empty; } public string StateId { get; } }
    public sealed class HiveBuildingActionPreview { public HiveBuildingActionPreview(string actionId, bool runtimeForbidden = false) { ActionId = actionId ?? string.Empty; RuntimeForbidden = runtimeForbidden; } public string ActionId { get; } public bool RuntimeForbidden { get; } }
    public sealed class HiveBuildingPrerequisiteNotice { public HiveBuildingPrerequisiteNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class HiveBuildingServerDependency { public HiveBuildingServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveBuildingDetailPanel
    {
        public HiveBuildingDetailPanel(string buildingId, string displayName, string zone, int levelPreview, string productionPreview, string capacityPreview, string status, IReadOnlyList<HiveBuildingActionPreview> blockedActions, HiveBuildingPrerequisiteNotice prerequisiteNotice, HiveBuildingServerDependency serverDependency)
        { BuildingId = buildingId ?? string.Empty; DisplayName = displayName ?? string.Empty; Zone = zone ?? string.Empty; LevelPreview = levelPreview; ProductionPreview = productionPreview ?? string.Empty; CapacityPreview = capacityPreview ?? string.Empty; Status = status ?? string.Empty; BlockedActions = blockedActions ?? Array.Empty<HiveBuildingActionPreview>(); PrerequisiteNotice = prerequisiteNotice; ServerDependency = serverDependency; }
        public string BuildingId { get; } public string DisplayName { get; } public string Zone { get; } public int LevelPreview { get; } public string ProductionPreview { get; } public string CapacityPreview { get; } public string Status { get; } public IReadOnlyList<HiveBuildingActionPreview> BlockedActions { get; } public HiveBuildingPrerequisiteNotice PrerequisiteNotice { get; } public HiveBuildingServerDependency ServerDependency { get; }
    }
    public sealed class HiveBuildingSelection
    {
        public HiveBuildingSelection(string selectionId, HiveBuildingDetailPanel panel, bool selectionVisible, bool exitVisible) { SelectionId = ColonyIntegrationIds.Require(selectionId); Panel = panel; SelectionVisible = selectionVisible; ExitVisible = exitVisible; }
        public string SelectionId { get; } public HiveBuildingDetailPanel Panel { get; } public bool SelectionVisible { get; } public bool ExitVisible { get; }
        public HiveBuildingDiagnostics Evaluate()
        {
            var findings = new List<HiveBuildingDiagnosticCode>();
            if (!SelectionVisible || !ExitVisible || Panel == null || string.IsNullOrWhiteSpace(Panel.BuildingId)) findings.Add(HiveBuildingDiagnosticCode.HiveBuildingSelectionMissing);
            if (Panel == null || string.IsNullOrWhiteSpace(Panel.DisplayName) || string.IsNullOrWhiteSpace(Panel.Zone) || string.IsNullOrWhiteSpace(Panel.ProductionPreview) || string.IsNullOrWhiteSpace(Panel.CapacityPreview) || string.IsNullOrWhiteSpace(Panel.Status)) findings.Add(HiveBuildingDiagnosticCode.HiveBuildingDetailDataMissing);
            if (Panel != null && Panel.BlockedActions.Any(a => a.RuntimeForbidden)) findings.Add(HiveBuildingDiagnosticCode.HiveBuildingActionRuntimeForbidden);
            if (Panel == null || Panel.PrerequisiteNotice == null || !Panel.PrerequisiteNotice.Visible || string.IsNullOrWhiteSpace(Panel.PrerequisiteNotice.Text)) findings.Add(HiveBuildingDiagnosticCode.HiveBuildingPrerequisiteHidden);
            if (Panel == null || Panel.ServerDependency == null || !Panel.ServerDependency.Visible) findings.Add(HiveBuildingDiagnosticCode.HiveBuildingServerDependencyHidden);
            return new HiveBuildingDiagnostics(findings);
        }
    }
    public sealed class HiveBuildingDiagnostics { public HiveBuildingDiagnostics(IReadOnlyList<HiveBuildingDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveBuildingDiagnosticCode>(); } public IReadOnlyList<HiveBuildingDiagnosticCode> Findings { get; } public bool Contains(HiveBuildingDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveBuildingSelected { public HiveBuildingSelected(string buildingId) { BuildingId = buildingId ?? string.Empty; } public string BuildingId { get; } }
    public sealed class HiveBuildingDetailViewed { public HiveBuildingDetailViewed(string buildingId) { BuildingId = buildingId ?? string.Empty; } public string BuildingId { get; } }
    public sealed class HiveBuildingActionBlocked { public HiveBuildingActionBlocked(string actionId) { ActionId = actionId ?? string.Empty; } public string ActionId { get; } }

    public enum HiveResourceKind { Honey, Wax, Population, Capacity }
    public enum HiveProductionDiagnosticCode { HiveResourceOfficialClaim, HiveProductionQueueRuntimeForbidden, HiveAccelerationForbidden, HiveCapacityMissing, HiveEconomyServerDependencyHidden }
    public sealed class HiveCapacityPreview { public HiveCapacityPreview(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class HiveAccelerationPreviewBlocker { public HiveAccelerationPreviewBlocker(bool accelerationClaim, bool blockedVisible) { AccelerationClaim = accelerationClaim; BlockedVisible = blockedVisible; } public bool AccelerationClaim { get; } public bool BlockedVisible { get; } }
    public sealed class HiveResourceStateNotice { public HiveResourceStateNotice(string text, bool officialClaim = false) { Text = text ?? string.Empty; OfficialClaim = officialClaim; } public string Text { get; } public bool OfficialClaim { get; } }
    public sealed class HiveEconomyServerDependency { public HiveEconomyServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveProductionQueuePreview
    {
        public HiveProductionQueuePreview(string queueId, HiveResourceKind resourceKind, string buildingId, string status, HiveCapacityPreview capacityPreview, HiveAccelerationPreviewBlocker accelerationBlocked, HiveEconomyServerDependency serverDependency, bool runtimeQueueClaim = false)
        { QueueId = queueId ?? string.Empty; ResourceKind = resourceKind; BuildingId = buildingId ?? string.Empty; Status = status ?? string.Empty; CapacityPreview = capacityPreview; AccelerationBlocked = accelerationBlocked; ServerDependency = serverDependency; RuntimeQueueClaim = runtimeQueueClaim; }
        public string QueueId { get; } public HiveResourceKind ResourceKind { get; } public string BuildingId { get; } public string Status { get; } public HiveCapacityPreview CapacityPreview { get; } public HiveAccelerationPreviewBlocker AccelerationBlocked { get; } public HiveEconomyServerDependency ServerDependency { get; } public bool RuntimeQueueClaim { get; }
    }
    public sealed class HiveResourceHudPreview
    {
        public HiveResourceHudPreview(string hudId, IReadOnlyList<HiveResourceStateNotice> resources, IReadOnlyList<HiveProductionQueuePreview> queues) { HudId = ColonyIntegrationIds.Require(hudId); Resources = resources ?? Array.Empty<HiveResourceStateNotice>(); Queues = queues ?? Array.Empty<HiveProductionQueuePreview>(); }
        public string HudId { get; } public IReadOnlyList<HiveResourceStateNotice> Resources { get; } public IReadOnlyList<HiveProductionQueuePreview> Queues { get; }
        public HiveProductionDiagnostics Evaluate()
        {
            var findings = new List<HiveProductionDiagnosticCode>();
            if (Resources.Count == 0 || Resources.Any(r => r.OfficialClaim)) findings.Add(HiveProductionDiagnosticCode.HiveResourceOfficialClaim);
            if (Queues.Any(q => q.RuntimeQueueClaim)) findings.Add(HiveProductionDiagnosticCode.HiveProductionQueueRuntimeForbidden);
            if (Queues.Any(q => q.AccelerationBlocked == null || q.AccelerationBlocked.AccelerationClaim || !q.AccelerationBlocked.BlockedVisible)) findings.Add(HiveProductionDiagnosticCode.HiveAccelerationForbidden);
            if (Queues.Any(q => q.CapacityPreview == null || !q.CapacityPreview.Visible || string.IsNullOrWhiteSpace(q.CapacityPreview.Text))) findings.Add(HiveProductionDiagnosticCode.HiveCapacityMissing);
            if (Queues.Any(q => q.ServerDependency == null || !q.ServerDependency.Visible)) findings.Add(HiveProductionDiagnosticCode.HiveEconomyServerDependencyHidden);
            return new HiveProductionDiagnostics(findings);
        }
    }
    public sealed class HiveProductionDiagnostics { public HiveProductionDiagnostics(IReadOnlyList<HiveProductionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveProductionDiagnosticCode>(); } public IReadOnlyList<HiveProductionDiagnosticCode> Findings { get; } public bool Contains(HiveProductionDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveResourceHudShown { public HiveResourceHudShown(string hudId) { HudId = hudId ?? string.Empty; } public string HudId { get; } }
    public sealed class HiveProductionQueueViewed { public HiveProductionQueueViewed(string queueId) { QueueId = queueId ?? string.Empty; } public string QueueId { get; } }
    public sealed class HiveProductionActionBlocked { public HiveProductionActionBlocked(string queueId) { QueueId = queueId ?? string.Empty; } public string QueueId { get; } }

    public enum BeeRoleKind { Worker, Nurse, Guard, Scout, Healer, Researcher, Soldier }
    public enum BeePopulationDiagnosticCode { BeePopulationOfficialClaim, BeeAssignmentRuntimeForbidden, BeeRoleMissing, BeeCapacityHidden, BeePopulationServerDependencyHidden }
    public sealed class BeeRoleAvailability { public BeeRoleAvailability(BeeRoleKind role, bool visible) { Role = role; Visible = visible; } public BeeRoleKind Role { get; } public bool Visible { get; } }
    public sealed class HivePopulationCapacityNotice { public HivePopulationCapacityNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class BeeAssignmentActionBlocker { public BeeAssignmentActionBlocker(bool runtimeAssignmentClaim, bool blockedVisible) { RuntimeAssignmentClaim = runtimeAssignmentClaim; BlockedVisible = blockedVisible; } public bool RuntimeAssignmentClaim { get; } public bool BlockedVisible { get; } }
    public sealed class BeePopulationServerDependency { public BeePopulationServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class BeeAssignmentPreview
    {
        public BeeAssignmentPreview(BeeRoleKind beeRole, string zoneId, int assignedCountPreview, int availableCountPreview, HivePopulationCapacityNotice capacityNotice, BeeAssignmentActionBlocker actionBlocked, BeePopulationServerDependency serverDependency, bool officialCountClaim = false)
        { BeeRole = beeRole; ZoneId = zoneId ?? string.Empty; AssignedCountPreview = assignedCountPreview; AvailableCountPreview = availableCountPreview; CapacityNotice = capacityNotice; ActionBlocked = actionBlocked; ServerDependency = serverDependency; OfficialCountClaim = officialCountClaim; }
        public BeeRoleKind BeeRole { get; } public string ZoneId { get; } public int AssignedCountPreview { get; } public int AvailableCountPreview { get; } public HivePopulationCapacityNotice CapacityNotice { get; } public BeeAssignmentActionBlocker ActionBlocked { get; } public BeePopulationServerDependency ServerDependency { get; } public bool OfficialCountClaim { get; }
    }
    public sealed class HiveBeePopulationView
    {
        public HiveBeePopulationView(string viewId, IReadOnlyList<BeeRoleAvailability> roles, IReadOnlyList<BeeAssignmentPreview> assignments) { ViewId = ColonyIntegrationIds.Require(viewId); Roles = roles ?? Array.Empty<BeeRoleAvailability>(); Assignments = assignments ?? Array.Empty<BeeAssignmentPreview>(); }
        public string ViewId { get; } public IReadOnlyList<BeeRoleAvailability> Roles { get; } public IReadOnlyList<BeeAssignmentPreview> Assignments { get; }
        public BeePopulationDiagnostics Evaluate()
        {
            var findings = new List<BeePopulationDiagnosticCode>();
            if (Assignments.Any(a => a.OfficialCountClaim)) findings.Add(BeePopulationDiagnosticCode.BeePopulationOfficialClaim);
            if (Assignments.Any(a => a.ActionBlocked == null || a.ActionBlocked.RuntimeAssignmentClaim || !a.ActionBlocked.BlockedVisible)) findings.Add(BeePopulationDiagnosticCode.BeeAssignmentRuntimeForbidden);
            if (!Enum.GetValues(typeof(BeeRoleKind)).Cast<BeeRoleKind>().All(role => Roles.Any(r => r.Role == role && r.Visible))) findings.Add(BeePopulationDiagnosticCode.BeeRoleMissing);
            if (Assignments.Any(a => a.CapacityNotice == null || !a.CapacityNotice.Visible || string.IsNullOrWhiteSpace(a.CapacityNotice.Text))) findings.Add(BeePopulationDiagnosticCode.BeeCapacityHidden);
            if (Assignments.Any(a => a.ServerDependency == null || !a.ServerDependency.Visible)) findings.Add(BeePopulationDiagnosticCode.BeePopulationServerDependencyHidden);
            return new BeePopulationDiagnostics(findings);
        }
    }
    public sealed class BeePopulationDiagnostics { public BeePopulationDiagnostics(IReadOnlyList<BeePopulationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<BeePopulationDiagnosticCode>(); } public IReadOnlyList<BeePopulationDiagnosticCode> Findings { get; } public bool Contains(BeePopulationDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveBeePopulationShown { public HiveBeePopulationShown(string viewId) { ViewId = viewId ?? string.Empty; } public string ViewId { get; } }
    public sealed class BeeAssignmentPreviewed { public BeeAssignmentPreviewed(BeeRoleKind role) { Role = role; } public BeeRoleKind Role { get; } }
    public sealed class BeeAssignmentActionBlocked { public BeeAssignmentActionBlocked(BeeRoleKind role) { Role = role; } public BeeRoleKind Role { get; } }

    public enum HiveVisualStageKind { Early, Mid, Late }
    public enum HiveStageDiagnosticCode { HiveStageMissing, HiveProgressionOfficialClaim, HiveStageReadabilityRisk, HiveStageAssetGap, HiveProgressionServerDependencyHidden }
    public sealed class HiveStageVisualNeed { public HiveStageVisualNeed(string text, bool readable) { Text = text ?? string.Empty; Readable = readable; } public string Text { get; } public bool Readable { get; } }
    public sealed class HiveStageUnlockPreview { public HiveStageUnlockPreview(string text, bool officialUnlockClaim = false, bool rewardClaim = false) { Text = text ?? string.Empty; OfficialUnlockClaim = officialUnlockClaim; RewardClaim = rewardClaim; } public string Text { get; } public bool OfficialUnlockClaim { get; } public bool RewardClaim { get; } }
    public sealed class HiveStageDensityRule { public HiveStageDensityRule(string density, bool readabilityRisk = false) { Density = density ?? string.Empty; ReadabilityRisk = readabilityRisk; } public string Density { get; } public bool ReadabilityRisk { get; } }
    public sealed class HiveStageAssetNeed { public HiveStageAssetNeed(string assetId, bool available) { AssetId = assetId ?? string.Empty; Available = available; } public string AssetId { get; } public bool Available { get; } }
    public sealed class HiveProgressionServerDependency { public HiveProgressionServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveVisualProgressionStage
    {
        public HiveVisualProgressionStage(string stageId, HiveVisualStageKind stageName, HiveStageDensityRule visualDensity, HiveStageUnlockPreview unlockedZonePreview, IReadOnlyList<HiveStageAssetNeed> assetNeeds, HiveStageVisualNeed readabilityNeed, HiveProgressionServerDependency serverDependency)
        { StageId = stageId ?? string.Empty; StageName = stageName; VisualDensity = visualDensity; UnlockedZonePreview = unlockedZonePreview; AssetNeeds = assetNeeds ?? Array.Empty<HiveStageAssetNeed>(); ReadabilityNeed = readabilityNeed; ServerDependency = serverDependency; }
        public string StageId { get; } public HiveVisualStageKind StageName { get; } public HiveStageDensityRule VisualDensity { get; } public HiveStageUnlockPreview UnlockedZonePreview { get; } public IReadOnlyList<HiveStageAssetNeed> AssetNeeds { get; } public HiveStageVisualNeed ReadabilityNeed { get; } public HiveProgressionServerDependency ServerDependency { get; }
    }
    public sealed class HiveVisualProgressionStageSet
    {
        public HiveVisualProgressionStageSet(string setId, IReadOnlyList<HiveVisualProgressionStage> stages) { SetId = ColonyIntegrationIds.Require(setId); Stages = stages ?? Array.Empty<HiveVisualProgressionStage>(); }
        public string SetId { get; } public IReadOnlyList<HiveVisualProgressionStage> Stages { get; }
        public HiveStageDiagnostics Evaluate()
        {
            var findings = new List<HiveStageDiagnosticCode>();
            if (!Enum.GetValues(typeof(HiveVisualStageKind)).Cast<HiveVisualStageKind>().All(stage => Stages.Any(s => s.StageName == stage))) findings.Add(HiveStageDiagnosticCode.HiveStageMissing);
            if (Stages.Any(s => s.UnlockedZonePreview != null && (s.UnlockedZonePreview.OfficialUnlockClaim || s.UnlockedZonePreview.RewardClaim))) findings.Add(HiveStageDiagnosticCode.HiveProgressionOfficialClaim);
            if (Stages.Any(s => s.ReadabilityNeed == null || !s.ReadabilityNeed.Readable || s.VisualDensity == null || s.VisualDensity.ReadabilityRisk)) findings.Add(HiveStageDiagnosticCode.HiveStageReadabilityRisk);
            if (Stages.Any(s => s.AssetNeeds.Count == 0 || s.AssetNeeds.Any(a => !a.Available))) findings.Add(HiveStageDiagnosticCode.HiveStageAssetGap);
            if (Stages.Any(s => s.ServerDependency == null || !s.ServerDependency.Visible)) findings.Add(HiveStageDiagnosticCode.HiveProgressionServerDependencyHidden);
            return new HiveStageDiagnostics(findings);
        }
    }
    public sealed class HiveStageDiagnostics { public HiveStageDiagnostics(IReadOnlyList<HiveStageDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveStageDiagnosticCode>(); } public IReadOnlyList<HiveStageDiagnosticCode> Findings { get; } public bool Contains(HiveStageDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveVisualStageDeclared { public HiveVisualStageDeclared(string stageId) { StageId = stageId ?? string.Empty; } public string StageId { get; } }
    public sealed class HiveStagePreviewInspected { public HiveStagePreviewInspected(string stageId) { StageId = stageId ?? string.Empty; } public string StageId { get; } }
    public sealed class HiveStageProgressionBlocked { public HiveStageProgressionBlocked(string stageId) { StageId = stageId ?? string.Empty; } public string StageId { get; } }

    public enum HiveAlertSeverityPreview { Info, Attention, Blocked, MisleadingUrgent }
    public enum HiveAlertDiagnosticCode { HiveAlertLiveClaim, HiveAlertRouteMissing, HiveAlertSeverityMisleading, HiveAlertActionForbidden, HiveAlertServerDependencyHidden }
    public sealed class HiveAlertSourceZone { public HiveAlertSourceZone(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }
    public sealed class HiveReturnHook { public HiveReturnHook(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class HiveAlertActionBlocker { public HiveAlertActionBlocker(bool liveClaim, bool officialActionClaim = false) { LiveClaim = liveClaim; OfficialActionClaim = officialActionClaim; } public bool LiveClaim { get; } public bool OfficialActionClaim { get; } }
    public sealed class HiveAlertServerDependency { public HiveAlertServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveAlertPreview
    {
        public HiveAlertPreview(string alertId, HiveAlertSourceZone sourceZone, HiveAlertSeverityPreview severityPreview, string playerMessage, HiveReturnHook returnHook, HiveAlertActionBlocker blockedAction, HiveAlertServerDependency serverDependency)
        { AlertId = alertId ?? string.Empty; SourceZone = sourceZone; SeverityPreview = severityPreview; PlayerMessage = playerMessage ?? string.Empty; ReturnHook = returnHook; BlockedAction = blockedAction; ServerDependency = serverDependency; }
        public string AlertId { get; } public HiveAlertSourceZone SourceZone { get; } public HiveAlertSeverityPreview SeverityPreview { get; } public string PlayerMessage { get; } public HiveReturnHook ReturnHook { get; } public HiveAlertActionBlocker BlockedAction { get; } public HiveAlertServerDependency ServerDependency { get; }
        public HiveAlertDiagnostics Evaluate()
        {
            var findings = new List<HiveAlertDiagnosticCode>();
            if (BlockedAction != null && BlockedAction.LiveClaim) findings.Add(HiveAlertDiagnosticCode.HiveAlertLiveClaim);
            if (SourceZone == null || string.IsNullOrWhiteSpace(SourceZone.ZoneId) || ReturnHook == null || string.IsNullOrWhiteSpace(ReturnHook.RouteId)) findings.Add(HiveAlertDiagnosticCode.HiveAlertRouteMissing);
            if (SeverityPreview == HiveAlertSeverityPreview.MisleadingUrgent || string.IsNullOrWhiteSpace(PlayerMessage)) findings.Add(HiveAlertDiagnosticCode.HiveAlertSeverityMisleading);
            if (BlockedAction != null && BlockedAction.OfficialActionClaim) findings.Add(HiveAlertDiagnosticCode.HiveAlertActionForbidden);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(HiveAlertDiagnosticCode.HiveAlertServerDependencyHidden);
            return new HiveAlertDiagnostics(findings);
        }
    }
    public sealed class HiveAlertDiagnostics { public HiveAlertDiagnostics(IReadOnlyList<HiveAlertDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveAlertDiagnosticCode>(); } public IReadOnlyList<HiveAlertDiagnosticCode> Findings { get; } public bool Contains(HiveAlertDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveAlertPreviewShown { public HiveAlertPreviewShown(string alertId) { AlertId = alertId ?? string.Empty; } public string AlertId { get; } }
    public sealed class HiveReturnHookFollowed { public HiveReturnHookFollowed(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class HiveAlertActionBlocked { public HiveAlertActionBlocked(string alertId) { AlertId = alertId ?? string.Empty; } public string AlertId { get; } }

    public enum HiveFilterKind { Production, Defense, Resources, Population, Research, Alliance, Alerts, Locked }
    public enum HiveViewportDiagnosticCode { HivePanControlMissing, HiveZoomReadabilityRisk, HiveFilterMissing, HiveFocusResetMissing, HiveGestureFinalClaim }
    public sealed class HivePanPreview { public HivePanPreview(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveZoomLevelNeed { public HiveZoomLevelNeed(bool readable, bool risk = false) { Readable = readable; Risk = risk; } public bool Readable { get; } public bool Risk { get; } }
    public sealed class HiveViewportAccessibilityNeed { public HiveViewportAccessibilityNeed(bool visible, bool certificationClaim = false) { Visible = visible; CertificationClaim = certificationClaim; } public bool Visible { get; } public bool CertificationClaim { get; } }
    public sealed class HiveFocusResetAction { public HiveFocusResetAction(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveDisplayFilter
    {
        public HiveDisplayFilter(string filterId, string label, HiveFilterKind? kind, bool visibleState, bool mobileReadabilityNeed, HiveViewportAccessibilityNeed accessibilityNeed, bool finalGestureClaimBlocked)
        { FilterId = filterId ?? string.Empty; Label = label ?? string.Empty; Kind = kind; VisibleState = visibleState; MobileReadabilityNeed = mobileReadabilityNeed; AccessibilityNeed = accessibilityNeed; FinalGestureClaimBlocked = finalGestureClaimBlocked; }
        public string FilterId { get; } public string Label { get; } public HiveFilterKind? Kind { get; } public bool VisibleState { get; } public bool MobileReadabilityNeed { get; } public HiveViewportAccessibilityNeed AccessibilityNeed { get; } public bool FinalGestureClaimBlocked { get; }
    }
    public sealed class HiveMobileViewportControl
    {
        public HiveMobileViewportControl(string controlId, HivePanPreview panPreview, HiveZoomLevelNeed zoomNeed, IReadOnlyList<HiveDisplayFilter> filters, HiveFocusResetAction focusResetAction, bool finalGestureClaim = false)
        { ControlId = ColonyIntegrationIds.Require(controlId); PanPreview = panPreview; ZoomNeed = zoomNeed; Filters = filters ?? Array.Empty<HiveDisplayFilter>(); FocusResetAction = focusResetAction; FinalGestureClaim = finalGestureClaim; }
        public string ControlId { get; } public HivePanPreview PanPreview { get; } public HiveZoomLevelNeed ZoomNeed { get; } public IReadOnlyList<HiveDisplayFilter> Filters { get; } public HiveFocusResetAction FocusResetAction { get; } public bool FinalGestureClaim { get; }
        public HiveViewportDiagnostics Evaluate()
        {
            var findings = new List<HiveViewportDiagnosticCode>();
            if (PanPreview == null || !PanPreview.Visible) findings.Add(HiveViewportDiagnosticCode.HivePanControlMissing);
            if (ZoomNeed == null || !ZoomNeed.Readable || ZoomNeed.Risk || Filters.Any(f => !f.MobileReadabilityNeed || f.AccessibilityNeed == null || !f.AccessibilityNeed.Visible)) findings.Add(HiveViewportDiagnosticCode.HiveZoomReadabilityRisk);
            if (!Enum.GetValues(typeof(HiveFilterKind)).Cast<HiveFilterKind>().All(kind => Filters.Any(f => f.Kind == kind && f.VisibleState))) findings.Add(HiveViewportDiagnosticCode.HiveFilterMissing);
            if (FocusResetAction == null || !FocusResetAction.Visible) findings.Add(HiveViewportDiagnosticCode.HiveFocusResetMissing);
            if (FinalGestureClaim || Filters.Any(f => !f.FinalGestureClaimBlocked || (f.AccessibilityNeed != null && f.AccessibilityNeed.CertificationClaim))) findings.Add(HiveViewportDiagnosticCode.HiveGestureFinalClaim);
            return new HiveViewportDiagnostics(findings);
        }
    }
    public sealed class HiveViewportDiagnostics { public HiveViewportDiagnostics(IReadOnlyList<HiveViewportDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveViewportDiagnosticCode>(); } public IReadOnlyList<HiveViewportDiagnosticCode> Findings { get; } public bool Contains(HiveViewportDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveViewportControlShown { public HiveViewportControlShown(string controlId) { ControlId = controlId ?? string.Empty; } public string ControlId { get; } }
    public sealed class HiveDisplayFilterApplied { public HiveDisplayFilterApplied(string filterId) { FilterId = filterId ?? string.Empty; } public string FilterId { get; } }
    public sealed class HiveFocusResetUsed { public HiveFocusResetUsed(string controlId) { ControlId = controlId ?? string.Empty; } public string ControlId { get; } }

    public enum HiveAssetCategory { ZoneIcon, CellTexture, Lighting, MicroAnimation, Transition, UiSound, SelectionFeedback, BlockFeedback }
    public enum HiveAssetDiagnosticCode { HiveAssetRequirementMissing, HivePlaceholderNotMarked, HiveFinalAssetClaim, HiveSoundAccessibilityRisk, HivePolishProductionClaim }
    public sealed class HiveAssetPlaceholderPolicy { public HiveAssetPlaceholderPolicy(bool temporaryMarked) { TemporaryMarked = temporaryMarked; } public bool TemporaryMarked { get; } }
    public sealed class HivePolishClaimGuard { public HivePolishClaimGuard(bool finalAssetClaim, bool polishProductionClaim = false) { FinalAssetClaim = finalAssetClaim; PolishProductionClaim = polishProductionClaim; } public bool FinalAssetClaim { get; } public bool PolishProductionClaim { get; } }
    public sealed class HiveUiAssetRequirement
    {
        public HiveUiAssetRequirement(string assetId, HiveAssetCategory? category, string usage, HiveAssetPlaceholderPolicy temporaryStatus, bool accessibilityConcern, string demoNeed, HivePolishClaimGuard finalClaimBlocked)
        { AssetId = assetId ?? string.Empty; Category = category; Usage = usage ?? string.Empty; TemporaryStatus = temporaryStatus; AccessibilityConcern = accessibilityConcern; DemoNeed = demoNeed ?? string.Empty; FinalClaimBlocked = finalClaimBlocked; }
        public string AssetId { get; } public HiveAssetCategory? Category { get; } public string Usage { get; } public HiveAssetPlaceholderPolicy TemporaryStatus { get; } public bool AccessibilityConcern { get; } public string DemoNeed { get; } public HivePolishClaimGuard FinalClaimBlocked { get; }
    }
    public sealed class HiveAnimationRequirement { public HiveAnimationRequirement(string animationId, bool intrusive = false) { AnimationId = animationId ?? string.Empty; Intrusive = intrusive; } public string AnimationId { get; } public bool Intrusive { get; } }
    public sealed class HiveSoundRequirement { public HiveSoundRequirement(string soundId, bool accessibilityRisk = false) { SoundId = soundId ?? string.Empty; AccessibilityRisk = accessibilityRisk; } public string SoundId { get; } public bool AccessibilityRisk { get; } }
    public sealed class HiveFeedbackEffectNeed { public HiveFeedbackEffectNeed(string effectId) { EffectId = effectId ?? string.Empty; } public string EffectId { get; } }
    public sealed class HiveUiAssetRequirementRegistry
    {
        public HiveUiAssetRequirementRegistry(string registryId, IReadOnlyList<HiveUiAssetRequirement> assets, IReadOnlyList<HiveAnimationRequirement> animations, IReadOnlyList<HiveSoundRequirement> sounds, IReadOnlyList<HiveFeedbackEffectNeed> feedbacks) { RegistryId = ColonyIntegrationIds.Require(registryId); Assets = assets ?? Array.Empty<HiveUiAssetRequirement>(); Animations = animations ?? Array.Empty<HiveAnimationRequirement>(); Sounds = sounds ?? Array.Empty<HiveSoundRequirement>(); Feedbacks = feedbacks ?? Array.Empty<HiveFeedbackEffectNeed>(); }
        public string RegistryId { get; } public IReadOnlyList<HiveUiAssetRequirement> Assets { get; } public IReadOnlyList<HiveAnimationRequirement> Animations { get; } public IReadOnlyList<HiveSoundRequirement> Sounds { get; } public IReadOnlyList<HiveFeedbackEffectNeed> Feedbacks { get; }
        public HiveAssetDiagnostics Evaluate()
        {
            var findings = new List<HiveAssetDiagnosticCode>();
            if (!Enum.GetValues(typeof(HiveAssetCategory)).Cast<HiveAssetCategory>().All(category => Assets.Any(a => a.Category == category)) || Animations.Count == 0 || Sounds.Count == 0 || Feedbacks.Count == 0) findings.Add(HiveAssetDiagnosticCode.HiveAssetRequirementMissing);
            if (Assets.Any(a => a.TemporaryStatus == null || !a.TemporaryStatus.TemporaryMarked)) findings.Add(HiveAssetDiagnosticCode.HivePlaceholderNotMarked);
            if (Assets.Any(a => a.FinalClaimBlocked != null && a.FinalClaimBlocked.FinalAssetClaim)) findings.Add(HiveAssetDiagnosticCode.HiveFinalAssetClaim);
            if (Sounds.Any(s => s.AccessibilityRisk) || Animations.Any(a => a.Intrusive) || Assets.Any(a => a.AccessibilityConcern)) findings.Add(HiveAssetDiagnosticCode.HiveSoundAccessibilityRisk);
            if (Assets.Any(a => a.FinalClaimBlocked != null && a.FinalClaimBlocked.PolishProductionClaim)) findings.Add(HiveAssetDiagnosticCode.HivePolishProductionClaim);
            return new HiveAssetDiagnostics(findings);
        }
    }
    public sealed class HiveAssetDiagnostics { public HiveAssetDiagnostics(IReadOnlyList<HiveAssetDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveAssetDiagnosticCode>(); } public IReadOnlyList<HiveAssetDiagnosticCode> Findings { get; } public bool Contains(HiveAssetDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveAssetNeedRegistered { public HiveAssetNeedRegistered(string assetId) { AssetId = assetId ?? string.Empty; } public string AssetId { get; } }
    public sealed class HiveAnimationNeedRegistered { public HiveAnimationNeedRegistered(string animationId) { AnimationId = animationId ?? string.Empty; } public string AnimationId { get; } }
    public sealed class HivePlaceholderFlagged { public HivePlaceholderFlagged(string assetId) { AssetId = assetId ?? string.Empty; } public string AssetId { get; } }

    public enum HiveViewTransferVerdict { ReadyForArchitectValidation, ReadyWithUiReserve, NeedsPlannerRevision, BlockedByMissingHiveZone, BlockedByMissingUiNeed, BlockedByProductionClaim, BlockedByHiddenServerDependency, BlockedByBee461Premature }
    public enum HiveViewClosureDiagnosticCode { HiveViewCoverageGap, Arch057RequirementMissing, HiveViewProductionClaim, HiveViewServerBoundaryHidden, Bee461PrematureRelease }
    public sealed class HiveViewArch057Compliance { public HiveViewArch057Compliance(bool zonesCovered, bool interactionsCovered, bool visualStatesCovered, bool dataCovered, bool assetsCovered) { ZonesCovered = zonesCovered; InteractionsCovered = interactionsCovered; VisualStatesCovered = visualStatesCovered; DataCovered = dataCovered; AssetsCovered = assetsCovered; } public bool ZonesCovered { get; } public bool InteractionsCovered { get; } public bool VisualStatesCovered { get; } public bool DataCovered { get; } public bool AssetsCovered { get; } }
    public sealed class HiveViewDemoEvidenceNeed { public HiveViewDemoEvidenceNeed(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveViewServerBoundaryAudit { public HiveViewServerBoundaryAudit(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class Bee461BlockerStatus { public Bee461BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class HiveViewCoverageMatrix
    {
        public HiveViewCoverageMatrix(string beeId, string arch057Requirement, string uiNeed, string demoNeed, string qaCheck, string serverBoundary, HiveViewTransferVerdict verdict)
        { BeeId = beeId ?? string.Empty; Arch057Requirement = arch057Requirement ?? string.Empty; UiNeed = uiNeed ?? string.Empty; DemoNeed = demoNeed ?? string.Empty; QaCheck = qaCheck ?? string.Empty; ServerBoundary = serverBoundary ?? string.Empty; Verdict = verdict; }
        public string BeeId { get; } public string Arch057Requirement { get; } public string UiNeed { get; } public string DemoNeed { get; } public string QaCheck { get; } public string ServerBoundary { get; } public HiveViewTransferVerdict Verdict { get; }
    }
    public sealed class HiveViewUiTransferClosureGate
    {
        public const string Bee461BlockedMessage = "BEE-461 bloquee jusqu'a validation architecte.";
        public HiveViewUiTransferClosureGate(string gateId, IReadOnlyList<HiveViewCoverageMatrix> coverage, HiveViewArch057Compliance arch057Compliance, HiveViewDemoEvidenceNeed demoEvidenceNeed, HiveViewServerBoundaryAudit serverBoundaryAudit, Bee461BlockerStatus bee461BlockerStatus)
        { GateId = ColonyIntegrationIds.Require(gateId); Coverage = coverage ?? Array.Empty<HiveViewCoverageMatrix>(); Arch057Compliance = arch057Compliance; DemoEvidenceNeed = demoEvidenceNeed; ServerBoundaryAudit = serverBoundaryAudit; Bee461BlockerStatus = bee461BlockerStatus ?? new Bee461BlockerStatus(false, Bee461BlockedMessage); }
        public string GateId { get; } public IReadOnlyList<HiveViewCoverageMatrix> Coverage { get; } public HiveViewArch057Compliance Arch057Compliance { get; } public HiveViewDemoEvidenceNeed DemoEvidenceNeed { get; } public HiveViewServerBoundaryAudit ServerBoundaryAudit { get; } public Bee461BlockerStatus Bee461BlockerStatus { get; }
        public HiveViewClosureDiagnostics Evaluate()
        {
            var findings = new List<HiveViewClosureDiagnosticCode>();
            if (Coverage.Count < 9 || Coverage.Any(c => string.IsNullOrWhiteSpace(c.BeeId) || string.IsNullOrWhiteSpace(c.UiNeed) || string.IsNullOrWhiteSpace(c.DemoNeed))) findings.Add(HiveViewClosureDiagnosticCode.HiveViewCoverageGap);
            if (Arch057Compliance == null || !Arch057Compliance.ZonesCovered || !Arch057Compliance.InteractionsCovered || !Arch057Compliance.VisualStatesCovered || !Arch057Compliance.DataCovered || !Arch057Compliance.AssetsCovered || Coverage.Any(c => string.IsNullOrWhiteSpace(c.Arch057Requirement))) findings.Add(HiveViewClosureDiagnosticCode.Arch057RequirementMissing);
            if (Coverage.Any(c => c.Verdict == HiveViewTransferVerdict.BlockedByProductionClaim)) findings.Add(HiveViewClosureDiagnosticCode.HiveViewProductionClaim);
            if (ServerBoundaryAudit == null || !ServerBoundaryAudit.Visible || Coverage.Any(c => string.IsNullOrWhiteSpace(c.ServerBoundary) || c.Verdict == HiveViewTransferVerdict.BlockedByHiddenServerDependency)) findings.Add(HiveViewClosureDiagnosticCode.HiveViewServerBoundaryHidden);
            if (Bee461BlockerStatus.PrematureAttempt) findings.Add(HiveViewClosureDiagnosticCode.Bee461PrematureRelease);
            return new HiveViewClosureDiagnostics(ResolveVerdict(findings), findings);
        }
        private static HiveViewTransferVerdict ResolveVerdict(IReadOnlyList<HiveViewClosureDiagnosticCode> findings)
        {
            if (findings.Contains(HiveViewClosureDiagnosticCode.Bee461PrematureRelease)) return HiveViewTransferVerdict.BlockedByBee461Premature;
            if (findings.Contains(HiveViewClosureDiagnosticCode.HiveViewServerBoundaryHidden)) return HiveViewTransferVerdict.BlockedByHiddenServerDependency;
            if (findings.Contains(HiveViewClosureDiagnosticCode.HiveViewProductionClaim)) return HiveViewTransferVerdict.BlockedByProductionClaim;
            if (findings.Contains(HiveViewClosureDiagnosticCode.Arch057RequirementMissing)) return HiveViewTransferVerdict.BlockedByMissingUiNeed;
            if (findings.Contains(HiveViewClosureDiagnosticCode.HiveViewCoverageGap)) return HiveViewTransferVerdict.BlockedByMissingHiveZone;
            return HiveViewTransferVerdict.ReadyForArchitectValidation;
        }
    }
    public sealed class HiveViewClosureDiagnostics { public HiveViewClosureDiagnostics(HiveViewTransferVerdict verdict, IReadOnlyList<HiveViewClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<HiveViewClosureDiagnosticCode>(); } public HiveViewTransferVerdict Verdict { get; } public IReadOnlyList<HiveViewClosureDiagnosticCode> Findings { get; } public bool Contains(HiveViewClosureDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveViewTransferGateEvaluated { public HiveViewTransferGateEvaluated(string gateId) { GateId = gateId ?? string.Empty; } public string GateId { get; } }
    public sealed class HiveViewCoverageGapDetected { public HiveViewCoverageGapDetected(string beeId) { BeeId = beeId ?? string.Empty; } public string BeeId { get; } }
    public sealed class Bee461BlockedByHiveViewGate { public Bee461BlockedByHiveViewGate(string message) { Message = message ?? string.Empty; } public string Message { get; } }
}
