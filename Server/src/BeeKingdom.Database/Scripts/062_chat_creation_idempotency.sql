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
