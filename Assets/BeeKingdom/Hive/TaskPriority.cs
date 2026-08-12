namespace BeeKingdom.Hive
{
    public readonly struct TaskPriority
    {
        public int BasePriority { get; }
        public int DynamicPriority { get; }
        public int Urgency { get; }
        public int Score => BasePriority + DynamicPriority + Urgency;

        public TaskPriority(int basePriority, int dynamicPriority, int urgency)
        {
            BasePriority = basePriority;
            DynamicPriority = dynamicPriority;
            Urgency = urgency;
        }

        public TaskPriority WithDynamicPriority(int value)
        {
            return new TaskPriority(BasePriority, value, Urgency);
        }
    }
}
