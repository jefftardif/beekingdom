/*
SERVER-039 HIVE LOOP LOCAL SQL OPT-IN DRY RUN ONLY

Purpose:
- Future SQL mapping plan for Hive Loop persistence readiness tables.
- Local development dry-run only.
- Not registered in BeeKingdom.Database.DatabaseCatalog.
- Not a production migration.
- Do not run against 104.129.128.136.
- Do not run against production databases.
- Does not persist data because it rolls back the transaction.

Required local target:
- Database name must start with BeeKingdom_Local or BeeKingdomDev.
*/

SET XACT_ABORT ON;

IF DB_NAME() NOT LIKE N'BeeKingdom[_]Local%' AND DB_NAME() NOT LIKE N'BeeKingdomDev%'
BEGIN
    THROW 51039, 'SERVER-039 dry-run is local opt-in only. Refusing non-local database.', 1;
END;

BEGIN TRANSACTION SERVER_039_HIVE_LOOP_DRY_RUN;

IF OBJECT_ID(N'dbo.player_resources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.player_resources
    (
        PlayerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        GameServerId uniqueidentifier NOT NULL,
        ResourceKey nvarchar(64) NOT NULL,
        Amount bigint NOT NULL,
        Capacity bigint NOT NULL,
        Revision bigint NOT NULL,
        CatalogVersion nvarchar(64) NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_player_resources_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_player_resources PRIMARY KEY (PlayerId, WorldId, GameServerId, ResourceKey),
        CONSTRAINT CK_player_resources_Amount_NonNegative CHECK (Amount >= 0),
        CONSTRAINT CK_player_resources_Capacity_NonNegative CHECK (Capacity >= 0),
        CONSTRAINT CK_player_resources_Revision_Positive CHECK (Revision >= 0)
    );

    CREATE INDEX IX_player_resources_scope_revision
        ON dbo.player_resources(PlayerId, WorldId, GameServerId, Revision);
END;

IF OBJECT_ID(N'dbo.hive_buildings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.hive_buildings
    (
        BuildingId uniqueidentifier NOT NULL CONSTRAINT PK_hive_buildings PRIMARY KEY,
        PlayerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        GameServerId uniqueidentifier NOT NULL,
        BuildingKey nvarchar(64) NOT NULL,
        Level int NOT NULL,
        Revision bigint NOT NULL,
        CatalogVersion nvarchar(64) NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_hive_buildings_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_hive_buildings_Level_NonNegative CHECK (Level >= 0),
        CONSTRAINT CK_hive_buildings_Revision_Positive CHECK (Revision >= 0)
    );

    CREATE UNIQUE INDEX UX_hive_buildings_player_scope_key
        ON dbo.hive_buildings(PlayerId, WorldId, GameServerId, BuildingKey);

    CREATE INDEX IX_hive_buildings_scope_revision
        ON dbo.hive_buildings(PlayerId, WorldId, GameServerId, Revision);
END;

IF OBJECT_ID(N'dbo.construction_queue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.construction_queue
    (
        QueueItemId uniqueidentifier NOT NULL CONSTRAINT PK_construction_queue PRIMARY KEY,
        PlayerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        GameServerId uniqueidentifier NOT NULL,
        BuildingId uniqueidentifier NOT NULL,
        BuildingKey nvarchar(64) NOT NULL,
        FromLevel int NOT NULL,
        ToLevel int NOT NULL,
        EnqueuedAtUtc datetime2 NOT NULL,
        CompleteAtUtc datetime2 NOT NULL,
        ExpectedResourceRevision bigint NOT NULL,
        ExpectedBuildingRevision bigint NOT NULL,
        Status int NOT NULL,
        IdempotencyKeyHash varbinary(32) NOT NULL,
        CatalogVersion nvarchar(64) NOT NULL,
        CONSTRAINT CK_construction_queue_LevelAdvance CHECK (ToLevel = FromLevel + 1),
        CONSTRAINT CK_construction_queue_TimeOrder CHECK (CompleteAtUtc > EnqueuedAtUtc)
    );

    CREATE INDEX IX_construction_queue_scope_status_time
        ON dbo.construction_queue(PlayerId, WorldId, GameServerId, Status, CompleteAtUtc);

    CREATE INDEX IX_construction_queue_expected_revisions
        ON dbo.construction_queue(PlayerId, WorldId, GameServerId, ExpectedResourceRevision, ExpectedBuildingRevision);
END;

IF OBJECT_ID(N'dbo.troop_counts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.troop_counts
    (
        PlayerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        GameServerId uniqueidentifier NOT NULL,
        TroopKey nvarchar(64) NOT NULL,
        Quantity bigint NOT NULL,
        ArmyRevision bigint NOT NULL,
        CatalogVersion nvarchar(64) NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_troop_counts_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_troop_counts PRIMARY KEY (PlayerId, WorldId, GameServerId, TroopKey),
        CONSTRAINT CK_troop_counts_Quantity_NonNegative CHECK (Quantity >= 0),
        CONSTRAINT CK_troop_counts_ArmyRevision_Positive CHECK (ArmyRevision >= 0)
    );

    CREATE INDEX IX_troop_counts_scope_revision
        ON dbo.troop_counts(PlayerId, WorldId, GameServerId, ArmyRevision);
END;

IF OBJECT_ID(N'dbo.training_queue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.training_queue
    (
        QueueItemId uniqueidentifier NOT NULL CONSTRAINT PK_training_queue PRIMARY KEY,
        PlayerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        GameServerId uniqueidentifier NOT NULL,
        TroopKey nvarchar(64) NOT NULL,
        Quantity int NOT NULL,
        EnqueuedAtUtc datetime2 NOT NULL,
        CompleteAtUtc datetime2 NOT NULL,
        ExpectedResourceRevision bigint NOT NULL,
        ExpectedArmyRevision bigint NOT NULL,
        Status int NOT NULL,
        IdempotencyKeyHash varbinary(32) NOT NULL,
        CatalogVersion nvarchar(64) NOT NULL,
        CONSTRAINT CK_training_queue_Quantity_Positive CHECK (Quantity > 0),
        CONSTRAINT CK_training_queue_TimeOrder CHECK (CompleteAtUtc > EnqueuedAtUtc)
    );

    CREATE INDEX IX_training_queue_scope_status_time
        ON dbo.training_queue(PlayerId, WorldId, GameServerId, Status, CompleteAtUtc);

    CREATE INDEX IX_training_queue_expected_revisions
        ON dbo.training_queue(PlayerId, WorldId, GameServerId, ExpectedResourceRevision, ExpectedArmyRevision);
END;

IF OBJECT_ID(N'dbo.idempotency_records', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.idempotency_records
    (
        PlayerId uniqueidentifier NOT NULL,
        WorldId uniqueidentifier NOT NULL,
        GameServerId uniqueidentifier NOT NULL,
        IdempotencyKeyHash varbinary(32) NOT NULL,
        RequestPayloadHash varbinary(32) NOT NULL,
        CommandKind nvarchar(64) NOT NULL,
        ResultPayloadHash varbinary(32) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        ExpiresAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_idempotency_records PRIMARY KEY (PlayerId, WorldId, GameServerId, IdempotencyKeyHash),
        CONSTRAINT CK_idempotency_records_TimeOrder CHECK (ExpiresAtUtc > CreatedAtUtc)
    );

    CREATE UNIQUE INDEX UX_idempotency_records_scope_payload
        ON dbo.idempotency_records(PlayerId, WorldId, GameServerId, IdempotencyKeyHash, RequestPayloadHash);

    CREATE INDEX IX_idempotency_records_expiration
        ON dbo.idempotency_records(ExpiresAtUtc);
END;

SELECT
    DB_NAME() AS DryRunDatabase,
    'SERVER-039 local opt-in dry-run only; transaction will be rolled back.' AS DryRunStatus;

ROLLBACK TRANSACTION SERVER_039_HIVE_LOOP_DRY_RUN;
