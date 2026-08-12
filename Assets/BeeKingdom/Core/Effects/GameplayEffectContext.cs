using System.Collections.Generic;

namespace BeeKingdom.Core.Effects
{
    public sealed class GameplayEffectContext
    {
        private readonly Dictionary<string, string> parameters;

        public string Source { get; }
        public string Target { get; }
        public string WorldId { get; }
        public string RegionId { get; }
        public string PlayerId { get; }
        public string AllianceId { get; }
        public double SimulationTimeSeconds { get; }
        public int Seed { get; }
        public IReadOnlyDictionary<string, string> Parameters => parameters;

        public GameplayEffectContext(string source, string target, string worldId, string regionId, string playerId, string allianceId, double simulationTimeSeconds, int seed, IReadOnlyDictionary<string, string> parameters = null)
        {
            Source = source ?? string.Empty;
            Target = target ?? string.Empty;
            WorldId = worldId ?? string.Empty;
            RegionId = regionId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
            AllianceId = allianceId ?? string.Empty;
            SimulationTimeSeconds = simulationTimeSeconds < 0d ? 0d : simulationTimeSeconds;
            Seed = seed;
            this.parameters = new Dictionary<string, string>(parameters ?? new Dictionary<string, string>());
        }
    }
}
