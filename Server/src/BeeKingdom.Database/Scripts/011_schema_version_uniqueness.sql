IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.SchemaVersion')
         AND is_unique = 1
         AND name IN (N'UQ_SchemaVersion_ScriptName', N'UX_SchemaVersion_ScriptName')
   )
BEGIN
    IF EXISTS
    (
        SELECT ScriptName
        FROM dbo.SchemaVersion
        GROUP BY ScriptName
        HAVING COUNT_BIG(*) > 1
    )
    BEGIN
        THROW 51057, 'Duplicate SchemaVersion rows must be reconciled before uniqueness can be enforced.', 1;
    END;

    CREATE UNIQUE INDEX UX_SchemaVersion_ScriptName
        ON dbo.SchemaVersion(ScriptName);
END
