using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum EggProductionState { Ready, Scheduled, Produced, Registered, Incubation, Paused, Blocked }
    public enum EggProductionBlockReason { None, NoQueen, QueenUnavailable, DemographicLimit, NurserySaturated, ResourceShortage, Paused }

    public sealed class EggProductionDefinition
    {
        public string DefinitionId { get; }
        public double BaseEggsPerDay { get; }
        public double FertilityWeight { get; }
        public double HealthWeight { get; }
        public double NutritionWeight { get; }
        public double PheromoneWeight { get; }
        public double TemperatureCoefficient { get; }
        public double SeasonCoefficient { get; }
        public double ResourceCoefficient { get; }
        public int MaxPopulation { get; }
        public int NurseryCapacity { get; }
        public double HungerSafetyThreshold { get; }

        public EggProductionDefinition(
            string definitionId,
            double baseEggsPerDay,
            double fertilityWeight,
            double healthWeight,
            double nutritionWeight,
            double pheromoneWeight,
            double temperatureCoefficient,
            double seasonCoefficient,
            double resourceCoefficient,
            int maxPopulation,
            int nurseryCapacity,
            double hungerSafetyThreshold)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            BaseEggsPerDay = Math.Max(0d, baseEggsPerDay);
            FertilityWeight = Clamp01(fertilityWeight);
            HealthWeight = Clamp01(healthWeight);
            NutritionWeight = Clamp01(nutritionWeight);
            PheromoneWeight = Clamp01(pheromoneWeight);
            TemperatureCoefficient = Clamp01(temperatureCoefficient);
            SeasonCoefficient = Clamp01(seasonCoefficient);
            ResourceCoefficient = Clamp01(resourceCoefficient);
            MaxPopulation = Math.Max(0, maxPopulation);
            NurseryCapacity = Math.Max(0, nurseryCapacity);
            HungerSafetyThreshold = Clamp01(hungerSafetyThreshold);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public readonly struct EggProductionContext
    {
        public string QueenId { get; }
        public int CurrentPopulation { get; }
        public int IncubatingEggs { get; }
        public double TemperatureFactor { get; }
        public double SeasonFactor { get; }
        public double ResourceFactor { get; }
        public double ColonyGoalFactor { get; }

        public EggProductionContext(string queenId, int currentPopulation, int incubatingEggs, double temperatureFactor = 1d, double seasonFactor = 1d, double resourceFactor = 1d, double colonyGoalFactor = 1d)
        {
            QueenId = queenId ?? string.Empty;
            CurrentPopulation = Math.Max(0, currentPopulation);
            IncubatingEggs = Math.Max(0, incubatingEggs);
            TemperatureFactor = Clamp01(temperatureFactor);
            SeasonFactor = Clamp01(seasonFactor);
            ResourceFactor = Clamp01(resourceFactor);
            ColonyGoalFactor = Clamp01(colonyGoalFactor);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class EggProductionRecord
    {
        public string EggId { get; }
        public string QueenId { get; }
        public string DefinitionId { get; }
        public EggProductionState State { get; private set; }
        public double ScheduledDay { get; }
        public double ProducedDay { get; private set; }

        public EggProductionRecord(string eggId, string queenId, string definitionId, double scheduledDay)
        {
            EggId = string.IsNullOrWhiteSpace(eggId) ? throw new ArgumentException("Egg id is required.", nameof(eggId)) : eggId;
            QueenId = queenId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            ScheduledDay = Math.Max(0d, scheduledDay);
            ProducedDay = -1d;
            State = EggProductionState.Scheduled;
        }

        public void MarkProduced(double producedDay)
        {
            ProducedDay = Math.Max(0d, producedDay);
            State = EggProductionState.Produced;
        }

        public void MarkRegistered() => State = EggProductionState.Registered;
        public void MarkIncubating() => State = EggProductionState.Incubation;
    }

    public sealed class EggProductionQueue
    {
        private readonly Queue<EggProductionRecord> scheduled = new Queue<EggProductionRecord>();
        private readonly List<EggProductionRecord> produced = new List<EggProductionRecord>();

        public int ScheduledCount => scheduled.Count;
        public int ProducedCount => produced.Count;

        public void Enqueue(EggProductionRecord record)
        {
            if (record != null) scheduled.Enqueue(record);
        }

        public EggProductionRecord Dequeue()
        {
            return scheduled.Count == 0 ? null : scheduled.Dequeue();
        }

        public void AddProduced(EggProductionRecord record)
        {
            if (record != null) produced.Add(record);
        }

        public IReadOnlyList<EggProductionRecord> QueryProduced()
        {
            List<EggProductionRecord> result = new List<EggProductionRecord>(produced);
            result.Sort((left, right) => string.CompareOrdinal(left.EggId, right.EggId));
            return result;
        }
    }

    public sealed class EggProductionStatistics
    {
        public int EggsScheduled { get; }
        public int EggsProduced { get; }
        public int EggsRegistered { get; }
        public double EggsPerDay { get; }
        public double FertilityRate { get; }
        public double HatchingRate { get; }
        public double GrowthPotential { get; }
        public double ReproductiveEfficiency { get; }

        public EggProductionStatistics(int eggsScheduled, int eggsProduced, int eggsRegistered, double eggsPerDay, double fertilityRate, double hatchingRate, double growthPotential, double reproductiveEfficiency)
        {
            EggsScheduled = eggsScheduled;
            EggsProduced = eggsProduced;
            EggsRegistered = eggsRegistered;
            EggsPerDay = eggsPerDay;
            FertilityRate = fertilityRate;
            HatchingRate = hatchingRate;
            GrowthPotential = growthPotential;
            ReproductiveEfficiency = reproductiveEfficiency;
        }
    }

    public sealed class EggProductionDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int Schedules { get; private set; }
        public int Produced { get; private set; }
        public int Registered { get; private set; }
        public int Pauses { get; private set; }
        public int Resumes { get; private set; }
        public EggProductionBlockReason LastBlockReason { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordSchedule() => Schedules++;
        public void RecordProduced() => Produced++;
        public void RecordRegistered() => Registered++;
        public void RecordPause() => Pauses++;
        public void RecordResume() => Resumes++;
        public void RecordBlock(EggProductionBlockReason reason) => LastBlockReason = reason;
    }

    public sealed class EggProductionEngine
    {
        public double CalculateEggRate(EggProductionDefinition definition, QueenInstance queen, EggProductionContext context)
        {
            if (definition == null || queen == null || !queen.IsAlive) return 0d;

            double queenFactor =
                queen.Definition.Fertility * definition.FertilityWeight +
                queen.Definition.Health * definition.HealthWeight +
                queen.Definition.Nutrition * definition.NutritionWeight;

            bool hasBroodSignal = false;
            foreach (QueenPheromoneType pheromone in queen.ActivePheromones)
            {
                if (pheromone == QueenPheromoneType.BroodSignal)
                {
                    hasBroodSignal = true;
                    break;
                }
            }

            double pheromoneFactor = hasBroodSignal ? definition.PheromoneWeight : 0d;
            double environment =
                context.TemperatureFactor * definition.TemperatureCoefficient *
                context.SeasonFactor * definition.SeasonCoefficient *
                context.ResourceFactor * definition.ResourceCoefficient *
                context.ColonyGoalFactor;

            return definition.BaseEggsPerDay * (queenFactor + pheromoneFactor) * environment;
        }

        public EggProductionBlockReason Validate(EggProductionDefinition definition, QueenInstance queen, EggProductionContext context, bool paused)
        {
            if (paused) return EggProductionBlockReason.Paused;
            if (queen == null) return EggProductionBlockReason.NoQueen;
            if (!queen.IsAlive || queen.State == QueenState.Egg || queen.State == QueenState.Virgin) return EggProductionBlockReason.QueenUnavailable;
            if (definition.MaxPopulation > 0 && context.CurrentPopulation >= definition.MaxPopulation) return EggProductionBlockReason.DemographicLimit;
            if (definition.NurseryCapacity > 0 && context.IncubatingEggs >= definition.NurseryCapacity) return EggProductionBlockReason.NurserySaturated;
            if (context.ResourceFactor < definition.HungerSafetyThreshold) return EggProductionBlockReason.ResourceShortage;
            return EggProductionBlockReason.None;
        }
    }

    public sealed class EggProductionManager
    {
        private readonly Dictionary<string, EggProductionDefinition> definitions = new Dictionary<string, EggProductionDefinition>();
        private readonly EggProductionEngine engine = new EggProductionEngine();
        private readonly EggProductionQueue queue = new EggProductionQueue();
        private readonly QueenManager queenManager;
        private readonly PopulationManager populationManager;
        private readonly BeeLifecycleManager lifecycleManager;
        private readonly IEventBus eventBus;
        private bool paused;
        private int eggSequence;
        private int registered;
        private double lastRate;

        public EggProductionDiagnostics Diagnostics { get; } = new EggProductionDiagnostics();

        public EggProductionManager(QueenManager queenManager, PopulationManager populationManager = null, BeeLifecycleManager lifecycleManager = null, IEventBus eventBus = null)
        {
            this.queenManager = queenManager ?? throw new ArgumentNullException(nameof(queenManager));
            this.populationManager = populationManager;
            this.lifecycleManager = lifecycleManager;
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(EggProductionDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public EggProductionRecord ScheduleEggProduction(string definitionId, EggProductionContext context, double scheduledDay = 0d)
        {
            if (!definitions.TryGetValue(definitionId, out EggProductionDefinition definition)) return null;
            QueenInstance queen = queenManager.GetQueen(context.QueenId);
            EggProductionBlockReason reason = engine.Validate(definition, queen, context, paused);
            if (reason != EggProductionBlockReason.None)
            {
                Diagnostics.RecordBlock(reason);
                return null;
            }

            string eggId = context.QueenId + "-egg-" + (++eggSequence).ToString("D6");
            EggProductionRecord record = new EggProductionRecord(eggId, context.QueenId, definitionId, scheduledDay);
            queue.Enqueue(record);
            Diagnostics.RecordSchedule();
            eventBus?.Publish(new EggScheduled(record.EggId, record.QueenId));
            return record;
        }

        public EggProductionRecord ProduceEgg(string definitionId, EggProductionContext context, double producedDay = 0d)
        {
            EggProductionRecord record = ScheduleEggProduction(definitionId, context, producedDay);
            if (record == null) return null;
            return ProduceNextEgg(producedDay);
        }

        public EggProductionRecord ProduceNextEgg(double producedDay = 0d)
        {
            if (paused)
            {
                Diagnostics.RecordBlock(EggProductionBlockReason.Paused);
                return null;
            }

            EggProductionRecord record = queue.Dequeue();
            if (record == null) return null;
            record.MarkProduced(producedDay);
            queue.AddProduced(record);
            QueenInstance queen = queenManager.GetQueen(record.QueenId);
            queen?.History.RecordEggs(1);
            Diagnostics.RecordProduced();
            eventBus?.Publish(new EggProduced(record.EggId, record.QueenId));
            RegisterEgg(record);
            return record;
        }

        public double CalculateEggRate(string definitionId, EggProductionContext context)
        {
            if (!definitions.TryGetValue(definitionId, out EggProductionDefinition definition)) return 0d;
            double rate = engine.CalculateEggRate(definition, queenManager.GetQueen(context.QueenId), context);
            if (Math.Abs(rate - lastRate) > 0.0001d) eventBus?.Publish(new EggProductionRateChanged(context.QueenId, rate));
            lastRate = rate;
            return rate;
        }

        public EggProductionStatistics QueryEggStatistics(string definitionId, EggProductionContext context)
        {
            double rate = CalculateEggRate(definitionId, context);
            double fertility = rate <= 0d ? 0d : Math.Min(1d, rate / Math.Max(1d, definitions[definitionId].BaseEggsPerDay));
            double hatching = queue.ProducedCount == 0 ? 0d : (double)registered / queue.ProducedCount;
            double growthPotential = Math.Max(0d, definitions[definitionId].MaxPopulation - context.CurrentPopulation);
            double efficiency = rate <= 0d ? 0d : Math.Min(1d, registered / Math.Max(1d, rate));
            return new EggProductionStatistics(queue.ScheduledCount, queue.ProducedCount, registered, rate, fertility, hatching, growthPotential, efficiency);
        }

        public void PauseEggProduction()
        {
            if (paused) return;
            paused = true;
            Diagnostics.RecordPause();
            eventBus?.Publish(new EggProductionPaused());
        }

        public void ResumeEggProduction()
        {
            if (!paused) return;
            paused = false;
            Diagnostics.RecordResume();
            eventBus?.Publish(new EggProductionResumed());
        }

        private void RegisterEgg(EggProductionRecord record)
        {
            record.MarkRegistered();
            registered++;
            Diagnostics.RecordRegistered();
            populationManager?.RegisterBee(new BeePopulationRecord(record.EggId, record.DefinitionId, BeeCaste.Egg));
            lifecycleManager?.RegisterBee(record.EggId, record.DefinitionId);
            record.MarkIncubating();
            eventBus?.Publish(new EggIncubating(record.EggId, record.QueenId));
        }
    }

    public readonly struct EggScheduled : IGameplayEvent, IBeeEvent { public string EggId { get; } public string QueenId { get; } public EggScheduled(string eggId, string queenId) { EggId = eggId; QueenId = queenId; } }
    public readonly struct EggProduced : IGameplayEvent, IBeeEvent { public string EggId { get; } public string QueenId { get; } public EggProduced(string eggId, string queenId) { EggId = eggId; QueenId = queenId; } }
    public readonly struct EggIncubating : IGameplayEvent, IBeeEvent { public string EggId { get; } public string QueenId { get; } public EggIncubating(string eggId, string queenId) { EggId = eggId; QueenId = queenId; } }
    public readonly struct EggProductionPaused : IGameplayEvent, IBeeEvent { }
    public readonly struct EggProductionResumed : IGameplayEvent, IBeeEvent { }
    public readonly struct EggProductionRateChanged : IGameplayEvent, IBeeEvent { public string QueenId { get; } public double Rate { get; } public EggProductionRateChanged(string queenId, double rate) { QueenId = queenId; Rate = rate; } }
}
