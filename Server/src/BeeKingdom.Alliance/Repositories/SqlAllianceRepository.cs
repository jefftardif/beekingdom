using System.Data;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class SqlAllianceRepository : IAllianceRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAllianceRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public AllianceEntity Save(AllianceEntity alliance)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.Alliances AS target
            USING (SELECT @AllianceId AS AllianceId) AS source
            ON target.AllianceId = source.AllianceId
            WHEN MATCHED THEN
                UPDATE SET Name = @Name, Tag = @Tag, Description = @Description, Language = @Language,
                           EmblemKey = @EmblemKey, JoinMode = @JoinMode, Status = @Status,
                           CreatedAtUtc = @CreatedAtUtc, CreatedByPlayerId = @CreatedByPlayerId,
                           LeaderPlayerId = @LeaderPlayerId, MemberCount = @MemberCount, MaxMembers = @MaxMembers,
                           PublicSlug = @PublicSlug, ChatConversationId = @ChatConversationId,
                           Revision = @Revision, DisbandedAtUtc = @DisbandedAtUtc
            WHEN NOT MATCHED THEN
                INSERT (AllianceId, Name, Tag, Description, Language, EmblemKey, JoinMode, Status, CreatedAtUtc,
                        CreatedByPlayerId, LeaderPlayerId, MemberCount, MaxMembers, PublicSlug, ChatConversationId,
                        Revision, DisbandedAtUtc)
                VALUES (@AllianceId, @Name, @Tag, @Description, @Language, @EmblemKey, @JoinMode, @Status, @CreatedAtUtc,
                        @CreatedByPlayerId, @LeaderPlayerId, @MemberCount, @MaxMembers, @PublicSlug, @ChatConversationId,
                        @Revision, @DisbandedAtUtc);
            """;
        AddAllianceParameters(command, alliance);
        command.ExecuteNonQuery();
        return alliance;
    }

    public AllianceEntity? Get(AllianceId allianceId)
        => QuerySingleAlliance("WHERE AllianceId = @AllianceId", command => Add(command, "@AllianceId", allianceId.Value));

    public AllianceEntity? GetBySlug(string slug)
        => QuerySingleAlliance("WHERE PublicSlug = @PublicSlug", command => Add(command, "@PublicSlug", slug));

    public IReadOnlyList<AllianceEntity> Search(AllianceSearchQuery query, out int totalCount)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();

        List<string> filters = new() { "Status = @Status" };
        void AddFilters(IDbCommand command)
        {
            Add(command, "@Status", AllianceStatus.Active.ToString());
            if (!string.IsNullOrWhiteSpace(query.NameOrTag))
            {
                Add(command, "@NameOrTag", $"%{query.NameOrTag.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(query.Language))
            {
                Add(command, "@Language", query.Language);
            }
            if (query.JoinMode.HasValue)
            {
                Add(command, "@JoinMode", query.JoinMode.Value.ToString());
            }
        }
        if (!string.IsNullOrWhiteSpace(query.NameOrTag)) filters.Add("(Name LIKE @NameOrTag OR Tag LIKE @NameOrTag)");
        if (!string.IsNullOrWhiteSpace(query.Language)) filters.Add("Language = @Language");
        if (query.JoinMode.HasValue) filters.Add("JoinMode = @JoinMode");
        string whereClause = "WHERE " + string.Join(" AND ", filters);

        using IDbCommand countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(*) FROM dbo.Alliances {whereClause};";
        AddFilters(countCommand);
        totalCount = Convert.ToInt32(countCommand.ExecuteScalar());

        int offset = Math.Max(0, query.Offset);
        int limit = Math.Clamp(query.Limit, 1, 200);
        using IDbCommand pageCommand = connection.CreateCommand();
        pageCommand.CommandText = $"""
            SELECT * FROM dbo.Alliances {whereClause}
            ORDER BY MemberCount DESC, AllianceId ASC
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
            """;
        AddFilters(pageCommand);
        Add(pageCommand, "@Offset", offset);
        Add(pageCommand, "@Limit", limit);
        using IDataReader reader = pageCommand.ExecuteReader();
        List<AllianceEntity> results = new();
        while (reader.Read())
        {
            results.Add(ReadAlliance(reader));
        }
        return results;
    }

    public AllianceId? GetCreateReceipt(PlayerId playerId, string clientRequestId)
        => QueryReceipt("dbo.AllianceCreateReceipts", "AllianceId", playerId, clientRequestId) is Guid id ? new AllianceId(id) : null;

    public void SaveCreateReceipt(PlayerId playerId, string clientRequestId, AllianceId allianceId)
        => SaveReceipt("dbo.AllianceCreateReceipts", "AllianceId", playerId, clientRequestId, allianceId.Value);

    public AllianceMembership SaveMembership(AllianceMembership membership)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AllianceMemberships AS target
            USING (SELECT @AllianceId AS AllianceId, @PlayerId AS PlayerId) AS source
            ON target.AllianceId = source.AllianceId AND target.PlayerId = source.PlayerId
            WHEN MATCHED THEN
                UPDATE SET Role = @Role, JoinedAtUtc = @JoinedAtUtc, InvitedByPlayerId = @InvitedByPlayerId,
                           ApplicationId = @ApplicationId, LastRoleChangedAtUtc = @LastRoleChangedAtUtc,
                           RemovedAtUtc = @RemovedAtUtc, Revision = @Revision
            WHEN NOT MATCHED THEN
                INSERT (AllianceId, PlayerId, Role, JoinedAtUtc, InvitedByPlayerId, ApplicationId,
                        LastRoleChangedAtUtc, RemovedAtUtc, Revision)
                VALUES (@AllianceId, @PlayerId, @Role, @JoinedAtUtc, @InvitedByPlayerId, @ApplicationId,
                        @LastRoleChangedAtUtc, @RemovedAtUtc, @Revision);
            """;
        AddMembershipParameters(command, membership);
        command.ExecuteNonQuery();
        return membership;
    }

    public AllianceMembership? GetActiveMembership(AllianceId allianceId, PlayerId playerId)
        => QuerySingleMembership(
            "WHERE AllianceId = @AllianceId AND PlayerId = @PlayerId AND RemovedAtUtc IS NULL",
            command => { Add(command, "@AllianceId", allianceId.Value); Add(command, "@PlayerId", playerId.Value); });

    public AllianceMembership? GetActiveMembershipForPlayer(PlayerId playerId)
        => QuerySingleMembership(
            "WHERE PlayerId = @PlayerId AND RemovedAtUtc IS NULL",
            command => Add(command, "@PlayerId", playerId.Value));

    public IReadOnlyList<AllianceMembership> ListActiveMembers(AllianceId allianceId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM dbo.AllianceMemberships
            WHERE AllianceId = @AllianceId AND RemovedAtUtc IS NULL
            ORDER BY Role DESC, JoinedAtUtc ASC;
            """;
        Add(command, "@AllianceId", allianceId.Value);
        using IDataReader reader = command.ExecuteReader();
        List<AllianceMembership> results = new();
        while (reader.Read())
        {
            results.Add(ReadMembership(reader));
        }
        return results;
    }

    public AllianceApplication SaveApplication(AllianceApplication application)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AllianceApplications AS target
            USING (SELECT @ApplicationId AS ApplicationId) AS source
            ON target.ApplicationId = source.ApplicationId
            WHEN MATCHED THEN
                UPDATE SET AllianceId = @AllianceId, PlayerId = @PlayerId, Status = @Status,
                           SubmittedAtUtc = @SubmittedAtUtc, RespondedAtUtc = @RespondedAtUtc,
                           RespondedByPlayerId = @RespondedByPlayerId, Message = @Message
            WHEN NOT MATCHED THEN
                INSERT (ApplicationId, AllianceId, PlayerId, Status, SubmittedAtUtc, RespondedAtUtc,
                        RespondedByPlayerId, Message)
                VALUES (@ApplicationId, @AllianceId, @PlayerId, @Status, @SubmittedAtUtc, @RespondedAtUtc,
                        @RespondedByPlayerId, @Message);
            """;
        AddApplicationParameters(command, application);
        command.ExecuteNonQuery();
        return application;
    }

    public AllianceApplication? GetApplication(Guid applicationId)
        => QuerySingleApplication("WHERE ApplicationId = @ApplicationId", command => Add(command, "@ApplicationId", applicationId));

    public AllianceApplication? GetPendingApplication(AllianceId allianceId, PlayerId playerId)
        => QuerySingleApplication(
            "WHERE AllianceId = @AllianceId AND PlayerId = @PlayerId AND Status = @Status",
            command =>
            {
                Add(command, "@AllianceId", allianceId.Value);
                Add(command, "@PlayerId", playerId.Value);
                Add(command, "@Status", AllianceApplicationStatus.Pending.ToString());
            });

    public IReadOnlyList<AllianceApplication> ListPendingApplications(AllianceId allianceId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM dbo.AllianceApplications
            WHERE AllianceId = @AllianceId AND Status = @Status
            ORDER BY SubmittedAtUtc ASC;
            """;
        Add(command, "@AllianceId", allianceId.Value);
        Add(command, "@Status", AllianceApplicationStatus.Pending.ToString());
        using IDataReader reader = command.ExecuteReader();
        List<AllianceApplication> results = new();
        while (reader.Read())
        {
            results.Add(ReadApplication(reader));
        }
        return results;
    }

    public Guid? GetApplicationReceipt(PlayerId playerId, string clientRequestId)
        => QueryReceipt("dbo.AllianceApplicationReceipts", "ApplicationId", playerId, clientRequestId);

    public void SaveApplicationReceipt(PlayerId playerId, string clientRequestId, Guid applicationId)
        => SaveReceipt("dbo.AllianceApplicationReceipts", "ApplicationId", playerId, clientRequestId, applicationId);

    public AllianceInvitation SaveInvitation(AllianceInvitation invitation)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AllianceInvitations AS target
            USING (SELECT @InvitationId AS InvitationId) AS source
            ON target.InvitationId = source.InvitationId
            WHEN MATCHED THEN
                UPDATE SET AllianceId = @AllianceId, InvitedPlayerId = @InvitedPlayerId,
                           InvitedByPlayerId = @InvitedByPlayerId, Status = @Status,
                           CreatedAtUtc = @CreatedAtUtc, RespondedAtUtc = @RespondedAtUtc
            WHEN NOT MATCHED THEN
                INSERT (InvitationId, AllianceId, InvitedPlayerId, InvitedByPlayerId, Status, CreatedAtUtc, RespondedAtUtc)
                VALUES (@InvitationId, @AllianceId, @InvitedPlayerId, @InvitedByPlayerId, @Status, @CreatedAtUtc, @RespondedAtUtc);
            """;
        AddInvitationParameters(command, invitation);
        command.ExecuteNonQuery();
        return invitation;
    }

    public AllianceInvitation? GetInvitation(Guid invitationId)
        => QuerySingleInvitation("WHERE InvitationId = @InvitationId", command => Add(command, "@InvitationId", invitationId));

    public AllianceInvitation? GetPendingInvitation(AllianceId allianceId, PlayerId invitedPlayerId)
        => QuerySingleInvitation(
            "WHERE AllianceId = @AllianceId AND InvitedPlayerId = @InvitedPlayerId AND Status = @Status",
            command =>
            {
                Add(command, "@AllianceId", allianceId.Value);
                Add(command, "@InvitedPlayerId", invitedPlayerId.Value);
                Add(command, "@Status", AllianceInvitationStatus.Pending.ToString());
            });

    public IReadOnlyList<AllianceInvitation> ListPendingInvitationsForPlayer(PlayerId playerId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM dbo.AllianceInvitations
            WHERE InvitedPlayerId = @InvitedPlayerId AND Status = @Status
            ORDER BY CreatedAtUtc DESC;
            """;
        Add(command, "@InvitedPlayerId", playerId.Value);
        Add(command, "@Status", AllianceInvitationStatus.Pending.ToString());
        using IDataReader reader = command.ExecuteReader();
        List<AllianceInvitation> results = new();
        while (reader.Read())
        {
            results.Add(ReadInvitation(reader));
        }
        return results;
    }

    public Guid? GetInvitationReceipt(PlayerId playerId, string clientRequestId)
        => QueryReceipt("dbo.AllianceInvitationReceipts", "InvitationId", playerId, clientRequestId);

    public void SaveInvitationReceipt(PlayerId playerId, string clientRequestId, Guid invitationId)
        => SaveReceipt("dbo.AllianceInvitationReceipts", "InvitationId", playerId, clientRequestId, invitationId);

    private AllianceEntity? QuerySingleAlliance(string whereClause, Action<IDbCommand> bind)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM dbo.Alliances {whereClause};";
        bind(command);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadAlliance(reader) : null;
    }

    private AllianceMembership? QuerySingleMembership(string whereClause, Action<IDbCommand> bind)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM dbo.AllianceMemberships {whereClause};";
        bind(command);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadMembership(reader) : null;
    }

    private AllianceApplication? QuerySingleApplication(string whereClause, Action<IDbCommand> bind)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM dbo.AllianceApplications {whereClause};";
        bind(command);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadApplication(reader) : null;
    }

    private AllianceInvitation? QuerySingleInvitation(string whereClause, Action<IDbCommand> bind)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM dbo.AllianceInvitations {whereClause};";
        bind(command);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadInvitation(reader) : null;
    }

    private Guid? QueryReceipt(string table, string valueColumn, PlayerId playerId, string clientRequestId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {valueColumn} FROM {table} WHERE PlayerId = @PlayerId AND ClientRequestId = @ClientRequestId;";
        Add(command, "@PlayerId", playerId.Value);
        Add(command, "@ClientRequestId", clientRequestId);
        object? result = command.ExecuteScalar();
        return result is Guid guid ? guid : null;
    }

    private void SaveReceipt(string table, string valueColumn, PlayerId playerId, string clientRequestId, Guid value)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            MERGE {table} AS target
            USING (SELECT @PlayerId AS PlayerId, @ClientRequestId AS ClientRequestId) AS source
            ON target.PlayerId = source.PlayerId AND target.ClientRequestId = source.ClientRequestId
            WHEN MATCHED THEN UPDATE SET {valueColumn} = @Value
            WHEN NOT MATCHED THEN INSERT (PlayerId, ClientRequestId, {valueColumn}) VALUES (@PlayerId, @ClientRequestId, @Value);
            """;
        Add(command, "@PlayerId", playerId.Value);
        Add(command, "@ClientRequestId", clientRequestId);
        Add(command, "@Value", value);
        command.ExecuteNonQuery();
    }

    private static AllianceEntity ReadAlliance(IDataReader reader) => new()
    {
        AllianceId = new AllianceId((Guid)reader["AllianceId"]),
        Name = (string)reader["Name"],
        Tag = (string)reader["Tag"],
        Description = (string)reader["Description"],
        Language = (string)reader["Language"],
        EmblemKey = (string)reader["EmblemKey"],
        JoinMode = Enum.Parse<AllianceJoinMode>((string)reader["JoinMode"]),
        Status = Enum.Parse<AllianceStatus>((string)reader["Status"]),
        CreatedAtUtc = AsUtc((DateTime)reader["CreatedAtUtc"]),
        CreatedByPlayerId = new PlayerId((Guid)reader["CreatedByPlayerId"]),
        LeaderPlayerId = new PlayerId((Guid)reader["LeaderPlayerId"]),
        MemberCount = (int)reader["MemberCount"],
        MaxMembers = (int)reader["MaxMembers"],
        PublicSlug = (string)reader["PublicSlug"],
        ChatConversationId = reader["ChatConversationId"] is DBNull ? null : (Guid)reader["ChatConversationId"],
        Revision = (long)reader["Revision"],
        DisbandedAtUtc = reader["DisbandedAtUtc"] is DBNull ? null : AsUtc((DateTime)reader["DisbandedAtUtc"])
    };

    private static AllianceMembership ReadMembership(IDataReader reader) => new()
    {
        AllianceId = new AllianceId((Guid)reader["AllianceId"]),
        PlayerId = new PlayerId((Guid)reader["PlayerId"]),
        Role = Enum.Parse<AllianceRole>((string)reader["Role"]),
        JoinedAtUtc = AsUtc((DateTime)reader["JoinedAtUtc"]),
        InvitedByPlayerId = reader["InvitedByPlayerId"] is DBNull ? null : new PlayerId((Guid)reader["InvitedByPlayerId"]),
        ApplicationId = reader["ApplicationId"] is DBNull ? null : (Guid)reader["ApplicationId"],
        LastRoleChangedAtUtc = AsUtc((DateTime)reader["LastRoleChangedAtUtc"]),
        RemovedAtUtc = reader["RemovedAtUtc"] is DBNull ? null : AsUtc((DateTime)reader["RemovedAtUtc"]),
        Revision = (long)reader["Revision"]
    };

    private static AllianceApplication ReadApplication(IDataReader reader) => new()
    {
        ApplicationId = (Guid)reader["ApplicationId"],
        AllianceId = new AllianceId((Guid)reader["AllianceId"]),
        PlayerId = new PlayerId((Guid)reader["PlayerId"]),
        Status = Enum.Parse<AllianceApplicationStatus>((string)reader["Status"]),
        SubmittedAtUtc = AsUtc((DateTime)reader["SubmittedAtUtc"]),
        RespondedAtUtc = reader["RespondedAtUtc"] is DBNull ? null : AsUtc((DateTime)reader["RespondedAtUtc"]),
        RespondedByPlayerId = reader["RespondedByPlayerId"] is DBNull ? null : new PlayerId((Guid)reader["RespondedByPlayerId"]),
        Message = (string)reader["Message"]
    };

    private static AllianceInvitation ReadInvitation(IDataReader reader) => new()
    {
        InvitationId = (Guid)reader["InvitationId"],
        AllianceId = new AllianceId((Guid)reader["AllianceId"]),
        InvitedPlayerId = new PlayerId((Guid)reader["InvitedPlayerId"]),
        InvitedByPlayerId = new PlayerId((Guid)reader["InvitedByPlayerId"]),
        Status = Enum.Parse<AllianceInvitationStatus>((string)reader["Status"]),
        CreatedAtUtc = AsUtc((DateTime)reader["CreatedAtUtc"]),
        RespondedAtUtc = reader["RespondedAtUtc"] is DBNull ? null : AsUtc((DateTime)reader["RespondedAtUtc"])
    };

    private static void AddAllianceParameters(IDbCommand command, AllianceEntity alliance)
    {
        Add(command, "@AllianceId", alliance.AllianceId.Value);
        Add(command, "@Name", alliance.Name);
        Add(command, "@Tag", alliance.Tag);
        Add(command, "@Description", alliance.Description);
        Add(command, "@Language", alliance.Language);
        Add(command, "@EmblemKey", alliance.EmblemKey);
        Add(command, "@JoinMode", alliance.JoinMode.ToString());
        Add(command, "@Status", alliance.Status.ToString());
        Add(command, "@CreatedAtUtc", alliance.CreatedAtUtc.UtcDateTime);
        Add(command, "@CreatedByPlayerId", alliance.CreatedByPlayerId.Value);
        Add(command, "@LeaderPlayerId", alliance.LeaderPlayerId.Value);
        Add(command, "@MemberCount", alliance.MemberCount);
        Add(command, "@MaxMembers", alliance.MaxMembers);
        Add(command, "@PublicSlug", alliance.PublicSlug);
        Add(command, "@ChatConversationId", alliance.ChatConversationId.HasValue ? alliance.ChatConversationId.Value : DBNull.Value);
        Add(command, "@Revision", alliance.Revision);
        Add(command, "@DisbandedAtUtc", alliance.DisbandedAtUtc.HasValue ? alliance.DisbandedAtUtc.Value.UtcDateTime : DBNull.Value);
    }

    private static void AddMembershipParameters(IDbCommand command, AllianceMembership membership)
    {
        Add(command, "@AllianceId", membership.AllianceId.Value);
        Add(command, "@PlayerId", membership.PlayerId.Value);
        Add(command, "@Role", membership.Role.ToString());
        Add(command, "@JoinedAtUtc", membership.JoinedAtUtc.UtcDateTime);
        Add(command, "@InvitedByPlayerId", membership.InvitedByPlayerId.HasValue ? membership.InvitedByPlayerId.Value.Value : DBNull.Value);
        Add(command, "@ApplicationId", membership.ApplicationId.HasValue ? membership.ApplicationId.Value : DBNull.Value);
        Add(command, "@LastRoleChangedAtUtc", membership.LastRoleChangedAtUtc.UtcDateTime);
        Add(command, "@RemovedAtUtc", membership.RemovedAtUtc.HasValue ? membership.RemovedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@Revision", membership.Revision);
    }

    private static void AddApplicationParameters(IDbCommand command, AllianceApplication application)
    {
        Add(command, "@ApplicationId", application.ApplicationId);
        Add(command, "@AllianceId", application.AllianceId.Value);
        Add(command, "@PlayerId", application.PlayerId.Value);
        Add(command, "@Status", application.Status.ToString());
        Add(command, "@SubmittedAtUtc", application.SubmittedAtUtc.UtcDateTime);
        Add(command, "@RespondedAtUtc", application.RespondedAtUtc.HasValue ? application.RespondedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@RespondedByPlayerId", application.RespondedByPlayerId.HasValue ? application.RespondedByPlayerId.Value.Value : DBNull.Value);
        Add(command, "@Message", application.Message);
    }

    private static void AddInvitationParameters(IDbCommand command, AllianceInvitation invitation)
    {
        Add(command, "@InvitationId", invitation.InvitationId);
        Add(command, "@AllianceId", invitation.AllianceId.Value);
        Add(command, "@InvitedPlayerId", invitation.InvitedPlayerId.Value);
        Add(command, "@InvitedByPlayerId", invitation.InvitedByPlayerId.Value);
        Add(command, "@Status", invitation.Status.ToString());
        Add(command, "@CreatedAtUtc", invitation.CreatedAtUtc.UtcDateTime);
        Add(command, "@RespondedAtUtc", invitation.RespondedAtUtc.HasValue ? invitation.RespondedAtUtc.Value.UtcDateTime : DBNull.Value);
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
