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
