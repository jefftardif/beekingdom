-- QA ONLY. Never execute in any existing BeeKingdom production database.
-- Create a disposable database named BeeKingdomSpatialSandbox_YYYYMMDD first;
-- then run 100, 101 and finally this file using sqlcmd -b -C -E -d.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE N'BeeKingdomSpatialSandbox[_]%'
    THROW 51201,'Refusing spatial QA against non-disposable database.',1;
IF OBJECT_ID(N'dbo.WorldMapSpatialScopes',N'U') IS NULL OR
   OBJECT_ID(N'dbo.WorldMapSpatialReservations',N'U') IS NULL OR
   OBJECT_ID(N'dbo.WorldMapSpatialReceipts',N'U') IS NULL OR
   OBJECT_ID(N'dbo.WorldMapCommitSpatialBatch',N'P') IS NULL OR
   TYPE_ID(N'dbo.WorldMapSpatialPlacementBatch') IS NULL
    THROW 51202,'QA schema/procedure missing.',1;
DECLARE @gs uniqueidentifier='aaaaaaaa-1111-2222-3333-444444444444',
        @actor uniqueidentifier='bbbbbbbb-1111-2222-3333-444444444444',
        @other uniqueidentifier='cccccccc-1111-2222-3333-444444444444',
        @world nvarchar(128)=N'QA-WORLD',
        @surface nvarchar(128)=N'QA-WAVE6',
        @terrain nvarchar(64)=N'qa-synthetic-reviewed-v1',
        @sha char(64)=REPLICATE('A',64),
        @payload char(64)=REPLICATE('1',64);
IF EXISTS(SELECT 1 FROM dbo.WorldMapSpatialScopes WHERE GameServerId=@gs)
    THROW 51203,'QA world already exists; use a fresh sandbox.',1;
INSERT dbo.WorldMapSpatialScopes
(GameServerId,WorldId,SurfaceId,TerrainRevision,TerrainContentSha256,OccupationRevision,WritesEnabled)
VALUES(@gs,@world,@surface,@terrain,@sha,1,1);

DECLARE @first dbo.WorldMapSpatialPlacementBatch;
INSERT @first(Kind,EntityId,PlacementId,X,Y,BoxLeft,BoxTop,BoxRight,BoxBottom)
VALUES(N'hive',N'JEFF',N'qa-hive-1',100,100,90,90,110,110);
EXEC dbo.WorldMapCommitSpatialBatch @gs,@world,@surface,@actor,N'qa-first',@payload,
 1,@terrain,@sha,@first;
IF (SELECT OccupationRevision FROM dbo.WorldMapSpatialScopes
    WHERE GameServerId=@gs AND WorldId=@world AND SurfaceId=@surface)<>2
 OR (SELECT COUNT(*) FROM dbo.WorldMapSpatialReservations WHERE GameServerId=@gs)<>1
    THROW 51204,'First atomic placement did not commit once.',1;
-- Exact replay must not advance the revision or add another row.
EXEC dbo.WorldMapCommitSpatialBatch @gs,@world,@surface,@actor,N'qa-first',@payload,
 1,@terrain,@sha,@first;
IF (SELECT OccupationRevision FROM dbo.WorldMapSpatialScopes WHERE GameServerId=@gs)<>2
 OR (SELECT COUNT(*) FROM dbo.WorldMapSpatialReservations WHERE GameServerId=@gs)<>1
    THROW 51205,'Idempotent replay unexpectedly mutated world.',1;

DECLARE @near dbo.WorldMapSpatialPlacementBatch;
INSERT @near(Kind,EntityId,PlacementId,X,Y,BoxLeft,BoxTop,BoxRight,BoxBottom)
VALUES(N'bestiary',N'SPIDER',N'qa-spider-too-near',111,112,103,104,119,120);
BEGIN TRY
 EXEC dbo.WorldMapCommitSpatialBatch @gs,@world,@surface,@actor,N'qa-near',@payload,
 2,@terrain,@sha,@near;
 THROW 51206,'Expected collision rejection did not occur.',1;
END TRY BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 IF ERROR_NUMBER()<>51110 THROW;
END CATCH;
IF (SELECT OccupationRevision FROM dbo.WorldMapSpatialScopes WHERE GameServerId=@gs)<>2
 OR (SELECT COUNT(*) FROM dbo.WorldMapSpatialReservations WHERE GameServerId=@gs)<>1
    THROW 51207,'Rejected collision changed persisted world.',1;

-- A different principal must not steal the same hive identity.
DECLARE @stolen dbo.WorldMapSpatialPlacementBatch;
INSERT @stolen(Kind,EntityId,PlacementId,X,Y,BoxLeft,BoxTop,BoxRight,BoxBottom)
VALUES(N'hive',N'JEFF',N'qa-stolen-hive',500,500,490,490,510,510);
BEGIN TRY
 EXEC dbo.WorldMapCommitSpatialBatch @gs,@world,@surface,@other,N'qa-steal',@payload,
 2,@terrain,@sha,@stolen;
 THROW 51208,'Expected unauthorized move rejection did not occur.',1;
END TRY BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 IF ERROR_NUMBER()<>51109 THROW;
END CATCH;

-- A valid far-away enemy can be committed; old hive remains unchanged.
DECLARE @far dbo.WorldMapSpatialPlacementBatch;
INSERT @far(Kind,EntityId,PlacementId,X,Y,BoxLeft,BoxTop,BoxRight,BoxBottom)
VALUES(N'bestiary',N'SPIDER',N'qa-spider-safe',500,500,490,490,510,510);
EXEC dbo.WorldMapCommitSpatialBatch @gs,@world,@surface,@actor,N'qa-far',@payload,
 2,@terrain,@sha,@far;
IF (SELECT OccupationRevision FROM dbo.WorldMapSpatialScopes WHERE GameServerId=@gs)<>3
 OR (SELECT COUNT(*) FROM dbo.WorldMapSpatialReservations WHERE GameServerId=@gs)<>2
 OR (SELECT X FROM dbo.WorldMapSpatialReservations WHERE GameServerId=@gs AND EntityId=N'JEFF')<>100
    THROW 51209,'Successful second reservation damaged existing hive.',1;

-- A stale client cannot mutate a new revision.
DECLARE @late dbo.WorldMapSpatialPlacementBatch;
INSERT @late(Kind,EntityId,PlacementId,X,Y,BoxLeft,BoxTop,BoxRight,BoxBottom)
VALUES(N'resource',N'POLLEN',N'qa-stale',700,700,690,690,710,710);
BEGIN TRY
 EXEC dbo.WorldMapCommitSpatialBatch @gs,@world,@surface,@actor,N'qa-stale',@payload,
 2,@terrain,@sha,@late;
 THROW 51210,'Expected stale revision rejection did not occur.',1;
END TRY BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 IF ERROR_NUMBER()<>51108 THROW;
END CATCH;
IF (SELECT OccupationRevision FROM dbo.WorldMapSpatialScopes WHERE GameServerId=@gs)<>3
    THROW 51211,'Stale operation advanced occupation revision.',1;
SELECT N'BK_SPATIAL_SQL_SANDBOX_PASS' AS Result,
 (SELECT COUNT(*) FROM dbo.WorldMapSpatialReservations WHERE GameServerId=@gs) AS Reservations,
 (SELECT OccupationRevision FROM dbo.WorldMapSpatialScopes WHERE GameServerId=@gs) AS Revision;
