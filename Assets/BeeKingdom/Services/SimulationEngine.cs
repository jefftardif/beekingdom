using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Data;

namespace BeeKingdom.Services
{
    public sealed class SimulationEngine : GameServiceBase, ISimulationEngine
    {
        private readonly SimulationBootstrap bootstrap;
        private SimulationContext context;
        private bool isRunning;
        private bool isPaused;

        public override int Priority => 90;
        public override IReadOnlyList<Type> Dependencies => new[]
        {
            typeof(ITimeService),
            typeof(ISimulationScheduler),
            typeof(IEventBus),
            typeof(ISaveService),
            typeof(IDataRegistry)
        };

        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public SimulationContext Context => context;
        public SimulationEngineDiagnostics Diagnostics { get; } = new SimulationEngineDiagnostics();

        public SimulationEngine()
            : this(new SimulationBootstrap())
        {
        }

        public SimulationEngine(SimulationBootstrap bootstrap)
        {
            this.bootstrap = bootstrap;
        }

        protected override void OnInitialize(IServiceRegistry services)
        {
            context = bootstrap.CreateContext(services);
        }

        protected override void OnStart()
        {
            isRunning = true;
            isPaused = false;
        }

        protected override void OnTick(float deltaTime)
        {
            if (!isRunning || isPaused)
            {
                return;
            }

            long start = Stopwatch.GetTimestamp();
            try
            {
                context.World.AdvanceRevision();
                Diagnostics.RecordTick(Stopwatch.GetTimestamp() - start);
            }
            catch (Exception exception)
            {
                Diagnostics.RecordError(exception);
                throw;
            }
        }

        protected override void OnPause()
        {
            isPaused = true;
        }

        protected override void OnResume()
        {
            isPaused = false;
        }

        protected override void OnShutdown()
        {
            Stop();
        }

        public void Stop()
        {
            isRunning = false;
        }

        public void Reset()
        {
            context?.World.Reset();
            Diagnostics.Reset();
            isPaused = false;
        }

        public SimulationStatistics GetStatistics()
        {
            int activeSystems = context?.Scheduler.Diagnostics.ActiveSystemCount ?? 0;
            long memory = context?.DataRegistry.Diagnostics.EstimatedMemoryBytes ?? 0;
            return new SimulationStatistics(
                Diagnostics.TicksExecuted,
                Diagnostics.AverageTickTicks,
                activeSystems,
                memory,
                Diagnostics.ErrorCount);
        }
    }
}
