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
