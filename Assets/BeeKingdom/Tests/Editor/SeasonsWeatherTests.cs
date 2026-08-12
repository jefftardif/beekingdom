using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SeasonsWeatherTests
    {
        [Test]
        public void SeasonManagerAdvancesByConfiguredLength()
        {
            SeasonManager manager = new SeasonManager(10d);

            manager.Execute(SimulationContextFactory.Create(10d));

            Assert.That(manager.CurrentSeason, Is.EqualTo(SimulationSeason.Summer));
        }

        [Test]
        public void WeatherManagerIsDeterministicForSameSeed()
        {
            WeatherManager first = new WeatherManager(new WorldSeed("weather"), weatherDurationSeconds: 10d);
            WeatherManager second = new WeatherManager(new WorldSeed("weather"), weatherDurationSeconds: 10d);

            first.Execute(SimulationContextFactory.Create(10d));
            second.Execute(SimulationContextFactory.Create(10d));

            Assert.That(first.CurrentWeather, Is.EqualTo(second.CurrentWeather));
        }

        [Test]
        public void WeatherProfileSelectsWeightedWeather()
        {
            WeatherProfile profile = WeatherProfile.Temperate();

            Assert.That(profile.Select(0d), Is.EqualTo(WorldWeather.Clear));
            Assert.That(profile.Select(0.99d), Is.EqualTo(WorldWeather.Storm));
        }

        [Test]
        public void ClimateRulesModifyProductionMovementAndConsumption()
        {
            ClimateRules rules = ClimateRules.CreateDefault();

            Assert.That(rules.GetProductionModifier(SimulationSeason.Spring), Is.GreaterThan(rules.GetProductionModifier(SimulationSeason.Winter)));
            Assert.That(rules.GetMovementModifier(WorldWeather.Storm), Is.LessThan(1d));
            Assert.That(rules.GetConsumptionModifier(SimulationSeason.Winter), Is.GreaterThan(1d));
        }

        [Test]
        public void ManualWeatherChangeUpdatesCurrentWeather()
        {
            WeatherManager manager = new WeatherManager(new WorldSeed("manual"));

            manager.SetWeather(WorldWeather.Rain);

            Assert.That(manager.CurrentWeather, Is.EqualTo(WorldWeather.Rain));
            Assert.That(manager.GetMovementModifier(), Is.LessThan(1d));
        }
    }
}
