IF OBJECT_ID(N'dbo.ChatGroupInvites', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ChatGroupInvites;
END

IF OBJECT_ID(N'dbo.ChatPreferences', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ChatPreferences;
END

DELETE FROM dbo.SchemaVersion WHERE ScriptName = N'094_chat_groups.sql';
