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
