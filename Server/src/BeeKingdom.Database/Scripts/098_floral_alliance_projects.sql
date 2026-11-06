IF OBJECT_ID(N'dbo.FloralAllianceProgression', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FloralAllianceProgression
    (
        AllianceId uniqueidentifier NOT NULL
            CONSTRAINT PK_FloralAllianceProgression PRIMARY KEY,
        Revision bigint NOT NULL,
        Level int NOT NULL,
        Experience bigint NOT NULL,
        CompletedProjects bigint NOT NULL,
        CONSTRAINT CK_FloralAllianceProgression_Nonnegative
            CHECK(Revision >= 0 AND Level >= 1 AND Experience >= 0
                AND CompletedProjects >= 0)
    );
END

IF OBJECT_ID(N'dbo.FloralAllianceProjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FloralAllianceProjects
    (
        ProjectId uniqueidentifier NOT NULL
            CONSTRAINT PK_FloralAllianceProjects PRIMARY KEY,
        AllianceId uniqueidentifier NOT NULL,
        WorldId nvarchar(64) NOT NULL,
        SiteId nvarchar(64) NOT NULL,
        CreatedByPlayerId uniqueidentifier NOT NULL,
        CreateIdempotencyKey nvarchar(128) NOT NULL,
        CreatePayloadHash char(64) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        Revision bigint NOT NULL,
        GoalPoints bigint NOT NULL,
        RequiredDistinctMembers int NOT NULL,
        MinimumPointsPerMember bigint NOT NULL,
        Points bigint NOT NULL,
        QualifiedMembers int NOT NULL,
        Status nvarchar(16) NOT NULL,
        CompletedAtUtc datetime2 NULL,
        CONSTRAINT CK_FloralAllianceProjects_Valid
            CHECK(Revision >= 0 AND GoalPoints >= 2
                AND RequiredDistinctMembers >= 2
                AND MinimumPointsPerMember >= 1 AND Points >= 0
                AND QualifiedMembers >= 0
                AND Status IN (N'Active',N'Completed')
                AND (Status <> N'Completed' OR CompletedAtUtc IS NOT NULL)),
        CONSTRAINT UX_FloralAllianceProjects_CreateKey
            UNIQUE(AllianceId, CreateIdempotencyKey)
    );
    CREATE UNIQUE INDEX UX_FloralAllianceProjects_OneActivePerSite
        ON dbo.FloralAllianceProjects(AllianceId, WorldId, SiteId)
        WHERE Status = N'Active';
    CREATE INDEX IX_FloralAllianceProjects_Alliance
        ON dbo.FloralAllianceProjects(AllianceId, CreatedAtUtc DESC);
END

IF OBJECT_ID(N'dbo.FloralAllianceProjectMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FloralAllianceProjectMembers
    (
        ProjectId uniqueidentifier NOT NULL,
        PlayerId uniqueidentifier NOT NULL,
        Points bigint NOT NULL,
        FirstContributionAtUtc datetime2 NOT NULL,
        LastContributionAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_FloralAllianceProjectMembers
            PRIMARY KEY(ProjectId, PlayerId),
        CONSTRAINT CK_FloralAllianceProjectMembers_Points CHECK(Points > 0)
    );
END

IF OBJECT_ID(N'dbo.FloralAllianceProjectDeliveries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FloralAllianceProjectDeliveries
    (
        MissionId uniqueidentifier NOT NULL
            CONSTRAINT PK_FloralAllianceProjectDeliveries PRIMARY KEY,
        ProjectId uniqueidentifier NOT NULL,
        PlayerId uniqueidentifier NOT NULL,
        Points bigint NOT NULL,
        DeliveredAtUtc datetime2 NOT NULL,
        CONSTRAINT CK_FloralAllianceProjectDeliveries_Points CHECK(Points > 0)
    );
    CREATE INDEX IX_FloralAllianceProjectDeliveries_Project
        ON dbo.FloralAllianceProjectDeliveries(ProjectId, DeliveredAtUtc);
END