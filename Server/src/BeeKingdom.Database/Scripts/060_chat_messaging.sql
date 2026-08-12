IF OBJECT_ID(N'dbo.ChatConversations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatConversations
    (
        ConversationId uniqueidentifier NOT NULL CONSTRAINT PK_ChatConversations PRIMARY KEY,
        GameServerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        ChannelType nvarchar(32) NOT NULL,
        AudienceKey nvarchar(256) NOT NULL,
        Title nvarchar(160) NULL,
        CreatedByPlayerId uniqueidentifier NULL,
        CreatedAtUtc datetime2 NOT NULL,
        LastMessageId uniqueidentifier NULL,
        LastActivityAtUtc datetime2 NULL,
        RetentionPolicy nvarchar(64) NOT NULL,
        SchemaVersion int NOT NULL
    );

    CREATE UNIQUE INDEX UX_ChatConversations_Audience ON dbo.ChatConversations(GameServerId, WorldId, ChannelType, AudienceKey);
    CREATE INDEX IX_ChatConversations_Activity ON dbo.ChatConversations(GameServerId, WorldId, LastActivityAtUtc DESC);
END

IF OBJECT_ID(N'dbo.ChatConversationParticipants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatConversationParticipants
    (
        ConversationId uniqueidentifier NOT NULL,
        PlayerId uniqueidentifier NOT NULL,
        Role nvarchar(32) NOT NULL,
        JoinedAtUtc datetime2 NOT NULL,
        RemovedAtUtc datetime2 NULL,
        CanRead bit NOT NULL,
        CanWrite bit NOT NULL,
        CONSTRAINT PK_ChatConversationParticipants PRIMARY KEY (ConversationId, PlayerId)
    );

    CREATE INDEX IX_ChatConversationParticipants_PlayerId ON dbo.ChatConversationParticipants(PlayerId, RemovedAtUtc);
END

IF OBJECT_ID(N'dbo.ChatConversationSequences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatConversationSequences
    (
        ConversationId uniqueidentifier NOT NULL CONSTRAINT PK_ChatConversationSequences PRIMARY KEY,
        NextSequence bigint NOT NULL
    );
END

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        MessageId uniqueidentifier NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        ConversationId uniqueidentifier NOT NULL,
        GameServerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        ChannelType nvarchar(32) NOT NULL,
        SenderPlayerId uniqueidentifier NOT NULL,
        SenderDisplayNameSnapshot nvarchar(128) NOT NULL,
        Body nvarchar(1000) NOT NULL,
        ContentPartsJson nvarchar(max) NOT NULL,
        MentionsJson nvarchar(max) NOT NULL,
        EmojiJson nvarchar(max) NOT NULL,
        ReplyToMessageId uniqueidentifier NULL,
        ClientCreatedAtUtc datetime2 NOT NULL,
        AcceptedAtUtc datetime2 NOT NULL,
        Sequence bigint NOT NULL,
        ClientRequestId nvarchar(128) NOT NULL,
        State nvarchar(32) NOT NULL,
        ModerationStatus nvarchar(32) NOT NULL,
        ModerationReasonCode nvarchar(64) NULL,
        EditedAtUtc datetime2 NULL,
        DeletedAtUtc datetime2 NULL,
        SchemaVersion int NOT NULL
    );

    CREATE UNIQUE INDEX UX_ChatMessages_ConversationSequence ON dbo.ChatMessages(ConversationId, Sequence);
    CREATE UNIQUE INDEX UX_ChatMessages_ClientRequest ON dbo.ChatMessages(SenderPlayerId, ConversationId, ClientRequestId);
    CREATE INDEX IX_ChatMessages_ConversationSequenceDesc ON dbo.ChatMessages(ConversationId, Sequence DESC);
    CREATE INDEX IX_ChatMessages_ChannelAccepted ON dbo.ChatMessages(GameServerId, WorldId, ChannelType, AcceptedAtUtc DESC);
END

IF OBJECT_ID(N'dbo.ChatInbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatInbox
    (
        PlayerId uniqueidentifier NOT NULL,
        ConversationId uniqueidentifier NOT NULL,
        LastMessageId uniqueidentifier NULL,
        LastActivityAtUtc datetime2 NULL,
        ReadCursorSequence bigint NOT NULL,
        UnreadCount int NOT NULL,
        MentionCount int NOT NULL,
        IsMuted bit NOT NULL,
        IsArchived bit NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_ChatInbox PRIMARY KEY (PlayerId, ConversationId)
    );

    CREATE INDEX IX_ChatInbox_PlayerActivity ON dbo.ChatInbox(PlayerId, IsArchived, LastActivityAtUtc DESC);
END

IF OBJECT_ID(N'dbo.ChatOutboxReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatOutboxReceipts
    (
        PlayerId uniqueidentifier NOT NULL,
        ConversationId uniqueidentifier NOT NULL,
        ClientRequestId nvarchar(128) NOT NULL,
        PayloadHash nvarchar(128) NOT NULL,
        MessageId uniqueidentifier NULL,
        AcceptedAtUtc datetime2 NULL,
        LastErrorCode nvarchar(64) NULL,
        CONSTRAINT PK_ChatOutboxReceipts PRIMARY KEY (PlayerId, ConversationId, ClientRequestId)
    );
END

IF OBJECT_ID(N'dbo.ChatModerationReports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatModerationReports
    (
        ReportId uniqueidentifier NOT NULL CONSTRAINT PK_ChatModerationReports PRIMARY KEY,
        MessageId uniqueidentifier NOT NULL,
        ReporterPlayerId uniqueidentifier NOT NULL,
        Category nvarchar(64) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        Status nvarchar(32) NOT NULL
    );

    CREATE INDEX IX_ChatModerationReports_MessageId ON dbo.ChatModerationReports(MessageId);
    CREATE INDEX IX_ChatModerationReports_Reporter ON dbo.ChatModerationReports(ReporterPlayerId, CreatedAtUtc DESC);
END
