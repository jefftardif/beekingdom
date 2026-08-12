using BeeKingdom.Colony;
using BeeKingdom.Colony.DependencyInjection;
using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Repositories;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Simulation;
using BeeKingdom.Simulation.DependencyInjection;
using BeeKingdom.Simulation.Models;
using BeeKingdom.Simulation.Scheduling;
using BeeKingdom.Simulation.Systems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class SimulationTests
{
    [Test]
    public void StartPauseResumeAndStopUpdateState()
    {
        SimulationManager simulation = CreateProvider().GetRequiredService<SimulationManager>();

        simulation.StartSimulation();
        simulation.PauseSimulation();
        SimulationState paused = simulation.State;
        simulation.ResumeSimulation();
        SimulationState resumed = simulation.State;
        simulation.StopSimulation();

        Assert.Multiple(() =>
        {
            Assert.That(paused, Is.EqualTo(SimulationState.Paused));
            Assert.That(resumed, Is.EqualTo(SimulationState.Running));
            Assert.That(simulation.State, Is.EqualTo(SimulationState.Stopped));
        });
    }

    [Test]
    public void ExecuteTickUsesStrictStageOrder()
    {
        ServiceProvider provider = CreateProvider();
        List<SimulationStage> observed = new();
        SimulationScheduler scheduler = provider.GetRequiredService<SimulationScheduler>();
        foreach (SimulationStage stage in Enum.GetValues<SimulationStage>().OrderByDescending(stage => stage))
        {
            scheduler.Register(new RecordingSystem(stage, observed));
        }

        SimulationManager simulation = provider.GetRequiredService<SimulationManager>();
        ColonyRecord colony = CreateAndLoadColony(provider, simulation);
        simulation.StartSimulation();

        SimulationTickResult result = simulation.ExecuteTick().Single();

        SimulationStage[] expected = Enum.GetValues<SimulationStage>().OrderBy(stage => stage).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(colony.Profile.ColonyId, Is.EqualTo(result.ColonyId));
            Assert.That(result.ExecutedStages, Is.EqualTo(expected));
            Assert.That(observed, Is.EqualTo(expected));
        });
    }

    [Test]
    public void PausePreventsTickExecutionUntilResume()
    {
        ServiceProvider provider = CreateProvider();
        SimulationManager simulation = provider.GetRequiredService<SimulationManager>();
        CreateAndLoadColony(provider, simulation);
        simulation.StartSimulation();
        simulation.PauseSimulation();

        IReadOnlyList<SimulationTickResult> paused = simulation.ExecuteTick();
        simulation.ResumeSimulation();
        IReadOnlyList<SimulationTickResult> resumed = simulation.ExecuteTick();

        Assert.Multiple(() =>
        {
            Assert.That(paused, Is.Empty);
            Assert.That(resumed, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void LoadAndUnloadColonyUpdatesDiagnostics()
    {
        ServiceProvider provider = CreateProvider();
        SimulationManager simulation = provider.GetRequiredService<SimulationManager>();
        ColonyRecord colony = CreateColony(provider, "Loaded Hive");

        simulation.LoadColony(colony.Profile.ColonyId);
        bool unloaded = simulation.UnloadColony(colony.Profile.ColonyId);

        Assert.Multiple(() =>
        {
            Assert.That(unloaded, Is.True);
            Assert.That(simulation.GetLoadedColonies(), Is.Empty);
            Assert.That(simulation.Diagnostics.ColoniesLoaded, Is.EqualTo(0));
        });
    }

    [Test]
    public void SaveCheckProducesIncrementalSnapshotOnConfiguredTick()
    {
        ServiceProvider provider = CreateProvider(autoSaveEveryTicks: 2);
        SimulationManager simulation = provider.GetRequiredService<SimulationManager>();
        ColonyRecord colony = CreateAndLoadColony(provider, simulation);
        simulation.StartSimulation();

        SimulationTickResult first = simulation.ExecuteTick().Single();
        SimulationTickResult second = simulation.ExecuteTick().Single();

        Assert.Multiple(() =>
        {
            Assert.That(first.SnapshotProduced, Is.False);
            Assert.That(second.SnapshotProduced, Is.True);
            Assert.That(provider.GetRequiredService<IColonyRepository>().GetLatestSnapshot(colony.Profile.ColonyId), Is.Not.Null);
            Assert.That(simulation.Diagnostics.SnapshotsProduced, Is.EqualTo(1));
        });
    }

    [Test]
    public void FixedTicksAreDeterministic()
    {
        SimulationTickResult first = ExecuteSingleFixedTick(CreateProvider());
        SimulationTickResult second = ExecuteSingleFixedTick(CreateProvider());

        Assert.Multiple(() =>
        {
            Assert.That(first.TickId, Is.EqualTo(second.TickId));
            Assert.That(first.Timestamp, Is.EqualTo(second.Timestamp));
            Assert.That(first.ExecutedStages, Is.EqualTo(second.ExecutedStages));
            Assert.That(first.SnapshotProduced, Is.EqualTo(second.SnapshotProduced));
        });
    }

    [Test]
    public void FastForwardExecutesRequestedFixedTicks()
    {
        ServiceProvider provider = CreateProvider(autoSaveEveryTicks: 100);
        SimulationManager simulation = provider.GetRequiredService<SimulationManager>();
        CreateAndLoadColony(provider, simulation);
        simulation.StartSimulation();

        IReadOnlyList<SimulationTickResult> results = simulation.FastForward(5);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(5));
            Assert.That(results.Select(result => result.TickId), Is.EqualTo(new long[] { 1, 2, 3, 4, 5 }));
        });
    }

    [Test]
    public void TickBatchCanSimulateManyLoadedColonies()
    {
        ServiceProvider provider = CreateProvider(maxColoniesPerTickBatch: 64);
        SimulationManager simulation = provider.GetRequiredService<SimulationManager>();
        for (int i = 0; i < 32; i++)
        {
            CreateAndLoadColony(provider, simulation, "Hive " + i);
        }

        simulation.StartSimulation();
        IReadOnlyList<SimulationTickResult> results = simulation.ExecuteTick();

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(32));
            Assert.That(simulation.Diagnostics.ColoniesSimulated, Is.EqualTo(32));
        });
    }

    private static SimulationTickResult ExecuteSingleFixedTick(ServiceProvider provider)
    {
        SimulationManager simulation = provider.GetRequiredService<SimulationManager>();
        CreateAndLoadColony(provider, simulation);
        simulation.StartSimulation();
        return simulation.ExecuteTick().Single();
    }

    private static ColonyRecord CreateAndLoadColony(ServiceProvider provider, SimulationManager simulation, string hiveName = "Simulation Hive")
    {
        ColonyRecord colony = CreateColony(provider, hiveName);
        simulation.LoadColony(colony.Profile.ColonyId);
        return colony;
    }

    private static ColonyRecord CreateColony(ServiceProvider provider, string hiveName)
    {
        ColonyManager colonies = provider.GetRequiredService<ColonyManager>();
        return colonies.CreateColony(new CreateColonyRequest(PlayerId.New(), Guid.NewGuid(), hiveName, BeeId.New()));
    }

    private static ServiceProvider CreateProvider(int autoSaveEveryTicks = 100, int maxColoniesPerTickBatch = 100)
    {
        Dictionary<string, string?> values = new()
        {
            ["Colony:MaxSnapshotBytes"] = "1048576",
            ["Colony:AutoSaveInterval"] = "00:05:00",
            ["Colony:CompressionPolicy"] = "None",
            ["Colony:RetentionDays"] = "30",
            ["Colony:VersioningStrategy"] = "Semantic",
            ["Simulation:FixedTickInterval"] = "00:00:01",
            ["Simulation:AutoSaveEveryTicks"] = autoSaveEveryTicks.ToString(),
            ["Simulation:InactiveUnloadAfter"] = "00:15:00",
            ["Simulation:MaxFastForwardTicks"] = "1000",
            ["Simulation:MaxColoniesPerTickBatch"] = maxColoniesPerTickBatch.ToString(),
            ["Simulation:SimulationEpochUtc"] = "1970-01-01T00:00:00+00:00"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomColony(configuration)
            .AddBeeKingdomSimulation(configuration)
            .BuildServiceProvider();
    }

    private sealed class RecordingSystem : ISimulationSystem
    {
        private readonly List<SimulationStage> observed;

        public RecordingSystem(SimulationStage stage, List<SimulationStage> observed)
        {
            Stage = stage;
            this.observed = observed;
        }

        public SimulationStage Stage { get; }
        public int Order => 0;
        public string Name => "Recording-" + Stage;
        public void Execute(SimulationContext context) => observed.Add(Stage);
    }
}
