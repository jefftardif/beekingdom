using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WeatherProfile
    {
        private readonly Dictionary<WorldWeather, double> weights;

        public string ProfileId { get; }
        public IReadOnlyDictionary<WorldWeather, double> Weights => weights;

        public WeatherProfile(string profileId, IReadOnlyDictionary<WorldWeather, double> weights)
        {
            ProfileId = string.IsNullOrWhiteSpace(profileId) ? "weather" : profileId;
            this.weights = new Dictionary<WorldWeather, double>(weights ?? new Dictionary<WorldWeather, double>());
        }

        public WorldWeather Select(double normalized)
        {
            double total = 0d;
            foreach (double weight in weights.Values)
            {
                total += weight;
            }

            if (total <= 0d)
            {
                return WorldWeather.Clear;
            }

            double cursor = normalized * total;
            foreach (var pair in weights)
            {
                cursor -= pair.Value;
                if (cursor <= 0d)
                {
                    return pair.Key;
                }
            }

            return WorldWeather.Clear;
        }

        public static WeatherProfile Temperate()
        {
            return new WeatherProfile("temperate", new Dictionary<WorldWeather, double>
            {
                { WorldWeather.Clear, 45d },
                { WorldWeather.Cloudy, 25d },
                { WorldWeather.Rain, 20d },
                { WorldWeather.Wind, 8d },
                { WorldWeather.Storm, 2d }
            });
        }
    }
}
