using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum SocialMmoProductPillar { SocialMmo, Alliances, Alliance, Diplomacy, War, PvP, PvpWar, Communication, PlayerProgression, HiveProgression, Army, Economy, LivingWorld, Simulation }
    public enum SocialServerAuthorityMarker { LocalProjection, DemoOnly, ServerAuthorityRequired, OutOfScope }

    public enum PlayerHiveIdentityStatus { ProjectedLocalIdentity, MissingHiveSource, MissingPlayerReference, ServerAuthorityRequired, OutOfScope, PersistentProfileBlocked }
    public enum PlayerHiveIdentityDiagnosticCode { PlayerHiveIdentitySourceMissing, PlayerReferenceNotAuthoritative, PersistentProfileRequested, IdentityVisibilityMissing, ServerAccountBypassRequested }

    public sealed class PlayerHiveIdentitySource
    {
        public PlayerHiveIdentitySource(string hiveReference, string sourceBee) { HiveReference = hiveReference ?? string.Empty; SourceBee = sourceBee ?? string.Empty; }
        public string HiveReference { get; }
        public string SourceBee { get; }
    }

    public sealed class PlayerHivePublicProfileProjection
    {
        public PlayerHivePublicProfileProjection(string displayName, string postureHint, string socialDisplayHint, string allianceDisplayHint, string specializationHint, IReadOnlyList<string> visibleLimitations)
        {
            DisplayName = displayName ?? string.Empty;
            PostureHint = postureHint ?? string.Empty;
            SocialDisplayHint = socialDisplayHint ?? string.Empty;
            AllianceDisplayHint = allianceDisplayHint ?? string.Empty;
            SpecializationHint = specializationHint ?? string.Empty;
            VisibleLimitations = visibleLimitations ?? Array.Empty<string>();
        }

        public string DisplayName { get; }
        public string PostureHint { get; }
        public string SocialDisplayHint { get; }
        public string AllianceDisplayHint { get; }
        public string SpecializationHint { get; }
        public IReadOnlyList<string> VisibleLimitations { get; }
    }

    public sealed class PlayerHiveIdentityVisibilityPolicy
    {
        public PlayerHiveIdentityVisibilityPolicy(bool publicNameVisible, bool sensitiveDataHidden, string limitation)
        {
            PublicNameVisible = publicNameVisible;
            SensitiveDataHidden = sensitiveDataHidden;
            Limitation = limitation ?? string.Empty;
        }

        public bool PublicNameVisible { get; }
        public bool SensitiveDataHidden { get; }
        public string Limitation { get; }
    }

    public sealed class PlayerHiveIdentityServerAuthorityMarker
    {
        public PlayerHiveIdentityServerAuthorityMarker(SocialServerAuthorityMarker marker, string serverOwnerHint, bool accountBypassRequested = false)
        {
            Marker = marker;
            ServerOwnerHint = serverOwnerHint ?? string.Empty;
            AccountBypassRequested = accountBypassRequested;
        }

        public SocialServerAuthorityMarker Marker { get; }
        public string ServerOwnerHint { get; }
        public bool AccountBypassRequested { get; }
    }

    public sealed class PlayerHiveIdentity
    {
        public PlayerHiveIdentity(string hiveIdentityId, string displayName, PlayerHiveIdentitySource source, string projectedPlayerReference, PlayerHivePublicProfileProjection profileProjection, PlayerHiveIdentityVisibilityPolicy visibilityPolicy, PlayerHiveIdentityServerAuthorityMarker serverAuthorityMarker, bool persistentProfileRequested = false)
        {
            HiveIdentityId = ColonyIntegrationIds.Require(hiveIdentityId);
            DisplayName = displayName ?? string.Empty;
            Source = source;
            ProjectedPlayerReference = projectedPlayerReference ?? string.Empty;
            ProfileProjection = profileProjection;
            VisibilityPolicy = visibilityPolicy;
            ServerAuthorityMarker = serverAuthorityMarker;
            PersistentProfileRequested = persistentProfileRequested;
        }

        public string HiveIdentityId { get; }
        public string DisplayName { get; }
        public PlayerHiveIdentitySource Source { get; }
        public string ProjectedPlayerReference { get; }
        public PlayerHivePublicProfileProjection ProfileProjection { get; }
        public PlayerHiveIdentityVisibilityPolicy VisibilityPolicy { get; }
        public PlayerHiveIdentityServerAuthorityMarker ServerAuthorityMarker { get; }
        public bool PersistentProfileRequested { get; }

        public PlayerHiveIdentityDiagnostics Evaluate()
        {
            var findings = new List<PlayerHiveIdentityDiagnosticCode>();
            if (Source == null || string.IsNullOrWhiteSpace(Source.HiveReference)) findings.Add(PlayerHiveIdentityDiagnosticCode.PlayerHiveIdentitySourceMissing);
            if (string.IsNullOrWhiteSpace(ProjectedPlayerReference) || (ServerAuthorityMarker != null && ServerAuthorityMarker.Marker == SocialServerAuthorityMarker.ServerAuthorityRequired)) findings.Add(PlayerHiveIdentityDiagnosticCode.PlayerReferenceNotAuthoritative);
            if (PersistentProfileRequested) findings.Add(PlayerHiveIdentityDiagnosticCode.PersistentProfileRequested);
            if (VisibilityPolicy == null || string.IsNullOrWhiteSpace(VisibilityPolicy.Limitation)) findings.Add(PlayerHiveIdentityDiagnosticCode.IdentityVisibilityMissing);
            if (ServerAuthorityMarker != null && ServerAuthorityMarker.AccountBypassRequested) findings.Add(PlayerHiveIdentityDiagnosticCode.ServerAccountBypassRequested);
            return new PlayerHiveIdentityDiagnostics(findings);
        }
    }

    public sealed class PlayerHiveIdentityDiagnostics
    {
        public PlayerHiveIdentityDiagnostics(IReadOnlyList<PlayerHiveIdentityDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlayerHiveIdentityDiagnosticCode>(); }
        public IReadOnlyList<PlayerHiveIdentityDiagnosticCode> Findings { get; }
        public bool Contains(PlayerHiveIdentityDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum InvestmentAxisType { TimeInvestment, EffortInvestment, EconomicInvestmentProjection, PreparationQuality, RecoveryCapacity, SocialContributionProjection }
    public enum PlayerInvestmentDiagnosticCode { InvestmentAxisMissing, MonetizationRuntimeRequested, PayToWinRiskUnclassified, CompetitiveRewardRequested, ServerAuthorityRequiredForInvestment }

    public sealed class InvestmentAxis
    {
        public InvestmentAxis(InvestmentAxisType axisType, double projectedValue, string evidence, string limitation, SocialMmoProductPillar pillar)
        {
            AxisType = axisType;
            ProjectedValue = ColonyIntegrationIds.Clamp01(projectedValue);
            Evidence = evidence ?? string.Empty;
            Limitation = limitation ?? string.Empty;
            Pillar = pillar;
        }

        public InvestmentAxisType AxisType { get; }
        public double ProjectedValue { get; }
        public string Evidence { get; }
        public string Limitation { get; }
        public SocialMmoProductPillar Pillar { get; }
    }

    public sealed class InvestmentProjectionImpact
    {
        public InvestmentProjectionImpact(string impactId, string targetDomain, string projectedEffect, string limitation)
        {
            ImpactId = impactId ?? string.Empty;
            TargetDomain = targetDomain ?? string.Empty;
            ProjectedEffect = projectedEffect ?? string.Empty;
            Limitation = limitation ?? string.Empty;
        }

        public string ImpactId { get; }
        public string TargetDomain { get; }
        public string ProjectedEffect { get; }
        public string Limitation { get; }
    }

    public sealed class InvestmentBalanceRisk
    {
        public InvestmentBalanceRisk(string riskId, bool payToWinUnclassified = false, bool competitiveRewardRequested = false)
        {
            RiskId = riskId ?? string.Empty;
            PayToWinUnclassified = payToWinUnclassified;
            CompetitiveRewardRequested = competitiveRewardRequested;
        }

        public string RiskId { get; }
        public bool PayToWinUnclassified { get; }
        public bool CompetitiveRewardRequested { get; }
    }

    public sealed class InvestmentServerAuthorityRequirement
    {
        public InvestmentServerAuthorityRequirement(string requirementId, string topic) { RequirementId = requirementId ?? string.Empty; Topic = topic ?? string.Empty; }
        public string RequirementId { get; }
        public string Topic { get; }
    }

    public sealed class PlayerInvestmentProfile
    {
        public PlayerInvestmentProfile(string profileId, string hiveIdentityId, IReadOnlyList<InvestmentAxis> axes, IReadOnlyList<InvestmentProjectionImpact> projectedImpacts, IReadOnlyList<InvestmentBalanceRisk> balanceRisks, IReadOnlyList<InvestmentServerAuthorityRequirement> serverAuthorityRequirements, bool monetizationRuntimeRequested = false)
        {
            ProfileId = ColonyIntegrationIds.Require(profileId);
            HiveIdentityId = hiveIdentityId ?? string.Empty;
            Axes = axes ?? Array.Empty<InvestmentAxis>();
            ProjectedImpacts = projectedImpacts ?? Array.Empty<InvestmentProjectionImpact>();
            BalanceRisks = balanceRisks ?? Array.Empty<InvestmentBalanceRisk>();
            ServerAuthorityRequirements = serverAuthorityRequirements ?? Array.Empty<InvestmentServerAuthorityRequirement>();
            MonetizationRuntimeRequested = monetizationRuntimeRequested;
        }

        public string ProfileId { get; }
        public string HiveIdentityId { get; }
        public IReadOnlyList<InvestmentAxis> Axes { get; }
        public IReadOnlyList<InvestmentProjectionImpact> ProjectedImpacts { get; }
        public IReadOnlyList<InvestmentBalanceRisk> BalanceRisks { get; }
        public IReadOnlyList<InvestmentServerAuthorityRequirement> ServerAuthorityRequirements { get; }
        public bool MonetizationRuntimeRequested { get; }

        public PlayerInvestmentProfileDiagnostics Evaluate()
        {
            var findings = new List<PlayerInvestmentDiagnosticCode>();
            if (Axes.Count == 0) findings.Add(PlayerInvestmentDiagnosticCode.InvestmentAxisMissing);
            if (MonetizationRuntimeRequested) findings.Add(PlayerInvestmentDiagnosticCode.MonetizationRuntimeRequested);
            if (BalanceRisks.Any(r => r.PayToWinUnclassified)) findings.Add(PlayerInvestmentDiagnosticCode.PayToWinRiskUnclassified);
            if (BalanceRisks.Any(r => r.CompetitiveRewardRequested)) findings.Add(PlayerInvestmentDiagnosticCode.CompetitiveRewardRequested);
            if (ServerAuthorityRequirements.Count > 0 || Axes.Any(a => a.AxisType == InvestmentAxisType.EconomicInvestmentProjection)) findings.Add(PlayerInvestmentDiagnosticCode.ServerAuthorityRequiredForInvestment);
            return new PlayerInvestmentProfileDiagnostics(findings);
        }
    }

    public sealed class PlayerInvestmentProfileDiagnostics
    {
        public PlayerInvestmentProfileDiagnostics(IReadOnlyList<PlayerInvestmentDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlayerInvestmentDiagnosticCode>(); }
        public IReadOnlyList<PlayerInvestmentDiagnosticCode> Findings { get; }
        public bool Contains(PlayerInvestmentDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum HiveProgressionDivergenceAxis { Population, Construction, Logistics, Defense, RegionalExpansion, MilitaryPreparation, Resilience, AllianceContribution, TradePotential }
    public enum AsymmetricHiveProgressionDiagnosticCode { AsymmetricAxisMissing, CompetitiveRankingRequested, ProgressionBalanceRiskMissing, ServerAuthorityRequiredForComparison, HiveComparisonSourceMissing }

    public sealed class HiveSpecializationProjection
    {
        public HiveSpecializationProjection(string specializationId, string socialValue) { SpecializationId = specializationId ?? string.Empty; SocialValue = socialValue ?? string.Empty; }
        public string SpecializationId { get; }
        public string SocialValue { get; }
    }

    public sealed class HiveBalanceWarning
    {
        public HiveBalanceWarning(string warningId, string description) { WarningId = warningId ?? string.Empty; Description = description ?? string.Empty; }
        public string WarningId { get; }
        public string Description { get; }
    }

    public sealed class AsymmetricHiveProgressionProfile
    {
        public AsymmetricHiveProgressionProfile(string profileId, string hiveIdentityId, IReadOnlyList<HiveProgressionDivergenceAxis> divergenceAxes, HiveSpecializationProjection specializationProjection, IReadOnlyList<HiveBalanceWarning> balanceWarnings, IReadOnlyList<string> serverAuthorityMarkers)
        {
            ProfileId = ColonyIntegrationIds.Require(profileId);
            HiveIdentityId = hiveIdentityId ?? string.Empty;
            DivergenceAxes = divergenceAxes ?? Array.Empty<HiveProgressionDivergenceAxis>();
            SpecializationProjection = specializationProjection;
            BalanceWarnings = balanceWarnings ?? Array.Empty<HiveBalanceWarning>();
            ServerAuthorityMarkers = serverAuthorityMarkers ?? Array.Empty<string>();
        }

        public string ProfileId { get; }
        public string HiveIdentityId { get; }
        public IReadOnlyList<HiveProgressionDivergenceAxis> DivergenceAxes { get; }
        public HiveSpecializationProjection SpecializationProjection { get; }
        public IReadOnlyList<HiveBalanceWarning> BalanceWarnings { get; }
        public IReadOnlyList<string> ServerAuthorityMarkers { get; }
    }

    public sealed class AsymmetricProgressionComparison
    {
        public AsymmetricProgressionComparison(string comparisonId, IReadOnlyList<AsymmetricHiveProgressionProfile> hiveProfiles, IReadOnlyList<string> differences, IReadOnlyList<HiveBalanceWarning> warnings, IReadOnlyList<string> limitations, bool competitiveRankingRequested = false)
        {
            ComparisonId = ColonyIntegrationIds.Require(comparisonId);
            HiveProfiles = hiveProfiles ?? Array.Empty<AsymmetricHiveProgressionProfile>();
            Differences = differences ?? Array.Empty<string>();
            Warnings = warnings ?? Array.Empty<HiveBalanceWarning>();
            Limitations = limitations ?? Array.Empty<string>();
            CompetitiveRankingRequested = competitiveRankingRequested;
        }

        public string ComparisonId { get; }
        public IReadOnlyList<AsymmetricHiveProgressionProfile> HiveProfiles { get; }
        public IReadOnlyList<string> Differences { get; }
        public IReadOnlyList<HiveBalanceWarning> Warnings { get; }
        public IReadOnlyList<string> Limitations { get; }
        public bool CompetitiveRankingRequested { get; }

        public AsymmetricHiveProgressionDiagnostics Evaluate()
        {
            var findings = new List<AsymmetricHiveProgressionDiagnosticCode>();
            if (HiveProfiles.Any(p => p.DivergenceAxes.Count == 0)) findings.Add(AsymmetricHiveProgressionDiagnosticCode.AsymmetricAxisMissing);
            if (CompetitiveRankingRequested) findings.Add(AsymmetricHiveProgressionDiagnosticCode.CompetitiveRankingRequested);
            if (Warnings.Count == 0 || HiveProfiles.Any(p => p.BalanceWarnings.Count == 0)) findings.Add(AsymmetricHiveProgressionDiagnosticCode.ProgressionBalanceRiskMissing);
            if (HiveProfiles.Any(p => p.ServerAuthorityMarkers.Count > 0)) findings.Add(AsymmetricHiveProgressionDiagnosticCode.ServerAuthorityRequiredForComparison);
            if (HiveProfiles.Count == 0 || HiveProfiles.Any(p => string.IsNullOrWhiteSpace(p.HiveIdentityId))) findings.Add(AsymmetricHiveProgressionDiagnosticCode.HiveComparisonSourceMissing);
            return new AsymmetricHiveProgressionDiagnostics(findings);
        }
    }

    public sealed class AsymmetricHiveProgressionDiagnostics
    {
        public AsymmetricHiveProgressionDiagnostics(IReadOnlyList<AsymmetricHiveProgressionDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AsymmetricHiveProgressionDiagnosticCode>(); }
        public IReadOnlyList<AsymmetricHiveProgressionDiagnosticCode> Findings { get; }
        public bool Contains(AsymmetricHiveProgressionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum PlaystylePostureType { Peaceful, Defensive, Expansionist, Militant, Mixed, Unclassified }
    public enum PlayerPlaystylePostureDiagnosticCode { PlaystyleSignalMissing, PostureVerdictFinalClaimed, PlayerSanctionRequested, DiplomacyRuntimeRequested, PvpActivationRequested }

    public sealed class PlaystylePostureSignal
    {
        public PlaystylePostureSignal(string signalId, string source, double value, double weight, string limitation)
        {
            SignalId = signalId ?? string.Empty;
            Source = source ?? string.Empty;
            Value = ColonyIntegrationIds.Clamp01(value);
            Weight = ColonyIntegrationIds.Clamp01(weight);
            Limitation = limitation ?? string.Empty;
        }

        public string SignalId { get; }
        public string Source { get; }
        public double Value { get; }
        public double Weight { get; }
        public string Limitation { get; }
    }

    public sealed class PlaystylePostureConfidence
    {
        public PlaystylePostureConfidence(double value, string limitation) { Value = ColonyIntegrationIds.Clamp01(value); Limitation = limitation ?? string.Empty; }
        public double Value { get; }
        public string Limitation { get; }
    }

    public sealed class PlaystylePostureConsequenceProjection
    {
        public PlaystylePostureConsequenceProjection(string consequenceId, string socialMeaning, string limitation) { ConsequenceId = consequenceId ?? string.Empty; SocialMeaning = socialMeaning ?? string.Empty; Limitation = limitation ?? string.Empty; }
        public string ConsequenceId { get; }
        public string SocialMeaning { get; }
        public string Limitation { get; }
    }

    public sealed class PlaystylePostureLimitation
    {
        public PlaystylePostureLimitation(string limitationId, string description) { LimitationId = limitationId ?? string.Empty; Description = description ?? string.Empty; }
        public string LimitationId { get; }
        public string Description { get; }
    }

    public sealed class PlayerPlaystylePosture
    {
        public PlayerPlaystylePosture(string postureId, string hiveIdentityId, PlaystylePostureType postureType, PlaystylePostureConfidence confidence, IReadOnlyList<PlaystylePostureSignal> signals, IReadOnlyList<PlaystylePostureConsequenceProjection> consequences, IReadOnlyList<PlaystylePostureLimitation> limitations, bool finalVerdictClaimed = false, bool playerSanctionRequested = false, bool diplomacyRuntimeRequested = false, bool pvpActivationRequested = false)
        {
            PostureId = ColonyIntegrationIds.Require(postureId);
            HiveIdentityId = hiveIdentityId ?? string.Empty;
            PostureType = postureType;
            Confidence = confidence;
            Signals = signals ?? Array.Empty<PlaystylePostureSignal>();
            Consequences = consequences ?? Array.Empty<PlaystylePostureConsequenceProjection>();
            Limitations = limitations ?? Array.Empty<PlaystylePostureLimitation>();
            FinalVerdictClaimed = finalVerdictClaimed;
            PlayerSanctionRequested = playerSanctionRequested;
            DiplomacyRuntimeRequested = diplomacyRuntimeRequested;
            PvpActivationRequested = pvpActivationRequested;
        }

        public string PostureId { get; }
        public string HiveIdentityId { get; }
        public PlaystylePostureType PostureType { get; }
        public PlaystylePostureConfidence Confidence { get; }
        public IReadOnlyList<PlaystylePostureSignal> Signals { get; }
        public IReadOnlyList<PlaystylePostureConsequenceProjection> Consequences { get; }
        public IReadOnlyList<PlaystylePostureLimitation> Limitations { get; }
        public bool FinalVerdictClaimed { get; }
        public bool PlayerSanctionRequested { get; }
        public bool DiplomacyRuntimeRequested { get; }
        public bool PvpActivationRequested { get; }
        public PlayerPlaystylePostureDiagnostics Evaluate()
        {
            var findings = new List<PlayerPlaystylePostureDiagnosticCode>();
            if (Signals.Count == 0 || Signals.Any(s => string.IsNullOrWhiteSpace(s.Source))) findings.Add(PlayerPlaystylePostureDiagnosticCode.PlaystyleSignalMissing);
            if (FinalVerdictClaimed) findings.Add(PlayerPlaystylePostureDiagnosticCode.PostureVerdictFinalClaimed);
            if (PlayerSanctionRequested) findings.Add(PlayerPlaystylePostureDiagnosticCode.PlayerSanctionRequested);
            if (DiplomacyRuntimeRequested) findings.Add(PlayerPlaystylePostureDiagnosticCode.DiplomacyRuntimeRequested);
            if (PvpActivationRequested) findings.Add(PlayerPlaystylePostureDiagnosticCode.PvpActivationRequested);
            return new PlayerPlaystylePostureDiagnostics(findings);
        }
    }

    public sealed class PlayerPlaystylePostureDiagnostics
    {
        public PlayerPlaystylePostureDiagnostics(IReadOnlyList<PlayerPlaystylePostureDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PlayerPlaystylePostureDiagnosticCode>(); }
        public IReadOnlyList<PlayerPlaystylePostureDiagnosticCode> Findings { get; }
        public bool Contains(PlayerPlaystylePostureDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AllianceMembershipStatus { ProjectedMember, Candidate, Invited, NotAffiliated, ServerAuthorityRequired, OutOfScope }
    public enum AllianceMembershipDiagnosticCode { AllianceMembershipSourceMissing, AllianceRoleFinalClaimed, AlliancePermissionRuntimeRequested, AllianceBankRuntimeRequested, AllianceServerAuthorityRequired }

    public sealed class AllianceMembershipRoleHint
    {
        public AllianceMembershipRoleHint(string roleId, string label, bool finalRoleClaimed = false) { RoleId = roleId ?? string.Empty; Label = label ?? string.Empty; FinalRoleClaimed = finalRoleClaimed; }
        public string RoleId { get; }
        public string Label { get; }
        public bool FinalRoleClaimed { get; }
    }

    public sealed class AllianceContributionSignal
    {
        public AllianceContributionSignal(string signalId, string source, string projectedContribution, string limitation) { SignalId = signalId ?? string.Empty; Source = source ?? string.Empty; ProjectedContribution = projectedContribution ?? string.Empty; Limitation = limitation ?? string.Empty; }
        public string SignalId { get; }
        public string Source { get; }
        public string ProjectedContribution { get; }
        public string Limitation { get; }
    }

    public sealed class AlliancePermissionBoundary
    {
        public AlliancePermissionBoundary(bool runtimePermissionRequested = false, bool bankRuntimeRequested = false) { RuntimePermissionRequested = runtimePermissionRequested; BankRuntimeRequested = bankRuntimeRequested; }
        public bool RuntimePermissionRequested { get; }
        public bool BankRuntimeRequested { get; }
    }

    public sealed class AllianceServerAuthorityMarker
    {
        public AllianceServerAuthorityMarker(bool serverAuthorityRequired, string topic) { ServerAuthorityRequired = serverAuthorityRequired; Topic = topic ?? string.Empty; }
        public bool ServerAuthorityRequired { get; }
        public string Topic { get; }
    }

    public sealed class AllianceMembershipProjection
    {
        public AllianceMembershipProjection(string projectionId, string hiveIdentityId, string allianceHint, AllianceMembershipRoleHint roleHint, IReadOnlyList<AllianceContributionSignal> contributionSignals, AlliancePermissionBoundary permissionBoundary, AllianceServerAuthorityMarker serverAuthorityMarker, AllianceMembershipStatus status)
        {
            ProjectionId = ColonyIntegrationIds.Require(projectionId);
            HiveIdentityId = hiveIdentityId ?? string.Empty;
            AllianceHint = allianceHint ?? string.Empty;
            RoleHint = roleHint;
            ContributionSignals = contributionSignals ?? Array.Empty<AllianceContributionSignal>();
            PermissionBoundary = permissionBoundary;
            ServerAuthorityMarker = serverAuthorityMarker;
            Status = status;
        }

        public string ProjectionId { get; }
        public string HiveIdentityId { get; }
        public string AllianceHint { get; }
        public AllianceMembershipRoleHint RoleHint { get; }
        public IReadOnlyList<AllianceContributionSignal> ContributionSignals { get; }
        public AlliancePermissionBoundary PermissionBoundary { get; }
        public AllianceServerAuthorityMarker ServerAuthorityMarker { get; }
        public AllianceMembershipStatus Status { get; }
        public AllianceMembershipDiagnostics Evaluate()
        {
            var findings = new List<AllianceMembershipDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(HiveIdentityId)) findings.Add(AllianceMembershipDiagnosticCode.AllianceMembershipSourceMissing);
            if (RoleHint != null && RoleHint.FinalRoleClaimed) findings.Add(AllianceMembershipDiagnosticCode.AllianceRoleFinalClaimed);
            if (PermissionBoundary != null && PermissionBoundary.RuntimePermissionRequested) findings.Add(AllianceMembershipDiagnosticCode.AlliancePermissionRuntimeRequested);
            if (PermissionBoundary != null && PermissionBoundary.BankRuntimeRequested) findings.Add(AllianceMembershipDiagnosticCode.AllianceBankRuntimeRequested);
            if (ServerAuthorityMarker != null && ServerAuthorityMarker.ServerAuthorityRequired) findings.Add(AllianceMembershipDiagnosticCode.AllianceServerAuthorityRequired);
            return new AllianceMembershipDiagnostics(findings);
        }
    }

    public sealed class AllianceMembershipDiagnostics
    {
        public AllianceMembershipDiagnostics(IReadOnlyList<AllianceMembershipDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AllianceMembershipDiagnosticCode>(); }
        public IReadOnlyList<AllianceMembershipDiagnosticCode> Findings { get; }
        public bool Contains(AllianceMembershipDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum DiplomacyRelationshipIntent { Neutral, Friendly, Protective, TradeInterested, Rival, Threatened, WarCandidate, OutOfScope }
    public enum DiplomacyRelationshipDiagnosticCode { DiplomacySourceMissing, DiplomacyTargetMissing, DiplomacyTreatyRuntimeRequested, WarDeclarationRuntimeRequested, DiplomacyServerAuthorityRequired }

    public sealed class DiplomacyTrustSignal
    {
        public DiplomacyTrustSignal(double value, string source) { Value = ColonyIntegrationIds.Clamp01(value); Source = source ?? string.Empty; }
        public double Value { get; }
        public string Source { get; }
    }

    public sealed class DiplomacyConflictRisk
    {
        public DiplomacyConflictRisk(string riskId, string reason, int severity, string limitation) { RiskId = riskId ?? string.Empty; Reason = reason ?? string.Empty; Severity = Math.Max(0, severity); Limitation = limitation ?? string.Empty; }
        public string RiskId { get; }
        public string Reason { get; }
        public int Severity { get; }
        public string Limitation { get; }
    }

    public sealed class DiplomacyServerAuthorityMarker
    {
        public DiplomacyServerAuthorityMarker(bool required, string topic) { Required = required; Topic = topic ?? string.Empty; }
        public bool Required { get; }
        public string Topic { get; }
    }

    public sealed class DiplomacyRelationshipProjection
    {
        public DiplomacyRelationshipProjection(string relationshipId, string sourceHiveOrAlliance, string targetHiveOrAlliance, DiplomacyRelationshipIntent intent, DiplomacyTrustSignal trustSignal, DiplomacyConflictRisk conflictRisk, DiplomacyServerAuthorityMarker serverAuthorityMarker, bool treatyRuntimeRequested = false, bool warDeclarationRuntimeRequested = false)
        {
            RelationshipId = ColonyIntegrationIds.Require(relationshipId);
            SourceHiveOrAlliance = sourceHiveOrAlliance ?? string.Empty;
            TargetHiveOrAlliance = targetHiveOrAlliance ?? string.Empty;
            Intent = intent;
            TrustSignal = trustSignal;
            ConflictRisk = conflictRisk;
            ServerAuthorityMarker = serverAuthorityMarker;
            TreatyRuntimeRequested = treatyRuntimeRequested;
            WarDeclarationRuntimeRequested = warDeclarationRuntimeRequested;
        }

        public string RelationshipId { get; }
        public string SourceHiveOrAlliance { get; }
        public string TargetHiveOrAlliance { get; }
        public DiplomacyRelationshipIntent Intent { get; }
        public DiplomacyTrustSignal TrustSignal { get; }
        public DiplomacyConflictRisk ConflictRisk { get; }
        public DiplomacyServerAuthorityMarker ServerAuthorityMarker { get; }
        public bool TreatyRuntimeRequested { get; }
        public bool WarDeclarationRuntimeRequested { get; }
        public DiplomacyRelationshipDiagnostics Evaluate()
        {
            var findings = new List<DiplomacyRelationshipDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(SourceHiveOrAlliance)) findings.Add(DiplomacyRelationshipDiagnosticCode.DiplomacySourceMissing);
            if (string.IsNullOrWhiteSpace(TargetHiveOrAlliance)) findings.Add(DiplomacyRelationshipDiagnosticCode.DiplomacyTargetMissing);
            if (TreatyRuntimeRequested) findings.Add(DiplomacyRelationshipDiagnosticCode.DiplomacyTreatyRuntimeRequested);
            if (WarDeclarationRuntimeRequested) findings.Add(DiplomacyRelationshipDiagnosticCode.WarDeclarationRuntimeRequested);
            if (ServerAuthorityMarker != null && ServerAuthorityMarker.Required) findings.Add(DiplomacyRelationshipDiagnosticCode.DiplomacyServerAuthorityRequired);
            return new DiplomacyRelationshipDiagnostics(findings);
        }
    }

    public sealed class DiplomacyRelationshipDiagnostics
    {
        public DiplomacyRelationshipDiagnostics(IReadOnlyList<DiplomacyRelationshipDiagnosticCode> findings) { Findings = findings ?? Array.Empty<DiplomacyRelationshipDiagnosticCode>(); }
        public IReadOnlyList<DiplomacyRelationshipDiagnosticCode> Findings { get; }
        public bool Contains(DiplomacyRelationshipDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum CommunicationChannelType { Global, Server, Alliance, Leadership, Private, Group, System, Notification, EventLog }
    public enum SocialCommunicationChannelDiagnosticCode { CommunicationChannelMissing, ChatRuntimeRequested, MessagePersistenceRequested, ModerationMissing, CommunicationServerAuthorityRequired }

    public sealed class CommunicationVisibilityRule
    {
        public CommunicationVisibilityRule(string audience, bool privateVisibility, string limitation)
        {
            Audience = audience ?? string.Empty;
            PrivateVisibility = privateVisibility;
            Limitation = limitation ?? string.Empty;
        }

        public string Audience { get; }
        public bool PrivateVisibility { get; }
        public string Limitation { get; }
    }

    public sealed class CommunicationModerationRequirement
    {
        public CommunicationModerationRequirement(bool required, string reason, string owner)
        {
            Required = required;
            Reason = reason ?? string.Empty;
            Owner = owner ?? string.Empty;
        }

        public bool Required { get; }
        public string Reason { get; }
        public string Owner { get; }
    }

    public sealed class CommunicationServerAuthorityMarker
    {
        public CommunicationServerAuthorityMarker(bool required, string topic)
        {
            Required = required;
            Topic = topic ?? string.Empty;
        }

        public bool Required { get; }
        public string Topic { get; }
    }

    public sealed class SocialCommunicationChannelProjection
    {
        public SocialCommunicationChannelProjection(string channelId, CommunicationChannelType channelType, CommunicationVisibilityRule visibilityRule, CommunicationModerationRequirement moderationRequirement, CommunicationServerAuthorityMarker serverAuthorityMarker, bool chatRuntimeRequested = false, bool messagePersistenceRequested = false)
        {
            ChannelId = channelId ?? string.Empty;
            ChannelType = channelType;
            VisibilityRule = visibilityRule;
            ModerationRequirement = moderationRequirement;
            ServerAuthorityMarker = serverAuthorityMarker;
            ChatRuntimeRequested = chatRuntimeRequested;
            MessagePersistenceRequested = messagePersistenceRequested;
        }

        public string ChannelId { get; }
        public CommunicationChannelType ChannelType { get; }
        public CommunicationVisibilityRule VisibilityRule { get; }
        public CommunicationModerationRequirement ModerationRequirement { get; }
        public CommunicationServerAuthorityMarker ServerAuthorityMarker { get; }
        public bool ChatRuntimeRequested { get; }
        public bool MessagePersistenceRequested { get; }

        public SocialCommunicationChannelDiagnostics Evaluate()
        {
            var findings = new List<SocialCommunicationChannelDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(ChannelId)) findings.Add(SocialCommunicationChannelDiagnosticCode.CommunicationChannelMissing);
            if (ChatRuntimeRequested) findings.Add(SocialCommunicationChannelDiagnosticCode.ChatRuntimeRequested);
            if (MessagePersistenceRequested) findings.Add(SocialCommunicationChannelDiagnosticCode.MessagePersistenceRequested);
            if (ModerationRequirement == null || !ModerationRequirement.Required || string.IsNullOrWhiteSpace(ModerationRequirement.Owner)) findings.Add(SocialCommunicationChannelDiagnosticCode.ModerationMissing);
            if (ServerAuthorityMarker != null && ServerAuthorityMarker.Required) findings.Add(SocialCommunicationChannelDiagnosticCode.CommunicationServerAuthorityRequired);
            return new SocialCommunicationChannelDiagnostics(findings);
        }
    }

    public sealed class SocialCommunicationChannelDiagnostics
    {
        public SocialCommunicationChannelDiagnostics(IReadOnlyList<SocialCommunicationChannelDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SocialCommunicationChannelDiagnosticCode>(); }
        public IReadOnlyList<SocialCommunicationChannelDiagnosticCode> Findings { get; }
        public bool Contains(SocialCommunicationChannelDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ArmyTrainingLifecycleStage { RecruitmentPlanning, TrainingPlanning, EquipmentPlanning, UnitOrganizationPlanning, MaintenancePlanning, ReadinessProjection, MobilizationProjection }
    public enum ArmyTrainingDiagnosticCode { ArmyBoundarySourceMissing, ArmySocialUseCaseMissing, ArmyRuntimeRequested, ParallelCombatSystemRequested, ServerAuthorityForArmyMissing }

    public sealed class ArmyTrainingSocialUseCase
    {
        public ArmyTrainingSocialUseCase(string useCaseId, string playerFacingValue, string serverBoundary)
        {
            UseCaseId = useCaseId ?? string.Empty;
            PlayerFacingValue = playerFacingValue ?? string.Empty;
            ServerBoundary = serverBoundary ?? string.Empty;
        }

        public string UseCaseId { get; }
        public string PlayerFacingValue { get; }
        public string ServerBoundary { get; }
    }

    public sealed class ArmyTrainingForbiddenRuntimeAction
    {
        public ArmyTrainingForbiddenRuntimeAction(string actionId, string reason)
        {
            ActionId = actionId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string ActionId { get; }
        public string Reason { get; }
    }

    public sealed class ArmyTrainingDomainResponsibility
    {
        public ArmyTrainingDomainResponsibility(ArmyTrainingLifecycleStage stage, string clientProjection, string serverResponsibility)
        {
            Stage = stage;
            ClientProjection = clientProjection ?? string.Empty;
            ServerResponsibility = serverResponsibility ?? string.Empty;
        }

        public ArmyTrainingLifecycleStage Stage { get; }
        public string ClientProjection { get; }
        public string ServerResponsibility { get; }
    }

    public sealed class ArmyTrainingDomainBoundary
    {
        public ArmyTrainingDomainBoundary(string boundaryId, IReadOnlyList<ArmyTrainingDomainResponsibility> responsibilities, IReadOnlyList<ArmyTrainingSocialUseCase> socialUseCases, IReadOnlyList<ArmyTrainingForbiddenRuntimeAction> forbiddenRuntimeActions, bool armyRuntimeRequested = false, bool parallelCombatSystemRequested = false, bool serverAuthorityMissing = false)
        {
            BoundaryId = boundaryId ?? string.Empty;
            Responsibilities = responsibilities ?? Array.Empty<ArmyTrainingDomainResponsibility>();
            SocialUseCases = socialUseCases ?? Array.Empty<ArmyTrainingSocialUseCase>();
            ForbiddenRuntimeActions = forbiddenRuntimeActions ?? Array.Empty<ArmyTrainingForbiddenRuntimeAction>();
            ArmyRuntimeRequested = armyRuntimeRequested;
            ParallelCombatSystemRequested = parallelCombatSystemRequested;
            ServerAuthorityMissing = serverAuthorityMissing;
        }

        public string BoundaryId { get; }
        public IReadOnlyList<ArmyTrainingDomainResponsibility> Responsibilities { get; }
        public IReadOnlyList<ArmyTrainingSocialUseCase> SocialUseCases { get; }
        public IReadOnlyList<ArmyTrainingForbiddenRuntimeAction> ForbiddenRuntimeActions { get; }
        public bool ArmyRuntimeRequested { get; }
        public bool ParallelCombatSystemRequested { get; }
        public bool ServerAuthorityMissing { get; }

        public ArmyTrainingDomainDiagnostics Evaluate()
        {
            var findings = new List<ArmyTrainingDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(BoundaryId) || Responsibilities.Count == 0) findings.Add(ArmyTrainingDiagnosticCode.ArmyBoundarySourceMissing);
            if (SocialUseCases.Count == 0 || SocialUseCases.Any(u => string.IsNullOrWhiteSpace(u.PlayerFacingValue))) findings.Add(ArmyTrainingDiagnosticCode.ArmySocialUseCaseMissing);
            if (ArmyRuntimeRequested) findings.Add(ArmyTrainingDiagnosticCode.ArmyRuntimeRequested);
            if (ParallelCombatSystemRequested) findings.Add(ArmyTrainingDiagnosticCode.ParallelCombatSystemRequested);
            if (ServerAuthorityMissing) findings.Add(ArmyTrainingDiagnosticCode.ServerAuthorityForArmyMissing);
            return new ArmyTrainingDomainDiagnostics(findings);
        }
    }

    public sealed class ArmyTrainingDomainDiagnostics
    {
        public ArmyTrainingDomainDiagnostics(IReadOnlyList<ArmyTrainingDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ArmyTrainingDiagnosticCode>(); }
        public IReadOnlyList<ArmyTrainingDiagnosticCode> Findings { get; }
        public bool Contains(ArmyTrainingDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum PvpWarOwnershipTopicType { ArmyPersistence, PvpCombat, WarDeclaration, Rally, Siege, Raid, Losses, Rewards, ResourcesWonLost, Territory, Rankings, Cooldowns, Matchmaking, Monetization, Sanctions, Protections }
    public enum WarAuthorityDecisionClass { ServerAuthoritative, ClientProjectionOnly, ForbiddenLocalMutation, ServerSpecRequired }
    public enum PvpWarServerAuthorityDiagnosticCode { PvpAuthorityTopicMissing, LocalWarResolutionRequested, LocalRewardMutationRequested, LocalRankingRequested, MatchmakingRuntimeRequested, MonetizationRuntimeRequested, ServerSpecRequired }

    public sealed class PvpWarOwnershipTopic
    {
        public PvpWarOwnershipTopic(PvpWarOwnershipTopicType topicType, WarAuthorityDecisionClass decisionClass, string owner)
        {
            TopicType = topicType;
            DecisionClass = decisionClass;
            Owner = owner ?? string.Empty;
        }

        public PvpWarOwnershipTopicType TopicType { get; }
        public WarAuthorityDecisionClass DecisionClass { get; }
        public string Owner { get; }
    }

    public sealed class ClientWarProjectionLimit
    {
        public ClientWarProjectionLimit(string limitId, string description)
        {
            LimitId = limitId ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string LimitId { get; }
        public string Description { get; }
    }

    public sealed class PvpWarServerEscalationExport
    {
        public PvpWarServerEscalationExport(string exportId, string requiredServerSpec, IReadOnlyList<PvpWarOwnershipTopicType> topics)
        {
            ExportId = exportId ?? string.Empty;
            RequiredServerSpec = requiredServerSpec ?? string.Empty;
            Topics = topics ?? Array.Empty<PvpWarOwnershipTopicType>();
        }

        public string ExportId { get; }
        public string RequiredServerSpec { get; }
        public IReadOnlyList<PvpWarOwnershipTopicType> Topics { get; }
    }

    public sealed class PvpWarServerAuthorityBoundary
    {
        public PvpWarServerAuthorityBoundary(string boundaryId, IReadOnlyList<PvpWarOwnershipTopic> ownershipTopics, IReadOnlyList<ClientWarProjectionLimit> clientProjectionLimits, PvpWarServerEscalationExport escalationExport, bool localWarResolutionRequested = false, bool localRewardMutationRequested = false, bool localRankingRequested = false, bool matchmakingRuntimeRequested = false, bool monetizationRuntimeRequested = false)
        {
            BoundaryId = boundaryId ?? string.Empty;
            OwnershipTopics = ownershipTopics ?? Array.Empty<PvpWarOwnershipTopic>();
            ClientProjectionLimits = clientProjectionLimits ?? Array.Empty<ClientWarProjectionLimit>();
            EscalationExport = escalationExport;
            LocalWarResolutionRequested = localWarResolutionRequested;
            LocalRewardMutationRequested = localRewardMutationRequested;
            LocalRankingRequested = localRankingRequested;
            MatchmakingRuntimeRequested = matchmakingRuntimeRequested;
            MonetizationRuntimeRequested = monetizationRuntimeRequested;
        }

        public string BoundaryId { get; }
        public IReadOnlyList<PvpWarOwnershipTopic> OwnershipTopics { get; }
        public IReadOnlyList<ClientWarProjectionLimit> ClientProjectionLimits { get; }
        public PvpWarServerEscalationExport EscalationExport { get; }
        public bool LocalWarResolutionRequested { get; }
        public bool LocalRewardMutationRequested { get; }
        public bool LocalRankingRequested { get; }
        public bool MatchmakingRuntimeRequested { get; }
        public bool MonetizationRuntimeRequested { get; }

        public PvpWarServerAuthorityDiagnostics Evaluate()
        {
            var findings = new List<PvpWarServerAuthorityDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(BoundaryId) || OwnershipTopics.Count == 0 || OwnershipTopics.Any(t => string.IsNullOrWhiteSpace(t.Owner))) findings.Add(PvpWarServerAuthorityDiagnosticCode.PvpAuthorityTopicMissing);
            if (LocalWarResolutionRequested) findings.Add(PvpWarServerAuthorityDiagnosticCode.LocalWarResolutionRequested);
            if (LocalRewardMutationRequested) findings.Add(PvpWarServerAuthorityDiagnosticCode.LocalRewardMutationRequested);
            if (LocalRankingRequested) findings.Add(PvpWarServerAuthorityDiagnosticCode.LocalRankingRequested);
            if (MatchmakingRuntimeRequested) findings.Add(PvpWarServerAuthorityDiagnosticCode.MatchmakingRuntimeRequested);
            if (MonetizationRuntimeRequested) findings.Add(PvpWarServerAuthorityDiagnosticCode.MonetizationRuntimeRequested);
            if (EscalationExport == null || string.IsNullOrWhiteSpace(EscalationExport.RequiredServerSpec)) findings.Add(PvpWarServerAuthorityDiagnosticCode.ServerSpecRequired);
            return new PvpWarServerAuthorityDiagnostics(findings);
        }
    }

    public sealed class PvpWarServerAuthorityDiagnostics
    {
        public PvpWarServerAuthorityDiagnostics(IReadOnlyList<PvpWarServerAuthorityDiagnosticCode> findings) { Findings = findings ?? Array.Empty<PvpWarServerAuthorityDiagnosticCode>(); }
        public IReadOnlyList<PvpWarServerAuthorityDiagnosticCode> Findings { get; }
        public bool Contains(PvpWarServerAuthorityDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum SocialMmoFoundationsVerdictType { ReadyForArchitectValidation, ReadyWithWarningsForArchitectValidation, NeedsPlannerRevision, BlockedByMissingProductPillar, BlockedBySocialFoundationGap, BlockedByServerAuthorityGap, BlockedByDemoEvidenceGap, BlockedByBee311Premature }
    public enum SocialMmoFoundationsDiagnosticCode { SocialMmoInputMissing, ProductPillarMissing, SimulationOnlyPillarRejected, AllianceFoundationGapOpen, DiplomacyFoundationGapOpen, CommunicationFoundationGapOpen, PvpAuthorityGapOpen, Bee311Premature }

    public sealed class SocialMmoFoundationsInputSet
    {
        public SocialMmoFoundationsInputSet(string identityRef, string investmentRef, string progressionRef, string playstyleRef, string allianceRef, string diplomacyRef, string communicationRef, string armyBoundaryRef, string pvpAuthorityRef)
        {
            IdentityRef = identityRef ?? string.Empty;
            InvestmentRef = investmentRef ?? string.Empty;
            ProgressionRef = progressionRef ?? string.Empty;
            PlaystyleRef = playstyleRef ?? string.Empty;
            AllianceRef = allianceRef ?? string.Empty;
            DiplomacyRef = diplomacyRef ?? string.Empty;
            CommunicationRef = communicationRef ?? string.Empty;
            ArmyBoundaryRef = armyBoundaryRef ?? string.Empty;
            PvpAuthorityRef = pvpAuthorityRef ?? string.Empty;
        }

        public string IdentityRef { get; }
        public string InvestmentRef { get; }
        public string ProgressionRef { get; }
        public string PlaystyleRef { get; }
        public string AllianceRef { get; }
        public string DiplomacyRef { get; }
        public string CommunicationRef { get; }
        public string ArmyBoundaryRef { get; }
        public string PvpAuthorityRef { get; }

        public bool HasMissingInput()
        {
            return string.IsNullOrWhiteSpace(IdentityRef)
                || string.IsNullOrWhiteSpace(InvestmentRef)
                || string.IsNullOrWhiteSpace(ProgressionRef)
                || string.IsNullOrWhiteSpace(PlaystyleRef)
                || string.IsNullOrWhiteSpace(AllianceRef)
                || string.IsNullOrWhiteSpace(DiplomacyRef)
                || string.IsNullOrWhiteSpace(CommunicationRef)
                || string.IsNullOrWhiteSpace(ArmyBoundaryRef)
                || string.IsNullOrWhiteSpace(PvpAuthorityRef);
        }
    }

    public sealed class SocialMmoProductPillarCoverage
    {
        public SocialMmoProductPillarCoverage(IReadOnlyList<SocialMmoProductPillar> pillars, bool simulationOnly, bool demoEvidencePresent)
        {
            Pillars = pillars ?? Array.Empty<SocialMmoProductPillar>();
            SimulationOnly = simulationOnly;
            DemoEvidencePresent = demoEvidencePresent;
        }

        public IReadOnlyList<SocialMmoProductPillar> Pillars { get; }
        public bool SimulationOnly { get; }
        public bool DemoEvidencePresent { get; }
        public bool Covers(SocialMmoProductPillar pillar) { return Pillars.Contains(pillar); }
    }

    public sealed class SocialMmoFoundationGap
    {
        public SocialMmoFoundationGap(string sourceBee, SocialMmoProductPillar pillar, string owner, string action)
        {
            SourceBee = sourceBee ?? string.Empty;
            Pillar = pillar;
            Owner = owner ?? string.Empty;
            Action = action ?? string.Empty;
        }

        public string SourceBee { get; }
        public SocialMmoProductPillar Pillar { get; }
        public string Owner { get; }
        public string Action { get; }
    }

    public sealed class SocialMmoFoundationsVerdict
    {
        public SocialMmoFoundationsVerdict(SocialMmoFoundationsVerdictType verdictType, IReadOnlyList<SocialMmoFoundationsDiagnosticCode> diagnostics)
        {
            VerdictType = verdictType;
            Diagnostics = diagnostics ?? Array.Empty<SocialMmoFoundationsDiagnosticCode>();
        }

        public SocialMmoFoundationsVerdictType VerdictType { get; }
        public IReadOnlyList<SocialMmoFoundationsDiagnosticCode> Diagnostics { get; }
        public bool Contains(SocialMmoFoundationsDiagnosticCode code) { return Diagnostics.Contains(code); }
    }

    public sealed class SocialMmoFoundationsExport
    {
        public SocialMmoFoundationsExport(string exportId, SocialMmoFoundationsVerdict verdict, string bee311Status, IReadOnlyList<SocialMmoFoundationGap> gaps)
        {
            ExportId = exportId ?? string.Empty;
            Verdict = verdict;
            Bee311Status = bee311Status ?? string.Empty;
            Gaps = gaps ?? Array.Empty<SocialMmoFoundationGap>();
        }

        public string ExportId { get; }
        public SocialMmoFoundationsVerdict Verdict { get; }
        public string Bee311Status { get; }
        public IReadOnlyList<SocialMmoFoundationGap> Gaps { get; }
    }

    public sealed class SocialMmoFoundationsGate
    {
        public const string Bee311BlockedStatus = "BEE-311 bloquee jusqu'a validation architecte.";

        public SocialMmoFoundationsGate(SocialMmoFoundationsInputSet inputSet, SocialMmoProductPillarCoverage pillarCoverage, IReadOnlyList<SocialMmoFoundationGap> gaps, bool serverAuthorityGapOpen = false, bool bee311Premature = false)
        {
            InputSet = inputSet;
            PillarCoverage = pillarCoverage;
            Gaps = gaps ?? Array.Empty<SocialMmoFoundationGap>();
            ServerAuthorityGapOpen = serverAuthorityGapOpen;
            Bee311Premature = bee311Premature;
        }

        public SocialMmoFoundationsInputSet InputSet { get; }
        public SocialMmoProductPillarCoverage PillarCoverage { get; }
        public IReadOnlyList<SocialMmoFoundationGap> Gaps { get; }
        public bool ServerAuthorityGapOpen { get; }
        public bool Bee311Premature { get; }

        public SocialMmoFoundationsVerdict Evaluate()
        {
            var diagnostics = BuildDiagnostics();
            return new SocialMmoFoundationsVerdict(ResolveVerdict(diagnostics), diagnostics);
        }

        public SocialMmoFoundationsExport Export(string exportId)
        {
            return new SocialMmoFoundationsExport(exportId, Evaluate(), Bee311BlockedStatus, Gaps);
        }

        private IReadOnlyList<SocialMmoFoundationsDiagnosticCode> BuildDiagnostics()
        {
            var diagnostics = new List<SocialMmoFoundationsDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput()) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.SocialMmoInputMissing);
            if (PillarCoverage == null || !PillarCoverage.Covers(SocialMmoProductPillar.SocialMmo)) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.ProductPillarMissing);
            if (PillarCoverage != null && PillarCoverage.SimulationOnly) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.SimulationOnlyPillarRejected);
            if (Gaps.Any(g => g.Pillar == SocialMmoProductPillar.Alliance)) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.AllianceFoundationGapOpen);
            if (Gaps.Any(g => g.Pillar == SocialMmoProductPillar.Diplomacy)) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.DiplomacyFoundationGapOpen);
            if (Gaps.Any(g => g.Pillar == SocialMmoProductPillar.Communication)) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.CommunicationFoundationGapOpen);
            if (ServerAuthorityGapOpen || Gaps.Any(g => g.Pillar == SocialMmoProductPillar.PvpWar)) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.PvpAuthorityGapOpen);
            if (Bee311Premature) diagnostics.Add(SocialMmoFoundationsDiagnosticCode.Bee311Premature);
            return diagnostics;
        }

        private SocialMmoFoundationsVerdictType ResolveVerdict(IReadOnlyList<SocialMmoFoundationsDiagnosticCode> diagnostics)
        {
            if (diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.Bee311Premature)) return SocialMmoFoundationsVerdictType.BlockedByBee311Premature;
            if (diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.PvpAuthorityGapOpen)) return SocialMmoFoundationsVerdictType.BlockedByServerAuthorityGap;
            if (diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.ProductPillarMissing) || diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.SimulationOnlyPillarRejected)) return SocialMmoFoundationsVerdictType.BlockedByMissingProductPillar;
            if (diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.AllianceFoundationGapOpen) || diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.DiplomacyFoundationGapOpen) || diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.CommunicationFoundationGapOpen)) return SocialMmoFoundationsVerdictType.BlockedBySocialFoundationGap;
            if (diagnostics.Contains(SocialMmoFoundationsDiagnosticCode.SocialMmoInputMissing) || (PillarCoverage != null && !PillarCoverage.DemoEvidencePresent)) return SocialMmoFoundationsVerdictType.BlockedByDemoEvidenceGap;
            return Gaps.Count == 0 ? SocialMmoFoundationsVerdictType.ReadyForArchitectValidation : SocialMmoFoundationsVerdictType.ReadyWithWarningsForArchitectValidation;
        }
    }
}
