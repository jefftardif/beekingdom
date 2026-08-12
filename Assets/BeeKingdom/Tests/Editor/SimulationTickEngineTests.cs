using BeeKingdom.Core.Simulation;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationTickEngineTests
    {
        [Test]
        public void FixedTickAccumulatesDeterministically()
        {
            SimulationTickEngine engine = new SimulationTickEngine(0.1d);

            Assert.That(engine.Advance(0.35d, SimulationTickMode.Fixed), Is.EqualTo(3));
            Assert.That(engine.TickIndex, Is.EqualTo(3));
        }

        [Test]
        public void VariableTickCreatesOneScaledTick()
        {
            SimulationTickEngine engine = new SimulationTickEngine();
            engine.SetTimeScale(2d);

            Assert.That(engine.Advance(1d, SimulationTickMode.Variable), Is.EqualTo(1));
            Assert.That(engine.TotalSeconds, Is.EqualTo(2d));
        }

        [Test]
        public void PauseStopsTicks()
        {
            SimulationTickEngine engine = new SimulationTickEngine();
            engine.SetPaused(true);

            Assert.That(engine.Advance(10d, SimulationTickMode.Fixed), Is.EqualTo(0));
        }

        [Test]
        public void FastForwardUsesFixedSteps()
        {
            SimulationTickEngine engine = new SimulationTickEngine(1d);

            Assert.That(engine.FastForward(10d), Is.EqualTo(10));
        }
    }
}
