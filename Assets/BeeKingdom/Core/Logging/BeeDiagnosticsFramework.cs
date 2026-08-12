using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Logging
{
    public readonly struct BeeDiagnosticEvent
    {
        public long Sequence { get; }
        public BeeLogLevel Level { get; }
        public string Category { get; }
        public string Message { get; }
        public double SimulationSeconds { get; }

        public BeeDiagnosticEvent(long sequence, BeeLogLevel level, string category, string message, double simulationSeconds)
        {
            Sequence = sequence;
            Level = level;
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            Message = message ?? string.Empty;
            SimulationSeconds = simulationSeconds;
        }
    }

    public sealed class BeeDiagnosticsSnapshot
    {
        public int Version { get; }
        public IReadOnlyList<BeeDiagnosticEvent> Events { get; }
        public int DroppedEvents { get; }

        public BeeDiagnosticsSnapshot(int version, IReadOnlyList<BeeDiagnosticEvent> events, int droppedEvents)
        {
            Version = version;
            Events = events ?? Array.Empty<BeeDiagnosticEvent>();
            DroppedEvents = droppedEvents;
        }
    }

    public sealed class BeeDiagnosticsCounters
    {
        public int DebugCount { get; private set; }
        public int InfoCount { get; private set; }
        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }
        public int DroppedEvents { get; private set; }

        public void Record(BeeLogLevel level)
        {
            if (level == BeeLogLevel.Debug) DebugCount++;
            if (level == BeeLogLevel.Info) InfoCount++;
            if (level == BeeLogLevel.Warning) WarningCount++;
            if (level == BeeLogLevel.Error) ErrorCount++;
        }

        public void RecordDropped() => DroppedEvents++;
    }

    public sealed class BeeDiagnosticsManager : IBeeLogger
    {
        private const int SnapshotVersion = 1;

        private readonly List<BeeDiagnosticEvent> events = new List<BeeDiagnosticEvent>();
        private readonly HashSet<string> mutedCategories = new HashSet<string>();
        private readonly int maxEvents;
        private long sequence;

        public BeeLogLevel MinimumLevel { get; set; }
        public BeeDiagnosticsCounters Counters { get; } = new BeeDiagnosticsCounters();
        public int EventCount => events.Count;

        public BeeDiagnosticsManager(BeeLogLevel minimumLevel = BeeLogLevel.Info, int maxEvents = 256)
        {
            MinimumLevel = minimumLevel;
            this.maxEvents = maxEvents <= 0 ? 1 : maxEvents;
        }

        public void Log(BeeLogLevel level, string message)
        {
            Record(level, "General", message, 0d);
        }

        public bool Record(BeeLogLevel level, string category, string message, double simulationSeconds)
        {
            if (level < MinimumLevel || mutedCategories.Contains(NormalizeCategory(category)))
            {
                return false;
            }

            if (events.Count >= maxEvents)
            {
                events.RemoveAt(0);
                Counters.RecordDropped();
            }

            BeeDiagnosticEvent diagnosticEvent = new BeeDiagnosticEvent(++sequence, level, category, message, simulationSeconds);
            events.Add(diagnosticEvent);
            Counters.Record(level);
            return true;
        }

        public void SetCategoryMuted(string category, bool muted)
        {
            string normalized = NormalizeCategory(category);
            if (muted) mutedCategories.Add(normalized);
            else mutedCategories.Remove(normalized);
        }

        public BeeDiagnosticsSnapshot CreateSnapshot()
        {
            return new BeeDiagnosticsSnapshot(SnapshotVersion, new List<BeeDiagnosticEvent>(events), Counters.DroppedEvents);
        }

        public void Clear()
        {
            events.Clear();
        }

        private static string NormalizeCategory(string category)
        {
            return string.IsNullOrWhiteSpace(category) ? "General" : category;
        }
    }
}
