using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;
public sealed class StrategicPathTests
{
    [Fact]
    public async Task ChoiceIsEligibleLockedAndIdempotent()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid(); string root = Path.Combine(Path.GetTempPath(), "path-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, 6, 0, new(), new Dictionary<string, int> { ["hive"] = 10 }, [], new()));
        var service = new StrategicPathService(repo, new Clock());
        StrategicPathCommandResult first = await service.ChooseAsync(new(p, h, "scout", 0, "k")); StrategicPathCommandResult replay = await service.ChooseAsync(new(p, h, "scout", 0, "k"));
        Assert.True(first.Succeeded); Assert.Equal("scout", first.Snapshot.SelectedPath); Assert.Equal(first.Snapshot.Revision, replay.Snapshot.Revision);
        StrategicPathCommandResult locked = await service.ChooseAsync(new(p, h, "alchemist", 1, "k2")); Assert.False(locked.Succeeded); Assert.Equal("game.strategic_path_locked", locked.Code);
    }

    // Reproduit un bug reel trouve en test live contre le serveur de developpement : un joueur
    // qui a deja fait n'importe quoi d'autre dans sa ruche (ici simule par une ecriture qui fait
    // avancer state.Revision sans toucher au chemin strategique) doit quand meme pouvoir choisir
    // sa voie des qu'il atteint le niveau de batiment requis - seule strategic.Revision (que le
    // client suit reellement, via le champ Revision du snapshot GET) doit faire foi.
    [Fact]
    public async Task ChoiceSucceedsWhenHiveRevisionHasAdvancedFromUnrelatedActivity()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid(); string root = Path.Combine(Path.GetTempPath(), "path-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, 6, 0, new(), new Dictionary<string, int> { ["hive"] = 10 }, [], new()));
        await repo.ExecuteAtomicallyAsync(p, h, state => state with { Revision = state.Revision + 5 });
        var service = new StrategicPathService(repo, new Clock());
        StrategicPathCommandResult result = await service.ChooseAsync(new(p, h, "scout", 0, "k"));
        Assert.True(result.Succeeded); Assert.Equal("scout", result.Snapshot.SelectedPath);
    }
    private sealed class Clock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-22T12:00:00Z"); }
}
