using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Simulation
{
    public enum SimulationMetricAggregation
    {
        Last,
        Sum,
        Min,
        Max,
        Average
    }

    public readonly struct SimulationMetricDefinition
    {
        public string MetricId { get; }
        public SimulationMetricAggregation Aggregation { get; }

        public SimulationMetricDefinition(string metricId, SimulationMetricAggregation aggregation)
        {
            MetricId = string.IsNullOrWhiteSpace(metricId) ? throw new ArgumentException("Metric id is required.", nameof(metricId)) : metricId;
            Aggregation = aggregation;
        }
    }

    public readonly struct SimulationMetricSample
    {
        public double TickSeconds { get; }
        public double Value { get; }

        public SimulationMetricSample(double tickSeconds, double value)
        {
            TickSeconds = tickSeconds;
            Value = value;
        }
    }

    public readonly struct SimulationMetricValue
    {
        public string MetricId { get; }
        public SimulationMetricAggregation Aggregation { get; }
        public double Value { get; }
        public int SampleCount { get; }

        public SimulationMetricValue(string metricId, SimulationMetricAggregation aggregation, double value, int sampleCount)
        {
            MetricId = metricId;
            Aggregation = aggregation;
            Value = value;
            SampleCount = sampleCount;
        }
    }

    public sealed class SimulationStatisticsSnapshot
    {
        public int Version { get; }
        public double SimulationSeconds { get; }
        public IReadOnlyList<SimulationMetricValue> Metrics { get; }

        public SimulationStatisticsSnapshot(int version, double simulationSeconds, IReadOnlyList<SimulationMetricValue> metrics)
        {
            Version = version;
            SimulationSeconds = simulationSeconds;
            Metrics = metrics ?? Array.Empty<SimulationMetricValue>();
        }
    }

    public sealed class SimulationStatisticsDiagnostics
    {
        public int RegisteredMetrics { get; private set; }
        public int RecordedSamples { get; private set; }
        public int RejectedSamples { get; private set; }
        public int SnapshotsCreated { get; private set; }

        public void RecordRegistration(int count) => RegisteredMetrics = count;
        public void RecordSample() => RecordedSamples++;
        public void RecordRejectedSample() => RejectedSamples++;
        public void RecordSnapshot() => SnapshotsCreated++;
    }

    public sealed class SimulationStatisticsEngine
    {
        private const int SnapshotVersion = 1;

        private readonly Dictionary<string, SimulationMetricDefinition> definitions = new Dictionary<string, SimulationMetricDefinition>();
        private readonly Dictionary<string, List<SimulationMetricSample>> samples = new Dictionary<string, List<SimulationMetricSample>>();

        public SimulationStatisticsDiagnostics Diagnostics { get; } = new SimulationStatisticsDiagnostics();

        public int MetricCount => definitions.Count;

        public bool RegisterMetric(SimulationMetricDefinition definition)
        {
            if (definitions.ContainsKey(definition.MetricId)) return false;
            definitions.Add(definition.MetricId, definition);
            samples.Add(definition.MetricId, new List<SimulationMetricSample>());
            Diagnostics.RecordRegistration(definitions.Count);
            return true;
        }

        public bool RecordSample(string metricId, double tickSeconds, double value)
        {
            if (!samples.TryGetValue(metricId, out List<SimulationMetricSample> metricSamples) || double.IsNaN(value) || double.IsInfinity(value))
            {
                Diagnostics.RecordRejectedSample();
                return false;
            }

            metricSamples.Add(new SimulationMetricSample(tickSeconds, value));
            Diagnostics.RecordSample();
            return true;
        }

        public bool TryGetValue(string metricId, out SimulationMetricValue value)
        {
            if (!definitions.TryGetValue(metricId, out SimulationMetricDefinition definition))
            {
                value = default;
                return false;
            }

            value = Aggregate(definition, samples[metricId]);
            return true;
        }

        public SimulationStatisticsSnapshot CreateSnapshot(double simulationSeconds)
        {
            List<SimulationMetricValue> values = new List<SimulationMetricValue>(definitions.Count);
            foreach (SimulationMetricDefinition definition in definitions.Values)
            {
                values.Add(Aggregate(definition, samples[definition.MetricId]));
            }

            values.Sort((left, right) => string.CompareOrdinal(left.MetricId, right.MetricId));
            Diagnostics.RecordSnapshot();
            return new SimulationStatisticsSnapshot(SnapshotVersion, simulationSeconds, values);
        }

        public void ClearSamples(string metricId)
        {
            if (samples.TryGetValue(metricId, out List<SimulationMetricSample> metricSamples))
            {
                metricSamples.Clear();
            }
        }

        private static SimulationMetricValue Aggregate(SimulationMetricDefinition definition, IReadOnlyList<SimulationMetricSample> metricSamples)
        {
            if (metricSamples.Count == 0)
            {
                return new SimulationMetricValue(definition.MetricId, definition.Aggregation, 0d, 0);
            }

            double result = metricSamples[0].Value;
            double sum = 0d;

            for (int i = 0; i < metricSamples.Count; i++)
            {
                double sample = metricSamples[i].Value;
                sum += sample;

                if (definition.Aggregation == SimulationMetricAggregation.Min && sample < result) result = sample;
                if (definition.Aggregation == SimulationMetricAggregation.Max && sample > result) result = sample;
                if (definition.Aggregation == SimulationMetricAggregation.Last) result = sample;
            }

            if (definition.Aggregation == SimulationMetricAggregation.Sum) result = sum;
            if (definition.Aggregation == SimulationMetricAggregation.Average) result = sum / metricSamples.Count;

            return new SimulationMetricValue(definition.MetricId, definition.Aggregation, result, metricSamples.Count);
        }
    }
}
