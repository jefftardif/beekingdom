IF OBJECT_ID(N'dbo.NewsArticles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NewsArticles
    (
        Slug nvarchar(200) NOT NULL CONSTRAINT PK_NewsArticles PRIMARY KEY,
        TitleEn nvarchar(400) NOT NULL,
        TitleFr nvarchar(400) NOT NULL,
        ExcerptEn nvarchar(1000) NOT NULL,
        ExcerptFr nvarchar(1000) NOT NULL,
        BodyEn nvarchar(max) NOT NULL,
        BodyFr nvarchar(max) NOT NULL,
        Status nvarchar(20) NOT NULL,
        PublishedAtUtc datetime2 NULL,
        CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_NewsArticles_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_NewsArticles_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedByAccountId uniqueidentifier NOT NULL
    );

    CREATE INDEX IX_NewsArticles_Status_PublishedAtUtc ON dbo.NewsArticles (Status, PublishedAtUtc DESC);
    CREATE INDEX IX_NewsArticles_UpdatedAtUtc ON dbo.NewsArticles (UpdatedAtUtc DESC);
END
