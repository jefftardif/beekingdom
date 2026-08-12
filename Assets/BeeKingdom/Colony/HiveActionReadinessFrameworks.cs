using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum HiveIntentDomain { Upgrade, Build, Produce, Assign, Defend, Research, Evidence }
    public enum HiveIntentRailDisplayMode { CompactMobile, Expanded, Empty }
    public enum HiveIntentAuthorityState { PreviewBlocked, ServerAuthorityRequired, DisabledForDemo }
    public enum HiveIntentRailDiagnosticCode { HiveIntentMissing, HiveIntentPriorityInvalid, HiveIntentPreviewBadgeMissing, HiveIntentOfficialActionClaim, HiveIntentServerDependencyHidden, HiveIntentRouteMissing }

    public sealed class HivePlayerActionIntent
    {
        public HivePlayerActionIntent(string intentId, HiveIntentDomain domain, int priority, string zoneId, string playerReason, bool previewBadgeVisible, HiveIntentAuthorityState authorityState, bool serverDependencyVisible, bool opensZoneDetail, bool officialActionClaim = false)
        { IntentId = intentId ?? string.Empty; Domain = domain; Priority = priority; ZoneId = zoneId ?? string.Empty; PlayerReason = playerReason ?? string.Empty; PreviewBadgeVisible = previewBadgeVisible; AuthorityState = authorityState; ServerDependencyVisible = serverDependencyVisible; OpensZoneDetail = opensZoneDetail; OfficialActionClaim = officialActionClaim; }
        public string IntentId { get; } public HiveIntentDomain Domain { get; } public int Priority { get; } public string ZoneId { get; } public string PlayerReason { get; } public bool PreviewBadgeVisible { get; } public HiveIntentAuthorityState AuthorityState { get; } public bool ServerDependencyVisible { get; } public bool OpensZoneDetail { get; } public bool OfficialActionClaim { get; }
    }

    public sealed class HivePlayerActionIntentRail
    {
        private static readonly HiveIntentDomain[] RequiredDomains = { HiveIntentDomain.Upgrade, HiveIntentDomain.Build, HiveIntentDomain.Produce, HiveIntentDomain.Assign, HiveIntentDomain.Defend, HiveIntentDomain.Research, HiveIntentDomain.Evidence };
        public HivePlayerActionIntentRail(string railId, IReadOnlyList<HivePlayerActionIntent> intents, HivePlayerActionIntent selectedIntent, HiveIntentRailDisplayMode displayMode)
        { RailId = ColonyIntegrationIds.Require(railId); Intents = (intents ?? Array.Empty<HivePlayerActionIntent>()).OrderBy(i => i.Priority).ToArray(); SelectedIntent = selectedIntent; DisplayMode = displayMode; }
        public string RailId { get; } public IReadOnlyList<HivePlayerActionIntent> Intents { get; } public HivePlayerActionIntent SelectedIntent { get; } public HiveIntentRailDisplayMode DisplayMode { get; }
        public HiveIntentRailDiagnostics Evaluate()
        {
            var findings = new List<HiveIntentRailDiagnosticCode>();
            if (DisplayMode == HiveIntentRailDisplayMode.Empty || RequiredDomains.Any(required => Intents.All(i => i.Domain != required)) || Intents.Any(i => string.IsNullOrWhiteSpace(i.IntentId) || string.IsNullOrWhiteSpace(i.PlayerReason))) findings.Add(HiveIntentRailDiagnosticCode.HiveIntentMissing);
            if (Intents.Any(i => i.Priority <= 0) || Intents.Select(i => i.Priority).Distinct().Count() != Intents.Count) findings.Add(HiveIntentRailDiagnosticCode.HiveIntentPriorityInvalid);
            if (Intents.Any(i => !i.PreviewBadgeVisible)) findings.Add(HiveIntentRailDiagnosticCode.HiveIntentPreviewBadgeMissing);
            if (Intents.Any(i => i.OfficialActionClaim)) findings.Add(HiveIntentRailDiagnosticCode.HiveIntentOfficialActionClaim);
            if (Intents.Any(i => !i.ServerDependencyVisible || i.AuthorityState != HiveIntentAuthorityState.ServerAuthorityRequired)) findings.Add(HiveIntentRailDiagnosticCode.HiveIntentServerDependencyHidden);
            if (Intents.Any(i => !i.OpensZoneDetail || string.IsNullOrWhiteSpace(i.ZoneId))) findings.Add(HiveIntentRailDiagnosticCode.HiveIntentRouteMissing);
            return new HiveIntentRailDiagnostics(findings);
        }
    }

    public sealed class HiveIntentRailDiagnostics { public HiveIntentRailDiagnostics(IReadOnlyList<HiveIntentRailDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveIntentRailDiagnosticCode>(); } public IReadOnlyList<HiveIntentRailDiagnosticCode> Findings { get; } public bool Contains(HiveIntentRailDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveIntentRailOpened { public HiveIntentRailOpened(string railId) { RailId = railId ?? string.Empty; } public string RailId { get; } }
    public sealed class HiveIntentSelected { public HiveIntentSelected(string intentId) { IntentId = intentId ?? string.Empty; } public string IntentId { get; } }
    public sealed class HiveIntentBlockedReasonShown { public HiveIntentBlockedReasonShown(string intentId, string message = "Action preparee, execution serveur future requise.") { IntentId = intentId ?? string.Empty; Message = message ?? string.Empty; } public string IntentId { get; } public string Message { get; } }

    public enum HiveUpgradePreviewStatus { PreviewAvailable, MissingRequirement, ServerAuthorityRequired, HiddenUntilMilestone, DisabledForDemo }
    public enum HiveUpgradeCandidateDiagnosticCode { UpgradeCandidateMissing, UpgradeRequirementMissing, UpgradeBenefitMissing, UpgradeOfficialClaim, UpgradeServerDependencyHidden, UpgradeIntentRouteMissing }

    public sealed class HiveUpgradePreviewRequirement
    {
        public HiveUpgradePreviewRequirement(string requirementId, string label, bool visible, bool serverDependencyVisible, bool officialCalculationClaim = false)
        { RequirementId = requirementId ?? string.Empty; Label = label ?? string.Empty; Visible = visible; ServerDependencyVisible = serverDependencyVisible; OfficialCalculationClaim = officialCalculationClaim; }
        public string RequirementId { get; } public string Label { get; } public bool Visible { get; } public bool ServerDependencyVisible { get; } public bool OfficialCalculationClaim { get; }
    }

    public sealed class HiveUpgradeCandidatePreview
    {
        public HiveUpgradeCandidatePreview(string buildingId, string currentLevelLabel, string previewNextLevelLabel, string expectedPlayerBenefit, IReadOnlyList<HiveUpgradePreviewRequirement> requirements, HiveUpgradePreviewStatus status, bool linkedToIntentRail, bool serverDependencyVisible, bool officialUpgradeClaim = false)
        { BuildingId = buildingId ?? string.Empty; CurrentLevelLabel = currentLevelLabel ?? string.Empty; PreviewNextLevelLabel = previewNextLevelLabel ?? string.Empty; ExpectedPlayerBenefit = expectedPlayerBenefit ?? string.Empty; Requirements = requirements ?? Array.Empty<HiveUpgradePreviewRequirement>(); Status = status; LinkedToIntentRail = linkedToIntentRail; ServerDependencyVisible = serverDependencyVisible; OfficialUpgradeClaim = officialUpgradeClaim; }
        public string BuildingId { get; } public string CurrentLevelLabel { get; } public string PreviewNextLevelLabel { get; } public string ExpectedPlayerBenefit { get; } public IReadOnlyList<HiveUpgradePreviewRequirement> Requirements { get; } public HiveUpgradePreviewStatus Status { get; } public bool LinkedToIntentRail { get; } public bool ServerDependencyVisible { get; } public bool OfficialUpgradeClaim { get; }
    }

    public sealed class HiveUpgradeCandidatePreviewPanel
    {
        public HiveUpgradeCandidatePreviewPanel(string panelId, IReadOnlyList<HiveUpgradeCandidatePreview> candidates, HiveUpgradeCandidatePreview selectedCandidate)
        { PanelId = ColonyIntegrationIds.Require(panelId); Candidates = candidates ?? Array.Empty<HiveUpgradeCandidatePreview>(); SelectedCandidate = selectedCandidate; }
        public string PanelId { get; } public IReadOnlyList<HiveUpgradeCandidatePreview> Candidates { get; } public HiveUpgradeCandidatePreview SelectedCandidate { get; }
        public HiveUpgradeCandidateDiagnostics Evaluate()
        {
            var findings = new List<HiveUpgradeCandidateDiagnosticCode>();
            if (Candidates.Count == 0 || Candidates.Any(c => string.IsNullOrWhiteSpace(c.BuildingId) || string.IsNullOrWhiteSpace(c.CurrentLevelLabel) || string.IsNullOrWhiteSpace(c.PreviewNextLevelLabel))) findings.Add(HiveUpgradeCandidateDiagnosticCode.UpgradeCandidateMissing);
            if (Candidates.Any(c => c.Requirements.Count == 0 || c.Requirements.Any(r => !r.Visible || string.IsNullOrWhiteSpace(r.Label)))) findings.Add(HiveUpgradeCandidateDiagnosticCode.UpgradeRequirementMissing);
            if (Candidates.Any(c => string.IsNullOrWhiteSpace(c.ExpectedPlayerBenefit))) findings.Add(HiveUpgradeCandidateDiagnosticCode.UpgradeBenefitMissing);
            if (Candidates.Any(c => c.OfficialUpgradeClaim || c.Requirements.Any(r => r.OfficialCalculationClaim))) findings.Add(HiveUpgradeCandidateDiagnosticCode.UpgradeOfficialClaim);
            if (Candidates.Any(c => !c.ServerDependencyVisible || c.Status != HiveUpgradePreviewStatus.ServerAuthorityRequired || c.Requirements.Any(r => !r.ServerDependencyVisible))) findings.Add(HiveUpgradeCandidateDiagnosticCode.UpgradeServerDependencyHidden);
            if (Candidates.Any(c => !c.LinkedToIntentRail)) findings.Add(HiveUpgradeCandidateDiagnosticCode.UpgradeIntentRouteMissing);
            return new HiveUpgradeCandidateDiagnostics(findings);
        }
    }

    public sealed class HiveUpgradeCandidateDiagnostics { public HiveUpgradeCandidateDiagnostics(IReadOnlyList<HiveUpgradeCandidateDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveUpgradeCandidateDiagnosticCode>(); } public IReadOnlyList<HiveUpgradeCandidateDiagnosticCode> Findings { get; } public bool Contains(HiveUpgradeCandidateDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveUpgradeCandidateListOpened { public HiveUpgradeCandidateListOpened(string panelId) { PanelId = panelId ?? string.Empty; } public string PanelId { get; } }
    public sealed class HiveUpgradeCandidateSelected { public HiveUpgradeCandidateSelected(string buildingId) { BuildingId = buildingId ?? string.Empty; } public string BuildingId { get; } }
    public sealed class HiveUpgradeBlockedReasonShown { public HiveUpgradeBlockedReasonShown(string buildingId) { BuildingId = buildingId ?? string.Empty; } public string BuildingId { get; } }

    public enum HiveConstructionRequirementSource { Location, VisualMaturity, Infrastructure, Resource, Workforce, Server }
    public enum HiveConstructionRequirementState { PreviewSatisfied, PreviewMissing, ServerUnknown }
    public enum HiveConstructionDiagnosticCode { ConstructionPanelMissing, ConstructionRequirementMissing, ConstructionServerUnknownMissing, ConstructionOfficialClaim, ConstructionIntentRouteMissing, ConstructionServerDependencyHidden }

    public sealed class HiveConstructionRequirement
    {
        public HiveConstructionRequirement(string requirementId, HiveConstructionRequirementSource source, HiveConstructionRequirementState state, string playerExplanation, bool visible, bool serverDependencyVisible, bool officialCalculationClaim = false)
        { RequirementId = requirementId ?? string.Empty; Source = source; State = state; PlayerExplanation = playerExplanation ?? string.Empty; Visible = visible; ServerDependencyVisible = serverDependencyVisible; OfficialCalculationClaim = officialCalculationClaim; }
        public string RequirementId { get; } public HiveConstructionRequirementSource Source { get; } public HiveConstructionRequirementState State { get; } public string PlayerExplanation { get; } public bool Visible { get; } public bool ServerDependencyVisible { get; } public bool OfficialCalculationClaim { get; }
    }

    public sealed class HiveConstructionPrerequisitePanel
    {
        public HiveConstructionPrerequisitePanel(string panelId, string lockedZoneId, IReadOnlyList<HiveConstructionRequirement> requirements, bool linkedToIntentRail, bool constructionStartClaim = false)
        { PanelId = ColonyIntegrationIds.Require(panelId); LockedZoneId = lockedZoneId ?? string.Empty; Requirements = requirements ?? Array.Empty<HiveConstructionRequirement>(); LinkedToIntentRail = linkedToIntentRail; ConstructionStartClaim = constructionStartClaim; }
        public string PanelId { get; } public string LockedZoneId { get; } public IReadOnlyList<HiveConstructionRequirement> Requirements { get; } public bool LinkedToIntentRail { get; } public bool ConstructionStartClaim { get; }
        public HiveConstructionDiagnostics Evaluate()
        {
            var findings = new List<HiveConstructionDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(LockedZoneId) || Requirements.Count == 0) findings.Add(HiveConstructionDiagnosticCode.ConstructionPanelMissing);
            if (Requirements.Any(r => !r.Visible || string.IsNullOrWhiteSpace(r.PlayerExplanation) || string.IsNullOrWhiteSpace(r.RequirementId))) findings.Add(HiveConstructionDiagnosticCode.ConstructionRequirementMissing);
            if (Requirements.All(r => r.State != HiveConstructionRequirementState.ServerUnknown)) findings.Add(HiveConstructionDiagnosticCode.ConstructionServerUnknownMissing);
            if (ConstructionStartClaim || Requirements.Any(r => r.OfficialCalculationClaim)) findings.Add(HiveConstructionDiagnosticCode.ConstructionOfficialClaim);
            if (!LinkedToIntentRail) findings.Add(HiveConstructionDiagnosticCode.ConstructionIntentRouteMissing);
            if (Requirements.Any(r => !r.ServerDependencyVisible)) findings.Add(HiveConstructionDiagnosticCode.ConstructionServerDependencyHidden);
            return new HiveConstructionDiagnostics(findings);
        }
    }

    public sealed class HiveConstructionPrerequisitePreview { public HiveConstructionPrerequisitePreview(string zoneId, HiveConstructionPrerequisitePanel panel) { ZoneId = zoneId ?? string.Empty; Panel = panel; } public string ZoneId { get; } public HiveConstructionPrerequisitePanel Panel { get; } }
    public sealed class HiveConstructionDiagnostics { public HiveConstructionDiagnostics(IReadOnlyList<HiveConstructionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveConstructionDiagnosticCode>(); } public IReadOnlyList<HiveConstructionDiagnosticCode> Findings { get; } public bool Contains(HiveConstructionDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveConstructionPrerequisitePanelOpened { public HiveConstructionPrerequisitePanelOpened(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }
    public sealed class HiveConstructionRequirementFocused { public HiveConstructionRequirementFocused(string requirementId) { RequirementId = requirementId ?? string.Empty; } public string RequirementId { get; } }
    public sealed class HiveConstructionPreviewBlocked { public HiveConstructionPreviewBlocked(string zoneId, string message = "Construction non disponible : prerequis preview et autorite serveur requis.") { ZoneId = zoneId ?? string.Empty; Message = message ?? string.Empty; } public string ZoneId { get; } public string Message { get; } }

    public enum HiveProductionQueuePreviewState { PreviewOnly, MissingResource, MissingRole, ServerAuthorityRequired, DisabledForDemo }
    public enum HiveProductionQueueDiagnosticCode { ProductionProducerMissing, ProductionSlotMissing, ProductionPreviewDataMissing, ProductionOfficialClaim, ProductionShortageRouteMissing, ProductionWorkforceRouteMissing, ProductionServerDependencyHidden }

    public sealed class HiveProductionIntentSlot
    {
        public HiveProductionIntentSlot(string slotId, string inputPreview, string outputPreview, string requiredRole, bool missingResource, bool missingRole, bool routeToShortage, bool routeToWorkforce, bool serverDependencyVisible, bool timerClaim = false, bool collectClaim = false, bool spendClaim = false, bool accelerationClaim = false)
        { SlotId = slotId ?? string.Empty; InputPreview = inputPreview ?? string.Empty; OutputPreview = outputPreview ?? string.Empty; RequiredRole = requiredRole ?? string.Empty; MissingResource = missingResource; MissingRole = missingRole; RouteToShortage = routeToShortage; RouteToWorkforce = routeToWorkforce; ServerDependencyVisible = serverDependencyVisible; TimerClaim = timerClaim; CollectClaim = collectClaim; SpendClaim = spendClaim; AccelerationClaim = accelerationClaim; }
        public string SlotId { get; } public string InputPreview { get; } public string OutputPreview { get; } public string RequiredRole { get; } public bool MissingResource { get; } public bool MissingRole { get; } public bool RouteToShortage { get; } public bool RouteToWorkforce { get; } public bool ServerDependencyVisible { get; } public bool TimerClaim { get; } public bool CollectClaim { get; } public bool SpendClaim { get; } public bool AccelerationClaim { get; }
    }

    public sealed class HiveProductionQueueIntentPreview
    {
        public HiveProductionQueueIntentPreview(string producerZoneId, IReadOnlyList<HiveProductionIntentSlot> slots, HiveProductionQueuePreviewState state)
        { ProducerZoneId = producerZoneId ?? string.Empty; Slots = slots ?? Array.Empty<HiveProductionIntentSlot>(); State = state; }
        public string ProducerZoneId { get; } public IReadOnlyList<HiveProductionIntentSlot> Slots { get; } public HiveProductionQueuePreviewState State { get; }
        public HiveProductionQueueDiagnostics Evaluate()
        {
            var findings = new List<HiveProductionQueueDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(ProducerZoneId)) findings.Add(HiveProductionQueueDiagnosticCode.ProductionProducerMissing);
            if (Slots.Count < 3 || Slots.Any(s => string.IsNullOrWhiteSpace(s.SlotId))) findings.Add(HiveProductionQueueDiagnosticCode.ProductionSlotMissing);
            if (Slots.Any(s => string.IsNullOrWhiteSpace(s.InputPreview) || string.IsNullOrWhiteSpace(s.OutputPreview) || string.IsNullOrWhiteSpace(s.RequiredRole))) findings.Add(HiveProductionQueueDiagnosticCode.ProductionPreviewDataMissing);
            if (Slots.Any(s => s.TimerClaim || s.CollectClaim || s.SpendClaim || s.AccelerationClaim) || State != HiveProductionQueuePreviewState.ServerAuthorityRequired) findings.Add(HiveProductionQueueDiagnosticCode.ProductionOfficialClaim);
            if (Slots.Any(s => s.MissingResource && !s.RouteToShortage)) findings.Add(HiveProductionQueueDiagnosticCode.ProductionShortageRouteMissing);
            if (Slots.Any(s => s.MissingRole && !s.RouteToWorkforce)) findings.Add(HiveProductionQueueDiagnosticCode.ProductionWorkforceRouteMissing);
            if (Slots.Any(s => !s.ServerDependencyVisible)) findings.Add(HiveProductionQueueDiagnosticCode.ProductionServerDependencyHidden);
            return new HiveProductionQueueDiagnostics(findings);
        }
    }

    public sealed class HiveProductionQueueDiagnostics { public HiveProductionQueueDiagnostics(IReadOnlyList<HiveProductionQueueDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveProductionQueueDiagnosticCode>(); } public IReadOnlyList<HiveProductionQueueDiagnosticCode> Findings { get; } public bool Contains(HiveProductionQueueDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveProductionIntentQueueOpened { public HiveProductionIntentQueueOpened(string producerZoneId) { ProducerZoneId = producerZoneId ?? string.Empty; } public string ProducerZoneId { get; } }
    public sealed class HiveProductionIntentSlotFocused { public HiveProductionIntentSlotFocused(string slotId) { SlotId = slotId ?? string.Empty; } public string SlotId { get; } }
    public sealed class HiveProductionIntentBlocked { public HiveProductionIntentBlocked(string producerZoneId, string message = "Production non lancee : file serveur future requise.") { ProducerZoneId = producerZoneId ?? string.Empty; Message = message ?? string.Empty; } public string ProducerZoneId { get; } public string Message { get; } }

    public enum HiveResourceResolutionKind { ImproveStorage, PrepareProduction, InspectWorkerNeed, AskAllianceLater, WaitForServerRules, OpenEvidence }
    public enum HiveResolutionAvailability { PreviewRouteAvailable, FutureServerRule, BlockedTransaction, DisabledForDemo }
    public enum HiveResourceShortageDiagnosticCode { ShortageMissing, ResolutionOptionMissing, ResolutionTransactionClaim, ResolutionRouteMissing, ResolutionServerDependencyHidden }

    public sealed class HiveResourceResolutionOption
    {
        public HiveResourceResolutionOption(string optionId, HiveResourceResolutionKind kind, string playerExplanation, HiveResolutionAvailability availability, string routeBee, bool serverDependencyVisible, bool transactionClaim = false)
        { OptionId = optionId ?? string.Empty; Kind = kind; PlayerExplanation = playerExplanation ?? string.Empty; Availability = availability; RouteBee = routeBee ?? string.Empty; ServerDependencyVisible = serverDependencyVisible; TransactionClaim = transactionClaim; }
        public string OptionId { get; } public HiveResourceResolutionKind Kind { get; } public string PlayerExplanation { get; } public HiveResolutionAvailability Availability { get; } public string RouteBee { get; } public bool ServerDependencyVisible { get; } public bool TransactionClaim { get; }
    }

    public sealed class HiveResourceShortageResolutionPath
    {
        public HiveResourceShortageResolutionPath(string resourceId, string shortageLabel, IReadOnlyList<HiveResourceResolutionOption> options)
        { ResourceId = resourceId ?? string.Empty; ShortageLabel = shortageLabel ?? string.Empty; Options = options ?? Array.Empty<HiveResourceResolutionOption>(); }
        public string ResourceId { get; } public string ShortageLabel { get; } public IReadOnlyList<HiveResourceResolutionOption> Options { get; }
        public HiveResourceShortageDiagnostics Evaluate()
        {
            var findings = new List<HiveResourceShortageDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(ResourceId) || string.IsNullOrWhiteSpace(ShortageLabel)) findings.Add(HiveResourceShortageDiagnosticCode.ShortageMissing);
            if (Options.Count < 3 || Options.Any(o => string.IsNullOrWhiteSpace(o.OptionId) || string.IsNullOrWhiteSpace(o.PlayerExplanation))) findings.Add(HiveResourceShortageDiagnosticCode.ResolutionOptionMissing);
            if (Options.Any(o => o.TransactionClaim)) findings.Add(HiveResourceShortageDiagnosticCode.ResolutionTransactionClaim);
            if (Options.Any(o => o.Kind == HiveResourceResolutionKind.ImproveStorage && !Same(o.RouteBee, "BEE-472")) || Options.Any(o => o.Kind == HiveResourceResolutionKind.PrepareProduction && !Same(o.RouteBee, "BEE-474")) || Options.Any(o => o.Kind == HiveResourceResolutionKind.InspectWorkerNeed && !Same(o.RouteBee, "BEE-476"))) findings.Add(HiveResourceShortageDiagnosticCode.ResolutionRouteMissing);
            if (Options.Any(o => !o.ServerDependencyVisible || (o.Kind == HiveResourceResolutionKind.AskAllianceLater && o.Availability != HiveResolutionAvailability.FutureServerRule))) findings.Add(HiveResourceShortageDiagnosticCode.ResolutionServerDependencyHidden);
            return new HiveResourceShortageDiagnostics(findings);
        }
        private static bool Same(string left, string right) { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }

    public sealed class HiveResourceShortageDiagnostics { public HiveResourceShortageDiagnostics(IReadOnlyList<HiveResourceShortageDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveResourceShortageDiagnosticCode>(); } public IReadOnlyList<HiveResourceShortageDiagnosticCode> Findings { get; } public bool Contains(HiveResourceShortageDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveResourceShortageOpened { public HiveResourceShortageOpened(string resourceId) { ResourceId = resourceId ?? string.Empty; } public string ResourceId { get; } }
    public sealed class HiveResourceResolutionOptionFocused { public HiveResourceResolutionOptionFocused(string optionId) { OptionId = optionId ?? string.Empty; } public string OptionId { get; } }
    public sealed class HiveResourceTransactionBlocked { public HiveResourceTransactionBlocked(string resourceId, string message = "Resolution preview uniquement : economie officielle serveur future.") { ResourceId = resourceId ?? string.Empty; Message = message ?? string.Empty; } public string ResourceId { get; } public string Message { get; } }

    public enum HiveWorkforceNeedCategory { Production, Defense, Research }
    public enum HiveWorkforceNeedStatus { PreviewNeed, ServerAssignmentRequired, TrainingFuture, DisabledForDemo }
    public enum HiveWorkforceDiagnosticCode { WorkforceNeedMissing, WorkforceSeverityMissing, WorkforceAssignmentClaim, WorkforcePopulationOfficialClaim, WorkforceServerDependencyHidden }

    public sealed class HiveWorkforceNeedPreview
    {
        public HiveWorkforceNeedPreview(string roleId, string zoneId, HiveWorkforceNeedCategory category, int severity, string playerExplanation, HiveWorkforceNeedStatus status, bool serverDependencyVisible, bool assignmentClaim = false, bool trainingClaim = false)
        { RoleId = roleId ?? string.Empty; ZoneId = zoneId ?? string.Empty; Category = category; Severity = severity; PlayerExplanation = playerExplanation ?? string.Empty; Status = status; ServerDependencyVisible = serverDependencyVisible; AssignmentClaim = assignmentClaim; TrainingClaim = trainingClaim; }
        public string RoleId { get; } public string ZoneId { get; } public HiveWorkforceNeedCategory Category { get; } public int Severity { get; } public string PlayerExplanation { get; } public HiveWorkforceNeedStatus Status { get; } public bool ServerDependencyVisible { get; } public bool AssignmentClaim { get; } public bool TrainingClaim { get; }
    }

    public sealed class HiveWorkforceCoverageSummary
    {
        public HiveWorkforceCoverageSummary(string text, bool visible, bool officialPopulationClaim = false) { Text = text ?? string.Empty; Visible = visible; OfficialPopulationClaim = officialPopulationClaim; }
        public string Text { get; } public bool Visible { get; } public bool OfficialPopulationClaim { get; }
    }

    public sealed class HiveWorkforcePreparationPlanner
    {
        private static readonly HiveWorkforceNeedCategory[] RequiredCategories = { HiveWorkforceNeedCategory.Production, HiveWorkforceNeedCategory.Defense, HiveWorkforceNeedCategory.Research };
        public HiveWorkforcePreparationPlanner(string plannerId, IReadOnlyList<HiveWorkforceNeedPreview> needs, HiveWorkforceCoverageSummary coverage)
        { PlannerId = ColonyIntegrationIds.Require(plannerId); Needs = needs ?? Array.Empty<HiveWorkforceNeedPreview>(); Coverage = coverage; }
        public string PlannerId { get; } public IReadOnlyList<HiveWorkforceNeedPreview> Needs { get; } public HiveWorkforceCoverageSummary Coverage { get; }
        public HiveWorkforceDiagnostics Evaluate()
        {
            var findings = new List<HiveWorkforceDiagnosticCode>();
            if (RequiredCategories.Any(required => Needs.All(n => n.Category != required)) || Needs.Any(n => string.IsNullOrWhiteSpace(n.RoleId) || string.IsNullOrWhiteSpace(n.ZoneId))) findings.Add(HiveWorkforceDiagnosticCode.WorkforceNeedMissing);
            if (Needs.Any(n => n.Severity <= 0 || string.IsNullOrWhiteSpace(n.PlayerExplanation)) || Coverage == null || !Coverage.Visible || string.IsNullOrWhiteSpace(Coverage.Text)) findings.Add(HiveWorkforceDiagnosticCode.WorkforceSeverityMissing);
            if (Needs.Any(n => n.AssignmentClaim || n.TrainingClaim || n.Status != HiveWorkforceNeedStatus.ServerAssignmentRequired)) findings.Add(HiveWorkforceDiagnosticCode.WorkforceAssignmentClaim);
            if (Coverage != null && Coverage.OfficialPopulationClaim) findings.Add(HiveWorkforceDiagnosticCode.WorkforcePopulationOfficialClaim);
            if (Needs.Any(n => !n.ServerDependencyVisible)) findings.Add(HiveWorkforceDiagnosticCode.WorkforceServerDependencyHidden);
            return new HiveWorkforceDiagnostics(findings);
        }
    }

    public sealed class HiveWorkforceDiagnostics { public HiveWorkforceDiagnostics(IReadOnlyList<HiveWorkforceDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveWorkforceDiagnosticCode>(); } public IReadOnlyList<HiveWorkforceDiagnosticCode> Findings { get; } public bool Contains(HiveWorkforceDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveWorkforcePlannerOpened { public HiveWorkforcePlannerOpened(string plannerId) { PlannerId = plannerId ?? string.Empty; } public string PlannerId { get; } }
    public sealed class HiveWorkforceNeedSelected { public HiveWorkforceNeedSelected(string roleId) { RoleId = roleId ?? string.Empty; } public string RoleId { get; } }
    public sealed class HiveWorkforceAssignmentBlocked { public HiveWorkforceAssignmentBlocked(string roleId, string message = "Affectation non appliquee : population serveur future requise.") { RoleId = roleId ?? string.Empty; Message = message ?? string.Empty; } public string RoleId { get; } public string Message { get; } }

    public enum HiveDefenseReadinessLevel { Calm, Fragile, Incomplete, PreparedPreview }
    public enum HiveDefenseItemState { PreviewNeed, RoleMissing, ServerThreatUnknown, DisabledForDemo }
    public enum HiveDefenseDiagnosticCode { DefenseZoneMissing, DefenseRoleNeedMissing, DefenseLiveActionClaim, DefenseServerBoundaryMissing, DefenseServerDependencyHidden }

    public sealed class HiveDefenseReadinessItem
    {
        public HiveDefenseReadinessItem(string zoneId, string roleNeed, string playerExplanation, HiveDefenseItemState state, bool serverDependencyVisible, bool liveActionClaim = false)
        { ZoneId = zoneId ?? string.Empty; RoleNeed = roleNeed ?? string.Empty; PlayerExplanation = playerExplanation ?? string.Empty; State = state; ServerDependencyVisible = serverDependencyVisible; LiveActionClaim = liveActionClaim; }
        public string ZoneId { get; } public string RoleNeed { get; } public string PlayerExplanation { get; } public HiveDefenseItemState State { get; } public bool ServerDependencyVisible { get; } public bool LiveActionClaim { get; }
    }

    public sealed class HiveDefenseReadinessSnapshot
    {
        private static readonly string[] RequiredZones = { "defense", "caserne", "centre-alliance" };
        public HiveDefenseReadinessSnapshot(string snapshotId, IReadOnlyList<HiveDefenseReadinessItem> items, HiveDefenseReadinessLevel previewLevel, IReadOnlyList<string> serverBoundaries)
        { SnapshotId = ColonyIntegrationIds.Require(snapshotId); Items = items ?? Array.Empty<HiveDefenseReadinessItem>(); PreviewLevel = previewLevel; ServerBoundaries = serverBoundaries ?? Array.Empty<string>(); }
        public string SnapshotId { get; } public IReadOnlyList<HiveDefenseReadinessItem> Items { get; } public HiveDefenseReadinessLevel PreviewLevel { get; } public IReadOnlyList<string> ServerBoundaries { get; }
        public HiveDefenseDiagnostics Evaluate()
        {
            var findings = new List<HiveDefenseDiagnosticCode>();
            if (RequiredZones.Any(required => Items.All(i => !string.Equals(i.ZoneId, required, StringComparison.OrdinalIgnoreCase)))) findings.Add(HiveDefenseDiagnosticCode.DefenseZoneMissing);
            if (Items.Any(i => string.IsNullOrWhiteSpace(i.RoleNeed) || string.IsNullOrWhiteSpace(i.PlayerExplanation))) findings.Add(HiveDefenseDiagnosticCode.DefenseRoleNeedMissing);
            if (Items.Any(i => i.LiveActionClaim)) findings.Add(HiveDefenseDiagnosticCode.DefenseLiveActionClaim);
            if (ServerBoundaries.Count < 5) findings.Add(HiveDefenseDiagnosticCode.DefenseServerBoundaryMissing);
            if (Items.Any(i => !i.ServerDependencyVisible || i.State != HiveDefenseItemState.ServerThreatUnknown)) findings.Add(HiveDefenseDiagnosticCode.DefenseServerDependencyHidden);
            return new HiveDefenseDiagnostics(findings);
        }
    }

    public sealed class HiveDefenseDiagnostics { public HiveDefenseDiagnostics(IReadOnlyList<HiveDefenseDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveDefenseDiagnosticCode>(); } public IReadOnlyList<HiveDefenseDiagnosticCode> Findings { get; } public bool Contains(HiveDefenseDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveDefenseReadinessOpened { public HiveDefenseReadinessOpened(string snapshotId) { SnapshotId = snapshotId ?? string.Empty; } public string SnapshotId { get; } }
    public sealed class HiveDefenseReadinessItemFocused { public HiveDefenseReadinessItemFocused(string zoneId) { ZoneId = zoneId ?? string.Empty; } public string ZoneId { get; } }
    public sealed class HiveDefenseLiveActionBlocked { public HiveDefenseLiveActionBlocked(string zoneId, string message = "Defense preview uniquement : combat et protection serveur futurs.") { ZoneId = zoneId ?? string.Empty; Message = message ?? string.Empty; } public string ZoneId { get; } public string Message { get; } }

    public enum HiveStrategicChoiceDomain { Research, Genetics, Economy, Defense, Workforce, AllianceSupport }
    public enum HiveChoiceAuthorityState { PreviewOnly, ServerProgressionRequired, ToDefine, DisabledForDemo }
    public enum HiveResearchGeneticsDiagnosticCode { StrategicChoiceMissing, ResearchGeneticsDomainMissing, StrategicChoiceBenefitMissing, StrategicChoiceOfficialClaim, StrategicChoiceServerDependencyHidden }

    public sealed class HiveStrategicChoicePreview
    {
        public HiveStrategicChoicePreview(string choiceId, HiveStrategicChoiceDomain domain, string playerFantasy, string previewBenefit, HiveChoiceAuthorityState authorityState, string zoneId, bool serverDependencyVisible, bool activationClaim = false, bool officialBonusClaim = false)
        { ChoiceId = choiceId ?? string.Empty; Domain = domain; PlayerFantasy = playerFantasy ?? string.Empty; PreviewBenefit = previewBenefit ?? string.Empty; AuthorityState = authorityState; ZoneId = zoneId ?? string.Empty; ServerDependencyVisible = serverDependencyVisible; ActivationClaim = activationClaim; OfficialBonusClaim = officialBonusClaim; }
        public string ChoiceId { get; } public HiveStrategicChoiceDomain Domain { get; } public string PlayerFantasy { get; } public string PreviewBenefit { get; } public HiveChoiceAuthorityState AuthorityState { get; } public string ZoneId { get; } public bool ServerDependencyVisible { get; } public bool ActivationClaim { get; } public bool OfficialBonusClaim { get; }
    }

    public sealed class HiveResearchGeneticsChoicePreview
    {
        public HiveResearchGeneticsChoicePreview(string previewId, IReadOnlyList<HiveStrategicChoicePreview> choices, HiveStrategicChoicePreview selectedChoice)
        { PreviewId = ColonyIntegrationIds.Require(previewId); Choices = choices ?? Array.Empty<HiveStrategicChoicePreview>(); SelectedChoice = selectedChoice; }
        public string PreviewId { get; } public IReadOnlyList<HiveStrategicChoicePreview> Choices { get; } public HiveStrategicChoicePreview SelectedChoice { get; }
        public HiveResearchGeneticsDiagnostics Evaluate()
        {
            var findings = new List<HiveResearchGeneticsDiagnosticCode>();
            if (Choices.Count < 5 || Choices.Any(c => string.IsNullOrWhiteSpace(c.ChoiceId) || string.IsNullOrWhiteSpace(c.ZoneId))) findings.Add(HiveResearchGeneticsDiagnosticCode.StrategicChoiceMissing);
            if (Choices.All(c => c.Domain != HiveStrategicChoiceDomain.Research) || Choices.All(c => c.Domain != HiveStrategicChoiceDomain.Genetics)) findings.Add(HiveResearchGeneticsDiagnosticCode.ResearchGeneticsDomainMissing);
            if (Choices.Any(c => string.IsNullOrWhiteSpace(c.PlayerFantasy) || string.IsNullOrWhiteSpace(c.PreviewBenefit))) findings.Add(HiveResearchGeneticsDiagnosticCode.StrategicChoiceBenefitMissing);
            if (Choices.Any(c => c.ActivationClaim || c.OfficialBonusClaim)) findings.Add(HiveResearchGeneticsDiagnosticCode.StrategicChoiceOfficialClaim);
            if (Choices.Any(c => !c.ServerDependencyVisible || (c.AuthorityState != HiveChoiceAuthorityState.ServerProgressionRequired && c.AuthorityState != HiveChoiceAuthorityState.ToDefine))) findings.Add(HiveResearchGeneticsDiagnosticCode.StrategicChoiceServerDependencyHidden);
            return new HiveResearchGeneticsDiagnostics(findings);
        }
    }

    public sealed class HiveResearchGeneticsDiagnostics { public HiveResearchGeneticsDiagnostics(IReadOnlyList<HiveResearchGeneticsDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveResearchGeneticsDiagnosticCode>(); } public IReadOnlyList<HiveResearchGeneticsDiagnosticCode> Findings { get; } public bool Contains(HiveResearchGeneticsDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveResearchGeneticsPreviewOpened { public HiveResearchGeneticsPreviewOpened(string previewId) { PreviewId = previewId ?? string.Empty; } public string PreviewId { get; } }
    public sealed class HiveStrategicChoicePreviewSelected { public HiveStrategicChoicePreviewSelected(string choiceId) { ChoiceId = choiceId ?? string.Empty; } public string ChoiceId { get; } }
    public sealed class HiveStrategicChoiceActivationBlocked { public HiveStrategicChoiceActivationBlocked(string choiceId, string message = "Choix non applique : progression officielle serveur future.") { ChoiceId = choiceId ?? string.Empty; Message = message ?? string.Empty; } public string ChoiceId { get; } public string Message { get; } }

    public enum HiveSessionStepStatus { NotStarted, Viewed, Understood, BlockedByPreviewLimit, Skipped }
    public enum HiveSessionGoalState { DemoQaPreview, TargetExperiencePreview, BlockedByPreviewLimits }
    public enum HiveSessionFlowDiagnosticCode { SessionStepCountInvalid, SessionRequiredStepMissing, SessionInstructionMissing, SessionRewardClaim, SessionPersistentSummaryClaim, SessionServerDependencyHidden }

    public sealed class HiveSessionGoalStep
    {
        public HiveSessionGoalStep(string stepId, string playerInstruction, string relatedBee, HiveSessionStepStatus status, bool serverDependencyVisible)
        { StepId = stepId ?? string.Empty; PlayerInstruction = playerInstruction ?? string.Empty; RelatedBee = relatedBee ?? string.Empty; Status = status; ServerDependencyVisible = serverDependencyVisible; }
        public string StepId { get; } public string PlayerInstruction { get; } public string RelatedBee { get; } public HiveSessionStepStatus Status { get; } public bool ServerDependencyVisible { get; }
    }

    public sealed class HiveSessionExitSummary
    {
        public HiveSessionExitSummary(string text, bool visible, bool persistentClaim = false, bool rewardClaim = false, bool streakClaim = false)
        { Text = text ?? string.Empty; Visible = visible; PersistentClaim = persistentClaim; RewardClaim = rewardClaim; StreakClaim = streakClaim; }
        public string Text { get; } public bool Visible { get; } public bool PersistentClaim { get; } public bool RewardClaim { get; } public bool StreakClaim { get; }
    }

    public sealed class HiveMobileSessionGoalFlow
    {
        public HiveMobileSessionGoalFlow(string flowId, IReadOnlyList<HiveSessionGoalStep> steps, HiveSessionGoalState state, HiveSessionExitSummary exitSummary)
        { FlowId = ColonyIntegrationIds.Require(flowId); Steps = steps ?? Array.Empty<HiveSessionGoalStep>(); State = state; ExitSummary = exitSummary; }
        public string FlowId { get; } public IReadOnlyList<HiveSessionGoalStep> Steps { get; } public HiveSessionGoalState State { get; } public HiveSessionExitSummary ExitSummary { get; }
        public HiveSessionFlowDiagnostics Evaluate()
        {
            var findings = new List<HiveSessionFlowDiagnosticCode>();
            if (Steps.Count < 4 || Steps.Count > 6) findings.Add(HiveSessionFlowDiagnosticCode.SessionStepCountInvalid);
            if (!HasBee("BEE-471") || !HasBee("BEE-475") || Steps.All(s => s.Status != HiveSessionStepStatus.BlockedByPreviewLimit) || ExitSummary == null || !ExitSummary.Visible) findings.Add(HiveSessionFlowDiagnosticCode.SessionRequiredStepMissing);
            if (Steps.Any(s => string.IsNullOrWhiteSpace(s.StepId) || string.IsNullOrWhiteSpace(s.PlayerInstruction) || s.PlayerInstruction.Length > 90)) findings.Add(HiveSessionFlowDiagnosticCode.SessionInstructionMissing);
            if (ExitSummary != null && (ExitSummary.RewardClaim || ExitSummary.StreakClaim)) findings.Add(HiveSessionFlowDiagnosticCode.SessionRewardClaim);
            if (ExitSummary != null && ExitSummary.PersistentClaim) findings.Add(HiveSessionFlowDiagnosticCode.SessionPersistentSummaryClaim);
            if (Steps.Any(s => !s.ServerDependencyVisible)) findings.Add(HiveSessionFlowDiagnosticCode.SessionServerDependencyHidden);
            return new HiveSessionFlowDiagnostics(findings);
        }
        private bool HasBee(string bee) { return Steps.Any(s => string.Equals(s.RelatedBee, bee, StringComparison.OrdinalIgnoreCase)); }
    }

    public sealed class HiveSessionFlowDiagnostics { public HiveSessionFlowDiagnostics(IReadOnlyList<HiveSessionFlowDiagnosticCode> findings) { Findings = findings ?? Array.Empty<HiveSessionFlowDiagnosticCode>(); } public IReadOnlyList<HiveSessionFlowDiagnosticCode> Findings { get; } public bool Contains(HiveSessionFlowDiagnosticCode code) { return Findings.Contains(code); } }
    public sealed class HiveSessionGoalFlowStarted { public HiveSessionGoalFlowStarted(string flowId) { FlowId = flowId ?? string.Empty; } public string FlowId { get; } }
    public sealed class HiveSessionGoalStepViewed { public HiveSessionGoalStepViewed(string stepId) { StepId = stepId ?? string.Empty; } public string StepId { get; } }
    public sealed class HiveSessionGoalExitSummaryShown { public HiveSessionGoalExitSummaryShown(string flowId) { FlowId = flowId ?? string.Empty; } public string FlowId { get; } }

    public enum HiveActionReadinessCoverageStatus { Covered, PreviewReserve, MissingDemoPath, NeedsRevision, HiddenServerDependency, OfficialActionActive, MissingSurface }
    public enum HiveActionReadinessVerdict { ReadyForArchitectValidation, ReadyWithReserves, BlockedByMissingSurface, BlockedByMissingDemoPath, BlockedByOfficialAction, BlockedByHiddenServerDependency, BlockedByBee481Premature }
    public enum Bee481BlockerStatus { BlockedUntilArchitectValidation, StillBlockedAfterRevision, ReleasedByFutureArchitectDecision }

    public sealed class HiveActionReadinessCoverageRow
    {
        public HiveActionReadinessCoverageRow(string beeId, string surface, HiveActionReadinessCoverageStatus status, string evidenceSource, bool officialActionActive, bool hiddenServerDependency, bool demoPathVisible)
        { BeeId = beeId ?? string.Empty; Surface = surface ?? string.Empty; Status = status; EvidenceSource = evidenceSource ?? string.Empty; OfficialActionActive = officialActionActive; HiddenServerDependency = hiddenServerDependency; DemoPathVisible = demoPathVisible; }
        public string BeeId { get; } public string Surface { get; } public HiveActionReadinessCoverageStatus Status { get; } public string EvidenceSource { get; } public bool OfficialActionActive { get; } public bool HiddenServerDependency { get; } public bool DemoPathVisible { get; }
    }

    public sealed class HiveActionReadinessReserve
    {
        public HiveActionReadinessReserve(string reserveId, string description) { ReserveId = reserveId ?? string.Empty; Description = description ?? string.Empty; }
        public string ReserveId { get; } public string Description { get; }
    }

    public sealed class HiveActionReadinessClosureGate
    {
        private static readonly string[] RequiredBees = { "BEE-471", "BEE-472", "BEE-473", "BEE-474", "BEE-475", "BEE-476", "BEE-477", "BEE-478", "BEE-479" };
        public HiveActionReadinessClosureGate(string gateId, IReadOnlyList<HiveActionReadinessCoverageRow> coverage, IReadOnlyList<HiveActionReadinessReserve> reserves, Bee481BlockerStatus bee481Status)
        { GateId = ColonyIntegrationIds.Require(gateId); Coverage = coverage ?? Array.Empty<HiveActionReadinessCoverageRow>(); Reserves = reserves ?? Array.Empty<HiveActionReadinessReserve>(); Bee481Status = bee481Status; Verdict = EvaluateVerdict(); }
        public string GateId { get; } public IReadOnlyList<HiveActionReadinessCoverageRow> Coverage { get; } public IReadOnlyList<HiveActionReadinessReserve> Reserves { get; } public HiveActionReadinessVerdict Verdict { get; } public Bee481BlockerStatus Bee481Status { get; }
        private HiveActionReadinessVerdict EvaluateVerdict()
        {
            if (Bee481Status == Bee481BlockerStatus.ReleasedByFutureArchitectDecision) return HiveActionReadinessVerdict.BlockedByBee481Premature;
            if (RequiredBees.Any(required => Coverage.All(r => !string.Equals(r.BeeId, required, StringComparison.OrdinalIgnoreCase))) || Coverage.Any(r => r.Status == HiveActionReadinessCoverageStatus.MissingSurface)) return HiveActionReadinessVerdict.BlockedByMissingSurface;
            if (Coverage.Any(r => r.OfficialActionActive || r.Status == HiveActionReadinessCoverageStatus.OfficialActionActive)) return HiveActionReadinessVerdict.BlockedByOfficialAction;
            if (Coverage.Any(r => r.HiddenServerDependency || r.Status == HiveActionReadinessCoverageStatus.HiddenServerDependency)) return HiveActionReadinessVerdict.BlockedByHiddenServerDependency;
            if (Coverage.Any(r => !r.DemoPathVisible || r.Status == HiveActionReadinessCoverageStatus.MissingDemoPath)) return HiveActionReadinessVerdict.BlockedByMissingDemoPath;
            return Reserves.Count > 0 || Coverage.Any(r => r.Status == HiveActionReadinessCoverageStatus.PreviewReserve || r.Status == HiveActionReadinessCoverageStatus.NeedsRevision) ? HiveActionReadinessVerdict.ReadyWithReserves : HiveActionReadinessVerdict.ReadyForArchitectValidation;
        }
    }

    public sealed class HiveActionReadinessGateEvaluated { public HiveActionReadinessGateEvaluated(string gateId, HiveActionReadinessVerdict verdict) { GateId = gateId ?? string.Empty; Verdict = verdict; } public string GateId { get; } public HiveActionReadinessVerdict Verdict { get; } }
    public sealed class HiveActionReadinessReserveRegistered { public HiveActionReadinessReserveRegistered(string reserveId) { ReserveId = reserveId ?? string.Empty; } public string ReserveId { get; } }
    public sealed class Bee481BlockerConfirmed { public Bee481BlockerConfirmed(Bee481BlockerStatus status) { Status = status; } public Bee481BlockerStatus Status { get; } }
}
