using System.Collections.Concurrent;
using System.Text.Json;

namespace BeeKingdom.HiveOperations;

public sealed class DurableJsonHiveStateRepository(string rootDirectory, Func<Guid, Guid, PlayerHiveState> newStateFactory) : IHiveStateRepository
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation, CancellationToken cancellationToken = default)
    {
        string path = PathFor(playerId, hiveId);
        SemaphoreSlim gate = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            PlayerHiveState state = HiveStateMigrator.ToCurrent(await ReadCoreAsync(path, cancellationToken) ?? newStateFactory(playerId, hiveId));
            PlayerHiveState updated = mutation(state);
            Directory.CreateDirectory(rootDirectory);
            string temp = path + ".tmp." + Guid.NewGuid().ToString("N");
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(updated, JsonOptions), cancellationToken);
            File.Move(temp, path, true);
            return updated;
        }
        finally { gate.Release(); }
    }

    public async Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default)
    {
        PlayerHiveState? state = await ReadCoreAsync(PathFor(playerId, hiveId), cancellationToken);
        return state is null ? null : HiveStateMigrator.ToCurrent(state);
    }

    public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootDirectory)) return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        string prefix = $"{playerId:N}_";
        var hiveIds = new List<Guid>();
        foreach (string file in Directory.EnumerateFiles(rootDirectory, prefix + "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            string hiveIdPart = name.Substring(prefix.Length);
            if (Guid.TryParseExact(hiveIdPart, "N", out Guid hiveId)) hiveIds.Add(hiveId);
        }
        return Task.FromResult<IReadOnlyList<Guid>>(hiveIds);
    }

    public async Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || !Directory.Exists(rootDirectory)) return Array.Empty<PlayerHiveState>();
        IEnumerable<string> files = Directory.EnumerateFiles(rootDirectory, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(limit);
        var states = new List<PlayerHiveState>();
        foreach (string file in files)
        {
            PlayerHiveState? state = await ReadCoreAsync(file, cancellationToken);
            if (state != null) states.Add(HiveStateMigrator.ToCurrent(state));
        }
        return states;
    }

    private static async Task<PlayerHiveState?> ReadCoreAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return null;
        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<PlayerHiveState>(stream, JsonOptions, ct);
    }

    private string PathFor(Guid playerId, Guid hiveId) => Path.Combine(rootDirectory, $"{playerId:N}_{hiveId:N}.json");
}
