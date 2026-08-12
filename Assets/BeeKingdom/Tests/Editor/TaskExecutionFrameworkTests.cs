using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class TaskExecutionFrameworkTests
    {
        [Test]
        public void CreateReserveAssignAndCompleteTask()
        {
            TaskExecutionManager manager = CreateManager();
            TaskInstance task = manager.CreateTask("build", "cell-1");

            Assert.That(task.State, Is.EqualTo(TaskExecutionState.Waiting));
            Assert.That(manager.ReserveTask(task.TaskId), Is.True);
            Assert.That(manager.AssignTask(task.TaskId, "bee-1"), Is.True);
            Assert.That(manager.ExecuteTask(task.TaskId, 2d), Is.EqualTo(TaskExecutionState.Completed));
        }

        [Test]
        public void PauseResumeAndCancelTask()
        {
            TaskExecutionManager manager = CreateManager();
            TaskInstance task = manager.CreateTask("build", "cell-1");

            Assert.That(manager.PauseTask(task.TaskId), Is.True);
            Assert.That(task.State, Is.EqualTo(TaskExecutionState.Paused));
            Assert.That(manager.ResumeTask(task.TaskId), Is.True);
            Assert.That(manager.CancelTask(task.TaskId), Is.True);
            Assert.That(task.State, Is.EqualTo(TaskExecutionState.Cancelled));
        }

        private static TaskExecutionManager CreateManager()
        {
            TaskExecutionManager manager = new TaskExecutionManager();
            manager.RegisterTaskDefinition(new TaskDefinition("build", TaskWorkType.Build, TaskPriority.Normal, 1d));
            return manager;
        }
    }
}
