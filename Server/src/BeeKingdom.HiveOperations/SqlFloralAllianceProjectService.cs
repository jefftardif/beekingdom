using System.Data;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// The only writable Alpha project backend. Reads REAL alliance membership
/// and role from dbo.AllianceMemberships in the same database. Never equates
/// Alliance Research to an alliance level. All tuning is QA-only.
/// </summary>
public sealed class SqlFloralAllianceProjectService(
    string connectionString, IServerClock clock,
    FloralAllianceProjectOptions options, FloralSiteOptions floral,
    int timeoutSeconds = 15) : IFloralAllianceProjectService
{
    public const string ContractVersion = "bk-floral-alliance-project-v1-alpha";
    private FloralAllianceProjectOptions Rules => options;

    public async Task<FloralAllianceProjectCreateResult> CreateAsync(
        Guid playerId, Guid allianceId, CreateFloralAllianceProjectRequest request,
        CancellationToken ct = default)
    {
        EnsureWritable();
        EnsureIds(playerId, allianceId);
        if (request is null || request.ExpectedProgressionRevision < 0
            || !CivilExpeditionRules.ValidKey(request.IdempotencyKey)
            || !FloralSiteOptions.ValidId(request.SiteId))
            throw new ArgumentException("game.invalid_request");
        FloralSiteDefinition definition = RequireSite(request.SiteId);
        string payloadHash = CivilExpeditionRules.Hash(
            "project-create|" + request.SiteId + "|" + request.ExpectedProgressionRevision);
        DateTimeOffset now = clock.UtcNow.ToUniversalTime();

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlTransaction tx = (SqlTransaction)
            await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAllianceAsync(connection, tx, allianceId, timeoutSeconds, ct);
        string role = await RequiredMembershipRoleAsync(
            connection, tx, playerId, allianceId, ct);
        if (role is not ("Officer" or "Leader"))
            throw new UnauthorizedAccessException("game.alliance_permission_denied");

        FloralAllianceProject? existing = await GetByCreateKeyAsync(
            connection, tx, allianceId, request.IdempotencyKey, ct);
        if (existing != null)
        {
            string storedHash = await ReadCreateHashAsync(
                connection, tx, existing.ProjectId, ct);
            if (existing.CreatedByPlayerId != playerId || storedHash != payloadHash)
                return new(false, "game.idempotency_conflict", null);
            FloralAllianceProgression prev = await ReadProgressionAsync(
                connection, tx, allianceId, ct);
            await tx.CommitAsync(ct);
            return new(true, "game.floral_project_created",
                Snapshot(existing, prev, now));
        }

        FloralAllianceProgression state = await ReadProgressionAsync(
            connection, tx, allianceId, ct);
        if (state.Revision != request.ExpectedProgressionRevision)
            return new(false, "game.revision_conflict", null);
        int activeCount = await CountActiveAsync(connection, tx, allianceId, ct);
        if (activeCount >= Rules.ActiveSlotsForLevel(state.Level))
            return new(false, "game.alliance_project_slots_full", null);
        if (await ActiveAtSiteAsync(connection, tx,
                allianceId, definition, ct))
            return new(false, "game.floral_project_duplicate_site", null);

        Guid id = Guid.NewGuid();
        var project = new FloralAllianceProject(
            id, allianceId, definition.WorldId, definition.SiteId,
            playerId, now, 0, Rules.GoalPoints,
            Rules.RequiredDistinctMembers, Rules.MinimumPointsPerMember,
            0, 0, "Active", null);

        await using (var command = Command(connection, tx))
        {
            command.CommandText = """
                INSERT dbo.FloralAllianceProjects
                (ProjectId,AllianceId,WorldId,SiteId,CreatedByPlayerId,
                CreateIdempotencyKey,CreatePayloadHash,CreatedAtUtc,Revision,
                GoalPoints,RequiredDistinctMembers,MinimumPointsPerMember,
                Points,QualifiedMembers,Status,CompletedAtUtc)
                VALUES (@id,@alliance,@world,@site,@creator,@key,@hash,@at,
                        0,@goal,@members,@minimum,0,0,N'Active',NULL);
                """;
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@alliance", allianceId);
            command.Parameters.AddWithValue("@world", definition.WorldId);
            command.Parameters.AddWithValue("@site", definition.SiteId);
            command.Parameters.AddWithValue("@creator", playerId);
            command.Parameters.AddWithValue("@key", request.IdempotencyKey);
            command.Parameters.AddWithValue("@hash", payloadHash);
            command.Parameters.AddWithValue("@at", now.UtcDateTime);
            command.Parameters.AddWithValue("@goal", project.GoalPoints);
            command.Parameters.AddWithValue("@members", project.RequiredDistinctMembers);
            command.Parameters.AddWithValue("@minimum", project.MinimumPointsPerMember);
            await command.ExecuteNonQueryAsync(ct);
        }
        FloralAllianceProgression changed = state with
        {
            Revision = checked(state.Revision + 1)
        };
        await PersistProgressionAsync(connection, tx, changed, state.Revision, ct);
        await tx.CommitAsync(ct);
        return new(true, "game.floral_project_created",
            Snapshot(project, changed, now));
    }

    public async Task<FloralAllianceProjectSnapshot> ReadAsync(
        Guid playerId, Guid allianceId, Guid projectId,
        CancellationToken ct = default)
    {
        EnsureReadable();
        EnsureIds(playerId, allianceId);
        if (projectId == Guid.Empty)
            throw new ArgumentException("game.invalid_request");
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlTransaction tx = (SqlTransaction)
            await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        _ = await RequiredMembershipRoleAsync(connection, tx, playerId, allianceId, ct);
        FloralAllianceProject project = await ReadProjectAsync(
            connection, tx, projectId, ct)
            ?? throw new KeyNotFoundException("game.not_found");
        if (project.AllianceId != allianceId)
            throw new KeyNotFoundException("game.not_found");
        FloralAllianceProgression progress = await ReadProgressionAsync(
            connection, tx, allianceId, ct);
        await tx.CommitAsync(ct);
        return Snapshot(project, progress, clock.UtcNow.ToUniversalTime());
    }

    public async Task<IReadOnlyList<FloralAllianceProjectSnapshot>> ListAsync(
        Guid playerId, Guid allianceId, CancellationToken ct = default)
    {
        EnsureReadable();
        EnsureIds(playerId, allianceId);
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlTransaction tx = (SqlTransaction)
            await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        _ = await RequiredMembershipRoleAsync(connection, tx, playerId, allianceId, ct);
        var results = new List<FloralAllianceProject>();
        await using (SqlCommand command = Command(connection, tx))
        {
            command.CommandText = """
                SELECT TOP (100) ProjectId,AllianceId,WorldId,SiteId,
                    CreatedByPlayerId,CreatedAtUtc,Revision,GoalPoints,
                    RequiredDistinctMembers,MinimumPointsPerMember,
                    Points,QualifiedMembers,Status,CompletedAtUtc
                FROM dbo.FloralAllianceProjects
                WHERE AllianceId=@alliance ORDER BY CreatedAtUtc DESC,ProjectId;
                """;
            command.Parameters.AddWithValue("@alliance", allianceId);
            await using SqlDataReader reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) results.Add(ParseProject(reader));
        }
        FloralAllianceProgression progress = await ReadProgressionAsync(
            connection, tx, allianceId, ct);
        await tx.CommitAsync(ct);
        DateTimeOffset now = clock.UtcNow.ToUniversalTime();
        return results.Select(x => Snapshot(x, progress, now)).ToList();
    }

    private static FloralAllianceProjectSnapshot Snapshot(
        FloralAllianceProject project, FloralAllianceProgression progress,
        DateTimeOffset now) => new(ContractVersion, now, project,
            progress, project.Status == "Completed");

    private void EnsureReadable()
    {
        Rules.Validate();
        floral.Validate();
        if (!Rules.CanRead || !floral.Enabled)
            throw new InvalidOperationException("game.floral_alliance_projects_disabled");
    }

    private void EnsureWritable()
    {
        EnsureReadable();
        if (!Rules.CanCreate)
            throw new InvalidOperationException("game.floral_alliance_projects_readonly");
    }

    private FloralSiteDefinition RequireSite(string siteId) =>
        floral.Sites.SingleOrDefault(x => x.SiteId == siteId)
            ?? throw new KeyNotFoundException("game.floral_site_not_found");

    private static void EnsureIds(Guid player, Guid alliance)
    {
        if (player == Guid.Empty || alliance == Guid.Empty)
            throw new ArgumentException("game.invalid_request");
    }

    internal static async Task LockAllianceAsync(
        SqlConnection conn, SqlTransaction tx, Guid allianceId,
        int timeoutSeconds, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandTimeout = timeoutSeconds;
        cmd.CommandText = """
            DECLARE @code int;
            EXEC @code=sys.sp_getapplock @Resource=@resource,
                @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=@timeout;
            IF @code < 0 THROW 51081,'Floral alliance lock unavailable.',1;
            """;
        cmd.Parameters.AddWithValue("@resource", $"floral-alliance:{allianceId:N}");
        cmd.Parameters.AddWithValue("@timeout", timeoutSeconds * 1000);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    internal static async Task<string> RequiredMembershipRoleAsync(
        SqlConnection conn, SqlTransaction tx, Guid playerId,
        Guid allianceId, CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection = conn, Transaction = tx,
            CommandText = """
                SELECT m.Role FROM dbo.AllianceMemberships m WITH (HOLDLOCK)
                INNER JOIN dbo.Alliances a WITH (HOLDLOCK)
                    ON a.AllianceId=m.AllianceId
                WHERE m.AllianceId=@alliance AND m.PlayerId=@player
                    AND m.RemovedAtUtc IS NULL AND a.Status=N'Active';
                """
        };
        cmd.Parameters.AddWithValue("@alliance", allianceId);
        cmd.Parameters.AddWithValue("@player", playerId);
        object? role = await cmd.ExecuteScalarAsync(ct);
        return role as string
            ?? throw new UnauthorizedAccessException("game.alliance_member_required");
    }

    internal static FloralAllianceProgression StartingProgression(Guid allianceId) =>
        new(allianceId, 0, 1, 0, 0);

    internal static async Task<FloralAllianceProgression> ReadProgressionAsync(
        SqlConnection conn, SqlTransaction tx, Guid allianceId, CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection = conn, Transaction = tx,
            CommandText = """
                SELECT Revision,Level,Experience,CompletedProjects
                FROM dbo.FloralAllianceProgression WITH (UPDLOCK,HOLDLOCK)
                WHERE AllianceId=@alliance;
                """
        };
        cmd.Parameters.AddWithValue("@alliance", allianceId);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new(allianceId, reader.GetInt64(0),reader.GetInt32(1),
                reader.GetInt64(2),reader.GetInt64(3))
            : StartingProgression(allianceId);
    }

    private static async Task PersistProgressionAsync(
        SqlConnection conn, SqlTransaction tx, FloralAllianceProgression state,
        long expectedRevision, CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection = conn, Transaction = tx,
            CommandText = """
                UPDATE dbo.FloralAllianceProgression
                SET Revision=@revision,Level=@level,Experience=@xp,
                    CompletedProjects=@done
                WHERE AllianceId=@alliance AND Revision=@before;
                IF @@ROWCOUNT=0 AND @before=0
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.FloralAllianceProgression
                        WHERE AllianceId=@alliance)
                        INSERT dbo.FloralAllianceProgression
                        (AllianceId,Revision,Level,Experience,CompletedProjects)
                        VALUES(@alliance,@revision,@level,@xp,@done);
                END;
                """
        };
        cmd.Parameters.AddWithValue("@alliance", state.AllianceId);
        cmd.Parameters.AddWithValue("@revision", state.Revision);
        cmd.Parameters.AddWithValue("@before", expectedRevision);
        cmd.Parameters.AddWithValue("@level", state.Level);
        cmd.Parameters.AddWithValue("@xp", state.Experience);
        cmd.Parameters.AddWithValue("@done", state.CompletedProjects);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> CountActiveAsync(
        SqlConnection conn, SqlTransaction tx, Guid allianceId,
        CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection=conn, Transaction=tx,
            CommandText = """
                SELECT COUNT(*) FROM dbo.FloralAllianceProjects WITH (UPDLOCK,HOLDLOCK)
                WHERE AllianceId=@alliance AND Status=N'Active';
                """
        };
        cmd.Parameters.AddWithValue("@alliance", allianceId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private static async Task<bool> ActiveAtSiteAsync(SqlConnection conn,
        SqlTransaction tx, Guid allianceId, FloralSiteDefinition site,
        CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection=conn, Transaction=tx,
            CommandText = """
                SELECT COUNT(*) FROM dbo.FloralAllianceProjects WITH (UPDLOCK,HOLDLOCK)
                WHERE AllianceId=@alliance AND WorldId=@world
                    AND SiteId=@site AND Status=N'Active';
                """
        };
        cmd.Parameters.AddWithValue("@alliance", allianceId);
        cmd.Parameters.AddWithValue("@world", site.WorldId);
        cmd.Parameters.AddWithValue("@site", site.SiteId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }

    private static async Task<FloralAllianceProject?> GetByCreateKeyAsync(
        SqlConnection conn, SqlTransaction tx, Guid allianceId,
        string key, CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection=conn, Transaction=tx,
            CommandText = """
                SELECT ProjectId,AllianceId,WorldId,SiteId,CreatedByPlayerId,
                    CreatedAtUtc,Revision,GoalPoints,RequiredDistinctMembers,
                    MinimumPointsPerMember,Points,QualifiedMembers,Status,CompletedAtUtc
                FROM dbo.FloralAllianceProjects WITH (UPDLOCK,HOLDLOCK)
                WHERE AllianceId=@alliance AND CreateIdempotencyKey=@key;
                """
        };
        cmd.Parameters.AddWithValue("@alliance", allianceId);
        cmd.Parameters.AddWithValue("@key", key);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ParseProject(reader) : null;
    }

    private static async Task<string> ReadCreateHashAsync(SqlConnection conn,
        SqlTransaction tx, Guid projectId, CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection=conn, Transaction=tx,
            CommandText = """
                SELECT CreatePayloadHash FROM dbo.FloralAllianceProjects
                WHERE ProjectId=@id;
                """
        };
        cmd.Parameters.AddWithValue("@id", projectId);
        return (string)(await cmd.ExecuteScalarAsync(ct)
            ?? throw new InvalidDataException("Project creation hash missing."));
    }

    private static async Task<FloralAllianceProject?> ReadProjectAsync(
        SqlConnection conn, SqlTransaction tx, Guid projectId,
        CancellationToken ct)
    {
        await using SqlCommand cmd = new()
        {
            Connection=conn, Transaction=tx,
            CommandText = """
                SELECT ProjectId,AllianceId,WorldId,SiteId,CreatedByPlayerId,
                    CreatedAtUtc,Revision,GoalPoints,RequiredDistinctMembers,
                    MinimumPointsPerMember,Points,QualifiedMembers,Status,CompletedAtUtc
                FROM dbo.FloralAllianceProjects WHERE ProjectId=@id;
                """
        };
        cmd.Parameters.AddWithValue("@id", projectId);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ParseProject(reader) : null;
    }

    private static FloralAllianceProject ParseProject(SqlDataReader reader) =>
        new(reader.GetGuid(0),reader.GetGuid(1),reader.GetString(2),
            reader.GetString(3),reader.GetGuid(4),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5),DateTimeKind.Utc)),
            reader.GetInt64(6),reader.GetInt64(7),reader.GetInt32(8),
            reader.GetInt64(9),reader.GetInt64(10),reader.GetInt32(11),
            reader.GetString(12),
            reader.IsDBNull(13) ? null : new DateTimeOffset(
                DateTime.SpecifyKind(reader.GetDateTime(13),DateTimeKind.Utc)));

    private static SqlCommand Command(SqlConnection connection, SqlTransaction tx) =>
        new() {Connection = connection, Transaction = tx};
}

public sealed class UnavailableFloralAllianceProjectService : IFloralAllianceProjectService
{
    public Task<FloralAllianceProjectCreateResult> CreateAsync(
        Guid playerId, Guid allianceId, CreateFloralAllianceProjectRequest request,
        CancellationToken ct = default)
        => throw new NotSupportedException("SQL-only collective projects are not available.");

    public Task<FloralAllianceProjectSnapshot> ReadAsync(
        Guid playerId, Guid allianceId, Guid projectId, CancellationToken ct = default)
        => throw new NotSupportedException("SQL-only collective projects are not available.");

    public Task<IReadOnlyList<FloralAllianceProjectSnapshot>> ListAsync(
        Guid playerId, Guid allianceId, CancellationToken ct = default)
        => throw new NotSupportedException("SQL-only collective projects are not available.");
}
