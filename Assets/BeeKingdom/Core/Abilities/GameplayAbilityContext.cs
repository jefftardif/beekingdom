using System.Collections.Generic;

namespace BeeKingdom.Core.Abilities
{
    public sealed class GameplayAbilityContext
    {
        private readonly List<string> targets;
        private readonly Dictionary<string, string> parameters;

        public string Source { get; }
        public IReadOnlyList<string> Targets => targets;
        public string WorldId { get; }
        public double SimulationTimeSeconds { get; }
        public int Seed { get; }
        public string ZoneId { get; }
        public string AllianceId { get; }
        public string PlayerId { get; }
        public GameplayAbilityActivationSource ActivationSource { get; }
        public IReadOnlyDictionary<string, string> Parameters => parameters;

        public GameplayAbilityContext(
            string source,
            IReadOnlyList<string> targets,
            string worldId,
            double simulationTimeSeconds,
            int seed,
            string zoneId,
            string allianceId,
            string playerId,
            GameplayAbilityActivationSource activationSource,
            IReadOnlyDictionary<string, string> parameters = null)
        {
            Source = source ?? string.Empty;
            this.targets = new List<string>(targets ?? new string[0]);
            WorldId = worldId ?? string.Empty;
            SimulationTimeSeconds = simulationTimeSeconds < 0d ? 0d : simulationTimeSeconds;
            Seed = seed;
            ZoneId = zoneId ?? string.Empty;
            AllianceId = allianceId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
            ActivationSource = activationSource;
            this.parameters = new Dictionary<string, string>(parameters ?? new Dictionary<string, string>());
        }
    }
}
