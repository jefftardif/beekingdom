IF OBJECT_ID(N'dbo.AllianceResearch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceResearch
    (
        AllianceId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceResearch PRIMARY KEY,
        ModelVersion int NOT NULL,
        Revision bigint NOT NULL,
        StateJson nvarchar(max) NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_AllianceResearch_UpdatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END
