using BeeKingdom.Core.Time;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationTimeSystemTests
    {
        [Test]
        public void AdvancesHourAndDayDeterministically()
        {
            SimulationTimeSystem time = new SimulationTimeSystem();

            SimulationTimePoint point = time.Advance(90000d);

            Assert.That(point.DayOfSeason, Is.EqualTo(2));
            Assert.That(point.Hour, Is.EqualTo(1));
        }

        [Test]
        public void TimeScaleAcceleratesTime()
        {
            SimulationTimeSystem time = new SimulationTimeSystem();
            time.SetTimeScale(10d);

            Assert.That(time.Advance(60d).TotalSeconds, Is.EqualTo(600d));
        }

        [Test]
        public void PauseStopsTime()
        {
            SimulationTimeSystem time = new SimulationTimeSystem();
            time.SetPaused(true);

            Assert.That(time.Advance(60d).TotalSeconds, Is.EqualTo(0d));
        }

        [Test]
        public void LiveOpsWindowUsesCanonicalSeconds()
        {
            SimulationTimeSystem time = new SimulationTimeSystem();
            time.Advance(100d);

            Assert.That(time.IsLiveOpsWindowActive(new LiveOpsCalendarWindow(50d, 150d)), Is.True);
        }
    }
}
