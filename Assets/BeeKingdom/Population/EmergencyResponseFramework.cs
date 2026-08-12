using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum EmergencyType { Fire, Flood, PredatorAttack, HiveBreach, QueenCritical, FoodShortage, DiseaseOutbreak, StructuralCollapse, PopulationCollapse, ExtremeWeather, WorldEvent, Custom }
    public enum EmergencySeverity { Advisory, Minor, Moderate, Major, Critical, ColonyThreatening }
    public enum EmergencyState { Detected, Activated, Escalated, Resolved, Cancelled }

    public sealed class EmergencyPlan
    {
        public string EmergencyId { get; }
        public EmergencyType Type { get; }
        public double ActivationThreshold { get; }
        public EmergencyPlan(string emergencyId, EmergencyType type, double activationThreshold)
        {
            EmergencyId = string.IsNullOrWhiteSpace(emergencyId) ? throw new ArgumentException("Emergency id is required.", nameof(emergencyId)) : emergencyId;
            Type = type;
            ActivationThreshold = activationThreshold < 0d ? 0d : activationThreshold > 1d ? 1d : activationThreshold;
        }
    }

    public sealed class EmergencyIncident
    {
        public string IncidentId { get; }
        public string EmergencyId { get; }
        public EmergencyType Type { get; }
        public EmergencySeverity Severity { get; private set; }
        public EmergencyState State { get; private set; }
        public double Score { get; private set; }
        public EmergencyIncident(string incidentId, EmergencyPlan plan, double score)
        {
            IncidentId = incidentId;
            EmergencyId = plan.EmergencyId;
            Type = plan.Type;
            Score = score;
            Severity = ResolveSeverity(score);
            State = EmergencyState.Detected;
        }
        public void Activate() => State = EmergencyState.Activated;
        public void Escalate(double score) { Score = Math.Max(Score, score); Severity = ResolveSeverity(Score); State = EmergencyState.Escalated; }
        public void Resolve() => State = EmergencyState.Resolved;
        public void Cancel() => State = EmergencyState.Cancelled;
        private static EmergencySeverity ResolveSeverity(double score) => score >= 0.95d ? EmergencySeverity.ColonyThreatening : score >= 0.8d ? EmergencySeverity.Critical : score >= 0.65d ? EmergencySeverity.Major : score >= 0.4d ? EmergencySeverity.Moderate : score >= 0.2d ? EmergencySeverity.Minor : EmergencySeverity.Advisory;
    }

    public sealed class EmergencyDetector { public bool Detect(EmergencyPlan plan, double signal) => signal >= plan.ActivationThreshold; }
    public sealed class EmergencyCoordinator { public void Activate(EmergencyIncident incident) => incident.Activate(); public void Resolve(EmergencyIncident incident) => incident.Resolve(); }
    public sealed class EmergencyEngine
    {
        private readonly EmergencyDetector detector = new EmergencyDetector();
        public bool Detect(EmergencyPlan plan, double signal) => detector.Detect(plan, signal);
    }
    public sealed class EmergencyDiagnostics { public int Detected { get; private set; } public int Activated { get; private set; } public int Resolved { get; private set; } public int Cancelled { get; private set; } public void RecordDetected() => Detected++; public void RecordActivated() => Activated++; public void RecordResolved() => Resolved++; public void RecordCancelled() => Cancelled++; }

    public sealed class EmergencyResponseManager
    {
        private readonly Dictionary<string, EmergencyPlan> plans = new Dictionary<string, EmergencyPlan>();
        private readonly Dictionary<string, EmergencyIncident> incidents = new Dictionary<string, EmergencyIncident>();
        private readonly EmergencyEngine engine = new EmergencyEngine();
        private readonly EmergencyCoordinator coordinator = new EmergencyCoordinator();
        private readonly IEventBus eventBus;
        private int sequence;
        public EmergencyDiagnostics Diagnostics { get; } = new EmergencyDiagnostics();
        public EmergencyResponseManager(IEventBus eventBus = null) { this.eventBus = eventBus; }
        public bool RegisterEmergencyType(EmergencyPlan plan) { if (plan == null || plans.ContainsKey(plan.EmergencyId)) return false; plans.Add(plan.EmergencyId, plan); return true; }
        public EmergencyIncident DetectEmergency(string emergencyId, double signal)
        {
            if (!plans.TryGetValue(emergencyId, out EmergencyPlan plan) || !engine.Detect(plan, signal)) return null;
            EmergencyIncident incident = new EmergencyIncident("emergency-" + (++sequence).ToString("D6"), plan, signal);
            incidents.Add(incident.IncidentId, incident);
            Diagnostics.RecordDetected();
            eventBus?.Publish(new EmergencyDetected(incident.IncidentId));
            return incident;
        }
        public bool ActivateEmergency(string incidentId) { if (!incidents.TryGetValue(incidentId, out EmergencyIncident incident)) return false; coordinator.Activate(incident); Diagnostics.RecordActivated(); eventBus?.Publish(new EmergencyActivated(incidentId)); return true; }
        public bool ResolveEmergency(string incidentId) { if (!incidents.TryGetValue(incidentId, out EmergencyIncident incident)) return false; coordinator.Resolve(incident); Diagnostics.RecordResolved(); eventBus?.Publish(new EmergencyResolved(incidentId)); eventBus?.Publish(new EmergencyReportGenerated(incidentId)); return true; }
        public bool CancelEmergency(string incidentId) { if (!incidents.TryGetValue(incidentId, out EmergencyIncident incident)) return false; incident.Cancel(); Diagnostics.RecordCancelled(); eventBus?.Publish(new EmergencyCancelled(incidentId)); return true; }
        public bool EscalateEmergency(string incidentId, double score) { if (!incidents.TryGetValue(incidentId, out EmergencyIncident incident)) return false; incident.Escalate(score); eventBus?.Publish(new EmergencyEscalated(incidentId, incident.Severity)); return true; }
        public IReadOnlyList<EmergencyIncident> QueryEmergencies() { List<EmergencyIncident> result = new List<EmergencyIncident>(incidents.Values); result.Sort((a, b) => string.CompareOrdinal(a.IncidentId, b.IncidentId)); return result; }
    }

    public readonly struct EmergencyDetected : IGameplayEvent, IBeeEvent { public string IncidentId { get; } public EmergencyDetected(string incidentId) { IncidentId = incidentId; } }
    public readonly struct EmergencyActivated : IGameplayEvent, IBeeEvent { public string IncidentId { get; } public EmergencyActivated(string incidentId) { IncidentId = incidentId; } }
    public readonly struct EmergencyEscalated : IGameplayEvent, IBeeEvent { public string IncidentId { get; } public EmergencySeverity Severity { get; } public EmergencyEscalated(string incidentId, EmergencySeverity severity) { IncidentId = incidentId; Severity = severity; } }
    public readonly struct EmergencyResolved : IGameplayEvent, IBeeEvent { public string IncidentId { get; } public EmergencyResolved(string incidentId) { IncidentId = incidentId; } }
    public readonly struct EmergencyCancelled : IGameplayEvent, IBeeEvent { public string IncidentId { get; } public EmergencyCancelled(string incidentId) { IncidentId = incidentId; } }
    public readonly struct EmergencyReportGenerated : IGameplayEvent, IBeeEvent { public string IncidentId { get; } public EmergencyReportGenerated(string incidentId) { IncidentId = incidentId; } }
}
