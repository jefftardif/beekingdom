IF OBJECT_ID(N'dbo.FloralCivilContributions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FloralCivilContributions
    (
        MissionId uniqueidentifier NOT NULL
            CONSTRAINT PK_FloralCivilContributions PRIMARY KEY,
        PlayerId uniqueidentifier NOT NULL,
        HiveId uniqueidentifier NOT NULL,
        WorldId nvarchar(64) NOT NULL,
        SiteId nvarchar(64) NOT NULL,
        PollenRemoved bigint NOT NULL,
        PollenCredited bigint NOT NULL,
        PollinationPoints bigint NOT NULL,
        CompletedAtUtc datetime2 NOT NULL,
        CONSTRAINT CK_FloralCivilContributions_Balances
            CHECK (PollenRemoved > 0
                AND PollenCredited = PollenRemoved
                AND PollinationPoints = PollenRemoved)
    );
    CREATE INDEX IX_FloralCivilContributions_Site
        ON dbo.FloralCivilContributions(WorldId, SiteId, CompletedAtUtc);
    CREATE INDEX IX_FloralCivilContributions_Player
        ON dbo.FloralCivilContributions(PlayerId, CompletedAtUtc);
END
IF OBJECT_ID(N'dbo.FloralCivilActiveMissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FloralCivilActiveMissions
    (
        MissionId uniqueidentifier NOT NULL
            CONSTRAINT PK_FloralCivilActiveMissions PRIMARY KEY,
        PlayerId uniqueidentifier NOT NULL,
        HiveId uniqueidentifier NOT NULL,
        WorldId nvarchar(64) NOT NULL,
        SiteId nvarchar(64) NOT NULL,
        ReturnsAtUtc datetime2 NOT NULL
    );
    CREATE INDEX IX_FloralCivilActiveMissions_Due
        ON dbo.FloralCivilActiveMissions(ReturnsAtUtc, MissionId);
    CREATE INDEX IX_FloralCivilActiveMissions_Owner
        ON dbo.FloralCivilActiveMissions(PlayerId, HiveId);
END
