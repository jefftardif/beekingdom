using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    public enum ManualProductionForecastState
    {
        Producing,
        Full,
        Unavailable
    }

    public sealed class ManualProductionForecast
    {
        private ManualProductionForecast(
            double pending,
            double capacity,
            double ratePerHour,
            double secondsUntilFull,
            ManualProductionForecastState state)
        {
            Pending = pending;
            Capacity = capacity;
            RatePerHour = ratePerHour;
            SecondsUntilFull = secondsUntilFull;
            State = state;
        }

        public double Pending { get; }
        public double Capacity { get; }
        public double RatePerHour { get; }
        public double SecondsUntilFull { get; }
        public ManualProductionForecastState State { get; }
        public double Fill01 => Capacity <= 0d ? 0d : Math.Max(0d, Math.Min(1d, Pending / Capacity));

        public static ManualProductionForecast Calculate(double pending, double capacity, double ratePerHour)
        {
            double safeCapacity = FiniteOrZero(capacity);
            double safeRate = FiniteOrZero(ratePerHour);
            if (safeCapacity <= 0d)
                return new ManualProductionForecast(0d, 0d, Math.Max(0d, safeRate), -1d, ManualProductionForecastState.Unavailable);

            double safePending = Math.Max(0d, Math.Min(safeCapacity, FiniteOrZero(pending)));
            if (safePending >= safeCapacity)
                return new ManualProductionForecast(safePending, safeCapacity, Math.Max(0d, safeRate), 0d, ManualProductionForecastState.Full);
            if (safeRate <= 0d)
                return new ManualProductionForecast(safePending, safeCapacity, 0d, -1d, ManualProductionForecastState.Unavailable);

            double seconds = ((safeCapacity - safePending) / safeRate) * 3600d;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0d)
                return new ManualProductionForecast(safePending, safeCapacity, safeRate, -1d, ManualProductionForecastState.Unavailable);

            return new ManualProductionForecast(safePending, safeCapacity, safeRate, seconds, ManualProductionForecastState.Producing);
        }

        public static bool TryEarliestSecondsUntilFull(IEnumerable<ManualProductionForecast> forecasts, out double seconds)
        {
            seconds = double.MaxValue;
            bool found = false;
            if (forecasts == null) return false;

            foreach (ManualProductionForecast forecast in forecasts)
            {
                if (forecast == null || forecast.State == ManualProductionForecastState.Unavailable) continue;
                if (forecast.SecondsUntilFull < 0d) continue;
                seconds = Math.Min(seconds, forecast.SecondsUntilFull);
                found = true;
            }

            if (!found) seconds = -1d;
            return found;
        }

        private static double FiniteOrZero(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
        }
    }
}
