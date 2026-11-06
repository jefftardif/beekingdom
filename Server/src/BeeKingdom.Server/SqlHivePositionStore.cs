using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using BeeKingdom.Alliance.Territories;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.TerritorySqlCandidate;

public sealed record HiveBaselineEntry(Guid ColonyId, Guid PlayerId, long ExpectedColonyRevision,
    string LegacyNodeId, float X, float Y);
public sealed record HivePositionBaseline(string BaselineId, string WorldId, string SurfaceId,
    string CoordinateUnit, string GenerationVersion, string SourceHash, HiveBaselineEntry[] Entries);
public sealed record PersistedHivePosition(Guid ColonyId, Guid PlayerId, string WorldId,
    string SurfaceId, string CoordinateUnit, string LegacyNodeId, float X, float Y, long Revision);
public sealed record HivePositionAdoption(bool Created, bool Replayed, int Count);
public sealed record HiveReconciliationIssue(Guid ColonyId, string Code);
public sealed record HivePositionReconciliation(string WorldId, string SurfaceId, bool BaselineAdopted,
    int ActiveColonies, int MatchedPositions, HiveReconciliationIssue[] Issues);

// Candidate persistent position source. Feature defaults OFF; no production DI registration.
// Adopt is a trusted administrative import, NOT a player-supplied coordinate endpoint.
public sealed partial class SqlHivePositionStore(string connectionString, string world, string surface, bool enabled = false)
{
    private void RequireEnabled()
    {
        if (!enabled) throw new InvalidOperationException("hive_positions_disabled");
    }

    private static bool Key(string value, int limit) => !string.IsNullOrWhiteSpace(value) && value.Length <= limit;

    private (Guid ColonyWorld, string Unit, string Generator) ReadBinding(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = SqlTerritoryStore.Command(connection, transaction,
            "SELECT ColonyWorldId, CoordinateUnit, GenerationVersion, Enabled FROM dbo.HiveMapBindings WITH(HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",
            ("@w", world), ("@s", surface));
        using var reader = command.ExecuteReader();
        if (!reader.Read() || !reader.GetBoolean(3)) throw new InvalidOperationException("hive_world_binding_unavailable");
        return (reader.GetGuid(0), reader.GetString(1), reader.GetString(2));
    }

    public HivePositionAdoption AdoptBaseline(HivePositionBaseline baseline, Action? beforeCommit = null)
    {
        RequireEnabled();
        if (baseline is null || baseline.WorldId != world || baseline.SurfaceId != surface
            || !Key(baseline.BaselineId, 128) || !Key(baseline.CoordinateUnit, 80) || !Key(baseline.GenerationVersion, 128)
            || baseline.SourceHash is null || baseline.SourceHash.Length != 64 || !baseline.SourceHash.All(Uri.IsHexDigit)
            || baseline.Entries is null || baseline.Entries.Length is < 1 or > 10000)
            throw new InvalidDataException("hive_baseline_invalid");
        baseline = baseline with { Entries = baseline.Entries.ToArray() };
        if (baseline.Entries.Any(entry => entry is null || entry.ColonyId == Guid.Empty || entry.PlayerId == Guid.Empty
            || entry.ExpectedColonyRevision < 0 || !Key(entry.LegacyNodeId, 128) || !float.IsFinite(entry.X) || !float.IsFinite(entry.Y)))
            throw new InvalidDataException("hive_entry_invalid");
        if (baseline.Entries.Select(entry => entry.ColonyId).Distinct().Count() != baseline.Entries.Length
            || baseline.Entries.Select(entry => entry.LegacyNodeId).Distinct(StringComparer.Ordinal).Count() != baseline.Entries.Length
            || baseline.Entries.Select(entry => (entry.X, entry.Y)).Distinct().Count() != baseline.Entries.Length)
            throw new InvalidDataException("hive_baseline_ambiguous");
        string payload = JsonSerializer.Serialize(baseline with { Entries = baseline.Entries.OrderBy(entry => entry.ColonyId).ToArray() });
        string hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload)));
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        // Resolve immutable bridge IDs without retaining SQL locks, then acquire canonical
        // hive locks BEFORE the territory lock. Revalidate in the transaction below.
        var canonicalHints = new List<(Guid Colony, Guid Player, Guid Hive)>();
        foreach (var entry in baseline.Entries)
        {
            using var hint = SqlTerritoryStore.Command(connection, null,
                "SELECT PlayerId,HiveId FROM dbo.HiveCanonicalIdentityBindings WHERE WorldId=@w AND SurfaceId=@s AND ColonyId=@c",
                ("@w", world), ("@s", surface), ("@c", entry.ColonyId));
            using var r = hint.ExecuteReader();
            if (!r.Read()) throw new InvalidDataException("hive_canonical_identity_required");
            canonicalHints.Add((entry.ColonyId, r.GetGuid(0), r.GetGuid(1)));
        }
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        foreach (var hint in canonicalHints.OrderBy(x => x.Player.ToString("N"), StringComparer.Ordinal).ThenBy(x => x.Hive.ToString("N"), StringComparer.Ordinal))
        {
            LockCanonicalHive(connection, transaction, hint.Player, hint.Hive);
            RequireCanonicalAssignment(connection, transaction, hint.Player);
            RequireCanonicalNotRevoked(connection, transaction, hint.Player, hint.Hive);
        }
        // Same SQL transaction lock as territory foundation/import; covers separate processes.
        string lockKey = "territory:" + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { world, surface })));
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;", ("@key", lockKey)))
            if (Convert.ToInt32(command.ExecuteScalar()) < 0) throw new InvalidOperationException("hive_position_lock_unavailable");
        var binding = ReadBinding(connection, transaction);
        if (binding.Unit != baseline.CoordinateUnit || binding.Generator != baseline.GenerationVersion)
            throw new InvalidDataException("hive_baseline_coordinate_contract_mismatch");
// A completed administrative import replays its receipt even after colony revisions change.
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "SELECT BaselineId,PayloadHash FROM dbo.HivePositionBaselines WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s", ("@w", world), ("@s", surface)))
        using (var reader = command.ExecuteReader())
            if (reader.Read())
            {
                if (reader.GetString(0) != baseline.BaselineId || reader.GetString(1) != hash)
                    throw new InvalidDataException("hive_existing_positions_preserved");
                return new(false, true, baseline.Entries.Length);
            }
        var colonies = new Dictionary<Guid, (Guid Player, long Revision)>();
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "SELECT ColonyId,PlayerId,Revision FROM dbo.Colonies WITH(HOLDLOCK) WHERE WorldId=@cw AND Status=1", ("@cw", binding.ColonyWorld)))
        using (var reader = command.ExecuteReader())
            while (reader.Read()) colonies.Add(reader.GetGuid(0), (reader.GetGuid(1), reader.GetInt64(2)));
        SqlTerritoryScope scope;
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "SELECT Revision, ConfigJson FROM dbo.TerritoryWorldScopes WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s", ("@w", world), ("@s", surface)))
        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read()) throw new InvalidDataException("hive_scope_missing");
            scope = JsonSerializer.Deserialize<SqlTerritoryScope>(reader.GetString(1)) ?? throw new InvalidDataException("hive_scope_invalid");
            if (scope.Revision != reader.GetInt64(0) || scope.WorldId != world || scope.SurfaceId != surface)
                throw new InvalidDataException("hive_scope_invalid");
        }
        if (colonies.Count != baseline.Entries.Length || baseline.Entries.Any(entry => !colonies.ContainsKey(entry.ColonyId)))
            throw new InvalidDataException("hive_baseline_inventory_incomplete");
        foreach (var entry in baseline.Entries)
        {
            var colony = colonies[entry.ColonyId];
            if (colony.Player != entry.PlayerId || colony.Revision != entry.ExpectedColonyRevision)
                throw new InvalidDataException("hive_colony_identity_or_revision_mismatch");
            if ((double)entry.X < (double)scope.Bounds.MinX || (double)entry.X > (double)scope.Bounds.MaxX
                || (double)entry.Y < (double)scope.Bounds.MinY || (double)entry.Y > (double)scope.Bounds.MaxY)
                throw new InvalidDataException("hive_position_outside_world");
            using var identity = SqlTerritoryStore.Command(connection, transaction,
                "SELECT COUNT(*) FROM dbo.HiveCanonicalIdentityBindings WITH(HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s AND ColonyId=@c AND PlayerId=@p AND HiveId=@h",
                ("@w", world), ("@s", surface), ("@c", entry.ColonyId), ("@p", entry.PlayerId),
                ("@h", canonicalHints.Single(x => x.Colony == entry.ColonyId).Hive));
            if (Convert.ToInt32(identity.ExecuteScalar()) != 1) throw new InvalidDataException("hive_canonical_identity_required");
        }
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "INSERT dbo.HivePositionBaselines VALUES(@w,@s,@id,@hash,@source,SYSUTCDATETIME())",
            ("@w", world), ("@s", surface), ("@id", baseline.BaselineId), ("@hash", hash), ("@source", baseline.SourceHash))) command.ExecuteNonQuery();
        foreach (var entry in baseline.Entries)
        {
            using var command = SqlTerritoryStore.Command(connection, transaction,
                "INSERT dbo.HiveMapPositions VALUES(@colony,@w,@s,@player,@legacy,@x,@y,1)",
                ("@colony", entry.ColonyId), ("@w", world), ("@s", surface), ("@player", entry.PlayerId), ("@legacy", entry.LegacyNodeId));
            command.Parameters.Add("@x", SqlDbType.Real).Value = entry.X;
            command.Parameters.Add("@y", SqlDbType.Real).Value = entry.Y;
            command.ExecuteNonQuery();
        }
        // Positions are not physical footprints. Never certify placement coverage from points.
        var next = scope with { Revision = checked(scope.Revision + 1), CompletePlacementCoverage = false };
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "UPDATE dbo.TerritoryWorldScopes SET Revision=@r,ConfigJson=@j WHERE WorldId=@w AND SurfaceId=@s; UPDATE dbo.TerritorySpatialSourceVersions SET Revision=Revision+1,Pending=1 WHERE WorldId=@w AND SurfaceId=@s AND Kind=N'hives';",
            ("@r", next.Revision), ("@j", JsonSerializer.Serialize(next)), ("@w", world), ("@s", surface))) command.ExecuteNonQuery();
        beforeCommit?.Invoke();
        transaction.Commit();
        return new(true, false, baseline.Entries.Length);
    }

    // Trusted server/operator diagnostic, deliberately not exposed as a player endpoint.
    // Never assigns coordinates or treats a point inventory as certified physical coverage.
    public HivePositionReconciliation InspectReconciliation()
    {
        RequireEnabled();
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var binding = ReadBinding(connection, transaction);
        bool adopted;
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "SELECT COUNT(*) FROM dbo.HivePositionBaselines WITH(HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s", ("@w", world), ("@s", surface)))
            adopted = Convert.ToInt32(command.ExecuteScalar()) == 1;
        var issues = new List<HiveReconciliationIssue>();
        int active = 0, matched = 0;
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            """
            SELECT c.ColonyId,c.PlayerId,c.WorldId,c.Status,p.PlayerId,p.ColonyId
            FROM dbo.Colonies c WITH(HOLDLOCK)
            FULL OUTER JOIN (SELECT ColonyId,PlayerId FROM dbo.HiveMapPositions WITH(HOLDLOCK)
                            WHERE WorldId=@w AND SurfaceId=@s) p ON p.ColonyId=c.ColonyId
            WHERE c.WorldId=@cw OR p.ColonyId IS NOT NULL
            """, ("@w", world), ("@s", surface), ("@cw", binding.ColonyWorld)))
        using (var reader = command.ExecuteReader())
            while (reader.Read())
            {
                Guid id = reader.IsDBNull(0) ? reader.GetGuid(5) : reader.GetGuid(0);
                if (reader.IsDBNull(0) || reader.GetGuid(2) != binding.ColonyWorld)
                    issues.Add(new(id, "position_world_reconciliation_required"));
                else if (reader.GetInt32(3) != 1)
                {
                    if (!reader.IsDBNull(5)) issues.Add(new(id, "inactive_position_retained"));
                }
                else
                {
                    active++;
                    if (reader.IsDBNull(5)) issues.Add(new(id, "position_missing"));
                    else if (reader.GetGuid(1) != reader.GetGuid(4)) issues.Add(new(id, "position_owner_reconciliation_required"));
                    else matched++;
                }
            }
        return new(world, surface, adopted, active, matched, issues.OrderBy(x => x.ColonyId).ToArray());
    }

    public PersistedHivePosition? ReadOwn(Guid player, Guid colony)
    {
        RequireEnabled();
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var binding = ReadBinding(connection, transaction);
        using (var command = SqlTerritoryStore.Command(connection, transaction,
            "SELECT COUNT(*) FROM dbo.Colonies WITH(HOLDLOCK) WHERE ColonyId=@c AND PlayerId=@p AND WorldId=@cw AND Status=1",
            ("@c", colony), ("@p", player), ("@cw", binding.ColonyWorld)))
            if (Convert.ToInt32(command.ExecuteScalar()) != 1) throw new InvalidOperationException("hive_colony_scope_denied");
        using var read = SqlTerritoryStore.Command(connection, transaction,
            "SELECT LegacyNodeId,X,Y,Revision,PlayerId FROM dbo.HiveMapPositions WITH(HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s AND ColonyId=@c",
            ("@w", world), ("@s", surface), ("@c", colony));
        using var reader = read.ExecuteReader();
        if (!reader.Read()) return null; // Explicitly unplaced; no random or seeded replacement.
        if (reader.GetGuid(4) != player) throw new InvalidOperationException("hive_position_reconciliation_required");
        return new(colony, player, world, surface, binding.Unit, reader.GetString(0), reader.GetFloat(1), reader.GetFloat(2), reader.GetInt64(3));
    }
}
