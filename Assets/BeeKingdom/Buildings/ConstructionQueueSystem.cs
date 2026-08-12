using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum ConstructionQueueState { Queued, WaitingResources, WaitingBuilders, Ready, Executing, Completed, Paused, Cancelled, Failed }
    public enum ConstructionPriority { Background, Low, Normal, High, Critical }

    public sealed class ConstructionQueueItem
    {
        public string ItemId { get; }
        public string WorkflowId { get; }
        public string BuildingEntityId { get; }
        public ConstructionPriority UserPriority { get; private set; }
        public ConstructionQueueState State { get; private set; }
        public IReadOnlyList<string> Dependencies { get; }
        public int ColonyUrgency { get; private set; }
        public long Sequence { get; }

        public ConstructionQueueItem(string itemId, string workflowId, string buildingEntityId, ConstructionPriority priority, IReadOnlyList<string> dependencies, long sequence)
        {
            ItemId = string.IsNullOrWhiteSpace(itemId) ? throw new ArgumentException("Item id is required.", nameof(itemId)) : itemId;
            WorkflowId = workflowId ?? string.Empty;
            BuildingEntityId = buildingEntityId ?? string.Empty;
            UserPriority = priority;
            State = ConstructionQueueState.Queued;
            Dependencies = dependencies ?? Array.Empty<string>();
            Sequence = sequence;
        }

        public void SetPriority(ConstructionPriority priority) => UserPriority = priority;
        public void SetUrgency(int urgency) => ColonyUrgency = urgency < 0 ? 0 : urgency;
        public void ChangeState(ConstructionQueueState state) => State = state;
    }

    public sealed class ConstructionPriorityResolver
    {
        public int ResolveScore(ConstructionQueueItem item, bool resourcesAvailable, bool buildersAvailable, bool dependenciesSatisfied)
        {
            int score = (int)item.UserPriority * 1000 + item.ColonyUrgency;
            if (resourcesAvailable) score += 100;
            if (buildersAvailable) score += 100;
            if (dependenciesSatisfied) score += 200;
            return score;
        }
    }

    public sealed class ConstructionQueue
    {
        private readonly List<ConstructionQueueItem> items = new List<ConstructionQueueItem>();

        public int Count => items.Count;
        public IReadOnlyList<ConstructionQueueItem> Items => items;

        public void Add(ConstructionQueueItem item) => items.Add(item);
        public bool Remove(ConstructionQueueItem item) => items.Remove(item);

        public void Sort(ConstructionPriorityResolver resolver, HashSet<string> completedDependencies)
        {
            items.Sort((left, right) =>
            {
                int rightScore = resolver.ResolveScore(right, true, true, DependenciesSatisfied(right, completedDependencies));
                int leftScore = resolver.ResolveScore(left, true, true, DependenciesSatisfied(left, completedDependencies));
                int scoreCompare = rightScore.CompareTo(leftScore);
                return scoreCompare != 0 ? scoreCompare : left.Sequence.CompareTo(right.Sequence);
            });
        }

        public ConstructionQueueItem FirstReady(HashSet<string> completedDependencies)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ConstructionQueueItem item = items[i];
                if ((item.State == ConstructionQueueState.Queued || item.State == ConstructionQueueState.Ready) && DependenciesSatisfied(item, completedDependencies))
                {
                    return item;
                }
            }

            return null;
        }

        public static bool DependenciesSatisfied(ConstructionQueueItem item, HashSet<string> completedDependencies)
        {
            for (int i = 0; i < item.Dependencies.Count; i++)
            {
                if (!completedDependencies.Contains(item.Dependencies[i])) return false;
            }

            return true;
        }
    }

    public sealed class ConstructionQueueDiagnostics
    {
        public int Enqueued { get; private set; }
        public int Dequeued { get; private set; }
        public int PriorityChanges { get; private set; }
        public int Paused { get; private set; }
        public int Resumed { get; private set; }
        public int Cancelled { get; private set; }
        public int Completed { get; private set; }

        public void RecordEnqueue() => Enqueued++;
        public void RecordDequeue() => Dequeued++;
        public void RecordPriorityChange() => PriorityChanges++;
        public void RecordPause() => Paused++;
        public void RecordResume() => Resumed++;
        public void RecordCancel() => Cancelled++;
        public void RecordComplete() => Completed++;
    }

    public sealed class ConstructionQueueManager
    {
        private readonly ConstructionQueue queue = new ConstructionQueue();
        private readonly ConstructionPriorityResolver resolver = new ConstructionPriorityResolver();
        private readonly Dictionary<string, ConstructionQueueItem> itemsById = new Dictionary<string, ConstructionQueueItem>();
        private readonly HashSet<string> completedItems = new HashSet<string>();
        private readonly ConstructionWorkflowManager workflowManager;
        private readonly IEventBus eventBus;
        private long sequence;

        public ConstructionQueueDiagnostics Diagnostics { get; } = new ConstructionQueueDiagnostics();
        public int Count => queue.Count;

        public ConstructionQueueManager(ConstructionWorkflowManager workflowManager = null, IEventBus eventBus = null)
        {
            this.workflowManager = workflowManager;
            this.eventBus = eventBus;
        }

        public ConstructionQueueItem EnqueueConstruction(string workflowId, string buildingEntityId, ConstructionPriority priority, IReadOnlyList<string> dependencies = null)
        {
            string itemId = "construction-queue-" + (++sequence);
            ConstructionQueueItem item = new ConstructionQueueItem(itemId, workflowId, buildingEntityId, priority, dependencies, sequence);
            queue.Add(item);
            itemsById.Add(itemId, item);
            Diagnostics.RecordEnqueue();
            eventBus?.Publish(new ConstructionQueued(itemId));
            Reorder();
            return item;
        }

        public bool DequeueConstruction(out ConstructionQueueItem item)
        {
            Reorder();
            item = queue.FirstReady(completedItems);
            if (item == null) return false;

            item.ChangeState(ConstructionQueueState.Executing);
            queue.Remove(item);
            Diagnostics.RecordDequeue();
            eventBus?.Publish(new ConstructionDequeued(item.ItemId));
            workflowManager?.StartConstruction(item.WorkflowId, item.BuildingEntityId);
            eventBus?.Publish(new ConstructionStarted(item.ItemId));
            return true;
        }

        public bool PromoteConstruction(string itemId)
        {
            return ChangePriority(itemId, 1);
        }

        public bool DemoteConstruction(string itemId)
        {
            return ChangePriority(itemId, -1);
        }

        public bool PauseConstruction(string itemId)
        {
            if (!itemsById.TryGetValue(itemId, out ConstructionQueueItem item)) return false;
            item.ChangeState(ConstructionQueueState.Paused);
            Diagnostics.RecordPause();
            eventBus?.Publish(new ConstructionPaused(itemId));
            return true;
        }

        public bool ResumeConstruction(string itemId)
        {
            if (!itemsById.TryGetValue(itemId, out ConstructionQueueItem item) || item.State != ConstructionQueueState.Paused) return false;
            item.ChangeState(ConstructionQueueState.Queued);
            Diagnostics.RecordResume();
            eventBus?.Publish(new ConstructionResumed(itemId));
            Reorder();
            return true;
        }

        public bool CancelConstruction(string itemId)
        {
            if (!itemsById.TryGetValue(itemId, out ConstructionQueueItem item)) return false;
            item.ChangeState(ConstructionQueueState.Cancelled);
            queue.Remove(item);
            Diagnostics.RecordCancel();
            eventBus?.Publish(new ConstructionCancelled(itemId));
            return true;
        }

        public bool CompleteConstruction(string itemId)
        {
            if (!itemsById.TryGetValue(itemId, out ConstructionQueueItem item)) return false;
            item.ChangeState(ConstructionQueueState.Completed);
            completedItems.Add(itemId);
            Diagnostics.RecordComplete();
            eventBus?.Publish(new ConstructionCompleted(itemId));
            Reorder();
            return true;
        }

        public IReadOnlyList<ConstructionQueueItem> QueryQueue()
        {
            Reorder();
            return new List<ConstructionQueueItem>(queue.Items);
        }

        private bool ChangePriority(string itemId, int delta)
        {
            if (!itemsById.TryGetValue(itemId, out ConstructionQueueItem item)) return false;
            int next = Math.Max((int)ConstructionPriority.Background, Math.Min((int)ConstructionPriority.Critical, (int)item.UserPriority + delta));
            item.SetPriority((ConstructionPriority)next);
            Diagnostics.RecordPriorityChange();
            eventBus?.Publish(new ConstructionPriorityChanged(itemId, item.UserPriority));
            Reorder();
            return true;
        }

        private void Reorder()
        {
            queue.Sort(resolver, completedItems);
            foreach (ConstructionQueueItem item in queue.Items)
            {
                if (item.State == ConstructionQueueState.Paused || item.State == ConstructionQueueState.Cancelled) continue;
                item.ChangeState(ConstructionQueue.DependenciesSatisfied(item, completedItems) ? ConstructionQueueState.Ready : ConstructionQueueState.Queued);
            }
        }
    }

    public readonly struct ConstructionQueued : IGameplayEvent, IBuildingEvent { public string ItemId { get; } public ConstructionQueued(string itemId) { ItemId = itemId; } }
    public readonly struct ConstructionDequeued : IGameplayEvent, IBuildingEvent { public string ItemId { get; } public ConstructionDequeued(string itemId) { ItemId = itemId; } }
    public readonly struct ConstructionPriorityChanged : IGameplayEvent, IBuildingEvent { public string ItemId { get; } public ConstructionPriority Priority { get; } public ConstructionPriorityChanged(string itemId, ConstructionPriority priority) { ItemId = itemId; Priority = priority; } }
}
