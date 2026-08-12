using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class TaskQueue
    {
        private readonly List<TaskInstance> tasks = new List<TaskInstance>();

        public int Count => tasks.Count;

        public void Enqueue(TaskInstance task)
        {
            tasks.Add(task);
        }

        public bool Remove(TaskInstance task)
        {
            return tasks.Remove(task);
        }

        public TaskInstance GetBestAvailable()
        {
            TaskInstance best = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < tasks.Count; i++)
            {
                TaskInstance task = tasks[i];
                if (task.State != TaskLifecycleState.Queued)
                {
                    continue;
                }

                int score = task.Priority.Score;
                if (score > bestScore)
                {
                    best = task;
                    bestScore = score;
                }
            }

            return best;
        }

        public IReadOnlyList<TaskInstance> GetAll()
        {
            return tasks;
        }
    }
}
