using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum MaintenanceState { Excellent, Good, Normal, NeedsMaintenance, Poor, Critical, Abandoned }
    public enum MaintenanceType { Cleaning, Repair, Reinforcement, ExpansionPreparation, Inspection, Ventilation, ResourceRemoval, StructuralMaintenance }
    public enum MaintenanceTaskState { Scheduled, Started, Suspended, Completed, Cancelled }

    public sealed class MaintenanceDefinition
    {
        public string DefinitionId { get; }
        public MaintenanceType Type { get; }
        public double BaseCost { get; }
        public double WearThreshold { get; }

        public MaintenanceDefinition(string definitionId, MaintenanceType type, double baseCost, double wearThreshold)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Type = type;
            BaseCost = baseCost < 0d ? 0d : baseCost;
            WearThreshold = wearThreshold < 0d ? 0d : wearThreshold;
        }
    }

    public sealed class MaintenanceTask
    {
        public string TaskId { get; }
        public string TargetId { get; }
        public MaintenanceDefinition Definition { get; }
        public MaintenanceTaskState State { get; private set; }
        public double Wear { get; private set; }

        public MaintenanceTask(string taskId, string targetId, MaintenanceDefinition definition, double wear)
        {
            TaskId = taskId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Wear = wear < 0d ? 0d : wear;
            State = MaintenanceTaskState.Scheduled;
        }

        public void Start() => State = MaintenanceTaskState.Started;
        public void Complete() { State = MaintenanceTaskState.Completed; Wear = 0d; }
        public void Cancel() => State = MaintenanceTaskState.Cancelled;
        public void Suspend() => State = MaintenanceTaskState.Suspended;
        public void Resume() => State = MaintenanceTaskState.Started;
    }

    public sealed class MaintenanceSchedule
    {
        private readonly List<MaintenanceTask> tasks = new List<MaintenanceTask>();
        public IReadOnlyList<MaintenanceTask> Tasks => tasks;
        public void Add(MaintenanceTask task) => tasks.Add(task);
        public bool Remove(MaintenanceTask task) => tasks.Remove(task);
    }

    public sealed class MaintenanceDiagnostics
    {
        public int Scheduled { get; private set; }
        public int Started { get; private set; }
        public int Completed { get; private set; }
        public int Cancelled { get; private set; }
        public int Inspections { get; private set; }
        public void RecordScheduled() => Scheduled++;
        public void RecordStarted() => Started++;
        public void RecordCompleted() => Completed++;
        public void RecordCancelled() => Cancelled++;
        public void RecordInspection() => Inspections++;
    }

    public sealed class MaintenanceEngine
    {
        public MaintenanceState Inspect(double wear)
        {
            if (wear < 10d) return MaintenanceState.Excellent;
            if (wear < 25d) return MaintenanceState.Good;
            if (wear < 50d) return MaintenanceState.Normal;
            if (wear < 70d) return MaintenanceState.NeedsMaintenance;
            if (wear < 85d) return MaintenanceState.Poor;
            if (wear < 100d) return MaintenanceState.Critical;
            return MaintenanceState.Abandoned;
        }

        public double CalculateMaintenanceCost(MaintenanceDefinition definition, double age, double traffic, double overload)
        {
            return definition.BaseCost + Math.Max(0d, age) * 0.1d + Math.Max(0d, traffic) * 0.2d + Math.Max(0d, overload) * 0.5d;
        }
    }

    public sealed class MaintenanceManager
    {
        private readonly Dictionary<string, MaintenanceDefinition> definitions = new Dictionary<string, MaintenanceDefinition>();
        private readonly Dictionary<string, MaintenanceTask> tasks = new Dictionary<string, MaintenanceTask>();
        private readonly MaintenanceSchedule schedule = new MaintenanceSchedule();
        private readonly MaintenanceEngine engine = new MaintenanceEngine();
        private readonly IEventBus eventBus;
        private long counter;

        public MaintenanceDiagnostics Diagnostics { get; } = new MaintenanceDiagnostics();
        public MaintenanceManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterDefinition(MaintenanceDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            return true;
        }

        public MaintenanceTask ScheduleMaintenance(string definitionId, string targetId, double wear)
        {
            if (!definitions.TryGetValue(definitionId, out MaintenanceDefinition definition)) return null;
            MaintenanceTask task = new MaintenanceTask("maintenance-" + (++counter), targetId, definition, wear);
            tasks[task.TaskId] = task;
            schedule.Add(task);
            Diagnostics.RecordScheduled();
            eventBus?.Publish(new MaintenanceScheduled(task.TaskId));
            if (wear >= definition.WearThreshold) eventBus?.Publish(new MaintenanceRequired(targetId));
            return task;
        }

        public bool CancelMaintenance(string taskId)
        {
            if (!tasks.TryGetValue(taskId, out MaintenanceTask task)) return false;
            task.Cancel();
            schedule.Remove(task);
            Diagnostics.RecordCancelled();
            eventBus?.Publish(new MaintenanceCancelled(taskId));
            return true;
        }

        public bool StartMaintenance(string taskId)
        {
            if (!tasks.TryGetValue(taskId, out MaintenanceTask task)) return false;
            task.Start();
            Diagnostics.RecordStarted();
            eventBus?.Publish(new MaintenanceStarted(taskId));
            return true;
        }

        public bool CompleteMaintenance(string taskId)
        {
            if (!tasks.TryGetValue(taskId, out MaintenanceTask task)) return false;
            task.Complete();
            Diagnostics.RecordCompleted();
            eventBus?.Publish(new MaintenanceCompleted(taskId));
            return true;
        }

        public MaintenanceState InspectBuilding(double wear)
        {
            Diagnostics.RecordInspection();
            MaintenanceState state = engine.Inspect(wear);
            eventBus?.Publish(new BuildingInspected(state));
            return state;
        }

        public IReadOnlyList<MaintenanceTask> QueryMaintenanceTasks()
        {
            List<MaintenanceTask> result = new List<MaintenanceTask>(schedule.Tasks);
            result.Sort((left, right) => string.CompareOrdinal(left.TaskId, right.TaskId));
            return result;
        }

        public double CalculateMaintenanceCost(string definitionId, double age, double traffic, double overload)
        {
            return definitions.TryGetValue(definitionId, out MaintenanceDefinition definition) ? engine.CalculateMaintenanceCost(definition, age, traffic, overload) : 0d;
        }
    }

    public readonly struct MaintenanceScheduled : IGameplayEvent, IBuildingEvent { public string TaskId { get; } public MaintenanceScheduled(string taskId) { TaskId = taskId; } }
    public readonly struct MaintenanceStarted : IGameplayEvent, IBuildingEvent { public string TaskId { get; } public MaintenanceStarted(string taskId) { TaskId = taskId; } }
    public readonly struct MaintenanceCompleted : IGameplayEvent, IBuildingEvent { public string TaskId { get; } public MaintenanceCompleted(string taskId) { TaskId = taskId; } }
    public readonly struct MaintenanceCancelled : IGameplayEvent, IBuildingEvent { public string TaskId { get; } public MaintenanceCancelled(string taskId) { TaskId = taskId; } }
    public readonly struct BuildingInspected : IGameplayEvent, IBuildingEvent { public MaintenanceState State { get; } public BuildingInspected(MaintenanceState state) { State = state; } }
    public readonly struct MaintenanceRequired : IGameplayEvent, IBuildingEvent { public string TargetId { get; } public MaintenanceRequired(string targetId) { TargetId = targetId; } }
}
