using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum OperationsSourceFreshness { Fresh, Recent, Stale, Unknown }
    public enum OperationsSourceLimit { DemoOnly, QaReviewOnly, ServerDependency, NotFinalEvidence, NoProductionTelemetry, RuntimeForbidden, Hidden }
    public enum OperationsSourceMobileStatus { Visible, VisibleWithWarning, Blocked, Missing }
    public enum OperationsSourceServerDependency { None, ReviewRequired, ServerAuthoritative, Blocked }
    public enum OperationsSourceDiagnosticCode { OperationsSourceMissing, OperationsSourceOwnerMissing, OperationsSourceLimitHidden, OperationsSourceTelemetryProductionForbidden, OperationsSourceUiSurfaceConfused }
    public sealed class OperationsSourceItem
    {
        public OperationsSourceItem(string sourceId, string sourceType, string sourceBeeRange, string ownerRole, OperationsSourceFreshness freshness, IReadOnlyList<OperationsSourceLimit> limits, OperationsSourceMobileStatus mobileStatus, OperationsSourceServerDependency serverDependency, bool telemetryProductionRequested = false, bool uiSurfaceConfused = false)
        {
            SourceId = sourceId ?? string.Empty; SourceType = sourceType ?? string.Empty; SourceBeeRange = sourceBeeRange ?? string.Empty; OwnerRole = ownerRole ?? string.Empty; Freshness = freshness; Limits = limits ?? Array.Empty<OperationsSourceLimit>(); MobileStatus = mobileStatus; ServerDependency = serverDependency; TelemetryProductionRequested = telemetryProductionRequested; UiSurfaceConfused = uiSurfaceConfused;
        }
        public string SourceId { get; } public string SourceType { get; } public string SourceBeeRange { get; } public string OwnerRole { get; } public OperationsSourceFreshness Freshness { get; } public IReadOnlyList<OperationsSourceLimit> Limits { get; } public OperationsSourceMobileStatus MobileStatus { get; } public OperationsSourceServerDependency ServerDependency { get; } public bool TelemetryProductionRequested { get; } public bool UiSurfaceConfused { get; }
    }
    public sealed class ScaleOperationsSourceInventory
    {
        public ScaleOperationsSourceInventory(string inventoryId, IReadOnlyList<OperationsSourceItem> sources) { InventoryId = ColonyIntegrationIds.Require(inventoryId); Sources = sources ?? Array.Empty<OperationsSourceItem>(); }
        public string InventoryId { get; } public IReadOnlyList<OperationsSourceItem> Sources { get; }
        public OperationsSourceDiagnostics Evaluate()
        {
            var findings = new List<OperationsSourceDiagnosticCode>();
            if (Sources.Count == 0 || Sources.Any(s => string.IsNullOrWhiteSpace(s.SourceId) || string.IsNullOrWhiteSpace(s.SourceType))) findings.Add(OperationsSourceDiagnosticCode.OperationsSourceMissing);
            if (Sources.Any(s => string.IsNullOrWhiteSpace(s.OwnerRole))) findings.Add(OperationsSourceDiagnosticCode.OperationsSourceOwnerMissing);
            if (Sources.Any(s => s.Limits.Count == 0 || s.Limits.Contains(OperationsSourceLimit.Hidden))) findings.Add(OperationsSourceDiagnosticCode.OperationsSourceLimitHidden);
            if (Sources.Any(s => s.TelemetryProductionRequested)) findings.Add(OperationsSourceDiagnosticCode.OperationsSourceTelemetryProductionForbidden);
            if (Sources.Any(s => s.UiSurfaceConfused)) findings.Add(OperationsSourceDiagnosticCode.OperationsSourceUiSurfaceConfused);
            return new OperationsSourceDiagnostics(findings);
        }
    }
    public sealed class OperationsSourceDiagnostics { public OperationsSourceDiagnostics(IReadOnlyList<OperationsSourceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<OperationsSourceDiagnosticCode>(); } public IReadOnlyList<OperationsSourceDiagnosticCode> Findings { get; } public bool Contains(OperationsSourceDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ScaleOperationsSourceInventoryBuilt { public ScaleOperationsSourceInventoryBuilt(string inventoryId) { InventoryId = inventoryId ?? string.Empty; } public string InventoryId { get; } }
    public sealed class ScaleOperationsSourceInspected { public ScaleOperationsSourceInspected(string sourceId) { SourceId = sourceId ?? string.Empty; } public string SourceId { get; } }
    public sealed class ScaleOperationsSourceBlocked { public ScaleOperationsSourceBlocked(string sourceId, OperationsSourceDiagnosticCode reason) { SourceId = sourceId ?? string.Empty; Reason = reason; } public string SourceId { get; } public OperationsSourceDiagnosticCode Reason { get; } }

    public enum MobileNavSurfaceKind { Player, ReviewTool, DebugQa, ServerAdmin, ProductionFuture }
    public enum MobileNavDiagnosticCode { MobileNavRouteMissing, MobileNavDeadEndDetected, MobileNavSurfaceBoundaryConfused, MobileNavTouchTargetTooSmall, MobileNavProductionFinalClaimForbidden }
    public sealed class MobileNavTab { public MobileNavTab(string tabId, bool activeStateVisible = true) { TabId = tabId ?? string.Empty; ActiveStateVisible = activeStateVisible; } public string TabId { get; } public bool ActiveStateVisible { get; } }
    public sealed class MobileNavBackRoute { public MobileNavBackRoute(string targetSurfaceId) { TargetSurfaceId = targetSurfaceId ?? string.Empty; } public string TargetSurfaceId { get; } }
    public sealed class MobileNavActiveState { public MobileNavActiveState(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class MobileNavSurfaceBoundary { public MobileNavSurfaceBoundary(bool playerToAdminRoute = false, bool confused = false) { PlayerToAdminRoute = playerToAdminRoute; Confused = confused; } public bool PlayerToAdminRoute { get; } public bool Confused { get; } }
    public sealed class MobileNavConstraints { public MobileNavConstraints(int touchTargetPixels, bool productionFinalClaim = false) { TouchTargetPixels = touchTargetPixels; ProductionFinalClaim = productionFinalClaim; } public int TouchTargetPixels { get; } public bool ProductionFinalClaim { get; } }
    public sealed class MobileNavSurface
    {
        public MobileNavSurface(string surfaceId, MobileNavSurfaceKind surfaceKind, MobileNavTab primaryTab, MobileNavBackRoute backRoute, IReadOnlyList<string> allowedTargets, IReadOnlyList<string> forbiddenTargets, MobileNavConstraints mobileConstraints, MobileNavActiveState activeState, MobileNavSurfaceBoundary boundary)
        {
            SurfaceId = surfaceId ?? string.Empty; SurfaceKind = surfaceKind; PrimaryTab = primaryTab; BackRoute = backRoute; AllowedTargets = allowedTargets ?? Array.Empty<string>(); ForbiddenTargets = forbiddenTargets ?? Array.Empty<string>(); MobileConstraints = mobileConstraints; ActiveState = activeState; Boundary = boundary;
        }
        public string SurfaceId { get; } public MobileNavSurfaceKind SurfaceKind { get; } public MobileNavTab PrimaryTab { get; } public MobileNavBackRoute BackRoute { get; } public IReadOnlyList<string> AllowedTargets { get; } public IReadOnlyList<string> ForbiddenTargets { get; } public MobileNavConstraints MobileConstraints { get; } public MobileNavActiveState ActiveState { get; } public MobileNavSurfaceBoundary Boundary { get; }
    }
    public sealed class MobileNavigationShellContract
    {
        public const int MinimumTouchTargetPixels = 44;
        public MobileNavigationShellContract(string shellId, IReadOnlyList<MobileNavSurface> surfaces) { ShellId = ColonyIntegrationIds.Require(shellId); Surfaces = surfaces ?? Array.Empty<MobileNavSurface>(); }
        public string ShellId { get; } public IReadOnlyList<MobileNavSurface> Surfaces { get; }
        public MobileNavigationDiagnostics Evaluate()
        {
            var findings = new List<MobileNavDiagnosticCode>();
            if (Surfaces.Count == 0 || Surfaces.Any(s => string.IsNullOrWhiteSpace(s.SurfaceId) || s.PrimaryTab == null || s.BackRoute == null || s.ActiveState == null || !s.ActiveState.Visible)) findings.Add(MobileNavDiagnosticCode.MobileNavRouteMissing);
            if (Surfaces.Any(s => s.AllowedTargets.Count == 0 && s.SurfaceKind != MobileNavSurfaceKind.ServerAdmin)) findings.Add(MobileNavDiagnosticCode.MobileNavDeadEndDetected);
            if (Surfaces.Any(s => s.Boundary != null && (s.Boundary.Confused || s.Boundary.PlayerToAdminRoute))) findings.Add(MobileNavDiagnosticCode.MobileNavSurfaceBoundaryConfused);
            if (Surfaces.Any(s => s.MobileConstraints == null || s.MobileConstraints.TouchTargetPixels < MinimumTouchTargetPixels)) findings.Add(MobileNavDiagnosticCode.MobileNavTouchTargetTooSmall);
            if (Surfaces.Any(s => s.MobileConstraints != null && s.MobileConstraints.ProductionFinalClaim)) findings.Add(MobileNavDiagnosticCode.MobileNavProductionFinalClaimForbidden);
            return new MobileNavigationDiagnostics(findings);
        }
    }
    public sealed class MobileNavigationDiagnostics { public MobileNavigationDiagnostics(IReadOnlyList<MobileNavDiagnosticCode> findings) { Findings = findings ?? Array.Empty<MobileNavDiagnosticCode>(); } public IReadOnlyList<MobileNavDiagnosticCode> Findings { get; } public bool Contains(MobileNavDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class MobileNavigationShellResolved { public MobileNavigationShellResolved(string shellId) { ShellId = shellId ?? string.Empty; } public string ShellId { get; } }
    public sealed class MobileNavigationRouteBlocked { public MobileNavigationRouteBlocked(string surfaceId, string targetId) { SurfaceId = surfaceId ?? string.Empty; TargetId = targetId ?? string.Empty; } public string SurfaceId { get; } public string TargetId { get; } }
    public sealed class MobileNavigationSurfaceBoundaryShown { public MobileNavigationSurfaceBoundaryShown(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }

    public enum SurfaceClass { Demo, ProductionFuture, DebugQa, ServerAdmin, MixedForbidden }
    public enum SurfaceAudience { Player, Reviewer, Qa, ServerAdmin, ForbiddenMixed }
    public enum SurfaceSeparationDiagnosticCode { SurfaceClassificationMissing, SurfaceMixedForbidden, SurfaceAdminExposedToPlayer, SurfaceProductionPromiseForbidden, SurfaceVisualMarkerMissing }
    public sealed class SurfaceVisualMarker { public SurfaceVisualMarker(string markerId, bool visible) { MarkerId = markerId ?? string.Empty; Visible = visible; } public string MarkerId { get; } public bool Visible { get; } }
    public sealed class SurfaceForbiddenRoute { public SurfaceForbiddenRoute(string routeId, bool exposedToPlayer = false) { RouteId = routeId ?? string.Empty; ExposedToPlayer = exposedToPlayer; } public string RouteId { get; } public bool ExposedToPlayer { get; } }
    public sealed class SurfacePromiseGuard { public SurfacePromiseGuard(bool productionPromiseClaimed = false, bool finalThemeClaimed = false) { ProductionPromiseClaimed = productionPromiseClaimed; FinalThemeClaimed = finalThemeClaimed; } public bool ProductionPromiseClaimed { get; } public bool FinalThemeClaimed { get; } }
    public sealed class SurfaceClassification
    {
        public SurfaceClassification(string surfaceId, SurfaceClass surfaceClass, SurfaceAudience audience, SurfaceVisualMarker visualMarker, IReadOnlyList<string> allowedRoutes, IReadOnlyList<SurfaceForbiddenRoute> forbiddenRoutes, SurfacePromiseGuard promiseGuard)
        {
            SurfaceId = surfaceId ?? string.Empty; SurfaceClass = surfaceClass; Audience = audience; VisualMarker = visualMarker; AllowedRoutes = allowedRoutes ?? Array.Empty<string>(); ForbiddenRoutes = forbiddenRoutes ?? Array.Empty<SurfaceForbiddenRoute>(); PromiseGuard = promiseGuard;
        }
        public string SurfaceId { get; } public SurfaceClass SurfaceClass { get; } public SurfaceAudience Audience { get; } public SurfaceVisualMarker VisualMarker { get; } public IReadOnlyList<string> AllowedRoutes { get; } public IReadOnlyList<SurfaceForbiddenRoute> ForbiddenRoutes { get; } public SurfacePromiseGuard PromiseGuard { get; }
    }
    public sealed class DemoProductionSurfaceSeparation
    {
        public DemoProductionSurfaceSeparation(string registryId, IReadOnlyList<SurfaceClassification> surfaces) { RegistryId = ColonyIntegrationIds.Require(registryId); Surfaces = surfaces ?? Array.Empty<SurfaceClassification>(); }
        public string RegistryId { get; } public IReadOnlyList<SurfaceClassification> Surfaces { get; }
        public SurfaceSeparationDiagnostics Evaluate()
        {
            var findings = new List<SurfaceSeparationDiagnosticCode>();
            if (Surfaces.Count == 0 || Surfaces.Any(s => string.IsNullOrWhiteSpace(s.SurfaceId))) findings.Add(SurfaceSeparationDiagnosticCode.SurfaceClassificationMissing);
            if (Surfaces.Any(s => s.SurfaceClass == SurfaceClass.MixedForbidden || s.Audience == SurfaceAudience.ForbiddenMixed)) findings.Add(SurfaceSeparationDiagnosticCode.SurfaceMixedForbidden);
            if (Surfaces.Any(s => s.SurfaceClass == SurfaceClass.ServerAdmin && s.Audience == SurfaceAudience.Player) || Surfaces.SelectMany(s => s.ForbiddenRoutes).Any(r => r.ExposedToPlayer)) findings.Add(SurfaceSeparationDiagnosticCode.SurfaceAdminExposedToPlayer);
            if (Surfaces.Any(s => s.PromiseGuard != null && (s.PromiseGuard.ProductionPromiseClaimed || s.PromiseGuard.FinalThemeClaimed))) findings.Add(SurfaceSeparationDiagnosticCode.SurfaceProductionPromiseForbidden);
            if (Surfaces.Any(s => s.VisualMarker == null || !s.VisualMarker.Visible)) findings.Add(SurfaceSeparationDiagnosticCode.SurfaceVisualMarkerMissing);
            return new SurfaceSeparationDiagnostics(findings);
        }
    }
    public sealed class SurfaceSeparationDiagnostics { public SurfaceSeparationDiagnostics(IReadOnlyList<SurfaceSeparationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SurfaceSeparationDiagnosticCode>(); } public IReadOnlyList<SurfaceSeparationDiagnosticCode> Findings { get; } public bool Contains(SurfaceSeparationDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class SurfaceClassified { public SurfaceClassified(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class SurfaceBoundaryViolationDetected { public SurfaceBoundaryViolationDetected(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class SurfacePromiseBlocked { public SurfacePromiseBlocked(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }

    public enum AssetCategory { Hive, World, Army, Alliance, Chat, Notification, Event, Report, NavigationIcon }
    public enum AssetUsageSurface { Demo, ProductionFuture, DebugQa, ReviewTool }
    public enum AssetReadinessStatus { Required, Missing, Temporary, AcceptableForDemo, Blocked }
    public enum AssetReadinessDiagnosticCode { AssetReadinessMissingCategory, AssetTemporaryMarkerMissing, AssetFinalClaimForbidden, AssetMobileUsageMissing, AssetProductionBlockerOpen }
    public sealed class AssetTemporaryMarker { public AssetTemporaryMarker(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class AssetProductionReadinessBlocker { public AssetProductionReadinessBlocker(string blockerId, bool open) { BlockerId = blockerId ?? string.Empty; Open = open; } public string BlockerId { get; } public bool Open { get; } }
    public sealed class AssetReadinessItem
    {
        public AssetReadinessItem(string assetId, AssetCategory? category, AssetUsageSurface usageSurface, string targetResolution, string mobileUsage, AssetReadinessStatus readinessStatus, AssetTemporaryMarker temporaryMarker, IReadOnlyList<AssetProductionReadinessBlocker> blockers, bool finalClaim = false)
        {
            AssetId = assetId ?? string.Empty; Category = category; UsageSurface = usageSurface; TargetResolution = targetResolution ?? string.Empty; MobileUsage = mobileUsage ?? string.Empty; ReadinessStatus = readinessStatus; TemporaryMarker = temporaryMarker; Blockers = blockers ?? Array.Empty<AssetProductionReadinessBlocker>(); FinalClaim = finalClaim;
        }
        public string AssetId { get; } public AssetCategory? Category { get; } public AssetUsageSurface UsageSurface { get; } public string TargetResolution { get; } public string MobileUsage { get; } public AssetReadinessStatus ReadinessStatus { get; } public AssetTemporaryMarker TemporaryMarker { get; } public IReadOnlyList<AssetProductionReadinessBlocker> Blockers { get; } public bool FinalClaim { get; }
    }
    public sealed class ProfessionalAssetReadinessRegistry
    {
        public ProfessionalAssetReadinessRegistry(string registryId, IReadOnlyList<AssetReadinessItem> items) { RegistryId = ColonyIntegrationIds.Require(registryId); Items = items ?? Array.Empty<AssetReadinessItem>(); }
        public string RegistryId { get; } public IReadOnlyList<AssetReadinessItem> Items { get; }
        public AssetReadinessDiagnostics Evaluate()
        {
            var findings = new List<AssetReadinessDiagnosticCode>();
            if (Items.Count == 0 || Items.Any(i => !i.Category.HasValue)) findings.Add(AssetReadinessDiagnosticCode.AssetReadinessMissingCategory);
            if (Items.Any(i => i.TemporaryMarker == null || !i.TemporaryMarker.Visible)) findings.Add(AssetReadinessDiagnosticCode.AssetTemporaryMarkerMissing);
            if (Items.Any(i => i.FinalClaim)) findings.Add(AssetReadinessDiagnosticCode.AssetFinalClaimForbidden);
            if (Items.Any(i => string.IsNullOrWhiteSpace(i.MobileUsage))) findings.Add(AssetReadinessDiagnosticCode.AssetMobileUsageMissing);
            if (Items.SelectMany(i => i.Blockers).Any(b => b.Open)) findings.Add(AssetReadinessDiagnosticCode.AssetProductionBlockerOpen);
            return new AssetReadinessDiagnostics(findings);
        }
    }
    public sealed class AssetReadinessDiagnostics { public AssetReadinessDiagnostics(IReadOnlyList<AssetReadinessDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AssetReadinessDiagnosticCode>(); } public IReadOnlyList<AssetReadinessDiagnosticCode> Findings { get; } public bool Contains(AssetReadinessDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ProfessionalAssetReadinessRegistered { public ProfessionalAssetReadinessRegistered(string assetId) { AssetId = assetId ?? string.Empty; } public string AssetId { get; } }
    public sealed class ProfessionalAssetTemporaryMarkerShown { public ProfessionalAssetTemporaryMarkerShown(string assetId) { AssetId = assetId ?? string.Empty; } public string AssetId { get; } }
    public sealed class ProfessionalAssetFinalClaimBlocked { public ProfessionalAssetFinalClaimBlocked(string assetId) { AssetId = assetId ?? string.Empty; } public string AssetId { get; } }

    public enum HomeHubTargetSurface { Hive, Alliance, Army, World, Chat, Events, Reports, Progression }
    public enum HomeHubDiagnosticCode { HomeHubZoneMissing, HomeHubRuntimeActionForbidden, HomeHubBadgeMisleading, HomeHubMobileOverlapDetected, HomeHubMarketingTextDetected }
    public sealed class HomeHubPrimaryAction { public HomeHubPrimaryAction(string actionId, bool runtimeAction = false) { ActionId = actionId ?? string.Empty; RuntimeAction = runtimeAction; } public string ActionId { get; } public bool RuntimeAction { get; } }
    public sealed class HomeHubStatusBadge { public HomeHubStatusBadge(string badgeId, bool misleading = false) { BadgeId = badgeId ?? string.Empty; Misleading = misleading; } public string BadgeId { get; } public bool Misleading { get; } }
    public sealed class HomeHubAlertSlot { public HomeHubAlertSlot(string alertId, bool visible) { AlertId = alertId ?? string.Empty; Visible = visible; } public string AlertId { get; } public bool Visible { get; } }
    public sealed class HomeHubRuntimeLimitMarker { public HomeHubRuntimeLimitMarker(string markerText, bool visible) { MarkerText = markerText ?? string.Empty; Visible = visible; } public string MarkerText { get; } public bool Visible { get; } }
    public sealed class HomeHubZone
    {
        public HomeHubZone(string zoneId, int priority, HomeHubTargetSurface targetSurface, HomeHubPrimaryAction primaryAction, HomeHubStatusBadge statusBadge, HomeHubRuntimeLimitMarker runtimeLimitMarker, string mobileLayoutRule, bool mobileOverlap = false, bool marketingTextDetected = false)
        {
            ZoneId = zoneId ?? string.Empty; Priority = priority; TargetSurface = targetSurface; PrimaryAction = primaryAction; StatusBadge = statusBadge; RuntimeLimitMarker = runtimeLimitMarker; MobileLayoutRule = mobileLayoutRule ?? string.Empty; MobileOverlap = mobileOverlap; MarketingTextDetected = marketingTextDetected;
        }
        public string ZoneId { get; } public int Priority { get; } public HomeHubTargetSurface TargetSurface { get; } public HomeHubPrimaryAction PrimaryAction { get; } public HomeHubStatusBadge StatusBadge { get; } public HomeHubRuntimeLimitMarker RuntimeLimitMarker { get; } public string MobileLayoutRule { get; } public bool MobileOverlap { get; } public bool MarketingTextDetected { get; }
    }
    public sealed class SocialMmoHomeHubUxContract
    {
        private static readonly HomeHubTargetSurface[] RequiredSurfaces = { HomeHubTargetSurface.Hive, HomeHubTargetSurface.Alliance, HomeHubTargetSurface.Army, HomeHubTargetSurface.World, HomeHubTargetSurface.Chat, HomeHubTargetSurface.Events, HomeHubTargetSurface.Reports, HomeHubTargetSurface.Progression };
        public SocialMmoHomeHubUxContract(string contractId, IReadOnlyList<HomeHubZone> zones, IReadOnlyList<HomeHubAlertSlot> alertSlots) { ContractId = ColonyIntegrationIds.Require(contractId); Zones = zones ?? Array.Empty<HomeHubZone>(); AlertSlots = alertSlots ?? Array.Empty<HomeHubAlertSlot>(); }
        public string ContractId { get; } public IReadOnlyList<HomeHubZone> Zones { get; } public IReadOnlyList<HomeHubAlertSlot> AlertSlots { get; }
        public HomeHubDiagnostics Evaluate()
        {
            var findings = new List<HomeHubDiagnosticCode>();
            if (RequiredSurfaces.Any(s => !Zones.Any(z => z.TargetSurface == s)) || Zones.Any(z => string.IsNullOrWhiteSpace(z.ZoneId))) findings.Add(HomeHubDiagnosticCode.HomeHubZoneMissing);
            if (Zones.Any(z => z.PrimaryAction != null && z.PrimaryAction.RuntimeAction)) findings.Add(HomeHubDiagnosticCode.HomeHubRuntimeActionForbidden);
            if (Zones.Any(z => z.StatusBadge == null || z.StatusBadge.Misleading)) findings.Add(HomeHubDiagnosticCode.HomeHubBadgeMisleading);
            if (Zones.Any(z => z.MobileOverlap || string.IsNullOrWhiteSpace(z.MobileLayoutRule))) findings.Add(HomeHubDiagnosticCode.HomeHubMobileOverlapDetected);
            if (Zones.Any(z => z.MarketingTextDetected)) findings.Add(HomeHubDiagnosticCode.HomeHubMarketingTextDetected);
            return new HomeHubDiagnostics(findings);
        }
    }
    public sealed class HomeHubDiagnostics { public HomeHubDiagnostics(IReadOnlyList<HomeHubDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HomeHubDiagnosticCode>(); } public IReadOnlyList<HomeHubDiagnosticCode> Findings { get; } public bool Contains(HomeHubDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class SocialMmoHomeHubResolved { public SocialMmoHomeHubResolved(string contractId) { ContractId = contractId ?? string.Empty; } public string ContractId { get; } }
    public sealed class SocialMmoHomeHubActionBlocked { public SocialMmoHomeHubActionBlocked(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }
    public sealed class SocialMmoHomeHubLimitShown { public SocialMmoHomeHubLimitShown(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }

    public enum SocialEntryTab { Alliance, WorldChat, ServerChat, AllianceChat, SystemMessages, Notifications }
    public enum ChatChannelKind { Alliance, World, Server, System, Notification }
    public enum SocialEntryDiagnosticCode { SocialEntryTabMissing, ChatLiveActivationForbidden, UnreadStateOfficialForbidden, ModerationFinalClaimForbidden, SocialSearchRuntimeForbidden }
    public sealed class UnreadStatePreview { public UnreadStatePreview(int count, bool official = false) { Count = count; Official = official; } public int Count { get; } public bool Official { get; } }
    public sealed class ModerationVisibleLimit { public ModerationVisibleLimit(string limitText, bool finalClaim = false) { LimitText = limitText ?? string.Empty; FinalClaim = finalClaim; } public string LimitText { get; } public bool FinalClaim { get; } }
    public sealed class SocialSearchFutureMarker { public SocialSearchFutureMarker(bool runtimeSearchRequested = false) { RuntimeSearchRequested = runtimeSearchRequested; } public bool RuntimeSearchRequested { get; } }
    public sealed class ChatChannelPreview
    {
        public ChatChannelPreview(string channelId, ChatChannelKind channelKind, SocialEntryTab tab, UnreadStatePreview unreadStatePreview, ModerationVisibleLimit moderationLimit, SocialSearchFutureMarker searchFutureMarker, bool runtimeBlocked)
        {
            ChannelId = channelId ?? string.Empty; ChannelKind = channelKind; Tab = tab; UnreadStatePreview = unreadStatePreview; ModerationLimit = moderationLimit; SearchFutureMarker = searchFutureMarker; RuntimeBlocked = runtimeBlocked;
        }
        public string ChannelId { get; } public ChatChannelKind ChannelKind { get; } public SocialEntryTab Tab { get; } public UnreadStatePreview UnreadStatePreview { get; } public ModerationVisibleLimit ModerationLimit { get; } public SocialSearchFutureMarker SearchFutureMarker { get; } public bool RuntimeBlocked { get; }
    }
    public sealed class AllianceChatMobileEntryContract
    {
        private static readonly SocialEntryTab[] RequiredTabs = { SocialEntryTab.Alliance, SocialEntryTab.WorldChat, SocialEntryTab.ServerChat, SocialEntryTab.AllianceChat, SocialEntryTab.SystemMessages, SocialEntryTab.Notifications };
        public AllianceChatMobileEntryContract(string contractId, IReadOnlyList<ChatChannelPreview> channels) { ContractId = ColonyIntegrationIds.Require(contractId); Channels = channels ?? Array.Empty<ChatChannelPreview>(); }
        public string ContractId { get; } public IReadOnlyList<ChatChannelPreview> Channels { get; }
        public SocialEntryDiagnostics Evaluate()
        {
            var findings = new List<SocialEntryDiagnosticCode>();
            if (RequiredTabs.Any(t => !Channels.Any(c => c.Tab == t))) findings.Add(SocialEntryDiagnosticCode.SocialEntryTabMissing);
            if (Channels.Any(c => !c.RuntimeBlocked)) findings.Add(SocialEntryDiagnosticCode.ChatLiveActivationForbidden);
            if (Channels.Any(c => c.UnreadStatePreview != null && c.UnreadStatePreview.Official)) findings.Add(SocialEntryDiagnosticCode.UnreadStateOfficialForbidden);
            if (Channels.Any(c => c.ModerationLimit == null || c.ModerationLimit.FinalClaim)) findings.Add(SocialEntryDiagnosticCode.ModerationFinalClaimForbidden);
            if (Channels.Any(c => c.SearchFutureMarker != null && c.SearchFutureMarker.RuntimeSearchRequested)) findings.Add(SocialEntryDiagnosticCode.SocialSearchRuntimeForbidden);
            return new SocialEntryDiagnostics(findings);
        }
    }
    public sealed class SocialEntryDiagnostics { public SocialEntryDiagnostics(IReadOnlyList<SocialEntryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialEntryDiagnosticCode>(); } public IReadOnlyList<SocialEntryDiagnosticCode> Findings { get; } public bool Contains(SocialEntryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class AllianceChatMobileEntryViewed { public AllianceChatMobileEntryViewed(string contractId) { ContractId = contractId ?? string.Empty; } public string ContractId { get; } }
    public sealed class AllianceChatMobileEntryBlocked { public AllianceChatMobileEntryBlocked(string channelId) { ChannelId = channelId ?? string.Empty; } public string ChannelId { get; } }
    public sealed class AllianceChatModerationLimitShown { public AllianceChatModerationLimitShown(string channelId) { ChannelId = channelId ?? string.Empty; } public string ChannelId { get; } }

    public enum WarActionIntentKind { TrainArmy, InspectReadiness, PrepareDefense, InspectAttackOptions, PlanRally, ReviewConflictReport }
    public enum ArmyWarDiagnosticCode { ArmyEntryUnitCardMissing, WarRuntimeCombatForbidden, ArmyOfficialScoreForbidden, PvpProtectionHidden, WarServerAuthorityRequired }
    public sealed class ArmyUnitCardPreview { public ArmyUnitCardPreview(string unitFamilyId, bool visible) { UnitFamilyId = unitFamilyId ?? string.Empty; Visible = visible; } public string UnitFamilyId { get; } public bool Visible { get; } }
    public sealed class PvpWarningMarker { public PvpWarningMarker(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class BeginnerProtectionMarker { public BeginnerProtectionMarker(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class WarServerAuthorityBlocker { public WarServerAuthorityBlocker(bool required, bool visible) { Required = required; Visible = visible; } public bool Required { get; } public bool Visible { get; } }
    public sealed class WarActionIntentPreview
    {
        public WarActionIntentPreview(string intentId, WarActionIntentKind intentKind, WarServerAuthorityBlocker requiredServerAuthority, PvpWarningMarker pvpWarning, BeginnerProtectionMarker beginnerProtection, bool runtimeBlocked, IReadOnlyList<string> assetNeeds, bool combatRuntimeRequested = false, bool officialScoreClaimed = false)
        {
            IntentId = intentId ?? string.Empty; IntentKind = intentKind; RequiredServerAuthority = requiredServerAuthority; PvpWarning = pvpWarning; BeginnerProtection = beginnerProtection; RuntimeBlocked = runtimeBlocked; AssetNeeds = assetNeeds ?? Array.Empty<string>(); CombatRuntimeRequested = combatRuntimeRequested; OfficialScoreClaimed = officialScoreClaimed;
        }
        public string IntentId { get; } public WarActionIntentKind IntentKind { get; } public WarServerAuthorityBlocker RequiredServerAuthority { get; } public PvpWarningMarker PvpWarning { get; } public BeginnerProtectionMarker BeginnerProtection { get; } public bool RuntimeBlocked { get; } public IReadOnlyList<string> AssetNeeds { get; } public bool CombatRuntimeRequested { get; } public bool OfficialScoreClaimed { get; }
    }
    public sealed class ArmyWarReadinessMobileEntry
    {
        public ArmyWarReadinessMobileEntry(string entryId, IReadOnlyList<ArmyUnitCardPreview> unitCards, IReadOnlyList<WarActionIntentPreview> intents) { EntryId = ColonyIntegrationIds.Require(entryId); UnitCards = unitCards ?? Array.Empty<ArmyUnitCardPreview>(); Intents = intents ?? Array.Empty<WarActionIntentPreview>(); }
        public string EntryId { get; } public IReadOnlyList<ArmyUnitCardPreview> UnitCards { get; } public IReadOnlyList<WarActionIntentPreview> Intents { get; }
        public ArmyWarDiagnostics Evaluate()
        {
            var findings = new List<ArmyWarDiagnosticCode>();
            if (UnitCards.Count == 0 || UnitCards.Any(c => string.IsNullOrWhiteSpace(c.UnitFamilyId) || !c.Visible) || Intents.Any(i => i.AssetNeeds.Count == 0)) findings.Add(ArmyWarDiagnosticCode.ArmyEntryUnitCardMissing);
            if (Intents.Any(i => i.CombatRuntimeRequested || !i.RuntimeBlocked)) findings.Add(ArmyWarDiagnosticCode.WarRuntimeCombatForbidden);
            if (Intents.Any(i => i.OfficialScoreClaimed)) findings.Add(ArmyWarDiagnosticCode.ArmyOfficialScoreForbidden);
            if (Intents.Any(i => i.PvpWarning == null || !i.PvpWarning.Visible || i.BeginnerProtection == null || !i.BeginnerProtection.Visible)) findings.Add(ArmyWarDiagnosticCode.PvpProtectionHidden);
            if (Intents.Any(i => i.RequiredServerAuthority == null || i.RequiredServerAuthority.Required && !i.RequiredServerAuthority.Visible)) findings.Add(ArmyWarDiagnosticCode.WarServerAuthorityRequired);
            return new ArmyWarDiagnostics(findings);
        }
    }
    public sealed class ArmyWarDiagnostics { public ArmyWarDiagnostics(IReadOnlyList<ArmyWarDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyWarDiagnosticCode>(); } public IReadOnlyList<ArmyWarDiagnosticCode> Findings { get; } public bool Contains(ArmyWarDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ArmyWarMobileEntryViewed { public ArmyWarMobileEntryViewed(string entryId) { EntryId = entryId ?? string.Empty; } public string EntryId { get; } }
    public sealed class ArmyWarIntentBlocked { public ArmyWarIntentBlocked(string intentId) { IntentId = intentId ?? string.Empty; } public string IntentId { get; } }
    public sealed class PvpWarningDisplayed { public PvpWarningDisplayed(string intentId) { IntentId = intentId ?? string.Empty; } public string IntentId { get; } }

    public enum WorldMapMarkerKind { TerritoryPreview, RoutePreview, EventPreview, AllianceInterest, RiskMarker, ServerDependency }
    public enum EventJournalPreviewType { WorldEvent, Territory, Route, Risk, ServerDependency }
    public enum WorldMapDiagnosticCode { WorldMapMarkerMissing, WorldMapEventLiveForbidden, WorldMapTerritoryOfficialForbidden, WorldMapPerformanceLimitHidden, WorldJournalFilterMissing }
    public sealed class WorldMapMarkerPreview { public WorldMapMarkerPreview(string markerId, WorldMapMarkerKind markerKind, bool runtimeLimitVisible, bool officialTerritory = false) { MarkerId = markerId ?? string.Empty; MarkerKind = markerKind; RuntimeLimitVisible = runtimeLimitVisible; OfficialTerritory = officialTerritory; } public string MarkerId { get; } public WorldMapMarkerKind MarkerKind { get; } public bool RuntimeLimitVisible { get; } public bool OfficialTerritory { get; } }
    public sealed class WorldMapFilterSet { public WorldMapFilterSet(IReadOnlyList<string> filterIds) { FilterIds = filterIds ?? Array.Empty<string>(); } public IReadOnlyList<string> FilterIds { get; } }
    public sealed class WorldMapZoomRule { public WorldMapZoomRule(string ruleId, bool performanceLimitVisible) { RuleId = ruleId ?? string.Empty; PerformanceLimitVisible = performanceLimitVisible; } public string RuleId { get; } public bool PerformanceLimitVisible { get; } }
    public sealed class WorldEventRuntimeLimit { public WorldEventRuntimeLimit(bool visible, bool liveEventRequested = false) { Visible = visible; LiveEventRequested = liveEventRequested; } public bool Visible { get; } public bool LiveEventRequested { get; } }
    public sealed class EventJournalEntryPreview
    {
        public EventJournalEntryPreview(string eventId, EventJournalPreviewType previewType, IReadOnlyList<string> markerRefs, WorldMapFilterSet filters, WorldEventRuntimeLimit runtimeLimit, OperationsSourceServerDependency serverDependency, int mobilePriority)
        {
            EventId = eventId ?? string.Empty; PreviewType = previewType; MarkerRefs = markerRefs ?? Array.Empty<string>(); Filters = filters; RuntimeLimit = runtimeLimit; ServerDependency = serverDependency; MobilePriority = mobilePriority;
        }
        public string EventId { get; } public EventJournalPreviewType PreviewType { get; } public IReadOnlyList<string> MarkerRefs { get; } public WorldMapFilterSet Filters { get; } public WorldEventRuntimeLimit RuntimeLimit { get; } public OperationsSourceServerDependency ServerDependency { get; } public int MobilePriority { get; }
    }
    public sealed class WorldMapEventJournalUxContract
    {
        public WorldMapEventJournalUxContract(string contractId, IReadOnlyList<WorldMapMarkerPreview> markers, WorldMapFilterSet filters, IReadOnlyList<EventJournalEntryPreview> entries, IReadOnlyList<WorldMapZoomRule> zoomRules) { ContractId = ColonyIntegrationIds.Require(contractId); Markers = markers ?? Array.Empty<WorldMapMarkerPreview>(); Filters = filters; Entries = entries ?? Array.Empty<EventJournalEntryPreview>(); ZoomRules = zoomRules ?? Array.Empty<WorldMapZoomRule>(); }
        public string ContractId { get; } public IReadOnlyList<WorldMapMarkerPreview> Markers { get; } public WorldMapFilterSet Filters { get; } public IReadOnlyList<EventJournalEntryPreview> Entries { get; } public IReadOnlyList<WorldMapZoomRule> ZoomRules { get; }
        public WorldMapDiagnostics Evaluate()
        {
            var findings = new List<WorldMapDiagnosticCode>();
            if (Markers.Count == 0 || Markers.Any(m => string.IsNullOrWhiteSpace(m.MarkerId) || !m.RuntimeLimitVisible)) findings.Add(WorldMapDiagnosticCode.WorldMapMarkerMissing);
            if (Entries.Any(e => e.RuntimeLimit != null && e.RuntimeLimit.LiveEventRequested)) findings.Add(WorldMapDiagnosticCode.WorldMapEventLiveForbidden);
            if (Markers.Any(m => m.OfficialTerritory)) findings.Add(WorldMapDiagnosticCode.WorldMapTerritoryOfficialForbidden);
            if (ZoomRules.Count == 0 || ZoomRules.Any(z => !z.PerformanceLimitVisible)) findings.Add(WorldMapDiagnosticCode.WorldMapPerformanceLimitHidden);
            if (Filters == null || Filters.FilterIds.Count == 0 || Entries.Any(e => e.Filters == null || e.Filters.FilterIds.Count == 0)) findings.Add(WorldMapDiagnosticCode.WorldJournalFilterMissing);
            return new WorldMapDiagnostics(findings);
        }
    }
    public sealed class WorldMapDiagnostics { public WorldMapDiagnostics(IReadOnlyList<WorldMapDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WorldMapDiagnosticCode>(); } public IReadOnlyList<WorldMapDiagnosticCode> Findings { get; } public bool Contains(WorldMapDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class WorldMapUxViewed { public WorldMapUxViewed(string contractId) { ContractId = contractId ?? string.Empty; } public string ContractId { get; } }
    public sealed class WorldEventJournalEntryInspected { public WorldEventJournalEntryInspected(string eventId) { EventId = eventId ?? string.Empty; } public string EventId { get; } }
    public sealed class WorldMapRuntimeClaimBlocked { public WorldMapRuntimeClaimBlocked(string eventId) { EventId = eventId ?? string.Empty; } public string EventId { get; } }

    public enum ReadabilityGateVerdict { AcceptedForExternalDemo, AcceptedWithWarnings, BlockedByReadability, BlockedByAccessibility, BlockedByTouchTarget, BlockedByLocalizationRisk, BlockedByFinalPolishClaim }
    public enum ReadabilityGateDiagnosticCode { ReadabilityTextTooSmall, ReadabilityOverlapDetected, AccessibilityContrastInsufficient, TouchTargetTooSmall, LocalizationRiskOpen, FinalPolishClaimForbidden }
    public sealed class ReadabilityCriterion
    {
        public ReadabilityCriterion(string criterionId, string targetSurface, bool requiredState, string failureReason, string mobileImpact, bool demoBlocking, int textSizePx = 16, bool overlap = false, bool contrastAcceptable = true, int touchTargetPixels = 44, bool localizationRisk = false, bool finalPolishClaim = false)
        {
            CriterionId = criterionId ?? string.Empty; TargetSurface = targetSurface ?? string.Empty; RequiredState = requiredState; FailureReason = failureReason ?? string.Empty; MobileImpact = mobileImpact ?? string.Empty; DemoBlocking = demoBlocking; TextSizePx = textSizePx; Overlap = overlap; ContrastAcceptable = contrastAcceptable; TouchTargetPixels = touchTargetPixels; LocalizationRisk = localizationRisk; FinalPolishClaim = finalPolishClaim;
        }
        public string CriterionId { get; } public string TargetSurface { get; } public bool RequiredState { get; } public string FailureReason { get; } public string MobileImpact { get; } public bool DemoBlocking { get; } public int TextSizePx { get; } public bool Overlap { get; } public bool ContrastAcceptable { get; } public int TouchTargetPixels { get; } public bool LocalizationRisk { get; } public bool FinalPolishClaim { get; }
    }
    public sealed class OperationsReadabilityAccessibilityGate
    {
        public OperationsReadabilityAccessibilityGate(string gateId, IReadOnlyList<ReadabilityCriterion> criteria) { GateId = ColonyIntegrationIds.Require(gateId); Criteria = criteria ?? Array.Empty<ReadabilityCriterion>(); }
        public string GateId { get; } public IReadOnlyList<ReadabilityCriterion> Criteria { get; }
        public ReadabilityGateDiagnostics Evaluate()
        {
            var findings = new List<ReadabilityGateDiagnosticCode>();
            if (Criteria.Any(c => c.TextSizePx < 14)) findings.Add(ReadabilityGateDiagnosticCode.ReadabilityTextTooSmall);
            if (Criteria.Any(c => c.Overlap)) findings.Add(ReadabilityGateDiagnosticCode.ReadabilityOverlapDetected);
            if (Criteria.Any(c => !c.ContrastAcceptable)) findings.Add(ReadabilityGateDiagnosticCode.AccessibilityContrastInsufficient);
            if (Criteria.Any(c => c.TouchTargetPixels < MobileNavigationShellContract.MinimumTouchTargetPixels)) findings.Add(ReadabilityGateDiagnosticCode.TouchTargetTooSmall);
            if (Criteria.Any(c => c.LocalizationRisk)) findings.Add(ReadabilityGateDiagnosticCode.LocalizationRiskOpen);
            if (Criteria.Any(c => c.FinalPolishClaim)) findings.Add(ReadabilityGateDiagnosticCode.FinalPolishClaimForbidden);
            return new ReadabilityGateDiagnostics(ResolveVerdict(findings), findings);
        }
        private static ReadabilityGateVerdict ResolveVerdict(IReadOnlyList<ReadabilityGateDiagnosticCode> findings)
        {
            if (findings.Contains(ReadabilityGateDiagnosticCode.FinalPolishClaimForbidden)) return ReadabilityGateVerdict.BlockedByFinalPolishClaim;
            if (findings.Contains(ReadabilityGateDiagnosticCode.LocalizationRiskOpen)) return ReadabilityGateVerdict.BlockedByLocalizationRisk;
            if (findings.Contains(ReadabilityGateDiagnosticCode.TouchTargetTooSmall)) return ReadabilityGateVerdict.BlockedByTouchTarget;
            if (findings.Contains(ReadabilityGateDiagnosticCode.AccessibilityContrastInsufficient)) return ReadabilityGateVerdict.BlockedByAccessibility;
            if (findings.Contains(ReadabilityGateDiagnosticCode.ReadabilityTextTooSmall) || findings.Contains(ReadabilityGateDiagnosticCode.ReadabilityOverlapDetected)) return ReadabilityGateVerdict.BlockedByReadability;
            return findings.Count == 0 ? ReadabilityGateVerdict.AcceptedForExternalDemo : ReadabilityGateVerdict.AcceptedWithWarnings;
        }
    }
    public sealed class ReadabilityGateDiagnostics { public ReadabilityGateDiagnostics(ReadabilityGateVerdict verdict, IReadOnlyList<ReadabilityGateDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<ReadabilityGateDiagnosticCode>(); } public ReadabilityGateVerdict Verdict { get; } public IReadOnlyList<ReadabilityGateDiagnosticCode> Findings { get; } public bool Contains(ReadabilityGateDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class OperationsReadabilityEvaluated { public OperationsReadabilityEvaluated(string gateId) { GateId = gateId ?? string.Empty; } public string GateId { get; } }
    public sealed class OperationsReadabilityBlocked { public OperationsReadabilityBlocked(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class OperationsAccessibilityWarningRaised { public OperationsAccessibilityWarningRaised(string criterionId) { CriterionId = criterionId ?? string.Empty; } public string CriterionId { get; } }

    public enum ScaleOperationsEntryVerdict { ReadyForArchitectValidation, ReadyWithUiWarnings, NeedsPlannerRevision, BlockedByMobileGap, BlockedBySurfaceConfusion, BlockedByAssetReadinessGap, BlockedByRuntimeClaim, BlockedByBee411Premature }
    public enum Bee411BlockerState { Blocked, AttemptBlocked, AwaitingArchitectValidation }
    public enum ScaleOperationsEntryDiagnosticCode { ScaleEntryInputMissing, ScaleEntryMobileGapOpen, ScaleEntrySurfaceConfusionOpen, ScaleEntryAssetGapOpen, ScaleEntryRuntimeClaimDetected, Bee411Premature }
    public sealed class ScaleOperationsEntryInput
    {
        public ScaleOperationsEntryInput(ScaleOperationsSourceInventory sourceInventory, MobileNavigationShellContract mobileNavigationShell, DemoProductionSurfaceSeparation surfaceSeparation, ProfessionalAssetReadinessRegistry assetRegistry, SocialMmoHomeHubUxContract homeHubUx, AllianceChatMobileEntryContract allianceChatEntry, ArmyWarReadinessMobileEntry armyWarEntry, WorldMapEventJournalUxContract worldMapJournal, OperationsReadabilityAccessibilityGate readabilityGate)
        {
            SourceInventory = sourceInventory; MobileNavigationShell = mobileNavigationShell; SurfaceSeparation = surfaceSeparation; AssetRegistry = assetRegistry; HomeHubUx = homeHubUx; AllianceChatEntry = allianceChatEntry; ArmyWarEntry = armyWarEntry; WorldMapJournal = worldMapJournal; ReadabilityGate = readabilityGate;
        }
        public ScaleOperationsSourceInventory SourceInventory { get; } public MobileNavigationShellContract MobileNavigationShell { get; } public DemoProductionSurfaceSeparation SurfaceSeparation { get; } public ProfessionalAssetReadinessRegistry AssetRegistry { get; } public SocialMmoHomeHubUxContract HomeHubUx { get; } public AllianceChatMobileEntryContract AllianceChatEntry { get; } public ArmyWarReadinessMobileEntry ArmyWarEntry { get; } public WorldMapEventJournalUxContract WorldMapJournal { get; } public OperationsReadabilityAccessibilityGate ReadabilityGate { get; }
    }
    public sealed class ScaleOperationsEntryCoverage { public ScaleOperationsEntryCoverage(bool mobileGapOpen = false, bool surfaceConfusionOpen = false, bool assetGapOpen = false, bool runtimeClaimDetected = false) { MobileGapOpen = mobileGapOpen; SurfaceConfusionOpen = surfaceConfusionOpen; AssetGapOpen = assetGapOpen; RuntimeClaimDetected = runtimeClaimDetected; } public bool MobileGapOpen { get; } public bool SurfaceConfusionOpen { get; } public bool AssetGapOpen { get; } public bool RuntimeClaimDetected { get; } }
    public sealed class ScaleOperationsEntryBlocker { public ScaleOperationsEntryBlocker(string blockerId, bool runtimeClaim = false) { BlockerId = blockerId ?? string.Empty; RuntimeClaim = runtimeClaim; } public string BlockerId { get; } public bool RuntimeClaim { get; } }
    public sealed class Bee411BlockerStatusForScale { public Bee411BlockerStatusForScale(Bee411BlockerState state, string message) { State = state; Message = message ?? string.Empty; } public Bee411BlockerState State { get; } public string Message { get; } }
    public sealed class ScaleOperationsEntryClosureGate
    {
        public const string Bee411BlockedMessage = "BEE-411 bloquee jusqu'a validation architecte.";
        public ScaleOperationsEntryClosureGate(string gateId, ScaleOperationsEntryInput input, ScaleOperationsEntryCoverage coverage, IReadOnlyList<ScaleOperationsEntryBlocker> blockers, Bee411BlockerStatusForScale bee411BlockerStatus)
        {
            GateId = ColonyIntegrationIds.Require(gateId); Input = input; Coverage = coverage ?? new ScaleOperationsEntryCoverage(); Blockers = blockers ?? Array.Empty<ScaleOperationsEntryBlocker>(); Bee411BlockerStatus = bee411BlockerStatus ?? new Bee411BlockerStatusForScale(Bee411BlockerState.Blocked, Bee411BlockedMessage);
        }
        public string GateId { get; } public ScaleOperationsEntryInput Input { get; } public ScaleOperationsEntryCoverage Coverage { get; } public IReadOnlyList<ScaleOperationsEntryBlocker> Blockers { get; } public Bee411BlockerStatusForScale Bee411BlockerStatus { get; }
        public ScaleOperationsEntryDiagnostics Evaluate()
        {
            var findings = new List<ScaleOperationsEntryDiagnosticCode>();
            if (Input == null || Input.SourceInventory == null || Input.MobileNavigationShell == null || Input.SurfaceSeparation == null || Input.AssetRegistry == null || Input.HomeHubUx == null || Input.AllianceChatEntry == null || Input.ArmyWarEntry == null || Input.WorldMapJournal == null || Input.ReadabilityGate == null) findings.Add(ScaleOperationsEntryDiagnosticCode.ScaleEntryInputMissing);
            if (Coverage.MobileGapOpen) findings.Add(ScaleOperationsEntryDiagnosticCode.ScaleEntryMobileGapOpen);
            if (Coverage.SurfaceConfusionOpen) findings.Add(ScaleOperationsEntryDiagnosticCode.ScaleEntrySurfaceConfusionOpen);
            if (Coverage.AssetGapOpen) findings.Add(ScaleOperationsEntryDiagnosticCode.ScaleEntryAssetGapOpen);
            if (Coverage.RuntimeClaimDetected || Blockers.Any(b => b.RuntimeClaim)) findings.Add(ScaleOperationsEntryDiagnosticCode.ScaleEntryRuntimeClaimDetected);
            if (Bee411BlockerStatus.State == Bee411BlockerState.AttemptBlocked) findings.Add(ScaleOperationsEntryDiagnosticCode.Bee411Premature);
            return new ScaleOperationsEntryDiagnostics(ResolveVerdict(findings), findings);
        }
        private static ScaleOperationsEntryVerdict ResolveVerdict(IReadOnlyList<ScaleOperationsEntryDiagnosticCode> findings)
        {
            if (findings.Contains(ScaleOperationsEntryDiagnosticCode.Bee411Premature)) return ScaleOperationsEntryVerdict.BlockedByBee411Premature;
            if (findings.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryRuntimeClaimDetected)) return ScaleOperationsEntryVerdict.BlockedByRuntimeClaim;
            if (findings.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryAssetGapOpen)) return ScaleOperationsEntryVerdict.BlockedByAssetReadinessGap;
            if (findings.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntrySurfaceConfusionOpen)) return ScaleOperationsEntryVerdict.BlockedBySurfaceConfusion;
            if (findings.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryMobileGapOpen)) return ScaleOperationsEntryVerdict.BlockedByMobileGap;
            if (findings.Contains(ScaleOperationsEntryDiagnosticCode.ScaleEntryInputMissing)) return ScaleOperationsEntryVerdict.NeedsPlannerRevision;
            return findings.Count == 0 ? ScaleOperationsEntryVerdict.ReadyForArchitectValidation : ScaleOperationsEntryVerdict.ReadyWithUiWarnings;
        }
    }
    public sealed class ScaleOperationsEntryDiagnostics { public ScaleOperationsEntryDiagnostics(ScaleOperationsEntryVerdict verdict, IReadOnlyList<ScaleOperationsEntryDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<ScaleOperationsEntryDiagnosticCode>(); } public ScaleOperationsEntryVerdict Verdict { get; } public IReadOnlyList<ScaleOperationsEntryDiagnosticCode> Findings { get; } public bool Contains(ScaleOperationsEntryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ScaleOperationsEntryClosureEvaluated { public ScaleOperationsEntryClosureEvaluated(string gateId) { GateId = gateId ?? string.Empty; } public string GateId { get; } }
    public sealed class ScaleOperationsEntryClosureBlocked { public ScaleOperationsEntryClosureBlocked(string blockerId) { BlockerId = blockerId ?? string.Empty; } public string BlockerId { get; } }
    public sealed class Bee411PrematureAttemptBlocked { public Bee411PrematureAttemptBlocked(string message) { Message = message ?? string.Empty; } public string Message { get; } }
}
