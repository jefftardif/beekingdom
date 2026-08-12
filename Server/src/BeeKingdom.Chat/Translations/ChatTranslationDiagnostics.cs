using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace BeeKingdom.Chat.Translations;

public sealed class ChatTranslationDiagnostics(ILogger<ChatTranslationDiagnostics> logger)
{
    private static readonly Meter Meter = new("BeeKingdom.Chat.Translations", "1.0");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("chat.translation.requests");
    private static readonly Histogram<double> Latency = Meter.CreateHistogram<double>("chat.translation.latency.ms");

    public Stopwatch Start() => Stopwatch.StartNew();

    public void Complete(string outcome, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        TagList tags = new() { { "outcome", outcome } };
        Requests.Add(1, tags);
        Latency.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
        logger.LogInformation("Chat translation completed with outcome {Outcome} in {LatencyMs} ms.", outcome, stopwatch.Elapsed.TotalMilliseconds);
    }
}
