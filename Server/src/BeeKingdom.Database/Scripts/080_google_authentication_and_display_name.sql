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
