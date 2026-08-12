namespace BeeKingdom.Core.Time
{
    public sealed class SimulationTimeSystem
    {
        private readonly int daysPerSeason;
        private readonly int seasonsPerYear;

        public double TotalSeconds { get; private set; }
        public double TimeScale { get; private set; } = 1d;
        public bool IsPaused { get; private set; }

        public SimulationTimeSystem(int daysPerSeason = 28, int seasonsPerYear = 4)
        {
            this.daysPerSeason = daysPerSeason <= 0 ? 28 : daysPerSeason;
            this.seasonsPerYear = seasonsPerYear <= 0 ? 4 : seasonsPerYear;
        }

        public void SetTimeScale(double scale) => TimeScale = scale < 0d ? 0d : scale;
        public void SetPaused(bool paused) => IsPaused = paused;

        public SimulationTimePoint Advance(double deltaSeconds)
        {
            if (!IsPaused && deltaSeconds > 0d)
            {
                TotalSeconds += deltaSeconds * TimeScale;
            }

            return GetTimePoint();
        }

        public SimulationTimePoint GetTimePoint()
        {
            int totalMinutes = (int)(TotalSeconds / 60d);
            int minute = totalMinutes % 60;
            int totalHours = totalMinutes / 60;
            int hour = totalHours % 24;
            int dayIndex = totalHours / 24;
            int seasonIndex = (dayIndex / daysPerSeason) % seasonsPerYear;
            int year = (dayIndex / (daysPerSeason * seasonsPerYear)) + 1;
            int dayOfSeason = (dayIndex % daysPerSeason) + 1;
            return new SimulationTimePoint(TotalSeconds, year, dayOfSeason, hour, minute, (SimulationSeason)seasonIndex);
        }

        public bool IsLiveOpsWindowActive(LiveOpsCalendarWindow window)
        {
            return TotalSeconds >= window.StartSeconds && TotalSeconds < window.EndSeconds;
        }
    }

    public readonly struct SimulationTimePoint
    {
        public double TotalSeconds { get; }
        public int Year { get; }
        public int DayOfSeason { get; }
        public int Hour { get; }
        public int Minute { get; }
        public SimulationSeason Season { get; }

        public SimulationTimePoint(double totalSeconds, int year, int dayOfSeason, int hour, int minute, SimulationSeason season)
        {
            TotalSeconds = totalSeconds;
            Year = year;
            DayOfSeason = dayOfSeason;
            Hour = hour;
            Minute = minute;
            Season = season;
        }
    }

    public readonly struct LiveOpsCalendarWindow
    {
        public double StartSeconds { get; }
        public double EndSeconds { get; }

        public LiveOpsCalendarWindow(double startSeconds, double endSeconds)
        {
            StartSeconds = startSeconds < 0d ? 0d : startSeconds;
            EndSeconds = endSeconds < StartSeconds ? StartSeconds : endSeconds;
        }
    }
}
