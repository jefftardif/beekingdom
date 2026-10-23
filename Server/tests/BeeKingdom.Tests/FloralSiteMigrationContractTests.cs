using BeeKingdom.Database;

namespace BeeKingdom.Tests;

public sealed class FloralSiteMigrationContractTests
{
    [Test]
    public void Global_state_schema_is_additive_and_identity_is_unique()
    {
        DatabaseScript migration = DatabaseCatalog.Migrations
            .Single(script => script.Name == "096_floral_site_states.sql");
        Assert.That(migration.Sql,
            Does.Contain("IF OBJECT_ID(N'dbo.FloralSiteStates', N'U') IS NULL"));
        Assert.That(migration.Sql,
            Does.Contain("CONSTRAINT PK_FloralSiteStates PRIMARY KEY (WorldId, SiteId)"));
        Assert.That(migration.Sql, Does.Contain("StateJson nvarchar(max) NOT NULL"));
        Assert.That(migration.Sql, Does.Contain("Revision bigint NOT NULL"));
        Assert.That(migration.Sql, Does.Not.Contain("DROP TABLE"));
        Assert.That(migration.Sql, Does.Not.Contain("DELETE FROM"));
    }
[Test]
    public void Civil_expedition_schema_guarantees_unique_payout_and_due_index()
    {
        DatabaseScript migration = DatabaseCatalog.Migrations
            .Single(script => script.Name == "097_floral_civil_contributions.sql");
        Assert.That(migration.Sql, Does.Contain("PK_FloralCivilContributions PRIMARY KEY"));
        Assert.That(migration.Sql, Does.Contain("CONSTRAINT CK_FloralCivilContributions_Balances"));
        Assert.That(migration.Sql, Does.Contain("PK_FloralCivilActiveMissions PRIMARY KEY"));
        Assert.That(migration.Sql, Does.Contain("IX_FloralCivilActiveMissions_Due"));
        Assert.That(migration.Sql, Does.Not.Contain("DROP TABLE"));
        Assert.That(migration.Sql, Does.Not.Contain("DELETE FROM"));
    }
}
