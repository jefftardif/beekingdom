using BeeKingdom.Hive;

namespace BeeKingdom.AI
{
    public sealed class BeeBrain
    {
        private readonly BeeBehaviorProfile profile;
        private readonly BeeBehaviorStateMachine stateMachine;
        private readonly BeeBehavior behavior;
        private TaskInstance task;
        private bool interrupted;

        public string BeeId { get; }
        public BeeBlackboard Blackboard { get; }

        public BeeBrain(string beeId, BeeBlackboard blackboard, BeeBehaviorProfile profile)
        {
            BeeId = beeId;
            Blackboard = blackboard;
            this.profile = profile;
            stateMachine = new BeeBehaviorStateMachine();
            behavior = new BeeBehavior();
        }

        public bool AssignTask(TaskInstance assignedTask)
        {
            if (Blackboard.State == BeeBehaviorState.Dead || assignedTask == null)
            {
                return false;
            }

            task = assignedTask;
            Blackboard.SetTask(assignedTask.TaskId, assignedTask.Definition.DefinitionId, true);
            ChangeState(profile.ResolveState(assignedTask.Definition.TaskType));
            interrupted = false;
            return true;
        }

        public bool CancelTask()
        {
            task = null;
            Blackboard.ClearTask();
            ChangeState(BeeBehaviorState.Idle);
            return true;
        }

        public bool UpdateBehavior(double deltaSeconds)
        {
            if (interrupted)
            {
                ChangeState(BeeBehaviorState.Waiting);
                return false;
            }

            bool completed = behavior.Execute(this, new BeeDecisionContext(deltaSeconds, task));
            if (completed)
            {
                task = null;
                Blackboard.ClearTask();
                ChangeState(BeeBehaviorState.Idle);
            }

            return completed;
        }

        public void Interrupt()
        {
            interrupted = true;
            ChangeState(BeeBehaviorState.Waiting);
        }

        public void Resume()
        {
            interrupted = false;
            if (task != null)
            {
                ChangeState(profile.ResolveState(task.Definition.TaskType));
            }
        }

        public BeeBehaviorState GetCurrentState()
        {
            return Blackboard.State;
        }

        public bool ChangeState(BeeBehaviorState state)
        {
            if (!stateMachine.CanTransition(Blackboard.State, state))
            {
                return false;
            }

            Blackboard.SetState(state);
            return true;
        }
    }
}
