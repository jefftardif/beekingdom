namespace BeeKingdom.Database;

public static class DatabaseRollbackCatalog
{
    public static IReadOnlyList<DatabaseScript> Rollbacks { get; } =
    [
        new DatabaseScript(
            "070_rollback_hive_operations.sql",
            """
            IF OBJECT_ID(N'dbo.HiveOperationQueue', N'U') IS NOT NULL DROP TABLE dbo.HiveOperationQueue;
            IF OBJECT_ID(N'dbo.HiveCommandReceipts', N'U') IS NOT NULL DROP TABLE dbo.HiveCommandReceipts;
            IF OBJECT_ID(N'dbo.HivePlayerStates', N'U') IS NOT NULL DROP TABLE dbo.HivePlayerStates;
            """),
        new DatabaseScript(
            "064_rollback_chat_contract_bounds.sql",
            """
            IF EXISTS (SELECT 1 FROM dbo.ChatMessages WHERE LEN(Body)>1000 OR LEN(ClientRequestId)>128) THROW 51064, 'Rollback blocked: ChatMessages data exceeds legacy bounds.', 1;
            IF EXISTS (SELECT 1 FROM dbo.ChatMessageTranslations WHERE LEN(TargetLocale)>16 OR LEN(ModelVersion)>64 OR LEN(TranslatedText)>2000) THROW 51064, 'Rollback blocked: translation data exceeds legacy bounds.', 1;
            ALTER TABLE dbo.ChatMessages ALTER COLUMN Body nvarchar(1000) NOT NULL;
            ALTER TABLE dbo.ChatMessages ALTER COLUMN ClientRequestId nvarchar(128) NOT NULL;
            ALTER TABLE dbo.ChatOutboxReceipts ALTER COLUMN ClientRequestId nvarchar(128) NOT NULL;
            ALTER TABLE dbo.ChatConversationCreationReceipts ALTER COLUMN ClientRequestId nvarchar(128) NOT NULL;
            ALTER TABLE dbo.ChatModerationReportReceipts ALTER COLUMN ClientRequestId nvarchar(128) NOT NULL;
            ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN TargetLocale nvarchar(16) NOT NULL;
            ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN ModelVersion nvarchar(64) NOT NULL;
            ALTER TABLE dbo.ChatMessageTranslations ALTER COLUMN TranslatedText nvarchar(2000) NOT NULL;
            """),
        new DatabaseScript(
            "063_rollback_chat_moderation_idempotency.sql",
            """
            IF OBJECT_ID(N'dbo.ChatModerationReportReceipts',N'U') IS NOT NULL DROP TABLE dbo.ChatModerationReportReceipts;
            """),
        new DatabaseScript(
            "062_rollback_chat_creation_idempotency.sql",
            """
            IF OBJECT_ID(N'dbo.ChatConversationCreationReceipts',N'U') IS NOT NULL DROP TABLE dbo.ChatConversationCreationReceipts;
            """),
        new DatabaseScript(
            "061_rollback_chat_translations.sql",
            """
            IF OBJECT_ID(N'dbo.ChatMessageTranslations', N'U') IS NOT NULL DROP TABLE dbo.ChatMessageTranslations;
            """),
        new DatabaseScript(
            "060_rollback_chat_messaging.sql",
            """
            IF OBJECT_ID(N'dbo.ChatModerationReports', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ChatModerationReports;
            END

            IF OBJECT_ID(N'dbo.ChatOutboxReceipts', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ChatOutboxReceipts;
            END

            IF OBJECT_ID(N'dbo.ChatInbox', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ChatInbox;
            END

            IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ChatMessages;
            END

            IF OBJECT_ID(N'dbo.ChatConversationSequences', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ChatConversationSequences;
            END

            IF OBJECT_ID(N'dbo.ChatConversationParticipants', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ChatConversationParticipants;
            END

            IF OBJECT_ID(N'dbo.ChatConversations', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ChatConversations;
            END
            """),
        new DatabaseScript(
            "050_rollback_colony_snapshots.sql",
            """
            IF OBJECT_ID(N'dbo.ColonySnapshots', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.ColonySnapshots;
            END
            """),
        new DatabaseScript(
            "040_rollback_colonies.sql",
            """
            IF OBJECT_ID(N'dbo.Colonies', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.Colonies;
            END
            """),
        new DatabaseScript(
            "030_rollback_authentication_sessions.sql",
            """
            IF OBJECT_ID(N'dbo.AuthenticationSessions', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.AuthenticationSessions;
            END

            IF OBJECT_ID(N'dbo.AuthenticationAccounts', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.AuthenticationAccounts;
            END
            """),
        new DatabaseScript(
            "020_rollback_accounts.sql",
            """
            IF OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL
            BEGIN
                DROP TABLE dbo.Accounts;
            END
            """)
    ];
}
