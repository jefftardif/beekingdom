namespace BeeKingdom.Core.Workflows
{
    public sealed class WorkflowDiagnostics
    {
        public int Requested { get; private set; }
        public int Completed { get; private set; }
        public int Cancelled { get; private set; }
        public int Interrupted { get; private set; }
        public int Failed { get; private set; }
        public void RecordRequested() { Requested++; }
        public void RecordCompleted() { Completed++; }
        public void RecordCancelled() { Cancelled++; }
        public void RecordInterrupted() { Interrupted++; }
        public void RecordFailed() { Failed++; }
    }
}
