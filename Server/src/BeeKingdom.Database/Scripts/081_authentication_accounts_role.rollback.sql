IF OBJECT_ID(N'dbo.AuthenticationAccounts', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'IX_AuthenticationAccounts_Role')
    BEGIN
        EXEC(N'DROP INDEX IX_AuthenticationAccounts_Role ON dbo.AuthenticationAccounts;');
    END

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND name = N'Role')
    BEGIN
        DECLARE @constraintName sysname;
        SELECT @constraintName = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') AND c.name = N'Role';

        IF @constraintName IS NOT NULL
        BEGIN
            EXEC(N'ALTER TABLE dbo.AuthenticationAccounts DROP CONSTRAINT [' + @constraintName + N'];');
        END

        EXEC(N'ALTER TABLE dbo.AuthenticationAccounts DROP COLUMN Role;');
    END
END
