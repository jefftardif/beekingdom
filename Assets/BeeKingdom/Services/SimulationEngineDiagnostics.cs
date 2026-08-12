using System;

namespace BeeKingdom.Services
{
    public sealed class SimulationEngineDiagnostics
    {
        public long TicksExecuted { get; private set; }
        public long TotalTickTicks { get; private set; }
        public long LastTickTicks { get; private set; }
        public int ErrorCount { get; private set; }
        public Exception LastError { get; private set; }
        public double AverageTickTicks => TicksExecuted == 0 ? 0d : (double)TotalTickTicks / TicksExecuted;

        public void RecordTick(long elapsedTicks)
        {
            TicksExecuted++;
            LastTickTicks = elapsedTicks;
            TotalTickTicks += elapsedTicks;
        }

        public void RecordError(Exception exception)
        {
            ErrorCount++;
            LastError = exception;
        }

        public void Reset()
        {
            TicksExecuted = 0;
            TotalTickTicks = 0;
            LastTickTicks = 0;
            ErrorCount = 0;
            LastError = null;
        }
    }
}
