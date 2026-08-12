using BeeKingdom.Colony;
using BeeKingdom.Colony.DependencyInjection;
using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Repositories;
using BeeKingdom.Colony.Snapshots;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class ColonyTests
{
    [Test]
    public void CreateColonyStoresProfileAndHistory()
    {
        ColonyManager colonies = CreateProvider().GetRequiredService<ColonyManager>();
        CreateColonyRequest request = CreateRequest("First Hive");

        ColonyRecord colony = colonies.CreateColony(request);

        Assert.Multiple(() =>
        {
            Assert.That(colony.Profile.ColonyId.Value, Is.Not.EqualTo(Guid.Empty));
            Assert.That(colony.Profile.PlayerId, Is.EqualTo(request.PlayerId));
            Assert.That(colony.Profile.HiveName, Is.EqualTo("First Hive"));
            Assert.That(colony.Profile.Status, Is.EqualTo(ColonyStatus.Creating));
            Assert.That(colony.History, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void StatusTransitionsAreValidated()
    {
        ColonyManager colonies = CreateProvider().GetRequiredService<ColonyManager>();
        ColonyRecord colony = colonies.CreateColony(CreateRequest("Transition Hive"));

        ColonyRecord active = colonies.SetColonyStatus(colony.Profile.ColonyId, ColonyStatus.Active);

        Assert.Multiple(() =>
        {
            Assert.That(active.Profile.Status, Is.EqualTo(ColonyStatus.Active));
            Assert.That(colonies.Diagnostics.ActiveColonies, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => colonies.SetColonyStatus(colony.Profile.ColonyId, ColonyStatus.Creating));
        });
    }

    [Test]
    public void SaveColonyCreatesFullAndIncrementalSnapshots()
    {
        ServiceProvider provider = CreateProvider();
        ColonyManager colonies = provider.GetRequiredService<ColonyManager>();
        IColonyRepository repository = provider.GetRequiredService<IColonyRepository>();
        ColonyRecord colony = colonies.CreateColony(CreateRequest("Snapshot Hive"));

        ColonySnapshot full = colonies.SaveColony(colony.Profile.ColonyId);
        ColonySnapshot incremental = colonies.SaveColony(colony.Profile.ColonyId, ColonySnapshotKind.Incremental);

        Assert.Multiple(() =>
        {
            Assert.That(full.Kind, Is.EqualTo(ColonySnapshotKind.Full));
            Assert.That(full.BaseRevision, Is.EqualTo(0));
            Assert.That(incremental.Kind, Is.EqualTo(ColonySnapshotKind.Incremental));
            Assert.That(incremental.BaseRevision, Is.EqualTo(1));
            Assert.That(repository.GetLatestSnapshot(colony.Profile.ColonyId), Is.EqualTo(incremental));
            Assert.That(colonies.Diagnostics.SnapshotCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void SnapshotPayloadRestoresColonyRecordDeterministically()
    {
        ColonyManager colonies = CreateProvider().GetRequiredService<ColonyManager>();
        ColonyRecord colony = colonies.CreateColony(CreateRequest("Restore Hive"));

        ColonySnapshot snapshot = colonies.SaveColony(colony.Profile.ColonyId);
        ColonyRecord restored = System.Text.Json.JsonSerializer.Deserialize<ColonyRecord>(snapshot.Payload, BeeJson.CreateDefaultOptions())!;

        Assert.Multiple(() =>
        {
            Assert.That(restored.Profile.ColonyId, Is.EqualTo(colony.Profile.ColonyId));
            Assert.That(restored.Revision, Is.EqualTo(snapshot.Revision));
            Assert.That(restored.History.Last().EventType, Is.EqualTo("Saved"));
        });
    }

    [Test]
    public void RenameAndDeleteUpdateStateWithoutSimulationLogic()
    {
        ColonyManager colonies = CreateProvider().GetRequiredService<ColonyManager>();
        ColonyRecord colony = colonies.CreateColony(CreateRequest("Old Hive"));

        ColonyRecord renamed = colonies.RenameColony(colony.Profile.ColonyId, "New Hive");
        ColonyRecord deleted = colonies.DeleteColony(colony.Profile.ColonyId);

        Assert.Multiple(() =>
        {
            Assert.That(renamed.Profile.HiveName, Is.EqualTo("New Hive"));
            Assert.That(deleted.Profile.Status, Is.EqualTo(ColonyStatus.Deleted));
            Assert.Throws<InvalidOperationException>(() => colonies.RenameColony(colony.Profile.ColonyId, "Late Hive"));
        });
    }

    [Test]
    public void QueryColonyFiltersByPlayerStatusAndName()
    {
        ColonyManager colonies = CreateProvider().GetRequiredService<ColonyManager>();
        PlayerId player = PlayerId.New();
        colonies.CreateColony(new CreateColonyRequest(player, Guid.NewGuid(), "Honey North", BeeId.New()));
        ColonyRecord second = colonies.CreateColony(new CreateColonyRequest(player, Guid.NewGuid(), "Wax South", BeeId.New()));
        colonies.SetColonyStatus(second.Profile.ColonyId, ColonyStatus.Active);

        IReadOnlyList<ColonyRecord> matches = colonies.QueryColony(new ColonyQuery(player, ColonyStatus.Active, "wax"));

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].Profile.HiveName, Is.EqualTo("Wax South"));
    }

    [Test]
    public void ConcurrentCreationKeepsDistinctColonies()
    {
        ColonyManager colonies = CreateProvider().GetRequiredService<ColonyManager>();
        PlayerId player = PlayerId.New();

        Parallel.For(0, 32, index =>
        {
            colonies.CreateColony(new CreateColonyRequest(player, Guid.NewGuid(), "Hive " + index, BeeId.New()));
        });

        Assert.That(colonies.QueryColony(new ColonyQuery(PlayerId: player)), Has.Count.EqualTo(32));
    }

    private static CreateColonyRequest CreateRequest(string name)
    {
        return new CreateColonyRequest(PlayerId.New(), Guid.NewGuid(), name, BeeId.New());
    }

    private static ServiceProvider CreateProvider(int maxSnapshotBytes = 1048576)
    {
        Dictionary<string, string?> values = new()
        {
            ["Colony:MaxSnapshotBytes"] = maxSnapshotBytes.ToString(),
            ["Colony:AutoSaveInterval"] = "00:05:00",
            ["Colony:CompressionPolicy"] = "None",
            ["Colony:RetentionDays"] = "30",
            ["Colony:VersioningStrategy"] = "Semantic"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomColony(configuration)
            .BuildServiceProvider();
    }
}
