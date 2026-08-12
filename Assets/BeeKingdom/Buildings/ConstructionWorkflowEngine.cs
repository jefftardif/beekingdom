using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum ConstructionWorkflowState
    {
        Requested,
        Validated,
        WaitingForResources,
        WaitingForBuilders,
        PreparingSite,
        UnderConstruction,
        Inspection,
        Operational,
        Suspended,
        Cancelled,
        Failed
    }

    public sealed class ConstructionPhase
    {
        public string PhaseId { get; }
        public double RequiredWorkSeconds { get; }
        public IReadOnlyList<BuildingResourceCost> ResourceCost { get; }

        public ConstructionPhase(string phaseId, double requiredWorkSeconds, IReadOnlyList<BuildingResourceCost> resourceCost = null)
        {
            PhaseId = string.IsNullOrWhiteSpace(phaseId) ? throw new ArgumentException("Phase id is required.", nameof(phaseId)) : phaseId;
            RequiredWorkSeconds = requiredWorkSeconds <= 0d ? 1d : requiredWorkSeconds;
            ResourceCost = resourceCost ?? Array.Empty<BuildingResourceCost>();
        }
    }

    public sealed class ConstructionWorkflowDefinition
    {
        public string WorkflowId { get; }
        public string BuildingId { get; }
        public IReadOnlyList<ConstructionPhase> Phases { get; }

        public ConstructionWorkflowDefinition(string workflowId, string buildingId, IReadOnlyList<ConstructionPhase> phases)
        {
            WorkflowId = string.IsNullOrWhiteSpace(workflowId) ? throw new ArgumentException("Workflow id is required.", nameof(workflowId)) : workflowId;
            BuildingId = string.IsNullOrWhiteSpace(buildingId) ? throw new ArgumentException("Building id is required.", nameof(buildingId)) : buildingId;
            Phases = phases == null || phases.Count == 0 ? new[] { new ConstructionPhase("default", 1d) } : phases;
        }
    }

    public readonly struct ConstructionProgress
    {
        public int PhaseIndex { get; }
        public double PhaseProgress { get; }
        public double TotalProgress { get; }

        public ConstructionProgress(int phaseIndex, double phaseProgress, double totalProgress)
        {
            PhaseIndex = phaseIndex;
            PhaseProgress = phaseProgress;
            TotalProgress = totalProgress;
        }
    }

    public sealed class ConstructionWorkflowInstance
    {
        public string InstanceId { get; }
        public ConstructionWorkflowDefinition Definition { get; }
        public string BuildingEntityId { get; }
        public ConstructionWorkflowState State { get; private set; }
        public int PhaseIndex { get; private set; }
        public double PhaseWorkSeconds { get; private set; }
        public int BuilderCount { get; private set; }

        public ConstructionWorkflowInstance(string instanceId, ConstructionWorkflowDefinition definition, string buildingEntityId)
        {
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            BuildingEntityId = buildingEntityId ?? string.Empty;
            State = ConstructionWorkflowState.Requested;
        }

        public ConstructionPhase CurrentPhase => Definition.Phases[Math.Min(PhaseIndex, Definition.Phases.Count - 1)];

        public bool ChangeState(ConstructionWorkflowState next)
        {
            if (!CanTransition(State, next)) return false;
            State = next;
            return true;
        }

        public void SetBuilders(int builderCount)
        {
            BuilderCount = builderCount < 0 ? 0 : builderCount;
        }

        public bool Advance(double deltaSeconds, double speedMultiplier)
        {
            if (State != ConstructionWorkflowState.UnderConstruction || deltaSeconds <= 0d || BuilderCount <= 0) return false;
            PhaseWorkSeconds += deltaSeconds * Math.Max(0d, speedMultiplier) * BuilderCount;
            if (PhaseWorkSeconds < CurrentPhase.RequiredWorkSeconds) return false;

            PhaseWorkSeconds = 0d;
            PhaseIndex++;
            return true;
        }

        public ConstructionProgress GetProgress()
        {
            double completed = PhaseIndex;
            double phaseProgress = PhaseIndex >= Definition.Phases.Count ? 1d : Math.Min(1d, PhaseWorkSeconds / CurrentPhase.RequiredWorkSeconds);
            double total = Math.Min(1d, (completed + phaseProgress) / Definition.Phases.Count);
            return new ConstructionProgress(PhaseIndex, phaseProgress, total);
        }

        private static bool CanTransition(ConstructionWorkflowState current, ConstructionWorkflowState next)
        {
            if (current == next) return true;
            if (next == ConstructionWorkflowState.Cancelled || next == ConstructionWorkflowState.Failed || next == ConstructionWorkflowState.Suspended) return true;
            if (current == ConstructionWorkflowState.Requested && next == ConstructionWorkflowState.Validated) return true;
            if (current == ConstructionWorkflowState.Validated && next == ConstructionWorkflowState.WaitingForResources) return true;
            if (current == ConstructionWorkflowState.WaitingForResources && next == ConstructionWorkflowState.WaitingForBuilders) return true;
            if (current == ConstructionWorkflowState.WaitingForBuilders && next == ConstructionWorkflowState.PreparingSite) return true;
            if (current == ConstructionWorkflowState.PreparingSite && next == ConstructionWorkflowState.UnderConstruction) return true;
            if (current == ConstructionWorkflowState.UnderConstruction && next == ConstructionWorkflowState.Inspection) return true;
            if (current == ConstructionWorkflowState.Inspection && next == ConstructionWorkflowState.Operational) return true;
            if (current == ConstructionWorkflowState.Suspended && next == ConstructionWorkflowState.UnderConstruction) return true;
            return false;
        }
    }

    public sealed class ConstructionDiagnostics
    {
        public int Started { get; private set; }
        public int Paused { get; private set; }
        public int Resumed { get; private set; }
        public int Completed { get; private set; }
        public int Cancelled { get; private set; }
        public int Failed { get; private set; }
        public int PhaseChanges { get; private set; }

        public void RecordStart() => Started++;
        public void RecordPause() => Paused++;
        public void RecordResume() => Resumed++;
        public void RecordComplete() => Completed++;
        public void RecordCancel() => Cancelled++;
        public void RecordFail() => Failed++;
        public void RecordPhaseChange() => PhaseChanges++;
    }

    public sealed class ConstructionWorkflowManager
    {
        private readonly Dictionary<string, ConstructionWorkflowDefinition> definitions = new Dictionary<string, ConstructionWorkflowDefinition>();
        private readonly Dictionary<string, ConstructionWorkflowInstance> instances = new Dictionary<string, ConstructionWorkflowInstance>();
        private readonly IEventBus eventBus;
        private long counter;

        public ConstructionDiagnostics Diagnostics { get; } = new ConstructionDiagnostics();
        public int ActiveConstructionCount => instances.Count;

        public ConstructionWorkflowManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(ConstructionWorkflowDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.WorkflowId)) return false;
            definitions.Add(definition.WorkflowId, definition);
            return true;
        }

        public ConstructionWorkflowInstance StartConstruction(string workflowId, string buildingEntityId, int buildersAvailable = 1, bool resourcesAvailable = true)
        {
            if (!definitions.TryGetValue(workflowId, out ConstructionWorkflowDefinition definition)) return null;
            string instanceId = workflowId + "-" + (++counter);
            ConstructionWorkflowInstance instance = new ConstructionWorkflowInstance(instanceId, definition, buildingEntityId);
            instance.ChangeState(ConstructionWorkflowState.Validated);
            instance.ChangeState(resourcesAvailable ? ConstructionWorkflowState.WaitingForBuilders : ConstructionWorkflowState.WaitingForResources);
            if (resourcesAvailable && buildersAvailable > 0)
            {
                instance.SetBuilders(buildersAvailable);
                instance.ChangeState(ConstructionWorkflowState.PreparingSite);
                instance.ChangeState(ConstructionWorkflowState.UnderConstruction);
            }

            instances.Add(instanceId, instance);
            Diagnostics.RecordStart();
            eventBus?.Publish(new ConstructionRequested(instanceId));
            eventBus?.Publish(new ConstructionStarted(instanceId));
            return instance;
        }

        public bool PauseConstruction(string instanceId)
        {
            if (!instances.TryGetValue(instanceId, out ConstructionWorkflowInstance instance)) return false;
            bool changed = instance.ChangeState(ConstructionWorkflowState.Suspended);
            if (changed) { Diagnostics.RecordPause(); eventBus?.Publish(new ConstructionPaused(instanceId)); }
            return changed;
        }

        public bool ResumeConstruction(string instanceId, int buildersAvailable = 1)
        {
            if (!instances.TryGetValue(instanceId, out ConstructionWorkflowInstance instance)) return false;
            instance.SetBuilders(buildersAvailable);
            bool changed = instance.ChangeState(ConstructionWorkflowState.UnderConstruction);
            if (changed) { Diagnostics.RecordResume(); eventBus?.Publish(new ConstructionResumed(instanceId)); }
            return changed;
        }

        public bool CancelConstruction(string instanceId)
        {
            if (!instances.TryGetValue(instanceId, out ConstructionWorkflowInstance instance)) return false;
            bool changed = instance.ChangeState(ConstructionWorkflowState.Cancelled);
            if (changed) { Diagnostics.RecordCancel(); eventBus?.Publish(new ConstructionCancelled(instanceId)); }
            return changed;
        }

        public bool AdvanceConstruction(string instanceId, double deltaSeconds, double speedMultiplier = 1d)
        {
            if (!instances.TryGetValue(instanceId, out ConstructionWorkflowInstance instance)) return false;
            bool phaseCompleted = instance.Advance(deltaSeconds, speedMultiplier);
            if (!phaseCompleted) return true;

            Diagnostics.RecordPhaseChange();
            eventBus?.Publish(new ConstructionPhaseChanged(instanceId, instance.PhaseIndex));
            if (instance.PhaseIndex >= instance.Definition.Phases.Count)
            {
                instance.ChangeState(ConstructionWorkflowState.Inspection);
                CompleteConstruction(instanceId);
            }

            return true;
        }

        public bool CompleteConstruction(string instanceId)
        {
            if (!instances.TryGetValue(instanceId, out ConstructionWorkflowInstance instance)) return false;
            bool changed = instance.ChangeState(ConstructionWorkflowState.Operational);
            if (changed) { Diagnostics.RecordComplete(); eventBus?.Publish(new ConstructionCompleted(instanceId)); }
            return changed;
        }

        public bool FailConstruction(string instanceId)
        {
            if (!instances.TryGetValue(instanceId, out ConstructionWorkflowInstance instance)) return false;
            bool changed = instance.ChangeState(ConstructionWorkflowState.Failed);
            if (changed) { Diagnostics.RecordFail(); eventBus?.Publish(new ConstructionFailed(instanceId)); }
            return changed;
        }

        public bool QueryConstruction(string instanceId, out ConstructionWorkflowInstance instance)
        {
            return instances.TryGetValue(instanceId, out instance);
        }
    }

    public readonly struct ConstructionRequested : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public ConstructionRequested(string instanceId) { InstanceId = instanceId; } }
    public readonly struct ConstructionStarted : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public ConstructionStarted(string instanceId) { InstanceId = instanceId; } }
    public readonly struct ConstructionPhaseChanged : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public int PhaseIndex { get; } public ConstructionPhaseChanged(string instanceId, int phaseIndex) { InstanceId = instanceId; PhaseIndex = phaseIndex; } }
    public readonly struct ConstructionPaused : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public ConstructionPaused(string instanceId) { InstanceId = instanceId; } }
    public readonly struct ConstructionResumed : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public ConstructionResumed(string instanceId) { InstanceId = instanceId; } }
    public readonly struct ConstructionCompleted : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public ConstructionCompleted(string instanceId) { InstanceId = instanceId; } }
    public readonly struct ConstructionCancelled : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public ConstructionCancelled(string instanceId) { InstanceId = instanceId; } }
    public readonly struct ConstructionFailed : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public ConstructionFailed(string instanceId) { InstanceId = instanceId; } }
}
