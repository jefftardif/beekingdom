namespace BeeKingdom.Persistence.Backups;

public interface IBackupService
{
    Task RequestBackupAsync(string reason, CancellationToken cancellationToken = default);
}
