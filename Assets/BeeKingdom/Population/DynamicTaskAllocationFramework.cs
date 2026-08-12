using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum AllocationPolicyType { ClosestWorker, HighestSkill, LowestFatigue, BalancedWorkload, EmergencyPriority, TeamAssignment, HybridStrategy }

    public sealed class AllocationPolicy
    {
        public string PolicyId { get; }
        public AllocationPolicyType Type { get; }
        public double SkillWeight { get; }
        public double FatigueWeight { get; }
        public double DistanceWeight { get; }
        public double PriorityWeight { get; }
        public AllocationPolicy(string policyId, AllocationPolicyType type, double skillWeight, double fatigueWeight, double distanceWeight, double priorityWeight)
        {
            PolicyId = string.IsNullOrWhiteSpace(policyId) ? throw new ArgumentException("Policy id is required.", nameof(policyId)) : policyId;
            Type = type;
            SkillWeight = Math.Max(0d, skillWeight);
            FatigueWeight = Math.Max(0d, fatigueWeight);
            DistanceWeight = Math.Max(0d, distanceWeight);
            PriorityWeight = Math.Max(0d, priorityWeight);
        }
    }

    public sealed class WorkerCandidate
    {
        public string BeeId { get; }
        public BeeCaste Caste { get; }
        public double Skill { get; }
        public double Fatigue { get; }
        public double Health { get; }
        public double Distance { get; }
        public int CurrentLoad { get; }
        public bool Available { get; }
        public WorkerCandidate(string beeId, BeeCaste caste, double skill, double fatigue, double health, double distance, int currentLoad, bool available = true)
        {
            BeeId = beeId ?? string.Empty;
            Caste = caste;
            Skill = Clamp01(skill);
            Fatigue = Clamp01(fatigue);
            Health = Clamp01(health);
            Distance = Math.Max(0d, distance);
            CurrentLoad = Math.Max(0, currentLoad);
            Available = available;
        }
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class TaskAssignment
    {
        public string TaskId { get; }
        public string BeeId { get; private set; }
        public string PolicyId { get; }
        public double Score { get; private set; }
        public TaskAssignment(string taskId, string beeId, string policyId, double score)
        {
            TaskId = taskId ?? string.Empty;
            BeeId = beeId ?? string.Empty;
            PolicyId = policyId ?? string.Empty;
            Score = score;
        }
        public void Reassign(string beeId, double score) { BeeId = beeId ?? string.Empty; Score = score; }
    }

    public sealed class WorkerEvaluator
    {
        public double Evaluate(WorkerCandidate worker, AllocationPolicy policy, TaskPriority priority)
        {
            double distanceScore = 1d / (1d + worker.Distance);
            double priorityScore = priority == TaskPriority.Emergency ? 1d : priority == TaskPriority.Critical ? 0.85d : priority == TaskPriority.High ? 0.7d : 0.5d;
            return worker.Skill * policy.SkillWeight + (1d - worker.Fatigue) * policy.FatigueWeight + distanceScore * policy.DistanceWeight + priorityScore * policy.PriorityWeight + worker.Health;
        }
    }

    public sealed class TaskMatcher
    {
        private readonly WorkerEvaluator evaluator = new WorkerEvaluator();
        public WorkerCandidate SelectBest(IReadOnlyList<WorkerCandidate> workers, AllocationPolicy policy, TaskPriority priority, out double score)
        {
            WorkerCandidate best = null;
            score = double.MinValue;
            for (int i = 0; i < workers.Count; i++)
            {
                if (!workers[i].Available) continue;
                double candidateScore = evaluator.Evaluate(workers[i], policy, priority) - workers[i].CurrentLoad * 0.1d;
                if (best == null || candidateScore > score || (Math.Abs(candidateScore - score) < 0.0001d && string.CompareOrdinal(workers[i].BeeId, best.BeeId) < 0))
                {
                    best = workers[i];
                    score = candidateScore;
                }
            }
            return best;
        }
    }

    public sealed class TaskAllocationEngine
    {
        private readonly TaskMatcher matcher = new TaskMatcher();
        public WorkerCandidate Allocate(IReadOnlyList<WorkerCandidate> workers, AllocationPolicy policy, TaskPriority priority, out double score) => matcher.SelectBest(workers, policy, priority, out score);
    }

    public sealed class AllocationDiagnostics
    {
        public int Allocated { get; private set; }
        public int Reallocated { get; private set; }
        public int Released { get; private set; }
        public int Failed { get; private set; }
        public void RecordAllocated() => Allocated++;
        public void RecordReallocated() => Reallocated++;
        public void RecordReleased() => Released++;
        public void RecordFailed() => Failed++;
    }

    public sealed class DynamicTaskAllocationManager
    {
        private readonly Dictionary<string, AllocationPolicy> policies = new Dictionary<string, AllocationPolicy>();
        private readonly Dictionary<string, TaskAssignment> assignments = new Dictionary<string, TaskAssignment>();
        private readonly TaskAllocationEngine engine = new TaskAllocationEngine();
        private readonly IEventBus eventBus;
        public AllocationDiagnostics Diagnostics { get; } = new AllocationDiagnostics();
        public DynamicTaskAllocationManager(IEventBus eventBus = null) { this.eventBus = eventBus; }
        public bool RegisterAllocationPolicy(AllocationPolicy policy) { if (policy == null || policies.ContainsKey(policy.PolicyId)) return false; policies.Add(policy.PolicyId, policy); return true; }
        public TaskAssignment AllocateTask(string taskId, string policyId, TaskPriority priority, IReadOnlyList<WorkerCandidate> workers)
        {
            if (!policies.TryGetValue(policyId, out AllocationPolicy policy)) return Fail(taskId);
            WorkerCandidate worker = engine.Allocate(workers ?? Array.Empty<WorkerCandidate>(), policy, priority, out double score);
            if (worker == null) return Fail(taskId);
            TaskAssignment assignment = new TaskAssignment(taskId, worker.BeeId, policyId, score);
            assignments[taskId] = assignment;
            Diagnostics.RecordAllocated();
            eventBus?.Publish(new WorkerSelected(taskId, worker.BeeId));
            eventBus?.Publish(new TaskAllocated(taskId, worker.BeeId));
            return assignment;
        }
        public TaskAssignment ReallocateTask(string taskId, IReadOnlyList<WorkerCandidate> workers)
        {
            if (!assignments.TryGetValue(taskId, out TaskAssignment assignment)) return null;
            TaskAssignment next = AllocateTask(taskId, assignment.PolicyId, TaskPriority.Normal, workers);
            if (next != null) { Diagnostics.RecordReallocated(); eventBus?.Publish(new TaskReallocated(taskId, next.BeeId)); }
            return next;
        }
        public bool ReleaseTask(string taskId) { bool removed = assignments.Remove(taskId); if (removed) { Diagnostics.RecordReleased(); eventBus?.Publish(new AssignmentReleased(taskId)); } return removed; }
        public IReadOnlyList<WorkerCandidate> EvaluateCandidates(IReadOnlyList<WorkerCandidate> workers) => workers ?? Array.Empty<WorkerCandidate>();
        public IReadOnlyList<TaskAssignment> QueryAssignments() { List<TaskAssignment> result = new List<TaskAssignment>(assignments.Values); result.Sort((a, b) => string.CompareOrdinal(a.TaskId, b.TaskId)); return result; }
        private TaskAssignment Fail(string taskId) { Diagnostics.RecordFailed(); eventBus?.Publish(new AllocationFailed(taskId ?? string.Empty)); return null; }
    }

    public readonly struct TaskAllocated : IGameplayEvent, IBeeEvent { public string TaskId { get; } public string BeeId { get; } public TaskAllocated(string taskId, string beeId) { TaskId = taskId; BeeId = beeId; } }
    public readonly struct TaskReallocated : IGameplayEvent, IBeeEvent { public string TaskId { get; } public string BeeId { get; } public TaskReallocated(string taskId, string beeId) { TaskId = taskId; BeeId = beeId; } }
    public readonly struct AssignmentReleased : IGameplayEvent, IBeeEvent { public string TaskId { get; } public AssignmentReleased(string taskId) { TaskId = taskId; } }
    public readonly struct WorkerSelected : IGameplayEvent, IBeeEvent { public string TaskId { get; } public string BeeId { get; } public WorkerSelected(string taskId, string beeId) { TaskId = taskId; BeeId = beeId; } }
    public readonly struct AllocationFailed : IGameplayEvent, IBeeEvent { public string TaskId { get; } public AllocationFailed(string taskId) { TaskId = taskId; } }
}
