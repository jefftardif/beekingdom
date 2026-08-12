IF OBJECT_ID(N'dbo.ColonySnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ColonySnapshots
    (
        SnapshotId uniqueidentifier NOT NULL CONSTRAINT PK_ColonySnapshots PRIMARY KEY,
        ColonyId uniqueidentifier NOT NULL,
        Kind int NOT NULL,
        BaseRevision bigint NOT NULL,
        Revision bigint NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        Version nvarchar(32) NOT NULL,
        Payload varbinary(max) NOT NULL,
        MetadataJson nvarchar(max) NOT NULL
    );

    CREATE INDEX IX_ColonySnapshots_ColonyId_Revision ON dbo.ColonySnapshots(ColonyId, Revision DESC);
    CREATE INDEX IX_ColonySnapshots_CreatedAtUtc ON dbo.ColonySnapshots(CreatedAtUtc);
END
