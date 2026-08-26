namespace BeeKingdom.Database;

public static class DatabaseCatalog
{
    public static IReadOnlyList<DatabaseScript> BootstrapScripts { get; } =
    [
        new DatabaseScript(
            "001_create_database.sql",
            """
            IF DB_ID(N'BeeKingdom') IS NULL
            BEGIN
                CREATE DATABASE [BeeKingdom];
            END
            """)
    ];

    public static IReadOnlyList<DatabaseScript> Migrations { get; } =
    [
        new DatabaseScript(
            "010_schema_version.sql",
            """
            IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.SchemaVersion
                (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
                    ScriptName nvarchar(256) NOT NULL CONSTRAINT UQ_SchemaVersion_ScriptName UNIQUE,
                    AppliedAtUtc datetime2 NOT NULL CONSTRAINT DF_SchemaVersion_AppliedAtUtc DEFAULT SYSUTCDATETIME()
                );
            END
            """),
        new DatabaseScript(
            "011_schema_version_uniqueness.sql",
            """
            IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NOT NULL
               AND NOT EXISTS
               (
                   SELECT 1
                   FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'dbo.SchemaVersion')
                     AND is_unique = 1
                     AND name IN (N'UQ_SchemaVersion_ScriptName', N'UX_SchemaVersion_ScriptName')
               )
            BEGIN
                IF EXISTS
                (
                    SELECT ScriptName
                    FROM dbo.SchemaVersion
                    GROUP BY ScriptName
                    HAVING COUNT_BIG(*) > 1
                )
                BEGIN
                    THROW 51057, 'Duplicate SchemaVersion rows must be reconciled before uniqueness can be enforced.', 1;
                END;

                CREATE UNIQUE INDEX UX_SchemaVersion_ScriptName
                    ON dbo.SchemaVersion(ScriptName);
            END
            """),
        new DatabaseScript(
            "020_accounts.sql",
            """
            IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Accounts
                (
                    AccountId uniqueidentifier NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
                    PlayerId uniqueidentifier NOT NULL,
                    DisplayName nvarchar(128) NOT NULL,
                    Email nvarchar(320) NOT NULL,
                    Language nvarchar(16) NOT NULL,
                    TimeZone nvarchar(64) NOT NULL,
                    Country nvarchar(8) NOT NULL,
                    Currency nvarchar(8) NOT NULL,
                    Status int NOT NULL,
                    AnalyticsEnabled bit NOT NULL,
                    CrossPlayEnabled bit NOT NULL,
                    PreferencesJson nvarchar(max) NOT NULL,
                    ProgressionJson nvarchar(max) NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    LastLoginUtc datetime2 NULL,
                    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_Accounts_UpdatedAtUtc DEFAULT SYSUTCDATETIME()
                );

                CREATE UNIQUE INDEX UX_Accounts_PlayerId ON dbo.Accounts(PlayerId);
                CREATE UNIQUE INDEX UX_Accounts_Email ON dbo.Accounts(Email);
                CREATE INDEX IX_Accounts_Status ON dbo.Accounts(Status);
            END
            """),
        new DatabaseScript(
            "030_authentication_sessions.sql",
            """
            IF OBJECT_ID(N'dbo.AuthenticationAccounts', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AuthenticationAccounts
                (
                    AccountId uniqueidentifier NOT NULL CONSTRAINT PK_AuthenticationAccounts PRIMARY KEY,
                    PlayerId uniqueidentifier NOT NULL,
                    Email nvarchar(320) NOT NULL,
                    PasswordHash nvarchar(512) NOT NULL,
                    SecurityState int NOT NULL,
                    FailedAttempts int NOT NULL,
                    LockedUntilUtc datetime2 NULL,
                    CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_AuthenticationAccounts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
                    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_AuthenticationAccounts_UpdatedAtUtc DEFAULT SYSUTCDATETIME()
                );

                CREATE UNIQUE INDEX UX_AuthenticationAccounts_PlayerId ON dbo.AuthenticationAccounts(PlayerId);
                CREATE UNIQUE INDEX UX_AuthenticationAccounts_Email ON dbo.AuthenticationAccounts(Email);
            END

            IF OBJECT_ID(N'dbo.AuthenticationSessions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AuthenticationSessions
                (
                    SessionId nvarchar(128) NOT NULL CONSTRAINT PK_AuthenticationSessions PRIMARY KEY,
                    AccountId uniqueidentifier NOT NULL,
                    PlayerId uniqueidentifier NOT NULL,
                    AuthenticationProvider int NOT NULL,
                    LoginUtc datetime2 NOT NULL,
                    LastActivityUtc datetime2 NOT NULL,
                    ExpirationUtc datetime2 NOT NULL,
                    ClientVersion nvarchar(32) NOT NULL,
                    IpAddress nvarchar(64) NOT NULL,
                    DeviceIdentifier nvarchar(128) NOT NULL,
                    Region nvarchar(32) NOT NULL,
                    IsRevoked bit NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_AuthenticationSessions_CreatedAtUtc DEFAULT SYSUTCDATETIME()
                );

                CREATE INDEX IX_AuthenticationSessions_AccountId ON dbo.AuthenticationSessions(AccountId);
                CREATE INDEX IX_AuthenticationSessions_PlayerId ON dbo.AuthenticationSessions(PlayerId);
                CREATE INDEX IX_AuthenticationSessions_ExpirationUtc ON dbo.AuthenticationSessions(ExpirationUtc);
            END
            """),
        new DatabaseScript(
            "040_colonies.sql",
            """
            IF OBJECT_ID(N'dbo.Colonies', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Colonies
                (
                    ColonyId uniqueidentifier NOT NULL CONSTRAINT PK_Colonies PRIMARY KEY,
                    PlayerId uniqueidentifier NOT NULL,
                    WorldId uniqueidentifier NOT NULL,
                    HiveName nvarchar(128) NOT NULL,
                    QueenId uniqueidentifier NOT NULL,
                    CurrentSeason nvarchar(64) NOT NULL,
                    CurrentPopulation int NOT NULL,
                    ColonyLevel int NOT NULL,
                    PrestigeLevel int NOT NULL,
                    Status int NOT NULL,
                    SavePolicy nvarchar(64) NOT NULL,
                    CompressionPolicy nvarchar(64) NOT NULL,
                    VersioningStrategy nvarchar(64) NOT NULL,
                    StatisticsJson nvarchar(max) NOT NULL,
                    HistoryJson nvarchar(max) NOT NULL,
                    Revision bigint NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_Colonies_UpdatedAtUtc DEFAULT SYSUTCDATETIME()
                );

                CREATE INDEX IX_Colonies_PlayerId ON dbo.Colonies(PlayerId);
                CREATE INDEX IX_Colonies_Status ON dbo.Colonies(Status);
                CREATE INDEX IX_Colonies_WorldId ON dbo.Colonies(WorldId);
            END
            """),
        new DatabaseScript(
            "050_colony_snapshots.sql",
            """
            IF OBJECT_ID(N'dbo.ColonySnapshots', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ColonySnapshots
                (
                    SnapshotId uniqueidentifier NOT NULL CONSTRAINT PK_ColonySnapshots PRIMARY KEY,
                    ColonyId uniqueidentifier NOT NULL,
                    Kind int NOT NULL,
                    BaseRevision bigint NOT NULL,
                    Revision bigint NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    Version nvarchar(32) NOT NULL,
                    Payload varbinary(max) NOT NULL,
                    MetadataJson nvarchar(max) NOT NULL
                );

                CREATE INDEX IX_ColonySnapshots_ColonyId_Revision ON dbo.ColonySnapshots(ColonyId, Revision DESC);
                CREATE INDEX IX_ColonySnapshots_CreatedAtUtc ON dbo.ColonySnapshots(CreatedAtUtc);
            END
            """),
        new DatabaseScript(
            "060_chat_messaging.sql",
            """
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
            """),
        new DatabaseScript(
            "061_chat_translations.sql",
            """
            IF OBJECT_ID(N'dbo.ChatMessageTranslations', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ChatMessageTranslations
                (
                    MessageId uniqueidentifier NOT NULL,
                    TargetLocale nvarchar(16) NOT NULL,
                    ModelVersion nvarchar(64) NOT NULL,
                    SourceLocale nvarchar(16) NOT NULL,
                    TranslatedText nvarchar(2000) NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    CONSTRAINT PK_ChatMessageTranslations PRIMARY KEY(MessageId,TargetLocale,ModelVersion),
                    CONSTRAINT FK_ChatMessageTranslations_Message FOREIGN KEY(MessageId) REFERENCES dbo.ChatMessages(MessageId) ON DELETE CASCADE
                );
                CREATE INDEX IX_ChatMessageTranslations_CreatedAtUtc ON dbo.ChatMessageTranslations(CreatedAtUtc);
            END
            """),
        new DatabaseScript(
            "062_chat_creation_idempotency.sql",
            """
            IF OBJECT_ID(N'dbo.ChatConversationCreationReceipts',N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ChatConversationCreationReceipts
                (
                    PlayerId uniqueidentifier NOT NULL,
                    ClientRequestId nvarchar(128) NOT NULL,
                    PayloadHash char(64) NOT NULL,
                    ConversationId uniqueidentifier NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    CONSTRAINT PK_ChatConversationCreationReceipts PRIMARY KEY(PlayerId,ClientRequestId),
                    CONSTRAINT FK_ChatConversationCreationReceipts_Conversation FOREIGN KEY(ConversationId) REFERENCES dbo.ChatConversations(ConversationId)
                );
            END
            """),
        new DatabaseScript(
            "063_chat_moderation_idempotency.sql",
            """
            IF OBJECT_ID(N'dbo.ChatModerationReportReceipts',N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ChatModerationReportReceipts
                (
                    ReporterPlayerId uniqueidentifier NOT NULL,
                    ClientRequestId nvarchar(128) NOT NULL,
                    PayloadHash char(64) NOT NULL,
                    ReportId uniqueidentifier NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    ExpiresAtUtc datetime2 NOT NULL CONSTRAINT DF_ChatModerationReportReceipts_ExpiresAtUtc DEFAULT DATEADD(day,30,SYSUTCDATETIME()),
                    CONSTRAINT PK_ChatModerationReportReceipts PRIMARY KEY(ReporterPlayerId,ClientRequestId),
                    CONSTRAINT FK_ChatModerationReportReceipts_Report FOREIGN KEY(ReportId) REFERENCES dbo.ChatModerationReports(ReportId)
                );
                CREATE INDEX IX_ChatModerationReportReceipts_ExpiresAtUtc ON dbo.ChatModerationReportReceipts(ExpiresAtUtc);
            END
            """),
        new DatabaseScript(
            "064_chat_contract_bounds.sql",
            """
            IF COL_LENGTH(N'dbo.ChatMessages', N'Body') IS NOT NULL ALTER TABLE dbo.ChatMessages ALTER COLUMN Body nvarchar(4000) NOT NULL;
            IF COL_LENGTH(N'dbo.ChatMessages', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatMessages ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
            IF COL_LENGTH(N'dbo.ChatOutboxReceipts', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatOutboxReceipts ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
            IF COL_LENGTH(N'dbo.ChatConversationCreationReceipts', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatConversationCreationReceipts ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
            IF COL_LENGTH(N'dbo.ChatModerationReportReceipts', N'ClientRequestId') IS NOT NULL ALTER TABLE dbo.ChatModerationReportReceipts ALTER COLUMN ClientRequestId nvarchar(256) NOT NULL;
            IF COL_LENGTH(N'dbo.ChatMessageTranslations', N'TargetLocale') IS NOT NULL ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN TargetLocale nvarchar(35) NOT NULL;
            IF COL_LENGTH(N'dbo.ChatMessageTranslations', N'ModelVersion') IS NOT NULL ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN ModelVersion nvarchar(128) NOT NULL;
            IF COL_LENGTH(N'dbo.ChatMessageTranslations', N'TranslatedText') IS NOT NULL ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN TranslatedText nvarchar(max) NOT NULL;
            """),
        new DatabaseScript(
            "070_hive_operations.sql",
            """
            IF OBJECT_ID(N'dbo.HivePlayerStates', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.HivePlayerStates
                (
                    PlayerId uniqueidentifier NOT NULL,
                    HiveId uniqueidentifier NOT NULL,
                    ModelVersion int NOT NULL,
                    Revision bigint NOT NULL,
                    StateJson nvarchar(max) NOT NULL,
                    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_HivePlayerStates_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_HivePlayerStates PRIMARY KEY (PlayerId, HiveId),
                    CONSTRAINT CK_HivePlayerStates_ModelVersion CHECK (ModelVersion > 0),
                    CONSTRAINT CK_HivePlayerStates_Revision CHECK (Revision >= 0)
                );
            END

            IF OBJECT_ID(N'dbo.HiveCommandReceipts', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.HiveCommandReceipts
                (
                    PlayerId uniqueidentifier NOT NULL,
                    HiveId uniqueidentifier NOT NULL,
                    IdempotencyKeyHash char(64) NOT NULL,
                    PayloadHash char(64) NOT NULL,
                    ResultCode nvarchar(64) NOT NULL,
                    ResultJson nvarchar(max) NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    ExpiresAtUtc datetime2 NOT NULL,
                    CONSTRAINT PK_HiveCommandReceipts PRIMARY KEY (PlayerId, HiveId, IdempotencyKeyHash)
                );
                CREATE INDEX IX_HiveCommandReceipts_ExpiresAtUtc ON dbo.HiveCommandReceipts(ExpiresAtUtc);
            END

            IF OBJECT_ID(N'dbo.HiveOperationQueue', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.HiveOperationQueue
                (
                    OperationId uniqueidentifier NOT NULL CONSTRAINT PK_HiveOperationQueue PRIMARY KEY,
                    PlayerId uniqueidentifier NOT NULL,
                    HiveId uniqueidentifier NOT NULL,
                    BuildingKey nvarchar(128) NOT NULL,
                    FromLevel int NOT NULL,
                    ToLevel int NOT NULL,
                    StartedAtUtc datetime2 NOT NULL,
                    CompletesAtUtc datetime2 NOT NULL,
                    Status nvarchar(32) NOT NULL,
                    ProducedResourceKey nvarchar(64) NOT NULL,
                    ProducedAmount bigint NOT NULL,
                    CollectedAtUtc datetime2 NULL,
                    Revision bigint NOT NULL,
                    CONSTRAINT CK_HiveOperationQueue_Status CHECK (Status IN (N'Running', N'AwaitingCollection', N'Collected'))
                );
                CREATE INDEX IX_HiveOperationQueue_Due ON dbo.HiveOperationQueue(Status, CompletesAtUtc);
                CREATE INDEX IX_HiveOperationQueue_PlayerHive ON dbo.HiveOperationQueue(PlayerId, HiveId, Revision);
            END
            """),
        new DatabaseScript(
            "080_google_authentication_and_display_name.sql",
            """
            IF OBJECT_ID(N'dbo.AuthenticationAccounts', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'GoogleSubjectId')
                BEGIN
                    EXEC(N'ALTER TABLE dbo.AuthenticationAccounts ADD GoogleSubjectId nvarchar(64) NULL;');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'WorldId')
                BEGIN
                    EXEC(N'ALTER TABLE dbo.AuthenticationAccounts ADD WorldId uniqueidentifier NOT NULL CONSTRAINT DF_AuthenticationAccounts_WorldId DEFAULT ''00000000-0000-0000-0000-000000000101'';');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'DisplayName')
                BEGIN
                    EXEC(N'ALTER TABLE dbo.AuthenticationAccounts ADD DisplayName nvarchar(64) NULL;');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'IsOnboarded')
                BEGIN
                    EXEC(N'ALTER TABLE dbo.AuthenticationAccounts ADD IsOnboarded bit NOT NULL CONSTRAINT DF_AuthenticationAccounts_IsOnboarded DEFAULT 0;');
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'PasswordHash' AND is_nullable = 0)
                BEGIN
                    EXEC(N'ALTER TABLE dbo.AuthenticationAccounts ALTER COLUMN PasswordHash nvarchar(512) NULL;');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'UX_AuthenticationAccounts_GoogleSubjectId')
                BEGIN
                    EXEC(N'CREATE UNIQUE INDEX UX_AuthenticationAccounts_GoogleSubjectId ON dbo.AuthenticationAccounts(GoogleSubjectId) WHERE GoogleSubjectId IS NOT NULL;');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'UX_AuthenticationAccounts_WorldId_DisplayName')
                BEGIN
                    EXEC(N'CREATE UNIQUE INDEX UX_AuthenticationAccounts_WorldId_DisplayName ON dbo.AuthenticationAccounts(WorldId, DisplayName) WHERE DisplayName IS NOT NULL;');
                END
            END
            """),
        new DatabaseScript(
            "081_authentication_accounts_role.sql",
            """
            IF OBJECT_ID(N'dbo.AuthenticationAccounts', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'Role')
                BEGIN
                    EXEC(N'ALTER TABLE dbo.AuthenticationAccounts ADD Role int NOT NULL CONSTRAINT DF_AuthenticationAccounts_Role DEFAULT 0;');
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'IX_AuthenticationAccounts_Role')
                BEGIN
                    EXEC(N'CREATE INDEX IX_AuthenticationAccounts_Role ON dbo.AuthenticationAccounts(Role) WHERE Role <> 0;');
                END
            END
            """)
    ];
}
