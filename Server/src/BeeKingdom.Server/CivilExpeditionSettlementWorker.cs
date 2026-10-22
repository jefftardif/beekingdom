using BeeKingdom.HiveOperations;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Server;

/// <summary>
/// Background settlement of server-owned civilian flights, even with no Unity
/// clients online. Disabled by default. Multiple API instances are safe because
/// SQL app-locks and the unique ledger serialize completion within one DB.
/// </summary>
public sealed class CivilExpeditionSettlementWorker(
    IHiveStateRepository hives,
    ICivilExpeditionUnitOfWork coordinator,
    BeeKingdom.HiveOperations.IServerClock clock,
    IOptions<CivilExpeditionOptions> settings,
    IOptions<FloralSiteOptions> floral,
    ILogger<CivilExpeditionSettlementWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (settings.Value.CanSettle(floral.Value.Enabled)
                && coordinator is ICivilExpeditionDueIndex dueIndex)
            {
                try
                {
                    var expeditions = new CivilExpeditionService(
                        hives, coordinator, clock, settings.Value, floral.Value);
                    CivilExpeditionDueBatch batch = await new CivilExpeditionDueProcessor(
                        dueIndex, expeditions, clock).ProcessAsync(100, stoppingToken);
                    if (batch.Settled > 0)
                        logger.LogInformation(
                            "Pollination: {Settled}/{Inspected} civil missions completed",
                            batch.Settled, batch.Inspected);
                    foreach (Guid pending in batch.RetryMissionIds)
                        logger.LogWarning(
                            "Pollination: mission {MissionId} remains queued for retry",
                            pending);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception error)
                {
                    logger.LogError(error,
                        "Pollination: due-work scan failed; retrying later");
                }
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
