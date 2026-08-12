using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum QueenState { Egg, Virgin, Mating, YoungQueen, MatureQueen, AgingQueen, Weak, Sick, Dying, Dead }
    public enum QueenPheromoneType { RoyalPresence, BroodSignal, AlarmResponse, ExpansionSignal, SwarmPreparation }

    public sealed class QueenDefinition
    {
        public string DefinitionId { get; }
        public double Health { get; }
        public double Vitality { get; }
        public double Fertility { get; }
        public double EggProductionRate { get; }
        public double LongevityDays { get; }
        public double Leadership { get; }
        public double Genetics { get; }
        public double Stress { get; }
        public double Nutrition { get; }

        public QueenDefinition(string definitionId, double health, double vitality, double fertility, double eggProductionRate, double longevityDays, double leadership, double genetics, double stress = 0d, double nutrition = 1d)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Health = Clamp01(health);
            Vitality = Clamp01(vitality);
            Fertility = Clamp01(fertility);
            EggProductionRate = eggProductionRate < 0d ? 0d : eggProductionRate;
            LongevityDays = longevityDays <= 0d ? 1d : longevityDays;
            Leadership = Clamp01(leadership);
            Genetics = Clamp01(genetics);
            Stress = Clamp01(stress);
            Nutrition = Clamp01(nutrition);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class QueenHistory
    {
        private readonly List<string> events = new List<string>();

        public double BirthDay { get; }
        public string Origin { get; }
        public int EggsLaid { get; private set; }
        public int Descendants { get; private set; }
        public IReadOnlyList<string> Events => events;

        public QueenHistory(double birthDay, string origin)
        {
            BirthDay = birthDay;
            Origin = origin ?? string.Empty;
        }

        public void RecordEvent(string eventId) => events.Add(eventId ?? string.Empty);
        public void RecordEggs(int count) => EggsLaid += count < 0 ? 0 : count;
        public void RecordDescendants(int count) => Descendants += count < 0 ? 0 : count;
    }

    public sealed class QueenInstance
    {
        private readonly HashSet<QueenPheromoneType> activePheromones = new HashSet<QueenPheromoneType>();

        public string QueenId { get; }
        public string DefinitionId { get; }
        public QueenState State { get; private set; }
        public QueenDefinition Definition { get; }
        public QueenHistory History { get; }
        public IReadOnlyCollection<QueenPheromoneType> ActivePheromones => activePheromones;
        public bool IsAlive => State != QueenState.Dead;

        public QueenInstance(string queenId, QueenDefinition definition, double birthDay, string origin)
        {
            QueenId = string.IsNullOrWhiteSpace(queenId) ? throw new ArgumentException("Queen id is required.", nameof(queenId)) : queenId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            DefinitionId = definition.DefinitionId;
            State = QueenState.Egg;
            History = new QueenHistory(birthDay, origin);
        }

        public void ChangeState(QueenState state)
        {
            State = state;
            History.RecordEvent("state:" + state);
        }

        public double CalculateGrowthBonus() => Definition.Vitality * Definition.Leadership * (1d - Definition.Stress);
        public double CalculateReproductionBonus() => Definition.Fertility * Definition.EggProductionRate * Definition.Nutrition;
        public bool ActivatePheromone(QueenPheromoneType pheromone) => activePheromones.Add(pheromone);
        public bool DeactivatePheromone(QueenPheromoneType pheromone) => activePheromones.Remove(pheromone);
    }

    public sealed class QueenRegistry
    {
        private readonly Dictionary<string, QueenInstance> queens = new Dictionary<string, QueenInstance>();
        public int Count => queens.Count;
        public bool RegisterQueen(QueenInstance queen)
        {
            if (queen == null || queens.ContainsKey(queen.QueenId)) return false;
            queens.Add(queen.QueenId, queen);
            return true;
        }
        public bool TryGetQueen(string queenId, out QueenInstance queen) => queens.TryGetValue(queenId, out queen);
        public IReadOnlyList<QueenInstance> QueryQueens()
        {
            List<QueenInstance> result = new List<QueenInstance>(queens.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.QueenId, right.QueenId));
            return result;
        }
        public void Clear() => queens.Clear();
    }

    public sealed class QueenDiagnostics
    {
        public int Registered { get; private set; }
        public int StateChanges { get; private set; }
        public int EffectsApplied { get; private set; }
        public int Snapshots { get; private set; }
        public int Restores { get; private set; }
        public void RecordRegistered() => Registered++;
        public void RecordStateChange() => StateChanges++;
        public void RecordEffect() => EffectsApplied++;
        public void RecordSnapshot() => Snapshots++;
        public void RecordRestore() => Restores++;
    }

    public sealed class QueenSnapshot
    {
        public int Version;
        public QueenRecord[] Queens;
    }

    public struct QueenRecord
    {
        public string QueenId;
        public string DefinitionId;
        public QueenState State;
        public double BirthDay;
        public string Origin;
        public int EggsLaid;
        public int Descendants;
    }

    public sealed class QueenManager
    {
        private const int SnapshotVersion = 1;

        private readonly Dictionary<string, QueenDefinition> definitions = new Dictionary<string, QueenDefinition>();
        private readonly QueenRegistry registry = new QueenRegistry();
        private readonly IEventBus eventBus;

        public QueenDiagnostics Diagnostics { get; } = new QueenDiagnostics();

        public QueenManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(QueenDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            return true;
        }

        public QueenInstance RegisterQueen(string queenId, string definitionId, double birthDay = 0d, string origin = "")
        {
            if (!definitions.TryGetValue(definitionId, out QueenDefinition definition)) return null;
            QueenInstance queen = new QueenInstance(queenId, definition, birthDay, origin);
            if (!registry.RegisterQueen(queen)) return null;
            Diagnostics.RecordRegistered();
            eventBus?.Publish(new QueenBorn(queenId));
            return queen;
        }

        public QueenInstance GetQueen(string queenId) => registry.TryGetQueen(queenId, out QueenInstance queen) ? queen : null;
        public QueenState QueryQueenStatus(string queenId) => GetQueen(queenId)?.State ?? QueenState.Dead;

        public bool ChangeQueenState(string queenId, QueenState state)
        {
            QueenInstance queen = GetQueen(queenId);
            if (queen == null) return false;
            QueenState previous = queen.State;
            queen.ChangeState(state);
            Diagnostics.RecordStateChange();
            eventBus?.Publish(new QueenStateChanged(queenId, state));
            if (state == QueenState.MatureQueen && previous != QueenState.MatureQueen) eventBus?.Publish(new QueenMatured(queenId));
            if (state == QueenState.Weak || state == QueenState.Sick || state == QueenState.Dying) eventBus?.Publish(new QueenInjured(queenId));
            if (previous == QueenState.Sick && state == QueenState.MatureQueen) eventBus?.Publish(new QueenRecovered(queenId));
            if (state == QueenState.Dead) eventBus?.Publish(new QueenDied(queenId));
            return true;
        }

        public bool ApplyQueenEffect(string queenId, QueenPheromoneType pheromone)
        {
            QueenInstance queen = GetQueen(queenId);
            if (queen == null) return false;
            bool applied = queen.ActivatePheromone(pheromone);
            if (applied) Diagnostics.RecordEffect();
            return applied;
        }

        public QueenSnapshot CreateSnapshot()
        {
            List<QueenRecord> records = new List<QueenRecord>();
            foreach (QueenInstance queen in registry.QueryQueens())
            {
                records.Add(new QueenRecord
                {
                    QueenId = queen.QueenId,
                    DefinitionId = queen.DefinitionId,
                    State = queen.State,
                    BirthDay = queen.History.BirthDay,
                    Origin = queen.History.Origin,
                    EggsLaid = queen.History.EggsLaid,
                    Descendants = queen.History.Descendants
                });
            }
            Diagnostics.RecordSnapshot();
            return new QueenSnapshot { Version = SnapshotVersion, Queens = records.ToArray() };
        }

        public void RestoreSnapshot(QueenSnapshot snapshot)
        {
            registry.Clear();
            if (snapshot?.Queens == null) return;
            for (int i = 0; i < snapshot.Queens.Length; i++)
            {
                QueenRecord record = snapshot.Queens[i];
                if (!definitions.TryGetValue(record.DefinitionId, out QueenDefinition definition)) continue;
                QueenInstance queen = new QueenInstance(record.QueenId, definition, record.BirthDay, record.Origin);
                queen.ChangeState(record.State);
                queen.History.RecordEggs(record.EggsLaid);
                queen.History.RecordDescendants(record.Descendants);
                registry.RegisterQueen(queen);
            }
            Diagnostics.RecordRestore();
        }
    }

    public readonly struct QueenBorn : IGameplayEvent, IBeeEvent { public string QueenId { get; } public QueenBorn(string queenId) { QueenId = queenId; } }
    public readonly struct QueenMatured : IGameplayEvent, IBeeEvent { public string QueenId { get; } public QueenMatured(string queenId) { QueenId = queenId; } }
    public readonly struct QueenStateChanged : IGameplayEvent, IBeeEvent { public string QueenId { get; } public QueenState State { get; } public QueenStateChanged(string queenId, QueenState state) { QueenId = queenId; State = state; } }
    public readonly struct QueenInjured : IGameplayEvent, IBeeEvent { public string QueenId { get; } public QueenInjured(string queenId) { QueenId = queenId; } }
    public readonly struct QueenRecovered : IGameplayEvent, IBeeEvent { public string QueenId { get; } public QueenRecovered(string queenId) { QueenId = queenId; } }
    public readonly struct QueenDied : IGameplayEvent, IBeeEvent { public string QueenId { get; } public QueenDied(string queenId) { QueenId = queenId; } }
    public readonly struct QueenReplaced : IGameplayEvent, IBeeEvent { public string OldQueenId { get; } public string NewQueenId { get; } public QueenReplaced(string oldQueenId, string newQueenId) { OldQueenId = oldQueenId; NewQueenId = newQueenId; } }
}
