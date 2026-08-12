using System.Data;
using System.Text.Json;
using BeeKingdom.Chat.Models;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Repositories;

public sealed class SqlChatRepository : IChatRepository
{
    private static readonly JsonSerializerOptions JsonOptions = BeeJson.CreateDefaultOptions();
    private readonly SqlConnectionFactory connectionFactory;

    public SqlChatRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public ChatConversation SaveConversation(ChatConversation conversation, IReadOnlyList<ChatConversationParticipant> participants)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbTransaction transaction = connection.BeginTransaction();
        using IDbCommand conversationCommand = connection.CreateCommand();
        conversationCommand.Transaction = transaction;
        conversationCommand.CommandText = """
            MERGE dbo.ChatConversations AS target
            USING (SELECT @ConversationId AS ConversationId) AS source
            ON target.ConversationId = source.ConversationId
            WHEN MATCHED THEN
                UPDATE SET GameServerId = @GameServerId,
                           WorldId = @WorldId,
                           ChannelType = @ChannelType,
                           AudienceKey = @AudienceKey,
                           Title = @Title,
                           CreatedByPlayerId = @CreatedByPlayerId,
                           CreatedAtUtc = @CreatedAtUtc,
                           LastMessageId = @LastMessageId,
                           LastActivityAtUtc = @LastActivityAtUtc,
                           RetentionPolicy = @RetentionPolicy,
                           SchemaVersion = @SchemaVersion
            WHEN NOT MATCHED THEN
                INSERT (ConversationId, GameServerId, WorldId, ChannelType, AudienceKey, Title, CreatedByPlayerId, CreatedAtUtc,
                        LastMessageId, LastActivityAtUtc, RetentionPolicy, SchemaVersion)
                VALUES (@ConversationId, @GameServerId, @WorldId, @ChannelType, @AudienceKey, @Title, @CreatedByPlayerId, @CreatedAtUtc,
                        @LastMessageId, @LastActivityAtUtc, @RetentionPolicy, @SchemaVersion);
            """;
        AddConversationParameters(conversationCommand, conversation);
        conversationCommand.ExecuteNonQuery();

        using IDbCommand deleteParticipants = connection.CreateCommand();
        deleteParticipants.Transaction = transaction;
        deleteParticipants.CommandText = "DELETE FROM dbo.ChatConversationParticipants WHERE ConversationId = @ConversationId;";
        Add(deleteParticipants, "@ConversationId", conversation.ConversationId);
        deleteParticipants.ExecuteNonQuery();

        foreach (ChatConversationParticipant participant in participants)
        {
            using IDbCommand participantCommand = connection.CreateCommand();
            participantCommand.Transaction = transaction;
            participantCommand.CommandText = """
                INSERT INTO dbo.ChatConversationParticipants
                (ConversationId, PlayerId, Role, JoinedAtUtc, RemovedAtUtc, CanRead, CanWrite)
                VALUES
                (@ConversationId, @PlayerId, @Role, @JoinedAtUtc, @RemovedAtUtc, @CanRead, @CanWrite);
                """;
            AddParticipantParameters(participantCommand, participant);
            participantCommand.ExecuteNonQuery();
        }

        using IDbCommand sequenceCommand = connection.CreateCommand();
        sequenceCommand.Transaction = transaction;
        sequenceCommand.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM dbo.ChatConversationSequences WHERE ConversationId = @ConversationId)
            BEGIN
                INSERT INTO dbo.ChatConversationSequences (ConversationId, NextSequence)
                VALUES (@ConversationId, 1);
            END
            """;
        Add(sequenceCommand, "@ConversationId", conversation.ConversationId);
        sequenceCommand.ExecuteNonQuery();

        transaction.Commit();
        return conversation;
    }

    public ChatConversation? GetConversation(Guid conversationId)
        => QuerySingleConversation("WHERE ConversationId = @ConversationId", command => Add(command, "@ConversationId", conversationId));

    public ChatConversation? GetConversationByAudience(Guid gameServerId, Guid worldId, ChatChannelType channelType, string audienceKey)
    {
        return QuerySingleConversation(
            "WHERE GameServerId = @GameServerId AND WorldId = @WorldId AND ChannelType = @ChannelType AND AudienceKey = @AudienceKey",
            command =>
            {
                Add(command, "@GameServerId", gameServerId);
                Add(command, "@WorldId", worldId);
                Add(command, "@ChannelType", channelType.ToString());
                Add(command, "@AudienceKey", audienceKey);
            });
    }

    public IReadOnlyList<ChatConversation> ListConversations(PlayerId playerId, int offset, int limit)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectConversationColumns("c")}
            FROM dbo.ChatConversations c
            INNER JOIN dbo.ChatConversationParticipants p ON p.ConversationId = c.ConversationId
            WHERE p.PlayerId = @PlayerId
              AND p.RemovedAtUtc IS NULL
              AND p.CanRead = 1
            ORDER BY COALESCE(c.LastActivityAtUtc, c.CreatedAtUtc) DESC, c.ConversationId
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
            """;
        Add(command, "@PlayerId", playerId.Value);
        Add(command, "@Offset", Math.Max(0,offset));
        Add(command, "@Limit", Math.Clamp(limit, 1, 101));
        using IDataReader reader = command.ExecuteReader();
        return ReadConversations(reader);
    }

    public IReadOnlyList<ChatConversationParticipant> ListParticipants(Guid conversationId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectParticipantSql("WHERE ConversationId = @ConversationId");
        Add(command, "@ConversationId", conversationId);
        using IDataReader reader = command.ExecuteReader();
        return ReadParticipants(reader);
    }

    public ChatConversationParticipant? GetParticipant(Guid conversationId, PlayerId playerId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectParticipantSql("WHERE ConversationId = @ConversationId AND PlayerId = @PlayerId");
        Add(command, "@ConversationId", conversationId);
        Add(command, "@PlayerId", playerId.Value);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadParticipant(reader) : null;
    }

    public ChatConversationParticipant EnsureParticipant(ChatConversationParticipant participant)
    {
        ChatConversationParticipant? existing = GetParticipant(participant.ConversationId, participant.PlayerId);
        if (existing != null)
        {
            return existing;
        }

        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.ChatConversationParticipants AS target
            USING (SELECT @ConversationId AS ConversationId, @PlayerId AS PlayerId) AS source
            ON target.ConversationId = source.ConversationId AND target.PlayerId = source.PlayerId
            WHEN NOT MATCHED THEN
                INSERT (ConversationId, PlayerId, Role, JoinedAtUtc, RemovedAtUtc, CanRead, CanWrite)
                VALUES (@ConversationId, @PlayerId, @Role, @JoinedAtUtc, @RemovedAtUtc, @CanRead, @CanWrite);
            """;
        AddParticipantParameters(command, participant);
        command.ExecuteNonQuery();
        return GetParticipant(participant.ConversationId, participant.PlayerId) ?? participant;
    }

    public long NextSequence(Guid conversationId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        using IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM dbo.ChatConversationSequences WITH (UPDLOCK, HOLDLOCK) WHERE ConversationId = @ConversationId)
            BEGIN
                INSERT INTO dbo.ChatConversationSequences (ConversationId, NextSequence)
                VALUES (@ConversationId, 1);
            END

            DECLARE @Sequence bigint;
            SELECT @Sequence = NextSequence
            FROM dbo.ChatConversationSequences WITH (UPDLOCK, HOLDLOCK)
            WHERE ConversationId = @ConversationId;

            UPDATE dbo.ChatConversationSequences
            SET NextSequence = @Sequence + 1
            WHERE ConversationId = @ConversationId;

            SELECT @Sequence;
            """;
        Add(command, "@ConversationId", conversationId);
        long sequence = Convert.ToInt64(command.ExecuteScalar());
        transaction.Commit();
        return sequence;
    }

    public ChatOutboxReceipt? GetOutboxReceipt(PlayerId playerId, Guid conversationId, string clientRequestId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectOutboxSql("WHERE PlayerId = @PlayerId AND ConversationId = @ConversationId AND ClientRequestId = @ClientRequestId");
        Add(command, "@PlayerId", playerId.Value);
        Add(command, "@ConversationId", conversationId);
        Add(command, "@ClientRequestId", clientRequestId);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadOutbox(reader) : null;
    }

    public ChatOutboxReceipt SaveOutboxReceipt(ChatOutboxReceipt receipt)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.ChatOutboxReceipts AS target
            USING (SELECT @PlayerId AS PlayerId, @ConversationId AS ConversationId, @ClientRequestId AS ClientRequestId) AS source
            ON target.PlayerId = source.PlayerId
               AND target.ConversationId = source.ConversationId
               AND target.ClientRequestId = source.ClientRequestId
            WHEN MATCHED THEN
                UPDATE SET PayloadHash = @PayloadHash,
                           MessageId = @MessageId,
                           AcceptedAtUtc = @AcceptedAtUtc,
                           LastErrorCode = @LastErrorCode
            WHEN NOT MATCHED THEN
                INSERT (PlayerId, ConversationId, ClientRequestId, PayloadHash, MessageId, AcceptedAtUtc, LastErrorCode)
                VALUES (@PlayerId, @ConversationId, @ClientRequestId, @PayloadHash, @MessageId, @AcceptedAtUtc, @LastErrorCode);
            """;
        AddOutboxParameters(command, receipt);
        command.ExecuteNonQuery();
        return receipt;
    }
    public ChatConversationCreationReceipt? GetConversationCreationReceipt(PlayerId playerId,string clientRequestId)
    {using IDbConnection c=connectionFactory.CreateConnection();c.Open();using IDbCommand cmd=c.CreateCommand();cmd.CommandText="SELECT PayloadHash,ConversationId,CreatedAtUtc FROM dbo.ChatConversationCreationReceipts WHERE PlayerId=@PlayerId AND ClientRequestId=@ClientRequestId";Add(cmd,"@PlayerId",playerId.Value);Add(cmd,"@ClientRequestId",clientRequestId);using IDataReader r=cmd.ExecuteReader();return r.Read()?new(playerId,clientRequestId,r.GetString(0),r.GetGuid(1),AsUtc(r.GetDateTime(2))):null;}
    public ChatConversationCreationReceipt SaveConversationCreationReceipt(ChatConversationCreationReceipt receipt)
    {using IDbConnection c=connectionFactory.CreateConnection();c.Open();using IDbCommand cmd=c.CreateCommand();cmd.CommandText="INSERT INTO dbo.ChatConversationCreationReceipts(PlayerId,ClientRequestId,PayloadHash,ConversationId,CreatedAtUtc) VALUES(@PlayerId,@ClientRequestId,@PayloadHash,@ConversationId,@CreatedAtUtc)";Add(cmd,"@PlayerId",receipt.PlayerId.Value);Add(cmd,"@ClientRequestId",receipt.ClientRequestId);Add(cmd,"@PayloadHash",receipt.PayloadHash);Add(cmd,"@ConversationId",receipt.ConversationId);Add(cmd,"@CreatedAtUtc",receipt.CreatedAtUtc.UtcDateTime);cmd.ExecuteNonQuery();return receipt;}

    public ChatMessage SaveMessage(ChatMessage message)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbTransaction transaction = connection.BeginTransaction();
        using IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            MERGE dbo.ChatMessages AS target
            USING (SELECT @MessageId AS MessageId) AS source
            ON target.MessageId = source.MessageId
            WHEN MATCHED THEN
                UPDATE SET ConversationId = @ConversationId,
                           GameServerId = @GameServerId,
                           WorldId = @WorldId,
                           ChannelType = @ChannelType,
                           SenderPlayerId = @SenderPlayerId,
                           SenderDisplayNameSnapshot = @SenderDisplayNameSnapshot,
                           Body = @Body,
                           ContentPartsJson = @ContentPartsJson,
                           MentionsJson = @MentionsJson,
                           EmojiJson = @EmojiJson,
                           ReplyToMessageId = @ReplyToMessageId,
                           ClientCreatedAtUtc = @ClientCreatedAtUtc,
                           AcceptedAtUtc = @AcceptedAtUtc,
                           Sequence = @Sequence,
                           ClientRequestId = @ClientRequestId,
                           State = @State,
                           ModerationStatus = @ModerationStatus,
                           ModerationReasonCode = @ModerationReasonCode,
                           EditedAtUtc = @EditedAtUtc,
                           DeletedAtUtc = @DeletedAtUtc,
                           SchemaVersion = @SchemaVersion
            WHEN NOT MATCHED THEN
                INSERT (MessageId, ConversationId, GameServerId, WorldId, ChannelType, SenderPlayerId, SenderDisplayNameSnapshot,
                        Body, ContentPartsJson, MentionsJson, EmojiJson, ReplyToMessageId, ClientCreatedAtUtc, AcceptedAtUtc,
                        Sequence, ClientRequestId, State, ModerationStatus, ModerationReasonCode, EditedAtUtc, DeletedAtUtc, SchemaVersion)
                VALUES (@MessageId, @ConversationId, @GameServerId, @WorldId, @ChannelType, @SenderPlayerId, @SenderDisplayNameSnapshot,
                        @Body, @ContentPartsJson, @MentionsJson, @EmojiJson, @ReplyToMessageId, @ClientCreatedAtUtc, @AcceptedAtUtc,
                        @Sequence, @ClientRequestId, @State, @ModerationStatus, @ModerationReasonCode, @EditedAtUtc, @DeletedAtUtc, @SchemaVersion);
            """;
        AddMessageParameters(command, message);
        command.ExecuteNonQuery();

        using IDbCommand updateConversation = connection.CreateCommand();
        updateConversation.Transaction = transaction;
        updateConversation.CommandText = """
            UPDATE dbo.ChatConversations
            SET LastMessageId = @MessageId,
                LastActivityAtUtc = @AcceptedAtUtc
            WHERE ConversationId = @ConversationId;
            """;
        Add(updateConversation, "@MessageId", message.MessageId);
        Add(updateConversation, "@AcceptedAtUtc", message.AcceptedAtUtc.UtcDateTime);
        Add(updateConversation, "@ConversationId", message.ConversationId);
        updateConversation.ExecuteNonQuery();
        transaction.Commit();
        return message;
    }

    public ChatMessage? GetMessage(Guid messageId)
        => QuerySingleMessage("WHERE MessageId = @MessageId", command => Add(command, "@MessageId", messageId));

    public IReadOnlyList<ChatMessage> ListMessages(Guid conversationId, long afterSequence, int limit)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectMessageColumns()}
            FROM dbo.ChatMessages
            WHERE ConversationId = @ConversationId
              AND Sequence > @AfterSequence
            ORDER BY Sequence ASC
            OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY;
            """;
        Add(command, "@ConversationId", conversationId);
        Add(command, "@AfterSequence", afterSequence);
        Add(command, "@Limit", Math.Clamp(limit, 1, 100));
        using IDataReader reader = command.ExecuteReader();
        return ReadMessages(reader);
    }

    public long GetLastSequence(Guid conversationId)
    {
        using IDbConnection connection=connectionFactory.CreateConnection(); connection.Open(); using IDbCommand command=connection.CreateCommand();
        command.CommandText="SELECT COALESCE(MAX(Sequence),0) FROM dbo.ChatMessages WHERE ConversationId=@ConversationId"; Add(command,"@ConversationId",conversationId); return Convert.ToInt64(command.ExecuteScalar());
    }

    public ChatInboxEntry SaveInbox(ChatInboxEntry entry)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.ChatInbox AS target
            USING (SELECT @PlayerId AS PlayerId, @ConversationId AS ConversationId) AS source
            ON target.PlayerId = source.PlayerId
               AND target.ConversationId = source.ConversationId
            WHEN MATCHED THEN
                UPDATE SET LastMessageId = @LastMessageId,
                           LastActivityAtUtc = @LastActivityAtUtc,
                           ReadCursorSequence = CASE WHEN target.ReadCursorSequence > @ReadCursorSequence THEN target.ReadCursorSequence ELSE @ReadCursorSequence END,
                           UnreadCount = CASE WHEN target.ReadCursorSequence > @ReadCursorSequence AND target.UnreadCount < @UnreadCount THEN target.UnreadCount ELSE @UnreadCount END,
                           MentionCount = CASE WHEN target.ReadCursorSequence > @ReadCursorSequence AND target.MentionCount < @MentionCount THEN target.MentionCount ELSE @MentionCount END,
                           IsMuted = @IsMuted,
                           IsArchived = @IsArchived,
                           UpdatedAtUtc = @UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT (PlayerId, ConversationId, LastMessageId, LastActivityAtUtc, ReadCursorSequence,
                        UnreadCount, MentionCount, IsMuted, IsArchived, UpdatedAtUtc)
                VALUES (@PlayerId, @ConversationId, @LastMessageId, @LastActivityAtUtc, @ReadCursorSequence,
                        @UnreadCount, @MentionCount, @IsMuted, @IsArchived, @UpdatedAtUtc);
            """;
        AddInboxParameters(command, entry);
        command.ExecuteNonQuery();
        return GetInbox(entry.PlayerId,entry.ConversationId) ?? entry;
    }

    public ChatInboxEntry? GetInbox(PlayerId playerId, Guid conversationId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectInboxSql("WHERE PlayerId = @PlayerId AND ConversationId = @ConversationId");
        Add(command, "@PlayerId", playerId.Value);
        Add(command, "@ConversationId", conversationId);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadInbox(reader) : null;
    }

    public IReadOnlyList<ChatInboxEntry> ListInboxEntries(Guid conversationId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectInboxSql("WHERE ConversationId = @ConversationId");
        Add(command, "@ConversationId", conversationId);
        using IDataReader reader = command.ExecuteReader();
        return ReadInboxEntries(reader);
    }

    public ChatModerationReport SaveModerationReport(ChatModerationReport report)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.ChatModerationReports
            (ReportId, MessageId, ReporterPlayerId, Category, CreatedAtUtc, Status)
            VALUES
            (@ReportId, @MessageId, @ReporterPlayerId, @Category, @CreatedAtUtc, @Status);
            """;
        Add(command, "@ReportId", report.ReportId);
        Add(command, "@MessageId", report.MessageId);
        Add(command, "@ReporterPlayerId", report.ReporterPlayerId.Value);
        Add(command, "@Category", report.Category);
        Add(command, "@CreatedAtUtc", report.CreatedAtUtc.UtcDateTime);
        Add(command, "@Status", report.Status);
        command.ExecuteNonQuery();
        return report;
    }

    public ChatModerationReport? GetModerationReport(Guid reportId)
    { using IDbConnection c=connectionFactory.CreateConnection();c.Open();using IDbCommand cmd=c.CreateCommand();cmd.CommandText="SELECT MessageId,ReporterPlayerId,Category,CreatedAtUtc,Status FROM dbo.ChatModerationReports WHERE ReportId=@ReportId";Add(cmd,"@ReportId",reportId);using IDataReader r=cmd.ExecuteReader();return r.Read()?new(reportId,r.GetGuid(0),new PlayerId(r.GetGuid(1)),r.GetString(2),AsUtc(r.GetDateTime(3)),r.GetString(4)):null; }
    public ChatModerationReportReceipt? GetModerationReportReceipt(PlayerId reporterPlayerId,string clientRequestId)
    { using IDbConnection c=connectionFactory.CreateConnection();c.Open();using IDbCommand cmd=c.CreateCommand();cmd.CommandText="SELECT PayloadHash,ReportId,CreatedAtUtc FROM dbo.ChatModerationReportReceipts WHERE ReporterPlayerId=@ReporterPlayerId AND ClientRequestId=@ClientRequestId";Add(cmd,"@ReporterPlayerId",reporterPlayerId.Value);Add(cmd,"@ClientRequestId",clientRequestId);using IDataReader r=cmd.ExecuteReader();return r.Read()?new(reporterPlayerId,clientRequestId,r.GetString(0),r.GetGuid(1),AsUtc(r.GetDateTime(2))):null; }
    public ChatModerationReportReceipt SaveModerationReportReceipt(ChatModerationReportReceipt receipt)
    { using IDbConnection c=connectionFactory.CreateConnection();c.Open();using IDbCommand cmd=c.CreateCommand();cmd.CommandText="INSERT dbo.ChatModerationReportReceipts(ReporterPlayerId,ClientRequestId,PayloadHash,ReportId,CreatedAtUtc) VALUES(@ReporterPlayerId,@ClientRequestId,@PayloadHash,@ReportId,@CreatedAtUtc)";Add(cmd,"@ReporterPlayerId",receipt.ReporterPlayerId.Value);Add(cmd,"@ClientRequestId",receipt.ClientRequestId);Add(cmd,"@PayloadHash",receipt.PayloadHash);Add(cmd,"@ReportId",receipt.ReportId);Add(cmd,"@CreatedAtUtc",receipt.CreatedAtUtc.UtcDateTime);cmd.ExecuteNonQuery();return receipt; }
    public ChatModerationReport SaveModerationReportIdempotent(ChatModerationReport report,ChatModerationReportReceipt receipt)
    {
        using IDbConnection c=connectionFactory.CreateConnection();c.Open();using IDbTransaction tx=c.BeginTransaction(IsolationLevel.Serializable);
        using(IDbCommand find=c.CreateCommand()){find.Transaction=tx;find.CommandText="SELECT PayloadHash,ReportId FROM dbo.ChatModerationReportReceipts WITH(UPDLOCK,HOLDLOCK) WHERE ReporterPlayerId=@ReporterPlayerId AND ClientRequestId=@ClientRequestId";Add(find,"@ReporterPlayerId",receipt.ReporterPlayerId.Value);Add(find,"@ClientRequestId",receipt.ClientRequestId);using IDataReader r=find.ExecuteReader();if(r.Read()){string hash=r.GetString(0);Guid reportId=r.GetGuid(1);r.Close();if(!string.Equals(hash,receipt.PayloadHash,StringComparison.Ordinal))throw new InvalidOperationException("idempotency_conflict");using IDbCommand existing=c.CreateCommand();existing.Transaction=tx;existing.CommandText="SELECT MessageId,ReporterPlayerId,Category,CreatedAtUtc,Status FROM dbo.ChatModerationReports WHERE ReportId=@ReportId";Add(existing,"@ReportId",reportId);using IDataReader er=existing.ExecuteReader();if(!er.Read())throw new InvalidOperationException("idempotency_record_missing_report");ChatModerationReport value=new(reportId,er.GetGuid(0),new PlayerId(er.GetGuid(1)),er.GetString(2),AsUtc(er.GetDateTime(3)),er.GetString(4));er.Close();tx.Commit();return value;}}
        using(IDbCommand insertReport=c.CreateCommand()){insertReport.Transaction=tx;insertReport.CommandText="INSERT dbo.ChatModerationReports(ReportId,MessageId,ReporterPlayerId,Category,CreatedAtUtc,Status) VALUES(@ReportId,@MessageId,@ReporterPlayerId,@Category,@CreatedAtUtc,@Status)";Add(insertReport,"@ReportId",report.ReportId);Add(insertReport,"@MessageId",report.MessageId);Add(insertReport,"@ReporterPlayerId",report.ReporterPlayerId.Value);Add(insertReport,"@Category",report.Category);Add(insertReport,"@CreatedAtUtc",report.CreatedAtUtc.UtcDateTime);Add(insertReport,"@Status",report.Status);insertReport.ExecuteNonQuery();}
        using(IDbCommand insertReceipt=c.CreateCommand()){insertReceipt.Transaction=tx;insertReceipt.CommandText="INSERT dbo.ChatModerationReportReceipts(ReporterPlayerId,ClientRequestId,PayloadHash,ReportId,CreatedAtUtc) VALUES(@ReporterPlayerId,@ClientRequestId,@PayloadHash,@ReportId,@CreatedAtUtc)";Add(insertReceipt,"@ReporterPlayerId",receipt.ReporterPlayerId.Value);Add(insertReceipt,"@ClientRequestId",receipt.ClientRequestId);Add(insertReceipt,"@PayloadHash",receipt.PayloadHash);Add(insertReceipt,"@ReportId",receipt.ReportId);Add(insertReceipt,"@CreatedAtUtc",receipt.CreatedAtUtc.UtcDateTime);insertReceipt.ExecuteNonQuery();}tx.Commit();return report;
    }
    public int PurgeExpiredReceipts(DateTimeOffset cutoffUtc)
    {using IDbConnection c=connectionFactory.CreateConnection();c.Open();using IDbTransaction tx=c.BeginTransaction();int removed=0;foreach(string sql in new[]{"DELETE FROM dbo.ChatOutboxReceipts WHERE AcceptedAtUtc IS NOT NULL AND AcceptedAtUtc < @CutoffUtc","DELETE FROM dbo.ChatConversationCreationReceipts WHERE CreatedAtUtc < @CutoffUtc","DELETE FROM dbo.ChatModerationReportReceipts WHERE CreatedAtUtc < @CutoffUtc"}){using IDbCommand cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;Add(cmd,"@CutoffUtc",cutoffUtc.UtcDateTime);removed+=cmd.ExecuteNonQuery();}tx.Commit();return removed;}

    private ChatConversation? QuerySingleConversation(string whereClause, Action<IDbCommand> configure)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"{SelectConversationColumns()}\nFROM dbo.ChatConversations\n{whereClause};";
        configure(command);
        using IDataReader reader = command.ExecuteReader();
        return ReadConversations(reader).FirstOrDefault();
    }

    private ChatMessage? QuerySingleMessage(string whereClause, Action<IDbCommand> configure)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = $"{SelectMessageColumns()}\nFROM dbo.ChatMessages\n{whereClause};";
        configure(command);
        using IDataReader reader = command.ExecuteReader();
        return ReadMessages(reader).FirstOrDefault();
    }

    private static string SelectConversationColumns(string alias = "")
    {
        string prefix = string.IsNullOrWhiteSpace(alias) ? string.Empty : alias + ".";
        return $"""
            SELECT {prefix}ConversationId, {prefix}GameServerId, {prefix}WorldId, {prefix}ChannelType, {prefix}AudienceKey,
                   {prefix}Title, {prefix}CreatedByPlayerId, {prefix}CreatedAtUtc, {prefix}LastMessageId,
                   {prefix}LastActivityAtUtc, {prefix}RetentionPolicy, {prefix}SchemaVersion
            """;
    }

    private static IReadOnlyList<ChatConversation> ReadConversations(IDataReader reader)
    {
        List<ChatConversation> conversations = [];
        while (reader.Read())
        {
            conversations.Add(new ChatConversation(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                Enum.Parse<ChatChannelType>(reader.GetString(3), ignoreCase: true),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : new PlayerId(reader.GetGuid(6)),
                AsUtc(reader.GetDateTime(7)),
                reader.IsDBNull(8) ? null : reader.GetGuid(8),
                reader.IsDBNull(9) ? null : AsUtc(reader.GetDateTime(9)),
                reader.GetString(10),
                reader.GetInt32(11)));
        }

        return conversations;
    }

    private static string SelectParticipantSql(string whereClause)
    {
        return $"""
            SELECT ConversationId, PlayerId, Role, JoinedAtUtc, RemovedAtUtc, CanRead, CanWrite
            FROM dbo.ChatConversationParticipants
            {whereClause};
            """;
    }

    private static IReadOnlyList<ChatConversationParticipant> ReadParticipants(IDataReader reader)
    {
        List<ChatConversationParticipant> participants = [];
        while (reader.Read())
        {
            participants.Add(ReadParticipant(reader));
        }

        return participants;
    }

    private static ChatConversationParticipant ReadParticipant(IDataReader reader)
    {
        return new ChatConversationParticipant(
            reader.GetGuid(0),
            new PlayerId(reader.GetGuid(1)),
            Enum.Parse<ChatPermissionRole>(reader.GetString(2), ignoreCase: true),
            AsUtc(reader.GetDateTime(3)),
            reader.IsDBNull(4) ? null : AsUtc(reader.GetDateTime(4)),
            reader.GetBoolean(5),
            reader.GetBoolean(6));
    }

    private static string SelectMessageColumns()
    {
        return """
            SELECT MessageId, ConversationId, GameServerId, WorldId, ChannelType, SenderPlayerId, SenderDisplayNameSnapshot,
                   Body, ContentPartsJson, MentionsJson, EmojiJson, ReplyToMessageId, ClientCreatedAtUtc, AcceptedAtUtc,
                   Sequence, ClientRequestId, State, ModerationStatus, ModerationReasonCode, EditedAtUtc, DeletedAtUtc, SchemaVersion
            """;
    }

    private static IReadOnlyList<ChatMessage> ReadMessages(IDataReader reader)
    {
        List<ChatMessage> messages = [];
        while (reader.Read())
        {
            messages.Add(new ChatMessage(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                Enum.Parse<ChatChannelType>(reader.GetString(4), ignoreCase: true),
                new PlayerId(reader.GetGuid(5)),
                reader.GetString(6),
                reader.GetString(7),
                Deserialize<IReadOnlyList<ChatContentPart>>(reader.GetString(8)),
                Deserialize<IReadOnlyList<ChatMention>>(reader.GetString(9)),
                Deserialize<IReadOnlyList<ChatEmoji>>(reader.GetString(10)),
                reader.IsDBNull(11) ? null : reader.GetGuid(11),
                AsUtc(reader.GetDateTime(12)),
                AsUtc(reader.GetDateTime(13)),
                reader.GetInt64(14),
                reader.GetString(15),
                Enum.Parse<ChatMessageState>(reader.GetString(16), ignoreCase: true),
                Enum.Parse<ChatModerationStatus>(reader.GetString(17), ignoreCase: true),
                reader.IsDBNull(18) ? null : reader.GetString(18),
                reader.IsDBNull(19) ? null : AsUtc(reader.GetDateTime(19)),
                reader.IsDBNull(20) ? null : AsUtc(reader.GetDateTime(20)),
                reader.GetInt32(21)));
        }

        return messages;
    }

    private static string SelectInboxSql(string whereClause)
    {
        return $"""
            SELECT PlayerId, ConversationId, LastMessageId, LastActivityAtUtc, ReadCursorSequence, UnreadCount,
                   MentionCount, IsMuted, IsArchived, UpdatedAtUtc
            FROM dbo.ChatInbox
            {whereClause};
            """;
    }

    private static IReadOnlyList<ChatInboxEntry> ReadInboxEntries(IDataReader reader)
    {
        List<ChatInboxEntry> entries = [];
        while (reader.Read())
        {
            entries.Add(ReadInbox(reader));
        }

        return entries;
    }

    private static ChatInboxEntry ReadInbox(IDataReader reader)
    {
        return new ChatInboxEntry(
            new PlayerId(reader.GetGuid(0)),
            reader.GetGuid(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.IsDBNull(3) ? null : AsUtc(reader.GetDateTime(3)),
            reader.GetInt64(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetBoolean(7),
            reader.GetBoolean(8),
            AsUtc(reader.GetDateTime(9)));
    }

    private static string SelectOutboxSql(string whereClause)
    {
        return $"""
            SELECT PlayerId, ConversationId, ClientRequestId, PayloadHash, MessageId, AcceptedAtUtc, LastErrorCode
            FROM dbo.ChatOutboxReceipts
            {whereClause};
            """;
    }

    private static ChatOutboxReceipt ReadOutbox(IDataReader reader)
    {
        return new ChatOutboxReceipt(
            new PlayerId(reader.GetGuid(0)),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetGuid(4),
            reader.IsDBNull(5) ? null : AsUtc(reader.GetDateTime(5)),
            reader.IsDBNull(6) ? null : reader.GetString(6));
    }

    private static void AddConversationParameters(IDbCommand command, ChatConversation conversation)
    {
        Add(command, "@ConversationId", conversation.ConversationId);
        Add(command, "@GameServerId", conversation.GameServerId);
        Add(command, "@WorldId", conversation.WorldId);
        Add(command, "@ChannelType", conversation.ChannelType.ToString());
        Add(command, "@AudienceKey", conversation.AudienceKey);
        Add(command, "@Title", string.IsNullOrWhiteSpace(conversation.Title) ? DBNull.Value : conversation.Title);
        Add(command, "@CreatedByPlayerId", conversation.CreatedByPlayerId.HasValue ? conversation.CreatedByPlayerId.Value.Value : DBNull.Value);
        Add(command, "@CreatedAtUtc", conversation.CreatedAtUtc.UtcDateTime);
        Add(command, "@LastMessageId", conversation.LastMessageId.HasValue ? conversation.LastMessageId.Value : DBNull.Value);
        Add(command, "@LastActivityAtUtc", conversation.LastActivityAtUtc.HasValue ? conversation.LastActivityAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@RetentionPolicy", conversation.RetentionPolicy);
        Add(command, "@SchemaVersion", conversation.SchemaVersion);
    }

    private static void AddParticipantParameters(IDbCommand command, ChatConversationParticipant participant)
    {
        Add(command, "@ConversationId", participant.ConversationId);
        Add(command, "@PlayerId", participant.PlayerId.Value);
        Add(command, "@Role", participant.Role.ToString());
        Add(command, "@JoinedAtUtc", participant.JoinedAtUtc.UtcDateTime);
        Add(command, "@RemovedAtUtc", participant.RemovedAtUtc.HasValue ? participant.RemovedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@CanRead", participant.CanRead);
        Add(command, "@CanWrite", participant.CanWrite);
    }

    private static void AddMessageParameters(IDbCommand command, ChatMessage message)
    {
        Add(command, "@MessageId", message.MessageId);
        Add(command, "@ConversationId", message.ConversationId);
        Add(command, "@GameServerId", message.GameServerId);
        Add(command, "@WorldId", message.WorldId);
        Add(command, "@ChannelType", message.ChannelType.ToString());
        Add(command, "@SenderPlayerId", message.SenderPlayerId.Value);
        Add(command, "@SenderDisplayNameSnapshot", message.SenderDisplayNameSnapshot);
        Add(command, "@Body", message.Body);
        Add(command, "@ContentPartsJson", JsonSerializer.Serialize(message.ContentParts, JsonOptions));
        Add(command, "@MentionsJson", JsonSerializer.Serialize(message.Mentions, JsonOptions));
        Add(command, "@EmojiJson", JsonSerializer.Serialize(message.Emoji, JsonOptions));
        Add(command, "@ReplyToMessageId", message.ReplyToMessageId.HasValue ? message.ReplyToMessageId.Value : DBNull.Value);
        Add(command, "@ClientCreatedAtUtc", message.ClientCreatedAtUtc.UtcDateTime);
        Add(command, "@AcceptedAtUtc", message.AcceptedAtUtc.UtcDateTime);
        Add(command, "@Sequence", message.Sequence);
        Add(command, "@ClientRequestId", message.ClientRequestId);
        Add(command, "@State", message.State.ToString());
        Add(command, "@ModerationStatus", message.ModerationStatus.ToString());
        Add(command, "@ModerationReasonCode", string.IsNullOrWhiteSpace(message.ModerationReasonCode) ? DBNull.Value : message.ModerationReasonCode);
        Add(command, "@EditedAtUtc", message.EditedAtUtc.HasValue ? message.EditedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@DeletedAtUtc", message.DeletedAtUtc.HasValue ? message.DeletedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@SchemaVersion", message.SchemaVersion);
    }

    private static void AddInboxParameters(IDbCommand command, ChatInboxEntry entry)
    {
        Add(command, "@PlayerId", entry.PlayerId.Value);
        Add(command, "@ConversationId", entry.ConversationId);
        Add(command, "@LastMessageId", entry.LastMessageId.HasValue ? entry.LastMessageId.Value : DBNull.Value);
        Add(command, "@LastActivityAtUtc", entry.LastActivityAtUtc.HasValue ? entry.LastActivityAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@ReadCursorSequence", entry.ReadCursorSequence);
        Add(command, "@UnreadCount", entry.UnreadCount);
        Add(command, "@MentionCount", entry.MentionCount);
        Add(command, "@IsMuted", entry.IsMuted);
        Add(command, "@IsArchived", entry.IsArchived);
        Add(command, "@UpdatedAtUtc", entry.UpdatedAtUtc.UtcDateTime);
    }

    private static void AddOutboxParameters(IDbCommand command, ChatOutboxReceipt receipt)
    {
        Add(command, "@PlayerId", receipt.PlayerId.Value);
        Add(command, "@ConversationId", receipt.ConversationId);
        Add(command, "@ClientRequestId", receipt.ClientRequestId);
        Add(command, "@PayloadHash", receipt.PayloadHash);
        Add(command, "@MessageId", receipt.MessageId.HasValue ? receipt.MessageId.Value : DBNull.Value);
        Add(command, "@AcceptedAtUtc", receipt.AcceptedAtUtc.HasValue ? receipt.AcceptedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@LastErrorCode", string.IsNullOrWhiteSpace(receipt.LastErrorCode) ? DBNull.Value : receipt.LastErrorCode);
    }

    private static T Deserialize<T>(string payload)
    {
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidDataException("Invalid chat JSON payload.");
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
