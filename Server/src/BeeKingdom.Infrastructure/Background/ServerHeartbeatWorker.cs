using BeeKingdom.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Infrastructure.Background;

public sealed class ServerHeartbeatWorker : BackgroundService
{
    private readonly ILogger<ServerHeartbeatWorker> logger;
    private readonly BeeKingdomServerOptions options;

    public ServerHeartbeatWorker(ILogger<ServerHeartbeatWorker> logger, IOptions<BeeKingdomServerOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.EnableBackgroundWorkers)
        {
            logger.LogInformation("Bee Kingdom background workers are disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Bee Kingdom server heartbeat.");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
