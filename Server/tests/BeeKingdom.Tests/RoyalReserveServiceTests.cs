using BeeKingdom.HiveOperations;

namespace BeeKingdom.Tests;

public sealed class RoyalReserveServiceTests
{
    [Test]
    public async Task DepositMovesLiquidResourceIntoReserveAtomically()
    {
        var (service, _, player, hive, repo) = Create(bankLevel: 2);
        RoyalReserveReadSnapshot before = (await service.ReadAsync(player, hive))!;

        RoyalReserveCommandResult result = await service.DepositAsync(
            player,
            hive,
            new RoyalReserveTransferRequest("honey", 400, before.Revision, "deposit-1"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Code, Is.EqualTo("game.bank_reserve_deposited"));
        Assert.That(result.Snapshot.Reserved["honey"], Is.EqualTo(400));
        Assert.That(result.Snapshot.LiquidBalances["honey"].Amount, Is.EqualTo(600));
        Assert.That(result.Snapshot.ReservedTotal, Is.EqualTo(400));
        Assert.That(result.Snapshot.Capacity, Is.EqualTo(20_000));

        PlayerHiveState persisted = (await repo.ReadAsync(player, hive))!;
        Assert.That(persisted.Resources["honey"].Amount, Is.EqualTo(600));
        Assert.That(persisted.RoyalReserve!.Amounts["honey"], Is.EqualTo(400));
    }

    [Test]
    public async Task WithdrawMovesReservedResourceBackToLiquidStock()
    {
        var (service, _, player, hive, _) = Create(bankLevel: 2);
        RoyalReserveReadSnapshot before = (await service.ReadAsync(player, hive))!;
        RoyalReserveCommandResult deposit = await service.DepositAsync(
            player,
            hive,
            new RoyalReserveTransferRequest("honey", 400, before.Revision, "deposit-1"));

        RoyalReserveCommandResult withdraw = await service.WithdrawAsync(
            player,
            hive,
            new RoyalReserveTransferRequest("honey", 150, deposit.Snapshot.Revision, "withdraw-1"));

        Assert.That(withdraw.Succeeded, Is.True);
        Assert.That(withdraw.Code, Is.EqualTo("game.bank_reserve_withdrawn"));
        Assert.That(withdraw.Snapshot.Reserved["honey"], Is.EqualTo(250));
        Assert.That(withdraw.Snapshot.LiquidBalances["honey"].Amount, Is.EqualTo(750));
    }

    [Test]
    public async Task SameIdempotencyKeyAndPayloadCannotDoubleDeposit()
    {
        var (service, _, player, hive, repo) = Create(bankLevel: 2);
        RoyalReserveReadSnapshot before = (await service.ReadAsync(player, hive))!;
        var request = new RoyalReserveTransferRequest("honey", 100, before.Revision, "same-key");

        RoyalReserveCommandResult first = await service.DepositAsync(player, hive, request);
        RoyalReserveCommandResult replay = await service.DepositAsync(player, hive, request);

        Assert.That(first.Succeeded, Is.True);
        Assert.That(replay.Succeeded, Is.True);
        PlayerHiveState persisted = (await repo.ReadAsync(player, hive))!;
        Assert.That(persisted.Resources["honey"].Amount, Is.EqualTo(900));
        Assert.That(persisted.RoyalReserve!.Amounts["honey"], Is.EqualTo(100));
    }

    [Test]
    public async Task SameIdempotencyKeyWithDifferentPayloadIsRejected()
    {
        var (service, _, player, hive, _) = Create(bankLevel: 2);
        RoyalReserveReadSnapshot before = (await service.ReadAsync(player, hive))!;

        _ = await service.DepositAsync(
            player, hive, new RoyalReserveTransferRequest("honey", 100, before.Revision, "key"));

        RoyalReserveCommandResult conflict = await service.DepositAsync(
            player, hive, new RoyalReserveTransferRequest("honey", 200, before.Revision, "key"));

        Assert.That(conflict.Succeeded, Is.False);
        Assert.That(conflict.Code, Is.EqualTo("game.idempotency_conflict"));
    }

    [Test]
    public async Task DepositCannotExceedReserveCapacity()
    {
        var (service, _, player, hive, repo) = Create(bankLevel: 1);
        PlayerHiveState state = (await repo.ReadAsync(player, hive))!;
        repo.Replace(state with
        {
            Resources = new Dictionary<string, ResourceBalance>(state.Resources, StringComparer.Ordinal)
            {
                ["honey"] = new ResourceBalance(10_000, 1_000_000_000)
            }
        });

        RoyalReserveReadSnapshot before = (await service.ReadAsync(player, hive))!;
        RoyalReserveCommandResult result = await service.DepositAsync(
            player,
            hive,
            new RoyalReserveTransferRequest("honey", 5_001, before.Revision, "over-cap"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("game.bank_reserve_capacity_insufficient"));
        Assert.That(result.Snapshot.Capacity, Is.EqualTo(5_000));
    }

    [Test]
    public async Task WithdrawCannotExceedReservedAmount()
    {
        var (service, _, player, hive, _) = Create(bankLevel: 2);
        RoyalReserveReadSnapshot before = (await service.ReadAsync(player, hive))!;

        RoyalReserveCommandResult result = await service.WithdrawAsync(
            player,
            hive,
            new RoyalReserveTransferRequest("wax", 1, before.Revision, "empty-withdraw"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("game.bank_reserve_insufficient"));
    }

    [Test]
    public void CapacityGrowsQuadraticallyWithBankLevel()
    {
        Assert.That(RoyalReserveService.CapacityForBankLevel(1), Is.EqualTo(5_000));
        Assert.That(RoyalReserveService.CapacityForBankLevel(2), Is.EqualTo(20_000));
        Assert.That(RoyalReserveService.CapacityForBankLevel(3), Is.EqualTo(45_000));
        Assert.That(RoyalReserveService.CapacityForBankLevel(10), Is.EqualTo(500_000));
        Assert.That(RoyalReserveService.CapacityForBankLevel(30), Is.EqualTo(4_500_000));
    }

    [Test]
    public void MigratorAddsEmptyRoyalReserveToV12State()
    {
        Guid player = Guid.NewGuid();
        Guid hive = Guid.NewGuid();
        PlayerHiveState oldState = Seed(player, hive, bankLevel: 2) with
        {
            ModelVersion = 12,
            RoyalReserve = null
        };

        PlayerHiveState migrated = HiveStateMigrator.ToCurrent(oldState);

        Assert.That(migrated.ModelVersion, Is.EqualTo(13));
        Assert.That(migrated.RoyalReserve, Is.Not.Null);
        Assert.That(migrated.RoyalReserve!.Amounts["honey"], Is.Zero);
        Assert.That(migrated.RoyalReserve.Amounts["pollen"], Is.Zero);
        Assert.That(migrated.RoyalReserve.Amounts["wax"], Is.Zero);
        Assert.That(migrated.RoyalReserve.Receipts, Is.Empty);
    }

    private static (RoyalReserveService service, FakeClock clock, Guid player, Guid hive, MemoryRepo repo)
        Create(int bankLevel)
    {
        Guid player = Guid.NewGuid();
        Guid hive = Guid.NewGuid();
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        repo.Seed(Seed(player, hive, bankLevel));

        var storage = new HiveOfflineProductionOptions
        {
            Enabled = true,
            CatalogVersion = "bank-tests-v1",
            Catalog =
            [
                new("honey_storage", "honey", 10m, 10_000),
                new("wax_workshop", "wax", 5m, 10_000),
                new("warehouse_cells", "pollen", 8m, 10_000)
            ]
        };

        return (new RoyalReserveService(repo, clock, storage), clock, player, hive, repo);
    }

    private static PlayerHiveState Seed(Guid player, Guid hive, int bankLevel) =>
        new(
            player,
            hive,
            HiveStateMigrator.CurrentModelVersion,
            0,
            new Dictionary<string, ResourceBalance>(StringComparer.Ordinal)
            {
                ["honey"] = new(1_000, 1_000_000_000),
                ["wax"] = new(500, 1_000_000_000),
                ["pollen"] = new(500, 1_000_000_000)
            },
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["hive_bank"] = bankLevel,
                ["honey_storage"] = 1,
                ["wax_workshop"] = 1,
                ["warehouse_cells"] = 1,
                ["guard_post"] = 1
            },
            [],
            new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal));

    private sealed class FakeClock(DateTimeOffset now) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private sealed class MemoryRepo : IHiveStateRepository
    {
        private readonly Dictionary<(Guid, Guid), PlayerHiveState> data = [];

        public void Seed(PlayerHiveState state) => data[(state.PlayerId, state.HiveId)] = state;
        public void Replace(PlayerHiveState state) => Seed(state);

        public Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default) =>
            Task.FromResult(data.TryGetValue((playerId, hiveId), out PlayerHiveState? state) ? state : null);

        public Task<PlayerHiveState> ExecuteAtomicallyAsync(
            Guid playerId,
            Guid hiveId,
            Func<PlayerHiveState, PlayerHiveState> mutation,
            CancellationToken cancellationToken = default)
        {
            PlayerHiveState state = HiveStateMigrator.ToCurrent(data[(playerId, hiveId)]);
            PlayerHiveState updated = mutation(state);
            data[(playerId, hiveId)] = updated;
            return Task.FromResult(updated);
        }

        public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                data.Keys.Where(x => x.Item1 == playerId).Select(x => x.Item2).ToList());

        public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlayerHiveState>>(data.Values.Take(limit).ToList());

        public Task<bool> DeleteAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default) =>
            Task.FromResult(data.Remove((playerId, hiveId)));
    }
}
