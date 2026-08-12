using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Time;

namespace BeeKingdom.Services
{
    public sealed class UnityTimeService : GameServiceBase, ITimeService
    {
        private const double SecondsPerMinute = 60d;
        private const double SecondsPerHour = 3600d;
        private const double SecondsPerDay = 86400d;
        private const int DaysPerSeason = 30;

        private readonly SimulationClock clock = new SimulationClock();
        private readonly SimulationTickScheduler scheduler = new SimulationTickScheduler();
        private IEventBus eventBus;
        private float deltaTime;
        private float unscaledDeltaTime;
        private bool isPaused;
        private SimulationTimeScale timeScale = new SimulationTimeScale(1f);
        private double maxOfflineSeconds = 8d * SecondsPerHour;
        private long frameTicks;
        private SimulationSeason lastSeason = SimulationSeason.Spring;

        public override int Priority => 30;
        public override IReadOnlyList<Type> Dependencies => new[] { typeof(IEventBus) };

        public float DeltaTime => deltaTime;
        public float UnscaledDeltaTime => unscaledDeltaTime;
        public float Time => (float)clock.Timestamp.TotalSeconds;
        public bool IsPaused => isPaused;
        public SimulationTimeScale TimeScale => timeScale;
        public SimulationTimestamp Timestamp => clock.Timestamp;
        public SimulationCalendar Calendar => BuildCalendar(clock.Timestamp.TotalSeconds);
        public TimeDiagnostics Diagnostics => new TimeDiagnostics(frameTicks, scheduler.TenHzTicks, scheduler.FiveHzTicks, scheduler.OneHzTicks, clock.Timestamp.TotalSeconds);

        protected override void OnInitialize(IServiceRegistry services)
        {
            eventBus = services.Get<IEventBus>();
        }

        protected override void OnTick(float frameDeltaTime)
        {
            unscaledDeltaTime = frameDeltaTime;
            if (isPaused)
            {
                deltaTime = 0f;
                return;
            }

            double scaledDelta = frameDeltaTime * timeScale.Value;
            deltaTime = (float)scaledDelta;
            SimulationTimestamp timestamp = clock.Advance(scaledDelta);
            SimulationCalendar calendar = BuildCalendar(timestamp.TotalSeconds);
            frameTicks++;

            eventBus.Publish(new TickGenerated(SimulationTickFrequency.EveryFrame, timestamp, scaledDelta));
            scheduler.Advance(scaledDelta, timestamp, calendar, eventBus);

            if (calendar.Season != lastSeason)
            {
                lastSeason = calendar.Season;
                eventBus.Publish(new SeasonChanged(calendar.Season));
            }
        }

        public void SetTimeScale(float newTimeScale)
        {
            timeScale = new SimulationTimeScale(newTimeScale);
            eventBus?.Publish(new TimeScaleChanged(timeScale));
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused)
            {
                return;
            }

            isPaused = paused;
            eventBus?.Publish(new PauseStateChanged(isPaused));
        }

        public void SetMaxOfflineSeconds(double seconds)
        {
            maxOfflineSeconds = seconds < 0d ? 0d : seconds;
        }

        public OfflineTimeResult CalculateOfflineTime(DateTime previousUtc, DateTime currentUtc)
        {
            if (currentUtc < previousUtc)
            {
                return new OfflineTimeResult(0d, 0d);
            }

            double rawSeconds = (currentUtc - previousUtc).TotalSeconds;
            double cappedSeconds = rawSeconds > maxOfflineSeconds ? maxOfflineSeconds : rawSeconds;
            return new OfflineTimeResult(rawSeconds, cappedSeconds);
        }

        protected override void OnPause()
        {
            SetPaused(true);
        }

        protected override void OnResume()
        {
            SetPaused(false);
        }

        private static SimulationCalendar BuildCalendar(double totalSeconds)
        {
            int totalMinutes = (int)(totalSeconds / SecondsPerMinute);
            int totalHours = (int)(totalSeconds / SecondsPerHour);
            int day = (int)(totalSeconds / SecondsPerDay) + 1;
            int hour = totalHours % 24;
            int minute = totalMinutes % 60;
            int seasonIndex = ((day - 1) / DaysPerSeason) % 4;
            SimulationSeason season = seasonIndex == 0 ? SimulationSeason.Spring :
                seasonIndex == 1 ? SimulationSeason.Summer :
                seasonIndex == 2 ? SimulationSeason.Autumn :
                SimulationSeason.Winter;

            return new SimulationCalendar(day, hour, minute, season);
        }
    }
}
