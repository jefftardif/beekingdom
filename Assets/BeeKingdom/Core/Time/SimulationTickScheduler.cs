using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Time
{
    public sealed class SimulationTickScheduler
    {
        private const double TenHzInterval = 0.1d;
        private const double FiveHzInterval = 0.2d;
        private const double OneHzInterval = 1d;
        private const double SimulationMinuteInterval = 60d;
        private const double SimulationHourInterval = 3600d;
        private const double SimulationDayInterval = 86400d;

        private double tenHzAccumulator;
        private double fiveHzAccumulator;
        private double oneHzAccumulator;
        private double minuteAccumulator;
        private double hourAccumulator;
        private double dayAccumulator;

        public long TenHzTicks { get; private set; }
        public long FiveHzTicks { get; private set; }
        public long OneHzTicks { get; private set; }

        public void Advance(double deltaSeconds, SimulationTimestamp timestamp, SimulationCalendar calendar, IEventBus eventBus)
        {
            tenHzAccumulator += deltaSeconds;
            fiveHzAccumulator += deltaSeconds;
            oneHzAccumulator += deltaSeconds;
            minuteAccumulator += deltaSeconds;
            hourAccumulator += deltaSeconds;
            dayAccumulator += deltaSeconds;

            while (tenHzAccumulator >= TenHzInterval)
            {
                tenHzAccumulator -= TenHzInterval;
                TenHzTicks++;
                eventBus.Publish(new TickGenerated(SimulationTickFrequency.TenHz, timestamp, TenHzInterval));
            }

            while (fiveHzAccumulator >= FiveHzInterval)
            {
                fiveHzAccumulator -= FiveHzInterval;
                FiveHzTicks++;
                eventBus.Publish(new TickGenerated(SimulationTickFrequency.FiveHz, timestamp, FiveHzInterval));
            }

            while (oneHzAccumulator >= OneHzInterval)
            {
                oneHzAccumulator -= OneHzInterval;
                OneHzTicks++;
                eventBus.Publish(new TickGenerated(SimulationTickFrequency.OneHz, timestamp, OneHzInterval));
            }

            while (minuteAccumulator >= SimulationMinuteInterval)
            {
                minuteAccumulator -= SimulationMinuteInterval;
                eventBus.Publish(new TickGenerated(SimulationTickFrequency.SimulationMinute, timestamp, SimulationMinuteInterval));
                eventBus.Publish(new MinuteElapsed(calendar));
            }

            while (hourAccumulator >= SimulationHourInterval)
            {
                hourAccumulator -= SimulationHourInterval;
                eventBus.Publish(new TickGenerated(SimulationTickFrequency.SimulationHour, timestamp, SimulationHourInterval));
                eventBus.Publish(new HourElapsed(calendar));
            }

            while (dayAccumulator >= SimulationDayInterval)
            {
                dayAccumulator -= SimulationDayInterval;
                eventBus.Publish(new TickGenerated(SimulationTickFrequency.SimulationDay, timestamp, SimulationDayInterval));
                eventBus.Publish(new DayElapsed(calendar));
            }
        }
    }
}
