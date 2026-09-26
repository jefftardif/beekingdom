using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class SpeedUpInventoryServiceTests
{
    [Fact]
    public async Task ApplyConstructionSpeedUpConsumesAtomicallyAndClampsTimer()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState(clock.UtcNow, new Dictionary<string, int> { ["universal_60s"] = 1 }, new HiveOperation(Guid.NewGuid(), "honey_storage", 1, 2, clock.UtcNow, clock.UtcNow.AddMinutes(2), HiveOperationStatus.Running, "", 0, null)));
        var service = new SpeedUpInventoryService(repository, clock, Options());

        SpeedUpCommandResult result = await service.ApplyAsync(repository.State.PlayerId, repository.State.HiveId, new ApplySpeedUpRequest("universal_60s", SpeedUpCategories.Universal, "honey_storage", 60, 0, "apply-1"));

        Assert.True(result.Succeeded);
        Assert.Equal(1, repository.State.Revision);
        Assert.Equal(0, repository.State.SpeedUps!["universal_60s"]);
        Assert.Equal(clock.UtcNow.AddMinutes(1), repository.State.Operations[0].CompletesAtUtc);
    }

    [Fact]
    public async Task ReplayingSameIdempotencyKeyDoesNotConsumeAgain()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState(clock.UtcNow, new Dictionary<string, int> { ["universal_60s"] = 1 }, new HiveOperation(Guid.NewGuid(), "honey_storage", 1, 2, clock.UtcNow, clock.UtcNow.AddMinutes(2), HiveOperationStatus.Running, "", 0, null)));
        var service = new SpeedUpInventoryService(repository, clock, Options());
        var request = new ApplySpeedUpRequest("universal_60s", SpeedUpCategories.Universal, "honey_storage", 60, 0, "same-key");

        Assert.True((await service.ApplyAsync(repository.State.PlayerId, repository.State.HiveId, request)).Succeeded);
        SpeedUpCommandResult replay = await service.ApplyAsync(repository.State.PlayerId, repository.State.HiveId, request);

        Assert.True(replay.Succeeded);
        Assert.Equal(1, repository.State.Revision);
        Assert.Equal(0, repository.State.SpeedUps!["universal_60s"]);
    }

    [Fact]
    public async Task ConstructionReplayAfterReconnectReturnsOriginalReceiptAndCurrentSnapshot()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var operation = new HiveOperation(Guid.NewGuid(), "honey_storage", 1, 2, clock.UtcNow,
            clock.UtcNow.AddSeconds(60), HiveOperationStatus.Running, "", 0, null);
        var repository = new MemoryRepository(CreateState(clock.UtcNow,
            new Dictionary<string, int> { ["universal_60s"] = 2 }, operation));
        var request = new ApplySpeedUpRequest("universal_60s", SpeedUpCategories.Construction,
            "honey_storage", 60, 0, "reconnect");
        var first = await new SpeedUpInventoryService(repository, clock, Options())
            .ApplyAsync(repository.State.PlayerId, repository.State.HiveId, request);
        Assert.True(first.Succeeded);
        Assert.NotNull(first.Response);

        var upgrades = new BuildingUpgradeService(repository, clock, new BuildingUpgradeOptions
        {
            Enabled = true,
            CatalogVersion = "m095-test",
            Catalog = [new("honey_storage", 1, 2, TimeSpan.FromMinutes(1),
                new Dictionary<string, long> { ["honey"] = 20 })]
        });
        var completed = await upgrades.CompleteAsync(repository.State.PlayerId, repository.State.HiveId,
            operation.OperationId, new CompleteBuildingUpgradeRequest(1, "complete"));
        Assert.True(completed.Succeeded);

        var replay = await new SpeedUpInventoryService(repository, clock, Options())
            .ApplyAsync(repository.State.PlayerId, repository.State.HiveId, request);
        Assert.True(replay.Succeeded);
        Assert.NotNull(replay.Response);
        Assert.Equal(first.Response.Receipt, replay.Response.Receipt);
        Assert.Equal(2, replay.Response.Snapshot.Revision);
        Assert.Empty(replay.Response.Snapshot.Timers);
        Assert.Equal(1, repository.State.SpeedUps!["universal_60s"]);
        Assert.Equal(2, repository.State.BuildingLevels["honey_storage"]);
        Assert.Equal(clock.UtcNow, repository.State.Operations[0].CompletesAtUtc);
        var conflict = await new SpeedUpInventoryService(repository, clock, Options())
            .ApplyAsync(repository.State.PlayerId, repository.State.HiveId, request with { ExpectedRevision = 2 });
        Assert.False(conflict.Succeeded);
        Assert.Equal("idempotency_conflict", conflict.Code);
        Assert.Null(conflict.Response);
        Assert.Equal(1, repository.State.SpeedUps["universal_60s"]);
    }

    [Fact]
    public async Task RejectedConstructionReplayRemainsRejectedWithoutConsumption()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState(clock.UtcNow,
            new Dictionary<string, int> { ["universal_60s"] = 1 },
            new HiveOperation(Guid.NewGuid(), "honey_storage", 1, 2, clock.UtcNow,
                clock.UtcNow.AddMinutes(2), HiveOperationStatus.Running, "", 0, null)));
        var request = new ApplySpeedUpRequest("universal_60s", SpeedUpCategories.Construction,
            "honey_storage", 60, 99, "rejected");
        var service = new SpeedUpInventoryService(repository, clock, Options());
        var first = await service.ApplyAsync(repository.State.PlayerId, repository.State.HiveId, request);
        var replay = await service.ApplyAsync(repository.State.PlayerId, repository.State.HiveId, request);
        Assert.False(first.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.Equal("revision_conflict", replay.Code);
        Assert.Null(replay.Response);
        Assert.Equal(0, repository.State.Revision);
        Assert.Equal(1, repository.State.SpeedUps!["universal_60s"]);
    }

    [Fact]
    public async Task ConcurrentSecondApplyFailsOnRevisionWithoutConsumption()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState(clock.UtcNow, new Dictionary<string, int> { ["universal_60s"] = 2 }, new HiveOperation(Guid.NewGuid(), "honey_storage", 1, 2, clock.UtcNow, clock.UtcNow.AddMinutes(5), HiveOperationStatus.Running, "", 0, null)));
        var service = new SpeedUpInventoryService(repository, clock, Options());
        Task<SpeedUpCommandResult> first = service.ApplyAsync(repository.State.PlayerId, repository.State.HiveId, new ApplySpeedUpRequest("universal_60s", SpeedUpCategories.Universal, "honey_storage", 60, 0, "a"));
        Task<SpeedUpCommandResult> second = service.ApplyAsync(repository.State.PlayerId, repository.State.HiveId, new ApplySpeedUpRequest("universal_60s", SpeedUpCategories.Universal, "honey_storage", 60, 0, "b"));
        SpeedUpCommandResult[] results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.Succeeded);
        Assert.Single(results, result => !result.Succeeded && result.Code == "revision_conflict");
        Assert.Equal(1, repository.State.SpeedUps!["universal_60s"]);
    }

    private static SpeedUpOptions Options() => new()
    {
        Enabled = true,
        Catalog =
        [
            new SpeedUpDefinition("universal_60s", SpeedUpCategories.Universal, 60),
            new SpeedUpDefinition("research_60s", SpeedUpCategories.Research, 60)
        ]
    };

    private static PlayerHiveState CreateState(DateTimeOffset now, Dictionary<string, int> speedUps, HiveOperation operation) => new(
        Guid.NewGuid(), Guid.NewGuid(), 1, 0,
        new Dictionary<string, ResourceBalance> { ["honey"] = new(100, 1000) },
        new Dictionary<string, int> { ["honey_storage"] = 1 },
        new List<HiveOperation> { operation },
        new Dictionary<string, IdempotencyReceipt>(),
        SpeedUps: speedUps);

    private sealed class TestClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    private sealed class MemoryRepository(PlayerHiveState initial) : IHiveStateRepository
    {
        private readonly object gate = new();
        public PlayerHiveState State { get; private set; } = initial;

        public Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation, CancellationToken cancellationToken = default)
        {
            lock (gate) State = mutation(State);
            return Task.FromResult(State);
        }

        public Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default) => Task.FromResult<PlayerHiveState?>(State);
        public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(new[] { State.HiveId });
        public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlayerHiveState>>(new[] { State });
        public Task<bool> DeleteAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
