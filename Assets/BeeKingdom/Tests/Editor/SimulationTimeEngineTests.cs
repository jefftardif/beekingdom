using System;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Time;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationTimeEngineTests
    {
        [Test]
        public void TickAdvancesSimulationTimeAndPublishesFrameEvent()
        {
            UnityTimeService timeService = CreateStartedTimeService(out EventBus eventBus);
            int frameEvents = 0;
            eventBus.Subscribe<TickGenerated>(evt =>
            {
                if (evt.Frequency == SimulationTickFrequency.EveryFrame)
                {
                    frameEvents++;
                }
            });

            timeService.Tick(0.5f);

            Assert.That(timeService.Timestamp.TotalSeconds, Is.EqualTo(0.5d).Within(0.0001d));
            Assert.That(frameEvents, Is.EqualTo(1));
        }

        [Test]
        public void PauseStopsTimeAndResumeContinues()
        {
            UnityTimeService timeService = CreateStartedTimeService(out _);

            timeService.SetPaused(true);
            timeService.Tick(1f);
            Assert.That(timeService.Timestamp.TotalSeconds, Is.EqualTo(0d));

            timeService.SetPaused(false);
            timeService.Tick(1f);
            Assert.That(timeService.Timestamp.TotalSeconds, Is.EqualTo(1d).Within(0.0001d));
        }

        [Test]
        public void TimeScaleAcceleratesSimulation()
        {
            UnityTimeService timeService = CreateStartedTimeService(out EventBus eventBus);
            int scaleEvents = 0;
            eventBus.Subscribe<TimeScaleChanged>(_ => scaleEvents++);

            timeService.SetTimeScale(4f);
            timeService.Tick(0.5f);

            Assert.That(scaleEvents, Is.EqualTo(1));
            Assert.That(timeService.Timestamp.TotalSeconds, Is.EqualTo(2d).Within(0.0001d));
        }

        [Test]
        public void OfflineTimeIsCapped()
        {
            UnityTimeService timeService = CreateStartedTimeService(out _);
            timeService.SetMaxOfflineSeconds(60d);

            OfflineTimeResult result = timeService.CalculateOfflineTime(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc)
            );

            Assert.That(result.RawSeconds, Is.EqualTo(3600d));
            Assert.That(result.CappedSeconds, Is.EqualTo(60d));
            Assert.That(result.WasCapped, Is.True);
        }

        [Test]
        public void SchedulerPublishesOneHzTicksStably()
        {
            UnityTimeService timeService = CreateStartedTimeService(out EventBus eventBus);
            int oneHzTicks = 0;
            eventBus.Subscribe<TickGenerated>(evt =>
            {
                if (evt.Frequency == SimulationTickFrequency.OneHz)
                {
                    oneHzTicks++;
                }
            });

            for (int i = 0; i < 10; i++)
            {
                timeService.Tick(0.1f);
            }

            Assert.That(oneHzTicks, Is.EqualTo(1));
            Assert.That(timeService.Diagnostics.OneHzTicks, Is.EqualTo(1));
        }

        private static UnityTimeService CreateStartedTimeService(out EventBus eventBus)
        {
            ServiceContainer container = new ServiceContainer();
            eventBus = new EventBus();
            eventBus.Initialize(container);
            eventBus.Start();
            container.Register<IEventBus>(eventBus);

            UnityTimeService timeService = new UnityTimeService();
            timeService.Initialize(container);
            timeService.Start();
            return timeService;
        }
    }
}
