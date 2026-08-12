using System.Collections.Generic;

namespace BeeKingdom.Core.Workflows
{
    public sealed class WorkflowScheduler
    {
        private readonly Dictionary<WorkflowQueueType, WorkflowQueue> queues = new Dictionary<WorkflowQueueType, WorkflowQueue>();

        public void Queue(GameplayWorkflowInstance instance)
        {
            if (!queues.TryGetValue(instance.Definition.QueueType, out WorkflowQueue queue))
            {
                queue = new WorkflowQueue();
                queues[instance.Definition.QueueType] = queue;
            }
            queue.Enqueue(instance);
        }

        public GameplayWorkflowInstance DequeueNext()
        {
            GameplayWorkflowInstance best = null;
            WorkflowQueue bestQueue = null;
            foreach (WorkflowQueue queue in queues.Values)
            {
                GameplayWorkflowInstance candidate = queue.DequeueBest();
                if (candidate == null) continue;
                if (best == null || candidate.Definition.Priority.CompareTo(best.Definition.Priority) < 0 || candidate.Sequence < best.Sequence)
                {
                    if (best != null) bestQueue.Enqueue(best);
                    best = candidate;
                    bestQueue = queue;
                }
                else
                {
                    queue.Enqueue(candidate);
                }
            }
            return best;
        }
    }
}
