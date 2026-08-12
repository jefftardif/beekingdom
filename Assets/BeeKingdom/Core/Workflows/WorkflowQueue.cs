using System.Collections.Generic;

namespace BeeKingdom.Core.Workflows
{
    public sealed class WorkflowQueue
    {
        private readonly List<GameplayWorkflowInstance> items = new List<GameplayWorkflowInstance>();

        public int Count => items.Count;
        public void Enqueue(GameplayWorkflowInstance instance) => items.Add(instance);
        public bool Remove(GameplayWorkflowInstance instance) => items.Remove(instance);

        public GameplayWorkflowInstance DequeueBest()
        {
            if (items.Count == 0) return null;
            int best = 0;
            for (int i = 1; i < items.Count; i++)
            {
                if (Compare(items[i], items[best]) < 0) best = i;
            }
            GameplayWorkflowInstance selected = items[best];
            items.RemoveAt(best);
            return selected;
        }

        private static int Compare(GameplayWorkflowInstance a, GameplayWorkflowInstance b)
        {
            int priority = a.Definition.Priority.CompareTo(b.Definition.Priority);
            return priority != 0 ? priority : a.Sequence.CompareTo(b.Sequence);
        }
    }
}
