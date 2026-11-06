-- BeeKingdom WorldMap spatial reservations v1 -- STAGING ONLY
-- Separate feature migration, NEVER enrolled into automatic DatabaseCatalog.Migrations.
-- Must not be executed against the live game DB until signed/approved terrain,
-- server-side footprint catalog, API authority and tested rollback are available.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.WorldMapSpatialScopes',N'U') IS NULL
BEGIN
  CREATE TABLE dbo.WorldMapSpatialScopes(
    GameServerId uniqueidentifier NOT NULL,
    WorldId nvarchar(128) NOT NULL,
    SurfaceId nvarchar(128) NOT NULL,
    TerrainRevision nvarchar(64) NOT NULL,
    TerrainContentSha256 char(64) NOT NULL,
    OccupationRevision bigint NOT NULL CONSTRAINT DF_WMSpatialScopes_Revision DEFAULT 1,
    WritesEnabled bit NOT NULL CONSTRAINT DF_WMSpatialScopes_WritesEnabled DEFAULT 0,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_WMSpatialScopes_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_WMSpatialScopes_Updated DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_WMSpatialScopes PRIMARY KEY(GameServerId,WorldId,SurfaceId),
    CONSTRAINT CK_WMSpatialScopes_Revision CHECK(OccupationRevision>0),
    CONSTRAINT CK_WMSpatialScopes_TerrainSha CHECK(LEN(TerrainContentSha256)=64)
  );
END;
IF OBJECT_ID(N'dbo.WorldMapSpatialReservations',N'U') IS NULL
BEGIN
  CREATE TABLE dbo.WorldMapSpatialReservations(
    GameServerId uniqueidentifier NOT NULL,
    WorldId nvarchar(128) NOT NULL,
    SurfaceId nvarchar(128) NOT NULL,
    Kind nvarchar(32) NOT NULL,
    EntityId nvarchar(128) NOT NULL,
    PlacementId nvarchar(128) NOT NULL,
    OwnerPlayerId uniqueidentifier NULL,
    TerrainRevision nvarchar(64) NOT NULL,
    OccupationRevision bigint NOT NULL,
    X decimal(19,6) NOT NULL,
    Y decimal(19,6) NOT NULL,
    BoxLeft decimal(19,6) NOT NULL,
    BoxTop decimal(19,6) NOT NULL,
    BoxRight decimal(19,6) NOT NULL,
    BoxBottom decimal(19,6) NOT NULL,
    IsHistorical bit NOT NULL CONSTRAINT DF_WMSpatialReservations_Historical DEFAULT 0,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_WMSpatialReservations_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_WMSpatialReservations_Updated DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_WMSpatialReservations PRIMARY KEY(GameServerId,WorldId,SurfaceId,Kind,EntityId),
    CONSTRAINT FK_WMSpatialReservations_Scope FOREIGN KEY(GameServerId,WorldId,SurfaceId)
      REFERENCES dbo.WorldMapSpatialScopes(GameServerId,WorldId,SurfaceId),
    CONSTRAINT CK_WMSpatialReservations_Box CHECK(
       BoxLeft<BoxRight AND BoxTop<BoxBottom AND
       X>=BoxLeft AND X<=BoxRight AND Y>=BoxTop AND Y<=BoxBottom),
    CONSTRAINT CK_WMSpatialReservations_Revision CHECK(OccupationRevision>0)
  );
  CREATE UNIQUE INDEX UX_WMSpatialReservations_Placement
     ON dbo.WorldMapSpatialReservations(GameServerId,WorldId,SurfaceId,PlacementId);
  CREATE INDEX IX_WMSpatialReservations_Overlap
     ON dbo.WorldMapSpatialReservations(GameServerId,WorldId,SurfaceId,BoxLeft,BoxRight,BoxTop,BoxBottom);
END;
IF OBJECT_ID(N'dbo.WorldMapSpatialReceipts',N'U') IS NULL
BEGIN
  CREATE TABLE dbo.WorldMapSpatialReceipts(
    GameServerId uniqueidentifier NOT NULL,
    WorldId nvarchar(128) NOT NULL,
    SurfaceId nvarchar(128) NOT NULL,
    ActorPlayerId uniqueidentifier NOT NULL,
    ClientRequestId nvarchar(128) NOT NULL,
    PayloadHash char(64) NOT NULL,
    ResultJson nvarchar(max) NOT NULL,
    OccupationRevision bigint NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_WMSpatialReceipts_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_WMSpatialReceipts PRIMARY KEY(GameServerId,WorldId,SurfaceId,ActorPlayerId,ClientRequestId),
    CONSTRAINT FK_WMSpatialReceipts_Scope FOREIGN KEY(GameServerId,WorldId,SurfaceId)
      REFERENCES dbo.WorldMapSpatialScopes(GameServerId,WorldId,SurfaceId),
    CONSTRAINT CK_WMSpatialReceipts_Hash CHECK(LEN(PayloadHash)=64)
  );
END;
COMMIT TRANSACTION;
