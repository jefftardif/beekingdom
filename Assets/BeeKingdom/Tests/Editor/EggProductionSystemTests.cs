using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class EggProductionSystemTests
    {
        [Test]
        public void ProduceEggRegistersIncubatingEgg()
        {
            EggProductionManager manager = CreateManager(out QueenManager queenManager, out PopulationManager populationManager);
            EggProductionContext context = new EggProductionContext("queen-1", 12, 0);

            EggProductionRecord egg = manager.ProduceEgg("egg", context, 4d);

            Assert.That(egg, Is.Not.Null);
            Assert.That(egg.State, Is.EqualTo(EggProductionState.Incubation));
            Assert.That(queenManager.GetQueen("queen-1").History.EggsLaid, Is.EqualTo(1));
            Assert.That(populationManager.QueryByCaste(BeeCaste.Egg).Count, Is.EqualTo(1));
        }

        [Test]
        public void PauseAndResumeControlProduction()
        {
            EggProductionManager manager = CreateManager(out _, out _);
            EggProductionContext context = new EggProductionContext("queen-1", 12, 0);

            manager.PauseEggProduction();
            Assert.That(manager.ScheduleEggProduction("egg", context), Is.Null);
            Assert.That(manager.Diagnostics.LastBlockReason, Is.EqualTo(EggProductionBlockReason.Paused));

            manager.ResumeEggProduction();
            Assert.That(manager.ScheduleEggProduction("egg", context), Is.Not.Null);
        }

        [Test]
        public void DemographicLimitsBlockProduction()
        {
            EggProductionManager manager = CreateManager(out _, out _);
            EggProductionContext context = new EggProductionContext("queen-1", 20, 0);

            Assert.That(manager.ProduceEgg("egg", context), Is.Null);
            Assert.That(manager.Diagnostics.LastBlockReason, Is.EqualTo(EggProductionBlockReason.DemographicLimit));
        }

        [Test]
        public void EggRateIsDeterministic()
        {
            EggProductionManager manager = CreateManager(out QueenManager queenManager, out _);
            queenManager.ApplyQueenEffect("queen-1", QueenPheromoneType.BroodSignal);
            EggProductionContext context = new EggProductionContext("queen-1", 3, 0, 0.9d, 0.8d, 0.7d, 1d);

            double first = manager.CalculateEggRate("egg", context);
            double second = manager.CalculateEggRate("egg", context);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.GreaterThan(0d));
        }

        private static EggProductionManager CreateManager(out QueenManager queenManager, out PopulationManager populationManager)
        {
            queenManager = new QueenManager();
            queenManager.RegisterDefinition(new QueenDefinition("queen", 0.9d, 0.8d, 0.75d, 2d, 90d, 0.8d, 0.7d, 0.1d, 0.9d));
            queenManager.RegisterQueen("queen-1", "queen");
            queenManager.ChangeQueenState("queen-1", QueenState.MatureQueen);

            populationManager = new PopulationManager();
            populationManager.RegisterDefinition(new PopulationDefinition("egg", BeeCaste.Egg, 1d, 0d));

            EggProductionManager manager = new EggProductionManager(queenManager, populationManager);
            manager.RegisterDefinition(new EggProductionDefinition("egg", 6d, 0.4d, 0.25d, 0.2d, 0.15d, 1d, 1d, 1d, 20, 10, 0.25d));
            return manager;
        }
    }
}
