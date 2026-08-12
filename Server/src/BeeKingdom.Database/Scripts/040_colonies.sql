IF OBJECT_ID(N'dbo.Colonies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Colonies
    (
        ColonyId uniqueidentifier NOT NULL CONSTRAINT PK_Colonies PRIMARY KEY,
        PlayerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        HiveName nvarchar(128) NOT NULL,
        QueenId uniqueidentifier NOT NULL,
        CurrentSeason nvarchar(64) NOT NULL,
        CurrentPopulation int NOT NULL,
        ColonyLevel int NOT NULL,
        PrestigeLevel int NOT NULL,
        Status int NOT NULL,
        SavePolicy nvarchar(64) NOT NULL,
        CompressionPolicy nvarchar(64) NOT NULL,
        VersioningStrategy nvarchar(64) NOT NULL,
        StatisticsJson nvarchar(max) NOT NULL,
        HistoryJson nvarchar(max) NOT NULL,
        Revision bigint NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_Colonies_UpdatedAtUtc DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_Colonies_PlayerId ON dbo.Colonies(PlayerId);
    CREATE INDEX IX_Colonies_Status ON dbo.Colonies(Status);
    CREATE INDEX IX_Colonies_WorldId ON dbo.Colonies(WorldId);
END
