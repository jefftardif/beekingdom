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
