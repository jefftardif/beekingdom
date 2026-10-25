-- BeeKingdom P19 - SELECT-only SQL preflight.
-- Execute with an authorized READ-ONLY identity connected to database BeeKingdom.
-- No INSERT/UPDATE/DELETE/DDL; outputs aggregate counts, never player data.
SET NOCOUNT ON;
IF DB_NAME() <> N'BeeKingdom'
    THROW 51001, 'P19_SQL_WRONG_DATABASE: connect explicitly to BeeKingdom', 1;

SELECT item_name, status
FROM (VALUES
 (N'FloralSiteStates', OBJECT_ID(N'dbo.FloralSiteStates', N'U')),
 (N'FloralCivilContributions', OBJECT_ID(N'dbo.FloralCivilContributions', N'U')),
 (N'FloralCivilActiveMissions', OBJECT_ID(N'dbo.FloralCivilActiveMissions', N'U')),
 (N'FloralAllianceProjects', OBJECT_ID(N'dbo.FloralAllianceProjects', N'U')),
 (N'FloralAllianceProjectMembers', OBJECT_ID(N'dbo.FloralAllianceProjectMembers', N'U')),
 (N'FloralAllianceProjectDeliveries', OBJECT_ID(N'dbo.FloralAllianceProjectDeliveries', N'U')),
 (N'FloralAllianceProgression', OBJECT_ID(N'dbo.FloralAllianceProgression', N'U'))
) AS t(item_name, object_id)
CROSS APPLY (SELECT IIF(object_id IS NULL, N'BLOCKED', N'PASS') AS status) AS p
ORDER BY item_name;

SELECT N'IX_FloralCivilActiveMissions_Due' AS item_name,
    IIF(EXISTS(SELECT 1 FROM sys.indexes
      WHERE object_id=OBJECT_ID(N'dbo.FloralCivilActiveMissions')
        AND name=N'IX_FloralCivilActiveMissions_Due'), N'PASS', N'BLOCKED') AS status
UNION ALL
SELECT N'UX_FloralAllianceProjects_OneActivePerSite',
    IIF(EXISTS(SELECT 1 FROM sys.indexes
      WHERE object_id=OBJECT_ID(N'dbo.FloralAllianceProjects')
        AND name=N'UX_FloralAllianceProjects_OneActivePerSite'
        AND is_unique=1), N'PASS', N'BLOCKED');

IF OBJECT_ID(N'dbo.FloralCivilActiveMissions', N'U') IS NOT NULL
    EXEC sys.sp_executesql N'SELECT COUNT_BIG(*) AS due_missions
      FROM dbo.FloralCivilActiveMissions
      WHERE ReturnsAtUtc <= SYSUTCDATETIME()';
ELSE
    SELECT CAST(NULL AS bigint) AS due_missions;

IF OBJECT_ID(N'dbo.FloralCivilContributions', N'U') IS NOT NULL
    EXEC sys.sp_executesql N'SELECT COUNT_BIG(*) AS settled_contribution_rows
      FROM dbo.FloralCivilContributions';
ELSE
    SELECT CAST(NULL AS bigint) AS settled_contribution_rows;
