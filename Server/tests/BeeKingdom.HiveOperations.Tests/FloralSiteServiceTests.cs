using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class FloralSiteServiceTests
{
    private static readonly DateTimeOffset Moment =
        new(2026, 10, 21, 20, 0, 0, TimeSpan.Zero);

    private sealed class FakeClock(DateTimeOffset start) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = start;
    }

    private static FloralSiteOptions Options(bool enabled = true) => new()
    {
        Enabled = enabled,
        CatalogVersion = "alpha.qa.v1",
        Sites =
        [
            new("world-qa", "region-qa", "clover-qa-01", "clover",
                "Trèfle blanc", "White clover", 100, 6700, 5000),
            new("world-qa", "region-qa", "clover-qa-02", "clover",
                "Pré de trèfles", "Clover meadow", 250, 7500, 5200)
        ]
    };
    private static string Temp() => Path.Combine(Path.GetTempPath(),
        "bk-flora-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Disabled_world_exposes_no_catalogue_or_data()
    {
        string path = Temp();
        try
        {
            var service = new FloralSiteService(new DurableJsonFloralSiteRepository(path),
                new FakeClock(Moment), new FloralSiteOptions());
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ListAsync());
            Assert.False(Directory.Exists(path));
            Assert.False(new FloralSiteOptions().Enabled);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    [Fact]
    public async Task Separate_players_and_service_instances_read_same_shared_state()
    {
        string root = Temp();
        try
        {
            var first = new FloralSiteService(
                new DurableJsonFloralSiteRepository(root), new FakeClock(Moment), Options());
            FloralSiteSnapshot initial = await first.ReadAsync("clover-qa-01");
            var second = new FloralSiteService(
                new DurableJsonFloralSiteRepository(root),
                new FakeClock(Moment.AddDays(2)), Options());

            FloralSiteSnapshot repeated = await second.ReadAsync("clover-qa-01");
            Assert.Equal(initial.PollenRemaining, repeated.PollenRemaining);
            Assert.Equal(initial.Revision, repeated.Revision);
            Assert.Equal(initial.FertilityBp, repeated.FertilityBp);
            Assert.Equal(initial.CatalogVersion, repeated.CatalogVersion);
            Assert.Equal("Trèfle blanc", repeated.NameFr);
            Assert.Equal("White clover", repeated.NameEn);
            Assert.Equal(Moment.AddDays(2), repeated.ServerTimeUtc);
            Assert.Single(Directory.GetFiles(root, "*.json"));
            FloralSiteSnapshot other = await second.ReadAsync("clover-qa-02");
            Assert.Equal(250, other.PollenRemaining);
            Assert.Equal(2, Directory.GetFiles(root, "*.json").Length);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Competing_instance_creation_is_atomic_per_site()
    {
        string root = Temp();
        try
        {
            Task<FloralSiteSnapshot>[] requests = Enumerable.Range(0, 24)
                .Select(i => new FloralSiteService(
                    new DurableJsonFloralSiteRepository(root),
                    new FakeClock(Moment.AddMinutes(i)), Options())
                    .ReadAsync("clover-qa-01")).ToArray();
            FloralSiteSnapshot[] results = await Task.WhenAll(requests);
            Assert.All(results, site =>
            {
                Assert.Equal(100, site.PollenRemaining);
                Assert.Equal(0, site.Revision);
                Assert.Equal(6700, site.FertilityBp);
            });
            Assert.Single(Directory.GetFiles(root, "*.json"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Changing_catalogue_does_not_reset_shared_persisted_stock_or_fertility()
    {
        string root = Temp();
        try
        {
            var repository = new DurableJsonFloralSiteRepository(root);
            var pilot = new FloralSiteService(repository, new FakeClock(Moment), Options());
            await pilot.ReadAsync("clover-qa-01");
            FloralSiteOptions changed = Options();
            changed.CatalogVersion = "alpha.qa.v2";
            changed.Sites[0] = changed.Sites[0] with
            { MaximumPollenUnits = 900, InitialFertilityBp = 9999 };
            var reread = new FloralSiteService(repository,
                new FakeClock(Moment.AddDays(1)), changed);
            FloralSiteSnapshot stored = await reread.ReadAsync("clover-qa-01");
            Assert.Equal(100, stored.PollenCapacity);
            Assert.Equal(6700, stored.FertilityBp);
            Assert.Equal("alpha.qa.v1", stored.CatalogVersion);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void Official_uuid_world_id_roundtrips_without_relaxing_site_identifiers()
    {
        const string officialWorld = "00000000-0000-0000-0000-000000000101";
        FloralSiteOptions options = Options();
        options.Sites[0] = options.Sites[0] with { WorldId = officialWorld };
        options.Validate();
        Assert.True(FloralSiteOptions.ValidWorldId(officialWorld));
        Assert.True(FloralSiteOptions.ValidWorldId("world-qa"));
        Assert.False(FloralSiteOptions.ValidWorldId(Guid.Empty.ToString("D")));
        Assert.True(FloralSiteOptions.ValidWorldId("abcd0000-0000-4000-8000-000000000101"));
        Assert.False(FloralSiteOptions.ValidWorldId("ABCD0000-0000-4000-8000-000000000101"));
        FloralSiteState persisted = FloralSiteService.InitialState(
            options.Sites[0], options.CatalogVersion, Moment);
        FloralSiteService.ValidateStored(persisted);
        Assert.Equal(officialWorld, persisted.WorldId);

        options.Sites[0] = options.Sites[0] with { SiteId = officialWorld };
        Assert.Throws<InvalidDataException>(() => options.Validate());
    }
    [Fact]
    public void Invalid_catalogue_and_corrupt_persisted_state_are_rejected()
    {
        FloralSiteOptions duplicate = Options();
        duplicate.Sites[1] = duplicate.Sites[1] with
        { SiteId = "clover-qa-01", WorldId = "other-world" };
        Assert.Throws<InvalidDataException>(() => duplicate.Validate());
        FloralSiteState broken = FloralSiteService.InitialState(
            Options().Sites[0], "alpha.qa.v1", Moment) with
        { PollenRemaining = 101 };
        Assert.Throws<InvalidDataException>(
            () => FloralSiteService.ValidateStored(broken));
        Assert.Throws<InvalidDataException>(
            () => FloralSiteService.ValidateStored(broken with
            { PollenRemaining = -1 }));
    }

    [Fact]
    public async Task Invalid_or_unknown_site_is_not_created()
    {
        string root = Temp();
        try
        {
            var service = new FloralSiteService(
                new DurableJsonFloralSiteRepository(root), new FakeClock(Moment), Options());
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ReadAsync("../escape"));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ReadAsync("inaccessible-qa"));
            Assert.False(Directory.Exists(root));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact]
    public async Task Official_anchor_is_read_from_server_catalogue_not_hive_or_local_coordinates()
    {
        string root = Temp();
        try
        {
            FloralSiteOptions options = Options();
            options.Sites[0] = options.Sites[0] with
            {
                MapSurfaceId = "BK-DEMO-WORLD-WAVE6-LOCAL",
                MapAnchorId = "res_pollen_core"
            };
            var service = new FloralSiteService(
                new DurableJsonFloralSiteRepository(root), new FakeClock(Moment), options);
            FloralSiteSnapshot site = await service.ReadAsync("clover-qa-01");
            Assert.Equal("BK-DEMO-WORLD-WAVE6-LOCAL", site.MapSurfaceId);
            Assert.Equal("res_pollen_core", site.MapAnchorId);
            Assert.Equal(100, site.PollenRemaining);
            Assert.Equal(0, site.Revision);
            Assert.Equal("", (await service.ReadAsync("clover-qa-02")).MapAnchorId);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void Catalogue_rejects_unpaired_unsafe_or_overlapping_world_anchors()
    {
        FloralSiteOptions half = Options();
        half.Sites[0] = half.Sites[0] with
        { MapSurfaceId = "BK-DEMO-WORLD-WAVE6-LOCAL", MapAnchorId = "" };
        Assert.Throws<InvalidDataException>(() => half.Validate());

        half = Options();
        half.Sites[0] = half.Sites[0] with
        { MapSurfaceId = "BK-DEMO-WORLD-WAVE6-LOCAL", MapAnchorId = "../wrong" };
        Assert.Throws<InvalidDataException>(() => half.Validate());

        half = Options();
        half.Sites[0] = half.Sites[0] with
        { MapSurfaceId = "BK-DEMO-WORLD-WAVE6-LOCAL", MapAnchorId = "res_pollen_core" };
        half.Sites[1] = half.Sites[1] with
        { MapSurfaceId = "BK-DEMO-WORLD-WAVE6-LOCAL", MapAnchorId = "res_pollen_core" };
        Assert.Throws<InvalidDataException>(() => half.Validate());
    }

}
