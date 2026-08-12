using System;
using System.Collections.Generic;
using BeeKingdom.Config.Runtime;
using BeeKingdom.Core.Save;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Data;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationEngineTests
    {
        [Test]
        public void StartCreatesRunningEngineWithContext()
        {
            SimulationEngine engine = CreateStartedEngine();

            Assert.That(engine.IsRunning, Is.True);
            Assert.That(engine.Context, Is.Not.Null);
            Assert.That(engine.Context.TimeService, Is.Not.Null);
            Assert.That(engine.Context.Scheduler, Is.Not.Null);
            Assert.That(engine.Context.SaveService, Is.Not.Null);
            Assert.That(engine.Context.DataRegistry, Is.Not.Null);
        }

        [Test]
        public void TickUpdatesStatisticsAndWorldRevision()
        {
            SimulationEngine engine = CreateStartedEngine();

            engine.Tick(0.016f);

            Assert.That(engine.Context.World.Revision, Is.EqualTo(1));
            Assert.That(engine.GetStatistics().TicksExecuted, Is.EqualTo(1));
        }

        [Test]
        public void PauseStopsEngineTicksUntilResume()
        {
            SimulationEngine engine = CreateStartedEngine();

            engine.Pause();
            engine.Tick(0.016f);
            Assert.That(engine.GetStatistics().TicksExecuted, Is.EqualTo(0));

            engine.Resume();
            engine.Tick(0.016f);
            Assert.That(engine.GetStatistics().TicksExecuted, Is.EqualTo(1));
        }

        [Test]
        public void StopPreventsFurtherTicks()
        {
            SimulationEngine engine = CreateStartedEngine();

            engine.Stop();
            engine.Tick(0.016f);

            Assert.That(engine.GetStatistics().TicksExecuted, Is.EqualTo(0));
        }

        [Test]
        public void ResetClearsStatisticsAndWorld()
        {
            SimulationEngine engine = CreateStartedEngine();
            engine.Tick(0.016f);

            engine.Reset();

            Assert.That(engine.Context.World.Revision, Is.EqualTo(0));
            Assert.That(engine.GetStatistics().TicksExecuted, Is.EqualTo(0));
        }

        private static SimulationEngine CreateStartedEngine()
        {
            ServiceContainer container = new ServiceContainer();
            EventBus eventBus = new EventBus();
            UnityTimeService timeService = new UnityTimeService();
            SimulationScheduler scheduler = new SimulationScheduler();
            SaveEngine saveService = new SaveEngine(new InMemorySaveRepository());
            FakeDataRegistry dataRegistry = new FakeDataRegistry();
            SimulationEngine engine = new SimulationEngine();

            container.Register<IEventBus>(eventBus);
            container.Register<ITimeService>(timeService);
            container.Register<ISimulationScheduler>(scheduler);
            container.Register<ISaveService>(saveService);
            container.Register<IDataRegistry>(dataRegistry);
            container.Register<ISimulationEngine>(engine);

            eventBus.Initialize(container);
            eventBus.Start();
            timeService.Initialize(container);
            timeService.Start();
            scheduler.Initialize(container);
            scheduler.Start();
            saveService.Initialize(container);
            saveService.Start();
            dataRegistry.Initialize(container);
            dataRegistry.Start();
            engine.Initialize(container);
            engine.Start();
            return engine;
        }

        private sealed class FakeDataRegistry : IDataRegistry
        {
            public string ServiceName => nameof(FakeDataRegistry);
            public int Priority => 15;
            public ServiceState State { get; private set; } = ServiceState.Registered;
            public bool IsInitialized => State != ServiceState.Registered;
            public IReadOnlyList<Type> Dependencies => Array.Empty<Type>();
            public RegistryDiagnostics Diagnostics { get; } = new RegistryDiagnostics();

            public void Initialize(IServiceRegistry services) { State = ServiceState.Initialized; }
            public void Start() { State = ServiceState.Running; }
            public void Tick(float deltaTime) { }
            public void FixedTick(float deltaTime) { }
            public void LateTick(float deltaTime) { }
            public void Pause() { State = ServiceState.Paused; }
            public void Resume() { State = ServiceState.Running; }
            public void Shutdown() { State = ServiceState.Disposed; }
            public void Dispose() { Shutdown(); }
            public void Fail(Exception exception) { State = ServiceState.Failed; }

            public TDefinition Get<TDefinition>(string id) where TDefinition : class, IConfigurationDefinition
            {
                throw new KeyNotFoundException(id);
            }

            public bool TryGet<TDefinition>(string id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition
            {
                definition = null;
                return false;
            }

            public IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition
            {
                return Array.Empty<TDefinition>();
            }

            public bool Exists<TDefinition>(string id) where TDefinition : class, IConfigurationDefinition
            {
                return false;
            }

            public RegistryValidationResult Reload()
            {
                Diagnostics.RecordLoad(0, 0, 0);
                return new RegistryValidationResult(Array.Empty<RegistryValidationIssue>());
            }

            public RegistryValidationResult Validate()
            {
                return new RegistryValidationResult(Array.Empty<RegistryValidationIssue>());
            }
        }
    }
}
