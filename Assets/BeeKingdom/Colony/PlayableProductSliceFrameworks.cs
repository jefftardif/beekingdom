using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum WorldExitPreviewDomain { Exploration, Threats, Alliance, Events, Trade, War }
    public enum WorldExitPreviewState { PreviewAvailable, ServerAuthorityRequired, LiveMapBlocked, DisabledForDemo }
    public enum PlayableSliceDiagnosticCode { MissingSurface, MissingRoute, MissingServerBoundary, ForbiddenLiveClaim, MissingDemoEvidence, MissingQaControl, MissingPlayableLoop, Bee501Premature }

    public sealed class WorldExitAuthorityNotice { public WorldExitAuthorityNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class WorldExitPreviewRoute
    {
        public WorldExitPreviewRoute(string routeId, string playerLabel, WorldExitPreviewDomain domain, string previewReason, WorldExitPreviewState state, bool returnsToHive, bool serverBoundaryVisible, bool liveClaim = false)
        { RouteId = routeId ?? string.Empty; PlayerLabel = playerLabel ?? string.Empty; Domain = domain; PreviewReason = previewReason ?? string.Empty; State = state; ReturnsToHive = returnsToHive; ServerBoundaryVisible = serverBoundaryVisible; LiveClaim = liveClaim; }
        public string RouteId { get; } public string PlayerLabel { get; } public WorldExitPreviewDomain Domain { get; } public string PreviewReason { get; } public WorldExitPreviewState State { get; } public bool ReturnsToHive { get; } public bool ServerBoundaryVisible { get; } public bool LiveClaim { get; }
    }
    public sealed class HiveToWorldPlayerExitPreview
    {
        private static readonly WorldExitPreviewDomain[] RequiredDomains = { WorldExitPreviewDomain.Exploration, WorldExitPreviewDomain.Threats, WorldExitPreviewDomain.Alliance, WorldExitPreviewDomain.Events, WorldExitPreviewDomain.Trade, WorldExitPreviewDomain.War };
        public HiveToWorldPlayerExitPreview(string previewId, IReadOnlyList<WorldExitPreviewRoute> routes, WorldExitPreviewRoute selectedRoute, WorldExitAuthorityNotice authorityNotice)
        { PreviewId = ColonyIntegrationIds.Require(previewId); Routes = routes ?? Array.Empty<WorldExitPreviewRoute>(); SelectedRoute = selectedRoute; AuthorityNotice = authorityNotice; }
        public string PreviewId { get; } public IReadOnlyList<WorldExitPreviewRoute> Routes { get; } public WorldExitPreviewRoute SelectedRoute { get; } public WorldExitAuthorityNotice AuthorityNotice { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (RequiredDomains.Any(domain => Routes.All(r => r.Domain != domain))) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (Routes.Any(r => string.IsNullOrWhiteSpace(r.RouteId) || string.IsNullOrWhiteSpace(r.PlayerLabel) || string.IsNullOrWhiteSpace(r.PreviewReason) || !r.ReturnsToHive)) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (AuthorityNotice == null || !AuthorityNotice.Visible || Routes.Any(r => !r.ServerBoundaryVisible || r.State != WorldExitPreviewState.ServerAuthorityRequired)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Routes.Any(r => r.LiveClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }
    public sealed class WorldExitPreviewOpened { public WorldExitPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class WorldExitRouteSelected { public WorldExitRouteSelected(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class WorldLiveMapBlocked { public WorldLiveMapBlocked(string message = "Monde preview : carte persistante et joueurs reels seront serveur futurs.") { Message = message ?? string.Empty; } public string Message { get; } }

    public enum ScoutingPreviewRisk { LowPreview, MediumPreview, HighPreview, ServerUnknown }
    public enum ScoutingAuthorityState { PreviewOnly, ServerAuthorityRequired, SendBlocked, DisabledForDemo }
    public sealed class ScoutingIntentOption
    {
        public ScoutingIntentOption(string optionId, string targetKindLabel, string expectedInformation, ScoutingPreviewRisk risk, ScoutingAuthorityState authorityState, string requiredRole, bool returnsToWorldExit, bool serverBoundaryVisible, bool sendClaim = false, bool reportClaim = false)
        { OptionId = optionId ?? string.Empty; TargetKindLabel = targetKindLabel ?? string.Empty; ExpectedInformation = expectedInformation ?? string.Empty; Risk = risk; AuthorityState = authorityState; RequiredRole = requiredRole ?? string.Empty; ReturnsToWorldExit = returnsToWorldExit; ServerBoundaryVisible = serverBoundaryVisible; SendClaim = sendClaim; ReportClaim = reportClaim; }
        public string OptionId { get; } public string TargetKindLabel { get; } public string ExpectedInformation { get; } public ScoutingPreviewRisk Risk { get; } public ScoutingAuthorityState AuthorityState { get; } public string RequiredRole { get; } public bool ReturnsToWorldExit { get; } public bool ServerBoundaryVisible { get; } public bool SendClaim { get; } public bool ReportClaim { get; }
    }
    public sealed class WorldScoutingIntentPreview
    {
        private static readonly string[] RequiredTargets = { "ressource", "menace", "ruche inconnue", "point d'interet" };
        public WorldScoutingIntentPreview(string previewId, IReadOnlyList<ScoutingIntentOption> options, ScoutingIntentOption selectedOption)
        { PreviewId = ColonyIntegrationIds.Require(previewId); Options = options ?? Array.Empty<ScoutingIntentOption>(); SelectedOption = selectedOption; }
        public string PreviewId { get; } public IReadOnlyList<ScoutingIntentOption> Options { get; } public ScoutingIntentOption SelectedOption { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (RequiredTargets.Any(required => Options.All(o => !Contains(o.TargetKindLabel, required))) || Options.Any(o => string.IsNullOrWhiteSpace(o.ExpectedInformation) || string.IsNullOrWhiteSpace(o.RequiredRole))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (Options.Any(o => !o.ReturnsToWorldExit)) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (Options.Any(o => !o.ServerBoundaryVisible || o.AuthorityState != ScoutingAuthorityState.ServerAuthorityRequired)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Options.Any(o => o.SendClaim || o.ReportClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
        private static bool Contains(string text, string value) { return (text ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0; }
    }
    public sealed class ScoutingIntentPreviewOpened { public ScoutingIntentPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class ScoutingOptionFocused { public ScoutingOptionFocused(string optionId) { OptionId = optionId ?? string.Empty; } public string OptionId { get; } }
    public sealed class ScoutingSendBlocked { public ScoutingSendBlocked(string optionId) { OptionId = optionId ?? string.Empty; } public string OptionId { get; } }

    public enum AllianceJoinIntentState { PreviewOnly, ServerAuthorityRequired, JoinBlocked, DisabledForDemo }
    public sealed class AlliancePreviewCard
    {
        public AlliancePreviewCard(string alliancePreviewId, string displayName, string playstyleLabel, string languageLabel, string previewActivityLabel, string privacyNotice, bool serverBoundaryVisible, bool realPlayerClaim = false)
        { AlliancePreviewId = alliancePreviewId ?? string.Empty; DisplayName = displayName ?? string.Empty; PlaystyleLabel = playstyleLabel ?? string.Empty; LanguageLabel = languageLabel ?? string.Empty; PreviewActivityLabel = previewActivityLabel ?? string.Empty; PrivacyNotice = privacyNotice ?? string.Empty; ServerBoundaryVisible = serverBoundaryVisible; RealPlayerClaim = realPlayerClaim; }
        public string AlliancePreviewId { get; } public string DisplayName { get; } public string PlaystyleLabel { get; } public string LanguageLabel { get; } public string PreviewActivityLabel { get; } public string PrivacyNotice { get; } public bool ServerBoundaryVisible { get; } public bool RealPlayerClaim { get; }
    }
    public sealed class AllianceDiscoveryJoinIntentPreview
    {
        public AllianceDiscoveryJoinIntentPreview(string previewId, IReadOnlyList<AlliancePreviewCard> cards, AllianceJoinIntentState joinIntentState, bool returnsToAlliancePortal, bool returnsToWorldExit)
        { PreviewId = ColonyIntegrationIds.Require(previewId); Cards = cards ?? Array.Empty<AlliancePreviewCard>(); JoinIntentState = joinIntentState; ReturnsToAlliancePortal = returnsToAlliancePortal; ReturnsToWorldExit = returnsToWorldExit; }
        public string PreviewId { get; } public IReadOnlyList<AlliancePreviewCard> Cards { get; } public AllianceJoinIntentState JoinIntentState { get; } public bool ReturnsToAlliancePortal { get; } public bool ReturnsToWorldExit { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Cards.Count == 0 || Cards.Any(c => string.IsNullOrWhiteSpace(c.DisplayName) || string.IsNullOrWhiteSpace(c.PlaystyleLabel) || string.IsNullOrWhiteSpace(c.LanguageLabel) || string.IsNullOrWhiteSpace(c.PreviewActivityLabel) || string.IsNullOrWhiteSpace(c.PrivacyNotice))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (!ReturnsToAlliancePortal || !ReturnsToWorldExit) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (JoinIntentState != AllianceJoinIntentState.ServerAuthorityRequired || Cards.Any(c => !c.ServerBoundaryVisible)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Cards.Any(c => c.RealPlayerClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }
    public sealed class AllianceDiscoveryPreviewOpened { public AllianceDiscoveryPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class AlliancePreviewCardSelected { public AlliancePreviewCardSelected(string alliancePreviewId) { AlliancePreviewId = alliancePreviewId ?? string.Empty; } public string AlliancePreviewId { get; } }
    public sealed class AllianceJoinIntentBlocked { public AllianceJoinIntentBlocked(string message = "Adhesion preview : alliance serveur future requise.") { Message = message ?? string.Empty; } public string Message { get; } }

    public enum HelpRequestSendState { PreviewDraft, ServerAuthorityRequired, SendBlocked, DisabledForDemo }
    public sealed class HelpRequestDraftOption
    {
        public HelpRequestDraftOption(string draftId, string domainLabel, string playerMessagePreview, string privacyNotice, string sourceContext, bool serverBoundaryVisible, bool sendClaim = false, bool unreadClaim = false)
        { DraftId = draftId ?? string.Empty; DomainLabel = domainLabel ?? string.Empty; PlayerMessagePreview = playerMessagePreview ?? string.Empty; PrivacyNotice = privacyNotice ?? string.Empty; SourceContext = sourceContext ?? string.Empty; ServerBoundaryVisible = serverBoundaryVisible; SendClaim = sendClaim; UnreadClaim = unreadClaim; }
        public string DraftId { get; } public string DomainLabel { get; } public string PlayerMessagePreview { get; } public string PrivacyNotice { get; } public string SourceContext { get; } public bool ServerBoundaryVisible { get; } public bool SendClaim { get; } public bool UnreadClaim { get; }
    }
    public sealed class AllianceHelpRequestComposerPreview
    {
        private static readonly string[] RequiredDomains = { "ressource", "construction", "defense", "production" };
        public AllianceHelpRequestComposerPreview(string composerId, IReadOnlyList<HelpRequestDraftOption> draftOptions, HelpRequestDraftOption selectedDraft, HelpRequestSendState sendState, bool returnsToSource)
        { ComposerId = ColonyIntegrationIds.Require(composerId); DraftOptions = draftOptions ?? Array.Empty<HelpRequestDraftOption>(); SelectedDraft = selectedDraft; SendState = sendState; ReturnsToSource = returnsToSource; }
        public string ComposerId { get; } public IReadOnlyList<HelpRequestDraftOption> DraftOptions { get; } public HelpRequestDraftOption SelectedDraft { get; } public HelpRequestSendState SendState { get; } public bool ReturnsToSource { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (RequiredDomains.Any(required => DraftOptions.All(d => !Contains(d.DomainLabel, required))) || DraftOptions.Any(d => string.IsNullOrWhiteSpace(d.PlayerMessagePreview) || string.IsNullOrWhiteSpace(d.PrivacyNotice) || string.IsNullOrWhiteSpace(d.SourceContext))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (!ReturnsToSource) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (SendState != HelpRequestSendState.ServerAuthorityRequired || DraftOptions.Any(d => !d.ServerBoundaryVisible)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (DraftOptions.Any(d => d.SendClaim || d.UnreadClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
        private static bool Contains(string text, string value) { return (text ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0; }
    }
    public sealed class AllianceHelpComposerOpened { public AllianceHelpComposerOpened(string composerId) { ComposerId = composerId ?? string.Empty; } public string ComposerId { get; } }
    public sealed class HelpDraftSelected { public HelpDraftSelected(string draftId) { DraftId = draftId ?? string.Empty; } public string DraftId { get; } }
    public sealed class HelpRequestSendBlocked { public HelpRequestSendBlocked(string message = "Demande non envoyee : messagerie alliance serveur future.") { Message = message ?? string.Empty; } public string Message { get; } }

    public enum CommunicationPreviewKind { System, Alliance, World, PrivateFuture, Reports }
    public sealed class CommunicationPreviewChannel
    {
        public CommunicationPreviewChannel(string channelId, string label, CommunicationPreviewKind kind, string limitNotice, bool linkedToHelp, bool linkedToScoutingReports, bool serverBoundaryVisible, bool liveInputClaim = false, bool unreadClaim = false)
        { ChannelId = channelId ?? string.Empty; Label = label ?? string.Empty; Kind = kind; LimitNotice = limitNotice ?? string.Empty; LinkedToHelp = linkedToHelp; LinkedToScoutingReports = linkedToScoutingReports; ServerBoundaryVisible = serverBoundaryVisible; LiveInputClaim = liveInputClaim; UnreadClaim = unreadClaim; }
        public string ChannelId { get; } public string Label { get; } public CommunicationPreviewKind Kind { get; } public string LimitNotice { get; } public bool LinkedToHelp { get; } public bool LinkedToScoutingReports { get; } public bool ServerBoundaryVisible { get; } public bool LiveInputClaim { get; } public bool UnreadClaim { get; }
    }
    public sealed class CommunicationInboxPreview
    {
        private static readonly CommunicationPreviewKind[] RequiredKinds = { CommunicationPreviewKind.System, CommunicationPreviewKind.Alliance, CommunicationPreviewKind.World, CommunicationPreviewKind.PrivateFuture, CommunicationPreviewKind.Reports };
        public CommunicationInboxPreview(string inboxId, IReadOnlyList<CommunicationPreviewChannel> channels, CommunicationPreviewChannel selectedChannel)
        { InboxId = ColonyIntegrationIds.Require(inboxId); Channels = channels ?? Array.Empty<CommunicationPreviewChannel>(); SelectedChannel = selectedChannel; }
        public string InboxId { get; } public IReadOnlyList<CommunicationPreviewChannel> Channels { get; } public CommunicationPreviewChannel SelectedChannel { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (RequiredKinds.Any(kind => Channels.All(c => c.Kind != kind)) || Channels.Any(c => string.IsNullOrWhiteSpace(c.Label) || string.IsNullOrWhiteSpace(c.LimitNotice))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (Channels.All(c => !c.LinkedToHelp) || Channels.All(c => !c.LinkedToScoutingReports)) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (Channels.Any(c => !c.ServerBoundaryVisible)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Channels.Any(c => c.LiveInputClaim || c.UnreadClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }
    public sealed class CommunicationInboxPreviewOpened { public CommunicationInboxPreviewOpened(string inboxId) { InboxId = inboxId ?? string.Empty; } public string InboxId { get; } }
    public sealed class CommunicationChannelPreviewSelected { public CommunicationChannelPreviewSelected(string channelId) { ChannelId = channelId ?? string.Empty; } public string ChannelId { get; } }
    public sealed class CommunicationLiveInputBlocked { public CommunicationLiveInputBlocked(string channelId) { ChannelId = channelId ?? string.Empty; } public string ChannelId { get; } }

    public enum WorldEventBoardState { EmptyPreview, ServerAuthorityRequired, EventsPreviewed, DisabledForDemo }
    public enum WorldEventPreviewKind { AllianceCooperation, WorldThreat, EconomyOpportunity, FutureConflict }
    public enum WorldEventParticipationState { PreviewOnly, ServerAuthorityRequired, ParticipationBlocked, DisabledForDemo }
    public sealed class WorldEventPreviewCard
    {
        public WorldEventPreviewCard(string eventId, string eventTitle, WorldEventPreviewKind kind, string playerMotivation, WorldEventParticipationState participationState, bool communicationLinked, bool serverBoundaryVisible, bool rewardClaim = false, bool rankingClaim = false, bool timerLiveClaim = false)
        { EventId = eventId ?? string.Empty; EventTitle = eventTitle ?? string.Empty; Kind = kind; PlayerMotivation = playerMotivation ?? string.Empty; ParticipationState = participationState; CommunicationLinked = communicationLinked; ServerBoundaryVisible = serverBoundaryVisible; RewardClaim = rewardClaim; RankingClaim = rankingClaim; TimerLiveClaim = timerLiveClaim; }
        public string EventId { get; } public string EventTitle { get; } public WorldEventPreviewKind Kind { get; } public string PlayerMotivation { get; } public WorldEventParticipationState ParticipationState { get; } public bool CommunicationLinked { get; } public bool ServerBoundaryVisible { get; } public bool RewardClaim { get; } public bool RankingClaim { get; } public bool TimerLiveClaim { get; }
    }
    public sealed class WorldEventBoardPreview
    {
        private static readonly WorldEventPreviewKind[] RequiredKinds = { WorldEventPreviewKind.AllianceCooperation, WorldEventPreviewKind.WorldThreat, WorldEventPreviewKind.EconomyOpportunity, WorldEventPreviewKind.FutureConflict };
        public WorldEventBoardPreview(string boardId, IReadOnlyList<WorldEventPreviewCard> events, WorldEventBoardState state)
        { BoardId = ColonyIntegrationIds.Require(boardId); Events = events ?? Array.Empty<WorldEventPreviewCard>(); State = state; }
        public string BoardId { get; } public IReadOnlyList<WorldEventPreviewCard> Events { get; } public WorldEventBoardState State { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Events.Count < 4 || RequiredKinds.Any(kind => Events.All(e => e.Kind != kind)) || Events.Any(e => string.IsNullOrWhiteSpace(e.EventTitle) || string.IsNullOrWhiteSpace(e.PlayerMotivation))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (Events.Any(e => !e.CommunicationLinked)) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (State != WorldEventBoardState.ServerAuthorityRequired || Events.Any(e => !e.ServerBoundaryVisible || e.ParticipationState != WorldEventParticipationState.ServerAuthorityRequired)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Events.Any(e => e.RewardClaim || e.RankingClaim || e.TimerLiveClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }
    public sealed class WorldEventBoardPreviewOpened { public WorldEventBoardPreviewOpened(string boardId) { BoardId = boardId ?? string.Empty; } public string BoardId { get; } }
    public sealed class WorldEventPreviewCardFocused { public WorldEventPreviewCardFocused(string eventId) { EventId = eventId ?? string.Empty; } public string EventId { get; } }
    public sealed class WorldEventParticipationBlocked { public WorldEventParticipationBlocked(string eventId) { EventId = eventId ?? string.Empty; } public string EventId { get; } }

    public enum TradeOpportunityAuthorityState { PreviewOnly, ServerAuthorityRequired, MarketClosed, DisabledForDemo }
    public sealed class TradeOpportunityCard
    {
        public TradeOpportunityCard(string opportunityId, string resourceLabel, string partnerPreviewLabel, string benefitPreview, TradeOpportunityAuthorityState authorityState, bool serverBoundaryVisible, bool transactionClaim = false, bool priceClaim = false, bool deliveryClaim = false)
        { OpportunityId = opportunityId ?? string.Empty; ResourceLabel = resourceLabel ?? string.Empty; PartnerPreviewLabel = partnerPreviewLabel ?? string.Empty; BenefitPreview = benefitPreview ?? string.Empty; AuthorityState = authorityState; ServerBoundaryVisible = serverBoundaryVisible; TransactionClaim = transactionClaim; PriceClaim = priceClaim; DeliveryClaim = deliveryClaim; }
        public string OpportunityId { get; } public string ResourceLabel { get; } public string PartnerPreviewLabel { get; } public string BenefitPreview { get; } public TradeOpportunityAuthorityState AuthorityState { get; } public bool ServerBoundaryVisible { get; } public bool TransactionClaim { get; } public bool PriceClaim { get; } public bool DeliveryClaim { get; }
    }
    public sealed class TradeRouteOpportunityPreview
    {
        public TradeRouteOpportunityPreview(string previewId, IReadOnlyList<TradeOpportunityCard> opportunities, TradeOpportunityCard selectedOpportunity)
        { PreviewId = ColonyIntegrationIds.Require(previewId); Opportunities = opportunities ?? Array.Empty<TradeOpportunityCard>(); SelectedOpportunity = selectedOpportunity; }
        public string PreviewId { get; } public IReadOnlyList<TradeOpportunityCard> Opportunities { get; } public TradeOpportunityCard SelectedOpportunity { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Opportunities.Count == 0 || Opportunities.Any(o => string.IsNullOrWhiteSpace(o.ResourceLabel) || string.IsNullOrWhiteSpace(o.PartnerPreviewLabel) || string.IsNullOrWhiteSpace(o.BenefitPreview))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (Opportunities.Any(o => !o.ServerBoundaryVisible || o.AuthorityState != TradeOpportunityAuthorityState.ServerAuthorityRequired)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Opportunities.Any(o => o.TransactionClaim || o.PriceClaim || o.DeliveryClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }
    public sealed class TradeRouteOpportunityPreviewOpened { public TradeRouteOpportunityPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class TradeOpportunityCardFocused { public TradeOpportunityCardFocused(string opportunityId) { OpportunityId = opportunityId ?? string.Empty; } public string OpportunityId { get; } }
    public sealed class TradeTransactionBlocked { public TradeTransactionBlocked(string opportunityId) { OpportunityId = opportunityId ?? string.Empty; } public string OpportunityId { get; } }

    public enum ConflictPreviewSeverity { Calm, Watch, Risk, ServerUnknown }
    public enum ConflictAuthorityState { PreviewOnly, ServerAuthorityRequired, RallyBlocked, DisabledForDemo }
    public sealed class ConflictPreviewSignal
    {
        public ConflictPreviewSignal(string signalId, string playerExplanation, ConflictPreviewSeverity severity, ConflictAuthorityState authorityState, bool serverBoundaryVisible, bool liveTargetClaim = false)
        { SignalId = signalId ?? string.Empty; PlayerExplanation = playerExplanation ?? string.Empty; Severity = severity; AuthorityState = authorityState; ServerBoundaryVisible = serverBoundaryVisible; LiveTargetClaim = liveTargetClaim; }
        public string SignalId { get; } public string PlayerExplanation { get; } public ConflictPreviewSeverity Severity { get; } public ConflictAuthorityState AuthorityState { get; } public bool ServerBoundaryVisible { get; } public bool LiveTargetClaim { get; }
    }
    public sealed class RallyIntentPreview { public RallyIntentPreview(string rallyId, string purpose, bool defenseLinked, bool allianceLinked, bool serverBoundaryVisible, bool launchClaim = false) { RallyId = rallyId ?? string.Empty; Purpose = purpose ?? string.Empty; DefenseLinked = defenseLinked; AllianceLinked = allianceLinked; ServerBoundaryVisible = serverBoundaryVisible; LaunchClaim = launchClaim; } public string RallyId { get; } public string Purpose { get; } public bool DefenseLinked { get; } public bool AllianceLinked { get; } public bool ServerBoundaryVisible { get; } public bool LaunchClaim { get; } }
    public sealed class ConflictRiskRallyIntentPreview
    {
        public ConflictRiskRallyIntentPreview(string previewId, IReadOnlyList<ConflictPreviewSignal> signals, IReadOnlyList<RallyIntentPreview> rallyIntents, bool antiHarassmentNoticeVisible, bool beginnerProtectionNoticeVisible)
        { PreviewId = ColonyIntegrationIds.Require(previewId); Signals = signals ?? Array.Empty<ConflictPreviewSignal>(); RallyIntents = rallyIntents ?? Array.Empty<RallyIntentPreview>(); AntiHarassmentNoticeVisible = antiHarassmentNoticeVisible; BeginnerProtectionNoticeVisible = beginnerProtectionNoticeVisible; }
        public string PreviewId { get; } public IReadOnlyList<ConflictPreviewSignal> Signals { get; } public IReadOnlyList<RallyIntentPreview> RallyIntents { get; } public bool AntiHarassmentNoticeVisible { get; } public bool BeginnerProtectionNoticeVisible { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Signals.Count < 4 || RallyIntents.Count == 0 || Signals.Any(s => string.IsNullOrWhiteSpace(s.PlayerExplanation)) || RallyIntents.Any(r => string.IsNullOrWhiteSpace(r.Purpose))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (RallyIntents.Any(r => !r.DefenseLinked || !r.AllianceLinked)) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (!AntiHarassmentNoticeVisible || !BeginnerProtectionNoticeVisible || Signals.Any(s => !s.ServerBoundaryVisible || s.AuthorityState != ConflictAuthorityState.ServerAuthorityRequired) || RallyIntents.Any(r => !r.ServerBoundaryVisible)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Signals.Any(s => s.LiveTargetClaim) || RallyIntents.Any(r => r.LaunchClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }
    public sealed class ConflictRiskPreviewOpened { public ConflictRiskPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class RallyIntentFocused { public RallyIntentFocused(string rallyId) { RallyId = rallyId ?? string.Empty; } public string RallyId { get; } }
    public sealed class RallyLiveActionBlocked { public RallyLiveActionBlocked(string rallyId) { RallyId = rallyId ?? string.Empty; } public string RallyId { get; } }

    public enum DemoReadinessSurfaceState { ContractReady, VisualProofMissing, QaProofMissing, PreviewReserve, Blocked }
    public enum Bee500ReadinessStatus { ReadyForBEE500, ReadyWithReserves, BlockedByMissingSurface, BlockedByDemoException }
    public sealed class DemoReadinessSurface { public DemoReadinessSurface(string surfaceId, string relatedBeeRange, string expectedVisualProof, DemoReadinessSurfaceState state) { SurfaceId = surfaceId ?? string.Empty; RelatedBeeRange = relatedBeeRange ?? string.Empty; ExpectedVisualProof = expectedVisualProof ?? string.Empty; State = state; } public string SurfaceId { get; } public string RelatedBeeRange { get; } public string ExpectedVisualProof { get; } public DemoReadinessSurfaceState State { get; } }
    public sealed class DemoReadinessReserve { public DemoReadinessReserve(string reserveId, string description) { ReserveId = reserveId ?? string.Empty; Description = description ?? string.Empty; } public string ReserveId { get; } public string Description { get; } }
    public sealed class Bee500DemoReadinessAccumulator
    {
        public Bee500DemoReadinessAccumulator(string accumulatorId, IReadOnlyList<DemoReadinessSurface> surfaces, IReadOnlyList<DemoReadinessReserve> reserves)
        { AccumulatorId = ColonyIntegrationIds.Require(accumulatorId); Surfaces = surfaces ?? Array.Empty<DemoReadinessSurface>(); Reserves = reserves ?? Array.Empty<DemoReadinessReserve>(); Status = EvaluateStatus(); }
        public string AccumulatorId { get; } public IReadOnlyList<DemoReadinessSurface> Surfaces { get; } public IReadOnlyList<DemoReadinessReserve> Reserves { get; } public Bee500ReadinessStatus Status { get; }
        private Bee500ReadinessStatus EvaluateStatus()
        {
            string[] required = { "BEE-451..470", "BEE-471..480", "BEE-481..488" };
            if (required.Any(range => Surfaces.All(s => !string.Equals(s.RelatedBeeRange, range, StringComparison.OrdinalIgnoreCase)))) return Bee500ReadinessStatus.BlockedByMissingSurface;
            if (Surfaces.Any(s => s.State == DemoReadinessSurfaceState.Blocked)) return Bee500ReadinessStatus.BlockedByDemoException;
            return Reserves.Count > 0 || Surfaces.Any(s => s.State != DemoReadinessSurfaceState.ContractReady) ? Bee500ReadinessStatus.ReadyWithReserves : Bee500ReadinessStatus.ReadyForBEE500;
        }
    }
    public sealed class DemoReadinessSurfaceRegistered { public DemoReadinessSurfaceRegistered(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class DemoReadinessReserveRecorded { public DemoReadinessReserveRecorded(string reserveId) { ReserveId = reserveId ?? string.Empty; } public string ReserveId { get; } }
    public sealed class Bee500ReadinessStatusPrepared { public Bee500ReadinessStatusPrepared(Bee500ReadinessStatus status) { Status = status; } public Bee500ReadinessStatus Status { get; } }

    public enum Bee500MidwaveAlignmentStatus { AlignedForNextWave, AlignedWithReserves, BlockedByLiveClaim, BlockedByMissingSurface }
    public sealed class Bee500SliceSurfaceRow { public Bee500SliceSurfaceRow(string beeId, string playerVisibleSurface, string preparedLoopContribution, string forbiddenLiveClaim) { BeeId = beeId ?? string.Empty; PlayerVisibleSurface = playerVisibleSurface ?? string.Empty; PreparedLoopContribution = preparedLoopContribution ?? string.Empty; ForbiddenLiveClaim = forbiddenLiveClaim ?? string.Empty; } public string BeeId { get; } public string PlayerVisibleSurface { get; } public string PreparedLoopContribution { get; } public string ForbiddenLiveClaim { get; } }
    public sealed class Bee500NextStepRow { public Bee500NextStepRow(string stepId, string description) { StepId = stepId ?? string.Empty; Description = description ?? string.Empty; } public string StepId { get; } public string Description { get; } }
    public sealed class Bee500PlayableSliceMidwaveAlignment
    {
        public Bee500PlayableSliceMidwaveAlignment(string alignmentId, IReadOnlyList<Bee500SliceSurfaceRow> surfaces, IReadOnlyList<Bee500NextStepRow> nextSteps)
        { AlignmentId = ColonyIntegrationIds.Require(alignmentId); Surfaces = surfaces ?? Array.Empty<Bee500SliceSurfaceRow>(); NextSteps = nextSteps ?? Array.Empty<Bee500NextStepRow>(); Status = EvaluateStatus(); }
        public string AlignmentId { get; } public IReadOnlyList<Bee500SliceSurfaceRow> Surfaces { get; } public IReadOnlyList<Bee500NextStepRow> NextSteps { get; } public Bee500MidwaveAlignmentStatus Status { get; }
        private Bee500MidwaveAlignmentStatus EvaluateStatus()
        {
            for (int bee = 481; bee <= 489; bee++) if (Surfaces.All(s => !string.Equals(s.BeeId, "BEE-" + bee, StringComparison.OrdinalIgnoreCase))) return Bee500MidwaveAlignmentStatus.BlockedByMissingSurface;
            if (Surfaces.Any(s => string.IsNullOrWhiteSpace(s.ForbiddenLiveClaim))) return Bee500MidwaveAlignmentStatus.BlockedByLiveClaim;
            return NextSteps.Count > 0 ? Bee500MidwaveAlignmentStatus.AlignedForNextWave : Bee500MidwaveAlignmentStatus.AlignedWithReserves;
        }
    }
    public sealed class Bee500MidwaveAlignmentReviewed { public Bee500MidwaveAlignmentReviewed(string alignmentId) { AlignmentId = alignmentId ?? string.Empty; } public string AlignmentId { get; } }

    public enum OnboardingPreviewState { PreviewReady, ResumeLater, ServerBoundaryShown, BlockedByLiveClaim }
    public enum OnboardingStepLimit { PreviewOnly, ServerRequired, Skippable, Closeable }
    public sealed class OnboardingHiveStep { public OnboardingHiveStep(string stepId, string playerPrompt, string targetSurface, OnboardingStepLimit limit, bool rewardClaim = false) { StepId = stepId ?? string.Empty; PlayerPrompt = playerPrompt ?? string.Empty; TargetSurface = targetSurface ?? string.Empty; Limit = limit; RewardClaim = rewardClaim; } public string StepId { get; } public string PlayerPrompt { get; } public string TargetSurface { get; } public OnboardingStepLimit Limit { get; } public bool RewardClaim { get; } }
    public sealed class PlayerOnboardingFirstHiveMinute
    {
        public PlayerOnboardingFirstHiveMinute(string onboardingId, IReadOnlyList<OnboardingHiveStep> steps, OnboardingPreviewState state)
        { OnboardingId = ColonyIntegrationIds.Require(onboardingId); Steps = steps ?? Array.Empty<OnboardingHiveStep>(); State = state; }
        public string OnboardingId { get; } public IReadOnlyList<OnboardingHiveStep> Steps { get; } public OnboardingPreviewState State { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Steps.Count == 0 || Steps.Count > 5 || Steps.Any(s => string.IsNullOrWhiteSpace(s.PlayerPrompt) || string.IsNullOrWhiteSpace(s.TargetSurface))) findings.Add(PlayableSliceDiagnosticCode.MissingPlayableLoop);
            if (Steps.All(s => s.Limit != OnboardingStepLimit.ServerRequired)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Steps.Any(s => s.RewardClaim) || State == OnboardingPreviewState.BlockedByLiveClaim) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }

    public enum HomeHubPreviewState { PreviewReady, ServerBoundaryVisible, BlockedByLiveClaim }
    public sealed class HomeHubTilePreview { public HomeHubTilePreview(string tileId, string label, string targetSurface, string previewStatus, bool serverBoundaryVisible, bool unreadClaim = false, bool profileClaim = false) { TileId = tileId ?? string.Empty; Label = label ?? string.Empty; TargetSurface = targetSurface ?? string.Empty; PreviewStatus = previewStatus ?? string.Empty; ServerBoundaryVisible = serverBoundaryVisible; UnreadClaim = unreadClaim; ProfileClaim = profileClaim; } public string TileId { get; } public string Label { get; } public string TargetSurface { get; } public string PreviewStatus { get; } public bool ServerBoundaryVisible { get; } public bool UnreadClaim { get; } public bool ProfileClaim { get; } }
    public sealed class PlayerHomeCommandHubPreview
    {
        private static readonly string[] RequiredTiles = { "ruche", "monde", "alliance", "inbox", "evenements", "session" };
        public PlayerHomeCommandHubPreview(string hubId, IReadOnlyList<HomeHubTilePreview> tiles, HomeHubPreviewState state)
        { HubId = ColonyIntegrationIds.Require(hubId); Tiles = tiles ?? Array.Empty<HomeHubTilePreview>(); State = state; }
        public string HubId { get; } public IReadOnlyList<HomeHubTilePreview> Tiles { get; } public HomeHubPreviewState State { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (RequiredTiles.Any(required => Tiles.All(t => !Contains(t.TileId, required))) || Tiles.Any(t => string.IsNullOrWhiteSpace(t.Label) || string.IsNullOrWhiteSpace(t.TargetSurface) || string.IsNullOrWhiteSpace(t.PreviewStatus))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (Tiles.Any(t => !t.ServerBoundaryVisible)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Tiles.Any(t => t.UnreadClaim || t.ProfileClaim) || State == HomeHubPreviewState.BlockedByLiveClaim) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
        private static bool Contains(string text, string value) { return (text ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0; }
    }

    public enum GuidedActionStepState { PreviewReady, Viewed, ServerBlocked, ReturnAvailable }
    public enum GuidedActionLoopOutcome { PreparedOnly, ReturnedToHub, BlockedByServerAuthority, BlockedByLiveClaim }
    public sealed class GuidedActionLoopStep { public GuidedActionLoopStep(string stepId, string sourceBee, string playerInstruction, GuidedActionStepState state, bool spendClaim = false) { StepId = stepId ?? string.Empty; SourceBee = sourceBee ?? string.Empty; PlayerInstruction = playerInstruction ?? string.Empty; State = state; SpendClaim = spendClaim; } public string StepId { get; } public string SourceBee { get; } public string PlayerInstruction { get; } public GuidedActionStepState State { get; } public bool SpendClaim { get; } }
    public sealed class GuidedActionPreparationLoop
    {
        public GuidedActionPreparationLoop(string loopId, IReadOnlyList<GuidedActionLoopStep> steps, GuidedActionLoopOutcome outcome)
        { LoopId = ColonyIntegrationIds.Require(loopId); Steps = steps ?? Array.Empty<GuidedActionLoopStep>(); Outcome = outcome; }
        public string LoopId { get; } public IReadOnlyList<GuidedActionLoopStep> Steps { get; } public GuidedActionLoopOutcome Outcome { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Steps.Count == 0 || Steps.Count > 6 || Steps.Any(s => string.IsNullOrWhiteSpace(s.SourceBee) || string.IsNullOrWhiteSpace(s.PlayerInstruction))) findings.Add(PlayableSliceDiagnosticCode.MissingPlayableLoop);
            if (Steps.All(s => s.State != GuidedActionStepState.ServerBlocked)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Steps.All(s => s.State != GuidedActionStepState.ReturnAvailable)) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (Steps.Any(s => s.SpendClaim) || Outcome == GuidedActionLoopOutcome.BlockedByLiveClaim) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }

    public sealed class PlayerFeedbackBlockerMessage { public PlayerFeedbackBlockerMessage(string messageId, string domain, string shortText, string accessibilityHint, bool monetizationLanguage = false, bool blamesPlayer = false) { MessageId = messageId ?? string.Empty; Domain = domain ?? string.Empty; ShortText = shortText ?? string.Empty; AccessibilityHint = accessibilityHint ?? string.Empty; MonetizationLanguage = monetizationLanguage; BlamesPlayer = blamesPlayer; } public string MessageId { get; } public string Domain { get; } public string ShortText { get; } public string AccessibilityHint { get; } public bool MonetizationLanguage { get; } public bool BlamesPlayer { get; } }
    public sealed class PlayerFeedbackToneRule { public PlayerFeedbackToneRule(string ruleId, string description) { RuleId = ruleId ?? string.Empty; Description = description ?? string.Empty; } public string RuleId { get; } public string Description { get; } }
    public sealed class PlayerFeedbackBlockerToneCatalog
    {
        private static readonly string[] RequiredDomains = { "server", "preview", "action", "economy", "pvp", "chat", "demo", "fallback" };
        public PlayerFeedbackBlockerToneCatalog(string catalogId, IReadOnlyList<PlayerFeedbackBlockerMessage> messages, IReadOnlyList<PlayerFeedbackToneRule> toneRules)
        { CatalogId = ColonyIntegrationIds.Require(catalogId); Messages = messages ?? Array.Empty<PlayerFeedbackBlockerMessage>(); ToneRules = toneRules ?? Array.Empty<PlayerFeedbackToneRule>(); }
        public string CatalogId { get; } public IReadOnlyList<PlayerFeedbackBlockerMessage> Messages { get; } public IReadOnlyList<PlayerFeedbackToneRule> ToneRules { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (RequiredDomains.Any(required => Messages.All(m => !Contains(m.Domain, required))) || Messages.Any(m => string.IsNullOrWhiteSpace(m.ShortText) || string.IsNullOrWhiteSpace(m.AccessibilityHint) || m.ShortText.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Length > 1)) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (ToneRules.Count == 0) findings.Add(PlayableSliceDiagnosticCode.MissingQaControl);
            if (Messages.Any(m => m.MonetizationLanguage || m.BlamesPlayer)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
        private static bool Contains(string text, string value) { return (text ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0; }
    }

    public enum SessionRecapPersistenceState { LocalPreviewOnly, EmptyState, PersistentClaimBlocked }
    public sealed class SessionRecapFact { public SessionRecapFact(string factId, string playerText) { FactId = factId ?? string.Empty; PlayerText = playerText ?? string.Empty; } public string FactId { get; } public string PlayerText { get; } }
    public sealed class NextBestActionPreview { public NextBestActionPreview(string actionId, string playerReason, string targetSurface, string limitNotice, bool rewardClaim = false) { ActionId = actionId ?? string.Empty; PlayerReason = playerReason ?? string.Empty; TargetSurface = targetSurface ?? string.Empty; LimitNotice = limitNotice ?? string.Empty; RewardClaim = rewardClaim; } public string ActionId { get; } public string PlayerReason { get; } public string TargetSurface { get; } public string LimitNotice { get; } public bool RewardClaim { get; } }
    public sealed class PlayerSessionRecapPreview
    {
        public PlayerSessionRecapPreview(string recapId, IReadOnlyList<SessionRecapFact> facts, NextBestActionPreview nextBestAction, SessionRecapPersistenceState persistenceState)
        { RecapId = ColonyIntegrationIds.Require(recapId); Facts = facts ?? Array.Empty<SessionRecapFact>(); NextBestAction = nextBestAction; PersistenceState = persistenceState; }
        public string RecapId { get; } public IReadOnlyList<SessionRecapFact> Facts { get; } public NextBestActionPreview NextBestAction { get; } public SessionRecapPersistenceState PersistenceState { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Facts.Count > 3 || Facts.Any(f => string.IsNullOrWhiteSpace(f.PlayerText)) || NextBestAction == null || string.IsNullOrWhiteSpace(NextBestAction.TargetSurface)) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (NextBestAction == null || string.IsNullOrWhiteSpace(NextBestAction.LimitNotice)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (PersistenceState == SessionRecapPersistenceState.PersistentClaimBlocked || (NextBestAction != null && NextBestAction.RewardClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }

    public enum ProgressionStripAuthorityState { PreviewOnly, ServerBoundaryVisible, OfficialProgressionClaimBlocked }
    public enum ProgressionMarkerPreviewState { DotUnseen, DotSeen, DotExplained, DotServerBound, DotDemoReady, DotMuted, DotFocusRing }
    public sealed class PlayerProgressionPreviewMarker { public PlayerProgressionPreviewMarker(string markerId, string label, string visualAnchor, ProgressionMarkerPreviewState state, bool officialProgressionClaim = false) { MarkerId = markerId ?? string.Empty; Label = label ?? string.Empty; VisualAnchor = visualAnchor ?? string.Empty; State = state; OfficialProgressionClaim = officialProgressionClaim; } public string MarkerId { get; } public string Label { get; } public string VisualAnchor { get; } public ProgressionMarkerPreviewState State { get; } public bool OfficialProgressionClaim { get; } }
    public sealed class PlayerProgressionVisibilityStrip
    {
        public PlayerProgressionVisibilityStrip(string stripId, IReadOnlyList<PlayerProgressionPreviewMarker> markers, ProgressionStripAuthorityState authorityState)
        { StripId = ColonyIntegrationIds.Require(stripId); Markers = markers ?? Array.Empty<PlayerProgressionPreviewMarker>(); AuthorityState = authorityState; }
        public string StripId { get; } public IReadOnlyList<PlayerProgressionPreviewMarker> Markers { get; } public ProgressionStripAuthorityState AuthorityState { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Markers.Count == 0 || Markers.Any(m => string.IsNullOrWhiteSpace(m.Label) || string.IsNullOrWhiteSpace(m.VisualAnchor))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (Markers.All(m => m.State != ProgressionMarkerPreviewState.DotServerBound)) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Markers.All(m => m.State != ProgressionMarkerPreviewState.DotDemoReady)) findings.Add(PlayableSliceDiagnosticCode.MissingDemoEvidence);
            if (AuthorityState == ProgressionStripAuthorityState.OfficialProgressionClaimBlocked || Markers.Any(m => m.OfficialProgressionClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }

    public enum ArmyReadinessPreviewVerdict { PreparedPreview, ReadyWithMissingRoles, BlockedByLiveActionClaim, ServerAuthorityRequired }
    public sealed class ArmyReadinessPreviewItem { public ArmyReadinessPreviewItem(string itemId, string domain, string playerMeaning, string missingPreparation, bool roleLinked, bool scoutingLinked, bool rallyLinked, bool serverBoundaryVisible, bool liveActionClaim = false) { ItemId = itemId ?? string.Empty; Domain = domain ?? string.Empty; PlayerMeaning = playerMeaning ?? string.Empty; MissingPreparation = missingPreparation ?? string.Empty; RoleLinked = roleLinked; ScoutingLinked = scoutingLinked; RallyLinked = rallyLinked; ServerBoundaryVisible = serverBoundaryVisible; LiveActionClaim = liveActionClaim; } public string ItemId { get; } public string Domain { get; } public string PlayerMeaning { get; } public string MissingPreparation { get; } public bool RoleLinked { get; } public bool ScoutingLinked { get; } public bool RallyLinked { get; } public bool ServerBoundaryVisible { get; } public bool LiveActionClaim { get; } }
    public sealed class ArmyDefensePlayableReadinessPreview
    {
        private static readonly string[] RequiredItems = { "defense", "scout", "rally", "protection" };
        public ArmyDefensePlayableReadinessPreview(string previewId, IReadOnlyList<ArmyReadinessPreviewItem> items, ArmyReadinessPreviewVerdict verdict, bool antiHarassmentNoticeVisible)
        { PreviewId = ColonyIntegrationIds.Require(previewId); Items = items ?? Array.Empty<ArmyReadinessPreviewItem>(); Verdict = verdict; AntiHarassmentNoticeVisible = antiHarassmentNoticeVisible; }
        public string PreviewId { get; } public IReadOnlyList<ArmyReadinessPreviewItem> Items { get; } public ArmyReadinessPreviewVerdict Verdict { get; } public bool AntiHarassmentNoticeVisible { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (RequiredItems.Any(required => Items.All(i => !Contains(i.Domain, required))) || Items.Any(i => string.IsNullOrWhiteSpace(i.PlayerMeaning))) findings.Add(PlayableSliceDiagnosticCode.MissingSurface);
            if (Items.All(i => !i.RoleLinked) || Items.All(i => !i.ScoutingLinked) || Items.All(i => !i.RallyLinked)) findings.Add(PlayableSliceDiagnosticCode.MissingRoute);
            if (!AntiHarassmentNoticeVisible || Items.Any(i => !i.ServerBoundaryVisible) || Verdict != ArmyReadinessPreviewVerdict.ServerAuthorityRequired) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (Items.Any(i => i.LiveActionClaim)) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
        private static bool Contains(string text, string value) { return (text ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0; }
    }

    public sealed class ServerAuthorityFutureDomain { public ServerAuthorityFutureDomain(string domainId, string playerSurface, string futureAuthorityReason, string currentPreviewLimit) { DomainId = domainId ?? string.Empty; PlayerSurface = playerSurface ?? string.Empty; FutureAuthorityReason = futureAuthorityReason ?? string.Empty; CurrentPreviewLimit = currentPreviewLimit ?? string.Empty; } public string DomainId { get; } public string PlayerSurface { get; } public string FutureAuthorityReason { get; } public string CurrentPreviewLimit { get; } }
    public sealed class ServerAuthorityOpenQuestion { public ServerAuthorityOpenQuestion(string questionId, string domainId, string question) { QuestionId = questionId ?? string.Empty; DomainId = domainId ?? string.Empty; Question = question ?? string.Empty; } public string QuestionId { get; } public string DomainId { get; } public string Question { get; } }
    public sealed class ServerAuthorityReadinessMapForPlayableSlice
    {
        public ServerAuthorityReadinessMapForPlayableSlice(string mapId, IReadOnlyList<ServerAuthorityFutureDomain> domains, IReadOnlyList<ServerAuthorityOpenQuestion> openQuestions, bool backendImplementationClaim = false)
        { MapId = ColonyIntegrationIds.Require(mapId); Domains = domains ?? Array.Empty<ServerAuthorityFutureDomain>(); OpenQuestions = openQuestions ?? Array.Empty<ServerAuthorityOpenQuestion>(); BackendImplementationClaim = backendImplementationClaim; }
        public string MapId { get; } public IReadOnlyList<ServerAuthorityFutureDomain> Domains { get; } public IReadOnlyList<ServerAuthorityOpenQuestion> OpenQuestions { get; } public bool BackendImplementationClaim { get; }
        public PlayableSliceDiagnostics Evaluate()
        {
            var findings = new List<PlayableSliceDiagnosticCode>();
            if (Domains.Count < 10 || Domains.Any(d => string.IsNullOrWhiteSpace(d.PlayerSurface) || string.IsNullOrWhiteSpace(d.FutureAuthorityReason) || string.IsNullOrWhiteSpace(d.CurrentPreviewLimit))) findings.Add(PlayableSliceDiagnosticCode.MissingServerBoundary);
            if (OpenQuestions.Count == 0) findings.Add(PlayableSliceDiagnosticCode.MissingQaControl);
            if (BackendImplementationClaim) findings.Add(PlayableSliceDiagnosticCode.ForbiddenLiveClaim);
            return new PlayableSliceDiagnostics(findings);
        }
    }
    public sealed class ServerAuthorityFutureDomainMapped { public ServerAuthorityFutureDomainMapped(string domainId) { DomainId = domainId ?? string.Empty; } public string DomainId { get; } }
    public sealed class ServerAuthorityOpenQuestionRecorded { public ServerAuthorityOpenQuestionRecorded(string questionId) { QuestionId = questionId ?? string.Empty; } public string QuestionId { get; } }
    public sealed class ServerAuthorityPreviewLimitReviewed { public ServerAuthorityPreviewLimitReviewed(string domainId) { DomainId = domainId ?? string.Empty; } public string DomainId { get; } }

    public enum Bee500DemoQaReadinessVerdict { PreparedForMilestone, PreparedWithReserves, BlockedByMissingEvidence, BlockedByQaRisk, BlockedByLiveClaim }
    public sealed class Bee500EvidenceRequirement { public Bee500EvidenceRequirement(string requirementId, string playerVisibleProof, string requiredSurface, string forbiddenClaim, bool prepared) { RequirementId = requirementId ?? string.Empty; PlayerVisibleProof = playerVisibleProof ?? string.Empty; RequiredSurface = requiredSurface ?? string.Empty; ForbiddenClaim = forbiddenClaim ?? string.Empty; Prepared = prepared; } public string RequirementId { get; } public string PlayerVisibleProof { get; } public string RequiredSurface { get; } public string ForbiddenClaim { get; } public bool Prepared { get; } }
    public sealed class Bee500QaRisk { public Bee500QaRisk(string riskId, string description, bool blocking) { RiskId = riskId ?? string.Empty; Description = description ?? string.Empty; Blocking = blocking; } public string RiskId { get; } public string Description { get; } public bool Blocking { get; } }
    public sealed class Bee500DemoQaPlayableSliceReadiness
    {
        public Bee500DemoQaPlayableSliceReadiness(string readinessId, IReadOnlyList<Bee500EvidenceRequirement> evidenceRequirements, IReadOnlyList<Bee500QaRisk> qaRisks)
        { ReadinessId = ColonyIntegrationIds.Require(readinessId); EvidenceRequirements = evidenceRequirements ?? Array.Empty<Bee500EvidenceRequirement>(); QaRisks = qaRisks ?? Array.Empty<Bee500QaRisk>(); Verdict = EvaluateVerdict(); }
        public string ReadinessId { get; } public IReadOnlyList<Bee500EvidenceRequirement> EvidenceRequirements { get; } public IReadOnlyList<Bee500QaRisk> QaRisks { get; } public Bee500DemoQaReadinessVerdict Verdict { get; }
        private Bee500DemoQaReadinessVerdict EvaluateVerdict()
        {
            string[] required = { "first-minute", "hub", "hive", "loop", "world", "alliance", "inbox", "event", "trade", "conflict", "army" };
            if (required.Any(id => EvidenceRequirements.All(e => !string.Equals(e.RequirementId, id, StringComparison.OrdinalIgnoreCase))) || EvidenceRequirements.Any(e => !e.Prepared)) return Bee500DemoQaReadinessVerdict.BlockedByMissingEvidence;
            if (EvidenceRequirements.Any(e => string.IsNullOrWhiteSpace(e.ForbiddenClaim))) return Bee500DemoQaReadinessVerdict.BlockedByLiveClaim;
            return QaRisks.Any(r => r.Blocking) ? Bee500DemoQaReadinessVerdict.BlockedByQaRisk : QaRisks.Count > 0 ? Bee500DemoQaReadinessVerdict.PreparedWithReserves : Bee500DemoQaReadinessVerdict.PreparedForMilestone;
        }
    }
    public sealed class Bee500EvidenceRequirementRegistered { public Bee500EvidenceRequirementRegistered(string requirementId) { RequirementId = requirementId ?? string.Empty; } public string RequirementId { get; } }
    public sealed class Bee500QaRiskRecorded { public Bee500QaRiskRecorded(string riskId) { RiskId = riskId ?? string.Empty; } public string RiskId { get; } }
    public sealed class Bee500ReadinessVerdictPrepared { public Bee500ReadinessVerdictPrepared(Bee500DemoQaReadinessVerdict verdict) { Verdict = verdict; } public Bee500DemoQaReadinessVerdict Verdict { get; } }

    public enum PlayableSliceGateStatus { Covered, PreviewReserve, MissingProof, LiveClaim, HiddenServerAuthority, NeedsRevision }
    public enum PlayableProductMilestoneVerdict { ReadyForArchitectValidation, ReadyWithPreviewReserves, NeedsPlannerRevision, BlockedByMissingPlayableLoop, BlockedByLiveClaim, BlockedByHiddenServerAuthority, BlockedByBee501Premature }
    public enum Bee501BlockerStatus { BlockedUntilArchitectValidation, StillBlockedAfterRevision, ReleasedByFutureArchitectDecision }
    public sealed class PlayableSliceGateRow { public PlayableSliceGateRow(string domainId, string requiredProof, string forbiddenClaim, PlayableSliceGateStatus status) { DomainId = domainId ?? string.Empty; RequiredProof = requiredProof ?? string.Empty; ForbiddenClaim = forbiddenClaim ?? string.Empty; Status = status; } public string DomainId { get; } public string RequiredProof { get; } public string ForbiddenClaim { get; } public PlayableSliceGateStatus Status { get; } }
    public sealed class PlayableSliceReserve { public PlayableSliceReserve(string reserveId, string description) { ReserveId = reserveId ?? string.Empty; Description = description ?? string.Empty; } public string ReserveId { get; } public string Description { get; } }
    public sealed class PlayableProductMilestoneGate
    {
        private static readonly string[] RequiredDomains = { "premiere-minute", "hub", "ruche", "boucle-action", "feedbacks", "progression", "monde", "alliance", "communication", "evenements", "commerce", "conflit", "armee", "server-authority", "demo-qa" };
        public PlayableProductMilestoneGate(string gateId, IReadOnlyList<PlayableSliceGateRow> rows, IReadOnlyList<PlayableSliceReserve> reserves, Bee501BlockerStatus bee501Status)
        { GateId = ColonyIntegrationIds.Require(gateId); Rows = rows ?? Array.Empty<PlayableSliceGateRow>(); Reserves = reserves ?? Array.Empty<PlayableSliceReserve>(); Bee501Status = bee501Status; Verdict = EvaluateVerdict(); }
        public string GateId { get; } public IReadOnlyList<PlayableSliceGateRow> Rows { get; } public IReadOnlyList<PlayableSliceReserve> Reserves { get; } public PlayableProductMilestoneVerdict Verdict { get; } public Bee501BlockerStatus Bee501Status { get; }
        private PlayableProductMilestoneVerdict EvaluateVerdict()
        {
            if (Bee501Status == Bee501BlockerStatus.ReleasedByFutureArchitectDecision) return PlayableProductMilestoneVerdict.BlockedByBee501Premature;
            if (RequiredDomains.Any(domain => Rows.All(r => !string.Equals(r.DomainId, domain, StringComparison.OrdinalIgnoreCase))) || Rows.Any(r => r.Status == PlayableSliceGateStatus.MissingProof)) return PlayableProductMilestoneVerdict.BlockedByMissingPlayableLoop;
            if (Rows.Any(r => r.Status == PlayableSliceGateStatus.LiveClaim || string.IsNullOrWhiteSpace(r.ForbiddenClaim))) return PlayableProductMilestoneVerdict.BlockedByLiveClaim;
            if (Rows.Any(r => r.Status == PlayableSliceGateStatus.HiddenServerAuthority)) return PlayableProductMilestoneVerdict.BlockedByHiddenServerAuthority;
            if (Rows.Any(r => r.Status == PlayableSliceGateStatus.NeedsRevision)) return PlayableProductMilestoneVerdict.NeedsPlannerRevision;
            return Reserves.Count > 0 || Rows.Any(r => r.Status == PlayableSliceGateStatus.PreviewReserve) ? PlayableProductMilestoneVerdict.ReadyWithPreviewReserves : PlayableProductMilestoneVerdict.ReadyForArchitectValidation;
        }
    }
    public sealed class PlayableMilestoneGateEvaluated { public PlayableMilestoneGateEvaluated(string gateId, PlayableProductMilestoneVerdict verdict) { GateId = gateId ?? string.Empty; Verdict = verdict; } public string GateId { get; } public PlayableProductMilestoneVerdict Verdict { get; } }
    public sealed class PlayableMilestoneReserveRegistered { public PlayableMilestoneReserveRegistered(string reserveId) { ReserveId = reserveId ?? string.Empty; } public string ReserveId { get; } }
    public sealed class Bee501BlockerConfirmed { public Bee501BlockerConfirmed(Bee501BlockerStatus status) { Status = status; } public Bee501BlockerStatus Status { get; } }

    public sealed class PlayableSliceDiagnostics
    {
        public PlayableSliceDiagnostics(IReadOnlyList<PlayableSliceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlayableSliceDiagnosticCode>(); }
        public IReadOnlyList<PlayableSliceDiagnosticCode> Findings { get; }
        public bool Contains(PlayableSliceDiagnosticCode code) { return Findings.Contains(code); }
    }
}
