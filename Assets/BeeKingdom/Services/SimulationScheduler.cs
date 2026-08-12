using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;

namespace BeeKingdom.Services
{
    public sealed class SimulationScheduler : GameServiceBase, ISimulationScheduler
    {
        private readonly List<ISimulationSystem> systems = new List<ISimulationSystem>();
        private readonly List<SimulationPipelineEntry> entries = new List<SimulationPipelineEntry>();
        private readonly SimulationPipeline pipeline = new SimulationPipeline();
        private readonly SimulationDiagnostics diagnostics = new SimulationDiagnostics();
        private IServiceRegistry services;
        private IEventBus eventBus;
        private EventSubscription tickSubscription;
        private SimulationPipelineEntry[] executionOrder = Array.Empty<SimulationPipelineEntry>();
        private bool isDirty = true;
        private int registrationCounter;

        public override int Priority => 45;
        public override IReadOnlyList<Type> Dependencies => new[] { typeof(ITimeService), typeof(IEventBus) };
        public SimulationDiagnostics Diagnostics => diagnostics;

        protected override void OnInitialize(IServiceRegistry serviceRegistry)
        {
            services = serviceRegistry;
            eventBus = serviceRegistry.Get<IEventBus>();
        }

        protected override void OnStart()
        {
            tickSubscription = eventBus.Subscribe<TickGenerated>(OnTickGenerated);
        }

        protected override void OnShutdown()
        {
            tickSubscription?.Dispose();
            tickSubscription = null;
            systems.Clear();
            entries.Clear();
            executionOrder = Array.Empty<SimulationPipelineEntry>();
            isDirty = true;
        }

        public void RegisterSystem(ISimulationSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (FindEntry(system.SystemType) != null)
            {
                throw new InvalidOperationException($"Simulation system {system.SystemType.Name} is already registered.");
            }

            systems.Add(system);
            entries.Add(new SimulationPipelineEntry(system, registrationCounter++));
            MarkDirty();
        }

        public bool UnregisterSystem(Type systemType)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].System.SystemType == systemType)
                {
                    systems.Remove(entries[i].System);
                    entries.RemoveAt(i);
                    MarkDirty();
                    return true;
                }
            }

            return false;
        }

        public bool EnableSystem(Type systemType)
        {
            SimulationPipelineEntry entry = FindEntry(systemType);
            if (entry == null || entry.IsEnabled)
            {
                return false;
            }

            entry.IsEnabled = true;
            MarkDirty();
            return true;
        }

        public bool DisableSystem(Type systemType)
        {
            SimulationPipelineEntry entry = FindEntry(systemType);
            if (entry == null || !entry.IsEnabled)
            {
                return false;
            }

            entry.IsEnabled = false;
            MarkDirty();
            return true;
        }

        public void ExecuteTick(in SimulationExecutionContext context)
        {
            if (isDirty)
            {
                RebuildPipeline();
            }

            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < executionOrder.Length; i++)
            {
                SimulationPipelineEntry entry = executionOrder[i];
                long systemStart = Stopwatch.GetTimestamp();
                entry.System.Execute(context);
                entry.LastExecutionTicks = Stopwatch.GetTimestamp() - systemStart;
            }

            diagnostics.RecordTick(Stopwatch.GetTimestamp() - start);
            RefreshTimings();
        }

        public IReadOnlyList<ISimulationSystem> GetRegisteredSystems()
        {
            return systems;
        }

        private void OnTickGenerated(TickGenerated tick)
        {
            if (tick.Frequency != SimulationTickFrequency.EveryFrame)
            {
                return;
            }

            SimulationExecutionContext context = new SimulationExecutionContext(
                tick.Timestamp,
                services.Get<ITimeService>().Calendar,
                tick.Frequency,
                tick.DeltaSeconds,
                services);

            ExecuteTick(context);
        }

        private void MarkDirty()
        {
            isDirty = true;
        }

        private void RebuildPipeline()
        {
            executionOrder = pipeline.Build(entries);
            diagnostics.SetPipeline(
                BuildExecutionOrderTypes(),
                BuildDisabledSystemTypes(),
                new SimulationSystemTiming[entries.Count]);
            RefreshTimings();
            isDirty = false;
        }

        private Type[] BuildExecutionOrderTypes()
        {
            Type[] order = new Type[executionOrder.Length];
            for (int i = 0; i < executionOrder.Length; i++)
            {
                order[i] = executionOrder[i].System.SystemType;
            }

            return order;
        }

        private Type[] BuildDisabledSystemTypes()
        {
            int disabledCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsEnabled)
                {
                    disabledCount++;
                }
            }

            Type[] disabled = new Type[disabledCount];
            int index = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsEnabled)
                {
                    disabled[index++] = entries[i].System.SystemType;
                }
            }

            return disabled;
        }

        private void RefreshTimings()
        {
            SimulationSystemTiming[] timings = diagnostics.SystemTimings;
            if (timings.Length != entries.Count)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                timings[i] = new SimulationSystemTiming(entries[i].System.SystemType, entries[i].LastExecutionTicks);
            }
        }

        private SimulationPipelineEntry FindEntry(Type systemType)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].System.SystemType == systemType)
                {
                    return entries[i];
                }
            }

            return null;
        }
    }
}
