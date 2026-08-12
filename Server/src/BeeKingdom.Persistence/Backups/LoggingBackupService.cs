using Microsoft.Extensions.Logging;

namespace BeeKingdom.Persistence.Backups;

public sealed class LoggingBackupService : IBackupService
{
    private readonly ILogger<LoggingBackupService> logger;

    public LoggingBackupService(ILogger<LoggingBackupService> logger)
    {
        this.logger = logger;
    }

    public Task RequestBackupAsync(string reason, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Backup requested for BeeKingdom database. Reason: {Reason}", reason);
        return Task.CompletedTask;
    }
}
