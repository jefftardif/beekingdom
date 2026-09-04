-- M045-CL: Alliance Help Core Alpha. Two tables:
--   dbo.AllianceHelpRequests      - one row per help request (bound to the requester's Alliance,
--                                    the real operation category/targetId, and the balance snapshot
--                                    captured at creation - never a second timer).
--   dbo.AllianceHelpContributions - one row per helper. PRIMARY KEY (HelpRequestId, HelperPlayerId)
--                                    is the final backstop against a helper contributing twice, on
--                                    top of the application-level check in AllianceHelpService.
-- NOT executed against production by this mission - see Docs/AI/Missions/M045-CL-Alliance-Help-Core-Alpha.md
-- for why this migration is needed and what it creates, per the mission's explicit instruction to
-- stop before applying a new schema without CEO authorization.

IF OBJECT_ID(N'dbo.AllianceHelpRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceHelpRequests
    (
        HelpRequestId uniqueidentifier NOT NULL CONSTRAINT PK_AllianceHelpRequests PRIMARY KEY,
        AllianceId uniqueidentifier NOT NULL,
        RequestingPlayerId uniqueidentifier NOT NULL,
        RequestingHiveId uniqueidentifier NOT NULL,
        OperationCategory nvarchar(32) NOT NULL,
        OperationTargetId nvarchar(128) NOT NULL,
        OperationId uniqueidentifier NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        Status nvarchar(16) NOT NULL,
        OriginalDurationSeconds bigint NOT NULL,
        HelpCount int NOT NULL,
        MaxHelpCount int NOT NULL,
        Revision bigint NOT NULL,
        ClientRequestId nvarchar(256) NOT NULL
    );

    -- Enforces "no repeated request button for the same active operation" at the DB level, not just
    -- in application code (invariant checked first in AllianceHelpService, this is the backstop
    -- against a concurrent double-create race). Filtered on Status so a Completed/Cancelled/Expired
    -- request never blocks a brand new one for the same operation later.
    CREATE UNIQUE INDEX UX_AllianceHelpRequests_Player_Operation_Open
        ON dbo.AllianceHelpRequests(RequestingPlayerId, OperationCategory, OperationTargetId)
        WHERE Status = N'Open';

    -- Alliance Center "Aides" list: every open request for the caller's alliance, oldest first.
    CREATE INDEX IX_AllianceHelpRequests_Alliance_Status ON dbo.AllianceHelpRequests(AllianceId, Status, CreatedAtUtc);
END

IF OBJECT_ID(N'dbo.AllianceHelpContributions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllianceHelpContributions
    (
        HelpRequestId uniqueidentifier NOT NULL,
        HelperPlayerId uniqueidentifier NOT NULL,
        HelpedAtUtc datetime2 NOT NULL,
        DurationReductionSeconds bigint NOT NULL,
        ClientRequestId nvarchar(256) NOT NULL,
        CONSTRAINT PK_AllianceHelpContributions PRIMARY KEY (HelpRequestId, HelperPlayerId),
        CONSTRAINT FK_AllianceHelpContributions_Request FOREIGN KEY (HelpRequestId)
            REFERENCES dbo.AllianceHelpRequests(HelpRequestId)
    );
END
