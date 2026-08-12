using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum MemorySourceSurface { Hive, Goals, Alliance, ChatPreview, Army, World, Journal, Recap, Choice, SystemPreview }
    public enum MemoryImportanceHint { Low, Normal, Important, PreviewCritical }
    public enum MemoryServerDependency { None, AccountFuture, HistoryFuture, AnalyticsFuture, SocialFuture, WorldFuture, ArmyFuture, PersonalizationFuture }
    public enum PlayerMemoryDiagnosticCode { PlayerMemoryPersistenceForbidden, MemorySourceMissing, MemoryReturnRouteMissing, MemoryOfficialHistoryClaim, MemoryServerDependencyHidden }
    public sealed class MemoryReturnRoute { public MemoryReturnRoute(string routeId, string destination) { RouteId = routeId ?? string.Empty; Destination = destination ?? string.Empty; } public string RouteId { get; } public string Destination { get; } }
    public sealed class MemoryPreviewMoment
    {
        public MemoryPreviewMoment(string momentId, MemorySourceSurface? sourceSurface, string playerMeaning, MemoryImportanceHint importanceHint, MemoryReturnRoute returnRoute, bool officialHistoryBlocked, MemoryServerDependency serverDependency, bool serverDependencyVisible = true, bool persistent = false, bool officialHistoryClaim = false)
        { MomentId = momentId ?? string.Empty; SourceSurface = sourceSurface; PlayerMeaning = playerMeaning ?? string.Empty; ImportanceHint = importanceHint; ReturnRoute = returnRoute; OfficialHistoryBlocked = officialHistoryBlocked; ServerDependency = serverDependency; ServerDependencyVisible = serverDependencyVisible; Persistent = persistent; OfficialHistoryClaim = officialHistoryClaim; }
        public string MomentId { get; } public MemorySourceSurface? SourceSurface { get; } public string PlayerMeaning { get; } public MemoryImportanceHint ImportanceHint { get; } public MemoryReturnRoute ReturnRoute { get; } public bool OfficialHistoryBlocked { get; } public MemoryServerDependency ServerDependency { get; } public bool ServerDependencyVisible { get; } public bool Persistent { get; } public bool OfficialHistoryClaim { get; }
    }
    public sealed class PlayerMemoryPreview
    {
        public PlayerMemoryPreview(string previewId, IReadOnlyList<MemoryPreviewMoment> moments) { PreviewId = ColonyIntegrationIds.Require(previewId); Moments = moments ?? Array.Empty<MemoryPreviewMoment>(); }
        public string PreviewId { get; } public IReadOnlyList<MemoryPreviewMoment> Moments { get; }
        public PlayerMemoryDiagnostics Evaluate()
        {
            var findings = new List<PlayerMemoryDiagnosticCode>();
            if (Moments.Any(m => m.Persistent)) findings.Add(PlayerMemoryDiagnosticCode.PlayerMemoryPersistenceForbidden);
            if (Moments.Count == 0 || Moments.Any(m => m.SourceSurface == null || string.IsNullOrWhiteSpace(m.PlayerMeaning))) findings.Add(PlayerMemoryDiagnosticCode.MemorySourceMissing);
            if (Moments.Any(m => m.ReturnRoute == null || string.IsNullOrWhiteSpace(m.ReturnRoute.RouteId) || string.IsNullOrWhiteSpace(m.ReturnRoute.Destination))) findings.Add(PlayerMemoryDiagnosticCode.MemoryReturnRouteMissing);
            if (Moments.Any(m => m.OfficialHistoryClaim || !m.OfficialHistoryBlocked)) findings.Add(PlayerMemoryDiagnosticCode.MemoryOfficialHistoryClaim);
            if (Moments.Any(m => m.ServerDependency != MemoryServerDependency.None && !m.ServerDependencyVisible)) findings.Add(PlayerMemoryDiagnosticCode.MemoryServerDependencyHidden);
            return new PlayerMemoryDiagnostics(findings);
        }
    }
    public sealed class PlayerMemoryDiagnostics { public PlayerMemoryDiagnostics(IReadOnlyList<PlayerMemoryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlayerMemoryDiagnosticCode>(); } public IReadOnlyList<PlayerMemoryDiagnosticCode> Findings { get; } public bool Contains(PlayerMemoryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class PlayerMemoryPreviewOpened { public PlayerMemoryPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class MemoryMomentInspected { public MemoryMomentInspected(string momentId) { MomentId = momentId ?? string.Empty; } public string MomentId { get; } }
    public sealed class MemoryReturnRouteFollowed { public MemoryReturnRouteFollowed(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }

    public enum HiveMemoryDiagnosticCode { HiveMemoryMutationForbidden, HiveMemoryNeedMissing, HiveMemoryReturnRouteMissing, HiveMemoryCostClaim, HiveMemoryServerDependencyHidden }
    public sealed class HiveNeedMemorySource { public HiveNeedMemorySource(HiveNeedKind? needKind, string hiveSurface) { NeedKind = needKind; HiveSurface = hiveSurface ?? string.Empty; } public HiveNeedKind? NeedKind { get; } public string HiveSurface { get; } }
    public sealed class HivePriorityMemoryHint { public HivePriorityMemoryHint(string text) { Text = text ?? string.Empty; } public string Text { get; } }
    public sealed class HiveMemoryReturnRoute { public HiveMemoryReturnRoute(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class HiveMutationClaimGuard { public HiveMutationClaimGuard(bool mutationClaim, bool costClaim = false) { MutationClaim = mutationClaim; CostClaim = costClaim; } public bool MutationClaim { get; } public bool CostClaim { get; } }
    public sealed class HiveMemoryServerDependency { public HiveMemoryServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class HiveMemoryMoment
    {
        public HiveMemoryMoment(string momentId, HiveNeedMemorySource source, string playerReason, HivePriorityMemoryHint priorityHint, HiveMemoryReturnRoute returnRoute, HiveMutationClaimGuard mutationGuard, HiveMemoryServerDependency serverDependency)
        { MomentId = momentId ?? string.Empty; Source = source; PlayerReason = playerReason ?? string.Empty; PriorityHint = priorityHint; ReturnRoute = returnRoute; MutationGuard = mutationGuard; ServerDependency = serverDependency; }
        public string MomentId { get; } public HiveNeedMemorySource Source { get; } public string PlayerReason { get; } public HivePriorityMemoryHint PriorityHint { get; } public HiveMemoryReturnRoute ReturnRoute { get; } public HiveMutationClaimGuard MutationGuard { get; } public HiveMemoryServerDependency ServerDependency { get; }
        public HiveMemoryDiagnostics Evaluate()
        {
            var findings = new List<HiveMemoryDiagnosticCode>();
            if (MutationGuard != null && MutationGuard.MutationClaim) findings.Add(HiveMemoryDiagnosticCode.HiveMemoryMutationForbidden);
            if (Source == null || Source.NeedKind == null || string.IsNullOrWhiteSpace(Source.HiveSurface) || string.IsNullOrWhiteSpace(PlayerReason)) findings.Add(HiveMemoryDiagnosticCode.HiveMemoryNeedMissing);
            if (ReturnRoute == null || string.IsNullOrWhiteSpace(ReturnRoute.RouteId)) findings.Add(HiveMemoryDiagnosticCode.HiveMemoryReturnRouteMissing);
            if (MutationGuard != null && MutationGuard.CostClaim) findings.Add(HiveMemoryDiagnosticCode.HiveMemoryCostClaim);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(HiveMemoryDiagnosticCode.HiveMemoryServerDependencyHidden);
            return new HiveMemoryDiagnostics(findings);
        }
    }
    public sealed class HiveMemoryDiagnostics { public HiveMemoryDiagnostics(IReadOnlyList<HiveMemoryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveMemoryDiagnosticCode>(); } public IReadOnlyList<HiveMemoryDiagnosticCode> Findings { get; } public bool Contains(HiveMemoryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveMemoryMomentCreated { public HiveMemoryMomentCreated(string momentId) { MomentId = momentId ?? string.Empty; } public string MomentId { get; } }
    public sealed class HiveMemoryMomentInspected { public HiveMemoryMomentInspected(string momentId) { MomentId = momentId ?? string.Empty; } public string MomentId { get; } }
    public sealed class HiveMemoryReturnFollowed { public HiveMemoryReturnFollowed(string momentId) { MomentId = momentId ?? string.Empty; } public string MomentId { get; } }

    public enum AllianceMemoryDiagnosticCode { AllianceMemoryPersistenceForbidden, AllianceMemoryPersonalDataRisk, AllianceMessageSentClaim, AllianceMemoryRouteMissing, AllianceMemoryServerDependencyHidden }
    public sealed class AllyMemoryReference { public AllyMemoryReference(string label, bool exampleOnly, bool personalData = false) { Label = label ?? string.Empty; ExampleOnly = exampleOnly; PersonalData = personalData; } public string Label { get; } public bool ExampleOnly { get; } public bool PersonalData { get; } }
    public sealed class SharedMemoryPrivacyGuard { public SharedMemoryPrivacyGuard(bool visible, bool risk = false) { Visible = visible; Risk = risk; } public bool Visible { get; } public bool Risk { get; } }
    public sealed class AllianceMemoryReturnRoute { public AllianceMemoryReturnRoute(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class AllianceMemoryServerDependency { public AllianceMemoryServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class AllianceMemoryMoment
    {
        public AllianceMemoryMoment(string momentId, string socialKind, AllyMemoryReference allyReferencePreview, string playerMeaning, SharedMemoryPrivacyGuard privacyNotice, AllianceMemoryReturnRoute returnRoute, AllianceMemoryServerDependency serverDependency, bool persistenceClaim = false, bool messageSentClaim = false)
        { MomentId = momentId ?? string.Empty; SocialKind = socialKind ?? string.Empty; AllyReferencePreview = allyReferencePreview; PlayerMeaning = playerMeaning ?? string.Empty; PrivacyNotice = privacyNotice; ReturnRoute = returnRoute; ServerDependency = serverDependency; PersistenceClaim = persistenceClaim; MessageSentClaim = messageSentClaim; }
        public string MomentId { get; } public string SocialKind { get; } public AllyMemoryReference AllyReferencePreview { get; } public string PlayerMeaning { get; } public SharedMemoryPrivacyGuard PrivacyNotice { get; } public AllianceMemoryReturnRoute ReturnRoute { get; } public AllianceMemoryServerDependency ServerDependency { get; } public bool PersistenceClaim { get; } public bool MessageSentClaim { get; }
    }
    public sealed class AllianceSharedMemoryPreview
    {
        public AllianceSharedMemoryPreview(string previewId, IReadOnlyList<AllianceMemoryMoment> moments) { PreviewId = ColonyIntegrationIds.Require(previewId); Moments = moments ?? Array.Empty<AllianceMemoryMoment>(); }
        public string PreviewId { get; } public IReadOnlyList<AllianceMemoryMoment> Moments { get; }
        public AllianceMemoryDiagnostics Evaluate()
        {
            var findings = new List<AllianceMemoryDiagnosticCode>();
            if (Moments.Any(m => m.PersistenceClaim)) findings.Add(AllianceMemoryDiagnosticCode.AllianceMemoryPersistenceForbidden);
            if (Moments.Any(m => m.AllyReferencePreview == null || !m.AllyReferencePreview.ExampleOnly || m.AllyReferencePreview.PersonalData || m.PrivacyNotice == null || !m.PrivacyNotice.Visible || m.PrivacyNotice.Risk)) findings.Add(AllianceMemoryDiagnosticCode.AllianceMemoryPersonalDataRisk);
            if (Moments.Any(m => m.MessageSentClaim)) findings.Add(AllianceMemoryDiagnosticCode.AllianceMessageSentClaim);
            if (Moments.Count == 0 || Moments.Any(m => m.ReturnRoute == null || string.IsNullOrWhiteSpace(m.ReturnRoute.RouteId))) findings.Add(AllianceMemoryDiagnosticCode.AllianceMemoryRouteMissing);
            if (Moments.Any(m => m.ServerDependency == null || !m.ServerDependency.Visible)) findings.Add(AllianceMemoryDiagnosticCode.AllianceMemoryServerDependencyHidden);
            return new AllianceMemoryDiagnostics(findings);
        }
    }
    public sealed class AllianceMemoryDiagnostics { public AllianceMemoryDiagnostics(IReadOnlyList<AllianceMemoryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceMemoryDiagnosticCode>(); } public IReadOnlyList<AllianceMemoryDiagnosticCode> Findings { get; } public bool Contains(AllianceMemoryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class AllianceSharedMemoryOpened { public AllianceSharedMemoryOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class AllianceMemoryMomentInspected { public AllianceMemoryMomentInspected(string momentId) { MomentId = momentId ?? string.Empty; } public string MomentId { get; } }
    public sealed class AllianceMemoryReturnFollowed { public AllianceMemoryReturnFollowed(string momentId) { MomentId = momentId ?? string.Empty; } public string MomentId { get; } }

    public enum WorldMemoryMarkerKind { Threat, Opportunity, Route, Resource, Ally, FutureEvent, DefenseAdvice }
    public enum WorldMemoryDiagnosticCode { WorldMemoryLiveClaim, WorldMemoryRewardForbidden, WorldMemoryRouteMissing, WorldMemoryActionForbidden, WorldMemoryServerDependencyHidden }
    public sealed class WorldMemoryFreshnessPreview { public WorldMemoryFreshnessPreview(string text, bool liveClaim = false) { Text = text ?? string.Empty; LiveClaim = liveClaim; } public string Text { get; } public bool LiveClaim { get; } }
    public sealed class WorldMemoryRoute { public WorldMemoryRoute(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class WorldLiveClaimGuard { public WorldLiveClaimGuard(bool liveClaim, bool rewardClaim = false, bool actionClaim = false) { LiveClaim = liveClaim; RewardClaim = rewardClaim; ActionClaim = actionClaim; } public bool LiveClaim { get; } public bool RewardClaim { get; } public bool ActionClaim { get; } }
    public sealed class WorldMemoryServerDependency { public WorldMemoryServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class WorldEventMemoryMarker
    {
        public WorldEventMemoryMarker(string markerId, WorldMemoryMarkerKind markerKind, string playerMeaning, WorldMemoryFreshnessPreview freshnessPreview, WorldMemoryRoute returnRoute, WorldLiveClaimGuard blockedAction, WorldMemoryServerDependency serverDependency)
        { MarkerId = markerId ?? string.Empty; MarkerKind = markerKind; PlayerMeaning = playerMeaning ?? string.Empty; FreshnessPreview = freshnessPreview; ReturnRoute = returnRoute; BlockedAction = blockedAction; ServerDependency = serverDependency; }
        public string MarkerId { get; } public WorldMemoryMarkerKind MarkerKind { get; } public string PlayerMeaning { get; } public WorldMemoryFreshnessPreview FreshnessPreview { get; } public WorldMemoryRoute ReturnRoute { get; } public WorldLiveClaimGuard BlockedAction { get; } public WorldMemoryServerDependency ServerDependency { get; }
        public WorldMemoryDiagnostics Evaluate()
        {
            var findings = new List<WorldMemoryDiagnosticCode>();
            if ((FreshnessPreview != null && FreshnessPreview.LiveClaim) || (BlockedAction != null && BlockedAction.LiveClaim)) findings.Add(WorldMemoryDiagnosticCode.WorldMemoryLiveClaim);
            if (BlockedAction != null && BlockedAction.RewardClaim) findings.Add(WorldMemoryDiagnosticCode.WorldMemoryRewardForbidden);
            if (ReturnRoute == null || string.IsNullOrWhiteSpace(ReturnRoute.RouteId) || string.IsNullOrWhiteSpace(PlayerMeaning)) findings.Add(WorldMemoryDiagnosticCode.WorldMemoryRouteMissing);
            if (BlockedAction != null && BlockedAction.ActionClaim) findings.Add(WorldMemoryDiagnosticCode.WorldMemoryActionForbidden);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(WorldMemoryDiagnosticCode.WorldMemoryServerDependencyHidden);
            return new WorldMemoryDiagnostics(findings);
        }
    }
    public sealed class WorldMemoryDiagnostics { public WorldMemoryDiagnostics(IReadOnlyList<WorldMemoryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<WorldMemoryDiagnosticCode>(); } public IReadOnlyList<WorldMemoryDiagnosticCode> Findings { get; } public bool Contains(WorldMemoryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class WorldMemoryMarkerCreated { public WorldMemoryMarkerCreated(string markerId) { MarkerId = markerId ?? string.Empty; } public string MarkerId { get; } }
    public sealed class WorldMemoryMarkerInspected { public WorldMemoryMarkerInspected(string markerId) { MarkerId = markerId ?? string.Empty; } public string MarkerId { get; } }
    public sealed class WorldMemoryRouteFollowed { public WorldMemoryRouteFollowed(string markerId) { MarkerId = markerId ?? string.Empty; } public string MarkerId { get; } }

    public enum ArmyMemoryDiagnosticCode { ArmyMemoryTrainingClaim, ArmyMemoryCombatClaim, ArmyMemoryLossRewardForbidden, ArmyMemoryRouteMissing, ArmyMemoryServerDependencyHidden }
    public sealed class UnitFamilyMemoryReference { public UnitFamilyMemoryReference(string family, bool aggressiveLanguage = false) { Family = family ?? string.Empty; AggressiveLanguage = aggressiveLanguage; } public string Family { get; } public bool AggressiveLanguage { get; } }
    public sealed class DefenseMemoryHint { public DefenseMemoryHint(string text, bool officialClaim = false) { Text = text ?? string.Empty; OfficialClaim = officialClaim; } public string Text { get; } public bool OfficialClaim { get; } }
    public sealed class PvpRiskMemoryNotice { public PvpRiskMemoryNotice(string text, bool visible) { Text = text ?? string.Empty; Visible = visible; } public string Text { get; } public bool Visible { get; } }
    public sealed class ArmyMemoryActionGuard { public ArmyMemoryActionGuard(bool trainingClaim, bool combatClaim = false, bool lossClaim = false, bool rewardClaim = false) { TrainingClaim = trainingClaim; CombatClaim = combatClaim; LossClaim = lossClaim; RewardClaim = rewardClaim; } public bool TrainingClaim { get; } public bool CombatClaim { get; } public bool LossClaim { get; } public bool RewardClaim { get; } }
    public sealed class ArmyMemoryServerDependency { public ArmyMemoryServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class ArmyReadinessMemoryRecord
    {
        public ArmyReadinessMemoryRecord(string recordId, UnitFamilyMemoryReference unitFamily, DefenseMemoryHint readinessHint, string relatedWorldMarker, PvpRiskMemoryNotice pvpRiskNotice, string returnRoute, ArmyMemoryActionGuard actionGuard, ArmyMemoryServerDependency serverDependency)
        { RecordId = recordId ?? string.Empty; UnitFamily = unitFamily; ReadinessHint = readinessHint; RelatedWorldMarker = relatedWorldMarker ?? string.Empty; PvpRiskNotice = pvpRiskNotice; ReturnRoute = returnRoute ?? string.Empty; ActionGuard = actionGuard; ServerDependency = serverDependency; }
        public string RecordId { get; } public UnitFamilyMemoryReference UnitFamily { get; } public DefenseMemoryHint ReadinessHint { get; } public string RelatedWorldMarker { get; } public PvpRiskMemoryNotice PvpRiskNotice { get; } public string ReturnRoute { get; } public ArmyMemoryActionGuard ActionGuard { get; } public ArmyMemoryServerDependency ServerDependency { get; }
        public ArmyMemoryDiagnostics Evaluate()
        {
            var findings = new List<ArmyMemoryDiagnosticCode>();
            if (ActionGuard != null && ActionGuard.TrainingClaim) findings.Add(ArmyMemoryDiagnosticCode.ArmyMemoryTrainingClaim);
            if ((ActionGuard != null && ActionGuard.CombatClaim) || (ReadinessHint != null && ReadinessHint.OfficialClaim)) findings.Add(ArmyMemoryDiagnosticCode.ArmyMemoryCombatClaim);
            if (ActionGuard != null && (ActionGuard.LossClaim || ActionGuard.RewardClaim)) findings.Add(ArmyMemoryDiagnosticCode.ArmyMemoryLossRewardForbidden);
            if (string.IsNullOrWhiteSpace(ReturnRoute) || UnitFamily == null || string.IsNullOrWhiteSpace(UnitFamily.Family)) findings.Add(ArmyMemoryDiagnosticCode.ArmyMemoryRouteMissing);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(ArmyMemoryDiagnosticCode.ArmyMemoryServerDependencyHidden);
            return new ArmyMemoryDiagnostics(findings);
        }
    }
    public sealed class ArmyMemoryDiagnostics { public ArmyMemoryDiagnostics(IReadOnlyList<ArmyMemoryDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyMemoryDiagnosticCode>(); } public IReadOnlyList<ArmyMemoryDiagnosticCode> Findings { get; } public bool Contains(ArmyMemoryDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class ArmyMemoryRecordCreated { public ArmyMemoryRecordCreated(string recordId) { RecordId = recordId ?? string.Empty; } public string RecordId { get; } }
    public sealed class ArmyMemoryRecordInspected { public ArmyMemoryRecordInspected(string recordId) { RecordId = recordId ?? string.Empty; } public string RecordId { get; } }
    public sealed class ArmyMemoryReturnFollowed { public ArmyMemoryReturnFollowed(string recordId) { RecordId = recordId ?? string.Empty; } public string RecordId { get; } }

    public enum ChoiceDiagnosticCode { ChoiceOfficialClaimForbidden, ReflectionBonusClaim, ReflectionMatchmakingClaim, ReflectionReturnRouteMissing, ReflectionServerDependencyHidden }
    public sealed class PlaystyleReflectionHint { public PlaystyleReflectionHint(PlayerStyleSignalKind style, string text) { Style = style; Text = text ?? string.Empty; } public PlayerStyleSignalKind Style { get; } public string Text { get; } }
    public sealed class ChoiceOfficialClaimGuard { public ChoiceOfficialClaimGuard(bool officialChoice, bool bonusClaim = false, bool matchmakingClaim = false) { OfficialChoice = officialChoice; BonusClaim = bonusClaim; MatchmakingClaim = matchmakingClaim; } public bool OfficialChoice { get; } public bool BonusClaim { get; } public bool MatchmakingClaim { get; } }
    public sealed class ReflectionReturnRoute { public ReflectionReturnRoute(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class ReflectionServerDependency { public ReflectionServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class ChoiceReflectionSignal
    {
        public ChoiceReflectionSignal(string signalId, string sourceChoice, string reflectionText, PlaystyleReflectionHint playstyleHint, ReflectionReturnRoute returnRoute, ChoiceOfficialClaimGuard officialChoiceBlocked, ReflectionServerDependency serverDependency)
        { SignalId = signalId ?? string.Empty; SourceChoice = sourceChoice ?? string.Empty; ReflectionText = reflectionText ?? string.Empty; PlaystyleHint = playstyleHint; ReturnRoute = returnRoute; OfficialChoiceBlocked = officialChoiceBlocked; ServerDependency = serverDependency; }
        public string SignalId { get; } public string SourceChoice { get; } public string ReflectionText { get; } public PlaystyleReflectionHint PlaystyleHint { get; } public ReflectionReturnRoute ReturnRoute { get; } public ChoiceOfficialClaimGuard OfficialChoiceBlocked { get; } public ReflectionServerDependency ServerDependency { get; }
    }
    public sealed class PlayerChoiceReflection
    {
        public PlayerChoiceReflection(string reflectionId, IReadOnlyList<ChoiceReflectionSignal> signals) { ReflectionId = ColonyIntegrationIds.Require(reflectionId); Signals = signals ?? Array.Empty<ChoiceReflectionSignal>(); }
        public string ReflectionId { get; } public IReadOnlyList<ChoiceReflectionSignal> Signals { get; }
        public ChoiceDiagnostics Evaluate()
        {
            var findings = new List<ChoiceDiagnosticCode>();
            if (Signals.Any(s => s.OfficialChoiceBlocked != null && s.OfficialChoiceBlocked.OfficialChoice)) findings.Add(ChoiceDiagnosticCode.ChoiceOfficialClaimForbidden);
            if (Signals.Any(s => s.OfficialChoiceBlocked != null && s.OfficialChoiceBlocked.BonusClaim)) findings.Add(ChoiceDiagnosticCode.ReflectionBonusClaim);
            if (Signals.Any(s => s.OfficialChoiceBlocked != null && s.OfficialChoiceBlocked.MatchmakingClaim)) findings.Add(ChoiceDiagnosticCode.ReflectionMatchmakingClaim);
            if (Signals.Count == 0 || Signals.Any(s => s.ReturnRoute == null || string.IsNullOrWhiteSpace(s.ReturnRoute.RouteId) || string.IsNullOrWhiteSpace(s.ReflectionText))) findings.Add(ChoiceDiagnosticCode.ReflectionReturnRouteMissing);
            if (Signals.Any(s => s.ServerDependency == null || !s.ServerDependency.Visible)) findings.Add(ChoiceDiagnosticCode.ReflectionServerDependencyHidden);
            return new ChoiceDiagnostics(findings);
        }
    }
    public sealed class ChoiceDiagnostics { public ChoiceDiagnostics(IReadOnlyList<ChoiceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ChoiceDiagnosticCode>(); } public IReadOnlyList<ChoiceDiagnosticCode> Findings { get; } public bool Contains(ChoiceDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class PlayerChoiceReflectionShown { public PlayerChoiceReflectionShown(string reflectionId) { ReflectionId = reflectionId ?? string.Empty; } public string ReflectionId { get; } }
    public sealed class ChoiceReflectionInspected { public ChoiceReflectionInspected(string signalId) { SignalId = signalId ?? string.Empty; } public string SignalId { get; } }
    public sealed class ChoiceReflectionDismissed { public ChoiceReflectionDismissed(string reflectionId) { ReflectionId = reflectionId ?? string.Empty; } public string ReflectionId { get; } }

    public enum MemoryFilterCategory { Hive, Alliance, World, Army, Choice, SystemPreview, FavoritePreview, PrivacyMasked }
    public enum MemoryJournalDiagnosticCode { MemoryFilterCategoryMissing, MemoryPersonalDataLeak, MemoryExportForbidden, MemorySearchServerClaim, MemoryJournalServerDependencyHidden }
    public sealed class PrivacySafeMemoryView { public PrivacySafeMemoryView(bool maskVisible, bool personalDataLeak = false) { MaskVisible = maskVisible; PersonalDataLeak = personalDataLeak; } public bool MaskVisible { get; } public bool PersonalDataLeak { get; } }
    public sealed class MemorySearchPreviewBlocker { public MemorySearchPreviewBlocker(bool serverSearchClaim) { ServerSearchClaim = serverSearchClaim; } public bool ServerSearchClaim { get; } }
    public sealed class MemoryExportClaimGuard { public MemoryExportClaimGuard(bool exportBlocked, bool exportOfficialClaim = false) { ExportBlocked = exportBlocked; ExportOfficialClaim = exportOfficialClaim; } public bool ExportBlocked { get; } public bool ExportOfficialClaim { get; } }
    public sealed class MemoryJournalServerDependency { public MemoryJournalServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class MemoryJournalFilter
    {
        public MemoryJournalFilter(string filterId, MemoryFilterCategory? category, int visibleCountPreview, PrivacySafeMemoryView privacyMaskStatus, string localOnlyNotice, MemoryExportClaimGuard exportBlocked, MemorySearchPreviewBlocker searchBlocker, MemoryJournalServerDependency serverDependency)
        { FilterId = filterId ?? string.Empty; Category = category; VisibleCountPreview = visibleCountPreview; PrivacyMaskStatus = privacyMaskStatus; LocalOnlyNotice = localOnlyNotice ?? string.Empty; ExportBlocked = exportBlocked; SearchBlocker = searchBlocker; ServerDependency = serverDependency; }
        public string FilterId { get; } public MemoryFilterCategory? Category { get; } public int VisibleCountPreview { get; } public PrivacySafeMemoryView PrivacyMaskStatus { get; } public string LocalOnlyNotice { get; } public MemoryExportClaimGuard ExportBlocked { get; } public MemorySearchPreviewBlocker SearchBlocker { get; } public MemoryJournalServerDependency ServerDependency { get; }
        public MemoryJournalDiagnostics Evaluate()
        {
            var findings = new List<MemoryJournalDiagnosticCode>();
            if (Category == null || string.IsNullOrWhiteSpace(FilterId)) findings.Add(MemoryJournalDiagnosticCode.MemoryFilterCategoryMissing);
            if (PrivacyMaskStatus == null || !PrivacyMaskStatus.MaskVisible || PrivacyMaskStatus.PersonalDataLeak) findings.Add(MemoryJournalDiagnosticCode.MemoryPersonalDataLeak);
            if (ExportBlocked == null || !ExportBlocked.ExportBlocked || ExportBlocked.ExportOfficialClaim) findings.Add(MemoryJournalDiagnosticCode.MemoryExportForbidden);
            if (SearchBlocker != null && SearchBlocker.ServerSearchClaim) findings.Add(MemoryJournalDiagnosticCode.MemorySearchServerClaim);
            if (ServerDependency == null || !ServerDependency.Visible) findings.Add(MemoryJournalDiagnosticCode.MemoryJournalServerDependencyHidden);
            return new MemoryJournalDiagnostics(findings);
        }
    }
    public sealed class MemoryJournalDiagnostics { public MemoryJournalDiagnostics(IReadOnlyList<MemoryJournalDiagnosticCode> findings) { Findings = findings ?? Array.Empty<MemoryJournalDiagnosticCode>(); } public IReadOnlyList<MemoryJournalDiagnosticCode> Findings { get; } public bool Contains(MemoryJournalDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class MemoryJournalFilterApplied { public MemoryJournalFilterApplied(string filterId) { FilterId = filterId ?? string.Empty; } public string FilterId { get; } }
    public sealed class PrivacySafeMemoryViewShown { public PrivacySafeMemoryViewShown(string filterId) { FilterId = filterId ?? string.Empty; } public string FilterId { get; } }
    public sealed class MemoryExportBlocked { public MemoryExportBlocked(string filterId) { FilterId = filterId ?? string.Empty; } public string FilterId { get; } }

    public enum MemoryGoalDiagnosticCode { MemoryGoalSourceMissing, MemoryGoalRewardForbidden, MemoryGoalCompletionClaim, MemoryGoalRouteMissing, MemoryGoalServerDependencyHidden }
    public sealed class MemoryGoalSource { public MemoryGoalSource(string memoryId) { MemoryId = memoryId ?? string.Empty; } public string MemoryId { get; } }
    public sealed class GoalRewardClaimGuard { public GoalRewardClaimGuard(bool rewardClaim, bool completionClaim = false) { RewardClaim = rewardClaim; CompletionClaim = completionClaim; } public bool RewardClaim { get; } public bool CompletionClaim { get; } }
    public sealed class MemoryGoalReturnRoute { public MemoryGoalReturnRoute(string routeId) { RouteId = routeId ?? string.Empty; } public string RouteId { get; } }
    public sealed class MemoryGoalServerDependency { public MemoryGoalServerDependency(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class MemoryDerivedGoalPreview
    {
        public MemoryDerivedGoalPreview(string goalId, MemoryGoalSource sourceMemory, string goalText, string linkedSurface, MemoryGoalReturnRoute returnRoute, GoalRewardClaimGuard rewardBlocked, MemoryGoalServerDependency serverDependency)
        { GoalId = goalId ?? string.Empty; SourceMemory = sourceMemory; GoalText = goalText ?? string.Empty; LinkedSurface = linkedSurface ?? string.Empty; ReturnRoute = returnRoute; RewardBlocked = rewardBlocked; ServerDependency = serverDependency; }
        public string GoalId { get; } public MemoryGoalSource SourceMemory { get; } public string GoalText { get; } public string LinkedSurface { get; } public MemoryGoalReturnRoute ReturnRoute { get; } public GoalRewardClaimGuard RewardBlocked { get; } public MemoryGoalServerDependency ServerDependency { get; }
    }
    public sealed class MemoryGoalReturnBridge
    {
        public MemoryGoalReturnBridge(string bridgeId, IReadOnlyList<MemoryDerivedGoalPreview> goals) { BridgeId = ColonyIntegrationIds.Require(bridgeId); Goals = goals ?? Array.Empty<MemoryDerivedGoalPreview>(); }
        public string BridgeId { get; } public IReadOnlyList<MemoryDerivedGoalPreview> Goals { get; }
        public MemoryGoalDiagnostics Evaluate()
        {
            var findings = new List<MemoryGoalDiagnosticCode>();
            if (Goals.Count == 0 || Goals.Any(g => g.SourceMemory == null || string.IsNullOrWhiteSpace(g.SourceMemory.MemoryId))) findings.Add(MemoryGoalDiagnosticCode.MemoryGoalSourceMissing);
            if (Goals.Any(g => g.RewardBlocked != null && g.RewardBlocked.RewardClaim)) findings.Add(MemoryGoalDiagnosticCode.MemoryGoalRewardForbidden);
            if (Goals.Any(g => g.RewardBlocked != null && g.RewardBlocked.CompletionClaim)) findings.Add(MemoryGoalDiagnosticCode.MemoryGoalCompletionClaim);
            if (Goals.Any(g => g.ReturnRoute == null || string.IsNullOrWhiteSpace(g.ReturnRoute.RouteId) || string.IsNullOrWhiteSpace(g.LinkedSurface))) findings.Add(MemoryGoalDiagnosticCode.MemoryGoalRouteMissing);
            if (Goals.Any(g => g.ServerDependency == null || !g.ServerDependency.Visible)) findings.Add(MemoryGoalDiagnosticCode.MemoryGoalServerDependencyHidden);
            return new MemoryGoalDiagnostics(findings);
        }
    }
    public sealed class MemoryGoalDiagnostics { public MemoryGoalDiagnostics(IReadOnlyList<MemoryGoalDiagnosticCode> findings) { Findings = findings ?? Array.Empty<MemoryGoalDiagnosticCode>(); } public IReadOnlyList<MemoryGoalDiagnosticCode> Findings { get; } public bool Contains(MemoryGoalDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class MemoryGoalBridgeShown { public MemoryGoalBridgeShown(string bridgeId) { BridgeId = bridgeId ?? string.Empty; } public string BridgeId { get; } }
    public sealed class MemoryDerivedGoalInspected { public MemoryDerivedGoalInspected(string goalId) { GoalId = goalId ?? string.Empty; } public string GoalId { get; } }
    public sealed class MemoryGoalReturnFollowed { public MemoryGoalReturnFollowed(string goalId) { GoalId = goalId ?? string.Empty; } public string GoalId { get; } }

    public enum MemoryReadabilityVerdict { Readable, ReadableWithReserve, BlockedByText, BlockedByMissingSource, BlockedByMissingRoute, BlockedByMissingEvidence, BlockedByProductionClaim }
    public enum MemoryReadabilityDiagnosticCode { MemoryCardTextTooLong, MemorySourceNotVisible, MemoryReturnRouteNotVisible, MemoryDemoEvidenceMissing, MemoryProductionReadinessClaim }
    public sealed class MemoryTextLengthRule { public MemoryTextLengthRule(int maxCharacters) { MaxCharacters = maxCharacters; } public int MaxCharacters { get; } }
    public sealed class MemoryIconClarityNeed { public MemoryIconClarityNeed(bool clear) { Clear = clear; } public bool Clear { get; } }
    public sealed class MemoryDemoEvidenceFrame { public MemoryDemoEvidenceFrame(bool nonBlank, bool productionClaim = false) { NonBlank = nonBlank; ProductionClaim = productionClaim; } public bool NonBlank { get; } public bool ProductionClaim { get; } }
    public sealed class MemoryCardEvidenceNeed
    {
        public MemoryCardEvidenceNeed(string cardId, string text, bool sourceVisible, bool routeVisible, bool privacyVisible, bool previewVisible, MemoryDemoEvidenceFrame demoFrameNeed, MemoryReadabilityVerdict verdict)
        { CardId = cardId ?? string.Empty; Text = text ?? string.Empty; SourceVisible = sourceVisible; RouteVisible = routeVisible; PrivacyVisible = privacyVisible; PreviewVisible = previewVisible; DemoFrameNeed = demoFrameNeed; Verdict = verdict; }
        public string CardId { get; } public string Text { get; } public bool SourceVisible { get; } public bool RouteVisible { get; } public bool PrivacyVisible { get; } public bool PreviewVisible { get; } public MemoryDemoEvidenceFrame DemoFrameNeed { get; } public MemoryReadabilityVerdict Verdict { get; }
    }
    public sealed class MobileMemoryReadabilityCheck
    {
        public MobileMemoryReadabilityCheck(string checkId, MemoryTextLengthRule textRule, MemoryIconClarityNeed iconNeed, IReadOnlyList<MemoryCardEvidenceNeed> cards) { CheckId = ColonyIntegrationIds.Require(checkId); TextRule = textRule; IconNeed = iconNeed; Cards = cards ?? Array.Empty<MemoryCardEvidenceNeed>(); }
        public string CheckId { get; } public MemoryTextLengthRule TextRule { get; } public MemoryIconClarityNeed IconNeed { get; } public IReadOnlyList<MemoryCardEvidenceNeed> Cards { get; }
        public MemoryReadabilityDiagnostics Evaluate()
        {
            var findings = new List<MemoryReadabilityDiagnosticCode>();
            if (Cards.Any(c => TextRule == null || c.Text.Length > TextRule.MaxCharacters)) findings.Add(MemoryReadabilityDiagnosticCode.MemoryCardTextTooLong);
            if (Cards.Any(c => !c.SourceVisible)) findings.Add(MemoryReadabilityDiagnosticCode.MemorySourceNotVisible);
            if (Cards.Any(c => !c.RouteVisible)) findings.Add(MemoryReadabilityDiagnosticCode.MemoryReturnRouteNotVisible);
            if (Cards.Count == 0 || Cards.Any(c => c.DemoFrameNeed == null || !c.DemoFrameNeed.NonBlank)) findings.Add(MemoryReadabilityDiagnosticCode.MemoryDemoEvidenceMissing);
            if (Cards.Any(c => c.DemoFrameNeed != null && c.DemoFrameNeed.ProductionClaim || c.Verdict == MemoryReadabilityVerdict.BlockedByProductionClaim)) findings.Add(MemoryReadabilityDiagnosticCode.MemoryProductionReadinessClaim);
            return new MemoryReadabilityDiagnostics(ResolveVerdict(findings), findings);
        }
        private static MemoryReadabilityVerdict ResolveVerdict(IReadOnlyList<MemoryReadabilityDiagnosticCode> findings)
        {
            if (findings.Contains(MemoryReadabilityDiagnosticCode.MemoryProductionReadinessClaim)) return MemoryReadabilityVerdict.BlockedByProductionClaim;
            if (findings.Contains(MemoryReadabilityDiagnosticCode.MemoryDemoEvidenceMissing)) return MemoryReadabilityVerdict.BlockedByMissingEvidence;
            if (findings.Contains(MemoryReadabilityDiagnosticCode.MemoryReturnRouteNotVisible)) return MemoryReadabilityVerdict.BlockedByMissingRoute;
            if (findings.Contains(MemoryReadabilityDiagnosticCode.MemorySourceNotVisible)) return MemoryReadabilityVerdict.BlockedByMissingSource;
            if (findings.Contains(MemoryReadabilityDiagnosticCode.MemoryCardTextTooLong)) return MemoryReadabilityVerdict.BlockedByText;
            return MemoryReadabilityVerdict.Readable;
        }
    }
    public sealed class MemoryReadabilityDiagnostics { public MemoryReadabilityDiagnostics(MemoryReadabilityVerdict verdict, IReadOnlyList<MemoryReadabilityDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<MemoryReadabilityDiagnosticCode>(); } public MemoryReadabilityVerdict Verdict { get; } public IReadOnlyList<MemoryReadabilityDiagnosticCode> Findings { get; } public bool Contains(MemoryReadabilityDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class MemoryReadabilityChecked { public MemoryReadabilityChecked(string checkId) { CheckId = checkId ?? string.Empty; } public string CheckId { get; } }
    public sealed class MemoryEvidenceFrameRequested { public MemoryEvidenceFrameRequested(string cardId) { CardId = cardId ?? string.Empty; } public string CardId { get; } }
    public sealed class MemoryReadabilityIssueDetected { public MemoryReadabilityIssueDetected(string cardId) { CardId = cardId ?? string.Empty; } public string CardId { get; } }

    public enum MemoryDemoEvidenceVerdict { ReadyForArchitectValidation, ReadyWithDemoEvidenceReserve, NeedsPlannerRevision, BlockedByMissingMemorySource, BlockedByOfficialHistoryClaim, BlockedByPrivacyGap, BlockedByRewardOrCompletionClaim, BlockedByBee451Premature }
    public enum MemoryClosureDiagnosticCode { MemoryNetworkSourceGap, MemoryOfficialHistoryClaim, MemoryPrivacyGap, MemoryRewardClaimDetected, Bee451PrematureRelease }
    public sealed class MemoryOfficialClaimAudit { public MemoryOfficialClaimAudit(bool officialHistoryClaim, bool rewardOrCompletionClaim = false) { OfficialHistoryClaim = officialHistoryClaim; RewardOrCompletionClaim = rewardOrCompletionClaim; } public bool OfficialHistoryClaim { get; } public bool RewardOrCompletionClaim { get; } }
    public sealed class MemoryServerBoundaryAudit { public MemoryServerBoundaryAudit(bool visible) { Visible = visible; } public bool Visible { get; } }
    public sealed class Bee451BlockerStatus { public Bee451BlockerStatus(bool prematureAttempt, string message) { PrematureAttempt = prematureAttempt; Message = message ?? string.Empty; } public bool PrematureAttempt { get; } public string Message { get; } }
    public sealed class MemoryNetworkCoverageMatrix
    {
        public MemoryNetworkCoverageMatrix(string beeId, string memorySource, string returnRoute, string privacyStatus, string demoEvidence, string qaCheck, string serverBoundary, MemoryDemoEvidenceVerdict verdict)
        { BeeId = beeId ?? string.Empty; MemorySource = memorySource ?? string.Empty; ReturnRoute = returnRoute ?? string.Empty; PrivacyStatus = privacyStatus ?? string.Empty; DemoEvidence = demoEvidence ?? string.Empty; QaCheck = qaCheck ?? string.Empty; ServerBoundary = serverBoundary ?? string.Empty; Verdict = verdict; }
        public string BeeId { get; } public string MemorySource { get; } public string ReturnRoute { get; } public string PrivacyStatus { get; } public string DemoEvidence { get; } public string QaCheck { get; } public string ServerBoundary { get; } public MemoryDemoEvidenceVerdict Verdict { get; }
    }
    public sealed class PlayerMemoryNetworkClosureGate
    {
        public const string Bee451BlockedMessage = "BEE-451 bloquee jusqu'a validation architecte.";
        public PlayerMemoryNetworkClosureGate(string gateId, IReadOnlyList<MemoryNetworkCoverageMatrix> coverage, MemoryOfficialClaimAudit officialClaimAudit, MemoryServerBoundaryAudit serverBoundaryAudit, Bee451BlockerStatus bee451BlockerStatus)
        { GateId = ColonyIntegrationIds.Require(gateId); Coverage = coverage ?? Array.Empty<MemoryNetworkCoverageMatrix>(); OfficialClaimAudit = officialClaimAudit ?? new MemoryOfficialClaimAudit(false); ServerBoundaryAudit = serverBoundaryAudit; Bee451BlockerStatus = bee451BlockerStatus ?? new Bee451BlockerStatus(false, Bee451BlockedMessage); }
        public string GateId { get; } public IReadOnlyList<MemoryNetworkCoverageMatrix> Coverage { get; } public MemoryOfficialClaimAudit OfficialClaimAudit { get; } public MemoryServerBoundaryAudit ServerBoundaryAudit { get; } public Bee451BlockerStatus Bee451BlockerStatus { get; }
        public MemoryClosureDiagnostics Evaluate()
        {
            var findings = new List<MemoryClosureDiagnosticCode>();
            if (Coverage.Count < 9 || Coverage.Any(c => string.IsNullOrWhiteSpace(c.BeeId) || string.IsNullOrWhiteSpace(c.MemorySource) || string.IsNullOrWhiteSpace(c.ReturnRoute))) findings.Add(MemoryClosureDiagnosticCode.MemoryNetworkSourceGap);
            if (OfficialClaimAudit.OfficialHistoryClaim || Coverage.Any(c => c.Verdict == MemoryDemoEvidenceVerdict.BlockedByOfficialHistoryClaim)) findings.Add(MemoryClosureDiagnosticCode.MemoryOfficialHistoryClaim);
            if (Coverage.Any(c => string.IsNullOrWhiteSpace(c.PrivacyStatus) || c.Verdict == MemoryDemoEvidenceVerdict.BlockedByPrivacyGap)) findings.Add(MemoryClosureDiagnosticCode.MemoryPrivacyGap);
            if (OfficialClaimAudit.RewardOrCompletionClaim || Coverage.Any(c => c.Verdict == MemoryDemoEvidenceVerdict.BlockedByRewardOrCompletionClaim)) findings.Add(MemoryClosureDiagnosticCode.MemoryRewardClaimDetected);
            if (ServerBoundaryAudit == null || !ServerBoundaryAudit.Visible || Coverage.Any(c => string.IsNullOrWhiteSpace(c.ServerBoundary))) findings.Add(MemoryClosureDiagnosticCode.MemoryOfficialHistoryClaim);
            if (Bee451BlockerStatus.PrematureAttempt) findings.Add(MemoryClosureDiagnosticCode.Bee451PrematureRelease);
            return new MemoryClosureDiagnostics(ResolveVerdict(findings), findings);
        }
        private static MemoryDemoEvidenceVerdict ResolveVerdict(IReadOnlyList<MemoryClosureDiagnosticCode> findings)
        {
            if (findings.Contains(MemoryClosureDiagnosticCode.Bee451PrematureRelease)) return MemoryDemoEvidenceVerdict.BlockedByBee451Premature;
            if (findings.Contains(MemoryClosureDiagnosticCode.MemoryRewardClaimDetected)) return MemoryDemoEvidenceVerdict.BlockedByRewardOrCompletionClaim;
            if (findings.Contains(MemoryClosureDiagnosticCode.MemoryPrivacyGap)) return MemoryDemoEvidenceVerdict.BlockedByPrivacyGap;
            if (findings.Contains(MemoryClosureDiagnosticCode.MemoryOfficialHistoryClaim)) return MemoryDemoEvidenceVerdict.BlockedByOfficialHistoryClaim;
            if (findings.Contains(MemoryClosureDiagnosticCode.MemoryNetworkSourceGap)) return MemoryDemoEvidenceVerdict.BlockedByMissingMemorySource;
            return MemoryDemoEvidenceVerdict.ReadyForArchitectValidation;
        }
    }
    public sealed class MemoryClosureDiagnostics { public MemoryClosureDiagnostics(MemoryDemoEvidenceVerdict verdict, IReadOnlyList<MemoryClosureDiagnosticCode> findings) { Verdict = verdict; Findings = findings ?? Array.Empty<MemoryClosureDiagnosticCode>(); } public MemoryDemoEvidenceVerdict Verdict { get; } public IReadOnlyList<MemoryClosureDiagnosticCode> Findings { get; } public bool Contains(MemoryClosureDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class MemoryNetworkClosureGateEvaluated { public MemoryNetworkClosureGateEvaluated(string gateId) { GateId = gateId ?? string.Empty; } public string GateId { get; } }
    public sealed class MemoryNetworkGapDetected { public MemoryNetworkGapDetected(string beeId) { BeeId = beeId ?? string.Empty; } public string BeeId { get; } }
    public sealed class Bee451BlockedByMemoryGate { public Bee451BlockedByMemoryGate(string message) { Message = message ?? string.Empty; } public string Message { get; } }
}
