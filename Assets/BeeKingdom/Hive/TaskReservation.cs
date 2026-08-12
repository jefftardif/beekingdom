namespace BeeKingdom.Hive
{
    public readonly struct TaskReservation
    {
        public string TaskId { get; }
        public string BeeId { get; }
        public double ExpiresAtSeconds { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(TaskId) && !string.IsNullOrWhiteSpace(BeeId);

        public TaskReservation(string taskId, string beeId, double expiresAtSeconds)
        {
            TaskId = taskId;
            BeeId = beeId;
            ExpiresAtSeconds = expiresAtSeconds;
        }

        public bool IsExpired(double nowSeconds)
        {
            return nowSeconds >= ExpiresAtSeconds;
        }
    }
}
