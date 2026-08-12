using BeeKingdom.Database;
using Microsoft.Extensions.Logging;

namespace BeeKingdom.Persistence.Migrations;

public sealed class ScriptedMigrationRunner : IMigrationRunner
{
    private readonly ILogger<ScriptedMigrationRunner> logger;
    private readonly MigrationDiagnostics diagnostics;

    public ScriptedMigrationRunner(ILogger<ScriptedMigrationRunner> logger, MigrationDiagnostics diagnostics)
    {
        this.logger = logger;
        this.diagnostics = diagnostics;
    }

    public Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        diagnostics.RecordPendingCheck();
        IReadOnlyList<string> scripts = DatabaseCatalog.Migrations.Select(script => script.Name).ToArray();
        return Task.FromResult(scripts);
    }

    public Task ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        diagnostics.RecordApplyAttempt();
        foreach (DatabaseScript script in DatabaseCatalog.Migrations)
        {
            logger.LogInformation("Migration script registered: {ScriptName}", script.Name);
            diagnostics.RecordScriptApplied(script.Name);
        }

        return Task.CompletedTask;
    }
}
