using System;

namespace BeeKingdom.Core.Simulation
{
    public sealed class SimulationDiagnostics
    {
        public long TotalTicks { get; private set; }
        public long LastTickElapsedTicks { get; private set; }
        public int ActiveSystemCount { get; private set; }
        public Type[] ExecutionOrder { get; private set; } = Array.Empty<Type>();
        public Type[] DisabledSystems { get; private set; } = Array.Empty<Type>();
        public SimulationSystemTiming[] SystemTimings { get; private set; } = Array.Empty<SimulationSystemTiming>();

        public void RecordTick(long elapsedTicks)
        {
            TotalTicks++;
            LastTickElapsedTicks = elapsedTicks;
        }

        public void SetPipeline(Type[] executionOrder, Type[] disabledSystems, SimulationSystemTiming[] systemTimings)
        {
            ExecutionOrder = executionOrder ?? Array.Empty<Type>();
            DisabledSystems = disabledSystems ?? Array.Empty<Type>();
            SystemTimings = systemTimings ?? Array.Empty<SimulationSystemTiming>();
            ActiveSystemCount = ExecutionOrder.Length;
        }
    }

    public readonly struct SimulationSystemTiming
    {
        public Type SystemType { get; }
        public long LastExecutionTicks { get; }

        public SimulationSystemTiming(Type systemType, long lastExecutionTicks)
        {
            SystemType = systemType;
            LastExecutionTicks = lastExecutionTicks;
        }
    }
}
