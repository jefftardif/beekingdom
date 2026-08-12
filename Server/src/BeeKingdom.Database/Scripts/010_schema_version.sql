IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaVersion
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
        ScriptName nvarchar(256) NOT NULL CONSTRAINT UQ_SchemaVersion_ScriptName UNIQUE,
        AppliedAtUtc datetime2 NOT NULL CONSTRAINT DF_SchemaVersion_AppliedAtUtc DEFAULT SYSUTCDATETIME()
    );
END
