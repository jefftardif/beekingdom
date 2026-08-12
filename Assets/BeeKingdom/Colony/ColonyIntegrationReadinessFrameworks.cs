using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum ColonyIntegrationDomain { Hive, World, Population, BeeAI, Construction, Resources, Defense, Strategy, Demo }
    public enum ColonyIntegrationDirection { ReadOnly, CommandIntent, EventObservation, SnapshotProjection, ForbiddenDirectMutation }
    public enum ColonyIntegrationDiagnosticCode { BoundaryOwnerMissing, DirectMutationDetected, ManagerReplacementRequested, UnseededRandomnessRequested, IntegrationBlockerUnexplained }

    public sealed class ColonyIntegrationOwner
    {
        public ColonyIntegrationOwner(string ownerId, ColonyIntegrationDomain domain, string truthSource)
        {
            OwnerId = ColonyIntegrationIds.Require(ownerId);
            Domain = domain;
            TruthSource = ColonyIntegrationIds.Require(truthSource);
        }

        public string OwnerId { get; }
        public ColonyIntegrationDomain Domain { get; }
        public string TruthSource { get; }
    }

    public sealed class ColonyIntegrationInterface
    {
        public ColonyIntegrationInterface(string interfaceId, ColonyIntegrationDirection direction, string surface, bool usesEngineBoundary = true)
        {
            InterfaceId = ColonyIntegrationIds.Require(interfaceId);
            Direction = direction;
            Surface = ColonyIntegrationIds.Require(surface);
            UsesEngineBoundary = usesEngineBoundary;
        }

        public string InterfaceId { get; }
        public ColonyIntegrationDirection Direction { get; }
        public string Surface { get; }
        public bool UsesEngineBoundary { get; }
    }

    public sealed class ColonyIntegrationBlocker
    {
        public ColonyIntegrationBlocker(string blockerId, string description, bool isBlocking = true, bool isExplained = true)
        {
            BlockerId = ColonyIntegrationIds.Require(blockerId);
            Description = description ?? string.Empty;
            IsBlocking = isBlocking;
            IsExplained = isExplained;
        }

        public string BlockerId { get; }
        public string Description { get; }
        public bool IsBlocking { get; }
        public bool IsExplained { get; }
    }

    public sealed class ColonyIntegrationBoundary
    {
        public ColonyIntegrationBoundary(
            ColonyIntegrationDomain sourceDomain,
            ColonyIntegrationDomain targetDomain,
            ColonyIntegrationOwner owner,
            ColonyIntegrationInterface integrationInterface,
            IReadOnlyList<ColonyIntegrationBlocker> blockers = null,
            bool managerReplacementRequested = false,
            bool unseededRandomnessRequested = false)
        {
            SourceDomain = sourceDomain;
            TargetDomain = targetDomain;
            Owner = owner;
            Interface = integrationInterface;
            Blockers = blockers ?? Array.Empty<ColonyIntegrationBlocker>();
            ManagerReplacementRequested = managerReplacementRequested;
            UnseededRandomnessRequested = unseededRandomnessRequested;
        }

        public ColonyIntegrationDomain SourceDomain { get; }
        public ColonyIntegrationDomain TargetDomain { get; }
        public ColonyIntegrationOwner Owner { get; }
        public ColonyIntegrationInterface Interface { get; }
        public IReadOnlyList<ColonyIntegrationBlocker> Blockers { get; }
        public bool ManagerReplacementRequested { get; }
        public bool UnseededRandomnessRequested { get; }
    }

    public sealed class ColonyIntegrationBoundaryMap
    {
        private readonly List<ColonyIntegrationBoundary> boundaries;

        public ColonyIntegrationBoundaryMap(IReadOnlyList<ColonyIntegrationBoundary> boundaries)
        {
            this.boundaries = (boundaries ?? Array.Empty<ColonyIntegrationBoundary>())
                .OrderBy(b => b.SourceDomain)
                .ThenBy(b => b.TargetDomain)
                .ToList();
        }

        public IReadOnlyList<ColonyIntegrationBoundary> Boundaries => boundaries;

        public ColonyIntegrationDiagnostics Evaluate()
        {
            List<ColonyIntegrationDiagnostic> findings = new List<ColonyIntegrationDiagnostic>();
            foreach (ColonyIntegrationBoundary boundary in boundaries)
            {
                if (boundary.Owner == null)
                {
                    findings.Add(new ColonyIntegrationDiagnostic(ColonyIntegrationDiagnosticCode.BoundaryOwnerMissing, boundary.SourceDomain, boundary.TargetDomain, "Aucun owner de verite n'est declare."));
                }

                if (boundary.Interface == null || boundary.Interface.Direction == ColonyIntegrationDirection.ForbiddenDirectMutation)
                {
                    findings.Add(new ColonyIntegrationDiagnostic(ColonyIntegrationDiagnosticCode.DirectMutationDetected, boundary.SourceDomain, boundary.TargetDomain, "La boundary demande une mutation directe interdite."));
                }

                if (boundary.ManagerReplacementRequested)
                {
                    findings.Add(new ColonyIntegrationDiagnostic(ColonyIntegrationDiagnosticCode.ManagerReplacementRequested, boundary.SourceDomain, boundary.TargetDomain, "La boundary tente de remplacer un manager existant."));
                }

                if (boundary.UnseededRandomnessRequested)
                {
                    findings.Add(new ColonyIntegrationDiagnostic(ColonyIntegrationDiagnosticCode.UnseededRandomnessRequested, boundary.SourceDomain, boundary.TargetDomain, "La boundary demande du hasard non seedable."));
                }

                foreach (ColonyIntegrationBlocker blocker in boundary.Blockers)
                {
                    if (blocker.IsBlocking && !blocker.IsExplained)
                    {
                        findings.Add(new ColonyIntegrationDiagnostic(ColonyIntegrationDiagnosticCode.IntegrationBlockerUnexplained, boundary.SourceDomain, boundary.TargetDomain, blocker.BlockerId));
                    }
                }
            }

            return new ColonyIntegrationDiagnostics(findings);
        }
    }

    public readonly struct ColonyIntegrationDiagnostic
    {
        public ColonyIntegrationDiagnostic(ColonyIntegrationDiagnosticCode code, ColonyIntegrationDomain sourceDomain, ColonyIntegrationDomain targetDomain, string message)
        {
            Code = code;
            SourceDomain = sourceDomain;
            TargetDomain = targetDomain;
            Message = message ?? string.Empty;
        }

        public ColonyIntegrationDiagnosticCode Code { get; }
        public ColonyIntegrationDomain SourceDomain { get; }
        public ColonyIntegrationDomain TargetDomain { get; }
        public string Message { get; }
    }

    public sealed class ColonyIntegrationDiagnostics
    {
        public ColonyIntegrationDiagnostics(IReadOnlyList<ColonyIntegrationDiagnostic> findings)
        {
            Findings = findings ?? Array.Empty<ColonyIntegrationDiagnostic>();
        }

        public IReadOnlyList<ColonyIntegrationDiagnostic> Findings { get; }
        public bool IsValid => Findings.Count == 0;
        public bool Contains(ColonyIntegrationDiagnosticCode code) { return Findings.Any(f => f.Code == code); }
    }

    public enum PopulationWorldInfluenceKind { SeasonPressure, WeatherStress, RegionalResourceAvailability, BiomeSuitability, EcologyPressure, ExplorationVisibility, DangerLevel }
    public enum PopulationWorldLinkVerdict { InfluenceAvailable, InfluencePartial, InfluenceBlocked, WorldContextMissing }
    public enum PopulationWorldLinkDiagnosticCode { WorldSnapshotMissing, PopulationMutationRequested, InfluenceOwnerAmbiguous, UnseededInfluence, LegacyManagerConflict }

    public sealed class PopulationWorldInfluence
    {
        public PopulationWorldInfluence(PopulationWorldInfluenceKind kind, double normalizedValue, string sourceId, string ownerId, bool isSeeded = true)
        {
            Kind = kind;
            NormalizedValue = ColonyIntegrationIds.Clamp01(normalizedValue);
            SourceId = sourceId ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
            IsSeeded = isSeeded;
        }

        public PopulationWorldInfluenceKind Kind { get; }
        public double NormalizedValue { get; }
        public string SourceId { get; }
        public string OwnerId { get; }
        public bool IsSeeded { get; }
    }

    public sealed class ColonyPopulationWorldContext
    {
        public ColonyPopulationWorldContext(
            string colonyId,
            string regionId,
            string worldSnapshotId,
            int seed,
            IReadOnlyList<PopulationWorldInfluence> influences,
            bool populationMutationRequested = false,
            bool legacyManagerConflict = false)
        {
            ColonyId = ColonyIntegrationIds.Require(colonyId);
            RegionId = ColonyIntegrationIds.Require(regionId);
            WorldSnapshotId = worldSnapshotId ?? string.Empty;
            Seed = seed;
            Influences = (influences ?? Array.Empty<PopulationWorldInfluence>())
                .OrderBy(i => i.Kind)
                .ThenBy(i => i.SourceId, StringComparer.Ordinal)
                .ToArray();
            PopulationMutationRequested = populationMutationRequested;
            LegacyManagerConflict = legacyManagerConflict;
        }

        public string ColonyId { get; }
        public string RegionId { get; }
        public string WorldSnapshotId { get; }
        public int Seed { get; }
        public IReadOnlyList<PopulationWorldInfluence> Influences { get; }
        public bool PopulationMutationRequested { get; }
        public bool LegacyManagerConflict { get; }

        public PopulationWorldLinkDiagnostics Evaluate()
        {
            List<PopulationWorldLinkDiagnosticCode> findings = new List<PopulationWorldLinkDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(WorldSnapshotId)) findings.Add(PopulationWorldLinkDiagnosticCode.WorldSnapshotMissing);
            if (PopulationMutationRequested) findings.Add(PopulationWorldLinkDiagnosticCode.PopulationMutationRequested);
            if (LegacyManagerConflict) findings.Add(PopulationWorldLinkDiagnosticCode.LegacyManagerConflict);
            if (Influences.Any(i => string.IsNullOrWhiteSpace(i.OwnerId))) findings.Add(PopulationWorldLinkDiagnosticCode.InfluenceOwnerAmbiguous);
            if (Influences.Any(i => !i.IsSeeded)) findings.Add(PopulationWorldLinkDiagnosticCode.UnseededInfluence);

            PopulationWorldLinkVerdict verdict = string.IsNullOrWhiteSpace(WorldSnapshotId)
                ? PopulationWorldLinkVerdict.WorldContextMissing
                : findings.Count == 0
                    ? PopulationWorldLinkVerdict.InfluenceAvailable
                    : findings.Any(f => f == PopulationWorldLinkDiagnosticCode.PopulationMutationRequested)
                        ? PopulationWorldLinkVerdict.InfluenceBlocked
                        : PopulationWorldLinkVerdict.InfluencePartial;
            return new PopulationWorldLinkDiagnostics(verdict, findings);
        }
    }

    public sealed class PopulationWorldLinkDiagnostics
    {
        public PopulationWorldLinkDiagnostics(PopulationWorldLinkVerdict verdict, IReadOnlyList<PopulationWorldLinkDiagnosticCode> findings)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<PopulationWorldLinkDiagnosticCode>();
        }

        public PopulationWorldLinkVerdict Verdict { get; }
        public IReadOnlyList<PopulationWorldLinkDiagnosticCode> Findings { get; }
        public bool Contains(PopulationWorldLinkDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum AIWorldIntentKind { ExploreRegion, AvoidDanger, PrioritizeResource, ScoutOpportunity, DelayTravel, RequestDefenseReadiness, ObserveOnly }
    public enum AIWorldIntentReason { ResourceOpportunity, DangerDetected, VisibilityGap, WeatherRisk, EcologyPressure, StrategySignal, EmergencySignal }
    public enum AIWorldIntentBlocker { None, WorldContextMissing, PathfindingOutOfScope, TaskMutationForbidden, ReasonMissing, PriorityUnseeded }
    public enum AIWorldIntentVerdict { IntentAvailable, IntentBlocked, IntentObservationOnly, IntentForbidden }
    public enum AIWorldIntentDiagnosticCode { WorldContextMissing, PathfindingOutOfScope, IntentReasonMissing, DirectTaskMutationRequested, UnseededIntentPriority }

    public sealed class ColonyAIWorldIntent
    {
        public ColonyAIWorldIntent(
            AIWorldIntentKind kind,
            string regionId,
            string sourceId,
            AIWorldIntentReason? reason,
            int priority,
            AIWorldIntentBlocker blocker = AIWorldIntentBlocker.None,
            bool requiresPathfinding = false,
            bool directTaskMutationRequested = false,
            bool priorityIsSeeded = true)
        {
            Kind = kind;
            RegionId = ColonyIntegrationIds.Require(regionId);
            SourceId = sourceId ?? string.Empty;
            Reason = reason;
            Priority = priority;
            Blocker = blocker;
            RequiresPathfinding = requiresPathfinding;
            DirectTaskMutationRequested = directTaskMutationRequested;
            PriorityIsSeeded = priorityIsSeeded;
        }

        public AIWorldIntentKind Kind { get; }
        public string RegionId { get; }
        public string SourceId { get; }
        public AIWorldIntentReason? Reason { get; }
        public int Priority { get; }
        public AIWorldIntentBlocker Blocker { get; }
        public bool RequiresPathfinding { get; }
        public bool DirectTaskMutationRequested { get; }
        public bool PriorityIsSeeded { get; }
    }

    public sealed class AIWorldIntentDiagnostics
    {
        public AIWorldIntentDiagnostics(AIWorldIntentVerdict verdict, IReadOnlyList<AIWorldIntentDiagnosticCode> findings, IReadOnlyList<ColonyAIWorldIntent> intents)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<AIWorldIntentDiagnosticCode>();
            Intents = intents ?? Array.Empty<ColonyAIWorldIntent>();
        }

        public AIWorldIntentVerdict Verdict { get; }
        public IReadOnlyList<AIWorldIntentDiagnosticCode> Findings { get; }
        public IReadOnlyList<ColonyAIWorldIntent> Intents { get; }
        public bool Contains(AIWorldIntentDiagnosticCode code) { return Findings.Contains(code); }

        public static AIWorldIntentDiagnostics Evaluate(IReadOnlyList<ColonyAIWorldIntent> intents, bool worldContextMissing = false)
        {
            List<AIWorldIntentDiagnosticCode> findings = new List<AIWorldIntentDiagnosticCode>();
            IReadOnlyList<ColonyAIWorldIntent> ordered = (intents ?? Array.Empty<ColonyAIWorldIntent>())
                .OrderByDescending(i => i.Priority)
                .ThenBy(i => i.RegionId, StringComparer.Ordinal)
                .ThenBy(i => i.SourceId, StringComparer.Ordinal)
                .ToArray();
            if (worldContextMissing) findings.Add(AIWorldIntentDiagnosticCode.WorldContextMissing);
            if (ordered.Any(i => i.Reason == null)) findings.Add(AIWorldIntentDiagnosticCode.IntentReasonMissing);
            if (ordered.Any(i => i.RequiresPathfinding || i.Blocker == AIWorldIntentBlocker.PathfindingOutOfScope)) findings.Add(AIWorldIntentDiagnosticCode.PathfindingOutOfScope);
            if (ordered.Any(i => i.DirectTaskMutationRequested)) findings.Add(AIWorldIntentDiagnosticCode.DirectTaskMutationRequested);
            if (ordered.Any(i => !i.PriorityIsSeeded)) findings.Add(AIWorldIntentDiagnosticCode.UnseededIntentPriority);

            AIWorldIntentVerdict verdict = findings.Any(f => f == AIWorldIntentDiagnosticCode.DirectTaskMutationRequested)
                ? AIWorldIntentVerdict.IntentForbidden
                : findings.Count == 0
                    ? AIWorldIntentVerdict.IntentAvailable
                    : ordered.All(i => i.Kind == AIWorldIntentKind.ObserveOnly)
                        ? AIWorldIntentVerdict.IntentObservationOnly
                        : AIWorldIntentVerdict.IntentBlocked;
            return new AIWorldIntentDiagnostics(verdict, findings, ordered);
        }
    }

    public enum ConstructionFootprintConstraint { BiomeConstraint, RegionalResourceDependency, TerrainExposure, VisibilityRequirement, DefenseExposure, LogisticsDistance }
    public enum ConstructionWorldFootprintVerdict { FootprintReadable, FootprintWithWarnings, FootprintBlocked, WorldConstraintMissing }
    public enum ConstructionWorldGap { None, MissingBiomeData, MissingResourceData, MissingVisibilityData, MissingDefenseData, MissingLogisticsData }
    public enum ConstructionWorldFootprintDiagnosticCode { PlacementMutationRequested, ConstructionValidatorBypassed, WorldConstraintMissing, ExpansionDependencyUnknown, ScenePlacementRequested }

    public sealed class ConstructionExpansionDependency
    {
        public ConstructionExpansionDependency(string dependencyId, ColonyIntegrationDomain domain, bool isKnown = true)
        {
            DependencyId = ColonyIntegrationIds.Require(dependencyId);
            Domain = domain;
            IsKnown = isKnown;
        }

        public string DependencyId { get; }
        public ColonyIntegrationDomain Domain { get; }
        public bool IsKnown { get; }
    }

    public sealed class ConstructionFootprintConstraintEntry
    {
        public ConstructionFootprintConstraintEntry(ConstructionFootprintConstraint constraint, int severity, string sourceId, bool isKnown = true)
        {
            Constraint = constraint;
            Severity = Math.Max(0, severity);
            SourceId = sourceId ?? string.Empty;
            IsKnown = isKnown;
        }

        public ConstructionFootprintConstraint Constraint { get; }
        public int Severity { get; }
        public string SourceId { get; }
        public bool IsKnown { get; }
    }

    public sealed class ColonyConstructionWorldFootprint
    {
        public ColonyConstructionWorldFootprint(
            string projectId,
            string regionId,
            IReadOnlyList<ConstructionFootprintConstraintEntry> constraints,
            IReadOnlyList<ConstructionExpansionDependency> dependencies,
            IReadOnlyList<ConstructionWorldGap> gaps = null,
            bool placementMutationRequested = false,
            bool constructionValidatorBypassed = false,
            bool scenePlacementRequested = false)
        {
            ProjectId = ColonyIntegrationIds.Require(projectId);
            RegionId = ColonyIntegrationIds.Require(regionId);
            Constraints = (constraints ?? Array.Empty<ConstructionFootprintConstraintEntry>())
                .OrderByDescending(c => c.Severity)
                .ThenBy(c => c.Constraint)
                .ToArray();
            Dependencies = dependencies ?? Array.Empty<ConstructionExpansionDependency>();
            Gaps = gaps ?? Array.Empty<ConstructionWorldGap>();
            PlacementMutationRequested = placementMutationRequested;
            ConstructionValidatorBypassed = constructionValidatorBypassed;
            ScenePlacementRequested = scenePlacementRequested;
        }

        public string ProjectId { get; }
        public string RegionId { get; }
        public IReadOnlyList<ConstructionFootprintConstraintEntry> Constraints { get; }
        public IReadOnlyList<ConstructionExpansionDependency> Dependencies { get; }
        public IReadOnlyList<ConstructionWorldGap> Gaps { get; }
        public bool PlacementMutationRequested { get; }
        public bool ConstructionValidatorBypassed { get; }
        public bool ScenePlacementRequested { get; }

        public ConstructionWorldFootprintDiagnostics Evaluate()
        {
            List<ConstructionWorldFootprintDiagnosticCode> findings = new List<ConstructionWorldFootprintDiagnosticCode>();
            if (PlacementMutationRequested) findings.Add(ConstructionWorldFootprintDiagnosticCode.PlacementMutationRequested);
            if (ConstructionValidatorBypassed) findings.Add(ConstructionWorldFootprintDiagnosticCode.ConstructionValidatorBypassed);
            if (ScenePlacementRequested) findings.Add(ConstructionWorldFootprintDiagnosticCode.ScenePlacementRequested);
            if (Constraints.Count == 0 || Constraints.Any(c => !c.IsKnown)) findings.Add(ConstructionWorldFootprintDiagnosticCode.WorldConstraintMissing);
            if (Dependencies.Any(d => !d.IsKnown)) findings.Add(ConstructionWorldFootprintDiagnosticCode.ExpansionDependencyUnknown);

            ConstructionWorldFootprintVerdict verdict = findings.Contains(ConstructionWorldFootprintDiagnosticCode.WorldConstraintMissing)
                ? ConstructionWorldFootprintVerdict.WorldConstraintMissing
                : findings.Any(f => f == ConstructionWorldFootprintDiagnosticCode.PlacementMutationRequested || f == ConstructionWorldFootprintDiagnosticCode.ConstructionValidatorBypassed || f == ConstructionWorldFootprintDiagnosticCode.ScenePlacementRequested)
                    ? ConstructionWorldFootprintVerdict.FootprintBlocked
                    : findings.Count == 0
                        ? ConstructionWorldFootprintVerdict.FootprintReadable
                        : ConstructionWorldFootprintVerdict.FootprintWithWarnings;
            return new ConstructionWorldFootprintDiagnostics(verdict, findings);
        }
    }

    public sealed class ConstructionWorldFootprintDiagnostics
    {
        public ConstructionWorldFootprintDiagnostics(ConstructionWorldFootprintVerdict verdict, IReadOnlyList<ConstructionWorldFootprintDiagnosticCode> findings)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<ConstructionWorldFootprintDiagnosticCode>();
        }

        public ConstructionWorldFootprintVerdict Verdict { get; }
        public IReadOnlyList<ConstructionWorldFootprintDiagnosticCode> Findings { get; }
        public bool Contains(ConstructionWorldFootprintDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum LogisticsPressure { ShortageRisk, TravelExposure, WeatherDelay, RegionalDepletion, StorageConstraint, ReservationConflict }
    public enum LogisticsWorldRisk { None, ResourceSourceMissing, QuantityConservationRisk, RouteUnavailable, InventoryMutation, ReservationMutation }
    public enum LogisticsWorldDiagnosticCode { InventoryMutationRequested, ReservationMutationRequested, PathfindingOutOfScope, ResourceSourceMissing, QuantityConservationRisk }

    public sealed class WorldResourceAvailability
    {
        public WorldResourceAvailability(string resourceId, string regionId, double availableQuantity, bool hasSource = true)
        {
            ResourceId = ColonyIntegrationIds.Require(resourceId);
            RegionId = ColonyIntegrationIds.Require(regionId);
            AvailableQuantity = Math.Max(0d, availableQuantity);
            HasSource = hasSource;
        }

        public string ResourceId { get; }
        public string RegionId { get; }
        public double AvailableQuantity { get; }
        public bool HasSource { get; }
    }

    public sealed class ConceptualRoute
    {
        public ConceptualRoute(string routeId, string fromRegionId, string toColonyId, double conceptualDistance, bool physicalPathRequested = false)
        {
            RouteId = ColonyIntegrationIds.Require(routeId);
            FromRegionId = ColonyIntegrationIds.Require(fromRegionId);
            ToColonyId = ColonyIntegrationIds.Require(toColonyId);
            ConceptualDistance = Math.Max(0d, conceptualDistance);
            PhysicalPathRequested = physicalPathRequested;
        }

        public string RouteId { get; }
        public string FromRegionId { get; }
        public string ToColonyId { get; }
        public double ConceptualDistance { get; }
        public bool PhysicalPathRequested { get; }
    }

    public sealed class ColonyResourceLogisticsWorldLink
    {
        public ColonyResourceLogisticsWorldLink(
            WorldResourceAvailability availability,
            double colonyDemand,
            IReadOnlyList<LogisticsPressure> pressures,
            ConceptualRoute route,
            IReadOnlyList<LogisticsWorldRisk> risks = null,
            bool inventoryMutationRequested = false,
            bool reservationMutationRequested = false)
        {
            Availability = availability;
            ColonyDemand = Math.Max(0d, colonyDemand);
            Pressures = pressures ?? Array.Empty<LogisticsPressure>();
            Route = route;
            Risks = risks ?? Array.Empty<LogisticsWorldRisk>();
            InventoryMutationRequested = inventoryMutationRequested;
            ReservationMutationRequested = reservationMutationRequested;
        }

        public WorldResourceAvailability Availability { get; }
        public double ColonyDemand { get; }
        public IReadOnlyList<LogisticsPressure> Pressures { get; }
        public ConceptualRoute Route { get; }
        public IReadOnlyList<LogisticsWorldRisk> Risks { get; }
        public bool InventoryMutationRequested { get; }
        public bool ReservationMutationRequested { get; }

        public LogisticsWorldDiagnostics Evaluate()
        {
            List<LogisticsWorldDiagnosticCode> findings = new List<LogisticsWorldDiagnosticCode>();
            if (Availability == null || !Availability.HasSource) findings.Add(LogisticsWorldDiagnosticCode.ResourceSourceMissing);
            if (InventoryMutationRequested) findings.Add(LogisticsWorldDiagnosticCode.InventoryMutationRequested);
            if (ReservationMutationRequested) findings.Add(LogisticsWorldDiagnosticCode.ReservationMutationRequested);
            if (Route != null && Route.PhysicalPathRequested) findings.Add(LogisticsWorldDiagnosticCode.PathfindingOutOfScope);
            if (Risks.Contains(LogisticsWorldRisk.QuantityConservationRisk)) findings.Add(LogisticsWorldDiagnosticCode.QuantityConservationRisk);
            return new LogisticsWorldDiagnostics(findings);
        }
    }

    public sealed class LogisticsWorldDiagnostics
    {
        public LogisticsWorldDiagnostics(IReadOnlyList<LogisticsWorldDiagnosticCode> findings)
        {
            Findings = findings ?? Array.Empty<LogisticsWorldDiagnosticCode>();
        }

        public IReadOnlyList<LogisticsWorldDiagnosticCode> Findings { get; }
        public bool IsReadable => Findings.Count == 0;
        public bool Contains(LogisticsWorldDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum DefenseAlertSource { RegionalDanger, WeatherExposure, PredatorSignal, ResourceRouteThreat, ConstructionExposure, EmergencyPropagation }
    public enum DefenseAlertSeverity { Missing, Low, Moderate, High, Critical }
    public enum DefenseReadinessStatus { Observed, Ready, Warning, Blocked, CombatOutOfScope }
    public enum DefenseAlertBlocker { None, DangerSourceMissing, CombatOutOfScope, DefenseMutationForbidden, SeverityMissing, ThreatRollUnseeded }
    public enum DefenseAlertDiagnosticCode { DangerSourceMissing, CombatSimulationRequested, DefenseMutationRequested, AlertSeverityMissing, UnseededThreatRoll }

    public sealed class ColonyDefenseWorldAlert
    {
        public ColonyDefenseWorldAlert(
            DefenseAlertSource? source,
            string regionId,
            string colonyZoneId,
            DefenseAlertSeverity severity,
            DefenseReadinessStatus readiness,
            DefenseAlertBlocker blocker = DefenseAlertBlocker.None,
            bool combatSimulationRequested = false,
            bool defenseMutationRequested = false,
            bool threatRollSeeded = true)
        {
            Source = source;
            RegionId = ColonyIntegrationIds.Require(regionId);
            ColonyZoneId = ColonyIntegrationIds.Require(colonyZoneId);
            Severity = severity;
            Readiness = readiness;
            Blocker = blocker;
            CombatSimulationRequested = combatSimulationRequested;
            DefenseMutationRequested = defenseMutationRequested;
            ThreatRollSeeded = threatRollSeeded;
        }

        public DefenseAlertSource? Source { get; }
        public string RegionId { get; }
        public string ColonyZoneId { get; }
        public DefenseAlertSeverity Severity { get; }
        public DefenseReadinessStatus Readiness { get; }
        public DefenseAlertBlocker Blocker { get; }
        public bool CombatSimulationRequested { get; }
        public bool DefenseMutationRequested { get; }
        public bool ThreatRollSeeded { get; }
    }

    public sealed class DefenseAlertDiagnostics
    {
        public DefenseAlertDiagnostics(IReadOnlyList<DefenseAlertDiagnosticCode> findings, IReadOnlyList<ColonyDefenseWorldAlert> alerts)
        {
            Findings = findings ?? Array.Empty<DefenseAlertDiagnosticCode>();
            Alerts = alerts ?? Array.Empty<ColonyDefenseWorldAlert>();
        }

        public IReadOnlyList<DefenseAlertDiagnosticCode> Findings { get; }
        public IReadOnlyList<ColonyDefenseWorldAlert> Alerts { get; }
        public bool Contains(DefenseAlertDiagnosticCode code) { return Findings.Contains(code); }

        public static DefenseAlertDiagnostics Evaluate(IReadOnlyList<ColonyDefenseWorldAlert> alerts)
        {
            IReadOnlyList<ColonyDefenseWorldAlert> ordered = (alerts ?? Array.Empty<ColonyDefenseWorldAlert>())
                .OrderByDescending(a => a.Severity)
                .ThenBy(a => a.RegionId, StringComparer.Ordinal)
                .ThenBy(a => a.Source)
                .ToArray();
            List<DefenseAlertDiagnosticCode> findings = new List<DefenseAlertDiagnosticCode>();
            if (ordered.Any(a => a.Source == null)) findings.Add(DefenseAlertDiagnosticCode.DangerSourceMissing);
            if (ordered.Any(a => a.Severity == DefenseAlertSeverity.Missing)) findings.Add(DefenseAlertDiagnosticCode.AlertSeverityMissing);
            if (ordered.Any(a => a.CombatSimulationRequested || a.Readiness == DefenseReadinessStatus.CombatOutOfScope)) findings.Add(DefenseAlertDiagnosticCode.CombatSimulationRequested);
            if (ordered.Any(a => a.DefenseMutationRequested)) findings.Add(DefenseAlertDiagnosticCode.DefenseMutationRequested);
            if (ordered.Any(a => !a.ThreatRollSeeded)) findings.Add(DefenseAlertDiagnosticCode.UnseededThreatRoll);
            return new DefenseAlertDiagnostics(findings, ordered);
        }
    }

    public enum StrategyFeedbackSource { PopulationWorld, AIWorldIntent, ConstructionFootprint, ResourceLogistics, DefenseAlert, EmergencyPropagation, Analytics, Forecast }
    public enum StrategyFeedbackRecommendation { PopulationAdaptation, ResourcePriority, ConstructionTiming, DefenseReadiness, ExplorationTiming, EmergencyPreparation }
    public enum StrategyFeedbackConfidence { Missing, Low, Medium, High }
    public enum StrategyFeedbackLimit { None, SourceMissing, ForecastOutOfScope, ContradictorySignals, ObservationOnly, NoAutoDecision }
    public enum StrategyFeedbackStatus { Informational, LowConfidence, Blocked, ForbiddenAutoDecision }
    public enum StrategyFeedbackDiagnosticCode { FeedbackSourceMissing, DecisionMutationRequested, ConfidenceMissing, ForecastOutOfScope, ContradictorySignals }

    public sealed class ColonyStrategyFeedback
    {
        public ColonyStrategyFeedback(
            StrategyFeedbackRecommendation recommendation,
            IReadOnlyList<StrategyFeedbackSource> sources,
            StrategyFeedbackConfidence confidence,
            IReadOnlyList<StrategyFeedbackLimit> limits,
            StrategyFeedbackStatus status = StrategyFeedbackStatus.Informational,
            bool decisionMutationRequested = false)
        {
            Recommendation = recommendation;
            Sources = sources ?? Array.Empty<StrategyFeedbackSource>();
            Confidence = confidence;
            Limits = limits ?? Array.Empty<StrategyFeedbackLimit>();
            Status = status;
            DecisionMutationRequested = decisionMutationRequested;
        }

        public StrategyFeedbackRecommendation Recommendation { get; }
        public IReadOnlyList<StrategyFeedbackSource> Sources { get; }
        public StrategyFeedbackConfidence Confidence { get; }
        public IReadOnlyList<StrategyFeedbackLimit> Limits { get; }
        public StrategyFeedbackStatus Status { get; }
        public bool DecisionMutationRequested { get; }
    }

    public class ColonyStrategyFeedbackDiagnostics
    {
        public ColonyStrategyFeedbackDiagnostics(IReadOnlyList<StrategyFeedbackDiagnosticCode> findings)
        {
            Findings = findings ?? Array.Empty<StrategyFeedbackDiagnosticCode>();
        }

        public IReadOnlyList<StrategyFeedbackDiagnosticCode> Findings { get; }
        public bool Contains(StrategyFeedbackDiagnosticCode code) { return Findings.Contains(code); }

        public static ColonyStrategyFeedbackDiagnostics Evaluate(IReadOnlyList<ColonyStrategyFeedback> feedbacks)
        {
            IReadOnlyList<ColonyStrategyFeedback> entries = feedbacks ?? Array.Empty<ColonyStrategyFeedback>();
            List<StrategyFeedbackDiagnosticCode> findings = new List<StrategyFeedbackDiagnosticCode>();
            if (entries.Any(f => f.Sources.Count == 0 || f.Limits.Contains(StrategyFeedbackLimit.SourceMissing))) findings.Add(StrategyFeedbackDiagnosticCode.FeedbackSourceMissing);
            if (entries.Any(f => f.DecisionMutationRequested || f.Status == StrategyFeedbackStatus.ForbiddenAutoDecision)) findings.Add(StrategyFeedbackDiagnosticCode.DecisionMutationRequested);
            if (entries.Any(f => f.Confidence == StrategyFeedbackConfidence.Missing)) findings.Add(StrategyFeedbackDiagnosticCode.ConfidenceMissing);
            if (entries.Any(f => f.Limits.Contains(StrategyFeedbackLimit.ForecastOutOfScope))) findings.Add(StrategyFeedbackDiagnosticCode.ForecastOutOfScope);
            if (entries.Any(f => f.Limits.Contains(StrategyFeedbackLimit.ContradictorySignals))) findings.Add(StrategyFeedbackDiagnosticCode.ContradictorySignals);
            return new ColonyStrategyFeedbackDiagnostics(findings);
        }
    }

    public sealed class StrategyFeedbackDiagnostics : ColonyStrategyFeedbackDiagnostics
    {
        public StrategyFeedbackDiagnostics(IReadOnlyList<StrategyFeedbackDiagnosticCode> findings) : base(findings) { }
    }

    public enum EmergencyPropagationNode { WorldEvent, ColonyState, PopulationRisk, AIIntent, ResourcePressure, DefenseAlert, StrategyFeedback, DemoReadModel }
    public enum EmergencyPropagationSeverity { Low, Moderate, High, Critical, Conflict }
    public enum EmergencyPropagationBlocker { None, SourceMissing, CycleDetected, DestructiveEffectForbidden, EdgeUnexplained, SeverityConflict }
    public enum EmergencyPropagationDiagnosticCode { EmergencySourceMissing, PropagationCycleDetected, DestructiveEffectRequested, UnexplainedEdge, SeverityConflict }

    public sealed class EmergencyPropagationEdge
    {
        public EmergencyPropagationEdge(EmergencyPropagationNode from, EmergencyPropagationNode to, string sourceBee, string explanation, EmergencyPropagationSeverity severity)
        {
            From = from;
            To = to;
            SourceBee = sourceBee ?? string.Empty;
            Explanation = explanation ?? string.Empty;
            Severity = severity;
        }

        public EmergencyPropagationNode From { get; }
        public EmergencyPropagationNode To { get; }
        public string SourceBee { get; }
        public string Explanation { get; }
        public EmergencyPropagationSeverity Severity { get; }
    }

    public sealed class EmergencyPropagationNodeState
    {
        public EmergencyPropagationNodeState(EmergencyPropagationNode node, EmergencyPropagationSeverity severity, EmergencyPropagationBlocker blocker = EmergencyPropagationBlocker.None)
        {
            Node = node;
            Severity = severity;
            Blocker = blocker;
        }

        public EmergencyPropagationNode Node { get; }
        public EmergencyPropagationSeverity Severity { get; }
        public EmergencyPropagationBlocker Blocker { get; }
    }

    public sealed class ColonyEmergencyPropagation
    {
        public ColonyEmergencyPropagation(
            string emergencyId,
            IReadOnlyList<EmergencyPropagationNodeState> nodes,
            IReadOnlyList<EmergencyPropagationEdge> edges,
            bool destructiveEffectRequested = false)
        {
            EmergencyId = emergencyId ?? string.Empty;
            Nodes = nodes ?? Array.Empty<EmergencyPropagationNodeState>();
            Edges = edges ?? Array.Empty<EmergencyPropagationEdge>();
            DestructiveEffectRequested = destructiveEffectRequested;
        }

        public string EmergencyId { get; }
        public IReadOnlyList<EmergencyPropagationNodeState> Nodes { get; }
        public IReadOnlyList<EmergencyPropagationEdge> Edges { get; }
        public bool DestructiveEffectRequested { get; }

        public EmergencyPropagationDiagnostics Evaluate()
        {
            List<EmergencyPropagationDiagnosticCode> findings = new List<EmergencyPropagationDiagnosticCode>();
            if (string.IsNullOrWhiteSpace(EmergencyId) || Nodes.Count == 0) findings.Add(EmergencyPropagationDiagnosticCode.EmergencySourceMissing);
            if (DestructiveEffectRequested || Nodes.Any(n => n.Blocker == EmergencyPropagationBlocker.DestructiveEffectForbidden)) findings.Add(EmergencyPropagationDiagnosticCode.DestructiveEffectRequested);
            if (Edges.Any(e => string.IsNullOrWhiteSpace(e.SourceBee) || string.IsNullOrWhiteSpace(e.Explanation))) findings.Add(EmergencyPropagationDiagnosticCode.UnexplainedEdge);
            if (Nodes.Any(n => n.Severity == EmergencyPropagationSeverity.Conflict) || Edges.Any(e => e.Severity == EmergencyPropagationSeverity.Conflict)) findings.Add(EmergencyPropagationDiagnosticCode.SeverityConflict);
            if (HasCycle()) findings.Add(EmergencyPropagationDiagnosticCode.PropagationCycleDetected);
            return new EmergencyPropagationDiagnostics(findings);
        }

        private bool HasCycle()
        {
            Dictionary<EmergencyPropagationNode, List<EmergencyPropagationNode>> graph = new Dictionary<EmergencyPropagationNode, List<EmergencyPropagationNode>>();
            foreach (EmergencyPropagationEdge edge in Edges)
            {
                if (!graph.TryGetValue(edge.From, out List<EmergencyPropagationNode> next))
                {
                    next = new List<EmergencyPropagationNode>();
                    graph.Add(edge.From, next);
                }

                next.Add(edge.To);
            }

            HashSet<EmergencyPropagationNode> visiting = new HashSet<EmergencyPropagationNode>();
            HashSet<EmergencyPropagationNode> visited = new HashSet<EmergencyPropagationNode>();
            foreach (EmergencyPropagationNode node in graph.Keys)
            {
                if (Visit(node, graph, visiting, visited)) return true;
            }

            return false;
        }

        private static bool Visit(EmergencyPropagationNode node, Dictionary<EmergencyPropagationNode, List<EmergencyPropagationNode>> graph, HashSet<EmergencyPropagationNode> visiting, HashSet<EmergencyPropagationNode> visited)
        {
            if (visited.Contains(node)) return false;
            if (!visiting.Add(node)) return true;
            if (graph.TryGetValue(node, out List<EmergencyPropagationNode> next))
            {
                for (int i = 0; i < next.Count; i++)
                {
                    if (Visit(next[i], graph, visiting, visited)) return true;
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }

    public sealed class EmergencyPropagationDiagnostics
    {
        public EmergencyPropagationDiagnostics(IReadOnlyList<EmergencyPropagationDiagnosticCode> findings)
        {
            Findings = findings ?? Array.Empty<EmergencyPropagationDiagnosticCode>();
        }

        public IReadOnlyList<EmergencyPropagationDiagnosticCode> Findings { get; }
        public bool Contains(EmergencyPropagationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyIntegrationDemoSection { BoundaryMap, PopulationWorld, AIWorldIntent, ConstructionFootprint, ResourceLogistics, DefenseAlerts, StrategyFeedback, EmergencyPropagation, OpenBlockers }
    public enum ColonyIntegrationDemoBadge { Connected, Partial, Blocked, ObservationOnly, OutOfScope }
    public enum ColonyIntegrationDemoDiagnosticCode { DemoSectionSourceMissing, GameplayLogicDetected, DemoMutationRequested, LimitMissing, EvidenceMissing }

    public sealed class ColonyIntegrationDemoEvidence
    {
        public ColonyIntegrationDemoEvidence(string evidenceId, string sourceBee, string description)
        {
            EvidenceId = ColonyIntegrationIds.Require(evidenceId);
            SourceBee = ColonyIntegrationIds.Require(sourceBee);
            Description = description ?? string.Empty;
        }

        public string EvidenceId { get; }
        public string SourceBee { get; }
        public string Description { get; }
    }

    public sealed class ColonyIntegrationDemoSectionState
    {
        public ColonyIntegrationDemoSectionState(
            ColonyIntegrationDemoSection section,
            ColonyIntegrationDemoBadge badge,
            string sourceBee,
            IReadOnlyList<ColonyIntegrationDemoEvidence> evidences,
            IReadOnlyList<string> blockers,
            IReadOnlyList<string> limits,
            bool gameplayLogicDetected = false,
            bool demoMutationRequested = false)
        {
            Section = section;
            Badge = badge;
            SourceBee = sourceBee ?? string.Empty;
            Evidences = evidences ?? Array.Empty<ColonyIntegrationDemoEvidence>();
            Blockers = blockers ?? Array.Empty<string>();
            Limits = limits ?? Array.Empty<string>();
            GameplayLogicDetected = gameplayLogicDetected;
            DemoMutationRequested = demoMutationRequested;
        }

        public ColonyIntegrationDemoSection Section { get; }
        public ColonyIntegrationDemoBadge Badge { get; }
        public string SourceBee { get; }
        public IReadOnlyList<ColonyIntegrationDemoEvidence> Evidences { get; }
        public IReadOnlyList<string> Blockers { get; }
        public IReadOnlyList<string> Limits { get; }
        public bool GameplayLogicDetected { get; }
        public bool DemoMutationRequested { get; }
    }

    public sealed class ColonyIntegrationDemoReadModel
    {
        public ColonyIntegrationDemoReadModel(string colonyId, IReadOnlyList<ColonyIntegrationDemoSectionState> sections)
        {
            ColonyId = ColonyIntegrationIds.Require(colonyId);
            Sections = (sections ?? Array.Empty<ColonyIntegrationDemoSectionState>()).OrderBy(s => s.Section).ToArray();
        }

        public string ColonyId { get; }
        public IReadOnlyList<ColonyIntegrationDemoSectionState> Sections { get; }

        public ColonyIntegrationDemoReadModelDiagnostics Evaluate()
        {
            List<ColonyIntegrationDemoDiagnosticCode> findings = new List<ColonyIntegrationDemoDiagnosticCode>();
            if (Sections.Any(s => string.IsNullOrWhiteSpace(s.SourceBee))) findings.Add(ColonyIntegrationDemoDiagnosticCode.DemoSectionSourceMissing);
            if (Sections.Any(s => s.GameplayLogicDetected)) findings.Add(ColonyIntegrationDemoDiagnosticCode.GameplayLogicDetected);
            if (Sections.Any(s => s.DemoMutationRequested)) findings.Add(ColonyIntegrationDemoDiagnosticCode.DemoMutationRequested);
            if (Sections.Any(s => s.Limits.Count == 0)) findings.Add(ColonyIntegrationDemoDiagnosticCode.LimitMissing);
            if (Sections.Any(s => s.Evidences.Count == 0)) findings.Add(ColonyIntegrationDemoDiagnosticCode.EvidenceMissing);
            return new ColonyIntegrationDemoReadModelDiagnostics(findings);
        }
    }

    public class ColonyIntegrationDemoReadModelDiagnostics
    {
        public ColonyIntegrationDemoReadModelDiagnostics(IReadOnlyList<ColonyIntegrationDemoDiagnosticCode> findings)
        {
            Findings = findings ?? Array.Empty<ColonyIntegrationDemoDiagnosticCode>();
        }

        public IReadOnlyList<ColonyIntegrationDemoDiagnosticCode> Findings { get; }
        public bool Contains(ColonyIntegrationDemoDiagnosticCode code) { return Findings.Contains(code); }
    }

    public sealed class ColonyIntegrationDemoDiagnostics : ColonyIntegrationDemoReadModelDiagnostics
    {
        public ColonyIntegrationDemoDiagnostics(IReadOnlyList<ColonyIntegrationDemoDiagnosticCode> findings) : base(findings) { }
    }

    public enum ColonyIntegrationReadinessVerdict { IntegrationReadyForReview, ReadyWithWarnings, NeedsRevision, BlockedByDirectMutation, BlockedByDemoGap, BlockedByBee261Premature }
    public enum ColonyIntegrationReadinessRisk { MissingCriterion, DemoGap, DirectMutation, ManagerReplacement, ParallelGameplay, EngineBypass, UnseededRandomness, Bee261Premature }
    public enum ColonyIntegrationReadinessDiagnosticCode { Bee261Premature, DemoImpactMissing, DirectDomainMutation, ManagerReplacementDetected, ParallelGameplayDetected, UnseededRandomnessDetected }

    public sealed class ColonyIntegrationReadinessCriterion
    {
        public ColonyIntegrationReadinessCriterion(
            string beeId,
            bool passed,
            string evidence,
            bool demoImpactDeclared = true,
            bool directDomainMutation = false,
            bool managerReplacementDetected = false,
            bool parallelGameplayDetected = false,
            bool unseededRandomnessDetected = false)
        {
            BeeId = ColonyIntegrationIds.Require(beeId);
            Passed = passed;
            Evidence = evidence ?? string.Empty;
            DemoImpactDeclared = demoImpactDeclared;
            DirectDomainMutation = directDomainMutation;
            ManagerReplacementDetected = managerReplacementDetected;
            ParallelGameplayDetected = parallelGameplayDetected;
            UnseededRandomnessDetected = unseededRandomnessDetected;
        }

        public string BeeId { get; }
        public bool Passed { get; }
        public string Evidence { get; }
        public bool DemoImpactDeclared { get; }
        public bool DirectDomainMutation { get; }
        public bool ManagerReplacementDetected { get; }
        public bool ParallelGameplayDetected { get; }
        public bool UnseededRandomnessDetected { get; }
    }

    public sealed class ColonyIntegrationReadinessGate
    {
        public ColonyIntegrationReadinessGate(IReadOnlyList<ColonyIntegrationReadinessCriterion> criteria, bool bee261Referenced = false)
        {
            Criteria = (criteria ?? Array.Empty<ColonyIntegrationReadinessCriterion>()).OrderBy(c => c.BeeId, StringComparer.Ordinal).ToArray();
            Bee261Referenced = bee261Referenced;
        }

        public IReadOnlyList<ColonyIntegrationReadinessCriterion> Criteria { get; }
        public bool Bee261Referenced { get; }

        public ColonyIntegrationReadinessDiagnostics Evaluate()
        {
            List<ColonyIntegrationReadinessDiagnosticCode> findings = new List<ColonyIntegrationReadinessDiagnosticCode>();
            if (Bee261Referenced) findings.Add(ColonyIntegrationReadinessDiagnosticCode.Bee261Premature);
            if (Criteria.Any(c => !c.DemoImpactDeclared)) findings.Add(ColonyIntegrationReadinessDiagnosticCode.DemoImpactMissing);
            if (Criteria.Any(c => c.DirectDomainMutation)) findings.Add(ColonyIntegrationReadinessDiagnosticCode.DirectDomainMutation);
            if (Criteria.Any(c => c.ManagerReplacementDetected)) findings.Add(ColonyIntegrationReadinessDiagnosticCode.ManagerReplacementDetected);
            if (Criteria.Any(c => c.ParallelGameplayDetected)) findings.Add(ColonyIntegrationReadinessDiagnosticCode.ParallelGameplayDetected);
            if (Criteria.Any(c => c.UnseededRandomnessDetected)) findings.Add(ColonyIntegrationReadinessDiagnosticCode.UnseededRandomnessDetected);

            ColonyIntegrationReadinessVerdict verdict = findings.Contains(ColonyIntegrationReadinessDiagnosticCode.Bee261Premature)
                ? ColonyIntegrationReadinessVerdict.BlockedByBee261Premature
                : findings.Contains(ColonyIntegrationReadinessDiagnosticCode.DirectDomainMutation)
                    ? ColonyIntegrationReadinessVerdict.BlockedByDirectMutation
                    : findings.Contains(ColonyIntegrationReadinessDiagnosticCode.DemoImpactMissing)
                        ? ColonyIntegrationReadinessVerdict.BlockedByDemoGap
                        : Criteria.Any(c => !c.Passed)
                            ? ColonyIntegrationReadinessVerdict.NeedsRevision
                            : findings.Count == 0
                                ? ColonyIntegrationReadinessVerdict.IntegrationReadyForReview
                                : ColonyIntegrationReadinessVerdict.ReadyWithWarnings;
            return new ColonyIntegrationReadinessDiagnostics(verdict, findings, BuildRisks(findings));
        }

        private static IReadOnlyList<ColonyIntegrationReadinessRisk> BuildRisks(IReadOnlyList<ColonyIntegrationReadinessDiagnosticCode> findings)
        {
            List<ColonyIntegrationReadinessRisk> risks = new List<ColonyIntegrationReadinessRisk>();
            if (findings.Contains(ColonyIntegrationReadinessDiagnosticCode.Bee261Premature)) risks.Add(ColonyIntegrationReadinessRisk.Bee261Premature);
            if (findings.Contains(ColonyIntegrationReadinessDiagnosticCode.DemoImpactMissing)) risks.Add(ColonyIntegrationReadinessRisk.DemoGap);
            if (findings.Contains(ColonyIntegrationReadinessDiagnosticCode.DirectDomainMutation)) risks.Add(ColonyIntegrationReadinessRisk.DirectMutation);
            if (findings.Contains(ColonyIntegrationReadinessDiagnosticCode.ManagerReplacementDetected)) risks.Add(ColonyIntegrationReadinessRisk.ManagerReplacement);
            if (findings.Contains(ColonyIntegrationReadinessDiagnosticCode.ParallelGameplayDetected)) risks.Add(ColonyIntegrationReadinessRisk.ParallelGameplay);
            if (findings.Contains(ColonyIntegrationReadinessDiagnosticCode.UnseededRandomnessDetected)) risks.Add(ColonyIntegrationReadinessRisk.UnseededRandomness);
            return risks;
        }
    }

    public sealed class ColonyIntegrationReadinessDiagnostics
    {
        public ColonyIntegrationReadinessDiagnostics(ColonyIntegrationReadinessVerdict verdict, IReadOnlyList<ColonyIntegrationReadinessDiagnosticCode> findings, IReadOnlyList<ColonyIntegrationReadinessRisk> risks)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<ColonyIntegrationReadinessDiagnosticCode>();
            Risks = risks ?? Array.Empty<ColonyIntegrationReadinessRisk>();
        }

        public ColonyIntegrationReadinessVerdict Verdict { get; }
        public IReadOnlyList<ColonyIntegrationReadinessDiagnosticCode> Findings { get; }
        public IReadOnlyList<ColonyIntegrationReadinessRisk> Risks { get; }
        public bool Contains(ColonyIntegrationReadinessDiagnosticCode code) { return Findings.Contains(code); }
    }

    internal static class ColonyIntegrationIds
    {
        public static string Require(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable identifier is required.");
            }

            return value;
        }

        public static double Clamp01(double value)
        {
            if (value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }
    }
}
