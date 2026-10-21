using System.Data;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// Runs ONLY inside the canonical flight settlement SERIALIZABLE transaction.
/// No contribution/event can exist without the matching pollen debit/credit.
/// Membership is read from the actual Alliance platform tables, not a replica.
/// </summary>
internal static class SqlFloralAllianceContributionApplier
{
    public static async Task ApplyAsync(SqlConnection conn, SqlTransaction tx,
        CivilExpeditionMission flight, CivilExpeditionContribution delivered,
        FloralAllianceProjectOptions options, int timeoutSeconds,
        CancellationToken ct)
    {
        if (!options.Enabled || delivered.PollinationPoints <= 0) return;
        options.Validate();
        Guid? allianceId = await MembershipAllianceAsync(
            conn, tx, delivered.PlayerId, ct);
        if (!allianceId.HasValue || flight.AllianceIdAtStart != allianceId) return;

        // Lock in canonical order: hive -> floral -> floral-alliance.
        // Creation only locks floral-alliance; it never takes hive/floral locks.
        await SqlFloralAllianceProjectService.LockAllianceAsync(
            conn, tx, allianceId.Value, timeoutSeconds, ct);

        (Guid id, long revision, long points, long goal,
            int qualified, int required, long minMember)? target =
            await ActiveProjectAsync(conn, tx,
                allianceId.Value, delivered.WorldId,
                delivered.SiteId, flight.StartedAtUtc, ct);
        if (target is null) return; // Public solo flight is still meaningful.

        Guid projectId = target.Value.id;
        long credited = delivered.PollinationPoints;
        await using (var insert = Command(conn, tx))
        {
            insert.CommandText = """
                INSERT dbo.FloralAllianceProjectDeliveries
                    (MissionId,ProjectId,PlayerId,Points,DeliveredAtUtc)
                VALUES(@mission,@project,@player,@points,@at);
                """;
            insert.Parameters.AddWithValue("@mission", delivered.MissionId);
            insert.Parameters.AddWithValue("@project", projectId);
            insert.Parameters.AddWithValue("@player", delivered.PlayerId);
            insert.Parameters.AddWithValue("@points", credited);
            insert.Parameters.AddWithValue("@at", delivered.AtUtc.UtcDateTime);
            await insert.ExecuteNonQueryAsync(ct);
        }
        await using (var command = Command(conn, tx))
        {
            command.CommandText = """
                UPDATE dbo.FloralAllianceProjectMembers
                SET Points=Points+@points,LastContributionAtUtc=@at
                WHERE ProjectId=@project AND PlayerId=@player;
                IF @@ROWCOUNT=0
                    INSERT dbo.FloralAllianceProjectMembers
                        (ProjectId,PlayerId,Points,FirstContributionAtUtc,
                        LastContributionAtUtc)
                    VALUES(@project,@player,@points,@at,@at);
                """;
            command.Parameters.AddWithValue("@project", projectId);
            command.Parameters.AddWithValue("@player", delivered.PlayerId);
            command.Parameters.AddWithValue("@points", credited);
            command.Parameters.AddWithValue("@at", delivered.AtUtc.UtcDateTime);
            await command.ExecuteNonQueryAsync(ct);
        }

        int qualified;
        await using (var count = Command(conn, tx))
        {
            count.CommandText = """
                SELECT COUNT(*) FROM dbo.FloralAllianceProjectMembers
                WHERE ProjectId=@project AND Points>=@minimum;
                """;
            count.Parameters.AddWithValue("@project", projectId);
            count.Parameters.AddWithValue("@minimum", target.Value.minMember);
            qualified = Convert.ToInt32(await count.ExecuteScalarAsync(ct));
        }

        long total = checked(target.Value.points + credited);
        bool completed = total >= target.Value.goal
            && qualified >= target.Value.required;
        await using (var command = Command(conn, tx))
        {
            command.CommandText = """
                UPDATE dbo.FloralAllianceProjects
                SET Revision=Revision+1,Points=@points,
                    QualifiedMembers=@qualified,
                    Status=CASE WHEN @completed=1
                        THEN N'Completed' ELSE N'Active' END,
                    CompletedAtUtc=CASE WHEN @completed=1
                        THEN @now ELSE NULL END
                WHERE ProjectId=@project AND Revision=@before
                    AND Status=N'Active';
                IF @@ROWCOUNT<>1
                    THROW 51086,'Stale floral project revision.',1;
                """;
            command.Parameters.AddWithValue("@project", projectId);
            command.Parameters.AddWithValue("@before", target.Value.revision);
            command.Parameters.AddWithValue("@points", total);
            command.Parameters.AddWithValue("@qualified", qualified);
            command.Parameters.AddWithValue("@completed", completed);
            command.Parameters.AddWithValue("@now", delivered.AtUtc.UtcDateTime);
            await command.ExecuteNonQueryAsync(ct);
        }
        if (completed)
        {
            FloralAllianceProgression old =
                await SqlFloralAllianceProjectService.ReadProgressionAsync(
                    conn, tx, allianceId.Value, ct);
            long experience = checked(old.Experience + target.Value.goal);
            int level = Math.Max(old.Level, options.Level(experience));
            await using var command = Command(conn, tx);
            command.CommandText = """
                UPDATE dbo.FloralAllianceProgression
                SET Revision=Revision+1,Experience=@experience,
                    Level=@level,CompletedProjects=CompletedProjects+1
                WHERE AllianceId=@alliance AND Revision=@revision;
                IF @@ROWCOUNT<>1
                    THROW 51087,'Alliance progression revision changed.',1;
                """;
            command.Parameters.AddWithValue("@alliance", allianceId.Value);
            command.Parameters.AddWithValue("@revision", old.Revision);
            command.Parameters.AddWithValue("@experience", experience);
            command.Parameters.AddWithValue("@level", level);
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    internal static async Task<Guid?> MembershipAllianceAsync(
        SqlConnection conn, SqlTransaction tx, Guid playerId, CancellationToken ct)
    {
        await using var command = Command(conn, tx);
        command.CommandText = """
            SELECT TOP (1) m.AllianceId
            FROM dbo.AllianceMemberships m WITH (HOLDLOCK)
            INNER JOIN dbo.Alliances a WITH (HOLDLOCK)
                ON a.AllianceId=m.AllianceId
            WHERE m.PlayerId=@player AND m.RemovedAtUtc IS NULL
                AND a.Status=N'Active';
            """;
        command.Parameters.AddWithValue("@player", playerId);
        object? result = await command.ExecuteScalarAsync(ct);
        return result is Guid alliance ? alliance : null;
    }

    private static async Task<(Guid id, long revision, long points,
        long goal, int qualified, int required, long minMember)?>
        ActiveProjectAsync(SqlConnection conn, SqlTransaction tx,
            Guid allianceId, string worldId, string siteId,
            DateTimeOffset missionStartedAt, CancellationToken ct)
    {
        await using var command = Command(conn, tx);
        command.CommandText = """
            SELECT TOP (1) ProjectId,Revision,Points,GoalPoints,
                QualifiedMembers,RequiredDistinctMembers,MinimumPointsPerMember
            FROM dbo.FloralAllianceProjects WITH (UPDLOCK,HOLDLOCK)
            WHERE AllianceId=@alliance AND WorldId=@world
                AND SiteId=@site AND Status=N'Active'
                AND CreatedAtUtc<=@started
            ORDER BY CreatedAtUtc;
            """;
        command.Parameters.AddWithValue("@alliance", allianceId);
        command.Parameters.AddWithValue("@world", worldId);
        command.Parameters.AddWithValue("@site", siteId);
        command.Parameters.AddWithValue("@started", missionStartedAt.UtcDateTime);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return (reader.GetGuid(0), reader.GetInt64(1), reader.GetInt64(2),
            reader.GetInt64(3), reader.GetInt32(4), reader.GetInt32(5),
            reader.GetInt64(6));
    }

    private static SqlCommand Command(SqlConnection conn, SqlTransaction tx) =>
        new() { Connection=conn, Transaction=tx };
}
