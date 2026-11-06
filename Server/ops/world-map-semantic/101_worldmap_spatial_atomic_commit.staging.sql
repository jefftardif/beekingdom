-- BeeKingdom WorldMap atomic reservations - staging SQL contract only.
-- This stored procedure is NOT automatically migrated or granted to the live API.
-- The caller must first validate each full artwork+caption footprint against
-- independently reviewed SemanticTerrain; the DB enforces transaction/overlap.
-- Scopes default to WritesEnabled=0 and no scope is auto-seeded.
IF TYPE_ID(N'dbo.WorldMapSpatialPlacementBatch') IS NULL
EXEC(N'CREATE TYPE dbo.WorldMapSpatialPlacementBatch AS TABLE(
 Kind nvarchar(32) NOT NULL,
 EntityId nvarchar(128) NOT NULL,
 PlacementId nvarchar(128) NOT NULL,
 X decimal(19,6) NOT NULL, Y decimal(19,6) NOT NULL,
 BoxLeft decimal(19,6) NOT NULL, BoxTop decimal(19,6) NOT NULL,
 BoxRight decimal(19,6) NOT NULL, BoxBottom decimal(19,6) NOT NULL
)');
GO
CREATE OR ALTER PROCEDURE dbo.WorldMapCommitSpatialBatch
 @GameServerId uniqueidentifier,
 @WorldId nvarchar(128), @SurfaceId nvarchar(128),
 @ActorPlayerId uniqueidentifier, @RequestId nvarchar(128),
 @PayloadHash char(64), @ExpectedRevision bigint,
 @TerrainRevision nvarchar(64), @TerrainContentSha256 char(64),
 @Items dbo.WorldMapSpatialPlacementBatch READONLY
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF @GameServerId='00000000-0000-0000-0000-000000000000' OR
    @ActorPlayerId='00000000-0000-0000-0000-000000000000' OR
    @WorldId IS NULL OR LEN(@WorldId)=0 OR
    @SurfaceId IS NULL OR LEN(@SurfaceId)=0 OR
    @RequestId IS NULL OR LEN(@RequestId)=0 OR
    @ExpectedRevision<1 OR @TerrainRevision IS NULL OR
    LEN(@TerrainRevision)=0 OR LEN(@TerrainContentSha256)<>64 OR
    LEN(@PayloadHash)<>64 OR NOT EXISTS(SELECT 1 FROM @Items) OR
    (SELECT COUNT_BIG(*) FROM @Items)>100
    THROW 51100,'Spatial reservation request invalid.',1;
 IF EXISTS(SELECT 1 FROM @Items WHERE
   Kind=N'' OR EntityId=N'' OR PlacementId=N'' OR
   BoxLeft>=BoxRight OR BoxTop>=BoxBottom OR
   X<BoxLeft OR X>BoxRight OR Y<BoxTop OR Y>BoxBottom)
    THROW 51101,'Spatial placement geometry invalid.',1;
 IF EXISTS(SELECT 1 FROM @Items GROUP BY Kind,EntityId HAVING COUNT(*)>1)
    THROW 51102,'Repeated entity in spatial batch.',1;
 IF EXISTS(SELECT 1 FROM @Items GROUP BY PlacementId HAVING COUNT(*)>1)
    THROW 51103,'Repeated placement ID in spatial batch.',1;

 BEGIN TRANSACTION;
 DECLARE @lock nvarchar(255)=N'bk-map:'+CONVERT(varchar(64),
   HASHBYTES('SHA2_256',CONVERT(varchar(36),@GameServerId)+
   N'|'+@WorldId+N'|'+@SurfaceId),2);
 DECLARE @lockCode int;
 EXEC @lockCode=sys.sp_getapplock @Resource=@lock,
     @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=7000;
 IF @lockCode<0 THROW 51104,'Spatial scope lock unavailable.',1;

 DECLARE @revision bigint, @storedTerrain nvarchar(64),
         @storedHash char(64),@enabled bit;
 SELECT @revision=OccupationRevision,@storedTerrain=TerrainRevision,
        @storedHash=TerrainContentSha256,@enabled=WritesEnabled
 FROM dbo.WorldMapSpatialScopes WITH(UPDLOCK,HOLDLOCK)
 WHERE GameServerId=@GameServerId AND WorldId=@WorldId
   AND SurfaceId=@SurfaceId;
 IF @revision IS NULL OR @enabled<>1
    THROW 51105,'Spatial writes disabled or scope not provisioned.',1;
 IF @storedTerrain<>@TerrainRevision OR
    UPPER(@storedHash)<>UPPER(@TerrainContentSha256)
    THROW 51106,'Unapproved terrain revision.',1;
 DECLARE @oldHash char(64),@oldRevision bigint;
 SELECT @oldHash=PayloadHash,@oldRevision=OccupationRevision
 FROM dbo.WorldMapSpatialReceipts WITH(UPDLOCK,HOLDLOCK)
 WHERE GameServerId=@GameServerId AND WorldId=@WorldId
   AND SurfaceId=@SurfaceId AND ActorPlayerId=@ActorPlayerId
   AND ClientRequestId=@RequestId;
 IF @oldHash IS NOT NULL
 BEGIN
   IF @oldHash<>@PayloadHash THROW 51107,'Idempotency key conflict.',1;
   COMMIT TRANSACTION;
   SELECT N'idempotent_replay' AS Code,@oldRevision AS OccupationRevision;
   RETURN;
 END;
 IF @revision<>@ExpectedRevision
    THROW 51108,'Stale occupation revision.',1;

 -- A move can replace only an existing entity owned by the authenticated
 -- actor; a new client cannot steal any NPC or another player's placement.
 IF EXISTS(
   SELECT 1 FROM @Items AS p
   JOIN dbo.WorldMapSpatialReservations AS old WITH(UPDLOCK,HOLDLOCK)
     ON old.GameServerId=@GameServerId AND old.WorldId=@WorldId
    AND old.SurfaceId=@SurfaceId AND old.Kind=p.Kind AND old.EntityId=p.EntityId
   WHERE old.OwnerPlayerId IS NULL OR old.OwnerPlayerId<>@ActorPlayerId)
    THROW 51109,'Spatial move not authorized.',1;

 -- Conflict against EVERY other persisted footprint, including historical.
 IF EXISTS(
   SELECT 1 FROM @Items AS p
   JOIN dbo.WorldMapSpatialReservations AS old WITH(UPDLOCK,HOLDLOCK)
     ON old.GameServerId=@GameServerId AND old.WorldId=@WorldId
    AND old.SurfaceId=@SurfaceId
    AND NOT(old.Kind=p.Kind AND old.EntityId=p.EntityId)
   WHERE NOT(
     p.BoxRight+14<old.BoxLeft OR old.BoxRight+14<p.BoxLeft OR
     p.BoxBottom+14<old.BoxTop OR old.BoxBottom+14<p.BoxTop))
    THROW 51110,'Spatial footprint occupied by existing entity.',1;

 -- Also prohibit collisions INSIDE the same atomic request batch.
 IF EXISTS(
   SELECT 1 FROM @Items a JOIN @Items b ON
      (a.Kind<b.Kind OR (a.Kind=b.Kind AND a.EntityId<b.EntityId))
   WHERE NOT(
     a.BoxRight+14<b.BoxLeft OR b.BoxRight+14<a.BoxLeft OR
     a.BoxBottom+14<b.BoxTop OR b.BoxBottom+14<a.BoxTop))
    THROW 51111,'Spatial batch contains overlapping footprints.',1;

 -- A placement ID may never be reassigned to a different identity.
 IF EXISTS(
   SELECT 1 FROM @Items p
   JOIN dbo.WorldMapSpatialReservations old WITH(UPDLOCK,HOLDLOCK)
     ON old.GameServerId=@GameServerId AND old.WorldId=@WorldId
    AND old.SurfaceId=@SurfaceId AND old.PlacementId=p.PlacementId
   WHERE old.Kind<>p.Kind OR old.EntityId<>p.EntityId)
    THROW 51112,'Placement ID already owned by another entity.',1;

 DECLARE @nextRevision bigint=@revision+1;
 UPDATE dbo.WorldMapSpatialScopes
 SET OccupationRevision=@nextRevision,UpdatedAtUtc=SYSUTCDATETIME()
 WHERE GameServerId=@GameServerId AND WorldId=@WorldId AND
 SurfaceId=@SurfaceId AND OccupationRevision=@revision AND
 WritesEnabled=1 AND TerrainRevision=@TerrainRevision AND
 TerrainContentSha256=@TerrainContentSha256;
 IF @@ROWCOUNT<>1 THROW 51113,'Spatial scope changed during commit.',1;

 UPDATE old SET
   PlacementId=p.PlacementId,TerrainRevision=@TerrainRevision,
   OccupationRevision=@nextRevision,X=p.X,Y=p.Y,
   BoxLeft=p.BoxLeft,BoxTop=p.BoxTop,
   BoxRight=p.BoxRight,BoxBottom=p.BoxBottom,
   IsHistorical=0,UpdatedAtUtc=SYSUTCDATETIME()
 FROM dbo.WorldMapSpatialReservations old
 JOIN @Items p ON old.Kind=p.Kind AND old.EntityId=p.EntityId
 WHERE old.GameServerId=@GameServerId AND old.WorldId=@WorldId
   AND old.SurfaceId=@SurfaceId AND old.OwnerPlayerId=@ActorPlayerId;

 INSERT dbo.WorldMapSpatialReservations
  (GameServerId,WorldId,SurfaceId,Kind,EntityId,PlacementId,
   OwnerPlayerId,TerrainRevision,OccupationRevision,
   X,Y,BoxLeft,BoxTop,BoxRight,BoxBottom,IsHistorical)
 SELECT @GameServerId,@WorldId,@SurfaceId,p.Kind,p.EntityId,p.PlacementId,
   @ActorPlayerId,@TerrainRevision,@nextRevision,
   p.X,p.Y,p.BoxLeft,p.BoxTop,p.BoxRight,p.BoxBottom,0
 FROM @Items p WHERE NOT EXISTS (
   SELECT 1 FROM dbo.WorldMapSpatialReservations old
   WHERE old.GameServerId=@GameServerId AND old.WorldId=@WorldId
     AND old.SurfaceId=@SurfaceId AND old.Kind=p.Kind
     AND old.EntityId=p.EntityId
 );

 INSERT dbo.WorldMapSpatialReceipts
  (GameServerId,WorldId,SurfaceId,ActorPlayerId,ClientRequestId,
   PayloadHash,ResultJson,OccupationRevision)
 VALUES(@GameServerId,@WorldId,@SurfaceId,@ActorPlayerId,@RequestId,
   @PayloadHash,N'{"code":"committed"}',@nextRevision);
 COMMIT TRANSACTION;
 SELECT N'committed' AS Code,@nextRevision AS OccupationRevision;
END
GO
-- SECURITY: no GRANT EXECUTE here. DBA must grant only to a service
-- principal whose API validates reviewed terrain and server footprint specs.
