using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationSchedulerTests
    {
        [Test]
        public void ExecuteTickUsesPriorityOrder()
        {
            SimulationScheduler scheduler = CreateStartedScheduler(out _, out _);
            List<string> order = new List<string>();

            scheduler.RegisterSystem(new TestSimulationSystem(typeof(SlowSystem), "slow", 20, order));
            scheduler.RegisterSystem(new TestSimulationSystem(typeof(FastSystem), "fast", 10, order));

            scheduler.ExecuteTick(CreateContext());

            Assert.That(order, Is.EqualTo(new[] { "fast", "slow" }));
        }

        [Test]
        public void ExecuteTickRespectsRunsAfterDependencies()
        {
            SimulationScheduler scheduler = CreateStartedScheduler(out _, out _);
            List<string> order = new List<string>();

            scheduler.RegisterSystem(new TestSimulationSystem(typeof(AfterSystem), "after", 0, order, runsAfter: new[] { typeof(BeforeSystem) }));
            scheduler.RegisterSystem(new TestSimulationSystem(typeof(BeforeSystem), "before", 100, order));

            scheduler.ExecuteTick(CreateContext());

            Assert.That(order, Is.EqualTo(new[] { "before", "after" }));
        }

        [Test]
        public void ExecuteTickRespectsRunsBeforeDependencies()
        {
            SimulationScheduler scheduler = CreateStartedScheduler(out _, out _);
            List<string> order = new List<string>();

            scheduler.RegisterSystem(new TestSimulationSystem(typeof(BeforeSystem), "before", 100, order, runsBefore: new[] { typeof(AfterSystem) }));
            scheduler.RegisterSystem(new TestSimulationSystem(typeof(AfterSystem), "after", 0, order));

            scheduler.ExecuteTick(CreateContext());

            Assert.That(order, Is.EqualTo(new[] { "before", "after" }));
        }

        [Test]
        public void DisableSystemSkipsExecutionAndDiagnosticsExposeDisabledSystems()
        {
            SimulationScheduler scheduler = CreateStartedScheduler(out _, out _);
            List<string> order = new List<string>();

            scheduler.RegisterSystem(new TestSimulationSystem(typeof(FastSystem), "fast", 10, order));
            scheduler.RegisterSystem(new TestSimulationSystem(typeof(SlowSystem), "slow", 20, order));
            scheduler.DisableSystem(typeof(FastSystem));

            scheduler.ExecuteTick(CreateContext());

            Assert.That(order, Is.EqualTo(new[] { "slow" }));
            Assert.That(scheduler.Diagnostics.ActiveSystemCount, Is.EqualTo(1));
            Assert.That(scheduler.Diagnostics.DisabledSystems, Is.EqualTo(new[] { typeof(FastSystem) }));
        }

        [Test]
        public void CircularDependenciesAreRejected()
        {
            SimulationScheduler scheduler = CreateStartedScheduler(out _, out _);
            List<string> order = new List<string>();

            scheduler.RegisterSystem(new TestSimulationSystem(typeof(BeforeSystem), "before", 0, order, runsAfter: new[] { typeof(AfterSystem) }));
            scheduler.RegisterSystem(new TestSimulationSystem(typeof(AfterSystem), "after", 0, order, runsAfter: new[] { typeof(BeforeSystem) }));

            Assert.Throws<InvalidOperationException>(() => scheduler.ExecuteTick(CreateContext()));
        }

        [Test]
        public void LongSimulationRemainsStable()
        {
            SimulationScheduler scheduler = CreateStartedScheduler(out _, out _);
            List<string> order = new List<string>();

            scheduler.RegisterSystem(new TestSimulationSystem(typeof(FastSystem), "fast", 10, order));

            for (int i = 0; i < 2000; i++)
            {
                scheduler.ExecuteTick(CreateContext());
            }

            Assert.That(order.Count, Is.EqualTo(2000));
            Assert.That(scheduler.Diagnostics.TotalTicks, Is.EqualTo(2000));
        }

        [Test]
        public void TimeEngineTickExecutesScheduler()
        {
            SimulationScheduler scheduler = CreateStartedScheduler(out _, out UnityTimeService timeService);
            List<string> order = new List<string>();
            scheduler.RegisterSystem(new TestSimulationSystem(typeof(FastSystem), "fast", 10, order));

            timeService.Tick(0.25f);

            Assert.That(order, Is.EqualTo(new[] { "fast" }));
            Assert.That(scheduler.Diagnostics.TotalTicks, Is.EqualTo(1));
        }

        private static SimulationScheduler CreateStartedScheduler(out EventBus eventBus, out UnityTimeService timeService)
        {
            ServiceContainer container = new ServiceContainer();
            eventBus = new EventBus();
            timeService = new UnityTimeService();
            SimulationScheduler scheduler = new SimulationScheduler();

            container.Register<IEventBus>(eventBus);
            container.Register<ITimeService>(timeService);
            container.Register<ISimulationScheduler>(scheduler);

            eventBus.Initialize(container);
            eventBus.Start();
            timeService.Initialize(container);
            timeService.Start();
            scheduler.Initialize(container);
            scheduler.Start();
            return scheduler;
        }

        private static SimulationExecutionContext CreateContext()
        {
            return new SimulationExecutionContext(
                new SimulationTimestamp(1, 1d),
                new SimulationCalendar(1, 0, 0, SimulationSeason.Spring),
                SimulationTickFrequency.EveryFrame,
                1d,
                null);
        }

        private sealed class TestSimulationSystem : ISimulationSystem
        {
            private static readonly Type[] EmptyTypes = Array.Empty<Type>();
            private readonly List<string> executionOrder;

            public Type SystemType { get; }
            public string Name { get; }
            public SimulationPhase Phase { get; }
            public int Priority { get; }
            public IReadOnlyList<Type> RunsAfter { get; }
            public IReadOnlyList<Type> RunsBefore { get; }

            public TestSimulationSystem(
                Type systemType,
                string name,
                int priority,
                List<string> executionOrder,
                SimulationPhase phase = SimulationPhase.Simulation,
                IReadOnlyList<Type> runsAfter = null,
                IReadOnlyList<Type> runsBefore = null)
            {
                SystemType = systemType;
                Name = name;
                Priority = priority;
                this.executionOrder = executionOrder;
                Phase = phase;
                RunsAfter = runsAfter ?? EmptyTypes;
                RunsBefore = runsBefore ?? EmptyTypes;
            }

            public void Execute(in SimulationExecutionContext context)
            {
                executionOrder.Add(Name);
            }
        }

        private sealed class FastSystem { }
        private sealed class SlowSystem { }
        private sealed class BeforeSystem { }
        private sealed class AfterSystem { }
    }
}
