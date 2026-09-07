IF OBJECT_ID(N'dbo.ChatGroupInvites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatGroupInvites
    (
        InviteId uniqueidentifier NOT NULL CONSTRAINT PK_ChatGroupInvites PRIMARY KEY,
        ConversationId uniqueidentifier NOT NULL,
        InviterPlayerId uniqueidentifier NOT NULL,
        InviteePlayerId uniqueidentifier NOT NULL,
        Status nvarchar(20) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        RespondedAtUtc datetime2 NULL,
        InviterAcknowledged bit NOT NULL CONSTRAINT DF_ChatGroupInvites_InviterAcknowledged DEFAULT 0,
        CONSTRAINT FK_ChatGroupInvites_Conversation FOREIGN KEY(ConversationId) REFERENCES dbo.ChatConversations(ConversationId)
    );

    CREATE INDEX IX_ChatGroupInvites_Invitee_Status ON dbo.ChatGroupInvites (InviteePlayerId, Status);
    CREATE INDEX IX_ChatGroupInvites_Inviter_Ack ON dbo.ChatGroupInvites (InviterPlayerId, InviterAcknowledged);
END

IF OBJECT_ID(N'dbo.ChatPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatPreferences
    (
        PlayerId uniqueidentifier NOT NULL CONSTRAINT PK_ChatPreferences PRIMARY KEY,
        AutoInviteResponse nvarchar(20) NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL
    );
END
