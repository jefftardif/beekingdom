using System;
using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class QueenAggregate
    {
        private readonly Dictionary<QueenBonusType, float> bonuses = new Dictionary<QueenBonusType, float>();

        public string QueenId { get; }
        public string HiveId { get; }
        public double AgeSeconds { get; private set; }
        public QueenState State { get; private set; }
        public QueenHealth Health { get; private set; }
        public int Energy { get; private set; }
        public float Fertility { get; private set; }
        public QueenEggProduction EggProduction { get; }
        public QueenEvolution Evolution { get; }
        public int BonusCount => bonuses.Count;

        public QueenAggregate(string queenId, string hiveId, QueenHealth health, int energy, float fertility, QueenEggProduction eggProduction, QueenEvolution evolution)
        {
            QueenId = Require(queenId, nameof(queenId));
            HiveId = Require(hiveId, nameof(hiveId));
            Health = health;
            Energy = Clamp(energy, 0, 100);
            Fertility = fertility < 0f ? 0f : fertility;
            EggProduction = eggProduction ?? throw new ArgumentNullException(nameof(eggProduction));
            Evolution = evolution ?? throw new ArgumentNullException(nameof(evolution));
            State = QueenState.Egg;
        }

        public bool UpdateState(QueenState nextState)
        {
            if (!CanTransition(State, nextState))
            {
                return false;
            }

            State = nextState;
            return true;
        }

        public int ProduceEggs(double deltaSeconds, float seasonModifier = 1f, float researchModifier = 1f)
        {
            if (State != QueenState.ActiveQueen && State != QueenState.Swarming)
            {
                return 0;
            }

            float productionBonus = GetBonus(QueenBonusType.Production);
            return EggProduction.Produce(deltaSeconds, Health, Energy, Fertility, seasonModifier, 1f + productionBonus + (researchModifier - 1f), Evolution.Level);
        }

        public bool AddExperience(int amount)
        {
            return Evolution.AddExperience(amount);
        }

        public void ApplyBonus(QueenBonusType type, float value)
        {
            bonuses[type] = value;
        }

        public float GetBonus(QueenBonusType type)
        {
            return bonuses.TryGetValue(type, out float value) ? value : 0f;
        }

        public QueenStatistics GetStatistics()
        {
            return new QueenStatistics(Evolution.Level, Evolution.Experience, State, Health, Energy, Fertility, EggProduction.TotalProduced);
        }

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(QueenId) &&
                !string.IsNullOrWhiteSpace(HiveId) &&
                Fertility >= 0f &&
                Energy >= 0 &&
                !Health.IsDead == (State != QueenState.Dead);
        }

        public void Age(double seconds)
        {
            if (seconds > 0d)
            {
                AgeSeconds += seconds;
            }
        }

        public QueenSnapshot ToSnapshot()
        {
            return new QueenSnapshot
            {
                QueenId = QueenId,
                HiveId = HiveId,
                AgeSeconds = AgeSeconds,
                State = State,
                Health = Health.Current,
                MaxHealth = Health.Maximum,
                Energy = Energy,
                Fertility = Fertility,
                Level = Evolution.Level,
                Experience = Evolution.Experience,
                BaseEggsPerMinute = EggProduction.BaseEggsPerMinute
            };
        }

        public static QueenAggregate FromSnapshot(QueenSnapshot snapshot)
        {
            QueenAggregate queen = new QueenAggregate(
                snapshot.QueenId,
                snapshot.HiveId,
                new QueenHealth(snapshot.Health, snapshot.MaxHealth),
                snapshot.Energy,
                snapshot.Fertility,
                new QueenEggProduction(snapshot.BaseEggsPerMinute),
                new QueenEvolution());

            queen.AgeSeconds = snapshot.AgeSeconds;
            queen.State = snapshot.State;
            queen.Evolution.Load(snapshot.Level, snapshot.Experience);
            return queen;
        }

        private static bool CanTransition(QueenState current, QueenState next)
        {
            if (current == next)
            {
                return true;
            }

            if (current == QueenState.Dead)
            {
                return false;
            }

            if (next == QueenState.Dead || next == QueenState.Injured)
            {
                return true;
            }

            if (current == QueenState.Injured)
            {
                return next == QueenState.ActiveQueen || next == QueenState.Dead;
            }

            return current == QueenState.Egg && next == QueenState.Larva ||
                current == QueenState.Larva && next == QueenState.Pupa ||
                current == QueenState.Pupa && next == QueenState.VirginQueen ||
                current == QueenState.VirginQueen && next == QueenState.MatedQueen ||
                current == QueenState.MatedQueen && next == QueenState.ActiveQueen ||
                current == QueenState.ActiveQueen && next == QueenState.Swarming ||
                current == QueenState.Swarming && next == QueenState.ActiveQueen;
        }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", name);
            }

            return value;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
