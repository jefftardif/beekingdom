/*
SERVER-B-057 WORLD SCHEMA READINESS DRY RUN - LOCAL ONLY

This script validates only the minimum relational shape needed for a future
world-map persistence wave. It is not registered in DatabaseCatalog and rolls
back every object and synthetic row it creates.

Allowed target: BeeKingdom_Local_SERVERB057_* on local SQL Server LocalDB.
Never run against staging, production, or 104.129.128.136.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() NOT LIKE N'BeeKingdom[_]Local[_]SERVERB057[_]%'
BEGIN
    THROW 51057, 'SERVER-B-057 world schema dry-run refuses non-disposable databases.', 1;
END;

IF OBJECT_ID(N'dbo.world_chunks', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.world_hive_nodes', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.world_resource_nodes', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.world_flights', N'U') IS NOT NULL
BEGIN
    THROW 51057, 'SERVER-B-057 draft tables already exist; refusing to alter them.', 1;
END;

BEGIN TRANSACTION SERVERB057_WORLD_DRYRUN;

CREATE TABLE dbo.world_chunks
(
    WorldId uniqueidentifier NOT NULL,
    ChunkX int NOT NULL,
    ChunkY int NOT NULL,
    GameServerId uniqueidentifier NOT NULL,
    RegionKey nvarchar(128) NOT NULL,
    Revision bigint NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_world_chunks_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_world_chunks PRIMARY KEY (WorldId, ChunkX, ChunkY),
    CONSTRAINT UQ_world_chunks_region UNIQUE (WorldId, RegionKey),
    CONSTRAINT CK_world_chunks_revision CHECK (Revision >= 0)
);

CREATE INDEX IX_world_chunks_owner
    ON dbo.world_chunks(WorldId, GameServerId, ChunkX, ChunkY);

CREATE TABLE dbo.world_hive_nodes
(
    HiveNodeId uniqueidentifier NOT NULL CONSTRAINT PK_world_hive_nodes PRIMARY KEY,
    WorldId uniqueidentifier NOT NULL,
    GameServerId uniqueidentifier NOT NULL,
    ChunkX int NOT NULL,
    ChunkY int NOT NULL,
    OwnerPlayerId uniqueidentifier NOT NULL,
    OwnerColonyId uniqueidentifier NOT NULL,
    AllianceId uniqueidentifier NULL,
    PositionX int NOT NULL,
    PositionY int NOT NULL,
    ProtectionUntilUtc datetime2 NULL,
    VisibilityState int NOT NULL,
    PowerBand int NOT NULL,
    Revision bigint NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_world_hive_nodes_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_world_hive_nodes_world_id UNIQUE (WorldId, HiveNodeId),
    CONSTRAINT UQ_world_hive_nodes_colony UNIQUE (WorldId, OwnerColonyId),
    CONSTRAINT UQ_world_hive_nodes_position UNIQUE (WorldId, PositionX, PositionY),
    CONSTRAINT FK_world_hive_nodes_chunk FOREIGN KEY (WorldId, ChunkX, ChunkY)
        REFERENCES dbo.world_chunks(WorldId, ChunkX, ChunkY),
    CONSTRAINT CK_world_hive_nodes_revision CHECK (Revision >= 0)
);

CREATE INDEX IX_world_hive_nodes_chunk
    ON dbo.world_hive_nodes(WorldId, ChunkX, ChunkY, Revision);

CREATE INDEX IX_world_hive_nodes_alliance
    ON dbo.world_hive_nodes(WorldId, AllianceId) WHERE AllianceId IS NOT NULL;

CREATE TABLE dbo.world_resource_nodes
(
    ResourceNodeId uniqueidentifier NOT NULL CONSTRAINT PK_world_resource_nodes PRIMARY KEY,
    WorldId uniqueidentifier NOT NULL,
    GameServerId uniqueidentifier NOT NULL,
    ChunkX int NOT NULL,
    ChunkY int NOT NULL,
    PositionX int NOT NULL,
    PositionY int NOT NULL,
    ResourceKind int NOT NULL,
    RichnessBand int NOT NULL,
    RemainingAmountServerOnly bigint NOT NULL,
    OccupancyState int NOT NULL,
    OccupiedByFlightId uniqueidentifier NULL,
    RegeneratesAtUtc datetime2 NULL,
    Revision bigint NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_world_resource_nodes_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_world_resource_nodes_world_id UNIQUE (WorldId, ResourceNodeId),
    CONSTRAINT UQ_world_resource_nodes_position UNIQUE (WorldId, PositionX, PositionY),
    CONSTRAINT FK_world_resource_nodes_chunk FOREIGN KEY (WorldId, ChunkX, ChunkY)
        REFERENCES dbo.world_chunks(WorldId, ChunkX, ChunkY),
    CONSTRAINT CK_world_resource_nodes_amount CHECK (RemainingAmountServerOnly >= 0),
    CONSTRAINT CK_world_resource_nodes_revision CHECK (Revision >= 0)
);

CREATE INDEX IX_world_resource_nodes_chunk_kind
    ON dbo.world_resource_nodes(WorldId, ChunkX, ChunkY, ResourceKind, OccupancyState);

CREATE INDEX IX_world_resource_nodes_regeneration
    ON dbo.world_resource_nodes(WorldId, RegeneratesAtUtc) WHERE RegeneratesAtUtc IS NOT NULL;

CREATE TABLE dbo.world_flights
(
    FlightId uniqueidentifier NOT NULL CONSTRAINT PK_world_flights PRIMARY KEY,
    WorldId uniqueidentifier NOT NULL,
    GameServerId uniqueidentifier NOT NULL,
    OwnerPlayerId uniqueidentifier NOT NULL,
    OriginHiveNodeId uniqueidentifier NOT NULL,
    DestinationEntityId uniqueidentifier NOT NULL,
    FlightKind int NOT NULL,
    FlightState int NOT NULL,
    OriginX int NOT NULL,
    OriginY int NOT NULL,
    DestinationX int NOT NULL,
    DestinationY int NOT NULL,
    DepartedAtUtc datetime2 NOT NULL,
    ArrivesAtUtc datetime2 NOT NULL,
    ReturnsAtUtc datetime2 NULL,
    IdempotencyKeyHash varbinary(32) NOT NULL,
    RequestPayloadHash varbinary(32) NOT NULL,
    Revision bigint NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_world_flights_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_world_flights_world_id UNIQUE (WorldId, FlightId),
    CONSTRAINT UQ_world_flights_idempotency UNIQUE (WorldId, OwnerPlayerId, IdempotencyKeyHash),
    CONSTRAINT FK_world_flights_origin_hive FOREIGN KEY (WorldId, OriginHiveNodeId)
        REFERENCES dbo.world_hive_nodes(WorldId, HiveNodeId),
    CONSTRAINT CK_world_flights_arrival CHECK (ArrivesAtUtc > DepartedAtUtc),
    CONSTRAINT CK_world_flights_return CHECK (ReturnsAtUtc IS NULL OR ReturnsAtUtc >= ArrivesAtUtc),
    CONSTRAINT CK_world_flights_revision CHECK (Revision >= 0)
);

CREATE INDEX IX_world_flights_due
    ON dbo.world_flights(WorldId, FlightState, ArrivesAtUtc);

CREATE INDEX IX_world_flights_owner
    ON dbo.world_flights(WorldId, OwnerPlayerId, FlightState);

DECLARE @WorldA uniqueidentifier = NEWID();
DECLARE @WorldB uniqueidentifier = NEWID();
DECLARE @GameServer uniqueidentifier = NEWID();
DECLARE @Player uniqueidentifier = NEWID();
DECLARE @ColonyA uniqueidentifier = NEWID();
DECLARE @ColonyB uniqueidentifier = NEWID();
DECLARE @HiveA uniqueidentifier = NEWID();
DECLARE @HiveB uniqueidentifier = NEWID();
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @IdempotencyHash varbinary(32) = HASHBYTES('SHA2_256', N'server-b-057-idempotency');
DECLARE @PayloadHash varbinary(32) = HASHBYTES('SHA2_256', N'server-b-057-payload');

INSERT INTO dbo.world_chunks (WorldId, ChunkX, ChunkY, GameServerId, RegionKey, Revision)
VALUES
    (@WorldA, 4, 7, @GameServer, N'4:7', 1),
    (@WorldB, 4, 7, @GameServer, N'4:7', 1);

INSERT INTO dbo.world_hive_nodes
    (HiveNodeId, WorldId, GameServerId, ChunkX, ChunkY, OwnerPlayerId, OwnerColonyId, PositionX, PositionY, VisibilityState, PowerBand, Revision)
VALUES
    (@HiveA, @WorldA, @GameServer, 4, 7, @Player, @ColonyA, 4001, 7001, 1, 1, 1),
    (@HiveB, @WorldB, @GameServer, 4, 7, @Player, @ColonyB, 4001, 7001, 1, 1, 1);

INSERT INTO dbo.world_flights
    (FlightId, WorldId, GameServerId, OwnerPlayerId, OriginHiveNodeId, DestinationEntityId, FlightKind, FlightState,
     OriginX, OriginY, DestinationX, DestinationY, DepartedAtUtc, ArrivesAtUtc, IdempotencyKeyHash, RequestPayloadHash, Revision)
VALUES
    (NEWID(), @WorldA, @GameServer, @Player, @HiveA, NEWID(), 1, 1, 4001, 7001, 4010, 7010, @Now, DATEADD(minute, 5, @Now), @IdempotencyHash, @PayloadHash, 1),
    (NEWID(), @WorldB, @GameServer, @Player, @HiveB, NEWID(), 1, 1, 4001, 7001, 4010, 7010, @Now, DATEADD(minute, 5, @Now), @IdempotencyHash, @PayloadHash, 1);

IF (SELECT COUNT(*) FROM dbo.world_chunks WHERE ChunkX = 4 AND ChunkY = 7) <> 2
   OR (SELECT COUNT(*) FROM dbo.world_flights WHERE IdempotencyKeyHash = @IdempotencyHash) <> 2
BEGIN
    THROW 51057, 'WorldId scoping validation failed in SERVER-B-057 dry-run.', 1;
END;

ROLLBACK TRANSACTION SERVERB057_WORLD_DRYRUN;
