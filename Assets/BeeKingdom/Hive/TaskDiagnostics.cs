namespace BeeKingdom.Hive
{
    public sealed class TaskDiagnostics
    {
        public TaskStatistics LastStatistics { get; private set; }
        public int ReservationCount { get; private set; }
        public int AssignmentCount { get; private set; }

        public void RecordStatistics(TaskStatistics statistics)
        {
            LastStatistics = statistics;
        }

        public void RecordReservation() { ReservationCount++; }
        public void RecordAssignment() { AssignmentCount++; }
    }
}
