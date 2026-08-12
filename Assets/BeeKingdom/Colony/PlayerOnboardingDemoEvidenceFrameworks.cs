using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum DemoSurfaceVisibleState { Visible, VisibleWithFallback, Invisible, BlueOnly, Blocked }
    public enum SandboxPlaygroundContinuityVerdict { Observable, ObservableWithFallback, BlockedByMissingAnchor, BlockedByMissingFallback, BlockedByHiddenReadOnlyLimit, BlockedByInvisibleSurface, BlockedByBootstrapReplacement }
    public enum DemoVisualContinuityDiagnosticCode { DemoRenderableAnchorMissing, DemoFallbackVisualMissing, DemoReadOnlyLimitHidden, DemoSurfaceInvisible, DemoBootstrapReplacementForbidden }
    public sealed class DemoRenderableAnchor { public DemoRenderableAnchor(string anchorId, bool visible) { AnchorId = anchorId ?? string.Empty; Visible = visible; } public string AnchorId { get; } public bool Visible { get; } }
    public sealed class DemoFallbackVisualState { public DemoFallbackVisualState(string message, bool visible) { Message = message ?? string.Empty; Visible = visible; } public string Message { get; } public bool Visible { get; } }
    public sealed class DemoReadOnlyLimitNotice { public DemoReadOnlyLimitNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class DemoSurfaceVisibilityCheck
    {
        public DemoSurfaceVisibilityCheck(string surfaceId, DemoRenderableAnchor expectedAnchor, DemoSurfaceVisibleState visibleState, DemoFallbackVisualState fallbackMessage, DemoReadOnlyLimitNotice readOnlyNotice, string blockingReason)
        { SurfaceId = surfaceId ?? string.Empty; ExpectedAnchor = expectedAnchor; VisibleState = visibleState; FallbackMessage = fallbackMessage; ReadOnlyNotice = readOnlyNotice; BlockingReason = blockingReason ?? string.Empty; }
        public string SurfaceId { get; } public DemoRenderableAnchor ExpectedAnchor { get; } public DemoSurfaceVisibleState VisibleState { get; } public DemoFallbackVisualState FallbackMessage { get; } public DemoReadOnlyLimitNotice ReadOnlyNotice { get; } public string BlockingReason { get; }
    }
    public sealed class DemoVisualContinuityGuard
    {
        public DemoVisualContinuityGuard(string guardId, IReadOnlyList<DemoSurfaceVisibilityCheck> checks, bool bootstrapReplacementRequested = false) { GuardId = ColonyIntegrationIds.Require(guardId); Checks = checks ?? Array.Empty<DemoSurfaceVisibilityCheck>(); BootstrapReplacementRequested = bootstrapReplacementRequested; }
        public string GuardId { get; } public IReadOnlyList<DemoSurfaceVisibilityCheck> Checks { get; } public bool BootstrapReplacementRequested { get; }
        public DemoVisualContinuityDiagnostics Evaluate()
        {
            var findings = new List<DemoVisualContinuityDiagnosticCode>();
            if (Checks.Count == 0 || Checks.Any(c => c.ExpectedAnchor == null || string.IsNullOrWhiteSpace(c.ExpectedAnchor.AnchorId) || !c.ExpectedAnchor.Visible)) findings.Add(DemoVisualContinuityDiagnosticCode.DemoRenderableAnchorMissing);
            if (Checks.Any(c => c.FallbackMessage == null || string.IsNullOrWhiteSpace(c.FallbackMessage.Message) || !c.FallbackMessage.Visible)) findings.Add(DemoVisualContinuityDiagnosticCode.DemoFallbackVisualMissing);
            if (Checks.Any(c => c.ReadOnlyNotice == null || string.IsNullOrWhiteSpace(c.ReadOnlyNotice.Text) || !c.ReadOnlyNotice.Visible)) findings.Add(DemoVisualContinuityDiagnosticCode.DemoReadOnlyLimitHidden);
            if (Checks.Any(c => c.VisibleState == DemoSurfaceVisibleState.Invisible || c.VisibleState == DemoSurfaceVisibleState.BlueOnly || c.VisibleState == DemoSurfaceVisibleState.Blocked)) findings.Add(DemoVisualContinuityDiagnosticCode.DemoSurfaceInvisible);
            if (BootstrapReplacementRequested) findings.Add(DemoVisualContinuityDiagnosticCode.DemoBootstrapReplacementForbidden);
            return new DemoVisualContinuityDiagnostics(ResolveVerdict(findings), findings);
        }
        private static SandboxPlaygroundContinuityVerdict ResolveVerdict(IReadOnlyList<DemoVisualContinuityDiagnosticCode> findings)
        {
            if (findings.Contains(DemoVisualContinuityDiagnosticCode.DemoBootstrapReplacementForbidden)) return SandboxPlaygroundContinuityVerdict.BlockedByBootstrapReplacement;
            if (findings.Contains(DemoVisualContinuityDiagnosticCode.DemoSurfaceInvisible)) return SandboxPlaygroundContinuityVerdict.BlockedByInvisibleSurface;
            if (findings.Contains(DemoVisualContinuityDiagnosticCode.DemoReadOnlyLimitHidden)) return SandboxPlaygroundContinuityVerdict.BlockedByHiddenReadOnlyLimit;
            if (findings.Contains(DemoVisualContinuityDiagnosticCode.DemoFallbackVisualMissing)) return SandboxPlaygroundContinuityVerdict.BlockedByMissingFallback;
            if (findings.Contains(DemoVisualContinuityDiagnosticCode.DemoRenderableAnchorMissing)) return SandboxPlaygroundContinuityVerdict.BlockedByMissingAnchor;
            return SandboxPlaygroundContinuityVerdict.Observable;
        }
    }
    public sealed class DemoVisualContinuityDiagnostics { public DemoVisualContinuityDiagnostics(SandboxPlaygroundContinuityVerdict verdict, IReadOnlyList<DemoVisualContinuityDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<DemoVisualContinuityDiagnosticCode>(); } public SandboxPlaygroundContinuityVerdict Verdict { get; } public IReadOnlyList<DemoVisualContinuityDiagnosticCode> Findings { get; } public bool Contains(DemoVisualContinuityDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class DemoVisualContinuityChecked { public DemoVisualContinuityChecked(string guardId) { GuardId = guardId ?? string.Empty; } public string GuardId { get; } }
    public sealed class DemoFallbackVisualShown { public DemoFallbackVisualShown(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class DemoSurfaceVisibilityBlocked { public DemoSurfaceVisibilityBlocked(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }

    public enum OnboardingStepStatus { Advice, PreviewAction, BlockedAction, FutureAction }
    public enum OnboardingServerDependency { None, AccountFuture, ProgressionFuture, SocialFuture, WorldFuture }
    public enum OnboardingDiagnosticCode { OnboardingStepMissing, OnboardingExitRouteMissing, OnboardingRuntimeTutorialClaim, OnboardingServerDependencyHidden, OnboardingMobileOverloadRisk }
    public sealed class OnboardingSurfaceLink { public OnboardingSurfaceLink(string surfaceId) { SurfaceId = surfaceId ?? string.Empty; } public string SurfaceId { get; } }
    public sealed class OnboardingExitRoute { public OnboardingExitRoute(string targetSurface, bool visible) { TargetSurface = targetSurface ?? string.Empty; Visible = visible; } public string TargetSurface { get; } public bool Visible { get; } }
    public sealed class OnboardingStepPreview
    {
        public OnboardingStepPreview(string stepId, string surface, string playerPrompt, OnboardingStepStatus status, OnboardingSurfaceLink navigationTarget, OnboardingServerDependency serverDependency, OnboardingExitRoute exitRoute, bool runtimeTutorialClaim = false, bool serverDependencyVisible = true)
        { StepId = stepId ?? string.Empty; Surface = surface ?? string.Empty; PlayerPrompt = playerPrompt ?? string.Empty; Status = status; NavigationTarget = navigationTarget; ServerDependency = serverDependency; ExitRoute = exitRoute; RuntimeTutorialClaim = runtimeTutorialClaim; ServerDependencyVisible = serverDependencyVisible; }
        public string StepId { get; } public string Surface { get; } public string PlayerPrompt { get; } public OnboardingStepStatus Status { get; } public OnboardingSurfaceLink NavigationTarget { get; } public OnboardingServerDependency ServerDependency { get; } public OnboardingExitRoute ExitRoute { get; } public bool RuntimeTutorialClaim { get; } public bool ServerDependencyVisible { get; }
    }
    public sealed class PlayerOnboardingPath
    {
        public PlayerOnboardingPath(string pathId, IReadOnlyList<OnboardingStepPreview> steps, int mobileVisibleStepCount) { PathId = ColonyIntegrationIds.Require(pathId); Steps = steps ?? Array.Empty<OnboardingStepPreview>(); MobileVisibleStepCount = mobileVisibleStepCount; }
        public string PathId { get; } public IReadOnlyList<OnboardingStepPreview> Steps { get; } public int MobileVisibleStepCount { get; }
        public OnboardingDiagnostics Evaluate()
        {
            var findings = new List<OnboardingDiagnosticCode>();
            if (Steps.Count < 6 || Steps.Any(s => string.IsNullOrWhiteSpace(s.StepId) || string.IsNullOrWhiteSpace(s.Surface) || string.IsNullOrWhiteSpace(s.PlayerPrompt) || s.NavigationTarget == null)) findings.Add(OnboardingDiagnosticCode.OnboardingStepMissing);
            if (Steps.Any(s => s.ExitRoute == null || !s.ExitRoute.Visible || string.IsNullOrWhiteSpace(s.ExitRoute.TargetSurface))) findings.Add(OnboardingDiagnosticCode.OnboardingExitRouteMissing);
            if (Steps.Any(s => s.RuntimeTutorialClaim)) findings.Add(OnboardingDiagnosticCode.OnboardingRuntimeTutorialClaim);
            if (Steps.Any(s => s.ServerDependency != OnboardingServerDependency.None && !s.ServerDependencyVisible)) findings.Add(OnboardingDiagnosticCode.OnboardingServerDependencyHidden);
            if (MobileVisibleStepCount > 6) findings.Add(OnboardingDiagnosticCode.OnboardingMobileOverloadRisk);
            return new OnboardingDiagnostics(findings);
        }
    }
    public sealed class OnboardingDiagnostics { public OnboardingDiagnostics(IReadOnlyList<OnboardingDiagnosticCode> findings) { Findings = findings ?? Array.Empty<OnboardingDiagnosticCode>(); } public IReadOnlyList<OnboardingDiagnosticCode> Findings { get; } public bool Contains(OnboardingDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class PlayerOnboardingPathStarted { public PlayerOnboardingPathStarted(string pathId) { PathId = pathId ?? string.Empty; } public string PathId { get; } }
    public sealed class PlayerOnboardingStepViewed { public PlayerOnboardingStepViewed(string stepId) { StepId = stepId ?? string.Empty; } public string StepId { get; } }
    public sealed class PlayerOnboardingStepBlocked { public PlayerOnboardingStepBlocked(string stepId) { StepId = stepId ?? string.Empty; } public string StepId { get; } }

    public enum HiveProfileVisibilityState { ExampleOnly, PrivatePreview, PublicFuture, Blocked }
    public enum HiveProfileServerDependency { None, AccountRequired, UniqueNameRequired, PublicProfileRequired, AllianceRequired }
    public enum PlayerStyleSignalKind { Peaceful, Defensive, Expansionist, Warlike, Unknown }
    public enum HiveProfileDiagnosticCode { HiveProfilePersistenceForbidden, PlayerIdentityServerDependencyHidden, PersonalDataForbidden, HiveProfileRankingClaimForbidden, PlayerStyleSignalMissing }
    public sealed class HiveIdentityBadge { public HiveIdentityBadge(string label, bool exampleOnly) { Label = label ?? string.Empty; ExampleOnly = exampleOnly; } public string Label { get; } public bool ExampleOnly { get; } }
    public sealed class PlayerStyleSignalPreview { public PlayerStyleSignalPreview(PlayerStyleSignalKind style, string text) { Style = style; Text = text ?? string.Empty; } public PlayerStyleSignalKind Style { get; } public string Text { get; } }
    public sealed class ProfilePrivacyFutureMarker { public ProfilePrivacyFutureMarker(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class PlayerHiveProfilePreview
    {
        public PlayerHiveProfilePreview(string profileId, string displayNamePreview, string hiveNamePreview, PlayerStyleSignalPreview styleSignal, HiveProfileVisibilityState visibilityState, HiveProfileServerDependency serverDependency, ProfilePrivacyFutureMarker privacyMarker, bool persistent = false, bool containsPersonalData = false, bool rankingClaim = false, bool serverDependencyVisible = true)
        { ProfileId = profileId ?? string.Empty; DisplayNamePreview = displayNamePreview ?? string.Empty; HiveNamePreview = hiveNamePreview ?? string.Empty; StyleSignal = styleSignal; VisibilityState = visibilityState; ServerDependency = serverDependency; PrivacyMarker = privacyMarker; Persistent = persistent; ContainsPersonalData = containsPersonalData; RankingClaim = rankingClaim; ServerDependencyVisible = serverDependencyVisible; }
        public string ProfileId { get; } public string DisplayNamePreview { get; } public string HiveNamePreview { get; } public PlayerStyleSignalPreview StyleSignal { get; } public HiveProfileVisibilityState VisibilityState { get; } public HiveProfileServerDependency ServerDependency { get; } public ProfilePrivacyFutureMarker PrivacyMarker { get; } public bool Persistent { get; } public bool ContainsPersonalData { get; } public bool RankingClaim { get; } public bool ServerDependencyVisible { get; }
        public HiveProfileDiagnostics Evaluate()
        {
            var findings = new List<HiveProfileDiagnosticCode>();
            if (Persistent) findings.Add(HiveProfileDiagnosticCode.HiveProfilePersistenceForbidden);
            if (ServerDependency != HiveProfileServerDependency.None && !ServerDependencyVisible) findings.Add(HiveProfileDiagnosticCode.PlayerIdentityServerDependencyHidden);
            if (ContainsPersonalData) findings.Add(HiveProfileDiagnosticCode.PersonalDataForbidden);
            if (RankingClaim) findings.Add(HiveProfileDiagnosticCode.HiveProfileRankingClaimForbidden);
            if (StyleSignal == null || StyleSignal.Style == PlayerStyleSignalKind.Unknown || string.IsNullOrWhiteSpace(StyleSignal.Text)) findings.Add(HiveProfileDiagnosticCode.PlayerStyleSignalMissing);
            return new HiveProfileDiagnostics(findings);
        }
    }
    public sealed class HiveProfileDiagnostics { public HiveProfileDiagnostics(IReadOnlyList<HiveProfileDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveProfileDiagnosticCode>(); } public IReadOnlyList<HiveProfileDiagnosticCode> Findings { get; } public bool Contains(HiveProfileDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveProfilePreviewOpened { public HiveProfilePreviewOpened(string profileId) { ProfileId = profileId ?? string.Empty; } public string ProfileId { get; } }
    public sealed class HiveIdentityBadgeViewed { public HiveIdentityBadgeViewed(string profileId) { ProfileId = profileId ?? string.Empty; } public string ProfileId { get; } }
    public sealed class PlayerStyleSignalInspected { public PlayerStyleSignalInspected(string profileId) { ProfileId = profileId ?? string.Empty; } public string ProfileId { get; } }

    public enum PlaystyleLockState { PreviewSelectable, PreviewSelected, PersistentBlocked, LockedForbidden }
    public enum PlaystyleDiagnosticCode { PlaystyleOfficialBonusForbidden, PlaystyleMatchmakingClaimForbidden, PlaystylePersistenceHidden, PlaystyleMonetizationClaimForbidden, PlaystyleReversibilityMissing }
    public sealed class PlaystyleImpactHint { public PlaystyleImpactHint(string text, bool officialBonus = false, bool matchmakingClaim = false, bool monetizationClaim = false) { Text = text ?? string.Empty; OfficialBonus = officialBonus; MatchmakingClaim = matchmakingClaim; MonetizationClaim = monetizationClaim; } public string Text { get; } public bool OfficialBonus { get; } public bool MatchmakingClaim { get; } public bool MonetizationClaim { get; } }
    public sealed class PlaystyleServerDependency { public PlaystyleServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class PlaystyleReversibilityNotice { public PlaystyleReversibilityNotice(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class PlaystyleOptionCard
    {
        public PlaystyleOptionCard(string playstyleId, string playerDescription, PlaystyleImpactHint futureImpactHint, bool previewSelected, PlaystyleReversibilityNotice reversibilityNotice, PlaystyleServerDependency serverDependency, PlaystyleLockState lockState)
        { PlaystyleId = playstyleId ?? string.Empty; PlayerDescription = playerDescription ?? string.Empty; FutureImpactHint = futureImpactHint; PreviewSelected = previewSelected; ReversibilityNotice = reversibilityNotice; ServerDependency = serverDependency; LockState = lockState; }
        public string PlaystyleId { get; } public string PlayerDescription { get; } public PlaystyleImpactHint FutureImpactHint { get; } public bool PreviewSelected { get; } public PlaystyleReversibilityNotice ReversibilityNotice { get; } public PlaystyleServerDependency ServerDependency { get; } public PlaystyleLockState LockState { get; }
    }
    public sealed class PlaystyleSelectionPreview
    {
        public PlaystyleSelectionPreview(string previewId, IReadOnlyList<PlaystyleOptionCard> options) { PreviewId = ColonyIntegrationIds.Require(previewId); Options = options ?? Array.Empty<PlaystyleOptionCard>(); }
        public string PreviewId { get; } public IReadOnlyList<PlaystyleOptionCard> Options { get; }
        public PlaystyleDiagnostics Evaluate()
        {
            var findings = new List<PlaystyleDiagnosticCode>();
            if (Options.Any(o => o.FutureImpactHint != null && o.FutureImpactHint.OfficialBonus)) findings.Add(PlaystyleDiagnosticCode.PlaystyleOfficialBonusForbidden);
            if (Options.Any(o => o.FutureImpactHint != null && o.FutureImpactHint.MatchmakingClaim)) findings.Add(PlaystyleDiagnosticCode.PlaystyleMatchmakingClaimForbidden);
            if (Options.Any(o => o.LockState != PlaystyleLockState.PersistentBlocked || o.ServerDependency == null || !o.ServerDependency.Visible)) findings.Add(PlaystyleDiagnosticCode.PlaystylePersistenceHidden);
            if (Options.Any(o => o.FutureImpactHint != null && o.FutureImpactHint.MonetizationClaim)) findings.Add(PlaystyleDiagnosticCode.PlaystyleMonetizationClaimForbidden);
            if (Options.Any(o => o.ReversibilityNotice == null || !o.ReversibilityNotice.Visible)) findings.Add(PlaystyleDiagnosticCode.PlaystyleReversibilityMissing);
            return new PlaystyleDiagnostics(findings);
        }
    }
    public sealed class PlaystyleDiagnostics { public PlaystyleDiagnostics(IReadOnlyList<PlaystyleDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlaystyleDiagnosticCode>(); } public IReadOnlyList<PlaystyleDiagnosticCode> Findings { get; } public bool Contains(PlaystyleDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class PlaystylePreviewOpened { public PlaystylePreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class PlaystyleOptionInspected { public PlaystyleOptionInspected(string playstyleId) { PlaystyleId = playstyleId ?? string.Empty; } public string PlaystyleId { get; } }
    public sealed class PlaystylePreviewSelectionBlocked { public PlaystylePreviewSelectionBlocked(string playstyleId) { PlaystyleId = playstyleId ?? string.Empty; } public string PlaystyleId { get; } }

    public enum GoalStackPriority { Primary, Secondary, Optional }
    public enum GoalStackServerDependency { None, AccountFuture, ProgressionFuture, RewardFuture, AnalyticsFuture }
    public enum FirstSessionGoalDiagnosticCode { FirstSessionGoalMissingSurface, FirstSessionRewardForbidden, FirstSessionCompletionOfficialClaim, FirstSessionGoalOverload, FirstSessionServerDependencyHidden }
    public sealed class GoalStackCompletionPreview { public GoalStackCompletionPreview(bool previewComplete, bool officialClaim = false) { PreviewComplete = previewComplete; OfficialClaim = officialClaim; } public bool PreviewComplete { get; } public bool OfficialClaim { get; } }
    public sealed class GoalStackRewardBlocker { public GoalStackRewardBlocker(bool rewardRequested = false) { RewardRequested = rewardRequested; } public bool RewardRequested { get; } }
    public sealed class FirstSessionGoalItem
    {
        public FirstSessionGoalItem(string goalId, GoalStackPriority priority, string playerOutcome, string linkedSurface, GoalStackCompletionPreview completionPreview, GoalStackRewardBlocker rewardBlocker, GoalStackServerDependency serverDependency, bool serverDependencyVisible = true)
        { GoalId = goalId ?? string.Empty; Priority = priority; PlayerOutcome = playerOutcome ?? string.Empty; LinkedSurface = linkedSurface ?? string.Empty; CompletionPreview = completionPreview; RewardBlocker = rewardBlocker; ServerDependency = serverDependency; ServerDependencyVisible = serverDependencyVisible; }
        public string GoalId { get; } public GoalStackPriority Priority { get; } public string PlayerOutcome { get; } public string LinkedSurface { get; } public GoalStackCompletionPreview CompletionPreview { get; } public GoalStackRewardBlocker RewardBlocker { get; } public GoalStackServerDependency ServerDependency { get; } public bool ServerDependencyVisible { get; }
    }
    public sealed class FirstSessionGoalStack
    {
        public FirstSessionGoalStack(string stackId, IReadOnlyList<FirstSessionGoalItem> goals, int initiallyVisibleGoals) { StackId = ColonyIntegrationIds.Require(stackId); Goals = goals ?? Array.Empty<FirstSessionGoalItem>(); InitiallyVisibleGoals = initiallyVisibleGoals; }
        public string StackId { get; } public IReadOnlyList<FirstSessionGoalItem> Goals { get; } public int InitiallyVisibleGoals { get; }
        public FirstSessionGoalDiagnostics Evaluate()
        {
            var findings = new List<FirstSessionGoalDiagnosticCode>();
            if (Goals.Count == 0 || Goals.Any(g => string.IsNullOrWhiteSpace(g.LinkedSurface))) findings.Add(FirstSessionGoalDiagnosticCode.FirstSessionGoalMissingSurface);
            if (Goals.Any(g => g.RewardBlocker != null && g.RewardBlocker.RewardRequested)) findings.Add(FirstSessionGoalDiagnosticCode.FirstSessionRewardForbidden);
            if (Goals.Any(g => g.CompletionPreview != null && g.CompletionPreview.OfficialClaim)) findings.Add(FirstSessionGoalDiagnosticCode.FirstSessionCompletionOfficialClaim);
            if (InitiallyVisibleGoals > 3) findings.Add(FirstSessionGoalDiagnosticCode.FirstSessionGoalOverload);
            if (Goals.Any(g => g.ServerDependency != GoalStackServerDependency.None && !g.ServerDependencyVisible)) findings.Add(FirstSessionGoalDiagnosticCode.FirstSessionServerDependencyHidden);
            return new FirstSessionGoalDiagnostics(findings);
        }
    }
    public sealed class FirstSessionGoalDiagnostics { public FirstSessionGoalDiagnostics(IReadOnlyList<FirstSessionGoalDiagnosticCode> findings) { Findings = findings ?? Array.Empty<FirstSessionGoalDiagnosticCode>(); } public IReadOnlyList<FirstSessionGoalDiagnosticCode> Findings { get; } public bool Contains(FirstSessionGoalDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class FirstSessionGoalStackShown { public FirstSessionGoalStackShown(string stackId) { StackId = stackId ?? string.Empty; } public string StackId { get; } }
    public sealed class FirstSessionGoalInspected { public FirstSessionGoalInspected(string goalId) { GoalId = goalId ?? string.Empty; } public string GoalId { get; } }
    public sealed class FirstSessionGoalBlocked { public FirstSessionGoalBlocked(string goalId) { GoalId = goalId ?? string.Empty; } public string GoalId { get; } }

    public enum SocialInvitationStatus { PreviewOnly, RuntimeBlocked, SentForbidden }
    public enum AllyDiscoveryDiagnosticCode { AllyCandidatePersonalDataForbidden, SocialInvitationRuntimeForbidden, AllyDiscoveryMatchmakingClaim, SocialPrivacyNoticeMissing, InvitationServerDependencyHidden }
    public sealed class CompatibilityHint { public CompatibilityHint(string reason, bool matchmakingClaim = false) { Reason = reason ?? string.Empty; MatchmakingClaim = matchmakingClaim; } public string Reason { get; } public bool MatchmakingClaim { get; } }
    public sealed class InvitationServerDependency { public InvitationServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class SocialPrivacyGuard { public SocialPrivacyGuard(bool visible, bool personalDataPresent = false) { Visible = visible; PersonalDataPresent = personalDataPresent; } public bool Visible { get; } public bool PersonalDataPresent { get; } }
    public sealed class AllyCandidatePreviewCard
    {
        public AllyCandidatePreviewCard(string candidateId, string displayLabel, PlayerStyleSignalKind playstyleHint, CompatibilityHint compatibilityReason, SocialPrivacyGuard privacyMarker, SocialInvitationStatus invitationStatus, InvitationServerDependency serverDependency)
        { CandidateId = candidateId ?? string.Empty; DisplayLabel = displayLabel ?? string.Empty; PlaystyleHint = playstyleHint; CompatibilityReason = compatibilityReason; PrivacyMarker = privacyMarker; InvitationStatus = invitationStatus; ServerDependency = serverDependency; }
        public string CandidateId { get; } public string DisplayLabel { get; } public PlayerStyleSignalKind PlaystyleHint { get; } public CompatibilityHint CompatibilityReason { get; } public SocialPrivacyGuard PrivacyMarker { get; } public SocialInvitationStatus InvitationStatus { get; } public InvitationServerDependency ServerDependency { get; }
    }
    public sealed class SocialInvitationPreview { public SocialInvitationPreview(string invitationId, bool runtimeRequested) { InvitationId = invitationId ?? string.Empty; RuntimeRequested = runtimeRequested; } public string InvitationId { get; } public bool RuntimeRequested { get; } }
    public sealed class AllyDiscoveryPreview
    {
        public AllyDiscoveryPreview(string previewId, IReadOnlyList<AllyCandidatePreviewCard> candidates, SocialInvitationPreview invitationPreview) { PreviewId = ColonyIntegrationIds.Require(previewId); Candidates = candidates ?? Array.Empty<AllyCandidatePreviewCard>(); InvitationPreview = invitationPreview; }
        public string PreviewId { get; } public IReadOnlyList<AllyCandidatePreviewCard> Candidates { get; } public SocialInvitationPreview InvitationPreview { get; }
        public AllyDiscoveryDiagnostics Evaluate()
        {
            var findings = new List<AllyDiscoveryDiagnosticCode>();
            if (Candidates.Any(c => c.PrivacyMarker != null && c.PrivacyMarker.PersonalDataPresent)) findings.Add(AllyDiscoveryDiagnosticCode.AllyCandidatePersonalDataForbidden);
            if (InvitationPreview != null && InvitationPreview.RuntimeRequested || Candidates.Any(c => c.InvitationStatus == SocialInvitationStatus.SentForbidden)) findings.Add(AllyDiscoveryDiagnosticCode.SocialInvitationRuntimeForbidden);
            if (Candidates.Any(c => c.CompatibilityReason != null && c.CompatibilityReason.MatchmakingClaim)) findings.Add(AllyDiscoveryDiagnosticCode.AllyDiscoveryMatchmakingClaim);
            if (Candidates.Any(c => c.PrivacyMarker == null || !c.PrivacyMarker.Visible)) findings.Add(AllyDiscoveryDiagnosticCode.SocialPrivacyNoticeMissing);
            if (Candidates.Any(c => c.ServerDependency == null || !c.ServerDependency.Visible)) findings.Add(AllyDiscoveryDiagnosticCode.InvitationServerDependencyHidden);
            return new AllyDiscoveryDiagnostics(findings);
        }
    }
    public sealed class AllyDiscoveryDiagnostics { public AllyDiscoveryDiagnostics(IReadOnlyList<AllyDiscoveryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllyDiscoveryDiagnosticCode>(); } public IReadOnlyList<AllyDiscoveryDiagnosticCode> Findings { get; } public bool Contains(AllyDiscoveryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class AllyDiscoveryPreviewOpened { public AllyDiscoveryPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class AllyCandidatePreviewInspected { public AllyCandidatePreviewInspected(string candidateId) { CandidateId = candidateId ?? string.Empty; } public string CandidateId { get; } }
    public sealed class SocialInvitationPreviewBlocked { public SocialInvitationPreviewBlocked(string invitationId) { InvitationId = invitationId ?? string.Empty; } public string InvitationId { get; } }

    public enum NonAggressivePosture { Peaceful, Defensive, Expansionist }
    public enum PeaceDefenseExpansionDiagnosticCode { PeacefulRewardOfficialForbidden, DefenseRuntimeEffectForbidden, ExpansionTerritoryClaimForbidden, EconomyServerDependencyHidden, NonAggressionMessageMissing }
    public sealed class NonAggressionLimitNotice { public NonAggressionLimitNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class WorldEconomyServerDependency { public WorldEconomyServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class PeaceDefenseExpansionIntent
    {
        public PeaceDefenseExpansionIntent(string intentId, NonAggressivePosture posture, string playerMeaning, string relatedSurface, string forbiddenRuntimeEffect, WorldEconomyServerDependency serverDependency, string nextPreviewRoute, NonAggressionLimitNotice nonAggressionNotice, bool officialReward = false, bool defenseRuntimeEffect = false, bool territoryClaim = false)
        { IntentId = intentId ?? string.Empty; Posture = posture; PlayerMeaning = playerMeaning ?? string.Empty; RelatedSurface = relatedSurface ?? string.Empty; ForbiddenRuntimeEffect = forbiddenRuntimeEffect ?? string.Empty; ServerDependency = serverDependency; NextPreviewRoute = nextPreviewRoute ?? string.Empty; NonAggressionNotice = nonAggressionNotice; OfficialReward = officialReward; DefenseRuntimeEffect = defenseRuntimeEffect; TerritoryClaim = territoryClaim; }
        public string IntentId { get; } public NonAggressivePosture Posture { get; } public string PlayerMeaning { get; } public string RelatedSurface { get; } public string ForbiddenRuntimeEffect { get; } public WorldEconomyServerDependency ServerDependency { get; } public string NextPreviewRoute { get; } public NonAggressionLimitNotice NonAggressionNotice { get; } public bool OfficialReward { get; } public bool DefenseRuntimeEffect { get; } public bool TerritoryClaim { get; }
    }
    public sealed class PeacefulGrowthPreview { public PeacefulGrowthPreview(PeaceDefenseExpansionIntent intent) { Intent = intent; } public PeaceDefenseExpansionIntent Intent { get; } }
    public sealed class DefensePreparationPreview { public DefensePreparationPreview(PeaceDefenseExpansionIntent intent) { Intent = intent; } public PeaceDefenseExpansionIntent Intent { get; } }
    public sealed class ExpansionOpportunityPreview { public ExpansionOpportunityPreview(PeaceDefenseExpansionIntent intent) { Intent = intent; } public PeaceDefenseExpansionIntent Intent { get; } }
    public sealed class PeaceDefenseExpansionPreview
    {
        public PeaceDefenseExpansionPreview(string previewId, IReadOnlyList<PeaceDefenseExpansionIntent> intents) { PreviewId = ColonyIntegrationIds.Require(previewId); Intents = intents ?? Array.Empty<PeaceDefenseExpansionIntent>(); }
        public string PreviewId { get; } public IReadOnlyList<PeaceDefenseExpansionIntent> Intents { get; }
        public PeaceDefenseExpansionDiagnostics Evaluate()
        {
            var findings = new List<PeaceDefenseExpansionDiagnosticCode>();
            if (Intents.Any(i => i.OfficialReward)) findings.Add(PeaceDefenseExpansionDiagnosticCode.PeacefulRewardOfficialForbidden);
            if (Intents.Any(i => i.DefenseRuntimeEffect)) findings.Add(PeaceDefenseExpansionDiagnosticCode.DefenseRuntimeEffectForbidden);
            if (Intents.Any(i => i.TerritoryClaim)) findings.Add(PeaceDefenseExpansionDiagnosticCode.ExpansionTerritoryClaimForbidden);
            if (Intents.Any(i => i.ServerDependency == null || !i.ServerDependency.Visible)) findings.Add(PeaceDefenseExpansionDiagnosticCode.EconomyServerDependencyHidden);
            if (Intents.Any(i => i.NonAggressionNotice == null || !i.NonAggressionNotice.Visible || string.IsNullOrWhiteSpace(i.NonAggressionNotice.Text))) findings.Add(PeaceDefenseExpansionDiagnosticCode.NonAggressionMessageMissing);
            return new PeaceDefenseExpansionDiagnostics(findings);
        }
    }
    public sealed class PeaceDefenseExpansionDiagnostics { public PeaceDefenseExpansionDiagnostics(IReadOnlyList<PeaceDefenseExpansionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PeaceDefenseExpansionDiagnosticCode>(); } public IReadOnlyList<PeaceDefenseExpansionDiagnosticCode> Findings { get; } public bool Contains(PeaceDefenseExpansionDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class PeaceDefenseExpansionPreviewOpened { public PeaceDefenseExpansionPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class NonAggressiveIntentInspected { public NonAggressiveIntentInspected(string intentId) { IntentId = intentId ?? string.Empty; } public string IntentId { get; } }
    public sealed class ExpansionIntentBlocked { public ExpansionIntentBlocked(string intentId) { IntentId = intentId ?? string.Empty; } public string IntentId { get; } }

    public enum NonBlankFrameCheck { NonBlank, Blank, UniformBlue, Missing }
    public enum EvidencePrivacyStatus { Safe, PersonalDataPresent, Redacted }
    public enum PlayModeCaptureVerdict { EvidenceReady, ReadyWithReserve, BlockedByMissingEvidence, BlockedByBlankFrame, BlockedByMissingLimit, BlockedByPersonalData, BlockedByStrongClaim }
    public enum DemoEvidenceDiagnosticCode { VisualEvidenceMissing, VisualFrameBlank, EvidenceLimitNoticeMissing, PersonalDataInCaptureForbidden, EvidenceClaimTooStrong }
    public sealed class EvidenceLimitNotice { public EvidenceLimitNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class VisualEvidenceFrame
    {
        public VisualEvidenceFrame(string frameId, string surface, string capturePurpose, NonBlankFrameCheck nonBlankVerdict, EvidenceLimitNotice visibleLimitNotice, EvidencePrivacyStatus privacyStatus, string evidenceScope, bool productionClaim = false)
        { FrameId = frameId ?? string.Empty; Surface = surface ?? string.Empty; CapturePurpose = capturePurpose ?? string.Empty; NonBlankVerdict = nonBlankVerdict; VisibleLimitNotice = visibleLimitNotice; PrivacyStatus = privacyStatus; EvidenceScope = evidenceScope ?? string.Empty; ProductionClaim = productionClaim; }
        public string FrameId { get; } public string Surface { get; } public string CapturePurpose { get; } public NonBlankFrameCheck NonBlankVerdict { get; } public EvidenceLimitNotice VisibleLimitNotice { get; } public EvidencePrivacyStatus PrivacyStatus { get; } public string EvidenceScope { get; } public bool ProductionClaim { get; }
    }
    public class DemoSurfaceCaptureManifest
    {
        public DemoSurfaceCaptureManifest(string manifestId, IReadOnlyList<VisualEvidenceFrame> frames) { ManifestId = ColonyIntegrationIds.Require(manifestId); Frames = frames ?? Array.Empty<VisualEvidenceFrame>(); }
        public string ManifestId { get; } public IReadOnlyList<VisualEvidenceFrame> Frames { get; }
        public DemoEvidenceDiagnostics Evaluate()
        {
            var findings = new List<DemoEvidenceDiagnosticCode>();
            if (Frames.Count == 0 || Frames.Any(f => string.IsNullOrWhiteSpace(f.FrameId) || string.IsNullOrWhiteSpace(f.Surface))) findings.Add(DemoEvidenceDiagnosticCode.VisualEvidenceMissing);
            if (Frames.Any(f => f.NonBlankVerdict != NonBlankFrameCheck.NonBlank)) findings.Add(DemoEvidenceDiagnosticCode.VisualFrameBlank);
            if (Frames.Any(f => f.VisibleLimitNotice == null || !f.VisibleLimitNotice.Visible || string.IsNullOrWhiteSpace(f.VisibleLimitNotice.Text))) findings.Add(DemoEvidenceDiagnosticCode.EvidenceLimitNoticeMissing);
            if (Frames.Any(f => f.PrivacyStatus == EvidencePrivacyStatus.PersonalDataPresent)) findings.Add(DemoEvidenceDiagnosticCode.PersonalDataInCaptureForbidden);
            if (Frames.Any(f => f.ProductionClaim)) findings.Add(DemoEvidenceDiagnosticCode.EvidenceClaimTooStrong);
            return new DemoEvidenceDiagnostics(ResolveVerdict(findings), findings);
        }
        private static PlayModeCaptureVerdict ResolveVerdict(IReadOnlyList<DemoEvidenceDiagnosticCode> findings)
        {
            if (findings.Contains(DemoEvidenceDiagnosticCode.EvidenceClaimTooStrong)) return PlayModeCaptureVerdict.BlockedByStrongClaim;
            if (findings.Contains(DemoEvidenceDiagnosticCode.PersonalDataInCaptureForbidden)) return PlayModeCaptureVerdict.BlockedByPersonalData;
            if (findings.Contains(DemoEvidenceDiagnosticCode.EvidenceLimitNoticeMissing)) return PlayModeCaptureVerdict.BlockedByMissingLimit;
            if (findings.Contains(DemoEvidenceDiagnosticCode.VisualFrameBlank)) return PlayModeCaptureVerdict.BlockedByBlankFrame;
            if (findings.Contains(DemoEvidenceDiagnosticCode.VisualEvidenceMissing)) return PlayModeCaptureVerdict.BlockedByMissingEvidence;
            return PlayModeCaptureVerdict.EvidenceReady;
        }
    }
    public sealed class DemoPlayModeEvidenceCapture : DemoSurfaceCaptureManifest { public DemoPlayModeEvidenceCapture(string manifestId, IReadOnlyList<VisualEvidenceFrame> frames) : base(manifestId, frames) { } }
    public sealed class DemoEvidenceDiagnostics { public DemoEvidenceDiagnostics(PlayModeCaptureVerdict verdict, IReadOnlyList<DemoEvidenceDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<DemoEvidenceDiagnosticCode>(); } public PlayModeCaptureVerdict Verdict { get; } public IReadOnlyList<DemoEvidenceDiagnosticCode> Findings { get; } public bool Contains(DemoEvidenceDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class DemoPlayModeEvidenceRequested { public DemoPlayModeEvidenceRequested(string manifestId) { ManifestId = manifestId ?? string.Empty; } public string ManifestId { get; } }
    public sealed class DemoVisualFrameCaptured { public DemoVisualFrameCaptured(string frameId) { FrameId = frameId ?? string.Empty; } public string FrameId { get; } }
    public sealed class DemoVisualEvidenceRejected { public DemoVisualEvidenceRejected(string frameId) { FrameId = frameId ?? string.Empty; } public string FrameId { get; } }

    public enum DemoEvidenceReadinessVerdict { ReadyForArchitectValidation, ReadyWithDemoEvidenceReserve, NeedsPlannerRevision, BlockedByMissingSurface, BlockedByHiddenServerDependency, BlockedByVisualRegressionRisk, BlockedByProductionClaim, BlockedByBee431Premature }
    public enum OnboardingClosureDiagnosticCode { OnboardingLotSurfaceMissing, DemoEvidenceReserveHidden, ServerDependencyAuditMissing, ProductionClaimDetected, Bee431PrematureRelease }
    public sealed class PlayerSurfaceLimitAudit { public PlayerSurfaceLimitAudit(bool productionClaim = false, bool visualRegressionRisk = false) { ProductionClaim = productionClaim; VisualRegressionRisk = visualRegressionRisk; } public bool ProductionClaim { get; } public bool VisualRegressionRisk { get; } }
    public sealed class ServerDependencyVisibilityAudit { public ServerDependencyVisibilityAudit(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class Bee431BlockerStatus { public Bee431BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class OnboardingLotCoverageMatrix
    {
        public OnboardingLotCoverageMatrix(string beeId, string playerSurface, string demoEvidence, string uiNeed, string qaCheck, string serverBoundary, DemoEvidenceReadinessVerdict closureVerdict)
        { BeeId = beeId ?? string.Empty; PlayerSurface = playerSurface ?? string.Empty; DemoEvidence = demoEvidence ?? string.Empty; UiNeed = uiNeed ?? string.Empty; QaCheck = qaCheck ?? string.Empty; ServerBoundary = serverBoundary ?? string.Empty; ClosureVerdict = closureVerdict; }
        public string BeeId { get; } public string PlayerSurface { get; } public string DemoEvidence { get; } public string UiNeed { get; } public string QaCheck { get; } public string ServerBoundary { get; } public DemoEvidenceReadinessVerdict ClosureVerdict { get; }
    }
    public sealed class PlayerOnboardingDemoEvidenceClosureGate
    {
        public const string Bee431BlockedMessage = "BEE-431 bloquee jusqu'a validation architecte.";
        public PlayerOnboardingDemoEvidenceClosureGate(string gateId, IReadOnlyList<OnboardingLotCoverageMatrix> coverage, PlayerSurfaceLimitAudit surfaceLimitAudit, ServerDependencyVisibilityAudit serverDependencyAudit, Bee431BlockerStatus bee431BlockerStatus)
        { GateId = ColonyIntegrationIds.Require(gateId); Coverage = coverage ?? Array.Empty<OnboardingLotCoverageMatrix>(); SurfaceLimitAudit = surfaceLimitAudit ?? new PlayerSurfaceLimitAudit(); ServerDependencyAudit = serverDependencyAudit; Bee431BlockerStatus = bee431BlockerStatus ?? new Bee431BlockerStatus(false, Bee431BlockedMessage); }
        public string GateId { get; } public IReadOnlyList<OnboardingLotCoverageMatrix> Coverage { get; } public PlayerSurfaceLimitAudit SurfaceLimitAudit { get; } public ServerDependencyVisibilityAudit ServerDependencyAudit { get; } public Bee431BlockerStatus Bee431BlockerStatus { get; }
        public OnboardingClosureDiagnostics Evaluate()
        {
            var findings = new List<OnboardingClosureDiagnosticCode>();
            if (Coverage.Count < 8 || Coverage.Any(c => string.IsNullOrWhiteSpace(c.BeeId) || string.IsNullOrWhiteSpace(c.PlayerSurface))) findings.Add(OnboardingClosureDiagnosticCode.OnboardingLotSurfaceMissing);
            if (Coverage.Any(c => string.IsNullOrWhiteSpace(c.DemoEvidence))) findings.Add(OnboardingClosureDiagnosticCode.DemoEvidenceReserveHidden);
            if (ServerDependencyAudit == null || !ServerDependencyAudit.Visible || Coverage.Any(c => string.IsNullOrWhiteSpace(c.ServerBoundary))) findings.Add(OnboardingClosureDiagnosticCode.ServerDependencyAuditMissing);
            if (SurfaceLimitAudit.ProductionClaim) findings.Add(OnboardingClosureDiagnosticCode.ProductionClaimDetected);
            if (Bee431BlockerStatus.PrematureAttempt) findings.Add(OnboardingClosureDiagnosticCode.Bee431PrematureRelease);
            return new OnboardingClosureDiagnostics(ResolveVerdict(findings), findings);
        }
        private static DemoEvidenceReadinessVerdict ResolveVerdict(IReadOnlyList<OnboardingClosureDiagnosticCode> findings)
        {
            if (findings.Contains(OnboardingClosureDiagnosticCode.Bee431PrematureRelease)) return DemoEvidenceReadinessVerdict.BlockedByBee431Premature;
            if (findings.Contains(OnboardingClosureDiagnosticCode.ProductionClaimDetected)) return DemoEvidenceReadinessVerdict.BlockedByProductionClaim;
            if (findings.Contains(OnboardingClosureDiagnosticCode.ServerDependencyAuditMissing)) return DemoEvidenceReadinessVerdict.BlockedByHiddenServerDependency;
            if (findings.Contains(OnboardingClosureDiagnosticCode.DemoEvidenceReserveHidden)) return DemoEvidenceReadinessVerdict.ReadyWithDemoEvidenceReserve;
            if (findings.Contains(OnboardingClosureDiagnosticCode.OnboardingLotSurfaceMissing)) return DemoEvidenceReadinessVerdict.BlockedByMissingSurface;
            return DemoEvidenceReadinessVerdict.ReadyForArchitectValidation;
        }
    }
    public sealed class OnboardingClosureDiagnostics { public OnboardingClosureDiagnostics(DemoEvidenceReadinessVerdict verdict, IReadOnlyList<OnboardingClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<OnboardingClosureDiagnosticCode>(); } public DemoEvidenceReadinessVerdict Verdict { get; } public IReadOnlyList<OnboardingClosureDiagnosticCode> Findings { get; } public bool Contains(OnboardingClosureDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class OnboardingDemoClosureGateEvaluated { public OnboardingDemoClosureGateEvaluated(string gateId) { GateId = gateId ?? string.Empty; } public string GateId { get; } }
    public sealed class OnboardingLotGapDetected { public OnboardingLotGapDetected(string beeId) { BeeId = beeId ?? string.Empty; } public string BeeId { get; } }
    public sealed class Bee431BlockedByClosureGate { public Bee431BlockedByClosureGate(string message) { Message = message ?? string.Empty; } public string Message { get; } }
}
