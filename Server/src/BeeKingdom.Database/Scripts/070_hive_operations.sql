IF OBJECT_ID(N'dbo.HivePlayerStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HivePlayerStates
    (
        PlayerId uniqueidentifier NOT NULL,
        HiveId uniqueidentifier NOT NULL,
        ModelVersion int NOT NULL,
        Revision bigint NOT NULL,
        StateJson nvarchar(max) NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_HivePlayerStates_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_HivePlayerStates PRIMARY KEY (PlayerId, HiveId),
        CONSTRAINT CK_HivePlayerStates_ModelVersion CHECK (ModelVersion > 0),
        CONSTRAINT CK_HivePlayerStates_Revision CHECK (Revision >= 0)
    );
END

IF OBJECT_ID(N'dbo.HiveCommandReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HiveCommandReceipts
    (
        PlayerId uniqueidentifier NOT NULL,
        HiveId uniqueidentifier NOT NULL,
        IdempotencyKeyHash char(64) NOT NULL,
        PayloadHash char(64) NOT NULL,
        ResultCode nvarchar(64) NOT NULL,
        ResultJson nvarchar(max) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        ExpiresAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_HiveCommandReceipts PRIMARY KEY (PlayerId, HiveId, IdempotencyKeyHash)
    );
    CREATE INDEX IX_HiveCommandReceipts_ExpiresAtUtc ON dbo.HiveCommandReceipts(ExpiresAtUtc);
END

IF OBJECT_ID(N'dbo.HiveOperationQueue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HiveOperationQueue
    (
        OperationId uniqueidentifier NOT NULL CONSTRAINT PK_HiveOperationQueue PRIMARY KEY,
        PlayerId uniqueidentifier NOT NULL,
        HiveId uniqueidentifier NOT NULL,
        BuildingKey nvarchar(128) NOT NULL,
        FromLevel int NOT NULL,
        ToLevel int NOT NULL,
        StartedAtUtc datetime2 NOT NULL,
        CompletesAtUtc datetime2 NOT NULL,
        Status nvarchar(32) NOT NULL,
        ProducedResourceKey nvarchar(64) NOT NULL,
        ProducedAmount bigint NOT NULL,
        CollectedAtUtc datetime2 NULL,
        Revision bigint NOT NULL,
        CONSTRAINT CK_HiveOperationQueue_Status CHECK (Status IN (N'Running', N'AwaitingCollection', N'Collected'))
    );
    CREATE INDEX IX_HiveOperationQueue_Due ON dbo.HiveOperationQueue(Status, CompletesAtUtc);
    CREATE INDEX IX_HiveOperationQueue_PlayerHive ON dbo.HiveOperationQueue(PlayerId, HiveId, Revision);
END
