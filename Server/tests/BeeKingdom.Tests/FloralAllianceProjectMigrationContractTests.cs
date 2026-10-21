using BeeKingdom.Database;

namespace BeeKingdom.Tests;

public sealed class FloralAllianceProjectMigrationContractTests
{
    [Test]
    public void P4_sql_enforces_unique_missions_and_distinct_project_members()
    {
        DatabaseScript migration = DatabaseCatalog.Migrations
            .Single(x => x.Name == "098_floral_alliance_projects.sql");
        Assert.That(migration.Sql,
            Does.Contain("CONSTRAINT PK_FloralAllianceProjectDeliveries PRIMARY KEY"));
        Assert.That(migration.Sql,
            Does.Contain("PRIMARY KEY(ProjectId, PlayerId)"));
        Assert.That(migration.Sql,
            Does.Contain("CONSTRAINT UX_FloralAllianceProjects_CreateKey"));
        Assert.That(migration.Sql,
            Does.Contain("UX_FloralAllianceProjects_OneActivePerSite"));
        Assert.That(migration.Sql,
            Does.Contain("dbo.FloralAllianceProgression"));
        Assert.That(migration.Sql, Does.Not.Contain("DROP TABLE"));
        Assert.That(migration.Sql, Does.Not.Contain("DELETE FROM"));
    }
}
