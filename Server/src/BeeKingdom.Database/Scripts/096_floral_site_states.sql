IF OBJECT_ID(N'dbo.FloralSiteStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FloralSiteStates
    (
        WorldId nvarchar(64) NOT NULL,
        SiteId nvarchar(64) NOT NULL,
        Revision bigint NOT NULL,
        StateJson nvarchar(max) NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL
            CONSTRAINT DF_FloralSiteStates_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_FloralSiteStates PRIMARY KEY (WorldId, SiteId),
        CONSTRAINT CK_FloralSiteStates_Revision CHECK (Revision >= 0)
    );
END
