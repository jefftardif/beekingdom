using BeeKingdom.Shared.Catalogs;
using BeeKingdom.Shared.Persistence;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Tests.Fakes;

namespace BeeKingdom.Tests;

public sealed class HiveLoopInMemoryRepositoryFakeTests
{
    [Test]
    public void FakeDescriptorRemainsNonLiveReadinessAndLocalOnly()
    {
        HiveLoopInMemoryReadinessRepositoryFake repository = new();
        HiveLoopRepositoryReadinessValidationResult validation = repository.Descriptor.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(repository.Descriptor.NonLive, Is.True);
            Assert.That(repository.Descriptor.ReadinessOnly, Is.True);
            Assert.That(repository.Descriptor.LocalOnly, Is.True);
            Assert.That(repository.Descriptor.ProductionSqlAllowed, Is.False);
            Assert.That(repository.Descriptor.EndpointExposed, Is.False);
            Assert.That(repository.Descriptor.OfficialProgressionAllowed, Is.False);
            Assert.That(validation.IsValid, Is.True, string.Join(Environment.NewLine, validation.Errors));
        });
    }

    [Test]
    public async Task FakeReadsResourcesBuildingsQueuesTroopsAndIdempotency()
    {
        HiveLoopInMemoryReadinessRepositoryFake repository = new();
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        BuildingId buildingId = BuildingId.New();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        repository.SeedResource(new HivePlayerResourceReadinessRecord(playerId, worldId, gameServerId, "honey", Amount: 100, Capacity: 500, Revision: 1, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: false, NonLive: false));
        repository.SeedBuilding(new HiveBuildingReadinessRecord(playerId, worldId, gameServerId, buildingId, "honey_storage", Level: 1, Revision: 1, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: false, NonLive: false));
        repository.SeedTroopCount(new HiveTroopCountReadinessRecord(playerId, worldId, gameServerId, "worker_bee", Quantity: 2, ArmyRevision: 1, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: false, NonLive: false));
        await repository.RecordIdempotencyResultAsync(CreateIdempotencyIntent(playerId, worldId, gameServerId, "idem-read", "payload-read", now));
        await repository.TryReserveUpgradeAsync(CreateUpgradeIntent(playerId, worldId, gameServerId, buildingId, "upgrade-key", "upgrade-payload"));
        await repository.TryReserveTrainingAsync(CreateTrainingIntent(playerId, worldId, gameServerId, "training-key", "training-payload"));

        IReadOnlyList<HivePlayerResourceReadinessRecord> resources = await repository.ReadPlayerResourcesAsync(playerId, worldId, gameServerId);
        IReadOnlyList<HiveBuildingReadinessRecord> buildings = await repository.ReadHiveBuildingsAsync(playerId, worldId, gameServerId);
        IReadOnlyList<HiveConstructionQueueReadinessRecord> construction = await repository.ReadConstructionQueueAsync(playerId, worldId, gameServerId);
        IReadOnlyList<HiveTroopCountReadinessRecord> troops = await repository.ReadTroopCountsAsync(playerId, worldId, gameServerId);
        IReadOnlyList<HiveTrainingQueueReadinessRecord> training = await repository.ReadTrainingQueueAsync(playerId, worldId, gameServerId);
        HiveIdempotencyReadinessRecord? idempotency = await repository.ReadIdempotencyRecordAsync(playerId, worldId, gameServerId, "idem-read");

        Assert.Multiple(() =>
        {
            Assert.That(resources.Single().ReadOnly, Is.True);
            Assert.That(resources.Single().NonLive, Is.True);
            Assert.That(buildings.Single().ReadOnly, Is.True);
            Assert.That(construction.Single().NonLive, Is.True);
            Assert.That(troops.Single().Quantity, Is.EqualTo(2));
            Assert.That(training.Single().NonLive, Is.True);
            Assert.That(idempotency, Is.Not.Null);
            Assert.That(idempotency!.NonLive, Is.True);
        });
    }

    [Test]
    public async Task FakeIdempotencyDistinguishesSamePayloadFromDifferentPayload()
    {
        HiveLoopInMemoryReadinessRepositoryFake repository = new();
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        HiveLoopIdempotencyRecordResult first = await repository.RecordIdempotencyResultAsync(CreateIdempotencyIntent(playerId, worldId, gameServerId, "idem-key", "payload-a", now));
        HiveLoopIdempotencyRecordResult samePayload = await repository.RecordIdempotencyResultAsync(CreateIdempotencyIntent(playerId, worldId, gameServerId, "idem-key", "payload-a", now));
        HiveLoopIdempotencyRecordResult differentPayload = await repository.RecordIdempotencyResultAsync(CreateIdempotencyIntent(playerId, worldId, gameServerId, "idem-key", "payload-b", now));

        Assert.Multiple(() =>
        {
            Assert.That(first.Recorded, Is.True);
            Assert.That(samePayload.Recorded, Is.False);
            Assert.That(samePayload.ResultCode, Is.EqualTo("ReadinessIdempotencyReplay"));
            Assert.That(differentPayload.Recorded, Is.False);
            Assert.That(differentPayload.ValidationErrors, Does.Contain("IdempotencyDifferentPayload"));
            Assert.That(new[] { first.LiveSqlWriteApplied, samePayload.LiveSqlWriteApplied, differentPayload.LiveSqlWriteApplied }, Is.All.False);
            Assert.That(new[] { first.OfficialProgressionApplied, samePayload.OfficialProgressionApplied, differentPayload.OfficialProgressionApplied }, Is.All.False);
        });
    }

    [Test]
    public async Task FakeReserveUpgradeAndTrainingOnlyMutateFakeQueues()
    {
        HiveLoopInMemoryReadinessRepositoryFake repository = new();
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        BuildingId buildingId = BuildingId.New();
        repository.SeedResource(new HivePlayerResourceReadinessRecord(playerId, worldId, gameServerId, "honey", Amount: 100, Capacity: 500, Revision: 1, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: true, NonLive: true));
        repository.SeedTroopCount(new HiveTroopCountReadinessRecord(playerId, worldId, gameServerId, "worker_bee", Quantity: 0, ArmyRevision: 1, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: true, NonLive: true));

        HiveLoopRepositoryReservationResult upgrade = await repository.TryReserveUpgradeAsync(CreateUpgradeIntent(playerId, worldId, gameServerId, buildingId, "upgrade-key", "upgrade-payload"));
        HiveLoopRepositoryReservationResult training = await repository.TryReserveTrainingAsync(CreateTrainingIntent(playerId, worldId, gameServerId, "training-key", "training-payload"));

        IReadOnlyList<HiveConstructionQueueReadinessRecord> construction = await repository.ReadConstructionQueueAsync(playerId, worldId, gameServerId);
        IReadOnlyList<HiveTrainingQueueReadinessRecord> trainingQueue = await repository.ReadTrainingQueueAsync(playerId, worldId, gameServerId);
        IReadOnlyList<HivePlayerResourceReadinessRecord> resources = await repository.ReadPlayerResourcesAsync(playerId, worldId, gameServerId);
        IReadOnlyList<HiveTroopCountReadinessRecord> troops = await repository.ReadTroopCountsAsync(playerId, worldId, gameServerId);

        Assert.Multiple(() =>
        {
            Assert.That(upgrade.Accepted, Is.True);
            Assert.That(training.Accepted, Is.True);
            Assert.That(construction, Has.Count.EqualTo(1));
            Assert.That(trainingQueue, Has.Count.EqualTo(1));
            Assert.That(resources.Single().Amount, Is.EqualTo(100));
            Assert.That(troops.Single().Quantity, Is.EqualTo(0));
            Assert.That(repository.FakeWriteCount, Is.EqualTo(2));
            Assert.That(new[] { upgrade.OfficialProgressionApplied, training.OfficialProgressionApplied }, Is.All.False);
            Assert.That(new[] { upgrade.LiveSqlWriteApplied, training.LiveSqlWriteApplied }, Is.All.False);
        });
    }

    [Test]
    public async Task FakeCompleteDueQueuesMarksFakeQueuesReadyWithoutOfficialProgression()
    {
        HiveLoopInMemoryReadinessRepositoryFake repository = new();
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        BuildingId buildingId = BuildingId.New();

        await repository.TryReserveUpgradeAsync(CreateUpgradeIntent(playerId, worldId, gameServerId, buildingId, "upgrade-key", "upgrade-payload"));
        await repository.TryReserveTrainingAsync(CreateTrainingIntent(playerId, worldId, gameServerId, "training-key", "training-payload"));

        HiveLoopDueQueueCompletionResult result = await repository.CompleteDueQueuesAsync(new HiveLoopDueQueueCompletionIntent(
            worldId,
            gameServerId,
            DateTimeOffset.UtcNow.AddHours(1),
            MaxItems: 10,
            NonLive: true,
            ReadinessOnly: true));
        IReadOnlyList<HiveConstructionQueueReadinessRecord> readyConstruction = await repository.ReadConstructionQueueAsync(playerId, worldId, gameServerId, HiveLoopQueueItemReadinessStatus.Ready);
        IReadOnlyList<HiveTrainingQueueReadinessRecord> readyTraining = await repository.ReadTrainingQueueAsync(playerId, worldId, gameServerId, HiveLoopQueueItemReadinessStatus.Ready);

        Assert.Multiple(() =>
        {
            Assert.That(result.QueueItemIds, Has.Count.EqualTo(2));
            Assert.That(result.ConstructionItemsConsidered, Is.EqualTo(1));
            Assert.That(result.TrainingItemsConsidered, Is.EqualTo(1));
            Assert.That(readyConstruction, Has.Count.EqualTo(1));
            Assert.That(readyTraining, Has.Count.EqualTo(1));
            Assert.That(result.NonLive, Is.True);
            Assert.That(result.ReadinessOnly, Is.True);
            Assert.That(result.OfficialProgressionApplied, Is.False);
            Assert.That(result.LiveSqlWriteApplied, Is.False);
        });
    }

    [Test]
    public void FakeSourceDoesNotUseSqlApis()
    {
        string sourcePath = FindRepositoryFile("Server", "tests", "BeeKingdom.Tests", "Fakes", "HiveLoopInMemoryReadinessRepositoryFake.cs");
        string source = File.ReadAllText(sourcePath);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("Microsoft.Data.SqlClient"));
            Assert.That(source, Does.Not.Contain("SqlConnection"));
            Assert.That(source, Does.Not.Contain("SqlCommand"));
            Assert.That(source, Does.Not.Contain("ExecuteNonQuery"));
            Assert.That(source, Does.Not.Contain("ExecuteReader"));
        });
    }

    private static HiveLoopUpgradeReservationIntent CreateUpgradeIntent(PlayerId playerId, WorldId worldId, GameServerId gameServerId, BuildingId buildingId, string idempotencyKeyHash, string payloadHash)
    {
        return new HiveLoopUpgradeReservationIntent(
            Guid.NewGuid(),
            playerId,
            worldId,
            gameServerId,
            buildingId,
            "honey_storage",
            FromLevel: 0,
            ToLevel: 1,
            ExpectedResourceRevision: 1,
            ExpectedBuildingRevision: 1,
            idempotencyKeyHash,
            payloadHash,
            HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion,
            NonLive: true,
            ReadinessOnly: true);
    }

    private static HiveLoopTrainingReservationIntent CreateTrainingIntent(PlayerId playerId, WorldId worldId, GameServerId gameServerId, string idempotencyKeyHash, string payloadHash)
    {
        return new HiveLoopTrainingReservationIntent(
            Guid.NewGuid(),
            playerId,
            worldId,
            gameServerId,
            "worker_bee",
            Quantity: 2,
            ExpectedResourceRevision: 1,
            ExpectedArmyRevision: 1,
            idempotencyKeyHash,
            payloadHash,
            HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion,
            NonLive: true,
            ReadinessOnly: true);
    }

    private static HiveLoopIdempotencyRecordIntent CreateIdempotencyIntent(PlayerId playerId, WorldId worldId, GameServerId gameServerId, string idempotencyKeyHash, string payloadHash, DateTimeOffset now)
    {
        return new HiveLoopIdempotencyRecordIntent(
            playerId,
            worldId,
            gameServerId,
            idempotencyKeyHash,
            payloadHash,
            "HiveLoopReadinessTest",
            "result-hash",
            now,
            now.AddHours(1),
            NonLive: true,
            ReadinessOnly: true);
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{Path.Combine(segments)}'.");
    }
}
