using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum ReturnPriorityHint { Low, Normal, High, CriticalPreview }
    public enum ReturnServerDependency { None, AccountFuture, SessionFuture, NotificationFuture, SocialFuture, WorldFuture, ArmyFuture }
    public enum PlayerReturnDiagnosticCode { ReturnNodeMissing, ReturnRouteDeadEnd, ReturnReasonMissing, ReturnPushNotificationForbidden, ReturnServerDependencyHidden }
    public sealed class ReturnNetworkNode { public ReturnNetworkNode(string nodeId, bool homeAccessible = false) { NodeId = nodeId ?? string.Empty; HomeAccessible = homeAccessible; } public string NodeId { get; } public bool HomeAccessible { get; } }
    public sealed class ReturnReasonPreview { public ReturnReasonPreview(string text, bool pushClaim = false) { Text = text ?? string.Empty; PushClaim = pushClaim; } public string Text { get; } public bool PushClaim { get; } }
    public sealed class ReturnNetworkRoute
    {
        public ReturnNetworkRoute(string routeId, string fromNode, string toNode, ReturnReasonPreview returnReason, ReturnPriorityHint priorityHint, ReturnServerDependency serverDependency, string fallbackRoute, bool serverDependencyVisible = true)
        { RouteId = routeId ?? string.Empty; FromNode = fromNode ?? string.Empty; ToNode = toNode ?? string.Empty; ReturnReason = returnReason; PriorityHint = priorityHint; ServerDependency = serverDependency; FallbackRoute = fallbackRoute ?? string.Empty; ServerDependencyVisible = serverDependencyVisible; }
        public string RouteId { get; } public string FromNode { get; } public string ToNode { get; } public ReturnReasonPreview ReturnReason { get; } public ReturnPriorityHint PriorityHint { get; } public ReturnServerDependency ServerDependency { get; } public string FallbackRoute { get; } public bool ServerDependencyVisible { get; }
    }
    public sealed class PlayerReturnNetwork
    {
        private static readonly string[] RequiredNodes = { "home", "ruche", "objectif", "alliance", "chat", "armee", "monde", "journal", "feedback" };
        public PlayerReturnNetwork(string networkId, IReadOnlyList<ReturnNetworkNode> nodes, IReadOnlyList<ReturnNetworkRoute> routes) { NetworkId = ColonyIntegrationIds.Require(networkId); Nodes = nodes ?? Array.Empty<ReturnNetworkNode>(); Routes = routes ?? Array.Empty<ReturnNetworkRoute>(); }
        public string NetworkId { get; } public IReadOnlyList<ReturnNetworkNode> Nodes { get; } public IReadOnlyList<ReturnNetworkRoute> Routes { get; }
        public PlayerReturnDiagnostics Evaluate()
        {
            var findings = new List<PlayerReturnDiagnosticCode>();
            if (RequiredNodes.Any(id => Nodes.All(n => !string.Equals(n.NodeId, id, StringComparison.OrdinalIgnoreCase)))) findings.Add(PlayerReturnDiagnosticCode.ReturnNodeMissing);
            if (Routes.Count == 0 || Nodes.Any(n => !string.Equals(n.NodeId, "home", StringComparison.OrdinalIgnoreCase) && Routes.All(r => !string.Equals(r.FromNode, n.NodeId, StringComparison.OrdinalIgnoreCase)))) findings.Add(PlayerReturnDiagnosticCode.ReturnRouteDeadEnd);
            if (Routes.Any(r => r.ReturnReason == null || string.IsNullOrWhiteSpace(r.ReturnReason.Text))) findings.Add(PlayerReturnDiagnosticCode.ReturnReasonMissing);
            if (Routes.Any(r => r.ReturnReason != null && r.ReturnReason.PushClaim)) findings.Add(PlayerReturnDiagnosticCode.ReturnPushNotificationForbidden);
            if (Routes.Any(r => r.ServerDependency != ReturnServerDependency.None && !r.ServerDependencyVisible)) findings.Add(PlayerReturnDiagnosticCode.ReturnServerDependencyHidden);
            return new PlayerReturnDiagnostics(findings);
        }
    }
    public sealed class PlayerReturnDiagnostics { public PlayerReturnDiagnostics(IReadOnlyList<PlayerReturnDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlayerReturnDiagnosticCode>(); } public IReadOnlyList<PlayerReturnDiagnosticCode> Findings { get; } public bool Contains(PlayerReturnDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class PlayerReturnNetworkBuilt { public PlayerReturnNetworkBuilt(string networkId) { NetworkId = networkId ?? string.Empty; } public string NetworkId { get; } }
    public sealed class PlayerReturnRouteInspected { public PlayerReturnRouteInspected(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class PlayerReturnReasonBlocked { public PlayerReturnReasonBlocked(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }

    public enum HomeReturnDiagnosticCode { HomeReturnMissing, SafeExitContextLost, UnsavedPreviewMessageMissing, HomeReturnRuntimeSaveClaim, ExitRouteOverConfirmation }
    public sealed class ReturnContextSnapshot { public ReturnContextSnapshot(string summary, bool contextLost = false) { Summary = summary ?? string.Empty; ContextLost = contextLost; } public string Summary { get; } public bool ContextLost { get; } }
    public sealed class UnsavedPreviewNotice { public UnsavedPreviewNotice(string text, bool visible, bool runtimeSaveClaim = false) { Text = text ?? string.Empty; Visible = visible; RuntimeSaveClaim = runtimeSaveClaim; } public string Text { get; } public bool Visible { get; } public bool RuntimeSaveClaim { get; } }
    public sealed class HomeReturnFallback { public HomeReturnFallback(string destination) { Destination = destination ?? string.Empty; } public string Destination { get; } }
    public sealed class ExitServerDependency { public ExitServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class SafeExitAction
    {
        public SafeExitAction(string surfaceId, string destination, ReturnContextSnapshot contextSummary, UnsavedPreviewNotice unsavedPreviewNotice, HomeReturnFallback fallbackDestination, ExitServerDependency serverDependency, int confirmationCount = 0)
        { SurfaceId = surfaceId ?? string.Empty; Destination = destination ?? string.Empty; ContextSummary = contextSummary; UnsavedPreviewNotice = unsavedPreviewNotice; FallbackDestination = fallbackDestination; ServerDependency = serverDependency; ConfirmationCount = confirmationCount; }
        public string SurfaceId { get; } public string Destination { get; } public ReturnContextSnapshot ContextSummary { get; } public UnsavedPreviewNotice UnsavedPreviewNotice { get; } public HomeReturnFallback FallbackDestination { get; } public ExitServerDependency ServerDependency { get; } public int ConfirmationCount { get; }
    }
    public sealed class HomeReturnRoute
    {
        public HomeReturnRoute(string routeId, IReadOnlyList<SafeExitAction> exits) { RouteId = ColonyIntegrationIds.Require(routeId); Exits = exits ?? Array.Empty<SafeExitAction>(); }
        public string RouteId { get; } public IReadOnlyList<SafeExitAction> Exits { get; }
        public HomeReturnDiagnostics Evaluate()
        {
            var findings = new List<HomeReturnDiagnosticCode>();
            if (Exits.Count == 0 || Exits.Any(e => !string.Equals(e.Destination, "home", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(e.SurfaceId) || e.FallbackDestination == null || string.IsNullOrWhiteSpace(e.FallbackDestination.Destination))) findings.Add(HomeReturnDiagnosticCode.HomeReturnMissing);
            if (Exits.Any(e => e.ContextSummary == null || e.ContextSummary.ContextLost || string.IsNullOrWhiteSpace(e.ContextSummary.Summary))) findings.Add(HomeReturnDiagnosticCode.SafeExitContextLost);
            if (Exits.Any(e => e.UnsavedPreviewNotice == null || !e.UnsavedPreviewNotice.Visible || string.IsNullOrWhiteSpace(e.UnsavedPreviewNotice.Text))) findings.Add(HomeReturnDiagnosticCode.UnsavedPreviewMessageMissing);
            if (Exits.Any(e => e.UnsavedPreviewNotice != null && e.UnsavedPreviewNotice.RuntimeSaveClaim)) findings.Add(HomeReturnDiagnosticCode.HomeReturnRuntimeSaveClaim);
            if (Exits.Any(e => e.ConfirmationCount > 1)) findings.Add(HomeReturnDiagnosticCode.ExitRouteOverConfirmation);
            return new HomeReturnDiagnostics(findings);
        }
    }
    public sealed class HomeReturnDiagnostics { public HomeReturnDiagnostics(IReadOnlyList<HomeReturnDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HomeReturnDiagnosticCode>(); } public IReadOnlyList<HomeReturnDiagnosticCode> Findings { get; } public bool Contains(HomeReturnDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HomeReturnRouteShown { public HomeReturnRouteShown(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class SafeExitTriggered { public SafeExitTriggered(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class ReturnContextRestored { public ReturnContextRestored(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }

    public enum RecapServerDependency { None, SessionHistoryFuture, AnalyticsFuture, RewardFuture }
    public enum SessionRecapDiagnosticCode { SessionRecapProgressClaim, SessionRecapRewardForbidden, SessionRecapPrivacyRisk, NextReturnPromptMissing, RecapServerDependencyHidden }
    public sealed class RecapVisitedSurface { public RecapVisitedSurface(string surfaceId, bool personalData = false) { SurfaceId = surfaceId ?? string.Empty; PersonalData = personalData; } public string SurfaceId { get; } public bool PersonalData { get; } }
    public sealed class NextReturnPrompt { public NextReturnPrompt(string routeId, string text) { RouteId = routeId ?? string.Empty; Text = text ?? string.Empty; } public string RouteId { get; } public string Text { get; } }
    public sealed class RecapProgressClaimGuard { public RecapProgressClaimGuard(bool progressClaim, bool rewardClaim) { ProgressClaim = progressClaim; RewardClaim = rewardClaim; } public bool ProgressClaim { get; } public bool RewardClaim { get; } }
    public sealed class RecapPrivacyNotice { public RecapPrivacyNotice(bool visible, bool privacyRisk = false) { Visible = visible; PrivacyRisk = privacyRisk; } public bool Visible { get; } public bool PrivacyRisk { get; } }
    public sealed class SessionRecapPreview
    {
        public SessionRecapPreview(string recapId, IReadOnlyList<RecapVisitedSurface> visitedSurfaces, string learnedSummary, NextReturnPrompt nextReturnPrompt, RecapPrivacyNotice privacyNotice, RecapServerDependency serverDependency, bool serverDependencyVisible = true, RecapProgressClaimGuard claimGuard = null)
        { RecapId = ColonyIntegrationIds.Require(recapId); VisitedSurfaces = visitedSurfaces ?? Array.Empty<RecapVisitedSurface>(); LearnedSummary = learnedSummary ?? string.Empty; NextReturnPrompt = nextReturnPrompt; PrivacyNotice = privacyNotice; ServerDependency = serverDependency; ServerDependencyVisible = serverDependencyVisible; ClaimGuard = claimGuard ?? new RecapProgressClaimGuard(false, false); }
        public string RecapId { get; } public IReadOnlyList<RecapVisitedSurface> VisitedSurfaces { get; } public string LearnedSummary { get; } public NextReturnPrompt NextReturnPrompt { get; } public RecapPrivacyNotice PrivacyNotice { get; } public RecapServerDependency ServerDependency { get; } public bool ServerDependencyVisible { get; } public RecapProgressClaimGuard ClaimGuard { get; }
        public SessionRecapDiagnostics Evaluate()
        {
            var findings = new List<SessionRecapDiagnosticCode>();
            if (ClaimGuard.ProgressClaim) findings.Add(SessionRecapDiagnosticCode.SessionRecapProgressClaim);
            if (ClaimGuard.RewardClaim) findings.Add(SessionRecapDiagnosticCode.SessionRecapRewardForbidden);
            if (PrivacyNotice == null || !PrivacyNotice.Visible || PrivacyNotice.PrivacyRisk || VisitedSurfaces.Any(v => v.PersonalData)) findings.Add(SessionRecapDiagnosticCode.SessionRecapPrivacyRisk);
            if (NextReturnPrompt == null || string.IsNullOrWhiteSpace(NextReturnPrompt.RouteId) || string.IsNullOrWhiteSpace(NextReturnPrompt.Text)) findings.Add(SessionRecapDiagnosticCode.NextReturnPromptMissing);
            if (ServerDependency != RecapServerDependency.None && !ServerDependencyVisible) findings.Add(SessionRecapDiagnosticCode.RecapServerDependencyHidden);
            return new SessionRecapDiagnostics(findings);
        }
    }
    public sealed class SessionRecapDiagnostics { public SessionRecapDiagnostics(IReadOnlyList<SessionRecapDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SessionRecapDiagnosticCode>(); } public IReadOnlyList<SessionRecapDiagnosticCode> Findings { get; } public bool Contains(SessionRecapDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class SessionRecapPreviewShown { public SessionRecapPreviewShown(string recapId) { RecapId = recapId ?? string.Empty; } public string RecapId { get; } }
    public sealed class NextReturnPromptInspected { public NextReturnPromptInspected(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class SessionRecapDismissed { public SessionRecapDismissed(string recapId) { RecapId = recapId ?? string.Empty; } public string RecapId { get; } }

    public enum ReturnNotificationKind { Hive, Alliance, Chat, Army, World, Goal, PreviewSystem }
    public enum NotificationDiagnosticCode { NotificationPushForbidden, UnreadOfficialClaimForbidden, NotificationRouteMissing, NotificationPersonalDataRisk, NotificationServerDependencyHidden }
    public sealed class NotificationRouteTarget { public NotificationRouteTarget(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class UnreadOfficialClaimGuard { public UnreadOfficialClaimGuard(bool officialUnreadClaim, bool pushLiveClaim = false) { OfficialUnreadClaim = officialUnreadClaim; PushLiveClaim = pushLiveClaim; } public bool OfficialUnreadClaim { get; } public bool PushLiveClaim { get; } }
    public sealed class NotificationServerDependency { public NotificationServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class NotificationPreviewExpiry { public NotificationPreviewExpiry(string hint) { Hint = hint ?? string.Empty; } public string Hint { get; } }
    public sealed class ReturnNotificationItem
    {
        public ReturnNotificationItem(string notificationId, ReturnNotificationKind kind, string previewText, NotificationRouteTarget routeTarget, UnreadOfficialClaimGuard unreadStatePreview, NotificationPreviewExpiry expiryHint, NotificationServerDependency serverDependency, bool personalData = false)
        { NotificationId = notificationId ?? string.Empty; Kind = kind; PreviewText = previewText ?? string.Empty; RouteTarget = routeTarget; UnreadStatePreview = unreadStatePreview; ExpiryHint = expiryHint; ServerDependency = serverDependency; PersonalData = personalData; }
        public string NotificationId { get; } public ReturnNotificationKind Kind { get; } public string PreviewText { get; } public NotificationRouteTarget RouteTarget { get; } public UnreadOfficialClaimGuard UnreadStatePreview { get; } public NotificationPreviewExpiry ExpiryHint { get; } public NotificationServerDependency ServerDependency { get; } public bool PersonalData { get; }
    }
    public sealed class NotificationReturnLoopPreview
    {
        public NotificationReturnLoopPreview(string previewId, IReadOnlyList<ReturnNotificationItem> items) { PreviewId = ColonyIntegrationIds.Require(previewId); Items = items ?? Array.Empty<ReturnNotificationItem>(); }
        public string PreviewId { get; } public IReadOnlyList<ReturnNotificationItem> Items { get; }
        public NotificationDiagnostics Evaluate()
        {
            var findings = new List<NotificationDiagnosticCode>();
            if (Items.Any(i => i.UnreadStatePreview != null && i.UnreadStatePreview.PushLiveClaim)) findings.Add(NotificationDiagnosticCode.NotificationPushForbidden);
            if (Items.Any(i => i.UnreadStatePreview != null && i.UnreadStatePreview.OfficialUnreadClaim)) findings.Add(NotificationDiagnosticCode.UnreadOfficialClaimForbidden);
            if (Items.Count == 0 || Items.Any(i => i.RouteTarget == null || string.IsNullOrWhiteSpace(i.RouteTarget.SurfaceId))) findings.Add(NotificationDiagnosticCode.NotificationRouteMissing);
            if (Items.Any(i => i.PersonalData)) findings.Add(NotificationDiagnosticCode.NotificationPersonalDataRisk);
            if (Items.Any(i => i.ServerDependency == null || !i.ServerDependency.Visible)) findings.Add(NotificationDiagnosticCode.NotificationServerDependencyHidden);
            return new NotificationDiagnostics(findings);
        }
    }
    public sealed class NotificationDiagnostics { public NotificationDiagnostics(IReadOnlyList<NotificationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<NotificationDiagnosticCode>(); } public IReadOnlyList<NotificationDiagnosticCode> Findings { get; } public bool Contains(NotificationDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class NotificationReturnPreviewShown { public NotificationReturnPreviewShown(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class NotificationRouteFollowed { public NotificationRouteFollowed(string notificationId) { NotificationId = notificationId ?? string.Empty; } public string NotificationId { get; } }
    public sealed class NotificationPreviewBlocked { public NotificationPreviewBlocked(string notificationId) { NotificationId = notificationId ?? string.Empty; } public string NotificationId { get; } }

    public enum HelpNeedKind { Resource, Defense, WorldOpportunity, StyleAdvice, OnboardingHelp }
    public enum AllianceHelpDiagnosticCode { AllianceHelpLiveSendForbidden, AllianceMembershipRequiredHidden, HelpReturnRouteMissing, HelpJournalEntryMissing, AllianceHelpServerDependencyHidden }
    public sealed class HelpNeedPreview { public HelpNeedPreview(HelpNeedKind needKind, string reason) { NeedKind = needKind; Reason = reason ?? string.Empty; } public HelpNeedKind NeedKind { get; } public string Reason { get; } }
    public sealed class HelpReturnRoute { public HelpReturnRoute(string allianceRoute, string chatRoute) { AllianceRoute = allianceRoute ?? string.Empty; ChatRoute = chatRoute ?? string.Empty; } public string AllianceRoute { get; } public string ChatRoute { get; } }
    public sealed class HelpJournalPreviewEntry { public HelpJournalPreviewEntry(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class AllianceHelpServerDependency { public AllianceHelpServerDependency(bool visible, bool membershipVisible = true) { Visible = visible; MembershipVisible = membershipVisible; } public bool Visible { get; } public bool MembershipVisible { get; } }
    public sealed class AllianceHelpRequestPreview
    {
        public AllianceHelpRequestPreview(string requestId, HelpNeedKind needKind, string playerMessagePreview, HelpReturnRoute returnRoute, HelpJournalPreviewEntry journalEntryPreview, AllianceHelpServerDependency serverDependency, bool liveSendRequested = false, bool personalMessage = false)
        { RequestId = requestId ?? string.Empty; NeedKind = needKind; PlayerMessagePreview = playerMessagePreview ?? string.Empty; ReturnRoute = returnRoute; JournalEntryPreview = journalEntryPreview; ServerDependency = serverDependency; LiveSendRequested = liveSendRequested; PersonalMessage = personalMessage; }
        public string RequestId { get; } public HelpNeedKind NeedKind { get; } public string PlayerMessagePreview { get; } public HelpReturnRoute ReturnRoute { get; } public HelpJournalPreviewEntry JournalEntryPreview { get; } public AllianceHelpServerDependency ServerDependency { get; } public bool LiveSendRequested { get; } public bool PersonalMessage { get; }
    }
    public sealed class AllianceHelpReturnLoop
    {
        public AllianceHelpReturnLoop(string loopId, IReadOnlyList<AllianceHelpRequestPreview> requests) { LoopId = ColonyIntegrationIds.Require(loopId); Requests = requests ?? Array.Empty<AllianceHelpRequestPreview>(); }
        public string LoopId { get; } public IReadOnlyList<AllianceHelpRequestPreview> Requests { get; }
        public AllianceHelpDiagnostics Evaluate()
        {
            var findings = new List<AllianceHelpDiagnosticCode>();
            if (Requests.Any(r => r.LiveSendRequested)) findings.Add(AllianceHelpDiagnosticCode.AllianceHelpLiveSendForbidden);
            if (Requests.Any(r => r.ServerDependency == null || !r.ServerDependency.MembershipVisible)) findings.Add(AllianceHelpDiagnosticCode.AllianceMembershipRequiredHidden);
            if (Requests.Count == 0 || Requests.Any(r => r.ReturnRoute == null || string.IsNullOrWhiteSpace(r.ReturnRoute.AllianceRoute) || string.IsNullOrWhiteSpace(r.ReturnRoute.ChatRoute))) findings.Add(AllianceHelpDiagnosticCode.HelpReturnRouteMissing);
            if (Requests.Any(r => r.JournalEntryPreview == null || !r.JournalEntryPreview.Visible || string.IsNullOrWhiteSpace(r.JournalEntryPreview.Text))) findings.Add(AllianceHelpDiagnosticCode.HelpJournalEntryMissing);
            if (Requests.Any(r => r.ServerDependency == null || !r.ServerDependency.Visible)) findings.Add(AllianceHelpDiagnosticCode.AllianceHelpServerDependencyHidden);
            return new AllianceHelpDiagnostics(findings);
        }
    }
    public sealed class AllianceHelpDiagnostics { public AllianceHelpDiagnostics(IReadOnlyList<AllianceHelpDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceHelpDiagnosticCode>(); } public IReadOnlyList<AllianceHelpDiagnosticCode> Findings { get; } public bool Contains(AllianceHelpDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class AllianceHelpLoopOpened { public AllianceHelpLoopOpened(string loopId) { LoopId = loopId ?? string.Empty; } public string LoopId { get; } }
    public sealed class AllianceHelpRequestPreviewed { public AllianceHelpRequestPreviewed(string requestId) { RequestId = requestId ?? string.Empty; } public string RequestId { get; } }
    public sealed class AllianceHelpReturnFollowed { public AllianceHelpReturnFollowed(string requestId) { RequestId = requestId ?? string.Empty; } public string RequestId { get; } }

    public enum HiveNeedKind { Resource, Population, Chamber, InternalDefense, UpgradePriority, AllianceHelpFuture }
    public enum HiveReturnPriority { Low, Medium, High, PreviewUrgent }
    public enum HiveNeedSurfaceOrigin { Home, Goals, Recap, Notification, Alliance, World }
    public enum HiveNeedDiagnosticCode { HiveNeedReasonMissing, HiveNeedRuntimeActionForbidden, HiveNeedReturnRouteMissing, HiveNeedCostOfficialClaim, HiveNeedServerDependencyHidden }
    public sealed class HiveNeedPreviewReason { public HiveNeedPreviewReason(string text, bool alarmist = false) { Text = text ?? string.Empty; Alarmist = alarmist; } public string Text { get; } public bool Alarmist { get; } }
    public sealed class HiveNeedBlocker { public HiveNeedBlocker(string blockedAction, bool runtimeAction = false, bool officialCostClaim = false) { BlockedAction = blockedAction ?? string.Empty; RuntimeAction = runtimeAction; OfficialCostClaim = officialCostClaim; } public string BlockedAction { get; } public bool RuntimeAction { get; } public bool OfficialCostClaim { get; } }
    public sealed class HiveNeedServerDependency { public HiveNeedServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveNeedReturnSignal
    {
        public HiveNeedReturnSignal(string signalId, HiveNeedKind needKind, HiveNeedSurfaceOrigin originSurface, HiveNeedPreviewReason playerReason, HiveReturnPriority returnPriority, string hiveRoute, HiveNeedBlocker blockedAction, HiveNeedServerDependency serverDependency)
        { SignalId = signalId ?? string.Empty; NeedKind = needKind; OriginSurface = originSurface; PlayerReason = playerReason; ReturnPriority = returnPriority; HiveRoute = hiveRoute ?? string.Empty; BlockedAction = blockedAction; ServerDependency = serverDependency; }
        public string SignalId { get; } public HiveNeedKind NeedKind { get; } public HiveNeedSurfaceOrigin OriginSurface { get; } public HiveNeedPreviewReason PlayerReason { get; } public HiveReturnPriority ReturnPriority { get; } public string HiveRoute { get; } public HiveNeedBlocker BlockedAction { get; } public HiveNeedServerDependency ServerDependency { get; }
        public HiveNeedDiagnostics Evaluate()
        {
            var findings = new List<HiveNeedDiagnosticCode>();
            if (PlayerReason == null || string.IsNullOrWhiteSpace(PlayerReason.Text) || PlayerReason.Alarmist) findings.Add(HiveNeedDiagnosticCode.HiveNeedReasonMissing);
            if (BlockedAction != null && BlockedAction.RuntimeAction) findings.Add(HiveNeedDiagnosticCode.HiveNeedRuntimeActionForbidden);
            if (string.IsNullOrWhiteSpace(HiveRoute)) findings.Add(HiveNeedDiagnosticCode.HiveNeedReturnRouteMissing);
            if (BlockedAction != null && BlockedAction.OfficialCostClaim) findings.Add(HiveNeedDiagnosticCode.HiveNeedCostOfficialClaim);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(HiveNeedDiagnosticCode.HiveNeedServerDependencyHidden);
            return new HiveNeedDiagnostics(findings);
        }
    }
    public sealed class HiveNeedDiagnostics { public HiveNeedDiagnostics(IReadOnlyList<HiveNeedDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveNeedDiagnosticCode>(); } public IReadOnlyList<HiveNeedDiagnosticCode> Findings { get; } public bool Contains(HiveNeedDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveNeedSignalShown { public HiveNeedSignalShown(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class HiveNeedReturnFollowed { public HiveNeedReturnFollowed(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class HiveNeedActionBlocked { public HiveNeedActionBlocked(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }

    public enum WorldReturnSignalKind { Threat, Opportunity, Route, Resource, Ally, Defense, FutureEvent }
    public enum WorldReturnRouteTarget { World, Army, Alliance, Hive, Journal, Home }
    public enum WorldDiagnosticCode { WorldReturnActionForbidden, WorldThreatLiveClaim, WorldOpportunityRewardClaim, WorldReturnRouteMissing, WorldServerDependencyHidden }
    public sealed class WorldActionBlocker { public WorldActionBlocker(string action, bool forbiddenRuntimeAction = false, bool liveThreatClaim = false, bool rewardClaim = false) { Action = action ?? string.Empty; ForbiddenRuntimeAction = forbiddenRuntimeAction; LiveThreatClaim = liveThreatClaim; RewardClaim = rewardClaim; } public string Action { get; } public bool ForbiddenRuntimeAction { get; } public bool LiveThreatClaim { get; } public bool RewardClaim { get; } }
    public sealed class WorldServerDependency { public WorldServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class WorldReturnSignal
    {
        public WorldReturnSignal(string signalId, WorldReturnSignalKind signalKind, string markerMeaning, WorldReturnRouteTarget? suggestedSurface, WorldActionBlocker blockedWorldAction, string freshnessPreview, WorldServerDependency serverDependency)
        { SignalId = signalId ?? string.Empty; SignalKind = signalKind; MarkerMeaning = markerMeaning ?? string.Empty; SuggestedSurface = suggestedSurface; BlockedWorldAction = blockedWorldAction; FreshnessPreview = freshnessPreview ?? string.Empty; ServerDependency = serverDependency; }
        public string SignalId { get; } public WorldReturnSignalKind SignalKind { get; } public string MarkerMeaning { get; } public WorldReturnRouteTarget? SuggestedSurface { get; } public WorldActionBlocker BlockedWorldAction { get; } public string FreshnessPreview { get; } public WorldServerDependency ServerDependency { get; }
        public WorldDiagnostics Evaluate()
        {
            var findings = new List<WorldDiagnosticCode>();
            if (BlockedWorldAction != null && BlockedWorldAction.ForbiddenRuntimeAction) findings.Add(WorldDiagnosticCode.WorldReturnActionForbidden);
            if (BlockedWorldAction != null && BlockedWorldAction.LiveThreatClaim) findings.Add(WorldDiagnosticCode.WorldThreatLiveClaim);
            if (BlockedWorldAction != null && BlockedWorldAction.RewardClaim) findings.Add(WorldDiagnosticCode.WorldOpportunityRewardClaim);
            if (SuggestedSurface == null || string.IsNullOrWhiteSpace(MarkerMeaning)) findings.Add(WorldDiagnosticCode.WorldReturnRouteMissing);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(WorldDiagnosticCode.WorldServerDependencyHidden);
            return new WorldDiagnostics(findings);
        }
    }
    public sealed class WorldThreatReturnPreview { public WorldThreatReturnPreview(WorldReturnSignal signal) { Signal = signal; } public WorldReturnSignal Signal { get; } }
    public sealed class WorldOpportunityReturnPreview { public WorldOpportunityReturnPreview(WorldReturnSignal signal) { Signal = signal; } public WorldReturnSignal Signal { get; } }
    public sealed class WorldDiagnostics { public WorldDiagnostics(IReadOnlyList<WorldDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WorldDiagnosticCode>(); } public IReadOnlyList<WorldDiagnosticCode> Findings { get; } public bool Contains(WorldDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class WorldReturnSignalShown { public WorldReturnSignalShown(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class WorldReturnRouteFollowed { public WorldReturnRouteFollowed(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class WorldActionBlocked { public WorldActionBlocked(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }

    public enum ArmyDiagnosticCode { ArmyReadinessOfficialClaim, ArmyTrainingRuntimeForbidden, ArmyLossRewardForbidden, ArmyReturnRouteMissing, ArmyServerDependencyHidden }
    public sealed class UnitFamilyReturnHint { public UnitFamilyReturnHint(string family, bool aggressiveLanguage = false) { Family = family ?? string.Empty; AggressiveLanguage = aggressiveLanguage; } public string Family { get; } public bool AggressiveLanguage { get; } }
    public sealed class DefenseReadinessPreview { public DefenseReadinessPreview(string readinessHint, bool officialClaim = false) { ReadinessHint = readinessHint ?? string.Empty; OfficialClaim = officialClaim; } public string ReadinessHint { get; } public bool OfficialClaim { get; } }
    public sealed class PvpRiskReturnNotice { public PvpRiskReturnNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class ArmyTrainingActionBlocker { public ArmyTrainingActionBlocker(string action, bool runtimeTraining = false, bool lossClaim = false, bool rewardClaim = false) { Action = action ?? string.Empty; RuntimeTraining = runtimeTraining; LossClaim = lossClaim; RewardClaim = rewardClaim; } public string Action { get; } public bool RuntimeTraining { get; } public bool LossClaim { get; } public bool RewardClaim { get; } }
    public sealed class ArmyServerDependency { public ArmyServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class ArmyReadinessReturnSignal
    {
        public ArmyReadinessReturnSignal(string signalId, DefenseReadinessPreview readinessHint, UnitFamilyReturnHint unitFamily, string relatedWorldSignal, string suggestedReturnRoute, ArmyTrainingActionBlocker blockedAction, ArmyServerDependency serverDependency, PvpRiskReturnNotice pvpRiskNotice = null)
        { SignalId = signalId ?? string.Empty; ReadinessHint = readinessHint; UnitFamily = unitFamily; RelatedWorldSignal = relatedWorldSignal ?? string.Empty; SuggestedReturnRoute = suggestedReturnRoute ?? string.Empty; BlockedAction = blockedAction; ServerDependency = serverDependency; PvpRiskNotice = pvpRiskNotice; }
        public string SignalId { get; } public DefenseReadinessPreview ReadinessHint { get; } public UnitFamilyReturnHint UnitFamily { get; } public string RelatedWorldSignal { get; } public string SuggestedReturnRoute { get; } public ArmyTrainingActionBlocker BlockedAction { get; } public ArmyServerDependency ServerDependency { get; } public PvpRiskReturnNotice PvpRiskNotice { get; }
        public ArmyDiagnostics Evaluate()
        {
            var findings = new List<ArmyDiagnosticCode>();
            if (ReadinessHint == null || ReadinessHint.OfficialClaim) findings.Add(ArmyDiagnosticCode.ArmyReadinessOfficialClaim);
            if (BlockedAction != null && BlockedAction.RuntimeTraining) findings.Add(ArmyDiagnosticCode.ArmyTrainingRuntimeForbidden);
            if (BlockedAction != null && (BlockedAction.LossClaim || BlockedAction.RewardClaim)) findings.Add(ArmyDiagnosticCode.ArmyLossRewardForbidden);
            if (string.IsNullOrWhiteSpace(SuggestedReturnRoute) || UnitFamily == null || string.IsNullOrWhiteSpace(UnitFamily.Family)) findings.Add(ArmyDiagnosticCode.ArmyReturnRouteMissing);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(ArmyDiagnosticCode.ArmyServerDependencyHidden);
            return new ArmyDiagnostics(findings);
        }
    }
    public sealed class ArmyDiagnostics { public ArmyDiagnostics(IReadOnlyList<ArmyDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyDiagnosticCode>(); } public IReadOnlyList<ArmyDiagnosticCode> Findings { get; } public bool Contains(ArmyDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ArmyReadinessSignalShown { public ArmyReadinessSignalShown(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class ArmyReturnRouteFollowed { public ArmyReturnRouteFollowed(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class ArmyTrainingActionBlocked { public ArmyTrainingActionBlocked(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }

    public enum ReturnAccessibilityVerdict { Pass, PassWithWarnings, BlockedByLabel, BlockedByAmbiguousTarget, BlockedByTouchTarget, BlockedByOverlap, BlockedByCertificationClaim }
    public enum AccessibilityDiagnosticCode { ReturnLabelTooLong, ReturnTargetAmbiguous, ReturnTouchTargetTooSmall, ReturnControlOverlap, ReturnAccessibilityCertificationClaim }
    public sealed class ReturnTargetLabelRule { public ReturnTargetLabelRule(string destinationLabel, bool ambiguous = false) { DestinationLabel = destinationLabel ?? string.Empty; Ambiguous = ambiguous; } public string DestinationLabel { get; } public bool Ambiguous { get; } }
    public sealed class ReturnTouchTargetNeed { public ReturnTouchTargetNeed(int sizeDp) { SizeDp = sizeDp; } public int SizeDp { get; } }
    public sealed class ReturnContrastNeed { public ReturnContrastNeed(float contrastRatio) { ContrastRatio = contrastRatio; } public float ContrastRatio { get; } }
    public sealed class ReturnControlReadabilityRule
    {
        public ReturnControlReadabilityRule(string controlId, ReturnTargetLabelRule destinationLabel, int maxTextConcern, ReturnTouchTargetNeed touchTargetNeed, ReturnContrastNeed contrastNeed, bool overlapStatus, ReturnAccessibilityVerdict verdict, bool certificationClaim = false)
        { ControlId = controlId ?? string.Empty; DestinationLabel = destinationLabel; MaxTextConcern = maxTextConcern; TouchTargetNeed = touchTargetNeed; ContrastNeed = contrastNeed; OverlapStatus = overlapStatus; Verdict = verdict; CertificationClaim = certificationClaim; }
        public string ControlId { get; } public ReturnTargetLabelRule DestinationLabel { get; } public int MaxTextConcern { get; } public ReturnTouchTargetNeed TouchTargetNeed { get; } public ReturnContrastNeed ContrastNeed { get; } public bool OverlapStatus { get; } public ReturnAccessibilityVerdict Verdict { get; } public bool CertificationClaim { get; }
    }
    public sealed class MobileReturnNavigationAccessibility
    {
        public MobileReturnNavigationAccessibility(string auditId, IReadOnlyList<ReturnControlReadabilityRule> controls) { AuditId = ColonyIntegrationIds.Require(auditId); Controls = controls ?? Array.Empty<ReturnControlReadabilityRule>(); }
        public string AuditId { get; } public IReadOnlyList<ReturnControlReadabilityRule> Controls { get; }
        public AccessibilityDiagnostics Evaluate()
        {
            var findings = new List<AccessibilityDiagnosticCode>();
            if (Controls.Any(c => c.DestinationLabel == null || c.DestinationLabel.DestinationLabel.Length > c.MaxTextConcern)) findings.Add(AccessibilityDiagnosticCode.ReturnLabelTooLong);
            if (Controls.Any(c => c.DestinationLabel == null || c.DestinationLabel.Ambiguous || string.IsNullOrWhiteSpace(c.DestinationLabel.DestinationLabel))) findings.Add(AccessibilityDiagnosticCode.ReturnTargetAmbiguous);
            if (Controls.Any(c => c.TouchTargetNeed == null || c.TouchTargetNeed.SizeDp < 44)) findings.Add(AccessibilityDiagnosticCode.ReturnTouchTargetTooSmall);
            if (Controls.Any(c => c.OverlapStatus)) findings.Add(AccessibilityDiagnosticCode.ReturnControlOverlap);
            if (Controls.Any(c => c.CertificationClaim || c.Verdict == ReturnAccessibilityVerdict.BlockedByCertificationClaim)) findings.Add(AccessibilityDiagnosticCode.ReturnAccessibilityCertificationClaim);
            return new AccessibilityDiagnostics(ResolveVerdict(findings), findings);
        }
        private static ReturnAccessibilityVerdict ResolveVerdict(IReadOnlyList<AccessibilityDiagnosticCode> findings)
        {
            if (findings.Contains(AccessibilityDiagnosticCode.ReturnAccessibilityCertificationClaim)) return ReturnAccessibilityVerdict.BlockedByCertificationClaim;
            if (findings.Contains(AccessibilityDiagnosticCode.ReturnControlOverlap)) return ReturnAccessibilityVerdict.BlockedByOverlap;
            if (findings.Contains(AccessibilityDiagnosticCode.ReturnTouchTargetTooSmall)) return ReturnAccessibilityVerdict.BlockedByTouchTarget;
            if (findings.Contains(AccessibilityDiagnosticCode.ReturnTargetAmbiguous)) return ReturnAccessibilityVerdict.BlockedByAmbiguousTarget;
            if (findings.Contains(AccessibilityDiagnosticCode.ReturnLabelTooLong)) return ReturnAccessibilityVerdict.BlockedByLabel;
            return ReturnAccessibilityVerdict.Pass;
        }
    }
    public sealed class AccessibilityDiagnostics { public AccessibilityDiagnostics(ReturnAccessibilityVerdict verdict, IReadOnlyList<AccessibilityDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<AccessibilityDiagnosticCode>(); } public ReturnAccessibilityVerdict Verdict { get; } public IReadOnlyList<AccessibilityDiagnosticCode> Findings { get; } public bool Contains(AccessibilityDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ReturnNavigationAccessibilityChecked { public ReturnNavigationAccessibilityChecked(string auditId) { AuditId = auditId ?? string.Empty; } public string AuditId { get; } }
    public sealed class ReturnControlIssueDetected { public ReturnControlIssueDetected(string controlId) { ControlId = controlId ?? string.Empty; } public string ControlId { get; } }
    public sealed class ReturnAccessibilityVerdictBuilt { public ReturnAccessibilityVerdictBuilt(ReturnAccessibilityVerdict verdict) { Verdict = verdict; } public ReturnAccessibilityVerdict Verdict { get; } }

    public enum ReturnLoopVerdict { ReadyForArchitectValidation, ReadyWithDemoEvidenceReserve, NeedsPlannerRevision, BlockedByDeadEndSurface, BlockedByHiddenServerDependency, BlockedByLiveNotificationClaim, BlockedByAccessibilityGap, BlockedByBee441Premature }
    public enum ReturnClosureDiagnosticCode { ReturnNetworkNodeGap, ReturnLoopDeadEnd, ReturnLiveClaimDetected, ReturnAccessibilityGap, Bee441PrematureRelease }
    public sealed class ReturnDemoEvidenceNeed { public ReturnDemoEvidenceNeed(string text, bool reserveVisible) { Text = text ?? string.Empty; ReserveVisible = reserveVisible; } public string Text { get; } public bool ReserveVisible { get; } }
    public sealed class ReturnServerBoundaryAudit { public ReturnServerBoundaryAudit(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class Bee441BlockerStatus { public Bee441BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class ReturnNetworkCoverageMatrix
    {
        public ReturnNetworkCoverageMatrix(string beeId, string returnSurface, string returnReason, string demoCheck, string uiCheck, string qaCheck, string serverBoundary, ReturnLoopVerdict verdict)
        { BeeId = beeId ?? string.Empty; ReturnSurface = returnSurface ?? string.Empty; ReturnReason = returnReason ?? string.Empty; DemoCheck = demoCheck ?? string.Empty; UiCheck = uiCheck ?? string.Empty; QaCheck = qaCheck ?? string.Empty; ServerBoundary = serverBoundary ?? string.Empty; Verdict = verdict; }
        public string BeeId { get; } public string ReturnSurface { get; } public string ReturnReason { get; } public string DemoCheck { get; } public string UiCheck { get; } public string QaCheck { get; } public string ServerBoundary { get; } public ReturnLoopVerdict Verdict { get; }
    }
    public sealed class PlayerReturnNetworkClosureGate
    {
        public const string Bee441BlockedMessage = "BEE-441 bloquee jusqu'a validation architecte.";
        public PlayerReturnNetworkClosureGate(string gateId, IReadOnlyList<ReturnNetworkCoverageMatrix> coverage, ReturnDemoEvidenceNeed demoEvidenceNeed, ReturnServerBoundaryAudit serverBoundaryAudit, Bee441BlockerStatus bee441BlockerStatus)
        { GateId = ColonyIntegrationIds.Require(gateId); Coverage = coverage ?? Array.Empty<ReturnNetworkCoverageMatrix>(); DemoEvidenceNeed = demoEvidenceNeed; ServerBoundaryAudit = serverBoundaryAudit; Bee441BlockerStatus = bee441BlockerStatus ?? new Bee441BlockerStatus(false, Bee441BlockedMessage); }
        public string GateId { get; } public IReadOnlyList<ReturnNetworkCoverageMatrix> Coverage { get; } public ReturnDemoEvidenceNeed DemoEvidenceNeed { get; } public ReturnServerBoundaryAudit ServerBoundaryAudit { get; } public Bee441BlockerStatus Bee441BlockerStatus { get; }
        public ReturnClosureDiagnostics Evaluate()
        {
            var findings = new List<ReturnClosureDiagnosticCode>();
            if (Coverage.Count < 9 || Coverage.Any(c => string.IsNullOrWhiteSpace(c.BeeId) || string.IsNullOrWhiteSpace(c.ReturnSurface) || string.IsNullOrWhiteSpace(c.ReturnReason))) findings.Add(ReturnClosureDiagnosticCode.ReturnNetworkNodeGap);
            if (Coverage.Any(c => c.Verdict == ReturnLoopVerdict.BlockedByDeadEndSurface)) findings.Add(ReturnClosureDiagnosticCode.ReturnLoopDeadEnd);
            if (Coverage.Any(c => c.Verdict == ReturnLoopVerdict.BlockedByLiveNotificationClaim)) findings.Add(ReturnClosureDiagnosticCode.ReturnLiveClaimDetected);
            if (Coverage.Any(c => c.Verdict == ReturnLoopVerdict.BlockedByAccessibilityGap)) findings.Add(ReturnClosureDiagnosticCode.ReturnAccessibilityGap);
            if (ServerBoundaryAudit == null || !ServerBoundaryAudit.Visible || Coverage.Any(c => string.IsNullOrWhiteSpace(c.ServerBoundary))) findings.Add(ReturnClosureDiagnosticCode.ReturnLiveClaimDetected);
            if (Bee441BlockerStatus.PrematureAttempt) findings.Add(ReturnClosureDiagnosticCode.Bee441PrematureRelease);
            return new ReturnClosureDiagnostics(ResolveVerdict(findings, DemoEvidenceNeed != null && DemoEvidenceNeed.ReserveVisible), findings);
        }
        private static ReturnLoopVerdict ResolveVerdict(IReadOnlyList<ReturnClosureDiagnosticCode> findings, bool demoEvidenceReserveVisible)
        {
            if (findings.Contains(ReturnClosureDiagnosticCode.Bee441PrematureRelease)) return ReturnLoopVerdict.BlockedByBee441Premature;
            if (findings.Contains(ReturnClosureDiagnosticCode.ReturnAccessibilityGap)) return ReturnLoopVerdict.BlockedByAccessibilityGap;
            if (findings.Contains(ReturnClosureDiagnosticCode.ReturnLiveClaimDetected)) return ReturnLoopVerdict.BlockedByLiveNotificationClaim;
            if (findings.Contains(ReturnClosureDiagnosticCode.ReturnLoopDeadEnd)) return ReturnLoopVerdict.BlockedByDeadEndSurface;
            if (findings.Contains(ReturnClosureDiagnosticCode.ReturnNetworkNodeGap)) return ReturnLoopVerdict.NeedsPlannerRevision;
            return demoEvidenceReserveVisible ? ReturnLoopVerdict.ReadyForArchitectValidation : ReturnLoopVerdict.ReadyWithDemoEvidenceReserve;
        }
    }
    public sealed class ReturnClosureDiagnostics { public ReturnClosureDiagnostics(ReturnLoopVerdict verdict, IReadOnlyList<ReturnClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<ReturnClosureDiagnosticCode>(); } public ReturnLoopVerdict Verdict { get; } public IReadOnlyList<ReturnClosureDiagnosticCode> Findings { get; } public bool Contains(ReturnClosureDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ReturnNetworkClosureGateEvaluated { public ReturnNetworkClosureGateEvaluated(string gateId) { GateId = gateId ?? string.Empty; } public string GateId { get; } }
    public sealed class ReturnNetworkGapDetected { public ReturnNetworkGapDetected(string beeId) { BeeId = beeId ?? string.Empty; } public string BeeId { get; } }
    public sealed class Bee441BlockedByReturnGate { public Bee441BlockedByReturnGate(string message) { Message = message ?? string.Empty; } public string Message { get; } }
}
