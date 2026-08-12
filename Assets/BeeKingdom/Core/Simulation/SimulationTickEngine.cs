namespace BeeKingdom.Core.Simulation
{
    public sealed class SimulationTickEngine
    {
        private readonly double fixedStepSeconds;
        private double accumulator;

        public bool IsPaused { get; private set; }
        public double TimeScale { get; private set; } = 1d;
        public long TickIndex { get; private set; }
        public double TotalSeconds { get; private set; }

        public SimulationTickEngine(double fixedStepSeconds = 0.05d)
        {
            this.fixedStepSeconds = fixedStepSeconds <= 0d ? 0.05d : fixedStepSeconds;
        }

        public void SetPaused(bool paused) => IsPaused = paused;
        public void SetTimeScale(double scale) => TimeScale = scale < 0d ? 0d : scale;

        public int Advance(double deltaSeconds, SimulationTickMode mode)
        {
            if (IsPaused || deltaSeconds <= 0d || TimeScale <= 0d) return 0;
            double scaled = deltaSeconds * TimeScale;
            if (mode == SimulationTickMode.Variable)
            {
                RecordTick(scaled);
                return 1;
            }

            accumulator += scaled;
            int ticks = 0;
            while (accumulator >= fixedStepSeconds)
            {
                accumulator -= fixedStepSeconds;
                RecordTick(fixedStepSeconds);
                ticks++;
            }
            return ticks;
        }

        public int FastForward(double seconds)
        {
            return Advance(seconds, SimulationTickMode.FastForward);
        }

        private void RecordTick(double seconds)
        {
            TickIndex++;
            TotalSeconds += seconds;
        }
    }
}
