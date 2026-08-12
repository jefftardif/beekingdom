using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum ConstructionHealthState { Excellent, Good, Normal, Warning, Critical }
    public enum ConstructionBottleneckType { MissingResources, MissingBuilders, BlockingDependencies, Congestion, BadPlacement, StructuralProblem, ReservationConflict }

    public sealed class ConstructionStatistics
    {
        public int ConstructionCount { get; }
        public double AverageDuration { get; }
        public double AverageProgress { get; }
        public int WaitingResources { get; }
        public int AvailableBuilders { get; }
        public int BusyBuilders { get; }
        public double WaitingTime { get; }
        public int Congestions { get; }
        public int Interruptions { get; }
        public double Efficiency { get; }

        public ConstructionStatistics(int constructionCount, double averageDuration, double averageProgress, int waitingResources, int availableBuilders, int busyBuilders, double waitingTime, int congestions, int interruptions, double efficiency)
        {
            ConstructionCount = constructionCount;
            AverageDuration = averageDuration;
            AverageProgress = averageProgress;
            WaitingResources = waitingResources;
            AvailableBuilders = availableBuilders;
            BusyBuilders = busyBuilders;
            WaitingTime = waitingTime;
            Congestions = congestions;
            Interruptions = interruptions;
            Efficiency = efficiency;
        }
    }

    public sealed class ConstructionDiagnosticReport
    {
        public string ReportId { get; }
        public ConstructionStatistics Statistics { get; }
        public ConstructionHealthState Health { get; }
        public IReadOnlyList<ConstructionBottleneckType> Bottlenecks { get; }

        public ConstructionDiagnosticReport(string reportId, ConstructionStatistics statistics, ConstructionHealthState health, IReadOnlyList<ConstructionBottleneckType> bottlenecks)
        {
            ReportId = reportId ?? string.Empty;
            Statistics = statistics;
            Health = health;
            Bottlenecks = bottlenecks ?? Array.Empty<ConstructionBottleneckType>();
        }
    }

    public sealed class ConstructionSnapshot
    {
        public int Version { get; }
        public ConstructionDiagnosticReport Report { get; }

        public ConstructionSnapshot(int version, ConstructionDiagnosticReport report)
        {
            Version = version;
            Report = report;
        }
    }

    public sealed class ConstructionHealthAnalyzer
    {
        public ConstructionHealthState QueryConstructionHealth(ConstructionStatistics statistics)
        {
            if (statistics.Efficiency >= 0.9d && statistics.Congestions == 0) return ConstructionHealthState.Excellent;
            if (statistics.Efficiency >= 0.75d) return ConstructionHealthState.Good;
            if (statistics.Efficiency >= 0.5d) return ConstructionHealthState.Normal;
            if (statistics.Efficiency >= 0.25d) return ConstructionHealthState.Warning;
            return ConstructionHealthState.Critical;
        }
    }

    public sealed class ConstructionBottleneckDetector
    {
        public IReadOnlyList<ConstructionBottleneckType> DetectBottlenecks(ConstructionStatistics statistics)
        {
            List<ConstructionBottleneckType> result = new List<ConstructionBottleneckType>();
            if (statistics.WaitingResources > 0) result.Add(ConstructionBottleneckType.MissingResources);
            if (statistics.AvailableBuilders <= 0 && statistics.ConstructionCount > 0) result.Add(ConstructionBottleneckType.MissingBuilders);
            if (statistics.Congestions > 0) result.Add(ConstructionBottleneckType.Congestion);
            if (statistics.Interruptions > 0) result.Add(ConstructionBottleneckType.ReservationConflict);
            return result;
        }
    }

    public sealed class ConstructionDiagnosticsManager
    {
        private const int SnapshotVersion = 1;
        private readonly ConstructionHealthAnalyzer healthAnalyzer = new ConstructionHealthAnalyzer();
        private readonly ConstructionBottleneckDetector bottleneckDetector = new ConstructionBottleneckDetector();
        private readonly IEventBus eventBus;
        private ConstructionDiagnosticReport lastReport;
        private long counter;

        public ConstructionDiagnosticsManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public ConstructionDiagnosticReport AnalyzeConstruction(ConstructionStatistics statistics)
        {
            ConstructionHealthState previous = lastReport?.Health ?? ConstructionHealthState.Normal;
            ConstructionHealthState health = healthAnalyzer.QueryConstructionHealth(statistics);
            IReadOnlyList<ConstructionBottleneckType> bottlenecks = bottleneckDetector.DetectBottlenecks(statistics);
            lastReport = new ConstructionDiagnosticReport("construction-report-" + (++counter), statistics, health, bottlenecks);
            eventBus?.Publish(new StatisticsUpdated(statistics.ConstructionCount));
            if (previous != health) eventBus?.Publish(new ConstructionHealthChanged(health));
            if (bottlenecks.Count > 0) eventBus?.Publish(new ConstructionBottleneckDetected(bottlenecks[0]));
            return lastReport;
        }

        public ConstructionDiagnosticReport GenerateDiagnostics(ConstructionStatistics statistics)
        {
            ConstructionDiagnosticReport report = AnalyzeConstruction(statistics);
            eventBus?.Publish(new DiagnosticGenerated(report.ReportId));
            return report;
        }

        public ConstructionStatistics QueryStatistics() => lastReport?.Statistics;
        public ConstructionHealthState QueryConstructionHealth() => lastReport?.Health ?? ConstructionHealthState.Normal;
        public IReadOnlyList<ConstructionBottleneckType> DetectBottlenecks() => lastReport?.Bottlenecks ?? Array.Empty<ConstructionBottleneckType>();
        public ConstructionSnapshot GenerateSnapshot() => new ConstructionSnapshot(SnapshotVersion, lastReport);
    }

    public readonly struct ConstructionHealthChanged : IGameplayEvent, IBuildingEvent { public ConstructionHealthState Health { get; } public ConstructionHealthChanged(ConstructionHealthState health) { Health = health; } }
    public readonly struct ConstructionBottleneckDetected : IGameplayEvent, IBuildingEvent { public ConstructionBottleneckType Type { get; } public ConstructionBottleneckDetected(ConstructionBottleneckType type) { Type = type; } }
    public readonly struct DiagnosticGenerated : IGameplayEvent, IBuildingEvent { public string ReportId { get; } public DiagnosticGenerated(string reportId) { ReportId = reportId; } }
    public readonly struct StatisticsUpdated : IGameplayEvent, IBuildingEvent { public int ConstructionCount { get; } public StatisticsUpdated(int constructionCount) { ConstructionCount = constructionCount; } }
}
