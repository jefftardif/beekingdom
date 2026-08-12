using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum MetricDomain { Population, Construction, Production, Resources, Logistics, Movement, Combat, Health, Fatigue, AI, Communication, Optimization }
    public enum MetricAggregationPeriod { Tick, Minute, Hour, Day, Season, ColonyLifetime }
    public enum TrendDirection { Stable, Rising, Falling }
    public enum ForecastDomain { Population, Resources, Construction, Food, Honey, Wax, Defense, Disease, ColonyGrowth, Logistics, Economy }
    public enum ForecastScenarioType { Current, Defensive, Economic, Expansion, Custom }
    public enum ColonyRiskType { Starvation, Overpopulation, BuilderShortage, FoodShortage, Congestion, ResourceShortage, ProductivityDrop, ExcessiveMortality }
    public enum ColonyEventType { Biological, Colony, Construction, Resource, Seasonal, Weather, Emergency, World, Player, Alliance }
    public enum ColonyEventState { Scheduled, Pending, Triggered, Active, Resolved, Archived, Cancelled, Failed, Expired }
    public enum ColonyEventPriority { Background, Normal, Important, Critical, Emergency }
    public enum AchievementType { ColonyGrowth, Population, Construction, Production, Combat, Exploration, Economy, Research, Seasons, World, Hidden, Story }
    public enum AchievementRewardType { Prestige, CosmeticUnlock, Badge, Title, Decoration, GameplayBonus, ResearchPoint, CollectionEntry }
    public enum ProgressionLevel { Settlement, YoungColony, GrowingColony, MatureColony, AdvancedColony, GreatColony, LegendaryColony }
    public enum PrestigeLevel { UnknownColony, RecognizedColony, RenownedColony, NobleColony, RoyalColony, LegendaryColony, EternalColony }
    public enum ScenarioType { Tutorial, Campaign, Story, Sandbox, Challenge, Survival, TimedChallenge, SeasonalEvent, CommunityEvent, CustomScenario }
    public enum ScenarioState { Locked, Available, Active, Paused, Completed, Failed, Archived }
    public enum SandboxMode { FreeBuild, InfiniteResources, InfinitePopulation, InstantConstruction, InstantResearch, AIObservation, DebugMode, PerformanceBenchmark, ReplayValidation, DeterministicTest }
    public enum SandboxState { Created, Running, Paused, Completed, Reset }
    public enum DemoType { LivingHive, ConstructionDemo, PopulationDemo, AIDemo, LogisticsDemo, ResourceDemo, GeneticsDemo, CombatDemo, PerformanceDemo, ServerSynchronizationDemo }
    public enum DemoState { Loaded, Running, Stopped, Completed }
    public enum ConstructionGameplayState { Requested, Validating, Reserved, Tasked, Assigned, DeliveringResources, Building, Completed, Paused, Cancelled, Blocked }

    public sealed class HiveAnalyticsManager
    {
        private readonly HiveAnalyticsEngine engine = new HiveAnalyticsEngine();
        public event Action<MetricSample> MetricRecorded;
        public event Action<AnalyticsReport> AnalyticsUpdated;
        public event Action<MetricAlert> ThresholdExceeded;
        public event Action<MetricTrend> TrendDetected;
        public event Action<AnalyticsReport> AnalyticsReportGenerated;

        public void RegisterMetric(MetricDefinition definition) { engine.Registry.RegisterMetric(definition); }
        public MetricSample RecordMetric(string metricId, double value, long tick) { MetricSample sample = engine.RecordMetric(metricId, value, tick); MetricRecorded?.Invoke(sample); foreach (MetricAlert alert in engine.DetectAlerts(metricId)) { ThresholdExceeded?.Invoke(alert); } MetricTrend trend = engine.DetectTrend(metricId); if (trend.Direction != TrendDirection.Stable) { TrendDetected?.Invoke(trend); } return sample; }
        public MetricAggregate AggregateMetrics(string metricId, MetricAggregationPeriod period) { return engine.AggregateMetrics(metricId, period); }
        public MetricSample QueryMetric(string metricId) { return engine.QueryMetric(metricId); }
        public IReadOnlyList<MetricSample> QueryHistory(string metricId) { return engine.QueryHistory(metricId); }
        public AnalyticsReport GenerateAnalyticsReport() { AnalyticsReport report = engine.GenerateAnalyticsReport(); AnalyticsUpdated?.Invoke(report); AnalyticsReportGenerated?.Invoke(report); return report; }
    }

    public sealed class HiveAnalyticsEngine
    {
        private readonly MetricsCollector collector = new MetricsCollector();
        private readonly MetricsAggregator aggregator = new MetricsAggregator();
        public MetricsRegistry Registry { get; } = new MetricsRegistry();
        public MetricSample RecordMetric(string metricId, double value, long tick) { MetricDefinition definition = Registry.Require(metricId); return collector.Record(definition, value, tick); }
        public MetricAggregate AggregateMetrics(string metricId, MetricAggregationPeriod period) { return aggregator.Aggregate(Registry.Require(metricId), collector.QueryHistory(metricId), period); }
        public MetricSample QueryMetric(string metricId) { return collector.QueryLatest(metricId); }
        public IReadOnlyList<MetricSample> QueryHistory(string metricId) { return collector.QueryHistory(metricId); }
        public MetricTrend DetectTrend(string metricId) { return aggregator.CalculateTrend(collector.QueryHistory(metricId)); }
        public IReadOnlyList<MetricAlert> DetectAlerts(string metricId) { return aggregator.DetectAlerts(Registry.Require(metricId), collector.QueryHistory(metricId)); }
        public AnalyticsReport GenerateAnalyticsReport() { return new AnalyticsReport(Registry.Count, collector.TotalSamples, Registry.Definitions.Select(d => AggregateMetrics(d.MetricId, MetricAggregationPeriod.ColonyLifetime)).ToArray()); }
    }

    public sealed class MetricsRegistry
    {
        private readonly Dictionary<string, MetricDefinition> definitions = new Dictionary<string, MetricDefinition>();
        public int Count => definitions.Count;
        public IEnumerable<MetricDefinition> Definitions => definitions.Values;
        public void RegisterMetric(MetricDefinition definition) { if (definition == null) throw new ArgumentNullException(nameof(definition)); definitions[definition.MetricId] = definition; }
        public MetricDefinition Require(string metricId) { if (!definitions.TryGetValue(metricId, out MetricDefinition definition)) throw new KeyNotFoundException(metricId); return definition; }
    }

    public sealed class MetricsCollector
    {
        private readonly Dictionary<string, List<MetricSample>> history = new Dictionary<string, List<MetricSample>>();
        public int TotalSamples { get; private set; }
        public MetricSample Record(MetricDefinition definition, double value, long tick) { if (!history.TryGetValue(definition.MetricId, out List<MetricSample> samples)) { samples = new List<MetricSample>(); history.Add(definition.MetricId, samples); } MetricSample sample = new MetricSample(definition.MetricId, value, tick); samples.Add(sample); TotalSamples++; return sample; }
        public MetricSample QueryLatest(string metricId) { IReadOnlyList<MetricSample> samples = QueryHistory(metricId); return samples.Count == 0 ? default : samples[samples.Count - 1]; }
        public IReadOnlyList<MetricSample> QueryHistory(string metricId) { return history.TryGetValue(metricId, out List<MetricSample> samples) ? samples.AsReadOnly() : Array.Empty<MetricSample>(); }
    }

    public sealed class MetricsAggregator
    {
        public MetricAggregate Aggregate(MetricDefinition definition, IReadOnlyList<MetricSample> samples, MetricAggregationPeriod period)
        {
            if (samples.Count == 0) return new MetricAggregate(definition.MetricId, period, 0d, 0d, 0d, 0d, 0d, 0d, TrendDirection.Stable, 0);
            double[] values = samples.Select(s => s.Value).OrderBy(v => v).ToArray();
            double min = values[0];
            double max = values[values.Length - 1];
            double avg = values.Average();
            double median = Percentile(values, 0.5d);
            double p90 = Percentile(values, 0.9d);
            TrendDirection trend = CalculateTrend(samples).Direction;
            return new MetricAggregate(definition.MetricId, period, min, max, avg, median, p90, samples[samples.Count - 1].Value, trend, samples.Count);
        }

        public MetricTrend CalculateTrend(IReadOnlyList<MetricSample> samples)
        {
            if (samples.Count < 3) return new MetricTrend(string.Empty, TrendDirection.Stable, 0d);
            double first = samples[0].Value;
            double last = samples[samples.Count - 1].Value;
            double delta = last - first;
            if (Math.Abs(delta) < 0.0001d) return new MetricTrend(samples[0].MetricId, TrendDirection.Stable, delta);
            return new MetricTrend(samples[0].MetricId, delta > 0d ? TrendDirection.Rising : TrendDirection.Falling, delta);
        }

        public IReadOnlyList<MetricAlert> DetectAlerts(MetricDefinition definition, IReadOnlyList<MetricSample> samples)
        {
            if (samples.Count == 0) return Array.Empty<MetricAlert>();
            MetricSample latest = samples[samples.Count - 1];
            List<MetricAlert> alerts = new List<MetricAlert>(2);
            if (latest.Value < definition.MinimumThreshold) alerts.Add(new MetricAlert(definition.MetricId, "BelowMinimum", latest.Value, definition.MinimumThreshold));
            if (latest.Value > definition.MaximumThreshold) alerts.Add(new MetricAlert(definition.MetricId, "AboveMaximum", latest.Value, definition.MaximumThreshold));
            return alerts;
        }

        private static double Percentile(double[] sorted, double percentile)
        {
            if (sorted.Length == 0) return 0d;
            double position = (sorted.Length - 1) * percentile;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            return lower == upper ? sorted[lower] : sorted[lower] + ((sorted[upper] - sorted[lower]) * (position - lower));
        }
    }

    public sealed class HiveAnalyticsDiagnostics { public AnalyticsReport LastReport { get; private set; } public void Capture(AnalyticsReport report) { LastReport = report; } }
    public sealed class MetricDefinition { public string MetricId { get; } public MetricDomain Domain { get; } public double MinimumThreshold { get; } public double MaximumThreshold { get; } public MetricDefinition(string metricId, MetricDomain domain, double minimumThreshold, double maximumThreshold) { MetricId = RequireId(metricId); Domain = domain; MinimumThreshold = minimumThreshold; MaximumThreshold = maximumThreshold; } internal static string RequireId(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Id is required."); return value; } }
    public readonly struct MetricSample { public string MetricId { get; } public double Value { get; } public long Tick { get; } public MetricSample(string metricId, double value, long tick) { MetricId = metricId; Value = value; Tick = tick; } }
    public readonly struct MetricTrend { public string MetricId { get; } public TrendDirection Direction { get; } public double Delta { get; } public MetricTrend(string metricId, TrendDirection direction, double delta) { MetricId = metricId; Direction = direction; Delta = delta; } }
    public readonly struct MetricAlert { public string MetricId { get; } public string Reason { get; } public double Value { get; } public double Threshold { get; } public MetricAlert(string metricId, string reason, double value, double threshold) { MetricId = metricId; Reason = reason; Value = value; Threshold = threshold; } }
    public sealed class MetricAggregate { public string MetricId { get; } public MetricAggregationPeriod Period { get; } public double Minimum { get; } public double Maximum { get; } public double Average { get; } public double Median { get; } public double Percentile90 { get; } public double Latest { get; } public TrendDirection Trend { get; } public int SampleCount { get; } public MetricAggregate(string metricId, MetricAggregationPeriod period, double minimum, double maximum, double average, double median, double percentile90, double latest, TrendDirection trend, int sampleCount) { MetricId = metricId; Period = period; Minimum = minimum; Maximum = maximum; Average = average; Median = median; Percentile90 = percentile90; Latest = latest; Trend = trend; SampleCount = sampleCount; } }
    public sealed class AnalyticsReport { public int RegisteredMetricCount { get; } public int TotalSampleCount { get; } public IReadOnlyList<MetricAggregate> Aggregates { get; } public AnalyticsReport(int registeredMetricCount, int totalSampleCount, IReadOnlyList<MetricAggregate> aggregates) { RegisteredMetricCount = registeredMetricCount; TotalSampleCount = totalSampleCount; Aggregates = aggregates ?? Array.Empty<MetricAggregate>(); } }

    public sealed class ColonyForecastManager
    {
        private readonly ForecastEngine engine = new ForecastEngine();
        public event Action<ForecastPrediction> ForecastGenerated;
        public event Action<ForecastPrediction> ForecastUpdated;
        public event Action<ForecastRisk> RiskPredicted;
        public event Action<ForecastPrediction> ForecastCompleted;
        public event Action<ScenarioComparison> ScenarioCompared;
        public void RegisterForecastModel(ForecastModel model) { engine.RegisterForecastModel(model); }
        public ForecastPrediction GenerateForecast(ForecastScenario scenario, ColonyForecastSnapshot snapshot) { ForecastPrediction prediction = engine.GenerateForecast(scenario, snapshot); ForecastGenerated?.Invoke(prediction); ForecastUpdated?.Invoke(prediction); foreach (ForecastRisk risk in prediction.Risks) RiskPredicted?.Invoke(risk); ForecastCompleted?.Invoke(prediction); return prediction; }
        public ScenarioComparison CompareScenarios(IEnumerable<ForecastScenario> scenarios, ColonyForecastSnapshot snapshot) { ScenarioComparison comparison = engine.CompareScenarios(scenarios, snapshot); ScenarioCompared?.Invoke(comparison); return comparison; }
        public ForecastPrediction QueryForecast(string forecastId) { return engine.QueryForecast(forecastId); }
        public IReadOnlyList<ForecastRisk> QueryRisks(string forecastId) { ForecastPrediction prediction = QueryForecast(forecastId); return prediction == null ? Array.Empty<ForecastRisk>() : prediction.Risks; }
        public double CalculateProbability(double value, double threshold) { return engine.CalculateProbability(value, threshold); }
    }

    public sealed class ForecastEngine
    {
        private readonly Dictionary<ForecastDomain, ForecastModel> models = new Dictionary<ForecastDomain, ForecastModel>();
        private readonly Dictionary<string, ForecastPrediction> predictions = new Dictionary<string, ForecastPrediction>();
        public void RegisterForecastModel(ForecastModel model) { if (model == null) throw new ArgumentNullException(nameof(model)); models[model.Domain] = model; }
        public ForecastPrediction GenerateForecast(ForecastScenario scenario, ColonyForecastSnapshot snapshot)
        {
            List<ForecastValue> values = new List<ForecastValue>();
            List<ForecastRisk> risks = new List<ForecastRisk>();
            foreach (ForecastModel model in models.Values)
            {
                double future = snapshot.Query(model.SourceKey) + (model.PerMinuteDelta * scenario.HorizonMinutes * scenario.IntensityMultiplier);
                values.Add(new ForecastValue(model.Domain, future));
                if (future < model.RiskThreshold) risks.Add(new ForecastRisk(model.RiskType, CalculateProbability(future, model.RiskThreshold), model.Impact, scenario.HorizonMinutes));
            }

            ForecastPrediction prediction = new ForecastPrediction(scenario.ScenarioId, values, risks);
            predictions[prediction.ForecastId] = prediction;
            return prediction;
        }

        public ScenarioComparison CompareScenarios(IEnumerable<ForecastScenario> scenarios, ColonyForecastSnapshot snapshot)
        {
            List<ForecastPrediction> compared = scenarios.Select(s => GenerateForecast(s, snapshot)).ToList();
            ForecastPrediction best = compared.OrderBy(p => p.Risks.Sum(r => r.Probability * r.Impact)).FirstOrDefault();
            return new ScenarioComparison(compared, best);
        }

        public ForecastPrediction QueryForecast(string forecastId) { return predictions.TryGetValue(forecastId, out ForecastPrediction prediction) ? prediction : null; }
        public double CalculateProbability(double value, double threshold) { if (threshold <= 0d) return 0d; return Math.Max(0d, Math.Min(1d, (threshold - value) / threshold)); }
    }

    public sealed class ForecastScenario { public string ScenarioId { get; } public ForecastScenarioType Type { get; } public double HorizonMinutes { get; } public double IntensityMultiplier { get; } public ForecastScenario(string scenarioId, ForecastScenarioType type, double horizonMinutes, double intensityMultiplier = 1d) { ScenarioId = MetricDefinition.RequireId(scenarioId); Type = type; HorizonMinutes = Math.Max(0d, horizonMinutes); IntensityMultiplier = intensityMultiplier; } }
    public sealed class ForecastModel { public ForecastDomain Domain { get; } public string SourceKey { get; } public double PerMinuteDelta { get; } public double RiskThreshold { get; } public ColonyRiskType RiskType { get; } public double Impact { get; } public ForecastModel(ForecastDomain domain, string sourceKey, double perMinuteDelta, double riskThreshold, ColonyRiskType riskType, double impact) { Domain = domain; SourceKey = MetricDefinition.RequireId(sourceKey); PerMinuteDelta = perMinuteDelta; RiskThreshold = riskThreshold; RiskType = riskType; Impact = impact; } }
    public sealed class ColonyForecastSnapshot { private readonly Dictionary<string, double> values; public ColonyForecastSnapshot(IReadOnlyDictionary<string, double> values) { this.values = new Dictionary<string, double>(values ?? new Dictionary<string, double>()); } public double Query(string key) { return values.TryGetValue(key, out double value) ? value : 0d; } }
    public sealed class ForecastPrediction { public string ForecastId { get; } public IReadOnlyList<ForecastValue> Values { get; } public IReadOnlyList<ForecastRisk> Risks { get; } public ForecastPrediction(string forecastId, IReadOnlyList<ForecastValue> values, IReadOnlyList<ForecastRisk> risks) { ForecastId = forecastId; Values = values ?? Array.Empty<ForecastValue>(); Risks = risks ?? Array.Empty<ForecastRisk>(); } }
    public readonly struct ForecastValue { public ForecastDomain Domain { get; } public double Value { get; } public ForecastValue(ForecastDomain domain, double value) { Domain = domain; Value = value; } }
    public readonly struct ForecastRisk { public ColonyRiskType Type { get; } public double Probability { get; } public double Impact { get; } public double EstimatedDelayMinutes { get; } public ForecastRisk(ColonyRiskType type, double probability, double impact, double estimatedDelayMinutes) { Type = type; Probability = probability; Impact = impact; EstimatedDelayMinutes = estimatedDelayMinutes; } }
    public sealed class ForecastAnalyzer { public double Score(ForecastPrediction prediction) { return prediction == null ? 0d : prediction.Values.Sum(v => v.Value) - prediction.Risks.Sum(r => r.Probability * r.Impact); } }
    public sealed class ForecastDiagnostics { public int GeneratedForecasts { get; private set; } public void RecordGenerated() { GeneratedForecasts++; } }
    public sealed class ScenarioComparison { public IReadOnlyList<ForecastPrediction> Predictions { get; } public ForecastPrediction BestPrediction { get; } public ScenarioComparison(IReadOnlyList<ForecastPrediction> predictions, ForecastPrediction bestPrediction) { Predictions = predictions ?? Array.Empty<ForecastPrediction>(); BestPrediction = bestPrediction; } }

    public sealed class ColonyEventManager
    {
        private readonly EventEngine engine = new EventEngine();
        public event Action<EventInstance> EventScheduled;
        public event Action<EventInstance> EventTriggered;
        public event Action<EventInstance> EventCompleted;
        public event Action<EventInstance> EventCancelled;
        public event Action<EventInstance> EventExpired;
        public event Action<EventInstance> EventChainStarted;
        public void RegisterEventDefinition(EventDefinition definition) { engine.RegisterEventDefinition(definition); }
        public EventInstance ScheduleEvent(string definitionId, long scheduledTick) { EventInstance instance = engine.ScheduleEvent(definitionId, scheduledTick); EventScheduled?.Invoke(instance); return instance; }
        public bool TriggerEvent(string eventId) { bool changed = engine.TriggerEvent(eventId); if (changed) EventTriggered?.Invoke(engine.QueryEvents().First(e => e.EventId == eventId)); return changed; }
        public bool ResolveEvent(string eventId) { bool changed = engine.ResolveEvent(eventId); if (changed) EventCompleted?.Invoke(engine.QueryEvents().First(e => e.EventId == eventId)); return changed; }
        public bool CancelEvent(string eventId) { bool changed = engine.CancelEvent(eventId); if (changed) EventCancelled?.Invoke(engine.QueryEvents().First(e => e.EventId == eventId)); return changed; }
        public bool ExpireEvent(string eventId) { bool changed = engine.ExpireEvent(eventId); if (changed) EventExpired?.Invoke(engine.QueryEvents().First(e => e.EventId == eventId)); return changed; }
        public IReadOnlyList<EventInstance> QueryEvents() { return engine.QueryEvents(); }
        public void Update(long tick) { foreach (EventInstance instance in engine.Update(tick)) { if (instance.State == ColonyEventState.Triggered) EventTriggered?.Invoke(instance); if (instance.Definition.DependencyIds.Count > 0) EventChainStarted?.Invoke(instance); } }
    }

    public sealed class EventEngine
    {
        private readonly Dictionary<string, EventDefinition> definitions = new Dictionary<string, EventDefinition>();
        private readonly Dictionary<string, EventInstance> events = new Dictionary<string, EventInstance>();
        public void RegisterEventDefinition(EventDefinition definition) { if (definition == null) throw new ArgumentNullException(nameof(definition)); definitions[definition.EventDefinitionId] = definition; }
        public EventInstance ScheduleEvent(string definitionId, long scheduledTick) { EventDefinition definition = definitions[definitionId]; string id = definitionId + "-" + events.Count; EventInstance instance = new EventInstance(id, definition, scheduledTick, ColonyEventState.Scheduled); events[id] = instance; return instance; }
        public IReadOnlyList<EventInstance> Update(long tick) { List<EventInstance> triggered = new List<EventInstance>(); foreach (EventInstance instance in events.Values) { if (instance.State == ColonyEventState.Scheduled && instance.ScheduledTick <= tick && DependenciesResolved(instance)) { instance.SetState(ColonyEventState.Triggered); triggered.Add(instance); } } return triggered; }
        public bool TriggerEvent(string eventId) { return SetState(eventId, ColonyEventState.Triggered); }
        public bool ResolveEvent(string eventId) { return SetState(eventId, ColonyEventState.Resolved); }
        public bool CancelEvent(string eventId) { return SetState(eventId, ColonyEventState.Cancelled); }
        public bool ExpireEvent(string eventId) { return SetState(eventId, ColonyEventState.Expired); }
        public IReadOnlyList<EventInstance> QueryEvents() { return events.Values.ToList(); }
        private bool SetState(string eventId, ColonyEventState state) { if (!events.TryGetValue(eventId, out EventInstance instance)) return false; instance.SetState(state); return true; }
        private bool DependenciesResolved(EventInstance instance) { return instance.Definition.DependencyIds.All(id => events.Values.Any(e => e.Definition.EventDefinitionId == id && e.State == ColonyEventState.Resolved)); }
    }

    public sealed class EventScheduler { public EventInstance Schedule(EventEngine engine, string definitionId, long tick) { return engine.ScheduleEvent(definitionId, tick); } }
    public sealed class EventDispatcher { public bool Dispatch(EventEngine engine, string eventId) { return engine.TriggerEvent(eventId); } }
    public sealed class EventDiagnostics { public int ArchivedEvents { get; private set; } public void RecordArchived() { ArchivedEvents++; } }
    public sealed class EventDefinition { public string EventDefinitionId { get; } public ColonyEventType Type { get; } public ColonyEventPriority Priority { get; } public IReadOnlyList<string> DependencyIds { get; } public EventDefinition(string eventDefinitionId, ColonyEventType type, ColonyEventPriority priority, IReadOnlyList<string> dependencyIds = null) { EventDefinitionId = MetricDefinition.RequireId(eventDefinitionId); Type = type; Priority = priority; DependencyIds = dependencyIds ?? Array.Empty<string>(); } }
    public sealed class EventInstance { public string EventId { get; } public EventDefinition Definition { get; } public long ScheduledTick { get; } public ColonyEventState State { get; private set; } public EventInstance(string eventId, EventDefinition definition, long scheduledTick, ColonyEventState state) { EventId = eventId; Definition = definition; ScheduledTick = scheduledTick; State = state; } public void SetState(ColonyEventState state) { State = state; } }

    public sealed class AchievementManager
    {
        private readonly AchievementEngine engine = new AchievementEngine();
        public event Action<AchievementInstance> AchievementUnlocked;
        public event Action<AchievementProgress> AchievementProgressUpdated;
        public event Action<AchievementReward> RewardGranted;
        public event Action<AchievementInstance> HiddenAchievementDiscovered;
        public event Action<AchievementInstance> AchievementCompleted;
        public void RegisterAchievement(AchievementDefinition definition) { engine.RegisterAchievement(definition); }
        public AchievementProgress UpdateProgress(string achievementId, double amount) { AchievementProgress progress = engine.UpdateProgress(achievementId, amount); AchievementProgressUpdated?.Invoke(progress); if (progress.IsComplete) UnlockAchievement(achievementId); return progress; }
        public bool UnlockAchievement(string achievementId) { AchievementInstance instance = engine.UnlockAchievement(achievementId); if (instance == null) return false; AchievementUnlocked?.Invoke(instance); if (instance.Definition.Type == AchievementType.Hidden) HiddenAchievementDiscovered?.Invoke(instance); AchievementCompleted?.Invoke(instance); return true; }
        public IReadOnlyList<AchievementInstance> QueryAchievements() { return engine.QueryAchievements(); }
        public AchievementProgress QueryProgress(string achievementId) { return engine.QueryProgress(achievementId); }
        public IReadOnlyList<AchievementReward> ClaimReward(string achievementId) { IReadOnlyList<AchievementReward> rewards = engine.ClaimReward(achievementId); foreach (AchievementReward reward in rewards) RewardGranted?.Invoke(reward); return rewards; }
    }

    public sealed class AchievementEngine
    {
        private readonly Dictionary<string, AchievementInstance> achievements = new Dictionary<string, AchievementInstance>();
        public void RegisterAchievement(AchievementDefinition definition) { achievements[definition.AchievementId] = new AchievementInstance(definition, new AchievementProgress(definition.AchievementId, 0d, definition.Maximum)); }
        public AchievementProgress UpdateProgress(string achievementId, double amount) { AchievementInstance instance = achievements[achievementId]; instance.Progress = new AchievementProgress(achievementId, Math.Min(instance.Definition.Maximum, instance.Progress.Progress + amount), instance.Definition.Maximum); return instance.Progress; }
        public AchievementInstance UnlockAchievement(string achievementId) { AchievementInstance instance = achievements[achievementId]; if (instance.Unlocked) return instance; instance.Unlock(); return instance; }
        public IReadOnlyList<AchievementInstance> QueryAchievements() { return achievements.Values.ToList(); }
        public AchievementProgress QueryProgress(string achievementId) { return achievements.TryGetValue(achievementId, out AchievementInstance instance) ? instance.Progress : null; }
        public IReadOnlyList<AchievementReward> ClaimReward(string achievementId) { AchievementInstance instance = achievements[achievementId]; if (!instance.Unlocked || instance.RewardClaimed) return Array.Empty<AchievementReward>(); instance.MarkRewardClaimed(); return instance.Definition.Rewards; }
    }

    public sealed class AchievementDefinition { public string AchievementId { get; } public AchievementType Type { get; } public double Maximum { get; } public bool Repeatable { get; } public IReadOnlyList<AchievementReward> Rewards { get; } public AchievementDefinition(string achievementId, AchievementType type, double maximum, bool repeatable, IReadOnlyList<AchievementReward> rewards) { AchievementId = MetricDefinition.RequireId(achievementId); Type = type; Maximum = Math.Max(1d, maximum); Repeatable = repeatable; Rewards = rewards ?? Array.Empty<AchievementReward>(); } }
    public sealed class AchievementInstance { public AchievementDefinition Definition { get; } public AchievementProgress Progress { get; internal set; } public bool Unlocked { get; private set; } public bool RewardClaimed { get; private set; } public AchievementInstance(AchievementDefinition definition, AchievementProgress progress) { Definition = definition; Progress = progress; } public void Unlock() { Unlocked = true; } public void MarkRewardClaimed() { RewardClaimed = true; } }
    public sealed class AchievementProgress { public string AchievementId { get; } public double Progress { get; } public double Maximum { get; } public bool IsComplete => Progress >= Maximum; public AchievementProgress(string achievementId, double progress, double maximum) { AchievementId = achievementId; Progress = progress; Maximum = maximum; } }
    public readonly struct AchievementReward { public AchievementRewardType Type { get; } public string RewardId { get; } public double Amount { get; } public AchievementReward(AchievementRewardType type, string rewardId, double amount) { Type = type; RewardId = rewardId; Amount = amount; } }
    public sealed class AchievementDiagnostics { public int UnlockCount { get; private set; } public void RecordUnlock() { UnlockCount++; } }

    public sealed class ColonyProgressionManager
    {
        private readonly ColonyProgressionEngine engine = new ColonyProgressionEngine();
        public event Action<ProgressionProfile> ProgressionUpdated;
        public event Action<ProgressionTier> TierUnlocked;
        public event Action<ProgressionLevel> ColonyLevelChanged;
        public event Action<string> ContentUnlocked;
        public event Action<ProgressionLevel> ProgressionMilestoneReached;
        public void RegisterProgressionDefinition(ProgressionDefinition definition) { engine.RegisterProgressionDefinition(definition); }
        public ProgressionProfile UpdateProgression(string source, double amount) { ProgressionLevel before = engine.Profile.Level; ProgressionProfile profile = engine.UpdateProgression(source, amount); ProgressionUpdated?.Invoke(profile); if (profile.Level != before) { ColonyLevelChanged?.Invoke(profile.Level); TierUnlocked?.Invoke(engine.QueryTier(profile.Level)); ProgressionMilestoneReached?.Invoke(profile.Level); foreach (string content in engine.QueryUnlockedContent()) ContentUnlocked?.Invoke(content); } return profile; }
        public ProgressionProfile CalculateProgression(IReadOnlyDictionary<string, double> sourceValues) { return engine.CalculateProgression(sourceValues); }
        public bool UnlockTier(ProgressionLevel level) { return engine.UnlockTier(level); }
        public ProgressionProfile QueryProgression() { return engine.Profile; }
        public IReadOnlyList<string> QueryUnlockedContent() { return engine.QueryUnlockedContent(); }
    }

    public sealed class ColonyProgressionEngine
    {
        private readonly Dictionary<ProgressionLevel, ProgressionTier> tiers = new Dictionary<ProgressionLevel, ProgressionTier>();
        private readonly Dictionary<string, double> sourceWeights = new Dictionary<string, double>();
        public ProgressionProfile Profile { get; private set; } = new ProgressionProfile(0d, ProgressionLevel.Settlement, new Dictionary<string, double>());
        public void RegisterProgressionDefinition(ProgressionDefinition definition) { foreach (ProgressionTier tier in definition.Tiers) tiers[tier.Level] = tier; foreach (KeyValuePair<string, double> pair in definition.SourceWeights) sourceWeights[pair.Key] = pair.Value; }
        public ProgressionProfile UpdateProgression(string source, double amount) { Dictionary<string, double> sources = new Dictionary<string, double>(Profile.SourceProgress); sources[source] = (sources.TryGetValue(source, out double existing) ? existing : 0d) + amount; return CalculateProgression(sources); }
        public ProgressionProfile CalculateProgression(IReadOnlyDictionary<string, double> sourceValues) { double total = sourceValues.Sum(pair => pair.Value * (sourceWeights.TryGetValue(pair.Key, out double weight) ? weight : 1d)); ProgressionLevel level = tiers.Values.Where(t => total >= t.RequiredProgress).OrderBy(t => t.RequiredProgress).Select(t => t.Level).LastOrDefault(); Profile = new ProgressionProfile(total, level, sourceValues); return Profile; }
        public bool UnlockTier(ProgressionLevel level) { return tiers.ContainsKey(level); }
        public ProgressionTier QueryTier(ProgressionLevel level) { return tiers.TryGetValue(level, out ProgressionTier tier) ? tier : null; }
        public IReadOnlyList<string> QueryUnlockedContent() { return tiers.Values.Where(t => Profile.TotalProgress >= t.RequiredProgress).SelectMany(t => t.Unlocks).Distinct().ToList(); }
    }

    public sealed class ProgressionDefinition { public IReadOnlyList<ProgressionTier> Tiers { get; } public IReadOnlyDictionary<string, double> SourceWeights { get; } public ProgressionDefinition(IReadOnlyList<ProgressionTier> tiers, IReadOnlyDictionary<string, double> sourceWeights) { Tiers = tiers ?? Array.Empty<ProgressionTier>(); SourceWeights = sourceWeights ?? new Dictionary<string, double>(); } }
    public sealed class ProgressionProfile { public double TotalProgress { get; } public ProgressionLevel Level { get; } public IReadOnlyDictionary<string, double> SourceProgress { get; } public ProgressionProfile(double totalProgress, ProgressionLevel level, IReadOnlyDictionary<string, double> sourceProgress) { TotalProgress = totalProgress; Level = level; SourceProgress = new Dictionary<string, double>(sourceProgress ?? new Dictionary<string, double>()); } }
    public sealed class ProgressionTier { public ProgressionLevel Level { get; } public double RequiredProgress { get; } public IReadOnlyList<string> Unlocks { get; } public ProgressionTier(ProgressionLevel level, double requiredProgress, IReadOnlyList<string> unlocks) { Level = level; RequiredProgress = requiredProgress; Unlocks = unlocks ?? Array.Empty<string>(); } }
    public sealed class ProgressionDiagnostics { public ProgressionProfile LastProfile { get; private set; } public void Capture(ProgressionProfile profile) { LastProfile = profile; } }

    public sealed class ColonyPrestigeManager
    {
        private readonly ColonyPrestigeEngine engine = new ColonyPrestigeEngine();
        public event Action<PrestigeProfile> PrestigeUpdated;
        public event Action<PrestigeTier> PrestigeTierUnlocked;
        public event Action<string> HistoricalMilestoneReached;
        public event Action<PrestigeLevel> ColonyRecognized;
        public event Action<string> PrestigeRewardGranted;
        public void RegisterPrestigeDefinition(PrestigeDefinition definition) { engine.RegisterPrestigeDefinition(definition); }
        public PrestigeProfile CalculatePrestige(IReadOnlyDictionary<string, double> sourceValues) { return engine.CalculatePrestige(sourceValues); }
        public PrestigeProfile UpdatePrestige(string source, double amount) { PrestigeLevel before = engine.Profile.Level; PrestigeProfile profile = engine.UpdatePrestige(source, amount); PrestigeUpdated?.Invoke(profile); if (profile.Level != before) { PrestigeTier tier = engine.QueryTier(profile.Level); PrestigeTierUnlocked?.Invoke(tier); ColonyRecognized?.Invoke(profile.Level); HistoricalMilestoneReached?.Invoke(profile.Level.ToString()); foreach (string reward in tier == null ? Array.Empty<string>() : tier.Rewards) PrestigeRewardGranted?.Invoke(reward); } return profile; }
        public bool UnlockPrestigeTier(PrestigeLevel level) { return engine.QueryTier(level) != null; }
        public PrestigeProfile QueryPrestige() { return engine.Profile; }
        public IReadOnlyList<string> QueryHistory() { return engine.History; }
    }

    public sealed class ColonyPrestigeEngine
    {
        private readonly Dictionary<PrestigeLevel, PrestigeTier> tiers = new Dictionary<PrestigeLevel, PrestigeTier>();
        private readonly Dictionary<string, double> sourceWeights = new Dictionary<string, double>();
        private readonly List<string> history = new List<string>();
        public PrestigeProfile Profile { get; private set; } = new PrestigeProfile(0d, PrestigeLevel.UnknownColony, new Dictionary<string, double>());
        public IReadOnlyList<string> History => history.AsReadOnly();
        public void RegisterPrestigeDefinition(PrestigeDefinition definition) { foreach (PrestigeTier tier in definition.Tiers) tiers[tier.Level] = tier; foreach (KeyValuePair<string, double> pair in definition.SourceWeights) sourceWeights[pair.Key] = pair.Value; }
        public PrestigeProfile UpdatePrestige(string source, double amount) { Dictionary<string, double> sources = new Dictionary<string, double>(Profile.SourcePrestige); sources[source] = (sources.TryGetValue(source, out double existing) ? existing : 0d) + amount; return CalculatePrestige(sources); }
        public PrestigeProfile CalculatePrestige(IReadOnlyDictionary<string, double> sourceValues) { double total = sourceValues.Sum(pair => pair.Value * (sourceWeights.TryGetValue(pair.Key, out double weight) ? weight : 1d)); PrestigeLevel level = tiers.Values.Where(t => total >= t.RequiredPrestige).OrderBy(t => t.RequiredPrestige).Select(t => t.Level).LastOrDefault(); if (level != Profile.Level) history.Add(level.ToString()); Profile = new PrestigeProfile(total, level, sourceValues); return Profile; }
        public PrestigeTier QueryTier(PrestigeLevel level) { return tiers.TryGetValue(level, out PrestigeTier tier) ? tier : null; }
    }

    public sealed class PrestigeDefinition { public IReadOnlyList<PrestigeTier> Tiers { get; } public IReadOnlyDictionary<string, double> SourceWeights { get; } public PrestigeDefinition(IReadOnlyList<PrestigeTier> tiers, IReadOnlyDictionary<string, double> sourceWeights) { Tiers = tiers ?? Array.Empty<PrestigeTier>(); SourceWeights = sourceWeights ?? new Dictionary<string, double>(); } }
    public sealed class PrestigeProfile { public double TotalPrestige { get; } public PrestigeLevel Level { get; } public IReadOnlyDictionary<string, double> SourcePrestige { get; } public PrestigeProfile(double totalPrestige, PrestigeLevel level, IReadOnlyDictionary<string, double> sourcePrestige) { TotalPrestige = totalPrestige; Level = level; SourcePrestige = new Dictionary<string, double>(sourcePrestige ?? new Dictionary<string, double>()); } }
    public sealed class PrestigeTier { public PrestigeLevel Level { get; } public double RequiredPrestige { get; } public IReadOnlyList<string> Rewards { get; } public PrestigeTier(PrestigeLevel level, double requiredPrestige, IReadOnlyList<string> rewards) { Level = level; RequiredPrestige = requiredPrestige; Rewards = rewards ?? Array.Empty<string>(); } }
    public sealed class PrestigeDiagnostics { public PrestigeProfile LastProfile { get; private set; } public void Capture(PrestigeProfile profile) { LastProfile = profile; } }

    public sealed class ColonyScenarioManager
    {
        private readonly ScenarioEngine engine = new ScenarioEngine();
        public event Action<ScenarioInstance> ScenarioStarted;
        public event Action<ScenarioObjective> ObjectiveCompleted;
        public event Action<ScenarioObjective> ObjectiveFailed;
        public event Action<ScenarioInstance> ScenarioCompleted;
        public event Action<ScenarioInstance> ScenarioFailed;
        public event Action<ScenarioInstance> ScenarioUnlocked;
        public void RegisterScenario(ScenarioDefinition definition) { engine.RegisterScenario(definition); }
        public ScenarioInstance LoadScenario(string scenarioId) { ScenarioInstance instance = engine.LoadScenario(scenarioId); ScenarioUnlocked?.Invoke(instance); return instance; }
        public ScenarioInstance StartScenario(string scenarioId) { ScenarioInstance instance = engine.StartScenario(scenarioId); ScenarioStarted?.Invoke(instance); return instance; }
        public bool PauseScenario(string scenarioId) { return engine.SetScenarioState(scenarioId, ScenarioState.Paused); }
        public bool ResumeScenario(string scenarioId) { return engine.SetScenarioState(scenarioId, ScenarioState.Active); }
        public bool CompleteScenario(string scenarioId) { bool changed = engine.SetScenarioState(scenarioId, ScenarioState.Completed); if (changed) ScenarioCompleted?.Invoke(engine.QueryScenario(scenarioId)); return changed; }
        public bool FailScenario(string scenarioId) { bool changed = engine.SetScenarioState(scenarioId, ScenarioState.Failed); if (changed) ScenarioFailed?.Invoke(engine.QueryScenario(scenarioId)); return changed; }
        public bool CompleteObjective(ScenarioObjective objective) { if (objective == null) return false; objective.ForceComplete(); ObjectiveCompleted?.Invoke(objective); return true; }
        public bool FailObjective(ScenarioObjective objective) { if (objective == null) return false; ObjectiveFailed?.Invoke(objective); return true; }
        public ScenarioInstance QueryScenario(string scenarioId) { return engine.QueryScenario(scenarioId); }
    }

    public sealed class ScenarioEngine
    {
        private readonly Dictionary<string, ScenarioDefinition> definitions = new Dictionary<string, ScenarioDefinition>();
        private readonly Dictionary<string, ScenarioInstance> instances = new Dictionary<string, ScenarioInstance>();
        public void RegisterScenario(ScenarioDefinition definition) { definitions[definition.ScenarioId] = definition; }
        public ScenarioInstance LoadScenario(string scenarioId) { ScenarioInstance instance = new ScenarioInstance(definitions[scenarioId], ScenarioState.Available); instances[scenarioId] = instance; return instance; }
        public ScenarioInstance StartScenario(string scenarioId) { ScenarioInstance instance = instances.TryGetValue(scenarioId, out ScenarioInstance existing) ? existing : LoadScenario(scenarioId); instance.SetState(ScenarioState.Active); return instance; }
        public bool SetScenarioState(string scenarioId, ScenarioState state) { ScenarioInstance instance = QueryScenario(scenarioId); if (instance == null) return false; instance.SetState(state); return true; }
        public ScenarioInstance QueryScenario(string scenarioId) { return instances.TryGetValue(scenarioId, out ScenarioInstance instance) ? instance : null; }
    }

    public sealed class ScenarioDefinition { public string ScenarioId { get; } public ScenarioType Type { get; } public IReadOnlyList<ScenarioObjective> Objectives { get; } public IReadOnlyList<ScenarioCondition> Constraints { get; } public ScenarioDefinition(string scenarioId, ScenarioType type, IReadOnlyList<ScenarioObjective> objectives, IReadOnlyList<ScenarioCondition> constraints) { ScenarioId = MetricDefinition.RequireId(scenarioId); Type = type; Objectives = objectives ?? Array.Empty<ScenarioObjective>(); Constraints = constraints ?? Array.Empty<ScenarioCondition>(); } }
    public sealed class ScenarioInstance { public ScenarioDefinition Definition { get; } public ScenarioState State { get; private set; } public ScenarioInstance(ScenarioDefinition definition, ScenarioState state) { Definition = definition; State = state; } public void SetState(ScenarioState state) { State = state; } }
    public sealed class ScenarioObjective { public string ObjectiveId { get; } public string MetricId { get; } public double Target { get; } public bool Completed { get; private set; } public ScenarioObjective(string objectiveId, string metricId, double target) { ObjectiveId = MetricDefinition.RequireId(objectiveId); MetricId = MetricDefinition.RequireId(metricId); Target = target; } public bool Update(double value) { Completed = value >= Target; return Completed; } public void ForceComplete() { Completed = true; } }
    public sealed class ScenarioCondition { public string ConditionId { get; } public string Key { get; } public double Limit { get; } public ScenarioCondition(string conditionId, string key, double limit) { ConditionId = MetricDefinition.RequireId(conditionId); Key = MetricDefinition.RequireId(key); Limit = limit; } }
    public sealed class ScenarioDiagnostics { public int StartedScenarios { get; private set; } public void RecordStarted() { StartedScenarios++; } }

    public sealed class ColonySandboxManager
    {
        private readonly SandboxEngine engine = new SandboxEngine();
        public event Action<SandboxSession> SandboxCreated;
        public event Action<SandboxSession> SandboxStarted;
        public event Action<SandboxSession> SandboxPaused;
        public event Action<SandboxSession> SandboxReset;
        public event Action<SandboxSession> SandboxConfigurationChanged;
        public event Action<SandboxSession> SandboxCompleted;
        public SandboxSession CreateSandbox(SandboxDefinition definition) { SandboxSession session = engine.CreateSandbox(definition); SandboxCreated?.Invoke(session); return session; }
        public SandboxSession LoadSandbox(SandboxSession session) { return engine.LoadSandbox(session); }
        public SandboxSession ConfigureSandbox(string sandboxId, SandboxConfigurator configurator) { SandboxSession session = engine.ConfigureSandbox(sandboxId, configurator); SandboxConfigurationChanged?.Invoke(session); return session; }
        public bool StartSandbox(string sandboxId) { bool changed = engine.SetState(sandboxId, SandboxState.Running); if (changed) SandboxStarted?.Invoke(engine.QuerySandbox(sandboxId)); return changed; }
        public bool PauseSandbox(string sandboxId) { bool changed = engine.SetState(sandboxId, SandboxState.Paused); if (changed) SandboxPaused?.Invoke(engine.QuerySandbox(sandboxId)); return changed; }
        public bool ResumeSandbox(string sandboxId) { return engine.SetState(sandboxId, SandboxState.Running); }
        public bool ResetSandbox(string sandboxId) { bool changed = engine.SetState(sandboxId, SandboxState.Reset); if (changed) SandboxReset?.Invoke(engine.QuerySandbox(sandboxId)); return changed; }
        public bool CompleteSandbox(string sandboxId) { bool changed = engine.SetState(sandboxId, SandboxState.Completed); if (changed) SandboxCompleted?.Invoke(engine.QuerySandbox(sandboxId)); return changed; }
        public SandboxSession QuerySandbox(string sandboxId) { return engine.QuerySandbox(sandboxId); }
    }

    public sealed class SandboxEngine
    {
        private readonly Dictionary<string, SandboxSession> sessions = new Dictionary<string, SandboxSession>();
        public SandboxSession CreateSandbox(SandboxDefinition definition) { SandboxSession session = new SandboxSession(definition, SandboxState.Created); sessions[definition.SandboxId] = session; return session; }
        public SandboxSession LoadSandbox(SandboxSession session) { sessions[session.Definition.SandboxId] = session; return session; }
        public SandboxSession ConfigureSandbox(string sandboxId, SandboxConfigurator configurator) { SandboxSession session = sessions[sandboxId]; session.Apply(configurator); return session; }
        public bool SetState(string sandboxId, SandboxState state) { if (!sessions.TryGetValue(sandboxId, out SandboxSession session)) return false; session.SetState(state); return true; }
        public SandboxSession QuerySandbox(string sandboxId) { return sessions.TryGetValue(sandboxId, out SandboxSession session) ? session : null; }
    }

    public sealed class SandboxDefinition { public string SandboxId { get; } public IReadOnlyList<SandboxMode> Modes { get; } public IReadOnlyDictionary<string, double> Options { get; } public SandboxDefinition(string sandboxId, IReadOnlyList<SandboxMode> modes, IReadOnlyDictionary<string, double> options) { SandboxId = MetricDefinition.RequireId(sandboxId); Modes = modes ?? Array.Empty<SandboxMode>(); Options = new Dictionary<string, double>(options ?? new Dictionary<string, double>()); } }
    public sealed class SandboxSession { public SandboxDefinition Definition { get; } public SandboxState State { get; private set; } public SandboxConfigurator Configurator { get; private set; } public SandboxSession(SandboxDefinition definition, SandboxState state) { Definition = definition; State = state; Configurator = new SandboxConfigurator(); } public void SetState(SandboxState state) { State = state; } public void Apply(SandboxConfigurator configurator) { Configurator = configurator ?? new SandboxConfigurator(); } }
    public sealed class SandboxConfigurator { private readonly Dictionary<string, double> options = new Dictionary<string, double>(); public IReadOnlyDictionary<string, double> Options => options; public SandboxConfigurator SetOption(string key, double value) { options[key] = value; return this; } public double GetOption(string key) { return options.TryGetValue(key, out double value) ? value : 0d; } }
    public sealed class SandboxDiagnostics { public int CreatedSessions { get; private set; } public void RecordCreated() { CreatedSessions++; } }

    public sealed class DemoManager
    {
        private readonly DemoEngine engine = new DemoEngine();
        public event Action<DemoScenario> DemoLoaded;
        public event Action<DemoScenario> DemoStarted;
        public event Action<DemoScenario> DemoStopped;
        public event Action<DemoScenario> DemoRestarted;
        public event Action<DemoScenario> DemoCompleted;
        public void RegisterDemo(DemoDefinition definition) { engine.RegisterDemo(definition); }
        public DemoScenario LoadDemo(string demoId) { DemoScenario scenario = engine.LoadDemo(demoId); DemoLoaded?.Invoke(scenario); return scenario; }
        public DemoScenario StartDemo(string demoId) { DemoScenario scenario = engine.StartDemo(demoId); DemoStarted?.Invoke(scenario); return scenario; }
        public bool StopDemo(string demoId) { bool changed = engine.SetState(demoId, DemoState.Stopped); if (changed) DemoStopped?.Invoke(engine.QueryDemo(demoId)); return changed; }
        public DemoScenario RestartDemo(string demoId) { DemoScenario scenario = engine.RestartDemo(demoId); DemoRestarted?.Invoke(scenario); return scenario; }
        public bool CompleteDemo(string demoId) { bool changed = engine.SetState(demoId, DemoState.Completed); if (changed) DemoCompleted?.Invoke(engine.QueryDemo(demoId)); return changed; }
        public DemoScenario QueryDemo(string demoId) { return engine.QueryDemo(demoId); }
    }

    public sealed class DemoEngine
    {
        private readonly Dictionary<string, DemoDefinition> definitions = new Dictionary<string, DemoDefinition>();
        private readonly Dictionary<string, DemoScenario> scenarios = new Dictionary<string, DemoScenario>();
        public void RegisterDemo(DemoDefinition definition) { definitions[definition.DemoId] = definition; }
        public DemoScenario LoadDemo(string demoId) { DemoScenario scenario = new DemoScenario(definitions[demoId], DemoState.Loaded); scenarios[demoId] = scenario; return scenario; }
        public DemoScenario StartDemo(string demoId) { DemoScenario scenario = scenarios.TryGetValue(demoId, out DemoScenario existing) ? existing : LoadDemo(demoId); scenario.SetState(DemoState.Running); return scenario; }
        public DemoScenario RestartDemo(string demoId) { DemoScenario scenario = LoadDemo(demoId); scenario.SetState(DemoState.Running); return scenario; }
        public bool SetState(string demoId, DemoState state) { if (!scenarios.TryGetValue(demoId, out DemoScenario scenario)) return false; scenario.SetState(state); return true; }
        public DemoScenario QueryDemo(string demoId) { return scenarios.TryGetValue(demoId, out DemoScenario scenario) ? scenario : null; }
    }

    public sealed class DemoDefinition { public string DemoId { get; } public DemoType Type { get; } public IReadOnlyDictionary<string, double> Configuration { get; } public IReadOnlyList<string> Overlays { get; } public DemoDefinition(string demoId, DemoType type, IReadOnlyDictionary<string, double> configuration, IReadOnlyList<string> overlays) { DemoId = MetricDefinition.RequireId(demoId); Type = type; Configuration = new Dictionary<string, double>(configuration ?? new Dictionary<string, double>()); Overlays = overlays ?? Array.Empty<string>(); } public static DemoDefinition CreateLivingHive() { return new DemoDefinition("living-hive", DemoType.LivingHive, new Dictionary<string, double> { { "bees", 50d }, { "queen", 1d }, { "activeConstruction", 1d } }, new[] { "FPS", "Tick", "Population", "Construction", "AI", "Resources" }); } }
    public sealed class DemoScenario { public DemoDefinition Definition { get; } public DemoState State { get; private set; } public DemoScenario(DemoDefinition definition, DemoState state) { Definition = definition; State = state; } public void SetState(DemoState state) { State = state; } }
    public sealed class DemoLauncher { public DemoScenario Launch(DemoManager manager, string demoId) { return manager.StartDemo(demoId); } }
    public sealed class DemoDiagnostics { public int Starts { get; private set; } public void RecordStart() { Starts++; } }

    public sealed class ConstructionGameplayManager
    {
        private readonly ConstructionGameplayEngine engine = new ConstructionGameplayEngine();
        public event Action<ConstructionGameplaySnapshot> ConstructionStarted;
        public event Action<ConstructionGameplaySnapshot> ConstructionPaused;
        public event Action<ConstructionGameplaySnapshot> ConstructionResumed;
        public event Action<ConstructionGameplaySnapshot> ConstructionCompleted;
        public event Action<ConstructionGameplaySnapshot> ConstructionCancelled;
        public event Action<ConstructionGameplaySnapshot> BuildingActivated;
        public ConstructionGameplaySnapshot StartConstruction(ConstructionRequest request) { ConstructionGameplaySnapshot snapshot = engine.StartConstruction(request); ConstructionStarted?.Invoke(snapshot); return snapshot; }
        public bool PauseConstruction(string constructionId) { bool changed = engine.SetState(constructionId, ConstructionGameplayState.Paused); if (changed) ConstructionPaused?.Invoke(engine.QueryConstruction(constructionId)); return changed; }
        public bool ResumeConstruction(string constructionId) { bool changed = engine.SetState(constructionId, ConstructionGameplayState.Building); if (changed) ConstructionResumed?.Invoke(engine.QueryConstruction(constructionId)); return changed; }
        public bool CancelConstruction(string constructionId) { bool changed = engine.SetState(constructionId, ConstructionGameplayState.Cancelled); if (changed) ConstructionCancelled?.Invoke(engine.QueryConstruction(constructionId)); return changed; }
        public ConstructionGameplaySnapshot QueryConstruction(string constructionId) { return engine.QueryConstruction(constructionId); }
        public IReadOnlyList<ConstructionGameplaySnapshot> QueryConstructionHistory() { return engine.QueryConstructionHistory(); }
        public ConstructionGameplaySnapshot UpdateConstruction(string constructionId, double work) { ConstructionGameplaySnapshot snapshot = engine.UpdateConstruction(constructionId, work); if (snapshot.State == ConstructionGameplayState.Completed) { ConstructionCompleted?.Invoke(snapshot); BuildingActivated?.Invoke(snapshot); } return snapshot; }
    }

    public sealed class ConstructionGameplayEngine
    {
        private readonly Dictionary<string, ConstructionGameplaySnapshot> active = new Dictionary<string, ConstructionGameplaySnapshot>();
        private readonly List<ConstructionGameplaySnapshot> history = new List<ConstructionGameplaySnapshot>();
        public ConstructionGameplaySnapshot StartConstruction(ConstructionRequest request) { ConstructionGameplaySnapshot snapshot = new ConstructionWorkflowCoordinator().CreateSnapshot(request, ConstructionGameplayState.Building, 0d); active[request.ConstructionId] = snapshot; history.Add(snapshot); return snapshot; }
        public ConstructionGameplaySnapshot UpdateConstruction(string constructionId, double work) { ConstructionGameplaySnapshot current = active[constructionId]; double progress = Math.Min(1d, current.Progress + Math.Max(0d, work / Math.Max(1d, current.Request.RequiredWork))); ConstructionGameplayState state = progress >= 1d ? ConstructionGameplayState.Completed : current.State; ConstructionGameplaySnapshot next = new ConstructionWorkflowCoordinator().CreateSnapshot(current.Request, state, progress); active[constructionId] = next; history.Add(next); return next; }
        public bool SetState(string constructionId, ConstructionGameplayState state) { if (!active.TryGetValue(constructionId, out ConstructionGameplaySnapshot current)) return false; ConstructionGameplaySnapshot next = new ConstructionGameplaySnapshot(current.Request, state, current.Progress); active[constructionId] = next; history.Add(next); return true; }
        public ConstructionGameplaySnapshot QueryConstruction(string constructionId) { return active.TryGetValue(constructionId, out ConstructionGameplaySnapshot snapshot) ? snapshot : null; }
        public IReadOnlyList<ConstructionGameplaySnapshot> QueryConstructionHistory() { return history.AsReadOnly(); }
    }

    public sealed class ConstructionWorkflowCoordinator { public ConstructionGameplaySnapshot CreateSnapshot(ConstructionRequest request, ConstructionGameplayState state, double progress) { if (request.AvailableResources < request.RequiredResources) state = ConstructionGameplayState.Blocked; return new ConstructionGameplaySnapshot(request, state, progress); } }
    public sealed class ConstructionGameplayDiagnostics { public int CompletedConstructions { get; private set; } public void RecordCompleted() { CompletedConstructions++; } }
    public sealed class ConstructionRequest { public string ConstructionId { get; } public string BuildingId { get; } public double RequiredResources { get; } public double AvailableResources { get; } public double RequiredWork { get; } public int Priority { get; } public ConstructionRequest(string constructionId, string buildingId, double requiredResources, double availableResources, double requiredWork, int priority) { ConstructionId = MetricDefinition.RequireId(constructionId); BuildingId = MetricDefinition.RequireId(buildingId); RequiredResources = requiredResources; AvailableResources = availableResources; RequiredWork = requiredWork; Priority = priority; } }
    public sealed class ConstructionGameplaySnapshot { public ConstructionRequest Request { get; } public ConstructionGameplayState State { get; } public double Progress { get; } public ConstructionGameplaySnapshot(ConstructionRequest request, ConstructionGameplayState state, double progress) { Request = request; State = state; Progress = progress; } }
}
