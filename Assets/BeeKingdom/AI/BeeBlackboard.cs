namespace BeeKingdom.AI
{
    public sealed class BeeBlackboard
    {
        public string CurrentTaskId { get; private set; }
        public string TargetId { get; private set; }
        public BeeBehaviorState State { get; private set; }
        public int Energy { get; private set; }
        public int Health { get; private set; }
        public bool HasReservation { get; private set; }

        public BeeBlackboard(int energy, int health)
        {
            Energy = energy;
            Health = health;
            State = BeeBehaviorState.Idle;
        }

        public void SetTask(string taskId, string targetId, bool hasReservation)
        {
            CurrentTaskId = taskId;
            TargetId = targetId;
            HasReservation = hasReservation;
        }

        public void ClearTask()
        {
            CurrentTaskId = null;
            TargetId = null;
            HasReservation = false;
        }

        public void SetState(BeeBehaviorState state)
        {
            State = state;
        }

        public void SetVitals(int energy, int health)
        {
            Energy = energy;
            Health = health;
            if (Health <= 0)
            {
                State = BeeBehaviorState.Dead;
            }
        }
    }
}
