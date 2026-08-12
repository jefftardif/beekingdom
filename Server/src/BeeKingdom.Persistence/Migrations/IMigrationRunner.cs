namespace BeeKingdom.Persistence.Migrations;

public interface IMigrationRunner
{
    Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
    Task ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default);
}
