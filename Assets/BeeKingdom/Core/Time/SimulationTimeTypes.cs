using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Time
{
    public enum SimulationSeason
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public enum SimulationTickFrequency
    {
        EveryFrame,
        TenHz,
        FiveHz,
        OneHz,
        SimulationMinute,
        SimulationHour,
        SimulationDay
    }

    public readonly struct SimulationTimestamp
    {
        public long TickIndex { get; }
        public double TotalSeconds { get; }

        public SimulationTimestamp(long tickIndex, double totalSeconds)
        {
            TickIndex = tickIndex;
            TotalSeconds = totalSeconds;
        }
    }

    public readonly struct SimulationCalendar
    {
        public int Day { get; }
        public int Hour { get; }
        public int Minute { get; }
        public SimulationSeason Season { get; }

        public SimulationCalendar(int day, int hour, int minute, SimulationSeason season)
        {
            Day = day;
            Hour = hour;
            Minute = minute;
            Season = season;
        }
    }

    public readonly struct SimulationTimeScale
    {
        public float Value { get; }

        public SimulationTimeScale(float value)
        {
            Value = value < 0f ? 0f : value;
        }
    }

    public readonly struct TimeDiagnostics
    {
        public long FrameTicks { get; }
        public long TenHzTicks { get; }
        public long FiveHzTicks { get; }
        public long OneHzTicks { get; }
        public double TotalSimulatedSeconds { get; }

        public TimeDiagnostics(long frameTicks, long tenHzTicks, long fiveHzTicks, long oneHzTicks, double totalSimulatedSeconds)
        {
            FrameTicks = frameTicks;
            TenHzTicks = tenHzTicks;
            FiveHzTicks = fiveHzTicks;
            OneHzTicks = oneHzTicks;
            TotalSimulatedSeconds = totalSimulatedSeconds;
        }
    }

    public readonly struct OfflineTimeResult
    {
        public double RawSeconds { get; }
        public double CappedSeconds { get; }
        public bool WasCapped { get; }

        public OfflineTimeResult(double rawSeconds, double cappedSeconds)
        {
            RawSeconds = rawSeconds;
            CappedSeconds = cappedSeconds;
            WasCapped = cappedSeconds < rawSeconds;
        }
    }

    public readonly struct TickGenerated : IGameplayEvent
    {
        public SimulationTickFrequency Frequency { get; }
        public SimulationTimestamp Timestamp { get; }
        public double DeltaSeconds { get; }

        public TickGenerated(SimulationTickFrequency frequency, SimulationTimestamp timestamp, double deltaSeconds)
        {
            Frequency = frequency;
            Timestamp = timestamp;
            DeltaSeconds = deltaSeconds;
        }
    }

    public readonly struct MinuteElapsed : IGameplayEvent
    {
        public SimulationCalendar Calendar { get; }
        public MinuteElapsed(SimulationCalendar calendar) { Calendar = calendar; }
    }

    public readonly struct HourElapsed : IGameplayEvent
    {
        public SimulationCalendar Calendar { get; }
        public HourElapsed(SimulationCalendar calendar) { Calendar = calendar; }
    }

    public readonly struct DayElapsed : IGameplayEvent
    {
        public SimulationCalendar Calendar { get; }
        public DayElapsed(SimulationCalendar calendar) { Calendar = calendar; }
    }

    public readonly struct SeasonChanged : IGameplayEvent
    {
        public SimulationSeason Season { get; }
        public SeasonChanged(SimulationSeason season) { Season = season; }
    }

    public readonly struct TimeScaleChanged : IGameplayEvent
    {
        public SimulationTimeScale TimeScale { get; }
        public TimeScaleChanged(SimulationTimeScale timeScale) { TimeScale = timeScale; }
    }

    public readonly struct PauseStateChanged : IGameplayEvent
    {
        public bool IsPaused { get; }
        public PauseStateChanged(bool isPaused) { IsPaused = isPaused; }
    }
}
