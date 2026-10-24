using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// Local test/development storage. Single site JSON file, atomic rename, shared
/// in-process gate across repository instances. NOT suitable for an MMO
/// cross-hive economy transaction; this repository exposes read/create only.
/// </summary>
public sealed class DurableJsonFloralSiteRepository(string rootDirectory)
    : IFloralSiteRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<FloralSiteState> ReadOrCreateAsync(
        FloralSiteDefinition definition, string catalogVersion,
        DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        string path = Path.GetFullPath(Path.Combine(rootDirectory,
            definition.WorldId + "_" + definition.SiteId + ".json"));
        var gate = Gates.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (File.Exists(path))
            {
                await using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read);
                FloralSiteState? loaded = await JsonSerializer
                    .DeserializeAsync<FloralSiteState>(stream, JsonOptions, ct);
                return loaded ?? throw new InvalidDataException(
                    "Empty persisted floral site document.");
            }
            FloralSiteState initial = FloralSiteService.InitialState(
                definition, catalogVersion, nowUtc);
            Directory.CreateDirectory(rootDirectory);
            string temporary = path + ".tmp." + Guid.NewGuid().ToString("N");
            try
            {
                await File.WriteAllTextAsync(temporary,
                    JsonSerializer.Serialize(initial, JsonOptions), ct);
                File.Move(temporary, path, false);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            return initial;
        }
        finally
        {
            gate.Release();
        }
    }
}

/// <summary>
/// Production-facing shared SQL aggregate. Uses ONE row per (worldId,siteId)
/// rather than a copy of the state in each player's hive. SQL schema
/// 096_floral_site_states must have been applied before enabling.
/// </summary>
public sealed class SqlFloralSiteRepository(
    string connectionString, int commandTimeoutSeconds = 15)
    : IFloralSiteRepository
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<FloralSiteState> ReadOrCreateAsync(
        FloralSiteDefinition definition, string catalogVersion,
        DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        await using SqlConnection conn = new(connectionString);
        await conn.OpenAsync(ct);
        await using SqlTransaction transaction = (SqlTransaction)
            await conn.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        // Same canonical global-resource key will be used by the future
        // cross-aggregate expedition coordinator, preventing duplicate seeds.
        await using (SqlCommand guard = conn.CreateCommand())
        {
            guard.Transaction = transaction;
            guard.CommandTimeout = commandTimeoutSeconds;
            guard.CommandText =
                "DECLARE @result int; "
                + "EXEC @result=sys.sp_getapplock "
                + "@Resource=@resource,@LockMode='Exclusive',"
                + "@LockOwner='Transaction',@LockTimeout=@timeout; "
                + "IF @result < 0 THROW 51071,'Floral site lock unavailable.',1;";
            guard.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255)
            { Value = $"floral:{definition.WorldId}:{definition.SiteId}" });
            guard.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int)
            { Value = commandTimeoutSeconds * 1000 });
            await guard.ExecuteNonQueryAsync(ct);
        }

        FloralSiteState? existing;
        await using (SqlCommand select = conn.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandTimeout = commandTimeoutSeconds;
            select.CommandText =
                "SELECT StateJson FROM dbo.FloralSiteStates WITH (UPDLOCK,HOLDLOCK) "
                + "WHERE WorldId=@world AND SiteId=@site";
            select.Parameters.Add(new SqlParameter("@world", SqlDbType.NVarChar, 64)
            { Value = definition.WorldId });
            select.Parameters.Add(new SqlParameter("@site", SqlDbType.NVarChar, 64)
            { Value = definition.SiteId });
            object? json = await select.ExecuteScalarAsync(ct);
            existing = json is string value
                ? JsonSerializer.Deserialize<FloralSiteState>(value, JsonOptions)
                : null;
        }

        if (existing is null)
        {
            existing = FloralSiteService.InitialState(
                definition, catalogVersion, nowUtc);
            await using var insert = conn.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandTimeout = commandTimeoutSeconds;
            insert.CommandText =
                "INSERT dbo.FloralSiteStates "
                + "(WorldId,SiteId,Revision,StateJson) "
                + "VALUES(@world,@site,@revision,@json)";
            insert.Parameters.Add(new SqlParameter("@world", SqlDbType.NVarChar, 64)
            { Value = definition.WorldId });
            insert.Parameters.Add(new SqlParameter("@site", SqlDbType.NVarChar, 64)
            { Value = definition.SiteId });
            insert.Parameters.Add(new SqlParameter("@revision", SqlDbType.BigInt)
            { Value = existing.Revision });
            insert.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1)
            { Value = JsonSerializer.Serialize(existing, JsonOptions) });
            await insert.ExecuteNonQueryAsync(ct);
        }

        // SQL READ of persisted global state — NEVER reconstruct from per-hive
        // state and NEVER silently reset stocks after a catalogue adjustment.
        await transaction.CommitAsync(ct);
        return existing;
    }
}
