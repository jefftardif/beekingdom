IF OBJECT_ID(N'dbo.ChatModerationReportReceipts',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatModerationReportReceipts
    (
        ReporterPlayerId uniqueidentifier NOT NULL,
        ClientRequestId nvarchar(128) NOT NULL,
        PayloadHash char(64) NOT NULL,
        ReportId uniqueidentifier NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        ExpiresAtUtc datetime2 NOT NULL CONSTRAINT DF_ChatModerationReportReceipts_ExpiresAtUtc DEFAULT DATEADD(day,30,SYSUTCDATETIME()),
        CONSTRAINT PK_ChatModerationReportReceipts PRIMARY KEY(ReporterPlayerId,ClientRequestId),
        CONSTRAINT FK_ChatModerationReportReceipts_Report FOREIGN KEY(ReportId) REFERENCES dbo.ChatModerationReports(ReportId)
    );
    CREATE INDEX IX_ChatModerationReportReceipts_ExpiresAtUtc ON dbo.ChatModerationReportReceipts(ExpiresAtUtc);
END
