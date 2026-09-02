IF OBJECT_ID(N'dbo.Alliances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alliances
    (
        AllianceId uniqueidentifier NOT NULL CONSTRAINT PK_Alliances PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        Tag nvarchar(16) NOT NULL,
        Description nvarchar(1000) NOT NULL,
        Language nvarchar(16) NOT NULL,
        EmblemKey nvarchar(128) NOT NULL,
        JoinMode nvarchar(32) NOT NULL,
        Status nvarchar(32) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CreatedByPlayerId uniqueidentifier NOT NULL,
        LeaderPlayerId uniqueidentifier NOT NULL,
        MemberCount int NOT NULL,
        MaxMembers int NOT NULL,
        PublicSlug nvarchar(160) NOT NULL,
        ChatConversationId uniqueidentifier NULL,
        Revision bigint NOT NULL,
        DisbandedAtUtc datetime2 NULL
    );

    CREATE UNIQUE INDEX UX_Alliances_PublicSlug ON dbo.Alliances(PublicSlug) WHERE PublicSlug <> N'';
    CREATE INDEX IX_Alliances_Status_MemberCount ON dbo.Alliances(Status, MemberCount DESC);
END

IF OBJECT_ID(N'dbo.AllianceCreateReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceCreateReceipts
    (
        PlayerId uniqueidentifier NOT NULL,
        ClientRequestId nvarchar(256) NOT NULL,
        AllianceId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AllianceCreateReceipts PRIMARY KEY (PlayerId, ClientRequestId)
    );
END

IF OBJECT_ID(N'dbo.AllianceMemberships', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceMemberships
    (
        AllianceId uniqueidentifier NOT NULL,
        PlayerId uniqueidentifier NOT NULL,
        Role nvarchar(32) NOT NULL,
        JoinedAtUtc datetime2 NOT NULL,
        InvitedByPlayerId uniqueidentifier NULL,
        ApplicationId uniqueidentifier NULL,
        LastRoleChangedAtUtc datetime2 NOT NULL,
        RemovedAtUtc datetime2 NULL,
        Revision bigint NOT NULL,
        CONSTRAINT PK_AllianceMemberships PRIMARY KEY (AllianceId, PlayerId)
    );

    CREATE UNIQUE INDEX UX_AllianceMemberships_ActivePlayer ON dbo.AllianceMemberships(PlayerId) WHERE RemovedAtUtc IS NULL;
    CREATE INDEX IX_AllianceMemberships_AllianceActive ON dbo.AllianceMemberships(AllianceId, RemovedAtUtc);
END

IF OBJECT_ID(N'dbo.AllianceApplications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceApplications
    (
        ApplicationId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceApplications PRIMARY KEY,
        AllianceId uniqueidentifier NOT NULL,
        PlayerId uniqueidentifier NOT NULL,
        Status nvarchar(32) NOT NULL,
        SubmittedAtUtc datetime2 NOT NULL,
        RespondedAtUtc datetime2 NULL,
        RespondedByPlayerId uniqueidentifier NULL,
        Message nvarchar(500) NOT NULL
    );

    CREATE INDEX IX_AllianceApplications_AllianceStatus ON dbo.AllianceApplications(AllianceId, Status, SubmittedAtUtc);
    CREATE INDEX IX_AllianceApplications_AlliancePlayerStatus ON dbo.AllianceApplications(AllianceId, PlayerId, Status);
END

IF OBJECT_ID(N'dbo.AllianceApplicationReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceApplicationReceipts
    (
        PlayerId uniqueidentifier NOT NULL,
        ClientRequestId nvarchar(256) NOT NULL,
        ApplicationId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AllianceApplicationReceipts PRIMARY KEY (PlayerId, ClientRequestId)
    );
END

IF OBJECT_ID(N'dbo.AllianceInvitations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceInvitations
    (
        InvitationId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceInvitations PRIMARY KEY,
        AllianceId uniqueidentifier NOT NULL,
        InvitedPlayerId uniqueidentifier NOT NULL,
        InvitedByPlayerId uniqueidentifier NOT NULL,
        Status nvarchar(32) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        RespondedAtUtc datetime2 NULL
    );

    CREATE INDEX IX_AllianceInvitations_AlliancePlayerStatus ON dbo.AllianceInvitations(AllianceId, InvitedPlayerId, Status);
    CREATE INDEX IX_AllianceInvitations_PlayerStatus ON dbo.AllianceInvitations(InvitedPlayerId, Status, CreatedAtUtc DESC);
END

IF OBJECT_ID(N'dbo.AllianceInvitationReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceInvitationReceipts
    (
        PlayerId uniqueidentifier NOT NULL,
        ClientRequestId nvarchar(256) NOT NULL,
        InvitationId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AllianceInvitationReceipts PRIMARY KEY (PlayerId, ClientRequestId)
    );
END

IF OBJECT_ID(N'dbo.AllianceActivitySequences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceActivitySequences
    (
        AllianceId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceActivitySequences PRIMARY KEY,
        NextSequence bigint NOT NULL
    );
END

IF OBJECT_ID(N'dbo.AllianceActivityEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceActivityEvents
    (
        ActivityId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceActivityEvents PRIMARY KEY,
        AllianceId uniqueidentifier NOT NULL,
        Type nvarchar(64) NOT NULL,
        OccurredAtUtc datetime2 NOT NULL,
        ActorPlayerId uniqueidentifier NULL,
        TargetPlayerId uniqueidentifier NULL,
        RelatedAllianceId uniqueidentifier NULL,
        RelatedEntityId uniqueidentifier NULL,
        Visibility nvarchar(32) NOT NULL,
        PayloadJson nvarchar(max) NULL,
        Sequence bigint NOT NULL
    );

    CREATE UNIQUE INDEX UX_AllianceActivityEvents_AllianceSequence ON dbo.AllianceActivityEvents(AllianceId, Sequence);
    CREATE INDEX IX_AllianceActivityEvents_AllianceSequenceDesc ON dbo.AllianceActivityEvents(AllianceId, Sequence DESC);
END

IF OBJECT_ID(N'dbo.AllianceActivityDedupe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceActivityDedupe
    (
        DedupeKey nvarchar(450) NOT NULL CONSTRAINT PK_AllianceActivityDedupe PRIMARY KEY,
        ActivityId uniqueidentifier NOT NULL
    );
END

IF OBJECT_ID(N'dbo.AllianceDiplomaticRelations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceDiplomaticRelations
    (
        RelationId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceDiplomaticRelations PRIMARY KEY,
        AllianceIdA uniqueidentifier NOT NULL,
        AllianceIdB uniqueidentifier NOT NULL,
        RelationType nvarchar(32) NOT NULL,
        Status nvarchar(32) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        InitiatedByAllianceId uniqueidentifier NOT NULL,
        Revision bigint NOT NULL
    );

    CREATE UNIQUE INDEX UX_AllianceDiplomaticRelations_Pair ON dbo.AllianceDiplomaticRelations(AllianceIdA, AllianceIdB);
    CREATE INDEX IX_AllianceDiplomaticRelations_AllianceB ON dbo.AllianceDiplomaticRelations(AllianceIdB);
END

IF OBJECT_ID(N'dbo.AllianceDiplomacyProposalReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceDiplomacyProposalReceipts
    (
        PlayerId uniqueidentifier NOT NULL,
        ClientRequestId nvarchar(256) NOT NULL,
        RelationId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AllianceDiplomacyProposalReceipts PRIMARY KEY (PlayerId, ClientRequestId)
    );
END

IF OBJECT_ID(N'dbo.AllianceWars', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceWars
    (
        WarId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceWars PRIMARY KEY,
        AttackerAllianceId uniqueidentifier NOT NULL,
        DefenderAllianceId uniqueidentifier NOT NULL,
        Status nvarchar(32) NOT NULL,
        DeclaredAtUtc datetime2 NOT NULL,
        StartedAtUtc datetime2 NULL,
        EndedAtUtc datetime2 NULL,
        WinnerAllianceId uniqueidentifier NULL,
        Revision bigint NOT NULL
    );

    CREATE INDEX IX_AllianceWars_Attacker_Status ON dbo.AllianceWars(AttackerAllianceId, Status);
    CREATE INDEX IX_AllianceWars_Defender_Status ON dbo.AllianceWars(DefenderAllianceId, Status);
END

IF OBJECT_ID(N'dbo.AllianceWarDeclareReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceWarDeclareReceipts
    (
        PlayerId uniqueidentifier NOT NULL,
        ClientRequestId nvarchar(256) NOT NULL,
        WarId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AllianceWarDeclareReceipts PRIMARY KEY (PlayerId, ClientRequestId)
    );
END
