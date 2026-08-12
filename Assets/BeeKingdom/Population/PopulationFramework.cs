using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum BeeCaste { Queen, Worker, Nurse, Builder, Forager, Guard, Scout, Cleaner, Ventilator, FoodProcessor, WaxProducer, Drone, Larva, Egg, Pupa }
    public enum BeePopulationState { Alive, Busy, Resting, Sleeping, Hungry, Injured, Sick, Dead }

    public sealed class PopulationDefinition
    {
        public string DefinitionId { get; }
        public BeeCaste Caste { get; }
        public double LifeExpectancyDays { get; }
        public double Productivity { get; }

        public PopulationDefinition(string definitionId, BeeCaste caste, double lifeExpectancyDays, double productivity)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Caste = caste;
            LifeExpectancyDays = lifeExpectancyDays <= 0d ? 1d : lifeExpectancyDays;
            Productivity = productivity < 0d ? 0d : productivity;
        }
    }

    public sealed class BeePopulationRecord
    {
        public string BeeId { get; }
        public string DefinitionId { get; }
        public BeeCaste Caste { get; private set; }
        public BeePopulationState State { get; private set; }
        public double AgeDays { get; private set; }
        public string Activity { get; private set; }
        public string Sector { get; private set; }
        public string Chamber { get; private set; }
        public string Role { get; private set; }

        public BeePopulationRecord(string beeId, string definitionId, BeeCaste caste, double ageDays = 0d)
        {
            BeeId = string.IsNullOrWhiteSpace(beeId) ? throw new ArgumentException("Bee id is required.", nameof(beeId)) : beeId;
            DefinitionId = definitionId ?? string.Empty;
            Caste = caste;
            AgeDays = ageDays < 0d ? 0d : ageDays;
            State = BeePopulationState.Alive;
            Activity = string.Empty;
            Sector = string.Empty;
            Chamber = string.Empty;
            Role = caste.ToString();
        }

        public void ChangeCaste(BeeCaste caste) { Caste = caste; Role = caste.ToString(); }
        public void ChangeState(BeePopulationState state) => State = state;
        public void SetLocation(string sector, string chamber) { Sector = sector ?? string.Empty; Chamber = chamber ?? string.Empty; }
        public void SetActivity(string activity) => Activity = activity ?? string.Empty;
        public void Age(double days) => AgeDays += Math.Max(0d, days);
    }

    public sealed class PopulationStatistics
    {
        public int TotalPopulation { get; }
        public IReadOnlyDictionary<BeeCaste, int> PopulationByCaste { get; }
        public double AverageAge { get; }
        public double LifeExpectancy { get; }
        public int Births { get; }
        public int Deaths { get; }
        public int Growth { get; }
        public double Productivity { get; }

        public PopulationStatistics(int totalPopulation, IReadOnlyDictionary<BeeCaste, int> populationByCaste, double averageAge, double lifeExpectancy, int births, int deaths, double productivity)
        {
            TotalPopulation = totalPopulation;
            PopulationByCaste = populationByCaste;
            AverageAge = averageAge;
            LifeExpectancy = lifeExpectancy;
            Births = births;
            Deaths = deaths;
            Growth = births - deaths;
            Productivity = productivity;
        }
    }

    public sealed class PopulationSnapshot
    {
        public int Version;
        public PopulationRecord[] Records;
    }

    public struct PopulationRecord
    {
        public string BeeId;
        public string DefinitionId;
        public BeeCaste Caste;
        public BeePopulationState State;
        public double AgeDays;
        public string Activity;
        public string Sector;
        public string Chamber;
        public string Role;
    }

    public sealed class PopulationDiagnostics
    {
        public int Registered { get; private set; }
        public int Removed { get; private set; }
        public int StatisticsUpdated { get; private set; }
        public int Snapshots { get; private set; }
        public int Restores { get; private set; }
        public void RecordRegistered() => Registered++;
        public void RecordRemoved() => Removed++;
        public void RecordStatistics() => StatisticsUpdated++;
        public void RecordSnapshot() => Snapshots++;
        public void RecordRestore() => Restores++;
    }

    public sealed class PopulationRegistry
    {
        private readonly Dictionary<string, BeePopulationRecord> bees = new Dictionary<string, BeePopulationRecord>();
        private readonly Dictionary<BeeCaste, HashSet<string>> byCaste = new Dictionary<BeeCaste, HashSet<string>>();
        private readonly Dictionary<BeePopulationState, HashSet<string>> byState = new Dictionary<BeePopulationState, HashSet<string>>();

        public int Count => bees.Count;
        public IReadOnlyDictionary<string, BeePopulationRecord> Bees => bees;

        public bool Register(BeePopulationRecord bee)
        {
            if (bee == null || bees.ContainsKey(bee.BeeId)) return false;
            bees.Add(bee.BeeId, bee);
            Add(byCaste, bee.Caste, bee.BeeId);
            Add(byState, bee.State, bee.BeeId);
            return true;
        }

        public bool Remove(string beeId)
        {
            if (!bees.TryGetValue(beeId, out BeePopulationRecord bee)) return false;
            bees.Remove(beeId);
            Remove(byCaste, bee.Caste, beeId);
            Remove(byState, bee.State, beeId);
            return true;
        }

        public bool ChangeCaste(string beeId, BeeCaste caste)
        {
            if (!bees.TryGetValue(beeId, out BeePopulationRecord bee)) return false;
            Remove(byCaste, bee.Caste, beeId);
            bee.ChangeCaste(caste);
            Add(byCaste, bee.Caste, beeId);
            return true;
        }

        public IReadOnlyList<BeePopulationRecord> QueryByCaste(BeeCaste caste) => Query(byCaste, caste);
        public IReadOnlyList<BeePopulationRecord> QueryByState(BeePopulationState state) => Query(byState, state);
        public IReadOnlyList<BeePopulationRecord> QueryPopulation()
        {
            List<BeePopulationRecord> result = new List<BeePopulationRecord>(bees.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.BeeId, right.BeeId));
            return result;
        }

        private IReadOnlyList<BeePopulationRecord> Query<T>(Dictionary<T, HashSet<string>> index, T key)
        {
            List<BeePopulationRecord> result = new List<BeePopulationRecord>();
            if (!index.TryGetValue(key, out HashSet<string> ids)) return result;
            foreach (string id in ids) result.Add(bees[id]);
            result.Sort((left, right) => string.CompareOrdinal(left.BeeId, right.BeeId));
            return result;
        }

        private static void Add<T>(Dictionary<T, HashSet<string>> index, T key, string id)
        {
            if (!index.TryGetValue(key, out HashSet<string> set)) { set = new HashSet<string>(); index[key] = set; }
            set.Add(id);
        }

        private static void Remove<T>(Dictionary<T, HashSet<string>> index, T key, string id)
        {
            if (index.TryGetValue(key, out HashSet<string> set)) set.Remove(id);
        }
    }

    public sealed class PopulationManager
    {
        private const int SnapshotVersion = 1;
        private readonly PopulationRegistry registry = new PopulationRegistry();
        private readonly Dictionary<string, PopulationDefinition> definitions = new Dictionary<string, PopulationDefinition>();
        private readonly IEventBus eventBus;
        private int births;
        private int deaths;

        public PopulationDiagnostics Diagnostics { get; } = new PopulationDiagnostics();
        public PopulationManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterDefinition(PopulationDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            return true;
        }

        public bool RegisterBee(BeePopulationRecord bee)
        {
            bool registered = registry.Register(bee);
            if (registered) { births++; Diagnostics.RecordRegistered(); eventBus?.Publish(new BeeRegistered(bee.BeeId)); eventBus?.Publish(new PopulationChanged(registry.Count)); }
            return registered;
        }

        public bool UnregisterBee(string beeId)
        {
            bool removed = registry.Remove(beeId);
            if (removed) { deaths++; Diagnostics.RecordRemoved(); eventBus?.Publish(new BeeRemoved(beeId)); eventBus?.Publish(new PopulationChanged(registry.Count)); }
            return removed;
        }

        public IReadOnlyList<BeePopulationRecord> QueryPopulation() => registry.QueryPopulation();
        public IReadOnlyList<BeePopulationRecord> QueryByCaste(BeeCaste caste) => registry.QueryByCaste(caste);
        public IReadOnlyList<BeePopulationRecord> QueryByState(BeePopulationState state) => registry.QueryByState(state);

        public bool ChangeBeeCaste(string beeId, BeeCaste caste)
        {
            bool changed = registry.ChangeCaste(beeId, caste);
            if (changed) eventBus?.Publish(new CasteChanged(beeId, caste));
            return changed;
        }

        public PopulationStatistics QueryStatistics()
        {
            Dictionary<BeeCaste, int> byCaste = new Dictionary<BeeCaste, int>();
            double age = 0d;
            double life = 0d;
            double productivity = 0d;
            foreach (BeePopulationRecord bee in registry.Bees.Values)
            {
                byCaste.TryGetValue(bee.Caste, out int count);
                byCaste[bee.Caste] = count + 1;
                age += bee.AgeDays;
                if (definitions.TryGetValue(bee.DefinitionId, out PopulationDefinition definition))
                {
                    life += definition.LifeExpectancyDays;
                    productivity += definition.Productivity;
                }
            }
            int total = registry.Count;
            Diagnostics.RecordStatistics();
            eventBus?.Publish(new PopulationStatisticsUpdated(total));
            return new PopulationStatistics(total, byCaste, total == 0 ? 0d : age / total, total == 0 ? 0d : life / total, births, deaths, productivity);
        }

        public PopulationSnapshot CreateSnapshot()
        {
            List<PopulationRecord> records = new List<PopulationRecord>();
            foreach (BeePopulationRecord bee in registry.QueryPopulation())
            {
                records.Add(new PopulationRecord { BeeId = bee.BeeId, DefinitionId = bee.DefinitionId, Caste = bee.Caste, State = bee.State, AgeDays = bee.AgeDays, Activity = bee.Activity, Sector = bee.Sector, Chamber = bee.Chamber, Role = bee.Role });
            }
            Diagnostics.RecordSnapshot();
            return new PopulationSnapshot { Version = SnapshotVersion, Records = records.ToArray() };
        }

        public void RestoreSnapshot(PopulationSnapshot snapshot)
        {
            if (snapshot?.Records == null) return;
            for (int i = 0; i < snapshot.Records.Length; i++)
            {
                PopulationRecord record = snapshot.Records[i];
                BeePopulationRecord bee = new BeePopulationRecord(record.BeeId, record.DefinitionId, record.Caste, record.AgeDays);
                bee.ChangeState(record.State);
                bee.SetActivity(record.Activity);
                bee.SetLocation(record.Sector, record.Chamber);
                registry.Register(bee);
            }
            Diagnostics.RecordRestore();
        }
    }

    public readonly struct BeeRegistered : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeRegistered(string beeId) { BeeId = beeId; } }
    public readonly struct BeeRemoved : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeRemoved(string beeId) { BeeId = beeId; } }
    public readonly struct PopulationChanged : IGameplayEvent, IBeeEvent { public int Count { get; } public PopulationChanged(int count) { Count = count; } }
    public readonly struct CasteChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeCaste Caste { get; } public CasteChanged(string beeId, BeeCaste caste) { BeeId = beeId; Caste = caste; } }
    public readonly struct PopulationStatisticsUpdated : IGameplayEvent, IBeeEvent { public int Count { get; } public PopulationStatisticsUpdated(int count) { Count = count; } }
}
