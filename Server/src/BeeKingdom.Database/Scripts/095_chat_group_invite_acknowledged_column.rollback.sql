IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ChatGroupInvites') AND name = 'IX_ChatGroupInvites_Inviter_Ack'
)
BEGIN
    DROP INDEX IX_ChatGroupInvites_Inviter_Ack ON dbo.ChatGroupInvites;
END

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ChatGroupInvites') AND name = 'InviterAcknowledged'
)
BEGIN
    ALTER TABLE dbo.ChatGroupInvites DROP CONSTRAINT DF_ChatGroupInvites_InviterAcknowledged;
    ALTER TABLE dbo.ChatGroupInvites DROP COLUMN InviterAcknowledged;
END

DELETE FROM dbo.SchemaVersion WHERE ScriptName = N'095_chat_group_invite_acknowledged_column.sql';
